"""Build the whole Kanto sample map in Blender and export it for Unity.

  blender -b --python tools/author_kanto_city.py -- [--preview N] [--no-export]

What it makes, all from the kit in tools/author_kanto_models.py (walls in one piece, swept
mouldings, recessed windows, the painted textures, shingled leaf foliage):

  ArtSource/kanto/kanto_city.blend       the assembled map, every piece a collection instance
  Assets/TumbangPreso/Art/Kanto/Models/*.glb   one file per model, geometry + material NAMES
  Assets/TumbangPreso/Art/Kanto/kanto_layout.json   where each model goes (Unity axes)

THE LAYOUT IS THE APPROVED BLOCKOUT (tools/author_kanto_blockout.py, owner: "this looks good,
proceed"): a 26 x 26 m park as the play area, a 10 m ring road, buildings facing the park from
30 m, grid streets running out. Its numbers are imported from there, not retyped.

⚠️ NO TWO SURFACES SHARE A PLANE. The blockout's road slab and pavement blocks both sat at 0
and z-fought across the city (owner, 2026-09-24: "make sure this doesnt survive the final
pass"). The ground here is built as ONE set of non-overlapping cells: a cell is road OR
pavement OR lawn OR court, never two stacked. Markings sit 6 mm above what they mark.

⚠️ TEXTURES ARE NOT EMBEDDED IN THE .glb FILES. Every model shares the same dozen painted
textures; embedding them would copy each one into every file. The .glb carries material names
and UVs, and Unity's KantoSceneBuilder builds one shared material per name from
ArtSource/kanto/textures (see MATERIAL_SPECS, written into the layout JSON).

COORDINATES: a Blender point (x, y, z) lands in Unity at (-x, z, -y). The glTF exporter writes
(x, z, -y) and glTFast then NEGATES X to go from right- to left-handed (Jobs.cs,
`destination->x = -src`). Models and layout go through the same conversion, so the city is not
mirrored. A rotation of t about Blender Z is -t degrees about Unity Y.
"""
import json
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_kanto_models as K          # noqa: E402  (the building kit)
import author_kanto_blockout as B        # noqa: E402  (the approved layout numbers)

ROOT = Path(__file__).resolve().parents[1]
MODELS_OUT = ROOT / "Assets" / "TumbangPreso" / "Art" / "Kanto" / "Models"
LAYOUT_OUT = ROOT / "Assets" / "TumbangPreso" / "Art" / "Kanto" / "kanto_layout.json"
UP = Vector((0, 0, 1))

EXTRA_PALETTE = {
    "plaster_cream": (0.93, 0.84, 0.64),
    "plaster_mint":  (0.52, 0.76, 0.64),
    "plaster_butter": (0.95, 0.80, 0.42),
    "plaster_rose":  (0.88, 0.55, 0.55),
    "plaster_sage":  (0.58, 0.70, 0.50),
    "plaster_sky":   (0.55, 0.72, 0.78),
    "mustard":       (0.85, 0.60, 0.16),
    "concrete":      (0.72, 0.70, 0.66),
    "tin_teal":      (0.24, 0.50, 0.48),
    "tin_red":       (0.62, 0.22, 0.17),
    "railing":       (0.14, 0.26, 0.22),
    "asphalt":       (0.29, 0.28, 0.31),
    "paving":        (0.79, 0.75, 0.68),
    "court":         (0.89, 0.84, 0.74),
    "grass":         (0.43, 0.63, 0.23),
    "kerb":          (0.85, 0.83, 0.77),
    "road_paint":    (0.93, 0.92, 0.86),
    "lane_yellow":   (0.95, 0.78, 0.20),
    "trunk":         (0.40, 0.27, 0.16),
    "pole":          (0.26, 0.28, 0.27),
    "timber_pole":   (0.42, 0.31, 0.22),
    "wire":          (0.09, 0.09, 0.10),
    "signal_red":    (0.95, 0.20, 0.16),
    "signal_amber":  (0.98, 0.84, 0.22),
    "signal_green":  (0.30, 0.85, 0.42),
    "bin_green":     (0.24, 0.50, 0.34),
    "lamp_glass":    (0.98, 0.94, 0.78),
    # VARIETY (owner: "you're reusing the same materials/textures making it all look the
    # same. the talisman ref has variety in their buildings' design styles").
    "brick_brown":   (0.40, 0.20, 0.12),
    "panel":         (0.85, 0.83, 0.79),
    "stucco_tan":    (0.66, 0.50, 0.32),
    "stucco_olive":  (0.50, 0.50, 0.32),
    "frame_dark":    (0.06, 0.10, 0.09),
    "metal_dark":    (0.12, 0.14, 0.15),
    # Shop bases. Kept clear of the role hues (Art_Direction.md 1: orange #f87020 and blue
    # #0080e8 mean offence and defence), so the reference's bright orange shop is ochre here.
    "paint_ochre":   (0.80, 0.55, 0.10),
    "paint_red":     (0.55, 0.10, 0.08),
    "paint_green":   (0.12, 0.34, 0.20),
    "paint_teal":    (0.10, 0.36, 0.36),
    # Downtown curtain walls: reflective, so the sky tints them; kept teal, green and slate
    # rather than blue so no tower reads as the defence colour.
    # Owner, review v8: "windows on the glass buildings should be less reflective. make it
    # match the rest of the windows". These are the window glass's own colours (3f8b86 at the
    # foot of a pane, lightening upward), with one greener and one deeper variant.
    "curtain_teal":  (0.050, 0.258, 0.238),
    "curtain_green": (0.056, 0.262, 0.170),
    "curtain_dark":  (0.030, 0.165, 0.160),
    "mullion_light": (0.80, 0.82, 0.79),
    "steel":         (0.30, 0.33, 0.33),
    "trunk_grey":    (0.46, 0.40, 0.34),
    "panel_sand":    (0.80, 0.66, 0.48),
    "panel_terracotta": (0.72, 0.42, 0.30),
    "panel_sage":    (0.58, 0.66, 0.52),
    "panel_cream":   (0.92, 0.86, 0.72),
    "roof_warm":     (0.50, 0.42, 0.36),
    "roof_light":    (0.70, 0.68, 0.64),
    "roof_tile":     (0.60, 0.22, 0.14),
    "roof_tile_green": (0.20, 0.38, 0.28),
    "roof_tile_grey": (0.34, 0.31, 0.30),
    "brass":         (0.72, 0.55, 0.20),
    "sign_green":    (0.10, 0.40, 0.22),
}
K.PALETTE.update(EXTRA_PALETTE)
# Which painted texture each material wears in Blender previews (Unity reads MATERIAL_SPECS).
K.TEXTURED.update({"asphalt": "asphalt", "paving": "paving", "court": "court", "grass": "grass",
                   "brick_brown": "brick_brown", "panel": "panel"})
for name in EXTRA_PALETTE:
    if name.startswith(("plaster_", "stucco_")):
        K.TEXTURED.setdefault(name, "plaster")
K.TEX_SCALE.update({"plaster": 0.5, "asphalt": 0.4, "grass": 0.5})
# Owner on review v3: "a lot of models are untextured. like the tree bodies, traffic lamp and
# streetposts, park railings". Bark, painted street metal and timber, all NEUTRAL (tinted by
# each material's own colour), painted by author_kanto_textures.py --only bark,metal,timber.
for _name, _tex in (("trunk", "bark"), ("trunk_grey", "bark"), ("timber_pole", "timber"), ("pole", "metal"),
                    ("railing", "metal"), ("metal", "metal"), ("metal_dark", "metal"), ("steel", "metal"),
                    ("bin_green", "metal"), ("brass", "metal"), ("sign_green", "metal")):
    K.TEXTURED[_name] = _tex
for _name in ("panel_sand", "panel_terracotta", "panel_sage", "panel_cream"):
    K.TEXTURED[_name] = "panelg"
# Roof tiles and bark: the drawings the owner approved on swatch sheet v2. They are painted in
# their own colours (clay, slate, brown), so they are NOT neutral and are never tinted.
K.TEXTURED["roof_tile"], K.TEXTURED["roof_tile_grey"] = "tiles_clay", "tiles_slate"
K.TEXTURED["roof_tile_green"] = "tiles_teal"   # glazed barrel tiles, swatch sheet v4
K.NEUTRAL_TEX.update({"metal", "timber", "panelg"})
K.ANTI_TILE.update({"plaster", "asphalt", "grass"})

# The same, for Unity: texture, tint (sRGB-ish multiplier, 1 = texture as painted), tiling.
TINTED = lambda rgb: [round(min(1.0, c * 1.2) ** (1 / 2.2), 4) for c in rgb]  # noqa: E731


def material_specs():
    specs = {}
    for name, rgb in K.PALETTE.items():
        tex = K.TEXTURED.get(name, "paint")
        if name.startswith(("leaf_light", "leaf_dark")):
            specs[name] = {"texture": "leaf", "tint": TINTED(rgb), "tiling": 1.0, "foliage": True}
            continue
        if name.startswith("curtain_"):
            specs[name] = {"texture": None, "tint": [round(c ** (1 / 2.2), 4) for c in rgb], "tiling": 1.0,
                           "glossy": True}
            continue
        if name in ("leaf_core", "soil", "ground", "wire", "signal_red", "signal_amber", "signal_green", "lamp_glass"):
            specs[name] = {"texture": None, "tint": [round(c ** (1 / 2.2), 4) for c in rgb], "tiling": 1.0,
                           "emissive": name.startswith("signal") or name == "lamp_glass"}
            continue
        tinted = name not in K.TEXTURED or name == "stone_shade" or tex in K.NEUTRAL_TEX
        tint = TINTED(rgb) if tinted else [1.0, 1.0, 1.0]
        if name == "stone_shade":
            tint = [0.82, 0.78, 0.70]
        specs[name] = {"texture": tex, "tint": tint, "tiling": K.TEX_SCALE.get(tex, 1.0),
                       "glossy": name == "glass"}
    return specs


_kit_material = K.material


