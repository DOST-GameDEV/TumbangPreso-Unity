"""Shared kit for the heroes' first-person hand companions (`ViewmodelArms.HandLife`): shapes, a model writer, a sheet.

A hand companion is a small modelled THING in the cast's toon dress (flat palette colour, ink line, chunky rounded
forms). One hero's build script (tools/build_hand_<hero>.py) imports this, makes named parts, and writes
`Assets/TumbangPreso/Resources/Models/HandCompanions/<hero>.glb`. No Blender is needed to BUILD; Blender is used to
LOOK (tools/review_hand_companion.py), because the game cannot be rendered while the owner's editor is open.

Conventions (the same as the cast and `tools/kubo_kuro.py`):
  * metres, y up, +z is the thing's FRONT (its face).
  * a mesh is (positions float32 Nx3, normals float32 Nx3, triangles uint32 Mx3), counter-clockwise from outside.
  * colour is a SLOT 0..15 of the hero's own sixteen-colour palette, written as a UV cell (`TumbangPreso/Toon`'s rule);
    the palette itself lives in the hero's C# class and is passed to `ToonSkin.Apply`. No textures.
  * every part is its own named node so the C# side can pose it (find by name, move, turn, scale about its origin).
    Put a part's ORIGIN where it should pivot: build the mesh around (0,0,0) and place it with `at`.
"""
import json
import math
import struct
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / "Assets/TumbangPreso/Resources/Models/HandCompanions"
LOGS = ROOT / "Logs/hand-companions"


# ------------------------------------------------------------------ shapes

def _sphere(seg, rings):
    pts, tris = [], []
    for r in range(rings + 1):
        v = math.pi * r / rings
        for s in range(seg + 1):
            u = 2 * math.pi * ((s % seg) + .5) / seg            # half a step off the axes: see kubo_kuro.py
            ring = 0.0 if r in (0, rings) else math.sin(v)       # poles exactly on the axis
            pts.append((ring * math.cos(u), 1.0 if r == 0 else -1.0 if r == rings else math.cos(v), ring * math.sin(u)))
    for r in range(rings):
        for s in range(seg):
            a = r * (seg + 1) + s
            b = a + seg + 1
            if r > 0:
                tris.append((a, a + 1, b))
            if r < rings - 1:
                tris.append((a + 1, b + 1, b))
    return np.array(pts, np.float32), np.array(tris, np.uint32)


def rounded(half, radius, at=(0, 0, 0), seg=20, rings=12):
    """A box of half-size `half` with every edge and corner rounded to `radius`: the cast's chunky block."""
    n, tris = _sphere(seg, rings)
    half = np.array(half, np.float32)
    core = np.maximum(half - radius, 0)
    scale = np.minimum(half, radius)
    pos = np.sign(n) * core + n * scale + np.array(at, np.float32)
    nrm = n / scale
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True)
    return pos.astype(np.float32), nrm.astype(np.float32), tris


def ellipsoid(half, at=(0, 0, 0), seg=16, rings=10):
    """A ball or bean of half-size `half`."""
    n, tris = _sphere(seg, rings)
    half = np.array(half, np.float32)
    pos = n * half + np.array(at, np.float32)
    nrm = n / half
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True)
    return pos.astype(np.float32), nrm.astype(np.float32), tris


def lathe(profile, seg=16, at=(0, 0, 0), squash_z=1.0):
    """A shape turned about y from `profile`, a list of (radius, y) from BOTTOM to TOP. Radius 0 at an end closes it to a
    point; an open end is capped flat. Smooth normals. Use for cones, flames, drops, cups, horns, bottles."""
    prof = list(profile)
    if prof[0][0] > 1e-6:
        prof.insert(0, (0.0, prof[0][1]))
    if prof[-1][0] > 1e-6:
        prof.append((0.0, prof[-1][1]))
    pts, nrm, tris = [], [], []
    for r, (radius, y) in enumerate(prof):
        lo, hi = prof[max(0, r - 1)], prof[min(len(prof) - 1, r + 1)]
        dr, dy = hi[0] - lo[0], hi[1] - lo[1]
        length = math.hypot(dr, dy) or 1.0
        out, up = dy / length, -dr / length                    # the profile's own normal
        for s in range(seg + 1):
            u = 2 * math.pi * (s % seg) / seg
            pts.append((radius * math.cos(u), y, radius * math.sin(u) * squash_z))
            nrm.append((out * math.cos(u), up, out * math.sin(u)) if radius > 1e-6 or r not in (0, len(prof) - 1)
                       else (0, -1 if r == 0 else 1, 0))
    for r in range(len(prof) - 1):
        for s in range(seg):
            a = r * (seg + 1) + s
            b = a + seg + 1
            if prof[r + 1][0] > 1e-6:
                tris.append((a, b, b + 1))
            if prof[r][0] > 1e-6:
                tris.append((a, b + 1, a + 1))
    pts = np.array(pts, np.float32) + np.array(at, np.float32)
    nrm = np.array(nrm, np.float32)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-6)
    return pts, nrm, np.array(tris, np.uint32)


