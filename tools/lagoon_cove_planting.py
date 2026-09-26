"""Lagoon Court blockout planting: coconut palms, grass tufts, broad leaves and flowering bushes.

  import lagoon_cove_planting as P
  P.palm(c, rng, x, y, z, height=None, lean=None)
  P.tuft(c, rng, x, y, z, scale=1.0)
  P.broadleaf(c, rng, x, y, z, scale=1.0)
  P.flower_bush(c, rng, x, y, z, scale=1.0)
  n = P.plant_gaps(c, rng, spots, height_fn, avoid_fn=None)

Still a BLOCKOUT (the final foliage is step 6 of docs/LAGOON_REWORK_GUIDE.md § 8), but it has to
read as the right plants from 30 to 150 m: the reference (Papaioanou, "Stylized Fishing Village")
has coconut palms leaning out of rock seams, broad leaves, grass tufts and red flowering accents
at the boulder feet. The previous planting was icospheres on sticks, which read as lollipops.

WHY THE MESHES ARE SHARED. A cove carries several hundred plants. Each plant TYPE is built as a
small kit of whole-plant variant meshes (one mesh per variant, several material slots), built once
per file from its own fixed seed, and every plant is ONE object linking one of those meshes with
its own location, turn and scale. So 400 plants cost 400 objects and about 20 meshes, and the
layout script's rng only decides WHERE and WHICH, never the shape of a leaf.

WHY THE COLOUR IS ON THE OBJECT. The house rule (docs/KANTO_DESIGN_GUIDE.md § 5) is two leaf
greens only, varied per PLANT and never per leaf. The leaf slot of every variant is linked to the
object rather than the mesh, so one shared mesh can be light green on one plant and dark on the
next without doubling the kit. The flower colour (crimson or yellow) works the same way.

WHY THE LEAVES ARE FOLDED. A flat card is invisible edge-on, and from the court most fronds are
seen edge-on. Every frond, blade and paddle is a ribbon with a raised midrib and dropped edges
(an inverted V), so from the side it still shows a strip of leaf.

Colours: nothing near offence orange #f87020 or defence blue #0080e8 (Art_Direction.md § 1).
"""
import math
import random

import bmesh
import bpy
from mathutils import Vector

# ---------------------------------------------------------------- materials

COLOURS = {
    "plant_leaf_light": (0.34, 0.62, 0.12),
    "plant_leaf_dark": (0.10, 0.36, 0.09),
    "plant_trunk": (0.46, 0.36, 0.25),
    "plant_trunk_ring": (0.33, 0.25, 0.17),
    "plant_nut": (0.26, 0.17, 0.07),
    "plant_flower_red": (0.80, 0.12, 0.22),
    "plant_flower_yellow": (0.98, 0.80, 0.14),
}
GREENS = ("plant_leaf_light", "plant_leaf_dark")
FLOWERS = ("plant_flower_red", "plant_flower_yellow")


def material(name):
    """A flat Principled BSDF colour. Created once per file, reused by name."""
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    r, g, b = COLOURS[name]
    m.diffuse_color = (r, g, b, 1)
    if m.node_tree is None:
        m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (r, g, b, 1)
    bsdf.inputs["Roughness"].default_value = 0.8
    # Leaves are open ribbons, seen from both sides.
    m.use_backface_culling = False
    return m


# ---------------------------------------------------------------- mesh building

class _Mesh:
    """Accumulates verts and faces with a material slot index per face."""

    def __init__(self):
        self.verts, self.faces, self.mats = [], [], []

    def add(self, verts, faces, slot):
        base = len(self.verts)
        self.verts += [tuple(v) for v in verts]
        self.add_faces(base, faces, slot)
        return base

    def add_faces(self, base, faces, slot):
        for f in faces:
            self.faces.append(tuple(base + i for i in f))
            self.mats.append(slot)

    def build(self, name, slots, smooth_slots=()):
        me = bpy.data.meshes.new(name)
        me.from_pydata(self.verts, [], self.faces)
        for s in slots:
            me.materials.append(material(s))
        for p, m in zip(me.polygons, self.mats):
            p.material_index = m
            p.use_smooth = m in smooth_slots
        me.validate()
        me.update()
        return me