def _material(name):
    """Curtain glass drawn like the street's windows, not a mirror (owner, review v8: "less
    reflective ... match the rest of the windows"). The window texture is one soft vertical
    gradient, darker at the foot of a pane and lighter at the head; here the same gradient
    repeats once per 3.6 m storey (object height, from the 8.9 m podium top), because the
    painted 2 m window tile repeated up a 120 m tower read as stripes. No metal, a matte
    finish: the sky no longer washes the towers out to grey."""
    if name.startswith("curtain_") and bpy.data.materials.get(name) is None:
        m = bpy.data.materials.new(name)
        rgb = K.PALETTE[name]
        m.diffuse_color = (*rgb, 1)
        m.use_nodes = True
        nodes, links = m.node_tree.nodes, m.node_tree.links
        bsdf = nodes["Principled BSDF"]
        bsdf.inputs["Metallic"].default_value = 0.0
        bsdf.inputs["Roughness"].default_value = 0.45
        coord, split = nodes.new("ShaderNodeTexCoord"), nodes.new("ShaderNodeSeparateXYZ")
        links.new(coord.outputs["Object"], split.inputs[0])
        shift, wrap = nodes.new("ShaderNodeMath"), nodes.new("ShaderNodeMath")
        shift.operation, shift.inputs[1].default_value = "SUBTRACT", 8.9
        wrap.operation, wrap.inputs[1].default_value = "FLOORED_MODULO", 3.6
        scale = nodes.new("ShaderNodeMath")
        scale.operation, scale.inputs[1].default_value = "DIVIDE", 3.6
        links.new(split.outputs["Z"], shift.inputs[0])
        links.new(shift.outputs[0], wrap.inputs[0])
        links.new(wrap.outputs[0], scale.inputs[0])
        ramp = nodes.new("ShaderNodeValToRGB")
        top = tuple(min(1.0, c * 1.55) for c in rgb)   # 45 per cent of the way to 7cc2b8, as the window texture
        ramp.color_ramp.elements[0].color, ramp.color_ramp.elements[1].color = (*rgb, 1), (*top, 1)
        links.new(scale.outputs[0], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
        return m
    return _kit_material(name)


K.material = _material   # Buf.finish looks `material` up in the kit's module at call time


# ------------------------------------------------------------------ the generic building

class Ctx:
    """Everything a building's extras need: its buffers, facades and measurements."""


def city_building(name, foot, order, ground, storey, upper, zones, *, seed=1, bay=3.3,
                  win_margin=1.05, win_h=None, recess=0.3, trim="stone", frame="frame",
                  courses="stone", cornice=K.CORNICE, cornice_mat="stone", parapet="brick",
                  shops=True, door=None, awnings=None, keystones=True, sills=True,
                  planters=True, acs=True, wrap=False, shop_frame="frame", extras=None,
                  window_style="classic", fascia="trim", slabs=None, roof="roof"):
    """A building from the house kit. `foot` is the footprint (Blender XY), `order` the street
    facade indices (consecutive), `ground` the ground-floor height, `storey`/`upper` the upper
    floors. `door` is (facade index, u) for a street door. `extras(ctx)` adds anything bespoke
    into the same buffers before they are finished."""
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    rng = random.Random(seed)
    TOP = ground + storey * upper
    xs, ys = [p[0] for p in foot], [p[1] for p in foot]
    inside = ((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2)
    ins = Vector((*inside, 0))
    facades = [K.Facade(foot[i], foot[(i + 1) % len(foot)], inside) for i in range(len(foot))]
    # WINDOW STYLES. "classic": punched windows with sills, lintels and keystones (the brick
    # corner). "grid": a loft's big dark-framed window, 3 panes high, narrow piers between.
    # "ribbon": one long band of glass per storey across the whole face, thin dark mullions.
    if window_style == "grid":
        win_margin, win_h = min(win_margin, 0.5), win_h or storey - 0.8
    elif window_style == "ribbon":
        bay, win_margin, win_h = 999.0, 1.0, win_h or storey - 1.2
    win_h = win_h or storey - 1.45
    windows, shop_bays = [], []
    for fi in order:
        f = facades[fi]
        n = max(1, round(f.length / bay))
        b = f.length / n
        margin = min(win_margin, b * 0.4)
        for s in range(upper):
            z0 = ground + s * storey
            for k in range(n):
                u = (k + 0.5) * b
                w = b - margin
                zc = z0 + 0.95 + win_h / 2 if win_h < storey - 1.0 else z0 + 0.45 + win_h / 2
                f.openings.append((u - w / 2, u + w / 2, zc - win_h / 2, zc + win_h / 2, recess, "glass"))
                windows.append((f, u, zc, w, win_h, fi, s, k))
        if not shops:
            continue
        for k in range(n):
            u = (k + 0.5) * b
            if door and door[0] == fi and abs(u - door[1]) < b * 0.5 + 0.4:
                continue
            w = b - min(0.95, b * 0.3)
            f.openings.append((u - w / 2, u + w / 2, 0.85, min(3.35, ground - 1.1), 0.22, "glass"))
            shop_bays.append((f, u, w))
    if door:
        f = facades[door[0]]
        if not 1.2 < door[1] < f.length - 1.2:
            raise ValueError(f"{name}: door at u={door[1]} is off its {f.length:.2f} m wall")
        f.openings.append((door[1] - 1.0, door[1] + 1.0, 0.0, min(2.9, ground - 1.3), 0.5, "glass"))

    K.wall_shell("shell", facades, TOP, ground, zones, roof).finish(col, bevel=0.035)
    c = Ctx()
    c.col, c.rng, c.facades, c.order, c.inside, c.ins, c.TOP = col, rng, facades, order, inside, ins, TOP
    c.ground, c.storey, c.upper, c.windows, c.shops = ground, storey, upper, windows, shop_bays
    c.trim, c.frames = K.Buf("trim"), K.Buf("frames")
    c.cloth, c.props, c.leaves = K.Buf("awnings"), K.Buf("props"), K.Buf("foliage", foliage=True)
    trimb = c.trim

    # Mouldings: plinth, fascia over the shops, a course at every floor, cornice + parapet.
    path_idx = list(range(len(facades))) if wrap else order
    closed = wrap
    if door and not wrap:
        f = facades[door[0]]
        # Split the plinth at the door, as the brick corner does.
        pts_a = [facades[order[0]].a] + [facades[i].b for i in order[:order.index(door[0])]] + [f.at(door[1] - 1.05, 0)]
        pts_b = [f.at(door[1] + 1.05, 0)] + [facades[i].b for i in order[order.index(door[0]):]]
        trimb.sweep([Vector((p.x, p.y, 0)) for p in pts_a], K.PLINTH, "stone_shade", ins)
        trimb.sweep([Vector((p.x, p.y, 0)) for p in pts_b], K.PLINTH, "stone_shade", ins)
    else:
        K.moulding(trimb, facades, path_idx, 0.0, K.PLINTH, "stone_shade", inside, closed=closed)
    if shops:
        K.moulding(trimb, facades, path_idx, ground - 1.05, K.FASCIA, fascia, inside, closed=closed)
    if courses:
        for s in range(upper):
            K.moulding(trimb, facades, path_idx, ground + s * storey, K.COURSE, courses, inside, closed=closed)
    if slabs:
        # Projecting floor slabs: the glass mid-rise's horizontal lines.
        for s in range(1, upper + 1):
            K.moulding(trimb, facades, path_idx, ground + s * storey - 0.05, SLAB, slabs, inside, closed=closed)
    if cornice:
        K.moulding(trimb, facades, None, TOP - 1.0, cornice, cornice_mat, inside, closed=True)
    if parapet:
        K.moulding(trimb, facades, None, TOP + 0.04, K.PARAPET, parapet, inside, closed=True)
        K.moulding(trimb, facades, None, TOP + 0.97, K.COPING, trim, inside, closed=True)
    for f, u, zc, w, h, fi, s, k in windows:
        if sills and window_style == "grid":
            trimb.sweep([f.at(u - w / 2 - 0.08, zc - h / 2), f.at(u + w / 2 + 0.08, zc - h / 2)], K.SILL, trim, ins)
        elif sills and window_style == "classic":
            wob = rng.uniform(-0.015, 0.015)
            trimb.sweep([f.at(u - w / 2 - 0.2, zc - h / 2 + wob), f.at(u + w / 2 + 0.2, zc - h / 2 - wob)], K.SILL, trim, ins)
            trimb.sweep([f.at(u - w / 2 - 0.14, zc + h / 2), f.at(u + w / 2 + 0.14, zc + h / 2)], K.LINTEL, trim, ins)
            if keystones:
                K.keystone(trimb, f, u, zc + h / 2)
    for f, u, w in shop_bays:
        for side in (-1, 1):
            uu = min(max(u + side * (w / 2 + 0.2), 0.35), f.length - 0.35)
            trimb.box(f.frame(uu, (ground - 1.05) / 2, 0.06), (0.5, 0.34, ground - 1.1), "stone")
    if door:
        f = facades[door[0]]
        dh = min(2.9, ground - 1.3)
        trimb.sweep([f.at(door[1] - 1.16, dh), f.at(door[1] + 1.16, dh)], K.LINTEL, trim, ins)
        trimb.box(f.frame(door[1], 0.05, -0.1), (2.5, 0.9, 0.14), "stone_shade")
        K.window_frame(c.frames, f, door[1], dh / 2, 1.96, dh - 0.04, cols=2, bar=0.15, depth=0.1,
                       out=-0.5 + 0.14, mat="wood", transom=dh / 2 - 0.6)
        c.props.box(f.frame(door[1], 0.5, -0.5 + 0.08), (1.9, 0.06, 0.86), "wood")

    for f, u, zc, w, h, *_ in windows:
        if window_style == "grid":
            cols, bar, tr = max(2, round(w / 0.85)), 0.09, [-h / 6, h / 6]
        elif window_style == "ribbon":
            cols, bar, tr = max(2, round(w / 1.25)), 0.07, None
        else:
            cols, bar, tr = 2, 0.12, h * 0.2
        K.window_frame(c.frames, f, u, zc, w - 0.02, h - 0.02, cols=cols, bar=bar, depth=0.1,
                       out=-recess + 0.14, mat=frame, transom=tr)
    for f, u, w in shop_bays:
        top = min(3.35, ground - 1.1)
        K.window_frame(c.frames, f, u, (0.85 + top) / 2, w - 0.02, top - 0.87, cols=max(1, round(w / 1.2)),
                       bar=0.13, depth=0.1, out=-0.07, mat=shop_frame, transom=(top - 0.85) / 2 - 0.55)
    for fi, stripes in (awnings or {}).items():
        f = facades[fi]
        K.awning(c.cloth, f, f.length / 2, ground - 1.2, f.length - 1.4, 1.2, 0.66, stripes)
    for f, u, zc, w, h, fi, s, k in windows:
        sill_top = zc - h / 2 + 0.035
        if planters and (s + k + fi + seed) % 4 == 0:
            soil = K.planter(c.props, f, u, sill_top, w * 0.72)
            basis = f.frame(0, 0).to_3x3()
            for p in (-1, 0, 1):
                K.foliage(c.leaves, f.at(u + p * w * 0.23, soil + 0.07, 0.05), (0.2, 0.18, 0.2), 200, 0.13, rng,
                          floor_z=soil - 0.02, basis=basis)
        elif acs and (s * 7 + k * 3 + fi + seed) % 5 == 1:
            K.ac_unit(c.props, f, u + w * 0.18, sill_top - 0.005, 0.02)
    if extras:
        extras(c)
    for buf, bev, seg in ((c.trim, 0.03, 2), (c.frames, 0.015, 1), (c.cloth, 0.012, 1), (c.props, 0.03, 2)):
        if len(buf.bm.faces):
            buf.finish(col, bevel=bev, segments=seg)
        else:
            buf.bm.free()
    if len(c.leaves.bm.faces):
        c.leaves.finish(col)
    else:
        c.leaves.bm.free()
    return col


def water_tank(props, x, y, z, r=1.2):
    for dx in (-0.9, 0.9):
        for dy in (-0.9, 0.9):
            props.box(Matrix.Translation((x + dx, y + dy, z + 1.3)), (0.2, 0.2, 2.6), "metal")
    props.box(Matrix.Translation((x, y, z + 2.66)), (2.5, 2.5, 0.16), "wood")
    props.cylinder((x, y, z + 3.9), r, 2.3, "tank", sides=20)
    for hz in (3.0, 3.9, 4.8):
        props.cylinder((x, y, z + hz), r + 0.05, 0.1, "metal", sides=20)
    props.cylinder((x, y, z + 5.4), r + 0.12, 0.6, "metal", sides=20, top_scale=0.15)


# ------------------------------------------------------------------ the buildings

def deco_corner():
    """Cream plaster, a ROUNDED street corner with a curved shop front, mustard fins between
    the bays, and a stepped round turret over the corner. Same lot and orientation as the
    brick corner: street faces on -X and +Y, the corner at the origin."""
    W = D = 18.0
    R = 6.0
    arc = [(R - R * math.cos(a), -R + R * math.sin(a)) for a in [math.pi / 2 * i / 5 for i in range(6)]]
    foot = arc + [(W, 0), (W, -D), (0, -D)]
    n = len(foot)
    # Facades: 0..4 the arc, 5 the +Y street, 6 and 7 party walls, 8 the -X street.
    order = [8, 0, 1, 2, 3, 4, 5]

    def extras(c):
        f5, f8 = c.facades[5], c.facades[8]
        for f in (f5, f8):
            nb = max(1, round(f.length / 3.3))
            for k in range(1, nb):
                u = k * f.length / nb
                c.trim.box(f.frame(u, (c.ground + c.TOP) / 2 - 0.4, 0.14), (0.34, 0.4, c.TOP - c.ground - 1.2), "mustard")
        # The turret: stacked drums on the corner's centre, stepping in, with mustard bands.
        cx, cy = R, -R
        z = c.TOP
        for i, (r, h, m) in enumerate([(R + 0.1, 1.2, "plaster_cream"), (R - 0.6, 0.35, "mustard"),
                                       (R - 1.4, 1.6, "plaster_cream"), (R - 1.9, 0.3, "mustard"),
                                       (R - 2.8, 1.2, "plaster_cream"), (R - 3.2, 0.3, "mustard")]):
            c.props.cylinder((cx, cy, z + h / 2), r, h, m, sides=40)
            z += h
        c.props.cylinder((cx, cy, z + 1.6), 0.06, 3.2, "metal", sides=8)
        c.props.box(Matrix.Translation((cx + 0.45, cy, z + 2.7)), (0.8, 0.02, 0.5), "awning_a")
        water_tank(c.props, 13.0, -12.0, c.TOP)

    return city_building("deco_corner", foot, order, 4.4, 3.4, 2, ("stone_blocks", "plaster_cream"), seed=5,
                         bay=3.2, trim="stone", courses="mustard", cornice_mat="mustard", parapet="plaster_cream",
                         door=(8, 10.5), awnings={5: ["awning_c", "awning_b"], 8: ["awning_c", "awning_b"]},
                         keystones=False, extras=extras)


def glass_tower():
    """A two-storey stone podium with shops, and a teal glass tower set back above it: ribbon
    glazing every storey, pale spandrel bands, vertical fins, a crown with plant and antenna."""
    W = D = 18.0
    foot = [(0, 0), (W, 0), (W, -D), (0, -D)]

    def extras(c):
        # The tower, its own shell, every face glazed.
        t0, S, n = 2.0, 14.0, 10
        tfoot = [(t0, -t0), (t0 + S, -t0), (t0 + S, -t0 - S), (t0, -t0 - S)]
        tin = (t0 + S / 2, -t0 - S / 2)
        tf = [K.Facade(tfoot[i], tfoot[(i + 1) % 4], tin) for i in range(4)]
        base, st = c.TOP + 1.0, 3.5
        for f in tf:
            nb = 5
            b = f.length / nb
            for s in range(n):
                for k in range(nb):
                    u = (k + 0.5) * b
                    f.openings.append((u - b / 2 + 0.16, u + b / 2 - 0.16, s * st + 0.9, s * st + 3.2, 0.18, "glass"))
        shell = K.wall_shell("tower", tf, n * st, 0.0, ("concrete", "concrete"), "roof")
        shell.bm.transform(Matrix.Translation((0, 0, base)))
        shell.finish(c.col, bevel=0.04)
        tb = K.Buf("tower trim")
        for s in range(n + 1):
            K.moulding(tb, tf, None, base + s * st + 0.35, K.COURSE, "stone", tin, closed=True)
        K.moulding(tb, tf, None, base + n * st - 0.2, K.CORNICE, "concrete", tin, closed=True)
        # The step from the podium roof up to the tower: a plinth ring.
        K.moulding(tb, tf, None, base - 1.0, K.PLINTH, "stone_shade", tin, closed=True)
        # Fills the step from the podium roof to the tower; overlaps both by 5 cm so it never
        # shares a plane with either.
        tb.box(Matrix.Translation((tin[0], tin[1], base - 0.5)), (S - 0.2, S - 0.2, 1.1), "concrete")
        for f in tf:
            for k in range(6):
                u = k * f.length / 5
                tb.box(f.frame(u, base + n * st / 2, 0.08), (0.22, 0.3, n * st - 0.6), "metal")
        tb.finish(c.col, bevel=0.03)
        fr = K.Buf("tower frames")
        for f in tf:
            b = f.length / 5
            for s in range(n):
                for k in range(5):
                    u = (k + 0.5) * b
                    K.window_frame(fr, f, u, base + s * st + 2.05, b - 0.34, 2.28, cols=2, bar=0.08, depth=0.06,
                                   out=-0.1, mat="metal")
        fr.bm.transform(Matrix.Identity(4))
        fr.finish(c.col, bevel=0.01, segments=1)
        # Crown: plant room, a railing frame, an antenna and a red beacon.
        top = base + n * st + 1.0
        c.props.box(Matrix.Translation((tin[0] + 2, tin[1] - 1, top + 1.4)), (6, 5, 2.8), "concrete")
        c.props.box(Matrix.Translation((tin[0] + 2, tin[1] - 1, top + 2.9)), (6.4, 5.4, 0.2), "stone")
        c.props.cylinder((tin[0] - 3, tin[1] + 3, top + 4), 0.1, 8, "metal", sides=8)
        c.props.cylinder((tin[0] - 3, tin[1] + 3, top + 8.2), 0.35, 0.4, "signal_red", sides=12)

    return city_building("glass_tower", foot, [3, 0], 4.6, 4.0, 1, ("stone_blocks", "concrete"), seed=7, bay=3.6,
                         win_h=2.8, trim="stone", courses="stone", parapet="concrete", door=(0, 4.5),
                         awnings={3: ["awning_a", "awning_b"]}, keystones=False, planters=False, extras=extras)


def townhouse(name, x0, w, h_storeys, colour, tin, seed, side_street=False):
    """One narrow house: shop below, two floors over it, a steep tin gable facing the street
    with eaves that overhang too far, and a bay window pushed out over the pavement."""
    D = 10.0
    foot = [(x0 - w, 0), (x0, 0), (x0, -D), (x0 - w, -D)]
    order = [0] if not side_street else [0, 1]
    ground, storey = 3.6, 3.0

    def extras(c):
        f = c.facades[0]
        TOP = c.TOP
        ridge = w * 0.55
        # Gable ends front and back, solid, painted like the wall.
        for y in (0.0, -D):
            tri = [Vector((x0 - w - 0.02, y, TOP)), Vector((x0 + 0.02, y, TOP)), Vector((x0 - w / 2, y, TOP + ridge))]
            back = [p + Vector((0, -0.3 if y == 0 else 0.3, 0)) for p in tri]
            c.props.face(tri, colour)
            c.props.face(list(reversed(back)), colour)
            for j in range(3):
                c.props.face([tri[j], back[j], back[(j + 1) % 3], tri[(j + 1) % 3]], colour)
        # Roof slopes: two tin planes with a deep overhang, ribs, and a ridge cap.
        half = w / 2 + 0.6
        slope = math.atan2(ridge, w / 2)
        L = math.hypot(half, ridge * half / (w / 2))
        for side in (-1, 1):
            cx = x0 - w / 2 + side * half / 2
            # Rotation about Y by +slope tips +X DOWN, which is the right-hand slope.
            zc = TOP + ridge - (ridge / (w / 2)) * (half / 2) + 0.08
            m = Matrix.Translation((cx, -D / 2, zc)) @ Matrix.Rotation(side * slope, 4, "Y")
            c.props.box(m, (L, D + 1.4, 0.14), tin)
            for r in range(int(D / 0.5)):
                c.props.box(m @ Matrix.Translation((0, -D / 2 - 0.5 + r * 0.5, 0.08)), (L, 0.05, 0.05), tin)
        c.props.box(Matrix.Translation((x0 - w / 2, -D / 2, TOP + ridge + 0.02)), (0.3, D + 1.5, 0.2), tin)
        # A bay window pushed out over the street on the first upper floor.
        z = c.ground + 0.5
        c.props.box(f.frame(w / 2, z + 1.2, 0.55), (w * 0.55, 1.1, 2.4), colour)
        c.props.box(f.frame(w / 2, z + 1.35, 1.12), (w * 0.45, 0.06, 1.5), "glass")
        K.window_frame(c.frames, f, w / 2, z + 1.35, w * 0.47, 1.62, cols=3, bar=0.1, depth=0.06, out=1.18, mat="frame")
        c.props.box(f.frame(w / 2, z + 2.5, 0.6), (w * 0.62, 1.3, 0.16), tin)
        c.props.box(f.frame(w / 2, z - 0.05, 0.55), (w * 0.6, 1.2, 0.14), "stone")
        for s in (-1, 1):
            c.props.box(f.frame(w / 2 + s * w * 0.22, z - 0.35, 0.4), (0.18, 0.7, 0.5), "wood")

    col = city_building(name, foot, order, ground, storey, h_storeys, ("stone_blocks", colour), seed=seed, bay=w / 2,
                        win_margin=0.9, recess=0.25, courses=None, cornice=None, cornice_mat="stone",
                        parapet=None, door=None, awnings={0: [tin, "awning_b"]}, keystones=False, extras=extras)
    return col


def townhouse_row():
    """Three narrow houses on the SW lot, each leaning a degree or two (Brainchild), plus a
    plain block behind them. Lot: x in [-18, 0], y in [-18, 0]; fronts face +Y, the end
    house's side wall faces +X."""
    root = bpy.data.collections.new("townhouse_row")
    bpy.context.scene.collection.children.link(root)
    specs = [("townhouse_a", 0.0, 6.2, 2, "plaster_butter", "tin_red", 11, True),
             ("townhouse_b", -6.2, 5.4, 3, "plaster_rose", "tin_teal", 12, False),
             ("townhouse_c", -11.6, 6.4, 2, "plaster_sage", "tin_red", 13, False)]
    for i, (name, x0, w, h, colour, tin, seed, side) in enumerate(specs):
        col = townhouse(name, x0, w, h, colour, tin, seed, side)
        # The lean: rotate the house about its own front-bottom corner.
        pivot = Vector((x0 - w / 2, 0, 0))
        lean = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians([1.2, -0.9, 1.5][i]), 4, "Y") @ Matrix.Translation(-pivot)
        for o in col.objects:
            o.matrix_world = lean @ o.matrix_world
        bpy.context.scene.collection.children.unlink(col)
        root.children.link(col)
    back = city_building("townhouse_back", [(-18, -10.3), (0, -10.3), (0, -18), (-18, -18)], [1], 3.6, 3.0, 2,
                         ("stone_blocks", "plaster_sky"), seed=14, shops=False, planters=False, awnings=None,
                         keystones=False)
    bpy.context.scene.collection.children.unlink(back)
    root.children.link(back)
    return root


SLAB = [(-0.12, -0.10), (0.42, -0.10), (0.48, -0.04), (0.48, 0.06), (0.42, 0.10), (-0.12, 0.10)]


def penthouse(c, inset, storeys, wall, frame="metal_dark", st=3.1):
    """A setback volume on the roof: ribbon-glazed on its street face, walls of `wall`
    elsewhere, a flat roof with a thin rail. Stands `inset` back from the street facade."""
    f0 = c.facades[c.order[0]]
    w = f0.length
    D = 7.0
    base = c.TOP + 0.02
    foot = [f0.at(inset, 0) - f0.out * inset, f0.at(w - inset, 0) - f0.out * inset,
            f0.at(w - inset, 0) - f0.out * (inset + D), f0.at(inset, 0) - f0.out * (inset + D)]
    foot = [(p.x, p.y) for p in foot]
    cen = (sum(p[0] for p in foot) / 4, sum(p[1] for p in foot) / 4)
    pf = [K.Facade(foot[i], foot[(i + 1) % 4], cen) for i in range(4)]
    ww = pf[0].length
    for sdx in range(storeys):
        pf[0].openings.append((0.4, ww - 0.4, sdx * st + 0.5, sdx * st + st - 0.4, 0.15, "glass"))
    shell = K.wall_shell("penthouse", pf, storeys * st, 0.0, (wall, wall), "roof")
    shell.bm.transform(Matrix.Translation((0, 0, base)))
    shell.finish(c.col, bevel=0.03)
    fr = K.Buf("penthouse frames")
    for sdx in range(storeys):
        K.window_frame(fr, pf[0], ww / 2, base + sdx * st + (st - 0.9) / 2 + 0.5, ww - 0.82, st - 0.92,
                       cols=max(2, round(ww / 1.3)), bar=0.07, depth=0.06, out=-0.1, mat=frame)
    fr.finish(c.col, bevel=0.01, segments=1)
    K.moulding(c.trim, pf, None, base + storeys * st - 0.1, SLAB, "concrete", cen, closed=True)


def bays(c, columns, s0, s1, wall, frame="frame_dark"):
    """Cantilevered box bays pushed 0.9 m out over the street, spanning storeys s0..s1,
    glazed on the front with a dark grid. The reference's stucco blocks are full of them."""
    f = c.facades[c.order[0]]
    n = max(1, round(f.length / 3.3))
    b = f.length / n
    z0, z1 = c.ground + s0 * c.storey + 0.2, c.ground + (s1 + 1) * c.storey - 0.25
    for k in columns:
        u = (k + 0.5) * b
        c.props.box(f.frame(u, (z0 + z1) / 2, 0.45), (b - 0.2, 0.9, z1 - z0), wall)
        c.props.box(f.frame(u, z1 + 0.08, 0.5), (b + 0.05, 1.0, 0.16), "concrete")
        for s in range(s0, s1 + 1):
            zc = c.ground + s * c.storey + c.storey / 2 + 0.05
            c.props.box(f.frame(u, zc, 0.92), (b - 0.7, 0.04, c.storey - 1.0), "glass")
            K.window_frame(c.frames, f, u, zc, b - 0.66, c.storey - 0.96, cols=3, bar=0.08, depth=0.05,
                           out=0.95, mat=frame, transom=[-(c.storey - 0.96) / 6])


# ------------------------------------------------------------------ roofs and massing
#
# Owner on review v3: "most of the other buildings are essentially just big rectangles ... you
# did not mess around with the shapes at all. i want more organic and fun shapes". Tiny
# Talisman's roofs are busy (AC units, vents, stair houses, antennas, dishes, skylights, water
# tanks) and Brainchild's houses wear steep tiled roofs with dormers and corner turrets. These
# are added to the street archetypes so the same footprint grows a different skyline.

def _free(taken, x0, y0, x1, y1):
    for a0, b0, a1, b1 in taken:
        if x0 < a1 and a0 < x1 and y0 < b1 and b0 < y1:
            return False
    taken.append((x0, y0, x1, y1))
    return True


def roof_clutter(c, w, D, rng, taken, wall):
    """Rooftop life on a flat roof (x 0..w, y -D..0; the parapet stands 0.55 m in). Each item
    sinks 2 cm into the roof slab."""
    z = c.TOP - 0.02
    p = c.props

    def spot(sx, sy):
        for _ in range(30):
            x = rng.uniform(0.9 + sx / 2, w - 0.9 - sx / 2)
            y = rng.uniform(-D + 0.9 + sy / 2, -0.9 - sy / 2)
            if _free(taken, x - sx / 2 - 0.3, y - sy / 2 - 0.3, x + sx / 2 + 0.3, y + sy / 2 + 0.3):
                return x, y
        return None

    s = spot(2.6, 2.3)   # a stair house with a door and a little roof
    if s:
        x, y = s
        p.box(Matrix.Translation((x, y, z + 1.3)), (2.6, 2.3, 2.6), wall)
        p.box(Matrix.Translation((x, y, z + 2.66)), (2.9, 2.6, 0.16), "concrete")
        p.box(Matrix.Translation((x - 0.3, y + 1.16, z + 1.05)), (0.9, 0.06, 2.0), "wood")
    for _ in range(rng.randint(1, 3)):   # AC condensers: a box, a fan, a grille
        s = spot(1.3, 0.9)
        if s:
            x, y = s
            p.box(Matrix.Translation((x, y, z + 0.45)), (1.3, 0.9, 0.9), "white")
            p.cylinder((x, y, z + 0.92), 0.34, 0.06, "metal_dark", sides=16)
            for k in range(3):
                p.box(Matrix.Translation((x - 0.3 + k * 0.3, y, z + 0.95)), (0.03, 0.7, 0.03), "metal")
    for _ in range(rng.randint(2, 4)):   # vent stacks with caps
        s = spot(0.4, 0.4)
        if s:
            x, y = s
            h = rng.uniform(0.6, 1.3)
            p.cylinder((x, y, z + h / 2), 0.13, h, "metal", sides=10)
            p.cylinder((x, y, z + h + 0.06), 0.22, 0.14, "metal", sides=10, top_scale=0.3)
    if rng.random() < 0.6:   # a satellite dish on a short post
        s = spot(1.0, 1.0)
        if s:
            x, y = s
            p.cylinder((x, y, z + 0.4), 0.05, 0.8, "metal", sides=8)
            m = Matrix.Translation((x, y, z + 0.95)) @ Matrix.Rotation(math.radians(55), 4, "X") @ Matrix.Rotation(rng.uniform(0, 6.3), 4, "Y")
            r = bmesh.ops.create_cone(p.bm, cap_ends=True, segments=16, radius1=0.45, radius2=0.12, depth=0.16, matrix=m)
            p._paint(r["verts"], "white")
    if rng.random() < 0.5:   # an antenna mast
        s = spot(0.6, 0.6)
        if s:
            x, y = s
            p.cylinder((x, y, z + 2.2), 0.04, 4.4, "metal", sides=6)
            for k, hh in enumerate((3.2, 3.8, 4.3)):
                p.box(Matrix.Translation((x, y, z + hh)), (1.2 - k * 0.3, 0.04, 0.04), "metal")
    if rng.random() < 0.5:   # a skylight
        s = spot(1.8, 1.2)
        if s:
            x, y = s
            p.box(Matrix.Translation((x, y, z + 0.2)), (1.8, 1.2, 0.4), "metal_dark")
            p.box(Matrix.Translation((x, y, z + 0.42)), (1.6, 1.0, 0.06), "glass")


def pitched_roof(c, w, D, tile, wall, rng, dormers=True):
    """A steep tiled roof across the whole block, ridge parallel to the street, eaves pushed
    out too far (Brainchild), solid gable ends in the wall colour, and dormers on the street
    slope. Replaces the parapet."""
    TOP, ov = c.TOP, 0.55
    h = D * 0.36
    slope = math.atan2(h, D / 2)
    L = (D / 2 + ov) / math.cos(slope)
    p = c.props
    for side in (1, -1):   # +1: the street slope, running from the front eave up to the ridge
        yc = -D / 2 + side * (D / 2 + ov) / 2
        zc = TOP + h - (h / (D / 2)) * ((D / 2 + ov) / 2) + 0.1
        m = Matrix.Translation((w / 2, yc, zc)) @ Matrix.Rotation(-side * slope, 4, "X")
        p.box(m, (w + 2 * ov * 0.6, L, 0.16), tile)
    p.box(Matrix.Translation((w / 2, -D / 2, TOP + h + 0.12)), (w + 0.8, 0.34, 0.24), tile)   # ridge cap
    for x in (0.0, w):   # gable ends, 0.3 thick, inside the eaves
        xin = x + (0.16 if x == 0 else -0.16)
        tri = [Vector((xin, 0.02, TOP - 0.02)), Vector((xin, -D - 0.02, TOP - 0.02)), Vector((xin, -D / 2, TOP + h - 0.05))]
        back = [v + Vector((0.3 if x == 0 else -0.3, 0, 0)) for v in tri]
        p.face(tri, wall)
        p.face(list(reversed(back)), wall)
        for j in range(3):
            p.face([tri[j], back[j], back[(j + 1) % 3], tri[(j + 1) % 3]], wall)
    if dormers:
        n = max(1, round(w / 3.3))
        b = w / n
        for k in range(n):
            if (k + c.upper) % 2:
                continue
            u = (k + 0.5) * b
            y0 = -1.1                              # dormer face, on the slope
            zs = TOP + h * (-y0 / (D / 2)) - 0.1   # where the slope is at the dormer face
            dh = 1.7
            p.box(Matrix.Translation((u, y0 - 0.9, zs + dh / 2 - 0.2)), (1.7, 1.8, dh + 0.4), wall)
            p.box(Matrix.Translation((u, y0 + 0.02, zs + 0.75)), (1.1, 0.08, 1.1), "glass")
            K.window_frame(c.frames, K.Facade((u - 1, y0), (u + 1, y0), (u, y0 - 1)), 1.0, zs + 0.75, 1.1, 1.1,
                           cols=2, bar=0.08, depth=0.06, out=0.06, mat="frame")
            for sd in (-1, 1):
                mm = Matrix.Translation((u + sd * 0.5, y0 - 0.8, zs + dh + 0.25)) @ Matrix.Rotation(sd * math.radians(38), 4, "Y")
                p.box(mm, (1.25, 2.1, 0.1), tile)


def turret(c, w, D, wall, cap, rng, at_left=True):
    """A corner bay tower: square, pushed 0.45 m proud of the street face, rising one storey
    past the roof with its own windows, a cornice, and a pyramid cap with a finial."""
    s = 3.0
    x = 1.6 if at_left else w - 1.6
    y = -1.5
    top = c.TOP + c.storey
    p = c.props
    p.box(Matrix.Translation((x, y + 0.45, (c.ground + top) / 2)), (s, s, top - c.ground), wall)
    f = K.Facade((x - s / 2, y + 0.45 + s / 2), (x + s / 2, y + 0.45 + s / 2), (x, y))
    for k in range(c.upper + 1):
        zc = c.ground + k * c.storey + c.storey / 2
        p.box(f.frame(s / 2, zc, -0.08), (1.3, 0.2, 1.6), "glass")
        K.window_frame(c.frames, f, s / 2, zc, 1.34, 1.64, cols=2, bar=0.08, depth=0.06, out=0.03, mat="frame")
    p.box(Matrix.Translation((x, y + 0.45, top + 0.12)), (s + 0.5, s + 0.5, 0.24), "stone")
    m = Matrix.Translation((x, y + 0.45, top + 1.45)) @ Matrix.Rotation(math.pi / 4, 4, "Z")
    r = bmesh.ops.create_cone(p.bm, cap_ends=True, segments=4, radius1=(s + 0.4) * 0.72, radius2=0.06, depth=2.5, matrix=m)
    p._paint(r["verts"], cap)
    p.cylinder((x, y + 0.45, top + 3.0), 0.05, 0.9, "brass", sides=6)


# The far street blocks, GENERATED rather than listed: 24 buildings from the six kinds, each
# with its own width, height, wall paint, shop paint and roof. The first roster had 8, each
# placed 12 times, and the owner saw it straight away: "noticeable repeating textures and
# colors". A kind's paints are chosen for it; white panel is gone from the palette entirely.
WALLS = {
    "loft": ["brick", "brick_brown"],
    "loft_ph": ["brick", "brick_brown"],
    "stucco": ["stucco_tan", "stucco_olive", "plaster_butter", "plaster_rose"],
    "shophouse": ["plaster_cream", "plaster_mint", "plaster_butter", "plaster_rose", "plaster_sage", "plaster_sky"],
    "panel": ["panel_sand", "panel_terracotta", "panel_sage", "panel_cream"],
    "glassmid": ["panel_sand", "panel_terracotta", "panel_sage"],
}
SHOPS = ["paint_green", "paint_ochre", "paint_red", "paint_teal"]


def _roster():
    rng = random.Random(404)
    kinds = ["loft"] * 5 + ["loft_ph"] * 3 + ["stucco"] * 4 + ["shophouse"] * 6 + ["panel"] * 3 + ["glassmid"] * 3
    rng.shuffle(kinds)
    out = []
    for i, kind in enumerate(kinds):
        storeys = rng.randint(3, 5) if kind == "shophouse" else rng.randint(4, 9)
        roof = "flat"
        if storeys <= 5 and kind in ("shophouse", "loft") and rng.random() < 0.6:
            roof = "pitched"
        elif rng.random() < 0.3 and kind in ("shophouse", "stucco", "panel"):
            roof = "turret"
        out.append((f"far_{kind}_{i:02d}", kind, round(rng.uniform(7.5, 12.0), 1), storeys,
                    rng.choice(WALLS[kind]), 500 + i, rng.choice(SHOPS), roof))
    return out


FAR_ROSTER = _roster()


def archetype(name, kind, w, storeys, colour, seed, detail=True, shop="paint_green", roof="flat"):
    """The street roster: six kinds of building, each a different construction, not a repaint.
      loft      brick, big dark-framed grid windows, painted shop base, water tank
      loft_ph   the loft with a glazed penthouse set back on the roof
      stucco    stucco, ribbon windows, cantilevered bays, a penthouse
      glassmid  concrete panels, ribbon glazing, projecting floor slabs
      panel     concrete panels, grid windows in cream frames, balconies
      shophouse painted plaster, classic windows and balconies (the first walk-up)
    `detail=False` drops planters and wall AC units for the far street blocks.
    `roof`: flat (with rooftop clutter), pitched (tiles and dormers), turret (a corner tower)."""
    D = 14.0
    foot = [(0, 0), (w, 0), (w, -D), (0, -D)]
    rr = random.Random(seed * 7 + 3)
    common = dict(seed=seed, planters=detail, acs=detail, keystones=False,
                  roof=rr.choice(("roof", "roof", "roof_warm", "roof_light")))
    # Owner: "when using the red clay tiles, dont use it on a building with the bricks texture,
    # otherwise it'll all look the same". Brick buildings get slate. (Green tiles wait for
    # their own approved drawing.)
    tile = (rr.choice(("roof_tile_grey", "roof_tile_green")) if colour in ("brick", "brick_brown")
            else rr.choice(("roof_tile", "roof_tile", "roof_tile_grey", "roof_tile_green")))

    def extras(c):
        taken = []
        if kind in ("loft", "loft_ph", "shophouse") and roof != "pitched" and (detail or rr.random() < 0.5):
            water_tank(c.props, w * 0.66, -D * 0.62, c.TOP, r=1.0)
            taken.append((w * 0.66 - 1.8, -D * 0.62 - 1.8, w * 0.66 + 1.8, -D * 0.62 + 1.8))
        if kind == "loft_ph" and roof != "pitched":
            penthouse(c, 1.6, 1, "brick_brown" if colour == "brick" else "concrete")
            taken.append((0, -1.6 - 7.4, w, 0))
        if kind == "stucco":
            n = max(1, round(w / 3.3))
            bays(c, [k for k in range(n) if k % 2 == 1] or [0], 0, max(0, c.upper - 2), colour)
            if roof != "pitched":
                penthouse(c, 1.8, 1, colour)
                taken.append((0, -1.8 - 7.4, w, 0))
        if kind in ("panel", "shophouse"):
            walkup_balconies(c)
        if roof == "pitched":
            pitched_roof(c, w, D, tile, colour, rr)
        else:
            if roof == "turret":
                at_left = rr.random() < 0.5
                turret(c, w, D, colour, tile, rr, at_left)
                taken.append((0, -3.5, 3.6, 0) if at_left else (w - 3.6, -3.5, w, 0))
            roof_clutter(c, w, D, rr, taken, "concrete" if kind in ("loft", "loft_ph") else colour)

    if kind in ("loft", "loft_ph"):
        return city_building(name, foot, [0], 4.4, 3.3, storeys, (shop, colour), window_style="grid",
                             frame="frame_dark", shop_frame="frame_dark", fascia=shop, courses="stone",
                             cornice=K.COURSE if kind == "loft_ph" else K.CORNICE,
                             parapet=None if roof == "pitched" else colour, recess=0.22,
                             awnings={0: [["awning_a", "awning_b"], ["awning_c", "awning_b"]][seed % 2]} if detail else None,
                             extras=extras, **common)
    if kind == "stucco":
        return city_building(name, foot, [0], 4.2, 3.2, storeys, ("stone_blocks", colour), window_style="ribbon",
                             frame="metal_dark", shop_frame="metal_dark", fascia=shop, courses=None,
                             slabs="concrete", cornice=None, parapet=None if roof == "pitched" else colour,
                             recess=0.18, extras=extras, **common)
    if kind == "glassmid":
        return city_building(name, foot, [0], 4.4, 3.4, storeys, (colour, colour), window_style="ribbon",
                             frame="metal_dark", shop_frame="metal_dark", fascia="metal_dark", courses=None,
                             slabs="concrete", cornice=None, parapet=colour, recess=0.2, extras=extras, **common)
    if kind == "panel":
        return city_building(name, foot, [0], 4.2, 3.1, storeys, (shop, colour), window_style="grid",
                             frame="frame", shop_frame="frame", fascia=shop, courses="concrete",
                             cornice=K.COURSE, parapet=colour, recess=0.2, extras=extras, **common)
    return city_building(name, foot, [0], 4.2, 3.2, storeys, ("stone_blocks", colour), bay=3.1, recess=0.26,
                         parapet=None if roof == "pitched" else colour, courses="stone", fascia=shop,
                         awnings={0: [["awning_a", "awning_b"], ["awning_c", "awning_b"]][seed % 2]} if detail else None,
                         extras=extras, **common)


def walkup_balconies(c):
    for f, u, zc, ww, h, fi, s, k in c.windows:
        if (k + s) % 2 == 0:
            zb = zc - h / 2
            c.props.box(f.frame(u, zb - 0.08, 0.55), (ww + 0.6, 1.1, 0.16), "concrete")
            rail = zb + 0.95
            c.props.box(f.frame(u, rail, 1.08), (ww + 0.6, 0.06, 0.06), "railing")
            for side in (-1, 1):
                c.props.box(f.frame(u + side * (ww / 2 + 0.28), rail, 0.58), (0.06, 1.0, 0.06), "railing")
            nb = int((ww + 0.5) / 0.16)
            for j in range(nb + 1):
                c.props.box(f.frame(u - (ww + 0.5) / 2 + j * (ww + 0.5) / nb, zb + 0.46, 1.08), (0.03, 0.03, 0.96), "railing")


# (name, kind, width, storeys, wall colour, seed, shop paint): the park-facing blocks, detailed.
PARKSIDE = [
    ("loft_red_5", "loft", 9.5, 5, "brick", 21, "paint_green"),
    ("stucco_tan_6", "stucco", 9.0, 6, "stucco_tan", 22, "paint_ochre"),
    ("shophouse_mint_4", "shophouse", 9.5, 4, "plaster_mint", 23, "paint_red"),
    ("glassmid_7", "glassmid", 9.5, 7, "panel_terracotta", 24, "metal_dark"),
    ("loft_brown_ph_4", "loft_ph", 9.0, 4, "brick_brown", 25, "paint_teal"),
    ("panel_5", "panel", 9.5, 5, "panel_sand", 26, "paint_red"),
    ("shophouse_rose_3", "shophouse", 9.5, 3, "plaster_rose", 27, "paint_green"),
    ("stucco_olive_5", "stucco", 9.0, 5, "stucco_olive", 28, "paint_red"),
    ("loft_red_ph_6", "loft_ph", 9.5, 6, "brick", 29, "paint_ochre"),
    ("shophouse_butter_4", "shophouse", 9.5, 4, "plaster_butter", 30, "paint_teal"),
    ("loft_brown_3", "loft", 9.0, 3, "brick_brown", 31, "paint_green"),
    ("glassmid_5", "glassmid", 9.5, 5, "panel_sage", 32, "metal_dark"),
]
PARKSIDE_ROOF = {"shophouse_rose_3": "pitched", "loft_brown_3": "pitched", "shophouse_mint_4": "turret",
                 "panel_5": "turret", "shophouse_butter_4": "pitched"}
# The far street blocks: FAR_ROSTER, generated above (24 buildings, own widths, paints, roofs).
FILLERS = FAR_ROSTER
# STREET ENDS. Review v7 looked down a street and saw it run off the edge of the ground into
# nothing. Each outward street now ends at a long building across its far end (a vista), one
# per arm, each its own construction and colour.
TERMINI = [
    ("terminus_n", "shophouse", 62.0, 4, "plaster_cream", 601, "paint_green", "pitched"),
    ("terminus_e", "loft", 62.0, 6, "brick", 602, "paint_ochre", "flat"),
    ("terminus_s", "stucco", 62.0, 5, "stucco_tan", 603, "paint_red", "flat"),
    ("terminus_w", "panel", 62.0, 6, "panel_sage", 604, "paint_teal", "turret"),
]
# ------------------------------------------------------------------ park and street pieces

def kit(name):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def limb(buf, p, q, r0, r1, mat, sides=10):
    """A tapered round branch from p to q (any direction): one cone, capped."""
    p, q = Vector(p), Vector(q)
    d = q - p
    m = Matrix.Translation((p + q) / 2) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4()
    r = bmesh.ops.create_cone(buf.bm, cap_ends=True, segments=sides, radius1=r0, radius2=r1, depth=d.length + 0.04, matrix=m)
    buf._paint(r["verts"], mat)


def tube(buf, pts, radii, mat, sides=10):
    """ONE continuous tapered tube through a polyline: a ring at every point, turned to the
    average of the segments either side, joined by quads. The trunk used to be one cone per
    segment, and every join showed as a crack round the trunk (owner, review v8 on the bark)."""
    pts = [Vector(p) for p in pts]
    rings = []
    for i, p in enumerate(pts):
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        q = d.to_track_quat("Z", "Y").to_matrix()
        rings.append([buf.bm.verts.new(p + q @ Vector((math.cos(a) * radii[i], math.sin(a) * radii[i], 0)))
                      for a in (k * math.tau / sides for k in range(sides))])
    idx = buf.mi(mat)
    for r0, r1 in zip(rings, rings[1:]):
        for k in range(sides):
            buf.bm.faces.new((r0[k], r0[(k + 1) % sides], r1[(k + 1) % sides], r1[k])).material_index = idx
    buf.bm.faces.new(list(reversed(rings[0]))).material_index = idx
    buf.bm.faces.new(rings[-1]).material_index = idx


def skin_wood(buf, nodes, edges, radii, mat, root=0, levels=2):
    """ONE FLUID PIECE OF WOOD from a skeleton. Owner, review v10: "can you make it so that tree
    trunks are actually fluidly one model? this one is a bunch of cylindrical segments". The
    trunk, root spurs, fork and every limb are the vertices and edges of one skeleton; Blender's
    Skin modifier wraps it in a single closed surface with smooth crotches where branches
    leave, and a subdivision pass rounds it. The result is baked into `buf` as ordinary faces,
    so the exported model is plain geometry. `radii` are the wanted radii; the skin is built
    a little fatter because subdivision shrinks it."""
    me = bpy.data.meshes.new("skeleton")
    me.from_pydata([tuple(n) for n in nodes], edges, [])
    ob = bpy.data.objects.new("skeleton", me)
    bpy.context.scene.collection.objects.link(ob)
    sk = ob.modifiers.new("skin", "SKIN")
    sk.branch_smoothing = 0.6
    sk.use_smooth_shade = True
    for i, r in enumerate(radii):
        v = me.skin_vertices[0].data[i]
        v.radius = (r * 1.3, r * 1.3)
        v.use_root = (i == root)
    sub = ob.modifiers.new("round", "SUBSURF")
    sub.levels = sub.render_levels = levels
    dg = bpy.context.evaluated_depsgraph_get()
    baked = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    for poly in baked.polygons:
        buf.face([baked.vertices[i].co.copy() for i in poly.vertices], mat)
    bpy.data.objects.remove(ob)
    bpy.data.meshes.remove(me)
    bpy.data.meshes.remove(baked)


class Skeleton:
    """A growing list of nodes (position, radius) and edges for skin_wood."""

    def __init__(self):
        self.nodes, self.radii, self.edges = [], [], []

    def add(self, p, r, parent=None):
        self.nodes.append(Vector(p))
        self.radii.append(r)
        i = len(self.nodes) - 1
        if parent is not None:
            self.edges.append((parent, i))
        return i

    def branch(self, start, tip, r0, r1, parent, steps=3, rise=0.0):
        """A limb from node `parent` (at `start`) to `tip`, bowed upward by `rise` mid-way."""
        start, tip = Vector(start), Vector(tip)
        last = parent
        for k in range(1, steps + 1):
            t = k / steps
            p = start.lerp(tip, t) + Vector((0, 0, rise * 4 * t * (1 - t)))
            last = self.add(p, r0 + (r1 - r0) * t, last)
        return last


# THE TREE ROSTER. Owner on review v3: the trees "all look the same, are oriented the same way,
# and are all the same size and color and type". Tiny Talisman's trees are big clumpy crowns,
# often a bright lime, on thick trunks that BEND. Variation here is per TREE (shape, trunk,
# size, leaf tint); per-LEAF colour was tried and reverted by the owner (design guide 5).
TREE_TINTS = {
    "_lime":  ((0.55, 0.80, 0.10), (0.26, 0.50, 0.05)),
    "_deep":  ((0.16, 0.46, 0.10), (0.05, 0.22, 0.05)),
    "_olive": ((0.50, 0.60, 0.12), (0.24, 0.32, 0.06)),
}


def _curve(sk, parent, p0, p1, p2, r0, r1, step=0.22):
    """A smooth limb along a quadratic curve p0 -> p2 (pulled toward p1), a node every ~22 cm,
    radius easing from r0 to r1. Returns the last node."""
    p0, p1, p2 = Vector(p0), Vector(p1), Vector(p2)
    n = max(3, int(((p1 - p0).length + (p2 - p1).length) / step))
    last = parent
    for k in range(1, n + 1):
        t = k / n
        p = p0 * (1 - t) ** 2 + p1 * 2 * t * (1 - t) + p2 * t * t
        last = sk.add(p, r0 + (r1 - r0) * t ** 0.8, last)
    return last


def tree(name, style, seed, tint="", trunk="trunk", s=1.0):
    """THE TREE, third build, drawn from the owner's references (review v15: "the trunk designs
    look horrible and wonky. i need you to reference actual stylized designs"). A stylized
    painted tree is a SLENDER trunk with one gentle curve and a smooth taper, a small flare only
    in its own radius at the foot (no root claws, no knobs), that SPLITS into two or three thin
    branches rising steeply into the crown, each ending in a leaf cluster; the broad tree forks
    once more. All of it is one skinned piece of wood (skin_wood).
    styles: round, broad, lean, young, tall (tall is replaced by the pine)."""
    col = kit(name)
    rng = random.Random(seed)
    wood, leaves = K.Buf("trunk"), K.Buf("foliage", foliage=True)
    wood.uv_mode = "trunk"
    clumps = []   # (centre, radii)
    sk = Skeleton()
    lean = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)).normalized() * s
    H = {"young": 2.3, "round": 2.4, "broad": 1.9, "lean": 2.2, "tall": 3.8}[style] * s
    R = {"young": 0.09, "round": 0.2, "broad": 0.24, "lean": 0.19, "tall": 0.17}[style] * s
    base = sk.add((0, 0, -0.06), R * 1.3)
    foot = sk.add((0, 0, 0.25 * s), R * 1.05, base)
    bow = 0.45 if style == "lean" else 0.18
    split_at = Vector(lean * (0.6 if style == "lean" else 0.12)) + Vector((0, 0, H))
    top = _curve(sk, foot, (0, 0, 0.25 * s), lean * bow + Vector((0, 0, H * 0.55)), split_at, R, R * 0.62)
    P = sk.nodes[top]
    rt = R * 0.62
    if style == "young":
        _curve(sk, top, P, P + Vector((0, 0, 0.3 * s)), P + Vector((0, 0, 0.7 * s)), rt, 0.025)
        clumps.append((P + Vector((0, 0, 0.8 * s)), (1.05 * s, 1.05 * s, 0.95 * s)))
        for k in range(4):   # a tree guard and an iron grate, as before
            a = k * math.tau / 4 + math.pi / 4
            wood.box(Matrix.Translation((math.cos(a) * 0.42, math.sin(a) * 0.42, 0.7)), (0.05, 0.05, 1.4), "railing")
        for z in (0.5, 1.35):
            wood.cylinder((0, 0, z), 0.47, 0.05, "railing", sides=16)
        wood.cylinder((0, 0, 0.005), 0.75, 0.03, "metal_dark", sides=4)
    elif style == "tall":
        _curve(sk, top, P, P + Vector((0, 0, 1.2 * s)), P + Vector((0, 0, 2.6 * s)), rt, 0.03)
        for dz, r in ((0.2, 1.3), (1.6, 1.15), (2.9, 0.85)):
            clumps.append((P + Vector((0, 0, dz * s)), (r * s, r * s, r * 0.95 * s)))
    else:
        n = {"round": 3, "broad": 4, "lean": 2}[style]
        a0 = rng.uniform(0, math.tau)
        spread = {"round": 0.62, "broad": 0.95, "lean": 0.55}[style]      # radians from vertical
        reach = {"round": 1.9, "broad": 2.4, "lean": 1.9}[style] * s
        for i in range(n):
            a = a0 + i * math.tau / n + rng.uniform(-0.25, 0.25)
            d = Vector((math.cos(a) * math.sin(spread), math.sin(a) * math.sin(spread), math.cos(spread)))
            tip = P + d * reach + Vector((0, 0, 0.35 * s))
            mid = P + d * reach * 0.45
            end = _curve(sk, top, P, mid, tip, rt * 0.8, 0.035 * s)
            size = {"round": 1.25, "broad": 1.35, "lean": 1.45}[style] * s
            # Lifted above the branch tip, so the branches show under the canopy (reference).
            clumps.append((tip + Vector((0, 0, 0.75 * s)), (size, size, size * 0.8)))
            if style == "broad":
                # One more fork half-way out: a twig rising to its own cluster.
                j = sk.nodes[end]
                fork = P + d * reach * 0.55 + Vector((0, 0, 0.2 * s))
                twig = fork + Vector((math.cos(a + 0.9), math.sin(a + 0.9), 0)) * 0.9 * s + Vector((0, 0, 1.1 * s))
                near = min(range(len(sk.nodes)), key=lambda q: (sk.nodes[q] - fork).length)
                _curve(sk, near, sk.nodes[near], sk.nodes[near].lerp(twig, 0.5) + Vector((0, 0, 0.1)), twig,
                       rt * 0.45, 0.03 * s)
                clumps.append((twig + Vector((0, 0, 0.3 * s)), (1.1 * s, 1.1 * s, 0.95 * s)))
                del j
        # The crown's centre, carried by a short leader so no cluster floats.
        _curve(sk, top, P, P + Vector((0, 0, 0.7 * s)), P + Vector((0, 0, 1.5 * s)), rt * 0.7, 0.04 * s)
        cs = {"round": 1.55, "broad": 1.5, "lean": 1.25}[style] * s
        clumps.append((P + Vector((0, 0, 2.0 * s)), (cs, cs, cs * 0.8)))
    skin_wood(wood, sk.nodes, sk.edges, sk.radii, trunk, levels=3)   # 3: no crease down the trunk
    for c, r in clumps:
        K.foliage(leaves, c, r, int(520 * r[0] * r[1]), 0.34 * s, rng, lift=0.3, spread=0.45, tint=tint)
    wood.finish(col, bevel=0)   # already round; a bevel would only crease the skinned surface
    leaves.finish(col)
    return col


