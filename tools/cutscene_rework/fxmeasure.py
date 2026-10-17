"""usage: fxmeasure.py <effect> <tag> [--blend]   (run from the kanto worktree root)

The effects bench's ruler. fxcompare.py puts the reference beside ours for the eye; this puts NUMBERS beside them, because
"ours is denser" cannot be tuned by looking at a small sheet. For each stage it reads the reference frame and ours at the
same moment and the same framing and prints, reference / ours / ratio:

  trails, for each of the five ribbons: coverage, brightness (mean and 90th percentile), the share that is white core,
          coloured body and faint halo, the body's hue and saturation, the width at ten heights, the number and size of
          holes and of bright blobs ("busy or calm"), and the scroll speed along the ribbon.
  wisp,   at each moment: the area that is blinding, pale cyan, deep blue and black, where each sits and how tall the
          blue stands.

A ratio further than 15 percent from 1 is marked with *. `--blend` also writes a 50/50 overlay of reference and ours per
moment (Logs/paete-ability-film/blend_<effect>_<tag>_<n>.jpg): if the floor grids and boards do not line up there, the
framing is wrong and every other number lies.

The reference videos are study material outside the repository (see fxcompare.py, which this borrows its table from).
"""
import glob
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fxcompare  # noqa: E402

W, H = 1280, 720

# Each ribbon's axis in the reference frame (1280 x 720): its foot on its board, and its tip.
TRAIL_AXES = [((107, 429), (300, 95)), ((344, 429), (480, 90)), ((640, 429), (640, 110)), ((935, 429), (795, 110)), ((1172, 429), (965, 85))]
BOARD_TOP = 426


def frames(effect, tag):
    video, crop, zero, ages, _ = fxcompare.EFFECTS[effect]
    ours = sorted(glob.glob("Logs/paete-ability-film/fxbench_%s_%s_frames/f*.png" % (effect, tag)))
    cap = cv2.VideoCapture(os.path.join(fxcompare.REFS, video))
    fps = cap.get(cv2.CAP_PROP_FPS)
    count = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    w, h = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH)), int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    box = (int(crop[0] * w), int(crop[1] * h), int(crop[2] * w), int(crop[3] * h))

    def reference(age):
        cap.set(cv2.CAP_PROP_POS_FRAMES, min(count - 1, int(round((zero + age) * fps))))
        ok, frame = cap.read()
        return cv2.resize(frame[box[1]:box[3], box[0]:box[2]], (W, H), interpolation=cv2.INTER_AREA)

    def mine(age):
        return cv2.resize(cv2.imread(ours[min(len(ours) - 1, int(round(age * 60)))]), (W, H), interpolation=cv2.INTER_AREA)

    return reference, mine, ages, 1.0 / fps


def hsv(image):
    out = cv2.cvtColor(image, cv2.COLOR_BGR2HSV).astype(np.float32)
    return out[..., 0] * 2.0, out[..., 1] / 255.0, out[..., 2] / 255.0


def show(name, a, b, digits=2):
    ratio = (b / a) if abs(a) > 1e-6 else float("nan")
    flag = "*" if not (0.85 <= ratio <= 1.15) else " "
    print(("  %-22s ref %9." + str(digits) + "f   ours %9." + str(digits) + "f   x%5.2f %s") % (name, a, b, ratio, flag))


# ---------------------------------------------------------------- trails

def nearest_axis():
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    best, which, along = np.full((H, W), 1e9, np.float32), np.zeros((H, W), np.int32), np.zeros((H, W), np.float32)
    for i, ((fx, fy), (tx, ty)) in enumerate(TRAIL_AXES):
        dx, dy = tx - fx, ty - fy
        t = ((xs - fx) * dx + (ys - fy) * dy) / (dx * dx + dy * dy)
        tc = np.clip(t, 0, 1)
        d = np.hypot(xs - (fx + tc * dx), ys - (fy + tc * dy))
        closer = d < best
        best[closer], which[closer], along[closer] = d[closer], i, t[closer]
    return which, along


