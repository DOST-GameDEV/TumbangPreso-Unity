"""Shared numbers and mesh helpers for the arena kits (tools/author_arena_<kit>.py).

Read docs/ARENA_ART_BRIEF.md first. Blender units are metres, z is up, y is north, the can is the
origin. `lathe` sweeps ONE closed profile round the centre; `Parts` gathers closed solids. The
flat `mat()` colours here are blockout placeholders: a kit paints its own textures.
"""
import bpy, bmesh, math, os, random, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "ArtSource", "arena")
LOGO = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "ui", "brand", "tump_logo.png")
LOGO_ASPECT = 1895.0 / 1247.0
random.seed(23)

# ---------------------------------------------------------------- the numbers, centre outward
PIT_R = 40.0                      # the open shaft the stage hovers in
FIELD_R = 85.0                    # the field ends at the lower bowl's front wall
BARRIER_R = 80.0
DECK_Z = -2.0                     # the hull's deck, outside the stadium
BASE_Z = -12.0                    # how far the stadium's solids run into the hull
LOWER_ROWS, UPPER_ROWS = 26, 20
UPPER_ARCS = ((-32.0, 32.0), (58.0, 122.0), (148.0, 212.0), (238.0, 302.0))
CANOPY = (150.0, 190.0)           # inner and outer radius
HULL_R = 236.0

PALETTE = {
    "turfA": (0.09, 0.40, 0.10), "turfB": (0.13, 0.50, 0.13), "line": (0.92, 0.95, 0.92),
    "track": (0.05, 0.06, 0.09), "void": (0.005, 0.006, 0.012),
    "seat": (0.03, 0.05, 0.13), "aisle": (0.16, 0.19, 0.27), "structure": (0.09, 0.11, 0.17),
    "steel": (0.13, 0.16, 0.24), "hull": (0.07, 0.085, 0.13), "hull2": (0.10, 0.12, 0.18),
    "ledWhite": (0.90, 0.95, 1.00), "ledBlue": (0.03, 0.06, 0.55), "ledMagenta": (0.85, 0.08, 0.55),
    "ledGold": (1.00, 0.80, 0.20), "flood": (0.95, 0.98, 1.00), "screen": (0.02, 0.03, 0.06),
    "metal": (0.16, 0.18, 0.25), "metal2": (0.22, 0.25, 0.34), "holo": (0.25, 0.90, 1.00),
    "thrust": (0.35, 0.85, 1.00), "glass": (0.05, 0.10, 0.20),
    "towerA": (0.035, 0.05, 0.11), "towerB": (0.05, 0.045, 0.10), "towerC": (0.03, 0.065, 0.10),
    "cityfloor": (0.012, 0.016, 0.04),
    "can": (0.85, 0.85, 0.9), "taya": (0.85, 0.20, 0.55), "attacker": (0.60, 0.40, 0.90),
    "jump": (0.30, 1.00, 0.80), "speed": (0.75, 1.00, 0.30), "pickup": (0.80, 0.60, 1.00),
    "beam": (0.75, 0.88, 1.00),
}
EMIT = {"line": 0.4, "ledWhite": 2.2, "ledBlue": 5.0, "ledMagenta": 4.0, "ledGold": 1.5, "flood": 14.0,
        "holo": 3.0, "thrust": 9.0, "jump": 4.0, "speed": 3.0, "pickup": 5.0, "can": 0.5}
SPECKLE = {   # bright cells over the base colour: a stand-in for the crowd, and for lit windows
    "seat": ((0.95, 0.95, 0.90), 0.16, 2.4, 0.5),
    "cityfloor": ((0.55, 0.75, 1.00), 0.05, 0.06, 1.0),
}
WINDOWS = {   # rows of windows, some lit: (lit colour, unlit colour)
    "towerA": ((0.60, 0.85, 1.00), (0.07, 0.10, 0.20)), "towerB": ((1.00, 0.62, 0.85), (0.10, 0.08, 0.19)),
    "towerC": ((0.55, 1.00, 0.92), (0.06, 0.12, 0.18)),
}
_mats = {}


