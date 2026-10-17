"""Rafi's shark for his ultimate's cutscene (BAHA): a WHALE SHARK (a butanding), sculpted as ONE SKIN and painted.

  py -3 tools/build_ultimate_shark.py [--out=<folder>]

It runs in two halves. Blender (found at BLENDER below, run by this script) FUSES the typed forms into one continuous
skin: the body, its back fins and its side fins are joined by a voxel remesh and smoothed, and the mouth is cut into it,
so a fin grows out of the body as a fin does and nothing is a shape set against another shape. Then this script, in the
system's Python, lays the paint's UVs on that skin, paints the atlas and writes the model.

The owner's notes, in order (2026-10-10):
  * of a faceted shark in flat palette colours with a hinged jaw: "shark model is not at all good, jaw is messed up,
    doesn't fit the hand painted illustrated stylistic art design of our characters and overall game style ... need you
    to read up on how we modeled the fpv hand pets (currently hidden) and the character model redesign docs";
  * of a round painted one built from the hand companion kit's balls and leaves, with big eyes and pink cheeks: "shark
    could probably be less bubbly, also maybe it should be a whale shark instead", then "it should be more fierce
    looking, and preferably not made with basic object shapes just put together without proper connection";
  * of a fused whale shark built from memory (a cone of a head, a small frown, big eyes under sculpted brows, the back
    fin too far forward): "that's a really bad whale shark model. i need you to research what a whaleshark looks like
    and give me references for you to follow and analyze".
  * and, asked how it should be fierce: through ATTITUDE, with its TRUE FACE. The real head, tiny eyes and wide straight
    mouth; the menace from its size, a dark mouth held open, and darker paint round the eye. No brow, no angry eyes.

⚠️ SO THIS ONE IS MEASURED, NOT REMEMBERED. Every number below is a share of its whole length read off photographs (a clean
side profile, a head-on, a museum model from above, two with the mouth open), `F` being how far along from the nose:
  * the HEAD is a wide flat wedge, a spade: about twice as wide as it is tall from the front, and the widest part of the
    animal. Its front edge is blunt and nearly square in plan; its underside is flat.
  * the MOUTH is a straight slot across that front edge, most of its width. Not underneath, not curved.
  * the EYES are tiny and low on the sides, just behind the corners of the mouth.
  * the body is deepest just behind the head (F 0.22 to 0.30, about 16%) and tapers steadily to a stalk about 3% deep.
  * THREE RIDGES run down each flank from behind the head; the lowest becomes the keel on the stalk.
  * the first back fin stands FAR back (F 0.51 to 0.66), a rounded triangle a tenth of the length tall; the second is
    much smaller (F 0.75 to 0.82).
  * the side fins are broad wings a fifth of the length long, from just behind the gills (F 0.20), swept back and down.
  * the tail is a tall crescent, about 30% tip to tip, its upper lobe clearly the longer.
  * five long gill slits each side, above and just ahead of the side fins.
  * grey-blue above, not bright blue; white below from the lower lip back. Small close spots on the head; on the body
    larger spots between pale upright bars, a chequer; the fins spotted too.
So the body is not a turned shape squashed: it is LOFTED from typed cross-sections (how wide, how high above the middle,
how deep below it, how square), which is what makes the head a spade and puts the ridges in the skin itself.

THE PARTS (`HeroIntroductionScene.Rafi.cs` finds them by name):
  body      the one skin: body, back fins, side fins, the small fins under it, with the mouth cut in. Nose to +z, back +y.
            The eyes ride on it. The inside of the cut is painted dark: the mouth is HELD OPEN, it is not a jaw.
  mouth     the pale pad on the floor of the mouth. The game may scale it a little.
  tail      the stem, its keels and both lobes, one skin, pivot at the root of the tail: turn about y to wag.

3.47 m nose to tail tip. Metres, y up.
"""
import math
import subprocess
import sys
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import ellipsoid, moved, rounded, tube  # noqa: E402

BLENDER = r"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe"
try:
    import bpy
    INSIDE_BLENDER = True
except ImportError:
    INSIDE_BLENDER = False


def arg(name, default):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


