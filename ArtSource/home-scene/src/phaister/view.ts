/**
 * ⚠️ HER SET IS A SMALL 3D SCENE PROJECTED BY HAND, NOT A FLAT PAINTING IN LAYERS.
 *
 * The street is drawn (🧑 2026-09-24: *"keep the stylized drawing of it, dont do 3d assets"*), but
 * every shape is authored in METRES, in the map's own coordinates, and projected through this camera
 * on every frame. That is what lets one camera follow a slipper through the bridge hoop, track her
 * along the west pavement and crane up out from under the guideway, in true perspective, without the
 * set reading as cardboard sliding past.
 *
 * World: the map's. X is east, Y up, Z north, metres; the can is at the origin, the south throwing
 * line at z = -8 (`Confinement.ThrowingLine`), the guideway over the road centre
 * (docs/Ilalim_Ng_Tulay.md § 2).
 *
 * ⚠️⚠️ V2 ADDS PITCH AND NEAR-PLANE CLIPPING, AND BOTH ARE LOAD-BEARING. The first version only
 * yawed and faked tilt by sliding the horizon, which is a shift lens: fine for a nod at the moon,
 * wrong for a crane that looks DOWN at a 21 m circle. And it clamped depth at the near plane instead
 * of clipping, so a polygon passing beside the lens (a facade on the tracking shot, the road under a
 * low camera) smeared across the frame. Polygons are clipped against the near plane now.
 */

export type View = {
  /** Lens position, metres. */
  x: number;
  y: number;
  z: number;
  /** Degrees. 0 looks north (+Z); positive turns toward east (+X). */
  yaw: number;
  /** Degrees. Positive looks DOWN. */
  pitch: number;
  /** Degrees of roll, applied to the whole frame by the scene. */
  roll: number;
  /** Focal length in pixels. 1150 is about 80 degrees across 1920. */
  f: number;
  /** Where the lens axis lands on screen. */
  cx: number;
  cy: number;
};

export type V3 = [number, number, number];
export type P2 = [number, number];

const deg = Math.PI / 180;

/** Camera space: x right, y up, z forward (depth). */
export const toCam = (v: View, p: V3): V3 => {
  const dx = p[0] - v.x;
  const dy = p[1] - v.y;
  const dz = p[2] - v.z;
  const cy = Math.cos(v.yaw * deg);
  const sy = Math.sin(v.yaw * deg);
  const xr = dx * cy - dz * sy;
  const zr = dx * sy + dz * cy;
  const cp = Math.cos(v.pitch * deg);
  const sp = Math.sin(v.pitch * deg);
  return [xr, dy * cp + zr * sp, zr * cp - dy * sp];
};

/** Near plane, metres. */
export const NEAR = 0.25;

const screen = (v: View, c: V3): P2 => [v.cx + (v.f * c[0]) / c[2], v.cy - (v.f * c[1]) / c[2]];

/** A point on screen. Points behind the lens are pushed to the near plane (callers cull those). */
export const project = (v: View, p: V3): P2 => {
  const c = toCam(v, p);
  return screen(v, [c[0], c[1], Math.max(NEAR, c[2])]);
};

export const depth = (v: View, p: V3) => toCam(v, p)[2];

/** Clip a camera-space polygon against the near plane (Sutherland-Hodgman, one plane). */
const clip = (pts: V3[]): V3[] => {
  const out: V3[] = [];
  for (let i = 0; i < pts.length; i++) {
    const a = pts[i];
    const b = pts[(i + 1) % pts.length];
    const ina = a[2] >= NEAR;
    const inb = b[2] >= NEAR;
    if (ina) out.push(a);
    if (ina !== inb) {
      const t = (NEAR - a[2]) / (b[2] - a[2]);
      out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, NEAR]);
    }
  }
  return out;
};

/** A polygon's SVG path from world points, clipped; '' when wholly behind the lens. */
export const poly = (v: View, pts: V3[]) => {
  const c = clip(pts.map((p) => toCam(v, p)));
  if (c.length < 3) return '';
  return c.map((p, i) => {
    const [x, y] = screen(v, p);
    return `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`;
  }).join('') + 'Z';
};

