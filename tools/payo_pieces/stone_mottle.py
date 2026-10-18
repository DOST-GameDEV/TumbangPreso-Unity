"""PAYO stone mottle: large soft splotches of a small hue offset over the stage stone's paint (owner, 2026-10-18, on the
fine grain: "the noise added is a bit too much ... they shouldnt be too fine. they should look like large splotches of
a small random hue offset ... some spots in the plain wall where its slightly greener, or redder than the main paint
color"). Replaces `stone_grain.py`'s fine grey grain on the stone.

The RGB is an OFFSET per channel around 0.5 (grey = no change): `TumbangPreso/LagoonPainted` with `_GrainHue` > 0
multiplies the paint by 1 + (tex * 2 - 1) * _GrainHue, box-projected at `_GrainScale` metres. Mostly plain, with
patches: a greener family, a redder (warm) family and a faint lighter/darker one, each a band of 1.6 to 5 m blobs whose
weak middle is cut away, so spots sit apart on plain paint. Built in the frequency domain, so it tiles exactly.

  py tools/payo_pieces/stone_mottle.py      writes Assets/TumbangPreso/Art/Payo/Textures/stone_mottle_albedo.png
"""
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/TumbangPreso/Art/Payo/Textures/stone_mottle_albedo.png"
N = 512
METRES = 10.0                      # the box size it is drawn at (the builder's StoneMottleMetres)


def band(rng, lo_m, hi_m):
    """Seamless noise holding wavelengths lo_m..hi_m metres, normalised to unit deviation."""
    f = np.fft.fftfreq(N)[:, None] ** 2 + np.fft.fftfreq(N)[None, :] ** 2
    f = np.sqrt(f) * N / METRES
    lo, hi = 1.0 / hi_m, 1.0 / lo_m
    mask = np.clip((f - lo * 0.7) / (lo * 0.6), 0, 1) * np.clip((hi * 1.3 - f) / (hi * 0.6), 0, 1)
    out = np.real(np.fft.ifft2(np.fft.fft2(rng.standard_normal((N, N))) * mask))
    return out / (out.std() + 1e-9)


def spots(rng, lo_m, hi_m, cut):
    """Blobs apart on plain ground: the field's weak middle cut away, the rest eased in."""
    n = band(rng, lo_m, hi_m)
    a = np.clip(np.abs(n) - cut, 0, None)
    a = np.clip(a / (np.percentile(a, 99.5) + 1e-9), 0, 1)
    return np.sign(n) * (a * a * (3 - 2 * a))           # smooth onset at the cut


def soften(a, sigma_m):
    """A seamless gaussian blur, sigma in metres."""
    f2 = np.fft.fftfreq(N)[:, None] ** 2 + np.fft.fftfreq(N)[None, :] ** 2
    s = sigma_m * N / METRES
    return np.real(np.fft.ifft2(np.fft.fft2(a) * np.exp(-2 * (np.pi * s) ** 2 * f2)))


def main():
    rng = np.random.default_rng(1018)
    green = np.clip(spots(rng, 1.6, 5.0, 0.55), 0, None)          # greener spots (moss-ish, not moss)
    warm = np.clip(spots(rng, 1.8, 5.0, 0.55), 0, None)           # redder spots
    tone = spots(rng, 2.5, 6.0, 0.3)                             # lighter and darker
    o = (green[..., None] * np.array([-0.55, 0.75, -0.25])
         + warm[..., None] * np.array([0.8, -0.2, -0.55])
         + tone[..., None] * 0.35)
    o = np.stack([soften(o[..., i], 0.45) for i in range(3)], -1)      # no edge anywhere: patches melt into the paint
    o /= np.percentile(np.abs(o), 99.7)
    img = (np.clip(0.5 + 0.5 * o, 0, 1) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(img, "RGB").save(OUT)
    print("wrote", OUT, "plain share %.2f" % float((np.abs(o).max(-1) < 0.05).mean()))


if __name__ == "__main__":
    main()
