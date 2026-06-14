using System.Globalization;
using GAC.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Xunit;

namespace GAC.Tests;

public class SharedLocalizerTests
{
    private static IStringLocalizer<SharedResource> BuildLocalizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(o => o.ResourcesPath = "Resources");
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResource>>();
    }

    [Fact]
    public void Resolves_ArabicValue_WhenCultureIsArabic()
    {
        var loc = BuildLocalizer();
        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            Assert.Equal("واتساب", loc["WhatsApp"].Value);
            Assert.Equal("الكل", loc["All"].Value);
            Assert.Equal("اتصل 123", loc["Call {0}", "123"].Value);
        }
        finally { CultureInfo.CurrentUICulture = prev; }
    }

    [Fact]
    public void Resolves_ValidationStrings_InArabic()
    {
        var loc = BuildLocalizer();
        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            string[] keys =
            {
                "Please enter your first name.",
                "Please select a branch.",
                "Thanks — we received your request."
            };
            foreach (var key in keys)
            {
                var value = loc[key].Value;
                Assert.False(string.IsNullOrEmpty(value));
                Assert.NotEqual(key, value);
            }
        }
        finally { CultureInfo.CurrentUICulture = prev; }
    }

    [Fact]
    public void FallsBackToEnglishKey_WhenCultureIsEnglish()
    {
        var loc = BuildLocalizer();
        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            // No English resx: missing key returns the key verbatim (= English source text).
            Assert.Equal("WhatsApp", loc["WhatsApp"].Value);
        }
        finally { CultureInfo.CurrentUICulture = prev; }
    }
}
