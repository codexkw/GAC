namespace GAC.Core.Services;

public interface IHtmlSanitizerService
{
    /// <summary>Strip everything except a small formatting allow-list. Null/blank → "".</summary>
    string Sanitize(string? html);
}
