"""PAETE'S RUN: THE ART. The massif, every stage's obstacle, the dressing and the horizon (2026-10-10).

  blender -b --python tools/author_paete_run_art.py -- [--review N] [--cams a,b,c] [--no-export]

Owner: "the design for the map will be somewhat coliseum ish so sand rocky etc. we can take inspo from the rocks in
lagooncove for style"; "the coliseum is not the shape of the course or necessarily the main poi. i just said that for
style reference"; of the slides, "try to incorporate them well into the map"; of the grey course, "looks good. now
surprise me".

THE PLACE: a weathered sandstone massif in a dry bowl, half rock and half ruin: a quarry-city cut into the rock in
benches, long abandoned, that the forest spirit's vines have begun to take back. The course zigzags up its stepped face
and comes back down.

WHAT IS REUSED: Lagoon Cove's rock kit and its approved rock material (meshes, edge-wear UVs and all, from
ArtSource/lagoon/lagoon_cove.blend); Lagoon's sand, earth, grass, cloth, clay, timber, plank and leaf drawings; Kanto's
dressed-stone, plain-stone and paving drawings and two of its trees. See tools/author_paete_run_art_textures.py.

FILES: author_paete_run_art_kit.py (materials, the mesh buffer), _world.py (the gameplay layout as data, the ground),
_stages.py (the masonry members, the movers, the small props), _solids.py (EVERY WALKED SURFACE: one piece per layout
solid), _chutes.py (the two slides), _dress.py (the festival, the plants, the skyline), _check.py (the cover check that
allows `hide_solids`, the coplanar-face check, the lane check). This file assembles, checks, exports and renders.

WRITES: ArtSource/paete_run/paete_run_art.blend; Assets/TumbangPreso/Art/PaeteRun/Models/*.glb, paete_run_art.json,
materials_paete_run.json; with --review N, Logs/paete-run/art/paete_run_art_<view>_vN.png.

UNITY AXES: Blender (x, y, z) is Unity (-x, z, -y), yaw t is -t, AFTER the offset that puts the start line at the origin
(read from paete_run_layout.json when the gameplay's layout exists; the start way point until then).
"""
import json
import math
import random
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Vector, noise

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import author_paete_run_art_kit as K                 # noqa: E402
import author_paete_run_art_world as W               # noqa: E402
import author_paete_run_art_stages as ST             # noqa: E402
import author_paete_run_art_solids as SO             # noqa: E402
import author_paete_run_art_chutes as CHU            # noqa: E402
import author_paete_run_art_dress as DR              # noqa: E402
import author_paete_run_art_check as CHK             # noqa: E402
import author_paete_run_art_audit as AUD             # noqa: E402

ROOT = HERE.parent
ART = ROOT / "Assets/TumbangPreso/Art/PaeteRun"
MODELS = ART / "Models"
SOURCE = ROOT / "ArtSource/paete_run"
LOGS = ROOT / "Logs/paete-run/art"
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
REVIEW = int(ARGV[ARGV.index("--review") + 1]) if "--review" in ARGV else 0
EXPORT = "--no-export" not in ARGV
ONLY = ARGV[ARGV.index("--cams") + 1].split(",") if "--cams" in ARGV else None
CHECK = "--no-check" not in ARGV
if "--debug-at" in ARGV:
    K.DEBUG_AT = tuple(float(v) for v in ARGV[ARGV.index("--debug-at") + 1].split(","))
DRESS = "--no-dress" not in ARGV
UP = Vector((0, 0, 1))
T = K.T

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
CLOTH = DR.cloth_models() if DRESS else {}           # the hanging cloth is SIMULATED, in an empty scene, before anything else exists
print("[art] cloth settled:", sorted(CLOTH))
K.load_rock_kit()
W.make()
print("[art] ground built: %d x %d cells" % (W.NX - 1, W.NY - 1))


def coll(name, parent=None, hide=False):
    c = bpy.data.collections.new(name)
    (parent or sc.collection).children.link(c)
    return c


C_STATIC, C_MOVE, C_ROCKS, C_PROPS, C_KIT = coll("Static"), coll("MoversAtRest"), coll("Boulders"), coll("Dressing"), coll("Kit")

