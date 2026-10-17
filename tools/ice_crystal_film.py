"""Film the ice crystals in the open editor, then join the video and a contact sheet.

  py -3 tools/ice_crystal_film.py <tag> [--join-only] [--round2]        (run from the worktree root)

--round2 films the second set, which is kept aside and loaded by nothing (Resources/Models/IceCrystalsRound2).

Asks `IceCrystalFilm` (Assets/TumbangPreso/Editor/MapKit/IceCrystalFilm.cs) for a film by writing the tag to
Temp/ice-crystal-film.request, and waits for Logs/ice-crystals/done_<tag>.txt or failed_<tag>.txt. The editor has to be
open and not in Play. It holds the one film lock (Temp/cutscene-film.lock, the cutscene films' own) from the request
to the last frame, so no other session's save recompiles the editor in the middle of this film.

Writes Logs/ice-crystals/film_<tag>.mp4 (once at 1x, then at 0.5x) and Logs/ice-crystals/film_<tag>_sheet.jpg: each
crystal at eight points of its turn. Prints NO FILM if the editor never answered, FAILED with the exception if it threw.
"""
import glob
import os
import sys
import time

import cv2

tag = sys.argv[1]
root = os.path.join("Logs", "ice-crystals")
lock = os.path.join("Temp", "cutscene-film.lock")
done, failed = os.path.join(root, "done_%s.txt" % tag), os.path.join(root, "failed_%s.txt" % tag)
request = os.path.join("Temp", "ice-crystal-film.request")

if "--join-only" not in sys.argv:
    held = False
    for _ in range(400):
        try:
            os.mkdir(lock)
            held = True
            break
        except FileExistsError:
            # A lock nobody has touched for 12 minutes belongs to a session that died.
            try:
                if time.time() - os.path.getmtime(lock) > 720:
                    os.rmdir(lock)
            except OSError:
                pass
            time.sleep(3)
    if not held:
        raise SystemExit("NO FILM: the film lock was never free")
    try:
        for path in (done, failed):
            if os.path.exists(path):
                os.remove(path)
        with open(request, "w") as f:
            f.write(tag + (" 2" if "--round2" in sys.argv else "") + "\n")
        for _ in range(100):
            time.sleep(3)
            if os.path.exists(done) or os.path.exists(failed):
                break
    finally:
        try:
            os.rmdir(lock)
        except OSError:
            pass
    if os.path.exists(failed):
        print("FAILED", tag)
        print(open(failed).read()[:1500])
        raise SystemExit(1)
    if not os.path.exists(done):
        if os.path.exists(request):
            os.remove(request)
        raise SystemExit("NO FILM %s: the editor never answered (closed, in Play, or it does not have the film yet)" % tag)
    print(open(done).read().strip())

frames = sorted(glob.glob(os.path.join(root, "film_%s_frames" % tag, "f*.png")))
images = [cv2.imread(f) for f in frames]
h, w = images[0].shape[:2]
path = os.path.join(root, "film_%s.mp4" % tag)
out = cv2.VideoWriter(path, cv2.VideoWriter_fourcc(*"mp4v"), 60, (w, h))
for label, repeat in (("1x", 1), ("0.5x", 2)):
    for im in images:
        shown = im.copy()
        cv2.putText(shown, label, (16, 36), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 0), 5)
        cv2.putText(shown, label, (16, 36), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (255, 255, 255), 2)
        for _ in range(repeat):
            out.write(shown)
out.release()
print("wrote", path, len(images), "frames, 1x then 0.5x")

# The sheet: a row a crystal, eight points of its turn, cropped to the middle of the frame where the crystal stands.
turn = 120
rows = []
for c in range(len(images) // turn):
    picks = [images[c * turn + k * turn // 8][:, w // 2 - h // 2: w // 2 + h // 2] for k in range(8)]
    rows.append(cv2.hconcat([cv2.resize(p, (300, 300), interpolation=cv2.INTER_AREA) for p in picks]))
sheet = os.path.join(root, "film_%s_sheet.jpg" % tag)
cv2.imwrite(sheet, cv2.vconcat(rows), [cv2.IMWRITE_JPEG_QUALITY, 90])
print("wrote", sheet)
