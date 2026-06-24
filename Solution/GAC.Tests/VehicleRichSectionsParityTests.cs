using System.Linq;
using GAC.Infrastructure.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using GAC.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

// Seeds an InMemory DB and backfills ALL cars ONCE (shared via the class fixture),
// then asserts each visible car's structural backbone collections are non-empty.
public class ParityFixture
{
    public ServiceProvider Sp { get; }

    public ParityFixture()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase("parity-allcars"));
        Sp = services.BuildServiceProvider();

        ContentSeeder.SeedAsync(Sp).GetAwaiter().GetResult();
        using var scope = Sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        VehicleContentMigrator.BackfillAllAsync(db).GetAwaiter().GetResult();
    }
}

// AC1 "no car opens blank" guard (hermetic). Every visible car must backfill from its
// seeded body into a non-empty structured backbone, and the gate must read true.
public class VehicleRichSectionsParityTests : IClassFixture<ParityFixture>
{
    private readonly ParityFixture _fx;
    public VehicleRichSectionsParityTests(ParityFixture fx) => _fx = fx;

    public static IEnumerable<object[]> VisibleCars() => new[]
    {
        new object[] { "gs4" }, new object[] { "m8" }, new object[] { "gs8" },
        new object[] { "gs8traveller" }, new object[] { "hyptec-ht" }, new object[] { "emkoo" },
        new object[] { "gs3emzoom" }, new object[] { "empow" }, new object[] { "empow-sport" },
        new object[] { "gn6" },
    };

    [Theory]
    [MemberData(nameof(VisibleCars))]
    public async Task EveryCar_BackfillsToNonEmptyStructuredContent(string slug)
    {
        using var scope = _fx.Sp.CreateScope();
        var svc = new VehicleService(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        var v = await svc.GetBySlugAsync(slug);

        Assert.NotNull(v);
        Assert.True(VehicleContent.HasStructuredContent(v!), $"{slug}: structured gate should be true");
        Assert.NotEmpty(v!.Headings);
        Assert.NotEmpty(v.Stats);
        Assert.NotEmpty(v.Sliders);
        Assert.NotEmpty(v.GalleryTabs);
        Assert.True(v.GalleryTabs.Sum(t => t.Images.Count) > 0, $"{slug}: gallery has no images");
        Assert.NotEmpty(v.Features);
        Assert.NotEmpty(v.Cards);
        Assert.NotEmpty(v.SafetyToggles);
    }
}
