"""Stack and label the frames of one Tikboy redesign version into review sheets. Stills only.

    py -3 tools/sheet_character_redesign_tikboy.py v03

Reads Logs/character-redesign-tikboy/<version>/*.png (written by
tools/render_character_redesign_tikboy.py -- <version> orig row turn face pair close hands look extremes)
and writes Logs/character-redesign-tikboy/<version>_<sheet>.png:

    row         THE TEST: him in a row with three redesigned heroes and the three approved Classics, front, three-quarter and behind
    row_heads   the seven heads close
    face        his face beside Dante's, Amihan's and his ORIGINAL's (wearing his palette), front and both three-quarters
    head        his whole head and cap from five sides over the original's
    turn        the turnaround over the original's
    pair        him between the original and Amihan's redesign, front and back
    look        the head turned 55 degrees and nodded 22, front and back, and the neck close
    hands       both hands close, in the rest pose and as they hang
    close       the pieces close
    stances     one frame inside each idle stance
    motion      the far frames of walk, sprint, jump and fall

A sheet whose frames are missing is skipped.
"""
import sys
import time
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "Logs" / "character-redesign-tikboy"


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
    who = "Dante  |  Zack  |  Amihan  |  TIKBOY %s  |  Bayan  |  Bebang  |  Lola Pacing" % version
    sheet(version, "row", [[("row_front", "THE TEST, front: " + who)], [("row_34", "three-quarter: " + who)],
                           [("row_back", "from behind (the order is reversed): Lola Pacing  |  Bebang  |  Bayan  |  TIKBOY  |  Amihan  |  Zack  |  Dante")]], 760)
    sheet(version, "row_heads", [[("row_heads", who)]], 520)
    sheet(version, "face", [
        [("faces_dante_front", "Dante redesign"), ("faces_new_front", "TIKBOY " + version), ("faces_amihan_front", "Amihan redesign"),
         ("faces_orig_front", "his ORIGINAL, his palette")],
        [("faces_dante_right34", "Dante"), ("faces_new_right34", "TIKBOY, three-quarter"), ("faces_amihan_right34", "Amihan"), ("faces_orig_right34", "ORIGINAL")],
        [("faces_dante_left34", "Dante"), ("faces_new_left34", "TIKBOY, the other side"), ("faces_amihan_left34", "Amihan"), ("faces_orig_left34", "ORIGINAL")],
    ], 560)
    sheet(version, "head", [
        [("head_new_front", "REDESIGN " + version), ("head_new_right34", "three-quarter"), ("head_new_left", "his left"), ("head_new_back", "back"), ("head_new_above", "above")],
        [("head_orig_front", "ORIGINAL"), ("head_orig_right34", "three-quarter"), ("head_orig_left", "his left"), ("head_orig_back", "back"), ("head_orig_above", "above")],
    ], 560)
    sheet(version, "turn", [
        [("orig_turn", "ORIGINAL, his palette: 0, 35, 90, 180")], [("turn", "REDESIGN " + version)],
        [("orig_turn2", "ORIGINAL: -35, -90, 145, -145")], [("turn2", "REDESIGN " + version)],
    ], 600)
    sheet(version, "pair", [[("pair", "ORIGINAL (his palette)  |  REDESIGN %s  |  Amihan redesign" % version)],
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
        [("close_head_front", "head"), ("close_head_left", "his left"), ("close_hair_back", "back"), ("close_head_above", "above")],
        [("close_chest", "chest"), ("close_neck", "neck"), ("close_belt34", "belt"), ("close_sling", "slingshot")],
        [("close_feet", "feet"), ("close_feet_back", "feet, behind"), ("close_side_left", "his left side"), ("close_below", "from below")],
    ], 500)
    stances = (("00", "stand (0 s)"), ("14", "lookout, left (1.1 s)"), ("25", "lookout, right (2.0 s)"), ("45", "the cap (3.6 s)"),
               ("66", "wound up (5.3 s)"), ("70", "the feint (5.6 s)"), ("86", "the snicker (6.9 s)"))
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


if __name__ == "__main__":
    main()
