using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminPageServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task UpdateContent_ChangesTitleAndMeta()
    {
        var db = NewDb(nameof(UpdateContent_ChangesTitleAndMeta));
        db.ContentPages.Add(new ContentPage { Slug = "about", Title = "About", IsVisible = true });
        await db.SaveChangesAsync();
        var svc = new AdminPageService(db);
        var page = await svc.GetContentAsync((await db.ContentPages.FirstAsync()).Id);
        page!.Title = "About Us"; page.MetaTitle = "Meta"; page.IsVisible = false;
        Assert.True(await svc.UpdateContentAsync(page));
        var r = await db.ContentPages.FirstAsync();
        Assert.Equal("About Us", r.Title.En);
        Assert.Equal("Meta", r.MetaTitle.En);
        Assert.False(r.IsVisible);
    }

    [Fact]
    public async Task UpdateContent_ReturnsFalse_WhenMissing()
    {
        var db = NewDb(nameof(UpdateContent_ReturnsFalse_WhenMissing));
        var svc = new AdminPageService(db);
        Assert.False(await svc.UpdateContentAsync(new ContentPage { Id = 999, Slug = "x", Title = "X" }));
    }

    [Fact]
    public async Task UpdateForm_ChangesTitleIntroMeta()
    {
        var db = NewDb(nameof(UpdateForm_ChangesTitleIntroMeta));
        db.FormPages.Add(new FormPage { Slug = "fleet", FormType = FormType.Fleet, Title = "Fleet", IsVisible = true });
        await db.SaveChangesAsync();
        var svc = new AdminPageService(db);
        var page = await svc.GetFormAsync((await db.FormPages.FirstAsync()).Id);
        page!.Title = "Fleet Sales"; page.IntroText = "Intro"; page.IsVisible = false;
        Assert.True(await svc.UpdateFormAsync(page));
        var r = await db.FormPages.FirstAsync();
        Assert.Equal("Fleet Sales", r.Title.En);
        Assert.Equal("Intro", r.IntroText.En);
        Assert.False(r.IsVisible);
    }

    [Fact]
    public async Task UpdateForm_ReturnsFalse_WhenMissing()
    {
        var db = NewDb(nameof(UpdateForm_ReturnsFalse_WhenMissing));
        var svc = new AdminPageService(db);
        Assert.False(await svc.UpdateFormAsync(new FormPage { Id = 999, Slug = "x", Title = "X" }));
    }
}
