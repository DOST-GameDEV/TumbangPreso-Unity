"""Paint the street kit's textures for the Ilalim ng Tulay rebuild (ILALIM-1.3, streets and ground).

  py -3 tools/author_ilalim_textures_street.py [--sheet N]     # paint everything + swatch sheet N
  py -3 tools/author_ilalim_textures_street.py --normals-only  # rebuild normals from painted heights

Writes ArtSource/ilalim/textures/street_<name>_albedo.png (and _height.png / _normal.png for the
surfaces that have relief), and Logs/ilalim-blender/street_swatches_vN.png for review. The
models (tools/author_ilalim_street.py) map them at world scale; every file says its size in
metres in TEX below, and the Blender material scales the UVs to match.

THE STYLE is the house style (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md section 2,
tools/author_ilalim_textures.py): FLAT fills, a few LARGE patches with FEATHERED organic edges,
low contrast, no grain, no noise, no streaks, no airbrushed blur. Every surface is its OWN
drawing; nothing here reuses another surface's generator:
  * asphalt: warm mid grey, broad sun-faded patches, cut-in repair patches drawn as wobbly
    rounded shapes, a few oil stains as soft lobed blobs. 8 m tile. Desaturated on purpose:
    inside the chalk box it is the ability floor.
  * pavement: Taft's big grey concrete slabs (research photo at the Padre Faura corner), each
    slab its own tone, wobbly joints, one or two stained slabs.
  * sidewalk: the side streets' small brick-red square tiles (Padre Faura photos), with grey
    cement patches where tiles broke. Muted brick, well clear of offence orange.
  * kerb_white: Taft's kerb paint (it is the east and west chalk), a STRIP drawn across the
    kerb's profile: the road face carries the gutter's mud band, the top has foot scuffs.
  * kerb_stripes: the side streets' yellow and black no-parking kerb, 1 m blocks.
  * median_wall: Taft's median planter wall, dark green paint over concrete, a STRIP by
    height: mud splash at the foot, rain tongues from the coping, chips.
  * coping, soil, lawn, lot, parking, drive: one drawing each.
  * paint: road-marking paint (neutral, tinted white, yellow or red per material), worn
    through in soft patches. chalk: the chalk box's powdery lines.
  * pothole: one decal, the dark hole with its ragged rim, fading into the asphalt.
  * steel (neutral, tinted per material), concrete_pole (with torn poster scraps, a very
    Manila detail), wall (campus wall STRIP by height, with a repainted patch where graffiti
    was covered).
  * signs, painted flat: the green street-name blades (TAFT AVE., PADRE FAURA ST.), ONE WAY,
    the MMDA "BAWAL TUMAWID" median sign, the SAKAYAN bus-stop panel, the signal's countdown
    digits and a hand-painted yellow and red barangay welcome board. The barangay and every
    name on it are INVENTED.
Colours: nothing near offence orange #f87020 or defence blue #0080e8 (Art_Direction.md section 1).
The Manila intersection red is a dull brick red, the blades a deep green.
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "ArtSource" / "ilalim" / "textures"
SHEETS = ROOT / "Logs" / "ilalim-blender"
FONTS = Path("C:/Windows/Fonts")

# name: (width m, height m, pixels wide) ; every tile is periodic in both directions.
TEX = {
    "asphalt": (8, 8, 1024), "pavement": (4, 4, 1024), "sidewalk": (4, 4, 1024),
    "kerb_white": (4, 1, 1024), "kerb_stripes": (4, 1, 1024), "median_wall": (4, 0.5, 1024),
    "coping": (4, 4, 512), "soil": (4, 4, 512), "lawn": (8, 8, 1024), "lot": (8, 8, 1024),
    "parking": (8, 8, 1024), "drive": (4, 4, 512), "paint": (4, 4, 512), "chalk": (4, 4, 512),
    "steel": (4, 4, 512), "concrete_pole": (2, 4, 512), "wall": (4, 3, 1024),
}
STRENGTH = {"asphalt": 0.6, "pavement": 0.9, "sidewalk": 0.9, "drive": 0.7}


def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


class Canvas:
    """A periodic canvas W x H metres. `u`, `v` are metre coordinates of every pixel."""

    def __init__(self, name):
        self.name = name
        self.wm, self.hm, w = TEX[name]
        self.px = w / self.wm
        self.w, self.h = w, int(round(self.hm * self.px))
        self.v, self.u = np.mgrid[0:self.h, 0:self.w] / self.px
        self.height = np.zeros((self.h, self.w))

    def smooth(self, scale_m, seed, stretch=(1.0, 1.0)):
        white = np.random.default_rng(seed).standard_normal((self.h, self.w))
        fy = np.fft.fftfreq(self.h)[:, None] * self.px
        fx = np.fft.fftfreq(self.w)[None, :] * self.px
        # frequencies are now in cycles per metre
        n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-((fx * stretch[0]) ** 2 + (fy * stretch[1]) ** 2)
                                                              * (scale_m ** 2) * 2)))
        return (n - n.mean()) / (n.std() + 1e-9)

    def mask(self, scale_m, coverage, seed, feather=0.5, stretch=(1.0, 1.0)):
        n = self.smooth(scale_m, seed, stretch) + 0.12 * self.smooth(scale_m / 2.5, seed + 1, stretch)
        n = (n - n.mean()) / n.std()
        edge = np.quantile(n, 1 - coverage)
        m = np.clip((n - edge) / feather + 0.5, 0, 1)
        return m * m * (3 - 2 * m)

    def pdist(self, cx, cy):
        """Periodic offsets from a point, in metres."""
        du = (self.u - cx + self.wm / 2) % self.wm - self.wm / 2
        dv = (self.v - cy + self.hm / 2) % self.hm - self.hm / 2
        return du, dv

    def blob(self, cx, cy, r, seed, lobes=3, wobble=0.18, feather=0.03, aspect=1.0, rot=0.0):
        """A drawn organic shape: a flat fill whose outline wobbles, with a narrow soft edge."""
        du, dv = self.pdist(cx, cy)
        c, s = math.cos(rot), math.sin(rot)
        x, y = (du * c + dv * s) / aspect, -du * s + dv * c
        ang = np.arctan2(y, x)
        rng = np.random.default_rng(seed)
        rr = r * (1 + sum(wobble / (k + 1) * np.sin((k + 2) * ang + rng.uniform(0, 6.28)) for k in range(lobes)))
        d = np.hypot(x, y)
        return np.clip((rr - d) / feather + 0.5, 0, 1)

    def rrect(self, cx, cy, hw, hh, r, seed, wobble=0.03, feather=0.02, rot=0.0):
        """A wobbly rounded rectangle, drawn by hand: its sides bow by up to `wobble` m."""
        du, dv = self.pdist(cx, cy)
        c, s = math.cos(rot), math.sin(rot)
        x, y = du * c + dv * s, -du * s + dv * c
        rng = np.random.default_rng(seed)
        bx = hw + wobble * np.sin(y * rng.uniform(2, 4) + rng.uniform(0, 6))
        by = hh + wobble * np.sin(x * rng.uniform(2, 4) + rng.uniform(0, 6))
        qx, qy = np.abs(x) - bx + r, np.abs(y) - by + r
        d = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
        return np.clip(-d / feather + 0.5, 0, 1)

    def wobbly_grid(self, step_u, step_v, width, wobble, seed, off_u=0.0, off_v=0.0):
        """Hand-drawn joints every step metres in u and v; each line drifts and swells a little."""
        out = np.zeros((self.h, self.w))
        for axis, step, off in (("u", step_u, off_u), ("v", step_v, off_v)):
            if not step:
                continue
            along, across = (self.v, self.u) if axis == "u" else (self.u, self.v)
            n_al = self.h if axis == "u" else self.w
            sm = self.smooth(0.7, seed + (0 if axis == "u" else 50))
            line = sm[:, 0] if axis == "u" else sm[0, :]
            idx = (along * self.px).astype(int) % n_al
            drift = line[idx] * wobble
            w = width * (1 + 0.3 * line[(idx + n_al // 3) % n_al])
            d = (across - off - drift) % step
            d = np.minimum(d, step - d)
            out = np.maximum(out, np.clip((w - d) / (width * 0.6) + 0.5, 0, 1))
        return out


def apply(img, m, colour):
    """Paint `colour` (an RGB multiplier if a tuple of 3 floats near 1, else an absolute colour)."""
    c = np.asarray(colour, dtype=float)
    return img * (1 - m[..., None]) + c * m[..., None]


def mult(img, m, factor):
    return img * (1 + (np.asarray(factor, dtype=float) - 1) * m[..., None])


def normal_from_height(h, strength):
    gy, gx = np.gradient(h * strength * 8)
    n = np.dstack((-gx, gy, np.ones_like(h)))
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def save(cv, img):
    """Canvas row 0 is v = 0, which Blender reads from the image's BOTTOM row: flip on save, so
    a strip's foot (a wall's splash band, a kerb's road face) lands at the foot of the model."""
    OUT.mkdir(parents=True, exist_ok=True)
    name = "street_" + cv.name
    img = np.flipud(img)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    if cv.name in STRENGTH:
        h = np.flipud(cv.height)
        span = np.ptp(h)
        h = (h - h.min()) / span if span > 1e-9 else np.full_like(h, 0.5)
        Image.fromarray((h * 255).astype(np.uint8)).save(OUT / f"{name}_height.png")
        Image.fromarray((normal_from_height(h, STRENGTH[cv.name]) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
    print("[street-tex]", name)


# ------------------------------------------------------------------ ground

def asphalt():
    cv = Canvas("asphalt")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("6c6a66")
    img = mult(img, cv.mask(2.6, 0.30, 11, feather=1.3), (1.07, 1.07, 1.065))     # sun-faded
    img = mult(img, cv.mask(1.8, 0.18, 12, feather=1.2), (0.95, 0.95, 0.95))
    rng = np.random.default_rng(13)
    # Cut-in repairs: darker, fresher asphalt in wobbly rounded rectangles, with a pale rim of
    # sealant. Two in a tile of 8 m, never lined up.
    for k, (cx, cy, hw, hh, rot) in enumerate(((1.6, 2.2, 0.9, 0.55, 0.08), (5.7, 6.1, 0.6, 1.1, -0.05))):
        m = cv.rrect(cx, cy, hw, hh, 0.12, seed=20 + k, wobble=0.04, rot=rot)
        rim = np.clip(cv.rrect(cx, cy, hw + 0.035, hh + 0.035, 0.14, seed=20 + k, wobble=0.04, rot=rot) - m, 0, 1)
        img = mult(img, rim, (1.10, 1.10, 1.09))
        img = mult(img, m, (0.9, 0.9, 0.905))
        cv.height += 0.4 * m
    # Oil stains: soft lobed blobs, a big one and its drips, where vehicles idle.
    for k, (cx, cy, r) in enumerate(((3.9, 4.3, 0.34), (4.3, 4.0, 0.16), (6.8, 1.4, 0.26), (0.6, 6.9, 0.2))):
        img = mult(img, cv.blob(cx, cy, r, seed=30 + k, aspect=1.4, rot=rng.uniform(0, 3), feather=0.06),
                   (0.84, 0.84, 0.85))
    save(cv, img)


def pavement():
    """Taft's pavement: 1.33 m grey concrete slabs, each its own tone."""
    cv = Canvas("pavement")
    step = 4 / 3
    iu, iv = (cv.u // step).astype(int), (cv.v // step).astype(int)
    rng = np.random.default_rng(41)
    tone = rng.choice([0.965, 0.99, 1.0, 1.015, 1.035], size=(3, 3))
    tone[1, 2], tone[2, 0] = 0.91, 0.94                    # two stained slabs
    img = np.ones((cv.h, cv.w, 3)) * hexcol("b4afa6") * tone[iv % 3, iu % 3][..., None]
    img = mult(img, cv.mask(1.4, 0.25, 42, feather=1.2), (0.965, 0.962, 0.96))
    # one replaced slab, a warmer, smoother pour
    m = cv.rrect(step * 0.5, step * 2.5, step / 2 - 0.03, step / 2 - 0.03, 0.05, seed=43, wobble=0.01)
    img = mult(img, m, (1.04, 1.03, 1.0))
    joints = cv.wobbly_grid(step, step, 0.012, 0.012, seed=44)
    img = mult(img, joints, (0.78, 0.77, 0.76))
    cv.height = -joints
    save(cv, img)


def sidewalk():
    """The side streets' small square tiles in muted brick red, some replaced by grey cement."""
    cv = Canvas("sidewalk")
    step = 0.4
    n = int(round(4 / step))
    iu, iv = (cv.u // step).astype(int) % n, (cv.v // step).astype(int) % n
    rng = np.random.default_rng(51)
    tone = rng.choice([0.95, 0.98, 1.0, 1.02, 1.05], size=(n, n))
    img = np.ones((cv.h, cv.w, 3)) * hexcol("8e6457") * tone[iv, iu][..., None]
    img = mult(img, cv.mask(1.2, 0.3, 52, feather=1.2), (1.05, 1.06, 1.07))      # dusty
    grout = cv.wobbly_grid(step, step, 0.008, 0.006, seed=53)
    img = apply(img, grout * 0.85, hexcol("8f7a70"))
    cv.height = -grout
    # Broken tiles replaced with grey cement: whole tiles (one to three), trowelled a little
    # past their edges, never a cloud shape.
    for k, (i0, j0, ni, nj) in enumerate(((2, 2, 2, 1), (6, 7, 1, 2), (8, 3, 1, 1))):
        m = cv.rrect((i0 + ni / 2) * step, (j0 + nj / 2) * step, ni * step / 2 + 0.01, nj * step / 2 + 0.01,
                     0.03, seed=54 + k, wobble=0.012, feather=0.012)
        img = apply(img, m, hexcol("9b9891"))
        cv.height = cv.height * (1 - m)
    save(cv, img)


def kerb(name, stripes):
    """A strip across the kerb profile: v 0..0.25 m is the road face (v 0.1 is road level),
    v 0.25..0.63 the top, the rest the back face tucked into the pavement."""
    cv = Canvas(name)
    if stripes:
        wob = 0.02 * cv.smooth(0.3, 61)[:, :1]
        yellow = ((cv.u + wob) % 2.0) < 1.0
        img = np.where(yellow[..., None], hexcol("d6b240"), hexcol("2d2c2a"))
    else:
        img = np.ones((cv.h, cv.w, 3)) * hexcol("e9e7e0")
    img = mult(img, cv.mask(0.8, 0.25, 62, feather=1.0), (0.96, 0.96, 0.95))
    wear = cv.mask(0.4, 0.06, 63, feather=0.5)
    img = apply(img, wear * 0.75, hexcol("b3aea2"))                       # paint worn to concrete
    # the gutter's mud band on the road face, its top wobbling
    top = 0.15 + 0.03 * cv.smooth(0.5, 64)[:1, :]
    band = np.clip((top - cv.v) / 0.025 + 0.5, 0, 1) * (cv.v > 0.02)
    img = mult(img, band, (0.72, 0.69, 0.64))
    # faint joints between the 1 m kerb stones, drawn across the whole profile
    img = mult(img, cv.wobbly_grid(1.0, None, 0.008, 0.006, seed=66, off_u=0.3), (0.84, 0.83, 0.81))
    # the top is walked on: one broad, very soft greying band along its middle
    walk = np.exp(-((cv.v - 0.45) / 0.12) ** 2) * (0.6 + 0.4 * cv.mask(1.2, 0.5, 65, feather=1.5))
    img = mult(img, walk, (0.95, 0.945, 0.935))
    save(cv, img)


def median_wall():
    cv = Canvas("median_wall")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("35533b")
    img = mult(img, cv.mask(0.9, 0.3, 71, feather=1.0, stretch=(1, 3)), (1.10, 1.09, 1.07))   # repaint
    # mud splash up the foot, wobbling
    top = 0.10 + 0.025 * cv.smooth(0.4, 73)[:1, :]
    band = np.clip((top - cv.v) / 0.02 + 0.5, 0, 1)
    img = mult(img, band, (0.78, 0.74, 0.66))
    # rain tongues down from the coping (the top of the strip)
    rng = np.random.default_rng(74)
    for k in range(4):
        u0, wdt, L = rng.uniform(0, 4), rng.uniform(0.12, 0.25), rng.uniform(0.10, 0.2)
        du = np.abs((cv.u - u0 + 2) % 4 - 2)
        down = cv.hm - cv.v
        half = wdt * np.clip(1 - down / L, 0, 1) ** 0.5
        m = np.clip((half - du) / 0.012 + 0.5, 0, 1) * (down < L)
        img = mult(img, m * rng.uniform(0.5, 0.8), (0.86, 0.86, 0.84))
    save(cv, img)


def coping():
    cv = Canvas("coping")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("bab3a4")
    img = mult(img, cv.mask(1.1, 0.3, 81, feather=1.2), (0.95, 0.945, 0.94))
    save(cv, img)


def soil():
    cv = Canvas("soil")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("4f3e30")
    img = mult(img, cv.mask(0.9, 0.3, 91, feather=1.0), (1.18, 1.16, 1.12))      # dry crust
    rng = np.random.default_rng(92)
    for k in range(26):                                                         # clods
        m = cv.blob(rng.uniform(0, 4), rng.uniform(0, 4), rng.uniform(0.03, 0.06), seed=93 + k, feather=0.01)
        img = mult(img, m, (1.28, 1.24, 1.18))
    save(cv, img)


def lawn():
    cv = Canvas("lawn")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("627a41")
    img = mult(img, cv.mask(2.2, 0.3, 101, feather=1.3), (1.14, 1.12, 1.02))     # sunlit, drier
    img = mult(img, cv.mask(1.6, 0.2, 102, feather=1.1), (0.86, 0.9, 0.86))     # lush, shaded
    bare = cv.mask(1.2, 0.03, 103, feather=0.7)
    img = apply(img, bare * 0.8, hexcol("7d6f4f"))                                    # worn to earth
    save(cv, img)


def lot():
    cv = Canvas("lot")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("a9a397")
    img = mult(img, cv.mask(2.8, 0.3, 111, feather=1.6), (1.03, 1.03, 1.025))
    img = mult(img, cv.mask(1.8, 0.15, 112, feather=1.4), (0.965, 0.96, 0.95))
    save(cv, img)


def parking():
    cv = Canvas("parking")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("7b7872")
    img = mult(img, cv.mask(3.0, 0.35, 121, feather=1.4), (1.07, 1.065, 1.06))
    rng = np.random.default_rng(122)
    # oil drips where cars stand, in two loose rows 5 m apart
    for k in range(7):
        cx, cy = rng.uniform(0, 8), (k % 2) * 4.0 + 1.5 + rng.uniform(-0.3, 0.3)
        img = mult(img, cv.blob(cx, cy, rng.uniform(0.15, 0.3), seed=123 + k, aspect=1.6, feather=0.05),
                   (0.85, 0.85, 0.86))
    save(cv, img)


def drive():
    cv = Canvas("drive")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("bdb7ab")
    img = mult(img, cv.mask(1.3, 0.28, 131, feather=1.2), (0.955, 0.95, 0.945))
    joints = cv.wobbly_grid(4.0, 2.0, 0.01, 0.015, seed=132, off_u=1.3, off_v=0.4)
    img = mult(img, joints, (0.8, 0.79, 0.78))
    cv.height = -joints
    save(cv, img)


def paint():
    cv = Canvas("paint")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("f1f0ea")
    img = mult(img, cv.mask(0.8, 0.3, 141, feather=1.0), (0.94, 0.94, 0.93))
    worn = cv.mask(0.45, 0.07, 142, feather=0.45)
    img = apply(img, worn * 0.8, hexcol("b9b8b2"))               # worn thin toward the asphalt
    save(cv, img)


def chalk():
    cv = Canvas("chalk")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("f3f2ec")
    img = mult(img, cv.mask(0.5, 0.3, 151, feather=1.0), (0.94, 0.94, 0.93))      # thinner strokes
    img = apply(img, cv.mask(0.3, 0.04, 152, feather=0.5) * 0.7, hexcol("c9c8c2"))  # scuffed through
    save(cv, img)


def steel():
    cv = Canvas("steel")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("dedede")
    img = mult(img, cv.mask(1.0, 0.25, 161, feather=1.2), (1.03, 1.03, 1.03))
    img = mult(img, cv.mask(1.3, 0.2, 162, feather=1.2), (0.965, 0.965, 0.965))
    img = mult(img, cv.mask(0.12, 0.006, 163, feather=0.6), (0.88, 0.83, 0.76))    # chips to rust
    img = mult(img, cv.mask(0.5, 0.03, 164, feather=0.8, stretch=(1, 0.3)), (0.95, 0.92, 0.88))    # rust bleed
    save(cv, img)


def concrete_pole():
    cv = Canvas("concrete_pole")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("b6b2a8")
    img = mult(img, cv.mask(0.8, 0.3, 171, feather=1.2, stretch=(1, 0.4)), (0.95, 0.945, 0.94))
    # Torn poster scraps, pasted and peeled: faded tones, feathered ragged edges.
    rng = np.random.default_rng(172)
    for k, colour in enumerate(("e8dca6", "e4c7c4", "efece4")):
        m = cv.rrect(rng.uniform(0, 2), rng.uniform(0.6, 3.4), rng.uniform(0.12, 0.2), rng.uniform(0.15, 0.3),
                     0.02, seed=173 + k, wobble=0.035, rot=rng.uniform(-0.2, 0.2))
        m *= 1 - cv.mask(0.08, 0.3, 180 + k, feather=0.3)          # torn away in places
        img = apply(img, m * 0.85, hexcol(colour))
    save(cv, img)


def wall():
    cv = Canvas("wall")
    img = np.ones((cv.h, cv.w, 3)) * hexcol("d6cbb2")
    img = mult(img, cv.mask(1.2, 0.3, 191, feather=1.2), (0.96, 0.955, 0.945))
    # a repainted patch where graffiti was covered: fresher, a touch paler, hard-ish edge
    m = cv.rrect(2.6, 1.2, 0.7, 0.45, 0.05, seed=192, wobble=0.05)
    img = mult(img, m, (1.05, 1.05, 1.04))
    top = 0.35 + 0.08 * cv.smooth(0.6, 193)[:1, :]
    band = np.clip((top - cv.v) / 0.05 + 0.5, 0, 1)
    img = mult(img, band, (0.80, 0.77, 0.71))
    # A few broad rain tongues from the coping, rounded at the end (the LRT kit's drip shape,
    # drawn fatter and fewer for a 3 m wall).
    rng = np.random.default_rng(194)
    for k in range(5):
        u0, wdt, L = rng.uniform(0, 4), rng.uniform(0.18, 0.35), rng.uniform(0.4, 1.1)
        du = np.abs((cv.u - u0 + 2) % 4 - 2)
        down = cv.hm - cv.v
        half = wdt * (0.55 + 0.45 * np.clip(1 - down / L, 0, 1))
        end = np.sqrt(np.clip(1 - ((down - (L - wdt)) / wdt) ** 2, 0, 1))
        half = np.where(down > L - wdt, half * end, half)
        mm = np.clip((half - du) / 0.03 + 0.5, 0, 1) * (down < L)
        img = mult(img, mm * rng.uniform(0.5, 0.8), (0.87, 0.86, 0.83))
    save(cv, img)


def gutter():
    """A MULTIPLIER overlay (white = clean) for the road's second UV map, UVGrime: u runs along
    the kerb (8 m), v is metres out from the kerb face (2 m), row 0 at the kerb. Silt and damp
    collect in a wobbling band against the kerb, with a few broad lobes reaching into the lane
    where water pools. Big drawn shapes, low contrast."""
    W_M, H_M, ppm = 8.0, 2.0, 128
    w, h = int(W_M * ppm), int(H_M * ppm)
    cv = Canvas.__new__(Canvas)
    cv.name, cv.wm, cv.hm, cv.px, cv.w, cv.h = "gutter", W_M, H_M, ppm, w, h
    cv.v, cv.u = np.mgrid[0:h, 0:w] / ppm
    a = np.ones((h, w, 3))
    edge = 0.32 + 0.08 * cv.smooth(0.9, 301)[:1, :]
    band = np.clip((edge - cv.v) / 0.05 + 0.5, 0, 1)
    a = mult(a, band, (0.80, 0.78, 0.74))
    rng = np.random.default_rng(302)
    for k in range(4):
        m = cv.blob(rng.uniform(0, 8), 0.2, rng.uniform(0.4, 0.7), seed=303 + k, aspect=2.4, feather=0.06)
        a = mult(a, m * (cv.v > 0.15), (0.88, 0.87, 0.86))
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.flipud(np.clip(a, 0, 1)) * 255).astype(np.uint8)).save(OUT / "street_gutter.png")
    print("[street-tex] street_gutter")


# ------------------------------------------------------------------ decals and signs (UV 0..1)

def pothole():
    """A decal 1.3 m across: asphalt at its edge, the ragged dark hole and a pale rim inside."""
    s = 512
    cv = Canvas.__new__(Canvas)
    cv.name, cv.wm, cv.hm, cv.px, cv.w, cv.h = "pothole", 1.3, 1.3, s / 1.3, s, s
    cv.v, cv.u = np.mgrid[0:s, 0:s] / cv.px
    img = np.ones((s, s, 3)) * hexcol("6c6a66")
    rim = cv.blob(0.65, 0.65, 0.5, seed=201, lobes=4, wobble=0.12, feather=0.03, aspect=1.25)
    hole = cv.blob(0.66, 0.64, 0.4, seed=202, lobes=4, wobble=0.16, feather=0.02, aspect=1.3)
    img = mult(img, rim, (1.13, 1.12, 1.1))
    img = apply(img, hole, hexcol("3a3835"))
    grit = cv.blob(0.55, 0.7, 0.14, seed=203, feather=0.02) + cv.blob(0.78, 0.58, 0.1, seed=204, feather=0.02)
    img = mult(img, np.clip(grit, 0, 1) * hole, (1.35, 1.33, 1.3))
    img = mult(img, cv.blob(0.72, 0.6, 0.16, seed=205, feather=0.03) * hole, (0.8, 0.8, 0.85))   # puddle-dark
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT / "street_pothole_albedo.png")
    print("[street-tex] street_pothole")


def font(names, size):
    for n in names:
        p = FONTS / n
        if p.exists():
            return ImageFont.truetype(str(p), size)
    return ImageFont.load_default()


def fit_text(draw, text, fnames, box_w, box_h):
    size = box_h
    while size > 8:
        f = font(fnames, size)
        l, t, r, b = draw.textbbox((0, 0), text, font=f)
        if r - l <= box_w and b - t <= box_h:
            return f, (l, t, r, b)
        size -= 2
    return font(fnames, 8), draw.textbbox((0, 0), text, font=font(fnames, 8))


def centred(draw, text, fnames, cx, cy, box_w, box_h, fill):
    f, (l, t, r, b) = fit_text(draw, text, fnames, box_w, box_h)
    draw.text((cx - (l + r) / 2, cy - (t + b) / 2), text, font=f, fill=fill)


def wear(img, seed, amount, colour):
    """Soft feathered sun-fade patches over a sign face, drawn, not noise."""
    a = np.asarray(img, dtype=float) / 255
    h, w = a.shape[:2]
    white = np.random.default_rng(seed).standard_normal((h, w))
    fy, fx = np.fft.fftfreq(h)[:, None], np.fft.fftfreq(w)[None, :]
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * (w / 5) ** 2)))
    n = (n - n.mean()) / n.std()
    m = np.clip((n - 0.8) / 0.6 + 0.5, 0, 1)
    a = a * (1 - amount * m[..., None]) + hexcol(colour) * amount * m[..., None]
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))


