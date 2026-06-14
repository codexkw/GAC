using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class ContentService : IContentService
{
    private readonly ApplicationDbContext _db;

    public ContentService(ApplicationDbContext db) => _db = db;

    public async Task<HomePage?> GetHomePageAsync()
        => await _db.HomePages
            .AsNoTracking()
            .Include(h => h.Slides.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync();

    public async Task<ContentPage?> GetContentPageBySlugAsync(string slug)
        => await _db.ContentPages
            .AsNoTracking()
            .Where(p => p.IsVisible && p.Slug == slug)
            .Include(p => p.Sections.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync();

    public async Task<FormPage?> GetFormPageBySlugAsync(string slug)
        => await _db.FormPages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsVisible && p.Slug == slug);

    public async Task<IReadOnlyList<NewsArticle>> GetPublishedNewsAsync()
        => await _db.NewsArticles
            .AsNoTracking()
            .Where(n => n.IsPublished)
            .OrderBy(n => n.SortOrder)
            .ToListAsync();

    public async Task<NewsArticle?> GetNewsBySlugAsync(string slug)
        => await _db.NewsArticles
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.IsPublished && n.Slug == slug);

    public async Task<IReadOnlyList<Offer>> GetActiveOffersAsync()
        => await _db.Offers
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.SortOrder)
            .ToListAsync();
}