TREES = {
    # The park's four corner trees: four shapes, four tints.
    "tree_broad_lime":   dict(style="broad", seed=201, tint="_lime"),
    "tree_round":        dict(style="round", seed=202),
    "tree_lean_olive":   dict(style="lean", seed=203, tint="_olive", trunk="trunk_grey"),
    "tree_tall_deep":    dict(style="tall", seed=204, tint="_deep"),
    # Street trees, mixed along the kerbs.
    "street_young":      dict(style="young", seed=211, tint="_lime", s=0.9),
    "street_round_deep": dict(style="round", seed=212, tint="_deep", s=0.72),
    "street_tall":       dict(style="tall", seed=213, trunk="trunk_grey", s=0.75),
    "street_broad":      dict(style="broad", seed=214, tint="_olive", s=0.62),
}
PARK_TREES = ["tree_broad_lime", "tree_round", "tree_lean_olive", "tree_tall_deep"]
for _t, (_light, _dark) in TREE_TINTS.items():
    K.PALETTE["leaf_light" + _t], K.PALETTE["leaf_dark" + _t] = _light, _dark
STREET_TREES = ["street_young", "street_round_deep", "street_tall", "street_broad"]


def hedge(name="hedge_bed"):
    """A 4.5 m hedge bed: a low stone kerb and a long shingled clump. Under 1 m tall."""
    col = kit(name)
    rng = random.Random(51)
    stone, leaves = K.Buf("kerb"), K.Buf("foliage", foliage=True)
    for side in (-1, 1):
        stone.box(Matrix.Translation((0, side * 0.55, 0.12)), (4.5, 0.12, 0.24), "stone")
        stone.box(Matrix.Translation((side * 2.2, 0, 0.12)), (0.12, 1.0, 0.24), "stone")
    stone.box(Matrix.Translation((0, 0, 0.1)), (4.3, 0.98, 0.18), "soil")
    for x in (-1.5, 0, 1.5):
        K.foliage(leaves, (x, 0, 0.35), (0.95, 0.48, 0.4), 420, 0.14, rng, floor_z=0.16)
    stone.finish(col, bevel=0.02)
    leaves.finish(col)
    return col


