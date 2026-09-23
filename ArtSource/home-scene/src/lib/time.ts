import { noise3D } from '@remotion/noise';

export const FPS = 30;
export const LOOP = 1260; // 42.0 s. Every ambient motion is periodic on this, so frame 1260 is frame 0.

export const s = (seconds: number) => Math.round(seconds * FPS);

export const clamp01 = (v: number) => Math.max(0, Math.min(1, v));

/** 0 before a, 1 after b, linear between. */
export const seg = (f: number, a: number, b: number) => clamp01((f - a) / (b - a));

export const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

export const easeInOut = (t: number) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);
export const easeOut = (t: number) => 1 - Math.pow(1 - t, 3);
export const easeIn = (t: number) => t * t * t;
export const easeOutBack = (t: number, k = 1.70158) => 1 + (k + 1) * Math.pow(t - 1, 3) + k * Math.pow(t - 1, 2);

/** Hold a drawing for n frames, the way hand animation is shot on twos and threes. */
export const onN = (f: number, n: number) => Math.floor(f / n) * n;

/**
 * ⚠️ PERIODIC NOISE. Sampling noise on a circle makes a value whose frame 900 equals its frame
 * 0, which is the only way wind, sway and flicker can run through the loop seam without a
 * visible jump. `radius` sets how much the value wanders inside one period; `cycles` is how
 * many times round the circle per loop, so it must be a whole number.
 */
export const loopNoise = (seed: string, f: number, radius = 1.2, cycles = 1, lane = 0) => {
  const a = (2 * Math.PI * cycles * f) / LOOP;
  return noise3D(seed, Math.cos(a) * radius, Math.sin(a) * radius, lane);
};

/** A sine that completes a whole number of cycles per loop. */
export const loopSin = (f: number, cycles: number, phase = 0) =>
  Math.sin((2 * Math.PI * cycles * f) / LOOP + phase);

/** Bell envelope: 0 outside [a, d], ramps up over [a, b], holds, ramps down over [c, d]. */
export const env = (f: number, a: number, b: number, c: number, d: number) => {
  if (f <= a || f >= d) return 0;
  if (f < b) return easeInOut((f - a) / (b - a));
  if (f <= c) return 1;
  return 1 - easeInOut((f - c) / (d - c));
};
