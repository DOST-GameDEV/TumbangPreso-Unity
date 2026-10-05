"""Review pictures of the Arena's rescue drone, SAGIP, and of its tractor beam, phase by phase.

  blender -b --python tools/review_arena_drone.py -- --version=v6 [--only=front,phases]

Builds ONLY the drone (tools/author_arena_stage.py, `drone`) with the kit's own materials and
textures, so a round of render, critique and fix takes seconds, not the whole stage. Writes
Logs/arena/stage/drone_<shot>_<vN>.png:

  front, quarter, above, below     the drone alone, close
  faces                            the four faces side by side
  distance                         at 18 m and at 30 m beside a 1.8 m figure, the beam on
  phase_1_search .. phase_6_leave  a MOCK of each phase of a carry: the drone posed as
                                   Runtime/Map/ArenaDrone.cs poses it (lean, squash, prongs, face),
                                   the beam's two cones at that phase's colour and length, and the
                                   pooled effects drawn as flat cards from the same shapes
                                   ArenaFx paints (`fx_shape` below is a copy of ArenaFx.Shape;
                                   the sheet of them is drone_fx_atlas_<vN>.png)
  phases                           the six on one sheet

The mock is a picture of the DESIGN. Nothing here runs the game's code: what the game draws is
unverified until it runs in Unity.
"""
import bpy, bmesh, math, os, sys
import numpy as np
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import author_arena_stage as S                                    # noqa: E402
from arena_kit import polar                                       # noqa: E402

LOGS = S.LOGS
CYAN, WHITE, GOLD, MAGENTA, LIME = (0.45, 0.95, 1.0), (0.92, 0.96, 1.0), (1.0, 0.86, 0.30), (1.0, 0.30, 0.85), (0.72, 1.0, 0.45)
CELLS = ("Dot", "Ring", "Streak", "Star", "Chevron", "Disc", "ThinRing", "Scan",
         "Band", "Star5", "Burst", "Halftone", "Target", "Line", "Sparkle", "Puff")


