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
            .Include(h => h.Slides.OrderBy(s => s.SortOrder))
            .Include(h => h.Promo!).ThenInclude(p => p.Campaigns.OrderBy(c => c.SortOrder))
            .Include(h => h.DualCards.OrderBy(c => c.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync();

    public async Task<ContentPage?> GetContentPageBySlugAsync(string slug)
        => await _db.ContentPages
            .Include(p => p.Sections.OrderBy(s => s.SortOrder))
            .AsNoTracking()
            .Where(p => p.IsVisible && p.Slug == slug)
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

    public async Task<IReadOnlyList<ContentPage>> GetAllContentPagesAsync()
        => await _db.ContentPages
            .AsNoTracking()
            .Where(p => p.IsVisible)
            .OrderBy(p => p.Slug)
            .ToListAsync();

    public async Task<IReadOnlyList<FormPage>> GetAllFormPagesAsync()
        => await _db.FormPages
            .AsNoTracking()
            .Where(p => p.IsVisible)
            .OrderBy(p => p.Slug)
            .ToListAsync();
}