def bench(name="park_bench"):
    """Wooden slats on two cast-iron ends with a curled leg and an armrest, bolt heads on the
    slats. The back faces +Y."""
    col = kit(name)
    b = K.Buf("bench")
    for i in range(3):
        b.box(Matrix.Translation((0, -0.2 + i * 0.16, 0.45)), (1.9, 0.13, 0.06), "wood")
    for i in range(2):
        b.box(Matrix.Translation((0, 0.28, 0.63 + i * 0.17)) @ Matrix.Rotation(math.radians(-12), 4, "X"), (1.9, 0.05, 0.12), "wood")
    for x in (-0.82, 0.82):
        # A cast end: front leg, back leg rising into the back support, a seat rail, an arm.
        b.box(Matrix.Translation((x, -0.22, 0.21)) @ Matrix.Rotation(math.radians(8), 4, "X"), (0.08, 0.08, 0.44), "railing")
        b.box(Matrix.Translation((x, 0.22, 0.44)) @ Matrix.Rotation(math.radians(-12), 4, "X"), (0.08, 0.08, 0.9), "railing")
        b.box(Matrix.Translation((x, 0.0, 0.41)), (0.08, 0.52, 0.06), "railing")
        b.box(Matrix.Translation((x, -0.05, 0.64)), (0.09, 0.42, 0.05), "railing")
        b.cylinder((x, -0.26, 0.61), 0.05, 0.09, "railing", sides=10)
        b.cylinder((x, -0.26, 0.02), 0.07, 0.04, "railing", sides=10)   # a curled foot
        for i in range(3):
            b.cylinder((x * 0.97, -0.2 + i * 0.16, 0.485), 0.018, 0.02, "metal", sides=6)
    b.finish(col, bevel=0.015, segments=1)
    return col


