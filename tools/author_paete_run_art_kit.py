"""PAETE'S RUN, the art's tool kit: the materials table, the mesh buffer and its chunky primitives (2026-10-10).

Imported by tools/author_paete_run_art.py (inside Blender). Nothing here builds a scene by itself.

HOUSE RULES THIS FILE ENFORCES (docs/KANTO_DESIGN_GUIDE.md 2 and 3, docs/LAGOON_REWORK_GUIDE.md 2, the Eskinita lessons):
  * every box keeps its own eight corners (nothing is welded across boxes before the bevel);
  * UVs are METRES, so a material's tiling is 1 / its tile in metres and one texture reads the same size everywhere;
  * each block gets its own UV offset, so two blocks side by side never show the same patch of the drawing;
  * rocks are Lagoon Cove's approved kit stones (tools/author_lagoon_rocks.py), taken from ArtSource/lagoon/
    lagoon_cove.blend WITH their baked edge-wear UVs ("UVBake") and the approved "rock_a+brush" material.
"""
import math
import random
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/TumbangPreso/Art/PaeteRun"
TEXTURES = ART / "Textures"
UP = Vector((0, 0, 1))
DEBUG_AT = None      # (x, y, z): print who builds any block near there

# name: (texture, tint, tiling, flags). Tiling is 1 / the tile in metres. "" is a flat tint.
# flags: f foliage (two-sided leaf card, alpha), c cutout, e emissive, g glossy
MAT = {
    "pr_rock": ("rock_a", (1, 1, 1), 0.25, ""),                       # Unity: TumbangPreso/LagoonRock (triplanar, edge atlas on UV2)
    "pr_ground": ("sand_a", (1, 1, 1), 0.25, ""),                     # Unity: TumbangPreso/LagoonGround (fields in COLOR_0 and UV2)
    "pr_ashlar": ("ashlar", (1.0, 0.84, 0.60), 0.5, ""),
    "pr_ashlar_dark": ("ashlar", (0.84, 0.62, 0.42), 0.5, ""),
    "pr_ashlar_rose": ("ashlar", (1.0, 0.74, 0.58), 0.5, ""),
    "pr_stone": ("stone", (1.0, 0.86, 0.64), 0.4, ""),
    "pr_stone_worn": ("stone", (0.86, 0.68, 0.48), 0.4, ""),
    "pr_stone_wet": ("stone", (0.50, 0.62, 0.56), 0.4, ""),
    "pr_paving": ("paving", (1.0, 0.84, 0.60), 0.4, ""),
    "pr_plaster": ("plaster", (0.98, 0.92, 0.80), 0.4, ""),
    "pr_iron": ("metal", (0.30, 0.24, 0.20), 1.0, ""),
    "pr_bronze": ("metal", (0.66, 0.50, 0.24), 1.0, ""),
    "pr_timber": ("timber", (1.0, 0.96, 0.90), 0.5, ""),
    "pr_plank": ("plank", (1, 1, 1), 0.5, ""),
    "pr_rope": ("rope", (1, 1, 1), 2.0, ""),
    "pr_clay": ("clay", (1, 1, 1), 1.0, ""),
    "pr_cloth_red": ("cloth", (0.80, 0.20, 0.24), 1.0, ""),
    "pr_cloth_yellow": ("cloth", (0.98, 0.80, 0.24), 1.0, ""),
    "pr_cloth_teal": ("cloth", (0.16, 0.58, 0.40), 1.0, ""),
    "pr_cloth_white": ("cloth", (0.98, 0.95, 0.88), 1.0, ""),
    "pr_paint_red": ("paint", (0.78, 0.22, 0.22), 1.0, ""),
    "pr_paint_yellow": ("paint", (0.97, 0.78, 0.22), 1.0, ""),
    "pr_paint_teal": ("paint", (0.14, 0.56, 0.40), 1.0, ""),
    "pr_paint_white": ("paint", (0.96, 0.93, 0.86), 1.0, ""),
    "pr_paint_black": ("paint", (0.14, 0.12, 0.12), 1.0, ""),
    "pr_leaf_light": ("leaf", (0.62, 0.86, 0.34), 1.0, "f"),
    "pr_leaf_dark": ("leaf", (0.34, 0.62, 0.30), 1.0, "f"),
    "pr_grass_light": ("blade", (0.93, 0.86, 0.50), 1.0, "f"),
    "pr_grass_dark": ("blade", (0.72, 0.70, 0.36), 1.0, "f"),
    "pr_vine": ("", (0.30, 0.46, 0.22), 1.0, ""),
    "pr_capiz": ("capiz", (1.0, 0.94, 0.80), 1.0, "e"),
    "pr_water": ("", (0.15, 0.78, 0.76), 1.0, "g"),                   # Unity: TumbangPreso/LagoonCoveWater
    "pr_water_running": ("", (0.22, 0.82, 0.78), 1.0, "g"),            # the sheet on the slides: Unity wears paete_run_water_running.mat on this name
    "pr_far_rock": ("", (0.80, 0.66, 0.50), 1.0, ""),                  # the horizon's stacks: a flat hazed tan
    "pr_tree_leaf_light": ("leaf_kanto", (0.70, 0.86, 0.36), 1.0, "f"),
    "pr_tree_leaf_dark": ("leaf_kanto", (0.42, 0.64, 0.30), 1.0, "f"),
    "pr_tree_trunk": ("bark", (1, 1, 1), 1.0, ""),
    "pr_tree_core": ("", (0.30, 0.48, 0.22), 1.0, ""),
    # ---- the art pass of 2026-10-10 (walked surfaces, the slides, the dressing)
    "pr_pool_floor": ("paving", (0.56, 0.80, 0.70), 0.4, ""),          # the basins' floors, seen through the water
    "pr_stone_stain": ("stone", (0.44, 0.52, 0.42), 0.4, ""),          # where water has run: the slides' streaks and tide lines
    "pr_moss": ("stone", (0.50, 0.66, 0.26), 0.4, ""),                 # moss on the slides' lips and the basins' shaded stones
    "pr_far_ruin": ("", (0.86, 0.74, 0.58), 1.0, ""),                  # the horizon's aqueduct and towers: flat, hazed
    "pr_palm_trunk": ("palm_trunk", (1, 1, 1), 1.0, ""),               # Lagoon Cove's palms, brought through whole
    "pr_frond_light": ("frond_broad", (1.0, 1.0, 0.98), 1.0, "f"),
    "pr_frond_dark": ("frond_broad", (0.79, 0.83, 0.74), 1.0, "f"),
    "pr_frond_small": ("frond", (0.85, 0.95, 0.46), 1.0, "f"),
    "pr_coconut": ("coconut", (1, 1, 1), 1.0, ""),
    "pr_flower_red": ("gumamela", (1.0, 0.42, 0.56), 1.0, "f"),
    "pr_flower_pink": ("bougainvillea", (1.0, 0.42, 0.56), 1.0, "f"),
    "pr_banana_stem": ("banana_stem", (1, 1, 1), 1.0, ""),
    "pr_paddle_light": ("paddle", (0.85, 0.98, 0.51), 1.0, "f"),
    "pr_paddle_dark": ("paddle", (0.51, 0.77, 0.39), 1.0, "f"),
    "pr_slat": ("pg_slat", (1, 1, 1), 1.0, ""),
    "pr_bamboo": ("bamboo", (1, 1, 1), 1.0, ""),
    "pr_thatch": ("thatch", (1, 1, 1), 1.0, ""),
    "pr_sawali": ("sawali", (1, 1, 1), 1.0, ""),
}
SHADER = {"pr_rock": "TumbangPreso/LagoonRock", "pr_ground": "TumbangPreso/LagoonGround", "pr_water": "TumbangPreso/LagoonCoveWater"}


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tint, tiling, flags = MAT[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.25 if "g" in flags else 0.85
    bsdf.inputs["Base Color"].default_value = (*tint, 1)
    m.diffuse_color = (*tint, 1)
    if name.startswith("pr_water"):
        bsdf.inputs["Alpha"].default_value = 0.72
        bsdf.inputs["Roughness"].default_value = 0.12
        if hasattr(m, "surface_render_method"):
            m.surface_render_method = "BLENDED"
        return m
    path = TEXTURES / (tex + "_albedo.png")
    if tex and path.exists():
        img = nodes.new("ShaderNodeTexImage")
        img.image = bpy.data.images.load(str(path), check_existing=True)
        uv, mp = nodes.new("ShaderNodeUVMap"), nodes.new("ShaderNodeMapping")
        uv.uv_map = "UVMap"
        mp.inputs["Scale"].default_value = (tiling, tiling, 1)
        links.new(uv.outputs["UV"], mp.inputs["Vector"]); links.new(mp.outputs["Vector"], img.inputs["Vector"])
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(img.outputs["Color"], mix.inputs[6]); mix.inputs[7].default_value = (*tint, 1)
        links.new(mix.outputs[2], bsdf.inputs["Base Color"])
        if "f" in flags or "c" in flags:
            links.new(img.outputs["Alpha"], bsdf.inputs["Alpha"])
            m.use_backface_culling = False
            if hasattr(m, "surface_render_method"):
                m.surface_render_method = "DITHERED"
        if "e" in flags:
            links.new(mix.outputs[2], bsdf.inputs["Emission Color"]); bsdf.inputs["Emission Strength"].default_value = 0.6
    return m


# ------------------------------------------------------------------ Lagoon's rock kit
ROCK_NAMES = (["rock_boulder_%02d" % i for i in range(1, 6)] + ["rock_stack_%02d" % i for i in range(1, 4)]
              + ["rock_slab_%02d" % i for i in range(1, 4)] + ["rock_split_%02d" % i for i in range(1, 3)]
              + ["rock_cobble_%02d" % i for i in range(1, 4)])
BOULDERS, STACKS, SLABS, SPLITS, COBBLES = range(0, 5), range(5, 8), range(8, 11), range(11, 13), range(13, 16)
ROCKS = []        # dicts: name, mesh, top, bottom, verts, polys, uv, uv2


def load_rock_kit():
    src = ROOT / "ArtSource/lagoon/lagoon_cove.blend"
    with bpy.data.libraries.load(str(src), link=False) as (a, b):
        b.meshes = [n for n in ROCK_NAMES if n in a.meshes]
    by = {me.name: me for me in b.meshes}
    rock = bpy.data.materials.get("rock")
    rock.name = "pr_rock"
    for n in ROCK_NAMES:
        me = by[n]
        uv, uv2 = me.uv_layers["UVMap"].data, me.uv_layers["UVBake"].data
        ROCKS.append(dict(name=n, mesh=me, top=me["rock_top_z"], bottom=me["rock_bottom_z"], verts=[v.co.copy() for v in me.vertices],
                          polys=[tuple(p.vertices) for p in me.polygons], loops=[tuple(p.loop_indices) for p in me.polygons],
                          uv=[tuple(d.uv) for d in uv], uv2=[tuple(d.uv) for d in uv2]))
    return ROCKS


# ------------------------------------------------------------------ flat convex polygons (walked tops are cut from these)
def area2(poly):
    return 0.5 * sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly)))


