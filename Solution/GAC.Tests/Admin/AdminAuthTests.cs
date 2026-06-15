using System.Net;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminAuthTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;
    public AdminAuthTests(AdminWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_Anonymous_RedirectsToLogin()
    {
        var res = await _factory.ClientForRole(null).GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Found, res.StatusCode);
        Assert.Contains("/admin/login", res.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Dashboard_Admin_Ok()
    {
        var res = await _factory.ClientForRole(Roles.Admin).GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_Anonymous_Ok()
    {
        var res = await _factory.ClientForRole(null).GetAsync("/admin/login");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Sales, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.Found)] // Editor lacks LeadsAccess → redirected to AccessDenied
    public async Task Leads_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/Leads");
        Assert.Equal(expected, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.OK)]
    [InlineData(Roles.Sales, HttpStatusCode.Found)] // Sales lacks ContentEditor
    public async Task Media_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/Media");
        Assert.Equal(expected, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.OK)]
    [InlineData(Roles.Sales, HttpStatusCode.Found)]
    public async Task Vehicles_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/Vehicles");
        Assert.Equal(expected, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.OK)]
    [InlineData(Roles.Sales, HttpStatusCode.Found)]
    public async Task Menu_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/Menu");
        Assert.Equal(expected, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.OK)]
    [InlineData(Roles.Sales, HttpStatusCode.Found)]
    public async Task HeroSlides_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/HomeContent");
        Assert.Equal(expected, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.OK)]
    [InlineData(Roles.Sales, HttpStatusCode.Found)]
    public async Task News_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/News");
        Assert.Equal(expected, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.OK)]
    [InlineData(Roles.Sales, HttpStatusCode.Found)]
    public async Task Offers_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/Offers");
        Assert.Equal(expected, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.OK)]
    [InlineData(Roles.Sales, HttpStatusCode.Found)]
    public async Task ContentPages_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/ContentPages");
        Assert.Equal(expected, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.OK)]
    [InlineData(Roles.Sales, HttpStatusCode.Found)]
    public async Task FormPages_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/FormPages");
        Assert.Equal(expected, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, HttpStatusCode.OK)]
    [InlineData(Roles.Editor, HttpStatusCode.Found)]
    [InlineData(Roles.Sales, HttpStatusCode.Found)]
    public async Task Settings_AccessByRole(string role, HttpStatusCode expected)
    {
        var res = await _factory.ClientForRole(role).GetAsync("/Admin/Settings");
        Assert.Equal(expected, res.StatusCode);
    }
}
