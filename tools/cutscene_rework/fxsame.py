"""usage: fxsame.py trails|whirlwind|cloud|cloudline <tag>   (run from the kanto worktree root)
Has a bench film stayed what the owner approved? Lays the kept frames of the approved film
(tools/cutscene_rework/approved/<effect>_<approved tag>/frames) against the same frames of film <tag> and prints, for
each, the mean difference and the share of pixels more than 8 levels out, in 0..255. Exits 1 if any frame's mean is
over half a level.

The trails are the case this was written for: the owner approved film t32 ("trails looks good"), and the trails share
`FxStudyRibbon.shader` and the bench's bloom with every other effect, so a change made for another effect can move
them without anybody looking. Two films of unchanged code are not byte identical (the frames are rendered through a
blooming half-float target and written as PNG), so this measures, it does not compare bytes."""
import glob
import os
import sys

import cv2
import numpy as np

APPROVED = {"trails": "t32", "whirlwind": "h9", "cloud": "g1", "cloudline": "g3"}
effect, tag = sys.argv[1], sys.argv[2]
kept = os.path.join("tools", "cutscene_rework", "approved", "%s_%s" % (effect, APPROVED[effect]), "frames")
worst = 0.0
for path in sorted(glob.glob(os.path.join(kept, "f*.png"))):
    name = os.path.basename(path)
    other = os.path.join("Logs", "paete-ability-film", "fxbench_%s_%s_frames" % (effect, tag), name)
    if not os.path.exists(other):
        print("  %s  MISSING in %s" % (name, tag))
        worst = 255.0
        continue
    a, b = cv2.imread(path).astype(np.float32), cv2.imread(other).astype(np.float32)
    gap = np.abs(a - b)
    mean, share = float(gap.mean()), float((gap.max(2) > 8).mean())
    worst = max(worst, mean)
    print("  %s  mean difference %.3f of 255   pixels over 8 levels out %.2f%%   largest %d" % (name, mean, share * 100, int(gap.max())))
print("%s %s against approved %s: worst frame mean %.3f  ->  %s" % (effect, tag, APPROVED[effect], worst, "SAME" if worst <= 0.5 else "CHANGED"))
sys.exit(0 if worst <= 0.5 else 1)