def smooth01(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


# ------------------------------------------------------------------ the body's cross-sections (typed)

NOSE, LENGTH = 1.45, 3.40            # where the nose is along z, and what "1.0 of its length" is in metres
F_END = 0.89                         # the body's skin ends at the root of the tail


def z_of(f):
    return NOSE - LENGTH * f


# How far along; half its width; how high its back is over the middle line; how deep its belly is under it; and how
# SQUARE the section is (2 an ellipse, 3 a box with round corners). Read off the photographs, one station at a time.
STATIONS = [
    # F      wide    top     under   square
    (0.000, 0.300, 0.065, 0.055, 3.0),     # the front edge: blunt, most of the head's width already
    (0.008, 0.325, 0.095, 0.078, 3.0),
    (0.020, 0.345, 0.118, 0.095, 3.0),
    (0.045, 0.368, 0.150, 0.115, 2.9),
    (0.090, 0.385, 0.195, 0.148, 2.8),     # the eyes are just behind here
    (0.150, 0.388, 0.262, 0.200, 2.6),     # the widest of it: the back of the head
    (0.220, 0.372, 0.325, 0.250, 2.4),
    (0.300, 0.342, 0.345, 0.275, 2.3),     # the deepest of it
    (0.400, 0.298, 0.325, 0.256, 2.2),
    (0.500, 0.246, 0.282, 0.214, 2.2),
    (0.600, 0.192, 0.226, 0.164, 2.1),
    (0.700, 0.140, 0.162, 0.108, 2.1),
    (0.780, 0.102, 0.110, 0.070, 2.0),
    (0.840, 0.080, 0.076, 0.052, 2.0),
    (0.890, 0.068, 0.056, 0.046, 2.0),     # the stalk, where the tail's own skin takes over
]
# The three ridges down each flank, as how far round the section they sit (degrees up from the side) at the head end
# and at the tail end. The lowest comes down to the side itself and is the keel of the stalk.
RIDGES = [(64.0, 58.0, 0.060), (38.0, 30.0, 0.070), (13.0, 0.0, 0.085)]
RIDGE_WIDE = 6.5                     # degrees, each side of a ridge's top


def section_numbers(f):
    f = np.asarray(f, np.float64)
    table = np.array(STATIONS, np.float64)
    return [np.interp(f, table[:, 0], table[:, k]) for k in (1, 2, 3, 4)]


def section(f, phi):
    """The skin's x and y at `f` along, `phi` radians round (0 the +x side, a quarter turn the top of the back)."""
    wide, top, under, square = section_numbers(f)
    c, s = np.cos(phi), np.sin(phi)
    power = 2.0 / square
    x = wide * np.sign(c) * np.abs(c) ** power
    y = np.where(s >= 0, top, under) * np.sign(s) * np.abs(s) ** power
    # The ridges: raised lines in the skin itself, from behind the head to the tail.
    there = smooth01(0.17, 0.26, f)
    up = np.degrees(np.arcsin(np.clip(s, -1, 1)))               # degrees up from the side, the same on both flanks
    lift = np.zeros_like(x)
    for at_head, at_tail, high in RIDGES:
        at = at_head + (at_tail - at_head) * smooth01(0.2, 0.89, f)
        lift = lift + high * np.exp(-((up - at) / RIDGE_WIDE) ** 2)
    lift = lift * there * (1 + 1.2 * smooth01(0.6, 0.89, f))    # stronger on the stalk, where the lowest is the keel
    return x * (1 + lift), y * (1 + lift * 0.6)


def body_loft(rings=96, seg=72):
    fs = np.concatenate([np.linspace(0, 0.05, 12, endpoint=False), np.linspace(0.05, F_END, rings - 12)])
    phi = np.linspace(0, 2 * np.pi, seg, endpoint=False)
    pts = [(0.0, 0.0, z_of(0.0) + 0.004)]
    for f in fs:
        x, y = section(np.full(seg, f), phi)
        pts += [(float(a), float(b), z_of(f)) for a, b in zip(x, y)]
    pts.append((0.0, 0.0, z_of(F_END) - 0.02))
    tris = []
    for s in range(seg):
        tris.append((0, 1 + (s + 1) % seg, 1 + s))
    for r in range(len(fs) - 1):
        for s in range(seg):
            a, b = 1 + r * seg + s, 1 + r * seg + (s + 1) % seg
            tris.append((a, b, a + seg)); tris.append((b, b + seg, a + seg))
    last, end = 1 + (len(fs) - 1) * seg, len(pts) - 1
    for s in range(seg):
        tris.append((end, last + s, last + (s + 1) % seg))
    p = np.array(pts, np.float32)
    return p, np.zeros_like(p), np.array(tris, np.uint32)


def blade(stations, seg=18):
    """A fin as a form to be fused, typed station by station along its SPAN (+y): how far out, where its leading edge
    is along z, where its trailing edge is, and how thick. Each station is a lens; the first is set inside the body so
    the fusing blends the root in."""
    pts, tris = [], []
    for span, lead, trail, thick in stations:
        mid, half = (lead + trail) * .5, (lead - trail) * .5
        for k in range(seg):
            u = 2 * math.pi * k / seg
            pts.append((thick * .5 * math.sin(u), span, mid + half * math.cos(u)))
    count = len(stations)
    for r in range(count - 1):
        for s in range(seg):
            a, b = r * seg + s, r * seg + (s + 1) % seg
            tris.append((a, a + seg, b)); tris.append((b, a + seg, b + seg))
    first = len(pts)
    pts.append((0.0, stations[0][0], (stations[0][1] + stations[0][2]) * .5))
    pts.append((0.0, stations[-1][0] + stations[-1][3] * .4, (stations[-1][1] + stations[-1][2]) * .5))
    for s in range(seg):
        tris.append((first, s, (s + 1) % seg))
        tris.append((first + 1, (count - 1) * seg + (s + 1) % seg, (count - 1) * seg + s))
    p = np.array(pts, np.float32)
    return p, np.zeros_like(p), np.array(tris, np.uint32)


# The first back fin: FAR back, a rounded triangle, its tip swept toward the tail.
BACK_FIN = [(0.110, -0.250, -0.800, 0.100), (0.235, -0.290, -0.810, 0.072), (0.330, -0.380, -0.830, 0.056), (0.430, -0.490, -0.860, 0.046),
            (0.500, -0.600, -0.885, 0.038), (0.535, -0.700, -0.880, 0.030)]
# The second: the same shape a third the size, just before the tail.
SECOND_FIN = [(0.040, -1.090, -1.330, 0.052), (0.118, -1.110, -1.340, 0.040), (0.180, -1.180, -1.362, 0.032), (0.220, -1.255, -1.380, 0.028)]
# A side fin, as built: its root at z = 0, its span along +y. A broad wing, a fifth of the animal long, swept back.
SIDE_FIN = [(-0.100, 0.030, -0.370, 0.090), (0.050, 0.010, -0.385, 0.076), (0.250, -0.080, -0.440, 0.060), (0.460, -0.215, -0.520, 0.050),
            (0.630, -0.365, -0.600, 0.042), (0.760, -0.515, -0.670, 0.035), (0.815, -0.615, -0.705, 0.030)]
# The small fins under it: a pair below the first back fin, and one below the second.
BELLY_FIN = [(-0.040, 0.020, -0.200, 0.050), (0.050, 0.000, -0.210, 0.040), (0.130, -0.070, -0.235, 0.032), (0.170, -0.140, -0.250, 0.028)]
UNDER_FIN = [(0.020, -1.130, -1.330, 0.044), (0.090, -1.160, -1.340, 0.034), (0.145, -1.225, -1.360, 0.028)]
SIDE_FIN_AT, SIDE_FIN_DROOP = (0.285, -0.115, 0.800), 24.0
BELLY_FIN_AT = (0.105, -0.120, -0.470)


def body_forms():
    """Everything that becomes the body's one skin, as separate closed forms that overlap."""
    forms = [body_loft(), blade(BACK_FIN), blade(SECOND_FIN)]
    # The right side fin is typed; the left is its mirror (turned the other way about z, which is the same thing for a
    # form that is the same on both of its faces).
    for side in (1, -1):
        x, y, z = SIDE_FIN_AT
        forms.append(moved(blade(SIDE_FIN), turn=(0, 0, side * -(90 + SIDE_FIN_DROOP)), at=(side * x, y, z)))
        x, y, z = BELLY_FIN_AT
        forms.append(moved(blade(BELLY_FIN), turn=(0, 0, side * -150), at=(side * x, y, z)))
    forms.append(moved(blade(UNDER_FIN), turn=(0, 0, 180)))
    return forms


TAIL_AT = (0.0, 0.0, z_of(F_END) + 0.02)
# The tail's lobes about its own root: a tall crescent, the upper lobe the longer, both raked hard back.
UPPER_LOBE = [(-0.030, 0.050, -0.200, 0.090), (0.110, -0.040, -0.300, 0.070), (0.270, -0.170, -0.400, 0.055), (0.430, -0.310, -0.490, 0.045),
              (0.570, -0.440, -0.565, 0.036), (0.660, -0.535, -0.610, 0.030)]
LOWER_LOBE = [(-0.030, 0.050, -0.200, 0.086), (0.090, -0.030, -0.270, 0.065), (0.210, -0.120, -0.335, 0.050), (0.320, -0.220, -0.385, 0.038),
              (0.395, -0.300, -0.410, 0.030)]


def tail_forms():
    """The tail, about its own root: a stem that starts inside the body, the keel either side of it, and the crescent."""
    forms = [tube([(0.0, 0.0, 0.15), (0.0, 0.0, 0.0), (0.0, 0.0, -0.10), (0.0, 0.0, -0.17)], [0.050, 0.052, 0.050, 0.046], seg=14)]
    forms.append(ellipsoid((0.086, 0.022, 0.150), at=(0.0, 0.0, 0.02), seg=14, rings=8))
    forms.append(blade(UPPER_LOBE))
    forms.append(moved(blade(LOWER_LOBE), turn=(0, 0, 180)))
    return forms


# What is taken out of the front of the head: a straight slot most of its width, held open, running well back.
MOUTH_HALF, MOUTH_AT = (0.262, 0.046, 0.210), (0.0, -0.004, NOSE - 0.130)


def mouth_cutter():
    return rounded(MOUTH_HALF, 0.042, at=MOUTH_AT, seg=24, rings=12)


# ------------------------------------------------------------------ Blender: fuse

if INSIDE_BLENDER:

    def to_object(name, mesh):
        p, _, t = mesh
        me = bpy.data.meshes.new(name)
        me.from_pydata([tuple(float(v) for v in row) for row in p], [], [tuple(int(v) for v in row) for row in t])
        me.update()
        ob = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(ob)
        return ob

    def apply(ob, kind, **settings):
        bpy.context.view_layer.objects.active = ob
        for o in bpy.context.scene.objects:
            o.select_set(o is ob)
        m = ob.modifiers.new(kind, kind)
        for key, value in settings.items():
            setattr(m, key, value)
        bpy.ops.object.modifier_apply(modifier=m.name)

    def fused(name, forms, voxel, cutter=None, keep=0.5, smooth=12):
        obs = [to_object("%s-%d" % (name, i), f) for i, f in enumerate(forms)]
        for o in bpy.context.scene.objects:
            o.select_set(o in obs)
        bpy.context.view_layer.objects.active = obs[0]
        bpy.ops.object.join()
        ob = bpy.context.view_layer.objects.active
        apply(ob, "REMESH", mode="VOXEL", voxel_size=voxel, adaptivity=0.0)
        # Smoothed BEFORE the mouth is cut, so the fins' roots melt into the body and the lips stay crisp.
        apply(ob, "SMOOTH", factor=0.5, iterations=smooth)
        if cutter is not None:
            cut = to_object(name + "-cut", cutter)
            apply(ob, "BOOLEAN", operation="DIFFERENCE", object=cut, solver="EXACT")
            bpy.data.objects.remove(cut)
            apply(ob, "REMESH", mode="VOXEL", voxel_size=voxel, adaptivity=0.0)
            apply(ob, "SMOOTH", factor=0.5, iterations=2)
        apply(ob, "DECIMATE", ratio=keep)
        apply(ob, "TRIANGULATE")
        me = ob.data
        for poly in me.polygons:
            poly.use_smooth = True
        me.update()
        pos = np.array([v.co[:] for v in me.vertices], np.float32)
        nrm = np.array([v.normal[:] for v in me.vertices], np.float32)
        tri = np.array([p.vertices[:] for p in me.polygons], np.uint32)
        bpy.data.objects.remove(ob)
        return pos, nrm, tri

    bpy.ops.wm.read_factory_settings(use_empty=True)
    out = arg("fuse", None)
    b = fused("body", body_forms(), 0.012, cutter=mouth_cutter(), keep=0.16)
    t = fused("tail", tail_forms(), 0.012, keep=0.34)
    np.savez(out, bp=b[0], bn=b[1], bt=b[2], tp=t[0], tn=t[1], tt=t[2])
    print("fused: body %d triangles, tail %d triangles" % (len(b[2]), len(t[2])))
    raise SystemExit


# ------------------------------------------------------------------ the paint

# The flat cells (the eyes and the inside of the mouth). The same list, in the same order, is `UltimateProp.SharkPalette`.
PALETTE = [
    "#10151C",  # 0  eye
    "#FFFFFF",  # 1  glint
    "#70767E",  # 2  the pad on the floor of the mouth
    "#161016",  # 3  inside the mouth
    "#3A2630",  # 4  (spare)
    "#4B5F6E", "#35434F", "#F1EDE2", "#5C7181", "#28333D", "#E6EBEA", "#D5E0E0", "#10151C", "#FFFFFF", "#70767E", "#161016",
]
INK, WHITE, PAD_GREY, GULLET, SPARE = range(5)

BACK, FLANK, DEEP, BELLY, PALE = "#36444F", "#4D6170", "#27323C", "#F1EDE2", "#E3ECE8"

ATLAS = 2048
PAD = 0.004
# The paint lives in the atlas's upper half (v under 0.5): the body's skin round its long axis; the tail seen from the
# side; the back fins seen from the side; the side fins seen from above; and the pale undersides of the fins.
R_BODY, R_TAIL = (0.0, 0.0, 0.62, 0.5), (0.62, 0.0, 0.80, 0.5)
R_BACK_FINS, R_SIDE_FINS, R_UNDER = (0.80, 0.0, 1.0, 0.20), (0.80, 0.20, 1.0, 0.40), (0.80, 0.40, 1.0, 0.5)
# What each flat projection spans, in metres (the tail's are about its own root).
BACK_FINS_Z, BACK_FINS_Y = (-1.46, -0.20), (0.0, 0.62)
SIDE_FINS_X, SIDE_FINS_Z = (0.20, 1.16), (0.02, 0.90)
TAIL_Z, TAIL_Y = (-0.66, 0.18), (-0.46, 0.72)
# A point this far outside the body's own typed section is a fin's, and is painted as a fin.
FIN_OUT = 0.034


def inset(rect):
    return (rect[0] + PAD, rect[1] + PAD, rect[2] - PAD, rect[3] - PAD)


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def round_of(x, y):
    """How far round the long axis a point is, 0 to 1: 0 and 1 straight under the belly, 0.25 the +x flank, 0.5 the top
    of the back, 0.75 the -x flank. The body's paint is laid left to right by this."""
    return (np.arctan2(x, -y) / (2 * np.pi)) % 1.0


SECTION_ROWS, SECTION_COLS = 240, 720


def section_radius_table():
    """How far the typed skin is from the long axis, by how far along and how far round: to tell a fin from the body."""
    table = np.zeros((SECTION_ROWS, SECTION_COLS), np.float32)
    phi = np.linspace(0, 2 * np.pi, 2880, endpoint=False)
    for i in range(SECTION_ROWS):
        f = F_END * i / (SECTION_ROWS - 1)
        x, y = section(np.full(phi.shape, f), phi)
        u = round_of(x, y)
        order = np.argsort(u)
        table[i] = np.interp((np.arange(SECTION_COLS) + .5) / SECTION_COLS, u[order], np.hypot(x, y)[order], period=1.0)
    return table


def planar(a, b, a_range, b_range, rect):
    r = inset(rect)
    u = r[0] + (r[2] - r[0]) * np.clip((a - a_range[0]) / (a_range[1] - a_range[0]), 0, 1)
    v = r[3] - (r[3] - r[1]) * np.clip((b - b_range[0]) / (b_range[1] - b_range[0]), 0, 1)
    return np.stack([u, v], axis=1).astype(np.float32)


def body_uv(mesh):
    """UVs for the fused body. The BODY's own skin is laid round its long axis (see `round_of`) and nose to tail top
    to bottom. A FIN is not: paint laid round the body is drawn out into streaks along a fin, so each fin is laid flat
    as it is seen (the back fins from the side, the side fins from above, their undersides apart). And the inside of
    the mouth is one flat dark cell. Returns a new mesh with its own points per triangle corner."""
    pos, nrm, tri = mesh
    corner = tri.reshape(-1)
    p, n = pos[corner].astype(np.float32), nrm[corner].astype(np.float32)
    f = np.clip((NOSE - p[:, 2]) / LENGTH, 0, F_END)
    u = round_of(p[:, 0], p[:, 1])
    table = section_radius_table()
    row = np.clip((f / F_END * (SECTION_ROWS - 1)).round().astype(int), 0, SECTION_ROWS - 1)
    col = np.clip((u * SECTION_COLS).astype(int), 0, SECTION_COLS - 1)
    out = np.hypot(p[:, 0], p[:, 1]) - table[row, col]

    cu = u.reshape(-1, 3).copy()
    wide = (cu.max(axis=1) - cu.min(axis=1)) > 0.5
    side = np.where(cu[wide].mean(axis=1, keepdims=True) > 0.5, 0.999, 0.001)
    cu[wide] = np.repeat(side, 3, axis=1)
    r = inset(R_BODY)
    uv = np.stack([r[0] + (r[2] - r[0]) * cu.reshape(-1), r[1] + (r[3] - r[1]) * (f / F_END)], axis=1).astype(np.float32)

    corners = p.reshape(-1, 3, 3)
    mid = corners.mean(axis=1)
    face = np.cross(corners[:, 1] - corners[:, 0], corners[:, 2] - corners[:, 0])
    face /= np.maximum(np.linalg.norm(face, axis=1, keepdims=True), 1e-9)
    fin = out.reshape(-1, 3).mean(axis=1) > FIN_OUT
    back_fin = fin & (mid[:, 1] > 0.05) & (np.abs(mid[:, 0]) < 0.14) & (mid[:, 2] < -0.15)
    side_fin = fin & (mid[:, 2] > 0.05) & (np.abs(mid[:, 0]) > 0.24)
    side_top = side_fin & (face[:, 1] > -0.40)
    under = fin & ~back_fin & ~side_top

    def three(mask):
        return np.repeat(mask, 3)

    uv[three(back_fin)] = planar(p[:, 2], p[:, 1], BACK_FINS_Z, BACK_FINS_Y, R_BACK_FINS)[three(back_fin)]
    uv[three(side_top)] = planar(np.abs(p[:, 0]), p[:, 2], SIDE_FINS_X, SIDE_FINS_Z, R_SIDE_FINS)[three(side_top)]
    uv[three(under)] = planar(np.abs(p[:, 0]), p[:, 2], (0.0, 1.16), (-1.46, 0.90), R_UNDER)[three(under)]
    # The inside of the mouth: whatever of the skin lies in the cut, behind the lips.
    half, at = np.array(MOUTH_HALF), np.array(MOUTH_AT)
    inside = (np.abs(mid[:, 0]) < half[0] + 0.006) & (np.abs(mid[:, 1] - at[1]) < half[1] + 0.010) & (mid[:, 2] > at[2] - half[2] - 0.03)
    inward = -np.sign(mid[:, 1] - at[1]) * face[:, 1]
    gullet = inside & ((mid[:, 2] < NOSE - 0.034) | (inward > 0.55))
    uv[three(gullet)] = np.array(kit.cell(GULLET), np.float32)
    counts = (int(back_fin.sum()), int(side_top.sum()), int(under.sum()), int(gullet.sum()))
    print("painted as back fins %d, side fins %d, undersides %d, mouth %d triangles" % counts)
    return (p, n, np.arange(len(corner), dtype=np.uint32).reshape(-1, 3)), uv


def tail_uv(mesh):
    pos, nrm, tri = mesh
    corner = tri.reshape(-1)
    p, n = pos[corner].astype(np.float32), nrm[corner].astype(np.float32)
    return (p, n, np.arange(len(corner), dtype=np.uint32).reshape(-1, 3)), planar(p[:, 2], p[:, 1], TAIL_Z, TAIL_Y, R_TAIL)


# The spots are placed by a typed table of nudges, taken in turn, so no two neighbours sit square to each other.
NUDGE = [(0.30, -0.20), (-0.25, 0.35), (0.10, 0.15), (-0.35, -0.30), (0.40, 0.05), (-0.10, -0.40), (0.20, 0.40), (-0.40, 0.10),
         (0.05, -0.15), (0.35, 0.30), (-0.20, -0.05)]
# Rows of spots by how far ACROSS the back they sit: 1 the +x edge of the animal seen from above, 0 the middle of its
# back, -1 the other edge. (By degrees round the section they bunched at the edges of the flat head and left its top bare.)
HEAD_ROWS = [0.995, 0.93, 0.83, 0.725, 0.62, 0.515, 0.41, 0.31, 0.205, 0.10, 0.0, -0.10, -0.205, -0.31, -0.41, -0.515, -0.62, -0.725,
             -0.83, -0.93, -0.995]
BODY_ROWS = [0.995, 0.95, 0.83, 0.66, 0.44, 0.16, -0.16, -0.44, -0.66, -0.83, -0.95, -0.995]
BODY_ROWS_BETWEEN = [0.98, 0.90, 0.75, 0.55, 0.30, 0.0, -0.30, -0.55, -0.75, -0.90, -0.98]
HEAD_FROM, HEAD_TO, HEAD_STEP = 0.012, 0.176, 0.0135
BARS_FROM, BARS_TO, BAR_STEP = 0.200, 0.870, 0.0335
GILLS = [0.148, 0.163, 0.178, 0.193, 0.208]
EYE_F, EYE_UP = 0.072, 2.0           # the eye: how far along, and degrees up from the side


def paint_atlas(path):
    from PIL import Image, ImageDraw, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc(BACK)

    def box(rect):
        return int(rect[0] * ATLAS), int(rect[1] * ATLAS), int(rect[2] * ATLAS), int(rect[3] * ATLAS)

    def mix(base, colour, weight):
        return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]

    # ---- the body's skin, texel by texel from the typed section: how high each texel is on the real animal decides
    # where the white belly starts, so its edge is a level line seen from the side and meets the lower lip at the front.
    x0, y0, x1, y1 = box(R_BODY)
    w, h = x1 - x0, y1 - y0
    us = (np.arange(w) + .5) / w
    phi = np.linspace(-np.pi / 2, 3 * np.pi / 2, 2880, endpoint=False)
    c = np.zeros((h, w, 3), np.float32)
    for row in range(h):
        f = F_END * (row + .5) / h
        sx, sy = section(np.full(phi.shape, f), phi)
        su = round_of(sx, sy)
        order = np.argsort(su)
        ty = np.interp(us, su[order], sy[order], period=1.0)
        _, top, under, _ = section_numbers(f)
        high = np.clip(ty / top, -1, 1)
        line = hexc(FLANK) + np.zeros((w, 3), np.float32)
        line = mix(line, BACK, smooth01(0.30, 0.95, high) * 0.90)
        line = mix(line, DEEP, np.full(w, float(smooth01(0.70, 0.89, f)) * 0.5))
        # The white begins a third of the way down the underside at the head (the lower lip) and lower toward the tail.
        edge = -under * (0.30 + 0.40 * float(smooth01(0.08, 0.40, f)) + 0.20 * float(smooth01(0.60, 0.89, f)))
        belly = smooth01(edge + 0.012, edge - 0.012, ty)
        line = mix(line, BELLY, belly)
        line = mix(line, "#C7D2D4", np.exp(-((ty - edge + 0.020) / 0.016) ** 2) * 0.35)      # a soft grey just under the edge
        c[row] = line
    img[y0:y1, x0:x1] = c

    # ---- the fins and the tail: the back's colour, deeper to their tips; the undersides pale.
    def ground(rect, colour, tip, tip_axis):
        bx0, by0, bx1, by1 = box(rect)
        g = np.zeros((by1 - by0, bx1 - bx0, 3), np.float32) + hexc(colour)
        ramp = np.linspace(0, 1, by1 - by0)[::-1, None] if tip_axis == "up" else np.linspace(0, 1, bx1 - bx0)[None, :]
        g = mix(g, tip, np.broadcast_to(smooth01(0.35, 1.0, ramp) * 0.65, g.shape[:2]))
        img[by0:by1, bx0:bx1] = g
        return bx0, by0, bx1, by1

    tail_box = ground(R_TAIL, BACK, DEEP, "up")
    back_box = ground(R_BACK_FINS, BACK, DEEP, "up")
    side_box = ground(R_SIDE_FINS, BACK, DEEP, "across")
    ground(R_UNDER, "#C4CCCE", "#7F8B94", "across")

    picture = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(picture, "RGBA")
    pale = tuple(int(v) for v in hexc(PALE))

    def at(f, up):
        """Where on the body's part of the atlas a place on the skin is, and how many pixels a metre is there each way."""
        ph = math.radians(up)
        sx, sy = section(np.array([f]), np.array([ph]))
        u = float(round_of(sx, sy)[0])
        reach = max(0.05, float(np.hypot(sx, sy)[0]))
        return x0 + u * w, y0 + (f / F_END) * h, w / (2 * math.pi * reach), h / (F_END * LENGTH)

    def spot(f, across, size, alpha=235):
        square = float(section_numbers(f)[3])
        up = math.degrees(math.acos(max(-1.0, min(1.0, math.copysign(abs(across) ** (square / 2.0), across)))))
        # The two outermost rows sit just under the edge, on the side of the head.
        up = -7.0 if across > 0.99 else 187.0 if across < -0.99 else up
        cx, cy, px, py = at(f, up)
        d.ellipse([cx - size * px, cy - size * py, cx + size * px, cy + size * py], fill=pale + (alpha,))

    # The head: small spots, close, in rows; every other row set half a step along.
    count = 0
    f = HEAD_FROM
    while f < HEAD_TO:
        for k, up in enumerate(HEAD_ROWS):
            nx, ny = NUDGE[count % len(NUDGE)]
            spot(f + (HEAD_STEP * .5 if k % 2 else 0) + ny * 0.004, up + nx * 0.03, 0.0085 + 0.0025 * ((count * 7) % 3) / 2)
            count += 1
        f += HEAD_STEP
    # The body: a chequer. Pale upright BARS, and a spot in every square between them; then a second, smaller set on
    # the bars themselves, between the rows.
    f = BARS_FROM
    bar = 0
    while f < BARS_TO:
        thin = 1 - 0.45 * float(smooth01(0.55, 0.87, f))
        points = [at(f + 0.003 * math.sin(i * 1.7 + bar), 2 + i * 8.0)[:2] for i in range(23)]
        d.line(points, fill=pale + (135,), width=max(3, int(6 * thin)), joint="curve")
        for k, up in enumerate(BODY_ROWS):
            nx, ny = NUDGE[count % len(NUDGE)]
            spot(f + BAR_STEP * .5 + ny * 0.006, up + nx * 0.03, (0.0165 + 0.004 * ((count * 5) % 3) / 2) * thin)
            count += 1
        for k, up in enumerate(BODY_ROWS_BETWEEN):
            nx, ny = NUDGE[count % len(NUDGE)]
            spot(f + ny * 0.003, up + nx * 0.02, 0.0095 * thin, 215)
            count += 1
        f += BAR_STEP
        bar += 1
    # The ridges: a pale line along the top of each and a dark one under it, so they read as keels from any side.
    for at_head, at_tail, _ in RIDGES:
        for flank in (0, 1):
            upper, lower = [], []
            for i in range(41):
                f = 0.20 + (0.885 - 0.20) * i / 40
                up = at_head + (at_tail - at_head) * float(smooth01(0.2, 0.89, f))
                up_a, up_b = (up + 2.5, up - 5.0) if flank == 0 else (180 - up - 2.5, 180 - up + 5.0)
                upper.append(at(f, up_a)[:2]); lower.append(at(f, up_b)[:2])
            d.line(lower, fill=(22, 30, 38, 150), width=7, joint="curve")
            d.line(upper, fill=pale + (120,), width=4, joint="curve")
    # Five gill slits each side, drawn: long dark strokes from low on the flank up, leaning back at their tops.
    for flank in (0, 1):
        for k, f in enumerate(GILLS):
            stroke = [at(f + 0.016 * (i / 6.0) ** 1.5, (8 + i * 11.0) if flank == 0 else (172 - i * 11.0))[:2] for i in range(7)]
            d.line(stroke, fill=(16, 22, 28, 235), width=9, joint="curve")
            light = [at(f + 0.005 + 0.016 * (i / 6.0) ** 1.5, (8 + i * 11.0) if flank == 0 else (172 - i * 11.0))[:2] for i in range(7)]
            d.line(light, fill=pale + (70,), width=3, joint="curve")
    # Darker paint round each eye: the only "expression" it is given.
    shade = Image.new("RGBA", picture.size, (0, 0, 0, 0))
    ds = ImageDraw.Draw(shade)
    for up in (EYE_UP, 180 - EYE_UP):
        cx, cy, px, py = at(EYE_F + 0.004, up + (4 if up < 90 else -4))
        ds.ellipse([cx - 0.060 * px, cy - 0.050 * py, cx + 0.060 * px, cy + 0.050 * py], fill=(14, 19, 25, 215))
    shade = shade.filter(ImageFilter.GaussianBlur(9))
    picture.paste(shade, (0, 0), shade)
    d = ImageDraw.Draw(picture, "RGBA")
    # A dark line along the upper lip, so the slot reads as a mouth even where the light is flat.
    lip = [at(0.004, 6 + i * 8.4)[:2] for i in range(21)]
    d.line(lip, fill=(16, 22, 28, 200), width=6, joint="curve")

    # The fins' and the tail's spots: typed rows, each row's spots set off from the last by the same table of nudges.
    def fin_spots(rect_box, rows, size):
        bx0, by0, bx1, by1 = rect_box
        bw, bh = bx1 - bx0, by1 - by0
        k = 0
        for v_at, starts in rows:
            for u_at in starts:
                nx, ny = NUDGE[k % len(NUDGE)]
                cx, cy = bx0 + (u_at + nx * 0.02) * bw, by0 + (1 - v_at + ny * 0.02) * bh
                r = size * (0.8 + 0.2 * (k % 3))
                d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=pale + (228,))
                k += 1

    eight = [0.06, 0.18, 0.30, 0.42, 0.54, 0.66, 0.78, 0.90]
    seven = [0.12, 0.24, 0.36, 0.48, 0.60, 0.72, 0.84]
    fin_spots(tail_box, [(v, eight if i % 2 == 0 else seven) for i, v in enumerate([0.06, 0.13, 0.20, 0.27, 0.34, 0.41, 0.48, 0.55, 0.62, 0.69,
                                                                                     0.76, 0.83, 0.90, 0.96])], 6.0)
    fin_spots(back_box, [(v, eight if i % 2 == 0 else seven) for i, v in enumerate([0.08, 0.20, 0.32, 0.44, 0.56, 0.68, 0.80, 0.92])], 5.0)
    fin_spots(side_box, [(v, eight if i % 2 == 0 else seven) for i, v in enumerate([0.07, 0.17, 0.27, 0.37, 0.47, 0.57, 0.67, 0.77, 0.87,
                                                                                     0.95])], 5.5)
    picture = picture.filter(ImageFilter.GaussianBlur(0.8))
    picture.save(path)
    return path


