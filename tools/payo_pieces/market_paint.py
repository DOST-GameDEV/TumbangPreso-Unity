"""PAYO market pieces: the ONE shared painted atlas (2026-10-19). System Python (numpy, PIL).

  python -I tools/payo_pieces/market_paint.py <ABS out folder>

Owner, 2026-10-19, of the basin's south terrace: "this central area of the basin wall needs some work too. its still
barren ... this one is large in area so it needs something more. maybe like some audience watching, or a marketplace
area." Every market piece (stalls, goods, stools, the cloth line) wears this one atlas, so in the game they are one
material (`py_market`), instanced and statically batched together.

The atlas follows Logs/payo/pieces/CALM_STYLE.md: each swatch is ONE base colour with one soft light-to-dark gradient
down the whole swatch (the kit's UVs stretch each face over its swatch, so the gradient runs down each whole face), a
few big motifs only (the woven red-black-white band on the mats and cloths), broad strand groups on the thatch and
the rice, no speckle. A height field in metres goes with it and is baked to a tangent-space normal map
(calm_glb.height_to_normal, region by region so neighbours never bleed).

Writes market_albedo.png, market_normal.png, market_height.png and market_regions.json (the swatch boxes, in pixels,
read by market_kit.py).
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import calm_glb  # noqa: E402

S = 1024
# name: (x, y, w, h) in pixels from the top left, and the swatch's size in metres (for the normal map's slope)
REGIONS = {
    "thatch":      ((0, 0, 512, 256), 2.4),
    "thatch_dark": ((512, 0, 256, 128), 1.2),
    "bamboo":      ((768, 0, 256, 128), 1.0),
    "wood":        ((512, 128, 256, 128), 1.0),
    "wood_light":  ((768, 128, 256, 128), 1.0),
    "mat":         ((0, 256, 256, 256), 1.6),
    "cloth_red":   ((256, 256, 128, 256), 1.2),
    "cloth_indigo": ((384, 256, 128, 256), 1.2),
    "cloth_ochre": ((512, 256, 128, 256), 1.2),
    "cloth_cream": ((640, 256, 128, 256), 1.2),
    "terracotta":  ((768, 256, 128, 128), 0.6),
    "clay_dark":   ((896, 256, 128, 128), 0.6),
    "basket":      ((768, 384, 256, 128), 0.8),
    "rice":        ((0, 512, 256, 128), 0.8),
    "banana":      ((256, 512, 128, 128), 0.3),
    "banana_green": ((384, 512, 128, 128), 0.3),
    "leaf":        ((512, 512, 128, 128), 1.0),
    "areca":       ((640, 512, 128, 128), 0.3),
    "betel":       ((768, 512, 128, 128), 0.3),
    "stone":       ((896, 512, 128, 128), 0.6),
    "rope":        ((0, 640, 128, 128), 0.3),
    "ash":         ((128, 640, 128, 128), 0.6),
    "tuber":       ((256, 640, 128, 128), 0.3),
    "greens":      ((384, 640, 128, 128), 0.3),
    "cream":       ((512, 640, 128, 128), 0.6),
    "red":         ((640, 640, 128, 128), 0.6),
    "black":       ((768, 640, 128, 128), 0.6),
    "turquoise":   ((896, 640, 128, 128), 0.6),
}


def rgb(h):
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (1, 3, 5)], np.float32)


def grad(h, w, top, bottom, power=1.0):
    """A soft vertical gradient, `top` at the swatch's top row to `bottom` at its last (CALM rule 1)."""
    t = (np.linspace(0, 1, h, dtype=np.float32) ** power)[:, None, None]
    return np.broadcast_to(top * (1 - t) + bottom * t, (h, w, 3)).copy()


