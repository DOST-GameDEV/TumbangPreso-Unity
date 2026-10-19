"""Payo plant and tree LOD1 builder.

Run:  blender -b --factory-startup --python tools/payo_plant_lods.py
      (optional after "--": model names to limit the run, e.g. -- tree_round)
      blender -b --factory-startup --python tools/payo_plant_lods.py -- lod2
      (the trees' far LOD2, see MODELS2; model names may follow "lod2")

Reads   Assets/TumbangPreso/Art/Payo/Models/<model>.glb   (never written)
Writes  Logs/payo/lod/<model>_lod1.glb, report.json, sheet.png  (staging only)
        with lod2: <model>_lod2.glb, report_lod2.json, sheet_lod2.png

LOD2 (2026-10-19, owner: "optimize the city side of the map, i still get fps
drops. start by lowering the detail and vertices for the trees far from the
play area"): the same card method from the full model, about 40 per cent of
LOD1's triangles, the cards scaled up to SCALE_CAP2 so the crown reaches COVER. PayoFlora draws it past its `Far` distance (220 m at 60
degrees, lodBias 1), where a tree is about 20 pixels tall.

Method, per material role:
  card   whole 2 triangle leaf cards. A well spread subset is kept (voxel grid
         over the card centres, per material so the light/dark mix holds) and
         each kept card is scaled about its own centre so the crown keeps its
         coverage (kept_fraction * scale^2 ~ COVER, scale capped). The card is
         pulled in toward the crown centre a little so the bigger card does not
         grow the silhouette, then shifted back inside the source leaf bbox
         if it pokes out (no spikes over the crown, no cards under the ground). UVs and normals are untouched.
  grid   a leaf, blade, flower or frond made as a UV grid strip (3 x N verts).
         It is rebuilt as a strip of fewer rows using only the u=min / u=max
         edge verts of the kept rows (palm fronds also keep the u=0.5 midrib
         column, their fold is what gives the crown its height), so the texture still maps 0..1 across the
         card. Optionally a subset is kept and scaled like cards.
  solid  trunk, stalk, coconut: welded and collapse decimated.
Material names, slot order, object/node names, mesh names, origin and axes stay
as the source. Normals of card verts are the source normals (stashed per corner
before editing and written back as custom normals).
"""

import bpy
import bmesh
import json
import math
import os
import re
import struct
import sys

import numpy as np
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Payo", "Models")
TEX = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Payo", "Textures")
MATJSON = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Payo", "materials_payo.json")
OUT = os.path.join(ROOT, "Logs", "payo", "lod")
RENDERS = os.path.join(OUT, "_renders")

COVER = 1.0        # target kept_fraction * scale^2 for subsets
SCALE_CAP = 3.0    # never scale a card more than this
PULL = 0.5         # how much of the extra half size a scaled card moves inward
SEED = 7

# Roles by material name.
CARD_RE = re.compile(r"^(leaf_(dark|light)(_lime|_olive)?|lagoon_leaf_round_.*)$")
SOLID_RE = re.compile(r"^(trunk|trunk_grey|lagoon_stalk_.*|py_palm_trunk)$")
GRID_RE = re.compile(r"^(lagoon_leaf_croton_.*|lagoon_leaf_blade_.*|lagoon_flower_.*|py_frond_.*)$")

# Per model budgets.
#   cards:   total leaf cards kept over all card materials
#   solid:   triangles for all solid parts together
#   rows:    {material regex: rows kept} for grid islands (2 rows = 1 quad)
#   mid:     {material regex: True} keep the middle UV column of grid islands
#   pull:    override PULL (flat ground patches use 0: pulling in shrinks their spread)
#   gkeep:   {material regex: fraction of grid islands kept}
TREE_ROWS = {}
MODELS = {
    "tree_tall_deep": dict(cards=170, solid=60),
    "tree_round": dict(cards=200, solid=90),
    "tree_broad_lime": dict(cards=220, solid=100),
    "tree_lean_olive": dict(cards=200, solid=90),
    "payo_plant_palm_0": dict(solid=70, rows={r"py_frond_(light|dark)": 4, r"py_frond_small": 3},
                              mid={r"py_frond_(light|dark)": True}),
    "plant_croton_0": dict(solid=36, rows={r".*": 2}),
    "plant_croton_1": dict(solid=36, rows={r".*": 2}),
    "plant_croton_2": dict(solid=36, rows={r".*": 2}),
    "plant_croton_3": dict(solid=24, rows={r".*": 2}),
    "plant_groundcover_0": dict(cards=60, rows={r".*": 2}, pull=0.0),
    "plant_groundcover_1": dict(cards=60, rows={r".*": 2}, pull=0.0),
    "plant_groundcover_3": dict(cards=60, rows={r".*": 2}, pull=0.0),
    "plant_bush_0": dict(cards=60, rows={r".*": 2}, gkeep={r"lagoon_flower_.*": 0.6}),
    "plant_bush_1": dict(cards=60, rows={r".*": 2}, gkeep={r"lagoon_flower_.*": 0.6}),
    "plant_bush_2": dict(cards=60, rows={r".*": 2}, gkeep={r"lagoon_flower_.*": 0.6}),
    "plant_bush_3": dict(cards=60, rows={r".*": 2}, gkeep={r"lagoon_flower_.*": 0.6}),
}

