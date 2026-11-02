export { clamp01, easeInOut, env, lerp, s, FPS } from '../lib/time';

/**
 * ⚠️ TWO CLIPS ON ONE CLOCK, as Zack's (`bgc/time.ts`). The film is frames 0 to INTRO - 1 on a 30 a second
 * timetable, shot at 60; the idle is INTRO to INTRO + IDLE - 1, repeating, and is periodic on IDLE.
 */
export const INTRO = 1350;
export const IDLE = 720;
export const ph = (f: number) => (((f - INTRO) % IDLE) + IDLE) % IDLE;
export const idleSin = (f: number, cycles: number, phase = 0) => Math.sin((2 * Math.PI * cycles * (f - INTRO)) / IDLE + phase);
