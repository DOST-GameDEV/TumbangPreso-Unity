"""Model the east side of Taft for the Ilalim ng Tulay rebuild: the shop row facing the court and
the commercial district behind it (ILALIM-1.3, east kit).

  py -3 tools/author_ilalim_textures_eastside.py          # paint the textures and sign faces first
  blender -b --python tools/author_ilalim_eastside.py -- [--preview N] [--shots a,b,c] [--no-lrt]

Writes ArtSource/ilalim/eastside.blend. With --preview it also writes versioned renders to
Logs/ilalim-blender/east_<shot>_vN.png (never overwriting an earlier version). --no-lrt leaves
the LRT-1 guideway (linked from ArtSource/ilalim/lrt_kit.blend for context) out of the renders.

THE PLACE (docs/ILALIM_REWORK_GUIDE.md section 0, research.md): the east side of Taft Avenue
south of Padre Faura, Ermita, a dense Manila commercial strip. Every OSM building east of the
corridor (x > 11) is built from ArtSource/ilalim/osm_layout.json at its real footprint and storey
count (unknown storeys get 2 to 5 from a fixed seed). The layout is the true map east of Taft;
the blockout's sightline override only moves things west of it. Blender X is the game's x (east),
Blender Y is the game's z (north), the origin is the court centre on Taft.

THE SHOP ROW is the east wall of the play area (the pavement runs x 7..11, its top at 0.212).
The West East Center and the Astral Tower podium stand on it, their Taft faces pinned to x = 11.0
(the real frontage is 11 m deep and was squeezed, research.md section 7). SHOP_ROW lists the
ground-floor businesses south to north; the owner's contract puts in:
  * PC EXPRESS facing the overclock pad at (9.0, 5.5): glass front, centre doors, kick plate,
    a slim overhang, and the one real brand on the map, its official mark on a lightbox (the
    recorded brand exception, see tools/author_ilalim_signs.py);
  * the PISONET at y 9..15 (its terminals are the prop kit's; the front is kept shallow so they
    stand against it), a KARINDERYA serving pares behind the pares cart at (8.8, -5), a
    XEROX / PRINT / BIND shop, a CELLPHONE repair stall, the tower's residential LOBBY with the
    dorm "bedspace" boards, and a BOTIKA with medical supplies and scrubs on the Padre Faura
    corner (PGH is across the street).
  Awnings are at varied depths (0.8 to 2.6 m), never lower than 2.4 m over the pavement and never
  past x = 8.4, so the 4 m pavement stays walkable; nothing of the kit stands on the pavement
  except the chalkboard pressed against the cell stall. Signs come from the sign kit's eleven
  systems and no two neighbours share one.

THE BUILDINGS, each with its own construction, not a repaint (KANTO_DESIGN_GUIDE.md section 4):
  * West East Center (4 storeys): salmon-pink podium, balconies on the Taft face with
    breeze-block panels and planters, recessed ribbon windows behind (Commons references).
  * The Astral Tower (19 storeys, UNNAMED, the skyline landmark): a cream podium with ribbon
    windows, then a rounded slab tower whose every floor is a cream balcony band over a salmon
    wall, broken by two salmon service shafts, with a lift house and tanks on the roof.
  * Manok ni Mang Carding (the old KFC building, 2 storeys): an invented Filipino fried-chicken
    place, set back behind a forecourt with its pylon, a maroon fascia band, full glazing and a
    drive-through canopy. No real brand on it.
  * Vista GL Taft (6 storeys): panel construction with vertical fins, punched windows, grilles,
    a laundry and a siomai stall below, and a vulcanizing tin sign by its drive.
  * Manila Science High School across Padre Faura (10 storeys): brick panels and a glass
    curtain wall in a grey concrete frame, a lower old wing. Unnamed.
  * Manila Baptist Church and Cosmopolitan Church: pitched halls with a tower and a cross.
  * GCK Building (11 storeys): an office slab with curtain walls. Everything else: concrete
    mid-rises with ribbon windows, cantilevered floors, hoods or balconies, and further away
    LOD blocks whose windows are painted facade textures. All carry roof tanks and parapets;
    the near ones carry aircon boxes and grilles.

FACADE IDENTITY (2026-09-30; owner: "can you think of a way to make the place look more lively,
more unique building shapes etc?", then "proceed"). Before this every unnamed building shared one
grey-white look. Now each unnamed building and Vista GL Taft gets a Look (assign_looks):
  * a PAINT from PALETTE, nine weathered pastels (mint, salmon, butter, lilac, cream, rose, sage,
    seafoam, chalk), chosen greedily so no two buildings within 45 m share one, with the salmon
    landmarks counting as salmon and rose neighbours and chalk kept off the front row (near-white
    read as the old look from the street). Each paint has its own drawings (east_fac_punched_*,
    _balcony_*, _ribbon_*, _shops_*), painted by the texture script with the window rhythm the
    grille cards rely on; far LOD blocks take the same drawing kind as before in their paint, over
    a ground storey of drawn shops with invented signboards;
  * a GRILLE card style (sunburst, diamond, wave, grid), a roll-up SHUTTER paint, an AWNING kind,
    a balcony PARAPET (solid in the paint or trim, or a grille card under a chunky rail) and its
    own aircon and grille densities;
  * TRAITS on its street faces (facade_traits, Site.exposed: edges whose ground 2.5 m out is not
    another building). Slots follow the construction (between fins, else about every 3.4 m) and a
    per-edge pattern (alternate, columns, mixed, sparse), so projecting BAYS run up the floors and
    BALCONIES stack in columns, never scattered. Balconies carry pots with round shrubs and, on
    some, a washing line on two rods; awnings (canvas or tin) hang over some ground-floor bays; one
    hand-painted BLADE sign (FACADE_SIGNS, invented names) goes near a corner of each front-row
    building. On Taft faces bays are 0.5 m and balconies 0.6 m deep, above the ground floor only,
    and no awnings or blades.
`detail` 2 is the front row (within 75 m of Taft and Padre Faura's corner, and Vista), 1 the
second row (its two longest street faces, sparse), 0 the far LOD blocks (paint and drawings only).
Every trait is checked against the other footprints, the corner store's lot (SARI_LOT, never
entered) and the street and tree kits' poles, signals, trunks and crowns (read_boxes links their
.blends only to measure, then unlinks them). Footprints, roof heights, roof slabs and the roof
kits are untouched: the traits use the Look's own random stream, and every draw the roof kit
depends on is made exactly as before, so tanks and stair houses stay where they were (another kit
builds on these roofs). The shop row, its props and the named buildings other than Vista keep
their own constructions. Kit faces went from about 46k to about 69k (93k with the linked
guideway, 71k before).

THE HOUSE STYLE (KANTO_DESIGN_GUIDE.md section 2, LAGOON_REWORK_GUIDE.md section 2): real
editable models from this script; chunky and organic, never fiddly (thick rounded members, every
footprint's corners filleted, detail in the painted textures); NO TWO SURFACES SHARE A PLANE
(slabs, fins, parapets and signs penetrate what they sit on by 1 to 5 cm, neighbouring footprints
overlap by 8 cm so their party walls cross instead of coinciding); every piece has a live Bevel
modifier with hardened normals; world-scale UVs, and storey-aligned UVs for facade drawings.

DIRT AND GRIME, positional as on the guideway: every wall writes UVGrime (metres below the slab or
parapet above, over 3 m) and UVSplash (metres above the street, over 1.5 m), and the materials
multiply east_grime_drips and east_grime_splash through them. Rust under grilles is drawn into
the facade textures.

ROLE HUES (Art_Direction.md section 1): nothing near #f87020 or #0080e8. The salmon is a pink
salmon, the glass is teal-grey, the school's accent is teal-grey where the real one is blue.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
import author_ilalim_lrt as L            # noqa: E402  (fillet, rounded_rect: read-only)
import author_ilalim_signs as S          # noqa: E402
from ilalim_antitile import anti_tile    # noqa: E402

ROOT = TOOLS.parent
SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
LAYOUT = SOURCE / "osm_layout.json"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
UP = Vector((0, 0, 1))

PAVE_TOP = 0.212
FRONT = 11.0            # the shop row's wall line
EYE = 1.25
GRIME_H, SPLASH_H = 3.0, 1.5

# ------------------------------------------------------------------ materials
# name: (texture, tint or None, normal strength, tile (u, v) in metres or None for fixed UVs).
# Flags live in the sets below.
MATERIALS = {
    "east_wec_render":   ("east_wec_render", None, 0, (4, 4)),
    "east_wec_breeze":   ("east_wec_breeze", None, 0.8, (1.6, 1.6)),
    "east_wec_wall":     ("east_wec_wall", None, 0, (4, 4)),
    "east_astral_wall":  ("east_astral_wall", None, 0, (6, 3.0)),
    "east_astral_band":  ("east_astral_band", None, 0, (4, 1.2)),
    "east_astral_cream": ("east_render", (0.97, 0.93, 0.84), 0, (4, 4)),
    "east_astral_salmon": ("east_render", (0.80, 0.58, 0.54), 0, (4, 4)),
    "east_glass_ribbon": ("east_glass_ribbon", None, 0, (4.8, 1.6)),
    "east_slab":         ("east_concrete", (1.05, 1.03, 1.0), 0.3, (4, 4)),
    "east_concrete":     ("east_concrete", None, 0.4, (4, 4)),
    "east_roof":         ("east_roof", None, 0, (6, 6)),
    "east_render_mint":  ("east_render", (0.74, 0.83, 0.74), 0, (4, 4)),
    "east_render_ochre": ("east_render", (0.86, 0.74, 0.50), 0, (4, 4)),
    "east_render_rose":  ("east_render", (0.86, 0.70, 0.68), 0, (4, 4)),
    "east_render_cream": ("east_render", (0.94, 0.89, 0.76), 0, (4, 4)),
    "east_render_grey":  ("east_render", (0.78, 0.77, 0.74), 0, (4, 4)),
    "east_render_teal":  ("east_render", (0.66, 0.77, 0.74), 0, (4, 4)),
    "east_render_white": ("east_render", (0.96, 0.95, 0.92), 0, (4, 4)),
    "east_lod_ribbon_a":  ("east_lod_ribbon", (0.97, 0.93, 0.83), 0, (6, 3.2)),
    "east_lod_ribbon_b":  ("east_lod_ribbon", (0.82, 0.83, 0.80), 0, (6, 3.2)),
    "east_lod_punched_a": ("east_lod_punched", (0.80, 0.88, 0.80), 0, (6, 3.2)),
    "east_lod_punched_b": ("east_lod_punched", (0.98, 0.90, 0.76), 0, (6, 3.2)),
    "east_lod_balcony_a": ("east_lod_balcony", (0.93, 0.82, 0.80), 0, (6, 3.2)),
    "east_lod_balcony_b": ("east_lod_balcony", (0.95, 0.95, 0.92), 0, (6, 3.2)),
    "east_tile_cream":   ("east_tile", None, 0.5, (1, 1)),
    "east_tile_maroon":  ("east_tile", (0.62, 0.34, 0.34), 0.5, (1, 1)),
    "east_tile_green":   ("east_tile", (0.50, 0.66, 0.54), 0.5, (1, 1)),
    "east_shutter":      ("east_shutter", None, 0.6, (2, 1)),
    "east_shop_glass":   ("east_shop_glass", None, 0, (4, 4)),
    "east_display":      ("east_shop_display", None, 0, (4, 3)),
    "east_tin_maroon":   ("east_tin", (0.60, 0.30, 0.30), 0.7, (2, 2)),
    "east_tin_green":    ("east_tin", (0.48, 0.60, 0.50), 0.7, (2, 2)),
    "east_tin_grey":     ("east_tin", (0.80, 0.80, 0.78), 0.7, (2, 2)),
    "east_tin_roof":     ("east_tin", (0.44, 0.50, 0.46), 0.7, (2, 2)),
    "east_canvas_green": ("east_canvas_green", None, 0, (2, 2)),
    "east_canvas_maroon": ("east_canvas_maroon", None, 0, (2, 2)),
    "east_timber":       ("east_timber", None, 0.3, (1.5, 1.5)),
    "east_int_shelves":  ("east_interior_shelves", None, 0, (4, 3)),
    "east_int_eatery":   ("east_interior_eatery", None, 0, (4, 3)),
    "east_int_pc":       ("east_interior_pc", None, 0, (4, 3)),
    "east_int_dark":     (None, (0.22, 0.20, 0.19), 0, None),
    "east_ac":           ("east_ac", None, 0.5, None),
    "east_ac_body":      ("east_render", (0.88, 0.87, 0.82), 0, (1, 1)),
    "east_tank":         ("east_tank", None, 0.4, (2, 1.2)),
    "east_tank_dark":    ("east_tank", (0.42, 0.45, 0.42), 0.4, (2, 1.2)),
    "east_brick":        ("east_brick", None, 0.6, (2, 2)),
    "east_curtain":      ("east_curtain", None, 0, (3, 3.4)),
    "east_grille":       ("east_grille", None, 0, None),
    "east_metal":        ("east_tin", (0.24, 0.24, 0.25), 0.3, (2, 2)),
    "east_steel_light":  ("east_tin", (0.72, 0.72, 0.70), 0.3, (2, 2)),
    "east_foliage":      (None, (0.24, 0.38, 0.20), 0, None),
    "east_foliage_lt":   (None, (0.36, 0.50, 0.26), 0, None),
    "east_soil":         (None, (0.25, 0.19, 0.15), 0, None),
    "east_maroon":       ("east_render", (0.52, 0.18, 0.20), 0, (4, 4)),
    "east_mustard":      ("east_render", (0.86, 0.72, 0.36), 0, (4, 4)),
    "east_cross":        (None, (0.93, 0.92, 0.88), 0, None),
    "east_cloth_a":      (None, (0.90, 0.88, 0.80), 0, None),
    "east_cloth_b":      (None, (0.62, 0.72, 0.62), 0, None),
    "east_cloth_c":      (None, (0.78, 0.58, 0.62), 0, None),
    "east_cloth_d":      (None, (0.86, 0.78, 0.42), 0, None),
    "east_cloth_e":      (None, (0.55, 0.60, 0.66), 0, None),
}

# ------------------------------------------------------------------ facade identity tables
# These four tables are plain literals on purpose: tools/author_ilalim_textures_eastside.py reads
# them with ast.literal_eval (it cannot import this bpy script), so the painter and the builder
# share one source.
#
# The district palette, weathered pastels of the Manila vernacular (owner: "can you think of a way
# to make the place look more lively, more unique building shapes etc?"). key: (wall, trim), sRGB
# hex. Every hue stays clear of the role hues: the salmon is pushed pink (hue about 10 degrees, low
# saturation, where offence orange is 23 degrees and saturated), there is no mid blue, the lilac
# sits at 270 degrees and the seafoam at 160, far either side of defence blue's 207.
PALETTE = {
    "mint":    ("b3d4b6", "f1eee4"),
    "salmon":  ("e0b0a6", "f4ede2"),
    "butter":  ("ecd88f", "f6f2e4"),
    "lilac":   ("c4b6d6", "f3f0ee"),
    "cream":   ("e6d4b0", "9c7a68"),
    "rose":    ("d6a2aa", "f4ece6"),
    "sage":    ("c0c89e", "efeadb"),
    "seafoam": ("acd3c5", "f3f0e6"),
    "chalk":   ("e5e2d8", "8fa290"),
}
# Security grille drawings, each a cut-out card (RGBA): the kit's first sunburst plus three more.
GRILLE_STYLES = ("sunburst", "diamond", "wave", "grid")
# Roll-up shutter paints (None: bare galvanized, the kit's first shutter).
SHUTTER_PAINTS = {"galv": None, "green": "6e977a", "maroon": "8a4c4f", "butter": "d6c07c", "sage": "9dab8b"}
# Hand-painted vertical signs for the front-row buildings: invented businesses, no brands. Each is
# a blade in the sign kit's own system (tools/author_ilalim_signs.py), added to its SIGNS table.
FACADE_SIGNS = {
    "fac_gupitan": {"system": "blade", "w": 0.72, "h": 1.9, "lines": [["GUPITAN NI BOY", 1.0], ["HAIRCUT • SHAVE", 0.42]],
                    "bg": "2f6b4f", "fg": "f5f0e0", "accent": "f5f0e0"},
    "fac_tahian": {"system": "blade", "w": 0.72, "h": 1.9, "lines": [["TAHIAN NI NENA", 1.0], ["ALTERATION • UNIPORME", 0.42]],
                   "bg": "f1ecdf", "fg": "7a2a3e", "accent": "7a2a3e"},
    "fac_bigasan": {"system": "blade", "w": 0.72, "h": 1.9, "lines": [["BIGASAN", 1.0], ["BIGAS • ITLOG • ASUKAL", 0.42]],
                    "bg": "e8cf62", "fg": "4a1c1c", "accent": "4a1c1c"},
    "fac_paupahan": {"system": "blade", "w": 0.72, "h": 1.9, "lines": [["PAUPAHAN", 1.0], ["ROOM • BEDSPACE • 3F", 0.42]],
                     "bg": "7a2a2e", "fg": "f3e7c4", "accent": "f3e7c4"},
    "fac_manukan": {"system": "blade", "w": 0.72, "h": 1.9, "lines": [["LITSON MANOK", 1.0], ["LIEMPO • INASAL", 0.42]],
                    "bg": "efe4c7", "fg": "8f2126", "accent": "3a5a2a"},
    "fac_relo": {"system": "blade", "w": 0.72, "h": 1.9, "lines": [["RELOHERO NI KA ISKO", 1.0], ["RELO • BATERYA • SUSI", 0.42]],
                 "bg": "33342f", "fg": "e9d77a", "accent": "e9d77a"},
    "fac_kapehan": {"system": "blade", "w": 0.72, "h": 1.9, "lines": [["KAPEHAN NI LOLA", 1.0], ["KAPE • PANDESAL", 0.42]],
                    "bg": "d9e5d6", "fg": "233f33", "accent": "233f33"},
    "fac_hardware": {"system": "blade", "w": 0.72, "h": 1.9, "lines": [["HARDWARE NI OMENG", 1.0], ["PAKO • PINTURA • TUBO", 0.42]],
                     "bg": "3f5a3a", "fg": "f1e6c8", "accent": "f1e6c8"},
}
S.SIGNS.update(FACADE_SIGNS)

for _k, _spec in S.SIGNS.items():
    MATERIALS[S.face_material(_k)] = (f"east_sign_{_k}", None, 0, None)
MATERIALS.update(S.BODY_MATERIALS)


def _lin(hexs):
    out = []
    for i in (0, 2, 4):
        c = int(hexs[i:i + 2], 16) / 255
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return out


def paint_tint(hexs):
    """The tint that turns the neutral east_render drawing (base ecebe7) into this paint: a ratio of
    LINEAR colours, since the node multiplies after the texture's sRGB decode."""
    return tuple(min(1.05, c / b) for c, b in zip(_lin(hexs), _lin("ecebe7")))


