using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminPageServiceFormBannerTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task UpdateForm_Persists_BannerImagePath()
    {
        var db = NewDb(nameof(UpdateForm_Persists_BannerImagePath));
        db.FormPages.Add(new FormPage { Id = 1, Slug = "fleet", FormType = FormType.Fleet });
        await db.SaveChangesAsync();
        var svc = new AdminPageService(db);

        await svc.UpdateFormAsync(new FormPage { Id = 1, Slug = "fleet", FormType = FormType.Fleet,
            BannerImagePath = "/new-banner.jpg", Title = "Fleet" });

        var f = await db.FormPages.FindAsync(1);
        Assert.Equal("/new-banner.jpg", f!.BannerImagePath);
    }
}