# ================================================================== the ground
def ground_material():
    m = bpy.data.materials.new("pr_ground")
    m.use_nodes = True
    nt = m.node_tree
    N, L = nt.nodes, nt.links
    bsdf = N["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.9
    geo = N.new("ShaderNodeNewGeometry")

    def tex(name, tint=(1, 1, 1)):
        outs = []
        for (scale, rot, loc) in ((0.25, 0.0, (0, 0, 0)), (0.25 * 0.61, math.radians(37), (0.37, 0.71, 0))):
            mp = N.new("ShaderNodeMapping")
            mp.inputs["Scale"].default_value = (scale, scale, scale); mp.inputs["Rotation"].default_value = (0, 0, rot); mp.inputs["Location"].default_value = loc
            img = N.new("ShaderNodeTexImage")
            img.image = bpy.data.images.load(str(K.TEXTURES / (name + "_albedo.png")), check_existing=True)
            L.new(geo.outputs["Position"], mp.inputs["Vector"]); L.new(mp.outputs["Vector"], img.inputs["Vector"])
            outs.append(img.outputs["Color"])
        nz = N.new("ShaderNodeTexNoise")
        nz.inputs["Scale"].default_value = 0.06
        mr = N.new("ShaderNodeMapRange"); mr.interpolation_type = "SMOOTHSTEP"
        mr.inputs["From Min"].default_value, mr.inputs["From Max"].default_value = 0.42, 0.58
        L.new(geo.outputs["Position"], nz.inputs["Vector"]); L.new(nz.outputs[0], mr.inputs["Value"])
        mx = N.new("ShaderNodeMix"); mx.data_type = "RGBA"
        L.new(mr.outputs["Result"], mx.inputs["Factor"]); L.new(outs[0], mx.inputs[6]); L.new(outs[1], mx.inputs[7])
        if tint != (1, 1, 1):
            mul = N.new("ShaderNodeMix"); mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
            mul.inputs["Factor"].default_value = 1.0
            L.new(mx.outputs[2], mul.inputs[6]); mul.inputs[7].default_value = (*tint, 1)
            return mul.outputs[2]
        return mx.outputs[2]

    def field(name, lo, hi):
        at = N.new("ShaderNodeAttribute"); at.attribute_name = name
        mr = N.new("ShaderNodeMapRange"); mr.interpolation_type = "SMOOTHSTEP"
        mr.inputs["From Min"].default_value, mr.inputs["From Max"].default_value = lo, hi
        L.new(at.outputs["Fac"], mr.inputs["Value"])
        return mr.outputs["Result"]

    def mix(a, b, fac):
        mx = N.new("ShaderNodeMix"); mx.data_type = "RGBA"
        L.new(fac, mx.inputs["Factor"]); L.new(a, mx.inputs[6]); L.new(b, mx.inputs[7])
        return mx.outputs[2]

    rock, sand, grass, earth = tex("rock_a", (0.86, 0.80, 0.74)), tex("sand_a", (1.0, 0.90, 0.72)), tex("grass_a", (1.0, 0.86, 0.50)), tex("earth_a")
    seabed = tex("sand_a", (0.50, 0.80, 0.70))
    out = mix(rock, sand, field("f_sand", -0.12, 0.12))
    out = mix(out, seabed, field("f_wet", -0.05, 0.4))
    out = mix(out, grass, field("f_grass", -0.12, 0.12))
    out = mix(out, earth, field("f_court", -0.08, 0.08))
    L.new(out, bsdf.inputs["Base Color"])
    return m


FIELD_ORDER_A, FIELD_ORDER_B = ("court", "grass", "sand", "wet"), ("steep", "ring")
FIELD_RANGE = {"court": (-4, 3), "grass": (-4, 3), "sand": (-3, 3), "wet": (-2, 4), "steep": (-3, 3), "ring": (-1, 1)}


def field_mesh(name, xs, ys, Hh, fields, skip=None):
    """A grid mesh with the ground material's fields: float attributes for Blender, COLOR_0 and UV2 for Unity."""
    nx, ny = len(xs), len(ys)
    verts = [(xs[i], ys[j], float(Hh[i, j])) for i in range(nx) for j in range(ny)]
    faces = [(i * ny + j, (i + 1) * ny + j, (i + 1) * ny + j + 1, i * ny + j + 1) for i in range(nx - 1) for j in range(ny - 1)
             if not (skip and skip(i, j))]
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    for f in ("court", "grass", "sand", "wet", "steep"):
        a = me.attributes.new("f_" + f, "FLOAT", "POINT")
        a.data.foreach_set("value", fields[f].reshape(-1).astype(np.float32))
    loops = np.zeros(len(me.loops), dtype=np.int32)
    me.loops.foreach_get("vertex_index", loops)
    uv = me.uv_layers.new(name="UVMap")
    co = np.array(verts)
    uv.data.foreach_set("uv", (co[loops][:, :2] * 0.25).reshape(-1))
    uv2 = me.uv_layers.new(name="FieldsB")
    fb = np.stack([(fields[f].reshape(-1) - FIELD_RANGE[f][0]) / (FIELD_RANGE[f][1] - FIELD_RANGE[f][0]) for f in FIELD_ORDER_B], axis=1)
    uv2.data.foreach_set("uv", fb[loops].reshape(-1))
    col = me.color_attributes.new("Col", "FLOAT_COLOR", "CORNER")
    fa = np.stack([(fields[f].reshape(-1) - FIELD_RANGE[f][0]) / (FIELD_RANGE[f][1] - FIELD_RANGE[f][0]) for f in FIELD_ORDER_A], axis=1)
    col.data.foreach_set("color", fa[loops].reshape(-1))
    me.color_attributes.active_color = col
    me.uv_layers.active_index = 0
    me.materials.append(bpy.data.materials.get("pr_ground") or ground_material())
    me.polygons.foreach_set("use_smooth", [True] * len(me.polygons))
    me.update()
    o = bpy.data.objects.new(name, me)
    C_STATIC.objects.link(o)
    return o


xs, ys = np.linspace(W.X0, W.X1, W.NX), np.linspace(W.Y0, W.Y1, W.NY)
terrain = field_mesh("run_terrain", xs, ys, W.H, W.FIELDS)

# the horizon: the bowl's rim and the country beyond it, coarse, so the map never ends in a flat plane
HC = 16.0
hx, hy = np.arange(W.X0 - 26 * HC, W.X1 + 26 * HC + 1, HC), np.arange(W.Y0 - 26 * HC, W.Y1 + 26 * HC + 1, HC)
HH = np.zeros((len(hx), len(hy)))
for i, x in enumerate(hx):
    for j, y in enumerate(hy):
        r = math.hypot((x - W.CENTRE[0]) / 175.0, (y - W.CENTRE[1]) / 190.0)
        t = min(1.0, max(0.0, (r - 1.25) / 1.3)); t = t * t * (3 - 2 * t)
        ridge = abs(noise.noise(Vector((x * 0.004, y * 0.004, 5.0))))
        mesa = 46.0 * t * (0.35 + 1.5 * ridge) + 9.0 * t * noise.noise(Vector((x * 0.012, y * 0.012, 9.0)))
        inside = W.X0 - 1 <= x <= W.X1 + 1 and W.Y0 - 1 <= y <= W.Y1 + 1
        HH[i, j] = W.bowl_height(x, y) + mesa - (2.0 if inside else 0.0)
gx, gy = np.gradient(HH, HC)
hs = np.hypot(gx, gy)
hf = dict(court=np.full(HH.shape, -4.0), grass=np.full(HH.shape, -4.0), sand=np.clip((0.5 - hs) * 3.0, -3, 3), wet=np.full(HH.shape, -2.0),
          steep=np.clip((hs - 0.55) * 3.0, -3, 3), ring=np.full(HH.shape, -1.0))
horizon = field_mesh("run_horizon", hx, hy, HH, hf,
                     skip=lambda i, j: hx[i] >= W.X0 and hx[i + 1] <= W.X1 and hy[j] >= W.Y0 and hy[j + 1] <= W.Y1)

# ================================================================== every walked surface, the slides, the set pieces
ST.S, ST.P = ST.Set("run", 11), ST.Set("rest", 23)
SO.build_all()
CHU.build_all()
SO.show_movers()
L1, L36, L22 = W.LEGS[0], W.LEGS[35], W.LEGS[21]
rng = random.Random(77)
ST.gate(L1.A + L1.f * 1.5, L1.f, rng)
ST.gate(L36.C - L36.f * 6.2, L36.f, rng, finish=True)
ST.rotunda((36.0, 204.0), 46.0, rng)
print("[art] solids drawn: %d of %d" % (len(SO.BUILT), len(W.SOLIDS)), "| slides:", json.dumps(CHU.INFO))

# checkpoints: a standing stone beside the start of every leg that has ground there
for leg in W.LEGS:
    q = leg.A + leg.s * (leg.half + 1.9) + leg.f * 0.8
    g = W.ground(q.x, q.y)
    if abs(g - (leg.A.z - 0.35)) < 0.9 and not W.blocked(q.x, q.y, g, g + 2.5, 0.6, margin=0.2):
        ST.PROPS.append(("prop_checkpoint", q.x, q.y, g, random.Random(leg.i).uniform(0, 360), 1.0))

# The pools' water is UNITY'S (PaeteRunSceneBuilder lays a sheet on each layout pool): what is built here is for the
# review pictures only and is never exported, so the two never lie in one plane.
POOLW = K.Buf("run_pool_water", 6)
for wv in W.WATERS:
    _wx, _wy, zw = wv.sz / 2, wv.sx / 2, wv.d["surface"] - 0.04
    POOLW.quad([tuple(wv.at(a, b, zw)) for a, b in ((-_wx, -_wy), (_wx, -_wy), (_wx, _wy), (-_wx, _wy))][::-1 if wv.flat.cross(wv.side).z < 0 else 1], "pr_water")
WATER = K.Buf("review_water", 5)
for wv in W.WATERS:
    _wx, _wy, zw = wv.sz / 2, wv.sx / 2, wv.d["surface"]
    WATER.quad([tuple(wv.at(a, b, zw)) for a, b in ((-_wx, -_wy), (_wx, -_wy), (_wx, _wy), (-_wx, _wy))][::-1 if wv.flat.cross(wv.side).z < 0 else 1], "pr_water")

# ================================================================== the massif's boulders (Lagoon's kit, placed by the list)
PLACE = []      # (model, x, y, z, yaw degrees, scale)
brng = random.Random(4242)
placed = []


def boulder_ok(x, y, R, top, base):
    near, d = W.near_legs(x, y, R + 22.0)
    for row, dist in zip(near, d):
        zl, half, below, mode_i = row[2], row[3], row[4], int(row[6])
        if dist < R * 0.85 + half + 1.2:
            leg = W.ALL[mode_i - 1] if mode_i < 100 else W.CUTS[mode_i - 100]
            if leg.mode in ("pool", "chute"):
                return False
            if top > zl - (0.55 if leg.mode == "terrace" else below + 1.5):
                return False
        elif dist < R + 15.0:
            if top > max(zl + 3.2, base + 2.6):
                return False
    for s in W.SOLIDS:                                            # no boulder swallows a piece of the course
        if s.reach(x, y) < R * 0.8 and top > s.bot - 0.3 and not s.name.startswith("pool_"):
            return False
    for (px, py, pr) in placed:
        if math.hypot(px - x, py - y) < 0.66 * (pr + R):
            return False
    return True


cands = []
for _ in range(15000):
    x, y = brng.uniform(W.X0 + 8, W.X1 - 8), brng.uniform(W.Y0 + 8, W.Y1 - 8)
    sl = W.slope(x, y)
    g = W.ground(x, y)
    rocky = min(1.0, max(0.0, (sl - 0.3) * 1.7))
    on_massif = g > W.BOWL + 3.0
    p = rocky * 0.62 + (0.03 if on_massif else 0.0012)
    if brng.random() > p:
        continue
    big = brng.random() ** 1.6
    s = 1.9 + (6.4 if rocky > 0.3 else 2.4) * big
    cands.append((s, x, y))
cands.sort(reverse=True)
for (s, x, y) in cands:
    fam = brng.random()
    idx = brng.choice(list(K.BOULDERS)) if fam < 0.58 else brng.choice(list(K.COBBLES)) if fam < 0.74 else brng.choice(list(K.SPLITS)) if fam < 0.86 \
        else brng.choice(list(K.SLABS)) if fam < 0.95 else brng.choice(list(K.STACKS)[1:])
    if idx in K.STACKS:
        s *= 0.5
    rk = K.ROCKS[idx]
    R = 1.25 * s
    g0, gmin = W.ground(x, y), W.ground_min(x, y, R * 0.8)
    z = (g0 + gmin) / 2 - 0.06 * s
    top = z + rk["top"] * s
    if not boulder_ok(x, y, R, top, g0):
        continue
    placed.append((x, y, R))
    PLACE.append((rk["name"], x, y, z, brng.uniform(0, 360), s))
# the horizon's rock stacks, far and huge: baked into one piece in a flat hazed tan (Lagoon's far rocks were done so)
FAR = K.Buf("run_horizon_rocks", 3)
for k in range(40):
    a = math.tau * k / 40 + brng.uniform(-0.05, 0.05)
    rr = brng.uniform(1.75, 2.9)
    x, y = W.CENTRE[0] + math.cos(a) * 175 * rr, W.CENTRE[1] + math.sin(a) * 190 * rr
    i, j = int((x - hx[0]) / HC), int((y - hy[0]) / HC)
    if 0 <= i < len(hx) and 0 <= j < len(hy):
        s = brng.uniform(10, 26)
        idx = brng.choice(list(K.STACKS)[1:] + list(K.BOULDERS) + list(K.SPLITS))
        s *= 0.7 if idx in K.STACKS else 1.0
        FAR.rock(idx, T(x, y, HH[i, j] - 0.12 * s, brng.uniform(0, 6.28)) @ Matrix.Diagonal((s, s, s * brng.uniform(1.0, 1.5), 1)), mat="pr_far_rock")
        for j2 in range(brng.randint(1, 3)):                           # each far stack has a smaller one or two leaning on it
            a2, s2 = brng.uniform(0, 6.28), s * brng.uniform(0.4, 0.7)
            x2, y2 = x + math.cos(a2) * s * 1.6, y + math.sin(a2) * s * 1.6
            FAR.rock(brng.choice(list(K.BOULDERS)), T(x2, y2, HH[i, j] - 0.2 * s2, brng.uniform(0, 6.28)) @ Matrix.Diagonal((s2, s2, s2, 1)), mat="pr_far_rock")
# a few tall stacks standing off the lanes: the massif's skyline (slender, so they frame the course and do not hide it)
heroes = []
for _ in range(4000):
    x, y = brng.uniform(-120, 140), brng.uniform(-60, 215)
    g = W.ground(x, y)
    near, d = W.near_legs(x, y, 13.0)
    if g < W.BOWL + 2.0 or len(d) or any(math.hypot(hx_ - x, hy_ - y) < 34 for (hx_, hy_) in heroes) or any(s_.reach(x, y) < 7.0 for s_ in W.SOLIDS):
        continue
    idx = brng.choice(list(K.STACKS)[1:])
    s = brng.uniform(2.6, 4.8)
    heroes.append((x, y)); placed.append((x, y, 1.25 * s))
    PLACE.append((K.ROCKS[idx]["name"], x, y, W.ground_min(x, y, s) - 0.1 * s, brng.uniform(0, 360), s))
print("[art] boulders placed:", len(PLACE), "of them tall stacks", len(heroes))

# ================================================================== the dressing
if DRESS:
    DR.build_all(placed, PLACE, HH, hx, hy, HC)
print("[art] props placed:", len(ST.PROPS))

# ================================================================== objects
def set_objs(st, bevel, collection):
    out = []
    for b, bv in ((st.m, bevel), (st.r, 0.0), (st.c, 0.0)):
        if b.f:
            out.append(b.obj(bv, collection))
    return out


static_objs = {"run_terrain": terrain, "run_horizon": horizon}
for b, bv, nm in ((ST.S.m, 0.06, "run_masonry"), (ST.S.r, 0.0, "run_rockwork"), (ST.S.c, 0.0, "run_cards"), (SO.PAV, 0.0, "run_paving"),
                  (CHU.CH, 0.0, "run_slides"), (CHU.WAT, 0.0, "run_slide_water"), (POOLW, 0.0, "run_pool_water"), (FAR, 0.0, "run_horizon_rocks")) + tuple(DR.STATIC):
    if not b.f:
        continue
    b.name = nm
    static_objs[nm] = b.obj(bv, C_STATIC)
rest_objs = set_objs(ST.P, 0.045, C_MOVE)


def joined(objs, name, collection):
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
        o.modifiers.clear(); o.data = me
    if len(objs) > 1:
        with bpy.context.temp_override(active_object=objs[0], selected_editable_objects=objs, selected_objects=objs, object=objs[0]):
            bpy.ops.object.join()
    o = objs[0]
    o.name = name; o.data.name = name
    return o


kit_objs = {}


def kit_piece(name, fn, bevel=0.03, **kw):
    q = ST.Set(name, sum(ord(ch) for ch in name) % 1000)
    fn(q, **kw)
    c = coll("kit_" + name, C_KIT, hide=True)
    objs = set_objs(q, bevel, c)
    bpy.context.view_layer.update()
    kit_objs[name] = (joined(objs, name, c), c)


for name, fn in ST.PROP_FN.items():
    kit_piece(name, fn)
for kind, fn in ST.MOVER_FN.items():
    kit_piece("mover_" + kind, fn, 0.045)
kit_piece("mover_boulder_chain_b", ST.mv_boulder_chain, 0.045, r=1.9, kit=3, seed=4)
kit_piece("mover_boulder_chain_c", ST.mv_boulder_chain, 0.045, r=1.1, kit=15, seed=5)
kit_piece("mover_floating_rock_b", ST.mv_floating_rock, 0.045, r=1.9, kit=4, seed=3)
kit_piece("mover_pendulum_b", ST.mv_pendulum, 0.045, kit=3, seed=2)
for name, (o, c) in CLOTH.items():
    sc.collection.children.unlink(c); C_KIT.children.link(c)
    kit_objs[name] = (o, c)
for rk in K.ROCKS:                                                # the kit stones as their own models
    c = coll("kit_" + rk["name"], C_KIT, hide=True)
    o = bpy.data.objects.new(rk["name"], rk["mesh"]); c.objects.link(o)
    o["rock_scale"] = 1.0
    kit_objs[rk["name"]] = (o, c)
# approved models of the other maps, brought through whole with this map's material names (Kanto's trees; Lagoon
# Cove's palms, bushes, banana plants, jars, crates, benches, a table, a lean-to and its stall; Eskinita's cart)
TREE_MAT = (("leaf_light", "pr_tree_leaf_light"), ("leaf_dark", "pr_tree_leaf_dark"), ("core", "pr_tree_core"), ("trunk", "pr_tree_trunk"))
BORROW = [("tree_lean_olive", "Kanto", "tree_lean_olive", TREE_MAT), ("tree_round", "Kanto", "tree_round", TREE_MAT)] + (DR.BORROW if DRESS else [])
for (tree, folder, source, TREE_MAT) in BORROW:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(ROOT / "Assets/TumbangPreso/Art" / folder / "Models" / (source + ".glb")))
    new = [o for o in bpy.data.objects if o not in before]
    c = coll("kit_" + tree, C_KIT, hide=True)
    meshes = []
    for o in new:
        for uc in list(o.users_collection):
            uc.objects.unlink(o)
        c.objects.link(o)
        if o.type == "MESH":
            meshes.append(o)
            for slot in o.material_slots:
                nm = slot.material.name if slot.material else ""
                for key, to in TREE_MAT:
                    if key in nm:
                        slot.material = K.material(to); break
                else:
                    print("[art] BORROWED MODEL %s: no material rule for '%s'" % (tree, nm))
    kit_objs[tree] = (meshes[0] if meshes else None, c)
    print("[art] tree", tree, [o.name for o in new], sorted({s.material.name for o in meshes for s in o.material_slots if s.material}))

