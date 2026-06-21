using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class SiteService : ISiteService
{
    private readonly ApplicationDbContext _db;

    public SiteService(ApplicationDbContext db) => _db = db;

    public async Task<SiteSettings> GetSettingsAsync()
        => await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync() ?? new SiteSettings();

    public async Task<IReadOnlyList<MenuItem>> GetMenuAsync()
        => await _db.MenuItems
            .Include(m => m.Children.OrderBy(c => c.SortOrder))
            .AsNoTracking()
            .Where(m => m.ParentId == null)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

    public async Task<IReadOnlyList<DockItem>> GetDockItemsAsync()
        => await _db.DockItems
            .AsNoTracking()
            .Where(d => d.IsVisible)
            .OrderBy(d => d.SortOrder)
            .ToListAsync();
}
