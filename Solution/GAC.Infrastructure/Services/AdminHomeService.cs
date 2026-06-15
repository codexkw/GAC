using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminHomeService : IAdminHomeService
{
    private readonly ApplicationDbContext _db;
    public AdminHomeService(ApplicationDbContext db) => _db = db;

    private async Task<HomePage> EnsureHomeAsync(CancellationToken ct)
    {
        var home = await _db.HomePages.FirstOrDefaultAsync(ct);
        if (home is null)
        {
            home = new HomePage();
            _db.HomePages.Add(home);
            await _db.SaveChangesAsync(ct);
        }
        return home;
    }

    public async Task<IReadOnlyList<HeroSlide>> ListSlidesAsync(CancellationToken ct = default)
        => await _db.HeroSlides.OrderBy(s => s.SortOrder).ToListAsync(ct);

    public async Task<HeroSlide?> GetSlideAsync(int id, CancellationToken ct = default)
        => await _db.HeroSlides.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<int> CreateSlideAsync(HeroSlide slide, CancellationToken ct = default)
    {
        var home = await EnsureHomeAsync(ct);
        slide.HomePageId = home.Id;
        slide.SortOrder = await _db.HeroSlides.CountAsync(ct);
        _db.HeroSlides.Add(slide);
        await _db.SaveChangesAsync(ct);
        return slide.Id;
    }

    public async Task<bool> UpdateSlideAsync(HeroSlide slide, CancellationToken ct = default)
    {
        var e = await _db.HeroSlides.FirstOrDefaultAsync(s => s.Id == slide.Id, ct);
        if (e is null) return false;
        e.ImagePath = slide.ImagePath; e.Heading = slide.Heading; e.Subheading = slide.Subheading;
        e.CtaText = slide.CtaText; e.CtaLink = slide.CtaLink;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteSlideAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.HeroSlides.FindAsync([id], ct);
        if (s is null) return false;
        _db.HeroSlides.Remove(s);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveSlideAsync(int id, int direction, CancellationToken ct = default)
    {
        var all = await _db.HeroSlides.OrderBy(s => s.SortOrder).ToListAsync(ct);
        var idx = all.FindIndex(s => s.Id == id);
        if (idx < 0) return false;
        var swap = idx + direction;
        if (swap < 0 || swap >= all.Count) return false;
        (all[idx].SortOrder, all[swap].SortOrder) = (all[swap].SortOrder, all[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
