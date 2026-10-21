import React from 'react';

/**
 * ⚠️ THE LOOK, AS THE OWNER SET IT ON 2026-09-23: BLOCKY AND LINELESS.
 * 🧑 *"ur drawing is too circle he shulld atleat be blocking"* and *"dont use those outlines
 * please i dont know why ai keeps using the ugly red outlines"*. So every form is a chamfered
 * box or an angular shard, and nothing carries an outline. A shape is separated from what is
 * behind it by VALUE: a lit plane, a shade plane, and a thin rim of sun on the edge that faces
 * the light. It also agrees with the game, whose cast is voxel.
 */

export type Pt = [number, number];

export const polyD = (pts: Pt[], close = true) =>
  pts.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join('') + (close ? 'Z' : '');

/** A rectangle with its four corners cut at 45 degrees. */
export const chamferPts = (x: number, y: number, w: number, h: number, c: number): Pt[] => {
  const k = Math.min(c, w / 2, h / 2);
  return [
    [x + k, y],
    [x + w - k, y],
    [x + w, y + k],
    [x + w, y + h - k],
    [x + w - k, y + h],
    [x + k, y + h],
    [x, y + h - k],
    [x, y + k],
  ];
};
export const chamfer = (x: number, y: number, w: number, h: number, c: number) => polyD(chamferPts(x, y, w, h, c));

/** A box drawn as her slabs are: face, a darker floor under it, a lighter top band. */
export const Slab: React.FC<{
  x: number;
  y: number;
  w: number;
  h: number;
  c?: number;
  fill: string;
  floor?: string;
  top?: string;
  depth?: number;
}> = ({ x, y, w, h, c = 10, fill, floor, top, depth = 10 }) => (
  <g>
    {floor && <path d={chamfer(x, y + depth, w, h, c)} fill={floor} />}
    <path d={chamfer(x, y, w, h, c)} fill={fill} />
    {top && <path d={chamfer(x + c * 0.6, y + 4, w - c * 1.2, Math.max(6, h * 0.16), c * 0.5)} fill={top} />}
  </g>
);

/**
 * A limb segment from a to b as a blocky bar of width w, with square ends that overlap the next
 * segment. Rotation happens at the joints, like a voxel rig.
 */
export const barPts = (a: Pt, b: Pt, w: number, extend = 0): Pt[] => {
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  const len = Math.hypot(dx, dy) || 1;
  const ux = dx / len;
  const uy = dy / len;
  const nx = -uy * (w / 2);
  const ny = ux * (w / 2);
  const a2: Pt = [a[0] - ux * extend, a[1] - uy * extend];
  const b2: Pt = [b[0] + ux * extend, b[1] + uy * extend];
  return [
    [a2[0] + nx, a2[1] + ny],
    [b2[0] + nx, b2[1] + ny],
    [b2[0] - nx, b2[1] - ny],
    [a2[0] - nx, a2[1] - ny],
  ];
};

/** The shade half of a bar: the side away from the light, as a strip along its length. */
export const barShadePts = (a: Pt, b: Pt, w: number, side: 1 | -1, frac = 0.36, extend = 0): Pt[] => {
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  const len = Math.hypot(dx, dy) || 1;
  const ux = dx / len;
  const uy = dy / len;
  const nx = -uy * (w / 2) * side;
  const ny = ux * (w / 2) * side;
  const k = 1 - 2 * frac;
  const a2: Pt = [a[0] - ux * extend, a[1] - uy * extend];
  const b2: Pt = [b[0] + ux * extend, b[1] + uy * extend];
  return [
    [a2[0] + nx, a2[1] + ny],
    [b2[0] + nx, b2[1] + ny],
    [b2[0] + nx * k, b2[1] + ny * k],
    [a2[0] + nx * k, a2[1] + ny * k],
  ];
};
