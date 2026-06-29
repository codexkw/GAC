using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

// Regression guard: saving content in the admin area must redirect back to the
// admin list, not to the public site. News/Offers each have a same-named public
// controller with an attribute route (/news, /offers), so a bare
// RedirectToAction("Index") that relies only on the ambient area can resolve to
// the public route. The redirect must stay inside /Admin.
public class AdminSaveRedirectTests : IClassFixture<AdminRedirectWebApplicationFactory>
{
    private readonly AdminRedirectWebApplicationFactory _factory;
    public AdminSaveRedirectTests(AdminRedirectWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/Admin/News/Create", "/Admin/News/Save")]
    [InlineData("/Admin/Offers/Create", "/Admin/Offers/Save")]
    public async Task Save_RedirectsToAdminList_NotPublicSite(string createUrl, string saveUrl)
    {
        var client = _factory.ClientForRole(Roles.Editor);

        // GET the create form so the client picks up a valid antiforgery token + cookie.
        var formResponse = await client.GetAsync(createUrl);
        formResponse.EnsureSuccessStatusCode();
        var token = ExtractAntiforgeryToken(await formResponse.Content.ReadAsStringAsync());

        var fields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = "0",
            ["Slug"] = "redirect-test",
            ["Title.En"] = "Redirect Test",   // satisfies News' English-title requirement; ignored by Offer
            ["PublishedOn"] = "2026-01-01",
            ["SortOrder"] = "0",
        };
        var response = await client.PostAsync(saveUrl, new FormUrlEncodedContent(fields));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("/Admin/", location);   // the bug redirected to /news or /offers (public site)
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var m = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""");
        Assert.True(m.Success, "Antiforgery token not found in the create form.");
        return m.Groups[1].Value;
    }
}
