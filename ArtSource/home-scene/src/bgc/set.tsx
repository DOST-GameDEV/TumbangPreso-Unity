import React from 'react';
import { random } from 'remotion';
import { kf, inQuad, outQuad } from '../lib/kf';
import { easeInOut, env, idleSin, INTRO, lerp, ph } from './time';
import { B } from './beats';
import { depth, horizonY, line, poly, project, scaleAt, V3, View } from '../phaister/view';
import { box, Item, lit, mixHex, onPlane } from '../phaister/set';
import { Paint } from '../phaister/lettering';

/*
 * BGC, drawn. The shipped map (scene id `Kanto`, shown as BGC, SceneFlow.MapRegistry) laid out on its
 * own numbers from Assets/TumbangPreso/Art/Kanto/kanto_layout.json and docs/KANTO_DESIGN_GUIDE.md:
 * the 20 m paved court, the 14 m box, lawns at the corners, the ring road from 17 to 27, buildings from
 * 30 facing the park, signals at (16.4, 16.4), lamps at (3.4, 13), the same shop names.
 *
 * ⚠️ DRAWN, NOT THE MAP'S MESHES (🧑 2026-09-24: "keep the stylized drawing of it, dont do 3d assets").
 * The cast, the can and the slipper are the game's models.
 *
 * ⚠️ THE HOUR. A late warm afternoon, so the sky sits with HOME's maroon panels and the map's teal
 * glass is the only cool note, pushed to the green side as the front-end colour rule asks.
 */

const C = {
  asphalt: '#4F4A58',
  asphaltLight: '#5A5462',
  yellow: '#E3B62F',
  paint: '#EEE7D8',
  pave: '#B9AAA0',
  paveLine: '#A39488',
  kerb: '#D8CCBC',
  court: '#D9BE9A',
  courtLine: '#C9AC88',
  chalk: '#FBF4E2',
  grass: '#6DB43A',
  grassDark: '#5AA032',
  hedge: '#52892F',
  hedgeTop: '#8DBB3E',
  planter: '#CDB89C',
  wood: '#7C4A2A',
  metal: '#2F4A45',
  lampGlass: '#FFE9AE',
  glass: '#3E8778',
  glassLit: '#F8D58A',
  glassDim: '#2E6F63',
  pole: '#5E4632',
  wire: '#2A2024',
  signal: '#26302C',
  red: '#F0432C',
  amber: '#F7B32B',
  green: '#3BE07A',
};

let KEY = 0;
const k = () => `b${KEY++}`;
const face = (v: View, pts: V3[], fill: string, op = 1) => {
  const d = poly(v, pts);
  return d ? <path key={k()} d={d} fill={fill} opacity={op} /> : null;
};

// ------------------------------------------------------------------------------------ sky

const SKY = ['#C86A5C', '#D5786A', '#E18A72', '#EA9C7C', '#F1AF88', '#F6C198', '#F9D3AC', '#FBE2C0'];

export const Sky: React.FC<{ v: View }> = ({ v }) => {
  const hz = horizonY(v);
  const bands = SKY.map((c, i) => {
    const e0 = (7 - i) * 6.5;
    const y = hz - v.f * Math.tan((e0 * Math.PI) / 180);
    return <rect key={i} x={-300} y={Math.min(y, 1400) - (i === 0 ? 3000 : 0)} width={2520} height={3400} fill={c} />;
  });
  return <g>{bands}</g>;
};

const CLOUDS: [number, number, number, number][] = [[-12, 30, 7, 1.0], [8, 34, 8.5, 0.8], [24, 26, 6, 1.1], [-30, 22, 5, 0.7], [44, 31, 7, 0.9], [-52, 28, 8, 0.8]];

export const Clouds: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const R = 300;
  const toP = (az: number, el: number): V3 => {
    const a = (az * Math.PI) / 180;
    const e = (el * Math.PI) / 180;
    return [v.x + Math.sin(a) * Math.cos(e) * R, v.y + Math.sin(e) * R, v.z + Math.cos(a) * Math.cos(e) * R];
  };
  return (
    <g>
      {CLOUDS.map(([az, el, w, a], i) => {
        const az2 = az + 1.4 * idleSin(f, 1, i);
        const p = toP(az2, el);
        if (depth(v, p) < 10) return null;
        const [x, y] = project(v, p);
        const s = (v.f / R) * w * 9;
        return (
          <g key={i} opacity={0.8 * a}>
            <ellipse cx={x} cy={y} rx={s * 1.3} ry={s * 0.22} fill="#FFE9D2" />
            <ellipse cx={x - s * 0.45} cy={y - s * 0.17} rx={s * 0.55} ry={s * 0.24} fill="#FFF3E2" />
            <ellipse cx={x + s * 0.3} cy={y - s * 0.22} rx={s * 0.65} ry={s * 0.3} fill="#FFF3E2" />
            <ellipse cx={x} cy={y + s * 0.08} rx={s * 1.1} ry={s * 0.1} fill="#F2B9A0" />
          </g>
        );
      })}
    </g>
  );
};

/** The downtown: curtain-wall towers past the street blocks, hazed. */
const TOWERS: [number, number, number, number, string][] = [
  [-92, 124, 16, 92, '#7C9A8E'], [-52, 138, 14, 118, '#6E8F86'], [-10, 150, 18, 86, '#8AA79A'], [30, 134, 14, 132, '#6B8C82'],
  [72, 142, 16, 104, '#7A988C'], [110, 118, 14, 88, '#8AA79A'], [-130, 70, 16, 74, '#8AA79A'], [136, 62, 16, 96, '#7A988C'],
  [-80, -40, 14, 96, '#7A988C'], [82, -42, 14, 112, '#6E8F86'], [-40, -92, 16, 108, '#7A988C'], [12, -96, 18, 128, '#6E8F86'], [52, -84, 14, 88, '#8AA79A'],
];

