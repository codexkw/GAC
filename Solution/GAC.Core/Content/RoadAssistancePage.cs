namespace GAC.Core.Content;

// Singleton aggregate backing the structured, admin-editable /road-assistance page.
public class RoadAssistancePage
{
    public int Id { get; set; }
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Intro { get; set; } = new();            // multiline → paragraphs (split on '\n')
    public LocalizedText ContactLead { get; set; } = new();      // bold "Getting In Touch" lead
    public LocalizedText ContactText { get; set; } = new();      // instruction paragraph
    public string PhoneNumber { get; set; } = "";                // drives the tel: link
    public LocalizedText CallButtonLabel { get; set; } = new();  // visible call-button text
}
