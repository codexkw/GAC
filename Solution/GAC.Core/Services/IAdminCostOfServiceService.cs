using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminCostOfServiceService
{
    Task<CostOfServicePage> GetAsync(CancellationToken ct = default);       // ensures + loads the singleton (incl. ordered Rows/Models/Cells)
    Task SaveAsync(CostOfServicePage page, CancellationToken ct = default);  // upsert; replace the matrix wholesale (normalize: drop blanks, re-index, align cells)
}
