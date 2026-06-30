using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminWarrantyService
{
    Task<WarrantyPage> GetAsync(CancellationToken ct = default);       // ensures + loads the singleton (incl. ordered Callouts)
    Task SaveAsync(WarrantyPage page, CancellationToken ct = default);  // upsert the singleton; replace callout rows (drop blanks, re-index)
}
