import React, { useEffect, useState } from 'react';
import { AbsoluteFill, continueRender, delayRender, random, staticFile, useCurrentFrame } from 'remotion';
import { Defs } from '../lib/Defs';
import { kf } from '../lib/kf';
import { env, INTRO } from './time';
import { B } from './beats';
import { ActorImage, drawActor, drawKuro, drawProp, Light, useActor, useKuro, useProp } from '../three/actor';
import { apparent, depth, line, PERSON_SCALE, project, scaleAt, V3, View } from '../phaister/view';
import { Item, setNight } from '../phaister/set';
import { burstPath } from '../art/burst';
import { Paint } from '../phaister/lettering';
import { City, Ground, lampAt, Sky, Street, strobeAt, Viaduct } from './set';
import { Heavens } from './city';
import { camAt, focusAt, panSpeed } from './camera';
import { canAt, flyingAt, Frame, GUARD, kuroAt, nemu, phaister, RING, SKID_FROM, SLIP_REST, STOP, throwTrail } from './perform';

const useFonts = () => {
  const [handle] = useState(() => delayRender('fonts'));
  useEffect(() => {
    const faces = [new FontFace('Darumadrop One', `url(${staticFile('fonts/DarumadropOne-Regular.ttf')})`), new FontFace('Paalalabas', `url(${staticFile('fonts/PaalalabasDisplayWide.otf')})`)];
    Promise.all(faces.map((x) => x.load()))
      .then((l) => { l.forEach((x) => (document.fonts as unknown as { add: (y: FontFace) => void }).add(x)); continueRender(handle); })
      .catch(() => continueRender(handle));
  }, [handle]);
};

const WITCH = '#F444D4';
const WITCH_CORE = '#FFD6F4';
/** Sodium from above, plum in the shade, her pink on the rims (LORE: selective shop light). */
const lightOf = (lamp: number): Light => ({ dir: [-0.32, 0.72, 0.4], lit: lamp > 0.4 ? '#FFDDB0' : '#B9A0C0', shade: '#6E4C78', rim: '#FF9CE6', rim_strength: 0.75 });

const FOCUS = 0.5;
const CAN_M_PER_UNIT = 0.62 / 0.383;

const figure = (b: NonNullable<ReturnType<typeof useActor>>, v: View, p: Frame, light: Light, res = 1.35) => {
  const S = PERSON_SCALE;
  const focusY = FOCUS * S;
  const ppu = scaleAt(v, [p.x, focusY, p.z]) * S;
  const ap = apparent(v, [p.x, 0, p.z], p.yaw, focusY);
  const [fx, fy] = project(v, [p.x, focusY, p.z]);
  // ⚠️ From the floor while the feet are in frame: see `figure` in bgc/BgcScene.tsx.
  const footY = project(v, [p.x, 0.02, p.z])[1];
  const onFloor = Math.max(0, Math.min(1, (1300 - footY) / 220));
  const floorPitch = Math.atan2(v.y, Math.hypot(v.x - p.x, v.z - p.z)) / (Math.PI / 180);
  // Capped: past this a low fringe or a hat brim covers the face, and the flat faces are the performance.
  const pitch = ap.pitch + Math.max(0, Math.min(9, floorPitch) - ap.pitch) * onFloor;
  const img = drawActor(b, { x: fx, y: fy, ppu, focus: FOCUS, reach: 0.72, yaw: ap.yaw, pitch, layers: p.layers, samples: p.samples, lift: p.lift, face: p.face, eyeGlow: p.eyeGlow, slipper: p.slipper, res, light: { ...light, flash: 0, flashColour: '#F6FFA0' } });
  return { img, roll: ap.roll, fx, fy };
};

const shadowOf = (v: View, x: number, z: number, r: number) => {
  const pts: V3[] = [];
  for (let i = 0; i <= 18; i++) {
    const a = (i / 18) * 6.283;
    pts.push([x + r * Math.cos(a) + 0.1, 0.012, z + r * 0.72 * Math.sin(a) + 0.12]);
  }
  const d = line(v, pts);
  return d ? <path d={d} fill="#140A1C" opacity={0.45} /> : null;
};

