using System.Net;
using Xunit;

namespace GAC.Tests;

public class FormPagesTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public FormPagesTests(DevWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/book-a-test-drive")]
    [InlineData("/request-a-quote")]
    [InlineData("/contact-us")]
    [InlineData("/fleet")]
    [InlineData("/recall-enquiry")]
    public async Task FormPages_Render200(string url)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task ContactUs_RendersDirectoryBody_FromDb()
    {
        var res = await _factory.CreateClient().GetAsync("/contact-us");
        Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("dir-grid", html);
        Assert.DoesNotContain("data-form", html);
    }
}
