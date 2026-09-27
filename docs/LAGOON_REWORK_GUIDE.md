# Lagoon Court rework guide

The full rework of the shipped map **Lagoon Court** (`GameLaunch.Maps` id `lagoon`, scene
`Scenes/Maps/Lagoon.unity`), directed by the owner from 2026-09-26 (*"i wanna rework lagoon
court"*). Read this whole file before touching anything Lagoon, then `docs/KANTO_DESIGN_GUIDE.md`,
whose model, texture, foliage and working rules ALL apply here too (Kanto is the first map in the
house style). Every rule below came from the owner reacting to a render; the quote is there so
nobody re-litigates it. Status row: `docs/TODO.md` **LAGOON-1**.

⚠️⚠️ **CURRENT STATE (2026-09-27, night): THE COVE IS IN UNITY** as the UNREGISTERED sample scene
`Assets/TumbangPreso/Scenes/Maps/LagoonCove.unity` (like Kanto: not in SceneFlow.Maps, GameLaunch,
MapGeometryCheck.Gated or build settings; open it and press Play). The shipped `Lagoon.unity`,
`LagoonBuilder*.cs` and `LagoonWater.cs` are untouched. This block is the snapshot to resume from;
the step list is § 8, the Unity pipeline is the "IN UNITY" item below the approved list.

**The pipeline, end to end (run in this order after any Blender change):**
1. `blender -b --python tools/author_lagoon_cove.py -- --preview N` rebuilds
   `ArtSource/lagoon/lagoon_cove.blend` and renders `Logs/lagoon-blender/cove_*_vN.png` (latest v56).
   Before rebuilding: diff the owner's saved .blend against the last committed build and record hand
   edits in `OWNER_DELETE` / `OWNER_MOVE` / `OWNER_ADD_PLANTS` (they apply 6/8, 1/1 today; the two
   unmatched deletions have been gone since the plant mix changed and are harmless).
2. `blender -b ArtSource/lagoon/lagoon_cove.blend --python tools/export_lagoon_unity.py` (about 16 s,
   never saves the .blend) writes `Assets/TumbangPreso/Art/LagoonCove/` (Models/*.glb, Textures/,
   `lagoon_cove_layout.json`; coordinates (-x, z, -y), proven to micrometres in its log).
3. Unity: menu Tumbang Preso > Sample Map > Build Lagoon Cove (or batch
   `-executeMethod TumbangPreso.EditorTools.MapKit.LagoonCoveSceneBuilder.RunReview`, renders to
   `Logs/lagoon-cove-unity-vN/`). ⚠️ Batch Unity cannot run while the owner has the editor open on this
   worktree (Temp/UnityLockfile); ask, never kill it. ⚠️ Editing Runtime .cs while the owner is in Play
   reloads scripts mid-match and wipes state (it happened once: thousands of null refs).

**Approved by the owner:** layout (cove v16); rock kit with rock_a + brushed organic edge wear
(§ 8 step 2); thatch a/b/c and sawali a/b/c mixed per house; plank_c; smooth painted terrain
(sand_a, grass_a, earth_a); Kanto-style leaf cards; the organic timber kit; all nine prop kits;
per-boat hull and stripe colours; the SUNSET look (below). **Provisional, awaiting a pick:** timber_a,
bamboo_a, tin_b (§ 8 step 3); the landmark emblem (pawikan in the build; pagi and bangka are the
alternatives, `landmark_court_v4.png`); cooler rock tint (asked, no answer: rocks stay rock_a).

**The Unity look (owner-directed on 2026-09-27 in play, every value a reaction to a screenshot):**
- Lighting: its OWN `WorldLookProfile` entry, `LagoonCove` (code defaults AND
  `Resources/WorldLookProfile.asset`; `Find("LagoonCove")` falls back to Lagoon's if the asset lacks
  it). Owner: *"can we use the lighting from the other maps... as well as the cloud/sky shader we
  already have set up"*, then *"too afternoon-y... i want a more sunset style of vibes"*, *"can you also
  make the sun lower"*. Golden key at 6 degrees over the OPEN SEA south-south-east (Unity -0.30, +0.954;
  *"we need to move the sun because its getting covered by the mountains"*), violet shade, peach haze
  70..340 m, dusky blue zenith to a peach horizon, peach clouds with lilac shade.
- Sky: `Shaders/NeighbourhoodSky.shader` gained three OPT-IN properties, all 0 on the shipped maps:
  `_SunDisc` (feathered warm disc, 0.055 rad), `_SunHalo` (two glows, 1.0) and `_SunClear` (clouds
  thin to nothing within 0.45 rad of the sun; *"get rid of that cloud blocking the sun"*). The
  builder writes them into `Art/MapAtmosphere/LagoonCoveSky.mat`.
- Water: `Shaders/LagoonCoveWater.shader` (built-in pipeline, `_CameraDepthTexture` requested by
  `Runtime/Visual/WaterDepthRequest.cs`, no grab pass). Colour and clarity by VERTICAL depth; caustics
  drawn on the seabed; foam from depth intersection at every shore, rock, pile and hull; a sky
  reflection of the scene's haze colour taken from the SMOOTH swell (*"light noise blotches"* came
  from the ripple normal); organic ripples = three rotated gradient-noise octaves with the two finer
  ones fading by 30 and 60 m (*"blocky noise shaped"*, then *"weird unnatural pattern"* from a sine
  sum, then *"less noise when viewing from an angle"*); a GLITTER PATH from shallow sun-aimed facets
  (+-6 degrees, a ~3.5 degree hit window: *"we're not getting any of those brightest reflections"*)
  plus deep-water GLIMMER from steeper facets (*"glimmer from outside the main sun path where the
  deep waters are"*). A baked reflection probe was tried and REVERTED (*"this version looks a lot
  more noisy"*). The builder resets the water material to the shader's defaults on every build, so
  retune the DEFAULTS; edit `Materials/lagoon_cove_water.mat` too only to show a change without a
  rebuild.
- Seabed: `SEABED = -9` (was -3.5, which capped the whole bay at 1.7 m deep: *"the seabed isnt deep
  enough"*). The first 8 m from the coast keep the old 0.12 m per m shelf; past that 0.3 m per m.
- Bounds: PER-SIDE arena limits (owner: *"fan out the bounds so players can also somewhat reach the
  water at the shore"*, choosing per-side over a bigger symmetric box). The cove's walls: land z -13,
  sea z +24, sides x +-16. See `docs/TODO.md` LAGOON-1.1: CODE DONE, TEST GATE NOT RUN.

**In flight / next (in this order):**
1. **CORALS (the owner's next step), IN PROGRESS 2026-09-27.** First run of `reefs()` placed ONE
   piece: the 1.5 to 6.5 m band lies wholly on the 22.8 degree drop-off past the 8 m shelf, over the
   16 degree hug limit (1159 "slope" refusals), and anchors out to 55 m sat on the 7.2 m floor (1221
   "depth"). Fixed: seabed pieces take slopes to 30 degrees and lean only 40 per cent into them,
   sinking by the gap; anchors sit 10 to 20 m off the coast. **Owner, seeing it: *"corals need to
   be larger, more organic/creative in shape and more dense. the seabed is empty rn"*.** So:
   (a) the kit is being reworked bigger and more organic, with new kinds: `reef_head` (a whole
   colony on a stone mound, 4 to 7 m), `tube_sponge`, `soft_coral`, `giant_clam`, `anemone`;
   (b) the open floor rises into soft REEF MOUNDS (`author_lagoon_cove.reef_floor`, crowns 2.4 m
   under the surface, 16 m clear of the coast so the shore profile is unchanged), because at 7.2 m
   the water's deep colour hides anything; (c) `mound_reefs()` rings a reef head per colony with 5
   to 10 pieces, and the shore band is 90 patches plus 24 meadows. The exporter carries the reef tint
   (MULTIPLY on the material colour) and sea grass classifies as two-sided foliage (its card
   material nudges hue).
2. **AO flicker (game-wide, not the cove):** the occlusion vanished for 0.5 to 1.5 s at round starts.
   Root cause found and the fix COMMITTED, not yet confirmed by the owner in play (see
   `docs/TODO.md` LIGHT-3.6): `VfxMaterial.ConfigureBuiltInFade` faded Standard hero effects without
   the Transparent RenderType override, so they drew solid into `_CameraDepthNormalsTexture` and the
   1.8 m near guard dropped the whole frame's occlusion. A TEMPORARY `[AO trace]` / `[AO sample]`
   diagnostic sits in `Runtime/Visual/WorldOutline.cs` (TraceAo, SampleAo): remove it once the owner
   confirms the fix.
3. **Bounds test gate:** with the owner's editor closed, rebuild the cove, run EditMode, then
   `py -3 tools/playmode_suite.py --gate`, assert on the XML totals, then tick LAGOON-1.1.
4. The owner's picks (timber/bamboo/tin, emblem, rock tint); meshes carry no tangents (the exporter
   should write them).
5. **Owner's next steps after the reefs (2026-09-27), in the owner's order; bodies in `docs/TODO.md`:**
   the catch cutscene lit by the editor lighting instead of the match look (LAGOON-1.3); birds and
   fish as boids (LAGOON-1.4); ambient waves, wind and birds that swell with facing and distance
   (LAGOON-1.5); Lagoon Cove and Kanto into the main map pool (LAGOON-1.6).

**Loose files, deliberately uncommitted:** `Tests/PlayMode/LagoonCoveInGameShots.cs` (an in-game
capture that HUNG for hours in batch at the scene load; do not run it unattended; fix or delete);
`Scenes/Maps/Kanto.unity` and `Art/MapAtmosphere/KantoSky.mat` modified by the owner's editor, not by
this work (leave them for the owner).

**⚠️ The Blender-side sky port stays DEFERRED** (the Unity scene already wears the game's sky shader and blocky clouds, see above). Earlier note: **Sky is DEFERRED by the owner** (*"we can do the sky stuff later, just focus on the other
stuff"*). The game's new sky lives in the repo (branch ASTRAReworks, now merged into this branch
at `e662f361`): `Runtime/Visual/WorldLookProfile.cs` (per-map MapLook, Lagoon entry), 
`BlockyClouds.cs` + `Shaders/BlockyCloud.shader` (voxel cumulus), `Shaders/NeighbourhoodSky.shader`.
When resumed, port THAT into the Blender renders (`tools/author_lagoon_sky.py` currently holds an
earlier painted-panorama sky, committed, still called by the build). Its violet shade may clash
with the owner's earlier rejection of cold blue-grey rock shade: show it, do not silently warm it.

**History: landed 2026-09-27 before the Unity step (all committed locally, not pushed):**
- The organic timber kit, `tools/author_lagoon_structures.py`: `modelled_walk` (individual planks
  on stringers, planks fanning round bends, bents with round piles), `organic_stairs` (solid
  sawtooth side boards, blocky treads, landings, chunky rails), organic railing, ledge railing,
  cliff walk, pier and broken pier. Every plank and tread reads the terrain under its four corners
  and stands at least 4 cm clear (0 of 3992 corners within 1 cm). SWAPPED INTO the cove script:
  `walk_path`/`walk` and `plank_stairs` are no longer called (kept in the file for reference).
- All eight prop kits, simplified to the chunky organic rule: beach (nets, rope), village (signs,
  capiz lanterns), seating, clay, fishing (fish rack, bubo, bilao and bayong; own `pf_rattan` and
  `pf_fish` since the beach kit's were withdrawn), cargo (crate, barrel, water drum; the cargo kit
  owns the drum), shore (oars, driftwood, anchor stone, split firewood), textile (banig, hanging
  net, laundry line). Owner-facing sheet: `props_all_v1.png`. Open points for the owner: pastel
  washing faint at 20 m; crate boards plain in bright sun; catch heap abstract up close.
- PROP PLACEMENT, `tools/lagoon_props_place.py`, called by the cove after the planting: linked
  duplicates from a hidden "Props kit (source, not placed)" collection, its own Random(41),
  grounded on the lowest ground under the footprint, refused on water, the play walls (+-13 m),
  tilt over 0.35 m, spacing, or a downward ray hitting a stone, house, boat or railing (low
  plants yield and are removed afterwards; palms do not). Clusters: two piles at the pier's foot
  and cargo on its deck, gear round each beached bangka, fish racks with baskets and traps on
  the beach west of the court, per land house a lantern post, pots by the steps, two yard pieces
  and a capiz lantern under the eave (ray cast), the stall's sari-sari wall sign and cargo, tables
  and a lean-to on the court's edge, driftwood along the sand. First full build: cove v52.
- Owner: *"the last types of foliage and fauna added dont have much textures on them"*: grass
  blades and leaf stalks repainted (§ 8 step 5).
- Cove v53: 121 props. Added the water homes' porches (pieces must sit OUTSIDE the walls, a level
  ray from the room's middle meeting a wall first, and on level deck under the whole outline +5 %
  so nothing overhangs a railing), a capiz lantern under most water-home eaves, pots either side
  of each stair's foot, denser piles (spacing 0.15 m), and clearance rays over the full outline
  (owner's screenshot: a barrel touching the stall's step).
- Owner: *"theres this green untextured barrel too"*: `pg_plastic` (the water drums) was 3.5 %
  patches; now sun-chalked +14 %, warm grime -16 % and a few soft 14 cm scuffs with a shallow
  dent (`pg_drum_beforeafter_v1.png`; a 7 cm scuff pass read as a rash of specks and was cut).
- Owner, on v53 (four notes, all in cove v54):
  *"the baskets you put got clipped into the ground"*: props sat at the LOWEST ground under them
  with 0.35 m of fall allowed. Now low pieces (baskets, mats, nets, rope, driftwood, traps, jars)
  are TILTED onto a plane fitted to the real ground MESH under nine points (refused over 16
  degrees or where the ground bulges 4 cm off it); upright pieces take 5 cm of fall, legged ones
  (racks, tables, benches, posts) 15 cm. *"this shack near the play area"*: a lean-to stood over
  the foot of the court's west stair between the nine clearance samples; clearance is now a grid
  every 0.4 m, and nothing stands within 1.4 m of a flight's line or 2 m past its ends (pots at
  stair feet excepted). *"can we have some fishing stuff sparsely scattered in clusters around
  the rock platform outside the play area?"*: `court_ring`, six small clusters of two or three
  fishing pieces on the court's flat ground outside the play walls. *"can we have variations in
  the colors of the boats?"*: `boat_colours` in the cove script gives each PLACED boat its own
  hull paint (white 5 in 9; sea green, sun yellow, brick red, deep green; the first, paler set read white and salmon in the sun) and stripe (red, yellow,
  teal green, maroon, leaf green, white, dark), never a stripe of the hull's own hue family, as
  object-level material slots (Unity: one material per boat renderer).
- **IN UNITY (2026-09-27, owner: *"now put it in unity, make sure you create a proper water shader
  for it"*).** Export: `blender -b ArtSource/lagoon/lagoon_cove.blend --python
  tools/export_lagoon_unity.py` (never saves the .blend) writes 132 .glb prototypes, 152 textures
  and `Art/LagoonCove/lagoon_cove_layout.json` (1236 placements as Unity-axes matrices, 90
  materials, overrides, sun, gameplay). Scene: `Editor/MapKit/LagoonCoveSceneBuilder.cs`
  (`Run`, `RunReview`, `Open`; menu Tumbang Preso/Sample Map) builds
  `Scenes/Maps/LagoonCove.unity`, UNREGISTERED like Kanto: the shipped `Lagoon.unity`, its builder
  and `LagoonWater.cs` gameplay are untouched until § 7 is settled. Shaders (built-in pipeline; the
  URP package is installed but not active): `LagoonGround`, `LagoonRock`, `LagoonPainted`,
  `LagoonFoliage`, `LagoonNoise.cginc`, and the water, `LagoonCoveWater.shader`: colour and
  clarity by VERTICAL depth over the seabed (from `_CameraDepthTexture`, asked for by
  `Runtime/Visual/WaterDepthRequest.cs`), caustics drawn on the seabed with two-octave warp, foam
  from depth intersection at every shore, rock, pile and hull, gentle swells, sparse sun sparkles,
  no grab pass (Android). Its defaults are the source of truth: the builder resets the material to
  them on every build. Water review rounds: v5 fixed pale mint shallows and straight caustic
  cells; v6 fixed glints blowing out into white sheets facing the sun. Renders:
  `Logs/lagoon-cove-unity-v6/`. Open points: meshes carry no tangents (the exporter should write
  them; the shaders fall back), shadows beyond 150 m are off (project setting), the sky is the
  shared cloud sky (the ASTRAReworks sky is still deferred), 3.47 M placed triangles.
- **Next:** the props are true to scale and so small beside 3 to 6 m boulders from the reference
  angle; the owner decides whether the reference's density is wanted (more pieces, or bigger
  hero pieces such as boats on racks and net-drying frames). Then the owner's picks
  (timber/bamboo/tin, emblem, rock tint), and later the sky.

---

## 1 · References (owner-chosen)

| Reference | Take from it |
|---|---|
| [Anastasia Papaioanou, Stylized Fishing Village](https://www.artstation.com/artwork/GvJv5a) (Unreal, sold on FAB; owner: *"seems like a good ref"*) | **The art style and the ground layout.** A rock massif built as a PILE of big rounded boulders the village climbs; saturated turquoise water with a pale band at the sand; stocky plank houses with oversized deep-eaved roofs; dense tropical planting (palms leaning out of rock seams, broad leaves, red and orange accents at boulder feet); piers, stairs and plank walkways at several heights; boats, crates, barrels, nets, lanterns; one landmark at the waterline (the painted octopus rock); warm sun under big painted clouds. Built from a modular kit (plank wall modules, stacked roof modules, stair pieces, a small prop kit). |
| Owner photograph: a Filipino stilt-house village at sunset | **The Filipino subject.** *"we should make it more filipino like too btw, like these stilt houses but following the stylized artstyle"*. Steep nipa/cogon thatch roofs, woven sawali (split bamboo) and plank walls, slender irregular bamboo piles with X bracing, bamboo railings and ladders, fishing nets drying, small painted bangka outriggers, a green palm hill and a beach behind. |
| [ANGRY MESH, Stylized Water](https://www.fab.com/listings/1bb6ee1a-250b-4fe7-95eb-1f6c16275f9d) (Unreal, a Single Layer Water shader; owner 2026-09-26: *"the water should be stylized like this"*) | **The water.** CLEAR over a visible sandy, stony bottom in the shallows; colour by depth, vivid cyan-turquoise shallow to deep blue-green; a net of bright wobbly cellular CAUSTIC lines drifting over the shallows; soft contact foam and ripples. It is a SHADER, so in this project it is `LagoonWater.shader` (URP) in step 7, not geometry. |
| Owner photograph: a Sama-Bajau water village | **The village is ON THE WATER.** *"we need space for boats and free-standing stilt houses because badjao tribe isnt particularly land based"*. Small free-standing stilt homes over clear shallow turquoise water, narrow plank walks and ladders, tin and thatch roofs, laundry lines, boats both moored and paddled (long lepa hulls, bangkas). |

Earlier primary-source notes (Sama Dilaut stilt homes, lepa houseboats, the Lookan Banaran
photograph): `docs/reports/map-by-map-refinement-2026-09-23/lagoon-reference-notes.md`.

## 2 · Art-style rules (every Lagoon model and texture)

- **House style = Kanto's** (`KANTO_DESIGN_GUIDE.md` § 2, § 3, § 5): real Blender models built
  by `bpy` scripts, one-piece shells, swept trim, **no two surfaces sharing a plane**, live
  bevels; textures FLAT and hand-illustrated (a few large feathered patches, hand-drawn shapes,
  low contrast between a thing and its joints, no grain, no noise, no streaks); foliage as
  shingled see-through leaf cards, two tints only, variation per plant never per leaf.
- ⚠️ **Never repurpose one texture's generator for another surface.** The owner rejected bark and
  roof tiles that were the brick generator in disguise (*"did you just repurpose the brick
  texture?"*, *"it genuinely just looks like a repurposed brick texture"*). Every surface gets its
  own drawing.
- ⚠️ **Research real stylized references before drawing a new texture** (*"look into actual
  stylized hand illustrated textures online and research their style"*). Airbrushed smudges were
  rejected as *"garbage"*.
- ⚠️ **Swatches first, one change per round**: a flat swatch sheet beside approved swatches
  (brick, stone_blocks) and a 3 x 3 repeat, approved by the owner, before it touches a model;
  then one game-distance render (1.25 m eye, 95°) and one close-up; version every filename.
- ⚠️ **EVERY TEXTURE REVIEW SHOWS IT ON THE MODELS** (owner, 2026-09-26: *"when you texture i
  need a render of how it's gonna look on the models"*). A swatch sheet alone is not a review.
  `tools/render_lagoon_texture_preview.py` rebuilds a material around each candidate texture and
  renders the same game's-eye and close-up shots for each, composed into one labelled sheet
  (`Logs/lagoon-blender/<material>_on_models_vN.png`). Send it WITH the swatch sheet.
- **Role hues** (`Art_Direction.md` § 1): nothing near offence orange `#f87020` or defence blue
  `#0080e8`. The fishing village's bright turquoise ROOFS are too close to defence blue: roofs are
  thatch, with the odd tin roof in teal-GREEN or red. The water's turquoise is fine.
- **Filipino, not generic tropical**: nipa/cogon thatch, sawali walls, bamboo piles and railings,
  bangka and lepa boats, capiz windows, a capilla, a sari-sari stall, drying nets, laundry.
  Culturally specific motifs (lepa prow carvings, festival flags) are not copied onto every boat.
- Be critical of every render before sending it; the owner expects the lead to spot the flaws.
- ⚠️⚠️ **CHUNKY AND ORGANIC, NEVER FIDDLY** (owner, 2026-09-27: *"i dont like how details some
  of the props are. again we're going for a stylized semi-cartoony environment style"*, and
  *"you should really experiment more with being organic in how you shape things. the railings
  for example are just thin and plain shapes, where as the reference isnt just a straight
  rectangular prism"*). Every model: few, thick, rounded, slightly irregular members (taper,
  softened edges and ends, a small bend, lean and size jitter per piece); detail belongs in the
  painted texture, not in geometry (no strands, knots, thin slats, fine lattices, tiny
  hardware); textures use few, big, soft shapes at low contrast.
- ⚠️ **Walkways are MODELLED planks, not a textured strip** (owner: *"this deck is actually
  modeled but the main houses walkway is just a texture, fix that"*). Stairs follow the
  reference kit: sawtooth-sided stringers, thick treads, chunky railings.
- ⚠️ **Nothing coplanar with the terrain** (owner: *"z-fighting on some of the stair
  entrances"*): any deck, landing or tread meeting the ground sits clearly above it.

## 3 · Rocks (owner rejected cones and crystals)

- 🧑 on blockout v5: *"i want the mountain not to just be one spiky mountain peak"*, *"i assume
  the rocks wont be just like that correct? and actually be more unique?"*, *"experiment more on
  something organic until it looks similar to the reference"*.
- **The massif is a PILE of boulders** with several humps, ridges and a saddle, never one cone.
  Underneath it, a height field of several gaussian peaks is the fill between the stones.
- **Boulders are PILLOW STONES**: a subdivided cube pulled ~70 % of the way to a sphere,
  stretched taller than wide, chopped by 3 to 5 random planes into BROAD flat faces, jittered.
  SMOOTH shading with only edges over ~38° between faces kept sharp (flat shading made them read
  as crystals). A kit of unique shapes shared as linked meshes, each placed with its own size,
  stretch and turn.
- **Rock belongs to the massif**; the outer ring and the coast are sand and grass.
- **A ledge's downhill face is still stone**, capped just under the ledge floor so the house
  looks out over it (clearing the face showed smooth dirt cliffs). Steep ground is rock, never
  grass (green cliffs).
- Warm tan rock; lighter tops and darker bases come in the texture pass; AgX Punchy look.

## 4 · Layout (current: cove v16)

- **One asymmetric island weighted north-west**, drawn from a hand-placed coast curve (`COAST`,
  Catmull-Rom smoothed): a long sand SPIT curling south on the west (its own house and a tidal
  pool), rock dropping straight into the sea on the east. Nothing mirrors across x = 0.
- **The beach** is a band inside the coast (`BEACH_BAND` 8 m) with a noise-wobbled landward edge.
- **The court** is the biggest natural CLEARING: 28 x 26 at z = 0 (the current map's tested
  size), boulders on its landward sides with inner faces just outside the play walls, open south
  down to the beach and the water. Heights: beach -1.2, water -1.8, seabed -3.5.
- **Every land house has its own POCKET** (a flat ledge) at its own height: twelve pockets at nine
  heights from the spit up to the summit; a fence on the drop side; stairs between pockets. 🧑:
  *"notice how the houses are not just on a single level"*. The **capilla** (white, bell tower,
  red tin roof) on the summit ledge is the landmark seen from the court.
- **The Sama-Bajau water village** (cove v7) grows along ONE MAIN WALKWAY (`SPINE`): a jetty
  climbing off the sand just east of the court's beach front, then a plank walk ~1.9 m over the
  water wandering south-east. SMALL one-room stilt homes branch off it on 5.5 to 8.5 m spurs,
  alternating sides with the odd gap; a second walk (`EAST_WALK`) runs east under the sea cliff.
  Seven homes stand alone out in the sea (`FREE_HOMES`), reached only by boat. The first spine
  homes stand 12 to 20 m off the court's south edge, so the village shows from the court. Boats:
  moored along the spine and by homes, a few paddled out in open water, and bangkas pulled up on
  the spit's sand. (v6 was ten loose clusters on random headings and read as a scattered blob.)
- **Landmark rock** in the water south-west of the court's front (`LANDMARK`), left of the spine
  as seen from the court, to carry a painted emblem or banner.
- **Feature boulders** (`FEATURES`, v7): seven hand-placed giants give the massif its silhouette
  and the coast its landmarks (a stone in the surf under the east cliff, one behind the court's
  north-west corner, a pair at the spit's root, two on the northern skyline, a sea stack off the
  spit). The massif's stones now vary: about one in six is small (tucked in the seams), the odd
  one low down is half again as big; small shore rocks are half buried along the sand.
- **Water** (v15, `tools/lagoon_cove_water.py`): a Blender STAND-IN for the Unity water shader,
  close enough to judge the layout. Clear shallows (alpha 0.5) over a teal-tinted sandy seabed
  that shelves gently (0.12 m per m) so a wide band of shallows shows, vivid turquoise, then deep
  teal and opaque by ~30 m; still Voronoi caustic lines, bent by noise, strongest in the
  shallows and broken into patches. ⚠️ **NO MODELLED FOAM** (owner, 2026-09-26: *"we'll be
  implementing moving shore white foam using shaders in unity"*): v7 to v9 had a foam ribbon
  mesh; it is gone, and `build_foam` is kept only as a reference for the band's width.
  **Sky**: a vertical gradient seen by the camera only.
- ⚠️ **Smooth terrain** (v15; owner on v9: *"are we able to make these edges less jagged?"*):
  the height field had JUMPS at the waterline, the beach's landward edge and every pocket rim,
  which the grid drew as staircases. Every transition is continuous now (smoothstep blends), the
  waterline sits exactly on the coast curve, and the ground is smooth shaded. Keep it that way:
  never return a height that jumps between neighbouring points.
- ⚠️ **Painted ground edges** (v16; owner on v15, at the court's staircase colour edge: *"you'll
  need to retopologize the edges i think. unless you can figure out how to fix this jagged
  texture/color stuff"*). Not a topology fault: v7 to v15 stored a finished COLOUR per vertex,
  and colours blend across a 1.15 m cell as a staircase. Each vertex now stores continuous
  signed FIELDS in metres (`court_in`, `grass_in`, `sand_in`, `wet_depth`, `steep`, `ring`),
  and the `ground_painted` material cuts each at zero with a narrow smoothstep plus a ±0.6 m
  noise wobble, so every boundary is a smooth hand-painted curve at any grid size. These fields
  are the splat masks for the ground textures in step 2 onward; never go back to per-vertex
  colours.
- **Planting** (v7, blockout kit `tools/lagoon_cove_planting.py`): coconut palms with curved
  trunks and drooping fronds at the pockets' rims (leaning out over the drop) and along the sand,
  thickest on the spit (leaning to the sea); grass tufts, banana/taro broad leaves and crimson or
  yellow flowering bushes in the gaps at boulder feet.
- The ground is coloured per VERTEX (v7), so sand, grass, rock and court edges are soft painted
  boundaries rather than 1.35 m staircases.

## 5 · Layout history (rejected, do not return to)

| Version | What | Owner |
|---|---|---|
| blockout v1 to v3 | court on a square rock SHELF 4.5 m over the water, one green hill, a stilt-house ring | *"i was thinking of a more natural platform"* |
| blockout v5 | horseshoe cove, icosphere boulders, one cone spire, three flat green terraces | *"not ... one spiky mountain peak"*, *"houses are not just on a single level"*, rocks not *"unique"* |
| cove v1 to v4 | boulder massif, many pockets, but a mirror-symmetric box lagoon with stilt houses along its sides | *"absurdly symmetrical"*, beach not *"organic"*, *"free up some more space ... for boats and free-standing stilt houses"* |

## 6 · Files and commands

| What | Command | Output |
|---|---|---|
| Organic cove blockout (CURRENT) | `blender -b --python tools/author_lagoon_cove.py -- --preview N` | `ArtSource/lagoon/lagoon_cove.blend`, `Logs/lagoon-blender/cove_<shot>_vN.png` (plan, ref_angle, village, aerial, eye_north/east/south/west) |
| Sea gradient and foam line (called by the cove script) | `tools/lagoon_cove_water.py`: `build_sea`, `build_foam` | the "sea" and "foam" objects |
| Blockout planting kit (called by the cove script) | `tools/lagoon_cove_planting.py`: `palm`, `tuft`, `broadleaf`, `flower_bush`, `plant_gaps` | shared-mesh plants |
| Old shelf blockout (rejected; kept for its helpers) | `tools/author_lagoon_blockout.py` | `lagoon_blockout.blend` |
| Every shipped map photographed the same way (headless) | Unity `-batchmode -executeMethod TumbangPreso.EditorTools.MapKit.MapLineupCapture.Run` (menu Tumbang Preso > Maps > Render Map Lineup) | `Logs/map-lineup-vN/` |

Key names in `author_lagoon_cove.py`: `COAST`, `BEACH_BAND`, `POCKETS` (name, x, y, floor z, rx,
ry, surface), `STAIRS`, `PEAKS`, `coast_distance()`, `height()`, `boulder_mesh()`,
`place_boulders()`, `house(..., small=)`, `water_village()`, `walk()`, `planting()`.
`author_lagoon_blockout.py` supplies `col()`, `gameplay()` (chalk, lata, 1.6 m player stand-ins)
and colour names; `author_kanto_blockout.py` supplies `box/cylinder/blob/label/_obj/lighting`.
Opening a file for the owner: `Start-Process` blender.exe on the `.blend` (never screen-click).

## 7 · Gameplay constraints to settle before Unity

- The current map's court is 28 x 26; its water returns a slipper after 8 s (`LagoonWater`:
  SurfaceY -1.1, FloorY -3, Limit 42). The new court opens onto beach and shallow water on one
  side only; confirm the play walls, where the water starts and whether the return rule stays.
- `MapGeometryCheck`, `ArenaCheck`, the Hero Strike footprint and bot routing must pass on the
  rebuilt scene. The old builder is `Editor/MapKit/LagoonBuilder*.cs`.

## 7a · Gap review against the reference (2026-09-27, cove v48)

Owner: *"take a look at the references and identify what else is missing from our current
map"*. Compared against the ArtStation GvJv5a shots (the village overview, the beach with the
octopus rock, the boathouse close-up) and its kit sheets (buildings and props, stairs and wall
modules, foliage and terrain). In rough order of impact:

1. **Props: almost none.** The reference is dense with them: nets drying on the sand and piled
   on piers, barrels, crates, pots and jars, lanterns on posts, shovels and oars, rope coils,
   hanging shop signs, a round table with benches, overturned and broken rowboats. We have
   boats and laundry only. (Filipino versions: drying fish racks, bubo fish traps, banga jars,
   bamboo baskets, capiz lanterns, a sari-sari sign.) This is § 8 step 5.
2. **Railings along every ledge and walk.** The reference runs continuous wooden rails along
   each ledge edge, boardwalk and pier; ours has loose fence stubs and bare walks.
3. **Boardwalks along the cliff, not only stairs.** Its houses are joined by plank walks
   running ALONG the rock face at several levels, with stairs between; ours are stairs only.
4. **A proper beach pier.** A plank pier into the water with posts rising above the deck,
   barrels, a net pile and a moored rowboat, plus a broken old pier on the sand.
5. **The landmark's emblem.** Its signature is the painted octopus rock (and a small painted
   motif on the cliff). Our landmark rock is blank.
6. **Palms.** Its trunks curve (S bends, leaning) with warm banded orange-brown bark; its fronds
   are broad and chunky with a yellow-green gradient. Ours are straight, grey and thin-leafed.
7. **Foliage variety at rock feet.** Red and orange ferny accents (croton), monstera-like round
   leaves, dense low ground cover and grass tufts spilling over ledge edges. Ours: banana, taro,
   tufts and flower bushes, and grass pockets that are flat discs.
8. **House dressing.** Hanging signs, dormers, lean-to shades on posts, open shutters,
   lanterns, pots at the door, nets on the walls.
9. **Sky and light.** Big painted clouds, a warm golden key light and cool shadows. Ours is a
   plain gradient in flat grey haze (§ 8 step 7).
10. **Water.** Shader waves, a foam line and a depth colour (§ 8 step 7, Unity).
11. **Backdrop.** Tall rock spires and distant islands behind the village; our horizon is empty.
12. **Rock versus sand.** Its rocks are a cool grey-khaki against warm yellow sand, so the
    two separate; ours are both warm tan and blend. (rock_a is approved; this would be a tint
    decision for the owner, not a new texture.)

## 8 · Plan (in order; each step rendered in Blender and approved before the next)

1. **Finish the layout.** ✅ LAYOUT COMPLETE at cove v16 (2026-09-26), awaiting the owner's
   sign-off to start texturing. v9 closed every gap listed after v6 (shallows, a planted spit
   with beached bangkas, the water village on a walkway spine seen from the court, feature
   boulders and a wider stone size range, gap planting, rim stones on every ledge front, stone
   steps, a gradient sky). v15 answered the owner's v9 review: smooth terrain edges, clear
   stylized water over a visible seabed, no modelled foam; v16 painted the ground edges from
   continuous fields (§ 4). ⚠️ The reference's orange
   flower accents are CRIMSON and YELLOW here: orange sits too close to offence orange `#f87020`
   (§ 2). Blockout-grade on purpose, replaced by the kits in the later steps: every roof is one
   pyramid (step 3), stairs are plain stone blocks, the court floor is flat, the water is still.
2. **Rock kit**: final pillow-boulder models and a painted rock texture (swatch first: warm tan,
   lighter tops, soft darker seams); rebuild the massif from them. IN PROGRESS 2026-09-26.
   - ✅ **Texture: rock_a** (owner: *"rocka looks nice"*): flat warm tan, two feathered coats,
     4 m a tile, `tools/author_lagoon_textures.py`. ⚠️ **REJECTED: light lines painted INTO a
     tiling texture** (rock_b, rock_c): *"the issue with the other textures is the white cell
     lines you added ... it doesnt work that way on a tileing texture. you genuinely need to
     weather only the edges of the rock instead of imitating it on the tiled texture"*.
   - ✅ **Edge wear follows the MODEL's edges** (owner, of the reference: *"the edges are lined
     with a brighter color compared to the inside plane of the rock faces"*).
     `tools/bake_lagoon_rock_edges.py` gives every rock mesh a unique second UV map "UVBake" in
     its own cell of ONE shared atlas (`ArtSource/lagoon/textures/rock_edges_atlas.png`) and
     Cycles-bakes a convex-edge mask from its geometry (bevel-normal edge detector, convex only,
     bevel radius 0.14 of a ~2.5-unit stone). The material mixes a warm light tan (not white)
     along it with a SOFT ramp into the plane, broken by noise, strongest on upward-facing edges.
     v4's crisp hairline read as an outline and was replaced (`rock_on_models_rock_a+edges_*`).
     Unity reads the mask through UV2 with the same single material.
   - ⚠️ **REJECTED: a light line along every edge, in its own colour** (owner on v7: *"the
     weathered edges look really unnatural"*, *"even how the color of the weathered edges are
     unnatural"*). One even band round every break, sides and bottoms included, read as piping,
     and a flat tan mixed over the rock read as paint. **The wear now (v9, `ROCK_LOOK =
     "rock_a+chips"`)**: the bake stores THREE masks (R narrow plane breaks, G a broad soft
     shoulder gradient, B the stone's own occlusion); the material LIGHTENS the rounded
     shoulders softly (up to 1.3x, weighted to up-facing surfaces), DARKENS the crevices (to
     0.72x), and adds a few crisper chips on TOP edges only, broken into pieces by noise. It
     only ever scales the rock's own texture: never a separate colour
     (`rock_on_models_v9.png`).
   - ✅ **BRUSHED EDGES, FROM THE OWNER'S PAINT-OVER** (current, `ROCK_LOOK = "rock_a+brush"`,
     `brushed_edge()`). The owner rejected the inner-shadow version below (*"nope. its
     worse"*) and painted over two rocks in Photoshop
     (`Logs/lagoon-blender/owner_rock_edges_paintover.webp`, *"see the center 2 rocks"*): each
     face stays the PLAIN rock colour; a thin crisp pale LINE sits exactly on the edge; a
     NARROW light BAND runs just inside it, its inner side broken up like dry brush; strongest
     round the top faces, sides darker. Built from the narrow mask only (the broad mask
     lifted whole faces, which was the fault): band = mask above a fine-noise threshold
     (0.04 to 0.30) lifting the rock's own colour to 1.40x, line = mask 0.70 to 0.86 toward
     warm near-white at 0.75; both weighted to up-facing surfaces. `rock_on_models_v15.png`.
   - ⚠️ **WORLD-SIZE, ORGANIC WIDTHS** (owner on v15: *"it doesnt scale properly with the rock
     size, some larger rocks have it too thick and bulky"*, *"the inner ones are also always the
     same stroke width. theres no organicness aside from the edges"*). The mask was baked in the
     KIT's units, so a stone placed 8x larger had a band 8x wider; and the Cycles bevel detector
     peaked differently on every break, so it could not be read as a distance. Now: the R
     channel is the TRUE distance to the nearest plane break, computed from the mesh in numpy
     (`distance_to_breaks`, linear, 0 at `DIST_MAX` 0.25 stone units; atlas 4096). Only breaks
     over 20 degrees with at least one UP-facing face count (12 degrees lined every small facet
     and brought back the paving look). Every placed stone stores `rock_scale`; the material
     turns a width in METRES into a mask threshold, 1 - W / (scale x 0.25), so every rock gets
     the same world-size band (Unity: the scale from the transform). A slow noise swells and
     thins the band along each edge (0.12 to 0.55 m) and a fine noise frays its inner side;
     the line is ~5 cm and fainter along some stretches. Re-packing UVBake is idempotent
     (re-baking a saved file used to shrink every island into a sliver).
   - ⚠️ **THE LINE IS ORGANIC AND ROCK-COLOURED** (owner on v19: *"same one flat width issue
     for the white edge, also i think its a bit too white and in-organic"*, *"its okay if it
     gets cut off along an edge too"*): its own varying width (1.5 to 8.5 cm), frayed by the
     dry-brush noise, broken into stretches with gaps, and a lighter ROCK colour (the rock's
     own ~1.5x plus a hint of cream), never a near-white stroke. `rock_edge_line_crop_v20.png`.
   - ⚠️ **EVERY EDGE, TWO INDEPENDENT STROKES, GRUNGE** (owner on v20: *"you essentially binded
     the inner stroke to the white stroke, so where the white stroke doesnt appear, the inner one
     doesnt either"*, *"i want this to be applied to all edges of the rock, not just the
     top-facing ones"*, the line must be *"cut ALONG an edge like somewhere in the middle, not
     limited to just 1 cut"*, *"its a rock its supposed to be grungey and graining"*). Edges are
     found as the eye sees them (`plane_breaks`): faces grown into broad planes (13 degrees);
     two planes more than 24 degrees apart meet at a break; a small region is a bevel STRIP
     (part of the break) only if it lies between two big planes that really differ (counting
     every small patch lined the rounded bodies in a grid, v21). No up-facing filter or weight:
     side and bottom edges are lined too. The band is on every break regardless of the line;
     the line is cut several times along each edge (a noise at ~0.3 m), and a very fine grain
     eats into both. `rock_on_models_rock_a+brush_2_v22.png`, `_3_v22.png`.
     Then (owner: *"can you make the white color more subtle"*) the line lifts the rock's own
     colour 1.3x with a 0.05 hint of cream (was 1.5x and 0.18).
   - SUPERSEDED, **inner-shadow edges** (owner on v9: *"can you make them slightly more clear? think of
     like an inner shadow effect, the edges have the crispiest white and then it fades the
     closer it gets to the center"*). `ROCK_LOOK = "rock_a+inner"`, `inner_glow()` in
     `tools/render_lagoon_texture_preview.py`: a CRISP warm near-white core on the break
     (narrow mask R, `EDGE_WHITE` 0.99/0.88/0.68), then a FADE toward the face's centre that
     lightens the rock's OWN colour up to 1.45x (broad mask G), crevices darkened by the
     occlusion (B). Weighted to top faces: the core by the square of the up-weight, the fade
     too. Rejected on the way: a cooler white and an equal weight on the sides (v10/v11 turned
     shaded side faces blue-grey), a fade toward white (same fault), a short fade (did not
     read). `rock_on_models_v13.png`.
   - The rock MATERIAL maps rock_a by world-space box projection (Unity: triplanar) and adds
     light tops and dark undersides by face normal.
   - **Models** (`tools/author_lagoon_rocks.py`, `ArtSource/lagoon/lagoon_rocks.blend`): 16
     stones in 5 families (`ROCK_FAMILIES`: boulder 0-4, stack 5-7, slab 8-10, split 11-12,
     cobble 13-15). The first kit was too soft (soap bars); after the owner's rock-pack
     reference every stone is cut by 5 to 8 broad planes (near-flat top, near-vertical sides)
     with 2 or 3 chips off the top edges, narrow 4 cm bevels at the breaks, soft shading inside
     a plane; two stacks are 2 to 3 lump spires. Origin at the GROUND CONTACT (base centre, 20 %
     below z = 0). Each mesh: "UVMap" (world box projection, 1 unit = 4 m), "UVBake" (checked:
     0 overlapping pixels), `rock_top` attribute, `rock_top_z` and `rock_family` properties.
   - **In the cove since v17/v18**: `place_boulders` picks by family mix (`MASSIF_MIX`,
     `RIM_MIX`, `SHORE_MIX`), seats each stone halfway between the ground at its centre and the
     lowest ground under it (v17 seated on the lowest point: stones sank downhill and bared the
     fill as smooth brown cones), shrinks stacks in the pile to short spires (at full scale they
     were 20 to 30 m chimneys), and uses the lumpy spires, not the plain monolith (a chimney),
     as the surf, skyline and sea-stack features. Every build bakes the edge atlas and sets the
     rock material (rock_a + edges). Bake radius 0.24: 0.14 was too faint on the final kit,
     whose own bevels soften every break (`rock_on_models_v7.png`).
3. **Stilt house kit**: nipa/cogon thatch (its own texture, swatch first), sawali wall panel
   (swatch first), plank deck, bamboo piles with X bracing, ladders, railings; variants: land
   house on a pocket, small Bajau water home, sari-sari stall, capilla.
   IN PROGRESS 2026-09-26. ✅ Rocks approved first (owner: *"looks good. lets proceed to the
   next thing to texture"*). Models: `tools/author_lagoon_houses.py` (land, water, stall,
   capilla; material slots thatch, sawali, timber, plank, bamboo, tin, paint_white, capiz; UVs
   world-scale, thatch V UP the slope). Swatches in `tools/author_lagoon_textures.py`, each its
   own drawing, 2 m a tile, awaiting on-model renders and the owner's pick:
   - thatch a/b/c (`thatch_swatches_v3.png`): courses of vertical bundles with ragged pointed
     tips over the course below; golden cogon, weathered nipa, chunkier golden. v1: dark gaps
     read as holes, too contrasty; v2: a seam, the bottom course's tips drawn the wrong way.
   - sawali a/b/c (`sawali_swatches_v2.png`): twill, herringbone, checker; 10 cm strips (6 cm
     read as tweed from a distance), each run shaded as one 2-cell run.
   - bamboo a/b/c (`bamboo_swatches_v2.png`): straw, weathered, young green; node ridges at
     irregular spacing, broad tone bands (fine streaks read as wood grain).
   - plank a/b/c (`plank_swatches_v2.png`): warm brown, sun-greyed, wide rough boards; seams,
     staggered butt joints, a cel-lit edge band, no grain.
   - tin a/b/c (`tin_swatches_v2.png`): teal-green, red oxide, bare weathered; corrugations
     down the slope as cel bands, sheet laps, small soft half-strength rust (full-strength rust
     read as camouflage).
   - timber a/b/c (`timber_swatches_v1.png`): warm dark, sun-greyed, oiled red-brown; a
     hand-hewn face of long soft adze facets, no seams, no grain.
   In the cove since v28: `place_house()` builds a few seeds per kind once into a hidden
   "House kit (source, not placed)" collection and places LINKED DUPLICATES (editable, shared
   meshes); water homes stand at z = WATER (the kit lifts the floor and sinks the piles); the
   capilla on the summit ledge and a sari-sari stall at the court's east edge. On-model review
   sheet for the thatch: `thatch_on_models_v2.png` (v1's close-up camera sat inside the stall
   roof).
   - ✅ **THATCH: ALL THREE, FOR VARIETY** (owner: *"honestly keep all for variety"*).
     `thatch_variety()` gives each placed house and boat ONE of thatch_a/b/c for its whole roof,
     per house (object-level material slot, so the shared kit meshes stay untouched).
   - ⚠️ **EVERY SURFACE HAS DEPTH AND NORMAL MAPS** (owner: *"make sure it has depth/normal
     maps"*). Each texture ships `_albedo`, `_height` and `_normal`; the UV material drives its
     Displacement output (bump mode) from the height map on top of the normal map (Unity: the
     height as the height/parallax map). Timber and bamboo normals were near flat and were
     strengthened (normal strength 6 and 4).
   - ✅ **SAWALI: ALL THREE** (owner: *"just use all 3"*), mixed per house by `surface_variety()`
     like the thatch (`sawali_on_models_v2.png`). v1 on the
     models: walls under the deep eaves went cold blue-grey. The cove's LIGHTING fill is now a
     warmer grey (0.55, 0.60, 0.66) instead of saturated blue (the camera still sees the blue
     sky gradient), and the skin and inner strips are closer in value.
   - ✅ **PLANK: plank_c** (owner: *"plank c looks better"*, the wide rough boards; `CHOSEN` in
     the cove script) on decks, steps, walks and boats (`plank_on_models_v2.png`). v1: the
     water-village walks still wore the blockout "deck" colour; they now take the plank
     material with `box_uvs()`, boards laid ACROSS the walk like a real footbridge.
   - ⚠️ **WALKS ARE CONTINUOUS DECKS** (owner: *"z fighting and disconnected planks"*, then *"fix
     the textures"*). `walk_path()` builds a whole walk as ONE mitred strip; branches sit 3 cm
     under the walk they join; UVs are laid per straight run (spreading them along the
     centreline sheared the boards into chevrons at bends); the walks wear `plank_c_walk`, plank_c
     without mid-board joints (boards across a 1.4 m walk are one piece). Boats keep 6 m apart
     and 3 m clear of every walk (they overlapped, and sat under spur walks).
   - ⚠️ **ROCK ANTI-TILING** (owner: *"add some feathering and rotation offsets to the tiles in
     this rock texture? i remember we used some form of that in the kanto map"*): Kanto's method,
     a second sample of rock_a rotated in 3D, scaled 0.61, blended through a big feathered noise
     mask.
   - ⚠️ **NO PLANTS INSIDE ROCK** (owner: *"a bunch of assets inside this big rock and other rocks
     that are completely not visible or clipping"*). `cull_buried()` ray-tests every plant against
     the rocks: a buried plant is LIFTED onto the rock surface above when it is fairly flat
     (normal z > 0.75), otherwise removed (277 lifted, 186 removed; deleting all buried plants
     bared the massif). Refresh world matrices first: the first run saw everything at the origin.
   - ⚠️ **STAIRS ARE WOODEN PLANK FLIGHTS** (owner: *"you said the stairs will actually be
     planks"*, and *"need you to refer back to the references incase you forgot the style"*: the
     ArtStation GvJv5a ledges are joined by plank flights with railings). `plank_stairs()`:
     timber stringers, plank_c treads, posts and handrails both sides, legs to the ground;
     `clear_stair_paths()` removes small stones standing in a flight. The grey stone blocks are
     gone. **Re-read § 1 before building anything new.**
     Steps are always real size (0.2 m rise, 0.3 m going); a shallow slope becomes flights of
     at most 8 steps with FLAT landings between (owner: *"some stair steps are too long. if the
     slope is too low, just do smth like a set of stairs then a flat walkway, then a set of
     stairs again"*); `stair_profile()`.
   - ⚠️ **TERRAIN TEXTURED** (owner: *"the sand, grass and other terrain is still
     untextured"*): sand_a (soft ripples, tiny pebbles), grass_a (feathered clumps, drawn tuft
     marks), earth_a (the court: packed earth, small embedded stones), 4 m tiles projected
     top-down with Kanto's anti-tiling; rock fill is rock_a darkened; the seabed is sand_a tinted
     wet and teal. The field masks and soft painted edges are unchanged.
   - Fence posts (*"untextured and some are floating"*): timber, run 0.6 m into the ground.
   - Boats and other flat surfaces (*"boat bodies are untextured"*): `tools/lagoon_paint_materials.py`
     paints hull_paint (strakes, worn patches), trim_paint (neutral, multiplied by each boat's
     `trim_tint` vertex colour), lime_plaster (capilla), capiz (pearly shell panes in a lattice,
     a faint self-glow for translucency) and cloth (laundry, per-piece pastel `cloth_tint`).
   - ⚠️ **THE OWNER'S HAND EDITS ARE RECORDED IN THE SCRIPT** (*"made a few deletions myself"*,
     *"made some more tweaks again"*): the build regenerates the .blend, so `OWNER_DELETE` and
     `OWNER_MOVE` in author_lagoon_cove.py hold his edits by built position (diffed from his
     saved file) and `apply_owner_edits()` replays them at the end of every build. When he edits
     the .blend again, diff it against the last committed build and add rows BEFORE rebuilding.
5. **Plants (done early, 2026-09-27)**: owner: *"start texturing the plants"*, *"i really like
   what was used for leaves in kanto. just need to ensure it matches this environment"*.
   Kanto's leaf method kept exactly (one soft-painted greyscale card, alpha silhouette, two tints
   a plant, per plant never per leaf, shingled clumps), with tropical SHAPES in
   `tools/author_lagoon_leaves.py`: round leaf, coconut frond, banana paddle, grass blade;
   sunnier tints. `tools/lagoon_cove_planting.py` builds every plant from cards. Then the owner's
   close-ups (*"stem part of this leafy plant is untextured"*, *"is the pink stuff supposed to
   look like this or did u forget to texture"*, *"coconut and trunks of palm tree are
   untextured"*, *"leaves of palmtrees are also cut off"*): painted palm trunk with leaf-scar
   rings, coconuts, banana pseudo-stem and stalks, gumamela and bougainvillea flower cards, and
   fronds tapering to a point inside the card. Then (*"the last types of foliage and fauna added
   dont have much textures on them"*, circling a grass tuft and the taro stalks): the blade and
   stalk drawings were near-flat ramps (value std 17 of 255). Repainted in the same hand with
   marks strong enough to survive the tint (std about 46): the blade has a crease with a pale
   ridge, a lighter lit half, two vein bands, a darker margin and foot; the stalk has a pale
   channel edged by two dark lines, a shaded back, four soft lengthwise stripes, a pale collar at
   the leaf and a darker foot, plus a height and normal map, on 8-sided tubes (was 6).
   `plants_blade_stalk_beforeafter_v1.png`. Rule learnt: a leaf drawing's marks need about
   twice the contrast of a wall texture's, because the x 1.25 tint and bright sun flatten them.
4. **Boats**: bangka outrigger and lepa houseboat. MODELS DONE 2026-09-27:
   `tools/author_lagoon_boats.py` (bangka, lepa, bangka_beached; slots plank, paint_hull,
   paint_trim with a per-boat `trim_tint` vertex colour, bamboo, thatch, timber; origin the
   waterline at midships, bow +Y). In the cove since v29 as linked duplicates; moored boats sit
   3.2 m off the walk (the bangka is ~4.5 m across its floats).
5. **Props**: drying nets, laundry lines, crates, barrels, baskets, lanterns, fish racks.
6. **Foliage**: coconut palms (curved leaning trunks), broad leaves, flowering accents, grass
   tufts.
7. **Water and light**: rewrite `Resources/Shaders/LagoonWater.shader` (URP) after the ANGRY MESH
   reference (§ 1): depth-based colour and transparency from the camera depth texture, the
   seabed visible through the shallows, ANIMATED caustic lines, MOVING shore foam where the
   water meets sand, rock, stilts and hulls (depth intersection), gentle waves. Then warm sun,
   big painted clouds.
8. **Assemble** `ArtSource/lagoon/lagoon_city.blend` with linked duplicates (editable, as Kanto),
   full review, owner approval, THEN export to Unity and rebuild `Lagoon.unity` (§ 7, checks, a
   played match).

Split independent kit pieces across parallel agents (owner standing preference), each in its own
new file, integrated and verified by the lead session. Commit locally; never push until the owner
has played it.
