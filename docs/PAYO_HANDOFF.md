# Payo handoff (updated 2026-10-19, before the FOURTH manual compact)

Read `AGENTS.md`, then this file, then the FOURTH PASS section at the end of `docs/PAYO_MAP_BRIEF.md` (the full record
of the rebuild), then `docs/PAYO_LAND_SHADER.md` and `docs/PAYO_BLADES.md` only if you touch the land or the grass.

Worktree: `.claude/worktrees/kanto-blender-assembly-909442`, branch `QoLUpdates`. NOTHING of this pass is committed.
No background workers are running.

## Working rules (owner's standing rules)

- Context budget: stay between 200k and 500k tokens per context window; before 500k update the docs, refresh this
  handoff and ask the owner for a manual compact.
- The owner's Unity editor is usually OPEN on this worktree with the Payo scene: no batch Unity, never kill it. Every C#
  save recompiles in his editor: keep each save compiling and check `Logs/Editor.log` for `error CS` right after
  (an Editor-assembly error blocks Play for him; one happened this session: no `yield` inside a `for` iterator).
- Drive the open editor through `Temp/payo.request` (watcher in `Editor/MapKit/PayoSceneBuilder.Review.cs`): verbs
  `build`, `review`, `probe`, `probe fall`, `measure LABEL`, `stills LABEL`, `sheet`; answer in `Temp/payo.done`. Wait
  until no request file exists before writing one; never overwrite another worker's. It refuses on a dirty scene.
