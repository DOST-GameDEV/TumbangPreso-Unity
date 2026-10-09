"""Model the Eskinita Alley prop kit in Blender, export one glb per prop, and review it.

  blender -b --python tools/author_eskinita_props.py -- [--sheet N] [--only name,name]

Writes:
  ArtSource/eskinita/props.blend                              one collection per prop, live bevels
  Assets/TumbangPreso/Art/EskinitaAlley/Models/prop_<name>.glb one joined mesh, +Y up, bevels applied
  Assets/TumbangPreso/Art/EskinitaAlley/materials_props.json   the materials, by NAME (glb has no images)
  Assets/TumbangPreso/Art/EskinitaAlley/props_manifest.json    name, size, anchor and a placing note
  Logs/eskinita/props_sheet_vN.png, props_group_<x>_vN.png     with --sheet N

The painted sign textures come from tools/author_eskinita_props_textures.py (PIL, plain python),
which this script runs when they are missing; it also composes the contact sheet.

CONVENTIONS
  * Metres. Z up, the prop's front faces -Y. Ground props stand on z = 0. Wall props have their
    back 2 cm behind y = 0 (put y = 0 on the wall face and they sink in). Hanging props
    (banderitas, laundry line) have both end points at z = 0, x = -L/2 and +L/2, and sag below.
  * House style (docs/KANTO_DESIGN_GUIDE.md 2, 3, 5): chunky simple pieces, every solid object
    carries a live Bevel modifier, attached parts PENETRATE 1 to 2 cm, nothing shares a plane,
    nothing floats, every prop has a colour break. Words are painted textures, never geometry.
  * UVs of tiling materials are in METRES; Unity multiplies by the material's "tiling". Decal
    faces carry their own 0..1 UVs into a painted sheet. Leaf cards carry 0..1 UVs of `leaf`.
  * Tints in the JSON are sRGB 0..1 and multiply the texture.
  * Nothing near offence orange f87020 or defence blue 0080e8.
"""
import json
import math
import random
import subprocess
import sys
import tempfile
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets" / "TumbangPreso" / "Art" / "EskinitaAlley"
MODELS = ART / "Models"
TEX = ART / "Textures"
SOURCE = ROOT / "ArtSource" / "eskinita"
LOGS = ROOT / "Logs" / "eskinita"
TILES = Path(tempfile.gettempdir()) / "esk_props_tiles"
PAINTER = ROOT / "tools" / "author_eskinita_props_textures.py"
UP = Vector((0, 0, 1))

# ------------------------------------------------------------------ materials

MATS = {}


def M(name, tex, tint, tiling=1.0, fallback=None, **flags):
    """Register a material. `tint` (sRGB hex) multiplies the texture; `fallback` is the flat
    colour used in Blender when the texture file is not there yet."""
    MATS[name] = dict(texture=tex, tint=tint, tiling=tiling, fallback=fallback or tint, flags=flags)
    return name


WOOD = M("ekp_wood", "wood", "c08a58", 1.0, "9a6a40")
WOODD = M("ekp_wood_dark", "wood", "855a3e", 1.0, "6a4630")
PLANK = M("ekp_planks", "planks", "d6a874", 0.5, "b98a58")
TAN = M("ekp_tan", "paint", "d9b47e")
CONC = M("ekp_concrete", "concrete", "d2cabb", 0.5, "b9b2a4")
CHB = M("ekp_chb", "chb", "ffffff", 0.5, "a9a59a")
TIN = M("ekp_tin", "tin", "b7cfc4", 0.5, "9fb0a8")
RUST = M("ekp_tin_rust", "tin_rust", "ffffff", 0.5, "9a5a3c")
PLAS = M("ekp_plaster", "plaster", "f6d3b0", 0.333, "eac8a4")
DARK = M("ekp_metal_dark", "", "3c4644")
STEEL = M("ekp_steel", "", "c9cfca", glossy=True)
RUB = M("ekp_rubber", "", "2b2a2c")
RED = M("ekp_red", "paint", "c8372d")
YEL = M("ekp_yellow", "paint", "f2c230")
TEAL = M("ekp_teal", "paint", "2f9e8a")
CREAM = M("ekp_cream", "paint", "f3e7c8")
PINK = M("ekp_pink", "paint", "d9457a")
GREEN = M("ekp_green", "paint", "3f8a4c")
VIOLET = M("ekp_violet", "paint", "7a54a6")
WHITE = M("ekp_white", "paint", "f6f2e8")
CLAY = M("ekp_clay", "", "a9523c")
SOIL = M("ekp_soil", "", "3a2a20")
LL = M("ekp_leaf_light", "leaf", "8ed04a", 1.0, "6fae3c", foliage=True)
LD = M("ekp_leaf_dark", "leaf", "3f8a40", 1.0, "2f6a34", foliage=True)
LY = M("ekp_leaf_croton", "leaf", "ecd848", 1.0, "c9c23a", foliage=True)
T_TEAL = M("ekp_trapal_teal", "trapal_teal", "ffffff", 1.0, "2f9e8a")
T_RED = M("ekp_trapal_red", "trapal_red", "ffffff", 1.0, "c8372d")
T_YEL = M("ekp_trapal_yellow", "trapal_yellow", "ffffff", 1.0, "f2c230")
SAW = M("ekp_sawali", "sawali", "ffffff", 1.0, "c9a25e")
LAMP = M("ekp_lamp", "", "ffe9a8", emissive=True)
S_SARI = M("ekp_sign_sarisari", "prop_sign_sarisari", "ffffff", 1.0, "f3e7c8")
S_ARCH = M("ekp_sign_arch", "prop_sign_arch", "ffffff", 1.0, "1a5446")
DEC = M("ekp_decals", "prop_decals", "ffffff", 1.0, "f3e7c8")

FLAGS = ("foliage", "emissive", "glossy", "cutout")

# Regions of prop_decals_albedo.png in pixels (x0, y0, x1, y1), y down. Same table as the painter.
DECAL_PX = {
    "backboard": (0, 0, 512, 384), "brgy": (512, 0, 1024, 384), "dama": (0, 384, 384, 768),
    "sachets": (384, 384, 512, 768), "poster": (512, 384, 768, 768), "meter": (768, 384, 1024, 640),
    "plate": (768, 640, 1024, 768), "crate": (0, 768, 384, 896), "tag": (0, 896, 384, 1024),
    "sack": (384, 768, 768, 1024), "wash": (768, 768, 1024, 1024),
}
FULL = (0.0, 0.0, 1.0, 1.0)


def region(key, inset=4):
    x0, y0, x1, y1 = DECAL_PX[key]
    return ((x0 + inset) / 1024, 1 - (y1 - inset) / 1024, (x1 - inset) / 1024, 1 - (y0 + inset) / 1024)


def srgb(hexs):
    return tuple(int(hexs[i:i + 2], 16) / 255 for i in (0, 2, 4))


def linear(c):
    return ((c + 0.055) / 1.055) ** 2.4 if c > 0.04045 else c / 12.92


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    spec = MATS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    path = TEX / f"{spec['texture']}_albedo.png" if spec["texture"] else None
    has = path is not None and path.exists()
    rgb = tuple(linear(c) for c in srgb(spec["tint"] if has or not spec["texture"] else spec["fallback"]))
    m.diffuse_color = (*tuple(linear(c) for c in srgb(spec["fallback"])), 1)
    bsdf.inputs["Base Color"].default_value = (*rgb, 1)
    bsdf.inputs["Roughness"].default_value = 0.3 if spec["flags"].get("glossy") else 0.85
    if spec["flags"].get("glossy"):
        bsdf.inputs["Metallic"].default_value = 0.6
    if spec["flags"].get("emissive"):
        bsdf.inputs["Emission Color"].default_value = (*rgb, 1)
        bsdf.inputs["Emission Strength"].default_value = 4.0
    if has:
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(path), check_existing=True)
        uv = nodes.new("ShaderNodeUVMap")
        mp = nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (spec["tiling"], spec["tiling"], 1)
        links.new(uv.outputs["UV"], mp.inputs["Vector"])
        links.new(mp.outputs["Vector"], tex.inputs["Vector"])
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(tex.outputs["Color"], mix.inputs[6])
        mix.inputs[7].default_value = (*rgb, 1)
        links.new(mix.outputs[2], bsdf.inputs["Base Color"])
        if spec["flags"].get("foliage") or spec["flags"].get("cutout"):
            links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    if spec["flags"].get("foliage") or spec["flags"].get("cutout"):
        m.use_backface_culling = False
        if hasattr(m, "surface_render_method"):
            m.surface_render_method = "DITHERED"
    return m


# ------------------------------------------------------------------ geometry helpers

def R(axis, deg):
    return Matrix.Rotation(math.radians(deg), 4, axis)


def T(v):
    return Matrix.Translation(Vector(v))


def catmull(points, per=6):
    """A smooth polyline through `points` (a cord, a wire, a pipe)."""
    pts = [Vector(p) for p in points]
    ext = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    out = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for k in range(per):
            t = k / per
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t))
    out.append(pts[-1])
    return out


def sag_z(x, length, sag):
    """A hung line between x = -L/2 and +L/2, both ends at z = 0."""
    u = 2 * x / length
    return -sag * (1 - u * u)