FACADE_DRAWINGS = ("punched", "balcony", "ribbon", "shops")
for _key, (_wall, _trim) in PALETTE.items():
    MATERIALS[f"east_paint_{_key}"] = ("east_render", paint_tint(_wall), 0, (4, 4))
    MATERIALS[f"east_trim_{_key}"] = ("east_render", paint_tint(_trim), 0, (4, 4))
    for _d in FACADE_DRAWINGS:
        MATERIALS[f"east_fac_{_d}_{_key}"] = (f"east_fac_{_d}_{_key}", None, 0, (6, 3.2))
for _g in GRILLE_STYLES[1:]:
    MATERIALS[f"east_grille_{_g}"] = (f"east_grille_{_g}", None, 0, None)
for _p, _hex in SHUTTER_PAINTS.items():
    if _hex:
        MATERIALS[f"east_shutter_{_p}"] = (f"east_shutter_{_p}", None, 0.6, (2, 1))
MATERIALS.update({
    "east_pot_clay":  (None, (0.47, 0.31, 0.28), 0, None),
    "east_pot_white": (None, (0.84, 0.83, 0.79), 0, None),
    "east_pot_green": (None, (0.25, 0.35, 0.27), 0, None),
    "east_rail":      ("east_tin", (0.30, 0.31, 0.29), 0.3, (2, 2)),
})


def grille_mat(style):
    return "east_grille" if style == "sunburst" else f"east_grille_{style}"


def shutter_mat(paint):
    return "east_shutter" if SHUTTER_PAINTS.get(paint) is None else f"east_shutter_{paint}"

# Surfaces that take the positional grime (walls, parapets, slab edges).
GRIMED = {"east_wec_render", "east_wec_breeze", "east_wec_wall", "east_astral_wall", "east_astral_band",
          "east_astral_cream", "east_astral_salmon", "east_slab", "east_concrete", "east_render_mint",
          "east_render_ochre", "east_render_rose", "east_render_cream", "east_render_grey", "east_render_teal",
          "east_render_white", "east_lod_ribbon_a", "east_lod_ribbon_b", "east_lod_punched_a",
          "east_lod_punched_b", "east_lod_balcony_a", "east_lod_balcony_b", "east_tile_cream", "east_tile_maroon",
          "east_tile_green", "east_shutter", "east_brick", "east_maroon", "east_mustard", "east_tank",
          "east_tank_dark"}
GRIMED |= {m for m in MATERIALS if m.startswith(("east_paint_", "east_trim_", "east_fac_", "east_shutter_"))}
ALPHA = {"east_grille", S.face_material("pisonet")} | {grille_mat(g) for g in GRILLE_STYLES}
# The flat roofs repeated as wallpaper from above (owner: "not only ground but the flat roofs"):
# rotated, feathered extra samples (tools/ilalim_antitile.py).
ANTI_TILE = {"east_roof"}
EMIT = {S.face_material("pcx"): 0.22, S.face_material("dental"): 0.4, "east_int_pc": 0.25,
        S.face_material("manok_pylon"): 0.35}


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tint, strength, _tile = MATERIALS[name]
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.85 if "glass" not in name else 0.35
    if tex is None:
        bsdf.inputs["Base Color"].default_value = (*tint, 1)
        m.diffuse_color = (*tint, 1)
        return m
    uv = nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = bpy.data.images.load(str(TEXTURES / f"{tex}_albedo.png"), check_existing=True)
    links.new(uv.outputs["UV"], albedo.inputs["Vector"])
    colour = albedo.outputs["Color"]
    if name in ANTI_TILE:
        colour = anti_tile(nodes, links, uv.outputs["UV"], albedo.image, colour)
    if tint is not None:
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*tint, 1)
        colour = mix.outputs[2]
    if name in GRIMED:
        for image, layer in (("east_grime_drips", "UVGrime"), ("east_grime_splash", "UVSplash")):
            guv = nodes.new("ShaderNodeUVMap")
            guv.uv_map = layer
            gtex = nodes.new("ShaderNodeTexImage")
            gtex.image = bpy.data.images.load(str(TEXTURES / f"{image}.png"), check_existing=True)
            gtex.image.colorspace_settings.name = "Non-Color"
            gtex.extension = "EXTEND" if layer == "UVSplash" else "REPEAT"
            links.new(guv.outputs["UV"], gtex.inputs["Vector"])
            mul = nodes.new("ShaderNodeMix")
            mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
            mul.inputs["Factor"].default_value = 1.0
            links.new(colour, mul.inputs[6])
            links.new(gtex.outputs["Color"], mul.inputs[7])
            colour = mul.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    if name in EMIT:
        links.new(colour, bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = EMIT[name]
    if name in ALPHA:
        links.new(albedo.outputs["Alpha"], bsdf.inputs["Alpha"])
        try:
            m.surface_render_method = "DITHERED"
        except AttributeError:
            m.blend_method = "HASHED"
    hpath = TEXTURES / f"{tex}_normal.png"
    if strength and hpath.exists():
        normal = nodes.new("ShaderNodeTexImage")
        normal.image = bpy.data.images.load(str(hpath), check_existing=True)
        normal.image.colorspace_settings.name = "Non-Color"
        links.new(uv.outputs["UV"], normal.inputs["Vector"])
        nmap = nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = strength
        links.new(normal.outputs["Color"], nmap.inputs["Color"])
        links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    avg = (0.75, 0.72, 0.68) if tint is None else tuple(0.85 * c for c in tint)
    m.diffuse_color = (*avg, 1)
    return m


# ------------------------------------------------------------------ geometry

fillet, rounded_rect = L.fillet, L.rounded_rect


def area(poly):
    return 0.5 * sum(a[0] * b[1] - b[0] * a[1] for a, b in zip(poly, poly[1:] + poly[:1]))


def ccw(poly):
    return poly if area(poly) > 0 else list(reversed(poly))


def offset(poly, d):
    """Miter offset of a CCW polygon, outward by d (inward when negative), miters limited."""
    n = len(poly)
    out = []
    for i in range(n):
        p0, p1, p2 = Vector(poly[i - 1]), Vector(poly[i]), Vector(poly[(i + 1) % n])
        e0, e1 = (p1 - p0), (p2 - p1)
        if e0.length < 1e-6 or e1.length < 1e-6:
            out.append(tuple(p1))
            continue
        n0 = Vector((e0.y, -e0.x)).normalized()
        n1 = Vector((e1.y, -e1.x)).normalized()
        m = n0 + n1
        if m.length < 1e-6:
            m = n0
        m.normalize()
        k = d / max(0.35, m.dot(n0))
        q = p1 + m * k
        out.append((q.x, q.y))
    return out


def clip_x(poly, cut):
    """The part of a CCW polygon east of x = cut (Sutherland-Hodgman). Cutting, not clamping:
    clamping x slid the corner along and skewed the side walls (the lugawan board sank into the
    Padre Faura wall, review v6)."""
    out = []
    for k in range(len(poly)):
        (ax, ay), (bx, by) = poly[k - 1], poly[k]
        a_in, b_in = ax >= cut, bx >= cut
        if b_in:
            if not a_in:
                out.append((cut, ay + (by - ay) * (cut - ax) / (bx - ax)))
            out.append((bx, by))
        elif a_in:
            out.append((cut, ay + (by - ay) * (cut - ax) / (bx - ax)))
    clean = []
    for q in out:
        if not clean or math.dist(q, clean[-1]) > 0.05:
            clean.append(q)
    if len(clean) > 1 and math.dist(clean[0], clean[-1]) < 0.05:
        clean.pop()
    return ccw(clean)


def edge_normals(poly):
    """Outward normal and midpoint of each edge i (poly[i] -> poly[i+1]) of a CCW polygon."""
    res = []
    for a, b in zip(poly, poly[1:] + poly[:1]):
        a, b = Vector(a), Vector(b)
        e = b - a
        if e.length < 1e-6:
            res.append((Vector((1, 0)), a, 0.0))
            continue
        res.append((Vector((e.y, -e.x)).normalized(), (a + b) / 2, e.length))
    return res


class EBuf:
    """One object's geometry in a single bmesh, with material indices, world or banded UVs and
    the grime UV maps. The eastside twin of the guideway kit's Buf."""

    def __init__(self, name, drip_top=None, splash=False):
        self.name, self.bm, self.mats = name, bmesh.new(), []
        self.drip_top, self.splash = drip_top, splash
        self.uvk = self.bm.faces.layers.int.new("uvk")
        self.specs = []

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def _spec(self, faces, spec):
        self.specs.append(spec)
        for f in faces:
            f[self.uvk] = len(self.specs)

    def band(self, faces, v0, vh):
        """World u, but v = (z - v0) / vh: a storey-aligned facade drawing."""
        self._spec(faces, ("band", v0, vh))

    def along_y(self, faces):
        """u along world y (awning stripes run down the slope)."""
        self._spec(faces, ("y",))

    def along_t(self, faces, t):
        """u along a plan direction t (an awning on any edge: its stripes run down the slope)."""
        self._spec(faces, ("t", t.x, t.y))

    def face(self, verts, mat):
        f = self.bm.faces.new(verts)
        f.material_index = self.mi(mat)
        return f

    def quad(self, pts, mat, uvs=None):
        vs = [self.bm.verts.new(p) for p in pts]
        f = self.face(vs, mat)
        if uvs is not None:
            self._spec([f], ("fixed", list(uvs)))
        return f

    def loft(self, rings, mat, cap=True, mat_of=None):
        vs = [[self.bm.verts.new(c) for c in ring] for ring in rings]
        k = len(rings[0])
        faces = []
        for r0, r1 in zip(vs, vs[1:]):
            for j in range(k):
                faces.append(self.bm.faces.new((r0[j], r0[(j + 1) % k], r1[(j + 1) % k], r1[j])))
        if cap:
            faces.append(self.bm.faces.new(list(reversed(vs[0]))))
            faces.append(self.bm.faces.new(vs[-1]))
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        idx = self.mi(mat)
        for f in faces:
            f.material_index = self.mi(mat_of(f)) if mat_of else idx
        return faces

    def rings_z(self, poly, levels, seg_mat, seg_band=None, cap_top=None, cap_bottom=True):
        """A wall up a CCW footprint: `levels` is [(offset, z)], one ring each, shared between
        segments. seg_mat(i, edge) gives segment i's material on footprint edge `edge`;
        seg_band(i) gives (v0, vh) or None. Returns the top ring's verts."""
        rings = []
        for d, z in levels:
            rings.append([self.bm.verts.new((x, y, z)) for x, y in (offset(poly, d) if d else poly)])
        k = len(poly)
        for i, (r0, r1) in enumerate(zip(rings, rings[1:])):
            band = seg_band(i) if seg_band else None
            faces = []
            for j in range(k):
                f = self.bm.faces.new((r0[j], r0[(j + 1) % k], r1[(j + 1) % k], r1[j]))
                f.material_index = self.mi(seg_mat(i, j))
                faces.append(f)
            if band:
                self.band(faces, *band)
        if cap_bottom:
            f = self.bm.faces.new(list(reversed(rings[0])))
            f.material_index = self.mi(seg_mat(0, 0))
        if cap_top:
            f = self.bm.faces.new(rings[-1])
            f.material_index = self.mi(cap_top)
        return rings[-1]

    def annulus(self, outer, inner, z0, z1, mat, mat_top=None, band=None):
        """A solid horizontal ring between two CCW loops of equal length (a slab band, a balcony
        parapet, a roof parapet)."""
        k = len(outer)
        o0 = [self.bm.verts.new((x, y, z0)) for x, y in outer]
        o1 = [self.bm.verts.new((x, y, z1)) for x, y in outer]
        i0 = [self.bm.verts.new((x, y, z0)) for x, y in inner]
        i1 = [self.bm.verts.new((x, y, z1)) for x, y in inner]
        side, flat = [], []
        for j in range(k):
            n = (j + 1) % k
            side.append(self.face((o0[j], o0[n], o1[n], o1[j]), mat))
            side.append(self.face((i0[n], i0[j], i1[j], i1[n]), mat))
            flat.append(self.face((o1[j], o1[n], i1[n], i1[j]), mat_top or mat))
            flat.append(self.face((o0[n], o0[j], i0[j], i0[n]), mat))
        if band:
            self.band(side, *band)
        return side

    def extrude_y(self, profile_xz, y0, y1, mat, mat_of=None, xform=None):
        rings = []
        for y in (y0, y1):
            ring = [Vector((x, y, z)) for x, z in profile_xz]
            if xform:
                ring = [xform @ v for v in ring]
            rings.append(ring)
        return self.loft(rings, mat, mat_of=mat_of)

    def extrude_x(self, profile_yz, x0, x1, mat, mat_of=None):
        rings = [[Vector((x, y, z)) for y, z in profile_yz] for x in (x0, x1)]
        return self.loft(rings, mat, mat_of=mat_of)

    def extrude_z(self, profile_xy, z0, z1, mat, top_scale=1.0, offset=(0.0, 0.0), lean=(0.0, 0.0)):
        ox, oy = offset
        bottom = [Vector((ox + x, oy + y, z0)) for x, y in profile_xy]
        top = [Vector((ox + lean[0] + x * top_scale, oy + lean[1] + y * top_scale, z1)) for x, y in profile_xy]
        return self.loft([bottom, top], mat)

    def box(self, c, size, mat, r=0.04, rot=0.0):
        """A rounded box centred at c, `size` (x, y, z), turned by `rot` about Z."""
        n = len(self.bm.verts)
        self.extrude_z(rounded_rect(size[0] / 2, size[1] / 2, min(r, size[0] / 2.2, size[1] / 2.2)),
                       -size[2] / 2, size[2] / 2, mat)
        self.transform_new(n, Matrix.Translation(c) @ Matrix.Rotation(rot, 4, "Z"))

    def cylinder(self, c, radius, z0, z1, mat, sides=12, top_scale=1.0):
        prof = [(radius * math.cos(a), radius * math.sin(a)) for a in (k / sides * math.tau for k in range(sides))]
        return self.extrude_z(prof, z0, z1, mat, top_scale=top_scale, offset=c)

    def frame_y(self, outer, inner, y0, y1, mat, zc=0.0, xform=None):
        """A rectangular ring (a frame) extruded along Y, outer and inner loops of equal length."""
        xf = xform or Matrix.Identity(4)
        k = len(outer)

        def ring(pts, y):
            return [self.bm.verts.new(xf @ Vector((x, y, z + zc))) for x, z in pts]
        o0, o1, i0, i1 = ring(outer, y0), ring(outer, y1), ring(inner, y0), ring(inner, y1)
        faces = []
        for j in range(k):
            n = (j + 1) % k
            faces += [self.face((o0[j], o0[n], o1[n], o1[j]), mat), self.face((i0[n], i0[j], i1[j], i1[n]), mat),
                      self.face((o1[j], o1[n], i1[n], i1[j]), mat), self.face((o0[n], o0[j], i0[j], i0[n]), mat)]
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)

    def tube(self, path, radius, mat, sides=8):
        rings = []
        for i, p in enumerate(path):
            d = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
            side = d.cross(UP)
            if side.length < 1e-4:
                side = d.cross(Vector((1, 0, 0)))
            side.normalize()
            up = side.cross(d).normalized()
            rings.append([p + (side * math.cos(a) + up * math.sin(a)) * radius
                          for a in (k / sides * math.tau for k in range(sides))])
        return self.loft(rings, mat)

    def blob(self, center, radii, mat, subdiv=2):
        r = bmesh.ops.create_icosphere(self.bm, subdivisions=subdiv, radius=1.0,
                                       matrix=Matrix.Translation(center) @ Matrix.Diagonal((*radii, 1)))
        idx = self.mi(mat)
        for f in {f for v in r["verts"] for f in v.link_faces}:
            f.material_index = idx
            f.smooth = True

    def solidify_back(self, t, mat=None):
        """Give every face so far a back face t metres behind it along -Y (cloth, tin sheet)."""
        faces = list(self.bm.faces)
        for f in faces:
            vs = [self.bm.verts.new(v.co + Vector((0, -t, 0))) for v in reversed(f.verts)]
            nf = self.bm.faces.new(vs)
            nf.material_index = self.mi(mat) if mat else f.material_index

    def transform_new(self, n, m):
        self.bm.verts.ensure_lookup_table()
        vs = [self.bm.verts[i] for i in range(n, len(self.bm.verts))]
        if vs:
            bmesh.ops.transform(self.bm, matrix=m, verts=vs)

    def transform_all(self, m):
        bmesh.ops.transform(self.bm, matrix=m, verts=list(self.bm.verts))

    # ---------------------------------------------------------- UVs

    def uvs(self):
        uv = self.bm.loops.layers.uv.new("UVMap")
        drip = self.bm.loops.layers.uv.new("UVGrime")
        splash = self.bm.loops.layers.uv.new("UVSplash")
        clean = 0.999
        for f in self.bm.faces:
            mat = self.mats[f.material_index] if f.material_index < len(self.mats) else None
            tile = MATERIALS.get(mat, (None, None, 0, (4, 4)))[3] or (1, 1)
            n = f.normal
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            k = f[self.uvk]
            spec = self.specs[k - 1] if k else None
            for i, l in enumerate(f.loops):
                co = l.vert.co
                if spec and spec[0] == "fixed":
                    l[uv].uv = spec[1][i]
                elif spec and spec[0] == "band":
                    l[uv].uv = (co.dot(t) / tile[0], (co.z - spec[1]) / spec[2])
                elif spec and spec[0] == "y":
                    l[uv].uv = (co.y / tile[0], (co.x + co.z) / tile[1])
                elif spec and spec[0] == "t":
                    tx, ty = spec[1], spec[2]
                    l[uv].uv = ((co.x * tx + co.y * ty) / tile[0], (co.x * ty - co.y * tx + co.z) / tile[1])
                elif abs(n.z) > 0.7:
                    l[uv].uv = (co.x / tile[0], co.y / tile[1])
                else:
                    l[uv].uv = (co.dot(t) / tile[0], co.z / tile[1])
            cz = f.calc_center_median().z
            side = abs(n.z) <= 0.7
            top = self.drip_top(cz) if callable(self.drip_top) else self.drip_top
            for l in f.loops:
                co = l.vert.co
                if side and top is not None:
                    l[drip].uv = (co.dot(t) / 8.0, min(clean, max(0.0, (top - co.z) / GRIME_H)))
                else:
                    l[drip].uv = (co.x / 8.0, clean)
                if side and self.splash:
                    l[splash].uv = (co.dot(t) / 8.0, min(clean, max(0.0, (co.z - PAVE_TOP + 0.1) / SPLASH_H)))
                else:
                    l[splash].uv = (co.x / 8.0, clean)

    def finish(self, collection, bevel=0.04, segments=2, smooth=True, local=False):
        self.uvs()
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        if "uvk" in mesh.attributes:
            mesh.attributes.remove(mesh.attributes["uvk"])
        for m in self.mats:
            mesh.materials.append(material(m))
        for p in mesh.polygons:
            p.use_smooth = smooth and p.area < 0.3
        obj = bpy.data.objects.new(self.name, mesh)
        collection.objects.link(obj)
        if bevel:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments = bevel, segments
            mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
            mod.harden_normals, mod.use_clamp_overlap = True, True
        return obj


