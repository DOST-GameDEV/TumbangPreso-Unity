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

### Step 3: Render Turnarounds & Animation Verification Sheets

#### A. Single Character Turnaround & 32 Animation Suite (`PersonSwapProbe`)
Use this to render the active master character across all 4 angles (`Front`, `3/4 View`, `Side`, `Back`) and verify all 32 animations:
```powershell
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.PersonSwapProbe.Run -logFile Logs/swap.log" -Wait
```
*Outputs Generated in `Logs/`:*
- `Logs/person-swap-turnaround.png` (4-angle full body turnaround)
- `Logs/person-swap-probe.png` (32 animation clips test sheet)

#### B. Side-by-Side Comparison Lineups (`IterationTurnaroundProbe`)
Use this when presenting multiple design variants (hair volume, hats, skin tones) side-by-side:
```powershell
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.IterationTurnaroundProbe.Run -logFile Logs/iterations.log" -Wait
```

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
*Output Target:* `C:\Users\matth\Desktop\TumbangPreso-Unity\TumbangPreso.exe`
