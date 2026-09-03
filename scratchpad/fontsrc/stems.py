import os
from fontTools.ttLib import TTFont
from fontTools.pens.boundsPen import BoundsPen

SRC = "scratchpad/fontsrc"
DARUMA = "Assets/TumbangPreso/Art/ui/fonts/DarumadropOne-Regular.ttf"

def bounds(f, ch):
    gs, cmap = f.getGlyphSet(), f.getBestCmap()
    gn = cmap.get(ord(ch))
    if not gn: return None
    p = BoundsPen(gs); gs[gn].draw(p); return p.bounds

def stem(path, ch="I"):
    """Width of the cap-I stem in per-mille. A plain vertical bar in every sans here,
    so it is the weight itself rather than a tail or a serif."""
    f = TTFont(path); upm = f["head"].unitsPerEm
    b = bounds(f, ch)
    return round((b[2]-b[0]) * 1000.0 / upm) if b else None

CANDS = [
    ("Darumadrop One",   DARUMA, None),
    ("Nunito (current)", f"{SRC}/Nunito-Regular.ttf",          f"{SRC}/Nunito-Bold.ttf"),
    ("Hanken Grotesk",   f"{SRC}/HankenGrotesk-Regular.ttf",   f"{SRC}/HankenGrotesk-Bold.ttf"),
    ("Familjen Grotesk", f"{SRC}/FamiljenGrotesk-Regular.ttf", f"{SRC}/FamiljenGrotesk-Bold.ttf"),
    ("Archivo",          f"{SRC}/Archivo-Regular.ttf",         f"{SRC}/Archivo-Bold.ttf"),
    ("Work Sans",        f"{SRC}/WorkSans-Regular.ttf",        f"{SRC}/WorkSans-Bold.ttf"),
    ("Figtree",          f"{SRC}/Figtree-Regular.ttf",         f"{SRC}/Figtree-Bold.ttf"),
]
print(f"{'face':20} {'stem I reg':>10} {'stem I bold':>12} {'real bold':>10} {'KB/weight':>10}")
for n, r, b in CANDS:
    if not os.path.exists(r): print("MISSING", r); continue
    sr = stem(r); sb = stem(b) if b else None
    d = f"+{round((sb-sr)*100.0/sr)}%" if sb else "NONE"
    kb = os.path.getsize(r)//1024
    print(f"{n:20} {sr:>10} {str(sb) if sb else '-':>12} {d:>10} {kb:>10}")