def trail_stats(image, which, along, index):
    hue, sat, val = hsv(image)
    covered = (((sat > 0.22) & (val > 0.30)) | (val > 0.78)) & (which == index)
    covered[BOARD_TOP:, :] = False
    area = float(covered.sum())
    if area < 50:
        return None
    core = covered & (val > 0.86) & (sat < 0.40)
    body = covered & ~core & (sat >= 0.38) & (val > 0.45)
    faint = covered & ~core & ~body
    (fx, fy), (tx, ty) = TRAIL_AXES[index]
    length = float(np.hypot(tx - fx, ty - fy))
    widths = []
    for band in range(10):
        inside = covered & (along >= band / 10.0) & (along < (band + 1) / 10.0)
        widths.append(inside.sum() / (length / 10.0))
    solid = cv2.morphologyEx(covered.astype(np.uint8), cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (41, 41)))
    n_holes, _, hole_stats, _ = cv2.connectedComponentsWithStats(((solid > 0) & ~covered).astype(np.uint8))
    holes = [s for s in hole_stats[1:, cv2.CC_STAT_AREA] if s >= 30]
    n_blobs, _, blob_stats, _ = cv2.connectedComponentsWithStats(core.astype(np.uint8))
    blobs = [s for s in blob_stats[1:, cv2.CC_STAT_AREA] if s >= 12]
    return {
        "coverage px": area, "mean brightness": float(val[covered].mean()), "p90 brightness": float(np.percentile(val[covered], 90)),
        "core share": core.sum() / area, "body share": body.sum() / area, "faint share": faint.sum() / area,
        "body hue": float(hue[body].mean()) if body.any() else 0.0,
        "hue spread 5-95": float(np.percentile(hue[body], 95) - np.percentile(hue[body], 5)) if body.any() else 0.0, "body saturation": float(sat[body].mean()) if body.any() else 0.0,
        "mean width px": float(np.mean(widths)), "max width px": float(np.max(widths)),
        "width foot third": float(np.mean(widths[:3])), "width mid third": float(np.mean(widths[3:7])), "width tip third": float(np.mean(widths[7:])),
        "height reached": float(along[covered].max()), "solid px": float(solid.sum()),
        "holes count": float(len(holes)), "holes mean px": float(np.mean(holes)) if holes else 0.0,
        "bright blobs count": float(len(blobs)), "bright blobs mean px": float(np.mean(blobs)) if blobs else 0.0,
        "core rgb": image[core].mean(axis=0)[::-1] if core.any() else np.zeros(3), "body rgb": image[body].mean(axis=0)[::-1] if body.any() else np.zeros(3),
        "faint rgb": image[faint].mean(axis=0)[::-1] if faint.any() else np.zeros(3),
    }


def unroll(image, index, length=160, across=120):
    """The ribbon straightened: its axis laid along x, so a scroll along it is a shift in x."""
    (fx, fy), (tx, ty) = TRAIL_AXES[index]
    dx, dy = tx - fx, ty - fy
    n = np.hypot(dx, dy)
    ux, uy, px, py = dx / n, dy / n, -dy / n, dx / n
    ts, ss = np.meshgrid(np.linspace(0.1, 0.9, length) * n, np.linspace(-0.5, 0.5, across) * n * 0.45)
    grey = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY).astype(np.float32)
    return cv2.remap(grey, (fx + ux * ts + px * ss).astype(np.float32), (fy + uy * ts + py * ss).astype(np.float32), cv2.INTER_LINEAR), n * 0.8 / length


def scroll_speed(get, step, index):
    """Ribbon lengths a second that the picture moves toward the tip: the shift that best lines one frame up with the next."""
    speeds = []
    for start in (0.5, 1.0, 1.5, 2.0, 2.5, 3.0):
        a, scale = unroll(get(start), index)
        b, _ = unroll(get(start + step), index)
        a, b = a - a.mean(), b - b.mean()
        best, best_shift = -1e18, 0
        for shift in range(-25, 26):
            if shift >= 0:
                score = (a[:, :a.shape[1] - shift] * b[:, shift:]).mean()
            else:
                score = (a[:, -shift:] * b[:, :b.shape[1] + shift]).mean()
            if score > best:
                best, best_shift = score, shift
        speeds.append(best_shift * scale / step)
    (fx, fy), (tx, ty) = TRAIL_AXES[index]
    return float(np.median(speeds)) / float(np.hypot(tx - fx, ty - fy))


