"""Model the pavement JUMP PAD and export it for the game (Runtime/Map/JumpPad.cs).

  py -3 tools/author_jump_pad_textures.py                       # paint the atlas first
  blender -b --python tools/author_jump_pad.py -- [--preview DIR N]

Writes ArtSource/ilalim/jump_pad.blend and Assets/TumbangPreso/Resources/Map/JumpPad/jump_pad.glb,
then reads the .glb back and prints each node's bounds in Unity's axes. With --preview it also
writes versioned renders DIR/jump_pad_<shot>_vN.png (an existing file is never overwritten).

WHY (owner, 2026-10-04, on the pad JumpPad.cs used to build from flat unlit quads): "it looks
flat, doesnt have shading, texture or any of that stylized character to it", and the ask: "can you
fix up a better model for the jump pads something like this with animations". The sketch is a
square pad on the ground (an orange fill inside a white outline frame), two yellow chevrons
stacked over its middle pointing up, and ghostly white square outlines rising off it.

THE PROP is a street launcher a barangay might bolt to a pavement: a heavy rubber surround with
the sketch's white outline PAINTED on it, a fat amber cushion tucked into it, and two kinds of
floating marker that the game animates. Four objects, each its own node in the .glb, each with its
ORIGIN where the animation needs it:

  pad_base     origin on the ground at the centre. A rounded-square rubber tray 1.70 m square,
               sunk 1 cm, its edge a 45 degree CHAMFER (6 cm in, 6 cm up) so a body steps onto it,
               a flat top at 0.085 from half 0.79 in to the well at half 0.585, and a bolt head in
               each corner at (+-0.715, +-0.715) topping out at 0.100. The white outline is the
               painted band at half 0.605..0.695.
  pad_cushion  origin at its own base centre, which sits on the well floor at z 0.02. A pillow
               1.15 m square: a 3 cm wall, then a dome rising to 0.095 (0.115 above the ground,
               the prop's highest point). The game squashes and springs it along +Z about its
               origin, so it flattens INTO the tray and never floats off it.
  pad_chevron  origin at its centre. One chunky "^" standing upright in XZ, 0.70 wide, 0.42
               tall, arms 0.15 thick, 0.12 deep. Its FACE looks along -Y, which is +Z in Unity,
               the side the game turns toward the camera. The game instances it twice.
  pad_ring     origin at its centre. One rounded-square OUTLINE frame lying flat, half
               0.605..0.695 (exactly the painted band, so a ring lifts off as if the paint itself
               rose), 6 cm thick. The game instances it three times.

THE HOUSE STYLE is the prop kit's (tools/author_ilalim_props.py; fillet() and the lighting come
from tools/author_ilalim_lrt.py): chunky and rounded, one object per part, a live Bevel modifier
with hardened normals, no two surfaces sharing a plane, painted detail instead of fiddly geometry.

THE UVS. One atlas, jump_pad_paint.png (tools/author_jump_pad_textures.py; REGIONS and EXTENT
below repeat its numbers). Every part is a stack of rounded-square rings, and each ring carries
the half-width its vertices take IN THE DRAWING: the top is a plain top-down projection, and a
wall's lower ring is pushed OUTWARD in the drawing by the wall's height, so walls unfold like a
box lid's flaps with no seam for the bevel to smear. The chevron's face is a front projection.

AXES. Blender is Z-up and the exporter writes glTF's Y-up; glTFast then negates X. A Blender
point (x, y, z) is Unity (-x, z, -y): the pad lies flat with +Y up and the chevron stands
upright. Nothing is rotated or scaled at object level, so the meshes alone carry the shapes and
JumpPad.cs takes only the meshes.

ROLE HUES (Art_Direction.md section 1): the sketch's orange sits on the attackers' #f87020, so the
cushion is a golden AMBER (0.97, 0.70, 0.07); nothing here is near the taya's #0080e8.
"""
import json
import math
import struct
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_ilalim_lrt import fillet                     # noqa: E402  (read-only: the corner arcs)

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURE = SOURCE / "textures" / "jump_pad_paint.png"
RUNTIME = ROOT / "Assets" / "TumbangPreso" / "Resources" / "Map" / "JumpPad"

