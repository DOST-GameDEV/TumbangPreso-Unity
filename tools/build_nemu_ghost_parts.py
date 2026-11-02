"""Nemu's PEEKER mouse and FETCHER bear, AS PARTS (owner 2026-10-29, after the kit film k1).

  PEEKER: "the peeker looks to stiff unlike how we did kuro.. its a single rigid model, but i need it to be more ghost
           like and jumpy, with an animated face preferably"
  FETCHER: "bear ghost also doesnt look too ghosty.. looks more like a regular pet tbh" ("looks/acts")

So each is built the way Kuro is: separate named parts that the game poses every frame (`Runtime/Visual/PetGhostRig.cs`),
and a face made of small swapped meshes (Kuro's `KuroExpressions` way): no per-frame mesh work, no blend shapes.

The approved look is KEPT: the same block head, the same sizes, the same mint and the lineup's light cocoa, the same atlas painting and
cells as `tools/build_nemu_ghosts_simple.py --final` (it is imported and reused, not copied). What changes is the CUT:
  * the head is its own part (the block, with the bear's muzzle in it);
  * Kuro's three beads become a WISP: a body that tapers into two tail tiers, each on its own pivot so the game can bend
    it like cloth in water; no feet; the paint pales down it, and the game thins it toward the tip;
  * ears, eyes, mouth and nose are their own parts, each on its own pivot (an eye's pivot is its middle, so it blinks by
    its own height); the expressions are extra parts the game shows one set of at a time:
        eye (the plain dot), eye-sneak (a sly half lid, mouse), eye-wide (white, pupil, glint), eye-happy (a closed arc);
        mouth (the small dot), mouth-sneak (a sideways smirk, mouse), mouth-grin (open, with teeth), mouth-giggle (a
        cat's "w"); nose (mouse: a pink bead; the bear keeps its ink nose on the muzzle);
  * the mouse's cloth is two parts: `cloth` (the band over its eyes, which the game slides up to peek and down to duck)
    and `cloth-neck` (where it drops when it springs); its two little paws are parts under its chin;
  * the bear's two arms are NOT here: the game keeps posing `nemu_petghost_bear.glb`'s arm nubs at the rule's hand points.

Every node's TRANSLATION is its pivot; every mesh is built round that pivot. The game reads the pivots off the imported
model, so no number below has to be typed into C#.

  py -3 tools/build_nemu_ghost_parts.py [--out=<folder>]

Writes nemu_petghost_mouse_parts.glb / nemu_petghost_bear_parts.glb and their atlases into
Assets/TumbangPreso/Resources/Models/NemuGhosts (or --out), at the game's 1.35 export scale.
"""
import os
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
if os.environ.get("NEMU_PARTS_TOOLS"):          # run from outside the repo: where the kit and the simple builder live
    sys.path.insert(0, os.environ["NEMU_PARTS_TOOLS"])
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, moved, rounded, tube  # noqa: E402
import build_nemu_ghosts_simple as simple  # noqa: E402
from build_nemu_ghosts_simple import (INK, WHITE, BODY, SHADE_CELL, BEAK, EAR, PATCH, PAD, R_BODY, R_TAIL, R_PROP,  # noqa: E402
                                      block_form, paint_v7, final_atlas, cells, EXPORT_SCALE, OUT)

# The painted source atlases and the kit's palette notes go to a scratch folder when asked (`NEMU_PARTS_SCRATCH`).
if os.environ.get("NEMU_PARTS_SCRATCH"):
    OUT = Path(os.environ["NEMU_PARTS_SCRATCH"])
    kit.LOGS = OUT
DEST = Path(next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--out=")),
                 str(kit.ROOT / "Assets/TumbangPreso/Resources/Models/NemuGhosts")))


def inset(r):
    return (r[0] + PAD, r[1] + PAD, r[2] - PAD, r[3] - PAD)