class B:
    """One object's geometry in one bmesh. Everything is placed through the current matrix `M`."""

    def __init__(self, name, bevel=0.015, segs=2, foliage=False, smooth=None):
        self.name, self.bm, self.mats = name, bmesh.new(), []
        self.smooth = smooth if smooth is not None else (42 if bevel else 70)
        self.bevel, self.segs, self.foliage = bevel, segs, foliage
        self.M = Matrix()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.flag = self.bm.faces.layers.int.new("decal")
        self.clump = {}

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def _paint(self, verts, mat):
        idx = self.mi(mat)
        for f in {f for v in verts for f in v.link_faces}:
            f.material_index = idx

    def box(self, c, size, mat, rot=None):
        r = bmesh.ops.create_cube(self.bm, size=1.0, matrix=self.M @ T(c) @ (rot or Matrix()) @ Matrix.Diagonal((*size, 1)))
        self._paint(r["verts"], mat)

    def cyl(self, c, r, h, mat, sides=12, r2=None, rot=None):
        """A cylinder (or cone frustum) centred on `c`, its axis local z."""
        res = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=sides, radius1=r, radius2=r if r2 is None else r2,
                                    depth=h, matrix=self.M @ T(c) @ (rot or Matrix()))
        self._paint(res["verts"], mat)

    def sphere(self, c, radii, mat, rot=None, subdiv=2):
        if isinstance(radii, (int, float)):
            radii = (radii, radii, radii)
        # bmesh counts from the bare icosahedron: subdiv 1 here is 80 faces, 2 is 320.
        r = bmesh.ops.create_icosphere(self.bm, subdivisions=subdiv + 1, radius=1.0,
                                       matrix=self.M @ T(c) @ (rot or Matrix()) @ Matrix.Diagonal((*radii, 1)))
        self._paint(r["verts"], mat)

    def beam(self, a, b, w, mat, h=None, roll=0.0):
        """A square member from a to b."""
        a, b = Vector(a), Vector(b)
        q = (b - a).to_track_quat("Z", "Y").to_matrix().to_4x4() @ R("Z", roll)
        self.box((a + b) / 2, (w, h or w, (b - a).length), mat, rot=q)

    def loft(self, rings, mat, mats=None, cap0=None, cap1=None, caps=True):
        """Skin consecutive rings of points; `mats[i]` paints the span after ring i."""
        vr = [[self.bm.verts.new(self.M @ Vector(p)) for p in ring] for ring in rings]
        n = len(vr[0])
        for i in range(len(vr) - 1):
            idx = self.mi(mats[i] if mats else mat)
            for j in range(n):
                f = self.bm.faces.new((vr[i][j], vr[i][(j + 1) % n], vr[i + 1][(j + 1) % n], vr[i + 1][j]))
                f.material_index = idx
        if caps:
            self.bm.faces.new(list(reversed(vr[0]))).material_index = self.mi(cap0 or (mats[0] if mats else mat))
            self.bm.faces.new(vr[-1]).material_index = self.mi(cap1 or (mats[-1] if mats else mat))

    def lathe(self, c, prof, mat, sides=14, mats=None, cap0=None, cap1=None, squash=(1.0, 1.0)):
        """Revolve (radius, z) points about the vertical through `c`."""
        c = Vector(c)
        rings = [[c + Vector((max(r, 0.002) * math.cos(a) * squash[0], max(r, 0.002) * math.sin(a) * squash[1], z))
                  for a in (k / sides * math.tau for k in range(sides))] for r, z in prof]
        self.loft(rings, mat, mats, cap0, cap1)

    def tube(self, pts, r, mat, sides=6, r1=None):
        """A round member along a polyline, radius r (tapering to r1)."""
        pts = [Vector(p) for p in pts]
        n = len(pts)
        rings, prev = [], None
        for i, p in enumerate(pts):
            t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
            if prev is None:
                a = UP if abs(t.z) < 0.9 else Vector((1, 0, 0))
                nrm = (a - t * a.dot(t)).normalized()
            else:
                nrm = (prev - t * prev.dot(t)).normalized()
            prev = nrm
            bn = t.cross(nrm)
            rr = r if r1 is None else r + (r1 - r) * i / (n - 1)
            rings.append([p + (nrm * math.cos(a) + bn * math.sin(a)) * rr for a in (k / sides * math.tau for k in range(sides))])
        self.loft(rings, mat)

    def torus(self, c, R_, r, mat, axis="z", seg=20, sides=8, mat_of=None, squash=1.0):
        c = Vector(c)
        e1, e2, n = {"z": (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))),
                     "x": (Vector((0, 1, 0)), Vector((0, 0, 1)), Vector((1, 0, 0))),
                     "y": (Vector((1, 0, 0)), Vector((0, 0, 1)), Vector((0, 1, 0)))}[axis]
        vs = []
        for i in range(seg):
            a = i / seg * math.tau
            rad = e1 * math.cos(a) + e2 * math.sin(a)
            vs.append([self.bm.verts.new(self.M @ (c + rad * (R_ + r * math.cos(k / sides * math.tau))
                                                   + n * (r * squash * math.sin(k / sides * math.tau)))) for k in range(sides)])
        for i in range(seg):
            for j in range(sides):
                a, b = vs[i], vs[(i + 1) % seg]
                f = self.bm.faces.new((a[j], b[j], b[(j + 1) % sides], a[(j + 1) % sides]))
                f.material_index = self.mi(mat_of(j) if mat_of else mat)

    def panel(self, c, w, h, t, body, face, reg=FULL, rot=None, back=None):
        """A board w wide, h tall, t thick; its front (local -Y) wears the painting `face` on UV
        `reg`. `back` paints the rear face too, mirrored so the words still read."""
        mtx = self.M @ T(c) @ (rot or Matrix())
        x, y, z = w / 2, t / 2, h / 2
        cs = [(-x, -y, -z), (x, -y, -z), (x, -y, z), (-x, -y, z), (-x, y, -z), (x, y, -z), (x, y, z), (-x, y, z)]
        v = [self.bm.verts.new(mtx @ Vector(p)) for p in cs]
        u0, v0, u1, v1 = reg
        body_i = self.mi(body)
        for quad in ((1, 5, 6, 2), (4, 0, 3, 7), (3, 2, 6, 7), (4, 5, 1, 0)):
            self.bm.faces.new([v[i] for i in quad]).material_index = body_i
        front = self.bm.faces.new((v[0], v[1], v[2], v[3]))
        front.material_index = self.mi(face)
        front[self.flag] = 1
        for l, st in zip(front.loops, ((u0, v0), (u1, v0), (u1, v1), (u0, v1))):
            l[self.uv].uv = st
        rear = self.bm.faces.new((v[5], v[4], v[7], v[6]))
        rear.material_index = self.mi(back or body)
        if back:
            rear[self.flag] = 1
            for l, st in zip(rear.loops, ((u0, v0), (u1, v0), (u1, v1), (u0, v1))):
                l[self.uv].uv = st

    def sheet(self, grid, thick, mat, normal=(0, -1, 0), reg=None, decal=None):
        """Cloth or sheet metal: a grid of points (rows of columns) with thickness behind it.
        `mat` is a name or f(row, col). With `reg`, the front wears `decal` across that UV rect."""
        rows, cols = len(grid), len(grid[0])
        n = Vector(normal).normalized()
        top = [[self.bm.verts.new(self.M @ Vector(p)) for p in row] for row in grid]
        bot = [[self.bm.verts.new(self.M @ (Vector(p) - n * thick)) for p in row] for row in grid]
        name = mat if callable(mat) else (lambda i, j: mat)
        for i in range(rows - 1):
            for j in range(cols - 1):
                ft = self.bm.faces.new((top[i][j], top[i][j + 1], top[i + 1][j + 1], top[i + 1][j]))
                fb = self.bm.faces.new((bot[i + 1][j], bot[i + 1][j + 1], bot[i][j + 1], bot[i][j]))
                fb.material_index = self.mi(name(i, j))
                ft.material_index = self.mi(decal if reg else name(i, j))
                if reg:
                    ft[self.flag] = 1
                    u0, v0, u1, v1 = reg
                    for l, (a, b) in zip(ft.loops, ((i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j))):
                        l[self.uv].uv = (u0 + (u1 - u0) * b / (cols - 1), v1 - (v1 - v0) * a / (rows - 1))
        edge = self.mi(name(0, 0))
        for i in (0, rows - 1):
            for j in range(cols - 1):
                self.bm.faces.new((top[i][j], top[i][j + 1], bot[i][j + 1], bot[i][j])).material_index = edge
        for j in (0, cols - 1):
            for i in range(rows - 1):
                self.bm.faces.new((top[i][j], top[i + 1][j], bot[i + 1][j], bot[i][j])).material_index = edge

    def leaf(self, at, tip, normal, length, width, mat, clump=None):
        """One leaf card from `at` along `tip`, facing `normal`; UVs 0..1, stem at v = 0."""
        side = normal.cross(tip).normalized() * (width / 2)
        base, end = at - tip * (length * 0.08), at + tip * (length * 0.92)
        f = self.bm.faces.new([self.bm.verts.new(self.M @ c) for c in (base - side, base + side, end + side, end - side)])
        f.material_index = self.mi(mat)
        f[self.flag] = 1
        for l, st in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
            l[self.uv].uv = st
        self.clump[len(self.bm.faces) - 1] = None if clump is None else self.M @ Vector(clump)

    def blade(self, base, tip, normal, length, width, mat, bend=0.0, segs=3):
        """A long leaf (banana, snake plant): a strip of cards that droops by `bend` towards -Z,
        the drawing stretched along it."""
        base, tip, normal = Vector(base), Vector(tip).normalized(), Vector(normal).normalized()
        side = normal.cross(tip).normalized() * (width / 2)
        prev = None
        for k in range(segs + 1):
            t = k / segs
            p = base + tip * (length * t) - UP * (bend * length * t * t)
            wv = side * (0.55 + 0.45 * math.sin(math.pi * min(1.0, 0.15 + t * 0.85)))
            cur = (self.bm.verts.new(self.M @ (p - wv)), self.bm.verts.new(self.M @ (p + wv)))
            if prev:
                f = self.bm.faces.new((prev[0], prev[1], cur[1], cur[0]))
                f.material_index = self.mi(mat)
                f[self.flag] = 1
                t0 = (k - 1) / segs
                for l, st in zip(f.loops, ((0, t0), (1, t0), (1, t), (0, t))):
                    l[self.uv].uv = st
                self.clump[len(self.bm.faces) - 1] = None
            prev = cur

    def world_uvs(self):
        """UVs in metres: walls run along the face and up, flats from above."""
        for f in self.bm.faces:
            if f[self.flag]:
                continue
            n = f.normal
            if abs(n.z) > 0.7:
                for l in f.loops:
                    l[self.uv].uv = (l.vert.co.x, l.vert.co.y)
                continue
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                l[self.uv].uv = (l.vert.co.dot(t), l.vert.co.z)

    def finish(self, collection):
        if not len(self.bm.faces):
            self.bm.free()
            return None
        if not self.foliage:
            bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        self.bm.normal_update()
        self.world_uvs()
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(material(m))
        for p in mesh.polygons:
            p.use_smooth = True
        if self.foliage:
            # Every card's normal points away from its clump's centre, so a clump of flat cards
            # shades as one soft ball. Long blades keep their own normal.
            normals = []
            for p in mesh.polygons:
                c = self.clump.get(p.index)
                for li in p.loop_indices:
                    v = mesh.vertices[mesh.loops[li].vertex_index].co
                    normals.append(tuple((v - c).normalized()) if c is not None else tuple(p.normal))
            mesh.normals_split_custom_set(normals)
        else:
            mesh.set_sharp_from_angle(angle=math.radians(self.smooth))
        obj = bpy.data.objects.new(self.name, mesh)
        collection.objects.link(obj)
        if self.bevel and not self.foliage:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments = self.bevel, self.segs
            mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
            mod.harden_normals, mod.use_clamp_overlap = True, True
        return obj


class Prop:
    """One prop = one collection `prop_<name>` holding a few bevelled objects."""

    def __init__(self, name):
        self.name = name
        self.col = bpy.data.collections.new(f"prop_{name}")
        bpy.context.scene.collection.children.link(self.col)
        self.bufs = []
        self.rng = random.Random(sum(ord(ch) for ch in name) * 7 + 3)

    def buf(self, tag="body", bevel=0.015, segs=2, foliage=False, smooth=None):
        b = B(f"{self.name}_{tag}", bevel, segs, foliage, smooth)
        self.bufs.append(b)
        return b

    def leaves(self):
        return self.buf("leaves", foliage=True)

    def cloth(self, tag="cloth"):
        return self.buf(tag, bevel=0)

    def finish(self):
        for b in self.bufs:
            b.finish(self.col)


def foliage(lv, center, radii, count, leaf_len, rng, floor_z=None, lift=0.25, spread=0.35, light=LL, dark=LD):
    """A clump of SHINGLED leaf cards (the house pattern): each card lies on the ellipsoid facing
    out, tip running down the surface, evenly spread on a Fibonacci sphere. No core. Two tints:
    light above, dark underneath."""
    center = Vector(center)
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(count):
        zf = 1 - 2 * (i + 0.5) / count
        r = math.sqrt(max(0.0, 1 - zf * zf))
        a = golden * i + rng.uniform(-0.3, 0.3)
        d = Vector((math.cos(a) * r, math.sin(a) * r, zf))
        p = center + Vector((d.x * radii[0], d.y * radii[1], d.z * radii[2])) * rng.uniform(0.9, 1.02)
        if floor_z is not None and p.z < floor_z:
            continue
        down = -UP - d * (-UP).dot(d)
        if down.length < 0.2:
            t = Vector((rng.gauss(0, 1), rng.gauss(0, 1), 0))
            down = t - d * t.dot(d)
        down.normalize()
        side = d.cross(down)
        ang = rng.uniform(-spread, spread)
        tip = (down * math.cos(ang) + side * math.sin(ang) + d * lift).normalized()
        n = (d - tip * d.dot(tip)).normalized()
        length = leaf_len * rng.uniform(0.85, 1.15)
        lv.leaf(p - tip * length * 0.35, tip, n, length, length * 0.7, light if d.z > -0.2 else dark, center)


# ------------------------------------------------------------------ shared pieces

def pot(b, c, r0, r1, h, mat, rim=None, top=SOIL, inner=0.05, sides=14):
    """A HOLLOW pot: wall, rolled rim, and soil `inner` below the rim. Returns the soil height."""
    rim = rim or mat
    prof = [(r0, 0), (r1, h - 0.035), (r1 + 0.014, h - 0.03), (r1 + 0.014, h), (r1 - 0.012, h), (r1 - 0.02, h - inner)]
    b.lathe(c, prof, mat, sides=sides, mats=[mat, rim, rim, rim, rim], cap1=top)
    return c[2] + h - inner


def paint_can(b, c, r, h, mat, band, top=SOIL, inner=0.05):
    """A cut tin can with a painted label band, used as a pot."""
    prof = [(r, 0), (r, h * 0.28), (r + 0.006, h * 0.30), (r + 0.006, h * 0.68), (r, h * 0.70), (r, h - 0.012),
            (r + 0.01, h - 0.01), (r + 0.01, h), (r - 0.01, h), (r - 0.016, h - inner)]
    b.lathe(c, prof, mat, mats=[mat, band, band, band, mat, STEEL, STEEL, STEEL, STEEL], cap1=top)
    return c[2] + h - inner


def tire(b, c, R_=0.24, r=0.105, band=None, axis="z"):
    """An old tire: dark tread, slightly lighter sidewalls, and sometimes a painted ring."""
    def mat_of(j):
        if band and j == 2:
            return band
        return DARK if j in (1, 2, 3, 6, 7, 8) else RUB
    b.torus(c, R_, r, RUB, axis=axis, seg=20, sides=10, squash=0.82, mat_of=mat_of)


def drum(b, c, mat, lid):
    prof = [(0.25, 0), (0.29, 0.04), (0.29, 0.27), (0.308, 0.29), (0.308, 0.33), (0.29, 0.35), (0.29, 0.57),
            (0.308, 0.59), (0.308, 0.63), (0.29, 0.65), (0.29, 0.86), (0.31, 0.875), (0.31, 0.905), (0.22, 0.93)]
    b.lathe(c, prof, mat, sides=16, mats=[mat] * 10 + [lid] * 3, cap1=lid)


def tabo(b, c, mat, yaw=0.0):
    """The water dipper that lives on every drum."""
    keep = b.M
    b.M = keep @ T(c) @ R("Z", yaw)
    b.lathe((0, 0, 0), [(0.06, 0), (0.075, 0.12), (0.062, 0.12), (0.05, 0.02)], mat, sides=10)
    b.box((0.13, 0, 0.1), (0.14, 0.035, 0.022), mat)
    b.M = keep


def hollow_block(b, mtx, mat=CHB):
    """A hollow block lying on its side face: two long walls, two ends and a web, all hollow."""
    keep = b.M
    b.M = keep @ mtx
    for s in (-1, 1):
        b.box((0, s * 0.082, 0.075), (0.40, 0.036, 0.15), mat)
        b.box((s * 0.18, 0, 0.075), (0.036, 0.15, 0.146), mat)
    b.box((0, 0, 0.075), (0.036, 0.15, 0.142), mat)
    b.M = keep


def crate(b, c, yaw, mat, caps=None):
    keep = b.M
    b.M = keep @ T(c) @ R("Z", yaw)
    b.box((0, 0, 0.125), (0.44, 0.30, 0.25), mat)
    for s in (-1, 1):
        b.box((s * 0.2, 0, 0.235), (0.07, 0.33, 0.05), mat)        # hand-hold ends, proud of the body
    rot = R("Z", 180)
    b.panel((0, -0.148, 0.125), 0.40, 0.133, 0.014, mat, DEC, region("crate"))
    b.panel((0, 0.148, 0.125), 0.40, 0.133, 0.014, mat, DEC, region("crate"), rot=rot)
    if caps:
        for i in range(4):
            for j in range(3):
                b.cyl((-0.15 + i * 0.1, -0.09 + j * 0.09, 0.262), 0.032, 0.05, caps[(i + j) % len(caps)], sides=8)
    b.M = keep


