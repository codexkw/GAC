/* Append ?v=N to the styles.css and includes.js links on every page so
   browsers/CDN can't serve stale copies. Idempotent: replaces any existing
   ?v=… and adds one if absent. Bump V and re-run after asset changes. */
import { readFileSync, writeFileSync, readdirSync } from 'node:fs';

const V = '3';
const files = readdirSync('.').filter(f => f.endsWith('.html') && f !== 'live-empow-sport.html');
const report = [];

for (const f of files) {
  let src = readFileSync(f, 'utf8');
  const before = src;
  // styles.css (strip any existing ?v= then add)
  src = src.replace(/href="assets\/css\/styles\.css(?:\?v=[^"]*)?"/g, `href="assets/css/styles.css?v=${V}"`);
  // includes.js
  src = src.replace(/src="assets\/js\/includes\.js(?:\?v=[^"]*)?"/g, `src="assets/js/includes.js?v=${V}"`);
  if (src !== before) { writeFileSync(f, src, 'utf8'); report.push(`OK   ${f}`); }
  else report.push(`--   ${f}`);
}
console.log(report.join('\n'));
console.log(`\nStamped ?v=${V} across ${files.length} files.`);
