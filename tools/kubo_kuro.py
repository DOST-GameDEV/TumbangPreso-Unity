"""Kuro remodelled cuter ("Kubo"): the calm form's blocks rounded off, in place, on pet-nemu-ghost.glb.

Owner, 2026-10-06, of Kuro riding Nemu's first-person sleeve: "looks cute, but we need to remodel kuro to be more
cutesy", and of three blockouts (tools/review_kuro_cute_options.py): B, "this. both kuros". B is still the block he
has always been, rounded off and big-headed, with ear tufts, paws, big shining eyes, a cat's mouth and a stubby
flame tail.

  py -3 tools/kubo_kuro.py            rebuild the model from the kept original
  py -3 tools/kubo_kuro.py --restore  put the original back

WHAT IT CHANGES, AND ONLY THIS: the GEOMETRY of the seventeen calm parts under `RestoredCalm`.
  * No node is added, removed, renamed or moved. `GhostPetCompanion` finds his parts by name and poses them about
    their own origins, his baked idle clips key those nodes' transforms, and `KuroFormTests` counts them (five tail
    pieces, the mouth, the eyes). So the ears are the `ghost-body-top-rim` part and the paws the
    `ghost-body-bot-bevel` part: two pieces that were trim on a cube and have no job on a rounded body.
  * The drawn faces (`KuroExpressions`), the rage form, the rig, the animation and every material are untouched.
    The face stays a flat plane at the old depth across the middle, so the drawn faces still sit on it.
  * Colours are the palette cells the parts already used (each part keeps its own cell), so Nemu's palette and its
    recolours still dress him.

The original is kept beside the art source (ArtSource/kuro/pet-nemu-ghost.pre-kubo.glb) the first time this runs, and
every run rebuilds from THAT, so running it twice changes nothing.
"""
import json
import math
import shutil
import struct
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
GLB = ROOT / "Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb"
KEPT = ROOT / "ArtSource/kuro/pet-nemu-ghost.pre-kubo.glb"


# ------------------------------------------------------------------ shapes (y up, +z is his face)

def sphere(seg=20, rings=12):
    """Unit sphere: positions (which are its normals) and triangles. The columns sit HALF A STEP off the axes, so
    no vertex lies on an axis plane: `rounded` pushes each vertex out by the sign of its normal, and a vertex with a
    zero there has no side to go to (the first build had a seam down each side for it)."""
    pts, tris = [], []
    for r in range(rings + 1):
        v = math.pi * r / rings
        for s in range(seg + 1):
            u = 2 * math.pi * ((s % seg) + .5) / seg
            # The poles are EXACTLY on the axis. sin(pi) is 1e-16, not 0, and `rounded` reads the sign of that: the
            # bottom pole's points flew out to the four corners and left the flat underside open (seen in play from
            # below, 2026-10-06: "underside doesnt render").
            ring = 0.0 if r in (0, rings) else math.sin(v)
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
    """A box with every edge and corner rounded to `radius` (an ellipsoid when the radius is the half size)."""
    n, tris = sphere(seg, rings)
    half = np.array(half, np.float32)
    core = np.maximum(half - radius, 0)
    scale = np.minimum(half, radius)
    pos = np.sign(n) * core + n * scale + np.array(at, np.float32)
    nrm = n / scale
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True)
    return pos.astype(np.float32), nrm.astype(np.float32), tris


def blob(half, at=(0, 0, 0), seg=16, rings=10):
    return rounded(half, max(half), at, seg, rings) if False else _ellipsoid(half, at, seg, rings)


def _ellipsoid(half, at, seg, rings):
    n, tris = sphere(seg, rings)
    half = np.array(half, np.float32)
    pos = n * half + np.array(at, np.float32)
    nrm = n / half
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True)
    return pos.astype(np.float32), nrm.astype(np.float32), tris


