"""Model the Eskinita Alley's LIVE cats and dogs in Blender, export one glb each in posable parts, review.

  blender -b --python tools/author_eskinita_pets.py -- [--sheet N]

Owner, 2026-10-09: "can you make actual moving cats and dogs, with cat and dog chases as a background
life event". The game's older street animals (Art/models/ambient-life: aspin-*, pusakal-*) are boxy
voxel studies; the alley is round and chunky, so these are new, in the style of the alley's chickens
(tools/author_eskinita_chickens.py) and split the same way: no skeleton, a few rigid PARTS whose
origins are the joints, posed in code by `AlleyPets`.

Writes:
  ArtSource/eskinita/pets.blend
  Assets/TumbangPreso/Art/EskinitaAlley/Models/pet_<name>.glb   dog_tan, dog_white (askal), cat_tuxedo, cat_calico, cat_white
  Assets/TumbangPreso/Art/EskinitaAlley/materials_pets.json     the materials, by NAME (flat tints)
  Logs/eskinita/pets_sheet_vN.png                               with --sheet N (rest and POSED tiles)

THE PARTS (found by name anywhere under the model):
  pet_<name>              the root, origin on the GROUND under the middle of the animal
    trunk                 an empty at the HIP centre: lowered and pitched to sit, lie and stretch
      body                the trunk's mesh
      head                origin at the NECK: head, muzzle, ears, eyes
      tail                origin at the tail's base
      leg_fl, leg_fr      origin at the SHOULDERS
      leg_bl, leg_br      origin at the HIPS
  A leg hangs straight down from its origin, so turning it about the side axis swings it; the code
  counter-turns the legs when it pitches the trunk. The shoulders are about 1.4 leg lengths ahead of
  the hips, so a sit (hips down, trunk pitched up 30 to 40 degrees) puts the front paws on the ground.

CONVENTIONS: the chickens' (metres, Z up, front faces -Y, stands on z = 0, exported +Y up so the
animal faces Unity +Z and the parts on Blender +X are its LEFT: leg_fl, leg_bl). Materials `ekpet_*`,
flat sRGB tints, no textures. Nothing near offence orange f87020 or defence blue 0080e8.
"""
import json
import math
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
SOURCE = ROOT / "ArtSource" / "eskinita"
LOGS = ROOT / "Logs" / "eskinita"
TILES = Path(tempfile.gettempdir()) / "esk_chick_pet_tiles"
PAINTER = ROOT / "tools" / "author_eskinita_props_textures.py"

MATS = {
    "ekpet_tan": "c98f55",        # the tan askal
    "ekpet_tan_dark": "9a6436",   # his ears, saddle
    "ekpet_cream": "f0dcb4",      # muzzles, bellies, socks, the calico's ground colour
    "ekpet_white": "f5f0e6",
    "ekpet_patch": "8a5a3a",      # the white askal's brown patch and ear
    "ekpet_black": "2b2a2e",      # the tuxedo cat
    "ekpet_grey": "8f8c8a",       # the white cat's tail and ear
    "ekpet_calico": "d9a05e",     # the calico's tan patches
    "ekpet_nose": "2a2422",
    "ekpet_pink": "e79aa0",       # cat noses, inner ears
    "ekpet_eye": "22201f",
    "ekpet_eye_cat": "9fbf4a",    # cat irises
}


def srgb(hexs):
    return tuple(int(hexs[i:i + 2], 16) / 255 for i in (0, 2, 4))


def linear(c):
    return ((c + 0.055) / 1.055) ** 2.4 if c > 0.04045 else c / 12.92


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    rgb = tuple(linear(c) for c in srgb(MATS[name]))
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1)
    bsdf.inputs["Roughness"].default_value = 0.85
    m.diffuse_color = (*rgb, 1)
    return m


def RX(deg):
    return Matrix.Rotation(math.radians(deg), 4, "X")


def T(v):
    return Matrix.Translation(Vector(v))


