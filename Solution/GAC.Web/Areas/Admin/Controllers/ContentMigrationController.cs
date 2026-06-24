using GAC.Infrastructure.Content;
using GAC.Infrastructure.Data;
using GAC.Web.Areas.Admin;            // AdminPolicies lives here (VehiclesController uses this exact using)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.AdminOnly)]
[AutoValidateAntiforgeryToken]
public class ContentMigrationController : Controller
{
    private readonly ApplicationDbContext _db;
    public ContentMigrationController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> RunAll(bool force = false, CancellationToken ct = default)
    {
        var report = await VehicleContentMigrator.BackfillAllAsync(_db, force, ct);
        TempData["MigrationReport"] =
            $"Scanned {report.VehiclesScanned}, migrated {report.VehiclesMigrated}, skipped {report.VehiclesSkipped}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RunOne(int vehicleId, bool force = false, CancellationToken ct = default)
    {
        var ok = await VehicleContentMigrator.BackfillVehicleAsync(_db, vehicleId, force, ct);
        TempData["MigrationReport"] = ok
            ? $"Vehicle #{vehicleId} migrated."
            : $"Vehicle #{vehicleId} skipped (already has content or empty body).";
        return RedirectToAction(nameof(Index));
    }
}