def mat(name, alpha=1.0):
    key = (name, alpha)
    if key in _mats:
        return _mats[key]
    c = PALETTE[name]
    m = bpy.data.materials.new("arena_%s" % name)
    m.use_nodes = True
    m.diffuse_color = (c[0], c[1], c[2], alpha)
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (c[0], c[1], c[2], 1)
    b.inputs["Roughness"].default_value = 0.8
    if name in EMIT:
        b.inputs["Emission Color"].default_value = (c[0], c[1], c[2], 1)
        b.inputs["Emission Strength"].default_value = EMIT[name]
    if alpha < 1:
        b.inputs["Alpha"].default_value = alpha
        m.surface_render_method = "BLENDED"
    if name in SPECKLE:
        bright, share, scale, glow = SPECKLE[name]
        tex = nt.nodes.new("ShaderNodeTexCoord")
        vor = nt.nodes.new("ShaderNodeTexVoronoi"); vor.inputs["Scale"].default_value = scale
        ramp = nt.nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.interpolation = "CONSTANT"
        e = ramp.color_ramp.elements
        e[0].position = 0.0; e[0].color = (c[0], c[1], c[2], 1)
        e[1].position = 1.0 - share; e[1].color = (bright[0], bright[1], bright[2], 1)
        mid = e.new(1.0 - share * 2.2); mid.color = (c[0] * 2.4 + 0.02, c[1] * 2.4 + 0.03, c[2] * 2.2 + 0.06, 1)
        nt.links.new(tex.outputs["Object"], vor.inputs["Vector"])
        nt.links.new(vor.outputs["Color"], ramp.inputs["Fac"])
        nt.links.new(ramp.outputs["Color"], b.inputs["Base Color"])
        nt.links.new(ramp.outputs["Color"], b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = glow
    if name in WINDOWS:
        lit, unlit = WINDOWS[name]
        tex = nt.nodes.new("ShaderNodeTexCoord")
        sep = nt.nodes.new("ShaderNodeSeparateXYZ")
        add = nt.nodes.new("ShaderNodeMath"); add.operation = "ADD"
        com = nt.nodes.new("ShaderNodeCombineXYZ")
        brick = nt.nodes.new("ShaderNodeTexBrick")
        brick.inputs["Color1"].default_value = (unlit[0], unlit[1], unlit[2], 1)
        brick.inputs["Color2"].default_value = (lit[0], lit[1], lit[2], 1)
        brick.inputs["Mortar"].default_value = (c[0], c[1], c[2], 1)
        brick.inputs["Scale"].default_value = 0.16
        brick.inputs["Mortar Size"].default_value = 0.035
        brick.inputs["Bias"].default_value = -0.72
        brick.inputs["Brick Width"].default_value = 0.42
        brick.inputs["Row Height"].default_value = 0.36
        brick.offset = 0.0
        nt.links.new(tex.outputs["Object"], sep.inputs[0])
        nt.links.new(sep.outputs["X"], add.inputs[0]); nt.links.new(sep.outputs["Y"], add.inputs[1])
        nt.links.new(add.outputs[0], com.inputs["X"]); nt.links.new(sep.outputs["Z"], com.inputs["Y"])
        nt.links.new(com.outputs[0], brick.inputs["Vector"])
        nt.links.new(brick.outputs["Color"], b.inputs["Base Color"])
        nt.links.new(brick.outputs["Color"], b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = 0.55
    _mats[key] = m
    return m


def polar(r, a_deg, z=0.0):
    a = math.radians(a_deg)
    return Vector((r * math.sin(a), r * math.cos(a), z))


def collection(name):
    c = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(c)
    return c


def finish(bm, name, materials, coll):
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    for m in materials:
        me.materials.append(mat(m))
    ob = bpy.data.objects.new(name, me)
    coll.objects.link(ob)
    return ob


def lathe(name, profile, angles, materials, coll, full=True, origin=(0.0, 0.0, 0.0), paint=None, cap="structure",
          squash=(1.0, 1.0)):
    """ONE closed solid: `profile` is a closed loop of (r, z, tag) swept through `angles` (degrees,
    0 north, clockwise). The tag names the strip from that point to the next; `paint(tag, a0, a1)`
    answers that strip's material for the column between two angles. A full sweep closes on
    itself; a partial one is capped at both ends, so either way the mesh has no open edge."""
    bm = bmesh.new()
    o = Vector(origin)
    n = len(profile)
    axis = {}
    cols = []
    for a in angles:
        col = []
        for j, (r, z, _) in enumerate(profile):
            if r < 1e-6:
                if j not in axis:
                    axis[j] = bm.verts.new(o + Vector((0, 0, z)))
                col.append(axis[j])
            else:
                v = polar(r, a, z)
                col.append(bm.verts.new(o + Vector((v.x * squash[0], v.y * squash[1], z))))
        cols.append(col)
    index = {m: i for i, m in enumerate(materials)}
    m = len(angles)
    for i in range(m if full else m - 1):
        p, q = cols[i], cols[(i + 1) % m]
        a0 = angles[i]
        a1 = angles[(i + 1) % m] if i + 1 < m else angles[0] + 360.0
        for j in range(n):
            k = (j + 1) % n
            quad = []
            for v in (p[j], p[k], q[k], q[j]):
                if v not in quad:
                    quad.append(v)
            if len(quad) < 3:
                continue
            f = bm.faces.new(quad)
            tag = profile[j][2]
            f.material_index = index[paint(tag, a0, a1) if paint else tag]
    if not full:
        for col, flip in ((cols[0], False), (cols[-1], True)):
            vs = list(dict.fromkeys(col))
            f = bm.faces.new(vs[::-1] if flip else vs)
            f.material_index = index[cap]
    return finish(bm, name, materials, coll)


class Parts:
    """Separate closed solids gathered into one object: struts, masts, fins."""

    def __init__(self):
        self.bm = bmesh.new()

    def box(self, centre, size, yaw=0.0, pitch=0.0, material=0):
        hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        cy, sy = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        cp, sp = math.cos(math.radians(pitch)), math.sin(math.radians(pitch))
        vs = []
        for sx in (-1, 1):
            for syy in (-1, 1):
                for sz in (-1, 1):
                    x, y, z = sx * hx, syy * hy, sz * hz
                    y, z = y * cp - z * sp, y * sp + z * cp          # pitch about x: + lifts the far end
                    x, y = x * cy + y * sy, -x * sy + y * cy         # yaw, clockwise from north
                    vs.append(self.bm.verts.new((centre[0] + x, centre[1] + y, centre[2] + z)))
        for f in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
            self.bm.faces.new([vs[i] for i in f]).material_index = material
        return self

    def tube(self, p0, p1, radius, sides=8, radius1=None, material=0):
        """A closed prism from p0 to p1: a mast, a strut, a cable."""
        p0, p1 = Vector(p0), Vector(p1)
        axis = (p1 - p0).normalized()
        ref = Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0))
        u = axis.cross(ref).normalized(); v = axis.cross(u)
        ra, rb = radius, radius if radius1 is None else radius1
        a = [self.bm.verts.new(p0 + (u * math.cos(t) + v * math.sin(t)) * ra) for t in (2 * math.pi * i / sides for i in range(sides))]
        b = [self.bm.verts.new(p1 + (u * math.cos(t) + v * math.sin(t)) * rb) for t in (2 * math.pi * i / sides for i in range(sides))]
        for i in range(sides):
            k = (i + 1) % sides
            self.bm.faces.new((a[i], a[k], b[k], b[i])).material_index = material
        self.bm.faces.new(a[::-1]).material_index = material
        self.bm.faces.new(b).material_index = material
        return self

    def done(self, name, materials, coll):
        return finish(self.bm, name, materials, coll)


