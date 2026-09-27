"""Paints the voodoo doll's cloth atlas (HERO-10 v3, the VOODOO DOLL ultimate, model v11+) as PIXEL ART.

    python tools/paint_phaister_doll_cloth.py            # writes ArtSource/phaister/doll-20260927/cloth.png to look at
    (tools/build_phaister_doll_voxel.py imports it and embeds the same image in phaister-doll.glb)

Owner, 2026-09-27: *"thoroughly texture it"*, with a reference render of a burlap voodoo doll
(`ArtSource/phaister/doll-20260927/owner-reference-20260927.png`); then, on the first textured render, *"figure out a way to make it
feel like its part of teh game and not ultra realistic haha"*.

⚠️⚠️ SO IT IS NOT A PHOTOGRAPH OF BURLAP. The first paint (v11 to v14) was a simulated weave with wandering threads, loose hairs,
soft stains and fibre noise; on a cast of flat-coloured blocks it read as a real sack pasted onto a toy. The cast's own language
is flat colour, hard edges and the voxel: so every cloth here is a small PIXEL TILE typed by hand, three or four flat tones of one
cloth (a lit thread, a mid thread, a thread diving under its neighbour, the dark hole between), repeated and scaled up six times
with hard edges. It still says "burlap" (the over-under weave is drawn), but as a game texture drawn in pixels, like the blocks it
sits on. No gradient, no noise, no blur; the only irregularities are typed (a darker patch of threads, a slub).

⚠️⚠️ WHERE THE PAINT MAY GO. The toon shader remaps every texel in Unity rows 0 to 7 of a 16 x 16 grid to the palette and samples
the texture everywhere else (`Toon.shader`, the `_UsePalette` block). glTFast flips V on import, so the texture's TOP HALF (image
rows 0 to 1023 of 2048) is what reaches the screen as paint; the bottom half is where the palette cells sit (`cell_uv`), and is
filled with the flat palette only so the file reads sensibly. Every region below is in the top half, with a margin, so mip levels
never bleed one cloth into the next.
"""
import os

import numpy as np
from PIL import Image, ImageDraw

SIZE = 2048
OUT = "ArtSource/phaister/doll-20260927/cloth.png"

# One art pixel is PIXEL texels; the model maps DENSITY texels to a rig unit, so an art pixel is 1/150 of a rig unit (1.6 cm in
# the game): about a fifty-pixel-wide head, the same chunky scale as the cast's blocks.
PIXEL = 6
DENSITY = 900.0

# (x, y, width, height) in image pixels, top-left origin, all inside the top half.
REGIONS = {
    "burlap":        (8, 8, 496, 496),        # the body sack
    "burlap-b":      (520, 8, 496, 496),      # a second sack panel: the legs, the back
    "head":          (1032, 8, 496, 496),     # the head, lighter so the face reads first
    "mantle":        (1544, 8, 496, 496),     # the ragged capelet, coarser and darker
    "wrap":          (8, 520, 240, 496),      # her purple cloth wound round the arms and ankles
    "rope":          (264, 520, 112, 496),    # the gold rope belt; its length runs down the region
    "stuffing":      (392, 520, 112, 112),
    "straw":         (392, 648, 112, 240),    # the tuft at the crown
    "mitten":        (392, 904, 112, 112),
    "patch-chest":   (520, 520, 240, 240),    # charcoal diamond, lilac crosshatch (her coat cloth)
    "patch-belly":   (776, 520, 240, 240),    # her magenta, a charcoal rune
    "patch-back":    (1032, 520, 240, 240),   # charcoal, a gold rune
    "patch-head":    (1288, 520, 240, 240),   # crimson diamond, a gold rune
    "patch-thigh":   (1544, 520, 240, 240),   # crimson, a gold X
    "patch-calf":    (1800, 520, 240, 240),   # charcoal, a lilac X
    "tag":           (520, 776, 112, 240),    # a bone tag, a crimson rune
    "pouch":         (648, 776, 240, 240),    # charcoal pouch cloth
    "bundle":        (904, 776, 240, 240),    # the little purple bundle doll
    "sole":          (1160, 776, 240, 240),   # under the feet
    "hem":           (1416, 776, 240, 240),   # torn flaps: the sack's edge, a shade darker
}
PAD = 8


def _hex(code):
    return tuple(int(code[i:i + 2], 16) for i in (0, 2, 4))


