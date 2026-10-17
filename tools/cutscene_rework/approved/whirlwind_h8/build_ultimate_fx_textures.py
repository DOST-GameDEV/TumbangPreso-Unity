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

    ⚠️ KEEP EVERY NUMBER HERE, AND ALL FIVE TRAIL MASKS WITH IT. The owner, of the film made from exactly this sheet
    (trails t32, 2026-10-11): "trails looks good". A softer version (broad hems, eased overlaps) was built after that
    on a reading of the ribbon as too wiry, and was taken out again unfilmed when he approved this one. The approved
    masks, this script as it was, and the trails' code and shaders are kept in
    `tools/cutscene_rework/approved/trails_t32/`; a rebuild must give the five trail PNGs byte for byte.

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
    """The burn a strike leaves on the floor. Two pictures from one plan: the CHAR (a broad black patch, denser to its
    middle, its edge torn into tongues and bays, soot thrown clear of it) and the FISSURES inside it.

    ⚠️ A PATCH, NOT A SNAKE, AND CRACKS THAT BRANCH. The first plan was a long thin band with an even net of hairline
    cracks: on the floor it was a narrow black snake with wire in it. Now the patch is a wide lobed blot, and the
    fissures are a TREE: seven thick ones break outward from the strike in jagged walks, each throws thinner branches off
    to either side, each of those throws twigs, and a faint net of hairlines fills between. Each is painted as hot as
    it is thick and cooler toward its tip, so raising a threshold through the picture cools the hairlines first, then
    the twigs, then the branches, and the heart of the wound is the last thing glowing."""
    rng = np.random.default_rng(101)
    ys, xs = np.mgrid[0:size_y, 0:size_x]
    rough = cv2.resize(noise(102, 1.9).astype(np.float32), (size_x, size_y))
    slow = cv2.resize(noise(103, 2.6).astype(np.float32), (size_x, size_y))
    torn = cv2.resize(noise(104, 1.55).astype(np.float32), (size_x, size_y))
    # The patch: a blot about the strike, pushed out and pulled in by a few lobes that do not repeat.
    cx, cy = size_x * 0.50, size_y * 0.50
    dx, dy = (xs - cx) / (size_x * 0.31), (ys - cy) / (size_y * 0.31)
    angle = np.arctan2(dy, dx)

    def lobed(a):
        return 1.0 + 0.22 * np.sin(3 * a + 1.0) + 0.16 * np.sin(5 * a + 2.3) + 0.12 * np.sin(2 * a + 0.7) + 0.08 * np.sin(9 * a + 4.0)

    inside = 1.0 - np.hypot(dx, dy) / lobed(angle)          # 1 at the strike, 0 at the patch's edge, below 0 outside
    field = inside * 4.0 + (rough - 0.5) * 1.0 + (torn - 0.5) * 0.9 + (slow - 0.5) * 0.4
    char = smooth(-0.02, 0.14, field)
    soot = smooth(0.80, 0.84, cv2.resize(noise(105, 1.5).astype(np.float32), (size_x, size_y))) * smooth(-0.55, -0.05, inside)
    char = np.clip(np.maximum(char * (0.55 + 0.25 * smooth(0.0, 0.6, inside) + 0.35 * (torn - 0.3)), soot * 0.9), 0, 1)

    # The fissures: every stroke is kept (its points, its width, its heat) and the coolest are painted first, so a hot
    # crack is never painted over by a cold one that crosses it.
    scale = 2
    strokes = []

    def walk(x, y, heading, length, width, heat, depth):
        # Jagged, but going somewhere: every stride is thrown off the crack's own line afresh, and the line itself
        # only drifts.
        steps = max(3, int(length / 20))
        stride = length / steps
        for i in range(steps):
            a = i / float(steps)
            heading += rng.normal(0.0, 0.07)
            bent = heading + rng.normal(0.0, 0.42)
            nx, ny = x + math.cos(bent) * stride, y + math.sin(bent) * stride
            strokes.append((heat * (1.0 - 0.45 * a), (x, y), (nx, ny), width * (1.0 - 0.70 * a)))
            if depth > 0 and i > 0 and rng.random() < (0.50 if depth == 2 else 0.34):
                side = 1.0 if rng.random() < 0.5 else -1.0
                walk(nx, ny, heading + side * rng.uniform(0.55, 1.15), length * (1.0 - a) * rng.uniform(0.40, 0.70) + 14.0,
                     max(1.2, width * (1.0 - 0.70 * a) * 0.55), heat * 0.62, depth - 1)
            x, y = nx, ny

    for k in range(7):
        heading = 2.0 * math.pi * (k + rng.uniform(-0.24, 0.24)) / 7.0 - 0.02
        # As long as the patch reaches that way, less a margin, and in pixels of the sheet (it is twice as wide as high).
        reach = float(lobed(np.array(heading)))
        length = 0.86 * reach * math.hypot(math.cos(heading) * size_x * 0.31, math.sin(heading) * size_y * 0.31)
        walk(cx + rng.normal(0, 6), cy + rng.normal(0, 6), heading, length, 9.5, 1.0, 2)
    tree = np.zeros((size_y * scale, size_x * scale), np.float32)
    for heat, a, b, width in sorted(strokes, key=lambda stroke: stroke[0]):
        cv2.line(tree, (int(a[0] * scale), int(a[1] * scale)), (int(b[0] * scale), int(b[1] * scale)), float(heat),
                 max(1, int(round(width * scale))), lineType=cv2.LINE_AA)
    tree = cv2.resize(tree, (size_x, size_y), interpolation=cv2.INTER_AREA)
    # The hairlines: the borders between scattered cells.
    points = np.column_stack([rng.random(190) * size_x, rng.random(190) * size_y])
    near, _ = cKDTree(points).query(np.column_stack([xs.ravel(), ys.ravel()]), k=2)
    gap = (near[:, 1] - near[:, 0]).reshape(size_y, size_x)
    hair = smooth(2.6, 0.6, gap) * (0.16 + 0.14 * smooth(0.1, 0.8, inside))
    fissure = np.maximum(tree, hair) * smooth(0.02, 0.22, inside + (rough - 0.5) * 0.25)
    return char, np.clip(blur(fissure, 0.7, 0.7), 0, 1)


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


