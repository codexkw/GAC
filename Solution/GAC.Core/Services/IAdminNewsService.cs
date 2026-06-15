using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminNewsService
{
    Task<IReadOnlyList<NewsArticle>> ListAsync(CancellationToken ct = default);   // incl. unpublished
    Task<NewsArticle?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default);
    Task<int> CreateAsync(NewsArticle a, CancellationToken ct = default);
    Task<bool> UpdateAsync(NewsArticle a, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
