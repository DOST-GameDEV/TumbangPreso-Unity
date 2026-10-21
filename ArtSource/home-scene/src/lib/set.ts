import { P } from './palette';
import { LOOK } from './look';

// ⚠️ THE SET'S OWN PALETTE, ONE NAMED COLOUR PER ELEMENT. Zack's loop sits under the owner's
// hand-made HOME UI: maroon panels #56071D, menu #A31A24, tiles #F0B947 on a #BA490D..#EB8B33
// slab, SHOP #469678, TASK #F19910, PLAY #A5B93D/#80B04A, text #E1D2B7, peach inlays #FCD39F.
// The set is drawn so those read: the sky behind the top bar sits well above the maroon in
// value, and no large area of the set is gold, orange or deep red. The owner rejected a filter
// over the old colours (2026-10-21), so every colour here is chosen, not derived.
// Zack, his lightning, the actors, the lata and the TUMP logo keep their own colours (`P`).
// No blue, navy or cold grey in any layer (Front_End_Design.md).

type Tower = { fill: string; shade: string; win: string; lit: string };
type CloudTone = { fill: string; under: string; top: string };
type Garment = { fill: string; shade: string; stripe?: string };

export type SetLook = {
  /** The frame behind everything, and the first sky band. */
  bg: string;
  /** Nine bands, top to horizon, over the wide (west, the sun's side). */
  sky: string[];
  /** Seven bands, top to horizon, behind the reverse (east, the moon's side). */
  skyEast: string[];
  star: string;
  bird: string;
  cloudHigh: CloudTone;
  cloudLow: CloudTone;
  bankUnder: string;
  hills: string;
  /** The sun glow, centre out (four stops). */
  glow: [string, string, string, string];
  /** The parol-burst sun: fill, swirl, shade. */
  parol: { fill: string; swirl: string; shade: string };
  towerFar: Tower;
  towerMid: Tower;
  towerNear: Tower;
  /** The low sun's catch on every edge facing it. */
  rim: string;
  beacon: string;
  parapet: { face: string; seam: string; top: string; lip: string };
  chalk: string;
  floor: { base: string; band: string; seam: string; chalk: string };
  tank: { leg: string; body: string; shade: string; cap: string; mark: string };
  pole: string;
  /** Flag and laundry strings. */
  string: string;
  flags: [string, string][];
  peg: string;
  laundry: Garment[];
  plant: { leaf: [string, string, string, string]; pot: string; potShade: string; pot2: string; pot2Shade: string; band: string };
  chair: { back: string; seat: string; leg: string };
  dust: [string, string];
  motes: [string, string];
  shadow: string;
  /** Darkening washes over the frame (powered close-up, the hand). */
  dim: string;
  vignette: string;
  /** The paper grain's tint, 0..1 rgb. */
  grain: [number, number, number];
  east: { haze: string; front: string; side: string; top: string; crown: string; win: string; lit: string };
  court: { parapet: string; parapetTop: string; parapetBase: string; floor: string; seam: string; chalk: string; tank: { leg: string; body: string; shade: string; top: string } };
  side: Tower & { haze: string };
  /** The soft dark blocks passing in front of the chase camera. */
  fg: string;
};

const logoFlags: [string, string][] = [
  [P.chartreuse, '#AFA800'],
  [P.persimmon, '#D9622B'],
  [P.honey, '#E6B57C'],
  [P.golden, '#D69414'],
  [P.rimRed, '#9A220A'],
];

