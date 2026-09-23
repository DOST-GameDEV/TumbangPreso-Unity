import React from 'react';
import { random } from 'remotion';
import { clamp01, env, onN } from '../lib/time';
import { easeInOut } from '../lib/time';
import { kf, inCubic, outBack, outCubic } from '../lib/kf';
import { PB, pt } from './beats';
import { CAN, CanFrame, HerFrame, HOME, KNOCKS, REVEAL, sigilDraw, Stage } from './perform';
import { project, projectDir, scaleAt, V3, View } from './view';
import { DECK, MOON, TRAIN } from './set';

/*
 * Her effects, in the shapes her kit already has in the game (Runtime/Visual/VfxShapes.cs), so the
 * loop and a match speak one language:
 *   HEX          `WardCircle`: ruled rings, a band of written cells, two squares 45 degrees apart, a
 *                triangle that breaks the symmetry, four medallions. Drawn by her hand in the air,
 *                then laid on the chalk round the can.
 *   SHADOW BLINK `Rift`: a torn VERTICAL sheet, two ragged edges with the street showing between.
 *                No circle, no smoke ball: the place comes apart and she is gone through it.
 *   GRAND COVEN  `Corona`: a ring of tapering teeth round an empty middle, seen from below. Here it
 *                rims the swallowed moon.
 * Colours are her own: `UiTheme.HeroWitch` e828c5 and `HeroWitchBright` f444d4, "magenta into gold"
 * as VfxShapes puts it, the gold being her hat buckle's. Nothing is blue.
 */

export const WITCH = '#E828C5';
export const WITCH_BRIGHT = '#F444D4';
export const WITCH_CORE = '#FFD2F2';
export const GOLD = '#F8B824';

type Stroke = { pts: [number, number][]; closed: boolean; w: number; at: number; gold?: boolean; ring?: boolean };

const circle = (r: number, n = 72): [number, number][] => Array.from({ length: n }, (_, i) => [Math.cos((i / n) * Math.PI * 2 + Math.PI / 2) * r, Math.sin((i / n) * Math.PI * 2 + Math.PI / 2) * r]);

/** A small written glyph: two or three ruled strokes in a box, seeded, standing upright to face out. */
const rune = (cx: number, cy: number, s: number, a: number, seed: string): [number, number][][] => {
  const out: [number, number][][] = [];
  const n = 2 + Math.floor(random(seed + 'n') * 2);
  const up: [number, number] = [Math.cos(a), Math.sin(a)];
  const side: [number, number] = [-up[1], up[0]];
  const P = (u: number, v: number): [number, number] => [cx + side[0] * u * s + up[0] * v * s, cy + side[1] * u * s + up[1] * v * s];
  for (let k = 0; k < n; k++) {
    const r = (q: string) => Math.round((random(seed + k + q) - 0.5) * 2) * 0.5;
    const vert = random(seed + k + 'o') < 0.55;
    out.push(vert ? [P(r('a'), -0.5), P(r('a'), 0.5)] : [P(-0.5, r('b')), P(0.5, r('b'))]);
  }
  return out;
};

