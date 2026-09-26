"""Paete's skill props, modelled part by part: the sentry, the seedling and the thorn construct.

    python tools/build_paete_props.py

⚠️⚠️ WHY THESE ARE MODELS NOW (owner, 2026-09-26): *"the current models of all his skills look ugly
still its js blocks"*, *"they arent very detailed"*, *"thoroughly work on the detail of each part
manually instead of just generating it as a whole"*. Until this file the props were Unity cubes and
code tubes in flat colours with no ink, built at runtime in `PaeteVfx.cs`. That is not how anything
else in the cast is made. Paete himself is typed part by part in `tools/build_paete_voxel.py`, and
these props are made the same way and from the same helpers: chamfered bark blocks
(`box_polygons`), six-sided prism leaves (`_leaf`), smoothed normals so the inverted-hull ink closes
(`smooth_normals`), and HIS palette through the atlas cells (`cell_uv`), so they wear his bark, moss,
leaf and eye-glow exactly and get the toon shading and the ink outline at runtime (`ToonSkin.Apply`
with his palette).

⚠️⚠️ v2 (same evening), THE DIRECTION IS `docs/reports/paete-kit-2026-09-25/direction.md` § 5. The
owner on v1: *"wghat are the yellow shit"* (glow seams on the bark: light belongs to his eyes, palms
and the core only, so they are gone), *"the vines kinda look bad why do they just twirl there for no
reason"* (even corkscrews round the bundle: every vine now climbs FROM somewhere TO somewhere), and a
crop of Groot's limb, *"make it look likle this or smth"*: a rope of MANY cords woven over and under.
So the sentry's trunk is laid like a real rope: three plies of four cords, the plies twisting one way
and the cords inside each ply the other way, each cord its own girth and shade. Low poly (five
sides), so it stays in the blocky cast. And it has HIS face, because the ultimate is the mountain
waking up: brow planks, sunken sockets, eyes, eyelids and brows as their own nodes so it can wake,
look, blink and sleep.

⚠️ EVERY PART IS TYPED, NOT STAMPED. The owner's standing rule for Paete (*"do it one by one dont try
to mass generate it"*). Where a prop has twelve cords, five branches or seven thorns, each has its
own row of numbers below; nothing is one shape repeated round a circle with the same size.

⚠️ THE PARTS THAT MOVE ARE NAMED NODES, AUTHORED ALONG THEIR OWN +Z WITH A REST YAW. A crown branch,
a root, a petal or a thorn is built pointing down its node's +Z from the node's origin (its pivot),
and the node carries the yaw that aims it. The runtime keeps each node's imported rotation as its
rest and poses `rest * Euler(pitch, 0, 0)`, so a positive pitch always tips a part outward and down
whatever direction it faces, and the glTF-to-Unity X flip (glTFast negates X) cannot turn it inside
out: the rest absorbs it.

Output: Assets/TumbangPreso/Resources/Models/PaeteProps/{sentry,seedling,thorns}.glb, metres, +Y up,
the face on +Z. Units are the game's own (no PersonScale): these stand on the court at their real size.
"""
import math
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_paete_voxel as pv  # noqa: E402  (his builder: the same helpers and palette cells)

ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Models", "PaeteProps")
DONOR = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "characters", "persons", "team-paete.glb")

MOSS, MOSS_DARK, LEAF, LEAF_DARK = pv.MOSS, pv.MOSS_DARK, pv.LEAF, pv.LEAF_DARK
VINE, HEARTWOOD, ROOTC, MOSS_LIT = pv.VINE, pv.HEARTWOOD, pv.ROOT, pv.MOSS_LIT
INK, SOCKET, EYE, EYE_GLOW, FLOWER = pv.INK, pv.SOCKET, pv.EYE, pv.EYE_GLOW, pv.FLOWER_HEART
BARK, BARK_DARK, BARK_LIT = pv.BARK, pv.BARK_DARK, pv.BARK_LIT


# ---------------------------------------------------------------------------------------------
# Geometry.
# ---------------------------------------------------------------------------------------------
def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def mul(a, s): return (a[0] * s, a[1] * s, a[2] * s)
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def unit(a):
    n = math.sqrt(dot(a, a)) or 1.0
    return (a[0] / n, a[1] / n, a[2] / n)


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def tube(path, radii, sides=5, twist=0.0):
    """A tapered faceted tube along `path`, one radius per point, capped.

    ⚠️ PARALLEL-TRANSPORTED FRAME, NOT HIS `_branch`'S PER-POINT HELPER. `_branch` picks a fresh side
    vector at every ring and swaps its helper axis when the tangent passes 0.9 of +Z, which is fine for
    his short straight limbs and wrong for a cord winding round a rope: the rings turned against each
    other and the facets sheared into a candy twist. Carrying the side vector from ring to ring keeps
    every facet a straight strip, so a five-sided cord reads as a squared cord. `twist` (degrees over
    the whole length) turns the facets on purpose, which is how a laid cord looks.
    """
    n = len(path)
    rings, tangents = [], []
    side = None
    for i in range(n):
        t = unit(sub(path[min(n - 1, i + 1)], path[max(0, i - 1)]))
        if side is None:
            helper = (0.0, 1.0, 0.0) if abs(t[1]) < 0.9 else (1.0, 0.0, 0.0)
            side = unit(cross(t, helper))
        else:
            side = unit(sub(side, mul(t, dot(side, t))))
        up = cross(t, side)
        a0 = math.radians(twist) * i / max(1, n - 1)
        ring = []
        for j in range(sides):
            a = a0 + j * math.tau / sides
            ring.append(add(path[i], add(mul(side, radii[i] * math.cos(a)), mul(up, radii[i] * math.sin(a)))))
        rings.append(ring); tangents.append(t)
    faces = []
    for i in range(n - 1):
        mid = pv._rafi_mean([path[i], path[i + 1]])
        for j in range(sides):
            k = (j + 1) % sides
            quad = [rings[i][j], rings[i][k], rings[i + 1][k], rings[i + 1][j]]
            faces.append(pv._rafi_orient(quad, sub(pv._rafi_mean(quad), mid)))
    faces.append(pv._rafi_orient(rings[0], mul(tangents[0], -1)))
    faces.append(pv._rafi_orient(rings[-1], tangents[-1]))
    return faces


def ring(r, a_deg, y):
    """A point at radius r, compass angle a (0 = +Z, the face side; 90 = +X), height y."""
    a = math.radians(a_deg)
    return (r * math.sin(a), y, r * math.cos(a))


def lerp(a, b, t): return a + (b - a) * t


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


# ---------------------------------------------------------------------------------------------
# A tiny scene: named nodes, each a list of (slot, faces) in the node's own space.
# ---------------------------------------------------------------------------------------------
class Node:
    def __init__(self, name, origin=(0.0, 0.0, 0.0), parent=None, yaw=0.0, scale=1.0):
        self.name, self.origin, self.parent, self.yaw, self.scale = name, origin, parent, yaw, scale
        self.parts = []   # (slot, [ [points...], ... ])

    def box(self, lo, hi, slot, bevel=None):
        bevel = pv.bevel_for(lo, hi) if bevel is None else bevel
        self.parts.append((slot, [pts for _, pts in pv.box_polygons(lo, hi, -1, bevel)]))

    def obox(self, centre, size, slot, yaw=0.0, pitch=0.0, roll=0.0, bevel=None):
        """A box of `size` turned by yaw/pitch/roll (his `_rotate` order) and placed at `centre`."""
        w, h, d = size
        bevel = pv.bevel_for((0, 0, 0), size) if bevel is None else bevel
        faces = [pts for _, pts in pv.box_polygons((-w / 2, -h / 2, -d / 2), (w / 2, h / 2, d / 2), -1, bevel)]
        self.parts.append((slot, [[add(centre, pv._rotate(p, yaw, pitch, roll)) for p in f] for f in faces]))

    def tube(self, path, radii, slot, sides=5, twist=0.0):
        self.parts.append((slot, tube(path, radii, sides, twist)))

    def leaf(self, centre, length, width, yaw, pitch, roll=0.0, slot=LEAF, thickness=0.014):
        self.parts.append((slot, pv._leaf(centre, length, width, thickness, yaw, pitch, roll)))


def _normal(points):
    n = cross(sub(points[1], points[0]), sub(points[2], points[0]))
    length = math.sqrt(dot(n, n))
    return (0.0, 1.0, 0.0) if length < 1e-12 else tuple(v / length for v in n)


def _mesh(node):
    pos, nrm, uv, idx = [], [], [], []
    for slot, faces in node.parts:
        u, v = pv.cell_uv(slot)
        for points in faces:
            n = _normal(points)
            first = len(pos)
            for p in points:
                pos.append(p); nrm.append(n); uv.append((u, v))
            for k in range(1, len(points) - 1):
                idx.extend((first, first + k, first + k + 1))
    nrm = pv.smooth_normals(pos, nrm)
    return pos, nrm, uv, idx


def write(path, nodes):
    donor, dblob = pv.read_glb(DONOR)
    blob = bytearray()
    views, accessors, meshes, gnodes = [], [], [], []

    def view(data, target=None):
        while len(blob) % 4: blob.append(0)
        offset = len(blob); blob.extend(data)
        v = {"buffer": 0, "byteOffset": offset, "byteLength": len(data)}
        if target: v["target"] = target
        views.append(v); return len(views) - 1

    # The atlas and the material come from his own model, so the palette cells mean what they mean on him.
    image = donor["images"][0]
    if "uri" in image:
        import shutil
        source = os.path.join(os.path.dirname(DONOR), image["uri"])
        target = os.path.join(os.path.dirname(path), image["uri"])
        os.makedirs(os.path.dirname(target), exist_ok=True)
        shutil.copyfile(source, target)
        images = [{"uri": image["uri"], "name": image.get("name", "colormap")}]
    else:
        iv = donor["bufferViews"][image["bufferView"]]
        start = iv.get("byteOffset", 0)
        images = [{"bufferView": view(bytes(dblob[start:start + iv["byteLength"]])), "mimeType": image.get("mimeType", "image/png")}]

    index_of = {}
    for node in nodes:
        index_of[node.name] = len(gnodes)
        entry = {"name": node.name, "translation": list(node.origin)}
        if node.yaw:
            h = math.radians(node.yaw) * 0.5
            entry["rotation"] = [0.0, math.sin(h), 0.0, math.cos(h)]
        if node.scale != 1.0:
            entry["scale"] = [node.scale] * 3
        if node.parts:
            pos, nrm, uv, idx = _mesh(node)
            lo = [min(p[k] for p in pos) for k in range(3)]; hi = [max(p[k] for p in pos) for k in range(3)]
            a_pos = len(accessors); accessors.append({"bufferView": view(struct.pack(f"<{len(pos)*3}f", *[c for p in pos for c in p]), 34962),
                                                      "componentType": 5126, "count": len(pos), "type": "VEC3", "min": lo, "max": hi})
            a_nrm = len(accessors); accessors.append({"bufferView": view(struct.pack(f"<{len(nrm)*3}f", *[c for p in nrm for c in p]), 34962),
                                                      "componentType": 5126, "count": len(nrm), "type": "VEC3"})
            a_uv = len(accessors); accessors.append({"bufferView": view(struct.pack(f"<{len(uv)*2}f", *[c for p in uv for c in p]), 34962),
                                                     "componentType": 5126, "count": len(uv), "type": "VEC2"})
            a_idx = len(accessors); accessors.append({"bufferView": view(struct.pack(f"<{len(idx)}I", *idx), 34963),
                                                      "componentType": 5125, "count": len(idx), "type": "SCALAR"})
            meshes.append({"name": node.name, "primitives": [{"attributes": {"POSITION": a_pos, "NORMAL": a_nrm, "TEXCOORD_0": a_uv},
                                                              "indices": a_idx, "material": 0}]})
            entry["mesh"] = len(meshes) - 1
        gnodes.append(entry)
    for node in nodes:
        if node.parent is not None:
            gnodes[index_of[node.parent]].setdefault("children", []).append(index_of[node.name])
    roots = [index_of[n.name] for n in nodes if n.parent is None]
    gltf = {
        "asset": {"version": "2.0", "generator": "tools/build_paete_props.py"},
        "scene": 0, "scenes": [{"nodes": roots}], "nodes": gnodes, "meshes": meshes,
        "materials": [donor["materials"][0]],
        "textures": [{"sampler": 0, "source": 0}], "samplers": [donor["samplers"][0]] if donor.get("samplers") else [{}],
        "images": images, "buffers": [{"byteLength": len(blob)}],
        "bufferViews": views, "accessors": accessors,
    }
    mat = gltf["materials"][0]
    pbr = mat.get("pbrMetallicRoughness", {})
    if "baseColorTexture" in pbr: pbr["baseColorTexture"] = {"index": 0}
    for key in ("normalTexture", "occlusionTexture", "emissiveTexture"): mat.pop(key, None)
    pv.write_glb(path, gltf, blob)
    tris = sum(len(_mesh(n)[3]) // 3 for n in nodes if n.parts)
    print(f"{os.path.basename(path)}: {len(nodes)} nodes, {tris} triangles")


# ---------------------------------------------------------------------------------------------
# Typed-point helpers. The only arithmetic between typed points is a Catmull-Rom spline.
# ---------------------------------------------------------------------------------------------
def spline(points, per=5):
    """A smooth path through the typed points (Catmull-Rom), `per` samples per span."""
    out = []
    n = len(points)
    for i in range(n - 1):
        p0, p1, p2, p3 = points[max(0, i - 1)], points[i], points[i + 1], points[min(n - 1, i + 2)]
        for s in range(per):
            t = s / per
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[k]) + (-p0[k] + p2[k]) * t + (2 * p0[k] - 5 * p1[k] + 4 * p2[k] - p3[k]) * t2
                                    + (-p0[k] + 3 * p1[k] - 3 * p2[k] + p3[k]) * t3) for k in range(3)))
    out.append(points[-1])
    return out


def spread(values, per=5):
    """The typed radii stretched over the spline's samples, linearly per span."""
    out = []
    for i in range(len(values) - 1):
        for s in range(per):
            out.append(lerp(values[i], values[i + 1], s / per))
    out.append(values[-1])
    return out


def cord(node, keys, slot, sides=5, twist=0.0, per=5):
    """One cord from typed keys: (height, compass angle round the trunk, distance out, girth)."""
    pts = [ring(r, a, y) for y, a, r, _ in keys]
    node.tube(spline(pts, per), spread([g for _, _, _, g in keys], per), slot, sides, twist)



# The sentry's silhouette (v7, after the owner's cartoon tree): how far out the weave sits at each
# height, as a multiplier on each cord's typed distance. A root flare at the foot, a pinched waist, a
# swollen upper trunk where the eyes are, easing into the crown. Hand-set keys.
SILHOUETTE = [(-0.10, 1.95), (0.35, 1.28), (0.80, 1.00), (1.30, 0.90), (1.80, 1.04), (2.30, 1.24), (2.80, 1.30), (3.28, 1.18), (3.45, 1.10)]


def sil(y):
    for (y0, s0), (y1, s1) in zip(SILHOUETTE, SILHOUETTE[1:]):
        if y <= y1:
            return lerp(s0, s1, smooth((y - y0) / (y1 - y0)))
    return SILHOUETTE[-1][1]


def sring(r, a, y):
    """`ring`, pushed out to the sentry's silhouette at that height."""
    return ring(r * sil(y), a, y)


def scord(node, keys, slot, sides=5, twist=0.0, per=5):
    """`cord` on the sentry's silhouette."""
    cord(node, [(y, a, r * sil(y), g) for y, a, r, g in keys], slot, sides, twist, per)


def line(node, points, radii, slot, sides=5, twist=0.0, per=5):
    """One limb from typed xyz points and typed radii."""
    node.tube(spline(points, per), spread(radii, per), slot, sides, twist)


def sprig(node, base, compass, pitch, length, width, slot, roll=0.0, thickness=0.014):
    """One leaf growing from `base` toward compass angle `compass` (0 = +Z, 90 = +X), tipped by `pitch`
    (negative droops). Its own numbers every call."""
    d = (math.sin(math.radians(compass)) * math.cos(math.radians(pitch)), math.sin(math.radians(pitch)),
         math.cos(math.radians(compass)) * math.cos(math.radians(pitch)))
    centre = add(base, mul(d, length * 0.46))
    node.leaf(centre, length, width, compass - 90.0, pitch, roll, slot=slot, thickness=thickness)


def clump(node, centre, radii, slot, tilt=0.0, turn=0.0, sides=9):
    """One mass of leaves in the crown: a low round lump (the plaza trees' own canopy language, faceted then
    smoothed and inked), `radii` (x, y, z) across, tipped by `tilt` and turned by `turn`. Typed per call: its
    rows are a fixed profile, the size and placement are the caller's."""
    rx, ry, rz = radii
    profile = [(-1.00, 0.18), (-0.62, 0.78), (-0.10, 1.00), (0.42, 0.90), (0.80, 0.56), (1.00, 0.12)]
    rows = [(ry * h, 0.0, 0.0, rx * w, rz * w) for h, w in profile]
    local = Node("_clump")
    loft(local, rows, slot, sides=sides)
    for slot_, faces in local.parts:
        moved = [[add(centre, pv._rotate(p, turn, 0.0, tilt)) for p in face] for face in faces]
        node.parts.append((slot_, moved))


