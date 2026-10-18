"""usage: py -3 tools/build_ultimate_fx_textures.py [--sheet <out.jpg>]   (run from the repository root)

Paints the greyscale MASK TEXTURES of the effects study (`Runtime/Visual/FxStudy`, shader `TumbangPreso/FxStudyRibbon`)
into `Assets/TumbangPreso/Resources/Textures/UltimateFx/`.

WHY TEXTURES. The first cutscene effects kit (`UltimateErode.shader`) made its masks from noise inside the shader, and
the owner's verdict on what came of it was "none of the effects you added actually look like the references i gave you".
Every one of his references is built the other way: a small AUTHORED greyscale picture is scrolled along a mesh and the
picture IS the shape (a ribbon wearing a picture of ink loops is a ribbon of ink loops). So the pictures are painted
here, one function each, and the shader only scrolls, thresholds and colours them.

They are OUR OWN pictures. The references taught the recipes (islands to grey then a directional blur for an erosion
map; a ring, warped, thresholded, blurred outward for a radial one; five families of trail mask) and nothing is traced
from them: every stroke, sigil and ornament below is drawn by this script from typed numbers and a fixed seed.

WHICH WAY ROUND. For anything that scrolls, x is ALONG the piece and wraps (the left edge meets the right), y is ACROSS
it and does not. A mesh therefore carries u along and v across.

Everything is deterministic: a fixed seed per picture, so a rebuild writes the same bytes and the diff stays empty.
"""
import math
import os
import sys

import cv2
import numpy as np
from scipy import ndimage
from scipy.spatial import cKDTree

OUT = os.path.join("Assets", "TumbangPreso", "Resources", "Textures", "UltimateFx")
N = 512


# ---------------------------------------------------------------- small tools

def smooth(a, b, x):
    t = np.clip((x - a) / (b - a + 1e-9), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def noise(seed, beta=2.0, n=N, stretch=(1.0, 1.0), even=False):
    """Soft noise that wraps on both edges: white noise with its fine grain filtered away. `stretch` (x, y) above 1
    draws the grain out along that axis."""
    rng = np.random.default_rng(seed)
    spectrum = np.fft.fft2(rng.standard_normal((n, n)))
    fy = np.fft.fftfreq(n)[:, None] * stretch[1]
    fx = np.fft.fftfreq(n)[None, :] * stretch[0]
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    out = np.real(np.fft.ifft2(spectrum / f ** beta))
    out -= out.min()
    out /= out.max()
    if even:
        # Every grey level as common as every other, so "above a half" is always half the sheet.
        order = np.argsort(out, axis=None)
        ranked = np.empty(out.size)
        ranked[order] = np.linspace(0.0, 1.0, out.size)
        out = ranked.reshape(out.shape)
    return out


def warp(image, dx, dy):
    """Reads `image` from a place moved by (dx, dy) pixels, wrapping."""
    h, w = image.shape
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    return cv2.remap(image.astype(np.float32), (xs + dx.astype(np.float32)) % w, (ys + dy.astype(np.float32)) % h,
                     cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)


def blur(image, sx, sy, wrap_y=False):
    return ndimage.gaussian_filter(image, sigma=(sy, sx), mode=("wrap" if wrap_y else "nearest", "wrap"))


def save(name, image):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".png")
    cv2.imwrite(path, np.clip(image * 255.0 + 0.5, 0, 255).astype(np.uint8))
    return path


class Canvas:
    """A greyscale sheet painted at three times its size and shrunk, so every edge is clean. Shapes are given in 0..1
    and anything drawn near the left or right edge is drawn again on the other side, which is what makes it wrap."""

    def __init__(self, w=N, h=N, scale=3, wrap=True):
        self.w, self.h, self.s, self.wrap = w, h, scale, wrap
        self.a = np.zeros((h * scale, w * scale), np.float32)

    def _points(self, points, shift):
        return np.array([[(x + shift) * self.w * self.s, y * self.h * self.s] for x, y in points], np.int32)

    def layer(self):
        return np.zeros_like(self.a)

    def fill(self, layer, points, value=1.0):
        for shift in ((-1, 0, 1) if self.wrap else (0,)):
            cv2.fillPoly(layer, [self._points(points, shift)], float(value), lineType=cv2.LINE_AA)

    def line(self, layer, points, width, value=1.0, closed=False):
        for shift in ((-1, 0, 1) if self.wrap else (0,)):
            cv2.polylines(layer, [self._points(points, shift)], closed, float(value),
                          max(1, int(round(width * self.w * self.s))), lineType=cv2.LINE_AA)

    def nib(self, layer, points, widths, angle=40.0, flat=0.32, value=1.0):
        """A calligraphy stroke: a slanted flat nib dragged along the points, so the line is fat where it runs across
        the slant and thin where it runs with it."""
        for shift in ((-1, 0, 1) if self.wrap else (0,)):
            for (x, y), width in zip(points, widths):
                r = max(1.0, width * self.w * self.s)
                cv2.ellipse(layer, (int((x + shift) * self.w * self.s), int(y * self.h * self.s)),
                            (int(r), max(1, int(r * flat))), angle, 0, 360, float(value), -1, lineType=cv2.LINE_AA)

    def done(self, layer=None):
        return cv2.resize(self.a if layer is None else layer, (self.w, self.h), interpolation=cv2.INTER_AREA)


def spline(points, per=24, closed=False):
    """A smooth line through typed points (Catmull-Rom)."""
    p = [np.array(q, np.float64) for q in points]
    if closed:
        p = [p[-1]] + p + [p[0], p[1]]
    else:
        p = [p[0]] + p + [p[-1]]
    out = []
    for i in range(1, len(p) - 2):
        for k in range(per):
            t = k / per
            out.append(0.5 * ((2 * p[i]) + (-p[i - 1] + p[i + 1]) * t + (2 * p[i - 1] - 5 * p[i] + 4 * p[i + 1] - p[i + 2]) * t * t
                              + (-p[i - 1] + 3 * p[i] - 3 * p[i + 1] + p[i + 2]) * t ** 3))
    if not closed:
        out.append(p[-2])
    return [(float(q[0]), float(q[1])) for q in out]


def taper(count, fat, power=0.7, tail=0.0):
    """Stroke widths: nothing at both ends, `fat` in the middle; `tail` above 0 keeps the far end from closing."""
    return [fat * max(tail, math.sin(math.pi * (i / max(1, count - 1))) ** power) for i in range(count)]


# ---------------------------------------------------------------- the five trail masks
#
# ⚠️ READ AGAIN AT FULL SIZE, AND CORRECTED BY THE OWNER (2026-10-10). The first set of these was painted from a
# thumbnail's worth of looking and described as five unrelated things (shards, smoke, ink loops, squiggles, bands).
# His words: "first trail btw is a caustic, not a bunch of shapes"; "last trail is 2 full ribbons faded in the center";
# "i think the middle 2 are still applying the same concept of single continuous ribbons, just twisted and meandering
# different ways". So there are two ideas here, not five:
#   * mask 1 is a CAUSTIC, the net of light on the floor of a pool: soft veils lying over each other on a grey field,
#     a brighter line along each veil's edge, brighter still where veils overlap, a few dark cells where none lies;
#   * masks 2 to 5 are all A RIBBON OF CLOTH drawn the whole length of the sheet, BRIGHT AT ITS EDGES AND FADED TOWARD
#     ITS MIDDLE. 5 is two of them, plain. 3 and 4 are one each, TWISTED: its two edges cross and cross again, so it
#     pinches to a point where it is edge-on and opens into a lobe where it faces us (the "holes" are the faded
#     middles of the lobes). 2 is one broad one, pleated along its length.
# `ribbon` below is that one idea; the four masks are four settings of it, each typed.

