using System.ComponentModel.DataAnnotations;

namespace GAC.Web.Areas.Admin.Models;

public class UserRow
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "";
    public bool Disabled { get; set; }
}

public class CreateUserViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Editor";
}

public class EditUserViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    [Required] public string Role { get; set; } = "Editor";
    public bool Disabled { get; set; }
    [MinLength(8)] public string? NewPassword { get; set; }  // optional reset
}
