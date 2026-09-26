"""Paint Lagoon Court's leaf cards, in Kanto's approved leaf style, for tropical plants.

  py -3 tools/author_lagoon_leaves.py            # paint every leaf
  py -3 tools/author_lagoon_leaves.py --sheet N  # the review sheet, tinted, vN

OWNER, 2026-09-27: "start texturing the plants", "i really like what was used for leaves in
kanto. just need to ensure it matches this environment". Kanto's leaf (`leaf()` in
tools/author_kanto_textures.py, docs/KANTO_DESIGN_GUIDE.md section 5) is the method, kept exactly:
  * ONE soft-painted greyscale drawing with a crisp ALPHA silhouette, tinted by the material
    (two tints a plant, light on top and dark underneath; variation per plant, never per leaf:
    per-leaf colour was tried in Kanto and reverted by the owner);
  * value rises smoothly from stem (dark) to tip (light), one soft sunlit patch, only a whisper
    of a midrib, NO outline ("sharply outlined and not softly painted" was rejected).
What changes for the tropics is the SHAPE, since a cove of coconut palms and banana plants is
not a street of round-leaved shade trees:
  leaf_round   Kanto's own rounded leaf, redrawn here at the same proportions: bushes, gumamela
  frond        a whole coconut frond: a gently bowed midrib with narrow leaflets combing off
               both sides toward the tip, a few gaps where leaflets have split (drawn along V,
               base at the bottom); the kit bends the card along V so the frond droops
  paddle       a banana/taro leaf: a broad oval, a pale soft midrib, soft parallel vein bands,
               and two tears from the edge (the banana look)
  blade        a grass/pandan blade: long, narrow, tapered, a soft centre fold
Greyscale RGBA PNGs to ArtSource/lagoon/textures/<name>_albedo.png.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "ArtSource" / "lagoon" / "textures"
SHEETS = ROOT / "Logs" / "lagoon-blender"


def _grid(w, h):
    v, u = np.mgrid[0:h, 0:w].astype(np.float32)
    return u / (w - 1), 1 - v / (h - 1)          # u across 0..1, v base 0 .. tip 1


def _save(name, g, alpha):
    OUT.mkdir(parents=True, exist_ok=True)
    g = np.clip(g * 0.92, 0, 1)
    rgba = np.dstack([g, g, g, np.clip(alpha, 0, 1)])
    Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").save(OUT / f"{name}_albedo.png")
    print("[lagoon-leaf]", name)


def _soft_tone(x, v, d, hl_at=(-0.25, 0.62)):
    """Kanto's leaf shading: stem dark to tip light, a soft sunlit patch, a faint midrib."""
    tone = 0.74 + 0.26 * np.clip(v, 0, 1) ** 0.8
    tone = tone + 0.12 * np.exp(-(((x - hl_at[0]) / 0.35) ** 2 + ((v - hl_at[1]) / 0.22) ** 2))
    tone = tone + 0.04 * np.exp(-(x / 0.05) ** 2) * (v > 0.12)
    tone = tone - 0.06 * np.clip(1 - d / 0.25, 0, 1) * (v < 0.5)
    return tone


def leaf_round(size=512):
    u, v = _grid(size, size)
    x = (u - 0.5) * 2
    half = 0.95 * np.clip(np.sin(np.pi * np.clip(v, 0, 1) ** 0.85), 0, 1) ** 0.6 * (1 - 0.25 * v)
    d = half - np.abs(x)
    _save("leaf_round", _soft_tone(x, v, d), d * size / 2.5)


def frond(w=512, h=1024, seed=3):
    """A coconut frond seen flat: a midrib from the base (bottom) to the tip, bowing a little,
    leaflets combing forward off both sides, longest at mid-frond."""
    rng = np.random.default_rng(seed)
    u, v = _grid(w, h)
    x = (u - 0.5) * 2                                       # -1..1 across
    vy = v * 2.0                                            # v in the same units as x (h = 2w)
    mid = 0.06 * np.sin(np.pi * v)                          # the midrib bows gently
    xr = x - mid
    alpha = np.zeros_like(x)
    tone = np.zeros_like(x)
    # Sheet v1 read as a fern: leaflets short and dense. A coconut frond's leaflets are LONG and
    # thin and comb out at ~40 degrees, reaching most of the way to the card edge.
    n = 22
    for side in (-1, 1):
        for k in range(n):
            t = 0.06 + 0.9 * k / n + rng.uniform(-0.006, 0.006)
            if rng.random() < 0.08:
                continue                                    # a split, where a leaflet tore off
            length = 0.98 * np.sin(np.pi * min(1.0, t * 1.02)) ** 0.5 * (1 - 0.3 * t)   # v2 1.25: tips cut flat at the card edge
            ang = np.radians(rng.uniform(36, 46))           # leaflets comb toward the tip
            dx, dy = side * np.cos(ang), np.sin(ang)
            px, py = xr, vy - t * 2.0
            along = px * dx + py * dy
            across = np.abs(px * dy - py * dx)
            s = np.clip(along / (length + 1e-6), 0, 1)
            width = 0.038 * np.sin(np.pi * s) ** 0.5 + 0.004
            a = np.clip((width - across) * w / 3.0, 0, 1) * ((along > 0) & (along < length))
            lt = 0.76 + 0.2 * s + 0.1 * np.exp(-((s - 0.55) / 0.2) ** 2)
            tone = np.where(a > alpha, lt, tone)
            alpha = np.maximum(alpha, a)
    rib = np.clip((0.022 * (1 - 0.7 * v) - np.abs(xr)) * w / 3.0, 0, 1) * (v < 0.985)
    tone = np.where(rib > alpha * 0.5, 0.82 + 0.08 * v, tone)
    alpha = np.maximum(alpha, rib)
    _save("frond", tone, alpha)


