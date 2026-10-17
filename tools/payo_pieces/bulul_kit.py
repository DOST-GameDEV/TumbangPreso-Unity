"""PAYO, THE GUARDIANS (the bulul family): the Blender half of the kit (2026-10-12).

One piece is ONE object. It is typed as chunky closed parts (convex hulls of boxes with a wide chamfer on every edge,
squashed balls, turned shapes, fat tubes), carved with exact booleans, unioned into ONE closed skin, unwrapped, and
then FIVE maps are baked from the skin itself into its own UV layout: where each texel is in space, which way it
faces, how buried it is (ambient occlusion), how close it is to an edge (the bevel node), and which material it is.
tools/payo_pieces/bulul_paint.py (system Python) paints the atlas from those maps, so every pale bevel, dark groove,
moss cap and wet streak sits on THIS object's real edges, folds and tops. Nothing is a tiling texture.

AUTHORING SPACE everywhere in this family: metres, x right, Y UP, +Z IS THE FRONT (glTF). `B()` turns a point into
Blender's (z up, front is -Y). Pivot on the ground at the piece's centre.

  blender -b --python tools/payo_pieces/bulul_build.py -- <piece>      (uses this file)
"""
import json
import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
WORK = os.path.join(ROOT, "Logs", "payo", "pieces", "bulul", "work")
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "bulul")

# Material ids. The painter (bulul_paint.py) has the same list.
STONE, CLOTH, MOSS, INLAY, ACCENT, RICE, JAR, BASKET, LEAF, WOOD, DARK, WATER = range(12)
PBLACK, PCREAM, POCHRE, PRED = 12, 13, 14, 15          # the body material under worn paint of that colour

PARTS = []
MARKS = {}


def B(p):
    return Vector((p[0], -p[2], p[1]))


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    PARTS.clear(); MARKS.clear()


def mat(pid):
    name = "P%02d" % pid
    return bpy.data.materials.get(name) or bpy.data.materials.new(name)


def _finish(name, bm, pid, keep=True, smooth=False):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
    me.materials.append(mat(pid)); me.materials.append(mat(pid + 16))
    o["smooth"] = 1 if smooth else 0
    if keep:
        PARTS.append(o)
    return o


def rot(p, r):
    """Turn point p by r = (about x, about y, about z) degrees, authoring space, applied x then y then z."""
    x, y, z = p
    ax, ay, az = (math.radians(a) for a in r)
    y, z = y * math.cos(ax) - z * math.sin(ax), y * math.sin(ax) + z * math.cos(ax)
    x, z = x * math.cos(ay) + z * math.sin(ay), -x * math.sin(ay) + z * math.cos(ay)
    x, y = x * math.cos(az) - y * math.sin(az), x * math.sin(az) + y * math.cos(az)
    return (x, y, z)


def bx(c, h, top=1.0, r=(0, 0, 0), topz=None, shift=(0, 0)):
    """The eight corners of a box: centre c, half sizes h, its top face scaled by `top` (x) and `topz` (z) and
    slid by `shift` (x, z), then turned by r about its centre."""
    tz = top if topz is None else topz
    out = []
    for sy in (-1, 1):
        kx, kz = (top, tz) if sy > 0 else (1.0, 1.0)
        ox, oz = shift if sy > 0 else (0, 0)
        for sx in (-1, 1):
            for sz in (-1, 1):
                p = rot((sx * h[0] * kx + ox, sy * h[1], sz * h[2] * kz + oz), r)
                out.append((c[0] + p[0], c[1] + p[1], c[2] + p[2]))
    return out


def hull(pts, pid=STONE, bev=0.1, seg=1, name="hull", keep=True):
    """A convex carved block through the given points, with a chamfer of `bev` on every edge."""
    bm = bmesh.new()
    vs = [bm.verts.new(B(p)) for p in pts]
    bmesh.ops.convex_hull(bm, input=vs)
    for v in [v for v in bm.verts if not v.link_faces]:
        bm.verts.remove(v)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(1.0), verts=bm.verts[:], edges=bm.edges[:])
    if bev > 0:
        res = bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bev, offset_type="OFFSET", segments=seg, profile=0.5,
                              affect="EDGES", clamp_overlap=True)
        for f in res["faces"]:
            f.material_index = 1                    # the chamfer: painted pale (material pid + 16)
    return _finish(name, bm, pid, keep)


