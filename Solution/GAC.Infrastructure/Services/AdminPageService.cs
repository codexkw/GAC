using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminPageService : IAdminPageService
{
    private readonly ApplicationDbContext _db;
    public AdminPageService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContentPage>> ListContentAsync(CancellationToken ct = default)
        => await _db.ContentPages.OrderBy(p => p.Slug).ToListAsync(ct);

    public async Task<ContentPage?> GetContentAsync(int id, CancellationToken ct = default)
        => await _db.ContentPages.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> UpdateContentAsync(ContentPage page, CancellationToken ct = default)
    {
        var e = await _db.ContentPages.FirstOrDefaultAsync(p => p.Id == page.Id, ct);
        if (e is null) return false;
        e.Title = page.Title; e.MetaTitle = page.MetaTitle; e.MetaDescription = page.MetaDescription;
        e.IsVisible = page.IsVisible;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<FormPage>> ListFormsAsync(CancellationToken ct = default)
        => await _db.FormPages.OrderBy(p => p.Slug).ToListAsync(ct);

    public async Task<FormPage?> GetFormAsync(int id, CancellationToken ct = default)
        => await _db.FormPages.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> UpdateFormAsync(FormPage page, CancellationToken ct = default)
    {
        var e = await _db.FormPages.FirstOrDefaultAsync(p => p.Id == page.Id, ct);
        if (e is null) return false;
        e.Title = page.Title; e.IntroText = page.IntroText;
        e.MetaTitle = page.MetaTitle; e.MetaDescription = page.MetaDescription; e.IsVisible = page.IsVisible;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
