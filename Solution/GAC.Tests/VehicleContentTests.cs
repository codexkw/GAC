using System.Linq;
using GAC.Core.Content;
using GAC.Web.Infrastructure;
using Xunit;

namespace GAC.Tests;

public class VehicleContentTests
{
    [Fact]
    public void FeatureSection_DefaultLayout_IsImageLeft()
    {
        Assert.Equal(FeatureLayout.ImageLeft, new FeatureSection().Layout);
    }

    [Fact]
    public void HasStructuredContent_FalseWhenAllEmpty()
    {
        Assert.False(VehicleContent.HasStructuredContent(new Vehicle()));
    }

    [Fact]
    public void HasStructuredContent_TrueWhenAnyFeature()
    {
        var v = new Vehicle();
        v.Features.Add(new FeatureSection());
        Assert.True(VehicleContent.HasStructuredContent(v));
    }

    [Fact]
    public void HasStructuredContent_TrueWhenAnyTrimSpecOrColor()
    {
        var withTrim = new Vehicle(); withTrim.Trims.Add(new Trim());
        var withSpec = new Vehicle(); withSpec.SpecGroups.Add(new SpecGroup());
        var withColor = new Vehicle(); withColor.Colors.Add(new ColorOption());
        Assert.True(VehicleContent.HasStructuredContent(withTrim));
        Assert.True(VehicleContent.HasStructuredContent(withSpec));
        Assert.True(VehicleContent.HasStructuredContent(withColor));
    }

    [Theory]
    [InlineData(FeatureLayout.ImageLeft, "mp-feature")]
    [InlineData(FeatureLayout.ImageRight, "mp-feature mp-feature--reverse")]
    [InlineData(FeatureLayout.Banner, "mp-feature mp-feature--banner")]
    [InlineData(FeatureLayout.TextOnly, "mp-feature mp-feature--text")]
    public void FeatureLayoutCss_MapsEachVariant(FeatureLayout layout, string expected)
    {
        Assert.Equal(expected, VehicleContent.FeatureLayoutCss(layout));
    }

    [Fact]
    public void ShowsImage_FalseForTextOnly()
    {
        Assert.False(VehicleContent.ShowsImage(FeatureLayout.TextOnly));
        Assert.True(VehicleContent.ShowsImage(FeatureLayout.ImageLeft));
    }

    [Fact]
    public void TabKey_BuildsOneBasedSuffix()
    {
        Assert.Equal("d1", VehicleContent.TabKey("d", 0));
        Assert.Equal("p3", VehicleContent.TabKey("p", 2));
        Assert.Equal("g2", VehicleContent.TabKey("g", 1));
    }

    [Fact]
    public void DesignFeatures_FiltersAndOrders()
    {
        var v = new Vehicle();
        v.Features.Add(new FeatureSection { GroupKey = FeatureGroup.Performance, SortOrder = 0 });
        v.Features.Add(new FeatureSection { GroupKey = FeatureGroup.Design, SortOrder = 2 });
        v.Features.Add(new FeatureSection { GroupKey = FeatureGroup.Design, SortOrder = 1 });

        var design = VehicleContent.DesignFeatures(v).ToList();
        var perf = VehicleContent.PerformanceFeatures(v).ToList();

        Assert.Equal(2, design.Count);
        Assert.Equal(1, design[0].SortOrder);
        Assert.Equal(2, design[1].SortOrder);
        Assert.Single(perf);
    }

    [Fact]
    public void StateHelpers_OnlyFirstIsActiveOrOpen()
    {
        Assert.Equal(" is-active", VehicleContent.StateActive(true));
        Assert.Equal("", VehicleContent.StateActive(false));
        Assert.Equal(" is-open", VehicleContent.StateOpen(true));
        Assert.Equal("", VehicleContent.StateOpen(false));
        Assert.Equal("true", VehicleContent.AriaExpanded(true));
        Assert.Equal("false", VehicleContent.AriaExpanded(false));
    }
}
