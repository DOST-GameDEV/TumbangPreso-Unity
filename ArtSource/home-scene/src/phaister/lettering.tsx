import React from 'react';
import { random } from 'remotion';

/*
 * ⚠️⚠️ HAND-PAINTED LETTERING, NOT A FONT. 🧑 2026-09-24 on the map's own shop signs: *"dont outright use
 * fonts its so ugly"*, and *"make them actually look like the ones in PH but cartoonish"*
 * (docs/reports/.../ilalim-storefront-rework-plan.md). A street sign typeset in the UI face reads as a UI
 * label pasted on a wall. So every word on the street is PAINTED here: each letter is a brush skeleton
 * (the sign painters' single-stroke "Gothic", Eye on Design / Filipino Folk Foundry: pinched corners
 * from de-rounded brushwork, flicked ends), laid down as a fat stroke with a little wobble per letter,
 * over an outline and a hard drop shadow, which is the laminated sari-sari sign look in the owner's
 * collage (ICE FOR SALE, LOAD NA DITO, SORRY WE'RE CLOSED).
 *
 * Glyphs live on a box 1 tall; x runs 0..w. Strokes are polylines. Units at the call site are metres.
 */

type G = { w: number; s: [number, number][][] };

const GL: Record<string, G> = {
  A: { w: 0.66, s: [[[0, 1], [0.33, 0], [0.66, 1]], [[0.13, 0.62], [0.53, 0.62]]] },
  B: { w: 0.6, s: [[[0, 1], [0, 0], [0.44, 0], [0.56, 0.1], [0.56, 0.36], [0.44, 0.47], [0, 0.47]], [[0.44, 0.47], [0.6, 0.58], [0.6, 0.88], [0.48, 1], [0, 1]]] },
  C: { w: 0.6, s: [[[0.6, 0.14], [0.48, 0], [0.14, 0], [0, 0.16], [0, 0.84], [0.14, 1], [0.48, 1], [0.6, 0.86]]] },
  D: { w: 0.62, s: [[[0, 0], [0, 1], [0.42, 1], [0.62, 0.8], [0.62, 0.2], [0.42, 0], [0, 0]]] },
  E: { w: 0.54, s: [[[0.54, 0], [0, 0], [0, 1], [0.54, 1]], [[0, 0.48], [0.42, 0.48]]] },
  F: { w: 0.52, s: [[[0.52, 0], [0, 0], [0, 1]], [[0, 0.48], [0.4, 0.48]]] },
  G: { w: 0.62, s: [[[0.6, 0.14], [0.48, 0], [0.14, 0], [0, 0.16], [0, 0.84], [0.14, 1], [0.5, 1], [0.62, 0.86], [0.62, 0.55], [0.34, 0.55]]] },
  H: { w: 0.62, s: [[[0, 0], [0, 1]], [[0.62, 0], [0.62, 1]], [[0, 0.5], [0.62, 0.5]]] },
  I: { w: 0.14, s: [[[0.07, 0], [0.07, 1]]] },
  J: { w: 0.5, s: [[[0.5, 0], [0.5, 0.84], [0.36, 1], [0.12, 1], [0, 0.84]]] },
  K: { w: 0.6, s: [[[0, 0], [0, 1]], [[0.6, 0], [0, 0.58]], [[0.2, 0.42], [0.62, 1]]] },
  L: { w: 0.5, s: [[[0, 0], [0, 1], [0.5, 1]]] },
  M: { w: 0.8, s: [[[0, 1], [0, 0], [0.4, 0.6], [0.8, 0], [0.8, 1]]] },
  N: { w: 0.64, s: [[[0, 1], [0, 0], [0.64, 1], [0.64, 0]]] },
  O: { w: 0.64, s: [[[0.14, 0], [0.5, 0], [0.64, 0.16], [0.64, 0.84], [0.5, 1], [0.14, 1], [0, 0.84], [0, 0.16], [0.14, 0]]] },
  P: { w: 0.58, s: [[[0, 1], [0, 0], [0.44, 0], [0.58, 0.12], [0.58, 0.42], [0.44, 0.54], [0, 0.54]]] },
  Q: { w: 0.66, s: [[[0.14, 0], [0.5, 0], [0.64, 0.16], [0.64, 0.84], [0.5, 1], [0.14, 1], [0, 0.84], [0, 0.16], [0.14, 0]], [[0.4, 0.72], [0.68, 1.06]]] },
  R: { w: 0.6, s: [[[0, 1], [0, 0], [0.44, 0], [0.58, 0.12], [0.58, 0.4], [0.44, 0.52], [0, 0.52]], [[0.3, 0.52], [0.62, 1]]] },
  S: { w: 0.58, s: [[[0.58, 0.12], [0.46, 0], [0.12, 0], [0, 0.13], [0, 0.36], [0.12, 0.48], [0.46, 0.52], [0.58, 0.64], [0.58, 0.88], [0.46, 1], [0.12, 1], [0, 0.88]]] },
  T: { w: 0.6, s: [[[0, 0], [0.6, 0]], [[0.3, 0], [0.3, 1]]] },
  U: { w: 0.62, s: [[[0, 0], [0, 0.84], [0.14, 1], [0.48, 1], [0.62, 0.84], [0.62, 0]]] },
  V: { w: 0.64, s: [[[0, 0], [0.32, 1], [0.64, 0]]] },
  W: { w: 0.86, s: [[[0, 0], [0.2, 1], [0.43, 0.35], [0.66, 1], [0.86, 0]]] },
  X: { w: 0.62, s: [[[0, 0], [0.62, 1]], [[0.62, 0], [0, 1]]] },
  Y: { w: 0.62, s: [[[0, 0], [0.31, 0.5], [0.62, 0]], [[0.31, 0.5], [0.31, 1]]] },
  Z: { w: 0.58, s: [[[0, 0], [0.58, 0], [0, 1], [0.58, 1]]] },
  '0': { w: 0.58, s: [[[0.14, 0], [0.44, 0], [0.58, 0.16], [0.58, 0.84], [0.44, 1], [0.14, 1], [0, 0.84], [0, 0.16], [0.14, 0]]] },
  '1': { w: 0.3, s: [[[0, 0.16], [0.2, 0], [0.2, 1]], [[0, 1], [0.34, 1]]] },
  '2': { w: 0.56, s: [[[0, 0.14], [0.12, 0], [0.44, 0], [0.56, 0.13], [0.56, 0.38], [0, 1], [0.58, 1]]] },
  '3': { w: 0.56, s: [[[0, 0.1], [0.12, 0], [0.44, 0], [0.56, 0.12], [0.56, 0.36], [0.42, 0.48], [0.2, 0.48]], [[0.42, 0.48], [0.56, 0.6], [0.56, 0.88], [0.44, 1], [0.12, 1], [0, 0.9]]] },
  '4': { w: 0.58, s: [[[0.44, 1], [0.44, 0], [0, 0.66], [0.6, 0.66]]] },
  '5': { w: 0.56, s: [[[0.54, 0], [0.04, 0], [0, 0.46], [0.42, 0.44], [0.56, 0.58], [0.56, 0.88], [0.44, 1], [0.1, 1], [0, 0.9]]] },
  '6': { w: 0.56, s: [[[0.5, 0.04], [0.2, 0], [0, 0.2], [0, 0.86], [0.14, 1], [0.44, 1], [0.56, 0.86], [0.56, 0.6], [0.44, 0.48], [0.1, 0.5], [0, 0.6]]] },
  '7': { w: 0.54, s: [[[0, 0], [0.56, 0], [0.18, 1]]] },
  '8': { w: 0.56, s: [[[0.28, 0.48], [0.08, 0.4], [0.04, 0.12], [0.16, 0], [0.42, 0], [0.54, 0.12], [0.5, 0.4], [0.28, 0.48], [0.04, 0.6], [0, 0.86], [0.14, 1], [0.44, 1], [0.58, 0.86], [0.54, 0.6], [0.28, 0.48]]] },
  '9': { w: 0.56, s: [[[0.06, 0.96], [0.36, 1], [0.56, 0.8], [0.56, 0.14], [0.42, 0], [0.12, 0], [0, 0.14], [0, 0.4], [0.12, 0.52], [0.46, 0.5], [0.56, 0.4]]] },
  '!': { w: 0.14, s: [[[0.07, 0], [0.07, 0.68]], [[0.07, 0.92], [0.07, 1]]] },
  '-': { w: 0.34, s: [[[0, 0.55], [0.34, 0.55]]] },
  '.': { w: 0.12, s: [[[0.06, 0.94], [0.06, 1]]] },
  ',': { w: 0.12, s: [[[0.08, 0.92], [0.02, 1.08]]] },
  '·': { w: 0.16, s: [[[0.08, 0.5], [0.08, 0.56]]] },
  "'": { w: 0.1, s: [[[0.05, 0], [0.05, 0.22]]] },
  '&': { w: 0.62, s: [[[0.62, 1], [0.12, 0.42], [0.08, 0.14], [0.22, 0], [0.4, 0.08], [0.38, 0.3], [0, 0.66], [0.04, 0.9], [0.2, 1], [0.44, 0.9], [0.6, 0.6]]] },
  '=': { w: 0.4, s: [[[0, 0.4], [0.4, 0.4]], [[0, 0.64], [0.4, 0.64]]] },
  '₱': { w: 0.62, s: [[[0.1, 1], [0.1, 0], [0.46, 0], [0.58, 0.12], [0.58, 0.4], [0.46, 0.52], [0.1, 0.52]], [[0, 0.18], [0.66, 0.18]], [[0, 0.34], [0.66, 0.34]]] },
  '/': { w: 0.4, s: [[[0.4, 0], [0, 1]]] },
  '?': { w: 0.5, s: [[[0, 0.14], [0.12, 0], [0.38, 0], [0.5, 0.14], [0.5, 0.34], [0.25, 0.5], [0.25, 0.7]], [[0.25, 0.92], [0.25, 1]]] },
};

