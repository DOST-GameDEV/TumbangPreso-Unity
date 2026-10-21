"""PAYO basin dressing kit (2026-10-19): the pieces that dress the court's ring walls and paved plazas. Blender 5.0.

  blender -b --python tools/payo_pieces/plaza_kit.py -- <ABS out folder>      (plaza_run.sh build does it)

Owner, 2026-10-18/19: "need you to fix up the rest of the barren stuff here" (the paved ring round the stage) and
"bulul statues should be larger and look more grand" (then: "current model for bulul is okay, they just need to be
sized up, no need to remodel"). So the bulul models are NOT touched; these pieces go round them:

  plaza_plinth    a stepped carved plinth for a sized-up bulul: two base steps, a body carrying the walls' own fret
                  course (the meander frieze, painted AND a proud carved band, as court_build.py's build_bands), a
                  woven-band cap, paving on top. 8 m body, 9.8 m base, 3.8 m tall at scale 1; scaled per statue.
  plaza_planter   a long stone planter trough (6 x 1.4 x 0.75 m) the plants stand in along the parapets.
  plaza_vine      a curtain of creeper (2.6 m wide, strands 1.6 to 4.8 m; plaza_vine_long 3.4 m wide, to 10 m) that starts on a coping's top and spills down
                  the wall face: round leaf cards on thin stalks, normals out of the wall so it shades as one soft mass.

The stone pieces are built with court_build.py's own helpers and wear its material `py_court_stone`, UV'd into the
court's trim-sheet atlas (court_layout.py), so in the game they take the court platform's very material (atlas,
normal map, vertex colour gradient). The vine wears the Lagoon kit's leaf materials (`lagoon_leaf_round_*`,
`lagoon_stalk_monstera`), whose pictures the builder loads from Art/Payo/Textures. Blender x east, y north, z up;
a piece's FRONT is -y (glTF +z). Origins: plinth and planter at the centre of their foot; the vine at the coping's
outer top edge, the curtain hanging down its front (-y), its top tuft lying back over the coping (+y).
"""
import math
import os
import random
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import court_build as C  # noqa: E402
import court_layout as L  # noqa: E402

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else os.path.join(C.ROOT, "Logs", "payo", "pieces", "plaza")
TEX = os.path.join(C.ROOT, "Assets", "TumbangPreso", "Art", "Payo", "Textures")
PPM = L.PPM


# ------------------------------------------------------------------------------------------------ stone helpers
def side(mb, a, b, z0, z1, top, s0, colf, plain_rows=False):
    """One vertical face a -> b (outward to the right), z0..z1, painted from the WALL_TOP row as if `top` were the
    walls' top (so its plain course, then the fret course 1.2 m under `top`, then plain courses)."""
    A, B = C.V2(a), C.V2(b)
    ln = (B - A).length
    d = (B - A) / ln
    shift = L.WALL_TOP_Z - top
    corners = [(0.0, z0), (ln, z0), (ln, z1), (0.0, z1)]
    pts = [(A.x + d.x * t, A.y + d.y * t, z) for t, z in corners]
    uvs = [C.wall_uv(s0 + t, z + shift, 0, plain_rows) for t, z in corners]
    mb.face(pts, uvs, [colf(z) for _, z in corners])


def ring_sides(mb, half, z0, z1, top, colf, plain_rows=False):
    ring = [(half, -half), (half, half), (-half, half), (-half, -half)]          # CCW from above
    s = 0.0
    for i in range(4):
        a, b = ring[i], ring[(i + 1) % 4]
        side(mb, a, b, z0, z1, top, s, colf, plain_rows)                          # CCW travel: outward on the right
        s += 2 * half + 3.1                                                        # each side its own stretch of the row


