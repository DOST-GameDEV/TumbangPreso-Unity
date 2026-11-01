import React from 'react';
import { random } from 'remotion';
import { idleSin, INTRO, ph } from './time';
import { depth, poly, project, projectDir, scaleAt, V3, View } from '../phaister/view';
import { box, Item, mixHex, onPlane } from '../phaister/set';

/*
 * What makes Ermita a place and not a stage set (owner, 2026-11-01: "ang empty"). The map's own poster and
 * LORE.md's list: the old civic buildings, electronics shops, pisonet, pares, the jeepney, palms, parols, a
 * sorbetes cart, the wires. Everything is boxes, so it holds from every lens in the film.
 */
let KEY = 0;
const k = () => `c${KEY++}`;
const face = (v: View, pts: V3[], fill: string, op = 1) => {
  const d = poly(v, pts);
  return d ? <path key={k()} d={d} fill={fill} opacity={op} /> : null;
};
const LIT = ['#F4C26C', '#FFD98A', '#FFB86A', '#F7E3A8'];
const DARK = '#272033';

type Block = { x0: number; x1: number; z0: number; z1: number; h: number; body: string; lit: number; roof?: string; seed: string };

/** A building with windows on every face the lens can see. */
const block = (v: View, b: Block): Item | null => {
  const at: V3 = [(b.x0 + b.x1) / 2, b.h / 2, (b.z0 + b.z1) / 2];
  const d0 = depth(v, at);
  if (d0 < 1) return null;
  const n: React.ReactNode[] = [];
  n.push(<g key="b">{box(v, b.x0, b.x1, 0, b.h, b.z0, b.z1, { side: mixHex(b.body, '#1B1030', 0.28), end: b.body, top: b.roof ?? mixHex(b.body, '#FFFFFF', 0.08) })}</g>);
  const far = d0 > 70;
  const stepY = far ? 7 : 3.6;
  const stepH = far ? 8 : 3.5;
  const wins = (u0: number, u1: number, put: (u: number, y: number, w: number, h: number) => V3[], seed: string) => {
    const cols = Math.max(2, Math.round((u1 - u0) / stepH));
    const du = (u1 - u0) / cols;
    for (let r = 0; r < Math.floor((b.h - 4) / stepY); r++) {
      for (let c = 0; c < cols; c++) {
        const on = random(`${b.seed}${seed}${r}.${c}`) < b.lit;
        n.push(face(v, put(u0 + c * du + du * 0.26, 3.2 + r * stepY, du * 0.48, stepY * 0.62), on ? LIT[(r + c) % LIT.length] : DARK));
      }
    }
  };
  if (v.z < b.z0) wins(b.x0, b.x1, (u, y, w, h) => [[u, y, b.z0 - 0.02], [u + w, y, b.z0 - 0.02], [u + w, y + h, b.z0 - 0.02], [u, y + h, b.z0 - 0.02]], 's');
  if (v.z > b.z1) wins(b.x0, b.x1, (u, y, w, h) => [[u, y, b.z1 + 0.02], [u + w, y, b.z1 + 0.02], [u + w, y + h, b.z1 + 0.02], [u, y + h, b.z1 + 0.02]], 'n');
  if (v.x < b.x0) wins(b.z0, b.z1, (u, y, w, h) => [[b.x0 - 0.02, y, u], [b.x0 - 0.02, y, u + w], [b.x0 - 0.02, y + h, u + w], [b.x0 - 0.02, y + h, u]], 'w');
  if (v.x > b.x1) wins(b.z0, b.z1, (u, y, w, h) => [[b.x1 + 0.02, y, u], [b.x1 + 0.02, y, u + w], [b.x1 + 0.02, y + h, u + w], [b.x1 + 0.02, y + h, u]], 'e');
  return { z: d0, node: <g key={k()}>{n}</g> };
};

const TONES = ['#6B5A72', '#7A5F58', '#5A4A68', '#4C4060', '#6A4E5E', '#57506E', '#7C6A64', '#463A5C'];