# ⚠️ AND THE SECOND SET FAILED TOO. The owner marked those boards up against the reference's: a caustic drawn as flat
# regions with a hard white outline round each ("a contour map"), a curtain drawn as stripes, the twisted ribbons drawn
# as a row of separate identical diamonds and hooks ("3 and 4 dont even look close"), and eight edge lines on the last
# where the reference has four. What all five had wrong is the same: HARD UNIFORM OUTLINES, FLAT FILLS, REPETITION.
# The reference's boards are soft, shaded and hand-drawn in feel, a few large forms with a gradient everywhere. So:
# every form below is typed in a table so no two match, every fill is a gradient, a bright line appears only at a real
# edge or crossing, and every sheet is softened at the end.

def wave(xs, terms):
    """A sum of typed sine terms, each (cycles across the sheet, height, phase). Whole cycles only, so it wraps."""
    return sum(height * np.sin(2 * math.pi * cycles * xs + phase) for cycles, height, phase in terms)


def blend(table, t):
    """A typed table of numbers read smoothly round and round: t = 2.4 is four tenths of the way from entry 2 to 3."""
    i = int(math.floor(t)) % len(table)
    f = t - math.floor(t)
    f = f * f * (3 - 2 * f)
    return table[i] * (1 - f) + table[(i + 1) % len(table)] * f


def trail_shards():
    """Mask 1, THE CAUSTIC, made the way a caustic is made. (The owner, after it had been drawn as shards, as planes
    and as soft sheets: "they're caustics...".) Light is sent straight down through a rippled surface and each ray
    is bent by the slope of the surface where it passed; the picture is simply WHERE THE RAYS LAND. Where the surface
    focuses them they bunch into the bright curved folds and cusps, brighter again where folds cross, and where it
    spreads them the floor is dim. Nothing is outlined and nothing is a shape: the net falls out of the ripples.
    The surface is a few typed slow waves (whole cycles, so it wraps) and a little smooth noise; `depth` is how far
    below it the floor is, which sets how sharply the folds close."""
    rays = 4
    size = N * rays
    ys, xs = np.mgrid[0:size, 0:size] / float(size)
    # (cycles along x, cycles along y, height, phase): three slow waves and no more, so there are only a few folds
    # across the sheet and they run long.
    surface = sum(height * np.sin(2 * math.pi * (cx * xs + cy * ys) + phase) for cx, cy, height, phase in (
        (1, 1, 0.45, 0.3), (1, -2, 0.35, 1.9), (2, 1, 0.25, 4.2)))
    # Mostly slow smooth noise: three waves alone cross in a regular lattice, which no water makes.
    surface = surface + 5.0 * (cv2.resize(noise(11, 3.3).astype(np.float32), (size, size), interpolation=cv2.INTER_CUBIC) - 0.5)
    # Smoothed before it is differenced: the noise was painted at a quarter of this size and its steps would count
    # as ripples.
    surface = ndimage.gaussian_filter(surface, 10, mode="wrap")
    slope_y, slope_x = np.gradient(surface)
    # The floor is put WELL PAST where the ripples first focus (a ray moves by slope times depth, and the folds close
    # where that movement changes by a whole pixel from one ray to the next). At first focus the picture is a thin
    # bright net round dark cells; further down each fold has opened into a pale sheet with a bright edge and the
    # sheets lie over one another, which is the reference's board.
    bend = np.gradient(slope_x, axis=1) + np.gradient(slope_y, axis=0)
    # ⚠️ KEEP THESE FOUR NUMBERS (noise grain 3.3, depth 2.4, blur 2.4, the curve below). The owner, of exactly this
    # sheet: "caustic effect is like 90% close". A later, calmer setting was tried and was not what he had approved.
    depth = 2.4 / float(np.abs(bend).std())
    land_x = (np.mgrid[0:size, 0:size][1] + slope_x * depth).astype(np.float64)
    land_y = (np.mgrid[0:size, 0:size][0] + slope_y * depth).astype(np.float64)
    cell = ((np.floor(land_y / rays).astype(np.int64) % N) * N + (np.floor(land_x / rays).astype(np.int64) % N)).ravel()
    light = np.bincount(cell, minlength=N * N).reshape(N, N) / float(rays * rays)
    light = blur(light, 2.4, 2.4, wrap_y=True)
    # A soft curve: mostly mid grey, the folds pale, only the crossings white.
    return np.clip(0.14 + 0.46 * light ** 0.55, 0, 1)


# ⚠️ MASKS 2 TO 5 ARE RENDERS OF A STRIP OF THIN CLOTH IN SPACE, NOT DRAWINGS (2026-10-10). Three rounds of drawing
# them flat by formula (edge lines that cross, a stroke along a path) each came out mechanical, and the owner turned
# every one down. What the reference's boards actually look like is a strip of thin see-through stuff TWISTING IN
# SPACE, seen flat-on. So that is what is made: `cloth` takes any surface, given as where each of its points is (how
# far along, how far across), scatters light evenly over it and counts what lands in each pixel looking straight at
# it. Everything the drawings had to fake then comes for nothing, and correctly:
#   * where the cloth faces us it is thin and dim (the hollow of a loop);
#   * where it turns edge-on the same cloth is stacked into a few pixels and goes bright (a rim, a fold, a pinch);
#   * where it passes behind itself the two layers add;
#   * its two edges are bright because the strip is curled across its width like a ribbon off a spool (`curl`), so
#     the edges stand toward us and are seen thick.
# The four masks are four typed settings of one strip: its path, how it turns over, how wide it is.

def hemmed(t, v):
    """Cloth that is thin in the middle of the strip and thick at its two edges."""
    return 0.07 + 0.93 * np.abs(v) ** 4.0


def rimmed(t, v):
    """Cloth with a broad thick hem down each edge and almost nothing between: a loop of it is a fat bright rim round
    a dark hollow."""
    return 0.05 + 0.95 * smooth(0.12, 0.62, np.abs(v))


def cloth(point, along=5000, across=320, weight=None, gain=1.0, soften=1.2, seed=7):
    """Renders a surface flat-on. `point(t, v)` gives x (wraps), y for arrays t in 0..1 (along) and v in -1..1
    (across); `weight(t, v)` how much cloth is at each. Returns the sheet: how much cloth each pixel looks through."""
    rng = np.random.default_rng(seed)
    t = (np.arange(along)[:, None] + rng.random((along, across))) / along
    v = (np.arange(across)[None, :] + rng.random((along, across))) / across * 2.0 - 1.0
    x, y = point(t, v)
    w = np.ones_like(t) if weight is None else weight(t, v)
    px = np.floor(x * N).astype(np.int64) % N
    py = np.floor(y * N).astype(np.int64)
    keep = (py >= 0) & (py < N)
    hits = np.bincount((py[keep] * N + px[keep]).ravel(), weights=w[keep].ravel(), minlength=N * N).reshape(N, N)
    hits *= gain * N * N / float(along * across)
    return 1.0 - np.exp(-blur(hits, soften, soften))