def tube(points, radius, seg=10):
    """A round tube along `points` (a list of xyz), closed with domes. `radius` is one number or one per point: a vine,
    a rope, a tail, a horn, a bolt's limb. Smooth normals."""
    p = np.array(points, np.float32)
    count = len(p)
    radii = np.full(count, radius, np.float32) if np.isscalar(radius) else np.array(radius, np.float32)
    tangent = np.gradient(p, axis=0)
    tangent /= np.maximum(np.linalg.norm(tangent, axis=1, keepdims=True), 1e-6)
    ref = np.array([0, 0, 1], np.float32) if abs(tangent[0][2]) < .9 else np.array([1, 0, 0], np.float32)
    side = np.cross(tangent[0], ref)
    side /= np.linalg.norm(side)
    pts, nrm, tris = [], [], []
    rows = []
    for i in range(count):
        # Carry the frame along the path so the tube does not twist.
        side = side - tangent[i] * float(np.dot(side, tangent[i]))
        side /= max(float(np.linalg.norm(side)), 1e-6)
        other = np.cross(tangent[i], side)
        rows.append((p[i], side.copy(), other, radii[i], 0.0))
    # Domes: two extra shrinking rings past each end.
    first, last = rows[0], rows[-1]
    head = [(first[0] - tangent[0] * first[3] * k, first[1], first[2], first[3] * math.sqrt(max(0, 1 - k * k)), -k) for k in (1.0, .7)]
    foot = [(last[0] + tangent[-1] * last[3] * k, last[1], last[2], last[3] * math.sqrt(max(0, 1 - k * k)), k) for k in (.7, 1.0)]
    rows = head + rows + foot
    tangents = [tangent[0]] * 2 + list(tangent) + [tangent[-1]] * 2
    for (centre, a, b, r, lean), t in zip(rows, tangents):
        for s in range(seg + 1):
            u = 2 * math.pi * (s % seg) / seg
            ring = a * math.cos(u) + b * math.sin(u)
            pts.append(centre + ring * r)
            n = ring * math.sqrt(max(0.0, 1 - lean * lean)) + t * lean
            nrm.append(n)
    for r in range(len(rows) - 1):
        for s in range(seg):
            a = r * (seg + 1) + s
            b = a + seg + 1
            tris.append((a, a + 1, b)); tris.append((a + 1, b + 1, b))
    nrm = np.array(nrm, np.float32)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-6)
    return np.array(pts, np.float32), nrm, np.array(tris, np.uint32)


def slab(outline, depth, at=(0, 0, 0)):
    """A flat silhouette (a list of xy, counter-clockwise, CONVEX or star-shaped about its middle) extruded `depth`
    along z, centred: a leaf, a star, a bolt, a feather, a paper tag, a pinwheel blade. Hard edges."""
    o = np.array(outline, np.float32)
    mid = o.mean(axis=0)
    z = depth * .5
    pos, nrm, tris = [], [], []

    def add(p, n):
        pos.append(p); nrm.append(n)
        return len(pos) - 1

    n_out = len(o)
    front = [add((x, y, z), (0, 0, 1)) for x, y in o] + [add((mid[0], mid[1], z), (0, 0, 1))]
    back = [add((x, y, -z), (0, 0, -1)) for x, y in o] + [add((mid[0], mid[1], -z), (0, 0, -1))]
    for i in range(n_out):
        j = (i + 1) % n_out
        tris.append((front[i], front[j], front[-1]))
        tris.append((back[j], back[i], back[-1]))
        (x0, y0), (x1, y1) = o[i], o[j]
        n = np.array([y1 - y0, -(x1 - x0), 0], np.float32)
        n /= max(float(np.linalg.norm(n)), 1e-6)
        a, b, c, d = add((x0, y0, z), n), add((x1, y1, z), n), add((x1, y1, -z), n), add((x0, y0, -z), n)
        tris.append((a, d, c)); tris.append((a, c, b))
    return np.array(pos, np.float32) + np.array(at, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32)


def join(*meshes):
    pos, nrm, tris, base = [], [], [], 0
    for p, n, t in meshes:
        pos.append(p); nrm.append(n); tris.append(t + base); base += len(p)
    return np.concatenate(pos), np.concatenate(nrm), np.concatenate(tris)