/** The ring of city on every side of the lot, far towers behind it. */
const BLOCKS: Block[] = (() => {
  const out: Block[] = [];
  const add = (x0: number, x1: number, z0: number, z1: number, h: number, i: number, lit = 0.34) => out.push({ x0, x1, z0, z1, h, body: TONES[i % TONES.length], lit, seed: `r${i}` });
  // The north side beyond the hall, the west side, the south side, the east side past the viaduct.
  [-78, -60, -44, 14, 32, 50, 68].forEach((x, i) => add(x, x + 16, 25, 41, 13 + 9 * random(`n${i}`), i));
  [-70, -52, -34, -16, 2, 20, 38, 56].forEach((z, i) => add(-40, -24, z, z + 16, 12 + 12 * random(`w${i}`), i + 2));
  [-40, -22, -4, 14].forEach((x, i) => add(x, x + 16, -44, -28, 12 + 12 * random(`s${i}`), i + 4));
  [-80, -60, -40, -20, 0, 20, 40, 60, 80].forEach((z, i) => add(27, 43, z, z + 17, 13 + 16 * random(`e${i}`), i + 1));
  // Downtown behind all of it: the towers that fill the sky.
  for (let i = 0; i < 26; i++) {
    const a = (i / 26) * 6.283;
    const r = 105 + 40 * random(`ta${i}`);
    const x = Math.cos(a) * r;
    const z = Math.sin(a) * r;
    add(x - 9, x + 9, z - 9, z + 9, 34 + 46 * random(`th${i}`), i, 0.42);
  }
  return out;
})();

export const FarBlocks = (v: View): Item[] => BLOCKS.map((b) => block(v, b)).filter((x): x is Item => !!x);

/** The two old civic buildings of the poster: tall windows, cornices, a pediment, red roofs. */
export const OldBuildings = (v: View): Item[] => {
  const out: Item[] = [];
  const stone = '#B9A698';
  const stoneDark = '#8E7E80';
  const trim = '#D4C4B0';
  const hall = (x0: number, x1: number, z: number, h: number, seed: string, roof: string) => {
    const at: V3 = [(x0 + x1) / 2, h / 2, z];
    if (depth(v, at) < 1) return;
    const n: React.ReactNode[] = [];
    n.push(<g key="b">{box(v, x0, x1, 0, h, z, z + 16, { side: stoneDark, end: stone, top: roof })}</g>);
    n.push(<g key="r">{box(v, x0 + 1, x1 - 1, h, h + 2.2, z + 1, z + 15, { side: roof, end: roof, top: mixHex(roof, '#000000', 0.15) })}</g>);
    for (const y of [4.4, 8.6, 12.8, h - 0.7]) if (y < h) n.push(<g key={`c${y}`}>{box(v, x0 - 0.3, x1 + 0.3, y, y + 0.45, z - 0.35, z, { side: trim, end: trim, top: trim, bottom: stoneDark })}</g>);
    const cols = Math.round((x1 - x0) / 3.2);
    const dx = (x1 - x0) / cols;
    for (let r = 0; r < 4; r++) {
      const y = 1.2 + r * 4.2;
      if (y + 2.6 > h) break;
      for (let c = 0; c < cols; c++) {
        const x = x0 + c * dx + dx * 0.28;
        const on = random(`${seed}${r}.${c}`) < 0.3;
        n.push(face(v, [[x, y, z - 0.02], [x + dx * 0.44, y, z - 0.02], [x + dx * 0.44, y + 2.7, z - 0.02], [x, y + 2.7, z - 0.02]], on ? LIT[(r + c) % 4] : DARK));
      }
    }
    for (let c = 1; c < cols; c += 2) {
      const x = x0 + c * dx - 0.3;
      n.push(<g key={`p${c}`}>{box(v, x, x + 0.6, 0, h - 0.8, z - 0.5, z, { side: stoneDark, end: trim })}</g>);
    }
    // The pediment over the middle.
    const m = (x0 + x1) / 2;
    n.push(face(v, [[m - 9, h + 0.4, z - 0.4], [m + 9, h + 0.4, z - 0.4], [m, h + 4, z - 0.4]], trim));
    out.push({ z: depth(v, at), node: <g key={k()}>{n}</g> });
  };
  hall(-13, 12, 23, 18, 'hall', '#8A3A34');
  hall(-44, -15, 22, 13, 'old', '#A5442E');
  return out;
};

