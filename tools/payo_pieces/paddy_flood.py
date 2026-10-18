"""WATERED PADDIES for Payo (owner, 2026-10-18: "add patches reflective water puddles across the terraces as to show
some watered rice fields"). Picks whole paddy fields to stand under water and writes the map the land shader reads.

  python -I tools/payo_pieces/paddy_flood.py [--land=v59] [--share=0.16] [--seed=7]   (numpy, scipy, Pillow)

Writes Assets/TumbangPreso/Art/Payo/Textures/land/paddy_flood.png (8 bit, 1 px a metre, world space) and paddy_flood.txt
(x0 y0 x1 y1 of the picture in Blender metres, row 0 at y1, the north), plus a review picture
Logs/payo/unity/paddies/flood_map.png.

THE APPROVED LAND IS NOT TOUCHED. The land export (tools/payo_land.py, frozen) already has a few flooded paddies
(material 5), almost all of them 500 m and more from the court. This picks MORE fields, as a map over the existing paddy
material, so the land is not re-exported: the shader (PayoLand, `_SURFACE_PADDY`) turns a paddy into water where this
map says so, and `_SURFACE_FLOODED` is water everywhere.

How fields are picked:
- A field is a connected paddy area between walls and dikes, told apart exactly as rice_density() in
  tools/payo_land_tex.py tells them (the rasterised paddy faces minus the dilated walls, labelled).
- Only fields whose planting is already SPARSE (mean rice_density under SPARSE) can flood, so the rice blades grown from
  rice_density.png stand as young sprouts there and the blade baker needs no change. Ripe gold fields never flood.
- Fields flood in PATCHES, not one by one: a broad noise field (CLUSTER_M) decides where water is let in, so a run of
  neighbouring fields down a slope is under water together, as an irrigation channel feeds them in turn.
- The share of paddy area is about `share` overall, and NEAR (inside NEAR_M of the court) the near share is held to
  at least NEAR_SHARE so the patches show from where the players stand.
The value is 1 inside a flooded field and falls to 0 over about a metre at its dikes (a gaussian of BLUR px), so the
shader can lay a muddy shallow rim along the edge.
"""
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LAND_TEX = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Payo", "Textures", "land")
REVIEW = os.path.join(ROOT, "Logs", "payo", "unity", "paddies")

RICE_PPM = 2.0          # rice_density.png's resolution (fields are found on its grid)
OUT_PPM = 1.0           # the flood map's resolution
SPARSE = 0.42           # a field may flood when its mean rice density is under this
CLUSTER_M = 70.0        # the patches' size: the broad noise's gaussian, metres
NEAR_M, NEAR_SHARE = 320.0, 0.22
BLUR = 1.2              # px of OUT_PPM: the soft dike edge
MIN_AREA = 40.0         # m2: slivers are left dry


