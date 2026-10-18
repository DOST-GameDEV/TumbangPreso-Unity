"""ICE CRYSTALS, sculpted and baked: three single crystals and one cluster, for effects that need real ice.

  py -3 tools/build_ice_crystals.py forms  v1          the carved forms only, in clay, to look at the cuts
  py -3 tools/build_ice_crystals.py build  v1          carve, sculpt, bake, paint, write the models, then review
  py -3 tools/build_ice_crystals.py paint  v2          repaint the maps from the bakes already on disk, then review
  py -3 tools/build_ice_crystals.py review v2          only the turnaround sheet

WHY IT EXISTS. The owner read an article on making a stylised crystal (sculpt a high detail crystal, bake it down to
a low one, paint the maps from the bakes), asked why our ice was not made that way, and said: "try making an ice
crystals". The ice this game had (Cheska's) is flat typed facets with a painted atlas, and it reads as uniformly pale
frosted plastic: nothing in it knows where the crystal is thin and where it is deep. A bake does know. So this builds
each crystal twice:

  * the FORM: a stretched solid carved by TYPED plane cuts, one line a cut, chosen by hand for each crystal. Big flat
    faces of different sizes, an off centre tip, some edges softened by a typed bevel and the rest left crisp, chips
    cut out of typed edges. This is the low model the game draws.
  * the SCULPT: the same form remeshed fine, its planes made a little uneven, a fine grain laid on it and then partly
    flattened again on the main planes (so a face stays a face), and typed cracks cut into it. Nobody draws this one.
    It is baked onto the form: a tangent normal map, an edge mask at two widths, ambient occlusion, and THICKNESS (how
    much crystal lies behind each point of the surface), which is what lets the shader light the thin parts through.

Then the system's Python paints three maps from the bakes (see `paint`) and Blender renders a turnaround with a
material that imitates `Resources/Shaders/IceCrystal.shader`. It IMITATES it: say "seen in Blender only" of that sheet.

These are our own crystals. The article gave the method; no silhouette here is taken from its crystal.

Everything is typed and seeded, so two runs carve the same forms (the same triangle and vertex counts). They do NOT
write the same bytes: measured on 2026-10-10, two runs of one builder lay the atlas out a little differently (most on
the cluster) and bake with slightly different noise. In the game the two are the same crystal to the eye. So keep the
model and its three maps from ONE run together, never a map from another run. Metres, z up in here, y up in the .glb.

Maps, 1024 square, beside each model in Assets/TumbangPreso/Resources/Models/IceCrystals:
  <name>_albedo.png   RGB the paint (sRGB), A the glint's break-up
  <name>_normal.png   RGB tangent normal (y up), linear
  <name>_mask.png     R glow-through, G the soft wide edge, B the thin sharp edge, A thickness (1 is deep), linear
"""
import json
import math
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
OUT = ROOT / "Assets" / "TumbangPreso" / "Resources" / "Models" / "IceCrystals"
LOGS = ROOT / "Logs" / "ice-crystals"
WORK = LOGS / "bake"
BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe"
SIZE = 1024

# ---------------------------------------------------------------------------------------------------------------------
# THE CRYSTALS. A cut is (name, azimuth, elevation, distance, about x, about y, about z): the plane whose outward normal
# points along that azimuth and elevation (degrees), standing `distance` out from the `about` point; everything beyond
# it is carved away. A soft edge is (cut, cut, width, segments): the edge those two faces share is bevelled. A chip is
# (cut, cut, how far along the edge those two faces share, size, stretch along the edge): a faceted bite out of that
# edge, as ice chips, not a pocket in a face. A crack is a typed run of points near the surface.
# ---------------------------------------------------------------------------------------------------------------------

SPIKE = dict(
    cuts=[
        # the long sides, unevenly spaced round it and each leaning in by its own amount
        ("s1", 8, 4, 0.200, 0, 0, 0.60),
        ("s2", 62, 7, 0.172, 0, 0, 0.60),
        ("s3", 118, 3, 0.214, 0, 0, 0.60),
        ("s4", 171, 9, 0.160, 0, 0, 0.60),
        ("s5", 233, 5, 0.205, 0, 0, 0.60),
        ("s6", 296, 6, 0.178, 0, 0, 0.60),
        # one long slice down a side and one across the belly: the two big planes that stop it being a pencil
        ("long", 92, 13, 0.150, 0, 0, 0.95),
        ("belly", 318, -11, 0.186, 0, 0, 0.50),
        # the foot
        ("f1", 35, -32, 0.190, 0, 0, 0.25),
        ("f2", 150, -41, 0.170, 0, 0, 0.25),
        ("f3", 265, -28, 0.200, 0, 0, 0.25),
        ("f4", 330, -52, 0.158, 0, 0, 0.25),
        ("under", 200, -80, 0.235, 0, 0, 0.25),
        # the shoulders, about a point pushed to one side so the tip is not over the middle
        ("u1", 22, 11, 0.150, 0.035, -0.020, 1.02),
        ("u2", 141, 24, 0.120, 0.035, -0.020, 1.20),
        ("u3", 256, 15, 0.134, 0.035, -0.020, 1.20),
        # the tip, chopped twice
        ("t1", 84, 47, 0.050, 0.060, -0.030, 1.50),
        ("t2", 312, 38, 0.046, 0.060, -0.030, 1.52),
    ],
    soft=[("s1", "s2", 0.016, 2), ("s3", "s4", 0.022, 3), ("s5", "u3", 0.014, 2), ("f1", "s1", 0.012, 2),
          ("long", "u2", 0.010, 2), ("s6", "belly", 0.018, 2)],
    chips=[("s4", "s5", 0.45, 0.070, 1.5), ("s2", "long", 0.62, 0.052, 1.3), ("s1", "belly", 0.50, 0.060, 1.4),
           ("u1", "s1", 0.45, 0.044, 1.2), ("s5", "s6", 0.22, 0.056, 1.6)],
    cracks=[[(0.20, 0.02, 1.05), (0.21, -0.03, 0.86), (0.19, 0.01, 0.70), (0.22, -0.06, 0.52), (0.20, -0.02, 0.36)],
            [(0.21, -0.03, 0.86), (0.16, -0.13, 0.80), (0.10, -0.18, 0.70)],
            [(-0.16, 0.10, 1.10), (-0.19, 0.06, 0.92), (-0.15, 0.13, 0.76), (-0.18, 0.09, 0.60)]],
    voxel=0.0040,
)