type Shop = { name: string; x: number; w: number; col: string; sign: string; ink: string };
const SHOPS: Shop[] = [
  { name: 'PISONET', x: -13, w: 5, col: '#3C5A78', sign: '#FFD84A', ink: '#20142A' },
  { name: "PARES NI ALING", x: -7.6, w: 6.4, col: '#7C3430', sign: '#F2593A', ink: '#FFF3DC' },
  { name: 'ELECTRONICS', x: -0.8, w: 6, col: '#2C5A58', sign: '#46D8C2', ink: '#10202A' },
  { name: 'SARI-SARI', x: 5.6, w: 6, col: '#6A5A28', sign: '#F4C24A', ink: '#2A1810' },
];
/** The ground floor of the hall's row: LORE.md's "selective shop light". */
export const Shopfronts = (v: View, f: number): Item[] => {
  const z = 22.6;
  return SHOPS.map((s): Item | null => {
    const at: V3 = [s.x + s.w / 2, 2, z];
    if (depth(v, at) < 1 || v.z > z) return null;
    const flick = s.name === 'PISONET' && Math.sin(f * 0.21) > 0.92 ? 0.55 : 1;
    const n: React.ReactNode[] = [];
    n.push(<g key="f">{box(v, s.x, s.x + s.w, 0, 3.1, z - 0.6, z, { side: s.col, end: s.col, top: s.col })}</g>);
    // Lit interior, awning, the sign.
    n.push(face(v, [[s.x + 0.4, 0.3, z - 0.62], [s.x + s.w - 0.4, 0.3, z - 0.62], [s.x + s.w - 0.4, 2.1, z - 0.62], [s.x + 0.4, 2.1, z - 0.62]], '#FFD994'));
    n.push(face(v, [[s.x, 2.2, z - 1.3], [s.x + s.w, 2.2, z - 1.3], [s.x + s.w, 2.6, z - 0.6], [s.x, 2.6, z - 0.6]], mixHex(s.col, '#FFFFFF', 0.25)));
    n.push(onPlane(v, [s.x + 0.1, 3.75, z - 0.66], [1, 0, 0], [0, -1, 0], s.w - 0.2, 1.05, <g opacity={flick}>
      <rect x={0} y={0} width={s.w - 0.2} height={1.05} fill={s.sign} />
      <text x={(s.w - 0.2) / 2} y={0.74} fontSize={0.66} fontFamily="Darumadrop One" textAnchor="middle" fill={s.ink}>{s.name}</text>
    </g>, k()));
    return { z: depth(v, at) - 0.5, node: <g key={k()}>{n}</g> };
  }).filter((x): x is Item => !!x);
};

// ------------------------------------------------------------------------------------ the street

const carItem = (v: View, x: number, z: number, body: string, along: boolean): Item | null => {
  const at: V3 = [x, 1, z];
  if (depth(v, at) < 1) return null;
  const [lx, lz] = along ? [2.2, 1] : [1, 2.2];
  return { z: depth(v, at), node: <g key={k()}>
    {box(v, x - lx, x + lx, 0.3, 1.05, z - lz, z + lz, { side: body, end: mixHex(body, '#000000', 0.2), top: mixHex(body, '#FFFFFF', 0.12) })}
    {box(v, x - lx * 0.62, x + lx * 0.62, 1.05, 1.6, z - lz * 0.86, z + lz * 0.86, { side: '#2A3440', end: '#2A3440', top: mixHex(body, '#FFFFFF', 0.1) })}
  </g> };
};

