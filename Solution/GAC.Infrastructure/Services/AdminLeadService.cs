using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminLeadService : IAdminLeadService
{
    private readonly ApplicationDbContext _db;
    public AdminLeadService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Lead>> ListAsync(LeadFilter f, CancellationToken ct = default)
    {
        var q = _db.Leads.Include(l => l.Vehicle).AsQueryable();
        if (f.FormType is { } ft) q = q.Where(l => l.FormType == ft);
        if (f.Status is { } st) q = q.Where(l => l.Status == st);
        if (f.From is { } from) q = q.Where(l => l.CreatedAt >= new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (f.To is { } to) q = q.Where(l => l.CreatedAt < new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        return await q.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
    }

    public async Task<Lead?> GetAsync(int id, CancellationToken ct = default)
        => await _db.Leads.Include(l => l.Vehicle).FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<bool> SetStatusAsync(int id, LeadStatus status, CancellationToken ct = default)
    {
        var lead = await _db.Leads.FindAsync([id], ct);
        if (lead is null) return false;
        lead.Status = status;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var lead = await _db.Leads.FindAsync([id], ct);
        if (lead is null) return false;
        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
