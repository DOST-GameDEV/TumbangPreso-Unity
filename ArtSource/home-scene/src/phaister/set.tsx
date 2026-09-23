import React from 'react';
import { random } from 'remotion';
import { clamp01, loopNoise, loopSin } from '../lib/time';
import { poly, project, projectDir, View, V3 } from './view';

/*
 * Ilalim ng Tulay at night: Aurora Boulevard under the LRT-2 guideway, the Gilmore computer-shop
 * strip (docs/Ilalim_Ng_Tulay.md, LORE.md). The camera stands on the right-hand carriageway; the
 * guideway runs overhead to the left on median piers, the shophouses line the right pavement, the
 * road runs away to a vanishing point right of centre, and the moon hangs in the gap of sky
 * between the guideway's edge and the rooflines.
 *
 * ⚠️ NO BLUE, EVEN AT NIGHT (CLAUDE.md § 6.4, docs/HOME_SCREEN_ANIMATION_METHOD.md). Every colour
 * drawn here carries at least as much red as blue: night is plum and maroon, never navy. Blue is the
 * defender's colour in this game and the owner has refused it in the front end five times.
 *
 * ⚠️ THE GUIDEWAY'S DARK UNDERSIDE SITS UNDER THE HUB'S TOP BAND AND LEFT COLUMN ON PURPOSE, and the
 * right pavement under the mode card and PLAY is kept in shadow: busy, lit things live in the middle
 * third, where the hub leaves the picture alone (README.md § 5).
 */

export const N = {
  sky: ['#1A0514', '#22061A', '#2C0920', '#380C26', '#46112C', '#561630', '#6A1C34', '#822636', '#9C3236', '#B8443A'],
  glow: '#B85236',
  moon: '#FFF0CE',
  moonShade: '#EED3A2',
  moonBlood: '#8E2A22',
  star: '#FFE7BC',
  deckUnder: '#241618',
  deckUnderFar: '#34201F',
  deckSide: '#6A524B',
  deckSideShade: '#4A3634',
  parapet: '#7C6258',
  pierLit: '#5E4640',
  pierShade: '#34242A',
  road: '#2A1D20',
  roadFar: '#3A2728',
  median: '#3E2C2C',
  lane: '#8A745E',
  pave: '#3E2C2C',
  kerb: '#6A5448',
  chalk: '#F4E9D6',
  train: '#E2D2BE',
  trainShade: '#A8927E',
  trainStripe: '#8E2A5A',
  window: '#FFE4A0',
  lamp: '#FFF6D6',
};

// ------------------------------------------------------------------------------------ geometry

/** The guideway: a deck box over the median, its outer (right) fascia at DECK.right. */
// ⚠️ 5.3 m TO THE LEFT OF THE LENS, NOT 1.8. From right under its edge the deck hid the train on top
// completely (a render with the deck removed found it tiny behind her hat); from the kerb of the
// next lane the train's whole side shows over the parapet, which is also how Aurora Boulevard reads.
export const DECK = { left: -14, right: -5.3, under: 7.0, top: 8.3 };
/** The train's right side and its roof; it rides the near track. */
export const TRAIN = { left: -7.85, right: -5.4, floor: 8.45, roof: 11.35, car: 15.6, gap: 0.6 };
export const PIERS = [9, 36.5, 64, 91.5, 119, 146.5, 174, 201.5];
export const PIER_X = -7.7;
const MEDIAN: [number, number] = [-2.4, -9.5];
export const PAVE_X = 7.4;
export const FACADE_X = 9.6;

// ------------------------------------------------------------------------------------ sky

/**
 * The sky, in stepped bands like every gradient in this project (the blocky way to draw one).
 * `dark` is the eclipse: 0 at rest, 1 with the moon swallowed.
 */
