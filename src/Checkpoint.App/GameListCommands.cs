// Aplica acciones de listas a juegos seleccionados y persiste los cambios de colección.

using System;
using System.Linq;
using System.Text.Json;
using Checkpoint.Core;

namespace Checkpoint.App;

public partial class MainWindow
{
    private Guid[] ReadGameIds(JsonElement message)
    {
        if (
            !message.TryGetProperty("ids", out var values)
            || values.ValueKind != JsonValueKind.Array
            || values.GetArrayLength() is < 1 or > 500
        )
            throw new ArgumentException(I18n.T("Selecciona entre 1 y 500 juegos existentes."));
        return values
            .EnumerateArray()
            .Select(value =>
                value.ValueKind == JsonValueKind.String
                && Guid.TryParse(value.GetString(), out var id)
                    ? id
                    : throw new ArgumentException(
                        I18n.T("Selecciona entre 1 y 500 juegos existentes.")
                    )
            )
            .Distinct()
            .ToArray();
    }

    internal void SelectGameList(string value)
    {
        SwitchCollection(false);
        Preferences.ActiveList = value;
        friendsVisible = false;
        Search.Clear();
        StatusFilter.SelectedIndex = 0;
        Persist();
        Refresh();
    }

    internal void ApplyListAction(
        Guid[] ids,
        string operation,
        string source,
        string? target = null
    )
    {
        GameLists.Apply(Games, ids, Preferences.GameLists, operation, source, target);
        Persist();
        Refresh();
    }
}
