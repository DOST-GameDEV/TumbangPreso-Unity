import { noise3D } from '@remotion/noise';
export { clamp01, easeInOut, easeIn, easeOut, env, lerp, onN, s, seg, FPS } from '../lib/time';

/**
 * ⚠️ HER LOOP IS 54 s, NOT THE SHARED 42. 🧑 2026-09-24: *"i want u to not make it too fast and give
 * each scene time to breathe so they could be pcessed by people"*. Her loop tells a whole round and a
 * punchline; at 42 s the action had 21 s and read as a rush. So her own clock is 1620 frames, and every
 * periodic motion in her files is periodic on THIS, which is why they import from here rather than
 * from ../lib/time (whose LOOP is Zack's 1260 and stays his). HubSceneVideo plays any length.
 */
export const LOOP = 1620;

export const loopNoise = (seed: string, f: number, radius = 1.2, cycles = 1, lane = 0) => {
  const a = (2 * Math.PI * cycles * f) / LOOP;
  return noise3D(seed, Math.cos(a) * radius, Math.sin(a) * radius, lane);
};

export const loopSin = (f: number, cycles: number, phase = 0) => Math.sin((2 * Math.PI * cycles * f) / LOOP + phase);
