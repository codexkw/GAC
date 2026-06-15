using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminOfferService
{
    Task<IReadOnlyList<Offer>> ListAsync(CancellationToken ct = default);   // incl. inactive
    Task<Offer?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default);
    Task<int> CreateAsync(Offer a, CancellationToken ct = default);
    Task<bool> UpdateAsync(Offer a, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