# The far trees' LOD2 budgets (a quarter of LOD1's cards; the trunk a few facets).
# `cap` lets the fewer cards grow further: cards * cap^2 matches LOD1's coverage.
# First try (44 to 55 cards, cap 6) read thin and gappy at 250 m on the review sheet: 70 to 85 cards at cap 8
# reach COVER, as LOD1 does not (its cap 3 holds it under).
SCALE_CAP2 = 8.0
MODELS2 = {
    "tree_tall_deep": dict(cards=70, solid=16, cap=SCALE_CAP2),
    "tree_round": dict(cards=80, solid=20, cap=SCALE_CAP2),
    "tree_broad_lime": dict(cards=85, solid=24, cap=SCALE_CAP2),
    "tree_lean_olive": dict(cards=80, solid=20, cap=SCALE_CAP2),
}
# Which level this run makes ("lod1" unless "lod2" follows "--"); set by main().
LEVEL = "lod1"

# How far away the "small" review render pretends to be, metres, and the screen
# it pretends to be on (1080 px tall, 60 degree vertical field of view).
FAR_TREE = 100.0
FAR_TREE2 = 250.0
FAR_PLANT = 80.0
PX_PER_RAD = 1080.0 / math.radians(60.0)


def base_name(name):
    return re.sub(r"\.\d{3}$", "", name)


def role_of(mat_name):
    n = base_name(mat_name)
    if SOLID_RE.match(n):
        return "solid"
    if CARD_RE.match(n):
        return "card"
    if GRID_RE.match(n):
        return "grid"
    raise RuntimeError("no role for material " + n)


def lookup(table, mat_name, default):
    for pat, val in (table or {}).items():
        if re.fullmatch(pat, base_name(mat_name)):
            return val
    return default


# ---------------------------------------------------------------- glb reading

def glb_info(path):
    data = open(path, "rb").read()
    jlen = struct.unpack_from("<I", data, 12)[0]
    js = json.loads(data[20:20 + jlen])
    acc = js["accessors"]
    mn = [math.inf] * 3
    mx = [-math.inf] * 3
    tris = 0
    per_mat = {}
    attrs = set()
    for m in js["meshes"]:
        for p in m["primitives"]:
            a = acc[p["attributes"]["POSITION"]]
            for i in range(3):
                mn[i] = min(mn[i], a["min"][i])
                mx[i] = max(mx[i], a["max"][i])
            t = (acc[p["indices"]]["count"] if "indices" in p else a["count"]) // 3
            tris += t
            mname = js["materials"][p["material"]]["name"] if "material" in p else None
            per_mat[mname] = per_mat.get(mname, 0) + t
            attrs.update(p["attributes"].keys())
    nodes = []
    for n in js["nodes"]:
        nodes.append({k: n[k] for k in ("name", "translation", "rotation", "scale", "matrix") if k in n})
    mats = []
    for m in js.get("materials", []):
        pbr = m.get("pbrMetallicRoughness", {})
        mats.append(dict(name=m["name"], alphaMode=m.get("alphaMode", "OPAQUE"),
                         doubleSided=m.get("doubleSided", False),
                         texture="baseColorTexture" in pbr))
    return dict(tris=tris, tris_by_material=per_mat,
                bbox_min=[round(v, 4) for v in mn], bbox_max=[round(v, 4) for v in mx],
                nodes=nodes, mesh_names=[m.get("name") for m in js["meshes"]],
                materials=mats, attributes=sorted(attrs),
                images=[i.get("name") for i in js.get("images", [])])