def sign_save(img, name):
    OUT.mkdir(parents=True, exist_ok=True)
    img.save(OUT / f"street_{name}_albedo.png")
    print("[street-tex] street_" + name)


BOLD = ["arialbd.ttf", "ARIALNB.TTF"]
NARROW = ["ARIALNB.TTF", "arialbd.ttf"]
BLACK = ["ariblk.ttf", "impact.ttf", "arialbd.ttf"]


def blade(name, text):
    """The green street-name blade, 1.6 x 0.4 m: deep green, rounded white border, white caps."""
    W, H = 1024, 256
    img = Image.new("RGB", (W, H), "#1d4f36")
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((14, 14, W - 15, H - 15), radius=26, outline="#eeeee6", width=11)
    centred(d, text, NARROW, W / 2, H / 2 + 4, W - 110, 150, "#eeeee6")
    sign_save(wear(img, 211 + len(text), 0.25, "5f8f73"), name)


def one_way():
    W, H = 1024, 384
    img = Image.new("RGB", (W, H), "#202020")
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((10, 10, W - 11, H - 11), radius=22, outline="#e9e8e2", width=9)
    # the arrow points to +u (the models mirror it for signs that must point the other way)
    d.polygon([(70, 130), (720, 130), (720, 70), (960, 192), (720, 314), (720, 254), (70, 254)], fill="#e9e8e2")
    centred(d, "ONE WAY", BOLD, 395, 192, 580, 100, "#202020")
    sign_save(wear(img, 221, 0.18, "5a5a58"), "sign_oneway")


