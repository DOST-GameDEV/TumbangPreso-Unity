from fontTools.ttLib import TTFont
import glob, os
for p in sorted(glob.glob("scratchpad/fontsrc/*.ttf")):
    f = TTFont(p)
    nm = {r.nameID: r.toUnicode() for r in f["name"].names if r.platformID == 3}
    os2 = f["OS/2"]
    cmap = f.getBestCmap()
    gs = f.getGlyphSet()
    from fontTools.pens.boundsPen import BoundsPen
    def bb(ch):
        gn = cmap.get(ord(ch))
        if not gn: return None
        pen = BoundsPen(gs); gs[gn].draw(pen); return pen.bounds
    lb, ib, nb = bb("l"), bb("I"), bb("n")
    def w(b): return round(b[2]-b[0]) if b else None
    print(f"{os.path.basename(p):28s} family={nm.get(16) or nm.get(1):22s} sub={nm.get(17) or nm.get(2):12s} "
          f"usWeight={os2.usWeightClass:4d} width(l)={w(lb)} width(I)={w(ib)} width(n)={w(nb)}")
