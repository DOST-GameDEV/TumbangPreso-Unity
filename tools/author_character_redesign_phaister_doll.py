"""Build the Voodoo Doll redesign PROTOTYPE: Phaister's doll drawn as a member of the redesigned cast.

    py -3 tools/author_character_redesign_phaister_doll_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_phaister_doll.py

Writes, all under ArtSource/phaister/doll-redesign-20261031/ (nothing under Assets, nothing the game loads):
    phaister-doll-redesign.glb, phaister-doll-redesign.blend, beside the atlas the .glb names.

WHY. The v11 doll (tools/build_phaister_doll_voxel.py) wears a basket-weave burlap that reads noisy and realistic
beside Phaister and Paete. Owner: "make it feel like its part of teh game and not ultra realistic". So: flat tones
per form, set by hand; cloth told by a few big seams, stitches and patches, each a real piece; rounded lofted forms.

THE RIG IS v11's. The .glb is phaister-doll.glb with its four meshes replaced and two elbow bones appended after the
seven; every clip, both soul nodes and the file's internal name (`phaister-doll`, which `GaitStyles.For` keys) are
copied across. Mittens ride the forearm bones, because `CharacterVisual.HandBone` measures the palm there.

THE LIGHT, in v11's format, because `SoulGlow` and `SoulSpill` read it:
  glow-mesh  TEXCOORD0.x = 1 for a MOUTH (a flat polygon lying on the cloth, which the shader cuts through),
             TEXCOORD0.x = 0 for the INSIDE (walls and floor sunk into the doll), COLOR_0 graded dim to hot.
  spill-mesh additive, COLOR_0 is colour and strength; TEXCOORD0 = (flame phase, how far up the flame).

Blender axes: +x the doll's LEFT, its face looks along -y, z up, feet on zero, rig units.
"""
import math
import os
import struct
import sys
import time

import bpy
import bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree

TOOLS = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(TOOLS)
sys.path.insert(0, TOOLS)
import build_person_voxel as bpv  # noqa: E402  the cast's glb reader and writer, not edited
import author_character_redesign_phaister_doll_textures as tex  # noqa: E402

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/phaister-doll.glb")
FOLDER = os.path.join(ROOT, "ArtSource/phaister/doll-redesign-20261031")
OUT = os.path.join(FOLDER, "phaister-doll-redesign.glb")
BLEND = os.path.join(FOLDER, "phaister-doll-redesign.blend")
INTERNAL_NAME = "phaister-doll"
NAME = "phaister-doll-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
ELBOW_X = 0.350
ARM_Z = 0.400
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [n for n, _, _ in EXTRA_BONES]
TRIANGLE_BUDGET = 8000
SHARP_ANGLE = 45.0
FACE, BACK, LEFT, RIGHT, TOP = "front", "back", "left", "right", "top"
X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))

# ---------------------------------------------------------------------------------------------------------------------
# PIECES. A mapping says which flat tone a face takes.
# ---------------------------------------------------------------------------------------------------------------------


def flat(name):
    return ("flat", name)


def tones(lit, base, shade, up=0.5, down=-0.35):
    """By which way a face points: up takes the light step, down the shade step."""
    return ("tones", lit, base, shade, up, down)


def superellipse(cx, cy, z, hx, yf, yb, p, n, start=0.0):
    """A horizontal ring: half width hx, reaching yf toward the face (-y) and yb behind, squareness p."""
    pts = []
    for k in range(n):
        t = start + 2.0 * math.pi * k / n
        c, s = math.cos(t), math.sin(t)
        x = hx * math.copysign(abs(c) ** (2.0 / p), c)
        y = math.copysign(abs(s) ** (2.0 / p), s)
        y *= yb if y > 0 else yf
        pts.append(Vector((cx + x, cy + y, z)))
    return pts


def arm_ring(x, y0, z0, ry, rz, p, n):
    """A ring round the arm, which lies along x in the bind pose."""
    pts = []
    for k in range(n):
        t = 2.0 * math.pi * k / n
        c, s = math.cos(t), math.sin(t)
        pts.append(Vector((x, y0 + ry * math.copysign(abs(c) ** (2.0 / p), c), z0 + rz * math.copysign(abs(s) ** (2.0 / p), s))))
    return pts


def weave_coords(rings, weave):
    """u is fixed per column, measured round the reference (widest) ring and snapped to a whole number of weave periods, so a
    strand runs straight along its column and the weave closes with no seam. v runs along each column from that ring, so a
    strand stays one size over the belly's bulge."""
    j0, ref, axis = weave
    n = len(rings[0])
    ring = rings[ref]
    lengths = [(ring[(j + 1) % n] - ring[j]).length for j in range(n)]
    total = sum(lengths)
    periods = max(2, round(total / tex.WEAVE_PERIOD))
    snapped = periods * tex.WEAVE_PERIOD
    row = [0.0] * n
    arc = 0.0
    for m in range(n):
        j = (j0 + m) % n
        row[j] = arc * snapped / total
        arc += lengths[j]
    U = [row for _ in rings]
    perim = [snapped for _ in rings]
    base = rings[ref][j0].z if axis == "z" else abs(rings[ref][j0].x)
    W = [[0.0] * n for _ in rings]
    W[ref] = [base] * n
    for i in range(ref + 1, len(rings)):
        W[i] = [W[i - 1][j] + (rings[i][j] - rings[i - 1][j]).length for j in range(n)]
    for i in range(ref - 1, -1, -1):
        W[i] = [W[i + 1][j] - (rings[i][j] - rings[i + 1][j]).length for j in range(n)]
    return U, W, perim


