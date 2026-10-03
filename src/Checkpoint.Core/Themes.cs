using System;
using System.Collections.Generic;
using System.Linq;

namespace Checkpoint.Core;

public static class Themes
{
    public static IReadOnlyList<string> Ids { get; } = Array.AsReadOnly(new[] { "dark", "light", "midnight", "ocean", "forest", "plum", "amber", "contrast", "cyber-purple", "electric-blue", "neon-lime", "black-red", "black-orange", "synthwave", "blue-white", "purple-dark", "emerald-neutral", "black-white", "navy-cyan", "coral-cream", "orange-charcoal", "indigo-gray" });
    public static string Id(Settings settings) => settings.Theme is not null && Ids.Contains(settings.Theme)
        ? settings.Theme : settings.LightTheme ? "light" : "dark";
    public static bool IsLight(Settings settings) => Id(settings) is "light" or "blue-white" or "emerald-neutral" or "coral-cream" or "indigo-gray";
    public static string Name(string id) => id switch
    {
        "light" => I18n.T("Claro"), "midnight" => I18n.T("Medianoche"), "ocean" => I18n.T("Océano"),
        "forest" => I18n.T("Bosque"), "plum" => I18n.T("Ciruela"), "amber" => I18n.T("Ámbar"),
        "contrast" => I18n.T("Alto contraste"),
        "cyber-purple" => I18n.T("Púrpura cibernético"),
        "electric-blue" => I18n.T("Azul eléctrico"),
        "neon-lime" => I18n.T("Lima neón"),
        "black-red" => I18n.T("Negro y rojo"),
        "black-orange" => I18n.T("Negro y naranja"),
        "synthwave" => I18n.T("Onda sintética"),
        "blue-white" => I18n.T("Azul y blanco"),
        "purple-dark" => I18n.T("Púrpura oscuro"),
        "emerald-neutral" => I18n.T("Esmeralda y neutro"),
        "black-white" => I18n.T("Negro y blanco"),
        "navy-cyan" => I18n.T("Marino y cian"),
        "coral-cream" => I18n.T("Coral y crema"),
        "orange-charcoal" => I18n.T("Naranja y carbón"),
        "indigo-gray" => I18n.T("Índigo y gris suave"), _ => I18n.T("Oscuro")
    };
}
