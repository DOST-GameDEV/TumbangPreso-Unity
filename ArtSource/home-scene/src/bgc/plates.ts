import { platesOf } from '../lib/plates';

/**
 * No crossfade at the idle's end (owner: the cars "randomly teleport"). His hold has no people in it and its cars really loop (`LoopedCars`), so frame 719 already
 * leads into frame 0, and fading to the film's end only ghosted in cars that had crossed the frame there.
 */
export const BLEND = 0;

/** The recorded real map behind this clip's frame. plates/ is served by scripts/platesrv.mjs. */
export const { plateAt, preAt } = platesOf('zack');
