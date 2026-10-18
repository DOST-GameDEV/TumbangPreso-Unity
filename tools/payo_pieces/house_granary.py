"""PAYO town, building 3: the FLARED GRANARY (alang). Four carved posts with rat guards on a stone pad, a plank body
wider at the top than the bottom, a steep flared thatch, a ladder to its little door.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from house_kit import *

k = Kit("house_granary", 2048, seed=7)
OCHRE, CORAL, TURQ, CREAM, RED = [0.90, 0.68, 0.28], [0.88, 0.46, 0.34], [0.20, 0.64, 0.68], [0.95, 0.90, 0.78], [0.66, 0.22, 0.18]
GREY = [[0.56, 0.54, 0.51], [0.47, 0.455, 0.44], [0.64, 0.62, 0.58], [0.52, 0.50, 0.46], [0.60, 0.57, 0.55]]
WOOD = [0.50, 0.33, 0.22]

# ---- the stone pad
k.loft("stone", [ring(0, 0, -1.5, 6.6, 6.4), ring(0, 0, 0.1, 6.1, 5.9), ring(0, 0, 0.5, 5.7, 5.5)], bevel=0.13, base=GREY, bw=1.3, course=0.6, moss=0.6, ground=0.0)
for i, (y, z) in enumerate(((-3.2, 0.3), (-3.75, 0.08))):
    k.box("block", (-0.2, y, z - 0.4), (2.2 + i * 0.4, 0.8, 0.9), bevel=0.08, base=PALESTONE, sub=0.9, moss=0.6, seed=i, rz=(2, -3)[i])

# ---- four posts, splayed, carved in bands, each with a rat guard
ZF = 3.3
for sx in (-1, 1):
    for sy in (-1, 1):
        b = Vector((sx * 2.0, sy * 1.95, 0.45)); t = Vector((sx * 1.45, sy * 1.45, ZF))
        k.box("block", (b.x, b.y, 0.55), (1.0, 1.0, 0.36), bevel=0.08, base=PALESTONE, moss=0.5, top=(0.8, 0.8), seed=sx * 2 + sy, dens=0.7)
        k.cyl("wood", b, t, 0.30, 0.22, n=10, mid=[(0.25, 0.27), (0.30, 0.20), (0.42, 0.20), (0.47, 0.27)], col=WOOD, grain="z",
              bands=[dict(z0=0.6, z1=1.1, col=RED, col2=CREAM, pat="tri", period=0.22), dict(z0=2.05, z1=2.35, col=TURQ), dict(z0=2.9, z1=3.25, col=OCHRE, col2=RED, pat="dot", period=0.2)])
        g = b.lerp(t, 0.80)
        k.cyl("wood", g - Vector((0, 0, 0.07)), g + Vector((0, 0, 0.07)), 0.72, 0.66, n=12, col=[0.58, 0.40, 0.26], grain="h", dens=0.8)
# ---- the floor frame
for y in (-1.55, 1.55):
    k.beam("wood", (-2.35, y, ZF + 0.12), (2.35, y, ZF + 0.12), 0.30, 0.28, col=DARKWOOD, bevel=0.03)
for x in (-1.6, 0, 1.6):
    k.beam("wood", (x, -2.2, ZF + 0.36), (x, 2.2, ZF + 0.36), 0.24, 0.22, col=DARKWOOD, bevel=0.03)
k.box("wood", (0, 0, ZF + 0.54), (4.1, 4.1, 0.14), col=[0.56, 0.38, 0.25], plank=0.34, grain="h", bevel=0.02)

# ---- the body: planks, flaring out, an ochre coat with a red band and a rice stalk
body = Wall(0, 0, ZF + 0.6, 6.15, 3.3, 3.3, 5.3, 5.1)
k.loft("wood", body.rings(), bevel=0.07, col=[0.52, 0.35, 0.23], plank=0.36, grain="z", paint=dict(col=OCHRE, cover=0.62, scale=0.9), moss=0.25,
       bands=[dict(z0=5.55, z1=6.0, col=RED, col2=CREAM, pat="zig", period=0.5), dict(z0=ZF + 0.66, z1=ZF + 0.9, col=RED)],
       decals=[dict(dir="x+", motif="rice", at=(0, 0, 4.85), size=1.5, col=CREAM, wear=0.2), dict(dir="x-", motif="rice", at=(0, 0, 4.85), size=1.5, col=CREAM, wear=0.2),
               dict(dir="y+", motif="lizard", at=(0, 0, 4.8), size=1.5, col=RED, wear=0.2),
               dict(dir="y-", motif="rice", at=(1.15, 0, 4.9), size=1.2, col=CREAM, wear=0.2), dict(dir="y-", motif="rice", at=(-1.15, 0, 4.9), size=1.2, col=CREAM, wear=0.2)])
for sx in (-1, 1):
    for sy in (-1, 1):       # corner boards
        d = "y-" if sy < 0 else "y+"
        k.beam("wood", body.at(d, ZF + 0.6, -sx * sy * 1.65), body.at(d, 6.15, -sx * sy * 2.62), 0.26, 0.26, col=DARKWOOD, bevel=0.03)
# the little door with its sill
dm = body.at("y-", 4.75, 0)
obox(k, "door", dm, "y-", 0.95, 0.14, 1.3, out=0.06, col=TURQ, face="y-", hide=others("y-"), dens=1.5, plank=0.24,
     decals=[dict(dir="y-", motif="weave", at=(0, 0, 4.8), size=0.6, col=OCHRE, wear=0.2)])
obox(k, "wood", dm + Vector((0, 0, 0.78)), "y-", 1.45, 0.3, 0.22, out=0.1, col=RED, bevel=0.03)
obox(k, "wood", dm - Vector((0, 0, 0.74)), "y-", 1.6, 0.6, 0.16, out=0.26, col=DARKWOOD, bevel=0.03)
for s in (-1, 1):
    obox(k, "wood", dm, "y-", 0.18, 0.26, 1.34, out=0.08, side=s * 0.56, col=RED, bevel=0.03)
# the rim under the thatch
k.loft("wood", [ring(0, 0, 6.1, 5.2, 5.1), ring(0, 0, 6.38, 5.35, 5.25)], bevel=0.05, col=DARKWOOD, grain="h", bands=[])

# ---- the thatch: steep, flared, heavy
TH = dict(col=[0.90, 0.74, 0.40], dark=[0.50, 0.36, 0.20], layer=0.6, cx=0, cy=0, vscale=1.3, moss=0.6, bands=[dict(z0=9.5, z1=9.85, col=RED)])
prof = [(5.2, 7.85, 7.65), (5.55, 8.0, 7.8), (5.95, 7.0, 6.85), (6.7, 5.6, 5.5), (7.7, 4.0, 3.9), (8.8, 2.5, 2.45), (9.9, 1.3, 1.3), (10.6, 0.7, 0.7)]
rings = []
for i, (z, w, d) in enumerate(prof):
    r = ring(-(z - 5.5) ** 2 * 0.02, 0, z, w, d, n=16, power=3.0)
    if i == 0:
        r = [(x, y, zz + (0.14 if j % 2 else -0.12) + 0.12 * math.sin(j * 2.3)) for j, (x, y, zz) in enumerate(r)]
    rings.append(r)
k.loft("thatch", rings, **TH)
TX = -0.52
k.box("wood", (TX, 0, 10.72), (0.7, 0.7, 0.36), top=(0.7, 0.7), col=RED, bevel=0.04, bands=[])
for s in (-1, 1):
    k.beam("wood", (TX, -s * 0.2, 10.6), (TX, s * 0.9, 11.75), 0.16, 0.16, w1=0.05, h1=0.05, col=DARKWOOD)

# ---- the ladder, and what hangs under the floor
for s in (-0.34, 0.34):
    k.beam("wood", (-0.1 + s, -4.3, 0.3), (0.0 + s, -2.25, 4.15), 0.10, 0.10, col=[0.58, 0.40, 0.25], dens=0.6)
for i in range(8):
    t = 0.07 + i * 0.12
    y, z = -4.3 + 2.05 * t, 0.3 + 3.85 * t
    k.beam("wood", (-0.52 + 0.1 * t, y, z), (0.32 + 0.1 * t, y, z), 0.08, 0.08, col=[0.58, 0.40, 0.25], dens=0.5)
for i, (x, y, L, c) in enumerate(((-0.9, 0.5, 0.7, OCHRE), (0.2, -0.3, 1.0, [0.84, 0.66, 0.34]), (1.0, 0.8, 0.6, OCHRE), (0.5, 1.1, 0.9, [0.80, 0.56, 0.26]))):
    k.rope((x, y, ZF + 0.1), (x, y, ZF - L + 0.4), sag=0, n=1)
    k.box("thatch", (x, y, ZF - L), (0.34, 0.34, 0.8), top=(0.25, 0.25), col=c, layer=0.3, rz=i * 31, dens=0.7, moss=0)       # rice bundles
jar(k, (1.9, -0.2, 0.5), 0.42, 1.0)
jar(k, (-1.1, 1.1, 0.5), 0.36, 0.75, col=(0.56, 0.36, 0.30))
k.box("weave", (0.7, 1.2, 0.85), (0.9, 0.9, 0.7), top=(1.25, 1.25), col=[0.80, 0.64, 0.36], strip=0.10, bevel=0.03)         # a basket
# a drying line between two posts on the right side, a rice mortar and pestle by the steps
line = k.rope((2.25, -1.9, 2.2), (2.2, 1.9, 2.3), sag=0.25, n=4)
for i, c in enumerate(([0.80, 0.24, 0.20], [0.95, 0.90, 0.78], [0.20, 0.60, 0.64])):
    m = (line[i] + line[i + 1]) / 2
    k.box("cloth", (m.x, m.y + 0.2 * i, m.z - 0.5), (0.06, 0.8, 1.0 + 0.2 * (i % 2)), col=c, seed=i, rx=(i - 1) * 4)
k.loft("wood", [ring(-2.1, -2.0, 0.5, 0.6, 0.6, n=8), ring(-2.1, -2.0, 0.85, 0.5, 0.5, n=8), ring(-2.1, -2.0, 1.3, 0.85, 0.85, n=8)], col=[0.46, 0.31, 0.21], grain="z", dens=0.8)
k.cyl("wood", (-2.1, -2.0, 1.1), (-1.75, -2.3, 2.5), 0.07, 0.10, n=6, col=[0.60, 0.44, 0.28], dens=0.6)
k.plant((3.1, 2.9, 0.0), n=6, size=1.6, seed=1)
k.plant((-3.2, -2.9, 0.0), n=5, size=1.3, seed=2)


def deform(v):
    z = max(v.z, 0.0)
    a = math.radians(-1.0 * z)
    x, y = v.x * math.cos(a) - v.y * math.sin(a), v.x * math.sin(a) + v.y * math.cos(a)
    return (x - 0.020 * z ** 1.5, y + 0.010 * z ** 1.5, v.z)


k.deform = deform
k.meta.update(note="glow: none on this one (a granary has no lamp); house_granary_glow is empty",
              close=[(0.0, -1.5, 3.9), (3.6, -7.4, 2.6)], close_label="posts, rat guards, door and ladder", distance=13.0, look=0.5, figure=(2.6, -4.4))
k.build()
