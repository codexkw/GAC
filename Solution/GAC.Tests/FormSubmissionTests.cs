using System.Net;
using Xunit;

namespace GAC.Tests;

public class FormSubmissionTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public FormSubmissionTests(DevWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/book-a-service")]
    [InlineData("/book-a-test-drive")]
    [InlineData("/request-a-quote")]
    [InlineData("/fleet")]
    [InlineData("/recall-enquiry")]
    public async Task FormPage_RendersPostForm_WithAntiForgeryToken(string url)
    {
        var html = await (await _factory.CreateClient().GetAsync(url)).Content.ReadAsStringAsync();
        Assert.Contains("method=\"post\"", html);
        Assert.Contains("action=\"/forms/", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task FormPage_Arabic_ShowsLocalizedErrorText()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c%3Dar%7Cuic%3Dar");
        var raw = await (await client.GetAsync("/recall-enquiry")).Content.ReadAsStringAsync();
        var html = WebUtility.HtmlDecode(raw); // Razor encodes Arabic to numeric refs
        Assert.Contains("الرجاء إدخال الاسم الأول.", html); // "Please enter your first name." in AR
    }

    [Fact]
    public async Task ContactUs_StillRenders200()
    {
        var res = await _factory.CreateClient().GetAsync("/contact-us");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