def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(c)
    return c


def drip_levels(edges):
    """A drip_top function: the lowest slab or parapet edge above a face's centre."""
    edges = sorted(edges)

    def top(cz):
        for e in edges:
            if e > cz + 0.02:
                return e
        return None
    return top


# ------------------------------------------------------------------ the layout

def load_layout():
    import json
    return json.loads(LAYOUT.read_text(encoding="utf-8"))


def prepare(poly):
    """The footprint as the kit builds it: Taft frontage pinned to x = 11.0, grown 8 cm so party
    walls cross, corners filleted, CCW."""
    pts = [tuple(p) for p in poly]
    minx = min(x for x, _ in pts)
    if minx < 14.5:
        pts = [(FRONT if x < minx + 2.6 else x, y) for x, y in pts]
    clean = []
    for p in pts:
        if not clean or math.dist(p, clean[-1]) > 0.3:
            clean.append(p)
    if math.dist(clean[0], clean[-1]) < 0.3:
        clean.pop()
    clean = ccw(clean)
    grown = offset(clean, 0.08)
    grown = [(max(FRONT, x) if minx < 14.5 else x, y) for x, y in grown]
    return ccw(fillet(grown, 0.45, 2))


# ------------------------------------------------------------------ building parts

def roof_kit(buf, poly, z, rng, tanks=2, house=True, parapet_mat="east_concrete"):
    """The roof: parapet, a stair house, water tanks on stands, a vent."""
    buf.annulus(offset(poly, 0.06), offset(poly, -0.22), z - 0.06, z + 1.0, parapet_mat, mat_top="east_slab")
    xs, ys = [p[0] for p in poly], [p[1] for p in poly]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    w, d = max(xs) - min(xs), max(ys) - min(ys)
    if house and w > 7 and d > 7:
        hx, hy = cx + rng.uniform(-0.2, 0.2) * w, cy + rng.uniform(-0.2, 0.2) * d
        buf.box((hx, hy, z + 1.4), (3.2, 2.6, 2.9), parapet_mat, r=0.12)
        buf.box((hx, hy, z + 2.95), (3.6, 3.0, 0.22), "east_slab", r=0.08)
        buf.box((hx + 1.1, hy - 1.33, z + 1.0), (0.9, 0.12, 2.0), "east_metal", r=0.03)
    for k in range(tanks):
        tx = cx + rng.uniform(-0.3, 0.3) * max(w - 4, 1)
        ty = cy + rng.uniform(-0.3, 0.3) * max(d - 4, 1)
        tank(buf, (tx, ty), z, rng)


def tank(buf, at, z, rng):
    r = rng.choice((0.55, 0.65, 0.75))
    h = rng.choice((1.3, 1.6))
    buf.box((at[0], at[1], z + 0.35), (2 * r + 0.3, 2 * r + 0.3, 0.72), "east_concrete", r=0.08)
    buf.cylinder(at, r, z + 0.69, z + 0.69 + h, rng.choice(("east_tank", "east_tank", "east_tank_dark")), sides=14)
    buf.cylinder(at, r * 0.55, z + 0.67 + h, z + 0.84 + h, "east_steel_light", sides=12, top_scale=0.8)


def ac_box(buf, p, n, z, rng=None):
    """A window aircon standing out of a wall at plan point p with outward normal n: a chunky
    casing and its louvred front (fixed UVs on east_ac)."""
    t = Vector((-n.y, n.x))
    w, d, h = 0.68, 0.5, 0.44
    c = Vector((p.x + n.x * (d / 2 - 0.12), p.y + n.y * (d / 2 - 0.12), z + h / 2))
    ang = math.atan2(n.y, n.x)
    buf.box(c, (d, w, h), "east_ac_body", r=0.05, rot=ang)
    f0 = Vector((p.x + n.x * (d - 0.11), p.y + n.y * (d - 0.11)))
    a, b = f0 + t * (w / 2 - 0.035), f0 - t * (w / 2 - 0.035)
    buf.quad([Vector((a.x, a.y, z + 0.03)), Vector((b.x, b.y, z + 0.03)), Vector((b.x, b.y, z + h - 0.03)),
              Vector((a.x, a.y, z + h - 0.03))], "east_ac", [(0, 0), (1, 0), (1, 1), (0, 1)])


def grille(buf, a, b, n, z0, z1, gap=0.07):
    """A window grille in front of a wall run a..b (plan points), `gap` out along n."""
    a3 = Vector((a.x + n.x * gap, a.y + n.y * gap))
    b3 = Vector((b.x + n.x * gap, b.y + n.y * gap))
    reps = max(1, round((b - a).length / 1.2))
    buf.quad([Vector((a3.x, a3.y, z0)), Vector((b3.x, b3.y, z0)), Vector((b3.x, b3.y, z1)), Vector((a3.x, a3.y, z1))],
             "east_grille", [(0, 0), (reps, 0), (reps, 1), (0, 1)])


def window_grilles(buf, poly, bases, rng, prob=0.4, gap=0.06, mat="east_grille", busy=None):
    """Grilles over the windows drawn in east_lod_punched (and the east_fac_punched_* drawings,
    which keep its window rhythm): the drawing puts a window centre at s = 1, 3 and 5 m (mod 6)
    along each wall, where s is the world u of the facade UVs, and from 1.0 to 2.5 m over each
    floor. A grille is placed over some of those windows only, never where `busy(edge, s)` says a
    bay or balcony stands."""
    for j, (a, b) in enumerate(zip(poly, poly[1:] + poly[:1])):
        a, b = Vector(a), Vector(b)
        e = b - a
        if e.length < 2.0:
            continue
        n = Vector((e.y, -e.x)).normalized()
        t = Vector((-n.y, n.x))
        sa, sb = a.dot(t), b.dot(t)
        lo, hi = min(sa, sb) + 0.75, max(sa, sb) - 0.75
        k0 = math.floor(lo / 2.0)
        for k in range(k0, int(math.ceil(hi / 2.0)) + 1):
            sc = 2.0 * k + 1.0
            if not (lo <= sc <= hi):
                continue
            f = (sc - sa) / (sb - sa)
            c = a + e * f
            for z0 in bases:
                if rng.random() < prob and not (busy and busy(j, f * e.length, z0)):
                    buf.quad([Vector((q.x, q.y, z)) for q, z in
                              ((c - t * 0.68 + n * gap, z0 + 0.98), (c + t * 0.68 + n * gap, z0 + 0.98),
                               (c + t * 0.68 + n * gap, z0 + 2.52), (c - t * 0.68 + n * gap, z0 + 2.52))],
                             mat, [(0, 0), (1, 0), (1, 1), (0, 1)])


def laundry(buf, a, b, z, rng):
    """A drying line across a balcony with a few flat pieces of washing pegged on it."""
    buf.tube([Vector((a[0], a[1], z)), Vector((b[0], b[1], z))], 0.012, "east_sign_rope", sides=5)
    d = Vector((b[0] - a[0], b[1] - a[1]))
    n = int(d.length / 0.55)
    for i in range(n):
        if rng.random() < 0.3:
            continue
        p = Vector((a[0], a[1])) + d * ((i + 0.5) / n)
        w, h = rng.uniform(0.3, 0.5), rng.uniform(0.35, 0.7)
        buf.box((p.x, p.y, z - h / 2 + 0.02), (0.04, w, h), rng.choice(("east_cloth_a", "east_cloth_b", "east_cloth_c",
                                                                       "east_cloth_d", "east_cloth_e")), r=0.015,
                rot=math.atan2(d.y, d.x) - math.pi / 2)


def planter(buf, c, length, rng, along="y"):
    """A hollow concrete planter box with three round shrub clumps sunk into its soil."""
    sx, sy = (0.5, length) if along == "y" else (length, 0.5)
    buf.box((c[0], c[1], c[2] + 0.22), (sx, sy, 0.44), "east_concrete", r=0.06)
    buf.box((c[0], c[1], c[2] + 0.4), (sx - 0.12, sy - 0.12, 0.06), "east_soil", r=0.03)
    for k in range(3):
        t = (k + 0.5) / 3 - 0.5
        px = c[0] + (0 if along == "y" else t * length)
        py = c[1] + (t * length if along == "y" else 0)
        rr = rng.uniform(0.26, 0.38)
        buf.blob(Vector((px, py, c[2] + 0.45 + rr * 0.5)), (rr, rr, rr * 0.85),
                 rng.choice(("east_foliage", "east_foliage_lt")), subdiv=1)


