"""The arena's HOLO kit: gigantic hologram advertisements, and the massive slipper (ARENA-1.4).

    py -3 tools/author_arena_textures_holo.py            (the textures, first)
    blender -b --python tools/author_arena_holo.py -- --version=vN [--shots=eye,air,...] [--no-render]
    blender -b --python tools/author_arena_holo.py -- --version=vN --options     (the three balloon designs, blocked)
    blender -b --python tools/author_arena_holo.py -- --version=vN --shots=pose  (only the balloon's posed pictures)

Builds ArtSource/arena/kits/holo.blend (collection `arena_holo`), tools/arena_holo_motion.json and
the review pictures Logs/arena/holo/holo_<shot>_vN.png. Read docs/ARENA_ART_BRIEF.md first.

The owner, 2026-10-05, having played the map: "we're missing some gigantic holograms throughout the
map, i was thinking of like the screenshots attached could be used like ads (should include pc
express there)" (his pictures: a skyline with a wireframe globe and a ring of text, glowing logos on
rooftops and in the air, small cubes round them, line-art figures; and a city with enormous
translucent columns of stacked advertisements rising far above the buildings), and "i want a massive
slipper thing, idk if i want it as a hologram or something like a balloon, like the balloon cow in
overwatch".

WHAT IS HERE, all of it placed BY SIGHTLINE (`SITES`, and the SIGHT report printed on every build:
from a player's eye by the can, how many degrees of each thing stand over the stadium, and how much
of it the towers and the stadium really leave in view, by casting rays at the other kits):
  1. SIX AD COLUMNS. A tall, straight, FLAT strip of advertisements 52 m wide, square on to the can,
     that Unity scrolls upward; a second strip behind it reading the right way for the air; between
     and round them a dotted sheet, a soft beam, edge lines and scan bands, each in its own plane;
     a projector bar on two emitter buoys under it. Four stand at the open corners' shoulders 390
     to 410 m out, two further off among the towers. They top out ABOVE the towers.
  2. THE GLOBE over the east stand: a wireframe sphere 104 m across that turns on a leaning axis, and
     a ring of gold text that turns the other way.
  3. LOGOS AND LINE-ART FIGURES of the game's world, each a sheet of light with a dotted sheet behind
     it, corner brackets in front, a scan hoop, a fan of light and an emitter buoy: PC EXPRESS twice
     (near, over the east stand; far, on tower T22's roof; and on every ad column), the TUMP stamp, a jeepney, a carabao, a
     rooster, the can being struck, a sprig of sampaguita. Cubes of light drift round the big ones.
  4. THE MASSIVE SLIPPER, both ways, for the owner to choose:
       the BALLOON  an inflatable slipper CHARACTER 60 m tall, fat and round, with stubby arms and
                    feet, a scarf and a small happy face, sitting in the air over the south stand's
                    canopy and leaning toward the stage, moored by a tether and four guy ropes to a
                    winch and anchor rings on the canopy. Closed solids, painted, in RIGID PARTS
                    (body, two arms, two legs, two scarf tails, five ropes) that Unity animates:
                    Runtime/Map/ArenaBalloon.cs. (Its first design was a flat sole with a staring
                    face: see `balloon`.)
       the HOLOGRAM the same slipper as a wireframe with a faint striped volume, 84 m, standing on its heel and turning over
                    an emitter buoy north of the stadium.

HOW A HOLOGRAM IS BUILT so it is not a sticker: nothing is one flat quad. A sheet bows; a dotted
sheet stands behind it and brackets in front, so the layers slide against each other as the camera
moves; the hardware under it is a real closed solid. NOTHING IS COPLANAR: every layer has its own
offset. Anything that is READ (a logo, lettering) is a pair of one-sided sheets back to back, so it
reads the right way round from the stage and from the air and is never drawn mirrored; that is
material `arena_holo_logo`. Symmetric light (lines, dots, beams, cubes) is two-sided `arena_holo_fx`.

MATERIALS (6): arena_holo_ads, arena_holo_fx, arena_holo_logo (the fx atlas, one-sided),
arena_holo_metal, arena_holo_led, arena_holo_balloon.

WHAT MOVES is written to tools/arena_holo_motion.json and driven by Runtime/Map/ArenaHoloMotion.cs.
Every moving thing is its own object with its pivot where it turns.

Blender units are metres, z up, y north, the can is the origin. A bearing is degrees clockwise from
north. No object has a parent (the exporter refuses one).
"""
import bpy, bmesh, json, math, os, random, sys
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import arena_kit as K                      # noqa: E402  polar(), rim_elevation(), the stadium's numbers
import author_arena_textures_holo as T     # noqa: E402  the atlas boxes, the balloon's outline and islands (constants only)

ROOT = K.ROOT
KITS = os.path.join(ROOT, "ArtSource", "arena", "kits")
LOGS = os.path.join(ROOT, "Logs", "arena", "holo")
TEX = str(T.OUT)
MOTION = os.path.join(ROOT, "tools", "arena_holo_motion.json")
EYE = K.EYE
Z = Vector((0, 0, 1))
PRE = "arena_holo_"

EMIT = {"ads": 1.5, "fx": 1.4, "logo": 1.6, "metal": 1.0, "led": 2.4, "balloon": 1.0}
FILE = {"ads": "ads", "fx": "fx", "logo": "fx", "metal": "metal", "led": "led", "balloon": "balloon"}
FLAT = {"ads": (0.5, 0.8, 0.9), "fx": (0.4, 0.9, 1.0), "logo": (0.9, 0.8, 0.5), "metal": (0.2, 0.22, 0.36), "led": (0.4, 0.9, 1.0),
        "balloon": (0.95, 0.8, 0.15)}
LIGHT = ("ads", "fx", "logo")              # light, not surfaces
BLK = {"blk_yellow": (0.70, 0.60, 0.02), "blk_back": (0.58, 0.48, 0.02), "blk_maroon": (0.30, 0.02, 0.03), "blk_red": (0.48, 0.04, 0.05),
       "blk_cream": (0.90, 0.82, 0.58), "blk_pink": (0.86, 0.26, 0.22)}   # the option blockouts' flat colours: never in the kit
_mats = {}


def material(kind):
    """arena_holo_<kind>: its painted albedo and its emission image."""
    if kind in _mats:
        return _mats[kind]
    if kind in BLK:
        m = bpy.data.materials.new(kind)
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        b.inputs["Base Color"].default_value = (*BLK[kind], 1.0)
        b.inputs["Emission Color"].default_value = (*BLK[kind], 1.0)
        b.inputs["Emission Strength"].default_value = 0.35
        b.inputs["Roughness"].default_value = 0.5
        _mats[kind] = m
        return m
    m = bpy.data.materials.new(PRE + kind)
    m.use_nodes = True
    m.diffuse_color = (*FLAT[kind], 1.0)
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs["Roughness"].default_value = 0.55 if kind == "balloon" else 0.85
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(os.path.join(TEX, "arena_holo_%s.png" % FILE[kind]), check_existing=True)
    nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
    e = nt.nodes.new("ShaderNodeTexImage")
    e.image = bpy.data.images.load(os.path.join(TEX, "arena_holo_%s_emit.png" % FILE[kind]), check_existing=True)
    nt.links.new(e.outputs["Color"], b.inputs["Emission Color"])
    b.inputs["Emission Strength"].default_value = EMIT[kind]
    if kind in LIGHT:
        nt.links.new(tex.outputs["Alpha"], b.inputs["Alpha"])
        m.surface_render_method = "BLENDED"
        m.use_backface_culling = kind != "fx"
        # In Unity these are ADDED to the sky (TumbangPreso/ArenaGlow, SrcAlpha One). The Principled
        # node above is what the exporter reads (its files, its strength, its alpha); what Blender
        # DRAWS is the same thing added: the emission image over a clear sheet. A blended sheet
        # would darken what is behind it, which a hologram never does.
        em = nt.nodes.new("ShaderNodeEmission")
        em.inputs["Strength"].default_value = EMIT[kind] * 1.15
        nt.links.new(e.outputs["Color"], em.inputs["Color"])
        clear = nt.nodes.new("ShaderNodeBsdfTransparent")
        add = nt.nodes.new("ShaderNodeAddShader")
        nt.links.new(em.outputs[0], add.inputs[0])
        nt.links.new(clear.outputs[0], add.inputs[1])
        out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
        nt.links.new(add.outputs[0], out.inputs["Surface"])
    _mats[kind] = m
    return m


# ---------------------------------------------------------------- frames and the mesh builder
class Frame:
    """A place seen from the can: `o` its point, `dx` the viewer's right, `dy` away from the can."""

    def __init__(self, bearing, r, z=0.0, turn=0.0):
        self.bearing, self.r, self.z = bearing, r, z
        p = K.polar(r, bearing)
        self.o = Vector((p.x, p.y, z))
        a = math.radians(bearing + turn)
        self.dx = Vector((math.cos(a), -math.sin(a), 0.0))
        self.dy = Vector((math.sin(a), math.cos(a), 0.0))

    def p(self, x, y, z):
        """A point x to the right, y further from the can, z above this frame's point."""
        return self.o + self.dx * x + self.dy * y + Z * z


def led_uv(name):
    row = T.LED.index(name)
    return (0.05, (row + 0.2) / 8.0, 0.95, (row + 0.8) / 8.0)


class Mesh:
    """One object. Points are given in the WORLD; `done` moves them to the object's own space, its
    origin at `origin` (the pivot) and, with `basis`, its axes turned (a leaning axis to spin on)."""

    def __init__(self, name, origin, basis=None):
        self.name, self.bm, self.slots = PRE + name, bmesh.new(), []
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.origin = Vector(origin)
        self.basis = basis

    def slot(self, kind):
        if kind not in self.slots:
            self.slots.append(kind)
        return self.slots.index(kind)

    def face(self, pts, kind, uvs, smooth=False):
        f = self.bm.faces.new([self.bm.verts.new(p) for p in pts])
        f.material_index = self.slot(kind)
        f.smooth = smooth
        for l, uv in zip(f.loops, uvs):
            l[self.uv].uv = uv
        return f

    def quad(self, pts, kind, box, smooth=False):
        """pts: bottom-left, bottom-right, top-right, top-left as its reader sees it."""
        u0, v0, u1, v1 = box
        return self.face(pts, kind, ((u0, v0), (u1, v0), (u1, v1), (u0, v1)), smooth)

    def ribbon(self, pts, width, side, kind="fx", tile="line", closed=False, cross=None):
        """A line of light along pts: a strip `width` wide lying along `side` (a direction, or a
        function of the point's index), and with `cross` a second strip across it, so the line
        cannot be seen edge-on from anywhere."""
        u0, v0, u1, v1 = T.atlas_uv(T.FX[tile])
        n = len(pts)
        for i in range(n if closed else n - 1):
            a, b = Vector(pts[i]), Vector(pts[(i + 1) % n])
            for which in ((side, cross) if cross is not None else (side,)):
                sa = (which(i) if callable(which) else which).normalized() * (width / 2)
                sb = (which((i + 1) % n) if callable(which) else which).normalized() * (width / 2)
                self.quad([a - sa, b - sb, b + sb, a + sa], kind, (u0, v0, u1, v1))

    def lathe(self, centre, profile, sides=16, kinds=None, axis=Z, phase=0.0):
        """A closed solid of revolution: profile is (radius, height, kind) from the bottom of the
        axis round the outside to the top; a radius of 0 is on the axis. The kind names the strip
        from that point to the next."""
        centre = Vector(centre)
        ax = axis.normalized()
        ref = Vector((1, 0, 0)) if abs(ax.x) < 0.9 else Vector((0, 1, 0))
        u = ax.cross(ref).normalized()
        v = ax.cross(u)
        bm = self.bm
        cols = []
        for i in range(sides):
            t = phase + math.tau * i / sides
            d = u * math.cos(t) + v * math.sin(t)
            cols.append([None if r < 1e-6 else bm.verts.new(centre + d * r + ax * h) for r, h, _ in profile])
        poles = {j: bm.verts.new(centre + ax * h) for j, (r, h, _) in enumerate(profile) if r < 1e-6}
        length = 0.0
        for j in range(len(profile) - 1):
            r0, h0, kind = profile[j]
            r1, h1, _ = profile[j + 1]
            step = math.hypot(r1 - r0, h1 - h0)
            for i in range(sides):
                k = (i + 1) % sides
                a, b = cols[i][j] or poles[j], cols[k][j] or poles[j]
                c, d = cols[k][j + 1] or poles[j + 1], cols[i][j + 1] or poles[j + 1]
                loop = []
                for vert in (a, b, c, d):
                    if vert not in loop:
                        loop.append(vert)
                if len(loop) < 3:
                    continue
                f = bm.faces.new(loop)
                f.material_index = self.slot("led" if kind.startswith("led.") else kind)
                if kind.startswith("led."):
                    bx = led_uv(kind[4:])
                    uvs = {a: (bx[0], bx[1]), b: (bx[2], bx[1]), c: (bx[2], bx[3]), d: (bx[0], bx[3])}
                else:
                    rm = max(r0, r1)
                    s = 1.0 / T.METAL_TILE_M
                    uvs = {a: (math.tau * rm * i / sides * s, length * s), b: (math.tau * rm * (i + 1) / sides * s, length * s),
                           c: (math.tau * rm * (i + 1) / sides * s, (length + step) * s), d: (math.tau * rm * i / sides * s, (length + step) * s)}
                for l in f.loops:
                    l[self.uv].uv = uvs[l.vert]
            length += step
        return self

    def tube(self, p0, p1, r, sides=6, r1=None, kind="metal", cap=None):
        """A closed prism from p0 to p1: a strut, a rope, a pod."""
        p0, p1 = Vector(p0), Vector(p1)
        L = (p1 - p0).length
        r1 = r if r1 is None else r1
        return self.lathe(p0, [(0, 0, cap or kind), (r, 0, kind), (r1, L, cap or kind), (0, L, kind)], sides, axis=p1 - p0)

    def box(self, c, size, xdir, kind="metal", kinds=None):
        """A closed box at c: size is (along xdir, along the level direction across it, up)."""
        X = xdir.normalized()
        Y = Z.cross(X).normalized()
        c = Vector(c)
        hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        P = lambda a, b, d: c + X * (a * hx) + Y * (b * hy) + Z * (d * hz)
        sides = {"-y": [P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1)], "+y": [P(1, 1, -1), P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1)],
                 "-x": [P(-1, 1, -1), P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1)], "+x": [P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1)],
                 "-z": [P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1), P(-1, -1, -1)], "+z": [P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1)]}
        dims = {"-y": (size[0], size[2]), "+y": (size[0], size[2]), "-x": (size[1], size[2]), "+x": (size[1], size[2]), "-z": (size[0], size[1]), "+z": (size[0], size[1])}
        vs = {}
        for key, pts in sides.items():
            loop = []
            for p in pts:
                k = (round(p.x, 4), round(p.y, 4), round(p.z, 4))
                if k not in vs:
                    vs[k] = self.bm.verts.new(p)
                loop.append(vs[k])
            kd = (kinds or {}).get(key, kind)
            f = self.bm.faces.new(loop)
            f.material_index = self.slot("led" if kd.startswith("led.") else kd)
            if kd.startswith("led."):
                bx = led_uv(kd[4:])
                uv = ((bx[0], bx[1]), (bx[2], bx[1]), (bx[2], bx[3]), (bx[0], bx[3]))
            else:
                w, h = dims[key][0] / T.METAL_TILE_M, dims[key][1] / T.METAL_TILE_M
                uv = ((0, 0), (w, 0), (w, h), (0, h))
            for l, c2 in zip(f.loops, uv):
                l[self.uv].uv = c2
        return self

    def done(self, coll, weld=True):
        bm = self.bm
        inv = Matrix.Translation(-self.origin)
        if self.basis is not None:
            inv = self.basis.to_4x4().inverted() @ inv
        bmesh.ops.transform(bm, matrix=inv, verts=bm.verts)
        if weld:
            bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0004)
        solid = [f for f in bm.faces if self.slots[f.material_index] not in LIGHT]     # a sheet of light keeps the winding it was given
        if solid:
            bmesh.ops.recalc_face_normals(bm, faces=solid)
        me = bpy.data.meshes.new(self.name)
        bm.to_mesh(me); bm.free()
        for kind in self.slots:
            me.materials.append(material(kind))
        ob = bpy.data.objects.new(self.name, me)
        ob.location = self.origin
        if self.basis is not None:
            ob.rotation_euler = self.basis.to_euler()
        coll.objects.link(ob)
        return ob


