using System.Globalization;
using GAC.Web;
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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/admin/denied";
    options.LogoutPath = "/admin/logout";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(GAC.Web.Areas.Admin.AdminPolicies.ContentEditor,
        p => p.RequireRole(GAC.Core.Identity.Roles.Admin, GAC.Core.Identity.Roles.Editor));
    options.AddPolicy(GAC.Web.Areas.Admin.AdminPolicies.LeadsAccess,
        p => p.RequireRole(GAC.Core.Identity.Roles.Admin, GAC.Core.Identity.Roles.Sales));
    options.AddPolicy(GAC.Web.Areas.Admin.AdminPolicies.AdminOnly,
        p => p.RequireRole(GAC.Core.Identity.Roles.Admin));
});

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = [new CookieRequestCultureProvider()];
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource)));

builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IAdminVehicleService, AdminVehicleService>();
builder.Services.AddScoped<IAdminNewsService, AdminNewsService>();
builder.Services.AddScoped<IAdminOfferService, AdminOfferService>();
builder.Services.AddScoped<IAdminMenuService, AdminMenuService>();
builder.Services.AddScoped<IAdminHomeService, AdminHomeService>();
builder.Services.AddScoped<IAdminPageService, AdminPageService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.Configure<GAC.Infrastructure.Services.SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IAdminLeadService, AdminLeadService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.Configure<GAC.Core.Services.MediaOptions>(opt =>
{
    builder.Configuration.GetSection("Media").Bind(opt);
    if (string.IsNullOrWhiteSpace(opt.Root))
        opt.Root = Path.Combine(builder.Environment.WebRootPath ?? "wwwroot", "uploads");
});
builder.Services.AddScoped<IMediaService, MediaService>();

var app = builder.Build();

app.UseMiddleware<LegacyHtmlRedirectMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }
});
app.UseRequestLocalization();
app.UseRouting();
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

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
