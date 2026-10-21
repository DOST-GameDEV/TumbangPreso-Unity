"""PAYO, the valley's HEAD as ONE designed piece: cliff, lip, waterfall, plunge pool, outlet (Blender 5, headless).

  blender -b --factory-startup --python tools/payo_pieces/head_build.py

Writes Logs/payo/pieces/calm/head/head_fall.glb (+ head_fall.png atlas, head_fall_normal.png, head_fall_height.png)
and ArtSource/payo/head_fall.blend. Review: tools/payo_pieces/head_review.py, sheet head_review.png.

PLACEMENT. Blender metres, x east, y north, z up. The file's origin is the pool's centre at z = 0: place the glb at
(0, -208, 0) with no turn and scale 1; every height in it is the map's own (water 45.0, rim 46.2, lip 73.0).

HOW IT IS MADE (Logs/payo/pieces/CALM_STYLE.md: few big shapes, contrast between surfaces, relief in the normal map):
1. The rock is ONE sculpted mass: a signed field sampled on a 0.5 m grid (a plateau height field intersected with a
   horseshoe wall whose radius varies with height in five rounded tiers, smooth-unioned with the pool bowl, a sill, the
   lip shoulders and the spring boulder, then the outlet channel cut out), turned into a mesh by OpenVDB, decimated.
   The dressed stone (the kerb round the open north rim, two posts at the outlet) is modelled block by block.
2. One 2048 atlas for all the stone: position, normal and kind are baked to float pictures, the paint and a height
   field in metres are worked out per texel in numpy (one base colour per surface with a soft top-to-foot gradient,
   moss and grass as big soft caps, the dark wet stone behind the fall, faint lining joints, one sun-disc motif per
   post), and Cycles bakes the height into a tangent-space normal map (OpenGL, green up, MikkTSpace tangents exported).
3. The water: the stream on the top (from under the spring boulder to the lip), the sheet (terrace_falls.ribbon: the
   approved fall's soft bands and strands), foam ON the pool, mist, the pool surface, the spill over the outlet sill and
   the outlet water. Materials keep the land kit's names (py_fall, py_foam, py_mist, py_water) so the game's shaders
   take them as they take fall_great; the stone is py_head_rock (painted, its own atlas), plants py_terrace_plant.
"""
import math
import os
import random
import struct
import sys
import zlib

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import terrace_lib as K       # noqa: E402
import terrace_falls as TF    # noqa: E402

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "head")
TEX = os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "terrace", "tex")
BLEND = os.path.join(ROOT, "ArtSource", "payo", "head_fall.blend")
os.makedirs(OUT, exist_ok=True)
ATLAS = 2048

# ---------------------------------------------------------------- the contract
WATER, RIM, FLOOR = 45.0, 46.2, 42.0
ZLIP = 74.0                      # the notch floor at the lip; the fall leaves from here
LIP_R = 15.8                     # the lip ledge's front, south of the pool's centre
KERB_IN, KERB_OUT, KERB_FOOT = 17.0, 20.2, 43.6
CH_FLOOR, CH_WATER, CH_END = 43.2, 44.3, 19.3     # the outlet, to the water stair at y -189 (local +19)
SILL = 44.85
POOL_R = 18.6                    # the pool's water disc, its edge buried in the kerb and the cliff foot


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def smin(a, b, k):
    """Smooth minimum (for positive-inside fields this is a rounded intersection)."""
    h = np.maximum(k - np.abs(a - b), 0.0)
    return np.minimum(a, b) - h * h / (4 * k)


def smax(a, b, k):
    return -smin(-a, -b, k)


# ---------------------------------------------------------------- noise
_tables = {}


def table(seed, n=32, dims=3):
    key = (seed, n, dims)
    if key not in _tables:
        _tables[key] = np.random.default_rng(seed).random((n,) * dims).astype(np.float32)
    return _tables[key]


def vnoise3(P, scale, seed):
    """Value noise, 0..1, smooth, on points P (..., 3) in metres."""
    T = table(seed)
    q = P / scale
    i = np.floor(q).astype(np.int64)
    f = q - i
    f = f * f * (3 - 2 * f)
    i0, i1 = i % 32, (i + 1) % 32
    out = 0.0
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                ix = (i1 if dx else i0)[..., 0]
                iy = (i1 if dy else i0)[..., 1]
                iz = (i1 if dz else i0)[..., 2]
                w = (f[..., 0] if dx else 1 - f[..., 0]) * (f[..., 1] if dy else 1 - f[..., 1]) * (f[..., 2] if dz else 1 - f[..., 2])
                out = out + T[ix, iy, iz] * w
    return out


def vnoise2(x, y, scale, seed):
    P = np.stack(np.broadcast_arrays(x, y, np.zeros_like(x) + 0.5 * scale), -1)
    return vnoise3(P, scale, seed)


def wave(phi, seed, amp):
    r = random.Random(seed)
    return sum(amp * a * np.sin(f * phi + r.uniform(0, 6.283)) for f, a in ((3, 0.55), (6, 0.35), (11, 0.2)))


# ---------------------------------------------------------------- 1. the field
JOINTS = [50.6, 56.4, 61.8, 67.2]
SETBACK = [0.0, 0.9, 0.4, 1.4, 0.9]          # per tier, from the water up: ledges where it grows, overhangs where it shrinks


def trough_half(y):
    """The notch's flat floor half-width: 8 m at the lip, narrowing to 3 m at the spring."""
    return 3.0 + 5.0 * np.clip((y + 38.0) / (38.0 - LIP_R), 0.0, 1.0) ** 0.8


def trough_centre(y):
    """A gentle S in the stream, straight for the last few metres to the lip."""
    return 2.0 * np.sin((-y - LIP_R) * 0.17) * smooth(-17.0, -23.0, y)


