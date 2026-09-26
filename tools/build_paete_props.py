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
    for i, (yaw, pts, radii, slot, rider, rider_r, rider_slot, moss) in enumerate(roots):
        n = Node(f"buttress-{i}", origin=ring(0.46, yaw, 0.0), parent="sentry", yaw=yaw); nodes.append(n)
        line(n, pts, radii, slot, twist=30.0)
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
    for name, yaw, main, main_r, second, second_r, fork, fork_r, shade, tufts in branches:
        n = Node(name, origin=ring(0.30, yaw, 0.0), parent="crown", yaw=yaw); nodes.append(n)
        line(n, main, main_r, shade, twist=40.0, per=4)
        line(n, second, second_r, BARK if shade != BARK else BARK_DARK, twist=-30.0, per=4)
        line(n, fork, fork_r, shade, per=4)
        for base, compass, pitch, length, width, slot in tufts:
            sprig(n, base, compass, pitch, length, width, slot)
            sprig(n, base, compass + 70, pitch - 25, length * 0.8, width * 0.8, LEAF_DARK if slot == LEAF else LEAF)

    write(os.path.join(OUT, "sentry.glb"), nodes)


# ---------------------------------------------------------------------------------------------
# THE SEEDLING. 0.9 m. A three-cord stem (the sentry's weave as a child), arm leaves, a pod head.
# ---------------------------------------------------------------------------------------------
def seedling():
    root = Node("seedling")
    stem = Node("stem", parent="seedling")
    nodes = [root, stem]
    # Three cords twisting up a gentle S, each typed: (height, angle, distance out, girth).
    cord(stem, [(0.00, 10, 0.075, 0.058), (0.14, -58, 0.048, 0.054), (0.29, -140, 0.044, 0.050), (0.44, -222, 0.042, 0.046), (0.60, -296, 0.036, 0.040)], BARK_LIT, twist=30)
    cord(stem, [(0.00, 130, 0.072, 0.054), (0.14, 64, 0.046, 0.050), (0.29, -18, 0.045, 0.047), (0.44, -101, 0.040, 0.044), (0.60, -176, 0.037, 0.038)], BARK, twist=-25)
    cord(stem, [(0.00, 250, 0.078, 0.060), (0.14, 184, 0.050, 0.055), (0.29, 101, 0.043, 0.051), (0.44, 18, 0.043, 0.046), (0.60, -58, 0.035, 0.041)], HEARTWOOD, twist=45)
    stem.obox((0.0, 0.575, -0.01), (0.15, 0.06, 0.15), MOSS, yaw=18.0)                      # moss collar
    stem.obox((0.01, 0.24, 0.0), (0.13, 0.03, 0.13), BARK_DARK, yaw=-12.0, bevel=0.008)     # a carved band
    # Four small leaves fanned at the foot, each its own.
    sprig(stem, (0.02, 0.05, 0.02), 30, -10, 0.30, 0.17, LEAF)
    sprig(stem, (0.02, 0.04, -0.02), 125, -6, 0.26, 0.14, LEAF_DARK)
    sprig(stem, (-0.02, 0.06, -0.02), 210, -14, 0.32, 0.18, LEAF)
    sprig(stem, (-0.02, 0.04, 0.02), 300, -8, 0.25, 0.14, LEAF_DARK)
    # The two ARM leaves: big, low on the stem, their own nodes so they flick open, breathe and flare.
    arm0 = Node("arm-0", origin=(0.03, 0.30, 0.0), parent="stem", yaw=95.0); nodes.append(arm0)
    line(arm0, [(0.0, 0.0, 0.02), (0.0, 0.03, 0.10)], [0.018, 0.012], MOSS, sides=4, per=2)
    sprig(arm0, (0.0, 0.04, 0.09), 0, -16, 0.34, 0.19, LEAF, thickness=0.018)
    arm1 = Node("arm-1", origin=(-0.03, 0.36, 0.0), parent="stem", yaw=268.0); nodes.append(arm1)
    line(arm1, [(0.0, 0.0, 0.02), (0.0, 0.035, 0.09)], [0.017, 0.011], MOSS, sides=4, per=2)
    sprig(arm1, (0.0, 0.045, 0.08), 0, -20, 0.31, 0.17, LEAF_DARK, thickness=0.018)
    # The pod: the head, so it can nod and aim.
    pod = Node("pod", origin=(0.0, 0.62, 0.0), parent="stem"); nodes.append(pod)
    pod.obox((0.0, 0.0, 0.0), (0.20, 0.10, 0.20), MOSS_DARK, yaw=45.0)
    pod.obox((0.0, 0.06, 0.0), (0.15, 0.05, 0.15), MOSS_LIT, yaw=20.0)
    # Five petals hinged at the pod's rim, each its own size and shade, lying along their +Z.
    for name, yaw, length, width, slot in [("petal-0", 0, 0.30, 0.18, LEAF_DARK), ("petal-1", 74, 0.28, 0.16, VINE),
                                            ("petal-2", 146, 0.31, 0.19, LEAF_DARK), ("petal-3", 214, 0.27, 0.16, VINE),
                                            ("petal-4", 288, 0.29, 0.17, LEAF_DARK)]:
        n = Node(name, origin=ring(0.08, yaw, 0.03), parent="pod", yaw=yaw); nodes.append(n)
        sprig(n, (0.0, 0.0, 0.0), 0, 58, length, width, slot, thickness=0.022)
        line(n, [(0.0, 0.03, 0.02), (0.0, length * 0.47, length * 0.29)], [0.011, 0.005], MOSS_LIT, sides=4, per=2)
    shoe = Node("slipper", origin=(0.0, 0.16, 0.0), parent="pod"); nodes.append(shoe)
    shoe.box((-0.065, -0.018, -0.14), (0.065, 0.018, 0.14), BARK_LIT)
    shoe.box((-0.060, 0.016, -0.13), (0.060, 0.022, 0.13), HEARTWOOD, bevel=0)
    line(shoe, [(0.0, 0.03, 0.08), (0.045, 0.05, 0.05), (0.048, 0.03, -0.01)], [0.012, 0.012, 0.010], VINE, sides=4, per=3)
    line(shoe, [(0.0, 0.03, 0.08), (-0.045, 0.05, 0.05), (-0.048, 0.03, -0.01)], [0.012, 0.012, 0.010], VINE, sides=4, per=3)
    shoe.box((-0.012, 0.02, 0.07), (0.012, 0.05, 0.095), VINE)
    # Four roots gripping, each typed along its own +Z.
    for name, yaw, pts, radii in [("root-0", 20, [(0, 0.10, 0.02), (0.01, 0.05, 0.19), (0.0, -0.01, 0.33), (-0.01, -0.05, 0.42)], [0.050, 0.038, 0.022, 0.008]),
                                  ("root-1", 110, [(0, 0.09, 0.02), (-0.01, 0.04, 0.16), (0.01, -0.01, 0.28), (0.02, -0.05, 0.36)], [0.046, 0.035, 0.020, 0.008]),
                                  ("root-2", 205, [(0, 0.10, 0.02), (0.02, 0.05, 0.21), (0.01, 0.0, 0.36), (-0.01, -0.05, 0.46)], [0.052, 0.040, 0.023, 0.008]),
                                  ("root-3", 290, [(0, 0.08, 0.02), (-0.01, 0.04, 0.15), (0.0, -0.01, 0.26), (0.01, -0.05, 0.33)], [0.044, 0.033, 0.019, 0.007])]:
        n = Node(name, parent="seedling", yaw=yaw); nodes.append(n)
        line(n, pts, radii, BARK_DARK, per=3)
    write(os.path.join(OUT, "seedling.glb"), nodes)


