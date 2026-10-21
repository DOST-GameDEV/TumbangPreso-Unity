"""Stack and label one version of the Payo audience villager `woman` (v2) into review sheets. Stills only.

    py -3 tools/sheet_payo_audience_v2_woman.py v03

Reads ArtSource/payo/audience/v2/woman/renders/<version>/*.png (written by
tools/render_payo_audience_v2_woman.py) and writes renders/<version>_<sheet>.png:

    row        THE TEST: her with Rafi, Dante, Cheska (heroes), Inday (her base Classic, beside her for the
               finish) and Bayan, Bebang, Lola Pacing (approved Classics); front, three-quarter, back
    row_heads  the eight heads close
    faces      her face beside Cheska's, Dante's and Inday's
    turn       the turnaround
    close      the pieces close
    look       the head turned 55 and nodded 22
    clips      a still of idle (each stance), cheer, clap and sit (on the ledge)
    far        the row at crowd distance, and as a silhouette

A sheet whose frames are missing is skipped. A copy of tools/sheet_character_redesign_inday.py.
"""
import sys
import time
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "ArtSource" / "payo" / "audience" / "v2" / "woman" / "renders"
#   the same list as CLIP_FRAMES in tools/render_payo_audience_v2_woman.py (keep the two together)
CLIP_FRAMES = [("idle", 0.0, "stand"), ("idle", 0.219, "shout"), ("idle", 0.306, "shout, the third yell"),
               ("idle", 0.506, "nudge, the jab"), ("idle", 0.79, "clasp, bounce"), ("idle", 0.894, "the pump"),
               ("cheer", 0.0, "cheer, landing"), ("cheer", 0.42, "cheer, the top of the hop"),
               ("clap", 0.0, "clap, open"), ("clap", 0.13, "clap, hands meet"),
               ("sit", 0.0, "sit"), ("sit", 0.25, "sit")]
LENGTHS = {"idle": 8.0, "cheer": 1.0, "clap": 1.0, "sit": 2.0}


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
    names = ("Rafi  |  Dante  |  Cheska  (heroes)  |  WOMAN %s  |  Inday (her base Classic)  |  Bayan  |  Bebang  |  Lola Pacing"
             % version)
    sheet(version, "row", [[("row_front", names)], [("row_34", "three-quarter (each turned 32 degrees), the same order")],
                           [("row_back", "from behind, the same order")]], 760)
    sheet(version, "row_heads", [[("row_heads", names)]], 560)
    sheet(version, "faces", [
        [("faces_cheska_front", "Cheska"), ("faces_new_front", "WOMAN " + version), ("faces_dante_front", "Dante"), ("faces_inday_front", "Inday (base)")],
        [("faces_cheska_right34", "Cheska"), ("faces_new_right34", "WOMAN"), ("faces_dante_right34", "Dante"), ("faces_inday_right34", "Inday")],
    ], 560)
    sheet(version, "turn", [[("turn", "WOMAN %s: 0, 35, 90, 180" % version)], [("turn2", "-35, -90, 145, -145")]], 600)
    sheet(version, "close", [
        [("close_head_front", "head"), ("close_head_34", "three-quarter"), ("close_head_back", "back"), ("close_head_above", "above")],
        [("close_chest", "necklace"), ("close_chest34", "necklace, 3/4"), ("close_tapis", "tapis"), ("close_tapis34", "tapis, her left")],
        [("close_back", "back"), ("close_feet", "bare feet"), ("close_side_left", "her left"), ("close_side_right", "her right")],
    ], 500)
    looks = (("yaw55", "head turned 55"), ("yawm55", "turned -55"), ("nod22", "nodded 22"), ("yaw55_nod22", "turned 55, nodded 22"))
    sheet(version, "look", [[("look_%s_front" % t, l) for t, l in looks], [("look_%s_back" % t, l + ", back") for t, l in looks]], 460)
    stills = [("clip_%s_%03d" % (c, int(round(f * 1000))), "%s (%.2f s)" % (lab, f * LENGTHS[c])) for c, f, lab in CLIP_FRAMES]
    sheet(version, "clips", [
        [(s + "_front", lab) for s, lab in stills[:6]],
        [(s + "_34", "3/4") for s, _ in stills[:6]],
        [(s + "_front", lab) for s, lab in stills[6:]],
        [(s + "_34" if "sit" not in s else s + "_side", "3/4" if "sit" not in s else "side, on the ledge") for s, _ in stills[6:]],
    ], 600)
    sheet(version, "far", [[("far", "crowd distance: " + names)], [("far_sil", "silhouette")]], 280)


if __name__ == "__main__":
    main()
