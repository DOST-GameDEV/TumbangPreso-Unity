# Voxel Wearables & Accessories Catalog

This document defines the modular 3D voxel wearable and accessory catalog for *Tumbang Preso* characters. These items are designed for future character customization features where players can equip, swap, and layer headwear, eyewear, and jewelry onto their character models.

---

## 🎨 Palette Slot Reference

All wearable geometry uses standardized palette slots (defined in `tools/build_person_voxel.py` and `tools/wearables_registry.py`):

| Slot # | Constant | Role / Hue Description |
| :---: | :--- | :--- |
| `0` | `WHITE` | Clean snow-white fur, shirt fabric, socks, knit cuffs, highlight rivets |
| `1` | `SILVER` | Metallic hardware, buckle rings, star borders, goggle hinges |
| `2` | `CYAN_TRIM` | Vibrant athletic cyan (`#00c4cc` / `#2cbcd6`) for ribbons, piping, cord ties |
| `6` | `FROST_ACCENT` | Crystalline icy frost blue (`#a0ecf4` / `#c8f4fc`) for gems, lens depth, stars |
| `8` | `INK` / `HAIR` | Deep pitch black (`#141416`) for sculpted hair base and dark pupils |
| `9` | `OVERALLS` | Primary team cyan fabric (`#3bbcd6` / `#36c0d8`) for hat crowns and overalls |
| `10` | `WOOD_GOLD` | Warm amber spatula wood (`#e4a032` / `#f0ad42`) |
| `11` | `OVERALLS_DARK` | Shadow cyan / navy accent (`#1a6b82` / `#227088`) for seam stitching & straps |
| `12` | `COLLAR_TRIM` | Stepped frost lapels / collar highlights (`#5fcfd0`) |
| `13` | `SKIN` | Golden-bronze Filipina skin midtone (`#ecaa6c`) |
| `14` | `SKIN_DARK` | Warm golden shadow skin tone (`#d8985e`) |
| `15` | `SKIN_LIT` | Radiant lit golden skin tone (`#ecaa6c` - unified with midtone) |

---

## 🧢 Headwear Catalog

### 1. Volumetric Expedition Ushanka (`headwear/ushanka_expedition`)
- **Category**: Winter Trapper / Ushanka Hat
- **Description**: Cyan fabric trapper shell with turned-up snow-white fur visor, silver snowflake crest, streamlined side earflaps, hanging braided cord ties with cute snowball pom-poms, and seamless 360° back neck wrap.
- **Key Bounds**: $y \in [0.250, 0.782]$, $x \in [-0.226, 0.226]$, $z \in [-0.226, 0.234]$.
- **Modular Boxes**: Stored in `tools/wearables_registry.py` under `WEARABLE_USHANKA`.

### 2. Volumetric Slouchy Knit Beanie (`headwear/beanie_slouchy`)
- **Category**: Ribbed Knit Slouchy Beanie
- **Description**: Chunky 360° folded white knit cuff with cyan athletic stripe, dark patch with frost star icon, slouching back crown, and giant fluffy snowball bobble pom-pom on apex.
- **Key Bounds**: $y \in [0.590, 0.790]$, $x \in [-0.208, 0.208]$, $z \in [-0.232, 0.212]$.
- **Modular Boxes**: Stored in `tools/wearables_registry.py` under `WEARABLE_BEANIE`.

### 3. Volumetric Pastry Meister Baker Beret (`headwear/beret_baker`)
- **Category**: French Baker Beret / Cloud Puffy Beret
- **Description**: Chic flared snow-white puffy pastry cloud beret with fitted cyan base band, metallic silver snowflake brooch, and fluttering cyan ribbon tails on left side.
- **Key Bounds**: $y \in [0.565, 0.790]$, $x \in [-0.230, 0.230]$, $z \in [-0.230, 0.210]$.
- **Modular Boxes**: Stored in `tools/wearables_registry.py` under `WEARABLE_BERET`.

### 4. Volumetric Frost Plush Earmuffs & Headset (`headwear/earmuffs_frost`)
- **Category**: Winter Plush Earmuff Headset
- **Description**: Giant plush snow-white fur ear warmers with cyan crystal snowflake cores and a padded overhead arch band.
- **Key Bounds**: $y \in [0.380, 0.730]$, $x \in [-0.244, 0.244]$, $z \in [-0.070, 0.090]$.
- **Modular Boxes**: Stored in `tools/wearables_registry.py` under `WEARABLE_EARMUFFS`.

---

## 🥽 Eyewear Catalog

### Pro High-Tech Ski Goggles (`eyewear/ski_goggles_pro`)
- **Category**: Protective Eyewear / Forehead Goggles
- **Description**: Perched metallic silver chassis with dual cyan crystal lenses, specular glints, center nose bridge rivet, and wraparound dark elastic strap.
- **Key Bounds**: $y \in [0.625, 0.698]$, $x \in [-0.210, 0.210]$, $z \in [-0.228, 0.238]$.
- **Modular Boxes**: Stored in `tools/wearables_registry.py` under `PRO_SKI_GOGGLES`.

---

## 🎀 Hair Accessories & Jewelry

### Frosted Star Clasp & Twin Fluttering Ribbons (`hair_accessory/star_ribbons`)
- **Category**: Back Hair Clasp & Tail Ribbons
- **Description**: Silver-rimmed frosted star clasp with white diamond gem and twin cyan fluttering ribbon tails cascading down the back ponytail.
- **Key Bounds**: $y \in [0.200, 0.375]$, $x \in [-0.075, 0.075]$, $z \in [-0.232, -0.214]$.
- **Modular Boxes**: Stored in `tools/wearables_registry.py` under `FROSTED_STAR_RIBBONS`.

### Asymmetric Frost Drop Earrings (`jewelry/asymmetric_frost_earrings`)
- **Category**: Ear Jewelry
- **Description**: Silver stud with dangling frost cyan droplet on right ear, silver stud with frost gem on left ear.
- **Key Bounds**: $y \in [0.435, 0.508]$, $x \in [-0.219, 0.218]$, $z \in [-0.018, 0.008]$.
- **Modular Boxes**: Stored in `tools/wearables_registry.py` under `ASYMMETRIC_EARRINGS`.

---

## 🛠️ Usage in Python Build Pipeline

To build a character model with specific wearables:

```python
import tools.wearables_registry as wr
import tools.build_person_voxel as bpv

# Retrieve desired wearable components
ushanka = wr.get_wearable("headwear/ushanka_expedition")
goggles = wr.get_wearable("eyewear/ski_goggles_pro")
ribbons = wr.get_wearable("hair_accessory/star_ribbons")

# Combine with baseline hair and build model
bpv.HEAD = base_hair + ushanka + goggles + ribbons
bpv.main()
```
