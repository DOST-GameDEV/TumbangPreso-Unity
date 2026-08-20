# Character Customization & Wearables System: Agent Handoff & Implementation Blueprint

> **System Purpose**: This document is a standalone, self-contained implementation specification and execution prompt for adding the **In-Game Character Customization & Wearables System** to *Tumbang Preso*. It contains all voxel coordinate definitions, palette binding contracts, engine integration paths, and architecture required to resume or build this feature at any point in the future.

---

## 🎯 High-Level Objective

Build an interactive character customization feature allowing players to equip, mix, match, and preview modular wearables (headwear, eyewear, hairstyles, and accessories) on character voxel rigs in both the Select Screen and In-Game Matches.

---

## 🏗️ Architecture & Registry Location

All modular wearable box geometry is authored and stored in:
- **Registry Python Module**: [`tools/wearables_registry.py`](file:///C:/Users/matth/Documents/GitHub/TumbangPreso-Unity/tools/wearables_registry.py)
- **Wearables Catalog Doc**: [`docs/wearables_catalog.md`](file:///C:/Users/matth/Documents/GitHub/TumbangPreso-Unity/docs/wearables_catalog.md)
- **Master Model Pipeline**: [`tools/build_person_voxel.py`](file:///C:/Users/matth/Documents/GitHub/TumbangPreso-Unity/tools/build_person_voxel.py)
- **Asset Directory**: `Assets/TumbangPreso/Art/characters/persons/`
- **Unity Roster Book**: `Assets/TumbangPreso/Resources/RosterBook.asset`

---

## 📦 Available Wearables Catalog & Keys

The registry provides 7 modular presets mapped to distinct equipment slots:

### 1. `headwear` Slot
- **`headwear/ushanka_expedition`**: Volumetric Expedition Ushanka (cyan quilted crown, turned-up fur visor with snowflake crest, sculpted 360°-connected earflaps, inner white fleece cheek lining, and braided cord snowball pom-poms).
- **`headwear/beanie_slouchy`**: Volumetric Slouchy Knit Beanie (chunky 360° folded white knit cuff, cyan athletic stripe, dark patch with frost star, slouchy drape, and giant snowball bobble pom-pom).
- **`headwear/beret_baker`**: Volumetric Pastry Meister Baker Beret (flared snow-white puffy pastry cloud crown, fitted cyan base band, silver snowflake brooch, and fluttering ribbon tails).
- **`headwear/earmuffs_frost`**: Volumetric Frost Plush Earmuffs & Headset (giant snow-white fur ear warmers with cyan crystal snowflake cores and padded overhead arch band).

### 2. `eyewear` Slot
- **`eyewear/ski_goggles_pro`**: Pro High-Tech Ski Goggles (metallic silver chassis with 45° angle temple wraps, dual cyan crystal lenses, specular glints, center nose bridge rivet, and wraparound dark elastic strap).

### 3. `hair_accessory` Slot
- **`hair_accessory/star_ribbons`**: Frosted Star Clasp & Fluttering Twin Ribbons (silver-rimmed star clasp with white diamond gem and twin cyan fluttering ribbon tails cascading at $y \in [0.200, 0.340]$).

### 4. `jewelry` Slot
- **`jewelry/asymmetric_frost_earrings`**: Asymmetric Frost Drop Earrings (silver stud with dangling frost cyan droplet on right ear, silver stud with frost gem on left ear).

---

## 🎨 Unified Palette Slot Contract (16 Colors)

All wearable meshes index into the 16-color character palette:

```python
WHITE          = 0   # Fur trim, shirt fabric, socks, knit cuffs, specular rivets (#f4faff)
SILVER         = 1   # Metallic hardware, buckles, star borders, goggle hinges (#d4e2ec)
CYAN_TRIM      = 2   # Vibrant cyan piping, ribbons, cord ties (#2cbcd6)
FROST_ACCENT   = 6   # Crystalline icy blue for gems, stars, lens depth (#a8f0fa)
INK / HAIR     = 8   # Pitch black for sculpted hair and eyes (#141416)
OVERALLS       = 9   # Primary team cyan fabric for hat shell and overalls (#36c0d8)
WOOD_GOLD      = 10  # Warm amber wood for spatula handle peeking from pocket (#e4a032)
OVERALLS_DARK  = 11  # Deep cyan / navy shadow tone for seam stitching and straps (#1a6b82)
COLLAR_TRIM    = 12  # Stepped frost lapels and sleeve cuffs (#5fcfd0)
SKIN           = 13  # Option 2: Natural Fair Peach midtone (#f5b894)
SKIN_DARK      = 14  # Soft luminous warm peach shadow tone (#db9874)
SKIN_LIT       = 15  # 100% matched with SKIN midtone for uniform face/arms/legs (#f5b894)
```

---

## ⚙️ Implementation Roadmap for Customization System

When implementing character customization in Unity / C#:

1. **Modular Voxel Mesh Generation / Sub-Meshes**:
   - Option A (Baked per Loadout): Python build pipeline generates combination GLBs based on selected loadout IDs (e.g. `team-inday-loadout_01.glb`).
   - Option B (Runtime Equipping): Wearables are exported as separate modular GLB/FBX accessory prefabs with socket bone binding to `head`, `torso`, `arm-left`, `arm-right`, `leg-left`, `leg-right`.
2. **Character Customization UI (Select Screen)**:
   - Add a "Customize" button to the Character Select Screen.
   - Display a 3D turntable preview showing the character live with animated idle.
   - Provide category tabs (`Headwear`, `Eyewear`, `Accessories`, `Palettes`).
   - Allow equipping/unequipping with instantaneous 3D model swap.
3. **Player Profile & Persistence**:
   - Save player wearable loadout selection in `PlayerPrefs` or cloud save data (e.g., `inday_equipped_headwear = "headwear/ushanka_expedition"`).
   - `RosterBook` or `CharacterSpawner` instantiates the character with their customized loadout in matches.

---

## 📋 Copy-Paste Prompt for Future AI Agents

*Use this prompt to instruct any future AI assistant to continue work on customization:*

```markdown
You are tasked with expanding the Character Customization & Wearables System in Tumbang Preso.

Context & References:
- Read `docs/CUSTOMIZATION_SYSTEM_PROMPT.md` and `docs/wearables_catalog.md`.
- Inspect `tools/wearables_registry.py` for all modular wearable voxel definitions.
- Inspect `tools/build_person_voxel.py` for the core character rig, bone structure, and palette mappings.
- Character rig bounds require max head bone Y <= 0.7928 and total height within cast bounds [0.6613, 0.7928].
- Unity verification probe is run via:
  Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -ArgumentList "-batchmode -quit -projectPath . -executeMethod TumbangPreso.EditorTools.PersonSwapProbe.Run -logFile Logs/swap.log" -Wait

Your Task:
[Insert specific task here, e.g. "Add a UI customization menu in the Select Screen" or "Author 3 new summer-themed wearables in wearables_registry.py"]
```
