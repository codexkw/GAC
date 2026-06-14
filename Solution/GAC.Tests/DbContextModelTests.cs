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
}
