/*
  GAC -- replace GN6 placeholder content (cloned from gs4) with the REAL GN6 model
  content + images -- 2026-06-23.
  Data-only change. NO schema migration / no __EFMigrationsHistory row / no DDL.
  Run once against the GAC database (shared dev + live prod @83.229.86.221).
  Mirrors GAC.Infrastructure/SeedContent/vehicles/gn6.html (EN),
          GAC.Infrastructure/SeedContent/vehicles/ar/gn6.html (AR),
          and the GN6 edits in ContentSeeder.cs (tagline + hero/thumb image paths).

  PRECONDITION: the gn6 row already exists (added by 2026-06-22-add-gn6-and-reorder.sql,
  which cloned gs4). This script only UPDATEs that row's text + body, and replaces its
  two VehicleImages (Hero + Gallery thumbnail) so listing pages and the detail hero show
  GN6 imagery instead of gs4.

  Real GN6 images must be deployed to wwwroot/assets/img/ alongside this:
    /assets/img/hero-gn6.jpg, /assets/img/m-gn6.png, and the /assets/img/gn6/ image set.

  ENCODING: this file is UTF-8 (with BOM) so the N'...' Arabic literals load correctly.
  Run in SSMS (respects the BOM) or: sqlcmd -f 65001 -i 2026-06-23-gn6-real-content.sql

  Idempotent: UPDATE is absolute; the image rows are deleted+reinserted each run.
  Re-running is a safe no-op.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE Slug = N'gn6')
BEGIN
    RAISERROR('gn6 row not found -- run 2026-06-22-add-gn6-and-reorder.sql first.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- ============================================================
-- (A) Update the gn6 row with real GN6 content (EN + AR)
-- ============================================================
UPDATE dbo.Vehicles
SET
    Name_En            = N'GN6',
    Name_Ar            = N'GN6',
    Tagline_En         = N'Luxurious Space',
    Tagline_Ar         = N'مساحة فاخرة',
    IntroText_En       = N'The all-new GAC GN6 is a 7-seater MPV that combines luxurious space, a refined 2.0 Turbo drivetrain and comprehensive safety for the whole family.',
    IntroText_Ar       = N'جي أيه سي GN6 الجديدة كلياً هي سيارة عائلية بسبعة مقاعد تجمع بين المساحة الفاخرة ومحرك 2.0 تيربو المنقّح والأمان الشامل لكل أفراد العائلة.',
    MetaTitle_En       = N'GAC GN6 | 7-Seater Family MPV',
    MetaTitle_Ar       = N'جي أيه سي GN6 | سيارة عائلية بسبعة مقاعد',
    MetaDescription_En = N'Discover the GAC GN6: a 7-seater MPV with 2.0 Turbo power, 1100L luggage space, a 73.2-inch cabin and advanced safety. Book a test drive today.',
    MetaDescription_Ar = N'اكتشف جي أيه سي GN6: سيارة عائلية بسبعة مقاعد بمحرك 2.0 تيربو ومساحة أمتعة 1100 لتر ومقصورة بعرض 73.2 بوصة وأنظمة أمان متطورة. احجز قيادتك التجريبية اليوم.',
    BodyHtml_En        = N'  <main class="mp">

    <!-- ============ HERO / INTRO BANNER ============ -->
    <section class="mp-hero">
      <a class="mp-hero__link" href="#enquiry" aria-label="Book a Test Drive">
        <img class="mp-hero__img" src="/assets/img/hero-gn6.jpg" alt="GAC GN6" />
        <div class="mp-hero__overlay">
          <div class="container">
            <h1 class="mp-hero__title">GN6</h1>
            <p class="mp-hero__sub">Luxurious Space</p>
            <span class="btn btn--hero">Book a Test Drive</span>
          </div>
        </div>
      </a>
    </section>

    <!-- ============ SECTION JUMP NAV ============ -->
    <nav class="mp-subnav" aria-label="Model sections">
      <div class="container mp-subnav__inner">
        <a class="btn btn--subnav" href="#dimensions">Dimensions</a>
        <div class="mp-subnav__links">
          <a href="#exterior" class="is-active">Exterior</a>
          <a href="#design">Design</a>
          <a href="#interior">Interior</a>
          <a href="#gallery">Gallery</a>
          <a href="#space">Space</a>
          <a href="#performance">Performance</a>
          <a href="#safety">Safety</a>
          <a href="#warranty">Warranty</a>
        </div>
        <a class="btn btn--subnav" href="#enquiry">Order Online</a>
      </div>
    </nav>

    <!-- ============ OVERVIEW ============ -->
    <section class="mp-section" id="exterior">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">GAC GN6</h2>
          <p class="mp-head__sub">Luxurious Space</p>
        </header>
        <div class="mp-stats">
          <div class="mp-stat"><span class="mp-stat__label">Engine</span><span class="mp-stat__value">2.0 Turbo</span></div>
          <div class="mp-stat"><span class="mp-stat__label">Fuel Consumption</span><span class="mp-stat__value">15.6 KM/L</span></div>
          <div class="mp-stat"><span class="mp-stat__label">Seating</span><span class="mp-stat__value">7 Seater</span></div>
          <div class="mp-stat"><span class="mp-stat__label">Luggage Space</span><span class="mp-stat__value">1100 L</span></div>
        </div>
        <p class="mp-note">*Specifications may vary from one market to another. Please contact us for full details.</p>
      </div>
    </section>

    <!-- ============ EXTERIOR CAROUSEL ============ -->
    <section class="mp-slider-wrap">
      <div class="mp-slider" data-slider>
        <div class="mp-slider__viewport">
          <div class="mp-slider__track" data-slider-track>
            <figure class="mp-slide"><img src="/assets/img/gn6/ext-1.jpg" alt="GN6 dynamic and avant-garde LED lights" /></figure>
            <figure class="mp-slide"><img src="/assets/img/gn6/ext-2.jpg" alt="GN6 concealed D-pillar and privacy glass" /></figure>
            <figure class="mp-slide"><img src="/assets/img/gn6/ext-3.jpg" alt="GN6 connected LED taillights" /></figure>
          </div>
        </div>
        <div class="mp-slider__caption">
          <span class="mp-slider__eyebrow">Exterior</span>
          <span class="mp-slider__title">New Style and Elite Appearance</span>
        </div>
        <button class="mp-slider__arrow mp-slider__arrow--prev" data-slider-prev aria-label="Previous">‹</button>
        <button class="mp-slider__arrow mp-slider__arrow--next" data-slider-next aria-label="Next">›</button>
      </div>
    </section>

    <!-- ============ DESIGN ============ -->
    <section class="mp-section" id="design">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">Design</h2>
          <p class="mp-head__sub">New style and elite appearance</p>
        </header>

        <div class="mp-tabs" data-tabs-wrap>
          <div class="mp-tabs__nav" data-tabs>
            <button class="mp-tabs__btn is-active" data-tab-btn="d1">New Style and Elite Appearance</button>
            <button class="mp-tabs__btn" data-tab-btn="d2">Concealed D-Pillar and Privacy Glass</button>
            <button class="mp-tabs__btn" data-tab-btn="d3">Vigorous and Grander Design</button>
          </div>
          <div class="mp-tabs__root" data-tab-root>

            <div class="mp-feature is-active" data-tab-panel="d1">
              <div class="mp-feature__media"><img src="/assets/img/gn6/design-1.jpg" alt="GN6 dynamic and avant-garde LED lighting" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">New Style and Elite Appearance</h3>
                <p>The GN6 makes a confident first impression with a dynamic, avant-garde lighting signature that blends a modern identity with everyday practicality.</p>
                <ul class="mp-feature__list">
                  <li><strong>Dynamic, avant-garde LED lights:</strong> A bold front face with a contemporary, premium presence.</li>
                  <li><strong>Longitudinal matrix LED headlamp:</strong> Crisp illumination for excellent visibility and a striking look.</li>
                  <li><strong>Connected LED taillights:</strong> A full-width light bar that gives the GN6 an elegant, unmistakable rear signature.</li>
                </ul>
              </div>
            </div>

            <div class="mp-feature" data-tab-panel="d2">
              <div class="mp-feature__media"><img src="/assets/img/gn6/ext-2.jpg" alt="GN6 concealed D-pillar and privacy glass" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">Concealed D-Pillar and Privacy Glass</h3>
                <p>A floating-roof silhouette gives the GN6 a sleek, refined stance, while the concealed D-pillar and privacy glass deliver both visual sophistication and added comfort for rear passengers.</p>
                <ul class="mp-feature__list">
                  <li><strong>Concealed D-pillar:</strong> Creates a continuous, premium floating-roof effect.</li>
                  <li><strong>Privacy glass:</strong> Greater privacy and a cooler, more comfortable cabin.</li>
                </ul>
              </div>
            </div>

            <div class="mp-feature" data-tab-panel="d3">
              <div class="mp-feature__media"><img src="/assets/img/gn6/design-2.jpg" alt="GN6 vigorous and grander design" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">Vigorous and Grander Design</h3>
                <p>A vigorous and grander design language gives the GN6 genuine road presence, led by a brand-new flying-dynamics front grille that anchors its bold, MPV proportions.</p>
                <ul class="mp-feature__list">
                  <li><strong>Brand-new flying-dynamics front grille:</strong> A commanding, expressive front identity.</li>
                  <li><strong>Vigorous and grander proportions:</strong> A confident stance that stands out on every road.</li>
                </ul>
              </div>
            </div>

          </div>
        </div>
      </div>
    </section>

    <!-- ============ INTERIOR CAROUSEL ============ -->
    <section class="mp-slider-wrap" id="interior">
      <div class="mp-slider" data-slider>
        <div class="mp-slider__viewport">
          <div class="mp-slider__track" data-slider-track>
            <figure class="mp-slide"><img src="/assets/img/gn6/int-1.jpg" alt="GN6 73.2-inch 7-seat interior" /></figure>
            <figure class="mp-slide"><img src="/assets/img/gn6/int-2.jpg" alt="GN6 electric 6-way adjustable driver''s seat" /></figure>
          </div>
        </div>
        <div class="mp-slider__caption">
          <span class="mp-slider__eyebrow">Interior</span>
          <span class="mp-slider__title">Super Spacious Interior</span>
        </div>
        <button class="mp-slider__arrow mp-slider__arrow--prev" data-slider-prev aria-label="Previous">‹</button>
        <button class="mp-slider__arrow mp-slider__arrow--next" data-slider-next aria-label="Next">›</button>
      </div>
    </section>

    <!-- ============ GALLERY ============ -->
    <section class="mp-section" id="gallery">
      <div class="container">
        <header class="mp-head mp-head--center">
          <h2 class="mp-head__title">Gallery</h2>
        </header>

        <div class="mp-tabs" data-tabs-wrap>
          <div class="mp-tabs__nav" data-tabs>
            <button class="mp-tabs__btn is-active" data-tab-btn="gex">Exterior</button>
            <button class="mp-tabs__btn" data-tab-btn="gin">Interior</button>
            <button class="mp-tabs__btn" data-tab-btn="gte">Comfort</button>
          </div>
          <div class="mp-tabs__root" data-tab-root>

            <div class="mp-gpanel is-active" data-tab-panel="gex">
              <div class="mp-gallery">
                    <a class="mp-gshot" href="/assets/img/gn6/ext-1.jpg"><img src="/assets/img/gn6/ext-1.jpg" alt="GN6 exterior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/ext-2.jpg"><img src="/assets/img/gn6/ext-2.jpg" alt="GN6 exterior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/ext-3.jpg"><img src="/assets/img/gn6/ext-3.jpg" alt="GN6 exterior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/design-1.jpg"><img src="/assets/img/gn6/design-1.jpg" alt="GN6 exterior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/design-2.jpg"><img src="/assets/img/gn6/design-2.jpg" alt="GN6 exterior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/design-3.jpg"><img src="/assets/img/gn6/design-3.jpg" alt="GN6 exterior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
              </div>
            </div>

            <div class="mp-gpanel" data-tab-panel="gin">
              <div class="mp-gallery">
                    <a class="mp-gshot" href="/assets/img/gn6/int-1.jpg"><img src="/assets/img/gn6/int-1.jpg" alt="GN6 interior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/int-2.jpg"><img src="/assets/img/gn6/int-2.jpg" alt="GN6 interior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/gal-1.jpg"><img src="/assets/img/gn6/gal-1.jpg" alt="GN6 interior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/gal-2.jpg"><img src="/assets/img/gn6/gal-2.jpg" alt="GN6 interior" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
              </div>
            </div>

            <div class="mp-gpanel" data-tab-panel="gte">
              <div class="mp-gallery">
                    <a class="mp-gshot" href="/assets/img/gn6/gal-3.jpg"><img src="/assets/img/gn6/gal-3.jpg" alt="GN6 7.5-inch ultra-wide second-row corridor" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/gal-4.jpg"><img src="/assets/img/gn6/gal-4.jpg" alt="GN6 flexible and comfortable 3rd row" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/gal-5.jpg"><img src="/assets/img/gn6/gal-5.jpg" alt="GN6 1100L ultra-large luggage space" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
              </div>
            </div>

          </div>
        </div>
      </div>
    </section>

    <!-- ============ INTERIOR / TECHNOLOGY ============ -->
    <section class="mp-section mp-section--grey" id="technology">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">Super Spacious Interior</h2>
          <p class="mp-head__sub">A 73.2-inch, 7-seat cabin designed for comfort, with an electrically adjustable driver''s seat, smart climate control and generous, flexible storage throughout.</p>
        </header>
        <div class="mp-tech-banner"><img src="/assets/img/gn6/tech-banner.jpg" alt="GN6 spacious and luxurious cabin" /></div>
        <div class="mp-cards">
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/int-1.jpg" alt="GN6 73.2-inch 7-seat design" /></div>
            <h3 class="mp-card__title">73.2-Inch 7-Seat Design</h3>
            <p class="mp-card__text">A generous 7-seat layout that delivers space and comfort for the whole family.</p>
          </article>
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/int-2.jpg" alt="GN6 electric 6-way adjustable driver''s seat" /></div>
            <h3 class="mp-card__title">Electric 6-Way Driver''s Seat</h3>
            <p class="mp-card__text">An electrically adjustable driver''s seat to easily find your ideal driving position.</p>
          </article>
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/gal-1.jpg" alt="GN6 smart independent air conditioning with pure forest air" /></div>
            <h3 class="mp-card__title">Smart Independent AC + Pure Forest Air</h3>
            <p class="mp-card__text">Independent climate control with a pure-forest air system, plus customizable console storage.</p>
          </article>
        </div>
      </div>
    </section>

    <!-- ============ SPACIOUS AND LUXURIOUS SPACE ============ -->
    <section class="mp-section" id="space">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">Spacious and Luxurious Space</h2>
          <p class="mp-head__sub">Flexible, free and open — space that adapts to passengers and cargo alike.</p>
        </header>
        <div class="mp-cards">
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/gal-3.jpg" alt="GN6 7.5-inch ultra-wide second-row corridor" /></div>
            <h3 class="mp-card__title">7.5-Inch Ultra-Wide Second-Row Corridor</h3>
            <p class="mp-card__text">An ultra-wide aisle for easy, dignified access to the third row.</p>
          </article>
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/gal-4.jpg" alt="GN6 flexible and comfortable third row" /></div>
            <h3 class="mp-card__title">Flexible and Comfortable 3rd Row</h3>
            <p class="mp-card__text">Flexible and free — an open, comfortable third row for every journey.</p>
          </article>
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/gal-5.jpg" alt="GN6 1100L ultra-large luggage space" /></div>
            <h3 class="mp-card__title">1100L Ultra-Large Luggage Space</h3>
            <p class="mp-card__text">A magic, ultra-large luggage capacity of up to 1100L for everything you need to carry.</p>
          </article>
        </div>
      </div>
    </section>

    <!-- ============ PERFORMANCE ============ -->
    <section class="mp-section" id="performance">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">Safe and Comfortable Driving</h2>
          <p class="mp-head__sub">Confident performance with a refined, stable and quiet ride</p>
        </header>

        <div class="mp-tabs" data-tabs-wrap>
          <div class="mp-tabs__nav" data-tabs>
            <button class="mp-tabs__btn is-active" data-tab-btn="p1">2.0 Turbo + Next-Gen Aisin 6-Speed Gearbox</button>
            <button class="mp-tabs__btn" data-tab-btn="p2">High-Strength Chassis and Suspension</button>
            <button class="mp-tabs__btn" data-tab-btn="p3">Bosch Premium ESP 9.3</button>
          </div>
          <div class="mp-tabs__root" data-tab-root>

            <div class="mp-feature is-active" data-tab-panel="p1">
              <div class="mp-feature__media"><img src="/assets/img/gn6/perf-1.jpg" alt="GN6 2.0 Turbo engine and next-generation Aisin 6-speed gearbox" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">2.0 Turbo Power, Smoothly Delivered</h3>
                <p>The GN6 pairs a responsive 2.0 Turbo engine with a next-generation Aisin 6-speed gearbox for smooth, confident acceleration and efficient cruising — returning up to 15.6 KM/L.</p>
                <ul class="mp-feature__list">
                  <li><strong>2.0 Turbo engine:</strong> Strong, refined power for both city and highway driving.</li>
                  <li><strong>Next-gen Aisin 6-speed gearbox:</strong> Precise, seamless shifts for effortless progress.</li>
                </ul>
              </div>
            </div>

            <div class="mp-feature" data-tab-panel="p2">
              <div class="mp-feature__media"><img src="/assets/img/gn6/perf-2.jpg" alt="GN6 high-strength safe chassis and silent cockpit" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">High-Strength Chassis and Suspension</h3>
                <p>A high-strength safe chassis and a zero-vibration silent cockpit work together to deliver a quiet, composed and reassuring drive.</p>
                <ul class="mp-feature__list">
                  <li><strong>High-strength safe chassis + zero-vibration silent cockpit:</strong> Strength and refinement in equal measure.</li>
                  <li><strong>L-type McPherson strut + high-rigidity torsion beam:</strong> A balanced suspension tuned for comfort and stability.</li>
                </ul>
              </div>
            </div>

            <div class="mp-feature" data-tab-panel="p3">
              <div class="mp-feature__media"><img src="/assets/img/gn6/perf-3.jpg" alt="GN6 Bosch premium ESP 9.3 with EPB and auto hold" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">Intelligent Control and Confidence</h3>
                <p>Smart driving aids keep the GN6 stable and easy to manage in every condition.</p>
                <ul class="mp-feature__list">
                  <li><strong>Bosch premium ESP 9.3:</strong> Advanced stability control for secure handling.</li>
                  <li><strong>EPB + Auto Hold:</strong> Effortless stop-and-go and convenient parking.</li>
                  <li><strong>360° panoramic backup camera:</strong> A clear, all-round view for confident maneuvering.</li>
                </ul>
              </div>
            </div>

          </div>
        </div>
      </div>
    </section>

    <!-- ============ SAFETY ============ -->
    <section class="mp-section mp-section--grey" id="safety">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">Safe and Comfortable Driving</h2>
          <p class="mp-head__sub">Intelligent technology and a strong, safe body help you enjoy every journey with total confidence.</p>
          <p class="mp-head__body">From a high-strength safe chassis to Bosch premium ESP 9.3, Electronic Parking Brake (EPB) with Auto Hold, and a 360° panoramic backup camera, the GN6 surrounds you and your family with comprehensive protection.</p>
        </header>
        <div class="mp-stoggles">
          <article class="mp-stoggle is-open">
            <button class="mp-stoggle__head" type="button" aria-expanded="true"><span>High-Strength Safe Chassis</span><i class="mp-stoggle__icon"></i></button>
            <div class="mp-stoggle__body">
              <div class="mp-stoggle__media"><img src="/assets/img/gn6/safe-1.jpg" alt="GN6 high-strength safe chassis and silent cockpit" loading="lazy" /></div>
              <h3 class="mp-stoggle__strap">High-Strength Safe Chassis</h3>
              <p class="mp-stoggle__content">High-strength safe chassis with a zero-vibration silent cockpit for a quiet, secure ride.</p>
            </div>
          </article>
          <article class="mp-stoggle">
            <button class="mp-stoggle__head" type="button" aria-expanded="false"><span>Bosch Premium ESP 9.3</span><i class="mp-stoggle__icon"></i></button>
            <div class="mp-stoggle__body">
              <div class="mp-stoggle__media"><img src="/assets/img/gn6/safe-2.jpg" alt="GN6 Bosch premium ESP 9.3" loading="lazy" /></div>
              <h3 class="mp-stoggle__strap">Bosch Premium ESP 9.3</h3>
              <p class="mp-stoggle__content">Advanced electronic stability control, with EPB and Auto Hold for added convenience and safety.</p>
            </div>
          </article>
          <article class="mp-stoggle">
            <button class="mp-stoggle__head" type="button" aria-expanded="false"><span>360° Panoramic Backup Camera</span><i class="mp-stoggle__icon"></i></button>
            <div class="mp-stoggle__body">
              <div class="mp-stoggle__media"><img src="/assets/img/gn6/safe-3.jpg" alt="GN6 360-degree panoramic backup camera" loading="lazy" /></div>
              <h3 class="mp-stoggle__strap">360° Panoramic Backup Camera</h3>
              <p class="mp-stoggle__content">A full surround-view camera system for a clear, all-round view when parking and maneuvering.</p>
            </div>
          </article>
        </div>
      </div>
    </section>

    <!-- ============ DIMENSIONS ============ -->
    <section class="mp-section" id="dimensions">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">Dimensions</h2>
          <p class="mp-head__sub">Grand MPV proportions for space and presence</p>
        </header>
        <div class="mp-tech-banner"><img src="/assets/img/gn6/dimensions.png" alt="GN6 exterior dimensions diagram" /></div>
        <div class="mp-stats">
          <div class="mp-stat"><span class="mp-stat__label">Length</span><span class="mp-stat__value">4780 mm</span></div>
          <div class="mp-stat"><span class="mp-stat__label">Width</span><span class="mp-stat__value">1860 mm</span></div>
          <div class="mp-stat"><span class="mp-stat__label">Height</span><span class="mp-stat__value">1730 mm</span></div>
          <div class="mp-stat"><span class="mp-stat__label">Wheelbase</span><span class="mp-stat__value">2810 mm</span></div>
        </div>
      </div>
    </section>

    <!-- ============ WARRANTY ============ -->
    <section class="mp-section" id="warranty">
      <div class="container">
        <hr class="mp-hr" />
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">Warranty</h2>
          <p class="mp-head__sub">7 years warranty, or 200,000 km.</p>
        </header>
        <div class="mp-warranty__links">
          <a class="btn btn--doc" href="https://en.gacmotorsaudi.com/" target="_blank" rel="noopener">Manuals</a>
        </div>
      </div>
    </section>

    <!-- ============ ENQUIRY / CONTACT ============ -->
    <section class="mp-enquiry" id="enquiry" style="background-image:url(''/assets/img/hero-gn6.jpg'')">
      <div class="mp-enquiry__overlay">
        <div class="container mp-enquiry__grid">
          <div class="mp-enquiry__intro">
            <h2 class="mp-enquiry__title">Contact Us</h2>
            <p class="mp-enquiry__sub">request a price or book a test drive</p>
            <p class="mp-enquiry__lead">We will contact you within 24 working hours.</p>
            <div class="mp-enquiry__actions">
              <a class="mp-enquiry__action" href="tel:1833334">
                <svg viewBox="0 0 24 24"><path d="M6.6 10.8a15.5 15.5 0 0 0 6.6 6.6l2.2-2.2a1 1 0 0 1 1-.24 11.4 11.4 0 0 0 3.6.58 1 1 0 0 1 1 1V20a1 1 0 0 1-1 1A17 17 0 0 1 3 4a1 1 0 0 1 1-1h3.5a1 1 0 0 1 1 1 11.4 11.4 0 0 0 .58 3.6 1 1 0 0 1-.25 1z"/></svg>
                <span>Call us</span>
              </a>
              <a class="mp-enquiry__action" href="/contact-us">
                <svg viewBox="0 0 24 24"><path d="M12 2a7 7 0 0 0-7 7c0 5.25 7 13 7 13s7-7.75 7-13a7 7 0 0 0-7-7zm0 9.5A2.5 2.5 0 1 1 12 6.5a2.5 2.5 0 0 1 0 5z"/></svg>
                <span>Find a location</span>
              </a>
            </div>
          </div>

          <form class="mp-form" data-form novalidate>
            <div class="field">
              <label>Message</label>
              <textarea rows="3"></textarea>
            </div>
            <div class="field">
              <label>Select Branch *</label>
              <select required>
                <option value="">Please select ...</option>
                <option>Riyadh Branch</option>
                <option>GAC Motors Jeddah, Malibari Sq Showroom</option>
                <option>GAC Motors Jeddah, Kilo 3 Branch</option>
                <option>Dammam Branch</option>
                <option>GAC Motors Al-Madinah Al-Munawarrah Branch</option>
                <option>GAC Motors Khamis Mushait Branch</option>
                <option>GAC Motors Jazan Branch</option>
              </select>
            </div>
            <div class="field">
              <label>Title *</label>
              <select required>
                <option value="">Please select ...</option>
                <option>Mr</option><option>Ms</option><option>Mrs</option><option>Miss</option>
              </select>
            </div>
            <div class="field"><label>First Name *</label><input type="text" required /></div>
            <div class="field"><label>Last Name *</label><input type="text" required /></div>
            <div class="field"><label>Email Address *</label><input type="email" required /></div>
            <div class="field"><label>Contact Number *</label><input type="tel" required /></div>
            <div class="mp-form__dpp">
              <p class="mp-form__dpp-title">Privacy statement &amp; legal disclaimer</p>
              I acknowledge and understand that my information will be shared with Mutawa Alkadi Automotive Company, its affiliates, or other parties are required by law for compliance, safety campaigns, government inquiries, or similar legal process. My information may also be shared for product research and development purposes, and to manage customer relationships to provide support. Privacy Statement addresses how AAC and GAC handles the personal information shared with us at <a href="/privacy-policy">our Privacy Policy</a>
            </div>
            <label class="mp-check"><input type="checkbox" /> <span>I wish to receive any marketing information or have my information shared with third parties for purposes of providing me with marketing information.</span></label>
            <button class="mp-form__submit" type="submit">Submit</button>
          </form>
        </div>
      </div>
    </section>

  </main>

  <!-- =================================================
       FOOTER
       ================================================= -->

  <!-- Gallery lightbox viewer -->
  <div class="mp-lightbox" data-lightbox aria-hidden="true" role="dialog" aria-label="Image viewer">
    <button class="mp-lightbox__close" data-lb-close aria-label="Close">×</button>
    <button class="mp-lightbox__nav mp-lightbox__nav--prev" data-lb-prev aria-label="Previous image">‹</button>
    <img class="mp-lightbox__img" data-lb-img src="" alt="" />
    <button class="mp-lightbox__nav mp-lightbox__nav--next" data-lb-next aria-label="Next image">›</button>
    <div class="mp-lightbox__count" data-lb-count></div>
  </div>
',
    BodyHtml_Ar        = N'  <main class="mp">

    <!-- ============ HERO / INTRO BANNER ============ -->
    <section class="mp-hero">
      <a class="mp-hero__link" href="#enquiry" aria-label="احجز قيادة تجريبية">
        <img class="mp-hero__img" src="/assets/img/hero-gn6.jpg" alt="جي أيه سي GN6" />
        <div class="mp-hero__overlay">
          <div class="container">
            <h1 class="mp-hero__title">GN6</h1>
            <p class="mp-hero__sub">مساحة فاخرة</p>
            <span class="btn btn--hero">احجز قيادة تجريبية</span>
          </div>
        </div>
      </a>
    </section>

    <!-- ============ SECTION JUMP NAV ============ -->
    <nav class="mp-subnav" aria-label="أقسام الموديل">
      <div class="container mp-subnav__inner">
        <a class="btn btn--subnav" href="#dimensions">الأبعاد</a>
        <div class="mp-subnav__links">
          <a href="#exterior" class="is-active">الخارجي</a>
          <a href="#design">التصميم</a>
          <a href="#interior">الداخلي</a>
          <a href="#gallery">معرض الصور</a>
          <a href="#space">المساحة</a>
          <a href="#performance">الأداء</a>
          <a href="#safety">الأمان</a>
          <a href="#warranty">الضمان</a>
        </div>
        <a class="btn btn--subnav" href="#enquiry">اطلب عبر الإنترنت</a>
      </div>
    </nav>

    <!-- ============ OVERVIEW ============ -->
    <section class="mp-section" id="exterior">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">جي أيه سي GN6</h2>
          <p class="mp-head__sub">مساحة فاخرة</p>
        </header>
        <div class="mp-stats">
          <div class="mp-stat"><span class="mp-stat__label">المحرك</span><span class="mp-stat__value">2.0 تيربو</span></div>
          <div class="mp-stat"><span class="mp-stat__label">استهلاك الوقود</span><span class="mp-stat__value">15.6 كم/لتر</span></div>
          <div class="mp-stat"><span class="mp-stat__label">المقاعد</span><span class="mp-stat__value">7 مقاعد</span></div>
          <div class="mp-stat"><span class="mp-stat__label">مساحة الأمتعة</span><span class="mp-stat__value">1100 لتر</span></div>
        </div>
        <p class="mp-note">*قد تختلف المواصفات من سوق إلى آخر. يرجى التواصل معنا لمعرفة كافة التفاصيل.</p>
      </div>
    </section>

    <!-- ============ EXTERIOR CAROUSEL ============ -->
    <section class="mp-slider-wrap">
      <div class="mp-slider" data-slider>
        <div class="mp-slider__viewport">
          <div class="mp-slider__track" data-slider-track>
            <figure class="mp-slide"><img src="/assets/img/gn6/ext-1.jpg" alt="إضاءة LED عصرية وجريئة في GN6" /></figure>
            <figure class="mp-slide"><img src="/assets/img/gn6/ext-2.jpg" alt="عمود D مخفي وزجاج عازل للخصوصية في GN6" /></figure>
            <figure class="mp-slide"><img src="/assets/img/gn6/ext-3.jpg" alt="مصابيح خلفية LED متصلة في GN6" /></figure>
          </div>
        </div>
        <div class="mp-slider__caption">
          <span class="mp-slider__eyebrow">الخارجي</span>
          <span class="mp-slider__title">تصميم عصري ومظهر راقٍ</span>
        </div>
        <button class="mp-slider__arrow mp-slider__arrow--prev" data-slider-prev aria-label="السابق">‹</button>
        <button class="mp-slider__arrow mp-slider__arrow--next" data-slider-next aria-label="التالي">›</button>
      </div>
    </section>

    <!-- ============ DESIGN ============ -->
    <section class="mp-section" id="design">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">التصميم</h2>
          <p class="mp-head__sub">تصميم عصري ومظهر راقٍ</p>
        </header>

        <div class="mp-tabs" data-tabs-wrap>
          <div class="mp-tabs__nav" data-tabs>
            <button class="mp-tabs__btn is-active" data-tab-btn="d1">تصميم عصري ومظهر راقٍ</button>
            <button class="mp-tabs__btn" data-tab-btn="d2">عمود D مخفي وزجاج عازل للخصوصية</button>
            <button class="mp-tabs__btn" data-tab-btn="d3">تصميم قوي وفخم</button>
          </div>
          <div class="mp-tabs__root" data-tab-root>

            <div class="mp-feature is-active" data-tab-panel="d1">
              <div class="mp-feature__media"><img src="/assets/img/gn6/design-1.jpg" alt="إضاءة LED عصرية وجريئة في GN6" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">تصميم عصري ومظهر راقٍ</h3>
                <p>يترك GN6 انطباعاً أولياً واثقاً بفضل توقيع إضاءة عصري وجريء يجمع بين الهوية الحديثة والاستخدام العملي اليومي.</p>
                <ul class="mp-feature__list">
                  <li><strong>إضاءة LED عصرية وجريئة:</strong> واجهة أمامية لافتة بحضور حديث وفاخر.</li>
                  <li><strong>مصابيح أمامية LED مصفوفة طولية:</strong> إضاءة واضحة لرؤية ممتازة ومظهر مميز.</li>
                  <li><strong>مصابيح خلفية LED متصلة:</strong> شريط إضاءة ممتد بعرض المركبة يمنح GN6 توقيعاً خلفياً أنيقاً لا يُنسى.</li>
                </ul>
              </div>
            </div>

            <div class="mp-feature" data-tab-panel="d2">
              <div class="mp-feature__media"><img src="/assets/img/gn6/ext-2.jpg" alt="عمود D مخفي وزجاج عازل للخصوصية في GN6" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">عمود D مخفي وزجاج عازل للخصوصية</h3>
                <p>يمنح خط السقف الطافي مظهراً انسيابياً وراقياً لـ GN6، بينما يوفر عمود D المخفي والزجاج العازل للخصوصية رقياً بصرياً وراحة إضافية لركاب المقاعد الخلفية.</p>
                <ul class="mp-feature__list">
                  <li><strong>عمود D مخفي:</strong> يخلق تأثير سقف طافٍ متواصل وفاخر.</li>
                  <li><strong>زجاج عازل للخصوصية:</strong> خصوصية أكبر ومقصورة أكثر برودة وراحة.</li>
                </ul>
              </div>
            </div>

            <div class="mp-feature" data-tab-panel="d3">
              <div class="mp-feature__media"><img src="/assets/img/gn6/design-2.jpg" alt="تصميم قوي وفخم في GN6" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">تصميم قوي وفخم</h3>
                <p>تمنح لغة التصميم القوية والفخمة سيارة GN6 حضوراً حقيقياً على الطريق، يتصدّرها شبك أمامي جديد كلياً بتصميم ديناميكي يعزّز أبعادها الجريئة كسيارة متعددة الاستخدامات.</p>
                <ul class="mp-feature__list">
                  <li><strong>شبك أمامي جديد كلياً بتصميم ديناميكي:</strong> هوية أمامية مهيبة ومعبّرة.</li>
                  <li><strong>أبعاد قوية وفخمة:</strong> حضور واثق يلفت الأنظار على كل طريق.</li>
                </ul>
              </div>
            </div>

          </div>
        </div>
      </div>
    </section>

    <!-- ============ INTERIOR CAROUSEL ============ -->
    <section class="mp-slider-wrap" id="interior">
      <div class="mp-slider" data-slider>
        <div class="mp-slider__viewport">
          <div class="mp-slider__track" data-slider-track>
            <figure class="mp-slide"><img src="/assets/img/gn6/int-1.jpg" alt="مقصورة GN6 بتصميم 7 مقاعد بعرض 73.2 بوصة" /></figure>
            <figure class="mp-slide"><img src="/assets/img/gn6/int-2.jpg" alt="مقعد سائق كهربائي قابل للتعديل في 6 اتجاهات في GN6" /></figure>
          </div>
        </div>
        <div class="mp-slider__caption">
          <span class="mp-slider__eyebrow">الداخلي</span>
          <span class="mp-slider__title">مقصورة داخلية فسيحة للغاية</span>
        </div>
        <button class="mp-slider__arrow mp-slider__arrow--prev" data-slider-prev aria-label="السابق">‹</button>
        <button class="mp-slider__arrow mp-slider__arrow--next" data-slider-next aria-label="التالي">›</button>
      </div>
    </section>

    <!-- ============ GALLERY ============ -->
    <section class="mp-section" id="gallery">
      <div class="container">
        <header class="mp-head mp-head--center">
          <h2 class="mp-head__title">معرض الصور</h2>
        </header>

        <div class="mp-tabs" data-tabs-wrap>
          <div class="mp-tabs__nav" data-tabs>
            <button class="mp-tabs__btn is-active" data-tab-btn="gex">الخارجي</button>
            <button class="mp-tabs__btn" data-tab-btn="gin">الداخلي</button>
            <button class="mp-tabs__btn" data-tab-btn="gte">الراحة</button>
          </div>
          <div class="mp-tabs__root" data-tab-root>

            <div class="mp-gpanel is-active" data-tab-panel="gex">
              <div class="mp-gallery">
                    <a class="mp-gshot" href="/assets/img/gn6/ext-1.jpg"><img src="/assets/img/gn6/ext-1.jpg" alt="المظهر الخارجي لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/ext-2.jpg"><img src="/assets/img/gn6/ext-2.jpg" alt="المظهر الخارجي لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/ext-3.jpg"><img src="/assets/img/gn6/ext-3.jpg" alt="المظهر الخارجي لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/design-1.jpg"><img src="/assets/img/gn6/design-1.jpg" alt="المظهر الخارجي لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/design-2.jpg"><img src="/assets/img/gn6/design-2.jpg" alt="المظهر الخارجي لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/design-3.jpg"><img src="/assets/img/gn6/design-3.jpg" alt="المظهر الخارجي لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
              </div>
            </div>

            <div class="mp-gpanel" data-tab-panel="gin">
              <div class="mp-gallery">
                    <a class="mp-gshot" href="/assets/img/gn6/int-1.jpg"><img src="/assets/img/gn6/int-1.jpg" alt="المقصورة الداخلية لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/int-2.jpg"><img src="/assets/img/gn6/int-2.jpg" alt="المقصورة الداخلية لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/gal-1.jpg"><img src="/assets/img/gn6/gal-1.jpg" alt="المقصورة الداخلية لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/gal-2.jpg"><img src="/assets/img/gn6/gal-2.jpg" alt="المقصورة الداخلية لـ GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
              </div>
            </div>

            <div class="mp-gpanel" data-tab-panel="gte">
              <div class="mp-gallery">
                    <a class="mp-gshot" href="/assets/img/gn6/gal-3.jpg"><img src="/assets/img/gn6/gal-3.jpg" alt="ممر أوسط فائق الاتساع بعرض 7.5 بوصة في GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/gal-4.jpg"><img src="/assets/img/gn6/gal-4.jpg" alt="صف ثالث مرن ومريح في GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
                    <a class="mp-gshot" href="/assets/img/gn6/gal-5.jpg"><img src="/assets/img/gn6/gal-5.jpg" alt="مساحة أمتعة ضخمة تصل إلى 1100 لتر في GN6" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M21 21l-4-4M11 8v6M8 11h6"/></svg></span></a>
              </div>
            </div>

          </div>
        </div>
      </div>
    </section>

    <!-- ============ INTERIOR / TECHNOLOGY ============ -->
    <section class="mp-section mp-section--grey" id="technology">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">مقصورة داخلية فسيحة للغاية</h2>
          <p class="mp-head__sub">مقصورة بمساحة 73.2 بوصة وتصميم 7 مقاعد مصمّمة للراحة، مع مقعد سائق كهربائي قابل للتعديل، وتكييف ذكي، ومساحات تخزين سخية ومرنة في كل مكان.</p>
        </header>
        <div class="mp-tech-banner"><img src="/assets/img/gn6/tech-banner.jpg" alt="المقصورة الواسعة والفاخرة في GN6" /></div>
        <div class="mp-cards">
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/int-1.jpg" alt="تصميم 7 مقاعد بعرض 73.2 بوصة في GN6" /></div>
            <h3 class="mp-card__title">تصميم 7 مقاعد بعرض 73.2 بوصة</h3>
            <p class="mp-card__text">تصميم سخي بـ 7 مقاعد يوفّر مساحة وراحة لجميع أفراد العائلة.</p>
          </article>
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/int-2.jpg" alt="مقعد سائق كهربائي قابل للتعديل في 6 اتجاهات في GN6" /></div>
            <h3 class="mp-card__title">مقعد سائق كهربائي بـ 6 اتجاهات</h3>
            <p class="mp-card__text">مقعد سائق كهربائي قابل للتعديل لإيجاد وضعية القيادة المثالية بسهولة.</p>
          </article>
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/gal-1.jpg" alt="تكييف ذكي مستقل مع نظام هواء نقي في GN6" /></div>
            <h3 class="mp-card__title">تكييف ذكي مستقل + نظام هواء نقي</h3>
            <p class="mp-card__text">تحكّم مستقل بالتكييف مع نظام هواء نقي، إضافةً إلى مساحات تخزين قابلة للتخصيص في الكونسول.</p>
          </article>
        </div>
      </div>
    </section>

    <!-- ============ SPACIOUS AND LUXURIOUS SPACE ============ -->
    <section class="mp-section" id="space">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">مساحة واسعة وفاخرة</h2>
          <p class="mp-head__sub">مساحة مرنة ومنفتحة تتكيّف مع الركاب والأمتعة على حدٍ سواء.</p>
        </header>
        <div class="mp-cards">
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/gal-3.jpg" alt="ممر أوسط فائق الاتساع بعرض 7.5 بوصة في GN6" /></div>
            <h3 class="mp-card__title">ممر أوسط فائق الاتساع بعرض 7.5 بوصة</h3>
            <p class="mp-card__text">ممر واسع للوصول بسهولة وراحة إلى الصف الثالث.</p>
          </article>
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/gal-4.jpg" alt="صف ثالث مرن ومريح في GN6" /></div>
            <h3 class="mp-card__title">صف ثالث مرن ومريح</h3>
            <p class="mp-card__text">مرن ومنفتح — صف ثالث مريح لكل رحلة.</p>
          </article>
          <article class="mp-card">
            <div class="mp-card__media"><img src="/assets/img/gn6/gal-5.jpg" alt="مساحة أمتعة ضخمة تصل إلى 1100 لتر في GN6" /></div>
            <h3 class="mp-card__title">مساحة أمتعة ضخمة 1100 لتر</h3>
            <p class="mp-card__text">سعة أمتعة استثنائية وضخمة تصل إلى 1100 لتر لكل ما تحتاج إلى حمله.</p>
          </article>
        </div>
      </div>
    </section>

    <!-- ============ PERFORMANCE ============ -->
    <section class="mp-section" id="performance">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">قيادة آمنة ومريحة</h2>
          <p class="mp-head__sub">أداء واثق مع قيادة سلسة وثابتة وهادئة</p>
        </header>

        <div class="mp-tabs" data-tabs-wrap>
          <div class="mp-tabs__nav" data-tabs>
            <button class="mp-tabs__btn is-active" data-tab-btn="p1">2.0 تيربو + ناقل حركة Aisin من 6 سرعات</button>
            <button class="mp-tabs__btn" data-tab-btn="p2">هيكل وتعليق عالي المتانة</button>
            <button class="mp-tabs__btn" data-tab-btn="p3">نظام Bosch ESP 9.3 المتطور</button>
          </div>
          <div class="mp-tabs__root" data-tab-root>

            <div class="mp-feature is-active" data-tab-panel="p1">
              <div class="mp-feature__media"><img src="/assets/img/gn6/perf-1.jpg" alt="محرك 2.0 تيربو وناقل حركة Aisin من 6 سرعات من الجيل الجديد في GN6" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">قوة 2.0 تيربو بسلاسة تامة</h3>
                <p>يجمع GN6 بين محرك 2.0 تيربو سريع الاستجابة وناقل حركة Aisin من 6 سرعات من الجيل الجديد لتسارع سلس وواثق وقيادة اقتصادية — باستهلاك يصل إلى 15.6 كم/لتر.</p>
                <ul class="mp-feature__list">
                  <li><strong>محرك 2.0 تيربو:</strong> قوة متينة ومنقّحة للقيادة داخل المدينة وعلى الطرق السريعة.</li>
                  <li><strong>ناقل حركة Aisin من 6 سرعات من الجيل الجديد:</strong> تنقّل دقيق وسلس بين السرعات لانطلاقة سهلة.</li>
                </ul>
              </div>
            </div>

            <div class="mp-feature" data-tab-panel="p2">
              <div class="mp-feature__media"><img src="/assets/img/gn6/perf-2.jpg" alt="هيكل آمن عالي المتانة ومقصورة هادئة في GN6" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">هيكل وتعليق عالي المتانة</h3>
                <p>يعمل الهيكل الآمن عالي المتانة والمقصورة الهادئة الخالية من الاهتزاز معاً لتقديم قيادة هادئة ومتزنة ومطمئنة.</p>
                <ul class="mp-feature__list">
                  <li><strong>هيكل آمن عالي المتانة + مقصورة هادئة خالية من الاهتزاز:</strong> متانة ورقي في آنٍ واحد.</li>
                  <li><strong>نظام تعليق ماكفرسون من النوع L + عارضة التواء عالية الصلابة:</strong> تعليق متوازن مضبوط للراحة والثبات.</li>
                </ul>
              </div>
            </div>

            <div class="mp-feature" data-tab-panel="p3">
              <div class="mp-feature__media"><img src="/assets/img/gn6/perf-3.jpg" alt="نظام Bosch ESP 9.3 مع فرامل انتظار كهربائية وخاصية الثبات التلقائي في GN6" /></div>
              <div class="mp-feature__body">
                <h3 class="mp-feature__title">تحكّم ذكي وثقة على الطريق</h3>
                <p>تحافظ أنظمة القيادة الذكية على ثبات GN6 وسهولة التحكّم به في كل الظروف.</p>
                <ul class="mp-feature__list">
                  <li><strong>نظام Bosch ESP 9.3 المتطور:</strong> تحكّم متقدم بالثبات لقيادة آمنة.</li>
                  <li><strong>فرامل انتظار كهربائية EPB + خاصية الثبات التلقائي:</strong> سهولة في التوقف والانطلاق والركن.</li>
                  <li><strong>كاميرا رجوع بانورامية بزاوية 360 درجة:</strong> رؤية واضحة وشاملة للمناورة بثقة.</li>
                </ul>
              </div>
            </div>

          </div>
        </div>
      </div>
    </section>

    <!-- ============ SAFETY ============ -->
    <section class="mp-section mp-section--grey" id="safety">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">قيادة آمنة ومريحة</h2>
          <p class="mp-head__sub">تقنيات ذكية وهيكل قوي وآمن يساعدانك على الاستمتاع بكل رحلة بثقة تامة.</p>
          <p class="mp-head__body">من الهيكل الآمن عالي المتانة إلى نظام Bosch ESP 9.3 المتطور، وفرامل الانتظار الكهربائية EPB مع خاصية الثبات التلقائي، وكاميرا الرجوع البانورامية بزاوية 360 درجة، يحيط GN6 بك وبعائلتك بحماية شاملة.</p>
        </header>
        <div class="mp-stoggles">
          <article class="mp-stoggle is-open">
            <button class="mp-stoggle__head" type="button" aria-expanded="true"><span>هيكل آمن عالي المتانة</span><i class="mp-stoggle__icon"></i></button>
            <div class="mp-stoggle__body">
              <div class="mp-stoggle__media"><img src="/assets/img/gn6/safe-1.jpg" alt="هيكل آمن عالي المتانة ومقصورة هادئة في GN6" loading="lazy" /></div>
              <h3 class="mp-stoggle__strap">هيكل آمن عالي المتانة</h3>
              <p class="mp-stoggle__content">هيكل آمن عالي المتانة مع مقصورة هادئة خالية من الاهتزاز لقيادة هادئة وآمنة.</p>
            </div>
          </article>
          <article class="mp-stoggle">
            <button class="mp-stoggle__head" type="button" aria-expanded="false"><span>نظام Bosch ESP 9.3</span><i class="mp-stoggle__icon"></i></button>
            <div class="mp-stoggle__body">
              <div class="mp-stoggle__media"><img src="/assets/img/gn6/safe-2.jpg" alt="نظام Bosch ESP 9.3 في GN6" loading="lazy" /></div>
              <h3 class="mp-stoggle__strap">نظام Bosch ESP 9.3</h3>
              <p class="mp-stoggle__content">تحكّم إلكتروني متقدم بالثبات، مع فرامل انتظار كهربائية EPB وخاصية الثبات التلقائي لمزيد من الراحة والأمان.</p>
            </div>
          </article>
          <article class="mp-stoggle">
            <button class="mp-stoggle__head" type="button" aria-expanded="false"><span>كاميرا رجوع بانورامية 360°</span><i class="mp-stoggle__icon"></i></button>
            <div class="mp-stoggle__body">
              <div class="mp-stoggle__media"><img src="/assets/img/gn6/safe-3.jpg" alt="كاميرا رجوع بانورامية بزاوية 360 درجة في GN6" loading="lazy" /></div>
              <h3 class="mp-stoggle__strap">كاميرا رجوع بانورامية 360°</h3>
              <p class="mp-stoggle__content">نظام كاميرات برؤية محيطية كاملة لرؤية واضحة وشاملة عند الركن والمناورة.</p>
            </div>
          </article>
        </div>
      </div>
    </section>

    <!-- ============ DIMENSIONS ============ -->
    <section class="mp-section" id="dimensions">
      <div class="container">
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">الأبعاد</h2>
          <p class="mp-head__sub">أبعاد فخمة لسيارة متعددة الاستخدامات توفّر المساحة والحضور</p>
        </header>
        <div class="mp-tech-banner"><img src="/assets/img/gn6/dimensions.png" alt="مخطط الأبعاد الخارجية لـ GN6" /></div>
        <div class="mp-stats">
          <div class="mp-stat"><span class="mp-stat__label">الطول</span><span class="mp-stat__value">4780 ملم</span></div>
          <div class="mp-stat"><span class="mp-stat__label">العرض</span><span class="mp-stat__value">1860 ملم</span></div>
          <div class="mp-stat"><span class="mp-stat__label">الارتفاع</span><span class="mp-stat__value">1730 ملم</span></div>
          <div class="mp-stat"><span class="mp-stat__label">قاعدة العجلات</span><span class="mp-stat__value">2810 ملم</span></div>
        </div>
      </div>
    </section>

    <!-- ============ WARRANTY ============ -->
    <section class="mp-section" id="warranty">
      <div class="container">
        <hr class="mp-hr" />
        <header class="mp-head mp-head--left">
          <h2 class="mp-head__title">الضمان</h2>
          <p class="mp-head__sub">ضمان 7 سنوات أو 200,000 كم.</p>
        </header>
        <div class="mp-warranty__links">
          <a class="btn btn--doc" href="https://en.gacmotorsaudi.com/" target="_blank" rel="noopener">أدلة الاستخدام</a>
        </div>
      </div>
    </section>

    <!-- ============ ENQUIRY / CONTACT ============ -->
    <section class="mp-enquiry" id="enquiry" style="background-image:url(''/assets/img/hero-gn6.jpg'')">
      <div class="mp-enquiry__overlay">
        <div class="container mp-enquiry__grid">
          <div class="mp-enquiry__intro">
            <h2 class="mp-enquiry__title">اتصل بنا</h2>
            <p class="mp-enquiry__sub">اطلب عرض سعر أو احجز قيادة تجريبية</p>
            <p class="mp-enquiry__lead">سنتواصل معك خلال 24 ساعة عمل.</p>
            <div class="mp-enquiry__actions">
              <a class="mp-enquiry__action" href="tel:1833334">
                <svg viewBox="0 0 24 24"><path d="M6.6 10.8a15.5 15.5 0 0 0 6.6 6.6l2.2-2.2a1 1 0 0 1 1-.24 11.4 11.4 0 0 0 3.6.58 1 1 0 0 1 1 1V20a1 1 0 0 1-1 1A17 17 0 0 1 3 4a1 1 0 0 1 1-1h3.5a1 1 0 0 1 1 1 11.4 11.4 0 0 0 .58 3.6 1 1 0 0 1-.25 1z"/></svg>
                <span>اتصل بنا</span>
              </a>
              <a class="mp-enquiry__action" href="/contact-us">
                <svg viewBox="0 0 24 24"><path d="M12 2a7 7 0 0 0-7 7c0 5.25 7 13 7 13s7-7.75 7-13a7 7 0 0 0-7-7zm0 9.5A2.5 2.5 0 1 1 12 6.5a2.5 2.5 0 0 1 0 5z"/></svg>
                <span>ابحث عن موقع</span>
              </a>
            </div>
          </div>

          <form class="mp-form" data-form novalidate>
            <div class="field">
              <label>الرسالة</label>
              <textarea rows="3"></textarea>
            </div>
            <div class="field">
              <label>اختر الفرع *</label>
              <select required>
                <option value="">يرجى الاختيار ...</option>
                <option>فرع الرياض</option>
                <option>جي أيه سي جدة، صالة عرض ميدان المليباري</option>
                <option>جي أيه سي جدة، فرع كيلو 3</option>
                <option>فرع الدمام</option>
                <option>جي أيه سي فرع المدينة المنورة</option>
                <option>جي أيه سي فرع خميس مشيط</option>
                <option>جي أيه سي فرع جازان</option>
              </select>
            </div>
            <div class="field">
              <label>اللقب *</label>
              <select required>
                <option value="">يرجى الاختيار ...</option>
                <option>السيد</option><option>الآنسة</option><option>السيدة</option>
              </select>
            </div>
            <div class="field"><label>الاسم الأول *</label><input type="text" required /></div>
            <div class="field"><label>اسم العائلة *</label><input type="text" required /></div>
            <div class="field"><label>البريد الإلكتروني *</label><input type="email" required /></div>
            <div class="field"><label>رقم الهاتف *</label><input type="tel" required /></div>
            <div class="mp-form__dpp">
              <p class="mp-form__dpp-title">بيان الخصوصية وإخلاء المسؤولية القانونية</p>
              أقرّ وأفهم أن معلوماتي قد تتم مشاركتها مع شركة المطوع الكادي للسيارات والشركات التابعة لها أو أطراف أخرى وفقاً لما يقتضيه القانون لأغراض الامتثال أو حملات السلامة أو الاستفسارات الحكومية أو الإجراءات القانونية المماثلة. وقد تتم مشاركة معلوماتي أيضاً لأغراض أبحاث وتطوير المنتجات وإدارة علاقات العملاء لتقديم الدعم. يوضّح بيان الخصوصية كيفية تعامل الشركة و GAC مع المعلومات الشخصية التي تتم مشاركتها معنا في <a href="/privacy-policy">سياسة الخصوصية</a>
            </div>
            <label class="mp-check"><input type="checkbox" /> <span>أرغب في تلقّي أي معلومات تسويقية أو مشاركة معلوماتي مع أطراف ثالثة بغرض تزويدي بالمعلومات التسويقية.</span></label>
            <button class="mp-form__submit" type="submit">إرسال</button>
          </form>
        </div>
      </div>
    </section>

  </main>

  <!-- =================================================
       FOOTER
       ================================================= -->

  <!-- Gallery lightbox viewer -->
  <div class="mp-lightbox" data-lightbox aria-hidden="true" role="dialog" aria-label="عارض الصور">
    <button class="mp-lightbox__close" data-lb-close aria-label="إغلاق">×</button>
    <button class="mp-lightbox__nav mp-lightbox__nav--prev" data-lb-prev aria-label="الصورة السابقة">‹</button>
    <img class="mp-lightbox__img" data-lb-img src="" alt="" />
    <button class="mp-lightbox__nav mp-lightbox__nav--next" data-lb-next aria-label="الصورة التالية">›</button>
    <div class="mp-lightbox__count" data-lb-count></div>
  </div>
'
WHERE Slug = N'gn6';

-- ============================================================
-- (B) Replace gn6 images: Hero + Gallery thumbnail -> real GN6 art
-- ============================================================
DECLARE @gn6 INT = (SELECT Id FROM dbo.Vehicles WHERE Slug = N'gn6');

DELETE FROM dbo.VehicleImages WHERE VehicleId = @gn6;

INSERT INTO dbo.VehicleImages (VehicleId, Kind, Path, Alt_En, Alt_Ar, SortOrder)
VALUES
    (@gn6, 0, N'/assets/img/hero-gn6.jpg', N'GAC GN6', N'جي أيه سي GN6', 0),  -- 0 = Hero
    (@gn6, 1, N'/assets/img/m-gn6.png',    N'GN6',     N'GN6',        0);  -- 1 = Gallery (card thumb)

COMMIT TRANSACTION;

-- Verify (optional)
SELECT Slug, Name_En, Tagline_En, Tagline_Ar,
       LEN(BodyHtml_En) AS BodyEnLen, LEN(BodyHtml_Ar) AS BodyArLen
FROM dbo.Vehicles WHERE Slug = N'gn6';
SELECT Kind, Path, Alt_En FROM dbo.VehicleImages
WHERE VehicleId = (SELECT Id FROM dbo.Vehicles WHERE Slug = N'gn6') ORDER BY Kind;
GO