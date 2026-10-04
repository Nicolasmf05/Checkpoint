// Detecta procesos de juegos y relaciona instalaciones de Steam con ejecutables activos.
// El temporizador evita revisiones simultáneas y conserva las sesiones ya detectadas.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Checkpoint.Core;
using Microsoft.Win32;

namespace Checkpoint.App;

public partial class MainWindow
{
    internal RetroClient Retro = null!;
    private readonly DispatcherTimer detectionTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private readonly Dictionary<Guid, Window> achievementWindows = [];
    private HashSet<(Guid Game, int Process)> detectedSessions = [];
    private bool detecting;
    private List<InstalledSteamGame> installedSteam = [];
    private DateTimeOffset installedAt;

    private void StartGameDetection()
    {
        Retro = new(Store.DirectoryPath);
        detectionTimer.Tick += async (_, _) => await DetectGames();
        detectionTimer.Start();
        _ = DetectGames();
    }

    private static List<InstalledSteamGame> InstalledSteam()
    {
        var result = new List<InstalledSteamGame>();
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        if (key?.GetValue("SteamPath") is not string root || !Directory.Exists(root))
            return result;
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root };
        var folders = Path.Combine(root, "steamapps", "libraryfolders.vdf");
        if (File.Exists(folders))
            foreach (
                Match match in Regex.Matches(File.ReadAllText(folders), "\"path\"\\s*\"([^\"]+)\"")
            )
                libraries.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
        foreach (var library in libraries)
        {
            var steamapps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamapps))
                continue;
            foreach (
                var file in Directory.EnumerateFiles(steamapps, "appmanifest_*.acf").Take(10000)
            )
            {
                var text = File.ReadAllText(file);
                string Field(string name) =>
                    Regex.Match(text, "\"" + name + "\"\\s*\"([^\"]+)\"").Groups[1].Value;
                string directory = Field("installdir");
                if (
                    int.TryParse(Field("appid"), out int id)
                    && id > 0
                    && !string.IsNullOrWhiteSpace(directory)
                    && !directory.Contains("..")
                    && !Path.IsPathRooted(directory)
                )
                    result.Add(
                        new(
                            id,
                            Field("name"),
                            Path.GetFullPath(Path.Combine(steamapps, "common", directory))
                        )
                    );
            }
        }
        return result;
    }

    private static List<RunningGameProcess> RunningProcesses(bool paths, bool titles)
    {
        var results = new List<RunningGameProcess>();
        foreach (var process in Process.GetProcesses())
            using (process)
            {
                try
                {
                    string? path = null;
                    try
                    {
                        if (paths)
                            path = process.MainModule?.FileName;
                    }
                    catch (Exception ex)
                        when (ex
                                is System.ComponentModel.Win32Exception
                                    or InvalidOperationException
                                    or NotSupportedException
                        ) { }
                    results.Add(
                        new(
                            process.Id,
                            process.ProcessName,
                            titles ? process.MainWindowTitle : "",
                            path
                        )
                    );
                }
                catch (Exception ex)
                    when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                { }
            }
        return results;
    }

    private async Task DetectGames()
    {
        if (
            detecting
            || !Preferences.DetectGames
            || shutdown.IsCancellationRequested
            || App.Diagnostics
        )
            return;
        var candidates = Games
            .Where(g => g.SteamAppId.HasValue || !string.IsNullOrWhiteSpace(g.DetectionProcess))
            .ToArray();
        if (candidates.Length == 0)
        {
            detectedSessions.Clear();
            return;
        }
        detecting = true;
        try
        {
            if (
                candidates.Any(g => g.SteamAppId.HasValue)
                && DateTimeOffset.UtcNow - installedAt > TimeSpan.FromMinutes(5)
            )
            {
                installedAt = DateTimeOffset.UtcNow;
                try
                {
                    installedSteam = await Task.Run(InstalledSteam);
                }
                catch (Exception ex)
                    when (ex
                            is IOException
                                or UnauthorizedAccessException
                                or System.ComponentModel.Win32Exception
                    )
                {
                    Notice(
                        I18n.T(
                            "No se pudo revisar la detección de juegos. Puedes abrir los logros manualmente."
                        )
                    );
                }
            }
            var running = await Task.Run(() =>
                RunningProcesses(
                    candidates.Any(g => g.SteamAppId.HasValue),
                    candidates.Any(g => !string.IsNullOrWhiteSpace(g.DetectionWindowTitle))
                )
            );
            if (shutdown.IsCancellationRequested)
                return;
            var active = new HashSet<(Guid Game, int Process)>();
            foreach (var game in candidates)
            foreach (
                var process in running.Where(p => GameDetection.Matches(game, p, installedSteam))
            )
            {
                if (!active.Add((game.Id, process.Id)))
                    continue;
                if (!detectedSessions.Contains((game.Id, process.Id)))
                    OpenDetectedAchievements(game);
            }
            detectedSessions = active;
        }
        catch (Exception ex)
            when (ex
                    is IOException
                        or UnauthorizedAccessException
                        or System.ComponentModel.Win32Exception
            )
        {
            Notice(
                I18n.T(
                    "No se pudo revisar la detección de juegos. Puedes abrir los logros manualmente."
                )
            );
        }
        finally
        {
            detecting = false;
        }
    }

    internal void OpenDetectedAchievements(Game game)
    {
        if (achievementWindows.TryGetValue(game.Id, out var existing))
        {
            if (existing.WindowState == WindowState.Minimized)
                existing.WindowState = WindowState.Normal;
            return;
        }
        var window = Dialogs.Achievements(this, game, true);
        achievementWindows[game.Id] = window;
        window.Closed += (_, _) => achievementWindows.Remove(game.Id);
    }

    private readonly System.Collections.Generic.HashSet<Guid> refreshingAchievements = [];
    private readonly System.Collections.Generic.Dictionary<
        Guid,
        DateTimeOffset
    > achievementRefreshTimes = [];

    internal void ResetAchievementRefresh() => achievementRefreshTimes.Clear();

    internal async Task RefreshGameAchievements(Game game)
    {
        if (shutdown.IsCancellationRequested || !refreshingAchievements.Add(game.Id))
            return;
        try
        {
            if (
                achievementRefreshTimes.TryGetValue(game.Id, out var last)
                && DateTimeOffset.UtcNow - last < TimeSpan.FromSeconds(60)
            )
                return;
            achievementRefreshTimes[game.Id] = DateTimeOffset.UtcNow;
            if (game.SteamAppId.HasValue && Steam.Session is not null)
                await Sync(false, game);
            if (game.RetroGameId is int id && Retro.Session is { } retroSession)
            {
                try
                {
                    var result = await Retro.Achievements(id, shutdown.Token);
                    if (shutdown.IsCancellationRequested || Retro.Session != retroSession)
                        return;
                    var validation = new Game { Title = game.Title, RetroAchievements = result };
                    GameRules.Validate(validation);
                    var current = Games.FirstOrDefault(g => g.Id == game.Id);
                    if (current is null || current.RetroGameId != id)
                        return;
                    current.RetroAchievements = result;
                    Persist();
                    Refresh();
                }
                catch (Exception ex)
                    when (ex
                            is IOException
                                or InvalidOperationException
                                or OperationCanceledException
                                or ArgumentException
                    )
                {
                    if (!shutdown.IsCancellationRequested)
                        Notice(I18n.Error(ex));
                }
            }
        }
        finally
        {
            refreshingAchievements.Remove(game.Id);
        }
    }
}
