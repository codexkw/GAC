using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminSettingsService : IAdminSettingsService
{
    private readonly ApplicationDbContext _db;
    public AdminSettingsService(ApplicationDbContext db) => _db = db;

    public async Task<SiteSettings> GetAsync(CancellationToken ct = default)
    {
        var s = await _db.SiteSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new SiteSettings();
            _db.SiteSettings.Add(s);
            await _db.SaveChangesAsync(ct);
        }
        return s;
    }

    public async Task UpdateAsync(SiteSettings settings, CancellationToken ct = default)
    {
        var e = await GetAsync(ct);
        e.Phone = settings.Phone; e.WhatsApp = settings.WhatsApp; e.Email = settings.Email;
        e.InstagramUrl = settings.InstagramUrl; e.FacebookUrl = settings.FacebookUrl;
        e.TiktokUrl = settings.TiktokUrl; e.SnapchatUrl = settings.SnapchatUrl; e.XUrl = settings.XUrl;
        e.FooterTagline = settings.FooterTagline;
        await _db.SaveChangesAsync(ct);
    }
}