# ---------------------------------------------------------------------------------------------
# THE THORN CONSTRUCT. A fist of three roots and seven square thorns, each its own typed node.
# ---------------------------------------------------------------------------------------------
def thorns():
    root = Node("thorns")
    knot = Node("knot", parent="thorns")
    nodes = [root, knot]
    # Three roots twisting up out of the road into a knot, typed.
    line(knot, [ring(0.30, 0, -0.04), ring(0.22, 50, 0.14), ring(0.12, 105, 0.30), ring(0.05, 150, 0.40)], [0.11, 0.09, 0.065, 0.03], BARK_DARK, twist=35)
    line(knot, [ring(0.27, 125, -0.04), ring(0.19, 172, 0.12), ring(0.11, 228, 0.26), ring(0.05, 272, 0.34)], [0.10, 0.085, 0.06, 0.028], BARK, twist=-30)
    line(knot, [ring(0.29, 238, -0.04), ring(0.21, 292, 0.15), ring(0.12, 340, 0.33), ring(0.05, 25, 0.44)], [0.11, 0.09, 0.065, 0.03], BARK_LIT, twist=40)
    knot.obox((0.0, 0.16, 0.0), (0.22, 0.14, 0.22), BARK, yaw=25.0)
    knot.obox((0.0, 0.25, 0.0), (0.16, 0.05, 0.16), MOSS_DARK, yaw=50.0)
    # Seven thorns, each typed along its own +Z (tilted up and out), with two barbs.
    spikes = [
        ("thorn-0", 0, [(0, -0.05, 0.0), (-0.02, 0.31, 0.18), (-0.04, 0.60, 0.38), (-0.02, 0.76, 0.55)], [0.085, 0.060, 0.030, 0.004], BARK_DARK,
         [((-0.01, 0.26, 0.15), (0.07, 0.33, 0.18)), ((-0.03, 0.48, 0.30), (-0.11, 0.55, 0.32))], True),
        ("thorn-1", 52, [(0, -0.05, 0.0), (0.03, 0.24, 0.18), (0.05, 0.46, 0.38), (0.03, 0.58, 0.54)], [0.080, 0.056, 0.028, 0.004], BARK,
         [((0.02, 0.20, 0.15), (0.10, 0.26, 0.17)), ((0.04, 0.38, 0.30), (-0.04, 0.45, 0.33))], False),
        ("thorn-2", 103, [(0, -0.05, 0.0), (-0.02, 0.36, 0.16), (-0.04, 0.70, 0.32), (-0.02, 0.88, 0.47)], [0.090, 0.062, 0.031, 0.004], BARK_DARK,
         [((-0.01, 0.30, 0.13), (0.07, 0.37, 0.15)), ((-0.03, 0.56, 0.26), (-0.11, 0.63, 0.28))], True),
        ("thorn-3", 155, [(0, -0.05, 0.0), (0.02, 0.21, 0.16), (0.04, 0.42, 0.33), (0.02, 0.53, 0.47)], [0.078, 0.054, 0.027, 0.004], BARK,
         [((0.01, 0.18, 0.13), (0.09, 0.24, 0.15)), ((0.03, 0.34, 0.27), (-0.05, 0.40, 0.29))], True),
        ("thorn-4", 206, [(0, -0.05, 0.0), (-0.03, 0.34, 0.17), (-0.05, 0.66, 0.35), (-0.03, 0.82, 0.51)], [0.088, 0.061, 0.030, 0.004], BARK_DARK,
         [((-0.02, 0.28, 0.14), (0.06, 0.35, 0.16)), ((-0.04, 0.53, 0.28), (-0.12, 0.60, 0.30))], False),
        ("thorn-5", 258, [(0, -0.05, 0.0), (0.02, 0.26, 0.17), (0.04, 0.51, 0.36), (0.02, 0.64, 0.52)], [0.082, 0.058, 0.029, 0.004], BARK,
         [((0.01, 0.22, 0.14), (0.09, 0.28, 0.16)), ((0.03, 0.41, 0.29), (-0.05, 0.48, 0.31))], True),
        ("thorn-6", 309, [(0, -0.05, 0.0), (-0.01, 0.27, 0.19), (-0.03, 0.54, 0.40), (-0.01, 0.68, 0.58)], [0.084, 0.058, 0.029, 0.004], BARK_LIT,
         [((0.00, 0.23, 0.16), (0.08, 0.30, 0.18)), ((-0.02, 0.43, 0.32), (-0.10, 0.50, 0.34))], False),
    ]
    for name, yaw, pts, radii, slot, barbs, leafy in spikes:
        n = Node(name, origin=ring(0.30, yaw, 0.0), parent="thorns", yaw=yaw); nodes.append(n)
        line(n, pts, radii, slot, sides=4, twist=45, per=3)
        for a, b in barbs:
            n.tube([a, b], [0.028, 0.003], BARK_LIT, sides=4)
        if leafy:
            sprig(n, (0.0, 0.08, 0.06), 0, 25, 0.15, 0.09, LEAF if yaw % 2 else LEAF_DARK)
    write(os.path.join(OUT, "thorns.glb"), nodes)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    which = set(sys.argv[1:]) or {"sentry", "seedling", "thorns"}
    if "sentry" in which: sentry()
    if "seedling" in which: seedling()
    if "thorns" in which: thorns()
