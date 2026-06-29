using System.Text;

namespace GAC.Core.Content;

/// <summary>
/// Generates URL-safe slugs from arbitrary text. ASCII letters and digits are
/// kept (lower-cased); every other run of characters collapses to a single
/// hyphen, with leading/trailing hyphens trimmed. Input with no ASCII
/// alphanumerics (e.g. an Arabic-only title) yields an empty string, in which
/// case the caller should require a manually-entered slug.
/// </summary>
public static class Slug
{
    public static string From(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);
        var pendingHyphen = false;
        foreach (var ch in input.Trim().ToLowerInvariant())
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
            {
                sb.Append(ch);
                pendingHyphen = false;
            }
            else if (!pendingHyphen)
            {
                sb.Append('-');
                pendingHyphen = true;
            }
        }
        return sb.ToString().Trim('-');
    }
}
