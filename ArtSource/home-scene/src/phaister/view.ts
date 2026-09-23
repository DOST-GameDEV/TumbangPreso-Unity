/**
 * ⚠️ HER SET IS A SMALL 3D SCENE PROJECTED BY HAND, NOT A FLAT PAINTING IN LAYERS.
 *
 * Zack's roofdeck is a frontal painting whose layers slide at different rates. Phaister's loop is
 * ONE UNBROKEN SHOT that orbits her, tilts up to the moon and comes back down, and a layered
 * painting cannot orbit: the road, the guideway and the shopfronts all have to swing round in true
 * perspective or the camera move reads as the set sliding. So every shape in the set is authored in
 * metres, in the world, and projected through this camera on every frame. The figures (her rendered
 * model, the can) are placed with the same projection, so the stage and the actors can never
 * disagree about where the floor is.
 *
 * World: X to the right of the lens's rest line, Y up, Z away from the camera, metres. The road
 * runs along +Z; the LRT-2 guideway runs along it overhead to the left (docs/Ilalim_Ng_Tulay.md: the
 * guideway follows Aurora Boulevard, the piers stand in its median).
 */

export type View = {
  /** Camera position, metres. */
  x: number;
  z: number;
  h: number;
  /** Yaw, degrees; positive turns the lens to the right. */
  pan: number;
  /** Focal length in pixels. */
  f: number;
  /** Where the lens axis lands on screen: px is the vanishing point's x, hy the horizon's y. */
  px: number;
  hy: number;
  /** Degrees of roll, applied to the whole frame. */
  roll: number;
};

export type V3 = [number, number, number];
export type P2 = [number, number];

const deg = Math.PI / 180;

/** Camera-space depth and lateral offset of a world point. */
export const toCam = (v: View, p: V3) => {
  const dx = p[0] - v.x;
  const dz = p[2] - v.z;
  const c = Math.cos(v.pan * deg);
  const s = Math.sin(v.pan * deg);
  return { x: dx * c - dz * s, y: p[1] - v.h, z: dx * s + dz * c };
};

/** Near plane, metres. Geometry is authored to stay beyond it. */
export const NEAR = 0.6;

export const project = (v: View, p: V3): P2 => {
  const c = toCam(v, p);
  const z = Math.max(NEAR, c.z);
  return [v.px + (v.f * c.x) / z, v.hy - (v.f * c.y) / z];
};

/** A polygon's SVG path from world points. */
export const poly = (v: View, pts: V3[]) =>
  pts.map((p, i) => {
    const [x, y] = project(v, p);
    return `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`;
  }).join('') + 'Z';

/** A direction at infinity (the moon, the stars): azimuth and elevation in degrees. */
export const projectDir = (v: View, az: number, el: number): P2 => {
  const a = (az - v.pan) * deg;
  return [v.px + v.f * Math.tan(a), v.hy - (v.f * Math.tan(el * deg)) / Math.cos(a)];
};

/** Pixels per metre at a world point: what a figure standing there is scaled by. */
export const scaleAt = (v: View, p: V3) => v.f / Math.max(NEAR, toCam(v, p).z);

/**
 * How a model that faces `worldYaw` (0 = facing back down the road toward the lens's rest line,
 * positive turning toward +X, the actor's own sense)
 * appears from this camera, in the actor's own yaw convention (0 faces the lens, +90 faces screen
 * right). The actor renderer always looks at a figure head-on, so the angle the real camera sees
 * her from is folded into her yaw, and her pitch is the real elevation of the lens above her.
 */
export const apparent = (v: View, at: V3, worldYaw: number, focusY: number) => {
  const dx = at[0] - v.x;
  const dz = at[2] - v.z;
  const view = Math.atan2(dx, dz) / deg;
  const pitch = Math.atan2(v.h - focusY, Math.hypot(dx, dz)) / deg;
  // A figure to the lens's right, facing straight back down the road, is looking past the lens on
  // ITS right, which is screen right: so the viewing angle ADDS to her yaw.
  return { yaw: worldYaw + view, pitch };
};

/** One model unit of the rigs is this many metres (`PERSON_SCALE`, the game's). */
export const PERSON_SCALE = 2.38;