def field(h=0.5):
    x0, x1, y0, y1, z0, z1 = -56.0, 56.0, -70.0, 28.0, 36.0, 86.0
    xs = np.arange(x0, x1 + 1e-6, h, dtype=np.float32)
    ys = np.arange(y0, y1 + 1e-6, h, dtype=np.float32)
    zs = np.arange(z0, z1 + 1e-6, h, dtype=np.float32)
    X, Y = np.meshgrid(xs, ys, indexing="ij")
    Z = zs[None, None, :]
    r = np.hypot(X, Y)
    phi = np.arctan2(X, -Y)                          # 0 due south (behind the pool), + east
    aphi = np.abs(phi)

    # --- the plateau: the mass's top, higher to the east (hill 60..80), lower to the west (52..56)
    T0 = 76.5 + 1.8 * np.clip((-Y - 25) / 30, 0, 1) + 3.0 * smooth(0, 40, X) - 2.5 * smooth(0, 40, -X)
    T0 = T0 + 2.4 * (vnoise2(X, Y, 22.0, 3) - 0.5) - 7.0 * smooth(22.0, 46.0, np.hypot(X, Y))   # domed: rolls into the hill
    # the notch: a trough from the spring to the lip, 14 m flat, rounded walls
    hwf = trough_half(Y)
    m = (1 - smooth(hwf - 0.4, hwf + 2.6, np.abs(X - trough_centre(Y)))) * smooth(-42.5, -38.5, Y)
    Zn = ZLIP + 0.035 * np.clip(-Y - LIP_R, 0, None)
    Ht = T0 * (1 - m) + Zn * m
    # the arms come down northward to the rim (the east arm holds its height longer)
    Hn = 45.6 - 3.1 * smooth(20.6, 24.5, r)
    te, tw = smooth(-8, 12, Y), smooth(-16, 6, Y)
    t = tw + (te - tw) * smooth(-6, 6, X)
    Ht = Ht * (1 - t) + Hn * t
    # the skirt: outside the footprint the mass rolls down well below any ground
    s = ((np.abs(X) / 46.0) ** 2.4 + (np.abs(Y + 20.0) / 44.0) ** 2.4) ** (1 / 2.4)
    s = s * (1 + 0.16 * (vnoise2(X, Y, 16.0, 5) - 0.5))           # a lobed, not a ruled, outline
    q = smooth(0.74, 1.0, s)
    Ht = Ht - (Ht - 36.5) * q ** 1.6

    # --- the horseshoe wall: radius by height, in tiers
    R0 = 15.8 + 1.7 * smooth(0.6, 1.66, aphi)
    vs = 1 - smooth(1.75, 2.1, aphi)                 # north of the arms: no tiers (the dressed kerb is there)
    A = 1 - smooth(0.55, 0.70, aphi)                 # the fall's bay: a clean face behind the sheet
    zt = np.concatenate([[z0 - 1], JOINTS, [z1 + 1]])
    tierw = []                                       # soft weight of each tier at each height
    edges = [smooth(j - 0.25, j + 0.25, zs) for j in JOINTS]
    for k in range(5):
        lo = edges[k - 1] if k > 0 else np.ones_like(zs)
        hi = edges[k] if k < 4 else np.zeros_like(zs)
        tierw.append(lo - hi)
    SB = sum(SETBACK[k] * tierw[k] for k in range(5))[None, None, :]
    groove = sum(0.85 * np.exp(-((zs - j) / 0.75) ** 2) for j in JOINTS)[None, None, :]
    u = np.zeros_like(zs)
    for k in range(5):
        a, b = max(zt[k], WATER - 1), zt[k + 1]
        u = u + tierw[k] * np.clip((zs - a) / max(0.5, b - a), 0, 1)
    bulge = (0.35 * np.sin(np.pi * u))[None, None, :]
    rr = random.Random(11)
    var = 0.0
    for k in range(5):
        wk = wave(phi, 100 + k, 0.7)
        ck = 0.0
        if k >= 1:
            for _ in range(rr.choice((2, 3))):
                c = rr.choice((-1, 1)) * rr.uniform(0.62, 1.75)
                ck = ck + 1.0 * np.exp(-((phi - c) / 0.045) ** 2)
        var = var + (wk + ck)[..., None] * tierw[k][None, None, :]
    var = var * smooth(47.0, 49.0, Z)
    alcove = 2.6 * A[..., None] * smooth(45.5, 49.0, Z) * (1 - smooth(ZLIP - 2.6, ZLIP - 1.3, Z))
    Rw = R0[..., None] + vs[..., None] * (SB + var + groove - bulge) * (1 - A[..., None]) + alcove
    f = smin(Ht[..., None] - Z, r[..., None] - Rw, 1.0)

    def ell(c, rad):
        d = np.sqrt(((X[..., None] - c[0]) / rad[0]) ** 2 + ((Y[..., None] - c[1]) / rad[1]) ** 2 + ((Z - c[2]) / rad[2]) ** 2)
        return (1 - d) * min(rad)

    # the lip's two shoulders and the spring's boulder: the big rounded masses that frame the notch
    for sx in (-1, 1):
        f = smax(f, ell((sx * 14.0, -19.5, 77.2 + 1.2 * sx), (6.0, 5.5, 3.8)), 1.4)
    f = smax(f, ell((0.0, -43.0, 77.5), (9.5, 6.5, 5.5)), 1.5)
    for sx, cy, cz, rad in ((-1, -6.0, 70.0, (5.5, 6.5, 4.0)), (1, -4.0, 76.0, (6.0, 7.0, 4.5))):
        f = smax(f, ell((sx * 24.0, cy, cz), rad), 1.6)    # one rounded boss on each arm's top

    # the pool's bowl, 3 m deep at the middle
    Hb = FLOOR + 2.6 * (r / 17.0) ** 2.4
    f = smax(f, smin(Hb[..., None] - Z, 19.0 - r[..., None], 0.6), 1.0)
    # the sill across the outlet's mouth, just under the pool's surface
    box = np.maximum.reduce([np.abs(X) - 6.2 + 0 * Y, 15.4 - Y, Y - 17.2])[..., None]
    f = smax(f, -np.maximum(box, Z - SILL), 0.3)
    # the outlet channel: the water stair's own profile (flat 6 m floor, banks to 6 m half width at 45), cut through
    zz = np.broadcast_to(Z, f.shape)
    half = np.where(zz <= 45.0, 3.0 + (zz - CH_FLOOR) * (3.0 / 1.8), 6.0 + 0.4 * (zz - 45.0))
    fl = SILL + (CH_FLOOR - SILL) * smooth(17.2, 17.45, Y) - 0.07 * np.clip(Y - 19.5, 0, None)
    e = np.minimum(np.minimum(half - np.abs(X)[..., None], zz - fl[..., None]), (Y - 13.0)[..., None])
    f = smin(f, -e, 0.35)
    return f.astype(np.float32), (x0, y0, z0), h


