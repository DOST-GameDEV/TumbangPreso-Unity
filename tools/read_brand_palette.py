"""Reads the brand palette OUT OF the committed logo, instead of anybody typing it in by eye.

docs/TODO.md section 133.1, in his words: "the colors are final, ask it to use the same colors as
logo", and the rule that follows it:

    READ THE HEXES OFF THE COMMITTED FILE. DO NOT TYPE THEM IN BY EYE AND DO NOT SAMPLE A CHAT
    THUMBNAIL.

This is tools/build_input_glyphs.py's discipline applied to the brand: that script reads a pack's
palette and stops on a colour it has never seen rather than guessing, and it is why the glyph
recolour has never silently drifted.

WHY CLUSTERING THE ARTWORK RATHER THAN READING THE SWATCH LABELS. The swatch strips that shipped
with the first version of the logo carry printed hex text, and reading four-pixel-tall type with
OCR would be a guess wearing a lab coat. The artwork itself is FLAT FILLED, so every brand colour
occupies thousands of identical pixels and a histogram returns it exactly. Anti-aliased edge
pixels occupy a handful each and fall below the floor, which is what the floor is for.

    python tools/read_brand_palette.py
    python tools/read_brand_palette.py Assets/TumbangPreso/Art/ui/brand/tump_logo_v2.png
"""

import collections
import glob
import os
import sys

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is needed: python -m pip install pillow")

BRAND_DIR = "Assets/TumbangPreso/Art/ui/brand"

# A fill has to own at least this share of the visible pixels to be a brand colour rather than
# an anti-aliasing artefact. The logo's smallest deliberate fill is the drip's swirl, which is
# comfortably above it; a one-pixel edge blend is three orders of magnitude below.
FLOOR = 0.004

# How far two pixel values may sit apart and still be the same authored fill. JPEG chroma
# subsampling moves a flat colour by a handful of levels; a different brand colour is tens of
# levels away, so there is a wide gap to put this in.
MERGE = 14

# Anything this close to white or this transparent is the page, not the drawing.
WHITE = 246
ALPHA = 32


def describe(r, g, b):
    """A rough hue name, so a reader can match a row to one of the five named swatches.

    It is a LABEL FOR A HUMAN and nothing reads it. The hex is the output; this column only
    exists so that mapping 'the yellow-green one' to Chartreuse does not need a colour picker.
    """
    mx, mn = max(r, g, b), min(r, g, b)
    v = mx / 255.0
    s = 0.0 if mx == 0 else (mx - mn) / mx

    if s < 0.12:
        return "near-neutral (light)" if v > 0.6 else "near-neutral (dark)"

    if mx == r:
        h = 60.0 * (((g - b) / (mx - mn)) % 6)
    elif mx == g:
        h = 60.0 * (((b - r) / (mx - mn)) + 2)
    else:
        h = 60.0 * (((r - g) / (mx - mn)) + 4)

    if h < 12 or h >= 345:
        return "deep red" if v < 0.62 else "red"
    if h < 38:
        return "persimmon / orange" if v > 0.7 else "burnt orange"
    if h < 52:
        return "golden orange"
    if h < 66:
        return "honey / pale gold" if s < 0.45 else "yellow"
    if h < 95:
        return "chartreuse / yellow-green"
    if h < 150:
        return "olive / army" if v < 0.6 else "green"
    if h < 200:
        return "teal  <-- WARM PALETTE ONLY, see CLAUDE.md 6.4"
    if h < 260:
        return "BLUE  <-- BANNED, CLAUDE.md 6.4"
    return "magenta / pink"


def read(path):
    img = Image.open(path).convert("RGBA")
    counts = collections.Counter()
    visible = 0

    for r, g, b, a in img.getdata():
        if a < ALPHA:
            continue
        if r > WHITE and g > WHITE and b > WHITE:
            continue
        visible += 1
        counts[(r, g, b)] += 1

    if not visible:
        print(f"  {path}: nothing but page. Is it a white-on-white export?")
        return []

    # MERGE NEAR-IDENTICAL VALUES BEFORE COUNTING, because these arrive as JPEGs.
    #
    # The first run of this on logo.jpg reported the wordmark's outline EIGHT TIMES: 970617,
    # 980716, 980613, 960514, 970615, 980515, 960516 and 9d0710, together about a fifth of the
    # drawing. They are one flat fill that JPEG's chroma subsampling smeared, and a reader that
    # prints them as eight colours has handed the human exactly the guess this script exists to
    # remove. Anything inside MERGE of a heavier neighbour joins it, and the winner keeps the
    # PIXEL-COUNT-WEIGHTED CENTROID rather than the modal value, because the mode is itself a
    # compression artefact.
    ordered = counts.most_common()
    clusters = []          # [r_sum, g_sum, b_sum, n]

    for (r, g, b), n in ordered:
        for c in clusters:
            cr, cg, cb = c[0] / c[3], c[1] / c[3], c[2] / c[3]
            if abs(cr - r) <= MERGE and abs(cg - g) <= MERGE and abs(cb - b) <= MERGE:
                c[0] += r * n
                c[1] += g * n
                c[2] += b * n
                c[3] += n
                break
        else:
            clusters.append([r * n, g * n, b * n, n])

    clusters.sort(key=lambda c: -c[3])

    rows = []
    for cr, cg, cb, n in clusters:
        share = n / visible
        if share < FLOOR:
            continue
        r, g, b = round(cr / n), round(cg / n), round(cb / n)
        rows.append((share, f"{r:02x}{g:02x}{b:02x}", n, describe(r, g, b)))

    print(f"\n=== {path} ===")
    print(f"    {img.width} x {img.width}, {visible:,} visible pixels, "
          f"{len(counts):,} distinct colours, {len(rows)} flat fills over "
          f"{FLOOR * 100:.1f}%\n")
    print(f"    {'share':>7s}  {'hex':8s}  {'pixels':>10s}  what it looks like")
    print("    " + "-" * 68)
    for share, hexcode, n, name in rows:
        print(f"    {share * 100:6.2f}%  #{hexcode}  {n:10,}  {name}")

    banned = [h for _, h, _, n in rows if "BANNED" in n]
    if banned:
        print("\n    !! a banned hue is present in the artwork. CLAUDE.md 6.4 governs UI colours")
        print("       chosen in code, NOT his authored art, so this is a note rather than a fault.")

    return rows


def main():
    targets = sys.argv[1:]

    if not targets:
        targets = sorted(
            g for ext in ("png", "PNG", "jpg", "jpeg", "webp")
            for g in glob.glob(os.path.join(BRAND_DIR, f"*.{ext}"))
        )

    if not targets:
        sys.exit(
            f"nothing to read in {BRAND_DIR}.\n\n"
            "The logo is still only in the chat. Attention.md section 12 is the ask: drop the\n"
            "file into that folder so section 133 can read its actual pixels. A chat image\n"
            "cannot be sampled, sliced or drawn."
        )

    for path in targets:
        read(path)

    print("\nNEXT: map these rows onto the five named swatches (Honey Quartz, Chartreuse,")
    print("Persimmon, Khaki, Army) plus the deep red outline, and write them into")
    print("Assets/TumbangPreso/Runtime/UI/UiTheme.cs. Then rewrite CLAUDE.md section 6.4's")
    print("palette list IN THE SAME COMMIT: a rules file naming the old colours is worse than")
    print("no rule at all, and it is the one file every session reads first.")


if __name__ == "__main__":
    main()