# ---------------------------------------------------------------- the parts every hologram is built from
def buoy(m, top, R=9.0, sides=16, pods=3, ring="cyan", band="magenta"):
    """AN EMITTER BUOY, a closed solid: a dish with a lens in its crown, a lit ring round the lens,
    a lit band round its rim, and thruster pods under it that run up INSIDE the hull and end in a
    lit nozzle. It hovers as the stadium does; `top` is the middle of its lens."""
    top = Vector(top)
    prof = [(0, -0.62 * R, "metal"), (0.30 * R, -0.60 * R, "metal"), (0.72 * R, -0.46 * R, "metal"), (R, -0.24 * R, "led." + band),
            (R, -0.10 * R, "metal"), (0.90 * R, 0.0, "led." + ring), (0.58 * R, 0.0, "metal"), (0.50 * R, 0.07 * R, "led.white"), (0, 0.10 * R, "metal")]
    m.lathe(top, prof, sides)
    for k in range(pods):
        a = math.tau * (k + 0.5) / pods
        c = top + Vector((math.cos(a), math.sin(a), 0)) * (0.70 * R)
        m.tube(c - Z * (0.22 * R), c - Z * (0.95 * R), 0.17 * R, 8, 0.13 * R, cap="led.thrust")
        m.tube(top + Vector((math.cos(a), math.sin(a), 0)) * (0.20 * R) - Z * (0.30 * R), c - Z * (0.70 * R), 0.05 * R, 5)   # a brace from the hull's middle into the pod
    return m


def crossed(m, base, top, width, tile="beam", n=3):
    """A column of light: n upright sheets through one axis, so it is soft from every side."""
    box = T.atlas_uv(T.FX[tile])
    base, top = Vector(base), Vector(top)
    for k in range(n):
        a = math.pi * k / n + 0.3
        s = Vector((math.cos(a), math.sin(a), 0)) * (width / 2)
        m.quad([base - s, base + s, top + s, top - s], "fx", box)


def hoop(m, centre, radius, height, sides=20, tile="scan", turns=2.0):
    """A scan band standing round an axis."""
    u0, v0, u1, v1 = T.atlas_uv(T.FX[tile])
    centre = Vector(centre)
    for k in range(sides):
        a0, a1 = math.tau * k / sides, math.tau * (k + 1) / sides
        p0 = centre + Vector((math.cos(a0), math.sin(a0), 0)) * radius
        p1 = centre + Vector((math.cos(a1), math.sin(a1), 0)) * radius
        f0, f1 = (k * turns / sides) % 1.0, ((k * turns / sides) % 1.0) + turns / sides
        m.quad([p0, p1, p1 + Z * height, p0 + Z * height], "fx", (u0 + (u1 - u0) * f0, v0, u0 + (u1 - u0) * min(f1, 1.0), v1))


def sheet(m, F, w, h, tile, kind="logo", y=0.0, bow=0.035, segs=6, back=False):
    """A bowed sheet of light facing the can (or, `back`, facing away and reading the right way from
    there): its middle stands `bow` widths further from its reader than its edges."""
    u0, v0, u1, v1 = T.atlas_uv(T.FX[tile])
    for k in range(segs):
        f0, f1 = k / segs, (k + 1) / segs
        pts = []
        for f in (f0, f1):
            x = (f - 0.5) * w
            d = bow * w * (1 - (2 * f - 1) ** 2)
            pts.append((x, y + (d if not back else -d)))
        (xa, ya), (xb, yb) = pts
        if not back:
            m.quad([F.p(xa, ya, -h / 2), F.p(xb, yb, -h / 2), F.p(xb, yb, h / 2), F.p(xa, ya, h / 2)], kind,
                   (u0 + (u1 - u0) * f0, v0, u0 + (u1 - u0) * f1, v1))
        else:                                                     # seen from behind: its reader's left is +x here
            m.quad([F.p(xb, yb, -h / 2), F.p(xa, ya, -h / 2), F.p(xa, ya, h / 2), F.p(xb, yb, h / 2)], kind,
                   (u0 + (u1 - u0) * (1 - f1), v0, u0 + (u1 - u0) * (1 - f0), v1))


def brackets(m, F, w, h, y, arm=0.13, thick=None):
    """Four corner brackets of light standing in front of a sheet."""
    thick = thick or max(0.9, 0.012 * w)
    L = arm * min(w, h * 1.6)
    for sx in (-1, 1):
        for sz in (-1, 1):
            cx, cz = sx * w / 2, sz * h / 2
            m.ribbon([F.p(cx, y, cz), F.p(cx - sx * L, y, cz)], thick, Z)
            m.ribbon([F.p(cx, y, cz), F.p(cx, y, cz - sz * L)], thick, F.dx)


def cubes(coll, name, F, spread, count, seed, size=(3.0, 7.5), keep_out=0.0):
    """Cubes of light drifting round a hologram: one object, its pivot in their middle."""
    rng = random.Random(seed)
    m = Mesh(name, F.o)
    box = T.atlas_uv(T.FX["cube"])
    made = 0
    while made < count:
        x, y, z = rng.uniform(-spread[0], spread[0]), rng.uniform(-spread[1], spread[1]), rng.uniform(-spread[2], spread[2])
        if abs(x) < keep_out and abs(z) < keep_out * 0.6:
            continue
        c = F.p(x, y, z)
        s = rng.uniform(*size) / 2
        rot = Matrix.Rotation(rng.uniform(0, math.tau), 3, "Z") @ Matrix.Rotation(rng.uniform(0.2, 1.0), 3, "X")
        ax = [rot @ Vector(v) for v in ((1, 0, 0), (0, 1, 0), (0, 0, 1))]
        for i in range(3):
            for sg in (-1, 1):
                n, a, b = ax[i] * sg, ax[(i + 1) % 3], ax[(i + 2) % 3] * sg
                m.quad([c + (n - a - b) * s, c + (n + a - b) * s, c + (n + a + b) * s, c + (n - a + b) * s], "fx", box)
        made += 1
    return m.done(coll, weld=False)


# ---------------------------------------------------------------- the sites
# THE PLAN, BY SIGHTLINE. From the can the stadium hides the sky up to 23.1 degrees over a canopy and
# 18.3 behind a corner screen (9.8 either side of the screen, down to the concourse). The city's
# towers stand outside radius 300: landmarks on the corners' axes at 365 to 395 m, flankers at 480 to
# 540, the rest at 620 to 900 over the stands, leaving one avenue of sky a side. So:
#   NEAR (255 to 380 m, inside the towers): the globe, the figures, the logos, the slipper hologram,
#        each over the middle of a stand in front of a GAP or a low far tower, 26 to 41 degrees up;
#   MID  (390 to 410 m): four ad columns at the corners' shoulders, seen from the concourse up;
#   FAR  (620 to 760 m): two ad columns among the stand towers, and PC EXPRESS on tower T22's roof.
# The two orbits of sky traffic (radius 268 at z 140, radius 284 at z 176) pass in FRONT of the near
# ring and through nothing solid.
COLUMNS = [   # name, bearing, distance, foot of the strip, its top, the strip facing the can
    ("ne", 58.0, 400.0, 44.0, 548.0, 0),
    ("se", 148.0, 400.0, 44.0, 572.0, 1),
    ("sw", 212.0, 410.0, 44.0, 548.0, 0),
    ("nw", 328.0, 390.0, 44.0, 596.0, 1),
    ("s_far", 180.5, 700.0, 190.0, 870.0, 0),
    ("w_far", 290.0, 620.0, 150.0, 786.0, 1),
]
# name, tile, bearing, distance, height of its middle, width, colour of its hardware's lights
CARDS = [
    ("pcx_east", "pcx", 110.0, 380.0, 262.0, 112.0, ("white", "cyan")),
    ("tump", "tump", 352.0, 330.0, 218.0, 84.0, ("gold", "cyan")),
    ("sampaguita", "sampaguita", 68.0, 330.0, 206.0, 60.0, ("white", "magenta")),
    ("jeepney", "jeepney", 161.0, 340.0, 226.0, 104.0, ("gold", "magenta")),
    ("carabao", "carabao", 249.0, 340.0, 252.0, 88.0, ("cyan", "gold")),
    ("can", "can", 267.0, 330.0, 206.0, 50.0, ("white", "gold")),
    ("rooster", "rooster", 284.5, 330.0, 214.0, 48.0, ("magenta", "cyan")),
]
GLOBE = dict(bearing=86.0, r=320.0, z=222.0, R=52.0)
SLIPPER_HOLO = dict(bearing=22.0, r=255.0, z=174.0, length=84.0)
# The tether point, on the south canopy. The canopy's masts stand at radius 184.5 every ten degrees
# (..., 199.5, 209.5) with stays running in to its front edge: the mooring is laid BETWEEN two masts,
# and the balloon's heel rides above their heads (z 91).
# The BODY floats a little inboard and 2.5 degrees east of its mooring, so it stands clear of the
# south-west ad column (bearing 212) seen from the can.
BALLOON_AT = dict(bearing=204.5, r=178.0, body_bearing=202.0, body_r=171.0, yaw=6.0, lean=12.0, lift=27.0)
PCX_FAR = dict(tower="T22", bearing=189.0, r=760.0, width=132.0)
MOVES = []
BALLOON_OBJECTS = []
BALLOON_RIG = {}                           # the balloon's parts, for the posed pictures


def move(ob, kind, **kw):
    MOVES.append(dict(object=ob.name, kind=kind, **kw))
    return ob


COLUMN_W = 52.0                            # one advertisement across: an ad is 52 m by 26 m, so nothing is stretched