# ------------------------------------------------------------------ facade identity and traits

SARI_LOT = (11.8, 22.8, 38.3, 47.3)     # the corner store's lot (sarisari kit): nothing enters it


def inside(p, poly):
    x, y = p[0], p[1]
    c = False
    for (x0, y0), (x1, y1) in zip(poly[-1:] + poly[:-1], poly):
        if (y0 > y) != (y1 > y) and x < x0 + (y - y0) * (x1 - x0) / (y1 - y0):
            c = not c
    return c


def read_boxes(path, max_plan=8.0):
    """World boxes of the small mesh objects east of Taft in another kit's .blend (poles, signals,
    tree trunks and crowns), linked for the reading and unlinked again, so eastside.blend keeps no
    reference to it. World matrices are rebuilt from the saved parent chain."""
    if not path.exists():
        return []
    before = set(bpy.data.libraries)
    with bpy.data.libraries.load(str(path), link=True) as (src, dst):
        dst.objects = list(src.objects)

    def world(o):
        m = o.matrix_basis.copy()
        while o.parent is not None:
            m = o.parent.matrix_basis @ o.matrix_parent_inverse @ m
            o = o.parent
        return m
    boxes = []
    for o in dst.objects:
        if o is None or o.type != "MESH" or not o.data.vertices:
            continue
        m = world(o)
        lo, hi = Vector((1e9, 1e9, 1e9)), Vector((-1e9, -1e9, -1e9))
        for c in o.bound_box:
            w = m @ Vector(c)
            lo, hi = Vector(map(min, lo, w)), Vector(map(max, hi, w))
        if hi.x < 9.0 or hi.x - lo.x > max_plan or hi.y - lo.y > max_plan:
            continue
        boxes.append((lo.x - 0.25, hi.x + 0.25, lo.y - 0.25, hi.y + 0.25, lo.z, hi.z + 0.2))
    for lib in list(bpy.data.libraries):
        if lib not in before:
            bpy.data.libraries.remove(lib)
    return boxes


class Site:
    """What a facade trait must stay clear of: the other buildings' footprints, the corner store's
    lot, and (when those kits exist) the street kit's poles and signals and the tree kit's trunks
    and crowns. Taft's own poles are the street kit's too."""

    def __init__(self, polys):
        self.polys = polys
        x0, x1, y0, y1 = SARI_LOT
        self.boxes = [(x0 - 0.4, x1 + 0.4, y0 - 0.4, y1 + 0.4, -5.0, 99.0)]
        for blend in ("street.blend", "trees.blend"):
            got = read_boxes(SOURCE / blend)
            print("[east] obstacles from", blend, len(got))
            self.boxes += got

    def free(self, pts, z0, z1, own):
        for p in pts:
            for k, poly in self.polys.items():
                if k != own and inside(p, poly):
                    return False
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        ax0, ax1, ay0, ay1 = min(xs), max(xs), min(ys), max(ys)
        return not any(ax0 < bx1 and ax1 > bx0 and ay0 < by1 and ay1 > by0 and z0 < bz1 and z1 > bz0
                       for bx0, bx1, by0, by1, bz0, bz1 in self.boxes)

    def exposed(self, poly, own, min_len=3.5):
        """The street-facing edges of a footprint: (index, a, t, n, length) for every edge whose
        ground 2.5 m out is not another building."""
        out = []
        for j, (a, b) in enumerate(zip(poly, poly[1:] + poly[:1])):
            a, b = Vector(a), Vector(b)
            e = b - a
            if e.length < min_len:
                continue
            t = e.normalized()
            n = Vector((t.y, -t.x))
            probes = [a + t * (e.length * f) + n * 2.5 for f in (0.2, 0.5, 0.8)]
            if any(inside(p, q) for p in probes for k, q in self.polys.items() if k != own):
                continue
            out.append((j, a, t, n, e.length))
        return out


class Look:
    """One building's identity: its paint (a PALETTE key), trims, grille drawing, shutter paint,
    awning, balcony parapet and how busy its facade is. `detail` 2 is the front row (what the court
    and the two streets see), 1 the second row, 0 the far LOD blocks (paint and drawings only)."""

    def __init__(self, key, rng, detail):
        self.key, self.detail = key, detail
        self.wall, self.trim = f"east_paint_{key}", f"east_trim_{key}"
        self.grille = grille_mat(rng.choice(GRILLE_STYLES))
        self.shutter = shutter_mat(rng.choice(list(SHUTTER_PAINTS)))
        self.awning = rng.choice(("canvas_green", "canvas_maroon", "tin_green", "tin_maroon", "tin_grey", None))
        self.rail = rng.choice(("solid", "rail", "rail"))
        self.parapet = rng.choice((self.wall, self.trim))
        self.ac_p = rng.uniform(0.25, 0.6)
        self.grille_p = rng.uniform(0.25, 0.55)
        self.bays_from = rng.choice((0, 0, 1))          # bays corbel out from the 1st or the 2nd floor
        self.sign = None
        self.rng = rng


def _pt(a, t, n, s, o):
    return a + t * s + n * o


def _v3(p, z):
    return Vector((p.x, p.y, z))


def bay(buf, a, t, n, s, w, depth, zb, zt, floor_edges, fac, cap):
    """A projecting bay (a box window) standing out of the wall `depth` m, sunk 12 cm into it,
    from zb to zt, its walls carrying the building's facade drawing aligned to each floor, a slab
    under it and a cap on it."""
    rect = [_pt(a, t, n, s - w / 2, -0.12), _pt(a, t, n, s + w / 2, -0.12), _pt(a, t, n, s + w / 2, depth),
            _pt(a, t, n, s - w / 2, depth)]
    rect = ccw(fillet([(p.x, p.y) for p in rect], 0.16, 2))
    zs = [zb] + [z for z in floor_edges if zb + 0.4 < z < zt - 0.4] + [zt]
    bases = sorted(floor_edges)

    def band(i):
        z = zs[i] + 0.01
        return (max([e for e in bases if e <= z] or [zb]), 3.2)
    buf.rings_z(rect, [(0.0, z) for z in zs], lambda i, j: fac, seg_band=band, cap_bottom=False)
    buf.extrude_z(offset(rect, 0.07), zb - 0.19, zb + 0.03, "east_slab")
    buf.extrude_z(offset(rect, 0.09), zt - 0.05, zt + 0.19, cap)


def balcony(buf, kit, a, t, n, s, w, d, z0, look, rng):
    """A balcony: a chunky slab sunk 12 cm into the wall, a solid parapet or a painted grille card
    under a chunky rail, a pot or two, and sometimes a washing line on two rods."""
    rot = math.atan2(t.y, t.x)
    c = _pt(a, t, n, s, (d - 0.12) / 2)
    buf.box((c.x, c.y, z0 + 0.02), (w, d + 0.12, 0.24), "east_slab", r=0.06, rot=rot)
    if look.rail == "solid":
        f = _pt(a, t, n, s, d - 0.09)
        buf.box((f.x, f.y, z0 + 0.62), (w - 0.04, 0.14, 0.98), look.parapet, r=0.05, rot=rot)
        for sg in (-1, 1):
            p = _pt(a, t, n, s + sg * (w / 2 - 0.1), (d - 0.12) / 2 - 0.03)
            buf.box((p.x, p.y, z0 + 0.59), (0.14, d - 0.1, 0.94), look.parapet, r=0.05, rot=rot)
    else:
        lo, hi = z0 + 0.13, z0 + 1.0
        l0, l1 = _pt(a, t, n, s - w / 2 + 0.07, d - 0.08), _pt(a, t, n, s + w / 2 - 0.07, d - 0.08)
        w0, w1 = _pt(a, t, n, s - w / 2 + 0.07, 0.0), _pt(a, t, n, s + w / 2 - 0.07, 0.0)
        reps = max(1, round((w - 0.14) / 1.2))
        for p, q, u in ((l0, l1, reps), (w0, l0, 1), (l1, w1, 1)):
            kit.quad([_v3(p, lo), _v3(q, lo), _v3(q, hi), _v3(p, hi)], look.grille, [(0, 0), (u, 0), (u, 1), (0, 1)])
        for p, q in ((w0, l0), (l0, l1), (l1, w1)):
            kit.tube([_v3(p, hi + 0.03), _v3(q, hi + 0.03)], 0.045, "east_rail", sides=8)
        for p in (l0, l1):
            kit.box((p.x, p.y, (lo + hi) / 2), (0.1, 0.1, hi - lo + 0.04), "east_rail", r=0.03, rot=rot)
    # Pots: a clay, white or green pot with a round shrub, inside the front corners.
    for k in range(rng.choice((0, 1, 1, 2))):
        sg = (-1, 1)[k] if rng.random() < 0.5 else (1, -1)[k]
        p = _pt(a, t, n, s + sg * (w / 2 - 0.38), d - 0.36)
        pr = rng.uniform(0.13, 0.17)
        kit.cylinder((p.x, p.y), pr, z0 + 0.12, z0 + 0.46, rng.choice(("east_pot_clay", "east_pot_white", "east_pot_green")),
                     sides=10, top_scale=1.18)
        rr = rng.uniform(0.2, 0.3)
        kit.blob(Vector((p.x, p.y, z0 + 0.44 + rr * 0.55)), (rr, rr, rr * 0.85),
                 rng.choice(("east_foliage", "east_foliage_lt")), subdiv=1)
    if rng.random() < 0.45:
        # The washing line runs between two rods that stand on the parapet's front corners.
        p0, p1 = _pt(a, t, n, s - w / 2 + 0.12, d - 0.1), _pt(a, t, n, s + w / 2 - 0.12, d - 0.1)
        for p in (p0, p1):
            kit.tube([_v3(p, z0 + 0.95), _v3(p, z0 + 2.08)], 0.035, "east_rail", sides=6)
        laundry(kit, (p0.x, p0.y), (p1.x, p1.y), z0 + 2.0, rng)
    if rng.random() < look.ac_p * 0.5:
        ac_box(kit, _pt(a, t, n, s + rng.uniform(-0.25, 0.25) * w, 0.0), n, z0 + 0.15)


AWNING_MAT = {"canvas_green": "east_canvas_green", "canvas_maroon": "east_canvas_maroon", "tin_green": "east_tin_green",
              "tin_maroon": "east_tin_maroon", "tin_grey": "east_tin_grey"}


def edge_awning(kit, a, t, n, s0, s1, depth, zw, ze, kind, wall_o=-0.45):
    """The shop row's awnings on any edge: a sloped canvas with a deep valance on a chunky frame,
    or a corrugated tin sheet with a rolled edge on steel brackets. Starts `wall_o` out (inside the
    recessed ground-floor wall)."""
    mat, th = AWNING_MAT[kind], 0.035
    if kind.startswith("canvas"):
        prof = [(wall_o, zw), (depth, ze), (depth + 0.02, ze - 0.32), (depth - th, ze - 0.32), (depth - th, ze - th),
                (wall_o, zw - th)]
    else:
        prof = [(wall_o, zw), (depth, ze), (depth, ze - th), (wall_o, zw - th)]
    rings = [[Vector((p.x, p.y, z)) for p, z in ((_pt(a, t, n, s, o), z) for o, z in prof)] for s in (s0, s1)]
    faces = kit.loft(rings, mat)
    kit.along_t([f for f in faces if abs(f.normal.dot(Vector((t.x, t.y, 0)))) < 0.9], t)
    if kind.startswith("tin"):
        kit.tube([_v3(_pt(a, t, n, s0, depth - 0.02), ze - 0.02), _v3(_pt(a, t, n, s1, depth - 0.02), ze - 0.02)],
                 0.045, mat, sides=8)
    for s in (s0 + 0.3, s1 - 0.3):
        kit.tube([_v3(_pt(a, t, n, s, wall_o + 0.03), zw - 0.6), _v3(_pt(a, t, n, s, depth - 0.3), ze - 0.06)],
                 0.035, "east_metal", sides=8)


def facade_traits(buf, kit, signs, poly, own, edges, style, look, site, storey=3.2, shops=True):
    """Dress a mid-rise's street faces with its own vocabulary, deterministic per building:
    projecting bays and stacked balconies in columns (never in random scatter: Manila facades are
    built in bays), ground-floor awnings between the shop piers, and a hand-painted blade sign on
    the front row. Returns busy(edge, s, z): whether a trait stands at s along edge `edge` on the
    floor starting at z, so aircons and grilles keep clear."""
    rng = look.rng
    floors = len(edges) - 1
    roof = edges[-1]
    z_g = edges[0]
    taken = {}

    def mark(j, s0, s1, z0, z1):
        taken.setdefault(j, []).append((s0, s1, z0, z1))

    def busy(j, s, z=None):
        return any(s0 - 0.5 < s < s1 + 0.5 and (z is None or z0 - 0.5 < z < z1)
                   for s0, s1, z0, z1 in taken.get(j, []))
    if look.detail == 0 or floors < 1:
        return busy
    fac = f"east_fac_{'ribbon' if style == 'ribbon' else 'punched'}_{look.key}"
    exp = site.exposed(poly, own)
    if look.detail == 1:
        exp = sorted(exp, key=lambda r: -r[4])[:2]
    for j, a, t, n, L in exp:
        taft = n.x < -0.8 and (a + t * (L / 2)).x < 12.5
        # Slots across the edge: between the fins on a fins facade, else about every 3.4 m.
        if style == "fins":
            m = int(L / 3.5)
            if m < 1:
                continue
            marks = [L * i / (m + 1) for i in range(m + 2)]
            slots = [((p + q) / 2, (q - p) - 0.75) for p, q in zip(marks, marks[1:])]
        else:
            m = max(1, int((L - 1.0) / 3.4))
            pitch = (L - 1.0) / m
            slots = [(0.5 + pitch * (i + 0.5), min(3.0, pitch - 0.7)) for i in range(m)]
        slots = [(s, min(w, 2 * (s - 0.45), 2 * (L - s - 0.45))) for s, w in slots]
        slots = [(s, w) for s, w in slots if w >= 1.7]
        pattern = rng.choice(("alternate", "columns", "mixed", "sparse")) if look.detail == 2 else "sparse"
        for i, (s, w) in enumerate(slots):
            if pattern == "alternate":
                kind = "bay" if i % 2 == 0 else "balcony"
            elif pattern == "columns":
                kind = "balcony"
            elif pattern == "mixed":
                kind = rng.choice(("bay", "balcony", "balcony", None))
            else:
                kind = "balcony" if i % 3 == 1 else None
            if kind == "bay" and (style == "balcony" or floors < 2):
                kind = "balcony"
            if kind == "balcony" and style == "balcony":
                kind = None                       # its whole perimeter is balconies already
            if kind == "bay":
                depth = 0.5 if taft else rng.choice((0.6, 0.75, 0.9))
                zb = edges[min(look.bays_from, floors - 1)] + 0.02
                top = rng.choice((floors, floors, floors - 1)) if floors > 2 else floors
                zt = roof - 0.4 if top == floors else edges[top] + 0.02
                pts = [_pt(a, t, n, s + sg * w / 2, o) for sg in (-1, 1) for o in (0.1, depth + 0.1)]
                if zt - zb > 2.0 and site.free(pts, zb - 0.2, zt + 0.2, own):
                    bay(buf, a, t, n, s, w, depth, zb, zt, edges, fac, look.trim if rng.random() < 0.5 else "east_slab")
                    mark(j, s - w / 2, s + w / 2, zb - 0.2, zt + 0.2)
            elif kind == "balcony":
                d = 0.6 if taft else rng.choice((0.9, 1.0, 1.1))
                every = rng.choice((1, 1, 2))
                for k in range(floors):
                    if (k + i) % every:
                        continue
                    z0 = edges[k]
                    pts = [_pt(a, t, n, s + sg * w / 2, o) for sg in (-1, 1) for o in (0.1, d + 0.1)]
                    if site.free(pts, z0 - 0.15, z0 + 2.3, own):
                        balcony(buf, kit, a, t, n, s, w, d, z0, look, rng)
                        mark(j, s - w / 2, s + w / 2, z0 - 0.2, z0 + storey)
        # The ground floor: awnings over some of the shop bays between the piers.
        if shops and look.awning and not taft:
            count = max(1, round(L / 4.0))
            depth = rng.uniform(1.2, 1.8)
            zw = z_g - 0.82
            ze = zw - rng.uniform(0.3, 0.42)
            for i in range(count):
                s0, s1 = L * i / count + 0.3, L * (i + 1) / count - 0.3
                if s1 - s0 < 1.5 or rng.random() > 0.65:
                    continue
                pts = [_pt(a, t, n, s, o) for s in (s0, s1) for o in (0.2, depth + 0.1)]
                if site.free(pts, ze - 0.4, zw + 0.1, own):
                    edge_awning(kit, a, t, n, s0, s1, depth, zw, ze, look.awning)
    # One hand-painted blade sign per front-row building, near a corner of a street face.
    if look.detail == 2 and look.sign and style != "balcony":
        spec = S.SIGNS[look.sign]
        base = {"hood": 0.5, "ribbon": 0.6, "fins": 0.4}.get(style, 0.5)
        zb = z_g + base
        for j, a, t, n, L in sorted(exp, key=lambda r: -r[4]):
            if (n.x < -0.8 and (a + t * (L / 2)).x < 12.5) or L < 6:
                continue
            for s in (0.55, L - 0.55):
                if busy(j, s, zb):
                    continue
                pts = [_pt(a, t, n, s + sg * 0.15, o) for sg in (-1, 1) for o in (0.25, 0.45 + spec["w"])]
                if not site.free(pts, zb - 0.1, zb + spec["h"] + 0.1, own):
                    continue
                p = _pt(a, t, n, s, 0.0)
                S.build(look.sign, EBuf, signs, Matrix.Translation((p.x, p.y, zb))
                        @ Matrix.Rotation(math.atan2(n.y, n.x) - math.pi / 2, 4, "Z"))
                mark(j, s - 0.2, s + 0.2, zb, zb + spec["h"])
                look.sign = None
                break
            if look.sign is None:
                break
    return busy


