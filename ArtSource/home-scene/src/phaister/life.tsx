import React from 'react';
import { random } from 'remotion';
import { clamp01, LOOP, loopNoise, loopSin } from '../lib/time';
import { poly, project, V3, View } from './view';
import { DECK, FACADE_X, PAVE_X, PIER_X, PIERS } from './set';

/*
 * ⚠️ THE STREET HAS TO BE AS FULL AS ZACK'S ROOF. 🧑 2026-09-24: *"i want it atleast same level or EVEN
 * better"*. The first render was the right place and an empty one: flat piers, a flat road, no life.
 * Everything here is something docs/Ilalim_Ng_Tulay.md or LORE.md puts on this street (the pares
 * cart, the pisonet, the jeepney, lamps under the guideway, a tarpaulin), kept off the middle of the
 * stage so the trick keeps the eye. All of it is periodic on the loop.
 */

/** Sodium lamps hung under the guideway's edge on each pier cap, and the pools they throw. */
export const UnderLamps: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const out: React.ReactNode[] = [];
  for (const z of [...PIERS].reverse()) {
    const pool: string[] = [];
    for (let i = 0; i <= 20; i++) {
      const a = (i / 20) * Math.PI * 2;
      const [x, y] = project(v, [DECK.right + 0.6 + Math.cos(a) * 3.2, 0.01, z + Math.sin(a) * 3.6]);
      pool.push(`${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`);
    }
    out.push(<path key={`pl${z}`} d={pool.join('') + 'Z'} fill="#FFB050" opacity={0.2} filter="url(#glowBig)" />);
    out.push(<path key={`lb${z}`} d={poly(v, [[DECK.right - 0.5, 6.55, z - 0.35], [DECK.right - 0.5, 6.95, z - 0.35], [DECK.right - 0.1, 6.95, z - 0.35], [DECK.right - 0.1, 6.55, z - 0.35]])} fill="#2A181A" />);
    const [lx, ly] = project(v, [DECK.right - 0.3, 6.5, z - 0.36]);
    const s = Math.max(0.3, Math.min(3, 12 / z));
    const flick = 0.85 + 0.15 * loopNoise(`ul${z}`, f, 3);
    out.push(<circle key={`lg${z}`} cx={lx} cy={ly} r={40 * s} fill="#FFC870" opacity={0.45 * flick} filter="url(#glowBig)" />);
    out.push(<rect key={`ll${z}`} x={lx - 9 * s} y={ly - 3 * s} width={18 * s} height={5 * s} fill="#FFF0C0" />);
  }
  return <g>{out}</g>;
};

/** Moths round the nearest lamps: specks on slow orbits, a whole number of turns per loop. */
export const Moths: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const out: React.ReactNode[] = [];
  for (const z of PIERS.slice(0, 2)) {
    const [lx, ly] = project(v, [DECK.right - 0.3, 6.4, z - 0.4]);
    const s = Math.min(3, 12 / z);
    for (let i = 0; i < 5; i++) {
      const a = (2 * Math.PI * (i + 3) * f) / LOOP + i * 1.7;
      const r = (26 + 18 * random(`mo${z}${i}`)) * s;
      const x = lx + Math.cos(a * 3) * r;
      const y = ly + 20 * s + Math.sin(a * 5) * r * 0.5;
      out.push(<rect key={`${z}${i}`} x={x} y={y} width={3 * s + 1} height={3 * s + 1} fill="#FFE8B8" opacity={0.8} />);
    }
  }
  return <g>{out}</g>;
};

/**
 * Christmas lights strung across the road from the guideway to the shophouses, as a Manila street is
 * dressed for most of the year. Warm bulbs only (red, amber, gold, cream); each twinkles on its own
 * periodic clock.
 */
