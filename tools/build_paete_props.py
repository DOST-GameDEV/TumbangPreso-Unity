"""Paete's skill props, modelled part by part: the sentry, the seedling and the thorn construct.

    python tools/build_paete_props.py

⚠️⚠️ WHY THESE ARE MODELS NOW (owner, 2026-09-26): *"the current models of all his skills look ugly
still its js blocks"*, *"they arent very detailed"*, *"thoroughly work on the detail of each part
manually instead of just generating it as a whole"*. Until this file the props were Unity cubes and
code tubes in flat colours with no ink, built at runtime in `PaeteVfx.cs`. That is not how anything
else in the cast is made. Paete himself is typed part by part in `tools/build_paete_voxel.py`, and
these props are made the same way and from the same helpers: chamfered bark blocks
(`box_polygons`), faceted tapered branches (`_branch`), six-sided prism leaves (`_leaf`), smoothed
normals so the inverted-hull ink closes (`smooth_normals`), and HIS palette through the atlas cells
(`cell_uv`), so they wear his bark, moss, leaf and eye-glow exactly and get the toon shading and the
ink outline at runtime (`ToonSkin.Apply` with his palette).

⚠️ EVERY PART IS TYPED, NOT STAMPED. The owner's standing rule for Paete (*"do it one by one dont try
to mass generate it"*). Where a prop has five trunks or seven thorns, each has its own row of
numbers below; nothing is one shape repeated round a circle.

⚠️ THE PARTS THAT MOVE ARE NAMED NODES. The skills animate by posing these nodes from their age
(`PaeteSentryBody`, `PaetePlantBody`, `PaeteThornBody`), so a node's origin is its pivot: a crown
branch pivots where it leaves the trunk, a petal where it meets the pod, a thorn at the ground.

Output: Assets/TumbangPreso/Resources/Models/PaeteProps/{sentry,seedling,thorns}.glb, metres, +Y up.
Units are the game's own (no PersonScale): these stand on the court at their real size.
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
INK, EYE, EYE_GLOW, FLOWER = pv.INK, pv.EYE, pv.EYE_GLOW, pv.FLOWER_HEART
BARK, BARK_DARK, BARK_LIT = pv.BARK, pv.BARK_DARK, pv.BARK_LIT


# ---------------------------------------------------------------------------------------------
# A tiny scene: named nodes, each a list of (slot, faces) in the node's own space.
# ---------------------------------------------------------------------------------------------
class Node:
    def __init__(self, name, origin=(0.0, 0.0, 0.0), parent=None):
        self.name, self.origin, self.parent = name, origin, parent
        self.parts = []   # (slot, [ [points...], ... ])

    def box(self, lo, hi, slot, bevel=None):
        bevel = pv.bevel_for(lo, hi) if bevel is None else bevel
        self.parts.append((slot, [pts for _, pts in pv.box_polygons(lo, hi, -1, bevel)]))

    def branch(self, path, radii, slot, sides=6):
        self.parts.append((slot, pv._branch(path, radii, sides)))

    def leaf(self, centre, length, width, yaw, pitch, roll=0.0, slot=LEAF, thickness=0.012):
        self.parts.append((slot, pv._leaf(centre, length, width, thickness, yaw, pitch, roll)))


def _normal(points):
    a, b, c = points[0], points[1], points[2]
    n = pv._cross(pv._rafi_sub(b, a), pv._rafi_sub(c, a))
    length = math.sqrt(pv._dot(n, n))
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
        # His model references its atlas beside it; the props carry their own copy beside them.
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


def ring(r, a_deg, y):
    a = math.radians(a_deg)
    return (r * math.cos(a), y, r * math.sin(a))


# ---------------------------------------------------------------------------------------------
# THE SENTRY. 4.3 m. Five braided trunks, each typed; bark plates and knots on each; vines wound
# through; moss, fungus, glow cracks; six buttress roots; five crown branches as their own nodes.
# ---------------------------------------------------------------------------------------------
def sentry():
    root = Node("sentry")
    trunk = Node("trunk", parent="sentry")
    nodes = [root, trunk]

    # Each trunk: start angle, turns, radius at foot/top, girth at foot/top, bark slot.
    trunks = [
        (0.0, 0.55, 0.62, 0.30, 0.40, 0.22, BARK_LIT),
        (72.0, 0.48, 0.66, 0.28, 0.36, 0.19, BARK),
        (143.0, 0.60, 0.58, 0.34, 0.38, 0.21, BARK_LIT),
        (215.0, 0.52, 0.64, 0.26, 0.33, 0.18, BARK_DARK),
        (288.0, 0.57, 0.60, 0.31, 0.37, 0.20, BARK),
    ]
    for a0, turns, r0, r1, g0, g1, slot in trunks:
        path, radii = [], []
        for k in range(15):
            u = k / 14.0
            r = (r0 + (r1 - r0) * u) * (1.0 - 0.26 * math.sin(u * math.pi))
            path.append(ring(r, a0 + u * turns * 360.0, -0.06 + u * 3.95))
            radii.append(g0 + (g1 - g0) * u)
        trunk.branch(path, radii, slot, sides=6)

    # Bark plates: raised, bevelled plates on the trunks' outer faces, each placed and sized by hand
    # (angle round the bundle, height, width, height of plate, depth). Light bark on dark, dark on light.
    plates = [(20, 0.55, 0.20, 0.34, BARK_DARK), (95, 0.95, 0.18, 0.28, BARK_LIT), (160, 0.40, 0.22, 0.30, BARK_DARK),
              (230, 1.30, 0.16, 0.26, BARK_LIT), (300, 0.80, 0.20, 0.32, BARK_DARK), (40, 1.75, 0.15, 0.24, BARK),
              (130, 2.10, 0.14, 0.22, BARK_DARK), (205, 2.45, 0.13, 0.20, BARK_LIT), (275, 1.95, 0.15, 0.26, BARK),
              (340, 2.70, 0.12, 0.18, BARK_DARK), (70, 3.05, 0.12, 0.18, BARK_LIT), (185, 3.30, 0.11, 0.16, BARK)]
    for ang, y, w, h, slot in plates:
        r = 0.84 * (1.0 - 0.26 * math.sin(min(1.0, y / 3.95) * math.pi)) + 0.02
        c = ring(r, ang, y)
        a = math.radians(ang)
        # A plate is a thin box facing outward: build it axis-aligned then rotate its corners.
        faces = [pts for _, pts in pv.box_polygons((-w / 2, -h / 2, -0.03), (w / 2, h / 2, 0.03), -1, 0.012)]
        yaw = -math.degrees(a) + 90.0
        trunk.parts.append((slot, [[tuple(c[i] + q[i] for i in range(3)) for q in [pv._rotate(p, yaw, 0, 0) for p in f]] for f in faces]))
    # Knots: dark sunk bosses, and a knot hole with the eye light deep in it on the front.
    for ang, y, s in [(60, 1.10, 0.10), (250, 0.65, 0.12), (325, 2.30, 0.09)]:
        c = ring(0.80 * (1.0 - 0.26 * math.sin(y / 3.95 * math.pi)) + 0.03, ang, y)
        trunk.box((c[0] - s, c[1] - s * 0.8, c[2] - s), (c[0] + s, c[1] + s * 0.8, c[2] + s), INK)
    for ang, y in [(0, 1.55), (180, 2.05)]:
        c = ring(0.66, ang, y)
        trunk.box((c[0] - 0.06, c[1] - 0.12, c[2] - 0.06), (c[0] + 0.06, c[1] + 0.12, c[2] + 0.06), EYE)
    # Glow cracks: thin seams of eye light running up between trunks.
    for ang, y0, y1 in [(38, 0.9, 1.6), (110, 2.2, 2.8), (192, 0.5, 1.1), (262, 1.7, 2.4), (320, 2.8, 3.3)]:
        r = 0.72 * (1.0 - 0.26 * math.sin(((y0 + y1) / 2) / 3.95 * math.pi))
        c0, c1 = ring(r, ang, y0), ring(r, ang + 8, y1)
        trunk.branch([c0, c1], [0.035, 0.03], EYE, sides=4)
    # Three dark vines winding up the bundle, each its own pitch.
    for a0, turns, top, r in [(20, 1.6, 3.4, 0.86), (140, 1.3, 2.9, 0.82), (260, 1.9, 3.7, 0.78)]:
        path, radii = [], []
        for k in range(29):
            u = k / 28.0
            rr = r * (1.0 - 0.24 * math.sin(u * math.pi))
            path.append(ring(rr, a0 + u * turns * 360.0, 0.12 + u * top))
            radii.append(0.07 - 0.035 * u)
        trunk.branch(path, radii, VINE, sides=5)
        # Leaves on each vine, at their own places.
        for f, yaw, pitch in [(0.22, 30, -20), (0.47, 160, -30), (0.71, 290, -15), (0.90, 70, -40)]:
            i = int(f * 28)
            trunk.leaf(path[i], 0.22, 0.13, yaw + a0, pitch, slot=LEAF if i % 2 else LEAF_DARK)
    # Moss on the foot and the shoulders; bracket fungus in two tiers.
    for ang, y, w, h, d in [(15, 0.35, 0.30, 0.16, 0.20), (140, 0.25, 0.36, 0.14, 0.22), (250, 0.45, 0.26, 0.18, 0.18),
                            (80, 2.60, 0.22, 0.12, 0.16), (300, 3.10, 0.20, 0.10, 0.14)]:
        c = ring(0.86 * (1.0 - 0.26 * math.sin(y / 3.95 * math.pi)), ang, y)
        trunk.box((c[0] - w / 2, c[1] - h / 2, c[2] - d / 2), (c[0] + w / 2, c[1] + h / 2, c[2] + d / 2), MOSS if ang % 2 else MOSS_DARK)
    for ang, y, w in [(85, 1.00, 0.34), (95, 1.18, 0.24), (230, 1.85, 0.30), (240, 2.00, 0.20)]:
        c = ring(0.92 * (1.0 - 0.26 * math.sin(y / 3.95 * math.pi)), ang, y)
        trunk.box((c[0] - w / 2, c[1] - 0.03, c[2] - w / 2), (c[0] + w / 2, c[1] + 0.03, c[2] + w / 2), HEARTWOOD)
        trunk.box((c[0] - w / 2, c[1] - 0.05, c[2] - w / 2), (c[0] + w / 2, c[1] - 0.02, c[2] + w / 2), FLOWER, bevel=0)

    # Six buttress roots, each its own node so they can creep out after the trunk rises.
    for i, (ang, reach, g, slot) in enumerate([(15, 1.6, 0.30, BARK_DARK), (75, 1.3, 0.26, BARK), (130, 1.8, 0.32, BARK_DARK),
                                               (200, 1.4, 0.27, BARK), (250, 1.7, 0.31, BARK_DARK), (315, 1.2, 0.25, BARK)]):
        n = Node(f"buttress-{i}", parent="trunk"); nodes.append(n)
        pts = [ring(0.46, ang, 0.85), ring(0.80, ang + 3, 0.36), ring(0.80 + reach * 0.5, ang + 6, 0.10), ring(0.80 + reach, ang + 8, -0.06)]
        n.branch(pts, [g, g * 0.72, g * 0.40, g * 0.10], slot, sides=5)
        # A knuckle where the root bends into the road.
        k = pts[2]
        n.box((k[0] - 0.09, -0.02, k[2] - 0.09), (k[0] + 0.09, 0.14, k[2] + 0.09), BARK_LIT)

    # The crown: five branches, each its own node pivoting at the top of the bundle, each with a fork
    # and a leaf cluster, curling up and in round where the core will float.
    crown = Node("crown", origin=(0.0, 3.75, 0.0), parent="trunk"); nodes.append(crown)
    claws = [(0, 0.80, 1.25, 0.22, BARK_LIT), (74, 0.86, 1.18, 0.20, BARK), (146, 0.78, 1.30, 0.21, BARK_LIT),
             (214, 0.84, 1.22, 0.19, BARK), (288, 0.80, 1.28, 0.20, BARK_DARK)]
    for i, (ang, out, height, g, slot) in enumerate(claws):
        n = Node(f"claw-{i}", parent="crown"); nodes.append(n)
        p = [ring(0.30, ang, -0.10), ring(out, ang + 4, 0.30), ring(out + 0.02, ang + 8, 0.85), ring(0.38, ang + 12, height)]
        n.branch(p, [g, g * 0.70, g * 0.42, g * 0.12], slot, sides=5)
        fork = [p[1], ring(out + 0.28, ang - 10, 0.62), ring(out + 0.36, ang - 16, 0.86)]
        n.branch(fork, [g * 0.45, g * 0.30, g * 0.10], BARK, sides=5)
        for k, (f, yaw, pitch, size, s) in enumerate([(2, 20, -50, 0.42, LEAF), (3, -30, -30, 0.36, LEAF_DARK),
                                                       (2, 80, -65, 0.32, LEAF), (1, 140, -20, 0.30, LEAF_DARK)]):
            c = p[f]
            n.leaf((c[0], c[1] + 0.05, c[2]), size, size * 0.56, yaw + ang, pitch, slot=s)
        n.leaf(fork[-1], 0.30, 0.18, ang - 10, -40, slot=LEAF)
    write(os.path.join(OUT, "sentry.glb"), nodes)


# ---------------------------------------------------------------------------------------------
# THE SEEDLING. 0.9 m. A curved stem with bark bands, base leaves, a pod of five petals, the slipper.
# ---------------------------------------------------------------------------------------------
def seedling():
    root = Node("seedling")
    stem = Node("stem", parent="seedling")
    nodes = [root, stem]
    path = [(0.0, 0.0, 0.0), (0.03, 0.22, 0.01), (0.01, 0.46, -0.02), (-0.01, 0.62, 0.0)]
    stem.branch(path, [0.10, 0.085, 0.07, 0.065], BARK, sides=6)
    for y, slot in [(0.14, BARK_DARK), (0.33, BARK_LIT), (0.50, BARK_DARK)]:
        stem.box((-0.085, y - 0.02, -0.085), (0.085, y + 0.02, 0.085), slot, bevel=0.01)
    stem.box((-0.09, 0.56, -0.09), (0.09, 0.62, 0.09), MOSS)
    for yaw, pitch, size, s in [(30, -10, 0.36, LEAF), (125, -6, 0.30, LEAF_DARK), (210, -14, 0.38, LEAF), (300, -8, 0.30, LEAF_DARK)]:
        c = pv._rotate((0.0, 0.0, size * 0.45), yaw, 0, 0)
        stem.leaf((c[0], 0.06, c[2]), size, size * 0.55, yaw, pitch, slot=s)
    for yaw, y, size, s in [(80, 0.32, 0.24, LEAF), (250, 0.44, 0.22, LEAF_DARK)]:
        c = pv._rotate((0.0, 0.0, 0.12), yaw, 0, 0)
        stem.leaf((c[0], y, c[2]), size, size * 0.55, yaw, -35, slot=s)
    pod = Node("pod", origin=(0.0, 0.64, 0.0), parent="stem"); nodes.append(pod)
    pod.box((-0.11, -0.05, -0.11), (0.11, 0.06, 0.11), MOSS_DARK)
    pod.box((-0.08, 0.05, -0.08), (0.08, 0.09, 0.08), MOSS_LIT)
    for i, (yaw, size, s) in enumerate([(0, 0.30, LEAF_DARK), (74, 0.28, VINE), (146, 0.31, LEAF_DARK), (214, 0.27, VINE), (288, 0.29, LEAF_DARK)]):
        n = Node(f"petal-{i}", parent="pod"); nodes.append(n)
        # A petal pivots at the pod rim, lying along +Z of its own hinge before the hinge turns it.
        c = pv._rotate((0.0, 0.02, 0.15), yaw, 0, 0)
        n.leaf(c, size, size * 0.58, yaw, -10, slot=s, thickness=0.02)
        mid = pv._rotate((0.0, 0.03, 0.10), yaw, 0, 0)
        n.branch([pv._rotate((0.0, 0.02, 0.02), yaw, 0, 0), mid], [0.018, 0.012], MOSS, sides=4)
    shoe = Node("slipper", origin=(0.0, 0.16, 0.0), parent="pod"); nodes.append(shoe)
    shoe.box((-0.065, -0.018, -0.14), (0.065, 0.018, 0.14), BARK_LIT)
    shoe.box((-0.060, 0.016, -0.13), (0.060, 0.022, 0.13), HEARTWOOD, bevel=0)
    for x, yaw in [(0.03, 30), (-0.03, -30)]:
        a = pv._rotate((0.0, 0.0, 0.05), yaw, 0, 0)
        shoe.branch([(0.0, 0.03, 0.08), (x + a[0], 0.05, 0.04 + a[2] * 0.3), (x * 1.6, 0.03, -0.01)], [0.012, 0.012, 0.010], VINE, sides=4)
    shoe.box((-0.012, 0.02, 0.07), (0.012, 0.05, 0.095), VINE)
    for i, (yaw, length) in enumerate([(20, 0.42), (110, 0.36), (205, 0.46), (290, 0.33)]):
        n = Node(f"root-{i}", origin=(0.0, 0.0, 0.0), parent="seedling"); nodes.append(n)
        a, b = pv._rotate((0.0, 0.10, 0.0), yaw, 0, 0), pv._rotate((0.0, 0.03, length * 0.5), yaw, 0, 0)
        c = pv._rotate((0.0, -0.04, length), yaw, 0, 0)
        n.branch([a, b, c], [0.05, 0.035, 0.012], BARK_DARK, sides=5)
    write(os.path.join(OUT, "seedling.glb"), nodes)


# ---------------------------------------------------------------------------------------------
# THE THORN CONSTRUCT. A knot of roots with seven thorned spikes, each its own node.
# ---------------------------------------------------------------------------------------------
def thorns():
    root = Node("thorns")
    knot = Node("knot", parent="thorns")
    nodes = [root, knot]
    for ang, r, slot in [(0, 0.30, BARK_DARK), (120, 0.26, BARK), (240, 0.28, BARK_DARK)]:
        knot.branch([ring(r, ang, 0.0), ring(r * 0.6, ang + 60, 0.22), ring(0.08, ang + 120, 0.34)], [0.12, 0.09, 0.04], slot, sides=5)
    knot.box((-0.14, 0.0, -0.14), (0.14, 0.16, 0.14), BARK_LIT)
    spikes = [(0, 0.78, 62), (52, 0.62, 56), (103, 0.90, 66), (155, 0.55, 58), (206, 0.84, 64), (258, 0.66, 60), (309, 0.72, 57)]
    for i, (ang, h, lean) in enumerate(spikes):
        n = Node(f"thorn-{i}", origin=ring(0.34, ang, 0.0), parent="thorns"); nodes.append(n)
        out = pv._rotate((0.0, 0.0, 1.0), -ang + 90, 0, 0)
        tilt = math.radians(90 - lean)
        d = (out[0] * math.sin(tilt), math.cos(tilt), out[2] * math.sin(tilt))
        p = [(0.0, 0.0, 0.0), tuple(d[k] * h * 0.5 + (0.02 if k == 1 else 0) for k in range(3)), tuple(d[k] * h for k in range(3))]
        n.branch(p, [0.08, 0.05, 0.005], BARK if i % 2 else BARK_DARK, sides=5)
        # Barbs up the spike's back, two each at their own heights.
        for f, side in [(0.35, 1), (0.62, -1)]:
            base = tuple(d[k] * h * f for k in range(3))
            barb = (base[0] + out[2] * 0.06 * side, base[1] + 0.07, base[2] - out[0] * 0.06 * side)
            n.branch([base, barb], [0.025, 0.003], BARK_LIT, sides=4)
        n.leaf(tuple(d[k] * h * 0.2 for k in range(3)), 0.14, 0.08, -ang + 90, -30, slot=LEAF if i % 2 else LEAF_DARK)
    write(os.path.join(OUT, "thorns.glb"), nodes)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    sentry(); seedling(); thorns()
