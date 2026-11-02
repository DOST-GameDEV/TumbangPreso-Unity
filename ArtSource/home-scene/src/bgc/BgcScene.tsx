import React, { useEffect, useState } from 'react';
import { AbsoluteFill, continueRender, delayRender, Img, random, staticFile, useCurrentFrame } from 'remotion';
import { Defs } from '../lib/Defs';
import { Bolt, boltPoints, Crackle, Pt } from '../lib/bolt';
import { kf } from '../lib/kf';
import { env, INTRO, IDLE, ph } from './time';
import { B } from './beats';
import { ActorImage, drawActor, EYE_L, EYE_R, Light, useActor } from '../three/actor';
import { apparent, depth, line, PERSON_SCALE, poly, project, scaleAt, V3, View } from '../phaister/view';
import { setNight, Item } from '../phaister/set';
import { burstPath } from '../art/burst';
import { camAt, panSpeed } from './camera';
import { echoAt, curveAt, flatHead, flyingAt, FLAT, Frame, LINE, momentum, POST, PULL, pullAt, pullU, pullWhen, STOP, rago, STAND, zack } from './perform';
import { EYES_CATCH, IDLE_SPARKS, impactAt, litAt } from './light';
import { BLEND, plateAt, preAt } from './plates';

/** Zack's hit: electric yellow on ink, white at the heart. */
const Burst: React.FC<{ x: number; y: number; s: number }> = ({ x, y, s }) => (
  <g transform={`translate(${x} ${y})`}>
    <path d={burstPath(14, s * 0.5, s * 1.08, 'hit-ink', 0.25)} fill="#3A1020" />
    <path d={burstPath(14, s * 0.44, s, 'hit-ink', 0.25)} fill="#F6C22B" />
    <path d={burstPath(11, s * 0.24, s * 0.56, 'hit-core', 0.25)} fill="#FFFBD0" />
  </g>
);

const useFonts = () => {
  const [handle] = useState(() => delayRender('fonts'));
  useEffect(() => {
    const faces = [new FontFace('Darumadrop One', `url(${staticFile('fonts/DarumadropOne-Regular.ttf')})`), new FontFace('Paalalabas', `url(${staticFile('fonts/PaalalabasDisplayWide.otf')})`)];
    Promise.all(faces.map((f) => f.load()))
      .then((loaded) => {
        loaded.forEach((f) => (document.fonts as unknown as { add: (x: FontFace) => void }).add(f));
        continueRender(handle);
      })
      .catch(() => continueRender(handle));
  }, [handle]);
};

/** The map's clear morning: a neutral key, a cool shade, so the cast sits in the plate. `litAt` adds the current's own light. */
const DAY: Light = { dir: [0.45, 0.6, 0.66], lit: '#FFF8EE', shade: '#A9A2B6', rim: '#FFF4DC', rim_strength: 0.42 };
const lightFor = (zap: number): Light => (zap <= 0 ? DAY : { ...DAY, rim: '#F4F27A', rim_strength: 0.42 + 1.1 * zap });

const FOCUS = 0.5;
/** The fist in the forearm bone's space, on the redesigned rigs. */
const FIST_R: [number, number, number] = [-0.085, 0, 0];
const FIST_L: [number, number, number] = [0.085, 0, 0];
/** The lamp of the real signal the film watches (Kanto's Traffic, the north east corner). */
const SIGNAL: V3 = [16.7, 3.05, 16.6];
/** Lettering in the game's own display face. */
const Word: React.FC<{ text: string; h: number }> = ({ text, h }) => (
  <text x={0} y={0} textAnchor="middle" fontFamily="Paalalabas" fontSize={h} fill="#F6C22B" stroke="#3A1020" strokeWidth={h * 0.12} strokeLinejoin="round" style={{ paintOrder: 'stroke' }}>{text}</text>
);
const GHOSTS = 4;
const GHOST_LIFE = 11;
/** Rago's chalk: he keeps the score, in strokes on the court beside his post. */
const Tally: React.FC<{ v: View; a: number }> = ({ v, a }) => (
  <g opacity={a} fill="none" stroke="#FFFDF2" strokeWidth={7} strokeLinecap="round">
    <path d={line(v, [[1.05, 0.14, -1.75], [1.25, 0.14, -0.8]])} />
  </g>
);
const REST_X = 2.1;
const REST_Z = 2.0;

const figure = (b: NonNullable<ReturnType<typeof useActor>>, v: View, p: Frame, light: Light, slipper: boolean, extra: { flash?: number; res?: number } = {}) => {
  const focusY = FOCUS * PERSON_SCALE;
  const ppu = scaleAt(v, [p.x, p.y + focusY, p.z]) * PERSON_SCALE;
  const ap = apparent(v, [p.x, p.y, p.z], p.yaw, focusY);
  const [fx, fy] = project(v, [p.x, p.y + focusY, p.z]);
  // ⚠️ THE FIGURE IS ONE IMAGE AT ONE ANGLE. Measured from the hips, a low lens drew him from below: his
  // soles showed over a floor seen from above, and he floated (owner, 2026-11-01). While his feet are in
  // frame the angle is the one the lens sees the floor under them from.
  const footY = project(v, [p.x, 0.135, p.z])[1];
  const onFloor = Math.max(0, Math.min(1, (1300 - footY) / 220));
  const floorPitch = Math.atan2(v.y - 0.135, Math.hypot(v.x - p.x, v.z - p.z)) / (Math.PI / 180);
  const pitch = ap.pitch + Math.max(0, floorPitch - ap.pitch) * onFloor;
  const img = drawActor(b, {
    x: fx, y: fy, ppu, focus: FOCUS, reach: 0.72, yaw: ap.yaw, pitch, layers: p.layers, samples: p.samples, lift: p.lift, face: p.face, eyeGlow: p.eyeGlow,
    slipper: slipper ? p.slipper : undefined, res: extra.res ?? 1.3,
    light: { ...light, flash: extra.flash ?? 0, flashColour: '#F6FFA0' },
  });
  return { img, roll: ap.roll, fx, fy };
};