def bawal():
    """MMDA's median sign: BAWAL TUMAWID (no crossing), NAKAMAMATAY (it kills)."""
    W, H = 768, 512
    img = Image.new("RGB", (W, H), "#efeee8")
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((12, 12, W - 13, H - 13), radius=20, outline="#a3302a", width=16)
    centred(d, "BAWAL", BLACK, W / 2, 125, W - 120, 130, "#a3302a")
    centred(d, "TUMAWID", BLACK, W / 2, 265, W - 110, 120, "#a3302a")
    centred(d, "NAKAMAMATAY!", BOLD, W / 2, 400, W - 180, 70, "#2a2a2a")
    sign_save(wear(img, 231, 0.2, "c9bfae"), "sign_bawal")


def sakayan():
    W, H = 512, 768
    img = Image.new("RGB", (W, H), "#24533c")
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((12, 12, W - 13, H - 13), radius=20, outline="#ecebe4", width=10)
    centred(d, "SAKAYAN", BLACK, W / 2, 150, W - 80, 100, "#ecebe4")
    centred(d, "AT BABAAN", BOLD, W / 2, 255, W - 110, 70, "#ecebe4")
    d.rounded_rectangle((70, 340, W - 70, 700), radius=24, fill="#ecebe4")
    centred(d, "PUJ", BLACK, W / 2, 440, W - 200, 110, "#24533c")
    centred(d, "BUS", BLACK, W / 2, 600, W - 200, 110, "#24533c")
    sign_save(wear(img, 241, 0.2, "5d8a70"), "sign_sakayan")