# ---------------------------------------------------------------- wind (Amihan's gale)
# The gale is the first of these pictures to be made for the GAME and not for the bench, and the game is daylight:
# a pale light added to a bright road adds nothing. So every one of them is painted as a VALUE STRUCTURE to be read
# through a three-colour ramp that COVERS: its low greys are the dark ink a stroke is outlined in, its middle greys
# the body, and only the thin heart of a stroke goes to white. A stroke carries its own outline, as the cast do.

def _stamp(layer, cx, cy, radius, value):
    """One soft dab, wrapped on both edges, kept where it is the brightest thing there."""
    n = layer.shape[0]
    r = int(math.ceil(radius * 3)) + 1
    ys, xs = np.mgrid[-r:r + 1, -r:r + 1]
    fx, fy = cx - math.floor(cx), cy - math.floor(cy)
    dab = value * (1.0 + ((xs - fx) ** 2 + (ys - fy) ** 2) / max(radius * radius, 1e-6)) ** -1.6
    rows = (int(math.floor(cy)) + np.arange(-r, r + 1)) % n
    cols = (int(math.floor(cx)) + np.arange(-r, r + 1)) % n
    patch = layer[np.ix_(rows, cols)]
    layer[np.ix_(rows, cols)] = np.maximum(patch, dab)


def _stroke(layer, points, widths, values):
    """A brush stroke: dabs along a path (in sheet units, any real number: it wraps), each its own width and value."""
    n = layer.shape[0]
    for (x, y), w, v in zip(points, widths, values):
        if w > 1e-4 and v > 1e-3:
            _stamp(layer, x * n, y * n, w * n, v)


