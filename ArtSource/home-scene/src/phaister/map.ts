/**
 * ⚠️⚠️ THE MAP'S OWN NUMBERS. Every constant here is read out of
 * `Assets/TumbangPreso/Editor/MapKit/IlalimNgTulayBuilder.cs`, `docs/Ilalim_Ng_Tulay.md` or `Balance`,
 * not eyeballed off a screenshot. 🧑 2026-09-24: *"make sure it actually loooks like the map too ...
 * match it more to the features of our actual map like the pc express"*, then *"i lowk dont mind if
 * some shit in animation is diff from actual as long as the major landmarks can be seen"*. So the
 * LANDMARKS sit exactly where a player has thrown past them; the dressing between them is drawn freely.
 */

/** `Balance.ConfinementRadius`: the chalk box is the carriageway, |x| and |z| <= 7. */
export const BOX = 7.0;
/** `Confinement.ThrowingLine()`: radius + 1. */
export const LINE = 8.0;
/** `Confinement.AttackerSpawnRing()`. */
export const RING = 9.0;

export const ROAD_X = 7.0;
export const KERB_TOP = 0.15;
export const KERB_IN = 6.65;
export const PAVE_TOP = 0.212;
/** The wall face and the shopfront line. */
export const FACE_X = 11.0;

/** The guideway: a 10.5 m deck over the road centre, soffit 8.0, top 9.04, tracks at +/-2.35. */
export const DECK = { half: 5.25, under: 8.0, top: 9.04 };
export const TRACK_X = [-2.35, 2.35];
/** Train: three city cars at 2x, 15.6 m the consist, 2.6 m wide, 18 m/s. */
export const TRAIN = { length: 15.6, cars: 3, width: 2.6, height: 3.1, speed: 18 };

/** Live columns at (+/-4.45, +/-10), structural pairs at +/-19, then scenery down the line. */
export const COLUMN_X = 4.45;
export const COLUMN_HALF = 0.7;
export const COLUMN_Z = [-64, -55, -46, -37, -28, -19, -10, 10, 19, 28, 37, 46, 55, 64];

/** PC Express: the authored showroom on the west wall, centred at z 5.5, about 5 m wide. */
export const PCEX = { z0: 2.95, z1: 8.05, h: 4.4 };
/** The overclock pad outside it (`BuildOverclockPad`). */
export const PAD = { x: -9.0, z: 5.5, w: 1.3, d: 2.1 };
/** The bridge hoop beside the south west column, rim about 3.07 m up. */
export const HOOP = { x: -8.9, z: -10.0, rim: 3.05, r: 0.46 };
/** East pavement stalls. */
export const PISONET = { x: 9.7, z: 3.45 };
export const PARES = { x: 8.8, z: -5.0 };

/** Cross streets: the road runs on to intersections at |z| = 31. */
export const CROSS = [31, -31];
export const CROSS_HALF = 4.5;

/** Utility poles and lamp columns, both pavements. */
export const POLE_Z = [-40, -26, -13, 1.5, 14, 26, 40];
export const LAMP_Z = [-33, -20, -6.5, 8, 21, 34];

/** Characters. */
export const HER_MARK = { x: -0.9, z: -8.7 };
export const NEMU_MARK = { x: 0.95, z: 0.55 };