def mirror(mesh):
    """The same piece on the other side of x."""
    p, n, t = mesh
    flip = np.array([-1, 1, 1], np.float32)
    return p * flip, n * flip, t[:, ::-1].copy()


def moved(mesh, at=(0, 0, 0), turn=(0, 0, 0), scale=(1, 1, 1)):
    """`mesh` scaled, then turned by `turn` degrees about x, then y, then z, then moved to `at`."""
    p, n, t = mesh
    s = np.array(scale, np.float32)
    rx, ry, rz = (math.radians(a) for a in turn)
    mx = np.array([[1, 0, 0], [0, math.cos(rx), -math.sin(rx)], [0, math.sin(rx), math.cos(rx)]], np.float32)
    my = np.array([[math.cos(ry), 0, math.sin(ry)], [0, 1, 0], [-math.sin(ry), 0, math.cos(ry)]], np.float32)
    mz = np.array([[math.cos(rz), -math.sin(rz), 0], [math.sin(rz), math.cos(rz), 0], [0, 0, 1]], np.float32)
    m = mz @ my @ mx
    p2 = (p * s) @ m.T + np.array(at, np.float32)
    n2 = (n / s) @ m.T
    n2 /= np.maximum(np.linalg.norm(n2, axis=1, keepdims=True), 1e-6)
    return p2.astype(np.float32), n2.astype(np.float32), t


# ------------------------------------------------------------------ the model

def grid_uv(mesh, rect, cols, along="rows", axis=1):
    """UVs for a mesh made by `ellipsoid`, `lathe` or `tube` (rings of `cols` + 1 points), laid into `rect`
    (u0, v0, u1, v1 of the atlas, v down as glTF has it). Round the piece is left to right; `along` is bottom to top
    of the rect: "rows" by ring number, "axis" by the position along `axis` (0 x, 1 y, 2 z), for a piece whose rings
    are not evenly spaced. Call it on the piece as BUILT, before `moved` turns it."""
    pos = mesh[0]
    count = len(pos)
    rows = count // (cols + 1)
    u0, v0, u1, v1 = rect
    uv = np.zeros((count, 2), np.float32)
    lo, hi = float(pos[:, axis].min()), float(pos[:, axis].max())
    for i in range(count):
        r, c = divmod(i, cols + 1)
        t = r / max(1, rows - 1) if along == "rows" else (float(pos[i, axis]) - lo) / max(1e-6, hi - lo)
        uv[i] = (u0 + (u1 - u0) * c / cols, v1 - (v1 - v0) * t)
    return uv


def cell(slot):
    """The UV that means palette slot `slot` to `TumbangPreso/Toon` (and to `tools/kubo_kuro.py`'s Kuro)."""
    return ((2 * (slot % 8) + 1.5) / 16.0, 0.5938 if slot < 8 else 0.8438)


