# Payo: a stone stage over a pit, on a mountainside (BUILT as an unregistered sample, 2026-10-12)

Written 2026-10-12 by the session that built the playable side while the owner was away. STATE: the map can be opened
and played in the editor (`Assets/TumbangPreso/Scenes/Maps/Payo.unity`). It is in NO map list, menu, picker or build
settings, nothing is committed, and nothing new is on the network (protocol 158 is untouched). Read `AGENTS.md`, then
this, then `docs/ARENA_HANDOFF.md` (the system this map borrows) and `Logs/payo/draft/NOTES.md` (the art draft's notes).
"Payo" is the Ifugao word for a rice terrace.

> A SECOND PASS followed the same day (five layouts, a forced half time, geysers, life, sound, the hawk, the blue
> look). Its section is at the END of this file and is the current state where the two differ.

## What the owner asked for, in his words and in order (2026-10-12)

1. "a new map similar in concept to Arena, but ... aztec/ancient tropic filipino civilization ... rice terraces ... the
   main play area will be made up of stone columns/monoliths that rise up from the ground (maybe from a deep pit) ...
   the stages should be rectangular/square-ish, not circular like in arena ... the stones will be more grey than sandy".
2. "you can try having smaller moving platforms".
3. Stones that need a running jump: "yes".
4. "i meant the map changes to night at half time", "with fireflies and shooting stars". A sky goddess, Mayari, in the
   night sky over the open valley side.
5. "i dont think the whole map should be in an enclosed basin space": a mountainside, open to a valley on one side.
6. "surprise me" (he was away for five hours; the surprise is that the draft can be stood on and played).

## The decisions (settled; do not relitigate)

- THE ARENA'S GAME SYSTEM. The can at the world origin on a flat centre court, stone BLOCKS over a deep pit, a fixed
  small set of layouts (three: Liwasan the plaza, Hagdan the stairs, Pulo the islands), a block keeps its id and its
  plan and only RISES AND SINKS, the change happens at the breaks the Arena already has (8 s; 18 s at half time), open
  edges, a fall is caught far below as the Arena catches it, geysers (jump pads), speed pads, stamina pickups, two bonus
  blocks per layout.
- A few small slabs MOVE DURING A ROUND, on a clock every peer shares. Nothing is sent.
- Some stones need a running jump. Bots do not jump gaps (see Bots).
- Day for the first half, NIGHT from half time on, with fireflies, shooting stars, fire bowls and Mayari.
- No wall of scenery all round: a far camera range and fog that lets distance read.

## What is built, file by file

| File | What it is |
|---|---|
| `Assets/TumbangPreso/Art/Payo/payo_layout.json` | THE GAME'S COPY of the layout: `tools/payo_layouts_draft.json` copied whole (the plan sheet's "DRAFT 2", 21033 bytes), with a `gameCopy` line saying when. The art reads THIS file; block ids and footprints must stay stable. |
| `Runtime/Map/PayoLayout.cs` | The loader (`PayoLayout.Parse`): blocks, stairs, reach classes, pads, pickups, movers, spawns. |
| `Runtime/Map/ArenaStage.cs`, `ArenaStageMesh.cs` (EDITED) | A fifth piece kind, BLOCK (plan centre and size, a level top or a flight of stairs), and a `Stone` flag. See "What changed in the Arena's shared code". |
| `Runtime/Map/ArenaFx.cs`, `ArenaIntro.cs` (EDITED, a few lines each) | A stone stage gets the pooled light (the catch drone draws with it) and none of the stadium's show, ambience or opening film. |
| `Runtime/Map/PayoMover.cs`, `PayoMovers.cs` | The slabs that move during a round, and the carry of whoever stands on one (the run's `RunMovers`, kept to one motion). A closed form of the shared clock. |
| `Runtime/Map/PayoStageShow.cs` | The break in stone (a punch of the break's camera and dust on each block's lock), and the host's return of a BOT's slipper stranded on a stone a bot cannot reach. |
| `Runtime/Map/PayoNight.cs` | The night: `PayoNight.Factor` from the round number and the half-time break's clock, and the blend of light, air and sky from the "Payo" look row to "PayoNight". |
| `Runtime/Map/PayoNightLife.cs`, `PayoFlame.cs` | Stars, shooting stars, fireflies, and the fire bowls' flames and lights. Local, private random, no collider, nothing in the menus' map preview. |
| `Runtime/Visual/WorldLookProfile.cs` (two rows added) | "Payo" and "PayoNight". The builder writes both into `Resources/WorldLookProfile.asset` on every build. |
| `Editor/MapKit/PayoSceneBuilder.cs`, `PayoSceneBuilder.Review.cs` | The scene builder, the review renderer and the request watcher. |
| `Editor/MapKit/PayoPlayProbe.cs` | The play probe. |
| `Assets/TumbangPreso/Art/Payo/Generated/` | Meshes and materials the builder makes (mist, the stand-in look, the stand-in flames). |
| `Assets/TumbangPreso/Art/Payo/Placeholder/payo_mayari_placeholder.png` | The draft's Mayari (variant A), used only when the art gives no sky piece. |
| `Assets/TumbangPreso/Scenes/Maps/Payo.unity` | The built scene. |

Driving the open editor from a shell: write `build`, `review`, `probe` or `probe 280` (or several) into
`Temp/payo.request`; the answer is `Temp/payo.done`. Unity empties `Temp` when it starts, so check a Unity process is
running first. Menu: Tumbang Preso > Sample Map > Build Payo.

## How it works

- THE STAGE IS THE ARENA'S (`ArenaStage` with `Stone` set). Every layout's collision is solid boxes (one per monolith,
  from its top down its shaft; a stair is a body box and one box tilted 11 degrees), switched as a set at the break; the
  drawn blocks travel on the break's shared clock (a block in both layouts rises or drops, one leaving sinks 30 m into
  the mist, one arriving rises out of it; a slab that becomes a stair sinks and its stair rises). The layout is the
  Arena's own pure function of the match id and the round. A body on a sinking block needs nothing special: the break
  holds the simulation, and the round's own reset stands everybody on the new floor (the Arena's rule).
