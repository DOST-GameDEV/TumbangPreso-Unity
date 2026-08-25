#!/usr/bin/env python3
"""Trace the official PC Express artwork and extrude it as a layered channel-letter sign.

⚠️⚠️ THE MARK IS THREE COLOURS AND ALL THREE CARRY IT. The first version of this tool kept
only the WHITE pixels of the supplied artwork and extruded that single silhouette. That
deletes more of the logo than it keeps: the blue "P" of EXPRESS disappears completely (the
v14 capture reads "PC EX RESS"), the italic X loses the blue and red wedges that give it its
shape and collapses into a starburst, the red outline and thin white keyline around the PC
monogram merge into one blob, and the slanted red-over-blue field the whole lockup is built
on is gone. The source is a clean flat three-colour image:

    #FFFFFF white   letter faces and the keyline around PC
    #D22630 red     the lower/right field band and the outline of the PC monogram
    #003DA5 blue    the upper/left field band and the outline of EXPRESS

⚠️ SO THE TRACE SEGMENTS BY COLOUR AND EMITS FIVE STACKED PLATES, back to front:

    0  panel         the whole slanted parallelogram, BLUE. Every blue pixel in the artwork
                     is either this plate or a hole in a plate above it, which is why the
                     EXPRESS outline and the P/R counters need no geometry of their own.
    1  field red     the red band. A hole here shows the blue panel 8 mm behind it, which is
                     exactly how the blue keyline around EXPRESS reads on the real sign.
    2  keyline white the thin white line around the PC monogram.
    3  letter red    the red outline of PC, plus the P counter and the small red wedge in
                     the X, which are enclosed and would otherwise show panel blue.
    4  letter white  the white faces: the P and C bodies, and every letter of EXPRESS.

Masks come off ONE quantised image, so neighbouring plates share their pixel boundary exactly
and no plate can leave a seam of the plate behind it.

⚠️ THE REGISTERED MARK IS FORCED TO FIELD RED RATHER THAN FILTERED BY COMPONENT SIZE. It sits
INSIDE the panel at the top right, so the old "drop small white components near the corner"
rule left its red ring behind the moment the red layers started being traced.
`docs/Ilalim_Ng_Tulay.md` § 8.3: it is not mounted on the real storefront.

Run it, then re-run `PcExpressSignAuthor` so the lightbox behind it is rebuilt to match:

    python tools/build_pc_express_logo_mesh.py
"""

from __future__ import annotations

