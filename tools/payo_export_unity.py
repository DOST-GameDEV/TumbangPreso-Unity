"""PAYO TO UNITY (2026-10-18): the owner-approved dressed scene (ArtSource/payo/payo_dressed_d53.blend, made by
tools/payo_dress.py -- v59 d53 on land v59) written into the game's art files.

  blender -b --python tools/payo_export_unity.py -- export     land chunks, water, pieces, payo_art.json, materials_payo.json
  python tools/payo_export_unity.py --check                    counts, missing glbs, unknown materials, triangles per chunk

The approved files are only READ. Nothing in ArtSource/payo/approved-2026-10-18/ or pieces-approved-2026-10-12/ is
written; d53 is opened and never saved. The first run backs up the game's two json files to
Logs/payo/unity_export/backup/ (a later run keeps that first backup).

AXES (as tools/payo_assemble.py). The dressed scene is in the layout frame: x east, y north, z up. Unity is (x, y up,
z north), so a point (x, y, z) is Unity (x, z, y). A piece's glb is plain glTF (y up, +z its front); glTFast shows a
glTF point (gx, gy, gz) at Unity (-gx, gy, gz). Blender imports a glb as (gx, -gz, gy), so a piece placed in Blender
with rotation `r` about Z is Unity yaw (180 - r degrees). A Blender scale (sx, sy, sz) of a piece is Unity localScale
(sx, sz, sy). The meshes MADE here (land chunks, water) are written straight in glTF as (-x, z, y), so glTFast puts
them back at Unity (x, z, y): place them at the origin with yaw 0.

THE LAND. "Land" (smooth everywhere) and "Land terraces" (sharp over 30 degrees) share one point list; they are joined
into one mesh, with every edge between the two parts marked sharp, so the normals are exactly the two objects' own.
Chunks are 256 m squares of the layout frame (a triangle goes to the chunk its polygon centre is in). A chunk any part
of which is nearer the court (the origin) than 700 m is NEVER decimated. Farther chunks are collapse-decimated
(Blender's quadric collapse, which keeps creases such as the terrace cuts) to 40% (700 to 1200 m) and 22% (beyond),
with every point a near chunk or the other ring also uses pinned, so no crack opens between rings.
"""
import json
import math
import os
import shutil
import struct
import sys

import numpy as np

try:
    import bpy
except ImportError:
    bpy = None

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Payo")
MODELS = os.path.join(ART, "Models")
TEXTURES = os.path.join(ART, "Textures")
LAND_TEX = os.path.join(TEXTURES, "land")
OUT = os.path.join(ROOT, "Logs", "payo", "unity_export")
BACKUP = os.path.join(OUT, "backup")
D53 = os.path.join(ROOT, "ArtSource", "payo", "payo_dressed_d53.blend")
CALM = os.path.join(ROOT, "Logs", "payo", "pieces", "calm")
APPROVED_12 = os.path.join(ROOT, "ArtSource", "payo", "pieces-approved-2026-10-12")
FROZEN_18 = os.path.join(ROOT, "ArtSource", "payo", "approved-2026-10-18")
BUILD = os.path.join(ROOT, "Logs", "payo", "pieces", "build")
LAGOON = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "LagoonCove")
ESKINITA = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "EskinitaAlley")
TEX_CALM = os.path.join(ROOT, "Logs", "payo", "terrain", "tex_calm")

CHUNK = 256.0
NEAR_M, MID_M = 700.0, 1200.0
RATIO = {1: 0.40, 2: 0.22}
LAND_IDS = ["py_land_slope", "py_land_wall", "py_land_paddy", "py_land_rock", "py_land_paving", "py_land_flooded", "py_land_town_wall"]
LAND_SLOTS = {"land slope": 0, "land terrace wall": 1, "land paddy": 2, "land rock": 3, "land paving": 4, "land flooded paddy": 5, "land town wall": 6}
WATER = {"River": "payo_river", "Water stair": "payo_water_stair", "Inlet water": "payo_inlet_water", "Weir water": "payo_weir_water",
         "Basin water": "payo_basin_water", "Sacred pool": "payo_sacred_pool", "Gorge water": "payo_gorge_water"}
WATER_MAT = {"river": "py_river", "river rapids": "py_river_rapids"}
# the d53 collection a piece stands in -> the builder's group ("plant" sways, everything else is static)
PLANT_SETS = ("Trees", "Flora")
# where a player could stand if the play walls (wallHalf 22 m) were ever opened: given colliders when the builder honours `collide`
COLLIDE_MODELS = {"court_platform", "pool_basin", "head_fall", "cliff_wall"}
COLLIDE_LAND_M = 300.0

# The calm land paint as tools/payo_land_blend.py CALM_TEX (metres per tile, relief depth) and the pictures each surface reads.
SURFACES = {
    "py_land_slope": ("slope", "grass", 20.0, 0.12, ["grass_value.png", "grass_height.png", "grass_normal.png", "macro_value.png"]),
    "py_land_wall": ("wall", "earth", 6.0, 0.06, ["earth_color.png", "earth_height.png", "earth_normal.png", "grass_value.png", "grass_height.png", "grass_normal.png", "macro_value.png"]),
    "py_land_paddy": ("paddy", "paddy", 12.0, 0.02, ["paddy_value.png", "paddy_height.png", "paddy_normal.png", "paddy_rows.png", "paddy_rows.txt", "rice_density.png", "rice_density.txt", "macro_value.png"]),
    "py_land_rock": ("rock", "rock", 16.0, 0.22, ["rock_value.png", "rock_height.png", "rock_normal.png", "macro_value.png"]),
    "py_land_paving": ("paving", "paving", 10.0, 0.05, ["paving_value.png", "paving_height.png", "paving_normal.png"]),
    "py_land_flooded": ("flooded", "flooded", 14.0, 0.025, ["flooded_value.png", "flooded_height.png", "flooded_normal.png"]),
    "py_land_town_wall": ("town_wall", "bank", [12.0, 12.0, 5.0], 0.097, ["bank_color.png", "bank_value.png", "bank_shade.png", "bank_height.png", "bank_normal.png", "bank_mean.txt"]),
    "py_river": ("water", "water", 24.0, 0.10, ["water_value.png", "water_height.png", "water_normal.png"]),
    "py_river_rapids": ("foam", "foam", 10.0, 0.10, ["foam_value.png", "foam_height.png", "foam_normal.png"]),
}
LAGOON_TEXTURES = {"leaf_round", "blade", "croton_leaf", "monstera_leaf", "stalk", "gumamela", "bougainvillea"}