export const Sky: React.FC<{ v: View; f: number; dark: number }> = ({ v, f, dark }) => {
  const out: React.ReactNode[] = [];
  const bands = N.sky;
  // The glow sits on the horizon and follows it when the camera tilts.
  const top = v.hy - 900;
  const step = 900 / bands.length;
  for (let i = 0; i < bands.length; i++) {
    const y = top + i * step;
    const d: string[] = [`M -200 ${y + step + 40}`, `L -200 ${y}`];
    for (let x = -200; x <= 2200; x += 180) {
      const yy = y + (i === 0 ? -600 : Math.round(5 * Math.sin(x / 190 + i * 1.3) + 3 * loopSin(f, 1, x / 320 + i)));
      d.push(`L ${x} ${yy}`, `L ${x + 180} ${yy}`);
    }
    d.push(`L 2200 ${y + step + 40} Z`);
    out.push(<path key={i} d={d.join(' ')} fill={bands[i]} />);
  }
  out.push(<rect key="low" x={-200} y={v.hy - 10} width={2400} height={900} fill={bands[bands.length - 1]} />);
  // The city's own light low in the sky, a warm haze the eclipse cannot reach.
  out.push(<ellipse key="haze" cx={v.px} cy={v.hy + 10} rx={900} ry={150} fill={N.glow} opacity={0.35} filter="url(#glowBig)" />);
  // The eclipse darkens the sky a step, never to black: night comes down, it does not switch off.
  if (dark > 0) out.push(<rect key="dk" x={-200} y={-400} width={2400} height={v.hy + 400} fill="#0E0309" opacity={0.42 * dark} />);
  return <g>{out}</g>;
};

export const Stars: React.FC<{ v: View; f: number; dark: number }> = ({ v, f, dark }) => {
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 34; i++) {
    const [x, y] = projectDir(v, -30 + random(`ps${i}x`) * 60, 7 + random(`ps${i}y`) * 26);
    const tw = clamp01(0.4 + 0.75 * loopNoise(`ps${i}`, f, 1.3));
    // More of them come out while the moon is gone.
    const a = (0.2 + 0.8 * tw) * (random(`ps${i}k`) < 0.45 ? 1 : dark);
    if (a < 0.03) continue;
    const r = 2.2 + random(`ps${i}r`) * 3.2;
    out.push(
      <g key={i} transform={`translate(${x} ${y}) scale(${0.6 + tw * 0.5})`} opacity={a}>
        <rect x={-r * 0.4} y={-r * 1.8} width={r * 0.8} height={r * 3.6} fill={N.star} />
        <rect x={-r * 1.8} y={-r * 0.4} width={r * 3.6} height={r * 0.8} fill={N.star} />
      </g>,
    );
  }
  return <g>{out}</g>;
};

/** The moon's direction, degrees. It sits in the gap between the guideway and the rooflines. */
export const MOON = { az: 7.5, el: 13, r: 62 };

const octagon = (cx: number, cy: number, r: number) => {
  const k = r * 0.42;
  return `M${cx - k} ${cy - r}L${cx + k} ${cy - r}L${cx + r} ${cy - k}L${cx + r} ${cy + k}L${cx + k} ${cy + r}L${cx - k} ${cy + r}L${cx - r} ${cy + k}L${cx - r} ${cy - k}Z`;
};

/**
 * ⚠️ THE MOON AND THE SHADOW THAT SWALLOWS IT. CHARACTER_ORIGINS.md: on Capul she heard of a moon
 * that could disappear into a serpent's mouth; the National Museum records the Abaknon belief that
 * an eclipse (bakunawa) is the moon swallowed by a snake. The serpent is NEVER drawn as a monster
 * (LORE.md: nothing threatens the neighbourhood). It is a shadow with a mouth's curve that slides
 * across the disc, and in the full eclipse the moon glows the dull red a real total eclipse does.
 * `bite` 0..1 is how far across it is; `blood` 0..1 how red the swallowed moon has gone.
 */
export const Moon: React.FC<{ v: View; bite: number; blood: number; glow: number }> = ({ v, bite, blood, glow }) => {
  const [cx, cy] = projectDir(v, MOON.az, MOON.el);
  const r = MOON.r;
  // The shadow comes in from the lower left (from the guideway's side) and leaves the same way.
  const sx = cx - 2.3 * r + 2.3 * r * bite;
  const sy = cy + 0.9 * r - 0.9 * r * bite;
  const moonColour = blood > 0 ? mixHex(N.moon, N.moonBlood, blood) : N.moon;
  return (
    <g>
      <defs>
        <clipPath id="moonClip">
          <path d={octagon(cx, cy, r)} />
        </clipPath>
      </defs>
      <circle cx={cx} cy={cy} r={r * 2.6} fill={N.moon} opacity={0.16 * glow * (1 - 0.8 * blood)} filter="url(#glowBig)" />
      <path d={octagon(cx, cy, r)} fill={moonColour} />
      {/* Her maria, three flat blocks. */}
      <g opacity={0.5}>
        <rect x={cx - r * 0.45} y={cy - r * 0.35} width={r * 0.34} height={r * 0.26} fill={N.moonShade} />
        <rect x={cx + r * 0.08} y={cy + r * 0.12} width={r * 0.42} height={r * 0.3} fill={N.moonShade} />
        <rect x={cx - r * 0.2} y={cy + r * 0.42} width={r * 0.22} height={r * 0.18} fill={N.moonShade} />
      </g>
      {bite > 0 && bite < 1.02 && (
        <g clipPath="url(#moonClip)">
          <path d={octagon(sx, sy, r * 1.08)} fill="#1A0610" opacity={0.92 * (1 - blood * 0.55)} />
        </g>
      )}
    </g>
  );
};