def rock_mesh():
    import openvdb as vdb
    f, o, h = field()
    grid = vdb.FloatGrid()
    grid.copyFromArray(np.ascontiguousarray(np.clip(f, -3, 3) + 3.0))
    grid.name = "density"
    grid.transform = vdb.createLinearTransform(voxelSize=h)
    path = os.path.join(OUT, "_head_field.vdb")
    vdb.write(path, grids=[grid])
    vol = bpy.data.volumes.new("head_field"); vol.filepath = path
    vo = bpy.data.objects.new("head_field", vol); bpy.context.scene.collection.objects.link(vo)
    me = bpy.data.meshes.new("tmp"); mo = bpy.data.objects.new("tmp", me); bpy.context.scene.collection.objects.link(mo)
    md = mo.modifiers.new("v2m", "VOLUME_TO_MESH"); md.object = vo; md.threshold = 3.0; md.resolution_mode = "GRID"; md.adaptivity = 0.0
    dg = bpy.context.evaluated_depsgraph_get()
    raw = bpy.data.meshes.new_from_object(mo.evaluated_get(dg))
    bpy.data.objects.remove(mo); bpy.data.objects.remove(vo); bpy.data.volumes.remove(vol)
    os.remove(path)
    raw.transform(Matrix.Translation(Vector(o)))
    bm = bmesh.new(); bm.from_mesh(raw)
    bmesh.ops.delete(bm, geom=[fc for fc in bm.faces if all(v.co.z < 37.6 for v in fc.verts)], context="FACES")
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.to_mesh(raw); bm.free()
    ob = bpy.data.objects.new("head_fall_rock", raw); bpy.context.scene.collection.objects.link(ob)
    n0 = len(raw.polygons)
    dec = ob.modifiers.new("dec", "DECIMATE"); dec.ratio = 34000 / n0; dec.use_collapse_triangulate = True
    dg = bpy.context.evaluated_depsgraph_get()
    me2 = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    ob.modifiers.clear(); ob.data = me2; bpy.data.meshes.remove(raw)
    print("ROCK: field %s, %d triangles from the grid, %d after decimation" % (f.shape, n0, len(me2.polygons)))
    return ob


# ---------------------------------------------------------------- dressed stone: the kerb and the outlet's posts
def kerb_and_posts():
    bm = bmesh.new()
    prof = [(KERB_IN, KERB_FOOT), (KERB_OUT, KERB_FOOT), (KERB_OUT, RIM - 0.32), (KERB_OUT - 0.32, RIM), (KERB_IN + 0.32, RIM), (KERB_IN, RIM - 0.32)]
    mid = ((KERB_IN + KERB_OUT) / 2, (KERB_FOOT + RIM) / 2)

    def ring(a, shrink):
        pts = []
        for (pr, pz) in prof:
            pr2 = mid[0] + (pr - mid[0]) * (1 - shrink[0])
            pz2 = pz if pz <= KERB_FOOT + 0.01 else mid[1] + (pz - mid[1]) * (1 - shrink[1])
            pts.append(bm.verts.new((pr2 * math.sin(a), pr2 * math.cos(a), pz2)))
        return pts

    a_mouth = math.asin(6.6 / KERB_IN)
    a_end = math.radians(80.0)
    for side in (-1, 1):
        cuts = np.linspace(a_mouth, a_end, 4)
        for b0, b1 in zip(cuts[:-1], cuts[1:]):
            n = max(3, int(math.degrees(b1 - b0) / 3))
            d = 0.14 / 18.6
            angs = [b0, b0 + d] + list(np.linspace(b0 + d, b1 - d, n)[1:-1]) + [b1 - d, b1]
            rings = [ring(side * a, (0.08, 0.06) if k in (0, len(angs) - 1) else (0, 0)) for k, a in enumerate(angs)]
            for k in range(len(rings) - 1):
                for i in range(len(prof)):
                    j = (i + 1) % len(prof)
                    f = bm.faces.new((rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]))
            bm.faces.new(rings[0]); bm.faces.new(list(reversed(rings[-1])))
    # the posts: a tapered pillar with a cap, one each side of the mouth, on the kerb's end blocks
    for sx in (-1, 1):
        cx, cy = sx * 8.3, 17.2
        for (z0, z1, w0, w1, bev) in ((RIM - 0.25, 50.6, 1.62, 1.44, 0.12), (50.55, 51.15, 2.1, 2.0, 0.1)):
            res = bmesh.ops.create_cube(bm, size=1.0)
            vs = res["verts"]
            for v in vs:
                top = v.co.z > 0
                w = w1 if top else w0
                v.co = Vector((cx + v.co.x * w, cy + v.co.y * w, z1 if top else z0))
            bmesh.ops.bevel(bm, geom=list({e for v in vs for e in v.link_edges}), offset=bev, segments=1, affect="EDGES", profile=0.5, clamp_overlap=True)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new("kerb"); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("kerb", me); bpy.context.scene.collection.objects.link(ob)
    return ob


# ---------------------------------------------------------------- materials
def image(path, colour=True):
    im = bpy.data.images.load(path, check_existing=True)
    im.colorspace_settings.name = "sRGB" if colour else "Non-Color"
    return im


