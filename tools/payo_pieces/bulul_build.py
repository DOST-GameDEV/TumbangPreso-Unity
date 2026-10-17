"""PAYO, THE GUARDIANS: the shapes. One function per piece, typed in authoring space (y up, +z front, metres).

  blender -b --python tools/payo_pieces/bulul_build.py -- <piece>

Pieces here: seated (the giant seated bulul, 9.2 m). The family is in bulul_family.py (standing, pair, spout, lizard,
post). Writes Logs/payo/pieces/bulul/work/<name>_maps.npz, .json and _low.blend; then bulul_paint.py and
bulul_review.py (bulul_run.sh does all of it).

WHAT THE REVIEW ROUNDS TAUGHT (keep):
  * v1 was a block stack with a small head hidden behind its own knees and arms when seen from the ground. A giant is
    looked at from BELOW: the head is a third of the figure, set forward and NODDED down at whoever stands before it,
    the knees are apart with the loincloth hanging between them, and the arms sit low.
  * Its warmth is in the face: brows nearly level (ends a little lower than the middle), big dark eyes low in a pale
    shell, round cheeks, a wide shallow smile cut IN. A lower lip block made it pout.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bulul_kit as K                                                    # noqa: E402
from bulul_kit import (ACCENT, BASKET, CLOTH, DARK, INLAY, JAR, LEAF, MOSS, PBLACK, PCREAM, POCHRE, PRED, RICE, STONE, WATER, WOOD,  # noqa: E402,F401
                       bx, carve, ell, hull, lathe, ring, tube)


def mark(key, item):
    K.MARKS.setdefault(key, []).append(item)


def offerings(y, z, s=1.0, x0=0.0):
    """Rice bundles, a jar and a betel basket on a shelf at height y, around depth z. s scales them."""
    # the jar: a tapayan with a neck, a lid and a rope round its shoulder
    lathe([(0.20 * s, 0), (0.36 * s, 0.12 * s), (0.44 * s, 0.42 * s), (0.38 * s, 0.70 * s), (0.22 * s, 0.86 * s), (0.25 * s, 0.96 * s), (0.25 * s, 1.0 * s)],
          (x0 - 1.55 * s, y, z + 0.05 * s), JAR, seg=10)
    lathe([(0.29 * s, 0.0), (0.29 * s, 0.07 * s), (0.10 * s, 0.15 * s), (0.10 * s, 0.24 * s), (0.0, 0.26 * s)], (x0 - 1.55 * s, y + 1.0 * s, z + 0.05 * s), BASKET, seg=10)
    # the betel basket: squat, woven, a domed lid with a knob
    lathe([(0.40 * s, 0), (0.48 * s, 0.10 * s), (0.50 * s, 0.34 * s), (0.44 * s, 0.42 * s)], (x0 + 0.15 * s, y, z + 0.2 * s), BASKET, seg=10)
    lathe([(0.47 * s, 0.0), (0.40 * s, 0.14 * s), (0.16 * s, 0.24 * s), (0.10 * s, 0.34 * s), (0.0, 0.36 * s)], (x0 + 0.15 * s, y + 0.42 * s, z + 0.2 * s), BASKET, seg=10)
    # a small bowl of rice beside it
    lathe([(0.14 * s, 0), (0.30 * s, 0.16 * s), (0.30 * s, 0.2 * s)], (x0 - 0.75 * s, y, z + 0.45 * s), JAR, seg=9)
    ell((x0 - 0.75 * s, y + 0.2 * s, z + 0.45 * s), (0.26 * s, 0.12 * s, 0.26 * s), RICE, seg=8, rings=4)
    # three rice bundles: tied at the waist, flared at both ends, leaning on one another
    for (dx, dz, lean, h) in ((1.25, 0.05, -14, 0.95), (1.75, 0.25, 12, 0.85), (1.45, 0.55, 30, 0.7)):
        h *= s
        lathe([(0.24 * s, 0), (0.20 * s, 0.12 * h), (0.11 * s, 0.42 * h), (0.13 * s, 0.5 * h), (0.34 * s, 0.86 * h), (0.38 * s, h), (0.2 * s, h + 0.08 * s)],
              (x0 + dx * s, y, z + dz * s), RICE, seg=8, turn=(lean * 0.4, dx * 40, lean))
        ring((x0 + dx * s - math.sin(math.radians(lean)) * 0.46 * h, y + 0.46 * h * math.cos(math.radians(lean)), z + dz * s), 0.135 * s, 0.035 * s, CLOTH, axis="y", seg=8, tseg=4)


def vine(pts, r=0.075, leaves=4, side=1.0, big=0.34):
    """A fat vine along authoring points with a few chunky leaves hung off it."""
    tube(pts, r, LEAF, seg=5)
    n = len(pts)
    for i in range(leaves):
        p = pts[1 + (i * (n - 2)) // max(1, leaves - 1)] if n > 2 else pts[-1]
        s = big * (0.8 + 0.35 * ((i * 7) % 3) / 2)
        ell((p[0] + side * 0.12 * (1 if i % 2 else -1), p[1] - 0.16, p[2] + 0.04), (s * 0.62, s, s * 0.16), LEAF, seg=6, rings=4,
            turn=(12 * (i % 3 - 1), 30 * (i % 2) - 15, 24 * (1 if i % 2 else -1)))


def head(c, s, pid=STONE, band=True, smile=0.13, ears=True, brow_tilt=1.0, earring=True, tilt=0.0, female=False, moss=True,
         lite=False, spout=False, studs=True, lid=0.0):
    """The family face. c = the point under the chin, s = scale (1.0: 2.8 m wide, 3 m tall), tilt = a nod forward in
    degrees. A long flat nose from a heavy brow, big dark eyes in a pale shell under a lid, round cheeks, a wide
    shallow smile, big ears with a ring each, a flat crown, a woven band tied round the brow. Marks are left in the
    head's own (un-nodded) space with "head": True; the painter un-nods the texels."""
    n0 = len(K.PARTS)
    sg, rg = (7, 4) if lite else (10, 6)                 # lite: the 1.2 m figures, a fifth of the triangles
    bk = 0.0 if lite else 1.0

    def P(x, y, z):
        return (c[0] + x * s, c[1] + y * s, c[2] + z * s)

    def H(x, y, z):
        return (x * s, y * s, z * s)
    skull = hull(bx(P(0, 0.30, 0.02), H(0.74, 0.30, 0.86), top=1.3, topz=1.1) + bx(P(0, 1.70, -0.1), H(1.36, 0.66, 1.18), top=0.97)
                 + bx(P(0, 2.66, -0.14), H(1.10, 0.16, 0.94), top=0.84), pid, bev=0.16 * s, name="skull")
    cut = [ell(P(sx * 0.64, 1.50, 1.10), H(0.47, 0.31, 0.22), DARK, seg=10, rings=6, keep=False) for sx in (-1, 1)]
    # the smile: a wide shallow curve cut in, its corners turned up under the cheeks. (v2's tube sank away from the
    # face at its ends, so only the middle cut and it read as a flat grimace.)
    xs = (-0.62, -0.46, -0.24, 0, 0.24, 0.46, 0.62)
    mouth = tube([P(x, 0.36 + smile * (abs(x) / 0.62) ** 2 * 1.9, 0.955) for x in xs],
                 [0.06 * s, 0.085 * s, 0.10 * s, 0.105 * s, 0.10 * s, 0.085 * s, 0.06 * s], DARK, seg=6, keep=False)
    if spout:                                            # a round open mouth the water pours from
        mouth = ell(P(0, 0.52, 0.9), H(0.40, 0.36, 0.66), DARK, seg=10, rings=6, keep=False)
    carve(skull, *cut, mouth)
    if spout:
        # round puffed lips, and under them a stone trough the water runs along, its channel cut in
        ring(P(0, 0.52, 0.99), 0.44 * s, 0.11 * s, pid, axis="z", seg=12, tseg=6)
        lip = hull(bx(P(0, 0.12, 1.34), H(0.36, 0.15, 0.56), top=0.9), pid, bev=0.05 * s, name="trough")
        carve(lip, hull(bx(P(0, 0.26, 1.42), H(0.2, 0.13, 0.62)), DARK, bev=0.0, keep=False))
    for sx in (-1, 1):
        ell(P(sx * 0.64, 1.47, 1.00), H(0.40, 0.25, 0.14), INLAY, seg=8 if lite else 12, rings=4 if lite else 6)
        hull(bx(P(sx * 0.64, 1.74 - lid, 1.10), H(0.47, 0.07 + lid * 0.5, 0.14)), pid, bev=0.035 * s * bk, name="lid")
        hull(bx(P(sx * 0.68, 2.02, 1.14), H(0.72, 0.19, 0.25), r=(0, 0, -sx * brow_tilt)), pid, bev=0.075 * s * bk, name="brow")
        ell(P(sx * 0.74, 0.92, 0.90), H(0.42, 0.34, 0.24), pid, seg=sg, rings=rg)                # cheek
        if ears:
            ear = hull(bx(P(sx * 1.50, 1.55, -0.12), H(0.21, 0.74, 0.46), top=0.8, topz=0.8, shift=(0, -0.05 * s)) + bx(P(sx * 1.46, 0.86, -0.1), H(0.2, 0.2, 0.3)), pid, bev=0.08 * s * bk, name="ear")
            if not lite:
                carve(ear, ell(P(sx * 1.74, 1.62, -0.12), H(0.13, 0.46, 0.25), DARK, seg=8, rings=5, keep=False))
            if earring:
                ring(P(sx * 1.52, 0.50, -0.1), 0.27 * s, 0.075 * s, ACCENT, axis="x", seg=6 if lite else 10, tseg=4 if lite else 5)
        # three inlay studs on each cheek
        for (dx, dy) in (((0.98, 1.02), (1.10, 0.80), (0.90, 0.72)) if studs else ()):
            mark("dots", {"c": list(P(sx * dx, dy, 0.9)), "r": 0.065 * s, "col": "turq", "head": True})
    # the nose: long and flat from the brow, widening to a soft block
    hull(bx(P(0, 2.02, 1.18), H(0.2, 0.2, 0.2)) + bx(P(0, 1.02, 1.36), H(0.34, 0.17, 0.27)) + bx(P(0, 0.9, 1.2), H(0.40, 0.1, 0.2)), pid, bev=0.07 * s * bk, name="nose")
    hull(bx(P(0, 2.9, -0.14), H(0.82, 0.1, 0.7), top=0.9), pid, bev=0.06 * s * bk, name="crown")
    if female:                                                                                   # a bun at the back
        ell(P(0, 2.5, -1.15), H(0.6, 0.55, 0.5), pid, seg=sg, rings=rg)
    if band:
        hull(bx(P(0, 2.42, -0.1), H(1.40, 0.15, 1.22)), CLOTH, bev=0.05 * s * bk, name="band")
        ell(P(1.42, 2.4, 0.42), H(0.2, 0.24, 0.26), CLOTH, seg=6 if lite else 8, rings=4 if lite else 5)
        hull(bx(P(1.50, 1.72, 0.62), H(0.07, 0.62, 0.2), r=(8, 0, 4)), CLOTH, bev=0.03 * s * bk, name="tail")
        hull(bx(P(1.52, 1.95, 0.30), H(0.07, 0.44, 0.17), r=(-10, 0, 7)), CLOTH, bev=0.03 * s * bk, name="tail")
        mark("cloths", {"type": "ring", "c": list(P(0, 2.42, -0.1)), "h": 0.15 * s, "r": 1.3 * s, "head": True})
        mark("cloths", {"type": "strip", "c": list(P(1.50, 1.72, 0.62)), "half": list(H(0.2, 0.72, 0.3)), "wide": "z", "along": "y", "fringe": True, "head": True})
        mark("cloths", {"type": "strip", "c": list(P(1.52, 1.95, 0.30)), "half": list(H(0.2, 0.5, 0.22)), "wide": "z", "along": "y", "fringe": True, "head": True})
    if moss:
        ell(P(-0.25, 3.0, -0.2), H(0.86, 0.24, 0.72), MOSS, seg=sg, rings=rg - 1)
        if not lite:
            ell(P(0.52, 2.95, 0.3), H(0.44, 0.2, 0.4), MOSS, seg=8, rings=5)
    made = K.PARTS[n0:]
    if tilt:
        K.nod(made, c, tilt)
    K.MARKS["face"] = {"c": K.nodded(list(P(0, 1.4, 1.1)), c, tilt), "size": 3.0 * s}
    for sx in (-1, 1):
        mark("eyes", list(P(sx * 0.64, 1.47, 1.1)) + [0.40 * s])
    mark("heads", {"pivot": list(c), "tilt": tilt, "r": 2.4 * s, "mid": K.nodded(list(P(0, 1.5, 0)), c, tilt)})


