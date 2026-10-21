import React from 'react';
import { random } from 'remotion';
import { clamp01, env, LOOP, loopSin } from './time';
import { inCubic, kf, outBack, outCubic, outQuad } from '../lib/kf';
import { PB } from './beats';
import { depth, line, poly, project, scaleAt, V3, View } from './view';
import { CAST, HerFrame, SAFE, slipperTrail, TAG } from './perform';
import { NemuFrame } from './nemu';
import { DECK } from './map';

/*
 * Her effects, in the shapes her kit already has in the game (Runtime/Visual/VfxShapes.cs and
 * HeroHazards.CovenCircleBuild), so the loop and a match speak one language. Colours are hers:
 * `UiTheme.HeroWitch` e828c5, `HeroWitchBright` f444d4, the pale and deep violets of the coven
 * inscription, and the gold of the eclipse corona. Nothing is blue.
 */

export const WITCH = '#E828C5';
export const WITCH_BRIGHT = '#F444D4';
export const WITCH_CORE = '#FFD6F4';
export const PALE = '#F2CCF2';
export const DEEP = '#9C3AB4';
export const GOLD = '#F8B824';

const ring = (c: V3, r: number, a0 = 0, a1 = Math.PI * 2, n = 72, y = 0.02): V3[] => {
  const out: V3[] = [];
  const steps = Math.max(3, Math.ceil((n * (a1 - a0)) / (Math.PI * 2)));
  for (let i = 0; i <= steps; i++) {
    const a = a0 + ((a1 - a0) * i) / steps;
    out.push([c[0] + Math.cos(a) * r, y, c[2] + Math.sin(a) * r]);
  }
  return out;
};

/** A stroke drawn on in world space: `t` 0..1 how much of the polyline exists. */
const partial = (pts: V3[], t: number): V3[] => {
  if (t >= 1) return pts;
  if (t <= 0) return [];
  const n = (pts.length - 1) * t;
  const i = Math.floor(n);
  const out = pts.slice(0, i + 1);
  const a = pts[i];
  const b = pts[Math.min(pts.length - 1, i + 1)];
  const u = n - i;
  out.push([a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u, a[2] + (b[2] - a[2]) * u]);
  return out;
};

/** A glowing line: a soft bloom under a crisp core, the way her sigils read in the game. */
const Glow: React.FC<{ d: string; w: number; c: string; a?: number; core?: boolean }> = ({ d, w, c, a = 1, core = true }) =>
  !d ? null : (
    <g opacity={a}>
      <path d={d} stroke={c} strokeWidth={w * 4} fill="none" opacity={0.35} filter="url(#glow)" strokeLinecap="round" />
      <path d={d} stroke={c} strokeWidth={w} fill="none" strokeLinecap="round" strokeLinejoin="round" />
      {core && <path d={d} stroke={WITCH_CORE} strokeWidth={w * 0.35} fill="none" strokeLinecap="round" />}
    </g>
  );

// ------------------------------------------------------------------------------------ runes

/**
 * A rune: two to four ruled strokes in a box, seeded, every seed a different letter. Her lunar writing
 * is fictional (docs/CHARACTER_ORIGINS.md), never a real script.
 */
const rune = (seed: string): [number, number][][] => {
  const out: [number, number][][] = [];
  const n = 2 + Math.floor(random(seed + 'n') * 3);
  for (let k = 0; k < n; k++) {
    const q = (s: string) => Math.round((random(seed + k + s) - 0.5) * 4) / 4;
    const kind = random(seed + k + 'o');
    if (kind < 0.4) out.push([[q('a'), -0.5], [q('a') + q('c') * 0.5, 0.5]]);
    else if (kind < 0.75) out.push([[-0.5, q('b')], [0.5, q('b')]]);
    else out.push([[q('d'), -0.5], [q('e'), 0], [q('f'), 0.5]]);
  }
  return out;
};

// ------------------------------------------------------------------------------------ small magic

/**
 * The charm's hex: a tiny ward turning flat under the floating slipper, drawn round her hand's place on
 * screen (measured off the rig, so it sits where her palm really is), squashed as a flat ring is.
 */
const PalmHex: React.FC<{ f: number; at: [number, number]; s: number }> = ({ f, at, s }) => {
  // ⚠️ Retired with the levitating charm: the throw has no magic in it now (beats.ts). Kept for a later loop.
  const a = 0 * f;
  if (a <= 0.01) return null;
  const rot = f * 0.12;
  const [x, y] = [at[0], at[1] - 0.02 * s];
  const ell = (r: number) => `M${x - r} ${y}A${r} ${r * 0.35} 0 1 0 ${x + r} ${y}A${r} ${r * 0.35} 0 1 0 ${x - r} ${y}`;
  const tri = [0, 1, 2, 3].map((k) => `${k ? 'L' : 'M'}${(x + Math.cos(rot + (k * 2 * Math.PI) / 3) * 0.14 * s).toFixed(1)} ${(y + Math.sin(rot + (k * 2 * Math.PI) / 3) * 0.05 * s).toFixed(1)}`).join('');
  const w = Math.max(1.5, 0.012 * s);
  return <g><Glow d={ell(0.2 * s)} w={w} c={WITCH_BRIGHT} a={a} /><Glow d={ell(0.15 * s) + tri} w={w * 0.7} c={GOLD} a={a} core={false} /></g>;
};

