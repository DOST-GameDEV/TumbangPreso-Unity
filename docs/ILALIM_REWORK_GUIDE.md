# Ilalim ng Tulay rework guide (ILALIM-1)

⚠️⚠️ **CURRENT STATE (2026-09-29): ILALIM-1.1 PROPOSAL WRITTEN, AWAITING OWNER REVIEW.** The
owner chose Ilalim ng Tulay as the next map after Kanto and the Lagoon Court, and set it at UP
Manila's Padre Faura corner "cuz we wanna see our school's Rizal Hall in the game". § 0 is the
proposed place and feel, with the open decisions in § 0.6. The evidence is in
[the research report](reports/ilalim-rework-2026-09-29/research.md), and the plan is in
[place-plan-v1.png](reports/ilalim-rework-2026-09-29/place-plan-v1.png).
NEXT: the owner answers § 0.6, then ILALIM-1.2 (the grey Blender blockout). Nothing is modelled
yet. Update this block as the work moves; it is the snapshot a new session resumes from.

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
