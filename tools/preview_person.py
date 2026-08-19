"""A turnaround of a built person .glb without starting Unity.

    python tools/preview_person.py                       the whole figure
    python tools/preview_person.py --head                just the head

WHY THIS EXISTS. `PersonSwapProbe` is the real check and it photographs the character
properly, through the game's own shader, in the game's own scene. It also costs a Unity
launch, an asset reimport and a play-mode frame, which is minutes. Authoring hair is a
loop of small numeric changes where nearly every one of them is wrong, and a loop that
long is why four passes of it shipped without anyone seeing the result.

This draws the same triangles with a z-buffer, the palette's own colours and the toon
pass's two flat bands, in about a second. It is a DRAFTING tool: when the shape is right,
run the probe, because the things this does not model are exactly the things that have
bitten this file before.

⚠️ WHAT IT DOES NOT MODEL, so do not sign anything off on it:
  * the ink outline, which is 8 mm in this space and closes small gaps and thin strokes.
    `tools/face_mouth_sheet.py` covers that for the face specifically, and it had to.
  * `ColourGrade`'s ACES curve, so every colour here arrives cooler than in the game.
  * the animation clips. This is the bind pose, and a mop that clips the shoulder only
    does so once the head turns.
"""
import sys
import math

import numpy as np

sys.path.insert(0, "tools")
from glb_mesh_dump import read_glb, read_accessor  # noqa: E402

try:
    from PIL import Image, ImageDraw
except ImportError:
    raise SystemExit("this needs Pillow: python -m pip install pillow")

DEFAULT = "Assets/TumbangPreso/Art/characters/persons/team-zack.glb"
OUT = "Logs/preview-person.png"
BG = (0x2A, 0x2C, 0x3C)

# Roughly where the probe puts its key light. See the module docstring: this is a drafting
# approximation of `Toon.shader`, not a copy of it.
LIGHT = (-0.35, 0.62, 0.70)
BAND = 0.42
SHADOW = 0.62


def palette():
    sys.path.insert(0, ".")
    import importlib.util

    spec = importlib.util.spec_from_file_location("bpv", "tools/build_person_voxel.py")
    bpv = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(bpv)

    def rgb(h):
        h = h.lstrip("#")
        return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))

    return {s: rgb(bpv.PALETTE[s]) for s in bpv.PALETTE}, bpv


def load(path):
    gltf, buffer = read_glb(path)
    tris = []

    for node in gltf["nodes"]:
        if "mesh" not in node:
            continue

        for prim in gltf["meshes"][node["mesh"]]["primitives"]:
            pos = [tuple(p) for p in read_accessor(gltf, buffer, prim["attributes"]["POSITION"])]
            nrm = [tuple(n) for n in read_accessor(gltf, buffer, prim["attributes"]["NORMAL"])]
            uv = read_accessor(gltf, buffer, prim["attributes"]["TEXCOORD_0"])
            raw = read_accessor(gltf, buffer, prim["indices"])
            idx = [v[0] for v in raw] if isinstance(raw[0], tuple) else list(raw)

            for t in range(0, len(idx), 3):
                a, b, c = idx[t], idx[t + 1], idx[t + 2]
                n = tuple((nrm[a][k] + nrm[b][k] + nrm[c][k]) / 3.0 for k in range(3))
                tris.append(((pos[a], pos[b], pos[c]), n, uv[a]))

    return tris


def render(tris, pal, slot_at, yaw, label, lo, hi, half, W, H):
    """One orthographic view, z-buffered."""
    colour = np.zeros((H, W, 3), dtype=np.uint8)
    colour[:] = BG
    depth = np.full((H, W), 1e9)

    cy, sy = math.cos(yaw), math.sin(yaw)
    ln = math.sqrt(sum(v * v for v in LIGHT))
    light = tuple(v / ln for v in LIGHT)

    def rot(p):
        return (p[0] * cy + p[2] * sy, p[1], -p[0] * sy + p[2] * cy)

    ys, xs = np.mgrid[0:H, 0:W]

    for poly, n, uv in tris:
        r = [rot(p) for p in poly]
        rn = rot(n)

        px = [((p[0] + half) / (2 * half) * W, H - (p[1] - lo) / (hi - lo) * H) for p in r]

        x0 = max(0, int(min(p[0] for p in px)))
        x1 = min(W, int(max(p[0] for p in px)) + 2)
        y0 = max(0, int(min(p[1] for p in px)))
        y1 = min(H, int(max(p[1] for p in px)) + 2)

        if x0 >= x1 or y0 >= y1:
            continue

        (ax, ay), (bx, by), (cx, cy2) = px
        area = (bx - ax) * (cy2 - ay) - (cx - ax) * (by - ay)
        if abs(area) < 1e-9:
            continue

        sub_x = xs[y0:y1, x0:x1] + 0.5
        sub_y = ys[y0:y1, x0:x1] + 0.5

        w0 = ((bx - ax) * (sub_y - ay) - (sub_x - ax) * (by - ay)) / area
        w1 = ((sub_x - ax) * (cy2 - ay) - (cx - ax) * (sub_y - ay)) / area
        inside = (w0 >= 0) & (w1 >= 0) & (w0 + w1 <= 1)

        if not inside.any():
            continue

        # ⚠️ NEGATED. glTF puts +Z toward the viewer, so nearer is a LARGER z and a
        # depth buffer that keeps the smallest one photographs the back of the head.
        za, zb, zc = -r[0][2], -r[1][2], -r[2][2]
        z = za + w1 * (zb - za) + w0 * (zc - za)

        win = inside & (z < depth[y0:y1, x0:x1])
        if not win.any():
            continue

        lam = max(0.0, rn[0] * light[0] + rn[1] * light[1] + rn[2] * light[2])
        k = 1.0 if lam > BAND else SHADOW
        base = pal.get(slot_at(*uv), (255, 0, 255))
        rgb = np.array([min(255, int(v * k)) for v in base], dtype=np.uint8)

        sub_d = depth[y0:y1, x0:x1]
        sub_d[win] = z[win]
        depth[y0:y1, x0:x1] = sub_d

        sub_c = colour[y0:y1, x0:x1]
        sub_c[win] = rgb
        colour[y0:y1, x0:x1] = sub_c

    img = Image.fromarray(colour)
    if label:
        ImageDraw.Draw(img).text((8, H - 18), label, fill=(0xCC, 0xCC, 0xCC))
    return img


def main():
    path = next((a for a in sys.argv[1:] if not a.startswith("--")), DEFAULT)
    head_only = "--head" in sys.argv

    pal, bpv = palette()
    tris = load(path)

    lo, hi = (0.33, 0.79) if head_only else (-0.02, 0.79)
    half = 0.28 if head_only else 0.42
    W, H = 300, 420

    views = [(0.0, "front"), (math.radians(35), "three-quarter"),
             (math.radians(90), "side"), (math.radians(180), "back"),
             (math.radians(-90), "other side")]

    sheet = Image.new("RGB", (W * len(views), H), BG)
    for n, (yaw, label) in enumerate(views):
        sheet.paste(render(tris, pal, bpv._slot_at, yaw, label, lo, hi, half, W, H), (W * n, 0))

    sheet.save(OUT)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
