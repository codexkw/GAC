using GAC.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GAC.Tests;

public class CultureControllerTests
{
    [Fact]
    public void Set_WritesCultureCookie_AndRedirectsToReturnUrl()
    {
        var controller = new CultureController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Set("ar", "/gs8");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/gs8", redirect.Url);

        var setCookie = controller.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains(".AspNetCore.Culture", setCookie);
        Assert.Contains("c%3Dar", setCookie); // CookieRequestCultureProvider encodes "c=ar|uic=ar"
    }

    [Fact]
    public void Set_RejectsUnsupportedCulture_FallsBackToEnglish()
    {
        var controller = new CultureController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        controller.Set("fr", "/");

        var setCookie = controller.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("c%3Den", setCookie);
    }

    [Fact]
    public void Set_RejectsNonLocalReturnUrl_RedirectsHome()
    {
        var controller = new CultureController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Set("en", "https://evil.example.com");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }
}