/** A ring of her lunar writing on the floor. Fictional, per CHARACTER_ORIGINS.md. */
const Sigil: React.FC<{ v: View; x: number; z: number; r: number; a: number; f: number; draw?: number }> = ({ v, x, z, r, a, f, draw = 1 }) => {
  const ring = (q: number, n = 56) => {
    const pts: V3[] = [];
    for (let i = 0; i <= n * draw; i++) {
      const t = (i / n) * 6.283 + f * 0.03;
      pts.push([x + Math.cos(t) * r * q, 0.02, z + Math.sin(t) * r * q]);
    }
    return pts;
  };
  const ticks: React.ReactNode[] = [];
  for (let i = 0; i < 8; i++) {
    const t = (i / 8) * 6.283 - f * 0.04;
    ticks.push(<path key={i} d={line(v, [[x + Math.cos(t) * r * 0.74, 0.02, z + Math.sin(t) * r * 0.74], [x + Math.cos(t + 0.2) * r * 0.88, 0.02, z + Math.sin(t + 0.2) * r * 0.88]])} stroke={WITCH_CORE} strokeWidth={3} />);
  }
  return <g opacity={a} fill="none" strokeLinecap="round">
    <path d={line(v, ring(1))} stroke={WITCH} strokeWidth={12} opacity={0.4} filter="url(#glow)" />
    <path d={line(v, ring(1))} stroke={WITCH_CORE} strokeWidth={3.4} />
    <path d={line(v, ring(0.66))} stroke={WITCH} strokeWidth={2.4} />
    {draw >= 1 && ticks}
  </g>;
};

/** A blink: the seam in the air, then it shuts. */
const Rift: React.FC<{ x: number; y: number; s: number; open: number; seed: string }> = ({ x, y, s, open, seed }) => {
  if (open <= 0) return null;
  const sp: React.ReactNode[] = [];
  for (let i = 0; i < 10; i++) {
    const a = random(`${seed}${i}`) * 6.283;
    const q = s * (0.3 + 0.9 * (1 - open) + 0.3 * random(`${seed}q${i}`));
    const px = x + Math.cos(a) * q;
    const py = y + Math.sin(a) * q * 1.3;
    sp.push(<rect key={i} x={px} y={py} width={s * 0.05} height={s * 0.05} fill={i % 3 ? WITCH : WITCH_CORE} transform={`rotate(45 ${px} ${py})`} />);
  }
  return <g opacity={Math.min(1, open * 1.6)}>
    <ellipse cx={x} cy={y} rx={s * 0.42 * open} ry={s * 1.2} fill={WITCH} opacity={0.35} filter="url(#glow)" />
    <ellipse cx={x} cy={y} rx={s * 0.13 * open} ry={s * 1.05} fill="#1A0620" stroke={WITCH_CORE} strokeWidth={3} />
    {sp}
  </g>;
};

const Burst: React.FC<{ x: number; y: number; s: number }> = ({ x, y, s }) => (
  <g transform={`translate(${x} ${y})`}>
    <path d={burstPath(14, s * 0.5, s * 1.08, 'k-ink', 0.25)} fill="#1A0620" />
    <path d={burstPath(14, s * 0.44, s, 'k-ink', 0.25)} fill={WITCH} />
    <path d={burstPath(11, s * 0.24, s * 0.56, 'k-core', 0.25)} fill="#FFF3FB" />
  </g>
);

