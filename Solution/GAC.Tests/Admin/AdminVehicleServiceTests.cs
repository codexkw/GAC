using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminVehicleServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private static AdminVehicleService NewSvc(ApplicationDbContext db)
        => new(db, new HtmlSanitizerService());

    [Fact]
    public async Task Create_Then_Get_RoundTrips()
    {
        var db = NewDb(nameof(Create_Then_Get_RoundTrips));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "x1", Name = "X1", SortOrder = 5, IsVisible = true });
        var v = await svc.GetAsync(id);
        Assert.NotNull(v);
        Assert.Equal("x1", v!.Slug);
    }

    [Fact]
    public async Task SlugExists_DetectsDuplicate_IgnoringSelf()
    {
        var db = NewDb(nameof(SlugExists_DetectsDuplicate_IgnoringSelf));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "dup", Name = "D" });
        Assert.True(await svc.SlugExistsAsync("dup"));
        Assert.False(await svc.SlugExistsAsync("dup", exceptId: id));
    }

    [Fact]
    public async Task Move_SwapsSortOrder()
    {
        var db = NewDb(nameof(Move_SwapsSortOrder));
        var svc = NewSvc(db);
        var a = await svc.CreateAsync(new Vehicle { Slug = "a", Name = "A", SortOrder = 1 });
        var b = await svc.CreateAsync(new Vehicle { Slug = "b", Name = "B", SortOrder = 2 });
        Assert.True(await svc.MoveAsync(b, -1));
        Assert.Equal(1, (await db.Vehicles.FindAsync(b))!.SortOrder);
        Assert.Equal(2, (await db.Vehicles.FindAsync(a))!.SortOrder);
    }

    [Fact]
    public async Task AddImage_Then_Remove()
    {
        var db = NewDb(nameof(AddImage_Then_Remove));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "img", Name = "I" });
        var imgId = await svc.AddImageAsync(id, "/uploads/a.png", VehicleImageKind.Gallery);
        Assert.Equal(1, await db.VehicleImages.CountAsync());
        Assert.True(await svc.RemoveImageAsync(imgId));
        Assert.Equal(0, await db.VehicleImages.CountAsync());
    }

    [Fact]
    public async Task Delete_Removes()
    {
        var db = NewDb(nameof(Delete_Removes));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "del", Name = "D" });
        Assert.True(await svc.DeleteAsync(id));
        Assert.Null(await db.Vehicles.FindAsync(id));
    }

    [Fact]
    public async Task Update_ChangesFields_AndPreservesImages()
    {
        var db = NewDb(nameof(Update_ChangesFields_AndPreservesImages));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "u1", Name = "Old", SortOrder = 1 });
        await svc.AddImageAsync(id, "/uploads/keep.png", VehicleImageKind.Hero);

        var ok = await svc.UpdateAsync(new Vehicle
        {
            Id = id, Slug = "u1-new", Name = "New", Tagline = "T",
            Category = VehicleCategory.Suv | VehicleCategory.Ev, IsVisible = false, SortOrder = 9
        });

        Assert.True(ok);
        var v = await svc.GetAsync(id);
        Assert.Equal("u1-new", v!.Slug);
        Assert.Equal("New", v.Name.En);
        Assert.Equal(VehicleCategory.Suv | VehicleCategory.Ev, v.Category);
        Assert.False(v.IsVisible);
        Assert.Single(v.Images); // images NOT wiped by a scalar update
    }

    [Fact]
    public async Task UpdateAsync_PersistsBodyHtml()
    {
        var db = NewDb(nameof(UpdateAsync_PersistsBodyHtml));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "body", Name = "Body", SortOrder = 1 });

        var ok = await svc.UpdateAsync(new Vehicle
        {
            Id = id, Slug = "body", Name = "Body",
            BodyHtml = new LocalizedText { En = "<p>new body</p>" }
        });

        Assert.True(ok);
        var reloaded = await svc.GetAsync(id);
        Assert.Equal("<p>new body</p>", reloaded!.BodyHtml.En);
    }

    [Fact]
    public async Task Update_ReturnsFalse_WhenMissing()
    {
        var db = NewDb(nameof(Update_ReturnsFalse_WhenMissing));
        var svc = NewSvc(db);
        Assert.False(await svc.UpdateAsync(new Vehicle { Id = 4242, Slug = "nope", Name = "N" }));
    }

    [Fact]
    public async Task MoveImage_SwapsWithinVehicle()
    {
        var db = NewDb(nameof(MoveImage_SwapsWithinVehicle));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "mi", Name = "M" });
        var first = await svc.AddImageAsync(id, "/uploads/1.png", VehicleImageKind.Gallery);  // SortOrder 0
        var second = await svc.AddImageAsync(id, "/uploads/2.png", VehicleImageKind.Gallery); // SortOrder 1

        Assert.True(await svc.MoveImageAsync(second, -1));
        Assert.Equal(0, (await db.VehicleImages.FindAsync(second))!.SortOrder);
        Assert.Equal(1, (await db.VehicleImages.FindAsync(first))!.SortOrder);
    }

    [Fact]
    public async Task Move_OutOfBounds_ReturnsFalse()
    {
        var db = NewDb(nameof(Move_OutOfBounds_ReturnsFalse));
        var svc = NewSvc(db);
        var only = await svc.CreateAsync(new Vehicle { Slug = "solo", Name = "S", SortOrder = 0 });
        Assert.False(await svc.MoveAsync(only, -1)); // already at top
        Assert.False(await svc.MoveAsync(only, 1));  // already at bottom
        Assert.False(await svc.MoveAsync(999999, -1)); // not found
    }

    // ---- Task 20: UpdateAsync new fields ----

    [Fact]
    public async Task UpdateAsync_PersistsEnquiryAndTechFields()
    {
        var db = NewDb(nameof(UpdateAsync_PersistsEnquiryAndTechFields));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "enq", Name = "Enq" });

        var ok = await svc.UpdateAsync(new Vehicle
        {
            Id = id, Slug = "enq", Name = "Enq",
            TechBannerImage = "/uploads/tech.png",
            EnquiryBgImage = "/uploads/bg.png",
            StatsNote = new LocalizedText { En = "note en", Ar = "note ar" },
            EnquiryTitle = new LocalizedText { En = "Get a quote" },
            EnquirySub = new LocalizedText { En = "sub" },
            EnquiryLead = new LocalizedText { En = "lead" }
        });

        Assert.True(ok);
        var v = await svc.GetAsync(id);
        Assert.Equal("/uploads/tech.png", v!.TechBannerImage);
        Assert.Equal("/uploads/bg.png", v.EnquiryBgImage);
        Assert.Equal("note en", v.StatsNote.En);
        Assert.Equal("Get a quote", v.EnquiryTitle.En);
        Assert.Equal("sub", v.EnquirySub.En);
        Assert.Equal("lead", v.EnquiryLead.En);
    }

    // NOTE: GetAsync_Includes_NewCollections_AndQuality (Task 20 round-trip) calls methods from
    // Tasks 26-31 (AddCardAsync/AddSafetyToggleAsync/AddWarrantyLinkAsync/UpsertQualityAsync).
    // It is added in the Task 31 regression sweep once all Add* methods are present.

    // ---- Task 21: UpsertSectionHeading ----

    [Fact]
    public async Task UpsertSectionHeading_InsertsThenUpdatesInPlace()
    {
        var db = NewDb(nameof(UpsertSectionHeading_InsertsThenUpdatesInPlace));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "sh", Name = "S" });

        var first = await svc.UpsertSectionHeadingAsync(vid, SectionKey.Overview,
            new LocalizedText { En = "Overview" }, new LocalizedText { En = "sub" }, new LocalizedText { En = "body" });
        Assert.Equal(1, await db.Set<SectionHeading>().CountAsync());

        var second = await svc.UpsertSectionHeadingAsync(vid, SectionKey.Overview,
            new LocalizedText { En = "Overview 2" }, new LocalizedText { En = "sub 2" }, new LocalizedText { En = "body 2" });
        Assert.Equal(first, second); // same row, not a new one
        Assert.Equal(1, await db.Set<SectionHeading>().CountAsync());
        var row = await db.Set<SectionHeading>().FindAsync(first);
        Assert.Equal("Overview 2", row!.Title.En);
        Assert.Equal("body 2", row.Body.En);

        // a different key creates a second row
        await svc.UpsertSectionHeadingAsync(vid, SectionKey.Design,
            new LocalizedText { En = "Design" }, new LocalizedText(), new LocalizedText());
        Assert.Equal(2, await db.Set<SectionHeading>().CountAsync());
    }
}
