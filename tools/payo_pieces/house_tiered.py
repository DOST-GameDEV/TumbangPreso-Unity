"""PAYO town, building 4: the TIERED-ROOF HOUSE. An eight-sided painted stone base (the Kalinga octagon), three
round thatch skirts stacked crooked over two painted timber drums with lit windows, a porch, firewood, jars.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from house_kit import *

k = Kit("house_tiered", 2048, seed=11)
ROSE, MINT, OCHRE, CORAL, TURQ, CREAM, RED = [0.84, 0.50, 0.52], [0.56, 0.78, 0.66], [0.90, 0.68, 0.28], [0.88, 0.46, 0.34], [0.20, 0.64, 0.68], [0.95, 0.90, 0.78], [0.66, 0.22, 0.18]
GREY = [[0.56, 0.54, 0.51], [0.47, 0.455, 0.44], [0.64, 0.62, 0.58], [0.52, 0.50, 0.46], [0.60, 0.57, 0.55]]
AP = math.cos(math.pi / 8)                     # an octagon's flat is this far out, per unit of its corner radius
R = 2.0                                        # a small turn so no face sits exactly on a diagonal


def octo(cx, cy, z, w):
    return ring(cx, cy, z, w, w, n=8, rot=R)


# ---- foot and base
k.loft("stone", [octo(0, 0, -1.5, 10.6), octo(0, 0, 0.1, 10.0), octo(0, 0, 0.55, 9.5)], bevel=0.13, base=GREY, bw=1.4, course=0.6, moss=0.6, ground=0.0)
W0, W1 = 8.9, 8.0
k.loft("stone", [octo(0, 0, 0.55, W0), octo(0, 0, 3.7, W1)], bevel=0.10, base=GREY, bw=1.0, course=0.5, moss=0.45, eave=3.7, eave_len=0.9, foot=False,
       paint=dict(col=MINT, cover=0.68, fade=0.9),
       bands=[dict(z0=2.95, z1=3.5, col=OCHRE, col2=CORAL, pat="zig", period=0.75, wear=0.3), dict(z0=0.62, z1=0.95, col=CORAL, wear=0.5)],
       decals=[dict(dir="x+", motif="lizard", at=(0, 0.0, 1.95), size=2.0, col=CORAL, wear=0.24),
               dict(dir="x-", motif="weave", at=(0, 0.0, 1.95), size=1.7, col=OCHRE, wear=0.26),
               dict(dir="y+", motif="face", at=(0, 0, 1.95), size=1.6, col=CREAM, wear=0.26)])
base = Wall(0, 0, 0.55, 3.7, W0 * AP, W0 * AP, W1 * AP, W1 * AP)
# pilasters on the eight corners
for i in range(8):
    a = math.radians(45 * i + 22.5 - 90 + R)
    b = Vector((math.cos(a) * W0 / 2, math.sin(a) * W0 / 2, 0.55)); t = Vector((math.cos(a) * W1 / 2, math.sin(a) * W1 / 2, 3.72))
    k.beam("block", b, t, 0.62, 0.62, up=(math.cos(a), math.sin(a), 0), w1=0.5, h1=0.5, bevel=0.07, base=PALESTONE, sub=0.6, foot=False, moss=0.4, seed=i, dens=0.7,
           paint=dict(col=CORAL if i % 2 else OCHRE, cover=0.4, fade=0))
doorway(k, base.at("y-", 0.55, 0.0), "y-", 1.3, 2.2, RED, decals=[dict(motif="moon", at=(0, 0, 2.0), size=0.8, col=CREAM, wear=0.2)], paint=dict(col=OCHRE, cover=0.5, fade=0))
window(k, base.at("x+", 2.1, -1.0), "x+", 0.6, 0.8, lattice="grid", nx=2, nz=2)
window(k, base.at("x-", 2.2, 0.9), "x-", 0.7, 0.9, lattice="grid", nx=2, nz=2)
window(k, base.at("y+", 2.3, 1.05), "y+", 0.6, 0.8, lattice="grid", nx=2, nz=2)
# steps and a porch awning on two posts
for i, (y, z, w) in enumerate(((-4.7, 0.34, 2.6), (-5.2, 0.12, 3.0))):
    k.box("block", (0.05 * i, y, z - 0.45), (w, 0.9, 1.1), bevel=0.08, base=PALESTONE, sub=1.0, moss=0.6, rz=(-2, 2)[i], seed=i)
for s in (-1, 1):
    k.cyl("wood", (s * 1.35, -5.35, 0.3), (s * 1.25, -5.2, 3.35), 0.13, 0.10, n=8, col=DARKWOOD, bands=[dict(z0=1.2, z1=1.5, col=RED), dict(z0=2.6, z1=2.75, col=CREAM)])
k.beam("wood", (-1.6, -5.2, 3.38), (1.6, -5.2, 3.42), 0.16, 0.16, col=DARKWOOD)
k.box("thatch", (0, -4.75, 3.72), (3.7, 1.9, 0.2), rx=-30, col=[0.80, 0.60, 0.30], layer=0.36, moss=0.5)
lantern(k, (1.2, -5.2, 2.75), 0.8)
k.rope((1.2, -5.2, 3.35), (1.2, -5.2, 3.05), sag=0, n=1)

# ---- three thatch skirts over two timber drums, each a little off the one below
TH = dict(col=[0.66, 0.55, 0.40], dark=[0.34, 0.27, 0.21], layer=0.5, moss=0.7, age=0.5)      # old grey cogon
OFF = [(0.0, 0.0), (0.30, 0.12), (0.55, -0.05)]


def skirt(prof, off, n=18, seed=0, **kw):
    rings = []
    prof = [(prof[0][0] - 0.32, prof[0][1] * 0.985)] + list(prof)
    for i, (z, w) in enumerate(prof):
        r = ring(off[0], off[1], z, w, w * 0.96, n=n, power=2.2)
        if i == 0:
            r = [(x, y, zz + (0.12 if j % 2 else -0.10) + 0.10 * math.sin(j * 1.7 + seed)) for j, (x, y, zz) in enumerate(r)]
        rings.append(r)
    k.loft("thatch", rings, cx=off[0], cy=off[1], **dict(TH, **kw))


def drum(off, z0, z1, w0, w1, col, band, motif):
    k.loft("wood", [ring(off[0], off[1], z0, w0, w0, n=12, rot=R), ring(off[0], off[1], z1, w1, w1, n=12, rot=R)], col=[0.50, 0.33, 0.22], plank=0.42, grain="z",
           paint=dict(col=col, cover=0.66, scale=0.8), bands=[dict(z0=z0 + (z1 - z0) * 0.62, z1=z1 - 0.1, **band)],
           decals=[dict(dir="x+", motif=motif, at=(0, off[1], (z0 + z1) / 2 - 0.1), size=0.9, col=CREAM, wear=0.2)])
    w = Wall(off[0], off[1], z0, z1, w0 * 0.97, w0 * 0.97, w1 * 0.97, w1 * 0.97)
    for d, s in (("y-", 0.0), ("x-", 0.2), ("y+", -0.3)):
        at = w.at(d, (z0 + z1) / 2 - 0.05, s)
        obox(k, "glow", at, d, 0.62, 0.2, 0.62, out=0.02, glow=True, lattice="grid", nx=2, nz=2, hide=others(d))
        obox(k, "wood", at + Vector((0, 0, 0.40)), d, 0.95, 0.24, 0.14, out=0.05, col=DARKWOOD)
        obox(k, "wood", at - Vector((0, 0, 0.40)), d, 1.05, 0.3, 0.12, out=0.08, col=DARKWOOD)


skirt([(3.45, 12.6), (3.85, 11.2), (4.6, 9.6), (5.45, 7.9), (6.0, 6.9)], OFF[0], seed=1, bands=[dict(z0=5.5, z1=5.8, col=RED)])
drum(OFF[1], 5.7, 7.15, 6.3, 5.9, CORAL, dict(col=CREAM, col2=RED, pat="tri", period=0.4), "sun")
skirt([(6.75, 9.9), (7.1, 8.7), (7.8, 7.3), (8.5, 6.0), (8.95, 5.2)], OFF[1], seed=2, bands=[dict(z0=8.55, z1=8.8, col=TURQ)])
drum(OFF[2], 8.7, 9.95, 4.6, 4.3, OCHRE, dict(col=RED, col2=CREAM, pat="dot", period=0.4), "rice")
cx, cy = OFF[2]
prof = [(9.25, 7.5), (9.55, 7.6), (9.9, 6.5), (10.6, 5.0), (11.5, 3.4), (12.5, 2.0), (13.4, 1.0), (14.0, 0.5)]
rings = []
for i, (z, w) in enumerate(prof):
    r = ring(cx + (z - 9.55) ** 2 * 0.03, cy, z, w, w * 0.96, n=18, power=2.2)
    if i == 0:
        r = [(x, y, zz + (0.12 if j % 2 else -0.10) + 0.10 * math.sin(j * 2.9)) for j, (x, y, zz) in enumerate(r)]
    rings.append(r)
k.loft("thatch", rings, cx=cx, cy=cy, **dict(TH, bands=[dict(z0=12.75, z1=13.05, col=RED)]))
TX = cx + 0.6
k.loft("wood", [ring(TX, cy, 13.9, 0.6, 0.6, n=8), ring(TX, cy, 14.3, 0.75, 0.75, n=8), ring(TX + 0.03, cy, 15.2, 0.16, 0.16, n=8)], col=RED, grain="z", bands=[dict(z0=14.25, z1=14.4, col=CREAM)])
k.cyl("flat", (TX + 0.03, cy, 15.1), (TX + 0.05, cy, 15.5), 0.03, 0.03, n=4, col=[0.3, 0.25, 0.2])
k.cyl("flat", (TX + 0.05, cy - 0.05, 15.9), (TX + 0.05, cy + 0.05, 15.9), 0.42, 0.42, n=12, col=[0.86, 0.70, 0.34], edge=0.0, ringw=0.16)     # a bronze moon disc for Mayari

# ---- firewood under the eave, jars, a bench, washing
for i in range(9):
    r, c = divmod(i, 4)
    k.cyl("wood", (4.55 + 0.05 * r, 0.6 + c * 0.34 + 0.17 * (r % 2), 0.72 + r * 0.3), (5.5 + 0.08 * c, 0.65 + c * 0.34 + 0.17 * (r % 2), 0.72 + r * 0.3), 0.15, 0.14, n=6,
          col=[0.50 + 0.05 * (i % 3), 0.35, 0.22], grain="h", dens=0.6)
jar(k, (-4.9, -1.2, 0.5), 0.45, 1.05)
jar(k, (-5.0, -0.1, 0.5), 0.32, 0.7, col=(0.56, 0.36, 0.30))
k.box("wood", (-2.6, -4.55, 0.95), (1.7, 0.5, 0.12), col=[0.56, 0.38, 0.25], plank=0.25, grain="h", bevel=0.02, rz=-20)
for s in (-0.65, 0.65):
    k.box("wood", (-2.6 + s * 0.94, -4.55 - s * 0.34, 0.7), (0.14, 0.4, 0.5), col=DARKWOOD, rz=-20)
banner(k, (-5.3, -2.9, 0.2), h=6.6, col=CORAL, lean=(-0.55, -0.3), drop=2.6, seed=3)
k.plant((5.2, 3.6, 0.0), n=6, size=1.9, seed=1)
k.plant((-4.6, 4.1, 0.0), n=6, size=1.6, seed=2)
k.plant((-4.2, -4.3, 0.0), n=5, size=1.2, seed=3)
k.vine([(-3.2, 3.3, 3.7), (-3.4, 3.5, 2.9), (-3.5, 3.55, 2.1), (-3.7, 3.7, 1.3)], leaves=7, seed=4)


def deform(v):
    z = max(v.z, 0.0)
    a = math.radians(0.7 * z)
    x, y = v.x * math.cos(a) - v.y * math.sin(a), v.x * math.sin(a) + v.y * math.cos(a)
    return (x + 0.016 * z ** 1.5, y + 0.010 * z ** 1.5, v.z + 0.02 * v.x * min(z / 6.0, 1.5))


k.deform = deform
k.meta.update(note="glow: material house_tiered_glow (base windows, drum windows, porch lantern), same atlas",
              close=[(0.6, -3.5, 3.6), (5.4, -11.5, 2.6)], close_label="porch, door, first skirt and drum", distance=18.0, look=0.45, figure=(2.6, -6.2))
k.build()