/** The thrown slipper's streak: her colour, and it is the only time a thrown slipper glows. */
const Trail: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const pts = slipperTrail(f, 8);
  if (pts.length < 2) return null;
  const d = line(v, pts);
  const w = Math.max(3, 0.08 * scaleAt(v, pts[pts.length - 1]));
  return <Glow d={d} w={w} c={WITCH_BRIGHT} a={0.9} />;
};

// ------------------------------------------------------------------------------------ sound words

/**
 * Lettered sound, in the display face, as Zack's TUMP! is: TRES! at the hoop (the map's own callout,
 * docs/Ilalim_Ng_Tulay.md § 4.1) and KLANG! at the can. Each letter pops with an overshoot and rings
 * down (a smooth decaying tremble, never a random jitter).
 */
const Word: React.FC<{ text: string; f: number; t0: number; x: number; y: number; s: number; fill: string; edge: string; life?: number; tilt?: number }> = ({ text, f, t0, x, y, s, fill, edge, life = 34, tilt = -6 }) => {
  const t = f - t0;
  if (t < 0 || t > life) return null;
  const fade = kf(t, [[life - 10, 1], [life, 0]]);
  const size = 140 * s;
  const adv = size * 0.62;
  const letters = [...text];
  return (
    <g opacity={fade} transform={`translate(${x} ${y}) rotate(${tilt})`}>
      {letters.map((ch, i) => {
        const ti = i * 1.4;
        if (t < ti) return null;
        const sc = kf(t, [[ti, 0.2], [ti + 4, 1.2, outBack], [ti + 9, 1]]);
        const rg = Math.exp(-(t - ti) / 12) * Math.sin((t - ti) * 1.8 + i);
        const lx = (i - (letters.length - 1) / 2) * adv;
        return (
          <g key={i} transform={`translate(${lx} ${-Math.abs(i - (letters.length - 1) / 2) * 5 * s}) rotate(${(i % 2 ? 5 : -5) + 8 * rg}) scale(${sc})`}>
            <text x={5 * s} y={7 * s} fontFamily="Darumadrop One" fontSize={size} textAnchor="middle" fill="#2A0620">{ch}</text>
            <text x={0} y={0} fontFamily="Darumadrop One" fontSize={size} textAnchor="middle" fill={fill} stroke={edge} strokeWidth={8 * s} paintOrder="stroke">{ch}</text>
          </g>
        );
      })}
    </g>
  );
};

/** Rings of sound rolling off the can across the road. */
const SoundRings: React.FC<{ v: View; f: number; t0: number; at: V3; big?: number }> = ({ v, f, t0, at, big = 1 }) => {
  const out: React.ReactNode[] = [];
  for (let k = 0; k < 3; k++) {
    const t = (f - t0 - k * 4) / 22;
    if (t <= 0 || t >= 1) continue;
    const d = line(v, ring([at[0], 0, at[2]], (0.4 + 3.2 * outCubic(t)) * big, 0, Math.PI * 2, 40, 0.03));
    out.push(<path key={k} d={d} stroke="#FFF0CE" strokeWidth={Math.max(1.5, 5 * (1 - t))} fill="none" opacity={0.7 * (1 - t)} />);
  }
  return <g>{out}</g>;
};

// ------------------------------------------------------------------------------------ the run

/** Speed lines across the frame during the tracking run: screen space, sliding the way the world does. */
const SpeedLines: React.FC<{ f: number }> = ({ f }) => {
  const a = env(f, PB.run + 4, PB.run + 10, PB.scoop - 6, PB.scoop + 2) + 0.8 * env(f, PB.scoop + 16, PB.block + 4, PB.juke, PB.juke + 6);
  if (a <= 0.01) return null;
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 16; i++) {
    const y = 120 + random(`sl${i}`) * 840;
    if (y > 380 && y < 700 && random(`slm${i}`) < 0.7) continue;
    const len = 180 + random(`sll${i}`) * 420;
    const x = ((random(`slx${i}`) * 2400 + f * (70 + random(`slv${i}`) * 50)) % 2600) - 340;
    out.push(<rect key={i} x={x} y={y} width={len} height={2 + random(`slh${i}`) * 4} fill="#FFF4DC" opacity={0.25 * a} />);
  }
  return <g>{out}</g>;
};

/**
 * OVERCLOCK: the pad's boost trails behind her in three ribbons for the rest of the run (the map's
 * pad, 1.5x speed for 2.2 s, docs/Ilalim_Ng_Tulay.md § 4.2), chartreuse, magenta and gold.
 */