def row_sides(mb, half, z0, z1, row, colf, ring=None):
    """The four sides of a square band (or of `ring`, CCW) painted from a coping profile row (cap_out, the woven band)."""
    y0, m = L.ROWS[row]
    ring = ring or [(half, -half), (half, half), (-half, half), (-half, -half)]
    s = 0.0
    for i in range(4):
        a, b = ring[i], ring[(i + 1) % 4]
        A, B = C.V2(a), C.V2(b)
        ln = (B - A).length
        d = (B - A) / ln
        corners = [(0.0, z0), (ln, z0), (ln, z1), (0.0, z1)]
        pts = [(A.x + d.x * t, A.y + d.y * t, z) for t, z in corners]
        uvs = [((s + t) / L.PERIOD, 1.0 - (y0 + (z1 - z) / (z1 - z0) * m * PPM) / L.SIZE) for t, z in corners]
        mb.face(pts, uvs, [colf(z) for _, z in corners])
        s += ln + 5.3


def flat(mb, half, z, colf, up=True, shift=0.0):
    """A square top (or underside) in paving, 6 m rows like the court's paving."""
    ys = sorted({-half, half} | {6.0 * k - 3.0 for k in range(-3, 4) if -half < 6.0 * k - 3.0 < half})
    for i in range(len(ys) - 1):
        a, b = ys[i], ys[i + 1]
        k = math.floor((a + 3.0 + 1e-6) / 6.0)
        q = [(-half, a, z), (half, a, z), (half, b, z), (-half, b, z)]
        if not up:
            q = q[::-1]
        mb.face(q, [C.pave_uv(p[0], p[1] + 3.0, k, shift) for p in q], [colf(z)] * 4)


def lit(v, cool=0.0):
    return lambda z: C.tint(v, cool)


def foot_dark(z0, z1, lo=0.86, hi=1.03):
    return lambda z: C.tint(lo + (hi - lo) * min(1.0, max(0.0, (z - z0) / max(z1 - z0, 1e-3))), 0.3)


def block(mb, half, z0, z1, colf, top_shift=0.0, sides="plain", top=None, row=None):
    mb.new_island()
    if row:
        row_sides(mb, half, z0, z1, row, colf)
    else:
        ring_sides(mb, half, z0, z1, z1 if top is None else top, colf, plain_rows=(sides == "plain"))
    flat(mb, half, z1, colf, True, top_shift)


# ------------------------------------------------------------------------------------------------ the plinth
def plinth():
    mb, band = C.MB("plaza_plinth"), C.MB("plaza_plinth_fret")
    block(mb, 4.9, -0.6, 0.35, foot_dark(-0.6, 0.35, 0.84, 0.95), 1.3)        # sinks 0.6 under the paving
    block(mb, 4.55, 0.35, 0.7, foot_dark(0.35, 0.7, 0.92, 0.98), 4.1)
    block(mb, 4.0, 0.7, 3.35, foot_dark(0.7, 3.35, 0.93, 1.04), 7.7, sides="wall", top=3.35)
    block(mb, 4.35, 3.35, 3.8, lit(1.04), 2.2, row="cap_out")
    # under the cap's overhang
    mb.new_island()
    ring = [(4.35, -4.35), (4.35, 4.35), (-4.35, 4.35), (-4.35, -4.35)]
    inner = [(4.0, -4.0), (4.0, 4.0), (-4.0, 4.0), (-4.0, -4.0)]
    for i in range(4):
        j = (i + 1) % 4
        q = [(*ring[j], 3.35), (*ring[i], 3.35), (*inner[i], 3.35), (*inner[j], 3.35)]
        mb.face(q, [C.row_uv("cap_under_out", 0, 0)] * 4, [lit(0.86)(0)] * 4)
    # the carved fret band, proud 0.12 m, 1.2 m under the body's top (as build_bands)
    t, b, p = 3.35 - 1.2, 3.35 - 2.4, 0.12
    wt = L.WALL_TOP[0] + L.GUT
    sq = [(4.0, -4.0), (4.0, 4.0), (-4.0, 4.0), (-4.0, -4.0)]                  # CCW from above: outward on the right
    C.sweep(band, sq, True, [
        ((0.0, t), (p, t), "string", (0, 1)),
        ((p, t), (p, b), (wt + L.FRET[0] * PPM, 1.2), (1, 0)),
        ((p, b), (0.0, b), "string", (0, -1)),
    ], colf=lit(1.03))
    return [mb, band]


