using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminCostOfServiceService : IAdminCostOfServiceService
{
    private readonly ApplicationDbContext _db;
    public AdminCostOfServiceService(ApplicationDbContext db) => _db = db;

    private async Task<CostOfServicePage> EnsureAsync(CancellationToken ct)
    {
        var p = await _db.CostOfServicePages.FirstOrDefaultAsync(ct);
        if (p is null)
        {
            p = new CostOfServicePage();
            _db.CostOfServicePages.Add(p);
            await _db.SaveChangesAsync(ct);
        }
        return p;
    }

    public async Task<CostOfServicePage> GetAsync(CancellationToken ct = default)
    {
        var p = await EnsureAsync(ct);
        return await _db.CostOfServicePages
            .Include(x => x.Rows.OrderBy(r => r.SortOrder))
            .Include(x => x.Models.OrderBy(m => m.SortOrder)).ThenInclude(m => m.Cells.OrderBy(c => c.SortOrder))
            .AsSplitQuery()
            .FirstAsync(x => x.Id == p.Id, ct);
    }

    public async Task SaveAsync(CostOfServicePage page, CancellationToken ct = default)
    {
        var existing = await _db.CostOfServicePages
            .Include(p => p.Rows)
            .Include(p => p.Models).ThenInclude(m => m.Cells)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);

        var (rows, models) = Normalize(page);
        if (existing is null)
        {
            page.Id = 0;
            page.Rows = rows;
            page.Models = models;
            _db.CostOfServicePages.Add(page);
        }
        else
        {
            existing.Title = page.Title;
            existing.ButtonLabel = page.ButtonLabel;
            existing.ButtonUrl = page.ButtonUrl;
            existing.TableHeadLabel = page.TableHeadLabel;
            existing.FooterNote = page.FooterNote;
            // Replace the matrix wholesale (drop cells, then models, then rows).
            _db.CostServiceCells.RemoveRange(existing.Models.SelectMany(m => m.Cells));
            _db.CostServiceModels.RemoveRange(existing.Models);
            _db.CostServiceRows.RemoveRange(existing.Rows);
            existing.Rows = rows;
            existing.Models = models;
        }
        await _db.SaveChangesAsync(ct);
    }

    // Drop blank rows/models (the grid UI can submit empties), re-index, and align each
    // model's cells to the KEPT rows. Cells bind positionally to rows by index, so a dropped
    // row must drop the cell at THAT index in every model (not a tail truncation) — otherwise
    // blanking a non-last interval's label silently shifts/loses prices.
    private static (List<CostServiceRow> rows, List<CostServiceModel> models) Normalize(CostOfServicePage page)
    {
        var srcRows = page.Rows ?? new();
        // Original indices of the rows we keep, in order — used to pick the matching cell
        // from each model so cells stay paired with their interval.
        var keptRowIdx = new List<int>();
        for (var i = 0; i < srcRows.Count; i++)
        {
            var lbl = srcRows[i].Label;
            if (!string.IsNullOrWhiteSpace(lbl?.En) || !string.IsNullOrWhiteSpace(lbl?.Ar))
                keptRowIdx.Add(i);
        }

        var rows = keptRowIdx
            .Select((origIdx, newIdx) => new CostServiceRow { Label = srcRows[origIdx].Label ?? new(), SortOrder = newIdx })
            .ToList();

        var models = (page.Models ?? new())
            .Where(m => !string.IsNullOrWhiteSpace(m.Name)
                     || (m.Cells ?? new()).Any(c => !string.IsNullOrWhiteSpace(c.Value)))
            .Select((m, mi) =>
            {
                var src = m.Cells ?? new();
                var cells = keptRowIdx
                    .Select((origIdx, newIdx) => new CostServiceCell
                    {
                        SortOrder = newIdx,
                        Value = origIdx < src.Count ? src[origIdx].Value : null   // pad when the model has fewer cells than rows
                    })
                    .ToList();
                return new CostServiceModel { Name = m.Name ?? "", SortOrder = mi, Cells = cells };
            })
            .ToList();

        return (rows, models);
    }
}
