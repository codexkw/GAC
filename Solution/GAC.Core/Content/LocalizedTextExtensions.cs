using System.Globalization;

namespace GAC.Core.Content;

public static class LocalizedTextExtensions
{
    /// <summary>Value for the ambient UI culture (ar → Arabic, else English), with fallback. Null-safe.</summary>
    public static string Localize(this LocalizedText? text)
    {
        if (text is null) return string.Empty;
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return text.Get(culture);
    }
}
