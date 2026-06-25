using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

// Per-item "edit/replace" support for every image-bearing vehicle section
// (main images, colours, slider slides, gallery images, cards, safety toggles).
public class ImageEditTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private sealed class NoopSanitizer : GAC.Core.Services.IHtmlSanitizerService
    {
        public string Sanitize(string? html) => html ?? "";
    }

    private static AdminVehicleService Svc(ApplicationDbContext db) => new(db, new NoopSanitizer());

    [Fact]
    public async Task UpdateImageAsync_Replaces_Path_And_Kind_Preserves_Order()
    {
        var db = NewDb(nameof(UpdateImageAsync_Replaces_Path_And_Kind_Preserves_Order));
        db.VehicleImages.Add(new VehicleImage { Id = 1, VehicleId = 7, Path = "/old.png", Kind = VehicleImageKind.Hero, SortOrder = 3 });
        await db.SaveChangesAsync();

        Assert.True(await Svc(db).UpdateImageAsync(1, "/new.png", VehicleImageKind.Gallery));

        var e = await db.VehicleImages.FindAsync(1);
        Assert.Equal("/new.png", e!.Path);
        Assert.Equal(VehicleImageKind.Gallery, e.Kind);
        Assert.Equal(3, e.SortOrder);
        Assert.Equal(7, e.VehicleId);
    }

    [Fact]
    public async Task UpdateColorAsync_Replaces_Fields_Preserves_Order()
    {
        var db = NewDb(nameof(UpdateColorAsync_Replaces_Fields_Preserves_Order));
        db.Set<ColorOption>().Add(new ColorOption { Id = 1, VehicleId = 7, Name = new() { En = "Red" }, Hex = "#f00", ImagePath = "/old.png", SortOrder = 2 });
        await db.SaveChangesAsync();

        Assert.True(await Svc(db).UpdateColorAsync(1, new() { En = "Blue" }, "#00f", "/new.png"));

        var e = await db.Set<ColorOption>().FindAsync(1);
        Assert.Equal("/new.png", e!.ImagePath);
        Assert.Equal("Blue", e.Name.En);
        Assert.Equal("#00f", e.Hex);
        Assert.Equal(2, e.SortOrder);
        Assert.Equal(7, e.VehicleId);
    }

    [Fact]
    public async Task UpdateColorAsync_Blank_Hex_Coalesces()
    {
        var db = NewDb(nameof(UpdateColorAsync_Blank_Hex_Coalesces));
        db.Set<ColorOption>().Add(new ColorOption { Id = 1, VehicleId = 7, Hex = "#f00" });
        await db.SaveChangesAsync();
        await Svc(db).UpdateColorAsync(1, new() { En = "X" }, "  ", "/x.png");
        Assert.Equal("#000000", (await db.Set<ColorOption>().FindAsync(1))!.Hex);
    }

    [Fact]
    public async Task UpdateSliderSlideAsync_Replaces_Image_And_Alt_Preserves_Parent()
    {
        var db = NewDb(nameof(UpdateSliderSlideAsync_Replaces_Image_And_Alt_Preserves_Parent));
        db.Set<SliderSlide>().Add(new SliderSlide { Id = 1, SliderGroupId = 9, ImagePath = "/old.png", Alt = new() { En = "old" }, SortOrder = 4 });
        await db.SaveChangesAsync();

        Assert.True(await Svc(db).UpdateSliderSlideAsync(1, "/new.png", new() { En = "new" }));

        var e = await db.Set<SliderSlide>().FindAsync(1);
        Assert.Equal("/new.png", e!.ImagePath);
        Assert.Equal("new", e.Alt.En);
        Assert.Equal(4, e.SortOrder);
        Assert.Equal(9, e.SliderGroupId);
    }

    [Fact]
    public async Task UpdateGalleryImageAsync_Replaces_Image_And_Alt_Preserves_Parent()
    {
        var db = NewDb(nameof(UpdateGalleryImageAsync_Replaces_Image_And_Alt_Preserves_Parent));
        db.Set<GalleryImage>().Add(new GalleryImage { Id = 1, GalleryTabId = 9, ImagePath = "/old.png", Alt = new() { En = "old" }, SortOrder = 5 });
        await db.SaveChangesAsync();

        Assert.True(await Svc(db).UpdateGalleryImageAsync(1, "/new.png", new() { En = "new" }));

        var e = await db.Set<GalleryImage>().FindAsync(1);
        Assert.Equal("/new.png", e!.ImagePath);
        Assert.Equal("new", e.Alt.En);
        Assert.Equal(5, e.SortOrder);
        Assert.Equal(9, e.GalleryTabId);
    }

    [Fact]
    public async Task UpdateCardAsync_Replaces_Fields_Preserves_Order()
    {
        var db = NewDb(nameof(UpdateCardAsync_Replaces_Fields_Preserves_Order));
        db.CardItems.Add(new CardItem { Id = 1, VehicleId = 7, Title = new() { En = "t" }, Text = new() { En = "x" }, ImagePath = "/old.png", SortOrder = 1 });
        await db.SaveChangesAsync();

        Assert.True(await Svc(db).UpdateCardAsync(1, new() { En = "t2" }, new() { En = "x2" }, "/new.png"));

        var e = await db.CardItems.FindAsync(1);
        Assert.Equal("/new.png", e!.ImagePath);
        Assert.Equal("t2", e.Title.En);
        Assert.Equal("x2", e.Text.En);
        Assert.Equal(1, e.SortOrder);
        Assert.Equal(7, e.VehicleId);
    }

    [Fact]
    public async Task UpdateSafetyToggleAsync_Replaces_Fields_Preserves_Order()
    {
        var db = NewDb(nameof(UpdateSafetyToggleAsync_Replaces_Fields_Preserves_Order));
        db.SafetyToggles.Add(new SafetyToggle { Id = 1, VehicleId = 7, Title = new() { En = "t" }, ImagePath = "/old.png", Strap = new() { En = "s" }, Content = new() { En = "c" }, SortOrder = 6 });
        await db.SaveChangesAsync();

        Assert.True(await Svc(db).UpdateSafetyToggleAsync(1, new() { En = "t2" }, "/new.png", new() { En = "s2" }, new() { En = "c2" }));

        var e = await db.SafetyToggles.FindAsync(1);
        Assert.Equal("/new.png", e!.ImagePath);
        Assert.Equal("t2", e.Title.En);
        Assert.Equal("s2", e.Strap.En);
        Assert.Equal("c2", e.Content.En);
        Assert.Equal(6, e.SortOrder);
        Assert.Equal(7, e.VehicleId);
    }

    [Fact]
    public async Task Update_Returns_False_For_Missing_Items()
    {
        var db = NewDb(nameof(Update_Returns_False_For_Missing_Items));
        var s = Svc(db);
        Assert.False(await s.UpdateImageAsync(404, "/x.png", VehicleImageKind.Hero));
        Assert.False(await s.UpdateColorAsync(404, new(), "#000", "/x.png"));
        Assert.False(await s.UpdateSliderSlideAsync(404, "/x.png", new()));
        Assert.False(await s.UpdateGalleryImageAsync(404, "/x.png", new()));
        Assert.False(await s.UpdateCardAsync(404, new(), new(), "/x.png"));
        Assert.False(await s.UpdateSafetyToggleAsync(404, new(), "/x.png", new(), new()));
    }
}
