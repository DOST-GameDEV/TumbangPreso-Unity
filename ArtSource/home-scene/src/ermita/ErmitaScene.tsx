import React, { useEffect, useState } from 'react';
import { AbsoluteFill, continueRender, delayRender, Img, random, staticFile, useCurrentFrame } from 'remotion';
import { Defs } from '../lib/Defs';
import { GRADE_STYLE, GradeCorners, GradeDefs } from '../lib/grade';
import { kf } from '../lib/kf';
import { BLEND, platesOf } from '../lib/plates';
import { env, IDLE } from './time';
import { B } from './beats';
import { ActorImage, drawActor, Light, useActor } from '../three/actor';
import { apparent, depth, line, PERSON_SCALE, project, scaleAt, V3, View } from '../phaister/view';
import { setNight } from '../phaister/set';
import { burstPath } from '../art/burst';
import { camAt, panSpeed } from './camera';
import { ARRIVE, canAt, FLAT, Frame, kuroAt, nemu, phaister, SLIP_REST, STOP } from './perform';

const { plateAt, preAt } = platesOf('phaister');

const useFonts = () => {
  const [handle] = useState(() => delayRender('fonts'));
  useEffect(() => {
    const faces = [new FontFace('Darumadrop One', `url(${staticFile('fonts/DarumadropOne-Regular.ttf')})`)];
    Promise.all(faces.map((x) => x.load()))
      .then((l) => { l.forEach((x) => (document.fonts as unknown as { add: (y: FontFace) => void }).add(x)); continueRender(handle); })
      .catch(() => continueRender(handle));
  }, [handle]);
};

/** The game's own: `VoodooCursePresenter.DrainHue`, the swarm's violet smoke (`PhaisterSwarm`). */
const DRAIN = '#FF384D';
const DRAIN_CORE = '#FFE2E0';
const SMOKE = '#9460D1';
const INK = '#1A0620';
const DAY: Light = { dir: [0.45, 0.6, 0.66], lit: '#FFF8EE', shade: '#A9A2B6', rim: '#FFF4DC', rim_strength: 0.42 };
const FOCUS = 0.5;
/** The fist in the forearm bone's space, on the redesigned rigs. */
const FIST_L: [number, number, number] = [0.085, 0, 0];

const figure = (b: NonNullable<ReturnType<typeof useActor>>, v: View, p: Frame, res: number, flash = 0, flashColour = '#ffffff', at?: { x: number; z: number }) => {
  const S = PERSON_SCALE;
  const x = at?.x ?? p.x;
  const z = at?.z ?? p.z;
  const focusY = FOCUS * S;
  const ppu = scaleAt(v, [x, focusY, z]) * S;
  const ap = apparent(v, [x, 0, z], p.yaw, focusY);
  const [fx, fy] = project(v, [x, focusY, z]);
  const img = drawActor(b, { x: fx, y: fy, ppu, focus: FOCUS, reach: 0.72, yaw: ap.yaw, pitch: ap.pitch, layers: p.layers, samples: p.samples, lift: p.lift, face: 'rest', slipper: { at: 'none' }, res, light: { ...DAY, flash, flashColour } });
  return { img, roll: ap.roll, fx, fy };
};

const Burst: React.FC<{ x: number; y: number; s: number; c?: string; core?: string }> = ({ x, y, s, c = DRAIN, core = '#FFF3FB' }) => (
  <g transform={`translate(${x} ${y})`}>
    <path d={burstPath(14, s * 0.5, s * 1.08, 'k-ink', 0.25)} fill={INK} />
    <path d={burstPath(14, s * 0.44, s, 'k-ink', 0.25)} fill={c} />
    <path d={burstPath(11, s * 0.24, s * 0.56, 'k-core', 0.25)} fill={core} />
  </g>
);
/** Lettering in Darumadrop, the owner's pick. */
const Word: React.FC<{ text: string; h: number; fill?: string }> = ({ text, h, fill = DRAIN_CORE }) => (
  <text x={0} y={0} textAnchor="middle" fontFamily="Darumadrop One" fontSize={h} fill={fill} stroke={INK} strokeWidth={h * 0.12} strokeLinejoin="round" style={{ paintOrder: 'stroke' }}>{text}</text>
);

