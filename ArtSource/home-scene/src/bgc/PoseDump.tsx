import React, { useEffect, useState } from 'react';
import { continueRender, delayRender } from 'remotion';
import * as THREE from 'three';
import { Frame, canAt, flyingAt, rago, zack } from './perform';
import { IDLE, INTRO } from './time';
import { B } from './beats';
import { poseFrame, useActor } from '../three/actor';

const BONES = ['root', 'leg-left', 'leg-right', 'torso', 'arm-left', 'arm-right', 'head', 'forearm-left', 'forearm-right'];
const deg = Math.PI / 180;
const r = (n: number) => Math.round(n * 1e5) / 1e5;

/**
 * ⚠️ THE FILM'S ACTING, AS NUMBERS FOR UNITY (scripts/plates.mjs). The same clips, keys and smoothing as the
 * film always had, computed on the same rigs, so the game's own models can stand in the plate with the game's
 * own shader. One row per clock frame: see `HomeFilmPlateRecorder.Apply` for the layout.
 */
export const PoseDump: React.FC = () => {
  const zk = useActor('zack-redesign', true);
  const rg = useActor('sean-redesign');
  const [handle] = useState(() => delayRender('poses', { timeoutInMilliseconds: 900000 }));
  useEffect(() => {
    if (!zk || !rg) return;
    const row = (b: NonNullable<typeof zk>, p: Frame, slipper: boolean, f: number) => {
      const fl = slipper ? flyingAt(f) : null;
      const state = fl ? { at: 'free' as const, pos: [0, 0, 0] as [number, number, number], rot: fl.rot } : slipper ? p.slipper : { at: 'none' as const };
      const s = poseFrame(b, p.layers, p.samples, p.lift, state);
      const out = [p.x, p.z, p.yaw, p.eyeGlow];
      // Where the slipper is: 0 none, 1 on the body, 2 flying, then position, rotation, scale.
      out.push(!s.slipper ? 0 : fl ? 2 : 1);
      const at = fl ? fl.p : s.slipper ? s.slipper.p : [0, 0, 0];
      out.push(at[0], at[1], at[2], ...(s.slipper ? s.slipper.q : [0, 0, 0, 1]), (s.slipper ? s.slipper.s : 1) * (fl?.trail && f < B.closeOn ? 1.9 : 1));
      for (const k of BONES) out.push(...s.bones[k].p, ...s.bones[k].q);
      return out.map(r);
    };
    const canRow = (f: number) => {
      const c = canAt(f);
      const q = new THREE.Quaternion().setFromEuler(new THREE.Euler(0, 22 * deg, 0)).multiply(new THREE.Quaternion().setFromEuler(new THREE.Euler(c.rot[0] * deg, c.rot[1] * deg, c.rot[2] * deg, 'YXZ')));
      return [c.x, c.hop + 0.13, c.z, q.x, q.y, q.z, q.w, 0.125 * c.lying].map(r);
    };
    const rows: Record<string, { z: number[]; r: number[]; c: number[] }> = {};
    const add = (f: number) => { rows[String(f)] = { z: row(zk, zack(f), true, f), r: row(rg, rago(f), false, f), c: canRow(f) }; };
    for (let i = 0; i < INTRO * 2; i++) add(i / 2);
    for (let i = 0; i < IDLE; i++) add(INTRO + i);
    fetch('http://127.0.0.1:3377/poses', { method: 'POST', body: JSON.stringify({ bones: BONES, rows }) }).then(() => continueRender(handle));
  }, [zk, rg, handle]);
  return null;
};
