"""The arena's crowd kit: animated 2D sprites of the game's own people, packed into one atlas.

Read docs/ARENA_ART_BRIEF.md first. The owner: "for the crowd i was thinking of 2d animated
sprites/gifs of people/generic character models we have".

Two halves in one file:

  RENDER (Blender, headless):
      blender -b --python tools/author_arena_crowd.py -- --version=v4
    loads the twelve everyday rigs (character-male-a..f, character-female-a..f) with their own
    palettes (MapSource/materials_persons) and with garment recolours (only the garment slots are
    rewritten, the way IlalimSidewalkAuthor dresses its passers-by; skin, hair and slot 8, the
    face, stay the rig's), poses every one of them through the loops below and renders one strip
    per frame (all people side by side) with an orthographic camera placed where the stage is:
    in front and slightly below. Then it runs the second half with the system Python.

  PACK AND MOCK (system Python with numpy and Pillow; `py` on this machine):
      py tools/author_arena_crowd.py --stage=pack --version=v4
    inks the strips, shrinks them to the cell size, writes
      Assets/TumbangPreso/Art/Arena/Textures/arena_crowd_atlas.png        (colour, alpha is the cutout)
      Assets/TumbangPreso/Art/Arena/Textures/arena_crowd_atlas_emit.png   (what glows: sticks, phones, flags)
      tools/arena_crowd_atlas.json                                        (people, loops, frames, cell rects)
      tools/arena_crowd_rows_fallback.json                                (the rows, derived, until the bowl kit writes tools/arena_rows.json)
    and the review pictures in Logs/arena/crowd/<version>/: the atlas, contact sheets, and a mock
    stand filled with sprites at true scale from the stage and from the air, drawn by a small
    software copy of ArenaCrowd.shader's own rules (same loop choice, same night tint, alpha test).

THE CELL SIZE IS MEASURED, NOT GUESSED (see `screen_sizes`): at 1080p with a 60 degree vertical
field of view a 1.65 m spectator is 18 px tall in the first row (85 m), 25 px from the stage's
edge (63 m), and 8 px in the top row (176 m out, 58 m up). A 48 x 72 px cell covering
1.8 x 2.7 m (room for a jump, a raised stick and a flag with a 2 px clear band all round) draws
the figure 44 px tall: 1.8 times the nearest 1080p view, 0.9 times the nearest 4K view. A
64 x 96 cell would be 2.4 times the nearest 1080p view and would cut the people from 42 to 24.
"""
import json, math, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PERSONS = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "characters", "persons")
PALETTES = os.path.join(ROOT, "MapSource", "materials_persons")
TEXTURES = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Arena", "Textures")
ATLAS = os.path.join(TEXTURES, "arena_crowd_atlas.png")
ATLAS_EMIT = os.path.join(TEXTURES, "arena_crowd_atlas_emit.png")
LAYOUT = os.path.join(ROOT, "tools", "arena_crowd_atlas.json")
ROWS_FILE = os.path.join(ROOT, "tools", "arena_rows.json")
ROWS_FALLBACK = os.path.join(ROOT, "tools", "arena_crowd_rows_fallback.json")
SYSTEM_PYTHON = "py"

# ---------------------------------------------------------------- the cell, from the screen
CELL_PX = (48, 72)               # one frame of one person
CELL_M = (1.8, 2.7)              # what it covers in the world: 26.7 px per metre
GROUND_M = 0.20                  # the row's floor sits this far above the cell's bottom edge
ATLAS_PX = 2048
SUPER = 4                        # the strips are rendered this many times larger, then shrunk
RIG_SCALE = 2.38                 # the cast's scale: a 0.69 unit rig is a 1.65 m person
VIEW_PITCH = 8.0                 # degrees the stage looks UP at a spectator (0 in row one, 17 in the top row)
FIGURE_M = 1.65

# name, frames, frames per second calm, frames per second excited. THE ORDER IS THE ATLAS'S ROWS
# and ArenaCrowd.shader's `LoopStart`/`LoopCount` tables: change one, change the other.
LOOPS = (("idle", 2, 1.2, 2.0), ("clap", 2, 5.0, 8.0), ("cheer", 4, 7.0, 10.0), ("jump", 4, 7.0, 9.0),
         ("stick", 4, 4.0, 6.0), ("stick_up", 4, 7.0, 10.0), ("flag", 4, 5.0, 8.0), ("groan", 4, 3.0, 4.0))
FRAMES = sum(l[1] for l in LOOPS)

# rig file, palette file. The twelve everyday people of RosterBookBuilder.
RIGS = (("character-male-f", "person_a"), ("character-female-f", "person_b"),
        ("character-male-a", "person_totoy"), ("character-female-a", "person_inday"),
        ("character-male-b", "person_kuya-boy"), ("character-female-b", "person_ate-girlie"),
        ("character-male-c", "person_tikboy"), ("character-female-c", "person_bebang"),
        ("character-male-d", "person_jun-jun"), ("character-female-d", "person_lola-pacing"),
        ("character-male-e", "person_mang-kanor"), ("character-female-e", "person_aling-nena"))
# Garment recolours (top, bottom), sRGB hex. None of them is near offence orange or defence blue.
DRESS = (None,
         ("e6dfcc", "2f3550"), ("7a3446", "3a3a44"), ("2c3566", "5a4636"), ("d8d8e2", "30405a"),
         ("3f6a4a", "4a4038"), ("53406e", "2a2c3c"), ("c9b24a", "3a3a44"), ("b85a6a", "2f3550"),
         ("3a7d86", "4a4038"), ("8a8f9c", "2a2c3c"), ("efe9dc", "7a3446"), ("26283a", "6b6444"))
OFFENCE, DEFENCE = (0xf8 / 255, 0x70 / 255, 0x20 / 255), (0x00, 0x80 / 255, 0xe8 / 255)


def people():
    """Forty-two people: the twelve rigs as they are, then every rig in two garment recolours,
    then six more recolours. (rig index, dress index)."""
    out = [(r, 0) for r in range(12)]
    out += [(r, 1 + (r * 5 + 0) % 12) for r in range(12)]
    out += [(r, 1 + (r * 5 + 7) % 12) for r in range(12)]
    out += [(r, 1 + (r * 5 + 3) % 12) for r in (1, 2, 5, 6, 9, 10)]
    return out


def arg(name, default):
    for a in sys.argv:
        if a.startswith("--%s=" % name):
            return a.split("=", 1)[1]
    return default


VERSION = arg("version", "v1")
OUT = os.path.join(ROOT, "Logs", "arena", "crowd", VERSION)
STRIPS = os.path.join(OUT, "strips")


def screen_sizes(height=1080, fov=60.0, eye_z=3.3):
    """How tall a spectator is on screen, in pixels, seen from the stage."""
    f = (height / 2) / math.tan(math.radians(fov / 2))
    out = []
    for label, r, z in (("first row from the can", 85.3, 3.0), ("first row from the stage's edge (22 m out)", 63.3, 3.0),
                        ("lower bowl, top row", 129.2, 26.0), ("upper stand, first row", 141.9, 32.0),
                        ("upper stand, top row", 175.1, 58.0)):
        d = math.hypot(r, z - eye_z)
        out.append((label, d, FIGURE_M * f / d, CELL_M[1] * f / d))
    return out