export const mixHex = (a: string, b: string, t: number) => {
  const pa = parseInt(a.slice(1), 16);
  const pb = parseInt(b.slice(1), 16);
  const ch = (s: number) => Math.round(((pa >> s) & 255) + (((pb >> s) & 255) - ((pa >> s) & 255)) * t);
  return `#${((1 << 24) | (ch(16) << 16) | (ch(8) << 8) | ch(0)).toString(16).slice(1)}`;
};

// ------------------------------------------------------------------------------------ the city

type Block = { z0: number; z1: number; h: number; fill: string; side: string; seed: string; sign?: Sign };
type Sign = { text: string; bg: string; fg: string; w: number };

const FILLS = ['#5A2E2E', '#6A3830', '#4E2830', '#623228', '#58302E'];
const SIDES = ['#3C1E22', '#462620', '#361C24', '#44241E', '#3E2224'];

/**
 * The right-hand shophouses, far to near. The three that carry signs are the ones LORE.md names for
 * this place: a computer shop (Gilmore is the IT strip), the pisonet and the pares stall. Words are
 * generic trades, never a real business's mark.
 */
const RIGHT: Block[] = (() => {
  const out: Block[] = [];
  let z = 9;
  let i = 0;
  const signs: Record<number, Sign> = {
    1: { text: 'PISONET', bg: '#2E7A44', fg: '#F4F0C8', w: 2.3 },
    2: { text: 'COMPUTER', bg: '#C8322A', fg: '#FFE14A', w: 2.6 },
    4: { text: 'PARES', bg: '#F0B028', fg: '#8A1A14', w: 1.9 },
    6: { text: 'CELLPHONE', bg: '#E8DCC0', fg: '#9A1E1A', w: 2.6 },
    8: { text: 'PRINTING', bg: '#B8402E', fg: '#FFF0C8', w: 2.3 },
  };
  while (z < 190) {
    const w = 7 + random(`rb${i}w`) * 7;
    // Two and three storeys, as the strip is: low enough that the moon clears the rooflines.
    const h = 6.2 + random(`rb${i}h`) * 4 + (i % 3 === 1 ? 2.4 : 0);
    out.push({ z0: z, z1: z + w, h, fill: FILLS[i % FILLS.length], side: SIDES[i % SIDES.length], seed: `rb${i}`, sign: signs[i] });
    z += w + 0.2;
    i++;
  }
  return out;
})();

export const FarCity: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const out: React.ReactNode[] = [];
  // A far skyline beyond the road's end, both sides of it: towers along Aurora and E. Rodriguez.
  for (let i = 0; i < 26; i++) {
    const x = -140 + i * 11 + random(`fc${i}x`) * 5;
    const w = 6 + random(`fc${i}w`) * 7;
    const h = 20 + random(`fc${i}h`) * 55 * (Math.abs(x) > 18 ? 1 : 0.5);
    const z = 330;
    out.push(<path key={`t${i}`} d={poly(v, [[x, 0, z], [x, h, z], [x + w, h, z], [x + w, 0, z]])} fill={i % 2 ? '#4A1C24' : '#521F26'} />);
    for (let k = 0; k < 4; k++) {
      if (random(`fc${i}k${k}`) < 0.5) continue;
      const wy = 4 + random(`fc${i}y${k}`) * (h - 8);
      const wx = x + 1 + random(`fc${i}xx${k}`) * (w - 3);
      out.push(<path key={`w${i}${k}`} d={poly(v, [[wx, wy, z], [wx, wy + 1.4, z], [wx + 1.6, wy + 1.4, z], [wx + 1.6, wy, z]])} fill={N.window} opacity={0.35 + 0.3 * loopNoise(`fcw${i}${k}`, f, 0.6)} />);
    }
  }
  return <g>{out}</g>;
};