# ====================================================================================== glb
def read_glb(path):
    """(json, bin) of a .glb."""
    b = open(path, "rb").read()
    n = struct.unpack("<I", b[12:16])[0]
    doc = json.loads(b[20:20 + n].decode("utf-8"))
    rest = b[20 + n:]
    binary = rest[8:8 + struct.unpack("<I", rest[:4])[0]] if len(rest) >= 8 else b""
    return doc, binary


def glb_info(path):
    """Materials, triangles, whether it carries COLOR_0, image names."""
    doc, _ = read_glb(path)
    mats = [m.get("name", "?") for m in doc.get("materials", [])]
    tris, vcol, used = 0, False, set()
    for me in doc.get("meshes", []):
        for pr in me["primitives"]:
            if "indices" in pr:
                tris += doc["accessors"][pr["indices"]]["count"] // 3
            else:
                tris += doc["accessors"][pr["attributes"]["POSITION"]]["count"] // 3
            vcol = vcol or "COLOR_0" in pr["attributes"]
            if "material" in pr:
                used.add(mats[pr["material"]])
    images = [i.get("name") or i.get("uri") for i in doc.get("images", [])]
    tex_of = {}
    for m in doc.get("materials", []):
        t = (m.get("pbrMetallicRoughness") or {}).get("baseColorTexture")
        if t is not None and "textures" in doc:
            src = doc["textures"][t["index"]].get("source")
            if src is not None:
                tex_of[m.get("name")] = images[src]
    return dict(materials=sorted(used), tris=tris, vcol=vcol, images=images, texture_of=tex_of)


def write_glb(path, mesh_name, prims):
    """prims: [dict(material, pos (n,3), nrm (n,3), col (n,4) or None, uv (n,2), idx (m,))], all already glTF axes."""
    blob = bytearray()
    views, accessors = [], []

    def add(arr, target, comp, typ, minmax=False):
        arr = np.ascontiguousarray(arr)
        while len(blob) % 4:
            blob.append(0)
        off = len(blob)
        blob.extend(arr.tobytes())
        views.append(dict(buffer=0, byteOffset=off, byteLength=arr.nbytes, target=target))
        acc = dict(bufferView=len(views) - 1, componentType=comp, count=int(arr.shape[0]), type=typ)
        if minmax:
            acc["min"] = [float(v) for v in arr.min(axis=0)]; acc["max"] = [float(v) for v in arr.max(axis=0)]
        accessors.append(acc)
        return len(accessors) - 1

    materials, primitives = [], []
    for p in prims:
        if len(p["idx"]) == 0:
            continue
        if p["material"] not in materials:
            materials.append(p["material"])
        at = {"POSITION": add(p["pos"].astype(np.float32), 34962, 5126, "VEC3", True),
              "NORMAL": add(p["nrm"].astype(np.float32), 34962, 5126, "VEC3")}
        if p.get("uv") is not None:
            at["TEXCOORD_0"] = add(p["uv"].astype(np.float32), 34962, 5126, "VEC2")
        if p.get("col") is not None:
            at["COLOR_0"] = add(p["col"].astype(np.float32), 34962, 5126, "VEC4")
        big = len(p["pos"]) > 65535
        idx = add(p["idx"].astype(np.uint32 if big else np.uint16), 34963, 5125 if big else 5123, "SCALAR")
        primitives.append(dict(attributes=at, indices=idx, material=materials.index(p["material"]), mode=4))
    while len(blob) % 4:
        blob.append(0)
    doc = dict(asset=dict(version="2.0", generator="tools/payo_export_unity.py"), scene=0, scenes=[dict(nodes=[0])],
               nodes=[dict(name=mesh_name, mesh=0)], meshes=[dict(name=mesh_name, primitives=primitives)],
               materials=[dict(name=m, pbrMetallicRoughness=dict(baseColorFactor=[1, 1, 1, 1], metallicFactor=0.0, roughnessFactor=1.0)) for m in materials],
               accessors=accessors, bufferViews=views, buffers=[dict(byteLength=len(blob))])
    js = json.dumps(doc, separators=(",", ":")).encode("utf-8")
    js += b" " * ((4 - len(js) % 4) % 4)
    with open(path, "wb") as f:
        f.write(struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(blob)))
        f.write(struct.pack("<II", len(js), 0x4E4F534A)); f.write(js)
        f.write(struct.pack("<II", len(blob), 0x004E4942)); f.write(bytes(blob))


def to_gltf(v):
    """Layout frame (x east, y north, z up) to glTF as glTFast reads it back to Unity (x, z, y)."""
    return np.stack([-v[:, 0], v[:, 2], v[:, 1]], axis=1)


