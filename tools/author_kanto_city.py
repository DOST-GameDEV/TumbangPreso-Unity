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
}
K.PALETTE.update(EXTRA_PALETTE)
# Which painted texture each material wears in Blender previews (Unity reads MATERIAL_SPECS).
K.TEXTURED.update({"asphalt": "asphalt", "paving": "paving", "court": "court", "grass": "grass",
                   "brick_brown": "brick_brown", "panel": "panel"})
for name in EXTRA_PALETTE:
    if name.startswith(("plaster_", "stucco_")):
        K.TEXTURED.setdefault(name, "plaster")
K.TEX_SCALE.update({"plaster": 0.5, "asphalt": 0.4, "grass": 0.5})
K.ANTI_TILE.update({"plaster", "asphalt", "grass"})

# The same, for Unity: texture, tint (sRGB-ish multiplier, 1 = texture as painted), tiling.
TINTED = lambda rgb: [round(min(1.0, c * 1.2) ** (1 / 2.2), 4) for c in rgb]  # noqa: E731


def material_specs():
    specs = {}
    for name, rgb in K.PALETTE.items():
        tex = K.TEXTURED.get(name, "paint")
        if name in ("leaf_light", "leaf_dark"):
            specs[name] = {"texture": "leaf", "tint": TINTED(rgb), "tiling": 1.0, "foliage": True}
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


# ------------------------------------------------------------------ the generic building

class Ctx:
    """Everything a building's extras need: its buffers, facades and measurements."""


def city_building(name, foot, order, ground, storey, upper, zones, *, seed=1, bay=3.3,
                  win_margin=1.05, win_h=None, recess=0.3, trim="stone", frame="frame",
                  courses="stone", cornice=K.CORNICE, cornice_mat="stone", parapet="brick",
                  shops=True, door=None, awnings=None, keystones=True, sills=True,
                  planters=True, acs=True, wrap=False, shop_frame="frame", extras=None,
                  window_style="classic", fascia="trim", slabs=None):
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

    K.wall_shell("shell", facades, TOP, ground, zones, "roof").finish(col, bevel=0.035)
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