# ------------------------------------------------------------------ generic mid-rise

STYLE_TINTS = ["east_render_mint", "east_render_ochre", "east_render_rose", "east_render_cream", "east_render_grey",
               "east_render_teal", "east_render_white"]


def midrise(col, name, poly, levels, rng, style="ribbon", wall=None, storey=3.2, ground=4.0, base_z=0.0,
            ac=True, grilles=True, shops=True, tanks=2, look=None, site=None, own=None):
    """A concrete mid-rise. Styles, each a construction:
      ribbon   recessed ribbon windows wrapping every face, cantilevered slab bands.
      hood     plain walls with punched windows drawn, a deep concrete hood over each floor.
      fins     punched windows drawn, vertical concrete fins every ~3.5 m, thin sill bands.
      balcony  deep balconies on every floor, drawn sliding doors and laundry behind.
    With a `look` (facade identity) the walls take its paint and its own facade drawings, the
    ground floor its shutter paint, and facade_traits() dresses the street faces. The random
    draws that place the roof kit are unchanged, so every tank and stair house stays where it
    was (another kit builds on these roofs). Returns the roof height."""
    wall = wall or rng.choice(STYLE_TINTS)
    if look:
        wall = look.wall
    floors = max(1, int(levels) - 1)
    z_g = base_z + ground
    edges = [z_g + k * storey for k in range(floors + 1)]
    roof = edges[-1]
    buf = EBuf(name, drip_top=drip_levels([e - 0.1 for e in edges] + [roof + 1.0]), splash=True)
    kit = EBuf(name + "_kit", drip_top=roof + 1.0)
    en = edge_normals(poly)

    # Ground floor: a recessed shop band (shutters and glass alternating by edge) under a slab.
    levels_g = [(0.0, base_z - 0.3), (0.0, base_z + 0.35), (-0.4, base_z + 0.37), (-0.4, z_g - 0.75),
                (0.0, z_g - 0.73)]
    shutter = look.shutter if look else "east_shutter"
    shop_mat = lambda j: (shutter if (j * 7 + len(name)) % 3 else "east_shop_glass") if shops else wall

    def seg_g(i, j):
        return shop_mat(j) if i == 2 else wall
    buf.rings_z(poly, levels_g, seg_g, cap_bottom=False)
    buf.rings_z(poly, [(0.0, z_g - 0.73), (0.0, z_g + 0.02)], lambda i, j: wall, cap_bottom=False)
    buf.annulus(offset(poly, 0.3), offset(poly, -0.2), z_g - 0.2, z_g + 0.12, "east_slab")
    # Piers between the shop bays every 3.5 to 4.5 m, so a ground floor reads as a row of shops.
    for a, b in zip(poly, poly[1:] + poly[:1]):
        a, b = Vector(a), Vector(b)
        e = b - a
        if e.length < 3.0:
            continue
        nrm = Vector((e.y, -e.x)).normalized()
        count = max(1, round(e.length / 4.0))
        for i in range(count + 1):
            p = a + e * (i / count)
            buf.box((p.x - nrm.x * 0.1, p.y - nrm.y * 0.1, (base_z + z_g) / 2 - 0.1), (0.5, 0.5, z_g - base_z - 0.1),
                    wall, r=0.08, rot=math.atan2(e.y, e.x))

    for k in range(floors):
        z0 = edges[k]
        if style == "ribbon":
            lv = [(0.0, z0), (0.0, z0 + 0.95), (-0.28, z0 + 0.97), (-0.28, z0 + 2.55), (0.0, z0 + 2.57),
                  (0.0, z0 + storey + 0.02)]
            buf.rings_z(poly, lv, lambda i, j: "east_glass_ribbon" if i == 2 else wall,
                        seg_band=lambda i, z0=z0: (z0 + 0.97, 1.6) if i == 2 else None, cap_bottom=False)
            buf.annulus(offset(poly, 0.38), offset(poly, -0.1), z0 + storey - 0.2, z0 + storey + 0.1, "east_slab")
        elif style == "balcony":
            lm = "east_lod_balcony_a" if wall in ("east_render_rose", "east_render_ochre", "east_render_cream") \
                else "east_lod_balcony_b"
            if look:
                lm = f"east_fac_balcony_{look.key}"
            buf.rings_z(poly, [(-0.9, z0 - 0.02), (-0.9, z0 + storey + 0.02)], lambda i, j: lm,
                        seg_band=lambda i, z0=z0: (z0, 3.2), cap_bottom=False)
            buf.annulus(offset(poly, 0.05), offset(poly, -0.95), z0 - 0.1, z0 + 0.15, "east_slab")
            buf.annulus(offset(poly, 0.05), offset(poly, -0.12), z0 + 0.1, z0 + 1.1, wall, mat_top="east_slab")
        else:
            lm = "east_lod_punched_a" if wall in ("east_render_mint", "east_render_teal", "east_render_grey",
                                                   "east_render_white") else "east_lod_punched_b"
            if look:
                lm = f"east_fac_punched_{look.key}"
            buf.rings_z(poly, [(0.0, z0 - 0.02), (0.0, z0 + storey + 0.02)], lambda i, j: lm,
                        seg_band=lambda i, z0=z0: (z0, 3.2), cap_bottom=False)
            if style == "hood":
                buf.annulus(offset(poly, 0.55), offset(poly, -0.1), z0 + 2.62, z0 + 2.8, "east_slab")
            else:
                buf.annulus(offset(poly, 0.14), offset(poly, -0.1), z0 + 0.86, z0 + 0.98, "east_slab")
    # Roof cap and kit.
    top = [buf.bm.verts.new((x, y, roof + 0.02)) for x, y in offset(poly, -0.01)]
    buf.face(top, "east_roof")
    roof_kit(kit, poly, roof, rng, tanks=tanks)
    if style == "fins":
        for a, b in zip(poly, poly[1:] + poly[:1]):
            a, b = Vector(a), Vector(b)
            e = b - a
            if e.length < 4:
                continue
            nrm = Vector((e.y, -e.x)).normalized()
            for i in range(1, int(e.length / 3.5) + 1):
                p = a + e * (i / (int(e.length / 3.5) + 1))
                kit.box((p.x + nrm.x * 0.22, p.y + nrm.y * 0.22, (z_g + roof) / 2 + 0.4),
                        (0.28, 0.28, roof - z_g + 0.8), "east_slab", r=0.06, rot=math.atan2(e.y, e.x))
    busy = None
    if look and site:
        busy = facade_traits(buf, kit, bpy.data.collections["east shop signs"], poly, own, edges, style, look, site,
                             storey=storey, shops=shops)
    if ac:
        for j, (nrm, mid, length) in enumerate(en):
            if length < 3:
                continue
            for k in range(floors):
                if rng.random() < (look.ac_p if look else 0.45):
                    t = Vector((-nrm.y, nrm.x))
                    u = rng.uniform(-0.35, 0.35)
                    p = mid + t * u * length
                    if busy and busy(j, (u + 0.5) * length, edges[k]):
                        continue
                    inset = -0.28 if style == "ribbon" else (-0.9 if style == "balcony" else 0.0)
                    p = p + nrm * inset
                    zz = edges[k] + (1.05 if style != "balcony" else 1.2)
                    ac_box(kit, p, nrm, zz)
    if grilles and style in ("hood", "fins"):
        window_grilles(kit, poly, edges[:-1], rng, prob=look.grille_p if look else 0.35,
                       mat=look.grille if look else "east_grille", busy=busy)
    buf.finish(col, bevel=0.05)
    kit.finish(col, bevel=0.02)
    return roof


def lod_block(col, name, poly, levels, rng, storey=3.2, look=None):
    """A far building: one shell with a painted facade per storey, a roof, parapet and a tank. With
    a `look` the facade is its paint's own drawing of the same kind (ribbons, punched windows or
    balconies) over a ground storey of drawn shops, and the parapet takes its paint. The random
    draws are the same as before, so the tank stays put."""
    fac = rng.choice(["east_lod_ribbon_a", "east_lod_ribbon_b", "east_lod_punched_a", "east_lod_punched_b",
                      "east_lod_balcony_a", "east_lod_balcony_b"])
    ground_fac = fac
    if look:
        kind = "ribbon" if "ribbon" in fac else ("punched" if "punched" in fac else "balcony")
        fac, ground_fac = f"east_fac_{kind}_{look.key}", f"east_fac_shops_{look.key}"
    roof = 0.1 + levels * storey
    edges = [0.1 + k * storey for k in range(levels + 1)]
    buf = EBuf(name, drip_top=drip_levels([e - 0.05 for e in edges[1:]] + [roof + 0.9]), splash=True)
    for k in range(levels):
        z0 = edges[k]
        buf.rings_z(poly, [(0.0, z0 - (0.3 if k == 0 else 0.0)), (0.0, z0 + storey)],
                    lambda i, j, k=k: ground_fac if k == 0 else fac,
                    seg_band=lambda i, z0=z0: (z0, 3.2), cap_bottom=False)
    top = [buf.bm.verts.new((x, y, roof - 0.02)) for x, y in offset(poly, -0.01)]
    buf.face(top, "east_roof")
    par = rng.choice(("east_concrete", "east_render_grey", "east_render_cream"))
    if look and par != "east_concrete":
        par = look.wall if par == "east_render_grey" else look.trim
    buf.annulus(offset(poly, 0.05), offset(poly, -0.2), roof - 0.08, roof + 0.9, par, mat_top="east_slab")
    if rng.random() < 0.8:
        xs, ys = [p[0] for p in poly], [p[1] for p in poly]
        tank(buf, ((min(xs) + max(xs)) / 2 + rng.uniform(-2, 2), (min(ys) + max(ys)) / 2 + rng.uniform(-2, 2)),
             roof, rng)
    buf.finish(col, bevel=0.05)
    return roof


# ------------------------------------------------------------------ the shop row

# South to north along x = 11: (y0, y1, key, setback, front, awning).
#   front: "shutter_half" | "open_shelves" | "open_eatery" | "lobby" | "glass_pcx" | "open_pc" | "glass_shelves"
#   awning: (kind, depth, z at the wall, z at the edge) or None
SHOP_ROW = [
    (-24.3, -16.8, "cell",    0.70, "shutter_half",  ("canvas_green", 1.6, 3.35, 3.0)),
    (-16.8, -10.0, "print",   0.90, "open_shelves",  ("concrete", 1.0, 3.05, 3.05)),
    (-10.0, -3.9,  "eatery",  1.10, "open_eatery",   ("tin_maroon", 2.6, 3.4, 3.0)),
    (-3.9,  -0.3,  "lobby",   0.80, "lobby",         ("concrete", 1.2, 3.15, 3.15)),
    (-0.3,  8.6,   "pcx",     0.50, "glass_pcx",     ("metal", 0.8, 3.3, 3.3)),
    (8.6,   15.2,  "pisonet", 0.35, "open_pc",       ("tin_green", 2.0, 3.35, 2.95)),
    (15.2,  20.4,  "botika",  0.60, "glass_shelves", ("canvas_maroon", 1.4, 3.35, 3.0)),
]
WEC_G, ASTRAL_G = 4.4, 4.8              # ground storey of each podium
SPLIT_Y = -3.9                          # where the West East Center meets the Astral podium


def ground_h(y):
    return WEC_G if y < SPLIT_Y else ASTRAL_G


def east_matrix(x, y, z):
    """Place a sign on the east row: its face looks west, at the court."""
    return Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.radians(90), 4, "Z")


def awning(buf, y0, y1, spec, rng):
    kind, depth, zw, ze = spec
    xe = FRONT - depth
    # The contract: never past x = 8.4 over the pavement, and the lowest point (a canvas valance
    # hangs 0.32 under the edge, a tin edge roll 0.07) never under 2.4 m over the pavement top.
    low = ze - (0.32 if kind.startswith("canvas") else 0.07 if kind.startswith("tin") else 0.18)
    assert xe >= 8.39 and low >= PAVE_TOP + 2.4 - 1e-6, (kind, depth, ze, low)
    if kind == "concrete":
        buf.extrude_y(fillet([(xe, ze - 0.18), (FRONT + 0.1, ze - 0.18), (FRONT + 0.1, ze), (xe, ze)], 0.06, 2),
                      y0 + 0.1, y1 - 0.1, "east_slab")
        return
    if kind == "metal":
        buf.extrude_y(fillet([(xe, ze - 0.14), (FRONT + 0.08, ze - 0.14), (FRONT + 0.08, ze + 0.02),
                              (xe, ze + 0.02)], 0.05, 2), y0 + 0.15, y1 - 0.15, "east_metal")
        return
    mat = {"canvas_green": "east_canvas_green", "canvas_maroon": "east_canvas_maroon", "tin_maroon": "east_tin_maroon",
           "tin_green": "east_tin_green"}[kind]
    t = 0.035
    if kind.startswith("canvas"):
        # A sloped canvas with a deep valance, on a chunky frame that sinks into the wall.
        prof = [(FRONT + 0.05, zw), (xe, ze), (xe - 0.02, ze - 0.32), (xe + t, ze - 0.32), (xe + t, ze - t),
                (FRONT + 0.05, zw - t)]
        faces = buf.extrude_y(prof, y0 + 0.2, y1 - 0.2, mat)
        buf.along_y([f for f in faces if abs(f.normal.y) < 0.9])
        for y in (y0 + 0.3, y1 - 0.3):
            buf.tube([Vector((FRONT + 0.03, y, zw - 0.55)), Vector((xe + 0.06, y, ze - 0.05))], 0.035, "east_metal")
    else:
        # A corrugated tin sheet on two chunky steel brackets, its front edge rolled.
        prof = [(FRONT + 0.05, zw), (xe, ze), (xe, ze - t), (FRONT + 0.05, zw - t)]
        faces = buf.extrude_y(prof, y0 + 0.12, y1 - 0.12, mat)
        buf.along_y([f for f in faces if abs(f.normal.y) < 0.9])
        buf.tube([Vector((xe - 0.02, y0 + 0.12, ze - 0.02)), Vector((xe - 0.02, y1 - 0.12, ze - 0.02))], 0.045, mat)
        n = max(2, int((y1 - y0) / 2.4) + 1)
        for i in range(n):
            y = y0 + 0.4 + i * (y1 - y0 - 0.8) / (n - 1)
            buf.tube([Vector((FRONT + 0.04, y, zw - 0.7)), Vector((xe + 0.3, y, ze - 0.06))], 0.04, "east_metal")


