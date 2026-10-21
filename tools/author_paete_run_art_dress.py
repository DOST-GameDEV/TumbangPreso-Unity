"""PAETE'S RUN, THE DRESSING: a festival race through a ruin the forest is taking back (2026-10-10).

Owner, of the first art: "the map still looks unlively and bare".

WHAT THIS ADDS, all of it OFF the racing line (`put` and `free` ask the world: nothing stands in a lane, under a jump or
a swing, on a walked solid or in a pool) and none of it with collision:
  * THE FESTIVAL: poles with bunting across and along every run (some lines torn), banner poles, a numbered sign with an
    arrow at the start of every stage, stalls and awnings, benches, drums, a scoreboard at the start, a podium and
    flag masts at the finish, lanterns, jars, crates, carts;
  * THE WORKS: cranes with hanging blocks, scaffolds, ladders, coils of rope: the ruin was being mended for the race;
  * THE RUIN: broken statues, fallen column drums, blocks, scree at the foot of every big boulder;
  * THE FOREST: Lagoon Cove's own palms, bushes in flower and banana plants, Kanto's trees, dry grass, reeds and lilies
    at the pools, creepers up the columns;
  * THE SKYLINE: a ruined aqueduct and a far wall with towers on the bowl's rim, pale in the haze.
Approved models of the other maps are brought through whole (`BORROW`: model, the map it is from, the rules that give
its materials this map's names); everything else is built here.
"""
import math
import random

from mathutils import Matrix, Vector

import author_paete_run_art_kit as K
import author_paete_run_art_world as W
import author_paete_run_art_stages as ST
import author_paete_run_art_solids as SO

UP = Vector((0, 0, 1))
T, frame = K.T, K.frame
FAR = K.Buf("run_far_ruins", 61)
STATIC = [(FAR, 0.0, "run_far_ruins")]
COUNT = {}

PALM = (("palm_trunk", "pr_palm_trunk"), ("frond_broad_palm_light", "pr_frond_light"), ("frond_broad_palm_dark", "pr_frond_dark"), ("coconut", "pr_coconut"), ("frond_palm", "pr_frond_small"))
BUSH = (("bush_light", "pr_leaf_light"), ("bush_dark", "pr_leaf_dark"), ("gumamela", "pr_flower_red"), ("bougainvillea", "pr_flower_pink"))
BANANA = (("banana_stem", "pr_banana_stem"), ("banana_light", "pr_paddle_light"), ("banana_dark", "pr_paddle_dark"), ("stalk", "pr_vine"))
WOOD = (("pc_clay", "pr_clay"), ("pg_slat", "pr_slat"), ("bamboo", "pr_bamboo"), ("thatch", "pr_thatch"), ("sawali", "pr_sawali"), ("plank", "pr_plank"), ("timber", "pr_timber"))
CART = (("planks", "pr_plank"), ("teal", "pr_paint_teal"), ("wood_dark", "pr_timber"), ("rubber", "pr_paint_black"), ("metal_dark", "pr_iron"), ("red", "pr_paint_red"),
        ("tan", "pr_plaster"), ("cream", "pr_paint_white"), ("yellow", "pr_paint_yellow"))
BORROW = [("plant_palm_a", "LagoonCove", "plant_palm_0", PALM), ("plant_palm_b", "LagoonCove", "plant_palm_2", PALM), ("plant_palm_c", "LagoonCove", "plant_palm_3", PALM),
          ("plant_palm_d", "LagoonCove", "plant_palm_5", PALM), ("plant_bush_a", "LagoonCove", "plant_bush_0", BUSH), ("plant_bush_b", "LagoonCove", "plant_bush_1", BUSH),
          ("plant_bush_c", "LagoonCove", "plant_bush_2", BUSH), ("plant_bush_d", "LagoonCove", "plant_bush_3", BUSH), ("plant_banana_a", "LagoonCove", "plant_broadleaf_0", BANANA),
          ("plant_banana_b", "LagoonCove", "plant_broadleaf_2", BANANA), ("prop_banga_a", "LagoonCove", "pc_banga_2", WOOD), ("prop_banga_b", "LagoonCove", "pc_banga_3", WOOD),
          ("prop_crate_a", "LagoonCove", "pg_crate_2", WOOD), ("prop_crate_b", "LagoonCove", "pg_crate_4", WOOD), ("prop_bench", "LagoonCove", "pt_bench_1", WOOD),
          ("prop_lean_to", "LagoonCove", "pt_lean_to_1", WOOD), ("prop_stall", "LagoonCove", "house_stall_1000", WOOD), ("prop_cart", "EskinitaAlley", "prop_kariton", CART)]


# ------------------------------------------------------------------ this map's own small models
def sim_cloth(name, verts, faces, uvs, face_mat, mats, pins, wind, seed, frames=42, gravity=1.0):
    """ONE cloth mesh, its shape from a real cloth simulation: pinned where it is tied, settled under gravity and a light
    side wind, ONE frame baked, given 1.6 cm of thickness (so it has two sides). Its trim is a band of the SAME faces."""
    import bpy
    sc = bpy.context.scene
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    layer = me.uv_layers.new(name="UVMap")
    layer.data.foreach_set("uv", [c for f in faces for i in f for c in uvs[i]])
    for mt in mats:
        me.materials.append(K.material(mt))
    me.polygons.foreach_set("material_index", face_mat)
    me.polygons.foreach_set("use_smooth", [True] * len(faces))
    me.update()
    o = bpy.data.objects.new(name, me)
    c = bpy.data.collections.new("kit_" + name)
    sc.collection.children.link(c)
    c.objects.link(o)
    o.vertex_groups.new(name="pin").add(list(pins), 1.0, "REPLACE")
    mod = o.modifiers.new("Cloth", "CLOTH")
    st = mod.settings
    st.vertex_group_mass, st.quality, st.mass, st.air_damping = "pin", 8, 0.3, 1.5
    st.tension_stiffness, st.compression_stiffness, st.shear_stiffness, st.bending_stiffness = 18, 18, 6, 0.6
    st.effector_weights.gravity = gravity
    mod.point_cache.frame_start, mod.point_cache.frame_end = 1, frames
    bpy.ops.object.effector_add(type="WIND")
    e = bpy.context.active_object
    e.field.type, e.field.strength, e.field.noise, e.field.seed = "WIND", wind[1], 1.5, seed
    e.rotation_euler = Vector(wind[0]).to_track_quat("Z", "Y").to_euler()
    for f in range(1, frames + 1):
        sc.frame_set(f)
    for step in ("cloth", "solid"):
        if step == "solid":
            sol = o.modifiers.new("Solid", "SOLIDIFY")
            sol.thickness, sol.offset = 0.016, 0.0
        dg = bpy.context.evaluated_depsgraph_get()
        me2 = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
        o.modifiers.clear()
        o.data = me2
    bpy.data.objects.remove(e)
    sc.frame_set(1)
    o.data.name = name
    return o, c


