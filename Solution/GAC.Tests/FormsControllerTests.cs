using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web;
using GAC.Web.Controllers;
using GAC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GAC.Tests;

public class FormsControllerTests
{
    // --- fakes ---
    private sealed class FakeContent : IContentService
    {
        public FormPage? Form;
        public Task<FormPage?> GetFormPageBySlugAsync(string slug) => Task.FromResult(Form);
        public Task<HomePage?> GetHomePageAsync() => Task.FromResult<HomePage?>(null);
        public Task<WarrantyPage?> GetWarrantyPageAsync() => Task.FromResult<WarrantyPage?>(null);
        public Task<ContentPage?> GetContentPageBySlugAsync(string slug) => Task.FromResult<ContentPage?>(null);
        public Task<IReadOnlyList<NewsArticle>> GetPublishedNewsAsync() => Task.FromResult<IReadOnlyList<NewsArticle>>(new List<NewsArticle>());
        public Task<NewsArticle?> GetNewsBySlugAsync(string slug) => Task.FromResult<NewsArticle?>(null);
        public Task<IReadOnlyList<Offer>> GetActiveOffersAsync() => Task.FromResult<IReadOnlyList<Offer>>(new List<Offer>());
        public Task<IReadOnlyList<ContentPage>> GetAllContentPagesAsync() => Task.FromResult<IReadOnlyList<ContentPage>>(new List<ContentPage>());
        public Task<IReadOnlyList<FormPage>> GetAllFormPagesAsync() => Task.FromResult<IReadOnlyList<FormPage>>(new List<FormPage>());
    }
    private sealed class FakeVehicles : IVehicleService
    {
        public Vehicle? BySlug;
        public Task<IReadOnlyList<Vehicle>> GetVisibleAsync() => Task.FromResult<IReadOnlyList<Vehicle>>(new List<Vehicle>());
        public Task<Vehicle?> GetBySlugAsync(string slug) => Task.FromResult(BySlug);
    }
    private sealed class FakeLeads : ILeadService
    {
        public Lead? Created;
        public Task CreateAsync(Lead lead, CancellationToken ct = default) { Created = lead; return Task.CompletedTask; }
    }
    private sealed class ThrowingEmail : IEmailSender
    {
        public bool Called;
        public Task SendLeadNotificationAsync(Lead lead, string formTitle, CancellationToken ct = default)
        { Called = true; throw new InvalidOperationException("smtp down"); }
    }
    private sealed class PassThroughLoc : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name, false);
        public LocalizedString this[string name, params object[] arguments] => new(name, name, false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => System.Array.Empty<LocalizedString>();
    }

    private static FormsController Build(FakeContent content, FakeLeads leads, IEmailSender email, FakeVehicles? vehicles = null)
    {
        var c = new FormsController(content, vehicles ?? new FakeVehicles(), leads, email, new PassThroughLoc(), NullLogger<FormsController>.Instance);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new Microsoft.AspNetCore.Http.HostString("localhost");
        c.ControllerContext = new ControllerContext { HttpContext = httpContext };
        c.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            httpContext, new FakeTempDataProvider());
        return c;
    }
    private sealed class FakeTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext c) => new Dictionary<string, object>();
        public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext c, IDictionary<string, object> v) { }
    }

    private static FormPage Page(FormType t) => new() { Slug = "x", FormType = t, IsVisible = true, Title = new LocalizedText { En = "X" } };
    private static LeadFormInput ValidCore() => new() { Title = "Mr", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "12345678" };

    [Fact]
    public async Task Submit_UnknownSlug_ReturnsNotFound()
    {
        var ctrl = Build(new FakeContent { Form = null }, new FakeLeads(), new ThrowingEmail());
        var result = await ctrl.Submit("nope", ValidCore());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Submit_Invalid_ReturnsViewWithModel()
    {
        var ctrl = Build(new FakeContent { Form = Page(FormType.Contact) }, new FakeLeads(), new ThrowingEmail());
        ctrl.ModelState.AddModelError("FirstName", "required");
        var result = await ctrl.Submit("x", new LeadFormInput());
        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<FormPageViewModel>(view.Model);
    }

    [Fact]
    public async Task Submit_ServiceBooking_MissingModelAndBranch_IsInvalid()
    {
        var ctrl = Build(new FakeContent { Form = Page(FormType.ServiceBooking) }, new FakeLeads(), new ThrowingEmail());
        var result = await ctrl.Submit("x", ValidCore()); // no Model, no Branch
        Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
        Assert.True(ctrl.ModelState.ContainsKey("Model"));
        Assert.True(ctrl.ModelState.ContainsKey("Branch"));
    }

    [Fact]
    public async Task Submit_Valid_PersistsLead_AndRedirects_EvenIfEmailThrows()
    {
        var leads = new FakeLeads();
        var email = new ThrowingEmail();
        var ctrl = Build(new FakeContent { Form = Page(FormType.TestDrive) }, leads, email);
        var input = ValidCore(); input.Model = "GS8";
        var result = await ctrl.Submit("x", input);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/x", redirect.Url);
        Assert.NotNull(leads.Created);
        Assert.Equal("Mr Ada Lovelace", leads.Created!.Name);
        Assert.Equal(FormType.TestDrive, leads.Created.FormType);
        Assert.True(email.Called); // email attempted but its exception did not bubble
    }

    [Fact]
    public async Task SubmitEnquiry_UnknownSlug_ReturnsNotFound()
    {
        var ctrl = Build(new FakeContent(), new FakeLeads(), new ThrowingEmail(), new FakeVehicles { BySlug = null });
        var result = await ctrl.SubmitEnquiry("nope", ValidCore());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SubmitEnquiry_Valid_PersistsQuoteLead_WithVehicleAndSource_AndRedirects_EvenIfEmailThrows()
    {
        var leads = new FakeLeads();
        var email = new ThrowingEmail();
        var vehicle = new Vehicle { Id = 7, Slug = "emkoo", Name = new LocalizedText { En = "EMKOO" } };
        var ctrl = Build(new FakeContent(), leads, email, new FakeVehicles { BySlug = vehicle });
        var input = ValidCore(); input.Branch = "Riyadh Branch"; input.Message = "Interested";

        var result = await ctrl.SubmitEnquiry("emkoo", input);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/emkoo#enquiry", redirect.Url);
        Assert.NotNull(leads.Created);
        Assert.Equal(FormType.Quote, leads.Created!.FormType);
        Assert.Equal(7, leads.Created.VehicleId);
        Assert.Equal("/emkoo", leads.Created.SourcePage);
        Assert.Equal("Mr Ada Lovelace", leads.Created.Name);
        Assert.Equal("Riyadh Branch", leads.Created.Branch);
        Assert.True(email.Called); // attempted; its exception did not bubble
    }

    [Fact]
    public async Task SubmitEnquiry_Invalid_RedirectsBack_WithoutLead()
    {
        var leads = new FakeLeads();
        var vehicle = new Vehicle { Id = 1, Slug = "emkoo", Name = new LocalizedText { En = "EMKOO" } };
        var ctrl = Build(new FakeContent(), leads, new ThrowingEmail(), new FakeVehicles { BySlug = vehicle });
        ctrl.ModelState.AddModelError("Email", "required");

        var result = await ctrl.SubmitEnquiry("emkoo", new LeadFormInput());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/emkoo#enquiry", redirect.Url);
        Assert.Null(leads.Created);
    }
}
