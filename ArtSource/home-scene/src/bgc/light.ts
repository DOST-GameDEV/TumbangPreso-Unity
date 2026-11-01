import { kf } from '../lib/kf';
import { B } from './beats';
import { env, INTRO, ph } from './time';

/**
 * ⚠️ THE HOLD'S FEW MOMENTS OF CURRENT (owner, 2026-11-01: "from time to time", "dont spam it"). Frames on the idle's 24 s
 * clock at 30 a second, well clear of the loop point. Everything else on the idle is quiet.
 */
export const IDLE_SPARKS: { at: number; len: number; kind: 'fingers' | 'run' | 'hair'; lit: number }[] = [
  { at: 150, len: 16, kind: 'fingers', lit: 0.3 },
  { at: 446, len: 30, kind: 'run', lit: 0.9 },
  { at: 610, len: 14, kind: 'hair', lit: 0.3 },
];

/** How much of his own current lights him and the court at this moment, 0 to 1. */
export const litAt = (f: number) => {
  if (f >= INTRO) {
    const q = ph(f);
    let out = 0;
    for (const e of IDLE_SPARKS) out = Math.max(out, e.lit * env(q - e.at, 0, 2, e.len * 0.5, e.len + 8));
    return out;
  }
  return Math.max(
    f >= B.eyes + 6 && f < B.wide ? kf(f, [[B.eyes + 6, 0], [B.eyes + 26, 1], [B.eyes + 32, 0.55], [B.wide, 0.25]]) : 0,
    f >= B.run && f < B.scoop ? 0.55 : 0,
    f >= B.commit && f < B.snapBack + 14 ? kf(f, [[B.commit, 0.2], [B.snapBack, 0.9], [B.snapBack + 14, 0.5]]) : 0,
    f >= B.snapBack + 14 && f < B.land + 30 ? kf(f, [[B.snapBack + 14, 0.5], [B.land, 0.5], [B.land + 30, 0]]) : 0,
  );
};