class Model:
    """Named parts, each one node. `add(name, pieces, at, parent)`; `pieces` is a mesh with one slot, or a list of
    (mesh, slot) for a part in several colours."""

    def __init__(self, name):
        self.name = name
        self.parts = []
        self.yaws = {}

    def add(self, name, pieces, slot=None, at=(0, 0, 0), parent=None, yaw=0.0):
        """`yaw` (degrees about y, 0 = +z, 90 = +x) turns the NODE, so a part built pointing along +z can be stood
        round a centre and still be pitched about its own x by the game (a root, a leaf of a rosette)."""
        if slot is not None:
            pieces = [(pieces, slot)]
        self.parts.append((name, pieces, tuple(float(v) for v in at), parent))
        self.yaws[name] = float(yaw)
        return name

    def write(self, palette_hex=None):
        """Writes the .glb, and (for the review) a small json of the palette beside the review pictures."""
        doc = {"asset": {"version": "2.0", "generator": "tools/hand_companion_kit.py"}, "scene": 0, "scenes": [{"nodes": [0]}],
               "nodes": [{"name": self.name, "children": []}], "meshes": [], "accessors": [], "bufferViews": [],
               "materials": [{"name": "HandCompanionPalette", "pbrMetallicRoughness": {"baseColorFactor": [1, 1, 1, 1], "metallicFactor": 0, "roughnessFactor": 1}}],
               "buffers": [{"byteLength": 0}]}
        blob = bytearray()

        def put(array, kind, target, bounds=False):
            while len(blob) % 4:
                blob.append(0)
            doc["bufferViews"].append({"buffer": 0, "byteOffset": len(blob), "byteLength": array.nbytes, "target": target})
            blob.extend(array.tobytes())
            a = {"bufferView": len(doc["bufferViews"]) - 1, "componentType": 5126 if array.dtype == np.float32 else 5125,
                 "count": int(array.shape[0]) if array.ndim > 1 else int(array.size), "type": kind}
            if bounds:
                a["min"] = [float(v) for v in array.min(axis=0)]
                a["max"] = [float(v) for v in array.max(axis=0)]
            doc["accessors"].append(a)
            return len(doc["accessors"]) - 1

        index = {}
        triangles = 0
        for name, pieces, at, parent in self.parts:
            pos = np.concatenate([m[0] for m, _ in pieces]).astype(np.float32)
            nrm = np.concatenate([m[1] for m, _ in pieces]).astype(np.float32)
            # A slot is one flat palette colour; an array of UVs is a PAINTED piece (see `grid_uv`).
            uv = np.concatenate([np.tile(np.array(cell(slot), np.float32), (len(m[0]), 1)) if np.isscalar(slot)
                                 else np.asarray(slot, np.float32) for m, slot in pieces])
            tris, base = [], 0
            for m, _ in pieces:
                tris.append(m[2] + base); base += len(m[0])
            tris = np.concatenate(tris).astype(np.uint32)
            triangles += len(tris)
            doc["meshes"].append({"name": name, "primitives": [{"attributes": {
                "POSITION": put(pos, "VEC3", 34962, True), "NORMAL": put(nrm, "VEC3", 34962), "TEXCOORD_0": put(uv, "VEC2", 34962)},
                "indices": put(tris.reshape(-1), "SCALAR", 34963), "material": 0}]})
            doc["nodes"].append({"name": name, "mesh": len(doc["meshes"]) - 1, "translation": list(at)})
            if self.yaws.get(name):
                half = math.radians(self.yaws[name]) * .5
                doc["nodes"][-1]["rotation"] = [0.0, math.sin(half), 0.0, math.cos(half)]
            index[name] = len(doc["nodes"]) - 1
        for name, _, _, parent in self.parts:
            owner = doc["nodes"][index[parent]] if parent else doc["nodes"][0]
            owner.setdefault("children", []).append(index[name])
        if getattr(self, "texture", None):
            # A painted atlas beside the model, as the redesigned cast wear theirs: `TumbangPreso/Toon` takes the
            # palette for a UV in the atlas's lower half (where `cell` puts the flat slots) and the paint for the upper.
            doc["images"] = [{"uri": self.texture}]
            doc["samplers"] = [{"magFilter": 9729, "minFilter": 9987, "wrapS": 10497, "wrapT": 10497}]
            doc["textures"] = [{"source": 0, "sampler": 0}]
            doc["materials"][0]["pbrMetallicRoughness"]["baseColorTexture"] = {"index": 0}
        while len(blob) % 4:
            blob.append(0)
        doc["buffers"][0]["byteLength"] = len(blob)
        text = json.dumps(doc, separators=(",", ":")).encode()
        text += b" " * (-len(text) % 4)
        folder = getattr(self, "folder", None) or MODELS      # a prop that is not a hand companion says where it goes
        folder.mkdir(parents=True, exist_ok=True)
        path = folder / (self.name + ".glb")
        path.write_bytes(struct.pack("<4sII", b"glTF", 2, 28 + len(text) + len(blob)) + struct.pack("<I4s", len(text), b"JSON") + text
                         + struct.pack("<I4s", len(blob), b"BIN\0") + bytes(blob))
        LOGS.mkdir(parents=True, exist_ok=True)
        if palette_hex:
            (LOGS / (self.name + ".palette.json")).write_text(json.dumps(palette_hex))
        print("wrote %s: %d parts, %d triangles, %d bytes" % (path, len(self.parts), triangles, path.stat().st_size))
        return path


def sheet(hero, version, views=("front", "quarter", "side", "back", "under")):
    """Joins the pictures `tools/review_hand_companion.py` rendered into one sheet (system Python; needs Pillow)."""
    from PIL import Image, ImageDraw
    tiles = [Image.open(LOGS / ("%s_%s_%s.png" % (hero, version, v))).convert("RGB") for v in views]
    w, h = tiles[0].size
    out = Image.new("RGB", (w * len(tiles), h + 18), (20, 22, 30))
    d = ImageDraw.Draw(out)
    for k, (im, v) in enumerate(zip(tiles, views)):
        out.paste(im, (k * w, 18)); d.text((k * w + 6, 3), "%s %s  %s" % (hero, version, v), fill=(255, 255, 255))
    path = LOGS / ("%s_%s.png" % (hero, version))
    out.save(path)
    for v in views:
        (LOGS / ("%s_%s_%s.png" % (hero, version, v))).unlink()
    print("wrote", path)
    return path


if __name__ == "__main__":
    import sys
    if len(sys.argv) == 4 and sys.argv[1] == "sheet":
        sheet(sys.argv[2], sys.argv[3])
    else:
        print(__doc__)
