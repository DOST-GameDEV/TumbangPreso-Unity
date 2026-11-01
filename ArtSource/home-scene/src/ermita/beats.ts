import { s } from './time';

/**
 * ⚠️ THE ONE TIMETABLE for "Showtime", Phaister's film at Ermita, at night under the LRT.
 * docs/reports/home-scene/phaister-ermita.md is this list in prose.
 *
 * Owner, 2026-11-01, on the quiet first version: "i cant tell wtf is happening", "it isnt as dynamic and cool as the
 * original", and of the doll, "dont show her doll na". So one sentence per act, each a big action close to the lens:
 * she throws behind her back and hits the can; she runs for the slipper and Nemu nearly tags her, so she teleports;
 * she burns a ring the size of the lot under Nemu and sets it off. Nemu is unmoved by all of it, right to the last nod.
 */
export const B = {
  // I · The throw
  lamp: s(0.2),
  walk: s(3), // she struts in along the line
  dare: s(5.6), // points at the can over the slipper
  nemuLook: s(7), // Nemu and Kuro. Nothing.
  boast: s(8.4), // to us: watch this
  blind: s(10), // brim down, no looking
  twirl: s(11.2), // back to the can, the slipper spinning on one finger
  flick: s(13), // behind her back
  klang: s(13.7),
  tada: s(14.1), // arms open to the street
  nemuFlat: s(15.6), // Nemu has not moved
  sag: s(16.4),
  // II · The chase
  run: s(17),
  reset: s(17.4), // Nemu rights the can with one finger and comes
  scoop: s(19.2), // she slides in and takes the slipper
  faceOff: s(20), // she turns round into Nemu
  chase: s(20.6), // run
  reach: s(22.3), // the hand is nearly on her
  blink: s(22.8), // Teleport. The hand closes on air.
  skid: s(23.1), // out of a rift on the line
  nemuComes: s(24.2), // Nemu turns and simply keeps coming
  stomp: s(24.8),
  // III · The ring
  resolve: s(25.4), // brim up: enough
  raise: s(26.4), // arms up; the night comes down
  ring: s(27.6), // the ring draws itself across the whole lot, with Nemu inside it
  slam: s(31), // both hands down: it goes off
  laugh: s(33.2), // the best thing that has ever happened to her
  nod: s(35), // Nemu, across the lot, dusts a shoulder and nods. Once.
  settle: s(36.2),
  home: s(38),
  land: s(38.4), // the frame the player lives with
  fold: s(42),
};
