using System.ComponentModel.DataAnnotations;

namespace GAC.Web.Models;

/// <summary>
/// Posted by all five lead-capture forms. Field names match the existing markup (case-insensitive
/// binding). Model/Branch are conditionally required per FormType in FormsController.
/// </summary>
public class LeadFormInput
{
    [Required(ErrorMessage = "Please select a title.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Please enter your first name.")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Please enter your last name.")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Please enter a valid email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Please enter your contact number.")]
    public string? Phone { get; set; }

    public string? Model { get; set; }
    public string? Branch { get; set; }
    public string? Mileage { get; set; }
    public string? DueDate { get; set; }
    public string? Message { get; set; }
    public bool Marketing { get; set; }
}
