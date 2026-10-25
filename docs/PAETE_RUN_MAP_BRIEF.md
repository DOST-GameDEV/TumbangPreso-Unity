> Topic reference, reviewed 2026-10-25. Current source and [TODO](TODO.md)
> govern behavior and assignments. Historical values, proposals and unchecked
> boxes below are context, not automatic work orders.

# Paete's run: a hidden obstacle-run and surf mode (BRIEF, nothing built)

Written 2026-10-10 as the handoff into the next map. STATE: an idea in the owner's words, the facts that bear on it, and
the questions that must be answered before a blockout. No file for it exists yet. Read `AGENTS.md`, then this, then
`docs/ESKINITA_REBUILD.md` (the newest map and the pipeline to copy) and `docs/LAGOON_REWORK_GUIDE.md` section 8 (the
rock kit he names as the style).

## What the owner asked for (verbatim, 2026-10-10)

> "i wanna do a hidden gamemode obstacle run + surf for paete where people can swing around, jump/parkour, swim, and
> surf like in csgo. the design for the map will be somewhat coliseum ish so sand rocky etc. we can take inspo from the
> rocks in lagooncove for style"

So: a HIDDEN mode (not in the ordinary map vote), built round PAETE's movement, a course and not a court: swinging
(his vine), jumping and parkour, swimming, and SURFING as in Counter-Strike surf maps (a body slides along steep
angled ramps without friction and steers by air-strafing, gaining speed from the slope). The place is a coliseum of
sand and rock, in the style of Lagoon Cove's boulders.

## What exists to build on (checked in the code on 2026-10-10)