def sack(b, c, mat, yaw=0.0, tilt=0.0, size=(0.72, 0.45, 0.2)):
    """A filled sack: a pillow. `b` must be a buffer with a fat bevel (see sack_buf)."""
    b.box(c, size, mat, rot=R("Z", yaw) @ R("X", tilt))


def sack_buf(P):
    return P.buf("sacks", bevel=0.075, segs=3, smooth=60)


def shade_lamp(b, c, mat):
    """An enamel cone shade with a warm bulb under it; `c` is the top of the shade."""
    x, y, z = c
    b.lathe((x, y, z - 0.16), [(0.19, 0.0), (0.17, 0.02), (0.05, 0.13), (0.035, 0.17)], mat, sides=12, cap0=CREAM)
    b.sphere((x, y, z - 0.165), (0.07, 0.07, 0.06), LAMP, subdiv=1)


def corrugated(b, x0, x1, back, front, mat, pitch=0.12, thick=0.014, depth=0.016):
    """A sheet of tin roofing between a back edge and a front edge, (y, z) each, ridges along y."""
    n = max(2, int(round((x1 - x0) / pitch)) * 2)
    rows = []
    for (y, z) in (back, front):
        rows.append([(x0 + (x1 - x0) * j / n, y, z + (depth if j % 2 else 0.0)) for j in range(n + 1)])
    b.sheet(rows, thick, mat, normal=(0, 0, 1))


def stem_clump(P, wood, lv, base, height, radii, count, leaf_len, light=LL, dark=LD, lean=(0.0, 0.0)):
    """A little woody stem with one shingled clump on it, growing out of the soil at `base`."""
    x, y, z = base
    top = Vector((x + lean[0], y + lean[1], z + height))
    wood.tube([(x, y, z - 0.03), (x + lean[0] * 0.4, y + lean[1] * 0.4, z + height * 0.5), top], 0.014, WOODD, sides=5, r1=0.009)
    foliage(lv, top, radii, count, leaf_len, P.rng, floor_z=z + 0.01, light=light, dark=dark)


def snake_plant(P, lv, base, height=0.55, count=7):
    x, y, z = base
    for k in range(count):
        a = k / count * math.tau + P.rng.uniform(-0.2, 0.2)
        out = Vector((math.cos(a), math.sin(a), 0))
        tip = (UP + out * P.rng.uniform(0.12, 0.3)).normalized()
        lv.blade(Vector((x, y, z - 0.02)) + out * 0.03, tip, out, height * P.rng.uniform(0.7, 1.05), 0.085,
                 LL if k % 2 else LD, bend=0.02, segs=2)
        # A second card across the first, so the blade is never edge-on.
        lv.blade(Vector((x, y, z - 0.02)) + out * 0.03, tip, out.cross(UP), height * P.rng.uniform(0.6, 0.9), 0.07,
                 LD if k % 2 else LL, bend=0.02, segs=2)


def flower(b, c, mat=RED, r=0.05):
    b.sphere(c, (r, r, r * 0.55), mat, subdiv=1)
    b.sphere((c[0], c[1], c[2] + r * 0.4), r * 0.35, YEL, subdiv=1)


# ------------------------------------------------------------------ 1 the store

def sarisari_front(P):
    b = P.buf(bevel=0.022)
    f = P.buf("fine", bevel=0.008, segs=1)
    rng = P.rng
    # Base wall with a painted skirting, the counter, the posts, the head beam.
    b.box((0, -0.20, 0.46), (2.44, 0.44, 0.92), PLAS)
    b.box((0, -0.21, 0.16), (2.50, 0.50, 0.32), TEAL)
    b.box((0, -0.29, 0.945), (2.62, 0.68, 0.07), PLANK)
    for s in (-1, 1):
        b.box((s * 1.2, -0.21, 1.42), (0.13, 0.46, 0.94), WOOD)
        b.box((s * 0.8, 0.0, 2.2), (0.08, 0.06, 0.52), WOODD)          # the sign's two back posts
    b.box((0, -0.21, 1.90), (2.58, 0.52, 0.14), WOOD)
    b.box((0, 0.0, 1.42), (2.36, 0.06, 0.96), WOODD)                   # dark back of the shop
    # Two shelves of goods and a row of jars on the counter.
    palette = [RED, YEL, GREEN, PINK, WHITE, VIOLET, TEAL, CREAM]
    for zs in (1.30, 1.60):
        b.box((0, -0.12, zs), (2.30, 0.24, 0.04), PLANK)
        x = -1.06
        k = 0
        while x < 1.02:
            kind = rng.choice(("box", "box", "bottle", "jar", "stack"))
            top = zs + 0.012
            if kind == "box":
                w, h = rng.uniform(0.09, 0.15), rng.uniform(0.11, 0.2)
                f.box((x + w / 2, -0.13, top + h / 2), (w, 0.1, h), palette[(k * 3 + int(zs * 10)) % 8])
                f.box((x + w / 2, -0.184, top + h * 0.5), (w * 0.7, 0.012, h * 0.34), palette[(k * 3 + 3) % 8])
                x += w + 0.025
            elif kind == "stack":
                for j in range(3):
                    f.box((x + 0.06, -0.13, top + 0.03 + j * 0.056), (0.12, 0.11, 0.062), palette[(k + j * 2) % 8])
                x += 0.15
            elif kind == "bottle":
                for j in range(3):
                    f.cyl((x + 0.035 + j * 0.068, -0.12, top + 0.085), 0.03, 0.19, GREEN if k % 2 else RED, sides=8)
                    f.cyl((x + 0.035 + j * 0.068, -0.12, top + 0.2), 0.014, 0.07, YEL, sides=6)
                x += 0.235
            else:
                f.cyl((x + 0.06, -0.13, top + 0.08), 0.055, 0.18, CREAM, sides=10)
                f.cyl((x + 0.06, -0.13, top + 0.185), 0.06, 0.04, RED if k % 2 else TEAL, sides=10)
                x += 0.15
            k += 1
    for x, lid, fill in ((-0.95, RED, PINK), (-0.74, TEAL, YEL), (0.9, RED, WHITE)):
        f.cyl((x, -0.25, 1.075), 0.085, 0.21, fill, sides=12)
        f.cyl((x, -0.25, 1.195), 0.092, 0.05, lid, sides=12)
    # The grille: chunky bars, with a pass-through gap low in the middle.
    g = P.buf("grille", bevel=0.006, segs=1)
    g.box((0, -0.445, 1.805), (2.30, 0.05, 0.05), GREEN)
    g.box((0, -0.445, 1.005), (2.30, 0.05, 0.05), GREEN)
    for i in range(12):
        x = -1.1 + i * 0.2
        if abs(x) < 0.25:
            g.box((x, -0.445, 1.56), (0.032, 0.032, 0.48), GREEN)
        else:
            g.box((x, -0.445, 1.405), (0.032, 0.032, 0.78), GREEN)
    g.box((0, -0.445, 1.50), (2.26, 0.022, 0.03), GREEN)
    g.box((0, -0.445, 1.32), (0.64, 0.04, 0.04), GREEN)
    # Sachet strips hanging off the head beam, and the hand-written note.
    for x in (-1.0, -0.8, -0.6, 0.6, 0.8, 1.0):
        f.panel((x, -0.474, 1.60), 0.10, 0.50, 0.012, WHITE, DEC, region("sachets"), rot=R("Y", rng.uniform(-3, 3)))
    f.panel((0, -0.462, 1.62), 0.42, 0.14, 0.014, WHITE, DEC, region("tag"), rot=R("Y", -3))
    f.panel((0.72, -0.445, 0.62), 0.32, 0.48, 0.014, CREAM, DEC, region("poster"), rot=R("Y", 2))
    # Sign board, tilted a touch forward, and the tin awning under it on two braces.
    b.panel((0, -0.06, 2.22), 2.08, 0.52, 0.05, WOODD, S_SARI, rot=R("X", 4))
    t = P.buf("tin", bevel=0)
    corrugated(t, -1.42, 1.42, (-0.10, 1.985), (-0.80, 1.78), TIN)
    b.box((0, -0.77, 1.765), (2.86, 0.045, 0.045), WOOD)
    for s in (-1, 1):
        b.beam((s * 1.2, -0.42, 1.50), (s * 1.2, -0.77, 1.765), 0.045, WOOD)
    # A faded repaint patch on the base wall: large and flat.
    b.box((-0.55, -0.416, 0.62), (0.9, 0.02, 0.44), CREAM)


# ------------------------------------------------------------------ 2 the hoop

def basketball_hoop(P):
    b = P.buf(bevel=0.02)
    f = P.buf("fine", bevel=0.006, segs=1)
    tire(b, (0, 0, 0.1), 0.27, 0.12, band=CREAM)
    b.cyl((0, 0, 0.11), 0.2, 0.2, CONC, sides=12)
    b.M = R("X", -1.5)
    b.box((0, 0, 1.98), (0.11, 0.11, 3.86), WOOD)
    b.box((0, 0, 0.75), (0.125, 0.125, 0.5), TEAL)                       # an old paint band on the post
    b.M = Matrix()
    b.panel((0, -0.10, 3.47), 1.2, 0.9, 0.05, TAN, DEC, region("backboard"), rot=R("X", 2))
    for s in (-1, 1):
        b.beam((s * 0.42, -0.07, 3.75), (0, 0.06, 3.1), 0.05, WOOD)       # back braces
    b.box((0, -0.06, 3.3), (0.9, 0.06, 0.07), WOOD)
    # The bent rim, its bracket, and a chunky net.
    f.M = T((0, -0.13, 3.05)) @ R("X", -5) @ R("Y", 3)
    f.box((0, -0.03, -0.02), (0.14, 0.12, 0.05), RED)
    f.torus((0, -0.29, 0), 0.225, 0.016, RED, seg=16, sides=6)
    f.M = Matrix()
    n = P.buf("net", bevel=0)
    n.M = T((0, -0.42, 3.03)) @ R("X", -5) @ R("Y", 3)
    for k in range(8):
        a0, a1 = k / 8 * math.tau, (k + 0.5) / 8 * math.tau
        for (s0, s1) in ((a0, a1), (a1, a0 + math.tau / 8)):
            n.tube([(math.cos(s0) * 0.22, math.sin(s0) * 0.22, 0.0), (math.cos(s1) * 0.17, math.sin(s1) * 0.17, -0.2),
                    (math.cos(s0) * 0.13, math.sin(s0) * 0.13, -0.4)], 0.009, WHITE, sides=4)
    n.torus((0, 0, -0.2), 0.17, 0.008, WHITE, seg=12, sides=4)
    n.torus((0, 0, -0.4), 0.13, 0.009, WHITE, seg=12, sides=4)


# ------------------------------------------------------------------ 3 the posts

def street_head(b, at):
    x, y, z = at
    b.sphere((x, y, z), (0.13, 0.27, 0.075), DARK)
    b.sphere((x, y - 0.02, z - 0.035), (0.09, 0.19, 0.05), LAMP)


def poste(P):
    b = P.buf(bevel=0.02)
    f = P.buf("fine", bevel=0.008, segs=1)
    H = 7.0
    b.lathe((0, 0, 0), [(0.25, 0), (0.25, 0.42), (0.175, 0.5), (0.168, 1.3), (0.166, 1.46), (0.115, H)], WOODD,
            sides=12, mats=[CONC, CONC, CREAM, RED, WOODD])
    for z, w in ((6.55, 2.0), (5.95, 1.6)):
        b.box((0, -0.03, z), (w, 0.11, 0.13), WOOD)
        for s in (-1, 1):
            for k in (0.42, 0.9):
                f.cyl((s * w / 2 * k, -0.03, z + 0.11), 0.045, 0.12, CREAM, sides=8, r2=0.03)
        b.beam((0.0, -0.06, z - 0.5), (w * 0.36, -0.06, z - 0.03), 0.045, DARK)
        b.beam((0.0, -0.06, z - 0.5), (-w * 0.36, -0.06, z - 0.03), 0.045, DARK)
    # Transformer drum on its bracket.
    b.lathe((0.46, 0, 4.75), [(0.23, 0), (0.27, 0.05), (0.27, 0.2), (0.285, 0.22), (0.285, 0.28), (0.27, 0.3),
                              (0.27, 0.7), (0.22, 0.76)], STEEL, sides=14, mats=[STEEL, STEEL, DARK, DARK, DARK, STEEL, STEEL])
    b.box((0.2, 0, 5.1), (0.3, 0.1, 0.42), DARK)
    for s in (-1, 1):
        f.cyl((0.46 + s * 0.1, 0, 5.56), 0.04, 0.16, CREAM, sides=8, r2=0.025)
    # The meter board with three meters.
    b.box((0, -0.17, 2.7), (0.56, 0.06, 0.66), PLANK)
    for x, z in ((-0.14, 2.86), (0.14, 2.86), (0.0, 2.56)):
        f.box((x, -0.235, z), (0.2, 0.1, 0.2), STEEL)
        f.panel((x, -0.282, z), 0.16, 0.16, 0.014, STEEL, DEC, region("meter"))
    # Looped spare wire, a sagging run between the arms, and the service drop down the pole.
    w = P.buf("wires", bevel=0)
    w.M = T((0.26, -0.11, 6.02)) @ R("Y", 12)
    w.torus((0, 0, 0), 0.2, 0.022, RUB, axis="y", seg=14, sides=5)
    w.torus((0.02, -0.03, -0.03), 0.17, 0.02, RUB, axis="y", seg=14, sides=5)
    w.M = T((-0.3, -0.115, 5.5)) @ R("Y", -20)
    w.torus((0, 0, 0), 0.15, 0.02, RUB, axis="y", seg=12, sides=5)
    w.M = Matrix()
    w.tube(catmull([(-0.9, -0.03, 6.66), (-0.5, -0.12, 6.28), (0.0, -0.17, 6.2), (0.45, -0.1, 6.0), (0.72, -0.03, 6.04)], 4), 0.016, RUB, sides=5)
    w.tube(catmull([(0.84, -0.03, 6.66), (0.5, -0.14, 6.42), (0.1, -0.16, 6.5), (-0.4, -0.1, 6.1), (-0.72, -0.03, 6.04)], 4), 0.016, RUB, sides=5)
    w.tube(catmull([(-0.4, -0.08, 5.95), (-0.14, -0.15, 5.2), (-0.12, -0.16, 4.0), (-0.16, -0.19, 3.02)], 4), 0.016, RUB, sides=5)
    w.tube(catmull([(0.46, 0, 4.76), (0.3, -0.12, 4.4), (0.15, -0.17, 3.6), (0.16, -0.2, 3.02)], 4), 0.016, RUB, sides=5)
    # The street lamp arm.
    b.tube(catmull([(0, -0.08, 5.3), (0, -0.5, 5.62), (0, -1.15, 5.7)], 5), 0.035, STEEL, sides=6)
    street_head(b, (0, -1.3, 5.69))


