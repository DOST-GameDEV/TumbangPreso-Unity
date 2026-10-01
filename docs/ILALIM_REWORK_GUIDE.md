# Ilalim ng Tulay rework guide (ILALIM-1)

⚠️⚠️ **CURRENT STATE (2026-09-29, end of session): EVERY KIT BUILT IN BLENDER AND ASSEMBLED.
Awaiting owner decisions. Nothing is in Unity. All work is committed LOCALLY on branch
`claude/kanto-blender-assembly-909442` and NOT pushed (the owner plays before a push).**

- **The scene:** `blender -b --python tools/author_ilalim_city.py -- --preview N` links every kit
  into `ArtSource/ilalim/ilalim_city.blend` and renders `Logs/ilalim-blender/city_<shot>_vN.png`
  (latest v1; the Rizal shots are v2 plus `city_centre_tele_v1`).
- **The kits:** each has its own `tools/author_ilalim_<kit>.py`, a
  `tools/author_ilalim_textures_<kit>.py` and a `.blend`. They are:
  - `lrt` (the guideway, v11);
  - `rizal_hall`;
  - `heritage`;
  - `eastside` with `signs`;
  - `street`;
  - `trees`;
  - `props`;
  - `sarisari` (the corner store);
  - `train` and `vehicles`.

  The table and the open decisions are further down, under "The rest of the map, built in
  parallel". The layout is the real OpenStreetMap one (`osm_layout.json`), except for
  `sightline_override()` in `tools/author_ilalim_blockout.py`, which every kit imports.
- **Owner feedback applied this session:**
  - the guideway: its real underside, pale sand columns, drawn grime, ballast, gantries, and
    steel that reads;
  - Rizal Hall moved 26 m east, with the Supreme Court thinned (not removed);
  - the trees now reuse the Kanto leaf and the Lagoon round leaf, broad frond and blade
    ("can you use the leaf textures we made previously?").
- **The sari-sari store (owner: "can you put a sari sari store somewhere"), BUILT, awaiting
  review.** `py -3 tools/author_ilalim_textures_sarisari.py`, then
  `blender -b --python tools/author_ilalim_sarisari.py -- --preview N` writes
  `ArtSource/ilalim/sarisari.blend` and close-ups `Logs/ilalim-blender/sari_<shot>_vN.png`
  (latest v3). The city links it (`city_*sarisari*_v1.png`).
  - **Where:** the free north-east corner lot of Taft and Padre Faura, checked against every
    kit: the house stands at x 12.3..19.3, y 38.9..45.9, on the lot surface (0.24), clear of the
    Taft signal pole, the street pole and the fig tree.
  - **What:** Bebang's Sari-Sari Store is a two-storey corner house. The ground floor is rose
    plaster, the upper floor cream lap siding, and the roof galvanised iron. The front room has:
    - a grilled counter window with a plank ledge, candy jars, sachet strips and stocked
      shelves;
    - the BAWAL ANG UTANG / "bukas pwede :)" card;
    - a green and cream awning with snack strips;
    - the painted signboard.

    Outside there are a bench, a red cooler with a MAY YELO card, crates of empty bottles, a tin
    BIGAS / ASUKAL / MANTIKA sign, the electric meter with its service drop, a window aircon,
    and a maroon NO PARKING side gate.
  - **Sightline (ray-sampled):** fully in view from the taya spot, the north-east court, the
    west pavement and the hoop. From the spawn the street kit's BARANGAY 712 board (at
    (10.25, 20.6)) hides about half of the signboard; moving that board is an owner call.
  - The props kit's small sidewalk STAND at (-10.35, -14.2) stays.