def wind_streaks():
    """THE BODY OF A GUST: streaks that taper. Each is a comet lying along the sheet: a rounded head, fattest just
    behind it, drawn out into a long thinning tail, bright at the head and dying along the tail, on a faint bend.
    Forty-odd, each typed by its own draw (where, how long, how fat, how bright, which way it bends), none crossing
    the sheet's whole length, with clear air between them. Wraps both ways; the head is toward +x, which is the way
    the picture is scrolled."""
    rng = np.random.default_rng(301)
    layer = np.zeros((N, N), np.float32)
    for k in range(38):
        x0, y0 = rng.random(), (k + rng.random() * 0.8) / 38.0
        length = rng.uniform(0.26, 0.66) * (1.25 if k % 5 == 0 else 1.0)
        fat = rng.uniform(0.009, 0.020) * (1.6 if k % 7 == 0 else 1.0)
        bright = rng.uniform(0.55, 1.0)
        bend = rng.uniform(-0.022, 0.022)
        steps = int(length * N / 1.5)
        t = np.linspace(0.0, 1.0, steps)
        xs = x0 + t * length
        ys = y0 + bend * np.sin(math.pi * t) + 0.0012 * np.sin(2 * math.pi * (2 * t + k))
        widths = fat * (0.16 + 0.84 * t ** 0.7) * (1.0 - t ** 9)
        values = bright * (0.30 + 0.70 * t ** 0.9)
        _stroke(layer, zip(xs, ys), widths, values)
    return np.clip(blur(layer, 0.7, 0.7, wrap_y=True), 0, 0.97)


def wind_bands():
    """THE SKIN OF THE ROLL: a few broad bands of wind, each a sheet with an INKED EDGE. The first film of the gale on a
    lit map wore `wind_streaks` as its skin, forty thin bright lines, and stood on the court as a tangle of green
    wire: nothing in it was a shape. This game draws a thing as a flat shape with a dark outline, and so does this.

    Seven bands lie along the sheet, each typed (where, how long, how broad, how it bends): a comet with a round head
    and a long drawn tail. Painted INSIDE OUT for the ramp that reads it: the body of a band is a low, even grey (a
    pale colour, half see-through), a fold line or two inside it is a middle grey, and its edge is the brightest thing
    on the sheet, which the ramp turns into her dark ink at full strength. So a band is a clear pale sheet inside a
    dark line, like every other thing in the game, and what is behind it shows through its middle. Wraps both ways;
    the heads are toward +x."""
    ys, xs = np.mgrid[0:N, 0:N] / float(N)
    rng = np.random.default_rng(304)
    out = np.zeros((N, N))
    grain = noise(305, 1.7, stretch=(3.0, 1.0), even=True)
    count = 7
    for k in range(count):
        x0 = rng.random()
        y0 = (k + 0.5 + rng.uniform(-0.18, 0.18)) / count
        length = rng.uniform(0.62, 0.92)
        broad = rng.uniform(0.040, 0.058)
        bend = rng.uniform(-0.020, 0.020)
        t = ((xs - x0) % 1.0) / length                                   # 0 at the tail's end, 1 at the head's
        inside = t <= 1.0
        tc = np.clip(t, 0, 1)
        # Thin where it is drawn out, fullest two thirds along, round at the head.
        profile = (0.10 + 0.90 * smooth(0.0, 0.62, tc)) * np.sqrt(np.clip(1.0 - np.clip((tc - 0.90) / 0.10, 0, 1) ** 2, 0, 1))
        middle = y0 + bend * np.sin(math.pi * tc) + 0.006 * np.sin(2 * math.pi * 2 * tc + k)
        d = np.abs(((ys - middle + 0.5) % 1.0) - 0.5)
        wide = broad * profile
        edge = 0.0075
        body = smooth(wide, wide - 0.004, d)
        rim = body * smooth(wide - edge - 0.004, wide - edge, d)
        fold = 0.0
        for at, strength in ((0.38, 0.30), (-0.45, 0.22)):
            line = np.abs(((ys - middle - at * wide + 0.5) % 1.0) - 0.5)
            fold = np.maximum(fold, strength * smooth(0.0045, 0.0015, line) * smooth(0.18, 0.40, tc) * smooth(0.90, 0.70, tc))
        value = body * (0.42 + 0.06 * (grain - 0.5)) + fold * body
        value = np.maximum(value, rim * 0.96)
        out = np.maximum(out, np.where(inside, value, 0.0))
    return np.clip(blur(out, 0.6, 0.6, wrap_y=True), 0, 0.97)