# ---------------------------------------------------------------------------------------------------------------------
# THE TILES, typed pixel by pixel. L a lit thread, M its shaded side, S a thread diving under its neighbour, D the hole.
# ⚠️ S stays close to M: only the one-pixel holes are dark. With S dark too, the dive rows joined into lines and the sack read
# as plaid (render v15).
# ---------------------------------------------------------------------------------------------------------------------
# A plain weave, threads two pixels wide with a one-pixel hole: the warp on top at two crossings, the weft at the other two.
WEAVE_6 = (
    "LMSLLS",
    "LMSMMS",
    "SSDSSD",
    "LLSLMS",
    "MMSLMS",
    "SSDSSD",
)
# The capelet's coarser weave: threads three pixels wide.
WEAVE_8 = (
    "LLMSLLLS",
    "LLMSMMMS",
    "LLMSMMMS",
    "SSSDSSSD",
    "LLLSLLMS",
    "MMMSLLMS",
    "MMMSLLMS",
    "SSSDSSSD",
)
# Her purple cloth and the little bundles: a tight weave with no holes.
TWILL_4 = (
    "LMMS",
    "MLMM",
    "MMLM",
    "SMML",
)
# A chunky soft weave (a plush's knit): threads three pixels wide, no holes, the tones close together.
CHUNKY_8 = (
    "LLLMLLLM",
    "MMMSLLLM",
    "LLLMMMMS",
    "MMMSLLLM",
    "LLLMLLLM",
    "LLLMMMMS",
    "MMMSLLLM",
    "LLLMMMMS",
)
# Felt for the patches: almost flat, a faint grain.
FELT_4 = (
    "MMLM",
    "MMMM",
    "LMMM",
    "MMMS",
)


def tile_art(aw, ah, tile, tones, specks=(), stains=()):
    """A cloth at art resolution: the tile repeated, then the typed specks (single threads a tone off, `(x, y, tone)`) and
    stains (flat blobs one tone darker, `(x, y, radius)`), both in art pixels."""
    th, tw = len(tile), len(tile[0])
    art = np.zeros((ah, aw, 3), np.uint8)
    for y in range(ah):
        row = tile[y % th]
        for x in range(aw):
            art[y, x] = tones[row[x % tw]]
    darker = {"L": "M", "M": "S", "S": "D", "D": "D"}
    for sx, sy, radius in stains:
        for y in range(max(0, sy - radius), min(ah, sy + radius + 1)):
            for x in range(max(0, sx - radius), min(aw, sx + radius + 1)):
                if (x - sx) ** 2 + (y - sy) ** 2 <= radius * radius:
                    art[y, x] = tones[darker[tile[y % th][x % tw]]]
    for x, y, tone in specks:
        if 0 <= x < aw and 0 <= y < ah:
            art[y, x] = tones[tone]
    return art


def rope_art(aw, ah, tones):
    """Three strands twisted: diagonal bands, a dark groove between each."""
    art = np.zeros((ah, aw, 3), np.uint8)
    band = "DSMLM"
    for y in range(ah):
        for x in range(aw):
            art[y, x] = tones[band[(x + y) % len(band)]]
    return art


def straw_art(aw, ah, columns, tones):
    """Straw standing up the region: each column its typed tone, a dark line between the stalks."""
    art = np.zeros((ah, aw, 3), np.uint8)
    for x in range(aw):
        art[:, x] = tones[columns[x % len(columns)]]
    return art


def stuffing_art(aw, ah, tones, puffs):
    art = np.zeros((ah, aw, 3), np.uint8)
    art[:] = tones["M"]
    for x, y, tone in puffs:
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
            if 0 <= x + dx < aw and 0 <= y + dy < ah:
                art[y + dy, x + dx] = tones[tone]
    return art


