# Skill performances: progress and evidence

Plan: [plan.md](plan.md). One skill at a time. Evidence is pose sheets of the shipped glb clip
(`tools/cast_sheet.py`) and composition sketches; native captures are owed to Windows (no Unity
licence on the cloud machine).

| Skill | State | Evidence |
|---|---|---|
| Phaister GRAND COVEN (live) | **Cast rebuilt**: she draws the circle (point, sweep with the chest turning, hands up forward, clench on the 1.55 s close), upright, head level; rebaked into team-phaister and both custom rigs, `verify_hero_action.py` passes. **First person**: its own hand path (point, sweep across the bottom of the screen, lift, clench). **Circle**: shadow pool, bright cores with glow underlays on the three primary rules, a 0.95 m curtain at the rim, counter-rotating sigils, twenty orbiting and rising glyphs, a wave per curse, one snap on the close (not under reduced effects). | `sheets/hero-phaister-eclipse_before.png` (the lean back and the forward fold), `_v1`, `_v2`; `sheets/grand-coven-circle_v1.png` (before: thin rings that vanish at eye height; after: pool, cores, curtain). Engine look NOT seen. |

## Cast-sheet audit of every glb cast clip (2026-09-24)

All 17 glb cast clips were drawn with `tools/cast_sheet.py` (front three-quarter and side, eight
frames each; `sheets/<clip>.png` or `_before.png`). The recurring fault was one fault: **pitch on
a rig whose head is half its height**. A 56 degree fold plus a 34 degree head pitch does not read as
effort on this cast, it reads as falling over, hides the face and turns Phaister's brim into a slab.

| Clip | Head pitch (torso + head) before | After | Where the force went instead |
|---|---|---|---|
| Sean Supernova | -80 at the apex, **+100** on the slam (face into the road) | -28 .. +36 | fists driven into the road ahead, legs splayed on the landing |
| Zack Thunderstrike | **-82** on the call (face to the sky), +66 on the strike | -30 .. +32 | a side bend and twist for the reach, the arm level at the spot |
| Dante Seismic Stomp | +56 | +38 | wider splay, arms out |
| Dante Carapace | -49 .. +51 (a backbend) | -20 .. +16 | arms driven wider (46 degrees out), same leg splay |
| Dante Titan Fissure | -66 .. **+90** (head at the floor) | -26 .. +38 | arms higher on the raise, fists (not face) into the road, wider base |
| Cheska Glacial Nova | -23 .. +34 | -13 .. +18 | she stays upright; the arms carry it (her direction) |
| Nemu Ghost Step | -39 | -30 | unchanged otherwise |
| Nemu Astral Hijack | **-76** | -30 | higher lift, arms flung wider |
| Nemu Seance | -56 .. **+74** | -24 .. +18 | the legs tuck and the arms pull in for the collapse |
| Phaister Hex | -24 (a snap back on the draw) | -9 .. +20 | the arm draws; head level |
| Phaister Blink | -46 .. +46 | -18 .. +18 | arms and legs close in, then fling wide |

Read acceptably and unchanged: Sean Flame Rush (a 65 degree sprinter lean with the head
counter-pitched, which is correct), Ignition Cannon; Zack Bolt Sprint, Magnet; Cheska Frost Wave,
Ice Barricade; Phaister Grand Coven (rebuilt earlier). Evidence: `sheets/<clip>_before.png` against
`_v2.png` (and `_v3.png` for Roar, Summon, Seance after a second pass).

⚠️ **`author_hero_action.py` now refuses a table that exceeds the bound** (`HEAD_PITCH` -32..+40,
`TORSO_BACK` -30), so this cannot drift back one clip at a time. Every rebaked clip passes
`verify_hero_action.py` on its hero rig, `team-custom` and `team-custom-base` (strike peak at the
punch, stop after it, start and end at rest, grounded beats on the road).

**Rig compaction.** `glb_action.append_action(replace=True)` is append-only by design, so every
re-author left the old samples (and one earlier mesh repair left old geometry) in the file:
`team-custom` carried 533 dead accessors of 1116. New `tools/compact_glb.py` drops only
unreferenced accessors and views and asserts every referenced accessor and image is byte-identical.
The eight touched rigs shrank from 23.2 MB to 8.7 MB; every clip re-verified and two sheets rendered
from the compacted files are pixel-identical to the ones rendered before. Geometry, textures,
materials and triangle counts are untouched (CLAUDE.md 6.0 is not in play: nothing referenced was
changed).

Rafi's casts are Unity `.anim` files built in `HeroAbilityClips.Rafi.cs` and are not covered by the
sheet tool; they are reviewed from the code in the next pass. Engine look NOT seen for any of this.

## First-person cast paths, one per skill (2026-09-24)

`ViewmodelArms.CastGesture.cs` drew all 21 casts from seven shared shapes (Ignition Cannon, Ice
Barricade and Thunderstrike's call were one gesture). Each cast now has its own keyed path for both
hands, written from its body clip and the plan's section 3 and in its hero's motion language: Flame Rush
rakes back then drives down the middle; Ignition Cannon cups the free hand over the slipper at the
shoulder; Supernova rises out of frame and slams to the bottom centre; Bolt Sprint swings like a skater;
Magnet aims the off hand then snaps in with a chatter; Thunderstrike holds the call then points level;
Stomp hammers down and out; Carapace flexes wide and trembles; Fissure hangs then drives forward;
Permafrost is one flat right-to-left pass and Barricade one vertical line, both held with the left still;
Nova compresses then opens flat; Veil, Hijack and Seance float (no snap), Hijack flings after the spirit
and Seance collapses inward; Hex draws a circle then stamps; Blink collapses and throws open; Grand Coven
keeps its path; Crosscurrent is the off-hand cut, Mirrorwake a sell and cut back, Breakwater a cupped
hip-to-side release. Timings (contact and end) are unchanged. Evidence: `sheets/fpp-cast-paths_v1.png`,
the two hands' offset paths projected into a 16:9 frame (schematic: pivot offsets only, not the clip
rotations under them). Engine view NOT seen; owed on Windows through `CastAndMotionReel`.