# ---------------------------------------------------------------- scene utils

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_glb(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    return [o for o in bpy.data.objects if o not in before]


def export_glb(path, vcol):
    kw = dict(filepath=path, export_format="GLB", use_selection=False,
              export_yup=True, export_apply=True, export_cameras=False,
              export_lights=False, export_materials="EXPORT",
              export_image_format="AUTO", export_normals=True,
              export_texcoords=True, export_tangents=False,
              export_attributes=False, export_extras=False,
              export_animations=False, export_skins=False, export_morph=False)
    if vcol:
        kw.update(export_vertex_color="ACTIVE", export_all_vertex_colors=False,
                  export_active_vertex_color_when_no_material=True)
    else:
        kw.update(export_vertex_color="NONE")
    bpy.ops.export_scene.gltf(**kw)


def islands(bm, faces):
    """Connected face groups among `faces` (sharing an edge)."""
    allowed = set(faces)
    seen = set()
    out = []
    for f in faces:
        if f in seen:
            continue
        stack = [f]
        seen.add(f)
        comp = []
        while stack:
            g = stack.pop()
            comp.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h in allowed and h not in seen:
                        seen.add(h)
                        stack.append(h)
        out.append(comp)
    return out


def island_verts(comp):
    vs = []
    seen = set()
    for f in comp:
        for v in f.verts:
            if v not in seen:
                seen.add(v)
                vs.append(v)
    return vs


def centre(comp):
    vs = island_verts(comp)
    c = Vector()
    for v in vs:
        c += v.co
    return c / len(vs)


def spread_pick(points, k, rng):
    """Pick k indices spread evenly over the points (voxel grid, one per cell)."""
    n = len(points)
    if k >= n:
        return list(range(n))
    if k <= 0:
        return []
    pts = np.asarray(points)
    lo = pts.min(0)
    span = max(float((pts.max(0) - lo).max()), 1e-6)
    a, b = span / 400.0, span
    best = None
    for _ in range(40):
        cell = 0.5 * (a + b)
        keys = np.floor((pts - lo) / cell).astype(np.int64)
        uniq, inv = np.unique(keys, axis=0, return_inverse=True)
        inv = inv.reshape(-1)
        if len(uniq) >= k:
            best = (cell, uniq, inv)
            a = cell
        else:
            b = cell
    if best is None:
        return sorted(rng.choice(n, k, replace=False).tolist())
    cell, uniq, inv = best
    picks = []
    for ci in range(len(uniq)):
        idx = np.nonzero(inv == ci)[0]
        mean = pts[idx].mean(0)
        picks.append(int(idx[np.argmin(((pts[idx] - mean) ** 2).sum(1))]))
    if len(picks) > k:
        picks = rng.choice(picks, k, replace=False).tolist()
    return sorted(picks)


def subset_scale(bm, comps, keep_n, crown_c, rng, box, cap=SCALE_CAP, pull=PULL):
    """Keep keep_n islands spread over comps, scale them up; return faces to delete."""
    n = len(comps)
    if n == 0:
        return [], 1.0
    keep_n = max(1, min(n, keep_n))
    centres = [centre(c) for c in comps]
    picks = set(spread_pick([tuple(c) for c in centres], keep_n, rng))
    frac = keep_n / n
    s = min(cap, math.sqrt(COVER / frac)) if frac < 1.0 else 1.0
    dead = []
    for i, comp in enumerate(comps):
        if i not in picks:
            dead.extend(comp)
            continue
        if s == 1.0:
            continue
        c = centres[i]
        vs = island_verts(comp)
        half = max((v.co - c).length for v in vs)
        inward = crown_c - c
        if inward.length > 1e-6:
            step = min(inward.length * 0.5, (s - 1.0) * half * pull)
            c2 = c + inward.normalized() * step
        else:
            c2 = c
        for v in vs:
            v.co = c2 + (v.co - c) * s
        clamp_into(vs, box)
    return dead, s


def clamp_into(vs, box):
    """Shift a scaled card back inside the source leaf bounding box (per axis)."""
    lo, hi = box
    shift = Vector()
    for ax in range(3):
        mn = min(v.co[ax] for v in vs)
        mx = max(v.co[ax] for v in vs)
        if mx - mn >= hi[ax] - lo[ax]:
            shift[ax] = 0.5 * (lo[ax] + hi[ax]) - 0.5 * (mn + mx)
        elif mn < lo[ax]:
            shift[ax] = lo[ax] - mn
        elif mx > hi[ax]:
            shift[ax] = hi[ax] - mx
    if shift.length > 0:
        for v in vs:
            v.co += shift


def rebuild_grid(bm, comp, rows, uv_layer, mid=False):
    """Replace a UV grid strip by a coarser strip on its outer edges. Returns new faces."""
    vert_uv = {}
    vert_loop = {}
    for f in comp:
        for l in f.loops:
            u, v = l[uv_layer].uv
            vert_uv[l.vert] = (round(u, 4), round(v, 4))
            vert_loop[l.vert] = l
    us = sorted({uv[0] for uv in vert_uv.values()})
    vs = sorted({uv[1] for uv in vert_uv.values()})
    swap = len(us) > len(vs)        # long axis must be the "rows" axis
    if swap:
        us, vs = vs, us
    at = {}
    for vert, (u, v) in vert_uv.items():
        key = (v, u) if swap else (u, v)
        at[key] = vert
    n = len(vs)
    rows = max(2, min(rows, n))
    idx = sorted({int(round(i * (n - 1) / (rows - 1))) for i in range(rows)})
    # island facing, to keep winding
    nsum = Vector()
    for f in comp:
        nsum += f.normal * f.calc_area()
    new = []
    # mid=True keeps every u column (the folded midrib of a frond)
    cols = us if mid else [us[0], us[-1]]
    cells = [(a, b, u0, u1) for a, b in zip(idx[:-1], idx[1:]) for u0, u1 in zip(cols[:-1], cols[1:])]
    for a, b, u0, u1 in cells:
        quad = [at.get((u0, vs[a])), at.get((u1, vs[a])),
                at.get((u1, vs[b])), at.get((u0, vs[b]))]
        if any(q is None for q in quad) or len(set(quad)) < 4:
            raise RuntimeError("grid island is not a clean UV grid")
        try:
            f = bm.faces.new(quad, comp[0])
        except ValueError:
            continue
        f.normal_update()
        if f.normal.dot(nsum) < 0:
            f.normal_flip()
        for l in f.loops:
            l.copy_from(vert_loop[l.vert])
        new.append(f)
    return new


# ---------------------------------------------------------------- LOD build

def stash_normals(me):
    if "lod_n" in me.attributes:
        me.attributes.remove(me.attributes["lod_n"])
    att = me.attributes.new("lod_n", "FLOAT_VECTOR", "CORNER")
    flat = np.zeros(len(me.loops) * 3, dtype=np.float32)
    me.corner_normals.foreach_get("vector", flat)
    att.data.foreach_set("vector", flat)


def process_object(obj, cfg, rng, notes):
    me = obj.data
    stash_normals(me)
    roles = [role_of(m.name) for m in me.materials]

    bm = bmesh.new()
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    uv_layer = bm.loops.layers.uv.active

    leaf_pts = [v.co.copy() for f in bm.faces if roles[f.material_index] != "solid" for v in f.verts]
    if leaf_pts:
        box = (Vector([min(p[i] for p in leaf_pts) for i in range(3)]),
               Vector([max(p[i] for p in leaf_pts) for i in range(3)]))
    by_mat = {}
    for f in bm.faces:
        by_mat.setdefault(f.material_index, []).append(f)

    dead = []
    solid_faces = []
    card_groups = {}
    for mi, faces in sorted(by_mat.items()):
        mname = me.materials[mi].name
        r = roles[mi]
        if r == "solid":
            solid_faces.extend(faces)
            continue
        comps = islands(bm, faces)
        if r == "grid":
            rows = lookup(cfg.get("rows"), mname, 2)
            rebuilt = []
            for comp in comps:
                new = rebuild_grid(bm, comp, rows, uv_layer, mid=bool(lookup(cfg.get("mid"), mname, False)))
                dead.extend(comp)
                rebuilt.append(new)
            keep = lookup(cfg.get("gkeep"), mname, 1.0)
            card_groups[mi] = ("grid", rebuilt, keep)
            notes.append(f"{obj.name}/{base_name(mname)}: {len(comps)} grid islands -> {rows - 1} quad strip")
        else:
            card_groups[mi] = ("card", comps, None)

    # crown centre over all leafy islands of this object
    allc = []
    for kind, comps, _ in card_groups.values():
        allc.extend(centre(c) for c in comps if c)
    crown_c = sum(allc, Vector()) / len(allc) if allc else Vector()

    total_cards = sum(len(c) for k, c, _ in card_groups.values() if k == "card")
    budget = cfg.get("cards", total_cards)
    for mi, (kind, comps, keep) in sorted(card_groups.items()):
        mname = base_name(me.materials[mi].name)
        if kind == "card":
            k = max(2, int(round(budget * len(comps) / max(1, total_cards))))
            if len(comps) < 30:     # a small accent group: keep enough that no card balloons
                k = max(k, math.ceil(0.25 * len(comps)))
            d, s = subset_scale(bm, comps, k, crown_c, rng, box, cap=cfg.get("cap", SCALE_CAP), pull=cfg.get("pull", PULL))
            dead.extend(d)
            notes.append(f"{obj.name}/{mname}: kept {min(k, len(comps))}/{len(comps)} cards, scale {s:.2f}")
        elif keep < 1.0:
            k = max(1, int(round(keep * len(comps))))
            d, s = subset_scale(bm, comps, k, crown_c, rng, box, cap=1.6)
            dead.extend(d)
            notes.append(f"{obj.name}/{mname}: kept {k}/{len(comps)} rebuilt islands, scale {s:.2f}")

    if dead:
        bmesh.ops.delete(bm, geom=list(set(dead)), context="FACES")
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context="VERTS")
    bm.to_mesh(me)
    bm.free()

    solid_mis = {i for i, r in enumerate(roles) if r == "solid"}
    if solid_mis and any(p.material_index in solid_mis for p in me.polygons):
        if any(p.material_index not in solid_mis for p in me.polygons):
            solid_obj = split_off(obj, solid_mis)
        else:
            solid_obj = None
        target = solid_obj if solid_obj else obj
        decimate_solid(target, cfg.get("solid", 80), notes)
        if solid_obj:
            join_into(obj, solid_obj)

    finish_normals(obj, solid_mis)


