import { noise2D } from '@remotion/noise';
import { easeInOut, env, loopSin } from '../lib/time';
import { kf, outBack } from '../lib/kf';
import { PB, pt } from './beats';
import { HOME, stageAt } from './perform';
import { toCam, View } from './view';

/** Where the lens rests: on the right-hand carriageway at eye height, looking down the road. */
// ⚠️ LOW, AT 1.35 m, A LITTLE UNDER HER HAT'S BRIM. From eye height the camera looked down onto a
// pointed black hat and the first contact sheet had her face hidden under it in every bend and turn;
// from here the brim frames her face instead of covering it, which is also the heroic angle.
export const REST: View = { x: 0, z: 0, h: 1.35, pan: 0, f: 1150, px: 1150, hy: 500, roll: 0 };

const deg = 180 / Math.PI;

/**
 * ⚠️⚠️ ONE UNBROKEN SHOT, AND A CAMERA THAT NEVER STOPS MOVING. Zack's loop cuts eleven times; hers
 * never cuts once, because a trick shown without an edit is the one with nothing hidden in the edit
 * (docs/reports/home-scene/phaister.md § 2). 🧑 2026-09-24: *"use dynamic movement"*. So the energy a
 * cut would give is spent on MOVES, every one eased in and out and layered on the resting drift so
 * nothing restarts:
 *
 *  - at rest the lens walks slow arcs round her (it turns as it walks, so she holds still in frame
 *    while the street wheels behind her) and breathes in and out;
 *  - the Pledge leans in on the can;
 *  - her gaze TILTS and CRANES the lens up with her eyes to the moon (misdirection);
 *  - the sigil ORBITS her and DOLLIES IN to her face, dutched, and punches in as her eyes kindle;
 *  - the train WHIP-TILTS it up and RUMBLES it (smooth decaying noise, never a per-frame jitter);
 *  - when she is gone the lens WHIP-PANS to the can, with motion blur, and pushes in on the Prestige;
 *  - KLANG! punches it; the blink home whips it back; the false opening walks it round her again.
 */
export const camAt = (f: number): View => {
  const st = stageAt(f);
  const lean = env(f, PB.present, PB.present + pt(20), PB.gaze - pt(4), PB.gaze + pt(10))
    + 0.5 * env(f, PB.hat, PB.hat + pt(10), PB.hat + pt(24), PB.hat + pt(36));
  const gazeUp = env(f, PB.gaze, PB.gaze + pt(16), PB.sigil, PB.sigil + pt(14));
  const tilt = kf(f, [
    [PB.gaze, 0],
    [PB.gaze + pt(16), 1, easeInOut],
    [PB.sigil + pt(12), 0.5, easeInOut],
    [PB.train, 0.45],
    // The whip-tilt up to the train: fast in, a held look, then down.
    [PB.curtain - pt(10), 1.0, easeInOut],
    [PB.curtain + pt(4), 0.85],
    [PB.empty + pt(4), 0.15, easeInOut],
    [PB.release, 0.2],
    [PB.release + pt(10), 0.45, easeInOut],
    [PB.bow, 0.1, easeInOut],
    [PB.settle, 0, easeInOut],
  ]) + 0.28 * env(f, PB.moon + pt(6), PB.moon + pt(20), PB.moon + pt(40), PB.moon + pt(56));
  // ⚠️ THE ORBIT COMES BACK OUT BEFORE THE TRAIN. Walked left round her, the lens sits almost under the
  // guideway's edge, and from there the deck hides the train completely (the first train render
  // showed no train at all). Back on its mark the lens sees over the parapet to the windows.
  const orbit = kf(f, [[PB.sigil, 0], [PB.sigil + pt(32), 1, easeInOut], [PB.train - pt(4), 1], [PB.train + pt(14), 0, easeInOut]])
    + 0.55 * env(f, PB.tease, PB.tease + pt(16), PB.tease + pt(50), PB.tease + pt(72));
  // The whip to the empty stage, held through the Prestige, and the whip back when she blinks home.
  const push = kf(f, [[PB.curtain + pt(3), 0], [PB.curtain + pt(10), 0.55, easeInOut], [PB.reveal, 1, easeInOut], [PB.home, 1], [PB.home + pt(12), 0, easeInOut]]);
  const punch = env(f, PB.klang + 1, PB.klang + 3, PB.klang + 4, PB.klang + pt(14));

  const x = 0.35 * loopSin(f, 2, 0.4) - 1.1 * orbit;
  // Turn with every walk so she holds her place in frame: the lens swings round her, not past her.
  const keep = (Math.atan2(HOME.x - x, HOME.z) - Math.atan2(HOME.x, HOME.z)) * deg;
  const rum = st.rumble + 0.6 * punch;
  const n = (k: string) => noise2D(k, f * 0.35, 0) * rum;
  const wide: View = {
    ...REST,
    x: x + 0.02 * n('rx'),
    h: REST.h + 0.08 * loopSin(f, 3, 1.1) + 0.35 * gazeUp + 0.03 * n('rh'),
    pan: 1.0 * loopSin(f, 2, 2.0) + keep + 5.5 * push,
    f: REST.f * (1 + 0.025 * loopSin(f, 3, 0.3) + 0.08 * lean + 0.3 * push + 0.07 * punch),
    hy: REST.hy + 170 * tilt,
    roll: 0.6 * loopSin(f, 2, 0.5) - 2.2 * orbit + 1.5 * push * (1 - push) * 4 + 0.6 * n('rr'),
  };
  // ⚠️ THE CLOSE-UP, INSIDE THE ONE SHOT. Zack's loop earns its peak with a cut to his face; hers
  // dollies in to it instead, dutched, while she draws the ward beside her cheek, punches in a touch
  // as her eyes kindle, then pulls back out wide for the train. The lens zooms about HER FACE, so her
  // face holds still in frame while the street swells round it: no cut, no jump (method § 4.1).
  const close = env(f, PB.sigil + pt(4), PB.sigil + pt(24), PB.train - pt(2), PB.train + pt(12));
  if (close <= 0) return wide;
  const kindle = kf(f, [[PB.sigil + pt(26), 0], [PB.sigil + pt(32), 1, outBack], [PB.train, 0.6]]);
  const k = 1 + (0.85 + 0.18 * kindle) * close;
  const face = toCam(wide, [HOME.x - 0.1, 1.14, HOME.z]);
  const [fx, fy] = [wide.px + (wide.f * face.x) / face.z, wide.hy - (wide.f * face.y) / face.z];
  const [tx, ty] = [fx + (1080 - fx) * close, fy + (560 - fy) * close];
  return { ...wide, f: wide.f * k, px: tx - (wide.f * k * face.x) / face.z, hy: ty + (wide.f * k * face.y) / face.z, roll: wide.roll - 4 * close };
};

/**
 * How far the frame slides sideways between this frame and the next, in pixels, for the motion blur on
 * the whip pans. Measured off the camera itself, so any fast move blurs and a slow one never does.
 */
export const panSpeed = (f: number) => {
  const a = camAt(f - 1);
  const b = camAt(f + 1);
  const dx = ((b.pan - a.pan) / deg) * b.f - (b.px - a.px);
  return Math.abs(dx) / 2;
};
