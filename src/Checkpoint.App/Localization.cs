// Actualiza recursos WPF, bandeja y vistas cuando cambia el idioma seleccionado.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    private static void LoadLanguageResources(string language)
    {
        I18n.SetLanguage(language);
        using var stream =
            typeof(MainWindow).Assembly.GetManifestResourceStream(
                "Checkpoint.App.LocalizationKeys.json"
            ) ?? throw new InvalidOperationException("Missing localization keys.");
        var keys = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
        foreach (var pair in keys)
            Application.Current.Resources[pair.Key] = I18n.T(pair.Value);
    }

    internal void ApplyLanguage()
    {
        LoadLanguageResources(Preferences.Language);
        var items = tray.ContextMenuStrip!.Items;
        items[0].Text = I18n.T("Mostrar / ocultar");
        items[1].Text = I18n.T("Añadir juego");
        items[2].Text = I18n.T("Salir");
        NoticeText.Text = "";
        FriendsView.RefreshLanguage();
        ApplyPreferences();
        Refresh();
    }
}