def _ribbon(mesh, slot, spine, widths, fold=0.35, teeth=0.0):
    """A folded leaf along a spine. Each section is left edge, raised midrib, right edge; the
    edges sit BELOW the midrib by fold * width, an inverted V, so the leaf keeps a visible strip
    when seen edge-on. teeth > 0 pulls every other section's edges in, which gives a palm frond
    its stylized leaflet notches without per-leaflet geometry."""
    verts, faces = [], []
    n = len(spine)
    for i, p in enumerate(spine):
        t = (spine[min(i + 1, n - 1)] - spine[max(i - 1, 0)]).normalized()
        side = t.cross(Vector((0, 0, 1)))
        if side.length < 1e-4:
            side = Vector((1, 0, 0))
        side.normalize()
        up = side.cross(t).normalized()
        if up.z < 0:
            up = -up
        w = widths[i] * (1.0 - teeth if (teeth and i % 2 == 1 and 0 < i < n - 1) else 1.0)
        verts += [p + side * w - up * fold * w, p + up * 0.012, p - side * w - up * fold * w]
    for i in range(n - 1):
        a, b = i * 3, (i + 1) * 3
        faces += [(a, b, b + 1, a + 1), (a + 1, b + 1, b + 2, a + 2)]
    mesh.add(verts, faces, slot)


def _arc(start, azimuth, pitch0, pitch1, length, steps, bend=1.0):
    """A spine leaving start toward azimuth, its pitch sweeping from pitch0 to pitch1 (radians,
    up positive). bend > 1 keeps it straight longer and droops it late, like a frond's tip."""
    pts = [Vector(start)]
    ds = length / steps
    for k in range(steps):
        t = (k + 0.5) / steps
        pitch = pitch0 + (pitch1 - pitch0) * (t ** bend)
        d = Vector((math.cos(azimuth) * math.cos(pitch), math.sin(azimuth) * math.cos(pitch), math.sin(pitch)))
        pts.append(pts[-1] + d * ds)
    return pts


def _tube(mesh, slot, path, radii, sides=6, ring_slot=None, ring_every=0):
    """A swept tube along path. With ring_slot, every ring_every-th band takes the ring material,
    a darker collar: the stylized leaf-scar rings of a coconut trunk."""
    verts, faces, rings = [], [], []
    n = len(path)
    prev_side = None
    for i, p in enumerate(path):
        t = (path[min(i + 1, n - 1)] - path[max(i - 1, 0)]).normalized()
        side = t.cross(Vector((0, 1, 0))) if prev_side is None else prev_side - t * prev_side.dot(t)
        if side.length < 1e-4:
            side = t.cross(Vector((1, 0, 0)))
        side.normalize()
        prev_side = side
        other = t.cross(side).normalized()
        for k in range(sides):
            a = math.tau * k / sides
            verts.append(p + (side * math.cos(a) + other * math.sin(a)) * radii[i])
    for i in range(n - 1):
        collar = ring_slot is not None and ring_every and i % ring_every == ring_every - 1
        for k in range(sides):
            a, b = i * sides + k, i * sides + (k + 1) % sides
            f = (a, b, b + sides, a + sides)
            (rings if collar else faces).append(f)
    top = len(verts)
    verts.append(path[-1])
    for k in range(sides):
        faces.append(((n - 1) * sides + k, (n - 1) * sides + (k + 1) % sides, top))
    base = mesh.add(verts, faces, slot)
    if rings:
        mesh.add_faces(base, rings, ring_slot)


def _ico(mesh, slot, centre, radius, squash=(1, 1, 1), subdiv=1, jitter=0.0, rng=None):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius)
    for v in bm.verts:
        v.co = Vector((v.co.x * squash[0], v.co.y * squash[1], v.co.z * squash[2]))
        if jitter and rng:
            v.co += Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))) * jitter * radius
        v.co += Vector(centre)
    mesh.add([v.co.copy() for v in bm.verts], [[v.index for v in f.verts] for f in bm.faces], slot)
    bm.free()


# ---------------------------------------------------------------- variant kits

_KITS = {}


def _kit(kind, count, builder):
    """The shared meshes for one plant type, built once per file. Cached by name in bpy.data so
    a factory reset (which empties bpy.data) simply rebuilds them."""
    names = [f"plant_{kind}_{i}" for i in range(count)]
    meshes = [bpy.data.meshes.get(n) for n in names]
    if any(m is None for m in meshes):
        meshes = [builder(i, names[i]) for i in range(count)]
    return meshes


