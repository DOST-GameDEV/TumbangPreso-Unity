"""Stack and label the frames of one Aling Nena redesign version into review sheets. Stills only.

    py -3 tools/sheet_character_redesign_aling_nena.py v03

Reads Logs/character-redesign-aling_nena/<version>/*.png (written by
tools/render_character_redesign_aling_nena.py -- <version> orig row turn face pair close hands look extremes)
and writes Logs/character-redesign-aling_nena/<version>_<sheet>.png:

    row         THE TEST: her in a row with Amihan, Bebang, Dante, Lola Pacing, Bayan and Cheska, front, three-quarter and behind
    row_heads   the four heads close
    faces       her face beside Amihan's, Cheska's and her ORIGINAL's (wearing her palette), front and three-quarter
    face        her face beside the ORIGINAL's and Amihan's, larger
    turn        the turnaround over the original's
    pair        her between the original and Amihan's redesign, front and back
    look        the head turned 55 degrees and nodded 22, front and back, and the neck close
    hands       both hands close, in the rest pose and as they hang
    close       the pieces close
    stances     one frame inside each idle stance
    motion      the far frames of walk, sprint, jump and fall
    head_scale  (only if <version>_head100/pair.png exists) the head at 0.84 beside the head at 1.00

A sheet whose frames are missing is skipped.
"""
import sys
import time
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "Logs" / "character-redesign-aling_nena"


def tile(path, label, height):
    im = Image.open(path).convert("RGB")
    im = im.resize((max(1, int(im.width * height / im.height)), height), Image.LANCZOS)
    draw = ImageDraw.Draw(im)
    draw.rectangle([0, 0, 7 * len(label) + 10, 17], fill=(0, 0, 0))
    draw.text((5, 3), label, fill=(255, 255, 255))
    return im


def sheet(version, name, rows, height):
    """`rows` is a list of rows; a row is a list of (file stem or path, label)."""
    built = []
    for row in rows:
        tiles = []
        for stem, label in row:
            path = stem if isinstance(stem, Path) else BASE / version / (stem + ".png")
            if not path.exists():
                print("skipped %s: no %s" % (name, path.name))
                return
            tiles.append(tile(path, label, height))
        built.append(tiles)
    width = max(sum(t.width for t in tiles) for tiles in built)
    out = Image.new("RGB", (width, height * len(built)), (30, 30, 36))
    for j, tiles in enumerate(built):
        x = 0
        for t in tiles:
            out.paste(t, (x, j * height))
            x += t.width
    target = BASE / ("%s_%s.png" % (version, name))
    for attempt in range(6):
        try:
            out.save(target)
            break
        except OSError:      # Windows sometimes refuses the write
            time.sleep(1.0)
    print("wrote %s  %dx%d" % (target, out.width, out.height))


