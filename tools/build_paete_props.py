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
# *"its fine if u dont make maria makiling like other characters"*). direction.md section 5.10.
#
# Not the cast's chibi proportions, by the owner's leave: a tall, calm figure about 3.9 m, standing in
# the mist behind him, head bowed toward him, eyes closed, her cupped hands holding the seed of light
# (the painting's cupped hands); long black hair to the knees, a crown of white flowers, a white gown,
# and a pale green shawl. Every part is typed. (A deer from the paintings stood beside her for one
# film and was cut the same day, the owner: *"why is there a deer even did i ask for that"*.)
#
# ⚠️ HER OWN PALETTE, NOT HIS. The slots below mean HER colours; the runtime dresses her with
# `MakilingSpirit.Palette` (same sixteen cells, her values), then gives her a glowing pale outline in
# place of the ink, which is what reads as a spirit.
#   0 hair  1 hair lit  2 skin  3 skin shade  4 gown  5 gown shade  6 petal  7 flower heart
#   8 ink   9 leaf      10 light  11 shawl    12 deer 13 deer light 14 deer dark 15 antler
# ---------------------------------------------------------------------------------------------
MK_HAIR, MK_HAIR_LIT, MK_SKIN, MK_SKIN_SH, MK_GOWN, MK_GOWN_SH, MK_PETAL, MK_HEART = 0, 1, 2, 3, 4, 5, 6, 7
MK_INK, MK_LEAF, MK_LIGHT, MK_SHAWL, MK_DEER, MK_DEER_LT, MK_DEER_DK, MK_ANTLER = 8, 9, 10, 11, 12, 13, 14, 15


def blossom(node, centre, compass, tilt, size, petals):
    """One flower of the crown: `petals` rounded petals round a gold heart, its face turned out along
    `compass` and tipped up by `tilt`. Called once per flower with that flower's own numbers."""
    for k in range(petals):
        spin = k * 360.0 / petals + (13.0 if petals == 6 else 0.0)
        local = (size * 0.55 * math.cos(math.radians(spin)), size * 0.55 * math.sin(math.radians(spin)), 0.0)
        node.leaf(add(centre, pv._rotate(local, compass, 0.0, tilt)), size * 0.95, size * 0.62, compass, spin, 90.0 + tilt,
                  slot=MK_PETAL, thickness=0.010)
    node.obox(add(centre, pv._rotate((0.0, 0.0, 0.012), compass, 0.0, 0.0)), (size * 0.5, size * 0.5, size * 0.35), MK_HEART, yaw=compass, bevel=0.006)