/** The original dusk, kept as the reference the looks below are judged against. */
const dusk: SetLook = {
  bg: '#560815',
  sky: ['#560815', '#6A0B1A', '#84111D', '#A41F1F', '#C63520', '#E05726', '#EF7B34', '#F8A043', '#FCC05A'],
  skyEast: ['#3A0612', '#4C0917', '#661020', '#851A25', '#A82E2A', '#C84C33', '#DA6A3C'],
  star: '#FFE7A8',
  bird: '#3B0710',
  cloudHigh: { fill: '#B8321F', under: '#EE8A42', top: '#9A2419' },
  cloudLow: { fill: '#EE8540', under: '#FFC45C', top: '#D96E34' },
  bankUnder: '#FFD06A',
  hills: '#F09A50',
  glow: ['#FFE08A', '#FFC45A', '#FFA84A', '#FF9440'],
  parol: { fill: P.golden, swirl: P.goldenSwirl, shade: '#E0951C' },
  towerFar: { fill: '#E27E42', shade: '#D66F3A', win: '#EC9150', lit: '#FFE3A0' },
  towerMid: { fill: '#B63622', shade: '#962A1B', win: '#C8472A', lit: '#FFD45E' },
  towerNear: { fill: '#7E1519', shade: '#621014', win: '#94261E', lit: '#FFC94A' },
  rim: P.golden,
  beacon: P.rimRed,
  parapet: { face: '#A8492C', seam: '#8E3C25', top: '#D0724A', lip: P.golden },
  chalk: '#FFD9A8',
  floor: { base: '#CC8354', band: '#E09A5E', seam: '#B86A45', chalk: '#FFE3B8' },
  tank: { leg: '#3E0A10', body: '#77701A', shade: '#5E5914', cap: '#9A9124', mark: '#A89E2C' },
  pole: '#4A0A12',
  string: '#3A0610',
  flags: logoFlags,
  peg: P.golden,
  laundry: [
    { fill: P.golden, shade: '#D69414', stripe: P.rimRed },
    { fill: P.honey, shade: '#E6B57C', stripe: P.persimmon },
    { fill: P.chartreuse, shade: '#AFA800' },
    { fill: P.persimmon, shade: '#D9622B' },
    { fill: '#FFE7C0', shade: '#E8C696' },
  ],
  plant: { leaf: ['#9A9020', '#7C7418', '#6E6814', '#5C5610'], pot: P.rimRed, potShade: '#8E220A', pot2: P.persimmon, pot2Shade: '#C9602A', band: P.persimmon },
  chair: { back: P.rimRed, seat: '#E0502A', leg: '#9A220A' },
  dust: ['#F2C58E', '#E6AE74'],
  motes: ['#FFD98A', '#FFF3C8'],
  shadow: '#8E3E2C',
  dim: '#3A0610',
  vignette: '#2A0508',
  grain: [0.42, 0.16, 0.06],
  east: { haze: '#C8603A', front: '#F09A58', side: '#C4552F', top: '#FFD27A', crown: '#F2A160', win: '#D9774A', lit: '#FFE9A8' },
  court: {
    parapet: '#E4905A', parapetTop: '#F6B97C', parapetBase: '#C4703F', floor: '#E8A56B', seam: '#D08A58', chalk: '#FFEAC4',
    tank: { leg: '#6E3A22', body: '#A09528', shade: '#7C7420', top: '#D8CC4A' },
  },
  side: { haze: '#C8603A', fill: '#A83220', shade: '#8A261A', win: '#C8472A', lit: '#FFD45E' },
  fg: '#3A0A10',
};

/**
 * UBE DUSK. The evening goes to ube and dusty rose: a mauve sky lifting to a warm rose haze, plum
 * towers, a mauve-taupe concrete deck. Every gold, orange and green on her board sits on its
 * complement, and the maroon reads as the deepest, richest thing on the screen.
 */
const ube: SetLook = {
  bg: '#74486A',
  sky: ['#74486A', '#80506F', '#8D5973', '#9A6277', '#A86C7A', '#B6787D', '#C48580', '#D09484', '#DAA48A'],
  skyEast: ['#5E3858', '#6C4062', '#7C4A6A', '#8E5670', '#A26474', '#B47278', '#C2827E'],
  star: '#FFE7C8',
  bird: '#3E2038',
  cloudHigh: { fill: '#9B6680', under: '#D8A08C', top: '#87587A' },
  cloudLow: { fill: '#C88A88', under: '#F0C090', top: '#B07680' },
  bankUnder: '#F2C694',
  hills: '#C08A88',
  glow: ['#FFE2A8', '#FFC98A', '#F3AE8A', '#E0988A'],
  parol: { fill: P.golden, swirl: P.goldenSwirl, shade: '#E0951C' },
  towerFar: { fill: '#B0788A', shade: '#A06C80', win: '#BC8694', lit: '#FFE3A8' },
  towerMid: { fill: '#8A5274', shade: '#76466A', win: '#9A5F80', lit: '#FFD27A' },
  towerNear: { fill: '#5E3352', shade: '#4E2A46', win: '#6E3D5E', lit: '#FFC96A' },
  rim: '#F2B860',
  beacon: '#E0503A',
  parapet: { face: '#8C6470', seam: '#76525E', top: '#B08690', lip: '#F2C27A' },
  chalk: '#FFE6C8',
  floor: { base: '#A07E7E', band: '#B28E8A', seam: '#8C6C6E', chalk: '#FFE8CC' },
  tank: { leg: '#3E2038', body: '#6F6A2A', shade: '#57531E', cap: '#8E8836', mark: '#9E9840' },
  pole: '#3E2038',
  string: '#3E2038',
  flags: logoFlags,
  peg: '#F2B860',
  laundry: [
    { fill: P.golden, shade: '#D69414', stripe: P.rimRed },
    { fill: P.honey, shade: '#E6B57C', stripe: P.persimmon },
    { fill: P.chartreuse, shade: '#AFA800' },
    { fill: P.persimmon, shade: '#D9622B' },
    { fill: '#E8B4A0', shade: '#CC9888' },
  ],
  plant: { leaf: ['#9A9020', '#7C7418', '#6E6814', '#5C5610'], pot: '#C0563A', potShade: '#9A4430', pot2: P.persimmon, pot2Shade: '#C9602A', band: '#E08050' },
  chair: { back: P.rimRed, seat: '#E0502A', leg: '#9A220A' },
  dust: ['#C8A0A0', '#B48C8E'],
  motes: ['#FFE0A8', '#FFF0D0'],
  shadow: '#4E2A46',
  dim: '#2E1830',
  vignette: '#2E1830',
  grain: [0.3, 0.16, 0.26],
  east: { haze: '#A87484', front: '#C49088', side: '#986678', top: '#F0C090', crown: '#D8A090', win: '#B07C84', lit: '#FFE6A8' },
  court: {
    parapet: '#B08682', parapetTop: '#CCA092', parapetBase: '#8C626A', floor: '#A88482', seam: '#926E70', chalk: '#FFEAD0',
    tank: { leg: '#4E2A46', body: '#8E8836', shade: '#6F6A2A', top: '#C8C060' },
  },
  side: { haze: '#A87484', fill: '#8A5274', shade: '#76466A', win: '#9A5F80', lit: '#FFD27A' },
  fg: '#2E1830',
};

