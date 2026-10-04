// Define y normaliza atajos de teclado; rechaza duplicados y combinaciones reservadas.
// Las preferencias inválidas recuperan el conjunto predeterminado completo.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Checkpoint.Core;

public static class Shortcuts
{
    public static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<
        string,
        string
    >
    {
        ["add"] = "Ctrl+N",
        ["search"] = "Ctrl+F",
        ["undo"] = "Ctrl+Z",
        ["view"] = "F6",
        ["hide"] = "Escape",
        ["shortcuts"] = "F1",
        ["edit"] = "F2",
        ["up"] = "ArrowUp",
        ["down"] = "ArrowDown",
        ["first"] = "Home",
        ["last"] = "End",
        ["pageUp"] = "PageUp",
        ["pageDown"] = "PageDown",
        ["gameMenu"] = "Enter",
        ["moveUp"] = "Alt+ArrowUp",
        ["moveDown"] = "Alt+ArrowDown",
        ["global"] = "Ctrl+Alt+C",
    };
    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<
        string,
        string
    >
    {
        ["add"] = "Añadir juego",
        ["search"] = "Buscar juego",
        ["undo"] = "Recuperar último juego eliminado",
        ["view"] = "Cambiar vista",
        ["hide"] = "Ocultar widget",
        ["shortcuts"] = "Atajos de teclado",
        ["edit"] = "Editar juego",
        ["up"] = "Juego anterior",
        ["down"] = "Juego siguiente",
        ["first"] = "Primer juego",
        ["last"] = "Último juego",
        ["pageUp"] = "Página anterior",
        ["pageDown"] = "Página siguiente",
        ["gameMenu"] = "Abrir menú del juego",
        ["moveUp"] = "Mover juego hacia arriba",
        ["moveDown"] = "Mover juego hacia abajo",
        ["global"] = "Mostrar / ocultar desde cualquier aplicación",
    };

    // Convierte etiquetas localizadas y modificadores a una representación estable usada en las comparaciones.
    public static string Canonical(string value)
    {
        var parts = (value ?? "").Split('+', StringSplitOptions.TrimEntries);
        var key = parts[^1];
        key = key.ToLowerInvariant() switch
        {
            "↑" => "ArrowUp",
            "↓" => "ArrowDown",
            "inicio" => "Home",
            "fin" => "End",
            "repág" => "PageUp",
            "avpág" => "PageDown",
            "intro" => "Enter",
            "esc" => "Escape",
            "espacio" => "Space",
            _ => key,
        };
        var modifiers = new HashSet<string>();
        foreach (var part in parts[..^1])
        {
            var modifier = part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => "Ctrl",
                "alt" => "Alt",
                "shift" => "Shift",
                _ => "",
            };
            if (modifier == "" || !modifiers.Add(modifier))
                throw Invalid();
        }
        var special = new[]
        {
            "Escape",
            "ArrowUp",
            "ArrowDown",
            "Home",
            "End",
            "PageUp",
            "PageDown",
            "Enter",
            "Space",
        }.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
        key = special ?? key.ToUpperInvariant();
        if (special is null && !Regex.IsMatch(key, @"^(F([1-9]|1\d|2[0-4])|[A-Z0-9])$"))
            throw Invalid();
        if (key.Length == 1 && !modifiers.Contains("Ctrl") && !modifiers.Contains("Alt"))
            throw Invalid();
        return string.Join(
            '+',
            new[] { "Ctrl", "Alt", "Shift" }.Where(modifiers.Contains).Append(key)
        );
    }

    private static ArgumentException Invalid() =>
        new(
            I18n.T(
                "Combinación no válida. Usa Ctrl, Alt o Shift y una tecla; las letras requieren Ctrl o Alt."
            )
        );

    // Reserva Espacio para navegación y Escape para cierre; el atajo global necesita Ctrl o Alt.
    public static Dictionary<string, string> Validate(IReadOnlyDictionary<string, string>? values)
    {
        var result = new Dictionary<string, string>();
        var seen = new HashSet<string>();
        foreach (var pair in Defaults)
        {
            var value = Canonical(
                values is not null && values.TryGetValue(pair.Key, out var saved)
                    ? saved
                    : pair.Value
            );
            if (!seen.Add(value) || value == "Space" || value == "Escape" && pair.Key != "hide")
                throw new ArgumentException(
                    I18n.T(
                        "Hay atajos repetidos o reservados. Espacio y Escape se conservan para navegar y cerrar."
                    )
                );
            result[pair.Key] = value;
        }
        if (!result["global"].Contains("Ctrl+") && !result["global"].Contains("Alt+"))
            throw Invalid();
        return result;
    }

    public static Dictionary<string, string> Effective(IReadOnlyDictionary<string, string>? values)
    {
        try
        {
            return Validate(values);
        }
        catch (ArgumentException)
        {
            return new(Defaults);
        }
    }
}