def archetype(name, kind, w, storeys, colour, seed, detail=True, shop="paint_green"):
    """The street roster: six kinds of building, each a different construction, not a repaint.
      loft      brick, big dark-framed grid windows, painted shop base, water tank
      loft_ph   the loft with a glazed penthouse set back on the roof
      stucco    stucco, ribbon windows, cantilevered bays, a penthouse
      glassmid  concrete panels, ribbon glazing, projecting floor slabs
      panel     concrete panels, grid windows in cream frames, balconies
      shophouse painted plaster, classic windows and balconies (the first walk-up)
    `detail=False` drops planters, AC units and roof clutter for the far street blocks."""
    D = 14.0
    foot = [(0, 0), (w, 0), (w, -D), (0, -D)]
    common = dict(seed=seed, planters=detail, acs=detail, keystones=False)

    def extras(c):
        if kind in ("loft", "loft_ph", "shophouse") and detail:
            water_tank(c.props, w * 0.66, -D * 0.62, c.TOP, r=1.0)
        if kind == "loft_ph":
            penthouse(c, 1.6, 1, "brick_brown" if colour == "brick" else "concrete")
        if kind == "stucco":
            n = max(1, round(w / 3.3))
            bays(c, [k for k in range(n) if k % 2 == 1] or [0], 0, max(0, c.upper - 2), colour)
            penthouse(c, 1.8, 1, colour)
        if kind in ("panel", "shophouse"):
            walkup_balconies(c)

    if kind in ("loft", "loft_ph"):
        return city_building(name, foot, [0], 4.4, 3.3, storeys, (shop, colour), window_style="grid",
                             frame="frame_dark", shop_frame="frame_dark", fascia=shop, courses="stone",
                             cornice=K.COURSE if kind == "loft_ph" else K.CORNICE, parapet=colour, recess=0.22,
                             awnings={0: [["awning_a", "awning_b"], ["awning_c", "awning_b"]][seed % 2]} if detail else None,
                             extras=extras, **common)
    if kind == "stucco":
        return city_building(name, foot, [0], 4.2, 3.2, storeys, ("stone_blocks", colour), window_style="ribbon",
                             frame="metal_dark", shop_frame="metal_dark", fascia=shop, courses=None,
                             slabs="concrete", cornice=None, parapet=colour, recess=0.18, extras=extras, **common)
    if kind == "glassmid":
        return city_building(name, foot, [0], 4.4, 3.4, storeys, ("panel", "panel"), window_style="ribbon",
                             frame="metal_dark", shop_frame="metal_dark", fascia="metal_dark", courses=None,
                             slabs="panel", cornice=None, parapet="panel", recess=0.2, extras=extras, **common)
    if kind == "panel":
        return city_building(name, foot, [0], 4.2, 3.1, storeys, (shop, "panel"), window_style="grid",
                             frame="frame", shop_frame="frame", fascia=shop, courses="concrete",
                             cornice=K.COURSE, parapet="panel", recess=0.2, extras=extras, **common)
    return city_building(name, foot, [0], 4.2, 3.2, storeys, ("stone_blocks", colour), bay=3.1, recess=0.26,
                         parapet=colour, courses="stone", fascia=shop,
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
    ("glassmid_7", "glassmid", 9.5, 7, "panel", 24, "metal_dark"),
    ("loft_brown_ph_4", "loft_ph", 9.0, 4, "brick_brown", 25, "paint_teal"),
    ("panel_5", "panel", 9.5, 5, "panel", 26, "paint_red"),
    ("shophouse_rose_3", "shophouse", 9.5, 3, "plaster_rose", 27, "paint_green"),
    ("stucco_olive_5", "stucco", 9.0, 5, "stucco_olive", 28, "paint_red"),
    ("loft_red_ph_6", "loft_ph", 9.5, 6, "brick", 29, "paint_ochre"),
    ("shophouse_butter_4", "shophouse", 9.5, 4, "plaster_butter", 30, "paint_teal"),
    ("loft_brown_3", "loft", 9.0, 3, "brick_brown", 31, "paint_green"),
    ("glassmid_5", "glassmid", 9.5, 5, "panel", 32, "metal_dark"),
]
# The far street blocks: the same six kinds, lower detail, other widths and heights.
FILLERS = [
    ("far_loft_red_4", "loft", 9.0, 4, "brick", 41, "paint_green"),
    ("far_stucco_tan_7", "stucco", 11.0, 7, "stucco_tan", 42, "paint_ochre"),
    ("far_glassmid_9", "glassmid", 11.0, 9, "panel", 43, "metal_dark"),
    ("far_shophouse_sage_3", "shophouse", 8.0, 3, "plaster_sage", 44, "paint_red"),
    ("far_loft_brown_ph_5", "loft_ph", 9.0, 5, "brick_brown", 45, "paint_teal"),
    ("far_panel_6", "panel", 11.0, 6, "panel", 46, "paint_green"),
    ("far_shophouse_sky_4", "shophouse", 7.5, 4, "plaster_sky", 47, "paint_ochre"),
    ("far_stucco_olive_4", "stucco", 9.0, 4, "stucco_olive", 48, "paint_red"),
]
# ------------------------------------------------------------------ park and street pieces

