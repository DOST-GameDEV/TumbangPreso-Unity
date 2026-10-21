"""Stack and label the frames of one version of the Payo audience v2 `boy` into review sheets.

    py -3 tools/sheet_payo_audience_v2_boy.py v03

Reads ArtSource/payo/audience/v2/boy/renders/<version>/*.png (written by
tools/render_payo_audience_v2_boy.py) and writes ArtSource/payo/audience/v2/boy/renders/<version>_<sheet>.png:

    row      THE TEST: him in a row with Rafi, Dante, Cheska (heroes), Bayan, Bebang, Lola Pacing
             (approved Classics) and his base Classic TOTOY right beside him: front, three-quarter,
             back, the heads close, and far and small
    face     his face beside Dante's, Cheska's and Totoy's, front and three-quarter
    turn     the turnaround
    clips    idle (one still inside each stance), cheer, clap, sit on a ledge block, and locomotion
    look     the head turned 55 and nodded 22
    close    the pieces close

A copy of tools/sheet_character_redesign_totoy.py, rewritten for him. A sheet whose frames are
missing is skipped. Stills only.
"""
import sys
import time
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "boy" / "renders"


def tile(path, label, height):
    im = Image.open(path).convert("RGB")
    im = im.resize((max(1, int(im.width * height / im.height)), height), Image.LANCZOS)
    draw = ImageDraw.Draw(im)
    draw.rectangle([0, 0, 7 * len(label) + 10, 17], fill=(0, 0, 0))
    draw.text((5, 3), label, fill=(255, 255, 255))
    return im


def sheet(version, name, rows, height):
    built = []
    for row in rows:
        tiles = []
        for stem, label in row:
            path = BASE / version / (stem + ".png")
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
        except OSError:
            time.sleep(1.0)
    print("wrote %s  %dx%d" % (target, out.width, out.height))


def main():
    version = sys.argv[1] if len(sys.argv) > 1 else "v01"
    names = ("Rafi (hero) | Dante (hero) | Bayan (Classic) | Totoy (his BASE Classic) | BOY %s | Bebang (Classic) | "
             "Cheska (hero) | Lola Pacing (Classic)" % version)
    sheet(version, "row", [[("row_front", "front: " + names)], [("row_34", "three-quarter, the same order")],
                           [("row_back", "from behind, the same order")], [("row_heads", "the heads close, the same order")]], 620)
    sheet(version, "row_far", [[("row_far", "far and small, from above, as from the court: " + names)]], 260)
    sheet(version, "face", [
        [("face_dante_front", "Dante (hero)"), ("face_new_front", "BOY " + version), ("face_cheska_front", "Cheska (hero)"),
         ("face_totoy_front", "Totoy (base)")],
        [("face_dante_34", "Dante"), ("face_new_34", "BOY"), ("face_cheska_34", "Cheska"), ("face_totoy_34", "Totoy")],
    ], 560)
    sheet(version, "turn", [[("turn", "BOY %s: 0, 35, 90, 180" % version)], [("turn2", "-35, -90, 145, -145")]], 600)
    idle = (("stand", "idle: stand (0 s)"), ("watch", "watching, on his toes (1.1 s)"), ("point", "POINTING at the court (2.8 s)"),
            ("wave_a", "WAVING, out (5.0 s)"), ("wave_b", "waving, over (5.2 s)"), ("scratch", "scratching his head (7.1 s)"))
    sheet(version, "idle", [
        [("idle_%s_front" % t, l) for t, l in idle],
        [("idle_%s_side" % t, "side") for t, _ in idle],
        [("idle_%s_back" % t, "behind") for t, _ in idle],
    ], 520)
    sheet(version, "loops", [
        [("cheer_crouch_front", "cheer: crouch (0.0)"), ("cheer_top_front", "cheer: TOP (0.34)"), ("cheer_top_side", "cheer: top, side"),
         ("cheer_top_34", "cheer: top, 3/4"), ("cheer_land_front", "cheer: land (0.60)")],
        [("clap_apart_front", "clap: apart"), ("clap_clap_front", "clap: CLAP (0.15)"), ("clap_clap_side", "clap: side"),
         ("clap_clap_34", "clap: 3/4"), ("clap_apart_side", "clap: apart, side")],
        [("sit_a_front", "sit on a ledge: legs level"), ("sit_b_front", "sit: legs swung"), ("sit_a_side", "sit: side, seat at z 0"),
         ("sit_b_side", "sit: side, swung"), ("sit_c_34", "sit: 3/4")],
    ], 560)
    sheet(version, "motion", [
        [("loco_%s_%s" % (c, v), "%s, %s" % (c, v)) for c in ("walk", "sprint", "jump", "fall") for v in ("front", "side")],
    ], 520)
    looks = (("yaw55", "head turned 55"), ("yawm55", "turned -55"), ("nod22", "nodded 22"), ("yaw55_nod22", "turned 55, nodded 22"))
    sheet(version, "look", [
        [("look_%s_front" % t, l + ", front") for t, l in looks],
        [("look_%s_back" % t, l + ", back") for t, l in looks],
    ], 460)
    sheet(version, "close", [
        [("close_head_front", "head"), ("close_head_left", "his left"), ("close_head_back", "back"), ("close_head_above", "above")],
        [("close_chest", "chest, beads"), ("close_bahag34", "bahag, knot, top"), ("close_back", "back flap"), ("close_feet", "bare feet")],
    ], 500)


if __name__ == "__main__":
    main()