export const ErmitaScene: React.FC<{ part: 'intro' | 'idle' }> = ({ part }) => {
  useFonts();
  const frame = useCurrentFrame();
  const f = part === 'idle' ? INTRO + frame : frame / 2;
  setNight(0);
  const pb = useActor('phaister-redesign', true);
  const nb = useActor('nemu-redesign');
  const kk = useKuro();
  const can = useProp('lata_metal');
  const v = camAt(f);
  const P = phaister(f);
  const N = nemu(f);
  const C = canAt(f);
  const light = lightOf(lampAt(f));
  // Her night: everything but her pink comes down while the ring burns.
  const night = f >= INTRO ? 0 : kf(f, [[B.raise, 0], [B.raise + 16, 0.42], [B.settle, 0.42], [B.home, 0]]);
  const strobe = strobeAt(f);

  const city: Item[] = City(v, f).sort((a, b) => b.z - a.z);
  const items: Item[] = [...Viaduct(v, f), ...Street(v, f)];
  const push = (node: React.ReactNode, at: V3, bias = 0) => items.push({ z: depth(v, at) + bias, node });
  const front = (x: number, z: number) => depth(v, [x, 1, z]) > 0.7;
  let herHand: [number, number] | null = null;

  if (pb && P.visible > 0 && front(P.x, P.z)) {
    const d = figure(pb, v, P, night > 0.1 ? { ...light, rim: '#FF7AE6', rim_strength: 1 } : light, 1.5);
    herHand = d.img.at('forearm-right', [-0.085, 0, 0]);
    push(<g key="her"><g>{shadowOf(v, P.x, P.z, 0.72)}</g><g transform={`rotate(${d.roll} ${d.fx} ${d.fy})`}><ActorImage d={d.img} /></g></g>, [P.x, 1, P.z]);
  }
  if (nb && front(N.x, N.z)) {
    const d = figure(nb, v, N, light);
    push(<g key="nemu"><g>{shadowOf(v, N.x, N.z, 0.72)}</g><g transform={`rotate(${d.roll} ${d.fx} ${d.fy})`}><ActorImage d={d.img} /></g></g>, [N.x, 1, N.z]);
  }
  const K = kuroAt(f, N);
  if (kk && depth(v, K.p) > 0.7) {
    const ap = apparent(v, K.p, 0, 0.45);
    const [x, y] = project(v, [K.p[0], K.p[1] + 0.45, K.p[2]]);
    const ppu = scaleAt(v, K.p) * (0.85 / kk.height);
    const img = drawKuro(kk, { x, y, ppu, yaw: K.yaw, pitch: ap.pitch, face: { ...K.face, puff: K.puff }, light: { ...light, lit: '#F6E6FF', shade: '#8A6AA8' } });
    push(<g key="kuro" opacity={0.95}><ActorImage d={img} /></g>, K.p, -0.1);
  }
  if (can && depth(v, [C.x, 0.3, C.z]) > 0.6) {
    const at: V3 = [C.x, C.hop, C.z];
    const ppu = scaleAt(v, at) * CAN_M_PER_UNIT;
    const ap = apparent(v, [C.x, 0, C.z], 22, 0.3);
    const [x, y] = project(v, [at[0], at[1] + 0.3, at[2]]);
    const img = drawProp(can, { x, y, ppu, focus: 0.19, reach: 0.34, yaw: ap.yaw, pitch: ap.pitch, rot: C.rot, lift: [0, 0.125 * C.lying, 0], res: 1.5, light });
    push(<g key="can"><ActorImage d={img} /></g>, at);
  }
  const fl = flyingAt(f);
  if (fl && pb && depth(v, fl.p) > 0.6) {
    const ppu = scaleAt(v, fl.p) * PERSON_SCALE * (fl.big ? 1.8 : 1);
    const [x, y] = project(v, fl.p);
    const img = drawActor(pb, { x, y, ppu, focus: 0, target: [0, 0, 0], reach: 0.22, yaw: 0, pitch: apparent(v, fl.p, 0, 0).pitch, only: 'slipper', slipper: { at: 'free', pos: [0, 0, 0], rot: fl.rot }, light, res: 1.4 });
    push(<g key="slipper"><ActorImage d={img} /></g>, fl.p, -0.2);
  }
  items.sort((a, b) => b.z - a.z);

  // ---------------------------------------------------------------- effects
  const fx: React.ReactNode[] = [];
  const under: React.ReactNode[] = [];
  const seed = `${Math.floor(f / 2)}`;
  // The throw's ribbon: you can read where it went.
  const ribA = f < B.klang ? 1 : env(f, B.klang, B.klang, B.klang + 8, B.tada + 8);
  const tr = throwTrail(f).filter((p) => depth(v, p) > 0.6).map((p) => project(v, p));
  if (f >= B.flick && tr.length > 1 && ribA > 0) {
    const d = tr.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join('');
    fx.push(<g key="rib" opacity={ribA} fill="none" strokeLinecap="round"><path d={d} stroke={WITCH} strokeWidth={24} opacity={0.4} filter="url(#glow)" /><path d={d} stroke={WITCH_CORE} strokeWidth={6} /></g>);
  }
  // The ring: the size of the lot, drawing itself round Nemu.
  const draw = kf(f, [[B.ring, 0], [B.ring + 44, 1]]);
  const ringA = f >= INTRO ? 0 : env(f, B.ring, B.ring + 6, B.slam + 12, B.settle);
  if (ringA > 0) {
    under.push(<Sigil key="big" v={v} x={RING.x} z={RING.z} r={5.6} a={ringA} f={f} draw={draw} />);
    under.push(<Sigil key="small" v={v} x={RING.x} z={RING.z} r={2.8} a={ringA * 0.85} f={-f} draw={draw} />);
    under.push(<Sigil key="tiny" v={v} x={RING.x} z={RING.z} r={1.1} a={ringA} f={f * 2} draw={draw} />);
  }
  // A faint mark of hers stays on the can.
  const stageA = f >= INTRO ? 0.3 : kf(f, [[B.home, 0], [B.fold, 0.3]]);
  if (stageA > 0) under.push(<Sigil key="stage" v={v} x={0} z={0} r={1.2} a={stageA} f={f} />);
  // It goes off: a column of her light where Nemu stood, thrown up past the top of the frame.
  const beam = env(f, B.slam - 1, B.slam, B.slam + 8, B.slam + 30);
  if (beam > 0 && depth(v, [RING.x, 1, RING.z]) > 0.7) {
    const [bx0, by0] = project(v, [RING.x, 0, RING.z]);
    const s = scaleAt(v, [RING.x, 0, RING.z]);
    fx.push(<g key="beam" opacity={beam}>
      <path d={`M${bx0 - s * 1.5} ${by0}L${bx0 + s * 1.5} ${by0}L${bx0 + s * 2.6} -60L${bx0 - s * 2.6} -60Z`} fill={WITCH} opacity={0.55} filter="url(#glow)" />
      <path d={`M${bx0 - s * 0.8} ${by0}L${bx0 + s * 0.8} ${by0}L${bx0 + s * 1.3} -60L${bx0 - s * 1.3} -60Z`} fill={WITCH_CORE} opacity={0.9} />
    </g>);
  }
  for (const [at, x, z, big] of [[B.klang + 2, 0, 0, 6], [B.slam, RING.x, RING.z, 11]] as const) {
    if (f < at || f >= at + 28) continue;
    const t = (f - at) / 28;
    [0, 0.2].forEach((lag, n) => {
      const q = Math.max(0, t - lag);
      if (q <= 0) return;
      const ring: V3[] = [];
      for (let i = 0; i <= 40; i++) ring.push([x + Math.cos((i / 40) * 6.283) * (0.4 + big * Math.sqrt(q)), 0.02, z + Math.sin((i / 40) * 6.283) * (0.4 + big * Math.sqrt(q))]);
      fx.push(<path key={`rg${at}${n}`} d={line(v, ring)} stroke={WITCH_CORE} strokeWidth={12 * (1 - q)} fill="none" opacity={(1 - q) * 0.9} />);
    });
  }
  // The blink: out by Nemu's hand, in on the line.
  for (const [at, x, z] of [[B.blink, P.x, P.z], [B.skid, SKID_FROM.x, SKID_FROM.z]] as const) {
    const open = env(f, at - 2, at, at + 3, at + 16);
    if (open > 0 && depth(v, [x, 1, z]) > 0.7) {
      const [rx, ry] = project(v, [x, 1.2, z]);
      fx.push(<Rift key={`rift${at}`} x={rx} y={ry} s={scaleAt(v, [x, 1.2, z]) * 1.4} open={open} seed={`rf${at}${seed}`} />);
    }
  }
  if (f >= B.blink && f < B.blink + 3) fx.push(<rect key="blk" x={-100} y={-100} width={2200} height={1300} fill={WITCH_CORE} opacity={0.8} />);
  // Speed lines down the run and the chase.
  const rush = Math.max(env(f, B.run + 4, B.run + 10, B.scoop - 6, B.scoop), env(f, B.chase, B.chase + 4, B.blink - 2, B.blink));
  if (rush > 0) {
    for (let i = 0; i < 12; i++) {
      const y = 140 + 800 * random(`sl${i}`);
      const x = 40 + 1800 * random(`sx${i}${seed}`);
      fx.push(<rect key={`sl${i}`} x={x} y={y} width={120 + 240 * random(`sw${i}`)} height={3 + 3 * random(`sh${i}`)} fill="#FFF3E2" opacity={0.55 * rush} />);
    }
  }
  // Dust: the slide, the skid, Nemu's long ride.
  const puff = (key: string, x0: number, z0: number, start: number, life: number, size: number) => {
    if (f < start || f >= start + life || depth(v, [x0, 0.1, z0]) < 0.6) return;
    const t = (f - start) / life;
    const [x, y] = project(v, [x0, 0.1, z0]);
    const s2 = scaleAt(v, [x0, 0.1, z0]);
    for (let i = 0; i < 6; i++) {
      const ang = (i / 6) * Math.PI + random(`${key}a${i}`) * 0.5;
      const q = s2 * size * (0.35 + 1.1 * t);
      fx.push(<circle key={`${key}${i}`} cx={x + Math.cos(ang) * q * 1.4} cy={y - Math.sin(ang) * q * 0.55} r={q * 0.55} fill="#BFAEB4" opacity={0.6 * (1 - t)} />);
    }
  };
  puff('slide', SLIP_REST.x + 0.2, SLIP_REST.z, B.scoop - 5, 24, 0.4);
  puff('skid', P.x, P.z, B.skid, 18, 0.4);
  puff('land', GUARD.x, GUARD.z, B.slam + 20, 22, 0.5);
  puff('stomp', P.x, P.z, B.stomp + 2, 12, 0.25);
  // Her "!" when she turns into Nemu; Kuro's when the ring closes.
  const bang = (key: string, x: number, y: number, z: number, at: number, until: number, h: number) => {
    if (f < at || f >= until || depth(v, [x, y, z]) < 0.7) return;
    const [bx, by] = project(v, [x, y, z]);
    const q = scaleAt(v, [x, y, z]);
    fx.push(<g key={key} transform={`translate(${bx} ${by}) rotate(10) scale(${kf(f, [[at, 0.2], [at + 4, 1.2], [at + 8, 1]])})`}><Paint text="!" x={0} y={0} h={q * h} fill="#FFE27A" outline="#1A0620" seed={key} wobble={0.2} bounce={0} /></g>);
  };
  bang('pb', P.x - 0.7, 2.3, P.z, B.faceOff, B.chase, 0.6);
  bang('kb', K.p[0] + 0.34, K.p[1] + 0.85, K.p[2], B.ring + 4, B.slam, 0.55);
  // Her joy: her own sparks at each hop.
  if (f >= B.laugh && f < B.laugh + 34) {
    const t = (f - B.laugh) / 34;
    const [hx, hy] = project(v, [P.x, 2.3, P.z]);
    const s = scaleAt(v, [P.x, 2, P.z]);
    for (let i = 0; i < 16; i++) {
      const a = random(`j${i}`) * 6.283;
      const q = s * (0.5 + 1.2 * random(`jq${i}`)) * t;
      const px = hx + Math.cos(a) * q;
      const py = hy + Math.sin(a) * q + s * 0.9 * t * t;
      fx.push(<rect key={`j${i}`} x={px} y={py} width={s * 0.07} height={s * 0.07} transform={`rotate(${45 + f * 8} ${px} ${py})`} fill={i % 2 ? WITCH : '#FFD86A'} opacity={1 - t} />);
    }
  }

  const klangWord = env(f, B.klang + 4, B.klang + 5, B.klang + 24, B.klang + 30);
  const [icx, icy] = project(v, [0, 0.42, 0]);
  const sp = panSpeed(f);
  const bx = sp.x > 24 ? Math.min(14, (sp.x - 24) * 0.22) : 0;
  const by = sp.y > 24 ? Math.min(10, (sp.y - 24) * 0.22) : 0;
  const blur = bx > 0.5 || by > 0.5;

  return (
    <AbsoluteFill style={{ backgroundColor: '#1B1030' }}>
      <svg width={1920} height={1080} viewBox="0 0 1920 1080">
        <Defs frame={f} />
        <defs>
          <filter id="city" x="-5%" y="-5%" width="110%" height="110%"><feGaussianBlur stdDeviation={focusAt(f)} /></filter>
          {blur && <filter id="whip" x="-5%" y="-5%" width="110%" height="110%"><feGaussianBlur stdDeviation={`${bx} ${by}`} /></filter>}
        </defs>
        <g transform={`rotate(${v.roll} 960 540)`} filter={blur ? 'url(#whip)' : undefined}>
          <Sky v={v} />
          <Heavens v={v} f={f} />
          <g filter="url(#city)">{city.map((it, i) => <g key={i}>{it.node}</g>)}</g>
          <Ground v={v} f={f} />
          {under}
          {items.map((it, i) => <g key={i}>{it.node}</g>)}
          {fx}
        </g>
        {strobe > 0 && <rect width={1920} height={1080} fill="#E9D8FF" opacity={0.2 * strobe} style={{ mixBlendMode: 'screen' }} />}
        {night > 0 && <rect width={1920} height={1080} fill="#26083A" opacity={night} style={{ mixBlendMode: 'multiply' }} />}
        {f >= B.slam && f < B.slam + 2 && <rect width={1920} height={1080} fill={WITCH_CORE} opacity={0.9} />}
        <rect width={1920} height={1080} fill="#FF60C0" opacity={0.035} style={{ mixBlendMode: 'screen' }} />
        <rect width={1920} height={1080} fill="url(#vignette)" opacity={0.5} />
        {f >= B.klang && f < B.klang + 2 && <rect width={1920} height={1080} fill={WITCH_CORE} />}
        {f >= B.klang + 2 && f < B.klang + 4 && <g><rect width={1920} height={1080} fill="#14041C" opacity={0.92} /><Burst x={icx} y={icy} s={340} /></g>}
        {f >= B.klang + 4 && f < B.klang + STOP && <Burst x={icx} y={icy} s={280} />}
        {klangWord > 0 && <g opacity={klangWord} transform={`translate(${Math.min(1250, icx + 40)} ${Math.max(260, icy - 200)}) rotate(-7) scale(${kf(f, [[B.klang + 4, 0.4], [B.klang + 8, 1.15], [B.klang + 12, 1]])})`}>
          <Paint text="KLANG!" x={0} y={0} h={140} fill={WITCH_CORE} outline="#1A0620" shadow="#7A1A8C" seed="klang" wobble={0.4} bounce={0.3} />
        </g>}
        <rect width={1920} height={1080} filter="url(#grain)" opacity={0.26} />
      </svg>
    </AbsoluteFill>
  );
};