def kit(name):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def park_tree(name="park_tree", scale=1.0, seed=41):
    """A park tree in the house leaf style: a trunk that forks into three limbs, each carrying
    a big shingled clump, plus a crown clump. Leaves are larger than the window-box shrubs so
    the tree reads at 20 m."""
    col = kit(name)
    rng = random.Random(seed)
    wood, leaves = K.Buf("trunk"), K.Buf("foliage", foliage=True)
    s = scale
    # A tapered trunk. (sweep() cannot run straight up: it orients its profile from the path's
    # horizontal direction, and a vertical path has none, which left the first tree trunkless.)
    wood.cylinder((0, 0, 1.25 * s), 0.24 * s, 2.7 * s, "trunk", sides=10, top_scale=0.7)
    clumps = [((0, 0, 4.6 * s), 1.7 * s)]
    for i in range(3):
        a = i * math.tau / 3 + 0.4
        tip = Vector((math.cos(a) * 1.4 * s, math.sin(a) * 1.4 * s, 3.8 * s))
        # A limb: a tapered box from the fork to its clump.
        mid = (Vector((0, 0, 2.5 * s)) + tip) / 2
        d = (tip - Vector((0, 0, 2.5 * s)))
        m = Matrix.Translation(mid) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4()
        wood.box(m, (0.22 * s, 0.22 * s, d.length), "trunk")
        clumps.append((tuple(tip + Vector((0, 0, 0.5 * s))), 1.3 * s))
    for c, r in clumps:
        K.foliage(leaves, c, (r, r, r * 0.85), int(520 * r * r), 0.34 * s, rng, lift=0.3, spread=0.45)
    wood.finish(col, bevel=0.04)
    leaves.finish(col)
    return col


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
    col = kit(name)
    b = K.Buf("bench")
    for i in range(3):
        b.box(Matrix.Translation((0, -0.2 + i * 0.16, 0.45)), (1.9, 0.12, 0.06), "wood")
    for i in range(2):
        b.box(Matrix.Translation((0, 0.26, 0.62 + i * 0.16)) @ Matrix.Rotation(math.radians(-12), 4, "X"), (1.9, 0.05, 0.11), "wood")
    for x in (-0.78, 0.78):
        b.box(Matrix.Translation((x, 0, 0.22)), (0.07, 0.5, 0.44), "railing")
        b.box(Matrix.Translation((x, 0.25, 0.62)), (0.07, 0.06, 0.4), "railing")
        b.box(Matrix.Translation((x, 0.0, 0.62)), (0.07, 0.5, 0.05), "railing")
    b.finish(col, bevel=0.015, segments=1)
    return col


def lamp(name="park_lamp"):
    col = kit(name)
    b = K.Buf("lamp")
    b.cylinder((0, 0, 0.2), 0.16, 0.4, "railing", sides=12)
    b.cylinder((0, 0, 2.1), 0.06, 3.6, "railing", sides=10)
    b.cylinder((0, 0, 3.95), 0.2, 0.1, "railing", sides=12)
    b.cylinder((0, 0, 4.2), 0.17, 0.4, "lamp_glass", sides=12, top_scale=1.2)
    b.cylinder((0, 0, 4.45), 0.24, 0.1, "railing", sides=12, top_scale=0.3)
    b.finish(col, bevel=0.01, segments=1)
    return col


def fence(name="park_fence", length=12.5):
    """A low painted railing, 0.8 m: under the 1.0 m clutter rule, so it never becomes a
    platform. It stands just outside the invisible walls."""
    col = kit(name)
    b = K.Buf("fence")
    n = int(length / 1.5)
    for i in range(n + 1):
        x = -length / 2 + i * length / n
        b.box(Matrix.Translation((x, 0, 0.4)), (0.09, 0.09, 0.8), "railing")
        b.cylinder((x, 0, 0.84), 0.07, 0.08, "railing", sides=8)
    for z in (0.3, 0.7):
        b.box(Matrix.Translation((0, 0, z)), (length, 0.04, 0.05), "railing")
    j = 0
    x = -length / 2 + 0.15
    while x < length / 2 - 0.1:
        b.box(Matrix.Translation((x, 0, 0.5)), (0.025, 0.025, 0.4), "railing")
        x += 0.15
        j += 1
    b.finish(col, bevel=0.008, segments=1)
    return col


def traffic_signal(name="traffic_signal", reach=4.6):
    """A pole on the kerb, an arm over the road (+X in the model), a signal head facing -Y."""
    col = kit(name)
    b = K.Buf("signal")
    b.cylinder((0, 0, 0.15), 0.22, 0.3, "pole", sides=12)
    b.cylinder((0, 0, 2.7), 0.12, 5.4, "pole", sides=12)
    b.box(Matrix.Translation((reach / 2, 0, 5.2)), (reach, 0.12, 0.12), "pole")
    b.box(Matrix.Translation((reach * 0.3, 0, 4.85)) @ Matrix.Rotation(math.radians(-35), 4, "Y"), (1.3, 0.06, 0.06), "pole")
    for x, z in ((reach - 0.2, 4.5), (0.0, 2.6)):
        b.box(Matrix.Translation((x, 0, z)), (0.45, 0.4, 1.25), "railing")
        b.box(Matrix.Translation((x, -0.22, z + 0.55)), (0.5, 0.06, 0.08), "railing")
        for k, m in enumerate(("signal_red", "signal_amber", "signal_green")):
            b.cylinder((x, -0.21, z + 0.38 - k * 0.38), 0.12, 0.04, m, sides=14)
    b.finish(col, bevel=0.012, segments=1)
    return col