export const StringLights: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const out: React.ReactNode[] = [];
  const cols = ['#FFE7A0', '#FFC24A', '#FF7A4A', '#FFE7A0', '#F8B824', '#FF5A5A'];
  for (const [k, z] of [14, 25, 38, 55].entries()) {
    const a: V3 = [DECK.right, 7.3, z];
    const b: V3 = [FACADE_X, 5.8, z + 1.5];
    const n = 26;
    const pts: [number, number][] = [];
    for (let i = 0; i <= n; i++) {
      const t = i / n;
      const sag = Math.sin(t * Math.PI) * 1.1 + 0.05 * loopSin(f, 2, k + t * 3);
      pts.push(project(v, [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t - sag, a[2] + (b[2] - a[2]) * t]));
    }
    out.push(<path key={`w${k}`} d={pts.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join('')} stroke="#1E0E10" strokeWidth={2} fill="none" />);
    const s = Math.min(2, 16 / z);
    for (let i = 1; i < n; i++) {
      const [x, y] = pts[i];
      const tw = clamp01(0.55 + 0.6 * loopNoise(`sl${k}${i}`, f, 1.1));
      const c = cols[(i + k) % cols.length];
      out.push(<circle key={`g${k}${i}`} cx={x} cy={y + 4 * s} r={9 * s} fill={c} opacity={0.35 * tw} filter="url(#glow)" />);
      out.push(<rect key={`b${k}${i}`} x={x - 3 * s} y={y + 1 * s} width={6 * s} height={7 * s} fill={c} opacity={0.5 + 0.5 * tw} />);
    }
  }
  return <g>{out}</g>;
};

type Quad = (pts: V3[], fill: string, key: string, op?: number) => void;
const quads = (v: View, out: React.ReactNode[]): Quad => (pts, fill, key, op = 1) => {
  out.push(<path key={key} d={poly(v, pts)} fill={fill} opacity={op} />);
};

/**
 * A jeepney parked at the right kerb, tail toward us: the rear door and step, chrome, lit tail lamps
 * and a route board on the roof (Aurora Boulevard's jeepneys run to Cubao). Its own blocks and colours.
 */
