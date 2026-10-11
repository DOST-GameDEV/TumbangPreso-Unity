"""Join the frames of tools/render_character_redesign_lola_pacing.py into labelled review sheets.

    py -3 tools/sheet_character_redesign_lola_pacing.py v01

Reads Logs/character-redesign-lola_pacing/<version>/*.png and writes, beside that folder,
    <version>_face.png      her face beside Amihan's, Phaister's and her original's, front and three-quarter
    <version>_row.png       her in a row with the redesigned Amihan, Phaister and Dante: front, three-quarter, back
    <version>_turn.png      her turnaround over the original's
    <version>_pair.png      her between the original and Amihan's redesign, front and back
    <version>_look.png      the head turned 55 degrees and nodded 22, front, back and side, and the neck close
    <version>_hands.png     both fists close, in the rest pose and as they hang
    <version>_close.png     the close-ups
    <version>_head.png      her whole head beside the original's, five views
    <version>_idle.png      one frame inside each idle stance
    <version>_motion.png    the far frames of walk, sprint, jump, fall
A sheet whose frames were not rendered is skipped. Stills only.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "Logs" / "character-redesign-lola_pacing"
V = sys.argv[1] if len(sys.argv) > 1 else "v01"
SRC = BASE / V
BG = (30, 31, 38)


def _font(size):
    for name in ("arialbd.ttf", "arial.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def grid(out, cells, cols, height, title=None):
    """`cells` = [(file name without .png, label)]; every frame is scaled to `height` px tall."""
    tiles = []
    for name, label in cells:
        path = SRC / (name + ".png")
        if not path.exists():
            continue
        img = Image.open(path).convert("RGB")
        w = int(round(img.width * height / img.height))
        img = img.resize((w, height), Image.LANCZOS)
        ImageDraw.Draw(img).text((10, 8), label, fill=(255, 255, 255), font=_font(max(14, height // 24)),
                                 stroke_width=2, stroke_fill=(0, 0, 0))
        tiles.append(img)
    if not tiles:
        print("skipped %s (no frames)" % out)
        return
    rows = [tiles[i:i + cols] for i in range(0, len(tiles), cols)]
    pad = 6
    head = 44 if title else 0
    width = max(sum(t.width for t in r) + pad * (len(r) + 1) for r in rows)
    sheet = Image.new("RGB", (width, head + len(rows) * (height + pad) + pad), BG)
    if title:
        ImageDraw.Draw(sheet).text((12, 8), title, fill=(255, 255, 255), font=_font(24))
    y = head + pad
    for r in rows:
        x = pad
        for t in r:
            sheet.paste(t, (x, y))
            x += t.width + pad
        y += height + pad
    path = BASE / ("%s_%s.png" % (V, out))
    sheet.save(path)
    print("wrote %s" % path)


grid("face", [("faces_amihan_front", "Amihan redesign"), ("faces_new_front", "LOLA PACING " + V), ("faces_phaister_front", "Phaister redesign"),
              ("faces_orig_front", "her ORIGINAL, her palette"),
              ("faces_amihan_right34", "Amihan redesign"), ("faces_new_right34", "LOLA PACING " + V), ("faces_phaister_right34", "Phaister redesign"),
              ("faces_orig_right34", "her ORIGINAL, her palette")],
     4, 500, "lola_pacing %s: her face beside Amihan's, Phaister's and her original's" % V)
grid("row", [("row_front", "Amihan   |   LOLA PACING %s   |   Phaister   |   Dante" % V), ("row_34", "three-quarter"), ("row_back", "back")],
     1, 640, "lola_pacing %s: in a row with the redesigned heroes" % V)
grid("turn", [("turn", "REDESIGN " + V), ("orig_turn", "ORIGINAL, her palette"), ("turn2", "REDESIGN " + V), ("orig_turn2", "ORIGINAL, her palette")],
     1, 560, "lola_pacing %s: turnaround over the original's" % V)
grid("pair", [("pair", "original   |   REDESIGN %s   |   Amihan redesign" % V), ("pair_back", "original   |   REDESIGN %s   |   Amihan redesign" % V)],
     1, 700, "lola_pacing %s: between the original and Amihan's redesign" % V)
looks = [("yaw55", "turned 55"), ("yawm55", "turned -55"), ("nod22", "nodded 22"), ("up20", "up 20"), ("yaw55_nod22", "55 and nodded"),
         ("yawm55_nod22", "-55 and nodded")]
grid("look", [("look_%s_%s" % (tag, view), "%s, %s" % (label, view)) for view in ("front", "back", "side") for tag, label in looks]
     + [("neck_%s_%s" % (tag, view), "neck: %s, %s" % (label, view)) for view in ("front", "back") for tag, label in looks],
     6, 400, "lola_pacing %s: the head turned and nodded" % V)
grid("hands", [("hand_%s_%s" % (side, view), "%s fist, %s (rest pose)" % (side, view)) for side in ("left", "right")
               for view in ("front", "back", "above", "below", "end")]
     + [("handidle_%s_%s" % (side, view), "%s fist as it hangs, %s" % (side, view)) for side in ("left", "right") for view in ("front", "back", "out")]
     + [("rest_front", "rest pose, front"), ("rest_top", "rest pose, above")],
     5, 420, "lola_pacing %s: both hands" % V)
grid("close", [("close_" + name, name) for name in ("chest", "neck", "skirt", "back", "feet", "feet_back", "hair_back", "hair_left",
                                                     "hair_rear34", "hair_above", "side_left", "side_right", "above", "below")],
     5, 420, "lola_pacing %s: close" % V)
grid("head", [("head_%s_%s" % (tag, view), "%s, %s" % (label, view)) for tag, label in (("new", "REDESIGN " + V), ("orig", "ORIGINAL"))
              for view in ("front", "right34", "left", "back", "rear34", "above")],
     6, 420, "lola_pacing %s: the head and its bun beside the original's" % V)
IDLE = ((0, "stand"), (19, "her back"), (44, "the heat"), (60, "the heat"), (84, "the finger"))
grid("idle", [("ext_idle_%02d_%s" % (fr, view), "idle %d%%: %s, %s" % (fr, label, view)) for view in ("front", "side", "back") for fr, label in IDLE],
     5, 520, "lola_pacing %s: one frame inside each idle stance" % V)
frames = {"walk": (25, 50, 75), "sprint": (25, 50, 75), "jump": (6, 30, 95), "fall": (25, 75)}
grid("motion", [("ext_%s_%02d_%s" % (clip, fr, view), "%s %d%%, %s" % (clip, fr, view)) for clip, frs in frames.items()
                for fr in frs for view in ("front", "side", "back")],
     6, 480, "lola_pacing %s: the far frames of walk, sprint, jump and fall" % V)
