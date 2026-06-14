using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;

namespace GAC.Infrastructure.Services;

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _db;
    public LeadService(ApplicationDbContext db) => _db = db;

    public async Task CreateAsync(Lead lead, CancellationToken ct = default)
    {
        if (lead.CreatedAt == default)
            lead.CreatedAt = DateTimeOffset.UtcNow;
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
    }
}