def wisp(w, lift):
    """The body and its two tail tiers, each (name, pivot, pieces). Built where Kuro's three beads hung (the first bead's
    middle at lift - 0.01, 0.27 w across), leaning back as they did, ending in a fine curled point."""
    u0, v0, u1, v1 = R_TAIL
    def band(i):
        return (u0 + PAD, v1 - (v1 - v0) * (i + 1) / 3 + PAD, u1 - PAD, v1 - (v1 - v0) * i / 3 - PAD)
    seg = 16
    out = []
    # BODY: tucked up inside the head so no seam shows, widest just under it.
    pb = np.array((0.0, lift + 0.04, 0.0))
    body = tube([(0, 0, 0), (0, -0.06, -0.012), (0, -0.09, -0.022)], [w * .27, w * .26, w * .20], seg=seg)
    out.append(("body", pb, [(body, kit.grid_uv(body, band(0), seg, "rows", 1))]))
    # TAIL 0, hung from the body's foot; a ball at its joint hides the seam when it bends.
    p0 = pb + np.array((0.0, -0.09, -0.022))
    t0 = tube([(0, 0, 0), (0.004, -0.035, -0.022), (0.010, -0.060, -0.040)], [w * .20, w * .15, w * .11], seg=seg)
    j0 = ellipsoid((w * .20, w * .20, w * .20), seg=seg, rings=10)
    out.append(("tail-0", p0, [(t0, kit.grid_uv(t0, band(1), seg, "rows", 1)), (j0, kit.grid_uv(j0, band(1), seg, "rows", 1))]))
    # TAIL 1, the curled point.
    p1 = p0 + np.array((0.010, -0.060, -0.040))
    t1 = tube([(0, 0, 0), (0.012, -0.030, -0.025), (0.030, -0.045, -0.050)], [w * .11, w * .06, w * .012], seg=seg)
    j1 = ellipsoid((w * .11, w * .11, w * .11), seg=seg, rings=10)
    out.append(("tail-1", p1, [(t1, kit.grid_uv(t1, band(2), seg, "rows", 1)), (j1, kit.grid_uv(j1, band(2), seg, "rows", 1))]))
    return out


def on_face(front, x, y, pieces, lift_z=0.0):
    """A face part: built flat in its own frame (z out of the face), turned to the head's surface at (x, y). Returns the
    pivot (on the surface) and the turned pieces round it."""
    at, turn = front(x, y)
    return np.array(at) + np.array((0, 0, lift_z)), [(moved(m, turn=turn), slot) for m, slot in pieces]


def eye_parts(front, gap, y, sneak):
    parts = []
    for side, nm in ((-1, "l"), (1, "r")):
        x = side * gap
        parts.append(("eye-" + nm,) + on_face(front, x, y, [(ellipsoid((0.024, 0.030, 0.008), seg=16, rings=10), INK)]))
        wide = [(ellipsoid((0.034, 0.040, 0.005), seg=18, rings=10), WHITE),
                (ellipsoid((0.021, 0.026, 0.008), at=(0, -0.002, 0.003), seg=16, rings=10), INK),
                (ellipsoid((0.007, 0.007, 0.004), at=(-side * 0.007, 0.010, 0.010), seg=10, rings=6), WHITE)]
        parts.append(("eye-wide-" + nm,) + on_face(front, x, y, wide))
        arc = tube([(-0.022, -0.006, 0.004), (0, 0.010, 0.006), (0.022, -0.006, 0.004)], 0.0065, seg=8)
        parts.append(("eye-happy-" + nm,) + on_face(front, x, y, [(arc, INK)]))
        if sneak:
            # A sly half lid: a flat bar tipped down toward the nose (the scheming look), pupil showing under it.
            lid = moved(ellipsoid((0.030, 0.012, 0.008), seg=16, rings=10), turn=(0, 0, side * 10))
            pupil = ellipsoid((0.012, 0.010, 0.007), at=(side * 0.006, -0.012, 0.0), seg=12, rings=8)
            parts.append(("eye-sneak-" + nm,) + on_face(front, x, y, [(lid, INK), (pupil, INK)]))
    return parts