# ------------------------------------------------------------------------------------------------ the planter
def planter():
    mb = C.MB("plaza_planter")
    hx, hy, h, wall, soil = 3.0, 0.7, 0.75, 0.22, 0.6
    mb.new_island()
    ring = [(hx, -hy), (hx, hy), (-hx, hy), (-hx, -hy)]
    s = 0.0
    for i in range(4):
        a, b = ring[i], ring[(i + 1) % 4]
        side(mb, a, b, -0.3, h - 0.24, h, s, foot_dark(-0.3, h, 0.86, 1.02), plain_rows=True)
        s += 7.0
    row_sides(mb, 0, h - 0.24, h, "cap_out", lit(1.03), ring=ring)                # the woven band round its rim
    # the rim: a coping top all round, the inner faces down to the soil
    ix, iy = hx - wall, hy - wall
    inner = [(ix, -iy), (ix, iy), (-ix, iy), (-ix, -iy)]
    for i in range(4):
        j = (i + 1) % 4
        q = [(*ring[i], h), (*ring[j], h), (*inner[j], h), (*inner[i], h)]
        mb.face(q, [C.row_uv("cap_top", (k % 2) * 1.0 + i * 2.0, 0.1 + (0.0 if k in (0, 1) else 1.1)) for k in range(4)], [lit(1.04)(0)] * 4)
        side(mb, inner[j], inner[i], soil, h, h, 3.0 + i * 2.0, lit(0.84), plain_rows=True)
    # the soil: the dark patch (it is covered by plants in the game)
    q = [(-ix, -iy, soil), (ix, -iy, soil), (ix, iy, soil), (-ix, iy, soil)]
    mb.face(q, [C.patch_uv(L.DARK, p[0] + ix, p[1] + iy) for p in q], [C.tint(0.45, 0.0)] * 4)
    return [mb]


# ------------------------------------------------------------------------------------------------ the vine
def leaf_mat(name, rgb, picture):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(os.path.join(TEX, picture), check_existing=True)
    mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"
    mul.inputs[0].default_value = 1.0
    mul.inputs[7].default_value = (*rgb, 1.0)
    nt.links.new(tex.outputs["Color"], mul.inputs[6])
    nt.links.new(mul.outputs[2], b.inputs["Base Color"])
    nt.links.new(tex.outputs["Alpha"], b.inputs["Alpha"])
    b.inputs["Roughness"].default_value = 0.8
    try:
        m.surface_render_method = "DITHERED"
    except Exception:
        pass
    return m