export const FarCity = (v: View): Item[] =>
  TOWERS.map(([x, z, w, h, col], i) => {
    const hazed = mixHex(col, '#E9A98E', 0.38);
    const nodes: React.ReactNode[] = [...box(v, x - w / 2, x + w / 2, 0, h, z - w / 2, z + w / 2, { side: hazed, end: hazed, top: hazed })];
    const front = v.z < z ? z - w / 2 : z + w / 2;
    for (let y = 4; y < h - 4; y += 4.5) nodes.push(face(v, [[x - w / 2, y, front], [x + w / 2, y, front], [x + w / 2, y + 1.6, front], [x - w / 2, y + 1.6, front]], mixHex(hazed, '#4E6E66', 0.3)));
    for (let u = 1; u < 4; u++) { const mx = x - w / 2 + (w * u) / 4; nodes.push(face(v, [[mx - 0.3, 0, front], [mx + 0.3, 0, front], [mx + 0.3, h, front], [mx - 0.3, h, front]], hazed)); }
    nodes.push(...box(v, x - w * 0.28, x + w * 0.28, h, h + 7 + (i % 3) * 5, z - w * 0.28, z + w * 0.28, { side: hazed, end: hazed, top: hazed }));
    return { z: depth(v, [x, h / 2, z]) + 400, node: <g key={`t${i}`}>{nodes}</g> };
  });

// ------------------------------------------------------------------------------------ ground

const quad = (v: View, x0: number, x1: number, z0: number, z1: number, col: string, y = 0) =>
  face(v, [[x0, y, z0], [x1, y, z0], [x1, y, z1], [x0, y, z1]], col);

export const Ground: React.FC<{ v: View; tally: number }> = ({ v, tally }) => {
  const n: React.ReactNode[] = [];
  n.push(quad(v, -140, 140, -140, 140, C.asphalt));
  // Sidewalks outside the ring road, by the buildings.
  for (const [a, b] of [[27, 30], [-30, -27]]) {
    n.push(quad(v, -140, 140, a, b, C.pave, 0.05));
    n.push(quad(v, a, b, -140, 140, C.pave, 0.05));
  }
  // The park block: kerb, sidewalk, lawns, paved court.
  n.push(quad(v, -17, 17, -17, 17, C.kerb, 0.08));
  n.push(quad(v, -16.8, 16.8, -16.8, 16.8, C.pave, 0.1));
  for (let g = -16; g <= 16; g += 2) {
    const a: V3 = [g, 0.1, -16.8];
    const b: V3 = [g, 0.1, 16.8];
    const c: V3 = [-16.8, 0.1, g];
    const d: V3 = [16.8, 0.1, g];
    n.push(<path key={k()} d={line(v, [a, b]) + line(v, [c, d])} stroke={C.paveLine} strokeWidth={1} fill="none" opacity={0.5} />);
  }
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) {
    const x0 = sx > 0 ? 3 : -13.7;
    const x1 = sx > 0 ? 13.7 : -3;
    const z0 = sz > 0 ? 3 : -13.7;
    const z1 = sz > 0 ? 13.7 : -3;
    n.push(quad(v, x0, x1, z0, z1, C.grass, 0.11));
    n.push(quad(v, x0 + 1.2, x1 - 1.2, z0 + 1.2, z1 - 1.2, C.grassDark, 0.115));
  }
  n.push(quad(v, -10, 10, -10, 10, C.court, 0.12));
  for (let g = -10; g <= 10; g += 1) {
    const a: V3 = [g, 0.12, -10];
    const b: V3 = [g, 0.12, 10];
    const c: V3 = [-10, 0.12, g];
    const d: V3 = [10, 0.12, g];
    n.push(<path key={k()} d={line(v, [a, b]) + line(v, [c, d])} stroke={C.courtLine} strokeWidth={0.9} fill="none" opacity={0.55} />);
  }
  // A big soft patch or two, the map's gouache.
  n.push(face(v, [[-6, 0.125, 1], [-1, 0.125, 4], [3, 0.125, 2], [1, 0.125, -3], [-5, 0.125, -3]], '#CFAF88', 0.45));
  // Chalk: the 14 m box and the two throwing lines (`gameplay` box 7, throw 8).
  const chalk = (a: V3, b: V3, w = 3.2) => <path key={k()} d={line(v, [a, b])} stroke={C.chalk} strokeWidth={w} fill="none" strokeLinecap="round" opacity={0.92} />;
  const y = 0.13;
  n.push(chalk([-7, y, -7], [7, y, -7]), chalk([7, y, -7], [7, y, 7]), chalk([7, y, 7], [-7, y, 7]), chalk([-7, y, 7], [-7, y, -7]));
  n.push(chalk([-7, y, -8], [7, y, -8], 4.2), chalk([-7, y, 8], [7, y, 8], 4.2));
  // The can's ring.
  const ring: V3[] = [];
  for (let i = 0; i <= 28; i++) ring.push([0.62 * Math.cos((i / 28) * 2 * Math.PI), y, 0.62 * Math.sin((i / 28) * 2 * Math.PI)]);
  n.push(<path key={k()} d={line(v, ring)} stroke={C.chalk} strokeWidth={2.6} fill="none" opacity={0.9} />);
  // Road paint on the ring road.
  for (const [zc] of [[22], [-22]]) for (let x = -96; x < 96; x += 6) n.push(quad(v, x, x + 3, zc - 0.08, zc + 0.08, C.yellow, 0.02));
  for (const [xc] of [[22], [-22]]) for (let z = -96; z < 96; z += 6) n.push(quad(v, xc - 0.08, xc + 0.08, z, z + 3, C.yellow, 0.02));
  // Zebra crossings at the corners, as on the map card.
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) {
    for (let i = 0; i < 7; i++) {
      const a = 18 + i * 1.25;
      n.push(quad(v, sx * 14.6 - 1.2, sx * 14.6 + 1.2, sz * a, sz * (a + 0.7), C.paint, 0.02));
      n.push(quad(v, sx * a, sx * (a + 0.7), sz * 14.6 - 1.2, sz * 14.6 + 1.2, C.paint, 0.02));
    }
  }
  // Rago keeps the score: one chalk stroke beside the can, drawn as his arm comes up, and it stays.
  if (tally > 0.01) {
    n.push(<path key={k()} d={line(v, [[1.05, y, -1.75], [1.15 + 0.1 * tally, y, -1.75 + 0.95 * tally]])} stroke={C.chalk} strokeWidth={5} strokeLinecap="round" fill="none" />);
  }
  return <g>{n}</g>;
};