def smooth_noise(h, w, cells, seed):
    """Very low frequency value noise in 0..1 (broad colour drift, never speckle)."""
    rng = np.random.default_rng(seed)
    g = rng.random((cells + 2, cells + 2)).astype(np.float32)
    ys = np.linspace(0, cells, h, dtype=np.float32)
    xs = np.linspace(0, cells, w, dtype=np.float32)
    y0, x0 = ys.astype(int), xs.astype(int)
    fy, fx = ys - y0, xs - x0
    fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)
    a = g[y0][:, x0]; b = g[y0][:, x0 + 1]; c = g[y0 + 1][:, x0]; d = g[y0 + 1][:, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy)[:, None] + (c * (1 - fx) + d * fx) * fy[:, None]


def strands(h, w, groups, seed, depth=0.006):
    """Broad strand groups running down the swatch (thatch, rice): soft rounded ridges, a few per swatch."""
    rng = np.random.default_rng(seed)
    x = np.linspace(0, groups, w, dtype=np.float32)
    phase = rng.random(groups + 2).astype(np.float32)
    k = x.astype(int)
    f = x - k
    ridge = np.sin(np.pi * f) ** 0.7 * (0.8 + 0.4 * phase[k])
    wob = smooth_noise(h, w, 3, seed + 1) * 0.3
    return (ridge[None, :] * (0.85 + wob)) * depth


def band_stripes(n, spec):
    """Rows of colour along one axis: spec = [(start, end, colour)] in 0..1, later ones on top."""
    out = np.zeros((n, 3), np.float32)
    t = np.linspace(0, 1, n, dtype=np.float32)
    for a, b, c in spec:
        out[(t >= a) & (t < b)] = c
    return out