/** `VfxShapes.WardCircle` at unit radius, in the order her hand draws it (`at` 0..1). */
const WARD: Stroke[] = (() => {
  const s: Stroke[] = [];
  const bar = 0.03;
  s.push({ pts: circle(1.0), closed: true, w: bar, at: 0, ring: true });
  s.push({ pts: circle(0.845), closed: true, w: bar, at: 0.08, ring: true });
  const cells = 12;
  for (let c = 0; c < cells; c++) {
    const a = (c / cells) * Math.PI * 2;
    s.push({ pts: [[Math.cos(a) * 0.845, Math.sin(a) * 0.845], [Math.cos(a) * 0.97, Math.sin(a) * 0.97]], closed: false, w: bar * 0.5, at: 0.3 + c * 0.012 });
    const m = ((c + 0.5) / cells) * Math.PI * 2;
    for (const st of rune(Math.cos(m) * 0.91, Math.sin(m) * 0.91, 0.085, m, `ru${c}`)) s.push({ pts: st, closed: false, w: bar * 0.45, at: 0.34 + c * 0.012, gold: c % 3 === 0 });
  }
  const sq = (rot: number, at: number) => s.push({ pts: [0, 1, 2, 3].map((k) => [Math.cos(rot + (k * Math.PI) / 2) * 0.615, Math.sin(rot + (k * Math.PI) / 2) * 0.615]), closed: true, w: bar * 0.55, at });
  sq(0, 0.5);
  sq(Math.PI / 4, 0.58);
  s.push({ pts: [0, 1, 2].map((k) => [Math.cos(Math.PI / 2 + (k * 2 * Math.PI) / 3) * 0.44, Math.sin(Math.PI / 2 + (k * 2 * Math.PI) / 3) * 0.44]), closed: true, w: bar * 0.5, at: 0.66, gold: true });
  s.push({ pts: circle(0.185, 32), closed: true, w: bar * 0.9, at: 0.72, ring: true });
  for (let m = 0; m < 4; m++) {
    const a = (m / 4) * Math.PI * 2 + Math.PI / 4;
    const c: [number, number] = [Math.cos(a) * 0.845, Math.sin(a) * 0.845];
    s.push({ pts: circle(0.105, 20).map(([x, y]) => [x + c[0], y + c[1]]), closed: true, w: bar * 0.6, at: 0.78 + m * 0.03, ring: true, gold: true });
    for (const st of rune(c[0], c[1], 0.08, a, `md${m}`)) s.push({ pts: st, closed: false, w: bar * 0.42, at: 0.8 + m * 0.03 });
  }
  for (const st of rune(0, 0, 0.16, Math.PI / 2, 'hub')) s.push({ pts: st, closed: false, w: bar * 0.6, at: 0.92, gold: true });
  return s;
})();

/** Where the ward is: centre, radius, how far it has tipped from standing (0) to lying (1), spin. */
export type WardState = { C: V3; R: number; tilt: number; spin: number; draw: number; alpha: number; flare: number };

export const wardAt = (f: number): WardState | null => {
  if (f < PB.sigil || f > PB.release + pt(16)) return null;
  // Beside her, where her right hand draws it, never over her face: the first pass hung it in
  // front of her chest and hid her face for the whole Turn.
  const air: V3 = [HOME.x - 0.78, 1.48, HOME.z - 0.25];
  const ground: V3 = [CAN.x, 0.015, CAN.z];
  const drop = kf(f, [[PB.curtain + pt(2), 0], [PB.empty + pt(2), 1, inCubic]]);
  const C: V3 = [air[0] + (ground[0] - air[0]) * drop, air[1] + (ground[1] - air[1]) * drop, air[2] + (ground[2] - air[2]) * drop];
  const land = f >= PB.empty + pt(2) ? env(f, PB.empty + pt(2), PB.empty + pt(3), PB.empty + pt(4), PB.empty + pt(10)) : 0;
  const flare = env(f, PB.klang - 1, PB.klang + 1, PB.klang + 3, PB.klang + pt(10));
  return {
    C,
    R: 0.46 + 0.16 * drop + 0.05 * land + 0.25 * flare,
    tilt: kf(f, [[PB.curtain + pt(2), 0], [PB.empty + pt(2), 1, easeInOut]]),
    spin: (f - PB.sigil) * (0.004 + 0.01 * drop),
    draw: sigilDraw(f),
    alpha: kf(f, [[PB.klang + pt(4), 1], [PB.release + pt(16), 0]]),
    flare: Math.max(flare, land * 0.6, 0.35 * env(f, PB.train, PB.train + pt(8), PB.curtain, PB.curtain + pt(4))),
  };
};

