using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Content;

public sealed record MigratorReport(int VehiclesScanned, int VehiclesMigrated, int VehiclesSkipped, List<string> Notes);

/// <summary>One-off backfill: turns each vehicle's BodyHtml (EN + AR) into the new structured
/// collections. Idempotent — skips a car that already has structured rows unless force=true.
/// Never wired into startup; invoked on demand (see admin action / dev hook task).</summary>
public static class VehicleContentMigrator
{
    public static async Task<MigratorReport> BackfillAllAsync(ApplicationDbContext db, bool force = false, CancellationToken ct = default)
    {
        var ids = await db.Vehicles.Select(v => v.Id).ToListAsync(ct);
        int migrated = 0, skipped = 0;
        var notes = new List<string>();
        foreach (var id in ids)
        {
            var did = await BackfillVehicleAsync(db, id, force, ct);
            if (did) { migrated++; notes.Add($"#{id}: migrated"); }
            else { skipped++; notes.Add($"#{id}: skipped"); }
        }
        return new MigratorReport(ids.Count, migrated, skipped, notes);
    }

    public static async Task<bool> BackfillVehicleAsync(ApplicationDbContext db, int vehicleId, bool force = false, CancellationToken ct = default)
    {
        var v = await LoadWithCollectionsAsync(db, vehicleId, ct);
        if (v is null) return false;

        var hasStructured =
            v.Stats.Count > 0 || v.Sliders.Count > 0 || v.GalleryTabs.Count > 0 ||
            v.Cards.Count > 0 || v.SafetyToggles.Count > 0 || v.Headings.Count > 0 ||
            v.Features.Any(f => f.GroupKey != default || f.Bullets.Count > 0);

        if (hasStructured && !force) return false;          // preserve admin edits
        if (force) ClearStructured(db, v);

        var en = v.BodyHtml?.En;
        if (string.IsNullOrWhiteSpace(en)) return false;    // nothing to parse (e.g. hidden cars)

        var parsedEn = BodyHtmlParser.ParseAll(BodyHtmlParser.ParseHtml(en));
        var parsedAr = string.IsNullOrWhiteSpace(v.BodyHtml?.Ar)
            ? null
            : BodyHtmlParser.ParseAll(BodyHtmlParser.ParseHtml(v.BodyHtml!.Ar));

        Apply(db, v, parsedEn, parsedAr);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task<Vehicle?> LoadWithCollectionsAsync(ApplicationDbContext db, int id, CancellationToken ct)
        => await db.Vehicles
            .Include(v => v.Headings)
            .Include(v => v.Stats)
            .Include(v => v.Sliders).ThenInclude(s => s.Slides)
            .Include(v => v.Features).ThenInclude(f => f.Bullets)
            .Include(v => v.GalleryTabs).ThenInclude(t => t.Images)
            .Include(v => v.Cards)
            .Include(v => v.SafetyToggles)
            .Include(v => v.WarrantyLinks)
            .Include(v => v.Trims).ThenInclude(t => t.PriceRows)
            .Include(v => v.Quality)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    private static void ClearStructured(ApplicationDbContext db, Vehicle v)
    {
        db.Set<SectionHeading>().RemoveRange(v.Headings);
        db.Set<StatItem>().RemoveRange(v.Stats);
        db.Set<SliderSlide>().RemoveRange(v.Sliders.SelectMany(s => s.Slides));
        db.Set<SliderGroup>().RemoveRange(v.Sliders);
        db.Set<FeatureBullet>().RemoveRange(v.Features.SelectMany(f => f.Bullets));
        db.Set<FeatureSection>().RemoveRange(v.Features);
        db.Set<GalleryImage>().RemoveRange(v.GalleryTabs.SelectMany(t => t.Images));
        db.Set<GalleryTab>().RemoveRange(v.GalleryTabs);
        db.Set<CardItem>().RemoveRange(v.Cards);
        db.Set<SafetyToggle>().RemoveRange(v.SafetyToggles);
        db.Set<WarrantyLink>().RemoveRange(v.WarrantyLinks);
        db.Set<TrimPriceRow>().RemoveRange(v.Trims.SelectMany(t => t.PriceRows));
        db.Set<Trim>().RemoveRange(v.Trims);
        if (v.Quality is not null) db.Set<QualityBlock>().Remove(v.Quality);
    }

    // ---- EN/AR merge helpers (AR parser puts text in .En slot; copy to .Ar) ----
    private static LocalizedText Merge(LocalizedText en, LocalizedText? ar)
        => new() { En = en.En, Ar = ar?.En };

    private static void Apply(ApplicationDbContext db, Vehicle v, ParsedVehicleContent en, ParsedVehicleContent? ar)
    {
        // --- Vehicle scalar/localized fields ---
        v.TechBannerImage = en.TechBanner;
        v.EnquiryBgImage = en.EnquiryBg;
        v.StatsNote = new LocalizedText { En = en.StatsNote, Ar = ar?.StatsNote };
        v.EnquiryTitle = new LocalizedText { En = en.EnquiryTitle, Ar = ar?.EnquiryTitle };
        v.EnquirySub = new LocalizedText { En = en.EnquirySub, Ar = ar?.EnquirySub };
        v.EnquiryLead = new LocalizedText { En = en.EnquiryLead, Ar = ar?.EnquiryLead };

        // --- Headings (match AR by SectionKey) ---
        foreach (var h in en.Headings)
        {
            var a = ar?.Headings.FirstOrDefault(x => x.Key == h.Key);
            db.Set<SectionHeading>().Add(new SectionHeading
            {
                VehicleId = v.Id, Key = h.Key,
                Title = Merge(h.Title, a?.Title),
                Sub = Merge(h.Sub, a?.Sub),
                Body = Merge(h.Body, a?.Body),
            });
        }

        // --- Stats (positional) ---
        for (var i = 0; i < en.Stats.Count; i++)
        {
            var a = ar is not null && i < ar.Stats.Count ? ar.Stats[i] : null;
            db.Set<StatItem>().Add(new StatItem
            {
                VehicleId = v.Id, SortOrder = i,
                Label = Merge(en.Stats[i].Label, a?.Label),
                Value = Merge(en.Stats[i].Value, a?.Value),
            });
        }

        // --- Sliders + slides (positional) ---
        for (var gi = 0; gi < en.Sliders.Count; gi++)
        {
            var eg = en.Sliders[gi];
            var ag = ar is not null && gi < ar.Sliders.Count ? ar.Sliders[gi] : null;
            var group = new SliderGroup
            {
                VehicleId = v.Id, SortOrder = gi,
                Eyebrow = Merge(eg.Eyebrow, ag?.Eyebrow),
                Title = Merge(eg.Title, ag?.Title),
            };
            for (var si = 0; si < eg.Slides.Count; si++)
            {
                var asl = ag is not null && si < ag.Slides.Count ? ag.Slides[si] : null;
                group.Slides.Add(new SliderSlide
                {
                    SortOrder = si,
                    ImagePath = eg.Slides[si].ImagePath,
                    Alt = Merge(eg.Slides[si].Alt, asl?.Alt),
                });
            }
            db.Set<SliderGroup>().Add(group);
        }

        // --- Features + bullets (positional within whole list) ---
        for (var fi = 0; fi < en.Features.Count; fi++)
        {
            var ef = en.Features[fi];
            var af = ar is not null && fi < ar.Features.Count ? ar.Features[fi] : null;
            var feat = new FeatureSection
            {
                VehicleId = v.Id, SortOrder = ef.SortOrder, GroupKey = ef.GroupKey,
                ImagePath = ef.ImagePath,
                TabLabel = Merge(ef.TabLabel, af?.TabLabel),
                Heading = Merge(ef.Heading, af?.Heading),
                Lead = Merge(ef.Lead, af?.Lead),
                Body = new LocalizedText(),   // legacy field unused by new render
            };
            for (var bi = 0; bi < ef.Bullets.Count; bi++)
            {
                var ab = af is not null && bi < af.Bullets.Count ? af.Bullets[bi] : null;
                feat.Bullets.Add(new FeatureBullet
                {
                    SortOrder = bi,
                    Label = Merge(ef.Bullets[bi].Label, ab?.Label),
                    Text = Merge(ef.Bullets[bi].Text, ab?.Text),
                });
            }
            db.Set<FeatureSection>().Add(feat);
        }

        // --- Gallery tabs + images (positional) ---
        for (var ti = 0; ti < en.GalleryTabs.Count; ti++)
        {
            var et = en.GalleryTabs[ti];
            var at = ar is not null && ti < ar.GalleryTabs.Count ? ar.GalleryTabs[ti] : null;
            var tab = new GalleryTab { VehicleId = v.Id, SortOrder = ti, Label = Merge(et.Label, at?.Label) };
            for (var ii = 0; ii < et.Images.Count; ii++)
            {
                var ai = at is not null && ii < at.Images.Count ? at.Images[ii] : null;
                tab.Images.Add(new GalleryImage
                {
                    SortOrder = ii,
                    ImagePath = et.Images[ii].ImagePath,
                    Alt = Merge(et.Images[ii].Alt, ai?.Alt),
                });
            }
            db.Set<GalleryTab>().Add(tab);
        }

        // --- Quality ---
        if (en.Quality is not null)
        {
            db.Set<QualityBlock>().Add(new QualityBlock
            {
                VehicleId = v.Id,
                MainImage = en.Quality.MainImage, ThumbImage = en.Quality.ThumbImage,
                Strapline = new LocalizedText { En = en.Quality.Strapline.En, Ar = ar?.Quality?.Strapline.En },
                Content = new LocalizedText { En = en.Quality.Content.En, Ar = ar?.Quality?.Content.En },
            });
        }

        // --- Technology cards (positional) ---
        for (var ci = 0; ci < en.Cards.Count; ci++)
        {
            var a = ar is not null && ci < ar.Cards.Count ? ar.Cards[ci] : null;
            db.Set<CardItem>().Add(new CardItem
            {
                VehicleId = v.Id, SortOrder = ci, ImagePath = en.Cards[ci].ImagePath,
                Title = Merge(en.Cards[ci].Title, a?.Title),
                Text = Merge(en.Cards[ci].Text, a?.Text),
            });
        }

        // --- Safety toggles (positional) ---
        for (var si = 0; si < en.Safety.Count; si++)
        {
            var a = ar is not null && si < ar.Safety.Count ? ar.Safety[si] : null;
            db.Set<SafetyToggle>().Add(new SafetyToggle
            {
                VehicleId = v.Id, SortOrder = si, ImagePath = en.Safety[si].ImagePath,
                Title = Merge(en.Safety[si].Title, a?.Title),
                Strap = Merge(en.Safety[si].Strap, a?.Strap),
                Content = Merge(en.Safety[si].Content, a?.Content),
            });
        }

        // --- Trims + price rows (positional) ---
        for (var ti = 0; ti < en.Trims.Count; ti++)
        {
            var et = en.Trims[ti];
            var at = ar is not null && ti < ar.Trims.Count ? ar.Trims[ti] : null;
            var trim = new Trim
            {
                VehicleId = v.Id, SortOrder = ti, ImagePath = et.ImagePath, SpecPdf = et.SpecPdf,
                ModelLabel = Merge(et.ModelLabel, at?.ModelLabel),
                Name = Merge(et.Name, at?.Name),
                Highlights = new LocalizedText(),   // legacy field unused by new render
            };
            for (var pi = 0; pi < et.PriceRows.Count; pi++)
            {
                var ap = at is not null && pi < at.PriceRows.Count ? at.PriceRows[pi] : null;
                trim.PriceRows.Add(new TrimPriceRow { SortOrder = pi, Text = Merge(et.PriceRows[pi].Text, ap?.Text) });
            }
            db.Set<Trim>().Add(trim);
        }

        // --- Warranty links (positional) ---
        for (var wi = 0; wi < en.Warranty.Count; wi++)
        {
            var a = ar is not null && wi < ar.Warranty.Count ? ar.Warranty[wi] : null;
            db.Set<WarrantyLink>().Add(new WarrantyLink
            {
                VehicleId = v.Id, SortOrder = wi, Url = en.Warranty[wi].Url,
                Label = Merge(en.Warranty[wi].Label, a?.Label),
            });
        }
    }
}
