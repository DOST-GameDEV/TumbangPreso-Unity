"""PAYO night fires (2026-10-19): the carved stone fire pedestal and the standing bamboo torch. Blender 5.0, headless.

  blender -b --python tools/payo_pieces/fire_kit.py -- <ABS out folder>
      writes build/fire_pedestal_raw.glb and build/fire_torch_raw.glb (fire_run.sh then adds the normal map)

Owner, 2026-10-19, of the Payo map at night: "could also use some torch lights or those stone pedestals with fire in
them". Pieces first (map-art rule): each is built and rendered on its own before anything is placed.

  fire_pedestal   a squat Ifugao stone fire stand: a two-step square plinth, a round column with a carved band (the
                  walls' fret and diamond, carved proud), and a wide bowl with the copings' woven band round its rim,
                  full of coals. 1.12 m across, the coals 1.10 m up (the flame stands on them: `CoalTop` below).
  fire_torch      a standing torch: a bamboo pole with its nodes, planted in a squat river-stone foot, two red rattan
                  bindings and a split-cane basket head of coals at 1.96 m.

Both are lathes (rounded, smooth-shaded, a hard edge only where the profile turns sharply), one material
`payo_fire_kit` on the one atlas fire_paint.py paints (fire_layout.py's regions). A few hundred triangles each. The
flame is NOT in these models: the game draws it as its own shader-animated mesh on the coals (PayoFireFlame.shader).
Blender x east, y north, z up; origin at the centre of the foot.
"""
import math
import os
import sys

import bmesh
import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import fire_layout as L  # noqa: E402

OUT = sys.argv[sys.argv.index("--") + 1]
BUILD = os.path.join(OUT, "build")
os.makedirs(BUILD, exist_ok=True)
ATLAS = os.path.join(OUT, "fire_kit-atlas.png")

# The flame's foot on each piece (metres up from the origin): the builder reads the same numbers (PayoSceneBuilder.Fire.cs).
COAL_TOP = {"fire_pedestal": 1.10, "fire_torch": 1.96}


class MB:
    """A mesh in the making: faces with their own UVs, smooth by default."""

    def __init__(self, name):
        self.name, self.v, self.f, self.uv, self.flat = name, [], [], [], []

    def face(self, pts, uvs, flat=False):
        i = len(self.v)
        self.v.extend(pts)
        self.f.append(tuple(range(i, i + len(pts))))
        self.uv.append(uvs)
        self.flat.append(flat)

    def lathe(self, profile, sides, turn=0.0, seam_u=1.0, cap=None, flat=False):
        """profile: [(radius, z, region)], bottom to top; each band between two points takes the UPPER point's region,
        its fv from the band's place in that region's run. `cap`: (z, region) closes the top with a fan to the axis
        (a raised centre makes a mound). Faces point outward."""
        runs = {}
        for k in range(1, len(profile)):
            reg = profile[k][2]
            runs.setdefault(reg, []).append(k)
        span = {}
        for reg, ks in runs.items():
            z0 = min(profile[k - 1][1] for k in ks)
            z1 = max(profile[k][1] for k in ks)
            span[reg] = (z0, max(z1, z0 + 1e-4))
        for k in range(1, len(profile)):
            (r0, z0, _), (r1, z1, reg) = profile[k - 1], profile[k]
            lo, hi = span[reg]
            f0, f1 = (z0 - lo) / (hi - lo), (z1 - lo) / (hi - lo)
            if abs(z1 - z0) < 1e-5:                           # a flat ring (a step's tread): fv runs from out to in
                f0, f1 = 0.0, 1.0
            for s in range(sides):
                a0, a1 = turn + 2 * math.pi * s / sides, turn + 2 * math.pi * (s + 1) / sides
                u0, u1 = s / sides * seam_u, (s + 1) / sides * seam_u
                p = [(r0 * math.cos(a0), r0 * math.sin(a0), z0), (r0 * math.cos(a1), r0 * math.sin(a1), z0),
                     (r1 * math.cos(a1), r1 * math.sin(a1), z1), (r1 * math.cos(a0), r1 * math.sin(a0), z1)]
                uvs = [L.uv(reg, u0, f0), L.uv(reg, u1, f0), L.uv(reg, u1, f1), L.uv(reg, u0, f1)]
                self.face(p, uvs, flat)
        if cap:
            zc, reg = cap
            r, z, _ = profile[-1]
            for s in range(sides):
                a0, a1 = turn + 2 * math.pi * s / sides, turn + 2 * math.pi * (s + 1) / sides
                p = [(r * math.cos(a0), r * math.sin(a0), z), (r * math.cos(a1), r * math.sin(a1), z), (0.0, 0.0, zc)]
                uvs = [L.uv(reg, 0.5 + 0.5 * math.cos(a0), 0.5 + 0.5 * math.sin(a0)),
                       L.uv(reg, 0.5 + 0.5 * math.cos(a1), 0.5 + 0.5 * math.sin(a1)), L.uv(reg, 0.5, 0.5)]
                self.face(p, uvs, flat)

    def make(self, mat, sharp_deg=38.0):
        me = bpy.data.meshes.new(self.name)
        me.from_pydata(self.v, [], self.f)
        me.update()
        uvl = me.uv_layers.new(name="UVMap")
        li = 0
        for poly, uvs in zip(me.polygons, self.uv):
            for k in range(poly.loop_total):
                uvl.data[poly.loop_start + k].uv = uvs[k]
            li += poly.loop_total
        for poly, fl in zip(me.polygons, self.flat):
            poly.use_smooth = not fl
        bm = bmesh.new()
        bm.from_mesh(me)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        for e in bm.edges:                               # hard edges where the profile turns sharply
            if len(e.link_faces) == 2 and e.calc_face_angle(0) > math.radians(sharp_deg):
                e.smooth = False
            if len(e.link_faces) != 2:
                e.smooth = False
        bm.to_mesh(me)
        bm.free()
        me.materials.append(mat)
        ob = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(ob)
        return ob


