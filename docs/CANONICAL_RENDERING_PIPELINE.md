# Canonical In-Engine Rendering & Build Pipeline

> **MANDATE FOR ALL AGENTS**:  
> This document defines the **ONLY authorized method** for rendering, previewing, verifying, and building character models and scenes in *TumbangPreso-Unity*.  
> **DO NOT** use software OpenGL previews, external renderers, or Godot tools. All rendering must go through Unity's native batchmode probe pipeline.

---

## 1. Why In-Engine Rendering is the Only Valid Method

1. **Exact Toon Shading & Outlines**: Characters require the custom `TumbangPreso/Toon` shader, `ToonSkin.PersonOutlineWidth` ink border, and two-band cel-shading.
2. **Linear Color Space Accuracy**: Unity converts palette sRGB values to linear space (`c.linear`). External scripts bypass this, leading to washed-out or inaccurate colors.
3. **Rig & 32 Animation Validation**: `PersonSwapProbe` validates that the mesh bounds and all 32 retargeted animation clips execute with 0 warnings/errors.

---

## 2. The 4-Step Standard Rendering Workflow

### Step 1: Generate Mesh & Palette Asset
```powershell
# Build canonical in-game model:
python tools/build_person_voxel.py

# Or build multi-model comparison candidates:
python tools/build_iterations.py
```
*Outputs:* `Assets/TumbangPreso/Art/characters/persons/<character>.glb` and `MapSource/materials_persons/person_<character>.tres`.

---

### Step 2: Ingest Material Palette into Unity RosterBook
Whenever a `.tres` material palette is created or modified, it **must** be baked into Unity's `RosterBook.asset`:
```powershell
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.RosterBookBuilder.Build -logFile Logs/roster.log" -Wait
```

---

### Step 3: Render Turnarounds & Cast Lineup (`PersonSwapProbe`)

The canonical rendering norm in this repository produces **two standard visual outputs**:
1. **4-Angle Turnaround** (`Logs/person-swap-turnaround.png`): Front, 3/4, Side, and Back views of the character.
2. **Cast Lineup** (`Logs/cast_lineup.png`): Side-by-side roster lineup showing the character relative to the rest of the cast.

> [!IMPORTANT]
> **NO ORBIT RENDERS**: Orbit angle probes (`InGameAngleProbe` / `8angle_orbit`) are deprecated and removed. All model inspections and review audits must use the **4-angle turnaround** and/or **cast lineup** only.

Execute the canonical probe pipeline in Unity batchmode:
```powershell
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.PersonSwapProbe.Run -logFile Logs/swap.log" -Wait
```
*Outputs Generated in `Logs/`:*
- `Logs/person-swap-turnaround.png` (4-angle full body turnaround)
- `Logs/cast_lineup.png` (full cast comparative lineup)
- `Logs/person-swap-probe.png` (32 animation clips test sheet)

---

### Step 4: Cache-Busting Rule (CRITICAL for AI Agents)

> [!WARNING]
> **ALWAYS USE UNIQUE / VERSIONED FILENAMES FOR NEW RENDERS**  
> Web chat clients cache image URLs based on filename. If an agent outputs a new render overwriting `lineup-front.png`, the user's browser may continue to display the **old cached image from previous turns**, causing confusion.
> 
> **Best Practice**: In `IterationTurnaroundProbe.cs`, append a unique tag (e.g. `Logs/zack_hair_lineup_v1.png`, `Logs/zack_hair_lineup_v2.png`) or copy to the artifact directory under a fresh versioned name.

---

## 3. Standalone Windows Game Build to Desktop

To compile and package the playable standalone Windows game directly to the Desktop:

```powershell
# 1. Set scene build order (Ensures SplashScreen.unity is Scene 0):
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.MenuSceneBuilder.BuildAll -logFile Logs/menuscene.log" -Wait

# 2. Compile standalone Windows player to Desktop:
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows -logFile Logs/build.log" -Wait
```
*Output Target:* `<Desktop>\TumbangPreso-Unity\TumbangPreso.exe`

