"""PAYO's stamina orb (owner, 2026-10-19: "stamina orb looks too plain and flat"). A carved spirit bead floating over
the pickup plinth: a plump eight-sided amethyst crystal capped in gold at both ends, a gold band round its middle set
with four small violet gems, and a spiral of pale light wound round it (the Ifugao spiral; it seems to flow while the
cell turns). Around it the halo: a woven rattan-gold ring with ten jade stone leaves fanning out and ten violet gems
between their bases.

  py -I tools/payo_pieces/orb_build.py build     Logs/payo/pieces/orb/pickup_orb.glb (+ build/ atlas, height, normal)
  py -I tools/payo_pieces/orb_build.py sheet vN  Logs/payo/pieces/orb/orb_vN.png from the review shots
  py -I tools/payo_pieces/orb_build.py install   copies pickup_orb.glb to Assets/TumbangPreso/Art/Payo/Models/
  (bash tools/payo_pieces/orb_run.sh build | review vN | install)

THE CONTRACT (Runtime/Map/ArenaStaminaPickup.cs): two root nodes, `pickup_cell` and `pickup_halo`, each with its pivot
at its own centre. The component puts both at (0, 0.95, 0) under the pickup, bobs the cell 0.1 m and turns it about y;
the halo stands still. The carved plinth (pickup_plinth.glb, 0.40 m high) is the base, so there is no pickup_base.
Metres, y up. The cell is 0.40 m across its band and 0.62 m tall with its finials; the halo 0.74 m across.

MATERIALS (one painted 1024 atlas inside the glb, as every Payo piece):
  pickup_orb_glow  the crystal and every gem. Its picture is also its emission (kind glow in materials_payo.json, with
                   `day_glow`, so it shines by day too, not only at night).
  pickup_orb       the gold, the woven ring and the leaves: painted, with a normal map baked from the painted heights.
Colours: violet and amethyst, pale gold, jade. Nothing near the team hues (orange #f87020, blue #0080e8).
"""
import math
import shutil
import struct
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = ROOT / "Logs/payo/pieces/orb"
BUILD = OUT / "build"
GAME = ROOT / "Assets/TumbangPreso/Art/Payo/Models/pickup_orb.glb"
sys.path.insert(0, str(HERE))
import calm_glb                                          # noqa: E402

S = 1024
# atlas regions, pixels (x, y, w, h); y down the image
R_CRYSTAL = (0, 0, 512, 1024)
R_BAND = (512, 0, 512, 256)
R_RING = (512, 256, 512, 128)
R_GEM = (512, 384, 128, 128)
R_CAP = (640, 384, 256, 128)
R_LEAF = (512, 512, 512, 512)

# palette (sRGB)
VIO_D, VIO, VIO_L, VIO_P = (64, 26, 140), (120, 56, 226), (170, 112, 255), (232, 210, 255)
GOLD_D, GOLD, GOLD_L = (160, 112, 52), (222, 180, 96), (250, 226, 156)
WEAVE_A, WEAVE_B = (234, 204, 138), (178, 134, 70)
JADE_D, JADE, JADE_L, RIB = (84, 150, 118), (124, 194, 152), (178, 228, 186), (236, 242, 208)


# ------------------------------------------------------------------ meshes: dicts of pos, nrm, uv (0..1 in its region), tris
def mesh(pos, nrm, uv, tris, region):
    return {"pos": np.asarray(pos, np.float32), "nrm": np.asarray(nrm, np.float32), "uv": np.asarray(uv, np.float32),
            "tris": np.asarray(tris, np.uint32), "region": region}


def face_normals(pos, tris):
    n = np.zeros_like(pos)
    a, b, c = pos[tris[:, 0]], pos[tris[:, 1]], pos[tris[:, 2]]
    f = np.cross(b - a, c - a)
    for k in range(3):
        np.add.at(n, tris[:, k], f)
    return n / np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-9)