# ------------------------------------------------------------------ the model

def main():
    folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/UltimateProps"))
    folder.mkdir(parents=True, exist_ok=True)
    fuse = folder / "shark-fused.npz"
    done = subprocess.run([BLENDER, "-b", "--python", str(Path(__file__).resolve()), "--", "--fuse=" + str(fuse)], capture_output=True, text=True)
    print("\n".join(line for line in done.stdout.splitlines() if line.startswith("fused") or "Error" in line or "Traceback" in line))
    if not fuse.exists():
        print(done.stdout[-3000:]); print(done.stderr[-3000:])
        raise SystemExit("Blender did not fuse the shark.")
    data = np.load(fuse)
    skin = (data["bp"], data["bn"], data["bt"])
    tail = (data["tp"], data["tn"], data["tt"])
    data.close()
    fuse.unlink()

    m = kit.Model("shark")
    m.folder = folder
    m.texture = "shark-atlas.png"
    paint_atlas(m.folder / m.texture)

    body = [body_uv(skin)]
    # The eyes: TINY, dark, low on the side of the head just behind the corner of the mouth. Seated on the typed skin.
    sx, sy = section(np.array([EYE_F]), np.array([math.radians(EYE_UP)]))
    out, high = float(sx[0]), float(sy[0])
    for side in (1, -1):
        eye = ellipsoid((0.012, 0.019, 0.021), seg=12, rings=8, at=(side * (out - 0.003), high, z_of(EYE_F)))
        glint = ellipsoid((0.004, 0.005, 0.005), seg=8, rings=5, at=(side * (out + 0.008), high + 0.008, z_of(EYE_F) + 0.008))
        body += [(eye, INK), (glint, WHITE)]
    m.add("body", body)
    # The floor of the mouth: a low pale pad well inside the cut, which is all of the inside that catches the light.
    pad = ellipsoid((MOUTH_HALF[0] * 0.80, 0.012, 0.120), seg=18, rings=6)
    m.add("mouth", [(pad, PAD_GREY)], at=(0.0, MOUTH_AT[1] - MOUTH_HALF[1] + 0.006, NOSE - 0.175))
    m.add("tail", [tail_uv(tail)], at=TAIL_AT)
    m.write(PALETTE)


if __name__ == "__main__":
    main()