/** A ward-space point to world: the plane stands facing the lens at tilt 0 and lies on the road at 1. */
const wardWorld = (w: WardState, x: number, y: number): V3 => {
  const c = Math.cos(w.spin);
  const s = Math.sin(w.spin);
  const u = (x * c - y * s) * w.R;
  const v = (x * s + y * c) * w.R;
  const t = (w.tilt * Math.PI) / 2;
  return [w.C[0] + u, w.C[1] + v * Math.cos(t), w.C[2] + v * Math.sin(t)];
};

/**
 * The ward, drawn. `half` picks which half to draw, so a ward lying round the can can pass BEHIND
 * it at the back and IN FRONT of it at the front: 'far' is everything beyond the ward's centre.
 */
export const Ward: React.FC<{ v: View; w: WardState; half: 'far' | 'near' }> = ({ v, w, half }) => {
  const ppm = scaleAt(v, w.C);
  const out: React.ReactNode[] = [];
  const glow: string[] = [];
  const core: string[] = [];
  const gold: string[] = [];
  for (const st of WARD) {
    const vis = clamp01((w.draw - st.at) / (st.ring ? 0.22 : 0.06));
    if (vis <= 0) continue;
    const n = st.ring ? Math.max(2, Math.ceil(st.pts.length * vis)) : st.pts.length;
    const pts = st.pts.slice(0, n);
    if (st.closed && vis >= 1) pts.push(st.pts[0]);
    let d = '';
    let pen = false;
    for (const [x, y] of pts) {
      const wp = wardWorld(w, x, y);
      const far = wp[2] > w.C[2] + 1e-4;
      if ((half === 'far') !== far) {
        pen = false;
        continue;
      }
      const [sx, sy] = project(v, wp);
      d += `${pen ? 'L' : 'M'}${sx.toFixed(1)} ${sy.toFixed(1)}`;
      pen = true;
    }
    if (!d) continue;
    (st.gold ? gold : core).push(d);
    glow.push(d);
    void out;
  }
  const bw = Math.max(1.6, 0.03 * w.R * ppm);
  const a = w.alpha;
  return (
    <g opacity={a}>
      <path d={glow.join('')} stroke={WITCH} strokeWidth={bw * (3.2 + 2 * w.flare)} fill="none" opacity={0.45 + 0.4 * w.flare} filter="url(#glow)" strokeLinecap="square" />
      <path d={core.join('')} stroke={WITCH_BRIGHT} strokeWidth={bw} fill="none" strokeLinecap="square" strokeLinejoin="miter" />
      <path d={gold.join('')} stroke={GOLD} strokeWidth={bw} fill="none" strokeLinecap="square" />
      {w.flare > 0.3 && <path d={core.join('')} stroke={WITCH_CORE} strokeWidth={bw * 0.5} fill="none" opacity={(w.flare - 0.3) / 0.7} />}
    </g>
  );
};

// ------------------------------------------------------------------------------------ the blink

/**
 * `VfxShapes.Rift`: a torn vertical sheet. Two ragged edges walked bottom to top, pushed apart by a
 * width that swells in the middle and closes at both ends; only the EDGES are drawn, so the street
 * shows through the tear. `open` 0..1 is how far it has parted, `fade` how much is left of it.
 */
export const Rift: React.FC<{ v: View; at: V3; open: number; fade: number; seed: string }> = ({ v, at, open, fade, seed }) => {
  if (open <= 0.01 || fade <= 0.01) return null;
  const H = 2.3;
  const steps = 14;
  const edge = (side: number) => {
    const pts: string[] = [];
    for (let i = 0; i <= steps; i++) {
      const t = i / steps;
      const swell = Math.sin(t * Math.PI);
      const bite = (random(`${seed}${side}${i}`) - 0.5) * 0.16;
      const x = at[0] + side * (0.42 * swell * open + bite * swell);
      const [sx, sy] = project(v, [x, 0.05 + t * H, at[2]]);
      pts.push(`${i ? 'L' : 'M'}${sx.toFixed(1)} ${sy.toFixed(1)}`);
    }
    return pts.join('');
  };
  const w = Math.max(2, 0.05 * scaleAt(v, at));
  const d = edge(-1) + edge(1);
  return (
    <g opacity={fade}>
      <path d={d} stroke={WITCH} strokeWidth={w * 4} fill="none" opacity={0.5} filter="url(#glow)" />
      <path d={d} stroke={WITCH_BRIGHT} strokeWidth={w} fill="none" strokeLinejoin="miter" />
      <path d={d} stroke={WITCH_CORE} strokeWidth={w * 0.35} fill="none" />
    </g>
  );
};

