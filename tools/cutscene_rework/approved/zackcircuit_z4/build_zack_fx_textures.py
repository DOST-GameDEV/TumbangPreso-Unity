"""usage: py -3 tools/build_zack_fx_textures.py [--sheet <out.jpg>]   (run from the repository root)

Paints the greyscale masks of Isagani's (zack) electric skill effects into
`Assets/TumbangPreso/Resources/Textures/UltimateFx/`, beside the effects study's own
(`tools/build_ultimate_fx_textures.py`, which this never touches). They are worn through the same shader
(`TumbangPreso/FxStudyRibbon`), so the same rules hold: x is ALONG a piece and wraps where a picture scrolls, y is
ACROSS it, and a picture is plain grey that the shader thresholds and colours.

WHY THESE FOUR. The study had no electricity. A ribbon of light can be swept along a jagged path with the trails that
are already approved, but four things have no picture there:
  * `zack_bolt`         one bolt along x: a thin white line that kinks, a flat body either side of it, a faint veil,
                        and short forks leaving it at its kinks. The thin line is the only white in the effect.
  * `zack_crackle`      a sparse net of thin cracks over a low uneven veil: what a body made of charge is drawn in.
  * `zack_ground_star`  a strike seen from above: a few forked bolts of uneven length out of one small heart.
  * `zack_spark_sheet`  four small forked sparks, two a side, for the handful that are thrown off.

EVERY STROKE IS A CONSTRUCTION, NOT NOISE: a bolt is a line of typed-seed kinks, and each pixel is graded by how far it
is from that line in units of the line's own half width there (inside one: white; inside about three: body; beyond: a
veil that dies away). So a bolt stays crisp where it is thin and its forks taper to a point.

Deterministic: one fixed seed per picture, so a rebuild writes the same bytes.
"""
import math
import os
import sys

import cv2
import numpy as np

OUT = os.path.join("Assets", "TumbangPreso", "Resources", "Textures", "UltimateFx")
N = 512


def save(name, image):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".png")
    cv2.imwrite(path, np.clip(image * 255.0 + 0.5, 0, 255).astype(np.uint8))
    return path


def grade(r, veil=0.26):
    """The three steps across a bolt. r is the distance from its line in half widths: a white line inside 1 with a
    crisp edge, a flat body out to about 2.8 with a softer one, then a veil that is gone by about 9."""
    core = 1.0 - np.clip((r - 0.85) / 0.35, 0, 1)
    body = 0.56 * (1.0 - np.clip((r - 2.3) / 0.9, 0, 1))
    haze = veil * np.exp(-np.maximum(r - 2.0, 0) / 2.6)
    return np.maximum(np.maximum(core, body), haze)


class Sheet:
    """A square sheet in 0..1. Segments are stamped by distance, each with its own half width and strength at either
    end, and the brightest stamp wins, so crossing bolts do not add up to a white blot."""

    def __init__(self, n=N, wrap_x=False, wrap_y=False):
        self.n = n
        ys, xs = np.mgrid[0:n, 0:n]
        self.x = (xs + 0.5) / n
        self.y = (ys + 0.5) / n
        self.v = np.zeros((n, n), np.float32)
        self.shifts = [(dx, dy) for dx in ((-1, 0, 1) if wrap_x else (0,)) for dy in ((-1, 0, 1) if wrap_y else (0,))]

    def segment(self, a, b, wa, wb, ga=1.0, gb=1.0, veil=0.26):
        for dx, dy in self.shifts:
            ax, ay, bx, by = a[0] + dx, a[1] + dy, b[0] + dx, b[1] + dy
            reach = 10.0 * max(wa, wb)
            if max(ax, bx) < -reach or min(ax, bx) > 1 + reach or max(ay, by) < -reach or min(ay, by) > 1 + reach:
                continue
            ex, ey = bx - ax, by - ay
            length2 = max(ex * ex + ey * ey, 1e-9)
            t = np.clip(((self.x - ax) * ex + (self.y - ay) * ey) / length2, 0, 1)
            d = np.hypot(self.x - (ax + ex * t), self.y - (ay + ey * t))
            width = wa + (wb - wa) * t
            gain = ga + (gb - ga) * t
            self.v = np.maximum(self.v, gain * grade(d / np.maximum(width, 1e-5), veil))

    def path(self, points, widths, gains=None, veil=0.26):
        gains = gains or [1.0] * len(points)
        for i in range(len(points) - 1):
            self.segment(points[i], points[i + 1], widths[i], widths[i + 1], gains[i], gains[i + 1], veil)


