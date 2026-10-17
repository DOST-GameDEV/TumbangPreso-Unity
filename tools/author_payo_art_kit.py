"""PAYO, the art's tool kit (2026-10-12): the materials table, the land's height function, and a few round primitives.

Imported by tools/author_payo_art.py (inside Blender). The mesh buffer, its chunky primitives, the UVs in metres and the
bevel are Paete's run's own (tools/author_paete_run_art_kit.py): this file points that kit at Payo's materials and
textures and adds what this map needs.

AXES IN HERE ARE THE DRAFT'S AND THE LAYOUT'S: X = Unity x, Y = Unity z (north), Z = Unity y (up). A glb written from
Blender lands in Unity as (-x, z, -y), so tools/author_payo_art.py turns every mesh half a turn about Z as it exports;
after that a placement's Unity position is simply (X, Z, Y) and its Unity yaw is minus the yaw used here.
"""
import math
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Vector

import author_paete_run_art_kit as K

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/TumbangPreso/Art/Payo"
TEXTURES = ART / "Textures"

# name: (texture, tint, tiling, flags). Tiling is 1 / the tile in metres. "" is a flat tint.
# flags: f foliage (two-sided leaf card, alpha), c cutout, e emissive, g glossy
# SECOND ART PASS (2026-10-12): Payo's own drawings (tools/author_payo_art_paint.py) in the owner's teal reference's
# palette, and EVERY material below except the leaf cards is multiplied by VERTEX COLOUR, which this pass bakes into
# every mesh (worn pale tops, damp dark bases, stone-to-stone shifts, worn paint, cloth and skin colours).
MAT = {
    # ---- stone
    "py_block_top": ("paving_payo", (0.80, 0.83, 0.85), 0.25, ""),             # the walked tops: the court's paving, 4 m a tile
    "py_block_edge": ("stone_payo", (0.92, 0.94, 0.96), 0.25, ""),             # caps, kerbs, painted stone (its paint is vertex colour)
    "py_block_side": ("stone_payo", (0.74, 0.78, 0.82), 0.25, ""),    # shafts and walls
    "py_carve": ("carve_payo", (1, 1, 1), 1.0, ""),                    # the relief panels; UVs laid by hand, one tile = four panels
    "py_moss": ("moss", (1, 1, 1), 0.5, ""),
    "py_vine": ("", (0.20, 0.40, 0.22), 1.0, ""),
    "py_wall": ("riverstone", (0.70, 0.86, 0.74), 0.17, ""),                    # the paddies' dry river-stone walls, 3 m a tile
    "py_dike": ("dike_payo", (0.92, 1.0, 0.90), 0.25, ""),                     # bunds: trodden grass and mud
    "py_bulul": ("stone_payo", (0.50, 0.52, 0.56), 0.2, ""),
    "py_rock": ("rock_a_grey", (0.56, 0.72, 0.74), 0.25, ""),          # Unity: TumbangPreso/LagoonRock if wanted (the kit stones carry its UV2)
    # ---- the land
    "py_ground": ("grass_a", (0.44, 0.66, 0.50), 0.2, ""),
    "py_grass": ("grass_a", (0.86, 1.0, 0.84), 0.25, ""),
    "py_rice": ("rice_payo", (1, 1, 1), 0.33, ""),
    "py_water": ("water", (0.80, 1.0, 0.98), 0.12, "g"),               # Unity: a water shader may wear this name
    "py_fall": ("fall_payo", (0.74, 0.90, 0.92), 0.5, "g"),                     # UV v is metres down the fall: scroll it
    "py_cloud": ("", (0.92, 0.98, 0.98), 1.0, "e"),
    "py_canopy": ("grass_a", (0.62, 0.86, 0.60), 0.12, ""),
    # ---- what people built
    "py_thatch": ("thatch_payo", (1, 1, 1), 0.4, ""),
    "py_timber": ("timber_payo", (1, 1, 1), 0.5, ""),
    "py_weave": ("weave", (1, 1, 1), 0.5, ""),
    "py_clay": ("clay", (1, 1, 1), 1.0, ""),
    "py_sawali": ("sawali", (1, 1, 1), 1.0, ""),
    "py_bamboo": ("bamboo", (1, 1, 1), 1.0, ""),
    "py_flat": ("", (1, 1, 1), 1.0, ""),                               # skin, hide, feather, bronze, paint marks: all colour is vertex colour
    "py_inlay": ("", (0.28, 0.86, 0.78), 1.0, "e"),                    # turquoise inlay in the carvings; glows a little at night
    "py_decal": ("decals_payo", (1, 1, 1), 1.0, "c"),                  # lichen, moss, stains, cracks: alpha cut, laid 4 to 8 mm off the stone
    # ---- the sky, the night
    "py_mayari": ("mayari", (1, 1, 1), 1.0, "ce"),
    "py_fire": ("", (1.0, 0.56, 0.14), 1.0, "e"),
    "py_fire_core": ("", (1.0, 0.92, 0.56), 1.0, "e"),
    "py_window": ("capiz", (1.0, 0.80, 0.48), 1.0, "e"),
    "py_spill": ("spill_payo", (1, 1, 1), 1.0, "ce"),
    # ---- plants: Lagoon Cove's and Kanto's own models and drawings, and a pine made the same way
    "py_palm_trunk": ("palm_trunk", (1, 1, 1), 1.0, ""),
    "py_frond_light": ("frond_broad", (0.80, 1.0, 0.92), 1.0, "f"),
    "py_frond_dark": ("frond_broad", (0.52, 0.78, 0.70), 1.0, "f"),
    "py_frond_small": ("frond", (0.66, 0.92, 0.56), 1.0, "f"),
    "py_leaf_light": ("leaf", (0.50, 0.86, 0.50), 1.0, "f"),
    "py_leaf_dark": ("leaf", (0.24, 0.58, 0.44), 1.0, "f"),
    "py_banana_stem": ("banana_stem", (1, 1, 1), 1.0, ""),
    "py_paddle_light": ("paddle", (0.70, 0.98, 0.62), 1.0, "f"),
    "py_paddle_dark": ("paddle", (0.36, 0.74, 0.52), 1.0, "f"),
    "py_flower_red": ("gumamela", (1.0, 0.42, 0.56), 1.0, "f"),
    "py_grass_light": ("blade", (0.66, 0.92, 0.56), 1.0, "f"),
    "py_grass_dark": ("blade", (0.40, 0.72, 0.50), 1.0, "f"),
    "py_tree_leaf_light": ("leaf", (0.56, 0.94, 0.50), 1.0, "f"),
    "py_tree_leaf_dark": ("leaf", (0.28, 0.66, 0.42), 1.0, "f"),
    "py_tree_trunk": ("bark", (0.86, 0.90, 0.96), 1.0, ""),
    "py_tree_core": ("", (0.12, 0.38, 0.20), 1.0, ""),
    "py_pine": ("pine_payo", (1, 1, 1), 1.0, "f"),
}
NOTE = {
    "py_water": "flat paddy water; a water shader may be given to this name. Nothing else shares its plane.",
    "py_fall": "falling water, painted streaks: scroll UV v (metres down the fall). Unlit-ish, a little see-through.",
    "py_mayari": "the sky piece: unlit, alpha blended, no fog, drawn behind the far ranges. Atlas of four 1024 tiles.",
    "py_fire": "NIGHT: emissive flame body; hidden by day (fire_flame is a night placement)",
    "py_fire_core": "NIGHT: emissive pale flame core",
    "py_window": "NIGHT: emissive doors, windows and lantern panels; by day draw it dark (tint about 0.15)",
    "py_spill": "NIGHT ONLY: warm light painted on the wall beside a window (alpha, additive or emissive); hide by day",
    "py_inlay": "turquoise inlay: a faint emission by day (0.2), stronger at night (1.0)",
    "py_cloud": "the sea of cloud, mist puffs, foam: unlit pale, takes the fog colour; at night tint to moonlit blue",
    "py_rock": "the kit stones carry Lagoon Cove's baked edge wear on UV2 (TumbangPreso/LagoonRock can use it); tinted teal-grey here",
    "py_carve": "UVs laid by hand: u is panels (one tile = four relief panels), v is 0 at the band's foot and 1 at its top",
    "py_decal": "alpha-cut decals (4 x 4 atlas) laid 4 to 8 mm off stone: needs depth offset or just cutout; no shadows",
    "py_flat": "white: ALL its colour is vertex colour (skin, hide, feathers, bronze, painted marks)",
}
# NAMES THE CODE STILL WRITES, for what they mean, and the material and vertex tint they are drawn with. 49 materials.
ALIAS = {
    "py_block_top_b": ("py_block_top", (0.84, 0.90, 0.92)), "py_mover": ("py_block_top", (0.62, 1.0, 0.96)), "py_pave": ("py_block_top", (0.70, 0.78, 0.80)),
    "py_block_paint": ("py_block_edge", (1, 1, 1)), "py_block_side_dark": ("py_block_side", (0.80, 0.84, 0.88)), "py_block_dark": ("py_block_side", (0.40, 0.48, 0.54)),
    "py_pit_wall": ("py_block_side", (0.86, 0.92, 0.94)), "py_pit_wall_dark": ("py_block_side", (0.70, 0.78, 0.82)), "py_stone_wet": ("py_block_side", (0.42, 0.52, 0.56)),
    "py_wall_moss": ("py_wall", (0.72, 0.94, 0.70)), "py_mud": ("py_dike", (0.80, 0.70, 0.56)), "py_rice_ripe": ("py_rice", (1.0, 0.84, 0.46)),
    "py_thatch_dark": ("py_thatch", (0.84, 0.82, 0.84)), "py_timber_dark": ("py_timber", (0.56, 0.54, 0.58)),
    "py_canopy_a": ("py_canopy", (0.66, 0.80, 0.78)), "py_canopy_b": ("py_canopy", (1, 1, 1)), "py_canopy_c": ("py_canopy", (0.46, 0.64, 0.66)),
    "py_trunk": ("py_tree_trunk", (1, 1, 1)), "py_foam": ("py_cloud", (1, 1, 1)), "py_coconut": ("py_palm_trunk", (1, 1, 1)),
    "py_skin": ("py_flat", (1, 1, 1)), "py_hide": ("py_flat", (1, 1, 1)), "py_feather": ("py_flat", (1, 1, 1)), "py_bronze": ("py_flat", (1, 1, 1)),
    "py_pad_speed": ("py_flat", (0.92, 0.68, 0.22)), "py_ink": ("py_flat", (0.12, 0.14, 0.18)),
    "py_far_1": ("py_ground", (0.80, 0.96, 0.96)), "py_far_2": ("py_ground", (0.80, 0.96, 1.0)), "py_far_3": ("py_ground", (0.82, 0.98, 1.0)), "py_far_4": ("py_ground", (0.84, 1.0, 1.0)),
    "py_far_5": ("py_ground", (0.86, 1.0, 1.0)), "py_far_slope": ("py_ground", (0.86, 0.94, 0.92)), "py_far_1b": ("py_grass", (1, 1, 1)),
    "py_cloth_red": ("py_weave", (1, 1, 1)),
}
_buf_mat, _buf_face = K.Buf.mat, K.Buf.face