# ---------------------------------------------------------------------------------------------
# THE SENTRY. About 4.4 m to the leaf tips. direction.md section 5.2.
# ---------------------------------------------------------------------------------------------
def sentry():
    root = Node("sentry")
    trunk = Node("trunk", parent="sentry")
    nodes = [root, trunk]

    # --- THE WEAVE (the owner's Groot crop): eight big cords and three thin ones, each typed key by key.
    # Four climb clockwise and four counter-clockwise, so they cross; at each crossing one is OUT (further
    # from the axis) and one is IN, placed by hand, which is what makes it read as woven. The foot flares
    # (the cords parting into the roots) and the shoulder flares (parting into the crown).
    # Keys: (height, compass angle, distance out, girth).
    scord(trunk, [(-0.10, 5, 0.62, 0.148), (0.35, 46, 0.46, 0.141), (0.80, 88, 0.41, 0.135), (1.30, 127, 0.32, 0.131),
                 (1.80, 170, 0.39, 0.133), (2.30, 214, 0.31, 0.130), (2.80, 251, 0.41, 0.128), (3.28, 292, 0.47, 0.114),
                 (3.42, 305, 0.53, 0.080)], BARK_LIT, twist=40)
    scord(trunk, [(-0.10, 97, 0.60, 0.144), (0.35, 138, 0.50, 0.137), (0.80, 177, 0.34, 0.138), (1.30, 221, 0.40, 0.132),
                 (1.80, 262, 0.31, 0.129), (2.30, 300, 0.40, 0.135), (2.80, 343, 0.34, 0.125), (3.28, 381, 0.50, 0.112),
                 (3.40, 392, 0.55, 0.075)], BARK, twist=-30)
    scord(trunk, [(-0.10, 176, 0.63, 0.150), (0.35, 219, 0.43, 0.144), (0.80, 263, 0.42, 0.137), (1.30, 301, 0.33, 0.135),
                 (1.80, 344, 0.37, 0.136), (2.30, 388, 0.31, 0.128), (2.80, 427, 0.43, 0.130), (3.28, 466, 0.46, 0.115),
                 (3.43, 478, 0.52, 0.078)], HEARTWOOD, twist=55)
    scord(trunk, [(-0.10, 268, 0.59, 0.141), (0.35, 311, 0.50, 0.135), (0.80, 350, 0.35, 0.133), (1.30, 395, 0.41, 0.136),
                 (1.80, 436, 0.30, 0.127), (2.30, 474, 0.40, 0.132), (2.80, 513, 0.35, 0.124), (3.28, 556, 0.50, 0.111),
                 (3.41, 566, 0.56, 0.073)], BARK_LIT, twist=-45)
    scord(trunk, [(-0.10, 48, 0.61, 0.146), (0.35, 12, 0.44, 0.140), (0.80, -27, 0.41, 0.136), (1.30, -61, 0.31, 0.130),
                 (1.80, -98, 0.38, 0.135), (2.30, -133, 0.33, 0.129), (2.80, -170, 0.41, 0.127), (3.28, -204, 0.47, 0.113),
                 (3.42, -214, 0.54, 0.076)], BARK, twist=35)
    scord(trunk, [(-0.10, 141, 0.60, 0.148), (0.35, 104, 0.49, 0.138), (0.80, 69, 0.34, 0.135), (1.30, 31, 0.40, 0.137),
                 (1.80, -5, 0.31, 0.128), (2.30, -44, 0.38, 0.131), (2.80, -79, 0.36, 0.125), (3.28, -116, 0.48, 0.114),
                 (3.40, -127, 0.55, 0.079)], BARK_DARK, twist=-50)
    scord(trunk, [(-0.10, 222, 0.62, 0.145), (0.35, 188, 0.43, 0.142), (0.80, 151, 0.42, 0.133), (1.30, 114, 0.32, 0.132),
                 (1.80, 80, 0.39, 0.136), (2.30, 43, 0.32, 0.127), (2.80, 7, 0.42, 0.129), (3.28, -30, 0.46, 0.112),
                 (3.43, -41, 0.53, 0.075)], BARK_LIT, twist=28)
    scord(trunk, [(-0.10, 318, 0.59, 0.142), (0.35, 281, 0.50, 0.136), (0.80, 246, 0.35, 0.137), (1.30, 209, 0.39, 0.133),
                 (1.80, 172, 0.30, 0.129), (2.30, 136, 0.40, 0.133), (2.80, 101, 0.35, 0.123), (3.28, 63, 0.49, 0.113),
                 (3.41, 52, 0.55, 0.074)], BARK, twist=-38)
    # Three thin strands tucked into the gaps: they are what makes it read as MANY cords, like the crop.
    scord(trunk, [(0.05, 70, 0.50, 0.060), (0.60, 96, 0.40, 0.056), (1.20, 150, 0.37, 0.054), (1.75, 205, 0.40, 0.052),
                 (2.20, 238, 0.37, 0.050), (2.75, 280, 0.40, 0.046), (3.25, 330, 0.44, 0.040)], BARK_DARK, sides=4)
    scord(trunk, [(0.10, 250, 0.52, 0.058), (0.65, 222, 0.39, 0.055), (1.15, 175, 0.38, 0.053), (1.70, 128, 0.40, 0.050),
                 (2.25, 86, 0.36, 0.048), (2.70, 40, 0.41, 0.045), (3.20, -8, 0.45, 0.040)], HEARTWOOD, sides=4)
    scord(trunk, [(0.40, 330, 0.44, 0.050), (0.95, 300, 0.40, 0.048), (1.45, 262, 0.39, 0.046), (1.95, 228, 0.40, 0.044),
                 (2.45, 190, 0.38, 0.042), (2.95, 150, 0.42, 0.038)], BARK_LIT, sides=4)

    # --- THE EYES: TWO HOLLOWS IN THE WOOD WITH A SLANTED LIGHT IN EACH (owner, 2026-09-26: *"subtle eyes
    # only"*, *"engraved"*, *"Js make 2 fucking holles"*, then *"look the eyes of these"* and the cartoon
    # tree, *"use this for inspiration"*). What the references share: the eye is a HOLLOW the bark caves
    # into, and the light sits in it as a narrow slit slanting down toward the middle, a dark overhang of
    # wood above it. No brows as parts, no mask: v3 to v5 were laughed off. The swollen upper trunk (v7's
    # silhouette) is where they sit, 2.3 m up, and the light is its own node: the runtime opens it (wake),
    # narrows it (blink) and shuts it (sleep).
    face = Node("face", origin=(0.0, 2.48, 0.585), parent="trunk", scale=1.4); nodes.append(face)

    def pit(node, centre, length, width, depth, slant, slot):
        node.parts.append((slot, pv._leaf(centre, length, width, depth, 0.0, slant, 90.0)))

    pit(face, (-0.19, 0.010, -0.020), 0.250, 0.135, 0.090, -15.0, SOCKET)
    pit(face, (0.195, 0.000, -0.020), 0.245, 0.138, 0.090, 16.0, SOCKET)
    # The overhang: a lip of bark over each hollow, thick on top, typed point by point.
    line(face, [(-0.330, 0.040, 0.020), (-0.262, 0.104, 0.052), (-0.176, 0.098, 0.060), (-0.092, 0.040, 0.050), (-0.060, -0.016, 0.030)],
         [0.016, 0.040, 0.048, 0.034, 0.012], BARK_DARK, per=4)
    line(face, [(0.335, 0.028, 0.020), (0.268, 0.094, 0.052), (0.182, 0.090, 0.060), (0.098, 0.030, 0.050), (0.066, -0.026, 0.030)],
         [0.016, 0.041, 0.048, 0.033, 0.012], BARK_DARK, per=4)
    eyes = Node("eyes", origin=(0.0, 0.004, 0.0), parent="face"); nodes.append(eyes)
    pit(eyes, (-0.188, 0.000, 0.022), 0.170, 0.070, 0.008, -15.0, EYE_GLOW)
    pit(eyes, (0.193, -0.010, 0.022), 0.166, 0.072, 0.008, 16.0, EYE_GLOW)
    pit(eyes, (-0.186, -0.004, 0.029), 0.124, 0.040, 0.010, -15.0, EYE)
    pit(eyes, (0.191, -0.014, 0.029), 0.120, 0.041, 0.010, 16.0, EYE)

    # --- THIN WOVEN BRANCHES, NOT VINES (owner: *"dont use vines use woven tree branches"*), typed point
    # by point, none an even spiral. Each is two twigs laid round each other, in bark, with leaves.
    # (1) out of the road by the front-left root, up the left flank to the shoulder.
    b1a = [sring(1.02, 318, 0.02), sring(0.80, 322, 0.10), sring(0.63, 317, 0.36), sring(0.53, 313, 0.74), sring(0.51, 303, 1.12),
           sring(0.50, 298, 1.58), sring(0.50, 290, 2.06), sring(0.51, 288, 2.52), sring(0.54, 291, 2.96), sring(0.59, 301, 3.30)]
    b1b = [sring(1.00, 314, 0.03), sring(0.79, 316, 0.14), sring(0.61, 322, 0.40), sring(0.54, 308, 0.80), sring(0.50, 309, 1.18),
           sring(0.51, 293, 1.62), sring(0.49, 295, 2.10), sring(0.52, 284, 2.55), sring(0.53, 296, 2.98), sring(0.57, 296, 3.26)]
    line(trunk, b1a, [0.046, 0.046, 0.043, 0.040, 0.038, 0.036, 0.034, 0.031, 0.028, 0.022], BARK_LIT)
    line(trunk, b1b, [0.036, 0.036, 0.034, 0.032, 0.030, 0.028, 0.027, 0.025, 0.022, 0.018], BARK_DARK)
    sprig(trunk, b1a[3], 250, -15, 0.26, 0.15, LEAF_DARK)
    sprig(trunk, b1a[6], 330, 10, 0.22, 0.13, LEAF)
    sprig(trunk, b1a[9], 300, 30, 0.30, 0.17, LEAF)
    sprig(trunk, b1b[9], 250, 5, 0.24, 0.14, LEAF_DARK)
    # (2) the sash, like his own torso band: from the back right, across the chest under the eyes, round
    # to the back left; two branches laid together.
    s_a = [sring(0.49, 125, 1.20), sring(0.52, 78, 1.37), sring(0.54, 32, 1.52), sring(0.56, -6, 1.63), sring(0.55, -44, 1.67),
           sring(0.53, -84, 1.61), sring(0.50, -122, 1.47)]
    s_b = [sring(0.50, 120, 1.26), sring(0.53, 74, 1.33), sring(0.55, 28, 1.58), sring(0.55, -10, 1.59), sring(0.56, -48, 1.72),
           sring(0.52, -88, 1.57), sring(0.49, -118, 1.52)]
    line(trunk, s_a, [0.050, 0.056, 0.058, 0.056, 0.053, 0.046, 0.034], BARK)
    line(trunk, s_b, [0.034, 0.040, 0.042, 0.040, 0.038, 0.032, 0.024], BARK_LIT)
    sprig(trunk, s_a[2], 40, -35, 0.24, 0.14, LEAF)
    sprig(trunk, s_b[4], 320, -40, 0.20, 0.12, LEAF_DARK)
    sprig(trunk, s_a[6], 230, -20, 0.28, 0.16, LEAF_DARK)
    # (3) a short one, round a single cord low on the back, ending in a forked tip.
    b3 = [sring(0.60, 170, 0.05), sring(0.53, 182, 0.30), sring(0.51, 200, 0.52), sring(0.53, 190, 0.70), sring(0.56, 172, 0.78)]
    line(trunk, b3, [0.044, 0.041, 0.037, 0.031, 0.022], BARK_DARK)
    line(trunk, [b3[3], sring(0.60, 196, 0.86), sring(0.63, 205, 0.95)], [0.024, 0.018, 0.010], BARK_DARK, per=3)
    sprig(trunk, b3[4], 150, 15, 0.22, 0.13, LEAF)

    # --- WOVEN VINES (owner: *"add woven vines and branches to it and shit"*). A vine here THREADS the
    # weave: every typed point alternates OVER a cord (further out) and UNDER the next (tucked in), so it
    # is woven through the tree rather than wound round it (v1's corkscrews, *"why do they just twirl
    # there"*). Each climbs from somewhere to somewhere, in his vine greens, leaves at a few joints.
    # (A) from the road by the front-left root, across the front under the eyes, up the right shoulder.
    va = [sring(0.62, 300, 0.05), sring(0.47, 292, 0.40), sring(0.39, 305, 0.70), sring(0.49, 322, 0.98), sring(0.39, 340, 1.22),
          sring(0.48, 0, 1.44), sring(0.39, 22, 1.66), sring(0.49, 44, 1.90), sring(0.40, 64, 2.16), sring(0.50, 78, 2.46),
          sring(0.41, 80, 2.80), sring(0.50, 72, 3.10), sring(0.46, 64, 3.36)]
    line(trunk, va, [0.050, 0.048, 0.046, 0.046, 0.044, 0.044, 0.042, 0.042, 0.040, 0.038, 0.036, 0.033, 0.026], VINE, per=4)
    sprig(trunk, va[3], 320, -20, 0.24, 0.14, LEAF)
    sprig(trunk, va[7], 40, 10, 0.22, 0.13, LEAF_DARK)
    sprig(trunk, va[9], 90, -30, 0.26, 0.15, LEAF)
    sprig(trunk, va[12], 50, 35, 0.28, 0.16, LEAF)
    # (B) up the back the other way, from the back-right root to the back-left shoulder.
    vb = [sring(0.60, 150, 0.08), sring(0.46, 162, 0.42), sring(0.39, 150, 0.78), sring(0.48, 132, 1.10), sring(0.39, 118, 1.40),
          sring(0.49, 104, 1.72), sring(0.40, 98, 2.00), sring(0.49, 110, 2.30), sring(0.40, 128, 2.62), sring(0.49, 144, 2.95),
          sring(0.45, 158, 3.30)]
    line(trunk, vb, [0.046, 0.045, 0.043, 0.043, 0.041, 0.040, 0.039, 0.037, 0.035, 0.031, 0.024], MOSS_DARK, per=4)
    sprig(trunk, vb[2], 160, -15, 0.22, 0.13, LEAF_DARK)
    sprig(trunk, vb[5], 100, 5, 0.24, 0.14, LEAF)
    sprig(trunk, vb[10], 160, 40, 0.26, 0.15, LEAF_DARK)
    # (C) a short one hanging out of the crown on the back, swinging free, a leaf cluster at its end.
    vc = [sring(0.50, 168, 3.40), sring(0.60, 172, 3.18), sring(0.64, 177, 2.96), sring(0.62, 181, 2.78), sring(0.58, 178, 2.66)]
    line(trunk, vc, [0.036, 0.034, 0.032, 0.028, 0.020], VINE, per=4)
    sprig(trunk, vc[4], 180, -70, 0.24, 0.14, LEAF)
    sprig(trunk, vc[4], 240, -55, 0.20, 0.12, LEAF_DARK)

    # --- MOSS, only on the tops of things: where cords part at the shoulder and on a few cord crossings.
    trunk.obox(sring(0.42, 38, 3.24), (0.32, 0.07, 0.22), MOSS, yaw=38)
    trunk.obox(sring(0.44, 158, 3.27), (0.28, 0.06, 0.19), MOSS_DARK, yaw=158)
    trunk.obox(sring(0.40, 272, 3.22), (0.34, 0.07, 0.24), MOSS, yaw=272)
    trunk.obox(sring(0.45, 212, 1.05), (0.20, 0.05, 0.13), MOSS_DARK, yaw=212)
    trunk.obox(sring(0.44, 96, 2.64), (0.17, 0.05, 0.11), MOSS_LIT, yaw=96)
    trunk.obox(sring(0.50, 18, 0.42), (0.22, 0.05, 0.14), MOSS, yaw=18)

    # --- SIX CLAW ROOTS (v7, the owner's cartoon tree: *"lowk why not use this for inspiration"*): the
    # weave flares wide at the foot and parts into roots that grip the road like fingers, each ending in a
    # toe that curls up off the ground. Their own nodes on the ROOT, not the trunk, so the trunk turns to
    # look while they grip. Each typed along its +Z from the weave's foot: two cords laid together (a woven
    # root), a moss cap on the knuckle.
    roots = [
        (22, [(0, 0.70, -0.14), (0.03, 0.44, 0.20), (0.06, 0.14, 0.70), (0.02, 0.02, 1.18), (-0.02, 0.00, 1.52), (-0.03, 0.10, 1.70), (-0.02, 0.20, 1.66)],
         [0.30, 0.24, 0.16, 0.10, 0.065, 0.040, 0.018], BARK,
         [(-0.09, 0.62, -0.08), (-0.12, 0.38, 0.24), (-0.12, 0.12, 0.72), (-0.08, 0.02, 1.06)], [0.14, 0.11, 0.07, 0.02], BARK_LIT, MOSS),
        (80, [(0, 0.62, -0.12), (-0.02, 0.38, 0.16), (-0.06, 0.12, 0.56), (-0.03, 0.01, 0.98), (0.02, 0.00, 1.26), (0.04, 0.09, 1.42), (0.03, 0.18, 1.38)],
         [0.25, 0.20, 0.14, 0.085, 0.055, 0.034, 0.016], BARK_DARK,
         [(0.08, 0.54, -0.06), (0.10, 0.34, 0.20), (0.07, 0.10, 0.58), (0.04, 0.01, 0.86)], [0.12, 0.10, 0.06, 0.02], BARK, MOSS_DARK),
        (139, [(0, 0.72, -0.14), (0.02, 0.46, 0.22), (0.07, 0.16, 0.78), (0.09, 0.03, 1.30), (0.06, 0.00, 1.72), (0.02, 0.11, 1.92), (0.00, 0.22, 1.86)],
         [0.31, 0.25, 0.17, 0.11, 0.070, 0.042, 0.018], BARK,
         [(-0.10, 0.62, -0.08), (-0.09, 0.40, 0.26), (-0.05, 0.14, 0.80), (0.00, 0.03, 1.16)], [0.15, 0.12, 0.07, 0.02], HEARTWOOD, MOSS),
        (199, [(0, 0.60, -0.12), (-0.03, 0.36, 0.18), (-0.05, 0.12, 0.62), (0.00, 0.01, 1.04), (0.04, 0.00, 1.36), (0.05, 0.08, 1.52), (0.03, 0.17, 1.50)],
         [0.25, 0.20, 0.14, 0.085, 0.055, 0.034, 0.016], BARK_LIT,
         [(0.09, 0.52, -0.06), (0.11, 0.32, 0.22), (0.09, 0.10, 0.62), (0.05, 0.01, 0.92)], [0.12, 0.10, 0.06, 0.02], BARK, MOSS_DARK),
        (252, [(0, 0.68, -0.14), (0.02, 0.42, 0.18), (0.02, 0.14, 0.70), (-0.03, 0.02, 1.20), (-0.07, 0.00, 1.62), (-0.08, 0.10, 1.82), (-0.06, 0.20, 1.78)],
         [0.30, 0.24, 0.16, 0.10, 0.065, 0.040, 0.018], BARK_DARK,
         [(-0.09, 0.58, -0.07), (-0.11, 0.37, 0.22), (-0.10, 0.12, 0.70), (-0.06, 0.02, 1.08)], [0.14, 0.11, 0.07, 0.02], BARK_LIT, MOSS),
        (318, [(0, 0.58, -0.12), (-0.02, 0.34, 0.16), (0.03, 0.11, 0.52), (0.05, 0.01, 0.90), (0.03, 0.00, 1.18), (0.00, 0.08, 1.32), (-0.02, 0.16, 1.28)],
         [0.23, 0.19, 0.13, 0.075, 0.050, 0.030, 0.014], BARK,
         [(0.08, 0.50, -0.05), (0.09, 0.31, 0.18), (0.10, 0.09, 0.50), (0.07, 0.01, 0.76)], [0.11, 0.09, 0.055, 0.02], HEARTWOOD, MOSS_LIT),
    ]
    # ⚠️⚠️ THE TOES GO INTO THE COURT (owner, 2026-09-26: *"make it look like the roots GO INT he ground not
    # float off of it"*). v7's toes curled 0.2 m UP off the road at the tip, so from his screen every claw read
    # as lying on the court; now each root runs its last span DOWN through the surface (hidden below it) and a
    # heave of earth sits where it goes in. Toe depth and reach per root, typed.
    dives = [(0.16, 0.24), (0.13, 0.20), (0.18, 0.27), (0.14, 0.21), (0.17, 0.25), (0.12, 0.19)]
    heaves = [((0.30, 0.10, 0.22), 12), ((0.26, 0.09, 0.20), -20), ((0.34, 0.11, 0.24), 30),
              ((0.27, 0.09, 0.19), -8), ((0.32, 0.10, 0.23), 18), ((0.24, 0.08, 0.18), -26)]
    for i, (yaw, pts, radii, slot, rider, rider_r, rider_slot, moss) in enumerate(roots):
        n = Node(f"buttress-{i}", origin=ring(0.46, yaw, 0.0), parent="sentry", yaw=yaw); nodes.append(n)
        knee = pts[4]
        pts = pts[:5] + [(knee[0], -0.10, knee[2] + dives[i][0]), (knee[0], -0.26, knee[2] + dives[i][1])]
        line(n, pts, radii, slot, twist=30.0)
        size, turn = heaves[i]
        n.obox((knee[0], 0.02, knee[2] + 0.10), size, ROOTC, yaw=turn, pitch=-9.0)
        n.obox((knee[0] + 0.10, 0.015, knee[2] - 0.04), (size[0] * 0.55, size[1] * 0.8, size[2] * 0.6), MOSS_DARK, yaw=turn + 40)
        line(n, rider, rider_r, rider_slot)
        k = pts[2]
        n.obox((k[0], k[1] + radii[2] * 0.85, k[2]), (radii[2] * 1.7, 0.055, radii[2] * 1.3), moss)

    # --- THE CROWN (v7, the cartoon tree): the swollen top of the weave splits into SEVEN gnarled woven
    # branches reaching up and out, each with a fork or two, every tip curling over into a hook. v3's five
    # square antlers read as a hat. Each branch is two cords laid together, typed along its node's +Z;
    # leaves only in a few small tufts (he is a living forest, not a dead tree), the core held among them.
    crown = Node("crown", origin=(0.0, 3.22, 0.0), parent="trunk"); nodes.append(crown)
    branches = [
        # name, yaw, main cord, radii, second cord, radii, fork, fork radii, shade, leaf tufts
        ("claw-0", 8,
         [(0, -0.06, -0.10), (0.02, 0.30, 0.16), (0.00, 0.72, 0.40), (-0.04, 1.06, 0.70), (-0.02, 1.30, 0.94), (0.02, 1.42, 0.86), (0.03, 1.38, 0.74), (0.01, 1.28, 0.76)],
         [0.19, 0.16, 0.12, 0.085, 0.058, 0.040, 0.028, 0.014],
         [(0.06, -0.02, -0.06), (0.07, 0.34, 0.20), (0.03, 0.70, 0.46), (-0.01, 0.98, 0.72)], [0.10, 0.085, 0.060, 0.022],
         [(-0.03, 0.82, 0.50), (-0.20, 1.02, 0.60), (-0.30, 1.14, 0.54), (-0.27, 1.08, 0.46)], [0.060, 0.040, 0.024, 0.010],
         BARK_LIT, [((-0.02, 1.36, 0.86), 20, 30, 0.24, 0.14, LEAF)]),
        ("claw-1", 58,
         [(0, -0.05, -0.10), (-0.02, 0.24, 0.20), (0.02, 0.56, 0.52), (0.05, 0.84, 0.84), (0.04, 1.00, 1.08), (0.00, 1.06, 1.18), (-0.04, 1.00, 1.14), (-0.03, 0.94, 1.06)],
         [0.17, 0.145, 0.11, 0.080, 0.055, 0.038, 0.026, 0.013],
         [(-0.06, -0.02, -0.06), (-0.07, 0.28, 0.24), (-0.03, 0.56, 0.56), (0.01, 0.80, 0.84)], [0.09, 0.075, 0.055, 0.020],
         [(0.03, 0.62, 0.60), (0.20, 0.86, 0.66), (0.26, 1.02, 0.60), (0.22, 0.98, 0.52)], [0.055, 0.036, 0.022, 0.010],
         BARK, [((0.22, 1.00, 0.56), 60, 40, 0.20, 0.12, LEAF_DARK)]),
        ("claw-2", 110,
         [(0, -0.06, -0.10), (0.02, 0.32, 0.14), (-0.01, 0.78, 0.30), (-0.05, 1.18, 0.50), (-0.03, 1.48, 0.62), (0.02, 1.60, 0.52), (0.04, 1.54, 0.42), (0.02, 1.44, 0.44)],
         [0.18, 0.155, 0.12, 0.085, 0.058, 0.040, 0.028, 0.014],
         [(0.05, -0.02, -0.06), (0.07, 0.36, 0.18), (0.04, 0.78, 0.36), (0.00, 1.10, 0.52)], [0.095, 0.080, 0.058, 0.021],
         [(-0.04, 0.96, 0.40), (-0.24, 1.10, 0.52), (-0.36, 1.18, 0.46), (-0.33, 1.12, 0.38)], [0.058, 0.038, 0.023, 0.010],
         BARK_DARK, [((0.02, 1.56, 0.50), -30, 50, 0.26, 0.15, LEAF), ((-0.34, 1.16, 0.44), -80, 30, 0.20, 0.12, LEAF_DARK)]),
        ("claw-3", 163,
         [(0, -0.05, -0.10), (0.01, 0.22, 0.22), (0.03, 0.50, 0.58), (0.00, 0.74, 0.92), (-0.04, 0.88, 1.16), (-0.06, 0.92, 1.28), (-0.03, 0.86, 1.26), (-0.01, 0.80, 1.18)],
         [0.17, 0.14, 0.105, 0.078, 0.052, 0.036, 0.025, 0.012],
         [(-0.05, -0.02, -0.06), (-0.06, 0.26, 0.26), (-0.02, 0.50, 0.62), (0.01, 0.70, 0.92)], [0.09, 0.075, 0.052, 0.019],
         [(0.02, 0.58, 0.72), (0.18, 0.80, 0.80), (0.24, 0.96, 0.74), (0.20, 0.92, 0.66)], [0.050, 0.034, 0.021, 0.009],
         BARK_LIT, []),
        ("claw-4", 214,
         [(0, -0.06, -0.10), (-0.02, 0.34, 0.16), (0.00, 0.80, 0.36), (0.05, 1.16, 0.60), (0.04, 1.40, 0.78), (0.00, 1.50, 0.70), (-0.03, 1.45, 0.60), (-0.02, 1.36, 0.62)],
         [0.185, 0.16, 0.12, 0.085, 0.058, 0.040, 0.028, 0.014],
         [(0.06, -0.02, -0.06), (0.06, 0.36, 0.20), (0.02, 0.78, 0.42), (-0.02, 1.06, 0.62)], [0.10, 0.082, 0.058, 0.021],
         [(0.03, 0.90, 0.46), (0.22, 1.06, 0.58), (0.32, 1.14, 0.50), (0.28, 1.08, 0.42)], [0.058, 0.038, 0.023, 0.010],
         BARK, [((0.00, 1.48, 0.70), 10, 45, 0.24, 0.14, LEAF_DARK)]),
        ("claw-5", 266,
         [(0, -0.05, -0.10), (0.02, 0.26, 0.20), (0.00, 0.60, 0.50), (-0.03, 0.88, 0.80), (-0.02, 1.04, 1.02), (0.02, 1.10, 1.12), (0.05, 1.04, 1.10), (0.04, 0.98, 1.02)],
         [0.17, 0.145, 0.11, 0.080, 0.055, 0.038, 0.026, 0.013],
         [(-0.06, -0.02, -0.06), (-0.06, 0.30, 0.24), (-0.03, 0.60, 0.54), (0.00, 0.84, 0.80)], [0.09, 0.075, 0.055, 0.020],
         [(-0.02, 0.70, 0.62), (-0.20, 0.92, 0.70), (-0.28, 1.06, 0.62), (-0.24, 1.02, 0.54)], [0.055, 0.036, 0.022, 0.010],
         BARK_DARK, [((-0.26, 1.04, 0.60), -60, 35, 0.22, 0.13, LEAF)]),
        ("claw-6", 318,
         [(0, -0.06, -0.10), (-0.01, 0.30, 0.16), (0.02, 0.70, 0.40), (0.04, 1.04, 0.68), (0.02, 1.28, 0.90), (-0.02, 1.38, 0.84), (-0.04, 1.32, 0.74), (-0.02, 1.24, 0.74)],
         [0.18, 0.155, 0.115, 0.082, 0.056, 0.039, 0.027, 0.013],
         [(0.05, -0.02, -0.06), (0.06, 0.34, 0.20), (0.03, 0.70, 0.46), (0.00, 0.96, 0.70)], [0.095, 0.080, 0.056, 0.020],
         [(0.02, 0.80, 0.48), (0.20, 0.98, 0.56), (0.30, 1.10, 0.50), (0.27, 1.05, 0.42)], [0.056, 0.037, 0.022, 0.010],
         BARK_LIT, [((0.28, 1.08, 0.48), 70, 30, 0.22, 0.13, LEAF), ((-0.02, 1.34, 0.84), -20, 55, 0.20, 0.12, LEAF_DARK)]),
    ]
    # ⚠️⚠️ THE CROWN HAS LEAVES (owner, 2026-09-26, of the tree from his screen: *"it sucks"*, *"REFINE THIS TREE
    # MORE"*): v7 kept the seven claws almost bare, and at 9 m against the sky they read as a dead bundle of
    # sticks. He is a living forest. Each branch now carries clusters of broad leaves along its upper third and
    # round its fork, typed per branch: (point index on the main cord, compass, pitch, length, width, slot).
    canopy = {
        "claw-0": [(3, 300, 20, 0.48, 0.26, LEAF), (3, 60, 35, 0.42, 0.24, LEAF_DARK), (4, 170, 45, 0.50, 0.27, LEAF), (5, 20, 60, 0.40, 0.22, MOSS_LIT), (4, 250, -10, 0.38, 0.21, LEAF_DARK)],
        "claw-1": [(3, 320, 25, 0.46, 0.25, LEAF_DARK), (4, 80, 40, 0.50, 0.27, LEAF), (4, 200, 15, 0.40, 0.22, MOSS_LIT), (5, 10, 55, 0.42, 0.23, LEAF)],
        "claw-2": [(3, 30, 30, 0.50, 0.27, LEAF), (4, 280, 45, 0.46, 0.25, LEAF_DARK), (5, 140, 60, 0.44, 0.24, LEAF), (4, 90, -5, 0.38, 0.21, MOSS_LIT), (3, 200, 20, 0.40, 0.22, LEAF_DARK)],
        "claw-3": [(3, 340, 20, 0.44, 0.24, LEAF), (4, 60, 45, 0.48, 0.26, LEAF_DARK), (5, 200, 50, 0.40, 0.22, LEAF)],
        "claw-4": [(3, 100, 25, 0.48, 0.26, LEAF_DARK), (4, 320, 40, 0.50, 0.27, LEAF), (5, 210, 60, 0.42, 0.23, MOSS_LIT), (4, 30, 0, 0.38, 0.21, LEAF)],
        "claw-5": [(3, 280, 20, 0.46, 0.25, LEAF), (4, 40, 45, 0.44, 0.24, LEAF_DARK), (5, 160, 55, 0.42, 0.23, LEAF)],
        "claw-6": [(3, 20, 30, 0.48, 0.26, LEAF_DARK), (4, 250, 40, 0.46, 0.25, LEAF), (5, 110, 55, 0.44, 0.24, LEAF), (4, 170, 5, 0.38, 0.21, MOSS_LIT)],
    }
    # The leaf masses per branch, in the branch's own space (+Z along it): (centre, radii, slot, tilt, turn).
    # The shaded mass sits under and inside, the lit one over and outside, each its own size and lean.
    masses = {
        "claw-0": [((-0.04, 1.02, 0.66), (0.44, 0.32, 0.40), LEAF_DARK, 8.0, 10.0), ((0.10, 1.24, 0.60), (0.34, 0.27, 0.32), LEAF, -6.0, 40.0),
                   ((-0.26, 1.16, 0.48), (0.24, 0.20, 0.24), MOSS_LIT, 12.0, 70.0)],
        "claw-1": [((0.04, 0.80, 0.86), (0.42, 0.30, 0.40), LEAF_DARK, 10.0, -20.0), ((-0.08, 1.00, 0.92), (0.32, 0.26, 0.30), LEAF, -8.0, 15.0)],
        "claw-2": [((0.00, 1.26, 0.42), (0.46, 0.34, 0.40), LEAF_DARK, 6.0, 30.0), ((0.14, 1.46, 0.36), (0.33, 0.28, 0.31), LEAF, -4.0, 60.0),
                   ((-0.30, 1.10, 0.36), (0.26, 0.22, 0.25), LEAF, 14.0, -30.0)],
        "claw-3": [((-0.02, 0.64, 0.98), (0.40, 0.29, 0.38), LEAF_DARK, 12.0, 25.0), ((0.12, 0.82, 1.02), (0.30, 0.24, 0.28), MOSS_LIT, -6.0, 50.0)],
        "claw-4": [((0.04, 1.20, 0.58), (0.45, 0.33, 0.41), LEAF_DARK, 8.0, -15.0), ((-0.12, 1.40, 0.52), (0.34, 0.28, 0.32), LEAF, -5.0, 20.0),
                   ((0.28, 1.06, 0.46), (0.24, 0.20, 0.23), MOSS_LIT, 10.0, 80.0)],
        "claw-5": [((-0.04, 0.88, 0.86), (0.42, 0.30, 0.39), LEAF_DARK, 9.0, 35.0), ((0.10, 1.04, 0.90), (0.31, 0.25, 0.29), LEAF, -7.0, -10.0)],
        "claw-6": [((0.02, 1.08, 0.64), (0.44, 0.32, 0.40), LEAF_DARK, 7.0, -35.0), ((-0.10, 1.28, 0.62), (0.33, 0.27, 0.31), LEAF, -5.0, 5.0),
                   ((0.26, 1.02, 0.44), (0.25, 0.21, 0.24), LEAF, 13.0, 55.0)],
    }
    # A strand or two of old moss hanging from under the masses: typed, each its own length and sway.
    moss = {
        "claw-0": [([(-0.10, 0.80, 0.62), (-0.12, 0.60, 0.66), (-0.10, 0.42, 0.64)], [0.030, 0.024, 0.010])],
        "claw-1": [([(0.06, 0.58, 0.84), (0.08, 0.36, 0.88), (0.05, 0.20, 0.86)], [0.028, 0.022, 0.009])],
        "claw-2": [([(0.08, 1.02, 0.40), (0.10, 0.78, 0.42), (0.07, 0.62, 0.40)], [0.030, 0.024, 0.010]),
                   ([(-0.22, 0.96, 0.30), (-0.24, 0.80, 0.32)], [0.024, 0.010])],
        "claw-3": [([(0.02, 0.44, 0.96), (0.03, 0.26, 0.98), (0.01, 0.14, 0.96)], [0.026, 0.020, 0.008])],
        "claw-4": [([(-0.06, 0.96, 0.56), (-0.08, 0.72, 0.58), (-0.05, 0.56, 0.56)], [0.030, 0.024, 0.010])],
        "claw-5": [([(0.04, 0.66, 0.84), (0.06, 0.46, 0.86), (0.03, 0.30, 0.84)], [0.028, 0.022, 0.009])],
        "claw-6": [([(0.10, 0.86, 0.60), (0.12, 0.64, 0.62), (0.09, 0.48, 0.60)], [0.030, 0.024, 0.010]),
                   ([(-0.18, 0.84, 0.54), (-0.20, 0.70, 0.56)], [0.022, 0.009])],
    }
    for name, yaw, main, main_r, second, second_r, fork, fork_r, shade, tufts in branches:
        n = Node(name, origin=ring(0.30, yaw, 0.0), parent="crown", yaw=yaw); nodes.append(n)
        line(n, main, main_r, shade, twist=40.0, per=4)
        line(n, second, second_r, BARK if shade != BARK else BARK_DARK, twist=-30.0, per=4)
        line(n, fork, fork_r, shade, per=4)
        for base, compass, pitch, length, width, slot in tufts:
            sprig(n, base, compass, pitch, length, width, slot)
            sprig(n, base, compass + 70, pitch - 25, length * 0.8, width * 0.8, LEAF_DARK if slot == LEAF else LEAF)
        for idx, compass, pitch, length, width, slot in canopy[name]:
            sprig(n, main[idx], compass, pitch, length, width, slot, thickness=0.022)
        # ⚠️⚠️ v8 (2026-09-26 night): THE CROWN HAS MASS. The leaves alone were still flecks at 9 m (the owner's
        # *"REFINE THIS TREE MORE"*; `review/ult_inmatch_tree_v4_before_reauthor.png`: a bare trunk with sprouts).
        # Each branch now carries two or three round leaf masses round its upper third, the plaza trees' own
        # canopy shapes, dark underneath and lit on top, so the guardian reads as a great tree in leaf, with the
        # woven branches showing between the masses and the hooked tips poking out of them.
        for centre, radii, slot, tilt, turn in masses[name]:
            clump(n, centre, radii, slot, tilt, turn)
        for points, radii in moss[name]:
            line(n, points, radii, MOSS_DARK, sides=4, per=3)

    write(os.path.join(OUT, "sentry.glb"), nodes)


