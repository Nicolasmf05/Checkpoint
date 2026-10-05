// Una sola ruta aplica resultados a juegos existentes, sin sustituir cambios locales.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    private sealed record AchievementUpdate(bool Changed, List<Exception> Errors)
    {
        internal Exception? BlockingError =>
            Errors.FirstOrDefault(e => e is AchievementServiceException { StopsBatch: true });
    }

    private async Task<AchievementUpdate> UpdateGameAchievements(
        Guid gameId,
        bool steam,
        bool retro,
        CancellationToken token
    )
    {
        List<Exception> errors = [];
        bool changed = false;
        var game = Games.FirstOrDefault(g => g.Id == gameId);
        if (game is null)
            return new(false, errors);
        var steamId = game.SteamAppId;
        var retroId = game.RetroGameId;
        var steamSession = Steam.Session;
        var retroSession = Retro.Session;

        async Task Apply(Func<Task<List<Achievement>>> fetch, bool isSteam)
        {
            try
            {
                var items = await fetch();
                token.ThrowIfCancellationRequested();
                AchievementData.Validate(items);
                var current = Games.FirstOrDefault(g => g.Id == gameId);
                if (
                    current is null
                    || (
                        isSteam
                            ? Steam.Session != steamSession || current.SteamAppId != steamId
                            : Retro.Session != retroSession || current.RetroGameId != retroId
                    )
                )
                    return;
                if (isSteam)
                {
                    current.Achievements = items;
                    current.SyncedAt = DateTimeOffset.UtcNow;
                }
                else
                    current.RetroAchievements = items;
                Store.SaveExistingGame(current);
                changed = true;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
                when (error
                        is IOException
                            or InvalidOperationException
                            or HttpRequestException
                            or JsonException
                            or ArgumentException
                            or OperationCanceledException
                )
            {
                errors.Add(error);
            }
        }
        if (steam && steamId is int appId && steamSession is not null)
            await Apply(
                async () =>
                    (
                        await Steam.Achievements(Preferences.ServiceUrl, appId, token)
                    ).Achievements.ToList(),
                true
            );
        if (retro && retroId is int id && retroSession is not null)
            await Apply(() => Retro.Achievements(id, token), false);
        return new(changed, errors);
    }
}