/** Shreds of the tear: small squares that fly off its edges and fade, blocky and lineless. */
const Shreds: React.FC<{ v: View; at: V3; t: number; seed: string }> = ({ v, at, t, seed }) => {
  if (t <= 0 || t >= 1) return null;
  const ppm = scaleAt(v, at);
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 14; i++) {
    const side = i % 2 ? 1 : -1;
    const y = 0.2 + random(`${seed}y${i}`) * 2;
    const vx = side * (0.4 + random(`${seed}v${i}`) * 1.2);
    const [sx, sy] = project(v, [at[0] + vx * outCubic(t), y + 0.4 * t, at[2]]);
    const sz = (0.03 + random(`${seed}s${i}`) * 0.05) * ppm * (1 - t);
    out.push(<rect key={i} x={sx - sz / 2} y={sy - sz / 2} width={sz} height={sz} fill={i % 3 ? WITCH_BRIGHT : GOLD} transform={`rotate(${t * 200 + i * 30} ${sx} ${sy})`} />);
  }
  return <g opacity={1 - t}>{out}</g>;
};

/** The four blinks of her loop: out of the Turn, into the Prestige, out of it, and home. */
const blinks = (f: number): { at: V3; open: number; fade: number; shred: number; seed: string }[] => {
  const one = (t0: number, at: V3, seed: string) => ({
    at,
    open: kf(f, [[t0 - pt(3), 0], [t0, 1, outCubic], [t0 + pt(6), 0.2, inCubic]]),
    fade: env(f, t0 - pt(3), t0 - pt(1), t0 + pt(3), t0 + pt(10)),
    shred: (f - t0) / pt(14),
    seed,
  });
  return [
    one(PB.curtain + pt(5), [HOME.x, 0, HOME.z], 'b1'),
    one(PB.reveal + pt(1), [REVEAL.x, 0, REVEAL.z], 'b2'),
    one(PB.home + pt(5), [REVEAL.x, 0, REVEAL.z], 'b3'),
    one(PB.home + pt(9), [HOME.x, 0, HOME.z], 'b4'),
  ];
};

// ------------------------------------------------------------------------------------ KLANG

/**
 * ⚠️⚠️ KLANG! IS THE NOISE THAT SAVES THE MOON. In the Visayan telling of the bakunawa, people bang
 * pots and pans until the serpent lets the moon go; on a street court the pot is the lata. So the
 * word is a STRUCK TIN, not an impact: the letters ring (a damped tremble, the way a tin sings after
 * it is hit), rings of sound roll off the can, and a fainter echo of the word rises toward the moon.
 * It is deliberately unlike Zack's TUMP!, which is a golden burst with extruded letters: no burst,
 * no speed lines, no extrusion.
 */