/** An open polyline, clipped segment by segment. */
export const line = (v: View, pts: V3[]) => {
  let d = '';
  let pen = false;
  for (let i = 0; i < pts.length - 1; i++) {
    let a = toCam(v, pts[i]);
    let b = toCam(v, pts[i + 1]);
    if (a[2] < NEAR && b[2] < NEAR) { pen = false; continue; }
    if (a[2] < NEAR) { const t = (NEAR - a[2]) / (b[2] - a[2]); a = [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, NEAR]; pen = false; }
    if (b[2] < NEAR) { const t = (NEAR - a[2]) / (b[2] - a[2]); b = [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, NEAR]; }
    const [ax, ay] = screen(v, a);
    const [bx, by] = screen(v, b);
    d += `${pen ? '' : `M${ax.toFixed(1)} ${ay.toFixed(1)}`}L${bx.toFixed(1)} ${by.toFixed(1)}`;
    pen = b[2] > NEAR + 1e-6;
  }
  return d;
};

/** A direction at infinity (the moon, the stars, the sky bands): azimuth from north and elevation, degrees. */
export const projectDir = (v: View, az: number, el: number): P2 | null => {
  const p: V3 = [v.x + Math.sin(az * deg) * Math.cos(el * deg) * 1e4, v.y + Math.sin(el * deg) * 1e4, v.z + Math.cos(az * deg) * Math.cos(el * deg) * 1e4];
  const c = toCam(v, p);
  if (c[2] <= 1) return null;
  return screen(v, c);
};

/** Screen y of the horizon straight ahead: where the level plane through the lens lands. */
export const horizonY = (v: View) => v.cy - v.f * Math.tan(v.pitch * deg);

/** Pixels per metre at a world point: what a figure standing there is scaled by. */
export const scaleAt = (v: View, p: V3) => v.f / Math.max(NEAR, depth(v, p));

/** Aim a lens at a target: the yaw and pitch that put `t` on the lens axis. */
export const aim = (from: V3, t: V3): { yaw: number; pitch: number } => {
  const dx = t[0] - from[0];
  const dy = t[1] - from[1];
  const dz = t[2] - from[2];
  return { yaw: Math.atan2(dx, dz) / deg, pitch: -Math.atan2(dy, Math.hypot(dx, dz)) / deg };
};

/**
 * How a figure facing `worldYaw` appears from this lens, in the actor renderer's convention (0 faces
 * the lens, +90 faces screen right). World yaw is the map's: 0 faces SOUTH (toward a lens behind the
 * south line looking north), positive turns toward east. The renderer always looks at a figure
 * head-on, so the angle the real lens sees her from is folded into her yaw, and her pitch is the real
 * elevation of the lens above her. `roll` is the tilt of the true vertical at her place on screen, so
 * a figure off to the side of a pitched lens leans with the verticals round her instead of standing
 * bolt upright against converging lines.
 */
export const apparent = (v: View, at: V3, worldYaw: number, focusY: number) => {
  const dx = at[0] - v.x;
  const dz = at[2] - v.z;
  const view = Math.atan2(dx, dz) / deg;
  const pitch = Math.atan2(v.y - (at[1] + focusY), Math.hypot(dx, dz)) / deg;
  const a = project(v, [at[0], at[1] + focusY, at[2]]);
  const b = project(v, [at[0], at[1] + focusY + 0.5, at[2]]);
  const roll = Math.atan2(b[0] - a[0], a[1] - b[1]) / deg;
  // Facing azimuth is 180 - worldYaw, the azimuth from her to the lens is view + 180, and the renderer's
  // yaw is the second minus the first (its +90, screen right, is the lens's right-hand side).
  return { yaw: worldYaw + view, pitch, roll };
};

/** One model unit of the rigs is this many metres (`PERSON_SCALE`, the game's). */
export const PERSON_SCALE = 2.38;
