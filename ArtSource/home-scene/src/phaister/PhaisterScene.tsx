import React, { useEffect, useState } from 'react';
import { AbsoluteFill, continueRender, delayRender, staticFile, useCurrentFrame } from 'remotion';
import { Defs } from '../lib/Defs';
import { kf } from '../lib/kf';
import { env } from './time';
import { ActorImage, drawActor, drawKuro, drawProp, HAND_R, Light, useActor, useKuro, useProp } from '../three/actor';
import { apparent, depth, PERSON_SCALE, project, scaleAt, V3, View } from './view';
import { Banderitas, Clouds, Columns, Deck, FarCity, Ground, Hoop, Item, Lamps, mixHex, Poles, Rows, setNight, setPadFlare, Sky, Stars, Sun, Traffic } from './set';
import { Signs, Stalls } from './signs';
import { camAt, panSpeed } from './camera';
import { flyingAt, HerFrame, phaister } from './perform';
import { canAt, kuro, nemu } from './nemu';
import { Burst, covenAt, FxFront, FxGround, FxScreen, impactAt, TrainLight } from './fx';
import { PB } from './beats';
import { TRAIN } from './map';

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
 * The street's hour, per frame: how far the night has come down (Grand Coven's, from the top of the
 * sky), how hard the chalk court burns, and where the two trains are.
 */
const stage = (f: number) => {
  const night = kf(f, [[PB.build - 2, 0], [PB.build + 16, 1], [PB.dawn, 1], [PB.dawn + 84, 0]]);
  const court = kf(f, [[PB.ignite, 0], [PB.ignite + 10, 1], [PB.descend + 30, 1], [PB.descend + 90, 0.3], [PB.dawn, 0.3], [PB.dawn + 60, 0]]);
  // Trains: the trick's curtain (its nose passes over the tag at 12.2 s), then an ordinary one.
  let train: number | null = null;
  const t1 = 5 + (TRAIN.speed / 30) * (f - PB.tag);
  if (t1 > -70 && t1 < 150) train = t1;
  const t2 = -10 + (TRAIN.speed / 30) * (f - PB.train2);
  if (t2 > -70 && t2 < 150) train = t2;
  return { night, court, train };
};

/** The figures' light: gold key from the lens side by day, warm plum by night, her pink on the rims while the circle burns. */
const lightAt = (night: number, coven: number): Light => {
  const day = { lit: '#FFF0DC', shade: '#C7917E', rim: '#FFD27A' };
  const eve = { lit: '#FFE6D8', shade: '#B4849A', rim: '#FFC8EE' };
  return {
    dir: [0.45, 0.58, 0.68],
    lit: mixHex(day.lit, eve.lit, night),
    shade: mixHex(day.shade, eve.shade, night),
    rim: mixHex(mixHex(day.rim, eve.rim, night), '#FF7AE6', Math.min(1, coven)),
    rim_strength: 0.6 + 0.35 * Math.min(1, coven),
  };
};

const FOCUS = 0.5;
const CAN_M_PER_UNIT = 0.62 / 0.383;
const KURO_M = 0.55;