/** A jeepney: long, loud, chrome. The silver is the map's; the stripes are anyone's. */
const jeepney = (v: View, x: number, z: number): Item | null => {
  const at: V3 = [x, 1.2, z];
  if (depth(v, at) < 1) return null;
  const n: React.ReactNode[] = [];
  n.push(<g key="b">{box(v, x - 1.05, x + 1.05, 0.45, 1.5, z - 3.6, z + 2.6, { side: '#AEB4BC', end: '#8C929C', top: '#C6CCD4' })}</g>);
  n.push(<g key="h">{box(v, x - 1.05, x + 1.05, 0.45, 1.2, z - 4.8, z - 3.6, { side: '#C33A3A', end: '#E2CF4A', top: '#D85050' })}</g>);
  n.push(<g key="r">{box(v, x - 1.15, x + 1.15, 2.2, 2.42, z - 3.7, z + 2.8, { side: '#D8473C', end: '#E2CF4A', top: '#E05A4A', bottom: '#3A2E38' })}</g>);
  for (const sx of [-1, 1]) {
    const fx = x + sx * 1.06;
    n.push(face(v, [[fx, 0.85, z - 3.6], [fx, 1.05, z - 3.6], [fx, 1.05, z + 2.6], [fx, 0.85, z + 2.6]], '#D8473C'));
    n.push(face(v, [[fx, 1.05, z - 3.6], [fx, 1.2, z - 3.6], [fx, 1.2, z + 2.6], [fx, 1.05, z + 2.6]], '#E8B93A'));
    for (let i = 0; i < 5; i++) n.push(<g key={`w${sx}${i}`}>{box(v, x + sx * 1.0, x + sx * 1.08, 1.5, 2.2, z - 3.5 + i * 1.5, z - 3.42 + i * 1.5, { side: '#6A6E78', end: '#6A6E78' })}</g>);
    n.push(face(v, [[fx, 1.55, z - 3.5], [fx, 2.15, z - 3.5], [fx, 2.15, z + 2.5], [fx, 1.55, z + 2.5]], '#FFD994', 0.8));
  }
  return { z: depth(v, at), node: <g key={k()}>{n}</g> };
};

const blockTree = (v: View, x: number, z: number, h: number): Item | null => {
  const at: V3 = [x, h / 2, z];
  if (depth(v, at) < 1) return null;
  const lite = '#5E8A4E';
  const dark = '#2E5244';
  const n: React.ReactNode[] = [<g key="t">{box(v, x - 0.22, x + 0.22, 0, h * 0.6, z - 0.22, z + 0.22, { side: '#4A3024', end: '#3A2620' })}</g>];
  ([[0, 0.78, 0, 0.3], [-0.2, 0.68, 0.14, 0.2], [0.2, 0.7, -0.14, 0.2], [0, 0.95, 0, 0.18]] as const).forEach(([dx, y, dz, r], i) => {
    const R = r * h;
    n.push(<g key={i}>{box(v, x + dx * h - R, x + dx * h + R, y * h - R * 0.7, y * h + R * 0.7, z + dz * h - R, z + dz * h + R, { top: lite, side: mixHex(lite, dark, 0.55), end: dark, bottom: dark })}</g>);
  });
  return { z: depth(v, at), node: <g key={k()}>{n}</g> };
};

/** A royal palm: the poster's frontage. */
const palm = (v: View, x: number, z: number, h: number): Item | null => {
  const at: V3 = [x, h / 2, z];
  if (depth(v, at) < 1) return null;
  const n: React.ReactNode[] = [<g key="t">{box(v, x - 0.16, x + 0.16, 0, h, z - 0.16, z + 0.16, { side: '#6E5C50', end: '#5A4A44' })}</g>];
  for (let i = 0; i < 7; i++) {
    const a = (i / 7) * 6.283;
    const ex = x + Math.cos(a) * 2.2;
    const ez = z + Math.sin(a) * 2.2;
    n.push(face(v, [[x, h + 0.1, z], [x + Math.cos(a) * 1.2 - Math.sin(a) * 0.4, h + 0.5, z + Math.sin(a) * 1.2 + Math.cos(a) * 0.4], [ex, h - 0.8, ez], [x + Math.cos(a) * 1.2 + Math.sin(a) * 0.4, h + 0.5, z + Math.sin(a) * 1.2 - Math.cos(a) * 0.4]], i % 2 ? '#3E7A52' : '#2E5F48'));
  }
  return { z: depth(v, at), node: <g key={k()}>{n}</g> };
};

const parol = (v: View, x: number, y: number, z: number, f: number, seed: string): Item | null => {
  if (depth(v, [x, y, z]) < 1) return null;
  const [sx, sy] = project(v, [x, y, z]);
  const s = scaleAt(v, [x, y, z]) * 0.42;
  const pts = Array.from({ length: 10 }, (_, i) => {
    const r = i % 2 ? s * 0.42 : s;
    const a = (i / 10) * 6.283 - 1.57;
    return `${i ? 'L' : 'M'}${sx + Math.cos(a) * r} ${sy + Math.sin(a) * r}`;
  }).join('') + 'Z';
  const on = 0.75 + 0.25 * idleSin(f, 12, x);
  return { z: depth(v, [x, y, z]) - 0.2, node: <g key={k()}>
    <circle cx={sx} cy={sy} r={s * 1.9} fill="#FFB25A" opacity={0.22 * on} filter="url(#glow)" />
    <path d={pts} fill={seed === 'r' ? '#E0424A' : '#46B86A'} stroke="#FFE9B0" strokeWidth={Math.max(1, s * 0.08)} />
  </g> };
};

