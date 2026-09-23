import { s } from '../lib/time';

/**
 * ⚠️ PHAISTER'S ONE TIMETABLE. Every piece of her loop reads these; docs/reports/home-scene/
 * phaister.md § 3 is this list in prose. Frames at 30 fps, the shared 1260-frame (42 s) loop.
 *
 * ⚠️ IT IS A THREE-ACT TRICK, NOT A THROW. The Pledge (an ordinary can, shown and rapped), the
 * Turn (she misdirects every eye to the moon and the train, and under the train she is gone),
 * the Prestige (she is back, the can clangs, and the noise makes the serpent let the moon go).
 * That structure, not Zack's gather-throw-chase, is the spine: 🧑 2026-09-24, *"give phaister her
 * OWN animation that doesnt look too similar to zack's"*.
 *
 * ⚠️ EVERY MOMENT BREATHES (docs/HOME_SCREEN_ANIMATION_METHOD.md § 4). Offsets inside a beat are
 * written through `pt(n)`, the same re-timing factor as Zack's `bt`, so the beats were spaced for
 * reading first and every key inside one gets the same room. Natural-rate cycles (the train's
 * wheels, smoke, the doodle heartbeat) are not scaled.
 */
export const PK = 1.6;
export const pt = (n: number) => Math.round(n * PK);

export const PB = {
  // The Pledge
  present: s(3.0), // she steps to the can and shows it to us
  rap: s(4.8), // two knocks on the lid: solid, ordinary
  gaze: s(6.2), // she looks up past us at the moon; the camera follows her eyes
  // The Turn
  bite: s(7.2), // a shadow starts across the moon
  sigil: s(8.4), // the sigil, drawn the long way round, while the camera orbits her
  train: s(10.6), // a lamp down the guideway; she conducts it in
  curtain: s(11.8), // the train overhead; under its light she folds away
  empty: s(12.6), // the stage is empty and the moon is gone
  // The Prestige
  reveal: s(13.6), // behind the can, front-on, arms open, held for the room
  klang: s(14.6), // a flick: the ring flares, the can jumps and clatters down
  release: s(15.0), // the noise does it: the moon is let go
  bow: s(16.0),
  home: s(17.6), // she blinks back to her mark; the can rolls upright
  settle: s(19.2), // the long calm begins
  // Showmanship in the calm
  hat: s(23.0),
  moon: s(27.5),
  train2: s(31.5),
  tease: s(35.5),
};