export const PhaisterScene: React.FC = () => {
  useFonts();
  const f = useCurrentFrame();
  const ph = useActor('team-phaister', true);
  const ne = useActor('team-nemu');
  const kk = useKuro();
  const can = useProp('lata_metal');
  const st = stage(f);
  setNight(st.night);
  setPadFlare(env(f, PB.scoop, PB.scoop + 3, PB.scoop + 14, PB.block + 10));
  const v = camAt(f);
  const p = phaister(f);
  const n = nemu(f);
  const k = kuro(f, n);
  const c = canAt(f);
  const cv = covenAt(f);
  const light = lightAt(st.night, cv.build > 0 ? cv.bright * env(f, PB.build, PB.build + 20, PB.descend + 20, PB.descend + 60) : 0);

  const items: Item[] = [];
  const push = (node: React.ReactNode, at: V3, bias = 0) => items.push({ z: depth(v, at) + bias, node });

  // ---------------------------------------------------------------- the cast
  let handWorld: { at: [number, number]; s: number } | null = null;
  let kuroTop: [number, number] | null = null;
  // Where each head is on screen, and how many pixels a metre is there: the comic marks sit beside them.
  const headOf = (x: number, y: number, z: number): [number, number, number] | null => {
    if (depth(v, [x, y + 1.9, z]) < 0.8) return null;
    const [hx, hy] = project(v, [x, y + 2.05, z]);
    return [hx, hy, scaleAt(v, [x, y + 2.05, z])];
  };
  const heads = { her: p.visible > 0.5 ? headOf(p.x, p.y, p.z) : null, nemu: headOf(n.x, n.y - 0.25, n.z) };
  // ⚠️ A figure behind the lens is not drawn: projecting it clamps its depth to the near plane and it
  // came out as a giant hat across the frame (the first sheet's 'nothing' shot).
  const inFront = (x: number, y: number, z: number) => depth(v, [x, y + 1, z]) > 0.6;
  if (ph && p.visible > 0.001 && inFront(p.x, p.y, p.z)) {
    const d = figure(ph, v, p, light, '#F444D4', true);
    const k2 = 1 - 0.55 * p.fold;
    handWorld = f >= PB.twirl && f < PB.release ? { at: d.img.at('arm-right', HAND_R), s: scaleAt(v, [p.x, p.y + 1.3, p.z]) } : null;
    push(
      <g key="her" opacity={p.visible} transform={`translate(${d.fx} ${d.fy}) scale(${k2} ${1 - 0.7 * p.fold}) translate(${-d.fx} ${-d.fy})`}>
        <g transform={`rotate(${d.roll} ${d.fx} ${d.fy})`}><ActorImage d={d.img} /></g>
      </g>,
      [p.x, p.y + 1, p.z],
    );
  }
  if (ne && inFront(n.x, n.y, n.z)) {
    // ⚠️ Lit from the circle under her: at night her dark palette read as a silhouette (third sheet).
    const nl: Light = { ...light, lit: mixHex(light.lit, '#FFFFFF', st.night), shade: mixHex(light.shade, '#D8A0C8', st.night), rim_strength: light.rim_strength + 0.3 * st.night };
    const d = figure(ne, v, { ...n, lift: [0, 0, 0], face: 'rest', eyeGlow: 0, visible: 1, flash: 0, fold: 0, slipper: { at: 'none' } } as HerFrame, nl, '#FFFFFF', false);
    push(<g key="nemu" transform={`rotate(${d.roll} ${d.fx} ${d.fy})`}><ActorImage d={d.img} /></g>, [n.x, n.y + 1, n.z]);
  }
  if (kk) {
    const at: V3 = [k.x, k.y, k.z];
    if (depth(v, at) > 0.6) {
      const ppu = scaleAt(v, at) * (KURO_M / kk.height);
      const ap = apparent(v, at, 0, KURO_M * 0.5);
      const [x, y] = project(v, [at[0], at[1] + KURO_M * 0.5, at[2]]);
      const img = drawKuro(kk, { x, y, ppu, yaw: k.yaw, pitch: ap.pitch, face: k.face, tilt: k.tilt, light: { ...light, lit: mixHex(light.lit, '#FFFFFF', st.night), shade: mixHex(light.shade, '#E0B0D8', st.night) }, res: 1.3 });
      kuroTop = project(v, [at[0], at[1] + KURO_M * 1.1, at[2]]);
      push(<g key="kuro" opacity={0.95}><ActorImage d={img} /></g>, at);
    }
  }
  let canImg: React.ReactNode = null;
  if (can && depth(v, [c.x, 0.3, c.z]) > 0.6) {
    const at: V3 = [c.x, c.hop, c.z];
    const ppu = scaleAt(v, at) * CAN_M_PER_UNIT;
    const ap = apparent(v, [c.x, 0, c.z], 22, 0.3);
    const [x, y] = project(v, [at[0], at[1] + 0.3, at[2]]);
    const img = drawProp(can, { x, y, ppu, focus: 0.19, reach: 0.34, yaw: ap.yaw, pitch: ap.pitch, rot: c.rot, lift: [0, 0.125 * c.lying, 0], res: 1.6, light });
    canImg = <ActorImage d={img} />;
    push(<g key="can">{canImg}</g>, at);
    if (impactAt(f) && ph) {
      const flashImg = drawProp(can, { x, y, ppu, focus: 0.19, reach: 0.34, yaw: ap.yaw, pitch: ap.pitch, rot: c.rot, lift: [0, 0.125 * c.lying, 0], res: 1.6, light: { ...light, flash: 1, flashColour: '#FFF6EA' } });
      canImg = <ActorImage d={flashImg} />;
    }
  }
  const fl = flyingAt(f);
  if (fl && ph && depth(v, fl.p) > 0.6) {
    const at = fl.p;
    const ppu = scaleAt(v, at) * PERSON_SCALE;
    const [x, y] = project(v, at);
    const img = drawActor(ph, { x, y, ppu, focus: 0, target: [0, 0, 0], reach: 0.22, yaw: 0, pitch: apparent(v, at, 0, 0).pitch, only: 'slipper', slipper: { at: 'free', pos: [0, 0, 0], rot: fl.rot }, light, res: 1.4 });
    push(<g key="slipper"><ActorImage d={img} /></g>, at, -0.3);
  }

  // ---------------------------------------------------------------- the street's near things
  for (const it of [...Columns(v), ...Poles(v, f), ...Lamps(v), ...Banderitas(v, f, env(f, PB.stamp, PB.stamp + 2, PB.stamp + 10, PB.stamp + 40)), ...Traffic(v, f), ...Hoop(v, 0), ...Signs(v, f), ...Stalls(v, f)]) items.push(it);
  items.sort((a, b) => b.z - a.z);
  const rows = Rows(v, f).sort((a, b) => b.z - a.z);

  // The past few places she has been, for the overclock ribbons.
  const hist: V3[] = [];
  for (let g = Math.max(0, f - 8); g <= f; g++) {
    const q = phaister(g);
    hist.push([q.x, 0, q.z]);
  }
  const fx = { f, v, p, n, hist, hand: handWorld, canAt: [c.x, 0, c.z] as V3 };

  // Motion blur on the whips: a third of a frame's slide, from 14 px a frame up.
  const sp = panSpeed(f);
  // ⚠️ Capped low: at 34 px the whip after the slipper was a whole second of mush (second sheet).
  const bx = sp.x > 22 ? Math.min(14, (sp.x - 22) * 0.25) : 0;
  const by = sp.y > 22 ? Math.min(10, (sp.y - 22) * 0.25) : 0;
  const blur = bx > 0.5 || by > 0.5;
  const imp = impactAt(f);
  const [icx, icy] = project(v, [c.x, 0.35, c.z]);
  return (
    <AbsoluteFill style={{ backgroundColor: '#2A1418' }}>
      <svg width={1920} height={1080} viewBox="0 0 1920 1080">
        <Defs frame={f} />
        {blur && (
          <defs>
            <filter id="whip" x="-5%" y="-5%" width="110%" height="110%">
              <feGaussianBlur stdDeviation={`${bx} ${by}`} />
            </filter>
          </defs>
        )}
        <g transform={`rotate(${v.roll} 960 540)`} filter={blur ? 'url(#whip)' : undefined}>
          <g filter="url(#boilSoft)">
            <Sky v={v} f={f} wipe={st.night} />
            <Sun v={v} wipe={st.night} />
            <Clouds v={v} f={f} />
            <Stars v={v} f={f} amount={st.night} />
            <FarCity v={v} f={f} />
            <Ground v={v} f={f} court={st.court} />
            <TrainLight v={v} nose={st.train} f={f} />
          </g>
          <FxGround {...fx} />
          <g filter="url(#boilSoft)">
            {rows.map((it) => it.node)}
            <Deck v={v} f={f} train={st.train} strobe={0} />
          </g>
          {items.map((it, i) => <g key={i}>{it.node}</g>)}
          <FxFront {...fx} />
          <FxScreen f={f} v={v} can={[c.x, 0, c.z]} kuroTop={kuroTop} kuroBang={k.bang} heads={heads} />
        </g>
        {/* Golden hour: a warm lift from the sun's side; at night the frame closes in plum. */}
        <rect width={1920} height={1080} fill="#FFB060" opacity={0.07 * (1 - st.night)} style={{ mixBlendMode: 'screen' }} />
        <rect width={1920} height={1080} fill="url(#vignette)" opacity={0.45 + 0.35 * st.night} />
        {imp > 0 && (
          <g>
            <rect width={1920} height={1080} fill="#2A0620" opacity={0.94} />
            <Burst x={icx} y={icy} s={260} />
            {canImg}
          </g>
        )}
        <rect width={1920} height={1080} filter="url(#grain)" opacity={0.35} />
      </svg>
    </AbsoluteFill>
  );
};

/** A figure on the street through this lens: where it stands, how big, from what angle. */
const figure = (b: NonNullable<ReturnType<typeof useActor>>, v: View, p: HerFrame, light: Light, eye: string, withSlipper: boolean) => {
  const at: V3 = [p.x, p.y, p.z];
  const focusY = FOCUS * PERSON_SCALE;
  const ppu = scaleAt(v, [p.x, p.y + focusY, p.z]) * PERSON_SCALE;
  const ap = apparent(v, at, p.yaw, focusY);
  const [fx, fy] = project(v, [p.x, p.y + focusY, p.z]);
  const img = drawActor(b, {
    x: fx,
    y: fy,
    ppu,
    focus: FOCUS,
    reach: 0.7,
    yaw: ap.yaw,
    pitch: ap.pitch,
    pose: p.pose,
    lift: p.lift,
    face: p.face,
    eyeGlow: p.eyeGlow,
    eyeColour: eye,
    slipper: withSlipper ? p.slipper : undefined,
    res: 1.25,
    light: { ...light, flash: p.flash, flashColour: '#E828C5' },
  });
  return { img, fx, fy, roll: ap.roll };
};

export { HAND_R };