def trails(tag, blend):
    reference, mine, ages, ref_step = frames("trails", tag)
    which, along = nearest_axis()
    names = ["1 shards", "2 smoke", "3 ink", "4 squiggle", "5 bands"]
    for index in range(5):
        ref_all, our_all = [], []
        for age in ages:
            a, b = trail_stats(reference(age), which, along, index), trail_stats(mine(age), which, along, index)
            if a and b:
                ref_all.append(a)
                our_all.append(b)
        print("RIBBON", names[index], "(mean of %d moments)" % len(ref_all))
        for key in ref_all[0]:
            a, b = np.mean([r[key] for r in ref_all], axis=0), np.mean([r[key] for r in our_all], axis=0)
            if key.endswith("rgb"):
                print("  %-22s ref (%3d,%3d,%3d)   ours (%3d,%3d,%3d)" % ((key,) + tuple(int(v) for v in a) + tuple(int(v) for v in b)))
            else:
                show(key, float(a), float(b), 0 if "px" in key else 2)
        show("scroll lengths/s", scroll_speed(reference, ref_step, index), scroll_speed(mine, 1 / 30.0, index))
    if blend:
        for n, age in enumerate(ages[:3]):
            cv2.imwrite("Logs/paete-ability-film/blend_trails_%s_%d.jpg" % (tag, n), cv2.addWeighted(reference(age), 0.5, mine(age), 0.5, 0), [cv2.IMWRITE_JPEG_QUALITY, 90])


# ---------------------------------------------------------------- wisp

def wisp_stats(image):
    hue, sat, val = hsv(image)
    floor = float(np.median(val))
    classes = {
        "blinding": (val > 0.90) & (sat < 0.30),
        "pale cyan": (val > 0.55) & (sat >= 0.12) & (sat < 0.62) & (hue > 160) & (hue < 235) & ~((val > 0.90) & (sat < 0.30)),
        "deep blue": (sat > 0.62) & (val > 0.22) & (hue > 215) & (hue < 275),
        "black": val < floor - 0.055,
    }
    out = {}
    ys, xs = np.mgrid[0:H, 0:W]
    coloured = (sat > 0.25) & (val > 0.30)
    if coloured.sum() > 200:
        out["hue spread 5-95"] = float(np.percentile(hue[coloured], 95) - np.percentile(hue[coloured], 5))
        out["hue median"] = float(np.median(hue[coloured]))
    for name, mask in classes.items():
        area = float(mask.sum())
        out[name + " area%"] = 100.0 * area / (W * H)
        if area > 80:
            out[name + " centre x"] = float(xs[mask].mean()) / W
            out[name + " centre y"] = float(ys[mask].mean()) / H
            out[name + " top y"] = float(np.percentile(ys[mask], 2)) / H
            out[name + " spread x"] = float(np.percentile(xs[mask], 98) - np.percentile(xs[mask], 2)) / W
            out[name + " rgb"] = image[mask].mean(axis=0)[::-1]
    return out


def wisp(tag, blend):
    reference, mine, ages, _ = frames("wisp", tag)
    for n, age in enumerate(ages):
        ref_image, our_image = reference(age), mine(age)
        a, b = wisp_stats(ref_image), wisp_stats(our_image)
        print("WISP at %.2f s" % age)
        for key in a:
            if key not in b:
                print("  %-22s ref has it, ours has none *" % key)
            elif key.endswith("rgb"):
                print("  %-22s ref (%3d,%3d,%3d)   ours (%3d,%3d,%3d)" % ((key,) + tuple(int(v) for v in a[key]) + tuple(int(v) for v in b[key])))
            else:
                show(key, a[key], b[key], 2)
        if blend and n % 3 == 1:
            cv2.imwrite("Logs/paete-ability-film/blend_wisp_%s_%d.jpg" % (tag, n), cv2.addWeighted(ref_image, 0.5, our_image, 0.5, 0), [cv2.IMWRITE_JPEG_QUALITY, 90])


if __name__ == "__main__":
    {"trails": trails, "wisp": wisp}[sys.argv[1]](sys.argv[2], "--blend" in sys.argv)
