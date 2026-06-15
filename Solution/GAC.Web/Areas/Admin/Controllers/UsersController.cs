using GAC.Core.Identity;
using GAC.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.AdminOnly)]
[AutoValidateAntiforgeryToken]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    public UsersController(UserManager<ApplicationUser> users) => _users = users;

    public async Task<IActionResult> Index()
    {
        var list = await _users.Users.ToListAsync();
        var rows = new List<UserRow>();
        foreach (var u in list)
        {
            var roles = await _users.GetRolesAsync(u);
            rows.Add(new UserRow
            {
                Id = u.Id, Email = u.Email ?? "", DisplayName = u.DisplayName,
                Role = roles.FirstOrDefault() ?? "",
                Disabled = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
            });
        }
        return View(rows);
    }

    public IActionResult Create()
    {
        ViewBag.Roles = RoleSelect("Editor");
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel m)
    {
        if (!ModelState.IsValid) { ViewBag.Roles = RoleSelect(m.Role); return View(m); }
        var user = new ApplicationUser { UserName = m.Email, Email = m.Email, EmailConfirmed = true, DisplayName = m.DisplayName };
        var res = await _users.CreateAsync(user, m.Password);
        if (!res.Succeeded)
        {
            foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
            ViewBag.Roles = RoleSelect(m.Role); return View(m);
        }
        await _users.AddToRoleAsync(user, m.Role);
        TempData["Flash"] = "User created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var u = await _users.FindByIdAsync(id);
        if (u is null) return NotFound();
        var roles = await _users.GetRolesAsync(u);
        ViewBag.Roles = RoleSelect(roles.FirstOrDefault() ?? "Editor");
        return View(new EditUserViewModel
        {
            Id = u.Id, Email = u.Email ?? "", DisplayName = u.DisplayName,
            Role = roles.FirstOrDefault() ?? "Editor",
            Disabled = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditUserViewModel m)
    {
        var u = await _users.FindByIdAsync(m.Id);
        if (u is null) return NotFound();
        if (!ModelState.IsValid) { ViewBag.Roles = RoleSelect(m.Role); return View(m); }

        u.DisplayName = m.DisplayName;
        await _users.UpdateAsync(u);

        var current = await _users.GetRolesAsync(u);
        await _users.RemoveFromRolesAsync(u, current);
        await _users.AddToRoleAsync(u, m.Role);

        await _users.SetLockoutEnabledAsync(u, true);
        await _users.SetLockoutEndDateAsync(u, m.Disabled ? DateTimeOffset.MaxValue : null);

        if (!string.IsNullOrWhiteSpace(m.NewPassword))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(u);
            var reset = await _users.ResetPasswordAsync(u, token, m.NewPassword);
            if (!reset.Succeeded)
            {
                foreach (var e in reset.Errors) ModelState.AddModelError("", e.Description);
                ViewBag.Roles = RoleSelect(m.Role); return View(m);
            }
        }
        TempData["Flash"] = "User updated.";
        return RedirectToAction(nameof(Index));
    }

    private static List<SelectListItem> RoleSelect(string selected)
        => Roles.All.Select(r => new SelectListItem(r, r, r == selected)).ToList();
}
