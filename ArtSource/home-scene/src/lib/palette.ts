// ⚠️ Every hex here is either measured off her drawings (the logo, `slipper with hit`, the
// login buttons) or is Zack's own colour in the shipping game. CLAUDE.md § 6.4 forbids blue in
// the front end and this scene sits under the whole hub, so nothing here may carry more blue
// than red. Shadow tones are warm (mauve, deep red), never grey.

export const P = {
  // The logo's seven, CLAUDE.md § 6.4 table.
  deepRed: '#980715',
  honey: '#FCD39F',
  chartreuse: '#D6CE01',
  persimmon: '#FD8041',
  golden: '#F5B521',
  rimRed: '#C32E0D',
  army: '#B3A828',
  ink: '#1C0F06',

  // Outline for figures: a step deeper than the logo's deep red so a character reads in front
  // of a set drawn in the logo's own outline.
  line: '#6E0410',
  lineSoft: '#A3301C',

  // Her stripe and shine tones, sampled off the M and the slipper.
  persimmonStripe: '#E8693A',
  chartreuseShine: '#B5AE2A',
  goldenSwirl: '#EE9A2E',

  // Her login buttons.
  limeTop: '#BCD34A',
  limeFace: '#A9C23A',
  limeFloor: '#5E7A4E',

  // Zack, UiTheme.HeroElectric / HeroElectricBright and ViewmodelArms.SkinZack.
  electric: '#E8F53A',
  electricBright: '#F6FFA0',
  boltCore: '#FFFDE8',
  skin: '#C77A45',
  skinShade: '#9A5230',
  skinLight: '#E2A06A',
  hair: '#2E130D',
  hairShade: '#1E0A07',
  hairLight: '#5E2618',
  jacket: '#D6CE01',
  jacketShade: '#9E9412',
  jacketLight: '#EEE86A',
  tee: '#FCD39F',
  teeShade: '#E0A877',
  shorts: '#7A0E1E',
  shortsShade: '#56060F',
  eye: '#2A0D08',
};

// ---------------------------------------------------------------- the set's relight (2026-10-21)
//
// ⚠️ THE SET SITS UNDER THE OWNER'S HAND-MADE HOME UI, WHOSE PANELS ARE MAROON (#56071D). The old
// deep red sunset swallowed them (owner, 2026-10-21: recolour the HOME loops to suit the new UI;
// keep the characters' own colours, change only the set). Her board was drawn on a light page, so
// the set goes to a light golden hour: every SET colour passes through `rl`, which lifts its
// lightness into a high band, softens its chroma and eases reds toward peach. Zack, his lightning,
// the actors and the TUMP logo do not pass through it. One dial, so the look is tuned in one place.
export const RELIGHT = {
  lMin: 0.56,     // OKLab lightness of the darkest set colour after the relight
  lMax: 0.94,     // ... and of the lightest
  chroma: 0.62,   // chroma kept
  hueTo: 62,      // degrees: the warm peach the reds and oranges ease toward
  huePull: 0.32,  // how far they ease
};

const srgbToLin = (c: number) => (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
const linToSrgb = (c: number) => (c <= 0.0031308 ? 12.92 * c : 1.055 * c ** (1 / 2.4) - 0.055);

function toOklab(hex: string): [number, number, number] {
  const n = parseInt(hex.slice(1), 16);
  const r = srgbToLin(((n >> 16) & 255) / 255), g = srgbToLin(((n >> 8) & 255) / 255), b = srgbToLin((n & 255) / 255);
  const l = Math.cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
  const m = Math.cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
  const s = Math.cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
  return [0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s,
    1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s,
    0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s];
}

function fromOklab(L: number, a: number, b: number): string {
  const l = (L + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m = (L - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s = (L - 0.0894841775 * a - 1.291485548 * b) ** 3;
  const rgb = [4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
    -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s];
  return '#' + rgb.map((c) => Math.round(Math.min(1, Math.max(0, linToSrgb(c))) * 255).toString(16).padStart(2, '0')).join('').toUpperCase();
}

const relit = new Map<string, string>();
/** A set colour in the golden-hour relight. Accepts #RRGGBB; anything else passes through. */
export function rl(hex: string): string {
  if (!/^#[0-9a-fA-F]{6}$/.test(hex)) return hex;
  const hit = relit.get(hex);
  if (hit) return hit;
  const [L, a, b] = toOklab(hex);
  const C = Math.hypot(a, b);
  let h = (Math.atan2(b, a) * 180) / Math.PI;
  if (h < 0) h += 360;
  // reds, oranges and yellows (hue -40..110) ease toward the peach; olives and others keep theirs
  const warm = h > 320 || h < 110;
  if (warm) {
    const from = h > 320 ? h - 360 : h;
    h = from + (RELIGHT.hueTo - from) * RELIGHT.huePull;
  }
  const L2 = RELIGHT.lMin + (RELIGHT.lMax - RELIGHT.lMin) * Math.min(1, Math.max(0, L));
  const C2 = C * RELIGHT.chroma;
  const out = fromOklab(L2, C2 * Math.cos((h * Math.PI) / 180), C2 * Math.sin((h * Math.PI) / 180));
  relit.set(hex, out);
  return out;
}

/** The logo palette as the SET paints it (relit). Characters keep using `P`. */
export const PS = Object.fromEntries(Object.entries(P).map(([k, v]) => [k, rl(v)])) as typeof P;