const Overclock: React.FC<{ v: View; f: number; hist: V3[] }> = ({ v, f, hist }) => {
  const a = env(f, PB.scoop + 4, PB.scoop + 8, PB.tag - 4, PB.tag + 2);
  if (a <= 0.01 || hist.length < 2) return null;
  const cols = ['#C8F040', '#F050C8', '#F8C83A'];
  return (
    <g opacity={a}>
      {[0.45, 0.95, 1.4].map((h, i) => {
        const d = line(v, hist.map(([x, , z]) => [x, h, z] as V3));
        const w = Math.max(2, 0.07 * scaleAt(v, hist[hist.length - 1]));
        return <path key={i} d={d} stroke={cols[i]} strokeWidth={w} fill="none" opacity={0.8} strokeLinecap="round" />;
      })}
    </g>
  );
};

// ------------------------------------------------------------------------------------ the train

/**
 * The train over the street from the pavement's point of view: under the deck there is no train to
 * see, only what it does to the street. Its shadow sweeps in ahead of it (the map's warning, § 3.5),
 * and its windows throw a strobe of light down onto the pavements and shopfronts beside the deck.
 */
export const TrainLight: React.FC<{ v: View; nose: number | null; f: number }> = ({ v, nose }) => {
  if (nose === null) return null;
  const out: React.ReactNode[] = [];
  for (const sx of [-1, 1]) {
    for (let k = 0; k < 12; k++) {
      const z = nose - 0.8 - k * 1.3;
      if (Math.abs(z) > 40) continue;
      const x0 = sx * (DECK.half + 0.3);
      const x1 = sx * (DECK.half + 4.6);
      const d = poly(v, [[x0, 0.22, z], [x1, 0.22, z - 0.4], [x1, 0.22, z - 1.2], [x0, 0.22, z - 0.8]]);
      if (d) out.push(<path key={`${sx}${k}`} d={d} fill="#FFE4A0" opacity={0.28} />);
    }
  }
  return <g>{out}</g>;
};

// ------------------------------------------------------------------------------------ the blink

/**
 * `VfxShapes.Rift`: a torn vertical sheet, two ragged edges pushed apart by a width that swells in
 * the middle; only the EDGES are drawn, so the street shows through the tear. No flourish: it is the
 * one thing in her kit with none.
 */
export const Rift: React.FC<{ v: View; at: V3; open: number; fade: number; seed: string }> = ({ v, at, open, fade, seed }) => {
  if (open <= 0.01 || fade <= 0.01) return null;
  const H = 2.3;
  const edge = (side: number) => {
    const pts: V3[] = [];
    for (let i = 0; i <= 14; i++) {
      const t = i / 14;
      const swell = Math.sin(t * Math.PI);
      const bite = (random(`${seed}${side}${i}`) - 0.5) * 0.16;
      pts.push([at[0] + side * (0.42 * swell * open + bite * swell), at[1] + 0.05 + t * H, at[2]]);
    }
    return line(v, pts);
  };
  const w = Math.max(2, 0.05 * scaleAt(v, at));
  return <Glow d={edge(-1) + edge(1)} w={w} c={WITCH_BRIGHT} a={fade} />;
};

/** Shreds of the tear flying off its edges: small blocky squares. */
const Shreds: React.FC<{ v: View; at: V3; t: number; seed: string }> = ({ v, at, t, seed }) => {
  if (t <= 0 || t >= 1) return null;
  const ppm = scaleAt(v, at);
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 14; i++) {
    const side = i % 2 ? 1 : -1;
    const [sx, sy] = project(v, [at[0] + side * (0.4 + random(`${seed}v${i}`) * 1.2) * outCubic(t), 0.2 + random(`${seed}y${i}`) * 2 + 0.4 * t, at[2]]);
    const sz = (0.03 + random(`${seed}s${i}`) * 0.05) * ppm * (1 - t);
    out.push(<rect key={i} x={sx - sz / 2} y={sy - sz / 2} width={sz} height={sz} fill={i % 3 ? WITCH_BRIGHT : GOLD} transform={`rotate(${t * 200 + i * 30} ${sx} ${sy})`} />);
  }
  return <g opacity={1 - t}>{out}</g>;
};

const Blinks: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const one = (t0: number, at: V3, seed: string) => (
    <g key={seed}>
      <Rift v={v} at={at} seed={seed} open={kf(f, [[t0 - 3, 0], [t0, 1, outCubic], [t0 + 6, 0.2, inCubic]])} fade={env(f, t0 - 3, t0 - 1, t0 + 3, t0 + 10)} />
      <Shreds v={v} at={at} t={(f - t0) / 14} seed={seed} />
    </g>
  );
  return <g>{one(PB.blink, [TAG.x, 0, TAG.z], 'bl1')}{one(PB.safe - 1, [SAFE.x, 0, SAFE.z], 'bl2')}</g>;
};