/** Right-hand pavement and shophouses, drawn far to near. */
export const RightStreet: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const out: React.ReactNode[] = [];
  // Pavement and kerb face.
  out.push(<path key="pave" d={poly(v, [[PAVE_X, 0.16, 2], [FACADE_X, 0.16, 2], [FACADE_X, 0.16, 400], [PAVE_X, 0.16, 400]])} fill={N.pave} />);
  out.push(<path key="kerb" d={poly(v, [[PAVE_X, 0, 2], [PAVE_X, 0.16, 2], [PAVE_X, 0.16, 400], [PAVE_X, 0, 400]])} fill={N.kerb} />);
  for (const b of [...RIGHT].reverse()) {
    const { z0, z1, h } = b;
    // The end wall facing the lens, visible past the nearer, lower neighbour.
    out.push(<path key={`${b.seed}e`} d={poly(v, [[FACADE_X, 0, z0], [FACADE_X, h, z0], [FACADE_X + 12, h, z0], [FACADE_X + 12, 0, z0]])} fill={b.side} />);
    out.push(<path key={`${b.seed}f`} d={poly(v, [[FACADE_X, 0, z0], [FACADE_X, h, z0], [FACADE_X, h, z1], [FACADE_X, 0, z1]])} fill={b.fill} />);
    // Upper floors: a window per bay, some lit, some breathing on their own clock.
    const floors = Math.floor((h - 3.2) / 3);
    const bays = Math.max(2, Math.floor((z1 - z0) / 2.4));
    for (let fl = 0; fl < floors; fl++) {
      for (let k = 0; k < bays; k++) {
        const r = random(`${b.seed}w${fl}${k}`);
        const lit = r < 0.55;
        const za = z0 + (k + 0.25) * ((z1 - z0) / bays);
        const zb = za + 0.55 * ((z1 - z0) / bays);
        const y0 = 3.8 + fl * 3;
        const a = lit ? 0.75 + 0.25 * loopNoise(`${b.seed}l${fl}${k}`, f, 0.8) : 1;
        out.push(<path key={`${b.seed}w${fl}${k}`} d={poly(v, [[FACADE_X - 0.02, y0, za], [FACADE_X - 0.02, y0 + 1.5, za], [FACADE_X - 0.02, y0 + 1.5, zb], [FACADE_X - 0.02, y0, zb]])} fill={lit ? (r < 0.2 ? '#FFC870' : N.window) : '#2A1216'} opacity={a} />);
      }
    }
    // The shopfront: a lit opening under a roll shutter half down, and an awning over it.
    out.push(<path key={`${b.seed}s`} d={poly(v, [[FACADE_X - 0.02, 0.2, z0 + 0.5], [FACADE_X - 0.02, 2.7, z0 + 0.5], [FACADE_X - 0.02, 2.7, z1 - 0.5], [FACADE_X - 0.02, 0.2, z1 - 0.5]])} fill="#F2B874" opacity={0.62} />);
    out.push(<path key={`${b.seed}sh`} d={poly(v, [[FACADE_X - 0.03, 1.9, z0 + 0.5], [FACADE_X - 0.03, 2.7, z0 + 0.5], [FACADE_X - 0.03, 2.7, z1 - 0.5], [FACADE_X - 0.03, 1.9, z1 - 0.5]])} fill="#7A6458" />);
    out.push(<path key={`${b.seed}aw`} d={poly(v, [[FACADE_X, 3.0, z0], [FACADE_X - 1.6, 2.8, z0], [FACADE_X - 1.6, 2.8, z1], [FACADE_X, 3.0, z1]])} fill={b.side} />);
    out.push(<path key={`${b.seed}aw2`} d={poly(v, [[FACADE_X - 1.6, 2.55, z0], [FACADE_X - 1.6, 2.8, z0], [FACADE_X - 1.6, 2.8, z1], [FACADE_X - 1.6, 2.55, z1]])} fill={b.fill} />);
    // Light spilling across the pavement from the open front.
    out.push(<path key={`${b.seed}sp`} d={poly(v, [[FACADE_X, 0.17, z0 + 0.5], [PAVE_X - 0.6, 0.02, z0 - 0.4], [PAVE_X - 0.6, 0.02, z1 + 0.4], [FACADE_X, 0.17, z1 - 0.5]])} fill="#FFB860" opacity={0.16} />);
    if (b.sign) out.push(<BladeSign key={`${b.seed}bs`} v={v} f={f} z={z0 + 1.2} s={b.sign} seed={b.seed} />);
  }
  return <g>{out}</g>;
};

/**
 * A blade sign: a lightbox sticking out from the facade over the pavement, square to the street,
 * so it faces the lens and its word reads. It flickers the way a tired lightbox does, on its own
 * periodic clock so the seam cannot show.
 */
