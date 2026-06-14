using System.ComponentModel.DataAnnotations;
using GAC.Web.Models;
using Xunit;

namespace GAC.Tests;

public class LeadFormInputValidationTests
{
    private static IList<ValidationResult> Validate(LeadFormInput input)
    {
        var ctx = new ValidationContext(input);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Empty_Input_FailsRequiredCoreFields()
    {
        var results = Validate(new LeadFormInput());
        var members = results.SelectMany(r => r.MemberNames).ToHashSet();
        Assert.Contains(nameof(LeadFormInput.Title), members);
        Assert.Contains(nameof(LeadFormInput.FirstName), members);
        Assert.Contains(nameof(LeadFormInput.LastName), members);
        Assert.Contains(nameof(LeadFormInput.Email), members);
        Assert.Contains(nameof(LeadFormInput.Phone), members);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var input = new LeadFormInput
        {
            Title = "Mr", FirstName = "A", LastName = "B", Phone = "123", Email = "not-an-email"
        };
        var results = Validate(input);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(LeadFormInput.Email)));
    }

    [Fact]
    public void Valid_Core_Passes()
    {
        var input = new LeadFormInput
        {
            Title = "Mr", FirstName = "Ada", LastName = "Lovelace",
            Email = "ada@example.com", Phone = "12345678"
        };
        Assert.Empty(Validate(input));
    }
}
