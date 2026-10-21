import React from 'react';
import { random } from 'remotion';
import { clamp01, LOOP, loopNoise, loopSin } from './time';
import { depth, horizonY, line, poly, project, projectDir, V3, View } from './view';
import {
  BOX, COLUMN_HALF, COLUMN_X, COLUMN_Z, CROSS, CROSS_HALF, DECK, FACE_X, HOOP, KERB_IN, KERB_TOP, LAMP_Z, LINE,
  PAD, PAVE_TOP, PCEX, POLE_Z, ROAD_X, TRACK_X, TRAIN,
} from './map';
import { Paint } from './lettering';

/*
 * Ilalim ng Tulay, drawn. Aurora Boulevard under the LRT-2 guideway at Gilmore, laid out on the
 * shipped map's own coordinates (map.ts). The look is the first version's (flat blocky shapes, two
 * hard light bands, line boil from `Defs`), the geometry is the map's: the mint deck over the road
 * centre with its ribbed soffit, the square columns and their T caps, cream mid-rises trimmed in sage
 * behind single-storey glass shop boxes, brown utility poles with dark caps, hooked lamp columns, the
 * tiled pavements with the white kerb that IS the court's east and west chalk (docs/Ilalim_Ng_Tulay.md § 2).
 *
 * ⚠️ NO BLUE, EVEN AT NIGHT (CLAUDE.md § 6.4, docs/HOME_SCREEN_ANIMATION_METHOD.md). The map's mint
 * concrete is a warm sage here (red at or above blue), which is also what mint looks like in low gold
 * light. The pole caps and boots, navy in the map, are a dark plum. Night is plum, never navy.
 *
 * ⚠️ TWO LIGHTS, ONE PAINT. Every surface colour is written once, as its golden-hour colour, and goes
 * through `lit()` for the sun band and `tone()` for the night the ultimate brings down, so the whole
 * street changes hour together and nothing has two hand-kept palettes to drift. Things that EMIT (lit
 * windows, lightboxes, lamps) say what they become at night instead.
 */

// ------------------------------------------------------------------------------------ light

let NIGHT = 0;
/** 0 golden hour, 1 Grand Coven's night. Set once per frame by the scene before anything is built. */
export const setNight = (n: number) => { NIGHT = clamp01(n); };
export const night = () => NIGHT;

export const mixHex = (a: string, b: string, t: number) => {
  const pa = parseInt(a.slice(1), 16);
  const pb = parseInt(b.slice(1), 16);
  const ch = (s: number) => Math.round(((pa >> s) & 255) + (((pb >> s) & 255) - ((pa >> s) & 255)) * t);
  return `#${((1 << 24) | (ch(16) << 16) | (ch(8) << 8) | ch(0)).toString(16).slice(1)}`;
};
const mul = (hex: string, r: number, g: number, b: number, add = [0, 0, 0]) => {
  const p = parseInt(hex.slice(1), 16);
  const c = (v: number, k: number, a: number) => Math.max(0, Math.min(255, Math.round(v * k + a)));
  return `#${((1 << 24) | (c((p >> 16) & 255, r, add[0]) << 16) | (c((p >> 8) & 255, g, add[1]) << 8) | c(p & 255, b, add[2])).toString(16).slice(1)}`;
};
/** A surface's night colour: its paint under a plum moonlit sky. */
const nightOf = (hex: string) => mul(hex, 0.3, 0.2, 0.28, [14, 6, 16]);
/** Paint at this hour. */
export const tone = (hex: string) => (NIGHT <= 0 ? hex : mixHex(hex, nightOf(hex), NIGHT));
/** Something that lights up at night. */
export const emit = (day: string, nightHex: string) => (NIGHT <= 0 ? day : mixHex(day, nightHex, NIGHT));

/** The low sun: west south west, as the golden hour has it along Aurora. */
const SUN: V3 = (() => {
  const az = (242 * Math.PI) / 180;
  const el = (24 * Math.PI) / 180;
  return [Math.sin(az) * Math.cos(el), Math.sin(el), Math.cos(az) * Math.cos(el)];
})();
/** Two hard bands, like the cast's toon: lit gold, or in a warm plum shade. */
export const lit = (hex: string, n: V3) => {
  const d = n[0] * SUN[0] + n[1] * SUN[1] + n[2] * SUN[2];
  const day = d > 0.12 ? mul(hex, 1.05, 0.98, 0.86, [10, 4, 0]) : mul(hex, 0.7, 0.62, 0.66);
  return tone(day);
};

// ------------------------------------------------------------------------------------ palette

export const C = {
  road: '#5A504A',
  roadPatch: '#4C4440',
  roadLight: '#6A605A',
  lane: '#EFE4CC',
  chalk: '#F7EEDC',
  kerb: '#F2EBDD',
  kerbFace: '#B8AC98',
  pave: '#D6C9AE',
  paveLine: '#B9AC92',
  deck: '#A9BC9C',
  deckSide: '#8FA482',
  deckUnder: '#6E7E66',
  deckRib: '#5C6B55',
  column: '#AFC2A2',
  cap: '#9DB290',
  cream: '#F1E4C6',
  cream2: '#EAD9B6',
  rose: '#E7C9B8',
  trim: '#86A07E',
  trimRose: '#B06A62',
  glass: '#C9D2B2',
  shopFrame: '#556152',
  shopWall: '#CDBFA6',
  shopInside: '#3A3230',
  pole: '#7E3E24',
  poleCap: '#3E2B36',
  wire: '#2A1C1E',
  lamp: '#A8BF9C',
  shadow: '#000000',
};

// ------------------------------------------------------------------------------------ builder

export type Item = { z: number; node: React.ReactNode };

let KEY = 0;
const k = () => `s${KEY++}`;

const face = (v: View, pts: V3[], fill: string, op = 1, extra: React.SVGProps<SVGPathElement> = {}) => {
  const d = poly(v, pts);
  return d ? <path key={k()} d={d} fill={fill} opacity={op} {...extra} /> : null;
};

type BoxCol = { side: string; end?: string; top?: string; bottom?: string };
/** An axis-aligned block: only the faces turned to the lens are drawn, each in its own light band. */
export const box = (v: View, x0: number, x1: number, y0: number, y1: number, z0: number, z1: number, col: BoxCol) => {
  const out: React.ReactNode[] = [];
  const end = col.end ?? col.side;
  if (v.x > x1) out.push(face(v, [[x1, y0, z0], [x1, y1, z0], [x1, y1, z1], [x1, y0, z1]], lit(col.side, [1, 0, 0])));
  if (v.x < x0) out.push(face(v, [[x0, y0, z0], [x0, y1, z0], [x0, y1, z1], [x0, y0, z1]], lit(col.side, [-1, 0, 0])));
  if (v.z > z1) out.push(face(v, [[x0, y0, z1], [x0, y1, z1], [x1, y1, z1], [x1, y0, z1]], lit(end, [0, 0, 1])));
  if (v.z < z0) out.push(face(v, [[x0, y0, z0], [x0, y1, z0], [x1, y1, z0], [x1, y0, z0]], lit(end, [0, 0, -1])));
  if (v.y > y1 && col.top) out.push(face(v, [[x0, y1, z0], [x1, y1, z0], [x1, y1, z1], [x0, y1, z1]], lit(col.top, [0, 1, 0])));
  if (v.y < y0 && col.bottom) out.push(face(v, [[x0, y0, z0], [x1, y0, z0], [x1, y0, z1], [x0, y0, z1]], tone(col.bottom)));
  return out;
};

/** Depth of a box's middle, for sorting. */
export const mid = (v: View, x: number, y: number, z: number) => depth(v, [x, y, z]);

