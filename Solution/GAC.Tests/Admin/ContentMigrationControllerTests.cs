using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GAC.Tests.Admin;

public class ContentMigrationControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public ContentMigrationControllerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task RunAll_WithoutAuth_IsNotOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var res = await client.PostAsync("/Admin/ContentMigration/RunAll", new StringContent(""));
        // Unauthenticated admin POST -> redirect to login or 401/403, never 200.
        Assert.NotEqual(HttpStatusCode.OK, res.StatusCode);
    }
}