def patch_art(aw, ah, cloth_tones, motif, thread, shadow, border, stitches):
    """A patch in pixels: felt, a one-pixel darker edge, its motif stitched two pixels wide with a one-pixel shadow under it,
    and pale whip stitches over its edge at typed places (per side: top, right, bottom, left, as fractions along it)."""
    art = tile_art(aw, ah, FELT_4, cloth_tones)
    art[0, :] = art[-1, :] = cloth_tones["S"]
    art[:, 0] = art[:, -1] = cloth_tones["S"]
    img = Image.fromarray(art, "RGB")
    d = ImageDraw.Draw(img)
    for stroke in motif:
        pts = [(round(u * (aw - 1)), round(v * (ah - 1))) for u, v in stroke]
        d.line([(x + 1, y + 1) for x, y in pts], fill=shadow, width=2)
        d.line(pts, fill=thread, width=2)
    places = (
        lambda t: [(round(t * (aw - 1)), 0), (round(t * (aw - 1)) + 1, 1), (round(t * (aw - 1)) + 1, 2)],
        lambda t: [(aw - 1, round(t * (ah - 1))), (aw - 2, round(t * (ah - 1)) + 1), (aw - 3, round(t * (ah - 1)) + 1)],
        lambda t: [(round(t * (aw - 1)), ah - 1), (round(t * (aw - 1)) - 1, ah - 2), (round(t * (aw - 1)) - 1, ah - 3)],
        lambda t: [(0, round(t * (ah - 1))), (1, round(t * (ah - 1)) - 1), (2, round(t * (ah - 1)) - 1)],
    )
    for side, where in zip(places, stitches):
        for t in where:
            for x, y in side(t):
                if 0 <= x < aw and 0 <= y < ah:
                    img.putpixel((x, y), border)
    return np.asarray(img, np.uint8)


def _up(art, w, h):
    """Art pixels scaled up PIXEL times with hard edges, cut to the region."""
    big = np.repeat(np.repeat(art, PIXEL, axis=0), PIXEL, axis=1)
    return big[:h, :w]


