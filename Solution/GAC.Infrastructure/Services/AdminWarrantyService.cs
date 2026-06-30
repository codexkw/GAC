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
            .Include(p => p.BrandRows.OrderBy(r => r.SortOrder))
            .AsSplitQuery()
            .FirstAsync(p => p.Id == w.Id, ct);
    }

    public async Task SaveAsync(WarrantyPage page, CancellationToken ct = default)
    {
        var existing = await _db.WarrantyPages
            .Include(p => p.Callouts)
            .Include(p => p.BrandRows)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            page.Id = 0;
            page.Callouts = NormalizeCallouts(page.Callouts);
            page.BrandRows = NormalizeBrandRows(page.BrandRows);
            _db.WarrantyPages.Add(page);
        }
        else
        {
            existing.BannerImagePath = page.BannerImagePath;
            existing.BannerLabel = page.BannerLabel; existing.Heading = page.Heading;
            existing.Intro = page.Intro; existing.TermsImagePath = page.TermsImagePath;
            existing.TermsNote = page.TermsNote; existing.ExtendedHeading = page.ExtendedHeading;
            existing.ExtendedIntro = page.ExtendedIntro; existing.ExtendedTableHtml = page.ExtendedTableHtml;
            existing.TableBrandHeader = page.TableBrandHeader;
            existing.TableMfrWarrantyHeader = page.TableMfrWarrantyHeader;
            existing.TableMfrRoadsideHeader = page.TableMfrRoadsideHeader;
            existing.TableExtWarrantyHeader = page.TableExtWarrantyHeader;
            existing.TableExtRoadsideHeader = page.TableExtRoadsideHeader;
            existing.TablePolicyHeader = page.TablePolicyHeader;
            _db.WarrantyCallouts.RemoveRange(existing.Callouts);   // replace the callout list wholesale
            existing.Callouts = NormalizeCallouts(page.Callouts);
            _db.WarrantyBrandRows.RemoveRange(existing.BrandRows); // replace the brand table wholesale
            existing.BrandRows = NormalizeBrandRows(page.BrandRows);
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

    // Drop fully-blank brand rows (the admin "add row" UI can submit empties) and re-index.
    private static List<WarrantyBrandRow> NormalizeBrandRows(IEnumerable<WarrantyBrandRow> rows)
        => rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Brand)
                     || HasText(r.ManufacturerWarranty) || HasText(r.ManufacturerRoadside)
                     || HasText(r.ExtendedWarranty) || HasText(r.ExtendedRoadside)
                     || !string.IsNullOrWhiteSpace(r.PolicyUrl))
            .Select((r, i) => new WarrantyBrandRow
            {
                Brand = r.Brand ?? "",
                ManufacturerWarranty = r.ManufacturerWarranty ?? new(),
                ManufacturerRoadside = r.ManufacturerRoadside ?? new(),
                ExtendedWarranty = r.ExtendedWarranty ?? new(),
                ExtendedRoadside = r.ExtendedRoadside ?? new(),
                PolicyUrl = string.IsNullOrWhiteSpace(r.PolicyUrl) ? null : r.PolicyUrl.Trim(),
                SortOrder = i
            })
            .ToList();

    private static bool HasText(LocalizedText? t)
        => t is not null && (!string.IsNullOrWhiteSpace(t.En) || !string.IsNullOrWhiteSpace(t.Ar));
}