def split_off(obj, mis):
    """Move faces with material index in mis into a new object (same materials)."""
    dup = obj.copy()
    dup.data = obj.data.copy()
    bpy.context.scene.collection.objects.link(dup)
    for o, keep_solid in ((obj, False), (dup, True)):
        bm = bmesh.new()
        bm.from_mesh(o.data)
        kill = [f for f in bm.faces if (f.material_index in mis) != keep_solid]
        bmesh.ops.delete(bm, geom=kill, context="FACES")
        bm.to_mesh(o.data)
        bm.free()
    return dup


def decimate_solid(obj, target_tris, notes):
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bm.to_mesh(me)
    bm.free()
    src = sum(len(p.vertices) - 2 for p in me.polygons)
    if src > target_tris:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        mod = obj.modifiers.new("lod", "DECIMATE")
        mod.decimate_type = "COLLAPSE"
        mod.ratio = target_tris / src
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    got = sum(len(p.vertices) - 2 for p in me.polygons)
    notes.append(f"{obj.name}: solid welded + collapse {src} -> {got} tris")


def join_into(dst, src):
    bpy.ops.object.select_all(action="DESELECT")
    src.select_set(True)
    dst.select_set(True)
    bpy.context.view_layer.objects.active = dst
    bpy.ops.object.join()


