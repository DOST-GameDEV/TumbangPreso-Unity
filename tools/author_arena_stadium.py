"""The arena: a stadium floating in the night sky over a neon city. THE MODEL, before texturing.

    blender -b --python tools/author_arena_stadium.py -- --version=v13

Owner, 2026-10-05: a large stadium ("i need the map large"), the round layout ("i liked the previous
layout"), the night football stadium's colours and feel, the TUMP logo, and then: "concept is
floating arena in the sky with cyberpunk-esque aesthetic", "buildings will need actual more
detail", "fix disconnected parts", and of the blockout: "the model looks unoptimized and it seems
like connected pieces are just multiple rectangular prisms placed next to each other than actually
being single connectedmeshes" and "idk if thats true z fighting but fix it".

So this file builds real meshes, not stacked boxes:
  - every ring-shaped thing (the field, the bowls, the canopies, the hull, the stage's platforms)
    is ONE closed profile swept round the centre (`lathe`): rows of seats, aisles, walls, walkways
    and light strips are faces of the same surface, told apart by material, sharing every vertex;
  - nothing is laid on top of another face. A painted line is a strip of faces in the turf, a
    light strip is a band in the profile. Where two solids meet, one runs INTO the other;
  - every strut, mast and cable is built between the two points it joins and ends inside both.
`report()` counts open edges and triangles on every object at the end.

Blender units are metres, z is up, y is north. The gameplay markers and the light beams are in
their own collections and are not part of the model. Materials are flat placeholders: texturing
comes next (the crowd will be animated 2D sprites of the game's characters, placed per row in
Unity, not modelled here).
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


# ---------------------------------------------------------------- 1. the stage (the gameplay's platforms)
def stage(coll):
    """One layout: the can on a raised drum, a ring walk, four outer arcs, two lofts. Each is a
    hover plate: dark metal, a lit rim, a glowing emitter underneath that holds it up over the
    open shaft. These are stand-ins for the gameplay kit; the five layouts come from the data."""
    mats = ["metal", "holo", "metal2"]
    plate = lambda r0, r1, top, thick: [
        (r0, top, "holo"), (r0 + 0.3, top, "metal"), (r1 - 0.3, top, "holo"), (r1, top, "metal2"),
        (r1, top - thick, "holo"), (r0, top - thick, "metal2")]
    lathe("stage drum (the can, raised)", [(0, 1.6, "metal"), (5.7, 1.6, "holo"), (6.0, 1.6, "metal2"),
                                            (6.0, -0.6, "metal2"), (4.0, -1.4, "holo"), (0, -1.4, "holo")],
          circle(32), mats, coll)
    lathe("stage ring walk", plate(10.0, 15.0, 0.1, 1.0), circle(48), mats, coll)
    for a in (45, 135, 225, 315):
        lathe("stage outer arc %d" % a, plate(19.0, 24.0, 0.1, 1.0), [a - 34 + i * 68 / 12 for i in range(13)],
              mats, coll, full=False, cap="metal2")
    for a in (0, 180):
        lathe("stage loft %d (jump pad only)" % a, plate(19.5, 23.5, 4.1, 0.6), [a - 12 + i * 24 / 6 for i in range(7)],
              mats, coll, full=False, cap="metal2")
    links = Parts()
    for a in (0, 90, 180, 270):                                   # ramps, their ends inside the drum and the walk
        links.box(polar(8.0, a, 0.5), (4.0, 5.0, 0.7), yaw=a, pitch=-18.5)
    for a in (45, 135, 225, 315):                                 # bridges, their ends inside the walk and the arcs
        links.box(polar(17.0, a, -0.45), (3.5, 5.0, 0.9), yaw=a)
    links.done("stage ramps and bridges", ["metal2"], coll)


def markers(coll):
    p = Parts()
    p.box((0, 0, 1.75), (0.22, 0.22, 0.3), material=0)
    p.box((0, -2.5, 2.5), (0.6, 0.6, 1.8), material=1)
    for x in (-1.8, 0, 1.8):
        p.box((x, 12.5, 1.0), (0.6, 0.6, 1.8), material=2)
    for a in (0, 180):
        p.box(polar(16.5, a, 0.2), (1.7, 1.7, 0.16), material=3)
    for a in (90, 270):
        p.box(polar(12.5, a, 0.2), (1.5, 3.0, 0.16), yaw=a, material=4)
    for a, z in ((45, 1.0), (225, 1.0), (0, 5.0), (180, 5.0)):
        p.box(polar(21.5, a, z), (0.6, 0.6, 0.6), material=5)
    for k in range(6):
        p.box((-58 + k * 1.2, -24 + (k % 2) * 0.8, 0.9), (0.5, 0.5, 1.8), material=6)
    p.done("gameplay markers and six people for scale", ["can", "taya", "attacker", "jump", "speed", "pickup", "line"], coll)
    h = bmesh.new()
    ob = lathe("hologram of the next layout", [(26.0, 1.4, "holo"), (30.0, 1.4, "holo"), (30.0, 0.4, "holo"), (26.0, 0.4, "holo")],
               [60 + i * 10 for i in range(7)], ["holo"], coll, full=False, cap="holo")
    ob.modifiers.new("wire", "WIREFRAME").thickness = 0.12
    ob2 = lathe("hologram of the next layout 2", [(26.0, 1.4, "holo"), (30.0, 1.4, "holo"), (30.0, 0.4, "holo"), (26.0, 0.4, "holo")],
                [240 + i * 10 for i in range(7)], ["holo"], coll, full=False, cap="holo")
    ob2.modifiers.new("wire", "WIREFRAME").thickness = 0.12
    h.free()


# ---------------------------------------------------------------- 2. the field
def field(coll):
    """One solid from the shaft's wall to the lower bowl: the kerb, the turf (mown in sixteen
    wedges), the painted rings and spokes, the track, the LED barrier."""
    step = 360.0 / 64
    angles = []
    for i in range(64):
        a = i * step
        if i % 8 == 0:                                            # a painted spoke: a strip 0.2 degrees wide
            angles += [a - 0.1, a + 0.1]
        else:
            angles.append(a)
    angles = sorted(x % 360.0 for x in angles)
    profile = [
        (PIT_R, BASE_Z, "structure"), (PIT_R, -6.4, "holo"), (PIT_R, -6.0, "structure"), (PIT_R, -2.0, "holo"),
        (PIT_R, -1.6, "structure"), (PIT_R, 0.35, "steel"), (PIT_R + 0.5, 0.35, "holo"), (PIT_R + 1.3, 0.35, "steel"),
        (PIT_R + 2.0, 0.0, "turf"), (48.0, 0.0, "line"), (48.3, 0.0, "turf"), (68.0, 0.0, "line"), (68.3, 0.0, "turf"),
        (75.0, 0.0, "track"), (BARRIER_R - 0.6, 0.0, "led"), (BARRIER_R - 0.6, 1.5, "steel"), (BARRIER_R, 1.5, "led"),
        (BARRIER_R, 0.0, "track"), (FIELD_R, 0.0, "structure"), (FIELD_R, BASE_Z, "structure")]

    def paint(tag, a0, a1):
        mid = ((a0 + a1) / 2) % 360.0
        if tag == "turf":
            if a1 - a0 < 0.5:
                return "line"
            return "turfA" if int(mid // 22.5) % 2 == 0 else "turfB"
        if tag == "led":
            return "ledWhite" if int(mid // 11.25) % 2 == 0 else "ledBlue"
        return tag

    lathe("field (kerb, turf, lines, track, LED barrier)", profile, angles,
          ["structure", "holo", "steel", "turfA", "turfB", "line", "track", "ledWhite", "ledBlue"], coll, paint=paint)


# ---------------------------------------------------------------- 3. the bowls
def rows(r_in, r_out, z_in, z_out, count):
    """The stepped part of a stand's profile: a tread and a riser for every row."""
    tread, rise = (r_out - r_in) / count, (z_out - z_in) / (count - 1)
    pts = []
    for k in range(count):
        pts.append((r_in + k * tread, z_in + k * rise, "tread"))
        if k < count - 1:
            pts.append((r_in + (k + 1) * tread, z_in + k * rise, "riser"))
    return pts