# The atlas, in image pixels, top-left origin (same numbers as the texture script).
SIZE = 1024
REGIONS = {
    "cushion": (0, 0, 512, 512),
    "base": (512, 0, 1024, 512),
    "chevron": (0, 512, 512, 832),
    "ring": (512, 512, 1024, 1024),
    "bolt": (0, 832, 192, 1024),
}
EXTENT = {"cushion": 0.66, "base": 0.94, "ring": 0.78, "bolt": 0.048}
CHEVRON_PX_PER_M = 640.0

# What JumpPad.cs repeats: where the cushion sits and where a ring starts.
CUSHION_SEAT = 0.02
BASE_TOP = 0.085
RING_THICK = 0.06


def region_uv(part, mx, my):
    """Metres from a part's centre, top-down (+y up the image), to atlas UV."""
    x0, y0, x1, y1 = REGIONS[part]
    half = (x1 - x0) / 2
    px = (x0 + x1) / 2 + mx / EXTENT[part] * half
    py = (y0 + y1) / 2 - my / EXTENT[part] * half
    return (px / SIZE, 1 - py / SIZE)


def chevron_uv(x, z):
    x0, y0, x1, y1 = REGIONS["chevron"]
    px = (x0 + x1) / 2 + x * CHEVRON_PX_PER_M
    py = (y0 + y1) / 2 - z * CHEVRON_PX_PER_M
    return (px / SIZE, 1 - py / SIZE)


def square(half, r, seg=5):
    return fillet([(-half, -half), (half, -half), (half, half), (-half, half)], r, seg)


# ------------------------------------------------------------------ materials

# name: emission strength, the painted colour times this (IlalimPainted's "emission x colour").
# THE PAD GLOWS (owner, 2026-10-04: "jump pad needs to be glowy btw"). Because the glow is the
# drawing's own colour, the drawing is its own mask: the cream chevrons and the amber burn, the
# charcoal rubber stays matte, and the frame's white outline only smoulders. These are the idle
# values JumpPad.cs pulses about; it sets its own per part at runtime, the .glb's are not read.
MATERIALS = {"jump_pad_paint": 0.22, "jump_pad_cushion": 0.55, "jump_pad_chevron": 1.0, "jump_pad_ring": 0.8}