class Part:
    """One of the two cloth meshes the rig carries."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.bone = self.bm.verts.layers.int.new("bone")
        self.jobs = []
        self.pieces = []
        self.blend = {}
        self.forms = {}
        self.weave_uv = {}   # face -> {vertex: (u, v)} in rig units, for faces that may wear the weave

    def vert(self, p, bone):
        v = self.bm.verts.new(p)
        v[self.bone] = ALL_BONES.index(bone)
        return v

    def done(self, name, faces):
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self.pieces.append((name, sum(len(f.verts) - 2 for f in faces)))
        return faces

    def loft(self, name, bone, rings, mapping, caps=(False, False), tip=None, form=None, closed=True, weave=None):
        """Skin rings of equal size. `mapping` is one spec or f(segment, column) -> spec. A cap is False, True (a fan)
        or a point to fan to. `weave` = (front column, reference ring, "z" or "x"): lay the weave on this loft by true length."""
        n = len(rings[0])
        rows = [[self.vert(p, bone) for p in r] for r in rings]
        U, W, perim = weave_coords(rings, weave) if weave else (None, None, None)

        def weave_face(f, i, j):
            if not weave:
                return
            k = (j + 1) % n
            d = {}
            for r in (i, i + 1):
                uj, uk = U[r][j], U[r][k]
                if uk < uj:
                    uk += perim[r]      # the one column that crosses the back seam
                d[rows[r][j]] = (uj, W[r][j])
                d[rows[r][k]] = (uk, W[r][k])
            self.weave_uv[f] = d
        faces = []
        spec = mapping if callable(mapping) else (lambda i, j: mapping)
        for i in range(len(rows) - 1):
            for j in range(n if closed else n - 1):
                k = (j + 1) % n
                f = self.bm.faces.new((rows[i][j], rows[i][k], rows[i + 1][k], rows[i + 1][j]))
                self.jobs.append((f, spec(i, j)))
                faces.append(f)
                weave_face(f, i, j)

        def fan(row, seg, at):
            c = self.vert(at, bone)
            for j in range(n):
                f = self.bm.faces.new((row[j], row[(j + 1) % n], c))
                self.jobs.append((f, spec(seg, j)))
                faces.append(f)

        if caps[0]:
            fan(rows[0], 0, caps[0] if caps[0] is not True else sum(rings[0], Vector()) / n)
        if tip is not None:
            before = len(self.jobs)
            fan(rows[-1], len(rows) - 2, Vector(tip))
            if weave:
                last = len(rows) - 1
                for off, (f, _) in enumerate(self.jobs[before:]):
                    k = (off + 1) % n
                    uj, uk = U[last][off], U[last][k]
                    if uk < uj:
                        uk += perim[last]
                    centre = [v for v in f.verts if v not in (rows[last][off], rows[last][k])][0]
                    reach = (Vector(tip) - rings[last][off]).length
                    self.weave_uv[f] = {rows[last][off]: (uj, W[last][off]), rows[last][k]: (uk, W[last][k]),
                                        centre: ((uj + uk) / 2, W[last][off] + reach)}
        elif caps[1]:
            fan(rows[-1], len(rows) - 2, caps[1] if caps[1] is not True else sum(rings[-1], Vector()) / n)
        self.done(name, faces)
        if form:
            self.forms.setdefault(form, []).extend(faces)
        return faces

    def box(self, name, bone, centre, ax, ay, az, size, mapping, chamfer=0.0):
        """A block, its three axes given, `size` the full lengths; `chamfer` cuts its long corners (az is the long way)."""
        c = Vector(centre)
        hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        ch = min(chamfer, 0.45 * hx, 0.45 * hy)
        if ch > 0:
            flat_pts = [(hx, -hy + ch), (hx, hy - ch), (hx - ch, hy), (-hx + ch, hy), (-hx, hy - ch), (-hx, -hy + ch),
                        (-hx + ch, -hy), (hx - ch, -hy)]
        else:
            flat_pts = [(hx, -hy), (hx, hy), (-hx, hy), (-hx, -hy)]
        rings = [[c + ax * a + ay * b + az * (s * hz) for a, b in flat_pts] for s in (-1.0, 1.0)]
        return self.loft(name, bone, rings, mapping, caps=(True, True))

    def bar(self, name, bone, a, b, width, thick, normal, mapping):
        """A block running from a to b, its flat side toward `normal` (a stitch, a pin shaft, a thread)."""
        a, b = Vector(a), Vector(b)
        az = (b - a).normalized()
        n = Vector(normal)
        ay = (n - az * n.dot(az))
        if ay.length < 1e-4:
            ay = az.orthogonal()
        ay.normalize()
        ax = ay.cross(az)
        return self.box(name, bone, (a + b) / 2, ax, ay, az, (width, thick, (b - a).length), mapping)

    def tube(self, name, bone, path, radii, mapping, sides=6, tip=True, closed_loop=False):
        """A rope or a blade along points; the section is carried along so it never wrings."""
        pts = [Vector(p) for p in path]
        rings = []
        normal = None
        count = len(pts)
        for i, c in enumerate(pts):
            if closed_loop:
                tangent = (pts[(i + 1) % count] - pts[(i - 1) % count]).normalized()
            else:
                tangent = (pts[min(i + 1, count - 1)] - pts[max(i - 1, 0)]).normalized()
            if normal is None:
                helper = Z if abs(tangent.z) < 0.9 else Y
                normal = tangent.cross(helper).normalized()
            else:
                normal = (normal - tangent * normal.dot(tangent)).normalized()
            side = tangent.cross(normal).normalized()
            rings.append([c + normal * (radii[i] * math.cos(j * math.tau / sides)) + side * (radii[i] * math.sin(j * math.tau / sides))
                          for j in range(sides)])
        if closed_loop:
            rings.append(rings[0])
            return self.loft(name, bone, rings, mapping)
        if tip:
            return self.loft(name, bone, rings[:-1], mapping, caps=(True, False), tip=pts[-1])
        return self.loft(name, bone, rings, mapping, caps=(True, True))

    def blade(self, name, bone, base, mid, tip, width, thick, tone_a, tone_b):
        """One straw: a flat tapering blade with a bend in it, its two broad sides in two tones."""
        base, mid, tip = Vector(base), Vector(mid), Vector(tip)
        along = (tip - base).normalized()
        side = along.cross(Z)
        if side.length < 0.1:
            side = along.cross(X)
        side.normalize()
        face = side.cross(along).normalized()

        def ring(c, w, t):
            return [c + side * w + face * t, c - side * w + face * t, c - side * w - face * t, c + side * w - face * t]

        mapping = lambda i, j: flat(tone_a if j in (0, 3) else tone_b)
        return self.loft(name, bone, [ring(base, width, thick), ring(mid, width * 0.8, thick * 0.8)], mapping, caps=(True, False), tip=tip)

    def disc(self, name, bone, centre, normal, radius, depth, sides, tone_face, tone_side, turn=0.0):
        n = Vector(normal).normalized()
        ax = n.orthogonal().normalized()
        ay = n.cross(ax)
        t0 = math.radians(turn)
        ring = lambda offs, r: [Vector(centre) + n * offs + ax * (r * math.cos(t0 + math.tau * k / sides)) + ay * (r * math.sin(t0 + math.tau * k / sides))
                                for k in range(sides)]
        rings = [ring(-depth / 2, radius), ring(depth / 2, radius), ring(depth / 2 + depth * 0.25, radius * 0.86)]
        mapping = lambda i, j: flat(tone_side)
        faces = self.loft(name, bone, rings, mapping, caps=(True, False))
        # the front's own cap in its own tone
        row = [self.vert(p, bone) for p in rings[-1]]
        c = self.vert(Vector(centre) + n * (depth * 0.75), bone)
        cap = []
        for k in range(sides):
            f = self.bm.faces.new((row[k], row[(k + 1) % sides], c))
            self.jobs.append((f, flat(tone_face)))
            cap.append(f)
        self.done(name + "-face", cap)
        return faces

    def bend(self, faces, weights):
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def tree(self, form):
        """A ray-cast tree over one big form, so a detail lands on the real surface."""
        faces = self.forms[form]
        seen, verts, polys = {}, [], []
        for f in faces:
            poly = []
            for v in f.verts:
                if v not in seen:
                    seen[v] = len(verts)
                    verts.append(v.co.copy())
                poly.append(seen[v])
            polys.append(poly)
        return BVHTree.FromPolygons(verts, polys)

    def resolve_uvs(self):
        self.bm.normal_update()
        for face, spec in self.jobs:
            if spec[0] == "tones":
                _, lit, base, shade, up, down = spec
                nz = face.normal.z
                name = lit if nz > up else (shade if nz < down else base)
            else:
                name = spec[1]
            coords = self.weave_uv.get(face)
            if coords and name in tex.WEAVE_TONES:
                p = tex.WEAVE_PERIOD
                du = math.floor(min(c[0] for c in coords.values()) / p) * p
                dv = math.floor(min(c[1] for c in coords.values()) / p) * p
                if max(c[0] - du for c in coords.values()) > 6 * p or max(c[1] - dv for c in coords.values()) > 6 * p:
                    raise SystemExit("a woven face is wider than its tile in " + self.name)
                for loop in face.loops:
                    u, v = coords[loop.vert]
                    loop[self.uv].uv = tex.weave_uv(name, u - du, v - dv)
                continue
            uv = tex.atlas_uv(name)
            for loop in face.loops:
                loop[self.uv].uv = uv

    def to_object(self, armature):
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        for b in ALL_BONES:
            obj.vertex_groups.new(name=b)
        layer = self.bm.verts.layers.int["bone"]
        self.bm.verts.ensure_lookup_table()
        self.bm.verts.index_update()
        for v in self.bm.verts:
            mix = [(b, w) for b, w in self.blend.get(v, ()) if w > 0.001]
            if not mix:
                obj.vertex_groups[v[layer]].add([v.index], 1.0, "REPLACE")
                continue
            total = sum(w for _, w in mix)
            for b, w in mix:
                obj.vertex_groups[b].add([v.index], w / total, "REPLACE")
        for poly in mesh.polygons:
            poly.use_smooth = True
        mesh.set_sharp_from_angle(angle=math.radians(SHARP_ANGLE))
        mod = obj.modifiers.new("Armature", "ARMATURE")
        mod.object = armature
        obj.parent = armature
        return obj


# ---------------------------------------------------------------------------------------------------------------------
# THE LIGHT. Plain polygons with colours; nothing here goes through the UV job list.
# ---------------------------------------------------------------------------------------------------------------------


def linear(hexcode, alpha=1.0):
    def lin(c):
        c /= 255.0
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return tuple(lin(int(hexcode[i:i + 2], 16)) for i in (0, 2, 4)) + (alpha,)


C_CORE = linear("ffe8fb", 1.0)
C_HOT = linear("ff6ee8", 0.8)
C_SOUL = linear("f03cc8", 0.6)
C_DEEP = linear("9a1684", 0.3)
C_EMBER = linear("3a0a36", 0.1)
SPILL_HEX = "ff4ad8"
MOUTH, INSIDE = 1.0, 0.0
MOUTH_LIFT = 0.0040
SPILL_LIFT = 0.0044
SPILL_STRENGTH = 0.22


class Light:
    def __init__(self, name):
        self.name = name
        self.pos, self.nrm, self.uv, self.col, self.bone, self.idx = [], [], [], [], [], []

    def poly(self, bone, pts, normal, uvs, colours):
        pts = [Vector(p) for p in pts]
        nw = Vector()
        for k in range(len(pts)):
            a, b = pts[k], pts[(k + 1) % len(pts)]
            nw += a.cross(b)
        order = list(range(len(pts)))
        if nw.dot(normal) < 0:
            order.reverse()
        first = len(self.pos)
        for k in order:
            self.pos.append(pts[k])
            self.nrm.append(Vector(normal).normalized())
            self.uv.append(uvs[k])
            self.col.append(colours[k])
            self.bone.append(ALL_BONES.index(bone))
        for k in range(1, len(pts) - 1):
            self.idx += [first, first + k, first + k + 1]

    def to_object(self, armature):
        mesh = bpy.data.meshes.new(self.name)
        mesh.from_pydata([tuple(p) for p in self.pos], [], [tuple(self.idx[i:i + 3]) for i in range(0, len(self.idx), 3)])
        mesh.update()
        attr = mesh.color_attributes.new("Color", "FLOAT_COLOR", "POINT")
        for i, c in enumerate(self.col):
            attr.data[i].color = c
        layer = mesh.uv_layers.new(name="UVMap")
        for loop in mesh.loops:
            layer.data[loop.index].uv = self.uv[loop.vertex_index]
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        for b in ALL_BONES:
            obj.vertex_groups.new(name=b)
        for i, b in enumerate(self.bone):
            obj.vertex_groups[b].add([i], 1.0, "REPLACE")
        mod = obj.modifiers.new("Armature", "ARMATURE")
        mod.object = armature
        obj.parent = armature
        return obj


GLOW, SPILL = Light("glow-mesh"), Light("spill-mesh")


def glow(bone, pts, normal, colours, role=INSIDE):
    GLOW.poly(bone, pts, normal, [(role, 0.0)] * len(pts), colours)


def spill_poly(bone, pts, normal, alphas, uvs=None, colour=SPILL_HEX):
    SPILL.poly(bone, pts, normal, uvs or [(0.0, 0.0)] * len(pts), [linear(colour, a) for a in alphas])


RAYS = {
    FACE: lambda a, b: (Vector((a, -2.0, b)), Y),
    BACK: lambda a, b: (Vector((a, 2.0, b)), -Y),
    LEFT: lambda a, b: (Vector((2.0, a, b)), -X),
    RIGHT: lambda a, b: (Vector((-2.0, a, b)), X),
    TOP: lambda a, b: (Vector((a, b, 2.0)), -Z),
}


def on(tree, face, a, b):
    origin, direction = RAYS[face](a, b)
    hit, normal, _, _ = tree.ray_cast(origin, direction)
    if hit is None:
        raise SystemExit("nothing under %s %.3f %.3f" % (face, a, b))
    if normal.dot(direction) > 0:
        normal = -normal
    return hit, normal


def around(tree, angle, z, centre=(0.0, -0.03)):
    """A point on a form at a height, seen from outside at an angle (0 is the front, 90 its left side)."""
    t = math.radians(angle)
    out = Vector((math.sin(t), -math.cos(t), 0.0))
    origin = Vector((centre[0], centre[1], z)) + out * 2.0
    hit, normal, _, _ = tree.ray_cast(origin, -out)
    if hit is None:
        raise SystemExit("nothing round at %.1f %.3f" % (angle, z))
    if normal.dot(out) < 0:
        normal = -normal
    return hit, normal


def seam(bone, tree, face, points, gaps, depth, spread):
    """A tear cut into the body along typed points. Nothing stands off the cloth: `SoulGlow` cuts through the mouth and
    draws the cup. Returns its segments, for the stitches that lie across it."""
    hits = [on(tree, face, a, b) for a, b in points]
    segs = []
    for k in range(len(hits) - 1):
        (p0, n0), (p1, n1) = hits[k], hits[k + 1]
        n = (n0 + n1).normalized()
        x = p1 - p0
        length = x.length
        x = (x - n * x.dot(n)).normalized()
        y = n.cross(x).normalized()
        g, gb = gaps[k], gaps[k] * 0.62
        segs.append(dict(p0=p0, p1=p1, n=n, x=x, y=y, L=length, g=g, bone=bone))

        def at(p, across, up):
            return p + y * across + n * up

        glow(bone, [at(p0, -g, MOUTH_LIFT), at(p1, -g, MOUTH_LIFT), at(p1, g, MOUTH_LIFT), at(p0, g, MOUTH_LIFT)], n, [C_DEEP] * 4, MOUTH)
        for side in (-1.0, 1.0):
            glow(bone, [at(p0, side * g, 0.0), at(p1, side * g, 0.0), at(p1, side * gb, -depth * 0.45), at(p0, side * gb, -depth * 0.45)],
                 y * -side, [C_EMBER, C_EMBER, C_DEEP, C_DEEP])
            glow(bone, [at(p0, side * gb, -depth * 0.45), at(p1, side * gb, -depth * 0.45), at(p1, side * gb, -depth), at(p0, side * gb, -depth)],
                 y * -side, [C_DEEP, C_DEEP, C_HOT, C_HOT])
        stops = ((-gb, C_SOUL), (0.0, C_CORE), (gb, C_SOUL))
        for (u0, c0), (u1, c1) in zip(stops, stops[1:]):
            glow(bone, [at(p0, u0, -depth), at(p1, u0, -depth), at(p1, u1, -depth), at(p0, u1, -depth)], n, [c0, c0, c1, c1])
    for seg, end, out in ((segs[0], segs[0]["p0"], -1.0), (segs[-1], segs[-1]["p1"], 1.0)):
        y, n, g = seg["y"], seg["n"], seg["g"]
        glow(bone, [end - y * g, end + y * g, end + y * (g * 0.62) - n * depth, end - y * (g * 0.62) - n * depth],
             seg["x"] * -out, [C_EMBER, C_EMBER, C_HOT, C_HOT])
    count = len(hits)
    for side in (-1.0, 1.0):
        rows = []
        for k in range(count):
            near = [segs[j] for j in (k - 1, k) if 0 <= j < len(segs)]
            y = (near[0]["y"] + near[-1]["y"]).normalized() * side
            n = (near[0]["n"] + near[-1]["n"]).normalized()
            g = sum(s["g"] for s in near) / len(near)
            p = hits[k][0]
            fade = 0.3 if k in (0, count - 1) else 1.0
            rows.append((p + y * g + n * SPILL_LIFT, p + y * (g + spread) + n * SPILL_LIFT, n, fade))
        for (i0, o0, n0, f0), (i1, o1, n1, f1) in zip(rows, rows[1:]):
            spill_poly(bone, [i0, i1, o1, o0], (n0 + n1).normalized(), [SPILL_STRENGTH * f0, SPILL_STRENGTH * f1, 0.0, 0.0])
    return segs


def hole(bone, tree, face, centre, radii, depth, spread, turn=0.0):
    """A round hole cut into the body, typed radii at even turns. Returns (centre on the surface, its normal)."""
    c, cn = on(tree, face, centre[0], centre[1])
    rim = []
    for k, r in enumerate(radii):
        t = math.radians(turn + 360.0 * k / len(radii))
        rim.append(on(tree, face, centre[0] + r * math.cos(t), centre[1] + r * math.sin(t)))
    count = len(rim)
    inward = cn * -depth
    floor_c = c + inward
    lifted = [p + n * MOUTH_LIFT for p, n in rim]
    bottom = [c.lerp(p, 0.56) + inward for p, n in rim]
    middle = [c.lerp(p, 0.80) + inward * 0.45 for p, n in rim]
    for k in range(count):
        k1 = (k + 1) % count
        glow(bone, [c + cn * MOUTH_LIFT, lifted[k], lifted[k1]], cn, [C_DEEP] * 3, MOUTH)
        glow(bone, [rim[k][0], rim[k1][0], middle[k1], middle[k]], cn, [C_EMBER, C_EMBER, C_DEEP, C_DEEP])
        glow(bone, [middle[k], middle[k1], bottom[k1], bottom[k]], cn, [C_DEEP, C_DEEP, C_HOT, C_HOT])
        glow(bone, [floor_c, bottom[k], bottom[k1]], cn, [C_CORE, C_SOUL, C_SOUL])
    for k in range(count):
        (p0, n0), (p1, n1) = rim[k], rim[(k + 1) % count]
        r0 = (p0 - c).normalized()
        r1 = (p1 - c).normalized()
        spill_poly(bone, [p0 + n0 * SPILL_LIFT, p1 + n1 * SPILL_LIFT, p1 + r1 * spread + n1 * SPILL_LIFT, p0 + r0 * spread + n0 * SPILL_LIFT],
                   cn, [SPILL_STRENGTH, SPILL_STRENGTH, 0.0, 0.0])
    return c, cn


def stitch(part, name, seg, t, length, width, thick, angle, tone="thread"):
    """A stitch lying across a seam's gap, on the cloth, its two ends sunk a little into it."""
    centre = seg["p0"].lerp(seg["p1"], t) + seg["n"] * (thick * 0.45)
    a = math.radians(90.0 + angle)
    d = seg["x"] * math.cos(a) + seg["y"] * math.sin(a)
    part.bar(name, seg["bone"], centre - d * (length / 2) - seg["n"] * 0.003, centre + d * (length / 2) - seg["n"] * 0.003, width, thick, seg["n"], flat(tone))