/**
 * Content drawn on a flat plane in the world, in METRES, through the affine map its corner points
 * make on screen: a sign's rects and letters go in as if on paper. `u` is the plane's +x (reading
 * direction), `dn` its +y (down the sign). Culled when turned away or behind the lens.
 * ⚠️ Affine, not perspective: exact at the origin corner and a few pixels out at the far one for a
 * sign a few metres wide, which the line boil hides. A full perspective warp is not worth it here.
 */
export const onPlane = (v: View, o: V3, u: V3, dn: V3, w: number, h: number, children: React.ReactNode, keyName?: string) => {
  // The front normal is u x down: reading left to right with the page's top up, it points at the reader.
  const n: V3 = [u[1] * dn[2] - u[2] * dn[1], u[2] * dn[0] - u[0] * dn[2], u[0] * dn[1] - u[1] * dn[0]];
  const toLens: V3 = [v.x - o[0], v.y - o[1], v.z - o[2]];
  if (n[0] * toLens[0] + n[1] * toLens[1] + n[2] * toLens[2] <= 0) return null;
  const corners: V3[] = [o, [o[0] + u[0] * w, o[1] + u[1] * w, o[2] + u[2] * w], [o[0] + dn[0] * h, o[1] + dn[1] * h, o[2] + dn[2] * h]];
  if (corners.some((c) => depth(v, c) < 0.6)) return null;
  const [p0, p1, p2] = corners.map((c) => project(v, c));
  const a = (p1[0] - p0[0]) / w;
  const b = (p1[1] - p0[1]) / w;
  const c = (p2[0] - p0[0]) / h;
  const d = (p2[1] - p0[1]) / h;
  if (Math.abs(a * d - b * c) < 0.5) return null;
  return <g key={keyName ?? k()} transform={`matrix(${a} ${b} ${c} ${d} ${p0[0]} ${p0[1]})`}>{children}</g>;
};

/** Plane axes for the three sign orientations in this street. */
export const FACING = {
  /** On the west row's street face, read from the road: runs north, faces east. Origin is its south top corner. */
  east: { u: [0, 0, 1] as V3, dn: [0, -1, 0] as V3 },
  /** On the east row: runs south, faces west. Origin is its north top corner. */
  west: { u: [0, 0, -1] as V3, dn: [0, -1, 0] as V3 },
  /** Across the street, facing a lens to the south. */
  south: { u: [1, 0, 0] as V3, dn: [0, -1, 0] as V3 },
  north: { u: [-1, 0, 0] as V3, dn: [0, -1, 0] as V3 },
};

// ------------------------------------------------------------------------------------ sky

const DAY_SKY = ['#F3B071', '#F6BB7B', '#F8C585', '#FACF90', '#FCD99C', '#FDE2A8', '#FEEAB6', '#FFF0C4'];
const NIGHT_SKY = ['#1A0514', '#22061A', '#2C0920', '#380C26', '#46112C', '#561630', '#6A1C34', '#822636'];

/**
 * The sky in stepped bands (the blocky way to draw a gradient, as Zack's sunset is drawn). Bands are
 * elevations, so a tilt or a crane moves them correctly. The night comes down FROM THE TOP, band by
 * band (`wipe` 0..1 how far down the night has reached), which is how her ultimate reads as the sky
 * being taken rather than a dimmer switch; `lift` is the dawn at the loop's end going back up.
 */
export const Sky: React.FC<{ v: View; f: number; wipe: number }> = ({ v, f, wipe }) => {
  const out: React.ReactNode[] = [];
  const n = DAY_SKY.length;
  const edges = [89, 50, 34, 24, 16, 10, 5.5, 2.5, -30];
  const yAt = (e: number) => {
    const a = e + v.pitch;
    if (a >= 88) return -4000;
    if (a <= -88) return 5000;
    return v.cy - v.f * Math.tan((a * Math.PI) / 180);
  };
  for (let i = 0; i < n; i++) {
    const y0 = yAt(edges[i]);
    const y1 = yAt(edges[i + 1]);
    // Band i turns once the wipe has passed it: top band first.
    const t = clamp01(wipe * (n + 2) - i);
    const col = mixHex(DAY_SKY[i], NIGHT_SKY[i], t);
    const d: string[] = [`M -600 ${y1 + 60}`, `L -600 ${y0}`];
    for (let x = -600; x <= 2600; x += 200) {
      const yy = i === 0 ? y0 - 1500 : y0 + Math.round(4 * Math.sin(x / 190 + i * 1.3) + 3 * loopSin(f, 1, x / 320 + i));
      d.push(`L ${x} ${yy}`, `L ${x + 200} ${yy}`);
    }
    d.push(`L 2600 ${y1 + 60} Z`);
    out.push(<path key={i} d={d.join(' ')} fill={col} />);
  }
  return <g>{out}</g>;
};

/** The low sun, WSW, and its haze; it sinks out of the night and comes back with the dawn. */
export const Sun: React.FC<{ v: View; wipe: number }> = ({ v, wipe }) => {
  const p = projectDir(v, 242, 7);
  if (!p || wipe > 0.98) return null;
  const a = 1 - wipe;
  return (
    <g opacity={a}>
      <circle cx={p[0]} cy={p[1]} r={300} fill="#FFD890" opacity={0.45} filter="url(#glowBig)" />
      <circle cx={p[0]} cy={p[1]} r={70} fill="#FFF3D2" />
    </g>
  );
};

/** Flat stepped clouds, far off, drifting one whole lap of the sky per loop. */
export const Clouds: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 9; i++) {
    const az = random(`cl${i}`) * 360 + (f / LOOP) * 12;
    const el = 9 + random(`cle${i}`) * 18;
    const p = projectDir(v, az, el);
    if (!p) continue;
    const w = 90 + random(`clw${i}`) * 160;
    const col = emit('#FFE2B8', '#5A1A36');
    out.push(
      <g key={i} opacity={0.9}>
        <rect x={p[0] - w / 2} y={p[1]} width={w} height={14} fill={col} />
        <rect x={p[0] - w / 3} y={p[1] - 12} width={w * 0.55} height={14} fill={col} />
        <rect x={p[0] - w / 2 + 10} y={p[1] + 14} width={w - 20} height={6} fill={emit('#F7C28E', '#3E0E26')} />
      </g>,
    );
  }
  return <g>{out}</g>;
};

export const Stars: React.FC<{ v: View; f: number; amount: number }> = ({ v, f, amount }) => {
  if (amount < 0.02) return null;
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 70; i++) {
    const p = projectDir(v, random(`st${i}x`) * 360, 8 + random(`st${i}y`) * 60);
    if (!p) continue;
    const tw = clamp01(0.4 + 0.75 * loopNoise(`st${i}`, f, 1.3));
    const a = (0.25 + 0.75 * tw) * clamp01(amount * 1.4 - random(`st${i}k`) * 0.4);
    if (a < 0.03) continue;
    const r = 2 + random(`st${i}r`) * 3;
    out.push(
      <g key={i} transform={`translate(${p[0]} ${p[1]}) scale(${0.6 + tw * 0.5})`} opacity={a}>
        <rect x={-r * 0.4} y={-r * 1.8} width={r * 0.8} height={r * 3.6} fill="#FFE7BC" />
        <rect x={-r * 1.8} y={-r * 0.4} width={r * 3.6} height={r * 0.8} fill="#FFE7BC" />
      </g>,
    );
  }
  return <g>{out}</g>;
};

// ------------------------------------------------------------------------------------ far city

/**
 * A ring of mid-rises and towers all round, far beyond the walls (the map's lower mid-rise belt and
 * the cream towers down the corridor), as flat panels facing the middle of the street. Windows light
 * one by one as the night comes.
 */
