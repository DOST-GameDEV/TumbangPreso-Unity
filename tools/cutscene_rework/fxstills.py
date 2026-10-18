"""usage: fxstills.py <effect> <tag> <columns> <age> [<age> ...] [--crop x0 y0 x1 y1]   (run from the kanto worktree root)
Chosen moments of ONE bench film, large, in a grid: for an effect that is our own and has no reference to stand beside.
The crop is in fractions of the frame (default the whole of it). Writes Logs/paete-ability-film/stills_<effect>_<tag>.jpg.
A contact sheet of thirty small frames hides exactly what has to be judged (an edge, a ball, a blown-out patch)."""
import glob
import sys

import cv2

args = sys.argv[1:]
crop = (0.0, 0.0, 1.0, 1.0)
if "--crop" in args:
    at = args.index("--crop")
    crop = tuple(float(v) for v in args[at + 1:at + 5])
    args = args[:at] + args[at + 5:]
name = ""
if "--name" in args:
    at = args.index("--name")
    name = "_" + args[at + 1]
    args = args[:at] + args[at + 2:]
effect, tag, columns = args[0], args[1], int(args[2])
frames = sorted(glob.glob("Logs/paete-ability-film/fxbench_%s_%s_frames/f*.png" % (effect, tag)))
cells = []
for age in [float(a) for a in args[3:]]:
    image = cv2.imread(frames[min(len(frames) - 1, int(round(age * 60)))])
    h, w = image.shape[:2]
    part = image[int(crop[1] * h):int(crop[3] * h), int(crop[0] * w):int(crop[2] * w)]
    wide = 1920 // columns
    part = cv2.resize(part, (wide, int(round(wide * part.shape[0] / part.shape[1]))), interpolation=cv2.INTER_CUBIC)
    cv2.putText(part, "%s %s  %.2f s" % (effect, tag, age), (10, 28), cv2.FONT_HERSHEY_SIMPLEX, .8, (120, 255, 120), 2)
    cells.append(part)
while len(cells) % columns:
    cells.append(cells[-1] * 0)
rows = [cv2.hconcat(cells[i:i + columns]) for i in range(0, len(cells), columns)]
out = "Logs/paete-ability-film/stills_%s_%s%s.jpg" % (effect, tag, name)
cv2.imwrite(out, cv2.vconcat(rows), [cv2.IMWRITE_JPEG_QUALITY, 91])
print("wrote", out)
