using GAC.Core.Content;

namespace GAC.Web.Models;

public class FormPageViewModel
{
    public FormPage Page { get; set; } = new();
    public LeadFormInput Input { get; set; } = new();
}