def mouth_parts(front, y, sneak):
    parts = [("mouth",) + on_face(front, 0.0, y, [(ellipsoid((0.010, 0.007, 0.006)), INK)])]
    grin = [(ellipsoid((0.032, 0.019, 0.006), seg=18, rings=10), INK),
            (ellipsoid((0.024, 0.005, 0.004), at=(0, 0.010, 0.004), seg=12, rings=6), WHITE)]
    parts.append(("mouth-grin",) + on_face(front, 0.0, y - 0.004, grin))
    w_l = tube([(-0.020, 0.004, 0.003), (-0.010, -0.006, 0.004), (0.0, 0.002, 0.004)], 0.004, seg=8)
    w_r = tube([(0.0, 0.002, 0.004), (0.010, -0.006, 0.004), (0.020, 0.004, 0.003)], 0.004, seg=8)
    parts.append(("mouth-giggle",) + on_face(front, 0.0, y, [(w_l, INK), (w_r, INK)]))
    if sneak:
        smirk = tube([(-0.012, 0.0, 0.004), (0.002, -0.004, 0.005), (0.016, 0.005, 0.004)], 0.0045, seg=8)
        parts.append(("mouth-sneak",) + on_face(front, 0.0, y, [(smirk, INK)]))
    return parts


def export(name, c, nodes):
    """Paint the atlas the way the approved models are painted, then write the parts at the game's export scale."""
    src = Model(name + "_src"); src.folder = OUT; src.texture = name + "_src-atlas.png"
    OUT.mkdir(parents=True, exist_ok=True)
    paint_v7(OUT / src.texture, c)
    k = EXPORT_SCALE
    out = Model(name); out.folder = DEST; out.texture = name + "-atlas.png"
    DEST.mkdir(parents=True, exist_ok=True)
    final_atlas(OUT / src.texture, DEST / out.texture, cells(c))
    for node, pivot, pieces in nodes:
        out.add(node, [((m[0] * k, m[1], m[2]), slot) for m, slot in pieces], at=tuple(float(v) * k for v in pivot))
    out.write(cells(c))
    for node, pivot, _ in nodes:
        print("  %-14s pivot (%.3f, %.3f, %.3f)" % (node, *(float(v) * k for v in pivot)))


def head_block(w, h, d, lift):
    _, front, (cy, half) = block_form(w, h, d, lift, lift)
    radius = min(half) * 0.62
    blk = rounded(half, radius, seg=24, rings=16)
    return front, cy, half, (blk, kit.grid_uv(blk, inset(R_BODY), 24, "axis", 1))


