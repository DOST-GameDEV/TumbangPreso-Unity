"""MAGMA FIST's fist of FIRE: the silhouette its flame shells are drawn on.

    python tools/build_sean_magma_fist.py [--out=<folder>] [--measure]

WHY (owner 2026-10-29). Of the painted pass: "they look a bit cartoony to me", "rework the fist tbh, polish it"; then, of a
crusted opaque fist: "no, i was thinking a more fieryy design, like its not a solid opaque fist, but u can make it out to be
a fist still". So the fist is FIRE in the shape of a fist, and this model is only its SHAPE: `SeanFireFist.shader` draws it
three times, a hot core and two flame shells pushed out along its normals, painted with the bench's flame masks scrolling up
and torn by the cloud map. Nothing of this mesh is ever seen as a surface; only its silhouette and its normals matter.

SO IT IS ONE SMOOTH CLOSED SHAPE THAT READS AS A FIST AT A GLANCE, nothing more: a wrist rising out of the pool, the fist
block (v5's rows, `build_sean_props.FIST_ROWS`, the same size and forward pitch, so `SeanMagmaFist.GiantHeight` and
`GiantScale` still hold), four knuckles standing up off its top edge as one ridge of bumps (the read from every side), and a
chunky thumb across the front. No drawn fingers, no drips, no collar: the shader paints the finger lines into the core, the
pool is the collar, and flame does the rest.

ONE NODE, ONE MESH: the parts are typed in v5's two frames (the wrist's, and the fist's 0.3 m up it, tipped 26 degrees) and
then laid into one node, so the shader's object space runs from the pool (y 0) to the knuckles (about 1.06 m) for every part,
which is the space its flames scroll up.
Output: Assets/TumbangPreso/Resources/Models/SeanProps/giant_fist_flame.glb (or `--out`), through `build_dante_props.write`.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_sean_props as sp  # noqa: E402  (the fist's rows and helpers)

bp, dp, Node = sp.bp, sp.dp, sp.Node
L, R = sp.L, sp.R
TOP = sp.FIST_ROWS[-1][0]
FIST_UP = .3


def giant_fist_flame():
    root = Node("giant_fist")
    flame = Node("flame", parent="giant_fist")

    # The wrist, short and thick, rising out of the pool (its foot is under the court until the fist has risen).
    bp.loft(flame, [(-.1, 0, 0, .26, .22), (.1, 0, 0, .235, .2), (.24, 0, .01, .215, .18), (.34, 0, .02, .205, .17)],
            sp.CORE, sides=18)

    fist = Node("_fist")
    # The block, whole (v5's rows): the fist's mass.
    sp.band(fist, 0, TOP, sp.CORE)
    # The knuckles: four bumps standing up off the top edge, each its own girth and rise, wide enough to touch, so the top of
    # the silhouette is a ridge of four from any side (the read that says "fist" before anything else does).
    for x, r, rise in [(L * .222, .086, .0), (L * .074, .092, .014), (R * .074, .09, .007), (R * .22, .082, -.01)]:
        sp.lump(fist, (x, .69 + rise, .08), (r, .075, .12), sp.CORE, sides=12)
    # The thumb: chunky, lying up across the front from the palm's side toward the index (v5's path), standing well proud so
    # the silhouette shows it from the side.
    thumb = bp.spline([sp.on_fist(L * .33, .12, lift=.04), sp.on_fist(L * .29, .2, lift=.07), sp.on_fist(L * .17, .26, lift=.085),
                       sp.on_fist(L * .05, .29, lift=.08)], per=3)
    fist.tube(thumb, bp.spread([.085, .095, .095, .08], per=3), sp.CORE, sides=10)
    sp.lump(fist, sp.on_fist(L * .045, .29, lift=.09), (.07, .062, .058), sp.CORE, sides=10)
    sp.pitched(fist, 26)
    # Laid into the one node, up the wrist.
    for slot, faces in fist.parts:
        flame.parts.append((slot, [[(p[0], p[1] + FIST_UP, p[2]) for p in face] for face in faces]))
    return [root, flame]


def main(argv):
    out = next((a.split("=", 1)[1] for a in argv if a.startswith("--out=")), sp.OUT)
    nodes = giant_fist_flame()
    if "--measure" in argv:
        print("giant_fist_flame", sp.measure(nodes))
        return
    os.makedirs(out, exist_ok=True)
    dp.write(os.path.join(out, "giant_fist_flame.glb"), nodes)
    print("  bounds", sp.measure(nodes))


if __name__ == "__main__":
    main(sys.argv[1:])