export const FarCity: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const items: Item[] = [];
  for (let i = 0; i < 64; i++) {
    const az = (i / 64) * Math.PI * 2 + random(`fa${i}`) * 0.05;
    const r = 95 + random(`fr${i}`) * 70;
    const w = 12 + random(`fw${i}`) * 18;
    const along = Math.abs(Math.cos(az)) > 0.8;
    const h = (along ? 30 : 16) + random(`fh${i}`) * (along ? 45 : 26);
    const cx = Math.sin(az) * r;
    const cz = Math.cos(az) * r;
    const tx = Math.cos(az);
    const tz = -Math.sin(az);
    const pts: V3[] = [[cx - tx * w / 2, 0, cz - tz * w / 2], [cx - tx * w / 2, h, cz - tz * w / 2], [cx + tx * w / 2, h, cz + tz * w / 2], [cx + tx * w / 2, 0, cz + tz * w / 2]];
    const d = poly(v, pts);
    if (!d) continue;
    const base = i % 3 === 0 ? '#EBCFA8' : i % 3 === 1 ? '#E4C09A' : '#DDB794';
    const node: React.ReactNode[] = [<path key="b" d={d} fill={mixHex(tone(base), tone('#F6C792'), 0.35)} />];
    if (NIGHT > 0.05) {
      for (let q = 0; q < 6; q++) {
        if (random(`fw${i}q${q}`) < 0.35) continue;
        const u = 0.15 + random(`fu${i}${q}`) * 0.7;
        const y = 4 + random(`fy${i}${q}`) * (h - 8);
        const px = cx + tx * w * (u - 0.5);
        const pz = cz + tz * w * (u - 0.5);
        const wd = poly(v, [[px, y, pz], [px, y + 2, pz], [px + tx * 2.4, y + 2, pz + tz * 2.4], [px + tx * 2.4, y, pz + tz * 2.4]]);
        if (wd) node.push(<path key={q} d={wd} fill="#FFD27A" opacity={NIGHT * clamp01(0.5 + 0.5 * loopNoise(`fwl${i}${q}`, f, 0.7))} />);
      }
    }
    items.push({ z: depth(v, [cx, h / 2, cz]), node: <g key={`fc${i}`}>{node}</g> });
  }
  items.sort((a, b) => b.z - a.z);
  return <g>{items.map((it) => it.node)}</g>;
};

// ------------------------------------------------------------------------------------ ground

/** Ground: asphalt, the cross streets, pavements and kerbs, lane paint, the chalk court and the pad. */
export const Ground: React.FC<{ v: View; f: number; court: number }> = ({ v, f, court }) => {
  const out: React.ReactNode[] = [];
  const Z = 170;
  out.push(face(v, [[-200, -0.01, -Z], [200, -0.01, -Z], [200, -0.01, Z], [-200, -0.01, Z]], tone('#8E7E6C')));
  out.push(face(v, [[-ROAD_X, 0, -Z], [ROAD_X, 0, -Z], [ROAD_X, 0, Z], [-ROAD_X, 0, Z]], tone(C.road)));
  for (const cz of CROSS) out.push(face(v, [[-120, 0.001, cz - CROSS_HALF], [120, 0.001, cz - CROSS_HALF], [120, 0.001, cz + CROSS_HALF], [-120, 0.001, cz + CROSS_HALF]], tone(C.road)));
  // Patches and wear, a few paler, most darker.
  for (let i = 0; i < 40; i++) {
    const x = -6 + random(`rw${i}x`) * 11;
    const z = -40 + random(`rw${i}z`) * 80;
    if (Math.abs(x) < 1.6 && Math.abs(z) < 1.6) continue;
    const w = 0.6 + random(`rw${i}w`) * 2;
    const d = 0.4 + random(`rw${i}d`) * 1.6;
    out.push(face(v, [[x, 0.002, z], [x + w, 0.002, z], [x + w, 0.002, z + d], [x, 0.002, z + d]], tone(i % 5 ? C.roadPatch : C.roadLight)));
  }
  // The deck's hard shadow band across the carriageway and the columns' long shadows (§ 3.2 of the
  // map doc: the one map in the game with a roof, and the band is free contrast).
  const sh = 3.4;
  const shadowOp = 0.22 * (1 - NIGHT);
  if (shadowOp > 0.01) {
    out.push(face(v, [[-DECK.half + sh, 0.003, -Z], [Math.min(ROAD_X, DECK.half + sh), 0.003, -Z], [Math.min(ROAD_X, DECK.half + sh), 0.003, Z], [-DECK.half + sh, 0.003, Z]], '#2A1414', shadowOp));
    for (const cz of COLUMN_Z) for (const sx of [-1, 1]) {
      const x = sx * COLUMN_X;
      out.push(face(v, [[x - 0.7, 0.004, cz + 0.2], [x + 0.7, 0.004, cz - 0.2], [x + 8.4, 0.004, cz + 3.6], [x + 7.2, 0.004, cz + 4.4]], '#2A1414', shadowOp));
    }
  }
  // Lane dashes and the stop lines and zebras at the cross streets.
  for (let i = -30; i < 30; i++) {
    const z = i * 6 + 1.5;
    if (Math.abs(z) < BOX + 1.5) continue;
    if (CROSS.some((c) => Math.abs(z - c) < CROSS_HALF + 2)) continue;
    for (const x of [-3.5, 3.5]) out.push(face(v, [[x - 0.09, 0.004, z], [x + 0.09, 0.004, z], [x + 0.09, 0.004, z + 3], [x - 0.09, 0.004, z + 3]], tone(C.lane), 0.7));
  }
  for (const cz of CROSS) {
    for (let s = -6; s <= 6; s += 1.2) out.push(face(v, [[s, 0.004, cz - CROSS_HALF - 2.6], [s + 0.6, 0.004, cz - CROSS_HALF - 2.6], [s + 0.6, 0.004, cz - CROSS_HALF - 0.6], [s, 0.004, cz - CROSS_HALF - 0.6]].map(([x, y, z]) => [x, y, cz > 0 ? z : -z] as V3), tone(C.lane), 0.8));
  }
  // Potholes at |x| 3.4, off the spawn-to-can line, and a manhole.
  for (const [x, z] of [[3.4, -4.2], [-3.4, 3.1]]) out.push(ellipse(v, x, 0.005, z, 0.45, 0.35, tone('#3A3230')));
  out.push(ellipse(v, 2.1, 0.005, -12.4, 0.5, 0.5, tone('#4A423E')));
  // Pavements, tiles and kerbs.
  for (const sx of [-1, 1]) {
    const x0 = sx * ROAD_X;
    const x1 = sx * 14;
    for (const [z0, z1] of [[-Z, CROSS[1] - CROSS_HALF], [CROSS[1] + CROSS_HALF, CROSS[0] - CROSS_HALF], [CROSS[0] + CROSS_HALF, Z]]) {
      out.push(face(v, [[x0, PAVE_TOP, z0], [x1, PAVE_TOP, z0], [x1, PAVE_TOP, z1], [x0, PAVE_TOP, z1]], lit(C.pave, [0, 1, 0])));
      // Kerb face toward the road, and the white kerb top: the court's long side.
      out.push(face(v, [[x0, 0, z0], [x0, KERB_TOP, z0], [x0, KERB_TOP, z1], [x0, 0, z1]], lit(C.kerbFace, [-sx, 0, 0])));
      out.push(face(v, [[sx * KERB_IN, KERB_TOP + 0.001, z0], [x0, KERB_TOP + 0.001, z0], [x0, KERB_TOP + 0.001, z1], [sx * KERB_IN, KERB_TOP + 0.001, z1]], tone(C.kerb)));
      if (Math.abs(z0) < 60 || Math.abs(z1) < 60) {
        const za = Math.max(z0, -45);
        const zb = Math.min(z1, 45);
        for (let z = Math.ceil(za / 1.2) * 1.2; z < zb; z += 1.2) {
          const lx = line(v, [[x0, PAVE_TOP + 0.002, z], [sx * FACE_X, PAVE_TOP + 0.002, z]]);
          if (lx) out.push(<path key={k()} d={lx} stroke={tone(C.paveLine)} strokeWidth={1.2} fill="none" opacity={0.7} />);
        }
        for (const x of [8.2, 9.4, 10.6]) {
          const lz = line(v, [[sx * x, PAVE_TOP + 0.002, za], [sx * x, PAVE_TOP + 0.002, zb]]);
          if (lz) out.push(<path key={k()} d={lz} stroke={tone(C.paveLine)} strokeWidth={1.2} fill="none" opacity={0.7} />);
        }
      }
    }
  }
  // ⚠️ THE CHALK COURT, IN THE GAME'S SHAPES: the north and south lines across the carriageway at
  // z = +/-7 (the kerbs are the other two sides), the throwing lines at +/-8, the can's circle.
  // Hand-chalked: each line wobbles and is doubled where it was gone over. At `court` > 0 the lines
  // burn pink: Grand Coven's first ring is the court itself.
  const chalk = court > 0 ? mixHex(tone(C.chalk), '#FF7AE6', court) : tone(C.chalk);
  const cw = (z: number) => Math.max(1.4, 38 / Math.max(1, Math.abs(depth(v, [0, 0, z]))));
  const wob = (a: V3, b: V3, key: string) => {
    const pts: V3[] = [];
    for (let i = 0; i <= 14; i++) {
      const t = i / 14;
      pts.push([a[0] + (b[0] - a[0]) * t + (random(`${key}${i}`) - 0.5) * 0.06, 0.006, a[2] + (b[2] - a[2]) * t + (random(`${key}z${i}`) - 0.5) * 0.06]);
    }
    return line(v, pts);
  };
  for (const [z, w] of [[BOX, 1], [-BOX, 1], [LINE, 0.6], [-LINE, 0.6]] as [number, number][]) {
    const d = wob([-KERB_IN, 0, z], [KERB_IN, 0, z], `ch${z}`);
    if (d) out.push(<path key={k()} d={d} stroke={chalk} strokeWidth={cw(z) * w} fill="none" strokeLinecap="square" opacity={0.85} />);
    const d2 = wob([-KERB_IN * 0.6, 0, z + 0.05], [KERB_IN * 0.9, 0, z + 0.04], `ch2${z}`);
    if (d2) out.push(<path key={k()} d={d2} stroke={chalk} strokeWidth={cw(z) * w * 0.5} fill="none" opacity={0.4} />);
  }
  const ring: V3[] = [];
  for (let i = 0; i <= 28; i++) {
    const a = (i / 28) * Math.PI * 2;
    ring.push([Math.cos(a) * 0.85, 0.006, Math.sin(a) * 0.85]);
  }
  const rd = line(v, ring);
  if (rd) out.push(<path key={k()} d={rd} stroke={chalk} strokeWidth={cw(0)} fill="none" opacity={0.9} />);
  if (court > 0.02) {
    const glowD = [BOX, -BOX].map((z) => line(v, [[-KERB_IN, 0.01, z], [KERB_IN, 0.01, z]])).join('') + rd + [-1, 1].map((sx) => line(v, [[sx * 6.85, KERB_TOP + 0.01, -BOX], [sx * 6.85, KERB_TOP + 0.01, BOX]])).join('');
    out.push(<path key={k()} d={glowD} stroke="#F444D4" strokeWidth={14} fill="none" opacity={0.55 * court} filter="url(#glow)" />);
    out.push(<path key={k()} d={[-1, 1].map((sx) => line(v, [[sx * 6.85, KERB_TOP + 0.01, -BOX], [sx * 6.85, KERB_TOP + 0.01, BOX]])).join('')} stroke="#FFB8F0" strokeWidth={4} fill="none" opacity={court} />);
  }
  out.push(<OverclockPad key="pad" v={v} f={f} />);
  return <g>{out}</g>;
};