// ------------------------------------------------------------------------------------ buildings

type Facade = {
  side: 'N' | 'S' | 'E' | 'W';
  at: number;
  w: number;
  floors: number;
  paint: string;
  trim: string;
  frame: string;
  brick?: boolean;
  awning?: [string, string];
  sign?: { text: string; board: string; ink: string; line?: string };
  roof?: string;
  kind?: 'grid' | 'ribbon' | 'tall';
};

/** Every building facing the park, at its own place in kanto_layout.json, with its own shop name. */
const FACADES: Facade[] = [
  { side: 'N', at: -14.6, w: 9.4, floors: 5, paint: '#8A9E70', trim: '#E8DFC8', frame: '#2A3A34', kind: 'ribbon', sign: { text: "DORY'S SARI-SARI", board: '#C33A2E', ink: '#FFF4D8' } },
  { side: 'N', at: -4.5, w: 9.2, floors: 3, paint: '#D49A8E', trim: '#F2E6D2', frame: '#F2E6D2', awning: ['#C9413A', '#F7EEDC'], sign: { text: "BERTO'S BARBERSHOP", board: '#F2E6D2', ink: '#2A3A34' } },
  { side: 'N', at: 4.8, w: 9.2, floors: 5, paint: '#A9A268', trim: '#E8DFC8', frame: '#2A3A34', kind: 'ribbon', sign: { text: 'KAPEHAN SA KANTO', board: '#3B3226', ink: '#F6C04B' } },
  { side: 'N', at: 14.6, w: 9.4, floors: 6, paint: '#A5412F', trim: '#E9DBC4', frame: '#6E1F1A', brick: true, kind: 'grid', roof: '#3E3A3A', sign: { text: 'RJV HARDWARE', board: '#F0B429', ink: '#3A2A12' } },
  { side: 'N', at: 30, w: 15, floors: 4, paint: '#D7C49A', trim: '#6E4A32', frame: '#F2E6D2', awning: ['#3E7F6E', '#F7EEDC'] },
  { side: 'N', at: -30, w: 16, floors: 9, paint: '#74907F', trim: '#D9CFB8', frame: '#3A4A44', kind: 'tall' },
  { side: 'E', at: -14.6, w: 9.4, floors: 5, paint: '#BDB8A8', trim: '#EFE6D2', frame: '#EFE6D2', kind: 'grid', sign: { text: 'LUGAWAN NI LOLA ISING', board: '#E2E3C2', ink: '#7A2A1E' } },
  { side: 'E', at: -4.8, w: 9.2, floors: 4, paint: '#8F5B3C', trim: '#E9DBC4', frame: '#3A2A20', brick: true, kind: 'grid', sign: { text: 'MILK TEA NI BES', board: '#3E7F6E', ink: '#FFF4D8' } },
  { side: 'E', at: 4.5, w: 9.2, floors: 7, paint: '#B9B5A2', trim: '#E2DAC4', frame: '#2A3A34', kind: 'ribbon', sign: { text: 'KUYA JM CELLSHOP', board: '#F2E6D2', ink: '#A02A22' } },
  { side: 'E', at: 14.6, w: 9.4, floors: 6, paint: '#A5412F', trim: '#E9DBC4', frame: '#6E1F1A', brick: true, kind: 'grid' },
  { side: 'W', at: -4.5, w: 9.2, floors: 4, paint: '#E0C686', trim: '#F4EAD4', frame: '#F4EAD4', awning: ['#C9413A', '#F7EEDC'], sign: { text: "PURING'S KAINAN", board: '#2F6F5E', ink: '#FFF4D8' } },
  { side: 'W', at: 4.8, w: 9.2, floors: 3, paint: '#8F5B3C', trim: '#E9DBC4', frame: '#3A2A20', brick: true, sign: { text: 'NANAY LYDIA KAKANIN', board: '#F2E6D2', ink: '#7A2A1E' } },
  { side: 'W', at: 14.6, w: 9.4, floors: 5, paint: '#B9B5A2', trim: '#E2DAC4', frame: '#2A3A34', kind: 'ribbon' },
  { side: 'W', at: -14.6, w: 9.4, floors: 6, paint: '#8A9E70', trim: '#E8DFC8', frame: '#2A3A34', kind: 'grid' },
  { side: 'S', at: 4.5, w: 9.2, floors: 5, paint: '#A5412F', trim: '#E9DBC4', frame: '#6E1F1A', brick: true, kind: 'grid', sign: { text: 'DOC REY BOTIKA', board: '#F2E6D2', ink: '#2F6F5E' } },
  { side: 'S', at: -4.8, w: 9.2, floors: 6, paint: '#C4A87C', trim: '#EFE6D2', frame: '#3A2A20', kind: 'ribbon', sign: { text: 'LABAHAN NI ATE JOY', board: '#7A2A1E', ink: '#FFF4D8' } },
  { side: 'S', at: -14.6, w: 9.4, floors: 4, paint: '#8EBCA2', trim: '#F2E6D2', frame: '#F2E6D2', awning: ['#3E7F6E', '#F7EEDC'], sign: { text: 'PANADERIA NI MANG ERNING', board: '#FFF4D8', ink: '#7A2A1E' } },
  { side: 'S', at: 30, w: 15, floors: 6, paint: '#E0D4B8', trim: '#C9A23E', frame: '#3A3226', kind: 'tall' },
  { side: 'S', at: -30, w: 15, floors: 7, paint: '#A5412F', trim: '#E9DBC4', frame: '#6E1F1A', brick: true, kind: 'grid', roof: '#3E3A3A' },
];