def ccw(poly):
    return list(poly) if area2(poly) >= 0 else list(reversed(poly))


def clip_half(poly, a, b, left=True):
    """The part of a convex polygon to the left (or right) of the line a -> b."""
    if not poly:
        return []
    ex, ey = b[0] - a[0], b[1] - a[1]
    sg = 1.0 if left else -1.0
    d = [sg * (ex * (p[1] - a[1]) - ey * (p[0] - a[0])) for p in poly]
    out = []
    n = len(poly)
    for i in range(n):
        p, q, dp, dq = poly[i], poly[(i + 1) % n], d[i], d[(i + 1) % n]
        if dp >= 0:
            out.append(p)
        if (dp > 0 and dq < 0) or (dp < 0 and dq > 0):
            t = dp / (dp - dq)
            out.append((p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t))
    return out if len(out) >= 3 and abs(area2(out)) > 1e-5 else []


def intersect(poly, conv):
    """Convex polygon AND convex polygon (both counter-clockwise)."""
    for i in range(len(conv)):
        poly = clip_half(poly, conv[i], conv[(i + 1) % len(conv)], True)
        if not poly:
            return []
    return poly


def subtract(poly, conv):
    """Convex polygon MINUS convex polygon: a list of convex pieces."""
    lo = [min(p[k] for p in poly) for k in (0, 1)]; hi = [max(p[k] for p in poly) for k in (0, 1)]
    if any(min(c[k] for c in conv) > hi[k] or max(c[k] for c in conv) < lo[k] for k in (0, 1)):
        return [poly]
    out, rest = [], poly
    for i in range(len(conv)):
        a, b = conv[i], conv[(i + 1) % len(conv)]
        o = clip_half(rest, a, b, False)
        if o:
            out.append(o)
        rest = clip_half(rest, a, b, True)
        if not rest:
            break
    return out