const ellipse = (v: View, x: number, y: number, z: number, rx: number, rz: number, fill: string, op = 1) => {
  const pts: V3[] = [];
  for (let i = 0; i < 18; i++) {
    const a = (i / 18) * Math.PI * 2;
    pts.push([x + Math.cos(a) * rx, y, z + Math.sin(a) * rz]);
  }
  return face(v, pts, fill, op);
};

/** Flare 0..1 while she is on it. Set by the scene each frame. */
let PAD_FLARE = 0;
export const setPadFlare = (p: number) => { PAD_FLARE = p; };

/**
 * The PC Express overclock pad outside the showroom: a dark mat with three lit bars, as the map
 * builds it (`BuildOverclockPad`), cycling. RGB here is chartreuse, magenta and gold, the map's own
 * glow, plum and gold with the cold channel taken out.
 */
const OverclockPad: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const x0 = PAD.x - PAD.w / 2;
  const x1 = PAD.x + PAD.w / 2;
  const z0 = PAD.z - PAD.d / 2;
  const y = PAVE_TOP + 0.01;
  const out: React.ReactNode[] = [face(v, [[x0, y, z0], [x1, y, z0], [x1, y, z0 + PAD.d], [x0, y, z0 + PAD.d]], tone('#2E2626'))];
  const cols = ['#C8E040', '#F050C8', '#F8B824'];
  for (let i = 0; i < 3; i++) {
    const zz = z0 + 0.35 + i * 0.55;
    const c = cols[(i + Math.floor(f / 10)) % 3];
    out.push(face(v, [[x0 + 0.15, y + 0.002, zz], [x1 - 0.15, y + 0.002, zz], [x1 - 0.15, y + 0.002, zz + 0.25], [x0 + 0.15, y + 0.002, zz + 0.25]], c, 0.85 + 0.15 * PAD_FLARE));
  }
  if (PAD_FLARE > 0.01) out.push(face(v, [[x0 - 0.4, y + 0.003, z0 - 0.4], [x1 + 0.4, y + 0.003, z0 - 0.4], [x1 + 0.4, y + 0.003, z0 + PAD.d + 0.4], [x0 - 0.4, y + 0.003, z0 + PAD.d + 0.4]], '#F8E060', 0.5 * PAD_FLARE, { filter: 'url(#glow)' }));
  return <g>{out}</g>;
};

// ------------------------------------------------------------------------------------ the rows

type Bld = { side: -1 | 1; z0: number; z1: number; h: number; set: number; body: string; trim: string; seed: string; floors: number; shop: boolean; tank: boolean };

/**
 * The two rows of mid-rises, far to near. The map's near row varies width, height, setback and
 * palette per instance and the two sides carry different sequences on purpose (a mirrored street reads
 * as a level-editor corridor, `BuildShopfronts`); this does the same, and keeps the cross streets at
 * |z| = 31 open so the road runs on instead of ending in a wall.
 */