class Part:
    """One rigid piece. Everything is given in MODEL space; `finish` moves the mesh so the
    object's origin is the joint."""

    def __init__(self, name, origin):
        self.name, self.origin, self.bm, self.mats = name, Vector(origin), bmesh.new(), []

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def _paint(self, verts, mat):
        idx = self.mi(mat)
        for f in {f for v in verts for f in v.link_faces}:
            f.material_index = idx

    def sphere(self, c, radii, mat, rot=None, subdiv=2):
        if isinstance(radii, (int, float)):
            radii = (radii, radii, radii)
        r = bmesh.ops.create_icosphere(self.bm, subdivisions=subdiv + 1, radius=1.0,
                                       matrix=T(c) @ (rot or Matrix()) @ Matrix.Diagonal((*radii, 1)))
        self._paint(r["verts"], mat)

    def cone(self, a, b, r0, r1, mat, sides=8):
        """A round member from a (radius r0) to b (radius r1)."""
        a, b = Vector(a), Vector(b)
        q = (b - a).to_track_quat("Z", "Y").to_matrix().to_4x4()
        r = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=sides, radius1=r0, radius2=r1,
                                  depth=(b - a).length, matrix=T((a + b) / 2) @ q)
        self._paint(r["verts"], mat)

    def finish(self, col, parent, parent_origin, k):
        bmesh.ops.scale(self.bm, vec=(k, k, k), verts=self.bm.verts)
        bmesh.ops.translate(self.bm, vec=-self.origin * k, verts=self.bm.verts)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(material(m))
        for p in mesh.polygons:
            p.use_smooth = True
        mesh.set_sharp_from_angle(angle=math.radians(62))
        obj = bpy.data.objects.new(self.name, mesh)
        col.objects.link(obj)
        obj.parent = parent
        obj.location = (self.origin - parent_origin) * k
        return obj


def tube(part, pts, r0, r1, mat, sides=7):
    """A round tail through `pts`, tapering."""
    pts = [Vector(p) for p in pts]
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        ra = r0 + (r1 - r0) * i / (len(pts) - 1)
        rb = r0 + (r1 - r0) * (i + 1) / (len(pts) - 1)
        part.cone(a, b, ra, rb, mat, sides=sides)
        part.sphere(b, rb, mat, subdiv=0)


class Pet:
    def __init__(self, name, k=1.0):
        self.name, self.k = name, k
        self.col = bpy.data.collections.new(f"pet_{name}")
        bpy.context.scene.collection.children.link(self.col)
        self.root = bpy.data.objects.new(f"pet_{name}", None)
        self.col.objects.link(self.root)
        self.obj = {}
        self.dims = {}

    def build(self, hip, parts):
        hip = Vector(hip)
        trunk = bpy.data.objects.new("trunk", None)
        trunk.empty_display_size = 0.03
        self.col.objects.link(trunk)
        trunk.parent = self.root
        trunk.location = hip * self.k
        self.obj["trunk"] = trunk
        for name, part in parts.items():
            self.obj[name] = part.finish(self.col, trunk, hip, self.k)
        return self


def legs(parts, sx, sy, hx, hy, top, r, coat, paw, paw_r, thigh=None):
    """Four legs hanging from the shoulders (y = -sy) and hips (y = +hy), both at height `top`."""
    for s, side in ((1, "l"), (-1, "r")):
        f = Part(f"leg_f{side}", (s * sx, -sy, top))
        f.cone((s * sx, -sy, top + r * 0.4), (s * sx, -sy, paw_r[2]), r * 1.12, r * 0.92, coat, sides=8)
        f.sphere((s * sx, -sy, top), r * 1.25, coat, subdiv=1)
        f.sphere((s * sx, -sy - paw_r[1] * 0.35, paw_r[2]), paw_r, paw, subdiv=1)
        parts[f"leg_f{side}"] = f
        b = Part(f"leg_b{side}", (s * hx, hy, top))
        if thigh:
            b.sphere((s * hx, hy, top - thigh[2] * 0.45), thigh, coat, subdiv=1)
        b.sphere((s * hx, hy, top), r * 1.25, coat, subdiv=1)
        b.cone((s * hx, hy, top + r * 0.4), (s * hx, hy + r * 0.2, paw_r[2]), r * 1.12, r * 0.92, coat, sides=8)
        b.sphere((s * hx, hy - paw_r[1] * 0.3, paw_r[2]), paw_r, paw, subdiv=1)
        parts[f"leg_b{side}"] = b