# ---------------------------------------------------------------------------------------------
# ⚠️⚠️ BAKYA BLOOM IS A PITCHER PLANT AND THORN HARVEST IS AN ARMED RATTAN, NOT BROWN BARK
# (owner, 2026-09-26): *"do all his sentries look the same? i wanted all his sentries (ult and attacker
# skill and defender skill TO ALL look diff and distinct and have their own style)"*, then of the
# concept sheets *"thats pretty fucking good"*, and of the first rattan *"it looks like  a flimsy plant
# and not a dangerous cool plant"*. The v7 seedling (a three-cord bark stem with a petal bud) and the
# v1 thorn fist (bark roots and square thorns) wore the ultimate's woven bark and read as three of
# one tree. `docs/reports/paete-kit-2026-09-25/direction.md` section 5.11 has the design: three
# silhouettes, three materials, three motion languages.
#
# ⚠️ EACH HAS ITS OWN SIXTEEN-SLOT PALETTE. The slots below mean the SPECIES' colours, the way
# Makiling's do; the runtime dresses them with `PaetePitcher.Palette` and `PaeteRattan.Palette`
# (`PaeteTrees.cs`), which carry the same hex values as the tables here. File names are kept
# (seedling.glb, thorns.glb) so `PaeteProp.Spawn` still finds them.
# ---------------------------------------------------------------------------------------------
P_BODY, P_SHADE, P_LEAF, P_LEAF_DK, P_TENDRIL, P_WOOD, P_ROOT, P_MOSS = 0, 1, 2, 3, 4, 5, 6, 7
P_INK, P_INSIDE, P_RIM, P_SPECK, P_LID_UNDER, P_WOOD_DK, P_STRAP, P_MOSS_DK = 8, 9, 10, 11, 12, 13, 14, 15
# Pitcher slots: see the P_* names above (P_SPECK is unused since speckles read as stickers).
PITCHER_PALETTE = {0: "93B540", 1: "5B7F2C", 2: "4F8B2F", 3: "3A6B24", 4: "6F9B35", 5: "C29563",
                 6: "A8946A", 7: "5E7F24", 8: "1E140C", 9: "2B1512", 10: "8E2435", 11: "9A3243",
                 12: "B04A55", 13: "8A6240", 14: "3F5A1A", 15: "3F5A1A"}