export const BgcScene: React.FC<{ part: 'intro' | 'idle' }> = ({ part }) => {
  useFonts();
  const frame = useCurrentFrame();
  // The film is shot at 60 on the same 30 a second timetable; the idle's slow drift stays at 30.
  const f = part === 'idle' ? INTRO + frame : frame / 2;
  setNight(0);
  const zk = useActor('zack-redesign', true);
  const rg = useActor('sean-redesign');
  const v = camAt(f);
  const z = zack(f);
  const r = rago(f);

  const zap = litAt(f);
  const LIGHT = lightFor(zap);
  const items: Item[] = [];
  const push = (node: React.ReactNode, at: V3, bias = 0) => items.push({ z: depth(v, at) + bias, node });

  // ---------------------------------------------------------------- the cast
  let zackHand: [number, number] | null = null;
  let zackHand2: [number, number] | null = null;
  let zackHead: [number, number] | null = null;
  let zackAt: ((bone: string, local?: [number, number, number]) => [number, number]) | null = null;
  let ragoHead: [number, number] | null = null;
  const front = (x: number, zz: number) => depth(v, [x, 1, zz]) > 0.7;
  const closeOnFeet = f >= B.feet && f < B.rago;
  if (rg && front(r.x, r.z)) {
    // ⚠️ THE CAST IS IN THE PLATE NOW, ON THE GAME'S OWN SHADER. These figures are never shown: they only say where a bone lands on screen, for the effects.
    ragoHead = figure(rg, v, r, LIGHT, false, { res: 0.3 }).img.at('head', [0, 0.14, 0.1]);
  }
  if (zk && front(z.x, z.z)) {
    const snatched = f >= B.snapBack && f < B.lockR && Math.floor(f / 2) % 2 === 1;
    const d = figure(zk, v, z, LIGHT, false, { res: snatched ? 1.2 : 0.3, flash: 0.4 });
    zackHand = d.img.at('forearm-right', FIST_R);
    zackHand2 = d.img.at('forearm-left', FIST_L);
    zackAt = d.img.at;
    zackHead = d.img.at('head', [0, 0.14, 0.1]);
    // The current takes hold of him: his thin contour flickers on.
    if (snatched) push(<g key="zack" transform={`rotate(${d.roll} ${d.fx} ${d.fy})`} filter="url(#contour)"><ActorImage d={d.img} /></g>, [z.x, 1, z.z]);
  }
  // ⚠️ BACKTRACK'S GHOST FRAMES (zack-kit plan: "ghost frames pulling into the landing"; thin hot contour, transparent faces).
  // Each is his real pose where he really was, crisp at full size, and takes its place in the depth order so Rago can stand
  // in front of it. It only burns off once he has gone past: the newest is the strongest, which is what says which way he went.
  if (zk && ((f >= B.snapBack && f < B.lockR) || (f >= B.yank - 1 && f < B.land))) {
    for (let i = 0; i < GHOSTS; i++) {
      // Two hang in the slow part, the rest are torn off by the yank; none outlives it by much.
      const u = i < 2 ? 0.035 + 0.07 * i : 0.2 + 0.22 * (i - 2);
      const g = pullWhen(u);
      if (f < g) continue;
      const a = Math.min(1, 1 - (f - Math.max(g, B.yank)) / GHOST_LIFE);
      if (a <= 0) continue;
      const at = pullAt(u);
      if (!front(at.x, at.z)) continue;
      const fr = zack(g);
      const d = figure(zk, v, { ...fr, x: at.x, z: at.z, slipper: { at: 'none' }, eyeGlow: 0, face: 'rest' }, LIGHT, false, { flash: 0.42, res: 1.2 });
      push(<g key={`e${i}`} transform={`rotate(${d.roll} ${d.fx} ${d.fy})`}>
        <g opacity={0.18 * a}><ActorImage d={d.img} /></g>
        <g opacity={Math.min(1, a * 1.4)} filter="url(#contour)"><ActorImage d={d.img} /></g>
      </g>, [at.x, 1, at.z], 0.02 * i);
    }
  }

  items.sort((a, b) => b.z - a.z);

  // ---------------------------------------------------------------- effects, in screen space
  const fx: React.ReactNode[] = [];
  /** What the current throws on the floor, under everyone. */
  const under: React.ReactNode[] = [];
  /** A pool of its light on the court and the air round it: every effect of his lights the place it happens in. */
  const pool = (key: string, x: number, zz: number, r: number, a: number) => {
    if (a <= 0 || depth(v, [x, 0.14, zz]) < 0.6) return;
    const pts: V3[] = [];
    for (let i = 0; i < 28; i++) { const t = (i / 28) * 6.283; pts.push([x + Math.cos(t) * r, 0.14, zz + Math.sin(t) * r]); }
    const d = poly(v, pts);
    if (d) under.push(<path key={key} d={d} fill="url(#pool)" opacity={a} style={{ mixBlendMode: 'screen' }} />);
    const [ax, ay] = project(v, [x, 1.1, zz]);
    const q = scaleAt(v, [x, 1.1, zz]) * r * 1.3;
    under.push(<circle key={`${key}a`} cx={ax} cy={ay} r={q} fill="url(#air)" opacity={a * 0.55} style={{ mixBlendMode: 'screen' }} />);
  };
  /** A floor line of current: fat where it is live, drawn across the court along `path`. */
  const floorBolt = (key: string, path: V3[], w: number, opacity = 1, jag = 0.16, off = 0) => {
    const at = path.filter((p) => depth(v, p) > 0.6).map((p) => project(v, p) as Pt);
    if (at.length < 2) return;
    const pts: Pt[] = [];
    for (let i = 0; i < at.length - 1; i++) pts.push(...boltPoints(at[i], at[i + 1], `${key}${i}${Math.floor(f / 2)}`, 2, jag).slice(0, -1));
    pts.push(at[at.length - 1]);
    under.push(<Bolt key={key} pts={off ? pts.map(([x, y]) => [x, y + off] as Pt) : pts} w={w} glow={0.9} opacity={opacity} outline={null} />);
  };
  pool('zk', z.x, z.z, 1.5, Math.min(1, zap * 1.2));
  const slow = f >= B.snapBack && f < B.yank;
  const tick = Math.floor(f / (slow ? 5 : 2));
  const seed = (s: string) => `${s}${tick}`;

  // The current finds the real signal: it spits at the lamp as the light turns, twice, small.
  [B.spark + 20, B.green].forEach((g, n) => {
    const a = env(f, g, g + 1, g + 5, g + 10);
    if (a <= 0 || depth(v, SIGNAL) < 0.6) return;
    const [x, y] = project(v, SIGNAL);
    const q = scaleAt(v, SIGNAL);
    for (let i = 0; i < 2 + n; i++) {
      const ang = 2 + i * 1.7 + random(`${seed('sg')}${i}`);
      fx.push(<Crackle key={`sg${n}${i}`} a={[x, y]} b={[x + Math.cos(ang) * q * 0.75, y + Math.sin(ang) * q * 0.75]} seed={`${seed('sg')}${n}${i}`} w={Math.max(3, q * 0.022)} branches={1} glow={0.8} opacity={a} />);
    }
  });
  // The green has to read at a glance: the lamp blooms.
  const greenA = f >= B.green && f < B.eyes ? env(f, B.green, B.green + 2, B.eyes - 6, B.eyes) : 0;
  if (greenA > 0 && depth(v, SIGNAL) > 0.6) {
    const lamp: V3 = [SIGNAL[0], SIGNAL[1] - 0.66, SIGNAL[2]];
    const [x, y] = project(v, lamp);
    const q = scaleAt(v, lamp);
    fx.push(<g key="gl" style={{ mixBlendMode: 'screen' }} opacity={greenA}><circle cx={x} cy={y} r={q * 0.75} fill="url(#greenlamp)" /></g>);
  }
  // ⚠️ THE CURRENT LIVES ON HIM: arcs jump between points of his own body, never out into the air.
  const bodyArcs = (key: string, n: number, opacity = 1) => {
    if (!zackAt || n <= 0) return;
    const at = zackAt;
    const pts: Pt[] = [at('forearm-right', FIST_R), at('arm-right', [0, 0, 0]), at('torso', [0, 0.06, 0.1]), at('arm-left', [0, 0, 0]), at('forearm-left', FIST_L), at('leg-left', [0, 0, 0.05]), at('leg-left', [0, -0.16, 0.05]), at('leg-right', [0, -0.16, 0.05]), at('leg-right', [0, 0, 0.05]), at('head', [0.12, 0.02, 0]), at('head', [-0.12, 0.02, 0])];
    const s = scaleAt(v, [z.x, 1, z.z]);
    const stamp = Math.floor(f / 2);
    for (let i = 0; i < n; i++) {
      const from = Math.floor(random(`${key}p${i}${stamp}`) * pts.length);
      const to = (from + 1 + Math.floor(random(`${key}q${i}${stamp}`) * 2)) % pts.length;
      fx.push(<Crackle key={`${key}${i}`} a={pts[from]} b={pts[to]} seed={`${key}${i}${stamp}`} w={Math.max(2, s * 0.022)} branches={i % 2} glow={0.7} opacity={opacity} />);
    }
  };
  // Before the throw: now and then one small arc across him.
  if (zackAt && f >= B.whole && f < B.run) {
    const a = env(((f - INTRO) % 144 + 144) % 144, 8, 10, 22, 26);
    if (a > 0) bodyArcs('rest', 1, a);
  }
  // ⚠️ THE HOLD IS QUIET (owner, 2026-11-01: from time to time, "dont spam it"). A few moments on the 24 s clock
  // (light.ts), each a small piece of acting with an anticipation, a peak and a falloff, and nothing in between.
  if (part === 'idle' && zackAt) {
    const q = ph(f);
    const s2 = scaleAt(v, [z.x, 1, z.z]);
    const at = zackAt;
    const w = Math.max(3, s2 * 0.022);
    const stamp = Math.floor(f / 2);
    IDLE_SPARKS.forEach((e) => {
      const t = q - e.at;
      if (t < 0 || t >= e.len) return;
      const a = env(t, 0, 1, e.len * 0.55, e.len);
      if (e.kind === 'fingers' && zackHand2) {
        fx.push(<Crackle key="if1" a={zackHand2} b={[zackHand2[0] + s2 * 0.1, zackHand2[1] - s2 * 0.13]} seed={`if1${stamp}`} w={w} branches={1} glow={0.7} opacity={a} />);
      }
      if (e.kind === 'run') {
        // Down from the hair, along the arm and off the fingers into the floor: one line that travels.
        const route: Pt[] = [at('torso', [-0.12, 0.1, 0.05]), at('arm-right', [0, 0, 0]), at('forearm-right', [0, 0, 0]), at('forearm-right', FIST_R)];
        const u = Math.min(route.length - 1.001, (t / (e.len * 0.45)) * (route.length - 1));
        const i0 = Math.floor(u);
        const head: Pt = [route[i0][0] + (route[i0 + 1][0] - route[i0][0]) * (u - i0), route[i0][1] + (route[i0 + 1][1] - route[i0][1]) * (u - i0)];
        const live = [...route.slice(Math.max(0, i0 - 1), i0 + 1), head].reverse();
        if (t < e.len * 0.55) fx.push(<Bolt key="ir" pts={live.flatMap((p, n) => (n < live.length - 1 ? boltPoints(p, live[n + 1], `ir${n}${stamp}`, 2, 0.16).slice(0, -1) : [p]))} w={w * 1.3} glow={0.9} />);
        if (t >= e.len * 0.42 && zackHand) {
          const [gx, gy] = project(v, [z.x + 0.5, 0.15, z.z - 0.1]);
          fx.push(<Crackle key="ig" a={zackHand} b={[gx, gy]} seed={`ig${stamp}`} w={w * 1.1} branches={2} glow={0.9} opacity={a} />);
        }
      }
      if (e.kind === 'hair') {
        fx.push(<Crackle key="ih" a={at('head', [0.13, 0.12, 0])} b={at('head', [-0.12, 0.17, 0])} seed={`ih${stamp}`} w={w} branches={1} glow={0.7} opacity={a} />);
      }
    });
  }
  // The look: the current comes up through him as the eyes catch, and it is what grabs him.
  if (f >= B.look + 11 && f < B.snapBack) bodyArcs('cm', f >= EYES_CATCH && f < EYES_CATCH + 10 ? 4 : 2);
  if (env(f, B.shake + 4, B.shake + 8, B.shake + 16, B.shake + 22) > 0 && zackHand2) {
    const s = scaleAt(v, [z.x, 1, z.z]);
    fx.push(<Crackle key="shake" a={zackHand2} b={[zackHand2[0] - s * 0.16, zackHand2[1] + s * 0.14]} seed={seed('sh')} w={Math.max(2, s * 0.02)} branches={1} glow={0.8} />);
  }
  // The foot taps a tiny spark out of the chalk.
  if (closeOnFeet) {
    const tap = ((f - B.feet) % 18);
    if (tap < 5) {
      const [fx0, fy0] = project(v, [z.x + 0.2, 0.15, z.z - 0.25]);
      const s = scaleAt(v, [z.x, 0.2, z.z]);
      fx.push(<Crackle key="tap" a={[fx0, fy0]} b={[fx0 + s * 0.3, fy0 - s * 0.16]} seed={seed('t')} w={3} branches={1} glow={0.8} opacity={1 - tap / 5} />);
    }
  }
  // ⚠️ NO TRACE IN THE AIR (owner, 2026-11-02: "wala naman ganyan sa game"). The slipper alone makes the curve, with its current on it.
  const fl = flyingAt(f);
  if (fl?.trail && f < B.tump && depth(v, fl.p) > 0.6) {
    const [x, y] = project(v, fl.p);
    const q = scaleAt(v, fl.p);
    for (let i = 0; i < 2; i++) {
      const ang = random(`${seed('rh')}${i}`) * 6.283;
      fx.push(<Crackle key={`rh${i}`} a={[x, y]} b={[x + Math.cos(ang) * q * 0.5, y + Math.sin(ang) * q * 0.5]} seed={`${seed('rh')}${i}`} w={Math.max(3, q * 0.03)} branches={1} glow={0.8} />);
    }
  }
  // The bend: the current snaps it round, here.
  const clang = env(f, B.clang, B.clang + 1, B.clang + 3, B.clang + 12);
  const bend = curveAt((B.clang - B.snap - 2) / (B.tump - B.snap - 2));
  if (clang > 0 && depth(v, bend) > 0.6) {
    const [x, y] = project(v, bend);
    const q = scaleAt(v, bend);
    for (let i = 0; i < 5; i++) {
      const a = (i / 5) * Math.PI * 2 + random(`${seed('c')}${i}`);
      fx.push(<Crackle key={`cl${i}`} a={[x, y]} b={[x + Math.cos(a) * q * 1.4, y + Math.sin(a) * q * 1.4]} seed={`${seed('cl')}${i}`} w={5} branches={1} glow={0.8} opacity={clang} />);
    }
    fx.push(<circle key="clr" cx={x} cy={y} r={q * (0.25 + (1 - clang) * 1.3)} fill="none" stroke="#F6FFA0" strokeWidth={4} opacity={clang} />);
  }
  // Momentum's three stages: more of his body lit, and a fatter trail torn off his heels along the floor.
  const stage = momentum(f);
  if (stage > 0 && front(z.x, z.z)) {
    bodyArcs('m', stage);
    const s = scaleAt(v, [z.x, 0.4, z.z]);
    const reach = 6 + stage * 5;
    [-0.17, 0.17].forEach((side, n) => {
      const trail: V3[] = [];
      for (let k = 0; k <= reach; k += 2) {
        const g = echoAt(Math.max(B.run, f - k));
        const g2 = echoAt(Math.max(B.run, f - k - 1));
        const dx = g.x - g2.x;
        const dz = g.z - g2.z;
        const len = Math.hypot(dx, dz) || 1;
        trail.push([g.x + (dz / len) * side, 0.15, g.z - (dx / len) * side]);
      }
      floorBolt(`mt${n}`, trail, Math.max(3, s * 0.03) + stage * 1.6, 0.6 + stage * 0.13);
    });
    pool('mpool', z.x, z.z, 1 + stage * 0.35, 0.28 + stage * 0.17);
    // Sparks kicked up by the stride at the higher stages.
    if (stage >= 2) {
      for (let i = 0; i < stage * 3; i++) {
        const born = Math.floor(f / 3) * 3 - i * 2;
        const t = (f - born) / 12;
        if (t < 0 || t > 1) continue;
        const g = echoAt(Math.max(B.run, born));
        const a2 = random(`ms${born}a`) * 6.283;
        const p: V3 = [g.x + Math.cos(a2) * 0.6 * t, 0.16 + 1.1 * t - 1.4 * t * t, g.z + Math.sin(a2) * 0.6 * t];
        if (depth(v, p) < 0.6) continue;
        const [x, y] = project(v, p);
        const q = scaleAt(v, p);
        fx.push(<rect key={`ms${i}`} x={x} y={y} width={q * 0.05} height={q * 0.016} transform={`rotate(${born * 41} ${x} ${y})`} fill={i % 2 ? '#F6C22B' : '#FFFBD0'} opacity={1 - t} />);
      }
    }
  }
  // Rago's shock: action lines, the mark beside his head (never on it), sweat.
  if (f >= B.jolt && f < B.stare && ragoHead) {
    const a = env(f, B.jolt, B.jolt + 2, B.stare - 8, B.stare);
    const s2 = scaleAt(v, [r.x, 1.5, r.z]);
    const [hx, hy] = ragoHead;
    const lines: React.ReactNode[] = [];
    for (let i = 0; i < 26; i++) {
      const ang = (i / 26) * Math.PI * 2 + random(`al${i}`) * 0.2;
      const r0 = 640 + 170 * random(`ar${i}${Math.floor(f / 2)}`);
      const r1 = r0 + 520;
      lines.push(<path key={i} d={`M${960 + Math.cos(ang - 0.012) * r1} ${540 + Math.sin(ang - 0.012) * r1}L${960 + Math.cos(ang) * r0} ${540 + Math.sin(ang) * r0}L${960 + Math.cos(ang + 0.012) * r1} ${540 + Math.sin(ang + 0.012) * r1}Z`} fill="#3A1020" />);
    }
    const pop = kf(f, [[B.jolt, 0.2], [B.jolt + 5, 1.2], [B.jolt + 9, 1]]);
    fx.push(<g key="shock" opacity={a}>
      <g opacity={0.7}>{lines}</g>
      <g transform={`translate(${hx + s2 * 0.72} ${hy - s2 * 0.08}) rotate(10) scale(${pop})`}>
        <Word text="!" h={s2 * 0.6} />
      </g>
      {[0, 1, 2].map((d) => {
        const t = (((f - B.jolt) * 1.6 + d * 11) % 26) / 26;
        const sx = hx + (d === 1 ? 1 : -1) * s2 * (0.32 + 0.3 * t);
        const sy = hy - s2 * 0.42 + s2 * 0.62 * t * t - s2 * 0.1 * d;
        return <path key={d} d={`M${sx} ${sy - s2 * 0.07}Q${sx + s2 * 0.05} ${sy} ${sx} ${sy + s2 * 0.035}Q${sx - s2 * 0.05} ${sy} ${sx} ${sy - s2 * 0.07}Z`} fill="#EAF6F2" stroke="#3A1020" strokeWidth={1.6} opacity={1 - t} />;
      })}
    </g>);
  }
  // ⚠️ BACKTRACK, READ AS WHAT IT IS (zack-kit plan): a gold streak runs BACKWARDS along his path, and he reappears in a ring
  // of sparks that holds and breaks into four. Anticipation is the flicker and the arcs before it (`snatched`, 'cm' above). No
  // star-burst on his body: that reads as him being hit, and it is the one thing that did not happen.
  const pullEnd = B.snapBack + PULL;
  if (f >= B.snapBack && f < pullEnd + 18) {
    // The streak, on the floor: from the spot he left to wherever he has got to, fading from the old end.
    const a = env(f, B.snapBack, B.snapBack + 1, pullEnd, pullEnd + 18);
    const u0 = f < pullEnd ? pullU(f) : 1;
    const path: V3[] = [];
    for (let i = 0; i <= 14; i++) { const q = pullAt((u0 * (14 - i)) / 14); path.push([q.x, 0.15, q.z]); }
    if (f < B.land) floorBolt('bk0', path, 15, a, 0.09);
    if (f < B.land) floorBolt('bk1', path, 7, a * 0.8, 0.12, 7);
    pool('bkp', STAND.x, STAND.z, 1.5, env(f, B.snapBack, B.snapBack + 1, B.snapBack + 6, B.snapBack + 20) * 0.9);
    // The place he was snatched from cracks once: a thin ring along the floor and three short arcs.
    const t = (f - B.snapBack) / 24;
    if (t < 1) {
      const ring: V3[] = [];
      for (let i = 0; i <= 32; i++) ring.push([STAND.x + Math.cos((i / 32) * 6.283) * (0.3 + 1.5 * Math.sqrt(t)), 0.14, STAND.z + Math.sin((i / 32) * 6.283) * (0.3 + 1.5 * Math.sqrt(t))]);
      fx.push(<path key="bkr" d={line(v, ring)} stroke="#FFFBD0" strokeWidth={5 * (1 - t) + 1} fill="none" opacity={1 - t} />);
      const [x, y] = project(v, [STAND.x, 0.9, STAND.z]);
      const q = scaleAt(v, [STAND.x, 0.9, STAND.z]);
      for (let i = 0; i < 3; i++) {
        const ang = (i / 3) * 6.283 + 0.5;
        fx.push(<Crackle key={`bb${i}`} a={[x, y]} b={[x + Math.cos(ang) * q * 0.7, y + Math.sin(ang) * q * 0.7]} seed={`${seed('bb')}${i}`} w={Math.max(3, q * 0.02)} branches={1} glow={0.7} opacity={1 - t} />);
      }
    }
  }
  // ⚠️ THE ARRIVAL: everything the slow part held back. Current bursts along the floor from his feet, a shock ring runs out, arcs climb him.
  const arrive = B.land + 5;
  if (f >= arrive && f < arrive + 16) {
    const t = (f - arrive) / 16;
    for (let i = 0; i < 9; i++) {
      const ang = (i / 9) * 6.283 + random(`ab${i}`) * 0.5;
      const r0 = 0.6 + 3.4 * Math.sqrt(t);
      const r1 = Math.max(0.5, r0 - 1.5 - random(`ac${i}`));
      floorBolt(`ab${i}`, [[LINE.x + Math.cos(ang) * r0, 0.15, LINE.z + Math.sin(ang) * r0], [LINE.x + Math.cos(ang) * r1, 0.15, LINE.z + Math.sin(ang) * r1]], 12 * (1 - t) + 3, 1 - t * t, 0.22);
    }
    const ring: V3[] = [];
    for (let i = 0; i <= 40; i++) { const k = (i / 40) * 6.283; const rr = 0.7 + 5.5 * Math.sqrt(t); ring.push([LINE.x + Math.cos(k) * rr, 0.15, LINE.z + Math.sin(k) * rr]); }
    under.push(<path key="asr" d={line(v, ring)} stroke="#FFFBD0" strokeWidth={14 * (1 - t) + 1} fill="none" opacity={1 - t} />);
    pool('arp', LINE.x, LINE.z, 3, 1 - t);
    if (zackAt) bodyArcs('ar', 5, 1 - t);
  }
  // Where he lands: the ring holds until a beat after the whip shows it, then breaks into four sparks that run off along the floor.
  const ringEnd = B.land + 30;
  if (f >= pullEnd - 1 && f < ringEnd + 12) {
    const a = f < ringEnd ? 1 : 0;
    const flare = env(f, pullEnd - 1, pullEnd, pullEnd + 1, pullEnd + 10);
    const a0 = f >= arrive ? 1 : 0;
    if (a && a0) {
      const ring: V3[] = [];
      const r0 = 0.95 + 0.25 * flare;
      for (let i = 0; i <= 40; i++) { const t = (i / 40) * 6.283; const n = 1 + 0.025 * Math.sin(t * 9 + f); ring.push([LINE.x + Math.cos(t) * r0 * n, 0.15, LINE.z + Math.sin(t) * r0 * n]); }
      under.push(<g key="lr" fill="none"><path d={line(v, ring)} stroke="#F6C22B" strokeWidth={13} opacity={0.35} filter="url(#glow)" /><path d={line(v, ring)} stroke="#F4E23A" strokeWidth={5} /><path d={line(v, ring)} stroke="#FFFBD0" strokeWidth={2} /></g>);
      pool('lrp', LINE.x, LINE.z, 1.6, 0.5 + 0.4 * flare);
      if (Math.floor(f / 3) % 2 === 0) {
        const t = random(`lr${Math.floor(f / 3)}`) * 6.283;
        const q = scaleAt(v, [LINE.x, 0.15, LINE.z]);
        const [sx, sy] = project(v, [LINE.x + Math.cos(t) * r0, 0.15, LINE.z + Math.sin(t) * r0]);
        fx.push(<Crackle key="lrs" a={[sx, sy]} b={[sx + Math.cos(t) * q * 0.28, sy - q * 0.24]} seed={seed('lrs')} w={Math.max(3, q * 0.02)} branches={0} glow={0.7} />);
      }
    } else if (a0) {
      const t = (f - ringEnd) / 12;
      [0.8, 2.37, 3.94, 5.51].forEach((ang, i) => {
        const r1 = 0.95 + 1.8 * t;
        const p0: V3 = [LINE.x + Math.cos(ang) * r1, 0.15, LINE.z + Math.sin(ang) * r1];
        const p1: V3 = [LINE.x + Math.cos(ang) * (r1 - 0.45), 0.15, LINE.z + Math.sin(ang) * (r1 - 0.45)];
        floorBolt(`rs${i}`, [p0, p1], 8, 1 - t, 0.2);
      });
    }
    if (f < B.land + 30 && f >= pullEnd) bodyArcs('home', Math.round(kf(f, [[pullEnd, 3], [B.land, 2], [B.land + 30, 0]])));
  }
  // Stars round the head of a taya who has met his own can.
  if (f >= B.sprawl - 2 && f < B.rise + 30) {
    const a = env(f, B.sprawl - 2, B.sprawl + 4, B.rise + 8, B.rise + 30);
    const h = flatHead(r);
    if (depth(v, h) > 0.6) {
      const [hx, hy] = project(v, h);
      const s2 = scaleAt(v, h);
      for (let i = 0; i < 5; i++) {
        const ang = (f / 7) + (i / 5) * Math.PI * 2;
        const x = hx + Math.cos(ang) * s2 * 0.52;
        const y = hy - s2 * 0.42 + Math.sin(ang) * s2 * 0.15;
        const q = s2 * (0.075 + 0.025 * Math.sin(ang));
        const star = Array.from({ length: 10 }, (_, k) => {
          const rr = k % 2 ? q * 0.45 : q;
          const t = (k / 10) * Math.PI * 2 + f / 9;
          return `${k ? 'L' : 'M'}${x + Math.cos(t) * rr} ${y + Math.sin(t) * rr}`;
        }).join('') + 'Z';
        fx.push(<path key={`st${i}`} d={star} fill="#F6C22B" stroke="#3A1020" strokeWidth={1.8} opacity={a} />);
      }
    }
  }
  // The hit, on the court: rings along the floor, sparks and chips thrown up from the can.
  if (f >= B.tump + 2 && f < B.tump + 34) {
    const t = (f - B.tump - 2) / 32;
    [0, 0.22].forEach((lag, n) => {
      const u = Math.max(0, t - lag);
      if (u <= 0) return;
      const ring: V3[] = [];
      for (let i = 0; i <= 40; i++) ring.push([Math.cos((i / 40) * 6.283) * (0.4 + 7 * Math.sqrt(u)), 0.14, Math.sin((i / 40) * 6.283) * (0.4 + 7 * Math.sqrt(u))]);
      fx.push(<path key={`ring${n}`} d={line(v, ring)} stroke="#FFFBD0" strokeWidth={12 * (1 - u)} fill="none" opacity={(1 - u) * 0.9} />);
    });
    for (let i = 0; i < 22; i++) {
      const a = random(`sp${i}`) * 6.283;
      const sp = 1.2 + 3.4 * random(`sq${i}`);
      const up = 2.2 + 3 * random(`su${i}`);
      const p: V3 = [Math.cos(a) * sp * t * 1.6, 0.4 + up * t - 4.4 * t * t, Math.sin(a) * sp * t * 1.6];
      if (depth(v, p) < 0.6 || p[1] < 0.1) continue;
      const [x, y] = project(v, p);
      const q = scaleAt(v, p);
      fx.push(<rect key={`sp${i}`} x={x} y={y} width={q * 0.09} height={q * 0.03} transform={`rotate(${i * 37 + f * 12} ${x} ${y})`} fill={i % 3 ? '#F6C22B' : '#FFFBD0'} opacity={1 - t} />);
    }
  }
  // Dust: every step of a sprint, the trip, the floor.
  const puff = (key: string, at: V3, start: number, life: number, size: number) => {
    if (f < start || f >= start + life || depth(v, at) < 0.6) return;
    const t = (f - start) / life;
    const [x, y] = project(v, at);
    const s2 = scaleAt(v, at);
    for (let i = 0; i < 5; i++) {
      const ang = (i / 5) * Math.PI + random(`${key}a${i}`) * 0.5;
      const q = s2 * size * (0.35 + 1.1 * t);
      fx.push(<circle key={`${key}${i}`} cx={x + Math.cos(ang) * q * 1.4} cy={y - Math.sin(ang) * q * 0.55} r={q * (0.45 + 0.25 * random(`${key}r${i}`))} fill="#EBDCBE" opacity={0.55 * (1 - t)} />);
    }
  };
  for (let g = B.run + 4; g < B.snapBack; g += 7) { const q = echoAt(g); puff(`rd${g}`, [q.x, 0.15, q.z], g, 14, 0.13); }
  puff('slide', [REST_X, 0.15, REST_Z], B.scoop - 6, 26, 0.34);
  puff('trip', [r.x, 0.15, -0.3], B.trip, 18, 0.32);
  puff('flat', [FLAT.x + 0.3, 0.15, FLAT.z + 0.6], B.sprawl - 2, 34, 0.62);

  // ---------------------------------------------------------------- impact
  const tumpWord = env(f, B.tump + 5, B.tump + 6, B.deadpan - 3, B.deadpan);
  const [icx, icy] = project(v, [0, 0.4, 0]);
  const sp = panSpeed(f);
  const bx = sp.x > 24 ? Math.min(14, (sp.x - 24) * 0.22) : 0;
  const by = sp.y > 24 ? Math.min(10, (sp.y - 24) * 0.22) : 0;
  const blur = bx > 0.5 || by > 0.5;
  const hit = part === 'intro' ? impactAt(f) : { stage: 0, punch: 0 };
  const punch = 1 + 0.07 * hit.punch * hit.punch;
  const hush = slow ? Math.min(1, (f - B.snapBack) / 6, (B.yank - f) / 2) : 0;
  const eyes = hit.stage > 0 && zackAt && f >= EYES_CATCH && f < EYES_CATCH + 2 ? [EYE_L, EYE_R].map((e) => (zackAt as NonNullable<typeof zackAt>)('head', e)) : [];
  const tally = f >= INTRO ? 1 : kf(f, [[B.one + 2, 0], [B.one + 12, 1]]);

  return (
    <AbsoluteFill style={{ backgroundColor: '#C86A5C' }}>
      <svg width={1920} height={1080} viewBox="0 0 1920 1080">
        <Defs frame={f} />
        <defs>
          {/* The owner's approved Backtrack look: transparent faces, a hot thin contour (zack-kit-2026-10-25). */}
          <filter id="contour" x="-20%" y="-20%" width="140%" height="140%">
            <feMorphology in="SourceAlpha" operator="dilate" radius="1.8" result="fat" />
            <feComposite in="fat" in2="SourceAlpha" operator="out" result="edge" />
            <feFlood floodColor="#F9F46A" />
            <feComposite in2="edge" operator="in" result="line" />
            <feGaussianBlur in="line" stdDeviation="2" result="soft" />
            <feMerge><feMergeNode in="soft" /><feMergeNode in="line" /></feMerge>
          </filter>
          <filter id="impactA" colorInterpolationFilters="sRGB"><feColorMatrix type="saturate" values="0" /><feComponentTransfer><feFuncR type="discrete" tableValues="0.09 0.09 1 1" /><feFuncG type="discrete" tableValues="0.02 0.02 0.97 0.97" /><feFuncB type="discrete" tableValues="0.06 0.06 0.82 0.82" /></feComponentTransfer></filter>
          <filter id="impactB" colorInterpolationFilters="sRGB"><feColorMatrix type="saturate" values="0" /><feComponentTransfer><feFuncR type="discrete" tableValues="0.96 0.96 0.09 0.09" /><feFuncG type="discrete" tableValues="0.76 0.76 0.02 0.02" /><feFuncB type="discrete" tableValues="0.17 0.17 0.06 0.06" /></feComponentTransfer></filter>
          <radialGradient id="pool"><stop offset="0" stopColor="#FFF18A" stopOpacity={0.85} /><stop offset="0.45" stopColor="#F6C22B" stopOpacity={0.32} /><stop offset="1" stopColor="#F6C22B" stopOpacity={0} /></radialGradient>
          <radialGradient id="greenlamp"><stop offset="0" stopColor="#B8FFC8" stopOpacity={0.95} /><stop offset="0.3" stopColor="#3DFF7C" stopOpacity={0.5} /><stop offset="1" stopColor="#3DFF7C" stopOpacity={0} /></radialGradient>
          <radialGradient id="air"><stop offset="0" stopColor="#FFF3A8" stopOpacity={0.5} /><stop offset="1" stopColor="#F6C22B" stopOpacity={0} /></radialGradient>
        </defs>
        {blur && (
          <defs>
            <filter id="whip" x="-5%" y="-5%" width="110%" height="110%"><feGaussianBlur stdDeviation={`${bx} ${by}`} /></filter>
          </defs>
        )}
        {/* The real map, recorded through this same camera (scripts/plates.mjs). It already carries the roll. */}
        <g transform={`translate(960 540) scale(${punch}) translate(-960 -540)`}>
        <g filter={hit.stage === 1 ? 'url(#impactA)' : hit.stage === 2 ? 'url(#impactB)' : blur ? 'url(#whip)' : undefined} style={hush > 0 ? { filter: `saturate(${1 - 0.35 * hush}) contrast(${1 + 0.08 * hush})` } : undefined}>
          <foreignObject width={1920} height={1080}><Img src={plateAt(part, frame)} style={{ width: 1920, height: 1080, display: 'block' }} /></foreignObject>
          {/* The idle's end fades to the street that leads into its start, so the loop has no seam. */}
          {part === 'idle' && frame >= IDLE - BLEND && <foreignObject width={1920} height={1080} opacity={(frame - (IDLE - BLEND) + 1) / BLEND}><Img src={preAt(frame - (IDLE - BLEND))} style={{ width: 1920, height: 1080, display: 'block' }} /></foreignObject>}
        </g>
        <g transform={`rotate(${v.roll} 960 540)`} filter={blur ? 'url(#whip)' : undefined}>
          {under}
          {tally > 0 && <Tally v={v} a={tally} />}
          {items.map((it, i) => <g key={i}>{it.node}</g>)}
          {fx}
        </g>
        </g>
        {/* ⚠️ AN IMPACT FRAME: two held frames, ink and paper then flipped, with lines rushing to the moment. His eyes alone stay lit. */}
        {hit.stage > 0 && <g fill={hit.stage === 1 ? '#17060F' : '#F6E25A'} opacity={0.92}>{Array.from({ length: 34 }, (_, i) => {
          const ang = (i / 34) * Math.PI * 2 + random(`il${i}`) * 0.16;
          const r0 = 360 + 240 * random(`ir${i}${hit.stage}`);
          const r1 = 1300;
          return <path key={i} d={`M${960 + Math.cos(ang - 0.016) * r1} ${540 + Math.sin(ang - 0.016) * r1}L${960 + Math.cos(ang) * r0} ${540 + Math.sin(ang) * r0}L${960 + Math.cos(ang + 0.016) * r1} ${540 + Math.sin(ang + 0.016) * r1}Z`} />;
        })}</g>}
        {eyes.map(([x, y], i) => <circle key={i} cx={x} cy={y} r={24} fill="#FFFBD0" />)}
        <rect width={1920} height={1080} fill="url(#vignette)" opacity={0.14 + 0.5 * hush} />
        {/* ⚠️ THE MOMENT OF IMPACT: everything holds for STOP frames. A flash, ink, then the burst. */}
        {f >= B.tump && f < B.tump + 2 && <rect width={1920} height={1080} fill="#FFFBD0" />}
        {f >= B.tump + 2 && f < B.tump + 5 && (
          <g>
            <rect width={1920} height={1080} fill="#2A0620" opacity={0.92} />
            <Burst x={icx} y={icy} s={380} />
          </g>
        )}
        {f >= B.tump + 5 && f < B.tump + STOP && <Burst x={icx} y={icy} s={300} />}
        {tumpWord > 0 && (
          <g opacity={tumpWord} transform={`translate(620 340) rotate(-7) scale(${kf(f, [[B.tump + 5, 0.4], [B.tump + 9, 1.2], [B.tump + 13, 1]])})`}>
            <Word text="TUMP!" h={320} />
          </g>
        )}
      </svg>
    </AbsoluteFill>
  );
};

