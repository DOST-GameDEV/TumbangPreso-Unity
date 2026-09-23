import { easeInOut } from './time';

export type Ease = (t: number) => number;
export type Key = [frame: number, value: number, ease?: Ease];

/**
 * Keyframes, the way an animator writes them: a value at a frame, and the ease INTO that key.
 * Before the first key the first value holds; after the last, the last.
 */
export const kf = (f: number, keys: Key[]): number => {
  if (f <= keys[0][0]) return keys[0][1];
  for (let i = 1; i < keys.length; i++) {
    const [f1, v1, e] = keys[i];
    const [f0, v0] = keys[i - 1];
    if (f <= f1) {
      const t = f1 === f0 ? 1 : (f - f0) / (f1 - f0);
      return v0 + (v1 - v0) * (e ?? easeInOut)(t);
    }
  }
  return keys[keys.length - 1][1];
};

export const linear: Ease = (t) => t;
export const hold: Ease = () => 0;
export const snap: Ease = (t) => (t < 1 ? 0 : 1);
export const outQuad: Ease = (t) => 1 - (1 - t) * (1 - t);
export const inQuad: Ease = (t) => t * t;
export const outCubic: Ease = (t) => 1 - Math.pow(1 - t, 3);
export const inCubic: Ease = (t) => t * t * t;
export const outBack: Ease = (t) => {
  const k = 1.9;
  return 1 + (k + 1) * Math.pow(t - 1, 3) + k * Math.pow(t - 1, 2);
};
/** Settles past the target and back, twice, for a dead stop that still has weight. */
export const outElastic: Ease = (t) => (t === 0 || t === 1 ? t : Math.pow(2, -9 * t) * Math.sin((t * 10 - 0.75) * ((2 * Math.PI) / 3)) + 1);