def material():
    m = bpy.data.materials.new("payo_fire_kit")
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(ATLAS)
    tex.image.name = "fire_kit-atlas"
    nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = 0.9
    return m


def pedestal(mat):
    mb = MB("fire_pedestal")
    q = math.pi / 4                                       # a square from a four-sided lathe: corners on the diagonals
    d = math.sqrt(2.0)
    # the two-step plinth, sunk 4 cm into the ground, a soft chamfer on each step's top edge
    mb.lathe([(0.56 * d, -0.04, "stone"), (0.56 * d, 0.12, "stone"), (0.52 * d, 0.16, "stone"),
              (0.47 * d, 0.16, "flat")], 4, q, flat=True)
    mb.lathe([(0.47 * d, 0.16, "stone"), (0.47 * d, 0.25, "stone"), (0.43 * d, 0.28, "stone"),
              (0.25 * d, 0.28, "flat")], 4, q, flat=True)
    # the column and the bowl: one round lathe, eight sides (the ink and the smooth shading round it off)
    sides = 8
    mb.lathe([
        (0.40, 0.28, "stone"), (0.34, 0.34, "stone"), (0.33, 0.48, "stone"),
        (0.365, 0.50, "band"), (0.365, 0.66, "band"),
        (0.33, 0.68, "stone"), (0.32, 0.84, "stone"),
        (0.38, 0.89, "stone"), (0.47, 0.97, "stone"), (0.545, 1.05, "stone"),
        (0.56, 1.07, "weave"), (0.56, 1.16, "weave"),
        (0.51, 1.19, "flat"), (0.47, 1.15, "flat"), (0.45, 1.07, "flat"),
    ], sides, math.pi / sides, cap=(1.13, "coal"))
    return mb.make(mat)


def torch(mat):
    mb = MB("fire_torch")
    # the foot: a squat stone, six-sided, wider at the bottom
    mb.lathe([(0.24, -0.04, "river"), (0.25, 0.06, "river"), (0.19, 0.17, "river"), (0.10, 0.22, "river")], 6, 0.3,
             cap=(0.22, "river"))
    # the pole: bamboo with three nodes
    pole = [(0.052, 0.14, "bamboo")]
    for z0 in (0.62, 1.10):
        pole += [(0.046, z0 - 0.02, "bamboo"), (0.053, z0, "bamboo"), (0.046, z0 + 0.02, "bamboo")]
    pole += [(0.044, 1.62, "bamboo")]
    mb.lathe(pole, 6, 0.0)
    # two red bindings under the head
    for z in (1.52, 1.60):
        mb.lathe([(0.044, z - 0.02, "binding"), (0.058, z - 0.012, "binding"), (0.058, z + 0.012, "binding"),
                  (0.044, z + 0.02, "binding")], 6, 0.5)
    # the head: a split-cane basket flaring out, its rim rolled, coals inside
    mb.lathe([(0.044, 1.62, "basket"), (0.075, 1.70, "basket"), (0.115, 1.83, "basket"), (0.14, 1.93, "basket"),
              (0.146, 1.97, "binding"), (0.125, 1.99, "binding"), (0.115, 1.95, "basket")], 7, 0.0, cap=(1.97, "coal"))
    return mb.make(mat)


def export(ob, path):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_yup=True,
                              export_apply=True, export_normals=True, export_texcoords=True, export_materials="EXPORT",
                              export_image_format="AUTO")
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    print("FIRE KIT %s: %d triangles, %d verts -> %s" % (ob.name, tris, len(ob.data.vertices), path))


bpy.ops.wm.read_factory_settings(use_empty=True)
mat = material()
export(pedestal(mat), os.path.join(BUILD, "fire_pedestal_raw.glb"))
export(torch(mat), os.path.join(BUILD, "fire_torch_raw.glb"))
