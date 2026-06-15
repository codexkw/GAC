using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminSettingsServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Get_CreatesSingleton_WhenMissing()
    {
        var db = NewDb(nameof(Get_CreatesSingleton_WhenMissing));
        var svc = new AdminSettingsService(db);
        var s = await svc.GetAsync();
        Assert.NotNull(s);
        Assert.Equal(1, await db.SiteSettings.CountAsync());
    }

    [Fact]
    public async Task Update_PersistsFields_AndKeepsSingleton()
    {
        var db = NewDb(nameof(Update_PersistsFields_AndKeepsSingleton));
        var svc = new AdminSettingsService(db);
        await svc.UpdateAsync(new SiteSettings { Phone = "999", FooterTagline = "Tag" });
        var s = await svc.GetAsync();
        Assert.Equal("999", s.Phone);
        Assert.Equal("Tag", s.FooterTagline.En);
        Assert.Equal(1, await db.SiteSettings.CountAsync());
    }
}