/**
 * PANDAN NIGHT. The fiesta after dark: a pandan-olive sky over a city of lit windows, a moss and
 * khaki deck, the parol glowing gold. The maroon panels and the red menu sit on their complement;
 * the windows and the parol carry the warmth.
 */
const pandan: SetLook = {
  bg: '#46522A',
  sky: ['#46522A', '#4D5A2D', '#556230', '#5E6A33', '#6A7436', '#787E3A', '#88883E', '#9A9244', '#AC9C4A'],
  skyEast: ['#3A4422', '#424C26', '#4C5629', '#56602C', '#626A30', '#707434', '#807E38'],
  star: '#FFF0B0',
  bird: '#232A12',
  cloudHigh: { fill: '#5E6A34', under: '#9A9450', top: '#4E5A2C' },
  cloudLow: { fill: '#8A8840', under: '#C8B45A', top: '#76783A' },
  bankUnder: '#D0BC60',
  hills: '#8C8A44',
  glow: ['#FFE89A', '#F0D070', '#C8B056', '#A89A48'],
  parol: { fill: P.golden, swirl: P.goldenSwirl, shade: '#E0951C' },
  towerFar: { fill: '#7E7E40', shade: '#727438', win: '#8A8848', lit: '#FFE7A0' },
  towerMid: { fill: '#545E2E', shade: '#4A5228', win: '#5E6834', lit: '#FFD860' },
  towerNear: { fill: '#343C1C', shade: '#2A3216', win: '#3E4622', lit: '#FFC850' },
  rim: '#E8D070',
  beacon: '#E0503A',
  parapet: { face: '#6A6A3A', seam: '#565A30', top: '#8E8A4C', lip: '#E8D070' },
  chalk: '#F6EEC0',
  floor: { base: '#7E7448', band: '#8E8452', seam: '#6A6240', chalk: '#F4EAC0' },
  tank: { leg: '#2A3216', body: '#6E4E34', shade: '#584028', cap: '#8A6844', mark: '#9A7850' },
  pole: '#2A3216',
  string: '#232A12',
  flags: logoFlags,
  peg: '#E8D070',
  laundry: [
    { fill: P.golden, shade: '#D69414', stripe: P.rimRed },
    { fill: P.honey, shade: '#E6B57C', stripe: P.persimmon },
    { fill: '#E07850', shade: '#B85C3C' },
    { fill: P.persimmon, shade: '#D9622B' },
    { fill: '#E8C8A0', shade: '#C8A880' },
  ],
  plant: { leaf: ['#A6B03A', '#86902C', '#6A7420', '#56601A'], pot: '#B0502E', potShade: '#8A3E24', pot2: '#D07040', pot2Shade: '#A85A30', band: '#D07040' },
  chair: { back: P.rimRed, seat: '#E0502A', leg: '#9A220A' },
  dust: ['#A89A68', '#968A5C'],
  motes: ['#FFE89A', '#FFF6CC'],
  shadow: '#3E3C20',
  dim: '#1E2410',
  vignette: '#1E2410',
  grain: [0.22, 0.24, 0.08],
  east: { haze: '#7A7A40', front: '#9A9450', side: '#6E6E38', top: '#E0C870', crown: '#B0A458', win: '#8A8646', lit: '#FFE7A0' },
  court: {
    parapet: '#8E8650', parapetTop: '#B0A464', parapetBase: '#6E6A3C', floor: '#968A58', seam: '#7E7448', chalk: '#F4EAC0',
    tank: { leg: '#2A3216', body: '#6E4E34', shade: '#584028', top: '#9A7850' },
  },
  side: { haze: '#7A7A40', fill: '#545E2E', shade: '#4A5228', win: '#5E6834', lit: '#FFD860' },
  fg: '#1E2410',
};

