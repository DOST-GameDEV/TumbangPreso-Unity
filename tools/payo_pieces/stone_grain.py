"""PAYO stone grain: a seamless fine grain for the stage stone's detail layer (owner, 2026-10-18: "the textures look low
res"). The blocks' painted atlases carry 35 to 46 pixels a metre (the shafts 7), so up close their joints and motifs go
soft; this grain is sampled box-projected at about 1.4 m (and again finer) by `TumbangPreso/LagoonPainted`'s `_Grain`,
multiplying the paint by a few per cent so the surface reads crisp at any distance while the approved paint stays.

Neutral grey around 0.5: calm grain (6 to 60 mm), small pits, a few mineral specks and a faint mid-scale mottle. Built from
filtered noise in the frequency domain, so it tiles exactly.

  py tools/payo_pieces/stone_grain.py      writes Assets/TumbangPreso/Art/Payo/Textures/stone_grain_albedo.png
"""
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/TumbangPreso/Art/Payo/Textures/stone_grain_albedo.png"
N = 1024
METRES = 1.4                       # the box size it is drawn at (the shader's _GrainScale)


def band(rng, lo_m, hi_m):
    """Seamless noise holding wavelengths lo_m..hi_m metres, normalised to unit deviation."""
    f = np.fft.fftfreq(N)[:, None] ** 2 + np.fft.fftfreq(N)[None, :] ** 2
    f = np.sqrt(f) * N / METRES                     # cycles per metre
    lo, hi = 1.0 / hi_m, 1.0 / lo_m
    mask = np.clip((f - lo * 0.7) / (lo * 0.6), 0, 1) * np.clip((hi * 1.3 - f) / (hi * 0.6), 0, 1)
    spec = np.fft.fft2(rng.standard_normal((N, N))) * mask
    out = np.real(np.fft.ifft2(spec))
    return out / (out.std() + 1e-9)


def main():
    rng = np.random.default_rng(1018)
    g = 0.30 * band(rng, 0.006, 0.02)               # fine grain
    g += 0.35 * band(rng, 0.02, 0.06)               # coarse grain
    g += 0.40 * band(rng, 0.1, 0.4)                 # a soft mottle
    # pits: the darkest tail of a fine field, sharpened
    p = band(rng, 0.008, 0.02)
    g -= 1.6 * np.clip(p - 2.2, 0, None)
    # specks: the brightest tail of another
    s = band(rng, 0.006, 0.015)
    g += 1.2 * np.clip(s - 2.4, 0, None)
    g = 0.5 + 0.11 * g / (g.std() + 1e-9)
    img = (np.clip(g, 0, 1) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(img, "L").convert("RGB").save(OUT)
    print("wrote", OUT, "mean %.3f std %.3f" % (g.mean(), g.std()))


if __name__ == "__main__":
    main()