const BladeSign: React.FC<{ v: View; f: number; z: number; s: Sign; seed: string }> = ({ v, f, z, s, seed }) => {
  const x1 = FACADE_X - 0.2;
  const x0 = x1 - s.w;
  const y0 = 3.5;
  const y1 = 4.6;
  const [a, b] = [project(v, [x0, y1, z]), project(v, [x1, y0, z])];
  const w = b[0] - a[0];
  const h = b[1] - a[1];
  if (w < 3) return null;
  const flick = seed === 'rb4' ? 0.72 + 0.28 * Math.max(0, loopNoise(`fl${seed}`, f, 6)) : 1;
  return (
    <g opacity={flick}>
      <rect x={a[0] - w * 0.08} y={a[1] - h * 0.3} width={w * 1.16} height={h * 1.6} fill={s.bg} opacity={0.35} filter="url(#glow)" />
      <path d={poly(v, [[x0, y0, z], [x0, y1, z], [x1, y1, z], [x1, y0, z]])} fill={s.bg} />
      <path d={poly(v, [[x1, y1 + 0.2, z], [x1 + 0.2, y1 + 0.2, z], [x1 + 0.2, y0 - 0.2, z], [x1, y0 - 0.2, z]])} fill="#3A2622" />
      <text x={a[0] + w / 2} y={a[1] + h * 0.74} textAnchor="middle" fontFamily="Paalalabas" fontSize={h * 0.62} fill={s.fg} textLength={w * 0.84} lengthAdjust="spacingAndGlyphs">{s.text}</text>
    </g>
  );
};

// ------------------------------------------------------------------------------------ the guideway

/** The median, the far carriageway and the dim shops under the guideway on the left. */
export const LeftStreet: React.FC<{ v: View }> = ({ v }) => {
  const out: React.ReactNode[] = [];
  out.push(<path key="far" d={poly(v, [[-26, 0, 2], [MEDIAN[1], 0, 2], [MEDIAN[1], 0, 400], [-26, 0, 400]])} fill="#241718" />);
  out.push(<path key="shops" d={poly(v, [[-21.5, 0, 4], [-21.5, 6.8, 4], [-21.5, 6.8, 400], [-21.5, 0, 400]])} fill="#2E1A1C" />);
  for (let i = 0; i < 16; i++) {
    const z0 = 6 + i * 11 + random(`ls${i}`) * 4;
    out.push(<path key={`lw${i}`} d={poly(v, [[-21.4, 0.4, z0], [-21.4, 2.4, z0], [-21.4, 2.4, z0 + 5], [-21.4, 0.4, z0 + 5]])} fill="#FFC870" opacity={0.28 + 0.2 * random(`lsa${i}`)} />);
  }
  out.push(<path key="med" d={poly(v, [[MEDIAN[1], 0.22, 2], [MEDIAN[0], 0.22, 2], [MEDIAN[0], 0.22, 400], [MEDIAN[1], 0.22, 400]])} fill={N.median} />);
  out.push(<path key="medk" d={poly(v, [[MEDIAN[0], 0, 2], [MEDIAN[0], 0.22, 2], [MEDIAN[0], 0.22, 400], [MEDIAN[0], 0, 400]])} fill={N.kerb} />);
  return <g>{out}</g>;
};

