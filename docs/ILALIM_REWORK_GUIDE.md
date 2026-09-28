# Ilalim ng Tulay rework guide (ILALIM-1)

⚠️⚠️ **CURRENT STATE (2026-09-29): NOT STARTED.** The owner chose Ilalim ng Tulay as the next map
after Kanto and the Lagoon Court ("Ilalim ng Tulay", 2026-09-29). This guide is the starting
point: what the rebuild must keep, the pipeline Kanto and the Lagoon Cove proved, and every trap
those two maps hit. Update this block as the work moves; it is the snapshot a new session
resumes from.

Read first, in order: [AGENTS](../AGENTS.md), [VISION](VISION.md), [WORKING_RULES](WORKING_RULES.md),
this guide, then the map's existing design document [Ilalim_Ng_Tulay.md](Ilalim_Ng_Tulay.md)
(§ 0, § 1, § 3, § 4 and § 10.2 at least: they are the gameplay contract). The two finished
Blender maps are the worked examples: [KANTO_DESIGN_GUIDE.md](KANTO_DESIGN_GUIDE.md) (city
style, textures, signs, traffic, sound) and [LAGOON_REWORK_GUIDE.md](LAGOON_REWORK_GUIDE.md)
(organic terrain, prop kits, water, life, the Unity look).

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

1. **Research and blockout.** Real references first (Gilmore/Aurora Boulevard under the LRT-2
   guideway: the soffit, the column rhythm, the shopfronts, wires, jeepneys, the pisonet), then a
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
