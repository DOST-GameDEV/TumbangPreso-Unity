# Eskinita rebuild

Started 2026-10-08. **STATE (2026-10-09, evening): THIS IS ESKINITA NOW.** At the owner's word ("push this to the map
pool, replace the old eskinita") `EskinitaAlleySceneBuilder` writes the stepped alley OVER the shipped
`Assets/TumbangPreso/Scenes/Maps/Eskinita.unity` (same file and GUID, map index 0, same id on the wire). The old scene is
`Scenes/Vault/EskinitaClassic_2026-10-09.unity` and in git. The look row named "Eskinita" is the alley's (the old one is
"EskinitaClassic", code and asset). Protocol 158. The map card is the new arch picture. The art folder is still
`Art/EskinitaAlley`. NOTHING IS COMMITTED.

⚠️ WHAT THE SWAP LEAVES UNDONE (tell the owner again):
- About 174 test files name Eskinita and NONE has been run against the new map (the editor is open, so no batch run).
  Known to break by reading: `NearFadeTests` (12 `Poste_` nodes), `MapGeometryCheck` (Eskinita is Gated), anything
  that stands a body at a typed point off the middle of the old 16 by 35 m flat street: past z +/-8.5 the alley is
  only 10 m wide, so x beyond +/-5 there is inside a house.
- The guided tutorial and the training range start on Eskinita: unchecked on the new map.
- The old map's editor passes (`Eskinita*Author.cs`, `MapFinalPassAuthor`, the cat, dog and bird habitat authors) still
  have menu items that would write old dressing into the new scene. DO NOT RUN THEM; retire them when the tests move.
- `FiestaBunting` still runs on a scene named Eskinita and finds no `PosteRework_` support: it builds nothing (play
  probe 10 logged no error).
- Play probe 10 on the map under its shipped name: a match starts, 0 errors, all stairs and bridges walk, all three
  tarps land a steered body (they were moved against their targets after the movement rework cut air drift to about
  a metre). Its one failing line is a jump-down test at a place that is a stair now.

## The pipeline (run in this order after any change)

1. Textures: `py -3 tools/author_eskinita_textures.py [--sheet N]` writes `Art/EskinitaAlley/Textures/` (copies the
   approved Kanto, Lagoon and Ilalim drawings where the surface is the same; paints the rest). Prop textures:
   `py -3 tools/author_eskinita_props_textures.py`.
2. Props kit: `blender -b --python tools/author_eskinita_props.py -- --sheet N` writes `ArtSource/eskinita/props.blend`,
   `Art/EskinitaAlley/Models/prop_*.glb`, `materials_props.json`, `props_manifest.json`.
