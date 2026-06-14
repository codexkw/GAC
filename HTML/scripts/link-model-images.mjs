/* Make the car image (.lineup-card__media) on models.html a link to that model's
   page, using the same target as the card's "Explore" button. Idempotent. */
import { readFileSync, writeFileSync } from 'node:fs';

const FILE = 'models.html';
let src = readFileSync(FILE, 'utf8');

// Process each lineup-card. Each card is self-contained on one line.
src = src.replace(/<div class="lineup-card"[^>]*>[\s\S]*?<\/div>\s*<\/div>\s*<\/div>/g, (card) => {
  // already converted?
  if (/<a class="lineup-card__media"/.test(card)) return card;
  const hrefMatch = card.match(/class="btn btn--accent" href="([^"]+)"/);
  if (!hrefMatch) return card;
  const href = hrefMatch[1];
  return card.replace(
    /<div class="lineup-card__media">([\s\S]*?)<\/div>(<div class="lineup-card__body">)/,
    `<a class="lineup-card__media" href="${href}" aria-label="View details">$1</a>$2`
  );
});

writeFileSync(FILE, src, 'utf8');

const linked = (src.match(/<a class="lineup-card__media"/g) || []).length;
console.log(`Linked ${linked} car images in ${FILE}.`);