# The pitcher's body, typed: height up the pod, radius, forward lean. A fat belly, a waist, a flared mouth.
BODY = [(-0.02, 0.066, 0.000), (0.07, 0.170, 0.010), (0.15, 0.188, 0.020), (0.23, 0.156, 0.035),
        (0.29, 0.118, 0.050), (0.34, 0.110, 0.065), (0.38, 0.132, 0.085)]
# ⚠️ Refined 2026-09-26 after the first in-engine film (v19 to v21): the belly fattened (0.166 to 0.188) and
# the mouth opened (0.124 to 0.132) toward the concept's round jug, which read as a thin lightbulb in play.


def pitcher_body(pod, rows, slot, inside_slot, sides=7):
    path = [(0.0, y, lean) for y, _, lean in rows]
    radii = [r for _, r, _ in rows]
    faces = tube(spline(path, 4), spread(radii, 4), sides)
    top = faces[-1]
    t = unit(sub(path[-1], path[-2]))
    sunk = [sub(p, mul(t, 0.03)) for p in top]
    pod.parts.append((slot, faces[:-1]))
    pod.parts.append((inside_slot, [sunk, list(reversed(sunk))]))


def seedling():
    nodes = []
    root = Node("seedling"); nodes.append(root)
    moss = Node("moss", parent="seedling"); nodes.append(moss)
    moss.obox((0.10, 0.03, 0.06), (0.17, 0.07, 0.15), P_MOSS, yaw=14.0)
    moss.obox((-0.11, 0.025, -0.03), (0.14, 0.06, 0.16), P_MOSS_DK, yaw=-22.0)
    moss.obox((0.02, 0.02, -0.13), (0.12, 0.05, 0.11), P_MOSS, yaw=41.0)
    moss.obox((0.0, 0.01, 0.0), (0.34, 0.03, 0.30), P_ROOT, yaw=8.0)

    # Four fleshy roots gripping the court, each its own reach.
    for name, yaw, pts, radii in [("root-0", 40, [(0, 0.05, 0.10), (0.01, 0.02, 0.24), (0.0, -0.03, 0.33)], [0.038, 0.026, 0.008]),
                                  ("root-1", 128, [(0, 0.05, 0.10), (-0.02, 0.01, 0.21), (-0.01, -0.03, 0.27)], [0.034, 0.024, 0.008]),
                                  ("root-2", 214, [(0, 0.05, 0.10), (0.02, 0.02, 0.26), (0.01, -0.03, 0.36)], [0.040, 0.027, 0.008]),
                                  ("root-3", 301, [(0, 0.05, 0.10), (-0.01, 0.01, 0.20), (0.0, -0.03, 0.25)], [0.032, 0.022, 0.007])]:
        n = Node(name, parent="seedling", yaw=yaw); nodes.append(n)
        line(n, pts, radii, P_ROOT, per=3)

    # Five lance leaves in a rosette: glossy, stiff, each typed. Two are the ARMS that flare on the shot.
    d = 0.0
    for name, compass, pitch, length, width, slot in [("arm-0", 42, 18, 0.56, 0.15, P_LEAF), ("leaf-1", 118, 9, 0.50, 0.13, P_LEAF_DK),
                                                      ("leaf-2", 186, 14, 0.60, 0.14, P_LEAF), ("leaf-3", 250, 7, 0.46, 0.12, P_LEAF_DK),
                                                      ("arm-1", 318, 21, 0.54, 0.15, P_LEAF)]:
        n = Node(name, origin=(0.0, 0.07, 0.0), parent="seedling"); nodes.append(n)
        sprig(n, (0.0, 0.0, 0.0), compass, pitch - 26 * d, length, width, slot, thickness=0.02)
        # the midrib, a darker line down the leaf
        a = math.radians(compass); p = math.radians(pitch - 26 * d)
        dirv = (math.sin(a) * math.cos(p), math.sin(p) + 0.012, math.cos(a) * math.cos(p))
        line(n, [mul(dirv, 0.05), mul(dirv, length * 0.55), mul(dirv, length * 0.85)], [0.009, 0.007, 0.004], P_TENDRIL, sides=4, per=2)

    # One leaf carries a tendril and a baby pitcher: the species tell (a Nepenthes leaf ends in a pitcher).
    baby = Node("baby", parent="leaf-2"); nodes.append(baby)
    tip = (math.sin(math.radians(186)) * 0.58, 0.07 + 0.14, math.cos(math.radians(186)) * 0.58)
    line(baby, [tip, add(tip, (-0.02, 0.02, -0.06)), add(tip, (-0.03, -0.05, -0.10)), add(tip, (-0.02, -0.11, -0.09))],
         [0.013, 0.011, 0.010, 0.009], P_TENDRIL, sides=4, per=3)
    bb = add(tip, (-0.02, -0.19, -0.09))
    baby.tube([add(bb, (0, 0.0, 0)), add(bb, (0, 0.035, 0.003)), add(bb, (0, 0.07, 0.008)), add(bb, (0, 0.09, 0.012))],
              [0.022, 0.042, 0.030, 0.034], P_BODY, sides=6)
    baby.tube([add(bb, (-0.036, 0.092, 0.012)), add(bb, (0, 0.094, 0.046)), add(bb, (0.036, 0.092, 0.012)), add(bb, (0, 0.090, -0.022)), add(bb, (-0.036, 0.092, 0.012))],
              [0.010] * 5, P_RIM, sides=4)
    baby.obox(add(bb, (0, 0.118, -0.010)), (0.075, 0.014, 0.07), P_SHADE, roll=-24.0)

    # The tendril-neck: thick, rising in an S, so the pitcher can nod and aim.
    stem = Node("stem", parent="seedling"); nodes.append(stem)
    line(stem, [(0.0, 0.04, -0.02), (0.03, 0.20, -0.07), (0.01, 0.36, -0.08), (-0.02, 0.47, -0.03), (0.0, 0.53, 0.03)],
         [0.056, 0.048, 0.043, 0.040, 0.040], P_TENDRIL, sides=6)
    # a leaf-scar collar where the neck leaves the rosette
    stem.obox((0.0, 0.10, -0.04), (0.14, 0.03, 0.14), P_LEAF_DK, yaw=20.0)

    pod = Node("pod", origin=(0.0, 0.53, 0.03), parent="stem"); nodes.append(pod)
    pitcher_body(pod, BODY, P_BODY, P_INSIDE)
    # The peristome: a rolled maroon lip, typed round the mouth (the mouth leans 26 degrees forward).
    c = (0.0, 0.38, 0.085)
    t = unit((0.0, 0.04, 0.02)); side = (1.0, 0.0, 0.0); up = unit(cross(t, side))
    rim = []
    for ang, r in [(0, 0.128), (52, 0.132), (101, 0.126), (153, 0.131), (205, 0.127), (257, 0.133), (309, 0.129), (360, 0.128), (412, 0.132)]:
        a = math.radians(ang)
        rim.append(add(c, add(mul(side, r * math.cos(a)), mul(up, r * math.sin(a)))))
    pod.tube(spline(rim, 4), [0.042] * len(spline(rim, 4)), P_RIM, sides=6)
    # The lip's rolled inner edge, a lighter wine a step inside and above, so the peristome reads as a
    # thick rolled collar (the concept's) rather than a thin red band.
    lip = []
    for ang, r in [(0, 0.100), (60, 0.103), (118, 0.099), (177, 0.102), (236, 0.100), (295, 0.104), (360, 0.100), (420, 0.103)]:
        a = math.radians(ang)
        lip.append(add(add(c, mul(t, 0.022)), add(mul(side, r * math.cos(a)), mul(up, r * math.sin(a)))))
    pod.tube(spline(lip, 4), [0.020] * len(spline(lip, 4)), P_SPECK, sides=5)
    # Dark stripes down the belly, the concept's ink lines (a Nepenthes pitcher's veins). Each typed on the
    # surface by hand: its angle round the jug (0 = front), and the rows it runs between. The front two are
    # the wings below, so these sit on the flanks and the back.
    for pts in [[(0.105, 0.30, 0.018), (0.150, 0.22, 0.042), (0.158, 0.13, 0.060), (0.110, 0.04, 0.050)],
                [(-0.104, 0.30, 0.020), (-0.149, 0.22, 0.044), (-0.160, 0.14, 0.058), (-0.112, 0.05, 0.047)],
                [(0.074, 0.29, -0.052), (0.108, 0.20, -0.090), (0.118, 0.12, -0.106), (0.074, 0.04, -0.078)],
                [(-0.078, 0.29, -0.050), (-0.110, 0.21, -0.088), (-0.121, 0.12, -0.104), (-0.070, 0.03, -0.080)],
                [(0.0, 0.28, -0.085), (0.0, 0.20, -0.128), (0.0, 0.11, -0.150), (0.0, 0.03, -0.100)]]:
        line(pod, pts, [0.009, 0.011, 0.010, 0.005], P_LEAF_DK, sides=4, per=3)
    # Two wings down the front (a pitcher's ridges), each typed.
    line(pod, [(0.050, 0.33, 0.168), (0.055, 0.23, 0.194), (0.049, 0.12, 0.192), (0.032, 0.03, 0.096)], [0.020, 0.022, 0.018, 0.008], P_SHADE, sides=4, per=3)
    line(pod, [(-0.047, 0.33, 0.166), (-0.053, 0.22, 0.193), (-0.050, 0.11, 0.190), (-0.030, 0.03, 0.094)], [0.020, 0.022, 0.018, 0.008], P_SHADE, sides=4, per=3)

    # The lid, hinged at the back of the rim; its rest lies over the mouth.
    hinge = add(c, mul((0.0, 0.447, -0.894), 0.13))
    lid = Node("lid", origin=hinge, parent="pod"); nodes.append(lid)
    fwd = (0.0, -0.447, 0.894)
    # ⚠️ A ROUNDED LID BIGGER THAN THE MOUTH (v19 to v21 read as a thin tilted plate): a broad oval of three
    # crossed slabs, a thicker crown, the wine underside, and a hinge neck so it visibly grows from the rim.
    lid.obox(add(mul(fwd, 0.140), mul(t, 0.034)), (0.30, 0.050, 0.25), P_BODY, roll=26.6)
    lid.obox(add(mul(fwd, 0.146), mul(t, 0.034)), (0.21, 0.051, 0.33), P_BODY, roll=26.6)
    lid.obox(add(mul(fwd, 0.140), mul(t, 0.034)), (0.26, 0.052, 0.29), P_BODY, yaw=45.0, roll=26.6)
    lid.obox(add(mul(fwd, 0.132), mul(t, 0.058)), (0.16, 0.030, 0.17), P_SHADE, roll=26.6)
    lid.obox(add(mul(fwd, 0.140), mul(t, 0.008)), (0.24, 0.012, 0.24), P_LID_UNDER, roll=26.6)
    line(lid, [(0.0, -0.02, 0.0), add(mul(fwd, 0.03), mul(t, 0.03))], [0.034, 0.030], P_SHADE, sides=5, per=2)
    line(lid, [mul(t, 0.02), add(mul(t, 0.06), (0, 0.0, -0.035)), add(mul(t, 0.08), (0, 0.0, -0.075))], [0.022, 0.014, 0.004], P_SHADE, sides=4, per=2)

    # The bakya, the carved clog growing in the mouth: sole on two blocks, a strap across the toe.
    shoe = Node("slipper", origin=(0.0, 0.31, 0.075), parent="pod"); nodes.append(shoe)
    shoe.box((-0.060, 0.020, -0.130), (0.060, 0.045, 0.130), P_WOOD)
    shoe.box((-0.055, -0.030, -0.120), (0.055, 0.020, -0.050), P_WOOD_DK)
    shoe.box((-0.055, -0.030, 0.030), (0.055, 0.020, 0.100), P_WOOD_DK)
    shoe.box((-0.066, 0.045, 0.020), (0.066, 0.078, 0.075), P_STRAP)
    shoe.box((-0.020, 0.046, -0.090), (0.020, 0.050, -0.010), P_WOOD_DK, bevel=0)
    write(os.path.join(OUT, "seedling.glb"), nodes)



def _yaw(p, comp):
    a = math.radians(comp)
    x, y, z = p
    return (x * math.cos(a) + z * math.sin(a), y, -x * math.sin(a) + z * math.cos(a))


# Rattan slots: 0 blade  1 blade dark  2 blade lit  3 cane frond (rachis)  4 sheath  5 sheath dark
#   6 straw cane (the whip, the arnis stick)  7 cane node band  8 ink  9 spine  10 spine bone tip
#   11 soil  12 cane lit  13 to 15 his bark (unused, kept so a bark chunk can borrow them)
RATTAN_PALETTE = {0: "2F4219", 1: "1F2D10", 2: "4A5F26", 3: "6E6A34", 4: "34301A", 5: "221F10", 6: "D8B86E",
                   7: "7E5E2E", 8: "1E140C", 9: "130E09", 10: "C9BC98", 11: "6B4A2E", 12: "EAD49C",
                   13: "8C6440", 14: "553A22", 15: "B08450"}

# Sheaths: compass, distance out, height, girth, lean.
SHEATHS = [(14, 0.43, 0.44, 0.120, 12), (84, 0.41, 0.34, 0.108, 9), (150, 0.45, 0.50, 0.128, 14),
            (222, 0.42, 0.31, 0.104, 10), (293, 0.44, 0.40, 0.116, 13)]

# Spine collars per sheath: (angle round, height fraction, length, lean out from vertical).
SPINES = {
    0: [(0, .22, .20, 62), (60, .20, .16, 58), (118, .25, .22, 66), (185, .21, .17, 60), (245, .24, .21, 64), (305, .19, .15, 57),
        (30, .55, .18, 52), (100, .58, .21, 56), (170, .53, .16, 50), (235, .57, .19, 54), (330, .55, .17, 51),
        (70, .86, .14, 40), (200, .88, .16, 44), (290, .85, .13, 38)],
    1: [(20, .24, .18, 60), (85, .22, .21, 64), (150, .26, .16, 58), (220, .23, .19, 62), (290, .25, .17, 59),
        (50, .60, .17, 52), (130, .62, .20, 55), (210, .58, .15, 50), (320, .61, .18, 53),
        (0, .90, .12, 38), (170, .88, .14, 42)],
    2: [(8, .18, .23, 64), (68, .21, .18, 60), (132, .17, .24, 66), (195, .20, .19, 61), (258, .19, .22, 63), (322, .22, .17, 59),
        (38, .48, .20, 55), (106, .51, .17, 52), (176, .47, .22, 57), (240, .50, .18, 53), (300, .49, .21, 56),
        (70, .78, .16, 46), (150, .80, .14, 42), (230, .77, .17, 47), (330, .79, .13, 40)],
    3: [(34, .26, .17, 60), (104, .23, .20, 63), (172, .27, .16, 58), (248, .24, .19, 62), (318, .22, .15, 57),
        (68, .64, .16, 51), (205, .62, .18, 54), (290, .66, .14, 49), (140, .90, .12, 40)],
    4: [(12, .20, .21, 63), (80, .23, .17, 59), (146, .19, .22, 65), (214, .22, .18, 60), (282, .20, .20, 62), (345, .24, .16, 58),
        (48, .56, .19, 54), (128, .59, .16, 51), (202, .55, .20, 55), (300, .58, .17, 52),
        (95, .87, .14, 42), (250, .86, .15, 44)],
}

