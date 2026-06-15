using GAC.Infrastructure.Services;
using Xunit;

namespace GAC.Tests;

public class HtmlSanitizerServiceTests
{
    private readonly HtmlSanitizerService _svc = new();

    [Fact]
    public void Strips_Script_Tags()
    {
        var html = "<p>hi</p><script>alert(1)</script>";
        Assert.DoesNotContain("<script", _svc.Sanitize(html));
    }

    [Fact]
    public void Strips_Event_Handlers()
    {
        var html = "<a href=\"/x\" onclick=\"steal()\">link</a>";
        var result = _svc.Sanitize(html);
        Assert.DoesNotContain("onclick", result);
        Assert.Contains("href", result);
    }

    [Fact]
    public void Keeps_Allowed_Formatting()
    {
        var html = "<div><strong>bold</strong> <em>it</em><ul><li>a</li></ul></div>";
        var result = _svc.Sanitize(html);
        Assert.Contains("<strong>", result);
        Assert.Contains("<em>", result);
        Assert.Contains("<li>", result);
    }

    [Fact]
    public void Null_Or_Empty_Returns_Empty()
    {
        Assert.Equal("", _svc.Sanitize(null));
        Assert.Equal("", _svc.Sanitize(""));
    }
}
