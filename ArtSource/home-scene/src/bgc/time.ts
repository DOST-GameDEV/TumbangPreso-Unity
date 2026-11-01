export { clamp01, easeInOut, env, lerp, s, FPS } from '../lib/time';

/**
 * ⚠️ TWO CLIPS ON ONE CLOCK. The film plays once (frames 0 to INTRO - 1) and the idle holds forever
 * after it (frames INTRO to INTRO + IDLE - 1, repeating). Frame INTRO is simply the frame after the
 * film's last, so the hand-over is continuous by construction and nothing has to match by eye.
 *
 * Everything ambient is periodic on IDLE and measured from INTRO (`ph`), so it runs through the film
 * and carries on across the join.
 */
export const INTRO = 1350;
/** Three passes of the redesigned rigs' 8 s `idle`, so it tiles exactly. */
export const IDLE = 720;

/** Frames since the landing, wrapped into 0..IDLE. Negative frames wrap too: the film reads it. */
export const ph = (f: number) => (((f - INTRO) % IDLE) + IDLE) % IDLE;

export const idleSin = (f: number, cycles: number, phase = 0) => Math.sin((2 * Math.PI * cycles * (f - INTRO)) / IDLE + phase);
