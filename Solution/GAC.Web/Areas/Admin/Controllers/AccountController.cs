using GAC.Core.Identity;
using GAC.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AutoValidateAntiforgeryToken]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;

    public AccountController(SignInManager<ApplicationUser> signIn) => _signIn = signIn;

    [HttpGet("/admin/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/Admin");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("/admin/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signIn.PasswordSignInAsync(
            model.Email, model.Password, isPersistent: true, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        return Redirect(string.IsNullOrEmpty(model.ReturnUrl) ? "/Admin" : model.ReturnUrl);
    }

    [HttpPost("/admin/logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Redirect("/admin/login");
    }

    [HttpGet("/admin/denied")]
    [AllowAnonymous]
    public IActionResult Denied() => View();
}
