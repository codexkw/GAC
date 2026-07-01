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
        e.ImagePath = slide.ImagePath; e.LogoImagePath = slide.LogoImagePath; e.Heading = slide.Heading; e.Subheading = slide.Subheading;
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

    public async Task<HomePage> GetHomeAggregateAsync(CancellationToken ct = default)
    {
        var home = await EnsureHomeAsync(ct);
        return await _db.HomePages
            .Include(h => h.Promo!).ThenInclude(p => p.Campaigns.OrderBy(c => c.SortOrder))
            .Include(h => h.DualCards.OrderBy(c => c.SortOrder))
            .FirstAsync(h => h.Id == home.Id, ct);
    }

    public async Task SavePromoAsync(PromoSection promo, CancellationToken ct = default)
    {
        var home = await EnsureHomeAsync(ct);
        var existing = await _db.PromoSections.Include(p => p.Campaigns)
            .FirstOrDefaultAsync(p => p.HomePageId == home.Id, ct);
        if (existing is null)
        {
            promo.Id = 0;
            promo.HomePageId = home.Id;
            promo.Campaigns = NormalizeCampaigns(promo.Campaigns);
            _db.PromoSections.Add(promo);
        }
        else
        {
            existing.ImagePath = promo.ImagePath;
            existing.Eyebrow = promo.Eyebrow; existing.Heading = promo.Heading;
            existing.Description = promo.Description; existing.CtaText = promo.CtaText;
            existing.CtaLink = promo.CtaLink;
            _db.PromoCampaigns.RemoveRange(existing.Campaigns);   // replace the bullet list wholesale
            existing.Campaigns = NormalizeCampaigns(promo.Campaigns);
        }
        await _db.SaveChangesAsync(ct);
    }

    // Drop blank rows (the admin "add row" UI can submit empties) and re-index.
    private static List<PromoCampaign> NormalizeCampaigns(IEnumerable<PromoCampaign> campaigns)
        => campaigns
            .Where(c => !string.IsNullOrWhiteSpace(c.Text?.En) || !string.IsNullOrWhiteSpace(c.Text?.Ar))
            .Select((c, i) => new PromoCampaign { Text = c.Text ?? new LocalizedText(), SortOrder = i })
            .ToList();

    public async Task<bool> SaveCardAsync(DualCard card, CancellationToken ct = default)
    {
        var e = await _db.DualCards.FirstOrDefaultAsync(c => c.Id == card.Id, ct);
        if (e is null) return false;
        e.ImagePath = card.ImagePath; e.Link = card.Link;
        e.Eyebrow = card.Eyebrow; e.Title = card.Title;
        e.Description = card.Description; e.ButtonText = card.ButtonText;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> CreateCardAsync(DualCard card, CancellationToken ct = default)
    {
        var home = await EnsureHomeAsync(ct);
        card.Id = 0;
        card.HomePageId = home.Id;
        // Append after the highest existing SortOrder — using a count would collide
        // with a survivor's SortOrder after a non-last card is deleted.
        card.SortOrder = 1 + (await _db.DualCards
            .Where(c => c.HomePageId == home.Id)
            .MaxAsync(c => (int?)c.SortOrder, ct) ?? -1);
        _db.DualCards.Add(card);
        await _db.SaveChangesAsync(ct);
        return card.Id;
    }

    public async Task<bool> DeleteCardAsync(int id, CancellationToken ct = default)
    {
        var card = await _db.DualCards.FindAsync([id], ct);
        if (card is null) return false;
        _db.DualCards.Remove(card);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
