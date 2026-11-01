import React from 'react';
import { random } from 'remotion';
import { kf } from '../lib/kf';
import { idleSin, INTRO, ph } from './time';
import { B } from './beats';
import { depth, horizonY, poly, project, scaleAt, V3, View } from '../phaister/view';
import { box, Item } from '../phaister/set';
import { Clutter, FarBlocks, FloorDetail, OldBuildings, Shopfronts } from './city';

/*
 * ERMITA, drawn, at the hour of the game's own map film (`ErmitaFilm`): night under the LRT on Taft, sodium
 * lamps, a train overhead. Landmarks from `Resources/UI/map-previews/IlalimNgTulay-poster.png`: the lot's
 * chalk box with its yellow outer line, the viaduct and its piers down the east side, the white civic hall
 * with tall windows and a fenced frontage over the road, a hoop on the west, banderitas, tangled wires, a
 * tricycle.
 */
const C = {
  skyTop: '#1B1030', skyLow: '#6A2A55',
  ground: '#3E3448', road: '#2D2638', lot: '#54485A', lotLine: '#EDE3D2', yellow: '#D9A92E',
  hall: '#6E6478', hallLit: '#8A7C8A', col: '#857A8C', win: '#2A2438', winLit: '#F4C26C',
  deck: '#3C3444', deckLit: '#5A4F60', pier: '#4A4150', pierTop: '#5E5468',
  pole: '#231C2A', wire: '#120D18', lampGlass: '#FFD98A', sodium: '#FFB25A',
  train: '#6A6A78', trainLit: '#FFF2C8',
};

let KEY = 0;
const k = () => `e${KEY++}`;
const face = (v: View, pts: V3[], fill: string, op = 1) => {
  const d = poly(v, pts);
  return d ? <path key={k()} d={d} fill={fill} opacity={op} /> : null;
};

export const LAMPS: [number, number][] = [[-3.6, 2.2], [9.4, -6], [9.4, 9]];
const UNDER: [number, number][] = [[15, -36], [15, -6], [15, 24], [15, 54], [-11, -11], [-11, 11], [-30, 14], [24, 16]];
export const DECK_X = 15;

/** The stutter of the lamp over the can: dark, a buzz, on. Steady after. */
export const lampAt = (f: number) => (f >= B.lamp + 34 ? 1 : f < B.lamp ? 0 : random(`lmp${Math.floor(f / 2)}`) > 0.55 + (B.lamp + 34 - f) / 90 ? 0 : 0.9);

/** Where the train's nose is along the deck, or null. One pass in the film; a quiet one in each idle. */
export const trainAt = (f: number): number | null => {
  // One goes by as the film opens; the next is the roof over the chase.
  const first = f * 2.3 - 60;
  const t = f >= INTRO ? (ph(f) - 300) * 1.6 - 95 : first < 240 ? first : (f - B.blink + 60) * 2.3 - 55;
  return t > -100 && t < 240 ? t : null;
};

export const Sky: React.FC<{ v: View }> = ({ v }) => {
  const h = horizonY(v);
  return <g>
    <defs><linearGradient id="nightSky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor={C.skyTop} /><stop offset="1" stopColor={C.skyLow} /></linearGradient></defs>
    <rect x={-300} y={h - 1500} width={2520} height={1520} fill="url(#nightSky)" />
    <rect x={-300} y={h} width={2520} height={2000} fill={C.ground} />
  </g>;
};

const pool = (v: View, x: number, z: number, r: number, a: number, col = C.sodium) => {
  const out: React.ReactNode[] = [];
  for (const [q, o] of [[1, 0.1], [0.72, 0.13], [0.46, 0.17]] as const) {
    const pts: V3[] = [];
    for (let i = 0; i < 26; i++) pts.push([x + Math.cos((i / 26) * 6.283) * r * q, 0.01, z + Math.sin((i / 26) * 6.283) * r * q]);
    out.push(face(v, pts, col, o * a));
  }
  return out;
};

export const Ground: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const rect = (x0: number, x1: number, z0: number, z1: number, fill: string) => face(v, [[x0, 0, z0], [x1, 0, z0], [x1, 0, z1], [x0, 0, z1]], fill);
  const frame = (r: number, w: number, fill: string) => [rect(-r, r, -r, -r + w, fill), rect(-r, r, r - w, r, fill), rect(-r, -r + w, -r, r, fill), rect(r - w, r, -r, r, fill)];
  const pass = trainAt(f);
  return <g>
    {rect(-90, 90, 12, 21, C.road)}
    {rect(9.5, 90, -90, 90, C.road)}
    {rect(-11, 10.5, -13, 11.5, C.lot)}
    <FloorDetail v={v} />
    {frame(8, 0.09, C.yellow)}
    {frame(7, 0.09, C.lotLine)}
    {[-9, -5, -1, 3, 7].map((x) => <g key={x}>{rect(x * 3, x * 3 + 2.2, 16.4, 16.55, '#B9AE9E')}</g>)}
    {pool(v, LAMPS[0][0] + 2.4, LAMPS[0][1] - 2.8, 7, lampAt(f))}
    {pool(v, LAMPS[1][0] - 1, LAMPS[1][1], 5, 0.7)}
    {pool(v, LAMPS[2][0] - 1, LAMPS[2][1], 5, 0.7)}
    {UNDER.map(([x, z]) => <g key={`${x}.${z}`}>{pool(v, x + 1, z, 6, 0.75)}</g>)}
    {pass !== null && pool(v, DECK_X - 3, pass - 40, 16, 0.55, '#E9D8FF')}
  </g>;
};

