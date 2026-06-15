using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminMenuService : IAdminMenuService
{
    private readonly ApplicationDbContext _db;
    public AdminMenuService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MenuItem>> ListAllAsync(CancellationToken ct = default)
        => await _db.MenuItems.Include(m => m.Parent)
            .OrderBy(m => m.ParentId).ThenBy(m => m.SortOrder).ToListAsync(ct);

    public async Task<MenuItem?> GetAsync(int id, CancellationToken ct = default)
        => await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<int> CreateAsync(MenuItem item, CancellationToken ct = default)
    {
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task<bool> UpdateAsync(MenuItem item, CancellationToken ct = default)
    {
        var e = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == item.Id, ct);
        if (e is null) return false;
        e.Label = item.Label; e.Url = item.Url; e.ParentId = item.ParentId; e.SortOrder = item.SortOrder;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.MenuItems.FindAsync([id], ct);
        if (item is null) return false;
        var children = await _db.MenuItems.Where(m => m.ParentId == id).ToListAsync(ct);
        _db.MenuItems.RemoveRange(children);
        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default)
    {
        var item = await _db.MenuItems.FindAsync([id], ct);
        if (item is null) return false;
        var siblings = await _db.MenuItems
            .Where(m => m.ParentId == item.ParentId).OrderBy(m => m.SortOrder).ToListAsync(ct);
        var idx = siblings.FindIndex(m => m.Id == id);
        var swap = idx + direction;
        if (swap < 0 || swap >= siblings.Count) return false;
        (siblings[idx].SortOrder, siblings[swap].SortOrder) = (siblings[swap].SortOrder, siblings[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
