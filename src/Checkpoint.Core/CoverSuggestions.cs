// Valida identificadores de imágenes de IGDB y recuerda las propuestas rechazadas.
// Una carátula local tiene prioridad sobre cualquier sugerencia automática.

using System.Text.RegularExpressions;

namespace Checkpoint.Core;

public sealed record IgdbCover(int Id, string Name, string ImageId, int? Year, double Score);

public static class CoverSuggestions
{
    public static bool ValidImageId(string? id) =>
        id is not null && Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{1,80}\z");

    public static bool ShouldSuggest(Game game) =>
        game.Tracked
        && game.CustomCover is null
        && !string.Equals(game.IgdbCoverSearchTitle, game.Title, StringComparison.Ordinal);

    public static void Reject(Game game, string id)
    {
        if (!ValidImageId(id))
            throw new ArgumentException(I18n.T("La configuración de carátulas no es válida."));
        if (!game.RejectedIgdbCovers.Contains(id) && game.RejectedIgdbCovers.Count < 200)
            game.RejectedIgdbCovers.Add(id);
    }
}
