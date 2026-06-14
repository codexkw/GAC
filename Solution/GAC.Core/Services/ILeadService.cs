using GAC.Core.Content;

namespace GAC.Core.Services;

public interface ILeadService
{
    Task CreateAsync(Lead lead, CancellationToken ct = default);
}