def lathe(profile, seg, region, phase=0.0):
    """Turned about y; profile (radius, y) bottom to top, ends on the axis. u round, v along the profile."""
    prof = np.array(profile, np.float64)
    d = np.zeros_like(prof)
    d[1:-1] = prof[2:] - prof[:-2]
    d[0] = prof[1] - prof[0]
    d[-1] = prof[-1] - prof[-2]
    nr = np.stack([d[:, 1], -d[:, 0]], 1)
    nr /= np.maximum(np.linalg.norm(nr, axis=1, keepdims=True), 1e-9)
    s = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(prof, axis=0), axis=1))])
    s /= s[-1]
    pos, nrm, uv, tris = [], [], [], []
    for i, ((r, y), (n_r, n_y)) in enumerate(zip(prof, nr)):
        for k in range(seg + 1):
            a = 2 * math.pi * (k / seg) + phase
            pos.append((r * math.cos(a), y, r * math.sin(a)))
            nrm.append((n_r * math.cos(a), n_y, n_r * math.sin(a)))
            uv.append((k / seg, s[i]))
    for i in range(len(prof) - 1):
        for k in range(seg):
            a = i * (seg + 1) + k
            b = a + seg + 1
            tris += [(a, b, a + 1), (a + 1, b, b + 1)]
    return mesh(pos, nrm, uv, tris, region)


def torus(R, rx, ry, seg, sides, region, y=0.0):
    pos, nrm, uv, tris = [], [], [], []
    for i in range(seg + 1):
        a = 2 * math.pi * i / seg
        for j in range(sides + 1):
            b = 2 * math.pi * j / sides
            rr = R + rx * math.cos(b)
            pos.append((rr * math.cos(a), y + ry * math.sin(b), rr * math.sin(a)))
            n_r, n_y = math.cos(b) / rx, math.sin(b) / ry
            L = math.hypot(n_r, n_y)
            nrm.append((n_r / L * math.cos(a), n_y / L, n_r / L * math.sin(a)))
            uv.append((i / seg, j / sides))
    for i in range(seg):
        for j in range(sides):
            a = i * (sides + 1) + j
            b = a + sides + 1
            tris += [(a, a + 1, b), (a + 1, b + 1, b)]
    return mesh(pos, nrm, uv, tris, region)


def ellipsoid(half, region, seg=12, rings=8):
    pos, nrm, uv, tris = [], [], [], []
    hx, hy, hz = half
    for r in range(rings + 1):
        v = math.pi * r / rings
        for k in range(seg + 1):
            u = 2 * math.pi * k / seg
            n = (math.sin(v) * math.cos(u), math.cos(v), math.sin(v) * math.sin(u))
            pos.append((n[0] * hx, n[1] * hy, n[2] * hz))
            m = np.array((n[0] / hx, n[1] / hy, n[2] / hz))
            nrm.append(tuple(m / max(np.linalg.norm(m), 1e-9)))
            uv.append((k / seg, 1 - r / rings))
    for r in range(rings):
        for k in range(seg):
            a = r * (seg + 1) + k
            b = a + seg + 1
            tris += [(a, a + 1, b), (a + 1, b + 1, b)]
    return mesh(pos, nrm, uv, tris, region)


def leaf(L, W, T, region, nu=14, nv=12):
    """A fat carved leaf along +x from its base at x 0, flat in xz, its back curling up a little toward the tip."""
    pos, uv = [], []
    for i in range(nu + 1):
        t = i / nu
        w = W * (math.sin(math.pi * t ** 0.85)) ** 0.75 + 0.004
        th = T * (math.sin(math.pi * t ** 0.85)) ** 0.6 + 0.003
        yc = 0.16 * L * t * t
        for j in range(nv):
            c = 2 * math.pi * j / nv
            pos.append((L * t, yc + th * math.sin(c) + 0.25 * th * abs(math.cos(c)), w * math.cos(c)))
            uv.append((t, 0.5 + 0.5 * math.cos(c)))
    tris = []
    for i in range(nu):
        for j in range(nv):
            a, b = i * nv + j, i * nv + (j + 1) % nv
            c, d = a + nv, b + nv
            tris += [(a, c, b), (b, c, d)]
    pos = np.array(pos, np.float32)
    tris = np.array(tris, np.uint32)
    return mesh(pos, face_normals(pos, tris), uv, tris, region)