const KLANG = ['K', 'L', 'A', 'N', 'G', '!'];
const Klang: React.FC<{ f: number; x: number; y: number; s: number }> = ({ f, x, y, s }) => {
  const t = f - PB.klang - 2;
  if (t < 0 || t > pt(40)) return null;
  const fade = kf(t, [[pt(28), 1], [pt(40), 0]]);
  const out: React.ReactNode[] = [];
  const size = 150 * s;
  const adv = size * 0.62;
  for (let i = 0; i < KLANG.length; i++) {
    const t0 = i * 1.5;
    if (t < t0) continue;
    const sc = kf(t, [[t0, 0.2], [t0 + 4, 1.18, outBack], [t0 + 9, 1]]);
    // The ring: a tremble that decays, smooth, never a random jitter.
    const ring = Math.exp(-(t - t0) / 14) * Math.sin((t - t0) * 1.9 + i);
    const lx = (i - (KLANG.length - 1) / 2) * adv;
    const ly = -Math.abs(i - 2.5) * 6 * s;
    out.push(
      <g key={i} transform={`translate(${lx} ${ly}) rotate(${(i % 2 ? 5 : -5) + 9 * ring}) scale(${sc})`}>
        <text x={4 * s} y={6 * s} fontFamily="Darumadrop One" fontSize={size} textAnchor="middle" fill="#3A0A2A">{KLANG[i]}</text>
        <text x={0} y={0} fontFamily="Darumadrop One" fontSize={size} textAnchor="middle" fill="#FFF0CE" stroke={WITCH} strokeWidth={7 * s} paintOrder="stroke">{KLANG[i]}</text>
      </g>,
    );
  }
  // The echo, rising toward the moon, fainter and smaller.
  const e = kf(t, [[pt(6), 0], [pt(34), 1]]);
  return (
    <g opacity={fade}>
      <g transform={`translate(${x} ${y})`}>{out}</g>
      {e > 0 && e < 1 && (
        <text x={x + 120 * s + 140 * e} y={y - 120 * s - 260 * e} fontFamily="Darumadrop One" fontSize={size * (0.5 - 0.2 * e)} textAnchor="middle" fill="#FFF0CE" opacity={0.5 * (1 - e)}>KLANG</text>
      )}
    </g>
  );
};

/** Rings of sound rolling off the can, flat on the road, one per strike and one for the landing. */
const SoundRings: React.FC<{ v: View; f: number; c: CanFrame }> = ({ v, f, c }) => {
  const out: React.ReactNode[] = [];
  const at: V3 = [c.x + c.dx, 0.02, c.z];
  for (let k = 0; k < 3; k++) {
    const t = (f - (PB.klang + 2 + k * 4)) / pt(18);
    if (t <= 0 || t >= 1) continue;
    const r = 0.3 + 1.4 * outCubic(t);
    const pts: string[] = [];
    for (let i = 0; i <= 24; i++) {
      const a = (i / 24) * Math.PI * 2;
      const [sx, sy] = project(v, [at[0] + Math.cos(a) * r, 0.02 + 0.25 * t, at[2] + Math.sin(a) * r]);
      pts.push(`${i ? 'L' : 'M'}${sx.toFixed(1)} ${sy.toFixed(1)}`);
    }
    out.push(<path key={k} d={pts.join('')} stroke="#FFF0CE" strokeWidth={4 * (1 - t) + 1} fill="none" opacity={0.7 * (1 - t)} />);
  }
  return <g>{out}</g>;
};

// ------------------------------------------------------------------------------------ the sky

/**
 * `VfxShapes.Corona` round the swallowed moon: tapering teeth round an empty middle. It is her
 * ultimate's own shape, and here it is also the serpent's mouth closed on the moon, which is as far
 * as the serpent is ever drawn.
 */
export const Corona: React.FC<{ v: View; amount: number; f: number }> = ({ v, amount, f }) => {
  if (amount <= 0.01) return null;
  const [cx, cy] = projectDir(v, MOON.az, MOON.el);
  const teeth = 18;
  const out: string[] = [];
  const r0 = MOON.r * 1.18;
  const r1 = MOON.r * (1.55 + 0.1 * Math.sin(f * 0.1));
  const rot = f * 0.004;
  for (let i = 0; i < teeth; i++) {
    const a = (i / teeth) * Math.PI * 2 + rot;
    const w = (Math.PI / teeth) * 0.55;
    const len = r0 + (r1 - r0) * (i % 2 ? 0.6 : 1) * amount;
    out.push(`M${cx + Math.cos(a - w) * r0} ${cy + Math.sin(a - w) * r0}L${cx + Math.cos(a) * len} ${cy + Math.sin(a) * len}L${cx + Math.cos(a + w) * r0} ${cy + Math.sin(a + w) * r0}Z`);
  }
  return (
    <g opacity={amount}>
      <path d={out.join('')} fill={WITCH} opacity={0.5} filter="url(#glow)" />
      <path d={out.join('')} fill={WITCH_BRIGHT} />
    </g>
  );
};