def wind_hoops():
    """THE ROLL'S OWN SKIN: crescents that lie ACROSS the lane. `wind_bands` on the roll, whichever way it was laid,
    stood on the court as a handful of loose leaves with air between them: there was no body. The front this gale
    replaces was plainer and read better, five full arcs from one side of the lane to the other. So this is those
    arcs again, painted as one sheet to be run round a roll: six crescents, each reaching the whole width of the
    sheet and drawn to a point at both ends, fat in the middle, each typed (how broad, how it swells and pinches, how
    it wanders), two of them forked into two strands and joined again. Painted inside out like `wind_bands`: a low
    grey body, a fold line, and an edge that is the brightest thing on the sheet (the ramp's ink).

    Wraps along y only, which is the way it is scrolled; along x it runs once from one end of the chord to the other."""
    ys, xs = np.mgrid[0:N, 0:N] / float(N)
    rng = np.random.default_rng(306)
    grain = noise(307, 1.7, stretch=(3.0, 1.0), even=True)
    out = np.zeros((N, N))
    count = 6
    edge = 0.0125
    for k in range(count):
        y0 = (k + 0.5 + rng.uniform(-0.10, 0.10)) / count
        broad = rng.uniform(0.050, 0.068)
        a, b_, c = rng.uniform(0, 6.28), rng.uniform(0, 6.28), rng.uniform(0, 6.28)
        # Pointed at both ends of the chord, fullest a little off its middle, swelling and pinching on the way.
        lens = np.clip(np.sin(math.pi * np.clip(xs * 1.04 - 0.02, 0, 1)), 0, 1) ** 0.62
        wide = broad * lens * (1.0 + 0.22 * np.sin(2 * math.pi * xs * 1.5 + a) + 0.10 * np.sin(2 * math.pi * xs * 4 + b_))
        middle = y0 + 0.016 * np.sin(2 * math.pi * xs * 1.0 + c) + 0.006 * np.sin(2 * math.pi * xs * 3 + a)
        d = np.abs(((ys - middle + 0.5) % 1.0) - 0.5)
        body = smooth(wide, wide - 0.004, d)
        rim = body * smooth(wide - edge - 0.004, wide - edge, d)
        if k % 3 == 1:
            # The fork: a long eye opened inside the crescent, so for a stretch it is two strands.
            centre, reach = rng.uniform(0.35, 0.65), rng.uniform(0.16, 0.24)
            eye = wide * 0.46 * np.clip(1.0 - ((xs - centre) / reach) ** 2, 0, 1) ** 0.7
            shift = 0.15 * wide
            de = np.abs(((ys - middle - shift + 0.5) % 1.0) - 0.5)
            hole = smooth(eye, eye - 0.004, de) * (eye > 0.004)
            rim = np.maximum(rim * (1.0 - hole), smooth(eye + edge, eye + 0.002, de) * (1.0 - hole) * (eye > 0.004) * body)
            body = body * (1.0 - hole)
        line = np.abs(((ys - middle - 0.36 * wide + 0.5) % 1.0) - 0.5)
        fold = 0.26 * smooth(0.0045, 0.0015, line) * smooth(0.15, 0.35, xs) * smooth(0.90, 0.70, xs)
        value = body * (0.50 + 0.06 * (grain - 0.5) + fold)
        out = np.maximum(out, np.maximum(value, rim * 0.96))
    return np.clip(blur(out, 0.6, 0.6, wrap_y=True), 0, 0.97)


def wind_crest():
    """THE LIP OF THE ROLL: curl-over crests. Each stroke runs along the sheet and then hooks over on itself, a tight
    three-quarter curl whose end turns back under, the way the top of a breaker goes over; thickest on the shoulder
    of the curl and thinning into its tip. Fourteen, of typed sizes, a few with a second small hook inside the first.
    Wraps both ways."""
    rng = np.random.default_rng(302)
    layer = np.zeros((N, N), np.float32)
    for k in range(14):
        x0, y0 = rng.random(), (k + 0.5 + rng.uniform(-0.3, 0.3)) / 14.0
        run, radius = rng.uniform(0.16, 0.30), rng.uniform(0.030, 0.052)
        bright = rng.uniform(0.70, 1.0)
        for scale, lag in ((1.0, 0.0),) + (((0.55, 0.05),) if k % 3 == 0 else ()):
            points, widths, values = [], [], []
            steps = 260
            for i in range(steps):
                t = i / (steps - 1.0)
                if t < 0.55:
                    u = t / 0.55
                    x, y = x0 + lag + u * run * scale, y0 - 0.010 * math.sin(math.pi * u) * scale
                    w = 0.0035 + 0.0085 * u ** 1.3
                else:
                    u = (t - 0.55) / 0.45
                    turn = u * math.radians(265.0)
                    r = radius * scale * (1.0 - 0.45 * u)
                    cx, cy = x0 + lag + run * scale, y0 - radius * scale
                    x, y = cx + r * math.sin(turn), cy + r * math.cos(turn) + (radius * scale - r) * 0.2
                    w = 0.012 * (1.0 - u) ** 0.8 + 0.0025
                points.append((x, y)); widths.append(w * scale ** 0.5); values.append(bright * (0.45 + 0.55 * math.sin(math.pi * min(1.0, t * 1.15)) ** 0.6))
            _stroke(layer, points, widths, values)
    return np.clip(blur(layer, 0.7, 0.7, wrap_y=True), 0, 0.97)


