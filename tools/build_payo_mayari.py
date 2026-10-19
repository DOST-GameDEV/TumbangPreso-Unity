"""MAYARI, THE MOON GODDESS, a real modelled figure for Payo's night easter egg (`Map.PayoMoon`), 2026-10-19.

The owner, of the stand-in (`tools/payo_pieces/mayari_goddess.py`, rounded primitives): "mayari, you should model an
actual godess like the paete ult. then ghost effect her, place her in the sky, and make stars glow across her
sillhouete". So she is built the way Mariang Makiling was (`tools/build_paete_makiling.py`): typed part by part, the
cast's soft oval head with a flat drawn face, a tall slim body that is not chibi. Its shape helpers are imported, never
edited; nothing of Makiling's own design is used. Mayari is her own woman:

  * the MOON is her silhouette. Her hair leaves the back of her head as two great wings that sweep out and down round
    her and flick up at their ends: seen from the court she stands inside a crescent of her own hair. A night wind from
    her right carries her left wing higher and wider, so the two sides are typed separately and are not mirrors;
  * a TERNO's butterfly sleeves stand up off her shoulders (the strongest shapes against the sky after the hair), with
    sheer bell sleeves to the elbow;
  * a baro't saya: a fitted camisa, a folded panuelo with its point down her breast, a long saya flaring to a wide
    scalloped hem that trails back as she floats, a wrapped tapis over it to the knee, a bright sash knotted at her left
    hip with two tails;
  * gold: a circlet with an upturned crescent and a four-point star at her brow, crescent earrings, and behind her head
    a great crescent halo (open below) inside a thin full ring;
  * a serene face: two closed eyes and a small smile laid on the oval as ink, nothing else (the cast's rule);
  * her forearms come forward and her hands cup the moon at `HANDS` from below and the sides.

THE OUTPUT is the KNL1 mesh `PayoMoonMeshes.Goddess` reads (format and the A/B channels: `mayari_goddess.py`'s header;
the writer is that file's `write_knl`). Authored in glTF space: metres, hem y 0, crown about 4.3, +z toward the court.

  PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe (always -I)
  $PY -I tools/build_payo_mayari.py stage            bake to Logs/payo/pieces/moon/mayari/goddess.bytes (review)
  $PY -I tools/build_payo_mayari.py unity            bake over Resources/Map/PayoMoon/goddess.bytes (ONLY after review)
  "$B" -b --python tools/build_payo_mayari.py -- render <ABS_ROOT>    review shots of the staged bake
  $PY -I tools/build_payo_mayari.py sheet vN         Logs/payo/unity/moon/mayari_sheet_vN.png
"""
import math
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
STAGE = os.path.join(ROOT, "Logs", "payo", "pieces", "moon", "mayari")
SHOTS = os.path.join(STAGE, "shots")
UNITY_OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Map", "PayoMoon")
SHEETS = os.path.join(ROOT, "Logs", "payo", "unity", "moon")
HANDS = (0.0, 2.05, 0.82)          # PayoMoon.GoddessHands: the moon floats here, about 0.85 wide
MOON_R = 0.425

try:
    import bpy
    INBL = True
except ImportError:
    INBL = False