def paint():
    A = np.zeros((S, S, 3), np.float32) + rgb("#c9b48a")
    H = np.zeros((S, S), np.float32)

    def put(name, img, height=None):
        (x, y, w, h), _ = REGIONS[name]
        A[y:y + h, x:x + w] = img
        if height is not None:
            H[y:y + h, x:x + w] = height

    def size(name):
        (x, y, w, h), _ = REGIONS[name]
        return h, w

    # ---- thatch: one golden straw, lighter at the ridge, broad strand groups, a darker soft lip at the eave
    h, w = size("thatch")
    img = grad(h, w, rgb("#e8c878"), rgb("#c79a52"), 1.3)
    hh = strands(h, w, 14, 3, 0.012)
    img *= (0.94 + 0.10 * (hh / 0.012))[..., None]
    lip = np.clip((np.linspace(0, 1, h) - 0.86) / 0.14, 0, 1)[:, None, None]
    img = img * (1 - 0.22 * lip)
    # two soft horizontal course lines (the tied layers), broad and faint
    for c in (0.33, 0.66):
        t = np.exp(-((np.linspace(0, 1, h) - c) / 0.025) ** 2)[:, None, None]
        img *= 1 - 0.07 * t
        hh -= 0.006 * t[..., 0]
    put("thatch", img, hh)
    h, w = size("thatch_dark")
    put("thatch_dark", grad(h, w, rgb("#9b7442"), rgb("#7a5834")), strands(h, w, 8, 5, 0.005))

    # ---- bamboo: pale green-gold, three soft node rings down the pole
    h, w = size("bamboo")
    img = grad(h, w, rgb("#d9c47e"), rgb("#b39a58"))
    across = 0.9 + 0.12 * np.sin(np.linspace(0, np.pi, w))[None, :, None]
    img *= across
    hh = np.zeros((h, w), np.float32)
    for c in (0.18, 0.5, 0.82):
        t = np.exp(-((np.linspace(0, 1, h) - c) / 0.018) ** 2)[:, None]
        img = img * (1 - 0.18 * t[..., None])
        hh += 0.006 * t
    put("bamboo", img, hh)
    # ---- wood: warm brown, broad drift only (no grain lines)
    h, w = size("wood")
    put("wood", grad(h, w, rgb("#9a6a44"), rgb("#74492e")) * (0.95 + 0.08 * smooth_noise(h, w, 2, 7))[..., None])
    h, w = size("wood_light")
    put("wood_light", grad(h, w, rgb("#c49a6a"), rgb("#a37a50")) * (0.96 + 0.06 * smooth_noise(h, w, 2, 8))[..., None])

    # ---- the woven mat: pale buri, a soft checker weave in the height, the red-black-white band near each end
    h, w = size("mat")
    img = grad(h, w, rgb("#e6d3a2"), rgb("#cdb683"))
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    weave = (np.sin(xx / w * 2 * np.pi * 10) * np.sin(yy / h * 2 * np.pi * 10))
    hh = 0.003 * weave
    img *= (0.97 + 0.03 * weave)[..., None]
    band = band_stripes(w, [(0.08, 0.20, rgb("#a8322a")), (0.11, 0.13, rgb("#2a2422")), (0.15, 0.17, rgb("#f2e8d0")),
                            (0.80, 0.92, rgb("#a8322a")), (0.83, 0.85, rgb("#2a2422")), (0.87, 0.89, rgb("#f2e8d0"))])
    m = np.zeros(w, bool)
    t = np.linspace(0, 1, w)
    m[((t >= 0.08) & (t < 0.20)) | ((t >= 0.80) & (t < 0.92))] = True
    img[:, m] = band[None, m] * (0.97 + 0.03 * weave[:, m])[..., None]
    put("mat", img, hh)

    # ---- cloths: Ifugao blankets, one base colour, a bold band of thin stripes (2 colours), a plain field
    def cloth(name, base, a, b, seed):
        h, w = size(name)
        img = grad(h, w, rgb(base) * 1.04, rgb(base) * 0.86)
        spec = [(0.12, 0.30, rgb(a)), (0.15, 0.17, rgb(b)), (0.21, 0.23, rgb(b)), (0.25, 0.27, rgb(b)),
                (0.70, 0.88, rgb(a)), (0.73, 0.75, rgb(b)), (0.79, 0.81, rgb(b)), (0.83, 0.85, rgb(b))]
        rows = band_stripes(h, spec)
        t = np.linspace(0, 1, h)
        m = ((t >= 0.12) & (t < 0.30)) | ((t >= 0.70) & (t < 0.88))
        g = np.linspace(1.04, 0.86, h, dtype=np.float32)[:, None]
        img[m] = rows[m][:, None, :] * g[m][:, :, None]
        put(name, img, 0.002 * np.sin(np.linspace(0, 40 * np.pi, w, dtype=np.float32))[None, :].repeat(h, 0))

    cloth("cloth_red", "#a8322a", "#2a2422", "#f2e8d0", 1)
    cloth("cloth_indigo", "#33476e", "#f2e8d0", "#a8322a", 2)
    cloth("cloth_ochre", "#d39a3c", "#a8322a", "#2a2422", 3)
    cloth("cloth_cream", "#efe4c8", "#a8322a", "#2a2422", 4)

    # ---- clay: terracotta with one soft darker band (the incised shoulder), a dark clay
    h, w = size("terracotta")
    img = grad(h, w, rgb("#d47f52"), rgb("#a85a38"))
    t = np.linspace(0, 1, h)
    b = np.exp(-((t - 0.35) / 0.04) ** 2)[:, None, None]
    put("terracotta", img * (1 - 0.18 * b), -0.003 * b[..., 0].repeat(w, 1))
    h, w = size("clay_dark")
    put("clay_dark", grad(h, w, rgb("#6e4a36"), rgb("#4c3226")))
    # ---- basket: tan, a broad diagonal weave in the height and a faint matching shade
    h, w = size("basket")
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d1 = np.sin((xx + yy) / 9.0); d2 = np.sin((xx - yy) / 9.0)
    weave = np.where(((xx + yy) // 18 + (xx - yy) // 18) % 2 == 0, d1, d2)
    img = grad(h, w, rgb("#d8b47a"), rgb("#a77e4c")) * (0.95 + 0.05 * weave)[..., None]
    put("basket", img, 0.004 * weave)
    # ---- rice bundles: gold stalks in broad groups, the heads at the top lighter and bumpier
    h, w = size("rice")
    img = grad(h, w, rgb("#f0d27a"), rgb("#b48d44"), 0.8)
    hh = strands(h, w, 10, 11, 0.006)
    head = np.clip(1 - np.linspace(0, 1, h) / 0.35, 0, 1)[:, None]
    hh = hh + 0.004 * head * (np.sin(np.mgrid[0:h, 0:w][1] / 4.0) * 0.5 + 0.5)
    put("rice", img * (1 + 0.06 * head[..., None]), hh)
    # ---- fruit, leaves and the small goods: one colour each, soft gradient
    h, w = size("banana")
    img = grad(h, w, rgb("#f3d24e"), rgb("#d6a83a"))
    tip = np.clip((np.linspace(0, 1, h) - 0.9) / 0.1, 0, 1)[:, None, None]
    put("banana", img * (1 - tip) + rgb("#6b5a2a") * tip)
    put("banana_green", grad(*size("banana_green"), rgb("#a6c24e"), rgb("#76943a")))
    h, w = size("leaf")
    img = grad(h, w, rgb("#7fb04e"), rgb("#4e7e34"))
    rib = np.exp(-((np.linspace(-1, 1, w)) / 0.05) ** 2)[None, :, None]
    put("leaf", img * (1 - rib) + rgb("#c4d690") * rib, 0.003 * rib[..., 0].repeat(h, 0))
    put("areca", grad(*size("areca"), rgb("#f0a03a"), rgb("#c4702a")))
    put("betel", grad(*size("betel"), rgb("#4f8a3c"), rgb("#2f6030")))
    put("stone", grad(*size("stone"), rgb("#b7aea0"), rgb("#8a8278")) * (0.96 + 0.06 * smooth_noise(*size("stone"), 2, 21))[..., None])
    h, w = size("rope")
    put("rope", grad(h, w, rgb("#c8a86c"), rgb("#9c7e4a")), 0.002 * np.sin(np.mgrid[0:h, 0:w][1] / 3.0))
    h, w = size("ash")
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    r = np.sqrt((xx / w - 0.5) ** 2 + (yy / h - 0.5) ** 2)
    ember = np.clip(1 - r / 0.25, 0, 1)[..., None]
    put("ash", grad(h, w, rgb("#4a4440"), rgb("#2e2a28")) * (1 - ember) + rgb("#c25a2a") * ember)
    put("tuber", grad(*size("tuber"), rgb("#9a4a6a"), rgb("#6e3050")))
    put("greens", grad(*size("greens"), rgb("#8cc05a"), rgb("#5a8e3c")))
    put("cream", grad(*size("cream"), rgb("#f4ead2"), rgb("#d8c8a6")))
    put("red", grad(*size("red"), rgb("#b8392c"), rgb("#8a2620")))
    put("black", grad(*size("black"), rgb("#3a3230"), rgb("#221d1c")))
    put("turquoise", grad(*size("turquoise"), rgb("#4fb4a8"), rgb("#33867e")))
    return A, H


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "..", "..", "Logs", "payo", "pieces", "market")
    os.makedirs(out, exist_ok=True)
    A, H = paint()
    Image.fromarray((np.clip(A, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(out, "market_albedo.png"))
    rects = []
    for (x, y, w, h), metres in REGIONS.values():
        rects.append((x, y, x + w, y + h, h / metres))
    N = calm_glb.height_to_normal(H, rects, strength=1.0, blur=1.2)
    Image.fromarray(N).save(os.path.join(out, "market_normal.png"))
    calm_glb.save_height(H, os.path.join(out, "market_height.png"))
    json.dump({k: v[0] for k, v in REGIONS.items()}, open(os.path.join(out, "market_regions.json"), "w"), indent=1)
    print("PAINTED", out)


if __name__ == "__main__":
    main()
