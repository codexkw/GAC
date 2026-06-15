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
        db.ContentPages.Add(new ContentPage
        {
            Slug = "about",
            Title = "About",
            IsVisible = true,
            Sections = { new ContentSection { Heading = "S", Body = "B", SortOrder = 0 } }
        });
        await db.SaveChangesAsync();
        var id = (await db.ContentPages.FirstAsync()).Id;
        var svc = new AdminPageService(db);

        // Simulate the detached model-bound entity an MVC POST produces.
        var posted = new ContentPage
        {
            Id = id,
            Slug = "hacked-slug",        // must be ignored by the service
            Title = "About Us",
            MetaTitle = "Meta",
            BodyHtml = new LocalizedText { En = "<p>content body</p>" },
            IsVisible = false
        };
        Assert.True(await svc.UpdateContentAsync(posted));

        using var verify = NewDb(nameof(UpdateContent_ChangesTitleAndMeta));
        var r = await verify.ContentPages.Include(p => p.Sections).FirstAsync();
        Assert.Equal("About Us", r.Title.En);
        Assert.Equal("Meta", r.MetaTitle.En);
        Assert.Equal("<p>content body</p>", r.BodyHtml.En);
        Assert.False(r.IsVisible);
        Assert.Equal("about", r.Slug);       // slug preserved
        Assert.Single(r.Sections);           // sections preserved
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
        var id = (await db.FormPages.FirstAsync()).Id;
        var svc = new AdminPageService(db);

        // Simulate the detached model-bound entity an MVC POST produces.
        var posted = new FormPage
        {
            Id = id,
            Slug = "hacked",                 // must be ignored
            FormType = FormType.Quote,       // must be ignored
            Title = "Fleet Sales",
            IntroText = "Intro",
            BodyHtml = new LocalizedText { En = "<p>form body</p>" },
            IsVisible = false
        };
        Assert.True(await svc.UpdateFormAsync(posted));

        using var verify = NewDb(nameof(UpdateForm_ChangesTitleIntroMeta));
        var r = await verify.FormPages.FirstAsync();
        Assert.Equal("Fleet Sales", r.Title.En);
        Assert.Equal("Intro", r.IntroText.En);
        Assert.Equal("<p>form body</p>", r.BodyHtml.En);
        Assert.False(r.IsVisible);
        Assert.Equal("fleet", r.Slug);            // slug preserved
        Assert.Equal(FormType.Fleet, r.FormType); // formtype preserved
    }

    [Fact]
    public async Task UpdateForm_ReturnsFalse_WhenMissing()
    {
        var db = NewDb(nameof(UpdateForm_ReturnsFalse_WhenMissing));
        var svc = new AdminPageService(db);
        Assert.False(await svc.UpdateFormAsync(new FormPage { Id = 999, Slug = "x", Title = "X" }));
    }
}
