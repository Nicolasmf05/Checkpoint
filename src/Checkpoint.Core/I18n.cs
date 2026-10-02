using System.Text.Json;

namespace Checkpoint.Core;

/// <summary>Built-in interface translations; user content and wire values remain unchanged.</summary>
public static class I18n
{
    private static readonly IReadOnlyDictionary<string,string> english = LoadEnglish();
    public static string Language { get; private set; } = "es";
    public static bool IsEnglish => Language == "en";
    public static void SetLanguage(string? language) => Language = language == "en" ? "en" : "es";
    public static string T(string spanish) => IsEnglish && english.TryGetValue(spanish,out var translated) ? translated : spanish;
    private static Dictionary<string,string> LoadEnglish()
    {
        using var stream = typeof(I18n).Assembly.GetManifestResourceStream("Checkpoint.Core.Localization.en.json")
            ?? throw new InvalidOperationException("Missing English localization resource.");
        return JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!;
    }
}