def ell(c, r, pid=MOSS, seg=10, rings=6, turn=(0, 0, 0), name="ell", keep=True):
    """A squashed ball: centre c, radii r (x, y, z), turned by `turn`."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    for v in bm.verts:
        a = (v.co.x, v.co.z, -v.co.y)
        p = rot((a[0] * r[0], a[1] * r[1], a[2] * r[2]), turn)
        v.co = B((c[0] + p[0], c[1] + p[1], c[2] + p[2]))
    return _finish(name, bm, pid, keep, smooth=True)


def lathe(profile, c=(0, 0, 0), pid=STONE, seg=12, turn=(0, 0, 0), sx=1.0, sz=1.0, name="lathe", keep=True):
    """A turned shape about its own up: profile = [(radius, height)] bottom to top, closed with a fan at each end."""
    bm = bmesh.new()
    rings = []
    for rad, y in profile:
        ring = []
        for i in range(seg):
            a = 2 * math.pi * i / seg
            p = rot((math.cos(a) * rad * sx, y, math.sin(a) * rad * sz), turn)
            ring.append(bm.verts.new(B((c[0] + p[0], c[1] + p[1], c[2] + p[2]))))
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        for i in range(seg):
            j = (i + 1) % seg
            bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.faces.new(rings[0]); bm.faces.new(rings[-1][::-1])
    return _finish(name, bm, pid, keep, smooth=True)


def tube(points, radius, pid=LEAF, seg=7, closed=False, name="tube", keep=True, flat=1.0):
    """A fat round cord along authoring points. radius may be one number or one per point. flat squashes it."""
    pts = [B(p) for p in points]
    n = len(pts)
    rad = radius if isinstance(radius, (list, tuple)) else [radius] * n
    bm = bmesh.new()
    rings = []
    up = Vector((0.13, 0.21, 0.97)).normalized()
    for i in range(n):
        a = pts[(i - 1) % n] if (closed or i > 0) else pts[i]
        b = pts[(i + 1) % n] if (closed or i < n - 1) else pts[i]
        t = (b - a).normalized()
        u = (up - t * up.dot(t))
        u = u.normalized() if u.length > 1e-4 else t.orthogonal().normalized()
        w = t.cross(u)
        up = u
        rings.append([bm.verts.new(pts[i] + (u * math.cos(2 * math.pi * k / seg) + w * math.sin(2 * math.pi * k / seg) * flat) * rad[i])
                      for k in range(seg)])
    pairs = list(zip(rings, rings[1:])) + ([(rings[-1], rings[0])] if closed else [])
    for a, b in pairs:
        for k in range(seg):
            j = (k + 1) % seg
            bm.faces.new((a[k], a[j], b[j], b[k]))
    if not closed:
        bm.faces.new(rings[0][::-1]); bm.faces.new(rings[-1])
    return _finish(name, bm, pid, keep, smooth=True)


def ring(c, R, r, pid=ACCENT, axis="z", seg=12, tseg=6, name="ring"):
    """A torus: centre c, big radius R, cord radius r, its hole along `axis`."""
    pts = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        d = {"z": (math.cos(a) * R, math.sin(a) * R, 0), "y": (math.cos(a) * R, 0, math.sin(a) * R),
             "x": (0, math.cos(a) * R, math.sin(a) * R)}[axis]
        pts.append((c[0] + d[0], c[1] + d[1], c[2] + d[2]))
    return tube(pts, r, pid, seg=tseg, closed=True, name=name)


SKIP = {"n", "tilt", "keep", "moss", "beads", "w", "az_front", "atlas", "tris", "len", "lichen", "wet", "paintwear"}


def _walk(v, f, key=None):
    if isinstance(v, bool) or key in SKIP:
        return v
    if isinstance(v, (int, float)):
        return v * f
    if isinstance(v, list):
        return [_walk(x, f, key) for x in v]
    if isinstance(v, dict):
        return {k: _walk(x, f, k) for k, x in v.items()}
    return v


def rescale(f):
    """Scale every part and every mark about the pivot (the ground at the centre)."""
    for o in PARTS:
        o.data.transform(Matrix.Scale(f, 4))
    for k in list(MARKS):
        MARKS[k] = _walk(MARKS[k], f, k)


def nod(objs, pivot, deg):
    """Tip parts forward (toward +z, the front) by deg about the x axis through authoring point pivot."""
    pv = B(pivot)
    M = Matrix.Translation(pv) @ Matrix.Rotation(math.radians(deg), 4, "X") @ Matrix.Translation(-pv)
    for o in objs:
        o.data.transform(M)


def nodded(p, pivot, deg):
    a = math.radians(deg)
    y, z = p[1] - pivot[1], p[2] - pivot[2]
    return [p[0], pivot[1] + y * math.cos(a) - z * math.sin(a), pivot[2] + z * math.cos(a) + y * math.sin(a)]


def _bool(target, cutters, op):
    col = bpy.data.collections.new("ops")
    for o in cutters:
        col.objects.link(o)
    m = target.modifiers.new("b", "BOOLEAN")
    m.operation = op; m.operand_type = "COLLECTION"; m.collection = col; m.solver = "EXACT"
    try:
        m.material_mode = "TRANSFER"
    except Exception:
        pass
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(target.evaluated_get(dg))
    target.modifiers.clear()
    target.data = me
    bpy.data.collections.remove(col)


def carve(target, *cutters):
    """Cut the cutters out of target (exact boolean) and throw them away. A cutter's material shows in the cut."""
    _bool(target, cutters, "DIFFERENCE")
    for o in cutters:
        if o in PARTS:
            PARTS.remove(o)
        bpy.data.objects.remove(o)
    return target


