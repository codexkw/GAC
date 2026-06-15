using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminNewsService : IAdminNewsService
{
    private readonly ApplicationDbContext _db;
    public AdminNewsService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<NewsArticle>> ListAsync(CancellationToken ct = default)
        => await _db.NewsArticles.OrderByDescending(a => a.PublishedOn).ThenBy(a => a.SortOrder).ToListAsync(ct);

    public async Task<NewsArticle?> GetAsync(int id, CancellationToken ct = default)
        => await _db.NewsArticles.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default)
        => await _db.NewsArticles.AnyAsync(a => a.Slug == slug && (exceptId == null || a.Id != exceptId), ct);

    public async Task<int> CreateAsync(NewsArticle a, CancellationToken ct = default)
    {
        _db.NewsArticles.Add(a);
        await _db.SaveChangesAsync(ct);
        return a.Id;
    }

    public async Task<bool> UpdateAsync(NewsArticle a, CancellationToken ct = default)
    {
        var e = await _db.NewsArticles.FirstOrDefaultAsync(x => x.Id == a.Id, ct);
        if (e is null) return false;
        e.Slug = a.Slug; e.IsPublished = a.IsPublished; e.PublishedOn = a.PublishedOn;
        e.Title = a.Title; e.Excerpt = a.Excerpt; e.Body = a.Body; e.ImagePath = a.ImagePath; e.SortOrder = a.SortOrder;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.NewsArticles.FindAsync([id], ct);
        if (a is null) return false;
        _db.NewsArticles.Remove(a);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
