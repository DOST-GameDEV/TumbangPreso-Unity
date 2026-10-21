"""usage: fxcompare.py <effect> <tag> [cell width] [--video] [--strip <from s> <to s>]   (run from the kanto worktree root)

The effects bench's judge. Writes Logs/paete-ability-film/compare_<effect>_<tag>.jpg: for each stage of the effect, the
REFERENCE's frame at that moment on top and OUR frame at the matching moment under it, same size, labelled. The test an
effect has to pass is whether somebody shown both rows would call them the same kind of effect at the same finish, and
that can only be asked with the two side by side.

`--video` also writes compare_<effect>_<tag>.mp4: the reference on the left and ours on the right through the whole
effect, at 1x and then at 0.25x, because a sheet of poses says nothing about speed. `--strip a b` writes
strip_<effect>_<tag>.jpg: EVERY frame of the reference between two times over ours at the same moments, for the fast
parts (the first third of a second of a burst is ten frames, and each is a different picture).

The stages (which moment of the reference answers which age of ours, and which part of the reference's frame the effect
is in) are typed below per effect, from the frame-by-frame reading of the references.

The reference videos are STUDY MATERIAL and are not in the repository: they are read from the folder named by the
FXREF environment variable, or from the scratch folder they were studied in. Without them the sheet cannot be made.
"""
import glob
import os
import sys

import cv2
import numpy as np

REFS = os.environ.get("FXREF", "C:/Users/StarX/AppData/Local/Temp/claude/C--Users-StarX-Desktop-SCHOOL-career-building-projects-"
                               "TumbangPreso-Unity--claude-worktrees-heroes-ultimate-cutscenes-rework-8b26f3/"
                               "eb1c3c67-6c89-48a9-8f18-ffcea1afecc0/scratchpad/refs_vfx/video")

# effect: (video, crop of the reference frame as fractions (x0, y0, x1, y1), reference time of our age 0,
#          [ages of ours to show], stages a row)
EFFECTS = {
    # The five ribbons loop; the reference's clock and ours simply run together.
    "trails": ("Da22GHZv9kh_00001.mp4", (0.0, 0.0, 1.0, 1.0), 0.0, [0.5, 1.0, 1.5, 2.0, 2.5, 3.0], 3),
    # The finished explosion plays once at the very end of the breakdown; its first frame of light is at 61.90 s.
    # Our own effect: the reference is only what KIND of stage belongs where.
    "circle": ("DeH2wCYt9xl_00001.mp4", (0.5, 0.5, 1.0, 1.0), 0.0, [0.5, 0.9, 1.6, 2.3, 2.5, 2.7, 3.0, 3.3, 3.6, 4.1], 5),
    "wisp": ("DcQ-XbwylHx_00001.mp4", (0.0, 0.305, 1.0, 0.621), 61.90,
             [0.03, 0.08, 0.15, 0.25, 0.40, 0.55, 0.70, 0.85, 1.00, 1.15, 1.30, 1.50, 1.70, 1.90], 7),
}


def label(image, text, colour):
    cv2.putText(image, text, (8, 22), cv2.FONT_HERSHEY_SIMPLEX, .6, (0, 0, 0), 4)
    cv2.putText(image, text, (8, 22), cv2.FONT_HERSHEY_SIMPLEX, .6, colour, 2)


