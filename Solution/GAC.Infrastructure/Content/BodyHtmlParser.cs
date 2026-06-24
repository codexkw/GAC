using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using GAC.Core.Content;

namespace GAC.Infrastructure.Content;

/// <summary>Stateless parser: reads ONE language of a vehicle's BodyHtml (an AngleSharp
/// IDocument) and returns plain entities. Re-used for EN and AR by passing each language's doc.
/// Preserves InnerHtml for fields that may hold &lt;strong&gt;/&lt;br&gt;/&lt;a&gt;.</summary>
public static class BodyHtmlParser
{
    private static readonly IBrowsingContext Ctx =
        BrowsingContext.New(Configuration.Default);

    // section[id] -> SectionKey
    private static readonly Dictionary<string, SectionKey> SectionKeys = new()
    {
        ["exterior"]    = SectionKey.Overview,
        ["design"]      = SectionKey.Design,
        ["gallery"]     = SectionKey.Gallery,
        ["technology"]  = SectionKey.Technology,
        ["performance"] = SectionKey.Performance,
        ["safety"]      = SectionKey.Safety,
        ["trims"]       = SectionKey.Trims,
        ["warranty"]    = SectionKey.Warranty,
    };

    public static IDocument ParseHtml(string? html)
        => Ctx.OpenAsync(req => req.Content(html ?? string.Empty))
              .GetAwaiter().GetResult();

    // ---- helpers ----
    private static string? Txt(IElement? el) => el is null ? null : el.TextContent.Trim();
    private static string? Inner(IElement? el) => el is null ? null : el.InnerHtml.Trim();
    private static string? Attr(IElement? el, string name) => el?.GetAttribute(name)?.Trim();
    private static LocalizedText LtEn(string? en) => new() { En = en };

    // ---- 1. Hero ----
    public static (string? Img, string? Title, string? Sub) ParseHero(IDocument d)
    {
        var img = d.QuerySelector(".mp-hero__img");
        return (
            Attr(img, "src"),
            Txt(d.QuerySelector(".mp-hero__title")),
            Txt(d.QuerySelector(".mp-hero__sub")));
    }

    // ---- 2. Section headings ----
    public static List<SectionHeading> ParseHeadings(IDocument d)
    {
        var list = new List<SectionHeading>();
        foreach (var sec in d.QuerySelectorAll("section.mp-section[id]"))
        {
            if (!SectionKeys.TryGetValue(sec.Id ?? "", out var key)) continue;
            var head = sec.QuerySelector(".mp-head");
            if (head is null) continue;
            list.Add(new SectionHeading
            {
                Key = key,
                Title = LtEn(Txt(head.QuerySelector(".mp-head__title"))),
                Sub   = LtEn(Inner(head.QuerySelector(".mp-head__sub"))),
                Body  = LtEn(Inner(head.QuerySelector(".mp-head__body"))),
            });
        }
        return list;
    }

    // ---- 3. Stats + note ----
    public static List<StatItem> ParseStats(IDocument d)
    {
        var stats = new List<StatItem>();
        var i = 0;
        foreach (var st in d.QuerySelectorAll("section#exterior .mp-stat"))
        {
            stats.Add(new StatItem
            {
                Label = LtEn(Txt(st.QuerySelector(".mp-stat__label"))),
                Value = LtEn(Txt(st.QuerySelector(".mp-stat__value"))),
                SortOrder = i++,
            });
        }
        return stats;
    }

    public static string? ParseStatsNote(IDocument d)
        => Txt(d.QuerySelector("section#exterior .mp-note")) ?? Txt(d.QuerySelector(".mp-note"));

    // ---- 4. Sliders ----
    public static List<SliderGroup> ParseSliders(IDocument d)
    {
        var groups = new List<SliderGroup>();
        var gi = 0;
        foreach (var sl in d.QuerySelectorAll(".mp-slider-wrap .mp-slider"))
        {
            var group = new SliderGroup
            {
                Eyebrow = LtEn(Txt(sl.QuerySelector(".mp-slider__eyebrow"))),
                Title   = LtEn(Txt(sl.QuerySelector(".mp-slider__title"))),
                SortOrder = gi++,
            };
            var si = 0;
            foreach (var slide in sl.QuerySelectorAll(".mp-slide"))
            {
                var img = slide.QuerySelector("img");
                group.Slides.Add(new SliderSlide
                {
                    ImagePath = Attr(img, "src"),
                    Alt = LtEn(Attr(img, "alt")),
                    SortOrder = si++,
                });
            }
            groups.Add(group);
        }
        return groups;
    }

