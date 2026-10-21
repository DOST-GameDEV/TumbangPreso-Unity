"""The CALM texture pass (Logs/payo/pieces/CALM_STYLE.md): shared tools for every family's calm mode.

  python tools/payo_pieces/calm_glb.py <approved.glb> <atlas.png> <out.glb> [<normal.png>]

1. swap(): the calm painters re-paint the SAME UV layout, so the model does not change. This copies the approved glb
   and swaps only the embedded atlas (same image slot, same materials; the glow material keeps its emission). With a
   normal map it also adds that image as the MAIN material's normalTexture (OpenGL convention, green up, which is
   what glTF and Unity expect) and writes a TANGENT for every vertex (per-triangle UV tangents summed per vertex,
   Gram-Schmidt to the normal, handedness in w), because the game's painted shaders otherwise invent a tangent that
   is not tied to the UV direction (LagoonPainted.shader, "MISSING TANGENTS"). It then checks that every original
   buffer view except the atlas is byte-identical and no glTF entry changed other than the additions.
2. height_to_normal(): a height field in METRES (one float per atlas pixel) to a tangent-space normal map, the
   gradient taken inside each UV region only, so unrelated neighbours in the atlas never bleed into each other.
"""
import json
import struct
import sys

import numpy as np
from PIL import Image, ImageFilter


# ------------------------------------------------------------------ height to normals
def gblur(a, sigma):
    """A separable gaussian blur of a float array, edges held."""
    if sigma <= 0:
        return a
    r = max(1, int(sigma * 3))
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2)
    k /= k.sum()
    p = np.pad(a, ((r, r), (0, 0)), mode="edge")
    a = sum(k[i] * p[i:i + a.shape[0]] for i in range(2 * r + 1))
    p = np.pad(a, ((0, 0), (r, r)), mode="edge")
    return sum(k[i] * p[:, i:i + a.shape[1]] for i in range(2 * r + 1)).astype(np.float32)


def height_to_normal(H, rects, strength=1.0, blur=1.5):
    """H: float32 (h, w) metres. rects: [(x0, y0, x1, y1, ppm)] pixel boxes of the UV regions with their pixels per
    metre. Returns an RGB uint8 normal map: flat (128, 128, 255) outside every region."""
    h, w = H.shape
    N = np.zeros((h, w, 3), np.float32)
    N[..., 2] = 1.0
    for (x0, y0, x1, y1, ppm) in rects:
        x0, y0, x1, y1 = max(0, int(x0)), max(0, int(y0)), min(w, int(x1)), min(h, int(y1))
        if x1 - x0 < 3 or y1 - y0 < 3:
            continue
        sub = H[y0:y1, x0:x1]
        if blur:
            sub = gblur(sub.astype(np.float32), blur)
        gy, gx = np.gradient(sub)                         # metres per pixel, rows go DOWN the image
        k = ppm * strength
        n = np.stack([-gx * k, gy * k, np.ones_like(sub)], -1)       # green up: the image's up is -row
        n /= np.linalg.norm(n, axis=-1, keepdims=True)
        N[y0:y1, x0:x1] = n
    return (np.clip(N * 0.5 + 0.5, 0, 1) * 255 + 0.5).astype(np.uint8)


def save_height(H, path, lo=-0.06, hi=0.06):
    """The height map as a 16 bit grey PNG: 0 = lo metres, 65535 = hi metres (the normal map is baked from it)."""
    v = np.clip((H - lo) / (hi - lo), 0, 1)
    Image.fromarray((v * 65535 + 0.5).astype(np.uint16)).save(path)


# ------------------------------------------------------------------ glb
CT = {5126: ("f", 4), 5125: ("I", 4), 5123: ("H", 2), 5121: ("B", 1)}
NC = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}


def read(path):
    b = open(path, "rb").read()
    magic, ver, total = struct.unpack("<4sII", b[:12])
    assert magic == b"glTF" and ver == 2, path
    jl, jt = struct.unpack("<II", b[12:20])
    j = json.loads(b[20:20 + jl])
    bl, bt = struct.unpack("<II", b[20 + jl:28 + jl])
    return j, b[28 + jl:28 + jl + bl]


def pad4(b, fill=b"\0"):
    return b + fill * ((4 - len(b) % 4) % 4)


def accessor(j, bin_, i):
    a = j["accessors"][i]
    v = j["bufferViews"][a["bufferView"]]
    fmt, size = CT[a["componentType"]]
    n = NC[a["type"]]
    off = v.get("byteOffset", 0) + a.get("byteOffset", 0)
    stride = v.get("byteStride", size * n)
    dt = np.dtype("<" + fmt)
    if stride == size * n:
        arr = np.frombuffer(bin_, dt, a["count"] * n, off).reshape(a["count"], n)
    else:
        raw = np.frombuffer(bin_, np.uint8, stride * a["count"], off).reshape(a["count"], stride)
        arr = raw[:, :size * n].copy().view(dt).reshape(a["count"], n)
    return arr.astype(np.float64 if fmt == "f" else np.int64)