# Palm variants: (height, lean in degrees). The whole plant is authored leaning toward +X and
# turned to its heading at placement; the height is matched by a uniform scale of at most about
# 12 per cent, so a frond never grows past 4 m or shrinks under 3.
PALM_VARIANTS = [(6.5, 12), (7.5, 24), (8.5, 16), (9.5, 28), (10.5, 20), (11.0, 11)]


def _palm_mesh(i, name):
    rng = random.Random(4100 + i)
    height, lean_deg = PALM_VARIANTS[i]
    m = _Mesh()
    # TRUNK. Leans out at the base and curves back toward upright near the crown, the way a
    # coconut palm grows out of a seam toward the light. The chord from base to crown makes
    # lean_deg with the vertical; the base tangent is steeper, the top tangent gentler.
    offset = height * math.tan(math.radians(lean_deg))
    steps = 14
    path, radii = [], []
    for k in range(steps + 1):
        t = k / steps
        x = offset * (0.65 * (2 * t - t * t) + 0.35 * t)
        path.append(Vector((x, 0, height * t)))
        flare = 0.16 * max(0.0, 1 - t * 6)                  # root flare in the lowest metre
        radii.append(0.27 - 0.11 * t + flare)
    path[0].z -= 0.35                                       # sunk into the ground
    _tube(m, 0, path, radii, sides=7, ring_slot=1, ring_every=2)
    top = path[-1]
    tangent = (path[-1] - path[-2]).normalized()
    # CROWN. Fronds leave the top arching up, then droop past horizontal: the silhouette that
    # separates a coconut palm from a lollipop (a ball) or a star (straight spikes).
    n = rng.randint(7, 9)
    phase = rng.uniform(0, math.tau)
    for k in range(n):
        az = phase + math.tau * k / n + rng.uniform(-0.2, 0.2)
        length = rng.uniform(3.2, 4.0)
        # Two tiers: every other frond leaves lower and hangs further, so the crown is a
        # shaggy umbrella and not a flat star.
        up0 = math.radians(rng.uniform(28, 42) - (16 if k % 2 else 0))
        length = rng.uniform(3.5, 4.0) if k % 2 else length
        up1 = math.radians(rng.uniform(-88, -62))
        # Fronds on the lean side droop a little more, so the crown hangs over the lean.
        if math.cos(az) > 0.3:
            up1 -= math.radians(10)
        spine = _arc(top + tangent * 0.15, az, up0, up1, length, 10, bend=1.15)
        widths = [0.07 + 0.38 * math.sin(math.pi * min(1.0, (j / 10) ** 0.8)) for j in range(11)]
        widths[-1] = 0.02
        _ribbon(m, 2, spine, widths, fold=0.45, teeth=0.35)
    # Two young fronds nearly upright in the middle, so the crown has a top and not a hole.
    for k in range(2):
        az = phase + math.pi * k + 0.6
        spine = _arc(top + tangent * 0.2, az, math.radians(72), math.radians(40), 2.0, 5)
        widths = [0.05, 0.16, 0.22, 0.2, 0.12, 0.02]
        _ribbon(m, 2, spine, widths, fold=0.5)
    # A small dark nut cluster tucked under the crown.
    for k in range(rng.randint(3, 5)):
        a = rng.uniform(0, math.tau)
        c = top + Vector((math.cos(a) * 0.32, math.sin(a) * 0.32, rng.uniform(-0.55, -0.3)))
        _ico(m, 3, c, rng.uniform(0.15, 0.2), subdiv=1)
    return m.build(name, ["plant_trunk", "plant_trunk_ring", "plant_leaf_light", "plant_nut"],
                   smooth_slots=(0, 1))


def _tuft_mesh(i, name):
    rng = random.Random(4200 + i)
    m = _Mesh()
    n = rng.randint(5, 9)
    phase = rng.uniform(0, math.tau)
    for k in range(n):
        az = phase + math.tau * k / n + rng.uniform(-0.3, 0.3)
        length = rng.uniform(0.7, 1.25)
        start = Vector((math.cos(az) * 0.06, math.sin(az) * 0.06, -0.12))
        spine = _arc(start, az, math.radians(rng.uniform(62, 82)), math.radians(rng.uniform(5, 30)), length, 4)
        w0 = rng.uniform(0.06, 0.085)
        widths = [w0, w0 * 0.95, w0 * 0.75, w0 * 0.45, 0.008]
        _ribbon(m, 0, spine, widths, fold=0.6)
    return m.build(name, ["plant_leaf_light"])