// ------------------------------------------------------------------------------------ light

/** A pool of warm light on the road: the median lamp, and the follow spot during the trick. */
const Pool: React.FC<{ v: View; at: [number, number]; r: number; a: number; colour: string }> = ({ v, at, r, a, colour }) => {
  const pts: string[] = [];
  for (let i = 0; i <= 28; i++) {
    const t = (i / 28) * Math.PI * 2;
    const [sx, sy] = project(v, [at[0] + Math.cos(t) * r, 0.004, at[1] + Math.sin(t) * r]);
    pts.push(`${i ? 'L' : 'M'}${sx.toFixed(1)} ${sy.toFixed(1)}`);
  }
  return <path d={pts.join('')} fill={colour} opacity={a} filter="url(#glowBig)" />;
};

const Shadow: React.FC<{ v: View; at: V3; r: number; a: number }> = ({ v, at, r, a }) => {
  if (a <= 0.01) return null;
  const pts: string[] = [];
  for (let i = 0; i <= 20; i++) {
    const t = (i / 20) * Math.PI * 2;
    const [sx, sy] = project(v, [at[0] + Math.cos(t) * r, 0.005, at[2] + Math.sin(t) * r * 0.8]);
    pts.push(`${i ? 'L' : 'M'}${sx.toFixed(1)} ${sy.toFixed(1)}`);
  }
  return <path d={pts.join('')} fill="#0C0306" opacity={a} filter="url(#haze)" />;
};

// ------------------------------------------------------------------------------------ layers

export type FxProps = { f: number; v: View; p: HerFrame; c: CanFrame; st: Stage; trail: [number, number][]; doodle: [number, number][] };

/** On the road, under the figures. */
export const FxBack: React.FC<FxProps> = ({ f, v, p, c, st }) => {
  const w = wardAt(f);
  const canAt: V3 = [c.x + c.dx, 0, c.z];
  return (
    <g>
      <Pool v={v} at={[st.spot[0], st.spot[1]]} r={st.spot[2]} a={0.2 + 0.25 * st.theatre} colour="#FFC98A" />
      <Shadow v={v} at={[p.x, 0, p.z]} r={0.42 * (1 - 0.5 * p.fold)} a={0.5 * p.visible} />
      <Shadow v={v} at={canAt} r={0.2} a={0.45 * clamp01(1 - c.hop * 3)} />
      {w && <Ward v={v} w={w} half="far" />}
      <StageMark v={v} f={f} />
      <WindowLight v={v} head={st.train} />
    </g>
  );
};

