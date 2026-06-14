using GAC.Core.Content;

namespace GAC.Core.Services;

public interface ISiteService
{
    Task<SiteSettings> GetSettingsAsync();
    Task<IReadOnlyList<MenuItem>> GetMenuAsync();
}
