using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminPageService
{
    Task<IReadOnlyList<ContentPage>> ListContentAsync(CancellationToken ct = default);
    Task<ContentPage?> GetContentAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateContentAsync(ContentPage page, CancellationToken ct = default); // Title, Meta, IsVisible

    Task<IReadOnlyList<FormPage>> ListFormsAsync(CancellationToken ct = default);
    Task<FormPage?> GetFormAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateFormAsync(FormPage page, CancellationToken ct = default);       // Title, Intro, Meta, IsVisible
}
