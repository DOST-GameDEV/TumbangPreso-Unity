import sys
import os
import re
import math
import numpy as np

sys.path.insert(0, "tools")
from glb_mesh_dump import read_glb, read_accessor

try:
    from PIL import Image, ImageDraw
except ImportError:
    raise SystemExit("this needs Pillow: python -m pip install pillow")

DEFAULT = "Assets/TumbangPreso/Art/characters/persons/team-sean.glb"
OUT = "Logs/preview-person.png"
BG = (0x2A, 0x2C, 0x3C)
LIGHT = (-0.35, 0.62, 0.70)
BAND = 0.42
SHADOW = 0.62

def slot_at(uv):
    u, v = uv[0], uv[1]
    col = min(int(u * 16.0), 15)
    row = min(int(v * 16.0), 15)
    if row < 8:
        return 0
    return (col // 2) + (8 if row >= 12 else 0)

def parse_tres_palette(tres_path):
    if not os.path.exists(tres_path):
        return None
    with open(tres_path, "r", encoding="utf-8") as f:
        content = f.read()
    m = re.search(r"PackedColorArray\(([^)]+)\)", content)
    if not m:
        return None
    tokens = [t.strip() for t in m.group(1).split(",")]
    pal = {}
    slot = 0
    for i in range(0, len(tokens), 4):
        if i + 2 < len(tokens):
            r = int(float(tokens[i]) * 255)
            g = int(float(tokens[i+1]) * 255)
            b = int(float(tokens[i+2]) * 255)
            pal[slot] = (r, g, b)
            slot += 1
    return pal

def palette(path):
    base_name = os.path.splitext(os.path.basename(path))[0]
    tres_path = os.path.join("MapSource/materials_persons", f"person_{base_name}.tres")
    pal = parse_tres_palette(tres_path)
    if pal is not None:
        return pal
    
    # Fallback to general colors
    return {
        0: (255, 255, 255), 1: (200, 200, 200), 2: (40, 180, 220), 4: (212, 40, 40),
        5: (160, 30, 30), 6: (255, 186, 0), 7: (55, 80, 115), 8: (20, 20, 22),
        9: (40, 60, 90), 10: (255, 186, 0), 11: (20, 20, 22), 12: (60, 60, 65),
        13: (200, 138, 82), 14: (160, 110, 65), 15: (230, 160, 100)
    }

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

def render(tris, pal, yaw, pitch, label, lo, hi, half, W, H):
    colour = np.zeros((H, W, 3), dtype=np.uint8)
    colour[:] = BG
    depth = np.full((H, W), 1e9)

    cy, sy = math.cos(yaw), math.sin(yaw)
    cp, sp = math.cos(pitch), math.sin(pitch)
    ln = math.sqrt(sum(v * v for v in LIGHT))
    lx, ly, lz = LIGHT[0] / ln, LIGHT[1] / ln, LIGHT[2] / ln

    mid_y = (lo[1] + hi[1]) / 2.0
    scale = (H - 40) / (hi[1] - lo[1])

    for (p0, p1, p2), (nx, ny, nz), uv in tris:
        slot = slot_at(uv)
        base = pal.get(slot, (180, 180, 180))

        dot = nx * lx + ny * ly + nz * lz
        factor = 1.0 if dot > BAND else SHADOW
        rgb = tuple(int(c * factor) for c in base)

        scr = []
        zs = []
        for p in (p0, p1, p2):
            rx = p[0] * cy + p[2] * sy
            rz0 = -p[0] * sy + p[2] * cy
            ry = (p[1] - mid_y) * cp - rz0 * sp
            rz = (p[1] - mid_y) * sp + rz0 * cp

            sx = int(W / 2 + rx * scale)
            sy_px = int(H / 2 - ry * scale)
            scr.append((sx, sy_px))
            zs.append(rz)

        (x0, y0), (x1, y1), (x2, y2) = scr
        z0, z1, z2 = zs

        min_x = max(0, min(x0, x1, x2))
        max_x = min(W - 1, max(x0, x1, x2))
        min_y = max(0, min(y0, y1, y2))
        max_y = min(H - 1, max(y0, y1, y2))

        denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
        if abs(denom) < 1e-5:
            continue

        for y in range(min_y, max_y + 1):
            for x in range(min_x, max_x + 1):
                w0 = ((y1 - y2) * (x - x2) + (x2 - x1) * (y - y2)) / denom
                w1 = ((y2 - y0) * (x - x2) + (x0 - x2) * (y - y2)) / denom
                w2 = 1.0 - w0 - w1
                if w0 >= 0 and w1 >= 0 and w2 >= 0:
                    pz = w0 * z0 + w1 * z1 + w2 * z2
                    if pz < depth[y, x]:
                        depth[y, x] = pz
                        colour[y, x] = rgb

    img = Image.fromarray(colour)
    draw = ImageDraw.Draw(img)
    draw.text((10, H - 20), label, fill=(150, 150, 160))
    return img

def main():
    path = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith("--") else DEFAULT
    head_only = "--head" in sys.argv

    pal = palette(path)
    tris = load(path)

    all_pts = [p for (tri, n, uv) in tris for p in tri]
    min_x = min(p[0] for p in all_pts)
    max_x = max(p[0] for p in all_pts)
    min_y = min(p[1] for p in all_pts)
    max_y = max(p[1] for p in all_pts)
    min_z = min(p[2] for p in all_pts)
    max_z = max(p[2] for p in all_pts)

    if head_only:
        min_y = 0.343
        tris = [(tri, n, uv) for (tri, n, uv) in tris if any(p[1] >= min_y for p in tri)]

    lo = (min_x, min_y, min_z)
    hi = (max_x, max_y, max_z)
    half = max(max_x - min_x, max_z - min_z) / 2.0

    W, H = 240, 480
    views = [
        (0.0, 0.0, "front"),
        (math.pi / 4, 0.0, "three-quarter"),
        (math.pi / 2, 0.0, "side"),
        (math.pi, 0.0, "back"),
        (0.0, math.pi / 2, "top-down"),
    ]

    images = []
    for yaw, pitch, lbl in views:
        images.append(render(tris, pal, yaw, pitch, lbl, lo, hi, half, W, H))

    sheet = Image.new("RGB", (W * len(views), H))
    for i, im in enumerate(images):
        sheet.paste(im, (i * W, 0))

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.save(OUT)
    print(f"wrote {OUT}")

if __name__ == "__main__":
    main()