def sewn_patch(part, name, bone, tree, face, centre, outline, tone, edge, stitches=(), lift=0.007, turn=0.0, one_normal=False):
    """A patch sewn on: a typed outline (a, b offsets on the face) landed on the surface, a slab `lift` proud, one flat tone on
    its face and a darker one on its edge; `stitches` are outline indices that get a chunky stitch over the edge."""
    t = math.radians(turn)
    c0, s0 = math.cos(t), math.sin(t)
    fine = []
    for k in range(len(outline)):
        a0, b0 = outline[k]
        a1, b1 = outline[(k + 1) % len(outline)]
        for q in range(3):
            u = q / 3.0
            fine.append((a0 + (a1 - a0) * u, b0 + (b1 - b0) * u))
    place = lambda a, b: (centre[0] + a * c0 - b * s0, centre[1] + a * s0 + b * c0)
    hits = [on(tree, face, *place(a, b)) for a, b in fine]
    inner = [on(tree, face, *place(a * 0.55, b * 0.55)) for a, b in fine]
    mid, midn = on(tree, face, centre[0], centre[1])
    if one_normal:
        # every point lifted the same way, so the body's facets cannot put a wave in the patch's edge
        hits = [(p, midn) for p, n in hits]
        inner = [(p, midn) for p, n in inner]
    lo = [p - n * 0.004 for p, n in hits]
    hi = [p + n * lift for p, n in hits]
    part.loft(name, bone, [lo, hi], flat(edge))
    part.loft(name + "-face", bone, [hi, [p + n * (lift + 0.001) for p, n in inner]], flat(tone), caps=(False, mid + midn * (lift + 0.001)))
    for k in stitches:
        i = k * 3 + 1
        p, n = hits[i]
        q, m = hits[(i + 1) % len(hits)]
        e = (q + p) / 2
        out = (e - mid)
        out = (out - n * out.dot(n)).normalized()
        part.bar("%s-st%d" % (name, k), bone, e - out * 0.018 + n * (lift + 0.002), e + out * 0.016 + n * 0.002, 0.0105, 0.007, n, flat("thread"))


