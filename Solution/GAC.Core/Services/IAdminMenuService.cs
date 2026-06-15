using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminMenuService
{
    Task<IReadOnlyList<MenuItem>> ListAllAsync(CancellationToken ct = default);  // flat, ordered, +Parent
    Task<MenuItem?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(MenuItem item, CancellationToken ct = default);
    Task<bool> UpdateAsync(MenuItem item, CancellationToken ct = default);       // Label, Url, ParentId, SortOrder
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);             // also deletes children
    Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default); // swap within same ParentId scope
}