def main():
    version = sys.argv[1] if len(sys.argv) > 1 else "v01"
    sheet(version, "row", [[("row_front", "Amihan  |  Bebang  |  Dante  |  ALING NENA %s  |  Lola Pacing  |  Bayan  |  Cheska   (all redesigns)" % version)], [("row_34", "three-quarter")],
                           [("row_back", "from behind")]], 700)
    sheet(version, "row_heads", [[("row_heads", "Amihan  |  Lola Pacing  |  ALING NENA %s  |  Cheska" % version)]], 620)
    sheet(version, "faces", [
        [("faces_amihan_front", "Amihan redesign"), ("faces_new_front", "ALING NENA " + version), ("faces_cheska_front", "Cheska redesign"),
         ("faces_orig_front", "her ORIGINAL, her palette")],
        [("faces_amihan_right34", "Amihan"), ("faces_new_right34", "ALING NENA"), ("faces_cheska_right34", "Cheska"), ("faces_orig_right34", "ORIGINAL")],
    ], 560)
    sheet(version, "face", [
        [("faces_orig_front", "ORIGINAL, her palette"), ("faces_new_front", "REDESIGN " + version), ("faces_amihan_front", "Amihan redesign")],
        [("faces_orig_right34", "ORIGINAL, three-quarter"), ("faces_new_right34", "REDESIGN"), ("faces_amihan_right34", "Amihan redesign")],
    ], 620)
    sheet(version, "turn", [
        [("orig_turn", "ORIGINAL, her palette: 0, 35, 90, 180")], [("turn", "REDESIGN " + version)],
        [("orig_turn2", "ORIGINAL: -35, -90, 145, -145")], [("turn2", "REDESIGN " + version)],
    ], 600)
    sheet(version, "pair", [[("pair", "ORIGINAL (her palette)  |  REDESIGN %s  |  Amihan redesign" % version)],
                            [("pair_back", "the same three from behind")]], 760)
    looks = (("yaw55", "head turned 55"), ("yawm55", "turned -55"), ("yaw55_nod22", "turned 55, nodded 22"), ("yawm55_nod22", "turned -55, nodded 22"))
    sheet(version, "look", [
        [("look_%s_front" % tag, label + ", front") for tag, label in looks],
        [("look_%s_back" % tag, label + ", back") for tag, label in looks],
        [("neck_%s_front" % tag, "neck, " + label) for tag, label in looks],
        [("neck_%s_back" % tag, "neck from behind, " + label) for tag, label in looks],
        [("look_nod22_front", "nodded 22"), ("look_nod22_side", "nodded 22, side"), ("look_up20_side", "tipped back 20, side"), ("look_up20_back", "tipped back 20, back")],
    ], 460)
    sheet(version, "hands", [
        [("hand_%s_%s" % (side, view), "%s hand, %s" % (side, view)) for view in ("front", "back", "above", "below", "end")]
        for side in ("left", "right")
    ] + [[("handidle_left_front", "left, hanging"), ("handidle_left_out", "left, from outside"), ("handidle_right_front", "right, hanging"),
          ("handidle_right_out", "right, from outside"), ("rest_front", "rest pose")]], 440)
    sheet(version, "close", [
        [("close_head_front", "head"), ("close_head_left", "her left"), ("close_hair_back", "back"), ("close_head_above", "above")],
        [("close_chest", "chest"), ("close_neck", "neck"), ("close_skirt34", "apron"), ("close_back", "back")],
        [("close_pencil", "pencil"), ("close_pony_side", "ponytail"), ("close_hair_rear34", "ponytail"), ("close_bow", "bow")],
        [("close_feet", "feet"), ("close_feet_back", "feet, behind"), ("close_side_left", "her left side"), ("close_below", "from below")],
    ], 500)
    stances = (("00", "stand (0 s)"), ("16", "the wag (1.3 s)"), ("22", "the wag (1.8 s)"), ("50", "the apron (4.0 s)"),
               ("56", "the apron (4.5 s)"), ("80", "the call (6.4 s)"), ("86", "the call (6.9 s)"))
    sheet(version, "stances", [
        [("ext_idle_%s_front" % f, label) for f, label in stances],
        [("ext_idle_%s_side" % f, "side") for f, _ in stances],
        [("ext_idle_%s_back" % f, "behind") for f, _ in stances],
    ], 520)
    sheet(version, "motion", [
        [("ext_walk_25_front", "walk"), ("ext_walk_25_side", "walk"), ("ext_walk_75_side", "walk"), ("ext_walk_25_rear", "walk"),
         ("ext_sprint_25_front", "sprint"), ("ext_sprint_25_side", "sprint"), ("ext_sprint_75_side", "sprint"), ("ext_sprint_25_rear", "sprint")],
        [("ext_jump_06_front", "jump, take-off"), ("ext_jump_06_side", "jump"), ("ext_jump_30_front", "jump, hanging"), ("ext_jump_30_rear", "jump"),
         ("ext_fall_25_front", "fall"), ("ext_fall_25_side", "fall"), ("ext_fall_75_front", "fall"), ("ext_fall_75_rear", "fall")],
    ], 520)
    alt = BASE / (version + "_head100")
    if (alt / "pair.png").exists():
        sheet(version, "head_scale", [
            [("pair", "HEAD AT 0.84 (the rule): original  |  redesign  |  Amihan")],
            [(alt / "pair.png", "HEAD AT 1.00 (the original's size): original  |  redesign  |  Amihan")],
        ], 760)


if __name__ == "__main__":
    main()
