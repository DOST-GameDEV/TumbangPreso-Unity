"""usage: video.py <hero_tag>   (run from the kanto worktree root)
Joins a cutscene film's frames into an mp4: once at 1x, then once at 0.5x. Nothing else (owner 2026-10-08: "i just need a
1x and .5x version for the video renders")."""
import glob
import os
import sys

import cv2

name = sys.argv[1]
root = os.path.join("Logs", "paete-ability-film")
frames = sorted(glob.glob(os.path.join(root, "introfx_%s_frames" % name, "f*.png")))
first = cv2.imread(frames[0])
h, w = first.shape[:2]
size = (w * 2, h * 2)
path = os.path.join(root, "introfx_%s.mp4" % name)
out = cv2.VideoWriter(path, cv2.VideoWriter_fourcc(*"mp4v"), 30, size)
images = [cv2.resize(cv2.imread(f), size, interpolation=cv2.INTER_LINEAR) for f in frames]
for label, repeat in (("1x", 1), ("0.5x", 2)):
    for im in images:
        shown = im.copy()
        cv2.putText(shown, label, (16, 40), cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 0, 0), 5)
        cv2.putText(shown, label, (16, 40), cv2.FONT_HERSHEY_SIMPLEX, 1.0, (255, 255, 255), 2)
        for _ in range(repeat):
            out.write(shown)
out.release()
print("wrote", path, len(images), "frames, 1x then 0.5x")
