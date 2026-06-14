namespace GAC.Core.Content;

/// <summary>
/// A bilingual string (English + Arabic). Mapped as an EF owned type to
/// {Field}_En / {Field}_Ar columns. Exactly two languages, so no translation tables.
/// </summary>
public class LocalizedText
{
    public string? En { get; set; }
    public string? Ar { get; set; }

    /// <summary>Returns the value for the given two-letter culture, falling back to the other language, then empty.</summary>
    public string Get(string culture)
    {
        var primary = culture == "ar" ? Ar : En;
        return primary ?? En ?? Ar ?? string.Empty;
    }

    // Convenience for seeding/assignment: `LocalizedText t = "Hello";`
    public static implicit operator LocalizedText(string en) => new() { En = en };
}
