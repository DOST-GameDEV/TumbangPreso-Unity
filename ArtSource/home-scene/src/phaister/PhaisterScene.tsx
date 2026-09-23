import React, { useEffect, useState } from 'react';
import { AbsoluteFill, continueRender, delayRender, staticFile, useCurrentFrame } from 'remotion';
import { Defs } from '../lib/Defs';
import { ActorImage, ActorProps, Built, drawActor, drawProp, HAND_R, Light, measureActor, useActor, useProp } from '../three/actor';
import { apparent, PERSON_SCALE, project, scaleAt, V3, View } from './view';
import { Deck, FarCity, LeftStreet, Masts, Moon, Piers, RightStreet, Road, Sky, Stars, Train, Wires } from './set';
import { camAt, panSpeed } from './camera';
import { CAN, canAt, doodle, DOODLE, HerFrame, phaister, stageAt } from './perform';
import { Corona, Fx, FxBack } from './fx';
import { Jeepney, Moths, ParesCart, PierWear, Pisonet, Puddles, StringLights, UnderLamps } from './life';
import { PB, pt } from './beats';

const useFonts = () => {
  const [handle] = useState(() => delayRender('fonts'));
  useEffect(() => {
    const faces = [
      new FontFace('Darumadrop One', `url(${staticFile('fonts/DarumadropOne-Regular.ttf')})`),
      new FontFace('Paalalabas', `url(${staticFile('fonts/PaalalabasDisplayWide.otf')})`),
    ];
    Promise.all(faces.map((f) => f.load()))
      .then((loaded) => {
        loaded.forEach((f) => (document.fonts as unknown as { add: (x: FontFace) => void }).add(f));
        continueRender(handle);
      })
      .catch(() => continueRender(handle));
  }, [handle]);
};

/**
 * ⚠️ HER KEY LIGHT COMES FROM THE LENS SIDE, warm, as the shopfronts and the median lamp would throw
 * it (docs/HOME_SCREEN_ANIMATION_METHOD.md § 2: a figure lit only from behind is a silhouette with no
 * face, and her magenta hair under a black hat would be one dark blob). The moon behind her is the
 * RIM. The shade is a warm plum, never a grey.
 */
export const NIGHT: Light = {
  dir: [0.5, 0.55, 0.68],
  lit: '#FFE6CC',
  shade: '#9A6A74',
  rim: '#FFE9C4',
  rim_strength: 0.55,
};

/** The lata in the world: KALAWANG is 0.383 prop units tall, drawn 0.62 m tall here. */
const CAN_M_PER_UNIT = 0.62 / 0.383;
const FOCUS = 0.5;

/** Her figure's actor props at frame f, seen through camera v. Drawing and measuring share it. */
const herProps = (f: number, v: View, p: HerFrame, moonlight: number): ActorProps => {
  const at: V3 = [p.x, 0, p.z];
  const ppu = scaleAt(v, at) * PERSON_SCALE;
  const ap = apparent(v, at, p.yaw, FOCUS * PERSON_SCALE);
  const [fx, fy] = project(v, at);
  return {
    x: fx,
    y: fy - FOCUS * ppu,
    ppu,
    focus: FOCUS,
    reach: 0.66,
    yaw: ap.yaw,
    pitch: ap.pitch,
    pose: p.pose,
    lift: p.lift,
    face: p.face,
    eyeGlow: p.eyeGlow,
    eyeColour: '#F444D4',
    res: 1.3,
    light: { ...NIGHT, rim_strength: NIGHT.rim_strength + 0.35 * (moonlight - 1), flash: p.flash, flashColour: '#E828C5' },
  };
};

/** Her right hand on screen at frame g, in that frame's own camera. */
const handAt = (b: Built, g: number) => {
  const v = camAt(g);
  const p = phaister(g);
  return measureActor(b, herProps(g, v, p, stageAt(g).moonlight), [['arm-right', HAND_R]])[0];
};

