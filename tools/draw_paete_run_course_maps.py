"""PAETE'S RUN: three course MAPS for the owner to choose a shape from (2026-10-10). Drawings, not models.

  py -3 tools/draw_paete_run_course_maps.py   -> Logs/paete-run/paete_run_course_maps_vN.png

Two tries at grey 3D blockouts of these (tools/author_paete_run_blockout.py) could not be read: the rock under the
course hid the course. A course is a line with kinds of going along it, so each is drawn here twice: from above, and as
its height from start to finish. Way points are (x, y, z) in metres; a leg is one kind of going.
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent.parent / "Logs/paete-run"
VERSION = 7
KIND = {"run": ((214, 186, 130), "run"), "chute": ((30, 170, 160), "SLIDE CHUTE"), "swing": ((240, 200, 30), "vine swing"),
        "swim": ((60, 140, 210), "swim"), "hops": ((120, 110, 104), "parkour hops"), "pad": ((214, 50, 40), "bounce pad"),
        "dive": ((236, 128, 40), "cliff dive")}

# ⚠️ v3 (2026-10-10): ONE course, the one he chose ("i like b"), with its shortcuts ("alternative routes / harder shortcuts
# should also be an option"). The way points now live in tools/paete_run_course.py. (v1: three shapes, all refused. v2:
# the Knot and the Switchbacks.)
import sys
sys.path.insert(0, str(Path(__file__).resolve().parent))
from paete_run_course import COURSE, SHORTCUTS as CUTS, SPEED   # noqa: E402
from paete_run_stages import STAGE                              # noqa: E402

COURSES = [("THE SWITCHBACKS, with shortcuts", "Zigzags up a stepped rock face by hops, pads and vine swings. Only TWO slides, both long: off the summit and into the finish. The other ways down are cliff dives into pools, a swing and downhill hops. The three dashed purple lines are SHORTCUTS: shorter, harder, and a fall from one sets you back.", False, COURSE)]
SHORTCUT = []


def font(size):
    for name in ("arialbd.ttf", "arial.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def bow(a, c, kind, n=16):
    """A chute is drawn as a bend, the rest straight."""
    amp = 0.22 * math.hypot(c[0] - a[0], c[1] - a[1]) if kind == "chute" else 0.0
    dx, dy = c[0] - a[0], c[1] - a[1]
    ln = math.hypot(dx, dy) or 1.0
    sx, sy = dy / ln, -dx / ln
    return [(a[0] + dx * t + sx * amp * math.sin(math.pi * t), a[1] + dy * t + sy * amp * math.sin(math.pi * t)) for t in [k / n for k in range(n + 1)]]


W, H = 2040, 1140
sheet = Image.new("RGB", (W * len(COURSES), H), (247, 242, 232))
F1, F2, F3 = font(26), font(15), font(13)
for ci, (name, words, loop, legs) in enumerate(COURSES):
    d = ImageDraw.Draw(sheet)
    ox = ci * W
    d.text((ox + 24, 18), name, fill=(40, 34, 30), font=F1)
    line, y = "", 56
    for word in words.split():
        if d.textlength(line + word, font=F2) > W - 50:
            d.text((ox + 24, y), line, fill=(70, 62, 56), font=F2); y += 20; line = ""
        line += word + " "
    d.text((ox + 24, y), line, fill=(70, 62, 56), font=F2)
    # ---- the map
    allp = [p for l in legs for p in (l[1], l[2])]
    x0, x1 = min(p[0] for p in allp) - 150, max(p[0] for p in allp) + 14
    y0, y1 = min(p[1] for p in allp) - 10, max(p[1] for p in allp) + 10
    top, bottom = 130, 790
    sc = min((W - 600) / (x1 - x0), (bottom - top) / (y1 - y0))
    cx, cy = ox + (W - 540) / 2 - (x0 + x1) / 2 * sc, (top + bottom) / 2 + (y0 + y1) / 2 * sc

    def P(p):
        return (cx + p[0] * sc, cy - p[1] * sc)

    def draw(leg_list, dashed=False):
        for (kind, a, c) in leg_list:
            col = KIND[kind][0]
            pts = [P(q) for q in bow(a, c, kind)]
            wd = {"chute": 15, "swim": 20, "run": 11, "hops": 8, "swing": 5, "pad": 9, "dive": 9}[kind]
            if kind in ("swing", "hops") or dashed:
                for k in range(0, len(pts) - 1, 2):
                    d.line([pts[k], pts[k + 1]], fill=col, width=wd + (4 if kind == "hops" else 0))
            else:
                d.line(pts, fill=col, width=wd, joint="curve")
            if kind == "chute":
                d.line(pts, fill=(18, 110, 104), width=3)
    draw(legs)
    for k, (kind, a_, c_) in enumerate(legs):
        m = P(((a_[0] + c_[0]) / 2, (a_[1] + c_[1]) / 2))
        d.ellipse((m[0] - 9, m[1] - 9, m[0] + 9, m[1] + 9), fill=(255, 255, 255), outline=(60, 60, 60))
        d.text((m[0] - (4 if k < 9 else 7), m[1] - 7), str(k + 1), fill=(30, 30, 30), font=F3)
    if SHORTCUT and ci == 2:
        draw(SHORTCUT)
        m = P((6, 92, 0)); d.text((m[0] + 8, m[1]), "short, risky", fill=(150, 120, 10), font=F3)
        m = P((-36, 92, 0)); d.text((m[0] - 86, m[1]), "long, safe", fill=(18, 110, 104), font=F3)
    PURPLE = (140, 60, 170)
    ty = 130
    for (cname, asks, after, before, clegs) in CUTS:
        skipped = sum(math.dist(l[1], l[2]) for l in legs[after:before - 1])
        own = sum(math.dist(l[1], l[2]) for l in clegs)
        for (kind, a_, c_) in clegs:
            A, C = P(a_), P(c_)
            for k in range(0, 14, 2):
                d.line([(A[0] + (C[0] - A[0]) * k / 14, A[1] + (C[1] - A[1]) * k / 14), (A[0] + (C[0] - A[0]) * (k + 1) / 14, A[1] + (C[1] - A[1]) * (k + 1) / 14)], fill=PURPLE, width=7)
        m = P(clegs[0][1]); e = P(clegs[-1][2])
        mid = ((m[0] + e[0]) / 2, (m[1] + e[1]) / 2)
        d.text((mid[0] + 10, mid[1] - 8), cname, fill=PURPLE, font=F2)
        d.text((ox + 24, ty), "%s: %s." % (cname, asks), fill=PURPLE, font=F3)
        d.text((ox + 24, ty + 16), "leaves after %d, rejoins before %d: %d m in place of %d m" % (after, before, own, skipped), fill=(70, 62, 56), font=F3)
        ty += 44
    for k in range(len(legs)):                             # what each numbered stage IS
        words = STAGE.get(k + 1, ("", ""))[1]
        d.text((ox + W - 530, 130 + k * 19), "%2d  %s" % (k + 1, words), fill=(40, 34, 30) if words[:3].isupper() else (110, 100, 92), font=F3)
    s, f = P(legs[0][1]), P(legs[-1][2])
    d.ellipse((s[0] - 9, s[1] - 9, s[0] + 9, s[1] + 9), fill=(255, 255, 255), outline=(30, 30, 30), width=3)
    d.text((s[0] + 13, s[1] - 8), "START" + (" / FINISH (laps)" if loop else ""), fill=(30, 30, 30), font=F3)
    if not loop:
        d.rectangle((f[0] - 9, f[1] - 9, f[0] + 9, f[1] + 9), fill=(30, 30, 30))
        d.text((f[0] + 13, f[1] - 8), "FINISH", fill=(30, 30, 30), font=F3)
    # scale bar
    d.line([(ox + 30, 806), (ox + 30 + 50 * sc, 806)], fill=(40, 40, 40), width=3); d.text((ox + 30, 810), "50 m", fill=(40, 40, 40), font=F3)
    # ---- the height profile, start to finish
    dist, prof, total_t = 0.0, [], 0.0
    for (kind, a, c) in legs:
        ln = math.dist(a, c)
        prof.append((dist, a[2], dist + ln, c[2], kind)); dist += ln; total_t += ln / SPEED[kind]
    z0, z1 = min(min(p[1], p[3]) for p in prof) - 3, max(max(p[1], p[3]) for p in prof) + 3
    px0, px1, py0, py1 = ox + 40, ox + W - 30, 880, 1040
    d.text((ox + 24, 850), "height along the course", fill=(70, 62, 56), font=F2)
    for (da, za, dc, zc, kind) in prof:
        A = (px0 + (px1 - px0) * da / dist, py1 - (py1 - py0) * (za - z0) / (z1 - z0))
        C = (px0 + (px1 - px0) * dc / dist, py1 - (py1 - py0) * (zc - z0) / (z1 - z0))
        d.polygon([A, C, (C[0], py1), (A[0], py1)], fill=tuple(int(v * 0.5 + 247 * 0.5) for v in KIND[kind][0]))
        d.line([A, C], fill=KIND[kind][0], width=6)
    d.line([(px0, py1), (px1, py1)], fill=(60, 60, 60), width=2)
    drop = prof[0][1] - min(p[3] for p in prof)
    slide = sum(dc - da for (da, za, dc, zc, kind) in prof if kind == "chute")
    d.text((ox + 24, 1054), "about %d m long; climbs %d m and drops %d m in all; %d m of it sliding; roughly %d s %s (speeds are guesses)" % (dist, sum(max(0, zc - za) for (da, za, dc, zc, kind) in prof), sum(max(0, za - zc) for (da, za, dc, zc, kind) in prof), slide, total_t, "a lap" if loop else "a run"),
           fill=(40, 34, 30), font=F2)
# the key
d = ImageDraw.Draw(sheet)
x = 24
for kind in ("run", "chute", "swing", "swim", "hops", "pad", "dive"):
    d.rectangle((x, 1104, x + 26, 1120), fill=KIND[kind][0]); d.text((x + 32, 1103), KIND[kind][1], fill=(40, 34, 30), font=F2)
    x += 60 + int(d.textlength(KIND[kind][1], font=F2))
OUT.mkdir(parents=True, exist_ok=True)
sheet.save(OUT / ("paete_run_course_maps_v%d.png" % VERSION))
print("wrote", OUT / ("paete_run_course_maps_v%d.png" % VERSION))