def wind_veil():
    """THE WAKE AND THE DUST: a torn veil. Five long strips of soft stuff lie along the sheet, each typed (where, how
    broad, how strong), each with ragged edges that swell and pinch along its length, with holes torn through them;
    thinner (darker) toward every tear, so a threshold raised through the picture eats the thin parts first and leaves
    a few strands. Wraps both ways."""
    ys, xs = np.mgrid[0:N, 0:N] / float(N)
    # ⚠️ `stretch` is what an axis's ripples COST: the dearer an axis, the less the picture changes along it. These are
    # dear along x, so they change little along the sheet's length and are drawn out that way.
    tear = noise(312, 1.8, stretch=(3.5, 1.0), even=True)
    fine = noise(313, 1.5, stretch=(2.5, 0.8), even=True)
    ragged = noise(314, 1.3, stretch=(2.2, 1.0), even=True)
    veil = np.zeros((N, N))
    for k, (middle, broad, strong) in enumerate(((0.10, 0.060, 0.80), (0.29, 0.085, 1.00), (0.49, 0.050, 0.70), (0.68, 0.095, 0.95), (0.88, 0.065, 0.85))):
        rows = noise(320 + k, 1.7, even=True)
        swell, drift = rows[(k * 97) % N][None, :], rows[(k * 97 + 201) % N][None, :]
        wander = 0.045 * (drift - 0.5)
        d = np.abs(((ys - middle - wander + 0.5) % 1.0) - 0.5) / (broad * (0.25 + 1.3 * swell)) + (ragged - 0.5) * 0.9
        veil = np.maximum(veil, strong * smooth(1.0, 0.15, d))
    holes = 1.0 - smooth(0.50, 0.68, tear)
    veil = veil * holes * (0.50 + 0.50 * fine)
    return np.clip(blur(veil, 1.4, 0.9, wrap_y=True) * 1.05, 0, 0.92)


def abel_threads():
    """HER THREAD, wound into the gale. Three threads cross the sheet on a slant, each going exactly once round it so
    the sheet wraps, each a different GREY: the ramp it is read through turns the three greys into three thread
    colours of her abel (the dark one rust, the middle one teal, the pale one cream), so one picture and one material
    carry all three. A thread's soft edge passes through the darker greys on its way out, which outlines the cream
    thread in teal and rust and the teal one in rust. Each has the fine regular nick of plied yarn along it.

    ⚠️ The three greys are 0.25, 0.66 and 0.95 and the gale's ramp is cut to them (`AmihanGaleRoll`): move one and a
    thread changes colour."""
    ys, xs = np.mgrid[0:N, 0:N] / float(N)
    out = np.zeros((N, N))
    for offset, grey, fat, wobble in ((0.08, 0.95, 0.0125, 0.010), (0.41, 0.66, 0.0105, 0.014), (0.74, 0.25, 0.0115, 0.008)):
        line = offset + xs + wobble * np.sin(2 * math.pi * (3 * xs + offset * 5))
        d = np.abs(((ys - line + 0.5) % 1.0) - 0.5)
        ply = 1.0 - 0.10 * (0.5 + 0.5 * np.sin(2 * math.pi * 46 * xs + 40 * d / fat))
        out = np.maximum(out, grey * ply * smooth(fat, fat * 0.45, d))
    return np.clip(out, 0, 0.97)


