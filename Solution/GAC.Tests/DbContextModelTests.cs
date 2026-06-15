using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class DbContextModelTests
{
    [Fact]
    public void Model_Builds_WithAllContentEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=.;Database=_design;TrustServerCertificate=True")
            .Options;
        using var ctx = new ApplicationDbContext(options);

        var entityCount = ctx.Model.GetEntityTypes().Count();
        Assert.True(entityCount > 20);
        Assert.NotNull(ctx.Model.FindEntityType(typeof(GAC.Core.Content.Vehicle)));
    }

    [Fact]
    public void BodyHtml_IsMapped_OnVehicleContentPageAndFormPage()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=.;Database=_design;TrustServerCertificate=True")
            .Options;
        using var ctx = new ApplicationDbContext(options);

        foreach (var clr in new[] { typeof(GAC.Core.Content.Vehicle), typeof(GAC.Core.Content.ContentPage), typeof(GAC.Core.Content.FormPage) })
        {
            var et = ctx.Model.FindEntityType(clr)!;
            var nav = et.FindNavigation("BodyHtml")!;
            Assert.NotNull(nav);
            Assert.NotNull(nav.TargetEntityType.FindProperty("En"));
            Assert.NotNull(nav.TargetEntityType.FindProperty("Ar"));
        }
    }
}
