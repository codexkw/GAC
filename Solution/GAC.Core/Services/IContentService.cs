using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IContentService
{
    Task<HomePage?> GetHomePageAsync();
    Task<WarrantyPage?> GetWarrantyPageAsync();
    Task<ContentPage?> GetContentPageBySlugAsync(string slug);
    Task<FormPage?> GetFormPageBySlugAsync(string slug);
    Task<IReadOnlyList<NewsArticle>> GetPublishedNewsAsync();
    Task<NewsArticle?> GetNewsBySlugAsync(string slug);
    Task<IReadOnlyList<Offer>> GetActiveOffersAsync();
    Task<IReadOnlyList<ContentPage>> GetAllContentPagesAsync();
    Task<IReadOnlyList<FormPage>> GetAllFormPagesAsync();
}