def seat_paint(width_of_aisle):
    def paint(tag, a0, a1):
        mid = (a0 + a1) / 2
        aisle = (a1 - a0) < width_of_aisle
        if tag in ("tread", "riser"):
            return "aisle" if aisle else "seat"
        if tag == "ribbon":
            return "ledWhite" if int((mid % 360.0) // 15.0) % 2 == 0 else "ledBlue"
        if tag == "shell":
            return "steel" if aisle else "hull2"
        if tag == "doorwall":
            return "void" if aisle else "structure"
        return tag
    return paint


BOWL_MATS = ["structure", "steel", "seat", "aisle", "ledWhite", "ledBlue", "ledMagenta", "hull2", "track", "void"]


def lower_bowl(coll):
    """The whole lower tier, all the way round, and the concourse behind it: ONE solid. Two banks
    of rows with a cross walkway between them; the way in is a door in the wall behind that
    walkway at every aisle, and a tunnel at field level on the south side."""
    profile = [(84.5, BASE_Z, "structure"), (84.5, 1.3, "ribbon"), (84.5, 3.3, "structure"), (84.5, 4.1, "steel"),
               (85.3, 4.1, "structure")]
    profile += rows(85.3, 106.0, 3.0, 12.6, 13)
    profile += [(106.0, 12.6, "track"), (110.0, 12.6, "doorwall"), (110.0, 15.6, "ledBlue"), (110.0, 16.0, "steel"),
                (110.6, 16.0, "structure")]
    profile += rows(110.6, 130.0, 15.4, 26.0, 12)
    profile += [(130.0, 26.0, "track"), (140.0, 26.0, "structure"), (140.0, 27.2, "steel"), (141.0, 27.2, "shell"),
                (141.0, 8.0, "ledMagenta"), (141.0, 7.2, "shell"), (141.0, 2.6, "doorwall"), (141.0, DECK_Z, "structure"),
                (141.0, BASE_Z, "structure")]
    lathe("lower bowl and concourse", profile, section_angles(0.0, 360.0, 24, ends=True)[:-1], BOWL_MATS, coll,
          paint=seat_paint(2.0))
    tunnel = Parts()                                              # the players' tunnel: two walls and a roof, a dark throat
    for dx in (-4.6, 4.6):
        e = polar(86.0, 180.0, 2.2) + polar(dx, 270.0)
        tunnel.box((e.x, e.y, 2.2), (0.8, 7.0, 4.6), yaw=180.0, material=0)
    tunnel.box(polar(86.0, 180.0, 4.7), (10.6, 7.4, 0.8), yaw=180.0, material=0)
    tunnel.box(polar(87.6, 180.0, 2.1), (8.4, 5.0, 4.4), yaw=180.0, material=1)
    tunnel.done("players' tunnel", ["structure", "void"], coll)


def upper_stands(coll):
    """Four stands with open corners: each one solid, its ends walled, its outside lit."""
    profile = [(140.2, BASE_Z, "structure"), (140.2, 28.2, "ribbon"), (140.2, 31.4, "structure"), (140.2, 33.1, "steel"),
               (141.0, 33.1, "structure")]
    profile += rows(141.0, 176.0, 32.0, 58.0, UPPER_ROWS)
    profile += [(176.0, 58.0, "track"), (179.0, 58.0, "structure"), (179.0, 59.2, "steel"), (180.0, 59.2, "shell"),
                (180.0, 46.0, "ledMagenta"), (180.0, 45.2, "shell"), (181.2, 30.0, "ledBlue"), (181.2, 29.2, "shell"),
                (180.0, 9.0, "ledMagenta"), (180.0, 8.2, "shell"), (180.0, 3.0, "doorwall"), (180.0, DECK_Z, "structure"),
                (180.0, BASE_Z, "structure")]
    fins = Parts()
    for lo, hi in UPPER_ARCS:
        lathe("upper stand %d" % round((lo + hi) / 2), profile, section_angles(lo, hi, 4), BOWL_MATS, coll,
              full=False, paint=seat_paint(2.0))
        for k in range(9):                                        # buttress fins up the outside, their backs in the wall
            a = lo + (hi - lo) * k / 8
            fins.box(polar(180.6, a, 28.0), (1.2, 4.0, 62.0), yaw=a, pitch=2.0)
        for end, a in ((-1, lo), (1, hi)):                        # a stair tower closing each end of the stand
            c = polar(160.0, a + end * 1.2)
            fins.tube((c.x, c.y, DECK_Z - 1.0), (c.x, c.y, 60.0), 4.2, 8, 3.6)
    fins.done("stand buttresses and stair towers", ["steel"], coll)


# ---------------------------------------------------------------- 4. the canopies
def canopies(coll, fx):
    r0, r1 = CANOPY

    def under(r):                                                 # the canopy's underside at radius r
        return 68.4 + (64.0 - 68.4) * (r - r0) / (r1 - r0)

    profile = [(r0, 68.4, "ledBlue"), (r0, 69.4, "panel"), (r0 + 9.0, 71.2, "panel"), (r1 - 6.0, 69.4, "structure"),
               (r1, 67.6, "hull2"), (r1, 64.0, "steel")]
    steel, lamps, beams = Parts(), Parts(), Parts()
    for lo, hi in UPPER_ARCS:
        lathe("canopy %d" % round((lo + hi) / 2), profile, [lo + i * (hi - lo) / 16 for i in range(17)],
              ["structure", "steel", "ledBlue", "hull2", "glass"], coll, full=False, cap="steel",
              paint=lambda tag, a0, a1, lo=lo, hi=hi: ("glass" if int(round((a0 - lo) / ((hi - lo) / 16))) % 2 == 0 else "structure") if tag == "panel" else tag)
        for k in range(9):                                        # ribs: fins under the plate, their tops inside it
            a = lo + 2.0 + (hi - lo - 4.0) * k / 8
            mid_r = (r0 + r1) / 2 + 1.0
            steel.box(polar(mid_r, a, under(mid_r) - 0.9), (0.7, r1 - r0 - 5.0, 2.6), yaw=a,
                      pitch=math.degrees(math.atan2(64.0 - 68.4, r1 - r0)))
        for k in range(5):                                        # masts behind the stand, from the deck into the plate
            a = lo + 3.0 + (hi - lo - 6.0) * k / 4
            foot, head = polar(184.5, a, DECK_Z - 1.0), polar(184.5, a, under(184.5) + 0.8)
            steel.tube(foot, head, 1.5, 8, 1.1)
            steel.tube(polar(184.5, a, 46.0), polar(166.0, a, under(166.0) + 0.5), 0.45, 6)   # the tie strut
            steel.tube(polar(184.5, a, 30.0), polar(180.3, a, 44.0), 0.4, 6)                  # braced to the stand
        for k in range(5):                                        # floodlight banks hung on the front edge
            a = lo + (hi - lo) * (k + 0.5) / 5
            lamps.box(polar(r0 + 2.2, a, under(r0 + 2.2) - 0.7), (11.0, 2.4, 1.9), yaw=a, material=0)
            lamps.box(polar(r0 + 1.4, a, under(r0 + 1.4) - 1.6), (10.0, 1.2, 0.5), yaw=a, pitch=-20, material=1)
            src = polar(r0 + 1.4, a, under(r0 + 1.4) - 2.0); target = polar(60.0, a, 0.2)
            flat = Vector((target.x - src.x, target.y - src.y, 0))
            beams.box((src + target) / 2, (7.0, (target - src).length, 0.25),
                      yaw=math.degrees(math.atan2(flat.x, flat.y)),
                      pitch=math.degrees(math.atan2(target.z - src.z, flat.length)))
    steel.done("canopy ribs, masts and struts", ["steel"], coll)
    lamps.done("floodlight banks", ["steel", "flood"], coll)
    ob = beams.done("floodlight beams (preview only, not the model)", [], fx)
    ob.data.materials.append(mat("beam", alpha=0.03))
    ob.hide_viewport = True


# ---------------------------------------------------------------- 5. the scoreboard and the screens
def screens(coll):
    ring = [i * 45.0 + 22.5 for i in range(8)]
    lathe("scoreboard", [(0, 52.6, "steel"), (7.0, 52.6, "steel"), (9.4, 51.6, "ledWhite"), (9.4, 50.9, "steel"),
                         (8.5, 50.7, "screen"), (8.5, 44.3, "steel"), (9.4, 44.1, "ledBlue"), (9.4, 43.4, "steel"),
                         (6.0, 41.6, "holo"), (0, 41.6, "holo")],
          ring, ["steel", "screen", "ledWhite", "ledBlue", "holo"], coll)
    for a in (0, 90, 180, 270):
        logo("scoreboard logo %d" % a, polar(8.5 * math.cos(math.radians(22.5)) + 0.25, a, 47.5), a + 180, 5.6, coll)
    hang = Parts()
    for lo, hi in UPPER_ARCS:                                     # four cables, each end inside what it holds
        a = (lo + hi) / 2
        hang.tube(polar(6.5, a, 52.3), polar(CANOPY[0] + 0.6, a, 68.9), 0.16, 6)
    hang.done("scoreboard cables", ["steel"], coll)
    frames = Parts()
    for a in (45, 135, 225, 315):                                 # a screen in each open corner, on two braced masts
        c = polar(152.0, a, 44.0)
        frames.box((c.x, c.y, c.z), (40.0, 1.6, 18.0), yaw=a, material=1)             # the back plate (the dark screen)
        for dz in (-9.4, 9.4):
            frames.box((c.x, c.y, c.z + dz), (41.6, 2.6, 0.9), yaw=a, material=0)
        for dx in (-20.4, 20.4):
            e = c + polar(dx, a + 90)
            frames.box((e.x, e.y, e.z), (0.9, 2.3, 19.6), yaw=a, material=0)
        feet = []
        for off in (-13.0, 13.0):
            q = polar(153.6, a) + polar(off, a + 90)
            feet.append(q)
            frames.tube((q.x, q.y, DECK_Z - 1.0), (q.x, q.y, 52.0), 1.1, 8, 0.8, material=0)
        frames.tube((feet[0].x, feet[0].y, 4.0), (feet[1].x, feet[1].y, 30.0), 0.35, 6, material=0)
        frames.tube((feet[1].x, feet[1].y, 4.0), (feet[0].x, feet[0].y, 30.0), 0.35, 6, material=0)
        logo("corner screen logo %d" % a, polar(152.0 - 1.05, a, 44.0), a, 24.0, coll)
    frames.done("corner screens and their masts", ["steel", "screen"], coll)
    booth = Parts()                                               # the host's booth on the north concourse
    b = polar(135.0, 0.0)
    booth.box((b.x, b.y, 27.9), (24.0, 7.0, 4.2), material=0)
    booth.box((b.x, b.y - 3.3, 28.4), (22.0, 0.9, 2.4), material=1)                   # the glass, 0.15 m proud
    booth.box((b.x, b.y - 0.6, 30.3), (26.0, 9.4, 0.6), pitch=4.0, material=2)        # the roof, over the glass
    booth.done("host booth", ["structure", "glass", "ledGold"], coll)


# ---------------------------------------------------------------- 6. the hull it all floats on
def hull(coll):
    profile = [
        (PIT_R + 1.0, DECK_Z, "track"), (196.0, DECK_Z, "plaza"), (231.0, DECK_Z, "steel"), (231.0, -0.7, "steel"),
        (233.0, -0.7, "ledBlue"), (HULL_R, -3.0, "hull"), (HULL_R + 3.0, -9.0, "ledMagenta"), (HULL_R + 3.0, -10.2, "hull"),
        (228.0, -26.0, "panel"), (196.0, -42.0, "holo"), (193.0, -43.2, "panel"), (150.0, -60.0, "panel"),
        (92.0, -70.0, "hull"), (62.0, -76.0, "ledBlue"), (58.0, -76.4, "hull"), (PIT_R + 1.0, -76.4, "shaft"),
        (PIT_R + 1.0, -40.0, "holo"), (PIT_R + 1.0, -39.2, "shaft")]

    def paint(tag, a0, a1):
        mid = ((a0 + a1) / 2) % 360.0
        if tag == "panel":
            return "hull" if int(mid // 22.5) % 2 == 0 else "hull2"
        if tag == "plaza":
            return "structure" if int(mid // 11.25) % 2 == 0 else "hull2"
        if tag == "shaft":
            return "structure"
        return tag

    lathe("hull (deck, rim, underside, shaft)", profile, circle(64),
          ["track", "structure", "hull2", "steel", "ledBlue", "ledMagenta", "hull", "holo"], coll, paint=paint)
    for k in range(8):                                            # eight engines, their heads inside the hull
        a = k * 45.0 + 22.5
        c = polar(176.0, a)
        lathe("engine %d" % k, [(0, -44.0, "hull2"), (15.0, -44.0, "hull2"), (17.0, -62.0, "steel"), (14.0, -74.0, "hull2"),
                                (19.0, -90.0, "ledBlue"), (17.0, -90.0, "thrust"), (12.0, -77.0, "thrust"), (0, -77.0, "thrust")],
              circle(20), ["hull2", "steel", "ledBlue", "thrust"], coll, origin=(c.x, c.y, 0.0))
    for a in (45, 135, 225, 315):                                 # four landing pads on the rim
        c = polar(254.0, a)
        lathe("landing pad %d" % a, [(0, -1.4, "steel"), (12.0, -1.4, "ledWhite"), (12.6, -1.4, "steel"), (20.0, -1.4, "ledMagenta"),
                                     (22.0, -1.4, "hull"), (22.0, -4.0, "hull2"), (16.0, -10.0, "hull"), (0, -10.0, "hull")],
              circle(24), ["steel", "ledWhite", "ledMagenta", "hull", "hull2"], coll, origin=(c.x, c.y, 0.0))
    plaza = Parts()
    for lo, hi in UPPER_ARCS:                                     # a gate hall behind each stand, its back in the stand
        a = (lo + hi) / 2
        plaza.box(polar(192.0, a, 4.0), (46.0, 22.0, 13.0), yaw=a, material=0)
        plaza.box(polar(203.4, a, 2.6), (34.0, 1.0, 8.0), yaw=a, material=1)          # the lit glass front, 0.2 m proud
        plaza.box(polar(198.0, a, 11.4), (52.0, 34.0, 1.2), yaw=a, pitch=-4.0, material=2)   # the roof, oversailing
    for k in range(32):
        a = k * 11.25 + 5.6
        foot = polar(224.0, a, DECK_Z - 0.5)
        plaza.tube(foot, foot + Vector((0, 0, 9.0)), 0.3, 6, 0.18, material=2)
        plaza.tube(foot + Vector((0, 0, 8.6)), foot + Vector((0, 0, 9.8)), 0.6, 6, 0.8, material=3)
    plaza.done("plaza gate halls and lamps", ["structure", "ledGold", "steel", "ledWhite"], coll)
    fins = Parts()
    for k in range(8):                                            # keel fins between the engines, into the underside
        a = k * 45.0
        fins.box(polar(120.0, a, -68.0), (2.4, 70.0, 14.0), yaw=a, pitch=-10.0)
        fins.tube(polar(232.0, a, -18.0), polar(232.0, a, -64.0), 0.9, 6, 0.15)       # an antenna hung from the rim
    fins.done("keel fins and antennas", ["hull2"], coll)


# ---------------------------------------------------------------- 7. the city below and around
BODY = ("towerA", "towerB", "towerC")
GLOW = ("ledBlue", "ledMagenta", "holo")
FOOT = -760.0


def shaft(name, x, y, sides, turn, squash, levels, crown, body, glow, coll):
    """A tower's body as ONE solid: `levels` is [(radius, top)], each a setback with a lit ledge
    under it; `crown` is how far the top tapers. `sides` and `squash` give the plan: 4 a square,
    6 or 8 a faceted one, 16 round; squashed, a slab."""
    prof = [(0, FOOT, body), (levels[0][0], FOOT, body)]
    for k, (r, top) in enumerate(levels):
        prof += [(r, top - 7.0, glow), (r + 1.8, top - 6.0, glow), (r + 1.8, top - 3.6, body), (r, top - 2.6, body)]
        nxt = levels[k + 1][0] if k + 1 < len(levels) else r * 0.72
        prof += [(r, top, body), (nxt, top + (1.0 if k + 1 < len(levels) else crown * 0.4), body)]
    last_r, last_top = levels[-1][0] * 0.72, levels[-1][1] + crown * 0.4
    prof += [(last_r * 0.55, last_top + crown * 0.6, glow), (last_r * 0.55, last_top + crown * 0.6 + 2.0, body),
             (0, last_top + crown * 0.6 + 2.0, body)]
    lathe(name, prof, [turn + a for a in circle(sides)], [body, glow], coll, origin=(x, y, 0.0), squash=squash)
    return last_top + crown * 0.6 + 2.0


def tower(parts, signs, at, top, kind, pick, coll, size=1.0, face=None):
    """One tower, its foot far below. `kind` is how it is built, so neighbours differ in more
    than colour: stepped, twinned and bridged, round with a halo, a blade, a tower on a podium."""
    x, y = at
    body, glow = BODY[pick % 3], GLOW[(pick // 3) % 3]
    turn = random.uniform(0, 90) if face is None else face
    span = top - FOOT
    if kind == 0:                                                 # stepped, square, finned at the corners
        r = random.uniform(40, 54) * size
        levels = [(r, FOOT + span * 0.55), (r * 0.78, FOOT + span * 0.8), (r * 0.56, top)]
        peak = shaft("tower stepped", x, y, 4, turn + 45, (1, 1), levels, 26.0, body, glow, coll)
        for k in range(4):                                        # corner fins, their backs in the body
            c = polar(r * 0.98, turn + 45 + k * 90)
            parts.box((x + c.x, y + c.y, FOOT + span * 0.3), (3.0, 5.0, span * 0.62), yaw=turn + 45 + k * 90, material=0)
        parts.tube((x, y, peak - 2.0), (x, y, peak + 52.0), 1.5, 6, 0.2, material=0)
        f = polar(r * 0.707 - 0.6, turn)                          # a tall sign let into one face
        signs.box((x + f.x, y + f.y, top - 70.0 * size), (r * 0.8, 2.4, 90.0 * size), yaw=turn, material=pick % 2)
    elif kind == 1:                                               # two slabs, one taller, joined by bridges
        r = random.uniform(34, 44) * size
        gap = r * 0.95
        t2 = top - random.uniform(40, 90)
        for sgn, tt in ((-1, top), (1, t2)):
            o = polar(sgn * gap, turn + 90)
            levels = [(r, FOOT + (tt - FOOT) * 0.7), (r * 0.82, tt)]
            peak = shaft("tower twin", x + o.x, y + o.y, 4, turn + 45, (1.0, 0.42) if turn < 45 else (0.42, 1.0),
                         levels, 14.0, body, glow, coll)
            parts.tube((x + o.x, y + o.y, peak - 2.0), (x + o.x, y + o.y, peak + 30.0), 1.1, 6, 0.2, material=0)
        for zz in (t2 - 30.0, t2 - 95.0, t2 - 170.0):             # skybridges, their ends inside both slabs
            a0, a1 = polar(-gap, turn + 90), polar(gap, turn + 90)
            parts.tube((x + a0.x, y + a0.y, zz), (x + a1.x, y + a1.y, zz), 4.2, 6, material=0)
            parts.tube((x + a0.x, y + a0.y, zz - 12.0), (x + a1.x, y + a1.y, zz + 3.0), 0.7, 6, material=0)
    elif kind == 2:                                               # round, banded, a halo ring on spokes
        r = random.uniform(22, 30) * size
        levels = [(r, FOOT + span * (k + 1) / 5) for k in range(5)]
        levels = [(rr * (1.0 - 0.06 * k), tt) for k, (rr, tt) in enumerate(levels)]
        peak = shaft("tower round", x, y, 16, 0, (1, 1), levels, 30.0, body, glow, coll)
        hz = FOOT + span * 0.78
        lathe("tower halo", [(r + 14.0, hz, body), (r + 20.0, hz, glow), (r + 21.0, hz - 1.5, body), (r + 20.0, hz - 4.0, body),
                             (r + 14.0, hz - 4.0, glow), (r + 13.0, hz - 2.0, body)],
              circle(20), [body, glow], coll, origin=(x, y, 0.0))
        for k in range(4):
            c0, c1 = polar(r * 0.6, k * 90 + 20), polar(r + 15.0, k * 90 + 20)
            parts.tube((x + c0.x, y + c0.y, hz - 12.0), (x + c1.x, y + c1.y, hz - 2.0), 0.9, 6, material=0)
        parts.tube((x, y, peak - 2.0), (x, y, peak + 70.0), 1.4, 6, 0.2, material=0)
    elif kind == 3:                                               # a blade: a thin hexagon, a long taper, a sign fin
        r = random.uniform(34, 46) * size
        levels = [(r, FOOT + span * 0.86), (r * 0.8, top)]
        peak = shaft("tower blade", x, y, 6, turn, (0.4, 1.0), levels, 70.0, body, glow, coll)
        o = polar(r * 0.4 * 0.86 + 1.5, 90)
        signs.box((x + o.x, y, top - 110.0 * size), (5.0, r * 0.9, 160.0 * size), material=(pick + 1) % 2)
        parts.tube((x, y, peak - 3.0), (x, y, peak + 40.0), 1.0, 6, 0.2, material=0)
    else:                                                         # a slender tower on a broad podium, a deck on struts
        r = random.uniform(22, 28) * size
        levels = [(r * 2.3, FOOT + span * 0.34), (r, FOOT + span * 0.82), (r * 0.8, top)]
        peak = shaft("tower podium", x, y, 8, turn + 22.5, (1, 1), levels, 22.0, body, glow, coll)
        dz = FOOT + span * 0.6
        d = polar(r + 13.0, turn)
        parts.box((x + d.x, y + d.y, dz), (22.0, 30.0, 1.6), yaw=turn, material=0)       # a landing deck, its back in the body
        for off in (-8.0, 8.0):
            e = polar(r + 24.0, turn) + polar(off, turn + 90)
            g = polar(r * 0.7, turn) + polar(off, turn + 90)
            parts.tube((x + e.x, y + e.y, dz - 0.4), (x + g.x, y + g.y, dz - 22.0), 0.7, 6, material=0)
        for k in range(3):
            c = polar(r * 0.3, turn + k * 120)
            parts.tube((x + c.x, y + c.y, peak - 3.0), (x + c.x, y + c.y, peak + 20.0 + 14.0 * k), 0.8, 6, 0.15, material=0)


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


def city(coll):
    """THE TOWERS ARE PLACED BY SIGHTLINE (owner, 2026-10-05: "the buildings arent really visible
    from the stadium. i need you to think more about sightlines"). From the can the stadium hides
    everything below 25 degrees behind a stand and below 11 degrees in an open corner, so a tower
    only exists for a player if its top clears that. Three rings, each sized from the angle it
    must reach:
      - four HERO towers down the open corners, close, a lit sign turned to the arena;
      - a ring of giants behind the stands, tall enough to stand over the canopies;
      - a far ring filling between them.
    `SIGHT` lines printed at the end say how many degrees of each ring show."""
    parts, signs = Parts(), Parts()
    n = 0
    shown = {"corner": [], "over the stands": [], "far": []}

    def place(bearing, r, top_angle, kind, ring, size, face_in=True):
        nonlocal n
        p = polar(r, bearing)
        top = EYE + r * math.tan(math.radians(top_angle))
        tower(parts, signs, (p.x, p.y), top, kind, n, coll, size=size, face=(bearing + 180.0) if face_in else None)
        shown[ring].append(top_angle - rim_elevation(bearing))
        n += 1

    for c in (45, 135, 225, 315):                                  # the corners: a hero on the axis, a pair behind
        place(c, 400.0, 40.0, (0, 4, 0, 4)[(c // 90) % 4], "corner", 1.5)
        place(c - 10.0, 600.0, 34.0, 3, "corner", 1.3)
        place(c + 10.0, 640.0, 30.0, 2, "corner", 1.4, face_in=False)
    for c in (0, 90, 180, 270):                                    # over the stands: five giants each
        for k, off in enumerate((-24.0, -12.0, 0.0, 12.0, 24.0)):
            place(c + off, 520.0 + 70.0 * ((k * 2) % 3), 33.0 + 5.0 * ((k * 3) % 4) / 3.0, (k + c // 90) % 5,
                  "over the stands", 1.7, face_in=(k % 2 == 0))
    for k in range(28):                                            # the far ring, between and beyond
        b = k * 360.0 / 28 + 6.0
        place(b, random.uniform(900.0, 1500.0), rim_elevation(b) + random.uniform(-6.0, 5.0), k % 5, "far", 2.0, face_in=False)
    parts.done("tower fins, bridges, decks and masts", ["steel"], coll)
    signs.done("tower signs", ["ledMagenta", "holo"], coll)
    lathe("city floor", [(0, FOOT + 6.0, "cityfloor"), (3200.0, FOOT + 6.0, "cityfloor"), (3200.0, FOOT, "cityfloor"),
                         (0, FOOT, "cityfloor")], circle(48), ["cityfloor"], coll)
    for ring, vals in shown.items():
        seen = [v for v in vals if v > 0]
        print("SIGHT %-16s %2d of %2d towers clear the stadium from the can; they show %.0f to %.0f degrees above it" % (
            ring, len(seen), len(vals), min(seen) if seen else 0, max(seen) if seen else 0))


# ---------------------------------------------------------------- checks and pictures
def report():
    """Open edges (a closed solid has none) and triangles, for every mesh in the model."""
    total = 0
    for ob in sorted(bpy.data.objects, key=lambda o: o.name):
        if ob.type != "MESH":
            continue
        bm = bmesh.new(); bm.from_mesh(ob.data)
        open_edges = sum(1 for e in bm.edges if len(e.link_faces) == 1)
        loose = sum(1 for e in bm.edges if len(e.link_faces) == 0)
        tris = sum(len(f.verts) - 2 for f in bm.faces)
        bm.free()
        total += tris
        flag = "" if open_edges == 0 and loose == 0 else "   <-- OPEN %d LOOSE %d" % (open_edges, loose)
        print("MESH %-52s tris %7d%s" % (ob.name[:52], tris, flag))
    print("TOTAL TRIANGLES %d" % total)


def shoot(name, loc, target, lens=35.0):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new(name)
    cam = bpy.data.objects.new(name, cam_data)
    scene.collection.objects.link(cam)
    cam.location = loc
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = lens; cam_data.clip_start = 0.5; cam_data.clip_end = 6000
    scene.camera = cam
    scene.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)


def main():
    version = "v13"
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
    bpy.ops.wm.read_factory_settings()
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob)
    scene = bpy.context.scene
    fx = collection("0 preview only (markers, beams)")
    stage(collection("1 stage"))
    markers(fx)
    field(collection("2 field"))
    c = collection("3 bowls")
    lower_bowl(c); upper_stands(c)
    canopies(collection("4 canopies and floodlights"), fx)
    screens(collection("5 scoreboard and screens"))
    hull(collection("6 hull"))
    city(collection("7 city"))
    report()

    key = bpy.data.objects.new("floodlight key", bpy.data.lights.new("floodlight key", "SUN"))
    key.data.energy = 3.0; key.data.color = (0.86, 0.93, 1.0); key.data.angle = math.radians(25)
    key.rotation_euler = (math.radians(12), math.radians(8), 0)
    scene.collection.objects.link(key)
    world = bpy.data.worlds.new("night"); scene.world = world; world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.010, 0.016, 0.060, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1920, 1080
    scene.view_settings.view_transform = "Standard"
    # A stadium is 500 m across: the viewport's default clipping (0.01 m to 1000 m) is what makes
    # near surfaces flicker against each other from a distance.
    for screen in bpy.data.screens:
        for area in screen.areas:
            for space in area.spaces:
                if space.type == "VIEW_3D":
                    space.clip_start = 1.0; space.clip_end = 8000.0

    os.makedirs(OUT, exist_ok=True)
    for ob in fx.objects:
        ob.hide_viewport = False
    p = "arena_stadium_%s_" % version
    shoot(p + "1_sky", (250, -360, 250), (0, 0, 10), lens=20)
    shoot(p + "2_under", (200, -300, -190), (0, 0, -20), lens=20)
    shoot(p + "3_upper_stand", (0, -168, 56), (0, 10, 6), lens=22)
    shoot(p + "4_player", (0, -9, 3.6), (0, 60, 22), lens=18)
    shoot(p + "5_corner", (96, -96, 31), (-40, 60, 12), lens=18)
    for label, b in (("north", 0), ("north_east", 45), ("east", 90), ("south_east", 135)):
        t = polar(100.0, b, EYE + 38.0)
        shoot(p + "eye_" + label, (0, 0, EYE), (t.x, t.y, t.z), lens=14)
    t = polar(100.0, 45, 30.0)
    shoot(p + "eye_turf_north_east", (-40, -40, 1.7), (t.x, t.y, t.z), lens=16)
    shoot(p + "6_stage", (36, -58, 40), (0, 2, 0), lens=30)
    shoot(p + "7_aerial", (170, -270, 300), (0, 10, 0), lens=28)
    scene.render.engine = "BLENDER_WORKBENCH"                      # flat grey: how the meshes join, with no lighting to hide it
    scene.display.shading.light = "STUDIO"; scene.display.shading.color_type = "MATERIAL"
    shoot(p + "8_solid_stand", (150, -40, 50), (120, 40, 30), lens=28)
    shoot(p + "10_solid_outside", (300, -300, 60), (150, -150, 20), lens=30)
    shoot(p + "9_solid_canopy", (215, -60, 75), (170, 0, 62), lens=35)
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
    for ob in fx.objects:
        if "beams" in ob.name:
            ob.hide_viewport = True
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "arena_stadium.blend"))
    print("STADIUM_OK")


if __name__ == "__main__":
    main()
