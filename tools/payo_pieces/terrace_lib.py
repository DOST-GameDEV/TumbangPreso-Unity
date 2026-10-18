"""PAYO land kit: the modelling helpers (Blender Python). A piece is built face by face with its own UVs and vertex
colour, then welded. Blender axes while building: X along, Y into the hill, Z up. The glTF export turns that into
metres, y up, with the piece's front (the downhill side) on +z.
"""
import math
import os
import random

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "terrace")
TEX = os.path.join(OUT, "tex")

WHITE = (1, 1, 1, 1)


def V(*a):
    return Vector(a if len(a) > 1 else a[0])


class MB:
    """A mesh under construction: faces with their own corners (welded at the end), per corner uv and colour."""

    def __init__(self):
        self.v, self.f, self.uv, self.col, self.sm = [], [], [], [], []

    def face(self, pts, uvs=None, cols=None, smooth=False):
        n = len(self.v)
        self.v.extend(tuple(p) for p in pts)
        self.f.append(tuple(range(n, n + len(pts))))
        self.uv.append([tuple(u) for u in (uvs or [(0, 0)] * len(pts))])
        if cols is None:
            cols = [WHITE] * len(pts)
        elif not isinstance(cols[0], (tuple, list)):
            cols = [tuple(cols)] * len(pts)
        self.col.append([tuple(c) if len(c) == 4 else tuple(c) + (1,) for c in cols])
        self.sm.append(smooth)

    def add(self, other, m=None):
        n = len(self.v)
        self.v.extend(tuple(m @ Vector(p)) if m else p for p in other.v)
        self.f.extend(tuple(i + n for i in f) for f in other.f)
        self.uv.extend(other.uv); self.col.extend(other.col); self.sm.extend(other.sm)

    def tris(self):
        return sum(len(f) - 2 for f in self.f)

    def obj(self, name, mat, weld=1e-5):
        me = bpy.data.meshes.new(name)
        me.from_pydata(self.v, [], self.f)
        me.uv_layers.new(name="UVMap")
        me.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
        uvl, ca = me.uv_layers["UVMap"], me.color_attributes["Col"]      # fetched again: adding a layer moves the others
        i = 0
        for fi, poly in enumerate(me.polygons):
            poly.use_smooth = self.sm[fi]
            for k in range(len(self.f[fi])):
                uvl.data[i].uv = self.uv[fi][k]
                ca.data[i].color = self.col[fi][k]
                i += 1
        me.materials.append(mat)
        if weld:
            bm = bmesh.new(); bm.from_mesh(me)
            bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=weld)
            bm.to_mesh(me); bm.free()
        me.update()
        activate(me)
        ob = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(ob)
        return ob


_mats = {}


def activate(me):
    """Make the mesh's colour layer the one the exporter writes."""
    if me.color_attributes:
        ca = me.color_attributes[0]
        me.color_attributes.active_color = ca
        me.color_attributes.render_color_index = 0


def material(name, tex=None, alpha=False, colour=(1, 1, 1, 1)):
    if name in _mats:
        return _mats[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Roughness"].default_value = 0.9
    b.inputs["Base Color"].default_value = colour
    if tex:
        n = m.node_tree.nodes.new("ShaderNodeTexImage")
        n.image = bpy.data.images.load(os.path.join(TEX, tex), check_existing=True)
        m.node_tree.links.new(n.outputs["Color"], b.inputs["Base Color"])
        if alpha:
            m.node_tree.links.new(n.outputs["Alpha"], b.inputs["Alpha"])
            m.blend_method = "BLEND"
    elif colour[3] < 1:
        b.inputs["Alpha"].default_value = colour[3]
        m.blend_method = "BLEND"
    _mats[name] = m
    return m


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _mats.clear()


def export(name, objects, meta=None):
    """Write the named objects as one glb and return its triangle count."""
    bpy.ops.object.select_all(action="DESELECT")
    tris = 0
    for o in objects:
        o.select_set(True)
        tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
    path = os.path.join(OUT, name + ".glb")
    kw = dict(filepath=path, use_selection=True, export_format="GLB", export_yup=True, export_apply=True)
    try:
        bpy.ops.export_scene.gltf(export_vertex_color="ACTIVE", export_all_vertex_colors=False, **kw)
    except TypeError:
        bpy.ops.export_scene.gltf(**kw)
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for o in objects:
        for c in o.bound_box:
            p = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    size = hi - lo
    print("PIECE %s: %d triangles, %.2f x %.2f x %.2f m (along, deep, tall), materials %s" % (
        name, tris, size.x, size.y, size.z, sorted({m.name for o in objects for m in o.data.materials})))
    return dict(triangles=tris, size=[round(size.x, 2), round(size.y, 2), round(size.z, 2)],
                materials=sorted({m.name for o in objects for m in o.data.materials}), **(meta or {}))


# ------------------------------------------------------------------------------------------------ shapes
def blade(mb, base, out, h, w, lean, cell, r, shade=1.0, droop=0.0, cells=8, tipcell=None):
    """One chunky leaf blade: a quad and a tip triangle, bending outward. cell is its column in the plant picture."""
    z = Vector((0, 0, 1))
    side = z.cross(out).normalized()
    b = Vector(base)
    mid = b + z * (h * 0.58) + out * (lean * h * 0.30)
    tip = b + z * (h * (1.0 - droop)) + out * (lean * h * (1.0 + droop))
    u0, u1 = (cell + 0.08) / cells, (cell + 0.92) / cells
    um = (u0 + u1) / 2
    dk = (0.62 * shade, 0.62 * shade, 0.62 * shade, 1)
    lt = (shade, shade, shade, 1)
    p0, p1 = b - side * (w * 0.5), b + side * (w * 0.5)
    m0, m1 = mid - side * (w * 0.42), mid + side * (w * 0.42)
    mb.face([p0, p1, m1, m0], [(u0, 0.02), (u1, 0.02), (u1, 0.6), (u0, 0.6)], [dk, dk, lt, lt])
    mb.face([m0, m1, tip], [(u0, 0.6), (u1, 0.6), (um, 0.98)], [lt, lt, lt])


def tuft(mb, base, r, n=5, h=0.4, w=0.05, lean=0.45, cell=0, shade=1.0, droop=0.0, spread=0.03, turn=None):
    a0 = r.uniform(0, 6.28) if turn is None else turn
    for i in range(n):
        a = a0 + i * 6.283 / n + r.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), math.sin(a), 0))
        blade(mb, Vector(base) + out * spread, out, h * r.uniform(0.8, 1.08), w * r.uniform(0.85, 1.15),
              lean * r.uniform(0.6, 1.3), cell, r, shade * r.uniform(0.9, 1.06), droop)


