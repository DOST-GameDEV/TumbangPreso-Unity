"""PAYO ASSEMBLY (2026-10-12): the map put together from the approved single pieces. Run inside Blender.

  blender -b --python tools/payo_assemble.py -- land            the land meshes (the only big thing made here) into Logs/payo/assembly/work/gen
  blender -b --python tools/payo_assemble.py -- write           copy the pieces into Assets/.../Payo/Models, write payo_art.json and materials_payo.json
  blender -b --python tools/payo_assemble.py -- review N [views] render the review views of round N into Logs/payo/assembly/shots_vN
  blender -b --python tools/payo_assemble.py -- axis            the axis test picture and its description

Where things stand is tools/payo_assemble_data.py. Nothing here edits an approved piece: a glb is copied byte for byte.

AXES. Work is done in the layout frame (x east, y north, z up). A piece's glb is plain glTF (y up, +z its front).
glTFast gives Unity (-x, y, z) of the glTF, so in Unity a piece at yaw 0 faces +z (north) with its glTF +x on the west.
Blender's importer gives (x, -z, y): the piece faces -Y. Turning the imported piece (pi - yaw) about Z shows exactly
what Unity shows for that yaw, so the review pictures are what the game will place. A mesh MADE here (the land) is
turned half a turn about Z as it is exported, so Unity puts it back at the origin with yaw 0.
"""
import json
import math
import os
import shutil
import sys

import bpy
import numpy as np
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import payo_assemble_data as D                      # noqa: E402

ROOT = D.ROOT
ART = os.path.join(ROOT, "Assets/TumbangPreso/Art/Payo")
MODELS = os.path.join(ART, "Models")
TEXTURES = os.path.join(ART, "Textures")
OUT = os.path.join(ROOT, "Logs/payo/assembly")
WORK = D.WORK
GEN = os.path.join(WORK, "gen")
KEPT = os.path.join(WORK, "kept")
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
MODE = ARGS[0] if ARGS else "land"

KEPT_TEXTURES = ("mayari_albedo.png", "mayari_b_albedo.png", "mayari_c_albedo.png", "weave_albedo.png", "sawali_albedo.png", "rice_payo_albedo.png", "grass_a_albedo.png",
                 "water_albedo.png")
LAND = ["payo_land_near_ne", "payo_land_near_se", "payo_land_near_sw", "payo_land_near_nw", "payo_land_far_ne", "payo_land_far_se", "payo_land_far_sw", "payo_land_far_nw",
        "payo_pit_floor"]
FAR_STATIC = ["payo_range_1", "payo_range_2", "payo_range_3", "payo_range_4", "payo_range_5", "payo_clouds"]


def source(model):
    """Where a model's glb is: the approved backup first, then the builders' later pieces, the kept ones, the land."""
    for d in [os.path.join(D.APPROVED, f) for f in ("stone", "gate", "bulul", "house", "prop", "terrace")] + [D.LATE_STONE, D.LATE_TERRACE, KEPT, GEN]:
        p = os.path.join(d, model + ".glb")
        if os.path.exists(p):
            return p
    return None


# ====================================================================================== the land
def grid_mesh(name, xs, ys, keep, drop=None):
    X, Y = np.meshgrid(xs, ys)
    Z = D.ground(X, Y)
    if drop is not None:
        Z = Z - drop(X, Y)
    nx, ny = len(xs), len(ys)
    # the notch's cut is 12 m from the axis: the grid lines there are already on it (12 is a multiple of the cell)
    verts = np.stack([X.ravel(), Y.ravel(), Z.ravel()], axis=1)
    faces = []
    for j in range(ny - 1):
        for i in range(nx - 1):
            cx, cy = (xs[i] + xs[i + 1]) / 2, (ys[j] + ys[j + 1]) / 2
            if keep(cx, cy):
                a = j * nx + i
                faces.append((a, a + 1, a + nx + 1, a + nx))
    return verts, faces


def land_object(name, verts, faces, mats):
    me = bpy.data.meshes.new(name)
    used = sorted({i for f in faces for i in f})
    remap = {o: n for n, o in enumerate(used)}
    v = verts[used]
    me.from_pydata([tuple(p) for p in v], [], [tuple(remap[i] for i in f) for f in faces])
    me.update()
    me.uv_layers.new(name="UVMap")
    me.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
    for m in mats:
        me.materials.append(m)
    # ONE ground material over the whole land (the ground kit's grass, tiling at 4 m), tinted per VERTEX so nothing
    # changes along a triangle's edge: deeper forest green with height and in broad drifts, grey rock where it is steep.
    nl = len(me.loops)
    lv = np.empty(nl, dtype=np.int32); me.loops.foreach_get("vertex_index", lv)
    co = np.empty(len(me.vertices) * 3); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    e = 1.5
    gx = (D.ground(co[:, 0] + e, co[:, 1]) - D.ground(co[:, 0] - e, co[:, 1])) / (2 * e)
    gy = (D.ground(co[:, 0], co[:, 1] + e) - D.ground(co[:, 0], co[:, 1] - e)) / (2 * e)
    slope = np.hypot(gx, gy)
    rock = np.clip((slope - 1.30) / 0.8, 0.0, 1.0)
    rib = np.asarray(D.seam(co[:, 0], co[:, 1]), dtype=float)        # between two fields: steep forest, rock only down its spine
    rock = rock * (1.0 - rib) + np.clip((slope - 3.3) / 0.7, 0.0, 1.0) * rib
    wet = np.asarray(D.wet(co[:, 0], co[:, 1]), dtype=float)
    rock = np.maximum(rock, 0.8 * wet)
    high = np.clip((co[:, 2] - 5.0) / 110.0, 0.0, 1.0)
    drift = 0.5 + 0.5 * np.sin(co[:, 0] * 0.043 + 1.3) * np.cos(co[:, 1] * 0.036 - 0.4)
    drift2 = 0.5 + 0.5 * np.sin(co[:, 0] * 0.011 - co[:, 1] * 0.017 + 0.7)
    tone = 0.74 + 0.16 * drift - 0.20 * high - 0.10 * drift2
    green = np.stack([tone * (0.80 + 0.10 * drift2), tone, tone * (0.72 + 0.08 * (1 - drift2))], axis=1)
    grey = np.stack([0.66 + 0.0 * tone, 0.68 + 0.0 * tone, 0.70 + 0.0 * tone], axis=1) * (0.8 + 0.25 * drift[:, None])
    # smaller drifts so no slope is one flat green: darker undergrowth, a drier yellow in patches
    under = 0.5 + 0.5 * np.sin(co[:, 0] * 0.19 + 0.6 * np.sin(co[:, 1] * 0.13)) * np.cos(co[:, 1] * 0.17 + 0.5 * np.sin(co[:, 0] * 0.11))
    dry = np.clip(np.sin(co[:, 0] * 0.071 + 2.1) * np.sin(co[:, 1] * 0.063 - 1.2) - 0.35, 0.0, 1.0)
    green = green * (0.80 + 0.26 * under[:, None])
    green = green * (1.0 - 0.5 * dry[:, None]) + np.array([0.86, 0.84, 0.50]) * tone[:, None] * 0.5 * dry[:, None]
    grey = grey * (1.0 - 0.28 * wet[:, None])
    green = green * (1.0 - 0.35 * rock[:, None])                    # the grass darkens toward a rock face
    green = green * (1.0 - 0.30 * rib[:, None] * np.clip((slope - 1.2) / 1.5, 0.0, 1.0)[:, None])   # and under the ribs' forest
    npoly = len(me.polygons)
    me.polygons.foreach_set("use_smooth", np.ones(npoly, dtype=bool))
    uv = co[lv][:, :2] / 4.0
    lc = green[lv]
    if len(mats) > 1 and nl == npoly * 4:
        # A FACE THAT IS STEEP OR IN THE STREAM'S BED IS ROCK: the second material (the cliff kit's own picture), read
        # down the face where it is steep so the picture does not smear, tinted a light grey that varies along it.
        prock = rock[lv].reshape(-1, 4).mean(axis=1) > 0.45
        psteep = slope[lv].reshape(-1, 4).mean(axis=1) > 1.0
        me.polygons.foreach_set("material_index", prock.astype(np.int32))
        lrock = np.repeat(prock, 4); lsteep = np.repeat(psteep, 4)
        cl = co[lv]
        # read down a steep face along whichever of x and y the face runs (so the picture never smears into streaks)
        alongy = np.repeat(np.abs(gx[lv]).reshape(-1, 4).mean(axis=1) > np.abs(gy[lv]).reshape(-1, 4).mean(axis=1), 4)
        side = np.stack([np.where(alongy, cl[:, 1], cl[:, 0]) / 4.0, cl[:, 2] / 4.0], axis=1)
        uv = np.where((lrock & lsteep)[:, None], side, uv)
        # beds of darker and paler stone lying nearly level, bent a little along the face; moss in the hollows and
        # wherever the face eases; and the face fades to the grass's own dark green at its edge
        band = 0.5 + 0.5 * np.sin(co[:, 2] * 0.85 + 1.6 * np.sin(co[:, 0] * 0.06 + co[:, 1] * 0.05))
        band2 = 0.5 + 0.5 * np.sin(co[:, 2] * 2.3 + 0.9 * np.sin(co[:, 0] * 0.11 - co[:, 1] * 0.09) + 1.0)
        shade = 0.36 + 0.20 * band + 0.10 * band2 + 0.10 * drift
        stone = np.stack([shade * 0.97, shade, shade * 1.07], axis=1) * (1.0 - 0.30 * wet[:, None])
        moss = np.clip((under - 0.50) * 3.0, 0.0, 1.0) * np.clip((2.8 - slope) / 1.4, 0.0, 1.0) * (1.0 - wet)
        mossy = np.stack([0.34 + 0.0 * tone, 0.52 + 0.0 * tone, 0.26 + 0.0 * tone], axis=1)
        stone = stone * (1.0 - 0.7 * moss[:, None]) + mossy * 0.7 * moss[:, None]
        edge = np.clip((np.maximum(rock, 0.8 * wet) - 0.25) / 0.45, 0.0, 1.0)
        stone = stone * edge[:, None] + mossy * 0.8 * (1.0 - edge[:, None])
        lc = np.where(lrock[:, None], stone[lv], lc)
    lc = np.concatenate([lc, np.ones((len(lc), 1))], axis=1)
    me.uv_layers["UVMap"].data.foreach_set("uv", uv.ravel())
    me.color_attributes["Col"].data.foreach_set("color", lc.ravel())
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    return o