def inside(poly, x, y, margin=0.0):
    """Is (x, y) inside a counter-clockwise convex polygon, by at least `margin`."""
    n = len(poly)
    for i in range(n):
        a, b = poly[i], poly[(i + 1) % n]
        ex, ey = b[0] - a[0], b[1] - a[1]
        ln = math.hypot(ex, ey) or 1.0
        if (ex * (y - a[1]) - ey * (x - a[0])) / ln < margin:
            return False
    return True


# ------------------------------------------------------------------ frames
def frame(origin, fwd, tilt=0.0, roll=0.0):
    """A matrix whose local x runs along `fwd` (flattened), y to its left, z up; then tilted about local y, rolled about x."""
    f = Vector((fwd[0], fwd[1], 0))
    f = f.normalized() if f.length > 1e-6 else Vector((1, 0, 0))
    left = Vector((-f.y, f.x, 0))
    m = Matrix((f, left, UP)).transposed().to_4x4()
    m.translation = Vector(origin)
    if tilt:
        m = m @ Matrix.Rotation(tilt, 4, "Y")
    if roll:
        m = m @ Matrix.Rotation(roll, 4, "X")
    return m


def T(x, y, z, yaw=0.0):
    m = Matrix.Rotation(yaw, 4, "Z")
    m.translation = Vector((x, y, z))
    return m