def lamp(name="park_lamp"):
    """A park lantern: a stepped base, a fluted post with a brass collar, and a four-pane
    lantern with a pyramid cap and a finial."""
    col = kit(name)
    b = K.Buf("lamp")
    b.cylinder((0, 0, 0.12), 0.22, 0.24, "railing", sides=8)
    b.cylinder((0, 0, 0.3), 0.16, 0.14, "railing", sides=8, top_scale=0.7)
    b.cylinder((0, 0, 2.0), 0.065, 3.4, "railing", sides=8)
    b.cylinder((0, 0, 0.55), 0.1, 0.3, "railing", sides=8, top_scale=0.7)
    for z in (1.1, 3.55):
        b.cylinder((0, 0, z), 0.09, 0.08, "brass", sides=10)
    b.cylinder((0, 0, 3.72), 0.1, 0.2, "railing", sides=8, top_scale=1.6)
    b.box(Matrix.Translation((0, 0, 3.86)), (0.34, 0.34, 0.06), "railing")
    b.box(Matrix.Translation((0, 0, 4.12)), (0.26, 0.26, 0.46), "lamp_glass")
    for dx in (-0.14, 0.14):
        for dy in (-0.14, 0.14):
            b.box(Matrix.Translation((dx, dy, 4.12)), (0.04, 0.04, 0.5), "railing")
    b.box(Matrix.Translation((0, 0, 4.38)), (0.38, 0.38, 0.05), "railing")
    b.cylinder((0, 0, 4.52), 0.27, 0.24, "railing", sides=4, top_scale=0.15)
    b.cylinder((0, 0, 4.7), 0.03, 0.16, "brass", sides=6)
    b.finish(col, bevel=0.01, segments=1)
    return col


def fence(name="park_fence", length=12.5):
    """The reference's park fence: cream stone piers on a low stone kerb with dark iron
    railings and spear tips between. 0.85 m at the piers, under the 1.0 m clutter rule, so it
    never becomes a platform. It stands just outside the invisible walls."""
    col = kit(name)
    b = K.Buf("fence")
    b.box(Matrix.Translation((0, 0, 0.12)), (length, 0.3, 0.24), "stone")
    b.box(Matrix.Translation((0, 0, 0.255)), (length + 0.04, 0.36, 0.05), "stone_shade")
    n = max(2, round(length / 3.0))
    piers = [-length / 2 + i * length / n for i in range(n + 1)]
    for x in piers:
        b.box(Matrix.Translation((x, 0, 0.42)), (0.34, 0.34, 0.8), "stone")
        b.box(Matrix.Translation((x, 0, 0.83)), (0.42, 0.42, 0.06), "stone_shade")
        b.cylinder((x, 0, 0.88), 0.1, 0.06, "stone_shade", sides=8, top_scale=0.4)
    for x0, x1 in zip(piers, piers[1:]):
        a, c = x0 + 0.17, x1 - 0.17
        for z in (0.36, 0.7):
            b.box(Matrix.Translation(((a + c) / 2, 0, z)), (c - a + 0.04, 0.035, 0.04), "railing")
        x = a + 0.1
        while x < c - 0.05:
            b.box(Matrix.Translation((x, 0, 0.53)), (0.022, 0.022, 0.4), "railing")
            b.cylinder((x, 0, 0.765), 0.022, 0.07, "railing", sides=4, top_scale=0.1)
            x += 0.14
    b.finish(col, bevel=0.008, segments=1)
    return col


def traffic_signal(name="traffic_signal", reach=4.6):
    """A pole on the kerb, an arm over the road (+X in the model), a signal head facing -Y.
    Heads carry yellow-edged backplates and a visor over each lamp; the pole has a footing,
    a collar, a street-name blade and a push-button box."""
    col = kit(name)
    b = K.Buf("signal")
    b.cylinder((0, 0, 0.12), 0.26, 0.24, "concrete", sides=12)
    b.cylinder((0, 0, 0.3), 0.18, 0.18, "pole", sides=12, top_scale=0.75)
    b.cylinder((0, 0, 2.7), 0.12, 5.4, "pole", sides=12)
    b.cylinder((0, 0, 1.0), 0.14, 0.06, "lane_yellow", sides=12)
    b.box(Matrix.Translation((reach / 2, 0, 5.2)), (reach, 0.12, 0.12), "pole")
    b.box(Matrix.Translation((reach * 0.3, 0, 4.85)) @ Matrix.Rotation(math.radians(-35), 4, "Y"), (1.3, 0.06, 0.06), "pole")
    b.box(Matrix.Translation((0.55, 0, 5.62)), (1.1, 0.03, 0.26), "sign_green")
    b.box(Matrix.Translation((0, -0.16, 1.15)), (0.14, 0.12, 0.2), "lane_yellow")
    for x, z in ((reach - 0.2, 4.5), (0.0, 2.6)):
        b.box(Matrix.Translation((x, 0.02, z)), (0.62, 0.05, 1.42), "lane_yellow")      # the backplate
        b.box(Matrix.Translation((x, 0.0, z)), (0.56, 0.08, 1.36), "railing")
        b.box(Matrix.Translation((x, -0.1, z)), (0.42, 0.36, 1.2), "railing")
        for k, m in enumerate(("signal_red", "signal_amber", "signal_green")):
            zz = z + 0.38 - k * 0.38
            # The lens is a disc FACING THE ROAD (-Y), sunk 1 cm into the housing's face. It
            # was a vertical-axis cylinder, i.e. a pancake lying flat, and read as a half-disc
            # cut by its visor (owner's screenshot, review v8).
            limb(b, (x, -0.27, zz), (x, -0.31, zz), 0.12, 0.12, m, sides=16)
            # A hood over the top half of the lens, sticking out 18 cm, and its two cheeks.
            b.box(Matrix.Translation((x, -0.37, zz + 0.13)), (0.3, 0.2, 0.03), "railing")
            for sx in (-1, 1):
                b.box(Matrix.Translation((x + sx * 0.14, -0.37, zz + 0.06)), (0.025, 0.2, 0.15), "railing")
    b.finish(col, bevel=0.012, segments=1)
    return col


def power_pole(name="power_pole", kind="plain"):
    """A timber pole with a cross arm, braces and insulators. `kind`: plain, transformer (a
    drum and a cage), lamp (a curved street-light arm)."""
    col = kit(name)
    b = K.Buf("pole")
    b.cylinder((0, 0, 4.5), 0.16, 9.0, "timber_pole", sides=10, top_scale=0.8)
    b.cylinder((0, 0, 0.3), 0.19, 0.6, "timber_pole", sides=10)
    b.box(Matrix.Translation((0, 0, 8.4)), (0.14, 2.4, 0.14), "timber_pole")
    b.box(Matrix.Translation((0, 0, 7.7)), (0.12, 1.6, 0.12), "timber_pole")
    for side in (-1, 1):
        b.box(Matrix.Translation((0, side * 0.45, 8.05)) @ Matrix.Rotation(side * math.radians(40), 4, "X"), (0.06, 0.06, 0.9), "metal")
    for y in (-1.0, -0.35, 0.35, 1.0):
        b.cylinder((0, y, 8.55), 0.05, 0.16, "white", sides=8)
        b.cylinder((0, y, 8.6), 0.07, 0.03, "white", sides=8)
    b.box(Matrix.Translation((0, 0, 2.2)), (0.34, 0.03, 0.16), "lane_yellow")   # a number plate
    if kind == "transformer":
        b.cylinder((0.35, 0, 6.9), 0.32, 0.9, "concrete", sides=14)
        b.cylinder((0.35, 0, 7.4), 0.34, 0.1, "metal", sides=14)
        for z in (6.7, 7.1):
            b.cylinder((0.35, 0, z), 0.335, 0.04, "metal", sides=14)
        b.box(Matrix.Translation((0.16, 0, 6.9)), (0.1, 0.5, 0.1), "metal")
    elif kind == "lamp":
        for i in range(6):
            a0, a1 = i / 6 * math.pi / 2, (i + 1) / 6 * math.pi / 2
            p = Vector((math.sin(a0) * 1.6, 0, 6.2 + math.cos(a0) * 0.0 + math.sin(a0) * 0.6))
            q = Vector((math.sin(a1) * 1.6, 0, 6.2 + math.sin(a1) * 0.6))
            limb(b, p, q, 0.045, 0.045, "metal", sides=6)
        b.box(Matrix.Translation((1.75, 0, 6.72)), (0.6, 0.26, 0.12), "metal")
        b.box(Matrix.Translation((1.75, 0, 6.65)), (0.5, 0.2, 0.04), "lamp_glass")
    b.finish(col, bevel=0.012, segments=1)
    return col


def street_bin(name="street_bin"):
    """A painted litter bin: a banded body, a domed lid with a dark mouth, a small plate."""
    col = kit(name)
    b = K.Buf("bin")
    b.cylinder((0, 0, 0.05), 0.33, 0.1, "railing", sides=16)
    b.cylinder((0, 0, 0.45), 0.3, 0.75, "bin_green", sides=16)
    for z in (0.22, 0.72):
        b.cylinder((0, 0, z), 0.31, 0.05, "railing", sides=16)
    b.cylinder((0, 0, 0.87), 0.33, 0.08, "railing", sides=16)
    b.cylinder((0, 0, 0.98), 0.31, 0.16, "bin_green", sides=16, top_scale=0.55)
    b.box(Matrix.Translation((0, -0.26, 0.9)), (0.3, 0.1, 0.07), "frame_dark")
    b.box(Matrix.Translation((0, -0.29, 0.47)), (0.22, 0.03, 0.14), "white")
    b.finish(col, bevel=0.015, segments=1)
    return col


