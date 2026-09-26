"""The roster rework's props (ABILITY-2, 2026-09-26), modelled part by part: Dante's BOULDER and BARRIER,
Phaister's cursed DOLL and HIGOP's accretion ring.

    python3 tools/build_rework_props.py [boulder] [barrier] [doll] [higop]

⚠️ WHY MODELS: the first pass drew these from Unity cubes (`GeoVfx.cs`, `VoodooVfx.cs`), which is the
"js blocks" look the owner rejected on Paete's props (*"the current models of all his skills look ugly
still its js blocks"*). They are built the way Paete's are (tools/build_paete_props.py's helpers:
chamfered boxes, faceted tubes, six-sided prism leaves, smoothed normals so the inverted-hull ink
closes) and dressed at runtime with their OWN sixteen-slot palette (`ReworkProp.Palette*`), so each
power's things read as that power's: Geo is stepped stone with his gold veins and dust; Voodoo is
stitched cloth, dark thread and bright pins.

⚠️ EVERY PART IS TYPED, NOT STAMPED (the owner's standing rule, *"manually do each part of that builder
dont js auto generate looping shit"*): each slab, chip, vein, stitch and pin is its own row of numbers.
Parts that move are named nodes (the barrier's slabs rise one by one; the ring's pins turn).

Output: Assets/TumbangPreso/Resources/Models/ReworkProps/{boulder,barrier,doll,higop}.glb, metres, +Y up,
the front on +Z.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_paete_props as bp  # noqa: E402
from build_paete_props import Node, line, ring, add  # noqa: E402

ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Models", "ReworkProps")

# Geo slots: 0 stone, 1 stone dark, 2 stone lit, 3 gold vein, 4 gold dark, 5 dust, 6 grit, 8 ink.
GEO_PALETTE = {0: "8A7A66", 1: "5E5244", 2: "B09C80", 3: "E8B43A", 4: "A87A1E", 5: "C8B89A", 6: "4A4036",
               7: "7A6C5A", 8: "1E140C", 9: "3A3028", 10: "D8C8A8", 11: "6E6252", 12: "F2CE64", 13: "8C6440",
               14: "553A22", 15: "B08450"}
STONE, STONE_DK, STONE_LT, GOLD, GOLD_DK, DUST, GRIT = 0, 1, 2, 3, 4, 5, 6

# Voodoo slots: 0 cloth, 1 cloth dark, 2 stitch, 3 pin head, 4 pin steel, 5 button, 6 thread, 7 thread dark, 8 ink.
VOODOO_PALETTE = {0: "A9785F", 1: "74503F", 2: "2A1620", 3: "E24FA6", 4: "D8D0DA", 5: "1C1014", 6: "C2398F",
                  7: "7A1F5A", 8: "1E140C", 9: "F2E6DA", 10: "5A2A48", 11: "3A1830", 12: "FF8FD0", 13: "8C6440",
                  14: "553A22", 15: "B08450"}
CLOTH, CLOTH_DK, STITCH, PIN_HEAD, STEEL, BUTTON, THREAD, THREAD_DK = 0, 1, 2, 3, 4, 5, 6, 7


def boulder():
    """A chunky rock: a core block with a cap and two broken chips, faceted, one gold vein across it
    (his mark), a scatter of grit stuck to it. About 0.6 m across."""
    root = Node("boulder")
    body = Node("body", parent="boulder")
    body.obox((0.0, 0.0, 0.0), (0.52, 0.44, 0.50), STONE, yaw=12.0, pitch=8.0, roll=6.0)
    body.obox((0.06, 0.20, -0.04), (0.38, 0.20, 0.40), STONE_LT, yaw=41.0, pitch=-6.0, roll=4.0)
    body.obox((-0.20, -0.10, 0.14), (0.26, 0.26, 0.22), STONE_DK, yaw=-17.0, pitch=22.0, roll=28.0)
    body.obox((0.22, -0.12, -0.16), (0.20, 0.22, 0.24), STONE_DK, yaw=33.0, pitch=-18.0, roll=-12.0)
    body.obox((-0.04, -0.20, -0.06), (0.34, 0.12, 0.30), STONE_DK, yaw=5.0)
    # The gold vein, cut into three typed segments so it runs over the faces rather than floating.
    line(body, [(-0.24, 0.10, 0.25), (-0.05, 0.19, 0.27), (0.14, 0.12, 0.26), (0.27, -0.02, 0.18)],
         [0.022, 0.026, 0.024, 0.016], GOLD, sides=4, per=3)
    line(body, [(0.27, -0.02, 0.18), (0.29, -0.14, 0.02), (0.22, -0.20, -0.14)], [0.016, 0.018, 0.012], GOLD_DK, sides=4, per=3)
    for c, s, y in [((0.12, 0.23, 0.10), (0.06, 0.04, 0.05), 20), ((-0.18, 0.12, -0.20), (0.05, 0.04, 0.06), -35),
                    ((0.02, -0.02, 0.27), (0.04, 0.03, 0.03), 60)]:
        body.obox(c, s, GRIT, yaw=y)
    bp.write(os.path.join(OUT, "boulder.glb"), [root, body])


def barrier():
    """BARRIER: three stepped stone slabs across his front (the middle one tallest), each its own node so
    they grind up one by one, with his gold seams running up between them and a dust skirt at the foot.
    Width 3.4 m (`GeoRules.BarrierWidth`), authored with its base at y 0 and its face on +Z."""
    root = Node("barrier")
    nodes = [root]
    slabs = [("slab-0", (-1.13, 0.0, 0.05), -12.0, (1.12, 1.70, 0.16), STONE, [((-0.30, 1.20, 0.09), (0.40, 0.30, 0.02), STONE_LT), ((0.18, 0.55, 0.09), (0.34, 0.24, 0.02), STONE_DK)]),
             ("slab-1", (0.0, 0.0, 0.12), 0.0, (1.22, 2.10, 0.18), STONE_LT, [((0.22, 1.62, 0.10), (0.46, 0.34, 0.02), STONE), ((-0.28, 0.72, 0.10), (0.38, 0.30, 0.02), STONE_DK), ((0.10, 0.25, 0.10), (0.60, 0.16, 0.02), STONE)]),
             ("slab-2", (1.16, 0.0, 0.04), 14.0, (1.08, 1.55, 0.16), STONE, [((0.24, 1.05, 0.09), (0.36, 0.28, 0.02), STONE_DK), ((-0.20, 0.42, 0.09), (0.42, 0.22, 0.02), STONE_LT)])]
    for name, at, yaw, size, slot, facets in slabs:
        n = Node(name, origin=at, parent="barrier", yaw=yaw); nodes.append(n)
        w, h, d = size
        n.obox((0.0, h * 0.5, 0.0), size, slot)
        n.obox((0.0, h + 0.05, -0.01), (w * 0.8, 0.10, d * 1.1), STONE_DK)          # a capstone
        # ⚠️ NO FLAT PANELS ON THE FACE: v1 laid lighter and darker plates on each slab and they read as
        # stuck-on windows (Paete's "yellow shit" lesson). The stone reads by its chamfers, its capstone
        # and a chipped corner instead; `facets` now only places that one chip per slab.
        c, s_, fs = facets[0]
        n.obox((c[0] * 1.6, h - 0.08, 0.0), (0.22, 0.20, d * 1.15), STONE_DK, yaw=18.0, roll=12.0)
    seams = Node("seams", parent="barrier"); nodes.append(seams)
    line(seams, [(-0.58, 0.05, 0.26), (-0.55, 0.60, 0.27), (-0.60, 1.10, 0.26), (-0.57, 1.62, 0.25)], [0.030, 0.034, 0.030, 0.020], GOLD, sides=4, per=3)
    line(seams, [(0.60, 0.05, 0.25), (0.57, 0.50, 0.26), (0.62, 0.98, 0.26), (0.59, 1.46, 0.24)], [0.030, 0.032, 0.028, 0.018], GOLD, sides=4, per=3)
    skirt = Node("skirt", parent="barrier"); nodes.append(skirt)
    for c, s, y in [((-1.4, 0.04, 0.30), (0.40, 0.08, 0.26), 20), ((-0.7, 0.05, 0.36), (0.34, 0.10, 0.22), -15),
                    ((0.05, 0.05, 0.40), (0.46, 0.10, 0.24), 8), ((0.8, 0.04, 0.34), (0.32, 0.08, 0.24), -30),
                    ((1.45, 0.05, 0.28), (0.36, 0.10, 0.22), 25)]:
        skirt.obox(c, s, DUST, yaw=y)
    bp.write(os.path.join(OUT, "barrier.glb"), nodes)


def doll():
    """The cursed doll: a stitched cloth body and head, stub arms and legs, two button eyes (the only face,
    ink-dark, as the cast's faces are), a stitched seam down its front, three pins through it. 0.34 m."""
    root = Node("doll")
    b = Node("body", parent="doll")
    b.obox((0.0, 0.12, 0.0), (0.16, 0.20, 0.10), CLOTH)
    b.obox((0.0, 0.29, 0.0), (0.15, 0.14, 0.12), CLOTH)
    b.obox((-0.11, 0.15, 0.0), (0.07, 0.05, 0.06), CLOTH_DK, roll=-20.0)
    b.obox((0.11, 0.14, 0.0), (0.07, 0.05, 0.06), CLOTH_DK, roll=24.0)
    b.obox((-0.04, 0.0, 0.0), (0.06, 0.07, 0.07), CLOTH_DK)
    b.obox((0.045, -0.005, 0.0), (0.06, 0.06, 0.07), CLOTH_DK)
    b.obox((-0.035, 0.31, 0.062), (0.03, 0.03, 0.01), BUTTON)
    b.obox((0.035, 0.30, 0.062), (0.032, 0.032, 0.01), BUTTON)
    # The seam: one dark line down the front with short cross-stitches over it, each typed.
    b.obox((0.0, 0.12, 0.052), (0.008, 0.19, 0.006), STITCH)
    for c, w in [((0.0, 0.195, 0.056), 0.036), ((0.0, 0.150, 0.056), 0.030), ((0.0, 0.105, 0.056), 0.034), ((0.0, 0.060, 0.056), 0.028)]:
        b.obox(c, (w, 0.008, 0.006), STITCH)
    for a, bb in [((0.02, 0.33, -0.10), (0.05, 0.28, 0.10)), ((-0.12, 0.10, -0.06), (-0.02, 0.16, 0.10)), ((0.10, 0.20, 0.09), (0.02, 0.12, -0.08))]:
        line(b, [a, bb], [0.007, 0.006], STEEL, sides=4, per=2)
        b.obox(a, (0.028, 0.028, 0.028), PIN_HEAD)
    bp.write(os.path.join(OUT, "doll.glb"), [root, b])


def higop():
    """HIGOP's accretion ring: stitched thread wound round the hole in two uneven loops (each typed as
    its own points), with pins caught in it at their own angles. The dark core stays procedural."""
    root = Node("higop")
    nodes = [root]
    loop_a = Node("loop-a", parent="higop"); nodes.append(loop_a)
    pts = [ring(r, a, y) for a, r, y in [(0, 1.10, 0.00), (40, 1.18, 0.06), (85, 1.08, 0.02), (130, 1.20, -0.05),
                                         (178, 1.12, 0.00), (222, 1.22, 0.07), (270, 1.09, 0.01), (318, 1.17, -0.06),
                                         (360, 1.10, 0.00), (400, 1.18, 0.06)]]
    loop_a.tube(bp.spline(pts, 4), [0.045] * len(bp.spline(pts, 4)), THREAD, sides=5)
    loop_b = Node("loop-b", parent="higop"); nodes.append(loop_b)
    pts = [ring(r, a, y) for a, r, y in [(10, 1.45, 0.10), (60, 1.38, -0.12), (115, 1.52, 0.04), (160, 1.40, 0.14),
                                         (215, 1.48, -0.08), (265, 1.36, 0.10), (310, 1.50, -0.04), (370, 1.45, 0.10), (420, 1.38, -0.12)]]
    loop_b.tube(bp.spline(pts, 4), [0.030] * len(bp.spline(pts, 4)), THREAD_DK, sides=5)
    pins = Node("pins", parent="higop"); nodes.append(pins)
    for a, r, y, tilt, length in [(25, 1.30, 0.05, 25, 0.42), (97, 1.26, -0.04, -30, 0.36), (160, 1.34, 0.08, 18, 0.44),
                                  (233, 1.24, -0.02, -22, 0.38), (301, 1.32, 0.06, 32, 0.40)]:
        base = ring(r, a, y)
        tip = add(base, ring(length, a + 90 + tilt, length * 0.3))
        line(pins, [base, tip], [0.018, 0.010], STEEL, sides=4, per=2)
        pins.obox(tip, (0.07, 0.07, 0.07), PIN_HEAD, yaw=a)
    bp.write(os.path.join(OUT, "higop.glb"), nodes)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    which = set(sys.argv[1:]) or {"boulder", "barrier", "doll", "higop"}
    if "boulder" in which: boulder()
    if "barrier" in which: barrier()
    if "doll" in which: doll()
    if "higop" in which: higop()