    // ---- 5/9. Feature tabs (Design d1..d3, Performance p1..p3) ----
    public static List<FeatureSection> ParseFeatures(IDocument d)
    {
        var result = new List<FeatureSection>();
        ParseFeatureGroup(d, "section#design", FeatureGroup.Design, result);
        ParseFeatureGroup(d, "section#performance", FeatureGroup.Performance, result);
        return result;
    }

    private static void ParseFeatureGroup(IDocument d, string sectionSel, FeatureGroup grp, List<FeatureSection> sink)
    {
        var section = d.QuerySelector(sectionSel);
        if (section is null) return;
        var order = 0;
        foreach (var btn in section.QuerySelectorAll(".mp-tabs .mp-tabs__btn[data-tab-btn]"))
        {
            var key = Attr(btn, "data-tab-btn");
            var panel = section.QuerySelector($".mp-feature[data-tab-panel=\"{key}\"]");
            if (panel is null) continue;
            var media = panel.QuerySelector(".mp-feature__media img");
            var bodyEl = panel.QuerySelector(".mp-feature__body");
            var feat = new FeatureSection
            {
                GroupKey = grp,
                TabLabel = LtEn(Txt(btn)),
                ImagePath = Attr(media, "src"),
                Heading = LtEn(Txt(panel.QuerySelector(".mp-feature__title"))),
                Lead = LtEn(Inner(bodyEl?.QuerySelector("p"))),
                SortOrder = order++,
            };
            var bi = 0;
            foreach (var li in panel.QuerySelectorAll(".mp-feature__list li"))
            {
                var strong = li.QuerySelector("strong");
                string label, text;
                if (strong is not null)
                {
                    label = strong.TextContent.Trim().TrimEnd(':').Trim();
                    // text = everything after the <strong> label
                    var full = li.InnerHtml;
                    var idx = full.IndexOf("</strong>", StringComparison.OrdinalIgnoreCase);
                    text = idx >= 0 ? full[(idx + "</strong>".Length)..].Trim() : li.TextContent.Trim();
                }
                else { label = ""; text = li.InnerHtml.Trim(); }
                feat.Bullets.Add(new FeatureBullet
                {
                    Label = LtEn(label),
                    Text = LtEn(text),
                    SortOrder = bi++,
                });
            }
            sink.Add(feat);
        }
    }

    // ---- 6. Gallery tabs (gex/gin/gte) ----
    public static List<GalleryTab> ParseGalleryTabs(IDocument d)
    {
        var section = d.QuerySelector("section#gallery");
        var tabs = new List<GalleryTab>();
        if (section is null) return tabs;
        var ti = 0;
        foreach (var btn in section.QuerySelectorAll(".mp-tabs .mp-tabs__btn[data-tab-btn]"))
        {
            var key = Attr(btn, "data-tab-btn");
            var panel = section.QuerySelector($".mp-gpanel[data-tab-panel=\"{key}\"]");
            if (panel is null) continue;
            var tab = new GalleryTab { Label = LtEn(Txt(btn)), SortOrder = ti++ };
            var ii = 0;
            foreach (var a in panel.QuerySelectorAll(".mp-gshot[href]"))
            {
                var img = a.QuerySelector("img");
                tab.Images.Add(new GalleryImage
                {
                    ImagePath = Attr(a, "href") ?? Attr(img, "src"),
                    Alt = LtEn(Attr(img, "alt")),
                    SortOrder = ii++,
                });
            }
            tabs.Add(tab);
        }
        return tabs;
    }

    // ---- 7. Quality ----
    public static QualityBlock? ParseQuality(IDocument d)
    {
        var section = d.QuerySelector("section#quality");
        if (section is null) return null;
        return new QualityBlock
        {
            MainImage  = Attr(section.QuerySelector(".mp-quality__main img"), "src"),
            ThumbImage = Attr(section.QuerySelector(".mp-quality__thumb img"), "src"),
            Strapline  = LtEn(Inner(section.QuerySelector(".mp-quality__strapline"))),
            Content    = LtEn(Inner(section.QuerySelector(".mp-quality__content"))),
        };
    }

    // ---- 8. Technology (banner + cards) ----
    public static (string? Banner, List<CardItem> Cards) ParseTechnology(IDocument d)
    {
        var section = d.QuerySelector("section#technology");
        var cards = new List<CardItem>();
        if (section is null) return (null, cards);
        var banner = Attr(section.QuerySelector(".mp-tech-banner img"), "src");
        var ci = 0;
        foreach (var card in section.QuerySelectorAll(".mp-card"))
        {
            cards.Add(new CardItem
            {
                ImagePath = Attr(card.QuerySelector(".mp-card__media img"), "src"),
                Title = LtEn(Txt(card.QuerySelector(".mp-card__title"))),
                Text  = LtEn(Inner(card.QuerySelector(".mp-card__text"))),
                SortOrder = ci++,
            });
        }
        return (banner, cards);
    }