3. The alley: `blender -b --python tools/author_eskinita_alley.py -- [--review N] [--cams a,b] [--no-export]` writes
   `ArtSource/eskinita/eskinita_alley.blend`, the architecture as a few big `.glb` pieces, `collision.glb` (plain boxes
   and ramps: THE WHOLE of the map's collision; the art carries none), `pad_trapal.glb` and
   `eskinita_alley_layout.json` (Unity axes: Blender (x, y, z) is Unity (-x, z, -y), yaw t is -t). Props are PLACED in
   its `PLACE` table by name; solid ones get a box from its `SOLID` table.
4. Unity, WITH THE EDITOR OPEN: `bash C:/Users/StarX/AppData/Local/Temp/esk_build.sh` (it writes `build review` into
   `Temp/eskinita-alley.request`; `EskinitaAlleyRequestWatcher` refreshes assets, runs
   `EskinitaAlleySceneBuilder.Build` and `.Review`, answers in `Temp/eskinita-alley.done`, and reopens the scenes that
   were open; it refuses if an open scene is dirty). Pictures: `Logs/eskinita/unity/vN/`. Menu: Tumbang Preso > Sample
   Map > Build Eskinita Alley.

The look is its own `WorldLookProfile` row, "EskinitaAlley" (code defaults and the asset): a golden sun 32 degrees up
raking up the alley, lilac shade, warm haze from 60 m. Opaque materials are on `TumbangPreso/NearFade` (so Paete's sky
window opens through the walls), cut-outs on `KantoFoliage`. NOT registered in any map list; no protocol change.

## What was built, and what changed from the grey blockout

- THE TWO END TERRACES ARE 10 M WIDE, the can's terrace 16 m. Built at 16 m throughout it read as a plaza, not an alley;
  only the middle has to hold the 14 m box. So it is an alley that opens into one small court and closes again, and
  each plank bridge spans 10 m, not 18.
- Every house is solid back to the outer bound (x 13.6): the blockout left a slot behind the shallow ones.
- NO LADDERS. The game has no climbing (a CharacterController: step 0.3 m, slope 45 degrees), so every way up is a
  STAIR (collision: a ramp under 40 degrees) or a BOUNCE TARP (a `JumpPad` whose model is the striped trapal sheet
  itself, `pad_trapal`, 2 m square, launch 11 to 11.5). A ladder needs a climbing rule: ask the owner.
- Nothing bounces under a ledge, and no stair or pad is inside the chalk box (the Arena kept 1.6 m clear of pads).
- Ways up: a painted stair and a tarp on each end terrace, a painted stair on each side of the court. Roof to roof:
  eleven timber stairs, a third tarp on a west roof, two plank bridges with a rail. The west roofs do not join between
  the court and the top terrace except by that tarp and by dropping down.
- The alley's identity: concrete steps with every riser painted its own colour, trapal bounce awnings, plank bridges,
  tangled wires, fiesta bunting, two-tone painted walls on a concrete frame, flat tin roofs weighted with tires, a
  barangay arch at the low end and a hoop at the top.

## What is in the kit

- Textures (`textures_manifest.json`): COPIED approved drawings: plaster, tiles_clay, wood (Kanto's timber), paint, leaf,
  bark (Kanto), sawali (Lagoon), jalousie (Ilalim). PAINTED new: chb, tin, tin_rust, planks, floor, concrete, three
  trapal stripes, window_slide, door_wood, gate, grille, chalk, piko. The painter's own weakest list: concrete and
  floor repeat visibly, tin's ribs are uneven, the trapal's sun-fade reads as a smudge. NONE has had the owner's
  swatch review (the house rule is swatches first): show `Logs/eskinita/textures_swatches_v4.png` when he is back.
- Props (39, `props_manifest.json`, sheet `Logs/eskinita/props_sheet_v4.png`): sari-sari front ("Aling Nena's"), the
  barangay arch ("MABUHAY! ESKINITA SAMPAGUITA"), hoop, two postes, bunting, laundry, drums, pails, batya, pots, banana,
  monobloc, bench, dama table, roof tires and blocks, tank, antenna, dish, AC, meters, mailbox, tricycle, kariton,
  crates, sacks, tires, two cats, a rooster's tepee, wall lamp, reminder board, shrine. Weakest by the modeller's own
  word: the banana plant, the sitting cat's legs, the tricycle, the laundry shirt.
- Trees beyond the bounds are Kanto's approved park trees, copied with their materials (no pines).
- 102 materials in all (the props' 29 included). Ilalim's 694 lagged joiners; this is well under, not measured.

## What a play probe proved (2026-10-09, probe 6, `Logs/eskinita/unity/probe_6.txt` and pictures beside it)

`printf probe > Temp/eskinita-alley.request` enters Play in the open editor on this scene, drives a body, and answers in
`Temp/eskinita-alley.done` (`Editor/MapKit/EskinitaAlleyPlayProbe.cs`; it reads `probe_stairs`, `probe_bridges` and
`probe_pad_targets` from the layout). It ran clean: 0 errors logged, 0 failing lines.

- A solo match starts; four bodies stand on collision inside the bounds; the can is at the origin on its floor.
- All 21 stairs were walked up and down and both bridges crossed. All three tarps launch (11 to 12 m/s) and a body
  steered off each one LANDED on the ledge or roof it is for.
- Bots played 25 s: they tagged and moved, none fell or left the bounds, and NONE LEFT THE CAN'S TERRACE (no stair,
  roof or tarp). Whether bots can use the heights at all is unknown.
- The taya tagged 5 times in 25 s: attackers begin on the broad painted flight 9 m from the can in open ground. That
  may be too easy for the taya; it is a gameplay question for the owner.
- 4.9 ms a frame in the unfocused editor on this machine. The probe counted about 5 million triangles in Play, but the
  placed models add up to 408,000 from the files: the difference is not explained. No build, no weaker machine.

Probe 7 (the last build) reports ONE failing line, stair 1, and it is the probe's own: a parked bot stood at the
attackers' mark (0, -0.15, 9.04) on that flight and the test body walked into it; the same stair passed in probe 6 and
walked down in probe 7. In probe 7 one bot did reach the top terrace (feet y 0.97).

Two faults it found in the first build, both fixed: the three flights to the top terrace were buried under its slab
(the terrace is notched now), and the game read the court's floor as the plate under the map and stood one attacker in
a slot between two walls (the can's terrace is height ZERO now, like every other map: the low end is -0.9, the top end
+0.9; and the low end has one broad flight).

## After the owner played it (2026-10-09)

- "on the edges of the bridges there are z-fighting planks": the beam at each bridge end was level with the roof; it
  stands 3 cm proud now.
- "outer houses are too obviously too simple": the row behind each side, the first ring past it and the nearest rows at
  both ends are built by the alley's own `house` (frame, belts, pushed-in windows on front and flanks, a pitched roof or a
  parapet deck with a hut). The rings further out are still painted boxes.
- "the bot ais struggle to get around this map": (1) each rise is ONE flight wall to wall (no ramps, no cheek walls, no
  wall between flights); (2) "give them a route graph": `Runtime/Map/MapRoutes.cs`, a new component the builder puts on
  the scene from the layout's `routes` (231 way points written by `tools/author_eskinita_alley.py`: stair feet and
  heads, bridge ends, ledges, roofs, tarps and their landings, floor points, and the alley-side corners of everything
  against a wall). The builder BAKES which pairs can be walked straight and the shortest way between all of them; 8 stray
  points are shut in and unused, every roof and ledge has a way from the can and back. `AIController.Goto` asks it the
  way (one null test on every other map). (3) The tarps stand 0.28 m off their floor (were 0.75) so a bot, and anyone,
  walks onto one; launch speeds 11.5, 12, 12.5. (4) The two short posts stood through the court's ledges; moved.
  ⚠️ NOT SEEN: a bot actually taking a stair, a bridge or a tarp to a slipper on a roof. The bake says the ways exist;
  nobody has watched one walked. The play probe does not test it yet.
