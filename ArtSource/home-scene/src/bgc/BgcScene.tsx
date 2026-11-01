import React, { useEffect, useState } from 'react';
import { AbsoluteFill, continueRender, delayRender, random, staticFile, useCurrentFrame } from 'remotion';
import { Defs } from '../lib/Defs';
import { Bolt, boltPoints, Crackle, Pt } from '../lib/bolt';
import { kf } from '../lib/kf';
import { env, INTRO } from './time';
import { B } from './beats';
import { ActorImage, drawActor, drawProp, Light, useActor, useProp } from '../three/actor';
import { apparent, depth, line, PERSON_SCALE, poly, project, scaleAt, V3, View } from '../phaister/view';
import { setNight, Item } from '../phaister/set';
import { burstPath } from '../art/burst';
import { Buildings, Clouds, FarCity, FEED, Ground, Park, Poles, sagAt, Signals, Sky, Traffic } from './set';
import { camAt, focusAt, panSpeed } from './camera';
import { canAt, echoAt, curveAt, flatHead, flyingAt, FLAT, Frame, momentum, PULL, pullAt, pullU, STOP, rago, slipperTrail, STAND, zack } from './perform';
import { Paint } from '../phaister/lettering';

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

/** Warm key from the lens side, warm rim from the low sun (method § 2). */
const LIGHT: Light = { dir: [0.45, 0.6, 0.66], lit: '#FFF2E0', shade: '#BE8E86', rim: '#FFD99A', rim_strength: 0.6 };

const FOCUS = 0.5;
/** The fist in the forearm bone's space, on the redesigned rigs. */
const FIST_R: [number, number, number] = [-0.085, 0, 0];
const FIST_L: [number, number, number] = [0.085, 0, 0];
const CAN_M_PER_UNIT = 0.62 / 0.383;
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

/** A soft contact shadow, flat on the court. */
const shadowOf = (v: View, x: number, z: number, r: number) => {
  const pts: V3[] = [];
  for (let i = 0; i < 18; i++) {
    const a = (i / 18) * 2 * Math.PI;
    pts.push([x + r * Math.cos(a) + 0.12, 0.135, z + r * 0.72 * Math.sin(a) - 0.08]);
  }
  const d = poly(v, pts);
  return d ? <path d={d} fill="#2A1218" opacity={0.3} /> : null;
};