def mouse():
    c = dict(simple.SETS["pastel"]["dontlook"]); c["beak"] = "#F49CB4"    # the nose: a pink bead
    w, h, d, lift = 0.29, 0.25, 0.25, 0.17
    front, cy, half, head = head_block(w, h, d, lift)
    top = cy + half[1]
    face_y = cy + half[1] * .05
    hp = np.array((0.0, cy, 0.0))
    nodes = [("head", hp, [head])]
    for side, nm in ((-1, "l"), (1, "r")):
        # The ear's pivot is its root on the crown, so it flicks and flattens about where it grows.
        centre = np.array((side * 0.105, top + 0.015, -0.02)); root = np.array((side * 0.080, top - 0.030, -0.02))
        ear = moved(ellipsoid((0.066, 0.066, 0.016), seg=20, rings=12), turn=(0, -side * 12, -side * 14), at=tuple(centre - root))
        nodes.append(("ear-" + nm, root, [(ear, EAR)]))
        # The little paws under its chin, out of the top of the wisp.
        paw_root = np.array((side * 0.070, lift + 0.010, 0.040))
        paw = ellipsoid((0.030, 0.024, 0.030), at=(side * 0.016, -0.014, 0.040), seg=14, rings=10)
        nodes.append(("paw-" + nm, paw_root, [(paw, BODY)]))
    for name, pivot, pieces in eye_parts(front, 0.062, face_y + 0.004, sneak=True) + mouth_parts(front, face_y - 0.062, sneak=True):
        nodes.append((name, pivot, pieces))
    nodes.append(("nose",) + on_face(front, 0.0, face_y - 0.034, [(ellipsoid((0.013, 0.010, 0.009), seg=12, rings=8), BEAK)]))
    # The cloth over its eyes (its pivot at the eyes' height, so a slide up is a peek) and at its neck.
    hb = (half[0] + .010, .042, half[2] + .010)
    band = rounded(hb, min(hb) * .9, seg=24, rings=10)
    nodes.append(("cloth", np.array((0.0, face_y, 0.0)), [(band, kit.grid_uv(band, inset(R_PROP), 24, "axis", 1))]))
    hn = (half[0] + .016, .036, half[2] + .016)
    neck = rounded(hn, min(hn) * .9, seg=24, rings=10)
    nodes.append(("cloth-neck", np.array((0.0, cy - half[1] + 0.030, 0.0)), [(neck, kit.grid_uv(neck, inset(R_PROP), 24, "axis", 1))]))
    nodes += wisp(w, lift)
    print("PARTS mouse: head centre y %.3f, face y %.3f (x1.35 in game)" % (cy, face_y))
    export("nemu_petghost_mouse_parts", c, nodes)


def bear():
    # The approved lineup's light cocoa (owner 2026-10-29: "isnt the bear a more lighter brown?"), as the one-piece bear.
    c = dict(simple.BEARS["cocoa"])
    w, h, d, lift = 0.32, 0.23, 0.26, 0.15
    front, cy, half, head = head_block(w, h, d, lift)
    top = cy + half[1]
    face_y = cy + half[1] * .05
    my = face_y - 0.048
    mz = front(0.0, my)[0][2]
    hp = np.array((0.0, cy, 0.0))
    # The muzzle stays IN the head (it never moves on its own); its two pale lobes and ink nose, as the approved bear.
    pieces = [head]
    turn = front(0.0, my)[1]
    for s in (-1, 1):
        lobe = moved(ellipsoid((0.036, 0.027, 0.026), seg=20, rings=12), turn=turn, at=(s * 0.029, my - cy, mz + 0.004))
        pieces.append((lobe, PATCH))
    pieces.append((ellipsoid((0.020, 0.013, 0.012), at=(0.0, my + 0.022 - cy, mz + 0.024), seg=16, rings=10), INK))
    nodes = [("head", hp, pieces)]
    for side, nm in ((-1, "l"), (1, "r")):
        centre = np.array((side * (half[0] - 0.030), top + 0.004, -0.01)); root = np.array((side * (half[0] - 0.045), top - 0.025, -0.01))
        ear = moved(ellipsoid((0.046, 0.044, 0.024), seg=18, rings=12), turn=(0, 0, -side * 18), at=tuple(centre - root))
        nodes.append(("ear-" + nm, root, [(ear, BODY)]))
    for name, pivot, p in eye_parts(front, 0.064, face_y + 0.022, sneak=False):
        nodes.append((name, pivot, p))
    # The bear's mouth sits just under its muzzle lobes.
    for name, pivot, p in mouth_parts(lambda x, y: (front(x, y)[0][:2] + (front(x, y)[0][2] + 0.026,), front(x, y)[1]), my - 0.030, sneak=False):
        nodes.append((name, pivot, p))
    nodes += wisp(w, lift)
    print("PARTS bear: head centre y %.3f, face y %.3f (x1.35 in game)" % (cy, face_y))
    export("nemu_petghost_bear_parts", c, nodes)


if __name__ == "__main__":
    mouse()
    bear()
