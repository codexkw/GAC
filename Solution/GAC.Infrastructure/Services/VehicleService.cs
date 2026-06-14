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
            .Include(v => v.Trims)
            .Include(v => v.SpecGroups).ThenInclude(g => g.Rows)
            .Include(v => v.Colors)
            .Include(v => v.Features)
            .AsNoTracking()
            .Where(v => v.IsVisible && v.Slug == slug)
            .FirstOrDefaultAsync();
}