    // ---- 10. Safety toggles ----
    public static List<SafetyToggle> ParseSafety(IDocument d)
    {
        var section = d.QuerySelector("section#safety");
        var list = new List<SafetyToggle>();
        if (section is null) return list;
        var i = 0;
        foreach (var tog in section.QuerySelectorAll(".mp-stoggle"))
        {
            list.Add(new SafetyToggle
            {
                Title = LtEn(Txt(tog.QuerySelector(".mp-stoggle__head span"))),
                ImagePath = Attr(tog.QuerySelector(".mp-stoggle__media img"), "src"),
                Strap = LtEn(Inner(tog.QuerySelector(".mp-stoggle__strap"))),
                Content = LtEn(Inner(tog.QuerySelector(".mp-stoggle__content"))),
                SortOrder = i++,
            });
        }
        return list;
    }

    // ---- 11. Trims ----
    public static List<Trim> ParseTrims(IDocument d)
    {
        var section = d.QuerySelector("section#trims");
        var list = new List<Trim>();
        if (section is null) return list;
        var ti = 0;
        foreach (var tr in section.QuerySelectorAll(".mp-trim"))
        {
            var trim = new Trim
            {
                ImagePath = Attr(tr.QuerySelector(".mp-trim__media img"), "src"),
                ModelLabel = LtEn(Txt(tr.QuerySelector(".mp-trim__model"))),
                Name = LtEn(Txt(tr.QuerySelector(".mp-trim__name"))),
                SortOrder = ti++,
            };
            var pi = 0;
            foreach (var li in tr.QuerySelectorAll(".mp-trim__price li"))
            {
                trim.PriceRows.Add(new TrimPriceRow { Text = LtEn(Inner(li)), SortOrder = pi++ });
            }
            // 1st CTA is the static #enquiry button; 2nd a[href] is the spec PDF.
            var ctaLinks = tr.QuerySelectorAll(".mp-trim__cta a[href]").ToList();
            var pdf = ctaLinks.Select(a => Attr(a, "href"))
                              .FirstOrDefault(h => !string.IsNullOrWhiteSpace(h) && !h!.StartsWith("#"));
            trim.SpecPdf = pdf;
            list.Add(trim);
        }
        return list;
    }

    // ---- 12. Warranty ----
    public static List<WarrantyLink> ParseWarranty(IDocument d)
    {
        var list = new List<WarrantyLink>();
        var i = 0;
        foreach (var a in d.QuerySelectorAll("section#warranty .mp-warranty__links a.btn--doc"))
        {
            list.Add(new WarrantyLink
            {
                Label = LtEn(Txt(a)),
                Url = Attr(a, "href") ?? string.Empty,
                SortOrder = i++,
            });
        }
        return list;
    }

    // ---- 13. Enquiry ----
    public static (string? Bg, string? Title, string? Sub, string? Lead) ParseEnquiry(IDocument d)
    {
        var section = d.QuerySelector(".mp-enquiry");
        if (section is null) return (null, null, null, null);
        string? bg = null;
        var style = Attr(section, "style");
        if (!string.IsNullOrEmpty(style))
        {
            var m = Regex.Match(style, @"url\(\s*['""]?([^'""\)]+)['""]?\s*\)");
            if (m.Success) bg = m.Groups[1].Value.Trim();
        }
        return (
            bg,
            Txt(section.QuerySelector(".mp-enquiry__title")),
            Txt(section.QuerySelector(".mp-enquiry__sub")),
            Txt(section.QuerySelector(".mp-enquiry__lead")));
    }

    /// <summary>Convenience: run every section parser over one language's doc.</summary>
    public static ParsedVehicleContent ParseAll(IDocument d)
    {
        var (hImg, hTitle, hSub) = ParseHero(d);
        var (banner, cards) = ParseTechnology(d);
        var (bg, eTitle, eSub, eLead) = ParseEnquiry(d);
        return new ParsedVehicleContent
        {
            HeroImage = hImg, HeroTitle = hTitle, HeroSub = hSub,
            Headings = ParseHeadings(d),
            Stats = ParseStats(d),
            StatsNote = ParseStatsNote(d),
            Sliders = ParseSliders(d),
            Features = ParseFeatures(d),
            GalleryTabs = ParseGalleryTabs(d),
            Quality = ParseQuality(d),
            TechBanner = banner,
            Cards = cards,
            Safety = ParseSafety(d),
            Trims = ParseTrims(d),
            Warranty = ParseWarranty(d),
            EnquiryBg = bg, EnquiryTitle = eTitle, EnquirySub = eSub, EnquiryLead = eLead,
        };
    }
}
