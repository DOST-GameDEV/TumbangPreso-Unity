# Character Pipeline Handoff: Zack Sculpt & Facial Expressions

## 📌 Context & Project State

- **Branch**: `models/team-inday` (in `C:\Users\matth\Documents\GitHub\TumbangPreso-Unity`)
- **Inday / Cheska Status**: **COMPLETED & LOCKED IN** (Commit `7e4832f2`).
  - Master model: Volumetric Cyan Ushanka with Pro Ski Goggles & Option 2 Fair Peach skin tone (`#f5b894`).
  - Build: Standalone player compiled to `C:\Users\matth\Desktop\TumbangPreso-Unity\TumbangPreso.exe`.

---

## 🎯 Next Priority 1: Zack Hair Volume & Model Sculpt

### Visual Target
![Zack Reference](file:///C:/Users/matth/.gemini/antigravity/brain/36d960c3-2aa5-444e-89dd-b5dc440edfae/.user_uploaded/media_1787148503834.png)

### Key Styling Directives
1. **💇 High-Volume Punk Quiff / Faux-Hawk (NOT an Afro)**:
   - Must have a **sculpted 3D high-volume undercut faux-hawk / punk quiff**.
   - **Top / Crest**: Layered, swept-forward voluminous tufts dyed in **vibrant neon magenta / hot pink** (`#e61c78` / `#ff2a8d`).
   - **Sides & Back**: Dark black / midnight taper fade undercut (`#14121a`).
   - **Constraint**: **Do NOT make it a rounded afro or flat dome**. It must have sharp, stylized geometric stepped voxels and forward fringe locks as seen in the reference image.
2. **💜 Outfit & Accessories**:
   - Purple open jacket/blazer with turned-up collar and lapels (`#6a2cc9` / `#8438f0`).
   - Layered crystal pendant chain necklace (`#b870ff` / `#e2b8ff`).
   - Black fitted undershirt (`#1a1820`).
   - Black cargo pants with utility straps (`#14121a`).
   - Gold rectangular belt buckle (`#f5a820`) with dangling silver wallet chain (`#d4e2ec`).
   - Purple skate sneakers with white soles (`#6a2cc9`, `#ffffff`).
3. **🎨 Skin Palette**:
   - Uniform warm golden skin tone strictly matched across face, arms, neck, and legs (`SKIN`, `SKIN_LIT`, `SKIN_DARK`).

---

## 🎯 Next Priority 2: Facial Expressions System

- Refine facial expression textures and dynamic eye/mouth animation swapping in Unity for the roster characters.

---

## 🛠️ Build & Verification Pipeline

```powershell
# 1. Build GLB & Tres:
python tools/build_person_voxel.py  # or tools/build_zack_voxel.py

# 2. Ingest Palette into Unity RosterBook:
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.RosterBookBuilder.Build -logFile Logs/roster.log" -Wait

# 3. Verify in Unity Probe & Capture Turnaround:
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.PersonSwapProbe.Run -logFile Logs/swap.log" -Wait

# 4. Build Standalone Game to Desktop:
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows -logFile Logs/build.log" -Wait
```
