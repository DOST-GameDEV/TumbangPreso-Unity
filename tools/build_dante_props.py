"""Basilio's props, modelled part by part: the mountain beast of CONTINENTAL DRIFT's cutscene.

    python tools/build_dante_props.py [beast]
    python tools/build_dante_props.py --measure beast

WHY A MODEL (docs/reports/dante-presentation-2026-10-16/plan.md section 5.5, owner approved 2026-10-16): the cutscene's creature
was blocking prisms built at runtime (`HeroIntroductionScene.Dante.cs` said so in its own header). The owner's rule from Paete's
props stands for every hero: *"the current models of all his skills look ugly still its js blocks"*. So it is typed here like
Paete's sentry (`tools/build_paete_props.py`, whose helpers this uses): chamfered boxes, faceted tubes, smoothed normals so the
inverted-hull ink closes, its own sixteen-slot palette (`DantePalette` in `Visual/DanteProp.cs`, the same hex values).

WHAT IT IS: the horned basalt beast under Montalban, Bernardo Carpio's mountain, in Basilio's own family. His horn shape (the
swept, five-sided, stepped horn of his model, `tools/author_character_redesign_dante.py` `build_horn`): out from the temple, up,
then hooked; and like him, one side marked: its LEFT horn is chipped short, as his scar is on his left. A carabao's broad, low
skull (the island's beast of burden: the mountain is a working animal), a heavy brow, a hump of a back his court plate rides on,
two arms that end in fists. Blocky TUMP shapes, no realistic anatomy, no texture grain (the owner refused grain on his stone).

LIGHT IS CUT IN, NEVER RAISED (the method's Phaister lesson, *"its the opposite it should look like its from withhin"*). Its eyes
are sunken sockets under the brow and its seams are cracks into the rock; each is a `glow-*` node in the `SoulGlow` layout: a
flat MOUTH lying on the stone where the opening is (TEXCOORD0.x = 1) and an INSIDE cup sunk below it (TEXCOORD0.x = 0), its walls
dull at the lip and hot toward the floor (vertex colours). The hottest colour anywhere is molten hot (1, .62, .18); never white.

EVERY PART IS TYPED, NOT STAMPED (*"do it one by one dont try to mass generate it"*): each box, horn point, knuckle, seam and
chip is its own row. Nothing is one shape repeated round a circle.

NAMED NODES FOR WHAT MOVES (big things move little; its life is breath, seam pulses and shedding rock, not turning):
  beast > body > head > brow, jaw, horn-left, horn-right, glow-eyes
        > body > arm-left > fist-left ; arm-right > fist-right   (an arm hangs straight down -Y from its shoulder pivot)
        > body > back-0, back-1, back-2                           (the slabs that shed rock as it rises)
        > body > glow-seams
The saddle on top of its back is at y 3.4 (`HeroIntroductionScene.DnTitanTop`): Basilio's plate rides there.

⚠️ glTFast NEGATES X ON IMPORT (the method's first trap). Its face is on +Z; in Unity its RIGHT hand is at +X, so in this file its
right is at -X and its left at +X. `L` and `R` below say which is which; never type a side as a bare sign.

Output: Assets/TumbangPreso/Resources/Models/DanteProps/beast.glb, metres, +Y up, the face on +Z.
"""
import math
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_paete_props as bp  # noqa: E402
import build_paete_voxel as pv  # noqa: E402
from build_paete_props import Node, add, sub, mul, unit, cross, dot  # noqa: E402

ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Models", "DanteProps")

# Sides in THIS file's space (see the glTFast note above).
L, R = 1.0, -1.0

# His palette, the plan's (section 0, rule 7): solid basalt, flat shaded; the molten colours only inside the stone.
DANTE_PALETTE = {0: "4F453D", 1: "5C4D40", 2: "3A322C", 3: "6E6052", 4: "FF6B14", 5: "FF9E2E", 6: "8C290F", 7: "6B5238",
                 8: "1E140C", 9: "140E0A", 10: "2A2430", 11: "1A1620", 12: "463C34", 13: "7A6A58", 14: "33291F", 15: "55473A"}
