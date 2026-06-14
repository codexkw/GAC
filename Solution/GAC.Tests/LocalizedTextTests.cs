using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class LocalizedTextTests
{
    [Fact]
    public void Get_ReturnsArabic_ForArCulture()
    {
        var t = new LocalizedText { En = "Home", Ar = "الرئيسية" };
        Assert.Equal("الرئيسية", t.Get("ar"));
    }

    [Fact]
    public void Get_ReturnsEnglish_ForEnCulture()
    {
        var t = new LocalizedText { En = "Home", Ar = "الرئيسية" };
        Assert.Equal("Home", t.Get("en"));
    }

    [Fact]
    public void Get_FallsBackToEnglish_WhenArabicMissing()
    {
        var t = new LocalizedText { En = "Warranty", Ar = null };
        Assert.Equal("Warranty", t.Get("ar"));
    }

    [Fact]
    public void Get_FallsBackToArabic_WhenEnglishMissing()
    {
        var t = new LocalizedText { En = null, Ar = "عربي" };
        Assert.Equal("عربي", t.Get("en"));
    }

    [Fact]
    public void Get_ReturnsEmptyString_WhenBothNull()
    {
        var t = new LocalizedText();
        Assert.Equal(string.Empty, t.Get("en"));
    }

    [Fact]
    public void ImplicitFromString_SetsEnglish()
    {
        LocalizedText t = "Hello";
        Assert.Equal("Hello", t.En);
        Assert.Null(t.Ar);
    }
}