def strip(t, v, path, turn, half, curl=0.55, lean=None):
    """A point on a twisted strip. `path(t)` is its middle line (x, y); `turn(t)` how far it has turned over about that
    line (radians: 0 faces us, a quarter turn is edge-on); `half(t)` its half width. It is curled across its width
    by `curl` radians each side, and `lean(t)` tilts its width off the upright, in the sheet."""
    x0, y0 = path(t)
    # Which way the path is heading, from the path itself a hair either side (the sample points are scattered, so
    # they cannot be differenced against their neighbours).
    x1, y1 = path(t - 1e-4)
    x2, y2 = path(t + 1e-4)
    dx, dy = x2 - x1, y2 - y1
    n = np.maximum(np.hypot(dx, dy), 1e-9)
    ax, ay = -dy / n, dx / n                       # across, in the sheet
    if lean is not None:
        c, s_ = np.cos(lean(t)), np.sin(lean(t))
        ax, ay = ax * c - ay * s_, ax * s_ + ay * c
    flat = np.sin(v * curl) / math.sin(curl)       # across the strip, as seen face on
    proud = (1.0 - np.cos(v * curl)) / math.sin(curl)
    # Turned over by `turn`, the part of the strip's width that still lies in the sheet is its cosine; what stood
    # toward us comes round into the sheet by its sine.
    reach = half(t) * (flat * np.cos(turn(t)) + proud * np.sin(turn(t)))
    return x0 + ax * reach, y0 + ay * reach


def trail_smoke():
    """Mask 2, THE CURTAIN: one very wide strip, most of the sheet high, hanging in folds. The folds are real: along
    its length the cloth is gathered, pushed forward and back on itself by a few typed waves of different widths, so
    in places it doubles over and the doubled edge is a bright ridge with a soft valley beside it. The folds drift as
    they fall, the upper hem wanders, and the cloth thins away downward to nothing."""
    def point(t, v):
        fall = 0.5 + 0.5 * v                                    # 0 at the upper hem, 1 at the lower
        gather = (0.060 * np.sin(2 * math.pi * (3 * t + 0.10 + 0.70 * fall)) + 0.040 * np.sin(2 * math.pi * (5 * t + 0.55 - 0.90 * fall))
                  + 0.022 * np.sin(2 * math.pi * (7 * t + 0.80 + 1.30 * fall)) + 0.050 * np.sin(2 * math.pi * (2 * t + 0.30 - 0.50 * fall)))
        billow = 0.035 * np.sin(2 * math.pi * (4 * t + 1.7 * fall + 0.2)) + 0.025 * np.sin(2 * math.pi * (6 * t - 2.3 * fall + 0.7))
        hem = 0.17 + 0.045 * np.sin(2 * math.pi * (2 * t + 0.4)) + 0.030 * np.sin(2 * math.pi * (5 * t + 1.9))
        hang = 0.60 + 0.07 * np.sin(2 * math.pi * (3 * t + 1.1)) + 0.05 * np.sin(2 * math.pi * (t + 3.0))
        return t + gather * (0.45 + 0.55 * fall), hem + hang * fall ** 1.15 + billow * fall

    def weight(t, v):
        fall = 0.5 + 0.5 * v
        return smooth(0.0, 0.34, fall) * smooth(1.0, 0.30, fall) ** 1.6 * (1.0 + 0.35 * (1.0 - fall))

    out = cloth(point, along=6000, across=400, weight=weight, gain=0.95, soften=1.8)
    # The lower part is blurred more than the upper, so it trails off in soft uneven tongues and not in teeth.
    low = smooth(0.42, 0.78, np.mgrid[0:N, 0:N][0] / float(N))
    return np.clip(out * (1.0 - low) + blur(out, 4.0, 9.0) * low, 0, 1)


def trail_ink():
    """Mask 3, THE TWISTED RIBBON, slow and broad: one strip that turns over five times in the sheet, so it shows as
    five tall thin loops, each a thick soft rim round a DARK hollow (the cloth is all hem and no middle), pinched
    bright between them. The strip is widest just as it faces us and draws in fast either side, so every loop comes
    to a long point above and below. Each loop is typed (how tall, which way it leans, how fast it is turning), so no
    two match, and its middle line wanders."""
    tall = (0.46, 0.33, 0.48, 0.30, 0.42)
    leans = (0.34, -0.26, 0.40, -0.18, 0.22)
    hurry = (0.00, 0.20, -0.16, 0.12, -0.10)

    def table(values, t):
        at = t * len(values)
        i = np.floor(at).astype(np.int64) % len(values)
        f = at - np.floor(at)
        f = f * f * (3 - 2 * f)
        v = np.array(values)
        return v[i] * (1 - f) + v[(i + 1) % len(values)] * f

    path = lambda t: (t, 0.50 + 0.050 * np.sin(2 * math.pi * (t + 0.15)) + 0.030 * np.sin(2 * math.pi * (3 * t + 0.6)))
    turn = lambda t: math.pi * (5 * t + 0.5) + table(hurry, t) * 2.0
    half = lambda t: table(tall, t + 0.1) * (0.10 + 0.90 * np.abs(np.cos(turn(t))) ** 3.2)
    # A hem that is most of the strip's width, and a wide blur after: fat pale rims, not hairlines.
    rim = lambda t, v: 0.02 + 0.98 * smooth(0.10, 0.55, np.abs(v))
    return np.clip(cloth(lambda t, v: strip(t, v, path, turn, half, 0.15, lambda q: table(leans, q + 0.1)),
                         weight=rim, gain=0.95, soften=5.0), 0, 1)


def trail_squiggle():
    """Mask 4, THE TWISTED RIBBON, quicker and thinner: a narrower strip on a lazy meander across the whole sheet (a
    few big slow waves), turning over unevenly, flaring wide in typed places and pinching between, and thrown into two
    small loops where its path doubles back. Brush calligraphy, in one unbroken stroke.

    ⚠️ KEEP EVERY NUMBER HERE. The owner, of exactly this sheet: "trail squiggle is pretty much there". It was rebuilt
    once after that as a fat solid stroke, on a wrong reading of what he wanted, and had to be put back."""
    wide = (0.110, 0.190, 0.085, 0.210, 0.115, 0.170, 0.080)

    def table(values, t):
        at = t * len(values)
        i = np.floor(at).astype(np.int64) % len(values)
        f = at - np.floor(at)
        f = f * f * (3 - 2 * f)
        v = np.array(values)
        return v[i] * (1 - f) + v[(i + 1) % len(values)] * f

    def path(t):
        # The meander, and on it two places where the stroke runs back on itself into a small loop.
        back = 0.050 * np.exp(-((t - 0.30) / 0.05) ** 2) * np.sin(2 * math.pi * (t - 0.30) / 0.10) \
             + 0.045 * np.exp(-((t - 0.72) / 0.045) ** 2) * np.sin(2 * math.pi * (t - 0.72) / 0.09)
        return (t - back, 0.50 + 0.155 * np.sin(2 * math.pi * t + 0.5) + 0.070 * np.sin(2 * math.pi * 2 * t + 2.3)
                + 0.05 * np.exp(-((t - 0.30) / 0.05) ** 2) + 0.05 * np.exp(-((t - 0.72) / 0.045) ** 2))

    turn = lambda t: math.pi * (4 * t + 0.2) + 0.9 * np.sin(2 * math.pi * t + 1.0) + 0.5 * np.sin(2 * math.pi * 2 * t + 2.0)
    lean = lambda t: 0.5 * np.sin(2 * math.pi * (3 * t + 0.2))
    return np.clip(cloth(lambda t, v: strip(t, v, path, turn, lambda q: table(wide, q), 0.55, lean), weight=rimmed, gain=0.80, soften=2.2), 0, 1)