def primitives_from(co, corner_vert, corner_nrm, corner_col, tri_corners, tri_mat, names):
    """One primitive per material id: the triangles' corners, with points shared where vertex, normal and colour agree."""
    out = []
    for mid in np.unique(tri_mat):
        c = tri_corners[tri_mat == mid].ravel()
        v = corner_vert[c]
        nq = np.round(corner_nrm[c] * 2000.0).astype(np.int64)
        key = [v.astype(np.int64)[:, None], nq]
        if corner_col is not None:
            key.append(np.round(corner_col[c] * 8192.0).astype(np.int64))
        key = np.concatenate(key, axis=1)
        _, first, inv = np.unique(key, axis=0, return_index=True, return_inverse=True)
        cf = c[first]
        p = co[corner_vert[cf]]
        n = corner_nrm[cf]
        n = n / np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-9)
        out.append(dict(material=names[int(mid)], pos=to_gltf(p), nrm=to_gltf(n),
                        col=None if corner_col is None else corner_col[cf],
                        uv=np.stack([p[:, 0], 1.0 - p[:, 1]], axis=1),      # glTFast flips v: Unity reads (x, y north) in metres
                        idx=inv.ravel().astype(np.int64)))
    return out


def chunk_name(ix, iy):
    return "payo_land_x%s_y%s" % (("m%d" % -ix) if ix < 0 else str(ix), ("m%d" % -iy) if iy < 0 else str(iy))


def chunk_min_dist(ix, iy):
    dx = max(ix * CHUNK, -(ix + 1) * CHUNK, 0.0)
    dy = max(iy * CHUNK, -(iy + 1) * CHUNK, 0.0)
    return math.hypot(dx, dy)


def tier_of(ix, iy):
    d = chunk_min_dist(ix, iy)
    return 0 if d < NEAR_M else 1 if d < MID_M else 2


# ====================================================================================== where a piece's glb is
def find(name):
    """tools/payo_dress.py find(), in its order: the calm repaint, the 10-12 approved pieces, the builders' later pieces,
    the game's Payo models, Eskinita, Lagoon."""
    if os.path.isdir(CALM):
        for fam in os.listdir(CALM):
            p = os.path.join(CALM, fam, name + ".glb")
            if os.path.isdir(os.path.join(CALM, fam)) and os.path.exists(p):
                return p
    for d in os.listdir(APPROVED_12):
        p = os.path.join(APPROVED_12, d, name + ".glb")
        if os.path.exists(p):
            return p
    for p in (os.path.join(BUILD, "stone", name + ".glb"), os.path.join(BUILD, "terrace", name + ".glb"), os.path.join(MODELS, name + ".glb"),
              os.path.join(ESKINITA, "Models", name + ".glb"), os.path.join(LAGOON, "Models", name + ".glb")):
        if os.path.exists(p):
            return p
    return None


# ====================================================================================== Blender: the land
def mesh_arrays(me):
    nv, nl, npoly = len(me.vertices), len(me.loops), len(me.polygons)
    co = np.empty(nv * 3, np.float32); me.vertices.foreach_get("co", co)
    lv = np.empty(nl, np.int32); me.loops.foreach_get("vertex_index", lv)
    tot = np.empty(npoly, np.int32); me.polygons.foreach_get("loop_total", tot)
    st = np.empty(npoly, np.int32); me.polygons.foreach_get("loop_start", st)
    assert np.array_equal(st, np.concatenate([[0], np.cumsum(tot)[:-1]])), me.name + ": loops not in polygon order"
    mat = np.empty(npoly, np.int32); me.polygons.foreach_get("material_index", mat)
    remap = np.array([LAND_SLOTS[m.name.split(".")[0]] for m in me.materials], np.int32)
    col = np.empty(nl * 4, np.float32); me.color_attributes["Col"].data.foreach_get("color", col)
    return co.reshape(-1, 3), lv, tot, remap[mat], col.reshape(-1, 4)


def poly_centres(me):
    c = np.empty(len(me.polygons) * 3, np.float32); me.polygons.foreach_get("center", c)
    return c.reshape(-1, 3)


def poly_tiers(me):
    c = poly_centres(me)
    ix = np.floor(c[:, 0] / CHUNK).astype(np.int64); iy = np.floor(c[:, 1] / CHUNK).astype(np.int64)
    dx = np.maximum(np.maximum(ix * CHUNK, -(ix + 1) * CHUNK), 0.0)      # distance from the origin to the chunk's square
    dy = np.maximum(np.maximum(iy * CHUNK, -(iy + 1) * CHUNK), 0.0)
    d = np.hypot(dx, dy)
    return np.where(d < NEAR_M, 0, np.where(d < MID_M, 1, 2)).astype(np.int32), ix, iy


