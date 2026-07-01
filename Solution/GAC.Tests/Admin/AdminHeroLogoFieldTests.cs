using System.Threading.Tasks;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

// The hero-slide editor must expose a Logo image field. In-memory DB (no prod contact).
public class AdminHeroLogoFieldTests : IClassFixture<AdminInMemoryWebApplicationFactory>
{
    private readonly AdminInMemoryWebApplicationFactory _factory;
    public AdminHeroLogoFieldTests(AdminInMemoryWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task EditForm_HasLogoImagePathField()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var resp = await client.GetAsync("/Admin/HomeContent/Create");
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("name=\"LogoImagePath\"", html);
    }
}
