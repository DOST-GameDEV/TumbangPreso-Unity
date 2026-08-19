# Character Pipeline Handoff: Zack Hair Sculpt & Electricity Theme

## 📌 Context & Current Git State
- **Repository**: `C:\Users\matth\Documents\GitHub\TumbangPreso-Unity`
- **Active Branch**: `models/team-inday`
- **Latest Commit**: `a045ae6f` (`Polish Zack anime hair spikes with 45-degree flare and gold lightning insignia`)
- **Inday / Cheska Status**: **COMPLETED & LOCKED IN** (Volumetric Cyan Ushanka + Ski Goggles + Fair Peach skin `#f5b894`).
- **Zack Non-Hair Status**: **LOCKED IN** (Warm golden skin tone, purple open blazer with gold lapel pin, crystal pendant necklace, black undershirt, cargo pants, gold belt buckle with wallet chain, purple skate sneakers). **DO NOT TOUCH FACE, BODY, OR OUTFIT TABLES.**

---

## ⚡ The Character: Zack (Electric / Cyberpunk Specialist)
- **Archetype**: Electric / Lightning cyberpunk voxel anime character.
- **Hair Style Goal**: Chunky, high-volume spiky anime hair with vibrant neon magenta / hot pink crest spikes (`#ff2a8d`) and midnight black undercut/side spikes (`#14121a`).
- **Reference Images**: `media_1787150257691.png` and `media_1787149961710.png`.

---

## 💡 Key Lessons Learned & Pitfalls from Previous Iterations
1. **Avoid the "Afro Dome"**: Do NOT create a single massive continuous rounded box over the whole skull. It reads like a round afro/helmet.
2. **Avoid "Crab Legs / Antennae"**: Do NOT stick thin, isolated pipe-boxes out into empty air. Spikes must be chunky, stepped overlapping pyramid/wedge volumes that flare diagonally outward at ~45°.
3. **Avoid "Drooping Face Slabs"**: The front fringe must NOT hang down over the cheek or eye. It must stay cleanly above the brow.
4. **Avoid "High Bald Forehead"**: The hairline must sit low on the forehead, directly above the eyebrows (`y ≈ 0.515 - 0.530`) to preserve compact chibi/anime face proportions.
5. **Electric Lightning Motifs (⚡)**: Clean, crisp 3-stroke stylized gold lightning bolt geometry (`⚡`) on the back of the jacket and nape, NOT random scattered pixel noise.

---

## 📂 Archived Iterations
Saved for reference in `tools/saved_iterations/`:
- `tools/saved_iterations/build_zack_iteration1_quiff.py` / `team-zack-iteration1-quiff.glb`
- `tools/saved_iterations/build_zack_iteration2_spiky.py` / `team-zack-iteration2-spiky.glb`

---

## 🛠️ Canonical 3-Step In-Engine Rendering Pipeline

Whenever any voxel geometry in `tools/build_zack_voxel.py` is modified, run these exact commands in PowerShell:

```powershell
# 1. Build GLB Mesh & Tres Palette:
python tools/build_zack_voxel.py

# 2. Ingest Palette into Unity RosterBook (Headless):
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.RosterBookBuilder.Build -logFile Logs/roster.log" -Wait

# 3. Render Canonical 4-Angle Turnaround in Unity (Front, 3/4, Side, Back):
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.PersonSwapProbe.Run -logFile Logs/swap.log" -Wait
```

### 📁 Output Locations:
- **4-Angle Studio Turnaround**: `Logs/person-swap-turnaround.png`
- **32-Animation Test Suite**: `Logs/person-swap-probe.png`
- **Probe Log**: `Logs/swap.log`

---

## 🚀 Standalone Build Rule

> **CRITICAL RULE**: Do **NOT** run `GameBuilder.BuildWindows` until the user explicitly inspects `Logs/person-swap-turnaround.png` and confirms approval of the model.

```powershell
# Run ONLY after explicit user approval:
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows -logFile Logs/build.log" -Wait
```
