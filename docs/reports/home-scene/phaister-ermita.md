# HOME background scene: Phaister at Ermita, "Drained"

Replaces her "Gulatin si Nemu" loop and the drawn-set "Showtime" cut. Source is `ArtSource/home-scene/src/ermita/`, compositions
`PhaisterIntro` (45 s at 60 fps, plays once) and `PhaisterIdle` (24 s, loops). Rules are [research-2026-11-01.md](research-2026-11-01.md).
To edit it, see [the workflow guide](../../../ArtSource/home-scene/README.md).

## The real map

She stands on the real `IlalimNgTulay` (display name ERMITA), recorded in Play mode through the film's camera by the same pipeline as
[Zack](zack-bgc.md): `node scripts/plates.mjs phaister`. Phaister, Nemu, Kuro, the can, the slipper and her moths are the game's own
models on the game's own shaders in that recording. Only the Drain effect, smoke, dust, impact frames and lettering are drawn over it.

- **Hour:** the map's own daylight. Nothing in the story needs night, so there is no grade over it.
- **Floor:** 0.000 under the court, measured by raycast in the batch log.
- **Traffic:** it advances on its own clock in batch: one vehicle's z was logged moving from 1.4 to 201.9 over a test run, slowing with the climax.
- **The LRT:** wound to cross during the crux on the near track (`trainDir` -1). From the lot, the deck's parapet hides nearly all of the far track. In the slow part it was logged moving z -2.5 to -6.8, and it is visible in those shots.
- **Pigeons:** left out, as in Zack's film. The sidewalk people stay; they keep to the street's edge.

## The story

1. **The throw.** Can, taya, then a slipper going round a finger, then her. She walks on, points, turns her back on the can and flicks it behind her. KLANG!, with an impact frame. Nemu does not look. Kuro does.
2. **The chase.** She runs wide for the slipper. Nemu's hand rights the can, in its own shot, and she comes up running. As the hand reaches her hat, Teleport: she bursts into moths and they knit her back across the lot. Nemu does not slow, and is on the line first.
3. **The crux.** She stops running and turns. Curse: Drain in slow motion with the LRT crawling over them: her eyes catch, Nemu's ghost is dragged out of her into Phaister's palm, and Kuro is the only one frightened. The gulp comes in real time with impact frames and a crash zoom. The hand keeps coming through the curse's 1.5 s fuse and stops an inch from her hat. DRAINED! Nemu drops at her feet.
4. **The settle.** Joy. She strolls home; Nemu gets up and walks back to her can with Kuro. With no cuts, the lens goes round to face Phaister.

The hold faces her, in the gap between HOME's tiles and mode card, with Nemu and Kuro moving past her shoulder and the viaduct and street behind them. Her mark is 1.7 m inside the line, because from the line itself the south fence stood in front of the lens.

## Her kit, against the film

| In the film | In the game | Difference |
|---|---|---|
| Moths on Teleport | `PhaisterSwarm` | The game's moth model; the film places each one so it is deterministic. Fewer stragglers |
| The ghost and motes | `VoodooSoulDraw` | Same idea and colour, drawn over the plate. It goes into her open hand, because the doll is cut from this film |
| The crimson knot over Nemu, for the fuse | `VoodooCursePresenter` | Drawn over the plate |
| Slipper at her belt during the reach | `CharacterMotor.StowsCarriedSlipper` | It sits behind her hip |
| Lit eyes | Not part of the kit's table | **Film-only**, kept from the owner's old brief for her. The rule is that eyes only glow |
| The shock ring at the gulp, impact frames, lettering | Not in the game | **Film-only**, for the single peak and the two lettered hits |

The ring on the lot from "Showtime" is gone. The Voodoo Doll is not in this film; bring it back only if asked, at its true size.

## Eyes

Her eye and lash are one black shape on the atlas. `HomeFilmPlateRecorder.Figure.KeepLashesDark` keeps the top edge and outer wing of each shape dark and lights only what is under them, and the film adds a soft bloom on top. The owner rejected tinting the whole shape: "differentiate her eyes from her eyelashes". The shape is never changed.

## Assumptions the owner may want to change

- The arms: keyed from rest, these rigs' arms swing out sideways, so her reach and Nemu's are the models' own `holding-*-shoot` clips, held, with bodies turned so the arm points at the other person. Her face stays towards the lens as a result.
- Nemu's face is hidden by her fringe by design, so Kuro carries the reactions.
- The film plays once per run of the game, per hero.

`src/ermita/beats.ts` is the one timetable.
