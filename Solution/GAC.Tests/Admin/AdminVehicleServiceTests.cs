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

    // ---- Task 22: StatItem add/move/remove ----

    [Fact]
    public async Task Stat_AddMoveRemove()
    {
        var db = NewDb(nameof(Stat_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "st", Name = "S" });
        var a = await svc.AddStatAsync(vid, new LocalizedText { En = "Power" }, new LocalizedText { En = "177 HP" });
        var b = await svc.AddStatAsync(vid, new LocalizedText { En = "Torque" }, new LocalizedText { En = "270 Nm" });
        Assert.Equal(0, (await db.Set<StatItem>().FindAsync(a))!.SortOrder);
        Assert.Equal(1, (await db.Set<StatItem>().FindAsync(b))!.SortOrder);
        Assert.True(await svc.MoveStatAsync(b, -1));
        Assert.Equal(0, (await db.Set<StatItem>().FindAsync(b))!.SortOrder);
        Assert.True(await svc.RemoveStatAsync(a));
        Assert.Equal(1, await db.Set<StatItem>().CountAsync());
    }

    // ---- Task 23: SliderGroup + SliderSlide ----

    [Fact]
    public async Task Slider_And_Slide_AddMoveRemove()
    {
        var db = NewDb(nameof(Slider_And_Slide_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "sl", Name = "S" });
        var g1 = await svc.AddSliderAsync(vid, new LocalizedText { En = "Eye1" }, new LocalizedText { En = "T1" });
        var g2 = await svc.AddSliderAsync(vid, new LocalizedText { En = "Eye2" }, new LocalizedText { En = "T2" });
        Assert.Equal(0, (await db.Set<SliderGroup>().FindAsync(g1))!.SortOrder);
        Assert.True(await svc.MoveSliderAsync(g2, -1));
        Assert.Equal(0, (await db.Set<SliderGroup>().FindAsync(g2))!.SortOrder);

        var s1 = await svc.AddSliderSlideAsync(g1, "/uploads/a.png", new LocalizedText { En = "a" });
        var s2 = await svc.AddSliderSlideAsync(g1, "/uploads/b.png", new LocalizedText { En = "b" });
        Assert.Equal(0, (await db.Set<SliderSlide>().FindAsync(s1))!.SortOrder);
        Assert.Equal(1, (await db.Set<SliderSlide>().FindAsync(s2))!.SortOrder);
        Assert.True(await svc.MoveSliderSlideAsync(s2, -1));
        Assert.Equal(0, (await db.Set<SliderSlide>().FindAsync(s2))!.SortOrder);
        Assert.True(await svc.RemoveSliderSlideAsync(s1));
        Assert.Equal(1, await db.Set<SliderSlide>().CountAsync());
        Assert.True(await svc.RemoveSliderAsync(g1));
    }

    [Fact]
    public async Task AddSliderSlide_OnMissingGroup_ReturnsZero()
    {
        var db = NewDb(nameof(AddSliderSlide_OnMissingGroup_ReturnsZero));
        var svc = NewSvc(db);
        Assert.Equal(0, await svc.AddSliderSlideAsync(999999, "/x.png", new LocalizedText { En = "x" }));
    }

    // ---- Task 24: FeatureSection new fields + FeatureBullet ----

    [Fact]
    public async Task Feature_NewFields_Persist_AndBullets_AddMoveRemove()
    {
        var db = NewDb(nameof(Feature_NewFields_Persist_AndBullets_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "fb", Name = "F" });
        var fid = await svc.AddFeatureAsync(vid, new FeatureSection
        {
            Heading = "Panel",
            GroupKey = FeatureGroup.Design,
            TabLabel = new LocalizedText { En = "Design" },
            Lead = new LocalizedText { En = "lead text" }
        });
        var f = await svc.GetFeatureAsync(fid);
        Assert.Equal(FeatureGroup.Design, f!.GroupKey);
        Assert.Equal("Design", f.TabLabel.En);
        Assert.Equal("lead text", f.Lead.En);

        var b1 = await svc.AddFeatureBulletAsync(fid, new LocalizedText { En = "L1" }, new LocalizedText { En = "T1" });
        var b2 = await svc.AddFeatureBulletAsync(fid, new LocalizedText { En = "L2" }, new LocalizedText { En = "T2" });
        Assert.Equal(0, (await db.Set<FeatureBullet>().FindAsync(b1))!.SortOrder);
        Assert.Equal(1, (await db.Set<FeatureBullet>().FindAsync(b2))!.SortOrder);
        Assert.True(await svc.MoveFeatureBulletAsync(b2, -1));
        Assert.Equal(0, (await db.Set<FeatureBullet>().FindAsync(b2))!.SortOrder);
        Assert.True(await svc.RemoveFeatureBulletAsync(b1));
        Assert.Equal(1, await db.Set<FeatureBullet>().CountAsync());

        var ok = await svc.UpdateFeatureAsync(new FeatureSection
        {
            Id = fid, Heading = "Panel2", GroupKey = FeatureGroup.Performance,
            TabLabel = new LocalizedText { En = "Perf" }, Lead = new LocalizedText { En = "lead2" }
        });
        Assert.True(ok);
        var f2 = await svc.GetFeatureAsync(fid);
        Assert.Equal(FeatureGroup.Performance, f2!.GroupKey);
        Assert.Equal("Perf", f2.TabLabel.En);
        Assert.Equal("lead2", f2.Lead.En);
    }

    // ---- Task 25: GalleryTab + GalleryImage ----

    [Fact]
    public async Task GalleryTab_And_Image_AddMoveRemove()
    {
        var db = NewDb(nameof(GalleryTab_And_Image_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "gt", Name = "G" });
        var t1 = await svc.AddGalleryTabAsync(vid, new LocalizedText { En = "Exterior" });
        var t2 = await svc.AddGalleryTabAsync(vid, new LocalizedText { En = "Interior" });
        Assert.Equal(0, (await db.Set<GalleryTab>().FindAsync(t1))!.SortOrder);
        Assert.True(await svc.MoveGalleryTabAsync(t2, -1));
        Assert.Equal(0, (await db.Set<GalleryTab>().FindAsync(t2))!.SortOrder);

        var i1 = await svc.AddGalleryImageAsync(t1, "/uploads/a.png", new LocalizedText { En = "a" });
        var i2 = await svc.AddGalleryImageAsync(t1, "/uploads/b.png", new LocalizedText { En = "b" });
        Assert.Equal(0, (await db.Set<GalleryImage>().FindAsync(i1))!.SortOrder);
        Assert.Equal(1, (await db.Set<GalleryImage>().FindAsync(i2))!.SortOrder);
        Assert.True(await svc.MoveGalleryImageAsync(i2, -1));
        Assert.Equal(0, (await db.Set<GalleryImage>().FindAsync(i2))!.SortOrder);
        Assert.True(await svc.RemoveGalleryImageAsync(i1));
        Assert.Equal(1, await db.Set<GalleryImage>().CountAsync());
        Assert.True(await svc.RemoveGalleryTabAsync(t1));
    }

    [Fact]
    public async Task AddGalleryImage_OnMissingTab_ReturnsZero()
    {
        var db = NewDb(nameof(AddGalleryImage_OnMissingTab_ReturnsZero));
        var svc = NewSvc(db);
        Assert.Equal(0, await svc.AddGalleryImageAsync(999999, "/x.png", new LocalizedText { En = "x" }));
    }

    // ---- Task 26: QualityBlock upsert/remove ----

    [Fact]
    public async Task Quality_Upsert_IsSingleton_AndRemove()
    {
        var db = NewDb(nameof(Quality_Upsert_IsSingleton_AndRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "qb", Name = "Q" });

        // First upsert inserts
        var id1 = await svc.UpsertQualityAsync(vid, "/main1.jpg", "/thumb1.jpg",
            new LocalizedText { En = "Strap" }, new LocalizedText { En = "Body" });
        Assert.NotEqual(0, id1);
        Assert.Equal(1, await db.QualityBlocks.CountAsync());

        // Second upsert updates same row
        var id2 = await svc.UpsertQualityAsync(vid, "/main2.jpg", "/thumb2.jpg",
            new LocalizedText { En = "Strap 2" }, new LocalizedText { En = "Body 2" });
        Assert.Equal(id1, id2); // same row
        Assert.Equal(1, await db.QualityBlocks.CountAsync());
        var row = await db.QualityBlocks.FindAsync(id1);
        Assert.Equal("/main2.jpg", row!.MainImage);
        Assert.Equal("Strap 2", row.Strapline.En);

        // Remove returns true first time, false second time
        Assert.True(await svc.RemoveQualityAsync(vid));
        Assert.Equal(0, await db.QualityBlocks.CountAsync());
        Assert.False(await svc.RemoveQualityAsync(vid));
    }

    // ---- Task 27: CardItem add/move/remove ----

    [Fact]
    public async Task Card_AddMoveRemove()
    {
        var db = NewDb(nameof(Card_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "card", Name = "C" });

        var c1 = await svc.AddCardAsync(vid, new LocalizedText { En = "T1" }, new LocalizedText { En = "Body1" }, "/img1.png");
        var c2 = await svc.AddCardAsync(vid, new LocalizedText { En = "T2" }, new LocalizedText { En = "Body2" }, "/img2.png");
        Assert.Equal(0, (await db.CardItems.FindAsync(c1))!.SortOrder);
        Assert.Equal(1, (await db.CardItems.FindAsync(c2))!.SortOrder);
        Assert.Equal("/img2.png", (await db.CardItems.FindAsync(c2))!.ImagePath);

        Assert.True(await svc.MoveCardAsync(c2, -1));
        Assert.Equal(0, (await db.CardItems.FindAsync(c2))!.SortOrder);

        Assert.True(await svc.RemoveCardAsync(c1));
        Assert.Equal(1, await db.CardItems.CountAsync());
    }

    // ---- Task 28: SafetyToggle add/move/remove ----

    [Fact]
    public async Task SafetyToggle_AddMoveRemove()
    {
        var db = NewDb(nameof(SafetyToggle_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "st2", Name = "S" });

        var t1 = await svc.AddSafetyToggleAsync(vid, new LocalizedText { En = "Title1" }, "/img1.png",
            new LocalizedText { En = "Strap1" }, new LocalizedText { En = "Content1" });
        var t2 = await svc.AddSafetyToggleAsync(vid, new LocalizedText { En = "Title2" }, "/img2.png",
            new LocalizedText { En = "Strap2" }, new LocalizedText { En = "Content2" });
        Assert.Equal(0, (await db.SafetyToggles.FindAsync(t1))!.SortOrder);
        Assert.Equal(1, (await db.SafetyToggles.FindAsync(t2))!.SortOrder);
        Assert.Equal("Strap2", (await db.SafetyToggles.FindAsync(t2))!.Strap.En);

        Assert.True(await svc.MoveSafetyToggleAsync(t2, -1));
        Assert.Equal(0, (await db.SafetyToggles.FindAsync(t2))!.SortOrder);

        Assert.True(await svc.RemoveSafetyToggleAsync(t1));
        Assert.Equal(1, await db.SafetyToggles.CountAsync());
    }

    // ---- Task 29: Trim new fields + TrimPriceRow ----

    [Fact]
    public async Task Trim_NewFields_Persist_AndPriceRows_AddMoveRemove()
    {
        var db = NewDb(nameof(Trim_NewFields_Persist_AndPriceRows_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "tr", Name = "T" });

        var trimId = await svc.AddTrimAsync(vid, new Trim
        {
            Name = new LocalizedText { En = "Prestige" },
            ModelLabel = new LocalizedText { En = "GS4 MAX Prestige" },
            ImagePath = "/uploads/prestige.png"
        });
        var trim = await db.Set<Trim>().FindAsync(trimId);
        Assert.Equal("GS4 MAX Prestige", trim!.ModelLabel.En);
        Assert.Equal("/uploads/prestige.png", trim.ImagePath);

        var r1 = await svc.AddTrimPriceRowAsync(trimId, new LocalizedText { En = "Base price" });
        var r2 = await svc.AddTrimPriceRowAsync(trimId, new LocalizedText { En = "With options" });
        Assert.Equal(0, (await db.TrimPriceRows.FindAsync(r1))!.SortOrder);
        Assert.Equal(1, (await db.TrimPriceRows.FindAsync(r2))!.SortOrder);

        Assert.True(await svc.MoveTrimPriceRowAsync(r2, -1));
        Assert.Equal(0, (await db.TrimPriceRows.FindAsync(r2))!.SortOrder);

        Assert.True(await svc.RemoveTrimPriceRowAsync(r1));
        Assert.Equal(1, await db.TrimPriceRows.CountAsync());
    }

    [Fact]
    public async Task AddTrimPriceRow_OnMissingTrim_ReturnsZero()
    {
        var db = NewDb(nameof(AddTrimPriceRow_OnMissingTrim_ReturnsZero));
        var svc = NewSvc(db);
        Assert.Equal(0, await svc.AddTrimPriceRowAsync(999999, new LocalizedText { En = "x" }));
    }

    // ---- Task 30: WarrantyLink add/move/remove ----

    [Fact]
    public async Task WarrantyLink_AddMoveRemove()
    {
        var db = NewDb(nameof(WarrantyLink_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "wl", Name = "W" });

        var l1 = await svc.AddWarrantyLinkAsync(vid, new LocalizedText { En = "Policy" }, "https://example.com/policy");
        var l2 = await svc.AddWarrantyLinkAsync(vid, new LocalizedText { En = "Terms" }, "https://example.com/terms");
        Assert.Equal(0, (await db.WarrantyLinks.FindAsync(l1))!.SortOrder);
        Assert.Equal(1, (await db.WarrantyLinks.FindAsync(l2))!.SortOrder);
        Assert.Equal("https://example.com/terms", (await db.WarrantyLinks.FindAsync(l2))!.Url);

        Assert.True(await svc.MoveWarrantyLinkAsync(l2, -1));
        Assert.Equal(0, (await db.WarrantyLinks.FindAsync(l2))!.SortOrder);

        Assert.True(await svc.RemoveWarrantyLinkAsync(l1));
        Assert.Equal(1, await db.WarrantyLinks.CountAsync());
    }

    // ---- Task 31: regression sweep ----

    [Fact]
    public async Task AddMethods_OnMissingVehicle_ReturnZero()
    {
        var db = NewDb(nameof(AddMethods_OnMissingVehicle_ReturnZero));
        var svc = NewSvc(db);
        const int ghost = 987654;

        Assert.Equal(0, await svc.AddStatAsync(ghost, new LocalizedText { En = "x" }, new LocalizedText { En = "x" }));
        Assert.Equal(0, await svc.AddSliderAsync(ghost, new LocalizedText { En = "x" }, new LocalizedText { En = "x" }));
        Assert.Equal(0, await svc.AddGalleryTabAsync(ghost, new LocalizedText { En = "x" }));
        Assert.Equal(0, await svc.AddFeatureBulletAsync(ghost, new LocalizedText { En = "x" }, new LocalizedText { En = "x" }));  // missing featureSection
        Assert.Equal(0, await svc.AddCardAsync(ghost, new LocalizedText { En = "x" }, new LocalizedText { En = "x" }, null));
        Assert.Equal(0, await svc.AddSafetyToggleAsync(ghost, new LocalizedText { En = "x" }, null, new LocalizedText { En = "x" }, new LocalizedText { En = "x" }));
        Assert.Equal(0, await svc.AddWarrantyLinkAsync(ghost, new LocalizedText { En = "x" }, null));
        Assert.Equal(0, await svc.UpsertQualityAsync(ghost, null, null, new LocalizedText { En = "x" }, new LocalizedText { En = "x" }));
        Assert.Equal(0, await svc.AddTrimPriceRowAsync(ghost, new LocalizedText { En = "x" }));   // missing trim
    }

    [Fact]
    public async Task MoveMethods_OutOfBounds_ReturnFalse()
    {
        var db = NewDb(nameof(MoveMethods_OutOfBounds_ReturnFalse));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "oob", Name = "O" });

        var statId = await svc.AddStatAsync(vid, new LocalizedText { En = "L" }, new LocalizedText { En = "V" });
        Assert.False(await svc.MoveStatAsync(statId, -1));   // already at top (only 1)
        Assert.False(await svc.MoveStatAsync(statId, 1));    // already at bottom
        Assert.False(await svc.RemoveStatAsync(999999));     // missing id

        var cardId = await svc.AddCardAsync(vid, new LocalizedText { En = "T" }, new LocalizedText { En = "B" }, null);
        Assert.False(await svc.MoveCardAsync(cardId, -1));
        Assert.False(await svc.MoveCardAsync(cardId, 1));
        Assert.False(await svc.MoveCardAsync(999999, -1));

        var toggleId = await svc.AddSafetyToggleAsync(vid, new LocalizedText { En = "T" }, null, new LocalizedText { En = "S" }, new LocalizedText { En = "C" });
        Assert.False(await svc.MoveSafetyToggleAsync(toggleId, -1));
        Assert.False(await svc.MoveSafetyToggleAsync(toggleId, 1));

        var linkId = await svc.AddWarrantyLinkAsync(vid, new LocalizedText { En = "L" }, "https://example.com");
        Assert.False(await svc.MoveWarrantyLinkAsync(linkId, -1));
        Assert.False(await svc.MoveWarrantyLinkAsync(linkId, 1));

        var trimId = await svc.AddTrimAsync(vid, new Trim { Name = new LocalizedText { En = "Trim" } });
        var rowId = await svc.AddTrimPriceRowAsync(trimId, new LocalizedText { En = "Row" });
        Assert.False(await svc.MoveTrimPriceRowAsync(rowId, -1));
        Assert.False(await svc.MoveTrimPriceRowAsync(rowId, 1));
    }

    [Fact]
    public async Task GetAsync_EagerLoads_Grandchildren()
    {
        var db = NewDb(nameof(GetAsync_EagerLoads_Grandchildren));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "eager", Name = "E" });

        // Grandchild: SliderSlide
        var sliderId = await svc.AddSliderAsync(vid, new LocalizedText { En = "Eye" }, new LocalizedText { En = "Title" });
        await svc.AddSliderSlideAsync(sliderId, "/s.png", new LocalizedText { En = "alt" });

        // Grandchild: GalleryImage
        var tabId = await svc.AddGalleryTabAsync(vid, new LocalizedText { En = "Tab" });
        await svc.AddGalleryImageAsync(tabId, "/g.png", new LocalizedText { En = "alt" });

        // Grandchild: FeatureBullet
        var featureId = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "F" });
        await svc.AddFeatureBulletAsync(featureId, new LocalizedText { En = "Lbl" }, new LocalizedText { En = "Txt" });

        // Grandchild: TrimPriceRow
        var trimId = await svc.AddTrimAsync(vid, new Trim { Name = new LocalizedText { En = "Trim" } });
        await svc.AddTrimPriceRowAsync(trimId, new LocalizedText { En = "Row" });

        // Direct children new in Tasks 26-30
        await svc.AddStatAsync(vid, new LocalizedText { En = "Label" }, new LocalizedText { En = "Val" });
        await svc.AddCardAsync(vid, new LocalizedText { En = "CardTitle" }, new LocalizedText { En = "CardBody" }, null);
        await svc.AddSafetyToggleAsync(vid, new LocalizedText { En = "SafeTitle" }, null, new LocalizedText { En = "Strap" }, new LocalizedText { En = "Content" });
        await svc.AddWarrantyLinkAsync(vid, new LocalizedText { En = "Warranty" }, "https://example.com");
        await svc.UpsertQualityAsync(vid, null, null, new LocalizedText { En = "Strap" }, new LocalizedText { En = "Body" });
        await svc.UpsertSectionHeadingAsync(vid, SectionKey.Overview, new LocalizedText { En = "Heading" }, new LocalizedText { En = "Sub" }, new LocalizedText { En = "Body" });

        var v = await svc.GetAsync(vid);
        Assert.NotNull(v);
        Assert.Single(v!.Sliders);
        Assert.Single(v.Sliders[0].Slides);
        Assert.Single(v.GalleryTabs);
        Assert.Single(v.GalleryTabs[0].Images);
        Assert.Single(v.Features);
        Assert.Single(v.Features[0].Bullets);
        Assert.Single(v.Trims);
        Assert.Single(v.Trims[0].PriceRows);
        Assert.Single(v.Stats);
        Assert.Single(v.Cards);
        Assert.Single(v.SafetyToggles);
        Assert.Single(v.WarrantyLinks);
        Assert.NotNull(v.Quality);
        Assert.Single(v.Headings);
    }
}