def weld(name, budget, weight=None):
    """Union every part into ONE closed skin, clean it, unwrap it. Returns the object."""
    base, rest = PARTS[0], PARTS[1:]
    smooth_mats = {}
    for o in PARTS:
        smooth_mats.setdefault(o.data.materials[0].name, o["smooth"])
    if rest:
        _bool(base, rest, "UNION")
        for o in rest:
            bpy.data.objects.remove(o)
    base.name = name
    me = base.data
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=0.0008)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges[:], dist=0.0005)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.6), verts=bm.verts[:], edges=bm.edges[:], delimit={"MATERIAL"})
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    # the boolean can leave a stray wire edge where two cutters met: not part of the skin
    wires = [e for e in bm.edges if not e.link_faces]
    if wires:
        bmesh.ops.delete(bm, geom=wires, context="EDGES")
    for v in [v for v in bm.verts if not v.link_faces]:
        bm.verts.remove(v)
    # a membrane left inside where two parts shared a whole face: every one of its edges has more than two faces
    inner = [f for f in bm.faces if all(len(e.link_faces) > 2 for e in f.edges)]
    if inner:
        bmesh.ops.delete(bm, geom=inner, context="FACES")
    # sliver gaps where three parts met: weld only the verts on the gap, then fill what is left
    span = max(max(v.co[i] for v in bm.verts) - min(v.co[i] for v in bm.verts) for i in range(3))
    gap = [e for e in bm.edges if len(e.link_faces) == 1]
    if gap:
        bmesh.ops.remove_doubles(bm, verts=list({v for e in gap for v in e.verts}), dist=span * 0.006)
        bmesh.ops.dissolve_degenerate(bm, edges=bm.edges[:], dist=0.0005)
        gap = [e for e in bm.edges if len(e.link_faces) == 1]
        if gap:
            bmesh.ops.holes_fill(bm, edges=gap, sides=12)
            bmesh.ops.triangulate(bm, faces=bm.faces[:])
        for e in [e for e in bm.edges if not e.link_faces]:
            bm.edges.remove(e)
        for v in [v for v in bm.verts if not v.link_faces]:
            bm.verts.remove(v)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    tris = len(bm.faces)
    print("WELD", name, "triangles before any decimation", tris, "holes (edges with one face)", sum(1 for e in bm.edges if len(e.link_faces) == 1),
          "touching edges (more than two faces)", sum(1 for e in bm.edges if len(e.link_faces) > 2))
    bm.to_mesh(me); bm.free()
    if tris > budget:
        d = base.modifiers.new("d", "DECIMATE"); d.ratio = budget / tris
        dg = bpy.context.evaluated_depsgraph_get()
        base.data = bpy.data.meshes.new_from_object(base.evaluated_get(dg)); base.modifiers.clear()
        me = base.data
    bm = bmesh.new(); bm.from_mesh(me)
    edges_open = sum(1 for e in bm.edges if len(e.link_faces) == 1)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    me.set_sharp_from_angle(angle=math.radians(34))
    bpy.context.view_layer.objects.active = base
    for o in bpy.data.objects:
        o.select_set(o == base)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(62), island_margin=0.003, area_weight=0.0, correct_aspect=True, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    if weight:
        # give the parts that matter (the face, the eyes, the cloth) more of the atlas, and the hidden underside less
        from bpy_extras import mesh_utils
        uv = me.uv_layers.active.data
        names = [m.name for m in me.materials]
        for island in mesh_utils.mesh_linked_uv_islands(me):
            ws, loops = [], []
            for pi in island:
                poly = me.polygons[pi]
                c, n = poly.center, poly.normal
                ws.append(weight((c.x, c.z, -c.y), (n.x, n.z, -n.y), int(names[poly.material_index][1:]) % 16))
                loops.extend(poly.loop_indices)
            k = math.sqrt(sum(ws) / len(ws))
            cx = sum(uv[i].uv.x for i in loops) / len(loops); cy = sum(uv[i].uv.y for i in loops) / len(loops)
            for i in loops:
                uv[i].uv = ((uv[i].uv.x - cx) * k + cx, (uv[i].uv.y - cy) * k + cy)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT"); bpy.ops.uv.select_all(action="SELECT")
        bpy.ops.uv.pack_islands(margin=0.003, rotate=True)
        bpy.ops.object.mode_set(mode="OBJECT")
    return base, len(me.polygons), edges_open