def tuft(base, height, at, lean, seg=14, rings=6):
    """An ear: a fat cone with a rounded tip, leaning `lean` degrees outward about z."""
    pts, nrm, tris = [], [], []
    prof = []
    for r in range(rings + 1):
        t = r / rings
        # A cone that swells a little, closing to a small dome.
        radius = base * (1 - t) ** .75 * (1 + .12 * math.sin(t * math.pi)) + base * .10 * (1 - t)
        prof.append((radius if r < rings else 0.0, height * (t - .28)))
    for r, (radius, y) in enumerate(prof):
        for s in range(seg + 1):
            u = 2 * math.pi * s / seg
            pts.append((radius * math.cos(u), y, radius * math.sin(u) * .8))
            slope = base / height
            nrm.append((math.cos(u), slope if r < rings else 1.0, math.sin(u)))
    for r in range(rings):
        for s in range(seg):
            a = r * (seg + 1) + s
            b = a + seg + 1
            tris.append((a, b, a + 1))
            if r < rings - 1:
                tris.append((a + 1, b, b + 1))
    pts, nrm = np.array(pts, np.float32), np.array(nrm, np.float32)
    c, s = math.cos(math.radians(lean)), math.sin(math.radians(lean))
    turn = np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]], np.float32)
    pts = pts @ turn.T + np.array(at, np.float32)
    nrm = nrm @ turn.T
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True)
    return pts, nrm, np.array(tris, np.uint32)


def join(*parts):
    pos, nrm, tris, base = [], [], [], 0
    for p, n, t in parts:
        pos.append(p); nrm.append(n); tris.append(t + base); base += len(p)
    return np.concatenate(pos), np.concatenate(nrm), np.concatenate(tris)


def mirror(part):
    p, n, t = part
    return p * np.array([-1, 1, 1], np.float32), n * np.array([-1, 1, 1], np.float32), t[:, ::-1].copy()


# ------------------------------------------------------------------ Kubo, part by part (each in its node's own space)

def kubo():
    ear = tuft(.0150, .029, (.0250, .005, 0), -17)
    paw = _ellipsoid((.0115, .0105, .0115), (.0405, .018, .019), 14, 9)
    inner = _ellipsoid((.0072, .0105, .0030), (.0262, .0115, .0088), 12, 8)                # the inside of an ear, on its front
    band = rounded((.0423, .0042, .0373), .0042, (0, .0125, 0), 28, 8)                      # stands just proud of the body all round
    pad = _ellipsoid((.0052, .0046, .0026), (.0425, .0165, .0292), 10, 6)                   # a paw's pad, on its front
    shine = lambda: _ellipsoid((.0046, .0056, .0022), (-.0042, .0052, .0008), 12, 8)       # the big light, up and to one side
    spark = lambda: _ellipsoid((.0021, .0023, .0018), (.0046, -.0064, -.0006), 10, 6)      # the small one, opposite
    return {
        # The body: a block still, every edge rounded, a little wider than it is tall. The face plane stays flat
        # across the middle at the old depth (0.036), where the drawn faces sit.
        "ghost-body-core": rounded((.041, .0375, .036), .0165),
        "ghost-body-top-rim": join(ear, mirror(ear)),                 # node sits at y +0.038: the ear tufts
        "ghost-body-bot-bevel": join(paw, mirror(paw)),               # node sits at y -0.038: two paws, low and forward
        # STYLING (owner: "a bit lacking on textures and styling"). His parts are flat palette cells, so the styling is
        # more small pieces in other cells, riding the two parts above: the insides of his ears and a band round his
        # hem in the lavender of Nemu's own sleeve bands, and pale pads on his paws.
        "+ghost-body-top-rim": (join(inner, mirror(inner)), "ghost-tail-tier1"),
        "+ghost-body-bot-bevel": (join(band, pad, mirror(pad)), "ghost-tail-tier1"),
        # Big upright oval eyes, domed, a touch further apart than the squares were.
        "ghost-eye-l": _ellipsoid((.0122, .0152, .0042), (-.0018, .0005, -.0022), 20, 10),
        "ghost-eye-r": _ellipsoid((.0122, .0152, .0042), (.0018, .0005, -.0022), 20, 10),
        "ghost-eye-glint-l": shine(), "ghost-eye-glint-r": shine(),
        "ghost-eye-pupil-l": spark(), "ghost-eye-pupil-r": spark(),
        # A cat's mouth: two small lobes.
        "ghost-mouth-dot": join(_ellipsoid((.0036, .0021, .0028), (-.0031, 0, -.002), 10, 6), _ellipsoid((.0036, .0021, .0028), (.0031, 0, -.002), 10, 6)),
        "ghost-blush-l": _ellipsoid((.0062, .0034, .0022), (-.0068, .0012, -.0022), 12, 6),
        "ghost-blush-r": _ellipsoid((.0062, .0034, .0022), (.0068, .0012, -.0022), 12, 6),
        # The tail: a stubby flame of rounded steps, in the places the old wisps hung.
        # Each step overlaps the next, fat to thin, so it reads as one tapering flame and not as beads on a string.
        "ghost-tail-tier1": rounded((.0240, .0160, .0215), .0150),
        "ghost-tail-tier2": rounded((.0190, .0165, .0170), .0145),
        "ghost-tail-tier3": rounded((.0142, .0150, .0128), .0120),
        "ghost-tail-tip-wisp": _ellipsoid((.0098, .0112, .0088), (0, .002, 0), 12, 8),
        "ghost-tail-tip-glint": _ellipsoid((.0040, .0042, .0038), (0, 0, 0), 10, 6),
    }


