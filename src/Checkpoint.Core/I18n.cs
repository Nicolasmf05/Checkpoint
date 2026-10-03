using System.Text.Json;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;

namespace Checkpoint.Core;

/// <summary>Built-in interface translations; user content and wire values remain unchanged.</summary>
public static class I18n
{
    private static readonly IReadOnlyDictionary<string,string> english = LoadEnglish();
    public static string Language { get; private set; } = "es";
    public static bool IsEnglish => Language == "en";
    public static void SetLanguage(string? language)
    {
        Language = language == "en" ? "en" : "es";
        var culture = CultureInfo.GetCultureInfo(IsEnglish ? "en-US" : "es-ES");
        CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture; CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
    public static bool TryTranslateKnown(string text, out string translated)
    {
        if (english.TryGetValue(text, out var value)) { translated = IsEnglish ? value : text; return true; }
        foreach (var pair in english)
            if (pair.Value == text) { translated = IsEnglish ? text : pair.Key; return true; }
        translated = ""; return false;
    }
    public static string Error(Exception error)
    {
        if (TryTranslateKnown(error.Message, out var known)) return known;
        return T(error switch
        {
            HttpRequestException => "Sin conexión. Puedes seguir usando tu biblioteca local.",
            OperationCanceledException => "La operación se canceló o agotó el tiempo de espera. Vuelve a intentarlo.",
            CryptographicException => "Windows no pudo guardar la sesión de forma segura. Vuelve a abrir Checkpoint con tu usuario habitual.",
            UnauthorizedAccessException => "No tienes permiso para acceder a este archivo o carpeta.",
            IOException => "No se pudo leer o guardar el archivo. Comprueba que esté disponible y vuelve a intentarlo.",
            JsonException => "Los datos recibidos no tienen un formato válido.",
            _ => "No se pudo completar la operación. Vuelve a intentarlo."
        });
    }
    public static string T(string spanish) => IsEnglish && english.TryGetValue(spanish,out var translated) ? translated : spanish;
    private static Dictionary<string,string> LoadEnglish()
    {
        using var stream = typeof(I18n).Assembly.GetManifestResourceStream("Checkpoint.Core.Localization.en.json")
            ?? throw new InvalidOperationException("Missing English localization resource.");
        return JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!;
    }
}