def moved(m, at=(0, 0, 0), yaw=0.0, pitch=0.0, roll=0.0):
    """Turned about x (roll), then z (pitch), then y (yaw), degrees; then moved."""
    def R(axis, deg):
        c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))
        if axis == "x":
            return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])
        if axis == "y":
            return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])
        return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])
    M = R("y", yaw) @ R("z", pitch) @ R("x", roll)
    out = dict(m)
    out["pos"] = (m["pos"] @ M.T + np.array(at)).astype(np.float32)
    out["nrm"] = (m["nrm"] @ M.T).astype(np.float32)
    return out


# ------------------------------------------------------------------ the shapes
CRYSTAL = [(0.0, -0.25), (0.06, -0.215), (0.125, -0.14), (0.165, -0.05), (0.172, 0.0), (0.165, 0.05), (0.13, 0.13),
           (0.075, 0.20), (0.0, 0.25)]


def crystal_r(y):
    ys = [p[1] for p in CRYSTAL]
    return float(np.interp(y, ys, [p[0] for p in CRYSTAL]))


def cap(sign):
    """A small gold cap over one tip of the crystal, a little proud of it, with a lip and a finial knob: the crystal shows."""
    prof = [(crystal_r(0.198) + 0.012, 0.196), (crystal_r(0.204) + 0.017, 0.204), (crystal_r(0.218) + 0.010, 0.218),
            (crystal_r(0.234) + 0.007, 0.234), (0.015, 0.250), (0.021, 0.262), (0.019, 0.274), (0.0, 0.282)]
    if sign < 0:
        prof = [(r, -y) for r, y in prof][::-1]
        prof = prof[:]
        prof.insert(0, (0.0, prof[0][1] - 0.006))
        prof.append((0.0, prof[-1][1]))
        m = lathe(prof[1:], 16, R_CAP)
    else:
        prof.insert(0, (0.0, 0.196))
        m = lathe(prof, 16, R_CAP)
    return m


def cell_parts():
    glow = [lathe(CRYSTAL, 8, R_CRYSTAL)]
    for k in range(4):
        a = 45 + 90 * k
        g = ellipsoid((0.012, 0.026, 0.022), R_GEM)
        glow.append(moved(g, at=(0.19 * math.cos(math.radians(a)), 0.0, 0.19 * math.sin(math.radians(a))), yaw=-a))
    gold = [torus(0.168, 0.022, 0.036, 48, 10, R_BAND), cap(1), cap(-1)]
    return glow, gold


def halo_parts():
    glow, painted = [], []
    painted.append(torus(0.245, 0.02, 0.02, 64, 8, R_RING))
    n = 8
    for k in range(n):
        a = 360.0 * k / n
        lf = leaf(0.15, 0.048, 0.016, R_LEAF)
        lf = moved(lf, at=(0, 0, 0), pitch=22)                 # tipped up out of the ring's plane
        ca, sa = math.cos(math.radians(a)), math.sin(math.radians(a))
        painted.append(moved(lf, at=(0.22 * ca, 0.0, 0.22 * sa), yaw=-a))
        b = a + 180.0 / n
        cb, sb = math.cos(math.radians(b)), math.sin(math.radians(b))
        g = ellipsoid((0.026, 0.026, 0.026), R_GEM, seg=10, rings=7)
        glow.append(moved(g, at=(0.262 * cb, 0.018, 0.262 * sb)))
    return glow, painted


# ------------------------------------------------------------------ the paint
def grid(region):
    x, y, w, h = region
    u = (np.arange(w) + 0.5) / w
    v = 1.0 - (np.arange(h) + 0.5) / h                  # v up
    return np.meshgrid(u, v)