def poste_short(P):
    b = P.buf(bevel=0.02)
    H = 5.0
    b.lathe((0, 0, 0), [(0.2, 0), (0.2, 0.5), (0.15, 0.56), (0.146, 1.2), (0.144, 1.38), (0.1, H)], CONC, sides=8,
            mats=[YEL, YEL, CONC, DARK, CONC])
    b.tube(catmull([(0, -0.06, 4.3), (0, -0.45, 4.72), (0, -1.0, 4.8)], 5), 0.035, STEEL, sides=6)
    b.beam((0, -0.08, 4.0), (0, -0.5, 4.7), 0.03, STEEL)
    street_head(b, (0, -1.15, 4.79))
    b.box((0, 0, 4.86), (0.26, 0.26, 0.08), DARK)                         # a cap band near the top
    b.panel((0, -0.165, 1.78), 0.3, 0.15, 0.02, DARK, DEC, region("plate"))


# ------------------------------------------------------------------ 4 and 5 hung lines

def banderitas(length):
    def build(P):
        sag = length * 0.065
        r = P.buf("rope", bevel=0)
        n = int(length * 3)
        r.tube([(-length / 2 + length * i / n, 0, sag_z(-length / 2 + length * i / n, length, sag)) for i in range(n + 1)],
               0.012, CREAM, sides=5)
        c = P.cloth("flags")
        cols = [RED, YEL, TEAL, PINK, WHITE, GREEN, VIOLET]
        count = int((length - 0.5) / 0.42)
        for k in range(count):
            x = -length / 2 + 0.25 + (length - 0.5) * (k + 0.5) / count
            z = sag_z(x, length, sag)
            slope = (sag_z(x + 0.05, length, sag) - sag_z(x - 0.05, length, sag)) / 0.1
            hw = 0.135
            sway = P.rng.uniform(-0.07, 0.07)
            tri = [Vector((x - hw, 0, z - hw * slope + 0.008)), Vector((x + hw, 0, z + hw * slope + 0.008)),
                   Vector((x + P.rng.uniform(-0.03, 0.03), sway, z - 0.36))]
            t = Vector((0, 0.004, 0))
            c.loft([[p - t for p in tri], [p + t for p in tri]], cols[(k * 3 + k // 7) % 7])
    return build


def hang(c, zl, x0, w, drop, mat, cols=6, rows=5, wave=0.035, y=0.0, reg=None, phase=0.0, top=0.012, pinch=0.0):
    grid = []
    for i in range(rows):
        t = i / (rows - 1)
        row = []
        for j in range(cols):
            s = j / (cols - 1)
            x = x0 + w * s + (0.5 - s) * w * pinch * t
            row.append((x, y + wave * t * math.sin(s * math.tau * 1.3 + phase), zl(x0 + w * s) + top - drop * t))
        grid.append(row)
    c.sheet(grid, 0.014, mat, reg=reg, decal=DEC)


def laundry_line_6m(P):
    L, sag = 6.0, 0.26
    zl = lambda x: sag_z(x, L, sag)
    r = P.buf("rope", bevel=0)
    r.tube([(-3 + 6 * i / 24, 0, zl(-3 + 6 * i / 24)) for i in range(25)], 0.009, CREAM, sides=5)
    c = P.cloth()
    f = P.buf("pegs", bevel=0.004, segs=1)
    pegs = []
    # A T-shirt hung by its shoulders: body and two sleeves.
    hang(c, zl, -2.62, 0.5, 0.62, lambda i, j: WHITE if i == 1 else RED, phase=0.4)
    hang(c, zl, -2.80, 0.22, 0.24, RED, cols=3, rows=3, y=0.006, phase=1.0, top=0.0)
    hang(c, zl, -2.16, 0.22, 0.24, RED, cols=3, rows=3, y=0.006, phase=2.0, top=0.0)
    pegs += [-2.56, -2.18]
    # Shorts: a waistband and two legs.
    hang(c, zl, -1.72, 0.46, 0.2, lambda i, j: WHITE if i == 0 else TEAL, rows=3, phase=2.2)
    hang(c, zl, -1.72, 0.215, 0.5, TEAL, cols=3, y=0.006, phase=0.7, top=0.0)
    hang(c, zl, -1.475, 0.215, 0.5, TEAL, cols=3, y=0.006, phase=2.9, top=0.0)
    pegs += [-1.66, -1.32]
    # A striped towel.
    hang(c, zl, -0.95, 0.62, 0.95, lambda i, j: WHITE if i in (1, 5) else PINK, rows=7, phase=1.3)
    pegs += [-0.89, -0.39]
    # The flowered blanket, the big piece.
    hang(c, zl, -0.1, 1.35, 1.05, PINK, cols=9, rows=6, wave=0.05, reg=region("wash"), phase=0.2)
    pegs += [-0.04, 0.58, 1.19]
    # A child's yellow shirt and a violet sando.
    hang(c, zl, 1.5, 0.36, 0.42, lambda i, j: GREEN if i == 2 else YEL, phase=2.0)
    hang(c, zl, 1.37, 0.16, 0.17, YEL, cols=3, rows=3, y=0.006, top=0.0)
    hang(c, zl, 1.83, 0.16, 0.17, YEL, cols=3, rows=3, y=0.006, top=0.0)
    pegs += [1.55, 1.81]
    hang(c, zl, 2.15, 0.4, 0.6, VIOLET, phase=0.9, pinch=-0.18)
    pegs += [2.2, 2.5]
    for x in pegs:
        f.box((x, -0.002, zl(x) + 0.004), (0.022, 0.04, 0.075), WOOD if int(x * 10) % 2 else YEL)


# ------------------------------------------------------------------ 6 water things

def water_drum(mat, lid, dip):
    def build(P):
        b = P.buf(bevel=0.012)
        drum(b, (0, 0, 0), mat, lid)
        tabo(b, (-0.04, -0.03, 0.918), dip, yaw=-30)
        b.box((0.0, -0.288, 0.47), (0.26, 0.03, 0.17), lid)               # a painted name patch
    return build


def pail(b, c, mat, handle=True):
    x, y, z = c
    b.lathe(c, [(0.13, 0), (0.175, 0.27), (0.19, 0.27), (0.19, 0.3), (0.16, 0.3), (0.125, 0.03)], mat, sides=12)
    if handle:
        b.tube([(x - 0.185, y, z + 0.27), (x - 0.19, y - 0.1, z + 0.14), (x - 0.06, y - 0.2, z + 0.06),
                (x + 0.06, y - 0.2, z + 0.06), (x + 0.19, y - 0.1, z + 0.14), (x + 0.185, y, z + 0.27)], 0.008, STEEL, sides=4)


def pail_stack(P):
    b = P.buf(bevel=0.008)
    for k, m in enumerate((RED, YEL, GREEN)):
        pail(b, (0, 0, k * 0.085), m, handle=(k == 2))
    pail(b, (0.42, -0.1, 0), TEAL)
    tabo(b, (0.42, -0.1, 0.03), PINK, yaw=40)
    b.sphere((0.36, -0.36, 0.035), (0.09, 0.06, 0.04), WHITE, subdiv=1)    # a bar of laundry soap


def batya(P):
    b = P.buf(bevel=0.012)
    b.lathe((0, 0, 0), [(0.36, 0), (0.48, 0.2), (0.5, 0.2), (0.5, 0.225), (0.46, 0.225), (0.35, 0.03)], STEEL, sides=18,
            mats=[STEEL, TEAL, TEAL, TEAL, STEEL])
    c = P.buf("clothes", bevel=0)
    for x, y, z, m, r in ((-0.15, 0.05, 0.14, RED, 0.17), (0.14, 0.12, 0.15, YEL, 0.15), (0.1, -0.13, 0.13, WHITE, 0.17),
                          (-0.17, -0.16, 0.12, VIOLET, 0.13), (0.0, 0.0, 0.2, PINK, 0.14)):
        c.sphere((x, y, z), (r, r * 0.85, r * 0.55), m, rot=R("Z", x * 300))
    for x, y in ((0.27, -0.2), (0.33, -0.1), (0.23, -0.27)):
        c.sphere((x, y, 0.17), 0.055, WHITE, subdiv=1)                    # suds
    # The washboard leaning on the rim, and the palo-palo paddle.
    b.M = T((-0.3, 0.12, 0.0)) @ R("Y", -24)
    b.box((0, 0, 0.3), (0.05, 0.3, 0.56), PLANK)
    b.M = Matrix()
    b.beam((0.5, -0.3, 0.035), (0.86, -0.02, 0.035), 0.07, WOOD, h=0.04)


def jerrycans(P):
    b = P.buf(bevel=0.035, segs=3)
    f = P.buf("fine", bevel=0.008, segs=1)
    for x, y, yaw, m, cap in ((-0.3, 0.02, 8, YEL, RED), (0.05, 0.0, -6, YEL, RED), (0.42, -0.06, 20, GREEN, CREAM)):
        for q in (b, f):
            q.M = T((x, y, 0)) @ R("Z", yaw)
        b.box((0, 0, 0.21), (0.3, 0.19, 0.42), m)
        f.box((0, 0, 0.445), (0.15, 0.05, 0.07), m)                       # the handle bridge
        f.cyl((0.105, 0, 0.445), 0.035, 0.06, cap, sides=8)
        f.box((0, -0.097, 0.2), (0.2, 0.012, 0.2), cap)                   # a pressed X panel, flat colour
        for q in (b, f):
            q.M = Matrix()


# ------------------------------------------------------------------ 7 plants

def plant_pots_a(P):
    b = P.buf(bevel=0.01)
    lv = P.leaves()
    s = pot(b, (0, 0, 0), 0.16, 0.22, 0.34, CLAY, rim=CLAY)
    stem_clump(P, b, lv, (0, 0, s), 0.3, (0.3, 0.3, 0.27), 80, 0.2, light=LY, dark=LD)
    s = paint_can(b, (0.5, -0.12, 0), 0.14, 0.3, WHITE, RED)
    snake_plant(P, lv, (0.5, -0.12, s), 0.62)
    s = paint_can(b, (-0.42, -0.2, 0), 0.1, 0.2, TEAL, YEL)
    stem_clump(P, b, lv, (-0.42, -0.2, s), 0.12, (0.17, 0.17, 0.15), 40, 0.14)
    b.cyl((0.2, -0.34, 0.012), 0.11, 0.03, CLAY, sides=10)                 # a spare saucer


def plant_pots_b(P):
    b = P.buf(bevel=0.012)
    lv = P.leaves()
    # Gumamela in a cut cooking-oil tin.
    b.box((0, 0, 0.02), (0.36, 0.36, 0.04), YEL)                           # hollow: floor, four walls, soil below the rim
    for s in (-1, 1):
        b.box((s * 0.168, 0, 0.19), (0.024, 0.36, 0.38), YEL)
        b.box((0, s * 0.168, 0.188), (0.33, 0.024, 0.376), YEL)
    b.box((0, 0, 0.2), (0.375, 0.375, 0.14), RED)
    b.box((0, 0, 0.19), (0.32, 0.32, 0.3), SOIL)
    stem_clump(P, b, lv, (0, 0, 0.34), 0.54, (0.38, 0.36, 0.34), 110, 0.2)
    fl = P.buf("flowers", bevel=0)
    for x, y, z in ((-0.26, -0.2, 0.98), (0.2, -0.3, 1.1), (0.3, 0.05, 0.86), (-0.05, -0.34, 0.8), (-0.1, 0.1, 1.24)):
        d = Vector((x, y, z - 0.88)).normalized()
        fl.M = T((x, y, z)) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4()
        flower(fl, (0, 0, 0.0), RED, 0.07)
    fl.M = Matrix()
    s = pot(b, (0.52, -0.14, 0), 0.11, 0.15, 0.24, CLAY)
    snake_plant(P, lv, (0.52, -0.14, s), 0.48, count=6)
    s = pot(b, (-0.46, -0.2, 0), 0.09, 0.13, 0.19, CLAY)
    stem_clump(P, b, lv, (-0.46, -0.2, s), 0.1, (0.17, 0.17, 0.14), 36, 0.13, light=LY)
    # A cut plastic bottle with a cutting in it.
    b.lathe((-0.2, -0.42, 0), [(0.05, 0), (0.055, 0.14), (0.045, 0.14), (0.04, 0.1)], GREEN, sides=8, cap1=SOIL)
    stem_clump(P, b, lv, (-0.2, -0.42, 0.1), 0.08, (0.1, 0.1, 0.09), 18, 0.1)


def plant_pots_c(P):
    """A plank on two hollow blocks with a row of small pots: the eskinita's garden."""
    b = P.buf(bevel=0.01, segs=1)
    lv = P.leaves()
    for s in (-1, 1):
        hollow_block(b, T((s * 0.55, 0, 0)) @ R("Z", 90))
    b.box((0, 0, 0.165), (1.6, 0.26, 0.045), PLANK)
    b.box((0.35, 0, 0.167), (0.5, 0.265, 0.046), TEAL)                    # a painted stretch of the plank
    top = 0.18
    s = paint_can(b, (-0.62, 0, top), 0.09, 0.19, RED, CREAM)
    stem_clump(P, b, lv, (-0.62, 0, s), 0.12, (0.17, 0.16, 0.15), 40, 0.13)
    s = pot(b, (-0.3, 0.01, top), 0.08, 0.11, 0.17, CLAY)
    snake_plant(P, lv, (-0.3, 0.01, s), 0.4, count=5)
    s = paint_can(b, (0.02, -0.01, top), 0.1, 0.2, YEL, GREEN)
    stem_clump(P, b, lv, (0.02, -0.01, s), 0.16, (0.2, 0.18, 0.18), 48, 0.14, light=LY)
    s = pot(b, (0.35, 0.0, top), 0.08, 0.11, 0.17, CLAY)
    stem_clump(P, b, lv, (0.35, 0, s), 0.1, (0.15, 0.15, 0.13), 30, 0.12)
    fl = P.buf("flowers", bevel=0)
    for x, y, z in ((0.3, -0.1, s + 0.2), (0.42, 0.03, s + 0.24), (0.33, 0.08, s + 0.16)):
        flower(fl, (x, y, z), PINK, 0.04)
    s = paint_can(b, (0.66, 0.0, top), 0.085, 0.16, WHITE, TEAL)
    snake_plant(P, lv, (0.66, 0, s), 0.34, count=5)


def plant_tall(P):
    """A young banana in a cut drum."""
    b = P.buf(bevel=0.012)
    lv = P.leaves()
    b.lathe((0, 0, 0), [(0.27, 0), (0.3, 0.04), (0.3, 0.2), (0.315, 0.22), (0.315, 0.27), (0.3, 0.29), (0.3, 0.46),
                        (0.315, 0.47), (0.315, 0.5), (0.28, 0.5), (0.27, 0.42)], TEAL, sides=14,
            mats=[TEAL] * 2 + [CREAM] * 3 + [TEAL] * 5, cap1=SOIL)
    b.lathe((0, 0, 0.4), [(0.15, 0), (0.125, 0.5), (0.1, 1.15), (0.065, 1.7)], GREEN, sides=10, mats=[TAN, GREEN, GREEN],
            squash=(1.0, 0.92))
    b.sphere((0.02, -0.11, 0.9), (0.07, 0.03, 0.2), TAN)                    # a dried sheath patch on the stem
    rng = P.rng
    for k in range(9):
        a = k / 9 * math.tau + rng.uniform(-0.15, 0.15)
        out = Vector((math.cos(a), math.sin(a), 0))
        rise = (1.3, 0.55, 0.9)[k % 3] * rng.uniform(0.85, 1.1)
        tip = (out + UP * rise).normalized()
        side_n = (UP - tip * UP.dot(tip)).normalized()
        base = Vector((0, 0, 2.0 + rng.uniform(-0.1, 0.05))) + out * 0.02
        lv.blade(base, tip, side_n, rng.uniform(1.35, 1.75), 0.8, LD if k % 3 == 1 else LL,
                 bend=rng.uniform(0.3, 0.55) / max(rise, 0.6), segs=5)
    lv.blade(Vector((0, 0, 2.0)), UP, Vector((0, -1, 0)), 1.1, 0.3, LL, bend=0.0, segs=2)
    lv.blade(Vector((0, 0, 2.0)), UP, Vector((1, 0, 0)), 1.0, 0.26, LD, bend=0.0, segs=2)
    # A sucker at the foot.
    for a in (0.6, 2.4, 4.4):
        out = Vector((math.cos(a), math.sin(a), 0))
        lv.blade(Vector((0.19, -0.1, 0.42)), (UP + out * 0.5).normalized(), out, 0.6, 0.24, LL, bend=0.25, segs=3)


# ------------------------------------------------------------------ 8 seats and the dama table

def monobloc(P):
    b = P.buf(bevel=0.018)
    m = WHITE
    b.box((0, 0, 0.43), (0.46, 0.44, 0.045), m)
    for sx in (-1, 1):
        b.beam((sx * 0.24, -0.23, 0.0), (sx * 0.19, -0.17, 0.42), 0.055, m)
        b.beam((sx * 0.24, 0.25, 0.0), (sx * 0.2, 0.2, 0.8), 0.055, m)
        b.beam((sx * 0.235, -0.2, 0.62), (sx * 0.22, 0.21, 0.64), 0.05, m, h=0.035)      # armrest
        b.beam((sx * 0.215, -0.19, 0.42), (sx * 0.235, -0.2, 0.62), 0.045, m)
    b.box((0, 0.215, 0.82), (0.47, 0.045, 0.1), m)
    for x in (-0.13, 0, 0.13):
        b.box((x, 0.21, 0.61), (0.075, 0.035, 0.36), m)
    b.box((0, 0.015, 0.447), (0.3, 0.3, 0.02), CREAM)                        # the seat's worn centre
    c = P.cloth()
    grid = []
    for (y, z) in ((0.165, 0.55), (0.175, 0.84), (0.215, 0.886), (0.255, 0.84), (0.262, 0.62)):
        grid.append([(x, y, z) for x in (-0.16, -0.05, 0.06, 0.17)])
    c.sheet(grid, 0.012, lambda i, j: TEAL if j == 1 else PINK, normal=(0, 0, -1))


def bench_wood(P):
    b = P.buf(bevel=0.015)
    for k, y in enumerate((-0.15, -0.05, 0.05, 0.15)):
        b.box((P.rng.uniform(-0.015, 0.015), y, 0.445 + (k % 2) * 0.004), (1.84, 0.092, 0.04), TEAL if k == 2 else PLANK)
    for s in (-1, 1):
        for y in (-0.15, 0.15):
            b.beam((s * 0.8, y * 1.15, 0.0), (s * 0.76, y, 0.43), 0.07, WOODD)
        b.box((s * 0.775, 0, 0.395), (0.07, 0.4, 0.07), WOODD)
        b.box((s * 0.79, 0, 0.14), (0.05, 0.33, 0.05), WOODD)
    b.box((0, 0, 0.145), (1.56, 0.05, 0.055), WOODD)
    # A pair of tsinelas kicked off under it.
    f = P.buf("fine", bevel=0.008, segs=1)
    for x, yaw, m in ((0.25, 15, RED), (0.42, -25, RED)):
        f.M = T((x, -0.33, 0)) @ R("Z", yaw)
        f.box((0, 0, 0.012), (0.1, 0.25, 0.024), m)
        f.tube([(-0.04, 0.0, 0.02), (0, -0.06, 0.05), (0.04, 0.0, 0.02)], 0.009, YEL, sides=4)
        f.M = Matrix()


def table_dama(P):
    b = P.buf(bevel=0.015)
    f = P.buf("fine", bevel=0.004, segs=1)
    b.box((0, 0, 0.42), (0.62, 0.62, 0.05), PLANK)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.beam((sx * 0.27, sy * 0.27, 0.0), (sx * 0.24, sy * 0.24, 0.41), 0.06, WOODD)
    b.box((0, 0, 0.3), (0.5, 0.5, 0.04), WOODD)
    b.panel((0, 0, 0.445), 0.5, 0.5, 0.016, WOODD, DEC, region("dama"), rot=R("X", -90))
    cell = 0.5 * (384 - 56) / 384 / 8
    for i, j, m in ((0, 1, RED), (2, 1, RED), (4, 1, RED), (1, 2, RED), (6, 1, RED), (3, 4, RED),
                    (1, 6, STEEL), (3, 6, STEEL), (5, 6, STEEL), (7, 6, STEEL), (2, 5, STEEL), (4, 3, STEEL)):
        f.cyl(((i - 3.5) * cell, (j - 3.5) * cell, 0.458), 0.02, 0.014, m, sides=8)
    # Two low bangkito for the players.
    for s, m in ((-1, TEAL), (1, PLANK)):
        b.box((s * 0.62, 0, 0.26), (0.3, 0.26, 0.04), m)
        for sy in (-1, 1):
            b.box((s * 0.62, sy * 0.1, 0.125), (0.26, 0.04, 0.25), WOODD)
        b.box((s * 0.62, 0, 0.12), (0.04, 0.2, 0.06), WOODD)


# ------------------------------------------------------------------ 9 roof things

def roof_tire(P):
    b = P.buf(bevel=0)
    tire(b, (0, 0, 0.086), 0.25, 0.105, band=CREAM)
    lv = P.leaves()
    for a, r in ((0.5, 0.05), (2.7, 0.1), (4.4, 0.03)):
        lv.leaf(Vector((math.cos(a) * r, math.sin(a) * r, 0.02 + r * 0.2)), Vector((math.cos(a + 1), math.sin(a + 1), 0.1)).normalized(),
                UP, 0.16, 0.1, LY)


def roof_blocks(P):
    b = P.buf(bevel=0.01)
    hollow_block(b, T((0, 0, 0)) @ R("Z", 6))
    hollow_block(b, T((0.36, -0.06, 0.0)) @ R("Z", -78))
    b.box((-0.05, -0.105, 0.085), (0.2, 0.012, 0.09), CREAM)                # a lime-wash splash


def water_tank(P):
    b = P.buf(bevel=0.015)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.beam((sx * 0.5, sy * 0.5, 0.0), (sx * 0.4, sy * 0.4, 0.92), 0.06, DARK)
        b.box((sx * 0.4, 0, 0.9), (0.06, 0.9, 0.06), DARK)
        b.box((0, sx * 0.4, 0.895), (0.9, 0.06, 0.06), DARK)
        b.beam((sx * 0.48, -0.48, 0.15), (sx * 0.42, 0.42, 0.8), 0.035, DARK)
        b.beam((-0.48, sx * 0.48, 0.15), (0.42, sx * 0.42, 0.8), 0.035, DARK)
    b.box((0, 0, 0.935), (0.86, 0.86, 0.04), PLANK)
    b.lathe((0, 0, 0.94), [(0.4, 0), (0.44, 0.04), (0.44, 0.22), (0.455, 0.24), (0.455, 0.3), (0.44, 0.32), (0.44, 0.5),
                           (0.455, 0.52), (0.455, 0.58), (0.44, 0.6), (0.44, 0.74), (0.3, 0.86), (0.16, 0.9),
                           (0.16, 0.94), (0.06, 0.96)], STEEL, sides=16,
            mats=[STEEL] * 2 + [TEAL] * 3 + [STEEL] * 2 + [TEAL] * 3 + [STEEL] * 2 + [TEAL] * 2)
    b.tube(catmull([(0.3, -0.36, 1.0), (0.42, -0.5, 0.86), (0.47, -0.52, 0.4), (0.5, -0.56, 0.02)], 4), 0.03, WHITE, sides=6)
    b.cyl((0.47, -0.52, 0.5), 0.045, 0.07, RED, sides=8)                    # the gate valve


def antenna(P):
    b = P.buf(bevel=0.006, segs=1)
    paint_can(b, (0, 0, 0), 0.13, 0.26, RED, CREAM, top=CONC, inner=0.03)
    b.cyl((0, 0, 1.25), 0.02, 2.5, DARK, sides=6)
    b.box((0, 0.0, 2.38), (0.03, 1.3, 0.03), STEEL)
    for k, y in enumerate((-0.6, -0.42, -0.24, -0.06, 0.12, 0.3)):
        b.box((0, y, 2.4), (0.5 + k * 0.1, 0.022, 0.022), STEEL)
    for s in (-1, 1):
        b.beam((0, 0.5, 2.38), (s * 0.5, 0.64, 2.38 + 0.02), 0.022, STEEL)   # the rear V reflector
        b.beam((0, 0.5, 2.38), (s * 0.5, 0.64, 2.38 - 0.25), 0.022, STEEL)
    b.box((0, 0.45, 2.38), (0.09, 0.12, 0.07), CREAM)
    w = P.buf("wire", bevel=0)
    w.tube(catmull([(0, 0.45, 2.36), (0.03, 0.2, 2.2), (0.03, 0.02, 1.6), (0.035, 0.0, 0.6), (0.1, -0.1, 0.27)], 4), 0.01, RUB, sides=4)
    b.torus((0, 0, 1.6), 0.03, 0.012, YEL, seg=8, sides=4)
    b.torus((0, 0, 0.6), 0.03, 0.012, YEL, seg=8, sides=4)


def satellite_dish(P):
    b = P.buf(bevel=0.01)
    b.box((0, 0.05, 0.02), (0.36, 0.36, 0.04), DARK)
    b.cyl((0, 0.05, 0.3), 0.03, 0.56, DARK, sides=8)
    b.box((0, 0.03, 0.56), (0.1, 0.14, 0.12), DARK)
    d = P.buf("dish", bevel=0.006, segs=1)
    d.M = T((0, -0.04, 0.62)) @ R("X", 62)
    prof = [(0.03, 0.0), (0.2, 0.03), (0.34, 0.085), (0.43, 0.15), (0.445, 0.15), (0.34, 0.07), (0.2, 0.012), (0.03, -0.02)]
    d.lathe((0, 0, 0), prof, CREAM, sides=16, mats=[CREAM, CREAM, CREAM, RED, DARK, DARK, DARK], squash=(1.0, 0.9))
    d.tube([(0, -0.38, 0.11), (0, -0.2, 0.36), (0, 0.0, 0.5)], 0.014, DARK, sides=5)
    d.tube([(0, 0.0, 0.0), (0, 0.0, 0.5)], 0.012, DARK, sides=5)
    d.cyl((0, 0.0, 0.5), 0.04, 0.1, CREAM, sides=8)
    d.cyl((0, 0.0, 0.44), 0.045, 0.03, RED, sides=8)
    d.M = Matrix()


# ------------------------------------------------------------------ 10 wall fittings

def ac_unit(P):
    b = P.buf(bevel=0.02)
    f = P.buf("fine", bevel=0.006, segs=1)
    W, H, D, z0 = 0.74, 0.47, 0.5, 0.2
    yc, zc = 0.02 - D / 2, z0 + H / 2
    b.box((0, yc, zc), (W, D, H), CREAM)
    b.box((0, 0.0, zc), (W + 0.05, 0.08, H + 0.05), DARK)                    # the frame at the wall
    f.box((0, yc - D / 2 + 0.008, zc), (W - 0.09, 0.04, H - 0.09), DARK)      # recessed front
    for k in range(5):
        f.box((0, yc - D / 2 - 0.004, z0 + 0.1 + k * 0.068), (W - 0.13, 0.035, 0.036), CREAM,
              rot=R("X", 25))
    for s in (-1, 1):
        for k in range(4):
            f.box((s * (W / 2 + 0.001), yc + 0.02, zc + 0.1 - k * 0.062), (0.014, D * 0.5, 0.028), DARK)
        b.beam((s * 0.3, 0.0, 0.0), (s * 0.3, yc - D / 2 + 0.06, z0 + 0.01), 0.035, DARK)   # support struts
    b.box((0, yc - 0.02, z0 + 0.012), (W - 0.1, D - 0.08, 0.03), DARK)
    f.box((W * 0.29, yc - D / 2 - 0.006, z0 + H - 0.05), (0.12, 0.02, 0.035), TEAL)
    b.box((-0.16, yc - 0.04, z0 + H + 0.002), (0.34, 0.3, 0.012), TAN)       # a sun-faded patch on top
    w = P.buf("drip", bevel=0)
    w.tube(catmull([(0.2, yc + 0.1, z0 + 0.02), (0.22, yc + 0.02, z0 - 0.06), (0.24, 0.0, 0.02)], 4), 0.012, WHITE, sides=5)


def meter_box(P):
    b = P.buf(bevel=0.012)
    f = P.buf("fine", bevel=0.006, segs=1)
    b.box((0, -0.01, 0.62), (0.56, 0.06, 0.66), PLANK)
    for x in (-0.13, 0.13):
        b.box((x, -0.085, 0.74), (0.21, 0.11, 0.22), STEEL)
        f.panel((x, -0.142, 0.74), 0.17, 0.17, 0.014, STEEL, DEC, region("meter"))
        f.cyl((x, -0.085, 0.6), 0.03, 0.1, DARK, sides=8)
    b.box((0, -0.075, 0.43), (0.22, 0.09, 0.17), DARK)
    f.box((0.03, -0.125, 0.44), (0.045, 0.03, 0.08), RED)
    f.box((0.0, -0.035, 0.9), (0.4, 0.015, 0.06), YEL)                       # a hand-painted number strip
    w = P.buf("pipes", bevel=0)
    w.tube([(-0.13, -0.05, 0.84), (-0.13, -0.05, 1.3)], 0.022, STEEL, sides=6)
    w.tube([(0.13, -0.05, 0.84), (0.13, -0.05, 1.16), (0.2, -0.03, 1.3)], 0.022, STEEL, sides=6)
    w.tube([(0, -0.05, 0.36), (0, -0.05, 0.0)], 0.022, STEEL, sides=6)
    w.tube(catmull([(-0.13, -0.06, 0.62), (-0.2, -0.1, 0.4), (-0.1, -0.09, 0.3), (-0.02, -0.075, 0.36)], 4), 0.012, RUB, sides=4)


def mailbox(P):
    b = P.buf(bevel=0.02)
    f = P.buf("fine", bevel=0.006, segs=1)
    b.box((0, -0.07, 0.36), (0.3, 0.16, 0.28), RED)
    b.cyl((0, -0.07, 0.5), 0.15, 0.154, RED, sides=14, rot=R("X", 90))
    f.box((0, -0.156, 0.42), (0.24, 0.02, 0.05), DARK)                       # the slot
    f.box((0, -0.158, 0.31), (0.22, 0.02, 0.12), CREAM)
    f.box((0.16, -0.07, 0.48), (0.02, 0.04, 0.14), YEL)                      # the little flag
    b.panel((0, -0.012, 0.1), 0.34, 0.17, 0.03, WOODD, DEC, region("plate"))


# ------------------------------------------------------------------ 11 the tricycle

def tricycle(P):
    """Adapted from tools/author_eskinita_tricycle.py: a motorcycle with an open right sidecar."""
    b = P.buf(bevel=0.03)
    f = P.buf("fine", bevel=0.01, segs=1)

    def wheel(x, y, r=0.28, w=0.075):
        b.torus((x, y, r), r - w, w, RUB, axis="x", seg=16, sides=7)
        f.cyl((x, y, r), r * 0.52, 0.11, STEEL, sides=10, rot=R("Y", 90))

    wheel(-0.53, -0.77)
    wheel(-0.53, 0.66)
    wheel(0.99, 0.48, 0.25, 0.07)
    b.box((-0.53, 0.16, 0.44), (0.22, 1.15, 0.14), DARK)
    b.box((-0.53, -0.03, 0.5), (0.32, 0.4, 0.3), STEEL)
    b.box((-0.53, -0.25, 0.83), (0.4, 0.46, 0.25), RED)
    b.box((-0.53, 0.27, 0.84), (0.39, 0.66, 0.12), RUB)
    b.box((-0.53, 0.66, 0.63), (0.28, 0.52, 0.12), RED)
    b.box((-0.53, -0.77, 0.62), (0.24, 0.5, 0.08), RED)
    for x in (-0.64, -0.42):
        f.beam((x, -0.77, 0.28), (x, -0.58, 1.1), 0.045, STEEL)
    f.beam((-0.89, -0.6, 1.12), (-0.18, -0.6, 1.12), 0.04, STEEL)
    for x in (-0.85, -0.22):
        f.box((x, -0.6, 1.12), (0.16, 0.065, 0.065), RUB)
    f.cyl((-0.53, -0.76, 0.99), 0.14, 0.13, STEEL, sides=10, rot=R("X", 90))
    f.cyl((-0.53, -0.83, 0.99), 0.105, 0.03, LAMP, sides=10, rot=R("X", 90))
    f.beam((-0.87, 0.03, 0.39), (0.23, 0.03, 0.39), 0.06, DARK)
    # Sidecar: floor, nose, rear body, bench, canopy.
    b.box((0.40, 0.03, 0.32), (0.95, 1.43, 0.12), DARK)
    b.box((0.40, -0.57, 0.58), (0.93, 0.26, 0.46), GREEN, rot=R("X", -8))
    b.box((0.40, -0.705, 0.56), (0.86, 0.03, 0.09), YEL)                    # the painted stripe
    b.box((0.40, 0.64, 0.67), (0.95, 0.16, 0.67), GREEN)
    b.box((0.40, 0.35, 0.58), (0.79, 0.43, 0.13), RUB)
    b.box((0.40, 0.57, 0.83), (0.79, 0.12, 0.43), RUB)
    b.box((-0.05, 0.16, 0.56), (0.09, 0.95, 0.38), GREEN)
    b.box((0.86, 0.36, 0.56), (0.06, 0.5, 0.38), GREEN)
    b.box((0.99, 0.48, 0.56), (0.22, 0.6, 0.06), GREEN)                     # mudguard slab
    f.box((0.94, -0.12, 0.28), (0.25, 0.47, 0.08), DARK)
    for x in (-0.06, 0.86):
        f.beam((x, -0.59, 0.79), (x, -0.4, 1.5), 0.055, STEEL)
        f.beam((x, 0.66, 0.95), (x, 0.66, 1.57), 0.055, STEEL)
    b.box((0.40, 0.05, 1.59), (1.12, 1.51, 0.12), CREAM)
    b.box((0.40, -0.69, 1.555), (1.14, 0.06, 0.2), YEL)                     # canopy name board, flat colour
    f.box((0.72, 0.725, 0.54), (0.12, 0.03, 0.08), RED)
    f.box((0.40, 0.725, 0.45), (0.28, 0.02, 0.12), WHITE)
    # The driver's shade off the rear chassis.
    f.beam((-0.86, 0.66, 0.59), (-0.2, 0.66, 0.59), 0.07, DARK)
    for x in (-0.85, -0.21):
        f.beam((x, 0.66, 0.59), (x, 0.52, 1.73), 0.047, STEEL)
    b.box((-0.53, 0.02, 1.77), (0.86, 1.34, 0.085), CREAM)
    f.beam((-0.2, 0.1, 1.6), (-0.11, 0.1, 1.74), 0.05, STEEL)               # the two roofs tied together


# ------------------------------------------------------------------ 12 loads

def kariton(P):
    b = P.buf(bevel=0.015)
    b.box((0, 0, 0.44), (0.78, 1.24, 0.05), PLANK)
    for s in (-1, 1):
        b.box((s * 0.375, 0, 0.6), (0.045, 1.24, 0.3), PLANK)
        b.box((s * 0.378, 0, 0.745), (0.06, 1.27, 0.055), TEAL)
        b.box((0, s * 0.6, 0.595), (0.74, 0.045, 0.29), PLANK)
        b.beam((s * 0.34, 0.5, 0.52), (s * 0.4, 1.3, 0.78), 0.05, WOODD)         # the push handles
        b.beam((s * 0.3, 0.52, 0.42), (s * 0.32, 0.6, 0.0), 0.055, WOODD)        # the rear rest legs
        tire(b, (s * 0.47, -0.12, 0.27), 0.185, 0.085, axis="x")
        b.cyl((s * 0.47, -0.12, 0.27), 0.11, 0.09, RED, sides=10, rot=R("Y", 90))
    b.cyl((0, -0.12, 0.27), 0.03, 0.96, DARK, sides=6, rot=R("Y", 90))
    b.box((0, -0.12, 0.36), (0.6, 0.08, 0.13), WOODD)
    b.box((0, 1.28, 0.775), (0.86, 0.05, 0.05), WOODD)
    c = sack_buf(P)
    sack(c, (-0.1, 0.22, 0.56), TAN, yaw=4, size=(0.66, 0.44, 0.2))
    sack(c, (-0.06, 0.1, 0.74), CREAM, yaw=14, tilt=6, size=(0.62, 0.42, 0.2))
    g = P.buf("fine", bevel=0.01, segs=1)
    g.box((0.12, -0.38, 0.6), (0.3, 0.24, 0.26), YEL)
    g.box((0.12, -0.38, 0.6), (0.31, 0.25, 0.09), RED)


def crates_stack(P):
    b = P.buf(bevel=0.012, segs=1)
    crate(b, (0, 0, 0), 4, RED)
    crate(b, (0.01, 0.01, 0.245), -6, RED)
    crate(b, (-0.01, 0.0, 0.49), 9, YEL, caps=[GREEN, DARK, GREEN, RED])


def sacks(P):
    c = sack_buf(P)
    sack(c, (-0.2, 0, 0.1), TAN, yaw=5)
    sack(c, (0.5, 0.05, 0.1), CREAM, yaw=-12)
    sack(c, (0.14, 0.02, 0.28), CREAM, yaw=20)
    sack(c, (0.1, -0.02, 0.46), TAN, yaw=-8)
    lean = T((1.0, 0.0, 0.0)) @ R("Z", -20) @ R("X", 12)
    c.M = lean
    c.box((0, 0, 0.36), (0.46, 0.2, 0.74), CREAM)
    c.M = Matrix()
    b = P.buf(bevel=0.004, segs=1)
    b.panel((0.1, -0.02, 0.556), 0.42, 0.28, 0.016, TAN, DEC, region("sack"), rot=R("Z", -8) @ R("X", -90))
    b.M = lean
    b.panel((0.0, -0.098, 0.38), 0.33, 0.22, 0.016, CREAM, DEC, region("sack"))
    b.M = Matrix()
    # Tied ears on the standing sack, and a scoop.
    b.M = lean
    for s_ in (-1, 1):
        b.box((s_ * 0.2, 0, 0.74), (0.07, 0.06, 0.1), CREAM, rot=R("Y", s_ * 25))
    b.M = Matrix()
    b.cyl((0.62, -0.42, 0.045), 0.07, 0.09, RED, sides=10)
    b.cyl((0.62, -0.42, 0.085), 0.058, 0.02, WHITE, sides=10)


def tires_stack(P):
    b = P.buf(bevel=0)
    tire(b, (0, 0, 0.086), 0.25, 0.105)
    tire(b, (0.03, 0.02, 0.25), 0.25, 0.105, band=CREAM)
    tire(b, (-0.02, -0.01, 0.415), 0.24, 0.1)
    b.M = T((0.5, -0.05, 0.0)) @ R("Y", -22)
    tire(b, (0.02, 0, 0.34), 0.25, 0.105, band=YEL, axis="x")
    b.M = Matrix()


# ------------------------------------------------------------------ 13 animals

def cat_bits(b, head, fur, ear_a, ear_b, sleepy=False):
    x, y, z = head
    b.sphere(head, (0.092, 0.082, 0.078), fur)
    for s, m in ((-1, ear_a), (1, ear_b)):
        b.cyl((x + s * 0.052, y + 0.005, z + 0.085), 0.036, 0.07, m, sides=10, r2=0.004, rot=R("Y", s * 14))
    b.sphere((x, y - 0.062, z - 0.02), (0.042, 0.03, 0.028), WHITE, subdiv=1)
    b.sphere((x, y - 0.09, z - 0.008), (0.011, 0.008, 0.008), PINK, subdiv=1)
    for s in (-1, 1):
        if sleepy:
            b.sphere((x + s * 0.038, y - 0.075, z + 0.012), (0.018, 0.006, 0.005), RUB, subdiv=1)
        else:
            b.sphere((x + s * 0.038, y - 0.072, z + 0.014), (0.014, 0.01, 0.017), RUB, subdiv=1)


def cat_sit(P):
    b = P.buf(bevel=0)
    b.sphere((0, 0.03, 0.17), (0.105, 0.12, 0.17), CREAM)
    b.sphere((0, 0.06, 0.09), (0.125, 0.125, 0.092), CREAM)
    for s in (-1, 1):
        b.cyl((s * 0.045, -0.075, 0.085), 0.032, 0.17, CREAM, sides=12, r2=0.04)
        b.sphere((s * 0.045, -0.085, 0.02), (0.034, 0.045, 0.022), WHITE, subdiv=1)
    cat_bits(b, (0, -0.045, 0.37), CREAM, TAN, DARK)
    b.sphere((0.05, 0.09, 0.25), (0.075, 0.07, 0.085), TAN)                  # calico patches
    b.sphere((-0.06, 0.02, 0.12), (0.075, 0.09, 0.07), DARK)
    b.sphere((-0.035, -0.025, 0.415), (0.052, 0.05, 0.042), TAN)
    b.tube(catmull([(0, 0.15, 0.035), (0.11, 0.17, 0.03), (0.17, 0.07, 0.03), (0.15, -0.06, 0.03), (0.09, -0.12, 0.035)], 4),
           0.028, TAN, sides=6, r1=0.02)


def cat_loaf(P):
    b = P.buf(bevel=0)
    b.sphere((0, 0.02, 0.1), (0.125, 0.2, 0.105), DARK)
    cat_bits(b, (0, -0.17, 0.175), DARK, DARK, DARK, sleepy=True)
    b.sphere((0, -0.14, 0.085), (0.075, 0.06, 0.07), WHITE)
    for s in (-1, 1):
        b.sphere((s * 0.05, -0.2, 0.025), (0.034, 0.04, 0.024), WHITE, subdiv=1)
    b.tube(catmull([(0.02, 0.2, 0.04), (0.12, 0.2, 0.035), (0.16, 0.08, 0.03), (0.15, -0.06, 0.03)], 4), 0.028, DARK, sides=6, r1=0.022)
    b.sphere((0.148, -0.075, 0.03), 0.026, WHITE, subdiv=1)                  # the white tail tip


def manok_cage(P):
    """A fighting cock's tin tepee stand and his feed tin. The cock himself is alive: see below."""
    t = P.buf("tin", bevel=0)
    for s in (-1, 1):
        t.M = T((0.3, 0.05, 0)) @ R("Z", 90 if s > 0 else -90)
        corrugated(t, -0.34, 0.34, (0.01, 0.66), (-0.4, 0.0), RUST if s > 0 else TIN, pitch=0.11)
    t.M = Matrix()
    b = P.buf(bevel=0.012)
    b.box((0.3, 0.05, 0.655), (0.07, 0.74, 0.06), WOOD)                     # the ridge pole
    b.beam((0.3, -0.25, 0.0), (0.3, -0.29, 0.66), 0.045, WOOD)
    b.beam((0.3, 0.35, 0.0), (0.3, 0.39, 0.66), 0.045, WOOD)
    b.cyl((0.3, 0.05, 0.3), 0.02, 0.62, WOODD, sides=6, rot=R("X", 90))       # the perch
    b.cyl((-0.1, -0.3, 0.04), 0.07, 0.08, RED, sides=10)                     # feed tin
    b.cyl((-0.1, -0.3, 0.075), 0.055, 0.02, YEL, sides=10)
    # ⚠️ NO BIRD IN THIS PROP (owner, 2026-10-09: "if you're gonna make chickens, make them live, make
    # them like the birds in lagoon cove"). The statue rooster and his tether left; the live birds are
    # models of their own (tools/author_eskinita_chickens.py -> Models/chicken_*.glb), placed and
    # brought to life by EskinitaAlleyLifeAuthor. The ridge pole is one of their perches: its top is
    # at (0.3, 0.05, 0.685) here, and the life author reads that point, so tell it if the pole moves.


# ------------------------------------------------------------------ 14 wall pieces

def street_lamp_wall(P):
    b = P.buf(bevel=0.012)
    b.box((0, -0.005, 0.2), (0.16, 0.05, 0.4), DARK)
    b.tube(catmull([(0, -0.02, 0.1), (0, -0.3, 0.42), (0, -0.62, 0.5)], 5), 0.022, DARK, sides=6)
    b.beam((0, -0.02, 0.32), (0, -0.3, 0.42), 0.02, DARK)
    shade_lamp(b, (0, -0.64, 0.51), GREEN)
    w = P.buf("wire", bevel=0)
    w.tube(catmull([(0.05, -0.02, 0.38), (0.12, -0.05, 0.2), (0.07, -0.02, 0.02)], 3), 0.008, RUB, sides=4)


def sign_brgy(P):
    b = P.buf(bevel=0.012)
    b.panel((0, -0.02, 0.4), 1.0, 0.75, 0.05, WOODD, DEC, region("brgy"), rot=R("Y", -1.5))
    for s in (-1, 1):
        b.box((s * 0.3, 0.0, 0.4), (0.07, 0.04, 0.8), WOOD)                 # battens behind, reaching the wall
    f = P.buf("fine", bevel=0.004, segs=1)
    for sx in (-1, 1):
        for sz in (-1, 1):
            f.cyl((sx * 0.43, -0.048, 0.4 + sz * 0.31), 0.016, 0.014, STEEL, sides=6, rot=R("X", 90))


def shrine(P):
    b = P.buf(bevel=0.02)
    f = P.buf("fine", bevel=0.005, segs=1)
    b.box((0, -0.01, 0.4), (0.62, 0.06, 0.66), CONC)
    b.cyl((0, -0.01, 0.72), 0.31, 0.054, CONC, sides=20, rot=R("X", 90))
    b.box((0, -0.14, 0.04), (0.72, 0.34, 0.08), CONC)
    for s in (-1, 1):
        b.box((s * 0.265, -0.13, 0.4), (0.08, 0.26, 0.66), CREAM)
    rings = []
    for k in range(11):
        a = math.pi * k / 10
        cx, cz = math.cos(a) * 0.265, 0.72 + math.sin(a) * 0.265
        ox, oz = math.cos(a) * 0.04, math.sin(a) * 0.04
        rings.append([(cx - ox, 0.0, cz - oz), (cx + ox, 0.0, cz + oz), (cx + ox, -0.27, cz + oz), (cx - ox, -0.27, cz - oz)])
    b.loft(rings, CREAM)
    b.box((0, -0.045, 0.4), (0.46, 0.02, 0.62), TEAL)
    b.cyl((0, -0.045, 0.72), 0.225, 0.016, TEAL, sides=18, rot=R("X", 90))
    b.box((0, -0.3, 0.085), (0.74, 0.03, 0.035), YEL)                        # a painted lip on the shelf
    # The figure: a robe, a veil, a head, a halo.
    f.lathe((0, -0.14, 0.075), [(0.085, 0), (0.07, 0.2), (0.04, 0.3)], WHITE, sides=10)
    f.sphere((0, -0.125, 0.385), (0.062, 0.058, 0.075), TEAL)
    f.sphere((0, -0.15, 0.385), (0.04, 0.04, 0.046), TAN, subdiv=1)
    f.lathe((0, -0.132, 0.2), [(0.088, 0), (0.07, 0.12), (0.062, 0.16)], TEAL, sides=10, squash=(1.0, 0.75))
    f.torus((0, -0.085, 0.4), 0.085, 0.01, YEL, axis="y", seg=14, sides=4)
    for s in (-1, 1):
        f.cyl((s * 0.16, -0.2, 0.13), 0.02, 0.12, RED, sides=8)
        f.sphere((s * 0.16, -0.2, 0.205), (0.013, 0.013, 0.026), LAMP, subdiv=1)
    for k in range(7):
        x = -0.12 + k * 0.04
        f.sphere((x, -0.26 + 0.02 * math.cos(k * 1.9), 0.095), 0.024, WHITE if k % 3 else YEL, subdiv=1)
    f.box((0, -0.13, 1.1), (0.045, 0.045, 0.2), YEL)
    f.box((0, -0.13, 1.12), (0.13, 0.04, 0.04), YEL)


# ------------------------------------------------------------------ 15 the arch

def gate_arch(P):
    b = P.buf(bevel=0.03)
    f = P.buf("fine", bevel=0.01, segs=1)
    for s in (-1, 1):
        x = s * 4.25
        b.box((x, 0, 2.4), (0.5, 0.5, 4.8), CONC)
        b.box((x, 0, 0.32), (0.66, 0.66, 0.64), GREEN)
        b.box((x, 0, 0.74), (0.56, 0.56, 0.12), YEL)
        b.box((x, 0, 2.9), (0.53, 0.53, 0.5), CREAM)                         # a painted band under the board
        b.box((x, 0, 4.82), (0.64, 0.64, 0.14), CREAM)
        b.cyl((x, 0, 4.96), 0.07, 0.2, DARK, sides=8)
        b.sphere((x, 0, 5.16), 0.17, LAMP)
        b.cyl((x, 0, 5.03), 0.13, 0.04, DARK, sides=10)
        f.panel((x, -0.252, 1.7), 0.34, 0.5, 0.02, CREAM, DEC, region("poster"), rot=R("Y", s * 2))
    # The painted steel board, readable from both sides, in a yellow frame.
    b.panel((0, -0.28, 4.01), 8.9, 1.1, 0.07, DARK, S_ARCH, back=S_ARCH)
    for z in (3.455, 4.565):
        b.box((0, -0.28, z), (9.0, 0.11, 0.07), YEL)
    for s in (-1, 1):
        b.box((s * 4.47, -0.28, 4.01), (0.07, 0.105, 1.16), YEL)
    # The arched crown: a pipe bow on struts, with a sampaguita medallion at the top.
    bow = [(x, -0.28, 4.62 + 0.95 * (1 - (x / 4.4) ** 2)) for x in [-4.4 + 8.8 * i / 22 for i in range(23)]]
    b.tube(bow, 0.05, YEL, sides=6)
    for x in (-3.3, -2.2, -1.1, 1.1, 2.2, 3.3):
        b.tube([(x, -0.28, 4.58), (x, -0.28, 4.62 + 0.95 * (1 - (x / 4.4) ** 2))], 0.03, GREEN, sides=5)
    f.M = T((0, -0.28, 5.72))
    f.cyl((0, 0, 0), 0.4, 0.08, GREEN, sides=16, rot=R("X", 90))
    for k in range(6):
        a = k / 6 * math.tau + 0.3
        f.sphere((math.cos(a) * 0.2, 0, math.sin(a) * 0.2), (0.13, 0.065, 0.13), WHITE)
    f.sphere((0, 0, 0), (0.1, 0.085, 0.1), YEL, subdiv=1)
    f.M = Matrix()
    b.box((0, -0.28, 5.42), (0.12, 0.09, 0.4), GREEN)                        # the medallion's stalk on the bow


# ------------------------------------------------------------------ the kit

# name, builder, anchor, note
PROPS = [
    ("sarisari_front", sarisari_front, "wall", "Store front against a wall: back on y=0, counter and awning reach -0.8. Sign reads ALING NENA'S SARI-SARI STORE."),
    ("basketball_hoop", basketball_hoop, "ground", "Home-made hoop on a post in a tire of concrete; rim at 3.05 m, backboard faces -Y."),
    ("poste", poste, "ground", "7 m electric post: crossarms along X, transformer, meters, looped wires, lamp arm reaching -Y."),
    ("poste_short", poste_short, "ground", "5 m concrete lamp post, lamp arm reaching -Y."),
    ("banderitas_8m", banderitas(8.0), "hang", "Fiesta bunting. End points exactly at x=-4 and x=+4, z=0; sags 0.52 m, flags hang 0.36 below the line."),
    ("banderitas_17m", banderitas(17.0), "hang", "Fiesta bunting. End points exactly at x=-8.5 and x=+8.5, z=0; sags 1.1 m, flags hang 0.36 below the line."),
    ("laundry_line_6m", laundry_line_6m, "hang", "Clothesline. End points at x=-3 and x=+3, z=0; sags 0.26 m, the blanket hangs 1.3 m below the ends."),
    ("water_drum", water_drum(TEAL, CREAM, RED), "ground", "Plastic drum, teal-green, with a lid and a tabo."),
    ("water_drum_yellow", water_drum(YEL, GREEN, PINK), "ground", "Plastic drum, sun yellow."),
    ("pail_stack", pail_stack, "ground", "Three nested pails, a loose pail with a tabo, a bar of soap."),
    ("batya", batya, "ground", "Laundry basin with clothes, suds, a washboard and a paddle."),
    ("jerrycans", jerrycans, "ground", "Three water containers."),
    ("plant_pots_a", plant_pots_a, "ground", "Croton in clay, snake plant in a paint can, a small can."),
    ("plant_pots_b", plant_pots_b, "ground", "Gumamela in a cooking-oil tin, snake plant, small pots, a cut bottle."),
    ("plant_pots_c", plant_pots_c, "ground", "A plank on two hollow blocks with five small pots; long axis X."),
    ("plant_tall", plant_tall, "ground", "A young banana in a cut drum, about 3 m."),
    ("monobloc", monobloc, "ground", "Plastic chair with a towel over the back; sits facing -Y."),
    ("bench_wood", bench_wood, "ground", "Long papag bench, long axis X, with a pair of slippers in front."),
    ("table_dama", table_dama, "ground", "Low table with a painted dama board and bottle-cap pieces, a stool each side along X."),
    ("roof_tire", roof_tire, "ground", "Roof weight: an old tire lying flat. Put z=0 on the roof sheet."),
    ("roof_blocks", roof_blocks, "ground", "Roof weight: two hollow blocks."),
    ("water_tank", water_tank, "ground", "Rooftop tank on a steel stand, feet 1 m apart."),
    ("antenna", antenna, "ground", "Old TV antenna on a 2.5 m pole set in a tin of concrete; points -Y."),
    ("satellite_dish", satellite_dish, "ground", "Small dish on a base plate; looks up towards -Y."),
    ("ac_unit", ac_unit, "wall", "Window air-conditioner: back on y=0, body from z=0.2 on two struts down to z=0."),
    ("meter_box", meter_box, "wall", "Meter board with two meters, a breaker and conduits: back on y=0."),
    ("mailbox", mailbox, "wall", "Mailbox with a house-number plate under it: back on y=0."),
    ("tricycle", tricycle, "ground", "Passenger tricycle, front towards -Y, sidecar on +X."),
    ("kariton", kariton, "ground", "Wooden pushcart with sacks; handles towards +Y."),
    ("crates_stack", crates_stack, "ground", "Three softdrink crates."),
    ("sacks", sacks, "ground", "Rice sacks: four stacked, one standing."),
    ("tires_stack", tires_stack, "ground", "Three stacked tires and one leaning."),
    ("cat_sit", cat_sit, "ground", "A calico cat sitting, facing -Y."),
    ("cat_loaf", cat_loaf, "ground", "A dark cat loafing asleep, facing -Y."),
    ("manok_cage", manok_cage, "ground", "A fighting cock's tin tepee stand and feed tin (no bird: the live chickens are chicken_*.glb)."),
    ("street_lamp_wall", street_lamp_wall, "wall", "Wall lamp: plate on y=0, shade 0.64 m out."),
    ("sign_brgy", sign_brgy, "wall", "Hand-painted reminder board (PAALALA: BAWAL MAGKALAT SA ESKINITA): back on y=0."),
    ("shrine", shrine, "wall", "Wall grotto niche with a simple figure, candles and flowers: back on y=0."),
    ("gate_arch", gate_arch, "ground", "Barangay arch: posts centred at x=-4.25 and +4.25 (8 m clear), 3.42 m clear height, board reads MABUHAY! ESKINITA SAMPAGUITA on both sides, a globe lamp on each post."),
]


# ------------------------------------------------------------------ output

def join_for_export(col):
    """A copy of the prop with every modifier applied and all its objects joined into one mesh."""
    dg = bpy.context.evaluated_depsgraph_get()
    scene = bpy.context.scene
    tmp = []
    for o in col.objects:
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
        t = bpy.data.objects.new("tmp", me)
        scene.collection.objects.link(t)
        tmp.append(t)
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for t in tmp:
        t.select_set(True)
    bpy.context.view_layer.objects.active = tmp[0]
    if len(tmp) > 1:
        with bpy.context.temp_override(active_object=tmp[0], selected_objects=tmp, selected_editable_objects=tmp):
            bpy.ops.object.join()
    obj = tmp[0]
    obj.name = obj.data.name = col.name
    return obj


def export(col):
    obj = join_for_export(col)
    me = obj.data
    me.calc_loop_triangles()
    tris = len(me.loop_triangles)
    xs = [v.co for v in me.vertices]
    lo = Vector((min(v.x for v in xs), min(v.y for v in xs), min(v.z for v in xs)))
    hi = Vector((max(v.x for v in xs), max(v.y for v in xs), max(v.z for v in xs)))
    path = MODELS / f"{col.name}.glb"
    for o in bpy.context.view_layer.objects:
        o.select_set(o is obj)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.gltf(filepath=str(path), export_format="GLB", use_selection=True, export_yup=True,
                              export_apply=False, export_animations=False, export_image_format="NONE",
                              export_materials="EXPORT")
    used = [m.name for m in me.materials if m]
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(me)
    return tris, lo, hi, path, used


def setup_light():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("eskinita_sky")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.9, 0.84, 0.78, 1)
    bg.inputs["Strength"].default_value = 0.85
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 3.0, math.radians(4), (1.0, 0.9, 0.76)
    sun.rotation_euler = (Vector((0, 0, 0)) - Vector((-6, -9, 8))).to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "Standard"


def review(version, records):
    """Tiles of every prop composed into a labelled sheet, and three closer group renders."""
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    LOGS.mkdir(parents=True, exist_ok=True)
    TILES.mkdir(parents=True, exist_ok=True)
    rev = bpy.data.collections.new("review")
    scene.collection.children.link(rev)
    M("rev_ground", "", "cfc8bc")
    M("rev_wall", "plaster", "f2e2c6", 0.333, "e8cfa4")
    g = B("review_ground", bevel=0)
    g.box((0, 0, -0.5), (80, 80, 0.996), "rev_ground")
    ground = g.finish(rev)
    w = B("review_wall", bevel=0)
    w.box((0, 2.02, 4), (40, 4, 12), "rev_wall")
    wall = w.finish(rev)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cols = {name: bpy.data.collections[f"prop_{name}"] for name, *_ in PROPS}

    def show(names):
        for n, c in cols.items():
            c.hide_render = n not in names

    # ---- tiles
    scene.render.resolution_x, scene.render.resolution_y = 520, 440
    cam.data.type = "ORTHO"
    tiles = []
    for name, _fn, anchor, _note in PROPS:
        rec = records[name]
        show({name})
        lift = Vector((0, 0, 3.0)) if anchor == "hang" else Vector((0, 0, 1.2 if anchor == "wall" and name != "sarisari_front" else 0))
        for o in cols[name].objects:
            o.location = lift
        ground.hide_render = anchor == "hang"
        wall.hide_render = anchor != "wall"
        lo, hi = rec["lo"] + lift, rec["hi"] + lift
        d = Vector((-0.5, -1.0, 0.42)).normalized()
        if anchor == "hang":
            d = Vector((-0.12, -1.0, 0.1)).normalized()
        quat = d.to_track_quat("Z", "Y")
        right, upv = quat @ Vector((1, 0, 0)), quat @ Vector((0, 1, 0))
        corners = [Vector((x, y, z)) for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z, hi.z)]
        us, vs = [c.dot(right) for c in corners], [c.dot(upv) for c in corners]
        centre = right * ((min(us) + max(us)) / 2) + upv * ((min(vs) + max(vs)) / 2) + d * ((lo + hi) / 2).dot(d)
        cam.location = centre + d * 60
        cam.rotation_euler = quat.to_euler()
        cam.data.ortho_scale = max(max(us) - min(us), (max(vs) - min(vs)) * 520 / 440) * 1.14
        cam.data.clip_end = 200
        path = TILES / f"{name}.png"
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        for o in cols[name].objects:
            o.location = (0, 0, 0)
        s = rec["size"]
        tiles.append({"file": str(path), "label": name, "sub": f"{s[0]:.2f} x {s[1]:.2f} x {s[2]:.2f} m   {rec['tris']} tris"})
    spec = TILES / "sheet.json"
    spec.write_text(json.dumps({"title": f"Eskinita Alley props v{version}", "cols": 6, "tiles": tiles}))
    subprocess.run(["py", "-3", str(PAINTER), "--compose", str(spec), str(LOGS / f"props_sheet_v{version}.png")], check=False)

    # ---- groups
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1000
    cam.data.type = "PERSP"
    groups = {
        "tindahan": (True, (0.6, -8.6, 2.5), (0.2, 0, 1.75), 26, {
            "sarisari_front": (0, 0, 0, 0), "bench_wood": (2.75, -0.45, 0, 0), "monobloc": (-2.2, -1.2, 0, 35),
            "table_dama": (-3.4, -1.9, 0, 15), "cat_sit": (1.75, -1.5, 0, 20), "cat_loaf": (3.0, -0.45, 0.46, -15),
            "water_drum": (-1.75, -0.42, 0, 0), "water_drum_yellow": (-4.9, -0.45, 0, 0), "pail_stack": (-4.2, -1.1, 0, 0),
            "plant_pots_a": (4.6, -0.5, 0, 0), "plant_pots_b": (-6.0, -0.7, 0, 0), "ac_unit": (3.0, 0, 2.5, 0),
            "meter_box": (1.75, 0, 1.2, 0), "mailbox": (4.2, 0, 1.2, 0), "sign_brgy": (-3.1, 0, 1.5, 0),
            "shrine": (-4.75, 0, 1.25, 0), "street_lamp_wall": (-1.7, 0, 2.9, 0), "laundry_line_6m": (3.6, -1.0, 4.3, 0),
            "poste": (6.6, -0.5, 0, 0), "jerrycans": (5.6, -1.2, 0, 0)}),
        "laro": (False, (0.4, -10.5, 2.3), (0, 0, 1.9), 28, {
            "basketball_hoop": (0, 0.6, 0, 0), "kariton": (-2.6, -0.4, 0, 205), "crates_stack": (2.0, 0.2, 0, 0),
            "sacks": (3.3, -0.3, 0, 0), "tires_stack": (-4.4, 0.4, 0, 0), "manok_cage": (1.5, -2.2, 0, 0),
            "batya": (-1.1, -2.4, 0, 0), "plant_tall": (5.0, 0.6, 0, 0), "plant_pots_c": (4.6, -1.8, 0, 0),
            "tricycle": (-6.4, -0.9, 0, -30), "banderitas_8m": (0, 1.2, 4.9, 0), "poste_short": (-4.0, 1.4, 0, 0)}),
        "arko": (False, (1.5, -17.0, 2.6), (0, 0, 2.9), 30, {
            "gate_arch": (0, 0, 0, 0), "banderitas_17m": (0, 3.0, 7.4, 0), "water_tank": (-3.0, -4.0, 0, 0),
            "antenna": (-1.3, -4.4, 0, 0), "satellite_dish": (0.2, -4.6, 0, 0), "roof_tire": (1.5, -4.8, 0, 0),
            "roof_blocks": (2.7, -4.6, 0, 0), "poste": (-6.4, 1.0, 0, 0), "poste_short": (6.4, 1.0, 0, 180)}),
    }
    for label, (use_wall, pos, tgt, lens, placed) in groups.items():
        show(set(placed))
        ground.hide_render, wall.hide_render = False, not use_wall
        for n, (x, y, z, yaw) in placed.items():
            for o in cols[n].objects:
                o.matrix_world = T((x, y, z)) @ R("Z", yaw)
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(LOGS / f"props_group_{label}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[eskinita props] render", scene.render.filepath)
        for n in placed:
            for o in cols[n].objects:
                o.matrix_world = Matrix()
    show(set(cols))


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--sheet") + 1]) if "--sheet" in argv else None
    for d in (MODELS, TEX, SOURCE, LOGS):
        d.mkdir(parents=True, exist_ok=True)
    if not all((TEX / f"{n}_albedo.png").exists() for n in ("prop_sign_sarisari", "prop_sign_arch", "prop_decals")):
        subprocess.run(["py", "-3", str(PAINTER)], check=False)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for name, fn, _anchor, _note in PROPS:
        P = Prop(name)
        fn(P)
        P.finish()
    setup_light()
    bpy.context.preferences.filepaths.save_version = 0      # no props.blend1 beside the file
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "props.blend"))

    records, used_all, manifest = {}, [], []
    missing = sorted({s["texture"] for s in MATS.values() if s["texture"] and not (TEX / f"{s['texture']}_albedo.png").exists()})
    for name, _fn, anchor, note in PROPS:
        tris, lo, hi, path, used = export(bpy.data.collections[f"prop_{name}"])
        size = [round(hi.x - lo.x, 3), round(hi.y - lo.y, 3), round(hi.z - lo.z, 3)]
        ok = path.exists() and path.stat().st_size > 0
        records[name] = dict(tris=tris, lo=lo, hi=hi, size=size)
        used_all += [u for u in used if u not in used_all]
        manifest.append({"name": f"prop_{name}", "size": size, "note": note, "anchor": anchor,
                         "min": [round(c, 3) for c in lo], "max": [round(c, 3) for c in hi], "triangles": tris})
        print(f"[eskinita props] {'OK ' if ok else 'MISSING'} prop_{name:20s} {tris:6d} tris  "
              f"{size[0]:.2f} x {size[1]:.2f} x {size[2]:.2f}  {path.stat().st_size if ok else 0} bytes")
    mats = []
    for name in sorted(used_all):
        s = MATS[name]
        mats.append({"name": name, "texture": s["texture"], "tint": [round(c, 4) for c in srgb(s["tint"])],
                     "tiling": s["tiling"], **{k: bool(s["flags"].get(k)) for k in FLAGS}})
    (ART / "materials_props.json").write_text(json.dumps(
        {"tint_space": "sRGB 0..1, multiplies the texture", "materials": mats}, indent=1))
    (ART / "props_manifest.json").write_text(json.dumps(
        {"axes": "Blender metres: size = [x width, y depth, z height]; front faces -Y; in Unity (glTFast) a Blender point (x, y, z) lands at (-x, z, -y)",
         "anchors": {"ground": "stands on z=0", "wall": "back sits 2 cm behind y=0: put y=0 on the wall face",
                     "hang": "end points at x=-L/2 and +L/2, z=0; hangs below"},
         "props": manifest}, indent=1))
    print(f"[eskinita props] {len(PROPS)} props, {len(mats)} materials, {sum(r['tris'] for r in records.values())} triangles")
    if missing:
        print("[eskinita props] textures not on disk yet (flat tint used in renders):", ", ".join(missing))
    if version is not None:
        review(version, records)


main()