- Blender writes relative output paths under `C:\`: always pass absolute paths.
- No commit or push unless the owner asks in the chat. Coordinated commit: the "Eight heroes ultimate cutscenes rework"
  session (VFX coordinator) will signal when the owner's visibility fixes are in; then, with the owner's go-ahead here,
  make TWO commits (sole author, no Co-Authored-By, no em dashes, `git commit -F`): (1) the Payo set (path list in the
  brief's fourth pass, plus `docs/PAYO_HANDOFF.md`), (2) the ~88 Unity-reimported UI `.png.meta` files (brand,
  map-cards, mode-cards, owner-menu-edits; owner: keep them). NEVER commit
  `Resources/UI/composition-redesign/home-court.png.meta` or `loading-street.png.meta`, anything under
  `ArtSource/payo/` (13 GB of .blend saves, NOT gitignored: exclude explicitly), `.mp4` films, `Assets/_Recovery`, or
  `tools/payo_pieces/catcher_*.py` (concepts). The VFX session commits its own effects work and pushes after the owner
  confirms.
- Approved looks are locked; Paete's kit is locked (read-only reference).
- COMMIT NOTE added 2026-10-18: `tools/payo_pieces/catcher_cute.py` is no longer only a concept: it bakes Kanlit's game
  files (`unity` mode). Ask the owner whether it now joins the Payo commit (the other `catcher_*.py` stay out). The
  Payo set also gains: `Runtime/Map/PayoKanlit*.cs`, `Resources/Shaders/PayoKanlit*.shader`,
  `Resources/Map/PayoCatcher/`, `Runtime/Visual/WorldOutline.cs`, `Shaders/WorldOutline.shader`,
  `Shaders/LagoonPainted.shader`, `Runtime/UI/DebugBar.cs`, `Art/Payo/Textures/stone_grain_albedo.png`,
  `tools/payo_pieces/stone_grain.py`, `stone_blocks.py`, the 11 `Art/Payo/Models/ext_*.glb`.
- New props/creatures: build in the hand companion kit (`tools/hand_companion_kit.py`), review in Blender first; the
  CUTE BAR is `Logs/hand-companions/all_heroes_v1.png` (chibi proportions, huge glossy eyes with 2 to 3 highlights,
  blush, tiny mouths, chunky rounded forms, ink outline, one signature prop). Effects are 3D shapes animated in the
  shader, cost MEASURED in Play; motion judged by frame strips / onion skins, never stills.

## Where things stand

APPROVED by the owner:
- The Blender valley (land v59, dress d53; "more grass", then "looks good"), frozen in
  `ArtSource/payo/approved-2026-10-18/` (README). Live tools `tools/payo_land.py`, `payo_land_blend.py`,
  `payo_land_tex.py`, `payo_dress.py` equal the frozen copies (line endings aside).
- The Unity sunset day look ("sky looks okay"). Keeping the 88 reimported UI metas.

DONE in Unity and measured; the owner should play and judge:
- `Scenes/Maps/Payo.unity` built from `tools/payo_export_unity.py` (export of d53) and `PayoSceneBuilder*`.
- Performance, measured in Play: frame 16.3 to 6.5 ms, set-pass 5,780 to 1,300 (`Logs/payo/unity/perf/`).
- Grass and rice blades reworked (hue drift, backlit glow, wind, flowers): judge in motion.
- The fall works as designed (art colliders removed): `Logs/payo/unity/fall/after.txt`, 4 of 4 caught.
- F9 in a single-player Payo match (editor/dev builds) ends the round the normal way (`Runtime/Map/PayoNight.cs`).

## Done this window (2026-10-18, after the first compact), waiting on the owner's play

FALL CATCHER = KANLIT (owner's pick "1st design", variant A honey wood; Kanin's shape, Bulilit's carved bulul concept).
- Design: `tools/payo_pieces/catcher_cute.py` (`kanlit()`, variants `kanlit_honey/narra/ochre`, regions `wood_a/b/c`;
  `build2`, Blender `-- render v2 [quick]`, `sheet2`, `review2`; sheet `Logs/payo/pieces/catchers/cute/render_v2.png`).
- Game: `catcher_cute.py unity` bakes `Resources/Map/PayoCatcher/kanlit.bytes`, `kanlit_vine.bytes`,
  `kanlit_atlas.png`; runtime `Runtime/Map/PayoKanlit.cs` (acts: dive, vine grows round the body, haul, let go with
  flowers flying off, happy hop and spin, fly away), `PayoKanlitCatch.cs`, `PayoKanlitMeshes.cs`; shaders
  `Resources/Shaders/PayoKanlit.shader`, `PayoKanlitVine.shader` (NO ink line: owner "remove it"). Builder `Catcher()`
  in `PayoSceneBuilder.Life.cs` adds `PayoKanlitCatch` (bubble spirit kept as fallback). Kanlit holds the lead clear
  over the body's renderer top (tall forms). `probe fall`: 4 of 4, 0 errors; film/stills `Logs/payo/unity/catcher/`;
  cost 0.008 ms CPU a frame, about 32k triangles while drawn. Open: the let-go puff may be too big (asked the owner).
- AO / ink STRIPES on Payo fixed: the 16-bit depth-normals depth steps by far/65536 (5.6 cm at Payo's 3700 m). Keyword
  `WORLD_AO_FINE_DEPTH` in `Runtime/Visual/WorldOutline.cs` + `Shaders/WorldOutline.shader`, only for a far plane past
  `FineDepthFar` 1500 m (Payo only), reads `_CameraDepthTexture` as a GUARDED refinement (used only where it agrees with
  the old depth within 0.25 m + 1%, so the first-person arms, absent from depth-normals, never occlude; the unguarded
  first try caused the owner's "shading flickers like way back then"). Applied to the AO pass, its blur and the
  composite's ink edge term. Probe-only switches `WorldOutline.FineDepthTest/FineDepthView`; proof stills
  `still_floor_old/fine.png`. COST NOT YET MEASURED (it may add a depth pass on Payo).
- "textures look low res": (1) stage stone + court materials (`StoneNames`/`StoneGrain` in `PayoSceneBuilder.cs`) turn
  on `LagoonPainted`'s `_Grain` with `Textures/stone_grain_albedo.png` (`tools/payo_pieces/stone_grain.py`), new shader
  props `_GrainScale` 1.4 m, `_GrainFine` 0.37 (defaults 4 and 0 keep the Lagoon unchanged); `_GrainRange` is in
  LUMINANCE (linear space reads a 0.5 grey as 0.25): (0.18, 0.32, 0.87, 1.09), about 6 per cent. (2) the 11 `ext_*`
  shafts rebuilt at 2048 px / 28 ppm (`stone_blocks.py ext()`, `stone_build.py`, Blender `stone_weld.py`,
  `stone_calm_run.sh build`), copied into `Art/Payo/Models`.
- FPS counter in the debug strip (`Runtime/UI/DebugBar.cs`): FPS, mean ms, worst ms per half second, white/yellow/red.

## Done after the second compact (2026-10-18 evening)

- CITY FPS (task 1), measured with `PayoPerfProbe` (new: section 3 the town at the game's 95 degree FOV with toggles,
  4 the town at night, 5 a turning sweep with the worst frames, per-row "not the map" renderers and lights; a label with
  `shots` writes Play screenshots, `shotsonly` stops after them). Finding: looking at the town (SOUTH) cost about
  1,000 set-passes and 3 to 5 ms more than the valley; the plants near the town, as one object each and redrawn one by
  one in the ink's prepass, were the biggest share. FIX: `InstancedPlants` 80 m (was `NearPlants` 140) in
  `PayoSceneBuilder.Plants.cs`: plants past 80 m are drawn by `PayoFlora` (instanced, out of the prepass; they lose only
  the CPU sway and the mostly faded ink). Also every opaque dressing renderer wholly past 140 m leaves the ink's
  prepass (`InkClear`). Result (`perf/plants80_shots.txt` vs `town_c.txt`): south 11-14 ms / 1,750 set-pass to
  8.5 ms / 1,069; turning median 10.5-15 to 8.5 ms. Fine depth and stone grain measured at no cost. SEPARATE: bots'
  hero effects (Seat0 338 renderers, DanteContinentalDrift 215, ~SkyEvent/SkyFill light) spike to 18-25 ms; sent to
  the VFX session, which is tracing them on its side.
- TEXTURES (owner: noise "a bit too much", edges "too blurry and low qual"): `stone_kit.py` `UP = {"core": 2}` paints
  the court core's atlas at 2x (4096) into the same layout; a region keeps the 2x painting only where it matches the
  approved one (9 of 22 regions; the rest the approved painting scaled). The fine grain is replaced by a COLOUR MOTTLE
  (`tools/payo_pieces/stone_mottle.py` -> `Textures/stone_mottle_albedo.png`, soft greener/redder patches, 10 m box,
  `_GrainHue` 0.06 in `LagoonPainted`). `stone_grain.py` / `stone_grain_albedo.png` are now unused.
- HAZE (task 2): `Runtime/Map/PayoHaze.cs` (on the Night holder) sets globals; `WorldOutline.shader` pass 0 `frag`
  wraps the old body (`Ink`) and adds an aerial haze from the camera depth under `WORLD_AO_FINE_DEPTH` (Payo only):
  exponential with distance, pooling low, in `RenderSettings.fogColor`. Proof `perf/haze_compare.png` (sent).
  NOTE: the editor `stills` never run `WorldOutline` (not ExecuteAlways): judge AO, ink and haze from Play shots.
- GRASS (task 3, in progress): `PayoBlades.shader` `_GrassGrow` 2.2 / `_GrassGrowFrom` 10 / `_GrassGrowTo` 90 /
  `_GrassFarLift` 0.3 (far tufts bigger and lighter). Subtle so far; asked the owner where the grass reads worst.
- Geyser VFX (task 6): requested from the VFX session, which did NOT build it (it relayed "give the geyser back to the
  map session"). Owner, 2026-10-18: if the VFX session did not do the geyser effect, "thats not ur task". So the map
  session does NOT build or change the geyser's water/steam/burst (`PayoGeyser` stays as it is); only the stone bowl
  art is ours, as part of the pad remodel (task 5).

- SLIPPERS (owner: "can you put in the new slipper designs too", "Logs/slippers"): the ten remodels from
  `tools/build_slippers.py` (sheet `Logs/slippers/slippers_v6.png`, built 2026-10-11/12) copied over
  `Art/models/tsinelas_ID.glb` (metas kept); Sike is a new `tsinelas_sike.glb`, its old `.obj/.mtl` moved to
  `Logs/slippers/replaced_2026-10-18/` with the old glbs and roster assets; `RosterBookBuilder` row for sike now `.glb`.
  `Editor/SlipperInstall.cs` (request `Temp/slippers.request`) relinked all ten `Resources/Roster/slipper_ID.asset`
  (every link had changed: a glTF's main object id follows its scene). Seen in Play held in hand
  (`perf/slip_shotsonly_s.png`). Not touched: `pangbanyo` (not in the new set), the cutscene prop
  `UltimateProps/tsinelas.glb`. Commit with the slipper set (not the Payo commit): the 10 glbs, the sike glb + meta,
  the 10 roster assets, `RosterBookBuilder.cs`, `SlipperInstall.cs`, the removed sike obj/mtl, `tools/build_slippers.py`,
  `tools/weld_slippers.py`, `tools/review_slippers.py`.
  Then owner: "the slippers are backwards": the game's shoes carry the toe along -X, the built ones +X. New
  `tools/slipper_turn.py` turns a glb half round in its vertex data; all ten installed from `Logs/slippers/build/` through
  it (noted in `build_slippers.py`'s header). Proof `perf/grass_slipper_compare.png`. Also commit `tools/slipper_turn.py`.
- GRASS, owner: "the grass needs to be wayy taller": `PayoBlades.shader` `_GrassTall` 2.6 (blades stretch up, flower
  heads ride up on their stems), `_GrassGrow` eased to 1.5. Proof `perf/grass_slipper_compare.png`.
  Then "the terrace grass reads too short": `_GrassTall` 4.5, new `_RiceTall` 2.2, far growth (`_GrassGrow` 1.8 by
  90 m) now for rice too; the baker's chunk bounds pad 9 m in y (`PayoBladesBaker.cs`) so tall blades are not culled.
  Proof `paddies/terrace_grass_compare.png`, `perf/lawn_45.png`. ⚠️ Edits to `payo_blades.mat` on disk reach the editor
  only after a refresh: `Temp/paddy.request` shots do not refresh (send `refresh` to payo.request first).
- Tasks 4 (flooded paddies) and 5 (meso pads + geyser bowl art) were given to two background agents
  (2026-10-18): the paddies agent may edit the PayoLand shader and the land material section of `PayoSceneBuilder.cs`;
  the pads agent writes only `tools/payo_pieces/pad_*.py` and `Logs/payo/pieces/pads/` (the main session installs).
- PADS (task 5) INSTALLED: `tools/payo_pieces/pad_meso.py` + `pad_run.sh` (`build`, `review vN [props]`),
  `pad_review.py`, `pad_sheet.py`; sheets `Logs/payo/pieces/pads/pads_v1.png` (geyser bowl, plinth) and `pads_v2.png`
  (speed pad v2: bold filled cream chevrons, plain red channel). Footprints, origins, heights identical to the old ones
  (geyser inner wall 0.62 m for `PayoGeyser`'s pool, speed travel +z). Copied over `Art/Payo/Models/pad_geyser.glb`,
  `pad_speed.glb`, `pickup_plinth.glb` (old ones in `Logs/payo/pieces/pads/replaced_2026-10-18/`), scene rebuilt, 0
  missing. Not yet judged up close in Play (the court-eye shots do not show them). Geyser water: unchanged, not ours.
- WATERED PADDIES (task 4) by the agent: `tools/payo_pieces/paddy_flood.py` picks whole fields in 70 m noise patches
  among sparse-rice, non-gold fields (286 fields, about 21% of the paddy area; tune `--share`/`--seed`, rerun, `build`)
  into `Textures/land/paddy_flood.png/.txt`; `PayoLand.shader` draws sky-reflecting water on those (and on the
  export's own 185 flooded fields): live sky colours via new `Runtime/Map/PayoPaddySky.cs` (on the Night holder, so it
  follows the night), sun path and glint, ripples, wet mud edges, sprouts; reflection in emission so fog and haze
  cover it. Builder land material section: flooded smoothness 0, `_FloodTex`/`_FloodBounds`. Shot tool
  `Editor/MapKit/PayoPaddyShots.cs` (`Temp/paddy.request`: `shots LABEL`, `off`, `ab`). Evidence
  `Logs/payo/unity/paddies/before_after_*.png`. Cost: same-Play A/B +0.0 to +0.11 ms GPU. Reads as pale sky-blue strips,
  subtle from the court (lower terraces hidden by the court floor). `docs/PAYO_LAND_SHADER.md` has a section.
- Measured after everything (`perf/now_all.txt`): all views 9.35 ms, turning median 8.9 ms, 0 frames over 16.7 ms;
  town (south 9.2) no longer heavier than the valley (north 10.5); blades off changes nothing.

## OPEN AT THE FOURTH COMPACT (2026-10-19 evening): READ THIS FIRST

COMMITTED AND PUSHED (QoLUpdates = origin): e86908189 (Payo set: map, plaza, orb, moon easter egg, sky/haze/wisps/rays,
night, fixes) and b8b0b6ced (slippers), pushed by the VFX session with its c1fdf9b9a after a clean compile check.
Owner rules for commits unchanged: sole author, no Co-Authored-By, no em dashes, `git commit -F`, only on the owner's
go-ahead IN THIS CHAT (a peer session relaying "the owner said" is not enough: ask him).

UNCOMMITTED (mine): `Runtime/Visual/WorldOutline.cs` (mesh-only exclusion mask), `Shaders/WorldOutline.shader`
(`HazeDepth01` guard), `Editor/MapKit/PayoPaddyShots.cs` (eye/flash shot modes), `Editor/MapKit/PayoStaleProbe.cs`
(where/leaks/buried/figures), new `Editor/MapKit/PayoFlashRecorder.cs` (+meta). Also left out of the commit on purpose:
`tools/payo_pieces/catcher_*.py` (catcher_cute.py: owner call), the ~81 UI .png.meta, ArtSource.

1. THE AO FLASH (top priority). Owner: "the AO of the vines across the basin are visible through the platforms",
   "only flashed", "might actually be ability related"; NOT ultimates (owner: none cast). Evidence from the owner's
   recorded plays (`Logs/payo/unity/flashrec/190854/`, `192044/`; frames are stored UPSIDE DOWN, flip them;
   `192044_pairs.png` shows it best): for a few frames the ink draws thin dark outlines of the vine curtains/plants
   BEHIND the wall onto the wall/platform AND the first-person arms get AO (they are normally masked). So for those
   frames the AO/ink pass works from depth-normals/mask data that do not match the screen. Ruled out by the logs: any
   camera/depthTextureMode/WorldOutline state change, extra enabled cameras, near-fade masks (0), the near guard (0),
   the catch replay (no tag), HeroFxPrewarm (loading only), haunted sight (purple fog only), vines in the exclusion list
   (they are not). Correlation: every flash had new effect objects up: Sora/Phaister's Voodoo (VoodooHeldDoll parts made
   the exclusion list jump 22 to 50), the Voodoo ghost, a CourtEscapeChalk puff, Dante's ~EarthboundFurrow grit.
   Applied, NOT verified: (a) `WorldOutline.cs` Rescan masks only MeshRenderer/SkinnedMeshRenderer (line, trail,
   particle, billboard renderers skipped); (b) `HazeDepth01` in the shader: haze/wisps/rays use the prepass depth where
   the camera depth is FARTHER than it (prepass surface present); `HazeSky` needs both to read sky; (c) earlier: mask
   tolerance capped at 0.2 m past `FineDepthFar`. NEXT: the owner records another round with Sora using Voodoo near
   him (recorder is armed per editor session: `Temp/rec.request` `on`/`off`; on Play exit it writes the last 300 frames
   + a per-frame log of cues, flairs, voice, cameras, depth flags, the outline pass's exclusion list and near-fade
   counts, and effect roots each second into `Logs/payo/unity/flashrec/TIME/`, path in `Temp/rec.done`). Find flash
   frames by frame-to-frame change (they come in alternate-frame bursts), flip, compare with the log around them. If
   Voodoo still triggers it, read Phaister's Voodoo code (VoodooSoulDraw, VoodooCursePresenter, HiddenFromMainCamera /
   MainCameraOnly toggle renderers in Camera.onPreCull, ViewmodelArms.CastGesture) for anything that renders, toggles
   renderers mid-frame or swaps the arms' renderers; the hero kits belong to the VFX session ("Eight heroes ultimate
   cutscenes rework"): report there, fix only WorldOutline's side here.
2. MAYARI GODDESS: the VFX session is modelling her (`tools/build_payo_mayari.py`) to the KNL1 contract in
   `PayoMayariGhost.shader`'s header (bake over `Resources/Map/PayoMoon/goddess.bytes`); sheet
   `Logs/payo/unity/moon/mayari_sheet_v1.png` goes to the owner first. Re-film with `film LABEL` in `Temp/moon.request`.
   Moon v11 is APPROVED and locked (backup `Logs/payo/pieces/moon/approved_v11/`).
3. Boulder leftovers: the VFX session fixed `DanteBoulderStone.cs`; after a played round run `leaks` in
   `Temp/stale.request` and tell it whether any BoulderCast/BoulderHit roots remain shown (last check: none).
4. Waiting on the owner's verdict: basin dressing + bigger bulul, stamina orb, wisps, night look, floating villagers fix
   (26 brought down, 12 left out).

Tools added this window: `Temp/paddy.request` `shots eyeLABEL x y z lx ly lz` (one view: as played, view4 vines off,
view5 WorldOutline off, view6 PayoHaze off; add `flash` to the label to watch ~1,500 live frames and keep jumps),
`strip`, `orb` labels; `Temp/stale.request` `where` (game camera place, in eye form), `leaks`, `buried`, `figures`,
`clean`; `Temp/rec.request` (flight recorder). The editor only picks up new scripts after a refresh: write `refresh`
to `Temp/payo.request` (unknown verb, still refreshes) when nothing else is queued.

## Done after the third compact (2026-10-19)

- SUN: `SunYaw` 180 built; the disc sits in the valley's mouth looking north. Owner "sun is too blurry", "still hard to
  see": new `_SunCrisp` in `NeighbourhoodSky.shader` (0 = the Lagoon's approved feathered sun, unchanged), Payo sets
  0.85 in the builder's sky lines: thin edge, hot near-white face, 75% less halo. Proof `paddies/rays2_rim_ne_far.png`.
- HAZE CEILING: `WorldOutline.shader` height term is now `1 - smoothstep(base, base + w, y)`; `PayoHaze` Base -45,
  Thins 45 (none above the court's level). Owner "a bit too dreamy/haze-y": Strength 0.38 (night 0.28), Start 40,
  Reach 700. `PayoHaze`'s fields are now `[NonSerialized]`: the code is the one place to tune them, no rebuild needed.
- WISPS (owner: "should have some wispy moving clouds in the haze", "make the cloud wisps move"): two flat cloud sheets
  at y -4 and -18 in the same composite pass (`HazeWisp`), drawn out down the valley (`WindHeading` 15), drifting
  across it (`DriftHeading` 80, 3 m/s) and changing shape over time; shown only where the eye's ray crosses a sheet
  above the land, faded in the first 25 to 90 m. Proof `paddies/wisp3_over_ne.png`. Motion NOT yet judged on film.
- GOD RAYS AND LENS FLARE (owner: "can we have some god rays and lens flare?"): `SunLight()` in the same pass: 24
  depth taps toward the sun's screen spot count open sky (shafts cut by land, trees, grass; the open sky gets only 8%),
  plus five soft painted rings and a thin streak, dimmed by how much of the sun is hidden. `PayoHaze` Rays 0.25,
  Flare 0.18, both off through the dusk. `WorldOutline.cs` also passes `_HazeToView`. Measured with the haze,
  wisps, rays and flare all on (`perf/sunfx2.txt`): all views 8.14 ms, GPU 5.47 (was 9.35 / 6.69 in `now_all.txt`),
  turning median 8.32 ms; 4 frames over 16.7 ms while turning (worst 31.6) where there were 0, probably the agent's
  editor work during the run: recheck when the editor is quiet.
- PAETE MARKERS: VERIFIED, 91 `PaeteCatchMarker` copies (89 showing) had stacked over the session's Play runs
  (HideAndDontSave outlives Play in the editor). Minimal fix in `Runtime/Visual/PaeteCatchMarker.cs`: `Install()`
  destroys earlier copies first, and the marker destroys itself on `OnApplicationQuit`; look and rules untouched. New
  `Editor/MapKit/PayoStaleProbe.cs` (`Temp/stale.request`, `clean` to clear) counted and cleared them: now 0 after Play.
- BASIN WALLS AND BULUL: given to a background agent (owner: "get a parallel agent to work whats on the basin walls
  itself", "bulul statues should be larger and look more grand", then "current model for bulul is okay, they just need
  to be sized up, no need to remodel"). DONE by the agent (2026-10-19):
  - The bulul models are UNCHANGED, only sized up. New pieces, built and reviewed ALONE first in Blender:
    `tools/payo_pieces/plaza_kit.py` (+ `plaza_review.py`, `plaza_run.sh build | review vN | install`) makes
    `plaza_plinth` (stepped plinth, the walls' fret course painted and carved round it, woven-band cap) and
    `plaza_planter` (stone trough, woven-band rim), both `py_court_stone` UV'd into the court's trim atlas, so they wear
    the court's own material and normal map; and `plaza_vine` / `plaza_vine_long` (creeper curtains, Lagoon leaf and
    stalk materials). Reviews `Logs/payo/pieces/plaza/shots/v1_*.png`, `v2_*.png`; glbs in `Art/Payo/Models/plaza_*.glb`.
  - Placement: NEW `Editor/MapKit/PayoSceneBuilder.Plaza.cs` (`Plaza()`), ONE hook line in `Build()` right after
    `Dressing`. The art's 2 seated bulul: 1.4 on plinths at 1.2 (x +-37, z 6); the 2 standing at the gorge mouth: 1.7
    on plinths at 0.9, turned to the court; a NEW standing pair at the south corners (+-37, -54); 2 fire bowls flank
    every plinth (lit at night, own lamps). 43 creepers over the pit's rim lip and down the tall retaining walls (clear
    of the sun panels and the west niches), 22 planted planters (parapets, south wall, the lip's paving side), 4
    tree_round in the south corners, 10 banners at 1.35 along the lip, a rice-pounding corner either side of the gate,
    52 grass/fern tufts in the joints. No colliders (all of it is outside the play walls at 22 m). Every piece meant
    for the paving is SEATED by rays against a collider lent to the court mesh (and boxes round the pieces already
    placed), at its middle and four footprint points, and dropped unless all five meet open paving at y -6: the last
    build dropped 41 tufts (inside planters or banners), moved none; the build log line `[Payo] plaza:` says why.
  - Play shots: NEW `Editor/MapKit/PayoPlazaShots.cs` (`Temp/plaza.request` `shots LABEL`; `night` inside the label adds
    night views; the arms are hidden). Before/after `Logs/payo/unity/plaza/before_*.png` vs `a4night_*.png` (day, and
    `_night` for high_e and edge_s; `a1` to `a3` intermediate).
  - Cost (`perf/plaza2.txt`, 395 renderers, the creepers cast no shadow): all views 8.66 ms, GPU 5.56 (sunfx2 before
    it: 8.14 / 5.47); same-Play toggle looking north: Plaza off GPU 4.75 vs on 5.29 ms, about 0.5 ms GPU.
    (`perf/plaza.txt` was a disturbed run: ignore it.) Open: the owner's look; the south band's middle is kept clear on
    purpose; more could go there if he still finds it open.

- LATER 2026-10-19 (main session):
  - WISPS VOLUMETRIC (owner: "barely notice the wisps moving", "arent volumetric much?"): seven slices between y -24 and 0,
    rounded billows lit on top, drift 7 m/s across the valley (`DriftHeading` 80), shape evolving; strip proof
    `paddies/strip2_sheet.png` (new `strip` label in `PayoPaddyShots`: six frames 1.5 s apart, day and night).
  - NIGHT (owner: "too sharply dark and high contrast", haze/fog not noticed): `PayoNight` row in BOTH
    `WorldLookProfile.cs` and `Resources/WorldLookProfile.asset`: ambient lifted, shadow lift .035/.045/.085, shadow
    strength .55, fog .09/.13/.27 from 60 m; `PayoHaze` night colour = fog x2.2 (a moonlit mist), NightStrength 0.5.
  - GOD RAYS: 32 taps over 40% of the way to the sun (was 24 over all of it), sharper falloff.
  - AO THROUGH PLATFORMS (owner: "the AO of the vines across the basin are visible through the platforms"): the
    exclusion mask's depth tolerance (0.0008 of the far plane) was 3 m at Payo's 3700 m, so the new vine curtains up to 3 m
    behind a platform cut their shape out of its AO. `WorldOutline.cs`: past `FineDepthFar` it is capped at
    `MaskToleranceMetres` 0.2. NOT yet confirmed by the owner in play.
  - FLOATING VILLAGERS (owner: "floating models out here"): `PayoSceneBuilder.Life.cs` `Actors`: a figure with no land
    within 3 m keeps its height only on something solid, else is set on flat land within 60 m below, else left out
    (build: 26 brought down, 12 left out, 48 of 60).
  - MOON LIGHT at 22 degrees (`MoonElevation`, was 30) so the sky's moon glow sits on the cute moon.
  - Probes: `Editor/MapKit/PayoStaleProbe.cs` (`Temp/stale.request`: counts, `clean`, `figures`, `buried`);
    `PayoPaddyShots` `shots eyeLABEL x y z lx ly lz` (one viewpoint; its FineDepthView variants did not change the
    picture, unexplained).
  - MAYARI GODDESS MODEL: with the "Eight heroes ultimate cutscenes rework" session (KNL1 contract, sheet
    `Logs/payo/unity/moon/mayari_sheet_v1.png` to the owner before it bakes over `goddess.bytes`).

## OPEN AT THE THIRD COMPACT (2026-10-18 night): items 1 to 3 DONE (above), 4 with the agent

1. SUN (owner: "sun is off center", screenshot looking down the valley north from the court rim): `SunYaw` in
   `PayoSceneBuilder.cs` line ~69 is ALREADY CHANGED 150 -> 180 (due north, the valley's open end) but NOT rebuilt or
   checked. Do: `build`, then Play shots looking north (`measure X_shotsonly`, view n; or `Temp/paddy.request`
   `shots X` rim views after a `refresh`), confirm the sun disc sits in the valley's mouth; check shadows on the court
   still read. `MoonYaw` 193 untouched.
2. HAZE TOO HIGH (owner: "the fog/haze reaches too high. it should only be within the valley"): NOT applied yet. Plan:
   in `Shaders/WorldOutline.shader` pass 0 `frag` replace
   `float low = exp(-max(0, world.y - _MapHazeBase.x) / _MapHaze.w);` with a hard ceiling
   `float low = 1 - smoothstep(_MapHazeBase.x, _MapHazeBase.x + _MapHaze.w, world.y);` (and its comment on `_MapHaze`),
   and in `Runtime/Map/PayoHaze.cs` set `Base = -15f, Thins = 55f` (whole under -15 m, none 55 m above). Tune by Play
   shots looking north from the rim (the owner's view); the sky is not hazed by it (only Unity fog / the sky itself).
3. PAETE CATCH MARKERS "EVERYWHERE" (owner screenshot from a high camera: several green four-leaf markers over the
   stage). `Runtime/Visual/PaeteCatchMarker.cs` draws ONE marker for the local non-bot Paete. Unproven theory: it is
   installed per Play by `[RuntimeInitializeOnLoadMethod]` as a `HideAndDontSave` + DontDestroyOnLoad object, and
   DontSave objects survive leaving Play, so the ~25 automated Play runs this session may have stacked copies. VERIFY
   first (count `Resources.FindObjectsOfTypeAll<PaeteCatchMarker>()` in Play, e.g. a line in `PayoPerfProbe`), and
   check whether the leaves are something else (Kanlit's vine leaves/flowers, `PaeteLeafBurst`). Paete's kit is LOCKED
   (look and rules): only a minimal bug fix (e.g. destroy stale markers on install), no restyle. Ask the owner if
   unsure.
4. BARREN PLAZAS (owner: "need you to fix up the rest of the barren stuff here", two screenshots of the paved ring
   courtyards around the stage: big empty grey paving, plain walls, a few tiny props). Plan: dress them with EXISTING
   Payo models (plants, planters, jars, baskets, benches, drying racks, banners, bulul figures, offering tables, vines
   on walls, grass and moss in paving joints) along walls and corners, keeping the open run lanes free for slipper
   retrieval (no colliders in the middle). Can go to a background agent in a new partial file
   (`PayoSceneBuilder.Plaza.cs`) with one hook line; judge from Play shots (the editor stills do not run AO/ink/haze).
   "Map art: pieces first" rule: new pieces built and reviewed alone first if any are needed.

Shot tools: `measure LABEL_shotsonly` (court eye n/s/e/w + lawn lip/mid, haze on/off and grass on/off pairs) and the
paddies agent's `Temp/paddy.request` `shots LABEL` (rim and over-valley views, day and night; it does NOT refresh
assets: send `refresh` to `Temp/payo.request` first after editing a material on disk).

## NEXT TASKS (owner, 2026-10-18, in his words, then the plan) - all six below are DONE (see the sections above)

1. PERFORMANCE: "im getting huge fps drops when looking at the main city". The town (the terraced houses, walls and
   their dressing up the slope) is the suspect. Measure first: `measure LABEL` in Play (`PayoPerfProbe`) looking at the
   town vs away; count draws, set-passes, triangles, shadow casters; check whether the town houses/props are LOD'd and
   culled, whether the fine-depth AO change added a depth pass, and the stone grain's cost. Fix what the numbers say
   (LODs and far-copies for the town like the plants and land got, instancing, shadow casting off at range). Re-measure.
2. HAZE: "should add a haze". An aerial-perspective haze over the valley (the fog/look row in `WorldLookProfile` Payo
   and the sky). Keep the sunset look the owner approved ("sky looks okay"); add distance haze, judge at the court.
3. GRASS: "grass blades are barely visible" (`PayoBlades`, `docs/PAYO_BLADES.md`): make them read (density, height,
   colour contrast against the land paint, distance) without losing the calm look; re-measure the cost.
4. WATERED PADDIES: "add patches reflective water puddles across the terraces as to show some watered rice fields".
   Some paddy terraces flooded with reflective water (sky and sunset reflected, rice sprouts poking through), in
   patches, not everywhere; the land export already has a `water` and `py_land_flooded` surface: check what exists
   before building new.
5. PADS: "speedpad jump pad and geyser need a rework. you can take the ones we have from arena and just remodel them to
   be more meso instead of futuristic". Start from the Arena's `arena_speed_pad.glb`, `arena_jump_pad.glb` (and its
   pickup) in `Assets/TumbangPreso/Art/Arena/Props/`, keep their footprints, colliders and gameplay, remodel in the
   painted kit as Mesoamerican-flavoured / Ifugao temple stone (carved stone, the stage's paint), review in Blender
   first, then swap Payo's `pad_speed`, `pad_geyser` (jump pad) art.
6. GEYSER VFX: "geyser effects you can ask this session for vfx" = the "Eight heroes ultimate cutscenes rework"
   session (VFX coordinator, also in the kanto worktree). Ask it (SendMessage / ListAgents) for the geyser burst effect,
   with the pad's size, the jump's timing and the GPU-driven rule; do not build the VFX here.

## Open owner calls and the not-yet-reworked list (shown to him 2026-10-18)

- Pit mist: the art's pit floor (y -30) shows just under the catch line (-22) while falling; bring back a misty layer?
- Stage blocks: pale top rims grid, lizard panel colour, lighter small wooden bulul, optional weathered tower house;
  Unity keeps the layout's block yaws (a few face differently from the Blender render).
- Hamlets along the river (not given the town's denser/bigger treatment); far ridges/clouds beyond 2.8 km dropped;
  night (Mayari) not re-checked on the new map (4 stale glow names, 40 rebuilt lights unseen); 60 life figures only
  re-seated; sound placements unchecked; water less glossy than Blender (no reflection probe); river glb exported face
  down (the builder flips it; fix `payo_export_unity.py`); head fall's east shoulder plain; small grey stones on the
  earth walls may read as coins; bots not watched on the new art; near-court plants still sway on the CPU.
- Not checked in Play: the sacred pool and gorge, the inlet weir behind the gate.

## STAMINA ORB (2026-10-19, owner: "stamina orb looks too plain and flat")

- Cause: the Pickups loop in `PayoSceneBuilder.cs` hung only `pickup_plinth` (named "Plinth"), so `ArenaStaminaPickup`
  found no `Model` child and drew its grey-box: an unlit `Sprites/Default` violet sphere.
- New piece `Art/Payo/Models/pickup_orb.glb` (`tools/payo_pieces/orb_build.py`, `orb_review.py`, `orb_run.sh`:
  `build`, `review vN`, `install`). It is a carved spirit bead: a plump eight-sided amethyst crystal with small gold
  caps and finials, a gold band set with four violet gems, and a pale spiral of light wound round it that seems to flow
  while the cell turns. The halo is a woven rattan-gold ring with eight jade leaves fanning out and violet gems between
  them. Root nodes are `pickup_cell` (0.38 x 0.56 m) and `pickup_halo` (0.70 m across), each with its pivot at its centre.
  7,200 triangles in all, one 1024 atlas, normal map and tangents through `calm_glb.swap`. Blender sheets are
  `Logs/payo/pieces/orb/orb_v1.png` (caps too big, too pink) and `orb_v2.png` (current).
- Materials: two specs were added BY HAND to `materials_payo.json`. `pickup_orb` is painted. `pickup_orb_glow` is
  kind glow with a new optional `day_glow` 1.5: `BuildMaterial` now reads `day_glow` (default 0.1, so nothing else
  changes), and the night still takes it to 1.7. `tools/payo_export_unity.py` does not write these specs: keep them on
  a re-export. `Art.Orb` defaults to "pickup_orb", and `pads.pickupOrb` in payo_art.json can override it.
- Hook: one line in the Pickups loop hangs the orb as `ArenaStaminaPickup.ModelName`. The Plinth line is kept.
- Shots: `Temp/paddy.request` `shots orb` (any label containing "orb") shoots the nearest live pickup at 1.8, 6 and
  18 m by day, close and 6 m by night, and a strip of six close frames, into `Logs/payo/unity/orb/`.
- NOT YET DONE: `build` and the Play shots. The editor was idle at hand-off: its log was silent from 01:28, and the
  other worker's `measure plaza` request from 01:15 was still waiting. Compilation of the C# edits is unconfirmed.

## MAYARI MOON EASTER EGG (2026-10-19)

Owner: "the mayari could use a rework ... instead of her showing at all times, do an easter egg similar to the balloon
slipper in Arena where we have a cartoony moon with a cute face, hit it enough times and mayari appears in the sky,
looking like moving stars, + a meteor/falling star shower". Then, in game: the round moon "does not look like a moon at
all"; "i really like a yellow color for the moon but maybe a crescent design"; v9 "the moon eyes open looks so creepy";
v10 "larger eyes"; v11 "looks good" (APPROVED AND LOCKED: backup `Logs/payo/pieces/moon/approved_v11/`, README there);
of the first reward (a constellation of star beads): "model an actual godess like the paete ult. then ghost effect her,
place her in the sky, and make stars glow across her sillhouete". The goddess MODEL then passed to another session (the
Paete-cutscene one); this session keeps the moon and the reward plumbing.

WHAT IT IS. At night (second half) a cute crescent moon, BUWAN, rises over the valley's open end (bearing 13, 22 degrees
up, 2.9 km out, radius 320 m): warm butter yellow, face in profile on the inner curve, a closed content eye, blush, a
nose bump, a striped nightcap with a gold star. No face has a big open eye; worried and wow use a round bead eye. The art's always-shown Mayari cut-out is switched off by the builder
(kept in the scene, unused). Hit it with full-charge plain-slipper throws (the balloon's host rule): it is knocked back,
squashes, wobbles, the cap flops, star beads and gold stars burst off its cheek, faces: ouch (>), worried from the third
hit (bead eye, brow, sweat drop), dizzy at the fourth (swirl, orbiting stars). The fifth: dizzy 1.4 s, a ring of light, then MAYARI as
a ghost in the sky holding the moon in her cupped hands (forms outward from the moon over 3.2 s, see-through, glowing
rim, hem dissolving, stars drifting and twinkling across her and along her outline), with a falling-star shower round
her for 21 s. She stays 22 s, dissolves from the hem up over 4.5 s; the moon sleeps (closed lid, zzz) 40 s and cannot
be hit meanwhile (throws pass as ordinary throws); then it wakes fresh. A new match resets it.

FILES (all new unless marked):
- `Runtime/Map/PayoMoon.cs` (rule, wire, reactions, reward clock, placement), `Runtime/Map/PayoMoonMeshes.cs` (KNL1
  loader), `Runtime/Net/MatchRpc.PayoMoon.cs` (the `PayoMoon` named message, the balloon's layout).
- `Resources/Shaders/PayoMoon.shader` (the moon: faces, cap, zzz, dizzy stars in the vertex program; painted bands, pixel
  ink line), `PayoMoonSky.shader` (shower, comet, burst, glow, ring), `PayoMayariGhost.shader` (the goddess ghost).
- `Resources/Map/PayoMoon/`: `moon.bytes`, `moon_atlas.png`, `meteors.bytes`, `comet.bytes`, `burst.bytes`,
  `glow.bytes`, `goddess.bytes`.
- `Editor/MapKit/PayoSceneBuilder.Moon.cs` (hides the old Mayari, adds `PayoMoon`), `Editor/MapKit/PayoMoonShots.cs`
  (`Temp/moon.request`: `film LABEL`, `measure LABEL`).
- `tools/payo_pieces/moon_cute.py` (the moon, shower, comet, burst, glow; `stage` bakes to `Logs/payo/pieces/moon/bake`
  for review, `unity` installs; Blender `-- render <ABS_ROOT> [stage]`; `sheet vN`), `tools/payo_pieces/moon_strips.py`
  (frame strips), `tools/payo_pieces/mayari_goddess.py` (this session's goddess model, now the other session's to keep
  or replace; contract in `PayoMayariGhost.shader`'s header and `PayoMoon.GoddessHands/GoddessScale`).
- Existing files, one line each: `Runtime/Carrier.cs` (after the balloon's offer:
  `Map.PayoMoon.HostOfferThrow(thrown, _motor, origin, aimPoint, charge, affinity);`), `Runtime/Net/MatchRpc.cs`
  (`cm.RegisterNamedMessageHandler("PayoMoon", OnPayoMoonMsg);` and `SendPayoMoonSnapshot((ulong)peerId);`),
  `Editor/MapKit/PayoSceneBuilder.cs` (after `Night(...)`: `night += "; " + Moon(root);`). `PayoNight.cs` untouched.
  `docs/SKILL_NETWORK_CONTRACT.md`: section "Payo moon easter egg" (no protocol step: an older peer drops the unknown
  message and only misses the reaction).

HOW TO TRIGGER. In a single-player Payo match, reach the second half (F9 ends a round), throw a full-charge plain slipper
at the moon five times. For a film: `film LABEL` in `Temp/moon.request` (DevHit, no slipper), then
`python -I tools/payo_pieces/moon_strips.py LABEL`.

EVIDENCE (`Logs/payo/unity/moon/`): before/after from the court `before_round_court.png` (refused round moon) and
`after_crescent_court.png` (v11), `before_round_tele.png`; approved sheet `moon_crescent_v11_review.png` (history in
`Logs/payo/pieces/moon/moon_v2/v6/v10/v11.png`); goddess sheets `goddess_v2.png`, `goddess_v3.png`; Play strips `m6_*`
(v11: idle, hits, her forming in game and wide views, 0.25 s motion strip, leave) and `m5_*` (the reward, earlier moon),
frames in `frames/`. COST (`measure c1`, same Play A/B, 150-frame blocks): with the
moon, Mayari and the shower all in view +0.52 ms GPU (7.64 vs 7.12), the moon alone asleep +0.28 ms; its own CPU
0.007 to 0.009 ms a frame. Triangles: moon 9.7k (one face drawn), goddess 12.7k, shower 24.2k (only during the reward).

UNFINISHED. (1) Moon v11 is installed, built and filmed (`m6_*`); its look is locked. (2) The goddess model is being
built by the "Eight heroes ultimate cutscenes rework" session to the KNL1 contract; leave `goddess.bytes` alone until it
bakes over it (the current one is this session's stand-in), then `film` again. (3) Sounds are borrowed placeholders (balloon squeaks and boing, PA chime). (4) The aim rule is tested
only by DevHit, not by real throws in Play; actual peers not tested. (5) The sky's own moon-light glow sits about 8
degrees above the moon (the night key light is at 30 degrees; night look left untouched).