const ROWS: Bld[] = (() => {
  const out: Bld[] = [];
  const bodies = [C.cream, C.cream2, C.rose, C.cream, '#EFD9B0'];
  for (const side of [-1, 1] as const) {
    let z = -150;
    let i = 0;
    while (z < 150) {
      const w = 6 + random(`b${side}${i}w`) * 7;
      let z1 = z + w;
      const hitsCross = CROSS.some((c) => z1 > c - CROSS_HALF - 0.5 && z < c + CROSS_HALF + 0.5);
      if (hitsCross) {
        const c = CROSS.find((cc) => z1 > cc - CROSS_HALF - 0.5 && z < cc + CROSS_HALF + 0.5)!;
        if (z < c - CROSS_HALF - 3) z1 = c - CROSS_HALF - 0.5;
        else { z = c + CROSS_HALF + 0.5; continue; }
      }
      // PC Express is its own box on the west wall; the building behind it is set back.
      const onPcex = side === -1 && z1 > PCEX.z0 - 0.2 && z < PCEX.z1 + 0.2;
      const floors = 2 + Math.floor(random(`b${side}${i}f`) * 3.2);
      out.push({
        side, z0: z, z1, set: onPcex ? 3.2 : 1.2 + random(`b${side}${i}s`) * 1.6, h: 4.2 + floors * 3.1, floors,
        body: bodies[(i + (side > 0 ? 2 : 0)) % bodies.length], trim: random(`b${side}${i}t`) < 0.3 ? C.trimRose : C.trim,
        seed: `b${side}${i}`, shop: !onPcex, tank: random(`b${side}${i}k`) < 0.4,
      });
      z = z1 + 0.25;
      i++;
    }
  }
  return out;
})();

export type Glow = { at: V3; r: number; colour: string; a: number };

/** One building: the upper mass set back, windows in sage frames, a glass shop box at the wall line. */
const building = (v: View, b: Bld, f: number): Item[] => {
  const s = b.side;
  const fx = s * (FACE_X + b.set);
  const back = s * (FACE_X + b.set + 9);
  const [x0, x1] = s < 0 ? [back, fx] : [fx, back];
  const items: Item[] = [];
  const nodes: React.ReactNode[] = [];
  nodes.push(...box(v, x0, x1, 0, b.h, b.z0, b.z1, { side: b.body, top: b.trim }));
  // Floor bands and a parapet cap in the trim colour.
  const street = (s < 0 && v.x > fx) || (s > 0 && v.x < fx);
  if (street) {
    const n: V3 = [-s, 0, 0];
    for (let fl = 1; fl <= b.floors; fl++) {
      const y = 4.2 + (fl - 1) * 3.1;
      nodes.push(face(v, [[fx - s * 0.02, y - 0.18, b.z0], [fx - s * 0.02, y, b.z0], [fx - s * 0.02, y, b.z1], [fx - s * 0.02, y - 0.18, b.z1]], lit(b.trim, n)));
      const bays = Math.max(1, Math.floor((b.z1 - b.z0) / 2.6));
      for (let q = 0; q < bays; q++) {
        const za = b.z0 + ((q + 0.5) * (b.z1 - b.z0)) / bays - 0.7;
        const zb = za + 1.4;
        const wy = y + 0.6;
        nodes.push(face(v, [[fx - s * 0.03, wy - 0.12, za - 0.12], [fx - s * 0.03, wy + 1.62, za - 0.12], [fx - s * 0.03, wy + 1.62, zb + 0.12], [fx - s * 0.03, wy - 0.12, zb + 0.12]], lit(b.trim, n)));
        const on = random(`${b.seed}w${fl}${q}`) < 0.6;
        const glass = on ? emit(C.glass, random(`${b.seed}c${fl}${q}`) < 0.3 ? '#FFC870' : '#FFE0A0') : tone(mul(C.glass, 0.8, 0.8, 0.78));
        const flick = on ? 0.85 + 0.15 * loopNoise(`${b.seed}l${fl}${q}`, f, 0.8) : 1;
        nodes.push(face(v, [[fx - s * 0.04, wy, za], [fx - s * 0.04, wy + 1.5, za], [fx - s * 0.04, wy + 1.5, zb], [fx - s * 0.04, wy, zb]], glass, on ? flick : 1));
      }
    }
  }
  if (b.tank && v.y < b.h + 30) nodes.push(...box(v, s * (FACE_X + b.set + 3), s * (FACE_X + b.set + 3) + s * 1.6, b.h, b.h + 1.8, (b.z0 + b.z1) / 2 - 0.8, (b.z0 + b.z1) / 2 + 0.8, { side: '#C9B8A0', top: '#B8A690' }));
  items.push({ z: depth(v, [(x0 + x1) / 2, b.h / 2, (b.z0 + b.z1) / 2]), node: <g key={b.seed}>{nodes}</g> });
  if (b.shop) items.push(...shopBox(v, b, f));
  return items;
};

/** The single-storey glass shop box standing out at the wall line, as the map's row has them. */
const shopBox = (v: View, b: Bld, f: number): Item[] => {
  const s = b.side;
  const front = s * FACE_X;
  const rear = s * (FACE_X + b.set);
  const [x0, x1] = s < 0 ? [rear, front] : [front, rear];
  const z0 = b.z0 + 0.3;
  const z1 = b.z1 - 0.3;
  const h = 3.4;
  const nodes: React.ReactNode[] = [];
  nodes.push(...box(v, x0, x1, PAVE_TOP, h, z0, z1, { side: C.shopWall, top: '#7E8A74' }));
  const n: V3 = [-s, 0, 0];
  if ((s < 0 && v.x > front) || (s > 0 && v.x < front)) {
    const fx = front - s * 0.02;
    // Glass front: dark inside, warm when the lights are on at night, mullions in the frame colour.
    nodes.push(face(v, [[fx, 0.6, z0 + 0.25], [fx, 2.7, z0 + 0.25], [fx, 2.7, z1 - 0.25], [fx, 0.6, z1 - 0.25]], emit(mul(C.shopInside, 1.1, 1.05, 1), '#FFC878')));
    for (let m = 0; m <= 4; m++) {
      const z = z0 + 0.25 + ((z1 - z0 - 0.5) * m) / 4;
      nodes.push(face(v, [[fx - s * 0.01, 0.6, z - 0.06], [fx - s * 0.01, 2.7, z - 0.06], [fx - s * 0.01, 2.7, z + 0.06], [fx - s * 0.01, 0.6, z + 0.06]], lit(C.shopFrame, n)));
    }
    nodes.push(face(v, [[fx - s * 0.01, 2.7, z0], [fx - s * 0.01, h, z0], [fx - s * 0.01, h, z1], [fx - s * 0.01, 2.7, z1]], lit(C.shopFrame, n)));
    nodes.push(face(v, [[fx - s * 0.01, PAVE_TOP, z0], [fx - s * 0.01, 0.6, z0], [fx - s * 0.01, 0.6, z1], [fx - s * 0.01, PAVE_TOP, z1]], lit(C.shopWall, n)));
    // A shelf of goods behind the glass: monitors on the computer strip, blocks of colour elsewhere.
    for (let g = 0; g < 4; g++) {
      const z = z0 + 0.6 + g * ((z1 - z0 - 1.2) / 4);
      nodes.push(face(v, [[fx + s * 0.4, 1.3, z], [fx + s * 0.4, 1.9, z], [fx + s * 0.4, 1.9, z + 0.6], [fx + s * 0.4, 1.3, z + 0.6]], emit(g % 2 ? '#4E5A48' : '#6A4A40', '#FFE4A8'), 0.8));
    }
  }
  void f;
  return [{ z: depth(v, [(x0 + x1) / 2, 1.7, (z0 + z1) / 2]) - 0.5, node: <g key={`${b.seed}sh`}>{nodes}</g> }];
};