SHARD = dict(
    cuts=[
        # a slab: wide one way, thin the other, eight uneven sides
        ("e", 8, 5, 0.300, 0, 0, 0.40),
        ("ne", 62, 2, 0.215, 0, 0, 0.40),
        ("n", 97, 8, 0.168, 0, 0, 0.40),
        ("nw", 148, 4, 0.235, 0, 0, 0.40),
        ("w", 188, 7, 0.290, 0, 0, 0.40),
        ("sw", 240, 3, 0.205, 0, 0, 0.40),
        ("s", 275, 9, 0.172, 0, 0, 0.40),
        ("se", 322, 5, 0.240, 0, 0, 0.40),
        ("f1", 30, -36, 0.200, 0, 0, 0.20),
        ("f2", 160, -30, 0.220, 0, 0, 0.20),
        ("f3", 280, -42, 0.170, 0, 0, 0.20),
        ("under", 100, -84, 0.200, 0, 0, 0.20),
        # a chisel, not a point: one broad slanted cut and three lesser ones against it
        ("chisel", 15, 38, 0.150, 0.050, 0, 0.72),
        ("back", 185, 58, 0.130, 0.050, 0, 0.72),
        ("lip", 100, 50, 0.125, 0.050, 0, 0.72),
        ("near", 270, 44, 0.130, 0.050, 0, 0.72),
        # one slice off a shoulder
        ("nick", 330, 22, 0.262, 0, 0, 0.55),
    ],
    soft=[("e", "ne", 0.022, 3), ("w", "sw", 0.018, 2), ("chisel", "e", 0.014, 2), ("back", "w", 0.016, 2),
          ("f2", "nw", 0.014, 2)],
    chips=[("ne", "n", 0.55, 0.075, 1.5), ("s", "se", 0.35, 0.066, 1.4), ("chisel", "near", 0.50, 0.054, 1.3),
           ("w", "nw", 0.30, 0.060, 1.6), ("back", "lip", 0.50, 0.046, 1.2)],
    cracks=[[(0.30, 0.02, 0.66), (0.31, 0.07, 0.52), (0.29, 0.03, 0.40), (0.31, 0.09, 0.26)],
            [(0.29, 0.03, 0.40), (0.25, -0.12, 0.36), (0.21, -0.22, 0.30)],
            [(-0.20, 0.20, 0.62), (-0.24, 0.14, 0.48), (-0.20, 0.21, 0.34)]],
    voxel=0.0040,
)

STUB = dict(
    cuts=[
        ("s1", 0, 12, 0.230, 0, 0, 0.22),
        ("s2", 70, 5, 0.200, 0, 0, 0.22),
        ("s3", 140, 15, 0.240, 0, 0, 0.22),
        ("s4", 215, 8, 0.210, 0, 0, 0.22),
        ("s5", 290, 10, 0.222, 0, 0, 0.22),
        ("f1", 30, -40, 0.160, 0, 0, 0.12),
        ("f2", 150, -35, 0.170, 0, 0, 0.12),
        ("f3", 270, -45, 0.150, 0, 0, 0.12),
        ("under", 60, -86, 0.120, 0, 0, 0.12),
        ("top1", 100, 36, 0.122, 0.040, -0.030, 0.30),
        ("top2", 230, 31, 0.130, 0.040, -0.030, 0.30),
        ("top3", 345, 42, 0.112, 0.040, -0.030, 0.30),
        ("cap", 180, 66, 0.060, 0.040, -0.030, 0.42),
        ("slice", 252, 24, 0.196, 0, 0, 0.26),
    ],
    soft=[("s1", "s2", 0.020, 3), ("s3", "top1", 0.014, 2), ("s5", "slice", 0.012, 2), ("top2", "top3", 0.012, 2)],
    chips=[("s1", "s2", 0.50, 0.058, 1.5), ("top1", "top3", 0.55, 0.042, 1.3), ("s3", "s4", 0.40, 0.050, 1.4),
           ("s5", "s1", 0.30, 0.050, 1.5)],
    cracks=[[(0.22, 0.00, 0.40), (0.23, -0.05, 0.30), (0.21, -0.01, 0.20), (0.23, -0.06, 0.12)],
            [(-0.10, 0.18, 0.42), (-0.14, 0.17, 0.30), (-0.09, 0.20, 0.20)]],
    voxel=0.0035,
)

# Two forms only the cluster has: a leaning mid spike with a split tip, and a nub.
LEAN = dict(
    cuts=[
        ("s1", 25, 6, 0.150, 0, 0, 0.45),
        ("s2", 100, 3, 0.128, 0, 0, 0.45),
        ("s3", 160, 8, 0.152, 0, 0, 0.45),
        ("s4", 228, 4, 0.136, 0, 0, 0.45),
        ("s5", 300, 9, 0.146, 0, 0, 0.45),
        ("f1", 60, -36, 0.130, 0, 0, 0.15),
        ("f2", 190, -42, 0.120, 0, 0, 0.15),
        ("f3", 310, -33, 0.134, 0, 0, 0.15),
        ("under", 0, -88, 0.150, 0, 0, 0.15),
        ("u1", 55, 20, 0.098, -0.020, 0.025, 0.86),
        ("u2", 180, 27, 0.090, -0.020, 0.025, 0.86),
        ("u3", 292, 18, 0.102, -0.020, 0.025, 0.86),
        ("t1", 130, 52, 0.030, -0.030, 0.035, 1.00),
        ("split", 10, 33, 0.066, -0.020, 0.025, 0.98),
    ],
    soft=[("s1", "s2", 0.014, 2), ("s4", "s5", 0.016, 2), ("u1", "split", 0.010, 2)],
    chips=[("s2", "s3", 0.55, 0.050, 1.5), ("s5", "s1", 0.35, 0.044, 1.4), ("u2", "s3", 0.50, 0.034, 1.2)],
    cracks=[[(0.15, 0.04, 0.80), (0.16, 0.00, 0.64), (0.14, 0.05, 0.50), (0.16, 0.01, 0.34)]],
    voxel=0.0040,
)

NUB = dict(
    cuts=[
        ("s1", 10, 14, 0.150, 0, 0, 0.14),
        ("s2", 95, 8, 0.130, 0, 0, 0.14),
        ("s3", 170, 16, 0.156, 0, 0, 0.14),
        ("s4", 262, 10, 0.138, 0, 0, 0.14),
        ("f1", 45, -45, 0.110, 0, 0, 0.08),
        ("f2", 200, -40, 0.115, 0, 0, 0.08),
        ("under", 0, -88, 0.080, 0, 0, 0.08),
        ("top1", 60, 44, 0.094, 0.010, 0, 0.24),
        ("top2", 205, 50, 0.088, 0.010, 0, 0.24),
        ("top3", 320, 38, 0.100, 0.010, 0, 0.24),
    ],
    soft=[("s1", "s2", 0.014, 2), ("top1", "top3", 0.010, 2)],
    chips=[("s2", "s3", 0.50, 0.042, 1.4), ("top1", "top2", 0.50, 0.032, 1.2)],
    cracks=[[(0.14, -0.02, 0.26), (0.15, 0.02, 0.18), (0.14, -0.02, 0.10)]],
    voxel=0.0035,
)