def trail_bands(shift=0.115):
    """Mask 5, in the owner's words: "the 5th is one ribbon, fold applied, copied as is, then shifted down". Three steps
    and nothing else, after it had been built wrong three times (as a wave, as a ribbon and its mirror, and as one
    folded strip with a painted margin):

      1. ONE RIBBON: a strip bright at its two edges and falling to its thinnest only along its own centre line
         ("each ribbon fades to near 0 at the center"; and of a build that hollowed it out, "just the middle is near
         0, but urs, the whole inner part of the ribbon is transparent").
      2. FOLD APPLIED ("think of it like folding paper in a zigzag"; "its not a wave to turn, its more like a
         reflected ribbon"): the strip is folded back and forth and laid nearly flat. Each long edge of the paper is
         one zigzag line, and the other edge is the same line a fold's length along the sheet, so the paper runs in
         legs, each the reflection of the one before, and lies over itself at every fold: a wedge of doubled paper
         whose outer side is the fold and whose point is where the two edges cross. Under an upper fold the dark comes
         to a sharp point; a lower fold is cut flat.
      3. COPIED AS IS, THEN SHIFTED DOWN: the finished folded ribbon is laid down a second time, unchanged (not
         mirrored, not slid along), `shift` of the sheet lower. The two add as veils do.

    That is the whole of the reference's board: two bands on the same path, one under the other like an echo; four
    edge lines on every leg; an arch under each rounded fold; a floor in two steps under each lower fold.

    Measured off that board: one fold up and one down across the sheet (it travels three pixels a frame and repeats in
    about fifty frames, and the board is 148 pixels); the folds stand at 0.07 and 0.77 of the sheet with the upper one
    0.44 along; a fold is 0.285 of the sheet long; the upper folds are soft and tip a little (the far edge 0.055
    lower) and the lower ones are nearly flat; the echo is 0.115 lower, which is about half the strip's own width.

    ⚠️ The cloth's weight is multiplied by the sheet each scrap of paper covers, so the cloth is the same thickness
    everywhere and a fold does not stack into a white bar. Take that out and every fold is a glare line."""
    top, low, crest = 0.070, 0.770, 0.44
    soft, crisp, reach, tilt = 0.072, 0.046, 0.285, 0.055
    grid = np.arange(4096) / 4096.0
    saw = np.where(grid < crest, grid / crest, (1.0 - grid) / (1.0 - crest))        # 0 at the lower fold, 1 at the upper

    def rounded(sigma):
        k = np.exp(-0.5 * ((np.arange(-900, 901) / 4096.0) / sigma) ** 2)
        return np.convolve(np.concatenate([saw[-900:], saw, saw[:900]]), k / k.sum(), mode="valid")

    gentle, tight = rounded(soft), rounded(crisp)
    mix = smooth(0.30, 0.75, gentle)
    height = tight * (1.0 - mix) + gentle * mix
    height = (height - height.min()) / (height.max() - height.min())

    def lift(t):
        return np.interp(np.mod(t, 1.0), grid, height, period=1.0)

    def lay(t, f):
        # f runs across the paper, 0 on one long edge and 1 on the other; seen flat, the paper lies ALONG the sheet
        # from one to the other, which is what a fold is.
        up = lift(t)
        return t + 0.01 + f * reach, low + (top - low) * up + f * tilt * up

    def weight(t, v):
        f = 0.5 + 0.5 * v
        x1, y1 = lay(t - 2e-4, f)
        x2, y2 = lay(t + 2e-4, f)
        x3, y3 = lay(t, f - 1e-3)
        x4, y4 = lay(t, f + 1e-3)
        area = np.abs((x2 - x1) / 4e-4 * (y4 - y3) / 2e-3 - (y2 - y1) / 4e-4 * (x4 - x3) / 2e-3)
        tone = 0.15 + 0.61 * smooth(0.25, 0.85, np.abs(v))
        return area * -np.log(1.0 - tone)

    one = cloth(lambda t, v: lay(t, 0.5 + 0.5 * v), along=9000, across=520, weight=weight, gain=1.0, soften=1.9)
    # The copy: the same picture, moved straight down. Nothing of it reaches the sheet's lower edge (0.77 + 0.115).
    copy = np.zeros_like(one)
    rows = int(round(shift * N))
    copy[rows:] = one[:N - rows]
    return np.clip(1.0 - (1.0 - one) * (1.0 - copy), 0, 0.95)


# ---------------------------------------------------------------- erosion maps

def islands(seed, count=320, n=N):
    """Blobs, each one flat grey of its own (the breakdown's "islands to grayscale"), wrapping on both edges."""
    rng = np.random.default_rng(seed)
    s = 2
    a = np.zeros((n * s, n * s), np.float32)
    for _ in range(count):
        x, y = rng.random() * n * s, rng.random() * n * s
        rx, ry = rng.uniform(0.035, 0.09) * n * s, rng.uniform(0.025, 0.06) * n * s
        grey, turn = float(rng.uniform(0.08, 1.0)), float(rng.uniform(0, 180))
        for ox in (-n * s, 0, n * s):
            for oy in (-n * s, 0, n * s):
                cv2.ellipse(a, (int(x + ox), int(y + oy)), (int(rx), int(ry)), turn, 0, 360, grey, -1, lineType=cv2.LINE_AA)
    return cv2.resize(a, (n, n), interpolation=cv2.INTER_AREA)


def erode_blobs():
    return np.clip(blur(islands(51), 1.2, 1.2, wrap_y=True), 0, 1)


def erode_cloud():
    """The CLOUD a wisp dissolves by: large soft blobs, only a few across the sheet, with a little smaller unevenness
    on them. (The owner: the wisps "do a large cloud noise type fade that increases in intensity until the whole stroke
    disappears".) Thresholded on a wisp it opens a few big soft holes that widen, join and take the last islands."""
    return np.clip(0.80 * noise(55, 3.3, even=True) + 0.20 * noise(56, 2.2, even=True), 0, 1)


def erode_streak():
    """THE erosion map: the islands smeared along the piece. Thresholded on a ribbon it tears the ribbon into long
    streaks from one end, which is how every wisp in the study comes apart."""
    smeared = blur(islands(52), 30.0, 1.6, wrap_y=True)
    smeared -= smeared.min()
    return np.clip(smeared / smeared.max(), 0, 1)


