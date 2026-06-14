import { readFileSync, writeFileSync, readdirSync } from 'node:fs';

const DRAWER = `<div class="drawer" data-drawer>
    <div class="drawer__panel">
      <button class="drawer__close" data-drawer-close aria-label="Close menu">×</button>
      <nav class="drawer__nav">
        <a href="index.html">Home</a>
        <div class="drawer__group" data-drawer-group>
          <button type="button" class="drawer__toggle" aria-expanded="false">Models <span class="drawer__chev" aria-hidden="true">▾</span></button>
          <div class="drawer__sub">
            <a href="models.html">All Models</a>
            <a href="gs8traveller.html">GS8 Traveller</a>
            <a href="gs8.html">GS8</a>
            <a href="gs3emzoom.html">GS3 EMZOOM</a>
            <a href="emkoo.html">EMKOO</a>
            <a href="empow.html">EMPOW</a>
            <a href="m8.html">M8</a>
            <a href="empow-sport.html">EMPOW Sport</a>
            <a href="aion-v.html">AION V</a>
            <a href="aion-es.html">AION ES</a>
            <a href="hyptec-ht.html">HYPTEC HT</a>
            <a href="gs4.html">GS4</a>
          </div>
        </div>
        <div class="drawer__group" data-drawer-group>
          <button type="button" class="drawer__toggle" aria-expanded="false">Owners <span class="drawer__chev" aria-hidden="true">▾</span></button>
          <div class="drawer__sub">
            <a href="book-a-service.html">Book a Service</a>
            <a href="cost-of-service.html">Cost of Service</a>
            <a href="warranty.html">Warranty</a>
            <a href="recall-enquiry.html">Recall</a>
          </div>
        </div>
        <div class="drawer__group" data-drawer-group>
          <button type="button" class="drawer__toggle" aria-expanded="false">Shopping Tools <span class="drawer__chev" aria-hidden="true">▾</span></button>
          <div class="drawer__sub">
            <a href="book-a-test-drive.html">Book a Test Drive</a>
            <a href="request-a-quote.html">Request a Quote</a>
          </div>
        </div>
        <a href="contact-us.html">Locations</a>
        <div class="drawer__group" data-drawer-group>
          <button type="button" class="drawer__toggle" aria-expanded="false">More <span class="drawer__chev" aria-hidden="true">▾</span></button>
          <div class="drawer__sub">
            <a href="fleet.html">Fleet Sales</a>
            <a href="finance.html">Finance</a>
          </div>
        </div>
        <a href="https://api.whatsapp.com/send/?phone=966138212500" target="_blank" rel="noopener">WhatsApp</a>
        <a href="tel:8007525252">Call 800 7 525252</a>
      </nav>
    </div>
  </div>`;

const RE = /<div class="drawer" data-drawer>[\s\S]*?<\/nav>\s*<\/div>\s*<\/div>/;

const files = readdirSync('.').filter(f => f.endsWith('.html') && f !== 'live-empow-sport.html');
let changed = 0, skipped = [];
for (const f of files) {
  const src = readFileSync(f, 'utf8');
  const matches = src.match(/<div class="drawer" data-drawer>/g);
  if (!matches) { continue; }
  if (matches.length !== 1) { skipped.push(`${f} (found ${matches.length} drawers)`); continue; }
  if (!RE.test(src)) { skipped.push(`${f} (regex did not match end)`); continue; }
  const out = src.replace(RE, DRAWER);
  if (out === src) { skipped.push(`${f} (no change)`); continue; }
  writeFileSync(f, out, 'utf8');
  changed++;
}
console.log(`Rebuilt drawer in ${changed} files.`);
if (skipped.length) console.log('Skipped:\n  ' + skipped.join('\n  '));