def ground_material(name, picture):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    b.inputs["Roughness"].default_value = 1.0
    if picture and os.path.exists(picture):
        tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(picture)
        nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
    else:
        b.inputs["Base Color"].default_value = (0.2, 0.42, 0.16, 1) if "grass" in name else (0.16, 0.24, 0.13, 1)
    return m


NEAR_RECTS = [(-126.0, 126.0, -168.0, 126.0), (-126.0, -30.0, -222.0, -168.0), (-78.0, 78.0, 126.0, 246.0)]


def make_land():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(GEN, exist_ok=True)
    tex = os.path.join(D.LATE_TERRACE, "tex")
    grass = ground_material("py_ground_grass", os.path.join(tex, "py_ground_grass.png"))
    forest = ground_material("py_ground_forest", os.path.join(tex, "py_ground_forest.png"))
    rockm = ground_material("py_rock", os.path.join(tex, "py_rock.png"))
    stone = bpy.data.materials.new("payo_pit_dark"); stone.use_nodes = True
    stone.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.09, 0.10, 0.12, 1)
    stone.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
    H, N = D.HOLE, D.NOTCH
    hole = lambda x, y: (abs(x) < H and abs(y) < H) or (abs(x) < N and H <= y < 62.0)
    # THE NEAR LAND (a 1.5 m grid): the square round the court, a patch south west for the gorge's head, a patch north
    # for the knoll. THE FAR LAND (6 m) is everything else, lying 1.4 m under the near land where they overlap.
    rects = NEAR_RECTS
    quad = {"ne": lambda c: c[0] >= 0 and c[1] >= 0, "se": lambda c: c[0] >= 0 and c[1] < 0, "sw": lambda c: c[0] < 0 and c[1] < 0, "nw": lambda c: c[0] < 0 and c[1] >= 0}
    parts = {q: ([], []) for q in quad}
    for (x0, x1, y0, y1) in rects:
        v, f = grid_mesh("near", np.arange(x0, x1 + 0.01, 1.5), np.arange(y0, y1 + 0.01, 1.5), lambda x, y: not hole(x, y))
        for q, test in quad.items():
            pv_, pf_ = parts[q]
            base = sum(len(a) for a in pv_)
            pv_.append(v)
            pf_ += [tuple(base + i for i in fc) for fc in f if test(v[list(fc)].mean(axis=0))]
    objs = []
    for q in quad:
        objs.append(land_object("payo_land_near_" + q, np.concatenate(parts[q][0]), parts[q][1], [grass, rockm]))
    far = 342.0
    xf = np.arange(-far, far + 0.01, 6.0)
    inside = lambda x, y, m: any(x0 + m < x < x1 - m and y0 + m < y < y1 - m for (x0, x1, y0, y1) in rects)
    v, f = grid_mesh("far", xf, xf, lambda x, y: not inside(x, y, 12.0),
                     drop=lambda X, Y: 1.4 * np.any([(X > x0 + 1.0) & (X < x1 - 1.0) & (Y > y0 + 1.0) & (Y < y1 - 1.0) for (x0, x1, y0, y1) in rects], axis=0))
    for q, test in quad.items():
        ff = [fc for fc in f if test(v[list(fc)].mean(axis=0))]
        objs.append(land_object("payo_land_far_" + q, v, ff, [grass, rockm]))
    # the pit's ledge and the dark of the shaft under the wall kit, and the mouth of the notch below the lip
    S, LZ = D.SHAFT, D.LEDGE_Z
    pv, pf, pm = [], [], []

    def quadf(a, b, c, d, mat):
        n = len(pv); pv.extend([a, b, c, d]); pf.append((n, n + 1, n + 2, n + 3)); pm.append(mat)

    for (x0, x1, y0, y1) in ((-H, H, -H, -S), (-H, -S, -S, S), (S, H, -S, S), (-H, -N, S, H), (N, H, S, H)):       # the ledge
        quadf((x0, y0, LZ), (x1, y0, LZ), (x1, y1, LZ), (x0, y1, LZ), 0)
    deep = -62.0
    LZ_SHAFT = LZ
    for (a, b) in (((-S, -S), (S, -S)), ((S, -S), (S, S)), ((-S, S), (-S, -S)), ((S, S), (N, S)), ((-N, S), (-S, S)), ((N, S), (N, 96.0)), ((-N, 96.0), (-N, S))):
        quadf((a[0], a[1], LZ_SHAFT), (b[0], b[1], LZ_SHAFT), (b[0], b[1], deep), (a[0], a[1], deep), 1)
    for sx in (-1, 1):                                                                                             # the cheeks under the lip, past the kit
        ys = [60.0, 66.0, 72.0, 80.0, 96.0]
        for i in range(len(ys) - 1):
            za, zb = D.g(sx * (N + 0.5), ys[i]), D.g(sx * (N + 0.5), ys[i + 1])
            a, b = (sx * N, ys[i]), (sx * N, ys[i + 1])
            if sx < 0:
                a, b = b, a; za, zb = zb, za
            quadf((b[0], b[1], zb), (a[0], a[1], za), (a[0], a[1], deep), (b[0], b[1], deep), 1)
    me = bpy.data.meshes.new("payo_pit_floor")
    me.from_pydata(pv, [], pf); me.update()
    me.materials.append(forest); me.materials.append(stone)
    me.uv_layers.new(name="UVMap")
    me.polygons.foreach_set("material_index", np.array(pm, dtype=np.int32))
    lv = np.empty(len(me.loops), dtype=np.int32); me.loops.foreach_get("vertex_index", lv)
    co = np.array(pv, dtype=float)
    me.uv_layers["UVMap"].data.foreach_set("uv", (co[lv][:, :2] / 4.0).ravel())
    o = bpy.data.objects.new("payo_pit_floor", me); bpy.context.scene.collection.objects.link(o)
    objs.append(o)
    turn = Matrix.Rotation(math.pi, 4, "Z")
    tris = {}
    for o in objs:
        o.data.transform(turn)
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True); bpy.context.view_layer.objects.active = o
        kw = dict(filepath=os.path.join(GEN, o.name + ".glb"), export_format="GLB", use_selection=True, export_apply=True, export_yup=True)
        try:
            bpy.ops.export_scene.gltf(export_vertex_color="ACTIVE", export_all_vertex_colors=False, **kw)
        except TypeError:
            bpy.ops.export_scene.gltf(**kw)
        tris[o.name] = sum(len(p.vertices) - 2 for p in o.data.polygons)
    json.dump(tris, open(os.path.join(GEN, "land_tris.json"), "w"), indent=1)
    print("LAND", tris, sum(tris.values()))


# ====================================================================================== the game's files
def u_pos(p):
    return [round(p["x"], 3), round(p["z"], 3), round(p["y"], 3)]


def material_kind(name):
    if name.endswith("_glow"):
        return "glow"
    return {"py_water": "water", "py_fall": "fall", "py_foam": "foam", "py_mist": "mist", "py_wet": "decal", "py_terrace_plant": "foliage card", "py_terrace_far": "foliage card",
            "py_mayari": "unlit", "py_cloud": "cloud"}.get(name, "painted")