export const Jeepney: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const X0 = 4.55;
  const X1 = 6.45;
  const Z0 = 19;
  const Z1 = 25.5;
  const out: React.ReactNode[] = [];
  const q = quads(v, out);
  q([[X0, 0.55, Z0], [X0, 2.3, Z0], [X0, 2.3, Z1], [X0, 0.55, Z1]], '#B8322A', 'side');
  q([[X0 - 0.01, 1.45, Z0 + 0.3], [X0 - 0.01, 2.1, Z0 + 0.3], [X0 - 0.01, 2.1, Z1 - 0.6], [X0 - 0.01, 1.45, Z1 - 0.6]], '#3A1A1C', 'win');
  q([[X0 - 0.02, 1.0, Z0], [X0 - 0.02, 1.2, Z0], [X0 - 0.02, 1.2, Z1], [X0 - 0.02, 1.0, Z1]], '#F2C23A', 'stripe');
  q([[X0 - 0.02, 0.75, Z0], [X0 - 0.02, 0.85, Z0], [X0 - 0.02, 0.85, Z1], [X0 - 0.02, 0.75, Z1]], '#E8E0D0', 'chrome');
  q([[X0, 2.3, Z0], [X0 - 0.1, 2.45, Z0], [X1 + 0.1, 2.45, Z0], [X1, 2.3, Z0]], '#8E2420', 'roofe');
  q([[X0 - 0.1, 2.45, Z0], [X0 - 0.1, 2.45, Z1], [X0, 2.3, Z1], [X0, 2.3, Z0]], '#6E1A18', 'roofs');
  q([[X0, 0.55, Z0], [X0, 2.3, Z0], [X1, 2.3, Z0], [X1, 0.55, Z0]], '#C83A2E', 'rear');
  q([[X0 + 0.55, 0.6, Z0 - 0.01], [X0 + 0.55, 2.05, Z0 - 0.01], [X1 - 0.55, 2.05, Z0 - 0.01], [X1 - 0.55, 0.6, Z0 - 0.01]], '#2A1214', 'door');
  q([[X0 + 0.5, 0.45, Z0 - 0.3], [X0 + 0.5, 0.55, Z0 - 0.3], [X1 - 0.5, 0.55, Z0 - 0.3], [X1 - 0.5, 0.45, Z0 - 0.3]], '#9A8E80', 'step');
  q([[X0 - 0.05, 0.4, Z0 - 0.05], [X0 - 0.05, 0.6, Z0 - 0.05], [X1 + 0.05, 0.6, Z0 - 0.05], [X1 + 0.05, 0.4, Z0 - 0.05]], '#E8E0D0', 'bumper');
  q([[X0 + 0.02, 1.9, Z0 - 0.01], [X0 + 0.02, 2.2, Z0 - 0.01], [X0 + 0.5, 2.2, Z0 - 0.01], [X0 + 0.5, 1.9, Z0 - 0.01]], '#F2C23A', 'pl');
  q([[X1 - 0.5, 1.9, Z0 - 0.01], [X1 - 0.5, 2.2, Z0 - 0.01], [X1 - 0.02, 2.2, Z0 - 0.01], [X1 - 0.02, 1.9, Z0 - 0.01]], '#F2C23A', 'pr');
  for (const x of [X0 + 0.2, X1 - 0.2]) {
    const [lx, ly] = project(v, [x, 0.95, Z0 - 0.02]);
    const glow = 0.8 + 0.2 * loopNoise(`tl${x}`, f, 0.5);
    out.push(<circle key={`tg${x}`} cx={lx} cy={ly} r={16} fill="#FF3A2A" opacity={0.5 * glow} filter="url(#glow)" />);
    out.push(<rect key={`tl${x}`} x={lx - 5} y={ly - 5} width={10} height={10} fill="#FF6A4A" />);
  }
  const [a, b] = [project(v, [X0 + 0.25, 2.95, Z0 + 0.2]), project(v, [X1 - 0.25, 2.5, Z0 + 0.2])];
  if (b[0] - a[0] > 8) {
    out.push(<rect key="rb" x={a[0]} y={a[1]} width={b[0] - a[0]} height={b[1] - a[1]} fill="#F4E9D6" />);
    out.push(<text key="rt" x={(a[0] + b[0]) / 2} y={b[1] - (b[1] - a[1]) * 0.2} textAnchor="middle" fontFamily="Paalalabas" fontSize={(b[1] - a[1]) * 0.72} fill="#9A1E1A" textLength={(b[0] - a[0]) * 0.86} lengthAdjust="spacingAndGlyphs">CUBAO</text>);
  }
  for (const z of [Z0 + 1.2, Z1 - 1.2]) q([[X0 - 0.02, 0, z - 0.45], [X0 - 0.02, 0.62, z - 0.45], [X0 - 0.02, 0.62, z + 0.45], [X0 - 0.02, 0, z + 0.45]], '#140A0C', `wh${z}`);
  return <g>{out}</g>;
};