/** PC Express: the authored showroom, red lightbox with white letters, glass front, the slim overhang. */
const pcExpress = (v: View, f: number): Item[] => {
  const x0 = -FACE_X - 3.2;
  const x1 = -FACE_X;
  const { z0, z1, h } = PCEX;
  const nodes: React.ReactNode[] = [];
  nodes.push(...box(v, x0, x1, PAVE_TOP, h, z0, z1, { side: '#E9E2D2', top: '#8E8A7E' }));
  if (v.x > x1) {
    const n: V3 = [1, 0, 0];
    const fx = x1 + 0.02;
    nodes.push(face(v, [[fx, 0.4, z0 + 0.2], [fx, 2.55, z0 + 0.2], [fx, 2.55, z1 - 0.2], [fx, 0.4, z1 - 0.2]], emit('#4A4644', '#FFE6B8')));
    for (let m = 0; m <= 6; m++) {
      const z = z0 + 0.2 + ((z1 - z0 - 0.4) * m) / 6;
      nodes.push(face(v, [[fx + 0.01, 0.4, z - 0.04], [fx + 0.01, 2.55, z - 0.04], [fx + 0.01, 2.55, z + 0.04], [fx + 0.01, 0.4, z + 0.04]], lit('#B8B4AC', n)));
    }
    // Monitors in the window, glowing: a showroom.
    for (let g = 0; g < 6; g++) {
      const z = z0 + 0.5 + g * 0.72;
      const on = 0.75 + 0.25 * loopNoise(`pcm${g}`, f, 2, 3);
      nodes.push(face(v, [[fx + 0.5, 1.2, z], [fx + 0.5, 1.75, z], [fx + 0.5, 1.75, z + 0.55], [fx + 0.5, 1.2, z + 0.55]], emit('#D9E4B8', '#F8F0C8'), on));
    }
    // The overhang, red lip, and the lightbox.
    nodes.push(...box(v, x1, x1 + 0.8, 2.55, 2.72, z0, z1, { side: '#C8322A', top: '#E8E2D2', bottom: '#8E2A24' }));
  }
  const sign = onPlane(v, [x1 + 0.12, 4.1, z0 + 0.3], FACING.east.u, FACING.east.dn, z1 - z0 - 0.6, 1.3,
    <g>
      <rect x={-0.08} y={-0.08} width={z1 - z0 - 0.44} height={1.46} fill={tone('#D8D2C4')} />
      <rect x={0} y={0} width={z1 - z0 - 0.6} height={1.3} fill={emit('#D12A2A', '#FF4A3A')} />
      <rect x={0} y={1.02} width={z1 - z0 - 0.6} height={0.28} fill={emit('#9A1A1E', '#C8242A')} />
      <Paint text="PC EXPRESS" x={(z1 - z0 - 0.6) / 2} y={0.9} h={0.62} fill={emit('#FFFFFF', '#FFF6E6')} fit={(z1 - z0 - 0.6) * 0.86} slant={0.16} weight={0.24} wobble={0.2} bounce={0} shadow="rgba(80,10,10,0.6)" seed="pcex" />
    </g>, 'pcexsign');
  if (sign) nodes.push(sign);
  if (NIGHT > 0.05) {
    const [gx, gy] = project(v, [x1 + 0.2, 3.5, (z0 + z1) / 2]);
    nodes.push(<circle key="pcg" cx={gx} cy={gy} r={140} fill="#FF4A3A" opacity={0.3 * NIGHT} filter="url(#glowBig)" />);
  }
  return [{ z: depth(v, [x1 - 1, 2, (z0 + z1) / 2]) - 0.8, node: <g key="pcex">{nodes}</g> }];
};

export const Rows = (v: View, f: number): Item[] => {
  KEY = 0;
  const items: Item[] = [];
  for (const b of ROWS) {
    if (depth(v, [b.side * (FACE_X + b.set + 4), b.h / 2, (b.z0 + b.z1) / 2]) < -30) continue;
    items.push(...building(v, b, f));
  }
  items.push(...pcExpress(v, f));
  return items;
};

// ------------------------------------------------------------------------------------ the guideway

/** Where the consist's nose is, metres north; null for no train. */
export const Deck: React.FC<{ v: View; f: number; train: number | null; strobe: number }> = ({ v, f, train }) => {
  const out: React.ReactNode[] = [];
  const Z = 170;
  const h = DECK.half;
  // Parapets and the fascia either side; the soffit; its ribs and the two girder lines.
  for (const sx of [-1, 1]) {
    out.push(face(v, [[sx * h, DECK.under, -Z], [sx * h, DECK.top + 0.55, -Z], [sx * h, DECK.top + 0.55, Z], [sx * h, DECK.under, Z]], lit(C.deckSide, [sx, 0, 0])));
    out.push(face(v, [[sx * h, DECK.under - 0.001, -Z], [sx * (h - 0.4), DECK.under - 0.001, -Z], [sx * (h - 0.4), DECK.under - 0.001, Z], [sx * h, DECK.under - 0.001, Z]], tone(C.deckRib)));
  }
  if (v.y < DECK.under) {
    out.push(face(v, [[-h, DECK.under, -Z], [h, DECK.under, -Z], [h, DECK.under, Z], [-h, DECK.under, Z]], tone(C.deckUnder)));
    for (let z = -120; z <= 120; z += 4.0) out.push(face(v, [[-h, DECK.under - 0.02, z], [h, DECK.under - 0.02, z], [h, DECK.under - 0.02, z + 0.35], [-h, DECK.under - 0.02, z + 0.35]], tone(C.deckRib)));
    for (const x of [-1.6, 1.6]) out.push(face(v, [[x - 0.35, DECK.under - 0.03, -Z], [x + 0.35, DECK.under - 0.03, -Z], [x + 0.35, DECK.under - 0.03, Z], [x - 0.35, DECK.under - 0.03, Z]], tone(mul(C.deckRib, 0.9, 0.9, 0.9))));
    // Under-deck lamps on the caps (the map's mercury lamps), a warm pool at night.
    for (const cz of COLUMN_Z) for (const sx of [-1, 1]) {
      const [lx, ly] = project(v, [sx * COLUMN_X, DECK.under - 0.9, cz]);
      if (depth(v, [sx * COLUMN_X, 7, cz]) < 1) continue;
      const s = Math.min(3, 14 / Math.max(1, depth(v, [sx * COLUMN_X, 7, cz])));
      if (NIGHT > 0.02) out.push(<circle key={k()} cx={lx} cy={ly} r={50 * s} fill="#FFD890" opacity={0.4 * NIGHT} filter="url(#glowBig)" />);
      out.push(<rect key={k()} x={lx - 8 * s} y={ly - 2 * s} width={16 * s} height={4 * s} fill={emit('#E8E0C8', '#FFF4D0')} />);
    }
  }
  // Catenary masts on the deck.
  for (let z = -120; z <= 120; z += 12) {
    for (const sx of [-1, 1]) {
      const x = sx * (h - 0.3);
      out.push(face(v, [[x - 0.1, DECK.top, z], [x - 0.1, DECK.top + 4.2, z], [x + 0.1, DECK.top + 4.2, z], [x + 0.1, DECK.top, z]], tone('#4A4A40')));
    }
    out.push(face(v, [[-(h - 0.3), DECK.top + 3.9, z], [h - 0.3, DECK.top + 3.9, z], [h - 0.3, DECK.top + 4.1, z], [-(h - 0.3), DECK.top + 4.1, z]], tone('#4A4A40')));
  }
  if (train !== null) out.push(<Train key="train" v={v} nose={train} f={f} />);
  return <g>{out}</g>;
};

/**
 * The consist on the westbound track, three cars, nose north. Cream body, a maroon waist band and
 * lit windows: the nearest this palette comes to the line's livery without blue.
 */