/** A curl of orchid smoke left in Nemu's closed hand. */
const Smoke: React.FC<{ v: View; f: number; n: NemuFrame }> = ({ v, f, n }) => {
  const a = env(f, PB.blink, PB.blink + 3, PB.deadpan, PB.deadpan + 20);
  if (a <= 0.01) return null;
  const hand: V3 = [n.x - 0.35, n.y + 1.25, n.z - 0.45];
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 7; i++) {
    const ph = ((f - PB.blink) / 40 + i / 7) % 1;
    const [sx, sy] = project(v, [hand[0] + 0.12 * Math.sin(ph * 7 + i), hand[1] + ph * 0.8, hand[2]]);
    const sz = (0.05 + ph * 0.12) * scaleAt(v, hand);
    out.push(<rect key={i} x={sx - sz / 2} y={sy - sz / 2} width={sz} height={sz} fill={i % 2 ? WITCH : '#C8A0D8'} opacity={0.5 * (1 - ph)} transform={`rotate(${ph * 90} ${sx} ${sy})`} />);
  }
  return <g opacity={a}>{out}</g>;
};

// ------------------------------------------------------------------------------------ Grand Coven

/**
 * ⚠️⚠️ THE CIRCLE IS THE GAME'S OWN INSCRIPTION, TRANSCRIBED, NOT INVENTED FOR THE LOOP.
 * `HeroHazards.CovenCircleBuild.BuildRings` places it BY HAND as a table (🧑 2026-08-27: *"manually
 * draw it instead of using some for loop"*, because a loop over even angles can only make a hubcap):
 * seven unevenly spaced rules, ticks over two thirds of the outer band, four arcs of script that start
 * and stop, nine medallions of four sizes (one overlapping pair), five spokes that do not all meet a
 * medallion, three star figures at three rotations, and floating glyphs. Pink and violet, never gold
 * (*"I DONT WANt GOLd on dark i WANT PURPE OR PINK"*); the corona overhead stays gold. It is drawn IN
 * STAGES, because the staging is the feature (*"they see the circles being constructed"*). Radius is
 * the game's `Reach`, 10.5 m, centred on her, so it covers the whole box and both pavements.
 */
const R = 10.5;
type El = { stage: number; draw: (v: View, c: V3, t: number, spin: number, a: number) => React.ReactNode };

const RULES: [number, string, number][] = [[1.0, WITCH_BRIGHT, 0.72], [0.958, WITCH_BRIGHT, 0.62], [0.742, PALE, 0.55], [0.706, PALE, 0.45], [0.512, DEEP, 0.52], [0.238, WITCH_BRIGHT, 0.66], [0.15, PALE, 0.74]];
const TICKS: [number, number, number][] = [[30, 200, 26], [240, 350, 16]];
const SCRIPT: [number, number, number, number, number, string][] = [[18, 168, 17, 0.976, 0.05, 's1401'], [196, 338, 15, 0.976, 0.05, 's1601'], [104, 286, 14, 0.724, 0.042, 's2203'], [0, 360, 11, 0.194, 0.028, 's3307']];
const MEDALLIONS: [number, number, number, number, number, string][] = [[92, 0.872, 0.15, 7, 3, WITCH_BRIGHT], [140, 0.846, 0.092, 0, 0, PALE], [176, 0.884, 0.128, 5, 2, WITCH_BRIGHT], [188, 0.806, 0.078, 3, 1, DEEP], [232, 0.858, 0.116, 6, 2, PALE], [276, 0.878, 0.148, 8, 3, WITCH_BRIGHT], [312, 0.822, 0.084, 0, 0, DEEP], [348, 0.866, 0.108, 4, 1, PALE], [40, 0.836, 0.096, 3, 1, WITCH_BRIGHT]];
const SPOKES: [number, number, number, string][] = [[92, 0.238, 0.72, WITCH_BRIGHT], [176, 0.238, 0.756, WITCH_BRIGHT], [276, 0.238, 0.73, WITCH_BRIGHT], [16, 0.512, 0.706, DEEP], [212, 0.512, 0.706, DEEP]];
const FIGURES: [number, number, number, number, string][] = [[8, 3, 0.672, 14, WITCH_BRIGHT], [6, 2, 0.448, -27, DEEP], [3, 1, 0.212, 63, PALE]];

const star = (c: V3, r: number, n: number, k: number, rot: number): V3[] => {
  const out: V3[] = [];
  for (let i = 0; i <= n; i++) {
    const a = rot + ((i * k) % n) * ((Math.PI * 2) / n);
    out.push([c[0] + Math.cos(a) * r, 0.025, c[2] + Math.sin(a) * r]);
  }
  return out;
};

const polar = (c: V3, deg: number, frac: number, spin: number): V3 => {
  const a = (deg * Math.PI) / 180 + spin;
  return [c[0] + Math.cos(a) * R * frac, 0.025, c[2] + Math.sin(a) * R * frac];
};

const lw = (v: View, c: V3, m: number) => Math.max(1.2, (m * v.f) / Math.max(2, depth(v, c)));