/** The pares stall: a cart, its pot steaming under a bare bulb, two red monobloc stools. */
export const ParesCart: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const x0 = PAVE_X + 0.35;
  const x1 = x0 + 0.9;
  const z0 = 11.2;
  const z1 = 12.6;
  const out: React.ReactNode[] = [];
  const q = quads(v, out);
  q([[x0, 0.16, z0], [x0, 1.0, z0], [x0, 1.0, z1], [x0, 0.16, z1]], '#C8902A', 'front');
  q([[x0, 0.16, z0], [x0, 1.0, z0], [x1, 1.0, z0], [x1, 0.16, z0]], '#8E5A1C', 'end');
  q([[x0, 1.0, z0], [x1, 1.0, z0], [x1, 1.0, z1], [x0, 1.0, z1]], '#E8C890', 'top');
  q([[x0 - 0.01, 0.45, z0 + 0.15], [x0 - 0.01, 0.8, z0 + 0.15], [x0 - 0.01, 0.8, z1 - 0.15], [x0 - 0.01, 0.45, z1 - 0.15]], '#F0B028', 'sign');
  const [sa, sb] = [project(v, [x0 - 0.02, 0.8, z0 + 0.2]), project(v, [x0 - 0.02, 0.45, z1 - 0.2])];
  out.push(<text key="st" x={(sa[0] + sb[0]) / 2} y={sb[1] - 3} textAnchor="middle" fontFamily="Paalalabas" fontSize={Math.abs(sb[1] - sa[1]) * 0.7} fill="#8A1A14">PARES</text>);
  q([[x0 + 0.25, 1.0, z0 + 0.35], [x0 + 0.25, 1.35, z0 + 0.35], [x0 + 0.25, 1.35, z0 + 0.95], [x0 + 0.25, 1.0, z0 + 0.95]], '#8A7A6E', 'pot');
  q([[x0 + 0.02, 1.0, z1 - 0.1], [x0 + 0.02, 2.4, z1 - 0.1], [x0 + 0.08, 2.4, z1 - 0.1], [x0 + 0.08, 1.0, z1 - 0.1]], '#2A181A', 'pole');
  const [bx, by] = project(v, [x0 + 0.3, 2.2, z0 + 0.6]);
  out.push(<circle key="bg" cx={bx} cy={by} r={46} fill="#FFD27A" opacity={0.45} filter="url(#glowBig)" />);
  out.push(<rect key="bb" x={bx - 5} y={by - 6} width={10} height={12} fill="#FFF4D0" />);
  // Steam in blocks, rising and thinning: 25 puffs a loop, so the seam is between two of them.
  for (let i = 0; i < 6; i++) {
    const ph = ((f * 25) / LOOP + i / 6) % 1;
    const [sx, sy] = project(v, [x0 + 0.3 + 0.1 * Math.sin(ph * 6 + i), 1.4 + ph * 1.1, z0 + 0.65]);
    const sz = 8 + ph * 22;
    out.push(<rect key={`sm${i}`} x={sx - sz / 2} y={sy - sz / 2} width={sz} height={sz} fill="#F4E4D0" opacity={0.3 * (1 - ph)} />);
  }
  for (const [k, z] of [z0 - 0.7, z1 + 0.5].entries()) {
    const sx0 = x0 - 0.55;
    q([[sx0, 0.16, z], [sx0, 0.6, z], [sx0 + 0.42, 0.6, z], [sx0 + 0.42, 0.16, z]], '#C0281E', `stl${k}`);
    q([[sx0, 0.6, z], [sx0, 0.6, z + 0.42], [sx0 + 0.42, 0.6, z + 0.42], [sx0 + 0.42, 0.6, z]], '#E0463A', `stt${k}`);
  }
  return <g>{out}</g>;
};

/** The pisonet: coin-op computer cabinets against a shopfront, their screens lighting the pavement. */
export const Pisonet: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const x1 = FACADE_X - 0.05;
  const x0 = x1 - 0.7;
  const out: React.ReactNode[] = [];
  for (const [k, z] of [15.4, 16.3].entries()) {
    out.push(<path key={`c${k}`} d={poly(v, [[x0, 0.16, z], [x0, 1.7, z], [x0, 1.7, z + 0.8], [x0, 0.16, z + 0.8]])} fill="#2E5A3A" />);
    const flick = 0.8 + 0.2 * loopNoise(`pn${k}`, f, 4);
    out.push(<path key={`s${k}`} d={poly(v, [[x0 - 0.01, 0.95, z + 0.1], [x0 - 0.01, 1.45, z + 0.1], [x0 - 0.01, 1.45, z + 0.7], [x0 - 0.01, 0.95, z + 0.7]])} fill="#FFF1C8" opacity={flick} />);
    out.push(<path key={`sp${k}`} d={poly(v, [[x0, 0.17, z - 0.2], [x0 - 1.4, 0.17, z - 0.4], [x0 - 1.4, 0.17, z + 1.2], [x0, 0.17, z + 1.0]])} fill="#FFE8B0" opacity={0.12 * flick} />);
  }
  return <g>{out}</g>;
};