def logo(name, centre, bearing, width, coll, glow=1.6):
    """The logo on an upright quad at `centre`, read by someone looking along `bearing`. It is the
    only face in its plane: it stands 0.25 m proud of the dark screen behind it."""
    h = width / LOGO_ASPECT
    side = polar(width / 2, bearing - 90)
    up = Vector((0, 0, h / 2))
    c = Vector(centre)
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in (c + side - up, c - side - up, c - side + up, c + side + up)], [], [(0, 1, 2, 3)])
    uv = me.uv_layers.new(name="UVMap")
    for i, co in enumerate(((0, 0), (1, 0), (1, 1), (0, 1))):
        uv.data[i].uv = co
    m = bpy.data.materials.get("arena_logo")
    if m is None:
        m = bpy.data.materials.new("arena_logo")
        m.use_nodes = True
        nt = m.node_tree
        b = nt.nodes.get("Principled BSDF")
        tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(LOGO, check_existing=True)
        nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
        nt.links.new(tex.outputs["Alpha"], b.inputs["Alpha"])
        nt.links.new(tex.outputs["Color"], b.inputs["Emission Color"])
        b.inputs["Emission Strength"].default_value = glow
        m.surface_render_method = "BLENDED"
    me.materials.append(m)
    ob = bpy.data.objects.new(name, me)
    coll.objects.link(ob)
    return ob


def circle(n):
    return [i * 360.0 / n for i in range(n)]


def section_angles(lo, hi, sections, aisle=1.6, split=3, ends=False):
    """Angles across lo..hi for seat sections with an aisle strip on every boundary between them
    (and, with `ends`, half an aisle at each end): the aisle is a column of the same surface."""
    width = (hi - lo) / sections
    out = []
    for k in range(sections):
        b = lo + k * width
        left = b + aisle / 2 if (k > 0 or ends) else b
        right = b + width - aisle / 2 if (k < sections - 1 or ends) else b + width
        if k > 0 or ends:
            out.append(b - aisle / 2 if k > 0 else b)
        for s in range(split):
            out.append(left + (right - left) * s / split)
        if k == sections - 1:
            out.append(right)
            if ends:
                out.append(b + width)
    return sorted(set(round(a, 4) for a in out))


EYE = 3.3                                                         # the eyes of a player standing by the can


def rim_elevation(bearing):
    """How high the stadium itself reaches into the sky seen from the can, in degrees, toward a
    compass bearing: the edge of the canopy over a stand, the concourse wall and the screen in a corner."""
    b = bearing % 360.0
    for lo, hi in UPPER_ARCS:
        if lo <= b <= hi or lo <= b - 360.0 <= hi:
            return math.degrees(math.atan2(71.2 - EYE, CANOPY[0] + 9.0))
    for c in (45, 135, 225, 315):
        if abs(b - c) <= 8.0:
            return math.degrees(math.atan2(53.5 - EYE, 152.0))
    return math.degrees(math.atan2(27.2 - EYE, 140.0))


