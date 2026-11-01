# HOME background scene: Phaister at Ermita, "Showtime"

Replaces her "Gulatin si Nemu" loop, which used her old model, and a quiet first attempt ("Palabas") the owner rejected: "i cant
tell wtf is happening", "it isnt as dynamic and cool as the original". The doll was also cut at the owner's request. Source is
`ArtSource/home-scene/src/ermita/`, compositions `PhaisterIntro` (45 s at 60 fps, plays once) and `PhaisterIdle` (24 s, loops).
Rules are [research-2026-11-01.md](research-2026-11-01.md). To edit it, see [the workflow guide](../../../ArtSource/home-scene/README.md).

## The story

One sentence per act, each a big action close to the lens.

1. **The throw.** She struts along the line, points at the can, boasts to us, pulls her brim down, turns her back, twirls the
   slipper and flicks it behind her back. The lens rides the slipper to the can. KLANG. Ta-da. Nemu has not moved. Her shoulders drop.
2. **The chase.** She runs for the slipper while Nemu rights the can with one finger and comes after her. She turns round into
   Nemu, runs for home under the train with Nemu on her heels, and as the hand reaches her she teleports. She skids out of a rift on
   the line. Nemu turns and keeps walking.
3. **The ring.** Enough. Arms up, the night comes down, and a ring the size of the lot draws itself round Nemu. Both hands down: a
   column of light throws Nemu back across the lot. She lands upright, dusts a shoulder, and nods once. Phaister is delighted.

The hold is Phaister in the gap between HOME's tiles and mode card, Nemu and Kuro by the can past her.

| Canon | Where | Used as |
|---|---|---|
| Treats a good setup like a performance | `LORE.md` | The boast, the no-look throw, the ta-da |
| "Wishes she would at least look surprised" | `CHARACTER_ORIGINS.md` | Nemu's flat reactions and the one nod |
| Teleport | `PhaisterHeroKit.cs` | The escape |
| Night under the LRT, sodium lamps, a train overhead | `ErmitaFilm.cs` | The hour and the chase |
| Her lunar writing is fictional; the moon a serpent swallows | `CHARACTER_ORIGINS.md` | The rings; the bitten moon |

The ring is a film's exaggeration and is not a skill in her kit. Cast: `phaister-redesign.glb`, `nemu-redesign.glb` and Kuro's pet
model. The set (`set.tsx`, `city.tsx`) is drawn from the landmarks in `IlalimNgTulay-poster.png`, in boxes like the cast.

`src/ermita/beats.ts` is the one timetable.