# ------------------------------------------------------------------ downtown skyscrapers
#
# Owner, 2026-09-24, on review v3: "theres not even any glass buildings/skyscrapers like the
# references given". Tiny Talisman's city is dominated by a downtown of 20 to 40 storey
# curtain-wall towers: glass that reflects the sky, a mullion grid, setbacks, and a different
# CROWN on every tower (glass pyramids with spires, slanted tops, stepped rings, fins, a
# helipad), one round tower and one with exposed steel bracing. These replace the grey
# skyline boxes. Every tower is its own model: no two share massing, glass and crown.

def poly_rect(w, d):
    return [(-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2)]


def poly_chamfer(w, d, c):
    x, y = w / 2, d / 2
    return [(-x + c, -y), (x - c, -y), (x, -y + c), (x, y - c), (x - c, y), (-x + c, y), (-x, y - c), (-x, -y + c)]


def poly_ngon(r, n):
    return [(r * math.cos(math.pi / n + i * math.tau / n), r * math.sin(math.pi / n + i * math.tau / n)) for i in range(n)]


def poly_offset(poly, d):
    """Offset a convex counter-clockwise polygon outward by d (inward if negative), mitred."""
    n, out, norms = len(poly), [], []
    for i in range(n):
        (ax, ay), (bx, by) = poly[i], poly[(i + 1) % n]
        L = math.hypot(bx - ax, by - ay)
        norms.append(((by - ay) / L, -(bx - ax) / L))
    for i in range(n):
        n1, n2 = norms[i - 1], norms[i]
        k = 1 + n1[0] * n2[0] + n1[1] * n2[1]
        out.append((poly[i][0] + (n1[0] + n2[0]) / k * d, poly[i][1] + (n1[1] + n2[1]) / k * d))
    return out


def ring(poly, z):
    return [Vector((x, y, z)) for x, y in poly]


def BAND(h, o=0.24):
    return [(-0.12, -h / 2), (o, -h / 2), (o + 0.04, -h / 2 + 0.04), (o + 0.04, h / 2 - 0.04), (o, h / 2), (-0.12, h / 2)]


COPE = [(-0.35, -0.55), (0.28, -0.55), (0.34, -0.2), (0.34, 0.25), (0.26, 0.32), (-0.35, 0.32)]


def brace(buf, p, q, t, mat):
    d = q - p
    m = Matrix.Translation((p + q) / 2) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4()
    buf.box(m, (t, t, d.length), mat)


def skyscraper(name, t):
    """One downtown tower from a spec dict:
      plan      rect (w, d) | chamfer (w, d, c) | ngon (r, sides)
      storeys   section heights in storeys; each section after the first is set back
      setback   metres each section steps in
      glass     curtain_teal | curtain_green | curtain_dark
      mullion   (material, spacing m); band = (every n storeys, height, material)
      lattice   exposed steel X bracing every n storeys, or absent
      crown     pyramid | slant | stepped | fins | helipad | mech (+ spire)
    Construction: a stone podium, then per section a glass prism, a proud mullion grid, swept
    spandrel bands and a coping, all overlapping by a few cm so no two faces share a plane."""
    col = kit(name)
    ST = t.get("storey", 3.6)
    kind, *dims = t["plan"]
    base = {"rect": poly_rect, "chamfer": poly_chamfer, "ngon": poly_ngon}[kind](*dims)
    glass, trim, frames, props = K.Buf("curtain"), K.Buf("tower trim"), K.Buf("mullions"), K.Buf("tower props")
    gm = t["glass"]
    # The podium: two tall storeys of stone with a shop band and a canopy on every face.
    pod = poly_offset(base, t.get("podium_out", 2.0))
    PH = 9.0
    trim.prism(pod, 0.0, PH, t.get("podium", "stone_blocks"))
    cen = (sum(p[0] for p in pod) / len(pod), sum(p[1] for p in pod) / len(pod))
    cv = Vector((*cen, 0))
    for i in range(len(pod)):
        f = K.Facade(pod[i], pod[(i + 1) % len(pod)], cen)
        if f.length < 2.5:
            continue
        props.box(f.frame(f.length / 2, 2.4, 0.0), (f.length - 1.6, 0.3, 3.4), "glass")
        props.box(f.frame(f.length / 2, 4.55, 0.7), (f.length - 1.0, 1.5, 0.2), t.get("canopy", "metal_dark"))
        m = max(2, round(f.length / 4.5))
        for k in range(1, m):
            frames.box(f.frame(k * f.length / m, 2.4, 0.06), (0.22, 0.3, 3.6), "metal_dark")
    trim.sweep(ring(pod, PH), COPE, "concrete", cv, closed=True)
    trim.prism(poly_offset(pod, -0.25), PH - 0.1, PH + 0.12, "roof")
    # The sections.
    poly, z = base, PH - 0.1
    mm, spacing = t.get("mullion", ("mullion_light", 1.5))
    every, bh, bmat = t.get("band", (1, 0.35, "mullion_light"))
    # Owner, review v15: "make the window panes less dense. and also thicken the frames".
    # Panes are twice as wide as the spec's spacing (never under 2.6 m), mullions about twice
    # as thick, and every spandrel band at least 0.6 m, so a tower reads as a few big panes
    # per floor like the street's windows, not a fine grid.
    spacing = max(2.6, spacing * 2.0)
    bh = max(0.6, bh * 1.6)
    for si, n in enumerate(t["storeys"]):
        if si:
            poly = poly_offset(poly, -t.get("setback", 2.5))
        z0, z1 = z, z + n * ST
        glass.prism(poly, z0, z1, gm)
        fcs = [K.Facade(poly[i], poly[(i + 1) % len(poly)], cen) for i in range(len(poly))]
        for f in fcs:
            m = max(1, round(f.length / spacing))
            for k in range(m):
                frames.box(f.frame(k * f.length / m, (z0 + z1) / 2 + 0.05, 0.05), (0.3, 0.36, z1 - z0 - 0.1), mm)
        for s in range(0, n, every):
            if s or si == 0:
                trim.sweep(ring(poly, z0 + s * ST + 0.05), BAND(bh), bmat, cv, closed=True)
        if t.get("lattice"):
            L = t["lattice"]
            for f in fcs:
                for s in range(0, n - L + 1, L):
                    za, zb = z0 + s * ST + 0.3, z0 + (s + L) * ST - 0.3
                    brace(frames, f.at(0.2, za, 0.32), f.at(f.length - 0.2, zb, 0.32), 0.34, "steel")
                    brace(frames, f.at(f.length - 0.2, za, 0.32), f.at(0.2, zb, 0.32), 0.34, "steel")
                    frames.box(f.frame(f.length / 2, zb + 0.3, 0.32), (f.length, 0.34, 0.34), "steel")
                frames.box(f.frame(0.0, (z0 + z1) / 2, 0.3), (0.5, 0.45, z1 - z0), "steel")
        trim.sweep(ring(poly, z1), COPE, t.get("cope", "concrete"), cv, closed=True)
        trim.prism(poly_offset(poly, -0.25), z1 - 0.1, z1 + 0.12, "roof")
        z = z1 + 0.05
    T = z + 0.07
    crown = t["crown"]
    top = poly_offset(poly, -0.3)
    xs, ys = [p[0] for p in top], [p[1] for p in top]
    span = min(max(xs) - min(xs), max(ys) - min(ys))
    crown_h = t.get("crown_h", span * 0.9)
    if crown == "pyramid":
        apex = Vector((*cen, T + crown_h))
        bot = ring(top, T - 0.2)
        for i in range(len(top)):
            glass.face([bot[i], bot[(i + 1) % len(top)], apex], gm)
            brace(frames, bot[i], apex, 0.2, mm)
        glass.face(list(reversed(bot)), gm)
    elif crown == "slant":
        h = t.get("crown_h", span * 0.6)
        lo, hi = min(xs), max(xs)
        bot = ring(top, T - 0.2)
        topv = [Vector((x, y, T - 0.2 + 0.3 + h * (x - lo) / (hi - lo))) for x, y in top]
        glass.face(list(reversed(bot)), gm)
        glass.face(topv, gm)
        for i in range(len(top)):
            j = (i + 1) % len(top)
            glass.face([bot[i], bot[j], topv[j], topv[i]], gm)
            brace(frames, topv[i], topv[j], 0.25, mm)
    elif crown == "stepped":
        p, zz = top, T - 0.1
        for k in range(3):
            p = poly_offset(p, -span * 0.1)
            trim.prism(p, zz, zz + 3.2, t.get("step_mat", "concrete"))
            trim.sweep(ring(p, zz + 3.2), COPE, "mullion_light", cv, closed=True)
            zz += 3.25
        T = zz
    elif crown == "fins":
        for x, y in top:
            v = Vector((x, y, 0)) - cv
            m = Matrix.Translation((x, y, T + 3.5)) @ v.to_track_quat("Y", "Z").to_matrix().to_4x4()
            frames.box(m, (0.3, 2.4, 9.0), mm)
    elif crown == "helipad":
        r = span * 0.34
        props.box(Matrix.Translation((cen[0], cen[1], T + 1.2)), (span * 0.5, span * 0.5, 2.6), "concrete")
        props.cylinder((cen[0], cen[1], T + 2.6), r, 0.3, "asphalt", sides=32)
        for k in range(20):
            a = k * math.tau / 20
            props.box(Matrix.Translation((cen[0] + math.cos(a) * r * 0.82, cen[1] + math.sin(a) * r * 0.82, T + 2.76))
                      @ Matrix.Rotation(a, 4, "Z"), (0.3, r * 0.26, 0.04), "lane_yellow")
        for dx in (-1, 1):
            props.box(Matrix.Translation((cen[0] + dx * r * 0.22, cen[1], T + 2.76)), (r * 0.1, r * 0.6, 0.04), "road_paint")
        props.box(Matrix.Translation((cen[0], cen[1], T + 2.76)), (r * 0.44, r * 0.1, 0.04), "road_paint")
    if crown == "mech":
        props.box(Matrix.Translation((cen[0] + span * 0.12, cen[1], T + 1.6)), (span * 0.45, span * 0.35, 3.3), "concrete")
        props.box(Matrix.Translation((cen[0] + span * 0.12, cen[1], T + 3.3)), (span * 0.5, span * 0.4, 0.2), "metal_dark")
        for k in range(3):
            props.cylinder((cen[0] - span * 0.25, cen[1] - span * 0.2 + k * 1.6, T + 0.8), 0.55, 1.5, "metal", sides=14)
    if t.get("spire"):
        top_z = T + (crown_h if crown == "pyramid" else 0.0) - 0.3
        tall = t["spire"]
        props.cylinder((cen[0], cen[1], top_z + tall / 2), 0.35, tall, "metal", sides=10, top_scale=0.25)
        props.cylinder((cen[0], cen[1], top_z + tall + 0.2), 0.3, 0.4, "signal_red", sides=10)
    glass.finish(col, bevel=0)
    trim.finish(col, bevel=0.03)
    frames.finish(col, bevel=0.012, segments=1)
    props.finish(col, bevel=0.02, segments=1)
    return col


# Ten towers, all different. Heights are storeys per section (3.6 m each), the first section
# standing on the 9 m podium.
TOWERS = {
    "tower_chamfer_spire": dict(plan=("chamfer", 24, 24, 5), storeys=[20, 8], setback=2.5, glass="curtain_teal",
                                mullion=("mullion_light", 1.5), band=(1, 0.3, "mullion_light"), crown="pyramid",
                                spire=12),
    "tower_slant_dark": dict(plan=("rect", 22, 17), storeys=[34], glass="curtain_dark", mullion=("metal_dark", 1.2),
                             band=(2, 0.4, "metal_dark"), crown="slant"),
    "tower_round_rings": dict(plan=("ngon", 11, 24), storeys=[18, 6], setback=1.6, glass="curtain_green",
                              mullion=("mullion_light", 1.4), band=(1, 0.55, "concrete"), crown="stepped", spire=6),
    "tower_lattice": dict(plan=("rect", 20, 20), storeys=[27], glass="curtain_teal", mullion=("metal_dark", 1.25),
                          band=(3, 0.3, "metal_dark"), lattice=6, crown="mech", spire=14),
    "tower_stepped_stone": dict(plan=("rect", 28, 20), storeys=[12, 8, 6, 4], setback=2.4, glass="curtain_dark",
                                mullion=("mullion_light", 1.6), band=(1, 0.9, "stone"), cope="stone",
                                podium="stone", crown="stepped", step_mat="stone", spire=8),
    "tower_octagon_fins": dict(plan=("ngon", 12, 8), storeys=[30], glass="curtain_teal", mullion=("mullion_light", 1.3),
                               band=(3, 0.5, "mullion_light"), crown="fins"),
    "tower_slim_pyramid": dict(plan=("rect", 15, 15), storeys=[38], glass="curtain_green", mullion=("mullion_light", 1.0),
                               band=(1, 0.25, "mullion_light"), crown="pyramid", crown_h=13, spire=9),
    "tower_helipad": dict(plan=("chamfer", 26, 20, 3), storeys=[24], glass="curtain_dark",
                          mullion=("mullion_light", 1.7), band=(1, 0.45, "stone"), crown="helipad"),
    "tower_round_spire": dict(plan=("ngon", 8, 20), storeys=[16, 12, 6], setback=1.4, glass="curtain_teal",
                              mullion=("metal_dark", 1.2), band=(2, 0.35, "metal_dark"), crown="mech", spire=18),
    "tower_slant_green": dict(plan=("chamfer", 20, 24, 2), storeys=[16, 10], setback=3.0, glass="curtain_green",
                              mullion=("metal_dark", 1.4), band=(2, 0.35, "concrete"), crown="slant", crown_h=12),
}


def tower_half(name):
    """Half the podium's widest extent, for spacing the downtown."""
    kind, *dims = TOWERS[name]["plan"]
    r = dims[0] if kind == "ngon" else math.hypot(dims[0], dims[1]) / 2
    return r + 2.0


def tower_height(name):
    t = TOWERS[name]
    return 9.0 + sum(t["storeys"]) * t.get("storey", 3.6)


AERIAL = ((-70.0, -78.0, 62.0), (4.0, 4.0, 4.0))   # the saved aerial cameras: where, and what they look at
AERIAL_CITY = ((-150.0, -40.0, 120.0), (20.0, 10.0, 10.0))


def place_towers():
    """The downtown: every tower twice, never next to its twin, around the city beyond the
    street blocks, with nothing tall on the sun's line through the court.
    Keep-outs: the four street arms (|cross| < 44 out to 118 m), the corner lots (48 m), the
    ground's edge (160 m), and each other. SHADOW RULE: Unity's sun is Euler(44, 140), which
    is Blender azimuth -50 degrees (south-east) at 44 degrees up, so a tower within 25 degrees
    of that bearing must be far enough that its shadow (1.04 x height) stops short of the court."""
    rng = random.Random(90)
    names = list(TOWERS) * 2
    rng.shuffle(names)
    sun_az = math.atan2(-0.551, 0.462)
    placed = []
    for name in names:
        half, h = tower_half(name), tower_height(name)
        for _ in range(4000):
            a = rng.uniform(-math.pi, math.pi)
            d = rng.uniform(62, 150)
            x, y = d * math.cos(a), d * math.sin(a)
            if max(abs(x), abs(y)) + half > 157:
                continue
            if max(abs(x), abs(y)) - half < 50:
                continue
            ok = True
            for arm in range(4):
                c, s = math.cos(-arm * math.pi / 2), math.sin(-arm * math.pi / 2)
                ax, ay = x * c - y * s, x * s + y * c
                if abs(ax) < 44 + half and ay - half < 160 and ay > 0:
                    ok = False
            dang = abs((a - sun_az + math.pi) % math.tau - math.pi)
            if dang < math.radians(25) and d - half < 1.04 * h + 14:
                ok = False
            for (px, py, pname) in placed:
                gap = math.hypot(x - px, y - py) - half - tower_half(pname)
                if gap < 8 or (pname == name and gap < 70):
                    ok = False
            # Keep the saved aerial camera's sight line clear (review v4 put it inside a tower).
            for (cx, cy, cz), (tx, ty, tz) in (AERIAL, AERIAL_CITY):
                sx, sy = tx - cx, ty - cy
                k = max(0.0, min(1.0, ((x - cx) * sx + (y - cy) * sy) / (sx * sx + sy * sy)))
                if math.hypot(x - cx - k * sx, y - cy - k * sy) < half + 6 and cz + k * (tz - cz) < h + 15:
                    ok = False
            if ok:
                placed.append((x, y, name))
                place(name, (x, y, B.WALK - 0.02), rng.choice((0.0, math.pi / 2, math.pi, -math.pi / 2)))
                break
        else:
            print(f"[kanto-city] WARNING: no room for {name}")
    return placed


