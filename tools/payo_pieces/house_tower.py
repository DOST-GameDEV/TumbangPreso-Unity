"""PAYO town, building 1: the LEANING STACKED TOWER-HOUSE. Three storeys of stone that step in and lean, a flared
thatch cap, a painted door, a balcony, cloth on a line, a lantern. Run through tools/payo_pieces/house_run.sh tower vN.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from house_kit import *

k = Kit("house_tower", 2048, seed=3)
ROSE, MINT, OCHRE, CORAL, TURQ, CREAM = [0.86, 0.52, 0.52], [0.56, 0.80, 0.66], [0.90, 0.68, 0.28], [0.88, 0.48, 0.36], [0.20, 0.66, 0.68], [0.95, 0.90, 0.78]
GREY = [[0.56, 0.54, 0.51], [0.47, 0.455, 0.44], [0.64, 0.62, 0.58], [0.52, 0.50, 0.46], [0.60, 0.57, 0.55]]

# ---- the foot: big rough blocks, sinks 1.5 m
k.loft("stone", [ring(0.1, 0, -1.5, 8.0, 7.3), ring(0.05, 0, 0.05, 7.5, 6.9), ring(0, 0.05, 0.55, 7.3, 6.6)], bevel=0.14, base=GREY, bw=1.5, course=0.62, moss=0.5, ground=0.0, bev=0.09)
for i, (y, z, w) in enumerate(((-3.55, 0.36, 2.6), (-4.0, 0.16, 3.0), (-4.5, -0.04, 3.3))):     # the door steps
    k.box("block", (-1.25 + i * 0.06, y, z - 0.45), (w, 0.9, 1.1), bevel=0.08, base=PALESTONE, sub=1.0, moss=0.6, rz=(-2, 1.5, -1)[i], seed=i)

# ---- storey one: bare grey stone, the last of a coral coat, a fret band
s1 = Wall(0, 0.05, 0.55, 4.35, 6.8, 6.1, 5.4, 4.9)
k.loft("stone", s1.rings(), bevel=0.10, base=GREY, bw=1.05, course=0.54, moss=0.55, eave=4.35, eave_len=0.6,
       paint=dict(col=CORAL, cover=0.34, fade=2.2),
       bands=[dict(z0=3.42, z1=4.02, col=OCHRE, col2=TURQ, pat="fret", period=0.9, wear=0.34)],
       decals=[dict(dir="y-", motif="rice", at=(1.95, 0, 1.45), size=0.9, col=CREAM, wear=0.36),
               dict(dir="x+", motif="hand", at=(0, -1.3, 2.3), size=0.7, col=CORAL, wear=0.34),
               dict(dir="x+", motif="hand", at=(0, -0.5, 2.5), size=0.7, col=CREAM, wear=0.38),
               dict(dir="y+", motif="spiral", at=(-0.5, 0, 2.2), size=1.5, col=TURQ, wear=0.38)])
quoins(k, s1, corners=((1, -1), (-1, -1), (1, 1), (-1, 1)), n=6, seed=2, paint=dict(col=CORAL, cover=0.22, fade=0))
proud_stones(k, s1, "y-", n=3, seed=1, zr=(0.08, 0.35))
proud_stones(k, s1, "x+", n=4, seed=2)
proud_stones(k, s1, "y+", n=4, seed=3)
doorway(k, s1.at("y-", 0.55, -1.25), "y-", 1.3, 2.3, TURQ, decals=[dict(motif="sun", at=(-1.25, 0, 2.05), size=0.95, col=OCHRE, wear=0.24)])
window(k, s1.at("y-", 2.55, 1.9), "y-", 0.8, 1.0, lattice="grid", nx=2, nz=2)
window(k, s1.at("x+", 2.3, 1.1), "x+", 0.7, 0.9, lattice="grid", nx=2, nz=2)
# a buttress on the right, a lean-to on the left
k.loft("stone", [ring(3.3, 1.2, -1.5, 1.7, 2.2), ring(3.05, 1.2, 1.7, 1.2, 1.8), ring(2.8, 1.2, 3.1, 0.5, 1.4)], bevel=0.10, base=GREY, bw=0.9, course=0.5, moss=0.7, ground=0.0)
k.box("thatch", (-3.75, 0.6, 2.75), (2.3, 3.3, 0.22), ry=-24, col=[0.78, 0.60, 0.30], layer=0.38, cx=-3.0, cy=0.6, moss=0.6)
for y in (-0.8, 2.0):
    k.cyl("wood", (-4.55, y, -1.5), (-4.5, y, 2.45), 0.11, 0.09, n=6, col=DARKWOOD, dens=0.7)
k.beam("wood", (-4.55, -1.2, 2.42), (-4.5, 2.3, 2.42), 0.14, 0.14, col=DARKWOOD, dens=0.7)
jar(k, (-3.75, -0.3, 0.5), 0.40, 0.95)
jar(k, (-3.9, 0.75, 0.5), 0.30, 0.7, col=(0.56, 0.36, 0.30))
jar(k, (-3.7, 1.55, 0.5), 0.36, 0.55, col=(0.74, 0.50, 0.30))

# ---- the first cornice: an ochre slab on corbels
k.loft("block", [ring(0.05, 0.05, 4.33, 5.9, 5.4), ring(0.08, 0.05, 4.80, 6.5, 6.0)], bevel=0.08, base=PALESTONE, sub=1.1, moss=0.5, foot=False,
       paint=dict(col=OCHRE, cover=0.72, fade=0), bands=[dict(z0=4.42, z1=4.74, col=OCHRE, col2=CORAL, pat="tri", period=0.5, wear=0.3)])
for i in range(7):
    obox(k, "block", s1.at("y-", 4.12, -2.25 + i * 0.75), "y-", 0.34, 0.46, 0.36, out=0.10, dens=0.6, bevel=0.05, base=PALESTONE, foot=False, moss=0.2, seed=i, top=(1.0, 1.25))
for i in range(6):
    obox(k, "block", s1.at("x+", 4.12, -1.9 + i * 0.76), "x+", 0.34, 0.46, 0.36, out=0.10, dens=0.6, bevel=0.05, base=PALESTONE, foot=False, moss=0.2, seed=i + 9)

# ---- storey two: rose, steps in and back
s2 = Wall(0.45, 0.30, 4.78, 7.95, 4.7, 4.4, 4.5, 4.2, sx=0.1)
k.loft("stone", s2.rings(), bevel=0.10, base=GREY, bw=0.95, course=0.5, moss=0.4, eave=7.95, eave_len=0.6, foot=False,
       paint=dict(col=ROSE, cover=0.70, fade=0.8),
       bands=[dict(z0=7.25, z1=7.75, col=CREAM, col2=CORAL, pat="zig", period=0.7, wear=0.30)],
       decals=[dict(dir="x+", motif="lizard", at=(0, 0.2, 6.0), size=2.6, col=TURQ, wear=0.26),
               dict(dir="y+", motif="weave", at=(0.2, 0, 6.1), size=1.3, col=OCHRE, wear=0.30),
               dict(dir="x-", motif="sun", at=(0, 0.3, 6.1), size=1.5, col=OCHRE, wear=0.32)])
quoins(k, s2, corners=((1, -1), (-1, 1)), n=5, seed=5, paint=dict(col=CREAM, cover=0.5, fade=0))
# the balcony
by0, by1, bz = -1.95, -3.55, 4.95
k.box("wood", (0.2, (by0 + by1) / 2, bz), (3.5, by0 - by1 + 0.1, 0.16), col=[0.50, 0.33, 0.22], plank=0.36, grain="h", bevel=0.03, ry=1.5)
for x in (-1.2, 1.6):
    k.beam("wood", (x, -2.75, 4.15), (x, -3.5, 4.86), 0.18, 0.20, col=DARKWOOD)
    k.beam("wood", (x, -2.6, 4.80), (x, -3.6, 4.82), 0.2, 0.2, col=DARKWOOD)
posts = [(-1.45, by1 + 0.1), (0.2, by1 + 0.05), (1.85, by1 + 0.1), (-1.45, by0 - 0.25), (1.85, by0 - 0.25)]
for i, (x, y) in enumerate(posts):
    k.box("wood", (x, y, bz + 0.62), (0.16, 0.16, 1.15), top=(0.8, 0.8), col=[0.62, 0.24, 0.18], bevel=0.03, rz=i * 7, ry=(i - 2) * 1.5,
          bands=[dict(z0=bz + 0.85, z1=bz + 1.0, col=CREAM)])
k.beam("wood", (-1.6, by1 + 0.1, bz + 1.08), (2.0, by1 + 0.1, bz + 1.14), 0.14, 0.12, col=DARKWOOD)
k.beam("wood", (-1.45, by1, bz + 1.1), (-1.45, by0 - 0.1, bz + 1.1), 0.12, 0.12, col=DARKWOOD)
k.beam("wood", (1.85, by1, bz + 1.14), (1.85, by0 - 0.1, bz + 1.14), 0.12, 0.12, col=DARKWOOD)
k.beam("wood", (-1.5, by1 + 0.1, bz + 0.55), (1.9, by1 + 0.1, bz + 0.58), 0.08, 0.08, col=DARKWOOD)
k.box("cloth", (0.95, by1 + 0.02, bz + 0.74), (1.0, 0.07, 0.95), col=[0.80, 0.22, 0.20], rz=2, ry=3)        # a blanket over the rail
k.box("cloth", (0.95, by1 + 0.14, bz + 1.17), (1.02, 0.22, 0.06), col=[0.80, 0.22, 0.20], rz=2, ry=3, stripes=[])
# the balcony door, lit, with a short curtain
dw = s2.at("y-", 4.95, 0.1)
obox(k, "glow", dw + Vector((0, 0, 0.95)), "y-", 1.0, 0.16, 1.9, out=0.03, glow=True, lattice="slit", hide=others("y-"))
obox(k, "block", dw + Vector((0, 0, 2.1)), "y-", 1.9, 0.52, 0.40, out=0.08, bevel=0.06, base=PALESTONE, foot=False, top=(0.9, 0.9), paint=dict(col=TURQ, cover=0.6, fade=0))
for s in (-1, 1):
    obox(k, "block", dw + Vector((0, 0, 0.95)), "y-", 0.30, 0.42, 1.92, out=0.06, side=s * 0.66, bevel=0.05, base=PALESTONE, foot=False, seed=s + 2, paint=dict(col=TURQ, cover=0.45, fade=0))
obox(k, "cloth", dw + Vector((0, 0, 1.55)), "y-", 1.0, 0.06, 0.62, out=0.14, col=OCHRE, stripes=[[0.72, 0.84, [0.75, 0.2, 0.18]], [0.9, 1.0, [0.14, 0.14, 0.18]]], col2=[0.75, 0.2, 0.18])
window(k, s2.at("y+", 6.3, 0.9), "y+", 0.7, 1.0, lattice="grid", nx=2, nz=2)
window(k, s2.at("x-", 6.5, 0.6), "x-", 0.7, 0.9, lattice="grid", nx=2, nz=2, hood=[0.78, 0.60, 0.30])

# ---- the second cornice: turquoise
k.loft("block", [ring(0.45, 0.3, 7.93, 4.85, 4.5), ring(0.40, 0.3, 8.33, 5.5, 5.1)], bevel=0.07, base=PALESTONE, sub=1.0, moss=0.55, foot=False,
       paint=dict(col=TURQ, cover=0.70, fade=0), bands=[dict(z0=8.0, z1=8.27, col=TURQ, col2=CREAM, pat="dot", period=0.42, wear=0.3)])

# ---- storey three: mint
s3 = Wall(0.15, 0.45, 8.31, 10.75, 3.45, 3.25, 3.85, 3.6)
k.loft("stone", s3.rings(), bevel=0.09, base=GREY, bw=0.9, course=0.48, moss=0.4, eave=10.6, eave_len=1.0, foot=False,
       paint=dict(col=MINT, cover=0.74, fade=0.6),
       bands=[dict(z0=8.38, z1=8.86, col=OCHRE, col2=CORAL, pat="dia", period=0.62, wear=0.3)],
       decals=[dict(dir="x+", motif="moon", at=(0, 0.42, 9.65), size=1.15, col=CREAM, wear=0.25),
               dict(dir="y+", motif="face", at=(0.38, 0, 9.6), size=1.1, col=CORAL, wear=0.3)])
window(k, s3.at("y-", 9.65, 0.0), "y-", 0.95, 1.15, lattice="dia", paint=dict(col=CORAL, cover=0.55, fade=0))
window(k, s3.at("x-", 9.6, 0.0), "x-", 0.7, 0.9, lattice="grid", nx=2, nz=2)
quoins(k, s3, corners=((-1, -1), (1, 1)), n=4, seed=7, paint=dict(col=CREAM, cover=0.5, fade=0))

# ---- the cap: a steep flared thatch, Ifugao fashion, with crossed horns
cx, cy = 0.2, 0.45
TH = dict(col=[0.84, 0.64, 0.30], dark=[0.44, 0.30, 0.18], layer=0.62, cx=cx, cy=cy, vscale=1.35, moss=0.55,
          bands=[dict(z0=12.95, z1=13.3, col=[0.62, 0.22, 0.18])])
prof = [(9.8, 6.55, 6.25), (10.15, 6.7, 6.4), (10.5, 5.9, 5.65), (11.2, 4.7, 4.5), (12.1, 3.4, 3.25), (13.1, 2.25, 2.15), (14.2, 1.3, 1.25), (15.1, 0.62, 0.6)]
rings = []
for i, (z, w, d) in enumerate(prof):
    r = ring(cx + (z - 10.2) ** 2 * 0.036, cy - (z - 10.2) ** 2 * 0.012, z, w, d, n=16, power=3.2)
    if i == 0:
        r = [(x, y, zz + (0.13 if j % 2 else -0.10) + 0.10 * math.sin(j * 1.9)) for j, (x, y, zz) in enumerate(r)]
    rings.append(r)
k.loft("thatch", rings, **TH)
TX, TY = cx + 0.86, cy - 0.29
k.box("wood", (TX, TY, 15.2), (0.62, 0.62, 0.34), top=(0.7, 0.7), col=[0.62, 0.22, 0.18], bevel=0.04, bands=[])
for s in (-1, 1):
    k.beam("wood", (TX - s * 0.25, TY, 15.1), (TX + s * 0.95, TY, 16.4), 0.16, 0.16, w1=0.05, h1=0.05, col=DARKWOOD)

# ---- the lantern, on a beam out of the first cornice
k.beam("wood", (2.35, -2.5, 4.45), (2.6, -4.15, 4.62), 0.16, 0.18, col=DARKWOOD)
k.rope((2.58, -4.0, 4.55), (2.58, -4.0, 3.95), sag=0, n=1)
lantern(k, (2.58, -4.0, 3.58), 1.1)

# ---- the cloth line: from the third storey down to a leaning pole
p_top = Vector((-4.75, -2.9, 6.3))
k.cyl("wood", (-4.25, -2.7, -1.5), p_top + Vector((-0.05, 0, 0.35)), 0.13, 0.08, n=6, col=DARKWOOD, dens=0.6)
line = k.rope(s3.at("x-", 9.9, -1.2), p_top, sag=0.55, n=8)
CL = [([0.82, 0.24, 0.20], 1.0, 1.25), ([0.95, 0.90, 0.78], 0.8, 0.9), ([0.20, 0.60, 0.64], 0.9, 1.4), ([0.92, 0.70, 0.26], 0.75, 1.0)]
for i, (c, w, h) in enumerate(CL):
    a, b = line[1 + i * 2], line[2 + i * 2]
    m = (a + b) / 2
    ang = math.degrees(math.atan2(b.y - a.y, b.x - a.x))
    k.box("cloth", (m.x, m.y, m.z - h / 2 + 0.03), (w, 0.06, h), rz=ang, ry=(i - 1.5) * 4, col=c, seed=i,
          stripes=[[0.08, 0.14, [0.14, 0.14, 0.18]], [0.76, 0.86, CREAM if i != 1 else [0.8, 0.24, 0.2]]], col2=[0.14, 0.14, 0.18])

# ---- green things
k.plant((3.7, -3.2, 0.0), n=6, size=1.9, seed=1)
k.plant((-3.3, -3.9, 0.0), n=5, size=1.4, seed=2)
k.plant((3.2, 3.8, 0.0), n=6, size=2.0, seed=3)
# a ladder left leaning on the back
for s in (-0.3, 0.3):
    k.beam("wood", (-1.6 + s, 4.6, -0.2), (-1.5 + s, 2.95, 4.9), 0.09, 0.09, col=[0.55, 0.38, 0.24], dens=0.6)
for i in range(8):
    t = 0.08 + i * 0.115
    k.beam("wood", (-1.95 + 0.1 * t, 4.6 - 1.65 * t, -0.2 + 5.1 * t), (-1.25 + 0.1 * t, 4.6 - 1.65 * t, -0.2 + 5.1 * t), 0.07, 0.07, col=[0.55, 0.38, 0.24], dens=0.5)
k.plant((2.4, 2.2, 4.8), n=5, size=0.7, seed=4)
k.vine([(2.9, 0.4, 4.8), (3.2, 0.0, 3.9), (3.15, -0.6, 3.0), (3.3, -0.9, 2.1)], leaves=7, seed=5)
k.vine([(-1.6, 2.75, 8.3), (-1.9, 2.85, 7.3), (-1.7, 2.8, 6.3), (-2.0, 2.9, 5.4)], leaves=7, seed=6)


def deform(v):
    z = max(v.z, 0.0)
    pts = [(0, 0), (4.3, 0), (4.8, 6), (7.9, 6), (8.35, -5), (10.2, -5), (17, -14)]
    a = 0.0
    for (z0, a0), (z1, a1) in zip(pts, pts[1:]):
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0); t = t * t * (3 - 2 * t)
            a = math.radians(a0 + (a1 - a0) * t)
    x, y = v.x * math.cos(a) - v.y * math.sin(a), v.x * math.sin(a) + v.y * math.cos(a)
    return (x + 0.031 * z ** 1.5, y - 0.008 * z ** 1.5, v.z)


k.deform = deform
k.meta.update(note="glow: material house_tower_glow (windows, balcony door, lantern), same atlas",
              close=[(0.3, -2.6, 3.9), (4.6, -10.0, 3.3)], close_label="door, balcony and lantern", distance=17.0, look=0.5)
k.build()
