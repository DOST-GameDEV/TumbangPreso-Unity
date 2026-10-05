"""The rescue drone's CONCEPTS, as quick blockouts side by side (owner, 2026-10-05: "drone design and
ufo effect needs to be more stylized").

  blender -b --python tools/review_arena_drone_options.py -- --version=v1

Writes Logs/arena/stage/drone_options_<vN>.png: three blockouts in flat colours, close (three-quarter
from a little below, as the carried player and the break camera see it) and at game distance (18 m,
beside a 1.8 m figure, with the silhouette alone under it). Nothing here is the kit: the one chosen
is built properly in tools/author_arena_stage.py.

  A  PLATITO   a toy flying saucer: glass dome, a ring of bulbs, a ball antenna, three ball feet.
  B  BANTAY    a referee: a ball in a striped cap with one big eye, a whistle, mitts, a halo ring.
  C  SAGIP     a chubby rescue bot wearing a lifebuoy (salbabida), a ceiling fan for a rotor, a
               jeepney nameplate on its brow, a two-eyed screen face and a crane-game claw.
"""
import bpy, bmesh, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "Logs", "arena", "stage")

CREAM, GOLD, RED, NAVY, ICE, DARK, GLASS = (0.95, 0.92, 0.82), (1.0, 0.62, 0.07), (0.76, 0.02, 0.08), (0.006, 0.012, 0.04), (0.25, 0.9, 1.0), (0.03, 0.04, 0.08), (0.35, 0.85, 0.95)
_m = {}


def mat(rgb, glow=0.45):
    key = (rgb, glow)
    if key not in _m:
        m = bpy.data.materials.new("flat")
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        b.inputs["Base Color"].default_value = (*rgb, 1)
        b.inputs["Roughness"].default_value = 0.85
        b.inputs["Emission Color"].default_value = (*rgb, 1)
        b.inputs["Emission Strength"].default_value = glow
        _m[key] = m
    return _m[key]