/** The piers in the median, far to near: a square shaft and a T cap under the deck. */
export const Piers: React.FC<{ v: View; lamp: number }> = ({ v, lamp }) => {
  const out: React.ReactNode[] = [];
  const hw = 0.8;
  for (const z of [...PIERS].reverse()) {
    const x0 = PIER_X - hw;
    const x1 = PIER_X + hw;
    // The face toward the road (+X) takes the street's light; the face toward the lens is in shade.
    out.push(<path key={`ps${z}`} d={poly(v, [[x1, 0.2, z - hw], [x1, 6.2, z - hw], [x1, 6.2, z + hw], [x1, 0.2, z + hw]])} fill={N.pierLit} />);
    out.push(<path key={`pf${z}`} d={poly(v, [[x0, 0.2, z - hw], [x0, 6.2, z - hw], [x1, 6.2, z - hw], [x1, 0.2, z - hw]])} fill={N.pierShade} />);
    // The cap: wide under the deck, stepped at its ends.
    out.push(<path key={`pc${z}`} d={poly(v, [[DECK.left + 0.8, 6.2, z - 1.1], [DECK.left + 0.8, DECK.under, z - 1.1], [DECK.right - 0.4, DECK.under, z - 1.1], [DECK.right - 0.4, 6.6, z - 1.1], [x1 + 0.4, 6.2, z - 1.1]])} fill={N.pierShade} />);
    out.push(<path key={`pcs${z}`} d={poly(v, [[DECK.right - 0.4, 6.6, z - 1.1], [DECK.right - 0.4, DECK.under, z - 1.1], [DECK.right - 0.4, DECK.under, z + 1.1], [DECK.right - 0.4, 6.6, z + 1.1]])} fill={N.pierLit} />);
    // BAWAL UMIHI DITO, stencilled on the shaft (docs/Ilalim_Ng_Tulay.md § 4.6), where a near pier shows it.
    if (z < 40) {
      const [a, b] = [project(v, [x1, 2.2, z - hw + 0.2]), project(v, [x1, 1.4, z + hw - 0.2])];
      if (b[0] - a[0] > 20) out.push(<text key={`bw${z}`} x={(a[0] + b[0]) / 2} y={(a[1] + b[1]) / 2 + 6} textAnchor="middle" fontFamily="Paalalabas" fontSize={Math.abs(b[1] - a[1]) * 0.4} fill="#C8B49A" opacity={0.55} textLength={(b[0] - a[0]) * 0.8} lengthAdjust="spacingAndGlyphs">BAWAL UMIHI</text>);
    }
  }
  // The median lamp's pool on the near pier: a warm wash, stronger when the stage lamp is up.
  out.push(<path key="lp" d={poly(v, [[PIER_X + hw + 0.01, 0.2, PIERS[0] - hw], [PIER_X + hw + 0.01, 4, PIERS[0] - hw], [PIER_X + hw + 0.01, 4, PIERS[0] + hw], [PIER_X + hw + 0.01, 0.2, PIERS[0] + hw]])} fill="#FFB860" opacity={0.18 * lamp} />);
  return <g>{out}</g>;
};

/** The deck: its dark underside and the lit fascia along its outer edge, plus the parapet. */
export const Deck: React.FC<{ v: View }> = ({ v }) => {
  const z0 = 1.2;
  const z1 = 420;
  return (
    <g>
      <path d={poly(v, [[DECK.left, DECK.under, z0], [DECK.right, DECK.under, z0], [DECK.right, DECK.under, z1], [DECK.left, DECK.under, z1]])} fill={N.deckUnder} />
      {/* Ribs across the underside: the box girder's segments, every 3 m, fading with depth. */}
      {Array.from({ length: 34 }, (_, i) => {
        const z = 3 + i * 3.1;
        return <path key={i} d={poly(v, [[DECK.left, DECK.under - 0.01, z], [DECK.right, DECK.under - 0.01, z], [DECK.right, DECK.under - 0.01, z + 0.35], [DECK.left, DECK.under - 0.01, z + 0.35]])} fill={N.deckUnderFar} opacity={0.8} />;
      })}
      <path d={poly(v, [[DECK.right, DECK.under, z0], [DECK.right, DECK.top, z0], [DECK.right, DECK.top, z1], [DECK.right, DECK.under, z1]])} fill={N.deckSide} />
      <path d={poly(v, [[DECK.right, DECK.under, z0], [DECK.right, DECK.under + 0.3, z0], [DECK.right, DECK.under + 0.3, z1], [DECK.right, DECK.under, z1]])} fill={N.deckSideShade} />
      {/* A low parapet, so the train above reads from the street: at 0.9 m it hid all but its roof. */}
      <path d={poly(v, [[DECK.right - 0.05, DECK.top, z0], [DECK.right - 0.05, DECK.top + 0.45, z0], [DECK.right - 0.05, DECK.top + 0.45, z1], [DECK.right - 0.05, DECK.top, z1]])} fill={N.parapet} />
    </g>
  );
};

/** Overhead line masts on the deck, black against the sky, with the contact wire between them. */
export const Masts: React.FC<{ v: View }> = ({ v }) => {
  const out: React.ReactNode[] = [];
  const zs = Array.from({ length: 16 }, (_, i) => 4 + i * 24);
  for (const z of zs) {
    out.push(<path key={`m${z}`} d={poly(v, [[DECK.right - 0.45, DECK.top, z], [DECK.right - 0.45, 14.2, z], [DECK.right - 0.2, 14.2, z], [DECK.right - 0.2, DECK.top, z]])} fill="#2A181A" />);
    out.push(<path key={`a${z}`} d={poly(v, [[DECK.right - 0.2, 13.6, z], [TRAIN.left, 13.6, z], [TRAIN.left, 13.9, z], [DECK.right - 0.2, 13.9, z]])} fill="#2A181A" />);
  }
  const wire = (x: number, y: number) => {
    const pts = zs.map((z) => project(v, [x, y, z]));
    return pts.map(([a, b], i) => `${i ? 'L' : 'M'}${a.toFixed(1)} ${b.toFixed(1)}`).join('');
  };
  out.push(<path key="w1" d={wire(-3.5, 12.4)} stroke="#2A181A" strokeWidth={3} fill="none" />);
  out.push(<path key="w2" d={wire(-3.5, 13.4)} stroke="#2A181A" strokeWidth={2} fill="none" />);
  return <g>{out}</g>;
};