def write_game():
    out = D.build()
    P = out["placements"]
    cat = D.CAT
    land_tris = json.load(open(os.path.join(GEN, "land_tris.json")))
    old = json.load(open(os.path.join(WORK, "old_keep.json")))
    used = set(p["model"] for p in P) | set(LAND) | set(FAR_STATIC) | {"payo_sky_mayari", "catch_bird"} | {e["model"] for e in out["life"]}
    blocks = []
    for bid, e in out["blocks"].items():
        row = dict(id=bid, model=e["model"], x=e["x"], z=e["z"], sx=e["sx"], sz=e["sz"], yaw=e["yaw"])
        if e["extension"]:
            row["extension"] = e["extension"]; used.add(e["extension"])
        if bid in out["stairs"]:
            st = out["stairs"][bid]
            row["stair"] = dict(model=st["model"], down=st["down"], drop=st["drop"], yaw=st["yaw"]); used.add(st["model"])
        if e["model"]:
            used.add(e["model"])
        blocks.append(row)
    lay = out["layout"]
    movers = {}
    for L in lay["layouts"]:
        for m in L.get("movers") or []:
            kind = "lift" if m["id"].startswith("lift") else "ferry" if m["id"].startswith("ferry") else "piston"
            if kind in out["movers"]:
                movers[m["id"]] = dict(model=out["movers"][kind]); used.add(out["movers"][kind])
    for v in out["pads"].values():
        used.add(v)
    hide = all(b["model"] for b in blocks) and all(bid in out["stairs"] for bid, e in out["blocks"].items() if e["stair"])

    # ---- copy the pieces, unchanged; clear what the rejected pass left
    os.makedirs(MODELS, exist_ok=True)
    removed = 0
    for f in os.listdir(MODELS):
        base = f[:-9] if f.endswith(".glb.meta") else f[:-4] if f.endswith(".glb") else None
        if base is not None and base not in used:
            os.remove(os.path.join(MODELS, f)); removed += 1
    copied = 0
    for m in sorted(used):
        src = source(m)
        if src is None:
            raise RuntimeError("no glb for " + m)
        dst = os.path.join(MODELS, m + ".glb")
        if not os.path.exists(dst) or open(src, "rb").read() != open(dst, "rb").read():
            shutil.copyfile(src, dst); copied += 1
    tex_removed = 0
    keep_tex = set(KEPT_TEXTURES)
    for m in used:                                              # the kept models' pictures (life, far ranges, trees) stay
        for mn in (cat.get(m) or {}).get("materials", {}):
            t = OLD_MATERIALS.get(mn, {}).get("texture")
            if t:
                keep_tex.update((t + "_albedo.png", t + "_normal.png"))
    if os.path.isdir(TEXTURES):
        for f in os.listdir(TEXTURES):
            base = f[:-5] if f.endswith(".meta") else f
            if base not in keep_tex and os.path.isfile(os.path.join(TEXTURES, f)):
                os.remove(os.path.join(TEXTURES, f)); tex_removed += 1

    # ---- placements
    rows = []
    for m in LAND + FAR_STATIC + ["payo_sky_mayari"]:
        rows.append(dict(model=m, position=[0.0, 0.0, 0.0], yaw=0.0, scale=1.0, group="static", family="land" if m in LAND else "far"))
    night_nodes = ("__flame", "__lantern")
    for p in P:
        row = dict(model=p["model"], position=u_pos(p), yaw=p["yaw"], scale=p["scale"], group=p["group"], family=p["family"])
        for k in ("far", "lod", "on_block", "note", "field", "row", "span"):
            if k in p and p[k] not in (None, False):
                row[k] = p[k]
        rows.append(row)
    tri_family = {}
    total = 0
    for r in rows:
        t = land_tris.get(r["model"]) or cat.get(r["model"], {}).get("tris", 0)
        tri_family[r["family"]] = tri_family.get(r["family"], 0) + t; total += t
    stage_tris = sum(cat[b["model"]]["tris"] + (cat[b["extension"]]["tris"] if b.get("extension") else 0) for b in blocks if b["model"])
    counts = {}
    for r in rows:
        counts[r["family"]] = counts.get(r["family"], 0) + 1

    # ---- the night list
    nodes = sorted({o["name"] for m in used if m in cat for o in cat[m]["objects"] if any(o["name"].endswith(s) for s in night_nodes)})
    glow = sorted({mn for m in used if m in cat for mn in cat[m]["materials"] if mn.endswith("_glow")})
    lights = []
    for p in P:
        if p["model"] in ("fire_bowl", "torch", "hearth_pot", "lantern_pole"):
            h = {"fire_bowl": 1.6, "torch": 2.3, "hearth_pot": 0.5, "lantern_pole": 2.3}[p["model"]] * p["scale"]
            e = dict(position=[round(p["x"], 2), round(p["z"] + h, 2), round(p["y"], 2)], model=p["model"], range=9.0 if p["model"] == "fire_bowl" else 6.0)
            if p.get("on_block"):
                e["on_block"] = p["on_block"]
            lights.append(e)

    doc = {
        "note": "Written by tools/payo_assemble.py from tools/payo_assemble_data.py: the map ASSEMBLED from the approved single pieces "
                "(ArtSource/payo/pieces-approved-2026-10-12 and the same builders' later pieces). Unity axes. Positions are (x, y up, z north); yaw is degrees clockwise from above.",
        "axes": "EVERY PIECE IS A PLAIN glTF (y up, +z its front), copied unchanged. glTFast shows a glTF point (x, y, z) at Unity (-x, y, z): at yaw 0 a piece's FRONT faces "
                "+z (NORTH, the valley) and its glTF +x side is on the WEST. Place a piece at `position` with rotation (0, yaw, 0) and uniform `scale`: front = (sin yaw, cos yaw) in (x, z). "
                "The meshes made by the assembly (payo_land_*, payo_pit_floor) and the kept far pieces are already turned: place them at the origin with yaw 0. "
                "TEST (tools/payo_assemble.py axis, Logs/payo/assembly/axis_test.png): gate_portal at (0, -6, -49) yaw 0 has its stair toward the court and its sun disc facing north; "
                "house_shrine at (44.5, -6, 0) yaw 270 has its long stair toward the pit (west); block slabE with yaw 180 has its stepped carving on the EAST (outer) side, "
                "and stair_E with yaw 180 descends toward +x.",
        "layout_read": "Assets/TumbangPreso/Art/Payo/payo_layout.json",
        "placements": rows,
        "blocks": blocks,
        "blocks_note": "ONE MODEL PER BLOCK ID (the four corners share monolith). A model's origin is the centre of its block's COLLISION TOP and its top face is AT the origin: place it at "
                       "(x, top - 0.01, z) WITH ROTATION (0, yaw, 0) -- the yaw is new and it matters: slabE, slabW, midE, bridgeE and most stones need their half turn or their carving "
                       "faces inward, and BOTH STAIRS need yaw 180 or the ramp descends the wrong way under its collider. `extension` is a plain 40 m shaft with the same origin and yaw, "
                       "starting 12.6 m down: hang it on the block so a raised block shows no foot. Where a layout makes a block a stair, draw `stair.model` (yaw `stair.yaw`) instead.",
        "movers": movers,
        "movers_note": "Origin at the centre of the slab's top; place 1 cm under the collider's top at the mover's current (x, y, z).",
        "pads": out["pads"],
        "pads_note": "Origin on the floor at the layout's (x, y, z). pad_speed's travel is its +z: yaw 0 for +z, 180 for -z, 90 for +x, 270 for -x (unchanged: glTFast keeps z).",
        "hide_solids": bool(hide),
        "night": {
            "note": "By day HIDE the nodes named in `nodes_lit_at_night` that end __flame (they are separate meshes inside fire_bowl, torch and hearth_pot) and draw __lantern and "
                    "every *_glow material dim; from half time show the flames (breathe them), give each entry of `lights` a warm point light, and raise the emission of the glow "
                    "materials (the turquoise inlay of the gate and the stage, the houses' windows). There is no separate flame placement any more and no group 'night'.",
            "nodes_lit_at_night": nodes,
            "glow_materials": glow,
            "windows": [g_ for g_ in glow if "house" in g_],
            "lights": lights,
        },
        "sky": old["sky"],
        "life": out["life"],
        "life_note": old["life_note"] + " (2026-10-12 assembly: the 60 entries keep their kinds and models and stand on the new land: villagers and animals in the houses' yards, "
                     "planters, egrets and the wallowing carabao in the nearer paddies, hawks over the valley.)",
        "catch": old["catch"],
        "vertex_colour": "The land (payo_land_*: TWO materials, py_ground_grass tiling at 4 m with its forest shade in vertex colour, and py_rock on steep faces and the stream's bed), the terrace kit's stone, ground and plants, the life figures, the trees' solid parts and the kept far pieces carry vertex colour (COLOR_0) that their "
                         "materials must multiply by; see `vertex_colour` per material in materials_payo.json. The painted single pieces (stone, gate, bulul, houses, props) do not need it.",
        "terrace_lod": {"note": "Every terrace placement has `lod` (by its distance from the 44 m play square: full under `full_under`, mid under `mid_under`, far beyond; "
                                "`far` true) and `span` (metres of wall it stands for). A 12 m placement's three levels share pivot, footprint and curve: full is the bare name, "
                                "mid is <name>_mid (with the spout's fall; about one in five) or <name>_mid_plain, far is <name>_far. FAR ROWS ARE MERGED: a run of straight far "
                                "modules of one crop is ONE placement of <name>_far24 (span 24) or <name>_far48 (span 48), same pivot (the run's first module) and yaw; these "
                                "have no full or mid form, so do not swap them by distance. Caps exist only full: a far row has no cap. Far material py_terrace_far_wall is new; "
                                "a far flooded module's water is the object named *_water on the far atlas (py_terrace_far).", "full_under": D.FULL_D, "mid_under": D.MID_D},
        "far_note": old["far_note"] + " KEPT from the rejected pass (they were not the complaint) with their old materials py_ground, py_grass and py_cloud.",
        "sun": old["sun"],
        "look": old["look"],
        "counts": counts,
        "triangles": {"dressing_as_placed": total, "stage_29_blocks_with_extensions": stage_tris, "all": total + stage_tris, "by_family": tri_family},
        "notes": [
            "The pit: the wall kit runs from the lip (y -6) down 12 m to a ledge, at 30 m from the axis (the layout's pit); payo_pit_floor is that 4 m ledge and the dark shaft under it at 26 m.",
            "Spout heads (bulul_spout) sit in the pit_wall_spout holes with a fall_medium at 0.62 under each, its pool on the ledge.",
            "Falls are static pieces; py_fall's UV v runs downstream (scroll it). py_mist is the soft cloud at a fall's foot.",
            "rope_bridge spans the stream bed south east of the court; its deck sags 0.58 m below its pivot.",
            "TREES: the approved pieces have none, so the forest, palms, banana and bushes are the earlier Payo set's own models (tree_fig_*, tree_pine_*, payo_plant_*), kept with their old materials and textures; group 'plant' (they sway).",
            "The great fall stands at the head of a gorge cut straight up the south field's western slope on bearing 202.92 from the can (it faces the can, yaw 22.92); the cliff kit lines both walls and the head.",
            "THE STREAM runs down that one line: two small falls in the gorge, three medium falls down the hillside, one fall at each of the town's three benches, a runnel across the rim to the pit's spout at (-12, -30). No stream piece exists: the water is the falls' own pools; the land's vertex colour is wet rock along the bed.",
            "THE TOWN stands on three benches of the south field (y -3.7, 0.9 and 5.5), the rows of rice between them; three rope bridges cross the stream.",
            "A giant stands on its own platform ten walls up the east terraces (bulul_seated, scale 1.25) and the west (bulul_standing, 1.25).",
            "THE KNOLL: a terraced hill with three houses 150 to 230 m north of the can, standing out of the cloud; payo_land_near_ne and _nw carry it.",
        ],
    }
    json.dump(doc, open(os.path.join(ART, "payo_art.json"), "w"), indent=1)

    # ---- materials
    mats = {}
    for m in sorted(used):
        c = cat.get(m)
        if not c:
            continue
        for o in c["objects"]:
            for mn in o["materials"]:
                e = mats.setdefault(mn, dict(name=mn, kind=material_kind(mn), texture=None, models=[], vertex_colour=False))
                e["vertex_colour"] = e["vertex_colour"] or bool(o["vcol"])
                if m not in e["models"]:
                    e["models"].append(m)
        for mn, info in c["materials"].items():
            if mn in mats and info.get("texture"):
                mats[mn]["texture"] = info["texture"]
    for name in ("py_ground_grass", "py_ground_forest", "py_rock"):
        e = mats.setdefault(name, dict(name=name, kind="painted", texture=name, models=[], vertex_colour=True))
        e["models"] += [m for m in LAND if m not in e["models"] and (name != "py_rock" or m != "payo_pit_floor") and (name != "py_ground_forest" or m == "payo_pit_floor")]
        e["vertex_colour"] = True; e["tiling_m"] = 4.0
        if name == "py_rock":
            e["note"] = "also the SECOND material of every payo_land_* mesh: its steep faces and the stream's bed (vertex colour is a light grey there)"
    mats.setdefault("payo_pit_dark", dict(name="payo_pit_dark", kind="painted", texture=None, models=["payo_pit_floor"], vertex_colour=False, tint=[0.09, 0.10, 0.12]))
    old_m = {e["name"]: e for e in json.load(open(os.path.join(WORK, "rejected_materials_payo.json")))["materials"]}
    out_m = []
    for name in sorted(mats):
        e = mats[name]
        if name in old_m and e["texture"] is None:
            o = dict(old_m[name]); o["kind"] = e["kind"]; o["models"] = e["models"]; o["vertex_colour"] = True
            o["texture_from"] = "Textures/<texture>_albedo.png (kept from the rejected pass)" if o.get("texture") else "none"
            if name == "py_water":
                o["texture_from"] = "none: give it the game's paddy water"; o["note"] = "paddies, pools, troughs: the game's water shader"
            out_m.append(o); continue
        e["texture_from"] = "embedded in each glb that uses it" if e["texture"] else "none (a flat tint)"
        e["two_sided"] = e["kind"] in ("foliage card", "fall", "mist", "decal")
        e["cutout"] = e["kind"] == "foliage card"
        if e["kind"] == "glow":
            e["note"] = "its own small mesh: dim by day, emissive from half time"
        if name == "py_fall":
            e["note"] = "UV v runs downstream: scroll it (Shaders/PayoFall)"
        e["models"] = e["models"] if len(e["models"]) <= 12 else e["models"][:12] + ["... %d more" % (len(e["models"]) - 12)]
        out_m.append(e)
    embedded = sum(len(cat[m]["images"]) for m in used if m in cat)
    distinct = sorted({i[0] for m in used if m in cat for i in cat[m]["images"]})
    json.dump({"note": "One entry per material name the pieces use, AS NAMED IN THE GLBS (nothing was renamed). `kind` picks the shader: painted (LagoonPainted with the glb's own "
                       "texture), glow, water, fall, foam, mist, foliage card (alpha cut, both faces), decal, cloud, unlit. Each approved glb carries its own copy of its atlas: "
                       "bind the material to the texture INSIDE that glb, or share one texture per name (see `textures`).",
               "textures": {"embedded_copies": embedded, "distinct_pictures": len(distinct), "names": distinct,
                            "sharing": "The 24 props share 4 atlases (seta..setd); the terrace, pit, cliff and ground kits share 12 pictures (py_*); every house, gate piece, bulul and "
                                       "stage block has its own. One material per distinct picture is enough: about %d materials for %d embedded copies." % (len(distinct), embedded)},
               "materials": out_m}, open(os.path.join(ART, "materials_payo.json"), "w"), indent=1)
    print("WRITE models", len(used), "copied", copied, "removed files", removed, "textures removed", tex_removed)
    print("WRITE placements", len(rows), counts)
    print("WRITE triangles", total, "+ stage", stage_tris, "=", total + stage_tris, tri_family)
    print("WRITE hide_solids", hide, "materials", len(out_m), "embedded textures", embedded, "distinct", len(distinct))


