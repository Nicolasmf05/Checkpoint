using System;
using System.Collections.Generic;
using System.Linq;

namespace Checkpoint.Core;

public static class Themes
{
    public static IReadOnlyList<string> Ids { get; } = Array.AsReadOnly(new[] { "dark", "light", "midnight", "ocean", "forest", "plum", "amber", "contrast" });
    public static string Id(Settings settings) => settings.Theme is not null && Ids.Contains(settings.Theme)
        ? settings.Theme : settings.LightTheme ? "light" : "dark";
    public static bool IsLight(Settings settings) => Id(settings) == "light";
    public static string Name(string id) => id switch
    {
        "light" => I18n.T("Claro"), "midnight" => I18n.T("Medianoche"), "ocean" => I18n.T("Océano"),
        "forest" => I18n.T("Bosque"), "plum" => I18n.T("Ciruela"), "amber" => I18n.T("Ámbar"),
        "contrast" => I18n.T("Alto contraste"), _ => I18n.T("Oscuro")
    };
}