/** Puddles on the road, each holding a smear of the sign or lamp above it. */
export const Puddles: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const out: React.ReactNode[] = [];
  const list: [number, number, number, number, string][] = [
    [2.6, 8.5, 1.6, 1.1, '#C8322A'],
    [4.4, 13, 1.2, 1.4, '#2E7A44'],
    [-1.6, 9.6, 1.0, 0.8, '#FFC870'],
    [3.4, 5.8, 0.9, 0.7, '#FFB050'],
  ];
  for (const [i, [x, z, w, d, c]] of list.entries()) {
    const pts: string[] = [];
    for (let k = 0; k <= 14; k++) {
      const a = (k / 14) * Math.PI * 2;
      const r = 1 + 0.18 * Math.sin(a * 3 + i);
      const [sx, sy] = project(v, [x + Math.cos(a) * w * r * 0.5, 0.004, z + Math.sin(a) * d * r * 0.5]);
      pts.push(`${k ? 'L' : 'M'}${sx.toFixed(1)} ${sy.toFixed(1)}`);
    }
    out.push(<path key={`p${i}`} d={pts.join('') + 'Z'} fill="#3E2A30" />);
    out.push(<path key={`r${i}`} d={pts.join('') + 'Z'} fill={c} opacity={0.22 + 0.06 * loopSin(f, 6, i)} />);
  }
  return <g>{out}</g>;
};

/** Weather on the near piers: rain stains down the lit face, a splash band, a barangay tarpaulin. */
export const PierWear: React.FC<{ v: View }> = ({ v }) => {
  const out: React.ReactNode[] = [];
  const x1 = PIER_X + 0.8 + 0.005;
  for (const z of PIERS.slice(0, 3)) {
    out.push(<path key={`b${z}`} d={poly(v, [[x1, 0.2, z - 0.8], [x1, 0.9, z - 0.8], [x1, 0.9, z + 0.8], [x1, 0.2, z + 0.8]])} fill="#3A2826" opacity={0.7} />);
    for (let i = 0; i < 5; i++) {
      const zz = z - 0.7 + random(`st${z}${i}`) * 1.3;
      const len = 1.5 + random(`sl${z}${i}`) * 3;
      out.push(<path key={`s${z}${i}`} d={poly(v, [[x1, 6.2 - len, zz], [x1, 6.2, zz], [x1, 6.2, zz + 0.08], [x1, 6.2 - len, zz + 0.08]])} fill="#3E2C2A" opacity={0.6} />);
    }
  }
  const z = PIERS[0];
  out.push(<path key="tarp" d={poly(v, [[x1 + 0.01, 3.0, z - 0.7], [x1 + 0.01, 4.4, z - 0.7], [x1 + 0.01, 4.4, z + 0.7], [x1 + 0.01, 3.0, z + 0.7]])} fill="#EDE2CC" />);
  out.push(<path key="tarpb" d={poly(v, [[x1 + 0.012, 4.05, z - 0.7], [x1 + 0.012, 4.4, z - 0.7], [x1 + 0.012, 4.4, z + 0.7], [x1 + 0.012, 4.05, z + 0.7]])} fill="#B8282A" />);
  const [a, b] = [project(v, [x1 + 0.02, 3.9, z - 0.6]), project(v, [x1 + 0.02, 3.3, z + 0.6])];
  if (b[0] - a[0] > 14) out.push(<text key="tt" x={(a[0] + b[0]) / 2} y={(a[1] + b[1]) / 2 + 4} textAnchor="middle" fontFamily="Paalalabas" fontSize={Math.abs(b[1] - a[1]) * 0.45} fill="#6A1418" textLength={(b[0] - a[0]) * 0.8} lengthAdjust="spacingAndGlyphs">LIGA NG BARANGAY</text>);
  return <g>{out}</g>;
};
