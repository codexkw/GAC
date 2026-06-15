using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewData["NewLeads"] = await _db.Leads.CountAsync(l => l.Status == GAC.Core.Content.LeadStatus.New);
        ViewData["Vehicles"] = await _db.Vehicles.CountAsync();
        ViewData["News"] = await _db.NewsArticles.CountAsync();
        return View();
    }
}