def pin(part, name, bone, tree, face, a, b, direction, length, gem_size, gem, gem_lit):
    """A pin pushed into the sack: a shaft of her gold and one big cut gem."""
    p, n = on(tree, face, a, b)
    d = Vector(direction).normalized()
    tip = p + d * length
    part.bar(name, bone, p - d * 0.02, tip, 0.012, 0.012, n, flat("gold_dark"))
    centre = tip + d * (gem_size * 0.5)
    ax = d.orthogonal().normalized()
    ay = d.cross(ax)
    r = gem_size * 0.55
    ring = lambda offs, rr: [centre + d * offs + ax * (rr * math.cos(math.tau * k / 6)) + ay * (rr * math.sin(math.tau * k / 6)) for k in range(6)]
    up = ring(gem_size * 0.1, r)
    lo = ring(-gem_size * 0.1, r)
    part.loft(name + "-gem", bone, [lo, up], lambda i, j: flat(gem if j % 2 else gem_lit), caps=(centre - d * (gem_size * 0.62), centre + d * (gem_size * 0.62)))


# ---------------------------------------------------------------------------------------------------------------------
# THE BIG FORMS. Every table is typed.
# ---------------------------------------------------------------------------------------------------------------------
# (z, half width, reach forward, reach back, squareness). The head is a big soft sack, fullest at the cheeks, gathered at the top.
HEAD_ROWS = [
    (0.446, 0.120, 0.100, 0.104, 2.4),
    (0.462, 0.172, 0.138, 0.142, 2.7),
    (0.490, 0.200, 0.156, 0.158, 3.0),
    (0.530, 0.212, 0.164, 0.164, 3.1),
    (0.575, 0.208, 0.164, 0.164, 3.1),
    (0.620, 0.200, 0.160, 0.162, 3.2),
    (0.648, 0.186, 0.152, 0.154, 3.0),
    (0.670, 0.164, 0.138, 0.140, 2.8),
    (0.688, 0.134, 0.116, 0.118, 2.5),
    (0.702, 0.100, 0.094, 0.096, 2.2),
    (0.712, 0.084, 0.080, 0.082, 2.2),
]
HEAD_BANDS = ["sack_shade", "sack_shade", "sack", "sack", "sack", "sack", "sack_lit", "sack_lit", "sack_lit", "sack_shade"]

# The body: a core, the FAT POT BELLY pushed forward and sagging low, over the rope.
BODY_ROWS = [
    (0.146, 0.150, 0.118, 0.112, 2.4),
    (0.166, 0.186, 0.150, 0.128, 2.6),
    (0.196, 0.200, 0.172, 0.136, 2.6),
    (0.222, 0.222, 0.216, 0.146, 2.5),
    (0.252, 0.246, 0.262, 0.156, 2.5),
    (0.292, 0.248, 0.276, 0.160, 2.5),
    (0.334, 0.238, 0.250, 0.160, 2.5),
    (0.374, 0.222, 0.198, 0.156, 2.6),
    (0.416, 0.206, 0.166, 0.150, 2.7),
    (0.446, 0.170, 0.138, 0.128, 2.6),
    (0.470, 0.116, 0.100, 0.096, 2.4),
]
BODY_BANDS = ["sack_shade", "sack_shade", "sack_deep", "sack_shade", "sack", "sack_lit", "sack_lit", "sack", "sack", "sack"]
N_HEAD, N_BODY = 28, 30


def body_at(z, offset=0.0):
    """The body's section at a height, interpolated off its rows, grown by `offset`."""
    rows = BODY_ROWS
    for a, b in zip(rows, rows[1:]):
        if a[0] <= z <= b[0]:
            t = (z - a[0]) / (b[0] - a[0])
            return tuple(a[i] + (b[i] - a[i]) * t + (offset if i < 4 else 0.0) for i in range(1, 5))
    r = rows[-1] if z > rows[-1][0] else rows[0]
    return (r[1] + offset, r[2] + offset, r[3] + offset, r[4])


def build_head(part):
    rings = [superellipse(0.0, 0.0, z, hx, yf, yb, p, N_HEAD) for z, hx, yf, yb, p in HEAD_ROWS]
    part.loft("head", "head", rings, lambda i, j: flat(HEAD_BANDS[i]), caps=(True, False), form="head", weave=(21, 3, "z"))

    # THE CROWN: the sack's torn top. Past the tie it flares into big torn teeth, then folds down inside, where the light is.
    teeth = [0.054, 0.030, 0.062, 0.026, 0.048, 0.034, 0.066, 0.024, 0.056, 0.032, 0.060, 0.028, 0.050, 0.036]
    n = len(teeth)
    base = superellipse(0.0, 0.0, 0.712, 0.084, 0.080, 0.082, 2.2, n)
    waist = superellipse(0.0, 0.0, 0.724, 0.092, 0.088, 0.090, 2.2, n)
    rim = []
    for k, h in enumerate(teeth):
        t = math.tau * k / n
        spread = 0.118 + 0.34 * h
        rim.append(Vector((spread * math.cos(t), spread * math.sin(t), 0.716 + h)))
    inner = [p * Vector((0.78, 0.78, 1.0)) - Vector((0, 0, 0.014)) for p in rim]
    well = superellipse(0.0, 0.0, 0.704, 0.070, 0.070, 0.070, 2.0, n)

    def crown_tone(i, j):
        if i == 0:
            return flat("sack_shade")
        if i == 1:
            return flat("sack_lit" if j % 2 == 0 else "sack")
        return flat("sack_shade" if i == 2 else "sack_dark")
    part.loft("crown", "head", [base, waist, rim, inner, well], crown_tone, form="crown")
    # The light falling on the crown's own inside wall: full at the foot, gone at the torn lip. Cloth lit by the hole below it.
    for k in range(n):
        k1 = (k + 1) % n
        lift = lambda p: p + (Vector((0, 0, p.z)) - p).normalized() * 0.003
        mid_n = (Vector((0, 0, well[k].z)) - well[k]).normalized() + Z
        spill_poly("head", [lift(well[k]), lift(well[k1]), lift(inner[k1]), lift(inner[k])], mid_n, [0.95, 0.95, 0.10, 0.10], colour="ff6ee8")
    part.forms["crown-floor"] = []
    row = [part.vert(p, "head") for p in well]
    c = part.vert((0, 0, 0.704), "head")
    floor = []
    for k in range(n):
        f = part.bm.faces.new((row[k], row[(k + 1) % n], c))
        part.jobs.append((f, flat("sack_dark")))
        floor.append(f)
    part.done("crown-floor", floor)
    part.forms["crown-floor"] = floor