def _art_size(name):
    w, h = REGIONS[name][2], REGIONS[name][3]
    return -(-w // PIXEL), -(-h // PIXEL), w, h


def tones(light, mid, dive, hole):
    return {"L": _hex(light), "M": _hex(mid), "S": _hex(dive), "D": _hex(hole)}


# ---------------------------------------------------------------------------------------------------------------------
# THE CLOTHS, each typed
# ---------------------------------------------------------------------------------------------------------------------
STYLES = ("pixel", "felt", "painted", "chunky")


def _shade(code, k):
    return "".join(f"{min(255, max(0, round(int(code[i:i + 2], 16) * k))):02x}" for i in (0, 2, 4))


def sack_art(style, aw, ah, tile, t, specks, seed):
    """One sack cloth in the chosen style, from its typed tones (`t`: the pixel weave's four).
    pixel: the over-under weave with its holes (v16). felt: nearly flat, a faint grain. painted: flat, with short drawn thread
    strokes scattered over it. chunky: a big soft weave with no holes, the tones close together."""
    if style == "pixel":
        return tile_art(aw, ah, tile, t, specks=specks)
    mid = "".join(f"{v:02x}" for v in t["M"])
    if style == "felt":
        return tile_art(aw, ah, FELT_4, tones(_shade(mid, 1.05), mid, _shade(mid, 0.95), _shade(mid, 0.9)))
    if style == "chunky":
        return tile_art(aw, ah, CHUNKY_8, tones(_shade(mid, 1.08), mid, _shade(mid, 0.9), _shade(mid, 0.84)))
    art = np.zeros((ah, aw, 3), np.uint8)
    art[:] = t["M"]
    rng = np.random.default_rng(seed)
    light, dark = _hex(_shade(mid, 1.12)), _hex(_shade(mid, 0.84))
    for _ in range(aw * ah // 26):
        x, y = int(rng.integers(0, aw)), int(rng.integers(0, ah))
        length = int(rng.integers(2, 4))
        colour = dark if rng.random() < 0.6 else light
        if rng.random() < 0.5:
            art[y, x:x + length] = colour
        else:
            art[y:y + length, x] = colour
    return art


def paint(style="pixel"):
    img = np.zeros((SIZE, SIZE, 3), np.uint8)

    def put(name, art):
        """The cloth into its region, and its own edge texels smeared PAD outward so a mip level never mixes in black."""
        x, y, w, h = REGIONS[name]
        a = _up(art, w, h)
        padded = np.pad(a, ((PAD, PAD), (PAD, PAD), (0, 0)), mode="edge")
        y0, x0 = max(0, y - PAD), max(0, x - PAD)
        img[y0:y + h + PAD, x0:x + w + PAD] = padded[y0 - (y - PAD):, x0 - (x - PAD):][:y + h + PAD - y0, :x + w + PAD - x0]

    aw, ah, _, _ = _art_size("burlap")
    put("burlap", sack_art(style, aw, ah, WEAVE_6, tones("c89a5c", "b88b52", "a67d49", "6e4c2a"),
                           ((5, 7, "M"), (22, 13, "L"), (40, 31, "S"), (61, 44, "M"), (13, 58, "L"), (70, 70, "S")), 11))
    aw, ah, _, _ = _art_size("burlap-b")
    put("burlap-b", sack_art(style, aw, ah, WEAVE_6, tones("bd9660", "ad8856", "9c7a4c", "664a2e"),
                             ((9, 4, "S"), (33, 26, "L"), (51, 11, "M"), (27, 63, "S"), (74, 49, "L")), 23))
    aw, ah, _, _ = _art_size("head")
    put("head", sack_art(style, aw, ah, WEAVE_6, tones("d6ab70", "c69c64", "b48c58", "7c5a36"),
                         ((12, 20, "M"), (47, 9, "L"), (66, 58, "S"), (30, 72, "M")), 37))
    aw, ah, _, _ = _art_size("mantle")
    put("mantle", sack_art(style, aw, ah, WEAVE_8, tones("b98a52", "a67a48", "946c3e", "5a3e22"),
                           ((6, 12, "S"), (29, 40, "L"), (55, 18, "M"), (71, 66, "S")), 41))
    aw, ah, _, _ = _art_size("wrap")
    put("wrap", tile_art(aw, ah, TWILL_4, tones("7a3cb4", "5f2a96", "4a1e78", "2e1050")))
    aw, ah, _, _ = _art_size("rope")
    put("rope", rope_art(aw, ah, tones("f2c255", "d6a23a", "a8781c", "6a4a10")))
    aw, ah, _, _ = _art_size("stuffing")
    put("stuffing", stuffing_art(aw, ah, tones("fff6de", "f0e3c4", "d8c7a2", "b8a680"),
                                 ((2, 3, "L"), (9, 6, "S"), (14, 1, "L"), (5, 12, "S"), (12, 14, "L"), (16, 9, "S"))))
    aw, ah, _, _ = _art_size("straw")
    put("straw", straw_art(aw, ah, "LLMDMMLSDLMMD", tones("ecd494", "d4b670", "b8944e", "7a5c2c")))
    aw, ah, _, _ = _art_size("mitten")
    put("mitten", sack_art(style, aw, ah, WEAVE_6, tones("b88c56", "a87e4c", "967044", "5e4228"), ((4, 9, "L"), (14, 3, "S")), 73))
    aw, ah, _, _ = _art_size("sole")
    put("sole", sack_art(style, aw, ah, WEAVE_6, tones("8e6a44", "80603c", "725434", "46321e"), (), 79))
    aw, ah, _, _ = _art_size("hem")
    put("hem", sack_art(style, aw, ah, WEAVE_6, tones("b08450", "a07748", "8e6a40", "563c22"), ((7, 5, "S"), (30, 26, "L")), 83))
    aw, ah, _, _ = _art_size("pouch")
    put("pouch", tile_art(aw, ah, TWILL_4, tones("4a3e58", "3a3046", "2c2436", "1c1624")))
    aw, ah, _, _ = _art_size("bundle")
    put("bundle", tile_art(aw, ah, TWILL_4, tones("a868de", "8c4ec4", "7038a8", "4a2078")))

    # The tag: bone card, a darker edge, a punched hole, a crimson rune in pixels.
    aw, ah, _, _ = _art_size("tag")
    tag = Image.fromarray(tile_art(aw, ah, FELT_4, tones("f6ecd2", "eadcbc", "d4c29c", "b8a47c")), "RGB")
    d = ImageDraw.Draw(tag)
    d.rectangle((0, 0, aw - 1, ah - 1), outline=_hex("c8b48a"))
    d.rectangle((aw // 2 - 1, 2, aw // 2, 3), fill=_hex("4a3624"))
    crimson = _hex("a8182c")
    for stroke in (((0.50, 0.30), (0.50, 0.86)), ((0.50, 0.46), (0.22, 0.32)), ((0.50, 0.46), (0.78, 0.32)),
                   ((0.50, 0.66), (0.26, 0.80)), ((0.50, 0.66), (0.74, 0.80))):
        d.line([(round(u * (aw - 1)), round(v * (ah - 1))) for u, v in stroke], fill=crimson, width=2)
    put("tag", np.asarray(tag, np.uint8))

    # The patches, each its own cloth, motif and stitching (art resolution 40 x 40).
    charcoal = tones("4e4260", "3e344c", "2e263a", "1c1626")
    crimson_felt = tones("c42a44", "a8203a", "86182c", "58101c")
    magenta_felt = tones("f04a9a", "d8307e", "b02266", "74143e")
    lilac, gold, ink, pale, dark = _hex("c486f0"), _hex("f2b840"), _hex("2a1a36"), _hex("f4e6c2"), _hex("140c18")

    def patch(name, cloth, motif, thread, stitches):
        aw, ah, _, _ = _art_size(name)
        put(name, patch_art(aw, ah, cloth, motif, thread, dark, pale, stitches))

    patch("patch-chest", charcoal, [
        [(0.10, 0.32), (0.68, 0.90)], [(0.10, 0.10), (0.90, 0.90)], [(0.32, 0.10), (0.90, 0.68)],
        [(0.10, 0.68), (0.68, 0.10)], [(0.10, 0.90), (0.90, 0.10)], [(0.32, 0.90), (0.90, 0.32)],
    ], lilac, ([0.20, 0.50, 0.80], [0.18, 0.48, 0.78], [0.22, 0.52, 0.82], [0.16, 0.46, 0.76]))
    patch("patch-belly", magenta_felt, [
        [(0.50, 0.14), (0.50, 0.88)], [(0.50, 0.36), (0.24, 0.16)], [(0.50, 0.36), (0.77, 0.17)],
        [(0.50, 0.62), (0.22, 0.84)], [(0.50, 0.62), (0.79, 0.85)], [(0.30, 0.50), (0.70, 0.50)],
    ], ink, ([0.20, 0.50, 0.80], [0.22, 0.54, 0.84], [0.16, 0.48, 0.78], [0.24, 0.56, 0.86]))
    patch("patch-back", charcoal, [
        [(0.46, 0.12), (0.54, 0.90)], [(0.20, 0.30), (0.80, 0.70)], [(0.80, 0.28), (0.22, 0.72)],
        [(0.30, 0.14), (0.50, 0.30)], [(0.70, 0.12), (0.50, 0.30)],
    ], gold, ([0.16, 0.44, 0.70, 0.90], [0.20, 0.50, 0.78], [0.14, 0.38, 0.64, 0.88], [0.22, 0.52, 0.82]))
    patch("patch-head", crimson_felt, [
        [(0.50, 0.16), (0.50, 0.86)], [(0.50, 0.40), (0.28, 0.22)], [(0.50, 0.40), (0.72, 0.22)],
        [(0.30, 0.62), (0.70, 0.62)],
    ], gold, ([0.24, 0.54, 0.82], [0.20, 0.52, 0.84], [0.26, 0.58, 0.86], [0.18, 0.48, 0.80]))
    patch("patch-thigh", crimson_felt, [
        [(0.16, 0.18), (0.84, 0.84)], [(0.84, 0.16), (0.18, 0.86)], [(0.14, 0.50), (0.86, 0.52)],
    ], gold, ([0.22, 0.52, 0.80], [0.18, 0.48, 0.82], [0.24, 0.56, 0.84], [0.20, 0.50, 0.78]))
    patch("patch-calf", charcoal, [
        [(0.18, 0.20), (0.82, 0.82)], [(0.82, 0.18), (0.20, 0.84)],
        [(0.34, 0.16), (0.66, 0.16)], [(0.32, 0.86), (0.68, 0.86)],
    ], lilac, ([0.20, 0.48, 0.78], [0.26, 0.58, 0.86], [0.18, 0.46, 0.74], [0.22, 0.52, 0.82]))
    return img


def fill_palette_half(img, palette_hex):
    """The bottom half: every palette cell in its flat colour (the shader replaces these; this is only so the file is honest)."""
    cell = SIZE // 16
    for slot in range(16):
        col = 2 * (slot % 8) + 1
        row = 9 if slot < 8 else 13
        c = [int(palette_hex[slot][i:i + 2], 16) for i in (0, 2, 4)]
        for dc in (-1, 0):
            img[row * cell:(row + 1) * cell, (col + dc) * cell:(col + dc + 1) * cell] = c


def build(palette_hex, style="pixel"):
    img = paint(style)
    fill_palette_half(img, palette_hex)
    return Image.fromarray(img, "RGB")


if __name__ == "__main__":
    image = build(["808080"] * 16)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    image.save(OUT, optimize=True)
    print(f"wrote {OUT}")
    for option in STYLES:
        path = OUT.replace(".png", f"-{option}.png")
        build(["808080"] * 16, option).save(path, optimize=True)
        print(f"wrote {path}")