def leaf(mb, base, out, length, width, cell, r, up=0.5, shade=1.0, cells=8):
    """A broad leaf (taro, fern frond): a diamond of four triangles folded along its rib, arching out and over."""
    z = Vector((0, 0, 1))
    side = z.cross(out).normalized()
    b = Vector(base)
    m = b + out * (length * 0.5) + z * (length * up)
    t = b + out * length + z * (length * up * 0.55)
    l, rr = m - side * (width / 2) - z * (width * 0.12), m + side * (width / 2) - z * (width * 0.12)
    u0, u1 = (cell + 0.04) / cells, (cell + 0.96) / cells
    um = (u0 + u1) / 2
    c = (shade, shade, shade, 1)
    d = (shade * 0.7, shade * 0.7, shade * 0.7, 1)
    mb.face([b, rr, m], [(um, 0.02), (u1, 0.5), (um, 0.5)], [d, c, c])
    mb.face([b, m, l], [(um, 0.02), (um, 0.5), (u0, 0.5)], [d, c, c])
    mb.face([m, rr, t], [(um, 0.5), (u1, 0.5), (um, 0.98)], [c, c, c])
    mb.face([m, t, l], [(um, 0.5), (um, 0.98), (u0, 0.5)], [c, c, c])


def tube(mb, pts, radius, seg=8, uv=(0, 1, 0.75, 1.0), metres_u=1.6, cap=True, col=WHITE, radii=None):
    """A round bar along pts. uv = (u offset, u scale unused, v0, v1): u runs along the length, v round the bar."""
    pts = [Vector(p) for p in pts]
    rings, dist = [], 0.0
    us = []
    for i, p in enumerate(pts):
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        ref = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
        a = d.cross(ref).normalized(); b = d.cross(a).normalized()
        rr = radii[i] if radii else radius
        rings.append([p + (a * math.cos(k * 6.2832 / seg) + b * math.sin(k * 6.2832 / seg)) * rr for k in range(seg)])
        if i:
            dist += (p - pts[i - 1]).length
        us.append(uv[0] + dist / metres_u)
    v0, v1 = uv[2], uv[3]
    for i in range(len(pts) - 1):
        for k in range(seg):
            k2 = (k + 1) % seg
            va, vb = v0 + (v1 - v0) * k / seg, v0 + (v1 - v0) * (k + 1) / seg
            mb.face([rings[i][k], rings[i][k2], rings[i + 1][k2], rings[i + 1][k]],
                    [(us[i], va), (us[i], vb), (us[i + 1], vb), (us[i + 1], va)], col, smooth=True)
    if cap:
        vm = (v0 + v1) / 2
        mb.face(list(reversed(rings[0])), [(us[0], vm)] * seg, [(0.35, 0.3, 0.25, 1)] * seg)
        mb.face(rings[-1], [(us[-1], vm)] * seg, [(0.35, 0.3, 0.25, 1)] * seg)


def box(mb, lo, hi, uv=(0, 0, 1, 1), col=WHITE, m=None):
    x0, y0, z0 = lo; x1, y1, z1 = hi
    c = [Vector(p) for p in ((x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1))]
    if m:
        c = [m @ p for p in c]
    u0, v0, u1, v1 = uv
    q = [(u0, v0), (u1, v0), (u1, v1), (u0, v1)]
    for idx in ((0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7), (4, 5, 6, 7), (3, 2, 1, 0)):
        mb.face([c[i] for i in idx], q, col)


