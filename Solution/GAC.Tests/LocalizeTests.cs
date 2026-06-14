using System.Globalization;
using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class LocalizeTests
{
    private static void With(string culture, Action body)
    {
        var prev = CultureInfo.CurrentUICulture;
        try { CultureInfo.CurrentUICulture = new CultureInfo(culture); body(); }
        finally { CultureInfo.CurrentUICulture = prev; }
    }

    [Fact] public void Localize_ReturnsArabic_WhenCultureIsAr()
    { var t = new LocalizedText { En = "Hello", Ar = "مرحبا" }; With("ar", () => Assert.Equal("مرحبا", t.Localize())); }

    [Fact] public void Localize_ReturnsEnglish_WhenCultureIsEn()
    { var t = new LocalizedText { En = "Hello", Ar = "مرحبا" }; With("en", () => Assert.Equal("Hello", t.Localize())); }

    [Fact] public void Localize_NullSafe_ReturnsEmpty()
    { LocalizedText? t = null; With("en", () => Assert.Equal(string.Empty, t.Localize())); }
}