# ====================================================================================== the review
_ink = [None]
OLD_MATERIALS = {e["name"]: e for e in json.load(open(os.path.join(WORK, "rejected_materials_payo.json")))["materials"]}


def ink_material():
    if _ink[0] is None:
        m = bpy.data.materials.new("ink"); m.use_nodes = True; m.use_backface_culling = True
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = (0.03, 0.03, 0.04, 1); b.inputs["Roughness"].default_value = 1
        _ink[0] = m
    return _ink[0]


def night_value(nt, day, night):
    """A number that is `day` in a scene whose custom property "night" is 0 and `night` where it is 1."""
    at = nt.nodes.new("ShaderNodeAttribute"); at.attribute_type = "VIEW_LAYER"; at.attribute_name = "night"
    mr = nt.nodes.new("ShaderNodeMapRange"); mr.inputs["To Min"].default_value = day; mr.inputs["To Max"].default_value = night
    nt.links.new(at.outputs["Fac"], mr.inputs["Value"])
    return mr.outputs["Result"]


def flame_dress(m):
    if m.get("flame_done"):
        return
    m["flame_done"] = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    if b is None:
        return
    nt.links.new(night_value(nt, 0.0, 9.0), b.inputs["Emission Strength"])
    nt.links.new(night_value(nt, 0.0, 1.0), b.inputs["Alpha"])
    m.surface_render_method = "DITHERED"


