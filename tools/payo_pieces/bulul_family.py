"""PAYO, THE GUARDIANS: the giant seated bulul's family. Shapes only; see bulul_build.py and bulul_kit.py.

  blender -b --python tools/payo_pieces/bulul_build.py -- standing | small_m | small_f | spout | lizard | post

  standing  bulul_standing   a giant standing bulul, 8.6 m, holding an offering bowl of rice (stone; wood as a second atlas)
  small_m   bulul_small_m    the granary pair, 1.2 m each, dark hardwood (stone as a second atlas): he wears the head
  small_f   bulul_small_f    band and waist cloth, she has a bun, a bead collar and a wrap
  spout     bulul_spout      a guardian head on a wall plate, its open mouth a WATER SPOUT. Pivot: the wall face, at
                             the plate's foot. marks["water_from"] is the lip the water leaves.
  lizard    bulul_lizard     the Ifugao lizard, carved in relief on a wall panel. Pivot: the wall face, at its foot.
  post      bulul_post       a hornbill (kalaw) guardian post, 4 m.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bulul_kit as K                                                    # noqa: E402
from bulul_kit import (ACCENT, BASKET, CLOTH, DARK, INLAY, JAR, LEAF, MOSS, PBLACK, PCREAM, POCHRE, PRED, RICE, STONE,  # noqa: E402,F401
                       bx, carve, ell, hull, lathe, ring, tube)
import bulul_build as BB                                                 # noqa: E402
from bulul_build import head, mark, plinth, vine                         # noqa: E402


def standing():
    plinth(2.1, 1.9, 1.3, shelf=0.0)
    Y = 1.3
    for sx in (-1, 1):
        hull(bx((sx * 0.9, Y + 0.25, 0.45), (0.5, 0.25, 0.85), top=0.85), STONE, bev=0.1, name="foot")
        mark("grooves", {"c": [sx * 0.9, Y + 0.25, 1.15], "h": [0.5, 0.3, 0.22], "axis": "x", "n": 4})
        # knees a little bent, as the standing ones are carved
        hull(bx((sx * 0.9, Y + 0.6, 0.1), (0.42, 0.2, 0.42)) + bx((sx * 0.95, Y + 1.95, 0.5), (0.52, 0.35, 0.5)), STONE, bev=0.13, name="shin")
        hull(bx((sx * 0.95, Y + 1.95, 0.45), (0.53, 0.38, 0.5)) + bx((sx * 0.82, Y + 3.1, -0.05), (0.62, 0.4, 0.62)), STONE, bev=0.13, name="thigh")
        lathe([(0.36, 0), (0.36, 0.06), (0.25, 0.12), (0.0, 0.14)], (sx * 0.95, Y + 1.95, 0.98), STONE, seg=10, turn=(90, 0, 0), name="kneedisc")
        mark("discs", {"c": [sx * 0.95, Y + 1.95, 1.05], "r": 0.36, "axis": "z"})
        hull(bx((sx * 0.9, Y + 0.85, 0.12), (0.5, 0.13, 0.5)), ACCENT, bev=0.05, name="anklet")
        ell((sx * 1.85, Y + 5.45, 0.0), (0.7, 0.62, 0.72), STONE, seg=10, rings=6)
        hull(bx((sx * 1.88, Y + 5.2, 0.05), (0.42, 0.42, 0.44)) + bx((sx * 2.0, Y + 3.95, 0.45), (0.4, 0.38, 0.4)), STONE, bev=0.13, name="upperarm")
        hull(bx((sx * 2.0, Y + 3.9, 0.55), (0.4, 0.34, 0.36)) + bx((sx * 1.2, Y + 3.45, 1.65), (0.38, 0.32, 0.34)), STONE, bev=0.11, name="forearm")
        hull(bx((sx * 1.12, Y + 3.3, 1.95), (0.3, 0.4, 0.42)), STONE, bev=0.1, name="hand")
        mark("grooves", {"c": [sx * 1.12, Y + 3.3, 2.1], "h": [0.34, 0.42, 0.34], "axis": "y", "n": 4})
        hull(bx((sx * 1.94, Y + 4.6, 0.25), (0.5, 0.15, 0.5), r=(18, 0, 0)), ACCENT, bev=0.05, name="armlet")
        hull(bx((sx * 0.85, Y + 4.7, -0.78), (0.6, 0.62, 0.12), top=0.8), STONE, bev=0.07, name="blade")
    hull(bx((0, Y + 3.15, -0.1), (1.45, 0.5, 0.82)), STONE, bev=0.15, name="hips")
    hull(bx((0, Y + 3.3, -0.1), (1.25, 0.4, 0.7)) + bx((0, Y + 4.1, -0.1), (1.12, 0.3, 0.62)) + bx((0, Y + 5.3, 0.0), (1.62, 0.48, 0.8)), STONE, bev=0.15, name="torso")
    ell((0, Y + 4.05, 0.42), (0.95, 0.8, 0.62), STONE, seg=12, rings=7)                              # a round belly
    mark("dots", {"c": [0, Y + 3.95, 1.04], "r": 0.1, "col": "turq"})                                 # its navel, an inlay stud
    # the waist cloth and its flap hanging between the legs
    hull(bx((0, Y + 3.2, -0.1), (1.5, 0.2, 0.86)), CLOTH, bev=0.05, name="waistband")
    mark("cloths", {"type": "ring", "c": [0, Y + 3.2, -0.1], "h": 0.2, "r": 1.4})
    hull(bx((0, Y + 2.1, 0.72), (0.5, 1.05, 0.07), top=1.1, r=(-6, 0, 0)), CLOTH, bev=0.03, name="flap")
    mark("cloths", {"type": "strip", "c": [0, Y + 2.1, 0.72], "half": [0.58, 1.1, 0.3], "wide": "x", "along": "y", "fringe": True})
    hull(bx((0, Y + 2.2, -0.9), (0.42, 0.85, 0.07), r=(5, 0, 0)), CLOTH, bev=0.03, name="flapback")
    mark("cloths", {"type": "strip", "c": [0, Y + 2.2, -0.9], "half": [0.48, 0.9, 0.25], "wide": "x", "along": "y", "fringe": True})
    # the offering bowl in both hands, heaped with rice
    lathe([(0.5, 0), (0.7, 0.08), (1.1, 0.46), (1.18, 0.6), (1.02, 0.64), (0.94, 0.48)], (0, Y + 2.9, 2.0), BASKET, seg=14, name="bowl")
    ell((0, Y + 3.44, 2.0), (0.98, 0.42, 0.98), RICE, seg=10, rings=5)
    hull(bx((0, Y + 5.85, 0.0), (0.78, 0.4, 0.68)), STONE, bev=0.1, name="neck")
    hull(bx((0, Y + 5.78, -0.02), (1.08, 0.13, 0.98)), ACCENT, bev=0.06, name="collar")
    head((0, Y + 6.12, 0.3), 1.12, tilt=7, band=False, lid=0.07, smile=0.15)
    # a carved top knot in place of the head band, and worn red paint across the brow
    hull(bx((0, Y + 6.12 + 3.4, 0.55), (0.5, 0.26, 0.45), top=0.8), STONE, bev=0.08, name="knot")
    lathe([(0.86, 0), (0.86, 0.1), (0.62, 0.2), (0.3, 0.2), (0.26, 0.3), (0.0, 0.32)], (0, Y + 4.75, -0.78), STONE, seg=14, turn=(-92, 0, 0), name="disc")
    mark("discs", {"c": [0, Y + 4.75, -0.92], "r": 0.86, "axis": "z"})
    for c, r in (((-1.9, Y + 6.0, -0.1), (0.58, 0.2, 0.6)), ((1.85, Y + 5.98, -0.2), (0.44, 0.17, 0.5)), ((1.9, 1.32, -1.5), (0.6, 0.18, 0.5)),
                 ((-2.2, 0.62, 1.9), (0.55, 0.2, 0.5)), ((-1.5, 1.32, 1.4), (0.42, 0.14, 0.4))):
        ell(c, r, MOSS, seg=9, rings=5)
    vine([(1.9, Y + 6.0, -0.5), (2.3, Y + 5.3, -0.6), (2.35, Y + 4.3, -0.5), (2.2, Y + 3.3, -0.7), (1.9, Y + 2.3, -0.8)], leaves=5)
    vine([(-0.7, Y + 9.4, -0.6), (-1.1, Y + 8.7, -1.0), (-0.95, Y + 7.8, -1.15)], leaves=3, side=-1)
    # a jar and a rice bundle at its feet
    lathe([(0.20, 0), (0.36, 0.12), (0.44, 0.42), (0.38, 0.70), (0.22, 0.86), (0.25, 1.0)], (1.7, 1.3, 1.3), JAR, seg=10)
    lathe([(0.24, 0), (0.20, 0.1), (0.11, 0.38), (0.13, 0.45), (0.34, 0.78), (0.38, 0.9), (0.2, 0.98)], (-1.65, 1.3, 1.35), RICE, seg=8, turn=(6, 0, 10))
    K.MARKS["courses"] = {"h": 1.02, "ymax": Y + 5.7, "len": 2.3}

    def weight(c, n, pid):
        if n[1] < -0.9 and c[1] < 0.05:
            return 0.04
        if pid == INLAY:
            return 9.0
        if c[1] > 5.9:
            return 2.6 if c[2] > 0.5 else 1.5
        if pid in (CLOTH, RICE, JAR):
            return 1.6
        return 1.0
    K.finish("bulul_standing", size=2048, budget=7600, weight=weight, height=8.6)


def small(female):
    """One of the granary pair: squatting on a block, arms folded on the knees. Typed at about seven times its size.
    v1 came to 2,700 triangles and the decimator tore 77 edges open bringing it to 1,500. Now only the big forms
    (block, shins, thighs, torso, skull) are chamfered and nothing is decimated."""
    hull(bx((0, 0.35, 0.1), (1.9, 0.35, 1.8), top=0.9), STONE, bev=0.16, name="block")
    Y = 0.7
    for sx in (-1, 1):
        hull(bx((sx * 1.12, Y + 0.17, 1.3), (0.46, 0.2, 0.62)), STONE, bev=0.1, name="foot")
        hull(bx((sx * 1.12, Y + 0.5, 1.1), (0.36, 0.2, 0.38)) + bx((sx * 1.2, Y + 2.7, 1.35), (0.48, 0.42, 0.5)), STONE, bev=0.14, name="shin")
        hull(bx((sx * 1.2, Y + 2.65, 1.2), (0.48, 0.45, 0.45)) + bx((sx * 1.0, Y + 0.6, -0.5), (0.58, 0.6, 0.55)), STONE, bev=0.12, name="thigh")
        hull(bx((sx * (1.6 if female else 1.75), Y + 3.5, -0.2), (0.4, 0.48, 0.46), top=0.7) + bx((sx * 1.85, Y + 3.25, 1.2), (0.36, 0.36, 0.36)), STONE, bev=0.0, name="upperarm")
        mark("grooves", {"c": [sx * 1.12, Y + 0.2, 1.75], "h": [0.46, 0.25, 0.2], "axis": "x", "n": 4})
    hull(bx((0, Y + 0.6, -0.7), (1.3, 0.6, 0.75)) + bx((0, Y + 2.0, -0.6), (0.95, 0.3, 0.6)) + bx((0, Y + 3.5, -0.4), (1.3 if female else 1.5, 0.45, 0.75)), STONE, bev=0.16, name="torso")
    # the forearms: one from each elbow to the far knee, the left over the right
    # (their elbow ends are a little SMALLER than the upper arms' ends: equal boxes shared whole faces and the union
    # kept the membranes between them)
    hull(bx((1.83, Y + 3.27, 1.5), (0.32, 0.29, 0.3)) + bx((-1.0, Y + 3.3, 1.72), (0.4, 0.32, 0.3)), STONE, bev=0.0, name="forearm")
    hull(bx((-1.83, Y + 3.5, 1.42), (0.32, 0.28, 0.3)) + bx((1.0, Y + 3.85, 1.6), (0.4, 0.3, 0.3)), STONE, bev=0.0, name="forearm")
    mark("grooves", {"c": [-1.1, Y + 3.3, 1.9], "h": [0.34, 0.34, 0.2], "axis": "x", "n": 4})
    mark("grooves", {"c": [1.1, Y + 3.85, 1.78], "h": [0.34, 0.32, 0.2], "axis": "x", "n": 4})
    if female:
        hull(bx((0, Y + 0.8, -0.1), (1.48, 0.55, 1.42)), CLOTH, bev=0.0, name="wrap")
        mark("cloths", {"type": "ring", "c": [0, Y + 0.8, -0.1], "h": 0.55, "r": 1.4})
        hull(bx((0, Y + 3.92, -0.3), (1.0, 0.14, 0.9)), ACCENT, bev=0.0, name="collar")
        head((0, Y + 4.0, 0.15), 1.08, tilt=5, lite=True, band=False, female=True, studs=False, moss=False, smile=0.16, earring=False, lid=0.05)
        K.MARKS["beads"] = 14
    else:
        hull(bx((0, Y + 0.93, -0.72), (1.66, 0.19, 0.84)), CLOTH, bev=0.0, name="waistband")
        mark("cloths", {"type": "ring", "c": [0, Y + 0.93, -0.72], "h": 0.19, "r": 1.4})
        hull(bx((0, Y + 0.83, 1.32), (0.42, 0.87, 0.06)), CLOTH, bev=0.0, name="flap")
        mark("cloths", {"type": "strip", "c": [0, Y + 0.85, 1.32], "half": [0.46, 0.9, 0.2], "wide": "x", "along": "y", "fringe": True})
        head((0, Y + 4.0, 0.15), 1.15, tilt=5, lite=True, band=True, studs=False, moss=False, earring=False)
    K.MARKS["moss"] = 0.35
    K.MARKS["lichen"] = 0.0
    K.MARKS["primary"] = "wood"

    def weight(c, n, pid):
        if n[1] < -0.9 and c[1] < 0.02:
            return 0.04
        if pid == INLAY:
            return 6.0
        return 2.2 if c[1] > 0.68 and c[2] > 0.1 else 1.0
    K.finish("bulul_small_f" if female else "bulul_small_m", size=1024, budget=2200, weight=weight, height=1.2 if not female else 1.14)


def spout():
    """A guardian head on a wall plate. Its back is z = 0 (the wall face); it reaches 1.6 m out."""
    hull(bx((0, 1.0, 0.13), (1.12, 1.0, 0.13), top=0.9), STONE, bev=0.06, name="plate")
    hull(bx((0, 0.14, 0.2), (1.2, 0.14, 0.2)), STONE, bev=0.05, name="sill")
    hull(bx((0, 2.05, 0.18), (0.8, 0.14, 0.18), top=0.8), STONE, bev=0.05, name="cap")
    s = 0.56
    c = (0, 0.42, 0.78)
    head(c, s, tilt=6, band=False, spout=True, studs=True, earring=True, brow_tilt=-2.0, moss=False)
    # a crest of five stepped stones fanned over the brow, as the references' totems wear
    for i, a in enumerate((-50, -25, 0, 25, 50)):
        r = math.radians(a)
        hgt = 0.34 if i % 2 == 0 else 0.26
        hull(bx((math.sin(r) * 0.66, c[1] + 2.5 * s + math.cos(r) * 0.36, c[2] + 0.2), (0.16, hgt, 0.2), top=0.6, r=(8, 0, -a)), POCHRE if i % 2 == 0 else PRED, bev=0.05, name="crest")
    ell((-0.55, 2.2, 0.2), (0.42, 0.14, 0.2), MOSS, seg=8, rings=4)
    ell((0.8, 0.3, 0.3), (0.34, 0.12, 0.2), MOSS, seg=8, rings=4)
    ell((0.3, c[1] + 2.95 * s, 0.75), (0.4, 0.12, 0.34), MOSS, seg=8, rings=4)
    vine([(-0.9, 2.1, 0.3), (-1.08, 1.6, 0.34), (-1.0, 1.1, 0.36), (-1.1, 0.6, 0.36)], r=0.045, leaves=4, side=-1, big=0.2)
    K.MARKS["water_from"] = K.nodded([0, c[1] + 0.2 * s, c[2] + 1.6 * s], c, 6)
    K.MARKS["wall"] = True
    K.MARKS["wet"] = 1.0
    K.MARKS["paintwear"] = 0.14

    def weight(cc, n, pid):
        if n[2] < -0.9 and cc[2] < 0.02:
            return 0.04
        if pid == INLAY:
            return 5.0
        return 1.6 if cc[2] > 0.9 else 1.0
    K.finish("bulul_spout", size=1024, budget=3400, weight=weight)


def lizard():
    """The Ifugao lizard climbing a framed panel, in relief. Back is z = 0. v2 read as a gingerbread man (a round
    head, a body with buttons, a tail lost among the legs): now a kite of a head with a snout, a long tail a third of
    its length, thin bent legs with four long toes, and chevrons cut across the back."""
    hull(bx((0, 1.3, 0.09), (0.75, 1.3, 0.09)), STONE, bev=0.03, name="slab")
    for sx in (-1, 1):
        hull(bx((sx * 0.66, 1.3, 0.2), (0.1, 1.3, 0.05)), STONE, bev=0.035, name="frame")
        for cy in (0.2, 2.4):
            mark("dots", {"c": [sx * 0.66, cy, 0.26], "r": 0.035, "col": "turq"})
    hull(bx((0, 2.5, 0.21), (0.82, 0.11, 0.06), top=0.94), STONE, bev=0.035, name="frame")
    hull(bx((0, 0.1, 0.21), (0.82, 0.1, 0.06)), STONE, bev=0.035, name="frame")
    mark("fret", {"y0": 2.4, "y1": 2.6, "hw": 0.8, "hd": 0.27})
    mark("fret", {"y0": 0.01, "y1": 0.19, "hw": 0.82, "hd": 0.27})
    Z = 0.25
    hull(bx((0, 1.9, Z), (0.11, 0.05, 0.06)) + bx((0, 1.64, Z + 0.015), (0.145, 0.12, 0.09)) + bx((0, 1.4, Z), (0.1, 0.05, 0.06)), POCHRE, bev=0.03, name="body")
    hull(bx((0, 1.98, Z), (0.08, 0.04, 0.055)) + bx((0, 2.07, Z + 0.01), (0.125, 0.045, 0.075)) + bx((0, 2.32, Z - 0.01), (0.03, 0.03, 0.04)), POCHRE, bev=0.025, name="lizhead")
    mark("grooves", {"c": [0, 1.65, Z + 0.07], "h": [0.19, 0.26, 0.06], "axis": "y", "n": 7, "w": 0.7})
    for sx in (-1, 1):
        mark("dots", {"c": [sx * 0.092, 2.08, Z + 0.085], "r": 0.018, "col": "turq"})
        for (p0, p1, p2, d) in (((0.1, 1.86), (0.36, 1.96), (0.41, 2.2), 1), ((0.1, 1.42), (0.38, 1.34), (0.43, 1.08), -1)):
            hull(bx((sx * p0[0], p0[1], Z - 0.02), (0.04, 0.045, 0.045)) + bx((sx * p1[0], p1[1], Z - 0.02), (0.045, 0.045, 0.045)), POCHRE, bev=0.015, name="leg")
            hull(bx((sx * p1[0], p1[1], Z - 0.02), (0.045, 0.045, 0.045)) + bx((sx * p2[0], p2[1], Z - 0.03), (0.04, 0.04, 0.04)), POCHRE, bev=0.015, name="leg")
            for a in (-48, -16, 16, 48):
                r = math.radians(a)
                hull(bx((sx * p2[0] + math.sin(r) * 0.07, p2[1] + d * (0.02 + math.cos(r) * 0.07), Z - 0.04), (0.016, 0.07, 0.028), r=(0, 0, -a * d)), POCHRE, bev=0.0, name="toe")
    tube([(0, 1.38, Z), (0.0, 1.14, Z), (-0.07, 0.9, Z - 0.01), (0.03, 0.68, Z - 0.02), (0.11, 0.52, Z - 0.02), (0.06, 0.38, Z - 0.03), (-0.02, 0.34, Z - 0.03)],
         [0.105, 0.09, 0.07, 0.052, 0.04, 0.028, 0.016], POCHRE, seg=6, flat=0.8, name="tail")
    mark("grooves", {"c": [0, 0.95, Z + 0.05], "h": [0.16, 0.42, 0.07], "axis": "y", "n": 9, "w": 0.6})
    mark("paintboxes", {"c": [0, 1.3, 0.185], "half": [0.56, 1.15, 0.012], "col": "red", "keep": 0.36})
    ell((-0.5, 2.62, 0.14), (0.3, 0.07, 0.14), MOSS, seg=7, rings=4)
    ell((0.6, 0.22, 0.24), (0.2, 0.06, 0.1), MOSS, seg=7, rings=4)
    vine([(0.6, 2.55, 0.28), (0.72, 2.2, 0.3), (0.66, 1.85, 0.3), (0.74, 1.5, 0.3)], r=0.03, leaves=4, big=0.13)
    K.MARKS["face"] = {"c": [0, 1.85, 0.3], "size": 0.75}
    K.MARKS["wall"] = True
    K.MARKS["lichen"] = 0.0
    K.MARKS["paintwear"] = 0.06
    K.finish("bulul_lizard", size=1024, budget=2000, weight=lambda c, n, pid: 0.04 if (n[2] < -0.9 and c[2] < 0.02) else (2.0 if c[2] > 0.19 else 1.0))


def post():
    """A hornbill (kalaw) guardian post: a carved tapering shaft, a flared capital, the bird on top looking out."""
    hull(bx((0, 0.2, 0), (0.62, 0.2, 0.62), top=0.9), STONE, bev=0.07, name="base")
    hull(bx((0, 0.6, 0), (0.42, 0.2, 0.42)) + bx((0, 2.3, 0), (0.33, 0.1, 0.33)), STONE, bev=0.07, name="shaft")
    for y, w in ((0.92, 0.47), (2.02, 0.4)):
        hull(bx((0, y, 0), (w, 0.09, w)), ACCENT, bev=0.035, name="ring")
    lathe([(0.27, 0), (0.27, 0.05), (0.19, 0.1), (0.0, 0.12)], (0, 1.48, 0.36), STONE, seg=10, turn=(90, 0, 0), name="disc")
    mark("discs", {"c": [0, 1.48, 0.42], "r": 0.27, "axis": "z"})
    hull(bx((0, 2.55, 0), (0.5, 0.16, 0.5), top=1.16), STONE, bev=0.06, name="capital")
    mark("fret", {"y0": 2.42, "y1": 2.68, "hw": 0.52, "hd": 0.52})
    # the bird: black, a cream breast and tail tip, a great ochre beak under a red casque. Typed, then grown by a
    # fifth about its perch (v2's bird was small for its post).
    n0, BIG, PV = len(K.PARTS), 1.22, (0.0, 2.71, 0.0)

    def G(p):
        return [PV[0] + (p[0] - PV[0]) * BIG, PV[1] + (p[1] - PV[1]) * BIG, PV[2] + (p[2] - PV[2]) * BIG]
    ell((0, 3.3, -0.1), (0.44, 0.56, 0.52), PBLACK, seg=10, rings=6, turn=(-18, 0, 0))
    ell((0, 3.2, 0.2), (0.33, 0.4, 0.3), PCREAM, seg=8, rings=5)                                     # the breast
    for sx in (-1, 1):
        hull(bx((sx * 0.43, 3.26, -0.22), (0.08, 0.42, 0.48), top=0.7, r=(-28, 0, sx * 5)), PBLACK, bev=0.05, name="wing")
        hull(bx((sx * 0.5, 3.12, -0.3), (0.035, 0.07, 0.4), r=(-28, 0, sx * 5)), PCREAM, bev=0.0, name="wingbar")
        hull(bx((sx * 0.16, 2.78, 0.2), (0.07, 0.07, 0.16)), POCHRE, bev=0.02, name="claw")
        mark("dots", {"c": G([sx * 0.31, 4.02, 0.5]), "r": 0.075 * BIG, "col": "cream"})
        mark("dots", {"c": G([sx * 0.33, 4.02, 0.53]), "r": 0.036 * BIG, "col": "black"})
    hull(bx((0, 2.95, -0.7), (0.24, 0.07, 0.3), r=(-38, 0, 0)), PBLACK, bev=0.04, name="tail")
    hull(bx((0, 2.72, -0.98), (0.27, 0.07, 0.2), top=1.0, r=(-38, 0, 0)), PCREAM, bev=0.04, name="tailtip")
    ell((0, 3.98, 0.26), (0.34, 0.36, 0.38), PBLACK, seg=10, rings=6)                                 # the head
    hull(bx((0, 3.94, 0.62), (0.14, 0.15, 0.12)) + bx((0, 3.8, 1.08), (0.08, 0.09, 0.09)) + bx((0, 3.54, 1.3), (0.03, 0.035, 0.035)), POCHRE, bev=0.035, name="beak")
    hull(bx((0, 4.2, 0.5), (0.11, 0.13, 0.24)) + bx((0, 4.32, 0.94), (0.065, 0.07, 0.08)), PRED, bev=0.035, name="casque")
    hull(bx((0, 4.34, 0.0), (0.05, 0.13, 0.2), r=(-20, 0, 0)), PBLACK, bev=0.02, name="crestfeather")
    from mathutils import Matrix
    pvb = K.B(PV)
    for o in K.PARTS[n0:]:
        o.data.transform(Matrix.Translation(pvb) @ Matrix.Scale(BIG, 4) @ Matrix.Translation(-pvb))
    ell((-0.25, 2.74, -0.3), (0.3, 0.07, 0.26), MOSS, seg=7, rings=4)
    ell((0.4, 0.42, 0.42), (0.26, 0.07, 0.22), MOSS, seg=7, rings=4)
    hx = []
    for i in range(11):                                                                              # a vine wound up the shaft
        a = i * 1.15
        rr = 0.47 - 0.011 * i * 1.0
        hx.append((math.cos(a) * rr, 0.5 + i * 0.185, math.sin(a) * rr))
    vine(hx, r=0.04, leaves=6, big=0.17)
    K.MARKS["face"] = {"c": G([0, 3.95, 0.6]), "size": 1.15}
    K.MARKS["lichen"] = 0.6
    K.finish("bulul_post", size=1024, budget=2600,
             weight=lambda c, n, pid: 0.04 if (n[1] < -0.9 and c[1] < 0.02) else (2.0 if c[1] > 2.75 else 1.0))


PIECES = {"standing": standing, "small_m": lambda: small(False), "small_f": lambda: small(True), "spout": spout, "lizard": lizard, "post": post}
