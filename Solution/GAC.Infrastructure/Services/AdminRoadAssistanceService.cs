using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminRoadAssistanceService : IAdminRoadAssistanceService
{
    private readonly ApplicationDbContext _db;
    public AdminRoadAssistanceService(ApplicationDbContext db) => _db = db;

    private async Task<RoadAssistancePage> EnsureAsync(CancellationToken ct)
    {
        var r = await _db.RoadAssistancePages.FirstOrDefaultAsync(ct);
        if (r is null)
        {
            r = new RoadAssistancePage();
            _db.RoadAssistancePages.Add(r);
            await _db.SaveChangesAsync(ct);
        }
        return r;
    }

    public async Task<RoadAssistancePage> GetAsync(CancellationToken ct = default)
        => await EnsureAsync(ct);

    public async Task SaveAsync(RoadAssistancePage page, CancellationToken ct = default)
    {
        var existing = await _db.RoadAssistancePages.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            page.Id = 0;
            _db.RoadAssistancePages.Add(page);
        }
        else
        {
            existing.Heading = page.Heading;
            existing.Intro = page.Intro;
            existing.ContactLead = page.ContactLead;
            existing.ContactText = page.ContactText;
            existing.PhoneNumber = page.PhoneNumber;
            existing.CallButtonLabel = page.CallButtonLabel;
        }
        await _db.SaveChangesAsync(ct);
    }
}
