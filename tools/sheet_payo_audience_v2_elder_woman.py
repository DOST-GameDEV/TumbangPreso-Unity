"""Join the frames of tools/render_payo_audience_v2_elder_woman.py into labelled review sheets.

    py -3 tools/sheet_payo_audience_v2_elder_woman.py v01

Reads ArtSource/payo/audience/v2/elder_woman/renders/<version>/*.png and writes, beside that folder,
    <version>_row.png      in a row with Rafi, Cheska, Dante, Bayan, Bebang and Lola Pacing (her base, beside her): front, 3/4, back, far
    <version>_pair.png     beside Lola Pacing, close, to compare the finish
    <version>_face.png     her face beside Rafi's and Cheska's (and Lola Pacing's)
    <version>_turn.png     her turnaround and her head from six sides
    <version>_look.png     the head turned 55 and nodded 22
    <version>_clips.png    idle, cheer, clap, sit (on a ledge), walk, sprint, jump, fall
    <version>_seat.png     the sit from the side, front and below, on a ledge whose top is z 0
    <version>_close.png    the close-ups
A sheet whose frames were not rendered is skipped. Stills only. A copy of
tools/sheet_character_redesign_lola_pacing.py rewritten for her.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "elder_woman" / "renders"
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
    sheet = Image.new("RGB", (width, head + sum(max(t.height for t in r) + pad for r in rows) + pad), BG)
    if title:
        ImageDraw.Draw(sheet).text((12, 8), title, fill=(255, 255, 255), font=_font(24))
    y = head + pad
    for r in rows:
        x = pad
        for t in r:
            sheet.paste(t, (x, y))
            x += t.width + pad
        y += max(t.height for t in r) + pad
    path = BASE / ("%s_%s.png" % (V, out))
    sheet.save(path)
    print("wrote %s" % path)


ORDER = "Rafi | Cheska | Dante | Bayan | Bebang | Lola Pacing (base) | ELDER WOMAN " + V
grid("row", [("row_front", ORDER), ("row_34", "three-quarter"), ("row_back", "back"), ("row_far", "crowd distance")],
     1, 600, "elder_woman %s: in a row with three heroes and three approved Classics" % V)
grid("pair", [("pair_front", "Lola Pacing (base)  |  ELDER WOMAN " + V), ("pair_34", "three-quarter"), ("pair_back", "back")],
     3, 620, "elder_woman %s: beside her base Classic, for the finish" % V)
grid("face", [("faces_rafi_front", "Rafi"), ("faces_new_front", "ELDER WOMAN " + V), ("faces_cheska_front", "Cheska"),
              ("faces_lola_front", "Lola Pacing"),
              ("faces_rafi_right34", "Rafi"), ("faces_new_right34", "ELDER WOMAN " + V), ("faces_cheska_right34", "Cheska"),
              ("faces_lola_right34", "Lola Pacing")],
     4, 480, "elder_woman %s: her face beside two heroes' (and her base's)" % V)
grid("turn", [("turn", "turnaround " + V)], 1, 640, "elder_woman %s: turnaround" % V)
grid("head", [("head_" + v, v) for v in ("front", "34", "side", "back", "rear34", "above")], 6, 420,
     "elder_woman %s: the head, hair and bun" % V)
looks = (("yaw55", "turned 55"), ("yawm55", "turned -55"), ("nod22", "nodded 22"), ("yaw55_nod22", "55 and nodded"))
grid("look", [("look_%s_%s" % (t, v), "%s, %s" % (l, v)) for v in ("front", "back") for t, l in looks], 4, 420,
     "elder_woman %s: the head turned and nodded" % V)
cells = []
for clip, fracs, views in (("idle", (0, 18, 36, 50, 66, 85), ("front", "side", "back")), ("cheer", (0, 25, 50), ("front", "side")),
                           ("clap", (0, 15, 40), ("front", "side")), ("sit", (0, 55), ("front", "side", "back")),
                           ("walk", (25, 75), ("front", "side")), ("sprint", (25,), ("front", "side")),
                           ("jump", (30,), ("front",)), ("fall", (25,), ("front",))):
    for fr in fracs:
        for v in views:
            cells.append(("clip_%s_%02d_%s" % (clip, fr, v), "%s %d%%, %s" % (clip, fr, v)))
grid("clips", cells, 9, 440, "elder_woman %s: her clips (the sit on a ledge whose top is z 0)" % V)
grid("seat", [("seat_side", "sit: side"), ("seat_front", "sit: front"), ("seat_below", "sit: from below")], 3, 560,
     "elder_woman %s: the sit on a ledge, top at z 0 = the root origin" % V)
grid("close", [("close_" + n, n) for n in ("chest", "chest34", "back", "skirt", "feet", "basket")], 3, 460,
     "elder_woman %s: close" % V)