def mixc(a, b, t):
    t = np.clip(t, 0, 1)[..., None]
    return np.array(a, np.float32) * (1 - t) + np.array(b, np.float32) * t


def ramp(stops, t):
    t = np.clip(t, 0, 1)
    out = np.zeros(t.shape + (3,), np.float32)
    for (t0, c0), (t1, c1) in zip(stops, stops[1:]):
        sel = (t >= t0) & (t <= t1)
        out[sel] = mixc(c0, c1, (t[sel] - t0) / max(t1 - t0, 1e-6))
    return out


def soft(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def paint():
    rgb = np.ones((S, S, 3), np.float32) * 128
    H = np.zeros((S, S), np.float32)

    def put(region, col, h=None):
        x, y, w, hh = region
        rgb[y:y + hh, x:x + w] = col
        if h is not None:
            H[y:y + hh, x:x + w] = h

    # the crystal: deep violet below to pale lilac above, each of the eight facets lit across, a spiral of light round it
    u, v = grid(R_CRYSTAL)
    col = ramp([(0.0, VIO_D), (0.4, VIO), (0.85, VIO_L), (1.0, VIO_P)], v * 0.9 + 0.05)
    f = (u * 8) % 1.0
    col *= (0.86 + 0.20 * np.sin(math.pi * f) ** 0.8)[..., None]
    spiral = np.abs(((u * 2 + v * 1.4) % 1.0) - 0.5)
    band = soft(0.15, 0.07, spiral)
    col = mixc(col, VIO_P, band * 0.85 * soft(0.0, 0.18, v) * soft(1.0, 0.82, v))
    col = mixc(col, (255, 248, 255), soft(0.05, 0.0, np.abs(((u * 2 + v * 1.4) % 1.0) - 0.5)) * 0.6 * soft(0.1, 0.3, v) * soft(0.95, 0.75, v))
    core = np.exp(-((v - 0.5) / 0.22) ** 2)
    col = col * (1 + 0.10 * core[..., None])
    put(R_CRYSTAL, col)

    # gems: pale at the top, deep below
    u, v = grid(R_GEM)
    col = ramp([(0.0, VIO_D), (0.45, VIO), (0.8, VIO_L), (1.0, VIO_P)], v)
    put(R_GEM, col)

    # the gold band: lit on top, darker under, a carved groove every 15 degrees
    u, v = grid(R_BAND)
    up = np.sin(2 * math.pi * v)                        # +1 on top of the tube
    col = ramp([(0.0, GOLD_D), (0.5, GOLD), (1.0, GOLD_L)], 0.5 + 0.5 * up)
    g = np.abs(((u * 24) % 1.0) - 0.5)
    groove = soft(0.08, 0.02, 0.5 - g)
    col = col * (1 - 0.22 * groove[..., None])
    put(R_BAND, col, -0.004 * groove)

    # the caps: gold, a groove at the lip
    u, v = grid(R_CAP)
    col = ramp([(0.0, GOLD_D), (0.5, GOLD), (1.0, GOLD_L)], 0.3 + 0.6 * v)
    lip = soft(0.03, 0.0, np.abs(v - 0.16))
    col = col * (1 - 0.25 * lip[..., None])
    put(R_CAP, col, -0.003 * lip)

    # the woven ring: broad diagonal strands, two tones
    u, v = grid(R_RING)
    w = ((u * 40 + v * 1.0) % 1.0)
    strand = soft(0.0, 0.18, w) * soft(1.0, 0.82, w)
    up = 0.5 + 0.5 * np.sin(2 * math.pi * v)
    col = mixc(WEAVE_B, WEAVE_A, 0.25 + 0.55 * strand) * (0.82 + 0.25 * up)[..., None]
    put(R_RING, col, 0.003 * strand)

    # the leaf: jade, paler to the tip, a raised cream midrib and three soft veins a side
    u, v = grid(R_LEAF)
    across = np.abs(v - 0.5) * 2                        # 0 on the midrib, 1 at the edge
    col = ramp([(0.0, JADE_D), (0.4, JADE), (1.0, JADE_L)], 0.25 + 0.6 * u)
    col = col * (1 - 0.18 * soft(0.6, 1.0, across))[..., None]
    rib = soft(0.07, 0.02, across) * soft(0.0, 0.1, u) * soft(1.0, 0.85, u)
    col = mixc(col, RIB, rib * 0.85)
    vein = np.abs(((u * 4 - across * 1.2) % 1.0) - 0.5)
    vg = soft(0.06, 0.0, vein) * soft(0.1, 0.25, across) * soft(0.85, 0.6, across)
    col = col * (1 - 0.12 * vg)[..., None]
    put(R_LEAF, col, 0.004 * rib - 0.002 * vg)
    return np.clip(rgb, 0, 255).astype(np.uint8), H


# ------------------------------------------------------------------ the glb
def to_atlas(m):
    x, y, w, h = m["region"]
    uv = np.empty_like(m["uv"])
    uv[:, 0] = (x + 0.5 + np.clip(m["uv"][:, 0], 0, 1) * (w - 1)) / S
    uv[:, 1] = (y + 0.5 + (1 - np.clip(m["uv"][:, 1], 0, 1)) * (h - 1)) / S
    return uv


def merged(parts):
    pos, nrm, uv, tris, base = [], [], [], [], 0
    for m in parts:
        pos.append(m["pos"]); nrm.append(m["nrm"]); uv.append(to_atlas(m)); tris.append(m["tris"] + base)
        base += len(m["pos"])
    return (np.concatenate(pos).astype(np.float32), np.concatenate(nrm).astype(np.float32),
            np.concatenate(uv).astype(np.float32), np.concatenate(tris).astype(np.uint32))


def write_glb(path, nodes, png_bytes):
    """nodes: [(name, [(material index, (pos, nrm, uv, tris)), ...])]."""
    doc = {"asset": {"version": "2.0", "generator": "tools/payo_pieces/orb_build.py"}, "scene": 0,
           "scenes": [{"nodes": list(range(len(nodes)))}], "nodes": [], "meshes": [], "accessors": [], "bufferViews": [],
           "buffers": [{"byteLength": 0}], "samplers": [{"magFilter": 9729, "minFilter": 9987}],
           "images": [], "textures": [{"sampler": 0, "source": 0}],
           "materials": [
               {"name": "pickup_orb_glow", "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicFactor": 0.0, "roughnessFactor": 0.5},
                "emissiveTexture": {"index": 0}, "emissiveFactor": [1.0, 1.0, 1.0]},
               {"name": "pickup_orb", "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicFactor": 0.0, "roughnessFactor": 0.8}}]}
    blob = bytearray()

    def view(data, target=None):
        while len(blob) % 4:
            blob.append(0)
        v = {"buffer": 0, "byteOffset": len(blob), "byteLength": len(data)}
        if target:
            v["target"] = target
        doc["bufferViews"].append(v)
        blob.extend(data)
        return len(doc["bufferViews"]) - 1

    def acc(array, kind, target, bounds=False):
        a = {"bufferView": view(array.tobytes(), target), "componentType": 5126 if array.dtype == np.float32 else 5125,
             "count": int(array.shape[0]) if array.ndim > 1 else int(array.size), "type": kind}
        if bounds:
            a["min"] = [float(t) for t in array.min(axis=0)]; a["max"] = [float(t) for t in array.max(axis=0)]
        doc["accessors"].append(a)
        return len(doc["accessors"]) - 1

    for name, prims in nodes:
        ps = []
        for mat, (pos, nrm, uv, tris) in prims:
            ps.append({"attributes": {"POSITION": acc(pos, "VEC3", 34962, True), "NORMAL": acc(nrm, "VEC3", 34962),
                                      "TEXCOORD_0": acc(uv, "VEC2", 34962)},
                       "indices": acc(tris.reshape(-1), "SCALAR", 34963), "material": mat})
        doc["meshes"].append({"name": name, "primitives": ps})
        doc["nodes"].append({"name": name, "mesh": len(doc["meshes"]) - 1})
    doc["images"].append({"bufferView": view(png_bytes), "mimeType": "image/png", "name": "pickup_orb-atlas"})
    while len(blob) % 4:
        blob.append(0)
    doc["buffers"][0]["byteLength"] = len(blob)
    text = json.dumps(doc, separators=(",", ":")).encode()
    text += b" " * (-len(text) % 4)
    Path(path).write_bytes(struct.pack("<4sII", b"glTF", 2, 28 + len(text) + len(blob)) + struct.pack("<I4s", len(text), b"JSON") + text
                           + struct.pack("<I4s", len(blob), b"BIN\0") + bytes(blob))


