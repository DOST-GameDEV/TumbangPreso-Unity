"""Model the Eskinita Alley's LIVE chickens in Blender, export one rigged-by-parts glb each, review.

  blender -b --python tools/author_eskinita_chickens.py -- [--sheet N]

Owner, 2026-10-09, looking at the rooster statue beside its tepee: "if you're gonna make chickens,
make them live, make them like the birds in lagoon cove". So the statue left `prop_manok_cage`
(tools/author_eskinita_props.py) and the birds are models of their own, split the way the Lagoon's
birds are (tools/lagoon_prop_fauna.py: no skeleton, a few rigid PARTS whose origins are the joints,
posed in code), with the extra joints a walking ground bird needs.

Writes:
  ArtSource/eskinita/chickens.blend                                   one collection per bird
  Assets/TumbangPreso/Art/EskinitaAlley/Models/chicken_<name>.glb     rooster, rooster_white, hen, hen_white, chick
  Assets/TumbangPreso/Art/EskinitaAlley/materials_chickens.json       the materials, by NAME (flat tints)
  Logs/eskinita/chickens_sheet_vN.png                                 with --sheet N (rest and POSED tiles)

THE PARTS (what `AlleyChickens` in Unity looks for, by name, anywhere under the model):
  chicken_<name>          the root, origin on the GROUND between the feet
    leg_l, leg_r          origin at the HIP: swing about the side axis to walk and scratch
    torso                 an empty at the hip centre: pitch it to peck, roll it to waddle
      body                the trunk and tail, no joint of its own
      head                origin at the NECK BASE: neck, head, comb, wattles, beak, eyes
      wing_l, wing_r      origin at the SHOULDER, modelled FOLDED on the flank (a ground bird's
                          rest pose): roll about the body's forward axis to flap

CONVENTIONS (the prop kit's)
  * Metres. Z up, the bird's front faces -Y, it stands on z = 0. Exported +Y up: in Unity
    (glTFast) a Blender point (x, y, z) lands at (-x, z, -y), so the bird faces Unity +Z, and
    the part on Blender +X is on Unity -X: the bird's LEFT. wing_l and leg_l are on Blender +X.
  * Chunky simple pieces that PENETRATE each other, flat colours, no textures, nothing floating.
    No bevel modifiers are left live: every piece is a round form.
  * Materials are named `ekc_*`. Tints in the JSON are sRGB 0..1.
  * Nothing near offence orange f87020 or defence blue 0080e8.
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
TILES = Path(tempfile.gettempdir()) / "esk_chick_tiles"
PAINTER = ROOT / "tools" / "author_eskinita_props_textures.py"

MATS = {
    "ekc_rooster": "b8552f",       # the red rooster's trunk and head
    "ekc_gold": "f0b63a",          # his hackle and saddle
    "ekc_wing_dark": "7c3b27",     # his wing, the brown hen's wing tip and tail
    "ekc_tail_dark": "23403c",     # sickle feathers, green-black
    "ekc_tail_teal": "2f7d74",     # sickle feathers and the wing bar
    "ekc_comb": "d8362b",          # comb, wattles, the face patch
    "ekc_beak": "e9b430",          # beak, shanks and toes
    "ekc_eye": "22201f",
    "ekc_white": "f6f1e4",         # the white birds, and the eye's highlight
    "ekc_cream": "f1dda8",         # the white rooster's hackle
    "ekc_grey": "c9c2b4",          # the white birds' wings
    "ekc_hen": "cf9152",           # the brown hen
    "ekc_hen_dark": "a56a3a",      # her neck and wing
    "ekc_chick": "f7d95c",
    "ekc_chick_wing": "e8bf3e",
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


class Bird:
    def __init__(self, name, k=1.0):
        self.name, self.k = name, k
        self.col = bpy.data.collections.new(f"chicken_{name}")
        bpy.context.scene.collection.children.link(self.col)
        self.root = bpy.data.objects.new(f"chicken_{name}", None)
        self.col.objects.link(self.root)
        self.obj = {}

    def build(self, hip, parts):
        """`parts` = {name: Part}. leg_* hang from the root; the rest from the torso empty at `hip`."""
        hip = Vector(hip)
        torso = bpy.data.objects.new("torso", None)
        torso.empty_display_size = 0.03
        self.col.objects.link(torso)
        torso.parent = self.root
        torso.location = hip * self.k
        self.obj["torso"] = torso
        for name, part in parts.items():
            if name.startswith("leg"):
                self.obj[name] = part.finish(self.col, self.root, Vector((0, 0, 0)), self.k)
            else:
                self.obj[name] = part.finish(self.col, torso, hip, self.k)
        # Blender suffixes a second "body" as "body.001": the names must be exact in every file,
        # so every bird is exported from a scene where only ITS objects carry the plain names.
        return self


def legs(parts, hipx, hipy, hipz, shank, toe, fluff=None, spur=False):
    for s, name in ((1, "leg_l"), (-1, "leg_r")):
        p = Part(name, (s * hipx, hipy, hipz))
        x = s * hipx
        if fluff:
            p.sphere((x, hipy + 0.004, hipz - 0.012), (0.036, 0.046, 0.05), fluff, subdiv=1)
        p.cone((x, hipy + 0.004, hipz - 0.03), (x, hipy - 0.006, 0.012), shank * 1.1, shank, "ekc_beak", sides=7)
        for d, ln in ((-1, 0.82), (0, 1.0), (1, 0.82)):
            p.cone((x, hipy, 0.012), (x + d * toe * 0.5, hipy - toe * ln, 0.01), shank * 0.9, shank * 0.42, "ekc_beak", sides=6)
        p.cone((x, hipy - 0.004, 0.012), (x, hipy + toe * 0.5, 0.01), shank * 0.85, shank * 0.4, "ekc_beak", sides=6)
        if spur:
            p.cone((x, hipy, 0.055), (x - s * 0.006, hipy + 0.03, 0.062), shank * 0.6, 0.002, "ekc_beak", sides=5)
        parts[name] = p


def adult(name, k, pal, rooster):
    """A rooster or a hen. `pal`: body, neck, wing, bar, tail (a list the tail feathers cycle)."""
    b = Bird(name, k)
    hip = (0, 0.015, 0.17)
    parts = {}
    legs(parts, 0.05, 0.015, 0.17, 0.0155, 0.078, fluff=pal["body"], spur=rooster)

    body = Part("body", hip)
    body.sphere((0, 0.02, 0.262), (0.108, 0.15, 0.106), pal["body"], rot=RX(-12))
    body.sphere((0, -0.078, 0.268), (0.09, 0.08, 0.096), pal["body"])                      # the chest
    body.sphere((0, 0.118, 0.288), (0.072, 0.076, 0.066), pal["body"])                     # the rump
    root = Vector((0, 0.165, 0.3))
    if rooster:
        body.sphere((0, 0.098, 0.318), (0.076, 0.072, 0.044), pal["neck"], subdiv=1)       # the saddle
        fan = ((78, 0.13, 0.0), (62, 0.165, 0.014), (46, 0.175, -0.014), (28, 0.155, 0.012), (8, 0.115, -0.01))
        for i, (ang, ln, x) in enumerate(fan):
            a = math.radians(ang)
            body.sphere(root + Vector((x, math.cos(a) * ln * 0.78, math.sin(a) * ln * 0.78)),
                        (0.028, ln, 0.05), pal["tail"][i % len(pal["tail"])], rot=RX(ang), subdiv=1)
    else:
        for i, (ang, ln, x) in enumerate(((66, 0.085, 0.0), (48, 0.095, 0.014), (48, 0.095, -0.014), (28, 0.08, 0.0))):
            a = math.radians(ang)
            body.sphere(root + Vector((x, math.cos(a) * ln * 0.6 - 0.02, math.sin(a) * ln * 0.6)),
                        (0.03, ln, 0.045), pal["tail"][i % len(pal["tail"])], rot=RX(ang), subdiv=1)
    parts["body"] = body

    head = Part("head", (0, -0.07, 0.31))
    head.sphere((0, -0.066, 0.326), (0.086, 0.076, 0.072), pal["neck"])                    # the cape, on the shoulders
    head.cone((0, -0.062, 0.3), (0, -0.116, 0.425), 0.076, 0.05, pal["neck"], sides=12)    # the neck
    head.sphere((0, -0.126, 0.446), (0.058, 0.06, 0.056), pal["body"])
    head.cone((0, -0.168, 0.44), (0, -0.226, 0.428), 0.021, 0.003, "ekc_beak", sides=7)
    if rooster:
        for y, z, ry, rz in ((-0.152, 0.5, 0.02, 0.022), (-0.126, 0.512, 0.024, 0.032), (-0.098, 0.5, 0.022, 0.024)):
            head.sphere((0, y, z), (0.013, ry, rz), "ekc_comb", subdiv=1)
    else:
        head.sphere((0, -0.138, 0.498), (0.011, 0.026, 0.016), "ekc_comb", subdiv=1)
    for s in (-1, 1):
        head.sphere((s * 0.014, -0.166, 0.404 if rooster else 0.41), (0.012, 0.012, 0.027 if rooster else 0.016), "ekc_comb", subdiv=1)
        if rooster:
            head.sphere((s * 0.047, -0.138, 0.44), (0.013, 0.026, 0.024), "ekc_comb", subdiv=1)   # the face patch
        head.sphere((s * 0.049, -0.15, 0.457), 0.0155, "ekc_eye", subdiv=1)
        head.sphere((s * 0.058, -0.158, 0.464), 0.005, "ekc_white", subdiv=0)
    parts["head"] = head

    for s, wname in ((1, "wing_l"), (-1, "wing_r")):
        w = Part(wname, (s * 0.1, -0.035, 0.305))
        w.sphere((s * 0.109, 0.04, 0.262), (0.026, 0.126, 0.076), pal["wing"], rot=RX(-14))
        w.sphere((s * 0.113, 0.118, 0.236), (0.021, 0.062, 0.04), pal["bar"], rot=RX(-22), subdiv=1)
        parts[wname] = w
    return b.build(hip, parts)


def chick(name):
    b = Bird(name, 1.0)
    hip = (0, 0.0, 0.046)
    parts = {}
    legs(parts, 0.021, 0.0, 0.05, 0.0058, 0.03)
    body = Part("body", hip)
    body.sphere((0, 0.008, 0.078), (0.052, 0.06, 0.05), "ekc_chick")
    body.sphere((0, 0.058, 0.092), (0.022, 0.024, 0.02), "ekc_chick", subdiv=1)
    parts["body"] = body
    head = Part("head", (0, -0.024, 0.098))
    head.sphere((0, -0.034, 0.128), (0.043, 0.043, 0.041), "ekc_chick")
    head.cone((0, -0.07, 0.122), (0, -0.099, 0.116), 0.012, 0.002, "ekc_beak", sides=6)
    for s in (-1, 1):
        head.sphere((s * 0.031, -0.06, 0.136), 0.0085, "ekc_eye", subdiv=1)
        head.sphere((s * 0.035, -0.064, 0.14), 0.003, "ekc_white", subdiv=0)
    parts["head"] = head
    for s, wname in ((1, "wing_l"), (-1, "wing_r")):
        w = Part(wname, (s * 0.046, -0.006, 0.094))
        w.sphere((s * 0.051, 0.014, 0.08), (0.014, 0.04, 0.03), "ekc_chick_wing", rot=RX(-12), subdiv=1)
        parts[wname] = w
    return b.build(hip, parts)


RED = dict(body="ekc_rooster", neck="ekc_gold", wing="ekc_wing_dark", bar="ekc_tail_teal", tail=["ekc_tail_dark", "ekc_tail_teal"])
PUTI = dict(body="ekc_white", neck="ekc_cream", wing="ekc_grey", bar="ekc_tail_dark", tail=["ekc_tail_dark", "ekc_grey"])
BROWN = dict(body="ekc_hen", neck="ekc_hen_dark", wing="ekc_hen_dark", bar="ekc_wing_dark", tail=["ekc_wing_dark", "ekc_hen_dark"])
WHITE = dict(body="ekc_white", neck="ekc_white", wing="ekc_grey", bar="ekc_grey", tail=["ekc_grey", "ekc_white"])

BIRDS = [
    ("rooster", lambda: adult("rooster", 0.82, RED, True), "The red rooster: gold hackle, green-black sickle tail."),
    ("rooster_white", lambda: adult("rooster_white", 0.82, PUTI, True), "A white rooster with a dark tail."),
    ("hen", lambda: adult("hen", 0.74, BROWN, False), "A brown hen."),
    ("hen_white", lambda: adult("hen_white", 0.74, WHITE, False), "A white hen."),
    ("chick", lambda: chick("chick"), "A yellow chick."),
]

PART_NAMES = ("torso", "body", "head", "wing_l", "wing_r", "leg_l", "leg_r")


def bounds(bird):
    lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    bpy.context.view_layer.update()
    for o in bird.col.objects:
        if o.type != "MESH":
            continue
        for v in o.data.vertices:
            p = o.matrix_world @ v.co
            lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
            hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
    return lo, hi


def export(bird, others):
    """The exporter writes object names: this bird's parts get the plain names for the export,
    everyone else's are moved aside first (Blender keeps names unique across the file)."""
    for other in others:
        for key, o in other.obj.items():
            o.name = f"{other.name}__{key}"
            if o.data:
                o.data.name = f"{other.name}__{key}"
    for key, o in bird.obj.items():
        o.name = key
        if o.data:
            o.data.name = f"{bird.name}_{key}"
    bpy.context.view_layer.update()
    mine = set(bird.col.objects)
    for o in bpy.context.view_layer.objects:
        o.select_set(o in mine)
    bpy.context.view_layer.objects.active = bird.root
    path = MODELS / f"chicken_{bird.name}.glb"
    bpy.ops.export_scene.gltf(filepath=str(path), export_format="GLB", use_selection=True, export_yup=True,
                              export_apply=True, export_animations=False, export_image_format="NONE",
                              export_materials="EXPORT")
    tris = 0
    used = []
    for o in bird.col.objects:
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