for (model, x, y, z, yaw, s) in PLACE + ST.PROPS:
    o, c = kit_objs[model]
    if model.startswith("rock_"):
        e = bpy.data.objects.new("b_" + model, o.data)
        e["rock_scale"] = s
        C_ROCKS.objects.link(e)
    else:
        e = bpy.data.objects.new("p_" + model, None)
        e.instance_type, e.instance_collection = "COLLECTION", c
        C_PROPS.objects.link(e)
    e.location, e.rotation_euler, e.scale = (x, y, z), (0, 0, math.radians(yaw)), (s, s, s)

# the cloth's settled shapes in a row on the far sand, for the close review picture only (never exported)
SHOW = next(((float(x), float(y), W.ground(x, y) + 3.4) for x in range(-150, 160, 6) for y in (-104, -96, 250, 258)
             if all(math.hypot(px - x - 8, py - y) > pr + 16 for (px, py, pr) in placed) and abs(W.ground(x, y) - W.ground(x + 22, y)) < 1.5), (-150.0, -100.0, W.ground(-150.0, -100.0) + 3.4))
for _k, _nm in enumerate(["banner_red_a", "banner_red_b", "banner_red_c", "banner_red_d", "banner_teal_a", "banner_teal_c", "banner_yellow_b", "banner_yellow_d"]):
    if _nm in kit_objs:
        e = bpy.data.objects.new("show_" + _nm, None)
        e.instance_type, e.instance_collection = "COLLECTION", kit_objs[_nm][1]
        e.location = (SHOW[0] + _k * 1.5, SHOW[1], SHOW[2])
        C_MOVE.objects.link(e)
