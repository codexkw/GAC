using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Html;
using System.IO;
using System.Text.Encodings.Web;
using Xunit;

namespace GAC.Tests;

public class DockIconsTests
{
    private static string Html(IHtmlContent c)
    {
        using var sw = new StringWriter();
        c.WriteTo(sw, HtmlEncoder.Default);
        return sw.ToString();
    }

    [Fact]
    public void Known_Key_Returns_Svg()
    {
        Assert.Contains("whatsapp", DockIcons.Keys);
        Assert.Contains("<svg", Html(DockIcons.Render("whatsapp")));
    }

    [Fact]
    public void Unknown_Key_Returns_Default_Svg()
    {
        Assert.Contains("<svg", Html(DockIcons.Render("nope")));
        Assert.Contains("<svg", Html(DockIcons.Render(null)));
    }
}
