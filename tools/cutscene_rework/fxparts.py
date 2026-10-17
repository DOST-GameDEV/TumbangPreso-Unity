"""usage: fxparts.py <tag>   (run from the kanto worktree root)
Have the whirlwind's approved parts (its tapes, ground and motes: film h1, "rest of the sfx looks good") stayed what
they were? Lays the kept frames of h1 (tools/cutscene_rework/approved/whirlwind_h1/frames) against the same frames of
film `whirlwindparts <tag>`, which is the whirlwind built without its horn. h1 had a horn, so inside the box a horn
stands in the two are different pictures; outside it they should be the same but for what h1's bright horn spilled
through the bloom. Prints, per frame, the mean and largest difference outside the box and the share of pixels there
more than 8 levels out, in 0..255. Exits 1 if any frame's mean outside the box is over half a level."""
import glob
import os
import sys

import cv2
import numpy as np

BOX = (330, 0, 690, 400)   # x0, y0, x1, y1 on a 960 by 540 frame
tag = sys.argv[1]
kept = os.path.join("tools", "cutscene_rework", "approved", "whirlwind_h1", "frames")
worst = 0.0
for path in sorted(glob.glob(os.path.join(kept, "f*.png"))):
    name = os.path.basename(path)
    other = os.path.join("Logs", "paete-ability-film", "fxbench_whirlwindparts_%s_frames" % tag, name)
    if not os.path.exists(other):
        print("  %s  MISSING in %s" % (name, tag))
        worst = 255.0
        continue
    gap = np.abs(cv2.imread(path).astype(np.float32) - cv2.imread(other).astype(np.float32)).max(axis=2)
    outside = np.ones(gap.shape, bool)
    outside[BOX[1]:BOX[3], BOX[0]:BOX[2]] = False
    g = gap[outside]
    worst = max(worst, float(g.mean()))
    print("  %s  outside the horn's box: mean %.3f of 255   largest %d   over 8 levels out %.2f%%" % (name, g.mean(), int(g.max()), 100.0 * (g > 8).mean()))
print("whirlwind parts %s against approved h1: worst frame mean %.3f  ->  %s" % (tag, worst, "SAME" if worst <= 0.5 else "DIFFERENT"))
sys.exit(0 if worst <= 0.5 else 1)