const Train: React.FC<{ v: View; nose: number; f: number }> = ({ v, nose }) => {
  const out: React.ReactNode[] = [];
  const cx = TRACK_X[0];
  const w = TRAIN.width / 2;
  const y0 = DECK.top + 0.25;
  const y1 = y0 + TRAIN.height;
  const car = TRAIN.length / TRAIN.cars;
  for (let c = 0; c < TRAIN.cars; c++) {
    const z1 = nose - c * car;
    const z0 = z1 - car + 0.25;
    out.push(...box(v, cx - w, cx + w, y0, y1, z0, z1, { side: '#EDE2CC', end: '#D8CCB4', top: '#C8BCA4' }));
    for (const sx of [-1, 1]) {
      if ((sx < 0 && v.x > cx - w) || (sx > 0 && v.x < cx + w)) continue;
      const x = cx + sx * (w + 0.02);
      out.push(face(v, [[x, y0 + 0.5, z0], [x, y0 + 0.8, z0], [x, y0 + 0.8, z1], [x, y0 + 0.5, z1]], tone('#8E2A3A')));
      for (let q = 0; q < 4; q++) {
        const za = z0 + 0.6 + q * 1.25;
        out.push(face(v, [[x, y0 + 1.1, za], [x, y0 + 2.5, za], [x, y0 + 2.5, za + 0.95], [x, y0 + 1.1, za + 0.95]], emit('#6A5A50', '#FFE4A0')));
      }
    }
  }
  if (v.z > nose) {
    const [lx, ly] = project(v, [cx, y0 + 0.9, nose + 0.05]);
    const s = Math.min(3, 30 / Math.max(1, depth(v, [cx, y0, nose])));
    out.push(<circle key="lg" cx={lx} cy={ly} r={50 * s + 10} fill="#FFF6D6" opacity={0.6} filter="url(#glowBig)" />);
  }
  return <g>{out}</g>;
};

// ------------------------------------------------------------------------------------ near things

/** The columns and their T caps, every one down the line. */
export const Columns = (v: View): Item[] => {
  const items: Item[] = [];
  for (const cz of COLUMN_Z) for (const sx of [-1, 1]) {
    const x = sx * COLUMN_X;
    if (depth(v, [x, 4, cz]) < -2) continue;
    const nodes: React.ReactNode[] = [];
    nodes.push(...box(v, x - COLUMN_HALF, x + COLUMN_HALF, 0, DECK.under - 1.3, cz - COLUMN_HALF, cz + COLUMN_HALF, { side: C.column }));
    // A plinth, and the cap flaring out under the deck.
    nodes.push(...box(v, x - 0.85, x + 0.85, 0, 0.35, cz - 0.85, cz + 0.85, { side: C.cap, top: C.cap }));
    nodes.push(...box(v, x - 1.0, x + 1.0, DECK.under - 1.3, DECK.under, cz - 1.0, cz + 1.0, { side: C.cap, bottom: C.deckRib }));
    // BAWAL UMIHI DITO on the two live columns' street faces (docs/Ilalim_Ng_Tulay.md § 4.6).
    if (Math.abs(cz) === 10) {
      const fc = sx < 0 ? FACING.east : FACING.west;
      const o: V3 = sx < 0 ? [x + COLUMN_HALF + 0.02, 1.9, cz - 0.55] : [x - COLUMN_HALF - 0.02, 1.9, cz + 0.55];
      const p = onPlane(v, o, fc.u, fc.dn, 1.1, 0.72, <g>
        <rect width={1.1} height={0.72} fill={tone('#F4EEE0')} />
        <rect width={1.1} height={0.72} fill="none" stroke={tone('#B8282A')} strokeWidth={0.06} />
        {['BAWAL', 'UMIHI', 'DITO'].map((t, i) => <Paint key={t} text={t} x={0.55} y={0.25 + i * 0.2} h={0.14} fill={tone('#B8282A')} weight={0.2} wobble={1} seed={`bw${t}`} />)}
      </g>);
      if (p) nodes.push(p);
    }
    items.push({ z: depth(v, [x, 3, cz]), node: <g key={`col${sx}${cz}`}>{nodes}</g> });
  }
  return items;
};

/** Brown timber poles with a dark crossarm, and their wires running along each pavement. */
export const Poles = (v: View, f: number): Item[] => {
  const items: Item[] = [];
  for (const sx of [-1, 1]) {
    const x = sx * 7.6;
    for (const z of POLE_Z) {
      if (depth(v, [x, 3, z]) < -1) continue;
      const nodes: React.ReactNode[] = [];
      nodes.push(...box(v, x - 0.16, x + 0.16, PAVE_TOP, 7.4, z - 0.16, z + 0.16, { side: C.pole }));
      nodes.push(...box(v, x - 0.28, x + 0.28, PAVE_TOP, 1.0, z - 0.28, z + 0.28, { side: C.poleCap, top: C.poleCap }));
      nodes.push(...box(v, x - 1.2, x + 1.2, 6.9, 7.15, z - 0.12, z + 0.12, { side: C.poleCap, top: C.poleCap, bottom: C.poleCap }));
      nodes.push(...box(v, x - 0.3, x + 0.3, 7.4, 7.7, z - 0.3, z + 0.3, { side: C.poleCap, top: C.poleCap }));
      items.push({ z: depth(v, [x, 3.5, z]), node: <g key={`pl${sx}${z}`}>{nodes}</g> });
    }
    // Wires: three per side, sagging between poles, swaying a hair.
    for (let w = 0; w < 3; w++) {
      const pts: V3[] = [];
      for (let i = 0; i < POLE_Z.length - 1; i++) {
        const za = POLE_Z[i];
        const zb = POLE_Z[i + 1];
        for (let t = 0; t <= 1; t += 0.125) {
          const sag = Math.sin(t * Math.PI) * (0.7 + 0.1 * w) + 0.04 * loopSin(f, 2, w + i);
          pts.push([x + sx * (-1.0 + w * 1.0), 7.1 - sag, za + (zb - za) * t]);
        }
      }
      const d = line(v, pts);
      if (d) items.push({ z: depth(v, [x, 7, v.z + 6]), node: <path key={`wr${sx}${w}`} d={d} stroke={tone(C.wire)} strokeWidth={2.2} fill="none" /> });
    }
  }
  return items;
};

/** Lamp columns with a hooked arm over the kerb; they come on with the night. */
export const Lamps = (v: View): Item[] => {
  const items: Item[] = [];
  for (const sx of [-1, 1]) {
    const x = sx * 7.3;
    for (const z of LAMP_Z) {
      if (depth(v, [x, 3, z]) < -1) continue;
      const nodes: React.ReactNode[] = [];
      nodes.push(...box(v, x - 0.11, x + 0.11, PAVE_TOP, 6.2, z - 0.11, z + 0.11, { side: C.lamp }));
      nodes.push(...box(v, Math.min(x, x - sx * 1.4), Math.max(x, x - sx * 1.4), 6.1, 6.3, z - 0.1, z + 0.1, { side: C.lamp, bottom: C.lamp }));
      const hx = x - sx * 1.4;
      nodes.push(...box(v, hx - 0.28, hx + 0.28, 5.85, 6.1, z - 0.18, z + 0.18, { side: '#8EA282', bottom: '#F8F0D0' }));
      if (NIGHT > 0.02) {
        const [lx, ly] = project(v, [hx, 5.8, z]);
        const s = Math.min(3, 12 / Math.max(1, depth(v, [hx, 5.8, z])));
        nodes.push(<circle key="g" cx={lx} cy={ly} r={46 * s} fill="#FFD890" opacity={0.5 * NIGHT} filter="url(#glowBig)" />);
        const pool = ellipse(v, hx, 0.02, z, 2.4, 2.6, '#FFC870', 0.16 * NIGHT);
        if (pool) nodes.push(pool);
      }
      items.push({ z: depth(v, [x, 3, z]), node: <g key={`lp${sx}${z}`}>{nodes}</g> });
    }
  }
  return items;
};

