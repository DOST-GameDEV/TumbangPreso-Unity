#!/usr/bin/env python3
"""Puts Maclin Macalindong's CC BY jeepney into the map, AS DELIVERED, and checks the licence.

WHAT THIS ANSWERS
=================

`Attention.md` section 11.2 and `docs/Asset_Sourcing.md` section 7.1. The model is a signed-in
Sketchfab download that has to reach `Art/models/kits/car/` with its credit line already in the
credits screen.

WARNING: THIS SCRIPT USED TO OPTIMISE THE MODEL AND THAT WAS THE MISTAKE. It decimated 74,170
triangles to 3,000, collapsed seventeen materials to one, and rewrote the UVs onto the map's
nine-swatch palette atlas so `tumbang-warm-c` would recolour it like a Kenney van. Every step was
defensible on its own. 🧑, opening the render: *"ew what is that jeep wtf did u do"*, **"u ate all
its colors and design wtf"**, then the rule: *"no need to lower triangles or compress dont worry
it wont lag"*. **`CLAUDE.md` section 6.0 is that rule and it was written from this prop.** The
model was in the map for its silhouette AND its livery, and the optimisation deleted both.

WARNING: SO THE ONLY THING THIS DOES TO THE MESH IS NOTHING. It copies the file. Section 7.1's
sentence about decimating was written before anybody had the model and it is now wrong; the
entry records the reversal. What is still ours is where the prop is placed and how large it is
drawn, and both of those live at the placement site in `IlalimNgTulayBuilder`.

WARNING: THE CREDIT SHIPS IN THE SAME COMMIT AS THE MODEL OR NEITHER SHIPS.
`Attention.md` section 11.2: *"It is CC BY, so the credit line in Asset_Sourcing.md section 9 has
to reach the credits screen in the same commit that ships the model."* This script REFUSES to
copy the .glb if `CreditsContent.CcByCredits` does not already name the author, so the ordering
cannot be got wrong by forgetting rather than by deciding.

USAGE
-----
    python tools/build_jeepney.py [--dry-run]
"""

import argparse
import json
import os
import shutil
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)

SOURCE = os.path.join(REPO, "scratchpad", "asset-src", "sketchfab", "jeepney.glb")
KIT = os.path.join(REPO, "Assets", "TumbangPreso", "Art", "models", "kits", "car")
OUT = os.path.join(KIT, "jeepney.glb")
CREDITS = os.path.join(REPO, "Assets", "TumbangPreso", "Runtime", "UI", "CreditsContent.cs")

# The author, as the file's own metadata gives it. ⚠️ CHECKED AGAINST THE CREDITS SCREEN RATHER
# THAN AGAINST A CONSTANT SOMEBODY TYPED: if Sketchfab ever hands back a different author for
# this id, the check fails loudly instead of approving a credit for the wrong person.
EXPECT_LICENCE = "CC-BY"


def glb_json(path):
    with open(path, "rb") as f:
        data = f.read()
    total = struct.unpack("<I", data[8:12])[0]
    off = 12
    js = None
    while off < total:
        clen, ctype = struct.unpack("<I4s", data[off:off + 8])
        if ctype == b"JSON":
            js = json.loads(data[off + 8:off + 8 + clen].decode("utf-8"))
        off += 8 + clen
    return js


def triangles(js):
    total = 0
    for mesh in js["meshes"]:
        for prim in mesh["primitives"]:
            acc = prim.get("indices")
            if acc is None:
                acc = prim["attributes"]["POSITION"]
            total += js["accessors"][acc]["count"] // 3
    return total


def author_surname(author):
    """The last word of the author field, which is what a credit line has to contain."""
    name = (author or "").split("(")[0].strip()
    return name.split()[-1] if name else ""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    if not os.path.isfile(SOURCE):
        print("build_jeepney: no source model at " + SOURCE)
        print("               Attention.md section 11.2: it is a signed-in Sketchfab download.")
        return 2

    js = glb_json(SOURCE)
    extras = js.get("asset", {}).get("extras", {})
    licence = extras.get("license", "")
    author = extras.get("author", "")

    print("build_jeepney: %s" % os.path.relpath(SOURCE, REPO))
    print("               %d triangles, %d meshes, %d materials, %d textures"
          % (triangles(js), len(js["meshes"]), len(js.get("materials", [])),
             len(js.get("images", []))))
    print("               licence: %s" % licence)
    print("               author : %s" % author)

    if EXPECT_LICENCE not in licence.upper():
        print("build_jeepney: REFUSING. The file's own metadata does not say CC BY, and this")
        print("               pipeline only knows how to satisfy that licence.")
        return 2

    surname = author_surname(author)
    if not surname:
        print("build_jeepney: REFUSING. The file carries no author to credit.")
        return 2

    if not os.path.isfile(CREDITS):
        print("build_jeepney: REFUSING. No credits screen at " + CREDITS)
        return 2

    with open(CREDITS, "r", encoding="utf-8") as f:
        credits_text = f.read()

    if surname not in credits_text:
        print("build_jeepney: REFUSING to copy the model.")
        print("               It is %s and CreditsContent.CcByCredits does not name '%s'."
              % (licence, surname))
        print("               Asset_Sourcing.md section 9 has the line, and Attention.md")
        print("               section 11.2 says it ships in the SAME commit.")
        return 2

    print("               credit : present in CreditsContent.CcByCredits")

    if args.dry_run:
        print("build_jeepney: dry run, nothing written")
        return 0

    os.makedirs(KIT, exist_ok=True)
    shutil.copyfile(SOURCE, OUT)

    print()
    print("build_jeepney: copied to %s, unmodified." % os.path.relpath(OUT, REPO))
    print("               CLAUDE.md section 6.0: no decimation, no compression, no recolour.")
    print("               It is placed with an EMPTY palette in IlalimNgTulayBuilder so the")
    print("               kit's atlas swap leaves its own materials alone.")
    print()
    print("Run MapGeometryCheck after replacement (Asset_Sourcing.md section 7.1), and render")
    print("the map: a background landmark is a silhouette judgement and no probe makes it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
