# Final Handoff: Phaister Witch Hero & 1:1 Cast Scale Integration

## 📌 Context & Repository State
- **Repository**: `DOST-GameDEV/TumbangPreso-Unity`
- **Active Branch**: `feat/hero-witch-v2` (rebased directly on top of `origin/feat/ilalim-ng-tulay-map` @ `349b0171`)
- **Remote Status**: Clean, synchronized, and pushed to `origin/feat/hero-witch-v2`
- **Verification Status**:
  - Unit Tests: 69/69 passed (`dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj`)
  - Rig & Animation Probe: 32/32 binds moving cleanly (`PersonSwapProbe.Run`)
  - Standalone Windows Build: 100% successful (`GameBuilder.BuildWindows`)

---

## 🎯 Summary of User Directives & Work Completed

### 1. 1:1 Standard Cast Proportions & Scale
- **User Directive**: *"she looks smaller than other characterrs bcz ur using her hat as part of her height can u make her same size as opther characters? use her head as the basis not ehr hat"*, *"look how short she is compared to the normal (dont compare to sean)"*.
- **Implementation**:
  - Re-authored all bounding boxes to full standard Kenney cast scale:
    - **Arm Span**: `[-0.3836, 0.3836] m` (exact match with Zack, Cheska, and Dante).
    - **Torso Width**: `[-0.142, 0.142] m` with flared side hip skirt panels and peplum flaps.
    - **Head Width**: `[-0.225, 0.225] m` (solid 0.45m wide skull enclosure, 0 skin leaks).
    - **Face Opening**: Width 0.29m (`X in [-0.145, 0.145] m`, `Y in [0.343, 0.540] m`).
    - **Chin & Eye Alignment**: Chin at `Y = 0.343 m`, eyes at `Y in [0.435, 0.505] m` horizontally aligning with the entire cast.
    - **Witch Hat**: Sits snugly on top of the head crown (`Y in [0.655, 1.015] m`), rising naturally above the hair.

### 2. Expressive Face & Playful Witch Smirk
- **User Directive**: *"also improve her facial expression, its js eyes no mouth"*.
- **Implementation**:
  - **Eyes**: Crisp 3x2 voxel eyes in Slot 8 `INK` (`#14101c`).
  - **Eyebrows**: Confident arched brows (`Y in [0.518, 0.542] m`).
  - **Mouth**: Playful witch smirk (`:3` smile with upturned left corner from `Y = 0.365` to `0.408 m`).
  - **Bangs**: Multi-tiered fringe stepping down diagonally with the center-right dipped tuft, emerging directly from under the hat brim.

### 3. Stepped 3D Swallowtail Cape & Peplum Flaps
- **Cape Exterior**: Deep black frock coat back with stepped 5-segment swallowtail wings reaching `Y = 0.048 m`.
- **Chevron Trim**: Continuous royal purple (`#4a1e78`) inverted-V `/ \` trim band down to `Y = 0.038 m`.
- **Cape Inner Lining**: Crimson red (`#8c1424`) inner lining visible in front and 3/4 views flanking the trousers while leaving the center rear notch completely hollow.
- **Celestial Emblems**: Gold crescent moon (`🌙`) with shadow cutout and 2 stars (`+`) on the cape back.
- **Chest & Neck Details**: High crimson upturned collar tips, scalloped gold V-chain, teal collar bow/knot (`#20b2aa`), and faceted amethyst medallion (`#9838d8`) with gold prongs and specular glint.

### 4. Volumetric 5-Lock Stepped Hair Mane
- Reconstructed the rear and side hair into 5 cascading vertical locks (Outer Left, Mid Left, Center Spine, Mid Right, Outer Right) with stepped highlight bevels (`#e82882`) down to `Y = 0.250 m`.

### 5. FPP Viewmodel Arms (`ViewmodelArms.cs`)
- Implemented `BuildPhaisterAccessories` in `Assets/TumbangPreso/Runtime/Camera/ViewmodelArms.cs`:
  - Deep black upper sleeve (`#181622`).
  - Royal purple forearm band (`#4a1e78`).
  - Gold cross emblem (`#f8b824`) on the outer wrist.
  - Crimson accent stripe (`#8c1424`).
  - Flared white ruffled cuff (`#ffffff`).
  - Porcelain peach skin hand (`#f4c098`) with knuckle shading.

---

## 🎨 16-Color Palette Slot Mapping

| Slot | Identifier | Hex Color | Description |
| :---: | :--- | :--- | :--- |
| **0** | `COAT_DARK` | `#181622` | Deep black/charcoal coat, hat, trousers |
| **1** | `CLOTH_PURPLE` | `#4a1e78` | Royal purple hat band, belt, sleeve band, trim, shoes |
| **2** | `LILAC_GEM` | `#9838d8` | Medallion crystal, wand crystal tips |
| **3** | `GOLD` | `#f8b824` | Hat buckle, V-chain, waist buckle, sleeve cross, moon & stars |
| **4** | `WAND_WOOD` | `#7c3c20` | Warm wood wand shafts |
| **5** | `WAND_BAND` | `#b83424` | Crimson wand wrap bands |
| **6** | `HAIR_MAGENTA` | `#d8186e` | Rich magenta / hot pink hair body |
| **7** | `HAIR_HIGHLIGHT` | `#e82882` | Magenta hair highlights & stepped lock tips |
| **8** | `INK` | `#14101c` | Solid ink for eyes, eyebrows, and smirk mouth |
| **9** | `TEAL_KNOT` | `#20b2aa` | Teal/cyan collar knot above medallion |
| **10** | `CRIMSON` | `#8c1424` | Crimson cape lining, high collar, sleeve stripe, ankle stripe |
| **11** | `GOLD_SHADOW` | `#b87814` | Deep gold buckle shadow |
| **12** | `WHITE` | `#ffffff` | Crisp white shoe sole slabs, shirt cuffs |
| **13** | `SKIN` | `#f4c098` | Warm porcelain peach skin |
| **14** | `SKIN_DARK` | `#e0a078` | Warm peach skin shadow |
| **15** | `SKIN_LIT` | `#f4c098` | Uniform skin tone |

---

## 📁 Key Files & Locations

- `tools/build_phaister_voxel.py`: Full procedural voxel generator for Phaister (head, body, skeleton retargeter, glTF exporter).
- `Assets/TumbangPreso/Runtime/Camera/ViewmodelArms.cs`: First-person viewmodel arm meshes, cuffs, gold crosses, and hands.
- `Assets/TumbangPreso/Editor/PersonSwapProbe.cs`: Automated probe script for lineup, turnaround, and animation matrix captures.
- `Assets/TumbangPreso/Art/characters/persons/team-phaister.glb`: Generated 3D rigged model.
- `Assets/TumbangPreso/Resources/Roster/person_phaister.asset`: Roster definition for Phaister.
- `HANDOFF.md`: This document in the root directory for easy access across devices.

---

## 🛠️ Verification & Build Commands

To re-verify or build on any machine:

```powershell
# 1. Regenerate Phaister 3D Model
python tools/build_phaister_voxel.py

# 2. Run Unity Rig & Animation Probe (outputs to Logs/person-swap-turnaround.png and Logs/cast_lineup.png)
"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.PersonSwapProbe.Run -logFile Logs/probe-run.log

# 3. Run Core Unit Tests
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj

# 4. Build Standalone Windows Executable
"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows -logFile Logs/game-build.log
```