def _mat(self, name):
    return _buf_mat(self, ALIAS[name][0] if name in ALIAS else name)


def _face(self, idx, uvs, mat, smooth=False, uv2=None):
    """Every face remembers a vertex tint: the buffer's current `tint` times its material name's own."""
    _buf_face(self, idx, uvs, mat, smooth, uv2)
    cols = self.__dict__.setdefault("cols", [])
    while len(cols) < len(self.f) - 1:
        cols.append((1.0, 1.0, 1.0))
    t = getattr(self, "tint", (1.0, 1.0, 1.0))
    a = ALIAS[mat][1] if mat in ALIAS else (1.0, 1.0, 1.0)
    cols.append((t[0] * a[0], t[1] * a[1], t[2] * a[2]))


K.Buf.mat = _mat
K.Buf.face = _face
K.MAT = MAT
K.TEXTURES = TEXTURES
K.ART = ART
Buf, T = K.Buf, K.T
UP = Vector((0, 0, 1))


def material(name):
    """Paete's run's material, then multiplied by the mesh's vertex colour (attribute "Col"), as the game's
    LagoonPainted does with its vertex tint switched on."""
    name = ALIAS[name][0] if name in ALIAS else name
    m = bpy.data.materials.get(name)
    if m and m.get("vcol"):
        return m
    m = K.material(name)
    if name in ("py_mayari", "py_spill") or "f" in MAT[name][3]:
        m["vcol"] = True
        return m
    N, L = m.node_tree.nodes, m.node_tree.links
    bsdf = N.get("Principled BSDF")
    col = N.new("ShaderNodeVertexColor"); col.layer_name = "Col"
    mix = N.new("ShaderNodeMix"); mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    src = bsdf.inputs["Base Color"]
    if src.links:
        L.new(src.links[0].from_socket, mix.inputs[6])
    else:
        mix.inputs[6].default_value = src.default_value
    L.new(col.outputs["Color"], mix.inputs[7])
    L.new(mix.outputs[2], bsdf.inputs["Base Color"])
    if "e" in MAT[name][3]:
        bsdf.inputs["Emission Color"].default_value = (*MAT[name][1], 1)
    m["vcol"] = True
    return m