def build_land():
    L, T = bpy.data.objects["Land"], bpy.data.objects["Land terraces"]
    assert L.matrix_world.is_identity and T.matrix_world.is_identity
    co, lv0, tot0, mat0, col0 = mesh_arrays(L.data)
    co1, lv1, tot1, mat1, col1 = mesh_arrays(T.data)
    assert np.array_equal(co, co1), "Land and Land terraces no longer share one point list"
    src_tris = int((tot0 - 2).sum() + (tot1 - 2).sum())
    me = bpy.data.meshes.new("payo_land_joined")
    me.vertices.add(len(co)); me.vertices.foreach_set("co", co.ravel())
    lv = np.concatenate([lv0, lv1]); tot = np.concatenate([tot0, tot1])
    me.loops.add(len(lv)); me.loops.foreach_set("vertex_index", lv)
    me.polygons.add(len(tot))
    me.polygons.foreach_set("loop_start", np.concatenate([[0], np.cumsum(tot)[:-1]]).astype(np.int32))
    me.polygons.foreach_set("loop_total", tot)
    me.update()
    for n in LAND_IDS:
        me.materials.append(bpy.data.materials.new(n))
    me.polygons.foreach_set("material_index", np.concatenate([mat0, mat1]).astype(np.int32))
    ca = me.color_attributes.new("Col", "FLOAT_COLOR", "CORNER"); ca.data.foreach_set("color", np.concatenate([col0, col1]).ravel())
    part = me.attributes.new("part", "INT", "FACE"); part.data.foreach_set("value", np.concatenate([np.zeros(len(tot0), np.int32), np.ones(len(tot1), np.int32)]))
    ob = bpy.data.objects.new("payo_land_joined", me)
    bpy.context.scene.collection.objects.link(ob)
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    print("LAND joined", len(co), "points", len(tot), "polygons", src_tris, "triangles")
    # the far rings, each with every point another ring also uses pinned (edit-mode decimate never moves an unselected point)
    bpy.context.tool_settings.mesh_select_mode = (True, False, False)
    for t in (1, 2):
        tiers, _, _ = poly_tiers(me)
        tl = np.empty(len(me.polygons), np.int32); me.polygons.foreach_get("loop_total", tl)
        lvx = np.empty(len(me.loops), np.int32); me.loops.foreach_get("vertex_index", lvx)
        ltier = np.repeat(tiers, tl)
        inside = np.zeros(len(me.vertices), bool); other = np.zeros(len(me.vertices), bool)
        inside[lvx[ltier == t]] = True; other[lvx[ltier != t]] = True
        sel = inside & ~other
        for nm in (".select_edge", ".select_poly", ".select_vert"):          # selection lives in these attributes (the RNA setters crash on a mesh without them)
            a = me.attributes.get(nm)
            if a is not None:
                me.attributes.remove(a)
        me.attributes.new(".select_vert", "BOOLEAN", "POINT").data.foreach_set("value", sel)
        before = int((tl[tiers == t] - 2).sum())
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.decimate(ratio=RATIO[t])
        bpy.ops.object.mode_set(mode="OBJECT")
        tiers2, _, _ = poly_tiers(me)
        tl2 = np.empty(len(me.polygons), np.int32); me.polygons.foreach_get("loop_total", tl2)
        print("LAND ring", t, "triangles", before, "->", int((tl2[tiers2 == t] - 2).sum()))
    # smooth everywhere; sharp where the two parts meet, and within the terraces over 30 degrees (payo_land_blend.py)
    me.shade_smooth()
    pa = np.empty(len(me.polygons), np.int32); me.attributes["part"].data.foreach_get("value", pa)
    pn = np.empty(len(me.polygons) * 3, np.float32); me.polygons.foreach_get("normal", pn); pn = pn.reshape(-1, 3)
    le = np.empty(len(me.loops), np.int32); me.loops.foreach_get("edge_index", le)
    tl = np.empty(len(me.polygons), np.int32); me.polygons.foreach_get("loop_total", tl)
    lp = np.repeat(np.arange(len(me.polygons)), tl)
    order = np.argsort(le, kind="stable"); les, lps = le[order], lp[order]
    count = np.bincount(le, minlength=len(me.edges))
    first = np.concatenate([[0], np.cumsum(count)[:-1]])
    sharp = count != 2
    two = np.where(count == 2)[0]
    f1, f2 = lps[first[two]], lps[first[two] + 1]
    dot = (pn[f1] * pn[f2]).sum(axis=1)
    sharp[two] = (pa[f1] != pa[f2]) | ((pa[f1] == 1) & (dot < math.cos(math.radians(30.0))))
    sa = me.attributes.get("sharp_edge") or me.attributes.new("sharp_edge", "BOOLEAN", "EDGE")
    sa.data.foreach_set("value", sharp)
    me.update()
    return ob


def export_land(ob):
    me = ob.data
    nl = len(me.loops)
    co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    lv = np.empty(nl, np.int32); me.loops.foreach_get("vertex_index", lv)
    cn = np.empty(nl * 3, np.float32); me.corner_normals.foreach_get("vector", cn); cn = cn.reshape(-1, 3)
    col = np.empty(nl * 4, np.float32); me.color_attributes["Col"].data.foreach_get("color", col); col = col.reshape(-1, 4)
    me.calc_loop_triangles()
    nt = len(me.loop_triangles)
    tc = np.empty(nt * 3, np.int32); me.loop_triangles.foreach_get("loops", tc); tc = tc.reshape(-1, 3)
    tp = np.empty(nt, np.int32); me.loop_triangles.foreach_get("polygon_index", tp)
    pm = np.empty(len(me.polygons), np.int32); me.polygons.foreach_get("material_index", pm)
    tiers, ix, iy = poly_tiers(me)
    tix, tiy, tm = ix[tp], iy[tp], pm[tp]
    chunks = {}
    for a, b in np.unique(np.stack([tix, tiy], 1), axis=0):
        a, b = int(a), int(b)
        sel = (tix == a) & (tiy == b)
        name = chunk_name(a, b)
        prims = primitives_from(co, lv, cn, col, tc[sel], tm[sel], LAND_IDS)
        write_glb(os.path.join(MODELS, name + ".glb"), name, prims)
        chunks[name] = dict(ix=a, iy=b, tier=tier_of(a, b), min_dist_m=round(chunk_min_dist(a, b), 1), tris=int(sel.sum()),
                            points=int(sum(len(p["pos"]) for p in prims)), materials=sorted({p["material"] for p in prims}))
    print("LAND chunks", len(chunks), "triangles", nt)
    return chunks


