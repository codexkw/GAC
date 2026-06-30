using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminOfferService : IAdminOfferService
{
    private readonly ApplicationDbContext _db;
    public AdminOfferService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Offer>> ListAsync(CancellationToken ct = default)
        => await _db.Offers.OrderBy(a => a.SortOrder).ToListAsync(ct);

    public async Task<Offer?> GetAsync(int id, CancellationToken ct = default)
        => await _db.Offers.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default)
        => await _db.Offers.AnyAsync(a => a.Slug == slug && (exceptId == null || a.Id != exceptId), ct);

    public async Task<int> CreateAsync(Offer a, CancellationToken ct = default)
    {
        _db.Offers.Add(a);
        await _db.SaveChangesAsync(ct);
        return a.Id;
    }

    public async Task<bool> UpdateAsync(Offer a, CancellationToken ct = default)
    {
        var e = await _db.Offers.FirstOrDefaultAsync(x => x.Id == a.Id, ct);
        if (e is null) return false;
        e.Slug = a.Slug; e.IsActive = a.IsActive;
        e.Title = a.Title; e.Body = a.Body; e.ButtonLabel = a.ButtonLabel; e.ImagePath = a.ImagePath; e.ValidUntil = a.ValidUntil; e.SortOrder = a.SortOrder;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.Offers.FindAsync([id], ct);
        if (a is null) return false;
        _db.Offers.Remove(a);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
