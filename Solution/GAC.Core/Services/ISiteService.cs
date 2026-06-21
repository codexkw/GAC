using GAC.Core.Content;

namespace GAC.Core.Services;

public interface ISiteService
{
    /// <summary>The site settings row, or a default (all-null fields) instance if none exists. Never returns null.</summary>
    Task<SiteSettings> GetSettingsAsync();
    Task<IReadOnlyList<MenuItem>> GetMenuAsync();
    Task<IReadOnlyList<DockItem>> GetDockItemsAsync();
}