def export_object(ob, name, matmap, colour=None):
    """A small mesh object (water, the low ground) as its own glb in world space."""
    me = ob.data.copy()
    me.transform(ob.matrix_world)
    nl = len(me.loops)
    co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    lv = np.empty(nl, np.int32); me.loops.foreach_get("vertex_index", lv)
    cn = np.empty(nl * 3, np.float32); me.corner_normals.foreach_get("vector", cn); cn = cn.reshape(-1, 3)
    me.calc_loop_triangles()
    nt = len(me.loop_triangles)
    tc = np.empty(nt * 3, np.int32); me.loop_triangles.foreach_get("loops", tc); tc = tc.reshape(-1, 3)
    tm = np.empty(nt, np.int32); me.loop_triangles.foreach_get("material_index", tm)
    names = [matmap[m.name.split(".")[0]] for m in me.materials]
    col = None if colour is None else np.tile(np.array(colour, np.float32), (nl, 1))
    write_glb(os.path.join(MODELS, name + ".glb"), name, primitives_from(co, lv, cn, col, tc, tm, names))
    bpy.data.meshes.remove(me)
    return nt


# ====================================================================================== Blender: the pieces
def read_pieces():
    layout = json.load(open(os.path.join(ART, "payo_layout.json")))
    tops = {(round(float(p["x"]), 2), round(float(p["z"]), 2)): p for p in layout["layouts"][0]["pieces"] if p.get("kind") == "block"}
    rows, stage = [], {}
    for o in bpy.data.objects:
        if o.instance_type != "COLLECTION" or not o.instance_collection or not o.instance_collection.name.startswith("lib_"):
            continue
        model = o.instance_collection.name[4:]
        grp = o.users_collection[0].name
        x, y, z = o.matrix_world.translation
        rx, ry, rz = o.rotation_euler
        assert abs(rx) < 1e-5 and abs(ry) < 1e-5 and o.parent is None, o.name
        yaw = round((180.0 - math.degrees(rz)) % 360.0, 3) % 360.0
        s = o.scale
        if grp == "Stage":
            p = tops.get((round(x, 2), round(y, 2)))
            assert p is not None, "a stage piece at (%.2f, %.2f) is on no block of layout 0" % (x, y)
            e = stage.setdefault(p["id"], {})
            e["extension" if model.startswith("ext_") else "model"] = model
            if not model.startswith("ext_"):
                e["yaw"] = yaw; e["dy"] = round(z - (float(p["top"]) - 0.01), 4)
            continue
        row = dict(model=model, position=[round(x, 3), round(z, 3), round(y, 3)], yaw=yaw, scale=round(s.x, 4),
                   group="plant" if grp in PLANT_SETS or model.startswith(("tree_", "payo_plant_palm")) else "static", family=grp.lower())
        if abs(s.x - s.y) > 1e-4 or abs(s.x - s.z) > 1e-4:
            row["scale3"] = [round(s.x, 4), round(s.z, 4), round(s.y, 4)]
        if model in COLLIDE_MODELS:
            row["collide"] = True
        rows.append(row)
    return rows, stage


# ====================================================================================== writing the game's files
def material_kind(name):
    if name.endswith("_glow"):
        return "glow"
    return {"py_water": "water", "py_fall": "fall", "py_foam": "foam", "py_mist": "mist", "py_wet": "decal", "py_terrace_plant": "foliage card",
            "py_terrace_far": "foliage card", "py_mayari": "unlit", "py_cloud": "cloud"}.get(name, "painted")


def copy_glb(model, report):
    src = find(model)
    if src is None:
        report["missing_source"].append(model); return None
    for fam in ("court", "head"):                       # the authored pieces must match their frozen approved copy
        fz = os.path.join(FROZEN_18, "calm", fam, model + ".glb")
        if os.path.exists(fz) and open(fz, "rb").read() != open(src, "rb").read():
            report["differs_from_frozen"].append(model)
    dst = os.path.join(MODELS, model + ".glb")
    if os.path.abspath(src) != os.path.abspath(dst) and (not os.path.exists(dst) or open(src, "rb").read() != open(dst, "rb").read()):
        shutil.copyfile(src, dst); report["copied"].append(model)
    return src


