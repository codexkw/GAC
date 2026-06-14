// Downloads the real GAC Motor Saudi assets into assets/img/ for the local clone.
// Run:  node scripts/download-assets.mjs
import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const ND = 'https://images.netdirector.co.uk/gforces-auto/image/upload';
const CF = 'https://d2638j3z8ek976.cloudfront.net/f99c41942d9c48c1d40d8b393f62e1e5f80b1510/1777300604/images';
const HERO = 'w_1600,h_900,q_auto:best,c_fill,f_auto,fl_lossy';
const JB = 'w_420,h_280,q_auto,c_fill,f_auto,fl_lossy';
const TILE = 'w_800,h_600,q_auto,c_fill,f_auto,fl_lossy';
const SQ = 'w_600,h_600,q_auto,c_fill,f_auto,fl_lossy';

const assets = {
  // brand
  'logo.png': `${CF}/logo.png`,
  'aljomaih.png': `${CF}/al-jomaih-automotive.png`,
  // hero key visuals
  'hero-gs4.jpg':            `${ND}/${HERO}/auto-client/350345aa86331c6269750643cd96f73b/mvm_1887_edit_copy.jpg`,
  'hero-hyptec-ht.jpg':      `${ND}/${HERO}/auto-client/083a25d93585ca847b9de8778614761e/hyptech_ht_kv45.jpg`,
  'hero-aion-v.jpg':         `${ND}/${HERO}/auto-client/5c4991dcf9224026b2fb3392641e7fe4/aion_v_2_copy_2.jpg`,
  'hero-aion-es.jpg':        `${ND}/${HERO}/auto-client/fefc449780006c6ac5b485c2f9368ada/exterior_silver_1.jpg`,
  'hero-empow-sport.jpg':    `${ND}/${HERO}/auto-client/72279a426e96f39481090c7ef7c98623/pr_image6.jpg`,
  'hero-gs8-traveller.png':  `${ND}/${HERO}/auto-client/905676371586523b590924f8dfc76534/1920x1080_website_english_min.png`,
  'hero-m8.png':             `${ND}/${HERO}/auto-client/6c15b6f21faf7d12dccb90f323cd6909/p_1_01_pc.png`,
  'hero-gs3-emzoom.jpg':     `${ND}/${HERO}/auto-client/7e9b93cc3fcb7bbcb846a823a4daf7f1/gs3_zoom_image_1_min.jpg`,
  // model thumbnails (jellybeans)
  'm-gs8-traveller.png': `${ND}/${JB}/auto-client/418622db834c5c6d2c73344dcc989b88/traveller_jellybean_min.png`,
  'm-hyptec-ht.png':     `${ND}/${JB}/auto-client/e0f18353d080f9daf9751d686f068207/hyptec_ht_jellybean.png`,
  'm-aion-v.png':        `${ND}/${JB}/auto-client/fd2c1618a6f60de6108a90f3a78d44f4/aion_v_jellybean.png`,
  'm-aion-es.png':       `${ND}/${JB}/auto-client/1cfa81d0294c1bec8bdfc9fd5477f3c9/aion_es_jellybean.png`,
  'm-gs8.jpg':           `${ND}/${JB}/auto-client/d708f564659225690b9d40977ab3593b/3_2_trim.jpg`,
  'm-gs4.png':           `${ND}/${JB}/auto-client/0eb3c42491ce39f8698378276bc9177e/homepage_dropdown_gs4_max.png`,
  'm-gs3-emzoom.png':    `${ND}/${JB}/auto-client/6826e56f0f8209877300353e8474147e/jellybean_gs3_emzoom_min.png`,
  'm-m8.png':            `${ND}/${JB}/auto-client/f6a7f1158529cd7e23c1d03095cc529b/24_m8_jellybean_min.png`,
  'm-emkoo.png':         `${ND}/${JB}/auto-client/93b1b417402986e336debab4c932be23/homepage_dropdown_emkoo2_min.png`,
  'm-empow.png':         `${ND}/${JB}/auto-client/4cea33fd9b51c952302204197eb67c9f/empow_jellybean.png`,
  'm-empow-sport.png':   `${ND}/${JB}/auto-client/95d6517b1af7eaf9aa6d550a17e1be32/homepage_dropdown_empow_1_copy.png`,
  // featured band
  'feature-gs8-traveller.jpg': `${ND}/w_1200,h_675,q_auto:best,c_fill,f_auto,fl_lossy/auto-client/eeb4d0a4006c9a456e875b328bc113f2/img_5799.jpg`,
  // promo / dual tiles
  'tile-offers.jpg':    `${ND}/${TILE}/auto-client/84c15ffb9d85e61dff5b50e02e8cc9c9/img_6643.jpg`,
  'tile-locations.jpg': `${ND}/${TILE}/auto-client/0b051e02dd5023832643674822747b70/3_2_jump02.jpg`,
  'tile-service.jpg':   `${ND}/${TILE}/auto-client/20bdb0412d574b3305f94ed7912454fb/3_2_jump03.jpg`,
  // news
  'news-empow.jpg':     `${ND}/${SQ}/auto-client/8fcfa34f9e2a947f51c7e73d0e793f6e/raj09231.jpg`,
  'news-training.jpg':  `${ND}/${SQ}/auto-client/1a2c3b6eb8db2c6144e9f46d2b9d3391/420a9914.jpg`,
  'news-gs3-award.jpg': `${ND}/q_auto,c_crop,f_auto,fl_lossy,x_0,y_168,w_1080,h_1080/${SQ}/auto-client/8f36f7069d893850845c06c4d5e85bf3/0731_gs3emzoom_is_ranked_no.1_in_the_2024_china_automobile_quality_ranking_for_compact_suvs_gs3_emzoom_2024_suv_.jpg`,
  'news-m8.png':        `${ND}/${SQ}/auto-client/6c15b6f21faf7d12dccb90f323cd6909/p_1_01_pc.png`,
};

const outDir = resolve(dirname(fileURLToPath(import.meta.url)), '..', 'assets', 'img');
await mkdir(outDir, { recursive: true });

const entries = Object.entries(assets);
let ok = 0, fail = 0;
async function dl([name, url]) {
  try {
    const res = await fetch(url, { headers: { 'User-Agent': 'Mozilla/5.0', 'Referer': 'https://en.gacmotorsaudi.com/' } });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const buf = Buffer.from(await res.arrayBuffer());
    await writeFile(resolve(outDir, name), buf);
    ok++; console.log(`  ok  ${name}  (${(buf.length/1024).toFixed(0)} KB)`);
  } catch (e) {
    fail++; console.log(`  FAIL ${name}  -> ${e.message}`);
  }
}
// 4 at a time
for (let i = 0; i < entries.length; i += 4) {
  await Promise.all(entries.slice(i, i + 4).map(dl));
}
console.log(`\nDone. ${ok} downloaded, ${fail} failed. -> ${outDir}`);