def material(name, strength=None):
    """A part's material; with `strength`, a review copy of it glowing that much instead."""
    base = name
    if strength is not None:
        name = f"review_{name}_{strength:.2f}"
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.85
    bsdf.inputs["Metallic"].default_value = 0.0
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(str(TEXTURE), check_existing=True)
    links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(tex.outputs["Color"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = MATERIALS[base] if strength is None else strength
    m.diffuse_color = (0.8, 0.6, 0.2, 1)
    return m


# ------------------------------------------------------------------ geometry

class Part:
    """One object's geometry in a bmesh, with its atlas UVs written as it is built."""

    def __init__(self, name):
        self.name, self.bm = name, bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")

    def stack(self, part, rings, center=(0.0, 0.0), turn=0.0, close_uv=None):
        """Loft rounded-square rings, each (half, corner radius, z, half IN THE DRAWING), capped
        at both ends. With `close_uv` (inner, outer drawing halves) the last ring joins the first
        instead: a closed frame, whose joining band takes those halves. `turn` (degrees) turns
        the drawing about the centre, so four bolts do not show four identical slots."""
        cx, cy = center
        c, s = math.cos(math.radians(turn)), math.sin(math.radians(turn))
        verts, uvs = [], []
        for half, r, z, drawn in rings:
            prof = square(half, r) if r > 0 else disc(half)
            verts.append([self.bm.verts.new((cx + x, cy + y, z)) for x, y in prof])
            k = drawn / half
            uvs.append([region_uv(part, (x * c - y * s) * k, (x * s + y * c) * k) for x, y in prof])
        n = len(verts[0])
        faces = []

        def quad(i0, i1, uv0=None, uv1=None):
            uv0, uv1 = uv0 or uvs[i0], uv1 or uvs[i1]
            for j in range(n):
                j2 = (j + 1) % n
                f = self.bm.faces.new((verts[i0][j], verts[i0][j2], verts[i1][j2], verts[i1][j]))
                for loop, uv in zip(f.loops, (uv0[j], uv0[j2], uv1[j2], uv1[j])):
                    loop[self.uv].uv = uv
                faces.append(f)

        for i in range(len(rings) - 1):
            quad(i, i + 1)
        if close_uv:
            inner, outer = close_uv
            last, first = rings[-1], rings[0]
            prof_l, prof_f = square(last[0], last[1]), square(first[0], first[1])
            quad(len(rings) - 1, 0,
                 [region_uv(part, x * inner / last[0], y * inner / last[0]) for x, y in prof_l],
                 [region_uv(part, x * outer / first[0], y * outer / first[0]) for x, y in prof_f])
        else:
            for ring, ring_uv, flip in ((verts[0], uvs[0], True), (verts[-1], uvs[-1], False)):
                order = list(range(n))
                if flip:
                    order.reverse()
                f = self.bm.faces.new([ring[j] for j in order])
                for loop, j in zip(f.loops, order):
                    loop[self.uv].uv = ring_uv[j]
                faces.append(f)
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        return faces

    def extrude(self, profile_xz, y_front, y_back, uv_fn):
        """A closed x-z profile swept along y, both faces drawn by `uv_fn(x, z)`. The walls get
        the outline's own UVs at both ends, so they take the colour drawn along the rim."""
        rings = [[self.bm.verts.new((x, y, z)) for x, z in profile_xz] for y in (y_front, y_back)]
        uvs = [uv_fn(x, z) for x, z in profile_xz]
        n = len(profile_xz)
        faces = []
        for j in range(n):
            j2 = (j + 1) % n
            f = self.bm.faces.new((rings[0][j], rings[0][j2], rings[1][j2], rings[1][j]))
            for loop, uv in zip(f.loops, (uvs[j], uvs[j2], uvs[j2], uvs[j])):
                loop[self.uv].uv = uv
            faces.append(f)
        for ring, order in ((rings[0], list(range(n))), (rings[1], list(reversed(range(n))))):
            f = self.bm.faces.new([ring[j] for j in order])
            for loop, j in zip(f.loops, order):
                loop[self.uv].uv = uvs[j]
            faces.append(f)
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        return faces

    def finish(self, collection, mat, location=(0, 0, 0), bevel=0.012, segments=2, sharp_deg=38.0):
        """Smooth everywhere except across edges steeper than `sharp_deg`, then the house Bevel."""
        for f in self.bm.faces:
            f.smooth = True
        limit = math.radians(sharp_deg)
        for e in self.bm.edges:
            if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > limit:
                e.smooth = False
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        mesh.materials.append(material(mat))
        obj = bpy.data.objects.new(self.name, mesh)
        obj.location = location
        collection.objects.link(obj)
        if bevel:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments = bevel, segments
            mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
            mod.harden_normals, mod.use_clamp_overlap = True, True
        return obj


def disc(radius, sides=24):
    """A bolt head's outline. 24 points, the count square() gives, so one loft code serves both."""
    return [(radius * math.cos(k / sides * math.tau), radius * math.sin(k / sides * math.tau)) for k in range(sides)]


def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(c)
    return c


# ------------------------------------------------------------------ the four parts

def pad_base(col):
    b = Part("pad_base")
    b.stack("base", [
        (0.850, 0.160, -0.010, 0.890),       # sunk a centimetre into the pavement
        (0.850, 0.160, 0.022, 0.850),        # the foot of the chamfer
        (0.790, 0.100, BASE_TOP, 0.790),     # the chamfer's top edge: a foot rolls up this
        (0.585, 0.110, BASE_TOP, 0.585),     # the flat top, carrying the painted outline
        (0.585, 0.110, CUSHION_SEAT, 0.565),  # the well the cushion sits in
    ])
    # A bolt head in each corner, outside the painted outline, sunk 5 mm into the top.
    for k, (sx, sy) in enumerate(((-1, -1), (1, -1), (1, 1), (-1, 1))):
        b.stack("bolt", [
            (0.034, 0, BASE_TOP - 0.005, 0.040),
            (0.034, 0, BASE_TOP + 0.009, 0.032),
            (0.026, 0, BASE_TOP + 0.015, 0.026),
        ], center=(sx * 0.715, sy * 0.715), turn=(23, 110, -41, 68)[k])
    return b.finish(col, "jump_pad_paint", bevel=0.010)


def pad_cushion(col):
    b = Part("pad_cushion")
    b.stack("cushion", [
        (0.575, 0.100, 0.000, 0.625),
        (0.575, 0.100, 0.030, 0.575),        # the wall, mostly hidden in the well
        (0.558, 0.097, 0.052, 0.556),        # then the pillow's dome
        (0.500, 0.085, 0.072, 0.500),
        (0.380, 0.065, 0.086, 0.380),
        (0.220, 0.040, 0.093, 0.220),
        (0.080, 0.015, 0.095, 0.080),
    ])
    return b.finish(col, "jump_pad_cushion", location=(0, 0, CUSHION_SEAT), bevel=0.008, sharp_deg=50.0)


def pad_chevron(col, location):
    b = Part("pad_chevron")
    outline = [(0.0, 0.21), (0.35, -0.06), (0.35, -0.21), (0.0, 0.06), (-0.35, -0.21), (-0.35, -0.06)]
    b.extrude(fillet(outline, 0.035, 3), -0.06, 0.06, chevron_uv)
    return b.finish(col, "jump_pad_chevron", location=location, bevel=0.018, segments=2)


def pad_ring(col, location):
    b = Part("pad_ring")
    h = RING_THICK / 2
    b.stack("ring", [
        (0.695, 0.070, -h, 0.755),
        (0.695, 0.070, h, 0.695),
        (0.605, 0.020, h, 0.605),
        (0.605, 0.020, -h, 0.545),
    ], close_uv=(0.605, 0.695))
    return b.finish(col, "jump_pad_ring", location=location, bevel=0.009)


# ------------------------------------------------------------------ the game's curves, for posing
# The same easing JumpPad.cs runs, so a review render shows a frame the game can really reach.

def out_back(t):
    t = min(1.0, max(0.0, t)) - 1.0
    return 1.0 + 2.70158 * t * t * t + 1.70158 * t * t


def in_back(t):
    t = min(1.0, max(0.0, t))
    return 2.70158 * t * t * t - 1.70158 * t * t


def glow(u, bright, dim):
    """A floating part is LIGHT: brightest as it leaves the pad, dimming as it rises."""
    return dim + (bright - dim) * (1 - u) * (1 - u)


def ring_pose(u):
    """(height of centre, scale in plan, scale in thickness) at loop position u."""
    rise = u + 0.6 * ((1 - (1 - u) ** 2) - u)
    emerge = out_back(u / 0.15)
    shrink = max(0.0, 1.0 - in_back((u - 0.68) / 0.32))
    plan = (1.0 + 0.10 * u) * shrink
    return (BASE_TOP + 0.005 + 1.5 * rise, plan, max(0.0, (0.15 + 0.85 * emerge) * shrink))


def chevron_pose(u):
    """(height of centre, width scale, height scale) at loop position u."""
    a = min(1.0, u / 0.7)
    climb = u + 0.5 * (out_back(a) - u)
    size = max(0.0, out_back(u / 0.22) * (1.0 - in_back((u - 0.78) / 0.22)))
    hurry = (1 - a) * (1 - a)
    return (0.45 + 0.9 * climb, size * (1 - 0.12 * hurry), size * (1 + 0.25 * hurry))


# ------------------------------------------------------------------ review

def review(kit):
    """Linked duplicates posed mid-loop on a pavement-grey ground under a warm low sun."""
    col = collection("review")
    ground = bpy.data.meshes.new("review_ground")
    ground.from_pydata([(-9, -9, 0), (9, -9, 0), (9, 9, 0), (-9, 9, 0)], [], [(0, 1, 2, 3)])
    grey = bpy.data.materials.new("review_pavement")
    if grey.node_tree is None:
        grey.use_nodes = True
    bsdf = grey.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.36, 0.35, 0.34, 1)
    bsdf.inputs["Roughness"].default_value = 0.95
    ground.materials.append(grey)
    col.objects.link(bpy.data.objects.new("review_ground", ground))
    floating = []
    for k, u in enumerate((0.10, 0.4333, 0.7667)):
        z, plan, thick = ring_pose(u)
        o = bpy.data.objects.new(f"review_ring_{k}", kit["pad_ring"].data)
        o.location, o.scale = (0, 0, z), (plan, plan, thick)
        o["glow"] = glow(u, 1.6, 0.35)
        floating.append(o)
    for k, u in enumerate((0.30, 0.80)):
        z, w, h = chevron_pose(u)
        o = bpy.data.objects.new(f"review_chevron_{k}", kit["pad_chevron"].data)
        o.location, o.scale = (0, 0, z), (w, w, h)
        o.rotation_euler = (0, 0, math.radians(32))          # turned to the beauty camera
        o["glow"] = glow(u, 1.6, 0.6)
        floating.append(o)
    for o in floating:
        o.modifiers.new("Bevel", "BEVEL")
        src = kit["pad_ring" if "ring" in o.name else "pad_chevron"].modifiers["Bevel"]
        mod = o.modifiers["Bevel"]
        mod.width, mod.segments, mod.limit_method = src.width, src.segments, "ANGLE"
        mod.angle_limit, mod.harden_normals = src.angle_limit, True
        # Its own glow: the material goes on the OBJECT's slot, the shared mesh keeps the kit's.
        o.material_slots[0].link = "OBJECT"
        o.material_slots[0].material = material(src.id_data.data.materials[0].name, o["glow"])
        col.objects.link(o)
    # The pad's own light and its ground halo, as JumpPad.cs builds them at runtime: a small
    # warm point light 0.45 up, and a soft amber wash on the pavement 2 cm up, fading by 2 m.
    lamp = bpy.data.objects.new("review_pad_light", bpy.data.lights.new("review_pad_light", "POINT"))
    lamp.data.color, lamp.data.energy, lamp.data.shadow_soft_size = (1.0, 0.78, 0.34), 30.0, 0.3
    lamp.data.use_shadow = False
    lamp.location = (0, 0, 0.45)
    col.objects.link(lamp)
    halo = bpy.data.meshes.new("review_halo")
    halo.from_pydata([(-2, -2, 0.02), (2, -2, 0.02), (2, 2, 0.02), (-2, 2, 0.02)], [], [(0, 1, 2, 3)])
    hm = bpy.data.materials.new("review_halo")
    if hm.node_tree is None:
        hm.use_nodes = True
    nodes, links = hm.node_tree.nodes, hm.node_tree.links
    nodes.remove(nodes["Principled BSDF"])
    coord = nodes.new("ShaderNodeTexCoord")
    grad = nodes.new("ShaderNodeTexGradient")
    grad.gradient_type = "QUADRATIC_SPHERE"
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (0.5, 0.5, 0.5)       # object space: zero by 2 m out
    links.new(coord.outputs["Object"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], grad.inputs["Vector"])
    fade = nodes.new("ShaderNodeMath")
    fade.operation = "MULTIPLY"
    fade.inputs[1].default_value = 0.6
    links.new(grad.outputs["Fac"], fade.inputs[0])
    clear, light, mix = nodes.new("ShaderNodeBsdfTransparent"), nodes.new("ShaderNodeEmission"), nodes.new("ShaderNodeMixShader")
    light.inputs["Color"].default_value = (1.0, 0.72, 0.22, 1)
    light.inputs["Strength"].default_value = 1.6
    links.new(fade.outputs[0], mix.inputs[0])
    links.new(clear.outputs[0], mix.inputs[1])
    links.new(light.outputs[0], mix.inputs[2])
    links.new(mix.outputs[0], nodes["Material Output"].inputs["Surface"])
    if hasattr(hm, "surface_render_method"):
        hm.surface_render_method = "BLENDED"
    halo.materials.append(hm)
    col.objects.link(bpy.data.objects.new("review_halo", halo))
    return floating


def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.74, 0.80, 0.90, 1)
    bg.inputs["Strength"].default_value = 0.55
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 3.0, math.radians(3), (1.0, 0.84, 0.64)
    # A warm LOW sun, about 28 degrees up, raking across the pad so the bevels and the dome show.
    sun.rotation_euler = Vector((0.70, 0.55, -0.50)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "Standard"      # the painted colours as Unity shows them


def preview(folder, version, kit, floating):
    """Every shot twice: in the warm low sun, and in SHADE (the sun off, the sky alone), which is
    where most of these pads stand, under the LRT deck, and where the glow has to carry."""
    folder.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    shots = [
        ("beauty", (2.25, -3.55, 2.05), (0.0, 0.0, 0.62), 35, True),
        ("top", (0.0, -0.02, 6.2), (0.0, 0.0, 0.0), 50, True),
        ("cushion_close", (0.85, -1.35, 0.80), (0.0, -0.05, 0.06), 45, False),
        ("edge_low", (1.9, -1.5, 0.28), (0.3, 0.0, 0.06), 50, False),
    ]
    kit["pad_chevron"].hide_render = kit["pad_ring"].hide_render = True    # the kit's own copies
    for name, pos, tgt, lens, show in shots:
        for o in floating:
            o.hide_render = not show
        pos, tgt = Vector(pos), Vector(tgt)
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        for light, energy in (("sun", 3.0), ("shade", 0.0)):
            if light == "shade" and name not in ("beauty", "top"):
                continue
            bpy.data.objects["sun"].data.energy = energy
            path = folder / f"jump_pad_{name}_{light}_v{version}.png"
            if path.exists():
                print("[jump-pad] exists, skipped", path)
                continue
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            print("[jump-pad] preview", path)
    bpy.data.objects["sun"].data.energy = 3.0
    kit["pad_chevron"].hide_render = kit["pad_ring"].hide_render = False


# ------------------------------------------------------------------ export and read-back

def export(kit):
    """One .glb, the four parts as separate nodes, modifiers applied, no cameras or lights, the
    way tools/export_ilalim_unity.py's export_glb() writes the map's kits."""
    RUNTIME.mkdir(parents=True, exist_ok=True)
    path = RUNTIME / "jump_pad.glb"
    bpy.ops.object.select_all(action="DESELECT")
    for o in kit.values():
        o.select_set(True)
    bpy.context.view_layer.objects.active = kit["pad_base"]
    bpy.ops.export_scene.gltf(
        filepath=str(path), export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
        export_materials="EXPORT", export_image_format="NONE", export_texcoords=True, export_normals=True,
        export_tangents=False, export_vertex_color="NONE", export_attributes=False, export_cameras=False,
        export_lights=False, export_animations=False, export_skins=False, export_morph=False, export_extras=False)
    return path


def read_back(path):
    """Parse the written .glb and print what Unity will get: X negated, as glTFast does."""
    data = path.read_bytes()
    length = struct.unpack_from("<I", data, 12)[0]
    doc = json.loads(data[20:20 + length])
    print(f"[jump-pad] {path.name}: {len(data)} bytes, {len(doc['nodes'])} nodes, "
          f"{len(doc.get('cameras', []))} cameras, {len(doc.get('images', []))} images")
    for node in doc["nodes"]:
        mesh = doc["meshes"][node["mesh"]]
        t = node.get("translation", [0, 0, 0])
        for prim in mesh["primitives"]:
            acc = doc["accessors"][prim["attributes"]["POSITION"]]
            lo, hi = acc["min"], acc["max"]
            tris = doc["accessors"][prim["indices"]]["count"] // 3
            print(f"[jump-pad]   {node['name']:<12} unity x {-hi[0]:+.3f}..{-lo[0]:+.3f}  y {lo[1]:+.3f}..{hi[1]:+.3f}"
                  f"  z {lo[2]:+.3f}..{hi[2]:+.3f}  node at ({-t[0]:+.3f}, {t[1]:+.3f}, {t[2]:+.3f})"
                  f"  uv {'TEXCOORD_0' in prim['attributes']}  rot {'rotation' in node}  {tris} tris")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    bpy.ops.wm.read_factory_settings(use_empty=True)
    col = collection("jump pad (kit)")
    kit = {
        "pad_base": pad_base(col),
        "pad_cushion": pad_cushion(col),
        "pad_chevron": pad_chevron(col, (0, 0, 0.75)),      # parked over the pad; the game places them
        "pad_ring": pad_ring(col, (0, 0, 0.35)),
    }
    floating = review(kit)
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "jump_pad.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True, relative_remap=True)
    backup = SOURCE / "jump_pad.blend1"
    if backup.exists():
        backup.unlink()
    print(f"[jump-pad] saved {out}")
    read_back(export(kit))
    if "--preview" in argv:
        i = argv.index("--preview")
        preview(Path(argv[i + 1]), int(argv[i + 2]), kit, floating)


if __name__ == "__main__":
    main()
