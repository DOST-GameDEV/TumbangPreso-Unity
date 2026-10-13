"""
WHERE EVERY HERO'S EYES ARE IN THEIR ATLAS (Nemu's LET THEM IN, 2026-10-14, `docs/reports/nemu-ultimate-2026-10-11/direction.md`
section 16). The owner liked glowing eyes but not glows floating in front of the face, so the possessed glow is drawn ON the
painted eyes: `Shaders/NemuEyes` lights the texels inside these rectangles that are not skin.

For each `Art/CharacterRedesign/<hero>/<hero>-redesign.glb`, the head mesh is drawn from the front with its atlas; the skin colour
is read off the cheeks; the eyes are the two blobs that are not skin, enclosed by skin, either side of the middle. Writes
`Resources/Visual/eye-rects.txt` (UVs in Unity's convention, v up) and a check sheet to `Logs/eye-rects.png`.

    python tools/author_eye_rects.py
"""
import glob
import json
import os
import struct

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
ART = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign")
OUT = os.path.join(ROOT, "Assets/TumbangPreso/Resources/Visual/eye-rects.txt")
SHEET = os.path.join(ROOT, "Logs/eye-rects.png")
S = 240
# Only the playable heroes (the ones with ultimates; `tools/author_ultimate_intros.py`): any of them can be possessed.
HEROES = ("amihan", "cheska", "dante", "nemu", "paete", "phaister", "rafi", "sean", "zack")
# Where to look on the front view (pixels, y0 y1 x0 x1) for faces the search alone gets wrong: Nemu's eyes sit right under her
# fringe, Dante's touch his, Cheska's head reads with her whole body, Phaister's face is under her hat.
HINTS = {
    "nemu": (148, 170, 55, 185),
    "dante": (143, 170, 30, 160),
    "cheska": (95, 120, 70, 170),
    "phaister": (166, 186, 65, 160),
}


def load(path):
    b = open(path, "rb").read()
    n = struct.unpack("<I", b[12:16])[0]
    j = json.loads(b[20:20 + n])
    data = b[20 + n + 8:]

    def acc(i):
        a = j["accessors"][i]
        bv = j["bufferViews"][a["bufferView"]]
        k = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
        dt = {5126: np.float32, 5123: np.uint16, 5125: np.uint32, 5121: np.uint8}[a["componentType"]]
        off = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
        arr = np.frombuffer(data, dt, a["count"] * k, off)
        return arr.reshape(-1, k) if k > 1 else arr

    prim = [m for m in j["meshes"] if "head" in m["name"]][0]["primitives"][0]
    return acc(prim["attributes"]["POSITION"]), acc(prim["attributes"]["TEXCOORD_0"]), acc(prim["indices"]).reshape(-1, 3)


def front(P, UV, I, atlas):
    """The head from the front (+z): its colours and, per pixel, the atlas UV (glTF convention, v down)."""
    H, W = atlas.shape[:2]
    x0, x1, y0, y1 = P[:, 0].min(), P[:, 0].max(), P[:, 1].min(), P[:, 1].max()
    span = max(x1 - x0, y1 - y0)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    x0, y1 = cx - span / 2, cy + span / 2
    img = np.zeros((S, S, 3))
    depth = np.full((S, S), -9.0)
    uvs = np.full((S, S, 2), -1.0)
    for t in I:
        a, b, c = P[t]
        if np.cross(b - a, c - a)[2] <= 1e-9:
            continue
        pa, pb, pc = [np.array(((P[k][0] - x0) / span * S, (y1 - P[k][1]) / span * S)) for k in t]
        lo = np.floor(np.minimum(np.minimum(pa, pb), pc)).astype(int).clip(0, S - 1)
        hi = np.ceil(np.maximum(np.maximum(pa, pb), pc)).astype(int).clip(0, S - 1)
        xs, ys = np.meshgrid(np.arange(lo[0], hi[0] + 1) + .5, np.arange(lo[1], hi[1] + 1) + .5)
        v0, v1 = pb - pa, pc - pa
        den = v0[0] * v1[1] - v1[0] * v0[1]
        if abs(den) < 1e-12:
            continue
        wx, wy = xs - pa[0], ys - pa[1]
        l1 = (wx * v1[1] - v1[0] * wy) / den
        l2 = (v0[0] * wy - wx * v0[1]) / den
        l0 = 1 - l1 - l2
        m = (l0 >= 0) & (l1 >= 0) & (l2 >= 0)
        z = l0 * P[t[0], 2] + l1 * P[t[1], 2] + l2 * P[t[2], 2]
        uv = l0[..., None] * UV[t[0]] + l1[..., None] * UV[t[1]] + l2[..., None] * UV[t[2]]
        iy, ix, zz, u = ys.astype(int)[m], xs.astype(int)[m], z[m], uv[m]
        ok = zz > depth[iy, ix]
        depth[iy[ok], ix[ok]] = zz[ok]
        uvs[iy[ok], ix[ok]] = u[ok]
        img[iy[ok], ix[ok]] = atlas[(u[ok, 1] * H).astype(int).clip(0, H - 1), (u[ok, 0] * W).astype(int).clip(0, W - 1)]
    return img, uvs, depth > -9