- The cursor: the click that takes the mouse back is no longer swallowed (`PlayerInputReader.cs`).

## Life, sound and the later play fixes (2026-10-09, all at the owner's word while he played)

- NEAR FADE IS SHUT on this map's materials ("distance fade effect is now being applied to everything"): start 0.001,
  so only Paete's sky window opens the walls.
- Geometry faults he found, all fixed in `tools/author_eskinita_alley.py`: knee braces under the bridges removed; wall
  fittings slide off doors and windows (`clear_of_openings`); the top flight's houses have footings; EVERY BOX HAS ITS
  OWN CORNERS (boxes were welded to their neighbours before the bevel, which skewed stacked steps); roofs, posts and
  belts stand 2 cm past a house's flank; frame pieces stand 1 cm into their openings; the two court stairs stand 1.5 m
  clear of the house behind their foot (his "no gap" meant there was no room to get on; I first read it backwards).
- Bounce tarps: the frames and chevrons are the Ilalim pad's own meshes and paint (a combined texture,
  `tools/author_eskinita_pad_paint.py`); the launch sound is the Arena pad's (`JumpPad.ArenaSound`).
- Bots: `MapRoutes` accepts a goal a body cannot stand on (0.75 m slack) and a start in a tight corner, and falls back
  to the nearest way point on the goal's level. A bot with no route writes why to `Logs/eskinita/routes_trace.txt`
  (editor only). STILL NOT WATCHED WORKING.
- LIFE (other agents' work, checked by me on stepped stills only): `AlleyChickens` (8 birds, burst into feathers on a
  thrown slipper and walk back in), `AlleyPets` (two dogs, three cats, a chase, swat or chicken-scatter event every 75
  to 160 s), `AlleyStreet` with `SidewalkLife` (a tricycle or jeepney about every half minute, a taho vendor, three
  children at tag, passers-by), `AlleyLifeSound` (animal voices) and `AlleySoundscape` (five beds, nine one-shots), all
  REAL CC0 Freesound recordings listed in `tools/eskinita_life_sfx_sources.json`. Authors:
  `EskinitaAlleyLifeAuthor.cs`, `EskinitaAlleyPetsAuthor.cs`, `EskinitaAlleyStreetAuthor.cs`. Motion sheets:
  `Temp/eskinita-chickens.request` ("N", "N pets") and `Temp/eskinita-street.request`. NOBODY HAS HEARD ANY OF IT.
  No radio or karaoke one-shot (it could not be checked for a recognisable song). Chases stay on one end terrace:
  the court's side strips are full of props and stairs.
- The road outside is drawn (asphalt, centre line, kerbs, sidewalks, an apron through the arch).

## Not seen, not done, to tell the owner

- The tarp's striped sheet is the JumpPad's own model, so it shows only in Play: in the editor view the tarp frames
  stand empty.
- Hung lines (bunting, laundry, wires) have no collision. If Paete's vine needs something to catch, that is a rule to
  agree with the owner.
- The spawn ring, the can, the taya's box and slipper retrieval on a stepped floor: see the play probe's report
  (`Logs/eskinita/unity/probe_N.txt`) once it has run; anything not in it is unverified.
- Not registered as a map, no map card, no ambient sound or life (cats and the rooster are statues), no LODs, no
  occlusion bake, no test moved. Replacing map index 0 with it is the owner's call and touches about 200 tests.

## What the owner asked for (verbatim)

- *"now we're going to pick up map designs again. this time we're re-working eskinita."* with *"arena and lagoon as a
  baseline for an ideal map art style"*.