def tangents(P, Nn, UV, I):
    """Per-vertex tangents (xyz, w handedness) from positions, normals, uvs and triangle indices."""
    T = np.zeros_like(P)
    B = np.zeros_like(P)
    tri = I.reshape(-1, 3)
    p0, p1, p2 = P[tri[:, 0]], P[tri[:, 1]], P[tri[:, 2]]
    # glTF v runs DOWN the image; the normal map's green is the image's UP, so the bitangent is -dv
    w0, w1, w2 = UV[tri[:, 0]] * [1, -1], UV[tri[:, 1]] * [1, -1], UV[tri[:, 2]] * [1, -1]
    e1, e2 = p1 - p0, p2 - p0
    d1, d2 = w1 - w0, w2 - w0
    r = d1[:, 0] * d2[:, 1] - d2[:, 0] * d1[:, 1]
    r = np.where(np.abs(r) < 1e-12, 1e-12, r)
    t = (e1 * d2[:, 1:2] - e2 * d1[:, 1:2]) / r[:, None]
    b = (e2 * d1[:, 0:1] - e1 * d2[:, 0:1]) / r[:, None]
    for k in range(3):
        np.add.at(T, tri[:, k], t)
        np.add.at(B, tri[:, k], b)
    T = T - Nn * np.sum(Nn * T, 1, keepdims=True)
    ln = np.linalg.norm(T, axis=1, keepdims=True)
    bad = ln[:, 0] < 1e-9
    alt = np.cross(Nn, np.where(np.abs(Nn[:, 1:2]) < 0.9, [[0, 1, 0]], [[1, 0, 0]]))
    T = np.where(bad[:, None], alt, T)
    T /= np.maximum(np.linalg.norm(T, axis=1, keepdims=True), 1e-9)
    wv = np.where(np.sum(np.cross(Nn, T) * B, 1) < 0, -1.0, 1.0)
    return np.concatenate([T, wv[:, None]], 1).astype(np.float32)


def swap(src, png, dst, normal=None):
    j, bin_ = read(src)
    assert len(j.get("images", [])) == 1, "expected one image in %s" % src
    iv = j["images"][0]["bufferView"]
    views = j["bufferViews"]
    chunks = [open(png, "rb").read() if i == iv else bin_[v.get("byteOffset", 0):v.get("byteOffset", 0) + v["byteLength"]] for i, v in enumerate(views)]
    if normal:
        for mesh in j["meshes"]:
            for prim in mesh["primitives"]:
                at = prim["attributes"]
                if "TANGENT" in at or "TEXCOORD_0" not in at:
                    continue
                Pp, Nn, UV = accessor(j, bin_, at["POSITION"]), accessor(j, bin_, at["NORMAL"]), accessor(j, bin_, at["TEXCOORD_0"])
                I = accessor(j, bin_, prim["indices"])[:, 0] if "indices" in prim else np.arange(len(Pp))
                T = tangents(Pp, Nn / np.maximum(np.linalg.norm(Nn, axis=1, keepdims=True), 1e-9), UV, I)
                views.append({"buffer": 0, "byteLength": T.nbytes, "target": 34962})
                chunks.append(T.tobytes())
                j["accessors"].append({"bufferView": len(views) - 1, "componentType": 5126, "count": len(T), "type": "VEC4"})
                at["TANGENT"] = len(j["accessors"]) - 1
        views.append({"buffer": 0, "byteLength": 0})
        chunks.append(open(normal, "rb").read())
        j["images"].append({"bufferView": len(views) - 1, "mimeType": "image/png", "name": j["images"][0].get("name", "atlas") + "-normal"})
        j["textures"].append({"sampler": j["textures"][0].get("sampler", 0), "source": len(j["images"]) - 1})
        for m in j["materials"]:
            if "emissiveTexture" in m:                    # the glow keeps its look exactly
                continue
            m["normalTexture"] = {"index": len(j["textures"]) - 1}
    out = b""
    for v, data in zip(views, chunks):
        out = pad4(out)
        v["byteOffset"] = len(out)
        v["byteLength"] = len(data)
        out += data
    out = pad4(out)
    j["buffers"][0]["byteLength"] = len(out)
    js = pad4(json.dumps(j, separators=(",", ":")).encode(), b" ")
    with open(dst, "wb") as f:
        f.write(struct.pack("<4sII", b"glTF", 2, 12 + 8 + len(js) + 8 + len(out)))
        f.write(struct.pack("<II", len(js), 0x4E4F534A)); f.write(js)
        f.write(struct.pack("<II", len(out), 0x004E4942)); f.write(out)
    # check: every original buffer view but the atlas is byte-identical, and nothing else changed but the additions
    j0, b0 = read(src)
    j1, b1 = read(dst)
    for i, v0 in enumerate(j0["bufferViews"]):
        if i == iv:
            continue
        v1 = j1["bufferViews"][i]
        assert b0[v0.get("byteOffset", 0):v0.get("byteOffset", 0) + v0["byteLength"]] == b1[v1.get("byteOffset", 0):v1.get("byteOffset", 0) + v1["byteLength"]], "view %d changed" % i
    assert j1["accessors"][:len(j0["accessors"])] == j0["accessors"]
    for k in ("nodes", "scenes", "samplers"):
        assert j0.get(k) == j1.get(k), k
    print("CALM GLB %s: geometry and UVs identical to the approved glb; atlas swapped%s" % (dst, ", normal map and tangents added" if normal else ""))


if __name__ == "__main__":
    swap(*sys.argv[1:5])