# The cluster: each piece is (form, where its foot stands, lean about x and about y in degrees, turn about z, scale).
# Set out by hand: the tall one off centre and leaning away, a second leaning the other way and lower, the chunk
# low in front, and the small ones filling the gaps on different sides, none of them a mirror of another.
CLUSTER_PIECES = [
    ("SPIKE", (-0.060, 0.050, -0.20), (6, -7), 40, 1.00),
    ("LEAN", (0.250, -0.020, -0.14), (-9, 21), 200, 1.12),
    ("SHARD", (0.060, -0.290, -0.20), (22, 8), 115, 0.78),
    ("LEAN", (-0.330, -0.100, -0.12), (12, -27), 320, 0.84),
    ("STUB", (-0.170, 0.330, -0.10), (-24, -12), 75, 0.92),
    ("NUB", (0.330, 0.270, -0.06), (-20, 26), 10, 1.10),
    ("NUB", (0.360, -0.300, -0.05), (18, 30), 250, 0.86),
]

FORMS = {"SPIKE": SPIKE, "SHARD": SHARD, "STUB": STUB, "LEAN": LEAN, "NUB": NUB}
SINGLES = [("ice_spike", "SPIKE"), ("ice_shard", "SHARD"), ("ice_stub", "STUB")]
NAMES = [n for n, _ in SINGLES] + ["ice_cluster"]

# The look, shared by `IceCrystal.shader` (its property defaults are these same numbers) and the review material.
# Colours are sRGB, as a Unity colour property is typed.
LOOK = dict(
    key=(-0.62, 0.60, -0.22),        # toward the key light in Unity's axes: high, to the left, a little in front
    deep=(0.012, 0.050, 0.200),      # the navy a face turned away, and a deep part looked into, goes to
    shade=0.40,
    deep_amount=0.50,
    lit_colour=(0.42, 0.80, 1.00),   # the pale frost only the brightest step is pushed toward
    lit_amount=0.42,
    depth=0.70,
    glow_deep=(0.00, 0.56, 1.00),    # the saturated aqua of light coming through
    glow_hot=(0.62, 0.98, 1.00),     # and where it is strongest
    glow=1.35, glow_base=0.40, glow_back=0.80, glow_through=0.80,
    edge_soft=(0.10, 0.62, 0.95), edge_sharp=(0.72, 0.97, 1.00),
    edge_soft_amount=0.42, edge_sharp_amount=0.95,
    rim=(0.10, 0.70, 1.00), rim_amount=0.55,
    glint=(0.90, 1.00, 1.00), glint_power=56.0, glint_amount=0.60,
    bump=0.8,
)


# =====================================================================================================================
# THE BLENDER HALF
# =====================================================================================================================