const SPACE = 0.34;
const GAP = 0.16;

/** Width of a painted word at letter height h, before any squeeze. */
export const paintWidth = (text: string, h: number) => {
  let w = 0;
  for (const ch of text.toUpperCase()) w += (ch === ' ' ? SPACE : (GL[ch]?.w ?? 0.5) + GAP) * h;
  return Math.max(0, w - GAP * h);
};

export type PaintOpts = {
  /** Letter height, metres. */
  h: number;
  fill: string;
  /** Outline round every stroke (the laminated-sign look). */
  outline?: string;
  /** Hard drop shadow, offset down-right. */
  shadow?: string;
  /** Stroke weight as a fraction of the letter height. Chunky signs 0.2, fine print 0.12. */
  weight?: number;
  /** Squeeze or stretch to exactly this width, as a painter fits a word to a board. */
  fit?: number;
  anchor?: 'start' | 'middle' | 'end';
  /** Forward lean, as a fraction (script-ish ads lean 0.18). */
  slant?: number;
  /** How unsteady the hand is: 0 machine, 1 a quick painter. */
  wobble?: number;
  /** Letters bob up and down on the baseline a little, the way a painted line does. */
  bounce?: number;
  seed?: string;
  opacity?: number;
};

/** A painted line of letters with its baseline at (x, y), in the parent's units (metres on a sign). */
export const Paint: React.FC<{ text: string; x: number; y: number } & PaintOpts> = ({ text, x, y, h, fill, outline, shadow, weight = 0.19, fit, anchor = 'middle', slant = 0, wobble = 0.6, bounce = 0.4, seed = 'p', opacity = 1 }) => {
  const natural = paintWidth(text, h);
  const sx = fit ? fit / Math.max(0.001, natural) : 1;
  const w = natural * sx;
  const x0 = anchor === 'start' ? x : anchor === 'end' ? x - w : x - w / 2;
  const top = y - h;
  const paths: string[] = [];
  let cx = 0;
  const chars = [...text.toUpperCase()];
  for (let i = 0; i < chars.length; i++) {
    const ch = chars[i];
    if (ch === ' ') { cx += SPACE * h; continue; }
    const g = GL[ch];
    if (!g) { cx += 0.5 * h; continue; }
    const by = (random(`${seed}b${i}`) - 0.5) * 0.08 * h * bounce;
    const rot = (random(`${seed}r${i}`) - 0.5) * 0.08 * wobble;
    for (const [si, st] of g.s.entries()) {
      const pts = st.map(([gx, gy], pi) => {
        const jx = (random(`${seed}${i}${si}${pi}x`) - 0.5) * 0.05 * wobble;
        const jy = (random(`${seed}${i}${si}${pi}y`) - 0.5) * 0.05 * wobble;
        const lx = gx + jx + rot * (gy - 0.5);
        const ly = gy + jy;
        return [x0 + (cx + (lx + slant * (1 - ly)) * h) * sx, top + by + ly * h];
      });
      paths.push(pts.map(([px, py], pi) => `${pi ? 'L' : 'M'}${px.toFixed(4)} ${py.toFixed(4)}`).join(''));
    }
    cx += (g.w + GAP) * h;
  }
  const d = paths.join('');
  const sw = weight * h * Math.min(1, Math.max(0.7, sx));
  const common = { fill: 'none', strokeLinecap: 'square' as const, strokeLinejoin: 'miter' as const, strokeMiterlimit: 3 };
  return (
    <g opacity={opacity}>
      {shadow && <path d={d} {...common} stroke={shadow} strokeWidth={sw + (outline ? sw * 0.9 : 0)} transform={`translate(${sw * 0.55} ${sw * 0.6})`} />}
      {outline && <path d={d} {...common} stroke={outline} strokeWidth={sw * 1.9} />}
      <path d={d} {...common} stroke={fill} strokeWidth={sw} />
    </g>
  );
};