def wind_scuff():
    """WHAT THE GALE'S FOOT LEAVES ON THE ROAD: short scrapes along the way it went, in two loose rails with strays
    between, each a quick dry stroke heavier at its start. Sparse: it is a mark, not a carpet. Wraps along x."""
    rng = np.random.default_rng(303)
    layer = np.zeros((N, N), np.float32)
    for k in range(54):
        rail = (0.16, 0.84, 0.50)[k % 3 if k % 9 else 2]
        x0 = rng.random()
        y0 = rail + rng.normal(0.0, 0.045 if rail != 0.50 else 0.16)
        length = rng.uniform(0.03, 0.12)
        bright = rng.uniform(0.40, 0.95)
        t = np.linspace(0.0, 1.0, int(length * N / 1.2) + 2)
        _stroke(layer, zip(x0 + t * length, y0 + 0.004 * np.sin(6 * t + k) + 0 * t), 0.0045 * (1.0 - 0.7 * t), bright * (1.0 - 0.55 * t))
    return np.clip(blur(layer, 0.6, 0.6), 0, 0.95)


# ---------------------------------------------------------------- all of it

# ---------------------------------------------------------------- the whirlwind's horn

# ⚠️ THE HORN OF THE WHIRLWIND IS ONE NOISE, AND THE OWNER SHOWED IT. Of the first bench whirlwind, whose horn was shells
# of the wisp's streak map with a net of trail veins over them: "the middle horn is supposed to be a skewed large
# transparent celled perlin noise". Then he showed which: a picture of SIMPLEX noise with its negative half cut off,
# soft white blobs on black that touch and bridge into each other, and said of it: "but more white", and "feather".
# So this is exactly that and no more: one octave of simplex noise, LARGE (a few lattice steps across the sheet), lifted
# before the cut so that more of the sheet is white, cut at nothing, FEATHERED (its white falls away to black in a
# broad soft slope), and SKEWED (sheared one whole sheet, so its cells lie as diagonals and wind round the horn when
# it is wrapped on it). Black is where the horn is not there at all. No second octave and no lines: his has neither.
# (How many steps, and how the cells came to be drawn out into streaks, is in the painter.)

def _simplex(seed, steps_x, steps_y, n=N):
    """One octave of simplex noise that wraps on both edges: `steps_x` by `steps_y` lattice steps across the sheet
    (`steps_y` even). The lattice is rows of triangles, each row half a step along from the last; a point is the sum of
    its triangle's three corners. About -1 to 1."""
    rng = np.random.default_rng(seed)
    turn = rng.uniform(0.0, 2 * math.pi, (steps_y, steps_x))
    ys, xs = np.mgrid[0:n, 0:n].astype(np.float64)
    x, y = (xs + 0.5) * steps_x / n, (ys + 0.5) * steps_y / n
    u, v = x + 0.5 * y, y
    ix, iy = np.floor(u), np.floor(v)
    lower = (u - ix) > (v - iy)
    corners = ((ix - 0.5 * iy, iy),
               (ix - 0.5 * iy + np.where(lower, 1.0, -0.5), iy + np.where(lower, 0.0, 1.0)),
               (ix - 0.5 * iy + 0.5, iy + 1.0))
    total = np.zeros_like(x)
    for cx, cy in corners:
        dx, dy = x - cx, y - cy
        row = np.mod(cy, steps_y)
        column = np.rint(np.mod(cx, steps_x) + 0.5 * row).astype(int) % steps_x
        angle = turn[row.astype(int), column]
        total += np.maximum(0.8 - (dx * dx + dy * dy), 0.0) ** 4 * (np.cos(angle) * dx + np.sin(angle) * dy)
    return total * 10.9


def _skew(image, sheets=1):
    """Shears a sheet that wraps: each row is slid along by its height, a whole number of sheets from top to bottom, so
    it still wraps both ways. What ran straight along the sheet now runs as a diagonal."""
    n = image.shape[0]
    ys, xs = np.mgrid[0:n, 0:n].astype(np.float32)
    return cv2.remap(image.astype(np.float32), (xs + ys * sheets) % n, ys, cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)