/** In front of the figures. */
export const Fx: React.FC<FxProps> = ({ f, v, p, c, st, trail, doodle }) => {
  const w = wardAt(f);
  const out: React.ReactNode[] = [];
  if (w) out.push(<Ward key="ward" v={v} w={w} half="near" />);
  for (const b of blinks(f)) {
    out.push(<Rift key={`r${b.seed}`} v={v} at={b.at} open={b.open} fade={b.fade} seed={b.seed} />);
    out.push(<Shreds key={`s${b.seed}`} v={v} at={b.at} t={b.shred} seed={b.seed} />);
  }
  // Her hand's light: the path it took over the last frames, as a comet.
  if (trail.length > 1) {
    const d = trail.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join('');
    out.push(<path key="tr" d={d} stroke={WITCH} strokeWidth={16} fill="none" opacity={0.45} filter="url(#glow)" strokeLinecap="round" />);
    out.push(<path key="tr2" d={d} stroke={WITCH_BRIGHT} strokeWidth={5} fill="none" strokeLinecap="round" strokeLinejoin="round" />);
  }
  // The doodle: the little loop she idly draws, fading as it closes.
  if (doodle.length > 1) {
    const d = doodle.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join('');
    out.push(<path key="dd" d={d} stroke={WITCH} strokeWidth={10} fill="none" opacity={0.35} filter="url(#glow)" strokeLinecap="round" />);
    out.push(<path key="dd2" d={d} stroke={WITCH_BRIGHT} strokeWidth={3} fill="none" strokeLinecap="round" strokeLinejoin="round" opacity={0.9} />);
  }
  // The raps: two little strike marks off the lid.
  for (let k = 0; k < 2; k++) {
    const t = f - PB.rap - KNOCKS[k];
    if (t < 0 || t > 8) continue;
    const [sx, sy] = project(v, [c.x, 0.66, c.z]);
    for (let i = 0; i < 3; i++) {
      const a = -Math.PI / 2 + (i - 1) * 0.7;
      out.push(<rect key={`k${k}${i}`} x={sx + Math.cos(a) * (18 + t * 3)} y={sy + Math.sin(a) * (18 + t * 3)} width={10} height={4} fill="#FFF0CE" opacity={1 - t / 8} transform={`rotate(${(a * 180) / Math.PI} ${sx + Math.cos(a) * (18 + t * 3)} ${sy + Math.sin(a) * (18 + t * 3)})`} />);
    }
  }
  out.push(<SoundRings key="snd" v={v} f={f} c={c} />);
  // To the can's left, clear of her behind it: the first pass lettered it straight over her face.
  const [kx, ky] = project(v, [CAN.x - 1.7, 1.25, CAN.z + 0.4]);
  out.push(<Klang key="klang" f={f} x={kx} y={ky} s={scaleAt(v, [CAN.x, 0, CAN.z]) / 240} />);
  void p;
  void st;
  void onN;
  return <g>{out}</g>;
};

/** The small ward she sets under the can when she puts the stage back. */
const StageMark: React.FC<{ v: View; f: number }> = ({ v, f }) => {
  const t0 = PB.home + pt(10);
  const a = env(f, t0, t0 + pt(4), t0 + pt(16), t0 + pt(30));
  if (a <= 0) return null;
  const w: WardState = { C: [CAN.x, 0.012, CAN.z], R: 0.5, tilt: 1, spin: (f - t0) * 0.02, draw: kf(f, [[t0, 0], [t0 + pt(14), 1]]), alpha: a, flare: 0.3 };
  return (
    <g>
      <Ward v={v} w={w} half="far" />
      <Ward v={v} w={w} half="near" />
    </g>
  );
};

/**
 * The train's windows throwing their light down across the road as it passes: one pale block per
 * window, sliding toward the lens with the car above it. This is the curtain the trick hides behind,
 * so it has to be felt on the street, not only seen on the guideway.
 */
const WindowLight: React.FC<{ v: View; head: number }> = ({ v, head }) => {
  if (head > 120) return null;
  const out: React.ReactNode[] = [];
  for (let c = 0; c < 3; c++) {
    const za = head + c * (TRAIN.car + TRAIN.gap);
    for (let k = 0; k < 7; k++) {
      const w0 = za + 0.9 + k * 2.1 - 1.2;
      const w1 = w0 + 1.5;
      if (w1 < 1 || w0 > 45) continue;
      const near = Math.max(1, w0);
      out.push(<path key={`${c}${k}`} d={polyRoad(v, [[DECK.right + 0.4, near], [3.2, near - 0.8], [3.2, w1 - 0.8], [DECK.right + 0.4, w1]])} fill="#FFE4A0" opacity={0.2 * clamp01((45 - w0) / 20)} />);
    }
  }
  return <g filter="url(#haze)">{out}</g>;
};

const polyRoad = (v: View, pts: [number, number][]) =>
  pts.map(([x, z], i) => {
    const [sx, sy] = project(v, [x, 0.006, z]);
    return `${i ? 'L' : 'M'}${sx.toFixed(1)} ${sy.toFixed(1)}`;
  }).join('') + 'Z';
