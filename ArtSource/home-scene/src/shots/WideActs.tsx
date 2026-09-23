import React from 'react';
import { B, bt, K } from '../lib/beats';
import { Bolt, boltPoints, Crackle, Pt } from '../lib/bolt';
import { inCubic, kf, outBack } from '../lib/kf';
import { loopSin, onN } from '../lib/time';
import { Dust } from '../art/props';
import { Whip } from '../lib/whip';
import { ActorImage, drawActor, DUSK, HAND_L, HAND_R, HEAD_TOP, useActor } from '../three/actor';
import { arriveDX, zackWide } from '../three/zackWide';
import { Cam, Wide, ZACK_AT, zackAnchor } from './Wide';
import { WindStreaks } from './CloseUp';

/**
 * ⚠️ THE CAMERA IS NEVER STILL, EVEN AT REST. 🧑 2026-09-23: *"dynamic camera movement ... not
 * js liek dat"*. At rest it breathes: a slow drift in, out and across and a fraction of a degree
 * of roll, all periodic on the loop so the seam cannot show. The push-in rolls into a dutch
 * angle as it gathers; the run home is caught by a whip pan that overshoots and settles.
 */
export const wideCam = (f: number): Cam => {
  const drift: Cam = {
    zoom: 1.02 + 0.018 * loopSin(f, 2),
    cx: 960 + 14 * loopSin(f, 3, 1),
    cy: 540 + 8 * loopSin(f, 2, 2),
    rot: 0.45 * loopSin(f, 2, 0.5),
  };
  if (f >= B.pushIn && f < B.cu) {
    const z = kf(f, [[B.pushIn, drift.zoom], [B.cu, 1.9, inCubic]]);
    return {
      zoom: z,
      cx: kf(f, [[B.pushIn, drift.cx], [B.cu, 955]]),
      cy: kf(f, [[B.pushIn, drift.cy], [B.cu, 470]]),
      rot: kf(f, [[B.pushIn, drift.rot ?? 0], [B.cu, -5, inCubic]]),
    };
  }
  if (f >= B.arrive && f < B.settle + bt(40)) {
    // The camera chases him in from the left, lags, then catches up and settles on its mark.
    const dx = arriveDX(f);
    const follow = kf(f, [[B.arrive, 0.7], [B.settle, 0.4], [B.settle + bt(40), 0]]);
    return {
      zoom: kf(f, [[B.arrive, 1.22], [B.settle, 1.12], [B.settle + bt(3), 1.17, outBack], [B.settle + bt(40), drift.zoom]]),
      cx: drift.cx + dx * follow,
      cy: kf(f, [[B.arrive, 580], [B.settle + bt(40), drift.cy]]),
      rot: kf(f, [[B.arrive, 4], [B.settle, 2], [B.settle + bt(3), -1.5, outBack], [B.settle + bt(40), drift.rot ?? 0]]),
    };
  }
  return drift;
};

/** A few short arcs crackling off a point. */
export const Sparks: React.FC<{ at: Pt; step: number; seed: string; amount: number; r?: number; n?: number; w?: number }> = ({ at, step, seed, amount, r = 110, n = 2, w = 5 }) => {
  if (amount <= 0.02) return null;
  const out: React.ReactNode[] = [];
  for (let i = 0; i < n; i++) {
    const a = step * 0.9 + i * 2.4 + seed.length;
    out.push(
      <Crackle key={i} a={[at[0] + Math.cos(a) * 14, at[1] + Math.sin(a) * 14]} b={[at[0] + Math.cos(a) * r, at[1] + Math.sin(a) * r * 0.8]}
        seed={`${seed}${step}${i}`} w={w} branches={1} glow={amount} opacity={Math.min(1, amount)} />,
    );
  }
  return <g>{out}</g>;
};