def awning_underside(spec, x):
    kind, depth, zw, ze = spec
    xe = FRONT - depth
    return ze + (zw - ze) * (x - xe) / depth - 0.05


def shopfront(buf, glass, y0, y1, key, setback, front, rng):
    """One bay's ground floor behind the pilasters: floor step, back wall with its interior,
    partitions, ceiling, and the front itself (glass, shutter, counter, doors)."""
    g = ground_h((y0 + y1) / 2)
    xf = FRONT + setback
    back = xf + 4.2
    ceil = g - 1.15
    # The shop floor, a step above the pavement, and its ceiling.
    buf.box(((FRONT - 0.02 + back) / 2 + 0.05, (y0 + y1) / 2, 0.14), (back - FRONT + 0.12, y1 - y0 - 0.1, 0.46),
            "east_concrete", r=0.03)
    buf.box(((xf + back) / 2, (y0 + y1) / 2, ceil + 0.15), (back - xf + 0.3, y1 - y0 + 0.1, 0.3), "east_slab", r=0.03)
    interior = {"open_shelves": "east_int_shelves", "glass_shelves": "east_int_shelves", "open_eatery": "east_int_eatery",
                "open_pc": "east_int_pc", "glass_pcx": "east_int_shelves", "shutter_half": "east_int_shelves",
                "lobby": "east_int_dark"}[front]
    buf.quad([Vector((back, y1 - 0.1, 0.36)), Vector((back, y0 + 0.1, 0.36)), Vector((back, y0 + 0.1, ceil + 0.01)),
              Vector((back, y1 - 0.1, ceil + 0.01))], interior,
             [(0, 0), ((y1 - y0 - 0.2) / 4.0, 0), ((y1 - y0 - 0.2) / 4.0, (ceil - 0.36) / 3.0), (0, (ceil - 0.36) / 3.0)])
    for y in (y0 + 0.05, y1 - 0.05):
        buf.box(((xf + back) / 2, y, ceil / 2 + 0.2), (back - xf + 0.1, 0.2, ceil - 0.2), "east_wec_wall", r=0.02)
    mid = (y0 + y1) / 2
    w = y1 - y0 - 0.6
    if front in ("glass_pcx", "glass_shelves"):
        # Glass between chunky dark mullions, centre doors, a kick plate.
        # The door pair spans mid +/- 1.08: the kick plate stops at it, and a mullion stands at each
        # door edge instead of the even rhythm running through the leaves (owner: "weird doors").
        dh = 1.14
        for ya, yb in ((y0 + 0.25, mid - dh), (mid + dh, y1 - 0.25)):
            buf.box((xf + 0.02, (ya + yb) / 2, 0.55), (0.14, yb - ya, 0.4), "east_metal", r=0.03)
        n = max(2, round(w / 1.6))
        ys = [y0 + 0.3 + i * w / n for i in range(n + 1)]
        ys = [y for y in ys if abs(y - mid) > dh + 0.35] + [mid - dh, mid + dh]
        for y in ys:
            buf.box((xf, y, (0.36 + ceil) / 2), (0.16, 0.12, ceil - 0.3), "east_metal", r=0.03)
        buf.box((xf, mid, ceil - 0.05), (0.18, w + 0.1, 0.16), "east_metal", r=0.03)
        glass.quad([Vector((xf + 0.01, y1 - 0.3, 0.72)), Vector((xf + 0.01, y0 + 0.3, 0.72)),
                    Vector((xf + 0.01, y0 + 0.3, ceil - 0.1)), Vector((xf + 0.01, y1 - 0.3, ceil - 0.1))], "east_display",
                   [(0, 0), (w / 4, 0), (w / 4, (ceil - 0.8) / 3), (0, (ceil - 0.8) / 3)])
        # Door leaves: frames a little proud of the glass, a push bar each.
        for s in (-1, 1):
            dy = mid + s * 0.55
            buf.frame_y(rounded_rect(0.53, 1.12, 0.04), rounded_rect(0.44, 1.03, 0.03), -0.05, 0.05, "east_metal",
                        zc=1.5, xform=Matrix.Translation((xf - 0.02, dy, 0)) @ Matrix.Rotation(math.radians(90), 4, "Z"))
            buf.tube([Vector((xf - 0.12, dy - s * 0.3, 1.05)), Vector((xf - 0.12, dy - s * 0.3, 1.5))], 0.03, "east_steel_light")
    elif front == "shutter_half":
        # A roll-up shutter half down over a glass counter: the box at the head, the shutter, the
        # counter below it with a glass top.
        buf.box((xf + 0.1, mid, ceil - 0.2), (0.45, w + 0.3, 0.42), "east_steel_light", r=0.1)
        sh = [Vector((xf + 0.02, y1 - 0.3, 1.95)), Vector((xf + 0.02, y0 + 0.3, 1.95)),
              Vector((xf + 0.02, y0 + 0.3, ceil - 0.3)), Vector((xf + 0.02, y1 - 0.3, ceil - 0.3))]
        buf.quad(sh, "east_shutter", [(0, 0), (w / 2, 0), (w / 2, (ceil - 2.25) / 1), (0, (ceil - 2.25))])
        buf.box((xf + 0.35, mid, 0.85), (0.6, w, 0.98), "east_tile_green", r=0.05)
        glass.box((xf + 0.35, mid, 1.37), (0.62, w + 0.02, 0.08), "east_shop_glass", r=0.02)
    elif front == "lobby":
        # The tower's lobby: a glass door behind a steel gate, a guard's desk.
        glass.quad([Vector((xf + 0.01, y1 - 0.3, 0.37)), Vector((xf + 0.01, y0 + 0.3, 0.37)),
                    Vector((xf + 0.01, y0 + 0.3, ceil - 0.1)), Vector((xf + 0.01, y1 - 0.3, ceil - 0.1))], "east_shop_glass",
                   [(0, 0), (w / 4, 0), (w / 4, ceil / 4), (0, ceil / 4)])
        buf.box((xf - 0.2, mid, ceil - 0.05), (0.14, w + 0.1, 0.14), "east_metal", r=0.03)
        grille(buf, Vector((xf - 0.3, y1 - 0.3)), Vector((xf - 0.3, y0 + 0.3)), Vector((-1, 0)), 0.38, ceil - 0.12, gap=0.0)
        buf.box((xf + 1.2, mid - 0.6, 0.85), (0.7, 1.2, 0.95), "east_timber", r=0.05)
    else:
        # Open fronts: the shutter rolled up in its box, and a counter across part of the front.
        buf.box((xf + 0.1, mid, ceil - 0.2), (0.45, w + 0.3, 0.42), "east_steel_light", r=0.1)
        if front == "open_eatery":
            # The ulam counter: tiled base, a glass display on top with the pots behind.
            buf.box((xf + 0.45, mid + 0.4, 0.62), (0.7, w - 1.6, 0.56), "east_tile_cream", r=0.05)
            glass.box((xf + 0.45, mid + 0.4, 1.12), (0.66, w - 1.7, 0.48), "east_shop_glass", r=0.04)
            for i in range(4):
                y = mid - 0.9 + i * 0.95
                buf.cylinder((xf + 0.55, y), 0.2, 0.88, 1.18, "east_steel_light", sides=12)
        elif front == "open_shelves":
            buf.box((xf + 0.4, mid - 0.6, 0.66), (0.62, w - 2.2, 0.64), "east_tile_cream", r=0.05)
            buf.box((xf + 0.4, mid - 0.6, 1.02), (0.7, w - 2.1, 0.08), "east_timber", r=0.02)
            buf.box((xf + 1.6, mid + 1.4, 0.9), (0.9, 0.7, 1.1), "east_render_grey", r=0.06)   # the copier
        elif front == "open_pc":
            buf.box((xf + 0.6, mid, 0.66), (0.5, w - 0.4, 0.6), "east_render_grey", r=0.04)


def pilaster(buf, y, depth, mat):
    # From 1 cm under the pavement top into the fascia beam (bottom 3.2) by 8 cm.
    buf.box((FRONT + depth / 2, y, 1.74), (depth, 0.56, 3.08), mat, r=0.1)


def shop_row(col, rng):
    """The ground floors of the West East Center and the Astral podium, bay by bay, with their
    awnings and signs."""
    buf = EBuf("east_shoprow", drip_top=drip_levels([3.2, 3.4, WEC_G - 0.1, ASTRAL_G - 0.1]), splash=True)
    glass = EBuf("east_shoprow_glass")
    kit = EBuf("east_shoprow_awnings")
    signs = bpy.data.collections["east shop signs"]
    # The end walls of the row, where the podiums turn the corner. They start 6 cm behind the
    # frontage, inside the end pilaster: flush at x = FRONT their faces shared the pilaster's plane
    # and the tiles flickered (owner: "z-fighting + clipping signage").
    buf.box((FRONT + 3.33, -24.28, WEC_G / 2 - 0.1), (6.54, 0.42, WEC_G + 0.1), "east_wec_render", r=0.06)
    buf.box((FRONT + 3.33, 20.38, ASTRAL_G / 2 - 0.1), (6.54, 0.42, ASTRAL_G + 0.1), "east_astral_cream", r=0.06)
    # The fascia beam over the whole row (the sign zone), in each podium's own paint.
    for (ya, yb), g, mat in (((-24.3, SPLIT_Y + 0.08), WEC_G, "east_wec_render"), ((SPLIT_Y - 0.08, 20.4), ASTRAL_G, "east_astral_cream")):
        buf.extrude_y(fillet([(FRONT, 3.2), (FRONT + 1.8, 3.2), (FRONT + 1.8, g + 0.05), (FRONT, g + 0.05)], 0.08, 2),
                      ya, yb, mat)
    tiles = {"cell": "east_tile_green", "print": "east_tile_cream", "eatery": "east_tile_maroon", "lobby": "east_tile_cream",
             "pcx": "east_tile_cream", "pisonet": "east_tile_green", "botika": "east_tile_cream"}
    bounds = sorted({y for y0, y1, *_ in SHOP_ROW for y in (y0, y1)})
    for y in bounds:
        near = [r for r in SHOP_ROW if r[0] == y or r[1] == y]
        depth = max(r[3] for r in near) + 0.25
        key = near[-1][2]
        pilaster(buf, y, depth, tiles[key])
    # The street kit's poles and lamps that still stand under the awnings: each awning is cut into
    # pieces round them, as awnings on Taft are cut round the poles (owner: "fix these canopy +
    # pole clipping issues"). Boxes come padded 0.25 m by read_boxes().
    poles = [bx for bx in read_boxes(SOURCE / "street.blend") if bx[5] - bx[4] > 5.0]
    for y0, y1, key, setback, front, aw in SHOP_ROW:
        shopfront(buf, glass, y0 + 0.28, y1 - 0.28, key, setback, front, rng)
        if aw:
            xe = FRONT - aw[1]
            gaps = sorted((by0 - 0.1, by1 + 0.1) for bx0, bx1, by0, by1, bz0, bz1 in poles
                          if bx0 < FRONT and bx1 > xe and by1 > y0 and by0 < y1)
            a0 = y0 + 0.2
            for g0, g1 in gaps + [(y1 - 0.2, y1 - 0.2)]:
                if g0 - a0 >= 0.8:
                    awning(kit, a0, g0, aw, rng)
                a0 = max(a0, g1)

    # The signs, each on its own system (tools/author_ilalim_signs.py).
    def put(key, x, y, z, m=None):
        S.build(key, EBuf, signs, m or east_matrix(x, y, z))
    put("cell", 0, 0, 0, Matrix.Translation((FRONT - 0.25, -20.55, awning_underside(SHOP_ROW[0][5], FRONT - 0.25) + 0.03))
        @ Matrix.Rotation(math.radians(90), 4, "Z"))
    put("cell_board", 0, 0, 0, Matrix.Translation((FRONT - 0.42, -17.9, PAVE_TOP - 0.01)) @ Matrix.Rotation(math.radians(90), 4, "Z"))
    put("print", FRONT, -13.4, 3.2)
    put("dorm_a", FRONT, -3.05, 3.55)
    put("dorm_b", FRONT, -2.15, 3.62)
    put("dorm_c", FRONT, -1.2, 3.5)
    put("pcx", FRONT, 4.15, 3.42)
    put("pisonet", FRONT, 11.9, 3.42)
    # The botika's blade stands out from the podium's slab edge above the awning; its banner hangs
    # on the Padre Faura face.
    put("botika", 11.85, 19.2, ASTRAL_G + 0.15)
    put("scrubs", 0, 0, 0, Matrix.Translation((13.9, 20.62, 0.95)))
    # Round the corner on Padre Faura: a lugawan under its own framed board.
    put("lugaw", 0, 0, 0, Matrix.Translation((23.0, 20.07, 3.58)) @ Matrix.Rotation(math.atan2(-1.5, 36.34), 4, "Z"))
    buf.finish(col, bevel=0.04)
    glass.finish(col, bevel=0.01)
    kit.finish(col, bevel=0.015)


# ------------------------------------------------------------------ West East Center

def podium_base(buf, poly, g, wall):
    """A podium's ground floor away from Taft: shops on its side streets too, a recessed band of
    roll-up shutters and glass under the slab, never a blank wall."""
    lv = [(0.0, -0.2), (0.0, 0.4), (-0.35, 0.42), (-0.35, g - 1.3), (0.0, g - 1.28), (0.0, g + 0.02)]
    buf.rings_z(poly, lv, lambda i, j: (("east_shutter", "east_display", "east_shutter")[j % 3] if i == 2 else wall),
                seg_band=lambda i: (0.42, 3.0) if i == 2 else None, cap_bottom=False)