# ------------------------------------------------------------------ ground, markings, wires

def ground():
    """THE GROUND AS NON-OVERLAPPING CELLS. The plan is cut on every edge line (kerbs, park
    edge, court, paths) into rectangles; each rectangle is exactly one surface at its own
    height, so nothing is ever stacked on anything else."""
    col = kit("ground")
    g = K.Buf("ground")
    g.keep_winding = True   # every face below is wound by hand; see Buf.finish
    E = 160.0
    inner, outer = B.ROAD - B.ROAD_HALF, B.ROAD + B.ROAD_HALF
    lines = sorted({-E, -outer, -inner, -B.PARK, -B.COURT, -1.5, 1.5, B.COURT, B.PARK, inner, outer, E})
    W = B.WALK

    def kind(x, y):
        ax, ay = abs(x), abs(y)
        if ax < B.PARK and ay < B.PARK:
            if ax < B.COURT and ay < B.COURT:
                return "court", W + 0.03
            if ax < 1.5 or ay < 1.5:
                return "paving", W + 0.01
            return "grass", W + 0.02
        if inner < ax < outer or inner < ay < outer:
            return "asphalt", 0.0
        return "paving", W

    cells = {}
    for i in range(len(lines) - 1):
        for j in range(len(lines) - 1):
            x0, x1, y0, y1 = lines[i], lines[i + 1], lines[j], lines[j + 1]
            cells[(i, j)] = (kind((x0 + x1) / 2, (y0 + y1) / 2), (x0, x1, y0, y1))
    for (i, j), ((mat, h), (x0, x1, y0, y1)) in cells.items():
        g.face([Vector((x0, y0, h)), Vector((x1, y0, h)), Vector((x1, y1, h)), Vector((x0, y1, h))], mat)
        # Vertical faces only where this cell is higher than its neighbour: kerbs, never
        # two faces in one plane.
        for (di, dj), edge in (((1, 0), ((x1, y0), (x1, y1))), ((-1, 0), ((x0, y1), (x0, y0))),
                               ((0, 1), ((x1, y1), (x0, y1))), ((0, -1), ((x0, y0), (x1, y0)))):
            nb = cells.get((i + di, j + dj))
            lower = nb[0][1] if nb else -0.3
            if h > lower + 1e-4:
                (ax, ay), (bx, by) = edge
                side_mat = "kerb" if mat in ("paving",) and lower <= 0.0 else mat
                g.face([Vector((ax, ay, lower)), Vector((bx, by, lower)), Vector((bx, by, h)), Vector((ax, ay, h))], side_mat)
    # Markings, 6 mm proud of the asphalt they mark.
    m = K.Buf("markings")
    z = 0.006
    for sx, sy in B.QUADS:
        a = inner + 0.8
        while a < outer - 0.6:
            m.box(Matrix.Translation((sx * (inner - 1.8), sy * a, z)), (3.2, 0.55, 0.01), "road_paint")
            m.box(Matrix.Translation((sx * a, sy * (inner - 1.8), z)), (0.55, 3.2, 0.01), "road_paint")
            a += 1.1
    for c in (-B.ROAD, B.ROAD):
        d = -150.0
        while d < 150:
            if not (inner - 1 < abs(d) < outer + 1):
                m.box(Matrix.Translation((c, d, z)), (0.2, 2.4, 0.01), "lane_yellow")
                m.box(Matrix.Translation((d, c, z)), (2.4, 0.2, 0.01), "lane_yellow")
            d += 4.5
    g.finish(col, bevel=0)
    m.finish(col, bevel=0)
    return col


def wires(poles):
    """Sagging bundles between consecutive power poles along each far sidewalk: three wires
    with different sag, each a thin swept tube."""
    col = kit("wires")
    b = K.Buf("wires")
    prof = [(math.cos(a) * 0.022, math.sin(a) * 0.022) for a in [i * math.tau / 6 for i in range(6)]]
    # SPANS ACROSS THE STREET: every other pole along an outward street sends a sagging pair
    # over the road to a bracket on the building opposite (its face is 14 m off the centre).
    for line in poles:
        along_y = abs(line[0].x - line[-1].x) < 1e-3
        for i, p in enumerate(line):
            d = p.y if along_y else p.x
            if i % 2 or abs(d) < 50:
                continue
            c = p.x if along_y else p.y
            far = (Vector((math.copysign(14.3, c), p.y, 0)) if along_y else Vector((p.x, math.copysign(14.3, c), 0)))
            for k, (off, h_end, sag) in enumerate(((-0.35, 7.4, 0.9), (0.35, 7.1, 1.1))):
                a = p + Vector((0, off, 0) if along_y else (off, 0, 0)) + UP * 8.55
                bq = far + Vector((0, off, 0) if along_y else (off, 0, 0)) + UP * h_end
                pts = [a.lerp(bq, t) - UP * sag * 4 * t * (1 - t) for t in [i2 / 14 for i2 in range(15)]]
                for u, v in zip(pts, pts[1:]):
                    dd = v - u
                    mtx = Matrix.Translation((u + v) / 2) @ dd.to_track_quat("Z", "Y").to_matrix().to_4x4()
                    b.box(mtx, (0.04, 0.04, dd.length + 0.01), "wire")
                b.box(Matrix.Translation(bq + Vector((math.copysign(-0.15, c), 0, 0) if along_y else (0, math.copysign(-0.15, c), 0))),
                      (0.3, 0.3, 0.12), "metal_dark")   # the bracket on the wall
    for line in poles:
        for p, q in zip(line, line[1:]):
            for k, (off, sag) in enumerate(((-0.9, 0.55), (0.0, 0.8), (0.9, 0.65))):
                side = (q - p).normalized().cross(UP) * off
                a, bq = p + side + UP * 8.55, q + side + UP * 8.55
                pts = [a.lerp(bq, t) - UP * sag * 4 * t * (1 - t) for t in [i / 12 for i in range(13)]]
                # A wire has no inside; sweep's 'inside' just orients the tube.
                for u, v in zip(pts, pts[1:]):
                    d = v - u
                    mtx = Matrix.Translation((u + v) / 2) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4()
                    b.box(mtx, (0.04, 0.04, d.length + 0.01), "wire")
    b.finish(col, bevel=0)
    return col


# ------------------------------------------------------------------ assembly and export

PLACE = []   # (model, blender location, rotation about Z in radians, uniform scale)


def place(model, at, rot=0.0, scale=1.0):
    PLACE.append((model, tuple(round(v, 4) for v in at), round(rot, 6), round(scale, 4)))


def facing(model, width, front_center, direction):
    """Place a model whose front is along +Y from x=0..width, so its front centre lands at
    `front_center` facing `direction` (a unit axis vector in the plan)."""
    theta = math.atan2(direction[1], direction[0]) - math.pi / 2
    r = Matrix.Rotation(theta, 3, "Z")
    origin = Vector((front_center[0], front_center[1], 0)) - r @ Vector((width / 2, 0, 0))
    # 2 cm into the pavement, so a building's underside never shares the pavement's plane.
    place(model, (origin.x, origin.y, B.WALK - 0.02), theta)


def layout():
    F, LOT = B.FACADE, B.LOT
    Z = B.WALK - 0.02
    place("brick_corner", (F, F, Z), math.pi / 2)          # NE
    place("deco_corner", (-F, F, Z), math.pi)              # NW
    place("glass_tower", (F, -F, Z), 0.0)                  # SE
    place("townhouse_row", (-F, -F, Z), 0.0)               # SW
    inner = B.ROAD - B.ROAD_HALF - B.SIDE
    k = 0
    for arm in range(4):
        r = Matrix.Rotation(arm * math.pi / 2, 3, "Z")
        u = -inner
        for i in range(3):
            name, _kind, w, *_ = PARKSIDE[k % len(PARKSIDE)]
            front = r @ Vector((u + w / 2, F, 0))
            facing(name, w, (front.x, front.y), tuple(r @ Vector((0, -1, 0)))[:2])
            u += w + 0.3
            k += 1
    # Street blocks beyond the corner lots, both sides of every outward road, facing it.
    for arm in range(4):
        r = Matrix.Rotation(arm * math.pi / 2, 3, "Z")
        for c in (-B.ROAD, B.ROAD):
            for side in (-1, 1):
                d = F + LOT + 0.5
                while d < 131:   # was 105: the street walls stopped and the street ran into the void
                    name, _kind, w, *_ = FILLERS[k % len(FILLERS)]
                    x = c + side * (B.ROAD_HALF + B.SIDE)
                    front = r @ Vector((x, d + w / 2, 0))
                    facing(name, w, (front.x, front.y), tuple(r @ Vector((-side, 0, 0)))[:2])
                    d += w + 0.3
                    k += 1
    for arm, row in enumerate(TERMINI):
        r = Matrix.Rotation(arm * math.pi / 2, 3, "Z")
        front = r @ Vector((0, 144.0, 0))
        facing(row[0], row[2], (front.x, front.y), tuple(r @ Vector((0, -1, 0)))[:2])
    # The park.
    lawn = B.WALK + 0.01   # 1 cm into the lawn (its top is WALK + 0.02)
    vary = random.Random(77)
    for q, (sx, sy) in enumerate(B.QUADS):
        # A different tree on every corner, turned and sized its own way (owner, review v3).
        place(PARK_TREES[q], (sx * 11.6, sy * 11.6, lawn), vary.uniform(-math.pi, math.pi), vary.uniform(0.95, 1.1))
        place("hedge_bed", (sx * 11.6, sy * 6.0, lawn), math.pi / 2)
        place("hedge_bed", (sx * 6.0, sy * 11.6, lawn), 0.0)
        # The bench model's back is on +Y; turn it so the back faces AWAY from the court.
        place("park_bench", (sx * (B.COURT + 0.8), sy * 3.0, lawn), -sx * math.pi / 2)
        place("park_bench", (sx * 3.0, sy * (B.COURT + 0.8), lawn), 0.0 if sy > 0 else math.pi)
        place("park_lamp", (sx * (B.PARK - 1.0), sy * 3.4, lawn), 0.0)
        place("park_lamp", (sx * 3.4, sy * (B.PARK - 1.0), lawn), 0.0)
    for arm in range(4):
        rr = arm * math.pi / 2
        r = Matrix.Rotation(rr, 3, "Z")
        for side in (-1, 1):
            p = r @ Vector((side * (1.5 + (B.PARK - 1.5) / 2), B.PARK - 0.3, 0))
            place("park_fence", (p.x, p.y, B.WALK + 0.01), rr)
    # Streets: signals on every park corner, bins, street trees and power poles.
    inner_k, outer_k = B.ROAD - B.ROAD_HALF, B.ROAD + B.ROAD_HALF
    for arm, (sx, sy) in enumerate(B.QUADS):
        # The model's arm reaches along +X: point it out over the ring road on this side.
        place("traffic_signal", (sx * (inner_k - 0.6), sy * (inner_k - 0.6), B.WALK - 0.01), 0.0 if sx > 0 else math.pi)
        place("street_bin", (sx * (inner_k - 1.2), sy * 5.5, B.WALK), 0.0)
        for d in (6.0, 12.0):
            for at in ((sx * d, sy * (outer_k + 1.5), B.WALK - 0.01), (sx * (outer_k + 1.5), sy * d, B.WALK - 0.01)):
                place(vary.choice(STREET_TREES), at, vary.uniform(-math.pi, math.pi), vary.uniform(0.85, 1.15))
    poles = []
    for c in (-(outer_k + 0.8), outer_k + 0.8):
        for along_x in (True, False):
            line = []
            d = -104.0
            while d <= 104:
                if not (inner_k - 2 < abs(d) < outer_k + 2):
                    at = Vector((d, c, 0)) if along_x else Vector((c, d, 0))
                    kind = vary.choice(("power_pole", "power_pole", "power_pole_transformer", "power_pole_lamp"))
                    place(kind, (at.x, at.y, B.WALK - 0.01), (0.0 if along_x else math.pi / 2) + vary.uniform(-0.05, 0.05))
                    line.append(at)
                else:
                    if len(line) > 1:
                        poles.append(line)
                    line = []
                d += 16
            if len(line) > 1:
                poles.append(line)
    place_towers()
    place("ground", (0, 0, 0), 0.0)
    place("wires", (0, 0, 0), 0.0)
    return poles


BUILDERS = {
    "brick_corner": K.brick_corner,
    "deco_corner": deco_corner,
    "glass_tower": glass_tower,
    "townhouse_row": townhouse_row,
    "hedge_bed": hedge,
    "park_bench": bench,
    "park_lamp": lamp,
    "park_fence": fence,
    "traffic_signal": traffic_signal,
    "power_pole": power_pole,
    "power_pole_transformer": lambda: power_pole("power_pole_transformer", "transformer"),
    "power_pole_lamp": lambda: power_pole("power_pole_lamp", "lamp"),
    "street_bin": street_bin,
    "ground": ground,
}
for _name in TOWERS:
    BUILDERS[_name] = (lambda n=_name: skyscraper(n, TOWERS[n]))
for _name, _t in TREES.items():
    BUILDERS[_name] = (lambda n=_name, t=_t: tree(n, **t))
for _row in PARKSIDE:
    BUILDERS[_row[0]] = (lambda r=_row: archetype(r[0], r[1], r[2], r[3], r[4], r[5], True, r[6],
                                                  PARKSIDE_ROOF.get(r[0], "flat")))
for _row in TERMINI:
    BUILDERS[_row[0]] = (lambda r=_row: archetype(r[0], r[1], r[2], r[3], r[4], r[5], False, r[6], r[7]))
for _row in FILLERS:
    BUILDERS[_row[0]] = (lambda r=_row: archetype(r[0], r[1], r[2], r[3], r[4], r[5], False, r[6], r[7]))


def export_glb(col, name):
    """One model, modifiers applied, material NAMES only (Unity supplies the materials)."""
    MODELS_OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    objs = [o for o in col.all_objects if o.type == "MESH"]
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    # Materials are exported as plain colour factors (no images), which keeps their NAMES in
    # the file for KantoSceneBuilder to swap for the shared textured materials.
    bpy.ops.export_scene.gltf(filepath=str(MODELS_OUT / f"{name}.glb"), export_format="GLB", use_selection=True,
                              export_apply=True, export_materials="EXPORT", export_yup=True)


def build_model(name, export=True):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    K.USE_TEXTURES = False
    col = BUILDERS[name]()
    if export:
        export_glb(col, name)
    return col