class Block:
    def __init__(self, name):
        self.bm = bmesh.new(); self.name = name; self.mats = []

    def _mi(self, m):
        if m not in self.mats:
            self.mats.append(m)
        return self.mats.index(m)

    def lathe(self, prof, colour, sides=24, at=(0, 0, 0), glow=0.12, tilt=None):
        mi = self._mi(mat(colour, glow))
        o = Vector(at)
        cols = []
        for i in range(sides):
            a = 2 * math.pi * i / sides
            col = []
            for r, z in prof:
                p = Vector((r * math.sin(a), r * math.cos(a), z))
                if tilt:
                    p = tilt @ p
                col.append(self.bm.verts.new(o + p))
            cols.append(col)
        n = len(prof)
        for i in range(sides):
            p, q = cols[i], cols[(i + 1) % sides]
            for j in range(n - 1):
                try:
                    self.bm.faces.new((p[j], p[j + 1], q[j + 1], q[j])).material_index = mi
                except ValueError:
                    pass
        return self

    def ball(self, r, at, colour, glow=0.12, squash=1.0, sides=14):
        prof = [(max(1e-4, r * math.sin(math.pi * k / 8)), r * squash * math.cos(math.pi * k / 8)) for k in range(9)]
        return self.lathe(prof, colour, sides, at, glow)

    def box(self, centre, half, colour, yaw=0.0, glow=0.12, roll=0.0):
        mi = self._mi(mat(colour, glow))
        c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        cr, sr = math.cos(math.radians(roll)), math.sin(math.radians(roll))
        vs = []
        for sx in (-1, 1):
            for sy in (-1, 1):
                for sz in (-1, 1):
                    x, y, z = sx * half[0], sy * half[1], sz * half[2]
                    x, z = x * cr + z * sr, -x * sr + z * cr
                    vs.append(self.bm.verts.new((centre[0] + x * c + y * s, centre[1] - x * s + y * c, centre[2] + z)))
        for f in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
            self.bm.faces.new([vs[i] for i in f]).material_index = mi
        return self

    def torus(self, R, r, z, colours, sides=24, tube=8, glow=0.12):
        ring = []
        for i in range(sides):
            a = 2 * math.pi * i / sides
            ring.append([self.bm.verts.new(((R + r * math.cos(2 * math.pi * k / tube)) * math.sin(a), (R + r * math.cos(2 * math.pi * k / tube)) * math.cos(a),
                                            z + r * math.sin(2 * math.pi * k / tube))) for k in range(tube)])
        for i in range(sides):
            mi = self._mi(mat(colours[(i * 8 // sides) % len(colours)], glow))
            p, q = ring[i], ring[(i + 1) % sides]
            for k in range(tube):
                self.bm.faces.new((p[k], q[k], q[(k + 1) % tube], p[(k + 1) % tube])).material_index = mi
        return self

    def done(self, at):
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        for f in self.bm.faces:
            f.smooth = True
        me = bpy.data.meshes.new(self.name); self.bm.to_mesh(me); self.bm.free()
        for m in self.mats:
            me.materials.append(m)
        ob = bpy.data.objects.new(self.name, me)
        ob.location = at
        bpy.context.scene.collection.objects.link(ob)
        for p in me.polygons:
            p.use_smooth = True
        mod = ob.modifiers.new("edge", "EDGE_SPLIT"); mod.split_angle = math.radians(40)
        return ob


def platito(at):
    b = Block("A platito")
    b.lathe([(0.001, 0.10), (0.42, 0.10), (0.74, 0.0), (0.80, -0.05), (0.70, -0.12), (0.34, -0.22), (0.001, -0.22)], CREAM)
    b.lathe([(0.001, 0.50), (0.20, 0.46), (0.33, 0.34), (0.38, 0.18), (0.38, 0.08), (0.001, 0.08)], GLASS, glow=0.5)
    b.lathe([(0.39, 0.13), (0.44, 0.13), (0.44, 0.07), (0.39, 0.07)], GOLD)
    for k in range(10):
        a = math.radians(36 * k)
        b.ball(0.06, (0.60 * math.sin(a), 0.60 * math.cos(a), 0.035), GOLD, glow=2.0, sides=8)
    b.lathe([(0.001, 0.76), (0.012, 0.76), (0.012, 0.48), (0.001, 0.48)], DARK, sides=6)
    b.ball(0.055, (0, 0, 0.78), RED, glow=1.5)
    b.lathe([(0.001, -0.20), (0.22, -0.20), (0.14, -0.32), (0.001, -0.32)], ICE, glow=2.0)
    for k in range(3):
        a = math.radians(120 * k + 60)
        b.ball(0.08, (0.52 * math.sin(a), 0.52 * math.cos(a), -0.22), GOLD)
    b.ball(0.05, (-0.10, 0.20, 0.30), NAVY); b.ball(0.05, (0.10, 0.20, 0.30), NAVY)       # the pilot's eyes in the dome
    return b.done(at)


def bantay(at):
    b = Block("B bantay")
    b.ball(0.44, (0, 0, 0), CREAM, sides=20)
    b.lathe([(0.001, 0.52), (0.30, 0.47), (0.44, 0.30), (0.455, 0.20), (0.001, 0.20)], DARK)                                # the cap
    for k in range(6):                                                                                                    # its stripes
        a = 60 * k
        b.box((0.33 * math.sin(math.radians(a)), 0.33 * math.cos(math.radians(a)), 0.36), (0.05, 0.10, 0.11), CREAM, yaw=a)
    b.box((0, 0.52, 0.22), (0.28, 0.16, 0.02), DARK)                                                                      # the visor
    b.ball(0.05, (0, 0, 0.53), GOLD)
    b.lathe([(0.001, 0.0), (0.19, 0.0), (0.19, 0.05), (0.001, 0.05)], NAVY, at=(0, 0.40, 0.02), tilt=None, sides=16)
    tilt = __import__("mathutils").Matrix.Rotation(math.radians(90), 3, "X")
    b.lathe([(0.001, 0.0), (0.20, 0.0), (0.20, 0.06), (0.001, 0.06)], NAVY, at=(0, 0.36, 0.02), tilt=tilt, sides=16)        # the eye's socket
    b.lathe([(0.001, 0.0), (0.12, 0.0), (0.12, 0.08), (0.001, 0.08)], ICE, at=(0, 0.345, 0.02), tilt=tilt, sides=16, glow=2.5)
    b.box((0, 0.42, -0.26), (0.10, 0.06, 0.06), GOLD)                                                                     # the whistle
    b.lathe([(0.001, 0.07), (0.085, 0.07), (0.085, -0.07), (0.001, -0.07)], GOLD, at=(0.0, 0.36, -0.30), tilt=__import__("mathutils").Matrix.Rotation(math.radians(90), 3, "Y"), sides=12)
    for sx in (-1, 1):                                                                                                    # stubby arms and mitts
        b.box((sx * 0.52, 0.02, -0.14), (0.10, 0.07, 0.07), DARK, roll=sx * 30)
        b.ball(0.12, (sx * 0.66, 0.04, -0.24), RED)
    b.torus(0.62, 0.035, 0.72, [GOLD], glow=2.0)                                                                          # the halo
    b.lathe([(0.001, -0.36), (0.20, -0.36), (0.12, -0.50), (0.001, -0.50)], ICE, glow=2.0)
    return b.done(at)


def sagip(at):
    b = Block("C sagip")
    b.lathe([(0.001, 0.40), (0.16, 0.39), (0.30, 0.33), (0.40, 0.22), (0.455, 0.06), (0.46, -0.08), (0.42, -0.22), (0.33, -0.33), (0.30, -0.36), (0.001, -0.36)], CREAM)
    b.torus(0.50, 0.115, -0.10, [RED, CREAM])                                                                             # the salbabida
    # the screen face: a dark band across the front with two eyes
    for k in range(-3, 4):
        a = k * 11
        r = 0.455
        b.box((r * math.sin(math.radians(a)), r * math.cos(math.radians(a)), 0.07), (0.05, 0.012, 0.12), NAVY, yaw=a)
    for sx in (-1, 1):
        a = sx * 15
        b.ball(0.07, (0.455 * math.sin(math.radians(a)), 0.455 * math.cos(math.radians(a)), 0.08), ICE, glow=3.0, squash=1.2, sides=10)
    for k in range(-3, 4):                                                                                                # the nameplate on its brow
        a = k * 10
        b.box((0.42 * math.sin(math.radians(a)), 0.42 * math.cos(math.radians(a)), 0.27), (0.045, 0.05, 0.055), GOLD, yaw=a, glow=0.5)
    for sx in (-1, 1):                                                                                                    # the ear lamps
        b.ball(0.085, (sx * 0.44, 0.0, 0.14), GOLD, glow=1.8)
    b.lathe([(0.001, 0.62), (0.045, 0.62), (0.045, 0.36), (0.001, 0.36)], DARK, sides=8)                                   # the fan's stalk
    b.lathe([(0.001, 0.78), (0.09, 0.77), (0.14, 0.72), (0.14, 0.66), (0.08, 0.62), (0.001, 0.62)], GOLD)
    for k in range(3):
        a = 120 * k + 20
        b.box((0.46 * math.sin(math.radians(a)), 0.46 * math.cos(math.radians(a)), 0.70), (0.13, 0.34, 0.014), CREAM, yaw=a)
        b.box((0.76 * math.sin(math.radians(a)), 0.76 * math.cos(math.radians(a)), 0.70), (0.13, 0.05, 0.016), RED, yaw=a)
    for k in range(3):                                                                                                    # the crane-game claw
        a = 120 * k + 60
        b.box((0.36 * math.sin(math.radians(a)), 0.36 * math.cos(math.radians(a)), -0.42), (0.045, 0.045, 0.11), GOLD, yaw=a, roll=0)
        b.box((0.33 * math.sin(math.radians(a)), 0.33 * math.cos(math.radians(a)), -0.56), (0.04, 0.07, 0.035), GOLD, yaw=a)
    b.lathe([(0.001, -0.34), (0.19, -0.34), (0.15, -0.40), (0.001, -0.40)], ICE, glow=2.0)
    b.lathe([(0.001, 0.0), (0.016, 0.0), (0.016, 0.26), (0.001, 0.26)], DARK, at=(-0.10, -0.27, 0.30), sides=6)            # the antenna
    b.ball(0.055, (-0.10, -0.27, 0.58), RED, glow=2.0)
    return b.done(at)


def figure(at):
    b = Block("figure")
    b.lathe([(0.001, 1.8), (0.11, 1.76), (0.125, 1.66), (0.09, 1.55), (0.07, 1.50), (0.21, 1.44), (0.23, 1.05), (0.19, 0.92), (0.17, 0.45), (0.13, 0.02), (0.001, 0.0)], (0.35, 0.38, 0.45), sides=12)
    return b.done(at)


def shoot(path, loc, target, lens, res):
    scene = bpy.context.scene
    cd = bpy.data.cameras.new("c"); cam = bpy.data.objects.new("c", cd)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cd.lens = lens; cd.clip_start = 0.05; cd.clip_end = 500
    scene.camera = cam
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


def main():
    version = "v1"
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
    bpy.ops.wm.read_factory_settings()
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    scene = bpy.context.scene
    made = [platito((2.6, 0, 3.0)), bantay((0, 0, 3.0)), sagip((-2.6, 0, 3.0))]      # the cameras look south: +x is on the left
    for x in (-2.6, 0, 2.6):
        figure((x - 1.05, 0, 0))
    floor = Block("floor"); floor.box((0, 0, -0.05), (40, 40, 0.05), (0.10, 0.12, 0.17), glow=0.02); floor.done((0, 0, 0))
    key = bpy.data.objects.new("key", bpy.data.lights.new("key", "SUN"))
    key.data.energy = 3.4; key.data.color = (0.9, 0.95, 1.0); key.rotation_euler = (math.radians(50), math.radians(-18), math.radians(25))
    scene.collection.objects.link(key)
    world = bpy.data.worlds.new("night"); scene.world = world; world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.02, 0.03, 0.08, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.view_settings.view_transform = "Standard"
    os.makedirs(LOGS, exist_ok=True)
    tmp = os.path.join(LOGS, "_drone_options_%s_" % version)
    shoot(tmp + "close.png", (-1.6, 9.5, 2.4), (0, 0, 2.95), 40, (2400, 800))
    shoot(tmp + "under.png", (-0.8, 6.4, 0.3), (0, 0, 3.0), 30, (2400, 800))
    shoot(tmp + "far.png", (-2.0, 19.0, 1.7), (0, 0, 2.3), 62, (2400, 800))
    for ob in made:                                                    # the silhouettes: every colour to white on black
        for i in range(len(ob.data.materials)):
            ob.data.materials[i] = mat((1.0, 1.0, 1.0), 4.0)
    for ob in scene.objects:
        if ob.name.startswith("figure") or ob.name == "floor":
            ob.hide_render = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0, 0, 0, 1)
    key.hide_render = True
    shoot(tmp + "silhouette.png", (0.0, 19.0, 3.0), (0, 0, 3.05), 78, (2400, 420))
    print("DRONE_OPTIONS_OK", version)


if __name__ == "__main__":
    main()