def build():
    BUILD.mkdir(parents=True, exist_ok=True)
    rgb, H = paint()
    atlas = BUILD / "pickup_orb-atlas.png"
    Image.fromarray(rgb).save(atlas)
    calm_glb.save_height(H, BUILD / "pickup_orb_height.png", lo=-0.01, hi=0.01)
    rects = [(r[0], r[1], r[0] + r[2], r[1] + r[3], ppm) for r, ppm in
             ((R_BAND, 480), (R_CAP, 400), (R_RING, 330), (R_LEAF, 2400))]
    Image.fromarray(calm_glb.height_to_normal(H, rects, strength=3.0, blur=1.2)).save(BUILD / "pickup_orb_normal.png")
    cg, cp = cell_parts()
    hg, hp = halo_parts()
    nodes = [("pickup_cell", [(0, merged(cg)), (1, merged(cp))]), ("pickup_halo", [(0, merged(hg)), (1, merged(hp))])]
    raw = BUILD / "pickup_orb_raw.glb"
    write_glb(raw, nodes, atlas.read_bytes())
    calm_glb.swap(str(raw), str(atlas), str(OUT / "pickup_orb.glb"), str(BUILD / "pickup_orb_normal.png"))
    tri = sum(len(p[1][3]) for n in nodes for p in n[1])
    for name, prims in nodes:
        pos = np.concatenate([p[1][0] for p in prims])
        print("%s: %d triangles, size %s m" % (name, sum(len(p[1][3]) for p in prims), np.round(pos.max(0) - pos.min(0), 3)))
    print("pickup_orb.glb: %d triangles" % tri)