# ================================================================ RENDER (Blender)
def render():
    import bpy, re
    from mathutils import Matrix, Quaternion, Vector
    sys.path.insert(0, os.path.join(ROOT, "tools"))
    import arena_kit                                               # the stadium's numbers

    os.makedirs(STRIPS, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    who = people()
    n = len(who)

    def srgb(hex6):
        return tuple(int(hex6[i:i + 2], 16) / 255 for i in (0, 2, 4))

    def palette(name):
        text = open(os.path.join(PALETTES, name + ".tres"), encoding="utf-8").read()
        v = [float(t) for t in re.search(r"PackedColorArray\(([^)]+)\)", text).group(1).split(",")]
        return [tuple(v[i:i + 3]) for i in range(0, 64, 4)]

    def near(c, d):
        return math.dist(c, d) < 0.22

    def slot_of(u, v):
        """The palette slot a UV names (Toon.shader's table, in the .glb's own rows)."""
        col, row = min(int(u * 16), 15), min(int((1 - v) * 16), 15)
        return -1 if row < 8 else (col // 2) + (8 if row >= 12 else 0)

    def toon(name, image=None, colour=None, flat=False):
        """The cast's look: a flat colour in two bands, the unlit band cooler (a night stadium)."""
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        nt = m.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        emit = nt.nodes.new("ShaderNodeEmission")
        nt.links.new(emit.outputs[0], out.inputs[0])
        if image is not None:
            base = nt.nodes.new("ShaderNodeTexImage")
            base.image = image
            base.interpolation = "Closest"
            base_out = base.outputs["Color"]
        else:
            base = nt.nodes.new("ShaderNodeRGB")
            c = [((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in colour]
            base.outputs[0].default_value = (c[0], c[1], c[2], 1)
            base_out = base.outputs[0]
        if flat:
            nt.links.new(base_out, emit.inputs["Color"])
            return m
        lit = nt.nodes.new("ShaderNodeBsdfDiffuse")
        lit.inputs["Color"].default_value = (1, 1, 1, 1)
        to_rgb = nt.nodes.new("ShaderNodeShaderToRGB")
        ramp = nt.nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.interpolation = "CONSTANT"
        e = ramp.color_ramp.elements
        e[0].position = 0.0; e[0].color = (SHADE[0], SHADE[1], SHADE[2], 1)
        e[1].position = 0.22; e[1].color = (1, 1, 1, 1)
        mix = nt.nodes.new("ShaderNodeMixRGB")
        mix.blend_type = "MULTIPLY"; mix.inputs[0].default_value = 1.0
        nt.links.new(lit.outputs[0], to_rgb.inputs[0])
        nt.links.new(to_rgb.outputs[0], ramp.inputs[0])
        nt.links.new(base_out, mix.inputs[1]); nt.links.new(ramp.outputs[0], mix.inputs[2])
        nt.links.new(mix.outputs[0], emit.inputs["Color"])
        return m

    black = toon("crowd_black", colour=(0, 0, 0), flat=True)
    white = toon("crowd_glow", colour=(1, 1, 1), flat=True)
    grey = toon("crowd_glow_flag", colour=(FLAG_GLOW,) * 3, flat=True)
    stick_mat = toon("crowd_stick", colour=(0.96, 0.98, 1.0), flat=True)
    grip_mat = toon("crowd_grip", colour=(0.30, 0.32, 0.40))
    phone_mat = toon("crowd_phone", colour=(0.92, 0.96, 1.0), flat=True)
    cloth_mat = toon("crowd_flag_cloth", colour=(0.90, 0.92, 0.97))
    band_mat = toon("crowd_flag_band", colour=(0.13, 0.16, 0.42))
    pole_mat = toon("crowd_flag_pole", colour=(0.55, 0.58, 0.66))

    def skinned(name, arm, bone, verts, faces, mats, face_mats):
        me = bpy.data.meshes.new(name)
        me.from_pydata(verts, [], faces)
        for m in mats:
            me.materials.append(m)
        for p, k in zip(me.polygons, face_mats):
            p.material_index = k
        ob = bpy.data.objects.new(name, me)
        scene.collection.objects.link(ob)
        ob.parent = arm
        g = ob.vertex_groups.new(name=bone)
        g.add(range(len(verts)), 1.0, "REPLACE")
        mod = ob.modifiers.new("Armature", "ARMATURE")
        mod.object = arm
        return ob

    def box(lo, hi):
        (x0, y0, z0), (x1, y1, z1) = lo, hi
        v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
        f = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
        return v, f

    def join(parts):
        verts, faces, mats = [], [], []
        for (v, f), k in parts:
            base = len(verts)
            verts += v
            faces += [tuple(i + base for i in face) for face in f]
            mats += [k] * len(f)
        return verts, faces, mats

    # ------------------------------------------------------------ the people
    crowd = []
    for i, (rig, dress) in enumerate(who):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=os.path.join(PERSONS, RIGS[rig][0] + ".glb"))
        new = [o for o in bpy.data.objects if o not in before]
        arm = next(o for o in new if o.type == "ARMATURE")
        meshes = [o for o in new if o.type == "MESH" and o.parent == arm]
        for o in new:
            if o.type == "MESH" and o.parent != arm:
                bpy.data.objects.remove(o)                         # the importer's bone shape
        arm.animation_data_clear()
        arm.location = ((i - (n - 1) / 2) * CELL_M[0], 0, 0)
        arm.scale = (RIG_SCALE,) * 3
        pal = palette(RIGS[rig][1])
        # Which slots are garments: on the body, hardly on the head (skin and hair are on both),
        # and never slot 8 (the face's ink).
        area = {}
        for o in meshes:
            uv = o.data.uv_layers[0].data
            groups = {g.index: g.name for g in o.vertex_groups}
            for p in o.data.polygons:
                u = sum(uv[l].uv[0] for l in p.loop_indices) / p.loop_total
                v = sum(uv[l].uv[1] for l in p.loop_indices) / p.loop_total
                vg = o.data.vertices[p.vertices[0]].groups
                bone = groups[max(vg, key=lambda g: g.weight).group] if vg else "?"
                key = (slot_of(u, v), "head" if bone == "head" else "legs" if bone.startswith("leg") else "top")
                a = area.setdefault(key, [0.0, -9.0])
                a[0] += p.area; a[1] = max(a[1], p.center.z)
        def garment(s):
            body = area.get((s, "top"), [0, 0])[0] + area.get((s, "legs"), [0, 0])[0]
            return s not in (8, -1) and body > 4 * area.get((s, "head"), [0, 0])[0]
        tops = sorted((s for s in range(16) if garment(s) and (s, "top") in area), key=lambda s: -area[(s, "top")][0])
        top = tops[0] if tops else None
        bottoms = sorted((s for s in range(16) if garment(s) and s != top and (s, "legs") in area and area[(s, "legs")][1] > 0.09),
                         key=lambda s: -area[(s, "legs")][0])
        bottom = bottoms[0] if bottoms else None
        for s in range(16):                                        # no GARMENT in the stand is a team colour (skin and hair stay)
            if garment(s) and near(pal[s], OFFENCE):
                pal[s] = (0.62, 0.24, 0.22)
            if garment(s) and near(pal[s], DEFENCE):
                pal[s] = (0.22, 0.24, 0.50)
        if dress:
            if top is not None:
                pal[top] = srgb(DRESS[dress][0])
            if bottom is not None:
                pal[bottom] = srgb(DRESS[dress][1])
        img = bpy.data.images.new("crowd_palette_%02d" % i, 16, 16, alpha=False)
        px = [0.0] * (16 * 16 * 4)
        for row in range(16):                                      # the .glb's row; Blender's image is bottom up
            for col in range(16):
                s = (col // 2) + (8 if row >= 12 else 0) if row >= 8 else 8
                k = ((15 - row) * 16 + col) * 4
                px[k:k + 4] = (pal[s][0], pal[s][1], pal[s][2], 1.0)
        img.pixels = px
        body_mat = toon("crowd_person_%02d" % i, image=img)
        for o in meshes:
            o.data.materials.clear()
            o.data.materials.append(body_mat)
        # What they hold, in the rig's own units, built along the right arm's rest line (it points
        # along -x from the shoulder at x -0.1; the hand is at x -0.37) and skinned to that arm.
        hx, hy, hz = -0.375, 0.017, 0.288
        v, f, k = join([(box((hx - 0.04, hy - 0.018, hz - 0.018), (hx + 0.03, hy + 0.018, hz + 0.018)), 1),
                        (box((hx - 0.27, hy - 0.028, hz - 0.028), (hx - 0.04, hy + 0.028, hz + 0.028)), 0)])
        stick = skinned("stick_%02d" % i, arm, "arm-right", v, f, [stick_mat, grip_mat], k)
        v, f, k = join([(box((hx - 0.10, hy - 0.014, hz - 0.040), (hx + 0.005, hy + 0.014, hz + 0.040)), 0)])
        phone = skinned("phone_%02d" % i, arm, "arm-right", v, f, [phone_mat], k)
        flags = []
        for wave in (0, 1):                                        # two ripples of the cloth, alternated
            parts = [(box((hx - 0.30, hy - 0.009, hz - 0.009), (hx + 0.03, hy + 0.009, hz + 0.009)), 2)]
            x0, x1 = hx - 0.29, hx - 0.07
            for seg in range(5):
                za, zb = hz + 0.008 + seg * 0.05, hz + 0.008 + (seg + 1) * 0.05
                ya = hy + (0.022 if (seg + wave) % 2 else -0.022)
                yb = hy + (0.022 if (seg + wave + 1) % 2 else -0.022)
                droop = 0.012 * seg * (1 if wave else -1)
                verts = [(x0 + droop, ya, za), (x1 + droop, ya, za), (x1 + droop * 1.4, yb, zb), (x0 + droop * 1.4, yb, zb)]
                parts.append(((verts, [(0, 1, 2, 3)]), 1 if seg in (1, 2) else 0))
            v, f, k = join(parts)
            flags.append(skinned("flag_%02d_%d" % (i, wave), arm, "arm-right", v, f, [cloth_mat, band_mat, pole_mat], k))
        crowd.append({"arm": arm, "meshes": meshes, "mat": body_mat, "stick": stick, "phone": phone, "flags": flags,
                      "rig": rig, "dress": dress, "top": top, "bottom": bottom})
        print("person %02d %s dress %d top slot %s bottom slot %s" % (i, RIGS[rig][0], dress, top, bottom))

    # ------------------------------------------------------------ light and camera
    sun = bpy.data.objects.new("floodlight", bpy.data.lights.new("floodlight", "SUN"))
    sun.data.energy = 4.0
    sun.data.angle = math.radians(1.0)
    scene.collection.objects.link(sun)
    sun.rotation_mode = "QUATERNION"
    sun.rotation_quaternion = Vector(LIGHT_FROM).normalized().to_track_quat("Z", "Y")
    scene.world = bpy.data.worlds.new("night")
    scene.world.use_nodes = True
    scene.world.node_tree.nodes["Background"].inputs[1].default_value = 0.0
    p = math.radians(VIEW_PITCH)
    fwd, up = Vector((0, math.cos(p), math.sin(p))), Vector((0, -math.sin(p), math.cos(p)))
    cam = bpy.data.objects.new("stage", bpy.data.cameras.new("stage"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = n * CELL_M[0]
    cam.data.clip_start, cam.data.clip_end = 0.1, 100.0
    cam.location = up * (CELL_M[1] / 2 - GROUND_M) - fwd * 20.0
    cam.rotation_euler = (math.radians(90.0 + VIEW_PITCH), 0, 0)
    scene.collection.objects.link(cam)
    scene.camera = cam
    for engine in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            pass
    scene.render.resolution_x, scene.render.resolution_y = n * CELL_PX[0] * SUPER, CELL_PX[1] * SUPER
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.filter_size = 0.8
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    try:
        scene.eevee.taa_render_samples = 8
    except AttributeError:
        pass

    # ------------------------------------------------------------ the poses
    def rot(axis, degrees):
        return Matrix.Rotation(math.radians(degrees), 3, axis)

    def aim(side, x, y, z):
        """An arm pointing along (x out to its own side, y forward, z up) in the body's frame."""
        rest = Vector((1.0 if side == "L" else -1.0, 0, 0))
        to = Vector(((x if side == "L" else -x), -y, z)).normalized()
        return rest.rotation_difference(to).to_matrix()

    def pose(loop, t, person):
        """Bone rotations (in the rig's own axes: x the body's left, -y its front, z up) and the
        root's lift in rig units for frame `t` of a loop. Every bone is one rigid block."""
        D = {"leg-left": rot("X", 0), "leg-right": rot("X", 0), "torso": rot("X", 0), "head": rot("X", 0),
             "arm-left": aim("L", 0.20, 0.10, -0.97), "arm-right": aim("R", 0.20, 0.10, -0.97)}
        lift, hold = 0.0, None
        seat = lambda: D.update({"leg-left": rot("X", -78) @ rot("Y", -5), "leg-right": rot("X", -78) @ rot("Y", 5)})
        if loop == "idle":
            seat()
            s = (-1, 1)[t]
            D["torso"] = rot("Y", 3.0 * s)
            D["head"] = rot("Y", 5.0 * s) @ rot("Z", 9.0 * s)
            D["arm-left"] = aim("L", 0.16, 0.62, -0.77); D["arm-right"] = aim("R", 0.16, 0.62, -0.77)
        elif loop == "clap":
            seat()
            open_ = t == 0
            D["torso"] = rot("X", 5 if open_ else 8)
            D["head"] = rot("X", -6)
            a = (0.38, 0.86, 0.34) if open_ else (-0.30, 0.86, 0.40)
            D["arm-left"] = aim("L", *a); D["arm-right"] = aim("R", *a)
        elif loop == "cheer":
            s = (1, -1, 1, -1)[t]
            lift = (0.0, 0.05, 0.0, 0.05)[t]
            D["torso"] = rot("X", -6) @ rot("Y", 5.0 * s)
            D["head"] = rot("X", -12) @ rot("Y", 6.0 * s)
            wide = (0.40, 0.24, 0.40, 0.24)[t]
            D["arm-left"] = aim("L", wide + 0.06 * s, 0.10, 0.88); D["arm-right"] = aim("R", wide - 0.06 * s, 0.10, 0.88)
            D["leg-left"] = rot("Y", -7); D["leg-right"] = rot("Y", 7)
        elif loop == "jump":
            lift = (0.0, 0.11, 0.18, 0.07)[t]
            D["torso"] = rot("X", (16, -4, -8, 4)[t])
            D["head"] = rot("X", (10, -10, -16, -4)[t])
            a = ((0.30, -0.45, -0.84), (0.36, 0.30, 0.88), (0.46, 0.05, 0.88), (0.44, 0.35, 0.70))[t]
            D["arm-left"] = aim("L", *a); D["arm-right"] = aim("R", *a)
            splay = (4, 6, 13, 8)[t]
            D["leg-left"] = rot("Y", -splay) @ rot("X", (0, 0, 14, 0)[t]); D["leg-right"] = rot("Y", splay) @ rot("X", (0, 0, -18, 0)[t])
        elif loop == "stick":
            seat()
            s = (-1, 0, 1, 0)[t]
            hold = "phone" if person % 3 == 2 else "stick"
            D["torso"] = rot("Y", 6.0 * s)
            D["head"] = rot("Y", 6.0 * s) @ rot("X", -6)
            D["arm-left"] = aim("L", 0.16, 0.62, -0.77)
            D["arm-right"] = aim("R", 0.10 + 0.22 * s, 0.22, 0.94) if hold == "stick" else aim("R", 0.20 + 0.14 * s, 0.50, 0.82)
        elif loop == "stick_up":
            hold = "phone" if person % 3 == 2 else "stick"
            s = (1, 0, -1, 0)[t]
            lift = (0.0, 0.05, 0.0, 0.05)[t]
            D["torso"] = rot("X", -4) @ rot("Y", 5.0 * s)
            D["head"] = rot("X", -10) @ rot("Y", 5.0 * s)
            D["arm-right"] = aim("R", 0.10 + 0.20 * s, (0.15, 0.50, 0.15, 0.50)[t], (0.96, 0.84, 0.96, 0.84)[t])
            D["arm-left"] = aim("L", (0.26, 0.34, 0.26, 0.34)[t], 0.45, (0.30, 0.86, 0.30, 0.86)[t])
            D["leg-left"] = rot("Y", -6); D["leg-right"] = rot("Y", 6)
        elif loop == "flag":
            hold = "flag%d" % (t % 2)
            s = (-1.0, -0.3, 1.0, 0.3)[t]
            D["torso"] = rot("Y", 5.0 * s) @ rot("X", -4)
            D["head"] = rot("X", -10) @ rot("Y", 5.0 * s)
            D["arm-right"] = aim("R", 0.02 + 0.16 * s, 0.12, 0.97)
            D["arm-left"] = aim("L", 0.26, 0.40, 0.70 if t % 2 else 0.25)
            D["leg-left"] = rot("Y", -8); D["leg-right"] = rot("Y", 8)
        elif loop == "groan":
            seat()
            D["torso"] = rot("X", (-8, 4, 15, 4)[t])
            D["head"] = rot("X", (-14, 4, 20, 4)[t]) @ rot("Z", (0, 20, 0, -20)[t])
            a = (0.20, 0.42, 0.88) if t != 2 else (0.10, 0.60, 0.79)
            D["arm-left"] = aim("L", *a); D["arm-right"] = aim("R", *a)
        return D, lift, hold

    black_all = [black]

    def shoot(path, glow):
        for c in crowd:
            for o in c["meshes"]:
                o.data.materials[0] = black if glow else c["mat"]
        stick_mat_now = (white, black) if glow else (stick_mat, grip_mat)
        for c in crowd:
            c["stick"].data.materials[0], c["stick"].data.materials[1] = stick_mat_now
            c["phone"].data.materials[0] = white if glow else phone_mat
            for fl in c["flags"]:
                mats = (grey, grey, black) if glow else (cloth_mat, band_mat, pole_mat)
                for k in range(3):
                    fl.data.materials[k] = mats[k]
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)

    row = 0
    for name, count, _, _ in LOOPS:
        for t in range(count):
            for i, c in enumerate(crowd):
                D, lift, hold = pose(name, t, i)
                arm = c["arm"]
                for pb in arm.pose.bones:
                    pb.rotation_mode = "QUATERNION"
                    rest = pb.bone.matrix_local.to_3x3()
                    pb.rotation_quaternion = (rest.inverted() @ D.get(pb.name, Matrix.Identity(3)) @ rest).to_quaternion()
                    pb.location = rest.inverted() @ Vector((0, 0, lift)) if pb.name == "root" else (0, 0, 0)
                c["stick"].hide_render = hold != "stick"
                c["phone"].hide_render = hold != "phone"
                c["flags"][0].hide_render = hold != "flag0"
                c["flags"][1].hide_render = hold != "flag1"
            bpy.context.view_layer.update()
            shoot(os.path.join(STRIPS, "row_%02d.png" % row), False)
            shoot(os.path.join(STRIPS, "row_%02d_emit.png" % row), True)
            print("rendered row %02d %s %d" % (row, name, t))
            row += 1

    # ------------------------------------------------------------ the rows, from the stadium's numbers
    def bank(name, r_in, r_out, z_in, z_out, count, sections):
        tread, rise = (r_out - r_in) / count, (z_out - z_in) / (count - 1)
        return {"name": name, "tread": round(tread, 4), "rise": round(rise, 4),
                "rows": [{"r": round(r_in + k * tread, 4), "z": round(z_in + k * rise, 4)} for k in range(count)],
                "sections": [[round(a, 3), round(b, 3)] for a, b in sections]}

    aisle = 1.6
    ring = [(k * 15.0 + aisle / 2, k * 15.0 + 15.0 - aisle / 2) for k in range(24)]
    banks = [bank("lower_front", 85.3, 106.0, 3.0, 12.6, 13, ring), bank("lower_back", 110.6, 130.0, 15.4, 26.0, 12, ring)]
    for lo, hi in arena_kit.UPPER_ARCS:
        w = (hi - lo) / 4
        secs = [(lo + k * w + (aisle / 2 if k else 0), lo + (k + 1) * w - (aisle / 2 if k < 3 else 0)) for k in range(4)]
        banks.append(bank("upper_%03d" % round(((lo + hi) / 2) % 360), 141.0, 176.0, 32.0, 58.0, arena_kit.UPPER_ROWS, secs))
    rows = {"note": "DERIVED by tools/author_arena_crowd.py from docs/ARENA_ART_BRIEF.md's table and the blockout "
                    "(tools/author_arena_stadium.py: 13 + 12 lower rows, arena_kit.UPPER_ROWS upper rows, 24 lower "
                    "sections, 4 per upper stand, 1.6 degree aisles). tools/arena_rows.json, when the bowl kit writes "
                    "it, replaces this. Blender frame: metres, z up, bearings in degrees clockwise from north (+y). "
                    "r and z are a row's FRONT edge and its floor; a spectator stands at r + 0.55 * tread.",
            "voids": [{"lo": 174.0, "hi": 186.0, "z_max": 6.2, "why": "the players' tunnel"}],
            "banks": banks}
    json.dump(rows, open(ROWS_FALLBACK, "w", encoding="utf-8"), indent=1)
    json.dump({"people": [{"rig": RIGS[c["rig"]][0], "palette": RIGS[c["rig"]][1], "dress": c["dress"],
                           "top_slot": c["top"], "bottom_slot": c["bottom"]} for c in crowd]},
              open(os.path.join(STRIPS, "people.json"), "w", encoding="utf-8"), indent=1)
    import subprocess
    subprocess.call([SYSTEM_PYTHON, os.path.abspath(__file__), "--stage=pack", "--version=" + VERSION])


# The unlit band's colour (linear multipliers: cooler and darker), where the floodlight comes from
# (toward the light, in Blender axes: the people face -y), and how much a flag glows.
SHADE = (0.20, 0.23, 0.36)
LIGHT_FROM = (-0.35, -0.75, 0.62)
FLAG_GLOW = 0.30

# ================================================================ PACK AND MOCK (system Python)
# What the shader does to a sprite, copied here so the mock shows the stand as the game will.
NIGHT_TINT = (0.30, 0.33, 0.50)        # ArenaCrowd.shader _NightTint
EMIT_GAIN = 1.0                        # _EmitStrength, before any bloom
GLOW = ((0.94, 0.97, 1.0), (0.62, 0.90, 1.0), (0.30, 0.40, 1.0), (1.0, 0.42, 0.80))
GLOW_SHARE = (0.46, 0.30, 0.19, 0.05)   # white, ice, deep LED blue, a rare magenta
NIGHT_GREY = 0.35                      # _NightDesaturate: how far a body is pulled to its own grey before the tint
TINTS = ((1.0, 1.0, 1.0), (0.86, 0.88, 0.96), (1.0, 0.94, 0.88), (0.78, 0.80, 0.90), (0.92, 1.0, 0.96),
         (0.70, 0.72, 0.84), (1.0, 0.90, 0.94), (0.88, 0.92, 1.0))
SEAT_PITCH = 0.95
FILL = 0.86
STICK_SHARE, FLAG_SHARE = 0.22, 0.035


def hash01(*k):
    h = 2166136261
    for v in k:
        h = ((h ^ (int(v) & 0xffffffff)) * 16777619) & 0xffffffff
        h ^= h >> 15
        h = (h * 2246822519) & 0xffffffff
        h ^= h >> 13
    return (h & 0xffffff) / float(0x1000000)


def pack():
    import numpy as np
    from PIL import Image, ImageDraw, ImageFilter
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(TEXTURES, exist_ok=True)
    who = json.load(open(os.path.join(STRIPS, "people.json"), encoding="utf-8"))["people"]
    n = len(who)
    cw, ch = CELL_PX
    atlas = np.zeros((ATLAS_PX, ATLAS_PX, 4), np.float32)
    emit = np.zeros((ATLAS_PX, ATLAS_PX), np.float32)
    ink = np.array((0.045, 0.05, 0.085), np.float32)
    touching = []

    def shrink(a):
        h, w = a.shape[0] // SUPER, a.shape[1] // SUPER
        return a.reshape(h, SUPER, w, SUPER, -1).mean(axis=(1, 3))

    for row in range(FRAMES):
        hi = np.asarray(Image.open(os.path.join(STRIPS, "row_%02d.png" % row)).convert("RGBA"), np.float32) / 255
        glow = np.asarray(Image.open(os.path.join(STRIPS, "row_%02d_emit.png" % row)).convert("RGBA"), np.float32) / 255
        a = hi[..., 3]
        solid = (a > 0.5)
        # The ink line: one supersampled pixel of the outline colour round every shape, the cast's
        # inverted hull at this size. Drawn before the shrink, so it lands as a soft dark rim.
        grown = np.asarray(Image.fromarray((solid * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(2 * INK + 1))) > 127
        rgb = hi[..., :3].copy()
        rim = grown & ~solid
        rgb[rim] = ink
        alpha = np.where(grown, 1.0, 0.0).astype(np.float32)
        # Below the hip a spectator is in the shade of the row in front: darker toward the floor.
        # It also keeps a seated body's pale soles, which face the stage, from being its brightest part.
        height = (CELL_M[1] - GROUND_M) - (np.arange(rgb.shape[0]) + 0.5) / rgb.shape[0] * CELL_M[1]
        shade = np.clip(LEG_SHADE + (1 - LEG_SHADE) * (height - 0.15) / 0.55, LEG_SHADE, 1.0).astype(np.float32)
        lit_prop = (glow[..., :3].max(axis=2) > 0.05)[..., None]
        rgb = np.where(lit_prop, rgb, rgb * shade[:, None, None])
        pre = shrink(np.concatenate([rgb * alpha[..., None], alpha[..., None]], axis=2))
        al = pre[..., 3]
        col = np.where(al[..., None] > 1e-4, pre[..., :3] / np.maximum(al[..., None], 1e-4), 0)
        g = shrink((glow[..., :3].max(axis=2) * glow[..., 3])[..., None])[..., 0]
        y0 = row * ch
        atlas[y0:y0 + ch, :n * cw, :3] = col
        atlas[y0:y0 + ch, :n * cw, 3] = al
        emit[y0:y0 + ch, :n * cw] = g
        for p in range(n):
            cell = al[:, p * cw:(p + 1) * cw]
            if cell[:2].max() > 0.05 or cell[-2:].max() > 0.05 or cell[:, :2].max() > 0.05 or cell[:, -2:].max() > 0.05:
                touching.append((row, p))

    # Colour is bled outward into the clear pixels so a bilinear or mipped sample at an edge never
    # pulls in black: the cutout is the alpha's job alone.
    rgb, al = atlas[..., :3], atlas[..., 3]
    known = al > 0.02
    for _ in range(6):
        acc = np.zeros_like(rgb); cnt = np.zeros(al.shape, np.float32)
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            acc += np.roll(rgb * known[..., None], (dy, dx), (0, 1)); cnt += np.roll(known.astype(np.float32), (dy, dx), (0, 1))
        fill = (~known) & (cnt > 0)
        rgb[fill] = acc[fill] / cnt[fill][:, None]
        known = known | fill
    lin = lambda x: np.clip(x, 0, 1)
    out = np.concatenate([lin(rgb), lin(al)[..., None]], axis=2)
    Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA").save(ATLAS)
    Image.fromarray((lin(emit) * 255 + 0.5).astype(np.uint8), "L").save(ATLAS_EMIT)

    starts, k = [], 0
    for l in LOOPS:
        starts.append(k); k += l[1]
    layout = {
        "atlas": "Assets/TumbangPreso/Art/Arena/Textures/arena_crowd_atlas.png",
        "emission": "Assets/TumbangPreso/Art/Arena/Textures/arena_crowd_atlas_emit.png",
        "atlas_px": ATLAS_PX, "cell_px": list(CELL_PX), "cell_m": list(CELL_M), "ground_m": GROUND_M,
        "columns": n, "rows": FRAMES, "view_pitch_degrees": VIEW_PITCH, "figure_m": FIGURE_M,
        "note": "A person is a COLUMN, a frame is a ROW. Rects are pixels, origin top left: x = person * 48, "
                "y = (loop.start + frame) * 72. The emission atlas has the same layout, one channel.",
        "loops": [{"name": l[0], "start": starts[i], "frames": l[1], "fps_calm": l[2], "fps_excited": l[3]} for i, l in enumerate(LOOPS)],
        "people": [dict(p, column=i, x=i * cw) for i, p in enumerate(who)],
        "cells": [{"person": p, "loop": l[0], "frame": t, "rect": [p * cw, (starts[i] + t) * ch, cw, ch]}
                  for p in range(n) for i, l in enumerate(LOOPS) for t in range(l[1])],
        "screen_px_1080p_fov60": [{"where": s[0], "distance_m": round(s[1], 1), "figure_px": round(s[2], 1), "cell_px": round(s[3], 1)}
                                  for s in screen_sizes()],
    }
    json.dump(layout, open(LAYOUT, "w", encoding="utf-8"), indent=1)

    # ------------------------------------------------------------ review pictures
    img = Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA")
    used = img.crop((0, 0, n * cw, FRAMES * ch))
    for bg, tag in (((70, 74, 96), "grey"), ((14, 20, 44), "seat")):
        back = Image.new("RGBA", used.size, bg + (255,))
        back.alpha_composite(used)
        back.convert("RGB").save(os.path.join(OUT, "%s_atlas_on_%s.png" % (VERSION, tag)))
    Image.fromarray((lin(emit[:FRAMES * ch, :n * cw]) * 255).astype(np.uint8), "L").save(os.path.join(OUT, "%s_atlas_emit.png" % VERSION))

    def cell(p, row, scale=1, night=False, glow_index=0):
        a = out[row * ch:(row + 1) * ch, p * cw:(p + 1) * cw].copy()
        if night:
            e = emit[row * ch:(row + 1) * ch, p * cw:(p + 1) * cw][..., None]
            a[..., :3] = np.clip(a[..., :3] * np.array(NIGHT_TINT) * (1 - e) + e * np.array(GLOW[glow_index]) * EMIT_GAIN, 0, 1)
            a[..., 3] = (a[..., 3] > 0.5)
        im = Image.fromarray((a * 255 + 0.5).astype(np.uint8), "RGBA")
        return im.resize((cw * scale, ch * scale), Image.NEAREST) if scale != 1 else im

    # Contact sheet: the twelve rigs as they are, every frame of every loop, three times the size.
    S = 3
    sheet = Image.new("RGBA", (60 + FRAMES * cw * S, 30 + 12 * ch * S), (58, 62, 84, 255))
    d = ImageDraw.Draw(sheet)
    x = 60
    for l in LOOPS:
        d.text((x + 4, 8), "%s (%d)" % (l[0], l[1]), fill=(255, 255, 255, 255))
        d.line((x, 0, x, sheet.size[1]), fill=(30, 32, 48, 255))
        x += l[1] * cw * S
    for p in range(12):
        d.text((4, 30 + p * ch * S + 90), who[p]["rig"].replace("character-", ""), fill=(255, 255, 255, 255))
        for row in range(FRAMES):
            sheet.alpha_composite(cell(p, row, S), (60 + row * cw * S, 30 + p * ch * S))
    sheet.convert("RGB").save(os.path.join(OUT, "%s_contact_rigs.png" % VERSION))
    # The same at the stand's own colours: on the seat navy, under the night tint, glow added.
    sheet = Image.new("RGBA", (FRAMES * cw * S, 12 * ch * S), (14, 20, 44, 255))
    for p in range(12):
        for row in range(FRAMES):
            sheet.alpha_composite(cell(12 + p, row, S, True, p % len(GLOW)), (row * cw * S, p * ch * S))
    sheet.convert("RGB").save(os.path.join(OUT, "%s_contact_night.png" % VERSION))
    # Everybody: all 42 in idle, cheer and the stick loop, twice the size.
    S = 2
    rows_shown = (0, starts[2], starts[3] + 2, starts[4], starts[5], starts[6], starts[7] + 2)
    sheet = Image.new("RGBA", (n * cw * S, len(rows_shown) * ch * S), (58, 62, 84, 255))
    for k, row in enumerate(rows_shown):
        for p in range(n):
            sheet.alpha_composite(cell(p, row, S), (p * cw * S, k * ch * S))
    sheet.convert("RGB").save(os.path.join(OUT, "%s_contact_everybody.png" % VERSION))

    # ------------------------------------------------------------ the mock stand
    rows_path = ROWS_FILE if os.path.exists(ROWS_FILE) else ROWS_FALLBACK
    stand = json.load(open(rows_path, encoding="utf-8"))
    cache = {}

    def sprite(p, row, h_px, tint, glow_index, flash, gain=1.0):
        """One cell at its on-screen height: shrunk the way a mip chain does (area average), then
        ALPHA TESTED, tinted for night, its glow added. This is ArenaCrowd.shader in numpy."""
        w_px = max(1, round(h_px * cw / ch)); h_px = max(1, round(h_px))
        key = (p, row, w_px, h_px)
        if key not in cache:
            a = out[row * ch:(row + 1) * ch, p * cw:(p + 1) * cw]
            e = emit[row * ch:(row + 1) * ch, p * cw:(p + 1) * cw]
            pre = np.concatenate([a[..., :3] * a[..., 3:4], a[..., 3:4], e[..., None]], axis=2)
            im = [np.asarray(Image.fromarray(pre[..., c], "F").resize((w_px, h_px), Image.BOX)) for c in range(5)]
            cache[key] = np.stack(im, axis=2)
        s = cache[key]
        al = s[..., 3]
        rgb = s[..., :3] / np.maximum(al[..., None], 1e-4)
        e = np.clip(s[..., 4] / np.maximum(al, 1e-4), 0, 1)[..., None]
        grey = (rgb * np.array((0.30, 0.59, 0.11))).sum(axis=2, keepdims=True)
        rgb = (rgb + (grey - rgb) * NIGHT_GREY) * np.array(NIGHT_TINT) * np.array(tint) * (1 - e) + e * np.array(GLOW[glow_index]) * EMIT_GAIN * gain
        if flash:
            yy, xx = np.mgrid[0:h_px, 0:w_px]
            near = (np.abs(xx - w_px * 0.5) < max(0.6, w_px * 0.045)) & (np.abs(yy - h_px * 0.42) < max(0.6, h_px * 0.03))
            rgb = np.where(near[..., None], 1.0, rgb)
        return np.clip(rgb, 0, 1), al > ALPHA_CUT

    loop_start = {l[0]: starts[i] for i, l in enumerate(LOOPS)}
    loop_count = {l[0]: l[1] for l in LOOPS}
    loop_fps = {l[0]: (l[2], l[3]) for l in LOOPS}

    def glow_of(u):
        for k, share in enumerate(GLOW_SHARE):
            if u < share:
                return k
            u -= share
        return 0

    def choose(seat_id, bearing, excitement, groan, time):
        """Which person, loop and frame a seat shows: the shader's rule."""
        person = int(hash01(seat_id, 1) * n) % n
        r, r2, phase = hash01(seat_id, 2), hash01(seat_id, 3), hash01(seat_id, 4)
        kind = hash01(seat_id, 5)
        e = excitement
        calm = 0.5 + 0.5 * math.sin(bearing * 0.21 + time * 0.35 + r2 * 2.0)
        if kind < FLAG_SHARE:
            loop = "flag"
        elif kind < FLAG_SHARE + STICK_SHARE:
            loop = "stick_up" if e > 0.15 + r * 0.6 else "stick"
        elif groan > r:
            loop = "groan"
        elif e > 0.20 + r * 0.65:
            loop = "jump" if r2 < 0.45 else "cheer"
        elif e + 0.16 * calm > 0.10 + r * 0.9:
            loop = "clap"
        else:
            loop = "idle"
        fps = loop_fps[loop][0] + (loop_fps[loop][1] - loop_fps[loop][0]) * e
        frame = int(((time * fps / loop_count[loop] + phase) % 1.0) * loop_count[loop])
        return person, loop_start[loop] + frame, loop

    def view(name, eye, look_at, excitement, groan=0.0, size=(1920, 1080), fov=60.0, time=3.7, lod=None, crop=None, zoom=1,
             flashes=0.0):
        W, H = size
        f = (H / 2) / math.tan(math.radians(fov / 2))
        ex, ey, ez = eye
        fw = np.array(look_at, float) - np.array(eye, float); fw /= np.linalg.norm(fw)
        rt = np.cross(fw, (0, 0, 1.0)); rt /= np.linalg.norm(rt)
        up = np.cross(rt, fw)

        def project(pt):
            v = np.array(pt, float) - np.array(eye, float)
            z = v @ fw
            if z < 1.0:
                return None
            return (W / 2 + (v @ rt) * f / z, H / 2 - (v @ up) * f / z, z)

        canvas = Image.new("RGB", (W, H), (5, 7, 16))
        dr = ImageDraw.Draw(canvas)
        seat_c, riser_c, aisle_c, wall_c = (13, 18, 40), (10, 14, 32), (30, 35, 52), (18, 22, 36)
        counts = {}
        drawn = 0

        def polar(r, a, z):
            return (r * math.sin(math.radians(a)), r * math.cos(math.radians(a)), z)

        def quad(pts, fill):
            pr = [project(p) for p in pts]
            if any(p is None for p in pr):
                return
            if max(p[0] for p in pr) < 0 or min(p[0] for p in pr) > W or max(p[1] for p in pr) < 0 or min(p[1] for p in pr) > H:
                return
            dr.polygon([(p[0], p[1]) for p in pr], fill=fill)

        # Far to near: by distance from the eye of each bank's rows, outer rows first.
        order = []
        for bi, b in enumerate(stand["banks"]):
            for ri, rw in enumerate(b["rows"]):
                order.append((bi, ri))
        order.sort(key=lambda o: -stand["banks"][o[0]]["rows"][o[1]]["r"])
        arr = np.asarray(canvas).astype(np.float32) / 255
        for bi, ri in order:
            b = stand["banks"][bi]; rw = b["rows"][ri]
            tread, rise = b["tread"], b["rise"]
            lo_all, hi_all = b["sections"][0][0], b["sections"][-1][1]
            full = (hi_all - lo_all) > 340
            # the structure behind this row: its riser up to the next row (or the back wall), and its floor
            canvas = Image.fromarray((arr * 255 + 0.5).astype(np.uint8)); dr = ImageDraw.Draw(canvas)
            a = 0.0 if full else lo_all
            end = 360.0 if full else hi_all
            while a < end - 1e-6:
                a1 = min(a + 1.0, end)
                in_seat = any(s[0] <= (a + a1) / 2 <= s[1] for s in b["sections"])
                top = rw["z"] + (rise if ri < len(b["rows"]) - 1 else 1.2)
                quad([polar(rw["r"] + tread, a, rw["z"]), polar(rw["r"] + tread, a1, rw["z"]), polar(rw["r"] + tread, a1, top), polar(rw["r"] + tread, a, top)],
                     riser_c if in_seat else aisle_c)
                if ez > rw["z"]:
                    quad([polar(rw["r"], a, rw["z"]), polar(rw["r"], a1, rw["z"]), polar(rw["r"] + tread, a1, rw["z"]), polar(rw["r"] + tread, a, rw["z"])],
                         seat_c if in_seat else aisle_c)
                if ri == 0:
                    quad([polar(rw["r"], a, rw["z"] - 4.0), polar(rw["r"], a1, rw["z"] - 4.0), polar(rw["r"], a1, rw["z"]), polar(rw["r"], a, rw["z"])], wall_c)
                a = a1
            arr = np.asarray(canvas).astype(np.float32) / 255
            step = 1
            r_seat = rw["r"] + 0.55 * tread
            if lod is not None:
                centre_d = math.dist(eye, polar(r_seat, (lo_all + hi_all) / 2 if not full else math.degrees(math.atan2(look_at[0], look_at[1])), rw["z"]))
                step = 2 if centre_d > lod else 1
            for si, (lo, hi) in enumerate(b["sections"]):
                arc = math.radians(hi - lo) * r_seat
                seats = max(1, int(arc / SEAT_PITCH))
                for k in range(0, seats, step):
                    seat_id = ((bi * 64 + ri) * 64 + si) * 4096 + k
                    if hash01(seat_id, 9) > FILL:
                        continue
                    bearing = lo + (k + 0.5 + (hash01(seat_id, 12) - 0.5) * 0.5) * (hi - lo) / seats
                    if any(v["lo"] <= bearing <= v["hi"] and rw["z"] < v["z_max"] for v in stand.get("voids", [])):
                        continue
                    jitter = (hash01(seat_id, 6) - 0.5) * 0.30 + (0.11 if k % 2 else -0.11)
                    pos = polar(r_seat + jitter, bearing, rw["z"])
                    pr = project(pos)
                    if pr is None:
                        continue
                    scale = (0.93 + 0.14 * hash01(seat_id, 7)) * (LOD_WIDEN if step == 2 else 1.0)
                    h = CELL_M[1] * scale * f / pr[2]
                    w = h * cw / ch
                    x0, y1 = pr[0] - w / 2, pr[1] + GROUND_M * scale * f / pr[2]
                    if x0 > W or x0 + w < 0 or y1 - h > H or y1 < 0:
                        continue
                    person, row, loop = choose(seat_id, bearing, excitement, groan, time)
                    if loop in ("cheer", "jump", "stick_up"):          # the shader's hop: a body on its feet bobs
                        y1 -= abs(math.sin(time * 7.0 + hash01(seat_id, 4) * 6.283)) * HOP_M * excitement * f / pr[2]
                    counts[loop] = counts.get(loop, 0) + 1
                    flash = hash01(seat_id, int(time * 8), 11) < flashes
                    rgb, mask = sprite(person, row, h, TINTS[int(hash01(seat_id, 8) * len(TINTS))], glow_of(hash01(seat_id, 10)), flash, 1.0 + 0.5 * excitement)
                    hh, ww = mask.shape
                    X, Y = int(round(x0)), int(round(y1 - hh))
                    xa, ya, xb, yb = max(0, X), max(0, Y), min(W, X + ww), min(H, Y + hh)
                    if xa >= xb or ya >= yb:
                        continue
                    m = mask[ya - Y:yb - Y, xa - X:xb - X]
                    region = arr[ya:yb, xa:xb]
                    region[m] = rgb[ya - Y:yb - Y, xa - X:xb - X][m]
                    drawn += 1
        res = Image.fromarray((np.clip(arr, 0, 1) * 255 + 0.5).astype(np.uint8))
        if crop:
            res = res.crop(crop)
        if zoom != 1:
            res = res.resize((res.size[0] * zoom, res.size[1] * zoom), Image.NEAREST)
        res.save(os.path.join(OUT, "%s_%s.png" % (VERSION, name)))
        lum = np.asarray(res.convert("L"), np.float32)
        print("%-34s sprites %6d  mean luma %5.1f  p99 %5.1f  loops %s" % (name, drawn, lum.mean(), np.percentile(lum, 99), counts))
        return drawn

    eye = (0.0, 0.0, 3.3)
    north = (0.0, 150.0, 30.0)
    view("stage_calm", eye, north, 0.0, lod=LOD_DISTANCE)
    view("stage_excited", eye, north, 1.0, lod=LOD_DISTANCE, flashes=0.05)
    view("stage_groan", eye, north, 0.0, groan=0.7, lod=LOD_DISTANCE)
    view("stage_edge_first_rows_x3", (0.0, 22.0, 3.3), (0.0, 100.0, 9.0), 0.4, crop=(720, 380, 1200, 640), zoom=3, lod=LOD_DISTANCE)
    view("stage_first_rows_calm_x4", eye, north, 0.0, crop=(800, 590, 1120, 770), zoom=4, lod=LOD_DISTANCE)
    view("stage_first_rows_excited_x4", eye, north, 1.0, crop=(800, 590, 1120, 770), zoom=4, lod=LOD_DISTANCE, flashes=0.05)
    view("stage_top_rows_lod_x4", eye, north, 0.5, crop=(800, 370, 1120, 550), zoom=4, lod=LOD_DISTANCE)
    view("stage_top_rows_full_x4", eye, north, 0.5, crop=(800, 370, 1120, 550), zoom=4, lod=None)
    view("air_calm", (0.0, -30.0, 120.0), (0.0, 130.0, 20.0), 0.0, lod=LOD_DISTANCE)
    view("air_excited", (60.0, 20.0, 70.0), (20.0, 140.0, 25.0), 1.0, lod=LOD_DISTANCE, flashes=0.05)
    view("drone_close", (0.0, 60.0, 14.0), (0.0, 100.0, 10.0), 0.6, lod=LOD_DISTANCE)

    if arg("gif", "1") == "1":
        frames = []
        for k in range(24):
            e = max(0.0, min(1.0, (k - 4) / 3.0)) * max(0.0, min(1.0, (22 - k) / 8.0))
            view("_gif", (0.0, 22.0, 3.3), (0.0, 100.0, 9.0), e, crop=(720, 380, 1200, 640), zoom=3, lod=LOD_DISTANCE, time=3.7 + k / 8.0,
                 flashes=0.05 * e)
            frames.append(Image.open(os.path.join(OUT, "%s__gif.png" % VERSION)).convert("P", palette=Image.ADAPTIVE))
        frames[0].save(os.path.join(OUT, "%s_cheer_from_stage_edge_x3.gif" % VERSION), save_all=True, append_images=frames[1:], duration=125, loop=0)
        os.remove(os.path.join(OUT, "%s__gif.png" % VERSION))

    # counts for the report
    total = 0; per_bank = []
    for bi, b in enumerate(stand["banks"]):
        c = 0
        for ri, rw in enumerate(b["rows"]):
            r_seat = rw["r"] + 0.55 * b["tread"]
            for si, (lo, hi) in enumerate(b["sections"]):
                seats = max(1, int(math.radians(hi - lo) * r_seat / SEAT_PITCH))
                for k in range(seats):
                    seat_id = ((bi * 64 + ri) * 64 + si) * 4096 + k
                    bearing = lo + (k + 0.5) * (hi - lo) / seats
                    if hash01(seat_id, 9) <= FILL and not any(v["lo"] <= bearing <= v["hi"] and rw["z"] < v["z_max"] for v in stand.get("voids", [])):
                        c += 1
        per_bank.append((b["name"], c)); total += c
    print("rows from", os.path.relpath(rows_path, ROOT))
    print("spectators", total, per_bank)
    print("cells with ink in their 2 px guard band (bleed risk):", len(touching), sorted(set(t[0] for t in touching)))
    for s in screen_sizes():
        print("  %-44s %6.1f m  figure %5.1f px  cell %5.1f px" % s)


LEG_SHADE = 0.5         # how dark the feet are (1 is no shade), rising to full light at hip height
INK = 2                 # the ink rim, in supersampled pixels (0.5 px of the cell)
ALPHA_CUT = 0.5         # the mock's alpha test, the shader's _Cutoff
LOD_DISTANCE = 150.0    # past this a section draws every other seat
HOP_M = 0.30            # how high the shader bobs a body that is on its feet, at full excitement
LOD_WIDEN = 1.3         # and its sprites this much larger

if __name__ == "__main__":
    stage = arg("stage", None)
    try:
        import bpy  # noqa: F401
        in_blender = True
    except ImportError:
        in_blender = False
    if in_blender and stage in (None, "render"):
        render()
    else:
        pack()