const cart = (v: View, x: number, z: number): Item | null => {
  if (depth(v, [x, 1, z]) < 1) return null;
  const n: React.ReactNode[] = [];
  n.push(<g key="c">{box(v, x - 0.7, x + 0.7, 0.3, 1.1, z - 0.4, z + 0.4, { side: '#E9C24C', end: '#D4A83A', top: '#F2DE9A' })}</g>);
  n.push(<g key="p">{box(v, x - 0.04, x + 0.04, 1.1, 2.4, z - 0.04, z + 0.04, { side: '#3A3440', end: '#3A3440' })}</g>);
  const R = 1.2;
  for (let i = 0; i < 8; i++) {
    const a = (i / 8) * 6.283;
    const b = ((i + 1) / 8) * 6.283;
    n.push(face(v, [[x, 2.7, z], [x + Math.cos(a) * R, 2.2, z + Math.sin(a) * R], [x + Math.cos(b) * R, 2.2, z + Math.sin(b) * R]], i % 2 ? '#3FA06A' : '#E8C040'));
  }
  return { z: depth(v, [x, 1, z]), node: <g key={k()}>{n}</g> };
};

const smallBox = (v: View, x: number, z: number, w: number, h: number, d: number, col: string, y = 0): Item | null => {
  if (depth(v, [x, h / 2, z]) < 0.8) return null;
  return { z: depth(v, [x, h / 2, z]), node: <g key={k()}>{box(v, x - w, x + w, y, y + h, z - d, z + d, { side: mixHex(col, '#1B1030', 0.3), end: col, top: mixHex(col, '#FFFFFF', 0.15) })}</g> };
};

/** Everything that sits on the street and round the lot. */
export const Clutter = (v: View, f: number): Item[] => {
  const c: (Item | null)[] = [];
  c.push(jeepney(v, 11.8, 2), jeepney(v, 11.8, -24));
  c.push(carItem(v, -8, 18.8, '#B83A3A', true), carItem(v, 0, 18.8, '#D8D8D0', true), carItem(v, 8, 14.4, '#3A5A8A', true), carItem(v, 17, -4, '#C8B24A', false), carItem(v, 12, 22, '#5A7A6A', false), carItem(v, -18, 14.5, '#D8D8D0', true));
  // Moving traffic along the far road, periodic on the idle.
  const lane = ((ph(f) / 720) * 120) % 120;
  c.push(carItem(v, -60 + lane, 14.4, '#E8B93A', true), carItem(v, 60 - ((lane + 60) % 120), 18.8, '#7A4A8A', true));
  for (const [x, z, h] of [[-20, 10, 8], [-22, -6, 7], [-17, -14, 9], [-14, 13, 6.5], [12, 24, 8], [20, 11, 7], [20, -12, 8]] as const) c.push(blockTree(v, x, z, h));
  for (const [x, z, h] of [[-16, 20, 10], [-11.5, 20, 9], [-27, 19, 11]] as const) c.push(palm(v, x, z, h));
  // Planters at the piers, barriers, the cart, the benches, crates and a plastic chair.
  for (const z of [-42, -12, 18, 48]) c.push(smallBox(v, 15, z, 2.6, 0.5, 2.6, '#4C5A48'));
  for (const z of [-10, -7, -4, 3, 6, 9]) c.push(smallBox(v, 10.2, z, 0.1, 0.8, 1.2, '#D9A92E'));
  c.push(cart(v, -9.4, 9.4));
  c.push(smallBox(v, -10.4, -2, 0.35, 0.42, 1.3, '#7C5A3A'), smallBox(v, -10.2, -8, 0.3, 0.55, 0.3, '#B03A4A'), smallBox(v, -9.6, 7, 0.32, 0.5, 0.32, '#3F8A5A'), smallBox(v, -9.6, 7, 0.28, 0.4, 0.28, '#C25A3A', 0.5), smallBox(v, 9.1, 10.3, 0.35, 0.75, 0.35, '#4A5A6A'));
  // Parols on the poles.
  c.push(parol(v, 9.4, 4.6, -6, f, 'g'), parol(v, 9.4, 4.6, 9, f, 'r'), parol(v, -3.6, 4.8, 2.2, f, 'r'), parol(v, -10, 5, 11, f, 'g'), parol(v, 4, 5.2, 11.5, f, 'g'));
  return c.filter((x): x is Item => x !== null);
};

