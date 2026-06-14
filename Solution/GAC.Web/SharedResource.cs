namespace GAC.Web;

/// <summary>
/// Marker type for the shared <see cref="Microsoft.Extensions.Localization.IHtmlLocalizer{T}"/>.
/// Lives in the assembly root namespace (NOT in GAC.Web.Resources) so that with
/// ResourcesPath="Resources" the resource base name resolves to
/// GAC.Web.Resources.SharedResource, matching Resources/SharedResource.ar.resx.
/// Resource KEYS are the English source text, so English needs no resx file.
/// </summary>
public sealed class SharedResource
{
}
