"""Join the frames `FpvHandLifeProbe` wrote into a contact sheet (for judging poses) and a gif (for judging motion).

    py -3 tools/sheet_fpv_hand_life.py nemu_v01 [--every 6] [--from 12.0 --to 16.5]

Reads Logs/shots-fpv-hands/<run>/f####.png and beats.txt; writes sheet.png (or sheet_<from>_<to>.png) and
motion.gif beside them. Each tile carries its time and the beat it belongs to.
"""
import argparse
import glob
import os

from PIL import Image, ImageDraw

FPS = 24

ap = argparse.ArgumentParser()
ap.add_argument("run")
ap.add_argument("--every", type=int, default=8, help="one tile every N frames")
ap.add_argument("--from", dest="start", type=float, default=None)
ap.add_argument("--to", dest="end", type=float, default=None)
ap.add_argument("--cols", type=int, default=6)
ap.add_argument("--crop", default=None, help="x0,y0,x1,y1 of each frame to keep")
a = ap.parse_args()

root = os.path.join("Logs", "shots-fpv-hands", a.run)
frames = sorted(glob.glob(os.path.join(root, "f*.png")))
if not frames:
    raise SystemExit("no frames in " + root)
beats = []
if os.path.exists(os.path.join(root, "beats.txt")):
    for line in open(os.path.join(root, "beats.txt")):
        when, _, what = line.strip().partition(" ")
        if what:
            beats.append((float(when), what))


def beat(t):
    name = ""
    for when, what in beats:
        if t >= when:
            name = what
    return name


crop = tuple(int(v) for v in a.crop.split(",")) if a.crop else None
picked = []
for i, path in enumerate(frames):
    t = i / FPS
    if a.start is not None and t < a.start:
        continue
    if a.end is not None and t > a.end:
        continue
    if i % a.every:
        continue
    picked.append((t, path))

tiles = []
for t, path in picked:
    im = Image.open(path).convert("RGB")
    if crop:
        im = im.crop(crop)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, im.width, 13], fill=(20, 22, 30))
    d.text((3, 1), "%.2fs  %s" % (t, beat(t)), fill=(255, 255, 255))
    tiles.append(im)
w, h = tiles[0].size
rows = (len(tiles) + a.cols - 1) // a.cols
sheet = Image.new("RGB", (w * a.cols, h * rows), (20, 22, 30))
for k, im in enumerate(tiles):
    sheet.paste(im, ((k % a.cols) * w, (k // a.cols) * h))
name = "sheet.png" if a.start is None and a.end is None else "sheet_%s_%s.png" % (a.start, a.end)
sheet.save(os.path.join(root, name))
print("wrote", os.path.join(root, name), sheet.size, len(tiles), "tiles")

if a.start is None and a.end is None:
    gif = [Image.open(p).convert("RGB").resize((480, 270)) for p in frames[::2]]
    gif[0].save(os.path.join(root, "motion.gif"), save_all=True, append_images=gif[1:], duration=1000 // 12, loop=0)
    print("wrote", os.path.join(root, "motion.gif"), len(gif), "frames")