def components(mask):
    """4-connected blobs of a mask: a list of (ys, xs) index arrays."""
    seen = np.zeros_like(mask, bool)
    out = []
    for y0, x0 in zip(*np.nonzero(mask)):
        if seen[y0, x0]:
            continue
        stack, ys, xs = [(y0, x0)], [], []
        seen[y0, x0] = True
        while stack:
            y, x = stack.pop()
            ys.append(y)
            xs.append(x)
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                v, w = y + dy, x + dx
                if 0 <= v < S and 0 <= w < S and mask[v, w] and not seen[v, w]:
                    seen[v, w] = True
                    stack.append((v, w))
        out.append((np.array(ys), np.array(xs)))
    return out


def eyes(img, cover, enclosed=.45):
    """The skin colour and the two eye blobs (or None). `enclosed`: how much of the ring round a blob must be skin."""
    ys, xs = np.nonzero(cover)
    left, right, top, bottom = xs.min(), xs.max(), ys.min(), ys.max()
    width, mid = right - left, (left + right) / 2
    # The skin: the commonest colour on the lower middle of the face (the cheeks and chin).
    patch = img[int(top + (bottom - top) * .62):int(top + (bottom - top) * .85), int(mid - width * .25):int(mid + width * .25)].reshape(-1, 3)
    keys, counts = np.unique((patch // 12).astype(int), axis=0, return_counts=True)
    skin = (keys[counts.argmax()] * 12 + 6).astype(float)
    off = np.linalg.norm(img - skin, axis=2)
    skinish = cover & (off < 40)
    # A feature: clearly not skin (darker, or another colour altogether, such as a yellow eye).
    feature = cover & (off > 70)
    found = []
    for cy, cx in components(feature):
        if len(cy) < 6 or len(cy) > S * S * .04:
            continue
        y0, y1, x0, x1 = cy.min(), cy.max(), cx.min(), cx.max()
        if y0 <= top + 1 or x0 <= left + 1 or x1 >= right - 1:
            continue
        # Enclosed by skin: most of the ring just outside its box is skin (hair is not).
        ring = np.zeros((S, S), bool)
        ring[max(0, y0 - 3):y1 + 4, max(0, x0 - 3):x1 + 4] = True
        ring[y0:y1 + 1, x0:x1 + 1] = False
        if skinish[ring].mean() < enclosed:
            continue
        cxm = cx.mean()
        if abs(cxm - mid) < width * .08:
            continue  # the mouth or the nose, in the middle
        found.append(dict(ys=cy, xs=cx, x=cxm, y=cy.mean(), n=len(cy)))
    pair, score = None, None
    for a in found:
        for b in found:
            if a["x"] >= mid or b["x"] <= mid:
                continue
            sym = abs((mid - a["x"]) - (b["x"] - mid)) + abs(a["y"] - b["y"]) * 2
            # A symmetric pair, high on the face and big.
            s = sym + (a["y"] + b["y"]) * .05 - (a["n"] + b["n"]) * .02
            if score is None or s < score:
                pair, score = (a, b), s
    return skin, pair


def hinted(img, uvs, box):
    """The skin (the cheeks just under the box) and, in each half of the box, the blob that is not skin: the pair on one row
    of the atlas (a strand of hair across the face maps somewhere else), the biggest."""
    y0, y1, x0, x1 = box
    patch = img[y1:y1 + 12, x0:x1].reshape(-1, 3)
    keys, counts = np.unique((patch // 12).astype(int), axis=0, return_counts=True)
    skin = (keys[counts.argmax()] * 12 + 6).astype(float)
    feature = np.zeros((S, S), bool)
    feature[y0:y1, x0:x1] = np.linalg.norm(img[y0:y1, x0:x1] - skin, axis=2) > 70
    mid = (x0 + x1) / 2
    halves = [[], []]
    for cy, cx in components(feature):
        if len(cy) < 4:
            continue
        u = uvs[cy, cx]
        u = u[u[:, 0] >= 0]
        if len(u) == 0:
            continue
        halves[0 if cx.mean() < mid else 1].append(dict(ys=cy, xs=cx, n=len(cy), v=float(np.median(u[:, 1]))))
    # Every hero's face is painted in the atlas's top band (glTF v under 0.2): blobs mapped elsewhere are hair or hat.
    face = [[e for e in h if e["v"] < .2] for h in halves]
    if face[0] and face[1]:
        halves = face
    if not halves[0] or not halves[1]:
        return skin, None
    return skin, min(((a, b) for a in halves[0] for b in halves[1]), key=lambda p: abs(p[0]["v"] - p[1]["v"]) * 200 - (p[0]["n"] + p[1]["n"]) * .01)


def main():
    rows, tiles = [], []
    for folder in sorted(glob.glob(os.path.join(ART, "*/"))):
        hero = os.path.basename(folder.rstrip("/\\"))
        if hero not in HEROES:
            continue
        glb = os.path.join(folder, hero + "-redesign.glb")
        png = os.path.join(folder, hero + "-redesign-atlas.png")
        if not (os.path.exists(glb) and os.path.exists(png)):
            continue
        P, UV, I = load(glb)
        atlas = np.asarray(Image.open(png).convert("RGB")).astype(float)
        img, uvs, cover = front(P, UV, I, atlas)
        skin, pair = hinted(img, uvs, HINTS[hero]) if hero in HINTS else eyes(img, cover)
        # Eyes that touch a fringe or a frame (Nemu's sit right under her fringe): a looser ring.
        for enclosed in (.3, .18):
            if pair is None:
                skin, pair = eyes(img, cover, enclosed)
        tile = Image.fromarray(img.astype(np.uint8))
        draw = ImageDraw.Draw(tile)
        draw.text((3, 2), hero, fill=(255, 255, 255))
        tiles.append(tile)
        if pair is None:
            draw.text((3, 14), "NO EYES FOUND", fill=(255, 60, 60))
            continue
        fields = [hero + "-redesign-atlas"]
        for e in pair:
            u = uvs[e["ys"], e["xs"]]
            u = u[u[:, 0] >= 0]
            # Only the face's own patch of the atlas: hair hanging in front of the face maps elsewhere (Nemu's fringe).
            u = u[np.abs(u - np.median(u, axis=0)).max(axis=1) < .03]
            # A texel and a half of slack all round, then Unity's v (up).
            pad = 1.5 / atlas.shape[0]
            u0, u1 = u[:, 0].min() - pad, u[:, 0].max() + pad
            v0, v1 = 1 - (u[:, 1].max() + pad), 1 - (u[:, 1].min() - pad)
            fields += ["%.5f" % x for x in (u0, v0, u1, v1)]
            draw.rectangle([e["xs"].min() - 1, e["ys"].min() - 1, e["xs"].max() + 1, e["ys"].max() + 1], outline=(190, 80, 255))
        fields += ["%.3f" % (c / 255) for c in skin]
        rows.append(" ".join(fields))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", newline="\n") as f:
        f.write("# atlas  left-eye u0 v0 u1 v1  right-eye u0 v0 u1 v1  skin r g b   (tools/author_eye_rects.py; do not edit by hand)\n")
        f.write("\n".join(rows) + "\n")
    cols = 7
    sheet = Image.new("RGB", (cols * S, ((len(tiles) + cols - 1) // cols) * S))
    for k, t in enumerate(tiles):
        sheet.paste(t, ((k % cols) * S, (k // cols) * S))
    os.makedirs(os.path.dirname(SHEET), exist_ok=True)
    sheet.save(SHEET)
    print(len(rows), "heroes ->", OUT)


if __name__ == "__main__":
    main()