def west_east_center(col, poly, rng):
    """Four storeys of salmon podium. Upper floors: a recessed core with ribbon windows and, on
    the Taft face, continuous balconies whose parapets alternate render and breeze-block panels,
    with planters. The ground floor is the shop row."""
    storey = 3.2
    edges = [WEC_G + k * storey for k in range(4)]
    roof = edges[-1]
    core = clip_x(poly, FRONT + 1.3)
    buf = EBuf("east_wec", drip_top=drip_levels([e - 0.1 for e in edges] + [roof + 1.0]), splash=False)
    kit = EBuf("east_wec_kit", drip_top=roof + 1.0)
    # The ground-floor mass behind the shops.
    podium_base(buf, clip_x(poly, FRONT + 6.0), WEC_G, "east_wec_render")
    for k in range(3):
        z0 = edges[k]
        lv = [(0.0, z0 - 0.02), (0.0, z0 + 0.9), (-0.25, z0 + 0.92), (-0.25, z0 + 2.5), (0.0, z0 + 2.52),
              (0.0, z0 + storey + 0.02)]
        buf.rings_z(core, lv, lambda i, j: "east_glass_ribbon" if i == 2 else "east_wec_wall",
                    seg_band=lambda i, z0=z0: (z0 + 0.92, 1.6) if i == 2 else None, cap_bottom=False)
    top = [buf.bm.verts.new((x, y, roof + 0.02)) for x, y in offset(core, -0.01)]
    buf.face(top, "east_roof")
    roof_kit(kit, core, roof, rng, tanks=3, parapet_mat="east_wec_render")
    # Balconies on the Taft face: slab from x 10.4 into the core, the parapet on its lip.
    y_s, y_n = min(p[1] for p in poly) + 0.3, SPLIT_Y - 0.1
    for k in range(3):
        z0 = edges[k]
        buf.extrude_y(fillet([(10.4, z0 - 0.2), (FRONT + 1.5, z0 - 0.2), (FRONT + 1.5, z0 + 0.12), (10.4, z0 + 0.12)],
                             0.07, 2), y_s, y_n, "east_slab")
        # Parapet panels, 2.6 to 3.4 m each, render or breeze block, each with its own lean.
        y = y_s
        i = 0
        while y < y_n - 0.5:
            wlen = min(rng.uniform(2.6, 3.4), y_n - y)
            mat = "east_wec_breeze" if i % 3 == 1 else "east_wec_render"
            buf.extrude_y(fillet([(10.42, z0 + 0.08), (10.66, z0 + 0.08), (10.66, z0 + 1.08), (10.42, z0 + 1.1)], 0.05, 2),
                          y + 0.02, y + wlen - 0.02, mat)
            if mat == "east_wec_render" and rng.random() < 0.55:
                planter(kit, (10.95, y + wlen / 2, z0 + 0.1), wlen - 0.8, rng)
            y += wlen
            i += 1
        # Washing on a line across part of the balcony, on some floors.
        if rng.random() < 0.7:
            ya = rng.uniform(y_s + 1.0, y_n - 7.0)
            laundry(kit, (11.2, ya), (11.2, ya + rng.uniform(3.0, 5.5)), z0 + 2.05, rng)
        # End walls of the balcony.
        for yy in (y_s + 0.1, y_n - 0.1):
            buf.box((FRONT + 0.2, yy, z0 + 0.6), (1.6, 0.22, 1.05), "east_wec_render", r=0.05)
        # Grilles over half the windows, aircon boxes in the ribbon.
        for yy in (-22.0, -15.5, -8.5):
            if rng.random() < 0.6:
                grille(kit, Vector((FRONT + 0.98, yy + 1.2)), Vector((FRONT + 0.98, yy - 1.2)), Vector((-1, 0)),
                       z0 + 0.95, z0 + 2.45, gap=0.0)
        for yy in (-19.0, -12.0, -6.5):
            if rng.random() < 0.55:
                ac_box(kit, Vector((FRONT + 1.05, yy)), Vector((-1, 0)), z0 + 1.0)
    buf.finish(col, bevel=0.05)
    kit.finish(col, bevel=0.02)
    # Upper-floor signs: the dental clinic's lightbox on the second-floor parapet over the print
    # shop, the review centre's tarpaulin tied to the fourth-floor parapet over the cell stall.
    signs = bpy.data.collections["east shop signs"]
    S.build("dental", EBuf, signs, east_matrix(10.4, -13.4, edges[0] + 0.05))
    S.build("review", EBuf, signs, east_matrix(10.4, -20.2, edges[2] + 0.02))
    S.build("eatery", EBuf, signs, east_matrix(10.4, -7.0, edges[0] + 0.02))
    return roof


# ------------------------------------------------------------------ the Astral Tower

def astral_tower(col, poly, rng):
    """A cream podium (ground plus two floors with ribbon windows and slab bands) under a rounded
    slab tower of 16 floors: every floor a cream balcony band over the salmon wall, and two salmon
    service shafts standing proud of the bands on the long faces. Unnamed."""
    storey_p, storey_t = 3.2, 3.0
    podium = [ASTRAL_G + k * storey_p for k in range(3)]
    base_t = podium[-1]
    floors_t = 19 - 3
    edges_t = [base_t + k * storey_t for k in range(floors_t + 1)]
    roof = edges_t[-1]
    buf = EBuf("east_astral_podium", drip_top=drip_levels([e - 0.1 for e in podium] + [base_t + 1.0]))
    kit = EBuf("east_astral_kit", drip_top=roof + 1.0)
    core = clip_x(poly, FRONT + 0.9)
    podium_base(buf, clip_x(poly, FRONT + 6.0), ASTRAL_G, "east_astral_cream")
    for k in range(2):
        z0 = podium[k]
        lv = [(0.0, z0 - 0.02), (0.0, z0 + 0.85), (-0.3, z0 + 0.87), (-0.3, z0 + 2.45), (0.0, z0 + 2.47),
              (0.0, z0 + storey_p + 0.02)]
        buf.rings_z(core, lv, lambda i, j: "east_glass_ribbon" if i == 2 else ("east_astral_salmon" if i == 4 else "east_astral_cream"),
                    seg_band=lambda i, z0=z0: (z0 + 0.87, 1.6) if i == 2 else None, cap_bottom=False)
        buf.annulus(offset(core, 0.42), offset(core, -0.1), z0 - 0.22, z0 + 0.1, "east_slab")
        for yy in (1.5, 6.5, 12.0, 17.5):
            if rng.random() < 0.6:
                ac_box(kit, Vector((FRONT + 0.9 - 0.3, yy)), Vector((-1, 0)), z0 + 1.0)
    top = [buf.bm.verts.new((x, y, base_t + 0.02)) for x, y in offset(core, -0.01)]
    buf.face(top, "east_roof")
    buf.annulus(offset(core, 0.06), offset(core, -0.22), base_t - 0.06, base_t + 1.05, "east_astral_cream", mat_top="east_slab")
    # Salmon fins up the podium's Taft face, one per shop bay, so the ribbons read in bays. 0.7 m
    # deep, so their face (x 11.37) stands 11 cm proud of the slab edges (x 11.48); at 0.5 m it sat
    # 1 cm off them and the bevels z-fought.
    # The first sits at -3.84, not -3.9, so its south face clears the West East Center's balcony
    # end walls (south face y -4.11) by 5 cm instead of sharing their plane.
    for yy in (-3.84, -0.3, 4.15, 8.6, 15.2, 20.0):
        buf.box((FRONT + 0.72, yy, (ASTRAL_G + base_t) / 2 + 0.45), (0.7, 0.42, base_t - ASTRAL_G + 0.95),
                "east_astral_salmon", r=0.1)
    buf.finish(col, bevel=0.05)

    # The tower: a rounded slab set back on the podium roof.
    cx, cy = 31.5, 8.2
    tw, td = 25.0, 15.0
    tower = ccw(fillet([(cx - tw / 2, cy - td / 2), (cx + tw / 2, cy - td / 2), (cx + tw / 2, cy + td / 2),
                        (cx - tw / 2, cy + td / 2)], 3.2, 5))
    tb = EBuf("east_astral_tower", drip_top=drip_levels([e - 0.05 for e in edges_t] + [e + 1.1 for e in edges_t] + [roof + 1.2]))
    for k in range(floors_t):
        z0 = edges_t[k]
        tb.rings_z(tower, [(0.0, z0 - 0.02), (0.0, z0 + storey_t + 0.02)], lambda i, j: "east_astral_wall",
                   seg_band=lambda i, z0=z0: (z0, 3.0), cap_bottom=(k == 0))
        # The balcony band: slab and a solid cream parapet, 0.85 m out from the wall.
        tb.annulus(offset(tower, 0.85), offset(tower, -0.08), z0 - 0.05, z0 + 1.1 + rng.uniform(-0.01, 0.01),
                   "east_astral_band", mat_top="east_slab", band=(z0 - 0.05, 1.2))
    top = [tb.bm.verts.new((x, y, roof + 0.02)) for x, y in offset(tower, -0.01)]
    tb.face(top, "east_roof")
    tb.annulus(offset(tower, 0.9), offset(tower, 0.62), roof - 0.05, roof + 1.2, "east_astral_band", mat_top="east_slab")
    # Two service shafts on each long face, salmon, proud of the bands, running past the roof.
    for sy in (-1, 1):
        for sx in (-6.5, 5.0):
            tb.box((cx + sx, cy + sy * (td / 2 + 0.55), (base_t + roof) / 2 + 1.2), (2.6, 2.3, roof - base_t + 2.4),
                   "east_astral_salmon", r=0.35)
    # The lift house and the crown.
    tb.box((cx + 2, cy, roof + 2.2), (7.0, 5.0, 4.4), "east_astral_salmon", r=0.5)
    tb.box((cx + 2, cy, roof + 4.5), (7.6, 5.6, 0.35), "east_astral_band", r=0.15)
    tb.finish(col, bevel=0.06)
    for x, y in ((cx - 6.5, cy + 2.5), (cx - 8.5, cy - 2.5), (cx + 9, cy + 1)):
        tank(kit, (x, y), roof, rng)
    kit.tube([Vector((cx + 4.5, cy + 1.5, roof + 4.4)), Vector((cx + 4.5, cy + 1.5, roof + 10.5))], 0.09, "east_metal")
    # Aircon boxes stand on the balconies, against the wall, on some floors.
    for k in range(floors_t):
        for (px, py, nx, ny) in ((cx - 9, cy - td / 2, 0, -1), (cx + 1, cy - td / 2, 0, -1), (cx - 3, cy + td / 2, 0, 1),
                                 (cx - tw / 2, cy, -1, 0), (cx + 9, cy + td / 2, 0, 1)):
            if rng.random() < 0.35:
                ac_box(kit, Vector((px, py)), Vector((nx, ny)), edges_t[k] + 0.08)
    kit.finish(col, bevel=0.02)
    return roof


# ------------------------------------------------------------------ Manok ni Mang Carding

def manok(col, poly, rng):
    """The old KFC building as an invented Filipino fried-chicken place. Set back 5 m behind a
    forecourt (the real one has its drive-through loop), so its pylon stands on its own lot."""
    body = clip_x(poly, FRONT + 5.0)
    g, roof = 4.2, 7.6
    buf = EBuf("east_manok", drip_top=drip_levels([g - 0.1, roof + 1.0]), splash=True)
    # Ground floor: full glazing recessed behind a maroon frame.
    buf.rings_z(body, [(0.0, -0.2), (0.0, 0.5), (-0.3, 0.52), (-0.3, g - 0.62), (0.0, g - 0.6), (0.0, g + 0.02)],
                lambda i, j: "east_shop_glass" if i == 2 else "east_maroon", cap_bottom=False)
    # The fascia band wraps the building, the upper floor behind it in cream with a ribbon.
    buf.annulus(offset(body, 0.35), offset(body, -0.1), g - 0.7, g + 0.55, "east_maroon", mat_top="east_mustard")
    lv = [(0.0, g), (0.0, g + 1.0), (-0.25, g + 1.02), (-0.25, g + 2.5), (0.0, g + 2.52), (0.0, roof + 0.02)]
    buf.rings_z(body, lv, lambda i, j: "east_glass_ribbon" if i == 2 else "east_render_cream",
                seg_band=lambda i: (g + 1.02, 1.6) if i == 2 else None, cap_bottom=False)
    top = [buf.bm.verts.new((x, y, roof + 0.02)) for x, y in offset(body, -0.01)]
    buf.face(top, "east_roof")
    # A mustard crown, sloping out a little, the fast-food silhouette.
    outer, inner = offset(body, 0.45), offset(body, -0.15)
    buf.annulus(outer, inner, roof - 0.05, roof + 1.3, "east_mustard", mat_top="east_slab")
    # The drive-through canopy on the south face.
    ys = min(p[1] for p in body)
    xs = [p[0] for p in body]
    buf.extrude_x(fillet([(ys - 3.6, 3.3), (ys + 0.3, 3.3), (ys + 0.3, 3.62), (ys - 3.6, 3.62)], 0.08, 2),
                  min(xs) + 2, max(xs) - 3, "east_maroon")
    for x in (min(xs) + 2.5, max(xs) - 3.5):
        buf.box((x, ys - 3.2, 1.75), (0.35, 0.35, 3.3), "east_mustard", r=0.1)
    kit = EBuf("east_manok_kit", drip_top=roof + 1.3)
    roof_kit(kit, body, roof, rng, tanks=1, house=False, parapet_mat="east_mustard")
    for yy in (-33.0, -28.5):
        ac_box(kit, Vector((FRONT + 5.0 - 0.25, yy)), Vector((-1, 0)), g + 1.1)
    buf.finish(col, bevel=0.05)
    kit.finish(col, bevel=0.02)
    signs = bpy.data.collections["east shop signs"]
    ymid = (min(p[1] for p in body) + max(p[1] for p in body)) / 2
    S.build("manok_fascia", EBuf, signs, east_matrix(FRONT + 5.0 - 0.35, ymid, g - 0.62))
    S.build("manok_pylon", EBuf, signs, Matrix.Translation((12.6, -35.8, 0.24)))


# ------------------------------------------------------------------ churches, the school, the office

def church(col, name, poly, rng, tower_at="nw"):
    """A pitched hall of warm white render with a tall square tower carrying a cross."""
    xs, ys = [p[0] for p in poly], [p[1] for p in poly]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    buf = EBuf(name, drip_top=drip_levels([8.0, 9.0]), splash=True)
    eave = 8.0
    buf.rings_z(poly, [(0.0, -0.2), (0.0, eave)], lambda i, j: "east_render_white", cap_bottom=False)
    top = [buf.bm.verts.new((x, y, eave - 0.02)) for x, y in offset(poly, -0.01)]
    buf.face(top, "east_roof")
    # The pitched roof along the hall's long axis, eaves overhanging.
    along_x = (x1 - x0) >= (y1 - y0)
    span = (y1 - y0) if along_x else (x1 - x0)
    ridge = eave + min(span * 0.32, 6.0)
    if along_x:
        buf.extrude_x(fillet([(y0 - 0.7, eave - 0.3), (y1 + 0.7, eave - 0.3), (y1 + 0.7, eave - 0.05), ((y0 + y1) / 2, ridge),
                              (y0 - 0.7, eave - 0.05)], 0.15, 2), x0 - 0.6, x1 + 0.6, "east_tin_roof")
    else:
        buf.extrude_y(fillet([(x0 - 0.7, eave - 0.3), (x1 + 0.7, eave - 0.3), (x1 + 0.7, eave - 0.05), ((x0 + x1) / 2, ridge),
                              (x0 - 0.7, eave - 0.05)], 0.15, 2), y0 - 0.6, y1 + 0.6, "east_tin_roof")
    tx = x0 + 2.5 if "w" in tower_at else x1 - 2.5
    ty = y1 - 2.5 if "n" in tower_at else y0 + 2.5
    th = ridge + 8.0
    buf.box((tx, ty, th / 2 - 0.2), (4.2, 4.2, th + 0.4), "east_render_cream", r=0.3)
    buf.box((tx, ty, th + 0.2), (4.8, 4.8, 0.45), "east_slab", r=0.15)
    for s in (-1, 1):
        buf.box((tx - 2.08, ty + s * 0.9, th - 3.0), (0.1, 0.5, 2.6), "east_int_dark", r=0.05)
    buf.box((tx, ty, th + 2.2), (0.32, 0.32, 3.6), "east_cross", r=0.08)
    buf.box((tx, ty, th + 2.9), (0.3, 1.8, 0.32), "east_cross", r=0.08)
    buf.finish(col, bevel=0.05)