def dress(m):
    """An imported material made to look nearer the game (the piece builders' own review look)."""
    if m.get("dressed"):
        return
    m["dressed"] = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    if b is None:
        return
    tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None)
    name = m.name.split(".")[0]
    old = OLD_MATERIALS.get(name)
    if tex is None and old and old.get("texture"):
        pic = os.path.join(TEXTURES, old["texture"] + "_albedo.png")
        if os.path.exists(pic):
            tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(pic, check_existing=True)
            if old.get("foliage") or old.get("cutout"):
                nt.links.new(tex.outputs["Alpha"], b.inputs["Alpha"])
                m.surface_render_method = "DITHERED"; m.use_backface_culling = False
                m["card"] = True
            tn = old.get("tint") or [1, 1, 1]
            if tuple(tn) != (1, 1, 1):
                tm = nt.nodes.new("ShaderNodeMixRGB"); tm.blend_type = "MULTIPLY"; tm.inputs[0].default_value = 1.0
                tm.inputs[2].default_value = (tn[0], tn[1], tn[2], 1.0)
                nt.links.new(tex.outputs["Color"], tm.inputs[1])
                m["tinted"] = True
    b.inputs["Roughness"].default_value = 1.0
    b.inputs["Metallic"].default_value = 0.0
    if name == "py_mayari":
        for l in list(nt.links):
            nt.links.remove(l)
        em = nt.nodes.new("ShaderNodeEmission"); tr = nt.nodes.new("ShaderNodeBsdfTransparent"); mx = nt.nodes.new("ShaderNodeMixShader")
        out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
        if tex:
            nt.links.new(tex.outputs["Color"], em.inputs[0])
            mul = nt.nodes.new("ShaderNodeMath"); mul.operation = "MULTIPLY"
            nt.links.new(tex.outputs["Alpha"], mul.inputs[0]); nt.links.new(night_value(nt, 0.0, 1.0), mul.inputs[1])
            nt.links.new(mul.outputs[0], mx.inputs[0])
        em.inputs[1].default_value = 1.5
        nt.links.new(tr.outputs[0], mx.inputs[1]); nt.links.new(em.outputs[0], mx.inputs[2]); nt.links.new(mx.outputs[0], out.inputs["Surface"])
        m.surface_render_method = "DITHERED"; m.use_backface_culling = False
        return
    if name == "py_water":
        for l in list(nt.links):
            nt.links.remove(l)
        em = nt.nodes.new("ShaderNodeEmission"); em.inputs[0].default_value = (0.30, 0.62, 0.74, 1); em.inputs[1].default_value = 0.9
        nt.links.new(night_value(nt, 0.9, 0.10), em.inputs[1])
        out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
        nt.links.new(em.outputs[0], out.inputs["Surface"])
        return
    col = None
    if tex:
        col = tex.outputs["Color"]
        if m.get("tinted"):
            col = next(n for n in nt.nodes if n.type == "MIX_RGB" and n.blend_type == "MULTIPLY").outputs[0]
    elif old is not None and old.get("tint"):
        rgb = nt.nodes.new("ShaderNodeRGB"); tn = old["tint"]
        rgb.outputs[0].default_value = (tn[0], tn[1], tn[2], 1.0)
        col = rgb.outputs[0]
    if name.startswith("py_") and old is not None or name in ("py_terrace_stone", "py_terrace_ground", "py_terrace_plant", "py_rock", "py_ground_grass", "py_ground_forest", "py_flat", "py_weave", "py_ground", "py_grass", "py_pit_stone", "py_terrace_wood", "py_terrace_far", "py_terrace_far_wall"):
        at = nt.nodes.new("ShaderNodeAttribute"); at.attribute_name = "Col"; at.attribute_type = "GEOMETRY"
        if col is not None:
            mul = nt.nodes.new("ShaderNodeMixRGB"); mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1.0
            nt.links.new(col, mul.inputs[1]); nt.links.new(at.outputs["Color"], mul.inputs[2])
            col = mul.outputs[0]
        else:
            col = at.outputs["Color"]
    if col is not None:
        for l in list(b.inputs["Base Color"].links):
            nt.links.remove(l)
        nt.links.new(col, b.inputs["Base Color"]); nt.links.new(col, b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = {"py_terrace_plant": 0.45, "py_terrace_far": 0.45, "py_fall": 0.45, "py_foam": 0.8, "py_mist": 0.85, "py_cloud": 0.8}.get(name, 0.22)
    if name.endswith("_glow"):
        b.inputs["Emission Strength"].default_value = 1.2
        m["glow"] = True
        nt.links.new(night_value(nt, 1.2, 7.0), b.inputs["Emission Strength"])
    elif col is not None:
        e0 = b.inputs["Emission Strength"].default_value
        nt.links.new(night_value(nt, e0, e0 * 0.045), b.inputs["Emission Strength"])
    if name == "py_cloud":
        b.inputs["Base Color"].default_value = (0.9, 0.96, 0.98, 1); b.inputs["Emission Color"].default_value = (0.9, 0.96, 0.98, 1); b.inputs["Emission Strength"].default_value = 0.75
    if name in ("py_fall", "py_wet", "py_mist"):
        m.surface_render_method = "DITHERED"
        if name == "py_mist":
            b.inputs["Alpha"].default_value = 0.7
    m.use_backface_culling = name not in ("py_terrace_plant", "py_terrace_far", "py_fall", "py_wet", "py_mist") and not m.get("card")


INKED = ("stone", "gate", "house", "bulul", "prop", "pit", "stage")


def load_model(model, family):
    """One collection per model, its objects as imported (front at -Y), inked where the builders inked."""
    name = "M_" + model
    if name in bpy.data.collections:
        return bpy.data.collections[name]
    src = source(model)
    if src is None:
        raise RuntimeError("no glb for " + model)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=src)
    obs = [o for o in bpy.data.objects if o not in before]
    c = bpy.data.collections.new(name)
    for o in obs:
        for uc in list(o.users_collection):
            uc.objects.unlink(o)
        c.objects.link(o)
        if o.type != "MESH":
            continue
        for ca in o.data.color_attributes:
            ca.name = "Col"; break
        for m in o.data.materials:
            if m:
                dress(m)
        o["flame"] = o.name.split(".")[0].endswith("__flame")
        if o["flame"]:
            for m in o.data.materials:
                if m:
                    flame_dress(m)
        if family in INKED and not o["flame"] and not any(m and m.name.split(".")[0] in ("py_terrace_plant", "py_wet") for m in o.data.materials) and len(o.data.polygons) > 30:
            thick = 0.035 if family in ("stage", "gate", "house", "pit") else 0.02
            me = o.data
            n = len(me.vertices)
            co = np.empty(n * 3); me.vertices.foreach_get("co", co)
            no = np.empty(n * 3); me.vertices.foreach_get("normal", no)
            co = (co + no * thick).reshape(-1, 3)
            faces = [tuple(reversed(p.vertices[:])) for p in me.polygons]
            im = bpy.data.meshes.new(me.name + "_ink"); im.from_pydata([tuple(v) for v in co], [], faces); im.update()
            im.materials.append(ink_material())
            io = bpy.data.objects.new(o.name + "_ink", im); io.matrix_world = o.matrix_world.copy(); io.parent = o.parent
            c.objects.link(io)
    return c


def stand(model, family, x, y, z, yaw, scale=1.0, holder=None):
    c = load_model(model, family)
    e = bpy.data.objects.new(model, None)
    e.instance_type = "COLLECTION"; e.instance_collection = c
    e.matrix_world = Matrix.Translation((x, y, z)) @ Matrix.Rotation(math.pi - math.radians(yaw), 4, "Z") @ Matrix.Scale(scale, 4)
    (holder or bpy.context.scene.collection).objects.link(e)
    return e


