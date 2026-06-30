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
    // model's cells to the kept row count (pad with blanks, truncate extras) so the matrix
    // always renders without gaps.
    private static (List<CostServiceRow> rows, List<CostServiceModel> models) Normalize(CostOfServicePage page)
    {
        var rows = (page.Rows ?? new())
            .Where(r => !string.IsNullOrWhiteSpace(r.Label?.En) || !string.IsNullOrWhiteSpace(r.Label?.Ar))
            .Select((r, i) => new CostServiceRow { Label = r.Label ?? new(), SortOrder = i })
            .ToList();
        var n = rows.Count;

        var models = (page.Models ?? new())
            .Where(m => !string.IsNullOrWhiteSpace(m.Name)
                     || (m.Cells ?? new()).Any(c => !string.IsNullOrWhiteSpace(c.Value)))
            .Select((m, mi) =>
            {
                var cells = (m.Cells ?? new())
                    .Take(n)
                    .Select((c, ci) => new CostServiceCell { SortOrder = ci, Value = c.Value })
                    .ToList();
                while (cells.Count < n) cells.Add(new CostServiceCell { SortOrder = cells.Count, Value = null });
                return new CostServiceModel { Name = m.Name ?? "", SortOrder = mi, Cells = cells };
            })
            .ToList();

        return (rows, models);
    }
}