def polar_to_square(polar, n=N):
    """`polar` has the angle along x (wrapping) and the radius down y (0 at the top is the middle of the disc)."""
    ys, xs = np.mgrid[0:n, 0:n].astype(np.float32)
    dx, dy = (xs - n / 2 + 0.5) / (n / 2), (ys - n / 2 + 0.5) / (n / 2)
    radius = np.sqrt(dx * dx + dy * dy)
    angle = (np.arctan2(dy, dx) / (2 * math.pi)) % 1.0
    out = cv2.remap(polar.astype(np.float32), angle * polar.shape[1], np.clip(radius, 0, 1) * (polar.shape[0] - 1),
                    cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)
    return out * (radius < 1.0)


def erode_radial():
    """The radial erosion map: a ring, its edge bent by noise, cut to blobs, each blob its own grey, then blurred
    OUTWARD. A star of streaks: thresholded on a disc it opens the disc from its middle in rays."""
    rng = np.random.default_rng(61)
    polar = np.zeros((N, N), np.float32)
    # The islands, laid out round the disc: each a wedge with its own grey, its own start and its own reach.
    for _ in range(90):
        angle, wide = rng.random() * N, rng.uniform(0.006, 0.035) * N
        start, reach = rng.uniform(0.10, 0.30) * N, rng.uniform(0.45, 0.98) * N
        grey = float(rng.uniform(0.25, 1.0))
        for shift in (-N, 0, N):
            cv2.ellipse(polar, (int(angle + shift), int((start + reach) / 2)), (max(1, int(wide)), int((reach - start) / 2)), 0, 0, 360, grey, -1, lineType=cv2.LINE_AA)
    polar = ndimage.gaussian_filter(polar, sigma=(22, 1.0), mode=("nearest", "wrap"))
    radius = np.mgrid[0:N, 0:N][0] / float(N)
    polar *= smooth(0.02, 0.10, radius) * smooth(0.99, 0.82, radius)
    return np.clip(polar_to_square(polar / polar.max()) * 1.15, 0, 1)


# ---------------------------------------------------------------- sprites

def glow_soft():
    ys, xs = (np.mgrid[0:256, 0:256] - 127.5) / 127.5
    r = np.sqrt(xs * xs + ys * ys)
    return np.clip((1.0 / (1.0 + 14.0 * r * r) - 1.0 / 15.0) * 1.08, 0, 1) * smooth(1.0, 0.8, r)


def flare_star():
    """A four-point star flare: two long thin crossed streaks, two short ones between, a round heart."""
    ys, xs = (np.mgrid[0:N, 0:N] - (N - 1) / 2) / (N / 2)
    def streak(a, b, long, thin):
        return np.exp(-np.abs(a) / long) * np.exp(-(b / thin) ** 2)
    d1, d2 = (xs + ys) * 0.7071, (xs - ys) * 0.7071
    r = np.sqrt(xs * xs + ys * ys)
    out = streak(xs, ys, 0.22, 0.018) + streak(ys, xs, 0.22, 0.018) + 0.45 * (streak(d1, d2, 0.09, 0.014) + streak(d2, d1, 0.09, 0.014))
    out += np.exp(-(r / 0.07) ** 2) + 0.25 / (1.0 + 60 * r * r)
    return np.clip(out, 0, 1) * smooth(1.0, 0.75, r)


def fleck():
    """A teardrop spark: round at the head (right), drawn out to a point behind it."""
    ys, xs = np.mgrid[0:128, 0:256] / np.array([128.0, 256.0])[:, None, None]
    along = xs
    half = 0.36 * np.sin(math.pi * np.clip(along, 0, 1) ** 1.9) ** 0.9
    d = np.abs(ys - 0.5) / np.maximum(half, 1e-4)
    return np.clip(smooth(1.0, 0.55, d) * smooth(0.0, 0.08, along) * smooth(1.0, 0.92, along), 0, 1)


# One row per particle: (fatness, where along it is fattest 0..1, how much it curves, fork depth, notch depth, lobes).
# Sixteen, typed, no two alike: fat drops, thin slivers, curved commas, forked and notched ones, blunt ones, shards.
FLECKS = (
    (0.34, 0.70, 0.00, 0.00, 0.00, 0), (0.12, 0.60, 0.05, 0.00, 0.00, 0), (0.24, 0.75, 0.30, 0.00, 0.00, 0), (0.28, 0.65, 0.00, 0.45, 0.00, 0),
    (0.30, 0.55, -0.12, 0.00, 0.40, 0), (0.40, 0.80, 0.08, 0.00, 0.00, 0), (0.22, 0.60, 0.00, 0.00, 0.00, 2), (0.16, 0.45, -0.22, 0.00, 0.00, 0),
    (0.20, 0.72, 0.18, 0.30, 0.00, 0), (0.36, 0.50, 0.00, 0.00, 0.30, 0), (0.10, 0.50, -0.06, 0.00, 0.00, 0), (0.26, 0.82, -0.28, 0.00, 0.00, 0),
    (0.30, 0.62, 0.10, 0.55, 0.20, 0), (0.18, 0.68, 0.00, 0.00, 0.00, 3), (0.42, 0.66, -0.05, 0.25, 0.00, 0), (0.14, 0.78, 0.25, 0.00, 0.25, 0),
)


def fleck_sheet():
    """THE PARTICLES. (The owner, of the one clean teardrop every spark used to be: "the particles from the original
    aren't all the same or a single image. they're more organic with roughened edges".) Sixteen different dabs in a
    four by four grid, head to the right: each a typed outline (`FLECKS`), its edge then TORN by taking a fine noise
    away from it, so the outline is ragged and slightly broken; inside, a dry-brush streak along its length, brighter
    down one side, and a soft fall-off at the tail. A spark's brightness thins this mask, so as a spark dies its thin
    ragged parts go first and it frays instead of shrinking."""
    cell = 128
    sheet = np.zeros((4 * cell, 4 * cell))
    ys, xs = np.mgrid[0:cell, 0:cell] / float(cell)
    for k, (fat, belly, curve, fork, notch, lobes) in enumerate(FLECKS):
        u = np.clip((xs - 0.06) / 0.88, 0, 1)
        mid = 0.5 + curve * np.sin(math.pi * u) - curve * 0.5
        half = fat * np.sin(math.pi * u ** (math.log(0.5) / math.log(belly))) ** 0.8
        if lobes:
            half = half * (0.72 + 0.28 * np.cos(2 * math.pi * lobes * u))
        d = (ys - mid) / np.maximum(half, 1e-4)
        soft = np.clip(1.0 - np.abs(d), 0, 1) * (half > 0.004)
        # A fork: a wedge cut into the tail. A notch: a bite out of one side.
        soft *= 1.0 - smooth(0.25, 0.0, np.abs(d) / np.maximum(fork * (1.0 - u / 0.55), 1e-4)) * (u < 0.55) * (fork > 0)
        soft *= 1.0 - notch * 2.2 * np.exp(-((u - 0.55) / 0.10) ** 2 - ((d - 0.9) / 0.5) ** 2)
        torn = noise(140 + k, 1.4, n=cell, even=True)
        shape = smooth(0.0, 0.16, soft - 0.34 * torn * (0.5 + 0.8 * (1.0 - u)))
        streak = noise(160 + k, 2.0, n=cell, stretch=(0.25, 3.0), even=True)
        inner = (0.55 + 0.45 * streak) * (0.70 + 0.30 * np.clip(0.5 - d * 0.5, 0, 1)) * (0.45 + 0.55 * u ** 0.7)
        r, c = divmod(k, 4)
        sheet[r * cell:(r + 1) * cell, c * cell:(c + 1) * cell] = shape * inner * smooth(0.0, 0.06, np.minimum(np.minimum(xs, 1 - xs), np.minimum(ys, 1 - ys)))
    return np.clip(sheet * 1.25, 0, 1)


