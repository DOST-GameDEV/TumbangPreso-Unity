"""PAYO land kit, the CALM pass, step 2: the calm glbs (system Python, numpy).

  py -3 tools/payo_pieces/terrace_calm_glb.py [name ...]        (no names: every glb in build/terrace)

The terrace and land kit's glbs carry SEVERAL tiling pictures (wall, ground, plant, rock, fall...), so the shared
calm_glb.swap (one atlas) does not fit; this is its multi-picture sibling and reuses calm_glb's reader and tangents.
For each approved glb in Logs/payo/pieces/build/terrace (read only), it writes Logs/payo/pieces/calm/terrace/<name>.glb:

1. every embedded picture is swapped, by its image name, for the calm one from calm/terrace/tex (terrace_calm_paint.py);
2. every material whose picture has a calm normal map gets it as normalTexture, and every primitive wearing such a
   material gets a TANGENT (calm_glb.tangents: per-triangle UV tangents, OpenGL green up);
3. the vertex colours (COLOR_0), which paint these pieces as much as the pictures do (pale bevels, dark joints, neon
   moss), are calmed per material: a NEW accessor is appended and the primitive points at it, so the original colour
   data stays byte-identical in the file; positions, normals, uvs and indices are never touched.
Then it checks: every original buffer view other than the pictures is byte-identical, the original accessors are
unchanged, nodes, scenes and samplers are unchanged.
"""
import json
import struct
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import calm_glb as C   # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Logs/payo/pieces/build/terrace"
OUT = ROOT / "Logs/payo/pieces/calm/terrace"
TEX = OUT / "tex"
assert "build" not in OUT.parts


# ------------------------------------------------------------------ vertex colour
def to_srgb(x):
    x = np.clip(x, 0, 1)
    return np.where(x <= 0.0031308, x * 12.92, 1.055 * np.power(x, 1 / 2.4) - 0.055)


def to_lin(x):
    x = np.clip(x, 0, 1)
    return np.where(x <= 0.04045, x / 12.92, np.power((x + 0.055) / 1.055, 2.4))


ROCK_TEX = np.array([172, 165, 152]) / 255.0                       # the calm rock picture's base colour
CMOSS = np.array([126, 144, 92]) / 255.0
MOSS_V = to_srgb(to_lin(CMOSS) / to_lin(ROCK_TEX))                 # the vertex colour that turns the rock picture moss green


def calm_rock(c):
    """sRGB vertex colours of the rocks and dressed blocks: a grey value (bevels pale, foot dark) lerped toward a neon
    moss (0.60, 0.92, 0.36). Split them again, pull the grey values close together, and give the moss the muted
    shared moss at three quarters of its amount. Very dark faces (the inside of a niche, the cliff's back) stay dark."""
    r, g = c[:, 0], c[:, 1]
    bm = np.clip((g - r) / 0.32, 0, None)            # the moss part's weight (moss colour times its amount and shade)
    a = np.clip(r - 0.60 * bm, 0, None)              # the grey part's weight
    m = np.clip(bm / np.maximum(a + bm, 1e-4), 0, 1)
    L = np.clip(a + bm, 0, 1.1)                      # the face's brightness, moss or not
    Lc = np.where(L >= 0.5, 0.94 + (L - 0.9) * 0.4, L * (0.78 / 0.5))
    Lc = np.clip(Lc, 0, 1)
    mm = (m * 0.75)[:, None]
    grey = np.stack([Lc, Lc, Lc * 0.995], 1)
    return grey * (1 - mm) + (MOSS_V[None, :] * Lc[:, None]) * mm


def compress(k):
    return lambda c: 1 - (1 - np.clip(c, 0, 1)) * k


CALM_VC = {
    "py_terrace_stone": compress(0.15),        # the wall's joint backing 0.30 -> 0.90: joints are faint, not black slots
    "py_pit_stone": compress(0.15),
    "py_rock": calm_rock,
    "py_terrace_plant": compress(0.40),        # blade bases 0.62 -> 0.85: a blade is one colour, a paddy one band
    "py_terrace_wood": compress(0.70),
    "py_terrace_ground": compress(0.60),
    "py_ground_grass": compress(0.60),
    "py_ground_forest": compress(0.60),
}
def tint(*k):
    return lambda c: np.clip(c, 0, 1) * np.array(k)[None, :]


# By mesh name: the thin seep down each terrace wall and the spout's drop were pure white ticks, the loudest thing
# left on a calm hillside. They keep their shape and stay water, but sit only a little lighter and cooler than the stone.
CALM_MESH_VC = {"_seep": tint(0.60, 0.67, 0.70), "_spout_fall": tint(0.76, 0.84, 0.87), "_falls": tint(0.68, 0.75, 0.78),
                "_pipe_water": tint(0.86, 0.92, 0.94), "_pipe_spill": tint(0.86, 0.92, 0.94)}
NO_NORMAL = {"py_fall", "py_wet", "py_terrace_far"}                # water sheets, the stain decal, the alpha cards


def read_colour(j, bin_, i):
    a = j["accessors"][i]
    raw = C.accessor(j, bin_, i).astype(np.float64)
    if a["componentType"] == 5123:
        raw /= 65535.0
    elif a["componentType"] == 5121:
        raw /= 255.0
    return raw