def sheet(version):
    shots = OUT / "shots"
    names = sorted(p.stem for p in shots.glob("orb_*.png"))
    if not names:
        print("no shots"); return
    ims = [Image.open(shots / (n + ".png")).convert("RGB") for n in names]
    w, h = ims[0].size
    cols = 3
    rows = (len(ims) + cols - 1) // cols
    sh = Image.new("RGB", (w * cols, 40 + rows * (h + 22)), (20, 22, 28))
    d = ImageDraw.Draw(sh)
    try:
        font = ImageFont.truetype("arial.ttf", 16)
    except OSError:
        font = ImageFont.load_default()
    d.text((8, 10), "PAYO STAMINA ORB %s  (carved spirit bead over the pickup plinth; warm sunset key, ink hull, 1.75 m figure)" % version, fill=(240, 220, 160), font=font)
    for i, (n, im) in enumerate(zip(names, ims)):
        x, y = (i % cols) * w, 40 + (i // cols) * (h + 22)
        d.text((x + 6, y + 2), n.replace("orb_", ""), fill=(230, 230, 230), font=font)
        sh.paste(im, (x, y + 20))
    sh.save(OUT / ("orb_%s.png" % version))
    print("SHEET", OUT / ("orb_%s.png" % version))


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "build"
    if what == "build":
        build()
    elif what == "sheet":
        sheet(sys.argv[2] if len(sys.argv) > 2 else "v1")
    elif what == "install":
        shutil.copyfile(OUT / "pickup_orb.glb", GAME)
        print("installed", GAME)
