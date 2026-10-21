"""PAYO town, building 5: the GONG TOWER. Tall and thin: a stone shaft that narrows then swells, a slab on corbels,
an open top storey of four posts with a hanging bronze gong, a wide low thatch hat. It leans the other way.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from house_kit import *

k = Kit("house_gong", 2048, seed=13)
ROSE, MINT, OCHRE, CORAL, TURQ, CREAM, RED = [0.84, 0.50, 0.52], [0.56, 0.78, 0.66], [0.90, 0.68, 0.28], [0.88, 0.46, 0.34], [0.20, 0.64, 0.68], [0.95, 0.90, 0.78], [0.66, 0.22, 0.18]
GREY = [[0.56, 0.54, 0.51], [0.47, 0.455, 0.44], [0.64, 0.62, 0.58], [0.52, 0.50, 0.46], [0.60, 0.57, 0.55]]
BRONZE = [0.74, 0.54, 0.24]

# ---- foot
k.loft("stone", [ring(0, 0, -1.5, 5.6, 5.4), ring(0, 0, 0.1, 5.1, 4.9), ring(0, 0, 0.6, 4.6, 4.4)], bevel=0.13, base=GREY, bw=1.3, course=0.6, moss=0.6, ground=0.0)
for i, (y, z, w) in enumerate(((-2.6, 0.38, 2.0), (-3.1, 0.14, 2.4))):
    k.box("block", (0.0, y, z - 0.45), (w, 0.9, 1.1), bevel=0.08, base=PALESTONE, sub=0.9, moss=0.6, rz=(3, -2)[i], seed=i)

# ---- the shaft, in three lifts: grey, coral, ochre
lifts = [Wall(0, 0, 0.6, 4.2, 3.7, 3.5, 3.0, 2.9), Wall(0, 0, 4.5, 8.4, 2.9, 2.8, 2.5, 2.45), Wall(0, 0, 8.7, 10.6, 2.5, 2.45, 3.0, 2.9)]
k.loft("stone", lifts[0].rings(), bevel=0.10, base=GREY, bw=1.0, course=0.54, moss=0.55, eave=4.2, eave_len=0.5,
       paint=dict(col=CORAL, cover=0.25, fade=2.0), bands=[dict(z0=3.35, z1=3.95, col=TURQ, col2=CREAM, pat="fret", period=0.8, wear=0.34)],
       decals=[dict(dir="x+", motif="hand", at=(0, 0.2, 2.2), size=0.8, col=CREAM, wear=0.3), dict(dir="y+", motif="spiral", at=(0, 0, 2.1), size=1.4, col=TURQ, wear=0.3),
               dict(dir="x-", motif="step", at=(0, 0, 2.0), size=1.4, col=OCHRE, wear=0.3)])
k.loft("stone", lifts[1].rings(), bevel=0.09, base=GREY, bw=0.9, course=0.5, moss=0.4, eave=8.4, eave_len=0.5, foot=False,
       paint=dict(col=CORAL, cover=0.72, fade=0.6), bands=[dict(z0=4.6, z1=5.05, col=CREAM, col2=RED, pat="tri", period=0.5, wear=0.3)],
       decals=[dict(dir="y-", motif="sun", at=(0, 0, 6.9), size=1.7, col=OCHRE, wear=0.2), dict(dir="x+", motif="lizard", at=(0, 0.4, 6.6), size=1.6, col=CREAM, wear=0.22),
               dict(dir="y+", motif="moon", at=(0, 0, 6.8), size=1.5, col=CREAM, wear=0.22), dict(dir="x-", motif="rice", at=(0, 0.5, 7.0), size=1.5, col=CREAM, wear=0.22)])
k.loft("stone", lifts[2].rings(), bevel=0.09, base=GREY, bw=0.9, course=0.48, moss=0.4, eave=10.6, eave_len=0.5, foot=False,
       paint=dict(col=OCHRE, cover=0.72, fade=0.3), bands=[dict(z0=9.6, z1=10.2, col=TURQ, col2=CREAM, pat="dia", period=0.6, wear=0.3)])
for z, w, col, pat in ((4.2, 3.3, OCHRE, "dot"), (8.4, 2.85, TURQ, "tri")):          # the string courses between the lifts
    k.loft("block", [ring(0, 0, z - 0.03, w, w - 0.08), ring(0, 0, z + 0.35, w + 0.45, w + 0.36)], bevel=0.07, base=PALESTONE, sub=0.9, moss=0.6, foot=False,
           paint=dict(col=col, cover=0.68, fade=0), bands=[dict(z0=z + 0.04, z1=z + 0.3, col=col, col2=CREAM if pat == "dot" else CORAL, pat=pat, period=0.4, wear=0.3)])
quoins(k, lifts[0], corners=((1, -1), (-1, -1), (1, 1), (-1, 1)), n=5, seed=3, proud=0.08)
proud_stones(k, lifts[1], "y+", n=3, seed=4)
doorway(k, lifts[0].at("y-", 0.6, 0.0), "y-", 1.1, 2.1, OCHRE, decals=[dict(motif="spiral", at=(0, 0, 1.9), size=0.7, col=RED, wear=0.2)])
for wl, d, z, s in ((lifts[0], "x+", 2.7, 0.3), (lifts[1], "y-", 5.6, 0.3), (lifts[1], "x-", 5.9, -0.2), (lifts[1], "y+", 5.4, 0.2), (lifts[2], "y-", 9.2, 0.0), (lifts[2], "x+", 9.15, 0.0)):
    at = wl.at(d, z, s)
    obox(k, "glow", at, d, 0.40, 0.2, 0.95, out=0.02, glow=True, lattice="slit", hide=others(d))                 # arrow slits that light at night
    obox(k, "block", at + Vector((0, 0, 0.58)), d, 0.7, 0.34, 0.2, out=0.06, bevel=0.04, base=PALESTONE, foot=False, moss=0.2)
    obox(k, "block", at - Vector((0, 0, 0.56)), d, 0.62, 0.36, 0.16, out=0.07, bevel=0.04, base=PALESTONE, foot=False, moss=0.2)

# ---- the gong floor: a slab on corbels, four posts, a rail, the gong
ZP = 10.6
for d in ("y-", "y+", "x+", "x-"):
    for s in (-0.9, 0.0, 0.9):
        obox(k, "block", lifts[2].at(d, ZP - 0.22, s), d, 0.36, 0.7, 0.4, out=0.22, bevel=0.05, base=PALESTONE, foot=False, moss=0.2, top=(1.0, 1.0), dens=0.6, seed=int(s * 3))
k.loft("block", [ring(0, 0, ZP, 4.1, 4.0), ring(0, 0, ZP + 0.42, 4.5, 4.4)], bevel=0.08, base=PALESTONE, sub=1.1, moss=0.6, foot=False, paint=dict(col=ROSE, cover=0.66, fade=0),
       bands=[dict(z0=ZP + 0.06, z1=ZP + 0.36, col=ROSE, col2=CREAM, pat="zig", period=0.5, wear=0.3)])
ZT = 14.0
for sx in (-1, 1):
    for sy in (-1, 1):
        k.cyl("wood", (sx * 1.75, sy * 1.7, ZP + 0.4), (sx * 1.6, sy * 1.55, ZT), 0.17, 0.13, n=8, mid=[(0.30, 0.16), (0.34, 0.20), (0.38, 0.16)], col=[0.60, 0.24, 0.18], grain="z",
              bands=[dict(z0=11.2, z1=11.45, col=CREAM), dict(z0=12.9, z1=13.2, col=OCHRE, col2=RED, pat="dot", period=0.2)])
for d, o in (("y-", -1.72), ("y+", 1.72)):
    k.beam("wood", (-1.9, o, ZP + 0.95), (1.9, o, ZP + 0.98), 0.13, 0.11, col=DARKWOOD)
    k.box("weave", (0.0, o, ZP + 0.66), (3.2, 0.07, 0.44), col=[0.82, 0.66, 0.38], strip=0.11, diamond=0.22, edge=0.0)
for o in (-1.76, 1.76):
    k.beam("wood", (o, -1.9, ZP + 0.97), (o, 1.9, ZP + 0.95), 0.13, 0.11, col=DARKWOOD)
    k.box("weave", (o, 0.0, ZP + 0.66), (0.07, 3.1, 0.44), col=[0.82, 0.66, 0.38], strip=0.11, diamond=0.22, edge=0.0)
k.beam("wood", (-2.0, 0, ZT - 0.05), (2.0, 0, ZT - 0.05), 0.22, 0.24, col=DARKWOOD, bevel=0.03)
k.beam("wood", (0, -2.0, ZT - 0.02), (0, 2.0, ZT - 0.02), 0.20, 0.22, col=DARKWOOD, bevel=0.03)
k.beam("wood", (-1.75, -1.72, ZT), (1.75, -1.72, ZT), 0.18, 0.2, col=DARKWOOD)
k.beam("wood", (-1.75, 1.72, ZT), (1.75, 1.72, ZT), 0.18, 0.2, col=DARKWOOD)
GZ = 12.45
for s in (-0.45, 0.45):
    k.rope((s, 0, ZT - 0.15), (s * 1.1, 0, GZ + 0.62), sag=0, n=1, r=0.03)
k.cyl("flat", (0, -0.09, GZ), (0, 0.09, GZ), 1.05, 1.05, n=16, col=BRONZE, ringw=0.2, edge=0.03, dens=1.4)               # the gong
k.cyl("flat", (0, -0.2, GZ), (0, -0.07, GZ), 0.2, 0.26, n=10, col=[0.90, 0.72, 0.36], edge=0.02)                        # its boss
k.rope((1.3, -0.3, ZT - 0.15), (1.3, -0.3, 12.5), sag=0, n=1, r=0.02)
k.cyl("wood", (1.3, -0.3, 12.5), (1.32, -0.3, 11.75), 0.04, 0.04, n=5, col=[0.58, 0.40, 0.25], dens=0.5)                # the beater
k.cyl("cloth", (1.32, -0.3, 11.85), (1.32, -0.3, 11.6), 0.13, 0.13, n=8, col=RED, stripes=[])

# ---- the hat: wide and low, a salakot, with a tall spike and a pennant
TH = dict(col=[0.78, 0.40, 0.32], dark=[0.40, 0.20, 0.18], layer=0.42,       # a hat dyed red
           cx=0, cy=0, moss=0.5, bands=[dict(z0=15.5, z1=15.8, col=CREAM)])
prof = [(13.3, 6.1), (13.65, 6.2), (14.0, 5.3), (14.6, 4.1), (15.4, 2.7), (16.3, 1.5), (17.0, 0.7)]
rings = []
for i, (z, w) in enumerate(prof):
    r = ring(0, 0, z, w, w * 0.97, n=18, power=2.3)
    if i == 0:
        r = [(x, y, zz + (0.11 if j % 2 else -0.09) + 0.14 * math.sin(j * 1.1)) for j, (x, y, zz) in enumerate(r)]
    rings.append(r)
k.loft("thatch", rings, **TH)
k.loft("wood", [ring(0, 0, 16.9, 0.8, 0.8, n=8), ring(0, 0, 17.2, 0.5, 0.5, n=8), ring(0, 0, 18.6, 0.10, 0.10, n=8)], col=RED, grain="z", bands=[dict(z0=17.2, z1=17.4, col=CREAM)])
k.box("cloth", (0.55, 0, 18.1), (1.0, 0.05, 0.5), top=(0.6, 0.4), col=TURQ, stripes=[[0.0, 0.2, CREAM]], ry=-6)

# ---- a hanging banner down one face, a bamboo scaffold up another, plants
k.beam("wood", (1.66, -1.5, 10.42), (1.7, -0.4, 10.42), 0.08, 0.08, col=DARKWOOD)
k.box("cloth", (1.66, -0.95, 8.35), (0.05, 0.7, 3.9), col=RED, seed=4, stripes=[[0.03, 0.07, CREAM], [0.10, 0.13, [0.14, 0.14, 0.18]], [0.82, 0.88, [0.95, 0.76, 0.24]], [0.92, 1.0, [0.14, 0.14, 0.18]]], rx=1.5)
BAM = [0.62, 0.54, 0.28]
for (x0, y0, x1, y1) in ((-2.9, 1.9, -1.7, 1.7), (-2.9, -0.9, -1.75, -0.7)):
    k.cyl("wood", (x0, y0, -1.0), (x1, y1, 10.5), 0.08, 0.06, n=6, col=BAM, dens=0.5, bands=[dict(z0=z, z1=z + 0.07, col=[0.36, 0.30, 0.16]) for z in (1.5, 3.0, 4.5, 6.0, 7.5, 9.0)])
for z in (2.2, 4.4, 6.6, 8.8):
    t = (z + 1.0) / 11.5
    xa = -2.9 + 1.2 * t
    k.cyl("wood", (xa, 2.1 - 0.2 * t, z), (xa + 0.02, -1.1 + 0.2 * t, z + 0.1), 0.06, 0.06, n=5, col=BAM, dens=0.5)
k.box("wood", (-2.1, 0.5, 6.72), (1.0, 2.2, 0.1), col=[0.56, 0.38, 0.25], plank=0.3, grain="h", ry=-3)
k.plant((2.8, 2.6, 0.0), n=6, size=1.6, seed=1)
k.plant((2.6, -2.9, 0.0), n=5, size=1.2, seed=2)
k.vine([(1.5, 1.5, 10.6), (1.55, 1.6, 9.7), (1.4, 1.5, 8.8), (1.5, 1.45, 7.9), (1.35, 1.4, 7.0)], leaves=8, seed=3)


def deform(v):
    z = max(v.z, 0.0)
    a = math.radians(-0.9 * z)
    x, y = v.x * math.cos(a) - v.y * math.sin(a), v.x * math.sin(a) + v.y * math.cos(a)
    bend = -0.020 * z ** 1.5 + 0.35 * math.sin(min(z, 16) / 16 * math.pi)
    return (x + bend, y + 0.006 * z ** 1.5, v.z + 0.03 * v.x * (z > 13.0))


k.deform = deform
k.meta.update(note="glow: material house_gong_glow (six slits), same atlas",
              close=[(0.0, 0.0, 12.6), (6.5, -10.5, 11.0)], close_label="the gong floor", close_fov=42, distance=18.5, look=0.5, figure=(2.2, -3.6))
k.build()
