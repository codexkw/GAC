namespace GAC.Web.Resources;

/// <summary>
/// Marker type for the shared <see cref="Microsoft.Extensions.Localization.IHtmlLocalizer{T}"/>.
/// Translations live in SharedResource.{culture}.resx. Resource KEYS are the English source
/// text, so English needs no resx file (a missing key returns the key verbatim).
/// </summary>
public sealed class SharedResource
{
}