- THE TAYA'S BOX is the game's square of 7 m here, not the Arena's circle.
- THE FALL is the Arena's whole: the updraft in the slots, the catch at y -22, the drone's carry to the last safe place
  or the nearest block OF THE WALK SET, the tag's 5 s. A slipper that leaves the stage comes back to the nearest walk
  block. The catch is drawn as the Arena's grey-box drone: what catches a body here is an open question.
- REACH CLASSES. Blocks of class jump, sprint and bonus are marked so a fallen body or a returned slipper is never set
  down on them (a bot could not walk off).
- THE NIGHT. Day through the first half. In the break after the middle round the sun dims to nothing and the moon comes
  up from the valley side (the light does not swing through the ground: its angle is swapped at the dark middle), the
  air and the sky go to the night row, the fire bowls catch one after another round the stage, the stars come out and
  Mayari fades in. A peer that joins in the second half is in the night at once.
- THE ART is taken from `payo_art.json` when it is there (it was, from the third build on): 166 placements, a model per
  block (`block_ID`, `block_ID_stair`), per mover, the geyser's kerb, the speed pad, the pickup's plinth, fires that
  ride their blocks (`on_block`), windows that glow at night (`py_window`), and Mayari as the art's sky piece 2.9 km
  out. `hide_solids` true: no stand-in is drawn. Without the file the builder draws grey stand-in monoliths, pit walls
  with the north wall broken open, a plaza, a fan of terraces and far hills.

## Bots

A bot walks; it does not jump a gap, and with `AIController.EdgeSense` (on with the stage) it does not step off an edge.
Each layout has its own way points on the WALK SET (`MapRoutes`, on the layout's `Features` object, baked against that
layout's own collision without the moving slabs): Liwasan 17 points, Hagdan 18, Pulo 9, every one reachable from the
can and with a way back. So bots keep to the walk set and are not stuck; the jump stones, the bonus blocks and the
moving slabs are the humans'. A bot's slipper that comes to rest on a block off the walk set is sent back to the walk
set by the host after 2.5 s. ⚠️ Not watched for long: one 7 s look per probe (the bots moved 5 to 16 m and none fell).

## What is proven, and by what

Three play probes ran in the owner's editor (`Logs/payo/unity/probe_1.txt`, `probe_2.txt`, `probe_3.txt`, pictures
beside each). Each pins a match of 6 rounds of 30 s for that Play only and drives a body with the motor's own input.
The table says which probe saw what; the final message of the session has the last probe's exact lines.