def in_blender():
    import bmesh
    import bpy
    import numpy as np
    from mathutils import Euler, Matrix, Vector, noise
    from mathutils.bvhtree import BVHTree

    argv = sys.argv[sys.argv.index("--") + 1:]
    stage, tag = argv[0], argv[1]
    WORK.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)

    def direction(az, el):
        a, e = math.radians(az), math.radians(el)
        return Vector((math.cos(e) * math.cos(a), math.cos(e) * math.sin(a), math.sin(e)))

    def linear(c):
        return tuple(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c)

    def activate(ob):
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob

    def to_object(name, mesh):
        ob = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(ob)
        return ob

    def apply_modifier(ob, kind, **settings):
        mod = ob.modifiers.new(kind.lower(), kind)
        for key, value in settings.items():
            setattr(mod, key, value)
        activate(ob)
        bpy.ops.object.modifier_apply(modifier=mod.name)

    # ---- the form --------------------------------------------------------------------------------------------------
    def carve(name, recipe):
        """The typed cuts, the typed bevels and the typed chips. A closed, flat faced solid."""
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        bmesh.ops.scale(bm, vec=(4.0, 4.0, 8.0), verts=bm.verts)
        cut = bm.faces.layers.int.new("cut")
        for f in bm.faces:
            f[cut] = 0
        index = {}
        for i, (label, az, el, d, cx, cy, cz) in enumerate(recipe["cuts"], 1):
            index[label] = i
            n = direction(az, el)
            p = Vector((cx, cy, cz)) + n * d
            res = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-6,
                                         plane_co=p, plane_no=n, clear_outer=True, clear_inner=False)
            edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
            if not edges:
                print("WARNING %s: cut %s touches nothing" % (name, label))
                continue
            for f in bmesh.ops.edgeloop_fill(bm, edges=edges)["faces"]:
                f[cut] = i
        left = sum(1 for f in bm.faces if f[cut] == 0)
        if left:
            print("WARNING %s: %d faces of the starting block were never carved away" % (name, left))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

        def shared(a, b):
            want = {index[a], index[b]}
            return [e for e in bm.edges if len(e.link_faces) == 2 and {e.link_faces[0][cut], e.link_faces[1][cut]} == want]

        # Where each chip bites is read off the edges BEFORE any is bevelled, so a chip can sit on a softened edge too.
        bites = []
        for k, (a, b, along, size, stretch) in enumerate(recipe["chips"]):
            edges = shared(a, b)
            if not edges:
                print("WARNING %s: no edge between %s and %s to chip" % (name, a, b))
                continue
            edge = max(edges, key=lambda e: e.calc_length())
            lower, upper = sorted((v.co.copy() for v in edge.verts), key=lambda c: c.z)
            run = (upper - lower).normalized()
            out = (edge.link_faces[0].normal + edge.link_faces[1].normal).normalized()
            # A different twist for each chip, so no two bites are cut by the same tool held the same way.
            twist = Matrix.Rotation(math.radians(37.0 * (k + 1)), 3, run)
            frame = twist @ Matrix((run, out, run.cross(out))).transposed()
            bites.append((lower.lerp(upper, along) + out * size * 0.42, frame, size, stretch))
        for a, b, width, segments in recipe["soft"]:
            edges = shared(a, b)
            if not edges:
                print("WARNING %s: no edge between %s and %s to soften" % (name, a, b))
                continue
            made = bmesh.ops.bevel(bm, geom=edges, offset=width, offset_type="OFFSET", segments=segments, profile=0.5,
                                   affect="EDGES", clamp_overlap=True)
            for f in made["faces"]:
                f[cut] = -1
        mesh = bpy.data.meshes.new(name + "_form")
        bm.to_mesh(mesh)
        bm.free()
        ob = to_object(name + "_form", mesh)
        for k, (centre, frame, size, stretch) in enumerate(bites):
            # The tool is a twenty sided ball, stretched along the edge: what it leaves is a faceted scoop.
            block = bmesh.new()
            bmesh.ops.create_icosphere(block, subdivisions=1, radius=size)
            bmesh.ops.scale(block, vec=(stretch, 1.0, 0.8), verts=block.verts)
            bmesh.ops.rotate(block, cent=(0, 0, 0), matrix=frame, verts=block.verts)
            bmesh.ops.translate(block, vec=centre, verts=block.verts)
            cm = bpy.data.meshes.new("chip")
            block.to_mesh(cm)
            block.free()
            cutter = to_object("chip", cm)
            before, kept = len(ob.data.polygons), ob.data.copy()
            apply_modifier(ob, "BOOLEAN", operation="DIFFERENCE", solver="EXACT", object=cutter)
            if len(ob.data.polygons) < 4:
                # The solver has been seen to hand back nothing at all. A chip is not worth a crystal.
                print("WARNING %s: chip %d emptied the form and was left out" % (name, k))
                ob.data = kept
            elif len(ob.data.polygons) == before:
                print("WARNING %s: chip %d cut nothing" % (name, k))
            bpy.data.objects.remove(cutter)
        return ob

    def crack_points(recipe, matrix=None):
        runs = []
        for run in recipe["cracks"]:
            pts = [Vector(p) for p in run]
            runs.append([matrix @ p for p in pts] if matrix is not None else pts)
        return runs

    def piece_matrix(foot, lean, turn, scale):
        return (Matrix.Translation(foot) @ Euler((math.radians(lean[0]), math.radians(lean[1]), 0.0)).to_matrix().to_4x4()
                @ Matrix.Rotation(math.radians(turn), 4, "Z") @ Matrix.Scale(scale, 4))

    def cluster_form():
        """The pieces, each carved as itself, stood where it is typed and joined into one closed solid."""
        whole, cracks = None, []
        for k, (form, foot, lean, turn, scale) in enumerate(CLUSTER_PIECES):
            ob = carve("cluster_%d" % k, FORMS[form])
            m = piece_matrix(foot, lean, turn, scale)
            ob.data.transform(m)
            cracks += crack_points(FORMS[form], m)
            if whole is None:
                whole = ob
                continue
            apply_modifier(whole, "BOOLEAN", operation="UNION", solver="EXACT", object=ob)
            bpy.data.objects.remove(ob)
        whole.name = "ice_cluster_form"
        return whole, cracks

    # ---- the sculpt ------------------------------------------------------------------------------------------------
    def sculpt(name, form, voxel, cracks, seed):
        """The form remeshed fine, made uneven, grained, partly flattened on its planes, and cracked."""
        high = form.copy()
        high.data = form.data.copy()
        high.name = name + "_high"
        bpy.context.scene.collection.objects.link(high)
        apply_modifier(high, "REMESH", mode="VOXEL", voxel_size=voxel, use_smooth_shade=True)
        me = high.data
        count = len(me.vertices)
        dg = bpy.context.evaluated_depsgraph_get()
        tree = BVHTree.FromObject(form, dg)
        co = np.empty(count * 3, dtype=np.float64)
        no = np.empty(count * 3, dtype=np.float64)
        me.vertices.foreach_get("co", co)
        me.vertices.foreach_get("normal", no)
        co, no = co.reshape(-1, 3), no.reshape(-1, 3)
        push = np.zeros(count)
        off = Vector((seed * 3.7, seed * 1.3, seed * 2.1))
        for i in range(count):
            p, n = Vector(co[i]), Vector(no[i])
            hit = tree.find_nearest(p)
            # How squarely this point sits on one of the form's planes: 1 in the middle of a face, 0 on an edge.
            flat = 0.0
            if hit[0] is not None:
                flat = min(1.0, max(0.0, (n.dot(hit[1]) - 0.955) / 0.04))
            # The plane itself is not quite flat: a slow swell, as a hand cut plane is.
            swell = noise.noise(p * 2.6 + off) * 0.0070
            # Shallow chisel marks: the ridges between cells.
            cells = noise.voronoi(p * 9.0 + off, distance_metric="DISTANCE", exponent=2.5)[0]
            marks = (cells[1] - cells[0]) * 0.0040 - 0.0012
            grain = noise.fractal(p * 34.0 + off, 1.0, 2.0, 3) * 0.0018
            # PARTLY flattened on the main planes: the grain stays strong along the edges, which is where ice is rough.
            push[i] = swell + (marks + grain) * (1.0 - 0.60 * flat)
        # The cracks: each typed run is cut finer, wandered a little, laid on the surface and pressed in as a groove.
        for k, run in enumerate(cracks):
            fine = []
            for a, b in zip(run[:-1], run[1:]):
                steps = max(2, int((b - a).length / 0.012))
                for s in range(steps):
                    p = a.lerp(b, s / steps)
                    p = p + Vector(noise.noise_vector(p * 26.0 + off)) * 0.008
                    hit = tree.find_nearest(p)
                    fine.append(np.array(hit[0] if hit[0] is not None else p))
            hit = tree.find_nearest(run[-1])
            fine.append(np.array(hit[0] if hit[0] is not None else run[-1]))
            fine = np.array(fine)
            lo, hi = fine.min(0) - 0.03, fine.max(0) + 0.03
            near = np.where(np.all((co > lo) & (co < hi), axis=1))[0]
            if len(near) == 0:
                continue
            pts = co[near]
            best = np.full(len(near), 1e9)
            along = np.zeros(len(near))
            total = len(fine) - 1
            for s in range(total):
                a, b = fine[s], fine[s + 1]
                ab = b - a
                t = np.clip(((pts - a) @ ab) / max(1e-12, ab @ ab), 0.0, 1.0)
                d = np.linalg.norm(pts - (a + t[:, None] * ab), axis=1)
                closer = d < best
                best[closer] = d[closer]
                along[closer] = (s + t[closer]) / total
            # A crack is widest in its middle and dies to a hair at both ends.
            taper = np.clip(np.minimum(along, 1.0 - along) * 5.0, 0.15, 1.0)
            width = 0.0085 * taper
            groove = np.clip(1.0 - best / width, 0.0, 1.0) ** 1.4
            push[near] -= groove * 0.0075 * taper
        co += no * push[:, None]
        me.vertices.foreach_set("co", co.reshape(-1))
        me.update()
        return high

    # ---- the low model ---------------------------------------------------------------------------------------------
    def lowpoly(name, form, open_below=None):
        low = form.copy()
        low.data = form.data.copy()
        low.name = name
        low.data.name = name
        bpy.context.scene.collection.objects.link(low)
        bm = bmesh.new()
        bm.from_mesh(low.data)
        if open_below is not None:
            # A cluster stands on the ground: what is under it is never seen and is not drawn.
            bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-6, plane_co=(0, 0, open_below),
                                   plane_no=(0, 0, -1), clear_outer=True, clear_inner=False)
        bmesh.ops.dissolve_degenerate(bm, dist=1e-5, edges=bm.edges[:])
        bmesh.ops.triangulate(bm, faces=bm.faces[:])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        bm.to_mesh(low.data)
        bm.free()
        me = low.data
        for poly in me.polygons:
            poly.use_smooth = True
        # Crisp where the form turns sharply, smooth across a bevel's steps: that is the "few softened, few crisp".
        me.set_sharp_from_angle(angle=math.radians(27.0))
        for edge in me.edges:
            edge.use_seam = edge.use_edge_sharp
        me.uv_layers.new(name="UVMap")
        activate(low)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.unwrap(method="ANGLE_BASED", margin=0.012)
        bpy.ops.uv.select_all(action="SELECT")
        bpy.ops.uv.pack_islands(margin=0.012)
        bpy.ops.object.mode_set(mode="OBJECT")
        return low

    # ---- the bakes -------------------------------------------------------------------------------------------------
    def emit_material(name, build):
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        nt = mat.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        em = nt.nodes.new("ShaderNodeEmission")
        nt.links.new(em.outputs[0], out.inputs["Surface"])
        combine = nt.nodes.new("ShaderNodeCombineXYZ")
        nt.links.new(combine.outputs[0], em.inputs["Color"])
        r, g, b = build(nt)
        for socket, value in zip(combine.inputs, (r, g, b)):
            nt.links.new(value, socket)
        return mat

    def ao_node(nt, distance, inside, samples=12):
        node = nt.nodes.new("ShaderNodeAmbientOcclusion")
        node.inside, node.samples, node.only_local = inside, samples, True
        node.inputs["Distance"].default_value = distance
        return node.outputs["AO"]

    def edge_node(nt, radius, gain):
        """How far the surface's direction, averaged over `radius`, has turned from the direction right here."""
        geo = nt.nodes.new("ShaderNodeNewGeometry")
        bevel = nt.nodes.new("ShaderNodeBevel")
        bevel.samples = 16
        bevel.inputs["Radius"].default_value = radius
        dot = nt.nodes.new("ShaderNodeVectorMath")
        dot.operation = "DOT_PRODUCT"
        nt.links.new(bevel.outputs[0], dot.inputs[0])
        nt.links.new(geo.outputs["Normal"], dot.inputs[1])
        turn = nt.nodes.new("ShaderNodeMath")
        turn.operation = "SUBTRACT"
        turn.inputs[0].default_value = 1.0
        nt.links.new(dot.outputs["Value"], turn.inputs[1])
        scale = nt.nodes.new("ShaderNodeMath")
        scale.operation = "MULTIPLY"
        scale.use_clamp = True
        nt.links.new(turn.outputs[0], scale.inputs[0])
        scale.inputs[1].default_value = gain
        return scale.outputs[0]

    def noise_node(nt, scale, detail, distortion, offset):
        coord = nt.nodes.new("ShaderNodeTexCoord")
        move = nt.nodes.new("ShaderNodeVectorMath")
        move.operation = "ADD"
        nt.links.new(coord.outputs["Object"], move.inputs[0])
        move.inputs[1].default_value = offset
        tex = nt.nodes.new("ShaderNodeTexNoise")
        tex.inputs["Scale"].default_value = scale
        tex.inputs["Detail"].default_value = detail
        tex.inputs["Distortion"].default_value = distortion
        nt.links.new(move.outputs[0], tex.inputs["Vector"])
        return tex.outputs[0]

    def bake_all(name, low, high, seed):
        zs = [v.co.z for v in low.data.vertices]
        z0, z1 = min(zs), max(zs)

        def surface(nt):       # R ambient occlusion, G the edge over a wide radius, B the edge over a thin one
            return ao_node(nt, 0.22, False), edge_node(nt, 0.050, 5.0), edge_node(nt, 0.0140, 9.0)

        def inside(nt):        # R thickness over a long reach, G height, B thickness over a short reach
            coord = nt.nodes.new("ShaderNodeTexCoord")
            split = nt.nodes.new("ShaderNodeSeparateXYZ")
            nt.links.new(coord.outputs["Object"], split.inputs[0])
            span = nt.nodes.new("ShaderNodeMapRange")
            span.inputs["From Min"].default_value, span.inputs["From Max"].default_value = z0, z1
            nt.links.new(split.outputs["Z"], span.inputs["Value"])
            return ao_node(nt, 0.30, True, 16), span.outputs[0], ao_node(nt, 0.10, True, 16)

        def paint(nt):         # R big slow blotches, G painterly patches, B a fine speckle for the glint
            o = (seed * 1.7, seed * 0.9, seed * 2.3)
            return noise_node(nt, 2.4, 1.5, 0.6, o), noise_node(nt, 6.5, 2.0, 1.8, o), noise_node(nt, 30.0, 3.0, 0.4, o)

        target = bpy.data.materials.new(name + "_target")
        target.use_nodes = True
        node = target.node_tree.nodes.new("ShaderNodeTexImage")
        target.node_tree.nodes.active = node
        low.data.materials.clear()
        low.data.materials.append(target)
        scene = bpy.context.scene
        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = 32
        scene.cycles.use_denoising = False
        scene.cycles.seed = 7
        bake = scene.render.bake
        bake.margin, bake.margin_type = 8, "EXTEND"
        bake.use_selected_to_active, bake.use_clear = True, True
        # ⚠️ THE CAGE. Rays leave along the low model's normals, and at a crisp edge those are split in two, so the
        # rays fan apart and the sculpt's edge, the very thing the edge masks are for, falls in the gap: it baked as a
        # dotted line. A copy of the low model with every edge smooth, swollen a little, has no gaps.
        cage = low.copy()
        cage.data = low.data.copy()
        cage.name = name + "_cage"
        bpy.context.scene.collection.objects.link(cage)
        for edge in cage.data.edges:
            edge.use_edge_sharp = False
        cage.data.update()
        for vert in cage.data.vertices:
            vert.co = vert.co + vert.normal * 0.022
        cage.hide_render = True
        bake.use_cage, bake.cage_object = True, cage
        bake.cage_extrusion, bake.max_ray_distance = 0.0, 0.0
        bake.normal_space = "TANGENT"
        for label, kind, build in (("normal", "NORMAL", None), ("surface", "EMIT", surface), ("inside", "EMIT", inside),
                                   ("paint", "EMIT", paint)):
            image = bpy.data.images.new(name + "_" + label, SIZE, SIZE, alpha=True, float_buffer=True)
            image.colorspace_settings.name = "Non-Color"
            node.image = image
            high.data.materials.clear()
            if build is not None:
                high.data.materials.append(emit_material(name + "_" + label, build))
            bpy.ops.object.select_all(action="DESELECT")
            high.select_set(True)
            low.select_set(True)
            bpy.context.view_layer.objects.active = low
            bpy.ops.object.bake(type=kind)
            px = np.empty(SIZE * SIZE * 4, dtype=np.float32)
            image.pixels.foreach_get(px)
            np.save(str(WORK / (name + "_" + label + ".npy")), px.reshape(SIZE, SIZE, 4)[::-1])
            print("baked", name, label)
        low.data.materials.clear()
        plain = bpy.data.materials.new("ice")
        low.data.materials.append(plain)

    def export(name, low):
        activate(low)
        bpy.ops.export_scene.gltf(filepath=str(OUT / (name + ".glb")), export_format="GLB", use_selection=True,
                                  export_tangents=True, export_yup=True, export_animations=False)
        tris = len(low.data.polygons)
        box = [list(low.bound_box[0]), list(low.bound_box[6])]
        top = max(low.data.vertices, key=lambda v: v.co.z).co
        json.dump({"triangles": tris, "vertices": len(low.data.vertices), "box": box, "tip": list(top)},
                  open(WORK / (name + ".json"), "w"))
        print("wrote", name, tris, "triangles")

    def forms():
        made = []
        for k, (name, form) in enumerate(SINGLES):
            made.append((name, carve(name, FORMS[form]), crack_points(FORMS[form]), FORMS[form]["voxel"], None, k + 1))
        whole, cracks = cluster_form()
        made.append(("ice_cluster", whole, cracks, 0.0050, 0.0, 9))
        return made

    # ---- looking at it ---------------------------------------------------------------------------------------------
    VIEWS = [("front", 0, 10), ("quarter", 45, 10), ("side", 90, 10), ("back", 180, 10), ("top", 20, 68)]

    def studio():
        scene = bpy.context.scene
        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = 12
        scene.cycles.use_denoising = False
        scene.view_settings.view_transform = "Standard"
        scene.render.film_transparent = False
        world = bpy.data.worlds.new("dark")
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs[0].default_value = (0.018, 0.020, 0.026, 1)
        scene.world = world
        cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
        scene.collection.objects.link(cam)
        scene.camera = cam
        cam.data.type = "ORTHO"
        return scene, cam

    def shoot(scene, cam, ob, prefix, width, height):
        pts = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
        lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
        hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
        mid, tall, wide = (lo + hi) * 0.5, hi.z - lo.z, max(hi.x - lo.x, hi.y - lo.y)
        scene.render.resolution_x, scene.render.resolution_y = width, height
        cam.data.ortho_scale = max(wide * 1.25 * max(1.0, width / height), (tall + wide * 0.4) * 1.12 * max(1.0, width / height))
        for label, yaw, pitch in VIEWS:
            # The crystal turns and the lens and the light stay, as they do in the game's film.
            ob.rotation_mode = "XYZ"
            ob.rotation_euler = (0, 0, math.radians(yaw))
            e = math.radians(pitch)
            cam.location = mid + Vector((0, -math.cos(e), math.sin(e))) * 6.0
            cam.rotation_euler = (math.radians(90) - e, 0, 0)
            scene.render.filepath = str(WORK / ("%s_%s.png" % (prefix, label)))
            bpy.ops.render.render(write_still=True)
        ob.rotation_euler = (0, 0, 0)

    def game_material(name):
        """`IceCrystal.shader`, written again in nodes. Emission only: nothing here is ray traced through the crystal."""
        mat = bpy.data.materials.new(name + "_game")
        mat.use_nodes = True
        nt = mat.node_tree
        nt.nodes.clear()
        L = LOOK

        def image(suffix, colour):
            node = nt.nodes.new("ShaderNodeTexImage")
            node.image = bpy.data.images.load(str(OUT / (name + suffix)))
            node.image.colorspace_settings.name = "sRGB" if colour else "Non-Color"
            # The glint and the thickness ride in alpha and are not a coverage.
            node.image.alpha_mode = "CHANNEL_PACKED"
            return node

        def put(socket, value):
            if hasattr(value, "is_output"):
                nt.links.new(value, socket)
            elif isinstance(value, (tuple, list)):
                socket.default_value = tuple(value)[:len(socket.default_value)] if hasattr(socket.default_value, "__len__") else value[0]
            else:
                socket.default_value = value

        def m(op, a, b=0.0, c=0.0, clamp=False):
            node = nt.nodes.new("ShaderNodeMath")
            node.operation, node.use_clamp = op, clamp
            put(node.inputs[0], a), put(node.inputs[1], b), put(node.inputs[2], c)
            return node.outputs[0]

        def v(op, a, b=(0, 0, 0), scale=None):
            node = nt.nodes.new("ShaderNodeVectorMath")
            node.operation = op
            put(node.inputs[0], a)
            put(node.inputs[1], b)
            if scale is not None:
                put(node.inputs["Scale"], scale)
            return node.outputs["Value"] if op in ("DOT_PRODUCT", "LENGTH") else node.outputs[0]

        def step(lo, hi, x):
            node = nt.nodes.new("ShaderNodeMapRange")
            node.interpolation_type = "SMOOTHSTEP"
            put(node.inputs["Value"], x)
            node.inputs["From Min"].default_value, node.inputs["From Max"].default_value = lo, hi
            return node.outputs[0]

        def mix(a, b, f):      # a + (b - a) * f, on vectors
            return v("ADD", a, v("SCALE", v("SUBTRACT", b, a), scale=f))

        def sat(x):
            return m("MAXIMUM", m("MINIMUM", x, 1.0), 0.0)

        albedo, mask, bump = image("_albedo.png", True), image("_mask.png", False), image("_normal.png", False)
        split = nt.nodes.new("ShaderNodeSeparateXYZ")
        nt.links.new(mask.outputs["Color"], split.inputs[0])
        glow, wide, thin, thick = split.outputs[0], split.outputs[1], split.outputs[2], mask.outputs["Alpha"]
        nmap = nt.nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = L["bump"]
        nt.links.new(bump.outputs["Color"], nmap.inputs["Color"])
        geo = nt.nodes.new("ShaderNodeNewGeometry")
        N, V = nmap.outputs[0], geo.outputs["Incoming"]
        kx, ky, kz = L["key"]
        key = Vector((kx, kz, ky)).normalized()          # Unity's y up to Blender's z up; the lens looks along +y here
        ndl = v("DOT_PRODUCT", N, tuple(key))
        top, mid = step(0.58, 0.68, ndl), step(0.18, 0.28, ndl)
        lit = m("ADD", m("ADD", m("MULTIPLY", step(-0.25, -0.15, ndl), 0.35), m("MULTIPLY", mid, 0.35)),
                m("MULTIPLY", top, 0.30))
        deep = linear(L["deep"])
        col = v("SCALE", albedo.outputs["Color"], scale=m("ADD", L["shade"], m("MULTIPLY", lit, 1.0 - L["shade"])))
        col = mix(col, deep, m("MULTIPLY", m("SUBTRACT", 1.0, lit), L["deep_amount"]))
        col = mix(col, linear(L["lit_colour"]), m("MULTIPLY", m("ADD", m("MULTIPLY", mid, 0.3), m("MULTIPLY", top, 0.7)), L["lit_amount"]))
        facing = sat(v("DOT_PRODUCT", N, V))
        col = mix(col, tuple(c * 0.6 for c in deep), m("MULTIPLY", m("MULTIPLY", thick, m("MULTIPLY", facing, facing)), L["depth"]))
        back = sat(m("ADD", m("MULTIPLY", v("DOT_PRODUCT", V, tuple(-key)), 0.5), 0.5))
        through = sat(m("ADD", m("MULTIPLY", ndl, -0.5), 0.5))
        fres = m("POWER", m("SUBTRACT", 1.0, facing), 3.0)
        g = m("MULTIPLY", glow, m("ADD", m("ADD", L["glow_base"], m("MULTIPLY", m("MULTIPLY", back, back), L["glow_back"])),
                                  m("MULTIPLY", through, L["glow_through"])))
        g = m("ADD", g, m("MULTIPLY", m("MULTIPLY", fres, L["rim_amount"]), m("SUBTRACT", 1.0, m("MULTIPLY", thick, 0.6))))
        glow_colour = mix(linear(L["glow_deep"]), linear(L["glow_hot"]), step(0.80, 1.50, g))
        col = v("ADD", col, v("SCALE", glow_colour, scale=m("MULTIPLY", g, L["glow"])))
        col = v("ADD", col, v("SCALE", linear(L["edge_soft"]), scale=m("MULTIPLY", m("MULTIPLY", wide, L["edge_soft_amount"]),
                                                                          m("ADD", 0.6, m("MULTIPLY", lit, 0.4)))))
        col = v("ADD", col, v("SCALE", linear(L["edge_sharp"]), scale=m("MULTIPLY", m("MULTIPLY", thin, L["edge_sharp_amount"]),
                                                                           m("ADD", 0.55, m("MULTIPLY", lit, 0.45)))))
        half = v("NORMALIZE", v("ADD", V, tuple(key)))
        spec = m("POWER", sat(v("DOT_PRODUCT", N, half)), L["glint_power"])
        glint = step(0.42, 0.50, m("MULTIPLY", spec, m("ADD", 0.35, albedo.outputs["Alpha"])))
        col = v("ADD", col, v("SCALE", linear(L["glint"]), scale=m("MULTIPLY", glint, L["glint_amount"])))
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        em = nt.nodes.new("ShaderNodeEmission")
        nt.links.new(col, em.inputs["Color"])
        nt.links.new(em.outputs[0], out.inputs["Surface"])
        return mat

    bpy.ops.wm.read_factory_settings(use_empty=True)
    if stage == "forms":
        made = forms()
        scene, cam = studio()
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
        sun.data.energy, sun.data.angle = 3.0, math.radians(8)
        sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
        scene.collection.objects.link(sun)
        fill = bpy.data.objects.new("fill", bpy.data.lights.new("fill", "SUN"))
        fill.data.energy, fill.data.color = 0.8, (0.6, 0.8, 1.0)
        fill.rotation_euler = (math.radians(70), 0, math.radians(150))
        scene.collection.objects.link(fill)
        clay = bpy.data.materials.new("clay")
        clay.use_nodes = True
        clay.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.45, 0.6, 0.75, 1)
        for name, form, cracks, voxel, cut, seed in made:
            for other in made:
                other[1].hide_render = other[1] is not form
            form.data.materials.append(clay)
            print("FORM", name, len(form.data.polygons), "faces")
            shoot(scene, cam, form, "forms_" + name, 360, 480 if name != "ice_cluster" else 400)
    elif stage == "bake":
        only = argv[2:]
        for name, form, cracks, voxel, cut, seed in forms():
            if only and name not in only:
                continue
            high = sculpt(name, form, voxel, cracks, seed)
            low = lowpoly(name, form, cut)
            form.hide_render = True
            print("SCULPT", name, len(high.data.polygons), "faces; low", len(low.data.polygons), "triangles")
            bake_all(name, low, high, seed)
            export(name, low)
            high.hide_render = True
            low.hide_render = True
    elif stage == "review":
        scene, cam = studio()
        for name in NAMES:
            before = set(bpy.data.objects)
            bpy.ops.import_scene.gltf(filepath=str(OUT / (name + ".glb")))
            ob = [o for o in set(bpy.data.objects) - before if o.type == "MESH"][0]
            ob.data.materials.clear()
            ob.data.materials.append(game_material(name))
            for other in bpy.data.objects:
                if other.type == "MESH":
                    other.hide_render = other is not ob
            shoot(scene, cam, ob, "review_" + name, 400, 560 if name != "ice_cluster" else 440)