# The ruff: long black spines rising off the shared clump between the stems. (compass, radius, length, lean out, tilt)
RUFF = [(0, 0.56, 0.405, 30, 0), (33, .38, .22, 38, 8), (50, .41, .26, 26, -6), (118, .40, .28, 34, 5), (133, .37, .20, 40, -4),
        (172, .42, .32, 28, 3), (190, .39, .21, 42, -8), (248, .41, .27, 32, 6), (262, .38, .19, 44, -2), (318, .40, .29, 30, -5),
        (338, .37, .22, 38, 7), (100, .43, .18, 46, 0)]

# Fronds: rachis points (rearing up, arching over, the tip hooking down like a talon), radii, then blade pairs:
# (rachis point index, length, width, sweep off the rachis, lift). Positive sweep = right side.
FRONDS = [
    ([(0, 0, 0), (0, .26, .09), (0, .50, .24), (0, .64, .46), (0, .60, .68), (0, .44, .84), (0, .26, .88), (0, .17, .80)],
     [.056, .050, .042, .034, .026, .018, .012, .007],
     [(1, 0.48, 0.086, 36, 44), (1, 0.45, 0.081, -38, 42), (2, 0.53, 0.092, 33, 30), (2, 0.50, 0.089, -35, 28), (3, 0.48, 0.081, 30, 8),
      (3, 0.46, 0.078, -31, 6), (4, 0.36, 0.068, 28, -22), (4, 0.35, 0.065, -29, -24)]),
    ([(0, 0, 0), (0, .22, .10), (0, .42, .25), (0, .52, .45), (0, .48, .63), (0, .34, .76), (0, .19, .79), (0, .11, .72)],
     [.050, .045, .038, .030, .023, .016, .011, .007],
     [(1, 0.43, 0.078, 37, 40), (1, 0.42, 0.076, -36, 42), (2, 0.49, 0.086, 34, 26), (2, 0.48, 0.084, -33, 27), (3, 0.43, 0.076, 30, 4),
      (3, 0.42, 0.074, -30, 5), (4, 0.32, 0.062, 27, -20), (4, 0.31, 0.061, -28, -21)]),
    ([(0, 0, 0), (0, .30, .08), (0, .58, .22), (0, .76, .44), (0, .74, .68), (0, .58, .88), (0, .36, .96), (0, .22, .90), (0, .16, .80)],
     [.060, .054, .046, .037, .029, .021, .014, .009, .006],
     [(1, 0.50, 0.092, 35, 48), (1, 0.49, 0.089, -37, 46), (2, 0.57, 0.097, 32, 34), (2, 0.56, 0.095, -34, 33), (3, 0.53, 0.089, 29, 12),
      (3, 0.52, 0.086, -30, 11), (4, 0.42, 0.076, 27, -14), (4, 0.41, 0.073, -28, -15), (5, 0.28, 0.057, 25, -38), (5, 0.27, 0.054, -26, -40)]),
    ([(0, 0, 0), (0, .21, .11), (0, .38, .27), (0, .46, .46), (0, .41, .62), (0, .28, .73), (0, .15, .74), (0, .08, .67)],
     [.048, .043, .036, .029, .022, .015, .010, .006],
     [(1, 0.42, 0.076, 38, 38), (1, 0.41, 0.073, -37, 40), (2, 0.46, 0.081, 34, 24), (2, 0.45, 0.078, -35, 25), (3, 0.41, 0.072, 30, 2),
      (3, 0.39, 0.070, -31, 3), (4, 0.31, 0.059, 27, -22), (4, 0.29, 0.058, -28, -23)]),
    ([(0, 0, 0), (0, .25, .09), (0, .47, .24), (0, .60, .45), (0, .57, .66), (0, .42, .81), (0, .24, .86), (0, .14, .79)],
     [.054, .048, .041, .033, .025, .017, .011, .007],
     [(1, 0.46, 0.084, 36, 43), (1, 0.45, 0.081, -37, 41), (2, 0.52, 0.089, 33, 29), (2, 0.50, 0.086, -34, 30), (3, 0.46, 0.080, 30, 7),
      (3, 0.45, 0.077, -30, 8), (4, 0.35, 0.066, 28, -21), (4, 0.34, 0.063, -29, -20)]),
]


def blade(node, base, compass, pitch, length, width, slot):
    """A stiff sword leaflet: the six-sided prism, long and narrow, thick enough to look hard."""
    sprig(node, base, compass, pitch, length, width, slot, thickness=0.024)