def ink_splats():
    """Four ink splats on one sheet (two by two): a disc pulled about by slow noise until it has lobes, hooks and a
    few thrown drops, cut hard. Flat black smoke for the ground, there for contrast with the light."""
    half = N // 2
    sheet = np.zeros((N, N))
    for k in range(4):
        ys, xs = (np.mgrid[0:half, 0:half] - (half - 1) / 2) / (half / 2)
        pull = noise(71 + k, 1.9, n=half, even=True)
        fine = noise(81 + k, 1.7, n=half)
        r = np.sqrt((xs * (1.25 if k % 2 else 0.95)) ** 2 + (ys * (0.95 if k % 2 else 1.2)) ** 2)
        field = (1.0 - r / 0.33) + (pull - 0.5) * 2.4 + (fine - 0.5) * 0.5
        # Pulled about once more after it is cut, which is what draws its lobes out into hooks and tails.
        blot = warp(smooth(0.0, 0.035, field), (noise(95 + k, 2.3, n=half) - 0.5) * 60, (noise(99 + k, 2.3, n=half) - 0.5) * 60)
        drops = smooth(0.80, 0.83, noise(91 + k, 1.9, n=half)) * smooth(0.95, 0.75, r) * smooth(0.25, 0.45, r)
        edge = smooth(0.98, 0.80, r)
        sheet[(k // 2) * half:(k // 2 + 1) * half, (k % 2) * half:(k % 2 + 1) * half] = np.maximum(blot, drops) * edge
    return np.clip(sheet, 0, 1)


# ---------------------------------------------------------------- our own glyphs, and what is built from them

GLYPH_COLS, GLYPH_ROWS, GLYPH_CELL = 8, 4, 128

# The vocabulary every sigil is written from. Each is a typed stroke inside a unit box (x right, y down).
STROKES = {
    "stem": [(0.50, 0.14), (0.48, 0.50), (0.50, 0.86)],
    "lean": [(0.30, 0.84), (0.46, 0.50), (0.70, 0.16)],
    "back": [(0.28, 0.18), (0.50, 0.48), (0.72, 0.84)],
    "bar": [(0.22, 0.42), (0.50, 0.38), (0.78, 0.42)],
    "low": [(0.24, 0.72), (0.50, 0.76), (0.76, 0.70)],
    "cap": [(0.24, 0.30), (0.38, 0.17), (0.62, 0.17), (0.76, 0.30)],
    "cup": [(0.24, 0.56), (0.38, 0.78), (0.62, 0.78), (0.76, 0.56)],
    "hook": [(0.34, 0.20), (0.62, 0.22), (0.68, 0.46), (0.48, 0.58)],
    "tail": [(0.52, 0.52), (0.66, 0.70), (0.56, 0.86), (0.36, 0.80)],
    "bow": [(0.50, 0.16), (0.76, 0.32), (0.74, 0.60), (0.50, 0.72)],
    "wave": [(0.20, 0.52), (0.34, 0.40), (0.50, 0.54), (0.66, 0.66), (0.80, 0.50)],
    "zig": [(0.28, 0.22), (0.66, 0.36), (0.34, 0.58), (0.72, 0.80)],
    "eye": [(0.50, 0.34), (0.66, 0.50), (0.50, 0.66), (0.34, 0.50), (0.50, 0.34)],
    "tick": [(0.60, 0.16), (0.72, 0.26)],
    "foot": [(0.30, 0.86), (0.42, 0.80)],
}
# Thirty-two sigils, each a typed choice of strokes and whether it carries a dot (and where).
GLYPHS = [
    ("stem bar", None), ("stem cap", (0.5, 0.92)), ("lean back", None), ("hook tail", None), ("bow stem", None), ("cup tick", (0.5, 0.36)),
    ("wave stem", None), ("zig", (0.76, 0.20)), ("eye stem", None), ("cap low", (0.5, 0.52)), ("lean bar", None), ("back cup", None),
    ("hook low", None), ("bow bar foot", None), ("stem tail tick", None), ("cap cup", (0.5, 0.48)), ("wave tick", (0.3, 0.80)),
    ("eye bar", None), ("zig foot", None), ("lean cap", None), ("back low", (0.72, 0.24)), ("hook stem", None), ("bow low", None),
    ("cup stem", None), ("wave low", None), ("eye cap", None), ("stem bar low", None), ("lean tail", None), ("back hook", None),
    ("cap stem cup", None), ("zig bar", None), ("bow tick foot", (0.30, 0.30)),
]


def glyph(index, size=GLYPH_CELL, fat=0.082):
    """One sigil, drawn alone on its own square (0 outside the strokes)."""
    names, dot = GLYPHS[index % len(GLYPHS)]
    c = Canvas(size, size, 4, wrap=False)
    ink = c.layer()
    # Each sigil leans its own way a little, so a ring of them does not read as type.
    lean = math.radians(((index * 37) % 21) - 10)
    def place(p):
        x, y = p[0] - 0.5, p[1] - 0.5
        return (0.5 + (x * math.cos(lean) - y * math.sin(lean)) * 0.98, 0.5 + (x * math.sin(lean) + y * math.cos(lean)) * 0.98)
    for name in names.split():
        path = spline([place(p) for p in STROKES[name]], per=16)
        c.nib(ink, path, taper(len(path), fat, 0.5, 0.45), angle=35.0 + (index % 3) * 12, flat=0.50)
    if dot is not None:
        c.nib(ink, [place(dot)], [fat * 0.8], flat=0.7)
    return np.clip(c.done(ink), 0, 1)


def glyph_atlas():
    """Thirty-two sigils in an eight by four grid. Each keeps clear of its cell's edges, so the sheet tiles."""
    sheet = np.zeros((GLYPH_ROWS * GLYPH_CELL, GLYPH_COLS * GLYPH_CELL))
    for i in range(GLYPH_COLS * GLYPH_ROWS):
        r, col = divmod(i, GLYPH_COLS)
        sheet[r * GLYPH_CELL:(r + 1) * GLYPH_CELL, col * GLYPH_CELL:(col + 1) * GLYPH_CELL] = glyph(i)
    return sheet


def stamp(sheet, image, cx, cy, size, turn):
    """Adds `image` (a square) to `sheet` centred at (cx, cy) in pixels, `size` pixels across, turned `turn` radians."""
    h, w = image.shape
    k = size / w
    c, s = math.cos(turn) * k, math.sin(turn) * k
    m = np.array([[c, -s, cx - c * w / 2 + s * h / 2], [s, c, cy - s * w / 2 - c * h / 2]], np.float64)
    np.maximum(sheet, cv2.warpAffine(image.astype(np.float32), m, (sheet.shape[1], sheet.shape[0]), flags=cv2.INTER_AREA), out=sheet)


def ring_points(radius, count=180, cx=0.5, cy=0.5, start=0.0, sweep=2 * math.pi):
    return [(cx + radius * math.cos(start + sweep * i / count), cy + radius * math.sin(start + sweep * i / count)) for i in range(count + 1)]


def circle_art(size=1024):
    """The first, pale circle: fine line art. Many thin rings, a ring of sixteen sigils, a ring of ticks, a crescent,
    three leaning arcs round a small inner circle."""
    c = Canvas(size, size, 2, wrap=False)
    ink = c.layer()
    for radius, width in ((0.470, 0.006), (0.452, 0.002), (0.385, 0.004), (0.372, 0.0015), (0.290, 0.003), (0.180, 0.004), (0.165, 0.0015), (0.075, 0.003)):
        c.line(ink, ring_points(radius), width)
    for i in range(48):
        a = 2 * math.pi * i / 48
        long = 0.018 if i % 4 else 0.034
        c.line(ink, [(0.5 + 0.452 * math.cos(a), 0.5 + 0.452 * math.sin(a)), (0.5 + (0.452 - long) * math.cos(a), 0.5 + (0.452 - long) * math.sin(a))], 0.002)
    # Four small satellite rings between the third and fourth rings, each with a bead in it.
    for k in range(4):
        a = 2 * math.pi * k / 4 + 0.6
        sx, sy = 0.5 + 0.335 * math.cos(a), 0.5 + 0.335 * math.sin(a)
        c.line(ink, ring_points(0.036, 40, sx, sy), 0.003)
        c.nib(ink, [(sx, sy)], [0.011], flat=1.0)
    # The crescent: a filled moon inside the fourth ring, the boldest mark on the disc.
    moon = c.layer()
    c.fill(moon, ring_points(0.262, 120), 1.0)
    c.fill(moon, ring_points(0.250, 120, 0.5 + 0.052, 0.5 - 0.022), 0.0)
    np.maximum(ink, moon, out=ink)
    # Three leaning arcs between the inner rings, a third of a turn apart.
    for k in range(3):
        a = 2 * math.pi * k / 3 + 0.4
        c.line(ink, ring_points(0.16, 40, 0.5 + 0.13 * math.cos(a), 0.5 + 0.13 * math.sin(a), a + 1.9, 2.5), 0.003)
        c.nib(ink, [(0.5 + 0.235 * math.cos(a + 1.05), 0.5 + 0.235 * math.sin(a + 1.05))], [0.009], flat=0.9)
    out = c.done(ink)
    for i in range(16):
        a = 2 * math.pi * i / 16
        stamp(out, glyph(i * 2 + 1, 128, 0.07), size * (0.5 + 0.420 * math.cos(a)), size * (0.5 + 0.420 * math.sin(a)), size * 0.062, a + math.pi / 2)
    return np.clip(out, 0, 1)


def circle_fill(size=512):
    """The pale wash between the first circle's rings (drawn under its line art): two soft bands and a dim heart."""
    ys, xs = (np.mgrid[0:size, 0:size] - (size - 1) / 2) / (size / 2)
    r = np.sqrt(xs * xs + ys * ys) / 2 * 2
    band = lambda a, b, soft=0.012: smooth(a - soft, a + soft, r) * smooth(b + soft, b - soft, r)
    return np.clip(0.55 * band(0.775, 0.90) + 0.34 * band(0.37, 0.745) + 0.5 * band(0.0, 0.33) * (0.6 + 0.4 * r / 0.33), 0, 1)


def circle_seal(size=1024):
    """The second, lasting circle: bold. A thick outer ring carrying three scroll knots, a thin inner ring, ten sigils
    spaced between the two, a ring of dots inside."""
    c = Canvas(size, size, 2, wrap=False)
    ink = c.layer()
    c.line(ink, ring_points(0.440), 0.016)
    c.line(ink, ring_points(0.412), 0.003)
    c.line(ink, ring_points(0.205), 0.005)
    for k in range(3):
        a = 2 * math.pi * k / 3 - math.pi / 2
        cx, cy = 0.5 + 0.440 * math.cos(a), 0.5 + 0.440 * math.sin(a)
        # A scroll knot: two small spirals turned away from each other on the ring, and a bead between them.
        for side in (-1, 1):
            curl = []
            for i in range(40):
                t = i / 39.0
                radius, sweep = 0.034 * (1 - 0.72 * t), side * (0.3 + 5.2 * t)
                ox, oy = side * 0.040, -0.004
                px, py = ox + radius * math.cos(sweep + math.pi / 2), oy + radius * math.sin(sweep + math.pi / 2)
                curl.append((cx + px * math.cos(a + math.pi / 2) - py * math.sin(a + math.pi / 2), cy + px * math.sin(a + math.pi / 2) + py * math.cos(a + math.pi / 2)))
            c.nib(ink, curl, taper(len(curl), 0.008, 0.4, 0.5), flat=0.8)
        c.nib(ink, [(cx, cy)], [0.017], flat=1.0)
    for i in range(12):
        a = 2 * math.pi * i / 12
        c.nib(ink, [(0.5 + 0.160 * math.cos(a), 0.5 + 0.160 * math.sin(a))], [0.0062], flat=1.0)
    out = c.done(ink)
    for i in range(10):
        a = 2 * math.pi * (i + 0.5) / 10
        stamp(out, glyph(i * 3 + 2, 128, 0.11), size * (0.5 + 0.318 * math.cos(a)), size * (0.5 + 0.318 * math.sin(a)), size * 0.098, a + math.pi / 2)
    return np.clip(out, 0, 1)


# ---------------------------------------------------------------- frost and scorch

def frost_fern(size=1024):
    """The frost under the ice flower: a six-armed fern. Each arm throws side fronds at a third of a turn, each frond
    throws smaller ones, and every line thins to its tip; a feathery bloom round the heart."""
    c = Canvas(size, size, 2, wrap=False)
    ink = c.layer()

    def frond(x, y, angle, length, width, depth):
        ex, ey = x + length * math.cos(angle), y + length * math.sin(angle)
        steps = 8
        for i in range(steps):
            a, b = i / steps, (i + 1) / steps
            c.line(ink, [(x + (ex - x) * a, y + (ey - y) * a), (x + (ex - x) * b, y + (ey - y) * b)], max(0.0009, width * (1 - 0.8 * a)))
        if depth == 0:
            return
        count = 5 if depth == 2 else 3
        for i in range(1, count + 1):
            t = i / (count + 1.0)
            reach = length * (0.46 if depth == 2 else 0.5) * (1 - t * 0.72) * (1.15 if i % 2 else 0.85)
            for side in (-1, 1):
                frond(x + (ex - x) * t, y + (ey - y) * t, angle + side * math.radians(55), reach, width * 0.62, depth - 1)

    for arm in range(6):
        frond(0.5, 0.5, 2 * math.pi * arm / 6 + 0.26, 0.46, 0.013, 2)
        frond(0.5, 0.5, 2 * math.pi * (arm + 0.5) / 6 + 0.26, 0.27, 0.009, 1)
    lines = np.clip(c.done(ink), 0, 1)
    ys, xs = (np.mgrid[0:size, 0:size] - (size - 1) / 2) / (size / 2)
    r = np.sqrt(xs * xs + ys * ys)
    rime = blur(lines, 9, 9) * 1.5 + np.exp(-(r / 0.22) ** 2) * 0.5
    return np.clip((lines * (1.0 - 0.45 * r) + rime * 0.5) * smooth(1.0, 0.9, r), 0, 1)


def scorch(size_x=1024, size_y=512):
    """The burn a beam leaves on the floor, long along x. Two pictures from one plan: the CHAR (a ragged black patch,
    denser to its middle) and the FISSURES (a net of cracks inside it, hottest along the beam's line, so that raising a
    threshold cools them from the outside in)."""
    rng = np.random.default_rng(101)
    ys, xs = np.mgrid[0:size_y, 0:size_x]
    u, v = xs / float(size_x), (ys - size_y / 2) / float(size_y)
    rough = cv2.resize(noise(102, 1.9).astype(np.float32), (size_x, size_y))
    slow = cv2.resize(noise(103, 2.6).astype(np.float32), (size_x, size_y))
    # The patch: long, but never a lens. Its width wanders along the beam, its middle wanders off the beam's line, and
    # its edge is torn by coarse noise into tongues and bays, with soot thrown clear of it.
    line = np.interp(np.linspace(0, 1, size_x), np.linspace(0, 1, 9), [0.06, 0.15, 0.11, 0.19, 0.13, 0.20, 0.12, 0.17, 0.07])
    wander = np.interp(np.linspace(0, 1, size_x), np.linspace(0, 1, 7), [0.02, -0.05, 0.04, -0.02, 0.06, -0.04, 0.03])
    half = line[None, :] * smooth(0.0, 0.07, u) * smooth(1.0, 0.93, u)
    inside = 1.0 - np.abs(v - wander[None, :]) / np.maximum(half, 1e-4)
    torn = cv2.resize(noise(104, 1.55).astype(np.float32), (size_x, size_y))
    field = inside + (rough - 0.5) * 1.3 + (torn - 0.5) * 1.1 + (slow - 0.5) * 0.4
    char = smooth(-0.02, 0.10, field)
    soot = smooth(0.80, 0.84, cv2.resize(noise(105, 1.5).astype(np.float32), (size_x, size_y))) * smooth(-1.6, -0.2, inside)
    char = np.clip(np.maximum(char * (0.55 + 0.25 * smooth(0.0, 0.8, inside) + 0.35 * (torn - 0.3)), soot * 0.9), 0, 1)
    # The cracks: the borders between scattered cells, the cells drawn out along the beam.
    points = np.column_stack([rng.random(150) * size_x, size_y / 2 + (rng.random(150) - 0.5) * size_y * 0.9])
    # A spine of cells along the beam's line gives one long crack down the middle.
    spine = np.column_stack([np.linspace(0.05, 0.97, 22) * size_x + rng.normal(0, 9, 22), size_y / 2 + rng.normal(0, 16, 22)])
    tree = cKDTree(np.vstack([points, spine]) * np.array([1.0, 1.7]))
    near, _ = tree.query(np.column_stack([xs.ravel(), ys.ravel() * 1.7]), k=2)
    gap = (near[:, 1] - near[:, 0]).reshape(size_y, size_x)
    wide = 2.2 + 5.0 * smooth(0.2, 1.0, inside)
    crack = smooth(wide, wide * 0.25, gap)
    heat = np.clip(0.30 + 0.70 * smooth(0.1, 1.0, inside) + (rough - 0.5) * 0.25, 0, 1)
    fissure = crack * heat * smooth(0.0, 0.25, inside + (rough - 0.5) * 0.4)
    return char, np.clip(fissure, 0, 1)


def crystal_grunge():
    """What breaks up a crystal's flat faces: broad soft patches of lighter and darker (the painterly blotches), a few
    brighter cloudy cores, fine specks, and a handful of thin cracks. Mid grey is "no change"."""
    patches = noise(111, 2.4, even=True)
    cloud = smooth(0.62, 0.92, noise(112, 2.0, even=True))
    specks = smooth(0.90, 0.94, noise(113, 0.9, even=True))
    rng = np.random.default_rng(114)
    points = rng.random((16, 2)) * N
    tiled = np.vstack([points + np.array([dx, dy]) for dx in (-N, 0, N) for dy in (-N, 0, N)])
    ys, xs = np.mgrid[0:N, 0:N]
    near, _ = cKDTree(tiled).query(np.column_stack([xs.ravel(), ys.ravel()]), k=2)
    cracks = smooth(1.6, 0.4, (near[:, 1] - near[:, 0]).reshape(N, N)) * smooth(0.35, 0.6, noise(115, 2.2, even=True))
    return np.clip(0.18 + 0.52 * patches + 0.22 * cloud + 0.30 * specks + 0.45 * cracks, 0, 1)


# ---------------------------------------------------------------- all of it

def build():
    made = {}
    for name, paint in (("trail_shards", trail_shards), ("trail_smoke", trail_smoke), ("trail_ink", trail_ink),
                        ("trail_squiggle", trail_squiggle), ("trail_bands", trail_bands), ("erode_blobs", erode_blobs),
                        ("erode_streak", erode_streak), ("erode_cloud", erode_cloud), ("erode_radial", erode_radial), ("glow_soft", glow_soft),
                        ("flare_star", flare_star), ("fleck", fleck), ("fleck_sheet", fleck_sheet), ("ink_splats", ink_splats), ("glyph_atlas", glyph_atlas),
                        ("circle_art", circle_art), ("circle_fill", circle_fill), ("circle_seal", circle_seal), ("frost_fern", frost_fern),
                        ("crystal_grunge", crystal_grunge)):
        made[name] = paint()
    made["scorch_char"], made["scorch_fissure"] = scorch()
    for name, image in made.items():
        save(name, image)
    return made


def contact(made, path):
    cell = 256
    names = list(made)
    cols = 5
    rows = (len(names) + cols - 1) // cols
    sheet = np.zeros((rows * cell, cols * cell), np.uint8)
    for i, name in enumerate(names):
        image = cv2.resize((np.clip(made[name], 0, 1) * 255).astype(np.uint8), (cell, cell), interpolation=cv2.INTER_AREA)
        cv2.putText(image, name, (6, 16), cv2.FONT_HERSHEY_SIMPLEX, .45, 0, 3)
        cv2.putText(image, name, (6, 16), cv2.FONT_HERSHEY_SIMPLEX, .45, 255, 1)
        r, c = divmod(i, cols)
        sheet[r * cell:(r + 1) * cell, c * cell:(c + 1) * cell] = image
    cv2.imwrite(path, sheet, [cv2.IMWRITE_JPEG_QUALITY, 92])


if __name__ == "__main__":
    everything = build()
    print("wrote", len(everything), "textures to", OUT)
    if "--sheet" in sys.argv:
        contact(everything, sys.argv[sys.argv.index("--sheet") + 1])
        print("sheet", sys.argv[sys.argv.index("--sheet") + 1])
