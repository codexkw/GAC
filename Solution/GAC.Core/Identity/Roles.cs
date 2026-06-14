namespace GAC.Core.Identity;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string Sales = "Sales";

    public static readonly string[] All = { Admin, Editor, Sales };
}