def material(name, tex=None, alpha=False, colour=(1, 1, 1, 1), normal=None, two_sided=False):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    b.inputs["Roughness"].default_value = 0.9
    b.inputs["Base Color"].default_value = colour
    if tex:
        n = nt.nodes.new("ShaderNodeTexImage"); n.image = image(tex)
        nt.links.new(n.outputs["Color"], b.inputs["Base Color"])
        if alpha:
            nt.links.new(n.outputs["Alpha"], b.inputs["Alpha"])
            m.surface_render_method = "BLENDED"
    elif colour[3] < 1:
        b.inputs["Alpha"].default_value = colour[3]
        m.surface_render_method = "BLENDED"
    if normal:
        n = nt.nodes.new("ShaderNodeTexImage"); n.image = image(normal, False)
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(n.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    m.use_backface_culling = not two_sided
    return m


# ---------------------------------------------------------------- PNG writers (no PIL inside Blender)
def write_png(path, arr):
    """arr: (h, w) uint16 grey, or (h, w, 3) uint8 RGB; row 0 is the TOP."""
    h, w = arr.shape[:2]
    if arr.ndim == 2:
        raw = arr.astype(">u2").tobytes(); bpp, ctype, depth = w * 2, 0, 16
    else:
        raw = arr.astype(np.uint8).tobytes(); bpp, ctype, depth = w * 3, 2, 8
    rows = b"".join(b"\0" + raw[i * bpp:(i + 1) * bpp] for i in range(h))

    def chunk(t, d):
        c = struct.pack(">I", len(d)) + t + d
        return c + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    with open(path, "wb") as fh:
        fh.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, depth, ctype, 0, 0, 0))
                 + chunk(b"IDAT", zlib.compress(rows, 9)) + chunk(b"IEND", b""))


# ---------------------------------------------------------------- 2. the atlas
def bake_setup():
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 1
    sc.render.bake.margin = 8
    sc.render.bake.use_clear = True