def finish_normals(obj, solid_mis):
    me = obj.data
    nl = len(me.loops)
    stash = np.zeros(nl * 3, dtype=np.float32)
    me.attributes["lod_n"].data.foreach_get("vector", stash)
    me.attributes.remove(me.attributes["lod_n"])
    if "custom_normal" in me.attributes:
        me.attributes.remove(me.attributes["custom_normal"])
    # solid parts: flat shaded facets (the trunks were faceted to begin with)
    for p in me.polygons:
        if p.material_index in solid_mis:
            p.use_smooth = False
    auto = np.zeros(nl * 3, dtype=np.float32)
    me.corner_normals.foreach_get("vector", auto)
    stash = stash.reshape(-1, 3)
    auto = auto.reshape(-1, 3)
    out = auto.copy()
    for p in me.polygons:
        if p.material_index in solid_mis:
            continue
        s = p.loop_start
        out[s:s + p.loop_total] = stash[s:s + p.loop_total]
    ln = np.linalg.norm(out, axis=1, keepdims=True)
    out = out / np.maximum(ln, 1e-8)
    me.normals_split_custom_set([tuple(v) for v in out])


def build(name, report):
    cfg = (MODELS2 if LEVEL == "lod2" else MODELS)[name]
    src = os.path.join(SRC, name + ".glb")
    dst = os.path.join(OUT, name + "_" + LEVEL + ".glb")
    reset()
    objs = import_glb(src)
    vcol = any(len(o.data.color_attributes) for o in objs if o.type == "MESH")
    rng = np.random.default_rng(SEED)
    notes = []
    names_before = sorted(o.name for o in objs)
    for o in sorted(objs, key=lambda o: o.name):
        if o.type == "MESH":
            process_object(o, cfg, rng, notes)
    names_after = sorted(o.name for o in bpy.data.objects)
    assert names_before == names_after, (names_before, names_after)
    export_glb(dst, vcol)
    a = glb_info(src)
    b = glb_info(dst)
    mats_a = sorted(m["name"] for m in a["materials"])
    mats_b = sorted(m["name"] for m in b["materials"])
    report[name] = dict(
        source_tris=a["tris"], lod1_tris=b["tris"],
        materials=mats_b, materials_match_source=mats_a == mats_b,
        tris_by_material=dict(source=a["tris_by_material"], lod1=b["tris_by_material"]),
        bbox_before=dict(min=a["bbox_min"], max=a["bbox_max"]),
        bbox_after=dict(min=b["bbox_min"], max=b["bbox_max"]),
        nodes_before=a["nodes"], nodes_after=b["nodes"],
        mesh_names_before=a["mesh_names"], mesh_names_after=b["mesh_names"],
        attributes_before=a["attributes"], attributes_after=b["attributes"],
        material_modes_before=a["materials"], material_modes_after=b["materials"],
        images_after=b["images"], notes=notes)
    print(f"[lod] {name} {LEVEL}: {a['tris']} -> {b['tris']} tris  materials match: {mats_a == mats_b}")


