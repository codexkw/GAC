using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class MediaController : Controller
{
    private readonly IMediaService _media;
    public MediaController(IMediaService media) => _media = media;

    public async Task<IActionResult> Index() => View(await _media.ListAsync());

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile? file, bool json = false)
    {
        if (file is null || file.Length == 0)
            return json ? BadRequest(new { error = "No file." }) : RedirectToAction(nameof(Index));

        await using var stream = file.OpenReadStream();
        var res = await _media.UploadAsync(stream, file.FileName, file.ContentType, file.Length);
        if (json) return res.Ok ? Ok(new { path = res.Path }) : BadRequest(new { error = res.Error });

        TempData["Flash"] = res.Ok ? "Uploaded." : res.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _media.DeleteAsync(id);
        TempData["Flash"] = "Deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> List() => Json((await _media.ListAsync()).Select(m => new { m.Id, m.Path }));
}