def bake_float(ob, socket_fn, name):
    """Bake an emission made by socket_fn(node_tree) to a float picture; returns (h, w, 4) with row 0 at the BOTTOM."""
    m = bpy.data.materials.new("bake_" + name); m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        if n.type != "OUTPUT_MATERIAL":
            nt.nodes.remove(n)
    out = next(n for n in nt.nodes if n.type == "OUTPUT_MATERIAL")
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(socket_fn(nt), em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    img = bpy.data.images.new("bake_" + name, ATLAS, ATLAS, alpha=True, float_buffer=True)
    img.colorspace_settings.name = "Non-Color"
    tn = nt.nodes.new("ShaderNodeTexImage"); tn.image = img
    nt.nodes.active = tn
    ob.data.materials.clear(); ob.data.materials.append(m)
    bpy.ops.object.bake(type="EMIT")
    a = np.empty(ATLAS * ATLAS * 4, np.float32); img.pixels.foreach_get(a)
    return a.reshape(ATLAS, ATLAS, 4)


def paint(P, N, KND, valid):
    """Colour (sRGB 0..1) and height (metres) per texel."""
    x, y, z = P[..., 0], P[..., 1], P[..., 2]
    nz = N[..., 2]
    r = np.hypot(x, y)
    up = smooth(0.5, 0.82, nz)
    g = np.clip((z - 44.0) / 34.0, 0, 1) ** 0.85
    LOW, HIGH = np.array([0.31, 0.35, 0.31]), np.array([0.44, 0.475, 0.39])
    col = LOW + (HIGH - LOW) * g[..., None]
    col = col * (1 + 0.08 * (vnoise3(P, 24.0, 21) - 0.5))[..., None]
    col = col * (1 - 0.08 * np.clip(-nz, 0, 1))[..., None]                          # undersides a touch darker
    # the dark wet stone behind the fall
    wet = (1 - smooth(8.5, 11.5, np.abs(x))) * (1 - smooth(19.0, 21.5, r)) * (1 - smooth(72.4, 73.4, z)) * (1 - smooth(-9.0, -5.0, y))
    col = col + (col * 0.74 + np.array([0.0, 0.012, 0.03]) - col) * (0.85 * wet)[..., None]
    band = np.exp(-((z - 45.35) / 0.6) ** 2) * (1 - smooth(21, 24, r))               # a damp band at the waterline
    col = col * (1 - 0.10 * band)[..., None]
    under = 1 - smooth(44.6, 45.0, z)
    col = col + (np.array([0.37, 0.42, 0.37]) - col) * (0.6 * under)[..., None]
    # moss on the ledges of the face: a few big soft patches
    MOSS = np.array([0.37, 0.50, 0.25])
    patch = smooth(0.40, 0.60, vnoise3(P, 9.0, 31))
    mo = up * patch * smooth(46.4, 47.4, z) * (1 - smooth(70.0, 72.5, z))
    col = col + (MOSS - col) * (0.9 * mo)[..., None]
    # grass on the top and the outer shoulders, as the hillside's own colour
    GRASS, SIDE = np.array([0.35, 0.60, 0.20]), np.array([0.33, 0.48, 0.22])
    bed = (1 - smooth(8.0, 10.0, np.abs(x))) * (1 - smooth(-15.6, -14.6, y)) * smooth(-41.5, -39.5, y) * (1 - smooth(74.6, 75.6, z))
    top = np.maximum(smooth(19.5, 23.0, r), smooth(70.0, 72.5, z)) * (1 - bed)
    edge = smooth(0.35, 0.65, vnoise3(P, 6.0, 41) * 0.5 + up * 0.75)                 # a soft, lobed edge to the cap
    gw = top * edge
    outer = smooth(23.0, 28.0, r) * smooth(46.5, 48.0, z) * (1 - bed)
    gcol = SIDE + (GRASS - SIDE) * up[..., None]
    col = col + (gcol - col) * np.maximum(gw, outer * 0.85)[..., None]
    col = col + (np.array([0.40, 0.43, 0.38]) - col) * (0.8 * bed)[..., None]        # the stream's bed
    # the dressed stone: the kerb, the posts, the lined outlet and its sill
    lining = (1 - smooth(7.0, 7.6, np.abs(x))) * smooth(13.4, 14.2, y) * (1 - smooth(46.3, 46.8, z)) * smooth(14.5, 15.0, r)
    dressed = np.clip(np.maximum(KND, lining), 0, 1)
    sg = np.clip((z - 43.0) / 8.0, 0, 1)
    STONE = np.array([0.60, 0.575, 0.52]) + (np.array([0.69, 0.66, 0.60]) - np.array([0.60, 0.575, 0.52])) * sg[..., None]
    STONE = STONE * (1 + 0.05 * (vnoise3(P, 7.0, 51) - 0.5))[..., None]
    STONE = STONE + (np.array([0.47, 0.49, 0.44]) - STONE) * (0.55 * under)[..., None]
    kmoss = up * smooth(0.45, 0.62, vnoise3(P, 5.0, 61)) * smooth(15.0, 9.0, np.abs(y)) * 0.75   # moss where the kerb meets the arms
    STONE = STONE + (MOSS - STONE) * kmoss[..., None]
    col = col + (STONE - col) * dressed[..., None]
    # the motif: a bold sun disc on each post's north face
    face = (KND > 0.5) & (N[..., 1] > 0.7) & (np.abs(np.abs(x) - 8.3) < 0.9) & (z > 47.2) & (z < 50.5)
    d = np.hypot(np.abs(x) - 8.3, z - 49.0)
    ang = np.arctan2(z - 49.0, np.abs(x) - 8.3)
    ray = (np.abs(((ang / (np.pi / 4)) + 0.5) % 1.0 - 0.5) < 0.17) & (d > 0.66) & (d < 0.96)
    TERRA, CREAM = np.array([0.72, 0.39, 0.28]), np.array([0.87, 0.81, 0.67])
    disc, ring = face & (d < 0.48), face & (d >= 0.48) & (d < 0.66)
    col[disc] = TERRA; col[ring] = CREAM; col[face & ray] = TERRA

    # height, metres, low frequency only
    Pw = P + 1.2 * np.stack([vnoise3(P, 7.0, 71) - 0.5] * 3, -1)
    strata = 0.07 * np.sin(2 * np.pi * Pw[..., 2] / 4.2) * (1 - np.abs(nz)) ** 2 * (0.4 + vnoise3(P, 9.0, 83))
    lumps = 0.22 * (vnoise3(P, 4.0, 81) - 0.5) + 0.07 * (vnoise3(P, 1.6, 91) - 0.5)
    H = strata + lumps
    H = H + (0.05 * (vnoise3(P, 1.8, 95) - 0.5) - H) * np.maximum(gw, outer)
    jy = np.abs((y / 2.4 + 0.5) % 1.0 - 0.5) * 2.4
    Hd = 0.015 * (vnoise3(P, 3.0, 97) - 0.5) - 0.025 * np.exp(-(jy / 0.07) ** 2) * lining
    H = H + (Hd - H) * dressed
    H[disc] += 0.03; H[ring] += 0.015; H[face & ray] += 0.02
    col = np.clip(col, 0, 1)
    col[~valid] = 0.45
    H[~valid] = 0.0
    return col, H


def bake_normal(ob, height_img):
    m = bpy.data.materials.new("bake_normal"); m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    hn = nt.nodes.new("ShaderNodeTexImage"); hn.image = height_img; hn.interpolation = "Cubic"
    bump = nt.nodes.new("ShaderNodeBump"); bump.inputs["Strength"].default_value = 1.0; bump.inputs["Distance"].default_value = 5.0
    nt.links.new(hn.outputs["Color"], bump.inputs["Height"]); nt.links.new(bump.outputs["Normal"], b.inputs["Normal"])
    img = bpy.data.images.new("bake_n", ATLAS, ATLAS, alpha=False, float_buffer=True); img.colorspace_settings.name = "Non-Color"
    tn = nt.nodes.new("ShaderNodeTexImage"); tn.image = img; nt.nodes.active = tn
    ob.data.materials.clear(); ob.data.materials.append(m)
    sc = bpy.context.scene
    sc.render.bake.normal_space = "TANGENT"
    sc.cycles.samples = 4
    bpy.ops.object.bake(type="NORMAL")
    a = np.empty(ATLAS * ATLAS * 4, np.float32); img.pixels.foreach_get(a)
    return a.reshape(ATLAS, ATLAS, 4)


def atlas(ob):
    bake_setup()
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True); bpy.context.view_layer.objects.active = ob

    def pos(nt):
        return nt.nodes.new("ShaderNodeNewGeometry").outputs["Position"]

    def nrm(nt):
        return nt.nodes.new("ShaderNodeNewGeometry").outputs["Normal"]

    def kind(nt):
        a = nt.nodes.new("ShaderNodeAttribute"); a.attribute_name = "kind"
        return a.outputs["Color"]
    P = bake_float(ob, pos, "pos")[..., :3]
    N = bake_float(ob, nrm, "nrm")[..., :3]
    KND = bake_float(ob, kind, "kind")[..., 0]
    valid = np.linalg.norm(N, axis=-1) > 0.5
    N = N / np.maximum(np.linalg.norm(N, axis=-1, keepdims=True), 1e-6)
    col, H = paint(P, N, KND, valid)
    print("ATLAS: %.1f%% of the texels used" % (100 * valid.mean()))
    # Blender's rows run bottom-up; the PNGs top-down
    rgb = (np.clip(col, 0, 1) * 255 + 0.5).astype(np.uint8)[::-1]
    write_png(os.path.join(OUT, "head_fall.png"), rgb)
    lo, hi = -0.15, 0.15
    write_png(os.path.join(OUT, "head_fall_height.png"), (np.clip((H - lo) / (hi - lo), 0, 1) * 65535 + 0.5).astype(np.uint16)[::-1])
    himg = bpy.data.images.new("height_m", ATLAS, ATLAS, alpha=False, float_buffer=True); himg.colorspace_settings.name = "Non-Color"
    hp = np.zeros((ATLAS, ATLAS, 4), np.float32); hp[..., 0] = hp[..., 1] = hp[..., 2] = H; hp[..., 3] = 1
    himg.pixels.foreach_set(hp.ravel())
    nmap = bake_normal(ob, himg)[..., :3]
    nmap[~valid] = (0.5, 0.5, 1.0)
    write_png(os.path.join(OUT, "head_fall_normal.png"), (np.clip(nmap, 0, 1) * 255 + 0.5).astype(np.uint8)[::-1])
    for im in [i for i in bpy.data.images if i.name.startswith("bake") or i.name == "height_m"]:
        bpy.data.images.remove(im)
    for mt in [mm for mm in bpy.data.materials if mm.name.startswith("bake")]:
        bpy.data.materials.remove(mt)