# Which existing part's palette cell each rebuilt part wears (itself unless named here).
CELL_FROM = {"ghost-eye-pupil-l": "ghost-eye-glint-l", "ghost-eye-pupil-r": "ghost-eye-glint-r", "ghost-mouth-dot": "ghost-tail-tier3",
             "ghost-tail-tier3": "ghost-tail-tier1"}


# ------------------------------------------------------------------ the file

def read(path):
    data = path.read_bytes()
    n = struct.unpack_from("<I", data, 12)[0]
    doc = json.loads(data[20:20 + n])
    bn = struct.unpack_from("<I", data, 20 + n)[0]
    return doc, bytearray(data[28 + n:28 + n + bn])


def write(path, doc, blob_):
    while len(blob_) % 4:
        blob_.append(0)
    doc["buffers"][0]["byteLength"] = len(blob_)
    text = json.dumps(doc, separators=(",", ":")).encode()
    text += b" " * (-len(text) % 4)
    out = struct.pack("<4sII", b"glTF", 2, 28 + len(text) + len(blob_)) + struct.pack("<I4s", len(text), b"JSON") + text \
        + struct.pack("<I4s", len(blob_), b"BIN\0") + bytes(blob_)
    path.write_bytes(out)


def accessor_array(doc, blob_, index):
    a = doc["accessors"][index]
    view = doc["bufferViews"][a["bufferView"]]
    kind = {5126: "f4", 5123: "u2", 5125: "u4", 5121: "u1"}[a["componentType"]]
    width = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
    start = view.get("byteOffset", 0) + a.get("byteOffset", 0)
    return np.frombuffer(bytes(blob_), kind, a["count"] * width, start).reshape(a["count"], width)


def add(doc, blob_, array, kind, target=None, bounds=False):
    while len(blob_) % 4:
        blob_.append(0)
    view = {"buffer": 0, "byteOffset": len(blob_), "byteLength": array.nbytes}
    if target:
        view["target"] = target
    blob_ += array.tobytes()
    doc["bufferViews"].append(view)
    a = {"bufferView": len(doc["bufferViews"]) - 1, "componentType": 5126 if array.dtype == np.float32 else 5125,
         "count": int(array.shape[0]) if array.ndim > 1 else int(array.size), "type": kind}
    if bounds:
        a["min"] = [float(v) for v in array.min(axis=0)]
        a["max"] = [float(v) for v in array.max(axis=0)]
    doc["accessors"].append(a)
    return len(doc["accessors"]) - 1