if not INBL:
    import numpy as np
    sys.path.insert(0, HERE)
    sys.path.insert(0, os.path.join(HERE, "payo_pieces"))
    from hand_companion_kit import ellipsoid, join, lathe, moved, tube  # noqa: E402
    from build_paete_bloom import smooth01  # noqa: E402
    from build_paete_makiling import _front_of, _oval_rows, _row_at, aimed, catmull, ribbon, smooth_normals, soft_box  # noqa: E402
    from mayari_goddess import write_knl  # noqa: E402

    SKIN, HAIR, INK, GOLD, SAYA, BRIGHT = 0, 1, 2, 3, 4, 5
    VALUE = {SKIN: .78, HAIR: .38, INK: .05, GOLD: 1.0, SAYA: .62, BRIGHT: .95}

    # ------------------------------------------------------------------ her head: the cast's oval, 0.80 m tall
    # v1 sat her waist at 2.0 m, so the moon (fixed at HANDS, y 2.05) was held at her WAIST like a parcel. Her body
    # is set lower and her head larger, so the moon is held at her breast and her head reads at sky distance.
    H0 = 2.64                        # where her head meets her neck
    NECK = 0.09
    _oval = _oval_rows(0.80, 0.330, 0.310, 0.330)
    # Her neck is part of her head (Makiling's lesson: a separate neck tube shows as a lit rod inside a ghost's head).
    HEAD_ROWS = [(-0.24, NECK, NECK, NECK, 2.0), (-0.10, NECK, NECK, NECK, 2.0), (0.0, NECK, NECK, NECK, 2.0), (0.01, NECK, NECK, NECK, 2.0)] + \
                [r for r in _oval if r[0] > 0.01 and r[1] > NECK * 1.25]
    # Her hair over her skull: larger, set back so her face stands through, drawn back below her cheek to frame it.
    HAIR_ROWS = [(y * 1.04 + 0.05, w + 0.028, f - 0.015 - 0.11 * float(smooth01(0.30, 0.09, y)), b + 0.05, e)
                 for y, w, f, b, e in HEAD_ROWS if y >= 0.05]

    def face_z(x, y):
        return _front_of(HEAD_ROWS, x, y - H0)

    def brow_z(x, y):
        return max(face_z(x, y), _front_of(HAIR_ROWS, x, y - H0))

    def cols_normals(mesh, cols):
        return (mesh[0], smooth_normals(mesh[0], mesh[2], cols), mesh[2])

    def ink_line(points, radii, lift=0.006):
        """A stroke laid on her face: `points` are (x, y) in metres, y above her neck."""
        pts, rad = catmull([(x, H0 + y, 0.0) for x, y in points], radii, 6)
        line = [(float(p[0]), float(p[1]), face_z(float(p[0]), float(p[1])) + lift) for p in pts]
        return tube(line, [max(0.006, float(r)) for r in rad], seg=8)

    def along(mesh, root):
        """Sway weight for a lock or a tail: 0 at its root, 1 at its far end."""
        d = np.linalg.norm(mesh[0] - np.array(root, np.float32), axis=1)
        return np.clip(d / max(float(d.max()), 1e-6), 0, 1)

    def hem_table(table, angle):
        """A typed table of (angle, value) round her, read at `angle` degrees with a smooth step between rows."""
        a = angle % 360.0
        for (a0, v0), (a1, v1) in zip(table, table[1:] + [(table[0][0] + 360.0, table[0][1])]):
            if a0 <= a <= a1:
                k = (a - a0) / (a1 - a0)
                k = k * k * (3 - 2 * k)
                return v0 + (v1 - v0) * k
        return table[0][1]

    # ------------------------------------------------------------------ her, part by part
    def parts():
        """(name, part id, mesh, sway pivot or None, sway weight (number or per vertex), value or None)."""
        out = []

        # ================= THE SAYA: from her waist to a wide hem, scalloped, trailing back as she floats.
        WAIST = 1.66
        prof = [(1.12, 0.0), (1.06, 0.21), (0.94, 0.50), (0.80, 0.83), (0.64, 1.16), (0.48, 1.45), (0.36, 1.66), (0.33, 1.72)]
        fine = []
        for (r0, y0), (r1, y1) in zip(prof, prof[1:]):
            for k in range(5):
                t = k / 5
                fine.append((r0 + (r1 - r0) * t, y0 + (y1 - y0) * t))
        fine.append(prof[-1])
        seg = 64
        saya = lathe(fine, seg=seg, squash_z=0.86)
        p = saya[0].copy()
        # Her hem, typed round her (angle 0 is her front, 90 her left): how far each scallop stands out, how high it lifts.
        HEM_OUT = [(0, 1.00), (28, 1.05), (55, 0.98), (84, 1.07), (112, 1.01), (146, 1.09), (180, 1.12), (214, 1.08), (243, 1.00),
                   (272, 1.06), (304, 0.97), (333, 1.04)]
        HEM_LIFT = [(0, 0.00), (28, 0.07), (55, 0.01), (84, 0.09), (112, 0.03), (146, 0.11), (180, 0.02), (214, 0.10), (243, 0.02),
                    (272, 0.08), (304, 0.00), (333, 0.06)]
        for i in range(len(p)):
            x, y, z = (float(v) for v in p[i])
            low = max(0.0, 1.0 - y / 1.1)
            ang = math.degrees(math.atan2(x, z / 0.86 if abs(z) > 1e-9 else 1e-9))
            s = 1.0 + (hem_table(HEM_OUT, ang) - 1.0) * low ** 1.6
            p[i, 0] = x * s
            p[i, 2] = z * s - 0.36 * low ** 2                    # she floats: the hem trails back
            p[i, 1] = y + hem_table(HEM_LIFT, ang) * low ** 2.5
        saya = cols_normals((p, saya[1], saya[2]), seg)
        out.append(("saya", SAYA, saya, (0, WAIST, 0), np.clip(1 - p[:, 1] / WAIST, 0, 1) ** 1.5, None))

        # ================= THE TAPIS: a wrapped overskirt to the knee, a shade deeper than the saya, its hem dipping at
        # her front left where the wrap overlaps.
        tprof = [(0.375, 1.69), (0.44, 1.51), (0.60, 1.26), (0.75, 1.03), (0.865, 0.84), (0.885, 0.78)]
        tf = []
        for (r0, y0), (r1, y1) in zip(tprof, tprof[1:]):
            for k in range(4):
                t = k / 4
                tf.append((r0 + (r1 - r0) * t, y0 + (y1 - y0) * t))
        tf.append(tprof[-1])
        tapis = lathe(tf, seg=seg, squash_z=0.86)
        q = tapis[0].copy()
        TAPIS_DIP = [(0, 0.05), (30, 0.13), (55, 0.04), (110, 0.0), (200, 0.02), (290, 0.0), (330, 0.02)]
        for i in range(len(q)):
            x, y, z = (float(v) for v in q[i])
            low = max(0.0, 1.0 - (y - 0.78) / 0.26)
            ang = math.degrees(math.atan2(x, z / 0.86 if abs(z) > 1e-9 else 1e-9))
            q[i, 1] = y - hem_table(TAPIS_DIP, ang) * low
            q[i, 2] = z - 0.36 * max(0.0, 1.0 - q[i, 1] / 1.1) ** 2
        tapis = cols_normals((q, tapis[1], tapis[2]), seg)
        out.append(("tapis", SAYA, tapis, (0, WAIST, 0), np.clip(1 - q[:, 1] / WAIST, 0, 1) ** 1.5 * 0.6, 0.57))

        # ================= THE SASH at her waist, knotted at her left hip, two tails hanging over the tapis.
        def on_waist(angle, y, out_=0.0):
            a = math.radians(angle)
            return ((0.37 + out_) * math.sin(a), y, (0.37 + out_) * math.cos(a) * 0.86)
        band = tube([on_waist(a, WAIST) for a in range(0, 361, 8)], 0.05, seg=10)
        knot_at = on_waist(34, WAIST - 0.01, 0.05)
        knot = ellipsoid((0.085, 0.07, 0.07), at=knot_at, seg=16, rings=10)
        out.append(("sash", BRIGHT, join(band, knot), None, 0.0, None))
        k0 = knot_at
        tail_a = [k0, (k0[0] + 0.05, k0[1] - 0.26, k0[2] + 0.06), (k0[0] + 0.09, k0[1] - 0.54, k0[2] + 0.07), (k0[0] + 0.07, k0[1] - 0.80, k0[2] + 0.05)]
        tail_b = [k0, (k0[0] + 0.10, k0[1] - 0.20, k0[2] + 0.02), (k0[0] + 0.17, k0[1] - 0.40, k0[2] + 0.00), (k0[0] + 0.21, k0[1] - 0.57, k0[2] - 0.03)]
        ta = ribbon(tail_a, [0.10, 0.13, 0.12, 0.03], thick=0.30, face=(0.45, 0, 0.9), seg=10, per=4)
        tb_ = ribbon(tail_b, [0.08, 0.10, 0.09, 0.02], thick=0.30, face=(0.6, 0, 0.8), seg=10, per=4)
        out.append(("sash_tail_long", BRIGHT, ta, k0, along(ta, k0) * 0.7, None))
        out.append(("sash_tail_short", BRIGHT, tb_, k0, along(tb_, k0) * 0.7, None))

        # ================= THE CAMISA: fitted from her waist to her neck, a small waist, a soft bust.
        bod = lathe([(0.29, 1.64), (0.28, 1.78), (0.32, 1.96), (0.35, 2.14), (0.36, 2.28), (0.34, 2.40), (0.24, 2.52), (0.13, 2.62), (0.0, 2.66)],
                    seg=36, squash_z=0.72)
        out.append(("camisa", SKIN, bod, None, 0.0, None))

        # ================= THE PANUELO: a folded kerchief over her shoulders, its point down her breast.
        pan = lathe([(0.425, 2.30), (0.415, 2.36), (0.35, 2.47), (0.22, 2.57), (0.125, 2.645)], seg=36, squash_z=0.78)
        point = ribbon([(0.0, 2.53, 0.20), (0.0, 2.38, 0.32), (0.0, 2.20, 0.30), (0.0, 2.08, 0.275)], [0.42, 0.32, 0.15, 0.012],
                       thick=0.12, face=(0, 0, 1), cup=-0.12, seg=10, per=4)
        out.append(("panuelo", BRIGHT, join(pan, point), None, 0.0, None))

        # ================= THE TERNO SLEEVES: a stiff butterfly arch standing up off each shoulder. Each typed.
        # v1's were thin arches and read as rabbit ears; a terno sleeve is a stiff PUFF with a raised peak.
        left_wing = join(moved(ellipsoid((0.21, 0.15, 0.17), seg=24, rings=14), turn=(0, 0, 24), at=(0.44, 2.50, -0.01)),
                         moved(ellipsoid((0.12, 0.13, 0.12), seg=18, rings=10), turn=(0, 0, 30), at=(0.50, 2.62, -0.02)))
        right_wing = join(moved(ellipsoid((0.20, 0.15, 0.17), seg=24, rings=14), turn=(0, 0, -22), at=(-0.43, 2.50, -0.01)),
                          moved(ellipsoid((0.12, 0.125, 0.12), seg=18, rings=10), turn=(0, 0, -28), at=(-0.49, 2.61, -0.02)))
        out.append(("terno_left", SAYA, left_wing, (0.30, 2.46, 0.0), 0.04, 0.70))
        out.append(("terno_right", SAYA, right_wing, (-0.30, 2.46, 0.0), 0.04, 0.70))

        # ================= HER ARMS: down to the elbow in a sheer bell sleeve, the forearm forward, the hands cupping
        # the moon from below and the sides. Each side typed.
        bell = [(0.115, 0.0), (0.125, 0.16), (0.16, 0.32), (0.21, 0.45), (0.235, 0.49), (0.20, 0.48), (0.0, 0.43)]
        for side, sh, el, wr, palm, fingers, thumb, roll in (
                (1, (0.34, 2.44, 0.0), (0.50, 2.02, 0.16), (0.43, 1.80, 0.60), (0.30, 1.70, 0.84), (0.455, 1.88, 0.84), (0.27, 1.77, 1.04), 35),
                (-1, (-0.34, 2.44, 0.0), (-0.51, 2.03, 0.14), (-0.44, 1.81, 0.59), (-0.31, 1.71, 0.83), (-0.46, 1.90, 0.82), (-0.28, 1.78, 1.03), -33)):
            sleeve = aimed(lathe(bell, seg=24), sh, el)
            fore = tube([el, ((el[0] + wr[0]) * .5, (el[1] + wr[1]) * .5 - 0.02, (el[2] + wr[2]) * .5), wr, palm], [0.078, 0.070, 0.062, 0.056], seg=12)
            hand = moved(ellipsoid((0.10, 0.055, 0.15), seg=16, rings=10), turn=(-8, side * -18, roll), at=palm)
            curl = moved(ellipsoid((0.058, 0.13, 0.11), seg=14, rings=10), turn=(0, 0, side * 18), at=fingers)
            th = moved(ellipsoid((0.038, 0.075, 0.038), seg=10, rings=8), turn=(20, 0, side * -30), at=thumb)
            out.append(("sleeve_%+d" % side, SAYA, sleeve, sh, 0.10, None))
            out.append(("arm_%+d" % side, SKIN, join(fore, hand, curl, th), None, 0.0, None))

        # ================= HER HEAD and neck, one surface.
        skull = soft_box(HEAD_ROWS, n=44)
        out.append(("head", SKIN, moved(skull, at=(0.0, H0, 0.0)), None, 0.0, None))
        cap = soft_box(HAIR_ROWS, n=44)
        out.append(("hair_cap", HAIR, moved(cap, at=(0.0, H0, 0.0)), None, 0.0, None))

        # ================= HER FACE: two closed eyes curving down like smiles, a lash flick at each outer corner, a
        # small smile. Ink only (the cast's rule).
        for side, cx in ((1, 0.133), (-1, -0.133)):
            out.append(("eye_%+d" % side, INK, ink_line([(cx - 0.060, 0.324), (cx - 0.031, 0.297), (cx, 0.287), (cx + 0.031, 0.297), (cx + 0.060, 0.324)],
                                                        [0.011, 0.024, 0.029, 0.024, 0.011]), None, 0.0, None))
            out.append(("lash_%+d" % side, INK, ink_line([(cx + 0.056 * side, 0.318), (cx + 0.095 * side, 0.307)], [0.018, 0.006]), None, 0.0, None))
        out.append(("mouth", INK, ink_line([(-0.047, 0.213), (-0.020, 0.191), (0.020, 0.191), (0.047, 0.213)], [0.007, 0.017, 0.017, 0.007]), None, 0.0, None))

        # ================= HER FRINGE: parted in the middle and swept to her temples, clear of her eyes. Each typed.
        top = H0 + 0.78
        for name, x0, x1, low, wide in (("fringe_l1", 0.039, 0.167, 0.52, 0.17), ("fringe_l2", 0.122, 0.283, 0.40, 0.18), ("fringe_l3", 0.222, 0.339, 0.245, 0.13),
                                         ("fringe_r1", -0.044, -0.178, 0.50, 0.17), ("fringe_r2", -0.133, -0.291, 0.38, 0.18), ("fringe_r3", -0.228, -0.333, 0.265, 0.12)):
            line = []
            for k in range(5):
                f = k / 4.0
                x = x0 + (x1 - x0) * (1 - (1 - f) ** 2)
                y = top + (H0 + low - top) * f
                line.append((x, y, brow_z(x, y) + 0.010 + 0.022 * math.sin(f * math.pi)))
            fr = ribbon(line, [wide * .8, wide, wide * .95, wide * .7, wide * .2], thick=0.40, face=(0, 0, 1), cup=-0.10, seg=10, per=4)
            out.append((name, HAIR, fr, line[0], along(fr, line[0]) * 0.15, None))

        # ================= TWO LONG FRONT LOCKS from her temples down over her panuelo; her right one a little shorter.
        fl = [(0.28, 3.06, 0.12), (0.34, 2.84, 0.18), (0.355, 2.64, 0.24), (0.34, 2.46, 0.33), (0.31, 2.24, 0.35), (0.28, 2.06, 0.34)]
        fr_ = [(-0.29, 3.05, 0.12), (-0.35, 2.82, 0.17), (-0.37, 2.60, 0.24), (-0.35, 2.42, 0.32), (-0.32, 2.22, 0.33)]
        a = ribbon(fl, [0.07, 0.15, 0.17, 0.15, 0.11, 0.015], thick=0.40, face=(0.3, 0, 1), seg=12, per=4)
        b = ribbon(fr_, [0.07, 0.15, 0.16, 0.13, 0.015], thick=0.40, face=(-0.3, 0, 1), seg=12, per=4)
        out.append(("front_lock_left", HAIR, a, fl[0], along(a, fl[0]) * 0.6, None))
        out.append(("front_lock_right", HAIR, b, fr_[0], along(b, fr_[0]) * 0.6, None))

        # ================= HER HAIR'S CRESCENT: two broad wings from the back of her head, lifted out by the night wind,
        # sweeping down round her and curling IN at their ends like a crescent's horns, with one loose strand flicking out
        # from each. v1 had three locks a side hanging straight down (tentacles, a curtain); a wing is ONE broad mass now.
        # The wind from her right lifts her LEFT wing (+x) higher and wider. Every lock typed: (name, line, widths).
        LOCKS = [
            ("wing_left", [(0.06, 3.22, -0.26), (0.42, 3.10, -0.46), (0.86, 2.86, -0.56), (1.24, 2.44, -0.62), (1.46, 1.92, -0.63), (1.48, 1.40, -0.60),
                           (1.32, 1.00, -0.54), (1.08, 0.78, -0.49), (0.84, 0.76, -0.46)],
             [0.46, 0.52, 0.60, 0.66, 0.58, 0.50, 0.40, 0.20, 0.02]),
            ("strand_left", [(0.20, 3.30, -0.26), (0.62, 3.26, -0.42), (1.10, 3.06, -0.50), (1.50, 2.66, -0.55), (1.74, 2.12, -0.55), (1.80, 1.62, -0.50),
                             (1.94, 1.34, -0.46)],
             [0.08, 0.15, 0.18, 0.17, 0.14, 0.08, 0.01]),
            ("wing_right", [(-0.06, 3.20, -0.26), (-0.38, 3.04, -0.46), (-0.78, 2.78, -0.56), (-1.10, 2.32, -0.62), (-1.26, 1.76, -0.63), (-1.24, 1.24, -0.60),
                            (-1.08, 0.90, -0.54), (-0.86, 0.72, -0.49), (-0.66, 0.74, -0.46)],
             [0.46, 0.50, 0.58, 0.62, 0.54, 0.46, 0.36, 0.18, 0.02]),
            ("strand_right", [(-0.20, 3.28, -0.26), (-0.56, 3.20, -0.42), (-0.96, 2.96, -0.50), (-1.32, 2.56, -0.55), (-1.52, 2.04, -0.54), (-1.56, 1.62, -0.50),
                              (-1.66, 1.42, -0.47)],
             [0.08, 0.14, 0.17, 0.16, 0.12, 0.07, 0.01]),
            ("back_fall", [(0.0, 3.16, -0.30), (0.02, 2.70, -0.52), (0.0, 2.15, -0.66), (-0.03, 1.60, -0.78), (0.03, 1.20, -0.86)],
             [0.56, 0.70, 0.68, 0.54, 0.03]),
        ]
        for name, line, wide in LOCKS:
            m = ribbon(line, wide, thick=0.24, face=(0, 0, 1), cup=0.18, seg=12, per=5)
            out.append((name, HAIR, m, line[0], along(m, line[0]), None))

        # ================= GOLD
        # The circlet round her hair at her brow, riding outside the fringe at the front.
        def circlet(angle, out_=0.0):
            a = math.radians(angle)
            c, q = math.sin(a), math.cos(a)
            lift = 0.59 + 0.07 * (1 - q) * .5
            _, half, front, back, e = _row_at(HAIR_ROWS, lift)
            out_ += 0.06 * max(0.0, q) ** 0.5
            x = math.copysign(abs(c) ** (2.0 / e), c) * (half + 0.012 + out_)
            z = math.copysign(abs(q) ** (2.0 / e), q) * ((front if q >= 0 else back) + 0.012 + out_)
            return (x, H0 + lift, z)
        ring = tube([circlet(a) for a in range(0, 361, 6)], 0.02, seg=8)
        cf = circlet(0)
        cres = []
        for k in range(15):                                     # the upturned crescent at her brow, horns up
            ang = math.radians(200 + 140 * k / 14)
            cres.append((0.115 * math.cos(ang), cf[1] + 0.14 + 0.115 * math.sin(ang), cf[2] + 0.02))
        crescent = tube(cres, [0.011 + 0.033 * math.sin(math.pi * k / 14) for k in range(15)], seg=8)
        star_at = (0.0, cf[1] + 0.175, cf[2] + 0.03)
        star_v = ribbon([(star_at[0], star_at[1] - 0.082, star_at[2]), star_at, (star_at[0], star_at[1] + 0.082, star_at[2])], [0.004, 0.05, 0.004],
                        thick=0.35, face=(0, 0, 1), seg=8, per=3)
        star_h = ribbon([(star_at[0] - 0.06, star_at[1], star_at[2] + 0.004), (star_at[0], star_at[1], star_at[2] + 0.004),
                         (star_at[0] + 0.06, star_at[1], star_at[2] + 0.004)], [0.004, 0.044, 0.004], thick=0.35, face=(0, 0, 1), seg=8, per=3)
        # Crescent earrings, below her hair at each jaw.
        ears = []
        for sx in (1, -1):
            ex = sx * 0.335
            ears.append(ellipsoid((0.02, 0.02, 0.02), at=(ex, H0 + 0.165, 0.03)))
            ears.append(tube([(ex + sx * 0.05 * math.cos(math.radians(a)), H0 + 0.095 + 0.05 * math.sin(math.radians(a)), 0.03)
                              for a in range(110, 431, 40)][:7], [0.007, 0.013, 0.018, 0.02, 0.018, 0.013, 0.007], seg=8))
        # The great crescent halo behind her head, open below, its horns behind her shoulders; and a thin full ring.
        HC = H0 + 0.52
        halo = []
        for k in range(41):
            ang = math.radians(-28 + 236 * k / 40)
            halo.append((0.85 * math.cos(ang), HC + 0.85 * math.sin(ang), -0.68))
        halo_m = tube(halo, [0.010 + 0.08 * math.sin(math.pi * k / 40) ** 1.2 for k in range(41)], seg=10)
        full = tube([(0.64 * math.cos(math.radians(a)), HC + 0.64 * math.sin(math.radians(a)), -0.66) for a in range(0, 361, 6)], 0.015, seg=8)
        out.append(("gold", GOLD, join(ring, crescent, star_v, star_h, *ears, halo_m, full), None, 0.0, None))
        return out

    def bake(out_dir):
        os.makedirs(out_dir, exist_ok=True)
        P, N, UV, A, Bv, T = [], [], [], [], [], []
        base = 0
        hands = np.array(HANDS, np.float32)
        everything = parts()
        far = max(float(np.linalg.norm(m[0] - hands, axis=1).max()) for _, _, m, _, _, _ in everything)
        top = max(float(m[0][:, 1].max()) for _, _, m, _, _, _ in everything)
        crown = 4.3
        for name, part, mesh, pivot, sway, value in everything:
            p, n, t = mesh
            k = len(p)
            order = np.linalg.norm(p - hands, axis=1) / far
            sw = np.full(k, float(sway), np.float32) if np.isscalar(sway) else np.asarray(sway, np.float32)
            v = VALUE[part] if value is None else value
            a = np.stack([np.full(k, float(part)), np.full(k, v), sw, order], 1)
            pv = np.array(pivot if pivot is not None else (0, 0, 0), np.float32)
            b = np.concatenate([np.tile(pv, (k, 1)), np.zeros((k, 1), np.float32)], 1)
            uv = np.stack([np.clip(p[:, 1] / crown, 0, 1), np.zeros(k)], 1)
            P.append(p); N.append(n); UV.append(uv); A.append(a); Bv.append(b); T.append(np.asarray(t, np.int64) + base)
            base += k
        Pc = np.concatenate(P)
        lo, hi = Pc.min(0), Pc.max(0)
        print("MAYARI bounds x %.2f..%.2f  y %.2f..%.2f  z %.2f..%.2f  (top %.2f)" % (lo[0], hi[0], lo[1], hi[1], lo[2], hi[2], top))
        inside = int((np.linalg.norm(Pc - hands, axis=1) < MOON_R - 0.01).sum())
        print("vertices inside the moon's sphere:", inside)
        tris = write_knl(os.path.join(out_dir, "goddess.bytes"), Pc, np.concatenate(N), np.concatenate(UV), np.concatenate(A),
                         np.concatenate(Bv), np.concatenate(T))
        print("MAYARI", tris, "triangles,", len(everything), "pieces")

    def sheet(version):
        from PIL import Image, ImageDraw
        names = [("solid_front", "front"), ("solid_quarter", "three-quarter"), ("solid_side", "side"), ("solid_back", "back"),
                 ("silhouette", "silhouette"), ("ghost", "ghost (approx. of the game shader)"), ("closeup_hands", "hands + 0.85 sphere"),
                 ("closeup_face", "face")]
        tiles = [(label, os.path.join(SHOTS, n + ".png")) for n, label in names if os.path.exists(os.path.join(SHOTS, n + ".png"))]
        ims = [Image.open(p).convert("RGB") for _, p in tiles]
        w, h = ims[0].size
        cols = 4
        rows = (len(ims) + cols - 1) // cols
        out = Image.new("RGB", (w * cols, (h + 24) * rows), (12, 10, 24))
        d = ImageDraw.Draw(out)
        for k, (im, (label, _)) in enumerate(zip(ims, tiles)):
            x, y = (k % cols) * w, (k // cols) * (h + 24)
            out.paste(im, (x, y + 24)); d.text((x + 8, y + 6), "MAYARI %s  %s" % (version, label), fill=(255, 255, 255))
        os.makedirs(SHEETS, exist_ok=True)
        path = os.path.join(SHEETS, "mayari_sheet_%s.png" % version)
        out.save(path); print("wrote", path)

    if __name__ == "__main__":
        cmd = sys.argv[1] if len(sys.argv) > 1 else "stage"
        if cmd == "stage":
            bake(STAGE)
        elif cmd == "unity":
            bake(UNITY_OUT)
        elif cmd == "sheet":
            sheet(sys.argv[2] if len(sys.argv) > 2 else "v1")

else:
    import numpy as np
    from mathutils import Vector

    def read_knl(path):
        data = open(path, "rb").read()
        v, i = struct.unpack("<ii", data[4:12])
        at = 12

        def take(n):
            nonlocal at
            arr = np.frombuffer(data, np.float32, n, at); at += n * 4
            return arr
        P = take(v * 3).reshape(-1, 3); N = take(v * 3).reshape(-1, 3); UV = take(v * 2).reshape(-1, 2)
        A = take(v * 4).reshape(-1, 4); Bv = take(v * 4).reshape(-1, 4)
        T = np.frombuffer(data, np.int32, i, at).reshape(-1, 3)
        return P, N, UV, A, Bv, T

    def tb(p):
        """Unity space to Blender's (z up)."""
        return (float(p[0]), float(p[2]), float(p[1]))

    def render():
        argv = sys.argv[sys.argv.index("--") + 1:]
        root = argv[1] if len(argv) > 1 else ROOT
        stage = os.path.join(root, "Logs", "payo", "pieces", "moon", "mayari")
        shots = os.path.join(stage, "shots")
        os.makedirs(shots, exist_ok=True)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        sc = bpy.context.scene
        engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
        sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
        sc.view_settings.view_transform = "Standard"
        sc.render.film_transparent = False
        w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
        bg = w.node_tree.nodes["Background"]
        bg.inputs[0].default_value = (0.20, 0.19, 0.27, 1); bg.inputs[1].default_value = 1.0
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
        sun.data.energy = 3.2
        sun.rotation_euler = Vector((.5, .9, -.6)).normalized().to_track_quat("-Z", "Y").to_euler()
        fill = bpy.data.objects.new("fill", bpy.data.lights.new("fill", "SUN")); sc.collection.objects.link(fill)
        fill.data.energy = 1.0
        fill.rotation_euler = Vector((-.6, -.5, -.3)).normalized().to_track_quat("-Z", "Y").to_euler()
        cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam); sc.camera = cam

        def aim(eye, look, fov):
            cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
            cam.location = Vector(tb(eye))
            cam.rotation_euler = (Vector(tb(look)) - Vector(tb(eye))).to_track_quat("-Z", "Y").to_euler()

        def shoot(name, res_xy=(700, 900)):
            sc.render.resolution_x, sc.render.resolution_y = res_xy
            sc.render.filepath = os.path.join(shots, name + ".png")
            bpy.ops.render.render(write_still=True)
            print("SHOT", name)

        P, N, UV, A, Bv, T = read_knl(os.path.join(stage, "goddess.bytes"))
        part = A[T[:, 0], 0].astype(int)
        value = A[T[:, 0], 1]
        hue = (0.80, 0.76, 1.0)
        gold = (1.0, 0.80, 0.38)

        def mat(name, rgb, emit=0.0):
            m = bpy.data.materials.new(name); m.use_nodes = True
            b = m.node_tree.nodes["Principled BSDF"]
            b.inputs["Base Color"].default_value = (*rgb, 1); b.inputs["Roughness"].default_value = .55
            if emit:
                b.inputs["Emission Color"].default_value = (*rgb, 1); b.inputs["Emission Strength"].default_value = emit
            m.use_backface_culling = True
            return m
        # One material per distinct value, in her one hue (gold its own), so every cloth layer reads by brightness.
        keys = sorted(set((int(p_), round(float(v_), 2)) for p_, v_ in zip(part, value)))
        mats, index = [], {}
        for p_, v_ in keys:
            rgb = gold if p_ == 3 else tuple(min(1.0, hue[i] * (0.12 + 0.88 * v_)) for i in range(3))
            index[(p_, v_)] = len(mats)
            mats.append(mat("m_%d_%.2f" % (p_, v_), rgb))
        me = bpy.data.meshes.new("mayari")
        me.from_pydata([tb(p) for p in P], [], [(int(t[0]), int(t[2]), int(t[1])) for t in T])
        for m in mats:
            me.materials.append(m)
        fidx = [index[(int(p_), round(float(v_), 2))] for p_, v_ in zip(part, value)]
        for poly, k in zip(me.polygons, fidx):
            poly.material_index = k
            poly.use_smooth = True
        ob = bpy.data.objects.new("mayari", me); sc.collection.objects.link(ob)

        look = (0.15, 2.15, 0)
        for n, eye in {"solid_front": (0, 2.3, 11.0), "solid_quarter": (7.4, 2.8, 7.8), "solid_side": (11.0, 2.3, 0.3), "solid_back": (-1.6, 2.8, -11.0)}.items():
            aim(eye, look, 28); shoot(n)

        # A moon stand-in: a plain 0.85 sphere at the hands point, for the close-ups.
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.425, segments=48, ring_count=24, location=tb((-HANDS[0], HANDS[1], HANDS[2])))
        moon = bpy.context.active_object
        moon.data.materials.append(mat("moon", (1.0, 0.86, 0.35), emit=0.6))
        for poly in moon.data.polygons:
            poly.use_smooth = True
        aim((1.6, 2.6, 4.6), (0.0, 2.45, 0.3), 30); shoot("closeup_hands")
        aim((0.5, 3.10, 2.7), (0.0, 3.02, 0.0), 22); shoot("closeup_face")
        moon.hide_render = True

        # SILHOUETTE: flat, against the night, for the outline only.
        bg.inputs[0].default_value = (0.02, 0.025, 0.07, 1)
        flat = bpy.data.materials.new("flat"); flat.use_nodes = True
        nt = flat.node_tree; nt.nodes.clear()
        o = nt.nodes.new("ShaderNodeOutputMaterial"); e = nt.nodes.new("ShaderNodeEmission")
        e.inputs["Color"].default_value = (0.78, 0.74, 0.95, 1); e.inputs["Strength"].default_value = 1.0
        nt.links.new(e.outputs[0], o.inputs["Surface"])
        me.materials.clear(); me.materials.append(flat)
        for poly in me.polygons:
            poly.material_index = 0
        aim((0, 2.3, 11.0), look, 28); shoot("silhouette")

        # GHOST: see-through in the middle, lit at the edges (an approximation of PayoMayariGhost.shader).
        g = bpy.data.materials.new("ghost"); g.use_nodes = True
        nt = g.node_tree; nt.nodes.clear()
        o = nt.nodes.new("ShaderNodeOutputMaterial")
        lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs["Blend"].default_value = .5
        em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Color"].default_value = (0.92, 0.88, 1.0, 1)
        mul = nt.nodes.new("ShaderNodeMath"); mul.operation = "MULTIPLY"; mul.inputs[1].default_value = 2.6
        nt.links.new(lw.outputs["Facing"], mul.inputs[0]); nt.links.new(mul.outputs[0], em.inputs["Strength"])
        em2 = nt.nodes.new("ShaderNodeEmission"); em2.inputs["Color"].default_value = (0.5, 0.46, 0.85, 1); em2.inputs["Strength"].default_value = .3
        add = nt.nodes.new("ShaderNodeAddShader"); tr = nt.nodes.new("ShaderNodeBsdfTransparent"); mix = nt.nodes.new("ShaderNodeMixShader")
        nt.links.new(em.outputs[0], add.inputs[0]); nt.links.new(em2.outputs[0], add.inputs[1])
        mix.inputs["Fac"].default_value = .5
        nt.links.new(tr.outputs[0], mix.inputs[1]); nt.links.new(add.outputs[0], mix.inputs[2])
        nt.links.new(mix.outputs[0], o.inputs["Surface"])
        try:
            g.surface_render_method = "BLENDED"
        except Exception:
            g.blend_method = "BLEND"
        g.use_backface_culling = True
        me.materials.clear(); me.materials.append(g)
        aim((0, 2.3, 11.0), look, 28); shoot("ghost")
        print("RENDERED", shots)

    render()