def thorns():
    nodes = []
    root = Node("thorns"); nodes.append(root)
    rise = 1.0
    clump = Node("clump", parent="thorns"); nodes.append(clump)
    loop = [ring(r, a, y) for a, r, y in [(0, .41, .03), (47, .38, .05), (95, .40, .025), (140, .43, .055), (188, .39, .03),
                                           (236, .41, .045), (282, .42, .025), (328, .39, .05), (360, .41, .03), (407, .38, .05)]]
    clump.tube(spline(loop, 4), spread([.11, .10, .105, .115, .10, .108, .112, .10, .11, .10], 4), 5, sides=6)
    for a, r, w, d, yw in [(30, .56, .18, .11, 12), (120, .54, .14, .10, -20), (205, .59, .17, .12, 33), (300, .53, .15, .11, -8),
                           (75, .60, .10, .08, 40), (255, .57, .12, .09, 5)]:
        clump.obox(ring(r, a, 0.012), (w, 0.035, d), 11, yaw=yw)
    for comp, r, length, out, tilt in RUFF:
        p0 = ring(r, comp, 0.08)
        a = math.radians(comp + tilt); o = math.radians(out)
        dirv = (math.sin(a) * math.sin(o), math.cos(o), math.cos(a) * math.sin(o))
        tip = add(p0, mul(dirv, length))
        clump.tube([p0, add(p0, mul(dirv, length * 0.7)), tip], [0.030, 0.016, 0.002], 9, sides=4)
        clump.tube([add(p0, mul(dirv, length * 0.78)), tip], [0.0165, 0.002], 10, sides=4)    # a pale bone tip
    for i, (comp, dist, h, girth, lean) in enumerate(SHEATHS):
        base = ring(dist, comp, -0.06 - (1 - rise) * 0.6)
        s = Node(f"sheath-{i}", origin=base, parent="thorns", yaw=comp); nodes.append(s)
        L = math.radians(lean)
        top = (0.0, h * math.cos(L), h * math.sin(L))
        s.tube([(0, 0, 0), (0, h * 0.5 * math.cos(L), h * 0.5 * math.sin(L) - 0.01), top],
               [girth, girth * 0.93, girth * 0.78], 4 if i % 2 == 0 else 5, sides=6)
        s.obox(add(top, (0, 0.014, 0)), (girth * 1.35, 0.034, girth * 1.35), 5, yaw=17 * (i + 1))
        for ang, frac, length, out in SPINES[i]:
            a = math.radians(ang)
            p0 = (girth * 0.92 * math.sin(a), h * frac * math.cos(L), h * frac * math.sin(L) + girth * 0.92 * math.cos(a))
            o = math.radians(out)
            dirv = (math.sin(a) * math.sin(o), math.cos(o), math.cos(a) * math.sin(o))
            s.tube([p0, add(p0, mul(dirv, length))], [0.022, 0.0018], 9, sides=4)

        rach, radii, blades = FRONDS[i]
        f = Node(f"frond-{i}", origin=add(base, _yaw(top, comp)), parent="thorns", yaw=comp); nodes.append(f)
        line(f, rach, radii, 3, sides=5, per=4)
        for k, (j, length, width, sweep, lift) in enumerate(blades):
            slot = 0 if (k // 2) % 2 == 0 else 1
            blade(f, rach[j], sweep, lift, length, width, slot if sweep > 0 else 1 - slot)
        # hooked barbs down the rachis back, each typed: (point index, fraction to next, length)
        for j, fr, ln in [(1, .5, .08), (2, .3, .09), (3, .1, .085), (3, .7, .07), (4, .4, .06)]:
            if j + 1 >= len(rach):
                continue
            p = tuple(rach[j][q] + (rach[j + 1][q] - rach[j][q]) * fr for q in range(3))
            f.tube([add(p, (0, 0.03, -0.01)), add(p, (0, 0.03 + ln * 0.8, -ln * 0.6))], [0.018, 0.002], 9, sides=4)
        # the talon: the rachis tip hardens into a black hooked point
        tip = rach[-1]
        prev = rach[-2]
        tdir = unit(sub(tip, prev))
        f.tube([tip, add(tip, mul(tdir, 0.07)), add(add(tip, mul(tdir, 0.10)), (0, 0.0, -0.035))], [0.012, 0.008, 0.001], 9, sides=4)
        if True:
            whip = Node(f"whip-{i}", origin=rach[3], parent=f"frond-{i}"); nodes.append(whip)
            # the cirrus at rest: coiled under the arch like a sprung trap
            wpts = [(0, 0, 0), (0.02, -0.08, 0.08), (0.03, -0.20, 0.10), (0.02, -0.28, 0.04), (0.0, -0.25, -0.03), (-0.01, -0.18, -0.01)]
            line(whip, wpts, [0.018, 0.016, 0.014, 0.012, 0.010, 0.008], 6, sides=5, per=3)
            whip.obox((0.03, -0.20, 0.10), (0.04, 0.02, 0.04), 7)
            for hp, hd in [((0.03, -0.14, 0.10), (0.05, 0.03, 0.02)), ((0.03, -0.14, 0.10), (-0.05, 0.03, 0.02)),
                           ((0.01, -0.27, 0.02), (0.045, 0.02, 0.04)), ((0.01, -0.27, 0.02), (-0.045, 0.02, 0.04))]:
                whip.tube([hp, add(hp, hd)], [0.013, 0.0015], 9, sides=4)
    write(os.path.join(OUT, "thorns.glb"), nodes)



# ---------------------------------------------------------------------------------------------
# MARIANG MAKILING, THE SPIRIT IN HIS ULTIMATE (owner, 2026-09-26: *"make it seem like the spirit of
# maria makiling or smth is watching over him"*, Aphelios and Alune as the model, *"one place i want
# this maria makiling or smht to shhow up is his ult cutscene"*, two paintings of her as reference, and
# *"its fine if u dont make maria makiling like other characters"*). direction.md sections 5.10 and 5.13.
#
# ⚠️⚠️ v3 (2026-09-26 night): SHE WAS A COLUMN. The owner on the in-match film: *"how she looks needs to be
# refined"*, then *"pls imporv ehow the girl in his cutscene looks too"* and *"its okay if makiling doesnt
# copy or follow norms of others as long as it still has her style"*. The v2 render (`Logs/paete-review/
# paete_makiling_v2.png`, `PaeteSpiritReviewProbe`) showed why: a straight tube of gown from the chest to the
# floor, a cube head with a nose block, six stick locks of hair, and through the ghost shader one flat green
# plastic. A ghost reads by SILHOUETTE and by the light running round curved edges, and a box has neither.
#
# So she is built from ROUND, FLOWING forms (lofts of typed ellipses and flat typed ribbons, smoothed), and she
# wears what makes her Mariang Makiling to anyone from here: a BARO'T SAYA. A sheer camisa whose wide bell
# sleeves hang open under her raised forearms (piña cloth is see-through, which is what a ghost is), a folded
# PAÑUELO across her shoulders that makes the one crisp bright shape at the top of her, a long SAYA flaring into
# the mountain's mist, a darker TAPIS wrapped over it at the hips and knotted at her left, long black hair
# parted in the middle and falling in three broad locks to her knees with two curtains framing her face, and a
# wreath of SAMPAGUITA (the national flower) round her head. Her face is closed eyes, soft brows and a small
# smile, drawn on a rounded head: no nose. Through `SpiritGhost.shader` her colours become VALUE only, so the
# costume is also her light and shade: dark hair, bright camisa and pañuelo, a mid-dark tapis band that gives
# her a waist, the brightest points the flowers and the light in her hands.
#
# 3.0 m tall authored (a head is 0.45 m, about one sixth of her): tall, graceful, stylised, not the cast's
# chibi proportions, by the owner's leave. The scene stands her at 1.1 times behind his right shoulder.
#
# ⚠️ HER OWN PALETTE, NOT HIS. The runtime dresses her with `MakilingSpirit.Palette`, the same sixteen hex
# values as the table below, and the ghost shader reads only their value:
#   0 hair  1 hair lit  2 skin  3 skin shade  4 camisa  5 camisa shade  6 petal  7 flower heart
#   8 ink   9 leaf      10 light  11 pañuelo  12 saya  13 saya fold  14 tapis  15 tapis trim
# Every part below is typed with its own numbers (the owner's standing rule for Paete's world).
# Nodes the runtime moves: body, head, hair, flower-crown, arm-left, arm-right, seed.
# ---------------------------------------------------------------------------------------------
MK_HAIR, MK_HAIR_LIT, MK_SKIN, MK_SKIN_SH, MK_CAMISA, MK_CAMISA_SH, MK_PETAL, MK_HEART = 0, 1, 2, 3, 4, 5, 6, 7
MK_INK, MK_LEAF, MK_LIGHT, MK_PANUELO, MK_SAYA, MK_SAYA_FOLD, MK_TAPIS, MK_TRIM = 8, 9, 10, 11, 12, 13, 14, 15


def loft(node, sections, slot, sides=14, cap_first=True, cap_last=True):
    """A smooth closed body through typed horizontal ellipses: each section is (y, cx, cz, rx, rz).
    ⚠️ Sections are typed per part; nothing here invents a shape, it only joins the rows it is given."""
    rings = []
    for y, cx, cz, rx, rz in sections:
        rings.append([(cx + rx * math.sin(j * math.tau / sides), y, cz + rz * math.cos(j * math.tau / sides)) for j in range(sides)])
    faces = []
    for i in range(len(rings) - 1):
        axis = (sections[i][1] * 0.5 + sections[i + 1][1] * 0.5, sections[i][0] * 0.5 + sections[i + 1][0] * 0.5,
                sections[i][2] * 0.5 + sections[i + 1][2] * 0.5)
        for j in range(sides):
            k = (j + 1) % sides
            quad = [rings[i][j], rings[i][k], rings[i + 1][k], rings[i + 1][j]]
            faces.append(pv._rafi_orient(quad, sub(pv._rafi_mean(quad), axis)))
    down = (0.0, -1.0, 0.0) if sections[0][0] < sections[-1][0] else (0.0, 1.0, 0.0)
    if cap_first:
        faces.append(pv._rafi_orient(rings[0], down))
    if cap_last:
        faces.append(pv._rafi_orient(rings[-1], mul(down, -1.0)))
    node.parts.append((slot, faces))


def ribbon(node, points, widths, thickness, slot, facing, per=4):
    """A flat band along typed points (a lock of hair, a fold of cloth): at each sample a flattened hexagon
    `widths` across and `thickness` deep, its broad side turned toward `facing` (a vector, or a function of
    the sample's point). Capped. The spline is the only arithmetic between the typed points."""
    path = spline(points, per)
    ws = spread(widths, per)
    rings = []
    for i, p in enumerate(path):
        t = unit(sub(path[min(len(path) - 1, i + 1)], path[max(0, i - 1)]))
        f = facing(p) if callable(facing) else facing
        n = unit(sub(f, mul(t, dot(f, t))))
        s = unit(cross(t, n))
        w, h = ws[i] * 0.5, thickness * 0.5
        outline = [(w, 0.0), (0.62 * w, h), (-0.62 * w, h), (-w, 0.0), (-0.62 * w, -h), (0.62 * w, -h)]
        rings.append([add(p, add(mul(s, a), mul(n, b))) for a, b in outline])
    faces = []
    for i in range(len(rings) - 1):
        mid = pv._rafi_mean([path[i], path[i + 1]])
        for j in range(6):
            k = (j + 1) % 6
            quad = [rings[i][j], rings[i][k], rings[i + 1][k], rings[i + 1][j]]
            faces.append(pv._rafi_orient(quad, sub(pv._rafi_mean(quad), mid)))
    t0 = unit(sub(path[1], path[0])); t1 = unit(sub(path[-1], path[-2]))
    faces.append(pv._rafi_orient(rings[0], mul(t0, -1.0)))
    faces.append(pv._rafi_orient(rings[-1], t1))
    node.parts.append((slot, faces))


def bell(node, points, radii, wall, slot, inside_slot, sides=12, per=3):
    """A sleeve open at its mouth: an outer skin along typed points, an inner skin `wall` thinner, and the rim
    between them, so the mouth reads as cloth hanging open rather than a capped tube."""
    path = spline(points, per)
    rs = spread(radii, per)
    outer, inner = [], []
    side = None
    for i, p in enumerate(path):
        t = unit(sub(path[min(len(path) - 1, i + 1)], path[max(0, i - 1)]))
        if side is None:
            helper = (0.0, 1.0, 0.0) if abs(t[1]) < 0.9 else (1.0, 0.0, 0.0)
            side = unit(cross(t, helper))
        else:
            side = unit(sub(side, mul(t, dot(side, t))))
        up = cross(t, side)
        ro, ri = rs[i], max(0.004, rs[i] - wall)
        outer.append([add(p, add(mul(side, ro * math.cos(j * math.tau / sides)), mul(up, ro * math.sin(j * math.tau / sides)))) for j in range(sides)])
        inner.append([add(p, add(mul(side, ri * math.cos(j * math.tau / sides)), mul(up, ri * math.sin(j * math.tau / sides)))) for j in range(sides)])
    out_faces, in_faces = [], []
    for i in range(len(path) - 1):
        mid = pv._rafi_mean([path[i], path[i + 1]])
        for j in range(sides):
            k = (j + 1) % sides
            q = [outer[i][j], outer[i][k], outer[i + 1][k], outer[i + 1][j]]
            out_faces.append(pv._rafi_orient(q, sub(pv._rafi_mean(q), mid)))
            q = [inner[i][j], inner[i][k], inner[i + 1][k], inner[i + 1][j]]
            in_faces.append(pv._rafi_orient(q, sub(mid, pv._rafi_mean(q))))
    t0 = unit(sub(path[1], path[0])); t1 = unit(sub(path[-1], path[-2]))
    out_faces.append(pv._rafi_orient(outer[0], mul(t0, -1.0)))
    for j in range(sides):
        k = (j + 1) % sides
        q = [outer[-1][j], outer[-1][k], inner[-1][k], inner[-1][j]]
        out_faces.append(pv._rafi_orient(q, t1))
    node.parts.append((slot, out_faces))
    node.parts.append((inside_slot, in_faces))


def drape(node, top, bottom, thickness, slot, rows=5, belly=(0.0, 0.0, 0.0)):
    """A hanging panel of cloth between a typed TOP edge (where it is attached) and a typed BOTTOM edge (its hem),
    both the same number of points; `rows` rows between them, bellied out by `belly` at the middle, `thickness`
    deep. For the camisa's hanging sleeves: cloth that falls rather than a tube that puffs."""
    n = len(top)
    grid = []
    for r in range(rows + 1):
        u = r / rows
        bulge = math.sin(u * math.pi)
        grid.append([add(add(mul(top[i], 1.0 - u), mul(bottom[i], u)), mul(belly, bulge)) for i in range(n)])
    front, back = [], []
    for r in range(rows + 1):
        fr, bk = [], []
        for i in range(n):
            a = grid[r][max(0, i - 1)]; b = grid[r][min(n - 1, i + 1)]
            c = grid[max(0, r - 1)][i]; d = grid[min(rows, r + 1)][i]
            nrm = unit(cross(sub(b, a), sub(d, c)))
            fr.append(add(grid[r][i], mul(nrm, thickness * 0.5)))
            bk.append(add(grid[r][i], mul(nrm, -thickness * 0.5)))
        front.append(fr); back.append(bk)
    faces = []
    for r in range(rows):
        for i in range(n - 1):
            faces.append([front[r][i], front[r][i + 1], front[r + 1][i + 1], front[r + 1][i]])
            faces.append([back[r][i], back[r + 1][i], back[r + 1][i + 1], back[r][i + 1]])
    for r in range(rows):
        faces.append([front[r][0], front[r + 1][0], back[r + 1][0], back[r][0]])
        faces.append([front[r][n - 1], back[r][n - 1], back[r + 1][n - 1], front[r + 1][n - 1]])
    for i in range(n - 1):
        faces.append([front[0][i], back[0][i], back[0][i + 1], front[0][i + 1]])
        faces.append([front[rows][i], front[rows][i + 1], back[rows][i + 1], back[rows][i]])
    # Each face turned away from the nearest point of the panel's own middle surface.
    out = []
    mids = [grid[r][i] for r in range(rows + 1) for i in range(n)]
    for f in faces:
        m = pv._rafi_mean(f)
        best = min(mids, key=lambda g: sum((m[k] - g[k]) ** 2 for k in range(3)))
        away = sub(m, best)
        if dot(away, away) < 1e-12:
            away = unit(cross(sub(f[1], f[0]), sub(f[2], f[0])))
        out.append(pv._rafi_orient(f, away))
    node.parts.append((slot, out))


def on_rows(rows, a_deg, y, out=0.0):
    """The point at compass `a_deg` (0 = +Z, 90 = +X) and height `y` on a typed loft's surface, `out` metres
    proud of it: so a fold or a trim lies ON the skirt instead of at a guessed radius."""
    a = math.radians(a_deg)
    for (y0, cx0, cz0, rx0, rz0), (y1, cx1, cz1, rx1, rz1) in zip(rows, rows[1:]):
        if min(y0, y1) <= y <= max(y0, y1):
            u = (y - y0) / (y1 - y0)
            cx, cz, rx, rz = lerp(cx0, cx1, u), lerp(cz0, cz1, u), lerp(rx0, rx1, u), lerp(rz0, rz1, u)
            return (cx + (rx + out) * math.sin(a), y, cz + (rz + out) * math.cos(a))
    y0, cx, cz, rx, rz = rows[-1] if y < rows[-1][0] else rows[0]
    return (cx + (rx + out) * math.sin(a), y, cz + (rz + out) * math.cos(a))


def face_stroke(node, head_rows, a, b, width, slot, lift=0.004):
    """One ink stroke drawn ON her rounded face from (x, y) `a` to (x, y) `b`: the stroke is laid on the surface
    of the head loft at that height (its depth read off the typed rows) and turned to its tangent."""
    def front_z(x, y):
        for (y0, _, cz0, rx0, rz0), (y1, _, cz1, rx1, rz1) in zip(head_rows, head_rows[1:]):
            if y0 <= y <= y1:
                u = (y - y0) / (y1 - y0)
                rx, rz, cz = lerp(rx0, rx1, u), lerp(rz0, rz1, u), lerp(cz0, cz1, u)
                return cz + rz * math.sqrt(max(0.0, 1.0 - (x / rx) ** 2)), rx
        return head_rows[-1][2], head_rows[-1][3]
    mx, my = (a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5
    z, rx = front_z(mx, my)
    length = math.hypot(b[0] - a[0], b[1] - a[1])
    pitch = math.degrees(math.atan2(b[1] - a[1], b[0] - a[0]))
    yaw = math.degrees(math.asin(max(-0.95, min(0.95, mx / rx))))
    node.obox((mx, my, z + lift), (length + width * 0.6, width, 0.010), slot, yaw=yaw, pitch=pitch, bevel=0)


def sampaguita(node, centre, compass, tilt, size, petals, turn):
    """One sampaguita of her wreath: `petals` round petals in a pinwheel round a small cream heart, turned out
    along `compass` and tipped by `tilt`, the pinwheel rotated by `turn`. Its own numbers every call."""
    for k in range(petals):
        spin = turn + k * 360.0 / petals
        local = (size * 0.48 * math.cos(math.radians(spin)), size * 0.48 * math.sin(math.radians(spin)), 0.0)
        node.leaf(add(centre, pv._rotate(local, compass, 0.0, tilt)), size * 0.78, size * 0.58, compass, spin + 12.0, 90.0 + tilt,
                  slot=MK_PETAL, thickness=0.012)
    node.obox(add(centre, pv._rotate((0.0, 0.0, 0.012), compass, 0.0, 0.0)), (size * 0.24, size * 0.24, size * 0.22), MK_HEART, yaw=compass, bevel=0.004)


# Her head, typed row by row from the chin to the crown: (height, centre x, centre z, half width, half depth).
MK_HEAD_ROWS = [(-0.020, 0.0, 0.030, 0.046, 0.044), (0.010, 0.0, 0.026, 0.094, 0.092), (0.060, 0.0, 0.016, 0.144, 0.144),
                (0.120, 0.0, 0.008, 0.170, 0.172), (0.185, 0.0, 0.002, 0.180, 0.184), (0.250, 0.0, -0.004, 0.178, 0.184),
                (0.310, 0.0, -0.010, 0.166, 0.174), (0.365, 0.0, -0.018, 0.136, 0.146), (0.405, 0.0, -0.026, 0.084, 0.094),
                (0.425, 0.0, -0.030, 0.022, 0.024)]


def makiling():
    root = Node("makiling")
    body = Node("body", parent="makiling")
    nodes = [root, body]

    # --- THE SAYA: a long skirt from the waist, flaring into the mist and trailing a little behind her (she is
    # floating toward him). Typed rows, waist to hem.
    saya = [(1.560, 0.0, 0.000, 0.182, 0.150), (1.420, 0.0, -0.006, 0.232, 0.188), (1.200, 0.0, -0.016, 0.286, 0.236),
            (0.950, 0.0, -0.032, 0.346, 0.290), (0.680, 0.0, -0.052, 0.420, 0.354), (0.400, 0.0, -0.082, 0.518, 0.438),
            (0.140, 0.0, -0.110, 0.636, 0.536), (-0.040, 0.0, -0.132, 0.716, 0.606)]
    loft(body, saya, MK_SAYA, sides=16)
    # Six soft folds down the saya, each its own compass, start, drift round the skirt and width. ⚠️ Laid ON the
    # skirt through `on_rows` (v3a guessed a radius and in profile the folds stood off the cloth like a cage).
    for compass, drift, top, radii in [(18, -8, 1.16, [0.014, 0.020, 0.026, 0.030]), (66, 6, 1.02, [0.016, 0.022, 0.028, 0.032]),
                                       (124, 5, 1.12, [0.014, 0.020, 0.026, 0.030]), (206, -4, 0.98, [0.016, 0.024, 0.030, 0.034]),
                                       (262, -3, 1.08, [0.014, 0.022, 0.028, 0.032]), (318, 7, 1.06, [0.016, 0.020, 0.026, 0.030])]:
        heights = [top, lerp(top, 0.0, 0.34), lerp(top, 0.0, 0.68), -0.02]
        pts = [on_rows(saya, compass + drift * k / 3.0, h, -0.004) for k, h in enumerate(heights)]
        line(body, pts, radii, MK_SAYA_FOLD, sides=5, per=3)

    # --- THE TAPIS: the darker wrap over the saya from the waist to above the knee, its hem trimmed, its edge down
    # her left front, knotted at her left hip with two short tails. (v3a ran it to the knee and it swallowed a third
    # of her: the bright saya is her light, the tapis only a band that gives her a waist.)
    tapis = [(1.590, 0.0, 0.000, 0.194, 0.160), (1.430, 0.0, -0.006, 0.246, 0.200), (1.210, 0.0, -0.016, 0.300, 0.250),
             (1.040, 0.0, -0.026, 0.344, 0.290)]
    loft(body, tapis, MK_TAPIS, sides=16)
    line(body, [on_rows(tapis, a, 1.046, 0.004) for a in (0, 45, 90, 135, 180, 225, 270, 315, 360)], [0.015] * 9, MK_TRIM, sides=5, per=3)
    line(body, [on_rows(tapis, 30, 1.585, 0.006), on_rows(tapis, 32, 1.40, 0.006), on_rows(tapis, 34, 1.20, 0.006), on_rows(tapis, 35, 1.05, 0.006)],
         [0.013, 0.014, 0.015, 0.014], MK_TRIM, sides=5, per=3)
    line(body, [on_rows(tapis, a, 1.572, 0.006) for a in (0, 50, 100, 150, 200, 250, 300, 360)], [0.020] * 8, MK_TRIM, sides=5, per=3)
    knot = on_rows(tapis, 58, 1.52, 0.02)
    body.obox(knot, (0.078, 0.066, 0.058), MK_TAPIS, yaw=58.0, roll=10.0, bevel=0.018)
    ribbon(body, [add(knot, (0.004, -0.02, 0.006)), add(knot, (0.030, -0.140, 0.030)), add(knot, (0.048, -0.270, 0.040))], [0.048, 0.044, 0.028], 0.014, MK_TAPIS, (0.8, 0.0, 0.6))
    ribbon(body, [add(knot, (-0.010, -0.02, 0.012)), add(knot, (-0.004, -0.120, 0.052)), add(knot, (-0.002, -0.220, 0.070))], [0.042, 0.038, 0.024], 0.014, MK_TRIM, (0.5, 0.0, 0.9))

    # --- THE CAMISA'S BODY: fitted from the waist, the bust, up to a narrow neckline under the pañuelo.
    # ⚠️ The shoulders SLOPE UP INTO THE NECK and close there (v3a ended the bodice in a flat 0.1 m cap round the
    # neck, and through the ghost it read as a plate, a robot's collar, with the neck standing in it as a tube).
    loft(body, [(1.520, 0.0, 0.000, 0.170, 0.136), (1.640, 0.0, 0.006, 0.182, 0.142), (1.780, 0.0, 0.014, 0.200, 0.152),
                (1.900, 0.0, 0.020, 0.212, 0.160), (2.000, 0.0, 0.014, 0.212, 0.152), (2.080, 0.0, 0.002, 0.204, 0.136),
                (2.140, 0.0, -0.006, 0.176, 0.118), (2.180, 0.0, -0.006, 0.118, 0.094), (2.212, 0.0, -0.004, 0.064, 0.058)], MK_CAMISA, sides=14)
    # The neck: short and tapering, its top well inside the head (v3a's stood proud under the chin like a plug).
    line(body, [(0.0, 2.170, -0.004), (0.0, 2.225, 0.004), (0.0, 2.262, 0.010)], [0.058, 0.052, 0.044], MK_SKIN, sides=10, per=3)

    # --- THE PAÑUELO: a folded kerchief over her shoulders, its two front ends crossing to a point on her
    # chest, its third corner hanging down her back. The brightest, crispest shape on her.
    outward = lambda p: unit((p[0], 0.9, p[2] + 0.02))
    ribbon(body, [(0.000, 1.930, 0.198), (0.070, 1.995, 0.196), (0.150, 2.080, 0.160), (0.214, 2.160, 0.070), (0.222, 2.190, -0.030),
                  (0.160, 2.215, -0.110), (0.000, 2.230, -0.140)], [0.024, 0.070, 0.118, 0.150, 0.146, 0.120, 0.100], 0.022, MK_PANUELO, outward)
    ribbon(body, [(0.000, 1.930, 0.206), (-0.070, 1.992, 0.200), (-0.150, 2.078, 0.164), (-0.214, 2.158, 0.072), (-0.222, 2.188, -0.028),
                  (-0.160, 2.213, -0.108), (0.000, 2.228, -0.138)], [0.024, 0.068, 0.116, 0.148, 0.144, 0.118, 0.098], 0.022, MK_PANUELO, outward)
    ribbon(body, [(0.000, 2.200, -0.150), (0.000, 2.080, -0.176), (0.000, 1.960, -0.190), (0.000, 1.840, -0.196)],
           [0.380, 0.300, 0.170, 0.030], 0.020, MK_PANUELO, (0.0, 0.2, -1.0))
    # Its embroidered edge (the one line of pattern on her), and a sampaguita pinned where the ends cross.
    line(body, [(0.000, 1.915, 0.214), (0.090, 2.005, 0.208), (0.176, 2.098, 0.168), (0.246, 2.170, 0.060)], [0.008, 0.008, 0.008, 0.008], MK_CAMISA_SH, sides=4, per=3)
    line(body, [(0.000, 1.915, 0.220), (-0.090, 2.003, 0.212), (-0.176, 2.096, 0.172), (-0.246, 2.168, 0.062)], [0.008, 0.008, 0.008, 0.008], MK_CAMISA_SH, sides=4, per=3)
    sampaguita(body, (0.0, 1.938, 0.226), 0.0, 10.0, 0.068, 7, 8.0)

    # --- THE ARMS, on their own nodes so she can reach and part her hands. Each: the camisa's sleeve fitted round
    # the arm and forearm (a little puffed at the shoulder), a rounded hand (no fingers, the cast's rule), and the
    # long ANGEL SLEEVE hanging open from the forearm to her hip. ⚠️ v3a hung a round bell on each arm and they
    # read as two balloons; sheer cloth that FALLS is what gives a spirit her flowing silhouette.
    for name, sx in (("arm-left", 1.0), ("arm-right", -1.0)):
        arm = Node(name, origin=(0.215 * sx, 2.100, -0.010), parent="body"); nodes.append(arm)
        line(arm, [(0.004 * sx, 0.010, 0.000), (0.040 * sx, -0.110, 0.024), (0.070 * sx, -0.260, 0.080)], [0.086, 0.074, 0.060], MK_CAMISA, sides=10, per=3)
        line(arm, [(0.070 * sx, -0.260, 0.080), (-0.020 * sx, -0.200, 0.190), (-0.132 * sx, -0.128, 0.292)], [0.060, 0.058, 0.056], MK_CAMISA, sides=10, per=3)
        line(arm, [(-0.126 * sx, -0.132, 0.286), (-0.160 * sx, -0.116, 0.312)], [0.036, 0.034], MK_SKIN, sides=7, per=2)
        arm.obox((-0.190 * sx, -0.108, 0.334), (0.078, 0.034, 0.104), MK_SKIN, yaw=-38.0 * sx, roll=-10.0, pitch=16.0 * sx, bevel=0.016)
        arm.obox((-0.168 * sx, -0.092, 0.322), (0.030, 0.058, 0.090), MK_SKIN_SH, yaw=-38.0 * sx, pitch=16.0 * sx, bevel=0.010)
        # The cuff: the camisa's embroidered edge at the wrist.
        line(arm, [(-0.126 * sx, -0.132 + 0.062 * math.sin(math.radians(a)), 0.286 + 0.062 * math.cos(math.radians(a)) * 0.6) for a in range(0, 361, 45)],
             [0.010] * 9, MK_CAMISA_SH, sides=4, per=3)
        # The hanging sleeve: attached under the forearm from the elbow to the wrist, falling to her hip, its hem
        # wider and swinging back, bellied out a little. Typed edges.
        top = [(0.074 * sx, -0.300, 0.070), (0.030 * sx, -0.262, 0.150), (-0.040 * sx, -0.214, 0.222), (-0.110 * sx, -0.170, 0.280)]
        hem = [(0.130 * sx, -0.700, -0.020), (0.090 * sx, -0.760, 0.090), (0.020 * sx, -0.780, 0.190), (-0.060 * sx, -0.720, 0.270)]
        drape(arm, top, hem, 0.018, MK_CAMISA, rows=5, belly=(0.070 * sx, 0.0, 0.020))
        line(arm, [add(p, (0.0, -0.006, 0.0)) for p in hem], [0.010, 0.011, 0.011, 0.010], MK_CAMISA_SH, sides=4, per=3)

    # --- THE LIGHT she gives him, cupped in her hands.
    seed = Node("seed", origin=(0.0, 2.020, 0.345), parent="body"); nodes.append(seed)
    seed.obox((0.0, 0.0, 0.0), (0.085, 0.085, 0.085), MK_LIGHT, yaw=45.0, roll=35.0, bevel=0.018)

    # --- THE HEAD: rounded, on its own node so it bows. A calm face: closed eyes, soft brows, a small smile.
    head = Node("head", origin=(0.0, 2.272, 0.010), parent="body"); nodes.append(head)
    loft(head, MK_HEAD_ROWS, MK_SKIN, sides=16)
    # Closed eyes, each a lowered lid curving down (three strokes) with two short lashes at its outer end.
    for sx in (1.0, -1.0):
        face_stroke(head, MK_HEAD_ROWS, (0.036 * sx, 0.190), (0.066 * sx, 0.178), 0.013, MK_INK)
        face_stroke(head, MK_HEAD_ROWS, (0.066 * sx, 0.178), (0.094 * sx, 0.182), 0.013, MK_INK)
        face_stroke(head, MK_HEAD_ROWS, (0.094 * sx, 0.182), (0.112 * sx, 0.194), 0.012, MK_INK)
        face_stroke(head, MK_HEAD_ROWS, (0.104 * sx, 0.186), (0.118 * sx, 0.170), 0.008, MK_INK)
        face_stroke(head, MK_HEAD_ROWS, (0.090 * sx, 0.180), (0.098 * sx, 0.164), 0.008, MK_INK)
        # A soft brow, high and gently arched: calm, not stern.
        face_stroke(head, MK_HEAD_ROWS, (0.040 * sx, 0.246), (0.074 * sx, 0.256), 0.009, MK_HAIR)
        face_stroke(head, MK_HEAD_ROWS, (0.074 * sx, 0.256), (0.108 * sx, 0.250), 0.009, MK_HAIR)
    # The smile: small, curving up at both corners.
    face_stroke(head, MK_HEAD_ROWS, (-0.034, 0.090), (-0.012, 0.080), 0.010, MK_INK)
    face_stroke(head, MK_HEAD_ROWS, (-0.012, 0.080), (0.012, 0.080), 0.010, MK_INK)
    face_stroke(head, MK_HEAD_ROWS, (0.012, 0.080), (0.034, 0.090), 0.010, MK_INK)

    # --- HER HAIR. The cap over the crown and back of the head, a little proud of it.
    loft(head, [(0.270, 0.0, -0.030, 0.186, 0.182), (0.320, 0.0, -0.016, 0.182, 0.190), (0.370, 0.0, -0.020, 0.154, 0.166),
                (0.415, 0.0, -0.028, 0.104, 0.114), (0.450, 0.0, -0.034, 0.036, 0.040)], MK_HAIR, sides=16)
    # The fringe, parted in the middle and swept to each temple.
    for sx in (1.0, -1.0):
        ribbon(head, [(0.012 * sx, 0.440, 0.110), (0.080 * sx, 0.408, 0.158), (0.140 * sx, 0.340, 0.158), (0.182 * sx, 0.262, 0.112)],
               [0.050, 0.094, 0.082, 0.040], 0.040, MK_HAIR, lambda p: unit((p[0], p[1] - 0.20, p[2] + 0.02)))
    ribbon(head, [(0.100, 0.392, 0.164), (0.146, 0.330, 0.162), (0.176, 0.268, 0.124)], [0.030, 0.034, 0.018], 0.020, MK_HAIR_LIT,
           lambda p: unit((p[0], p[1] - 0.20, p[2] + 0.04)))
    # Two curtains falling past her cheeks, over her collarbones, to her breast: they frame the face. Each with its own
    # soft wave, resting out on the shoulder and tucking in below it.
    ribbon(head, [(0.176, 0.300, 0.070), (0.198, 0.160, 0.092), (0.212, 0.010, 0.116), (0.228, -0.150, 0.140), (0.214, -0.320, 0.150), (0.182, -0.480, 0.156), (0.160, -0.580, 0.150)],
           [0.090, 0.112, 0.114, 0.104, 0.092, 0.060, 0.020], 0.040, MK_HAIR, lambda p: unit((1.0, 0.0, 0.40)))
    ribbon(head, [(-0.176, 0.300, 0.072), (-0.196, 0.150, 0.096), (-0.214, -0.004, 0.118), (-0.226, -0.170, 0.138), (-0.210, -0.340, 0.148), (-0.186, -0.500, 0.150), (-0.168, -0.600, 0.144)],
           [0.090, 0.110, 0.112, 0.102, 0.090, 0.058, 0.018], 0.040, MK_HAIR, lambda p: unit((-1.0, 0.0, 0.40)))
    ribbon(head, [(0.216, 0.080, 0.130), (0.232, -0.090, 0.152), (0.222, -0.260, 0.164), (0.200, -0.380, 0.166)], [0.030, 0.032, 0.024, 0.008], 0.020, MK_HAIR_LIT, lambda p: unit((1.0, 0.0, 0.5)))

    # --- THE LONG HAIR down her back, its own node so it sways: three broad locks side by side, each its own
    # length and wave, and one lighter lock over them.
    hair = Node("hair", origin=(0.0, 0.320, -0.140), parent="head"); nodes.append(hair)
    back = (0.0, 0.0, -1.0)
    # The middle lock: the broadest, an S down her back to below the knee.
    ribbon(hair, [(0.000, 0.080, 0.000), (0.006, -0.160, -0.074), (-0.030, -0.520, -0.126), (0.024, -0.920, -0.164), (-0.020, -1.320, -0.180), (0.014, -1.640, -0.172), (-0.006, -1.860, -0.150)],
           [0.160, 0.196, 0.210, 0.200, 0.176, 0.130, 0.036], 0.056, MK_HAIR, back)
    # Her left and right locks, overlapping the middle one so the back reads as ONE sheet of hair with its grooves
    # (v3b's three locks read as separate planks from behind), each its own length and wave.
    ribbon(hair, [(0.100, 0.050, 0.014), (0.130, -0.170, -0.046), (0.150, -0.540, -0.098), (0.140, -0.940, -0.128), (0.162, -1.300, -0.138), (0.140, -1.560, -0.128), (0.124, -1.700, -0.114)],
           [0.140, 0.170, 0.180, 0.170, 0.140, 0.090, 0.022], 0.050, MK_HAIR, lambda p: unit((0.5, 0.0, -1.0)))
    ribbon(hair, [(-0.100, 0.050, 0.014), (-0.132, -0.180, -0.046), (-0.146, -0.580, -0.102), (-0.164, -1.000, -0.132), (-0.140, -1.380, -0.142), (-0.156, -1.640, -0.130), (-0.140, -1.780, -0.114)],
           [0.140, 0.170, 0.180, 0.170, 0.136, 0.086, 0.020], 0.050, MK_HAIR, lambda p: unit((-0.5, 0.0, -1.0)))
    # One lighter lock over the middle, where the light catches her hair.
    ribbon(hair, [(0.060, 0.030, -0.030), (0.086, -0.300, -0.112), (0.048, -0.740, -0.168), (0.092, -1.160, -0.198), (0.070, -1.480, -0.196)],
           [0.050, 0.062, 0.066, 0.052, 0.014], 0.028, MK_HAIR_LIT, back)

    # --- THE WREATH of sampaguita round her head, lower at the brow and higher behind, on a thin green vine,
    # seven flowers and two buds, each its own size, tilt and turn, and four leaves.
    crown = Node("flower-crown", origin=(0.0, 0.0, 0.0), parent="head"); nodes.append(crown)
    vine = [(0.000, 0.352, 0.212), (0.150, 0.366, 0.150), (0.212, 0.384, 0.000), (0.150, 0.404, -0.160), (0.000, 0.414, -0.218),
            (-0.150, 0.404, -0.160), (-0.212, 0.384, 0.000), (-0.150, 0.366, 0.150), (0.000, 0.352, 0.212)]
    line(crown, vine, [0.012] * len(vine), MK_LEAF, sides=4, per=3)
    for compass, size, petals, turn, tilt in [(0, 0.082, 8, 6.0, -8.0), (40, 0.070, 7, 20.0, -14.0), (84, 0.076, 8, 2.0, -18.0),
                                              (128, 0.064, 7, 31.0, -22.0), (-44, 0.072, 7, 14.0, -12.0), (-88, 0.078, 8, 25.0, -18.0),
                                              (-134, 0.062, 7, 9.0, -22.0)]:
        a = math.radians(compass)
        r = 0.214
        y = 0.352 + 0.062 * (1.0 - math.cos(a)) * 0.5
        sampaguita(crown, (r * math.sin(a), y, r * math.cos(a)), float(compass), tilt, size, petals, turn)
    for compass, length in [(22, 0.040), (-66, 0.036)]:
        a = math.radians(compass)
        crown.obox((0.216 * math.sin(a), 0.360 + 0.030 * (1.0 - math.cos(a)), 0.216 * math.cos(a)), (0.028, 0.024, length), MK_PETAL,
                   yaw=float(compass), bevel=0.010)
    # Four small leaves tucked along the vine (v3a's stood out from her temples like feelers).
    sprig(crown, (0.190, 0.372, 0.092), 150, -30, 0.058, 0.032, MK_LEAF)
    sprig(crown, (-0.186, 0.374, 0.098), -150, -28, 0.054, 0.030, MK_LEAF)
    sprig(crown, (0.170, 0.396, -0.124), 40, -32, 0.052, 0.028, MK_LEAF)
    sprig(crown, (-0.172, 0.394, -0.120), -44, -30, 0.056, 0.030, MK_LEAF)

    write(os.path.join(OUT, "makiling.glb"), nodes)


# ---------------------------------------------------------------------------------------------
# ⚠️⚠️ HER MEADOW (owner, 2026-09-26 night): *"add in ult cutscene of paete that when maria makiling starts
# coming into the pic flowers start sprouting and lushh greenery and plants and shit (pls dotn reuse existing
# models)"*, *"and they disappear slowly as she disappears thoroughly direct it"*. direction.md section 5.13.
#
# Where the mountain's spirit stands, the mountain comes up through the plaza: a carpet of moss runs over the
# stone, grass springs up, fern fiddleheads push up and UNROLL, sampaguita buds open white (her wreath's own
# flower), and makahiya (Mimosa pudica, the "shy" plant every Filipino child has touched) spreads its feathery
# leaves and pink puffs. All of it NEW, typed here plant by plant: nothing reuses the tree's, the pitcher's or
# the rattan's parts. The cutscene grows it as a wave out from her, flinches it from his slam (the makahiya
# fold shut at the touch, as the real plant does), and lets it wilt and sink from the outer edge in as she goes.
#
# One glb, `meadow.glb`, laid out in the caster's space round her stand (0.78, -1.30) and his (0, 0), clear of
# the court between him and the landing where the veins run. Every plant is its own node at its own ground
# point; the parts that move are their own nodes: fern fronds in three chained segments (the runtime curls
# them), sampaguita blossoms, makahiya leaves and puffs.
#
# ⚠️ ITS OWN PALETTE (`PaeteMeadow.Palette` in C#, the same hex values):
#   0 moss   1 moss dark  2 grass   3 grass dark  4 fern   5 fern light  6 petal  7 flower heart
#   8 ink    9 stem       10 makahiya pink  11 makahiya tip  12 makahiya leaf  13 soil  14 bud  15 leaf dark
# ---------------------------------------------------------------------------------------------
MD_MOSS, MD_MOSS_DK, MD_GRASS, MD_GRASS_DK, MD_FERN, MD_FERN_LT, MD_PETAL, MD_HEART = 0, 1, 2, 3, 4, 5, 6, 7
MD_INK, MD_STEM, MD_PINK, MD_PINK_TIP, MD_MIMOSA, MD_SOIL, MD_BUD, MD_LEAF_DK = 8, 9, 10, 11, 12, 13, 14, 15


def moss_patch(node, outline, height, slot):
    """A flat cushion of moss on the stone: a typed outline of (compass, radius) points, `height` thick,
    its rim bevelled down to the court."""
    top = [(r * 0.86 * math.sin(math.radians(a)), height, r * 0.86 * math.cos(math.radians(a))) for a, r in outline]
    rim = [(r * math.sin(math.radians(a)), height * 0.35, r * math.cos(math.radians(a))) for a, r in outline]
    foot = [(r * 1.04 * math.sin(math.radians(a)), 0.0, r * 1.04 * math.cos(math.radians(a))) for a, r in outline]
    faces = [pv._rafi_orient(top, (0.0, 1.0, 0.0)), pv._rafi_orient(foot, (0.0, -1.0, 0.0))]
    n = len(outline)
    for i in range(n):
        k = (i + 1) % n
        for a_ring, b_ring in ((top, rim), (rim, foot)):
            q = [a_ring[i], a_ring[k], b_ring[k], b_ring[i]]
            faces.append(pv._rafi_orient(q, (q[0][0] + q[1][0], 0.0, q[0][2] + q[1][2])))
    node.parts.append((slot, faces))


def grass_tuft(node, blades):
    """A tuft of grass: each blade (compass, pitch up from the court, length, width, slot) typed."""
    for compass, pitch, length, width, slot in blades:
        sprig(node, (0.0, 0.0, 0.0), compass, pitch, length, width, slot, thickness=0.010)


def frond(nodes, fern, name, compass, segments):
    """One fern frond in three chained segments (so it can unroll): `segments` is a typed list of three
    (length, rise in degrees, pinnae lengths) rows. Each segment is a node whose origin is the end of the one
    before, turned to `compass`; its stalk rises by its own angle and carries its pinnae left and right."""
    parent, origin = fern.name, (0.0, 0.0, 0.0)
    for s, (length, rise, pinnae) in enumerate(segments):
        seg = Node(f"{name}-{'abc'[s]}", origin=origin, parent=parent, yaw=compass if s == 0 else 0.0)
        nodes.append(seg)
        end = (0.0, length * math.sin(math.radians(rise)), length * math.cos(math.radians(rise)))
        seg.tube([(0.0, 0.0, 0.0), mul(end, 0.5), end], [0.012 - 0.003 * s, 0.010 - 0.003 * s, 0.008 - 0.003 * s], MD_STEM, sides=4)
        for k, pl in enumerate(pinnae):
            u = (k + 0.6) / (len(pinnae) + 0.4)
            at = mul(end, u)
            for side in (1.0, -1.0):
                # Each pinna leans a little forward along the frond and droops, its own length.
                seg.leaf(add(at, (side * pl * 0.48, -pl * 0.10, pl * 0.12)), pl, pl * 0.34, 90.0 * side - 12.0 * side, -8.0, 0.0,
                         slot=MD_FERN if (k + s) % 2 == 0 else MD_FERN_LT, thickness=0.008)
        parent, origin = seg.name, end


def blossom_node(nodes, parent, name, at, size, petals, turn, tilt):
    b = Node(name, origin=at, parent=parent); nodes.append(b)
    sampaguita_bloom(b, (0.0, 0.0, 0.0), tilt, size, petals, turn)
    return b


def sampaguita_bloom(node, centre, tilt, size, petals, turn):
    """A sampaguita facing up (tipped by `tilt`): petals in a pinwheel, a small pale heart."""
    for k in range(petals):
        spin = turn + k * 360.0 / petals
        local = (size * 0.46 * math.cos(math.radians(spin)), 0.0, size * 0.46 * math.sin(math.radians(spin)))
        node.leaf(add(centre, pv._rotate(local, 0.0, 0.0, tilt)), size * 0.74, size * 0.52, -spin, 8.0, tilt, slot=MD_PETAL, thickness=0.010)
    node.obox(add(centre, (0.0, 0.010, 0.0)), (size * 0.22, size * 0.16, size * 0.22), MD_HEART, bevel=0.004)


def puff(node, centre, size, stamens):
    """A makahiya flower head: a pink ball with `stamens` typed (compass, elevation) rays tipped pale."""
    node.obox(centre, (size * 0.5, size * 0.5, size * 0.5), MD_PINK, yaw=30.0, roll=20.0, bevel=size * 0.12)
    for compass, elevation in stamens:
        d = (math.cos(math.radians(elevation)) * math.sin(math.radians(compass)), math.sin(math.radians(elevation)),
             math.cos(math.radians(elevation)) * math.cos(math.radians(compass)))
        tip = add(centre, mul(d, size))
        node.tube([add(centre, mul(d, size * 0.2)), tip], [0.004, 0.003], MD_PINK, sides=3)
        node.obox(tip, (0.010, 0.010, 0.010), MD_PINK_TIP, bevel=0.002)


def mimosa_leaf(node, length, rise, pairs):
    """A makahiya leaf along its node's +Z: a stalk rising by `rise` and `pairs` typed leaflet sizes each side."""
    end = (0.0, length * math.sin(math.radians(rise)), length * math.cos(math.radians(rise)))
    node.tube([(0.0, 0.0, 0.0), end], [0.006, 0.004], MD_STEM, sides=4)
    for k, size in enumerate(pairs):
        at = mul(end, (k + 1.0) / (len(pairs) + 0.6))
        for side in (1.0, -1.0):
            node.leaf(add(at, (side * size * 0.5, 0.004, 0.0)), size, size * 0.42, 90.0 * side, 4.0, 0.0, slot=MD_MIMOSA, thickness=0.006)


def meadow():
    root = Node("meadow")
    nodes = [root]

    # --- MOSS, the first thing she brings: cushions spreading over the stone, one under her, one under him.
    # Each: name, ground point, typed outline (compass, radius), thickness, shade.
    for name, at, outline, height, slot in [
        ("moss-0", (0.78, 0.0, -1.30), [(0, 1.30), (40, 1.12), (85, 1.42), (130, 1.05), (175, 1.36), (220, 1.18), (265, 1.46), (310, 1.10)], 0.050, MD_MOSS),
        ("moss-1", (0.10, 0.0, 0.10), [(10, 0.82), (60, 0.66), (110, 0.90), (160, 0.72), (210, 0.86), (260, 0.64), (310, 0.80)], 0.042, MD_MOSS_DK),
        ("moss-2", (1.62, 0.0, 0.92), [(0, 0.76), (55, 0.92), (105, 0.62), (150, 0.84), (205, 0.70), (255, 0.88), (305, 0.66)], 0.040, MD_MOSS),
        ("moss-3", (-0.90, 0.0, -0.84), [(20, 0.70), (75, 0.58), (130, 0.78), (190, 0.64), (240, 0.74), (300, 0.56)], 0.038, MD_MOSS_DK),
        ("moss-4", (1.92, 0.0, -2.20), [(0, 0.86), (50, 0.70), (100, 0.94), (150, 0.76), (200, 0.88), (250, 0.68), (300, 0.90)], 0.044, MD_MOSS),
        ("moss-5", (0.34, 0.0, 1.62), [(15, 0.60), (70, 0.74), (125, 0.56), (180, 0.70), (235, 0.62), (290, 0.72)], 0.036, MD_MOSS_DK),
    ]:
        n = Node(name, origin=at, parent="meadow"); nodes.append(n)
        moss_patch(n, outline, height, slot)

    # --- GRASS: ten tufts, each blade typed (compass, pitch, length, width, shade).
    for name, at, blades in [
        ("grass-0", (1.30, 0.0, 0.40), [(0, 72, 0.34, 0.040, MD_GRASS), (70, 64, 0.28, 0.036, MD_GRASS_DK), (140, 76, 0.38, 0.042, MD_GRASS), (210, 60, 0.26, 0.034, MD_GRASS_DK), (285, 70, 0.32, 0.038, MD_GRASS)]),
        ("grass-1", (1.92, 0.0, 1.62), [(20, 68, 0.30, 0.038, MD_GRASS_DK), (95, 74, 0.36, 0.040, MD_GRASS), (170, 62, 0.27, 0.035, MD_GRASS), (250, 70, 0.33, 0.039, MD_GRASS_DK), (320, 78, 0.40, 0.042, MD_GRASS)]),
        ("grass-2", (0.88, 0.0, 2.24), [(40, 66, 0.26, 0.034, MD_GRASS), (120, 74, 0.33, 0.038, MD_GRASS_DK), (200, 70, 0.30, 0.036, MD_GRASS), (300, 62, 0.24, 0.032, MD_GRASS_DK)]),
        ("grass-3", (2.62, 0.0, 0.18), [(10, 76, 0.38, 0.042, MD_GRASS), (80, 64, 0.30, 0.036, MD_GRASS_DK), (150, 70, 0.34, 0.040, MD_GRASS), (225, 60, 0.27, 0.034, MD_GRASS), (300, 72, 0.36, 0.040, MD_GRASS_DK), (350, 66, 0.29, 0.035, MD_GRASS)]),
        ("grass-4", (-0.62, 0.0, -1.62), [(30, 70, 0.32, 0.038, MD_GRASS_DK), (110, 76, 0.38, 0.041, MD_GRASS), (190, 64, 0.28, 0.035, MD_GRASS), (270, 72, 0.34, 0.039, MD_GRASS_DK)]),
        ("grass-5", (1.60, 0.0, -2.62), [(0, 64, 0.29, 0.036, MD_GRASS), (60, 74, 0.35, 0.040, MD_GRASS_DK), (130, 68, 0.31, 0.037, MD_GRASS), (200, 78, 0.40, 0.043, MD_GRASS), (275, 62, 0.27, 0.034, MD_GRASS_DK)]),
        ("grass-6", (2.42, 0.0, -1.20), [(25, 72, 0.33, 0.039, MD_GRASS), (100, 66, 0.29, 0.036, MD_GRASS_DK), (175, 74, 0.36, 0.041, MD_GRASS), (255, 60, 0.26, 0.033, MD_GRASS_DK), (330, 70, 0.31, 0.037, MD_GRASS)]),
        ("grass-7", (-1.28, 0.0, -0.18), [(45, 68, 0.30, 0.037, MD_GRASS_DK), (125, 74, 0.35, 0.040, MD_GRASS), (215, 62, 0.27, 0.034, MD_GRASS), (310, 72, 0.33, 0.038, MD_GRASS_DK)]),
        ("grass-8", (0.40, 0.0, 1.10), [(5, 60, 0.24, 0.032, MD_GRASS), (90, 70, 0.29, 0.036, MD_GRASS_DK), (180, 66, 0.26, 0.034, MD_GRASS), (265, 74, 0.31, 0.037, MD_GRASS)]),
        ("grass-9", (2.24, 0.0, 2.62), [(35, 70, 0.31, 0.037, MD_GRASS_DK), (115, 64, 0.27, 0.034, MD_GRASS), (195, 76, 0.36, 0.040, MD_GRASS), (280, 68, 0.30, 0.036, MD_GRASS_DK)]),
    ]:
        n = Node(name, origin=at, parent="meadow"); nodes.append(n)
        grass_tuft(n, blades)

    # --- FERNS round her hem and behind him, tallest nearest her: each frond typed as three segments of
    # (length, rise, pinnae lengths), unrolled at rest; the cutscene curls them into fiddleheads and lets them go.
    for name, at, fronds in [
        ("fern-0", (0.10, 0.0, -2.30), [(20, [(0.30, 58, [0.10, 0.12, 0.13]), (0.26, 34, [0.12, 0.11, 0.10]), (0.20, 4, [0.08, 0.06, 0.04])]),
                                         (95, [(0.28, 62, [0.09, 0.11, 0.12]), (0.24, 38, [0.11, 0.10, 0.09]), (0.18, 8, [0.07, 0.05, 0.03])]),
                                         (170, [(0.32, 55, [0.10, 0.12, 0.14]), (0.27, 30, [0.13, 0.12, 0.10]), (0.21, 0, [0.08, 0.06, 0.04])]),
                                         (245, [(0.27, 60, [0.09, 0.11, 0.12]), (0.23, 36, [0.11, 0.10, 0.08]), (0.17, 6, [0.07, 0.05, 0.03])]),
                                         (320, [(0.30, 57, [0.10, 0.12, 0.13]), (0.25, 32, [0.12, 0.11, 0.09]), (0.19, 2, [0.08, 0.06, 0.04])])]),
        ("fern-1", (1.72, 0.0, -1.88), [(0, [(0.34, 60, [0.11, 0.13, 0.15]), (0.29, 36, [0.14, 0.13, 0.11]), (0.23, 6, [0.09, 0.07, 0.04])]),
                                         (72, [(0.31, 56, [0.10, 0.12, 0.14]), (0.27, 32, [0.13, 0.12, 0.10]), (0.21, 2, [0.08, 0.06, 0.04])]),
                                         (140, [(0.36, 62, [0.11, 0.14, 0.15]), (0.30, 38, [0.14, 0.13, 0.11]), (0.24, 8, [0.09, 0.07, 0.04])]),
                                         (215, [(0.30, 58, [0.10, 0.12, 0.13]), (0.26, 34, [0.12, 0.11, 0.09]), (0.20, 4, [0.08, 0.06, 0.03])]),
                                         (290, [(0.33, 54, [0.11, 0.13, 0.14]), (0.28, 30, [0.13, 0.12, 0.10]), (0.22, 0, [0.09, 0.06, 0.04])])]),
        ("fern-2", (-0.72, 0.0, -1.12), [(40, [(0.26, 60, [0.09, 0.10, 0.11]), (0.22, 36, [0.10, 0.09, 0.08]), (0.16, 6, [0.07, 0.05, 0.03])]),
                                          (130, [(0.28, 56, [0.09, 0.11, 0.12]), (0.23, 32, [0.11, 0.10, 0.08]), (0.17, 2, [0.07, 0.05, 0.03])]),
                                          (220, [(0.25, 62, [0.08, 0.10, 0.11]), (0.21, 38, [0.10, 0.09, 0.07]), (0.15, 8, [0.06, 0.05, 0.03])]),
                                          (310, [(0.27, 58, [0.09, 0.11, 0.12]), (0.22, 34, [0.11, 0.10, 0.08]), (0.16, 4, [0.07, 0.05, 0.03])])]),
        ("fern-3", (2.30, 0.0, -0.62), [(15, [(0.29, 58, [0.10, 0.11, 0.13]), (0.24, 34, [0.12, 0.11, 0.09]), (0.18, 4, [0.07, 0.06, 0.03])]),
                                         (105, [(0.27, 62, [0.09, 0.11, 0.12]), (0.23, 38, [0.11, 0.10, 0.08]), (0.17, 8, [0.07, 0.05, 0.03])]),
                                         (190, [(0.30, 55, [0.10, 0.12, 0.13]), (0.25, 30, [0.12, 0.11, 0.09]), (0.19, 0, [0.08, 0.06, 0.04])]),
                                         (275, [(0.28, 60, [0.09, 0.11, 0.12]), (0.23, 36, [0.11, 0.10, 0.08]), (0.17, 6, [0.07, 0.05, 0.03])])]),
        ("fern-4", (-1.20, 0.0, -2.02), [(30, [(0.31, 57, [0.10, 0.12, 0.13]), (0.26, 33, [0.12, 0.11, 0.09]), (0.20, 3, [0.08, 0.06, 0.04])]),
                                          (115, [(0.29, 61, [0.09, 0.11, 0.13]), (0.24, 37, [0.12, 0.10, 0.09]), (0.18, 7, [0.07, 0.05, 0.03])]),
                                          (205, [(0.32, 55, [0.10, 0.12, 0.14]), (0.27, 31, [0.13, 0.12, 0.10]), (0.21, 1, [0.08, 0.06, 0.04])]),
                                          (295, [(0.28, 59, [0.09, 0.11, 0.12]), (0.24, 35, [0.11, 0.10, 0.08]), (0.18, 5, [0.07, 0.05, 0.03])])]),
    ]:
        n = Node(name, origin=at, parent="meadow"); nodes.append(n)
        # a tight crown of old stalk bases where the fronds leave the ground
        n.obox((0.0, 0.03, 0.0), (0.10, 0.06, 0.10), MD_MOSS_DK, yaw=20.0, bevel=0.02)
        for i, (compass, segs) in enumerate(fronds):
            frond(nodes, n, f"{name}-f{i}", float(compass), segs)

    # --- SAMPAGUITA bushes: a mound of dark glossy leaves (each typed: compass, pitch, length), white blossoms
    # that open on their own nodes (position, size, petals, turn, tilt), and a bud or two.
    for name, at, leaves, blooms, buds in [
        ("samp-0", (1.48, 0.0, 1.30), [(0, 38, 0.16, 0.08), (55, 30, 0.14, 0.07), (110, 42, 0.17, 0.08), (165, 26, 0.13, 0.07), (220, 36, 0.15, 0.08), (280, 44, 0.16, 0.08), (330, 28, 0.14, 0.07)],
         [((0.04, 0.24, 0.02), 0.070, 8, 5.0, -6.0), ((-0.08, 0.20, 0.06), 0.060, 7, 22.0, 10.0), ((0.07, 0.19, -0.08), 0.064, 8, 13.0, -14.0)], [(-0.02, 0.22, -0.07)]),
        ("samp-1", (0.62, 0.0, 1.94), [(20, 34, 0.15, 0.08), (85, 40, 0.16, 0.08), (150, 28, 0.13, 0.07), (215, 38, 0.15, 0.08), (290, 32, 0.14, 0.07)],
         [((0.02, 0.21, 0.03), 0.066, 8, 9.0, 8.0), ((-0.07, 0.18, -0.05), 0.058, 7, 30.0, -10.0)], [(0.06, 0.20, 0.06)]),
        ("samp-2", (2.52, 0.0, -1.92), [(10, 40, 0.17, 0.08), (70, 32, 0.15, 0.07), (135, 44, 0.18, 0.09), (200, 28, 0.14, 0.07), (260, 38, 0.16, 0.08), (320, 34, 0.15, 0.08)],
         [((0.03, 0.25, 0.01), 0.072, 8, 17.0, -4.0), ((-0.09, 0.21, 0.05), 0.062, 7, 2.0, 12.0), ((0.08, 0.20, -0.07), 0.066, 8, 26.0, -12.0)], [(-0.04, 0.23, -0.06)]),
        ("samp-3", (-0.92, 0.0, -2.42), [(30, 36, 0.15, 0.08), (100, 42, 0.16, 0.08), (170, 30, 0.14, 0.07), (240, 38, 0.15, 0.08), (310, 34, 0.14, 0.07)],
         [((0.01, 0.22, 0.02), 0.068, 8, 11.0, 6.0), ((0.08, 0.19, -0.05), 0.060, 7, 24.0, -8.0)], [(-0.06, 0.20, 0.04)]),
        ("samp-4", (1.20, 0.0, -0.32), [(15, 34, 0.14, 0.07), (80, 40, 0.15, 0.08), (145, 30, 0.13, 0.07), (215, 38, 0.15, 0.08), (285, 32, 0.14, 0.07)],
         [((0.02, 0.20, 0.02), 0.062, 8, 7.0, -6.0), ((-0.06, 0.17, 0.06), 0.056, 7, 19.0, 10.0)], []),
    ]:
        n = Node(name, origin=at, parent="meadow"); nodes.append(n)
        for i, (compass, pitch, length, width) in enumerate(leaves):
            sprig(n, (0.0, 0.05 + 0.02 * (i % 2), 0.0), float(compass), float(pitch), length, width, MD_LEAF_DK if i % 3 else MD_GRASS_DK, thickness=0.012)
        n.tube([(0.0, 0.0, 0.0), (0.01, 0.12, 0.0), (0.0, 0.19, 0.01)], [0.016, 0.012, 0.008], MD_STEM, sides=4)
        for k, (pos, size, petals, turn, tilt) in enumerate(blooms):
            blossom_node(nodes, name, f"{name}-b{k}", pos, size, petals, turn, tilt)
        for pos in buds:
            n.obox(pos, (0.026, 0.040, 0.026), MD_BUD, yaw=25.0, bevel=0.010)

    # --- MAKAHIYA: low sprawling stems of feathery leaves (each leaf its own node: compass, length, rise, the
    # leaflet sizes) and pink puffs on their own nodes (position, size, stamens as compass and elevation).
    for name, at, leaves, puffs in [
        ("maka-0", (2.10, 0.0, 1.00), [(10, 0.20, 18, [0.030, 0.034, 0.034, 0.030, 0.024]), (95, 0.18, 22, [0.028, 0.032, 0.030, 0.024]),
                                        (180, 0.22, 16, [0.030, 0.035, 0.036, 0.032, 0.026, 0.020]), (265, 0.19, 20, [0.029, 0.033, 0.031, 0.025])],
         [((0.05, 0.13, 0.04), 0.050, [(0, 20), (60, 50), (120, 15), (180, 45), (240, 25), (300, 60), (30, 80), (200, 75)]),
          ((-0.06, 0.11, -0.03), 0.044, [(20, 30), (90, 55), (160, 20), (230, 50), (300, 35), (120, 80)])]),
        ("maka-1", (1.02, 0.0, 2.70), [(40, 0.19, 20, [0.029, 0.033, 0.032, 0.026]), (130, 0.21, 16, [0.030, 0.034, 0.034, 0.030, 0.024]),
                                        (220, 0.18, 22, [0.028, 0.032, 0.030, 0.023]), (310, 0.20, 18, [0.030, 0.034, 0.033, 0.028, 0.022])],
         [((0.03, 0.12, 0.05), 0.048, [(10, 25), (75, 55), (140, 20), (205, 50), (270, 30), (335, 60), (100, 80)])]),
        ("maka-2", (2.88, 0.0, -0.42), [(0, 0.21, 16, [0.030, 0.035, 0.035, 0.031, 0.025]), (90, 0.19, 20, [0.029, 0.033, 0.031, 0.025]),
                                         (185, 0.20, 18, [0.030, 0.034, 0.033, 0.028, 0.022]), (270, 0.18, 22, [0.028, 0.032, 0.030, 0.024])],
         [((-0.04, 0.12, 0.03), 0.052, [(0, 30), (70, 50), (140, 25), (210, 55), (280, 20), (340, 45), (180, 80), (40, 75)]),
          ((0.06, 0.10, -0.05), 0.042, [(30, 25), (110, 50), (190, 30), (270, 55), (350, 20)])]),
        ("maka-3", (-1.58, 0.0, -1.12), [(20, 0.20, 18, [0.030, 0.034, 0.033, 0.027, 0.021]), (110, 0.18, 22, [0.028, 0.032, 0.030, 0.024]),
                                          (200, 0.21, 16, [0.030, 0.035, 0.035, 0.031, 0.025]), (290, 0.19, 20, [0.029, 0.033, 0.031, 0.025])],
         [((0.04, 0.12, 0.02), 0.048, [(15, 30), (85, 55), (155, 25), (225, 50), (295, 35), (0, 80)])]),
        ("maka-4", (0.42, 0.0, -2.92), [(35, 0.19, 20, [0.029, 0.033, 0.032, 0.026]), (125, 0.21, 16, [0.030, 0.034, 0.034, 0.029, 0.023]),
                                         (215, 0.18, 22, [0.028, 0.032, 0.030, 0.024]), (305, 0.20, 18, [0.030, 0.034, 0.033, 0.028])],
         [((0.02, 0.12, -0.04), 0.050, [(5, 25), (70, 50), (135, 20), (200, 55), (265, 30), (330, 50), (100, 80)])]),
    ]:
        n = Node(name, origin=at, parent="meadow"); nodes.append(n)
        n.obox((0.0, 0.02, 0.0), (0.06, 0.04, 0.06), MD_STEM, yaw=15.0, bevel=0.012)
        for i, (compass, length, rise, pairs) in enumerate(leaves):
            leaf = Node(f"{name}-l{i}", origin=(0.0, 0.03, 0.0), parent=name, yaw=float(compass)); nodes.append(leaf)
            mimosa_leaf(leaf, length, rise, pairs)
        for k, (pos, size, stamens) in enumerate(puffs):
            p = Node(f"{name}-p{k}", origin=pos, parent=name); nodes.append(p)
            n.tube([(0.0, 0.03, 0.0), (pos[0] * 0.5, pos[1] * 0.7, pos[2] * 0.5), pos], [0.006, 0.005, 0.004], MD_STEM, sides=3)
            puff(p, (0.0, 0.0, 0.0), size, stamens)

    write(os.path.join(OUT, "meadow.glb"), nodes)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    which = set(sys.argv[1:]) or {"sentry", "seedling", "thorns", "makiling"}
    if "sentry" in which: sentry()
    if "seedling" in which: seedling()
    if "thorns" in which: thorns()
    if "makiling" in which: makiling()
    # ⚠️ The meadow is built only when named (`python tools/build_paete_props.py meadow`) until the cutscene wires it
    # (TODO HERO-9, "HER MEADOW"): nothing loads `meadow.glb` yet.
    if "meadow" in sys.argv[1:]: meadow()