def dog(name, pal):
    """An askal: lean, short coat, pointed ears (one folded on the white one), tail curled over the back."""
    p = Pet(name, 0.9)
    top, sy = 0.25, 0.17
    hip = (0, sy, top)
    parts = {}
    legs(parts, 0.062, sy, 0.066, sy, top, 0.035, pal["coat"], pal["paw"], (0.036, 0.048, 0.024), thigh=(0.05, 0.07, 0.075))
    body = Part("body", hip)
    body.sphere((0, 0.0, 0.295), (0.098, 0.235, 0.1), pal["coat"])
    body.sphere((0, -0.135, 0.3), (0.103, 0.105, 0.112), pal["coat"])                 # the chest
    body.sphere((0, 0.15, 0.298), (0.094, 0.1, 0.098), pal["coat"])                   # the rump
    body.sphere((0, -0.03, 0.262), (0.082, 0.2, 0.07), pal["belly"], subdiv=1)        # the pale underside
    body.sphere((0, -0.2, 0.275), (0.07, 0.05, 0.08), pal["belly"], subdiv=1)         # and bib
    if pal.get("patch"):
        body.sphere((0.045, 0.07, 0.345), (0.075, 0.105, 0.06), pal["patch"], subdiv=1)
        body.sphere((-0.06, -0.08, 0.33), (0.05, 0.07, 0.06), pal["patch"], subdiv=1)
    else:
        body.sphere((0, 0.02, 0.352), (0.07, 0.19, 0.05), pal["dark"], subdiv=1)      # a darker saddle
    parts["body"] = body
    head = Part("head", (0, -0.2, 0.36))
    head.cone((0, -0.17, 0.33), (0, -0.265, 0.445), 0.078, 0.064, pal["coat"], sides=10)
    head.sphere((0, -0.295, 0.47), (0.086, 0.09, 0.08), pal["coat"])
    head.sphere((0, -0.378, 0.448), (0.05, 0.066, 0.046), pal["belly"])               # the muzzle
    head.sphere((0, -0.44, 0.46), (0.02, 0.016, 0.016), "ekpet_nose", subdiv=1)
    for s in (-1, 1):
        head.sphere((s * 0.047, -0.362, 0.497), 0.0155, "ekpet_eye", subdiv=1)
        head.sphere((s * 0.053, -0.372, 0.503), 0.005, "ekpet_white", subdiv=0)
        ear = pal["ear_l"] if s > 0 else pal["ear_r"]
        if pal.get("flop") and s < 0:
            head.cone((s * 0.06, -0.265, 0.525), (s * 0.085, -0.28, 0.585), 0.036, 0.024, ear, sides=7)
            head.cone((s * 0.085, -0.28, 0.585), (s * 0.1, -0.335, 0.56), 0.024, 0.006, ear, sides=7)
        else:
            head.cone((s * 0.058, -0.265, 0.525), (s * 0.084, -0.268, 0.63), 0.038, 0.006, ear, sides=7)
    parts["head"] = head
    tail = Part("tail", (0, 0.23, 0.335))
    tube(tail, [(0, 0.22, 0.33), (0, 0.285, 0.39), (0, 0.3, 0.46), (0, 0.27, 0.515), (0, 0.22, 0.53)], 0.03, 0.016, pal["coat"])
    tail.sphere((0, 0.22, 0.53), 0.02, pal["tip"], subdiv=1)
    parts["tail"] = tail
    p.dims = dict(kind="dog")
    return p.build(hip, parts)