def main():
    if "--restore" in sys.argv:
        if not KEPT.exists():
            raise SystemExit("no kept original at " + str(KEPT))
        shutil.copyfile(KEPT, GLB)
        print("restored", GLB)
        return
    if not KEPT.exists():
        KEPT.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(GLB, KEPT)
        print("kept the original at", KEPT)
    doc, blob_ = read(KEPT)
    if doc.get("extras", {}).get("kuboKuro"):
        raise SystemExit("the kept original is already remodelled: refusing to build on it")
    nodes = {n.get("name"): n for n in doc["nodes"]}
    cells = {}
    for name, node in nodes.items():
        if name and name.startswith("ghost-") and "mesh" in node:
            prim = doc["meshes"][node["mesh"]]["primitives"][0]
            if "TEXCOORD_0" in prim["attributes"]:
                cells[name] = accessor_array(doc, blob_, prim["attributes"]["TEXCOORD_0"])[0].copy()
    parts = kubo()
    extras = {name[1:]: value for name, value in parts.items() if name.startswith("+")}
    parts = {name: value for name, value in parts.items() if not name.startswith("+")}
    # How much of the tail is left at each height (in the calm form's own space): all of it under the body, none at
    # the tip. `Shaders/KuroTail` reads it from the vertex alpha.
    def left_at(y):
        return float(np.clip((y + .126) / .078, 0, 1) ** .85)
    for name, (pos, nrm, tris) in parts.items():
        node = nodes.get(name)
        if node is None or "mesh" not in node:
            raise SystemExit("the model has no part named " + name)
        prim = doc["meshes"][node["mesh"]]["primitives"][0]
        attributes = {"POSITION": add(doc, blob_, pos, "VEC3", 34962, True), "NORMAL": add(doc, blob_, nrm, "VEC3", 34962)}
        if "TEXCOORD_0" in prim["attributes"]:
            cell = cells[CELL_FROM.get(name, name)]
            uv = np.tile(cell.astype(np.float32), (len(pos), 1))
            if name in extras:
                # More pieces in the same part, each in another part's palette cell.
                (epos, enrm, etris), cell_from = extras[name]
                tris = np.concatenate([tris, etris + len(pos)])
                pos = np.concatenate([pos, epos]); nrm = np.concatenate([nrm, enrm])
                uv = np.concatenate([uv, np.tile(cells[cell_from].astype(np.float32), (len(epos), 1))])
                attributes = {"POSITION": add(doc, blob_, pos, "VEC3", 34962, True), "NORMAL": add(doc, blob_, nrm, "VEC3", 34962)}
            attributes["TEXCOORD_0"] = add(doc, blob_, uv, "VEC2", 34962)
        if name.startswith("ghost-tail"):
            base = node.get("translation", [0, 0, 0])[1]
            colour = np.ones((len(pos), 4), np.float32)
            colour[:, 3] = [left_at(base + y) for y in pos[:, 1]]
            attributes["COLOR_0"] = add(doc, blob_, colour, "VEC4", 34962)
        prim["attributes"] = attributes
        prim["indices"] = add(doc, blob_, tris.astype(np.uint32).reshape(-1), "SCALAR", 34963)
        prim.pop("targets", None)
        print("  %-22s %4d vertices %4d triangles" % (name, len(pos), len(tris)))
    doc.setdefault("extras", {})["kuboKuro"] = "2026-10-06 rounded, ear tufts, paws, big eyes"
    write(GLB, doc, blob_)
    print("wrote", GLB, GLB.stat().st_size, "bytes")


main()