def kinked(rng, start, heading, length, steps, swing, wander=0.5):
    """A line of kinks: from `start`, `steps` straight pieces along `heading` (radians), each turned to one side then
    the other by up to `swing` radians, so it zigzags about its heading and never curls."""
    points = [start]
    side = 1 if rng.random() < 0.5 else -1
    x, y = start
    for i in range(steps):
        piece = length / steps * rng.uniform(0.6, 1.4)
        turn = heading + side * swing * rng.uniform(wander, 1.0)
        x, y = x + math.cos(turn) * piece, y + math.sin(turn) * piece
        points.append((x, y))
        side = -side
    return points


def fork(sheet, rng, start, heading, length, width, gain=0.9, depth=1):
    """A fork off a bolt: kinked, tapering to a point, dimmer than what it left; it may fork once more."""
    steps = max(2, int(round(length / 0.045)))
    points = kinked(rng, start, heading, length, steps, 0.55)
    widths = [width * (1.0 - 0.92 * i / steps) for i in range(steps + 1)]
    gains = [gain * (1.0 - 0.25 * i / steps) for i in range(steps + 1)]
    sheet.path(points, widths, gains, veil=0.20)
    if depth > 0 and steps >= 3:
        at = points[steps // 2]
        fork(sheet, rng, at, heading + rng.choice((-1, 1)) * rng.uniform(0.5, 0.9), length * 0.45, width * 0.55, gain * 0.8, depth - 1)


def zack_bolt():
    """One bolt along x, wrapping. Its line kinks seven times across the sheet, and NO TWO PIECES OF IT MATCH: the
    pieces are long and short by turns of a seeded draw, and each kink is thrown far or barely at all, so it never
    repeats a tooth (the first one had ten even kinks and read as a drawn zigzag). Three forks leave it at kinks, as
    bright as the line where they leave and tapering to a point; one stretch of the line runs thin."""
    rng = np.random.default_rng(4107)
    sheet = Sheet(wrap_x=True)
    count = 7
    steps = np.array([1.9, 0.55, 1.2, 0.45, 2.3, 0.7, 1.4])
    xs = np.concatenate(([0.0], np.cumsum(steps)[:-1])) / steps.sum()
    throws = (0.115, 0.03, 0.085, 0.125, 0.02, 0.10, 0.055)
    sides = (1, -1, 1, -1, -1, 1, -1)
    ys = [0.5 + sides[i] * throws[i] for i in range(count)]
    points = [(float(xs[i]), ys[i]) for i in range(count)] + [(1.0, ys[0])]
    half = 0.017
    widths = [half * (0.6 if i == 4 else 1.0) * rng.uniform(0.9, 1.15) for i in range(count)]
    widths.append(widths[0])
    sheet.path(points, widths)
    for index, side, length in ((0, 1, 0.24), (3, -1, 0.20), (5, 1, 0.15)):
        fork(sheet, rng, points[index], side * rng.uniform(0.6, 0.95), length, half * 0.8, 1.0)
    # Nothing reaches the two long edges: a fork cut by the edge of its ribbon would end in a flat line.
    edge = np.clip(np.minimum(sheet.y, 1 - sheet.y) / 0.10, 0, 1)
    return sheet.v * edge * edge * (3 - 2 * edge)


def zack_crackle():
    """A sparse net of thin cracks that wraps both ways, over a low uneven veil. The veil is what lets a body drawn in
    it keep a faint fill between its cracks; a piece that wants the cracks alone cuts above it (0.24)."""
    rng = np.random.default_rng(912)
    sheet = Sheet(wrap_x=True, wrap_y=True)
    for i in range(7):
        start = (rng.random(), rng.random())
        heading = rng.uniform(0, 2 * math.pi)
        length = rng.uniform(0.45, 0.8)
        steps = int(length / 0.07)
        points = kinked(rng, start, heading, length, steps, 0.6)
        widths = [0.0075 * rng.uniform(0.8, 1.2) * (1.0 - 0.6 * abs(2.0 * k / steps - 1.0)) for k in range(steps + 1)]
        sheet.path(points, widths, veil=0.0)
        at = points[steps // 2]
        fork(sheet, rng, at, heading + rng.choice((-1, 1)) * rng.uniform(0.7, 1.2), length * 0.4, 0.005, 0.85, 0)
    # The veil: two broad soft waves, wrapping, never above a fifth.
    veil = 0.11 + 0.05 * np.sin(2 * math.pi * (sheet.x + 0.6 * sheet.y)) + 0.04 * np.sin(2 * math.pi * (2 * sheet.y - sheet.x) + 1.3)
    return np.maximum(sheet.v, veil)


def zack_ground_star():
    """A strike from above: six forked bolts of uneven length out of one heart, none of them reaching the edge
    of the sheet. Uneven on purpose: a star with equal arms is a badge, not a discharge."""
    rng = np.random.default_rng(77)
    sheet = Sheet()
    arms = 6
    for i in range(arms):
        heading = 2 * math.pi * (i + rng.uniform(-0.3, 0.3)) / arms
        length = (0.44, 0.24, 0.36, 0.20, 0.41, 0.28)[i]
        steps = max(3, int(length / 0.055))
        points = kinked(rng, (0.5, 0.5), heading, length, steps, 0.5)
        # Thick at the heart and bright to the very tip: an arm tapers in WIDTH only. One that also dimmed went grey on
        # the road, and a grey line is the first thing daylight takes.
        widths = [0.024 * (1.0 - 0.85 * k / steps) + 0.002 for k in range(steps + 1)]
        gains = [1.0] * (steps + 1)
        sheet.path(points, widths, gains, veil=0.22)
        if length > 0.25:
            at = points[max(1, steps // 2)]
            fork(sheet, rng, at, heading + rng.choice((-1, 1)) * rng.uniform(0.5, 0.85), length * 0.42, 0.011, 1.0, 0)
    r = np.hypot(sheet.x - 0.5, sheet.y - 0.5)
    heart = np.clip(1.0 - r / 0.075, 0, 1) ** 0.5
    out = np.maximum(sheet.v, heart)
    return out * np.clip((0.5 - r) / 0.05, 0, 1)


def zack_spark_sheet():
    """Four sparks, two a side. Each is drawn along x in its own cell, head to the right: a short kinked dash with one
    or two prongs, fat at the head and gone to a point at the tail. Black to every cell's border, so no spark shows a
    sliver of its neighbour."""
    rng = np.random.default_rng(31)
    out = np.zeros((N, N), np.float32)
    half = N // 2
    for cell in range(4):
        sheet = Sheet(n=half)
        steps = 4
        points = kinked(rng, (0.14, 0.5 + rng.uniform(-0.05, 0.05)), 0.0, 0.70, steps, 0.42)
        widths = [0.012 + 0.030 * (k / steps) ** 0.8 for k in range(steps + 1)]
        widths[-1] *= 0.7
        sheet.path(points, widths, veil=0.18)
        for prong in range(1 + cell % 2):
            at = points[2 + prong]
            heading = (1 if (cell + prong) % 2 == 0 else -1) * rng.uniform(0.7, 1.1) + math.pi * (0.0 if prong else 1.0) * 0.0
            fork(sheet, rng, at, heading - math.pi * 0.0, rng.uniform(0.16, 0.24), 0.017, 0.9, 0)
        edge = np.minimum(np.minimum(sheet.x, 1 - sheet.x), np.minimum(sheet.y, 1 - sheet.y))
        image = sheet.v * np.clip((edge - 0.02) / 0.08, 0, 1)
        row, column = cell // 2, cell % 2
        out[row * half:(row + 1) * half, column * half:(column + 1) * half] = image
    return out


PICTURES = (("zack_bolt", zack_bolt), ("zack_crackle", zack_crackle), ("zack_ground_star", zack_ground_star), ("zack_spark_sheet", zack_spark_sheet))


def build():
    made = []
    for name, paint in PICTURES:
        image = paint()
        path = save(name, image)
        white, body, shown = float((image > 0.8).mean()), float(((image > 0.4) & (image <= 0.8)).mean()), float((image > 0.24).mean())
        print("%-18s white %.1f%%  body %.1f%%  above the veil %.1f%%  %s" % (name, white * 100, body * 100, shown * 100, path))
        made.append((name, image))
    return made


def contact(made, path):
    cells = []
    for name, image in made:
        cell = cv2.cvtColor(np.clip(image * 255, 0, 255).astype(np.uint8), cv2.COLOR_GRAY2BGR)
        cell = cv2.resize(cell, (N, N))
        cv2.putText(cell, name, (10, 26), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (80, 220, 255), 2)
        cells.append(cell)
    cv2.imwrite(path, np.hstack(cells))
    print("sheet", path)


if __name__ == "__main__":
    made = build()
    if "--sheet" in sys.argv:
        contact(made, sys.argv[sys.argv.index("--sheet") + 1])
