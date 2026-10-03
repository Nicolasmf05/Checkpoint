using System.Text.RegularExpressions;

namespace Checkpoint.Core;

// Only this compatibility boundary knows the historical service prefix.
// Public presentation and input use the complete Checkpoint name.
public static class FriendCodes
{
    public const int DisplayLength = 23;
    private const string LegacyPrefix = "cp-";
    private const string Prefix = "checkpoint-";

    private static string Suffix(string code)
    {
        string value = code.Trim().ToLowerInvariant();
        string suffix = value.StartsWith(Prefix, StringComparison.Ordinal) ? value[Prefix.Length..]
            : value.StartsWith(LegacyPrefix, StringComparison.Ordinal) ? value[LegacyPrefix.Length..] : "";
        if (!Regex.IsMatch(suffix, "^[a-f0-9]{12}$", RegexOptions.CultureInvariant))
            throw new ArgumentException(I18n.T("El código tiene el formato checkpoint- seguido de 12 caracteres."));
        return suffix;
    }

    public static string Display(string code) => Prefix + Suffix(code);
    internal static string ForService(string code) => LegacyPrefix + Suffix(code);
}