def make_world(night):
    """The sky the camera sees: a gradient, with flat painted cloud by day and stars at night. The light it gives is
    a cool blue (the references' cool tone is LIGHT; the stone stays grey)."""
    w = bpy.data.worlds.new("Night sky" if night else "Day sky"); w.use_nodes = True
    nt = w.node_tree
    bg = nt.nodes["Background"]
    lp = nt.nodes.new("ShaderNodeLightPath"); mixn = nt.nodes.new("ShaderNodeMixRGB")
    geo0 = nt.nodes.new("ShaderNodeNewGeometry"); sep = nt.nodes.new("ShaderNodeSeparateXYZ"); ramp = nt.nodes.new("ShaderNodeValToRGB")
    geo = nt.nodes.new("ShaderNodeVectorMath"); geo.operation = "SCALE"; geo.inputs["Scale"].default_value = -1.0      # Incoming points back at the eye: turn it to look out
    nt.links.new(geo0.outputs["Incoming"], geo.inputs[0])
    nt.links.new(geo.outputs["Vector"], sep.inputs[0]); nt.links.new(sep.outputs["Z"], ramp.inputs[0])
    ramp.color_ramp.elements[0].position = 0.0; ramp.color_ramp.elements[1].position = 0.55
    if night:
        mixn.inputs[1].default_value = (0.035, 0.055, 0.14, 1)
        ramp.color_ramp.elements[0].color = (0.06, 0.10, 0.24, 1); ramp.color_ramp.elements[1].color = (0.006, 0.010, 0.045, 1)
    else:
        mixn.inputs[1].default_value = (0.40, 0.58, 0.92, 1)
        ramp.color_ramp.elements[0].color = (0.46, 0.72, 0.94, 1); ramp.color_ramp.elements[1].color = (0.12, 0.34, 0.76, 1)
        ramp.color_ramp.elements[1].position = 0.42
    sky = ramp.outputs[0]
    # cloud: noise read on the sky's dome flattened toward the horizon, cut to flat shapes
    dv = nt.nodes.new("ShaderNodeMath"); dv.operation = "ADD"; dv.inputs[1].default_value = 0.22
    nt.links.new(sep.outputs["Z"], dv.inputs[0])
    dx = nt.nodes.new("ShaderNodeMath"); dx.operation = "DIVIDE"; dy = nt.nodes.new("ShaderNodeMath"); dy.operation = "DIVIDE"
    nt.links.new(sep.outputs["X"], dx.inputs[0]); nt.links.new(dv.outputs[0], dx.inputs[1])
    nt.links.new(sep.outputs["Y"], dy.inputs[0]); nt.links.new(dv.outputs[0], dy.inputs[1])
    cmb = nt.nodes.new("ShaderNodeCombineXYZ"); nt.links.new(dx.outputs[0], cmb.inputs[0]); nt.links.new(dy.outputs[0], cmb.inputs[1])
    noise = nt.nodes.new("ShaderNodeTexNoise"); noise.inputs["Scale"].default_value = 1.15; noise.inputs["Detail"].default_value = 3.0
    noise.inputs["Roughness"].default_value = 0.55
    nt.links.new(cmb.outputs[0], noise.inputs["Vector"])
    cr = nt.nodes.new("ShaderNodeValToRGB"); cr.color_ramp.elements[0].position = 0.50; cr.color_ramp.elements[1].position = 0.545
    nt.links.new(noise.outputs["Fac"], cr.inputs[0])
    up = nt.nodes.new("ShaderNodeMapRange"); up.inputs["From Min"].default_value = 0.02; up.inputs["From Max"].default_value = 0.16
    nt.links.new(sep.outputs["Z"], up.inputs["Value"])
    cf = nt.nodes.new("ShaderNodeMath"); cf.operation = "MULTIPLY"
    nt.links.new(cr.outputs[0], cf.inputs[0]); nt.links.new(up.outputs["Result"], cf.inputs[1])
    amt = nt.nodes.new("ShaderNodeMath"); amt.operation = "MULTIPLY"; amt.inputs[1].default_value = 0.30 if night else 0.92
    nt.links.new(cf.outputs[0], amt.inputs[0])
    # a second, softer read of the same noise shades each cloud's underside
    cr2 = nt.nodes.new("ShaderNodeValToRGB"); cr2.color_ramp.elements[0].position = 0.53; cr2.color_ramp.elements[1].position = 0.64
    cr2.color_ramp.elements[0].color = (0.10, 0.14, 0.26, 1) if night else (0.84, 0.88, 0.96, 1)
    cr2.color_ramp.elements[1].color = (0.20, 0.26, 0.42, 1) if night else (1.0, 1.0, 1.0, 1)
    nt.links.new(noise.outputs["Fac"], cr2.inputs[0])
    cm = nt.nodes.new("ShaderNodeMixRGB")
    nt.links.new(amt.outputs[0], cm.inputs[0]); nt.links.new(sky, cm.inputs[1]); nt.links.new(cr2.outputs[0], cm.inputs[2])
    sky = cm.outputs[0]
    if night:
        vor = nt.nodes.new("ShaderNodeTexVoronoi"); vor.inputs["Scale"].default_value = 70.0
        nt.links.new(geo.outputs["Vector"], vor.inputs["Vector"])
        st = nt.nodes.new("ShaderNodeMapRange"); st.inputs["From Min"].default_value = 0.20; st.inputs["From Max"].default_value = 0.09
        nt.links.new(vor.outputs["Distance"], st.inputs["Value"])
        few = nt.nodes.new("ShaderNodeSeparateXYZ"); nt.links.new(vor.outputs["Color"], few.inputs[0])
        gt = nt.nodes.new("ShaderNodeMath"); gt.operation = "GREATER_THAN"; gt.inputs[1].default_value = 0.72
        nt.links.new(few.outputs["X"], gt.inputs[0])
        tw = nt.nodes.new("ShaderNodeMath"); tw.operation = "MULTIPLY"
        nt.links.new(st.outputs["Result"], tw.inputs[0]); nt.links.new(gt.outputs[0], tw.inputs[1])
        hz = nt.nodes.new("ShaderNodeMapRange"); hz.inputs["From Min"].default_value = 0.0; hz.inputs["From Max"].default_value = 0.12
        nt.links.new(sep.outputs["Z"], hz.inputs["Value"])
        tw2 = nt.nodes.new("ShaderNodeMath"); tw2.operation = "MULTIPLY"
        nt.links.new(tw.outputs[0], tw2.inputs[0]); nt.links.new(hz.outputs["Result"], tw2.inputs[1])
        inv = nt.nodes.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
        nt.links.new(amt.outputs[0], inv.inputs[1])
        tw3 = nt.nodes.new("ShaderNodeMath"); tw3.operation = "MULTIPLY"
        nt.links.new(tw2.outputs[0], tw3.inputs[0]); nt.links.new(inv.outputs[0], tw3.inputs[1])
        add = nt.nodes.new("ShaderNodeMixRGB"); add.blend_type = "ADD"; add.inputs[2].default_value = (0.95, 0.97, 1.0, 1)
        nt.links.new(tw3.outputs[0], add.inputs[0]); nt.links.new(sky, add.inputs[1])
        sky = add.outputs[0]
    nt.links.new(sky, mixn.inputs[2])
    nt.links.new(lp.outputs["Is Camera Ray"], mixn.inputs[0]); nt.links.new(mixn.outputs[0], bg.inputs[0])
    bg.inputs[1].default_value = 1.0
    return w


FAMILY_COLLECTION = {"land": "Land", "stage": "Stage", "pit": "Pit", "gate": "Gate", "bulul": "Guardians", "house": "Town", "prop": "Props", "path": "Props",
                     "terrace_prop": "Props", "tree": "Plants", "cover": "Plants", "water": "Water", "cliff": "Cliffs", "far": "Far", "life": "Life"}
COLLECTIONS = ["Land", "Stage", "Pit", "Gate", "Guardians", "Town", "Terraces full", "Terraces mid", "Terraces far", "Props", "Plants", "Water", "Cliffs", "Far", "Life",
               "Cameras", "Sun day", "Sun night", "Night lights"]
_col = {}


def holder(name):
    if name not in _col:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
        _col[name] = c
    return _col[name]


def scene_setup():
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    sc.unit_settings.system = "METRIC"; sc.unit_settings.scale_length = 1.0
    _col.clear()
    for nm in COLLECTIONS:
        holder(nm)
    worlds = {False: make_world(False), True: make_world(True)}
    # DAY: a warm sun over the valley to the north east (bearing 50, elevation 31) under a cool sky's fill.
    # NIGHT: the moon low over the northern ranges, and little else: the fires do the work.
    for night, suns in ((False, (((59, 0, 180 + 50), 2.6, (1.0, 0.93, 0.80)), ((118, 0, -30), 0.75, (0.55, 0.72, 1.0)))),
                        (True, (((60, 0, 180 + 13), 0.13, (0.50, 0.64, 1.0)), ((118, 0, -30), 0.02, (0.4, 0.5, 1.0))))):
        for rot, energy, colour in suns:
            sun = bpy.data.objects.new("moon" if night else "sun", bpy.data.lights.new("moon" if night else "sun", "SUN"))
            holder("Sun night" if night else "Sun day").objects.link(sun)
            sun.data.energy = energy; sun.data.color = colour; sun.rotation_euler = tuple(math.radians(a) for a in rot)
    cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); holder("Cameras").objects.link(cam)
    cam.data.clip_start = 0.3; cam.data.clip_end = 6000; cam.data.lens = 20
    sc.camera = cam
    sc.render.resolution_x, sc.render.resolution_y = 1120, 630
    sc["worlds"] = [worlds[False].name, worlds[True].name]
    set_time(sc, False)
    return sc, cam


def set_time(sc, night):
    sc["night"] = 1.0 if night else 0.0
    sc.world = bpy.data.worlds["Night sky" if night else "Day sky"]
    for nm, on in (("Sun day", not night), ("Sun night", night), ("Night lights", night)):
        c = bpy.data.collections.get(nm)
        if c:
            c.hide_render = not on
            lc = find_layer(bpy.context.view_layer.layer_collection, nm) if sc == bpy.context.scene else None
            if lc:
                lc.exclude = not on
    sc.update_tag()


def find_layer(lc, name):
    if lc.collection.name == name:
        return lc
    for ch in lc.children:
        r = find_layer(ch, name)
        if r:
            return r
    return None


def haze(path, depth_path, night):
    """Distance haze laid over the render from its depth (the game's fog does this): pale aqua by day, deep blue at night."""
    from_file = bpy.data.images.load(path)
    w, h = from_file.size
    px = np.array(from_file.pixels[:]).reshape(h, w, 4)
    dimg = bpy.data.images.load(depth_path)
    d = np.array(dimg.pixels[:]).reshape(h, w, 4)[:, :, 0]
    fog = np.clip((d - 90.0) / 2600.0, 0.0, 1.0) ** 0.55 * 0.92
    fog = np.where(d > 5000.0, 0.0, fog)
    colour = np.array([0.03, 0.05, 0.13]) if night else np.array([0.62, 0.80, 0.90])
    px[:, :, :3] = px[:, :, :3] * (1.0 - fog[:, :, None]) + colour * fog[:, :, None]
    from_file.pixels = px.ravel().tolist()
    from_file.save()
    bpy.data.images.remove(from_file); bpy.data.images.remove(dimg)


def look(cam, eye, target, lens=20):
    cam.data.lens = lens
    cam.location = Vector(eye)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()


