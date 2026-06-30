using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminWarrantyService : IAdminWarrantyService
{
    private readonly ApplicationDbContext _db;
    public AdminWarrantyService(ApplicationDbContext db) => _db = db;

    private async Task<WarrantyPage> EnsureAsync(CancellationToken ct)
    {
        var w = await _db.WarrantyPages.FirstOrDefaultAsync(ct);
        if (w is null)
        {
            w = new WarrantyPage();
            _db.WarrantyPages.Add(w);
            await _db.SaveChangesAsync(ct);
        }
        return w;
    }

    public async Task<WarrantyPage> GetAsync(CancellationToken ct = default)
    {
        var w = await EnsureAsync(ct);
        return await _db.WarrantyPages
            .Include(p => p.Callouts.OrderBy(c => c.SortOrder))
            .FirstAsync(p => p.Id == w.Id, ct);
    }

    public async Task SaveAsync(WarrantyPage page, CancellationToken ct = default)
    {
        var existing = await _db.WarrantyPages.Include(p => p.Callouts).FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            page.Id = 0;
            page.Callouts = NormalizeCallouts(page.Callouts);
            _db.WarrantyPages.Add(page);
        }
        else
        {
            existing.BannerImagePath = page.BannerImagePath;
            existing.BannerLabel = page.BannerLabel; existing.Heading = page.Heading;
            existing.Intro = page.Intro; existing.TermsImagePath = page.TermsImagePath;
            existing.TermsNote = page.TermsNote; existing.ExtendedHeading = page.ExtendedHeading;
            existing.ExtendedIntro = page.ExtendedIntro; existing.ExtendedTableHtml = page.ExtendedTableHtml;
            _db.WarrantyCallouts.RemoveRange(existing.Callouts);   // replace the callout list wholesale
            existing.Callouts = NormalizeCallouts(page.Callouts);
        }
        await _db.SaveChangesAsync(ct);
    }

    // Drop blank rows (the admin "add row" UI can submit empties) and re-index.
    private static List<WarrantyCallout> NormalizeCallouts(IEnumerable<WarrantyCallout> callouts)
        => callouts
            .Where(c => !string.IsNullOrWhiteSpace(c.Lead?.En) || !string.IsNullOrWhiteSpace(c.Lead?.Ar)
                     || !string.IsNullOrWhiteSpace(c.Text?.En) || !string.IsNullOrWhiteSpace(c.Text?.Ar))
            .Select((c, i) => new WarrantyCallout { Lead = c.Lead ?? new(), Text = c.Text ?? new(), SortOrder = i })
            .ToList();
}