def roundtrip(name):
    src = os.path.join(SRC, name + ".glb")
    tmp = os.path.join(OUT, name + "_roundtrip.glb")
    reset()
    objs = import_glb(src)
    vcol = any(len(o.data.color_attributes) for o in objs if o.type == "MESH")
    export_glb(tmp, vcol)
    a = glb_info(src)
    b = glb_info(tmp)
    os.remove(tmp)
    same = a["bbox_min"] == b["bbox_min"] and a["bbox_max"] == b["bbox_max"]
    return dict(model=name, bbox_source=[a["bbox_min"], a["bbox_max"]],
                bbox_roundtrip=[b["bbox_min"], b["bbox_max"]], identical_bbox=same,
                tris=[a["tris"], b["tris"]], attributes=[a["attributes"], b["attributes"]],
                materials_source=a["materials"], materials_roundtrip=b["materials"])


# ---------------------------------------------------------------- review render

def load_matspec():
    d = json.load(open(MATJSON))
    out = {}
    for m in d["materials"]:
        tint = m.get("tint_linear")
        if isinstance(tint, str):
            tint = json.loads(tint)
            linear = True
        else:
            tint = m.get("tint") or [1, 1, 1]
            linear = False
        out[m["name"]] = dict(tex=m.get("texture"), tint=tint, linear=linear,
                              vcol=m.get("vertex_colour", False))
    return out


def srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


_preview_cache = {}


def preview_material(name, spec):
    if name in _preview_cache:
        return _preview_cache[name]
    m = bpy.data.materials.new("preview_" + name)
    m.use_backface_culling = False
    m.surface_render_method = "DITHERED"
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.8
    tint = spec["tint"] if spec["linear"] else [srgb_to_lin(c) for c in spec["tint"]]
    rgb = nt.nodes.new("ShaderNodeRGB")
    rgb.outputs[0].default_value = (*tint[:3], 1.0)
    colour = rgb.outputs[0]
    texfile = os.path.join(TEX, f"{spec['tex']}_albedo.png") if spec["tex"] else None
    if texfile and os.path.exists(texfile):
        img = bpy.data.images.load(texfile, check_existing=True)
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = img
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type = "RGBA"
        mul.blend_type = "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        nt.links.new(tex.outputs["Color"], mul.inputs[6])
        nt.links.new(colour, mul.inputs[7])
        colour = mul.outputs[2]
        if img.depth == 32 or img.channels == 4:
            gt = nt.nodes.new("ShaderNodeMath")
            gt.operation = "GREATER_THAN"
            gt.inputs[1].default_value = 0.5
            nt.links.new(tex.outputs["Alpha"], gt.inputs[0])
            nt.links.new(gt.outputs[0], bsdf.inputs["Alpha"])
    if spec["vcol"]:
        va = nt.nodes.new("ShaderNodeVertexColor")
        va.layer_name = "Color"
        mul2 = nt.nodes.new("ShaderNodeMix")
        mul2.data_type = "RGBA"
        mul2.blend_type = "MULTIPLY"
        mul2.inputs["Factor"].default_value = 1.0
        nt.links.new(colour, mul2.inputs[6])
        nt.links.new(va.outputs["Color"], mul2.inputs[7])
        colour = mul2.outputs[2]
    nt.links.new(colour, bsdf.inputs["Base Color"])
    _preview_cache[name] = m
    return m


