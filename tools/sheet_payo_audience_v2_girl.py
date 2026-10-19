"""Stack and label the frames of one Payo audience v2 GIRL version into review sheets. Stills only.

    py -3 tools/sheet_payo_audience_v2_girl.py v01

Reads ArtSource/payo/audience/v2/girl/renders/<version>/*.png (written by
tools/render_payo_audience_v2_girl.py) and writes renders/<version>_<sheet>.png:

    row     THE TEST: the girl with Rafi, Dante, Cheska, Bebang (her base, beside her), Bayan and
            Lola Pacing; front, three-quarter, back, and the row seen small
    base    her beside Bebang, close, front and back: the finish compared directly
    face    her face beside Cheska's, Rafi's and Bebang's
    turn    the turnaround
    clips   idle stances, cheer, clap, sit on a ledge
    look    the head turned and nodded
    motion  walk, sprint, jump, fall

A sheet whose frames are missing is skipped.
"""
import sys
import time
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "girl" / "renders"


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
    v = sys.argv[1] if len(sys.argv) > 1 else "v01"
    names = "Rafi | Dante | Cheska | GIRL %s | Bebang (her base) | Bayan | Lola Pacing" % v
    sheet(v, "row", [[("row_front", names)], [("row_34", "three-quarter")], [("row_back", "from behind")]], 640)
    sheet(v, "row_small", [[("row_small", "lineup distance: " + names)]], 360)
    sheet(v, "base", [[("base_pair", "Bebang redesign (base)  |  GIRL " + v), ("base_pair_back", "from behind")]], 760)
    sheet(v, "face", [
        [("faces_cheska_front", "Cheska"), ("faces_new_front", "GIRL " + v), ("faces_rafi_front", "Rafi"), ("faces_bebang_front", "Bebang")],
        [("faces_cheska_right34", "Cheska"), ("faces_new_right34", "GIRL"), ("faces_rafi_right34", "Rafi"), ("faces_bebang_right34", "Bebang")],
    ], 520)
    sheet(v, "turn", [[("turn", "GIRL %s: 0, 35, 90, 180" % v)], [("turn2", "-35, -90, 145, -145")]], 600)
    tags = (("idle_hold", "idle: holding her elbows (0 s)"), ("idle_rock", "idle: rocking (1.3 s)"), ("idle_peek", "idle: the peek (4 s)"),
            ("idle_chin", "idle: hands under chin (6 s)"), ("cheer_top", "cheer: top of the hop"), ("clap_shut", "clap: hands meet"),
            ("clap_open", "clap: apart"), ("sit_lap", "sit: hands on lap"), ("sit_clap", "sit: clapping"))
    sheet(v, "clips", [
        [("clip_%s_front" % t, l) for t, l in tags],
        [("clip_%s_side" % t, "side") for t, _ in tags],
        [("clip_%s_back" % t, "behind") for t, _ in tags],
    ], 520)
    sheet(v, "look", [
        [("look_yaw55_front", "turned 55"), ("look_yawm55_nod22_front", "turned -55, nodded 22"), ("look_nod22_front", "nodded 22"), ("look_up20_front", "tipped back 20")],
        [("look_yaw55_back", "back"), ("look_yawm55_nod22_back", "back"), ("look_nod22_back", "back"), ("look_up20_back", "back")],
    ], 460)
    sheet(v, "motion", [[("motion_walk_25_front", "walk"), ("motion_walk_75_side", "walk"), ("motion_sprint_25_side", "sprint"),
                         ("motion_jump_30_front", "jump"), ("motion_fall_25_front", "fall")]], 520)


if __name__ == "__main__":
    main()
