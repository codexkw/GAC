using Microsoft.AspNetCore.Localization;
using Xunit;

namespace GAC.Tests;

public class ArabicRenderTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ArabicRenderTests(DevWebApplicationFactory factory) => _factory = factory;

    private System.Net.Http.HttpClient ArabicClient()
    {
        var client = _factory.CreateClient();
        var cookie = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("ar"));
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookie}");
        return client;
    }

    [Fact]
    public async Task Home_InArabic_IsRtl_AndLoadsRtlCssAndCairo()
    {
        var html = await ArabicClient().GetStringAsync("/");
        Assert.Contains("dir=\"rtl\"", html);
        Assert.Contains("rtl.css", html);
        Assert.Contains("family=Cairo", html);
    }

    [Fact]
    public async Task Home_InArabic_RendersArabicMenuLabels()
    {
        // Razor HTML-encodes Arabic to numeric character references
        // (e.g. &#x627;...), so decode before asserting the literal text.
        var html = System.Net.WebUtility.HtmlDecode(await ArabicClient().GetStringAsync("/"));
        Assert.Contains("الرئيسية", html);  // Home (DB-driven menu label)
        Assert.Contains("الموديلات", html); // Models
    }

    [Fact]
    public async Task Home_InEnglish_IsLtr_NoRtlAssets()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");
        Assert.Contains("dir=\"ltr\"", html);
        Assert.DoesNotContain("rtl.css", html);
    }
}