def write_game(rows, stage, chunks, water_tris, low_tris):
    os.makedirs(BACKUP, exist_ok=True)
    for f in ("payo_art.json", "materials_payo.json"):
        if not os.path.exists(os.path.join(BACKUP, f)):
            shutil.copyfile(os.path.join(ART, f), os.path.join(BACKUP, f))
    old = json.load(open(os.path.join(BACKUP, "payo_art.json")))
    old_m = {e["name"]: e for e in json.load(open(os.path.join(BACKUP, "materials_payo.json")))["materials"]}
    report = dict(copied=[], missing_source=[], differs_from_frozen=[], block_yaw_changed=[], dropped=[])

    # ---- the stage: the art's table by block id, models and turns as d53 placed them
    blocks = []
    for b in old["blocks"]:
        b = dict(b)
        d = stage.get(b["id"])
        if d:
            # d53 turned every block to "face out" (tools/payo_dress.py), which turns the oblong ones a quarter and the corners an
            # eighth: their tops then no longer lie on their collision footprints (and overlapped, which is why d53 needed `dy`).
            # The 10-12 assembly's turns (checked by its axis test) keep each top on its footprint, so they are kept.
            if b.get("yaw") != d["yaw"]:
                report["block_yaw_changed"].append("%s kept %s (d53 %s)" % (b["id"], b.get("yaw"), d["yaw"]))
            assert b["model"] == d["model"] and b.get("extension") == d.get("extension"), b["id"]
            b["dy"] = d["dy"]
        else:
            b["note"] = "not in layout 0, so d53 did not place it: model and yaw kept from the 2026-10-12 assembly"
        blocks.append(b)
    movers, pads = old["movers"], old["pads"]

    # ---- placements: the land at the origin, then every d53 piece
    land_rows = []
    for name, c in sorted(chunks.items()):
        r = dict(model=name, position=[0.0, 0.0, 0.0], yaw=0.0, scale=1.0, group="static", family="land", lod_tier=c["tier"])
        if c["min_dist_m"] < COLLIDE_LAND_M:
            r["collide"] = True
        land_rows.append(r)
    land_rows.append(dict(model="payo_land_low", position=[0.0, 0.0, 0.0], yaw=0.0, scale=1.0, group="static", family="land"))
    for name in sorted(water_tris):
        land_rows.append(dict(model=name, position=[0.0, 0.0, 0.0], yaw=0.0, scale=1.0, group="static", family="water"))
    sky = old["sky"]
    land_rows.append(dict(model=sky["model"], position=[0.0, 0.0, 0.0], yaw=0.0, scale=1.0, group="static", family="far"))
    placements = land_rows + rows

    # ---- the glbs
    used = sorted({r["model"] for r in rows} | {b[k] for b in blocks for k in ("model", "extension") if b.get(k)}
                  | {b["stair"]["model"] for b in blocks if b.get("stair")} | {m["model"] for m in movers.values()} | set(pads.values())
                  | {e["model"] for e in old["life"]} | {old["catch"]["model"], sky["model"]})
    sources = {m: copy_glb(m, report) for m in used}
    made = sorted(chunks) + ["payo_land_low"] + sorted(water_tris)
    info = {m: glb_info(os.path.join(MODELS, m + ".glb")) for m in used + made if os.path.exists(os.path.join(MODELS, m + ".glb"))}

    # ---- textures: the calm land paint, and Lagoon's leaf drawings the copied plants need (the builder reads Payo/Textures only)
    os.makedirs(LAND_TEX, exist_ok=True)
    land_files = sorted({f for s in SURFACES.values() for f in s[4]})
    for f in land_files:
        shutil.copyfile(os.path.join(TEX_CALM, f), os.path.join(LAND_TEX, f))

    # ---- materials
    lagoon_spec = {m["name"]: m for m in json.load(open(os.path.join(LAGOON, "lagoon_cove_layout.json")))["materials"]}
    esk_spec = {m["name"]: m for m in json.load(open(os.path.join(ESKINITA, "eskinita_alley_layout.json")))["materials"]}
    mats = {}
    for m in used:
        if m not in info:
            continue
        for mn in info[m]["materials"]:
            e = mats.setdefault(mn, dict(models=[], vcol=False, tex=None))
            e["models"].append(m); e["vcol"] = e["vcol"] or info[m]["vcol"]
            e["tex"] = e["tex"] or info[m]["texture_of"].get(mn)
    out_m, lagoon_copied = [], []
    for name in sorted(mats):
        e = mats[name]
        models = e["models"] if len(e["models"]) <= 16 else e["models"][:16] + ["... %d more" % (len(e["models"]) - 16)]
        if name.startswith("lagoon_") and name in lagoon_spec:
            s = lagoon_spec[name]
            tex = s["albedo"][:-len("_albedo.png")] if s.get("albedo") else None
            for suffix in ("_albedo.png", "_normal.png"):
                src = os.path.join(LAGOON, "Textures", (tex or "") + suffix)
                dst = os.path.join(TEXTURES, (tex or "") + suffix)
                if tex and os.path.exists(src) and (not os.path.exists(dst) or open(src, "rb").read() != open(dst, "rb").read()):
                    shutil.copyfile(src, dst); lagoon_copied.append(os.path.basename(dst))
            out_m.append(dict(name=name, kind="foliage card" if s["kind"] == "foliage" else "painted", texture=tex, tint=s["tint"][:3],
                              tint_linear=next((x["value"] for x in s.get("extra", []) if x.get("key") == "tintLinear"), None),
                              models=models, vertex_colour=False, two_sided=bool(s.get("twoSided")), cutout=s["kind"] == "foliage",
                              texture_from="Textures/<texture>_albedo.png (copied from LagoonCove/Textures; spec from lagoon_cove_layout.json)"))
            continue
        if name in old_m:
            o = dict(old_m[name]); o["models"] = models
            if name in ("py_mist", "py_foam", "py_fall", "py_water") and "head_fall" in e["models"]:
                o["note"] = (o.get("note", "") + " head_fall carries it (the great fall at the valley's head).").strip()
            out_m.append(o); continue
        if name in esk_spec:
            o = dict(esk_spec[name]); o["kind"] = "foliage card" if o.get("foliage") else "painted"; o["models"] = models
            o["vertex_colour"] = e["vcol"]; o["texture_from"] = "Textures/<texture>_albedo.png (Payo already has it; spec from eskinita_alley_layout.json)"
            out_m.append(o); continue
        kind = material_kind(name)
        o = dict(name=name, kind=kind, texture=e["tex"], models=models, vertex_colour=e["vcol"],
                 texture_from="embedded in each glb that uses it" if e["tex"] else "none (a flat tint)",
                 two_sided=kind in ("foliage card", "fall", "mist", "decal"), cutout=kind == "foliage card")
        if name == "py_court_stone":
            o["vertex_colour"] = True; o["note"] = "the court platform, pit and gorge (court_platform) and the sacred pool's rim (pool_basin): its own atlas and normal map inside the glb, times vertex colour"
        if name == "py_head_rock":
            o["vertex_colour"] = False; o["note"] = "the head's cliff (head_fall): its own picture and normal map inside the glb, NO vertex colour"
        out_m.append(o)
    have = {o["name"] for o in out_m}
    for name, (surface, tex, tile, relief, files) in SURFACES.items():
        users = [c for c, v in chunks.items() if name in v["materials"]] + ["payo_land_low" if name == "py_land_slope" else None] \
                + [w for w in water_tris if name in info.get(w, {}).get("materials", [])]
        users = [u for u in users if u]
        out_m.append(dict(name=name, kind="payo_land", surface=surface, models=users if len(users) <= 16 else users[:16] + ["... %d more" % (len(users) - 16)],
                          vertex_colour=name not in ("py_river", "py_river_rapids"), texture=None, texture_set=tex, tile_m=tile, relief_m=relief,
                          textures=["Textures/land/" + f for f in files], world_uv="UV0 is (x, z) in metres; the shader may project from world space instead",
                          note="COLOR_0 is the face's own colour, LINEAR float; the calm paint multiplies it (tools/payo_land_blend.py calm_paint)."
                          if name not in ("py_river", "py_river_rapids") else "the river ribbon and its sheets; no vertex colour"))
        have.add(name)

    # ---- night: the old lists, the fires and lamps where d53 stands them
    lights = []
    for p in rows:
        if p["model"] in ("fire_bowl", "torch", "hearth_pot", "lantern_pole"):
            h = {"fire_bowl": 1.6, "torch": 2.3, "hearth_pot": 0.5, "lantern_pole": 2.3}[p["model"]] * p["scale"]
            lights.append(dict(position=[p["position"][0], round(p["position"][1] + h, 2), p["position"][2]], model=p["model"],
                               range=9.0 if p["model"] == "fire_bowl" else 6.0))
    night = dict(old["night"]); night["lights"] = lights
    all_nodes = set()
    for m in used:
        if os.path.exists(os.path.join(MODELS, m + ".glb")):
            all_nodes.update(n.get("name", "") for n in read_glb(os.path.join(MODELS, m + ".glb"))[0].get("nodes", []))
    report["night_nodes_absent"] = [n for n in night["nodes_lit_at_night"] if n not in all_nodes]
    report["glow_materials_absent"] = [g for g in night["glow_materials"] if g not in mats]
    life_missing = [e["model"] for e in old["life"] if e["model"] not in info]

    counts = {}
    for r in placements:
        counts[r["family"]] = counts.get(r["family"], 0) + 1
    tri_family = {}
    for r in placements:
        tri_family[r["family"]] = tri_family.get(r["family"], 0) + info.get(r["model"], {}).get("tris", 0)
    doc = {
        "note": "Written by tools/payo_export_unity.py from the owner-approved scene ArtSource/payo/payo_dressed_d53.blend (2026-10-18; frozen copy in "
                "ArtSource/payo/approved-2026-10-18). Unity axes. Positions are (x, y up, z north); yaw is degrees clockwise from above.",
        "axes": old["axes"] + " 2026-10-18: the land is payo_land_x<i>_y<j> (256 m chunks of the layout frame, chunk i covering x 256 i .. 256 (i + 1), "
                "'m' a minus), payo_land_low (the plain under everything) and the water ribbons payo_river, payo_water_stair, payo_inlet_water, payo_weir_water: "
                "all made here, already turned, placed at the origin with yaw 0. `scale3` (Unity localScale x, y, z) is given where a piece is NOT scaled "
                "uniformly (the cliff_wall run below the pit's open front is 1.35 tall); `scale` is then its x.",
        "layout_read": old["layout_read"],
        "placements": placements,
        "blocks": blocks,
        "blocks_note": old["blocks_note"] + " 2026-10-18: models are the calm repaint (same names), yaws are the 10-12 assembly's (d53 turned them to face out, "
                       "which put oblong and corner tops off their collision footprints). "
                       "`dy` is d53's own height offset from (top - 0.01): every block a few millimetres apart so no two tops share a plane (they flickered, owner 2026-10-18); "
                       "add it to the block's (and its extension's) y.",
        "movers": movers, "movers_note": old["movers_note"], "pads": pads, "pads_note": old["pads_note"],
        "hide_solids": old["hide_solids"],
        "night": night,
        "sky": sky,
        "life": old["life"],
        "life_note": old["life_note"] + " (2026-10-18: carried over UNCHANGED from the 10-12 assembly; their positions stand on THAT land, not on land v59.)",
        "catch": old["catch"],
        "vertex_colour": "The land chunks (py_land_*: COLOR_0 the face's own colour, linear) and the court stone (py_court_stone), the calm terrace kit, the life figures "
                         "and the kept trees carry vertex colour their materials multiply by; see `vertex_colour` per material in materials_payo.json. "
                         "py_head_rock and the Lagoon and Eskinita plants do not.",
        "collision": "THE BUILDER STRIPS EVERY COLLIDER FROM THE ART; play is on the stage's own boxes inside the play walls (wallHalf 22 m) and nothing outside them is "
                     "reachable. `collide: true` marks what should get a MeshCollider if that ever changes: the land chunks nearer than %d m (the court platform's "
                     "paving, the gorge's floor and the land round them), court_platform (platform paving, parapets, pit floor and walls, gorge), pool_basin, "
                     "head_fall and the cliff_wall run. Not read by the builder yet." % int(COLLIDE_LAND_M),
        "land_lod": {"note": "lod_tier on each land chunk: 0 full detail (any part nearer the can than %d m), 1 decimated to %d%% (to %d m), 2 to %d%% beyond."
                             % (int(NEAR_M), int(RATIO[1] * 100), int(MID_M), int(RATIO[2] * 100)),
                     "chunk_m": CHUNK, "chunks": chunks},
        "sun": old["sun"], "look": old["look"],
        "counts": counts,
        "triangles": {"by_family_as_placed": tri_family, "all_as_placed": sum(tri_family.values())},
        "notes": [
            "The court platform, the pit and the gorge are ONE authored piece (court_platform, origin at the can); the sacred pool (pool_basin) and the head's cliff, great fall and plunge pool (head_fall) likewise.",
            "Not carried over from the 10-12 assembly: the terrace modules (the land is terraced now), payo_pit_floor, the far ranges payo_range_1..5 and payo_clouds (the land runs to 2.8 km).",
            "Not exported from d53: its review sky dressing (the setting sun disc, its glow, the cloud banks): the game has its own sky.",
        ],
    }
    json.dump(doc, open(os.path.join(ART, "payo_art.json"), "w"), indent=1)
    json.dump({"note": "One entry per material name the exported glbs use, AS NAMED IN THE GLBS. `kind` picks the shader: painted, glow, water, fall, foam, mist, "
                       "foliage card, decal, cloud, unlit, and payo_land (2026-10-18: the land and river paint; `surface` says which, `textures` are under "
                       "Art/Payo/Textures/land, `tile_m` metres per tile and `relief_m` as tools/payo_land_blend.py CALM_TEX). Written by tools/payo_export_unity.py.",
               "materials": out_m}, open(os.path.join(ART, "materials_payo.json"), "w"), indent=1)
    report["lagoon_textures_copied"] = lagoon_copied
    report["life_models_missing"] = life_missing
    report["land_textures_copied"] = len(land_files)
    report["placements"] = len(placements)
    report["materials"] = len(out_m)
    json.dump(report, open(os.path.join(OUT, "export_report.json"), "w"), indent=1)
    print("WRITE", json.dumps({k: (v if not isinstance(v, list) or len(v) < 30 else len(v)) for k, v in report.items()}))