/**
 * The LRT-2 consist: three cars on the near track, `head` metres down the line (its front end).
 * Its side faces the lens over the deck's edge; the front carries the lamp. Cream body with a plum
 * band, the nearest this palette can come to the line's real livery without blue.
 */
export const Train: React.FC<{ v: View; head: number; f: number }> = ({ v, head, f }) => {
  const out: React.ReactNode[] = [];
  const T = TRAIN;
  for (let c = 2; c >= 0; c--) {
    const za = head + c * (T.car + T.gap);
    const zb = za + T.car;
    if (zb < 0.8 || za > 420) continue;
    const a = Math.max(0.9, za);
    out.push(<path key={`s${c}`} d={poly(v, [[T.right, T.floor, a], [T.right, T.roof, a], [T.right, T.roof, zb], [T.right, T.floor, zb]])} fill={N.train} />);
    out.push(<path key={`b${c}`} d={poly(v, [[T.right + 0.01, T.floor + 0.35, a], [T.right + 0.01, T.floor + 0.8, a], [T.right + 0.01, T.floor + 0.8, zb], [T.right + 0.01, T.floor + 0.35, zb]])} fill={N.trainStripe} />);
    out.push(<path key={`r${c}`} d={poly(v, [[T.right, T.roof, a], [T.right - 0.4, T.roof + 0.25, a], [T.right - 0.4, T.roof + 0.25, zb], [T.right, T.roof, zb]])} fill={N.trainShade} />);
    // Windows: a lit strip broken by pillars, with passengers as darker blocks.
    for (let k = 0; k < 7; k++) {
      const w0 = za + 0.9 + k * 2.1;
      const w1 = w0 + 1.5;
      if (w1 < 0.9) continue;
      out.push(<path key={`w${c}${k}`} d={poly(v, [[T.right + 0.01, T.floor + 1.2, Math.max(0.9, w0)], [T.right + 0.01, T.roof - 0.5, Math.max(0.9, w0)], [T.right + 0.01, T.roof - 0.5, w1], [T.right + 0.01, T.floor + 1.2, w1]])} fill={N.window} />);
      if (random(`tp${c}${k}`) < 0.6) {
        const p0 = w0 + 0.3 + random(`tpx${c}${k}`) * 0.6;
        out.push(<path key={`p${c}${k}`} d={poly(v, [[T.right + 0.02, T.floor + 1.2, Math.max(0.9, p0)], [T.right + 0.02, T.floor + 2.3, Math.max(0.9, p0)], [T.right + 0.02, T.floor + 2.3, p0 + 0.45], [T.right + 0.02, T.floor + 1.2, p0 + 0.45]])} fill="#9A6A58" />);
      }
    }
  }
  // The cab end, facing the lens, and its lamp.
  if (head > 0.9) {
    out.push(<path key="cab" d={poly(v, [[T.left, T.floor, head], [T.left, T.roof, head], [T.right, T.roof, head], [T.right, T.floor, head]])} fill={N.trainShade} />);
    out.push(<path key="cabw" d={poly(v, [[T.left + 0.3, T.floor + 1.4, head - 0.01], [T.left + 0.3, T.roof - 0.4, head - 0.01], [T.right - 0.3, T.roof - 0.4, head - 0.01], [T.right - 0.3, T.floor + 1.4, head - 0.01]])} fill="#4A2A2A" />);
    const [lx, ly] = project(v, [(T.left + T.right) / 2, T.floor + 0.8, head - 0.02]);
    const s = Math.min(3, 30 / head);
    out.push(<circle key="lg" cx={lx} cy={ly} r={60 * s + 10} fill={N.lamp} opacity={0.5} filter="url(#glowBig)" />);
    out.push(<rect key="l1" x={lx - 26 * s} y={ly - 5 * s} width={14 * s} height={10 * s} fill={N.lamp} />);
    out.push(<rect key="l2" x={lx + 12 * s} y={ly - 5 * s} width={14 * s} height={10 * s} fill={N.lamp} />);
  }
  void f;
  return <g>{out}</g>;
};

// ------------------------------------------------------------------------------------ the road

