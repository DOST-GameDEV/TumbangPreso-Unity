"""usage: fxboards.py <texture folder> <out.jpg>
The five trail masks' gate: for each, the reference's board cropped from the video at two moments (left pair) beside
OUR mask (right), all the same size. A mask is not filmed until it passes a look at this."""
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fxcompare  # noqa: E402

folder, out = sys.argv[1], sys.argv[2]
video = cv2.VideoCapture(os.path.join(fxcompare.REFS, "Da22GHZv9kh_00001.mp4"))
names = ["trail_shards", "trail_smoke", "trail_ink", "trail_squiggle", "trail_bands"]
size = 360
rows = []
for i, x in enumerate((32, 269, 565, 860, 1097)):
    cells = []
    for frame in (20, 50):
        video.set(cv2.CAP_PROP_POS_FRAMES, frame)
        ok, image = video.read()
        cells.append(cv2.resize(image[428:577, x:x + 150], (size, size), interpolation=cv2.INTER_CUBIC))
    mine = cv2.resize(cv2.imread(os.path.join(folder, names[i] + ".png")), (size, size), interpolation=cv2.INTER_AREA)
    cv2.putText(cells[0], "REF", (6, 20), cv2.FONT_HERSHEY_SIMPLEX, .6, (0, 255, 255), 2)
    cv2.putText(mine, "OURS " + names[i], (6, 20), cv2.FONT_HERSHEY_SIMPLEX, .6, (120, 255, 120), 2)
    rows.append(cv2.hconcat(cells + [mine]))
half = cv2.vconcat(rows[:3]), cv2.vconcat(rows[3:] + [np.zeros_like(rows[0])])
cv2.imwrite(out, cv2.hconcat(list(half)), [cv2.IMWRITE_JPEG_QUALITY, 92])
print("wrote", out)
