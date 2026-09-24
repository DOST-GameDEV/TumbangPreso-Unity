import { s } from './time';

/**
 * ⚠️ PHAISTER'S ONE TIMETABLE, "Gulatin si Nemu". Every piece of her loop reads these;
 * docs/reports/home-scene/phaister.md § 3 is this list in prose. Seconds on HER 54 s loop (time.ts).
 *
 * The spine is a whole round of the street game told as a three-attempt joke (§ 1 of that file):
 * I · the throw (blindfolded by her own brim, behind the back, KLANG!, and the slipper ricochets onto the
 * overclock pad), II · the run for the tsinelas, the tag, the blink home, III · Grand Coven over the
 * whole street. Nemu never flinches.
 *
 * ⚠️⚠️ THE THROW WAS REMADE BECAUSE IT DID NOT MAKE SENSE. 🧑 2026-09-24, on the first master: *"why does
 * she throw a slipper and it just floats on her hand and then it flies to a wall FOR NO REASON"*, *"the
 * place she picks up slipper from isnt really the right place"*. The old throw levitated the slipper, sent
 * it the WRONG way through the bridge hoop, round a column and back, 3.4 s in the air and most of it a few
 * pixels wide, and the ricochet onto the pad happened off screen. Every link in a beat's cause and effect
 * must be SEEN: now the throw is a trick anyone reads (no look, behind the back), it flies under a second,
 * and one wide shot holds her, the can, Nemu and the pad, so the slipper is watched all the way to where
 * she later picks it up.
 *
 * ⚠️⚠️ EVERY BEAT BREATHES, AND EVERY BEAT HAS A REACTION. 🧑 2026-09-24: *"not make it too fast and give
 * each scene time to breathe so they could be pcessed by people"*, *"make her more expressive and like
 * make it showcase her personaliyt"*. At 42 s the action had 21 s and read as a rush. Now each attempt
 * is set up, lands, and is followed by her ACTING about it before the next one starts: the boast to us
 * before the throw, the blindfold and the twirl, the ta-da and the sag when Nemu does not react, the
 * stomp after the blink, the knuckles before the ultimate, the clap when Kuro finally does. Those
 * reactions are the personality (LORE.md: she *"enjoys the audience almost as much as the contest"*).
 */
export const PK = 1.0;
export const pt = (n: number) => Math.round(n * PK);

export const PB = {
  // I · Ang Tira
  dare: s(3.0), // she points the tsinelas down the court at Nemu; Nemu does not look
  boast: s(4.4), // to us: hand on her chest, a flourish and a little bow. Watch this.
  blind: s(5.8), // she pulls her brim down over her eyes: no looking
  twirl: s(7.4), // eyes covered, she twirls the tsinelas on one finger, taking her time
  release: s(10.15), // and flicks it behind her back at the can without looking
  klang: s(11.1), // KLANG!
  tada: s(11.4), // arms open to the room
  sag: s(12.5), // she looks at Nemu. Nothing. Her shoulders drop.
  skid: s(12.3), // the slipper has glanced onto the overclock pad
  // II · Ang Takbuhan
  run: s(13.4), // she sprints for her slipper
  reset: s(13.6), // Nemu drifts to the fallen can
  upright: s(14.8), // one finger: the can hops back into its circle
  scoop: s(15.5), // she slides onto the pad and scoops it: OVERCLOCK
  block: s(16.4), // she turns for home and Nemu is already there
  warn: s(16.8), // the train's shadow and hum from the south
  juke: s(17.6),
  tag: s(19.0), // Nemu reaches, the train roars over
  blink: s(19.15), // Shadow Blink: gone
  safe: s(19.8), // thrown open across the south line, safe
  deadpan: s(21.0), // Nemu turns, slowly. Nothing.
  stomp: s(22.3), // a little stamp of frustration
  // III · Grand Coven
  resolve: s(23.6), // brim down
  knuckles: s(24.4), // hands together, cracked; she walks to the middle of the line
  rise: s(26.4), // off the road
  ignite: s(27.4), // her eyes; the court burns pink
  sky: s(28.0), // arms thrown up
  build: s(28.53), // sixteen frames later the night comes down and the circle builds
  stamp: s(31.0), // it stamps; the curse closes on Nemu
  well: s(31.9), // "well?"
  nothing: s(33.0), // Nemu. Nothing.
  kuro: s(34.1), // Kuro: !
  take: s(35.2), // double take
  clap: s(36.0), // delight: a clap
  tipK: s(36.8), // and a hat tip to Kuro
  // The calm, at night
  descend: s(38.2),
  free: s(39.8),
  hat: s(43.0),
  train2: s(46.0),
  float: s(49.0),
  dawn: s(50.4),
};

/** Frames between her arms reaching the sky and the night coming down (ASTRA.md 1F, Grand Coven). */
export const COVEN = 16;