# ---------------------------------------------------------------- 3. the water
FALL_TOP_Y = -14.05              # the sheet's top at its sides; its middle lies SAG further south (the lip is an arc)
SAG = 2.3                        # so the sheet is concave like the horseshoe, 0.6 to 0.8 m back from the lip all along


def sheet_top(x):
    return FALL_TOP_Y - SAG * max(0.0, 1.0 - (x / 8.6) ** 2)
STREAM_Z = 0.3                   # the stream's surface over its bed


def stream_z(y):
    return ZLIP + 0.035 * max(0.0, -y - LIP_R) + STREAM_Z


def water_objects(r):
    m_fall = material("py_fall", os.path.join(TEX, "py_fall.png"), alpha=True, two_sided=True)
    m_foam = material("py_foam", os.path.join(TEX, "py_foam.png"))
    m_mist = material("py_mist", os.path.join(TEX, "py_foam.png"), two_sided=True)
    m_water = material("py_water", None, colour=(0.30, 0.62, 0.66, 0.85))
    obs = []
    # --- the sheet: built as terrace_falls builds it (pouring toward -Y), then turned to pour north into the pool
    Hf = stream_z(sheet_top(0.0)) - 0.02 - WATER
    w0, w1, throw, tile, cols, rows = 16.6, 18.4, 4.6, 10.0, 14, 24
    sh = K.MB()
    uw = max(0.3, w1 / (tile / 4.0) * 0.5)
    TF.ribbon(sh, Hf, throw, w0, w1, 0.0, 1.0, 0.0, 1.0, 0.0, (0.0, uw), tile, cols, rows, r, bulge=0.0, ragged=0.04, lip_run=0.0)
    for k in range(9):
        uc = -0.8 + 1.6 * (k + r.uniform(0.2, 0.8)) / 9
        fr = r.uniform(0.12, 0.22)
        u0 = r.uniform(0, 1)
        TF.ribbon(sh, Hf * r.uniform(0.97, 1.0), throw * r.uniform(1.0, 1.10), w0, w1 * r.uniform(1.0, 1.08), uc, fr, r.uniform(0.02, 0.08), r.uniform(0.84, 1.0),
                  (0.015 + 0.010 * k) * w1 * r.uniform(1.0, 1.4), (u0, u0 + uw * fr * 1.3), tile * r.uniform(0.8, 1.0), 4, rows, r, bulge=0.2, ragged=0.10)
    # the spill over the outlet's sill: a short curved sheet from the pool's surface to the outlet's water
    sp = K.MB()
    for i in range(5):
        for j in range(8):
            def pt(ii, jj):
                t = ii / 5
                hw = 6.1 - 1.15 * t
                u = -1 + 2 * jj / 8
                return (u * hw, 17.05 + 0.6 * math.sqrt(t), WATER + 0.02 - 0.82 * t)
            tint = lambda ii: (0.62 + 0.3 * ii / 5, 0.86 + 0.1 * ii / 5, 0.90 + 0.08 * ii / 5, 1)
            sp.face([pt(i, j), pt(i, j + 1), pt(i + 1, j + 1), pt(i + 1, j)],
                    [(j / 8 * 0.6, 1 - i / 5 * 0.15), ((j + 1) / 8 * 0.6, 1 - i / 5 * 0.15), ((j + 1) / 8 * 0.6, 1 - (i + 1) / 5 * 0.15), (j / 8 * 0.6, 1 - (i + 1) / 5 * 0.15)],
                    [tint(i), tint(i), tint(i + 1), tint(i + 1)], smooth=True)
    turn = Matrix.Translation((0.0, FALL_TOP_Y, WATER)) @ Matrix.Rotation(math.pi, 4, "Z")
    sheet = K.MB(); sheet.add(sh, turn)
    sheet.v = [(p[0], p[1] - SAG * max(0.0, 1.0 - (p[0] / 8.6) ** 2), p[2]) for p in sheet.v]
    sheet.add(sp)
    obs.append(sheet.obj("head_fall_sheet", m_fall))
    # --- the foam: a lumpy white skirt sitting ON the pool where the sheet lands, and the boil under it
    foot = Vector((0.0, sheet_top(0.0) + throw, 0.0))
    fo = K.MB()
    white = lambda nn, p: (1, 1, 1, 1) if nn.z > 0.1 else (0.74, 0.90, 0.94, 1)
    n = 22
    for k in range(n):
        a = math.pi * (k + 0.5) / n
        c = foot + Vector((-math.cos(a) * w1 * 0.56, math.sin(a) * (w1 * 0.28) + 0.4, 0))
        s = w1 * r.uniform(0.065, 0.10)
        K.blob(fo, (c.x, c.y, WATER + s * 0.12), (s, s * 0.85, s * 0.42), r, seg=8, rings=5, jitter=0.10, flat_bottom=WATER - 0.03, colour=white)
    for k in range(13):
        c = foot + Vector((-0.45 * w1 + 0.9 * w1 * (k + r.uniform(0.2, 0.8)) / 13, r.uniform(-0.4, 1.6), 0))
        s = w1 * r.uniform(0.075, 0.12)
        K.blob(fo, (c.x, c.y, WATER + s * 0.22), (s, s * 0.8, s * 0.6), r, seg=8, rings=5, jitter=0.10, flat_bottom=WATER - 0.03, colour=white)
    for sx in (-1, 1):                                                  # a little white where the spill meets the outlet
        for k in range(3):
            s = r.uniform(0.55, 0.8)
            K.blob(fo, (sx * r.uniform(0.6, 3.6), 17.9 + r.uniform(0, 0.6), CH_WATER + s * 0.15), (s, s * 0.8, s * 0.5), r, seg=8, rings=4, jitter=0.1, flat_bottom=CH_WATER - 0.03, colour=white)
    obs.append(fo.obj("head_fall_foam", m_foam))
    # --- the mist: a few soft puffs drifting off the foot
    mi = K.MB()
    for k in range(5):
        a = math.pi * (0.12 + 0.76 * (k + r.uniform(0.2, 0.8)) / 5)
        dd = w1 * r.uniform(0.45, 0.7)
        c = foot + Vector((-math.cos(a) * dd, math.sin(a) * dd * 0.45 + 1.5, WATER + w1 * r.uniform(0.12, 0.28)))
        TF.puff(mi, c, w1 * r.uniform(0.13, 0.19), r)
    for sx in (-1, 1):                                                  # and two drifting up beside the sheet
        c = foot + Vector((sx * w1 * r.uniform(0.46, 0.52), r.uniform(0.8, 1.6), WATER + r.uniform(3.8, 5.0)))
        TF.puff(mi, c, w1 * r.uniform(0.11, 0.14), r)
    obs.append(mi.obj("head_fall_mist", m_mist))
    # --- the still water: the pool (its edge buried in the kerb and the cliff's foot), the stream, the outlet
    wa = K.MB()
    ring_ = []
    for k in range(72):
        a = 2 * math.pi * k / 72
        p = [POOL_R * math.cos(a), POOL_R * math.sin(a)]
        if p[1] > 17.1:
            p[1] = 17.1
        ring_.append((p[0], p[1], WATER))
    wa.face(ring_, [(p[0] / 8, p[1] / 8) for p in ring_])
    nc, nr = 10, 26                                                     # the stream: its end follows the sheet's arc
    grid = []
    for i in range(nr + 1):
        row = []
        for j in range(nc + 1):
            u = -1 + 2 * j / nc
            ye = sheet_top(u * 8.0) + 0.06
            y = -40.0 + (ye + 40.0) * (i / nr)
            x = u * (float(trough_half(y)) + 0.4)
            ye = sheet_top(x) + 0.06
            y = -40.0 + (ye + 40.0) * (i / nr)
            x = u * (float(trough_half(y)) + 0.4) + float(trough_centre(y))
            row.append((x, y, stream_z(y)))
        grid.append(row)
    for i in range(nr):
        for j in range(nc):
            q = [grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]]
            wa.face(q, [(p[0] / 8, p[1] / 8) for p in q])
    wa.face([(-5.0, 17.45, CH_WATER), (5.0, 17.45, CH_WATER), (5.0, CH_END + 0.3, CH_WATER), (-5.0, CH_END + 0.3, CH_WATER)],
            [(-5 / 8, 17.45 / 8), (5 / 8, 17.45 / 8), (5 / 8, (CH_END + 0.3) / 8), (-5 / 8, (CH_END + 0.3) / 8)])
    obs.append(wa.obj("head_fall_water", m_water))
    return obs, dict(sheet_top=(0.0, round(sheet_top(0.0), 2), round(Hf + WATER, 2)), foot=(0.0, round(foot.y, 2), WATER), w0=w0, w1=w1)