def power_pole(name="power_pole"):
    col = kit(name)
    b = K.Buf("pole")
    b.cylinder((0, 0, 4.5), 0.16, 9.0, "timber_pole", sides=10)
    b.box(Matrix.Translation((0, 0, 8.4)), (0.14, 2.4, 0.14), "timber_pole")
    b.box(Matrix.Translation((0, 0, 7.7)), (0.12, 1.6, 0.12), "timber_pole")
    for y in (-1.0, -0.35, 0.35, 1.0):
        b.cylinder((0, y, 8.55), 0.05, 0.16, "white", sides=8)
    b.cylinder((0.35, 0, 6.9), 0.32, 0.9, "concrete", sides=14)
    b.cylinder((0.35, 0, 7.4), 0.34, 0.1, "metal", sides=14)
    b.finish(col, bevel=0.012, segments=1)
    return col


def street_bin(name="street_bin"):
    col = kit(name)
    b = K.Buf("bin")
    b.cylinder((0, 0, 0.45), 0.3, 0.9, "bin_green", sides=16)
    b.cylinder((0, 0, 0.93), 0.33, 0.08, "railing", sides=16)
    b.finish(col, bevel=0.015, segments=1)
    return col


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

PLACE = []   # (model, blender location, rotation about Z in radians)


def place(model, at, rot=0.0):
    PLACE.append((model, tuple(round(v, 4) for v in at), round(rot, 6)))


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
                while d < 105:
                    name, _kind, w, *_ = FILLERS[k % len(FILLERS)]
                    x = c + side * (B.ROAD_HALF + B.SIDE)
                    front = r @ Vector((x, d + w / 2, 0))
                    facing(name, w, (front.x, front.y), tuple(r @ Vector((-side, 0, 0)))[:2])
                    d += w + 0.3
                    k += 1
    # The park.
    lawn = B.WALK + 0.01   # 1 cm into the lawn (its top is WALK + 0.02)
    for sx, sy in B.QUADS:
        place("park_tree", (sx * 11.6, sy * 11.6, lawn), sx * sy * 0.7)
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
            place("street_tree", (sx * d, sy * (outer_k + 1.5), B.WALK), d)
            place("street_tree", (sx * (outer_k + 1.5), sy * d, B.WALK), d * 2)
    poles = []
    for c in (-(outer_k + 0.8), outer_k + 0.8):
        for along_x in (True, False):
            line = []
            d = -104.0
            while d <= 104:
                if not (inner_k - 2 < abs(d) < outer_k + 2):
                    at = Vector((d, c, 0)) if along_x else Vector((c, d, 0))
                    place("power_pole", (at.x, at.y, B.WALK), 0.0 if along_x else math.pi / 2)
                    line.append(at)
                else:
                    if len(line) > 1:
                        poles.append(line)
                    line = []
                d += 16
            if len(line) > 1:
                poles.append(line)
    place("ground", (0, 0, 0), 0.0)
    place("wires", (0, 0, 0), 0.0)
    return poles


BUILDERS = {
    "brick_corner": K.brick_corner,
    "deco_corner": deco_corner,
    "glass_tower": glass_tower,
    "townhouse_row": townhouse_row,
    "park_tree": park_tree,
    "street_tree": lambda: park_tree("street_tree", 0.75, 42),
    "hedge_bed": hedge,
    "park_bench": bench,
    "park_lamp": lamp,
    "park_fence": fence,
    "traffic_signal": traffic_signal,
    "power_pole": power_pole,
    "street_bin": street_bin,
    "ground": ground,
}
for _row in PARKSIDE:
    BUILDERS[_row[0]] = (lambda r=_row: archetype(r[0], r[1], r[2], r[3], r[4], r[5], True, r[6]))
for _row in FILLERS:
    BUILDERS[_row[0]] = (lambda r=_row: archetype(r[0], r[1], r[2], r[3], r[4], r[5], False, r[6]))


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


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
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
        "placements": [{"model": m, "position": [-p[0], p[2], -p[1]], "yaw": round(-math.degrees(r), 4)} for m, p, r in PLACE],
    }
    LAYOUT_OUT.write_text(json.dumps(data, indent=1))
    print(f"[kanto-city] {len(PLACE)} placements, {len(names)} models -> {LAYOUT_OUT}")


if __name__ == "__main__":
    main()
