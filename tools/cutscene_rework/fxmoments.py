"""usage: fxmoments.py <effect> <tag> <age> [<age> ...]   (run from the kanto worktree root)
The reference beside ours at a few chosen moments, ENLARGED on the middle of the frame: fxcompare.py's sheet is too
small to see a fleck's edge or whether a thing is see-through. Writes Logs/paete-ability-film/moments_<effect>_<tag>.jpg."""
import glob
import os
import sys

import cv2

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fxcompare  # noqa: E402

effect, tag = sys.argv[1], sys.argv[2]
video, crop, zero, _, _ = fxcompare.EFFECTS[effect]
cap = cv2.VideoCapture(os.path.join(fxcompare.REFS, video))
fps, w, h = cap.get(cv2.CAP_PROP_FPS), int(cap.get(3)), int(cap.get(4))
ours = sorted(glob.glob("Logs/paete-ability-film/fxbench_%s_%s_frames/f*.png" % (effect, tag)))
rows = []
for age in [float(a) for a in sys.argv[3:]]:
    cap.set(cv2.CAP_PROP_POS_FRAMES, int(round((zero + age) * fps)))
    ok, frame = cap.read()
    frame = cv2.resize(frame[int(crop[1] * h):int(crop[3] * h), int(crop[0] * w):int(crop[2] * w)], (960, 540))
    mine = cv2.resize(cv2.imread(ours[min(len(ours) - 1, int(round(age * 60)))]), (960, 540))
    pair = []
    for image, name, colour in ((frame, "REF", (0, 255, 255)), (mine, "OURS " + tag, (120, 255, 120))):
        big = cv2.resize(image[190:520, 190:770], (960, 546), interpolation=cv2.INTER_CUBIC)
        cv2.putText(big, "%s %.2fs" % (name, age), (10, 30), cv2.FONT_HERSHEY_SIMPLEX, .8, colour, 2)
        pair.append(big)
    rows.append(cv2.hconcat(pair))
out = "Logs/paete-ability-film/moments_%s_%s.jpg" % (effect, tag)
cv2.imwrite(out, cv2.vconcat(rows), [cv2.IMWRITE_JPEG_QUALITY, 90])
print("wrote", out)