def cat(name, pal):
    p = Pet(name, 1.0)
    top, sy = 0.125, 0.092
    hip = (0, sy, top)
    parts = {}
    legs(parts, 0.034, sy, 0.038, sy, top, 0.0225, pal["coat"], pal["paw"], (0.022, 0.028, 0.014), thigh=(0.03, 0.045, 0.045))
    body = Part("body", hip)
    body.sphere((0, 0.0, 0.152), (0.062, 0.135, 0.06), pal["coat"])
    body.sphere((0, -0.08, 0.158), (0.059, 0.058, 0.064), pal["chest"])
    body.sphere((0, 0.085, 0.152), (0.06, 0.06, 0.06), pal["coat"])
    for c, r, m in pal.get("patches", ()):
        body.sphere(c, r, m, subdiv=1)
    parts["body"] = body
    head = Part("head", (0, -0.108, 0.185))
    head.sphere((0, -0.112, 0.19), (0.045, 0.04, 0.045), pal["chest"], subdiv=1)                # the neck
    head.sphere((0, -0.152, 0.236), (0.07, 0.063, 0.059), pal["coat"])
    head.sphere((0, -0.2, 0.222), (0.034, 0.026, 0.023), pal["muzzle"], subdiv=1)
    head.sphere((0, -0.224, 0.23), (0.008, 0.006, 0.006), "ekpet_pink", subdiv=0)
    for c, r, m in pal.get("face", ()):
        head.sphere(c, r, m, subdiv=1)
    for s in (-1, 1):
        head.sphere((s * 0.031, -0.203, 0.25), (0.0135, 0.008, 0.016), "ekpet_eye_cat", subdiv=1)
        head.sphere((s * 0.031, -0.209, 0.25), (0.006, 0.005, 0.013), "ekpet_eye", subdiv=1)
        ear = pal["ear_l"] if s > 0 else pal["ear_r"]
        head.cone((s * 0.04, -0.148, 0.275), (s * 0.052, -0.146, 0.338), 0.03, 0.004, ear, sides=7)
        head.cone((s * 0.04, -0.158, 0.28), (s * 0.05, -0.154, 0.325), 0.018, 0.003, "ekpet_pink", sides=5)
    parts["head"] = head
    tail = Part("tail", (0, 0.135, 0.165))
    pts = [(0, 0.13, 0.165), (0, 0.2, 0.185), (0, 0.255, 0.235), (0, 0.27, 0.3), (0, 0.25, 0.35)]
    tube(tail, pts, 0.021, 0.016, pal["tail"])
    tail.sphere(pts[-1], 0.02, pal["tip"], subdiv=1)
    parts["tail"] = tail
    p.dims = dict(kind="cat")
    return p.build(hip, parts)


TAN = dict(coat="ekpet_tan", belly="ekpet_cream", paw="ekpet_cream", dark="ekpet_tan_dark", ear_l="ekpet_tan_dark", ear_r="ekpet_tan_dark", tip="ekpet_cream")
PUTI = dict(coat="ekpet_white", belly="ekpet_white", paw="ekpet_white", patch="ekpet_patch", ear_l="ekpet_white", ear_r="ekpet_patch", tip="ekpet_patch", flop=True)
TUX = dict(coat="ekpet_black", chest="ekpet_white", paw="ekpet_white", muzzle="ekpet_white", tail="ekpet_black", tip="ekpet_white",
           ear_l="ekpet_black", ear_r="ekpet_black", face=[((0, -0.19, 0.205), (0.04, 0.03, 0.03), "ekpet_white")])
CALICO = dict(coat="ekpet_cream", chest="ekpet_white", paw="ekpet_white", muzzle="ekpet_white", tail="ekpet_calico", tip="ekpet_black",
              ear_l="ekpet_calico", ear_r="ekpet_black",
              patches=[((0.03, 0.06, 0.185), (0.045, 0.06, 0.035), "ekpet_calico"), ((-0.035, -0.02, 0.18), (0.04, 0.055, 0.035), "ekpet_black"),
                       ((0.045, -0.05, 0.16), (0.03, 0.04, 0.04), "ekpet_calico")],
              face=[((0.035, -0.15, 0.27), (0.04, 0.045, 0.03), "ekpet_calico"), ((-0.04, -0.14, 0.265), (0.036, 0.04, 0.03), "ekpet_black")])
WHITE = dict(coat="ekpet_white", chest="ekpet_white", paw="ekpet_white", muzzle="ekpet_white", tail="ekpet_grey", tip="ekpet_grey",
             ear_l="ekpet_grey", ear_r="ekpet_white", patches=[((0.0, 0.07, 0.19), (0.045, 0.06, 0.03), "ekpet_grey")])

PETS = [
    ("dog_tan", lambda: dog("dog_tan", TAN)), ("dog_white", lambda: dog("dog_white", PUTI)),
    ("cat_tuxedo", lambda: cat("cat_tuxedo", TUX)), ("cat_calico", lambda: cat("cat_calico", CALICO)), ("cat_white", lambda: cat("cat_white", WHITE)),
]


def bounds(pet):
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    bpy.context.view_layer.update()
    for o in pet.col.objects:
        if o.type != "MESH":
            continue
        for v in o.data.vertices:
            q = o.matrix_world @ v.co
            lo = Vector((min(lo.x, q.x), min(lo.y, q.y), min(lo.z, q.z)))
            hi = Vector((max(hi.x, q.x), max(hi.y, q.y), max(hi.z, q.z)))
    return lo, hi