# =====================================================================================================================
# THE PAINTING HALF (the system's Python)
# =====================================================================================================================

def paint(name):
    """The three maps, painted from the bakes. Every mask is levelled against its own crystal's range."""
    import cv2
    import numpy as np
    from PIL import Image

    def load(label):
        return np.load(str(WORK / (name + "_" + label + ".npy"))).astype(np.float32)

    def levels(x, lo, hi, gamma=1.0):
        return np.clip((x - lo) / max(1e-6, hi - lo), 0.0, 1.0) ** gamma

    def smooth(x):
        return x * x * (3.0 - 2.0 * x)

    def blur(x, sigma):
        return cv2.GaussianBlur(x, (0, 0), sigma)

    def spread(x, where, lo, hi):
        a, b = np.percentile(x[where], [lo, hi])
        return levels(x, a, b)

    def mixc(a, b, f):
        return a + (np.array(b, dtype=np.float32) - a) * f[..., None]

    normal, surface, inside, noise = load("normal"), load("surface"), load("inside"), load("paint")
    # What the bake reached (its margin included). The noise is never exactly nothing where it was written.
    cover = noise[..., 0] > 1e-4
    ao = np.clip(blur(surface[..., 0], 1.0), 0, 1)
    wide_raw, thin_raw = surface[..., 1], surface[..., 2]
    thick_long, height, thick_short = blur(inside[..., 0], 1.6), inside[..., 1], blur(inside[..., 2], 1.2)
    blotch, patch, speck = spread(noise[..., 0], cover, 2, 98), spread(noise[..., 1], cover, 2, 98), spread(noise[..., 2], cover, 2, 98)

    # THICKNESS: 1 where a lot of crystal lies behind the surface.
    thick = spread(0.65 * thick_long + 0.35 * thick_short, cover, 6, 94)
    # GLOW-THROUGH: the thin parts, pushed apart from the thick ones so there is no grey between, lifted at the tip and
    # broken by the big blotches so it is not an even film.
    thin_part = smooth(levels(1.0 - thick, 0.46, 0.96))
    tip = levels(height, 0.70, 1.0, 1.6)
    foot = levels(1.0 - height, 0.86, 1.0, 1.5)
    glow = np.clip(thin_part * (0.55 + 0.70 * blotch) + 0.62 * tip + 0.30 * foot, 0.0, 1.0) ** 1.25
    glow[~cover] = 0.0

    # EDGES. The wide pass is soft; the thin pass is cut hard so the grain does not speckle it.
    wide = smooth(levels(blur(wide_raw, 2.0), 0.03, 0.55))
    thin = smooth(levels(blur(thin_raw, 0.8), 0.035, 0.30))

    # ALBEDO. A gradient up the crystal, big blotches over it, painterly patches lighter and darker, the deep parts
    # darker still, a little frost where the edges are.
    navy, low_blue, high_blue = (0.020, 0.075, 0.250), (0.035, 0.200, 0.560), (0.085, 0.400, 0.800)
    aqua, pale = (0.150, 0.560, 0.900), (0.640, 0.880, 0.980)
    col = np.array(low_blue, dtype=np.float32)[None, None, :] + (np.array(high_blue, dtype=np.float32) - np.array(low_blue, dtype=np.float32)) * smooth(height)[..., None]
    col = col * (0.55 + 0.90 * blotch)[..., None]
    lighter = smooth(levels(patch, 0.60, 0.68))
    darker = smooth(levels(1.0 - patch, 0.62, 0.70))
    col = mixc(col, aqua, 0.55 * lighter)
    col = mixc(col, navy, 0.36 * darker)
    col = mixc(col, navy, 0.50 * smooth(thick))
    col = col * (0.50 + 0.50 * ao)[..., None]
    col = mixc(col, pale, np.clip(0.26 * wide + 0.30 * thin, 0, 1))
    # THE GLINT'S BREAK-UP: patches of yes and no with a speckled border, never a clean bar.
    glint = smooth(levels(0.30 * speck + 0.70 * patch, 0.50, 0.60))

    def save(path, planes):
        arr = (np.clip(np.dstack(planes), 0, 1) * 255.0 + 0.5).astype(np.uint8)
        Image.fromarray(arr, "RGBA" if arr.shape[2] == 4 else "RGB").save(path)

    save(OUT / (name + "_albedo.png"), [col[..., 0], col[..., 1], col[..., 2], glint])
    nrm = normal[..., :3].copy()
    nrm[~(normal[..., 3] > 0.5)] = (0.5, 0.5, 1.0)
    save(OUT / (name + "_normal.png"), [nrm[..., 0], nrm[..., 1], nrm[..., 2]])
    save(OUT / (name + "_mask.png"), [glow, wide, thin, thick])
    # What each map looks like alone, for the contact sheet: a flat grey thickness or a speckled edge means a bad bake.
    tiles = [col, np.dstack([glint] * 3), nrm, np.dstack([thick] * 3), np.dstack([glow] * 3), np.dstack([wide] * 3),
             np.dstack([thin] * 3), np.dstack([ao] * 3)]
    return [cv2.resize((np.clip(t, 0, 1) * 255).astype(np.uint8)[..., ::-1], (320, 320), interpolation=cv2.INTER_AREA) for t in tiles]