def noise_simplex_cells(step="built", stretch=3, white=0.82, first=False):
    """The horn's noise (see above). `step` stops it early, for a board: "plain" is the noise with its negative half cut
    off and nothing else, as in the picture he showed; "white" lifts it first, so that `white` of the sheet is not
    black, and is the sheet before its shear; "built" (what is built) shears that. Wraps both ways at every step.
    x is along the horn (foot to mouth) and y round it.

    ⚠️ THE SHEET IS THE SLOPE, AND THE HORN DOES THE CUTTING. What is painted is the lifted noise itself: nothing where the
    noise is under the lift, and above it a slope that climbs to each cell's spine. Whoever wears it cuts that slope
    (the horn's material does), and by cutting it HIGHER can make every cell smaller and the gaps between them bigger.
    That is how the horn's mouth is made, in the owner's words: "instead of just feathering at the rim, you should
    instead increase the empty cells near the top so its more wispy at the rim". A sheet already cut to plateaus could
    only be faded there; a slope can be eaten.

    ⚠️ STREAKS, NOT BLOBS. Built with cells as wide as they were long, at four lattice steps and then at six ("use the
    finer cells"), the horn still drew his: "i think the noise is too blob-y". Wind is streaks. So the lattice is
    `stretch` times longer ROUND the horn than along it (six steps along, two round at 3 to 1), which draws every cell
    out into a long lens with pointed ends, lying round the horn; and the shear then slides each row along the horn by
    how far round it is, a whole sheet in one turn, so a streak CLIMBS as it goes round, which is the way the horn
    spins. A slow bend of the whole sheet keeps the streaks from lying ruler-parallel. `stretch=1` is the blobs.

    ⚠️ AND WHITE, NOT GREY. The first build (`first`, kept so a board can show it) lifted the noise until 62 percent of
    the sheet was not black and then blurred the whole cell for a feather: the horn it made was a few dim lobes with
    gaps, and he said so ("a bit too sparse for the main horn"). This one is lifted until 82 percent is not black."""
    if stretch not in (1, 3, 5):
        raise ValueError("the horn's noise is drawn out 3 to 1; 1 (the blobs) and 5 are painted only for a board")
    along, round_ = (4, 4) if first else (6 * stretch // 3 if stretch > 1 else 6, 2 if stretch > 1 else 6)
    raw = _simplex(481, along, round_)
    if step == "plain":
        return np.clip(raw / np.quantile(raw, 0.995), 0, 1)
    cell = N / float(along)
    if first:
        lifted = raw - np.quantile(raw, 1.0 - 0.62)
        lifted = np.clip(lifted / np.quantile(lifted, 0.995), 0, 1)
        if step == "white":
            return lifted
        soft = blur(smooth(0.0, 0.92, lifted) ** 1.15, cell / 9.0, cell / 9.0, wrap_y=True)
        soft = np.clip(soft / soft.max(), 0, 1)
        return soft if step == "feathered" else np.clip(_skew(soft), 0, 1)
    if stretch > 1:
        # The slow bend: the streaks wander a little across their own width as they run, by a field that wraps.
        raw = warp(raw, _simplex(482, 2, 2) * (N / 24.0), _simplex(483, 2, 2) * (N / 60.0))
    lifted = raw - np.quantile(raw, 1.0 - white)
    lifted = np.clip(lifted / np.quantile(lifted, 0.995), 0, 1)
    # Only enough of a blur to take the lattice's corners off the cells.
    lifted = blur(lifted, cell / 30.0, cell / 30.0, wrap_y=True)
    lifted = np.clip(lifted / lifted.max(), 0, 1)
    return lifted if step == "white" else np.clip(_skew(lifted), 0, 1)


def build():
    made = {}
    for name, paint in (("trail_shards", trail_shards), ("trail_smoke", trail_smoke), ("trail_ink", trail_ink),
                        ("trail_squiggle", trail_squiggle), ("trail_bands", trail_bands), ("erode_blobs", erode_blobs),
                        ("erode_streak", erode_streak), ("erode_cloud", erode_cloud), ("erode_radial", erode_radial), ("glow_soft", glow_soft),
                        ("flare_star", flare_star), ("fleck", fleck), ("fleck_sheet", fleck_sheet), ("ink_splats", ink_splats), ("glyph_atlas", glyph_atlas),
                        ("circle_art", circle_art), ("circle_fill", circle_fill), ("circle_seal", circle_seal), ("frost_fern", frost_fern),
                        ("crystal_grunge", crystal_grunge), ("wind_streaks", wind_streaks), ("wind_bands", wind_bands), ("wind_hoops", wind_hoops), ("wind_crest", wind_crest),
                        ("wind_veil", wind_veil), ("abel_threads", abel_threads), ("wind_scuff", wind_scuff),
                        ("noise_simplex_cells", noise_simplex_cells)):
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
