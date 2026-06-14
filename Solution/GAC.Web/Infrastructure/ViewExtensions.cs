using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GAC.Web.Infrastructure;

public static class ViewExtensions
{
    /// <summary>Returns " error" (note leading space) when the given ModelState key is invalid, else "".
    /// Used to add the .error class to a .field wrapper so the existing .err span shows.</summary>
    public static string FieldErrorClass(this IHtmlHelper html, string key)
        => html.ViewData.ModelState.GetFieldValidationState(key) == ModelValidationState.Invalid ? " error" : "";
}