const ELEMENTS: El[] = [
  ...RULES.map(([fr, col, al], i): El => ({
    stage: i < 2 ? 0 : i < 5 ? 1 : 2,
    draw: (v, c, t, spin, a) => <Glow key={`r${i}`} d={line(v, partial(ring(c, R * fr, spin, spin + Math.PI * 2, 96, 0.02), t))} w={lw(v, c, 0.07)} c={col} a={a * (0.5 + al * 0.6)} core={i === 0 || i === 5} />,
  })),
  ...TICKS.map(([a0, a1, n], i): El => ({
    stage: 3,
    draw: (v, c, t, spin, a) => {
      const pts: string[] = [];
      const m = Math.floor(n * t);
      for (let q = 0; q < m; q++) {
        const d = a0 + ((a1 - a0) * q) / (n - 1);
        pts.push(line(v, [polar(c, d, 0.958, spin), polar(c, d, 1.0, spin)]));
      }
      return <Glow key={`t${i}`} d={pts.join('')} w={lw(v, c, 0.05)} c={WITCH_BRIGHT} a={a * 0.8} core={false} />;
    },
  })),
  ...SCRIPT.map(([a0, a1, n, fr, sz, seed], i): El => ({
    stage: 4,
    draw: (v, c, t, spin, a) => {
      const d: string[] = [];
      const m = Math.floor(n * t);
      for (let q = 0; q < m; q++) {
        const deg = a0 + ((a1 - a0) * (q + 0.5)) / n;
        const ctr = polar(c, deg, fr, spin);
        const ang = (deg * Math.PI) / 180 + spin;
        const up: [number, number] = [Math.cos(ang), Math.sin(ang)];
        const side: [number, number] = [-up[1], up[0]];
        for (const st of rune(`${seed}${q}`)) d.push(line(v, st.map(([u, w]) => [ctr[0] + (side[0] * u + up[0] * w) * R * sz, 0.025, ctr[2] + (side[1] * u + up[1] * w) * R * sz] as V3)));
      }
      return <Glow key={`s${i}`} d={d.join('')} w={lw(v, c, 0.045)} c={i === 2 ? PALE : WITCH_BRIGHT} a={a * 0.85} core={false} />;
    },
  })),
  ...MEDALLIONS.map(([deg, fr, sz, pts, skip, col], i): El => ({
    stage: 5,
    draw: (v, c, t, spin, a) => {
      const ctr = polar(c, deg, fr, spin);
      const r = R * sz * outBack(Math.min(1, t * 1.2));
      let d = line(v, ring(ctr, Math.max(0.01, r), 0, Math.PI * 2, 32, 0.025));
      if (pts >= 3 && t > 0.4) d += line(v, star(ctr, r * 0.78, pts, skip, spin * 2 + deg));
      return <Glow key={`m${i}`} d={d} w={lw(v, c, 0.05)} c={col} a={a} core={false} />;
    },
  })),
  ...SPOKES.map(([deg, r0, r1, col], i): El => ({
    stage: 6,
    draw: (v, c, t, spin, a) => <Glow key={`p${i}`} d={line(v, partial([polar(c, deg, r0, spin), polar(c, deg, r1, spin)], t))} w={lw(v, c, 0.05)} c={col} a={a} core={false} />,
  })),
  ...FIGURES.map(([n, k, fr, rot, col], i): El => ({
    stage: 7,
    draw: (v, c, t, spin, a) => <Glow key={`f${i}`} d={line(v, partial(star(c, R * fr, n, k, (rot * Math.PI) / 180 - spin * (i === 1 ? 1.6 : 0.6)), t))} w={lw(v, c, 0.06)} c={col} a={a} core={i !== 1} />,
  })),
];
const STAGES = 8;

/** 0..1 how far the inscription has built, 0..1 how bright it is, and its slow turn. */
export const covenAt = (f: number) => {
  const build = clamp01((f - PB.build) / 58);
  const bright = kf(f, [[PB.build, 1], [PB.stamp - 2, 1], [PB.stamp + 2, 1.6], [PB.stamp + 12, 1], [PB.descend, 1], [PB.descend + 60, 0.45], [PB.dawn, 0.4], [PB.dawn + 70, 0]]);
  const spin = f > PB.build ? ((f - PB.build) / LOOP) * Math.PI * 2 * 1.5 : 0;
  // At the dawn its rings fold back into the chalk: the radius draws in as it fades.
  const shrink = kf(f, [[PB.dawn, 1], [PB.dawn + 70, 0.66, outQuad]]);
  return { build: f >= PB.build && f < PB.dawn + 72 ? build : 0, bright, spin, shrink };
};