const FLOOR = 3.3;

/** A point on a facade: u along it, h up, out from the wall. */
const on = (b: Facade, u: number, h: number, out = 0): V3 => {
  const d = 30 + -out;
  if (b.side === 'N') return [b.at + u, h, d];
  if (b.side === 'S') return [b.at - u, h, -d];
  if (b.side === 'E') return [d, h, b.at - u];
  return [-d, h, b.at + u];
};

const facadeOut = (b: Facade): V3 => (b.side === 'N' ? [0, 0, -1] : b.side === 'S' ? [0, 0, 1] : b.side === 'E' ? [-1, 0, 0] : [1, 0, 0]);

const building = (v: View, b: Facade) => {
  const n: React.ReactNode[] = [];
  const h = b.floors * FLOOR + 1.2;
  const hw = b.w / 2;
  const out = facadeOut(b);
  const wall = lit(b.paint, out);
  const trim = lit(b.trim, out);
  const pl = (u0: number, u1: number, h0: number, h1: number, col: string, o = 0) => face(v, [on(b, u0, h0, o), on(b, u1, h0, o), on(b, u1, h1, o), on(b, u0, h1, o)], col);
  // Body, so a corner seen from an angle has a side.
  const back = 11;
  const p0 = on(b, -hw, 0, -back);
  const p1 = on(b, hw, h, 0);
  n.push(...box(v, Math.min(p0[0], p1[0]), Math.max(p0[0], p1[0]), 0, h, Math.min(p0[2], p1[2]), Math.max(p0[2], p1[2]), { side: mixHex(b.paint, '#3E3238', 0.12), end: mixHex(b.paint, '#3E3238', 0.12), top: b.roof ?? '#5C5552' }));
  n.push(pl(-hw, hw, 0, h, wall));
  // Brick: wobbly courses, low contrast (KANTO guide § 3).
  if (b.brick) {
    for (let y = 4.2; y < h - 1; y += 0.75) {
      n.push(pl(-hw, hw, y, y + 0.1, lit(mixHex(b.paint, '#3A1A14', 0.2), out)));
    }
  }
  // Windows. Window system differs by kind: construction, not only colour (guide § 4).
  const glass = lit(C.glass, out);
  const cols = b.kind === 'grid' ? 4 : 3;
  for (let fl = 1; fl < b.floors; fl++) {
    const y0 = fl * FLOOR + 0.6;
    if (b.kind === 'ribbon') {
      n.push(pl(-hw + 0.5, hw - 0.5, y0 + 0.2, y0 + 1.9, lit(b.frame, out), 0.03));
      n.push(pl(-hw + 0.65, hw - 0.65, y0 + 0.32, y0 + 1.78, glass, 0.05));
      for (let i = 1; i < 5; i++) {
        const u = -hw + 0.5 + ((b.w - 1) * i) / 5;
        n.push(pl(u - 0.06, u + 0.06, y0 + 0.32, y0 + 1.78, lit(b.frame, out), 0.06));
      }
      n.push(pl(-hw, hw, y0 - 0.26, y0 - 0.06, trim, 0.1));
    } else {
      for (let i = 0; i < cols; i++) {
        const cw = b.w / cols;
        const uc = -hw + cw * (i + 0.5);
        const ww = cw * (b.kind === 'grid' ? 0.76 : 0.56);
        const wh = b.kind === 'tall' ? 2.5 : 2.0;
        n.push(pl(uc - ww / 2 - 0.12, uc + ww / 2 + 0.12, y0 - 0.1, y0 + wh + 0.1, lit(b.frame, out), 0.03));
        const on2 = random(`w${b.side}${b.at}${fl}${i}`) > 0.82;
        n.push(pl(uc - ww / 2, uc + ww / 2, y0, y0 + wh, on2 ? lit(C.glassLit, out) : random(`d${b.side}${b.at}${fl}${i}`) > 0.6 ? lit(C.glassDim, out) : glass, 0.05));
        n.push(pl(uc - 0.05, uc + 0.05, y0, y0 + wh, lit(b.frame, out), 0.06));
        n.push(pl(uc - ww / 2 - 0.25, uc + ww / 2 + 0.25, y0 - 0.25, y0 - 0.08, trim, 0.14));
        if (random(`ac${b.side}${b.at}${fl}${i}`) > 0.72) n.push(pl(uc + ww / 2 - 0.7, uc + ww / 2 - 0.05, y0 - 0.9, y0 - 0.3, lit('#D9D2C2', out), 0.32));
      }
    }
  }
  // Cornice.
  n.push(pl(-hw - 0.15, hw + 0.15, h - 0.65, h, trim, 0.25));
  // Ground floor: shop glass, door, fascia, awning.
  n.push(pl(-hw, hw, 0, 3.3, lit(mixHex(b.trim, b.paint, 0.35), out), 0.02));
  n.push(pl(-hw + 0.5, hw - 0.5, 0.5, 2.5, lit(C.glassDim, out), 0.04));
  n.push(pl(-0.75, 0.75, 0.08, 2.5, lit('#2E2628', out), 0.05));
  if (b.awning) {
    const stripes = 12;
    for (let i = 0; i < stripes; i++) {
      const u0 = -hw + 0.2 + ((b.w - 0.4) * i) / stripes;
      const u1 = -hw + 0.2 + ((b.w - 0.4) * (i + 1)) / stripes;
      n.push(face(v, [on(b, u0, 3.0, 0.1), on(b, u1, 3.0, 0.1), on(b, u1, 2.45, 1.2), on(b, u0, 2.45, 1.2)], lit(b.awning[i % 2], [out[0], 0.7, out[2]])));
    }
  }
  if (b.sign) {
    n.push(pl(-hw + 0.4, hw - 0.4, 3.0, 4.15, lit(b.sign.board, out), 0.18));
    const o = on(b, -hw + 0.4, 4.15, 0.2);
    const u: V3 = b.side === 'N' ? [1, 0, 0] : b.side === 'S' ? [-1, 0, 0] : b.side === 'E' ? [0, 0, -1] : [0, 0, 1];
    n.push(onPlane(v, o, u, [0, -1, 0], b.w - 0.8, 1.15, (
      <Paint text={b.sign.text} x={(b.w - 0.8) / 2} y={0.82} h={0.56} fill={b.sign.ink} fit={b.w - 1.3} seed={b.sign.text} wobble={0.02} bounce={0.01} />
    )));
  }
  return <g key={`f${b.side}${b.at}`}>{n}</g>;
};

