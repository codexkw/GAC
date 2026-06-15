namespace GAC.Web.Areas.Admin.Models;

public class LocalizedFieldModel
{
    public string Label { get; set; } = "";
    public string NameEn { get; set; } = "";   // form field name, e.g. "Name.En"
    public string NameAr { get; set; } = "";   // form field name, e.g. "Name.Ar"
    public string? ValueEn { get; set; }
    public string? ValueAr { get; set; }
    public bool Multiline { get; set; }
    public bool Code { get; set; }              // monospace raw-HTML editor (Phase 6b)
}
