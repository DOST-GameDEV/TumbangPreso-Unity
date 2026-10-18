"""PAYO ASSEMBLY: the review sheets of a round, from Logs/payo/assembly/shots_vN into Logs/payo/assembly/payo_assembly_<sheet>_vN.png.
  python tools/payo_assemble_sheet.py N [sheet ...]      sheets: can, blocks, wide, night, compare
"""
import os
import sys

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Logs/payo/assembly")
N = sys.argv[1]
SHOTS = os.path.join(OUT, "shots_v" + N)
SHEETS = {
    "can": (4, ["a_can_n", "a_can_ne", "a_can_e", "a_can_se", "a_can_s", "a_can_sw", "a_can_w", "a_can_nw"], "from 1.7 m at the can, eight directions"),
    "blocks": (4, ["b_midN_out", "b_midE_out", "b_midS_out", "b_midW_out", "b_midN_back", "b_midE_back", "b_midS_back", "b_midW_back"], "from 1.7 m on four outer blocks: outward, and back across the court"),
    "wide": (2, ["c_gate_stair", "c_gate_foot", "d_town", "d_air_south", "d_air_north", "d_air_top"], "from the gate stair; the town; from the air"),
    "stream": (3, ["a_can_fall", "f_stream", "f_gorge", "f_bench2", "f_east", "f_west", "f_knoll"], "the great fall and its stream; the benches; the east and west subjects; the knoll"),
    "night": (3, ["a_can_s", "e_night_s", "e_night_town", "a_can_e", "e_night_e", "e_night_air", "e_night_n", "e_night_w"], "day and night (glows, flames, lamps, stars, Mayari)"),
}


def sheet(name, cols, shots, title, width=1500):
    ims = []
    for s in shots:
        p = os.path.join(SHOTS, s + ".png")
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGB")
        cw = width // cols
        im = im.resize((cw, int(im.height * cw / im.width)), Image.LANCZOS)
        d = ImageDraw.Draw(im); d.rectangle([0, 0, 8 + 7 * len(s), 16], fill=(16, 18, 24)); d.text((4, 2), s, fill=(255, 255, 255))
        ims.append(im)
    if not ims:
        return
    rows = [ims[i:i + cols] for i in range(0, len(ims), cols)]
    H = 22 + sum(max(i.height for i in r) for r in rows)
    out = Image.new("RGB", (width, H), (16, 18, 24))
    ImageDraw.Draw(out).text((6, 4), "PAYO ASSEMBLY v%s: %s" % (N, title), fill=(255, 255, 255))
    y = 22
    for r in rows:
        for k, im in enumerate(r):
            out.paste(im, (k * (width // cols), y))
        y += max(i.height for i in r)
    path = os.path.join(OUT, "payo_assembly_%s_v%s.png" % (name, N))
    out.save(path); print(path, out.size)


def compare():
    """One eye-height view beside the rejected whole-map pass and the owner's two references."""
    tmp = r"C:\Users\StarX\AppData\Local\Temp\claude\C--Users-StarX-Desktop-SCHOOL-career-building-projects-TumbangPreso-Unity--claude-worktrees-kanto-blender-assembly-909442\a7a8e7e5-636c-4f00-bb54-3aaf12020ecc\images"
    cells = [("THIS ASSEMBLY, 1.7 m at the can, looking south to the gate", os.path.join(SHOTS, "a_can_s.png")),
             ("THIS ASSEMBLY, from the south block looking out", os.path.join(SHOTS, "b_midS_out.png")),
             ("THE REJECTED PASS (payo_art_sheet_outer_v8)", os.path.join(ROOT, "Logs/payo/art/payo_art_sheet_outer_v8.png")),
             ("the owner's reference 238", os.path.join(tmp, "238.webp")), ("the owner's reference 239", os.path.join(tmp, "239.webp")),
             ("THIS ASSEMBLY, the town from above the east rim", os.path.join(SHOTS, "d_town.png"))]
    cw = 750
    ims = []
    for label, p in cells:
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGB"); im = im.resize((cw, int(im.height * cw / im.width)), Image.LANCZOS)
        d = ImageDraw.Draw(im); d.rectangle([0, 0, 10 + 6 * len(label), 16], fill=(16, 18, 24)); d.text((4, 2), label, fill=(255, 255, 255))
        ims.append(im)
    rows = [ims[i:i + 2] for i in range(0, len(ims), 2)]
    H = sum(max(i.height for i in r) for r in rows)
    out = Image.new("RGB", (cw * 2, H), (16, 18, 24)); y = 0
    for r in rows:
        for k, im in enumerate(r):
            out.paste(im, (k * cw, y))
        y += max(i.height for i in r)
    path = os.path.join(OUT, "payo_assembly_compare_v%s.png" % N)
    out.save(path); print(path, out.size)


want = sys.argv[2:] or list(SHEETS) + ["compare"]
for w in want:
    if w == "compare":
        compare()
    else:
        sheet(w, *SHEETS[w])
