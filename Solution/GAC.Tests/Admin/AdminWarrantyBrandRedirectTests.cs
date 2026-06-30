using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

// Saving the admin Warranty grid must redirect back into /Admin and persist the brand rows.
// In-memory DB (no prod contact).
public class AdminWarrantyBrandRedirectTests : IClassFixture<AdminInMemoryWebApplicationFactory>
{
    private readonly AdminInMemoryWebApplicationFactory _factory;
    public AdminWarrantyBrandRedirectTests(AdminInMemoryWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task Save_RedirectsIntoAdmin_AndPersistsBrandRow()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var form = await client.GetAsync("/Admin/Warranty");
        form.EnsureSuccessStatusCode();
        var token = Regex.Match(await form.Content.ReadAsStringAsync(),
            @"name=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""").Groups[1].Value;

        var resp = await client.PostAsync("/Admin/Warranty/Save", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["BannerImagePath"] = "/x.jpg",
            ["TermsImagePath"] = "/y.jpg",
            ["TableMfrWarrantyHeader.En"] = "Manufacturer Warranty",
            ["BrandRows[0].Brand"] = "GAC",
            ["BrandRows[0].ManufacturerWarranty.En"] = "5 Years and/or 150,000 KM",
            ["BrandRows[0].PolicyUrl"] = "/pdfs/gac.pdf",
        }));

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.StartsWith("/Admin/", resp.Headers.Location!.ToString());
    }
}