def join(prefix, rows, columns, out, labels=True):
    import cv2
    import numpy as np
    sheet = []
    for row in rows:
        ims = []
        for column in columns:
            im = cv2.imread(str(WORK / ("%s_%s_%s.png" % (prefix, row, column))))
            if labels:
                cv2.putText(im, row.replace("ice_", "") + " " + column, (8, 20), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (235, 235, 235), 1)
            ims.append(im)
        sheet.append(ims)
    width = max(sum(im.shape[1] for im in ims) for ims in sheet)
    lines = []
    for ims in sheet:
        line = cv2.hconcat(ims)
        if line.shape[1] < width:
            line = cv2.copyMakeBorder(line, 0, 0, 0, width - line.shape[1], cv2.BORDER_CONSTANT, value=(6, 5, 5))
        lines.append(line)
    cv2.imwrite(str(out), cv2.vconcat(lines), [cv2.IMWRITE_JPEG_QUALITY, 90])
    print("wrote", out)


def blender(*words):
    done = subprocess.run([BLENDER, "-b", "--python", str(Path(__file__).resolve()), "--"] + list(words),
                          capture_output=True, text=True)
    keep = [l for l in done.stdout.splitlines() if l.startswith(("WARNING", "FORM", "SCULPT", "baked", "wrote", "Error", "Traceback"))]
    print("\n".join(keep))
    if done.returncode != 0 or "Traceback" in done.stdout + done.stderr:
        print(done.stdout[-3000:], done.stderr[-3000:])
        raise SystemExit("Blender failed at " + " ".join(words))


