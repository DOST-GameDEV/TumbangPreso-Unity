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

/** The film's impact frames: when the eyes catch at the end of the chase, the snap of the throw, the snatch of Backtrack. Film clock, 30 a second. */
export const EYES_CATCH = B.look + 16;
export const IMPACTS = [EYES_CATCH, B.snap - 1, B.snapBack, B.land + 5];

/** 1 and 2 are the two held frames of an impact; the punch-in then runs off. */
export const impactAt = (f: number) => {
  for (const t of IMPACTS) {
    if (f >= t && f < t + 1) return { stage: 1, punch: 1 };
    if (f >= t + 1 && f < t + 2) return { stage: 2, punch: 1 };
    if (f >= t + 2 && f < t + 12) return { stage: 0, punch: 1 - (f - t - 2) / 10 };
  }
  return { stage: 0, punch: 0 };
};

/** How much of his own current lights him and the court at this moment, 0 to 1. */
export const litAt = (f: number) => {
  if (f >= INTRO) {
    const q = ph(f);
    let out = 0;
    for (const e of IDLE_SPARKS) out = Math.max(out, e.lit * env(q - e.at, 0, 2, e.len * 0.5, e.len + 8));
    return out;
  }
  return Math.max(
    f >= B.run && f < B.look + 11 ? 0.55 : 0,
    f >= B.look + 11 && f < B.snapBack + 14 ? kf(f, [[B.look + 11, 0.4], [EYES_CATCH, 1], [B.snapBack, 0.9], [B.snapBack + 14, 0.5]]) : 0,
    f >= B.snapBack + 14 && f < B.land + 30 ? kf(f, [[B.snapBack + 14, 0.6], [B.land, 0.5], [B.land + 30, 0]]) : 0,
  );
};