def cloth_models():
    """The hanging cloth, simulated BEFORE anything else is in the scene: banners (four settled shapes a colour, so no two
    neighbours match) and the awnings' canopies. Returns {model: (object, its collection)}."""
    out = {}
    W_, H_, NX, NY = 1.1, 2.7, 8, 12
    for col, (main, trim) in (("red", ("pr_cloth_red", "pr_cloth_yellow")), ("teal", ("pr_cloth_teal", "pr_cloth_white")), ("yellow", ("pr_cloth_yellow", "pr_cloth_red"))):
        for vi, (wd, ws) in enumerate((((0.3, 1.0, 0.0), 2.0), ((-0.6, -1.0, 0.1), 3.0), ((0.9, 0.5, 0.0), 1.5), ((-0.3, 1.0, -0.1), 3.5))):      # AT REST: a breath of air, no more
            verts, uvs = [], []
            for j in range(NY + 1):
                for i in range(NX + 1):
                    x = -W_ / 2 + W_ * i / NX
                    drop = H_ - 0.5 * (1 - abs(x) / (W_ / 2))        # a swallow tail: the foot is cut to a V
                    # the head is gathered on its rod (a little narrower than the cloth), so it falls in vertical folds
                    verts.append((x * (0.86 + 0.14 * min(1.0, j / 3.0)) if j < 3 else x, 0.012 * math.sin(i * 2.1 + vi * 1.7) * min(1.0, j / 2.0), -drop * j / NY)); uvs.append((i / NX, 1 - j / NY))
            faces = [(j * (NX + 1) + i, (j + 1) * (NX + 1) + i, (j + 1) * (NX + 1) + i + 1, j * (NX + 1) + i + 1) for j in range(NY) for i in range(NX)]
            fm = [1 if j in (2,) or (j == NY - 2) else 0 for j in range(NY) for i in range(NX)]
            name = "banner_%s_%s" % (col, "abcd"[vi])
            out[name] = sim_cloth(name, verts, faces, uvs, fm, (main, trim), range(NX + 1), (wd, ws), 11 + vi * 7 + len(out))
    AX, AY = 12, 8
    for col, (a, b) in (("red", ("pr_cloth_red", "pr_cloth_white")), ("teal", ("pr_cloth_teal", "pr_cloth_yellow"))):
        verts, uvs = [], []
        for j in range(AY + 1):
            for i in range(AX + 1):
                verts.append((-1.75 + 3.5 * i / AX, -1.15 + 2.75 * j / AY, 2.92 - 0.62 * j / AY)); uvs.append((i / AX, j / AY))
        faces = [(j * (AX + 1) + i, j * (AX + 1) + i + 1, (j + 1) * (AX + 1) + i + 1, (j + 1) * (AX + 1) + i) for j in range(AY - 0) for i in range(AX)]
        fm = [(i // 2) % 2 for j in range(AY) for i in range(AX)]
        pins = [i for i in range(AX + 1)] + [(AY - 1) * (AX + 1) + i for i in range(AX + 1)]        # tied along the back rail and the front rail: the last row hangs as the valance
        name = "awning_cloth_%s" % col
        out[name] = sim_cloth(name, verts, faces, uvs, fm, (a, b), pins, ((0.3, 1.0, 0.0), 4), 5 + len(out), frames=36)
    return out


def prop_banner_pole(Q, seed=0):
    """The pole and its yard only: the cloth is a model of its own (cloth_models), one to a pole."""
    Q.m.tube([(0, 0, -0.4), (0.04, 0, 3.0), (0.0, 0, 5.4)], 0.085, "pr_timber", n=6, r1=0.06)
    Q.m.tube([(-0.72, 0, 5.0), (0.72, 0, 5.0)], 0.045, "pr_timber", n=5)
    Q.m.lathe(T(0, 0, 0), [(0.05, 5.38), (0.13, 5.5), (0.04, 5.66)], 6, "pr_bronze", cap_top="pr_bronze")
    Q.m.lathe(T(0, 0, 0), [(0.3, -0.3), (0.22, 0.22)], 8, "pr_stone_worn", cap_top="pr_stone_worn")


def prop_crane(Q, seed=0):
    """A builder's crane: a mast in a stone foot, a braced jib, a block of stone hanging on its rope."""
    Q.m.block(T(0, 0, 0), 0.7, 0.7, -0.4, 0.5, "pr_ashlar_dark", taper=0.85)
    Q.m.tube([(0, 0, 0.3), (0, 0, 6.6)], 0.17, "pr_timber", n=7, r1=0.14)
    Q.m.tube([(0, 0, 5.9), (3.6, 0, 7.3)], 0.13, "pr_timber", n=6, r1=0.1)
    Q.m.tube([(0, 0, 3.6), (2.2, 0, 6.72)], 0.09, "pr_timber", n=5)
    Q.m.tube([(0, 0, 6.5), (-1.5, 0, 5.2), (-1.7, 0, 0.3)], 0.035, "pr_rope", n=4)
    Q.r.tube([(3.5, 0, 7.2), (3.5, 0, 3.1)], 0.035, "pr_rope", n=4)
    Q.m.block(T(3.5, 0, 0, 0.5), 0.5, 0.42, 2.2, 3.0, "pr_ashlar")
    Q.r.tube([(3.5 + 0.56 * math.cos(a), 0.47 * math.sin(a), 2.62) for a in [math.tau * k / 8 + 0.5 for k in range(8)]], 0.035, "pr_rope", n=4, closed=True)
    Q.m.lathe(T(3.5, 0, 0) @ Matrix.Rotation(math.pi / 2, 4, "X"), [(0.2, -0.08), (0.2, 0.08)], 8, "pr_iron", cap_top="pr_iron", cap_bot="pr_iron")


def prop_scaffold(Q, seed=0):
    """Bamboo-thick poles lashed into a two-stage tower, planks on each stage."""
    rng = random.Random(seed)
    for sx in (-1, 1):
        for sy in (-1, 1):
            Q.m.tube([(sx * 1.1, sy * 0.8, -0.3), (sx * (1.05 + rng.uniform(-0.05, 0.05)), sy * 0.78, 5.0 + rng.uniform(-0.3, 0.4))], 0.075, "pr_bamboo", n=6)
    for z in (1.9, 3.9):
        for sy in (-1, 1):
            Q.m.tube([(-1.35, sy * 0.8, z), (1.35, sy * 0.8, z + rng.uniform(-0.04, 0.04))], 0.06, "pr_bamboo", n=5)
        for k in range(3):
            Q.m.block(T(-0.75 + k * 0.75, 0, 0, rng.uniform(-0.03, 0.03)), 0.35, 1.0, z + 0.07, z + 0.13 + 0.004 * k, "pr_plank")
    Q.m.tube([(-1.1, -0.8, 0.1), (1.1, -0.8, 1.9)], 0.05, "pr_bamboo", n=5)
    Q.m.tube([(1.1, 0.8, 2.0), (-1.1, 0.8, 3.9)], 0.05, "pr_bamboo", n=5)


def prop_ladder(Q, seed=0):
    for sx in (-1, 1):
        Q.m.tube([(sx * 0.3, 0, -0.1), (sx * 0.27, 1.1, 4.2)], 0.05, "pr_timber", n=5)
    for k in range(7):
        t = (k + 0.7) / 7.6
        Q.m.tube([(-0.32, 1.1 * t, -0.1 + 4.3 * t), (0.32, 1.1 * t, -0.1 + 4.3 * t)], 0.04, "pr_timber", n=5)


def prop_drum(Q, seed=0):
    """A festival drum on a trestle, a painted band, two beaters."""
    for sx in (-1, 1):
        Q.m.tube([(sx * 0.5, -0.35, -0.1), (sx * 0.4, 0, 0.75), (sx * 0.5, 0.35, -0.1)], 0.055, "pr_timber", n=5)
    rot = Matrix.Rotation(math.pi / 2, 4, "Y")
    Q.m.lathe(T(0, 0, 1.15) @ rot, [(0.5, -0.5), (0.62, -0.2), (0.62, 0.2), (0.5, 0.5)], 12, "pr_timber", cap_top="pr_plaster", cap_bot="pr_plaster")
    Q.m.lathe(T(0, 0, 1.15) @ rot, [(0.632, -0.09), (0.632, 0.09)], 12, "pr_paint_red")
    Q.m.tube([(0.75, 0.1, 0.0), (0.82, 0.2, 0.7)], 0.03, "pr_timber", n=4)
    Q.m.lathe(T(0.82, 0.2, 0.7), [(0.03, -0.02), (0.085, 0.05), (0.03, 0.13)], 6, "pr_cloth_white", cap_top="pr_cloth_white")


def prop_statue(Q, seed=0):
    """What is left of a guardian: a plinth, two great feet and shins, the head lying beside it."""
    Q.m.block(T(0, 0, 0), 1.2, 0.95, -0.4, 0.7, "pr_ashlar")
    Q.m.block(T(0, 0, 0), 1.32, 1.06, 0.62, 0.82, "pr_stone")
    for sx in (-1, 1):
        Q.m.block(T(sx * 0.5, 0.15, 0), 0.34, 0.55, 0.8, 1.08, "pr_stone_worn", taper=0.9)
        Q.m.lathe(T(sx * 0.5, -0.12, 0), [(0.3, 1.0), (0.26, 1.6), (0.3, 2.1 + 0.5 * (sx > 0)), (0.18, 2.2 + 0.5 * (sx > 0))], 8, "pr_stone_worn", cap_top="pr_stone")
    Q.m.lathe(T(2.1, 0.5, 0.25) @ Matrix.Rotation(1.25, 4, "Y") @ Matrix.Rotation(0.5, 4, "Z"), [(0.3, -0.5), (0.55, -0.3), (0.6, 0.15), (0.45, 0.5), (0.2, 0.62)], 9, "pr_stone", cap_top="pr_stone_worn", cap_bot="pr_stone_worn")
    Q.m.block(T(2.25, 0.5, 0.0, 0.5) @ Matrix.Rotation(1.25, 4, "Y"), 0.12, 0.1, 0.5, 0.74, "pr_stone_worn")      # its nose


def prop_drum_fallen(Q, seed=0):
    rng = random.Random(seed)
    for k in range(3):
        m = T(k * 1.25 + rng.uniform(-0.1, 0.1), rng.uniform(-0.25, 0.25) + 0.3 * k, 0.45 - 0.08 * k, rng.uniform(-0.4, 0.4)) @ Matrix.Rotation(math.pi / 2 + rng.uniform(-0.12, 0.12), 4, "Y")
        Q.m.lathe(m, [(0.56, -0.5), (0.56, 0.5)], 12, "pr_stone" if k != 1 else "pr_stone_worn", cap_top="pr_stone_worn", cap_bot="pr_stone_worn", phase=rng.uniform(0, 1))


def prop_scree(Q, seed=0):
    """Loose stones where a boulder meets the sand."""
    rng = random.Random(seed)
    for k in range(7):
        a = math.tau * k / 7 + rng.uniform(-0.3, 0.3)
        d = rng.uniform(0.2, 1.5)
        r = rng.uniform(0.22, 0.55) * (1.3 if k == 0 else 1.0)
        m = T(math.cos(a) * d, math.sin(a) * d, -0.08, rng.uniform(0, 6)) @ Matrix.Rotation(rng.uniform(-0.25, 0.25), 4, "X")
        Q.r.lathe(m, [(r * 0.9, -0.1), (r, r * 0.25), (r * 0.62, r * 0.62)], 5, "pr_rock", cap_top="pr_rock", squash=rng.uniform(0.65, 1.0), smooth=False)


def prop_awning(Q, seed=0):
    """Four poles and two rails, a counter with jars. Its canopy is a simulated cloth model of its own (awning_cloth_*)."""
    for sx in (-1, 1):
        Q.m.tube([(sx * 1.6, -1.15, -0.2), (sx * 1.6, -1.15, 3.05)], 0.06, "pr_timber", n=5)
        Q.m.tube([(sx * 1.6, 1.26, -0.2), (sx * 1.6, 1.26, 2.5)], 0.06, "pr_timber", n=5)
    Q.m.tube([(-1.8, -1.15, 2.9), (1.8, -1.15, 2.9)], 0.045, "pr_timber", n=5)
    Q.m.tube([(-1.8, 1.26, 2.36), (1.8, 1.26, 2.36)], 0.045, "pr_timber", n=5)
    Q.m.block(T(0, -0.2, 0), 1.3, 0.38, 0.0, 0.92, "pr_plank")
    Q.m.block(T(0, -0.2, 0), 1.4, 0.46, 0.9, 0.99, "pr_timber")
    for k, x in enumerate((-0.9, -0.3, 0.45, 1.0)):
        Q.m.lathe(T(x, -0.2, 0.98), [(0.1, 0.0), (0.2, 0.12), (0.2, 0.3), (0.1, 0.42), (0.13, 0.5)], 8, "pr_clay" if k % 2 else "pr_paint_teal", cap_top="pr_paint_black")


def prop_scoreboard(Q, seed=0):
    """A timber frame with painted slats: the race's board."""
    for sx in (-1, 1):
        Q.m.tube([(sx * 1.7, 0, -0.3), (sx * 1.7, 0, 4.3)], 0.11, "pr_timber", n=6)
    Q.m.block(T(0, 0, 0), 1.95, 0.1, 3.95, 4.2, "pr_timber")
    cols = ["pr_paint_red", "pr_paint_yellow", "pr_paint_teal", "pr_paint_white"]
    for k in range(4):
        Q.m.block(T(0, 0, 0), 1.6, 0.05, 1.5 + k * 0.6, 2.0 + k * 0.6, "pr_plank")
        Q.m.block(T(-1.25, -0.05, 0), 0.22, 0.03, 1.56 + k * 0.6, 1.94 + k * 0.6, cols[k])
        Q.m.block(T(0.2 + 0.1 * (k % 2), -0.05, 0), 0.9 - 0.2 * (k % 3), 0.025, 1.68 + k * 0.6, 1.82 + k * 0.6, "pr_paint_black")
    Q.m.lathe(T(0, 0, 0) @ Matrix.Rotation(math.pi / 2, 4, "X"), [(0.42, -0.06), (0.42, 0.06)], 10, "pr_bronze", cap_top="pr_bronze", cap_bot="pr_bronze")


def plant_reeds(Q, seed=0):
    rng = random.Random(seed)
    for k in range(11):
        a = math.tau * k / 11 + rng.uniform(-0.3, 0.3)
        d = rng.uniform(0.05, 0.4)
        out = Vector((math.cos(a), math.sin(a), 0))
        h = rng.uniform(1.3, 2.3)
        Q.c.card(out * d - UP * 0.1, Vector((-out.y, out.x, 0)) * 0.1, UP * h + out * (h * rng.uniform(0.05, 0.22)), "pr_leaf_dark" if k % 3 else "pr_grass_light")


def plant_lily(Q, seed=0):
    rng = random.Random(seed)
    for k in range(4):
        a = math.tau * k / 4 + rng.uniform(-0.4, 0.4)
        d = rng.uniform(0.25, 0.75) if k else 0.0
        r = rng.uniform(0.3, 0.45)
        c = Vector((math.cos(a) * d, math.sin(a) * d, 0.012 + 0.003 * k))
        u, v = Vector((math.cos(a + 0.6), math.sin(a + 0.6), 0)) * r, Vector((-math.sin(a + 0.6), math.cos(a + 0.6), 0)) * r
        Q.c.quad([c - u - v, c + u - v, c + u + v, c - u + v], "pr_leaf_light" if k % 2 else "pr_leaf_dark", uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    Q.m.lathe(T(0.12, 0.1, 0), [(0.04, 0.02), (0.14, 0.1), (0.05, 0.22)], 6, "pr_paint_white", cap_top="pr_paint_yellow")


def plant_flower(Q, seed=0, mat="pr_paint_red"):
    rng = random.Random(seed)
    ST.plant_shrub(Q, seed)
    for k in range(9):
        a, e = rng.uniform(0, 6.28), rng.uniform(0.3, 1.2)
        c = Vector((math.cos(a) * math.cos(e) * 0.85, math.sin(a) * math.cos(e) * 0.85, 0.55 + math.sin(e) * 0.7))
        n = Vector((math.cos(a), math.sin(a), 0.5)).normalized()
        u = n.cross(UP).normalized() * 0.11; v = n.cross(u).normalized() * 0.11
        Q.c.quad([c - u, c - v, c + u, c + v], mat, uvs=[(0, 0), (1, 0), (1, 1), (0, 1)]); Q.c.quad([c + v, c + u, c - v, c - u], mat, uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])


ST.PROP_FN.update({
    "prop_banner_pole": prop_banner_pole, "prop_crane": prop_crane, "prop_scaffold": prop_scaffold, "prop_ladder": prop_ladder,
    "prop_drum": prop_drum, "prop_statue": prop_statue, "prop_drum_fallen": prop_drum_fallen, "prop_scree_a": lambda Q, seed=0: prop_scree(Q, 3), "prop_scree_b": lambda Q, seed=0: prop_scree(Q, 8),
    "prop_awning": prop_awning, "prop_scoreboard": prop_scoreboard,
    "plant_reeds_a": lambda Q, seed=0: plant_reeds(Q, 4), "plant_reeds_b": lambda Q, seed=0: plant_reeds(Q, 9), "plant_lily": plant_lily,
    "plant_flower_a": lambda Q, seed=0: plant_flower(Q, 2, "pr_paint_red"), "plant_flower_b": lambda Q, seed=0: plant_flower(Q, 7, "pr_paint_yellow"),
})


# ------------------------------------------------------------------ placing
ROCKS_AT = []


def keep_out():
    """The set pieces that are not layout solids (the two gates, the summit's rotunda): nothing is stood in them."""
    a, b = W.LEGS[0], W.LEGS[-1]
    ga, gb = a.A + a.f * 1.5, b.C - b.f * 6.2
    return [(36.0, 204.0, 10.5), (ga.x, ga.y, 8.5), (gb.x, gb.y, 8.5)]


def free(x, y, z0, z1, r, margin=0.35):
    if not (W.X0 + 12 < x < W.X1 - 12 and W.Y0 + 12 < y < W.Y1 - 12):
        return False
    if any(math.hypot(x - kx, y - ky) < kr + r for (kx, ky, kr) in keep_out()):
        return False
    return not W.blocked(x, y, z0, z1, r, margin=margin)


def put(model, x, y, rng, yaw=None, s=1.0, r=0.6, h=2.6, sink=0.05, rocks=True, slope=0.6, z=None):
    g = W.ground(x, y) if z is None else z
    if z is None and (W.slope(x, y) > slope or not free(x, y, g, g + h, r)):
        return False
    if rocks and any(math.hypot(px - x, py - y) < pr * 0.8 + r * 0.5 for (px, py, pr) in ROCKS_AT):
        return False
    yaw = rng.uniform(0, 360) if yaw is None else yaw
    if model.startswith("prop_banner_"):                          # a pole, and exactly one cloth on it
        col = model[len("prop_banner_"):]
        row = ("banner_%s_%s" % (col, "abcd"[ST._BV[0] % 4]), x, y, g - sink + 5.0, yaw, 1.0)
        ST._BV[0] += 1
        ST.PROPS.append(("prop_banner_pole", x, y, g - sink, yaw, 1.0)); ST.PROPS.append(row); ST.BANNERS.append(row)
    elif model.startswith("prop_awning_"):
        ST.PROPS.append(("prop_awning", x, y, g - sink, yaw, s)); ST.PROPS.append(("awning_cloth_" + model[len("prop_awning_"):], x, y, g - sink, yaw, s))
    else:
        ST.PROPS.append((model, x, y, g - sink, yaw, s))
    COUNT[model] = COUNT.get(model, 0) + 1
    return True


def face(d):
    """The yaw (degrees) that turns a model's -Y (its front) along d."""
    return math.degrees(math.atan2(d.x, -d.y))


def pole(x, y, h, rng, own=None):
    g = W.ground(x, y)
    if W.blocked(x, y, g, g + h, 0.2, own, margin=0.25) or any(math.hypot(px - x, py - y) < pr * 0.7 for (px, py, pr) in ROCKS_AT):
        return None
    ST.S.m.tube([(x, y, g - 0.5), (x + rng.uniform(-0.05, 0.05), y + rng.uniform(-0.05, 0.05), g + h)], 0.085, "pr_timber", n=6, r1=0.06)
    ST.S.m.lathe(T(x, y, 0), [(0.32, g - 0.3), (0.24, g + 0.25)], 8, "pr_stone_worn", cap_top="pr_stone_worn")
    COUNT["pole"] = COUNT.get("pole", 0) + 1
    return Vector((x, y, g + h - 0.12))


DIGITS = {"0": [[(0, 0), (2, 0), (2, 4), (0, 4), (0, 0)]], "1": [[(1, 0), (1, 4), (0.3, 3.2)]], "2": [[(0, 4), (2, 4), (2, 2), (0, 2), (0, 0), (2, 0)]],
          "3": [[(0, 4), (2, 4), (2, 0), (0, 0)], [(0.5, 2), (2, 2)]], "4": [[(0, 4), (0, 2), (2, 2)], [(2, 4), (2, 0)]], "5": [[(2, 4), (0, 4), (0, 2), (2, 2), (2, 0), (0, 0)]],
          "6": [[(2, 4), (0, 4), (0, 0), (2, 0), (2, 2), (0, 2)]], "7": [[(0, 4), (2, 4), (2, 0)]], "8": [[(0, 0), (2, 0), (2, 4), (0, 4), (0, 0)], [(0, 2), (2, 2)]],
          "9": [[(2, 2), (0, 2), (0, 4), (2, 4), (2, 0), (0, 0)]], ">": [[(0, 2), (2.6, 2)], [(1.5, 3.3), (2.7, 2), (1.5, 0.7)]]}


def paint(text, origin, right, out, size, mat):
    """Thick painted strokes on a wall: `origin` is the text's bottom left, `out` the wall's outward normal."""
    right, out = Vector(right).normalized(), Vector(out).normalized()
    u = size / 4.0
    x0, k = 0.0, 0
    for ch in text:
        for line in DIGITS.get(ch, []):
            for a, b in zip(line, line[1:]):
                pa = Vector(origin) + right * ((x0 + a[0]) * u) + UP * (a[1] * u)
                pb = Vector(origin) + right * ((x0 + b[0]) * u) + UP * (b[1] * u)
                d = (pb - pa); ln = d.length; d.normalize()
                m = Matrix((d, out.cross(d), out)).transposed().to_4x4()
                m.translation = (pa + pb) / 2
                ST.S.m.block(m, ln / 2 + u * 0.12, u * 0.24, -0.02 - 0.003 * (k % 5), 0.022 + 0.004 * (k % 5), mat)
                k += 1
        x0 += 3.0 if ch != ">" else 3.6


def sign(leg, rng):
    """A numbered board with an arrow, on two posts, at the start of a stage: it faces the racer coming to it."""
    for sd in (-1, 1):
        q = leg.A + leg.s * (sd * (leg.half + 2.2)) + leg.f * 2.5
        g = W.ground(q.x, q.y)
        if abs(g - leg.A.z) > 2.2 or W.blocked(q.x, q.y, g, g + 3.4, 0.45, margin=0.2) or any(math.hypot(px - q.x, py - q.y) < pr * 0.8 for (px, py, pr) in ROCKS_AT):
            continue
        back = -leg.f
        right = Vector((-back.y, back.x, 0))
        m = frame(Vector((q.x, q.y, 0)), right)
        for e in (-1, 1):
            ST.S.m.tube([Vector((q.x, q.y, g - 0.4)) + right * (e * 0.8), Vector((q.x, q.y, g + 3.2)) + right * (e * 0.8)], 0.07, "pr_timber", n=5)
        ST.S.m.block(m, 1.0, 0.05, g + 1.75, g + 3.0, "pr_plank")
        ST.S.m.block(m, 1.08, 0.07, g + 2.98, g + 3.1, ["pr_paint_red", "pr_paint_teal", "pr_paint_yellow"][leg.i % 3])
        txt = str(leg.i)
        face_ = Vector((q.x, q.y, g + 2.05)) + back * 0.05
        if sd < 0:                                                # the board stands left of the lane: number, then an arrow pointing in
            paint(txt, face_ - right * 0.86, right, back, 0.7, "pr_paint_white" if leg.i % 2 else "pr_paint_yellow")
            paint(">", face_ + right * 0.3 + UP * 0.1, right, back, 0.5, "pr_paint_red")
        else:
            paint(txt, face_ - right * (0.02 if len(txt) > 1 else -0.25), right, back, 0.7, "pr_paint_white" if leg.i % 2 else "pr_paint_yellow")
            paint(">", face_ - right * 0.3 + UP * 0.1, -right, back, 0.5, "pr_paint_red")
        COUNT["stage sign"] = COUNT.get("stage sign", 0) + 1
        return True
    return False


def festoon(leg, rng):
    """Poles down both sides of a run, bunting across the lane between each pair and along one side: some lines torn."""
    if leg.flat < 9:
        return
    n = max(1, int(leg.flat / 11.0))
    last = {}
    for k in range(n + 1):
        al = 1.5 + (leg.flat - 3.0) * k / n
        if any(math.hypot(leg.A.x + leg.f.x * al - kx, leg.A.y + leg.f.y * al - ky) < kr + 1.0 for (kx, ky, kr) in keep_out()):
            last = {}                                             # no pole, and no line, through a gate or the rotunda
            continue
        tops = {}
        for sd in (-1, 1):
            q = leg.A + leg.f * al + leg.s * (sd * (leg.half + 1.25))
            if abs(W.ground(q.x, q.y) - leg.A.z) < 2.5:
                t = pole(q.x, q.y, leg.A.z + 6.2 - W.ground(q.x, q.y), rng, own=leg.i)
                if t is not None:
                    tops[sd] = t
        if len(tops) == 2:
            if rng.random() < 0.22:                               # a torn line: it hangs from one pole
                a = tops[1]
                ST.bunting(a, a + (tops[-1] - a) * 0.3 - UP * 3.2, 0.3, rng)
            else:
                ST.bunting(tops[-1], tops[1], 1.25, rng)
            COUNT["bunting line"] = COUNT.get("bunting line", 0) + 1
        for sd, t in tops.items():
            if sd in last and (k + (sd > 0)) % 2 == 0:
                ST.bunting(last[sd], t, 0.9, rng); COUNT["bunting line"] = COUNT.get("bunting line", 0) + 1
        last = tops


def creeper(x, y, z0, z1, r, rng, turns=1.4):
    pts = [Vector((x + (r + 0.05) * math.cos(a0), y + (r + 0.05) * math.sin(a0), z)) for a0, z in
           [(rng.uniform(0, 6) * 0 + math.tau * turns * k / 9 + x, z0 + (z1 - z0) * k / 9) for k in range(10)]]
    ST.vine(ST.S.c, pts, rng, 0.5, 0.05, 0.36)
    COUNT["creeper"] = COUNT.get("creeper", 0) + 1


def skyline(HH, hx, hy, HC, rng):
    """The bowl's rim: a ruined aqueduct marching along one arc, a wall with towers on another. Flat and pale: far things."""
    def gz(x, y):
        i, j = int((x - hx[0]) / HC), int((y - hy[0]) / HC)
        return float(HH[min(max(i, 0), len(hx) - 1), min(max(j, 0), len(hy) - 1)])
    cx, cy = W.CENTRE
    prev = None
    n = 0
    for k in range(34):                                           # the aqueduct: north to west
        a = math.radians(40 + k * 4.4)
        R = 300 + 22 * math.sin(k * 0.7)
        x, y = cx + math.cos(a) * R * 0.92, cy + math.sin(a) * R
        g = gz(x, y)
        top = 62.0
        gone = k in (7, 8, 15, 22, 23, 24, 30)
        if not gone:
            FAR.block(T(x, y, 0, a + math.pi / 2), 3.4, 2.6, g - 6, top - (0 if prev else rng.uniform(4, 14)), "pr_far_ruin", taper=0.86)
            n += 1
            if prev is not None:
                px, py = prev
                ST.arch(FAR, Vector(((x + px) / 2, (y + py) / 2, top - 9.0)), Vector((x - px, y - py, 0)), math.hypot(x - px, y - py) / 2 - 2.0, 7.0, 2.0, 7, rng, mat="pr_far_ruin", th=2.0, plain=True)
                m, ln = ST.lay(Vector((px, py, top)), Vector((x, y, top)))
                FAR.block(m, ln / 2 + 1.5, 2.5 - 0.05 * (k % 3), -2.2 - 0.3 * (k % 4), 1.6 + 0.35 * (k % 3) - 0.2 * (k % 2), "pr_far_ruin")
            prev = (x, y)
        else:
            prev = None
    for k in range(22):                                           # the wall and its towers: south-east
        a = math.radians(-95 + k * 5.2)
        R = 318 + 14 * math.cos(k * 0.9)
        x, y = cx + math.cos(a) * R * 0.92, cy + math.sin(a) * R
        g = gz(x, y)
        if k % 4 == 1:
            FAR.block(T(x, y, 0, a), 5.5, 5.5, g - 6, g + rng.uniform(34, 46), "pr_far_ruin", taper=0.82)
            FAR.block(T(x, y, 0, a), 5.2, 5.2, g + 30, g + rng.uniform(36, 50), "pr_far_ruin", taper=1.12)
            n += 1
        if k not in (5, 6, 14):
            a2 = math.radians(-95 + (k + 1) * 5.2); R2 = 318 + 14 * math.cos((k + 1) * 0.9)
            x2, y2 = cx + math.cos(a2) * R2 * 0.92, cy + math.sin(a2) * R2
            m, ln = ST.lay(Vector((x, y, g + 12)), Vector((x2, y2, gz(x2, y2) + 12)))
            FAR.block(m, ln / 2 + 0.5, 1.8, -20.0, rng.uniform(2.0, 9.0), "pr_far_ruin")
    COUNT["far aqueduct piers and towers"] = n


# ------------------------------------------------------------------ everything
def build_all(placed, PLACE, HH, hx, hy, HC):
    rng = random.Random(909)
    ROCKS_AT.extend(placed)
    L = W.LEGS
    # ---- every stage: its sign; every run: its poles and bunting
    for leg in L:
        sign(leg, rng)
        if leg.mode == "terrace":
            festoon(leg, rng)
    # ---- along the course: banners, lanterns, the works, the ruin, the crowd's things (never two the same side by side)
    crowd = ["prop_bench", "prop_banga_a", "prop_crate_a", "prop_jar_b", "prop_drum", "prop_banga_b", "prop_crate_b", "prop_jar_a", "prop_bench", "prop_cart"]
    works = ["prop_scaffold", "prop_crane", "prop_ladder", "prop_block", "prop_crane", "prop_scaffold"]
    ruin = ["prop_column_stub", "prop_drum_fallen", "prop_statue", "prop_block", "prop_drum_fallen", "prop_column_stub"]
    ban = ["prop_banner_red", "prop_banner_teal", "prop_banner_yellow"]
    ci = wi = ri = bi = 0
    for leg in W.ALL:
        step = 3.0
        n = max(1, int(leg.flat / step))
        for k in range(n + 1):
            for sd in (-1, 1):
                al = leg.flat * k / n + rng.uniform(-1.2, 1.2)
                off = leg.half + rng.uniform(1.6, 6.5) + (2.0 if leg.mode != "terrace" else 0.0)
                q = leg.A + leg.f * al + leg.s * (sd * off)
                z = leg.A.z + (leg.C.z - leg.A.z) * min(1, max(0, al / max(leg.flat, 1)))
                g = W.ground(q.x, q.y)
                near_floor = abs(g - z) < 2.6
                pick = rng.random()
                inward = -leg.s * sd
                if pick < 0.16 and near_floor:
                    bi += put(ban[bi % 3], q.x, q.y, rng, yaw=face(-leg.f) + rng.uniform(-25, 25), r=1.7, h=5.6)
                elif pick < 0.30 and near_floor:
                    put("prop_lantern", q.x, q.y, rng, yaw=math.degrees(math.atan2(inward.y, inward.x)), r=0.4, h=3.0)
                elif pick < 0.50 and near_floor:
                    m = crowd[ci % len(crowd)]
                    if put(m, q.x, q.y, rng, yaw=face(inward) + rng.uniform(-30, 30), s=rng.uniform(1.15, 1.5) if "banga" in m or "crate" in m else 1.0, r=0.9, sink=0.02):
                        ci += 1
                        if rng.random() < 0.6:                    # things come in twos and threes
                            put(crowd[(ci + 3) % len(crowd)], q.x + rng.uniform(-1.4, 1.4), q.y + rng.uniform(-1.4, 1.4), rng, s=rng.uniform(1.0, 1.4), r=0.7, sink=0.02)
                elif pick < 0.58:
                    wi += put(works[wi % len(works)], q.x, q.y, rng, yaw=face(inward) + rng.uniform(-40, 40), r=1.6, h=7.0, slope=0.4)
                elif pick < 0.72:
                    ri += put(ruin[ri % len(ruin)], q.x, q.y, rng, s=rng.uniform(0.9, 1.35), r=1.3)
    # ---- the stalls: at the start, the summit, the finish and two landings between
    for (leg, t, sd, model) in ((L[0], 0.12, 1, "prop_stall"), (L[0], 0.3, -1, "prop_awning_red"), (L[0], 0.5, 1, "prop_lean_to"), (L[0], 0.72, -1, "prop_awning_teal"), (L[20], 0.3, 1, "prop_awning_teal"),
                                (L[20], 0.6, -1, "prop_lean_to"), (L[20], 0.85, 1, "prop_awning_red"), (L[35], 0.2, -1, "prop_awning_red"), (L[35], 0.5, 1, "prop_stall"), (L[35], 0.75, -1, "prop_lean_to"),
                                (L[9], 0.4, 1, "prop_awning_teal"), (L[9], 0.7, -1, "prop_awning_red"), (L[25], 0.5, 1, "prop_awning_red"), (L[25], 0.3, -1, "prop_lean_to"), (L[22], 0.5, 1, "prop_awning_teal")):
        for off in (4.2, 5.5, 7.0, 9.0):
            q = leg.at(t) + leg.s * (sd * (leg.half + off))
            if abs(W.ground(q.x, q.y) - leg.A.z) < 1.6 and put(model, q.x, q.y, rng, yaw=face(-leg.s * sd), r=2.6, h=4.5, slope=0.35):
                for j in range(3):
                    put(rng.choice(["prop_jar_a", "prop_jar_b", "prop_banga_a", "prop_crate_a", "prop_bench", "prop_drum"]), q.x + rng.uniform(-3.2, 3.2), q.y + rng.uniform(-3.2, 3.2), rng, r=0.7, sink=0.02)
                break
    q = L[0].A - L[0].f * 9.5 + L[0].s * 9.5
    put("prop_scoreboard", q.x, q.y, rng, yaw=face(L[0].f), r=0.4, h=4.5, z=W.ground(q.x, q.y))
    # ---- the finish: a podium behind the platform, a mast each side, a board
    fin = W.BY["finish_platform"]
    f36, s36 = L[35].f, L[35].s
    base = fin.c + f36 * (fin.sz / 2 + 2.6)
    g = W.ground(base.x, base.y)
    yaw = math.atan2(f36.y, f36.x)
    for k, (off, h, col) in enumerate(((0.0, 1.3, "pr_paint_yellow"), (-1.7, 0.9, "pr_paint_white"), (1.7, 0.6, "pr_paint_red"))):
        c = base + s36 * off
        ST.S.m.block(T(c.x, c.y, 0, yaw), 0.8, 0.82, g - 0.6, g + h, SO.ASHLAR[k], top="pr_stone")
        paint("123"[k], Vector((c.x, c.y, g + max(0.08, h * 0.5 - 0.22))) - f36 * 0.8 - s36 * 0.12, s36, -f36, 0.44, col if col != "pr_paint_white" else "pr_paint_black")
    for sd in (-1, 1):
        c = base + s36 * (sd * 4.4) + f36 * 0.8
        t = pole(c.x, c.y, 8.0, rng)
        if t is not None:
            if not any(math.hypot(px - c.x, py - c.y) < pr * 1.25 + 1.3 for (px, py, pr) in ROCKS_AT):      # the cloth hangs clear of every boulder
                ST.banner(t - UP * 0.3, -f36, 4.2, 1.5, "pr_cloth_teal" if sd < 0 else "pr_cloth_red", rng)
    q = base + f36 * 3.4
    put("prop_scoreboard", q.x, q.y, rng, yaw=face(-f36), r=0.4, h=4.5, z=W.ground(q.x, q.y))
    COUNT["podium"] = 1
    # ---- creepers up the hop columns and the arch posts
    for s in W.SOLIDS:
        if s.name.startswith(("column_", "drum_", "arch_", "island_post", "spur_")) and s.idx % 2 == 0:
            g = W.ground(s.c.x, s.c.y)
            creeper(s.c.x, s.c.y, g + 0.2, min(s.top - 0.5, g + rng.uniform(5.0, 9.0)), s.r * (0.76 if s.name.startswith("column_") else 1.06), rng)
    # ---- the forest: grass and flowers near the course, palms and banana in the hollows and at the pools, trees on the benches
    for _ in range(5200):
        x, y = rng.uniform(W.X0 + 20, W.X1 - 20), rng.uniform(W.Y0 + 20, W.Y1 - 20)
        near, d = W.near_legs(x, y, 22.0)
        g = W.ground(x, y)
        green = float(W._bil(W.FIELDS["grass"], x, y)) > -0.6
        wet = min((wv.reach(x, y) for wv in W.WATERS), default=99.0)
        if float(W._bil(W.FIELDS["wet"], x, y)) > -0.5:
            continue
        p = rng.random()
        if wet < 7.0 and p < 0.75:
            put(rng.choice(["plant_reeds_a", "plant_reeds_b", "plant_banana_a", "plant_banana_b", "plant_palm_a", "plant_palm_c", "plant_flower_a", "plant_bush_b"]), x, y, rng, s=rng.uniform(0.85, 1.3), r=0.8, h=3.0, sink=0.1)
        elif len(d) and p < 0.30:
            put(rng.choice(["plant_tuft_a", "plant_tuft_b"]), x, y, rng, s=rng.uniform(0.9, 1.7), r=0.3, h=1.0, sink=0.03)
        elif len(d) and p < 0.42:
            put(rng.choice(["plant_bush_a", "plant_bush_b", "plant_bush_c", "plant_bush_d", "plant_flower_a", "plant_flower_b", "plant_shrub_a"]), x, y, rng, s=rng.uniform(0.9, 1.6), r=0.9, h=1.6, sink=0.08)
        elif (green or g < W.BOWL + 4.0) and len(d) and p < 0.50 and COUNT.get("palms", 0) < 34:
            if put(rng.choice(["plant_palm_a", "plant_palm_b", "plant_palm_c", "plant_palm_d"]), x, y, rng, s=rng.uniform(0.8, 1.15), r=1.6, h=11.0, sink=0.25, slope=0.4):
                COUNT["palms"] = COUNT.get("palms", 0) + 1
        elif green and p < 0.56 and g > W.BOWL + 3 and COUNT.get("trees", 0) < 20:
            if put(rng.choice(["tree_lean_olive", "tree_round"]), x, y, rng, s=rng.uniform(1.0, 1.6), r=2.0, h=7.0, sink=0.1, slope=0.4):
                COUNT["trees"] = COUNT.get("trees", 0) + 1
    for wv in W.WATERS:                                           # lilies on the water, along the rims, out of the swimmers' line
        for k in range(9):
            al, sd = rng.uniform(-wv.sz / 2 + 2, wv.sz / 2 - 2), rng.choice((-1, 1))
            q = wv.at(al, sd * (wv.sx / 2 - rng.uniform(0.9, 2.6)), wv.d["surface"])
            if not any(o.reach(q.x, q.y) < 0.8 for o in W.SOLIDS if not o.name.endswith("floor")) and not any(m.reach(q.x, q.y) < 2.2 for m in W.MOVERS):
                ST.PROPS.append(("plant_lily", q.x, q.y, q.z, rng.uniform(0, 360), rng.uniform(0.9, 1.5))); COUNT["plant_lily"] = COUNT.get("plant_lily", 0) + 1
    # ---- scree at the foot of the big boulders, so the massif meets the sand
    n = 0
    for (model, x, y, z, yaw, s) in PLACE:
        if s < 3.0 or n >= 300:
            continue
        for j in range(2):
            a = rng.uniform(0, 6.28)
            d = 1.25 * s * rng.uniform(0.85, 1.1)
            qx, qy = x + math.cos(a) * d, y + math.sin(a) * d
            if W.X0 + 12 < qx < W.X1 - 12 and W.Y0 + 12 < qy < W.Y1 - 12 and W.slope(qx, qy) < 0.7 and free(qx, qy, W.ground(qx, qy), W.ground(qx, qy) + 0.8, 1.2, 0.1):
                ST.PROPS.append((rng.choice(["prop_scree_a", "prop_scree_b"]), qx, qy, W.ground(qx, qy), rng.uniform(0, 360), rng.uniform(0.8, 1.7) * min(1.6, s / 4.0)))
                n += 1
    COUNT["scree"] = n
    skyline(HH, hx, hy, HC, rng)
    print("[dress] placed by kind:", ", ".join("%s %d" % kv for kv in sorted(COUNT.items())))