def blob(mb, centre, radii, r, seg=10, rings=6, jitter=0.12, uvbox=(0, 0, 1, 1), colour=None, smooth=True, flat_bottom=None):
    """A lumpy ball. colour(normal, point) gives the corner colour."""
    cx, cy, cz = centre
    grid = []
    for j in range(rings + 1):
        th = math.pi * j / rings
        row = []
        for i in range(seg):
            ph = 6.2832 * i / seg
            n = Vector((math.sin(th) * math.cos(ph), math.sin(th) * math.sin(ph), math.cos(th)))
            k = 1 + (r.uniform(-jitter, jitter) if 0 < j < rings else 0)
            p = Vector((cx + n.x * radii[0] * k, cy + n.y * radii[1] * k, cz + n.z * radii[2] * k))
            if flat_bottom is not None and p.z < flat_bottom:
                p.z = flat_bottom
            row.append((p, n))
        grid.append(row)
    u0, v0, u1, v1 = uvbox
    for j in range(rings):
        for i in range(seg):
            i2 = (i + 1) % seg
            quad = [grid[j + 1][i], grid[j + 1][i2], grid[j][i2], grid[j][i]]
            uvs = [(u0 + (u1 - u0) * (ii / seg), v0 + (v1 - v0) * (1 - jj / rings)) for jj, ii in ((j + 1, i), (j + 1, i + 1), (j, i + 1), (j, i))]
            cols = [colour(n, p) if colour else WHITE for p, n in quad]
            pts = [p for p, n in quad]
            if j == 0:
                mb.face(pts[:2] + [pts[2]], uvs[:3], cols[:3], smooth)
            elif j == rings - 1:
                mb.face([pts[0], pts[2], pts[3]], [uvs[0], uvs[2], uvs[3]], [cols[0], cols[2], cols[3]], smooth)
            else:
                mb.face(pts, uvs, cols, smooth)


def rock_object(name, mat, r, size, cuts=9, bevel=0.07, tile=2.5, moss=0.6, at=(0, 0, 0), sink=0.12):
    """A chunky boulder: a block cut by random planes, every edge given a wide bevel. Vertex colour paints it: pale
    bevels, a dark foot, moss where it faces the sky (alpha of the colour is the moss amount, should a shader want it).
    """
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges, cuts=1, use_grid_fill=True)
    for v in bm.verts:
        n = v.co.normalized()
        v.co = v.co * 0.55 + n * 0.5 * 0.6
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    for _ in range(cuts):
        n = Vector((r.uniform(-1, 1), r.uniform(-1, 1), r.uniform(-0.25, 1.0))).normalized()
        d = r.uniform(0.30, 0.47)
        co = Vector((n.x * size[0] * d, n.y * size[1] * d, n.z * size[2] * d))
        geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
        res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=n, clear_outer=True)
        edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if edges:
            bmesh.ops.edgeloop_fill(bm, edges=edges)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(4), verts=bm.verts, edges=bm.edges)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.03 * max(size))
    faces_before = set(f for f in bm.faces)
    for f in bm.faces:
        f.tag = True
    res = bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, segments=1, profile=0.5, affect="EDGES", clamp_overlap=True)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.faces.ensure_lookup_table()
    zlo = min(v.co.z for v in bm.verts)
    zhi = max(v.co.z for v in bm.verts)
    uvl = bm.loops.layers.uv.new("UVMap")
    cl = bm.loops.layers.color.new("Col")
    bevel_faces = set(res["faces"])
    bm.normal_update()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        isb = f in bevel_faces
        for l in f.loops:
            p = l.vert.co
            uv = [(p.y, p.z), (p.x, p.z), (p.x, p.y)][ax]
            l[uvl].uv = (uv[0] / tile + 0.37 * ax, uv[1] / tile + 0.21 * ax)
            hgt = (p.z - zlo) / max(1e-6, zhi - zlo)
            k = 0.72 + 0.34 * hgt
            if isb:
                k *= 1.22 if n.z > -0.2 else 0.8
            c = Vector((k, k, k * 0.99))
            m = max(0.0, min(1.0, (n.z - (1.0 - moss)) * 3.0)) * (0.5 + 0.5 * hgt)
            if n.z > 0.8:
                m = max(m, moss * 0.9)
            c = c.lerp(Vector((0.60, 0.92, 0.36)) * (1.08 if isb else 0.92), m)
            if hgt < 0.12:
                c = c.lerp(Vector((0.36, 0.42, 0.30)), 0.5)
            l[cl] = (min(1, c.x), min(1, c.y), min(1, c.z), 1.0)
        f.smooth = False
    for v in bm.verts:
        v.co.z -= zlo + sink * (zhi - zlo)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    activate(me)
    ob = bpy.data.objects.new(name, me)
    ob.location = at
    bpy.context.scene.collection.objects.link(ob)
    return ob
