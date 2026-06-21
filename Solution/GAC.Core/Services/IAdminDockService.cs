using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminDockService
{
    Task<IReadOnlyList<DockItem>> ListAllAsync(CancellationToken ct = default);   // ordered by SortOrder
    Task<DockItem?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(DockItem item, CancellationToken ct = default);
    Task<bool> UpdateAsync(DockItem item, CancellationToken ct = default);        // Label, ShortLabel, Url, Icon, LinkType, IsVisible, SortOrder
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default);  // swap with neighbour
}
