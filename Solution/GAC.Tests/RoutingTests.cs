using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GAC.Tests;

public class RoutingTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public RoutingTests(DevWebApplicationFactory factory) => _factory = factory;

    private System.Net.Http.HttpClient NoRedirect() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/about.html", "/about")]
    [InlineData("/book-a-service.html", "/book-a-service")]
    [InlineData("/index.html", "/")]
    [InlineData("/contact.html", "/contact-us")]
    public async Task LegacyHtml_Redirects301(string from, string to)
    {
        var res = await NoRedirect().GetAsync(from);
        Assert.Equal(HttpStatusCode.MovedPermanently, res.StatusCode);
        Assert.Equal(to, res.Headers.Location!.ToString());
    }

    [Fact] public async Task About_Renders() => await Ok("/about", "About");
    [Fact] public async Task BookService_Renders() => await Ok("/book-a-service", "");
    [Fact] public async Task Gs8_Renders() => await Ok("/gs8", "GS8");

    [Fact]
    public async Task UnknownSlug_Returns404()
    {
        var res = await _factory.CreateClient().GetAsync("/does-not-exist-xyz");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private async Task Ok(string url, string contains)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        if (contains.Length > 0)
            Assert.Contains(contains, await res.Content.ReadAsStringAsync());
    }
}
