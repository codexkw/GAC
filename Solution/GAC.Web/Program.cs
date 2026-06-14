using System.Globalization;
using GAC.Core.Identity;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = [new CookieRequestCultureProvider()];
});

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IContentService, ContentService>();

var app = builder.Build();

app.UseMiddleware<LegacyHtmlRedirectMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed roles + default admin, then EN content at startup (idempotent). Does NOT run migrations.
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
    await ContentSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

public partial class Program { } // exposes Program to the test host