def plinth(w, d, y1, shelf=1.1):
    """The seat block: a wide foot slab with an offering shelf in front, a waist with a fret course, an overhanging cap.
    w, d = half width and half depth of the cap. Top at y1."""
    hull(bx((0, 0.3, shelf * 0.5), (w + 0.4, 0.3, d + 0.35 + shelf * 0.5), top=0.97), STONE, bev=0.11, name="foot")
    hull(bx((0, (0.6 + y1 - 0.5) * 0.5, 0), (w - 0.28, (y1 - 0.5 - 0.6) * 0.5 + 0.05, d - 0.28)), STONE, bev=0.05, name="waist")
    hull(bx((0, y1 - 0.25, 0), (w, 0.25, d), top=0.95), STONE, bev=0.12, name="cap")
    for sx in (-1, 1):                                               # two studs at the back corners of the foot slab
        hull(bx((sx * (w + 0.05), 0.75, -d - 0.0), (0.33, 0.2, 0.33), top=0.8), STONE, bev=0.07, name="stud")
    mark("fret", {"y0": 0.62, "y1": y1 - 0.52, "hw": w - 0.28, "hd": d - 0.28})


def seated():
    plinth(2.7, 2.5, 1.4)
    Y = 1.4
    for sx in (-1, 1):
        hull(bx((sx * 1.25, Y + 0.3, 1.2), (0.48, 0.3, 0.5)) + bx((sx * 1.3, Y + 0.2, 2.0), (0.6, 0.2, 0.38)), STONE, bev=0.1, name="foot")
        mark("grooves", {"c": [sx * 1.3, Y + 0.22, 2.2], "h": [0.6, 0.26, 0.26], "axis": "x", "n": 4})
        hull(bx((sx * 1.25, Y + 0.7, 1.15), (0.42, 0.2, 0.42)) + bx((sx * 1.34, Y + 2.95, 1.5), (0.55, 0.45, 0.55)), STONE, bev=0.14, name="shin")
        ell((sx * 1.34, Y + 3.2, 1.62), (0.62, 0.5, 0.6), STONE, seg=10, rings=6)                      # the knee cap
        # a carved disc on each knee and a painted ring round each ankle: the shins were two bare slabs
        lathe([(0.42, 0), (0.42, 0.07), (0.3, 0.13), (0.0, 0.15)], (sx * 1.34, Y + 2.62, 2.03), STONE, seg=10, turn=(90, 0, 0), name="kneedisc")
        mark("discs", {"c": [sx * 1.34, Y + 2.62, 2.1], "r": 0.42, "axis": "z"})
        hull(bx((sx * 1.26, Y + 0.95, 1.17), (0.5, 0.14, 0.5)), ACCENT, bev=0.05, name="anklet")
        hull(bx((sx * 1.34, Y + 2.9, 1.4), (0.56, 0.5, 0.5)) + bx((sx * 1.12, Y + 0.7, -0.7), (0.68, 0.66, 0.62)), STONE, bev=0.14, name="thigh")
        ell((sx * 1.98, Y + 4.02, -0.5), (0.74, 0.64, 0.76), STONE, seg=10, rings=6)                   # shoulder
        hull(bx((sx * 0.9, Y + 2.9, -1.42), (0.62, 0.7, 0.12), top=0.8), STONE, bev=0.07, name="blade")   # shoulder blade
    hull(bx((0, Y + 0.55, -0.95), (1.72, 0.55, 0.88)), STONE, bev=0.16, name="hips")
    hull(bx((0, Y + 0.8, -0.9), (1.4, 0.5, 0.72)) + bx((0, Y + 2.0, -0.85), (1.22, 0.3, 0.66)) + bx((0, Y + 3.9, -0.55), (1.72, 0.5, 0.84)), STONE, bev=0.16, name="torso")
    # the waist cloth: a band round the hips, its flap lying forward between the feet and hanging over the seat's edge
    hull(bx((0, Y + 1.3, -0.9), (1.5, 0.2, 0.82)), CLOTH, bev=0.05, name="waistband")
    mark("cloths", {"type": "ring", "c": [0, Y + 1.3, -0.9], "h": 0.2, "r": 1.45})
    hull(bx((0, Y + 0.05, 1.3), (0.56, 0.07, 1.3)), CLOTH, bev=0.03, name="flap")
    hull(bx((0, Y - 0.5, 2.6), (0.56, 0.58, 0.07), top=1.08), CLOTH, bev=0.03, name="flap")
    mark("cloths", {"type": "strip", "c": [0, Y + 0.05, 1.3], "half": [0.56, 0.12, 1.3], "wide": "x", "along": "z"})
    mark("cloths", {"type": "strip", "c": [0, Y - 0.5, 2.6], "half": [0.62, 0.6, 0.12], "wide": "x", "along": "y", "fringe": True})
    # arms crossed on the knees. Its +x arm lies under, its -x arm over, so the two sides are not a mirror.
    hull(bx((2.0, Y + 3.85, -0.3), (0.44, 0.44, 0.46)) + bx((2.16, Y + 3.58, 1.35), (0.42, 0.4, 0.42)), STONE, bev=0.13, name="upperarm")
    hull(bx((2.1, Y + 3.6, 1.56), (0.42, 0.36, 0.36)) + bx((-0.95, Y + 3.72, 1.95), (0.4, 0.33, 0.34)), STONE, bev=0.11, name="forearm")
    hull(bx((-1.42, Y + 3.66, 1.98), (0.46, 0.42, 0.4), r=(0, 10, 6)), STONE, bev=0.12, name="hand")
    mark("grooves", {"c": [-1.42, Y + 3.6, 2.1], "h": [0.44, 0.44, 0.34], "axis": "x", "n": 4})
    hull(bx((-2.0, Y + 3.9, -0.3), (0.44, 0.44, 0.46)) + bx((-2.16, Y + 4.12, 1.25), (0.42, 0.4, 0.42)), STONE, bev=0.13, name="upperarm")
    hull(bx((-2.1, Y + 4.18, 1.46), (0.42, 0.34, 0.36)) + bx((0.95, Y + 4.36, 1.76), (0.4, 0.32, 0.34)), STONE, bev=0.11, name="forearm")
    hull(bx((1.42, Y + 4.34, 1.8), (0.46, 0.4, 0.4), r=(0, -8, -6)), STONE, bev=0.12, name="hand")
    mark("grooves", {"c": [1.42, Y + 4.3, 1.92], "h": [0.44, 0.42, 0.34], "axis": "x", "n": 4})
    # carved arm rings with worn paint
    for sx in (-1, 1):
        hull(bx((sx * 2.07, Y + 3.74, 0.5), (0.52, 0.52, 0.16), r=(-9 * (1 if sx > 0 else -1), 0, 0)), ACCENT, bev=0.05, name="armlet")
    # neck and a bead collar
    hull(bx((0, Y + 4.5, -0.2), (0.8, 0.42, 0.7)), STONE, bev=0.1, name="neck")
    hull(bx((0, Y + 4.42, -0.25), (1.12, 0.13, 1.0)), ACCENT, bev=0.06, name="collar")
    head((0, Y + 4.78, 0.3), 1.2, tilt=9)
    # the back: a carved sun disc between the shoulder blades and a spine ridge under it
    lathe([(0.86, 0), (0.86, 0.1), (0.62, 0.2), (0.3, 0.2), (0.26, 0.3), (0.0, 0.32)], (0, Y + 2.95, -1.46), STONE, seg=14, turn=(-93, 0, 0), name="disc")
    hull(bx((0, Y + 1.75, -1.6), (0.2, 0.32, 0.1), top=0.7), STONE, bev=0.05, name="spine")
    mark("discs", {"c": [0, Y + 2.95, -1.6], "r": 0.86, "axis": "z"})
    # moss pads on the shoulders, a knee and the plinth
    for c, r in (((-2.05, Y + 4.6, -0.55), (0.6, 0.22, 0.62)), ((1.95, Y + 4.58, -0.72), (0.46, 0.18, 0.5)), ((-1.36, Y + 3.66, 1.5), (0.34, 0.12, 0.36)),
                 ((2.6, 0.62, -2.3), (0.62, 0.2, 0.6)), ((-2.75, 0.62, 2.9), (0.55, 0.2, 0.5)), ((2.3, Y + 0.02, 1.9), (0.46, 0.15, 0.46)), ((-2.0, Y + 0.02, -1.9), (0.66, 0.17, 0.56))):
        ell(c, r, MOSS, seg=9, rings=5)
    vine([(-2.1, Y + 4.6, -0.9), (-2.5, Y + 4.0, -1.05), (-2.5, Y + 3.0, -1.15), (-2.2, Y + 2.0, -1.5), (-2.0, Y + 1.0, -1.72)], leaves=5, side=-1)
    vine([(0.8, Y + 8.2, -0.6), (1.1, Y + 7.5, -1.1), (0.95, Y + 6.6, -1.25), (1.1, Y + 5.6, -1.15)], leaves=4)
    vine([(2.5, Y + 4.4, -0.3), (2.66, Y + 3.7, 0.3), (2.68, Y + 3.0, 0.6)], leaves=3)
    offerings(0.6, 3.2)
    K.MARKS["moss"] = 1.0
    K.MARKS["courses"] = {"h": 1.02, "ymax": Y + 4.4, "len": 2.3}

    def weight(c, n, pid):
        if n[1] < -0.9 and c[1] < 0.05:
            return 0.04                                              # the underside nobody sees
        if pid == INLAY:
            return 9.0
        if c[1] > 5.4:
            return 2.6 if c[2] > 0.6 else 1.5                         # the head; its face most of all
        if pid in (CLOTH, RICE, JAR, BASKET):
            return 1.6
        return 1.0
    K.finish("bulul_seated", size=2048, budget=8800, weight=weight, height=9.2)


PIECES = {"seated": seated}

if __name__ == "__main__":
    which = [a for a in sys.argv[sys.argv.index("--") + 1:]] if "--" in sys.argv else ["seated"]
    import importlib
    try:
        more = importlib.import_module("bulul_family"); PIECES.update(more.PIECES)
    except ModuleNotFoundError:
        pass
    for w in which:
        K.reset()
        PIECES[w]()
