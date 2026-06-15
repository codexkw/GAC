using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class VehicleContentTests
{
    [Fact]
    public void FeatureSection_DefaultLayout_IsImageLeft()
    {
        Assert.Equal(FeatureLayout.ImageLeft, new FeatureSection().Layout);
    }
}
