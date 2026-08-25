#!/usr/bin/env python3
"""Emit the warm atlas replacements used by Ilalim ng Tulay.

Kenney's commercial, industrial, roads and train kits each use one colormap atlas. Tinting a
renderer therefore multiplies already-coloured pixels and cannot independently move the blue
and orange swatches away from the game's role hues. This tool replaces the whole atlas while
preserving every model's UV layout.
"""

from __future__ import annotations

import colorsys
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
KITS = ROOT / "Assets" / "TumbangPreso" / "Art" / "models" / "kits"

# Output hues stay clear of offense orange (24 degrees) and defence blue (208 degrees).
# Saturation is capped because these are world surfaces, not role or hero signals.
VARIANTS = {
    "warm-a": {
        "red": 356.0,
        "orange": 43.0,
        "yellow": 48.0,
        "green": 157.0,
        "blue": 165.0,
        "purple": 324.0,
    },
    "warm-b": {
        "red": 350.0,
        "orange": 157.0,
        "yellow": 43.0,
        "green": 48.0,
        "blue": 350.0,
        "purple": 356.0,
    },
    "warm-c": {
        "red": 356.0,
        "orange": 350.0,
        "yellow": 157.0,
        "green": 43.0,
        "blue": 48.0,
        "purple": 324.0,
    },
    "lrt": {
        "red": 350.0,
        "orange": 46.0,
        "yellow": 46.0,
        "green": 155.0,
        "blue": 292.0,
        "purple": 292.0,
    },
}

TARGETS = {
    "car": ("warm-a", "warm-b", "warm-c"),
    "commercial": ("warm-a", "warm-b", "warm-c"),
    "industrial": ("warm-a", "warm-b"),
    "factory": ("warm-a",),
    "roads": ("warm-a",),
    "train": ("lrt",),
}


def family(hue: float) -> str:
    degrees = hue * 360.0
    if degrees < 18.0 or degrees >= 338.0:
        return "red"
    if degrees < 43.0:
        return "orange"
    if degrees < 72.0:
        return "yellow"
    if degrees < 178.0:
        return "green"
    if degrees < 252.0:
        return "blue"
    return "purple"


def remap(pixel: tuple[int, int, int, int], hues: dict[str, float]) -> tuple[int, int, int, int]:
    r, g, b, a = pixel
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)

    if v < 0.055:
        return r, g, b, a

    if s < 0.115:
        # Warm every neutral toward concrete or cream without turning shadows brown.
        warmth = 0.035 + 0.055 * v
        out_h = 42.0 / 360.0
        out_s = warmth
    else:
        out_h = hues[family(h)] / 360.0
        out_s = min(0.42, 0.12 + s * 0.34)

    # Pull the kit's fluorescent highlights into the same matte value band as the other maps.
    out_v = min(0.94, 0.055 + v * 0.89)
    rr, gg, bb = colorsys.hsv_to_rgb(out_h, out_s, out_v)
    return round(rr * 255), round(gg * 255), round(bb * 255), a


def hue_distance(a: float, b: float) -> float:
    delta = abs(a - b) % 360.0
    return min(delta, 360.0 - delta)


def assert_role_clear(image: Image.Image, path: Path) -> None:
    for r, g, b, _ in image.get_flattened_data():
        h, s, _ = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
        if s < 0.30:
            continue
        degrees = h * 360.0
        if hue_distance(degrees, 24.0) < 17.0 or hue_distance(degrees, 208.0) < 25.0:
            raise ValueError(f"{path}: saturated pixel {(r, g, b)} approaches a role hue")


def main() -> None:
    for kit, variants in TARGETS.items():
        source_path = KITS / kit / "Textures" / "colormap.png"
        source = Image.open(source_path).convert("RGBA")

        for variant in variants:
            output = Image.new("RGBA", source.size)
            output.putdata([remap(pixel, VARIANTS[variant])
                            for pixel in source.get_flattened_data()])
            output_path = source_path.with_name(f"tumbang-{variant}.png")
            assert_role_clear(output, output_path)
            output.save(output_path, optimize=True)
            print(output_path.relative_to(ROOT))


if __name__ == "__main__":
    main()
