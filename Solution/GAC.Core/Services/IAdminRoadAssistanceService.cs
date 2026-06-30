using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminRoadAssistanceService
{
    Task<RoadAssistancePage> GetAsync(CancellationToken ct = default);       // ensures + loads the singleton
    Task SaveAsync(RoadAssistancePage page, CancellationToken ct = default);  // upsert the singleton
}