# ---------------------------------------------------------------- ArenaFx's shapes (a copy of ArenaFx.Shape)
def fx_shape(cell, x, y):
    r = np.hypot(x, y)
    c01 = lambda v: np.clip(v, 0, 1)                              # noqa: E731
    band = lambda at, half: np.exp(-(r - at) ** 2 / (half * half))  # noqa: E731
    ang = np.arctan2(x, y)
    if cell == "Dot":
        return np.exp(-r * r * 5.5) * c01((0.95 - r) * 6)
    if cell == "Ring":
        return band(0.80, 0.085) + 0.22 * c01((r - 0.35) / 0.45) * c01((0.80 - r) * 30)
    if cell == "Streak":
        return np.exp(-x * x * 26) * c01((0.92 - np.abs(y)) * 2.2) * c01((1 - np.abs(y)) * 1.2)
    if cell == "Star":
        ax, ay = np.abs(x), np.abs(y)
        cross = np.maximum(np.exp(-ay * ay * 420) * (1 - ax), np.exp(-ax * ax * 420) * (1 - ay))
        d0, d1 = np.abs(x - y) * 0.7071, np.abs(x + y) * 0.7071
        diag = np.maximum(np.exp(-d0 * d0 * 700), np.exp(-d1 * d1 * 700)) * c01(1 - r * 2.2) * 0.7
        return np.maximum(cross * c01(1.05 - r), diag) + np.exp(-r * r * 40)
    if cell == "Chevron":
        arm = np.abs(y - (0.35 - np.abs(x) * 0.9))
        return c01((0.13 - arm) * 14) * c01((0.82 - np.abs(x)) * 8)
    if cell == "Disc":
        return c01((0.92 - r) * 3.2) * (0.55 + 0.45 * np.exp(-r * r * 2))
    if cell == "ThinRing":
        return band(0.90, 0.022)
    if cell == "Scan":
        return band(0.90, 0.03) + 0.5 * c01((r - 0.55) / 0.35) * c01((0.90 - r) * 40)
    # ---- the comic shapes (v6): hard edges, flat fills
    hard = 40.0
    if cell == "Band":                                            # a bold flat ring
        return c01((0.115 - np.abs(r - 0.76)) * hard)
    if cell == "Star5":                                           # a chunky five-pointed star
        u = ang * 5 / (2 * np.pi)
        phi = np.abs(u - np.floor(u + 0.5)) * (2 * np.pi / 5)       # the angle from the nearest point, 0 to 36 degrees
        edge = 0.92 * 0.44 * np.sin(np.pi / 5) / (0.92 * np.sin(phi) + 0.44 * np.sin(np.pi / 5 - phi))
        return c01((edge - r) * hard)
    if cell == "Burst":                                           # a zigzag crown: a comic burst's outline
        edge = 0.70 + 0.16 * (np.abs(((ang * 12 / (2 * np.pi)) % 1.0) * 2 - 1) * 2 - 1)
        return c01((0.075 - np.abs(r - edge)) * hard)
    if cell == "Halftone":                                        # dots that shrink toward the rim
        gx, gy = (x * 5.0 + 0.5) % 1.0 - 0.5, (y * 5.0 + 0.5) % 1.0 - 0.5
        return c01((0.44 * (1 - r) - np.hypot(gx, gy)) * 18.0) * c01((0.9 - r) * 20)
    if cell == "Target":                                          # the landing mark: a ring, four darts, a spot
        ring = c01((0.045 - np.abs(r - 0.84)) * hard)
        q = (ang + np.pi / 4) % (np.pi / 2) - np.pi / 4           # the angle from the nearest of four axes
        dart = c01((0.30 * (0.70 - r) / 0.26 - np.abs(q) * r) * hard) * c01((r - 0.44) * hard) * c01((0.70 - r) * hard)
        spot = c01((0.12 - r) * hard)
        dash = c01((0.03 - np.abs(r - 0.30)) * hard) * c01((np.abs(((ang * 8 / (2 * np.pi)) % 1.0) - 0.5) - 0.2) * 12)
        return np.maximum(np.maximum(ring, dart), np.maximum(spot, dash))
    if cell == "Line":                                            # a speed line: a hard sliver, pointed at both ends
        return c01((0.085 * (1 - np.abs(y) / 0.94) - np.abs(x)) * 60.0)
    if cell == "Sparkle":                                         # a fat four-pointed sparkle
        ax, ay = np.abs(x), np.abs(y)
        return c01((0.86 - (ax + ay + 2.6 * np.sqrt(ax * ay))) * 22.0)
    if cell == "Puff":                                            # a cartoon puff: three lobes and a flat foot
        lobe = np.minimum(np.minimum(np.hypot(x + 0.36, y + 0.10) - 0.36, np.hypot(x - 0.36, y + 0.10) - 0.36), np.hypot(x, y - 0.20) - 0.46)
        return c01(-lobe * hard) * c01((y + 0.42) * hard)
    return r * 0