def _broadleaf_mesh(i, name):
    rng = random.Random(4300 + i)
    m = _Mesh()
    banana = i % 2 == 0
    stem_base = Vector((0, 0, -0.1))
    if banana:
        # A banana's short fat pseudo-stem; the leaves leave from its top.
        h = rng.uniform(0.7, 1.0)
        path = [Vector((0, 0, -0.15)), Vector((0, 0, h * 0.5)), Vector((0, 0, h))]
        _tube(m, 0, path, [0.16, 0.13, 0.11], sides=6)
        stem_base = Vector((0, 0, h - 0.05))
    n = rng.randint(4, 6)
    phase = rng.uniform(0, math.tau)
    for k in range(n):
        az = phase + math.tau * k / n + rng.uniform(-0.25, 0.25)
        stem_len = rng.uniform(0.35, 0.6) if banana else rng.uniform(0.7, 1.1)
        stem = _arc(stem_base, az, math.radians(rng.uniform(62, 78)), math.radians(rng.uniform(50, 65)),
                    stem_len, 3)
        _tube(m, 0, stem, [0.045, 0.04, 0.035, 0.03], sides=4)
        # The paddle: a long blunt oval, rising off the stem and arching down at the tip.
        length = rng.uniform(1.2, 1.6) if banana else rng.uniform(0.8, 1.1)
        spine = _arc(stem[-1], az, math.radians(rng.uniform(35, 55)), math.radians(rng.uniform(-40, -15)),
                     length, 8, bend=1.3)
        wmax = length * (0.24 if banana else 0.36)
        widths = [wmax * (0.25 + 0.75 * math.sin(math.pi * min(1.0, 0.12 + j / 8 * 0.88)) ** 0.6)
                  for j in range(9)]
        widths[-1] = wmax * 0.12
        _ribbon(m, 0, spine, widths, fold=0.3)
    return m.build(name, ["plant_leaf_light"])


def _bush_mesh(i, name):
    rng = random.Random(4400 + i)
    m = _Mesh()
    # The body: a few low-poly lumps packed into a dome, flat shaded so it stays blocky.
    lumps = []
    for k in range(rng.randint(4, 6)):
        a = rng.uniform(0, math.tau)
        d = 0.0 if k == 0 else rng.uniform(0.35, 0.6)
        r = rng.uniform(0.62, 0.78) if k == 0 else rng.uniform(0.42, 0.58)
        c = Vector((math.cos(a) * d, math.sin(a) * d, r * 0.75 - 0.1 + (0.2 if k == 0 else 0)))
        lumps.append((c, r))
        _ico(m, 0, c, r, squash=(1, 1, 0.85), subdiv=2, jitter=0.06, rng=rng)
    # Flower clumps dotted over the upper half of the dome, sitting on the surface.
    for k in range(rng.randint(26, 34)):
        c, r = lumps[rng.randrange(len(lumps))]
        a = rng.uniform(0, math.tau)
        el = rng.uniform(0.1, 1.2)
        d = Vector((math.cos(a) * math.cos(el), math.sin(a) * math.cos(el), math.sin(el) * 0.85))
        p = c + d * r * 0.98
        if any((p - c2).length < r2 * 0.85 for c2, r2 in lumps if c2 is not c):
            continue   # buried inside a neighbouring lump
        _ico(m, 1, p, rng.uniform(0.13, 0.19), subdiv=1)
    return m.build(name, ["plant_leaf_light", "plant_flower_red"])


# ---------------------------------------------------------------- placement

def _place(c, kind, mesh, x, y, z, scale, heading, slots):
    """One plant = one object linking a shared mesh. slots maps a material slot index to the
    material name this plant wears there (per-plant colour on a shared mesh)."""
    o = bpy.data.objects.new(kind, mesh)
    o.location = (x, y, z)
    o.rotation_euler = (0, 0, heading)
    o.scale = (scale, scale, scale)
    c.objects.link(o)
    for idx, mat_name in slots.items():
        slot = o.material_slots[idx]
        slot.link = "OBJECT"
        slot.material = material(mat_name)
    return o


