"""usage: fxvideo.py <effect_tag>   (run from the kanto worktree root)
Joins an effects bench film's frames (Logs/paete-ability-film/fxbench_<effect_tag>_frames, sixty a second) into an mp4:
once at 1x, then once at 0.5x. video.py does the same for a cutscene film; this one differs only in the names and the
frame rate."""
import glob
import os
import sys

import cv2

name = sys.argv[1]
root = os.path.join("Logs", "paete-ability-film")
frames = sorted(glob.glob(os.path.join(root, "fxbench_%s_frames" % name, "f*.png")))
first = cv2.imread(frames[0])
h, w = first.shape[:2]
path = os.path.join(root, "fxbench_%s.mp4" % name)
out = cv2.VideoWriter(path, cv2.VideoWriter_fourcc(*"mp4v"), 60, (w, h))
images = [cv2.imread(f) for f in frames]
for label, repeat in (("1x", 1), ("0.5x", 2)):
    for im in images:
        shown = im.copy()
        cv2.putText(shown, label, (16, 36), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 0), 5)
        cv2.putText(shown, label, (16, 36), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (255, 255, 255), 2)
        for _ in range(repeat):
            out.write(shown)
out.release()
print("wrote", path, len(images), "frames, 1x then 0.5x")