def column(coll, name, bearing, r, z0, z1, strip, built):
    """AN AD COLUMN: A TALL, STRAIGHT, FLAT STRIP OF LIGHT, square on to the can. (The first build bent
    the strip round a tube; the owner, 2026-10-05: "the ad columns should be straight flat, not
    tubes". His reference is flat banners of stacked advertisements standing in the sky.)

    One ad fills the strip's width, so an ad is read whole and undistorted from the stage; v is the
    height over eight ads, so moving v scrolls the stack. Depth the flat way, every layer in its own
    plane (metres further from the can than the front strip):
        -1.4  a scan band across the head and the foot       -0.8  edge lines and divider lines
         0.0  THE STRIP, one-sided, reading toward the can    0.9  a soft beam, wider than the strip
         1.6  a broken dotted sheet                           3.0  THE BACK STRIP, the other eight ads,
         3.8  the back's edge lines                                one-sided, reading the right way
         4.4  the back's scan bands                                from the air
    and one sheet of beam square across them all, so the column is not a hairline seen edge-on.
    Under it a projector bar rides on two emitter buoys."""
    F = Frame(bearing, r)
    w = COLUMN_W
    ad_h = w / T.AD_ASPECT
    ads = Mesh("column_%s_ads" % name, F.p(0, 0, z0))
    for face, st in ((0, strip), (1, 1 - strip)):
        ua, ub, _ = T.ad_uv(st)
        ua, ub = ua + 0.5 / T.ADS_W, ub - 0.5 / T.ADS_W
        v0 = 0.37 * face
        v1 = v0 + (z1 - z0) / (ad_h * T.ADS_PER_STRIP)
        if face == 0:
            pts = [F.p(-w / 2, 0.0, z0), F.p(w / 2, 0.0, z0), F.p(w / 2, 0.0, z1), F.p(-w / 2, 0.0, z1)]
        else:                                                     # seen from behind: its reader's left is +x here
            pts = [F.p(w / 2, 3.0, z0), F.p(-w / 2, 3.0, z0), F.p(-w / 2, 3.0, z1), F.p(w / 2, 3.0, z1)]
        ads.quad(pts, "ads", (ua, v0, ub, v1))
    ob = move(ads.done(coll, weld=False), "scroll", v_per_second=0.018 + 0.004 * (strip * 2 - 1), material="arena_holo_ads")
    built.append(("column " + name, [ob], "ad column"))

    rig = Mesh("column_%s_rig" % name, F.p(0, 0, z0))
    dbox = T.atlas_uv(T.FX["dots"])
    cell = w * 1.12 / 4
    rows = int((z1 - z0 + 12.0) / cell)
    for j in range(rows):                                         # the dotted sheet: square panels, one in three left out
        for k in range(4):
            if (j + k * 2 + int(bearing)) % 3 == 0:
                continue
            xa, za = -w * 0.56 + k * cell, z0 - 6.0 + j * cell
            rig.quad([F.p(xa, 1.6, za), F.p(xa + cell, 1.6, za), F.p(xa + cell, 1.6, za + cell), F.p(xa, 1.6, za + cell)], "fx", dbox)
    beam = T.atlas_uv(T.FX["beam"])
    foot, head = z0 - 26.0, z1 + 80.0
    rig.quad([F.p(-w * 0.85, 0.9, foot), F.p(w * 0.85, 0.9, foot), F.p(w * 0.85, 0.9, head), F.p(-w * 0.85, 0.9, head)], "fx", beam)
    rig.quad([F.p(0.0, -w * 0.30, foot), F.p(0.0, w * 0.36, foot), F.p(0.0, w * 0.36, head), F.p(0.0, -w * 0.30, head)], "fx", beam)
    scan = T.atlas_uv(T.FX["scan"])
    for y, sgn in ((-0.8, 1), (3.8, -1)):                         # the frame: an edge line up each side, a divider every four ads
        for sx in (-1, 1):
            rig.ribbon([F.p(sx * (w / 2 + 1.3), y, z0 - 3.0), F.p(sx * (w / 2 + 1.3), y, z1 + 3.0)], 1.1, F.dx, cross=F.dy)
        zz = z0 + ad_h * 4
        while zz < z1 - 1.0:
            rig.ribbon([F.p(-w / 2 - 0.4, y + 0.2 * sgn, zz), F.p(w / 2 + 0.4, y + 0.2 * sgn, zz)], 0.8, Z)
            zz += ad_h * 4
        for zz in (z0 - 3.0, z1 + 3.0):                           # corner ticks closing the frame at the foot and the head
            for sx in (-1, 1):
                rig.ribbon([F.p(sx * (w / 2 + 1.85), y - 0.2 * sgn, zz), F.p(sx * (w / 2 - 7.0), y - 0.2 * sgn, zz)], 1.1, Z)
    for y in (-1.4, 4.4):
        for zz in (z0 - 9.0, z1 + 5.0):
            rig.quad([F.p(-w * 0.54, y, zz), F.p(w * 0.54, y, zz), F.p(w * 0.54, y, zz + 3.4), F.p(-w * 0.54, y, zz + 3.4)], "fx", scan)
    # The emitter: a projector bar under the strip, its ends inside two buoys.
    band = "gold" if strip == 0 else "magenta"
    top = z0 - 26.0
    rig.box(F.p(0.0, 1.5, top - 1.0), (w * 0.94, 5.0, 3.2), F.dx, kinds={"+z": "led.white", "-y": "led." + band, "+y": "led." + band})
    for sx in (-1, 1):
        buoy(rig, F.p(sx * w * 0.47, 1.5, top), 9.5, 16 if strip == 0 else 8, 3, "cyan", band)
        rig.tube(F.p(sx * w * 0.47, 1.5, top - 0.6), F.p(sx * w * 0.47, 1.5, top + 6.0), 1.3, 6, 0.7, cap="led.white")   # a horn in each buoy's lens
    fan = T.atlas_uv(T.FX["slice"])
    rig.quad([F.p(-w * 0.47, 1.25, top + 0.8), F.p(w * 0.47, 1.25, top + 0.8), F.p(w * 0.5, 1.25, z0 - 9.5), F.p(-w * 0.5, 1.25, z0 - 9.5)], "fx", fan)
    built.append(("column " + name, [rig.done(coll)], None))


def card(coll, name, tile, bearing, r, zc, w, lights, built, ray_roof=None):
    """A LOGO OR A FIGURE: two one-sided sheets back to back (it reads the right way from both
    sides), a dotted sheet between them and larger than they are, brackets in front, a scan hoop
    and a fan of light from the emitter buoy under it."""
    x0, y0, x1, y1 = T.FX[tile]
    h = w * (y1 - y0) / (x1 - x0)
    F = Frame(bearing, r, zc)
    m = Mesh("sign_%s" % name, F.o)
    sheet(m, F, w, h, tile, "logo", y=0.0)
    sheet(m, F, w, h, tile, "logo", y=3.2, back=True)
    dz = T.atlas_uv(T.FX["dots"])
    dw, dh = w * 1.14, h * 1.22
    nx = max(2, round(dw / max(dh / 2, 1e-3) / 1.0)) if dw > dh else 2
    ny = max(2, round(dh / (dw / nx)))
    for i in range(nx):                                           # the dotted sheet, in square panels, 1.6 m behind the front sheet
        for j in range(ny):
            xa, xb = -dw / 2 + dw * i / nx, -dw / 2 + dw * (i + 1) / nx
            za, zb = -dh / 2 + dh * j / ny, -dh / 2 + dh * (j + 1) / ny
            m.quad([F.p(xa, 1.6, za), F.p(xb, 1.6, za), F.p(xb, 1.6, zb), F.p(xa, 1.6, zb)], "fx", dz)
    brackets(m, F, w * 1.08, h * 1.14, -2.6)
    brackets(m, F, w * 1.08, h * 1.14, 5.8)
    ob = move(m.done(coll, weld=False), "bob", metres=1.2, period=9.0 + (len(name) % 4))
    move(ob, "pulse", low=0.86, high=1.0, period=3.4 + 0.3 * (len(name) % 5))
    built.append((name, [ob], "logo" if tile in ("pcx", "tump") else "figure"))

    rig = Mesh("sign_%s_rig" % name, F.o)
    drop = h / 2 + max(22.0, h * 0.42)
    top = F.p(0, 1.6, -drop)
    fan = T.atlas_uv(T.FX["fan"])
    for a in (0.0, math.pi / 2):                                  # the fan: two sheets from the lens to the sheet's foot
        s = (F.dx * math.cos(a) + F.dy * math.sin(a)) * (w * (0.52 if a == 0.0 else 0.10))
        rig.quad([top - s + Z * 0.6, top + s + Z * 0.6, F.p(0, 1.6, -h / 2 + 2.0) + s, F.p(0, 1.6, -h / 2 + 2.0) - s], "fx", fan)
    hoop(rig, top + Z * (drop - h / 2 - 9.0), w * 0.20, 2.6, 18)
    hoop(rig, top + Z * 5.0, w * 0.09, 1.8, 14)
    buoy(rig, top, max(6.0, w * 0.085), 12, 3, lights[0], lights[1])
    built.append((name, [rig.done(coll)], None))
    return F, h