def export(pet, others):
    for other in others:
        for key, o in other.obj.items():
            o.name = f"{other.name}__{key}"
            if o.data:
                o.data.name = f"{other.name}__{key}"
    for key, o in pet.obj.items():
        o.name = key
        if o.data:
            o.data.name = f"{pet.name}_{key}"
    bpy.context.view_layer.update()
    mine = set(pet.col.objects)
    for o in bpy.context.view_layer.objects:
        o.select_set(o in mine)
    bpy.context.view_layer.objects.active = pet.root
    path = MODELS / f"pet_{pet.name}.glb"
    bpy.ops.export_scene.gltf(filepath=str(path), export_format="GLB", use_selection=True, export_yup=True,
                              export_apply=True, export_animations=False, export_image_format="NONE",
                              export_materials="EXPORT")
    tris, used = 0, []
    for o in pet.col.objects:
        if o.type == "MESH":
            o.data.calc_loop_triangles()
            tris += len(o.data.loop_triangles)
            used += [m.name for m in o.data.materials if m and m.name not in used]
    return path, tris, used


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


def pose(pet, name):
    """The poses `AlleyPets` makes, by the same arithmetic, in Blender's axes (about X: + tips the front
    DOWN and swings a leg BACK). `drop` lowers the trunk; a leg's world angle is kept by counter-turning."""
    o = pet.obj
    for x in o.values():
        x.rotation_euler = (0, 0, 0)
        x.scale = (1, 1, 1)
    rest = pet.trunk_z                                              # hip and shoulder height standing = leg length
    span = abs(o["leg_fl"].location.y - o["leg_bl"].location.y)
    cat_ = pet.dims["kind"] == "cat"
    pitch, drop, legs_w, head, tail = 0.0, 0.0, dict(fl=0, fr=0, bl=0, br=0), (0, 0), 0
    if name == "sit":
        pitch = -(30 if cat_ else 38)
        drop = span * math.sin(math.radians(-pitch))
        legs_w = dict(fl=0, fr=0, bl=-78, br=-78)
        head = (-pitch * 0.85, 0)
    elif name == "lie":
        drop = rest * (0.56 if cat_ else 0.62)
        legs_w = dict(fl=-84, fr=-84, bl=-84, br=-84)
        head = (6, 20)
    elif name == "loaf":
        drop = rest * 0.56
        legs_w = dict(fl=-84, fr=-84, bl=-84, br=-84)
        o["leg_fl"].scale = o["leg_fr"].scale = (0.05, 0.05, 0.05)
        head = (4, 0)
    elif name == "run":
        legs_w = dict(fl=-48, fr=-30, bl=44, br=28)
        pitch, head, tail = 5, (-8, 0), -30
    elif name == "walk":
        legs_w = dict(fl=-24, fr=24, bl=24, br=-24)
    elif name == "stretch":
        pitch = 22
        legs_w = dict(fl=-58, fr=-58, bl=8, br=8)
        head, tail = (-30, 0), 35
    elif name == "bark":
        pitch, head = -8, (-28, 0)
        legs_w = dict(fl=-10, fr=-10, bl=12, br=12)
    elif name == "sniff":
        pitch, head = 10, (48, 0)
    o["trunk"].location.z = pet.trunk_z - drop
    o["trunk"].rotation_euler = (math.radians(pitch), 0, 0)
    for key, ang in legs_w.items():
        o["leg_" + key].rotation_euler = (math.radians(ang - pitch), 0, 0)
    o["head"].rotation_euler = (math.radians(head[0]), 0, math.radians(head[1]))
    o["tail"].rotation_euler = (math.radians(tail), 0, 0)