def build_scene(layout_name="liwasan", only=None, empty=True):
    if empty:
        bpy.ops.wm.read_factory_settings(use_empty=True)
    _ink[0] = None
    sc, cam = scene_setup()
    out = D.build()
    for m in LAND:
        stand(m, "land", 0, 0, 0, 0.0, holder=holder("Land"))
    for m in FAR_STATIC + ["payo_sky_mayari"]:
        e = stand(m, "far", 0, 0, 0, 0.0, holder=holder("Far"))
        if m == "payo_sky_mayari":
            e["no_fog"] = True
    for p in out["placements"]:
        if only and p["family"] not in only:
            continue
        cn = ("Terraces " + p.get("lod", "full")) if p["family"] == "terrace" else FAMILY_COLLECTION.get(p["family"], "Props")
        stand(p["model"], p["family"], p["x"], p["y"], p["z"], p["yaw"], p["scale"], holder=holder(cn))
    for e in out["life"]:
        x, z, y = e["position"]
        stand(e["model"], "life", x, y, z, e["yaw"], holder=holder("Life"))
    L = next(l for l in out["layout"]["layouts"] if l["name"] == layout_name)
    st_ = holder("Stage")
    for pc in L["pieces"]:
        b = out["blocks"][pc["id"]]
        if pc.get("stairDown") and pc["id"] in out["stairs"]:
            st = out["stairs"][pc["id"]]
            stand(st["model"], "stage", pc["x"], pc["z"], pc["top"] - 0.01, st["yaw"], holder=st_)
        elif b["model"]:
            stand(b["model"], "stage", pc["x"], pc["z"], pc["top"] - 0.01, b["yaw"], holder=st_)
        if b["extension"]:
            stand(b["extension"], "stage", pc["x"], pc["z"], pc["top"] - 0.01, b["yaw"], holder=st_)
    for kind, key in (("jumpPads", "jumpPads"), ("speedPads", "speedPads"), ("pickups", "pickups")):
        for pd in L.get(kind) or []:
            if key in out["pads"]:
                yaw = {"+z": 0.0, "-z": 180.0, "+x": 90.0, "-x": 270.0}.get(pd.get("along"), 0.0)
                stand(out["pads"][key], "stage", pd["x"], pd["z"], pd["y"], yaw, holder=st_)
    for m in L.get("movers") or []:
        kind = "lift" if m["id"].startswith("lift") else "ferry" if m["id"].startswith("ferry") else "piston"
        if kind in out["movers"]:
            stand(out["movers"][kind], "stage", m["from"][0], m["from"][2], m["from"][1] - 0.01, 0.0, holder=st_)
    # THE NIGHT'S LAMPS: a warm point light at every fire bowl, torch, lantern and hearth, and one before every house
    # (its windows' light on the ground). They are in "Night lights", which the day leaves out.
    nl = holder("Night lights")
    watts = {"fire_bowl": (1.7, 2600.0, 0.9), "torch": (2.4, 1100.0, 0.5), "hearth_pot": (0.6, 800.0, 0.5), "lantern_pole": (2.3, 900.0, 0.4)}
    for p in out["placements"]:
        if p["model"] in watts:
            h, w, rad = watts[p["model"]]
            l = bpy.data.objects.new("fire", bpy.data.lights.new("fire", "POINT")); nl.objects.link(l)
            l.location = (p["x"], p["y"], p["z"] + h * p["scale"]); l.data.energy = w
            l.data.color = (1.0, 0.55, 0.20); l.data.shadow_soft_size = rad; l.data.use_shadow = False
        elif p["family"] == "house":
            fx, fy = D.front(p["yaw"])
            d = -D.CAT[p["model"]]["lo"][1] * p["scale"] + 1.6
            l = bpy.data.objects.new("window", bpy.data.lights.new("window", "POINT")); nl.objects.link(l)
            l.location = (p["x"] + fx * d, p["y"] + fy * d, p["z"] + 2.6); l.data.energy = 700.0
            l.data.color = (1.0, 0.66, 0.30); l.data.shadow_soft_size = 1.2; l.data.use_shadow = False
    set_time(sc, False)
    return sc, cam, out


def shoot(sc, name, folder, night=False):
    """The picture, then the same view as distance only, and the distance laid over it as haze (the game's fog)."""
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, name + ".png")
    sc.render.filepath = path
    sc.render.image_settings.file_format = "PNG"
    sc.render.film_transparent = False
    bpy.context.view_layer.material_override = None
    bpy.ops.render.render(write_still=True)
    fog = bpy.data.materials.get("fog_pass")
    if fog is None:
        fog = bpy.data.materials.new("fog_pass"); fog.use_nodes = True
        nt = fog.node_tree
        for n in list(nt.nodes):
            nt.nodes.remove(n)
        cd = nt.nodes.new("ShaderNodeCameraData"); mr = nt.nodes.new("ShaderNodeMapRange")
        mr.inputs["From Min"].default_value = 110.0; mr.inputs["From Max"].default_value = 2400.0
        em = nt.nodes.new("ShaderNodeEmission"); out = nt.nodes.new("ShaderNodeOutputMaterial")
        nt.links.new(cd.outputs["View Distance"], mr.inputs["Value"]); nt.links.new(mr.outputs["Result"], em.inputs["Color"]); nt.links.new(em.outputs[0], out.inputs["Surface"])
    hidden = [o for o in bpy.data.objects if o.get("no_fog") and not o.hide_render]
    for o in hidden:
        o.hide_render = True
    fpath = os.path.join(folder, "_fog.png")
    sc.render.filepath = fpath
    sc.render.film_transparent = True
    bpy.context.view_layer.material_override = fog
    bpy.ops.render.render(write_still=True)
    bpy.context.view_layer.material_override = None
    sc.render.film_transparent = False
    for o in hidden:
        o.hide_render = False
    a = bpy.data.images.load(path); b = bpy.data.images.load(fpath)
    w, h = a.size
    pa = np.array(a.pixels[:]).reshape(h, w, 4); pb = np.array(b.pixels[:]).reshape(h, w, 4)
    f = np.clip(pb[:, :, 0], 0.0, 1.0) * pb[:, :, 3] * (0.80 if night else 0.86)
    colour = np.array([0.025, 0.04, 0.11]) if night else np.array([0.66, 0.84, 0.92])
    pa[:, :, :3] = pa[:, :, :3] * (1.0 - f[:, :, None]) + colour * f[:, :, None]
    a.pixels = pa.ravel().tolist(); a.save()
    bpy.data.images.remove(a); bpy.data.images.remove(b)
    return path


def views():
    v = {}
    names = ["n", "ne", "e", "se", "s", "sw", "w", "nw"]
    for i, nm in enumerate(names):
        a = math.radians(45.0 * i)
        v["a_can_" + nm] = ((0.0, 0.0, 1.7), (math.sin(a) * 60, math.cos(a) * 60, 6.0), 20)
    for nm, (bx, by) in (("midE", (18.0, 0.0)), ("midS", (0.0, -18.0)), ("midW", (-18.0, 0.0)), ("midN", (0.0, 18.0))):
        n = math.hypot(bx, by)
        ox, oy = bx / n, by / n
        v["b_%s_out" % nm] = ((bx + ox * 2.0, by + oy * 2.0, 1.7), (bx + ox * 60, by + oy * 60, 3.0), 20)
        v["b_%s_back" % nm] = ((bx + ox * 2.5, by + oy * 2.5, 1.7), (-ox * 30 + oy * 14, -oy * 30 - ox * 14, 2.0), 20)
    v["c_gate_stair"] = ((0.0, D.GATE_Y + 6.0, D.RIM_Z + 2.0 + 1.7), (0.0, 0.0, 1.0), 20)
    v["c_gate_foot"] = ((0.0, D.GATE_Y + 15.0, D.RIM_Z + 1.7), (0.0, 0.0, 3.0), 20)
    v["d_air_south"] = ((70.0, 190.0, 120.0), (0.0, -30.0, 0.0), 24)
    v["d_air_north"] = ((-120.0, -170.0, 150.0), (0.0, 0.0, -6.0), 24)
    v["d_air_top"] = ((0.0, -2.0, 330.0), (0.0, 0.0, 0.0), 28)
    v["d_town"] = ((34.0, -22.0, 9.0), (-10.0, -70.0, 0.0), 22)
    sp = D.stream_point
    v["a_can_fall"] = ((0.0, 0.0, 1.7), sp(150.0) + (30.0,), 35)
    v["f_stream"] = (sp(66.0, -5.0) + (D.RIM_Z + 1.7,), sp(170.0) + (34.0,), 20)
    v["f_bench2"] = ((-6.0, -84.5, D.RIM_Z + 6.9 + 1.7), (-60.0, -98.0, 4.0), 20)
    v["f_gorge"] = (sp(158.0, 2.0) + (D.STREAM[5]["upper"] + 1.7,), sp(206.0) + (D.POOL_Z + 20.0,), 16)
    v["f_knoll"] = ((0.0, 0.0, 1.7), (0.0, 190.0, -10.0), 50)
    v["f_east"] = ((0.0, 0.0, 1.7), (60.0, 4.0, 6.0), 35)
    v["f_west"] = ((0.0, 0.0, 1.7), (-60.0, -4.0, 6.0), 35)
    return v


NIGHT_VIEWS = (("s", "a_can_s"), ("e", "a_can_e"), ("n", "a_can_n"), ("w", "a_can_w"), ("town", "d_town"), ("air", "d_air_south"))


def review(n, wanted=None):
    folder = os.path.join(OUT, "shots_v%s" % n)
    sc, cam, out = build_scene()
    for name, (eye, target, lens) in views().items():
        if wanted and not any(name.startswith(w) for w in wanted):
            continue
        look(cam, eye, target, lens)
        shoot(sc, name, folder)
        print("SHOT", name)
    if not wanted or "e" in wanted or "e_night" in wanted:
        set_time(sc, True)
        for short, name in NIGHT_VIEWS:
            eye, target, lens = views()[name]
            look(cam, eye, target, lens)
            shoot(sc, "e_night_" + short, folder, True)
            print("SHOT night", name)