def timer():
    W, H = 256, 192
    img = Image.new("RGB", (W, H), "#161616")
    d = ImageDraw.Draw(img)
    centred(d, "18", ["impact.ttf", "ariblk.ttf"], W / 2, H / 2, W - 50, H - 40, "#c9302a")
    sign_save(img, "sign_timer")


def barangay():
    """A hand-painted welcome board, 3.2 x 1.8 m. Every name on it is invented. Each letter is
    set by hand: a small random tilt and baseline hop, so it reads painted, not printed."""
    W, H = 1280, 720
    img = Image.new("RGB", (W, H), "#e7c139")
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((18, 18, W - 19, H - 19), radius=40, outline="#b3312a", width=22)
    d.rounded_rectangle((52, 52, W - 53, H - 53), radius=26, outline="#b3312a", width=6)
    rng = np.random.default_rng(251)

    def painted(text, fnames, cy, box_w, box_h, fill):
        f, (l, t, r, b) = fit_text(d, text, fnames, box_w, box_h)
        x = W / 2 - (r - l) / 2
        for ch in text:
            cw = d.textlength(ch, font=f)
            if ch != " ":
                tile = Image.new("RGBA", (int(cw) + 40, int(b - t) + 60), (0, 0, 0, 0))
                ImageDraw.Draw(tile).text((20, 20 - t), ch, font=f, fill=fill)
                tile = tile.rotate(rng.uniform(-4, 4), resample=Image.BICUBIC)
                img.paste(tile, (int(x - 20), int(cy - (b - t) / 2 - 20 + rng.uniform(-4, 4))), tile)
            x += cw

    painted("MALIGAYANG PAGDATING SA", BOLD, 118, 900, 58, "#2b2a28")
    painted("BARANGAY 712", BLACK, 250, 1060, 170, "#b3312a")
    painted("ZONE 78  ·  DISTRITO V  ·  ERMITA", BOLD, 372, 980, 52, "#2b2a28")
    painted("KAP. NENITA \"NITA\" BAUTISTA-REYES", BOLD, 470, 1020, 56, "#b3312a")
    painted("AT MGA KAGAWAD: OMENG DELA PAZ, BOYET SANTILLAN, LOLENG MACARAIG", NARROW, 560, 1080, 38, "#2b2a28")
    painted("BAWAL ANG MAGKALAT!", BLACK, 640, 700, 50, "#b3312a")
    sign_save(wear(img, 252, 0.22, "f3e3a0"), "sign_barangay")


