using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class VehicleRichEntitiesTests
{
    [Fact]
    public void SectionKey_HasExpectedMembers()
    {
        Assert.Equal(0, (int)SectionKey.Overview);
        Assert.Equal(7, (int)SectionKey.Warranty);
        Assert.Equal(8, System.Enum.GetValues(typeof(SectionKey)).Length);
    }

    [Fact]
    public void FeatureGroup_HasDesignAndPerformance()
    {
        Assert.Equal(0, (int)FeatureGroup.Design);
        Assert.Equal(1, (int)FeatureGroup.Performance);
    }

    [Fact]
    public void SectionHeading_DefaultsLocalizedTextNonNull()
    {
        var h = new SectionHeading { VehicleId = 1, Key = SectionKey.Overview };
        Assert.NotNull(h.Title);
        Assert.NotNull(h.Sub);
        Assert.NotNull(h.Body);
    }

    [Fact]
    public void StatItem_IsOrderable_WithLocalizedFields()
    {
        IOrderable s = new StatItem { VehicleId = 1, SortOrder = 3 };
        Assert.Equal(3, s.SortOrder);
        var stat = (StatItem)s;
        Assert.NotNull(stat.Label);
        Assert.NotNull(stat.Value);
    }
}
