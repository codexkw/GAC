using System.Linq;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GAC.Tests;

// Swaps the app's SQL Server ApplicationDbContext for an isolated in-memory one,
// so an integration test that boots the app (WebApplicationFactory) can assert
// rendering against data it controls, without touching the real database.
internal static class InMemoryTestDb
{
    public static void Swap(IServiceCollection services, string dbName)
    {
        var toRemove = services.Where(d =>
            d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
            d.ServiceType == typeof(DbContextOptions) ||
            d.ServiceType == typeof(ApplicationDbContext) ||
            (d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("IDbContextOptionsConfiguration"))).ToList();
        foreach (var d in toRemove) services.Remove(d);

        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
    }
}