def apply_preview(objs, specs):
    for o in objs:
        if o.type != "MESH":
            continue
        for slot in o.material_slots:
            if slot.material is None:
                continue
            n = base_name(slot.material.name)
            if n in specs:
                slot.material = preview_material(n, specs[n])


def world_bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs if o.type == "MESH" for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def label(text, loc, rot, size):
    cu = bpy.data.curves.new("lbl", "FONT")
    cu.body = text
    cu.size = size
    cu.align_x = "CENTER"
    ob = bpy.data.objects.new("lbl", cu)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = rot
    m = bpy.data.materials.new("lbl")
    nt = m.node_tree
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (0.02, 0.02, 0.02, 1)
    nt.links.new(em.outputs[0], nt.nodes["Material Output"].inputs[0])
    cu.materials.append(m)
    return ob


def render_pair(name, info, specs):
    reset()
    _preview_cache.clear()
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.eevee.taa_render_samples = 32
    sc.view_settings.view_transform = "Standard"
    sc.render.film_transparent = False
    world = bpy.data.worlds.new("w")
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.68, 0.85, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    sun_d = bpy.data.lights.new("sun", "SUN")
    sun_d.energy = 3.5
    sun_d.angle = math.radians(8)
    sun = bpy.data.objects.new("sun", sun_d)
    sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(50), 0, math.radians(30))

    full = import_glb(os.path.join(SRC, name + ".glb"))
    lod = import_glb(os.path.join(OUT, name + "_" + LEVEL + ".glb"))
    apply_preview(full + lod, specs)
    mn, mx = world_bounds(full)
    size = mx - mn
    ctr = (mn + mx) / 2
    az = math.radians(35)
    el = math.radians(20)
    right = Vector((math.cos(az), math.sin(az), 0))
    view = Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)))
    horiz = max(size.x, size.y)
    off = 0.62 * horiz + 0.15
    roots = lambda objs: [o for o in objs if o.parent is None]
    for o in roots(full):
        o.location -= right * off + Vector((ctr.x, ctr.y, 0))
    for o in roots(lod):
        o.location += right * off - Vector((ctr.x, ctr.y, 0))
    bpy.context.view_layer.update()

    cam_d = bpy.data.cameras.new("cam")
    cam_d.type = "ORTHO"
    cam = bpy.data.objects.new("cam", cam_d)
    sc.collection.objects.link(cam)
    sc.camera = cam
    focus = Vector((0, 0, ctr.z))
    cam.location = focus + view * (horiz + size.z) * 4
    cam.rotation_euler = (-view).to_track_quat("-Z", "Y").to_euler()
    W, H = 900, 500
    width_needed = (2 * off + horiz) * 1.12
    height_needed = (size.z * math.cos(el) + horiz * math.sin(el)) * 1.35
    cam_d.ortho_scale = max(width_needed, height_needed * W / H)
    # labels below each model
    rot = cam.rotation_euler.copy()
    up = rot.to_matrix() @ Vector((0, 1, 0))
    drop = up * (height_needed / 1.35 * 0.5 + cam_d.ortho_scale * H / W * 0.06)
    lsize = cam_d.ortho_scale * 0.035
    l1 = label(f"full  {info['source_tris']} tris", focus - right * off - drop, rot, lsize)
    l2 = label(f"{LEVEL}  {info['lod1_tris']} tris", focus + right * off - drop, rot, lsize)

    os.makedirs(RENDERS, exist_ok=True)
    mid = os.path.join(RENDERS, name + ("_lod2" if LEVEL == "lod2" else "") + "_mid.png")
    sc.render.resolution_x, sc.render.resolution_y = W, H
    sc.render.resolution_percentage = 100
    sc.render.filepath = mid
    bpy.ops.render.render(write_still=True)

    # small: same framing, pixels per metre as seen from far away
    far = (FAR_TREE2 if LEVEL == "lod2" else FAR_TREE) if name.startswith("tree_") else FAR_PLANT
    ppm = PX_PER_RAD / far
    sw = max(8, int(round(cam_d.ortho_scale * ppm)))
    sh = max(4, int(round(sw * H / W)))
    l1.hide_render = True
    l2.hide_render = True
    small = os.path.join(RENDERS, name + ("_lod2" if LEVEL == "lod2" else "") + "_small.png")
    sc.render.resolution_x, sc.render.resolution_y = sw, sh
    sc.render.filepath = small
    bpy.ops.render.render(write_still=True)
    return mid, small, far


