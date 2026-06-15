using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class AdminVehicleSectionsTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);
    private static AdminVehicleService NewSvc(ApplicationDbContext db) => new(db, new HtmlSanitizerService());

    [Fact]
    public async Task AddFeature_SanitizesBody_AndSetsOrder()
    {
        var db = NewDb(nameof(AddFeature_SanitizesBody_AndSetsOrder));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "f", Name = "F" });

        var id1 = await svc.AddFeatureAsync(vid, new FeatureSection
        {
            Heading = "H1",
            Body = new LocalizedText { En = "<p>ok</p><script>bad()</script>" },
            Layout = FeatureLayout.ImageRight
        });
        var id2 = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "H2" });

        var f1 = await svc.GetFeatureAsync(id1);
        Assert.DoesNotContain("<script", f1!.Body.En);
        Assert.Equal(FeatureLayout.ImageRight, f1.Layout);
        Assert.Equal(0, f1.SortOrder);
        Assert.Equal(1, (await svc.GetFeatureAsync(id2))!.SortOrder);
    }

    [Fact]
    public async Task UpdateFeature_ChangesFields_AndSanitizes()
    {
        var db = NewDb(nameof(UpdateFeature_ChangesFields_AndSanitizes));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "f2", Name = "F" });
        var id = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "old" });

        var ok = await svc.UpdateFeatureAsync(new FeatureSection
        {
            Id = id, Heading = "new",
            Body = new LocalizedText { En = "<b>x</b><img src=x onerror=y>" },
            Layout = FeatureLayout.Banner
        });

        Assert.True(ok);
        var f = await svc.GetFeatureAsync(id);
        Assert.Equal("new", f!.Heading.En);
        Assert.Equal(FeatureLayout.Banner, f.Layout);
        Assert.DoesNotContain("onerror", f.Body.En);
        Assert.DoesNotContain("<img", f.Body.En);
    }

    [Fact]
    public async Task RemoveFeature_Deletes()
    {
        var db = NewDb(nameof(RemoveFeature_Deletes));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "f3", Name = "F" });
        var id = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "x" });
        Assert.True(await svc.RemoveFeatureAsync(id));
        Assert.Null(await svc.GetFeatureAsync(id));
    }

    [Fact]
    public async Task MoveFeature_SwapsOrder()
    {
        var db = NewDb(nameof(MoveFeature_SwapsOrder));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "f4", Name = "F" });
        var a = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "A" });
        var b = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "B" });
        Assert.True(await svc.MoveFeatureAsync(b, -1));
        Assert.Equal(0, (await svc.GetFeatureAsync(b))!.SortOrder);
        Assert.Equal(1, (await svc.GetFeatureAsync(a))!.SortOrder);
    }

    [Fact]
    public async Task SpecGroup_And_Row_AddRemove()
    {
        var db = NewDb(nameof(SpecGroup_And_Row_AddRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "s", Name = "S" });
        var gid = await svc.AddSpecGroupAsync(vid, new LocalizedText { En = "Engine" });
        var rid = await svc.AddSpecRowAsync(gid, new LocalizedText { En = "Power" }, new LocalizedText { En = "200hp" });
        Assert.Equal(1, await db.Set<SpecGroup>().CountAsync());
        Assert.Equal(1, await db.Set<SpecRow>().CountAsync());
        Assert.True(await svc.RemoveSpecRowAsync(rid));
        Assert.True(await svc.RemoveSpecGroupAsync(gid));
        Assert.Equal(0, await db.Set<SpecRow>().CountAsync());
    }

    [Fact]
    public async Task Color_AddMoveRemove()
    {
        var db = NewDb(nameof(Color_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "c", Name = "C" });
        var a = await svc.AddColorAsync(vid, new LocalizedText { En = "Red" }, "#ff0000", null);
        var b = await svc.AddColorAsync(vid, new LocalizedText { En = "Blue" }, "#0000ff", null);
        Assert.True(await svc.MoveColorAsync(b, -1));
        Assert.Equal(0, (await db.Set<ColorOption>().FindAsync(b))!.SortOrder);
        Assert.True(await svc.RemoveColorAsync(a));
        Assert.Equal(1, await db.Set<ColorOption>().CountAsync());
    }

    [Fact]
    public async Task Trim_AddRemove()
    {
        var db = NewDb(nameof(Trim_AddRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "t", Name = "T" });
        var id = await svc.AddTrimAsync(vid, new Trim { Name = "GT", Price = 100000m });
        Assert.Equal(1, await db.Set<Trim>().CountAsync());
        Assert.True(await svc.RemoveTrimAsync(id));
        Assert.Equal(0, await db.Set<Trim>().CountAsync());
    }
}