def build_body(part):
    rings = [superellipse(0.0, -0.0, z, hx, yf, yb, p, N_BODY) for z, hx, yf, yb, p in BODY_ROWS]
    part.loft("body", "torso", rings, lambda i, j: flat(BODY_BANDS[i]), caps=(True, True), form="body", weave=(22, 5, "z"))

    # THE TORN HEM under the rope: big teeth over the tops of the legs, a separate skirt piece with a dark inside.
    drops = [0.126, 0.146, 0.120, 0.150, 0.130, 0.116, 0.148, 0.124, 0.142, 0.118, 0.150, 0.128, 0.114, 0.146,
             0.122, 0.150, 0.118, 0.140, 0.126, 0.112, 0.148, 0.120, 0.144, 0.130]
    n = len(drops)
    top = superellipse(0.0, 0.0, 0.206, 0.204, 0.178, 0.142, 2.6, n)
    bottom = []
    for k, z in enumerate(drops):
        t = math.tau * k / n
        c, s = math.cos(t), math.sin(t)
        x = 0.222 * math.copysign(abs(c) ** (2 / 2.6), c)
        y = math.copysign(abs(s) ** (2 / 2.6), s)
        y *= 0.166 if y > 0 else 0.200
        bottom.append(Vector((x, y, z)))
    inside = [p * Vector((0.90, 0.90, 1.0)) + Vector((0, 0, 0.006)) for p in bottom]
    inner_top = superellipse(0.0, 0.0, 0.180, 0.150, 0.130, 0.110, 2.4, n)
    faces = part.loft("hem", "torso", [top, bottom, inside, inner_top], lambda i, j: flat(("sack_shade" if j % 2 else "sack_b_shade") if i == 0 else "sack_dark"), weave=(18, 0, "z"))

    def hem_weights(co):
        w = 0.55 * max(0.0, min(1.0, (0.182 - co.z) / 0.05))
        leg = "leg-left" if co.x >= 0 else "leg-right"
        return [("torso", 1.0 - w), (leg, w)]
    part.bend(faces, hem_weights)

    # A DARK NECK under the head, so a nod shows no gap.
    part.loft("neck", "torso", [superellipse(0, 0, 0.462, 0.100, 0.092, 0.092, 2.2, 12), superellipse(0, 0, 0.520, 0.086, 0.080, 0.080, 2.2, 12)],
              flat("sack_dark"), caps=(False, True))

    # THE CAPE: her purple, torn at the bottom edge, one piece round the shoulders, its edge a band of her gold.
    zs = [0.356, 0.388, 0.344, 0.380, 0.336, 0.378, 0.350, 0.392, 0.342, 0.384, 0.352, 0.396, 0.360, 0.388, 0.350, 0.394,
          0.346, 0.380, 0.338, 0.376, 0.344, 0.390, 0.348, 0.382]
    n = len(zs)

    def cape_point(k, z, off):
        hx, yf, yb, p = body_at(z, off)
        t = math.tau * k / n
        c, s = math.cos(t), math.sin(t)
        x = hx * math.copysign(abs(c) ** (2 / p), c)
        y = math.copysign(abs(s) ** (2 / p), s)
        return Vector((x, y * (yb if y > 0 else yf), z))

    collar = [cape_point(k, 0.486, 0.026) for k in range(n)]
    shoulders = [cape_point(k, 0.452, 0.018) for k in range(n)]
    upper = [cape_point(k, 0.420, 0.018) for k in range(n)]
    mids = [cape_point(k, zs[k] + 0.030, 0.017) for k in range(n)]
    band = [cape_point(k, zs[k] + 0.006, 0.017) for k in range(n)]
    edge = [cape_point(k, zs[k], 0.019) for k in range(n)]
    under = [cape_point(k, zs[k] + 0.010, 0.002) for k in range(n)]

    def cape_tone(i, j):
        return flat(["purple_lit", "purple", "purple", "gold", "gold_dark", "purple_dark"][i])
    part.loft("cape", "torso", [collar, shoulders, upper, mids, band, edge, under], cape_tone)
    # the collar: a gold roll round the neck
    part.loft("collar", "torso", [superellipse(0, 0, 0.474, 0.150, 0.132, 0.126, 2.5, 18), superellipse(0, 0, 0.492, 0.152, 0.134, 0.128, 2.5, 18),
                                  superellipse(0, 0, 0.502, 0.132, 0.116, 0.110, 2.4, 18)], lambda i, j: flat("gold" if i == 0 else "gold_lit"))


LEG_ROWS = [  # (z, half width, forward, back, squareness), about the hip, feet on zero: a thick stuffed stump with a round toe
    (0.000, 0.072, 0.108, 0.082, 2.4),
    (0.012, 0.086, 0.122, 0.092, 2.4),
    (0.040, 0.088, 0.124, 0.094, 2.5),
    (0.066, 0.084, 0.100, 0.090, 2.5),
    (0.094, 0.080, 0.084, 0.084, 2.5),
    (0.140, 0.080, 0.082, 0.082, 2.5),
    (0.190, 0.076, 0.078, 0.078, 2.5),
]


def build_leg(part, side, bone, base_tone):
    cx = side * 0.100
    rings = [superellipse(cx, -0.006, z, hx, yf, yb, p, 16) for z, hx, yf, yb, p in LEG_ROWS]
    lit, mid, shade = {"a": ("sack_lit", "sack", "sack_shade"), "b": ("sack_b_lit", "sack_b", "sack_b_shade")}[base_tone]
    bands = ["sack_deep", shade, mid, mid, mid, shade]

    def tone(i, j):
        if i == 1:
            return flat(lit)
        return flat(bands[i])
    part.loft("leg-%d" % side, bone, rings, tone, caps=(True, True), form="leg-%d" % side, weave=(12, 5, "z"))
    # two turns of her purple round the ankle
    for k, (z, tilt) in enumerate(((0.078, 3.0 * side), (0.100, -4.0 * side))):
        band = []
        for zz, g in ((-0.012, 0.0), (0.0, 0.007), (0.012, 0.0)):
            ring = superellipse(cx, -0.006, z + zz, 0.088 + g, 0.092 + g, 0.092 + g, 2.5, 12)
            for p in ring:
                p.z += math.tan(math.radians(tilt)) * (p.x - cx)
            band.append(ring)
        part.loft("wrap-ankle-%d-%d" % (side, k), bone, band, tones("purple_lit", "purple", "purple_dark", up=0.5, down=-0.5))


ARM_ROWS = [  # x along the arm, half depth, half height
    (0.186, 0.048, 0.050), (0.215, 0.064, 0.060), (0.262, 0.070, 0.062), (0.310, 0.071, 0.062), (0.336, 0.070, 0.062),
    (0.364, 0.070, 0.063), (0.402, 0.072, 0.066), (0.436, 0.072, 0.066),
]
MITTEN_ROWS = [  # the MITTEN STUMP: fatter than the wrist, one round end, no fingers, no thumb
    (0.430, 0.070, 0.064), (0.452, 0.084, 0.076), (0.488, 0.090, 0.082), (0.526, 0.086, 0.078), (0.552, 0.066, 0.060),
]
MITTEN_TIP = 0.566


def limb_tones(lit, base, shade, n):
    """Which column of a ring round the arm takes which tone: the top third lit, the bottom third shaded, straight lines."""
    def mapping(i, j):
        up = math.sin(math.tau * (j + 0.5) / n)
        return flat(lit if up > 0.55 else (shade if up < -0.55 else base))
    return mapping


def arm_weights(side):
    arm, fore = ("arm-left", "forearm-left") if side > 0 else ("arm-right", "forearm-right")

    def weights(co):
        w = max(0.0, min(1.0, (abs(co.x) - (ELBOW_X - 0.018)) / 0.036))
        return [(arm, 1.0 - w), (fore, w)]
    return weights


def build_arm(part, side):
    arm, fore = ("arm-left", "forearm-left") if side > 0 else ("arm-right", "forearm-right")
    rings = [arm_ring(side * x, 0.0, ARM_Z, ry, rz, 2.3, 14) for x, ry, rz in ARM_ROWS]
    mapping = limb_tones("sack_lit", "sack", "sack_shade", 14) if side > 0 else limb_tones("sack_b_lit", "sack_b", "sack_b_shade", 14)
    faces = part.loft("arm-%d" % side, arm, rings, mapping, caps=(True, False), form="arm-%d" % side, weave=(7, 0, "x"))
    part.bend(faces, arm_weights(side))
    rings = [arm_ring(side * x, 0.0, ARM_Z, ry, rz, 2.4, 14) for x, ry, rz in MITTEN_ROWS]
    part.loft("mitten-%d" % side, fore, rings, limb_tones("mitten_lit", "mitten", "mitten_shade", 14), caps=(True, False),
              tip=(side * MITTEN_TIP, 0.0, ARM_Z), form="mitten-%d" % side, weave=(7, 0, "x"))

    # HER PURPLE WRAPS: three big turns round the forearm, one round the upper arm, each its own lean.
    wraps = [(0.272, 0.042, 6, arm), (0.384, 0.038, -5, fore), (0.420, 0.036, 4, fore)] if side > 0 else \
            [(0.282, 0.040, -5, arm), (0.382, 0.036, 6, fore), (0.418, 0.038, -4, fore)]
    for k, (x, w, lean, bone) in enumerate(wraps):
        l = math.radians(lean)
        band = []
        for dx, g in ((-w / 2, 0.0), (-w / 2 + 0.005, 0.008), (w / 2 - 0.005, 0.008), (w / 2, 0.0)):
            ring = arm_ring(side * (x + dx), 0.0, ARM_Z, 0.073 + g, 0.068 + g, 2.3, 12)
            for p in ring:
                p.x += math.tan(l) * (p.z - ARM_Z)
            band.append(ring)
        part.loft("wrap-%d-%d" % (side, k), bone, band, limb_tones("purple_lit", "purple", "purple_dark", 12))


