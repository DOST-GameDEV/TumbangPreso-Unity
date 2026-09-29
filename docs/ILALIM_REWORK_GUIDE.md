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
  - **Build order:** eastside, then rooftops and streetlife, then the city.
- **OPEN owner decisions** (details under "Open decisions for the owner" below):
  - the train is invisible behind the solid parapet; a steel-railing parapet is recommended;
  - cable shadows on the court;
  - how big Rizal Hall reads from the court;
  - names.
- **Then ILALIM-1.4:** export and a Unity builder into an unregistered sample scene. The grime UV
  channels, emissive and cutout materials, the train prefab, the collider resizes and the
  triangle budget are all listed below.

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