# Poses for the sheet, in Blender's axes, degrees: torso/head/leg about X (+ tips the front DOWN,
# swings a leg BACK), head about Z (a look to the side), wings about Y (wing_l lifts with a
# NEGATIVE angle, wing_r with a positive one). This is the check that the joints are where the
# Unity code will turn them.
POSES = {
    "peck": {"torso": ("X", 34), "head": ("X", 46), "leg_l": ("X", 0), "leg_r": ("X", 0)},
    "flap": {"torso": ("X", -14), "wing_l": ("Y", -78), "wing_r": ("Y", 78), "leg_l": ("X", 30), "leg_r": ("X", -30)},
    "look": {"head": ("Z", 55)},
    "walk": {"leg_l": ("X", -28), "leg_r": ("X", 28), "torso": ("X", 4)},
    "crow": {"torso": ("X", -20), "head": ("X", -34), "wing_l": ("Y", -16), "wing_r": ("Y", 16)},
}


def pose(bird, name):
    for o in bird.obj.values():
        o.rotation_euler = (0, 0, 0)
    for key, (axis, deg) in POSES.get(name, {}).items():
        e = [0.0, 0.0, 0.0]
        e["XYZ".index(axis)] = math.radians(deg)
        bird.obj[key].rotation_euler = e


def review(version, birds, records):
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
    by = {b.name: b for b in birds}
    views = {"front": Vector((-0.55, -1.0, 0.4)), "side": Vector((-1.0, -0.1, 0.16)), "back": Vector((0.6, 1.0, 0.45)),
             "top": Vector((-0.2, -0.45, 1.0))}
    shots = []
    for b in birds:
        shots += [(b.name, "rest", "front"), (b.name, "rest", "side")]
    shots += [("rooster", "rest", "back"), ("rooster", "peck", "side"), ("rooster", "flap", "front"), ("rooster", "crow", "side"),
              ("rooster", "walk", "side"), ("hen", "peck", "front"), ("hen", "look", "front"), ("hen_white", "flap", "back"),
              ("chick", "flap", "front"), ("chick", "peck", "side"), ("rooster", "flap", "top"), ("hen", "rest", "top")]
    tiles = []
    for name, pname, vname in shots:
        b = by[name]
        for other in birds:
            other.col.hide_render = other is not b
        pose(b, pname)
        lo, hi = bounds(b)
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
        pose(b, "rest")
        s = records[name]["size"]
        tiles.append({"file": str(path), "label": f"chicken_{name}  {pname}  {vname}",
                      "sub": f"{s[0]:.2f} x {s[1]:.2f} x {s[2]:.2f} m   {records[name]['tris']} tris"})
    for other in birds:
        other.col.hide_render = False
    spec = TILES / "sheet.json"
    spec.write_text(json.dumps({"title": f"Eskinita Alley chickens v{version}", "cols": 6, "tiles": tiles}))
    subprocess.run(["py", "-3", str(PAINTER), "--compose", str(spec), str(LOGS / f"chickens_sheet_v{version}.png")], check=False)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--sheet") + 1]) if "--sheet" in argv else None
    for d in (MODELS, SOURCE, LOGS):
        d.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    birds = [fn() for _name, fn, _note in BIRDS]
    setup_light()

    records, used_all = {}, []
    for b in birds:
        path, tris, used = export(b, [o for o in birds if o is not b])
        lo, hi = bounds(b)
        size = [round(hi.x - lo.x, 3), round(hi.y - lo.y, 3), round(hi.z - lo.z, 3)]
        records[b.name] = dict(tris=tris, size=size)
        used_all += [u for u in used if u not in used_all]
        joints = {k: [round(c, 3) for c in o.matrix_world.translation] for k, o in b.obj.items()}
        print(f"[eskinita chickens] chicken_{b.name:14s} {tris:5d} tris  {size[0]:.2f} x {size[1]:.2f} x {size[2]:.2f}  "
              f"{path.stat().st_size} bytes  low z {lo.z:.3f}  joints {joints}")
    # Leave the file with every part named for its bird, so nothing in it is called body.003.
    for b in birds:
        for key, o in b.obj.items():
            o.name = f"{b.name}__{key}"
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "chickens.blend"))
    mats = [{"name": n, "texture": "", "tint": [round(c, 4) for c in srgb(MATS[n])], "tiling": 1.0,
             "foliage": False, "emissive": False, "glossy": False, "cutout": False} for n in sorted(used_all)]
    (ART / "materials_chickens.json").write_text(json.dumps(
        {"tint_space": "sRGB 0..1, flat colours (no textures)",
         "parts": "root chicken_<name> (origin on the ground); leg_l, leg_r (hip); torso (empty, hip centre) > body, head (neck base), wing_l, wing_r (shoulder). Unity: faces +Z, wing_l is on -X.",
         "materials": mats}, indent=1))
    print(f"[eskinita chickens] {len(birds)} birds, {len(mats)} materials, {sum(r['tris'] for r in records.values())} triangles")
    if version is not None:
        review(version, birds, records)


main()