/** Everything behind the focus layer. */
export const City = (v: View, f: number): Item[] => [...FarBlocks(v), ...OldBuildings(v), ...Shopfronts(v, f)];

const Z_RUN = 170;
/** The guideway and its piers: the weight over everything. */
export const Viaduct = (v: View, f: number): Item[] => {
  const out: Item[] = [];
  const w = 5.25;
  const x = DECK_X;
  const nose = trainAt(f);
  const train: React.ReactNode[] = [];
  if (nose !== null) {
    for (let c = 0; c < 4; c++) {
      const a = nose - 40 - c * 16;
      if (depth(v, [x - 2.3, 10, a]) < 1) continue;
      train.push(<g key={`car${c}`}>{box(v, x - 3.6, x - 1, 9.1, 12.4, a - 14.5, a, { side: C.train, end: '#55525F', top: '#7D7C88' })}</g>);
      for (let i = 0; i < 6; i++) {
        const z = a - 13.4 + i * 2.2;
        train.push(face(v, [[x - 3.61, 10.4, z], [x - 3.61, 11.7, z], [x - 3.61, 11.7, z + 1.5], [x - 3.61, 10.4, z + 1.5]], C.trainLit));
      }
    }
  }
  out.push({ z: depth(v, [x, 8, Math.max(-Z_RUN, v.z + 30)]) + 40, node: <g key={k()}>
    {train}
    {box(v, x - w, x + w, 8, 9.6, -Z_RUN, Z_RUN, { side: C.deckLit, end: C.deck, bottom: C.deck })}
  </g> });
  for (let z = -60; z <= 90; z += 30) {
    const at: V3 = [x, 4, z - 12];
    if (depth(v, at) < 0.8) continue;
    out.push({ z: depth(v, at), node: <g key={k()}>
      {box(v, x - 1.1, x + 1.1, 0, 6.7, z - 13.1, z - 10.9, { side: C.pier, end: C.pierTop })}
      {box(v, x - 4, x + 4, 6.7, 8, z - 13.6, z - 10.4, { side: C.pierTop, end: C.pier, bottom: C.pier })}
    </g> });
  }
  return out;
};

const lampItem = (v: View, x: number, z: number, a: number): Item | null => {
  if (depth(v, [x, 3, z]) < 0.8) return null;
  const [gx, gy] = project(v, [x + 1.3, 6.15, z]);
  const s = scaleAt(v, [x, 6, z]);
  return { z: depth(v, [x, 3, z]), node: <g key={k()}>
    {box(v, x - 0.09, x + 0.09, 0, 6.5, z - 0.09, z + 0.09, { side: C.pole, end: C.pole })}
    {box(v, x, x + 1.5, 6.4, 6.5, z - 0.05, z + 0.05, { side: C.pole, end: C.pole, bottom: C.pole })}
    {box(v, x + 1, x + 1.6, 6.22, 6.4, z - 0.14, z + 0.14, { side: C.lampGlass, end: C.lampGlass, bottom: C.lampGlass })}
    <circle cx={gx} cy={gy} r={s * 1.1} fill={C.sodium} opacity={0.3 * a} filter="url(#glow)" />
  </g> };
};