def fx_atlas(path, px=128):
    """ArenaFx's atlas as it paints it: four by four, white, the shape in the alpha."""
    from_img = np.zeros((4 * px, 4 * px, 4), dtype=np.float32)
    g = (np.arange(px) + 0.5) / px * 2 - 1
    x, y = np.meshgrid(g, -g)
    rim = np.clip((1 - np.maximum(np.abs(x), np.abs(y))) * 12, 0, 1)
    for i, cell in enumerate(CELLS):
        a = np.clip(fx_shape(cell, x, y), 0, 1) * rim
        from_img[(i // 4) * px:(i // 4 + 1) * px, (i % 4) * px:(i % 4 + 1) * px, :3] = 1.0
        from_img[(i // 4) * px:(i // 4 + 1) * px, (i % 4) * px:(i % 4 + 1) * px, 3] = a
    img = bpy.data.images.new("fx_atlas", 4 * px, 4 * px, alpha=True)
    img.pixels.foreach_set(from_img[::-1].ravel())
    img.filepath_raw = path; img.file_format = "PNG"; img.save()
    return img


_fx_mats = {}


def fx_mat(atlas, colour, alpha):
    key = (colour, round(alpha, 2))
    if key in _fx_mats:
        return _fx_mats[key]
    m = bpy.data.materials.new("fx")
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = atlas; tex.extension = "CLIP"
    mul = nt.nodes.new("ShaderNodeMath"); mul.operation = "MULTIPLY"; mul.inputs[1].default_value = alpha
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs[0], b.inputs["Alpha"])
    b.inputs["Base Color"].default_value = (0, 0, 0, 1)
    b.inputs["Emission Color"].default_value = (*colour, 1)
    b.inputs["Emission Strength"].default_value = 2.2
    m.surface_render_method = "BLENDED"; m.use_backface_culling = False
    _fx_mats[key] = m
    return m


class Cards:
    """The pooled effects of one mock frame: quads cut from the atlas, flat on the ground plane,
    facing the camera, or stretched along a line (ArenaFx's three ways of turning a quad)."""

    def __init__(self, atlas, coll, eye):
        self.atlas, self.coll, self.eye, self.made = atlas, coll, Vector(eye), []

    def _quad(self, cell, pts, colour, alpha):
        i = CELLS.index(cell)
        u0, v0 = (i % 4) / 4.0, 1.0 - (i // 4 + 1) / 4.0
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        f = bm.faces.new([bm.verts.new(p) for p in pts])
        for loop, (a, b) in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
            loop[uv].uv = (u0 + a * 0.25, v0 + b * 0.25)
        me = bpy.data.meshes.new("card"); bm.to_mesh(me); bm.free()
        me.materials.append(fx_mat(self.atlas, colour, alpha))
        ob = bpy.data.objects.new("card", me)
        self.coll.objects.link(ob); self.made.append(ob)

    def flat(self, cell, at, size, colour, alpha, yaw=0.0):
        c = Vector(at); h = size / 2
        a = math.radians(yaw)
        r, f = Vector((math.cos(a), -math.sin(a), 0)) * h, Vector((math.sin(a), math.cos(a), 0)) * h
        self._quad(cell, (c - r - f, c + r - f, c + r + f, c - r + f), colour, alpha)

    def face(self, cell, at, size, colour, alpha, roll=0.0):
        c = Vector(at)
        look = (self.eye - c).normalized()
        r = Vector((0, 0, 1)).cross(look).normalized(); u = look.cross(r)
        a = math.radians(roll)
        r, u = (r * math.cos(a) + u * math.sin(a)) * size / 2, (u * math.cos(a) - r * math.sin(a)) * size / 2
        self._quad(cell, (c - r - u, c + r - u, c + r + u, c - r + u), colour, alpha)

    def line(self, cell, a, b, w0, w1, colour, alpha):
        a, b = Vector(a), Vector(b)
        side = (b - a).cross(self.eye - (a + b) / 2).normalized()
        self._quad(cell, (a - side * w0 / 2, a + side * w0 / 2, b + side * w1 / 2, b - side * w1 / 2), colour, alpha)

    def clear(self):
        for ob in self.made:
            bpy.data.objects.remove(ob)
        self.made = []


# ---------------------------------------------------------------- the scene
def shoot(name, loc, target, lens, res=(1600, 900)):
    scene = bpy.context.scene
    cd = bpy.data.cameras.new(name); cam = bpy.data.objects.new(name, cd)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cd.lens = lens; cd.clip_start = 0.05; cd.clip_end = 2000
    scene.camera = cam
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.filepath = os.path.join(LOGS, name + ".png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


def tint(colour, strength=1.6, core=None):
    for key, c in (("beam", colour), ("beamcore", core or colour)):
        nt = bpy.data.materials["arena_stage_" + key].node_tree
        nt.nodes["phase tint"].inputs[7].default_value = (*c, 1)
        nt.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = strength


def pose(parts, at, face="search", beam=0.0, claws=0.0, lean=(0, 0, 0), squash=(1, 1, 1), yaw=0.0, spin=0.0, scroll=0.0, core=1.0):
    """The drone posed as the game poses it. `beam` is the cones' length in metres (0: none)."""
    from mathutils import Euler
    turn = Matrix.Translation(at) @ Matrix.Rotation(-math.radians(yaw), 4, "Z") @ Euler(lean).to_matrix().to_4x4() @ Matrix.Diagonal((squash[0], squash[1], squash[2], 1))
    for name, ob in parts.items():
        local = Matrix.Identity(4)
        if name == "drone_antenna":
            local = Matrix.Translation(polar(0.22, 205.0, 0.35))
        elif name == "drone_fan":
            local = Matrix.Rotation(math.radians(spin), 4, "Z")
        elif name.startswith("drone_claw_"):
            a = S.DRONE_CLAWS[int(name[-1])]
            local = Matrix.Translation(polar(0.27, a, -0.35)) @ Matrix.Rotation(-math.radians(a), 4, "Z") @ Matrix.Rotation(math.radians(claws), 4, "X")
        elif name.startswith("drone_beam"):
            wide = core if name.endswith("core") else 1.0
            local = Matrix.Translation((0, 0, S.DRONE_BELLY)) @ Matrix.Diagonal((wide, wide, max(beam, 1e-3) / S.DRONE_BEAM, 1))
            ob.hide_render = beam <= 0.05
        elif name.startswith("drone_face_"):
            ob.hide_render = name != "drone_face_" + face
        ob.matrix_world = turn @ local
    for key, speed in (("beam", 1.0), ("beamcore", 1.7)):          # the scroll: the texture slid along v
        nt = bpy.data.materials["arena_stage_" + key].node_tree
        m = nt.nodes.get("scroll")
        if m is None:
            m = nt.nodes.new("ShaderNodeMapping"); m.name = "scroll"
            co = nt.nodes.new("ShaderNodeTexCoord")
            nt.links.new(co.outputs["UV"], m.inputs["Vector"])
            for n in nt.nodes:
                if n.type == "TEX_IMAGE":
                    nt.links.new(m.outputs["Vector"], n.inputs["Vector"])
        m.inputs["Location"].default_value = (0, -scroll * speed, 0)


def main():
    version, only = "v6", None
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
        if a.startswith("--only="):
            only = set(a.split("=", 1)[1].split(","))
    want = lambda k: only is None or k in only                    # noqa: E731
    bpy.ops.wm.read_factory_settings()
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    scene = bpy.context.scene
    S.materials()
    for key in ("beam", "beamcore"):                              # the game ADDS the beam's light: no lit white under it here
        nt = bpy.data.materials["arena_stage_" + key].node_tree
        b = nt.nodes["Principled BSDF"]
        nt.links.remove(b.inputs["Base Color"].links[0])
        b.inputs["Base Color"].default_value = (0, 0, 0, 1)
        scale = nt.nodes.new("ShaderNodeMath"); scale.operation = "MULTIPLY"; scale.inputs[1].default_value = 0.62
        nt.links.new(b.inputs["Alpha"].links[0].from_socket, scale.inputs[0])
        nt.links.new(scale.outputs[0], b.inputs["Alpha"])
    kit = S.collection("kit"); kit.hide_render = True
    P = S.drone(kit)
    stage = S.collection("review")
    parts = S.drone_set(P, stage, "review_")
    tris = {k: sum(len(p.vertices) - 2 for p in ob.data.polygons) for k, ob in parts.items()}
    shown = sum(v for k, v in tris.items() if not k.startswith("drone_face_")) + tris["drone_face_search"]
    print("DRONE_TRIS", tris, "| drawn at once (one face)", shown, "| every part", sum(tris.values()))
    for k, ob in parts.items():
        bm = bmesh.new(); bm.from_mesh(ob.data)
        bad = sum(1 for e in bm.edges if len(e.link_faces) != 2)
        bm.free()
        if bad:
            print("DRONE_OPEN", k, bad)

    spot = bpy.data.objects.new("review_spot", P["drone_spot"].data); stage.objects.link(spot)
    spot.location = (60, 0, 0)
    fig = S.figure("review_figure", S.flat("review_figure", (0.939, 0.162, 0.014), 0.25), stage)
    fig2 = S.figure("review_figure_b", S.flat("review_figure_b", (0.0, 0.216, 0.807), 0.25), stage)
    floor = bpy.data.meshes.new("review_floor")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=60.0)
    uvl = bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        for loop in f.loops:
            loop[uvl].uv = (loop.vert.co.x / 8.0, loop.vert.co.y / 8.0)
    bm.to_mesh(floor); bm.free()
    floor.materials.append(S.K.mat("deck"))
    deck = bpy.data.objects.new("review_floor", floor); stage.objects.link(deck)
    key = bpy.data.objects.new("floodlight key", bpy.data.lights.new("floodlight key", "SUN"))
    key.data.energy = 3.6; key.data.color = (0.86, 0.93, 1.0); key.data.angle = math.radians(25)
    key.rotation_euler = (math.radians(38), math.radians(18), math.radians(150))
    scene.collection.objects.link(key)
    fill = bpy.data.objects.new("the deck's bounce", bpy.data.lights.new("the deck's bounce", "SUN"))
    fill.data.energy = 1.1; fill.data.color = (0.9, 0.95, 1.0); fill.data.angle = math.radians(60)
    fill.rotation_euler = (math.radians(160), 0, math.radians(20))
    scene.collection.objects.link(fill)
    world = bpy.data.worlds.new("night"); scene.world = world; world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.006, 0.010, 0.035, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.view_settings.view_transform = "Standard"
    os.makedirs(LOGS, exist_ok=True)
    atlas = fx_atlas(os.path.join(LOGS, "drone_fx_atlas_%s.png" % version))
    sfx = "_%s" % version
    H = 4.4                                                       # the drone's middle over the deck: a body's feet 2.6 m under it, 1.8 m up
    far = (60.0, 0.0, 0.0)

    # ---- the drone alone
    fig.location = far; fig2.location = far
    deck.hide_render = True
    pose(parts, (0, 0, H), face="search", claws=-14.0)
    if want("front"):
        shoot("drone_front" + sfx, (0, 3.4, H + 0.15), (0, 0, H + 0.05), 50)
    if want("quarter"):
        shoot("drone_quarter" + sfx, (-2.2, 2.7, H + 0.9), (0, 0, H + 0.05), 50)
        pose(parts, (0, 0, H), face="proud", claws=28.0)
        shoot("drone_quarter_back" + sfx, (2.3, -2.6, H + 0.7), (0, 0, H + 0.05), 50)
    pose(parts, (0, 0, H), face="search", claws=-14.0)
    if want("above"):
        shoot("drone_above" + sfx, (0.0, 0.6, H + 3.6), (0, 0, H), 50)
    if want("below"):
        pose(parts, (0, 0, H), face="lock", claws=28.0)
        shoot("drone_below" + sfx, (-0.5, 1.0, H - 2.6), (0, 0, H - 0.1), 40)
    if want("faces"):
        row = []
        for i, name in enumerate(S.DRONE_FACES):
            more = S.drone_set(P, stage, "face%d_" % i)
            pose(more, (2.4 - 1.6 * i, 0, H), face=name, claws=(-14, 32, 26, 8)[i], lean=((0.12, 0, 0), (0.2, 0, 0), (0.1, 0, 0), (-0.05, 0.12, 0))[i],
                 squash=((1, 1, 1), (1.1, 1.1, 0.84), (0.93, 0.93, 1.14), (1, 1, 1))[i])
            row.append(more)
        for ob in parts.values():
            ob.hide_render = True
        shoot("drone_faces" + sfx, (0, 6.2, H + 0.25), (0, 0, H + 0.05), 50, (2400, 760))
        for more in row:
            for ob in more.values():
                bpy.data.objects.remove(ob)
        for ob in parts.values():
            ob.hide_render = False

    # ---- at game distance
    deck.hide_render = False
    if want("distance"):
        tint(CYAN)
        pose(parts, (0, 0, H), face="carry", beam=2.75, claws=28.0, lean=(0.10, 0, 0), scroll=0.1)
        fig.location = (0, 0, H - 2.6); fig2.location = (-2.2, 0.4, 0)
        shoot("drone_distance_18m" + sfx, (-3.0, 18.0, 1.7), (-0.6, 0, 2.6), 35)
        shoot("drone_distance_30m" + sfx, (-5.0, 29.5, 1.7), (-0.6, 0, 2.6), 35)

    # ---- the phases
    if want("phases"):
        eye = (-2.4, 7.4, 2.5)
        cards = Cards(atlas, stage, eye)
        fig2.location = far

        def frame(name, target=(0, 0, 2.5), lens=30, where=eye):
            shoot("drone_phase_" + name + sfx, where, target, lens, (1200, 900))
            cards.clear()

        # 1 SEARCH: dropping in, a thin gold scan line sweeping, the prongs shut, eyes looking down
        deck.hide_render = True
        tint(GOLD, 1.3)
        feet = Vector((0.5, 0, 0.2))
        fig.location = feet; fig.rotation_euler = (0.5, 0.3, 0)
        pose(parts, (0, 0, 6.3), face="search", beam=5.2, claws=0.0, lean=(0.16, 0.05, 0), squash=(0.94, 0.94, 1.12), yaw=-14, spin=30, core=0.32)
        parts["drone_beam"].hide_render = True
        cards.flat("Scan", feet + Vector((0, 0, 0.9)), 3.4, GOLD, 0.8)
        for k in range(4):
            cards.line("Line", (-0.9 + 0.6 * k, 0.2 * k, 7.6 + 0.3 * (k % 2)), (-0.9 + 0.6 * k, 0.2 * k, 9.2 + 0.4 * (k % 2)), 0.12, 0.12, WHITE, 0.5)
        frame("1_search", (0.2, 0, 3.6), 24, (-2.6, 9.6, 3.0))
        fig.rotation_euler = (0, 0, 0)

        # 2 LOCK ON: the target snaps round the body, gold eyes, the prongs spring open, a squash
        feet = Vector((0, 0, 1.8))
        fig.location = feet
        tint(WHITE, 2.0, GOLD)
        pose(parts, (0, 0, feet.z + 2.6), face="lock", beam=2.75, claws=32.0, squash=(1.14, 1.14, 0.80), core=0.6, scroll=0.3)
        mid = feet + Vector((0, 0, 0.95))
        cards.face("Target", mid, 2.9, GOLD, 0.95, roll=20)
        cards.face("Burst", mid, 4.3, WHITE, 0.7)
        cards.face("Sparkle", mid + Vector((1.5, 0, 1.0)), 0.7, WHITE, 0.95)
        cards.face("Sparkle", mid + Vector((-1.6, 0, -0.5)), 0.5, GOLD, 0.95)
        cards.face("Star5", (0.75, 0.3, feet.z + 3.25), 0.5, GOLD, 0.95, roll=15)
        cards.face("Star5", (-0.8, 0.3, feet.z + 3.1), 0.36, GOLD, 0.95, roll=-20)
        frame("2_lock", (0, 0, feet.z + 1.5))

        # 3 THE HAUL: stretched tall, bands racing, speed lines and rings left behind in the shaft
        tint(CYAN, 1.9)
        pose(parts, (0, 0, feet.z + 2.6), face="carry", beam=2.75, claws=28.0, squash=(0.90, 0.90, 1.22), scroll=0.62, spin=70)
        for k, (a, r, z, ln) in enumerate(((20, 1.5, 0.4, 2.6), (75, 1.9, 1.6, 2.0), (140, 1.4, -0.4, 3.0), (200, 1.8, 2.4, 2.2), (255, 1.5, 0.9, 2.8),
                                           (310, 2.0, -0.8, 2.4), (350, 1.3, 3.0, 1.8), (100, 2.3, 3.2, 2.0), (230, 2.4, 0.0, 2.6))):
            p = polar(r, a, feet.z + z)
            cards.line("Line", p, p - Vector((0, 0, ln)), 0.16, 0.16, WHITE if k % 3 else CYAN, 0.8)
        for k, dz in enumerate((-0.5, -1.9, -3.6)):
            cards.flat("Band", feet + Vector((0, 0, dz)), 2.6 + 0.9 * k, CYAN, 0.85 - 0.22 * k)
        cards.face("Dot", feet + Vector((0, 0, 0.9)), 3.2, CYAN, 0.35)
        for a, r, z, sz in ((40, 0.8, 0.3, 0.34), (160, 0.7, 1.2, 0.26), (280, 0.9, 0.8, 0.3), (100, 0.6, 1.9, 0.22)):
            cards.face("Star5", polar(r, a, feet.z + z), sz, GOLD, 0.95, roll=a)
        frame("3_haul", (0, 0, feet.z + 0.9), 24)

        # 4 ACROSS: banked into its travel, the body trailing, a ribbon of sparkles behind
        deck.hide_render = False
        feet = Vector((0, 0, 3.2))
        fig.location = feet + Vector((0.28, 0, 0.05)); fig.rotation_euler = (0, -0.22, 0)
        tint(CYAN, 1.5)
        pose(parts, (0, 0, feet.z + 2.6), face="carry", beam=2.75, claws=24.0, lean=(0.0, 0.26, 0), scroll=0.85, spin=110)
        for k in range(7):
            p = Vector((0.9 + 0.75 * k, 0, feet.z + 0.5 + 0.35 * math.sin(k * 1.3)))
            cards.face("Sparkle" if k % 2 else "Star5", p, 0.48 - 0.045 * k, GOLD if k % 2 else WHITE, 0.95 - 0.1 * k, roll=25 * k)
        for k in range(3):
            cards.line("Line", (1.2, 0, feet.z + 2.3 + 0.35 * k), (3.4 + 0.5 * k, 0, feet.z + 2.3 + 0.35 * k), 0.12, 0.12, WHITE, 0.6)
        cards.flat("Band", feet + Vector((0, 0, 0.05)), 2.3, CYAN, 0.7)
        frame("4_across", (0.6, 0, feet.z + 1.2), 26)
        fig.rotation_euler = (0, 0, 0)

        # 5 SET-DOWN: the painted landing mark turning on the deck, the beam narrowing, a soft descent
        feet = Vector((0, 0, 0.7))
        fig.location = feet
        tint(GOLD, 1.3, WHITE)
        pose(parts, (0, 0, feet.z + 2.6), face="search", beam=2.75 + 0.6, claws=20.0, squash=(1.03, 1.03, 0.95), scroll=0.2, core=0.8)
        spot.location = (0, 0, 0.06); spot.rotation_euler = (0, 0, 0.5); spot.scale = (1.25, 1.25, 1)
        cards.flat("Band", (0, 0, 0.04), 4.6, GOLD, 0.45)
        frame("5_setdown", (0, 0, 1.9), 28, (-2.4, 7.4, 3.3))
        spot.scale = (0.9, 0.9, 1); spot.rotation_euler = (0, 0, 1.4)

        # 6 PROUD, THEN GONE: the beam off, ^ ^, a twirl, stars and a puff where the feet landed, then the streak
        fig.location = (0, 0, 0)
        pose(parts, (0, 0, 2.9), face="proud", beam=0.0, claws=8.0, lean=(-0.06, 0.16, 0), yaw=35, squash=(1.06, 1.06, 0.92))
        cards.flat("Burst", (0, 0, 0.05), 4.4, WHITE, 0.8)
        cards.flat("Band", (0, 0, 0.04), 3.0, GOLD, 0.7)
        for k, (a, r, z, sz) in enumerate(((30, 1.3, 0.9, 0.5), (100, 1.5, 1.5, 0.36), (170, 1.2, 0.6, 0.42), (240, 1.6, 1.2, 0.5), (310, 1.3, 1.8, 0.34))):
            cards.face("Star5", polar(r, a, z), sz, GOLD if k % 2 else WHITE, 0.95, roll=a)
        for a in (60, 200, 320):
            cards.face("Puff", polar(1.0, a, 0.25), 0.8, WHITE, 0.55)
        cards.face("Sparkle", (0.9, 0, 3.7), 0.6, WHITE, 0.95)
        cards.face("Sparkle", (-0.95, 0, 3.4), 0.42, GOLD, 0.95)
        frame("6_proud", (0, 0, 1.9), 28, (-2.4, 7.4, 2.3))
        pose(parts, (0.5, 0, 6.6), face="proud", beam=0.0, claws=0.0, lean=(0, -0.3, 0), squash=(0.84, 0.84, 1.36), spin=40)
        cards.line("Line", (0.35, 0, 5.6), (-0.5, 0, 2.6), 0.5, 0.06, WHITE, 0.85)
        cards.line("Line", (0.75, 0, 5.4), (0.2, 0, 3.4), 0.22, 0.04, GOLD, 0.8)
        cards.line("Line", (0.0, 0, 5.3), (-0.75, 0, 3.6), 0.22, 0.04, GOLD, 0.8)
        cards.face("Sparkle", (-0.5, 0, 2.7), 0.7, WHITE, 0.9)
        spot.location = (60, 0, 0)
        frame("6_leave", (0, 0, 3.6), 24, (-2.6, 9.0, 2.6))
    print("DRONE_REVIEW_OK", version)


if __name__ == "__main__":
    main()