from collections import defaultdict, deque
import json
from pathlib import Path
import subprocess
import tempfile

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets" / "TumbangPreso" / "Art" / "models" / "textures" / "pc_express_horizontal_rgb.png"
OBJ = ROOT / "Assets" / "TumbangPreso" / "Art" / "models" / "env_pc_express_logo_3d.obj"
BLENDER = Path(r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe")
HELPER = ROOT / "tools" / "blender_extrude_pc_express_logo.py"

# ⚠️ THE MODEL SPACE IS THE STORE'S OWN. `env_pc_express_store.obj` faces -Z; its fascia board
# is x -2.15..2.15 and y 3.00..4.137, measured out of the file and repeated in
# `PcExpressSignAuthor`. The artwork is 3.785:1, so the plate is set to the board width less a
# 30 mm reveal and its height SOLVED from that, never typed in: a typed height is how a traced
# logo ends up subtly stretched and nobody can say by how much.
BOARD_MIN_X, BOARD_MAX_X = -2.15, 2.15
BOARD_MIN_Y, BOARD_MAX_Y = 3.00, 4.137
REVEAL = 0.030

# Working resolution of the trace. 900 px across the 4.24 m plate is 4.7 mm per pixel, which is
# finer than the 12 mm relief between plates, so the contour is not the limiting detail.
TRACE_WIDTH = 900

WHITE = (255, 255, 255)
RED = (210, 38, 48)
BLUE = (0, 61, 165)
PALETTE = np.array([WHITE, RED, BLUE])
IDX_WHITE, IDX_RED, IDX_BLUE = 0, 1, 2

# ⚠️⚠️ THE EMISSION VALUES WERE MORE THAN HALVED AFTER v16, AND THE SIGN LIGHT WITH
# THEM. The plates shipped at 0.30 to 0.52 Ke while `IlalimNgTulayBuilder` also aimed a
# 1.5-intensity point light at them from 0.45 m away, so in `ilalim_pcexpress_close_v16.png`
# the white letters clip to flat paper and the red field washes out to pink across its
# middle. The mark was traced correctly and unreadable in the same frame, which is the worst
# possible outcome: it looks like the trace failed. A lit acrylic face in daylight is only
# slightly brighter than the wall it is bolted to, and the toon ramp supplies the rest.
#
# ⚠️ NAMED, NOT LITERAL, BECAUSE THE ORDER IS THE WHOLE DESIGN. Each plate stands 6 to 8 mm
# proud of the one behind it and the mark reaches 48 mm off the lightbox face at -3.130. Less
# than this and the toon shader gives the five colours no edge to separate on at street
# distance; more and the fascia stops reading as one flush sign from the overview.
PLATE_FRONT_BACK = {
    "panel": (-3.150, -3.125),
    "field": (-3.158, -3.140),
    "keyline": (-3.164, -3.148),
    "letter_red": (-3.170, -3.154),
    "letter_white": (-3.178, -3.160),
}


def quantised(width: int) -> np.ndarray:
    """The artwork as one of three palette indices per pixel, area-averaged down to `width`."""
    source = Image.open(SOURCE).convert("RGB")
    height = round(source.height * width / source.width)
    rgb = np.asarray(source.resize((width, height), Image.Resampling.BOX)).astype(np.int32)
    distance = ((rgb[:, :, None, :] - PALETTE[None, None, :, :]) ** 2).sum(axis=3)
    return distance.argmin(axis=2)


def registered_mark_region(shape) -> np.ndarray:
    """The corner the ® occupies.

    ⚠️ IT IS INTERSECTED WITH THE PANEL BY THE CALLER, AND THAT IS NOT OPTIONAL. Forcing the
    raw rectangle to field red before the panel is solved paints the WHITE PAGE above the
    lockup's slanted top edge red too, and the panel then grows a square red horn out of its
    top right corner. That is what the first run of this rewrite produced.
    """
    height, width = shape
    mark = np.zeros(shape, dtype=bool)
    mark[: int(height * 0.22), int(width * 0.930):] = True
    return mark


def components(mask: np.ndarray):
    """Every 4-connected run of `mask`, largest first, as boolean masks."""
    height, width = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    found = []

    for start_y in range(height):
        for start_x in range(width):
            if not mask[start_y, start_x] or seen[start_y, start_x]:
                continue
            queue = deque([(start_y, start_x)])
            seen[start_y, start_x] = True
            pixels = []
            while queue:
                y, x = queue.popleft()
                pixels.append((y, x))
                for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                    if 0 <= yy < height and 0 <= xx < width and mask[yy, xx] and not seen[yy, xx]:
                        seen[yy, xx] = True
                        queue.append((yy, xx))
            piece = np.zeros_like(mask)
            for y, x in pixels:
                piece[y, x] = True
            found.append((len(pixels), piece))

    found.sort(key=lambda item: item[0], reverse=True)
    return found


def fill_holes(mask: np.ndarray) -> np.ndarray:
    """`mask` with every enclosed hole closed, by flooding the outside from the border."""
    height, width = mask.shape
    outside = np.zeros_like(mask)
    queue = deque()

    for x in range(width):
        for y in (0, height - 1):
            if not mask[y, x] and not outside[y, x]:
                outside[y, x] = True
                queue.append((y, x))
    for y in range(height):
        for x in (0, width - 1):
            if not mask[y, x] and not outside[y, x]:
                outside[y, x] = True
                queue.append((y, x))

    while queue:
        y, x = queue.popleft()
        for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= yy < height and 0 <= xx < width and not mask[yy, xx] and not outside[yy, xx]:
                outside[yy, xx] = True
                queue.append((yy, xx))

    return ~outside


def boundary_loops(mask: np.ndarray):
    """Closed pixel-edge loops around every run and every hole in `mask`."""
    height, width = mask.shape
    outgoing = defaultdict(list)
    for y in range(height):
        for x in range(width):
            if not mask[y, x]:
                continue
            if y == 0 or not mask[y - 1, x]:
                outgoing[(x, y)].append((x + 1, y))
            if x == width - 1 or not mask[y, x + 1]:
                outgoing[(x + 1, y)].append((x + 1, y + 1))
            if y == height - 1 or not mask[y + 1, x]:
                outgoing[(x + 1, y + 1)].append((x, y + 1))
            if x == 0 or not mask[y, x - 1]:
                outgoing[(x, y + 1)].append((x, y))

    while outgoing:
        start = next(iter(outgoing))
        current = start
        loop = [start]
        for _ in range((width + 1) * (height + 1) * 4):
            choices = outgoing.get(current)
            if not choices:
                break
            next_point = choices.pop()
            if not choices:
                del outgoing[current]
            current = next_point
            if current == start:
                break
            loop.append(current)
        if current == start and len(loop) >= 4:
            yield loop


def collinear_simplify(points):
    result = []
    for point in points:
        result.append(point)
        while len(result) >= 3:
            a, b, c = result[-3:]
            if (b[0] - a[0]) * (c[1] - b[1]) != (b[1] - a[1]) * (c[0] - b[0]):
                break
            result.pop(-2)
    return result


def distance_to_line(point, start, end):
    if start == end:
        return ((point[0] - start[0]) ** 2 + (point[1] - start[1]) ** 2) ** 0.5
    numerator = abs((end[1] - start[1]) * point[0] - (end[0] - start[0]) * point[1] +
                    end[0] * start[1] - end[1] * start[0])
    denominator = ((end[1] - start[1]) ** 2 + (end[0] - start[0]) ** 2) ** 0.5
    return numerator / denominator


def rdp(points, epsilon):
    if len(points) < 3:
        return points
    distances = [distance_to_line(point, points[0], points[-1]) for point in points[1:-1]]
    if not distances or max(distances) <= epsilon:
        return [points[0], points[-1]]
    index = distances.index(max(distances)) + 1
    return rdp(points[:index + 1], epsilon)[:-1] + rdp(points[index:], epsilon)


def area(points):
    return 0.5 * sum(points[i][0] * points[(i + 1) % len(points)][1] -
                     points[(i + 1) % len(points)][0] * points[i][1]
                     for i in range(len(points)))


def model_loops(mask: np.ndarray, extents):
    """Pixel loops mapped into model space and simplified. Model +X is artwork -X: the store
    faces -Z and its fascia is read from the street, so the plate is mirrored here rather than
    by a negative scale on the instance, which would flip its normals."""
    min_x, max_x, min_y, max_y = extents
    height, width = mask.shape
    result = []
    epsilon = 0.85 / width * (max_x - min_x)

    for pixel_loop in boundary_loops(mask):
        mapped = [(max_x - x / width * (max_x - min_x),
                   max_y - y / height * (max_y - min_y))
                  for x, y in collinear_simplify(pixel_loop)]
        simplified = rdp(mapped + [mapped[0]], epsilon)[:-1]
        if len(simplified) >= 3 and abs(area(simplified)) > 0.000001:
            result.append([[round(x, 6), round(y, 6)] for x, y in simplified])

    return result


def union(masks):
    total = np.zeros_like(masks[0])
    for mask in masks:
        total |= mask
    return total


def main() -> None:
    if not BLENDER.exists():
        raise FileNotFoundError(f"Blender not found at {BLENDER}")

    classes = quantised(TRACE_WIDTH)
    height, width = classes.shape

    red = classes == IDX_RED
    blue = classes == IDX_BLUE
    white = classes == IDX_WHITE

    red_parts = components(red)
    blue_parts = components(blue)
    if not red_parts or not blue_parts:
        raise RuntimeError("the artwork did not segment into a red band and a blue band")

    # The two field bands are the two largest runs of their colour by a wide margin: the red
    # band is 124,879 px against 14,009 for the whole PC monogram outline.
    field_red = red_parts[0][1]
    field_blue = blue_parts[0][1]
    panel = fill_holes(field_red | field_blue)

    # The ® is inside the panel, so it is answered by filling its footprint with field red
    # rather than by dropping a component. See `registered_mark_region`.
    mark = registered_mark_region(classes.shape) & panel
    field_red = (field_red | mark) & panel

    # Everything else is lettering, and only lettering that lands ON the panel counts.
    letter_red = (red & ~field_red) & panel & ~mark
    letter_white = white & panel & ~mark

    white_parts = [(count, piece) for count, piece in components(letter_white) if count > 200]
    if len(white_parts) < 4:
        raise RuntimeError(f"expected the PC keyline and the EXPRESS words, found {len(white_parts)}")

    # ⚠️ THE KEYLINE IS FOUND BY SHAPE, NOT BY INDEX. It is the one white run that is a thin
    # line wrapped around two letters: it spans the PC monogram's whole bounding box while
    # filling under a fifth of it. Indexing into a size-sorted list instead would silently pick
    # a different piece the day the trace resolution moves.
    keyline = None
    bodies = []
    for count, piece in white_parts:
        ys, xs = np.where(piece)
        box = (xs.max() - xs.min() + 1) * (ys.max() - ys.min() + 1)
        if keyline is None and xs.max() < width * 0.45 and count < box * 0.20:
            keyline = piece
            continue
        bodies.append(piece)

    if keyline is None:
        raise RuntimeError("no thin white keyline was found around the PC monogram")

    extents_width = (BOARD_MAX_X - BOARD_MIN_X) - REVEAL * 2.0
    extents_height = extents_width * height / width
    centre_y = (BOARD_MIN_Y + BOARD_MAX_Y) * 0.5
    extents = (
        BOARD_MIN_X + REVEAL,
        BOARD_MAX_X - REVEAL,
        centre_y - extents_height * 0.5,
        centre_y + extents_height * 0.5,
    )

    plates = [
        ("panel", "pcex_logo_panel_blue", panel,
         (0.043, 0.176, 0.545), (0.010, 0.032, 0.086)),
        ("field", "pcex_logo_field_red", field_red,
         (0.800, 0.130, 0.165), (0.118, 0.020, 0.024)),
        ("keyline", "pcex_logo_keyline_white", keyline,
         (0.975, 0.975, 0.968), (0.150, 0.150, 0.148)),
        ("letter_red", "pcex_logo_letter_red", letter_red,
         (0.845, 0.140, 0.175), (0.130, 0.022, 0.026)),
        ("letter_white", "pcex_logo_letter_white", union(bodies),
         (0.985, 0.985, 0.980), (0.205, 0.205, 0.203)),
    ]

    layers = []
    for key, name, mask, colour, emission in plates:
        front, back = PLATE_FRONT_BACK[key]
        loops = model_loops(mask, extents)
        if not loops:
            raise RuntimeError(f"plate '{key}' traced to nothing")
        layers.append({
            "name": name,
            "front": front,
            "back": back,
            "colour": list(colour),
            "emission": list(emission),
            "loops": loops,
        })

    payload = {"output": str(OBJ), "layers": layers}
    with tempfile.TemporaryDirectory(prefix="pcex-logo-") as temp:
        payload_path = Path(temp) / "logo.json"
        payload_path.write_text(json.dumps(payload), encoding="utf-8")
        subprocess.run([str(BLENDER), "--background", "--python", str(HELPER), "--", str(payload_path)],
                       check=True)

    span = f"x {extents[0]:.3f}..{extents[1]:.3f}, y {extents[2]:.3f}..{extents[3]:.3f}"
    print(f"{OBJ.relative_to(ROOT)}: {len(layers)} colour plates, "
          f"{sum(len(layer['loops']) for layer in layers)} contours, {span}, "
          "registered mark omitted")


if __name__ == "__main__":
    main()