export const Buildings = (v: View): Item[] =>
  FACADES.map((b) => {
    const c = on(b, 0, 6, -5);
    return { z: depth(v, c) + 80, node: building(v, b) };
  }).filter((it) => it.z > 0);

// ------------------------------------------------------------------------------------ park furniture

/**
 * ⚠️ BLOCKS, LIKE THE CAST AND THE BUILDINGS. A drawn leaf ball read as a green blob on a stick (owner,
 * 2026-11-01) and, being flat, turned to nothing under the crane. Real boxes hold from every lens.
 */
const leafBlocks = (v: View, t: { x: number; z: number; lite: string; dark: string }, cubes: [number, number, number, number][], swayM: number) =>
  cubes
    .map(([dx, y, dz, r]) => ({ dx, y, dz, r, d: depth(v, [t.x + dx, y, t.z + dz]) }))
    .sort((a, b) => b.d - a.d)
    .flatMap(({ dx, y, dz, r }) => {
      const x = t.x + dx + swayM * (y / 8);
      const z = t.z + dz;
      return box(v, x - r, x + r, y - r * 0.8, y + r * 0.8, z - r, z + r, { top: t.lite, side: mixHex(t.lite, t.dark, 0.55), end: t.dark });
    });

/** The park's four trees, each its own kind, at the map's corners. */
const TREES: { x: number; z: number; h: number; clumps: [number, number, number][]; lite: string; dark: string; pine?: boolean }[] = [
  { x: -11.6, z: 11.6, h: 8.5, clumps: [], lite: '#6BA24A', dark: '#3E7A3C', pine: true },
  { x: 11.6, z: 11.6, h: 6.8, clumps: [[0, 6.6, 2.5], [1.8, 6.0, 1.7], [-1.5, 5.8, 1.6]], lite: '#B3CD3F', dark: '#7BA232' },
  { x: 11.6, z: -11.6, h: 6.8, clumps: [[-1.8, 6.2, 2.1], [0.7, 6.6, 2.4], [2.2, 5.8, 1.7]], lite: '#A9CB3B', dark: '#6E9C30' },
  { x: -11.6, z: -11.6, h: 7, clumps: [[-1.2, 6.6, 2.3], [1.2, 6.3, 2.2], [0, 5.5, 1.8]], lite: '#B3CD3F', dark: '#7BA232' },
];

const lampItem = (v: View, x: number, z: number, f: number): Item | null => {
  if (depth(v, [x, 2, z]) < 0.8) return null;
  const n: React.ReactNode[] = [];
  n.push(...box(v, x - 0.09, x + 0.09, 0.1, 3.05, z - 0.09, z + 0.09, { side: C.metal, end: C.metal }));
  n.push(...box(v, x - 0.17, x + 0.17, 0.1, 0.55, z - 0.17, z + 0.17, { side: C.metal, end: C.metal }));
  n.push(...box(v, x - 0.2, x + 0.2, 3.05, 3.55, z - 0.2, z + 0.2, { side: C.lampGlass, end: C.lampGlass }));
  n.push(...box(v, x - 0.26, x + 0.26, 3.55, 3.68, z - 0.26, z + 0.26, { side: C.metal, end: C.metal, top: C.metal }));
  void f;
  return { z: depth(v, [x, 2, z]), node: <g key={k()}>{n}</g> };
};

const FENCE_RUNS: [number, number, number, number][] = [
  [3.2, 13.7, 13.7, 13.7], [-13.7, 13.7, -3.2, 13.7], [3.2, -13.7, 13.7, -13.7], [-13.7, -13.7, -3.2, -13.7],
  [13.7, 3.2, 13.7, 13.7], [13.7, -13.7, 13.7, -3.2], [-13.7, 3.2, -13.7, 13.7], [-13.7, -13.7, -13.7, -3.2],
];

