import { Layer } from '../three/actor';

const INTRO = 1350;
const IDLE = 720;
const smooth = (t: number) => { const u = Math.max(0, Math.min(1, t)); return u * u * (3 - 2 * u); };
const sin = (f: number, cycles: number, phase = 0) => Math.sin((2 * Math.PI * cycles * (f - INTRO)) / IDLE + phase);

/**
 * ⚠️ THE HOLD IS QUIET, WITH LONG GAPS BETWEEN ITS MOVES (owner, 2026-11-02: "he moves WAYY too much", "increase the distance between each movement",
 * "add more of the slow or subtle movements"). The clip's own 8 s gesture would come three times in 24 s. This layer rests over it and holds the body near the
 * clip's first pose, with only a slow breath and a small drifting look of its own. It lifts once in the 24 s (`move`), so the clip's gesture plays once.
 * Everything is periodic on the idle's clock. `strength` ramps it in during the film's settle.
 */
export const calmLayer = (f: number, opts: { hold: number; rest?: number; move?: [number, number]; ramp?: [number, number]; phase?: number }): Layer => {
  const q = (((f - INTRO) % IDLE) + IDLE) % IDLE;
  const rest = opts.rest ?? 0.86;
  const [a, b] = opts.move ?? [290, 470];
  const open = smooth((q - a) / 45) * (1 - smooth((q - (b - 45)) / 45));
  const ramp = opts.ramp ? smooth((f - opts.ramp[0]) / (opts.ramp[1] - opts.ramp[0])) : 1;
  const p = opts.phase ?? 0;
  return {
    clip: 'idle',
    t: opts.hold,
    w: ramp * rest * (1 - 0.8 * open),
    pose: {
      root: [0.5 * sin(f, 6, p), 0, 0],
      torso: [1.2 * sin(f, 6, p), 1.6 * sin(f, 2, p + 1), 0.5 * sin(f, 3, p + 2)],
      head: [1 * sin(f, 3, p + 0.5), 5 * sin(f, 2, p + 2.2), 1.2 * sin(f, 1, p)],
      armL: [1.5 * sin(f, 6, p + 0.4), 0, 0],
      armR: [1.5 * sin(f, 6, p + 0.8), 0, 0],
    },
  };
};
