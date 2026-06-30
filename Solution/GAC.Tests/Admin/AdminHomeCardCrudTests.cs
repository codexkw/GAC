using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GAC.Core.Identity;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Admin;

// End-to-end (in-memory) coverage of the Home Sections "add card" and "remove
// card" admin actions: the routes exist, are antiforgery-protected, redirect
// back into /Admin, and actually create / delete a DualCard row. Also asserts
// the editor page renders the Add + Remove controls.
public class AdminHomeCardCrudTests : IClassFixture<AdminInMemoryWebApplicationFactory>
{
    private readonly AdminInMemoryWebApplicationFactory _factory;
    public AdminHomeCardCrudTests(AdminInMemoryWebApplicationFactory f) => _factory = f;

    private async Task<string> TokenAsync(HttpClient client)
    {
        var page = await client.GetAsync("/Admin/HomeSections");
        page.EnsureSuccessStatusCode();
        return Regex.Match(await page.Content.ReadAsStringAsync(),
            @"name=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""").Groups[1].Value;
    }

    private async Task<int> CardCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.DualCards.CountAsync();
    }

    [Fact]
    public async Task HomeSections_Page_RendersAddAndRemoveControls()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var html = await (await client.GetAsync("/Admin/HomeSections")).Content.ReadAsStringAsync();
        Assert.Contains("/Admin/HomeSections/AddCard", html);
        Assert.Contains("/Admin/HomeSections/DeleteCard", html);
    }

    [Fact]
    public async Task AddCard_CreatesRow_RedirectsIntoAdmin()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var token = await TokenAsync(client);
        var before = await CardCountAsync();

        var resp = await client.PostAsync("/Admin/HomeSections/AddCard", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ImagePath"] = "/zzz-add.jpg",
            ["Title.En"] = "ZZZ-ADD-CARD",
        }));

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.StartsWith("/Admin/", resp.Headers.Location!.ToString());
        Assert.Equal(before + 1, await CardCountAsync());
    }

    [Fact]
    public async Task DeleteCard_RemovesRow_RedirectsIntoAdmin()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var token = await TokenAsync(client);

        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            id = await db.DualCards.Select(c => c.Id).FirstAsync();
        }
        var before = await CardCountAsync();

        var resp = await client.PostAsync("/Admin/HomeSections/DeleteCard", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["id"] = id.ToString(),
        }));

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.StartsWith("/Admin/", resp.Headers.Location!.ToString());
        Assert.Equal(before - 1, await CardCountAsync());
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Null(await db.DualCards.FindAsync(id));
        }
    }
}