/** The carriageway with its lane lines, and the chalk court drawn on it. */
export const Road: React.FC<{ v: View; can: V3 }> = ({ v, can }) => {
  const out: React.ReactNode[] = [];
  out.push(<path key="road" d={poly(v, [[MEDIAN[0], 0, 1.2], [PAVE_X, 0, 1.2], [PAVE_X, 0, 420], [MEDIAN[0], 0, 420]])} fill={N.road} />);
  // Wear in the asphalt: darker patches, a few paler ones.
  for (let i = 0; i < 26; i++) {
    const x = MEDIAN[0] + random(`rw${i}x`) * (PAVE_X - MEDIAN[0] - 1.6);
    const z = 2 + Math.pow(random(`rw${i}z`), 1.6) * 60;
    const w = 0.6 + random(`rw${i}w`) * 1.8;
    const d = 0.4 + random(`rw${i}d`) * 1.4;
    out.push(<path key={`rw${i}`} d={poly(v, [[x, 0.001, z], [x + w, 0.001, z], [x + w, 0.001, z + d], [x, 0.001, z + d]])} fill={i % 4 ? '#23181B' : '#3A2A2A'} />);
  }
  // Lane dashes down the middle of the carriageway.
  for (let i = 0; i < 40; i++) {
    const z = 1.5 + i * 6;
    out.push(<path key={`ld${i}`} d={poly(v, [[2.35, 0.002, z], [2.55, 0.002, z], [2.55, 0.002, z + 3], [2.35, 0.002, z + 3]])} fill={N.lane} opacity={0.55} />);
  }
  // The chalk court: the taya's box and the can's circle, in the game's own shapes (a square, not a
  // circle, CLAUDE.md § 4), hand-drawn: each line wobbles a little and is doubled where it was gone
  // over twice.
  const bx: [number, number] = [can[0] - 2.4, can[0] + 2.4];
  const bz: [number, number] = [can[2] - 2.0, can[2] + 3.2];
  const line = (a: V3, b: V3, k: string) => {
    const pts: string[] = [];
    for (let i = 0; i <= 12; i++) {
      const t = i / 12;
      const p: V3 = [a[0] + (b[0] - a[0]) * t + (random(`${k}${i}`) - 0.5) * 0.05, 0.003, a[2] + (b[2] - a[2]) * t + (random(`${k}z${i}`) - 0.5) * 0.05];
      const [x, y] = project(v, p);
      pts.push(`${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`);
    }
    return pts.join('');
  };
  const cw = (z: number) => Math.max(1.5, 26 / Math.max(1, z));
  const edges: [V3, V3, string][] = [
    [[bx[0], 0, bz[0]], [bx[1], 0, bz[0]], 'c0'],
    [[bx[1], 0, bz[0]], [bx[1], 0, bz[1]], 'c1'],
    [[bx[1], 0, bz[1]], [bx[0], 0, bz[1]], 'c2'],
    [[bx[0], 0, bz[1]], [bx[0], 0, bz[0]], 'c3'],
  ];
  for (const [a, b, k] of edges) out.push(<path key={k} d={line(a, b, k)} stroke={N.chalk} strokeWidth={cw((a[2] + b[2]) / 2)} strokeLinecap="square" fill="none" opacity={0.7} />);
  const ring: string[] = [];
  for (let i = 0; i <= 16; i++) {
    const a = (i / 16) * Math.PI * 2;
    const [x, y] = project(v, [can[0] + Math.cos(a) * 0.5, 0.003, can[2] + Math.sin(a) * 0.5]);
    ring.push(`${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`);
  }
  out.push(<path key="ring" d={ring.join('')} stroke={N.chalk} strokeWidth={cw(can[2])} fill="none" opacity={0.75} />);
  return <g>{out}</g>;
};

/** Utility lines sagging across the right-hand side from the facade poles. Foreground, dark. */
export const Wires: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const out: React.ReactNode[] = [];
  for (let k = 0; k < 4; k++) {
    const y = 7.4 + k * 0.45;
    const pts: string[] = [];
    for (let i = 0; i <= 30; i++) {
      const z = 2.5 + i * 3;
      const sag = Math.sin(((i % 10) / 10) * Math.PI) * 0.9;
      const sway = 0.06 * loopSin(f, 3, k + i * 0.1);
      const [x, yy] = project(v, [PAVE_X - 0.2 - k * 0.3, y - sag + sway, z]);
      pts.push(`${i ? 'L' : 'M'}${x.toFixed(1)} ${yy.toFixed(1)}`);
    }
    out.push(<path key={k} d={pts.join('')} stroke="#1E0E10" strokeWidth={3} fill="none" />);
  }
  return <g>{out}</g>;
};