PAINTERS = [asphalt, pavement, sidewalk, lambda: kerb("kerb_white", False), lambda: kerb("kerb_stripes", True),
            median_wall, coping, soil, lawn, lot, parking, drive, paint, chalk, steel, concrete_pole, wall]
SIGNS = [gutter, pothole, lambda: blade("sign_blade_taft", "TAFT AVE."), lambda: blade("sign_blade_faura", "PADRE FAURA ST."),
         one_way, bawal, sakayan, timer, barangay]


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    names = [n for n in TEX]
    cell, pad, cols = 300, 16, 6
    rows = math.ceil(len(names) / cols) + 2
    sheet = Image.new("RGB", (cols * (cell + pad) + pad, rows * (cell + 40) + pad), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, n in enumerate(names):
        tile = Image.open(OUT / f"street_{n}_albedo.png").convert("RGB")
        wm, hm, _ = TEX[n]
        # 2 x 2 repeat, scaled so every swatch shows the same metres per pixel where it fits
        rep = Image.new("RGB", (tile.width * 2, tile.height * 2))
        for i in range(2):
            for j in range(2):
                rep.paste(tile, (i * tile.width, j * tile.height))
        rep.thumbnail((cell, cell))
        x, y = pad + (k % cols) * (cell + pad), pad + (k // cols) * (cell + 40)
        sheet.paste(rep, (x, y))
        draw.text((x, y + rep.height + 4), f"street_{n} (2x2, {2 * wm:g} x {2 * hm:g} m)", fill=(40, 40, 40))
    x, y = pad, pad + (math.ceil(len(names) / cols)) * (cell + 40)
    for n in ("pothole", "sign_blade_taft", "sign_blade_faura", "sign_oneway", "sign_bawal", "sign_sakayan",
              "sign_timer", "sign_barangay"):
        im = Image.open(OUT / f"street_{n}_albedo.png").convert("RGB")
        im.thumbnail((cell * 1.6, cell))
        if x + im.width > sheet.width - pad:
            x, y = pad, y + cell + 40
        sheet.paste(im, (x, y))
        draw.text((x, y + im.height + 4), "street_" + n, fill=(40, 40, 40))
        x += im.width + pad
    path = SHEETS / f"street_swatches_v{version}.png"
    sheet.save(path)
    print("[street-tex] sheet", path)


def main():
    if "--normals-only" in sys.argv:
        for n in STRENGTH:
            h = np.asarray(Image.open(OUT / f"street_{n}_height.png"), dtype=float) / 255
            Image.fromarray((normal_from_height(h, STRENGTH[n]) * 255).astype(np.uint8)).save(OUT / f"street_{n}_normal.png")
        return
    for p in PAINTERS + SIGNS:
        p()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
