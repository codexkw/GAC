namespace GAC.Core.Content;

public class FormPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public FormType FormType { get; set; }
    public bool IsVisible { get; set; } = true;
    public LocalizedText Title { get; set; } = new();
    public LocalizedText IntroText { get; set; } = new();
    public LocalizedText BodyHtml { get; set; } = new();
    public LocalizedText MetaTitle { get; set; } = new();
    public LocalizedText MetaDescription { get; set; } = new();
}
