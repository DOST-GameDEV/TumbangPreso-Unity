import os
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
SRC = "scratchpad/fontsrc"
path = os.path.join(SRC, "FamiljenGrotesk-VF.ttf")
for label, wght in (("Regular", 400), ("Bold", 700)):
    f = TTFont(path)
    axes = {}
    for a in f["fvar"].axes:
        axes[a.axisTag] = wght if a.axisTag == "wght" else a.defaultValue
    # updateFontNames writes the instance's own family/subfamily, so the Bold file
    # reports itself as Bold rather than as a second Regular. Unity matches on the
    # fontNames in the .meta, but a file that lies about its own weight is the kind of
    # thing that costs an hour the first time a fallback picks the wrong one.
    instantiateVariableFont(f, axes, inplace=True, updateFontNames=True)
    out = os.path.join(SRC, f"FamiljenGrotesk-{label}.ttf")
    f.save(out)
    n = TTFont(out)["name"]
    fam = n.getDebugName(1); sub = n.getDebugName(2)
    print(f"{label}: {os.path.getsize(out)//1024} KB  family={fam!r} subfamily={sub!r}")