def paddle(w=512, h=1024, seed=5):
    """A banana leaf: LONG oval (sheet v1's round paddle read as a lily pad), a soft pale midrib,
    soft vein bands, and two TEARS from the edge toward the midrib, wide at the edge and closing
    (v1's hairline tears read as scratches)."""
    rng = np.random.default_rng(seed)
    u, v = _grid(w, h)
    x = (u - 0.5) * 2
    half = 0.94 * np.clip(np.sin(np.pi * np.clip(v, 0, 1)), 0, 1) ** 0.4
    d = half - np.abs(x)
    alpha = d * w / 2.5
    for side, t0 in ((1, rng.uniform(0.3, 0.42)), (-1, rng.uniform(0.55, 0.68))):
        vein_v = t0 + np.abs(x) * 0.12
        gap = 0.012 * np.clip((np.abs(x) - 0.12) / 0.8, 0, 1)
        tear = (np.abs(v - vein_v) < gap) & (x * side > 0.12)
        alpha = np.where(tear, 0, alpha)
    tone = _soft_tone(x, v, d, hl_at=(-0.3, 0.55))
    tone = tone + 0.07 * np.exp(-(x / 0.03) ** 2) * (v > 0.03)             # the pale midrib
    veins = np.sin((v - np.abs(x) * 0.12) * np.pi * 40)
    tone = tone + 0.025 * np.clip(veins, 0, 1) ** 4 * (np.abs(x) > 0.05)    # soft vein bands
    _save("paddle", tone, alpha)


def blade(w=256, h=1024):
    u, v = _grid(w, h)
    x = (u - 0.5) * 2
    half = 0.9 * (1 - v) ** 0.7 * np.clip(v * 8, 0, 1) ** 0.3
    d = half - np.abs(x)
    tone = 0.72 + 0.28 * v ** 0.9 - 0.05 * (x > 0)                          # a soft centre fold
    _save("blade", tone, d * w / 3.0)


# Tints: (light, dark) per plant. Sunnier and more saturated than Kanto's street greens
# (0.30, 0.64, 0.05 / 0.10, 0.33, 0.04), leaning yellow-green in the light and deeper green in
# the shade, to sit against the turquoise water and tan rock. Nothing near defence blue.
TINTS = {
    "palm":   ((0.46, 0.70, 0.14), (0.10, 0.36, 0.12)),
    "banana": ((0.56, 0.76, 0.18), (0.18, 0.44, 0.10)),
    "taro":   ((0.30, 0.60, 0.16), (0.08, 0.30, 0.12)),
    "bush":   ((0.34, 0.62, 0.10), (0.10, 0.32, 0.08)),
    "grass":  ((0.50, 0.70, 0.18), (0.20, 0.44, 0.12)),
}


def sheet(version):
    """Each leaf on a sand-coloured card, tinted light and dark for its plant."""
    rows = [("leaf_round", "bush"), ("frond", "palm"), ("paddle", "banana"), ("paddle", "taro"), ("blade", "grass")]
    cw, ch, pad, head = 260, 420, 14, 34
    page = Image.new("RGB", (pad + len(rows) * 2 * (cw + pad), ch + head + 2 * pad), (232, 214, 170))
    d = ImageDraw.Draw(page)
    try:
        font = ImageFont.truetype("arial.ttf", 16)
    except OSError:
        font = ImageFont.load_default()
    x = pad
    for leaf, plant in rows:
        img = Image.open(OUT / f"{leaf}_albedo.png").convert("RGBA")
        g = np.asarray(img).astype(np.float32) / 255
        for which, tint in zip(("light", "dark"), TINTS[plant]):
            rgb = g[..., :3] * np.array([min(1.0, c * 1.25) for c in tint])
            tinted = Image.fromarray((np.dstack([rgb, g[..., 3]]) * 255).astype(np.uint8), "RGBA")
            tinted.thumbnail((cw, ch))
            page.paste(tinted, (x + (cw - tinted.width) // 2, head + pad), tinted)
            d.text((x, 8), f"{leaf} / {plant} {which}", fill=(40, 36, 32), font=font)
            x += cw + pad
    out = SHEETS / f"leaves_swatches_v{version}.png"
    page.save(out)
    print("[lagoon-leaf] sheet", out)


def main():
    if "--sheet" in sys.argv:
        sheet(int(sys.argv[sys.argv.index("--sheet") + 1]))
        return
    leaf_round()
    frond()
    paddle()
    blade()


if __name__ == "__main__":
    main()
