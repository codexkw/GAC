using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminVehicleService
{
    Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken ct = default);          // incl. hidden, ordered
    Task<Vehicle?> GetAsync(int id, CancellationToken ct = default);                 // +Images
    Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default);
    Task<int> CreateAsync(Vehicle vehicle, CancellationToken ct = default);          // returns new Id
    Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken ct = default);         // scalar + localized fields
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default);     // -1 up, +1 down (swaps SortOrder)
    Task<int> AddImageAsync(int vehicleId, string path, VehicleImageKind kind, CancellationToken ct = default);
    Task<bool> RemoveImageAsync(int imageId, CancellationToken ct = default);
    Task<bool> MoveImageAsync(int imageId, int direction, CancellationToken ct = default);
}