BASALT, WARM, DARK, LIT, CORE, HOT, COOLED, DUST, INK, SOCKET, HORN, HORN_DK, MID, PALE, DEEP, WARM_DK = range(16)

MOLTEN_HOT = (1.0, 0.62, 0.18)
MOLTEN_CORE = (1.0, 0.42, 0.08)
COOLED_RED = (0.55, 0.16, 0.06)


# ---------------------------------------------------------------------------------------------
# Light cut into stone: SoulGlow mouths and cups.
# ---------------------------------------------------------------------------------------------
class Glow:
    """A `glow-*` node's polygons: (role, colour rgba, [points]) with role 1 the mouth and 0 the inside."""

    def __init__(self, name, origin=(0.0, 0.0, 0.0), parent=None):
        self.name, self.origin, self.parent, self.yaw, self.scale = name, origin, parent, 0.0, 1.0
        self.polys = []

    def cut(self, centre, normal, across, stroke, depth):
        """A crack cut into a flat face. `stroke` is [(u, v, half_width), ...] in the face's own axes (`across` is u,
        normal x across is v), each point typed. The mouth lies 4 mm proud of the face; the cup sinks `depth` below it."""
        n = unit(normal); u = unit(across); v = unit(cross(n, u))

        def at(a, b, d): return add(add(add(centre, mul(u, a)), mul(v, b)), mul(n, d))
        for (a0, b0, w0), (a1, b1, w1) in zip(stroke, stroke[1:]):
            da, db = a1 - a0, b1 - b0
            ln = math.hypot(da, db) or 1.0
            sa, sb = -db / ln, da / ln                                     # the stroke's own sideways, in (u, v)
            m0l, m0r = at(a0 + sa * w0, b0 + sb * w0, .004), at(a0 - sa * w0, b0 - sb * w0, .004)
            m1l, m1r = at(a1 + sa * w1, b1 + sb * w1, .004), at(a1 - sa * w1, b1 - sb * w1, .004)
            self.polys.append((1, (0, 0, 0, 0), [m0l, m1l, m1r, m0r]))
            # The cup: narrower at its floor, so its walls are seen through the mouth.
            f0l, f0r = at(a0 + sa * w0 * .45, b0 + sb * w0 * .45, -depth), at(a0 - sa * w0 * .45, b0 - sb * w0 * .45, -depth)
            f1l, f1r = at(a1 + sa * w1 * .45, b1 + sb * w1 * .45, -depth), at(a1 - sa * w1 * .45, b1 - sb * w1 * .45, -depth)
            lip0l, lip0r = at(a0 + sa * w0, b0 + sb * w0, 0.0), at(a0 - sa * w0, b0 - sb * w0, 0.0)
            lip1l, lip1r = at(a1 + sa * w1, b1 + sb * w1, 0.0), at(a1 - sa * w1, b1 - sb * w1, 0.0)
            self.polys.append((0, None, [(lip0l, COOLED_RED), (lip1l, COOLED_RED), (f1l, MOLTEN_CORE), (f0l, MOLTEN_CORE)]))
            self.polys.append((0, None, [(lip0r, COOLED_RED), (f0r, MOLTEN_CORE), (f1r, MOLTEN_CORE), (lip1r, COOLED_RED)]))
            self.polys.append((0, None, [(f0l, MOLTEN_HOT), (f1l, MOLTEN_HOT), (f1r, MOLTEN_HOT), (f0r, MOLTEN_HOT)]))

    def socket(self, centre, normal, across, outline, depth):
        """An eye: a flat mouth of the typed `outline` [(u, v), ...] on the face, and a deep cup whose floor is the hottest."""
        n = unit(normal); u = unit(across); v = unit(cross(n, u))

        def at(a, b, d): return add(add(add(centre, mul(u, a)), mul(v, b)), mul(n, d))
        self.polys.append((1, (0, 0, 0, 0), [at(a, b, .004) for a, b in outline]))
        floor = [at(a * .5, b * .5 - .02, -depth) for a, b in outline]
        lip = [at(a, b, 0.0) for a, b in outline]
        k = len(outline)
        for i in range(k):
            j = (i + 1) % k
            self.polys.append((0, None, [(lip[i], COOLED_RED), (lip[j], COOLED_RED), (floor[j], MOLTEN_CORE), (floor[i], MOLTEN_CORE)]))
        self.polys.append((0, None, [(p, MOLTEN_HOT) for p in floor]))


