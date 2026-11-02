import { s } from './time';

/**
 * ⚠️ THE ONE TIMETABLE for "Drained", Phaister's film on the real Ermita map (IlalimNgTulay) in its own daylight.
 * docs/reports/home-scene/phaister-ermita.md is this list in prose.
 *
 * She hits the can without looking. Nemu rights it and runs her down. Teleport buys a second, and Nemu does not even slow.
 * Cut off from the line with Nemu's hand coming, she stops running and turns.
 *
 * ⚠️ THE CRUX: Curse: Drain, in slow motion, with the LRT crawling over them. Nemu's own ghost is dragged out of her into
 * Phaister's palm. The gulp, real time, and the hand is still coming: the curse has a 1.5 s fuse, as in the game. It stops an
 * inch from her hat. Nemu drops at her feet. No stamina left.
 */
export const B = {
  // I · Small to whole
  lata: s(0), // the can on the chalk; Kuro comes down to look at it
  nemu: s(3), // the taya, and her ghost at her shoulder
  twirl: s(5.4), // a slipper going round a finger, then the brim, then the grin
  walk: s(8), // she walks on to her mark
  dare: s(10.2), // and points at Nemu
  nothing: s(11), // nothing. Kuro waves.
  noLook: s(12), // she turns her back on the can and looks at us
  flick: s(14.5), // behind her back
  klang: s(15.3),
  reactN: s(15.7), // the can goes past Nemu's head. She does not look. Kuro does.
  canDown: s(16.5),
  tada: s(17),
  // II · The chase
  run: s(18.2),
  rights: s(19.1), // Nemu's hand on the can, in its own shot
  scoop: s(21.3), // she slides in and takes the slipper
  chase: s(21.8), // and Nemu is right there
  reach: s(24.5), // the hand
  blink: s(25), // Teleport: she bursts into moths
  arrive: s(25.4), // they knit her back across the lot
  notice: s(26.2), // Nemu has not slowed
  bolt: s(26.5), // she runs for the line
  cutOff: s(27.4), // Nemu is on it first
  // III · The crux
  turn: s(28), // she stops running, and turns round
  slow: s(29), // everything nearly stops. The ghost starts to come out.
  eyes: s(30), // her eyes catch
  nemuFace: s(30.8), // Nemu's fringe, and the ghost leaving her
  kuroFace: s(31.5), // the only one who is frightened
  full: s(32), // the whole of it, side on, the train over them
  gulp: s(32.6), // real time: it goes into her palm
  hand: s(33.1), // the hand is still coming. The curse is on a 1.5 s fuse.
  drained: s(34.1), // an inch from her hat, and she drops
  // IV · The settle
  joy: s(35), // the best thing that has ever happened to her
  home: s(36.4), // she steps round Nemu and strolls home
  land: s(37), // no cuts from here
  rise: s(38), // Nemu gets up, in no hurry
  back: s(39), // and walks back to her can, Kuro beside her
  atCan: s(42.9),
  round: s(40), // the lens goes round to face Phaister
  settled: s(44), // the last second is the idle's own motion
  /** Street seconds after the first plate at which the LRT is level with the court: over them, in the middle of the slow part. */
  trainLevel: s(29) / 30 + 2 * 0.12 - 5 / 18,
};