def _tree(m, image, kind, lo, hi, scale):
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(em.outputs[0], out.inputs[0])
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = image; nt.nodes.active = tex
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    if kind == "pos":
        a = nt.nodes.new("ShaderNodeVectorMath"); a.operation = "SUBTRACT"; a.inputs[1].default_value = lo
        b = nt.nodes.new("ShaderNodeVectorMath"); b.operation = "DIVIDE"; b.inputs[1].default_value = tuple(h - l for h, l in zip(hi, lo))
        nt.links.new(geo.outputs["Position"], a.inputs[0]); nt.links.new(a.outputs[0], b.inputs[0]); nt.links.new(b.outputs[0], em.inputs[0])
    elif kind == "nrm":
        a = nt.nodes.new("ShaderNodeVectorMath"); a.operation = "MULTIPLY_ADD"
        a.inputs[1].default_value = (0.5, 0.5, 0.5); a.inputs[2].default_value = (0.5, 0.5, 0.5)
        nt.links.new(geo.outputs["Normal"], a.inputs[0]); nt.links.new(a.outputs[0], em.inputs[0])
    elif kind in ("ao", "ao2"):
        a = nt.nodes.new("ShaderNodeAmbientOcclusion"); a.samples = 16
        a.inputs["Distance"].default_value = scale * (0.07 if kind == "ao" else 0.30)
        nt.links.new(a.outputs["AO"], em.inputs[0])
    elif kind == "edge":
        bv = nt.nodes.new("ShaderNodeBevel"); bv.samples = 8; bv.inputs["Radius"].default_value = scale * 0.014
        d = nt.nodes.new("ShaderNodeVectorMath"); d.operation = "DOT_PRODUCT"
        nt.links.new(bv.outputs[0], d.inputs[0]); nt.links.new(geo.outputs["True Normal"], d.inputs[1])
        s = nt.nodes.new("ShaderNodeMath"); s.operation = "SUBTRACT"; s.inputs[0].default_value = 1.0
        nt.links.new(d.outputs["Value"], s.inputs[1]); nt.links.new(s.outputs[0], em.inputs[0])
    elif kind == "id":
        v = (int(m.name[1:]) + 1) / 32.0
        em.inputs[0].default_value = (v, v, v, 1)


def bake(obj, name, size, scale):
    """Bake the six maps of the skin into <WORK>/<name>_maps.npz. scale = the piece's height, to size AO and bevels."""
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 24
    sc.render.bake.margin = max(6, size // 128); sc.render.bake.use_clear = True
    sc.render.bake.use_selected_to_active = False
    bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    lo = tuple(min(p[i] for p in bb) - 0.05 for i in range(3)); hi = tuple(max(p[i] for p in bb) + 0.05 for i in range(3))
    maps = {}
    for kind in ("pos", "nrm", "ao", "ao2", "edge", "id"):
        img = bpy.data.images.new("bake_" + kind, size, size, alpha=False, float_buffer=True)
        img.colorspace_settings.name = "Non-Color"
        for m in obj.data.materials:
            _tree(m, img, kind, lo, hi, scale)
        sc.cycles.samples = 1 if kind in ("pos", "nrm", "id") else 32
        bpy.ops.object.bake(type="EMIT")
        a = np.empty(size * size * 4, dtype=np.float32); img.pixels.foreach_get(a)
        a = a.reshape(size, size, 4)[::-1]
        maps[kind] = a[:, :, :3].copy() if kind in ("pos", "nrm") else a[:, :, 0].copy()
        bpy.data.images.remove(img)
    os.makedirs(WORK, exist_ok=True)
    np.savez_compressed(os.path.join(WORK, name + "_maps.npz"), lo=np.array(lo), hi=np.array(hi), **maps)


def finish(name, size=2048, budget=8000, scale=None, weight=None, height=None):
    if height:                                    # bring the whole piece to a stated height
        top = max(v.co.z for o in PARTS for v in o.data.vertices)
        rescale(height / top)
    obj, tris, open_edges = weld(name, budget, weight)
    bb = [Vector(c) for c in obj.bound_box]
    dims = [max(p[i] for p in bb) - min(p[i] for p in bb) for i in range(3)]
    height = max(p[2] for p in bb)
    bake(obj, name, size, scale or max(dims))
    MARKS.update({"name": name, "tris": tris, "open_edges": open_edges, "size_xyz": [round(dims[0], 3), round(dims[2], 3), round(dims[1], 3)],
                  "height": round(height, 3), "atlas": size})
    json.dump(MARKS, open(os.path.join(WORK, name + ".json"), "w"), indent=1)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, name + "_low.blend"))
    print("BUILT", name, "tris", tris, "open edges", open_edges, "size", MARKS["size_xyz"])
