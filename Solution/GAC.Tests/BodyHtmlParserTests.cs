using System.Reflection;
using AngleSharp.Dom;
using GAC.Infrastructure.Content;
using Xunit;

namespace GAC.Tests;

public class BodyHtmlParserTests
{
    // Loads the embedded emkoo seed-body fixture (resource name = "GAC.Tests.Fixtures.emkoo.html").
    internal static string LoadFixture()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .Single(n => n.EndsWith("Fixtures.emkoo.html", StringComparison.Ordinal));
        using var s = asm.GetManifestResourceStream(name)!;
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }

    private static IDocument Doc() => BodyHtmlParser.ParseHtml(LoadFixture());

    [Fact]
    public void Fixture_LoadsNonEmptyHtml()
    {
        var html = LoadFixture();
        Assert.Contains("mp-hero__title", html);
        Assert.Contains("data-tab-btn", html);
    }

    [Fact]
    public void ParseHtml_ReturnsDocument_WithExpectedRootSections()
    {
        var doc = Doc();
        var ids = doc.QuerySelectorAll("section.mp-section[id]")
                     .Select(e => e.Id).ToList();
        Assert.Contains("exterior", ids);
        Assert.Contains("design", ids);
        Assert.Contains("gallery", ids);
        Assert.Contains("performance", ids);
        Assert.Contains("trims", ids);
        Assert.Contains("warranty", ids);
    }
}
