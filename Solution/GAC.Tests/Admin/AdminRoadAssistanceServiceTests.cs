using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminRoadAssistanceServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Get_OnEmptyDb_CreatesSingleton()
    {
        var db = NewDb(nameof(Get_OnEmptyDb_CreatesSingleton));
        var svc = new AdminRoadAssistanceService(db);
        var r = await svc.GetAsync();
        Assert.NotNull(r);
        Assert.Equal(1, await db.RoadAssistancePages.CountAsync());
    }

    [Fact]
    public async Task Save_Upserts_TheSingleton()
    {
        var db = NewDb(nameof(Save_Upserts_TheSingleton));
        var svc = new AdminRoadAssistanceService(db);

        await svc.SaveAsync(new RoadAssistancePage { Heading = "H1", PhoneNumber = "111" });
        await svc.SaveAsync(new RoadAssistancePage { Heading = "H2", PhoneNumber = "222" });

        Assert.Equal(1, await db.RoadAssistancePages.CountAsync());   // upsert, not insert-twice
        var r = await db.RoadAssistancePages.FirstAsync();
        Assert.Equal("H2", r.Heading.En);
        Assert.Equal("222", r.PhoneNumber);
    }
}