export const Park = (v: View, f: number): Item[] => {
  const out: Item[] = [];
  for (const t of TREES) {
    if (depth(v, [t.x, 3, t.z]) < 1) continue;
    const sway = 0.05 * idleSin(f, 3, t.x);
    const n: React.ReactNode[] = [];
    const trunk = t.pine ? '#5A3A22' : '#6B4226';
    n.push(<g key="tr">{box(v, t.x - 0.28, t.x + 0.28, 0.1, t.pine ? 2.2 : t.h - 2.4, t.z - 0.28, t.z + 0.28, { side: trunk, end: '#4A2E1A', top: trunk })}</g>);
    const cubes: [number, number, number, number][] = t.pine
      ? [[0, 2.5, 0, 2.1], [0, 4.1, 0, 1.6], [0, 5.5, 0, 1.15], [0, 6.7, 0, 0.75], [0, 7.6, 0, 0.42]]
      : t.clumps.flatMap(([dx, y, r], i): [number, number, number, number][] => [[dx, y, i - 1, r * 0.72], [dx * 0.6, y + r * 0.55, (i - 1) * 0.5 - 0.4, r * 0.48]]);
    n.push(<g key="leaf">{leafBlocks(v, t, cubes, sway)}</g>);
    out.push({ z: depth(v, [t.x, 3, t.z]), node: <g key={`tree${t.x}${t.z}`}>{n}</g> });
  }
  for (const [x, z] of [[3.4, 13], [-3.4, 13], [3.4, -13], [-3.4, -13], [13, 3.4], [-13, 3.4], [13, -3.4], [-13, -3.4]]) {
    const it = lampItem(v, x, z, f);
    if (it) out.push(it);
  }
  // Hedge beds in their planters.
  for (const [x, z, horiz] of [[11.6, 6, 0], [6, 11.6, 1], [-11.6, 6, 0], [-6, 11.6, 1], [11.6, -6, 0], [6, -11.6, 1], [-11.6, -6, 0], [-6, -11.6, 1]] as [number, number, number][]) {
    const hw = horiz ? 2 : 0.55;
    const hd = horiz ? 0.55 : 2;
    if (depth(v, [x, 0.5, z]) < 1) continue;
    const n = [...box(v, x - hw, x + hw, 0.1, 0.5, z - hd, z + hd, { side: C.planter, end: C.planter, top: C.hedge }), ...box(v, x - hw * 0.86, x + hw * 0.86, 0.5, 0.82, z - hd * 0.86, z + hd * 0.86, { side: C.hedge, end: C.hedge, top: C.hedgeTop })];
    out.push({ z: depth(v, [x, 0.5, z]), node: <g key={k()}>{n}</g> });
  }
  // Benches.
  for (const [x, z, a] of [[3, 10.8, 0], [-3, 10.8, 0], [3, -10.8, 0], [-3, -10.8, 0], [10.8, 3, 1], [10.8, -3, 1], [-10.8, 3, 1], [-10.8, -3, 1]] as [number, number, number][]) {
    if (depth(v, [x, 0.5, z]) < 1) continue;
    const hw = a ? 0.3 : 0.9;
    const hd = a ? 0.9 : 0.3;
    const n = [...box(v, x - hw, x + hw, 0.42, 0.52, z - hd, z + hd, { side: C.wood, end: C.wood, top: '#94603A' }), ...box(v, x - hw, x + hw, 0.1, 0.42, z - hd * 0.1, z + hd * 0.1, { side: C.metal, end: C.metal })];
    out.push({ z: depth(v, [x, 0.5, z]), node: <g key={k()}>{n}</g> });
  }
  // The low fence, post and rail.
  for (const [x0, z0, x1, z1] of FENCE_RUNS) {
    const mx = (x0 + x1) / 2;
    const mz = (z0 + z1) / 2;
    if (depth(v, [mx, 0.5, mz]) < 1) continue;
    const n: React.ReactNode[] = [<path key="r" d={line(v, [[x0, 0.85, z0], [x1, 0.85, z1]]) + line(v, [[x0, 0.3, z0], [x1, 0.3, z1]])} stroke={C.metal} strokeWidth={2.2} fill="none" />];
    const L = Math.hypot(x1 - x0, z1 - z0);
    for (let i = 0; i <= L * 3; i++) {
      const u = i / (L * 3);
      n.push(<path key={i} d={line(v, [[x0 + (x1 - x0) * u, 0.15, z0 + (z1 - z0) * u], [x0 + (x1 - x0) * u, 0.85, z0 + (z1 - z0) * u]])} stroke={C.metal} strokeWidth={1.1} fill="none" />);
    }
    out.push({ z: depth(v, [mx, 0.5, mz]) + 0.5, node: <g key={k()}>{n}</g> });
  }
  return out;
};

// ------------------------------------------------------------------------------------ the signal

/**
 * ⚠️ THE SIGNAL IS THE STORY'S CLOCK. Red until Zack gets his green. Past the landing it cycles on IDLE
 * and every lamp ramps (method § 4.1), and it comes back to green exactly at the seam.
 */
export const signalAt = (f: number) => {
  if (f < INTRO) {
    const red = kf(f, [[B.amber - 1, 1], [B.amber + 2, 0]]);
    const amber = env(f, B.amber - 1, B.amber + 2, B.green - 2, B.green + 1);
    const green = kf(f, [[B.green - 2, 0], [B.green + 1, 1]]);
    return { red, amber, green };
  }
  const i = ph(f);
  return {
    green: i < 360 ? 1 : i < 363 ? 1 - (i - 360) / 3 : i >= 705 ? kf(i, [[705, 0], [717, 0], [720, 1]]) : 0,
    amber: env(i, 360, 363, 397, 400),
    red: env(i, 397, 400, 714, 717),
  };
};

