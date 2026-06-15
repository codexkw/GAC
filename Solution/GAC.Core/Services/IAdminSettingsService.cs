using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminSettingsService
{
    Task<SiteSettings> GetAsync(CancellationToken ct = default);          // creates the singleton if missing
    Task UpdateAsync(SiteSettings settings, CancellationToken ct = default);
}
