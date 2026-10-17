"""usage: fxclip.py <effect> <tag> [from seconds] [to seconds]
How much of each frame of a bench film is blown out: the share of its pixels at or over 250 in all three channels
(white, nothing left to read) and the share at or over 250 in any one (a clipped colour). Prints the worst frames.
A frame that is a white-out says so here as a number, before anybody has to look at it."""
import glob
import os
import sys

import cv2
import numpy as np

effect, tag = sys.argv[1], sys.argv[2]
start = float(sys.argv[3]) if len(sys.argv) > 3 else 0.0
end = float(sys.argv[4]) if len(sys.argv) > 4 else 1e9
frames = sorted(glob.glob(os.path.join("Logs", "paete-ability-film", "fxbench_%s_%s_frames" % (effect, tag), "f*.png")))
rows = []
for i, path in enumerate(frames):
    age = i / 60.0
    if age < start or age > end:
        continue
    image = cv2.imread(path)
    white = float((image.min(2) >= 250).mean())
    any_one = float((image.max(2) >= 250).mean())
    rows.append((white, any_one, age, i))
rows.sort(reverse=True)
print("%s %s: %d frames looked at" % (effect, tag, len(rows)))
for white, any_one, age, i in rows[:6]:
    print("  %.2f s (f%03d)  white %.1f%%  any channel clipped %.1f%%" % (age, i, white * 100, any_one * 100))
over = sorted(age for white, any_one, age, i in rows if white > 0.12)
print("  frames over 12%% white: %d%s" % (len(over), "  (%.2f to %.2f s)" % (over[0], over[-1]) if over else ""))