def vine(seed=7, width=2.6, reach=1.0, name="plaza_vine"):
    """Strands from the coping's top over its edge and down the face, leaves alternating along them."""
    rng = random.Random(seed)
    light = leaf_mat("lagoon_leaf_round_groundcover_light", (0.55, 0.82, 0.14), "leaf_round_albedo.png")
    dark = leaf_mat("lagoon_leaf_round_bush_dark", (0.125, 0.40, 0.10), "leaf_round_albedo.png")
    stalk = leaf_mat("lagoon_stalk_monstera", (0.35, 0.65, 0.20), "stalk_albedo.png")
    verts, faces, uvs, mats, nrms = [], [], [], [], []

    def quad(p, u, n, mat_i):
        i = len(verts)
        verts.extend(p); faces.append((i, i + 1, i + 2, i + 3)); uvs.append(u); mats.append(mat_i); nrms.append(n)

    out = Vector((0.0, -1.0, 0.35)).normalized()                 # the mass's soft normal: out of the wall, a little up
    count = int(width / 0.19)
    for k in range(count):
        x = -width / 2 + (k + 0.5) * width / count + rng.uniform(-0.06, 0.06)
        # longer in the middle of the curtain, ragged at its ends
        mid = 1.0 - abs(x) / (width / 2)
        length = (1.4 + 3.6 * (0.35 + 0.65 * mid) * rng.uniform(0.6, 1.0)) * reach
        back = rng.uniform(0.35, 0.75)                           # how far over the coping's top it starts
        path = [Vector((x, back, 0.03)), Vector((x, 0.05, 0.03)), Vector((x, -0.06, -0.05))]
        drift = rng.uniform(-0.25, 0.25)
        steps = max(3, int(length / 0.35))
        for s in range(1, steps + 1):
            t = s / steps
            path.append(Vector((x + drift * t * t + 0.05 * math.sin(t * 7 + k), -0.07 - 0.05 * t, -0.05 - length * t)))
        # the stalk: a thin ribbon facing out of the wall
        for a, b in zip(path, path[1:]):
            w = 0.018
            quad([a + Vector((-w, -0.005, 0)), a + Vector((w, -0.005, 0)), b + Vector((w, -0.005, 0)), b + Vector((-w, -0.005, 0))],
                 [(0.45, 0), (0.55, 0), (0.55, 1), (0.45, 1)], out, 2)
        # leaves every ~0.13 m along it, alternating sides, larger and denser near the top
        total = sum((b - a).length for a, b in zip(path, path[1:]))
        at = 0.0
        side_ = 1
        while at < total - 0.05:
            # point at arc length `at`
            acc, p, d = 0.0, path[0], Vector((0, 0, -1))
            for a, b in zip(path, path[1:]):
                ln = (b - a).length
                if acc + ln >= at:
                    p = a.lerp(b, (at - acc) / ln); d = (b - a).normalized(); break
                acc += ln
            f = at / total
            size = rng.uniform(0.24, 0.36) * (1.15 - 0.45 * f)
            # the leaf hangs from its stalk tip-down and turned out of the wall, tilted to its side
            ang = side_ * rng.uniform(0.5, 1.1)
            down = Vector((math.sin(ang) * 0.8, -0.35, -math.cos(ang))).normalized()
            if p.z > -0.05:                                      # on the coping's top: leaves lie flat-ish, pointing back
                down = Vector((side_ * 0.7, 0.6 * rng.uniform(-1, 1), 0.25)).normalized()
            across = down.cross(Vector((0, -1, 0.2))).normalized() * (size * 0.42)
            base = p + Vector((0, -0.012, 0))
            tip = base + down * size
            # the picture's tip is its top (v = 1): base at v 0
            quad([base - across, base + across, tip + across, tip - across], [(0, 0), (1, 0), (1, 1), (0, 1)],
                 (out + Vector((rng.uniform(-0.2, 0.2), 0, rng.uniform(-0.1, 0.2)))).normalized(), 0 if rng.random() < 0.45 else 1)
            at += rng.uniform(0.09, 0.15) * (0.8 + 0.6 * f)
            side_ = -side_
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in verts], [], faces)
    for m in (light, dark, stalk):
        me.materials.append(m)
    uvl = me.uv_layers.new(name="UVMap")
    li = 0
    for fi, poly in enumerate(me.polygons):
        poly.material_index = mats[fi]
        poly.use_smooth = True
        for k in range(4):
            uvl.data[li + k].uv = uvs[fi][k]
        li += 4
    me.validate(); me.update()
    me.normals_split_custom_set([tuple(nrms[fi]) for fi in range(len(faces)) for _ in range(4)])
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    return o


# ------------------------------------------------------------------------------------------------ export
def export_stone(parts, name, path):
    mat = C.material(False)
    objs = [p.obj(mat, 0.05) for p in parts]
    tris = C.export(objs, path=path, name=name)
    print("PLAZA %s: %d triangles -> %s" % (name, tris, path))


def export_obj(o, path):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.gltf(filepath=path, use_selection=True, export_format="GLB", export_yup=True, export_apply=True,
                              export_normals=True, export_tangents=False)
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    print("PLAZA %s: %d triangles -> %s" % (o.name, tris, path))


def main():
    os.makedirs(os.path.join(OUT, "build"), exist_ok=True)
    for name, make in (("plaza_plinth", plinth), ("plaza_planter", planter)):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        export_stone(make(), name, os.path.join(OUT, "build", name + "_raw.glb"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    export_obj(vine(), os.path.join(OUT, "plaza_vine.glb"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    export_obj(vine(seed=19, width=3.4, reach=2.1, name="plaza_vine_long"), os.path.join(OUT, "plaza_vine_long.glb"))


if __name__ == "__main__":
    main()