export const Coven: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const cv = covenAt(f);
  if (cv.build <= 0 || cv.bright <= 0.01) return null;
  const c: V3 = [CAST.x, 0, CAST.z];
  const out: React.ReactNode[] = [];
  for (const el of ELEMENTS) {
    const t = clamp01(cv.build * STAGES - el.stage * 0.82);
    if (t <= 0) continue;
    out.push(el.draw(v, c, outQuad(Math.min(1, t)), cv.spin, Math.min(1, cv.bright)));
  }
  const glow = Math.max(0, cv.bright - 1);
  return (
    <g transform={cv.shrink < 1 ? undefined : undefined}>
      {glow > 0 && <path d={poly(v, ring(c, R * 1.02, 0, Math.PI * 2, 48, 0.02))} fill={WITCH} opacity={0.25 * glow} filter="url(#glowBig)" />}
      <g opacity={cv.shrink < 1 ? cv.shrink * 1.2 - 0.2 : 1}>{out}</g>
    </g>
  );
};

/**
 * Twenty glyphs rising out of the circle, every one a different letter (the game's count: *"less
 * glyphs ... BUt theyre all different like 20 or so"*), standing up to face the lens as they float.
 */
const Floaters: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const t0 = PB.build + 40;
  if (f < t0 || f > PB.dawn + 60) return null;
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 20; i++) {
    const start = t0 + i * 3;
    const life = 150 + random(`fl${i}l`) * 120;
    const u = (f - start) / life;
    if (u <= 0) continue;
    const uu = u % 1;
    const deg = random(`fla${i}`) * 360;
    const fr = 0.3 + random(`flr${i}`) * 0.65;
    const base = polar([CAST.x, 0, CAST.z], deg, fr, 0);
    const at: V3 = [base[0], 0.3 + uu * 5.5, base[2]];
    const [sx, sy] = project(v, at);
    if (depth(v, at) < 0.8) continue;
    const s = 0.28 * scaleAt(v, at);
    const a = Math.sin(uu * Math.PI) * (u > 1 ? 0.6 : 1) * (f > PB.dawn ? 1 - (f - PB.dawn) / 60 : 1);
    const d = rune(`glyph${i}`).map((st) => st.map(([x, y], k) => `${k ? 'L' : 'M'}${(sx + x * s).toFixed(1)} ${(sy + y * s).toFixed(1)}`).join('')).join('');
    out.push(<Glow key={i} d={d} w={Math.max(1.5, s * 0.1)} c={i % 3 ? WITCH_BRIGHT : PALE} a={a} core={false} />);
  }
  return <g>{out}</g>;
};

/**
 * `VfxShapes.Corona` hung face-down under the deck over her: a ring of tapering gold teeth round an
 * empty middle, the eclipse of Grand Coven (`SpawnGrandCovenEclipse`, 11 m up, "gold, with no
 * magenta in it").
 */
const Corona: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const a = env(f, PB.build - 4, PB.build + 20, PB.descend, PB.descend + 60) * 0.9 + 0.3 * env(f, PB.descend + 30, PB.descend + 60, PB.dawn, PB.dawn + 40);
  if (a <= 0.01) return null;
  const c: V3 = [CAST.x, DECK.under - 0.7, CAST.z];
  const teeth = 22;
  const rot = (f / LOOP) * Math.PI * 2;
  const r0 = 2.2;
  const r1 = 3.4 + 0.2 * loopSin(f, 30);
  const out: string[] = [];
  for (let i = 0; i < teeth; i++) {
    const t = (i / teeth) * Math.PI * 2 + rot;
    const w = (Math.PI / teeth) * 0.55;
    const len = r0 + (r1 - r0) * (i % 2 ? 0.6 : 1);
    const d = poly(v, [[c[0] + Math.cos(t - w) * r0, c[1], c[2] + Math.sin(t - w) * r0], [c[0] + Math.cos(t) * len, c[1], c[2] + Math.sin(t) * len], [c[0] + Math.cos(t + w) * r0, c[1], c[2] + Math.sin(t + w) * r0]]);
    if (d) out.push(d);
  }
  const d = out.join('');
  return (
    <g opacity={a}>
      <path d={d} fill={GOLD} opacity={0.5} filter="url(#glow)" />
      <path d={d} fill="#FFD870" />
    </g>
  );
};

/** The stamp: a shockwave running out along the road, and a flash. */
const Shock: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const t = (f - PB.stamp) / 16;
  if (t <= 0 || t >= 1) return null;
  const d = line(v, ring([CAST.x, 0, CAST.z], 0.5 + 15 * outCubic(t), 0, Math.PI * 2, 80, 0.04));
  return <Glow d={d} w={Math.max(3, 30 * (1 - t))} c={WITCH_BRIGHT} a={1 - t} />;
};

/** The curse closing on Nemu: two turning rings round her and a band of runes between them. */
const Shackle: React.FC<{ v: View; f: number; n: NemuFrame }> = ({ v, f, n }) => {
  if (n.cursed <= 0.01) return null;
  const c: V3 = [n.x, 0, n.z];
  const spin = (f - PB.stamp) * 0.05;
  const close = outBack(Math.min(1, (f - PB.stamp) / 8));
  const d: string[] = [];
  for (const [h, r] of [[n.y + 0.35, 0.55], [n.y + 1.05, 0.5]] as [number, number][]) d.push(line(v, ring(c, r * (2 - close), spin, spin + Math.PI * 2, 36, h)));
  const w = Math.max(1.5, 0.03 * scaleAt(v, c));
  return <Glow d={d.join('')} w={w} c={WITCH_BRIGHT} a={n.cursed} />;
};

