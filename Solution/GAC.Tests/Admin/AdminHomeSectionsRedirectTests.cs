using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

// The admin "Home Sections" page lives in the Admin area; saving must redirect
// back into /Admin (not leak to a public route), and the page must render.
public class AdminHomeSectionsRedirectTests : IClassFixture<AdminRedirectWebApplicationFactory>
{
    private readonly AdminRedirectWebApplicationFactory _factory;
    public AdminHomeSectionsRedirectTests(AdminRedirectWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task SavePromo_RedirectsIntoAdmin()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var form = await client.GetAsync("/Admin/HomeSections");
        form.EnsureSuccessStatusCode();
        var token = Regex.Match(await form.Content.ReadAsStringAsync(),
            @"name=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""").Groups[1].Value;

        var resp = await client.PostAsync("/Admin/HomeSections/SavePromo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ImagePath"] = "/a.jpg",
            ["Heading.En"] = "Latest Offers",
        }));

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.StartsWith("/Admin/", resp.Headers.Location!.ToString());
    }
}