export const PhaisterScene: React.FC = () => {
  useFonts();
  const f = useCurrentFrame();
  const ph = useActor('team-phaister', false);
  const can = useProp('lata_metal');
  const st = stageAt(f);
  const v = camAt(f);
  const p = phaister(f);
  const c = canAt(f);

  // The light her hand leaves: the last few frames of its path, measured on the real rig. During
  // the big sigil it is a comet; during a doodle it is the whole little loop, fading as it closes.
  const trail: [number, number][] = [];
  const loop: [number, number][] = [];
  if (ph) {
    if (f >= PB.sigil + pt(4) && f < PB.sigil + pt(36)) for (let g = Math.max(PB.sigil + pt(4), f - 9); g <= f; g++) trail.push(handAt(ph, g));
    const d = doodle(f);
    if (d && d.t > 12 && d.fade > 0.02) {
      const c0 = Math.floor(f / DOODLE) * DOODLE;
      for (let g = c0 + 12; g <= Math.min(f, c0 + 44); g += 2) loop.push(handAt(ph, g));
    }
  }

  let figure: React.ReactNode = null;
  if (ph && p.visible > 0.001) {
    const props = herProps(f, v, p, st.moonlight);
    const d = drawActor(ph, props);
    // Into and out of the blink she folds toward her middle as well as crouching.
    const k = 1 - 0.55 * p.fold;
    const cx = props.x;
    const cy = props.y;
    figure = (
      <g opacity={p.visible} transform={`translate(${cx} ${cy}) scale(${k} ${1 - 0.7 * p.fold}) translate(${-cx} ${-cy})`}>
        <ActorImage d={d} />
      </g>
    );
  }
  let canImg: React.ReactNode = null;
  if (can) {
    const at: V3 = [c.x + c.dx, 0, c.z];
    const ppu = scaleAt(v, at) * CAN_M_PER_UNIT;
    const [cx, cy] = project(v, [c.x + c.dx, c.hop, c.z]);
    const ap = apparent(v, at, c.yaw, 0.3);
    const d = drawProp(can, { x: cx, y: cy - 0.19 * ppu, ppu, focus: 0.19, reach: 0.34, yaw: ap.yaw, pitch: ap.pitch, rot: c.rot, lift: c.lift, res: 1.6, light: NIGHT });
    canImg = <ActorImage d={d} />;
  }
  const figs: [number, React.ReactNode][] = [[c.z, canImg], [p.z, figure]];
  figs.sort((a, b) => b[0] - a[0]);

  const fx = { f, v, p, c, st, trail, doodle: loop };
  // Motion blur on the whip pans only: a third of a frame's slide, from 12 px of slide per frame up.
  const slide = panSpeed(f);
  const blur = slide > 12 ? Math.min(30, (slide - 12) * 0.35) : 0;
  const moonNight = Math.min(1, st.blood);
  return (
    <AbsoluteFill style={{ backgroundColor: '#16050F' }}>
      <svg width={1920} height={1080} viewBox="0 0 1920 1080">
        <Defs frame={f} />
        {blur > 0.5 && (
          <defs>
            <filter id="whip" x="-5%" y="-2%" width="110%" height="104%">
              <feGaussianBlur stdDeviation={`${blur} 0`} />
            </filter>
          </defs>
        )}
        <g transform={`rotate(${v.roll} 960 540)`} filter={blur > 0.5 ? 'url(#whip)' : undefined}>
          <g filter="url(#boilSoft)">
            <Sky v={v} f={f} dark={st.dark} />
            <Stars v={v} f={f} dark={st.dark} />
            <Moon v={v} bite={st.bite} blood={st.blood} glow={st.moonlight} />
            <Corona v={v} amount={moonNight} f={f} />
            <FarCity v={v} f={f} />
            <Train v={v} head={st.train} f={f} />
            <Masts v={v} />
            <LeftStreet v={v} />
            <RightStreet v={v} f={f} />
            <Road v={v} can={[CAN.x, 0, CAN.z]} />
            <Puddles v={v} f={f} />
            <Pisonet v={v} f={f} />
            <Jeepney v={v} f={f} />
            <ParesCart v={v} f={f} />
            <Piers v={v} lamp={1} />
            <PierWear v={v} />
            <Deck v={v} />
            <UnderLamps v={v} f={f} />
            <StringLights v={v} f={f} />
          </g>
          <Moths v={v} f={f} />
          {/* The eclipse's grade over the set, under the figures: night comes down on the street. */}
          {st.dark > 0 && <rect x={-200} y={-200} width={2320} height={1480} fill="#1A0414" opacity={0.34 * st.dark} />}
          <FxBack {...fx} />
          {figs.map(([, n], i) => <g key={i}>{n}</g>)}
          <Fx {...fx} />
          <Wires v={v} f={f} />
        </g>
        <Iris v={v} spot={st.spot} amount={st.theatre} />
        {/* The train's windows sweeping light over the street while it passes overhead. */}
        {st.strobe > 0.01 && <rect width={1920} height={1080} fill="#FFE4A0" opacity={0.11 * st.strobe} />}
        {/* The moon let go: a breath of its light over everything, fading. */}
        <rect width={1920} height={1080} fill="#FFF0CE" opacity={0.1 * Math.max(0, st.moonlight - 1.3)} />
        <rect width={1920} height={1080} fill="url(#vignette)" opacity={0.7} />
        <rect width={1920} height={1080} filter="url(#grain)" opacity={0.4} />
      </svg>
    </AbsoluteFill>
  );
};

/** The follow spot's iris: the frame darkens all round the spot, centred on it on screen. */
const Iris: React.FC<{ v: View; spot: [number, number, number]; amount: number }> = ({ v, spot, amount }) => {
  if (amount <= 0.01) return null;
  const [sx, sy] = project(v, [spot[0], 1.0, spot[1]]);
  const r = scaleAt(v, [spot[0], 0, spot[1]]) * spot[2] * 2.2;
  return (
    <g>
      <defs>
        <radialGradient id="irisG" gradientUnits="userSpaceOnUse" cx={sx} cy={sy} r={r} gradientTransform={`translate(${sx} ${sy}) scale(1.3 1) translate(${-sx} ${-sy})`}>
          <stop offset="35%" stopColor="#0E0309" stopOpacity={0} />
          <stop offset="100%" stopColor="#0E0309" stopOpacity={1} />
        </radialGradient>
      </defs>
      <rect width={1920} height={1080} fill="url(#irisG)" opacity={0.62 * amount} />
    </g>
  );
};
