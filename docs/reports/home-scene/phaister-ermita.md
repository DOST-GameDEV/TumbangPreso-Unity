# HOME background scene: Phaister at Ermita, "Palabas"

Replaces her "Gulatin si Nemu" loop, which used her old model. Source is `ArtSource/home-scene/src/ermita/`,
compositions `PhaisterIntro` (45 s at 60 fps, plays once) and `PhaisterIdle` (24 s, loops). Rules are
[research-2026-11-01.md](research-2026-11-01.md); the way the clips hand over is the same as
[Zack's](zack-bgc.md).

## The direction

The owner asked for a completely new direction. Zack's film is an action shot; hers is a stage trick, and she
never looks at it. `CHARACTER_ORIGINS.md`: "invite a chase, leave a false opening, close the circle", and
"Phaister wishes she would at least look surprised".

She is taya and turns her back on the can on purpose, bowing to an empty street. Nemu takes the shot and the can
goes. Phaister holds up one finger and does not turn. While Nemu walks for her slipper, the doll that was slumped
against the can lights from inside, gets up and rights it. Phaister's hand comes out behind her and the mark closes
on Nemu, who slows. The train goes over; in its light Phaister opens a seam and is gone. Nemu straightens up with
the slipper and Phaister is standing there, arms open. Kuro jumps. Nemu does not. The shoulders drop. Then, once,
Nemu nods, and Phaister hops for joy and blinks home because she can. All of them stay on screen.

| Canon | Where | Used as |
|---|---|---|
| The curse is a held hand, not a throw | `PhaisterHeroKit.cs` | The reach |
| Voodoo Doll, a sentient helper | `PhaisterHeroKit.cs` | The doll rights the can |
| Teleport | `PhaisterHeroKit.cs` | Both blinks |
| Night under the LRT, a sodium lamp, the train's light | `ErmitaFilm.cs` | The hour and the curtain |
| Her lunar writing is fictional | `CHARACTER_ORIGINS.md` | The rings on the floor |

Cast: `phaister-redesign.glb`, `nemu-redesign.glb`, `phaister-doll.glb`, Kuro's own pet model. The set is drawn
from the map's landmarks in `IlalimNgTulay-poster.png`, in the same way as Zack's.

`src/ermita/beats.ts` is the one timetable.

## Working on it

From `ArtSource/home-scene/`:

```
npm run refs
node scripts/look.mjs PhaisterIntro tag 300 2699
node scripts/chunks.mjs PhaisterIntro out/phaister_intro_1080p60.mp4 1 17 270 2 45
node scripts/chunks.mjs PhaisterIdle out/phaister_idle_1080p30.mp4 1 17 270 2 45
node scripts/ship-zack.mjs phaister
```

Delete `scratch/chunks_*` after changing the source. `PhaistersLoopPlaysWhenPicked` captures the film and hold
under the real board to `Logs/shots-native-ui/Hub-HomeScene-Phaister-*`.
