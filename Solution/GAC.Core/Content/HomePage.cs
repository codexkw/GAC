namespace GAC.Core.Content;

public class HomePage
{
    public int Id { get; set; }
    public List<HeroSlide> Slides { get; set; } = new();
}