const FLAGS = ['#D8346A', '#E6A92E', '#5BB56A', '#8C4AC8', '#EDE3D2'];
/** The lot's edge, its street and its strings: what stands in front of the city. */
export const Street = (v: View, f: number): Item[] => {
  const out: Item[] = [];
  const add = (it: Item | null) => { if (it) out.push(it); };
  add(lampItem(v, LAMPS[0][0], LAMPS[0][1], lampAt(f)));
  add(lampItem(v, LAMPS[1][0], LAMPS[1][1], 0.8));
  add(lampItem(v, LAMPS[2][0], LAMPS[2][1], 0.8));
  for (const [x, z] of UNDER) add(lampItem(v, x, z, 0.8));
  // The fence along the north of the lot and the hoop on the west.
  for (let x = -10.5; x <= 8; x += 3.7) {
    const at: V3 = [x, 1, 11.5];
    if (depth(v, at) < 0.8) continue;
    out.push({ z: depth(v, at), node: <g key={k()}>
      {box(v, x - 0.25, x + 0.25, 0, 1.5, 11.25, 11.75, { side: '#6F6472', end: '#857A84', top: '#92868E' })}
      {x < 7 && box(v, x + 0.25, x + 3.45, 1.0, 1.08, 11.47, 11.53, { side: C.pole, end: C.pole })}
      {x < 7 && Array.from({ length: 9 }, (_, i) => <g key={i}>{box(v, x + 0.5 + i * 0.32, x + 0.54 + i * 0.32, 0.08, 1.15, 11.48, 11.52, { side: C.pole, end: C.pole })}</g>)}
    </g> });
  }
  for (let x = -10.5; x <= 8; x += 3.7) {
    if (depth(v, [x, 1, -13]) < 0.8) continue;
    out.push({ z: depth(v, [x, 1, -13]), node: <g key={k()}>{box(v, x - 0.25, x + 0.25, 0, 1.5, -13.25, -12.75, { side: '#6F6472', end: '#857A84', top: '#92868E' })}{x < 7 && box(v, x + 0.25, x + 3.45, 1.0, 1.08, -13.03, -12.97, { side: C.pole, end: C.pole })}</g> });
  }
  if (depth(v, [-10, 2, 5]) > 0.8) out.push({ z: depth(v, [-10, 2, 5]), node: <g key={k()}>
    {box(v, -10.1, -9.95, 0, 3.1, 4.95, 5.1, { side: '#B7923A', end: '#D2AC4A' })}
    {box(v, -10, -9.9, 3, 4.1, 4.3, 5.8, { side: '#D8CFC0', end: '#BDB4A6' })}
  </g> });
  // The tricycle, parked under the far lamp.
  const tx = 8.6, tz = 15;
  if (depth(v, [tx, 1, tz]) > 0.8) out.push({ z: depth(v, [tx, 1, tz]), node: <g key={k()}>
    {box(v, tx - 1.1, tx + 1.1, 0.35, 1.3, tz - 0.6, tz + 0.6, { side: '#D9A92E', end: '#B88F24', top: '#E6BD46' })}
    {box(v, tx - 1.1, tx + 1.1, 1.8, 1.92, tz - 0.65, tz + 0.65, { side: '#3A3440', end: '#3A3440', top: '#55505C', bottom: '#2A2530' })}
    {box(v, tx - 1, tx - 0.92, 1.3, 1.8, tz - 0.55, tz - 0.47, { side: C.pole, end: C.pole })}
    {box(v, tx + 0.92, tx + 1, 1.3, 1.8, tz - 0.55, tz - 0.47, { side: C.pole, end: C.pole })}
  </g> });
  // Banderitas and wires, strung the width of the lot.
  const sway = 0.1 * idleSin(f, 4);
  const strung: React.ReactNode[] = [];
  for (const [z, y, dip] of [[11, 6.1, 1.1], [11.8, 7, 0.9], [12.4, 7.5, 1.4], [10.6, 7.6, 1.8], [12.8, 8.2, 0.7], [11.2, 8.6, 2.1]] as const) {
    let d = '';
    const flags: React.ReactNode[] = [];
    for (let i = 0; i <= 24; i++) {
      const u = i / 24;
      const x = -14 + 24 * u;
      const py = y - dip * 4 * u * (1 - u) + sway * 4 * u * (1 - u);
      if (depth(v, [x, py, z]) < 0.8) continue;
      const [sx, sy] = project(v, [x, py, z]);
      d += `${d ? 'L' : 'M'}${sx.toFixed(1)} ${sy.toFixed(1)}`;
      if (z === 11) {
        const s = scaleAt(v, [x, py, z]);
        flags.push(<path key={i} d={`M${sx - s * 0.22} ${sy}L${sx + s * 0.22} ${sy}L${sx} ${sy + s * 0.48}Z`} fill={FLAGS[i % FLAGS.length]} opacity={0.92} />);
      }
    }
    if (d) strung.push(<g key={z}><path d={d} fill="none" stroke={C.wire} strokeWidth={Math.max(1.2, scaleAt(v, [0, y, z]) * 0.03)} />{flags}</g>);
  }
  if (depth(v, [0, 6, 11]) > 0.8) out.push({ z: depth(v, [0, 6, 11]) - 0.5, node: <g key={k()}>{strung}</g> });
  out.push(...Clutter(v, f));
  return out;
};

/** The train's window light through the piers, across everything near: bars, not a flash. */
export const strobeAt = (f: number) => {
  if (f >= INTRO) return 0;
  return kf(f, [[B.chase, 0], [B.chase + 8, 1], [B.blink + 6, 1], [B.blink + 20, 0]]) * (0.5 + 0.5 * Math.sin(f * 1.05));
};
