"""Join rendered frames into a video the owner can play (OpenCV; there is no ffmpeg on this machine).

  py -3 tools/video_paete_ability.py body vine v02            the body film's frames (Editor/MapKit/PaeteAbilityFilm.cs)
  py -3 tools/video_paete_ability.py hands paete_v11 25.2 27.2  a stretch of a first-person render (Editor/FpvHandLifeProbe.cs)

Writes an .mp4 beside the frames: the move at full speed, then again at a third of the speed, twice over, so a
quarter-second wind-up can actually be looked at.
"""
import glob
import os
import sys

import cv2


def write(path, frames, fps, slow=3, loops=2, scale=1):
    first = cv2.imread(frames[0])
    h, w = first.shape[:2]
    size = (w * scale, h * scale)
    out = cv2.VideoWriter(path, cv2.VideoWriter_fourcc(*"mp4v"), fps, size)
    images = [cv2.resize(cv2.imread(f), size, interpolation=cv2.INTER_NEAREST if scale > 1 else cv2.INTER_AREA) for f in frames]
    for _ in range(loops):
        for im in images:
            out.write(im)
        for im in images:
            labelled = im.copy()
            cv2.putText(labelled, "1/%d speed" % slow, (10, 24), cv2.FONT_HERSHEY_SIMPLEX, .6, (255, 255, 255), 2)
            for _ in range(slow):
                out.write(labelled)
    out.release()
    print("wrote", path, len(images), "frames")


kind = sys.argv[1]
if kind == "body":
    subject, tag = sys.argv[2], sys.argv[3]
    root = os.path.join("Logs", "paete-ability-film")
    frames = sorted(glob.glob(os.path.join(root, "%s_%s_frames" % (subject, tag), "f*.png")))
    write(os.path.join(root, "%s_%s.mp4" % (subject, tag)), frames, 30, scale=2)
else:
    run, start, end = sys.argv[2], float(sys.argv[3]), float(sys.argv[4])
    root = os.path.join("Logs", "shots-fpv-hands", run)
    frames = sorted(glob.glob(os.path.join(root, "f*.png")))[int(start * 24):int(end * 24)]
    write(os.path.join(root, "clip_%s_%s.mp4" % (sys.argv[3], sys.argv[4])), frames, 24, scale=2)