def bake(o, b=None, mode="prop"):
    """Write vertex colour "Col" on a mesh: each face's tint, shaded by where it is.
      block   worn pale at the top, damp and dark down the shaft, stone-to-stone shifts
      wall    as block, by world height, for the pit and the gate
      land    broad soft hue and value drift, so no hillside is one colour
      prop    a little darker toward its foot; face-to-face shifts
      plain   the tints only"""
    me = o.data
    n = len(me.polygons)
    cols = np.ones((n, 3))
    if b is not None and getattr(b, "cols", None):
        c = np.array(b.cols + [(1.0, 1.0, 1.0)] * (n - len(b.cols)))[:n]
        cols *= c
    ctr = np.zeros(n * 3); me.polygons.foreach_get("center", ctr); ctr = ctr.reshape(n, 3)
    nor = np.zeros(n * 3); me.polygons.foreach_get("normal", nor); nor = nor.reshape(n, 3)
    x, y, z = ctr[:, 0], ctr[:, 1], ctr[:, 2]
    jit = (np.sin(x * 12.9898 + y * 78.233 + z * 37.719) * 43758.5453) % 1.0
    if mode in ("block", "wall"):
        ref = 0.0 if mode == "block" else float(z.max())
        depth = np.clip((ref - z) / 16.0, 0.0, 1.0)
        f = 1.04 - 0.46 * depth ** 0.7
        f = np.where(nor[:, 2] > 0.7, f * 1.06, np.where(nor[:, 2] < -0.5, f * 0.72, f))
        damp = np.clip((ref - z - 1.0) / 10.0, 0.0, 1.0)[:, None] * np.array([[-0.10, 0.02, 0.04]])
        cols = cols * (f * (0.93 + 0.14 * jit))[:, None] * (1.0 + damp)
    elif mode == "land":
        n1, n2 = nz(x / 37.0, y / 31.0, 3), nz(x / 13.0, y / 17.0, 8)
        cols = cols * (1.0 + 0.10 * n1 + 0.05 * n2)[:, None] * (1.0 + np.stack([0.06 * n2, 0.03 * n1, -0.05 * n2], 1))
        cols = np.where((nor[:, 2] < 0.5)[:, None], cols * (0.90 + 0.16 * jit)[:, None], cols)
    elif mode == "prop":
        zr = (z - z.min()) / max(1e-6, float(z.max() - z.min()))
        cols = cols * (0.80 + 0.22 * np.clip(zr * 2.2, 0, 1) + 0.06 * (jit - 0.5))[:, None]
        cols = np.where((nor[:, 2] < -0.5)[:, None], cols * 0.75, cols)
    cols = np.clip(cols, 0.0, 1.0)
    counts = np.zeros(n, dtype=np.int32); me.polygons.foreach_get("loop_total", counts)
    rgba = np.concatenate([np.repeat(cols, counts, axis=0), np.ones((int(counts.sum()), 1))], 1)
    att = me.color_attributes.get("Col") or me.color_attributes.new(name="Col", type="BYTE_COLOR", domain="CORNER")
    att.data.foreach_set("color_srgb" if hasattr(att.data[0], "color_srgb") else "color", rgba.ravel())
    try:
        me.color_attributes.active_color = att
        me.color_attributes.render_color_index = list(me.color_attributes).index(att)
    except Exception:
        pass
    for i, sl in enumerate(o.material_slots):
        if sl.material:
            sl.material = material(sl.material.name)