def build_rope(part, tree):
    """Her gold rope, under the belly, knotted on its left front, two hanging ends. Its turns are two gold tones, not a drawing."""
    stations = [(0, 0.2045), (24, 0.2045), (48, 0.205), (72, 0.206), (96, 0.208), (120, 0.210), (144, 0.212), (168, 0.213), (192, 0.213),
                (216, 0.212), (240, 0.210), (264, 0.208), (288, 0.206), (312, 0.205), (336, 0.2045)]
    hits = [around(tree, a, z) for a, z in stations]
    r = 0.0145
    path = []
    for i in range(len(hits)):
        p0, n0 = hits[i]
        p1, n1 = hits[(i + 1) % len(hits)]
        path.append(p0 + n0 * (r * 0.5))
        path.append((p0 + p1) / 2 + (n0 + n1).normalized() * (r * 0.5))
    part.tube("rope", "torso", path, [r] * len(path), lambda i, j: flat("gold" if i % 2 else "gold_dark"), sides=6, closed_loop=True)

    def hang(angle):
        return around(tree, angle, 0.204)

    # the knot
    kp, kn = hang(26)
    k = kp + kn * 0.022
    part.box("rope-knot", "torso", k, X, kn, Z, (0.052, 0.040, 0.044), flat("gold"), chamfer=0.012)
    part.tube("rope-end-1", "torso", [k + Vector((0.008, -0.004, -0.010)), k + Vector((0.020, -0.006, -0.050)), k + Vector((0.024, -0.004, -0.080))],
              [0.012, 0.011, 0.011], lambda i, j: flat("gold_lit" if i % 2 else "gold"), sides=6, tip=False)
    part.box("rope-end-1-tuft", "torso", k + Vector((0.024, -0.004, -0.092)), X, Y, Z, (0.026, 0.024, 0.024), flat("straw"), chamfer=0.006)
    part.tube("rope-end-2", "torso", [k + Vector((-0.010, -0.004, -0.010)), k + Vector((-0.016, -0.006, -0.044)), k + Vector((-0.018, -0.004, -0.066))],
              [0.012, 0.011, 0.011], lambda i, j: flat("gold" if i % 2 else "gold_lit"), sides=6, tip=False)
    part.box("rope-end-2-tuft", "torso", k + Vector((-0.018, -0.004, -0.078)), X, Y, Z, (0.024, 0.024, 0.022), flat("straw_lit"), chamfer=0.006)

    # CHARMS. A little purple bundle doll, a bone tag with a crimson X, a charcoal pouch, a second bundle at the right hip.
    p, n = hang(-20)
    a = p + n * 0.020
    part.bar("charm-doll-loop", "torso", a + Vector((0, 0, 0.004)), a + Vector((0, 0, -0.016)), 0.007, 0.007, n, flat("gold_dark"))
    part.box("charm-doll-head", "torso", a + Vector((0, -0.002, -0.030)), X, Y, Z, (0.038, 0.034, 0.034), flat("purple_lit"), chamfer=0.009)
    part.box("charm-doll-body", "torso", a + Vector((0, -0.002, -0.068)), X, Y, Z, (0.050, 0.036, 0.046), flat("purple"), chamfer=0.010)
    part.box("charm-doll-tie", "torso", a + Vector((0, -0.002, -0.048)), X, Y, Z, (0.036, 0.030, 0.008), flat("gold"))

    p, n = hang(-2)
    a = p + n * 0.020
    part.bar("charm-tag-loop", "torso", a + Vector((0, 0, 0.004)), a + Vector((0.001, 0, -0.014)), 0.006, 0.006, n, flat("gold_dark"))
    tag = a + Vector((0.001, -0.002, -0.044))
    part.box("charm-tag", "torso", tag, X, Y, Z, (0.044, 0.010, 0.060), flat("bone"), chamfer=0.003)
    part.bar("charm-tag-x1", "torso", tag + Vector((-0.012, -0.0055, -0.015)), tag + Vector((0.012, -0.0055, 0.015)), 0.008, 0.003, -Y, flat("crimson"))
    part.bar("charm-tag-x2", "torso", tag + Vector((0.012, -0.0055, -0.015)), tag + Vector((-0.012, -0.0055, 0.015)), 0.008, 0.003, -Y, flat("crimson"))

    p, n = hang(56)
    a = p + n * 0.026
    part.box("charm-pouch-neck", "torso", a + Vector((0, 0, -0.016)), X, Y, Z, (0.036, 0.032, 0.012), flat("gold"))
    part.box("charm-pouch", "torso", a + Vector((0, 0, -0.050)), X, Y, Z, (0.066, 0.054, 0.056), flat("coat_lit"), chamfer=0.014)

    p, n = hang(-74)
    a = p + n * 0.020
    part.box("charm-hip-tie", "torso", a + Vector((0, 0, -0.016)), X, Y, Z, (0.032, 0.030, 0.008), flat("gold"))
    part.box("charm-hip", "torso", a + Vector((0, 0, -0.042)), X, Y, Z, (0.040, 0.036, 0.046), flat("lilac_dark"), chamfer=0.009)


def straw(part, bone, centre, rows):
    """Hand-set straw: (root offset, mid offset, tip offset, half width)."""
    c = Vector(centre)
    for k, (a, m, t, w) in enumerate(rows):
        part.blade("straw-%s-%d" % (bone, k) + ("-%d" % len(part.pieces)), bone, c + Vector(a), c + Vector(m), c + Vector(t), w, w * 0.45,
                   "straw" if k % 2 else "straw_lit", "straw_dark" if k % 3 == 0 else "straw")


