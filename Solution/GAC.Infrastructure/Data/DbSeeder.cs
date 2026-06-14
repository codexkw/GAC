using GAC.Core.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GAC.Infrastructure.Data;

public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@gacsaudi.local";
    public const string DefaultAdminPassword = "ChangeMe!2026";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(DefaultAdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrator"
            };
            var result = await userManager.CreateAsync(admin, DefaultAdminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"DbSeeder: failed to create default admin — {string.Join("; ", result.Errors.Select(e => e.Description))}");

            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}