# ====================================================================================== the scene saved for the owner
BLEND = os.path.join(ROOT, "ArtSource/payo/payo_assembly.blend")
CAMERAS = (("Can South (1.7 m eye)", "a_can_s"), ("Can East (1.7 m eye)", "a_can_e"), ("Can West (1.7 m eye)", "a_can_w"), ("Can North (1.7 m eye)", "a_can_n"),
           ("Can to the great fall (1.7 m eye)", "a_can_fall"), ("South block to gate (1.7 m eye)", "b_midS_out"), ("Gate stair to court (1.7 m eye)", "c_gate_stair"),
           ("Stream from the rim (1.7 m eye)", "f_stream"), ("Air south", "d_air_south"), ("Air north", "d_air_north"), ("Town from the east rim", "d_town"))


def save_blend(tag=None):
    """The whole assembly as a .blend: one collection of meshes per model, every placement an INSTANCE of it (an empty,
    so the file stays light), collections by family, named cameras, a Day scene and a Night scene. Nothing in it needs
    a script: night is the scene's custom property "night", which the materials read."""
    import time
    t0 = time.time()
    bpy.ops.wm.read_homefile(app_template="")                       # the default file, for its screens (the viewport's shading is saved with it)
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for c in list(bpy.data.collections):
        bpy.data.collections.remove(c)
    sc, cam0, out = build_scene(empty=False)
    sc.name = "Day"
    bpy.data.objects.remove(cam0, do_unlink=True)
    v = views()
    cams = {}
    for label, key in CAMERAS:
        eye, target, lens = v[key]
        cam = bpy.data.objects.new(label, bpy.data.cameras.new(label)); holder("Cameras").objects.link(cam)
        cam.data.clip_start = 0.3; cam.data.clip_end = 6000
        look(cam, eye, target, lens)
        cams[label] = cam
    sc.camera = cams["Can South (1.7 m eye)"]
    # the pieces' pictures: one copy of each distinct picture, packed into the file
    seen = {}
    for im in list(bpy.data.images):
        if im.size[0] == 0:
            continue
        key = (im.name.split(".")[0], im.size[0], im.size[1], im.packed_file.size if im.packed_file else 0)
        if key in seen:
            im.user_remap(seen[key])
        else:
            seen[key] = im
    for im in list(bpy.data.images):
        if im.users == 0:
            bpy.data.images.remove(im)
    bpy.ops.file.pack_all()
    # the Night scene: the same collections, its own sky, moon and lamps
    night = bpy.data.scenes.new("Night")
    for c in sc.collection.children:
        night.collection.children.link(c)
    night.render.engine = sc.render.engine
    night.view_settings.view_transform = "Standard"
    night.unit_settings.system = "METRIC"
    night.render.resolution_x, night.render.resolution_y = sc.render.resolution_x, sc.render.resolution_y
    night.camera = sc.camera
    night["night"] = 1.0; sc["night"] = 0.0
    night.world = bpy.data.worlds["Night sky"]; sc.world = bpy.data.worlds["Day sky"]
    for scene, off in ((sc, ("Sun night", "Night lights")), (night, ("Sun day",))):
        for c in bpy.data.collections:
            c.hide_render = False
        for vl in scene.view_layers:
            for nm in COLLECTIONS:
                lc = find_layer(vl.layer_collection, nm)
                if lc:
                    lc.exclude = nm in off
    for scene in (sc, night):
        scene.display.shading.type = "MATERIAL" if hasattr(scene.display.shading, "type") else scene.display.shading.type
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        space.shading.use_scene_lights = True; space.shading.use_scene_world = True
                        space.clip_start = 0.3; space.clip_end = 6000.0
                        space.overlay.show_relationship_lines = False
                        space.overlay.show_extras = False
                        if space.region_3d:
                            space.region_3d.view_perspective = "CAMERA"
    bpy.context.window.scene = sc
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND, compress=True)
    if tag:
        shutil.copyfile(BLEND, BLEND.replace(".blend", "_%s.blend" % tag))
    print("BLEND", BLEND, "%.1f MB" % (os.path.getsize(BLEND) / 1e6), "objects", len(bpy.data.objects), "meshes", len(bpy.data.meshes), "images", len(bpy.data.images),
          "built in %.0f s" % (time.time() - t0))


def blend_check(folder):
    """Reopen the saved file and render its own Can South camera, day and night (not the build's live scene)."""
    import time
    t0 = time.time()
    bpy.ops.wm.open_mainfile(filepath=BLEND)
    print("BLEND opened in %.1f s" % (time.time() - t0), "scenes", [s_.name for s_ in bpy.data.scenes], "active camera", bpy.context.scene.camera.name)
    os.makedirs(folder, exist_ok=True)
    for scene in bpy.data.scenes:
        scene.render.filepath = os.path.join(folder, "blend_%s_can_south.png" % scene.name.lower())
        scene.render.image_settings.file_format = "PNG"
        bpy.ops.render.render(write_still=True, scene=scene.name)
        print("BLEND rendered", scene.name, scene.render.filepath)


def axis_test():
    """One house, the gate and one block, placed by the rule and described from their geometry."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc, cam = scene_setup()
    out = D.build()
    for m in LAND:
        stand(m, "land", 0, 0, 0, 0.0)
    for p in out["placements"]:
        if p["model"] in ("gate_portal", "gate_guardians") or (p["model"] == "house_shrine" and p.get("where") == "rim"):
            stand(p["model"], p["family"], p["x"], p["y"], p["z"], p["yaw"], p["scale"])
    b = out["blocks"]
    for bid in ("slabE", "core", "slabS", "slabN", "slabW", "cornerSE", "cornerNE", "cornerSW", "cornerNW"):
        e = b[bid]
        model = e["model"]
        stand(model, "stage", e["x"], e["z"], -0.01 if bid != "slabE" else 1.19, e["yaw"])
    st = out["stairs"]["slabE"]
    stand(st["model"], "stage", 8.0 + 14.0, 0.0, 1.19, st["yaw"])            # the stair shown beside its slab (moved 14 m east) to read its ramp
    look(cam, (40.0, 62.0, 55.0), (6.0, -22.0, -6.0), 26)
    sc.render.resolution_x, sc.render.resolution_y = 1400, 900
    shoot(sc, "axis_test", OUT)
    # the description, from the geometry itself
    lines = []
    for model, x, y, yaw in (("gate_portal", 0.0, D.GATE_Y, 0.0), ("house_shrine", 44.5, 0.0, 270.0), ("slab_E", 8.0, 0.0, b["slabE"]["yaw"]), ("stair_E", 8.0, 0.0, st["yaw"])):
        c = load_model(model, "stage")
        M = Matrix.Translation((x, y, 0)) @ Matrix.Rotation(math.pi - math.radians(yaw), 4, "Z")
        pts = []
        for o in c.objects:
            if o.type == "MESH" and not o.name.endswith("_ink"):
                W = M @ o.matrix_world
                pts += [W @ v.co for v in o.data.vertices]
        P = np.array([[p.x, p.y, p.z] for p in pts])
        lo, hi = P.min(axis=0), P.max(axis=0)
        desc = "%s at (%.1f, %.1f) yaw %.0f: spans x %.1f..%.1f, north %.1f..%.1f" % (model, x, y, yaw, lo[0], hi[0], lo[1], hi[1])
        if model == "gate_portal":
            low = P[P[:, 2] < 0.6]
            desc += "; its lowest steps reach north to y %.1f (the court is at y 0) and its back wall stands at y %.1f: THE STAIR FACES THE COURT" % (low[:, 1].max(), lo[1])
        if model == "house_shrine":
            low = P[P[:, 2] < 0.5]
            desc += "; its ground-level stair reaches west to x %.1f, toward the pit: IT FACES THE COURT" % low[:, 0].min()
        if model == "slab_E":
            desc += "; it overhangs its 6 m top by %.2f m on the EAST and %.2f m on the west: THE CARVING IS ON THE OUTER SIDE" % (hi[0] - 11.0, 5.0 - lo[0])
        if model == "stair_E":
            top = P[P[:, 2] > P[:, 2].max() - 1.3]
            east = top[top[:, 0] > 10.5][:, 2].max(); west = top[top[:, 0] < 5.5][:, 2].max()
            desc += "; its top is %.2f m at the west edge and %.2f m at the east edge: THE RAMP DESCENDS TOWARD +x" % (west, east)
        lines.append(desc)
    open(os.path.join(OUT, "axis_test.txt"), "w").write("\n".join(lines) + "\n")
    print("\n".join("AXIS " + l for l in lines))


if MODE == "land":
    make_land()
elif MODE == "write":
    write_game()
elif MODE == "review":
    review(ARGS[1], ARGS[2:] or None)
elif MODE == "axis":
    axis_test()
elif MODE == "blend":
    save_blend(ARGS[1] if len(ARGS) > 1 else None)
elif MODE == "blendcheck":
    blend_check(ARGS[1])
