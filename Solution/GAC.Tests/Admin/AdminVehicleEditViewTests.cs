using System.Net;
using System.Text.RegularExpressions;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminVehicleEditViewTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;
    public AdminVehicleEditViewTests(AdminWebApplicationFactory factory) => _factory = factory;

    // Resolve a real vehicle id from the shared dev DB via the admin list, then GET its Edit page as an Editor.
    private async Task<string> GetFirstVehicleEditHtmlAsync()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var list = await client.GetStringAsync("/Admin/Vehicles");
        var m = Regex.Match(list, @"/Admin/Vehicles/Edit/(\d+)");
        Assert.True(m.Success, "Expected at least one vehicle Edit link in the admin list.");
        var res = await client.GetAsync($"/Admin/Vehicles/Edit/{m.Groups[1].Value}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Edit_RendersScalarFields_TechBanner_StatsNote_Enquiry()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Technology banner image", html);
        Assert.Contains("Stats note", html);
        Assert.Contains("Enquiry background image", html);
        Assert.Contains("Enquiry title", html);
        Assert.Contains("Enquiry sub", html);
        Assert.Contains("Enquiry lead", html);
    }

    [Fact]
    public async Task Edit_RendersSectionNav()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("adm-section-nav", html);
    }

    [Fact]
    public async Task Edit_RendersSectionHeadingsPanel_AllEightKeys()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Section headings", html);
        foreach (var key in new[] { "Overview", "Design", "Gallery", "Technology", "Performance", "Safety", "Trims", "Warranty" })
            Assert.Contains($"UpsertSectionHeading", html);
        Assert.Contains("name=\"key\" value=\"Overview\"", html);
        Assert.Contains("name=\"key\" value=\"Warranty\"", html);
    }

    [Fact]
    public async Task Edit_RendersStatsPanel()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Overview stats", html);
        Assert.Contains("AddStat", html);
    }

    [Fact]
    public async Task Edit_RendersSlidersPanel_WithSlideForm()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Sliders", html);
        Assert.Contains("AddSlider", html);
        // AddSliderSlide only renders when at least one slider group exists
    }

    [Fact]
    public async Task Edit_FeaturesList_ShowsGroupColumn()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Feature sections", html);
        Assert.Contains(">Group<", html); // new column header
    }

    [Fact]
    public async Task Edit_RendersGalleryTabsPanel_WithImageForm()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Gallery tabs", html);
        Assert.Contains("AddGalleryTab", html);
        // AddGalleryImage only renders when at least one gallery tab exists in the DB
    }
}

public class AdminVehicleEditViewSmokeTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;
    public AdminVehicleEditViewSmokeTests(AdminWebApplicationFactory factory) => _factory = factory;

    private async Task<string> EditHtmlAsync(string role)
    {
        var client = _factory.ClientForRole(role);
        var list = await client.GetStringAsync("/Admin/Vehicles");
        var m = Regex.Match(list, @"/Admin/Vehicles/Edit/(\d+)");
        Assert.True(m.Success);
        var res = await client.GetAsync($"/Admin/Vehicles/Edit/{m.Groups[1].Value}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Edit_AsEditor_RendersEveryRichSectionPanel()
    {
        var html = await EditHtmlAsync(Roles.Editor);
        foreach (var heading in new[]
        {
            "adm-section-nav",
            "Section headings",
            "Overview stats",
            "Sliders",
            "Feature sections",
            "Gallery tabs",
            "Quality / awards",
            "Technology cards",
            "Safety toggles",
            "Trims",
            "Warranty links",
            "Technology banner image",
            "Enquiry title",
        })
            Assert.Contains(heading, html);
        // PickerModal rendered exactly once.
        Assert.Single(Regex.Matches(html, "id=\"mediaPicker\""));
        // Assert all key action names are present (unconditional in every section)
        Assert.Contains("UpsertSectionHeading", html);
        Assert.Contains("AddStat", html);
        Assert.Contains("AddSlider", html);
        Assert.Contains("AddGalleryTab", html);
        Assert.Contains("UpsertQuality", html);
        Assert.Contains("AddCard", html);
        Assert.Contains("AddSafetyToggle", html);
        Assert.Contains("AddWarrantyLink", html);
    }

    [Fact]
    public async Task Edit_AsSales_IsForbidden()
    {
        var client = _factory.ClientForRole(Roles.Sales);
        var list = await client.GetAsync("/Admin/Vehicles");
        // Sales lacks ContentEditor → redirected away from the Vehicles controller.
        Assert.Equal(HttpStatusCode.Found, list.StatusCode);
    }
}

public class AdminFeatureEditViewTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;
    public AdminFeatureEditViewTests(AdminWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task FeatureEdit_New_RendersGroupTabLabelLead()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var list = await client.GetStringAsync("/Admin/Vehicles");
        var m = Regex.Match(list, @"/Admin/Vehicles/Edit/(\d+)");
        Assert.True(m.Success);
        var vid = m.Groups[1].Value;
        var res = await client.GetAsync($"/Admin/Vehicles/FeatureEdit?vehicleId={vid}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("GroupKey", html);     // group select bound to FeatureGroup
        Assert.Contains("TabLabel.En", html);  // tab label field
        Assert.Contains("Lead.En", html);      // lead paragraph field
    }
}