- Asked rebuild or restyle: **"rebuild from scratch"**.
- The footprint: *"eskinita is a small/tight map to reflect the concept (alleyway)"*. Keep it tight.
- Solid, climbable houses: *"i like that idea that its a multi level / varied height map"*.
- *"just search up eskinita for reference, i guess you could have climbable ladders or just jump pads to get up. not much
  on the ideas for it but want something unique, maintaining the same fun illustrated stylized look"*.
- Of three layouts described in words: *"give me a block layout for all 3"*; of the three blockouts: **"go with 2, fix
  what you see wrong"**; of the second cut: *"the planks are floating with no end. continue polishing it in blender."*

## The grey blockout that was chosen: 2, THE STEPPED ALLEY (history; the numbers above supersede it)

`tools/author_eskinita_blockout.py` (`layout_2`; 1 and 3 are the refused ones, kept). Run:

    blender -b --python tools/author_eskinita_blockout.py -- --layout=2 --out=<abs>/Logs/eskinita-blockout

It writes six pictures (air, top, and four from a player's eye at 1.25 m and 95 degrees); the newest sheet is
`Logs/eskinita-blockout/eskinita_L2_v3_sheet.png`. Blender x is across the alley, y along it (Unity's z), z up.

- Footprint as today: the alley floor is 16 by 35 m (wall faces x 8, y 17.5). Houses stand OUTSIDE that on both sides
  (4 to 5.6 m deep) and are solid; an outer bound is at x 13.6.
- Three terraces, 0.9 m apart (`STEP`): the south end at 0, the can's terrace (17 m long, `MID` 8.5) at 0.9, the north
  end at 1.8. The 14 m chalk box and 1.5 m round it are on the one flat middle floor.
- Each rise has a broad flight, a narrow flight, a low wall beside each, and a narrow ramp along one wall. The two rises
  are not mirrors of each other.
- Seven houses a side, low roofs (about 2.5 m over their own floor) and high (about 5 m) in an uneven order. Ledges at
  the low roofs' height join them into one walk along each side; one stair a side goes from a low roof to a high one.
- A way up on every terrace: ladders (yellow), stairs, and BOUNCE TARPS (teal; the alley's jump pad, an awning).
- Two plank bridges at high-roof height, each over a rise and OUTSIDE the chalk box, each resting a metre onto a high
  roof at both ends on a beam, with a rail. Four clotheslines strung between poles (for Paete's vine), none over the can.

## What is doubted or unknown (say these to the owner again before art)

- The game puts attackers on a 9 m ring round the can (computed from the box, not from markers): on this layout that
  ring lands on the stairs and the other terraces. How spawning behaves on a step is unchecked.
- The can, the taya's box and slipper retrieval have never run on a map with height. Test in play with grey boxes
  BEFORE any art.
- Throws from the top terrace's high roofs (about 6.7 m up) down the whole alley may be too strong.
- The reference search found almost nothing usable; the layout is from general knowledge of Manila alleys.

## The plan from here

1. Finish polishing the blockout in Blender (he asked for this): look at every view for anything floating, unreachable
   or mirrored; check every roof can be reached and left.
2. Proposed, NOT yet agreed: put the blockout in the game as a throwaway grey test scene with collision only, so he can
   run, climb and throw on it before art.
3. Then art to the baseline: Blender-authored real models, one-piece shells with live bevels, hand-painted flat
   textures (one per surface), a `WorldLookProfile` row of its own (warm late afternoon, terracotta and tin roofs, cream
   and painted walls: his earlier asks for this map), swatches first and shown from the player's eye, three
   render-critique-fix rounds. The Lagoon and Arena pipelines are the model (`docs/LAGOON_REWORK_GUIDE.md`,
   `docs/ARENA_HANDOFF.md`, `docs/KANTO_DESIGN_GUIDE.md`).

## Constraints found in the docs

- Eskinita is map index 0 and the default stage for training, the dedicated server and about 200 tests
  (`Assets/TumbangPreso/Scenes/Maps/Eskinita.unity`). Replace the scene IN PLACE to keep its GUID and index; vault the
  old one. `NearFadeTests` asserts 12 `Poste_` nodes; `EnvColourPass` needs a child named exactly `Dressing`.
- Today's Eskinita is a Godot-era import (`MapSource/Eskinita.tscn` through `Editor/MapKit/TscnImporter.cs`) plus about
  fifteen finish passes (`MapFinalPassAuthor`, the `Eskinita*Author` files). The rebuild retires them; do not delete
  them until the new scene is in and the tests are moved.
- Hard bound from the rules: `ConfinementRadius + ThrowStandoff (1.2) + capsule <= wall face` (`docs/Design.md`).
- Phones and joining players: keep materials few (Ilalim's 694 materials lagged joiners); every registered map is
  previewed and cached in the menus.
- Paete's cutscene opens the sky through anything on `TumbangPreso/NearFade`: put the new map's walls and roofs on the
  map shaders that carry that rule, or she is hidden in the alley.