# ---------------------------------------------------------------- plants: vines on the face, tufts along the top edge
def plants(rock, r):
    m_pl = material("py_terrace_plant", os.path.join(TEX, "py_terrace_plant.png"), two_sided=True)
    dg = bpy.context.evaluated_depsgraph_get()
    bvh = BVHTree.FromObject(rock, dg)
    pl = K.MB()
    CELLS = 8

    def hit_face(phi, z):
        d = Vector((math.sin(phi), -math.cos(phi), 0))
        o = Vector((0, 0, z))
        loc, nrm, _, _ = bvh.ray_cast(o, d, 60.0)
        return loc, nrm

    def hanging_leaf(base, down, nrm, length, width, cell, shade):
        side = down.cross(nrm).normalized()
        m = base + down * (length * 0.5) + nrm * (length * 0.12)
        t = base + down * length + nrm * (length * 0.05)
        u0, u1 = (cell + 0.06) / CELLS, (cell + 0.94) / CELLS
        um = (u0 + u1) / 2
        c, dk = (shade, shade, shade, 1), (shade * 0.72, shade * 0.72, shade * 0.72, 1)
        lft, rgt = m - side * width / 2, m + side * width / 2
        pl.face([base, rgt, m], [(um, 0.02), (u1, 0.5), (um, 0.5)], [dk, c, c])
        pl.face([base, m, lft], [(um, 0.02), (um, 0.5), (u0, 0.5)], [dk, c, c])
        pl.face([m, rgt, t], [(um, 0.5), (u1, 0.5), (um, 0.98)], [c, c, c])
        pl.face([m, t, lft], [(um, 0.5), (um, 0.98), (u0, 0.5)], [c, c, c])

    vines = 0
    for phi_d, z in ((-58, 66.6), (-44, 61.4), (-74, 56.0), (47, 66.8), (63, 61.2), (78, 66.6), (-86, 61.4), (36, 56.0)):
        for k in range(r.randint(5, 8)):
            ph = math.radians(phi_d + r.uniform(-4.5, 4.5))
            loc, nrm = hit_face(ph, z + r.uniform(-0.3, 0.5))
            if loc is None:
                continue
            length = r.uniform(2.2, 6.0)
            p = loc + nrm * 0.25
            steps = int(length / 0.55)
            cell = r.choice((3, 4, 5))
            for s in range(steps):
                q = p - Vector((0, 0, 0.55))
                l2, n2, _, _ = bvh.ray_cast(q + nrm * 2.0, -nrm, 4.0)
                if l2 is not None:
                    q = l2 + n2 * 0.22
                    nrm = n2
                down = (q - p).normalized()
                side = 1 if s % 2 else -1
                lw = r.uniform(0.38, 0.55) * (1 - 0.35 * s / max(1, steps))
                tilt = (down + down.cross(nrm).normalized() * 0.55 * side).normalized()
                hanging_leaf(p, tilt, nrm if nrm.z < 0.9 else Vector((0, 1, 0)), lw * 1.6, lw, cell, r.uniform(0.85, 1.0))
                p = q
            vines += 1
    tufts = 0
    for k in range(70):                                                 # along the horseshoe's top edge and the notch
        ph = math.radians(r.uniform(-112, 112))
        if abs(math.degrees(ph)) < 24:
            continue
        rad = 18.8 + r.uniform(0.0, 4.5)
        o = Vector((rad * math.sin(ph), -rad * math.cos(ph), 95.0))
        loc, nrm, _, _ = bvh.ray_cast(o, Vector((0, 0, -1)), 80.0)
        if loc is None or nrm.z < 0.55 or loc.z < 50.0:
            continue
        K.tuft(pl, (loc.x, loc.y, loc.z - 0.1), r, n=r.randint(5, 7), h=r.uniform(0.8, 1.4), w=0.22, lean=0.55, cell=r.choice((3, 4)), shade=r.uniform(0.85, 1.0))
        tufts += 1
    for sx in (-1, 1):                                                  # ferns beside the lip, where the water leaves
        for k in range(5):
            o = Vector((sx * r.uniform(8.6, 12.5), r.uniform(-19.0, -16.0), 95.0))
            loc, nrm, _, _ = bvh.ray_cast(o, Vector((0, 0, -1)), 80.0)
            if loc is None:
                continue
            a0 = r.uniform(0, 6.28)
            for i in range(7):
                a = a0 + i * 6.283 / 7
                K.leaf(pl, loc - Vector((0, 0, 0.1)), Vector((math.cos(a), math.sin(a), 0)), r.uniform(1.3, 2.0), 0.55, 4 if i % 2 else 3, r, up=0.42, shade=r.uniform(0.85, 1.0))
    print("PLANTS: %d vines, %d tufts" % (vines, tufts))
    return pl.obj("head_fall_plants", m_pl)