def decal(b, M, w, h, cell, off=0.006):
    """A decal quad in M's XZ plane (it faces local -Y), w wide, hanging h down from M's origin. `cell` is (column,
    row) of the 4 x 4 atlas, row 0 at the top."""
    u0, v0 = cell[0] * 0.25, 0.75 - cell[1] * 0.25
    pts = [M @ Vector((-w / 2, -off, -h)), M @ Vector((w / 2, -off, -h)), M @ Vector((w / 2, -off, 0)), M @ Vector((-w / 2, -off, 0))]
    b.quad([tuple(p) for p in pts], "py_decal", uvs=[(u0 + 0.01, v0 + 0.01), (u0 + 0.24, v0 + 0.01), (u0 + 0.24, v0 + 0.24), (u0 + 0.01, v0 + 0.24)])


def decal_flat(b, x, y, z, w, cell, yaw=0.0):
    """A decal lying on a floor at height z."""
    u0, v0 = cell[0] * 0.25, 0.75 - cell[1] * 0.25
    M = T(x, y, z, yaw)
    pts = [M @ Vector((-w / 2, -w / 2, 0)), M @ Vector((w / 2, -w / 2, 0)), M @ Vector((w / 2, w / 2, 0)), M @ Vector((-w / 2, w / 2, 0))]
    b.quad([tuple(p) for p in pts], "py_decal", uvs=[(u0 + 0.01, v0 + 0.01), (u0 + 0.24, v0 + 0.01), (u0 + 0.24, v0 + 0.24), (u0 + 0.01, v0 + 0.24)])