def main():
    effect, tag = sys.argv[1], sys.argv[2]
    video, crop, zero, ages, per_row = EFFECTS[effect]
    numbers = [a for a in sys.argv[3:] if a.replace(".", "").isdigit()]
    strip = "--strip" in sys.argv
    if strip:
        at = sys.argv.index("--strip")
        lo, hi = float(sys.argv[at + 1]), float(sys.argv[at + 2])
        numbers = [a for a in sys.argv[3:at] if a.replace(".", "").isdigit()]
    cell = int(numbers[0]) if numbers else (640 if per_row <= 3 else 400)
    ours = sorted(glob.glob("Logs/paete-ability-film/fxbench_%s_%s_frames/f*.png" % (effect, tag)))
    if not ours:
        sys.exit("no frames for fxbench_%s_%s: film it first (fxbench.sh)" % (effect, tag))
    path = os.path.join(REFS, video)
    reference = cv2.VideoCapture(path)
    if not reference.isOpened():
        sys.exit("the reference video is not at %s (set FXREF to the folder it is in)" % path)
    fps = reference.get(cv2.CAP_PROP_FPS)
    count = int(reference.get(cv2.CAP_PROP_FRAME_COUNT))
    w, h = int(reference.get(cv2.CAP_PROP_FRAME_WIDTH)), int(reference.get(cv2.CAP_PROP_FRAME_HEIGHT))
    x0, y0, x1, y1 = int(crop[0] * w), int(crop[1] * h), int(crop[2] * w), int(crop[3] * h)
    mine = cv2.imread(ours[0])
    cell_h = int(cell * mine.shape[0] / mine.shape[1])
    rows = []
    for start in range(0, len(ages), per_row):
        top, bottom = [], []
        for age in ages[start:start + per_row]:
            reference.set(cv2.CAP_PROP_POS_FRAMES, min(count - 1, int(round((zero + age) * fps))))
            ok, frame = reference.read()
            frame = frame[y0:y1, x0:x1] if ok else np.zeros((y1 - y0, x1 - x0, 3), np.uint8)
            # The reference is fitted into our cell without stretching: bars where its shape differs from ours.
            scale = min(cell / frame.shape[1], cell_h / frame.shape[0])
            fitted = cv2.resize(frame, (int(frame.shape[1] * scale), int(frame.shape[0] * scale)), interpolation=cv2.INTER_AREA)
            ref_cell = np.zeros((cell_h, cell, 3), np.uint8)
            oy, ox = (cell_h - fitted.shape[0]) // 2, (cell - fitted.shape[1]) // 2
            ref_cell[oy:oy + fitted.shape[0], ox:ox + fitted.shape[1]] = fitted
            label(ref_cell, "REF %.2fs" % age, (0, 255, 255))
            our_cell = cv2.resize(cv2.imread(ours[min(len(ours) - 1, int(round(age * 60)))]), (cell, cell_h), interpolation=cv2.INTER_AREA)
            label(our_cell, "OURS %.2fs" % age, (120, 255, 120))
            top.append(ref_cell)
            bottom.append(our_cell)
        while len(top) < per_row:
            top.append(np.zeros((cell_h, cell, 3), np.uint8))
            bottom.append(np.zeros((cell_h, cell, 3), np.uint8))
        rows += [cv2.hconcat(top), cv2.hconcat(bottom), np.full((6, cell * per_row, 3), 40, np.uint8)]
    out = "Logs/paete-ability-film/compare_%s_%s.jpg" % (effect, tag)
    cv2.imwrite(out, cv2.vconcat(rows[:-1]), [cv2.IMWRITE_JPEG_QUALITY, 92])
    print("wrote", out, "(%d frames of ours)" % len(ours))

    def pair(age, width):
        """The reference's frame and ours at `age`, each `width` wide."""
        reference.set(cv2.CAP_PROP_POS_FRAMES, min(count - 1, int(round((zero + age) * fps))))
        ok, frame = reference.read()
        frame = frame[y0:y1, x0:x1] if ok else np.zeros((y1 - y0, x1 - x0, 3), np.uint8)
        height = int(width * mine.shape[0] / mine.shape[1])
        scale = min(width / frame.shape[1], height / frame.shape[0])
        fitted = cv2.resize(frame, (int(frame.shape[1] * scale), int(frame.shape[0] * scale)), interpolation=cv2.INTER_AREA)
        left = np.zeros((height, width, 3), np.uint8)
        oy, ox = (height - fitted.shape[0]) // 2, (width - fitted.shape[1]) // 2
        left[oy:oy + fitted.shape[0], ox:ox + fitted.shape[1]] = fitted
        right = cv2.resize(cv2.imread(ours[min(len(ours) - 1, int(round(age * 60)))]), (width, height), interpolation=cv2.INTER_AREA)
        return left, right

    if strip:
        tops, bottoms = [], []
        steps = int(round((hi - lo) * fps)) + 1
        for i in range(steps):
            age = lo + i / fps
            left, right = pair(age, 320)
            label(left, "REF %.3f" % age, (0, 255, 255))
            label(right, "OURS %.3f" % age, (120, 255, 120))
            tops.append(left)
            bottoms.append(right)
        sheet_rows = []
        for start in range(0, steps, 8):
            a, b = tops[start:start + 8], bottoms[start:start + 8]
            while len(a) < 8:
                a.append(np.zeros_like(tops[0]))
                b.append(np.zeros_like(tops[0]))
            sheet_rows += [cv2.hconcat(a), cv2.hconcat(b)]
        out = "Logs/paete-ability-film/strip_%s_%s.jpg" % (effect, tag)
        cv2.imwrite(out, cv2.vconcat(sheet_rows), [cv2.IMWRITE_JPEG_QUALITY, 90])
        print("wrote", out)

    if "--video" in sys.argv:
        out = "Logs/paete-ability-film/compare_%s_%s.mp4" % (effect, tag)
        width = 640
        height = int(width * mine.shape[0] / mine.shape[1])
        writer = cv2.VideoWriter(out, cv2.VideoWriter_fourcc(*"mp4v"), 60, (width * 2, height))
        for name, repeat in (("1x", 1), ("0.25x", 4)):
            for f in range(len(ours)):
                left, right = pair(f / 60.0, width)
                label(left, "REFERENCE  %s  %.2fs" % (name, f / 60.0), (0, 255, 255))
                label(right, "OURS", (120, 255, 120))
                both = cv2.hconcat([left, right])
                for _ in range(repeat):
                    writer.write(both)
        writer.release()
        print("wrote", out)


if __name__ == "__main__":
    main()