class Buf:
    """Faces with a material each, UVs in metres, a second UV layer for the rocks' baked edge wear."""

    def __init__(self, name, seed=1):
        self.name, self.v, self.f, self.uv, self.uv2, self.m, self.mats, self.sm = name, [], [], [], [], [], [], []
        self.rng = random.Random(seed)
        self.has_uv2 = False
        self.xf = Matrix.Identity(4)          # everything added is placed by this (a mover built once at the origin, shown at rest)

    def mat(self, name):
        if name not in self.mats:
            self.mats.append(name)
        return self.mats.index(name)

    def face(self, idx, uvs, mat, smooth=False, uv2=None):
        self.f.append(tuple(idx)); self.uv += list(uvs); self.uv2 += list(uv2) if uv2 else [(0.0, 0.0)] * len(idx)
        self.m.append(self.mat(mat)); self.sm.append(smooth)

    def quad(self, pts, mat, uvs=None, smooth=False):
        """pts counter-clockwise seen from outside, world coordinates."""
        if uvs is None:
            a, b, c = Vector(pts[0]), Vector(pts[1]), Vector(pts[2])
            n = (b - a).cross(c - a)
            ax = max(range(3), key=lambda i: abs(n[i]))
            pick = {0: (1, 2), 1: (0, 2), 2: (0, 1)}[ax]
            uvs = [(p[pick[0]], p[pick[1]]) for p in pts]
        base = len(self.v)
        self.v += [tuple(self.xf @ Vector(p)) for p in pts]
        self.face(range(base, base + len(pts)), uvs, mat, smooth)

    def block(self, M, hx, hy, z0, z1, mat, top=None, taper=1.0, skew=(0.0, 0.0), skip=""):
        """A box centred on M's origin in x and y: half extents hx, hy at z0, times `taper` at z1 (shifted by `skew`).
        ITS EIGHT CORNERS ARE ITS OWN. UVs come from its local coordinates plus an offset of its own."""
        tx, ty = hx * taper, hy * taper
        c = [(-hx, -hy, z0), (hx, -hy, z0), (hx, hy, z0), (-hx, hy, z0),
             (-tx + skew[0], -ty + skew[1], z1), (tx + skew[0], -ty + skew[1], z1), (tx + skew[0], ty + skew[1], z1), (-tx + skew[0], ty + skew[1], z1)]
        base = len(self.v)
        M = self.xf @ M
        self.v += [tuple(M @ Vector(q)) for q in c]
        if DEBUG_AT is not None:
            cz = (M @ Vector((0, 0, (z0 + z1) / 2)))
            if abs(cz.x - DEBUG_AT[0]) < 2.2 and abs(cz.y - DEBUG_AT[1]) < 2.2 and abs(cz.z - DEBUG_AT[2]) < 1.2:
                import traceback
                print("[debug] block at (%.1f, %.1f, %.2f) half %.2f x %.2f, %s, from %s" % (cz.x, cz.y, cz.z, hx, hy, mat, " < ".join(f.name for f in traceback.extract_stack()[-5:-1])))
        ou, ov = self.rng.uniform(0, 7), self.rng.uniform(0, 7)
        for letter, idx, m in (("x", (3, 0, 4, 7), mat), ("X", (1, 2, 6, 5), mat), ("y", (0, 1, 5, 4), mat), ("Y", (2, 3, 7, 6), mat),
                               ("z", (3, 2, 1, 0), mat), ("Z", (4, 5, 6, 7), top or mat)):
            if letter in skip:
                continue
            pick = {"x": (1, 2), "X": (1, 2), "y": (0, 2), "Y": (0, 2), "z": (0, 1), "Z": (0, 1)}[letter]
            self.face([base + i for i in idx], [(c[i][pick[0]] + ou, c[i][pick[1]] + ov) for i in idx], m)

    def flat(self, poly, z, mat, down=False):
        """A level polygon (counter-clockwise from above) at height z, facing up (or down)."""
        pts = [(p[0], p[1], z) for p in (reversed(poly) if down else poly)]
        self.quad(pts, mat, uvs=[(p[0], p[1]) for p in pts])

    def prism(self, poly, z0, z1, mat, top=None, bot=None, cap=True):
        """A counter-clockwise convex polygon stood up from z0 to z1: its own corners, sides in `mat`."""
        n = len(poly)
        ou = self.rng.uniform(0, 7)
        run = 0.0
        for i in range(n):
            a, b = poly[i], poly[(i + 1) % n]
            ln = math.hypot(b[0] - a[0], b[1] - a[1])
            self.quad([(a[0], a[1], z0), (b[0], b[1], z0), (b[0], b[1], z1), (a[0], a[1], z1)], mat,
                      uvs=[(ou + run, z0), (ou + run + ln, z0), (ou + run + ln, z1), (ou + run, z1)])
            run += ln
        if cap:
            self.flat(poly, z1, top or mat)
        if bot:
            self.flat(poly, z0, bot, down=True)

    def box(self, x0, x1, y0, y1, z0, z1, mat, top=None, yaw=0.0):
        self.block(T((x0 + x1) / 2, (y0 + y1) / 2, 0, yaw), abs(x1 - x0) / 2, abs(y1 - y0) / 2, min(z0, z1), max(z0, z1), mat, top)

    def lathe(self, M, prof, n, mat, cap_top=None, cap_bot=None, smooth=True, squash=1.0, phase=0.0):
        """prof: [(radius, z), ...] bottom to top, turned about M's z. Caps are n-gons in `cap_top` / `cap_bot`."""
        base = len(self.v)
        M = self.xf @ M
        ou = self.rng.uniform(0, 5)
        for (r, z) in prof:
            for k in range(n):
                a = math.tau * k / n + phase
                self.v.append(tuple(M @ Vector((r * math.cos(a), r * math.sin(a) * squash, z))))
        for j in range(len(prof) - 1):
            ra = (prof[j][0] + prof[j + 1][0]) * 0.5
            # v runs along the surface, so a flat ring (a capital's underside) is not a smear
            va = sum(math.hypot(prof[i + 1][0] - prof[i][0], prof[i + 1][1] - prof[i][1]) for i in range(j))
            vb = va + math.hypot(prof[j + 1][0] - prof[j][0], prof[j + 1][1] - prof[j][1])
            for k in range(n):
                k2 = (k + 1) % n
                u0, u1 = ou + math.tau * ra * k / n, ou + math.tau * ra * (k + 1) / n
                self.face([base + j * n + k, base + j * n + k2, base + (j + 1) * n + k2, base + (j + 1) * n + k],
                          [(u0, va), (u1, va), (u1, vb), (u0, vb)], mat, smooth)
        for (cap, j, rev) in ((cap_top, len(prof) - 1, False), (cap_bot, 0, True)):
            if not cap:
                continue
            r, z = prof[j]
            b2 = len(self.v)
            ring = [Vector((r * math.cos(math.tau * k / n + phase), r * math.sin(math.tau * k / n + phase) * squash, z)) for k in range(n)]
            self.v += [tuple(M @ q) for q in ring]
            idx = list(range(b2, b2 + n))
            uvs = [(q.x + ou, q.y) for q in ring]
            if rev:
                idx.reverse(); uvs.reverse()
            self.face(idx, uvs, cap)

    def drum(self, M, r, z0, z1, mat, top=None, n=14, r1=None, bot=None):
        self.lathe(M, [(r, z0), (r if r1 is None else r1, z1)], n, mat, cap_top=top or mat, cap_bot=bot)

    def tube(self, pts, r, mat, n=6, closed=False, r1=None, caps=True, smooth=True):
        """A round member along `pts` (world). `r1` tapers the far end."""
        pts = [Vector(p) for p in pts]
        cnt = len(pts)
        base = len(self.v)
        ref = None
        run = 0.0
        for i, p in enumerate(pts):
            a = pts[(i - 1) % cnt] if (closed or i > 0) else p
            c = pts[(i + 1) % cnt] if (closed or i < cnt - 1) else p
            t = (c - a).normalized()
            if ref is None:
                ref = t.cross(UP)
                if ref.length < 0.05:
                    ref = t.cross(Vector((1, 0, 0)))
            ref = (ref - t * ref.dot(t)).normalized()
            side = t.cross(ref)
            rr = r if r1 is None else r + (r1 - r) * i / max(1, cnt - 1)
            if i > 0:
                run += (p - pts[i - 1]).length
            for k in range(n):
                a2 = math.tau * k / n
                self.v.append(tuple(self.xf @ (p + ref * (rr * math.cos(a2)) + side * (rr * math.sin(a2)))))
        segs = cnt if closed else cnt - 1
        for i in range(segs):
            i2 = (i + 1) % cnt
            for k in range(n):
                k2 = (k + 1) % n
                self.face([base + i * n + k, base + i * n + k2, base + i2 * n + k2, base + i2 * n + k],
                          [(k / n, i * 0.5), ((k + 1) / n, i * 0.5), ((k + 1) / n, i * 0.5 + 0.5), (k / n, i * 0.5 + 0.5)], mat, smooth)
        if caps and not closed:
            self.face([base + k for k in reversed(range(n))], [(0, 0)] * n, mat)
            self.face([base + (cnt - 1) * n + k for k in range(n)], [(0, 0)] * n, mat)

    def chain(self, a, b, mat="pr_iron", link=0.62, r=0.075):
        """Chunky links from a to b, each turned a quarter from the last. Links overlap, so the chain is one thing."""
        a, b = Vector(a), Vector(b)
        d = b - a
        ln = d.length
        if ln < 1e-4:
            return
        d /= ln
        p0 = d.cross(UP)
        if p0.length < 0.05:
            p0 = d.cross(Vector((1, 0, 0)))
        p0.normalize()
        p1 = d.cross(p0)
        pitch = link * 0.72
        cnt = max(1, round(ln / pitch))
        pitch = ln / cnt
        L, W = pitch * 0.5 + r * 1.6, link * 0.27
        for i in range(cnt):
            c = a + d * (pitch * (i + 0.5))
            w = p0 if i % 2 == 0 else p1
            ring = []
            for j in range(8):
                th = math.tau * j / 8
                cx = math.cos(th)
                x = (L - W) * (1 if cx > 0.01 else -1 if cx < -0.01 else 0) + W * cx
                ring.append(c + d * x + w * (W * math.sin(th)))
            self.tube(ring, r, mat, n=4, closed=True)

    def rock(self, idx, M, mat="pr_rock"):
        """A kit stone under M, with its baked edge-wear UVs."""
        rk = ROCKS[idx]
        base = len(self.v)
        M = self.xf @ M
        self.v += [tuple(M @ v) for v in rk["verts"]]
        self.has_uv2 = True
        for poly, loops in zip(rk["polys"], rk["loops"]):
            self.face([base + i for i in poly], [rk["uv"][l] for l in loops], mat, True, [rk["uv2"][l] for l in loops])

    def rock_at(self, idx, x, y, z_top, across, yaw=0.0, squash=1.0, flat_top=True):
        """A kit stone `across` metres wide whose TOP is at z_top (so a body can be stood on it at a known height)."""
        s = across / 2.5
        rk = ROCKS[idx]
        m = T(x, y, z_top - rk["top"] * s * squash, yaw) @ Matrix.Diagonal((s, s, s * squash, 1))
        self.rock(idx, m)
        return m

    def card(self, c, right, up, mat, uv=((0, 0), (1, 0), (1, 1), (0, 1))):
        c, right, up = Vector(c), Vector(right), Vector(up)
        self.quad([c - right, c + right, c + right + up, c - right + up], mat, uvs=list(uv))

    def obj(self, bevel=0.0, collection=None, sharp=38.0):
        me = bpy.data.meshes.new(self.name)
        me.from_pydata(self.v, [], self.f)
        layer = me.uv_layers.new(name="UVMap")
        layer.data.foreach_set("uv", [c for uv in self.uv for c in uv])
        if self.has_uv2:
            l2 = me.uv_layers.new(name="UVBake")
            l2.data.foreach_set("uv", [c for uv in self.uv2 for c in uv])
            me.uv_layers.active_index = 0
            me.uv_layers["UVMap"].active_render = True
        for name in self.mats:
            me.materials.append(material(name))
        me.polygons.foreach_set("material_index", self.m)
        me.update()
        o = bpy.data.objects.new(self.name, me)
        (collection or bpy.context.scene.collection).objects.link(o)
        o["rock_scale"] = 2.0
        if bevel > 0:
            mod = o.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments, mod.limit_method, mod.angle_limit = bevel, 1, "ANGLE", math.radians(40)
            mod.harden_normals = True
            me.polygons.foreach_set("use_smooth", [True] * len(me.polygons))
        else:
            me.polygons.foreach_set("use_smooth", self.sm)
            try:
                me.set_sharp_from_angle(angle=math.radians(sharp))
            except Exception:
                pass
        return o


def tris_of(o):
    dg = bpy.context.evaluated_depsgraph_get()
    me = o.evaluated_get(dg).to_mesh()
    n = sum(len(p.vertices) - 2 for p in me.polygons)
    o.evaluated_get(dg).to_mesh_clear()
    return n