/** Two held frames at each of the film's peaks: ink, then flipped. Only ever at `klang`, `eyes`, `gulp` and `drained`. */
const impactAt = (f: number) => {
  for (const [at, n] of [[B.klang + 2, 1], [B.eyes + 5, 1], [B.gulp, 2], [B.drained + 6, 1]] as const) {
    const t = f - at;
    if (t >= 0 && t < 1) return { stage: 1, big: n };
    if (t >= 1 && t < 2) return { stage: 2, big: n };
  }
  return { stage: 0, big: 0 };
};

export const ErmitaScene: React.FC<{ part: 'intro' | 'idle' }> = ({ part }) => {
  useFonts();
  const frame = useCurrentFrame();
  const f = part === 'idle' ? 1350 + frame : frame / 2;
  setNight(0);
  const pb = useActor('phaister-redesign', true);
  const nb = useActor('nemu-redesign');
  const v = camAt(f);
  const P = phaister(f);
  const N = nemu(f);
  const C = canAt(f);
  const K = kuroAt(f);

  const fx: React.ReactNode[] = [];
  const under: React.ReactNode[] = [];
  const inFront = (p: V3) => depth(v, p) > 0.6;

  // ⚠️ THE CAST IS IN THE PLATE, ON THE GAME'S OWN SHADER. These figures are never shown: they only say where a bone lands on screen, for the effects.
  const curse = f >= B.turn + 10 && f < B.drained + 18;
  let palm: [number, number] | null = null;
  let palmWorld: V3 = [P.x, 1.3, P.z - 0.8];
  if (curse && pb && inFront([P.x, 1, P.z])) {
    const d = figure(pb, v, P, 0.3);
    palm = d.img.at('forearm-left', FIST_L);
    const ax = N.x - P.x;
    const az = N.z - P.z;
    const al = Math.hypot(ax, az) || 1;
    palmWorld = [P.x + (ax / al) * 0.8, 1.2, P.z + (az / al) * 0.8];
  }

  // ⚠️ HER EYES ARE LIGHT, NOT PAINT (owner: "what are those glowing eyes", on a mid pink that read as eyeshadow). The plate turns her own eye paint
  // hot white and never changes its shape; this is the light coming off it. Her witch's magenta only, and only while she is casting.
  if (P.eyeGlow > 0.02 && pb && inFront([P.x, 1.4, P.z])) {
    const d = figure(pb, v, P, 0.3);
    const q = scaleAt(v, [P.x, 1.4, P.z]);
    // Read off her glb: the middle of each eye, in the head bone's space.
    ([[0.066, 0.106, 0.135], [-0.066, 0.106, 0.135]] as [number, number, number][]).forEach((e, i) => {
      const [x, y] = d.img.at('head', e);
      fx.push(<ellipse key={`eye${i}`} cx={x} cy={y} rx={q * 0.13} ry={q * 0.065} fill="url(#eyeglow)" opacity={0.75 * P.eyeGlow} style={{ mixBlendMode: 'screen' }} />);
    });
  }

  // ⚠️ THE EFFECTS ARE OVER THE PICTURE, SO THEY CANNOT PASS BEHIND ANYONE. What Phaister or Nemu stands in front of is left out while they cover it.
  const covers = (who: { x: number; z: number }, p: V3) => {
    if (depth(v, p) <= depth(v, [who.x, 1, who.z])) return false;
    const [cx] = project(v, [who.x, 1, who.z]);
    return Math.abs(project(v, p)[0] - cx) < scaleAt(v, [who.x, 1, who.z]) * 0.9;
  };
  const covered = (p: V3, skipNemu = false) => (P.visible > 0 && covers(P, p)) || (!skipNemu && covers(N, p));

  // ---------------------------------------------------------------- ⚠️ THE CRUX: Curse: Drain (`VoodooSoulDraw`)
  // The soul is pulled out of the victim, not a cord: a see-through ghost of their own shape, lit in the curse's colour, dragged toward her,
  // with motes tearing off it and sucked in braided currents to her palm. The game's goes into her doll; the doll is cut from this film (owner),
  // so here it goes into her open hand. Crimson, as in the game. This is the only big effect in the film.
  const pull = f >= B.slow - 4 && f < B.gulp + 9;
  const fill = kf(f, [[B.slow - 4, 0], [B.gulp, 1]]);
  if (pull && nb && inFront([N.x, 1, N.z])) {
    const gulped = f >= B.gulp ? (f - B.gulp) / 9 : 0;
    const out = 0.3 + 0.95 * fill + 1.5 * gulped;
    const dx = palmWorld[0] - N.x;
    const dz = palmWorld[2] - N.z;
    const dd = Math.hypot(dx, dz) || 1;
    const step = Math.min(out, dd);
    const at = { x: N.x + (dx / dd) * step, z: N.z + (dz / dd) * step };
    const a = Math.min(1, fill * 4) * (1 - gulped);
    if (a > 0) {
      const d = figure(nb, v, N, 1.2, 1, DRAIN, at);
      const s = 1 - 0.6 * gulped;
      fx.push(<g key="soul" transform={`rotate(${d.roll} ${d.fx} ${d.fy}) translate(${d.fx} ${d.fy}) scale(${s}) translate(${-d.fx} ${-d.fy})`} style={{ mixBlendMode: 'screen' }}>
        <g opacity={0.5 * a}><ActorImage d={d.img} /></g>
        <g opacity={a} filter="url(#drainline)"><ActorImage d={d.img} /></g>
      </g>);
    }
    // The motes: separate specks, faster and smaller as they go in, in two currents twisting round each other.
    if (palm && f < B.gulp + 4) {
      const [ex, ey] = project(v, [at.x, 1.1, at.z]);
      const q = scaleAt(v, [at.x, 1.1, at.z]);
      const rate = f >= B.slow && f < B.gulp ? 0.06 : 0.3;
      for (let i = 0; i < 60; i++) {
        const t = (random(`m${i}`) + f * rate * (0.5 + random(`mr${i}`) * 0.5)) % 1;
        const tt = t * t;
        const strand = i % 2 ? 1 : -1;
        const tw = Math.sin(t * 9 + random(`mp${i}`) * 1.2) * strand * q * 0.3 * (1 - t);
        const sx = ex + (random(`mx${i}`) - 0.5) * q * 0.9 * (1 - t);
        const sy = ey + (random(`my${i}`) - 0.5) * q * 1.3 * (1 - t);
        const x = sx + (palm[0] - sx) * tt;
        const y = sy + (palm[1] - sy) * tt + tw;
        const r = q * (0.03 - 0.02 * t);
        fx.push(<circle key={`mt${i}`} cx={x} cy={y} r={Math.max(1.6, r)} fill={i % 3 ? DRAIN : DRAIN_CORE} opacity={(0.45 + 0.55 * t) * Math.min(1, fill * 5)} />);
      }
      // The light swelling at her palm.
      const g = q * (0.14 + 0.5 * fill);
      fx.push(<circle key="pg" cx={palm[0]} cy={palm[1]} r={g * 1.6} fill="url(#drainglow)" style={{ mixBlendMode: 'screen' }} />);
      fx.push(<circle key="pc" cx={palm[0]} cy={palm[1]} r={g * 0.3} fill={DRAIN_CORE} />);
    }
  }
  // The gulp: her palm flares, and a shock goes out along the floor from her feet.
  if (f >= B.gulp + 2 && f < B.gulp + 26) {
    const t = (f - B.gulp - 2) / 24;
    if (t < 1) {
      [0, 0.2].forEach((lag, n) => {
        const q = Math.max(0, t - lag);
        if (q <= 0) return;
        const ring: V3[] = [];
        for (let i = 0; i <= 40; i++) ring.push([P.x + Math.cos((i / 40) * 6.283) * (0.4 + 8 * Math.sqrt(q)), 0.02, P.z + Math.sin((i / 40) * 6.283) * (0.4 + 8 * Math.sqrt(q))]);
        under.push(<path key={`gr${n}`} d={line(v, ring)} stroke={DRAIN_CORE} strokeWidth={18 * (1 - q)} fill="none" opacity={(1 - q) * 0.9} />);
      });
      if (palm) fx.push(<g key="pf" opacity={1 - t}><Burst x={palm[0]} y={palm[1]} s={150 + 420 * Math.sqrt(t)} /></g>);
    }
  }
  // The mark: DRAIN's twisted crimson knot turning over the marked body, for its 1.5 s fuse (`VoodooCursePresenter`).
  if (f >= B.gulp + 6 && f < B.drained + 4 && nb) {
    const at: V3 = [N.x, 2.5, N.z];
    if (inFront(at)) {
      const [hx, hy] = project(v, at);
      const q = scaleAt(v, at);
      const tight = (f - B.gulp) / (B.drained - B.gulp);
      const rx = q * (0.32 - 0.12 * tight);
      const ry = q * 0.1;
      const turn = f * 0.35;
      const d = (o: number) => {
        let s = '';
        for (let i = 0; i <= 28; i++) { const t = (i / 28) * 6.283 + turn + o; s += `${i ? 'L' : 'M'}${hx + Math.cos(t) * rx} ${hy + Math.sin(t) * ry + Math.sin(t * 3) * ry * 0.6}`; }
        return s;
      };
      fx.push(<g key="knot" fill="none" strokeLinecap="round"><path d={d(0)} stroke={INK} strokeWidth={q * 0.05} /><path d={d(0)} stroke={DRAIN} strokeWidth={q * 0.03} /><path d={d(2)} stroke={DRAIN_CORE} strokeWidth={q * 0.014} /></g>);
    }
  }
  // Drained: her stamina goes, and what is left of it comes off her as she drops.
  if (f >= B.drained && f < B.drained + 26) {
    const t = (f - B.drained) / 26;
    const at: V3 = [FLAT.x, 0.6, FLAT.z];
    if (inFront(at)) {
      const q = scaleAt(v, at);
      const [x, y] = project(v, at);
      for (let i = 0; i < 18; i++) {
        const a = random(`dr${i}`) * 6.283;
        const r = q * (0.4 + 1.6 * random(`drq${i}`)) * t;
        fx.push(<circle key={`dr${i}`} cx={x + Math.cos(a) * r} cy={y + Math.sin(a) * r * 0.5 - q * 0.9 * t} r={q * 0.028} fill={i % 2 ? DRAIN : DRAIN_CORE} opacity={1 - t} />);
      }
    }
  }

  // ---------------------------------------------------------------- Teleport: violet smoke where she goes and where she knits. The moths are in the plate, on the game's own model.
  const smoke = (key: string, x0: number, z0: number, at: number) => {
    const t = (f - at) / 20;
    if (t < 0 || t >= 1 || !inFront([x0, 1, z0])) return;
    const q = scaleAt(v, [x0, 1, z0]);
    for (let i = 0; i < 5; i++) {
      const a = (i / 5) * 6.283 + random(`${key}${i}`);
      const r = q * (0.2 + 0.7 * t) * (0.7 + 0.5 * random(`${key}r${i}`));
      const [x, y] = project(v, [x0, 0.2 + (i % 4) * 0.42 + t * 0.5, z0]);
      if (!covered([x0, 1, z0], true)) fx.push(<circle key={`${key}${i}`} cx={x + Math.cos(a) * r} cy={y + Math.sin(a) * r * 0.5} r={q * (0.18 + 0.2 * t)} fill={SMOKE} opacity={0.34 * (1 - t)} />);
    }
  };
  smoke('sa', P.visible > 0 ? P.x : 0.8, P.visible > 0 ? P.z : -6.6, B.blink);
  smoke('sb', ARRIVE.x, ARRIVE.z, B.arrive - 2);

  // ---------------------------------------------------------------- dust and marks
  const puff = (key: string, x0: number, z0: number, start: number, life: number, size: number) => {
    if (f < start || f >= start + life || !inFront([x0, 0.1, z0])) return;
    const t = (f - start) / life;
    const [x, y] = project(v, [x0, 0.1, z0]);
    const s2 = scaleAt(v, [x0, 0.1, z0]);
    for (let i = 0; i < 6; i++) {
      const ang = (i / 6) * Math.PI + random(`${key}a${i}`) * 0.5;
      const q = s2 * size * (0.35 + 1.1 * t);
      fx.push(<circle key={`${key}${i}`} cx={x + Math.cos(ang) * q * 1.4} cy={y - Math.sin(ang) * q * 0.55} r={q * 0.55} fill="#EBDCBE" opacity={0.6 * (1 - t)} />);
    }
  };
  puff('slide', SLIP_REST.x + 0.3, SLIP_REST.z, B.scoop - 8, 22, 0.36);
  puff('candn', C.x, C.z, B.canDown, 14, 0.2);
  puff('arrive', ARRIVE.x, ARRIVE.z, B.arrive, 14, 0.3);
  puff('skid', -4.9, -5.2, B.cutOff, 16, 0.34);
  puff('flat', FLAT.x, FLAT.z - 0.2, B.drained + 5, 24, 0.42);
  const bang = (key: string, x: number, y: number, z: number, at: number, until: number, h: number) => {
    if (f < at || f >= until || !inFront([x, y, z])) return;
    const [bx, by] = project(v, [x, y, z]);
    const q = scaleAt(v, [x, y, z]);
    fx.push(<g key={key} transform={`translate(${bx} ${by}) rotate(10) scale(${kf(f, [[at, 0.2], [at + 4, 1.2], [at + 8, 1]])})`}><Word text="!" h={q * h} fill="#FFE27A" /></g>);
  };
  bang('kb', K[0] + 0.5, K[1] + 0.7, K[2], B.klang + 2, B.tada - 6, 0.95);
  bang('pb', P.x + 0.7, 2.3, P.z, B.notice, B.bolt + 2, 0.62);
  bang('kc', K[0] + 0.45, K[1] + 0.6, K[2], B.kuroFace + 2, B.full, 0.5);

  const klangWord = env(f, B.klang + 5, B.klang + 6, B.canDown - 4, B.canDown);
  const drainedWord = env(f, B.drained + 8, B.drained + 9, B.joy - 4, B.joy);
  const [icx, icy] = project(v, [0, 0.42, 0]);
  const sp = panSpeed(f);
  const bx = sp.x > 24 ? Math.min(14, (sp.x - 24) * 0.22) : 0;
  const by = sp.y > 24 ? Math.min(10, (sp.y - 24) * 0.22) : 0;
  const blur = bx > 0.5 || by > 0.5;
  const hit = part === 'intro' ? impactAt(f) : { stage: 0, big: 0 };
  const punch = hit.stage ? 1.05 : 1;
  const slowed = f >= B.slow && f < B.gulp;
  const hush = slowed ? Math.min(1, (f - B.slow) / 6, (B.gulp - f) / 2) : 0;
  const stopped = f >= B.klang && f < B.klang + STOP;

  return (
    <AbsoluteFill style={{ backgroundColor: '#C86A5C' }}>
      <svg width={1920} height={1080} viewBox="0 0 1920 1080">
        <Defs frame={f} />
        <GradeDefs />
        <defs>
          {/* A hot thin contour, as the game's ghost has. */}
          <filter id="drainline" x="-20%" y="-20%" width="140%" height="140%">
            <feMorphology in="SourceAlpha" operator="dilate" radius="2.4" result="fat" />
            <feComposite in="fat" in2="SourceAlpha" operator="out" result="edge" />
            <feFlood floodColor={DRAIN_CORE} />
            <feComposite in2="edge" operator="in" result="line" />
            <feGaussianBlur in="line" stdDeviation="3" result="soft" />
            <feMerge><feMergeNode in="soft" /><feMergeNode in="line" /></feMerge>
          </filter>
          <filter id="impactA" colorInterpolationFilters="sRGB"><feColorMatrix type="saturate" values="0" /><feComponentTransfer><feFuncR type="discrete" tableValues="0.09 0.09 1 1" /><feFuncG type="discrete" tableValues="0.02 0.02 0.9 0.9" /><feFuncB type="discrete" tableValues="0.08 0.08 0.88 0.88" /></feComponentTransfer></filter>
          <filter id="impactB" colorInterpolationFilters="sRGB"><feColorMatrix type="saturate" values="0" /><feComponentTransfer><feFuncR type="discrete" tableValues="1 1 0.09 0.09" /><feFuncG type="discrete" tableValues="0.22 0.22 0.02 0.02" /><feFuncB type="discrete" tableValues="0.3 0.3 0.08 0.08" /></feComponentTransfer></filter>
          <radialGradient id="eyeglow"><stop offset="0" stopColor="#FFF0FA" stopOpacity={0.95} /><stop offset="0.35" stopColor="#FF4FD0" stopOpacity={0.7} /><stop offset="1" stopColor="#FF4FD0" stopOpacity={0} /></radialGradient>
          <radialGradient id="drainglow"><stop offset="0" stopColor={DRAIN_CORE} stopOpacity={0.95} /><stop offset="0.4" stopColor={DRAIN} stopOpacity={0.55} /><stop offset="1" stopColor={DRAIN} stopOpacity={0} /></radialGradient>
          {blur && <filter id="whip" x="-5%" y="-5%" width="110%" height="110%"><feGaussianBlur stdDeviation={`${bx} ${by}`} /></filter>}
        </defs>
        <g transform={`translate(960 540) scale(${punch}) translate(-960 -540)`}>
          {/* The real map with the cast in it, recorded through this same camera (scripts/plates.mjs). It already carries the roll. */}
          <g filter={hit.stage === 1 ? 'url(#impactA)' : hit.stage === 2 ? 'url(#impactB)' : blur ? 'url(#whip)' : undefined} style={hush > 0 ? { filter: `saturate(${1 - 0.3 * hush}) contrast(${1 + 0.06 * hush})` } : undefined}>
            <foreignObject width={1920} height={1080}><Img src={plateAt(part, frame)} style={{ width: 1920, height: 1080, display: 'block', ...GRADE_STYLE }} /></foreignObject>
            {/* The idle's end fades to the street that leads into its start, so the loop has no seam. */}
            {part === 'idle' && frame >= IDLE - BLEND && <foreignObject width={1920} height={1080} opacity={(frame - (IDLE - BLEND) + 1) / BLEND}><Img src={preAt(frame - (IDLE - BLEND))} style={{ width: 1920, height: 1080, display: 'block', ...GRADE_STYLE }} /></foreignObject>}
          </g>
          <g transform={`rotate(${v.roll} 960 540)`} filter={blur ? 'url(#whip)' : undefined}>
            {under}
            {!hit.stage && fx}
          </g>
        </g>
        {/* ⚠️ AN IMPACT FRAME: two held frames, ink and paper then flipped, with lines rushing to the moment. */}
        {hit.stage > 0 && <g fill={hit.stage === 1 ? '#17060F' : '#FFE2E0'} opacity={0.92}>{Array.from({ length: 34 }, (_, i) => {
          const ang = (i / 34) * Math.PI * 2 + random(`il${i}`) * 0.16;
          const r0 = 360 + 240 * random(`ir${i}${hit.stage}`);
          const r1 = 1300;
          return <path key={i} d={`M${960 + Math.cos(ang - 0.016) * r1} ${540 + Math.sin(ang - 0.016) * r1}L${960 + Math.cos(ang) * r0} ${540 + Math.sin(ang) * r0}L${960 + Math.cos(ang + 0.016) * r1} ${540 + Math.sin(ang + 0.016) * r1}Z`} />;
        })}</g>}
         <GradeCorners />
        <rect width={1920} height={1080} fill="url(#vignette)" opacity={0.14 + 0.45 * hush} />
        {/* The can: everything holds for STOP frames. A flash, ink, then the burst. */}
        {f >= B.klang && f < B.klang + 2 && <rect width={1920} height={1080} fill={DRAIN_CORE} />}
        {stopped && f >= B.klang + 4 && <Burst x={icx} y={icy} s={300} c="#F444D4" />}
        {f >= B.gulp && f < B.gulp + 1 && <rect width={1920} height={1080} fill={DRAIN_CORE} />}
        {klangWord > 0 && <g opacity={klangWord} transform={`translate(520 300) rotate(-7) scale(${kf(f, [[B.klang + 5, 0.4], [B.klang + 9, 1.15], [B.klang + 13, 1]])})`}><Word text="KLANG!" h={190} /></g>}
        {drainedWord > 0 && <g opacity={drainedWord} transform={`translate(1240 300) rotate(-5) scale(${kf(f, [[B.drained + 8, 0.4], [B.drained + 12, 1.15], [B.drained + 16, 1]])})`}><Word text="DRAINED!" h={170} fill={DRAIN} /></g>}
      </svg>
    </AbsoluteFill>
  );
};