/** Her moon, and the stars the city leaves. CHARACTER_ORIGINS.md: the moon a serpent can swallow. */
export const Heavens: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const stars: React.ReactNode[] = [];
  for (let i = 0; i < 70; i++) {
    const p = projectDir(v, random(`sa${i}`) * 360 - 180, 12 + 62 * random(`se${i}`));
    if (!p) continue;
    const tw = 0.45 + 0.4 * Math.abs(Math.sin((f + INTRO) * 0.02 * (1 + (i % 3)) + i));
    stars.push(<circle key={i} cx={p[0]} cy={p[1]} r={1 + 1.6 * random(`sr${i}`)} fill="#FFEFD8" opacity={tw} />);
  }
  const m = projectDir(v, -30, 27);
  const R = v.f * 0.075;
  return <g>
    {stars}
    {m && <g>
      <circle cx={m[0]} cy={m[1]} r={R * 2.3} fill="#F2A8D8" opacity={0.2} filter="url(#glow)" />
      <circle cx={m[0]} cy={m[1]} r={R} fill="#FBE7C8" />
      <circle cx={m[0] - R * 0.3} cy={m[1] - R * 0.2} r={R * 0.2} fill="#E6C9A8" />
      <circle cx={m[0] + R * 0.35} cy={m[1] + R * 0.28} r={R * 0.28} fill="#E6C9A8" />
      <circle cx={m[0] + R * 0.1} cy={m[1] - R * 0.5} r={R * 0.12} fill="#E6C9A8" />
      {/* The bite out of it: the serpent is at it. */}
      <circle cx={m[0] + R * 0.8} cy={m[1] - R * 0.45} r={R * 0.72} fill="#3A1A44" />
    </g>}
  </g>;
};

/** Pavement, the road's paint and the marks of use: so no floor is a flat colour. */
export const FloorDetail: React.FC<{ v: View }> = ({ v }) => {
  const rect = (x0: number, x1: number, z0: number, z1: number, fill: string, op = 1) => face(v, [[x0, 0, z0], [x1, 0, z0], [x1, 0, z1], [x0, 0, z1]], fill, op);
  const n: React.ReactNode[] = [];
  n.push(rect(-90, 90, 11.5, 12.6, '#6A4040'));
  n.push(rect(-90, 90, 21, 22.6, '#7A6A70'));
  n.push(rect(-90, -11, -90, 11.5, '#4A4050'));
  n.push(rect(-90, 9.5, -90, -13, '#4A4050'));
  for (let z = -80; z < 90; z += 4.4) n.push(rect(9.6, 10, z, z + 2.2, '#D9A92E'), rect(9.6, 10, z + 2.2, z + 4.4, '#1E1A22'));
  for (let z = -84; z < 90; z += 8) n.push(rect(21.2, 21.5, z, z + 3.4, '#C9BEAE'), rect(16, 16.3, z, z + 3.4, '#C9BEAE'));
  for (let i = 0; i < 9; i++) n.push(rect(10.4 + i * 1.5, 11.2 + i * 1.5, 12.2, 15.6, '#CFC6B8'));
  for (let i = 0; i < 16; i++) {
    const x = -10 + 20 * random(`st${i}`);
    const z = -12 + 23 * random(`sz${i}`);
    const r = 0.5 + 1.8 * random(`sr${i}`);
    const pts: V3[] = [];
    for (let a = 0; a < 9; a++) pts.push([x + Math.cos(a * 0.7) * r, 0.004, z + Math.sin(a * 0.7) * r * 0.6]);
    n.push(face(v, pts, i % 3 ? '#3E3448' : '#6A5C6A', 0.28));
  }
  // The tiled lines of the lot.
  for (let x = -10; x <= 10; x += 2) n.push(rect(x, x + 0.03, -13, 11.5, '#463A4E', 0.5));
  for (let z = -12; z <= 11; z += 2) n.push(rect(-11, 10.5, z, z + 0.03, '#463A4E', 0.5));
  return <g>{n}</g>;
};
