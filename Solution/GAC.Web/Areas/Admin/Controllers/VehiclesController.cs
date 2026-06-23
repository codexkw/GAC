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

    public async Task<IActionResult> FeatureEdit(int vehicleId, int? id)
    {
        var feature = id is null
            ? new FeatureSection { VehicleId = vehicleId }
            : await _svc.GetFeatureAsync(id.Value);
        if (feature is null) return NotFound();
        feature.VehicleId = vehicleId;
        return View(feature);
    }

    [HttpPost]
    public async Task<IActionResult> FeatureSave(int vehicleId, FeatureSection feature)
    {
        feature.VehicleId = vehicleId;
        if (feature.Id == 0) await _svc.AddFeatureAsync(vehicleId, feature);
        else await _svc.UpdateFeatureAsync(feature);
        TempData["Flash"] = "Feature saved.";
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }

    [HttpPost] public async Task<IActionResult> RemoveFeature(int featureId, int vehicleId)
    { await _svc.RemoveFeatureAsync(featureId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> MoveFeature(int featureId, int vehicleId, int direction)
    { await _svc.MoveFeatureAsync(featureId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> AddSpecGroup(int vehicleId, string? titleEn, string? titleAr)
    { await _svc.AddSpecGroupAsync(vehicleId, new() { En = titleEn, Ar = titleAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveSpecGroup(int groupId, int vehicleId)
    { await _svc.RemoveSpecGroupAsync(groupId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveSpecGroup(int groupId, int vehicleId, int direction)
    { await _svc.MoveSpecGroupAsync(groupId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> AddSpecRow(int groupId, int vehicleId, string? labelEn, string? labelAr, string? valueEn, string? valueAr)
    { await _svc.AddSpecRowAsync(groupId, new() { En = labelEn, Ar = labelAr }, new() { En = valueEn, Ar = valueAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveSpecRow(int rowId, int vehicleId)
    { await _svc.RemoveSpecRowAsync(rowId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> AddColor(int vehicleId, string? nameEn, string? nameAr, string hex, string? imagePath)
    { await _svc.AddColorAsync(vehicleId, new() { En = nameEn, Ar = nameAr }, hex, imagePath); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveColor(int colorId, int vehicleId)
    { await _svc.RemoveColorAsync(colorId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveColor(int colorId, int vehicleId, int direction)
    { await _svc.MoveColorAsync(colorId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> AddTrim(int vehicleId, string? nameEn, string? nameAr, decimal? price, string? highlightsEn, string? highlightsAr, string? specPdf)
    {
        await _svc.AddTrimAsync(vehicleId, new Trim
        {
            Name = new() { En = nameEn, Ar = nameAr },
            Price = price,
            Highlights = new() { En = highlightsEn, Ar = highlightsAr },
            SpecPdf = specPdf
        });
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }
    [HttpPost] public async Task<IActionResult> RemoveTrim(int trimId, int vehicleId)
    { await _svc.RemoveTrimAsync(trimId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveTrim(int trimId, int vehicleId, int direction)
    { await _svc.MoveTrimAsync(trimId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> UpsertSectionHeading(int vehicleId, SectionKey key, string? titleEn, string? titleAr, string? subEn, string? subAr, string? bodyEn, string? bodyAr)
    { await _svc.UpsertSectionHeadingAsync(vehicleId, key, new() { En = titleEn, Ar = titleAr }, new() { En = subEn, Ar = subAr }, new() { En = bodyEn, Ar = bodyAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> AddStat(int vehicleId, string? labelEn, string? labelAr, string? valueEn, string? valueAr)
    { await _svc.AddStatAsync(vehicleId, new() { En = labelEn, Ar = labelAr }, new() { En = valueEn, Ar = valueAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveStat(int statId, int vehicleId)
    { await _svc.RemoveStatAsync(statId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveStat(int statId, int vehicleId, int direction)
    { await _svc.MoveStatAsync(statId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> AddSlider(int vehicleId, string? eyebrowEn, string? eyebrowAr, string? titleEn, string? titleAr)
    { await _svc.AddSliderAsync(vehicleId, new() { En = eyebrowEn, Ar = eyebrowAr }, new() { En = titleEn, Ar = titleAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveSlider(int sliderId, int vehicleId)
    { await _svc.RemoveSliderAsync(sliderId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveSlider(int sliderId, int vehicleId, int direction)
    { await _svc.MoveSliderAsync(sliderId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> AddSliderSlide(int sliderGroupId, int vehicleId, string? imagePath, string? altEn, string? altAr)
    { await _svc.AddSliderSlideAsync(sliderGroupId, imagePath, new() { En = altEn, Ar = altAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveSliderSlide(int slideId, int vehicleId)
    { await _svc.RemoveSliderSlideAsync(slideId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveSliderSlide(int slideId, int vehicleId, int direction)
    { await _svc.MoveSliderSlideAsync(slideId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> AddFeatureBullet(int featureSectionId, int vehicleId, string? labelEn, string? labelAr, string? textEn, string? textAr)
    { await _svc.AddFeatureBulletAsync(featureSectionId, new() { En = labelEn, Ar = labelAr }, new() { En = textEn, Ar = textAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveFeatureBullet(int bulletId, int vehicleId)
    { await _svc.RemoveFeatureBulletAsync(bulletId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveFeatureBullet(int bulletId, int vehicleId, int direction)
    { await _svc.MoveFeatureBulletAsync(bulletId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
}