/** The two signals facing the lens, and the service wire feeding the right-hand one. */
const SIGNALS: { x: number; z: number; hx: number; hz: number }[] = [
  { x: 16.4, z: 16.4, hx: 15.0, hz: 16.4 },
  { x: -16.4, z: 16.4, hx: -15.0, hz: 16.4 },
];
export const SIGNAL_HEAD: V3 = [15.0, 5.3, 16.4];
/** The wire the current runs along in the first shot. */
export const FEED: [V3, V3] = [[8, 8.5, 27.8], [16.4, 6.3, 16.4]];

export const sagAt = (a: V3, b: V3, u: number, sag = 0.9): V3 => [a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u - sag * 4 * u * (1 - u), a[2] + (b[2] - a[2]) * u];

export const Signals = (v: View, f: number): Item[] => {
  const st = signalAt(f);
  return SIGNALS.map((s) => {
    const d0 = depth(v, [s.x, 3, s.z]);
    if (d0 < 1) return null as unknown as Item;
    const n: React.ReactNode[] = [];
    n.push(...box(v, s.x - 0.1, s.x + 0.1, 0.1, 6.3, s.z - 0.1, s.z + 0.1, { side: C.signal, end: C.signal, top: C.signal }));
    const dir = Math.sign(s.hx - s.x);
    n.push(<path key={k()} d={line(v, [[s.x, 5.95, s.z], [s.hx + dir * -0.2, 5.95, s.hz]])} stroke={lit(C.signal, [0, 0, -1])} strokeWidth={Math.max(2, 0.12 * scaleAt(v, [s.hx, 5.9, s.hz]))} strokeLinecap="round" fill="none" />);
    // The head, three lamps, each with its hood.
    const hx = s.hx;
    const hz = s.hz;
    n.push(...box(v, hx - 0.25, hx + 0.25, 4.55, 6.05, hz - 0.14, hz + 0.14, { side: '#1E2624', end: '#1E2624', top: '#1E2624', bottom: '#1E2624' }));
    const px = scaleAt(v, [hx, 5.3, hz]);
    const lamp = (y: number, col: string, on: number, key: string) => {
      const [x, py] = project(v, [hx, y, hz - 0.15]);
      const dim = mixHex(col, '#10100E', 0.82);
      return (
        <g key={key}>
          {on > 0.02 && <circle cx={x} cy={py} r={px * 0.55} fill={col} opacity={0.35 * on} filter="url(#glow)" />}
          <circle cx={x} cy={py} r={px * 0.17} fill={on > 0.5 ? mixHex(col, '#FFFFFF', 0.28) : dim} />
          <path d={`M${x - px * 0.22} ${py - px * 0.15}Q${x} ${py - px * 0.3} ${x + px * 0.22} ${py - px * 0.15}`} stroke="#111614" strokeWidth={px * 0.05} fill="none" />
        </g>
      );
    };
    n.push(lamp(5.73, C.red, st.red, 'r'), lamp(5.3, C.amber, st.amber, 'a'), lamp(4.87, C.green, st.green, 'g'));
    return { z: d0, node: <g key={`sig${s.x}`}>{n}</g> };
  }).filter(Boolean);
};

// ------------------------------------------------------------------------------------ poles and wires

const POLES: [number, number][] = [[-8, 27.8], [8, 27.8], [27.8, 8], [27.8, -8], [8, -27.8], [-8, -27.8], [-27.8, 8], [-27.8, -8], [-24, 27.8], [24, 27.8]];

export const Poles = (v: View, f: number): Item[] => {
  const out: Item[] = [];
  for (const [x, z] of POLES) {
    const d0 = depth(v, [x, 4, z]);
    if (d0 < 1) continue;
    const n: React.ReactNode[] = [];
    n.push(...box(v, x - 0.14, x + 0.14, 0, 9, z - 0.14, z + 0.14, { side: C.pole, end: C.pole, top: C.pole }));
    n.push(...box(v, x - 1.1, x + 1.1, 8.3, 8.48, z - 0.08, z + 0.08, { side: C.pole, end: C.pole, top: C.pole }));
    if (x === -8) n.push(...box(v, x - 0.34, x + 0.34, 6.2, 7.5, z - 0.5, z - 0.14, { side: '#6E7E78', end: '#6E7E78', top: '#6E7E78' }));
    out.push({ z: d0, node: <g key={`p${x}${z}`}>{n}</g> });
  }
  // Sagging lines along the north block and across to the signal. They sway a touch, on the idle.
  const sw = 0.05 * idleSin(f, 3);
  const span = (a: V3, b: V3, sag = 1.0) => {
    const pts: V3[] = [];
    for (let i = 0; i <= 14; i++) {
      const p = sagAt(a, b, i / 14, sag + sw);
      pts.push(p);
    }
    return <path key={k()} d={line(v, pts)} stroke={C.wire} strokeWidth={1.7} fill="none" opacity={0.86} />;
  };
  const wires: React.ReactNode[] = [];
  for (const y of [8.45, 8.3]) {
    for (const [ax, bx] of [[-40, -24], [-24, -8], [-8, 8], [8, 24], [24, 40]]) {
      wires.push(span([ax, y, 27.8], [bx, y, 27.8], 0.8));
      wires.push(span([ax, y + 0.0, 27.3], [bx, y + 0.0, 27.3], 1.0));
    }
  }
  wires.push(span(FEED[0], FEED[1], 0.9));
  wires.push(span([-8, 8.4, 27.8], [-16.4, 6.3, 16.4], 0.9));
  out.push({ z: 25, node: <g key="wires">{wires}</g> });
  return out;
};