export const BgcScene: React.FC<{ part: 'intro' | 'idle' }> = ({ part }) => {
  useFonts();
  const frame = useCurrentFrame();
  // The film is shot at 60 on the same 30 a second timetable; the idle's slow drift stays at 30.
  const f = part === 'idle' ? INTRO + frame : frame / 2;
  setNight(0);
  const zk = useActor('zack-redesign', true);
  const rg = useActor('sean-redesign');
  const can = useProp('lata_metal');
  const v = camAt(f);
  const z = zack(f);
  const r = rago(f);
  const c = canAt(f);

  // The city sits behind a focus layer, the park and the cast in front of it (research: depth and air).
  const city: Item[] = [...FarCity(v), ...Buildings(v), ...Traffic(v, f), ...Poles(v, f)].sort((a, b) => b.z - a.z);
  const items: Item[] = [...Signals(v, f), ...Park(v, f)];
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
    const d = figure(rg, v, r, LIGHT, false);
    ragoHead = d.img.at('head', [0, 0.14, 0.1]);
    push(<g key="rago"><g>{shadowOf(v, r.x, r.z, 0.8)}</g><g transform={`rotate(${d.roll} ${d.fx} ${d.fy})`}><ActorImage d={d.img} /></g></g>, [r.x, 1, r.z]);
  }
  if (zk && front(z.x, z.z)) {
    const d = figure(zk, v, z, LIGHT, true, { res: closeOnFeet ? 2.2 : 1.35 });
    zackHand = d.img.at('forearm-right', FIST_R);
    zackHand2 = d.img.at('forearm-left', FIST_L);
    zackAt = d.img.at;
    zackHead = d.img.at('head', [0, 0.14, 0.1]);
    push(<g key="zack"><g>{shadowOf(v, z.x, z.z, 0.72)}</g><g transform={`rotate(${d.roll} ${d.fx} ${d.fy})`}><ActorImage d={d.img} /></g></g>, [z.x, 1, z.z]);
  }
  if (can && depth(v, [c.x, 0.3, c.z]) > 0.6) {
    const at: V3 = [c.x, c.hop + 0.13, c.z];
    const ppu = scaleAt(v, at) * CAN_M_PER_UNIT;
    const ap = apparent(v, [c.x, 0, c.z], 22, 0.3);
    const [x, y] = project(v, [at[0], at[1] + 0.3, at[2]]);
    const img = drawProp(can, { x, y, ppu, focus: 0.19, reach: 0.34, yaw: ap.yaw, pitch: ap.pitch, rot: c.rot, lift: [0, 0.125 * c.lying, 0], res: 1.5, light: LIGHT });
    push(<g key="can"><ActorImage d={img} /></g>, at);
  }
  const fl = flyingAt(f);
  if (fl && zk && depth(v, fl.p) > 0.6) {
    // Half as big again in the air: a slipper true to size is a few pixels across the wide.
    const ppu = scaleAt(v, fl.p) * PERSON_SCALE * (fl.trail && f < B.closeOn ? 1.9 : 1);
    const [x, y] = project(v, fl.p);
    const img = drawActor(zk, { x, y, ppu, focus: 0, target: [0, 0, 0], reach: 0.22, yaw: 0, pitch: apparent(v, fl.p, 0, 0).pitch, only: 'slipper', slipper: { at: 'free', pos: [0, 0, 0], rot: fl.rot }, light: LIGHT, res: 1.4 });
    push(<g key="slipper"><ActorImage d={img} /></g>, fl.p, -0.2);
  }

  // Backtrack: thin electric echoes of where he has been, merging into him as he lands.
  const echoes: React.ReactNode[] = [];
  // Each hangs where he passed and burns off, oldest first; the pose is the one he had there.
  if (zk && f >= B.snapBack && f < B.snapBack + PULL + 16) {
    for (let i = 0; i < 8; i++) {
      const g = B.snapBack + (i / 7) * (PULL - 1);
      if (f < g) continue;
      const a = 1 - (f - g) / 16;
      if (a <= 0) continue;
      const at = pullAt(pullU(g));
      if (!front(at.x, at.z)) continue;
      const fr = zack(g);
      const d = figure(zk, v, { ...fr, x: at.x, z: at.z, samples: undefined, slipper: { at: 'none' } }, LIGHT, false, { flash: 1, res: 0.8 });
      echoes.push(<g key={`e${i}`} opacity={a}>
        <g opacity={0.28}><ActorImage d={d.img} /></g>
        <g filter="url(#contour)"><ActorImage d={d.img} /></g>
      </g>);
    }
  }

  items.sort((a, b) => b.z - a.z);

  // ---------------------------------------------------------------- effects, in screen space
  const fx: React.ReactNode[] = [];
  const seed = (s: string) => `${s}${Math.floor(f / 2)}`;

  // The current on the wire, first shot.
  const wireT = kf(f, [[B.spark, 0], [B.feet - 14, 1]]);
  if (f >= B.spark && f < B.feet) {
    const pts: Pt[] = [];
    for (let i = 0; i <= 8; i++) {
      const u = Math.max(0, wireT - 0.16 + (i / 8) * 0.16);
      const p = sagAt(FEED[0], FEED[1], u, 0.9);
      if (depth(v, p) > 0.5) pts.push(project(v, p));
    }
    if (pts.length > 1 && wireT < 1) fx.push(<Bolt key="wire" pts={boltPoints(pts[0], pts[pts.length - 1], seed('wire'), 3, 0.09)} w={3} glow={0.8} />);
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
  // Resting: now and then one small arc across him.
  if (zackAt && (f >= B.land + 40 || f < B.run)) {
    const a = env(((f - INTRO) % 144 + 144) % 144, 8, 10, 22, 26);
    if (a > 0 && !closeOnFeet && f >= B.whole) bodyArcs('rest', 1, a);
  }
  // The green reaches him: it builds over his whole figure, then snaps into his eyes.
  const charge = f >= B.eyes + 6 && f < B.wide ? kf(f, [[B.eyes + 6, 0], [B.eyes + 26, 5], [B.eyes + 32, 2], [B.wide, 1]]) : 0;
  bodyArcs('ch', Math.round(charge));
  if (f >= B.stare + 8 && f < B.run) bodyArcs('st', 1);
  if (f >= B.commit && f < B.snapBack) bodyArcs('cm', 2);
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
  // The ribbon: the whole curve so far, staying on the air after the hit so its shape can be read.
  const ribbonA = f < B.tump ? 1 : env(f, B.tump, B.tump, B.tump + 14, B.deadpan);
  const tr = slipperTrail(f).filter((p) => depth(v, p) > 0.6).map((p) => project(v, p) as Pt);
  if (tr.length > 1 && ribbonA > 0) {
    const d = tr.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join('');
    fx.push(<g key="ribbon" opacity={ribbonA} fill="none" strokeLinecap="round" strokeLinejoin="round">
      <path d={d} stroke="#F6C22B" strokeWidth={22} opacity={0.4} filter="url(#glow)" />
      <path d={d} stroke="#F4E23A" strokeWidth={8} />
      <path d={d} stroke="#FFFBD0" strokeWidth={3} />
    </g>);
    // Crackle off the ribbon's head, where it is being bent.
    if (f < B.tump) {
      const h = tr[tr.length - 1];
      for (let i = 0; i < 2; i++) { const back = tr[Math.max(0, tr.length - 5 - i * 4)]; fx.push(<Crackle key={`rh${i}`} a={h} b={[back[0] + (i ? 26 : -26), back[1] + (i ? -20 : 20)]} seed={`${seed('rh')}${i}`} w={5} branches={1} glow={0.8} />); }
    }
  }
  // Its shadow on the court, which is what makes the shape legible from above.
  if (ribbonA > 0 && f >= B.snap && f < B.closeOn) {
    const sh: V3[] = slipperTrail(f).map((p) => [p[0], 0.14, p[2]] as V3);
    fx.unshift(<path key="ribShadow" d={line(v, sh)} stroke="#3A1020" strokeWidth={7} fill="none" opacity={0.28 * ribbonA} strokeLinecap="round" />);
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
    const [hx, hy] = project(v, [z.x, 0.25, z.z]);
    const s = scaleAt(v, [z.x, 0.4, z.z]);
    const g = echoAt(Math.max(B.run, f - 7 - stage * 2));
    const [bx0, by0] = project(v, [g.x, 0.12, g.z]);
    for (let i = 0; i < stage; i++) fx.push(<Bolt key={`m${i}`} pts={boltPoints([hx, hy + i * s * 0.08], [bx0, by0 + i * s * 0.05], `${seed('mt')}${i}`, 3, 0.1)} w={Math.max(3, s * 0.02) + stage} glow={0.5} />);
  }
  // The angle he has found: the whole curve draws itself in the air from his hand, and holds.
  const aimT = kf(f, [[B.wide + 18, 0], [B.snap - 10, 1]]);
  const aimA = f >= B.wide + 18 && f < B.snap + 6 ? env(f, B.wide + 18, B.wide + 24, B.snap, B.snap + 6) : 0;
  if (aimA > 0) {
    const pts: string[] = [];
    for (let i = 0; i <= 48 * aimT; i++) {
      const p = curveAt(i / 48);
      if (depth(v, p) < 0.6) continue;
      const [x, y] = project(v, p);
      pts.push(`${pts.length ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`);
    }
    if (pts.length > 1) {
      fx.push(<g key="aim" opacity={aimA} fill="none" strokeLinecap="round">
        <path d={pts.join('')} stroke="#F6C22B" strokeWidth={12} opacity={0.35} filter="url(#glow)" />
        <path d={pts.join('')} stroke="#FFFBD0" strokeWidth={3.6} strokeDasharray="16 13" strokeDashoffset={-f * 2.6} />
      </g>);
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
        <Paint text="!" x={0} y={0} h={s2 * 0.6} fill="#F6C22B" outline="#3A1020" seed="shock" wobble={0.2} bounce={0} />
      </g>
      {[0, 1, 2].map((d) => {
        const t = (((f - B.jolt) * 1.6 + d * 11) % 26) / 26;
        const sx = hx + (d === 1 ? 1 : -1) * s2 * (0.32 + 0.3 * t);
        const sy = hy - s2 * 0.42 + s2 * 0.62 * t * t - s2 * 0.1 * d;
        return <path key={d} d={`M${sx} ${sy - s2 * 0.07}Q${sx + s2 * 0.05} ${sy} ${sx} ${sy + s2 * 0.035}Q${sx - s2 * 0.05} ${sy} ${sx} ${sy - s2 * 0.07}Z`} fill="#EAF6F2" stroke="#3A1020" strokeWidth={1.6} opacity={1 - t} />;
      })}
    </g>);
  }
  // The creep and the lunge: speed lines rising behind the reach.
  const rush = env(f, B.commit, B.commit + 6, B.trip, B.trip + 8);
  if (rush > 0) {
    const n: React.ReactNode[] = [];
    for (let i = 0; i < 14; i++) {
      const y = 180 + 720 * random(`sl${i}`);
      const x = 60 + 1800 * random(`sx${i}${Math.floor(f / 2)}`);
      n.push(<rect key={i} x={x} y={y} width={120 + 260 * random(`sw${i}`)} height={3 + 3 * random(`sh${i}`)} fill="#FFF3E2" opacity={0.65} />);
    }
    fx.push(<g key="rush" opacity={rush}>{n}</g>);
  }
  // Backtrack: the flash where he was, then the route lit from there to the line.
  if (f >= B.snapBack - 1 && f < B.snapBack + 14) {
    const a = env(f, B.snapBack - 1, B.snapBack, B.snapBack + 5, B.snapBack + 14);
    const at: V3 = [STAND.x, 1.0, STAND.z];
    if (depth(v, at) > 0.6) {
      const [x, y] = project(v, at);
      const s2 = scaleAt(v, at);
      const burst = kf(f, [[B.snapBack - 1, 0.2], [B.snapBack + 8, 1]]);
      if (f < B.snapBack + 6) fx.push(<g key="btb" transform={`translate(${x} ${y}) scale(${burst})`}><path d={burstPath(10, s2 * 0.2, s2 * 0.62, 'bb-ink', 0.25)} fill="#3A1020" /><path d={burstPath(10, s2 * 0.17, s2 * 0.54, 'bb-ink', 0.25)} fill="#F6C22B" /><path d={burstPath(8, s2 * 0.08, s2 * 0.28, 'bb-c', 0.25)} fill="#FFFBD0" /></g>);
      for (let i = 0; i < 3; i++) {
        const ang = (i / 3) * Math.PI * 2 + 0.4;
        fx.push(<Crackle key={`bb${i}`} a={[x, y]} b={[x + Math.cos(ang) * s2 * 0.8 * burst, y + Math.sin(ang) * s2 * 0.8 * burst]} seed={`${seed('bb')}${i}`} w={5} branches={1} glow={0.8} opacity={a} />);
      }
    }
    // The current pulls taut ahead of him, all the way off to the line it is reeling him toward.
    if (f < B.snapBack + PULL) {
      const u0 = pullU(f);
      const taut: Pt[] = [];
      for (let i = 0; i <= 10; i++) {
        const q = pullAt(u0 + ((1 - u0) * i) / 10);
        if (depth(v, [q.x, 1, q.z]) > 0.6) taut.push(project(v, [q.x, 1, q.z]));
      }
      if (taut.length > 1) fx.push(<Bolt key="taut" pts={taut.flatMap((p, i) => (i < taut.length - 1 ? boltPoints(p, taut[i + 1], `${seed('tt')}${i}`, 2, 0.14).slice(0, -1) : [p]))} w={11} glow={0.9} />);
    }
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
  for (let g = B.run + 4; g < B.scoop; g += 7) { const q = echoAt(g); puff(`rd${g}`, [q.x, 0.15, q.z], g, 14, 0.13); }
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

  return (
    <AbsoluteFill style={{ backgroundColor: '#C86A5C' }}>
      <svg width={1920} height={1080} viewBox="0 0 1920 1080">
        <Defs frame={f} />
        <defs>
          <filter id="city" x="-5%" y="-5%" width="110%" height="110%"><feGaussianBlur stdDeviation={focusAt(f)} /></filter>
          {/* The owner's approved Backtrack look: transparent faces, a hot thin contour (zack-kit-2026-10-25). */}
          <filter id="contour" x="-20%" y="-20%" width="140%" height="140%">
            <feMorphology in="SourceAlpha" operator="dilate" radius="2.4" result="fat" />
            <feComposite in="fat" in2="SourceAlpha" operator="out" result="edge" />
            <feFlood floodColor="#F6FFA0" />
            <feComposite in2="edge" operator="in" result="line" />
            <feGaussianBlur in="line" stdDeviation="3.5" result="soft" />
            <feMerge><feMergeNode in="soft" /><feMergeNode in="line" /></feMerge>
          </filter>
        </defs>
        {blur && (
          <defs>
            <filter id="whip" x="-5%" y="-5%" width="110%" height="110%"><feGaussianBlur stdDeviation={`${bx} ${by}`} /></filter>
          </defs>
        )}
        <g transform={`rotate(${v.roll} 960 540)`} filter={blur ? 'url(#whip)' : undefined}>
          <g filter="url(#boilSoft)">
            <Sky v={v} />
            <Clouds v={v} f={f} />
            <Ground v={v} tally={f >= INTRO ? 1 : kf(f, [[B.one + 2, 0], [B.one + 12, 1]])} />
          </g>
          <g filter="url(#city)">{city.map((it, i) => <g key={i}>{it.node}</g>)}</g>
          {items.map((it, i) => <g key={i}>{it.node}</g>)}
          {echoes}
          {fx}
        </g>
        <rect width={1920} height={1080} fill="#FFB060" opacity={0.06} style={{ mixBlendMode: 'screen' }} />
        <rect width={1920} height={1080} fill="url(#vignette)" opacity={0.38} />
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
          <g opacity={tumpWord} transform={`translate(${icx + 40} ${Math.max(260, icy - 220)}) rotate(-7) scale(${kf(f, [[B.tump + 5, 0.4], [B.tump + 9, 1.2], [B.tump + 13, 1]])})`}>
            <Paint text="TUMP!" x={0} y={0} h={170} fill="#F6C22B" outline="#3A1020" shadow="#980715" seed="tump" wobble={0.4} bounce={0.3} />
          </g>
        )}
        <rect width={1920} height={1080} filter="url(#grain)" opacity={0.3} />
      </svg>
    </AbsoluteFill>
  );
};

export { line };