// ------------------------------------------------------------------------------------ assembly

export type FxProps = { f: number; v: View; p: HerFrame; n: NemuFrame; hist: V3[]; hand: { at: [number, number]; s: number } | null; canAt: V3 };

/** On the road, under the figures. */
export const FxGround: React.FC<FxProps> = ({ f, v, canAt: can }) => (
  <g>
    <Coven v={v} f={f} />
    <Shock v={v} f={f} />
    <SoundRings v={v} f={f} t0={PB.klang} at={can} />
    <SoundRings v={v} f={f} t0={PB.stamp} at={[CAST.x, 0, CAST.z]} big={3} />
  </g>
);

/** Over the figures. */
export const FxFront: React.FC<FxProps> = ({ f, v, n, hist, hand }) => (
  <g>
    {hand && <PalmHex f={f} at={hand.at} s={hand.s} />}
    <Trail v={v} f={f} />
    <Overclock v={v} f={f} hist={hist} />
    <Blinks v={v} f={f} />
    <Smoke v={v} f={f} n={n} />
    <Shackle v={v} f={f} n={n} />
    <Floaters v={v} f={f} />
    <Corona v={v} f={f} />
  </g>
);

/** Screen-space words and lines, over everything. */
export const FxScreen: React.FC<{ f: number; v: View; can: V3; kuroTop: [number, number] | null; kuroBang: number; heads: Heads }> = ({ f, v, can, kuroTop, kuroBang, heads }) => {
  const [cx, cy] = project(v, [can[0] - 0.8, 1.8, can[2]]);
  return (
    <g>
      <SpeedLines f={f} />
      <Marks f={f} heads={heads} />
      <Word text="KLANG!" f={f} t0={PB.klang + 2} x={Math.min(1500, Math.max(420, cx))} y={Math.max(260, cy)} s={1} fill="#FFF0CE" edge={WITCH} life={38} />
      {kuroTop && kuroBang > 0.01 && (
        <g transform={`translate(${kuroTop[0] + 30} ${kuroTop[1] - 20}) scale(${0.6 + 0.4 * outBack(Math.min(1, kuroBang))})`} opacity={kuroBang}>
          <text x={5} y={8} fontFamily="Darumadrop One" fontSize={150} textAnchor="middle" fill="#2A0620">!</text>
          <text x={0} y={0} fontFamily="Darumadrop One" fontSize={150} textAnchor="middle" fill="#FFF0CE" stroke={DEEP} strokeWidth={9} paintOrder="stroke">!</text>
        </g>
      )}
    </g>
  );
};

/**
 * ⚠️ COMIC MARKS BESIDE THE HEAD, NEVER ON THE FACE. 🧑: *"make her more expressive"*, and the rule
 * that faces are the models' own (docs/HOME_SCREEN_ANIMATION_METHOD.md § 2.1). Blocky characters in
 * manga and cartoons carry feeling in marks drawn NEXT to them, and these sit beside her hat and
 * Nemu's head: sparkles when Phaister shows off, a sweat drop when Nemu does not react, an anger vein at
 * the stomp; a slow "..." from Nemu after each attempt. Hand-drawn shapes, never a font.
 */
export type Heads = { her: [number, number, number] | null; nemu: [number, number, number] | null };

const Sparkles: React.FC<{ x: number; y: number; s: number; t: number; seed: string }> = ({ x, y, s, t, seed }) => {
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 3; i++) {
    const u = t * 1.4 - i * 0.18;
    if (u <= 0 || u >= 1) continue;
    const px = x + (random(`${seed}x${i}`) - 0.3) * s * 1.4;
    const py = y - (0.2 + random(`${seed}y${i}`) * 0.6) * s;
    const r = s * (0.12 + 0.08 * random(`${seed}r${i}`)) * Math.sin(u * Math.PI);
    const d = [0, 1, 2, 3, 4, 5, 6, 7].map((k) => { const a = (k * Math.PI) / 4; const rr = k % 2 ? r * 0.28 : r; return `${k ? 'L' : 'M'}${(px + Math.cos(a) * rr).toFixed(1)} ${(py + Math.sin(a) * rr).toFixed(1)}`; }).join('') + 'Z';
    out.push(<path key={i} d={d} fill="#FFF3C8" stroke="#E8A020" strokeWidth={Math.max(1, s * 0.012)} />);
  }
  return <g>{out}</g>;
};