if "awning_cloth_red" in kit_objs:
    for _k, _nm in enumerate(["awning_cloth_red", "prop_awning", "awning_cloth_teal", "prop_awning"]):
        e = bpy.data.objects.new("show_" + _nm, None)
        e.instance_type, e.instance_collection = "COLLECTION", kit_objs[_nm][1]
        e.location = (SHOW[0] + 15.0 + (_k // 2) * 5.0, SHOW[1], SHOW[2] - 3.4)
        C_MOVE.objects.link(e)

# ================================================================== light and cameras
ids = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in ids else "BLENDER_EEVEE"
w = bpy.data.worlds.new("sky"); sc.world = w; w.use_nodes = True
# The camera sees a clear blue sky; the LIGHTING is filled by a warm grey (Lagoon's lesson: a blue fill turned shaded stone cold).
_bg = w.node_tree.nodes["Background"]
_lp, _mx = w.node_tree.nodes.new("ShaderNodeLightPath"), w.node_tree.nodes.new("ShaderNodeMix")
_mx.data_type = "RGBA"
_gr = w.node_tree.nodes.new("ShaderNodeTexGradient"); _tc = w.node_tree.nodes.new("ShaderNodeTexCoord"); _sep = w.node_tree.nodes.new("ShaderNodeSeparateXYZ")
_ramp = w.node_tree.nodes.new("ShaderNodeMix"); _ramp.data_type = "RGBA"
_ramp.inputs[6].default_value = (0.80, 0.84, 0.86, 1); _ramp.inputs[7].default_value = (0.22, 0.46, 0.86, 1)
w.node_tree.links.new(_tc.outputs["Generated"], _sep.inputs[0])
_mr = w.node_tree.nodes.new("ShaderNodeMapRange"); _mr.inputs["From Min"].default_value, _mr.inputs["From Max"].default_value = -0.02, 0.45
w.node_tree.links.new(_sep.outputs["Z"], _mr.inputs["Value"]); w.node_tree.links.new(_mr.outputs["Result"], _ramp.inputs["Factor"])
_mx.inputs[6].default_value = (0.66, 0.66, 0.68, 1)
w.node_tree.links.new(_lp.outputs["Is Camera Ray"], _mx.inputs["Factor"]); w.node_tree.links.new(_ramp.outputs[2], _mx.inputs[7])
w.node_tree.links.new(_mx.outputs[2], _bg.inputs[0])
_bg.inputs[1].default_value = 0.95
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy, sun.data.color, sun.data.angle = 5.0, (1.0, 0.89, 0.70), math.radians(2)
SUN_DIR = Vector((0.52, -0.56, 0.64)).normalized()               # toward the sun: late morning, from the south-east, raking the stepped face
sun.rotation_euler = (-SUN_DIR).to_track_quat("-Z", "Y").to_euler()
sc.view_settings.view_transform = "AgX"
for look in ("AgX - Punchy", "Punchy"):
    try:
        sc.view_settings.look = look; break
    except TypeError:
        continue
try:
    sc.eevee.use_shadows = True
    sc.eevee.shadow_ray_count = 1
    sc.eevee.shadow_pool_size = "1024"
except Exception:
    pass
sc.render.resolution_x, sc.render.resolution_y = 1600, 900
EYE = Vector((0, 0, 1.25))
cen = Vector((W.CENTRE[0], W.CENTRE[1], 14))


def eye(i, t=0.0, back=1.5, ahead=0.6, side=0.0, lift=0.0, look_i=None, look_lift=1.0):
    leg = W.LEGS[i - 1]
    a = leg.at(t) - leg.f * back + leg.s * side + EYE + UP * lift
    tgt = (W.LEGS[look_i - 1] if look_i else leg).at(ahead) + UP * look_lift
    return (a, tgt, 63, None)


CAMS = {
    "air_south": (cen + Vector((120, -300, 185)), cen, 36, None), "air_north": (cen + Vector((-150, 310, 170)), cen, 36, None),
    "air_east": (cen + Vector((330, 40, 150)), cen, 36, None),
    "plan": (Vector((cen.x, cen.y, 600)), None, None, 410),
    "eye_start": eye(1, 0.0, back=6.0, lift=0.3), "eye_boulders": eye(2, 0.0, back=4.0, ahead=0.7, lift=0.4), "eye_beams": eye(3, 0.02, back=0.0, ahead=0.8),
    "eye_arches": eye(4, 0.0, back=3.0, ahead=0.6, look_lift=3.0), "eye_gauntlet": eye(5, 0.0, back=2.0, ahead=0.8, look_lift=2.0),
    "eye_spur": eye(7, 0.0, back=2.0, ahead=0.5, look_lift=5.0), "eye_bridge": eye(8, 0.0, back=1.0, ahead=0.8),
    "eye_colonnade": eye(9, 0.0, back=4.0, ahead=0.7, lift=0.5), "eye_dive": eye(11, 0.0, back=4.0, ahead=0.5, look_i=12, look_lift=0.0),
    "eye_wheels": eye(13, 0.0, back=5.0, ahead=0.7, lift=0.6), "eye_chains": eye(15, 0.0, back=2.0, ahead=0.6, look_lift=4.0),
    "eye_logs": eye(16, 0.0, back=3.0, ahead=0.7, lift=0.5, side=1.5), "eye_seesaws": eye(17, 0.0, back=4.0, ahead=0.7, lift=0.6),
    "eye_steps": eye(18, 0.0, back=5.0, ahead=0.8, lift=0.8), "eye_summit": eye(21, 0.1, back=0.0, ahead=1.0, look_lift=1.5),
    "eye_ledges": eye(25, 0.0, back=4.0, ahead=0.7, lift=0.5),
    "eye_floating": eye(29, 0.0, back=5.0, ahead=0.7, lift=1.0), "eye_crumble": eye(30, 0.0, back=3.0, ahead=0.8, lift=0.4),
    "eye_drums": eye(33, 0.0, back=5.0, ahead=0.7, lift=1.0), "eye_goat": (W.CUTS[4].at(0.0) + Vector((2.5, 2.0, 1.6)), W.CUTS[4].at(0.5) + UP * 0.5, 63, None),
    "eye_chimney": (W.CUTS[2].at(0.0) + Vector((-3.0, -4.0, 1.6)), W.CUTS[2].at(0.7) + UP * 3.0, 63, None), "eye_longvine": (W.CUTS[0].at(0.0) + Vector((1.0, -5.0, 1.6)), W.CUTS[0].at(0.6) + UP * 3.0, 63, None),
    "eye_pool1": eye(12, 0.1, back=0.0, ahead=1.0, lift=-0.6, look_lift=0.5), "eye_pool3": eye(32, 0.2, back=0.0, ahead=1.0, lift=-0.6, look_lift=1.0),
    "eye_finish": eye(36, 0.1, back=0.0, ahead=1.2, look_lift=2.0),
    "close_boulders": (W.LEGS[1].at(0.5) + Vector((6, -34, 16)), W.LEGS[1].at(0.5) + Vector((0, 0, 3)), 40, None),
    "close_colonnade": (W.LEGS[8].at(0.5) + Vector((8, -34, 14)), W.LEGS[8].at(0.5), 40, None),
    "close_pool1": (W.LEGS[11].at(0.5) + Vector((-46, -22, 26)), W.LEGS[11].at(0.5) + Vector((6, 6, 4)), 40, None),
    "close_seesaws": (W.LEGS[16].at(0.5) + Vector((-8, -34, 16)), W.LEGS[16].at(0.5), 40, None),
    "close_summit": (W.LEGS[20].at(0.5) + Vector((30, 38, 24)), W.LEGS[20].at(0.7) + Vector((0, 4, 2)), 40, None),
    "close_pool2": (W.LEGS[27].at(0.8) + Vector((30, -44, 30)), W.LEGS[28].at(0.3), 40, None),
    "close_last": (W.LEGS[30].at(0.6) + Vector((50, -30, 34)), W.LEGS[30].at(0.7), 40, None),
    "close_finish": (Vector((-128, 22, 22)), Vector((-80, -12, 5)), 40, None),
    "close_gate": (L1.A + Vector((-12, 5, 3.0)), L1.A + Vector((4, 0, 4.0)), 50, None),
    "close_lintel": (L1.A + L1.f * 1.5 - L1.f * 3.2 + Vector((0.8, 0, 1.3)), L1.A + L1.f * 1.5 + Vector((0, 0, 6.3)), 70, None),
    "close_gate_side": (L1.A + L1.f * 0.2 + L1.s * 11.0 + UP * 4.0, L1.A + L1.f * 0.2 + L1.s * 5.3 + UP * 4.0, 50, None),
    "close_fgate_side": (L36.C - L36.f * 7.6 + L36.s * 11.0 + UP * 16.0, L36.C - L36.f * 7.6 + L36.s * 5.3 + UP * 16.0, 50, None),
    "close_pendulum": (W.LEGS[4].at(0.5) + Vector((9, -13, 7)), W.LEGS[4].at(0.5) + UP * 5.5, 50, None),
    "close_beams": (W.LEGS[2].at(0.3) + Vector((-2, -9, 4.5)), W.LEGS[2].at(0.5), 50, None),
    "close_bridge": (W.LEGS[7].at(0.0) + Vector((3.5, 1.5, 2.3)), W.LEGS[7].at(0.7) + UP * 0.5, 63, None),
    "close_geyser6": (W.LEGS[5].at(0.3) + Vector((6, -9, 7)), W.LEGS[5].at(0.75), 50, None),
    "close_banners": (Vector((SHOW[0] + 5.2, SHOW[1] - 9.5, SHOW[2] - 0.6)), Vector((SHOW[0] + 5.2, SHOW[1], SHOW[2] - 1.4)), 50, None),
    "close_awnings": (Vector((SHOW[0] + 17.5, SHOW[1] + 9.0, SHOW[2] - 0.6)), Vector((SHOW[0] + 17.5, SHOW[1], SHOW[2] - 1.9)), 50, None),
}
for _n in (22, 31):                                              # the racer's eye on both slides: top, middle, end
    _p = W.LEGS[_n - 1].pts
    for _nm, _i, _j in (("top", 0, 3), ("mid", 5, 8), ("end", 9, 12)):
        _f = (_p[_i + 1] - _p[_i]).normalized()
        CAMS["eye_slide%d_%s" % (_n, _nm)] = (_p[_i] - _f * (2.6 if _nm == "top" else 0.0) + EYE + UP * (0.15 if _nm == "top" else 0.0), _p[_j] + UP * 0.8, 63, None)
    CAMS["close_slide%d" % _n] = (_p[6] + Vector((-26, -30, 22)) if _n == 22 else _p[6] + Vector((30, 18, 20)), _p[6], 44, None)
    CAMS["under_slide%d" % _n] = (_p[7] + Vector((10, -16, -5)) if _n == 22 else _p[4] + Vector((16, -6, -4)), _p[6] + UP * -1.0, 55, None)
for _nm in ("landing_5", "tier_6", "landing_9", "landing_20"):       # the round platforms: straight down, and from 1.7 m
    _s = W.BY[_nm]
    CAMS["top_" + _nm] = (Vector((_s.c.x, _s.c.y, _s.top + 40.0)), None, None, 15.0)
    _d = Vector((0.6, -0.8, 0.0))
    CAMS["eye_" + _nm] = (Vector((_s.c.x, _s.c.y, _s.top + 1.7)) + _d * 7.5, Vector((_s.c.x, _s.c.y, _s.top)), 63, None)
for name, (e, look, fov, ortho) in CAMS.items():
    cd = bpy.data.cameras.new(name); co = bpy.data.objects.new("cam_" + name, cd); sc.collection.objects.link(co)
    cd.clip_start, cd.clip_end = 0.2, 3000
    if ortho:
        cd.type, cd.ortho_scale = "ORTHO", ortho
    else:
        cd.sensor_fit, cd.angle_y = "VERTICAL", math.radians(fov)
    co.location = e
    co.rotation_euler = (0, 0, math.pi / 2) if look is None else (Vector(look) - Vector(e)).to_track_quat("-Z", "Y").to_euler()

SOURCE.mkdir(parents=True, exist_ok=True); LOGS.mkdir(parents=True, exist_ok=True); MODELS.mkdir(parents=True, exist_ok=True)
bpy.context.view_layer.update()
tri = {n: K.tris_of(o) for n, o in static_objs.items()}
ktri = {n: sum(K.tris_of(x) for x in c.objects if x.type == "MESH") for n, (o, c) in kit_objs.items() if o is not None}
for (tree, *_r) in BORROW:
    ktri[tree] = sum(K.tris_of(o) for o in kit_objs[tree][1].objects if o.type == "MESH")
counts = {}
for (model, *_r) in PLACE + ST.PROPS:
    counts[model] = counts.get(model, 0) + 1
placed_tris = sum(ktri[m] * n for m, n in counts.items())
rest_tris = sum(K.tris_of(o) for o in rest_objs)
print("[art] STATIC PIECES:", json.dumps(tri))
print("[art] KIT PIECES:", json.dumps(ktri))
print("[art] placed %d things, %d triangles; movers at rest %d triangles; static %d; WHOLE MAP %d" % (
    len(PLACE) + len(ST.PROPS), placed_tris, rest_tris, sum(tri.values()), placed_tris + rest_tris + sum(tri.values())))
print("[art] materials used:", len({s.material.name for o in bpy.data.objects if o.type == "MESH" for s in o.material_slots if s.material}))
for n in ST.NOTES:
    print("[art] NOTE", n)

# ================================================================== the checks
HIDE, COVER_WORDS, COPLANAR_WORDS, TERRAIN_WORDS, JUNCTION_WORDS, JPAIRS, CLOTH_WORDS = False, "not run", "not run", "not run", "not run", [], "not run"
if CHECK:
    walk = [static_objs[n] for n in ("run_masonry", "run_rockwork", "run_paving", "run_slides", "run_slide_water") if n in static_objs]
    fails, buried, worst = CHK.cover(walk)
    for (name, kind, bad) in fails:
        print("[check] COVER FAIL %s (%s): %s" % (name, kind, "; ".join("point %d: nearest art %s%s" % (i, "none" if dz is None else "%+.3f m" % dz, "" if tg is None else " (%s, %s)" % tg) for i, dz, tg in bad)))
    not_drawn = [s.name for s in W.SOLIDS if s.name not in SO.BUILT]
    COVER_WORDS = "%d of %d layout solids have art within 3 cm below to 1 cm above their top at all five points; %d fail; %d points are inside another solid's body and have no art of their own; worst offset %.1f mm" % (
        len(W.SOLIDS) - len(fails), len(W.SOLIDS), len(fails), buried, worst * 1000)
    print("[check] COVER:", COVER_WORDS, "| no builder:", not_drawn)
    HIDE = not fails and not not_drawn
    flat = [static_objs[n] for n in ("run_masonry", "run_paving", "run_slides", "run_slide_water") + tuple(n for _b, _v, n in DR.STATIC) if n in static_objs]
    found, nfaces = CHK.coplanar(flat)
    for row in found[:40]:
        print("[check] COPLANAR %.4f m2 at %s: %s/%s with %s/%s, normal %s" % row)
    COPLANAR_WORDS = "%d overlapping coplanar pairs among %d faces of the static art (same plane within 2 mm, same facing, over 4 cm2)" % (len(found), nfaces)
    print("[check] COPLANAR:", COPLANAR_WORDS)
    kit_bad = 0
    for n, (o, c) in kit_objs.items():
        if o is None or n.startswith("rock_") or n in [b[0] for b in BORROW]:
            continue
        f2, _n = CHK.coplanar([x for x in c.objects if x.type == "MESH"])
        if f2:
            kit_bad += len(f2)
            print("[check] COPLANAR in kit model %s: %d pairs, the largest %.4f m2 (%s with %s)" % (n, len(f2), f2[0][0], f2[0][3], f2[0][5]))
    COPLANAR_WORDS += "; %d pairs inside the kit models" % kit_bad
    rock_extra = []
    for (model, x, y, z, yaw, s) in PLACE:
        rk = next(r for r in K.ROCKS if r["name"] == model)
        M = T(x, y, z, math.radians(yaw)) @ Matrix.Diagonal((s, s, s, 1))
        rock_extra.append((model, [M @ v for v in rk["verts"]], rk["polys"]))
    tbad = CHK.terrain()
    for row in tbad[:30]:
        print("[check] TERRAIN over %s by %.3f m at %s" % row)
    TERRAIN_WORDS = "%d of %d solids have the ground less than 15 cm under their top somewhere" % (len(tbad), len(W.SOLIDS))
    print("[check] TERRAIN:", TERRAIN_WORDS)
    cpts = []
    _v, _p, _m = CHK.mesh_data(static_objs["run_cards"])
    _pen = sorted({i for poly, mm in zip(_p, _m) if mm.startswith("pr_cloth") for i in poly})
    cpts.append(("the pennants (run_cards)", [_v[i] for i in _pen]))
    for (m_, x, y, z, yaw, s) in ST.PROPS:
        if m_.startswith(("banner_", "awning_cloth_")):
            M = T(x, y, z, math.radians(yaw)) @ Matrix.Diagonal((s, s, s, 1))
            cpts.append(("%s at (%.0f, %.0f, %.0f)" % (m_, x, y, z), [M @ v.co for v in kit_objs[m_][0].data.vertices]))
    cbad = CHK.cloth([static_objs[n] for n in ("run_masonry", "run_rockwork", "run_paving", "run_slides") if n in static_objs], rock_extra, cpts)
    for row in cbad:
        print("[check] CLOTH %s: %d points within 3 cm of %s %s at %s" % (row[0], row[1], row[2], row[3], row[4]))
    CLOTH_WORDS = "%d of %d cloth meshes (banners, awnings, the pennants) come within 3 cm of anything that is not cloth, rod, pole or string" % (len(cbad), len(cpts))
    print("[check] CLOTH:", CLOTH_WORDS)
    JPAIRS, jbad = CHK.junctions(SO)
    for row in jbad:
        print("[check] JUNCTION %s should be drawn whole but %s is drawn inside it (%d pieces)" % row)
    JUNCTION_WORDS = "%d junctions of walked tops at one height; %d break the rule (round platform over plaza over deck, the winner drawn whole)" % (len(JPAIRS), len(jbad))
    print("[check] JUNCTIONS:", JUNCTION_WORDS)
    lane_hits, nl = CHK.lane(walk, rock_extra)
    for row in lane_hits[:60]:
        print("[check] LANE %s: art at %s (%s)" % (row[0], row[1], row[2]))
    print("[check] LANE: %d places where art stands in a lane (%d rays hit)" % (len(lane_hits), nl))

if "--junctions" in ARGV and JPAIRS:
    jdir = LOGS / "junctions"
    jdir.mkdir(parents=True, exist_ok=True)
    sc.render.resolution_x, sc.render.resolution_y = 480, 480
    jdone = []
    for k, (wn, ls) in enumerate(JPAIRS):
        X = K.intersect(SO.poly_of(wn), SO.poly_of(ls))
        cx, cy = sum(q[0] for q in X) / len(X), sum(q[1] for q in X) / len(X)
        if wn.shape != "box":                                     # look at the platform's edge where the deck crosses it
            d = Vector((ls.c.x - wn.c.x, ls.c.y - wn.c.y, 0))
            if d.length > 0.5:
                d.normalize(); cx, cy = wn.c.x + d.x * wn.r, wn.c.y + d.y * wn.r
        cd = bpy.data.cameras.new("j%d" % k); co = bpy.data.objects.new("jcam_%d" % k, cd); sc.collection.objects.link(co)
        cd.type, cd.ortho_scale, cd.clip_start, cd.clip_end = "ORTHO", 13.0, 0.2, 200
        co.location = (cx, cy, wn.top + 30.0); co.rotation_euler = (0, 0, 0)
        sc.camera = co
        path = jdir / ("%02d_%s_%s.png" % (k + 1, wn.name, ls.name))
        sc.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        jdone.append(path)
    TW, COLS = 320, 6
    for part, group in enumerate([jdone[i:i + 24] for i in range(0, len(jdone), 24)]):
        rows = (len(group) + COLS - 1) // COLS
        sheet = np.zeros((rows * TW, COLS * TW, 4), dtype=np.float32); sheet[..., 3] = 1.0
        for k, path in enumerate(group):
            img = bpy.data.images.load(str(path))
            px = np.empty(480 * 480 * 4, dtype=np.float32); img.pixels.foreach_get(px)
            px = px.reshape(480, 480, 4)[np.linspace(0, 479, TW).astype(int)][:, np.linspace(0, 479, TW).astype(int)]
            r, c = rows - 1 - k // COLS, k % COLS
            sheet[r * TW:(r + 1) * TW, c * TW:(c + 1) * TW] = px
            bpy.data.images.remove(img)
        out = bpy.data.images.new("jsheet", COLS * TW, rows * TW, alpha=False)
        out.pixels.foreach_set(sheet.reshape(-1))
        out.filepath_raw = str(jdir / ("sheet_%d.png" % (part + 1))); out.file_format = "PNG"; out.save()
    print("[art] %d junction pictures in %s" % (len(jdone), jdir))
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900

if "--audit" in ARGV:
    AUD.run([static_objs[n] for n in ("run_masonry", "run_rockwork", "run_paving", "run_slides", "run_slide_water") if n in static_objs], sc, LOGS,
            cams_only=ARGV[ARGV.index("--legs") + 1].split(",") if "--legs" in ARGV else None, render="--no-audit-render" not in ARGV)
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900

# ================================================================== export
if EXPORT:
    layout_path = ART / "paete_run_layout.json"
    OFF = Vector(W.LEGS[0].A)
    off_from = "the start way point (no gameplay layout yet)"
    if layout_path.exists():
        try:
            lay_json = json.loads(layout_path.read_text(encoding="utf-8"))
            for key in ("blender_offset", "offset_blender", "origin_blender"):
                if key in lay_json:
                    OFF = Vector(lay_json[key]); off_from = "paete_run_layout.json " + key
        except Exception as ex:
            print("[art] could not read the layout:", ex)

    def U(x, y, z):
        return [round(-(x - OFF.x), 4), round(z - OFF.z, 4), round(-(y - OFF.y), 4)]

    def export(objs, name):
        bpy.ops.object.select_all(action="DESELECT")
        for o in objs:
            o.hide_set(False); o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        kw = dict(filepath=str(MODELS / (name + ".glb")), export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
                  export_image_format="NONE", export_materials="EXPORT")
        try:
            bpy.ops.export_scene.gltf(export_vertex_color="ACTIVE", export_all_vertex_colors=False, **kw)
        except TypeError:
            bpy.ops.export_scene.gltf(**kw)

    for n, o in static_objs.items():
        export([o], n)
    for n, (o, c) in kit_objs.items():
        objs = [x for x in c.objects if x.type == "MESH"]
        export(objs, n)
    names = sorted({s.material.name for o in bpy.data.objects if o.type == "MESH" for s in o.material_slots if s.material})
    mats = []
    for n in names:
        tex, tint, tiling, flags = K.MAT.get(n, ("", (1, 1, 1), 1.0, ""))
        row = {"name": n, "texture": tex, "tint": [round(c, 4) for c in tint], "tiling": tiling, "foliage": "f" in flags, "emissive": "e" in flags,
               "glossy": "g" in flags, "cutout": "c" in flags}
        if n in K.SHADER:
            row["shader"] = K.SHADER[n]
        mats.append(row)
    for row in mats:
        if row["name"] == "pr_rock":
            row["note"] = ("Lagoon Cove's rock: use the material/shader Lagoon's own stones use (TumbangPreso/LagoonRock: rock_a triplanar at 4 m a tile, "
                           "edge wear from Art/LagoonCove/Textures/rock_edges_atlas.png through UV2, imported LINEAR). The kit stones here are Lagoon's meshes with their UV2.")
        if row["name"] == "pr_ground":
            row["note"] = ("One ground material. With TumbangPreso/LagoonGround: COLOR_0 = (court, grass, sand, wet), UV2 = (steep, ring), each 0..1; "
                           "field = min + value * span below. Textures sand_a, grass_a, earth_a, rock_a. Without it, plain sand_a still reads.")
            row["fieldMinA"] = [FIELD_RANGE[f][0] for f in FIELD_ORDER_A]; row["fieldSpanA"] = [FIELD_RANGE[f][1] - FIELD_RANGE[f][0] for f in FIELD_ORDER_A]
            row["fieldMinB"] = [FIELD_RANGE[f][0] for f in FIELD_ORDER_B]; row["fieldSpanB"] = [FIELD_RANGE[f][1] - FIELD_RANGE[f][0] for f in FIELD_ORDER_B]
        if row["name"] == "pr_water":
            row["note"] = "The pools' surface: TumbangPreso/LagoonCoveWater serves (it reads depth behind it; the basins are 3 m deep). Flat tint is the fallback."
    (ART / "materials_paete_run.json").write_text(json.dumps({"tint_space": "sRGB 0..1, multiplies the texture", "materials": mats}, indent=1), encoding="utf-8")
    light = -SUN_DIR
    lu = Vector((-light.x, light.z, -light.y))
    art = {
        "note": "Written by tools/author_paete_run_art.py. Unity axes: Blender (x, y, z) is Unity (-x, z, -y) after the offset; yaw t is -t.",
        "offset_blender": [OFF.x, OFF.y, OFF.z], "offset_from": off_from,
        "placements": [{"model": n, "position": U(0, 0, 0), "yaw": 0, "scale": 1.0} for n in static_objs]
        + [{"model": m, "position": U(x, y, z), "yaw": round(-yaw, 3), "scale": round(s, 4)} for (m, x, y, z, yaw, s) in PLACE + ST.PROPS],
        "hide_solids": HIDE,
        "cover_check": COVER_WORDS,
        "coplanar_check": COPLANAR_WORDS,
        "terrain_check": TERRAIN_WORDS,
        "junction_check": JUNCTION_WORDS,
        "cloth_check": CLOTH_WORDS,
        "lines_note": "The start and finish lines are faces of the paving in pr_paint_white (run_paving.glb). Any line Unity draws on top of them (the builder's Marks group) is a second surface and should go.",
        "banners": [{"model": m, "position": U(x, y, z), "yaw": round(-yaw, 3), "scale": round(s, 4)} for (m, x, y, z, yaw, s) in ST.BANNERS],
        "banners_note": ("STATIC. Each banner is ONE mesh (banner_<colour>_<a..d>.glb): a cloth simulation settled under gravity and a light side wind and baked, 1.6 cm thick, "
                         "its trim a band of the same faces. One per rod. They are also in `placements`; this list only names them. Do not animate them and do not add others. "
                         "The awnings' canopies (awning_cloth_*.glb) are made the same way."),
        "slides": CHU.INFO,
        "water": ("run_pool_water.glb: one flat sheet per layout pool (all four, pool_2_rocks too), in pr_water, 4 cm UNDER the layout's surface, so a sheet Unity lays at the surface never shares its plane. "
                  "run_slide_water.glb: the sloping sheet on each slide's bed and the fan it runs out in, in pr_water_running, 5 mm over the collision bed, UV v running downstream in units of 4.6 m."),
        "sun": [round(math.degrees(math.atan2(lu.x, lu.z)), 2), round(math.degrees(math.asin(SUN_DIR.z)), 2)],
        "look": ("Late morning in a dry bowl, the sun from the south-east at about 40 degrees raking the stepped face: warm key (1.0, 0.89, 0.70), "
                 "clear deep-blue sky fill, soft long shadows under the arches and in the chasms. Warm sand-coloured distance fog starting about 150 m, "
                 "thick by 600 m, so the far rock stacks sit pale in the haze. No bloom on the rock; the capiz lanterns and the water may glow a little. "
                 "Grade toward warm highlights and teal-leaning shadows, as Lagoon Cove's look row, a touch drier."),
        "notes": ST.NOTES,
    }
    (ART / "paete_run_art.json").write_text(json.dumps(art, indent=1), encoding="utf-8")
    print("[art] exported %d models, %d materials, %d placements; offset from %s" % (len(static_objs) + len(kit_objs), len(mats), len(art["placements"]), off_from))

# The kit's originals leave the scene (the placed copies are instances and stay): they all sit at the origin.
sc.collection.children.unlink(C_KIT)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "paete_run_art.blend"), compress=True)
if REVIEW:
    done = []
    sc.render.resolution_x, sc.render.resolution_y = (1280, 720) if "--small" in ARGV else (1600, 900)
    for name in CAMS:
        if ONLY and not any(name == o or name.startswith(o) for o in ONLY):
            continue
        sc.camera = bpy.data.objects["cam_" + name]
        path = LOGS / ("paete_run_art_%s_v%d.png" % (name, REVIEW))
        sc.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        done.append((name, path))
    # contact sheets: the pictures small, 4 across, named
    TW, TH, COLS = 640, 360, 4
    for part, group in enumerate([done[i:i + 16] for i in range(0, len(done), 16)]):
        rows = (len(group) + COLS - 1) // COLS
        sheet = np.zeros((rows * TH, COLS * TW, 4), dtype=np.float32); sheet[..., 3] = 1.0
        for k, (name, path) in enumerate(group):
            img = bpy.data.images.load(str(path))
            w, h = img.size
            px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px)
            px = px.reshape(h, w, 4)[np.linspace(0, h - 1, TH).astype(int)][:, np.linspace(0, w - 1, TW).astype(int)]
            r, c = rows - 1 - k // COLS, k % COLS
            sheet[r * TH:(r + 1) * TH, c * TW:(c + 1) * TW] = px
            bpy.data.images.remove(img)
        out = bpy.data.images.new("sheet", COLS * TW, rows * TH, alpha=False)
        out.pixels.foreach_set(sheet.reshape(-1))
        out.filepath_raw = str(LOGS / ("paete_run_art_sheet_v%d_%d.png" % (REVIEW, part + 1))); out.file_format = "PNG"; out.save()
        print("[art] contact sheet", out.filepath_raw, [n for n, _p in group])
print("ART_OK")