# ------------------------------------------------------------------ the place's numbers (the draft's)
STEP = 2.8
PLAZA_Y, LEDGE_Y, BREACH_Y, BREACH_HALF = -6.0, -10.0, -24.0, 12.4
LIP, PIT, FLOOR = 33.0, 30.0, -45.0
UPX, UPY = math.cos(math.radians(250.0)), math.sin(math.radians(250.0))
SHELF_X = [35.0, 51.0, 62.0, 73.0, 84.0, 95.0, 106.0]
SHELF_Y = -1.5
CLOUD_Y = -50.0
DIKE = 0.13            # how far across a paddy, in levels, its lip dike reaches


def shelf_span(x):
    return -13.0 - 0.12 * (x - 35.0), 29.0 + 0.10 * (x - 35.0)


def shelf_z(x):
    return SHELF_Y + 0.5 * max(0, min(5, sum(1 for b in SHELF_X[1:6] if x >= b)))


def sm(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def nz(x, y, k):
    return (np.sin(x + 1.7 * k) * np.cos(y * 1.3 - 0.9 * k) + 0.5 * np.sin(x * 2.3 - y * 1.9 + k) + 0.25 * np.cos(x * 4.1 + y * 3.7 + 2 * k)) / 1.75


def field(x, y):
    """The land at (x, y), arrays in, a dict of arrays out. THE SAME MOUNTAIN AS THE DRAFT (tools/author_payo_draft.py):
    h is the ground before it is terraced, phi = (h - plaza) / STEP its height in terrace levels, T how terraced the
    place is (1 full walls, 0 plain hillside), rock where it is bare cliff, e metres outside the plaza."""
    x, y = np.asarray(x, dtype=float), np.asarray(y, dtype=float)
    r = np.hypot(x, y) + 1e-6
    rs = (np.abs(x) ** 8 + np.abs(y) ** 8) ** 0.125
    e = np.maximum(0.0, rs - 41.0)
    c = (x * UPX + y * UPY) / r
    wup = sm(-0.15, 0.55, c)
    wdn = sm(0.05, 0.45, -c)
    g = 0.30 * c + 0.13 * wup
    es = np.where(g > 0, 520.0 * (1.0 - np.exp(-e / 520.0)), e)
    h = PLAZA_Y + es * g * (1.0 + 0.16 * nz(x / 90.0, y / 90.0, 1))
    h = h + 0.26 * np.maximum(0.0, es - 115.0) * wup * (1.0 + 0.5 * nz(x / 260.0, y / 260.0, 2))
    h = h - 0.0009 * np.maximum(0.0, e - 60.0) ** 2 * sm(0.0, 0.6, -c)
    q = x * (-UPY) + y * UPX
    h = h - 0.0010 * np.maximum(0.0, np.abs(q) - 70.0) ** 2 * (1.0 - 0.85 * wup)
    north = y - 36.0
    cliff = sm(0.0, 5.0, north) * sm(78.0, 44.0, np.abs(x))
    h = h - cliff * np.clip(north, 0.0, 70.0) * (1.05 + 0.25 * nz(x / 13.0, y / 17.0, 4))
    gw = 13.0 + 0.22 * np.maximum(0.0, y - 33.0)
    gm = sm(gw + 8.0, gw, np.abs(x)) * sm(33.0, 34.5, y)
    hg = BREACH_Y - 1.7 * np.maximum(0.0, y - 35.0)
    h = h + gm * (np.minimum(h, hg) - h)
    rock = np.maximum(cliff, gm)
    ing = (np.abs(x) < 24.0) & (y < -46.0) & (y > -99.0)
    h = np.where(ing, np.minimum(h, np.where(y > -58.0, -0.6, 5.5)), h)
    lo, hi = shelf_span(np.clip(x, 35.0, 106.0))
    smask = sm(22.0, 34.0, x) * sm(134.0, 108.0, x) * sm(lo - 24.0, lo, y) * sm(hi + 24.0, hi, y)
    top = SHELF_Y + 0.045 * np.clip(x - 35.0, 0.0, 71.0) - 1.6
    h = np.where(e > 0, h + smask * (np.maximum(h, top) - h), h)
    onshelf = (x > 34.9) & (x < 106.0) & (y > lo) & (y < hi)
    h = np.where(onshelf, np.minimum(h, top), h)
    n3 = nz(x / 55.0, y / 55.0, 5)
    tup = wup * (1.0 - sm(86.0, 128.0, e + 22.0 * n3))
    tdn = wdn * (1.0 - sm(200.0, 290.0, e + 25.0 * n3))
    T_ = sm(0.30, 0.62, np.maximum(tup, tdn) + 0.18 * nz(x / 23.0, y / 23.0, 7))
    T_ = T_ * (1.0 - np.clip(rock * 1.6, 0.0, 1.0)) * np.where(ing | onshelf, 0.0, 1.0) * sm(0.0, 6.0, smask * 0 + (1.0 - smask) * 6.0 + 0.0)
    T_ = np.where(e <= 0.0, 0.0, T_)
    phi = (h - PLAZA_Y) / STEP
    return {"h": h, "phi": phi, "e": e, "T": T_, "rock": rock, "c": c, "shelf": onshelf, "gate": ing, "smask": smask}


def surface(F):
    """The terraced surface of a field: each level's paddy flat, its lip dike 30 cm up, pulled toward that by T."""
    k = np.floor(F["phi"])
    f = F["phi"] - k
    zq = PLAZA_Y + STEP * k + np.where(f < DIKE, 0.3, 0.0)
    return F["h"] + F["T"] * (zq - F["h"])


def gz(x, y):
    return float(surface(field([x], [y]))[0])


def probe(x, y):
    F = field([x], [y])
    out = {k: float(v[0]) for k, v in F.items()}
    out["z"] = float(surface(F)[0])
    out["f"] = out["phi"] - math.floor(out["phi"])
    return out


def downhill(x, y):
    F = field([x + 0.6, x - 0.6, x, x], [y, y, y + 0.6, y - 0.6])["h"]
    v = Vector((F[1] - F[0], F[3] - F[2], 0.0))
    return v.normalized() if v.length > 1e-6 else Vector((0, 1, 0))


# far ranges, nearer than the draft's and smaller by the same factor, so they subtend what they did: (distance down
# the valley, half width, height over the valley floor, material)
RANGES = [(620.0, 190.0, 150.0, "py_far_1"), (960.0, 240.0, 250.0, "py_far_2"), (1420.0, 320.0, 350.0, "py_far_3"),
          (1980.0, 400.0, 450.0, "py_far_4"), (2650.0, 500.0, 560.0, "py_far_5")]
VALLEY_FLOOR = -130.0


def range_height(i, q, s):
    si, wi, ai, _ = RANGES[i]
    ridge = 0.62 + 0.38 * nz(q / (wi * 1.5), s / 520.0, i + 1) + 0.10 * nz(q / 90.0, s / 110.0, i + 4)
    return VALLEY_FLOOR + ai * np.exp(-((s - si) / wi) ** 2) * ridge


# ------------------------------------------------------------------ round forms
def ellipsoid(b, M, rx, ry, rz, mat, n=10, rings=6, smooth=True):
    prof = [(max(0.001, rx * math.sin(math.pi * j / rings)), -rz * math.cos(math.pi * j / rings)) for j in range(rings + 1)]
    b.lathe(M, prof, n, mat, smooth=smooth, squash=ry / rx)


def limb(b, M, p0, p1, r0, r1, mat, n=8):
    """A tapered round bar between two points given in M's frame."""
    b.tube([M @ Vector(p0), M @ Vector(p1)], r0 * M.to_scale().x, mat, n=n, r1=r1 * M.to_scale().x)


def band(b, x0, x1, y0, y1, z0, z1, mat, period=1.2, proud=0.0):
    """Four upright faces round a box, UVs laid so one period of a trim texture is `period` metres and its v runs the
    band's height once. For py_carve and py_weave."""
    x0, x1, y0, y1 = x0 - proud, x1 + proud, y0 - proud, y1 + proud
    run = 0.0
    for (a, c) in (((x0, y0), (x1, y0)), ((x1, y0), (x1, y1)), ((x1, y1), (x0, y1)), ((x0, y1), (x0, y0))):
        ln = math.hypot(c[0] - a[0], c[1] - a[1])
        n = max(1, round(ln / period))
        b.quad([(a[0], a[1], z0), (c[0], c[1], z0), (c[0], c[1], z1), (a[0], a[1], z1)], mat,
               uvs=[(run, 0.0), (run + n, 0.0), (run + n, 1.0), (run, 1.0)])
        run += n


def fog_all(group):
    """For the review pictures only: every material fades to the haze by distance and to mist by depth, as the draft's."""
    for m in bpy.data.materials:
        if not m.use_nodes or m.get("fogged") or m.name in ("py_mayari", "py_spill"):
            continue
        N, L = m.node_tree.nodes, m.node_tree.links
        out = next((n for n in N if n.type == "OUTPUT_MATERIAL"), None)
        if out is None or not out.inputs[0].links:
            continue
        cur = out.inputs[0].links[0].from_socket
        g = N.new("ShaderNodeGroup"); g.node_tree = group
        for key in ("pit", "dist"):
            e = N.new("ShaderNodeEmission")
            L.new(g.outputs[key + "_colour"], e.inputs[0])
            mx = N.new("ShaderNodeMixShader")
            L.new(g.outputs[key], mx.inputs[0]); L.new(cur, mx.inputs[1]); L.new(e.outputs[0], mx.inputs[2])
            cur = mx.outputs[0]
        L.new(cur, out.inputs[0])
        m["fogged"] = True
