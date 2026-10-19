"""Stack and label the frames of tools/render_payo_audience_v2_elder_man.py into review sheets.

    py -3 tools/sheet_payo_audience_v2_elder_man.py v01

Reads ArtSource/payo/audience/v2/elder_man/renders/<version>/ and writes <version>_*.png beside
that folder. A sheet is skipped when none of its frames were rendered. A copy of Mang Kanor's
sheet script with the sheets the Payo audience brief asks for: (a) the row with heroes and
Classics, his base Classic beside him, front, three-quarter and back; (b) the face beside two
heroes'; (c) a turnaround; (d) one still each of idle, cheer, clap and sit.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/ArtSource/payo/audience/v2/elder_man/renders"
V = sys.argv[1]
R = f"{S}/{V}/"
BG = (34, 36, 44)


def font(size):
    try:
        return ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", size)
    except OSError:
        return ImageFont.load_default()


def text(d, xy, s, size=24, anchor="mid"):
    f = font(size)
    w = d.textlength(s, font=f)
    x = xy[0] - w / 2 if anchor == "mid" else xy[0]
    d.text((x, xy[1]), s, font=f, fill=(255, 255, 255), stroke_width=3, stroke_fill=(20, 20, 26))


def grid(name, cells, cols, title=None, cell_w=None, label=20):
    ims = [(Image.open(R + f).convert("RGB"), lab) for f, lab in cells if os.path.exists(R + f)]
    if not ims:
        return
    if cell_w:
        ims = [(im.resize((cell_w, int(im.height * cell_w / im.width)), Image.LANCZOS), lab) for im, lab in ims]
    w, h = ims[0][0].size
    rows = (len(ims) + cols - 1) // cols
    top = 56 if title else 8
    sheet = Image.new("RGB", (cols * w + (cols + 1) * 8, top + rows * (h + 8)), BG)
    d = ImageDraw.Draw(sheet)
    if title:
        text(d, (16, 12), title, 26, anchor="left")
    for i, (im, lab) in enumerate(ims):
        x = 8 + (i % cols) * (w + 8); y = top + (i // cols) * (h + 8)
        sheet.paste(im, (x, y))
        text(d, (x + 12, y + 8), lab, label, anchor="left")
    sheet.save(f"{S}/{V}_{name}.png")
    print("wrote", f"{S}/{V}_{name}.png", sheet.size)


ORDER = "RAFI, DANTE, BAYAN, MANG KANOR (his base), ELDER, LOLA PACING, BEBANG, CHESKA"
grid("row", [("row_front.png", "front: " + ORDER), ("row_34.png", "three-quarter: the same"),
             ("row_back.png", "back: CHESKA, BEBANG, LOLA PACING, ELDER, MANG KANOR, BAYAN, DANTE, RAFI")], 1,
     "THE TEST: the elder " + V + " with three redesigned heroes and the approved Classics, his base Classic beside him", cell_w=2400)
grid("near", [("near_front.png", "front: MANG KANOR (base), ELDER, LOLA PACING"), ("near_34.png", "three-quarter"),
              ("near_back.png", "back: LOLA PACING, ELDER, MANG KANOR")], 1,
     "The finish beside his base Classic and Lola Pacing, close", cell_w=1700)
cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter")):
    cells += [(f"faces_dante_{view}.png", "DANTE: " + vlab), (f"faces_new_{view}.png", "ELDER: " + vlab),
              (f"faces_rafi_{view}.png", "RAFI: " + vlab), (f"faces_kanor_{view}.png", "MANG KANOR: " + vlab)]
grid("face", cells, 4, "His face beside two heroes' (Dante, Rafi) and his base Classic's", cell_w=520, label=17)
grid("turn", [("turn.png", "front, three-quarter, side, rear three-quarter, back"),
              ("turn2.png", "the other three-quarter, the other side, the other rear three-quarter")], 1,
     "The elder " + V + ": turnaround", cell_w=2000)
IDLE = (("00", "the stand"), ("watch", "watching the play"), ("shade", "shading his eyes"), ("thump", "the approving thump"),
        ("point", "pointing it out"))
cells = []
for tag, lab in IDLE:
    for view in ("front", "34", "side"):
        cells.append((f"idle_{tag}_{view}.png", f"idle, {lab}: {view}"))
grid("idle", cells, 6, "The idle's stances, leaning on the spear", cell_w=360, label=13)
cells = []
for tag, lab in (("cheer", "cheer, top of the hop"), ("clap", "clap, the hands meet and the butt thumps"),
                 ("clap_apart", "clap, between"), ("sit", "sit on a 0.2 m ledge")):
    for view in ("front", "34", "side", "back34"):
        cells.append((f"{tag}_{view}.png", f"{lab}: {view}"))
grid("clips", cells, 4, "cheer, clap and sit, at key frames", cell_w=420, label=14)
cells = []
for tag in ("yaw55", "yawm55", "nod22", "yaw55_nod22"):
    for view in ("front", "back"):
        cells.append((f"look_{tag}_{view}.png", f"{tag}: {view}"))
grid("look", cells, 4, "The head bone alone turned 55 and nodded 22 (rig test)", cell_w=420, label=14)
grid("close", [(f"close_{v}.png", v) for v in ("front", "34", "left", "back", "head34", "above", "feet")], 4, "Close pass", cell_w=520)
