"""usage: sheet.py <hero_tag> <out name> <frame numbers...>   (run from the kanto worktree root). A 4-wide contact sheet."""
import glob
import sys

import cv2

name, out = sys.argv[1], sys.argv[2]
pick = [int(x) for x in sys.argv[3:]]
fs = sorted(glob.glob("Logs/paete-ability-film/introfx_%s_frames/f*.png" % name))
ims = [cv2.imread(fs[min(i, len(fs) - 1)]) for i in pick]
for im, i in zip(ims, pick):
    cv2.putText(im, "%.2fs" % (i / 30.0), (8, 22), cv2.FONT_HERSHEY_SIMPLEX, .6, (255, 255, 255), 2)
while len(ims) % 4:
    ims.append(ims[-1] * 0)
rows = [cv2.hconcat(ims[r:r + 4]) for r in range(0, len(ims), 4)]
sheet = cv2.vconcat(rows)
h, w = sheet.shape[:2]
cv2.imwrite("Logs/paete-ability-film/%s.jpg" % out, cv2.resize(sheet, (1920, int(h * 1920 / w))), [cv2.IMWRITE_JPEG_QUALITY, 88])
print(len(fs), "frames")
