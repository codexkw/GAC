using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminNewsServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Create_Then_Get_RoundTrips()
    {
        var db = NewDb(nameof(Create_Then_Get_RoundTrips));
        var svc = new AdminNewsService(db);
        var id = await svc.CreateAsync(new NewsArticle { Slug = "n1", Title = "N1", IsPublished = true });
        var a = await svc.GetAsync(id);
        Assert.NotNull(a);
        Assert.Equal("n1", a!.Slug);
    }

    [Fact]
    public async Task SlugExists_IgnoresSelf()
    {
        var db = NewDb(nameof(SlugExists_IgnoresSelf));
        var svc = new AdminNewsService(db);
        var id = await svc.CreateAsync(new NewsArticle { Slug = "dup", Title = "D" });
        Assert.True(await svc.SlugExistsAsync("dup"));
        Assert.False(await svc.SlugExistsAsync("dup", exceptId: id));
    }

    [Fact]
    public async Task Update_TogglesPublish()
    {
        var db = NewDb(nameof(Update_TogglesPublish));
        var svc = new AdminNewsService(db);
        var id = await svc.CreateAsync(new NewsArticle { Slug = "n", Title = "T", IsPublished = true });
        var a = await svc.GetAsync(id);
        a!.IsPublished = false; a.Title = "T2";
        Assert.True(await svc.UpdateAsync(a));
        var r = await db.NewsArticles.FindAsync(id);
        Assert.False(r!.IsPublished);
        Assert.Equal("T2", r.Title.En);
    }

    [Fact]
    public async Task Update_ReturnsFalse_WhenMissing()
    {
        var db = NewDb(nameof(Update_ReturnsFalse_WhenMissing));
        var svc = new AdminNewsService(db);
        Assert.False(await svc.UpdateAsync(new NewsArticle { Id = 4242, Slug = "x", Title = "X" }));
    }

    [Fact]
    public async Task Delete_Removes()
    {
        var db = NewDb(nameof(Delete_Removes));
        var svc = new AdminNewsService(db);
        var id = await svc.CreateAsync(new NewsArticle { Slug = "d", Title = "D" });
        Assert.True(await svc.DeleteAsync(id));
        Assert.Null(await db.NewsArticles.FindAsync(id));
    }
}