def palm(c, rng, x, y, z, height=None, lean=None):
    """A coconut palm, 6 to 11 m, trunk curving out toward heading `lean` (radians; None =
    random), a drooping crown of 7 to 9 folded fronds and a nut cluster."""
    kit = _kit("palm", len(PALM_VARIANTS), _palm_mesh)
    if height is None:
        height = rng.uniform(6.0, 11.0)
    height = max(5.5, min(12.0, height))
    idx = min(range(len(PALM_VARIANTS)), key=lambda k: abs(PALM_VARIANTS[k][0] - height) + rng.uniform(0, 0.9))
    heading = rng.uniform(0, math.tau) if lean is None else lean
    return _place(c, "palm", kit[idx], x, y, z, height / PALM_VARIANTS[idx][0], heading,
                  {2: rng.choice(GREENS)})


def tuft(c, rng, x, y, z, scale=1.0):
    """A grass tuft: 5 to 9 folded blades fanning out, 0.6 to 1.2 m."""
    kit = _kit("tuft", 4, _tuft_mesh)
    return _place(c, "tuft", kit[rng.randrange(len(kit))], x, y, z, scale * rng.uniform(0.85, 1.15),
                  rng.uniform(0, math.tau), {0: rng.choice(GREENS)})


def broadleaf(c, rng, x, y, z, scale=1.0):
    """A banana or taro style plant: 4 to 6 big paddle leaves on stems, 1.5 to 3 m."""
    kit = _kit("broadleaf", 4, _broadleaf_mesh)
    return _place(c, "broadleaf", kit[rng.randrange(len(kit))], x, y, z, scale * rng.uniform(0.9, 1.3),
                  rng.uniform(0, math.tau), {0: rng.choice(GREENS)})


def flower_bush(c, rng, x, y, z, scale=1.0, flower=None):
    """A rounded low bush, 1 to 1.8 m, dotted with crimson (mostly) or yellow flower clumps.
    flower: 'plant_flower_red' or 'plant_flower_yellow'; None picks, red three times in four."""
    kit = _kit("bush", 4, _bush_mesh)
    if flower is None:
        flower = FLOWERS[0] if rng.random() < 0.75 else FLOWERS[1]
    return _place(c, "flower bush", kit[rng.randrange(len(kit))], x, y, z, scale * rng.uniform(0.9, 1.25),
                  rng.uniform(0, math.tau), {0: rng.choice(GREENS), 1: flower})


# The gap mix, as cumulative weights: mostly tufts and broad leaves, some flowering bushes, the
# occasional palm leaning out of the seam.
GAP_MIX = (("tuft", 0.42), ("broadleaf", 0.30), ("flower_bush", 0.18), ("palm", 0.10))


def plant_gaps(c, rng, spots, height_fn, avoid_fn=None, density=0.55, per_spot=(1, 3), mix=GAP_MIX, ring=0.9,
               scale=1.0):
    """Plants in the gap at boulder feet. spots: (x, y, radius) footprints. A fraction `density`
    of spots gets per_spot[0] to per_spot[1] plants on a ring at about ring * radius; each point is
    rejected when avoid_fn(x, y) is True. Palms lean OUTWARD, away from the spot centre. Every
    base is set at the LOWEST ground within its footprint, so no plant floats on a slope.
    `scale` sizes the low plants (not palms) to the stones they sit among. Returns how many
    plants were placed."""
    total = sum(w for _k, w in mix)
    placed = 0
    for sx, sy, r in spots:
        if rng.random() > density:
            continue
        for _ in range(rng.randint(*per_spot)):
            a = rng.uniform(0, math.tau)
            d = r * ring * rng.uniform(0.92, 1.08)
            x, y = sx + math.cos(a) * d, sy + math.sin(a) * d
            if avoid_fn is not None and avoid_fn(x, y):
                continue
            roll, kind = rng.uniform(0, total), mix[-1][0]
            for k, w in mix:
                if roll < w:
                    kind = k
                    break
                roll -= w
            foot = 0.9 if kind == "palm" else 0.5
            z = min(height_fn(x + dx * foot, y + dy * foot) for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)))
            if kind == "palm":
                palm(c, rng, x, y, z, lean=a + rng.uniform(-0.35, 0.35))
            elif kind == "tuft":
                tuft(c, rng, x, y, z, scale=scale * rng.uniform(0.8, 1.2))
            elif kind == "broadleaf":
                broadleaf(c, rng, x, y, z, scale=scale)
            else:
                flower_bush(c, rng, x, y, z, scale=scale)
            placed += 1
    return placed