def review(version, pets, records):
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    LOGS.mkdir(parents=True, exist_ok=True)
    TILES.mkdir(parents=True, exist_ok=True)
    MATS["rev_ground"] = "cfc8bc"
    g = Part("review_ground", (0, 0, 0))
    r = bmesh.ops.create_cube(g.bm, size=1.0, matrix=T((0, 0, -0.5)) @ Matrix.Diagonal((40, 40, 0.998, 1)))
    g._paint(r["verts"], "rev_ground")
    rev = bpy.data.collections.new("review")
    scene.collection.children.link(rev)
    ground = g.finish(rev, None, Vector((0, 0, 0)), 1.0)
    for p in ground.data.polygons:
        p.use_smooth = False
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type = "ORTHO"
    cam.data.clip_end = 200
    scene.render.resolution_x, scene.render.resolution_y = 520, 440
    by = {p.name: p for p in pets}
    views = {"front": Vector((-0.6, -1.0, 0.38)), "side": Vector((-1.0, -0.12, 0.16)), "back": Vector((0.6, 1.0, 0.45))}
    shots = [(p.name, "rest", v) for p in pets for v in ("front", "side")]
    shots += [("dog_tan", "sit", "side"), ("dog_tan", "sit", "front"), ("dog_white", "lie", "front"), ("dog_tan", "run", "side"),
              ("dog_white", "bark", "side"), ("dog_tan", "sniff", "side"), ("dog_white", "walk", "side"), ("dog_tan", "rest", "back"),
              ("cat_calico", "sit", "side"), ("cat_calico", "sit", "front"), ("cat_tuxedo", "loaf", "front"), ("cat_tuxedo", "run", "side"),
              ("cat_white", "stretch", "side"), ("cat_tuxedo", "lie", "side"), ("cat_calico", "walk", "side"), ("cat_tuxedo", "rest", "back")]
    tiles = []
    for name, pname, vname in shots:
        p = by[name]
        for other in pets:
            other.col.hide_render = other is not p
        pose(p, pname)
        lo, hi = bounds(p)
        lo.z = min(lo.z, 0.0)
        d = views[vname].normalized()
        quat = d.to_track_quat("Z", "Y")
        right, upv = quat @ Vector((1, 0, 0)), quat @ Vector((0, 1, 0))
        corners = [Vector((x, y, z)) for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z, hi.z)]
        us, vs = [c.dot(right) for c in corners], [c.dot(upv) for c in corners]
        centre = right * ((min(us) + max(us)) / 2) + upv * ((min(vs) + max(vs)) / 2) + d * ((lo + hi) / 2).dot(d)
        cam.location = centre + d * 30
        cam.rotation_euler = quat.to_euler()
        cam.data.ortho_scale = max(max(us) - min(us), (max(vs) - min(vs)) * 520 / 440) * 1.22
        path = TILES / f"{name}_{pname}_{vname}.png"
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        pose(p, "rest")
        s = records[name]["size"]
        tiles.append({"file": str(path), "label": f"pet_{name}  {pname}  {vname}",
                      "sub": f"{s[0]:.2f} x {s[1]:.2f} x {s[2]:.2f} m   {records[name]['tris']} tris"})
    for other in pets:
        other.col.hide_render = False
    spec = TILES / "sheet.json"
    spec.write_text(json.dumps({"title": f"Eskinita Alley cats and dogs v{version}", "cols": 6, "tiles": tiles}))
    subprocess.run(["py", "-3", str(PAINTER), "--compose", str(spec), str(LOGS / f"pets_sheet_v{version}.png")], check=False)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--sheet") + 1]) if "--sheet" in argv else None
    for d in (MODELS, SOURCE, LOGS):
        d.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    pets = [fn() for _name, fn in PETS]
    for p in pets:
        p.trunk_z = p.obj["trunk"].location.z
    setup_light()
    records, used_all = {}, []
    for p in pets:
        path, tris, used = export(p, [o for o in pets if o is not p])
        lo, hi = bounds(p)
        size = [round(hi.x - lo.x, 3), round(hi.y - lo.y, 3), round(hi.z - lo.z, 3)]
        records[p.name] = dict(tris=tris, size=size)
        used_all += [u for u in used if u not in used_all]
        print(f"[eskinita pets] pet_{p.name:12s} {tris:5d} tris  {size[0]:.2f} x {size[1]:.2f} x {size[2]:.2f}  {path.stat().st_size} bytes  low z {lo.z:.3f}")
    for p in pets:
        for key, o in p.obj.items():
            o.name = f"{p.name}__{key}"
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "pets.blend"))
    mats = [{"name": n, "texture": "", "tint": [round(c, 4) for c in srgb(MATS[n])], "tiling": 1.0,
             "foliage": False, "emissive": False, "glossy": False, "cutout": False} for n in sorted(used_all)]
    (ART / "materials_pets.json").write_text(json.dumps(
        {"tint_space": "sRGB 0..1, flat colours (no textures)",
         "parts": "root pet_<name> (origin on the ground) > trunk (empty, hip centre) > body, head (neck), tail (base), leg_fl, leg_fr (shoulders), leg_bl, leg_br (hips). Unity: faces +Z, leg_fl is on -X.",
         "materials": mats}, indent=1))
    print(f"[eskinita pets] {len(pets)} pets, {len(mats)} materials, {sum(r['tris'] for r in records.values())} triangles")
    if version is not None:
        review(version, pets, records)


main()
