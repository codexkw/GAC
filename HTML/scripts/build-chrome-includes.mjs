/* One-time refactor: replace the inline <header>+drawer and <footer> on every
   clone page with <div data-include> placeholders, and swap the main.js script
   tag for includes.js (which injects the partials then loads main.js).
   Idempotent: pages already converted are skipped. */
import { readFileSync, writeFileSync, readdirSync } from 'node:fs';

const HEADER_RE = /<header class="gac-header"[\s\S]*?<\/header>\s*(?:<!--[\s\S]*?-->\s*)*<div class="drawer" data-drawer>[\s\S]*?<\/nav>\s*<\/div>\s*<\/div>/;
const FOOTER_RE = /<footer class="site-footer">[\s\S]*?<\/footer>/;
const SCRIPT_RE = /<script src="assets\/js\/main\.js"><\/script>/;

const HEADER_SLOT = '<div data-include="header"></div>';
const FOOTER_SLOT = '<div data-include="footer"></div>';
const SCRIPT_TAG  = '<script src="assets/js/includes.js"></script>';

const files = readdirSync('.').filter(f => f.endsWith('.html') && f !== 'live-empow-sport.html');
const report = [];

for (const f of files) {
  let src = readFileSync(f, 'utf8');
  const already = src.includes('data-include="header"');
  const issues = [];
  let out = src;

  if (!already) {
    if ((out.match(/<header class="gac-header"/g) || []).length !== 1) issues.push('header count != 1');
    else if (!HEADER_RE.test(out)) issues.push('header+drawer regex no match');
    else out = out.replace(HEADER_RE, HEADER_SLOT);

    if ((out.match(/<footer class="site-footer">/g) || []).length !== 1) issues.push('footer count != 1');
    else if (!FOOTER_RE.test(out)) issues.push('footer regex no match');
    else out = out.replace(FOOTER_RE, FOOTER_SLOT);
  }

  if (SCRIPT_RE.test(out)) out = out.replace(SCRIPT_RE, SCRIPT_TAG);
  else if (!out.includes('assets/js/includes.js')) issues.push('main.js script tag not found');

  if (issues.length) { report.push(`SKIP ${f}: ${issues.join('; ')}`); continue; }
  if (out !== src) { writeFileSync(f, out, 'utf8'); report.push(`OK   ${f}`); }
  else report.push(`--   ${f} (already converted)`);
}

console.log(report.join('\n'));
console.log(`\nConverted/verified ${files.length} files.`);