const Sweat: React.FC<{ x: number; y: number; s: number; t: number }> = ({ x, y, s, t }) => {
  if (t <= 0 || t >= 1) return null;
  const py = y + s * 0.25 * t;
  const w = s * 0.09;
  const d = `M${x} ${py - w * 1.8}Q${x + w} ${py - w * 0.2} ${x + w * 0.7} ${py + w * 0.5}A${w * 0.72} ${w * 0.72} 0 1 1 ${x - w * 0.7} ${py + w * 0.5}Q${x - w} ${py - w * 0.2} ${x} ${py - w * 1.8}Z`;
  return <path d={d} fill="#E8F4F0" stroke="#7A8A88" strokeWidth={Math.max(1, s * 0.012)} opacity={Math.min(1, 4 * (1 - t))} />;
};

const Vein: React.FC<{ x: number; y: number; s: number; t: number }> = ({ x, y, s, t }) => {
  if (t <= 0 || t >= 1) return null;
  const k = s * 0.08 * (1 + 0.25 * Math.sin(t * 40));
  const arc = (a: number) => {
    const cx = x + Math.cos(a) * k * 1.3;
    const cy = y + Math.sin(a) * k * 1.3;
    return `M${cx + Math.cos(a + 2.2) * k} ${cy + Math.sin(a + 2.2) * k}Q${cx} ${cy} ${cx + Math.cos(a - 2.2) * k} ${cy + Math.sin(a - 2.2) * k}`;
  };
  const d = [0.8, 2.37, 3.94, 5.5].map(arc).join('');
  return <path d={d} stroke="#E8283A" strokeWidth={Math.max(2, s * 0.03)} fill="none" strokeLinecap="round" opacity={Math.min(1, 5 * (1 - t))} />;
};

const Dots: React.FC<{ x: number; y: number; s: number; t: number }> = ({ x, y, s, t }) => {
  if (t <= 0 || t >= 1) return null;
  const a = Math.min(1, 6 * (1 - t));
  return (
    <g opacity={a}>
      {[0, 1, 2].map((i) => (t * 3.2 > i * 0.8 ? <rect key={i} x={x + i * s * 0.16} y={y} width={s * 0.07} height={s * 0.07} fill="#FFF4E6" stroke="#3A2A36" strokeWidth={Math.max(1, s * 0.012)} /> : null))}
    </g>
  );
};

export const Marks: React.FC<{ f: number; heads: Heads }> = ({ f, heads }) => {
  const out: React.ReactNode[] = [];
  const P = PB;
  const win = (a: number, b: number) => (f >= a && f < b ? (f - a) / (b - a) : -1);
  if (heads.her) {
    const [x, y, s] = heads.her;
    const side = x + s * 0.55;
    for (const [a, b, k] of [[P.boast + 22, P.blind, 'bo'], [P.tada, P.tada + 34, 'td'], [P.safe + 4, P.safe + 30, 'sf'], [P.clap, P.tipK + 24, 'cl']] as [number, number, string][]) {
      const t = win(a, b);
      if (t >= 0) out.push(<Sparkles key={k} x={side} y={y} s={s} t={t} seed={k} />);
    }
    for (const [a, b] of [[P.sag + 8, P.run - 4], [P.deadpan + 20, P.stomp - 2]]) {
      const t = win(a, b);
      if (t >= 0) out.push(<Sweat key={`sw${a}`} x={x - s * 0.5} y={y + s * 0.3} s={s} t={t} />);
    }
    const tv = win(P.stomp, P.resolve + 6);
    if (tv >= 0) out.push(<Vein key="vein" x={side} y={y + s * 0.1} s={s} t={tv} />);
  }
  if (heads.nemu) {
    const [x, y, s] = heads.nemu;
    for (const [a, b] of [[P.klang + 14, P.run + 10], [P.deadpan + 12, P.stomp + 10], [P.nothing + 8, P.kuro - 2]]) {
      const t = win(a, b);
      if (t >= 0) out.push(<Dots key={`d${a}`} x={x + s * 0.45} y={y - s * 0.1} s={s} t={t} />);
    }
  }
  return <g>{out}</g>;
};

/**
 * The one impact frame, on KLANG!: three frames of flat plum with her burst behind the can, the
 * can drawn over it in white. It is the only one in her loop (Zack's has two); the rest of her power
 * is shown, not flashed.
 */
export const impactAt = (f: number) => (f >= PB.klang && f < PB.klang + 3 ? 1 : 0);

export const Burst: React.FC<{ x: number; y: number; s: number }> = ({ x, y, s }) => {
  const pts: string[] = [];
  for (let i = 0; i < 24; i++) {
    const a = (i / 24) * Math.PI * 2;
    const r = (i % 2 ? 0.45 : 1) * s * (0.85 + 0.3 * random(`bu${i}`));
    pts.push(`${i ? 'L' : 'M'}${(x + Math.cos(a) * r).toFixed(1)} ${(y + Math.sin(a) * r).toFixed(1)}`);
  }
  return <path d={pts.join('') + 'Z'} fill={WITCH_BRIGHT} />;
};

export { env };