def export():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=D53)
    rows, stage = read_pieces()
    water_tris = {}
    for obname, model in WATER.items():
        o = bpy.data.objects.get(obname)
        if o is not None and o.type == "MESH":
            water_tris[model] = export_object(o, model, WATER_MAT)
    low = bpy.data.objects["Low ground under everything"]
    low_tris = export_object(low, "payo_land_low", {"plain": "py_land_slope"}, colour=[0.32 ** 2.2, 0.42 ** 2.2, 0.32 ** 2.2, 1.0])
    print("WATER", water_tris, "LOW", low_tris)
    ob = build_land()
    chunks = export_land(ob)
    write_game(rows, stage, chunks, water_tris, low_tris)


# ====================================================================================== the check (plain Python)
def check():
    doc = json.load(open(os.path.join(ART, "payo_art.json")))
    mats = {e["name"] for e in json.load(open(os.path.join(ART, "materials_payo.json")))["materials"]}
    per_group, missing, unknown, infos = {}, set(), {}, {}

    def need(model, where):
        p = os.path.join(MODELS, model + ".glb")
        if not os.path.exists(p):
            missing.add("%s (%s)" % (model, where)); return
        if model not in infos:
            infos[model] = glb_info(p)
        for mn in infos[model]["materials"]:
            if mn not in mats:
                unknown.setdefault(mn, set()).add(model)

    for r in doc["placements"]:
        k = "%s/%s" % (r["group"], r["family"])
        per_group[k] = per_group.get(k, 0) + 1
        need(r["model"], "placement")
    for b in doc["blocks"]:
        for k in ("model", "extension"):
            if b.get(k):
                need(b[k], "block " + b["id"])
        if b.get("stair"):
            need(b["stair"]["model"], "stair " + b["id"])
    for m in doc["movers"].values():
        need(m["model"], "mover")
    for m in doc["pads"].values():
        need(m, "pad")
    for e in doc["life"]:
        need(e["model"], "life")
    need(doc["sky"]["model"], "sky"); need(doc["catch"]["model"], "catch")
    print("PLACEMENTS per group/family (%d):" % len(doc["placements"]))
    for k in sorted(per_group):
        print("  %-22s %6d" % (k, per_group[k]))
    print("scale3 placements:", sum(1 for r in doc["placements"] if "scale3" in r), " collide placements:", sum(1 for r in doc["placements"] if r.get("collide")))
    print("MISSING glbs:", sorted(missing) or "none")
    print("UNKNOWN materials:", {k: sorted(v)[:4] for k, v in unknown.items()} or "none")
    land = sorted((r["model"] for r in doc["placements"] if r["family"] == "land"))
    tot = {0: 0, 1: 0, 2: 0, "other": 0}
    print("LAND chunks (%d), triangles:" % len(land))
    line = []
    for m in land:
        c = doc.get("land_lod", {}).get("chunks", {}).get(m)
        t = infos.get(m, {}).get("tris", 0)
        tot[c["tier"] if c else "other"] += t
        line.append("%s=%d%s" % (m[len("payo_land_"):], t, "" if c is None else "/t%d" % c["tier"]))
    for i in range(0, len(line), 6):
        print("  " + "  ".join(line[i:i + 6]))
    print("LAND triangles by tier: near(t0) %d, mid(t1) %d, far(t2) %d, low plane %d; total %d" % (tot[0], tot[1], tot[2], tot["other"], sum(tot.values())))
    water = [r["model"] for r in doc["placements"] if r["family"] == "water"]
    print("WATER:", {m: infos.get(m, {}).get("tris") for m in water})
    print("TRIANGLES as placed:", doc.get("triangles", {}).get("all_as_placed"))
    texs = [t for e in json.load(open(os.path.join(ART, "materials_payo.json")))["materials"] for t in e.get("textures", [])]
    print("LAND textures missing:", sorted({t for t in texs if not os.path.exists(os.path.join(ART, t))}) or "none")


if __name__ == "__main__":
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    if "--check" in args or "check" in args:
        check()
    elif bpy is not None and "export" in args:
        export()
    else:
        print(__doc__)
