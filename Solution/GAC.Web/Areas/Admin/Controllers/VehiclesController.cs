using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class VehiclesController : Controller
{
    private readonly IAdminVehicleService _svc;
    public VehiclesController(IAdminVehicleService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.ListAsync());

    public IActionResult Create() => View("Edit", new Vehicle { IsVisible = true });

    public async Task<IActionResult> Edit(int id)
    {
        var v = await _svc.GetAsync(id);
        return v is null ? NotFound() : View(v);
    }

    [HttpPost]
    public async Task<IActionResult> Save(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.Slug))
            ModelState.AddModelError(nameof(vehicle.Slug), "Slug is required.");
        else if (await _svc.SlugExistsAsync(vehicle.Slug, vehicle.Id == 0 ? null : vehicle.Id))
            ModelState.AddModelError(nameof(vehicle.Slug), "That slug is already in use.");
        if (!ModelState.IsValid) return View("Edit", vehicle);

        if (vehicle.Id == 0)
        {
            var newId = await _svc.CreateAsync(vehicle);
            TempData["Flash"] = "Vehicle created.";
            return RedirectToAction(nameof(Edit), new { id = newId });
        }
        await _svc.UpdateAsync(vehicle);
        TempData["Flash"] = "Vehicle saved.";
        return RedirectToAction(nameof(Edit), new { id = vehicle.Id });
    }

    [HttpPost] public async Task<IActionResult> Delete(int id)
    { await _svc.DeleteAsync(id); TempData["Flash"] = "Vehicle deleted."; return RedirectToAction(nameof(Index)); }

    [HttpPost] public async Task<IActionResult> Move(int id, int direction)
    { await _svc.MoveAsync(id, direction); return RedirectToAction(nameof(Index)); }

    [HttpPost] public async Task<IActionResult> AddImage(int vehicleId, string path, VehicleImageKind kind)
    { if (!string.IsNullOrWhiteSpace(path)) await _svc.AddImageAsync(vehicleId, path, kind); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> RemoveImage(int imageId, int vehicleId)
    { await _svc.RemoveImageAsync(imageId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> MoveImage(int imageId, int vehicleId, int direction)
    { await _svc.MoveImageAsync(imageId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
}