def main():
    import cv2
    stage = sys.argv[1] if len(sys.argv) > 1 else "build"
    tag = sys.argv[2] if len(sys.argv) > 2 else "v1"
    only = sys.argv[3:]
    LOGS.mkdir(parents=True, exist_ok=True)
    views = ["front", "quarter", "side", "back", "top"]
    if stage == "forms":
        blender("forms", tag)
        join("forms", NAMES, views, LOGS / ("forms_%s.jpg" % tag))
        return
    if stage == "build":
        blender("bake", tag, *only)
    if stage in ("build", "paint"):
        rows = [cv2.hconcat(paint(name)) for name in NAMES]
        cv2.imwrite(str(LOGS / ("maps_%s.jpg" % tag)), cv2.vconcat(rows), [cv2.IMWRITE_JPEG_QUALITY, 88])
        print("wrote", LOGS / ("maps_%s.jpg" % tag), "(albedo, glint, normal, thickness, glow, wide edge, thin edge, occlusion)")
    blender("review", tag)
    join("review", NAMES, views, LOGS / ("sheet_%s.jpg" % tag))
    for name in NAMES:
        info = json.load(open(WORK / (name + ".json")))
        print(name, info["triangles"], "triangles,", info["vertices"], "vertices")


if __name__ == "__main__":
    try:
        import bpy  # noqa: F401
        inside_blender = True
    except ImportError:
        inside_blender = False
    if inside_blender:
        in_blender()
    else:
        main()
