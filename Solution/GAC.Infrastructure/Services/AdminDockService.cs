using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminDockService : IAdminDockService
{
    private readonly ApplicationDbContext _db;
    public AdminDockService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<DockItem>> ListAllAsync(CancellationToken ct = default)
        => await _db.DockItems.OrderBy(d => d.SortOrder).ToListAsync(ct);

    public async Task<DockItem?> GetAsync(int id, CancellationToken ct = default)
        => await _db.DockItems.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<int> CreateAsync(DockItem item, CancellationToken ct = default)
    {
        _db.DockItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task<bool> UpdateAsync(DockItem item, CancellationToken ct = default)
    {
        var e = await _db.DockItems.FirstOrDefaultAsync(d => d.Id == item.Id, ct);
        if (e is null) return false;
        e.Label = item.Label;
        e.ShortLabel = item.ShortLabel;
        e.Url = item.Url;
        e.Icon = item.Icon;
        e.LinkType = item.LinkType;
        e.IsVisible = item.IsVisible;
        e.SortOrder = item.SortOrder;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.DockItems.FindAsync([id], ct);
        if (item is null) return false;
        _db.DockItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default)
    {
        var list = await _db.DockItems.OrderBy(d => d.SortOrder).ToListAsync(ct);
        var idx = list.FindIndex(d => d.Id == id);
        if (idx < 0) return false;
        var swap = idx + direction;
        if (swap < 0 || swap >= list.Count) return false;
        (list[idx].SortOrder, list[swap].SortOrder) = (list[swap].SortOrder, list[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
