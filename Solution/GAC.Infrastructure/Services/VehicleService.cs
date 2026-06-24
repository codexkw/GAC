using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly ApplicationDbContext _db;

    public VehicleService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Vehicle>> GetVisibleAsync()
        => await _db.Vehicles
            .Include(v => v.Images)
            .AsNoTracking()
            .Where(v => v.IsVisible)
            .OrderBy(v => v.SortOrder)
            .ToListAsync();

    public async Task<Vehicle?> GetBySlugAsync(string slug)
        => await _db.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Features).ThenInclude(f => f.Bullets)
            .Include(v => v.SpecGroups).ThenInclude(g => g.Rows)
            .Include(v => v.Colors)
            .Include(v => v.Trims).ThenInclude(t => t.PriceRows)
            .Include(v => v.Headings)
            .Include(v => v.Stats)
            .Include(v => v.Sliders).ThenInclude(s => s.Slides)
            .Include(v => v.GalleryTabs).ThenInclude(g => g.Images)
            .Include(v => v.Cards)
            .Include(v => v.SafetyToggles)
            .Include(v => v.WarrantyLinks)
            .Include(v => v.Quality)
            // One query per collection. A single query with ~13 collection JOINs produces a
            // cartesian explosion (row count = product of every collection's size) that times out
            // on SQL Server for content-rich cars. AsSplitQuery issues a separate query per
            // collection instead. NOTE: this only reproduces on a real relational provider —
            // EF InMemory (used by the unit/integration tests) does not JOIN, so it cannot catch it.
            .AsSplitQuery()
            .AsNoTracking()
            .Where(v => v.IsVisible && v.Slug == slug)
            .FirstOrDefaultAsync();
}