def build_face(part, tree):
    # THE BUTTON EYE (its right, the viewer's left): a torn socket with the light in it, one big button of her purple sewn over it,
    # standing off the face so the light leaks round its rim, four holes the light shows through, a heavy cross of thread.
    c, cn = hole("head", tree, FACE, (-0.078, 0.592), [0.060, 0.062, 0.058, 0.061, 0.063, 0.059, 0.060, 0.062, 0.058, 0.061], 0.034, 0.018, turn=8.0)
    at = c + cn * 0.012
    part.disc("button", "head", at, cn, 0.054, 0.012, 16, "purple_lit", "purple_dark")
    front = at + cn * 0.0092
    ax = (X - cn * X.dot(cn)).normalized()
    ay = cn.cross(ax)
    d1, d2 = (ax + ay).normalized(), (ax - ay).normalized()
    part.bar("button-thread-a", "head", front - d1 * 0.026, front + d1 * 0.026, 0.0125, 0.006, cn, flat("thread_lit"))
    part.bar("button-thread-b", "head", front - d2 * 0.026, front + d2 * 0.026, 0.0125, 0.006, cn, flat("thread_lit"))

    # THE X EYE: a smaller hole, the light in it, a heavy X stitched across it.
    c, cn = hole("head", tree, FACE, (0.080, 0.592), [0.046, 0.043, 0.048, 0.044, 0.042, 0.047, 0.044, 0.048], 0.034, 0.016, turn=20.0)
    ax = (X - cn * X.dot(cn)).normalized()
    ay = cn.cross(ax)
    for name, angle, half in (("x-a", 45.0, 0.064), ("x-b", -45.0, 0.064)):
        a = math.radians(angle)
        d = ax * math.cos(a) + ay * math.sin(a)
        part.bar(name, "head", c - d * half + cn * 0.004, c + d * half + cn * 0.004, 0.023, 0.012, cn, flat("thread"))

    # THE MOUTH: a wide slit across the lower face, higher at its left end, the light inside, stitched shut.
    grin = seam("head", tree, FACE,
                [(-0.128, 0.532), (-0.094, 0.518), (-0.052, 0.510), (0.000, 0.507), (0.050, 0.510), (0.096, 0.522), (0.132, 0.540)],
                [0.008, 0.012, 0.014, 0.014, 0.012, 0.008], 0.030, 0.016)
    for k, (s, t, angle, length) in enumerate(((0, 0.50, -7, 0.044), (1, 0.45, -4, 0.052), (2, 0.40, -2, 0.056), (3, 0.10, 0, 0.056),
                                               (3, 0.80, 2, 0.056), (4, 0.50, 4, 0.052), (5, 0.50, 8, 0.042))):
        stitch(part, "mouth-st-%d" % k, grin[s], t, length, 0.0125, 0.008, angle)

    # A pin in the forehead, a big lilac gem: hers, and it breaks the head's silhouette.
    pin(part, "pin-head", "head", tree, FACE, 0.142, 0.650, (0.70, -0.36, 0.62), 0.072, 0.048, "gem_lilac", "gem_lilac_lit")
    pin(part, "pin-head-2", "head", tree, RIGHT, -0.030, 0.640, (-0.84, -0.08, 0.54), 0.066, 0.044, "gem_magenta", "gem_magenta_lit")

    # THE BACK OF THE HEAD: split, stitched, a crimson patch.
    b = seam("head", tree, BACK, [(0.012, 0.640), (-0.004, 0.600), (0.010, 0.560), (-0.002, 0.520)], [0.010, 0.014, 0.011], 0.030, 0.016)
    for k, (s, t, a) in enumerate(((0, 0.5, 6), (1, 0.5, -5), (2, 0.5, 5))):
        stitch(part, "head-back-st-%d" % k, b[s], t, 0.052, 0.0110, 0.007, a)
    sewn_patch(part, "patch-head", "head", tree, BACK, (-0.090, 0.570), [(-0.042, -0.036), (0.038, -0.042), (0.044, 0.032), (-0.036, 0.040)],
               "crimson", "crimson_dark", stitches=(0, 1, 2, 3), turn=8)

    # THE SACK'S SIDE SEAMS: a few chunky whip stitches down each side of the head.
    for face, sgn, rows in ((LEFT, 1, ((0.010, 0.606, 18), (0.014, 0.566, -16), (0.008, 0.526, 20))), (RIGHT, -1, ((0.006, 0.598, -20), (0.012, 0.558, 16), (0.006, 0.518, -18)))):
        for k, (a, z, ang) in enumerate(rows):
            p, n = on(tree, face, a, z)
            d = Vector((0.0, math.cos(math.radians(ang)), math.sin(math.radians(ang)))) * 0.024
            part.bar("whip-%d-%d" % (sgn, k), "head", p - d + n * 0.002, p + d + n * 0.002, 0.0105, 0.007, n, flat("thread"))

    # The tie under the crown: her gold rope, knotted on its left, two short ends.
    tie = [Vector((0.092 * math.cos(math.tau * k / 12), 0.090 * math.sin(math.tau * k / 12), 0.709)) for k in range(12)]
    part.tube("crown-tie", "head", tie, [0.0115] * 12, lambda i, j: flat("gold_lit" if i % 2 else "gold"), sides=6, closed_loop=True)
    k = Vector((0.088, -0.038, 0.700))
    part.box("crown-knot", "head", k, X, Y, Z, (0.034, 0.034, 0.030), flat("gold"), chamfer=0.008)
    part.tube("crown-end", "head", [k + Vector((0.006, -0.006, -0.008)), k + Vector((0.028, -0.014, -0.030)), k + Vector((0.036, -0.016, -0.056))],
              [0.009, 0.008, 0.0], lambda i, j: flat("gold"), sides=5)

    # The straw, standing out of the torn top.
    straw(part, "head", (0, 0, 0.700), [
        ((-0.040, 0.012, 0.0), (-0.066, 0.016, 0.060), (-0.092, 0.020, 0.098), 0.0115),
        ((-0.024, -0.030, 0.0), (-0.040, -0.052, 0.062), (-0.054, -0.076, 0.108), 0.0105),
        ((-0.006, 0.030, 0.0), (-0.010, 0.046, 0.066), (-0.018, 0.070, 0.106), 0.0125),
        ((0.014, -0.018, 0.0), (0.016, -0.026, 0.072), (0.024, -0.036, 0.122), 0.0120),
        ((0.032, 0.018, 0.0), (0.052, 0.028, 0.062), (0.076, 0.040, 0.104), 0.0120),
        ((0.046, -0.028, 0.0), (0.070, -0.040, 0.054), (0.098, -0.058, 0.086), 0.0105),
        ((-0.004, 0.002, 0.0), (-0.004, 0.002, 0.070), (-0.010, 0.000, 0.126), 0.0130),
    ])
    # The light down the crown: a hole in its floor, and a few tongues licking up between the straw.
    floor_tree = part.tree("crown-floor")
    hole("head", floor_tree, TOP, (0.0, 0.0), [0.060, 0.062, 0.059, 0.061, 0.060, 0.062, 0.059, 0.061], 0.028, 0.008)
    # TORN SLITS in the crown's own wall, so the hole shows from the front and the sides, not only from above. Real mouths.
    crown = part.tree("crown")
    for face, pts, gaps in (
            (FACE, [(-0.030, 0.728), (-0.034, 0.741), (-0.028, 0.754)], [0.012, 0.009]),
            (FACE, [(0.070, 0.730), (0.076, 0.740), (0.084, 0.750)], [0.011, 0.008]),
            (FACE, [(-0.086, 0.732), (-0.092, 0.741), (-0.098, 0.750)], [0.010, 0.008]),
            (LEFT, [(0.000, 0.728), (-0.004, 0.739), (0.001, 0.750)], [0.011, 0.008]),
            (RIGHT, [(-0.058, 0.728), (-0.054, 0.739), (-0.059, 0.750)], [0.011, 0.008]),
            (BACK, [(-0.030, 0.728), (-0.026, 0.738), (-0.031, 0.748)], [0.010, 0.008]),
            (BACK, [(0.070, 0.730), (0.076, 0.741), (0.082, 0.752)], [0.011, 0.008])):
        seam("head", crown, face, pts, gaps, 0.026, 0.010)
    # The tongues: rooted down in the hole, licking up between the straw, past the torn lips. Hot at the foot, gone at the tip.
    for name, base, h, w, lean, phase, colour in (
            ("flame-1", (0.008, -0.012), 0.168, 0.066, (0.006, -0.008), 0.00, "ff6ee8"),
            ("flame-2", (-0.038, 0.004), 0.142, 0.058, (-0.016, 0.002), 0.31, SPILL_HEX),
            ("flame-3", (0.044, 0.010), 0.150, 0.056, (0.018, 0.006), 0.57, SPILL_HEX),
            ("flame-4", (-0.018, -0.040), 0.124, 0.050, (-0.010, -0.014), 0.83, SPILL_HEX),
            ("flame-5", (0.030, -0.042), 0.130, 0.050, (0.012, -0.016), 0.12, "ff6ee8"),
            ("flame-6", (-0.006, 0.036), 0.150, 0.056, (-0.002, 0.012), 0.66, "ff6ee8"),
            ("flame-7", (-0.058, -0.022), 0.104, 0.042, (-0.022, -0.008), 0.44, SPILL_HEX)):
        foot = Vector((base[0], base[1], 0.706))
        tip = foot + Vector((lean[0], lean[1], h))
        for sd in (X, Y):
            pts = [foot - sd * (w * 0.5), foot + sd * (w * 0.5), tip + sd * (w * 0.06), tip - sd * (w * 0.06)]
            spill_poly("head", pts, sd.cross(Z), [1.0, 1.0, 0.0, 0.0], [(phase, 0.0), (phase, 0.0), (phase, 1.0), (phase, 1.0)], colour=colour)


def build_details(part):
    body = part.tree("body")
    build_rope(part, body)
    # THE FRONT SEAM: split down the belly to the rope, big X stitches across it. Off centre, so it reads hand-made.
    front = seam("torso", body, FACE, [(-0.006, 0.334), (-0.014, 0.312), (-0.003, 0.292), (-0.013, 0.272), (-0.004, 0.252), (-0.008, 0.234)],
                 [0.015, 0.022, 0.026, 0.022, 0.015], 0.040, 0.020)
    for k, (s, t, a1, a2) in enumerate(((0, 0.70, 40, -42), (3, 0.75, 38, -40))):
        stitch(part, "x-front-%da" % k, front[s], t, 0.076, 0.0135, 0.0085, a1)
        stitch(part, "x-front-%db" % k, front[s], t, 0.076, 0.0135, 0.0080, a2)
    # THE BELLY'S PATCHES: her magenta on its right, stitched, her crimson on its left.
    sewn_patch(part, "patch-belly", "torso", body, FACE, (-0.138, 0.290), [(-0.036, -0.032), (0.030, -0.036), (0.036, 0.028), (-0.030, 0.034)],
               "magenta", "magenta_dark", stitches=(1, 3), lift=0.009, turn=-8, one_normal=True)
    sewn_patch(part, "patch-belly-2", "torso", body, FACE, (0.128, 0.288), [(-0.024, -0.024), (0.026, -0.021), (0.023, 0.026), (-0.026, 0.021)],
               "crimson", "crimson_dark", stitches=(0, 2), lift=0.009, turn=10, one_normal=True)
    pin(part, "pin-belly", "torso", body, FACE, 0.166, 0.322, (0.62, -0.66, 0.42), 0.066, 0.046, "gem_gold", "gem_gold_lit")

    # THE BACK: split down it, ladder-stitched, a big crimson patch, a pin, a tuft of straw coming out of the seam.
    back = seam("torso", body, BACK, [(0.010, 0.330), (-0.008, 0.296), (0.010, 0.262), (-0.004, 0.226)], [0.015, 0.019, 0.015], 0.040, 0.020)
    for k, (s, t, a) in enumerate(((0, 0.5, 5), (1, 0.5, -5), (2, 0.5, 5))):
        stitch(part, "st-back-%d" % k, back[s], t, 0.072, 0.0130, 0.0085, a)
    sewn_patch(part, "patch-back", "torso", body, BACK, (0.120, 0.286), [(-0.048, -0.050), (0.046, -0.044), (0.050, 0.052), (-0.044, 0.046)],
               "crimson", "crimson_dark", stitches=(0, 1, 2, 3), turn=6)
    pin(part, "pin-back", "torso", body, BACK, -0.110, 0.300, (-0.52, 0.76, 0.38), 0.066, 0.046, "gem_crimson", "gem_crimson_lit")

    # THE LEGS: each split on its outside, one sewn patch on the left thigh.
    for side, bone, face in ((1, "leg-left", LEFT), (-1, "leg-right", RIGHT)):
        tree = part.tree("leg-%d" % side)
        s = seam(bone, tree, face, [(-0.004, 0.146), (0.008, 0.128), (-0.006, 0.112)], [0.009, 0.009], 0.030, 0.020)
        stitch(part, "st-leg-%d" % side, s[0], 0.9, 0.044, 0.0100, 0.0065, 6 * side)
    leg = part.tree("leg-1")
    sewn_patch(part, "patch-thigh", "leg-left", leg, FACE, (0.098, 0.128), [(-0.030, -0.022), (0.030, -0.026), (0.030, 0.024), (-0.030, 0.022)],
               "magenta", "magenta_dark", stitches=(0, 2), lift=0.006)

    # WHIP STITCHES where each mitten was sewn on, chunky and few.
    for side in (1, -1):
        fore = "forearm-left" if side > 0 else "forearm-right"
        mit = part.tree("mitten-%d" % side)
        for k, zz in enumerate((0.438, 0.400, 0.362)):
            p, n = on(mit, FACE, side * 0.452, zz)
            part.bar("mit-st-%d-%d" % (side, k), fore, p + Vector((-0.016, 0, 0)) + n * 0.002, p + Vector((0.016, 0, 0)) + n * 0.002, 0.0090, 0.006, n, flat("thread"))