| Need | What the game has | Where |
|---|---|---|
| Swinging | Paete's vine leap with a steerable swing (side speed 7, carried in and flung out) | `Runtime/CharacterMotor.Swing.cs`, `CharacterMotor.PaeteVine.cs`, `Runtime/Abilities/PaeteHazards.cs`; `docs/CHARACTER_REDESIGN_DANTE.md` 15.10, 15.11 |
| Parkour, hops, slides | The movement rework: ground acceleration and friction, AIR STRAFE GAIN, a buffered jump that keeps speed through a landing (bunny hop), crouch, a slide from a sprint. On for the whole game and online since protocol 157 | `Runtime/MovementRework.cs`, `CharacterMotor.MovementRework.cs` (ANOTHER SESSION'S FILES) |
| Swimming | Lagoon Cove's water: bodies swim, climb out, the surface shader reads the swimmers | `Runtime/Map/LagoonWater.cs`, `CharacterMotor.Status.cs` (`IsSwimming`), `Shaders/LagoonCoveWater.shader` |
| Launchers | `JumpPad` (any model, any speed, the Arena's sound by a flag), the Arena's speed pad and stamina orb | `Runtime/Map/JumpPad.cs`, `ArenaSpeedPad.cs`, `ArenaStaminaPickup.cs` |
| A fall that is not a death | The Arena's catch and haul back, the kill plane, edge recovery | `Runtime/Map/ArenaFallRecovery.cs`, `CharacterMotor.EdgeRecovery.cs`, `KillPlane` |
| A map that is not the tag game | The prototype map and the training range start a scene without an ordinary round | `Editor/CharacterPrototypeMap.cs`, `Runtime/Diagnostics/PrototypeMapPlay.cs`, `GameLaunch.TrainingRange` |
| Rock in his style | The Lagoon rock kit: big rounded boulders piled into a massif, brushed organic edge wear, its own shader | `tools/author_lagoon_rocks.py`, `tools/bake_lagoon_rock_edges.py`, `Shaders/LagoonRock.shader`, `LagoonGround.shader`; the sand_a and rock_a textures |

## What does NOT exist (each is a decision or a piece of work)

- **SURF.** The body is a Unity `CharacterController` with a 45 degree slope limit: on anything steeper it neither
  walks nor slides in a controlled way. Counter-Strike surf is a specific rule: on a ramp too steep to stand on, gravity
  is clipped to the ramp's plane, there is no friction, and strafing into the ramp turns the fall into speed along it.
  That is NEW MOVEMENT CODE in the motor, beside the rework another session owns. It must be asked for and placed with
  that session, and it is the mode's biggest unknown: surf feel is decided by a handful of numbers (air acceleration,
  the air speed cap, the clip) that the game's present air strafe was not tuned for.
- **A course mode.** Start and finish, a clock, checkpoints and what a fall costs, who is on the course together, how
  the hidden mode is entered. None of it exists. The rules of the tag game (taya, can, slippers, the chalk box) do not
  apply; whether slippers exist here at all is a question.
- **Everyone as Paete.** The vine is his ability. "For paete" may mean every player is Paete, or every hero is lent a
  vine, or only a Paete can enter.
- **Bots.** `MapRoutes` gives a bot a walked route and a tarp; it cannot swing, surf or swim on purpose. Assume no bots.
- **The network.** Movement is simulated by its owner and checked by distance on the host (`MatchRpc.AcceptMove`,
  `MoveBudget`). Surf speeds may exceed that budget. Protocol is 158; anything new on the wire is the owner's to approve.

## ANSWERED BY THE OWNER, 2026-10-10 (these supersede the questions below)

- WHO: "everyone is paete".
- WHAT A RUN IS: "race of several players".
- A FALL: "fall is just the same as if you fall in mariokart" (picked up and set back on the course a little way back,
  losing time; not sent to the start). The Arena's rescue drone is the precedent to reuse.
- HOW HIDDEN: "we'll figure it out later".
- SURF: "i was thinking of cs surf, but i wanna see what the 2nd option looks like". A grey side-by-side was made and
  sent (`tools/author_paete_run_slide_options.py`, `Logs/paete-run/slide_options_sheet_v2.png`): slide chutes (a shallow
  banked trough ridden facing downhill, with a drop and a gap) beside surf ramps (steep ridges flown along sideways,
  hopping gap to gap). HIS PICK IS AWAITED. It is a still picture: it shows the shapes, not how either feels.
- THE COLISEUM: "is not the shape of the course or necessarily the main poi. i just said that for style reference". So
  sand, rock and worn stone are the LOOK; the course's shape is free. The spiral-up-the-stands idea below is withdrawn.

- SURF OR CHUTES, ANSWERED: "maybe just the slide shutes but try to incorporate them well into the map". So NO surf and
  no new steep-slope movement code: the slides build on the movement rework's existing slide, and each chute must be
  part of the place (a dry water channel, a spillway, a gully), not a ride bolted on.
- THREE COURSE SHAPES were then sent as MAPS (`tools/draw_paete_run_course_maps.py`,
  `Logs/paete-run/paete_run_course_maps_v1.png`): 1 the Descent (point to point, downhill), 2 the Circuit (laps round a
  flooded bowl), 3 the Gorge (a canyon that splits into a long safe slide and a short risky line of vine swings). HIS
  PICK IS AWAITED. They are drawings: two tries at 3D grey blockouts (`tools/author_paete_run_blockout.py`) could not be
  read (the rock under the course hid the course) and were NOT shown to him; the 3D blockout is owed for the shape he
  picks. As drawn each is only about 250 to 280 m and about 30 s: too short unless he wants sprints.

- ALL THREE REFUSED: "i dont want it to be a circle, i dont want it to be a plainly going down map, i dont want it to
  essentialy be a straight line in one direction. the obstacle run should be long enough". So the course TURNS EVERY
  WAY, CLIMBS AS OFTEN AS IT DROPS, CROSSES OVER AND UNDER ITSELF, and runs about two minutes. Two such shapes were sent
  (`paete_run_course_maps_v2.png`): A THE KNOT (about 957 m, 131 s: a compact ruin the course ties a knot through, five
  slides, four climbs, three swims) and B THE SWITCHBACKS (about 880 m, 120 s: zigzags up a stepped rock face and down
  the other side, the slides passing under the climb). HIS PICK IS AWAITED. Times are from guessed speeds.

- THE SHAPE IS CHOSEN: "i like b" (the Switchbacks), and "alternative routes / harder shortcuts should also be an option".
  The course is now DATA in `tools/paete_run_course.py` (27 legs, about 880 m, 132 m climbed and 120 m dropped, about
  two minutes by guessed speeds) with three shortcuts: THE LONG VINE (after leg 6 to before 11, one 51 m double swing in
  place of 116 m), THE GOAT TRACK (16 to 20, a thin line of hops down the face, 68 m for 122 m), THE HIGH LINE (21 to
  27, swings over the last pool, 77 m for 176 m). Map: `Logs/paete-run/paete_run_course_maps_v4.png`. NEXT: a readable
  3D grey blockout from that data (the first 3D tries were unreadable: do not put rock under the course until it reads),
  then the same shape in Unity as solid grey collision to run on.

- "proceed": a grey 3D blockout was built from the data (`tools/author_paete_run_blockout.py`, on stilts, no rock) and
  shown. HIS VERDICT: "the run looks too small and short, theres way too many slides imo". COURSE v2 (in
  `tools/paete_run_course.py`): 36 legs, about 1340 m, 89 m climbed and 77 m dropped, about three minutes by guessed
  speeds, a site about 220 by 240 m, lanes a third wider, and ONLY TWO SLIDES, both long (off the summit, and the last
  descent). The other ways down are a new kind, a CLIFF DIVE into a pool (twice), a swing and downhill hops. Shortcuts:
  the Long Vine (swings over the first pool), the Chimney (swings straight up to the summit), the Goat Track (hops
  cutting a corner; saves only 14 m, weak). Map v6 and blockout v4 sent; his word on them is awaited.

- "the jumps are all so uniform.. needs more uniqueness and variety per stage. add floating chained boulders etc": EVERY
  LEG IS NOW ITS OWN OBSTACLE, named and built in `tools/paete_run_stages.py` (`STAGE`, `build`): chained boulders,
  three beams, a pendulum gauntlet, a broken bridge, a colonnade, mill wheels, a log roll, seesaws, giant steps, wall
  ledges, floating rocks chained to the bed, a crumbling walk, stone drums; swings come as broken arches, one long spur
  and swaying chains; bounce pads are sand geysers. Map v7 names each stage; blockout v5 shows them. ⚠️ MANY OF THESE
  MOVE (swaying boulders, turning wheels, tipping slabs, rolling logs, falling slabs, pendulums): moving, solid things
  that several racers stand on are NEW CODE and, online, a networking question (every peer must see the same boulder at
  the same time: drive them from the match clock, as `ArenaTraffic` and `AlleyStreet` do). Not yet raised as a cost.

- "looks good. now surprise me": BUILT (2026-10-10), by two agents and checked by play probes. In the tree, UNCOMMITTED:
  `tools/author_paete_run_layout.py` (the gameplay layout, plain python: 278 solids, 53 movers on one clock, 4 pools, 4
  geysers, 18 vine anchors, 40 checkpoints; it prints every gap's and swing's margin from the body's real numbers) writes
  `Art/PaeteRun/paete_run_layout.json`; `Runtime/Run/RunMode.cs`, `RunLayout.cs`, `RunMovers.cs`, `RunHud.cs` (everyone
  Paete, 3-2-1, checkpoints in order, the fall and set-down, finish, restart; chutes CARRY a body while it is over a bed);
  `Editor/MapKit/PaeteRunSceneBuilder.cs` builds the UNREGISTERED `Scenes/Maps/PaeteRun.unity`;
  `Editor/MapKit/PaeteRunPlayProbe.cs`; the art from `tools/author_paete_run_art*.py` (a sand and boulder massif, masonry,
  chains, a sluice at the summit, 1,275 placed pieces, a model for every mover). Drive the open editor with
  `Temp/paete-run.request` (`build`, `review`, `probe`, `probe 480`); ⚠️ Unity empties `Temp` when it starts, so a
  request written before the editor is up is lost; check a Unity process exists before waiting on one.
- PLAY PROBE 4 (`Logs/paete-run/unity/probe_4.txt`, pictures beside it; 0 errors): 35 of 36 legs completed by a body's
  own input, the finish counted (state Finished, place 1 of 1), a fall caught and set down 2.0 s later on its checkpoint
  with the clock running on, all three shortcuts taken and rejoined, restart works. What it took to get there: the
  swings' take-off was moved to within 4.2 m of the catch (two rows cast 8.2 m against a reach of 8); the chutes' mid-way
  hole and step were removed (the carry stops where there is no bed); pool checkpoints widened from 8.5 to 15 m (the
  second dive, leg 27, was never counted: NOT yet re-probed after the widening).
- STILL WRONG OR UNSEEN: grey checker decks and teal placeholder solids show through the art everywhere a body stands
  (the art does not yet replace the walked surfaces); the finish ledge is bare; the art's arches on legs 4, 15 and 35 were
  placed for the old swing spacing; nobody has PLAYED it (feel, the movers under a real player, sound: there is none);
  one racer only (online needs deciding: nothing new is sent yet); no way into the mode from the game.
- ALSO THIS DAY, Paete's leap: `Runtime/Visual/PaeteCatchMarker.cs` shows where the vines will catch before the cast
  (owner: "the catch point jumping to an edge is what happens, but i feel like this snap shouldnt be seen"; he chose the
  marker). Its first cut threw every frame (a hero kit is not a component); fixed; the marker itself is still unseen.

- AFTER HE PLAYED IT (2026-10-10): "fix the grey showing through the art", "remove the placeholder blocks": the builder
  now dresses every drawn solid in the art's own stone (`PaeteRunSceneBuilder.Dress`) and stops drawing a solid whose
  top the art covers (`HideWhatTheArtCovers`: 16 of 278 so far). Then: "do the art pass on the slide walls and decks";
  "the map still looks unlively and bare, theres a lot of z-fighting platforms going on. theres no WorldLook."
- ⚠️ IN FLIGHT WHEN THIS WAS WRITTEN: TWO BACKGROUND AGENTS, started for those three complaints. A session that ends
  kills them (it happened once already: check what landed before assuming anything).
  1. ART (Blender; files `tools/author_paete_run_art*.py`, `Art/PaeteRun/Models`, `Textures`, `paete_run_art.json`,
     `materials_paete_run.json`, pictures `Logs/paete-run/art/` v4 onward): real modelled art for EVERY walked surface
     (decks on corbels and piers, hop tops as what the stage says), the two slides as worn water channels with curved
     lips and a sheet of running water, then `"hide_solids": true` once its own cover check passes; a coplanar-face
     checker run to zero; a great deal of dressing (banners, stalls, scaffolds, plants, vines, rubble, a skyline, a finish
     plaza); the swing rows on legs 4, 15 and 35 rebuilt to the layout's new spacing.
  2. UNITY (files `PaeteRunSceneBuilder.cs`, `Runtime/Run/*`, `WorldLookProfile.cs` and its asset, new shaders and
     `tools/build_paete_run_sfx.py`): a `WorldLookProfile` row "PaeteRun" (golden hour in a desert bowl) in code AND
     asset, applied by the builder; the pools on `LagoonCoveWater`; the drawn solids merged so overlapping ones do not
     flicker; life (cloth in wind, sparse dust, birds, small animals, idling geysers, torches); sound from real CC0
     recordings (a bed, positional one-shots, the race's cues).
  WHEN THEY REPORT: look at their pictures yourself before showing him; rebuild (`build review`), then one `probe 480`
  with the editor left alone (tell him first: it holds the editor in Play about five minutes); legs 27 (pool checkpoint
  widened to 15 m) and the whole course must pass again after the art and the solids' drawing change.

Still open: how long a run is; how many racers; where the water is; whether slippers or any
of the tag game exist here; how it is entered.

## Questions to put to the owner BEFORE a blockout (rules and gameplay: ask, do not assume)

1. WHO PLAYS IT: is everyone Paete, or does every hero get a vine here, or is it only for a player who picked Paete?
2. WHAT IS A RUN: a timed solo course with a best time, a race of several players at once, or a free playground with
   no clock? Checkpoints, and what a fall does (back to the last checkpoint, or to the start)?
3. HOW HIDDEN: where does a player find it (a secret in a menu, a code, a door in another map), and may it be played
   online or only alone at first?
4. SURF: real Counter-Strike surf physics (new motor code, to be agreed with the movement rework's session), or a
   lighter "slide down chutes" that the present slide could be stretched to? He said "like in csgo", so assume the real
   thing and confirm the cost.
5. SIZE AND SHAPE: about how long is one run (one minute, three)? One coliseum whose arena floor and stands ARE the
   course (a loop), or a coliseum as the start and finish with the course running out through caves, cisterns and
   aqueducts and back?
6. WATER: a flooded arena floor, pools and channels to swim through, or a sea outside?
7. Do slippers, the can or any of the tag game belong here, or is it movement only?

## A first shape to offer (mine, NOT agreed; three options are owed as grey blockouts)

A sunken sandstone coliseum, half ruined and half flooded, in a rocky bowl:
- THE RING IS THE COURSE. The run climbs the stands in a spiral: the arena floor (sand, broken columns to hop), the
  vaults under it (flooded tunnels to swim), up through a breach to the lower stands (parkour over fallen seating),
  the outer arcade (vine swings from arch to arch), the top rim (surf ramps made of the tilted, collapsed upper tier,
  banking round the bowl), and a last swing down to the box of honour.
- Why a coliseum suits it: a ring gives a loop with the whole course in view from anywhere (the next obstacle is
  always readable), arches give the vine things to catch at regular spacing, tiered seating is natural stairs and
  ledges, and collapsed tiers give the long banked planes surf needs.
- Style: Lagoon Cove's rounded boulders and brushed edge wear for the bedrock and the ruin's rubble; big dressed
  sandstone blocks with soft chunky bevels for the masonry; the approved sand for the floor; turquoise water in the
  cisterns; a look row of its own. Filipino in its details where it can be (a ruined Spanish-era fort or a quarry
  amphitheatre reads as "coliseum-ish" without copying Rome): ASK which he means.

## How to build it (the pipeline that worked for Eskinita, with its lessons)

1. Answers to the questions. Then THREE GREY BLOCKOUTS in Blender, each shown from the air AND from a player's eye
   (1.25 m, 95 degrees), as `tools/author_eskinita_blockout.py` did. He picks one.
2. The movement first, in grey: put the chosen blockout in Unity as collision only and get swing, hop, swim and surf
   FEELING right on grey boxes before any art. (Eskinita's lesson: he found the real faults only by playing.)
3. Then art: one Blender script for the architecture (data tables at the top, a `Buf` mesh builder, everything
   exported in a few big pieces plus a layout json in Unity axes), a props kit, textures copied from approved ones
   where the surface is the same, a Unity builder that reads the layout, a request file so the open editor can be
   driven from a shell, a play probe that reads its checks from the layout.

LESSONS FROM ESKINITA THAT COST TIME (do these from the first build):
- COLLISION IS SOLID PRIMITIVES, never one hollow mesh (a slipper went through a floor face and stayed inside).
- The can's floor, or here the start line, is at world height zero; the game assumes it.
- Every box keeps its own corners: never weld touching boxes before a bevel (skewed, flickering steps).
- Nothing ends exactly on another thing's plane: roofs and trims run 2 cm past walls, frames 1 cm into openings.
- The near-camera fade is shut on a map where a body is always beside a wall.
- A thing fixed to a wall is placed by code that knows where the windows are and how tall the wall is.
- A bot walks straight at its goal: one wall-to-wall flight per rise, a route graph for anything else, and the game's
  own spots (the throw standoff) must exist on the map.
- A tarp or pad touches the edge of what it throws a body onto: since the movement rework a standing bounce drifts
  only about a metre.
- Agents: one per separate piece with its own files; each compiles outside Unity before saving; the coordinator looks
  at every sheet before the owner does; motion and sound are UNSEEN and UNHEARD until he plays, and the report says so.
- When he says a thing is wrong twice, read what the code actually decided (a trace file he makes by playing) before
  changing anything else.
- "no gap" meant "there is no room". When a short note can be read two ways, zoom in and ask.
- Patch scripts go in files written with the Write tool: a shell heredoc breaks on the first apostrophe, and a line
  break inside a C# string in a patch has twice left a compile error live in his editor.

## Standing rules (unchanged)

Never commit until asked; when asked, leave out the owner's own files (the `xelu` churn, the two composition-redesign
metas, ProjectAuditorSettings, TimeManager, QualitySettings, `court_chalk.mat`, the `prop_glass` materials, `_Recovery`,
the Arena blends and kits). No em dashes. Protocol 158. His editor is open on this worktree and he plays in it: compile
outside Unity before every C# save (`bash C:/Users/StarX/AppData/Local/Temp/cc_ed.sh`), keep saves few, never batch or
kill Unity. Show a picture the moment a piece exists; say what is unseen and unheard; rules and gameplay get a question
first, look just gets done.
