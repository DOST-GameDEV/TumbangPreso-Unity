#!/usr/bin/env python3
"""Trace the official PC Express artwork and extrude smooth letter contours in Blender."""

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

MIN_X, MAX_X = -2.03, 2.03
MIN_Y, MAX_Y = 3.095, 4.045


def connected_components(mask: np.ndarray):
    height, width = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    for start_y in range(height):
        for start_x in range(width):
            if not mask[start_y, start_x] or seen[start_y, start_x]:
                continue
            queue = deque([(start_y, start_x)])
            seen[start_y, start_x] = True
            pixels = []
            min_x = max_x = start_x
            min_y = max_y = start_y
            border = False
            while queue:
                y, x = queue.popleft()
                pixels.append((y, x))
                min_x, max_x = min(min_x, x), max(max_x, x)
                min_y, max_y = min(min_y, y), max(max_y, y)
                border |= x == 0 or y == 0 or x == width - 1 or y == height - 1
                for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                    if 0 <= yy < height and 0 <= xx < width and mask[yy, xx] and not seen[yy, xx]:
                        seen[yy, xx] = True
                        queue.append((yy, xx))
            yield pixels, (min_x, min_y, max_x, max_y), border


def official_letter_mask() -> np.ndarray:
    source = Image.open(SOURCE).convert("RGB")
    preview_width = 1536
    preview_height = round(source.height * preview_width / source.width)
    rgb = np.asarray(source.resize((preview_width, preview_height), Image.Resampling.LANCZOS))
    white = np.all(rgb >= 228, axis=2)
    kept = np.zeros_like(white)

    for pixels, bounds, border in connected_components(white):
        min_x, min_y, max_x, max_y = bounds
        centre_x = (min_x + max_x) * 0.5 / preview_width
        centre_y = (min_y + max_y) * 0.5 / preview_height
        registered_mark = centre_x > 0.90 and centre_y < 0.30
        if border or registered_mark or len(pixels) < 20:
            continue
        for y, x in pixels:
            kept[y, x] = True

    ys, xs = np.where(kept)
    if len(xs) == 0:
        raise RuntimeError("official logo produced no white letter components")
    crop = kept[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    target_width = 720
    target_height = max(1, round(crop.shape[0] * target_width / crop.shape[1]))
    reduced = Image.fromarray((crop * 255).astype(np.uint8)).resize(
        (target_width, target_height), Image.Resampling.LANCZOS)
    return np.asarray(reduced) >= 116


def dilate(mask: np.ndarray, radius: int) -> np.ndarray:
    height, width = mask.shape
    result = np.zeros_like(mask)
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            if dx * dx + dy * dy > radius * radius:
                continue
            y0, y1 = max(0, -dy), min(height, height - dy)
            x0, x1 = max(0, -dx), min(width, width - dx)
            result[y0 + dy:y1 + dy, x0 + dx:x1 + dx] |= mask[y0:y1, x0:x1]
    return result


def boundary_loops(mask: np.ndarray):
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


def model_loops(mask: np.ndarray):
    height, width = mask.shape
    result = []
    epsilon = 0.85 / width * (MAX_X - MIN_X)
    for pixel_loop in boundary_loops(mask):
        mapped = [(MAX_X - x / width * (MAX_X - MIN_X),
                   MAX_Y - y / height * (MAX_Y - MIN_Y))
                  for x, y in collinear_simplify(pixel_loop)]
        simplified = rdp(mapped + [mapped[0]], epsilon)[:-1]
        if len(simplified) >= 3 and abs(area(simplified)) > 0.000001:
            result.append([[round(x, 6), round(y, 6)] for x, y in simplified])
    return result


def main() -> None:
    if not BLENDER.exists():
        raise FileNotFoundError(f"Blender not found at {BLENDER}")
    letters = official_letter_mask()
    loops = model_loops(letters)
    payload = {
        "output": str(OBJ),
        "layers": [
            {"name": "logo_return_blue", "front": -3.205, "back": -3.162,
             "colour": [0.04, 0.19, 0.56], "emission": [0.015, 0.06, 0.18],
             "loops": loops},
            {"name": "logo_face_white", "front": -3.245, "back": -3.198,
             "colour": [0.985, 0.985, 0.98], "emission": [0.46, 0.46, 0.46],
             "loops": loops},
        ],
    }
    with tempfile.TemporaryDirectory(prefix="pcex-logo-") as temp:
        payload_path = Path(temp) / "logo.json"
        payload_path.write_text(json.dumps(payload), encoding="utf-8")
        subprocess.run([str(BLENDER), "--background", "--python", str(HELPER), "--", str(payload_path)],
                       check=True)
    print(f"{OBJ.relative_to(ROOT)}: {sum(len(x['loops']) for x in payload['layers'])} "
          "smooth contours, registered mark omitted")


if __name__ == "__main__":
    main()
