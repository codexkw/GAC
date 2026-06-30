using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

// The admin "Warranty page" editor lives in the Admin area; saving must redirect
// back into /Admin and the page must render. In-memory DB (no prod contact).
public class AdminWarrantyRedirectTests : IClassFixture<AdminInMemoryWebApplicationFactory>
{
    private readonly AdminInMemoryWebApplicationFactory _factory;
    public AdminWarrantyRedirectTests(AdminInMemoryWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task Save_RedirectsIntoAdmin()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var form = await client.GetAsync("/Admin/Warranty");
        form.EnsureSuccessStatusCode();
        var token = Regex.Match(await form.Content.ReadAsStringAsync(),
            @"name=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""").Groups[1].Value;

        var resp = await client.PostAsync("/Admin/Warranty/Save", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["BannerImagePath"] = "/a.jpg",
            ["TermsImagePath"] = "/t.jpg",
            ["Heading.En"] = "Warranty",
        }));

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.StartsWith("/Admin/", resp.Headers.Location!.ToString());
    }
}
