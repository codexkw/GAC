using GAC.Core.Content;

namespace GAC.Core.Services;

public record LeadFilter(FormType? FormType, LeadStatus? Status, DateOnly? From, DateOnly? To);

public interface IAdminLeadService
{
    Task<IReadOnlyList<Lead>> ListAsync(LeadFilter filter, CancellationToken ct = default);
    Task<Lead?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> SetStatusAsync(int id, LeadStatus status, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