⚠️ **The desktop is RESOLVED, not typed.** `GameBuilder` calls
`Environment.GetFolderPath(SpecialFolder.DesktopDirectory)`, so the player lands on whichever
machine's desktop ran the build. This line used to read `C:\Users\matth\Desktop\...`, which is
one particular checkout and sent a session looking for a build on a path that does not exist
here. Two other documents quoted the same stale user directory and both were deleted on
2026-08-26; `docs/README.md` records why.

---

## 4. Character-Specific Pipeline Log & Known Pitfalls (Nemu & Companions)

### Pitfall 1: Unity AssetDatabase Caching on Companion Prefabs / Sub-Assets
- **Symptom**: Rebuilding a companion pet or accessory (e.g. `pet-nemu-ghost.glb`) via Python modifies the file on disk, but batchmode Unity renders still show the old orientation or outdated geometry.
- **Root Cause**: `PersonSwapProbe.Run()` was only calling `AssetDatabase.ImportAsset(NewModel)` (reimporting `team-nemu.glb`), while `pet-nemu-ghost.glb` remained cached in Unity's internal asset memory.
- **Solution**: Explicitly call `AssetDatabase.ImportAsset("Assets/.../pets/pet-nemu-ghost.glb", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport)` before capturing turnarounds and lineup renders.

### Pitfall 2: Web UI Image URL Cache Collisions
- **Symptom**: Agent regenerates an updated render on disk, but the user's chat interface continues to display the old cached image from earlier in the session.
- **Root Cause**: Web browsers cache image URLs by filename (`reference_comparison.png`, `turnaround_before_after.png`). Overwriting the file on disk does not invalidate browser-side HTTP/file caches.
- **Solution**: Every new visual comparison presented to the user must use a unique timestamped or versioned filename (e.g. `ref_comparison_v1787314037.png`, `turnaround_comparison_v1787314037.png`).

### Pitfall 3: Companion Pet Coordinate System & Local Rotation
- **Symptom**: The companion pet floats with its face pointing backwards away from the camera in Front view, while showing its eyes in Back view.
- **Root Cause**: In `pet-nemu-ghost.glb`, the mesh was authored facing `-Z` (standard glTF rig convention). When parented to `model.transform` (which has `PersonModelYaw = 180°` applied), a `localRotation` of `Quaternion.identity` points the pet's `-Z` toward the model's `-Z` (Nemu's back).
- **Solution**: Set `pet.transform.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f)` when parented to `model.transform`, aligning the pet's face (+Z model space) with Nemu's forward gaze across all turnaround angles.

### Pitfall 4: Flared Bell Sleeve Z-Depth Causing Hand Anchor Failure
- **Symptom**: `PersonSwapProbe` fails `CheckHandAnchor` with `"FAIL: the shoe would be buried in the hand or floating above it"`.
- **Root Cause**: `PalmCentre` dynamically picks the limb axis using `max(size.x, size.y, size.z)`. Deep flared sleeve cuffs with large Z-depth caused `size.z` to exceed `size.x`. The probe then assumed the arm was pointing along Z (forward), taking the front rim of the sleeve cuff as the "palm" and computing an incorrect anchor offset.
- **Solution**: Author the arm boxes such that the horizontal reach span along X strictly exceeds the cuff depth along Z (`size.x > size.z`), ensuring `PalmCentre` correctly identifies the X axis and measures the true palm vertices.

### Pitfall 5: Inverted-Hull Toon Outline Occluding Sub-Centimeter Decals
- **Symptom**: Tiny decal boxes (e.g. pet eyes, blush, teeth) vanish or appear completely black/dark purple in Unity renders.
- **Root Cause**: `ToonSkin.Apply` extrudes geometry along vertex normals by `PersonOutlineWidth` (~8 to 12 mm). If decal boxes are only 2 to 4 mm proud of the underlying body box, the extruded outline hull completely envelopes the decal.
- **Solution**: Voxel decal boxes on small meshes must be authored at least 8 to 12 mm proud of the base geometry to remain crisp and visible above the toon ink outline.