def school(col, poly, rng):
    """Manila Science High School's new block: ten storeys in a grey concrete frame, brick panels
    on the ends and a glass curtain wall on the long faces, with a teal-grey accent strip where the
    real one is blue. Unnamed."""
    storey, floors = 3.4, 10
    edges = [0.1 + k * storey for k in range(floors + 1)]
    roof = edges[-1]
    buf = EBuf("east_school", drip_top=drip_levels([e - 0.1 for e in edges] + [roof + 1.0]), splash=True)
    en = edge_normals(poly)
    for k in range(floors):
        z0 = edges[k]
        buf.rings_z(poly, [(0.0, z0 - (0.3 if k == 0 else 0.02)), (0.0, z0 + storey + 0.02)],
                    lambda i, j: "east_curtain" if en[j][2] > 20 else ("east_brick" if k > 0 else "east_render_grey"),
                    seg_band=lambda i, z0=z0: (z0, 3.4), cap_bottom=False)
        buf.annulus(offset(poly, 0.3), offset(poly, -0.1), z0 + storey - 0.25, z0 + storey + 0.1, "east_render_grey")
    top = [buf.bm.verts.new((x, y, roof + 0.02)) for x, y in offset(poly, -0.01)]
    buf.face(top, "east_roof")
    buf.annulus(offset(poly, 0.35), offset(poly, -0.2), roof - 0.1, roof + 1.2, "east_render_grey", mat_top="east_slab")
    # Vertical frame fins at the corners of each long face, and the accent strip.
    for (nrm, mid, length), a in zip(en, poly):
        if length < 8:
            continue
        a = Vector(a)
        t = Vector((-nrm.y, nrm.x))
        for s in (-0.5, 0.5):
            p = mid + t * s * (length - 1.0)
            buf.box((p.x + nrm.x * 0.3, p.y + nrm.y * 0.3, roof / 2 + 0.5), (0.6, 0.6, roof + 1.2), "east_render_grey",
                    r=0.12, rot=math.atan2(t.y, t.x))
    kit = EBuf("east_school_kit", drip_top=roof + 1.2)
    roof_kit(kit, poly, roof, rng, tanks=3, parapet_mat="east_render_grey")
    # The boundary wall along Padre Faura, with a gate.
    ys = min(p[1] for p in poly)
    xs = [p[0] for p in poly]
    kit.extrude_x(fillet([(ys - 2.2, -0.1), (ys - 1.9, -0.1), (ys - 1.9, 2.2), (ys - 2.2, 2.2)], 0.05, 2),
                  min(xs), max(xs), "east_render_teal")
    buf.finish(col, bevel=0.05)
    kit.finish(col, bevel=0.02)


# ------------------------------------------------------------------ Vista GL Taft

def vista(col, poly, rng, look=None, site=None, own=None):
    roof = midrise(col, "east_vista_gl", poly, 6, rng, style="fins", wall="east_render_white", storey=3.0,
                   ground=4.0, shops=True, tanks=3, look=look, site=site, own=own)
    signs = bpy.data.collections["east shop signs"]
    S.build("laundry", EBuf, signs, east_matrix(FRONT - 0.25, -46.5, 3.78))
    S.build("siomai", EBuf, signs, east_matrix(FRONT, -52.0, 3.6) @ Matrix.Identity(4))
    S.build("vulcan", EBuf, signs, Matrix.Translation((FRONT - 0.25, -69.5, PAVE_TOP - 0.01)) @ Matrix.Rotation(math.radians(90), 4, "Z"))
    return roof


# ------------------------------------------------------------------ assembly

NAMED = {
    "Astral Tower": "astral", "West East Center": "wec", "KFC": "manok", "Vista GL Taft": "vista",
    "Manila Science High School": "school", "Manila Baptist Church": "church", "Cosmopolitan Church": "church",
    "GCK Building": "office",
}
# Near buildings without names get a full construction; the style is chosen by index for variety.
NEAR_STYLES = ["ribbon", "hood", "balcony", "fins"]


def assign_looks(polys):
    """A Look for every unnamed building and Vista GL Taft, deterministic per OSM index. The paint
    is chosen greedily so that no two buildings within 45 m share one (the least used nearby when
    all nine are taken); blade signs go round the front row in index order, one each."""
    looks = {}
    keys = list(PALETTE)
    signs = list(FACADE_SIGNS)
    # The salmon-pink landmarks count as salmon and rose neighbours, so their pink is not repeated
    # next door (v30 painted Vista rose beside the West East Center).
    pinks = [(cx, cy) for b, poly, cx, cy in polys.values() if NAMED.get(b["name"]) in ("wec", "astral")]
    for k in sorted(polys):
        b, poly, cx, cy = polys[k]
        kind = NAMED.get(b["name"])
        if kind not in (None, "vista"):
            continue
        lr = random.Random(k * 131 + 17)
        cand = keys[:]
        lr.shuffle(cand)
        near = [looks[j].key for j in looks if math.hypot(polys[j][2] - cx, polys[j][3] - cy) < 45]
        near += ["salmon", "rose"] * sum(math.hypot(px - cx, py - cy) < 70 for px, py in pinks)
        d = math.hypot(cx - 20, cy - 15)
        detail = 2 if (d < 75 or kind == "vista") else (1 if math.hypot(cx - 20, cy) < 95 else 0)
        if detail == 2:
            cand.remove("chalk")        # near-white reads as the old grey-white look from the street
        key = min(cand, key=lambda c: near.count(c))
        looks[k] = Look(key, lr, detail)
        if detail == 2 and signs:
            looks[k].sign = signs.pop(0)
    return looks


def buildings(root, layout):
    col = collection("east buildings", root)
    rng = random.Random(1947)
    bs = layout["buildings"]
    counts = {"named": 0, "near": 0, "lod": 0}
    polys = {}
    for k, b in enumerate(bs):
        p = b["poly"]
        cx = sum(x for x, _ in p) / len(p)
        cy = sum(y for _, y in p) / len(p)
        if cx <= 11 or cx > 260 or abs(cy) > 265:
            continue
        polys[k] = (b, prepare(p), cx, cy)
    shops = collection("east shop signs", col)
    looks = assign_looks(polys)
    site = Site({k: v[1] for k, v in polys.items()})
    for k, (b, poly, cx, cy) in polys.items():
        name = b["name"]
        kind = NAMED.get(name)
        levels = int(round(b["levels"])) if b["levels"] else random.Random(k).choice((2, 3, 3, 4, 4, 5))
        r = random.Random(k * 31 + 7)
        slug = "east_" + ("".join(c.lower() if c.isalnum() else "_" for c in name)[:20] if name else f"bldg_{k}")
        if kind == "wec":
            west_east_center(col, poly, r)
        elif kind == "astral":
            astral_tower(col, poly, r)
        elif kind == "manok":
            manok(col, poly, r)
        elif kind == "vista":
            vista(col, poly, r, looks.get(k), site, k)
        elif kind == "school":
            school(col, poly, r)
        elif kind == "church":
            church(col, slug, poly, r, tower_at="nw" if cx > 40 else "sw")
        elif kind == "office":
            midrise(col, slug, poly, levels, r, style="ribbon", wall="east_render_grey", storey=3.6, ground=4.5)
        elif math.hypot(cx - 20, cy) < 95:
            style = NEAR_STYLES[k % 4]
            midrise(col, slug, poly, max(2, levels), r, style=style, storey=3.2, ground=4.0,
                    ac=math.hypot(cx, cy) < 80, look=looks.get(k), site=site, own=k)
            counts["near"] += 1
            continue
        else:
            lod_block(col, slug, poly, max(2, levels), r, look=looks.get(k))
            counts["lod"] += 1
            continue
        counts["named"] += 1
    shop_row(col, random.Random(11))
    print("[east] buildings", counts)
    used = {}
    for lk in looks.values():
        used[lk.key] = used.get(lk.key, 0) + 1
    print("[east] paints", used)
    for k, lk in sorted(looks.items()):
        if lk.detail:
            print(f"[east] look {k}: {lk.key} detail {lk.detail} grille {lk.grille} shutter {lk.shutter} "
                  f"awning {lk.awning} parapet {lk.rail}")
    return col


# ------------------------------------------------------------------ context for the renders

def stand_ins(root):
    col = collection("review stand-ins (not part of the kit)", root)

    def slab(name, x0, x1, y0, y1, top, colour):
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        if m.node_tree is None:
            m.use_nodes = True
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*colour, 1)
        m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
        me = bpy.data.meshes.new(name)
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(((x0 + x1) / 2, (y0 + y1) / 2, top - 0.2))
                              @ Matrix.Diagonal((x1 - x0, y1 - y0, 0.4, 1)))
        bm.to_mesh(me)
        bm.free()
        me.materials.append(m)
        col.objects.link(bpy.data.objects.new(name, me))

    slab("stand-in road", -6.65, 6.65, -260, 260, 0.0, (0.26, 0.26, 0.27))
    for s in (-1, 1):
        slab("stand-in kerb", *sorted((s * 6.65, s * 7.0)), -260, 260, 0.15, (0.9, 0.9, 0.88))
        slab("stand-in pavement", *sorted((s * 7.0, s * 11.0)), -260, 260, PAVE_TOP, (0.60, 0.57, 0.52))
    slab("stand-in west lot", -260, -11.0, -260, 260, 0.24, (0.50, 0.56, 0.42))
    slab("stand-in east lot", 11.0, 270, -260, 22.0, 0.235, (0.55, 0.53, 0.50))
    slab("stand-in east lot N", 11.0, 270, 37.5, 260, 0.235, (0.55, 0.53, 0.50))
    slab("stand-in Padre Faura", 11.0, 270, 24.5, 35.0, 0.02, (0.28, 0.28, 0.29))
    for y in (22.0, 35.0):
        slab("stand-in Faura walk", 11.0, 270, y if y < 30 else 35.0, 24.5 if y < 30 else 37.5, 0.18, (0.6, 0.57, 0.52))
    for s in (-1, 1):
        slab("stand-in chalk", -7, 7, s * 7 - 0.06, s * 7 + 0.06, 0.012, (1, 1, 1))
    slab("stand-in overclock pad", 8.1, 9.9, 4.6, 6.4, PAVE_TOP + 0.03, (0.35, 0.8, 0.55))
    fig = bpy.data.meshes.new("figure")
    bm = bmesh.new()
    for at in ((2.0, -6.0, 0.85), (9.0, 2.0, 0.85 + PAVE_TOP)):
        bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=0.25, radius2=0.25, depth=1.7,
                              matrix=Matrix.Translation(at))
    bm.to_mesh(fig)
    bm.free()
    fm = bpy.data.materials.new("figure")
    fm.use_nodes = True
    fm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.85, 0.35, 0.55, 1)
    fig.materials.append(fm)
    col.objects.link(bpy.data.objects.new("1.7 m figures", fig))
    return col


def link_guideway(root):
    path = SOURCE / "lrt_kit.blend"
    if not path.exists():
        return None
    with bpy.data.libraries.load(str(path), link=True) as (src, dst):
        dst.collections = [c for c in src.collections if c == "guideway over the court"]
    col = dst.collections[0] if dst.collections else None
    if col:
        inst = bpy.data.objects.new("LRT-1 guideway (linked context)", None)
        inst.instance_type, inst.instance_collection = "COLLECTION", col
        root.objects.link(inst)
    return col


def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.74, 0.80, 0.90, 1)
    bg.inputs["Strength"].default_value = 0.65
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 3.8, math.radians(3), (1.0, 0.88, 0.72)
    sun.rotation_euler = Vector((0.80, -0.25, -0.55)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"


SHOTS = {
    # Game's eye: 1.25 m over the pavement (0.212) or the road, 95 degrees.
    "court_east":     ((-9.5, -4.0, 1.46), (12, 6, 3), "eye"),
    "spawn_east":     ((0.0, -9.0, 1.25), (14, 4, 4.5), "eye"),
    "pavement_north": ((9.3, -15.8, 1.46), (9.6, 10, 2.8), "eye"),
    "pavement_south": ((9.3, 16.2, 1.46), (9.8, -12, 2.6), "eye"),
    "court_up":       ((-6.0, 2.0, 1.25), (20, 8, 22), "eye"),
    "pcx_close":      ((5.8, 4.2, 1.8), (11.5, 4.2, 3.0), 22),
    "eatery_print":   ((6.2, -9.5, 1.9), (11.6, -11.5, 2.9), 20),
    "cell_close":     ((7.2, -17.0, 1.7), (11.4, -21.5, 2.6), 22),
    "pisonet_botika": ((5.5, 11.5, 2.0), (11.5, 17.0, 3.5), 20),
    "faura_corner":   ((3.0, 30.0, 1.5), (16, 18, 6), 20),
    "astral_faura":   ((-14.0, 42.0, 1.5), (30, 8, 28), 18),
    "south_manok":    ((8.2, -17.5, 1.46), (14, -34, 3.5), 22),
    "north_school":   ((6.0, 38.0, 1.5), (50, 100, 14), 22),
    "faura_lugaw":    ((21.0, 31.0, 1.6), (22.5, 20.0, 3.4), 24),
    "aerial":         ((-70.0, -95.0, 85.0), (40, 0, 8), 30),
    "aerial_close":   ((-25.0, -45.0, 38.0), (18, 0, 6), 28),
}


def preview(version, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 3000
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    for name, (pos, tgt, lens) in SHOTS.items():
        if only and name not in only:
            continue
        out = PREVIEWS / f"east_{name}_v{version}.png"
        if out.exists():
            print("[east] exists, skipped", out)
            continue
        pos, tgt = Vector(pos), Vector(tgt)
        cam.location, cam.data.lens = pos, (eye if lens == "eye" else lens)
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(out)
        bpy.ops.render.render(write_still=True)
        print("[east] preview", out)


def drop_replaced():
    """The landmarks kit (tools/author_ilalim_landmarks.py) replaces a few generic buildings with
    landmark silhouettes and lists them in ArtSource/ilalim/landmarks.json ({"hide": [...]}).
    Those buildings, and every object named after them ("<name>_..."), are removed AFTER the build,
    so every other building keeps exactly its random look."""
    import json
    path = SOURCE / "landmarks.json"
    if not path.exists():
        return
    names = json.loads(path.read_text(encoding="utf-8")).get("hide", [])
    gone = [o for o in bpy.data.objects if any(o.name == n or o.name.startswith(n + "_") for n in names)]
    for o in gone:
        bpy.data.objects.remove(o, do_unlink=True)
    print(f"[east] replaced by landmarks, removed {len(gone)} objects: {sorted(o for o in names)}")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = set(argv[argv.index("--shots") + 1].split(",")) if "--shots" in argv else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    root = collection("eastside")
    layout = load_layout()
    buildings(root, layout)
    drop_replaced()
    ctx = stand_ins(bpy.context.scene.collection)
    if "--no-lrt" not in argv:
        link_guideway(ctx)
    lighting()
    out = SOURCE / "eastside.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = SOURCE / "eastside.blend1"
    if backup.exists():
        backup.unlink()
    tris = sum(len(o.data.polygons) for o in bpy.data.objects if o.type == "MESH")
    print("[east] saved", out, "objects", len(bpy.data.objects), "faces", tris)
    if version:
        preview(version, only)


if __name__ == "__main__":
    main()
