import sys, os
from fontTools.ttLib import TTFont

DARUMA = "Assets/TumbangPreso/Art/ui/fonts/DarumadropOne-Regular.ttf"
SRC = "scratchpad/fontsrc"

CANDIDATES = [
    ("Darumadrop One", DARUMA, None),
    ("Nunito",             f"{SRC}/Nunito-Regular.ttf",           f"{SRC}/Nunito-Bold.ttf"),
    ("M PLUS Rounded 1c",  f"{SRC}/MPLUSRounded1c-Regular.ttf",   f"{SRC}/MPLUSRounded1c-Bold.ttf"),
    ("Figtree",            f"{SRC}/Figtree-Regular.ttf",          f"{SRC}/Figtree-Bold.ttf"),
    ("Baloo 2",            f"{SRC}/Baloo2-Regular.ttf",           f"{SRC}/Baloo2-Bold.ttf"),
]

# Glyphs this repo has recorded as MISSING from Darumadrop, plus Filipino diacritics.
NEEDED = {
    "MULTIPLY x":  0x00D7, "LEFT TRI":    0x25C0, "RIGHT TRI":  0x25B6,
    "single >":    0x203A, "single <":    0x2039, "ellipsis":   0x2026,
    "n-tilde":     0x00F1, "N-tilde":     0x00D1, "a-acute":    0x00E1,
    "e-acute":     0x00E9, "degree":      0x00B0, "bullet":     0x2022,
    "en dash":     0x2013, "check":       0x2713, "arrow left": 0x2190,
}

def stem_width(font, ch):
    """Horizontal run of the 'l' stem at mid x-height, in per-mille of em."""
    from fontTools.pens.boundsPen import BoundsPen
    gs = font.getGlyphSet()
    cmap = font.getBestCmap()
    gn = cmap.get(ord(ch))
    if not gn: return None
    pen = BoundsPen(gs)
    gs[gn].draw(pen)
    if pen.bounds is None: return None
    xmin, ymin, xmax, ymax = pen.bounds
    upm = font["head"].unitsPerEm
    return round((xmax - xmin) * 1000.0 / upm)

def glyph_bounds(font, ch):
    from fontTools.pens.boundsPen import BoundsPen
    gs = font.getGlyphSet()
    cmap = font.getBestCmap()
    gn = cmap.get(ord(ch))
    if not gn: return None
    pen = BoundsPen(gs)
    gs[gn].draw(pen)
    return pen.bounds

def report(name, reg, bold):
    f = TTFont(reg)
    upm = f["head"].unitsPerEm
    cmap = f.getBestCmap()
    os2 = f["OS/2"]

    xb = glyph_bounds(f, "x")
    hb = glyph_bounds(f, "H")
    ob = glyph_bounds(f, "o")
    xh = round((xb[3] - xb[1]) * 1000.0 / upm) if xb else 0
    cap = round((hb[3] - hb[1]) * 1000.0 / upm) if hb else 0
    # overshoot-free ratio: the single best predictor of small-size legibility
    ratio = round(xh / cap, 3) if cap else 0

    stem_r = stem_width(f, "l")
    stem_b = stem_width(TTFont(bold), "l") if bold else None
    delta = round((stem_b - stem_r) * 100.0 / stem_r) if (stem_b and stem_r) else None

    missing = [k for k, cp in NEEDED.items() if cp not in cmap]

    print(f"\n=== {name} ===")
    print(f"  glyphs in cmap      : {len(cmap)}")
    print(f"  units per em        : {upm}")
    print(f"  x-height  (per-mille): {xh}")
    print(f"  cap height(per-mille): {cap}")
    print(f"  x-height / cap      : {ratio}   <- higher reads bigger at the same fontSize")
    print(f"  stem 'l' regular    : {stem_r}")
    if stem_b is not None:
        print(f"  stem 'l' bold       : {stem_b}   (+{delta}% real weight)")
    else:
        print(f"  stem 'l' bold       : NONE SHIPPED  <- every bold is a synthetic smear")
    print(f"  missing of {len(NEEDED)} needed: {len(missing)}")
    if missing:
        print(f"    -> {', '.join(missing)}")

for n, r, b in CANDIDATES:
    if not os.path.exists(r):
        print(f"MISSING FILE: {r}"); continue
    report(n, r, b)