/**
 * Faded banderitas left up across the street after the fiesta, as they always are (discoverphilippines,
 * *The Life and Afterlife of Filipino Fiesta Banderitas*). Triangles in warm plastic colours, lifting
 * in the draught; the whole string whips when Grand Coven stamps (`whip`).
 */
export const Banderitas = (v: View, f: number, whip: number): Item[] => {
  const items: Item[] = [];
  const cols = ['#F2C23A', '#E8483A', '#F7EEDC', '#F07AB0', '#9CC23A', '#F08A3A'];
  for (const [si, z] of [-15, -3.5, 12.5, 23].entries()) {
    const nodes: React.ReactNode[] = [];
    const n = 30;
    const pts: V3[] = [];
    for (let i = 0; i <= n; i++) {
      const t = i / n;
      const sag = Math.sin(t * Math.PI) * 0.9 + 0.06 * loopSin(f, 3, si + t * 4) + 0.3 * whip * Math.sin(t * 9 + f * 0.8);
      pts.push([-10.6 + 21.2 * t, 6.9 - sag, z + 0.5 * Math.sin(t * Math.PI)]);
    }
    const d = line(v, pts);
    if (!d) continue;
    nodes.push(<path key="s" d={d} stroke={tone('#3A2A26')} strokeWidth={1.6} fill="none" />);
    for (let i = 1; i < n; i++) {
      const [x, y, zz] = pts[i];
      const flap = 0.12 * loopNoise(`bd${si}${i}`, f, 1.4, 3) + 0.35 * whip * Math.sin(i + f);
      const tri = poly(v, [[x - 0.2, y, zz], [x + 0.2, y, zz], [x + flap * 0.4, y - 0.42, zz + flap]]);
      if (tri) nodes.push(<path key={i} d={tri} fill={tone(cols[(i + si) % cols.length])} opacity={0.92} />);
    }
    items.push({ z: depth(v, [0, 6.5, z]) - 0.2, node: <g key={`bd${si}`}>{nodes}</g> });
  }
  return items;
};

/** A few parked cars and a jeepney beyond the north wall, boxy like the map's kit. */
export const Traffic = (v: View, f: number): Item[] => {
  const items: Item[] = [];
  const cars: [number, number, string][] = [[3.4, 21.5, '#8FA07A'], [5.2, 26, '#D98C8C'], [-4.9, 24.5, '#E8D8B0'], [4.4, -22.5, '#D9A870'], [-3.6, -26.5, '#9AA87E']];
  for (const [i, [x, z, col]] of cars.entries()) {
    if (depth(v, [x, 1, z]) < 0) continue;
    const nodes: React.ReactNode[] = [];
    nodes.push(...box(v, x - 0.9, x + 0.9, 0.25, 1.0, z - 2.1, z + 2.1, { side: col, top: col }));
    nodes.push(...box(v, x - 0.8, x + 0.8, 1.0, 1.55, z - 1.1, z + 0.9, { side: '#4A4440', top: col, end: '#5A5450' }));
    nodes.push(...box(v, x - 0.95, x + 0.95, 0, 0.45, z - 1.7, z - 1.1, { side: '#231A1A' }));
    nodes.push(...box(v, x - 0.95, x + 0.95, 0, 0.45, z + 1.1, z + 1.7, { side: '#231A1A' }));
    items.push({ z: depth(v, [x, 1, z]), node: <g key={`car${i}`}>{nodes}</g> });
  }
  items.push(jeepney(v, f));
  return items;
};

/** The jeepney north of the wall, nose toward us: silver, red and yellow, and a neon route board. */
const jeepney = (v: View, f: number): Item => {
  const x0 = -4.0;
  const x1 = -1.9;
  const z0 = 24.5;
  const z1 = 31.0;
  const nodes: React.ReactNode[] = [];
  nodes.push(...box(v, x0, x1, 0.55, 2.25, z0 + 1.2, z1, { side: '#D8D2C4', top: '#C83A2E', end: '#C83A2E' }));
  nodes.push(...box(v, x0 + 0.15, x1 - 0.15, 0.5, 1.45, z0, z0 + 1.2, { side: '#D8D2C4', top: '#C8C0B0', end: '#B8B0A0' }));
  nodes.push(...box(v, x0 - 0.02, x1 + 0.02, 1.0, 1.2, z0 + 1.2, z1, { side: '#F2C23A' }));
  nodes.push(...box(v, x0 - 0.1, x1 + 0.1, 2.25, 2.45, z0 + 1.0, z1 + 0.1, { side: '#8E2420', top: '#A8322A' }));
  nodes.push(...box(v, x0 - 0.05, x1 + 0.05, 0, 0.6, z0 + 0.6, z0 + 1.3, { side: '#1E1616' }));
  nodes.push(...box(v, x0 - 0.05, x1 + 0.05, 0, 0.6, z1 - 1.4, z1 - 0.7, { side: '#1E1616' }));
  const board = onPlane(v, [x0 + 0.25, 2.2, z0 + 1.15], FACING.south.u, FACING.south.dn, x1 - x0 - 0.5, 0.36, <g>
    <rect width={x1 - x0 - 0.5} height={0.36} fill="#141010" />
    <Paint text="CUBAO - GILMORE" x={(x1 - x0 - 0.5) / 2} y={0.28} h={0.2} fill={['#C8F040', '#FF6AD8', '#F8E040'][Math.floor(f / 45) % 3]} fit={(x1 - x0 - 0.5) * 0.9} weight={0.16} wobble={0.3} seed="jr" />
  </g>);
  if (board) nodes.push(board);
  return { z: depth(v, [(x0 + x1) / 2, 1.2, (z0 + z1) / 2]), node: <g key="jeep">{nodes}</g> };
};

/**
 * The bridge hoop beside the south west column: a post, a bracket and a ring at 3 m, the map's
 * one-of-a-kind trick shot (docs/Ilalim_Ng_Tulay.md § 4.1, *"TRES!"*). `flash` lights the rim when the
 * slipper passes through it.
 */
export const Hoop = (v: View, flash: number): Item[] => {
  const nodes: React.ReactNode[] = [];
  const { x, z, rim, r } = HOOP;
  nodes.push(...box(v, x - 0.1, x + 0.1, PAVE_TOP, rim + 0.3, z + 0.35, z + 0.55, { side: '#5A4A44' }));
  nodes.push(...box(v, x - 0.06, x + 0.06, rim - 0.05, rim + 0.05, z, z + 0.45, { side: '#5A4A44', top: '#5A4A44' }));
  // The ring stands upright, facing along the street, so it is a circle to the south line.
  const ring2: V3[] = [];
  for (let i = 0; i <= 24; i++) {
    const a = (i / 24) * Math.PI * 2;
    ring2.push([x + Math.cos(a) * r, rim + Math.sin(a) * r, z]);
  }
  const d = line(v, ring2);
  if (d) {
    const w = Math.max(3, 70 / Math.max(1, depth(v, [x, rim, z])));
    nodes.push(<path key="r" d={d} stroke={mixHex(tone('#F08A2A'), '#FFF0A0', flash)} strokeWidth={w} fill="none" />);
    if (flash > 0.02) nodes.push(<path key="rg" d={d} stroke="#FFE070" strokeWidth={w * 4} fill="none" opacity={0.6 * flash} filter="url(#glow)" />);
  }
  return [{ z: depth(v, [x, rim, z]), node: <g key="hoop">{nodes}</g> }];
};