def makiling():
    root = Node("makiling")
    body = Node("body", parent="makiling")
    nodes = [root, body]

    # --- The gown: a long skirt flaring from the waist to a hem the mist will swallow, and its folds.
    line(body, [(0, 1.62, 0.0), (0, 1.30, 0.01), (0, 0.95, 0.03), (0, 0.55, 0.05), (0, 0.22, 0.07), (0, 0.0, 0.08)],
         [0.22, 0.30, 0.40, 0.52, 0.63, 0.70], MK_GOWN, sides=9, per=3)
    body.obox(sring_free(0.46, 18, 0.62), (0.07, 1.05, 0.05), MK_GOWN_SH, yaw=18, roll=-14)
    body.obox(sring_free(0.49, 70, 0.55), (0.06, 1.15, 0.05), MK_GOWN_SH, yaw=70, roll=-15)
    body.obox(sring_free(0.44, 128, 0.66), (0.07, 0.95, 0.05), MK_GOWN_SH, yaw=128, roll=-13)
    body.obox(sring_free(0.50, 196, 0.52), (0.06, 1.20, 0.05), MK_GOWN_SH, yaw=196, roll=-16)
    body.obox(sring_free(0.47, 252, 0.60), (0.07, 1.00, 0.05), MK_GOWN_SH, yaw=252, roll=-14)
    body.obox(sring_free(0.45, 318, 0.64), (0.06, 1.02, 0.05), MK_GOWN_SH, yaw=318, roll=-13)
    # The bodice, and the off-shoulder wrap round it.
    line(body, [(0, 1.60, 0.0), (0, 1.85, 0.01), (0, 2.08, 0.02), (0, 2.26, 0.01)], [0.20, 0.185, 0.225, 0.235], MK_GOWN, sides=8, per=3)
    line(body, [ring(0.25, 0, 2.20), ring(0.26, 50, 2.25), ring(0.25, 100, 2.29), ring(0.23, 150, 2.24), ring(0.23, 210, 2.23),
                ring(0.25, 260, 2.29), ring(0.26, 310, 2.25), ring(0.25, 360, 2.20)], [0.045] * 8, MK_GOWN_SH, sides=5, per=3)
    # The shawl belt at the waist, knotted at her left hip, two tails hanging.
    line(body, [ring(0.215, a, 1.63) for a in (0, 60, 120, 180, 240, 300, 360)], [0.035] * 7, MK_SHAWL, sides=5, per=3)
    body.obox(ring(0.22, 40, 1.63), (0.08, 0.07, 0.06), MK_SHAWL, yaw=40)
    line(body, [ring(0.23, 40, 1.60), ring(0.26, 46, 1.35), ring(0.29, 50, 1.08)], [0.03, 0.028, 0.02], MK_SHAWL, sides=4, per=3)
    line(body, [ring(0.23, 34, 1.60), ring(0.28, 30, 1.40), ring(0.31, 26, 1.20)], [0.028, 0.026, 0.018], MK_SHAWL, sides=4, per=3)
    # Shoulders, neck (skin).
    body.box((0.17, 2.24, -0.07), (0.30, 2.33, 0.07), MK_SKIN, bevel=0.03)
    body.box((-0.30, 2.24, -0.07), (-0.17, 2.33, 0.07), MK_SKIN, bevel=0.03)
    line(body, [(0, 2.28, 0.0), (0, 2.42, 0.005), (0, 2.53, 0.01)], [0.075, 0.07, 0.068], MK_SKIN, sides=6, per=3)
    # The arms, down and forward, forearms meeting in front of her chest.
    line(body, [(0.27, 2.28, 0.0), (0.31, 2.10, 0.04), (0.30, 1.96, 0.12), (0.18, 1.99, 0.27), (0.07, 2.02, 0.35)],
         [0.064, 0.058, 0.052, 0.046, 0.040], MK_SKIN, sides=6, per=3)
    line(body, [(-0.27, 2.28, 0.0), (-0.31, 2.11, 0.03), (-0.30, 1.97, 0.12), (-0.18, 1.99, 0.28), (-0.07, 2.02, 0.35)],
         [0.064, 0.058, 0.052, 0.046, 0.040], MK_SKIN, sides=6, per=3)

    # --- Long bell sleeves hanging from her forearms (v2): a spirit's cloth, the one flowing shape the blocky
    # body lacked. Each typed from the elbow down and out, wide at the cuff, its hem trailing.
    line(body, [(0.30, 2.00, 0.10), (0.33, 1.84, 0.20), (0.36, 1.62, 0.26), (0.37, 1.40, 0.28), (0.35, 1.24, 0.26)],
         [0.070, 0.095, 0.120, 0.135, 0.090], MK_GOWN_SH, sides=7, per=3)
    line(body, [(-0.30, 2.00, 0.10), (-0.33, 1.83, 0.21), (-0.35, 1.60, 0.27), (-0.37, 1.37, 0.28), (-0.36, 1.20, 0.25)],
         [0.070, 0.095, 0.120, 0.135, 0.090], MK_GOWN_SH, sides=7, per=3)

    # --- The hands: cupped together, the painting's gesture, holding the seed of light.
    hands = Node("hands", origin=(0.0, 2.02, 0.37), parent="body"); nodes.append(hands)
    hands.obox((0.055, -0.01, 0.0), (0.085, 0.035, 0.13), MK_SKIN, roll=0.0, pitch=-24.0, bevel=0.015)
    hands.obox((-0.055, -0.01, 0.0), (0.085, 0.035, 0.13), MK_SKIN, roll=0.0, pitch=24.0, bevel=0.015)
    hands.obox((0.085, 0.03, 0.01), (0.03, 0.06, 0.12), MK_SKIN_SH, bevel=0.01)
    hands.obox((-0.085, 0.03, 0.01), (0.03, 0.06, 0.12), MK_SKIN_SH, bevel=0.01)
    seed = Node("seed", origin=(0.0, 0.05, 0.0), parent="hands"); nodes.append(seed)
    seed.obox((0.0, 0.0, 0.0), (0.075, 0.075, 0.075), MK_LIGHT, yaw=45, roll=35, bevel=0.012)

    # --- The head: bowed toward him on its own node, eyes closed, a calm small smile.
    head = Node("head", origin=(0.0, 2.52, 0.01), parent="body"); nodes.append(head)
    head.box((-0.150, 0.03, -0.155), (0.150, 0.42, 0.160), MK_SKIN, bevel=0.05)
    head.box((-0.095, -0.01, -0.08), (0.095, 0.08, 0.135), MK_SKIN, bevel=0.035)           # the chin, narrower
    head.obox((0.0, 0.17, 0.165), (0.035, 0.055, 0.03), MK_SKIN_SH, bevel=0.01)          # a small nose
    # Closed eyes: two short strokes each, a gentle downward arc (the painting's lowered lids).
    # ⚠️ v2 (2026-09-26, the owner: *"how she looks needs to be refined"*): the strokes were 4 cm and vanished at
    # cutscene distance. Each closed eye is now a long lowered lid (three strokes, 12 cm across) with two short
    # lashes hanging off its outer end, so the calm, bowed face reads from across the court.
    head.obox((0.036, 0.236, 0.163), (0.046, 0.014, 0.008), MK_INK, pitch=-10.0, bevel=0)
    head.obox((0.080, 0.230, 0.162), (0.046, 0.014, 0.008), MK_INK, pitch=6.0, bevel=0)
    head.obox((0.112, 0.238, 0.160), (0.026, 0.012, 0.008), MK_INK, pitch=38.0, bevel=0)
    head.obox((0.116, 0.220, 0.160), (0.020, 0.010, 0.008), MK_INK, pitch=-30.0, bevel=0)
    head.obox((-0.036, 0.236, 0.163), (0.046, 0.014, 0.008), MK_INK, pitch=10.0, bevel=0)
    head.obox((-0.080, 0.230, 0.162), (0.046, 0.014, 0.008), MK_INK, pitch=-6.0, bevel=0)
    head.obox((-0.112, 0.238, 0.160), (0.026, 0.012, 0.008), MK_INK, pitch=-38.0, bevel=0)
    head.obox((-0.116, 0.220, 0.160), (0.020, 0.010, 0.008), MK_INK, pitch=30.0, bevel=0)
    # The smile: two strokes meeting low in the middle.
    head.obox((0.018, 0.098, 0.150), (0.036, 0.010, 0.008), MK_INK, pitch=10.0, bevel=0)
    head.obox((-0.018, 0.098, 0.150), (0.036, 0.010, 0.008), MK_INK, pitch=-10.0, bevel=0)
    # Hair: the crown of the head, parted in the middle, falling past the face on both sides.
    # ⚠️ v2: THE HAIR NO LONGER SWALLOWS THE HEAD. v1 put a 0.34 m hair box over the whole skull, its front edge in
    # line with the face, plus two flat panels down the cheeks: through the ghost shader it read as a helmet
    # with a face slot. Now a cap over the crown and back only, a fringe parted in the middle and swept to each
    # side above the brow, and the locks below falling past the cheeks, so the face is the brightest, clearest
    # thing on her.
    head.box((-0.162, 0.34, -0.176), (0.162, 0.48, 0.070), MK_HAIR, bevel=0.05)
    head.box((-0.168, 0.12, -0.182), (0.168, 0.36, -0.040), MK_HAIR, bevel=0.04)
    head.obox((0.070, 0.405, 0.150), (0.140, 0.060, 0.040), MK_HAIR, roll=-16.0, bevel=0.015)
    head.obox((-0.070, 0.405, 0.150), (0.140, 0.060, 0.040), MK_HAIR, roll=16.0, bevel=0.015)
    head.obox((0.132, 0.360, 0.140), (0.050, 0.110, 0.040), MK_HAIR_LIT, roll=-8.0, bevel=0.012)
    head.obox((-0.132, 0.360, 0.140), (0.050, 0.110, 0.040), MK_HAIR_LIT, roll=8.0, bevel=0.012)
    line(head, [(0.155, 0.40, 0.10), (0.175, 0.15, 0.13), (0.185, -0.10, 0.15), (0.19, -0.36, 0.17)], [0.055, 0.050, 0.042, 0.028], MK_HAIR, sides=5, per=3)
    line(head, [(-0.155, 0.40, 0.10), (-0.172, 0.14, 0.12), (-0.182, -0.12, 0.15), (-0.186, -0.40, 0.16)], [0.055, 0.050, 0.042, 0.026], MK_HAIR, sides=5, per=3)
    line(head, [(0.14, 0.30, 0.05), (0.17, 0.05, 0.08), (0.20, -0.22, 0.10)], [0.040, 0.034, 0.020], MK_HAIR_LIT, sides=5, per=3)

    # --- The long hair down her back, its own node so it sways: six locks, each typed.
    hair = Node("hair", origin=(0.0, 0.34, -0.12), parent="head"); nodes.append(hair)
    locks = [
        ([(0.00, 0.10, 0.00), (0.02, -0.40, -0.08), (0.00, -0.95, -0.12), (0.03, -1.45, -0.10), (0.00, -1.78, -0.08)], [0.10, 0.10, 0.095, 0.08, 0.03], MK_HAIR),
        ([(0.10, 0.08, 0.02), (0.14, -0.38, -0.06), (0.16, -0.90, -0.09), (0.15, -1.36, -0.06), (0.13, -1.62, -0.04)], [0.08, 0.08, 0.075, 0.06, 0.02], MK_HAIR_LIT),
        ([(-0.10, 0.08, 0.02), (-0.13, -0.40, -0.07), (-0.15, -0.94, -0.10), (-0.16, -1.40, -0.07), (-0.14, -1.70, -0.05)], [0.08, 0.08, 0.075, 0.06, 0.02], MK_HAIR),
        ([(0.16, 0.02, 0.06), (0.21, -0.32, 0.00), (0.24, -0.76, -0.03), (0.25, -1.12, -0.01), (0.23, -1.34, 0.01)], [0.06, 0.06, 0.055, 0.045, 0.015], MK_HAIR),
        ([(-0.16, 0.02, 0.06), (-0.21, -0.34, 0.00), (-0.23, -0.80, -0.04), (-0.24, -1.18, -0.02), (-0.22, -1.42, 0.00)], [0.06, 0.06, 0.055, 0.045, 0.015], MK_HAIR_LIT),
        ([(0.05, 0.06, -0.03), (0.07, -0.50, -0.14), (0.05, -1.10, -0.18), (0.08, -1.55, -0.15)], [0.07, 0.07, 0.06, 0.02], MK_HAIR),
    ]
    for pts, radii, slot in locks:
        line(hair, pts, radii, slot, sides=5, per=3)

    # --- The crown of white flowers, nine blossoms, each its own size, turn and petal count, with leaves.
    crown = Node("flower-crown", origin=(0.0, 0.45, -0.01), parent="head"); nodes.append(crown)
    for compass, size, petals, lift in [(0, 0.070, 6, 0.00), (38, 0.058, 5, 0.01), (76, 0.064, 5, -0.01), (118, 0.052, 5, 0.00),
                                        (162, 0.060, 6, 0.01), (205, 0.054, 5, 0.00), (248, 0.066, 5, -0.01), (288, 0.057, 5, 0.01),
                                        (325, 0.062, 6, 0.00)]:
        blossom(crown, ring(0.185, compass, lift), compass, -20.0, size, petals)
    sprig(crown, ring(0.19, 20, -0.01), 20, -10, 0.10, 0.05, MK_LEAF)
    sprig(crown, ring(0.19, 140, 0.0), 140, -12, 0.09, 0.05, MK_LEAF)
    sprig(crown, ring(0.19, 228, -0.01), 228, -8, 0.10, 0.05, MK_LEAF)
    sprig(crown, ring(0.19, 305, 0.0), 305, -14, 0.09, 0.05, MK_LEAF)

    # --- The shawl: a pale green length of cloth over her right shoulder, down her back, trailing.
    shawl = Node("shawl", origin=(-0.24, 2.30, 0.0), parent="body"); nodes.append(shawl)
    line(shawl, [(0.0, 0.0, 0.06), (0.05, 0.02, -0.10), (0.25, -0.10, -0.22), (0.45, -0.45, -0.28), (0.62, -0.95, -0.34), (0.72, -1.45, -0.45)],
         [0.05, 0.05, 0.048, 0.045, 0.040, 0.030], MK_SHAWL, sides=4, per=3)
    line(shawl, [(0.0, 0.0, 0.06), (-0.04, -0.20, 0.10), (-0.06, -0.55, 0.14), (-0.02, -0.90, 0.16)], [0.045, 0.042, 0.036, 0.022], MK_SHAWL, sides=4, per=3)

    write(os.path.join(OUT, "makiling.glb"), nodes)


def sring_free(r, a, y):
    """A point on a ring (no silhouette), for the gown's folds."""
    return ring(r, a, y)

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    which = set(sys.argv[1:]) or {"sentry", "seedling", "thorns", "makiling"}
    if "sentry" in which: sentry()
    if "seedling" in which: seedling()
    if "thorns" in which: thorns()
    if "makiling" in which: makiling()