export const WideActs: React.FC<{ f: number }> = ({ f }) => {
  const zack = useActor('team-zack', true);
  const cam = wideCam(f);
  if (!zack) return null;
  const z = zackWide(f);
  const anchor = zackAnchor(z.dx, 0.4);
  const d = drawActor(zack, {
    ...anchor,
    ppu: ZACK_AT.ppu,
    yaw: z.yaw,
    pose: z.pose,
    lift: z.lift,
    face: z.face,
    slipper: z.slipper,
    reach: 0.74,
    res: Math.min(2.6, 1.4 * cam.zoom),
    light: { ...DUSK, flash: z.flash, flashColour: '#FFF3C8' },
  });
  const step = onN(f, 2);
  const hand = d.at('arm-right', HAND_R);
  const handL = d.at('arm-left', HAND_L);
  const head = d.at('head', HEAD_TOP);
  const feet: Pt = [ZACK_AT.x + z.dx, ZACK_AT.y];

  const under: React.ReactNode[] = [];
  // Backlit: his shadow runs toward the camera, a little left, off the bottom.
  under.push(<path key="sh" d={`M ${feet[0] - 150} ${feet[1] - 6} L ${feet[0] + 150} ${feet[1] - 6} L ${feet[0] + 80} ${feet[1] + 130} L ${feet[0] - 380} ${feet[1] + 130} Z`} fill="#8E3E2C" opacity={0.42} />);

  // The run home: electric skid marks scored along the court behind his feet.
  if (f >= B.arrive && f < B.settle + bt(26)) {
    const fade = f < B.settle ? 1 : 1 - (f - B.settle) / bt(26);
    for (let k = 0; k < 2; k++) {
      const y = feet[1] - 6 + k * 14;
      const mark = boltPoints([feet[0] - 900, y], [feet[0] - 40, y], `sk${step}${k}`, 5, 0.03);
      under.push(
        <g key={`sk${k}`} opacity={fade}>
          <Bolt pts={mark} w={6 - k * 2} glow={0.8} />
        </g>,
      );
    }
  }

  const over: React.ReactNode[] = [];
  if (f >= B.settle - bt(7) && f < B.settle + bt(26)) over.push(<Dust key="sd" t={(f - B.settle + bt(5)) / bt(18)} x={feet[0] + 60} y={feet[1]} s={1.5} seed="ad" />);
  // Electricity on him: at his fists and feet in proportion to the charge.
  over.push(<Sparks key="sh" at={hand} step={step} seed="hr" amount={z.charge} n={2 + Math.round(z.charge * 2)} />);
  over.push(<Sparks key="sl" at={handL} step={step} seed="hl" amount={z.charge * 0.8} />);
  if (z.charge > 0.5) over.push(<Sparks key="sf" at={feet} step={step} seed="ft" amount={z.charge - 0.3} r={140} />);
  // The static shake: arcs off the top of his streak, then one spark pops off it.
  if (z.hairStatic > 0) over.push(<Sparks key="hs" at={head} step={step} seed="hair" amount={z.hairStatic} r={120} n={3} />);
  const pop = f - (B.shake + bt(22));
  if (pop >= 0 && pop < 18) {
    over.push(
      <g key="pop" transform={`translate(${head[0] + pop * 9} ${head[1] - pop * 14 + pop * pop * 0.9})`} opacity={1 - pop / 18}>
        <rect x={-9} y={-9} width={18} height={18} fill="#FFFDE8" />
        <rect x={-15} y={-3} width={30} height={6} fill="#E8F53A" />
      </g>,
    );
  }

  const streaks = f >= B.pushIn && f < B.cu ? kf(f, [[B.pushIn, 0], [B.cu, 1]]) : f >= B.arrive && f < B.settle ? 1 : 0;
  // Whip in to the run home: the frame arrives sliding and blurred, and clears in five frames.
  const whip = f >= B.arrive && f < B.arrive + 6 ? 1 - (f - B.arrive) / 6 : 0;
  return (
    <Whip id="wwhip" blur={70 * whip} dx={-420 * inCubic(whip)}>
      <Wide f={f} cam={cam} figure={<ActorImage d={d} />} under={under} over={over} />
      <WindStreaks f={f} amount={streaks} />
      {/* The dead stop lands with a one-frame flash of the whole screen, not of him: washing the
          figure alone read as a grey ghost in the first draft. */}
      {f === B.settle && <rect x={0} y={0} width={1920} height={1080} fill="#FFF3C8" opacity={0.22} />}
    </Whip>
  );
};