| Claim | Evidence |
|---|---|
| The match comes up on the scene; four bodies stand on collision; the can stands at the origin on the layout's floor | probes 1, 2, 3 |
| Nothing of the stadium installs itself; the box is the square | probes 1, 2, 3 |
| All three layouts are played in one match and each break changes the layout, its colliders and its features; the blocks travel on screen through the break's own camera; every body stands on floor after | probes 1, 2, 3 |
| The walk set is walked on foot, block to block (two routes a layout: the can to an outer block over a causeway or a stair) | probe 1 (all six routes), probe 2 (three; see below), probe 3 (four; two were not started for want of round time). Every one of the six routes was walked whole in at least one probe |
| Geysers throw a body 5.9 m (6.0 expected) | probe 1 (one), probes 2 and 3 (both of the layout) |
| A body dropped into a slot is held by the updraft, caught at y -21.8 after 3.4 s, carried, set down on a walk block 4 s later, and frozen a further 4.6 s | probes 2 and 3 (the freeze timed in 3) |
| A body rides a moving slab (a ferry 9.5 m, a lift 5.2 m up and across) without leaving its top. The pistons were never ridden | probes 1 and 3 (ferry), 2 (lift) |
| A speed pad boosts. A stamina pickup was never seen TAKEN (the test body's stamina was full each time) | probes 1, 2, 3 |
| Half time lasts 18 s and the night comes during it (`Factor` 0 to 1); in round 4 the air is the night row's exactly, the key light is the moon's, Mayari is drawn, the night's life is drawn, the fire bowls are lit | probes 1, 2, 3 |
| No error or exception was logged in Play | probes 1, 2, 3 |

Probe 2's two failing lines were the probe's own: it tried to walk a body that was still frozen by the tag's 5 s after
its fall. Probe 1's one failing line was also the probe's: a stamina pickup is not taken by a body whose stamina is full.

The review stills (`Logs/payo/unity/vN/`, the newest is the one to look at) are edit-mode renders of each layout by day
and by night from the air, a three-quarter view and a 1.7 m eye on the court.

## What is NOT proven or not done

- NOBODY HAS PLAYED IT. Feel, fairness (Hagdan's taya stands on a 1.2 m dais with the attackers below it), the jump
  stones under a real player, the bonus blocks (a geyser, then 2 m of drift onto the block: never tried), the lifts
  carrying a body up to a bonus tower and off onto it.
- ONLINE, nothing was run. By reading: the layout, the movers and the night are functions of state every peer has;
  only bodies a peer simulates are carried by a slab; a remote body on a rising lift is drawn where its owner says.
- THE ARENA ITSELF was not run after its shared code changed (see below).
- NO SOUND of this map's own. The break is silent (the Arena's cues are the stadium's and synthesized); the geysers and
  pads use the Arena's existing pad sounds.
- The catch is the Arena's grey-box drone with its comic beam. Wrong for the place.
- The geyser draws the game's ordinary amber pad inside the art's stone kerb; the art's speed pad has no moving
  chevrons; the pickup's orb floats over the art's plinth.
- The paddy water is a glossy plain material, not the game's water.
- The art's flames are flat yellow shapes that breathe; no steam on the geysers; no animation of falls or cloud.
- The far plane is 3.7 km (the art's Mayari is 2.9 km out). The world outline reads a 16-bit depth: at that range its
  step is 5.6 cm, which the Arena's notes warn can speckle an edge close to the lens. Not looked for closely.
- Performance: the art is about 1.09 million triangles as placed, with no LODs. Not measured.
- Bots over a whole match, bots and the moving slabs, a slipper thrown onto a jump stone: not watched.
- In the night stills one firefly shows a thin vertical line beside it (`v5/payo_liwasan_night_eye_w.png`, top edge). Not found.
- Probes 1 and 2 ran before, and during, the art's arrival (probe 1 on the grey stand-in); probe 3 is the one on the scene as it stands, less a later change to where nine fireflies live and how big a firefly's halo is.
- Cultural accuracy (bulul figures, Mayari) has not been checked with anyone.

## What changed in the Arena's shared code

- `ArenaStage.ShapeKind` gained `Block`, LAST, so the four kinds a saved Arena scene holds keep their numbers.
  `ArenaStage.Shape` gained `X`, `Z`, `SX`, `SZ`, `Stair` (zero on every Arena piece) and the branches that read them
  (`Contains`, `HeightAt`, `Nearest`, `SameSolid`, `Inner`, `Outer`, `PiecePoints`); `MotionOf` never morphs a block;
  `TryNearestStandable` skips a sloped top (a ramp, as before, or a stair block).
- `ArenaStage.Stone` (false by default and in every saved Arena scene): when true `Start` installs nothing of the
  stadium, `ApplyLayout` uses the square box, `ArenaFx.Awake` adds no show or ambience, `ArenaIntro.Ensure` answers null.
- `ArenaStageMesh.Build` builds a block's mesh.
- Evidence the Arena is as it was: by reading (every new branch is behind `IsBlock` or `Stone`), and the Runtime,
  Editor and PlayTests assemblies compile outside Unity. ⚠️ NO Arena probe and no Arena play was run.

## Open questions for the owner

1. What catches a falling body here (the Arena's drone is the stand-in)?
2. Three layouts or five?
3. Is the taya's square of 7 m right? Liwasan's two geysers sit at (8, 8) and (-8, -8), 0.15 m outside it; the Arena
   kept its line 1.6 m clear of every pad.
4. May a bot's stranded slipper come back by itself (it does), and should a human's (it does not)?
5. Should the night also fall in a short match with no half-time break (under 6 rounds it snaps at the middle round's
   ordinary break; it does blend there too, over that break's 8 s)?
6. The geyser's look: the game's amber pad in a stone kerb, or steam and water of its own?
7. Registering the map (card, preview, pool) and its sounds: when?

## SECOND PASS, 2026-10-12 (after the owner played the first cut). READ THIS BEFORE THE SECTIONS ABOVE

Where this section and an earlier one disagree, this one is the state. Still unregistered, uncommitted, nothing new on
the network. ⚠️ `NetSession.ProtocolVersion` reads 159 in the working tree: another session's uncommitted change, not
this map's (no Payo file touches the network).

### The owner's words, in order, and what answered each

| He said | What was done | Seen working by |
|---|---|---|
| "everything looks untextured", "have you actually taken a look at how we made the other maps? theres a lack of liveliness and character to it. and the map essentially looks empty" | The art's materials are built on the shaders the Lagoon and the alley wear (`TumbangPreso/LagoonPainted`, `LagoonFoliage`, the cove's water for `py_water` with the depth request, instancing, the Lagoon's texture import rules, static flags and shadows as its builder sets them), not on plain Standard. A Lagoon Cove still is rendered by the same witness camera in every review (`compare_lagooncove_*.png`). | Review `v2` stills, looked at |
| FIVE layouts | `payo_layout.json` re-copied (34332 bytes: liwasan, hagdan, pulo, lusong, baitang); 129 solid boxes; way points baked per layout (17, 18, 9, 17, 17 points, none unreachable, none without a way back); the rotation is the Arena's own over all five | Probe 5: all five played in one match, each break changed layout, colliders and features. Lusong's can stood at y -0.60 on its floor, the bodies on the stepped court; its 0.3 m steps were walked up |
| Geysers get "water and steam of their own" | `PayoGeyser` on each pad (`JumpPad.NoLook`): a standing pool, a column that gurgles up as a body nears, a burst with a crown and thrown drops, three-lobed steam leaning with the wind, wet stone that dries; a recorded rush when it throws | Probes 5 and 6: every geyser threw 5.9 m with the amber pad off and its water seeing the throw; picture `probe_6/2_geyser.png` |
| Hagdan's dais is fair as it is | Nothing changed | n/a |
| Half time: "force it to have" | `HalftimePresentation.MiddleBreakMinRounds` (6 everywhere; `PayoNight` sets 2 while this scene is loaded, on every peer, and puts 6 back). A match of 2 or more rounds gets the 18 s middle break here | Probe 6: a 4-round match had an 18.0 s half time after round 2 and the night came during it. The Arena check confirmed the Arena still needs 6 |
| The catch is a great hawk spirit | `PayoCatchBird` draws the art's `catch_bird.glb` on the same replicated carry (`ArenaFallRecovery.NoDrone`); rules and timings untouched (caught at y -21.8 after 3.4 s, set down 4.0 s later on the walk set, frozen 4.6 s more) | Probes 5 and 6; picture `probe_6/3_catch.png` shows the hawk over the stage. Its pose and wingbeat were seen in ONE still only |
| "follow that cool teal tropical tone", then "the texture all reads as too green and teal. the reference i gave only had cool blue LIGHTING. the stones were the old grey" | The "Payo" and "PayoNight" rows are now cool blue LIGHT on whatever the stone is: a warm-white sun, blue sky and equator ambient (the shade), a neutral ground bounce, an aqua-blue haze from 80 m to 3 km, a blue zenith; the night a deep blue-indigo. No material is tinted by the builder | `v2/payo_liwasan_day_tone.png` (lit face, shade side and the valley in one frame). ⚠️ The stone in it is still blue-green: that is the art's present paint, which the art side is taking back to grey |
| "the map currently looks like an eyesore. the old process was to build the different buildings and props individually first to refine them" | The art on disk is a STAND-IN; the map will be re-dressed from approved single pieces. Nothing was polished for the present art. What is built survives a re-dress: everything below reads the art's json and warns when a model or material is missing | Builds on both art passes; the only change between them was honouring vertex colour and the new json lists |

### What the second art pass needed (and a re-dress will need again)

- VERTEX COLOUR: where `payo_art.json` has `vertex_colour`, every painted material multiplies by COLOR_0 (`_VertexTint`),
  and `py_rock` is painted too (the rock shader does not read vertex colour). Seen: the stone's tile-to-tile colour and
  the buildings' paint in `v2/payo_liwasan_day_court_n.png` and `_court_s.png`.
- `py_fall` is on a new shader, `Shaders/PayoFall.shader` (streaks that run down the fall by the art's UV, lit by the sky).
  It compiled (not pink in the stills). ⚠️ The falls read as near-white sheets; whether the streaks run was not seen (stills).
- `py_window` is dark by day and glows at night; `py_inlay` glows faintly by day and five times that at night; `py_decal`
  is cut out and casts no shadow; `py_cloud` is lit so the night darkens it; `py_mayari` is unlit, blended, at 2.9 km.
- The art's `life` list (60 entries) becomes figures `PayoActors` moves by their kind; a kind it does not know stands still
  and is warned of. Fires ride their blocks (`on_block`); 20 to 22 flames were lit at night, by layout.

### Life and sound (systems; all local, no collider, nothing sent)

- `PayoActors`: villagers stand, sit, carry, plant; a child waves; carabao, chickens, dogs, egrets, hawks. They turn to a
  throw, jump at a knocked can (`MatchFlair.Presented`, the Arena crowd's own hook), sag at a tag, cheer a round's end,
  and the people walk off home through the dusk. Probe 6: 60 of 60 out by day, a cheer answered, 6 of 60 out at night
  (the carabao and the dogs). ⚠️ Rigid figures moved whole: nobody has judged how it looks in motion.
- `PayoLife`: 550 plants lean in a gust that crosses the mountainside, the sea of cloud turns slowly, smoke at the cooking
  fire, mist at the feet of 8 of the 15 falls, swiftlets (the Lagoon's flock system) from dusk. Probe 6 counted them.
- `PayoSound` and `tools/build_payo_sfx.py`: 39 recordings, every one confirmed CC0 on its own Freesound page and
  recorded in `tools/payo_sfx_sources.json`; nothing synthesized. Beds: wind in leaves, a hill forest by day, paddy frogs
  and crickets by night, a village far off; falling water at the nearest fall; a hawk and a rooster by day, single frogs
  and an owl by night; stone grinding while the blocks travel and a thud at each lock; a gong at a round's end; the
  geyser's rush; the hawk's cry at a catch. Probe 6: 5 sources playing by day and by night with the day and night beds
  crossed, 16 world shots and 32 game moments played. ⚠️ NOBODY HAS HEARD ANY OF IT. The wind loop is only 19.7 s long.

### The Arena, re-proved

`probe arena` (the Arena's own saved scene, 3 rounds): not a stone stage, no block piece, the stadium's show, ambience,
screens and opening installed, the round box at its layout's radius, the can on its floor (at -1.20 on hukay), a body
dropped down the shaft caught at y -21.8 after 3.4 s and set down 4.0 s later, one 8.0 s break with the pieces travelling
(tore to krus), every body on floor after, the box following the layout (7.9 for krus), no forced half time, 0 errors
(`probe_7.txt`; `probe_4.txt` is the same check cut short when Play ended under it). NOT run: the Arena's own PlayMode
probes, a whole Arena match, online.

### Probes of this pass (`Logs/payo/unity/`)

| Probe | What | Result |
|---|---|---|
| `probe_4.txt` | Arena check | cut short: Play ended under it after the fall (another session uses this editor) |
| `probe_5.txt` | Payo, 10 rounds: all five layouts, half time at round 5 | 2 failing lines, the probe's own: a 9 s cap on an 18 m walk that took 9.1 s (the cap is 12 s since) |
| `probe_6.txt` | Payo, 4 rounds: the forced half time, the hawk, the figures, the sound | 0 failing lines |
| `probe_7.txt` | Arena check | 0 failing lines |

### Still not done or not seen

- Nobody has played this pass or heard it. Online: nothing run.
- No villagers on the cast's rigs (the art's figures are rigid); no dog or carabao sound; the gong is not tied to a gong model.
- Light shafts were not tried. The outline's depth step at a 3.7 km far plane was not examined.
- A thin bright line still shows beside one firefly in night stills (`v2/payo_liwasan_night_court_n.png`, by each bulul).
- The egret and hawk FLOCKS of the Lagoon's system, and the alley's chickens, are built only when the art's list brings
  none of its own (it does), so those paths have not run.
- The owner's reference picture was compared by eye only.

## THIRD PASS (2026-10-12): the assembled art's contract

Written and compiled outside Unity; NOT yet built or seen in the editor (it was closed). Nothing below is proven in a scene.

- **Blocks.** A block's model stands at (x, top - 0.01, z) turned by `blocks[].yaw`; a stair by `stair.yaw`. The
  `extension` shaft hangs on the same solid with the same origin and turn, so it rides with the block.
- **Axes.** glTFast shows a glTF point (x, y, z) at Unity (-x, y, z); at yaw 0 a piece's front faces +z. Checked from the
  glb vertices by arithmetic (not in a scene): stair_E at yaw 180 falls from 0 at x -3 to -1.2 at x +3, stair_W the other
  way, the gate's stair is on its +z side, slab_E's carved face is on +x. The review writes `cover.txt`: every block's art
  top against its collider top at nine points, per layout.
- **Materials.** `materials_payo.json` is read as specs only. A material is built the first time a glb that wears it is
  placed (`BuildMaterial`), from the texture inside that glb unless `Textures/<texture>_albedo.png` exists, and shared by
  name. Kinds: painted and cloud and decal and glow on LagoonPainted (decal cut at 0.35 with no shadow, glow with its
  picture as emission at 0.1 by day and 1.7 at night), foliage card on LagoonFoliage, water on the paddy water, fall and
  foam on PayoFall, mist on the soft sheet, unlit on the sky's blended material. A name with no spec keeps the
  importer's material and is warned of once.
- **Night.** Nodes ending `__flame` are hung on a pivot at their own foot, hidden by day and given to a `PayoFlame`;
  nodes ending `__lantern` glow with the night; `night.lights` are point lights, given to the fire within 2.5 m or, with
  none, to `PayoNight.Lamps`. Lights with `on_block` hang on that block's solid in each layout.
- **Terraces.** Placements of group "terrace" are joined into one static mesh a 72 m cell with a submesh a material
  (`Generated/terrace_X_Z.asset`); the art's own LOD per placement is kept as placed. If a mesh cannot be read they are
  placed one by one and the build says so.
- **Ground.** The firefly and figure ground probes read `payo_land*` and the joined terraces. Plants are the placements
  of group "plant". Smoke rises at `hearth_pot`; a fall's foot is the bottom of `fall_great` and `fall_medium`.
- **Review.** Stills from 1.7 m at the can in eight directions and on the four mid blocks in four, day and night, plus
  `stats.txt` (renderers, materials and triangles in the frustum from the can). Set-pass calls are the play probe's
  (the game view's own counter as round 1 goes live).

## FOURTH PASS (2026-10-17 to 10-18): the rebuilt valley, approved in Blender, brought into Unity. READ THIS FIRST

The handoff and working rules are in `docs/PAYO_HANDOFF.md`. This section records what was built and why.

### The valley (Blender), owner-steered
- Land: `tools/payo_land.py` (heightfield, terraces cut along contour lines so no edge is jagged, town tiers cut the
  same way with stone walls at the town's edge, the court platform and pool and head cut away under authored pieces;
  writes `Logs/payo/terrain/land_<tag>.npz`), `tools/payo_land_blend.py` (the land materials: painted, world-projected,
  anti-tiling as Kanto/Lagoon/Ilalim do it, hue drift, earth-and-grass valley walls from world-space noise, stone town
  walls, green paddies with rows along each field; sunset sky), `tools/payo_land_tex.py` (the textures, rice density),
  `tools/payo_dress.py` (places every piece; calm repainted pieces preferred; flora from Lagoon/Eskinita/Payo sets).
- Owner decisions in this pass: textures calm, not noisy (all pieces repainted, `Logs/payo/pieces/CALM_STYLE.md`);
  normal maps on pieces; the Toon shader is retiring; no jagged terraces; town on contour streets, houses 1.45x and
  21 m apart; the court and gorge "unpolished" until rebuilt as ONE authored stone piece
  (`tools/payo_pieces/court_build.py`, `pool_build.py`: platform, pit, gorge, stepped retaining walls, inlet weir,
  sacred pool with outlet); the head as ONE sculpted piece (`head_build.py`: horseshoe cliff, fall, plunge pool,
  outlet); the shrine (gate_portal) large and high on the cliff top above the fall; more foliage and flora; tiling
  must not show (rotation and feathering); slight hue drift on plain areas; "muddy brown with green patches" is for the
  VALLEY retaining walls only (not the paddies, not the town walls); "more grass" on them; then "looks good".
- Approved build: land v59, dress d53; frozen in `ArtSource/payo/approved-2026-10-18/`.
- Checks: `tools/payo_audit.py` (floating, sunk, straddling, z-fighting within 3 cm) on a dressed blend.

### Unity
- Export: `tools/payo_export_unity.py` (Blender `-- export`, Python `--check`): land in 256 m chunks (far chunks
  decimated), water, 105 piece glbs, `payo_art.json` (13,181 placements), `materials_payo.json` (land kind
  `payo_land`), textures to `Art/Payo/Textures/land/`. Axis: Blender (x, y, z) = Unity (x, z, y).
- Builder additions: `scale3`, block `dy`, plant LODs and far plants drawn instanced (`Runtime/Map/PayoFlora.cs`,
  `PayoSceneBuilder.Plants.cs`), far land and plants skip the outline prepass, blades baked
  (`PayoBladesBaker.cs`, `Runtime/Map/PayoBlades.cs`, `Resources/Shaders/PayoBlades.shader`), land shader
  `Resources/Shaders/PayoLand.shader`, Life re-seated and paddies found from `py_land_flooded`, compare/perf/stills
  tools (`PayoSceneBuilder.Compare.cs`, `.Perf.cs`, `PayoPerfProbe.cs`), art colliders NOT added (they broke the
  fall's void test), fall probe (`probe fall`), F9 next-round key (`PayoNight.cs`), fall catcher presentation
  (`PayoBubbleSpirit*.cs`, `PayoSpiritMeshes.cs`, shaders PayoSpirit/PayoBubble/PayoSplash; look rejected, being
  replaced), "Payo" WorldLook day row retuned to a sunset (sun yaw 150, 14 degrees; sun disc on the sky material).
- Measured in Play (owner's editor, RTX 5070): frame 16.3 to 6.5 ms median, GPU 9.6 to 5.4 ms, set-pass 5,780 to
  1,300, renderers 29,805 to 2,007; scene file 111 MB to 10 MB. Fall: 4 of 4 caught at about 3.2 s, carry 3.6 s,
  freeze 5.0 s, slipper back at the edge in 1.6 s.

### Commit set (when the owner asks; see the handoff for the exclusions)
`Assets/TumbangPreso/Art/Payo/**`, `Scenes/Maps/Payo.unity`, `Editor/MapKit/Payo*.cs` (+ PayoBladesBaker,
PayoPerfProbe, PayoPlayProbe, PayoCatcherFilm), `Runtime/Map/Payo*.cs`, `Resources/Shaders/Payo*.shader`,
`Runtime/Visual/WorldLookProfile.cs` and `Resources/WorldLookProfile.asset` (Payo row only),
`Art/MapAtmosphere/PayoSky.mat`, `tools/payo_export_unity.py`, `tools/payo_plant_lods.py`, `tools/payo_audit.py`,
`tools/payo_pieces/{court,pool,head}_*.py`, `docs/PAYO_*.md`. NOT `ArtSource/payo/`, NOT `tools/payo_pieces/catcher_*.py`.
