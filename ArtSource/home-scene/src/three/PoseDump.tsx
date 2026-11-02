import React, { useEffect, useState } from 'react';
import { continueRender, delayRender } from 'remotion';
import * as THREE from 'three';
import { Layer, PoseSample, SlipperState, poseFrame, useActor } from './actor';

const BONES = ['root', 'leg-left', 'leg-right', 'torso', 'arm-left', 'arm-right', 'head', 'forearm-left', 'forearm-right'];
const deg = Math.PI / 180;
const r = (n: number) => Math.round(n * 1e5) / 1e5;

type Model = Parameters<typeof useActor>[0];
/** The part of a performer's frame the plate needs. */
type Acted = { x: number; z: number; yaw: number; eyeGlow: number; layers: Layer[]; samples?: PoseSample[]; lift: [number, number, number]; slipper: SlipperState; belt?: boolean };
type Flying = { p: [number, number, number]; rot: [number, number, number] } | null;
type Can = { x: number; z: number; hop: number; rot: [number, number, number]; lying: number };

/** What one film gives the plates. The first actor holds the slipper. */
export type PoseFilm = {
  intro: number;
  idle: number;
  first: { model: Model; frame: (f: number) => Acted };
  second: { model: Model; frame: (f: number) => Acted };
  flyingAt: (f: number) => Flying;
  canAt: (f: number) => Can;
  /** A flying slipper's scale over its true size. */
  flyScale?: (f: number, fl: NonNullable<Flying>) => number;
  /** Kuro: x y z yaw pitch roll scale face (`HomeFilmPlateRecorder.Pet`). */
  kuro?: (f: number) => number[];
  /** Moths, seven numbers each (`HomeFilmPlateRecorder.Moths`). */
  moths?: (f: number) => number[];
};

/**
 * ⚠️ THE FILM'S ACTING, AS NUMBERS FOR UNITY (scripts/plates.mjs). The same clips, keys and smoothing as the
 * film always had, computed on the same rigs, so the game's own models can stand in the plate with the game's
 * own shader. One row per clock frame: see `HomeFilmPlateRecorder.Apply` for the layout.
 */
export const PoseDump: React.FC<{ film: PoseFilm }> = ({ film }) => {
  const a = useActor(film.first.model, true);
  const b = useActor(film.second.model);
  const [handle] = useState(() => delayRender('poses', { timeoutInMilliseconds: 900000 }));
  useEffect(() => {
    if (!a || !b) return;
    const row = (rig: NonNullable<typeof a>, p: Acted, slipper: boolean, f: number) => {
      const fl = slipper ? film.flyingAt(f) : null;
      const state = fl ? { at: 'free' as const, pos: [0, 0, 0] as [number, number, number], rot: fl.rot } : slipper ? p.slipper : { at: 'none' as const };
      const s = poseFrame(rig, p.layers, p.samples, p.lift, state);
      // At the belt: tucked behind her, hanging, in the model's space.
      if (p.belt && s.slipper && !fl) { s.slipper.p = [-0.06, 0.25, -0.17]; s.slipper.q = [0, 0, 0.7071, 0.7071]; }
      const out = [p.x, p.z, p.yaw, p.eyeGlow];
      // Where the slipper is: 0 none, 1 on the body, 2 flying, then position, rotation, scale.
      out.push(!s.slipper ? 0 : fl ? 2 : 1);
      const at = fl ? fl.p : s.slipper ? s.slipper.p : [0, 0, 0];
      out.push(at[0], at[1], at[2], ...(s.slipper ? s.slipper.q : [0, 0, 0, 1]), (s.slipper ? s.slipper.s : 1) * (fl && film.flyScale ? film.flyScale(f, fl) : 1));
      for (const k of BONES) out.push(...s.bones[k].p, ...s.bones[k].q);
      return out.map(r);
    };
    const canRow = (f: number) => {
      const c = film.canAt(f);
      const q = new THREE.Quaternion().setFromEuler(new THREE.Euler(0, 22 * deg, 0)).multiply(new THREE.Quaternion().setFromEuler(new THREE.Euler(c.rot[0] * deg, c.rot[1] * deg, c.rot[2] * deg, 'YXZ')));
      return [c.x, c.hop + 0.13, c.z, q.x, q.y, q.z, q.w, 0.125 * c.lying].map(r);
    };
    const rows: Record<string, { a: number[]; b: number[]; c: number[]; k?: number[]; m?: number[] }> = {};
    const add = (f: number) => {
      rows[String(f)] = { a: row(a, film.first.frame(f), true, f), b: row(b, film.second.frame(f), false, f), c: canRow(f), k: film.kuro?.(f).map(r), m: film.moths?.(f).map(r) };
    };
    for (let i = 0; i < film.intro * 2; i++) add(i / 2);
    for (let i = 0; i < film.idle; i++) add(film.intro + i);
    fetch('http://127.0.0.1:3377/poses', { method: 'POST', body: JSON.stringify({ bones: BONES, rows }) }).then(() => continueRender(handle));
  }, [a, b, film, handle]);
  return null;
};