// ------------------------------------------------------------------------------------ traffic

/**
 * How far the ring road's traffic has rolled, per the signal. Parked until the film's green, then
 * rolling; in the idle it rolls while green and waits at red, a whole number of wraps per IDLE.
 */
const WRAP = 72;
const rolled = (f: number) => {
  if (f < INTRO) return kf(f, [[B.green, -WRAP * 2], [B.green + 30, -WRAP * 2 + 3.6, inQuad], [INTRO, 0, (t) => t]]);
  const i = ph(f);
  return kf(i, [[0, 0], [345, WRAP * 0.95, (t) => t], [372, WRAP, outQuad], [705, WRAP], [720, WRAP + 0.01, inQuad]]);
};

type Car = { lane: number; dir: number; at: number; kind: 'sedan' | 'taxi' | 'jeep' | 'bus' | 'van'; col: string; col2?: string };
const CARS: Car[] = [
  { lane: 20.2, dir: -1, at: 8, kind: 'taxi', col: '#E3B62F' },
  { lane: 20.2, dir: -1, at: 28, kind: 'sedan', col: '#C83A2E' },
  { lane: 20.2, dir: -1, at: 52, kind: 'jeep', col: '#D23A2E', col2: '#F2E6D2' },
  { lane: 23.8, dir: 1, at: 2, kind: 'van', col: '#EFE6D2' },
  { lane: 23.8, dir: 1, at: 24, kind: 'bus', col: '#E8E2CE', col2: '#2F6F5E' },
  { lane: 23.8, dir: 1, at: 50, kind: 'sedan', col: '#8EBCA2' },
];
const PARKED: { x: number; z: number; ax: boolean; kind: Car['kind']; col: string; col2?: string }[] = [
  { x: 20.2, z: -6.4, ax: false, kind: 'jeep', col: '#D23A2E', col2: '#F2E6D2' },
  { x: 23.8, z: 10.6, ax: false, kind: 'sedan', col: '#C83A2E' },
  { x: -20.2, z: 5.9, ax: false, kind: 'sedan', col: '#C83A2E' },
  { x: -23.8, z: 6.2, ax: false, kind: 'bus', col: '#E8E2CE', col2: '#2F6F5E' },
  { x: -20.2, z: -9.4, ax: false, kind: 'van', col: '#EFE6D2' },
  { x: 8.9, z: -20.2, ax: true, kind: 'sedan', col: '#EFE6D2' },
  { x: -6.5, z: -20.2, ax: true, kind: 'sedan', col: '#C83A2E' },
];
const DIMS: Record<Car['kind'], [number, number, number]> = { sedan: [2.2, 1.0, 0.95], taxi: [2.2, 1.0, 0.95], jeep: [3.0, 1.9, 1.05], bus: [5.2, 2.7, 1.2], van: [2.4, 1.8, 1.0] };

const vehicle = (v: View, x: number, z: number, ax: boolean, kind: Car['kind'], col: string, col2?: string) => {
  const [hl, h, hw] = DIMS[kind];
  const X = ax ? hl : hw;
  const Z = ax ? hw : hl;
  const n: React.ReactNode[] = [];
  const body = kind === 'sedan' || kind === 'taxi' ? 0.72 : h * 0.55;
  n.push(...box(v, x - X, x + X, 0.22, body, z - Z, z + Z, { side: col, end: col, top: col }));
  const X2 = ax ? hl * (kind === 'bus' || kind === 'van' || kind === 'jeep' ? 0.98 : 0.56) : hw * 0.94;
  const Z2 = ax ? hw * 0.94 : hl * (kind === 'bus' || kind === 'van' || kind === 'jeep' ? 0.98 : 0.56);
  n.push(...box(v, x - X2, x + X2, body, h + 0.2, z - Z2, z + Z2, { side: C.glassDim, end: C.glassDim, top: col2 ?? col }));
  n.push(...box(v, x - X2 - 0.02, x + X2 + 0.02, h + 0.04, h + 0.22, z - Z2 - 0.02, z + Z2 + 0.02, { side: col2 ?? col, end: col2 ?? col, top: col2 ?? col }));
  if (kind === 'jeep') n.push(...box(v, x - X - 0.01, x + X + 0.01, body - 0.2, body - 0.05, z - Z - 0.01, z + Z + 0.01, { side: '#F0B429', end: '#F0B429' }));
  n.push(...box(v, x - X * 0.95, x + X * 0.95, 0, 0.24, z - Z * 0.95, z + Z * 0.95, { side: '#1C181C', end: '#1C181C' }));
  return n;
};

export const Traffic = (v: View, f: number): Item[] => {
  const out: Item[] = [];
  const d = rolled(f);
  for (const c of CARS) {
    const q = ((c.at + d) % WRAP + WRAP) % WRAP;
    const x = c.dir > 0 ? -36 + q : 36 - q;
    if (depth(v, [x, 1, c.lane]) < 1) continue;
    out.push({ z: depth(v, [x, 1, c.lane]), node: <g key={`c${c.lane}${c.at}`}>{vehicle(v, x, c.lane, true, c.kind, c.col, c.col2)}</g> });
  }
  for (const p of PARKED) {
    if (depth(v, [p.x, 1, p.z]) < 1) continue;
    out.push({ z: depth(v, [p.x, 1, p.z]), node: <g key={`pk${p.x}${p.z}`}>{vehicle(v, p.x, p.z, p.ax, p.kind, p.col, p.col2)}</g> });
  }
  return out;
};

export { easeInOut, lerp };