def globe(coll, built):
    """THE GLOBE: meridians and parallels as strips lying on the sphere, a faint striped shell
    inside it, and a ring of text. The globe turns on an axis leaning 16 degrees; the ring leans
    21 degrees the other way and turns against it."""
    g = GLOBE
    F = Frame(g["bearing"], g["r"], g["z"])
    R = g["R"]
    tilt = Matrix.Rotation(math.radians(16.0), 3, F.dy)
    m = Mesh("globe", F.o, basis=tilt)
    W = lambda v: F.o + tilt @ v
    P = lambda lam, phi, rr=R: Vector((rr * math.cos(phi) * math.cos(lam), rr * math.cos(phi) * math.sin(lam), rr * math.sin(phi)))
    for k in range(12):
        lam = math.tau * k / 12
        e = Vector((-math.sin(lam), math.cos(lam), 0))
        pts = [W(P(lam, math.radians(-84 + 168 * j / 22))) for j in range(23)]
        m.ribbon(pts, 1.7, tilt @ e)
    for phi_d in (-66, -44, -22, 0, 22, 44, 66):
        phi = math.radians(phi_d)
        pts = [W(P(math.tau * j / 40, phi)) for j in range(40)]
        norths = [tilt @ Vector((-math.sin(phi) * math.cos(math.tau * j / 40), -math.sin(phi) * math.sin(math.tau * j / 40), math.cos(phi))) for j in range(40)]
        m.ribbon(pts, 3.0 if phi_d == 0 else 1.7, lambda i, n=norths: n[i], closed=True)
    sl = T.atlas_uv(T.FX["slice"])
    for i in range(12):                                           # the shell inside: the globe has a body, not only lines
        for j in range(6):
            l0, l1 = math.tau * i / 12, math.tau * (i + 1) / 12
            p0, p1 = math.radians(-78 + 156 * j / 6), math.radians(-78 + 156 * (j + 1) / 6)
            if (i + j) % 2:
                continue
            m.quad([W(P(l0, p0, R * 0.9)), W(P(l1, p0, R * 0.9)), W(P(l1, p1, R * 0.9)), W(P(l0, p1, R * 0.9))], "fx", sl)
    ob = move(m.done(coll, weld=False), "rotate", degrees_per_second=5.0)
    built.append(("globe", [ob], "globe"))

    lean = Matrix.Rotation(math.radians(-21.0), 3, F.dx) @ Matrix.Rotation(math.radians(8.0), 3, F.dy)
    ring = Mesh("globe_ring", F.o, basis=lean)
    u0, v0, u1, v1 = T.atlas_uv(T.FX["ring"])
    rr, hh, n = R * 1.42, R * 0.36, 48
    for k in range(n):                                            # the text runs round twice; only the OUTSIDE is drawn (one-sided)
        a0, a1 = math.tau * k / n, math.tau * (k + 1) / n
        p0 = F.o + lean @ Vector((rr * math.cos(a0), rr * math.sin(a0), -hh / 2))
        p1 = F.o + lean @ Vector((rr * math.cos(a1), rr * math.sin(a1), -hh / 2))
        up = lean @ Vector((0, 0, hh))
        f0 = (k % (n // 2)) / (n // 2)
        ring.quad([p0, p1, p1 + up, p0 + up], "logo", (u0 + (u1 - u0) * f0, v0, u0 + (u1 - u0) * (f0 + 2.0 / n), v1))
    for off, rad in ((-hh * 0.62, rr + 3.0), (hh * 0.62, rr + 3.0)):   # a rail of light above and below it, a little outside
        pts = [F.o + lean @ Vector((rad * math.cos(math.tau * j / 48), rad * math.sin(math.tau * j / 48), off)) for j in range(48)]
        ring.ribbon(pts, 1.3, lean @ Z, closed=True)
    ob = move(ring.done(coll, weld=False), "rotate", degrees_per_second=-9.0)
    built.append(("globe", [ob], None))

    rig = Mesh("globe_rig", F.o)
    top = F.p(0, 0, -R - 34.0)
    fan = T.atlas_uv(T.FX["fan"])
    for a in (0.0, math.pi / 2):
        s = (F.dx * math.cos(a) + F.dy * math.sin(a)) * (R * 0.9)
        rig.quad([top - s + Z * 0.7, top + s + Z * 0.7, F.p(0, 0, -R * 0.55) + s, F.p(0, 0, -R * 0.55) - s], "fx", fan)
    hoop(rig, top + Z * 9.0, R * 0.22, 2.4, 18)
    hoop(rig, top + Z * 22.0, R * 0.42, 2.8, 22)
    buoy(rig, top, 11.0, 16, 4, "cyan", "gold")
    built.append(("globe", [rig.done(coll)], None))
    ob = move(cubes(coll, "globe_cubes", F, (R * 1.9, R * 0.9, R * 1.2), 11, 71, keep_out=R * 1.25), "bob", metres=2.2, period=13.0)
    built.append(("globe", [ob], None))


# ---------------------------------------------------------------- the slipper, both ways
def sole_point(x, t, z, L, W):
    """A point of the slipper in ITS OWN frame: x across (widths), t heel to toe (lengths), z out of
    the footbed (metres)."""
    return Vector((x * W, t * L, z))


def slipper_hologram(coll, built):
    """THE SLIPPER AS A HOLOGRAM. It stands on its heel as the balloon does, 12 degrees off upright,
    and turns about the vertical, so twice a turn it shows the stage its whole footbed. Drawn the way
    the figures are: a BOLD outline of the footbed, a welt line inside it, the tread's outline behind,
    eight posts between the two, the strap as a bold line with a fine one either side, and the
    footbed and the tread as faint striped sheets, which is its volume.
    (The first version lay at 52 degrees with hoops round it and read as a wire boat.)"""
    s = SLIPPER_HOLO
    F = Frame(s["bearing"], s["r"], s["z"])
    L = s["length"]
    W, Th = L * 25.0 / 58.0, L * 0.12
    pose = Matrix.Rotation(math.radians(78.0), 3, "X")            # heel down, toe up, leaning back a little
    m = Mesh("slipper", F.o)
    O = T.outline(48)
    n = len(O)
    cx = sum(p[0] for p in O) / n

    def P(x, t, z, widen=1.0):
        return F.o + pose @ Vector(((cx + (x - cx) * widen) * W, (t - 0.5) * widen * L, z))

    outs = []
    for i in range(n):
        a, b = Vector((O[i - 1][0] * W, O[i - 1][1] * L, 0)), Vector((O[(i + 1) % n][0] * W, O[(i + 1) % n][1] * L, 0))
        tg = (b - a).normalized()
        outs.append(pose @ Vector((tg.y, -tg.x, 0)))
    zdir = pose @ Vector((0, 0, 1))
    for z, widen, wd, cross in ((Th / 2, 1.0, 2.2, True), (Th / 2 + 0.3, 0.86, 0.9, False), (-Th / 2, 1.0, 1.3, True)):
        m.ribbon([P(x, t, z, widen) for x, t in O], wd, lambda i: outs[i], closed=True, cross=zdir if cross else None)
    for i in range(0, n, 6):                                      # posts between the footbed's edge and the tread's
        x, t = O[i]
        tg = pose @ (Vector((O[(i + 1) % n][0] * W, O[(i + 1) % n][1] * L, 0)) - Vector((O[i - 1][0] * W, O[i - 1][1] * L, 0))).normalized()
        m.ribbon([P(x, t, -Th / 2), P(x, t, Th / 2)], 0.9, tg, cross=outs[i])
    sl = T.atlas_uv(T.FX["slice"])
    half = n // 2
    for z in (Th / 2 - 0.5, -Th / 2 + 0.5):                       # the footbed and the tread: striped sheets, strip by strip across
        for k in range(1, half - 1):
            (xa, ta), (xb, tb) = O[k], O[k + 1]
            (xc, tc), (xd, td) = O[n - k - 1], O[n - k]
            v0, v1 = sl[1] + (sl[3] - sl[1]) * ta, sl[1] + (sl[3] - sl[1]) * tb
            m.face([P(xd, td, z), P(xa, ta, z), P(xb, tb, z), P(xc, tc, z)], "fx", ((sl[0], v0), (sl[2], v0), (sl[2], v1), (sl[0], v1)))
    post = T.FACE["post_v"]
    A = Vector((T.lean(post) * W, (post - 0.5) * L, Th / 2 + L * 0.075))
    m.ribbon([P(T.lean(post), post, Th / 2), F.o + pose @ A], 2.0, pose @ Vector((1, 0, 0)), cross=pose @ Vector((0, 1, 0)))
    for sx in (-1, 1):                                            # the strap: a bold line and a fine one either side, arching post to side
        t1 = T.FACE["anchor_v"]
        B = Vector(((T.lean(t1) + sx * T.half_width(t1) * 0.90) * W, (t1 - 0.5) * L, Th / 2))
        along = (B - A).normalized()
        side = Vector((along.y, -along.x, 0)).normalized()
        for off in (-2.4, 0.0, 2.4):
            pts = []
            for j in range(13):
                f = j / 12
                c = A.lerp(B, f) + Vector((0, 0, L * 0.085 * math.sin(math.pi * f) * (1 - 0.5 * f)))
                pts.append(F.o + pose @ (c + side * off * (0.4 + 0.6 * math.sin(math.pi * min(1.0, f * 1.3)))))
            m.ribbon(pts, 2.0 if off == 0.0 else 0.8, pose @ side, cross=zdir if off == 0.0 else None)
    ob = move(m.done(coll, weld=False), "rotate", degrees_per_second=11.0)
    move(ob, "bob", metres=1.6, period=7.0)
    built.append(("slipper hologram", [ob], "the slipper, as a hologram"))

    rig = Mesh("slipper_rig", F.o)
    top = F.p(0, 0, -L * 0.5 - 22.0)
    fan = T.atlas_uv(T.FX["fan"])
    for a in (0.0, math.pi / 2):
        sdir = (F.dx * math.cos(a) + F.dy * math.sin(a)) * (L * 0.26)
        rig.quad([top - sdir + Z * 0.7, top + sdir + Z * 0.7, F.p(0, 0, -L * 0.40) + sdir, F.p(0, 0, -L * 0.40) - sdir], "fx", fan)
    hoop(rig, top + Z * 8.0, L * 0.12, 2.2, 18)
    hoop(rig, top + Z * 17.0, L * 0.26, 2.6, 22)
    hoop(rig, F.p(0, 0, L * 0.56), L * 0.26, 2.2, 22)
    buoy(rig, top, 10.0, 16, 3, "gold", "cyan")
    built.append(("slipper hologram", [rig.done(coll)], None))
    ob = move(cubes(coll, "slipper_cubes", F, (L * 0.75, L * 0.5, L * 0.55), 9, 88, keep_out=L * 0.42), "bob", metres=2.0, period=11.0)
    built.append(("slipper hologram", [ob], None))


# ---------------------------------------------------------------- the inflatable toy builder
def toy_capsule(m, p0, p1, r0, r1, kind, box, sides=12, caps=3):
    """A closed, round-ended lobe from p0 to p1 (an arm, a foot, a knot): u runs along it by true
    length, v round it."""
    bm = m.bm
    p0, p1 = Vector(p0), Vector(p1)
    ax = (p1 - p0).normalized()
    ref = Z if abs(ax.z) < 0.9 else Vector((1, 0, 0))
    u = ax.cross(ref).normalized()
    v = ax.cross(u)
    stations = []
    for k in range(1, caps + 1):
        a = (math.pi / 2) * k / caps
        stations.append((p0 - ax * (r0 * math.cos(a)), r0 * math.sin(a)))
    for k in range(caps, 0, -1):
        a = (math.pi / 2) * k / caps
        stations.append((p1 + ax * (r1 * math.cos(a)), r1 * math.sin(a)))
    pole0, pole1 = bm.verts.new(p0 - ax * r0), bm.verts.new(p1 + ax * r1)
    rings = [[bm.verts.new(c + (u * math.cos(math.tau * i / sides) + v * math.sin(math.tau * i / sides)) * rr) for i in range(sides)] for c, rr in stations]
    edge = [(p0 - ax * r0, 0.0)] + stations + [(p1 + ax * r1, 0.0)]
    run = [0.0]
    for (ca, ra), (cb, rb) in zip(edge, edge[1:]):
        run.append(run[-1] + math.hypot((cb - ca).length, rb - ra))
    u0, v0, u1, v1 = box
    U = lambda i: u0 + (u1 - u0) * run[i] / run[-1]
    V = lambda k: v0 + (v1 - v0) * k / sides
    slot = m.slot(kind)

    def put(verts, uvs):
        f = bm.faces.new(verts)
        f.material_index = slot
        f.smooth = True
        for l, uv in zip(f.loops, uvs):
            l[m.uv].uv = uv

    for j in range(len(rings) - 1):
        for k in range(sides):
            k2 = (k + 1) % sides
            put((rings[j][k], rings[j][k2], rings[j + 1][k2], rings[j + 1][k]),
                ((U(j + 1), V(k)), (U(j + 1), V(k + 1)), (U(j + 2), V(k + 1)), (U(j + 2), V(k))))
    last = len(rings)
    for k in range(sides):
        k2 = (k + 1) % sides
        put((rings[0][k2], rings[0][k], pole0), ((U(1), V(k + 1)), (U(1), V(k)), (U(0), V(k + 0.5))))
        put((rings[-1][k], rings[-1][k2], pole1), ((U(last), V(k)), (U(last), V(k + 1)), (U(last + 1), V(k + 0.5))))


def toy_hose(m, path, radii, kind, box, sides=10, closed=False, u_span=(0.0, 1.0), ref=None):
    """A tube along a path (a strap, a scarf): closed at both ends, or a closed loop. `ref` is a
    direction the tube's sections are squared to, so they do not twist."""
    bm = m.bm
    n = len(path)
    ref = ref or Z
    rings = []
    for i, p in enumerate(path):
        a = path[(i - 1) % n] if (closed or i > 0) else path[0]
        b = path[(i + 1) % n] if (closed or i < n - 1) else path[-1]
        tg = (b - a).normalized()
        r_ = ref if abs(tg.dot(ref)) < 0.95 else (Vector((1, 0, 0)) if abs(tg.x) < 0.9 else Vector((0, 1, 0)))
        u = tg.cross(r_).normalized()
        v = tg.cross(u)
        rings.append([bm.verts.new(p + (u * math.cos(math.tau * k / sides) + v * math.sin(math.tau * k / sides)) * radii[i]) for k in range(sides)])
    u0, v0, u1, v1 = box
    slot = m.slot(kind)
    count = n if closed else n - 1
    for i in range(count):
        i2 = (i + 1) % n
        for k in range(sides):
            k2 = (k + 1) % sides
            f = bm.faces.new((rings[i][k], rings[i][k2], rings[i2][k2], rings[i2][k]))
            f.material_index = slot
            f.smooth = True
            ua = u0 + (u1 - u0) * (u_span[0] + (u_span[1] - u_span[0]) * i / count)
            ub = u0 + (u1 - u0) * (u_span[0] + (u_span[1] - u_span[0]) * (i + 1) / count)
            va, vb = v0 + (v1 - v0) * k / sides, v0 + (v1 - v0) * (k + 1) / sides
            for l, uv in zip(f.loops, ((ua, va), (ua, vb), (ub, vb), (ub, va))):
                l[m.uv].uv = uv
    if not closed:
        for ring, uu in ((rings[0][::-1], u_span[0]), (rings[-1], u_span[1])):
            f = bm.faces.new(ring)
            f.material_index = slot
            for l in f.loops:
                l[m.uv].uv = (u0 + (u1 - u0) * (uu * 0.98 + 0.01), (v0 + v1) / 2)


SKIN = dict(front="balloon", rim="balloon", back="balloon", strap="balloon", limb="balloon", scarf="balloon", uv=True, face=None)
BLOCK = dict(front="blk_yellow", rim="blk_maroon", back="blk_back", strap="blk_red", limb="blk_yellow", scarf="blk_cream", uv=False, face="blk_maroon")


def build_toy(m, heel, X, Y, N, P, skin, down=None, parts=None):
    """AN INFLATED SLIPPER CHARACTER, in the frame (X across, Y heel to toe, N out of the footbed),
    its heel's end at `heel`. P gives the proportions and what it has:
      the SOLE      one closed pillow: the slipper's outline swept from the middle of the back, round
                    the rim, to the middle of the front, fat (Th about half its width) and pinched
                    along its seams into big soft panels;
      the STRAP     a fat toe post with a knot and two arms that end inside the sole;
      a SCARF       a closed ring lying in the pinch under the face, a knot and two tails;
      LIMBS         round-ended lobes that start inside the sole;
      a FACE        painted (the kit's texture); the blockouts carry it as raised lines.
    Every part is a closed solid that starts INSIDE the sole, as an inflatable's lobes are sewn on.

    `parts` (the kit's balloon only): {"limbs": [a Mesh per limb], "tails": [a Mesh per scarf tail]}.
    Each of those lobes is then built into its OWN object, its pivot at the middle of the round end
    that sits inside the body (a limb's base, the scarf's knot), so Unity can swing it about that
    point and the join never opens: a ball turned about its own middle is the same ball."""
    L, W, Th = P["L"], P["W"], P["Th"]
    waist = P.get("waist", 0.8)
    hwf = lambda t: T.half_width(t, waist)
    O = T.outline(64, waist)
    n = len(O)
    cxy = (sum(p[0] for p in O) / n, 0.5)
    down = down or -Z
    bm, uvl = m.bm, m.uv
    isl = lambda name: T.ISLAND[name] if skin["uv"] else (0.0, 0.0, 1.0, 1.0)

    def S(x, t, z):
        return heel + X * (x * W) + Y * (t * L) + N * z

    def quilt(x, t):
        q = 1.0
        for t0, bow in P.get("seams", ()):
            q -= P.get("pinch", 0.24) * math.exp(-((t - (t0 + bow * math.cos(x * math.pi / 0.9))) / 0.038) ** 2)
        return q

    def rho(x, t):
        """How far (x, t) is from the sole's middle toward its outline, 0 to 1."""
        dx, dt = x - cxy[0], t - cxy[1]
        if abs(dx) < 1e-9 and abs(dt) < 1e-9:
            return 0.0

        def inside(k):
            tt, xx = cxy[1] + k * dt, cxy[0] + k * dx
            return 0.0 < tt < 1.0 and abs(xx - T.lean(tt)) < hwf(tt)
        lo, hi = 0.0, 1.0
        while inside(hi) and hi < 256.0:
            lo, hi = hi, hi * 2.0
        for _ in range(40):
            mid = (lo + hi) / 2
            lo, hi = (mid, hi) if inside(mid) else (lo, mid)
        return min(1.0, 1.0 / max(lo, 1e-6))

    def surf(x, t):
        """The front surface's height over the sole's middle plane at (x, t). The back is its mirror."""
        s = min(rho(x, t), 0.9999)
        phi = math.acos(s ** (1.0 / 0.42))
        return (Th / 2) * (math.sin(phi) ** 0.9) * (1.0 + 0.16 * (1.0 - s)) * quilt(x, t)

    # ---- the sole
    # Rings evenly spaced from the rim to the middle (the sweep is far from even in angle: at 80 degrees
    # a ring is still half way out, and the first blockout's middle was one fan of long triangles).
    half = [8.0, 22.0, 30.0, 44.0, 60.0, 73.0, 83.5, 88.8]
    phis = [-a for a in half[::-1]] + half
    band = 22
    rings = []
    for ph in phis:
        a = math.radians(ph)
        s = math.cos(a) ** 0.42
        z = (Th / 2) * (abs(math.sin(a)) ** 0.9) * (1 if ph > 0 else -1) * (1.0 + 0.16 * (1 - s))
        ring = []
        for x, t in O:
            xs, ts = cxy[0] + (x - cxy[0]) * s, cxy[1] + (t - cxy[1]) * s
            ring.append((xs, ts, z * quilt(xs, ts)))
        rings.append(ring)
    verts = [[bm.verts.new(S(*p)) for p in ring] for ring in rings]
    bottom = bm.verts.new(S(cxy[0], cxy[1], -Th / 2 * 1.16 * quilt(*cxy)))
    crown = bm.verts.new(S(cxy[0], cxy[1], Th / 2 * 1.16 * quilt(*cxy)))
    ru0, rv0, ru1, rv1 = isl("rim")

    def planar(x, t, side):
        return T.foot_uv(x, t, side) if skin["uv"] else (0.0, 0.0)

    for j in range(len(phis) - 1):
        rim = abs(phis[j]) <= band and abs(phis[j + 1]) <= band
        part = "rim" if rim else ("front" if phis[j + 1] > band else "back")
        for i in range(n):
            k = (i + 1) % n
            f = bm.faces.new((verts[j][i], verts[j][k], verts[j + 1][k], verts[j + 1][i]))
            f.material_index = m.slot(skin[part])
            f.smooth = True
            if rim:
                fa, fb = (phis[j] + band) / (2.0 * band), (phis[j + 1] + band) / (2.0 * band)
                ua, ub = ru0 + (ru1 - ru0) * (0.04 + 0.92 * fa), ru0 + (ru1 - ru0) * (0.04 + 0.92 * fb)
                va, vb = rv0 + (rv1 - rv0) * i / n, rv0 + (rv1 - rv0) * (i + 1) / n
                uvs = ((ua, va), (ua, vb), (ub, vb), (ub, va))
            else:
                # A strip that leaves the rim band is drawn on the planar island all the way.
                side = "foot" if part == "front" else "tread"
                uvs = tuple(planar(rings[a][b][0], rings[a][b][1], side) for a, b in ((j, i), (j, k), (j + 1, k), (j + 1, i)))
            for l, uv in zip(f.loops, uvs):
                l[uvl].uv = uv
    for ring_i, pole, part, flip in ((0, bottom, "back", True), (len(phis) - 1, crown, "front", False)):
        side = "foot" if part == "front" else "tread"
        for i in range(n):
            k = (i + 1) % n
            f = bm.faces.new((verts[ring_i][k], verts[ring_i][i], pole) if flip else (verts[ring_i][i], verts[ring_i][k], pole))
            f.material_index = m.slot(skin[part])
            f.smooth = True
            pu = planar(cxy[0], cxy[1], side)
            ua, ub = planar(rings[ring_i][i][0], rings[ring_i][i][1], side), planar(rings[ring_i][k][0], rings[ring_i][k][1], side)
            for l, uv in zip(f.loops, (ub, ua, pu) if flip else (ua, ub, pu)):
                l[uvl].uv = uv

    # ---- the strap: a fat post and knot at the toe, two arms out to the sides. Smooth, no bands.
    if P.get("post"):
        pt, at_, r = P["post"], P["anchor"], P["strap_r"]
        px = T.lean(pt)
        h0 = surf(px, pt)
        knot_h = h0 + P["strap_lift"]
        toy_hose(m, [S(px, pt, h0 - r), S(px, pt, h0 + r * 0.4), S(px, pt, knot_h - r * 0.5), S(px, pt, knot_h + r * 0.5), S(px, pt, knot_h + r * 1.1)],
                 [r * 0.80, r * 0.74, r * 1.22, r * 1.16, r * 0.42], skin["strap"], isl("strap"), 12, u_span=(0.0, 0.14), ref=X)
        for sx in (-1, 1):
            ax = T.lean(at_) + sx * hwf(at_) * P["anchor_x"]
            path, radii = [], []
            steps = 18
            for j in range(steps + 1):
                f = j / steps
                x = px + (ax - px) * f + sx * 0.035 * math.sin(math.pi * f)
                t = pt + (at_ - pt) * f
                rad = r * (0.86 + 0.30 * math.sin(math.pi * min(1.0, f * 1.2)) ** 0.7)
                ride = surf(x, t) + rad * 0.80 + P["strap_arch"] * math.sin(math.pi * f) * (1 - 0.45 * f)
                if f < 0.16:
                    k = f / 0.16
                    k = k * k * (3 - 2 * k)
                    ride = knot_h + (ride - knot_h) * k
                elif f > 0.86:
                    k = (f - 0.86) / 0.14
                    ride = ride + (surf(ax, at_) - rad * 1.1 - ride) * k * k
                path.append(S(x, t, ride))
                radii.append(rad)
            toy_hose(m, path, radii, skin["strap"], isl("strap"), 12, u_span=(0.16, 1.0), ref=N)

    # ---- the scarf: a ring lying in the pinch, a knot off to one side, two tails.
    if P.get("scarf"):
        st, sr = P["scarf"], P["scarf_r"]
        ln, hw = T.lean(st), hwf(st)
        front = [ln - hw * 0.97 + 2 * hw * 0.97 * k / 16 for k in range(17)]
        path = [S(ln - hw - sr * 0.55 / W, st, 0.0)]
        path += [S(x, st, surf(x, st) + sr * 0.55) for x in front]
        path += [S(ln + hw + sr * 0.55 / W, st, 0.0)]
        path += [S(x, st, -surf(x, st) - sr * 0.55) for x in front[::-1]]
        toy_hose(m, path, [sr] * len(path), skin["scarf"], isl("scarf"), 10, closed=True, ref=Y)
        kx = ln + hw * 0.52
        knot = S(kx, st, surf(kx, st) + sr * 1.5)
        toy_capsule(m, knot - X * (sr * 0.5), knot + X * (sr * 0.5), sr * 1.25, sr * 1.25, skin["scarf"], isl("scarf"), 10, 3)
        for k, (lean_, length) in enumerate(((0.10, sr * 4.6), (0.62, sr * 3.6))):
            d = (down + X * lean_ + N * 0.25).normalized()
            tail = [knot + d * (length * j / 5) + N * (sr * 0.5 * math.sin(math.pi * j / 5)) for j in range(6)]
            tm = m
            if parts:
                tm = parts["tails"][k]
                tm.origin = knot.copy()
            toy_hose(tm, tail, [sr * (0.92 - 0.06 * j) for j in range(6)], skin["scarf"], isl("scarf"), 8, u_span=(0.1 + 0.4 * k, 0.5 + 0.4 * k), ref=X)

    # ---- the limbs: (x in half widths, t, which face (+1 front, -1 back, 0 the edge), its direction in (X, Y, N), length, radii)
    for k, (hx, t, face, vec, length, r0, r1) in enumerate(P.get("limbs", ())):
        x = T.lean(t) + hx * hwf(t)
        base = S(x, t, face * surf(x, t) * 0.25)
        d = (X * vec[0] + Y * vec[1] + N * vec[2]).normalized()
        lm = m
        if parts:
            lm = parts["limbs"][k]
            lm.origin = base.copy()
        toy_capsule(lm, base, base + d * length, r0, r1, skin["limb"], isl("limb"), 18, 5)

    # ---- the face as raised lines: the blockouts only (the kit's is painted)
    if skin["face"] and P.get("face"):
        Fc = P["face"]
        asp = L / W
        box = (0.0, 0.0, 1.0, 1.0)
        lr = Fc["eye_r"] * W * 0.16

        def raised(pts):
            toy_hose(m, [S(x, t, surf(x, t) + lr * 0.5) for x, t in pts], [lr] * len(pts), skin["face"], box, 6, ref=N)

        for sx in (-1, 1):
            cx = T.lean(Fc["eye_t"]) + sx * Fc["eye_x"]
            raised([(cx + Fc["eye_r"] * math.cos(a), Fc["eye_t"] + Fc["eye_r"] * math.sin(a) / asp) for a in (math.radians(-10 + 200 * k / 10) for k in range(11))])
            bx_ = T.lean(Fc["eye_t"]) + sx * Fc["eye_x"] * 1.55
            bt = Fc["eye_t"] - Fc["eye_r"] * 1.0 / asp
            c = S(bx_, bt, surf(bx_, bt) - lr * 1.2)
            toy_capsule(m, c - X * (lr * 1.4), c + X * (lr * 1.4), lr * 2.4, lr * 2.4, "blk_pink", box, 8, 2)
        mr = Fc["mouth_r"]
        raised([(T.lean(Fc["mouth_t"]) + mr * math.cos(a), Fc["mouth_t"] + mr * 0.55 / asp + mr * math.sin(a) / asp) for a in (math.radians(205 + 130 * k / 8) for k in range(9))])
    return dict(S=S, surf=surf, hwf=hwf, cxy=cxy)


# THE BALLOON'S PROPORTIONS (design A, the one built: see `balloon`).
TOY_A = dict(L=T.BALLOON["length"], W=T.BALLOON["width"], Th=T.BALLOON["thick"], waist=T.BALLOON["waist"], seams=T.SEAMS, pinch=0.24,
             post=T.BAL["post"], anchor=T.BAL["anchor"], anchor_x=T.BAL["anchor_x"], strap_r=3.5, strap_lift=6.6, strap_arch=2.6,
             scarf=T.BAL["scarf_t"], scarf_r=2.5,
             limbs=((-0.93, 0.43, 0, (-0.78, -0.62, 0.10), 13.0, 5.6, 4.9),      # its right arm, down at its side
                    (0.93, 0.45, 0, (0.74, 0.64, 0.20), 14.0, 5.6, 4.9),         # its left arm, up: a wave
                    (-0.66, 0.075, 1, (-0.30, -0.62, 0.72), 11.5, 5.4, 5.0),     # two short legs, apart, dangling toward the stage
                    (0.66, 0.075, 1, (0.30, -0.62, 0.72), 11.5, 5.4, 5.0)),
             face=dict(eye_t=T.BAL["eye_t"], eye_x=T.BAL["eye_x"], eye_r=T.BAL["eye_r"], mouth_t=T.BAL["mouth_t"], mouth_r=T.BAL["mouth_r"]))
TOY_B = dict(L=70.0, W=30.0, Th=15.0, waist=0.82, seams=((0.36, 0.02), (0.68, -0.02)), pinch=0.22,
             post=0.74, anchor=0.48, anchor_x=0.80, strap_r=2.8, strap_lift=5.6, strap_arch=2.4,
             limbs=((-0.55, 0.22, -1, (-0.2, 0.0, -1.0), 9.0, 4.6, 4.2), (0.55, 0.22, -1, (0.2, 0.0, -1.0), 9.0, 4.6, 4.2),
                    (-0.55, 0.74, -1, (-0.2, 0.0, -1.0), 9.0, 4.6, 4.2), (0.55, 0.74, -1, (0.2, 0.0, -1.0), 9.0, 4.6, 4.2)),
             face=dict(eye_t=0.905, eye_x=0.13, eye_r=0.050, mouth_t=0.868, mouth_r=0.036))
TOY_C = dict(L=52.0, W=25.0, Th=12.0, waist=0.84, seams=((0.60, 0.02),), pinch=0.2,
             post=0.84, anchor=0.58, anchor_x=0.80, strap_r=2.4, strap_lift=4.8, strap_arch=2.0,
             face=dict(eye_t=0.38, eye_x=0.20, eye_r=0.07, mouth_t=0.32, mouth_r=0.05))


def balloon(coll, built, roof_z):
    """THE SLIPPER BALLOON, second design. The owner, of the first: "the balloon slipper design looks
    so off and unsettling". Looking at Logs/arena/holo/holo_close_balloon_front_v4.png, what was
    unsettling:
      * THE EYES. Whites, pupils and highlights, wide open, in the dead middle of a tall flat body:
        a mask that stares. With raised brows and an open mouth with a tongue it reads as alarm.
      * THE STRAP reads as two red arms or claws reaching down over the face from behind, and its
        painted bands make it a segmented limb. Nothing says "strap".
      * THE BODY is a thin tall slab (14 m thick on 74 m): a tongue or a shield, not an inflatable.
        It has a face but no body: no limbs, no posture, nothing the face belongs to.
      * A STITCHED SEAM runs through the cheeks, so the face looks sewn on.
      * The colours were invented (a coral strap), not the game's.
    The reference is the Overwatch balloon cow: a chunky, round, friendly CHARACTER with stubby limbs
    and a scarf. Three designs were blocked and compared (`options`, the sheet
    Logs/arena/holo/balloon_options_v5.png): A a seated mascot with arms, feet and a scarf; B a level
    parade balloon with a small face on its toe; C a pair tied together. A is built, because it is
    the only one of the three that is a character from the stage: B shows the stage its underside,
    and C is two thin soles again.

    A, AS BUILT: the sole is the body, 60 m tall, 36 m wide and 22 m thick, pinched under the face
    (the scarf lies in the pinch) and at the strap's line into three fat panels. It SITS in the air
    over the south canopy as a tethered balloon does, leaning 12 degrees toward the stage, its two
    short legs dangling apart toward the players, one arm down and one up in a wave. The strap is a fat
    smooth V high on its head, well clear of the face, like a cap worn back. The face is small, low
    and wide apart: two closed happy arcs, a small smile, two cheeks (the texture). A scarf with a
    knot and two tails, as the cow has. The back carries the game's stamp.
    It is built as rigid parts (below) that Unity moves; nothing of it is in the motion file."""
    at = BALLOON_AT
    F = Frame(at["bearing"], at["r"])
    zc = roof_z(F.o.x, F.o.y)
    pivot = Vector((F.o.x, F.o.y, zc + 2.2))                       # the winch's fairlead
    Fb = Frame(at["body_bearing"], at["body_r"])                   # the body floats over the masts' heads (z 91), a little inboard
    yaw = Matrix.Rotation(math.radians(at["yaw"]), 3, "Z")
    X = yaw @ Fb.dx
    N0 = yaw @ (-Fb.dy)                                            # the face looks at the can
    th = math.radians(at["lean"])
    Y = Z * math.cos(th) + N0 * math.sin(th)                       # heel to toe: up, and forward over the stage
    N = N0 * math.cos(th) - Z * math.sin(th)
    heel = Vector((Fb.o.x, Fb.o.y, zc + at["lift"]))
    P = TOY_A

    # IT IS ANIMATED IN UNITY (Runtime/Map/ArenaBalloon.cs; the owner: "the balloon should be animated
    # btw"), so it is SEVERAL RIGID OBJECTS, each with its pivot where it turns:
    #   balloon_body         the body (sole, strap, scarf ring and knot, valve, the rings' stubs). Its
    #                        pivot is the middle of the sole and ITS AXES ARE THE SLIPPER'S (x across,
    #                        z heel to toe, so Unity's y), so Unity squashes and stretches it along
    #                        its own length;
    #   balloon_arm_wave, balloon_arm_rest, balloon_leg_l, balloon_leg_r
    #                        the four lobes, pivot at the middle of the round end inside the body;
    #   balloon_tail_a, _b   the scarf's tails, pivot at the knot's middle;
    #   balloon_rope_0..4    the tether and the four guy ropes, pivot at the anchor on the canopy and
    #                        z (Unity's y) ALONG the rope: Unity aims each at its ring on the body and
    #                        stretches it to reach, so a rope is always fast at both ends;
    #   balloon_scrap        what is left of it when it has been popped: a heap by the winch (hidden
    #                        until then);
    #   balloon_mooring      the winch, the rings, the up-lights. Static.
    # A lobe turned about the middle of its own round end leaves that end where it was, so no join
    # can open however the parts move.
    centre = heel + X * (T.lean(0.5) * P["W"]) + Y * (0.5 * P["L"])
    axes = Matrix((X, -N, Y)).transposed()                         # columns: x across, y back, z heel to toe
    m = Mesh("balloon_body", centre, basis=axes)
    limb_names = ("arm_rest", "arm_wave", "leg_l", "leg_r")        # in TOY_A's order
    parts = dict(limbs=[Mesh("balloon_" + n, centre) for n in limb_names], tails=[Mesh("balloon_tail_" + n, centre) for n in "ab"])
    toy = build_toy(m, heel, X, Y, N, P, SKIN, parts=parts)
    S, surf, hwf = toy["S"], toy["surf"], toy["hwf"]

    # The valve, low on the back: steel and a red cap, standing in the skin.
    vx, vt = T.lean(0.12) + hwf(0.12) * 0.42, 0.12
    vp = S(vx, vt, -surf(vx, vt))
    m.tube(vp + N * 1.6, vp - N * 1.5, 0.9, 8, 0.7, kind="led.dark", cap="led.rope")
    m.tube(vp - N * 1.4, vp - N * 2.1, 1.2, 8, kind="led.red", cap="led.red")

    anchors = [("winch", pivot)]
    for k, (dx_, dy_) in enumerate(((-7.0, -5.5), (7.0, -5.5), (-7.5, 6.5), (7.5, 6.5))):
        q = F.p(dx_, dy_, 0.0)
        anchors.append(("ring%d" % k, Vector((q.x, q.y, roof_z(q.x, q.y) + 1.1))))
    ties = [(0.0, 0.035), (-0.55, 0.11), (0.55, 0.11), (-0.62, 0.30), (0.62, 0.30)]   # on the back: (x in half widths, t)
    ropes, tie_points = [], []
    for k, ((name, a), (hx, tt)) in enumerate(zip(anchors, ties)):
        x = T.lean(tt) + hx * hwf(tt)
        p = S(x, tt, -surf(x, tt))
        m.tube(p + N * 1.3, p - N * 1.0, 0.6, 6, kind="metal")                       # the D-ring's stub, in the skin
        along = (p - a).normalized()
        side = along.cross(Z).normalized()
        rope = Mesh("balloon_rope_%d" % k, a, basis=Matrix((side, along.cross(side), along)).transposed())
        # From half a metre inside the anchor to the middle of the ring's stub: its length is |p - a|.
        rope.tube(a - along * 0.5, p, 0.28 if name == "winch" else 0.18, 5, kind="led.rope")
        ropes.append(rope.done(coll))
        tie_points.append(p.copy())
    ob = m.done(coll)
    made = [ob]
    for part in parts["limbs"] + parts["tails"]:
        made.append(part.done(coll))
    for o in made:
        for poly in o.data.polygons:                              # the skin is smooth; the hardware is flat
            poly.use_smooth = o.data.materials[poly.material_index].name == PRE + "balloon"
    built.append(("slipper balloon", made + ropes, "the slipper, as a balloon"))

    # THE MOORING, on the canopy: a winch with its drum, four anchor rings, six up-lights. Static.
    g = Mesh("balloon_mooring", pivot)
    g.box(Vector((pivot.x, pivot.y, zc + 1.0)), (5.2, 3.6, 3.2), F.dx, kinds={"-y": "led.gold"})   # sunk 0.6 m into the roof
    g.tube(pivot - F.dx * 2.9 + Z * 0.2, pivot + F.dx * 2.9 + Z * 0.2, 1.3, 10, kind="metal", cap="led.red")
    for name, a in anchors[1:]:
        g.box(a - Z * 1.0, (2.0, 2.0, 1.6), F.dx)                 # the block, 0.5 m into the roof
        g.tube(a - Z * 0.5, a + Z * 0.7, 0.55, 8, kind="metal", cap="led.red")
    for k in range(6):                                            # up-lights round the winch, aimed at the balloon
        a = math.tau * k / 6 + 0.26
        c = pivot + F.dx * (math.cos(a) * 12.5) + F.dy * (math.sin(a) * 7.5)
        c.z = roof_z(c.x, c.y)
        aim = (heel + Y * (P["L"] * 0.45) - c).normalized()
        g.box(c + Z * 0.3, (1.8, 1.8, 1.4), F.dx)
        g.tube(c + Z * 0.6, c + Z * 0.6 + aim * 2.6, 0.8, 8, 1.15, kind="metal", cap="led.white")
    mo = g.done(coll)
    built.append(("slipper balloon", [mo], None))

    # WHAT IS LEFT WHEN IT HAS BEEN POPPED: a slumped heap of its skin beside the winch, toward the
    # stage, cream-tipped lobes flopped across each other. Four flat closed lobes sunk into each
    # other and into the roof. Unity hides it until the pop.
    sc = F.p(0.0, -9.5, 0.0)
    sc.z = roof_z(sc.x, sc.y)
    scrap = Mesh("balloon_scrap", Vector((sc.x, sc.y, sc.z)))
    flat = Matrix.Translation(scrap.origin) @ Matrix.Diagonal((1.0, 1.0, 0.36, 1.0)) @ Matrix.Translation(-scrap.origin)
    for dx_, dy_, length, r0, r1, turn in ((-3.4, 0.4, 7.0, 3.6, 3.0, 12.0), (2.6, -1.0, 6.0, 3.2, 2.6, 168.0), (0.2, 2.2, 5.0, 2.8, 2.4, 96.0), (-0.6, -2.4, 6.4, 2.2, 1.9, 40.0)):
        b0 = scrap.origin + F.dx * dx_ + F.dy * dy_ + Z * (r0 * 0.9)
        d = F.dx * math.cos(math.radians(turn)) + F.dy * math.sin(math.radians(turn))
        before = len(scrap.bm.verts)
        toy_capsule(scrap, b0, b0 + d * length + Z * 0.6, r0, r1, "balloon", T.ISLAND["limb"], 12, 3)
        scrap.bm.verts.ensure_lookup_table()
        bmesh.ops.transform(scrap.bm, matrix=flat, verts=scrap.bm.verts[before:])
    so = scrap.done(coll, weld=False)
    for poly in so.data.polygons:
        poly.use_smooth = True
    built.append(("slipper balloon scrap", [so], None))
    BALLOON_OBJECTS[:] = made + [mo]
    BALLOON_RIG.clear()
    BALLOON_RIG.update(body=ob, limbs=dict(zip(limb_names, made[1:5])), tails=made[5:7], ropes=ropes, scrap=so, mooring=mo,
                       ties=tie_points, pivot=pivot.copy(), axes=(X.copy(), Y.copy(), N.copy()))
    return heel, Y, N, X


def options(version):
    """THE THREE DESIGNS, blocked in flat colours and rendered side by side for the owner
    (Logs/arena/holo/balloon_options_<front|quarter>_<version>.png; the texture author's
    --options-sheet joins them). Not the kit: nothing here is saved."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    coll = K.collection("balloon options (not the kit)")
    X, N0 = Vector((1, 0, 0)), Vector((0, -1, 0))
    up = lambda deg: (Z * math.cos(math.radians(deg)) + N0 * math.sin(math.radians(deg)), N0 * math.cos(math.radians(deg)) - Z * math.sin(math.radians(deg)))

    a = Mesh("option_a", (0, 0, 0))
    Y, N = up(12.0)
    build_toy(a, Vector((-84.0, 0, 0)), X, Y, N, TOY_A, BLOCK)
    a.done(coll)

    b = Mesh("option_b", (0, 0, 0))                               # level, its toe toward the stage and dipped 26 degrees
    dip = math.radians(26.0)
    Yb = N0 * math.cos(dip) - Z * math.sin(dip)
    Nb = Z * math.cos(dip) + N0 * math.sin(dip)
    build_toy(b, Vector((0.0, 30.0, 44.0)), X, Yb, Nb, TOY_B, BLOCK)
    b.done(coll)

    c = Mesh("option_c", (0, 0, 0))                               # two, leaning on each other, their straps tied with a rope
    Y, N = up(10.0)
    one = build_toy(c, Vector((70.0, 0, 6.0)), X, Y, N, TOY_C, BLOCK)
    lean2 = Matrix.Rotation(math.radians(-24.0), 3, N0)
    Pc = dict(TOY_C)
    Pc.pop("face")
    two = build_toy(c, Vector((104.0, 9.0, 2.0)), lean2 @ X, lean2 @ Y, lean2 @ N, Pc, BLOCK)
    k1 = one["S"](T.lean(0.84), 0.84, one["surf"](T.lean(0.84), 0.84) + 6.0)
    k2 = two["S"](T.lean(0.84), 0.84, two["surf"](T.lean(0.84), 0.84) + 6.0)
    mid = (k1 + k2) / 2 - Z * 7.0
    toy_hose(c, [k1, k1.lerp(mid, 0.5) - Z * 2.0, mid, k2.lerp(mid, 0.5) - Z * 2.0, k2], [0.8] * 5, "blk_cream", (0, 0, 1, 1), 6, ref=N0)
    c.done(coll)

    sun = bpy.data.objects.new("key", bpy.data.lights.new("key", "SUN"))
    sun.data.energy = 3.2; sun.data.color = (0.9, 0.94, 1.0); sun.data.angle = math.radians(25)
    sun.rotation_euler = (math.radians(58), math.radians(-12), math.radians(-22))
    coll.objects.link(sun)
    world = bpy.data.worlds.new("night"); scene.world = world; world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.02, 0.026, 0.075, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.view_settings.view_transform = "Standard"
    os.makedirs(LOGS, exist_ok=True)
    for name, loc, target, lens in (("front", (6.0, -330.0, -120.0), (6.0, 0.0, 30.0), 50.0), ("quarter", (250.0, -270.0, 60.0), (10.0, 0.0, 28.0), 50.0)):
        cam_data = bpy.data.cameras.new(name)
        cam = bpy.data.objects.new(name, cam_data)
        scene.collection.objects.link(cam)
        cam.location = loc
        cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        cam_data.lens = lens; cam_data.clip_end = 5000
        scene.camera = cam
        scene.render.resolution_x, scene.render.resolution_y = 2400, 900
        scene.render.filepath = os.path.join(LOGS, "balloon_options_%s_%s.png" % (name, version))
        bpy.ops.render.render(write_still=True)
    print("OPTIONS_OK %s" % version)


def pcx_far(coll, built, ray):
    """PC EXPRESS standing on tower T22's roof, 760 m out: a bar of projectors on a gantry whose four
    legs end inside the roof, and the mark above it."""
    f = PCX_FAR
    F0 = Frame(f["bearing"], f["r"])
    roof = ray(F0.o.x, F0.o.y - 0.0, 2000.0)
    if roof is None:
        roof = 445.0
    # The roof under the gantry's feet: the top drum, 16 m toward the can from the axis.
    feet = []
    for lx, ly in ((-14.0, -24.0), (14.0, -24.0), (-8.0, -27.0), (8.0, -27.0)):     # on the ledge between the dome and the cornice
        q = F0.p(lx, ly, 0)
        z = ray(q.x, q.y, 2000.0)
        feet.append((q, z if z is not None else roof))
    deck = max(z for _, z in feet) + 46.0                          # clear of the dome; the mast runs on up behind
    w = f["width"]
    h = w / T.PCX_ASPECT
    F = Frame(f["bearing"], f["r"] - 8.0, deck + 16.0 + h / 2)
    m = Mesh("sign_pcx_far", F.o)
    sheet(m, F, w, h, "pcx", "logo", y=0.0, segs=8)
    sheet(m, F, w, h, "pcx", "logo", y=3.6, back=True, segs=8)
    dz = T.atlas_uv(T.FX["dots"])
    for i in range(8):
        for j in range(2):
            xa, xb = -w * 0.56 + w * 1.12 * i / 8, -w * 0.56 + w * 1.12 * (i + 1) / 8
            za, zb = -h * 0.62 + h * 1.24 * j / 2, -h * 0.62 + h * 1.24 * (j + 1) / 2
            m.quad([F.p(xa, 1.8, za), F.p(xb, 1.8, za), F.p(xb, 1.8, zb), F.p(xa, 1.8, zb)], "fx", dz)
    brackets(m, F, w * 1.08, h * 1.3, -3.0)
    brackets(m, F, w * 1.08, h * 1.3, 6.6)
    ob = move(m.done(coll, weld=False), "pulse", low=0.88, high=1.0, period=4.1)
    built.append(("pcx_far", [ob], "logo"))
    rig = Mesh("sign_pcx_far_rig", F.o)
    bar = Vector((F.o.x, F.o.y, deck))
    rig.box(bar + F.dy * 1.8, (w * 0.62, 3.0, 2.4), F.dx, kinds={"+z": "led.white", "-y": "led.cyan"})
    for q, z in feet:                                             # four legs from 1.2 m inside the roof up into the bar
        foot = Vector((q.x, q.y, z - 1.2))
        head = bar + F.dy * 1.8 + F.dx * ((q - F0.o).dot(F0.dx) * 1.6)
        rig.tube(foot, head, 0.9, 6, 0.7)
    fan = T.atlas_uv(T.FX["fan"])
    s = F.dx * (w * 0.5)
    rig.quad([bar + F.dy * 1.8 - s * 0.6 + Z * 1.4, bar + F.dy * 1.8 + s * 0.6 + Z * 1.4, F.p(0, 1.8, -h / 2 + 1.0) + s, F.p(0, 1.8, -h / 2 + 1.0) - s], "fx", fan)
    built.append(("pcx_far", [rig.done(coll)], None))


# ---------------------------------------------------------------- the other kits: for rays and for the pictures
def append_kits(names=("bowl", "roof", "hull", "city")):
    c = bpy.data.collections.new("review (the other kits, not this one)")
    bpy.context.scene.collection.children.link(c)
    got = {}
    for kit in names:
        path = os.path.join(KITS, kit + ".blend")
        if not os.path.exists(path):
            print("REVIEW no %s.blend" % kit)
            continue
        with bpy.data.libraries.load(path, link=False) as (src, dst):
            dst.collections = [n for n in ("arena_" + kit,) if n in src.collections]
        for col in dst.collections:
            if col is not None:
                c.children.link(col)
                got[kit] = col
    return c, got


def bvh_of(cols, skip=("_fx", "city_haze", "city_floor", "craft", "sky")):
    verts, polys = [], []
    for col in cols:
        for o in list(col.all_objects):
            if o.type != "MESH" or any(s in o.name for s in skip):
                continue
            M = o.matrix_basis
            base = len(verts)
            verts += [M @ v.co for v in o.data.vertices]
            polys += [tuple(base + i for i in p.vertices) for p in o.data.polygons]
    return BVHTree.FromPolygons(verts, polys, all_triangles=False)


def stadium_rim(b):
    """arena_kit.rim_elevation plus the stair tower closing each end of an upper stand (the city kit's)."""
    e = K.rim_elevation(b)
    for lo, hi in K.UPPER_ARCS:
        for a in (lo - 1.2, hi + 1.2):
            if abs(((b - a + 180.0) % 360.0) - 180.0) <= math.degrees(math.atan2(4.2, 160.0)):
                e = max(e, math.degrees(math.atan2(60.0 - EYE, 160.0 - 4.2)))
    return e


def sight(built, tree):
    """From the player's eye by the can. Per thing: where it stands, the elevations it spans, how
    many degrees of it stand over the stadium's rim there, and, by casting a ray at every vertex of
    its light or its skin against the other kits, how much of it is really in view."""
    eye = Vector((0, 0, EYE))
    groups = {}
    order = []
    for name, obs, label in built:
        if name not in groups:
            groups[name] = dict(obs=[], label=label)
            order.append(name)
        groups[name]["obs"] += obs
        groups[name]["label"] = groups[name]["label"] or label
    print("SIGHT from the can (eye z %.1f). The stadium hides the sky up to %.1f degrees over a canopy, %.1f behind a corner screen." % (EYE, K.rim_elevation(0), K.rim_elevation(45)))
    print("SIGHT %-18s %-28s %7s %6s %6s %13s %7s %7s %8s" % ("thing", "what", "bearing", "dist", "height", "elevation", "width", "shows", "in view"))
    rows = []
    for name in order:
        g = groups[name]
        pts = []
        for ob in g["obs"]:
            if ob.name.endswith(("_rig", "_mooring", "_cubes", "_scrap")):
                continue
            M = ob.matrix_basis
            keep = [i for i, mt in enumerate(ob.data.materials) if mt.name.split(PRE)[1] in ("ads", "logo", "fx", "balloon")]
            used = set()
            for p in ob.data.polygons:
                if p.material_index in keep:
                    used.update(p.vertices)
            pts += [M @ ob.data.vertices[i].co for i in used]
        if not pts:
            continue
        c = sum(pts, Vector()) / len(pts)
        bear = math.degrees(math.atan2(c.x, c.y)) % 360.0
        els, rel, seen, shows = [], [], 0, 0.0
        step = max(1, len(pts) // 600)
        sample = pts[::step]
        for p in sample:
            d = p - eye
            el = math.degrees(math.atan2(d.z, math.hypot(d.x, d.y)))
            b = math.degrees(math.atan2(d.x, d.y)) % 360.0
            els.append(el)
            rel.append(((b - bear + 180.0) % 360.0) - 180.0)
            shows = max(shows, el - stadium_rim(b))
            hit = tree.ray_cast(eye, d.normalized(), d.length - 0.5) if tree is not None else (None,)
            seen += hit[0] is None
        share = 100.0 * seen / len(sample)
        low = max(min(els), min(stadium_rim(bear + r) for r in (min(rel), 0.0, max(rel))))
        rows.append((name, g["label"], bear, math.hypot(c.x, c.y), c.z, min(els), max(els), max(rel) - min(rel), max(els) - low, share))
        print("SIGHT %-18s %-28s %7.1f %6.0f %6.0f %5.1f to %4.1f %7.1f %7.1f %7.0f%%%s"
              % (name, g["label"], bear, math.hypot(c.x, c.y), c.z, min(els), max(els), max(rel) - min(rel), max(els) - low, share,
                 "   <-- MOSTLY HIDDEN" if share < 45.0 else ""))
    return rows


def report(coll):
    """Open edges and triangles. A sheet of light is open by design; a solid must be closed."""
    total, bad = 0, 0
    for ob in sorted(coll.objects, key=lambda o: o.name):
        if ob.type != "MESH":
            continue
        bm = bmesh.new(); bm.from_mesh(ob.data)
        tris = sum(len(f.verts) - 2 for f in bm.faces)
        solid = {i for i, mt in enumerate(ob.data.materials) if mt.name.split(PRE)[1] in ("metal", "led", "balloon")}
        op = sum(1 for e in bm.edges if len(e.link_faces) == 1 and any(f.material_index in solid for f in e.link_faces))
        multi = sum(1 for e in bm.edges if len(e.link_faces) > 2)
        light = sum(1 for e in bm.edges if len(e.link_faces) == 1) - op
        bm.free()
        total += tris
        note = ""
        if op or multi:
            note = "   <-- OPEN %d ON A SOLID, SHARED BY MORE THAN TWO FACES %d" % (op, multi)
            bad += 1
        elif light:
            note = "   (sheets of light: open by design)"
        print("MESH %-38s tris %6d  materials %s%s" % (ob.name, tris, ",".join(mt.name.split(PRE)[1] for mt in ob.data.materials), note))
    print("TOTAL TRIANGLES %d in %d objects, %d materials; %d meshes with a solid that is not closed" % (total, len(coll.objects), len(_mats), bad))
    return total


def shoot(name, version, loc, target, lens=35.0, size=(1920, 1080)):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new(name)
    cam = bpy.data.objects.new("cam " + name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = lens; cam_data.clip_start = 0.5; cam_data.clip_end = 20000
    scene.camera = cam
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.render.filepath = os.path.join(LOGS, "holo_%s_%s.png" % (name, version))
    bpy.ops.render.render(write_still=True)


# THE BALLOON'S POSES, as Runtime/Map/ArenaBalloon.cs makes them: for the review pictures only (the
# kit is already saved, with every part at rest). The same order of moves as the component: the
# whole balloon leans and rides about the winch, the body squashes along its own length and swells,
# each lobe turns about its own pivot in the body's frame, each rope is aimed at its ring.
POSES = (   # name, lean toward the stage, lean across (degrees), ride (m), squash, swell, wave, rest arm, legs, tails (degrees), face, popped
    ("rest", 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, "", False),
    ("squash", -1.5, 0.0, -1.6, -0.15, 1.0, 10.0, 12.0, -10.0, 14.0, "ouch", False),
    ("stretch_wave", 1.5, 3.0, 1.6, 0.13, 1.0, 26.0, -8.0, 12.0, -18.0, "", False),
    ("hit_back", -9.0, -4.0, 0.8, 0.08, 1.04, -24.0, 22.0, 20.0, 32.0, "ouch", False),
    ("strained", 2.0, 2.0, 0.0, 0.03, 1.15, 8.0, 6.0, -6.0, 8.0, "worried", False),
    ("dizzy_swing", 8.0, 6.0, 0.0, -0.06, 1.12, 20.0, -18.0, -16.0, -30.0, "dizzy", False),
    ("popped", 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, "", True),
)


def pose_balloon(pose, rest):
    name, lean_in, lean_side, ride, squash, swell, wave, arm, legs, tails, face, popped = pose
    R = BALLOON_RIG
    X, Y, N = R["axes"]
    pivot = R["pivot"]
    body = R["body"]
    for o, mw in rest.items():
        o.matrix_basis = mw.copy()
        o.hide_render = False
    R["scrap"].hide_render = not popped
    for m_ in bpy.data.materials:
        if m_.name == PRE + "balloon":
            for node in m_.node_tree.nodes:
                if node.type == "TEX_IMAGE":
                    emit = node.image.filepath.replace("\\", "/").endswith("_emit.png")
                    node.image = bpy.data.images.load(os.path.join(TEX, "arena_holo_balloon%s%s.png" % ("_" + face if face else "", "_emit" if emit else "")), check_existing=True)
    if popped:
        for o in [body] + list(R["limbs"].values()) + R["tails"]:
            o.hide_render = True
        for k, rope in enumerate(R["ropes"]):                     # reeled in: a short slack length lying on the canopy
            a = rest[rope].translation
            out = (R["ties"][k] - a); out.z = 0.0
            d = (out.normalized() + Vector((0, 0, 0.04))).normalized()
            length = (R["ties"][k] - a).length
            side = d.cross(Z).normalized()
            rope.matrix_basis = Matrix.Translation(a) @ Matrix((side, d.cross(side), d)).transposed().to_4x4() @ Matrix.Diagonal((1.0, 1.0, 0.28, 1.0))
        return
    up = (rest[body].translation - pivot).normalized()
    root = (Matrix.Translation(pivot + up * ride) @ Matrix.Rotation(math.radians(lean_in), 4, X) @ Matrix.Rotation(math.radians(lean_side), 4, N)
            @ Matrix.Translation(-pivot))
    B = rest[body]
    scale = Matrix.Diagonal((swell * (1.0 - squash * 0.5), swell * (1.0 - squash * 0.5), swell * (1.0 + squash), 1.0))
    body.matrix_basis = root @ B @ scale
    delta = root @ B @ scale @ B.inverted()                       # what the body's move does to anything that rides on it
    turns = {"arm_wave": (N, wave), "arm_rest": (N, arm), "leg_l": (X, legs), "leg_r": (X, -legs * 0.6)}
    for key, ob in R["limbs"].items():
        axis, degrees = turns[key]
        pv = rest[ob].translation
        ob.matrix_basis = delta @ Matrix.Translation(pv) @ Matrix.Rotation(math.radians(degrees), 4, axis) @ Matrix.Translation(-pv) @ rest[ob]
    for k, ob in enumerate(R["tails"]):
        pv = rest[ob].translation
        ob.matrix_basis = (delta @ Matrix.Translation(pv) @ Matrix.Rotation(math.radians(tails * (1.0 if k == 0 else 0.7)), 4, N)
                           @ Matrix.Rotation(math.radians(tails * 0.4), 4, X) @ Matrix.Translation(-pv) @ rest[ob])
    for k, rope in enumerate(R["ropes"]):
        a = rest[rope].translation
        tie = delta @ R["ties"][k]
        d = (tie - a).normalized()
        side = d.cross(Z).normalized()
        rope.matrix_basis = (Matrix.Translation(a) @ Matrix((side, d.cross(side), d)).transposed().to_4x4()
                             @ Matrix.Diagonal((1.0, 1.0, (tie - a).length / (R["ties"][k] - a).length, 1.0)))


def posed_pictures(version, info):
    """The animation's extremes, from the stage (a long lens from the can's eye, and the game's wide
    lens from behind a player) and close: Logs/arena/holo/holo_pose_<pose>_<view>_vN.png."""
    R = BALLOON_RIG
    heel, Y, N, X = info["balloon"]
    mid = heel + Y * 30.0
    everything = [R["body"], R["scrap"]] + list(R["limbs"].values()) + R["tails"] + R["ropes"]
    rest = {o: o.matrix_basis.copy() for o in everything}
    for pose in POSES:
        pose_balloon(pose, rest)
        bpy.context.view_layer.update()
        shoot("pose_%s_stage" % pose[0], version, (0, 0, EYE), mid - Z * 12.0, lens=85, size=(1280, 720))
        shoot("pose_%s_close" % pose[0], version, mid + N * 150.0 + X * 46.0 - Z * 30.0, mid - Z * 16.0, lens=32, size=(1280, 720))
    pose_balloon(POSES[1], rest)
    shoot("pose_squash_game", version, K.polar(9.0, BALLOON_AT["body_bearing"] + 180.0, EYE + 1.6), K.polar(100.0, BALLOON_AT["body_bearing"], EYE + 44.0), lens=18)
    pose_balloon(POSES[3], rest)
    shoot("pose_hit_back_side", version, mid + X * 150.0 + N * 40.0 - Z * 10.0, mid - Z * 16.0, lens=32, size=(1280, 720))
    shoot("pose_hit_back_back", version, mid - N * 130.0 + Z * 30.0 - X * 40.0, mid - Z * 14.0, lens=30, size=(1280, 720))
    pose_balloon(POSES[0], rest)
    R["scrap"].hide_render = True


def pictures(version, shots, info):
    want = lambda k: (not shots) or k in shots
    heel, Y, N, X = info["balloon"]
    mid = heel + Y * 30.0
    if want("eye"):
        for b in (0, 45, 90, 135, 180, 225, 270, 315):
            shoot("eye_%03d" % b, version, (0, 0, EYE), K.polar(100.0, b, EYE + 44.0), lens=14)
    if want("can"):                                               # from the can's eye, long: is an ad read, is the balloon friendly
        shoot("can_column", version, (0, 0, EYE), K.polar(100.0, COLUMNS[0][1], EYE + 62.0), lens=30)
        shoot("can_balloon", version, (0, 0, EYE), K.polar(100.0, BALLOON_AT["body_bearing"], EYE + 70.0), lens=30)
    if want("game"):                                              # a narrower look from behind a player: nearer the game's camera
        for b in (200, 90, 20, 290):
            shoot("game_%03d" % b, version, K.polar(9.0, b + 180.0, EYE + 1.6), K.polar(100.0, b, EYE + 34.0), lens=18)
    if want("air"):
        shoot("air_south", version, K.polar(1150.0, 196.0, 520.0), (0, 0, 120.0), lens=28)
        shoot("air_east", version, K.polar(1000.0, 96.0, 380.0), (0, 0, 150.0), lens=26)
        shoot("air_high", version, K.polar(1500.0, 320.0, 1100.0), (0, 0, 100.0), lens=30)
    if want("close"):
        shoot("close_balloon_front", version, mid + N * 130.0 - Z * 6.0, mid, lens=32)
        shoot("close_balloon_quarter", version, mid + N * 105.0 + X * 95.0 + Z * 8.0, mid, lens=32)
        shoot("close_balloon_back", version, mid - N * 120.0 + Z * 34.0 - X * 44.0, mid - Z * 6.0, lens=30)
        shoot("close_balloon_mooring", version, info["pivot"] + N * 46.0 + Z * 26.0 + X * 30.0, info["pivot"] + Z * 12.0, lens=24)
        g = Frame(GLOBE["bearing"], GLOBE["r"], GLOBE["z"])
        shoot("close_globe", version, g.p(60.0, -210.0, -40.0), g.p(0, 0, -12.0), lens=30)
        s = Frame(SLIPPER_HOLO["bearing"], SLIPPER_HOLO["r"], SLIPPER_HOLO["z"])
        shoot("close_slipper_hologram", version, s.p(-50.0, -150.0, -30.0), s.p(0, 0, -6.0), lens=30)
        c = Frame(COLUMNS[0][1], COLUMNS[0][2], 150.0)
        shoot("close_column", version, c.p(-70.0, -230.0, -60.0), c.p(0, 0, 40.0), lens=26)
        shoot("close_column_side", version, c.p(170.0, -110.0, -40.0), c.p(0, 0, 10.0), lens=26)
        shoot("close_column_back", version, c.p(90.0, 230.0, 120.0), c.p(0, 0, 60.0), lens=26)
        p = Frame(CARDS[0][2], CARDS[0][3], CARDS[0][4])
        shoot("close_pcx", version, p.p(70.0, -200.0, -40.0), p.p(0, 0, -6.0), lens=32)
        j = Frame(CARDS[3][2], CARDS[3][3], CARDS[3][4])
        shoot("close_jeepney_side", version, j.p(150.0, -120.0, -20.0), j.p(0, 0, -8.0), lens=32)
        f = Frame(PCX_FAR["bearing"], PCX_FAR["r"], info["pcx_far_z"])
        shoot("close_pcx_far", version, f.p(-90.0, -300.0, -60.0), f.p(0, 0, -20.0), lens=32)
    if want("flat"):
        scene = bpy.context.scene
        engine = scene.render.engine
        scene.render.engine = "BLENDER_WORKBENCH"
        scene.display.shading.light = "STUDIO"; scene.display.shading.color_type = "MATERIAL"
        scene.display.shading.show_cavity = True
        shoot("flat_balloon_front", version, mid + N * 130.0 - Z * 6.0, mid, lens=32)
        shoot("flat_balloon_side", version, mid + X * 130.0 + N * 30.0 - Z * 6.0, mid - Z * 6.0, lens=30)
        shoot("flat_balloon_mooring", version, info["pivot"] + N * 46.0 + Z * 26.0 + X * 30.0, info["pivot"] + Z * 12.0, lens=24)
        c = Frame(COLUMNS[0][1], COLUMNS[0][2], 14.0)
        shoot("flat_buoy", version, c.p(-22.0, -46.0, 18.0), c.p(0, 0, -2.0), lens=30)
        scene.render.engine = engine


def main():
    version, shots, render = "v1", [], True
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for a in args:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
        elif a.startswith("--shots="):
            shots = a.split("=", 1)[1].split(",")
        elif a == "--no-render":
            render = False
    if "--options" in args:
        options(version)
        return
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene

    # The other kits first: the balloon's mooring is laid on the canopy by a ray, the far logo on a
    # tower's roof by another, and the SIGHT report casts rays at all of them.
    review, kits = append_kits()
    roof_tree = bvh_of([kits["roof"]]) if "roof" in kits else None
    city_tree = bvh_of([kits["city"]]) if "city" in kits else None
    all_tree = bvh_of(list(kits.values())) if kits else None

    def roof_z(x, y):
        hit = roof_tree.ray_cast(Vector((x, y, 400.0)), Vector((0, 0, -1))) if roof_tree else (None,)
        return hit[0].z if hit[0] is not None else 71.17 - 0.068 * (math.hypot(x, y) - 160.0)

    def city_z(x, y, top):
        hit = city_tree.ray_cast(Vector((x, y, top)), Vector((0, 0, -1))) if city_tree else (None,)
        return hit[0].z if hit[0] is not None else None

    coll = K.collection("arena_holo")
    built = []
    for name, b, r, z0, z1, strip in COLUMNS:
        column(coll, name, b, r, z0, z1, strip, built)
    for i, (name, tile, b, r, zc, w, lights) in enumerate(CARDS):
        F, h = card(coll, name, tile, b, r, zc, w, lights, built)
        if tile in ("pcx", "tump"):
            ob = move(cubes(coll, "sign_%s_cubes" % name, F, (w * 0.72, 16.0, h * 0.9 + 14.0), 8, 300 + i, keep_out=w * 0.56), "bob", metres=1.8, period=12.0 + i)
            built.append((name, [ob], None))
    globe(coll, built)
    slipper_hologram(coll, built)
    info = {"balloon": balloon(coll, built, roof_z)}
    info["pivot"] = BALLOON_RIG["pivot"].copy()
    pcx_far(coll, built, city_z)
    info["pcx_far_z"] = next(o for n, obs, _ in built if n == "pcx_far" for o in obs).location.z

    bpy.context.view_layer.update()
    if roof_tree is not None:                                     # the balloon against the roof kit: only its feet may touch the canopy
        for ob in BALLOON_OBJECTS:
            Mx = ob.matrix_basis
            vs = [Mx @ v.co for v in ob.data.vertices]
            tree = BVHTree.FromPolygons(vs, [tuple(p.vertices) for p in ob.data.polygons])
            pairs = tree.overlap(roof_tree)
            high = set()
            for mine, _ in pairs:
                c = Mx @ ob.data.polygons[mine].center
                if c.z > roof_z(c.x, c.y) + 2.6:
                    high.add(mine)
            print("ROOF %s: %d of its faces cross the roof kit, %d of them more than 2.6 m above the canopy%s"
                  % (ob.name, len({a for a, _ in pairs}), len(high), "   <-- IT HITS A MAST OR A STAY" if high else " (its feet, in the roof)"))
    total = report(coll)
    rows = sight(built, all_tree)
    with open(MOTION, "w", encoding="utf-8") as fh:
        json.dump(dict(about="What moves in the arena's holo kit. Written by tools/author_arena_holo.py; do not edit by hand. "
                             "ArenaSceneBuilder bakes it onto an ArenaHoloMotion component.",
                       kinds=dict(rotate="turns about the object's own up axis (Unity local +y), degrees_per_second, clockwise seen from above when positive",
                                  scroll="moves the named material's texture along v, v_per_second (tiles a second), by a property block",
                                  sway="leans about its pivot: `degrees` each way, one swing every `period` seconds, on two level axes out of step",
                                  bob="rises and falls `metres` each way every `period` seconds",
                                  pulse="its light's strength runs between `low` and `high` of itself every `period` seconds"),
                       balloon=dict(about="The slipper balloon's parts, by object name: Editor/MapKit/ArenaHoloAuthor.cs hands them to "
                                          "Runtime/Map/ArenaBalloon.cs, which animates them. Faces are texture files, happy first.",
                                    body=BALLOON_RIG["body"].name, arm_wave=BALLOON_RIG["limbs"]["arm_wave"].name, arm_rest=BALLOON_RIG["limbs"]["arm_rest"].name,
                                    leg_l=BALLOON_RIG["limbs"]["leg_l"].name, leg_r=BALLOON_RIG["limbs"]["leg_r"].name,
                                    tails=[o.name for o in BALLOON_RIG["tails"]], ropes=[o.name for o in BALLOON_RIG["ropes"]],
                                    scrap=BALLOON_RIG["scrap"].name, mooring=BALLOON_RIG["mooring"].name,
                                    faces=["arena_holo_balloon%s.png" % ("" if f == "happy" else "_" + f) for f in T.BALLOON_FACES]),
                       moves=MOVES), fh, indent=1)
    print("MOTION %d entries -> %s" % (len(MOVES), MOTION))

    # The kit is saved with only its own collection in it.
    for col in list(review.children):
        review.children.unlink(col)
    os.makedirs(KITS, exist_ok=True); os.makedirs(LOGS, exist_ok=True)
    keep_review = [c for c in kits.values()]
    for c in keep_review:
        c.use_fake_user = False
    scene.collection.children.unlink(review)
    stash = {}
    for kit, col in kits.items():
        stash[kit] = [o for o in col.all_objects]
    bpy.context.preferences.filepaths.save_version = 0
    # Take the other kits' data out of the file before it is written.
    for kit, col in kits.items():
        for o in list(col.all_objects):
            bpy.data.objects.remove(o)
        bpy.data.collections.remove(col)
    bpy.data.collections.remove(review)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for item in list(block):
            if item.users == 0:
                block.remove(item)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(KITS, "holo.blend"))
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_mainfile()
    print("SAVED holo.blend with collections %s" % [c.name for c in bpy.data.collections])

    if render:
        review, kits = append_kits()
        sun = bpy.data.objects.new("review moonlight", bpy.data.lights.new("review moonlight", "SUN"))
        sun.data.energy = 2.4; sun.data.color = (0.66, 0.76, 1.0); sun.data.angle = math.radians(20)
        sun.rotation_euler = (math.radians(50), math.radians(8), math.radians(20))
        review.objects.link(sun)
        world = bpy.data.worlds.new("night"); scene.world = world; world.use_nodes = True
        world.node_tree.nodes["Background"].inputs[0].default_value = (0.012, 0.016, 0.06, 1)
        world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
        engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
        scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
        scene.view_settings.view_transform = "Standard"
        for o in bpy.data.objects:                                # the craft and the haze sheets pass the lens: out of the pictures
            if "city_haze" in o.name:
                o.hide_render = True
        BALLOON_RIG["scrap"].hide_render = True                    # Unity shows it only when the balloon has been popped
        if shots != ["pose"]:
            pictures(version, shots, info)
        if not shots or "pose" in shots:
            posed_pictures(version, info)
    print("HOLO_OK %s triangles %d" % (version, total))


if __name__ == "__main__":
    main()