def main():
    args = dict(a[2:].split("=", 1) for a in sys.argv[1:] if a.startswith("--") and "=" in a)
    tag, share, seed = args.get("land", "v59"), float(args.get("share", 0.16)), int(args.get("seed", 7))
    d = np.load(os.path.join(ROOT, "Logs", "payo", "terrain", "land_%s.npz" % tag))
    v, lo, tot, mat, lc = d["verts"], d["loops"], d["totals"], d["mat"], d["lcol"]
    st = np.concatenate([[0], np.cumsum(tot)[:-1]])
    x0, y0, x1, y1 = [float(t) for t in open(os.path.join(LAND_TEX, "rice_density.txt")).read().split()[:4]]
    w, h = int((x1 - x0) * RICE_PPM), int((y1 - y0) * RICE_PPM)
    rice = np.asarray(Image.open(os.path.join(LAND_TEX, "rice_density.png")).convert("L"), np.float32) / 255.0
    assert rice.shape == (h, w), (rice.shape, h, w)

    def raster(faces, value=None, n=7):
        out = np.zeros((h, w), np.float32 if value is not None else bool)
        tri = np.stack([v[lo[st[faces] + k]] for k in range(3)], 1)
        for i in range(n + 1):
            for j in range(n + 1 - i):
                a_, b_ = i / n, j / n
                q = tri[:, 0] * (1 - a_ - b_) + tri[:, 1] * a_ + tri[:, 2] * b_
                ix = ((q[:, 0] - x0) * RICE_PPM).astype(int); iy = ((y1 - q[:, 1]) * RICE_PPM).astype(int)
                ok = (ix >= 0) & (ix < w) & (iy >= 0) & (iy < h)
                out[iy[ok], ix[ok]] = True if value is None else value[ok]
        return out

    pad = np.where((mat == 2) | (mat == 5))[0]
    c = lc[st[pad]]
    gold = (c[:, 0] / np.maximum(c[:, 1], 1e-3) > 1.0).astype(np.float32)
    old = (mat[pad] == 5).astype(np.float32)
    raw = raster(pad)
    goldm = raster(pad, gold) > 0.5
    oldm = raster(pad, old) > 0.5
    walls = ndi.binary_dilation(raster(np.where(mat == 1)[0]), iterations=1)
    fields, n = ndi.label(raw & ~walls)
    idx = np.arange(1, n + 1)
    area = ndi.sum(np.ones_like(rice), fields, idx) / RICE_PPM ** 2
    dens = ndi.mean(rice, fields, idx)
    goldf = ndi.mean(goldm.astype(np.float32), fields, idx) > 0.3
    oldf = ndi.mean(oldm.astype(np.float32), fields, idx) > 0.3
    cy, cx = np.array(ndi.center_of_mass(np.ones_like(rice), fields, idx)).T
    fx, fy = x0 + cx / RICE_PPM, y1 - cy / RICE_PPM
    dist = np.hypot(fx, fy)

    # the patches: a broad smooth noise, read at each field's centre
    rng = np.random.default_rng(seed)
    g = 8.0                                                  # metres a noise cell
    nz = ndi.gaussian_filter(rng.standard_normal((int(h / RICE_PPM / g) + 2, int(w / RICE_PPM / g) + 2)), CLUSTER_M / g)
    nz /= nz.std()
    patch = nz[np.clip((y1 - fy) / g, 0, nz.shape[0] - 1).astype(int), np.clip((fx - x0) / g, 0, nz.shape[1] - 1).astype(int)]
    jitter = rng.uniform(-0.35, 0.35, n)
    score = patch + jitter
    ok = (dens < SPARSE) & ~goldf & (area >= MIN_AREA)
    total = area.sum()

    def pick(sel, want):
        """The best-scored allowed fields of `sel` until `want` m2 are under water."""
        order = np.argsort(-np.where(sel & ok, score, -1e9))
        chosen = np.zeros(n, bool); got = 0.0
        for i in order:
            if not (sel[i] and ok[i]) or got >= want: break
            chosen[i] = True; got += area[i]
        return chosen

    near = dist < NEAR_M
    flood = pick(near, NEAR_SHARE * area[near].sum()) | pick(~near, share * area[~near].sum())
    flood |= oldf                                              # the export's own flooded fields stay water
    lab = np.concatenate([[False], flood])[fields]

    # down to the output grid, the soft edge
    k = int(round(RICE_PPM / OUT_PPM))
    hh, ww = h // k, w // k
    m = lab[:hh * k, :ww * k].reshape(hh, k, ww, k).mean((1, 3)).astype(np.float32)
    m = ndi.gaussian_filter(m, BLUR)
    Image.fromarray((np.clip(m, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(LAND_TEX, "paddy_flood.png"))
    ox1, oy0 = x0 + ww * k / RICE_PPM, y1 - hh * k / RICE_PPM
    open(os.path.join(LAND_TEX, "paddy_flood.txt"), "w").write(
        "%g %g %g %g\n# x0 y0 x1 y1 in metres (Blender object space of the land), %g px a metre, row 0 at y1 (north), "
        "column 0 at x0; 1 = the paddy is under water (tools/payo_pieces/paddy_flood.py)\n" % (x0, oy0, ox1, y1, OUT_PPM))

    os.makedirs(REVIEW, exist_ok=True)
    rv = np.zeros((h, w, 3), np.uint8)
    rv[raw] = (120, 150, 80); rv[goldm & raw] = (190, 160, 70); rv[walls] = (70, 60, 50)
    rv[lab] = (90, 170, 230); rv[lab & oldm] = (40, 90, 200)
    Image.fromarray(rv).resize((w // 4, h // 4), Image.NEAREST).save(os.path.join(REVIEW, "flood_map.png"))
    fa = area[flood].sum()
    print("fields %d, allowed %d; flooded %d fields, %.0f m2 = %.1f%% of paddy (near %d m: %.1f%%, %d fields; the export's own: %d)"
          % (n, ok.sum(), flood.sum(), fa, 100 * fa / total, NEAR_M, 100 * area[flood & near].sum() / max(area[near].sum(), 1),
             (flood & near).sum(), oldf.sum()))
    print("map %d x %d px -> %s" % (ww, hh, os.path.join(LAND_TEX, "paddy_flood.png")))


if __name__ == "__main__":
    main()