# ---------------------------------------------------------------- checks
def inside(bvh, p):
    """A point is inside the closed-ish rock if a ray up hits a face whose normal points up at it from inside."""
    loc, nrm, _, _ = bvh.ray_cast(p, Vector((0, 0, 1)), 200.0)
    return loc is not None and nrm.z > 0 and (loc - p).length > 0.02


def checks(rock, water_obs):
    dg = bpy.context.evaluated_depsgraph_get()
    bvh = BVHTree.FromObject(rock, dg)
    rep = {}
    for o in water_obs:
        pts = [o.matrix_world @ v.co for v in o.data.vertices]
        if o.name.endswith("sheet"):
            bad = [p for p in pts if WATER + 1.0 < p.z < ZLIP - 1.5 and p.y < 16 and inside(bvh, p)]
            rep["sheet vertices inside the rock (between pool and lip)"] = len(bad)
            rep["sheet inside sample"] = [tuple(round(c, 1) for c in p) for p in bad[:12]]
        if o.name.endswith("foam"):
            rep["foam lowest z"] = round(min(p.z for p in pts), 3)
    # the pool's edge: sample its ring, each point must be under rock or kerb (no gap of open water to the void)
    gaps = 0
    for k in range(180):
        a = 2 * math.pi * k / 180
        p = Vector((POOL_R * math.cos(a), POOL_R * math.sin(a), WATER + 0.05))
        if p.y > 17.0:
            continue
        loc, _, _, _ = bvh.ray_cast(p, Vector((0, 0, 1)), 40.0)
        if loc is None:
            gaps += 1
    rep["pool edge points with nothing over them"] = gaps
    return rep


# ---------------------------------------------------------------- build
def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    K._mats.clear()
    r = random.Random(7)
    rock = rock_mesh()
    kerb = kerb_and_posts()
    for ob, v in ((rock, 0.0), (kerb, 1.0)):
        a = ob.data.color_attributes.new("kind", "FLOAT_COLOR", "POINT")
        a.data.foreach_set("color", np.tile([v, v, v, 1.0], len(ob.data.vertices)).astype(np.float32))
    bpy.ops.object.select_all(action="DESELECT")
    rock.select_set(True); kerb.select_set(True); bpy.context.view_layer.objects.active = rock
    bpy.ops.object.join()
    me = rock.data
    for p in me.polygons:
        p.use_smooth = True
    me.set_sharp_from_angle(angle=math.radians(42))
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.003, area_weight=0.0, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    atlas(rock)
    me.color_attributes.remove(me.color_attributes["kind"])
    m_rock = material("py_head_rock", os.path.join(OUT, "head_fall.png"), normal=os.path.join(OUT, "head_fall_normal.png"))
    me.materials.clear(); me.materials.append(m_rock)
    water, wmeta = water_objects(r)
    pl = plants(rock, r)
    rep = checks(rock, water)
    obs = [rock] + water + [pl]
    rock.data.name = "head_fall_rock"
    for o in obs:
        bm = bmesh.new(); bm.from_mesh(o.data)
        bmesh.ops.triangulate(bm, faces=bm.faces[:])
        bm.to_mesh(o.data); bm.free()
        o.data.name = o.name
        K.activate(o.data)
    bpy.ops.object.select_all(action="DESELECT")
    for o in obs:
        o.select_set(True)
    path = os.path.join(OUT, "head_fall.glb")
    kw = dict(filepath=path, use_selection=True, export_format="GLB", export_yup=True, export_apply=True, export_tangents=True)
    try:
        bpy.ops.export_scene.gltf(export_vertex_color="ACTIVE", export_all_vertex_colors=False, **kw)
    except TypeError:
        bpy.ops.export_scene.gltf(**kw)
    tris = {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons) for o in obs}
    lo = Vector((1e9,) * 3); hi = Vector((-1e9,) * 3)
    for c in rock.bound_box:
        lo = Vector(map(min, lo, Vector(c))); hi = Vector(map(max, hi, Vector(c)))
    print("HEAD_FALL", path)
    print("  triangles", tris, "total", sum(tris.values()))
    print("  rock bounds", tuple(round(v, 1) for v in lo), tuple(round(v, 1) for v in hi))
    print("  fall", wmeta)
    print("  checks", rep)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND, compress=True)


main()