- **Anti-tiling on the flat roofs, and the viewport z-fighting** (owner: "dont we have something
  that we used in lagoon and kanto to have random tile rotation and feather offsets", "not only
  ground but the flat roofs", "z fighting on ground plane"):
  - `tools/ilalim_antitile.py` is Kanto's and the Lagoon's method as one shared node chain: two
    extra rotated, scaled and offset samples, each blended in through a big feathered noise
    mask. The street kit's lot, asphalt, parking and lawn already had the one-sample version.
    `east_roof` and `heritage_flat_roof` now use the shared chain, and `east_roof` is redrawn as
    broad fields (its small blotches repeated as wallpaper).
  - The grid and flicker on the ground were the VIEWPORT, not the geometry. The road top is
    z = 0 (the contract), where Blender draws its floor grid, and a 0.01 m near clip left the
    depth buffer about 6 cm of precision from the air. The city file now opens with the floor
    grid off and a 0.1 m near clip. Renders were always clean.
  - Unity (ILALIM-1.4) needs the same anti-tiling in the map shader.
- **The liveliness pass** (owner: "can you think of a way to make the place look more lively,
  more unique building shapes etc?", then "proceed"). It was built by four parallel agents, and
  the city links the three new kits when their files exist (`OPTIONAL_KITS`):
  - **East side facades** (`author_ilalim_eastside.py` and its texture script): each background
    building gets its own weathered pastel paint (9 paints, never repeated within 45 m) and
    facade traits on its street faces. The traits are bays, balconies with pots and laundry,
    four grille styles, painted roll-up shutters, awnings, and eight invented hand-painted blade
    signs. Footprints and roof slabs are unchanged. Detail falls off with distance from the
    Taft and Padre Faura corner.
  - **Rooftops** (`author_ilalim_rooftops.py`, `rooftops.blend`): 150 items on 72 flat roofs.
    They are water tanks (black, green, cream, galvanised, never blue), shacks, penthouses,
    laundry lines, dishes, gardens, three invented billboards and two cell masts. Placement is
    seeded per building and ray-checked. It is about 323k triangles, so it needs LODs or thinning
    for Unity. Re-run it after any east or heritage rebuild.
  - **Street life** (`author_ilalim_streetlife.py`, `streetlife.blend`): 13 banderitas spans
    along Padre Faura (never over Taft), 9 bamboo fiesta poles, 29 parols, 20 parked vehicles
    (18 from `vehicles.blend` and 2 pedicabs), 5 tarps, 4 notices and potted plants. It
    ray-checks against every kit and prints `CHECK: clean`. Re-run it after any east rebuild.
  - **Landmarks** (`author_ilalim_landmarks.py`, `landmarks.blend`, `landmarks.json`):
    - TANAW RESIDENCES (by the invented developer DALISAY LAND), a tower under construction with
      green netting and a yellow tower crane, behind the sari-sari store. It is seen from nearly
      every court eye point.
    - EDIFICIO AMIHAN, an art deco corner block, and the MAKABAYAN BUILDING, 1960s with a
      brise-soleil grid. Both are at the G. Apacible junction, about 115 to 140 m south, and read
      only as silhouettes from the court.

    The buildings they replace are listed in `landmarks.json` under `hide`. The east kit drops
    them after its build (`drop_replaced()`). A linked object's visibility cannot be saved in the
    city file, so hiding them there is not enough.
- **Ending the view along Taft** (owner: "lrt way ending is visible from the play area", then chose
  "Stations + haze"):
  - `author_ilalim_stations.py` (`stations.blend`) builds UN AVENUE (y 94 to 185) and PEDRO GIL
    (y -94 to -185), one design placed twice. They sit much closer than the real stations: a
    deliberate sightline departure, like Rizal Hall's.
    - The guideway meets each station through its own 6 cm expansion gap.
    - Each station has a closed concourse, stairs down to both pavements and a green barrel roof.
    - Behind each, a row of six buildings (fronts at |y| about 220) closes the street-level view
      where the ground ends.
  - `author_ilalim_city.py` adds a compositor haze, as Kanto does: fade toward a sky-close colour
    by the mist pass, from 45 m, capped at 0.55. A world volume blacked out the sun in EEVEE. Unity
    needs matching linear fog.
- **Street furniture validated** (owner: "validate all the street posts/lamps across the visible
  area from the play field and fix these canopy + pole clipping issues"). Every pole, lamp and sign
  post within about 70 m of the court was checked by mesh overlap against every kit. The fixes:
  - The east Taft poles and lamps stand 1.2 m off the shopfronts, and the shop-row awnings are cut
    round them.
  - The Padre Faura poles search for a spot 0.75 m clear of every footprint.
  - Trees keep their trunks off poles, lamps, signals, sign posts, shelters, the bamboo fiesta
    poles and the stations.
  - The west bench and the median lilies clear their posts.

  Only leaves brushing poles remain, which is accepted. The check script is `pole_clash.py` in the
  session scratchpad; re-run something like it after any furniture move.
- **Build order:** street, then trees, then eastside and props, then rooftops and streetlife (these
  read the east roofs and walls), then the city.
- **OPEN owner decisions** (details under "Open decisions for the owner" below):
  - the train is invisible behind the solid parapet; a steel-railing parapet is recommended;
  - cable shadows on the court;
  - how big Rizal Hall reads from the court;
  - names.
- **ILALIM-1.4, IN UNITY (2026-09-30, owner: "put it in game").** Unregistered sample scene
  `Scenes/Samples/IlalimRebuild.unity`:
  - Export: `blender -b ArtSource/ilalim/ilalim_city.blend --python tools/export_ilalim_unity.py`
    (about 2 minutes, never saves a .blend) writes `Art/IlalimRebuild/` (Models, Textures,
    `ilalim_layout.json`). Its docstring records every decision.
  - Build: `tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile <name> -executeMethod
    TumbangPreso.EditorTools.MapKit.IlalimSceneBuilder.RunReview` (build, frame proof, geometry
    check, renders into `Logs/ilalim-unity/vN`).
  - ⚠️ The frame: the kits are modelled in the game's frame, so Blender (x, y, z) is Unity
    (x, z, y), NOT Kanto's (-x, z, -y). The frame proof reads the shipped scene.
  - Grime stays positional: UVGrime, UVSplash, UVSill travel as TEXCOORD 1, 2, 3 and
    `Shaders/IlalimPainted.shader` multiplies them. The piers alone are baked (Cycles) so they can
    wear `TumbangPreso/NearFade`. Leaves use `LagoonFoliage`.
  - About 6.43 million placed triangles (trees 2.78 million). Nothing decimated; trees, lilies,
    rooftop items, street life and parked traffic get cull-only LODGroups.
  - **Own look (owner: "change the lighting setting so its less like the lagoon map").** A new
    `IlalimRebuild` WorldLookProfile row (code defaults and `Resources/WorldLookProfile.asset`;
    the shipped IlalimNgTulay row is untouched, the old alias is only a fallback). Blender's sun
    (27 degrees from the west-south-west, 1, .93, .84), shade a greyed lavender at the other rows'
    level, a smoggy grey sky and fog (zenith .58, .67, .76; horizon .85, .84, .80; fog .82, .82,
    .81) from 45 to 405 m, the Blender haze's slope. The builder authors the scene to match the
    row. Renders `Logs/ilalim-unity/v8` (v3 is the Lagoon-like before).
  - **Live traffic, sound and pigeons (owner: "make the live moving cars and pigeons + the sfx").**
    `Editor/MapKit/IlalimLifeAuthor.cs`, called by the builder. `KantoTraffic` gained a ROUTES
    mode (Kanto's grid is unchanged): the court is a closed stretch of Taft, so Padre Faura runs
    one-way west with a right turn up Taft, Taft's southbound traffic turns into Padre Faura, the
    south turns into G. Apacible, and two cars queue at the south closure. 21 vehicles (the 8
    placed Traffic ones plus 13 copies); parked cars stay parked. `KantoStreetSound` with Kanto's
    clips supplies the bed, engines, horns and sirens. Kanto's pigeon flock (`LagoonFlocks`) lands
    on perch lines measured against the meshes (guideway copings, roofs, station roofs, pavement
    edges). `RunReview` now also writes `life_probe.txt`: 90 s stepped without PlayMode, no
    overlaps, nothing in the court, every pigeon on a perch. No pigeon sounds exist in the repo.
  - **Sidewalk life (owner: "kids chasing each other around, some taho vendor, people stopping by
    to watch, maybe some beggar that comes sits down that you can interact with to donate to").**
    `Editor/MapKit/IlalimSidewalkAuthor.cs` (called by IlalimLifeAuthor) authors
    `Runtime/Map/SidewalkLife.cs`. The people are Classic rigs from the RosterBook with their own
    everyday palettes (garment slots only, measured per rig; skin, hair and the face slot kept;
    nothing near the role hues), animated by the rigs' own clips plus LagoonResident's arm cheer,
    a bow, a head shake and a laughing hop. All scenery: no collider, no CharacterMotor, nothing
    networked, never in the play area. Three kids play tag on the south-east pavement for about
    a minute, now and then; the magtataho (yoke and two aluminium buckets) walks the south-west
    pavement, stopping to call "TAHOOO!" as a small comic popup (no taho clip exists); three
    passers-by walk to the north corners and the PGH lot fence, watch the court, cheer a can
    going down and groan at a tag; a beggar sits on a carton against the PGH fence just past the
    south wall. Within 2.6 m (a player at the wall is 2 m away) the HUD offers "Give a coin" on
    Interact (`StreetInteractions`, read last by TumpMatchReadout and Hud; TouchHud shows
    INTERACT while it stands): a coin arcs into his cup, the pisonet's `ui_click` blip, a bow
    and wave, a thank-you popup. Cosmetic only. Every route is measured against the meshes at
    build (the "Sidewalk" build-log lines); `RunReview` also writes `sidewalk_probe.txt` (300 s
    beside the traffic) and `sidewalk_*.png`. Batch alone:
    `IlalimSidewalkAuthor.RunBuildAndProbe`.
  - **2026-09-30, the beggar's own model and the shoulder carry.** Owner: *"use a different model
    to make him look more like a beggar, and better textured with dirt and stuff whil still
    maintainiing artstyle"* and *"taho pole use option B on shoulder"*. The beggar is no longer a
    cast rig: `tools/build_beggar_voxel.py` (the voxel person pipeline, `build_person_voxel.py`'s
    machinery imported, on character-male-e's own skeleton so every clip still plays) writes
    `Art/IlalimRebuild/Life/npc-beggar.glb`, its own atlas `npc-beggar-atlas.png` and
    `npc-beggar-palette.json`; the author wraps them in `npc-beggar.asset` (a RosterEntryAsset
    outside the roster). A thin older man: greying messy hair receding at the front, a short grey
    beard, a sun-faded torn shirt, patched rolled trousers, dusty bare feet on worn tsinelas (one
    strap broken). His clothes and skin are PAINTED: `Toon.shader` samples the texture itself in
    the atlas's non-palette half, so garment boxes are projected onto hand-drawn swatches (flat
    fills, big feathered patches: sun-fade, sweat, grime at the hems, a torn hole, a frayed
    sleeve, sewn patches, dust on the seat, cuffs and feet). A tied cloth bundle and a plastic bag
    sit beside him. `BeggarOption.D_OwnModel` is the default; A (Mang Kanor's rig), B and C stay
    selectable. The magtataho's default is `TahoCarryStyle.Shoulder`: the pole rides on the
    torso bone at 0.765 m, 2 cm under the head's rim and over the hanging arms, with no hand on it
    (a raised hand put the pole through the 0.38 m thick sleeve). Films and stills:
    `IlalimSidewalkFilm.RunBuildProbeStills` then `RunVideos` (PlayMode-free, the life, traffic
    and pigeons stepped at 30 Hz, the game's own TAHOOO and SALAMAT popups), encoded by
    `tools/encode_ilalim_films.py` into `Logs/ilalim-unity/videos_v1/`.
  - **2026-10-01, the sidewalk people's animation and sound.** Owner: *"you should probably fix
    the animations for the walking and sitting of these characters"*, *"can you add a "Tahoooooo"
    voice sfx for the taho guy?"*, *"as much as possible all these liveliness-adding character
    need sounds"*. `SidewalkLife` (class note) now DRAWS the walk and run bone by bone like the
    cast's `CharacterAnimator.LocomotionArms`: each body's `GaitStyle` (kids and passers-by their
    rig's own entry; the magtataho and the beggar their own `TahoGait`, `BeggarGait`), the cadence
    from the measured ground speed over the no-slide stride, the cast's foot plant, damped turns,
    rounded corners, stepping while turning on the spot, the kids braking through a juke, the
    buckets swinging in step. The beggar no longer plays the `sit` clip (a chair sit: legs sunk,
    arms out at 45 degrees): a drawn floor sit on the carton (legs level and a little apart, hips
    at the height that rests the thighs on the carton, a slight lean back, head bowed, the left
    hand on the pavement by the cup, the right forearm over his lap), breathing, nods, looking up
    at passers-by; one continuous sit-down and stand-up; the bow and a wave from the seated pose;
    "SALAMAT PO!" everywhere. Offline check of the seated pose on the real mesh:
    `Logs/ilalim-unity/pose_offline/seated.png`. SOUND: `tools/synth_ilalim_life_sfx.py`
    (numpy/scipy formant and noise synthesis, fixed seeds) writes 36 clips, 3 variants each,
    into `Art/audio/ambience/` (`sfx_taho_call_1..3`, `sfx_life_{kid_giggle, kid_taya, cheer,
    clap, groan, salamat, coin_tin, carton, bucket, pigeon_coo, pigeon_flap}_1..3`), with
    spectrograms in `Logs/ilalim-unity/life_sfx/` and the call's pitch and formant analysis in
    `Logs/ilalim-unity/taho_call/`. Footsteps reuse `step_rubber`. They play on the life's own
    pool of 3D voices, like `KantoStreetSound` (SFX slider, replay duck, log rolloff, priority
    200, cooldowns); pigeons read `LagoonFlocks.BirdSettled`/`BirdTakingOff` (new, read only).
    The probe now reports planted-foot slip and the seated clearance; the films log every sound
    and camera, and `tools/encode_ilalim_films.py` mixes and muxes the soundtracks
    (`Logs/ilalim-unity/videos_v2/`, rendered later that day; superseded by the pass below).
  - **2026-10-01, second pass: arms, planted feet, the seat, the wave, the carton, the pop, the
    court's pavements.** Owner: *"the feet are moving but the hands arent"*, *"the cardboard is
    untextured"*, *"the guy looks like hes floating"*, *"the sit animation is too linear and too
    unlively not poppy enough"*, *"they also stop before getting to the middle of the sidewalk
    infront of the play area"*; the lead: the planted sole slid at body speed, and the thank-you
    wave showed no raised arm. Causes and fixes (`SidewalkLife` class note, `Locomote`, `DrawSeat`):
    * ARMS: the rigs' `idle` keys both arms, the chest and the head, and the PlayableGraph on the
      rig's Animator wrote that pose back over the drawn one (Play and films alike) while the legs,
      which idle does not key, kept theirs. Now there is no graph: each frame resets the seven bones
      to bind, SAMPLES the clip (`AnimationClip.SampleAnimation`, crossfades blended by hand; the
      Animator disabled) and draws on top. The arms swing opposite their own leg from the same
      phase, per role (kids x1.15 carried 14 degrees forward, passers-by at least 22 degrees, the
      beggar's own small swing, the magtataho's pole arm held within 9 degrees under the pole).
      ⚠️ Outside Play every film step renders in one editor frame, so the bodies' skinned meshes set
      `forceMatrixRecalculationPerRender` (edit mode only), or every frame shows the first pose.
    * FOOT SLIDE was REAL: with kneeless mirrored legs the LOWER sole (which the hips were dropped
      onto, and which the probe measured) is the one behind, which half of each step is the swing
      foot, dragged at twice the body's speed. Now the stance leg is chosen by the phase, its sole
      is locked to the world point where it landed while the body passes over it, the swing hip is
      hiked 4.5 degrees at passing so the swing sole clears, and the probe measures the stance sole.
      Probe (`videos_v3/probe/sidewalk_probe.txt`) planted sole along travel, before -> after:
      taho +0.80 -> +0.00 m/s (body 0.80), beggar +0.74 -> +0.00 (0.75), passers-by +1.13..1.15 ->
      +0.00..0.01 (1.15), kids +2.18..2.26 -> +0.04..0.08 (2.3..2.5; the rest is the run's flight
      and juke corrections). Left arm against left leg: -0.77 to -0.95 (opposite phase).
    * FLOATING: level legs join the chest at its middle and his heels (the foot block juts 5.7 cm
      under the thigh) held him up, so the seat hovered 14 cm over the carton. Now the legs rest 10
      degrees above level, set down by their real mesh corners (`Hull`), and the chest is lowered
      until its lowest corner is on the carton: probe seat +0.002 m, legs +0.000 m.
    * WAVE: the seated wave raises his right arm out to the side, fist 0.41 m over the shoulder
      and 0.33 m out beside the head, rocking at 2.6 a second, the head tilted 20 and the chest 10
      degrees away so the arm passes under the head's 0.3 m overhang (the probe's head-box check
      reads 0.086 m, a conservative box round the hair tufts; no visible cut in the stills).
    * POP: every gesture rides one curve (`Pop`: a wind-up the other way, a snap with an overshoot,
      a hold, an eased return with a settle); the sit-down winds up, drops faster into the carton,
      squashes on the cast's own squash spring (`CharacterSquashStretch`'s 24 / 8.5) and settles;
      the stand-up leans, pops up past standing and stretches; the head lags the chest a beat
      (`HeadLag`); cheer (arm pulled back then thrown up and out), a new clap after it, laugh hops
      that squash on landing, the juke. No linear ramps are left in the drawn layers.
    * CARTON AND PROPS: `tools/author_ilalim_textures_life.py` (the prop painter's helpers) paints
      `Art/IlalimRebuild/Life/life_{carton, cloth, bag, tin, aluminium, lid, bamboo, rope, coin}.png`
      (swatches `Logs/ilalim-unity/life_textures/`); the author puts them on the life materials. The
      carton has its own mesh (`CartonMesh`: a torn far corner, top face and corrugated edge UVs):
      kraft board, fold creases, a faded MARUPOK print and this-way-up arrows, water rings,
      pavement dirt, a darker worn seat patch. The tin cup is an old milk can (maroon GATAS label).
    * COURT PAVEMENTS: the magtataho now walks the whole west pavement (x -7.85) to z 12.5; a
      passer-by watches from the west pavement (-7.85, 5.5) and one from the east (7.55, 1.8,
      single file between the kerb and the pisonet cord and the overclock pad); the kids' tag runs
      up the east pavement to z -8. The route check and the probe no longer fail the play area:
      they fail the chalk box, the kerb (|x| under 7.3) and anything within 0.5 m of a gameplay
      prop (the layout's anchors: pisonet row and cord, pares cart, pad, hoop, stalls, crates,
      chairs, bench, bin, drum). Probe: 0 in the box, 0 on the kerb, 0 near a prop. The beggar's
      spot stays against the PGH fence (the court's pavements put him by a prop or a doorway).
    Films and stills: `Logs/ilalim-unity/videos_v3/` (`kids_tag`, `taho_calling`,
    `spectators_cheer`, `beggar_donation`, `court_wide`, with sound; `stills/wave/`,
    `stills/carton/`).
  - **2026-10-01, the known issues fixed (owner: "proceed with the fixes for the known issues").**
    * TIN CUP: its own mesh (`SidewalkLife.CupMesh`), each face in its own part of `life_tin.png`:
      the label wraps the side only; the top is the open can (a rolled rim, the dark inside, two
      coins); the bottom plain tin. The built-in Cylinder had put the whole label on the top.
    * RELAXED IDLE (`SidewalkLife.Relax`): anyone standing still no longer holds the rigs' own idle
      (both arms 45 degrees out). The arms hang at the sides a little forward and out, breathe and
      sway; every 3 to 7 s a weight shift onto one leg (hip roll, chest leaning back over it); now
      and then a passer-by folds the arms or sets the hands on the hips, a kid fidgets or bounces;
      all on the `Pop` curve, faded out by the walk and the seat, gestures drawn over it, with its
      own seeded randomness (the story is unchanged). Probe: standing arms hang 11.7 to 11.8
      degrees off straight down (magtataho, beggar), passers-by 22 to 27 on average with their touches.
    * SOLES: the seated legs are nearly level (4 degrees), 18 degrees apart and rolled 80 degrees
      onto their outer sides (knees out), so the soles angle away instead of facing the street,
      and the tsinelas are a duller worn grey-green (`npc-beggar-palette.json` slots 3 and 4,
      6a6d64 and 4d5048, matching `tools/build_beggar_voxel.py`). Rolled, the foot no longer juts
      under the thigh: probe thighs +0.003 m, legs +0.000 m, seat +0.002 m over the carton.
    * ARMS IN PLAY, CONFIRMED: `Tests/PlayMode/IlalimSidewalkPlayProbe` loads the sample in Play
      (`EditorSceneManager.LoadSceneAsyncInPlayMode`), lets the life run on its own `Update` 25 s
      and reads the limbs in a LateUpdate at execution order 32000, after the animation system
      (WaitForEndOfFrame never fires in batch). Result (`videos_v4/play_arms.txt`, passed): no rig
      Animator enabled; three passers-by walking, left arm against left leg -0.73 to -0.89, arm
      swing 35 to 48 degrees, planted sole +0.00 m/s. Run it with
      `py -3 tools/run_unity_guarded.py -batchmode -force-d3d11 -runTests -testPlatform PlayMode -testFilter "TumbangPreso.PlayTests.IlalimSidewalkPlayProbe" -testResults Logs/ilalim-unity/videos_v4/play_arms.xml -logFile Logs/ilalim-unity/play_v4.log`.
    Films, stills and probe: `Logs/ilalim-unity/videos_v4/` (`stills/carton/cup_above.png`, `soles_front.png`).
  - OPEN: the owner's look review in Play, cable shadows striping the court under the 27 degree
    sun (decision 2 below), then ILALIM-1.5 and 1.6.

---

**History of this guide's earlier states (kept for the reasoning; the block above is current).**
The owner chose
Ilalim ng Tulay as the next map after Kanto and the Lagoon Court, and set it at UP Manila's
Padre Faura corner "cuz we wanna see our school's Rizal Hall in the game". § 0 is the proposed
place and feel, with the open decisions in § 0.6. The evidence is in
[the research report](reports/ilalim-rework-2026-09-29/research.md), and the plan is in
[place-plan-v1.png](reports/ilalim-rework-2026-09-29/place-plan-v1.png).

The owner then asked "give me a blockout in blender". `tools/author_ilalim_blockout.py` writes
`ArtSource/ilalim/ilalim_blockout.blend`, and with `-- --preview N` it writes eight renders to
`Logs/ilalim-blender/blockout_<shot>_vN.png`. It takes § 0.6's DEFAULT answers:
- the campus on the west and PC Express on the east;
- Rizal Hall facing south, with the Oblation;
- twin-leg piers;
- late-afternoon sun.

Each default is a small edit if the owner picks the other answer.

⚠️ **v3 WAS REJECTED FOR ITS LAYOUT, NOT ITS MODELS.** The owner's words: "you should take a
look at street map to see how the place is actually laid out", then "the models are pretty
much accurate but the positioning, zoning and lack of sidewalks arent". Since v4, everything
outside the play area comes from OpenStreetMap:
- `tools/ilalim_osm_layout.js` converts an Overpass extract into
  `ArtSource/ilalim/osm_layout.json`, in game metres (ODbL, attribution in the file).
- Only the road and the east frontage band are squeezed into the 14 m box. Everything else
  keeps its true position.
- The ground is a 0.5 m grid, so every street gets real sidewalks with a kerb step.

What the true layout puts around the court:

| Where | What |
|---|---|
| West | PGH's fenced frontage (the fence lands exactly on the x = -11 wall), then the Nurses Home and the OPD |
| North across Padre Faura | The Supreme Court corner, with Lady Justice and Moses facing Taft |
| North-west, behind the Supreme Court | Rizal Hall: a quadrangle facing south across its lawn, with the Oblation |
| East | The Astral Tower and West East Center podium on the wall line, then KFC and Vista GL Taft south |
| North-east | Manila Science High School |

§ 0.3's picture of a "campus lawn" with Rizal Hall visible from the court is superseded by
this. ⚠️ **In the true layout Rizal Hall CANNOT be seen from the court.** It is about 115 m
away, behind PGH and the Supreme Court (render `court_to_rizal`).

**The sightline override (v9).** The owner's words: "we still wanna focus on sightlines, so
even though the model is now accurate, we need RH to be more visible". A first reading removed
the Supreme Court corner, and was corrected: "i only asked you to thin it down to make more room
to move RH". `sightline_override()` in the blockout script now does three things:
- It thins every Supreme Court building on the corner at x = -50, keeping its Taft side and the
  Moses and Lady Justice statues.
- It moves the whole Rizal Hall compound 26 m east into the freed space: the hall, the
  courtyard, the lawn, the fence, the wall, the Oblation and the Gat Andres Bonifacio block.
- It removes the one small PGH block in the court's view line.

It also clears trees within 4 m of the view lines from the spawn and from both pavements to
the portico. From the spawn, the portico and its lettering now read between PGH and the Supreme
Court (render `spawn_to_rizal`). `osm_layout.json` stays the true map.

The same review also rebuilt the ground: "fix the plane..". Each ground class is now one
dissolved surface, with kerb walls only where heights differ: about 3,300 faces in total,
instead of hundreds of thousands of boxes.

**The first kit: the LRT-1 guideway (v3).** The owner said "proceed. i want you to give me
models for the LRT way. we're still following that artistic stylized handdrawn design."
- `py -3 tools/author_ilalim_textures.py` paints four surfaces into `ArtSource/ilalim/textures/`,
  with a swatch sheet in `Logs/ilalim-blender/`. Each surface has its own drawing:
  - `lrt_concrete`: faint pour lines every 2 m;
  - `lrt_soffit`: soft damp patches and joints;
  - `lrt_track_bed`: brake-dust patches;
  - `lrt_steel`: neutral, tinted per material.
- `blender -b --python tools/author_ilalim_lrt.py -- --preview N` models the kit into
  `ArtSource/ilalim/lrt_kit.blend`. Every prototype sits at the origin in its own collection,
  and the same prototypes are assembled over the court as linked duplicates:
  - `lrt_pier`: twin tapered legs, a hammerhead cap, bearings, and a drain pipe with clamps;
  - `lrt_span_9/20/25`: a rounded box girder, the panelled parapet with coping, cable troughs,
    and slab track with rails;
  - `lrt_mast`: a catenary mast with its arm, brace and insulators, plus the sagging contact
    wires.
- The contract numbers hold: soffit 8.0, deck top 9.04, width 10.5, rail head 9.19, and legs
  1.4 m at x ±4.45. The piers carry their own material, `lrt_pier`, for NearFade.

Self-review faults fixed before the owner saw it:
- The first pier read as a Greek colonnade: flared bases, a cap overhanging like a cornice, and
  pour lines every metre reading as stone courses.
- Smooth shading streaked the big faces.
- Anti-tiling turned the pour lines diagonal. Directional textures are now left out of it, as
  on Kanto.

**Guideway v6: dirt, grime and the real track.** The owner said: "you should take a look at how
the lrt way actually looks. theres no dirt or grime on what you have". The references are in
research.md § 8. The changes:
- **Positional grime.** Two drawn multiplier overlays, `grime_drips` and `grime_splash`, go on
  through the UV maps `UVGrime` and `UVSplash`. Stain tongues hang from the coping and the cap,
  the pier feet carry a splash band, and a soot band creeps in from the deck edges underneath.
- **Ballasted track** with sleepers and rust-brown rails, in a trough in the girder. The rail head
  is still at 9.19.
- **Portal gantries** replace the single masts.

The self-review caught four more faults:
- the drips were airbrushed blur, then melting slime; they are now drawn shapes;
- the underside drips drew wood-grain stripes; the underside now takes the soot band instead;
- the ballast read as cobble paving, from its outlines and its bump;
- the textures were too strong.

⚠️ For ILALIM-1.4: Unity needs the two extra UV channels, or the grime baked into the albedo.

**Guideway v9: the real underside, and columns that stand apart.** The owner sent two street
views of Taft under LRT-1, with two notes: "rework the supporting columns texture in a way that
it doesnt look blended in to the main duct/railway", and "the underside also looks different
from what you currently have". The changes:
- **The span is now a deck slab carried by four precast girders,** with dark channels between
  them, a deep fascia beam along each edge under the parapet, and end crossbeams at the pier.
  It replaces the box girder. Soffit 8.0 and deck top 9.04 still hold.
- **The columns have their own texture,** `lrt_pier`: pale warm sand with broad vertical
  washes and no pour lines. The cap takes the girders' grey, `lrt_pier_cap`, but stays in the
  pier object, so it is on NearFade too.
- **The underside is darker:** `lrt_soffit` and `lrt_girder`.

Rain tongues now go only on the fascia, parapet, cap and columns. The soot band goes only on the
slab's underside. Mapped onto the girders themselves, both drew blocky patches.

The Taft median also has a green-painted planter wall with lilies around the columns. It is a
candidate for the prop kit.

**The rest of the map, built in parallel (2026-09-29).** The owner asked: "lets proceed with the
rest of the map. spin up parallel agents to develop different aspects of the map in parallel,
same workflow that was done with the lagooncove map". Seven agents each wrote their own
scripts, textures (with their own prefix) and .blend, then rendered and self-reviewed their
kit. The lead reviewed each and assembled them.

| Kit | Script | .blend |
|---|---|---|
| Rizal Hall and the Oblation | `author_ilalim_rizal_hall.py` | `rizal_hall.blend` |
| heritage and government buildings west of Taft | `author_ilalim_heritage.py` | `heritage.blend` |
| east shop row and district, hand-named signs | `author_ilalim_eastside.py`, `author_ilalim_signs.py` | `eastside.blend` |
| ground, kerbs, markings, median planter, furniture, cables, fences | `author_ilalim_street.py` | `street.blend` |
| trees, shrubs, median lilies (Kanto leaf-card method) | `author_ilalim_trees.py` | `trees.blend` |
| gameplay props (pisonet and cord hazard, pares cart, pad, hoop, stalls, column signs) | `author_ilalim_props.py` | `props.blend` |
| the corner sari-sari store (added after the parallel kits) | `author_ilalim_sarisari.py` | `sarisari.blend` |
| LRT-1 train and road vehicles | `author_ilalim_train.py`, `author_ilalim_vehicles.py` | `train.blend`, `vehicles.blend` |

Each kit has a matching `author_ilalim_textures_<kit>.py`. Every script's docstring records its
contents, positions and owner-facing decisions.

`tools/author_ilalim_city.py` LINKS every kit's placed collections into `ilalim_city.blend`, so
each kit stays editable in its own file. It also adds:
- the median lilies, placed on the street kit's placeholders;
- the train on the deck;
- traffic in the lanes the street kit left clear of the pier collars;
- a late-afternoon sun.

It renders `city_<shot>_vN.png`.

⚠️ **Open decisions for the owner:**
1. **The train cannot be seen from the play area.** The solid parapet top (10.32) hides the
   consist from both pavements, whatever the distance along the street. The recommended fix is
   what parts of LRT-1 really have: a low concrete upstand with a see-through steel railing
   above. The other options are a parapet about 0.6 m tall, or the train as sound only.
2. **Cable shadows stripe the court floor under the low sun.** That breaks "the ability floor
   stays quiet". The proposal: the overhead cables cast no shadows in Unity.
3. **Rizal Hall IS visible from the court,** but only through the gap between the PGH block and
   the thinned Supreme Court, and small at about 95 m (`city_centre_tele_v1`). If the owner
   wants it bigger, it can move further east, or the Supreme Court can be thinned more.
4. **Naming:** real names for Manila Science High School and the churches, the invented
   barangay boards (BRGY. 671 on the hoop, Barangay 712 on the street board), the dull
   brick-red intersection box, and the Manok forecourt setback.

For ILALIM-1.4, Unity needs, from every kit:
- the extra UV channels (`UVGrime`, `UVSplash`, `UVSill`), or the grime baked into the albedo;
- emissive materials for screens, pad bars, signs and lamps, and cutout for grilles and leaves;
- the train consist at scale 1 on `lrt_train_root`;
- the pisonet cord trigger and the pares collider resized to the new props;
- a triangle budget: heritage about 770k, street about 560k, 400 trees; the trees need LODs or
  instancing.

NEXT: owner review of the assembled map and the decisions above.

Read first, in order: [AGENTS](../AGENTS.md), [VISION](VISION.md), [WORKING_RULES](WORKING_RULES.md),
this guide, then the map's existing design document [Ilalim_Ng_Tulay.md](Ilalim_Ng_Tulay.md)
(§ 0, § 1, § 3, § 4 and § 10.2 at least: they are the gameplay contract). The two finished
Blender maps are the worked examples: [KANTO_DESIGN_GUIDE.md](KANTO_DESIGN_GUIDE.md) (city
style, textures, signs, traffic, sound) and [LAGOON_REWORK_GUIDE.md](LAGOON_REWORK_GUIDE.md)
(organic terrain, prop kits, water, life, the Unity look).

---

## 0 · Place and feel (ILALIM-1.1 PROPOSAL, awaiting owner review)

### 0.1 The place, corrected

The elevated line past UP Manila is **LRT-1 over Taft Avenue**, not LRT-2. The old design
document's "Gilmore strip on Aurora Boulevard" is LRT-2, and it is replaced.

The new setting is **Taft Avenue at the Padre Faura corner, Ermita**:
- the UP Manila and PGH campus on the west side;
- the student commercial strip on the east side;
- LRT-1 overhead;
- **Rizal Hall** about 100 m down Padre Faura.

The facts and the photograph notes are in [the research report](reports/ilalim-rework-2026-09-29/research.md).

### 0.2 One line

**"After class on Taft."** One side of the street is heritage: shady trees, a fence, and Rizal
Hall. The other side is dense and noisy: shops, wires and signs. The grey concrete LRT-1 rumbles
overhead, and the late afternoon sun comes through the campus trees.

### 0.3 How the real block becomes the game

See [place-plan-v1.png](reports/ilalim-rework-2026-09-29/place-plan-v1.png). The real block is on
the left, and the proposed game plan is on the right.

- **The court is Taft's carriageway, under the viaduct.** The gameplay contract in § 1 is
  unchanged: the 14 m box, the walls at |x| 11 and |z| 16.5, and the piers at (±4.45, ±10).
  The game's +z is Taft's north, toward UN Avenue.
- **The west side (-x) is the campus.** Its elements:
  - A low iron fence stands on the |x| = 11 wall line, with the invisible wall collider behind
    it.
  - Behind the fence are lawn, shade trees, and a hospital front with gates. PGH is the real
    neighbour.
  - Vendors work against the fence, with striped umbrellas.

  The fence is see-through, so the frame opens up to the west. This is where the map gets its
  calm. Real Taft on the PGH side looks like this.
- **The east side (+x) is the shop row.** Real Taft on the east side is like this. The row
  carries:
  - **PC Express and its overclock pad**, which move here from the west wall;
  - the pisonet;
  - the pares cart;
  - a print, photocopy and bind shop;
  - a carinderia;
  - a medical-supply and uniform shop, because PGH is across the street;
  - dorm "bedspace" boards.

  Wires, awnings and signs are hand-named, in the Kanto way.
- **Padre Faura is the north cross street, at z ≈ +31, one-way west.** The ≈ +31 position is
  where the current map already has an intersection. Its corners:
  - **North-west: Rizal Hall**, pulled in from 100 m to about 50 m. It stands in its true
    orientation, with the portico facing south onto Padre Faura. From the court you see it
    across the campus lawn, at an angle, over the low fence. The plan keeps that view line
    clear of the hospital block and of the columned corner hall.
  - **The corner in front of Rizal Hall:** a white columned heritage hall, standing in for the
    Supreme Court. It is generic, with no seal and no name.
  - **North-east:** a tall school block, standing in for Manila Science High School.
- **The skyline to the east** has one landmark: a banded cream and salmon residential tower.
  This is the Astral Tower silhouette, generic and with no name. The mid-rise district fades
  into fog, as § 10.3 of the old document requires.
- **The south end** is a generic side street toward Pedro Gil.

### 0.4 The look, in kit terms

- **Rizal Hall.** Its parts:
  - three storeys of ivory stucco;
  - tall Ionic columns;
  - an entablature with a dentil cornice and round rosettes;
  - a red-brown hipped roof with deep eaves and exposed rafters;
  - dark steel-sash windows in deep reveals;
  - maroon serif "RIZAL HALL" letters over the entrance;
  - aircon units on the facade.

  It is chunky and bevelled in the Kanto style. It is the ONE hero building, and it gets the
  modelling time that PC Express got in the old map.
- **The campus mass.** Low heritage wings in cream with **red roofs among trees**. From the air,
  the real campus is a sea of red roofs.
- **The LRT-1 viaduct.** It follows the real structure's language:
  - a grey box-girder deck with a projecting parapet lip;
  - a dark, panel-jointed soffit;
  - catenary masts on top.

  ⚠️ The real piers are single median columns. The game needs two per row, which gives the
  7.5 m centre lane and the 1.85 m gutters. The proposal is **twin-leg piers under one pier
  cap**, in the same grey concrete, so the rows read as LRT-1 and still play the same.
- **The train.** It is yellow and blue in the LRT-1 manner. ⚠️ The livery blue must be shifted
  away from the defence role hue `#0080e8` (Art_Direction § 1). Use a deep navy or teal-grey,
  never a mid blue. The consist keeps its 15.6 m length and its 18 m/s speed, so the 2.70 s
  window and `OverclockSeconds` stay true.
- **The street kit, all taken from the photographs:**
  - tangled overhead cables on leaning poles;
  - yellow-painted steel railings and crowd barriers;
  - striped vendor umbrellas;
  - green street-name blades;
  - black ONE WAY signs;
  - mast-arm traffic lights;
  - a hand-painted yellow and red barangay board with invented names;
  - jeepneys, UV Express vans and buses (the traffic stays outside |z| 16.5).
- **Light.** A warm late-afternoon sun, low from the west, through the campus trees. The viaduct
  keeps its hard shade band across the court. The look profile row is retuned in Play.
- **Sound.** An LRT rumble, and jeepney barkers calling Taft routes. An ambulance siren from the
  hospital side, now and then. Vendors, and the pisonet and pares callouts that already exist.
  The Kanto street-sound pattern supplies all of these.
- **Life.** Kanto's pigeons on the campus lawn, and birds in the campus trees.

### 0.5 Kept exactly, from § 1

- The box stays empty and flat.
- The 4.2 m flanks stay clear.
- The hoop stays by the south-west pier.
- The pad stays reachable on the pavement.
- The trip hazards stay off the spawn-to-can line.
- The train stays at 6 s, then every 150 s.
- Every pier stays on NearFade.
- The cars stay outside |z| 16.5.

The only contract-adjacent change is WHICH pavement PC Express stands on (§ 0.6 decision 1).

### 0.6 Decisions for the owner before the blockout

1. **Campus on the west, shops on the east.** This moves PC Express and its pad from the west
   wall to the east pavement. The alternative is to keep shops on both sides and show the
   campus only behind the north-west corner.
2. **Rizal Hall at about 50 m, true orientation, portico facing south.** The alternative is to
   turn it to face the court, which is less true but reads more strongly from the play area.
3. **The Oblation** in front of Rizal Hall: include it (a stylized, chunky statue) or leave it
   out.
4. **Real names on screen.** "RIZAL HALL" is on the building in real life, so the proposal
   keeps it. Should PGH be named on the hospital gate? The Supreme Court corner stays unnamed.
5. **Twin-leg piers** as the honest compromise with the real single piers.
6. **Late-afternoon light.** The current Ilalim look is a cooler dusk.
7. **The display name.** Keep ILALIM NG TULAY. The scene name must stay `IlalimNgTulay`
   either way.

---

## 1 · What must not change (the gameplay contract)

The current map is code-built (`Editor/MapKit/IlalimNgTulayBuilder.cs` and its `Ilalim*Author`
finish passes) and its shape is deliberate. A rebuild changes how it LOOKS and how it is BUILT,
never these, unless the owner says so:

1. **The chalk box is the carriageway.** The road is 14 m kerb to kerb because
   `Balance.ConfinementRadius` is 7.0: a player reads the taya's confinement off the kerb line.
   Nothing solid taller than `StepOffset` (0.30 m) inside the box (`MapGeometryCheck.CheckBoxIsClear`
   fails the build). Everything inside the chalk is flat and desaturated (the ability floor).
2. **The flanks.** Playable x +/-11.2, z +/-16.7; 4.2 m of legal throwing room on each long side,
   props against the shopfront edge, never mid-pavement (Ilalim_Ng_Tulay.md § 1, § 10.2.3). This
   room is WHY the map exists (Eskinita has 1.6 m).
3. **The things that only exist here** (§ 4): the bridge hoop (TRES!, awards no score, crossing
   tested against the previous frame, `BridgeHoop`), the PC Express overclock pad (1.5x for
   2.2 s, reachable), the pisonet and the pares cart (their sounds and callouts), bank shots off
   the columns, BAWAL UMIHI DITO on two column faces, the trip hazards (extension cord, broth
   slick, GPU boxes, potholes at |x| = 3.4, never on the spawn-to-can line).
4. **The train.** The LRT (`Runtime/Map/LrtTrainFlyby.cs`, its rumble moves with the consist)
   passes 6 s into a round and then every 150 s. ⚠️ Ilalim_Ng_Tulay.md still says "every 24
   seconds"; that is stale (owner: "i want train to play rarely"). The builder's value is what
   ships, because it is baked into the scene (the class default of 300 is dead text). It is
   also balance, not only mood: `OverheadPassWindow` doubles Hero Strike cooldown rate while the
   consist is overhead. The train players see must match the train window (§ 8.2).
5. **Cars stay outside |z| = 16.5**, wheel-supported.
6. **The guideway joins hold**: joined bays, pillar-to-soffit and track-to-deck joins, train on
   rail, wires spanning between grounded poles (the builder's elevated gate reports them).
7. ⚠️⚠️ **The pillars fade near the camera, and the ambient occlusion depends on it.** The columns
   use the `TumbangPreso/NearFade` shader so a player standing beside one sees past it. The
   depth-normals prepass still draws them solid at the lens, which once blanked the whole frame's
   AO when facing a pillar. Since 2026-09-27 `WorldOutline.NearGuard()` switches the 1.8 m AO/contact
   gate on ONLY while a NearFade renderer is within 2.8 m of the camera, so: **keep every column
   on the NearFade shader in the rebuild**, or the gate never turns on here and the old fault
   returns (and if you add other near-camera occluders, put them on NearFade too).

---

## 2 · The pipeline Kanto and the Lagoon Cove proved

Source of truth is Python in `tools/`, run by headless Blender 5.0; nothing is hand-modelled in a
`.blend` that a script cannot rebuild. Then one export, then one Unity builder.

1. **Research and blockout.** Real references first (Taft Avenue at Padre Faura under LRT-1,
   § 0: the viaduct, the pier rhythm, Rizal Hall, the campus fence, the shop row, wires,
   jeepneys, the pisonet), then a
   grey blockout at the EXACT gameplay dimensions of § 1, reviewed by the owner in Blender before
   any detail (Kanto: `tools/author_kanto_blockout.py`; Lagoon: `author_lagoon_blockout.py`).
2. **Kits, textures, renders.** One script per kit (buildings, signs, props, vehicles), flat
   hand-painted textures, the art rules of `Art_Direction.md` § 0 (cute, chunky, stylized, no
   realistic detail). Render after every change, version every filename, critique before sending.
3. **Assemble** the map in one script that writes `ArtSource/<map>/<map>.blend` and review shots
   from the court at eye height (1.25 m, 95 degrees) and aerial.
4. **Export** one `.glb` per prototype plus a layout JSON (Kanto: `author_kanto_city.py`; Lagoon:
   `tools/export_lagoon_unity.py`). ⚠️ glTF negates X: Blender (x, y, z) lands at Unity (-x, z, -y).
   Materials travel by NAME only and are built in Unity from the layout's material list.
5. **Unity builder** (`Editor/MapKit/KantoSceneBuilder.cs`, `LagoonCoveSceneBuilder.cs`): places
   the prototypes, builds materials, colliders, the gameplay markers (Bounds walls, chalk, spawns,
   `~Match`), the sun. Scenes are never hand-edited: rebuild from code.
6. **The look** is a `WorldLookProfile` entry for the scene name (code defaults in
   `Runtime/Visual/WorldLookProfile.cs` AND the row in `Resources/WorldLookProfile.asset`, which
   overrides the code). Ilalim already has one; retune it for the new art with the owner.
7. **Map card** for the map vote: `Resources/UI/map-cards/<SceneName>.png`, rendered IN PLAY by
   `Editor/MapKit/MapCardCapture.cs` (extend its map list).
8. **Checks and tests**: `MapGeometryCheck` (Ilalim is GATED: the rebuild must pass it), the map's
   PlayMode probes, then the grouped gate. See § 4 for what is already red.

---

## 3 · Traps Kanto and the Lagoon Cove hit (read before building)

- ⚠️ **The look only exists in Play.** `WorldLookPresentation`, `ColourGrade` and `WorldOutline`
  are not edit-mode scripts: an editor-mode render is the authored scene, not the game. Measure
  and capture in Play (`Editor/MapKit/KantoLookMeasure.cs` compares editor vs Play side by side and
  logs RenderSettings, sun, grade, quality; `MapCardCapture` shoots through the match camera).
- ⚠️ **No `WorldLookProfile` entry means no AO and no world look at all** in Play; the map keeps its
  raw scene lighting and reads much brighter than the others (Kanto, 2026-09-27).
- **The player's settings change the frame**: the graphics profile drops shadow distance to 40 m,
  HDR is off, and the default render style adds a colour fringe. Judge the look at those settings.
- **Chalk colour** comes from the look entry's `dark` flag: pale court = light chalk. Kanto first
  shipped black lines by copying Bayan Plaza's flag.
- **The catch cutscene** now renders with the match look (it used to copy the camera without its
  components). Check it on the new map anyway.
- **Signs are hand-named**, one explicit table, person or family name plus trade, original, no real
  brands (Kanto's `SIGNS` in `tools/author_kanto_signage.py`; the teammate's note: "ang weird ng
  LABADA"). Ilalim already has eleven sign systems (§ 10.4): keep that variety.
- **Moving things** (traffic, the train) must not be static-batched and must have no collider a
  player, the lata or a tsinelas can touch. Traffic, birds and ambience are cosmetic and not
  networked (`KantoTraffic`, `LagoonFlocks`, `KantoStreetSound`, `LagoonSoundscape` are the patterns).
- **Heavy maps cost menu time and memory**: Custom and Host prepare a live preview of EVERY
  registered map behind the loading curtain and keep them cached. Six courts took 3.5 s against
  2.3 s for five, mostly the Lagoon Cove (3.47 M triangles). Measure triangles and the preview
  preparation time before and after.
- **Map indices travel over the network** (`MatchRpc` SyncMap, queue votes). Replacing
  `IlalimNgTulay.unity` IN PLACE under the same scene name keeps its index (no protocol change);
  adding, removing or reordering maps needs a `NetSession.ProtocolVersion` bump. Recommended:
  build the rebuild as an unregistered sample scene (as Kanto and the Lagoon Cove were), then swap
  it in under the old name and vault the old scene in `Scenes/Vault/` (the Lagoon precedent).
- **Line endings, metas**: commit the `.meta` of every new model, texture, clip and script, or
  other clones re-import them under new GUIDs and the scene's references break.
- **Unity is often open on the owner's machine**: runtime `.cs` edits recompile live (never
  mid-Play); batch Unity needs the editor closed (ask, never kill it); shader and material edits
  apply live.

---

## 4 · Test baseline to know before starting (2026-09-28)

The grouped PlayMode gate on `ASTRAReworks` c6506327 plus the Kanto/Lagoon merge: 99 failures, and
89 of them fail identically on a clean `ASTRAReworks` checkout (6 more only inside full groups).
Do not chase these as map regressions; compare against a clean checkout (the method is in
[the integration report](reports/map-integration-2026-09-28/issues.md)). Relevant here:
`BotBehaviourProbe`'s whole matches **never end on Eskinita or on Ilalim ng Tulay** (64,000 frames,
all 8 rounds played, no MatchRecord) on clean `ASTRAReworks` too. EditMode: 4 failures, all
pre-existing (a tournament switch, the roster arm bakes, equipment head clearance).

---

## 5 · First steps (proposed; the owner directs)

1. Read § 1, Ilalim_Ng_Tulay.md, and look at the current map in Play and at its renders.
2. Research the real place and write down, with the owner, what the rebuild should FEEL like
   (Kanto's § 1 and the Lagoon's § 1 are the format).
3. Blockout in Blender at the § 1 dimensions; render from the court at eye height; owner review.
4. Kits and textures, one at a time, owner review each.
5. Assemble, export, Unity builder as an unregistered sample scene; look entry; Play review.
6. MapGeometryCheck clean, probes green, then swap in under `IlalimNgTulay` and vault the old scene.