# ------------------------------------------------------------------ the swap
def build(name):
    src = SRC / (name + ".glb")
    dst = OUT / (name + ".glb")
    j, bin_ = C.read(src)
    views = j["bufferViews"]
    chunks = [bin_[v.get("byteOffset", 0):v.get("byteOffset", 0) + v["byteLength"]] for v in views]
    img_views = set()
    report = []
    # 1. the pictures
    for im in j["images"]:
        p = TEX / (im["name"] + ".png")
        assert p.exists(), "no calm picture for %s" % im["name"]
        chunks[im["bufferView"]] = p.read_bytes()
        img_views.add(im["bufferView"])
    # 2. normal maps, one image per picture that has one
    tex_of_img = {}
    normal_tex = {}
    for ti, t in enumerate(j["textures"]):
        tex_of_img.setdefault(t["source"], ti)
    sampler = j["textures"][0].get("sampler", 0) if j.get("textures") else 0
    mat_normal = set()
    for mi, m in enumerate(j["materials"]):
        bt = m.get("pbrMetallicRoughness", {}).get("baseColorTexture")
        if bt is None or m["name"] in NO_NORMAL:
            continue
        img = j["textures"][bt["index"]]["source"]
        iname = j["images"][img]["name"]
        npng = TEX / (iname + "_normal.png")
        if not npng.exists():
            continue
        if iname not in normal_tex:
            views.append({"buffer": 0, "byteLength": 0}); chunks.append(npng.read_bytes())
            j["images"].append({"bufferView": len(views) - 1, "mimeType": "image/png", "name": iname + "_normal"})
            j["textures"].append({"sampler": j["textures"][bt["index"]].get("sampler", sampler), "source": len(j["images"]) - 1})
            normal_tex[iname] = len(j["textures"]) - 1
        m["normalTexture"] = {"index": normal_tex[iname]}
        mat_normal.add(mi)
    # 3. tangents and calm vertex colours, per primitive
    ntan = ncol = 0
    for mesh in j["meshes"]:
        for prim in mesh["primitives"]:
            at = prim["attributes"]
            mi = prim.get("material")
            mname = j["materials"][mi]["name"] if mi is not None else ""
            if mi in mat_normal and "TEXCOORD_0" in at and "TANGENT" not in at:
                P, N, UV = C.accessor(j, bin_, at["POSITION"]), C.accessor(j, bin_, at["NORMAL"]), C.accessor(j, bin_, at["TEXCOORD_0"])
                I = C.accessor(j, bin_, prim["indices"])[:, 0] if "indices" in prim else np.arange(len(P))
                T = C.tangents(P, N / np.maximum(np.linalg.norm(N, axis=1, keepdims=True), 1e-9), UV, I)
                views.append({"buffer": 0, "byteLength": T.nbytes, "target": 34962}); chunks.append(T.tobytes())
                j["accessors"].append({"bufferView": len(views) - 1, "componentType": 5126, "count": len(T), "type": "VEC4"})
                at["TANGENT"] = len(j["accessors"]) - 1
                ntan += 1
            fn = CALM_VC.get(mname)
            for suffix, f2 in CALM_MESH_VC.items():
                if mesh.get("name", "").endswith(suffix):
                    fn = f2
            if fn is not None and "COLOR_0" in at:
                a0 = j["accessors"][at["COLOR_0"]]
                col = read_colour(j, bin_, at["COLOR_0"])
                rgb = to_lin(fn(to_srgb(col[:, :3])))
                out = col.copy()
                out[:, :3] = rgb
                q = np.clip(np.round(out * 65535), 0, 65535).astype("<u2")
                views.append({"buffer": 0, "byteLength": q.nbytes, "target": 34962}); chunks.append(q.tobytes())
                j["accessors"].append({"bufferView": len(views) - 1, "componentType": 5123, "normalized": True, "count": len(q), "type": a0["type"]})
                at["COLOR_0"] = len(j["accessors"]) - 1
                ncol += 1
    # write
    out = b""
    for v, data in zip(views, chunks):
        out = C.pad4(out)
        v["byteOffset"] = len(out)
        v["byteLength"] = len(data)
        out += data
    out = C.pad4(out)
    j["buffers"][0]["byteLength"] = len(out)
    js = C.pad4(json.dumps(j, separators=(",", ":")).encode(), b" ")
    with open(dst, "wb") as fh:
        fh.write(struct.pack("<4sII", b"glTF", 2, 12 + 8 + len(js) + 8 + len(out)))
        fh.write(struct.pack("<II", len(js), 0x4E4F534A)); fh.write(js)
        fh.write(struct.pack("<II", len(out), 0x004E4942)); fh.write(out)
    # check
    j0, b0 = C.read(src)
    j1, b1 = C.read(dst)
    for i, v0 in enumerate(j0["bufferViews"]):
        if i in img_views:
            continue
        v1 = j1["bufferViews"][i]
        assert b0[v0.get("byteOffset", 0):v0.get("byteOffset", 0) + v0["byteLength"]] == b1[v1.get("byteOffset", 0):v1.get("byteOffset", 0) + v1["byteLength"]], "%s view %d changed" % (name, i)
    assert j1["accessors"][:len(j0["accessors"])] == j0["accessors"], name
    for k in ("nodes", "scenes", "samplers"):
        assert j0.get(k) == j1.get(k), (name, k)
    for m0, m1 in zip(j0["meshes"], j1["meshes"]):
        for p0, p1 in zip(m0["primitives"], m1["primitives"]):
            for key in ("POSITION", "NORMAL", "TEXCOORD_0"):
                assert p0["attributes"].get(key) == p1["attributes"].get(key), (name, key)
            assert p0.get("indices") == p1.get("indices") and p0.get("material") == p1.get("material"), name
    return "%s: %d pictures swapped, %d normal maps, %d tangent sets, %d colour sets calmed; geometry identical" % (
        name, len(img_views), len(normal_tex), ntan, ncol)


if __name__ == "__main__":
    names = sys.argv[1:] or sorted(p.stem for p in SRC.glob("*.glb"))
    for n in names:
        print(build(n))
    print("CALM GLBS", len(names))
