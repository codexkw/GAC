using GAC.Core.Content;

namespace GAC.Web.Models;

public class HomeViewModel
{
    public HomePage? Home { get; set; }
    public IReadOnlyList<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public IReadOnlyList<NewsArticle> News { get; set; } = new List<NewsArticle>();
}
