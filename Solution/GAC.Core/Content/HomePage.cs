namespace GAC.Core.Content;

public class HomePage
{
    public int Id { get; set; }
    public List<HeroSlide> Slides { get; set; } = new();
    public PromoSection? Promo { get; set; }
    public List<DualCard> DualCards { get; set; } = new();
}