/**
 * ROSAS AFTERNOON. Late afternoon in dusty rose and sand: a raspberry-rose sky warming to a
 * peach haze, clay-rose towers, a sun-warmed pink concrete deck. Lighter than the other two, so
 * the maroon panels sit on it like her board did on its page, without going pale.
 */
const rosas: SetLook = {
  bg: '#9A5E6C',
  sky: ['#9A5E6C', '#A4666F', '#AE6E72', '#B87874', '#C28276', '#CC8E7A', '#D49A7E', '#DCA684', '#E2B28A'],
  skyEast: ['#7E4A5A', '#8A5260', '#965A66', '#A2646C', '#B07072', '#BC7C76', '#C8887A'],
  star: '#FFEACC',
  bird: '#5A2A34',
  cloudHigh: { fill: '#B4747C', under: '#E8B092', top: '#A0666E' },
  cloudLow: { fill: '#D69A88', under: '#F4CC9C', top: '#C48880' },
  bankUnder: '#F4CE9E',
  hills: '#CC9286',
  glow: ['#FFE6B0', '#FFD098', '#F4B490', '#E89E88'],
  parol: { fill: P.golden, swirl: P.goldenSwirl, shade: '#E0951C' },
  towerFar: { fill: '#C69088', shade: '#B8847E', win: '#D09C92', lit: '#FFEBC0' },
  towerMid: { fill: '#A66A70', shade: '#925C64', win: '#B47A7E', lit: '#FFDC8A' },
  towerNear: { fill: '#7A4652', shade: '#683A46', win: '#8A5460', lit: '#FFD070' },
  rim: '#FFD08A',
  beacon: '#E0503A',
  parapet: { face: '#A87A72', seam: '#946A64', top: '#C8988A', lip: '#FFD08A' },
  chalk: '#FFEED8',
  floor: { base: '#B88E7E', band: '#C89C88', seam: '#A07A6E', chalk: '#FFEEDA' },
  tank: { leg: '#5A2A34', body: '#7A7430', shade: '#605A24', cap: '#9A9440', mark: '#AAA44A' },
  pole: '#5A2A34',
  string: '#5A2A34',
  flags: logoFlags,
  peg: '#FFD08A',
  laundry: [
    { fill: P.golden, shade: '#D69414', stripe: P.rimRed },
    { fill: P.honey, shade: '#E6B57C', stripe: P.persimmon },
    { fill: P.chartreuse, shade: '#AFA800' },
    { fill: P.persimmon, shade: '#D9622B' },
    { fill: '#F0C8A8', shade: '#D8A890' },
  ],
  plant: { leaf: ['#9A9020', '#7C7418', '#6E6814', '#5C5610'], pot: '#C05A3E', potShade: '#984632', pot2: P.persimmon, pot2Shade: '#C9602A', band: '#E08050' },
  chair: { back: P.rimRed, seat: '#E0502A', leg: '#9A220A' },
  dust: ['#D0AA9A', '#BC968A'],
  motes: ['#FFE6B0', '#FFF4DC'],
  shadow: '#7A4652',
  dim: '#3A1C24',
  vignette: '#3A1C24',
  grain: [0.36, 0.18, 0.2],
  east: { haze: '#B88480', front: '#D4A08E', side: '#A87478', top: '#FFD8A0', crown: '#E4B09A', win: '#C08E88', lit: '#FFEBC0' },
  court: {
    parapet: '#BC8C80', parapetTop: '#D6A692', parapetBase: '#986E6A', floor: '#B48A80', seam: '#9C746C', chalk: '#FFEEDA',
    tank: { leg: '#5A2A34', body: '#9A9440', shade: '#7A7430', top: '#C8C060' },
  },
  side: { haze: '#B88480', fill: '#A66A70', shade: '#925C64', win: '#B47A7E', lit: '#FFDC8A' },
  fg: '#3A1C24',
};

export const LOOKS = { dusk, ube, pandan, rosas };
export type LookName = keyof typeof LOOKS;

/** The set as it is drawn. */
export const S: SetLook = LOOKS[LOOK as LookName];