def read_png(path):
    img = bpy.data.images.load(path)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    return px  # bottom-up rows


def compose_sheet(rows, path):
    """rows: list of (mid_png, small_png). Writes one sheet, mid | small upscaled."""
    H = 500
    gap = 8
    blocks = []
    width = 0
    for mid, small in rows:
        a = read_png(mid)
        b = read_png(small)
        f = max(1, H // b.shape[0])
        b = np.repeat(np.repeat(b, f, axis=0), f, axis=1)
        pad = np.ones((H, b.shape[1], 4), dtype=np.float32)
        pad[:, :, :3] = 0.92
        y0 = (H - b.shape[0]) // 2
        pad[y0:y0 + b.shape[0]] = b
        row = np.concatenate([a, np.ones((H, gap, 4), np.float32), pad], axis=1)
        blocks.append(row)
        width = max(width, row.shape[1])
    full = []
    for row in blocks:
        if row.shape[1] < width:
            ext = np.ones((H, width - row.shape[1], 4), np.float32)
            row = np.concatenate([row, ext], axis=1)
        full.append(row)
        full.append(np.ones((gap, width, 4), np.float32) * np.array([0.2, 0.2, 0.2, 1], np.float32))
    # image pixels are bottom-up; first model should be on top
    sheet = np.concatenate(full[::-1], axis=0)
    h, w = sheet.shape[:2]
    img = bpy.data.images.new("sheet", w, h, alpha=False)
    img.pixels = sheet.reshape(-1).tolist()
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()


def main():
    global LEVEL
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if argv and argv[0] == "lod2":
        LEVEL = "lod2"
        argv = argv[1:]
    table = MODELS2 if LEVEL == "lod2" else MODELS
    names = argv or list(table)
    os.makedirs(OUT, exist_ok=True)
    rep_path = os.path.join(OUT, "report_lod2.json" if LEVEL == "lod2" else "report.json")
    report = {}
    if argv and os.path.exists(rep_path):
        report = json.load(open(rep_path)).get("models", {})
    rt = [roundtrip("tree_round"), roundtrip("payo_plant_palm_0")]
    for n in names:
        build(n, report)
    specs = load_matspec()
    rows = []
    for n in table:
        if n not in report:
            continue
        mid, small, far = render_pair(n, report[n], specs)
        report[n]["review_small_distance_m"] = far
        rows.append((mid, small))
    compose_sheet(rows, os.path.join(OUT, "sheet_lod2.png" if LEVEL == "lod2" else "sheet.png"))
    out = dict(
        tool="tools/payo_plant_lods.py",
        method=__doc__.strip(),
        params=dict(level=LEVEL, cover=COVER, scale_cap=SCALE_CAP2 if LEVEL == "lod2" else SCALE_CAP, pull=PULL, seed=SEED),
        roundtrip_check=rt,
        sheet="Logs/payo/lod/sheet.png (rows in model order; left: 3/4 view full vs lod1, "
              "right: same view rendered at the pixel size seen from review_small_distance_m on a "
              "1080p 60 degree screen, upscaled nearest)",
        models=report)
    json.dump(out, open(rep_path, "w"), indent=1)
    total_a = sum(r["source_tris"] for r in report.values())
    total_b = sum(r["lod1_tris"] for r in report.values())
    print(f"[lod] done. {total_a} -> {total_b} tris over {len(report)} models")


main()