# ---------------------------------------------------------------------------------------------------------------------
# THE FILE.
# ---------------------------------------------------------------------------------------------------------------------


def mesh_arrays(obj):
    """A cloth mesh as glTF arrays: +y up, -z the way Blender's -y faces... v flipped, up to four bones a vertex."""
    mesh = obj.data
    mesh.calc_loop_triangles()
    normals = mesh.corner_normals
    uvs = mesh.uv_layers[0].data
    mix_of = []
    for v in mesh.vertices:
        mix = sorted(((g.weight, ALL_BONES.index(obj.vertex_groups[g.group].name)) for g in v.groups if g.weight > 0.001), reverse=True)[:4]
        total = sum(wt for wt, _ in mix)
        mix_of.append([(b, wt / total) for wt, b in mix] + [(0, 0.0)] * (4 - len(mix)))
    seen, pos, nrm, uv, joints, weights, idx = {}, [], [], [], [], [], []
    for tri in mesh.loop_triangles:
        for loop in tri.loops:
            vi = mesh.loops[loop].vertex_index
            co = mesh.vertices[vi].co
            n = normals[loop].vector
            t = uvs[loop].uv
            key = (vi, round(n.x, 4), round(n.y, 4), round(n.z, 4), round(t.x, 6), round(t.y, 6))
            if key not in seen:
                seen[key] = len(pos)
                pos.append((co.x, co.z, -co.y))
                nrm.append((n.x, n.z, -n.y))
                uv.append((t.x, 1.0 - t.y))
                joints.append(tuple(b for b, _ in mix_of[vi]))
                weights.append(tuple(wt for _, wt in mix_of[vi]))
            idx.append(seen[key])
    return dict(pos=pos, nrm=nrm, uv=uv, joints=joints, weights=weights, idx=idx)


def light_arrays(light):
    return dict(pos=[(p.x, p.z, -p.y) for p in light.pos], nrm=[(n.x, n.z, -n.y) for n in light.nrm], uv=[tuple(u) for u in light.uv],
                joints=[(b, 0, 0, 0) for b in light.bone], weights=[(1.0, 0.0, 0.0, 0.0) for _ in light.bone], idx=list(light.idx),
                col=[tuple(c) for c in light.col])


def write_glb(meshes, out):
    """phaister-doll.glb with its four meshes swapped: skeleton, bind matrices, clips and soul nodes copied across."""
    gltf, buffer = bpv.read_glb(BASE)
    names = [gltf["nodes"][i]["name"] for i in gltf["skins"][0]["joints"]]
    if names != BONES:
        raise SystemExit("the base rig's joints are %s, not %s" % (names, BONES))
    blob = bytearray()
    views, accessors, remap = [], [], {}

    def align():
        while len(blob) % 4:
            blob.append(0)

    def keep(old):
        if old in remap:
            return remap[old]
        acc = dict(gltf["accessors"][old])
        data = bpv.accessor_bytes(gltf, buffer, old)
        align()
        start = len(blob)
        blob.extend(data)
        views.append({"buffer": 0, "byteOffset": start, "byteLength": len(data)})
        acc["bufferView"] = len(views) - 1
        acc.pop("byteOffset", None)
        accessors.append(acc)
        remap[old] = len(accessors) - 1
        return remap[old]

    def add(values, fmt, kind, component, minmax=False):
        align()
        start = len(blob)
        for v in values:
            blob.extend(struct.pack("<" + fmt * len(v), *v))
        acc = {"bufferView": len(views), "componentType": component, "count": len(values), "type": kind}
        if minmax:
            acc["min"] = [min(v[a] for v in values) for a in range(len(values[0]))]
            acc["max"] = [max(v[a] for v in values) for a in range(len(values[0]))]
        views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        accessors.append(acc)
        return len(accessors) - 1

    raw_binds = bpv.accessor_bytes(gltf, buffer, gltf["skins"][0]["inverseBindMatrices"])
    for anim in gltf["animations"]:
        for sampler in anim["samplers"]:
            sampler["input"] = keep(sampler["input"])
            sampler["output"] = keep(sampler["output"])

    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    skin = gltf["skins"][0]
    raw = struct.unpack("<%df" % (16 * len(BONES)), raw_binds)
    matrices = [raw[k * 16:(k + 1) * 16] for k in range(len(BONES))]
    for name, parent, side in EXTRA_BONES:
        world = (side * ELBOW_X, ARM_Z, 0.0)
        pw = [-matrices[BONES.index(parent)][12 + a] for a in range(3)]
        gltf["nodes"].append({"name": name, "translation": [world[a] - pw[a] for a in range(3)]})
        gltf["nodes"][node_of[parent]].setdefault("children", []).append(len(gltf["nodes"]) - 1)
        skin["joints"].append(len(gltf["nodes"]) - 1)
        matrices.append((1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -world[0], -world[1], -world[2], 1))
    binds = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)
    for other in gltf["skins"]:
        other["joints"] = list(skin["joints"])
        other["inverseBindMatrices"] = binds

    by_name = {m.get("name"): m for m in gltf["meshes"]}
    for name, m in meshes.items():
        attrs = {
            "POSITION": add(m["pos"], "f", "VEC3", 5126, minmax=True),
            "NORMAL": add(m["nrm"], "f", "VEC3", 5126),
            "TEXCOORD_0": add(m["uv"], "f", "VEC2", 5126),
            "JOINTS_0": add(m["joints"], "H", "VEC4", 5123),
            "WEIGHTS_0": add(m["weights"], "f", "VEC4", 5126),
        }
        if "col" in m:
            attrs["COLOR_0"] = add(m["col"], "f", "VEC4", 5126)
        by_name[name]["primitives"] = [{"attributes": attrs, "indices": add([(i,) for i in m["idx"]], "I", "SCALAR", 5125), "material": 0, "mode": 4}]

    gltf["accessors"] = accessors
    gltf["bufferViews"] = views
    gltf["buffers"] = [{"byteLength": len(blob)}]
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (phaister doll)"}
    gltf["extras"] = {"prototype": "doll-redesign-20261031", "replaces": "nothing; not loaded by the game"}
    gltf["images"] = [{"uri": tex.ATLAS_NAME, "name": NAME + "-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}]
    gltf["samplers"] = [{"magFilter": 9729, "minFilter": 9987}]
    gltf["materials"][0]["name"] = NAME
    gltf["materials"][0]["doubleSided"] = False
    gltf["nodes"][0]["name"] = INTERNAL_NAME
    gltf["scenes"][0]["name"] = INTERNAL_NAME
    for attempt in range(5):
        try:
            bpv.write_glb(out, gltf, blob)
            break
        except OSError:
            time.sleep(1.0)
    else:
        raise SystemExit("could not write " + out)


def verify(objects):
    lo, hi = Vector((1e9, 1e9, 1e9)), Vector((-1e9, -1e9, -1e9))
    tris = 0
    for obj in objects:
        obj.data.calc_loop_triangles()
        tris += len(obj.data.loop_triangles)
        print("%s: %d triangles" % (obj.name, len(obj.data.loop_triangles)))
        if obj.name in ("body-mesh", "head-mesh"):
            for v in obj.data.vertices:
                for a in range(3):
                    lo[a] = min(lo[a], v.co[a])
                    hi[a] = max(hi[a], v.co[a])
    print("cloth bounds  x %.3f..%.3f  y %.3f..%.3f  z %.3f..%.3f" % (lo.x, hi.x, lo.y, hi.y, lo.z, hi.z))
    print("triangles %d  (budget %d, v11 is 17846)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=BASE, bone_heuristic="BLENDER")
    armature = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    for obj in [o for o in bpy.data.objects if o.type == "MESH"]:
        bpy.data.objects.remove(obj)
    armature.name = INTERNAL_NAME
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode="EDIT")
    for name, parent, side in EXTRA_BONES:
        eb = armature.data.edit_bones.new(name)
        eb.head = (side * ELBOW_X, 0.0, ARM_Z)
        eb.tail = (side * (ELBOW_X + 0.06), 0.0, ARM_Z)
        eb.parent = armature.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    if armature.animation_data:
        armature.animation_data.action = None

    image_path = os.path.join(FOLDER, tex.ATLAS_NAME)
    if not os.path.exists(image_path):
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_phaister_doll_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])

    body, head = Part("body-mesh"), Part("head-mesh")
    build_body(body)
    build_leg(body, 1, "leg-left", "b")
    build_leg(body, -1, "leg-right", "a")
    build_arm(body, 1)
    build_arm(body, -1)
    build_head(head)
    build_details(body)
    build_face(head, head.tree("head"))
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        obj = part.to_object(armature)
        obj.data.materials.append(material)
        objects.append(obj)
        print(part.name, " ".join("%s:%d" % (n, t) for n, t in part.pieces))
    lights = [GLOW.to_object(armature), SPILL.to_object(armature)]
    verify(objects + lights)
    meshes = {"body-mesh": mesh_arrays(objects[0]), "head-mesh": mesh_arrays(objects[1]),
              "glow-mesh": light_arrays(GLOW), "spill-mesh": light_arrays(SPILL)}
    write_glb(meshes, OUT)
    print("wrote " + OUT)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