def preview_model(name, version):
    """Textured Eevee turn of one model: a street-level 3/4 view and a higher one, framed off
    its bounding box, lit like the brick corner's previews. Saves <name>.blend beside it."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    K.USE_TEXTURES = True
    col = BUILDERS[name]()
    K.setup_lighting()
    bpy.ops.wm.save_as_mainfile(filepath=str(K.SOURCE / f"{name}.blend"), compress=True)
    backup = K.SOURCE / f"{name}.blend1"
    if backup.exists():
        backup.unlink()
    pts = [o.matrix_world @ Vector(c) for o in col.all_objects if o.type == "MESH" for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    c, size = (lo + hi) / 2, (hi - lo).length
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    gm = bpy.data.meshes.new("ground")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=size * 2, matrix=Matrix.Translation((c.x, c.y, lo.z - 0.02)))
    bm.to_mesh(gm)
    bm.free()
    gm.materials.append(K.material("ground"))
    scene.collection.objects.link(bpy.data.objects.new("ground", gm))
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    # Street models face +Y (or -X and +Y for corner lots): look from the front-left.
    for label, direction, height, lens in (("street", Vector((-0.55, 1.0, 0)), 0.18, 30), ("high", Vector((-0.8, 1.0, 0)), 0.9, 35)):
        d = direction.normalized() * size * (1.05 if label == "street" else 1.2)
        cam.location = c + d + Vector((0, 0, size * height - (hi.z - lo.z) * 0.3))
        cam.data.lens = lens
        cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(K.PREVIEWS / f"{name}_{label}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[kanto-city] preview", scene.render.filepath)


# ------------------------------------------------------------------ the whole map in Blender

CITY_BLEND = K.SOURCE / "kanto_city.blend"
BUILDING_KINDS = {"brick_corner", "deco_corner", "glass_tower", "townhouse_row", *TOWERS, *(r[0] for r in TERMINI),
                  *(r[0] for r in PARKSIDE), *(r[0] for r in FILLERS)}
PARK_KINDS = {*PARK_TREES, "hedge_bed", "park_bench", "park_lamp", "park_fence"}
# Unity's fog is linear from 90 m to 360 m (KantoSceneBuilder). The Blender review fades the
# same way through the compositor's mist pass, capped so the hills stay as faint silhouettes.
MIST_START, MIST_DEPTH, MIST_CAP = 110.0, 450.0, 0.7
# KantoSceneBuilder's sky and fog, sRGB there, linear here: zenith 5c94db, horizon c7ddf0,
# fog c7dbeb. A flat sky read grey under AgX (review v1), so the gradient is carried over too.
SRGB = lambda c: tuple(round(((v + 0.055) / 1.055) ** 2.4, 4) for v in c)  # noqa: E731
ZENITH, HORIZON, FOG = SRGB((0.36, 0.58, 0.86)), SRGB((0.78, 0.87, 0.94)), SRGB((0.78, 0.86, 0.92))
SKY_STRENGTH = 1.0
# The game's eye: 1.25 m above the court, 95 degrees horizontal (16.5 mm on 36 mm film).
EYE_Z = B.WALK + 0.03 + 1.25
EYE_LENS = 18 / math.tan(math.radians(95 / 2))


def city_lighting():
    """The saved sun and sky, matched to KantoSceneBuilder's clear cool morning: the sun is
    Unity's Euler(44, 140, 0) brought through the (-x, -z, y) axis swap, so it sits in the
    south-east and falls over the viewer's shoulder when facing the brick corner."""
    K.setup_lighting()
    scene = bpy.context.scene
    nodes, links = scene.world.node_tree.nodes, scene.world.node_tree.links
    bg = nodes["Background"]
    bg.inputs["Strength"].default_value = SKY_STRENGTH
    # Horizon to zenith by the height of the view direction, like Unity's gradient skybox.
    coord, split, ramp = nodes.new("ShaderNodeTexCoord"), nodes.new("ShaderNodeSeparateXYZ"), nodes.new("ShaderNodeValToRGB")
    links.new(coord.outputs["Generated"], split.inputs[0])
    links.new(split.outputs["Z"], ramp.inputs["Fac"])
    ramp.color_ramp.elements[0].position, ramp.color_ramp.elements[0].color = 0.0, (*HORIZON, 1)
    ramp.color_ramp.elements[1].position, ramp.color_ramp.elements[1].color = 0.45, (*ZENITH, 1)
    links.new(ramp.outputs["Color"], bg.inputs["Color"])
    sun = bpy.data.objects["sun"]
    pitch, yaw = math.radians(44), math.radians(140)
    fwd = Vector((math.sin(yaw) * math.cos(pitch), -math.sin(pitch), math.cos(yaw) * math.cos(pitch)))
    travel = Vector((-fwd.x, -fwd.z, fwd.y))
    sun.rotation_euler = travel.to_track_quat("-Z", "Y").to_euler()
    sun.data.color, sun.data.energy = (1.0, 0.96, 0.9), 4.2
    scene.world.mist_settings.start = MIST_START
    scene.world.mist_settings.depth = MIST_DEPTH
    scene.world.mist_settings.falloff = "LINEAR"
    for screen in bpy.data.screens:
        for area in screen.areas:
            for space in area.spaces:
                if space.type == "VIEW_3D":
                    space.clip_start, space.clip_end = 0.1, 2000


def city_fog(scene):
    """Fade into Unity's fog colour by distance, in the compositor, PEAK's layered depth. The
    sky is excluded by the alpha of a transparent film and laid back underneath, so only
    geometry fogs."""
    scene.view_layers[0].use_pass_mist = True
    scene.view_layers[0].use_pass_environment = True
    if hasattr(scene, "compositing_node_group"):          # Blender 5
        tree = bpy.data.node_groups.new("kanto fog", "CompositorNodeTree")
        scene.compositing_node_group = tree
        tree.interface.new_socket("Image", in_out="OUTPUT", socket_type="NodeSocketColor")
        out = tree.nodes.new("NodeGroupOutput")
    else:
        scene.use_nodes = True
        tree = scene.node_tree
        tree.nodes.clear()
        out = tree.nodes.new("CompositorNodeComposite")
    rl = tree.nodes.new("CompositorNodeRLayers")
    cap = tree.nodes.new("ShaderNodeMath")
    cap.operation, cap.inputs[1].default_value = "MINIMUM", MIST_CAP
    L = tree.links.new

    def mix(blend, fac, a, b):
        n = tree.nodes.new("ShaderNodeMix")
        n.data_type, n.blend_type = "RGBA", blend
        for i, v in ((0, fac), (6, a), (7, b)):
            if isinstance(v, bpy.types.NodeSocket):
                L(v, n.inputs[i])
            else:
                n.inputs[i].default_value = v
        return n.outputs[2]

    # The film is transparent while rendering (see review), and Image is premultiplied, so:
    # geometry = Image * (1 - f) + FOG * alpha * f, and the sky (the Environment pass) fills
    # the rest: + Env * (1 - alpha). Nothing but geometry fogs.
    L(rl.outputs["Mist"], cap.inputs[0])
    fog_premul = mix("MULTIPLY", 1.0, (*FOG, 1), rl.outputs["Alpha"])
    fogged = mix("MIX", cap.outputs[0], rl.outputs["Image"], fog_premul)
    clear = tree.nodes.new("ShaderNodeMath")
    clear.operation, clear.inputs[0].default_value = "SUBTRACT", 1.0
    L(rl.outputs["Alpha"], clear.inputs[1])
    env = next(o for o in rl.outputs if o.name in ("Env", "Environment"))
    sky = mix("MULTIPLY", 1.0, env, clear.outputs[0])
    L(mix("ADD", 1.0, fogged, sky), out.inputs[0])


def chalk(col):
    """The court chalk and throwing lines, as KantoSceneBuilder draws them: 6 mm over the court
    (whose top is WALK + 0.03), in a plain bright chalk rather than the painted road paint."""
    m = bpy.data.materials.new("court_chalk")
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.98, 0.98, 0.94, 1)
    bsdf.inputs["Roughness"].default_value = 0.9
    b = K.Buf("chalk")
    z = B.WALK + 0.03 + 0.006 - 0.004
    r = B.BOX
    for s in (-1, 1):
        b.box(Matrix.Translation((s * r, 0, z)), (0.14, 2 * r + 0.14, 0.012), "chalk")
        b.box(Matrix.Translation((0, s * r, z)), (2 * r - 0.14, 0.14, 0.012), "chalk")
        # Unity puts the throwing lines at +/- z, which is Blender -/+ y: the same pair.
        b.box(Matrix.Translation((0, s * B.THROW, z)), (10, 0.07, 0.012), "chalk")
    b.bm.to_mesh(me := bpy.data.meshes.new("chalk"))
    b.bm.free()
    me.materials.append(m)
    col.objects.link(bpy.data.objects.new("chalk", me))


def city_cameras(scene, col):
    """Saved in the file, so the owner can look through each one (Numpad 0 on the active)."""
    cams = {}

    def cam(name, at, look, lens, kind="PERSP"):
        c = bpy.data.objects.new(name, bpy.data.cameras.new(name))
        c.data.type, c.data.lens, c.data.clip_start, c.data.clip_end = kind, lens, 0.05, 2000
        c.data.sensor_fit = "HORIZONTAL"
        c.location = at
        c.rotation_euler = (Vector(look) - Vector(at)).to_track_quat("-Z", "Y").to_euler()
        col.objects.link(c)
        cams[name] = c
        return c

    # From the attacker spawn ring on the opposite side, looking across the court at each side.
    for label, d in (("north", (0, 1)), ("east", (1, 0)), ("south", (0, -1)), ("west", (-1, 0))):
        at = (-d[0] * B.SPAWN, -d[1] * B.SPAWN, EYE_Z)
        cam(f"eye_{label}", at, (d[0] * 45, d[1] * 45, 5.0), EYE_LENS)
    cam("aerial", *AERIAL, 28)
    # A second, higher aerial that takes in the downtown ring as well as the park.
    cam("aerial_city", *AERIAL_CITY, 24)
    # Close-ups at a person's eye, for judging props: the owner's own review shot was the NE
    # park corner (tree, fence, crossing, signal, brick corner), and a street toward downtown.
    cam("detail_ne_corner", (7.0, 6.2, 1.9), (14.5, 14.5, 3.0), 26)
    cam("detail_street", (-24.8, 46.0, 1.7), (-22.0, 120.0, 12.0), 20)
    # The NE corner's traffic signal from the kerb (the owner's own review shot, v8).
    cam("detail_signal", (13.0, 12.4, 2.3), (16.4, 16.4, 3.4), 30)
    # A tiled roof up close (the owner judged the first tiles from about this distance).
    rose = next((o for o in scene.objects if o.name.startswith("shophouse_rose_3.")), None)
    if rose:
        mw = rose.matrix_world
        cam("detail_roof", mw @ Vector((-1.5, 6.0, 19.0)), mw @ Vector((4.75, -6.0, 14.5)), 28)
    scene.camera = cams["eye_north"]
    return cams


def assemble():
    """ArtSource/kanto/kanto_city.blend: every model built ONCE, textured, into its own
    collection under 'Kit' (excluded from the view layer), and placed as COLLECTION INSTANCES
    from the same PLACE list the Unity layout JSON is written from. So what is reviewed here
    is exactly what the export places. The ground, markings and wires are unique pieces and
    are linked in directly; chalk, the horizon ring, sun, sky, fog and review cameras too."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    K.USE_TEXTURES = True
    PLACE.clear()
    poles = layout()
    scene = bpy.context.scene
    root = scene.collection

    def child(name, parent=root):
        c = bpy.data.collections.new(name)
        parent.children.link(c)
        return c

    kit_root = child("Kit")
    kits = {}
    for name in sorted({p[0] for p in PLACE} - {"ground", "wires"}):
        col = BUILDERS[name]()
        root.children.unlink(col)
        kit_root.children.link(col)
        kits[name] = col
        print(f"[kanto-city] kit {name}: {sum(len(o.data.polygons) for o in col.all_objects if o.type == 'MESH')} faces")
    city = child("City")
    groups = {g: child(g, city) for g in ("Buildings", "Park", "Street")}
    placed = 0
    for i, (name, at, rot, sc) in enumerate(PLACE):
        if name in ("ground", "wires"):
            continue
        g = "Buildings" if name in BUILDING_KINDS else "Park" if name in PARK_KINDS else "Street"
        o = bpy.data.objects.new(f"{name}.{i:03d}", None)
        o.instance_type, o.instance_collection = "COLLECTION", kits[name]
        o.location, o.rotation_euler, o.scale = at, (0, 0, rot), (sc, sc, sc)
        o.empty_display_size = 0.5
        groups[g].objects.link(o)
        placed += 1
    ground_col = ground()
    root.children.unlink(ground_col)
    city.children.link(ground_col)
    chalk(ground_col)
    wire_col = wires(poles)
    root.children.unlink(wire_col)
    city.children.link(wire_col)
    horizon_col = child("Horizon (Blender only so far)")
    B.horizon(horizon_col)
    horizon_col.children[0].name = "Hills"
    for o in [o for o in horizon_col.all_objects if o.name.startswith("skyline")]:
        bpy.data.objects.remove(o)
    city_lighting()
    city_fog(scene)
    city_cameras(scene, child("Review cameras"))
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    bpy.context.view_layer.layer_collection.children["Kit"].exclude = True
    K.SOURCE.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(CITY_BLEND), compress=True)
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_as_mainfile(filepath=str(CITY_BLEND), compress=True)
    backup = CITY_BLEND.with_suffix(".blend1")
    if backup.exists():
        backup.unlink()
    print(f"[kanto-city] assembled {placed} instances of {len(kits)} models + ground, chalk, wires -> {CITY_BLEND}")


def review(version, only=None):
    """Render every saved review camera in the open file, versioned (chat clients cache images
    by filename)."""
    K.PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    # Transparent only while rendering: the fog compositor needs the sky's alpha, and a saved
    # transparent film would show no sky in the owner's Rendered viewport.
    scene.render.film_transparent = True
    # RGB, or the PNG keeps the transparent film's alpha: the sky saved as see-through and
    # edge pixels, un-premultiplied by a tiny alpha, drew as white outlines (review v2).
    scene.render.image_settings.color_mode = "RGB"
    for cam in sorted((o for o in scene.objects if o.type == "CAMERA"), key=lambda o: o.name):
        if only and cam.name not in only:
            continue
        scene.camera = cam
        scene.render.filepath = str(K.PREVIEWS / f"kanto_city_{cam.name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[kanto-city] review", scene.render.filepath)
    scene.render.film_transparent = False


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if "--assemble" in argv or "--review" in argv:
        # blender -b --python tools/author_kanto_city.py -- --assemble [--review N]
        # blender -b ArtSource/kanto/kanto_city.blend --python tools/author_kanto_city.py -- --review N
        #   (the second renders the file as saved, including any hand edits in it)
        if "--assemble" in argv:
            assemble()
        if "--review" in argv:
            # --cams a,b renders just those saved cameras (one change per round needs only the
            # views that show it).
            only = argv[argv.index("--cams") + 1].split(",") if "--cams" in argv else None
            review(int(argv[argv.index("--review") + 1]), only)
        return
    if "--preview-model" in argv:
        version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 1
        for name in argv[argv.index("--preview-model") + 1].split(","):
            preview_model(name, version)
        return
    only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
    export = "--no-export" not in argv
    poles = layout()
    names = sorted({p[0] for p in PLACE} - {"wires"})
    stats = {}
    for name in names:
        if only and name not in only:
            continue
        col = build_model(name, export)
        stats[name] = sum(len(o.data.polygons) for o in col.all_objects if o.type == "MESH")
        print(f"[kanto-city] {name}: {stats[name]} faces")
    if not only or "wires" in only:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        col = wires(poles)
        if export:
            export_glb(col, "wires")
    LAYOUT_OUT.parent.mkdir(parents=True, exist_ok=True)
    data = {
        "note": "Written by tools/author_kanto_city.py. Positions are UNITY axes: (x, y, z) = (-bx, bz, -by), glTFast's conversion; yaw is degrees about Unity Y.",
        "gameplay": {"box": B.BOX, "throw": B.THROW, "spawn": B.SPAWN, "half": B.HALF, "walk": B.WALK},
        # A LIST, not a map: Unity's JsonUtility cannot read a dictionary.
        "materials": [dict(name=k, texture=v.get("texture") or "", tint=v["tint"], tiling=v["tiling"],
                           foliage=bool(v.get("foliage")), emissive=bool(v.get("emissive")), glossy=bool(v.get("glossy")))
                      for k, v in material_specs().items()],
        "placements": [{"model": m, "position": [-p[0], p[2], -p[1]], "yaw": round(-math.degrees(r), 4), "scale": sc}
                       for m, p, r, sc in PLACE],
    }
    LAYOUT_OUT.write_text(json.dumps(data, indent=1))
    print(f"[kanto-city] {len(PLACE)} placements, {len(names)} models -> {LAYOUT_OUT}")


if __name__ == "__main__":
    main()