def _glow_mesh(glow):
    pos, nrm, uv, col, idx = [], [], [], [], []
    for role, _, pts in glow.polys:
        points = [p if role == 1 else p[0] for p in pts]
        colours = [(0.0, 0.0, 0.0, 0.0)] * len(points) if role == 1 else [(c[0], c[1], c[2], 1.0) for _, c in pts]
        n = bp._normal(points)
        first = len(pos)
        for p, c in zip(points, colours):
            pos.append(p); nrm.append(n); uv.append((float(role), 0.0)); col.append(c)
        for k in range(1, len(points) - 1):
            idx.extend((first, first + k, first + k + 1))
    return pos, nrm, uv, col, idx


def write(path, nodes):
    """`build_paete_props.write`, plus the glow nodes (COLOR_0, and TEXCOORD0 as the SoulGlow role, not an atlas cell)."""
    donor, dblob = pv.read_glb(bp.DONOR)
    blob = bytearray()
    views, accessors, meshes, gnodes = [], [], [], []

    def view(data, target=None):
        while len(blob) % 4: blob.append(0)
        offset = len(blob); blob.extend(data)
        v = {"buffer": 0, "byteOffset": offset, "byteLength": len(data)}
        if target: v["target"] = target
        views.append(v); return len(views) - 1

    def accessor(values, kind, width, target, bounds=False):
        flat = [c for p in values for c in p] if width > 1 else list(values)
        fmt = "I" if kind == "SCALAR" else "f"
        a = {"bufferView": view(struct.pack(f"<{len(flat)}{fmt}", *flat), target),
             "componentType": 5125 if kind == "SCALAR" else 5126, "count": len(values), "type": kind}
        if bounds:
            a["min"] = [min(p[k] for p in values) for k in range(3)]; a["max"] = [max(p[k] for p in values) for k in range(3)]
        accessors.append(a); return len(accessors) - 1

    # The atlas and the material come from Paete's donor model, as for his props: the palette slots mean the same cells.
    image = donor["images"][0]
    if "uri" in image:
        import shutil
        target = os.path.join(os.path.dirname(path), image["uri"])
        os.makedirs(os.path.dirname(target), exist_ok=True)
        shutil.copyfile(os.path.join(os.path.dirname(bp.DONOR), image["uri"]), target)
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
        if isinstance(node, Glow) and node.polys:
            pos, nrm, uv, col, idx = _glow_mesh(node)
            attrs = {"POSITION": accessor(pos, "VEC3", 3, 34962, True), "NORMAL": accessor(nrm, "VEC3", 3, 34962),
                     "TEXCOORD_0": accessor(uv, "VEC2", 2, 34962), "COLOR_0": accessor(col, "VEC4", 4, 34962)}
            meshes.append({"name": node.name, "primitives": [{"attributes": attrs, "indices": accessor(idx, "SCALAR", 1, 34963), "material": 0}]})
            entry["mesh"] = len(meshes) - 1
        elif not isinstance(node, Glow) and node.parts:
            pos, nrm, uv, idx = bp._mesh(node)
            attrs = {"POSITION": accessor(pos, "VEC3", 3, 34962, True), "NORMAL": accessor(nrm, "VEC3", 3, 34962),
                     "TEXCOORD_0": accessor(uv, "VEC2", 2, 34962)}
            meshes.append({"name": node.name, "primitives": [{"attributes": attrs, "indices": accessor(idx, "SCALAR", 1, 34963), "material": 0}]})
            entry["mesh"] = len(meshes) - 1
        gnodes.append(entry)
    for node in nodes:
        if node.parent is not None:
            gnodes[index_of[node.parent]].setdefault("children", []).append(index_of[node.name])
    roots = [index_of[n.name] for n in nodes if n.parent is None]
    gltf = {
        "asset": {"version": "2.0", "generator": "tools/build_dante_props.py"},
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
    tris = sum(len(bp._mesh(n)[3]) // 3 for n in nodes if not isinstance(n, Glow) and n.parts)
    tris += sum(len(_glow_mesh(n)[4]) // 3 for n in nodes if isinstance(n, Glow) and n.polys)
    print(f"{os.path.basename(path)}: {len(nodes)} nodes, {tris} triangles")


# ---------------------------------------------------------------------------------------------
# The beast.
# ---------------------------------------------------------------------------------------------
def horn(node, side, chipped):
    """His horn, at the beast's scale: out from the skull's top corner, up, then hooked in. Five sides, stepped radii (his
    model's horn is a five-plane loft). Each point typed. The chipped one stops short at a slanted break with a lighter scar."""
    s = side
    keys = [(s * 0.58, 0.48, 0.30), (s * 1.22, 0.62, 0.18), (s * 1.82, 0.94, 0.02), (s * 2.12, 1.48, -0.06),
            (s * 2.02, 2.02, 0.12), (s * 1.74, 2.34, 0.40)]
    radii = [0.44, 0.39, 0.32, 0.23, 0.14, 0.05]
    if chipped:
        keys, radii = keys[:5], [0.44, 0.39, 0.32, 0.23, 0.19]
        keys[4] = (s * 2.06, 1.84, 0.04)
    path = bp.spline(keys, 4)
    rr = bp.spread(radii, 4)
    node.tube(path, rr, HORN, sides=5, twist=18.0 * s)
    # Three growth steps round the root (his horn is stepped): typed collars, each its own girth.
    for k, (t, girth, slot) in enumerate([(1, 0.45, HORN_DK), (5, 0.41, HORN_DK), (9, 0.34, HORN)]):
        p0, p1 = path[t], path[t + 1]
        node.tube([p0, p1], [girth, girth * 0.96], slot, sides=5, twist=18.0 * s)
    if chipped:
        # The break: a pale, flat scar on the stump's slanted end, and one flake still hanging off it.
        tip = path[-1]
        node.obox(add(tip, (s * 0.02, 0.06, 0.02)), (0.30, 0.06, 0.28), PALE, yaw=30.0 * s, pitch=-24.0 * s, roll=12.0)
        node.obox(add(tip, (s * -0.10, -0.06, 0.10)), (0.10, 0.12, 0.06), HORN_DK, yaw=-15.0 * s, pitch=40.0, roll=-20.0)


def fist(node, side):
    """A fist of stacked basalt: the block of the hand, four knuckles each its own size and tilt, a thumb laid over the front."""
    s = side
    node.obox((0.0, 0.0, 0.0), (1.10, 0.86, 1.02), BASALT, yaw=4.0 * s, roll=3.0)
    node.obox((0.0, 0.30, -0.12), (0.92, 0.32, 0.70), WARM, yaw=-6.0 * s)                  # the back of the hand
    for x, y, z, w, h, d, pitch, slot in [(s * 0.36, -0.06, 0.52, 0.26, 0.30, 0.22, 8.0, DARK),
                                          (s * 0.11, -0.03, 0.56, 0.28, 0.33, 0.24, 3.0, BASALT),
                                          (s * -0.15, -0.05, 0.54, 0.27, 0.31, 0.23, -4.0, DARK),
                                          (s * -0.38, -0.10, 0.48, 0.22, 0.27, 0.20, -10.0, BASALT)]:
        node.obox((x, y, z), (w, h, d), slot, pitch=pitch)
    node.obox((s * 0.18, -0.30, 0.40), (0.62, 0.22, 0.28), WARM_DK, yaw=-12.0 * s, roll=-6.0 * s)  # the thumb, across
    node.obox((0.0, -0.42, 0.0), (0.98, 0.10, 0.90), DEEP)                                    # the heel it lands on


def beast():
    nodes = []
    root = Node("beast"); nodes.append(root)
    body = Node("body", parent="beast"); nodes.append(body)

    # THE MASS: a hump of basalt, rear lower than the shoulders, every block its own size and lean.
    body.obox((0.0, 1.05, -1.05), (2.55, 2.10, 1.90), BASALT, yaw=-3.0, roll=2.0)          # haunch
    body.obox((0.0, 1.55, 0.35), (3.05, 2.50, 2.10), WARM, yaw=2.0, pitch=-1.5)            # chest
    body.obox((0.05, 2.92, -0.25), (2.25, 0.96, 2.05), BASALT, yaw=-2.0)                    # the saddle, top at 3.4
    body.obox((0.0, 2.55, 0.95), (2.55, 0.80, 1.10), DARK, yaw=4.0, pitch=6.0)             # withers, over the neck
    body.obox((L * 1.05, 0.85, -0.10), (1.10, 1.70, 2.70), DARK, yaw=5.0, roll=-4.0)       # its left flank
    body.obox((R * 1.10, 0.80, 0.05), (1.05, 1.60, 2.55), DARK, yaw=-6.0, roll=5.0)        # its right flank
    body.obox((0.0, 0.32, 1.25), (2.30, 0.64, 0.95), DEEP, yaw=0.0)                         # the chest's foot in the court
    # Shoulders: two boulders, not a pair: the left rides higher and turns out more.
    body.obox((L * 1.95, 2.42, 0.78), (1.55, 1.40, 1.62), WARM_DK, yaw=18.0, roll=-11.0)
    body.obox((R * 1.92, 2.30, 0.70), (1.48, 1.32, 1.58), WARM_DK, yaw=-14.0, roll=9.0)
    body.obox((L * 1.70, 3.05, 0.55), (0.90, 0.42, 0.95), LIT, yaw=24.0, roll=-16.0)       # a cap on the left shoulder
    # Loose rubble at its foot, where it broke out of the court: five typed chunks.
    for c, size, y, slot in [((L * 1.62, 0.16, 1.62), (0.52, 0.32, 0.44), 22.0, DUST), ((R * 1.34, 0.12, 1.80), (0.40, 0.24, 0.36), -18.0, DUST),
                             ((L * 0.42, 0.10, 2.05), (0.30, 0.20, 0.26), 41.0, DARK), ((R * 2.05, 0.14, -0.55), (0.46, 0.28, 0.40), 9.0, DUST),
                             ((L * 2.00, 0.12, -1.20), (0.36, 0.24, 0.30), -33.0, DARK)]:
        body.obox(c, size, slot, yaw=y)

    # THE CRAGS: it is a mountain, so its shoulders and haunch break into peaks. Each a four-sided spire, typed: base, lean,
    # height, girth. Uneven on purpose: the left shoulder carries the tallest range, the right two lower, the haunch one.
    for base, top, r0, slot in [((L * 2.10, 3.02, 0.70), (L * 2.32, 4.18, 0.42), 0.40, LIT), ((L * 1.62, 3.12, 1.05), (L * 1.58, 3.78, 1.20), 0.28, MID),
                                ((L * 2.40, 2.90, 0.20), (L * 2.78, 3.52, -0.10), 0.24, BASALT),
                                ((R * 2.02, 2.88, 0.62), (R * 2.30, 3.82, 0.48), 0.34, MID), ((R * 1.60, 2.98, 0.20), (R * 1.78, 3.50, -0.08), 0.24, LIT),
                                ((R * 1.10, 2.40, -1.60), (R * 1.34, 3.30, -1.92), 0.32, LIT)]:
        # Chunky, broken-topped: a wide foot, a shoulder halfway, a blunt top (a crag, not a spike).
        mid = (base[0] * 0.55 + top[0] * 0.45, base[1] * 0.55 + top[1] * 0.45, base[2] * 0.55 + top[2] * 0.45)
        body.tube([base, mid, top], [r0 * 1.45, r0 * 1.05, r0 * 0.30], slot, sides=5, twist=14.0)

    # THE BACK PLATES that shed rock as it rises: three slabs, each its own node, laid on the flanks and the rump.
    for name, at, yaw, size, pitch, roll, slot in [("back-0", (L * 1.08, 2.86, -0.55), 8.0, (1.30, 0.34, 1.65), 4.0, -24.0, LIT),
                                                   ("back-1", (R * 1.15, 2.74, -0.30), -6.0, (1.22, 0.32, 1.80), -3.0, 27.0, MID),
                                                   ("back-2", (0.0, 2.42, -1.78), 3.0, (2.05, 0.36, 1.05), 34.0, 2.0, LIT)]:
        n = Node(name, origin=at, parent="body"); nodes.append(n)
        n.obox((0.0, 0.0, 0.0), size, slot, yaw=yaw, pitch=pitch, roll=roll)
        # Each carries one chip already lifting off its edge: the first piece to fall.
        w, h, d = size
        n.obox((w * 0.28, h * 0.62, d * 0.30), (0.34, 0.20, 0.30), DARK, yaw=yaw + 22.0, pitch=pitch + 10.0, roll=roll - 8.0)

    # THE HEAD: low and forward, a carabao's broad skull.
    head = Node("head", origin=(0.0, 1.62, 1.95), parent="body"); nodes.append(head)
    head.obox((0.0, 0.05, 0.38), (1.55, 1.20, 1.30), BASALT, yaw=0.0)                       # skull
    head.obox((0.0, -0.36, 1.08), (1.12, 0.78, 0.86), WARM, pitch=4.0)                      # muzzle
    # Nostrils: two low flared slits under the muzzle's front edge, never two dots (dots at eye height read as a cow's eyes).
    head.obox((L * 0.27, -0.66, 1.50), (0.24, 0.06, 0.05), DEEP, roll=-14.0)
    head.obox((R * 0.26, -0.65, 1.50), (0.22, 0.06, 0.05), DEEP, roll=12.0)
    head.obox((L * 0.80, 0.02, 0.30), (0.20, 0.62, 0.86), DARK, yaw=-6.0)                   # cheek slabs, not mirrored
    head.obox((R * 0.79, -0.04, 0.34), (0.20, 0.56, 0.80), DARK, yaw=8.0)
    brow = Node("brow", origin=(0.0, 0.48, 0.80), parent="head"); nodes.append(brow)
    # The overhang the eyes sit under: two heavy halves dropping toward the middle in a V, a scowl, never a level shelf.
    brow.obox((L * 0.44, 0.02, 0.06), (0.92, 0.36, 0.66), DARK, yaw=-6.0, roll=-13.0)
    brow.obox((R * 0.45, 0.00, 0.05), (0.90, 0.34, 0.64), DARK, yaw=7.0, roll=12.0)
    brow.obox((0.0, -0.10, 0.16), (0.36, 0.30, 0.50), DEEP, pitch=10.0)                       # the knot between them
    brow.obox((L * 0.62, 0.22, -0.14), (0.58, 0.20, 0.46), BASALT, yaw=-10.0, roll=-8.0)
    brow.obox((R * 0.60, 0.20, -0.12), (0.54, 0.18, 0.44), BASALT, yaw=11.0, roll=6.0)
    jaw = Node("jaw", origin=(0.0, -0.78, 0.70), parent="head"); nodes.append(jaw)
    jaw.obox((0.0, 0.0, 0.32), (1.02, 0.34, 0.92), WARM_DK)
    jaw.obox((0.0, -0.12, 0.72), (0.70, 0.18, 0.20), DEEP)
    hl = Node("horn-left", parent="head"); nodes.append(hl)
    horn(hl, L, chipped=True)
    hr = Node("horn-right", parent="head"); nodes.append(hr)
    horn(hr, R, chipped=False)

    # THE EYES: sunken lit sockets on the skull's face under the brow (skull face z = 0.38 + 0.65 = 1.03 in the head).
    eyes = Glow("glow-eyes", parent="head"); nodes.append(eyes)
    face_z = 1.03
    # Slanted down toward the middle under the V (u runs toward +X here, so the inner corner of the left eye is at -u).
    eyes.socket((L * 0.40, 0.17, face_z), (0, 0, 1), (1, 0, 0), [(-0.25, -0.04), (-0.05, 0.04), (0.23, 0.12), (0.20, -0.05), (-0.13, -0.12)], 0.30)
    eyes.socket((R * 0.40, 0.16, face_z), (0, 0, 1), (1, 0, 0), [(-0.22, 0.11), (0.04, 0.04), (0.25, -0.05), (0.13, -0.12), (-0.19, -0.06)], 0.30)

    # THE ARMS: each hangs straight down -Y from its shoulder pivot, so the cutscene raises it by pitching the node.
    for side, name, at in [(L, "arm-left", (L * 2.62, 2.45, 0.88)), (R, "arm-right", (R * 2.70, 2.42, 0.62))]:
        arm = Node(name, origin=at, parent="body"); nodes.append(arm)
        arm.obox((0.0, -0.55, 0.0), (1.02, 1.15, 1.00), WARM_DK, roll=4.0 * side)            # upper arm
        arm.obox((side * 0.04, -1.55, 0.10), (0.90, 1.05, 0.92), BASALT, pitch=3.0, roll=-3.0 * side)  # forearm
        arm.obox((side * 0.10, -1.05, 0.30), (0.46, 0.34, 0.40), DARK, yaw=20.0 * side)       # an elbow chip
        f = Node("fist-" + name.split("-")[1], origin=(side * 0.02, -2.42, 0.18), parent=name); nodes.append(f)
        fist(f, side)

    # THE SEAMS: cracks cut into its chest and shoulders, each stroke typed; the light runs from here to the eyes.
    seams = Glow("glow-seams", parent="body"); nodes.append(seams)
    chest_front = 0.35 + 1.05 + 0.002                                                        # the chest box's front face
    seams.cut((0.0, 1.55, chest_front), (0, 0, 1), (1, 0, 0),
              [(L * 0.10, -1.05, 0.040), (L * 0.18, -0.62, 0.055), (L * 0.02, -0.20, 0.060), (R * 0.14, 0.22, 0.052), (R * 0.06, 0.70, 0.040)], 0.18)
    seams.cut((0.0, 1.55, chest_front), (0, 0, 1), (1, 0, 0),
              [(L * 0.02, -0.20, 0.032), (L * 0.42, -0.04, 0.030), (L * 0.70, 0.18, 0.018)], 0.12)
    seams.cut((0.0, 1.55, chest_front), (0, 0, 1), (1, 0, 0),
              [(R * 0.14, 0.22, 0.028), (R * 0.52, 0.30, 0.022)], 0.10)
    # Along the saddle's top, where his plate rides: one dominant line and a short chip.
    seams.cut((0.05, 3.402, -0.25), (0, 1, 0), (1, 0, 0),
              [(L * 0.85, 0.62, 0.030), (L * 0.30, 0.20, 0.045), (R * 0.20, -0.18, 0.040), (R * 0.78, -0.70, 0.026)], 0.14)
    seams.cut((0.05, 3.402, -0.25), (0, 1, 0), (1, 0, 0), [(L * 0.30, 0.20, 0.024), (L * 0.05, 0.72, 0.016)], 0.08)

    write(os.path.join(OUT, "beast.glb"), nodes)
    return nodes


def measure(nodes):
    """The beast's extents (this file's space, so -X is its right), for the cutscene's numbers."""
    lo, hi = [1e9] * 3, [-1e9] * 3
    origin = {n.name: n.origin for n in nodes}
    parent = {n.name: n.parent for n in nodes}

    def world(name, p):
        while name is not None:
            p = add(p, origin[name]); name = parent[name]
        return p
    for n in nodes:
        pts = []
        if isinstance(n, Glow): pts = [q if r == 1 else q[0] for r, _, ps in n.polys for q in ps]
        else: pts = [q for _, faces in n.parts for f in faces for q in f]
        for q in pts:
            w = world(n.name, q)
            for k in range(3): lo[k] = min(lo[k], w[k]); hi[k] = max(hi[k], w[k])
    print("extent x %.2f..%.2f  y %.2f..%.2f  z %.2f..%.2f" % (lo[0], hi[0], lo[1], hi[1], lo[2], hi[2]))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    built = beast() if (not args or "beast" in args) else None
    if "--measure" in sys.argv and built: measure(built)
