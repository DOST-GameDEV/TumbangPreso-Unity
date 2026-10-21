"""Model Kanto's street traffic in Blender: sedans, a hatchback, a taxi, a pickup, a delivery van,
a city bus, a Filipino JEEPNEY and a TRICYCLE, and render review images of them.

  blender -b --python tools/author_kanto_vehicles.py -- --preview N

Writes Logs/kanto-blender/vehicles_lineup_vN.png (3/4 lineup), vehicles_eye_vN.png (the game's
eye: 1.25 m up, 95 degrees across) and vehicles_jeepney_vN.png (a close-up of the jeepney).
Saves no .blend: the map build calls build_vehicle(name) itself.

API
  VEHICLES            name -> spec (kind, paint, ...); every key builds
  build_vehicle(name) -> the bpy collection (via author_kanto_city.kit), origin at the vehicle's
                         ground centre, facing +X, tyres sunk 1 cm below z = 0
  PALETTE_ADD         the new material names, merged into author_kanto_models.PALETTE on import

WHY THIS EXISTS. The owner's main reference (Tiny Talisman, "Stylized Modern City") has streets
full of chunky cartoon traffic and ours were empty (docs/KANTO_DESIGN_GUIDE.md section 10, "Street
life"). This is a Filipino game, so next to the sedans, taxi, van and bus the street gets the two
vehicles a Filipino player recognises at a glance: the JEEPNEY (long hood with chrome horses,
chrome grille, open-sided passenger body with benches and a rear doorway, route board on the
roof) and the TRICYCLE (a motorcycle with a covered sidecar and a roof over the driver, "TODA"
painted on the side).

HOW THEY ARE BUILT, AND WHY:
  * CHUNKY, NOT REALISTIC (Art_Direction.md section 0). Big wheels, short overhangs, tall cabins,
    fat rounded bodies. Bodies are SIDE PROFILES (x, z) with filleted corners, extruded across
    the car; the cabin tapers inward toward the roof (tumblehome), and a big live Bevel rounds
    every long edge so the body reads like a toy, not a box.
  * WHEEL ARCHES are cut into the body profile and ringed by a bulging flare that stands 4 cm
    proud of the side.
  * GLASS is dark and sits PROUD of the paint by 12 mm (windscreen and rear screen as panels on
    the cabin's slope, side windows as a thin slab following the cabin taper), so a painted frame
    and pillars show all round.
  * NO TWO SURFACES SHARE A PLANE (design guide 2): every attached part penetrates what it sits on,
    trim stands proud of the face it runs along, and tyres sink 1 cm below the road.
  * ROLE HUES (Art_Direction.md section 1): no orange near #f87020 and no blue near #0080e8.
    Paints are reds, creams, mint, mustard, green and taxi yellow; glass is near-black teal.
  * Materials reach Unity by NAME, so new ones are in PALETTE_ADD; existing names are reused
    wherever one fits (steel, lamp_glass, signal_red, lane_yellow, sign_green, plaster_*).
"""
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_kanto_models as K          # noqa: E402  (Buf, materials, lighting)

# author_kanto_city is imported lazily (inside the functions that need it), so the city script
# can import this module at its top without a circular import.

PALETTE_ADD = {
    "car_red":      (0.58, 0.06, 0.05),   # a deep cherry, well clear of offence orange
    "tyre":         (0.045, 0.045, 0.05),
    "glass_dark":   (0.030, 0.060, 0.062),  # near-black teal, the street's window colour in shade
    "hubcap":       (0.70, 0.70, 0.67),
    "jeep_chrome":  (0.66, 0.68, 0.66),
    "chassis":      (0.07, 0.07, 0.075),
    "vinyl_dark":   (0.20, 0.06, 0.04),   # seats: oxblood vinyl
    "plate":        (0.94, 0.92, 0.84),
}
K.PALETTE.update(PALETTE_ADD)

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))


# ------------------------------------------------------------------ geometry helpers

def fillet(pts, n=4):
    """Round the corners of a closed 2D polygon. `pts` are (x, z, radius); radius 0 keeps the
    corner sharp. Returns (points, tangents): tangents[i] = (where the arc at vertex i starts,
    where it ends), so the straight part of original edge i runs tangents[i][1] ->
    tangents[i+1][0]."""
    P = [Vector((p[0], p[1])) for p in pts]
    N = len(P)
    out, tang = [], []
    for i in range(N):
        a, p, c = P[i - 1], P[i], P[(i + 1) % N]
        r = pts[i][2]
        u, v = a - p, c - p
        lu, lv = u.length, v.length
        u, v = u / lu, v / lv
        cosang = max(-1.0, min(1.0, u.dot(v)))
        if r <= 0 or cosang < -0.999:
            out.append(p.copy())
            tang.append((p.copy(), p.copy()))
            continue
        half = math.acos(cosang) / 2
        d = min(r / math.tan(half), 0.49 * lu, 0.49 * lv)
        rr = d * math.tan(half)
        t1, t2 = p + u * d, p + v * d
        cen = p + (u + v).normalized() * (rr / math.sin(half))
        a1 = math.atan2((t1 - cen).y, (t1 - cen).x)
        a2 = math.atan2((t2 - cen).y, (t2 - cen).x)
        da = (a2 - a1 + math.pi) % math.tau - math.pi
        for k in range(n + 1):
            t = a1 + da * k / n
            out.append(cen + Vector((math.cos(t), math.sin(t))) * rr)
        tang.append((t1, t2))
    return out, tang


def arch_pts(cx, cz, ra, zb, n=10):
    """The points of a wheel arch cut up into a body whose bottom edge is at zb, traversed from
    the rear intersection over the top to the front one (a CCW profile's bottom edge)."""
    s = max(-0.95, min(0.95, (zb - cz) / ra))
    a0, a1 = math.pi - math.asin(s), math.asin(s)
    return [(cx + ra * math.cos(a0 + (a1 - a0) * k / n), cz + ra * math.sin(a0 + (a1 - a0) * k / n), 0)
            for k in range(n + 1)]


def _hw(hw):
    return hw if callable(hw) else (lambda z, h=hw: h)


def taper(z0, hw0, z1, hw1):
    """A half width that runs linearly from hw0 at z0 to hw1 at z1 (so every face stays flat)."""
    return lambda z: hw0 + (hw1 - hw0) * (z - z0) / (z1 - z0)


def slab(b, prof, hw, mat, y=0.0, edge_mat=None):
    """Extrude a closed (x, z) side profile across the vehicle, centred on `y`, as ONE closed
    piece. `hw` is a half width or a function of z (a taper). `edge_mat(mid, normal)` may pick
    another material for a perimeter face."""
    pr = []
    for p in prof:
        p = Vector((p[0], p[1]))
        if not pr or (p - pr[-1]).length > 1e-5:
            pr.append(p)
    if (pr[0] - pr[-1]).length < 1e-5:
        pr.pop()
    area = sum(a.x * c.y - c.x * a.y for a, c in zip(pr, pr[1:] + pr[:1]))
    if area < 0:
        pr.reverse()
    f = _hw(hw)
    R = [b.bm.verts.new((p.x, y + f(p.y), p.y)) for p in pr]
    L = [b.bm.verts.new((p.x, y - f(p.y), p.y)) for p in pr]
    b.bm.faces.new(R).material_index = b.mi(mat)
    b.bm.faces.new(list(reversed(L))).material_index = b.mi(mat)
    n = len(pr)
    for i in range(n):
        j = (i + 1) % n
        m = mat
        if edge_mat:
            d = pr[j] - pr[i]
            m = edge_mat((pr[i] + pr[j]) / 2, Vector((d.y, -d.x)).normalized()) or mat
        b.bm.faces.new((L[i], L[j], R[j], R[i])).material_index = b.mi(m)


def hexa(b, c, mat):
    """A closed box from 8 corners: c[i][j][k], i along, j side, k depth."""
    vs = [[[b.bm.verts.new(c[i][j][k]) for k in range(2)] for j in range(2)] for i in range(2)]
    quads = []
    for i in range(2):
        quads.append([vs[i][0][0], vs[i][1][0], vs[i][1][1], vs[i][0][1]])
    for j in range(2):
        quads.append([vs[0][j][0], vs[1][j][0], vs[1][j][1], vs[0][j][1]])
    for k in range(2):
        quads.append([vs[0][0][k], vs[1][0][k], vs[1][1][k], vs[0][1][k]])
    idx = b.mi(mat)
    for q in quads:
        b.bm.faces.new(q).material_index = idx


def edge_panel(b, s, e, hw, mat, y=0.0, end=0.07, side=0.08, out=0.012, depth=0.04):
    """A glass (or trim) panel lying ON one straight perimeter face of a slab: the stretch s -> e
    of a CCW profile, inset `end` along it and `side` from each side, standing `out` proud."""
    f = _hw(hw)
    d = (e - s).normalized()
    nrm = Vector((d.y, -d.x))
    s2, e2 = s + d * end, e - d * end
    c = [[[None, None], [None, None]], [[None, None], [None, None]]]
    for i, p in enumerate((s2, e2)):
        w = f(p.y) - side
        for j, sy in enumerate((-1, 1)):
            for k, o in enumerate((-depth, out)):
                q = p + nrm * o
                c[i][j][k] = (q.x, y + sy * w, q.y)
    hexa(b, c, mat)


def band(b, cx, cz, r0, r1, a0, a1, y0, y1, mat, n=12):
    """A curved strip round (cx, cz) from angle a0 to a1, radii r0..r1, spanning y0..y1: wheel
    arch flares and mudguards."""
    rings = []
    for k in range(n + 1):
        a = a0 + (a1 - a0) * k / n
        ca, sa = math.cos(a), math.sin(a)
        rings.append([b.bm.verts.new((cx + r * ca, y, cz + r * sa)) for r, y in ((r0, y0), (r1, y0), (r1, y1), (r0, y1))])
    idx = b.mi(mat)
    for r0_, r1_ in zip(rings, rings[1:]):
        for q in range(4):
            b.bm.faces.new((r0_[q], r0_[(q + 1) % 4], r1_[(q + 1) % 4], r1_[q])).material_index = idx
    b.bm.faces.new(list(reversed(rings[0]))).material_index = idx
    b.bm.faces.new(rings[-1]).material_index = idx


def bx(b, c, s, mat, rot=None):
    m = Matrix.Translation(c)
    if rot is not None:
        m = m @ rot
    b.box(m, s, mat)


def disc(b, c, r, depth, mat, axis="X", sides=16, keep=False):
    """A cylinder of radius r along an axis, centred on c. Round parts go to the vehicle's
    "round" object (one light chamfer) unless `keep`: bevelling every cap edge of every lamp,
    hub and bar two segments deep cost the jeepney 7k faces for nothing visible."""
    if not keep:
        b = _RND[0] or b
    rot = {"X": Matrix.Rotation(math.pi / 2, 4, "Y"), "Y": Matrix.Rotation(math.pi / 2, 4, "X"),
           "Z": Matrix.Identity(4)}[axis]
    res = bmesh.ops.create_cone(b.bm, cap_ends=True, segments=sides, radius1=r, radius2=r, depth=depth,
                                matrix=Matrix.Translation(c) @ rot)
    b._paint(res["verts"], mat)


def rod(b, p, q, r, mat, sides=8):
    """A straight round bar from p to q."""
    import author_kanto_city as C
    C.limb(_RND[0] or b, p, q, r, r, mat, sides)


def wheel(tyres, det, x, y, r, tw, hub="hubcap", out=1, sides=28):
    """A fat tyre along Y (its own Buf, heavily bevelled into a round toy tyre), sunk 1 cm into the
    road, with a hubcap and a nut standing proud of the outer face."""
    zc = r - 0.01
    disc(tyres, (x, y, zc), r, tw, "tyre", "Y", sides, keep=True)
    disc(det, (x, y + out * (tw / 2), zc), r * 0.56, 0.06, hub, "Y", 24)
    disc(det, (x, y + out * (tw / 2 + 0.035), zc), r * 0.2, 0.05, "chassis", "Y", 12)


def text(b, s, size, at, facing, mat):
    """Letters from a Blender FONT curve, converted to mesh and baked into `b`, standing on a face
    whose outward normal is `facing` (+X, -X, +Y or -Y), centred on `at`."""
    # Letters are FLAT painted decals (no extrude) in the vehicle's own unbevelled "letters"
    # object, standing a few mm off the face they are painted on: extruded, bevelled glyphs
    # multiplied the jeepney past 27k faces. A low curve resolution keeps each glyph cheap.
    b = _LET[0] or b
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body, cu.size = s, size
    cu.resolution_u = 2
    cu.align_x, cu.align_y = "CENTER", "CENTER"
    ob = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    # Columns: where the text's own x (reading direction), y (up) and z (out of the face) go.
    right = {"+X": Y, "-X": -Y, "+Y": -X, "-Y": X}[facing]
    normal = {"+X": X, "-X": -X, "+Y": Y, "-Y": -Y}[facing]
    rot = Matrix((right, Z, normal)).transposed().to_4x4()
    m = Matrix.Translation(at) @ rot
    for p in me.polygons:
        b.face([m @ me.vertices[i].co for i in p.vertices], mat)
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    bpy.data.meshes.remove(me)


def horse(b, at, s, mat):
    """The jeepney's chrome hood horse, cute and chunky: a plinth, a round body, a raised neck and
    head, four stubby legs and a tail, facing +X."""
    x, y, z = at
    bx(b, (x, y, z + 0.015 * s), (0.26 * s, 0.10 * s, 0.05 * s), mat)
    b.blob(Vector((x, y, z + 0.19 * s)), (0.12 * s, 0.055 * s, 0.065 * s), mat, subdiv=2)
    b.blob(Vector((x + 0.10 * s, y, z + 0.27 * s)), (0.045 * s, 0.035 * s, 0.075 * s), mat, subdiv=1)
    b.blob(Vector((x + 0.15 * s, y, z + 0.33 * s)), (0.07 * s, 0.035 * s, 0.035 * s), mat, subdiv=1)
    b.blob(Vector((x - 0.13 * s, y, z + 0.17 * s)), (0.05 * s, 0.02 * s, 0.07 * s), mat, subdiv=1)
    for lx in (-0.07, 0.07):
        for ly in (-0.028, 0.028):
            rod(b, (x + lx * s, y + ly * s, z + 0.17 * s), (x + (lx + 0.02) * s, y + ly * s, z + 0.02 * s), 0.018 * s, mat, 6)


# ------------------------------------------------------------------ the car family

# Side-profile numbers for the four car shapes. x runs rear (-) to front (+).
#   L, W       length and width;  r, tw   wheel radius and tyre width;  axles  (front x, rear x)
#   zb         the body's bottom edge;  belt   the waist the cabin sits on
#   hood       the top of the nose;  cowl   where the hood meets the waist;  tail  top of the tail
#   cab        (rear base x, front base x, front top x, rear top x, roof z)
#   windows    side window runs (x from, x to); None = follow the cabin's slope
SHAPES = {
    "sedan": dict(L=3.9, W=1.86, r=0.39, tw=0.30, axles=(1.22, -1.24), zb=0.30, belt=0.90, hood=0.76,
                  cowl=0.95, tail=0.84, cab=(-1.55, 0.90, 0.22, -1.15, 1.52),
                  windows=[(None, -0.42), (-0.28, None)], bevel=0.09),
    "hatch": dict(L=3.4, W=1.80, r=0.39, tw=0.30, axles=(1.06, -1.08), zb=0.30, belt=0.90, hood=0.76,
                  cowl=0.85, tail=0.88, cab=(-1.66, 0.80, 0.12, -1.56, 1.56),
                  windows=[(None, -0.62), (-0.48, None)], bevel=0.09),
    "pickup": dict(L=4.4, W=1.92, r=0.43, tw=0.32, axles=(1.36, -1.34), zb=0.34, belt=0.98, hood=0.86,
                   cowl=1.10, tail=0.98, cab=(-0.45, 1.05, 0.52, -0.36, 1.72),
                   windows=[(None, None)], bevel=0.09),
    "van": dict(L=4.5, W=1.96, r=0.41, tw=0.31, axles=(1.46, -1.40), zb=0.32, belt=0.92, hood=0.82,
                cowl=1.50, tail=0.92, cab=(-2.20, 1.62, 0.84, -2.18, 2.05),
                windows=[(0.30, None)], bevel=0.10),
}


def car(name, spec):
    col_bufs = []
    sh = dict(SHAPES[spec["kind"] if spec["kind"] != "taxi" else "sedan"])
    L, W, r, tw = sh["L"], sh["W"], sh["r"], sh["tw"]
    xf, xr = sh["axles"]
    paint = spec["paint"]
    body, det, glass, tyres = (K.Buf(f"{name}_body"), K.Buf(f"{name}_trim"), K.Buf(f"{name}_glass"),
                               K.Buf(f"{name}_wheels"))
    zw, ra, zb, belt = r - 0.01, r + 0.07, sh["zb"], sh["belt"]

    # The lower body: one side profile with both wheel arches cut up into it.
    prof = [(-L / 2, zb, 0.18)] + arch_pts(xr, zw, ra, zb) + arch_pts(xf, zw, ra, zb)
    prof += [(L / 2, zb, 0.18), (L / 2, sh["hood"], 0.28), (sh["cowl"], belt, 0.35),
             (-L / 2 + 0.35, belt, 0.3), (-L / 2, sh["tail"], 0.22)]
    pts, _ = fillet(prof)
    slab(body, [(p.x, p.y) for p in pts], W / 2, paint)

    # The cabin: sits 6 cm down inside the waist and tapers in toward the roof.
    rb, fb, ft, rt, top = sh["cab"]
    zc = belt - 0.06
    tuck = 0.13 if spec["kind"] != "van" else 0.0   # the van is a box: plumb sides take lettering
    hwc = taper(zc, W / 2 - 0.05, top, W / 2 - 0.05 - tuck)
    base = [(rb, zc, 0.0), (fb, zc, 0.0), (ft, top, 0.22), (rt, top, 0.28 if spec["kind"] != "van" else 0.22)]
    cpts, tang = fillet(base)
    slab(body, [(p.x, p.y) for p in cpts], hwc, paint)
    # Windscreen and rear screen: dark panels ON the cabin's slopes, a painted frame all round.
    edge_panel(glass, tang[1][1], tang[2][0], hwc, "glass_dark", end=0.08, side=0.1)
    edge_panel(glass, tang[3][1], tang[0][0], hwc, "glass_dark", end=0.12, side=0.12)

    # Side windows: a thin dark slab that follows the cabin taper 12 mm proud of it.
    z0, z1 = belt + 0.1, top - 0.1

    def front_x(z):
        return fb + (ft - fb) * (z - zc) / (top - zc) - 0.1

    def rear_x(z):
        return rb + (rt - rb) * (z - zc) / (top - zc) + 0.1

    for xa, xb in sh["windows"]:
        poly = [((rear_x(z0) if xa is None else xa), z0, 0.05), ((front_x(z0) if xb is None else xb), z0, 0.05),
                ((front_x(z1) if xb is None else xb), z1, 0.08), ((rear_x(z1) if xa is None else xa), z1, 0.08)]
        wp, _ = fillet(poly, 3)
        slab(glass, [(p.x, p.y) for p in wp], lambda z: hwc(z) + 0.012, "glass_dark")

    # Wheels, bulging arch flares, and a dark underbody so nothing shows through below.
    for x in (xf, xr):
        for sy in (-1, 1):
            wheel(tyres, det, x, sy * (W / 2 - tw / 2 - 0.02), r, tw, out=sy)
            y0, y1 = (W / 2 - 0.12, W / 2 + 0.04) if sy > 0 else (-W / 2 - 0.04, -W / 2 + 0.12)
            s = (zb - zw) / (ra + 0.02)
            band(det, x, zw, ra - 0.02, ra + 0.07, math.pi - math.asin(s) - 0.05, math.asin(s) + 0.05, y0, y1, paint, n=18)
    bx(det, ((xf + xr) / 2, 0, zb + 0.02), (xf - xr + 0.4, W - 2 * tw - 0.1, 0.2), "chassis")

    # Face: big round headlights, a grille, a chunky bumper and plate. Tail: lamps and bumper.
    zl = zb + (sh["hood"] - zb) * 0.62
    for sy in (-1, 1):
        disc(det, (L / 2 - 0.03, sy * (W / 2 - 0.34), zl), 0.13, 0.12, "lamp_glass", "X", 16)
        disc(det, (L / 2 - 0.05, sy * (W / 2 - 0.34), zl), 0.155, 0.1, "chassis", "X", 16)
        bx(det, (-L / 2 + 0.02, sy * (W / 2 - 0.3), sh["tail"] - 0.16), (0.1, 0.34, 0.15), "signal_red")
        # Wing mirrors on a stub, at the foot of the windscreen.
        mx = front_x(zc + 0.18) - 0.02
        hw_m = hwc(zc + 0.18)
        bx(det, (mx, sy * (hw_m + 0.06), zc + 0.2), (0.08, 0.16, 0.05), paint)
        bx(det, (mx - 0.02, sy * (hw_m + 0.16), zc + 0.26), (0.12, 0.1, 0.13), paint)
    bx(det, (L / 2 - 0.02, 0, zb + 0.16), (0.1, W * 0.36, 0.16), "chassis")
    for sx in (-1, 1):
        bx(det, (sx * (L / 2 + 0.02), 0, zb + 0.03), (0.2, W + 0.05, 0.19), "steel")
        bx(det, (sx * (L / 2 + 0.115), 0, zb + 0.04), (0.03, 0.44, 0.12), "plate")

    kind = spec["kind"]
    if kind == "taxi":
        # The roof sign: a white box with TAXI on both sides, and a checker-dark stripe on the doors.
        rx = (ft + rt) / 2
        bx(det, (rx, 0, top + 0.09), (0.7, 0.3, 0.22), "white")
        bx(det, (rx, 0, top + 0.015), (0.5, 0.2, 0.06), "chassis")
        for sy in (-1, 1):
            text(det, "TAXI", 0.15, (rx, sy * 0.152, top + 0.09), "+Y" if sy > 0 else "-Y", "chassis")
    if kind == "pickup":
        # An open bed: walls standing 1 cm proud of the body sides, a dark liner, two crates.
        bx0, bx1 = -L / 2, rb - 0.06
        mid, ln = (bx0 + bx1) / 2, bx1 - bx0
        for sy in (-1, 1):
            bx(det, (mid, sy * (W / 2 - 0.05), belt + 0.13), (ln, 0.12, 0.46), paint)
        bx(det, (bx1 - 0.05, 0, belt + 0.13), (0.1, W - 0.1, 0.46), paint)
        bx(det, (bx0 + 0.05, 0, belt + 0.12), (0.12, W + 0.0, 0.44), paint)
        bx(det, (mid, 0, belt + 0.01), (ln - 0.1, W - 0.16, 0.06), "chassis")
        bx(det, (mid - 0.2, 0.3, belt + 0.26), (0.55, 0.5, 0.5), "wood")
        bx(det, (mid + 0.35, -0.35, belt + 0.21), (0.45, 0.45, 0.4), "wood")
    if kind == "van":
        # A green band and PADALA ("send") on the cargo sides; a rear door split.
        band_prof = [(rb + 0.02, 1.16), (fb - 0.95, 1.16), (fb - 0.95, 1.3), (rb + 0.02, 1.3)]
        slab(det, band_prof, lambda z: hwc(z) + 0.01, spec.get("band", "sign_green"))
        for sy in (-1, 1):
            text(det, "PADALA", 0.36, (-0.75, sy * (hwc(1.6) + 0.005), 1.6), "+Y" if sy > 0 else "-Y",
                 spec.get("band", "sign_green"))
        bx(det, (rb - 0.02, 0, (zc + top) / 2), (0.06, 0.05, top - zc - 0.3), "chassis")
    b_bevel = sh["bevel"]
    body.finish(K_col(), bevel=b_bevel, segments=3)
    det.finish(K_col(), bevel=0.025, segments=2)
    glass.finish(K_col(), bevel=0.015, segments=1)
    tyres.finish(K_col(), bevel=min(0.08, tw * 0.3), segments=3)
    return col_bufs


# ------------------------------------------------------------------ the bus

def bus(name, spec):
    L, W, r, tw = 9.6, 2.5, 0.52, 0.36
    xf, xr = 3.0, -2.7
    zw, ra, zb = r - 0.01, r + 0.08, 0.42
    body, det, glass, tyres = (K.Buf(f"{name}_body"), K.Buf(f"{name}_trim"), K.Buf(f"{name}_glass"),
                               K.Buf(f"{name}_wheels"))
    lower, upper, stripe = spec["lower"], spec["paint"], spec["stripe"]
    # The skirt: green, 2 cm wider and longer than the cream upper body, arches cut into it.
    prof = [(-L / 2 - 0.02, zb, 0.2)] + arch_pts(xr, zw, ra, zb) + arch_pts(xf, zw, ra, zb)
    prof += [(L / 2 + 0.02, zb, 0.2), (L / 2 + 0.02, 1.12, 0.0), (-L / 2 - 0.02, 1.12, 0.0)]
    pts, _ = fillet(prof)
    slab(body, [(p.x, p.y) for p in pts], W / 2 + 0.02, lower)
    top = 3.0
    up = [(-L / 2, 1.04, 0.0), (L / 2, 1.04, 0.0), (L / 2, top, 0.42), (-L / 2, top, 0.38)]
    upts, tang = fillet(up)
    slab(body, [(p.x, p.y) for p in upts], W / 2, upper)
    # A red stripe wrapping all round under the windows.
    slab(det, [(-L / 2 - 0.012, 1.3), (L / 2 + 0.012, 1.3), (L / 2 + 0.012, 1.42), (-L / 2 - 0.012, 1.42)],
         W / 2 + 0.012, stripe)
    # Windows: a row down each side, the windscreen, a rear window, the door on the kerb side (-Y).
    xs = [-4.45 + k * 1.3 for k in range(7)]
    for k, x0 in enumerate(xs):
        x1 = x0 + 1.14
        if x1 > 3.45:
            x1 = 3.45
        wp, _ = fillet([(x0, 1.6, 0.08), (x1, 1.6, 0.08), (x1, 2.58, 0.1), (x0, 2.58, 0.1)], 3)
        slab(glass, [(p.x, p.y) for p in wp], W / 2 + 0.012, "glass_dark")
    edge_panel(glass, Vector((L / 2, 1.5)), Vector((L / 2, 2.52)), W / 2, "glass_dark", end=0.0, side=0.14)
    edge_panel(glass, Vector((-L / 2, 2.55)), Vector((-L / 2, 1.9)), W / 2, "glass_dark", end=0.0, side=0.3)
    bx(glass, (L / 2 - 0.02, 0, 2.0), (0.08, 0.06, 1.02), "chassis")   # the windscreen's centre bar
    bx(glass, (3.98, -(W / 2 + 0.015), 1.55), (0.84, 0.07, 2.05), "glass_dark")
    bx(det, (3.98, -(W / 2 + 0.03), 1.55), (0.06, 0.05, 2.0), "chassis")
    # The destination sign over the windscreen, KANTO in yellow.
    bx(det, (L / 2 + 0.0, 0, 2.7), (0.1, 1.8, 0.28), "chassis")
    text(det, "KANTO", 0.22, (L / 2 + 0.054, 0, 2.7), "+X", "lane_yellow")
    for sy in (-1, 1):
        wheel(tyres, det, xf, sy * (W / 2 - tw / 2 - 0.03), r, tw, out=sy)
        wheel(tyres, det, xr, sy * (W / 2 - tw / 2 - 0.03), r, tw, out=sy)
        for x in (xf, xr):
            s = (zb - zw) / (ra + 0.02)
            y0, y1 = (W / 2 - 0.1, W / 2 + 0.07) if sy > 0 else (-W / 2 - 0.07, -W / 2 + 0.1)
            band(det, x, zw, ra - 0.02, ra + 0.08, math.pi - math.asin(s) - 0.05, math.asin(s) + 0.05, y0, y1, lower)
        # Headlights, tail lights, the bug-eye mirrors on drooping arms.
        disc(det, (L / 2 + 0.0, sy * 0.85, 0.78), 0.15, 0.1, "lamp_glass", "X", 16)
        disc(det, (L / 2 - 0.02, sy * 0.85, 0.78), 0.18, 0.1, "chassis", "X", 16)
        bx(det, (-L / 2 - 0.01, sy * 0.95, 0.85), (0.08, 0.3, 0.3), "signal_red")
        rod(det, (L / 2 - 0.1, sy * (W / 2 - 0.05), 2.75), (L / 2 + 0.35, sy * (W / 2 + 0.12), 2.65), 0.03, "chassis")
        bx(det, (L / 2 + 0.38, sy * (W / 2 + 0.12), 2.35), (0.08, 0.2, 0.55), "chassis")
    for sx in (-1, 1):
        bx(det, (sx * (L / 2 + 0.05), 0, zb + 0.04), (0.2, W + 0.06, 0.24), "steel")
    bx(det, (L / 2 + 0.16, 0, zb + 0.05), (0.03, 0.5, 0.14), "plate")
    bx(det, ((xf + xr) / 2, 0, zb + 0.02), (xf - xr + 0.5, W - 2 * tw - 0.12, 0.26), "chassis")
    # Roof: an air-con pod and a hatch.
    bx(det, (-0.8, 0, top + 0.12), (2.3, 1.5, 0.3), "white")
    bx(det, (-0.8, 0, top + 0.28), (1.6, 1.0, 0.06), "steel")
    bx(det, (2.3, 0, top + 0.04), (0.8, 0.8, 0.12), "steel")
    body.finish(K_col(), bevel=0.12, segments=3)
    det.finish(K_col(), bevel=0.03, segments=2)
    glass.finish(K_col(), bevel=0.015, segments=1)
    tyres.finish(K_col(), bevel=0.1, segments=3)


# ------------------------------------------------------------------ the jeepney

def jeepney(name, spec):
    """The King of the Road. Long hood with chrome horses and a chrome grille; an upright split
    windscreen; an open-sided passenger body with posts, two long benches and a rear doorway with
    a step and grab bars; a long roof with rails and the route board on its brow."""
    W, r, tw = 1.96, 0.42, 0.30
    xf, xr = 2.05, -1.62
    zw, ra = r - 0.01, r + 0.08
    paint, chrome = spec["paint"], "jeep_chrome"
    body, det, glass, tyres = (K.Buf(f"{name}_body"), K.Buf(f"{name}_trim"), K.Buf(f"{name}_glass"),
                               K.Buf(f"{name}_wheels"))
    x_back, x_cab = -3.05, 1.35
    # Side walls: 15 cm thick, the rear arch cut in, dropping to door height at the cab.
    wall = [(x_back, 0.5, 0.12)] + arch_pts(xr, zw, ra, 0.5) + [(x_cab, 0.5, 0.05), (x_cab, 1.02, 0.08),
                                                                 (0.42, 1.02, 0.1), (0.3, 1.27, 0.06), (x_back, 1.27, 0.1)]
    wpts, _ = fillet(wall)
    for sy in (-1, 1):
        slab(body, [(p.x, p.y) for p in wpts], 0.075, paint, y=sy * 0.9)
    bx(body, ((x_back + x_cab) / 2 - 0.02, 0, 0.6), (x_cab - x_back - 0.1, 1.72, 0.12), "chassis")   # floor
    bx(det, (0.0, 0, 0.47), (5.7, 0.9, 0.24), "chassis")                                              # frame
    # The rear: panels either side of a doorway, a header under the roof, lamps, a step, grab bars.
    for sy in (-1, 1):
        bx(body, (x_back + 0.03, sy * 0.72, 0.9), (0.1, 0.5, 0.82), paint)
        bx(det, (x_back - 0.03, sy * 0.76, 1.12), (0.06, 0.2, 0.14), "signal_red")
        rod(det, (x_back - 0.05, sy * 0.44, 0.62), (x_back - 0.05, sy * 0.44, 1.85), 0.025, chrome)
    bx(body, (x_back + 0.03, 0, 1.83), (0.1, 1.9, 0.2), spec["roof"])
    bx(det, (x_back - 0.08, 0, 0.44), (0.24, 0.9, 0.06), chrome)
    for sy in (-1, 1):   # the step hangs off the chassis on two brackets
        bx(det, (x_back + 0.05, sy * 0.36, 0.5), (0.34, 0.06, 0.16), "chassis")
    # Livery: stripes standing proud of the walls, a chrome waist rail, red kick strip.
    for sy in (-1, 1):
        y = sy * 0.975
        bx(det, ((x_back + 0.3) / 2, y, 1.06), (0.3 - x_back - 0.12, 0.03, 0.08), spec["stripe1"])
        bx(det, ((x_back + 0.3) / 2, y, 1.15), (0.3 - x_back - 0.12, 0.03, 0.05), spec["stripe2"])
        bx(det, ((x_back + 0.3) / 2, sy * 0.9, 1.275), (0.3 - x_back, 0.19, 0.05), chrome)
        bx(det, ((xr + ra + 0.1 + x_cab) / 2, y, 0.62), (x_cab - (xr + ra + 0.1) - 0.05, 0.03, 0.07), spec["stripe2"])
        text(det, spec["route"], 0.13, (-0.42, sy * 0.98, 0.8), "+Y" if sy > 0 else "-Y", spec["stripe1"])
    # Posts: the open window band, chrome, running up into the roof.
    for x in (0.3, -0.25, -0.8, -1.35, -1.9, -2.45, -2.98):
        for sy in (-1, 1):
            bx(det, (x, sy * 0.9, 1.6), (0.07, 0.09, 0.72), chrome)
    # Inside: two long benches facing each other, backrests on the walls; the driver's seat.
    for sy in (-1, 1):
        bx(det, (-1.3, sy * 0.6, 0.8), (3.1, 0.38, 0.36), "vinyl_dark")
        bx(det, (-1.3, sy * 0.79, 1.1), (3.1, 0.1, 0.3), "vinyl_dark")
    bx(det, (0.7, 0.45, 0.82), (0.45, 0.55, 0.4), "vinyl_dark")
    bx(det, (0.55, 0.45, 1.15), (0.1, 0.55, 0.45), "vinyl_dark")
    disc(det, (1.08, 0.45, 1.22), 0.17, 0.04, "chassis", "X", 16)
    # Roof: long, overhanging, with rails, and the route board on its brow.
    roof = [(-3.2, 1.9, 0.04), (1.62, 1.9, 0.04), (1.62, 2.07, 0.08), (-3.2, 2.07, 0.06)]
    rpts, _ = fillet(roof)
    slab(body, [(p.x, p.y) for p in rpts], 1.02, spec["roof"])
    for sy in (-1, 1):
        bx(det, (-0.8, sy * 1.03, 1.985), (4.84, 0.03, 0.06), spec["stripe1"])
        rod(det, (-2.9, sy * 0.9, 2.16), (1.2, sy * 0.9, 2.16), 0.025, chrome)
        for x in (-2.9, -1.5, 0.0, 1.2):
            rod(det, (x, sy * 0.9, 2.04), (x, sy * 0.9, 2.18), 0.02, chrome)
    bx(det, (1.38, 0, 2.23), (0.1, 1.6, 0.36), spec["board"])
    text(det, spec["board_text"], 0.24, (1.434, 0, 2.23), "+X", spec["board_ink"])
    text(det, spec["board_text"], 0.24, (1.326, 0, 2.23), "-X", spec["board_ink"])
    # The windscreen: two upright panes in a chrome frame, a sun visor under the roof's brow.
    for sy in (-1, 1):
        bx(glass, (x_cab, sy * 0.46, 1.54), (0.03, 0.8, 0.62), "glass_dark")
        bx(det, (x_cab, sy * 0.9, 1.5), (0.08, 0.1, 0.95), chrome)
        # Mirrors on long chrome arms.
        rod(det, (x_cab + 0.02, sy * 0.92, 1.6), (x_cab + 0.2, sy * 1.28, 1.52), 0.02, chrome)
        disc(det, (x_cab + 0.22, sy * 1.3, 1.5), 0.11, 0.05, chrome, "X", 14)
        disc(det, (x_cab + 0.19, sy * 1.3, 1.5), 0.085, 0.05, "glass_dark", "X", 14)
    bx(det, (x_cab, 0, 1.22), (0.08, 1.9, 0.07), chrome)
    bx(det, (x_cab, 0, 1.86), (0.08, 1.9, 0.08), chrome)
    bx(det, (x_cab, 0, 1.54), (0.07, 0.07, 0.64), chrome)
    # The hood: narrow and long, horses on top, a chrome grille, round lamps, a big bumper.
    hood = [(1.28, 0.62, 0.0), (2.98, 0.62, 0.06), (2.98, 1.12, 0.14), (2.7, 1.22, 0.3), (1.28, 1.24, 0.05)]
    hpts, _ = fillet(hood)
    slab(body, [(p.x, p.y) for p in hpts], 0.6, paint)
    bx(det, (2.02, 0, 1.225), (1.3, 0.12, 0.04), chrome)   # the hood's centre strip
    for sy in (-1, 1):   # chrome louvres down each side of the hood
        for k in range(4):
            bx(det, (1.75 + k * 0.2, sy * 0.605, 0.98), (0.1, 0.03, 0.2), chrome)
    for sy in (-1, 1):
        horse(_RND[0], (2.72, sy * 0.3, 1.18), 0.95, chrome)
    bx(det, (3.0, 0, 0.93), (0.08, 1.16, 0.56), "chassis")
    for k in range(5):
        bx(det, (3.05, -0.26 + k * 0.13, 0.93), (0.05, 0.05, 0.5), chrome)
    for zz in (0.66, 1.19):
        bx(det, (3.04, 0, zz), (0.08, 1.3, 0.06), chrome)
    for sy in (-1, 1):
        bx(det, (3.04, sy * 0.62, 0.93), (0.08, 0.07, 0.6), chrome)
        disc(det, (3.07, sy * 0.46, 0.96), 0.1, 0.06, "lamp_glass", "X", 16)
        disc(det, (3.05, sy * 0.46, 0.96), 0.125, 0.06, chrome, "X", 16)
        # Front fenders: a broad red mudguard arching over each wheel; a chrome running board.
        y0, y1 = (0.58, 1.0) if sy > 0 else (-1.0, -0.58)
        band(det, xf, zw, ra - 0.01, ra + 0.07, math.pi + 0.05, -0.12, y0, y1, paint, n=14)
        bx(det, ((x_cab + 1.55) / 2 - 0.2, sy * 0.8, 0.54), (1.35, 0.4, 0.06), chrome)
        s = (0.5 - zw) / (ra + 0.02)
        y0, y1 = (0.8, 1.02) if sy > 0 else (-1.02, -0.8)
        band(det, xr, zw, ra - 0.02, ra + 0.06, math.pi - math.asin(s) - 0.05, math.asin(s) + 0.05, y0, y1, chrome)
    bx(det, (3.14, 0, 0.56), (0.16, 2.0, 0.16), chrome)
    for sy in (-1, 1):
        det.blob(Vector((3.16, sy * 1.0, 0.56)), (0.1, 0.09, 0.09), chrome, subdiv=1)
        rod(det, (3.14, sy * 0.3, 0.56), (3.1, sy * 0.3, 1.02), 0.03, chrome)
    bx(det, (3.23, 0, 0.56), (0.03, 0.44, 0.12), "plate")
    for sy in (-1, 1):
        for x in (xf, xr):
            wheel(tyres, det, x, sy * (W / 2 - tw / 2 - 0.03), r, tw, hub=chrome, out=sy)
    body.finish(K_col(), bevel=0.05, segments=2)
    det.finish(K_col(), bevel=0.018, segments=1)
    glass.finish(K_col(), bevel=0.01, segments=1)
    tyres.finish(K_col(), bevel=0.08, segments=2)


# ------------------------------------------------------------------ the tricycle

def tricycle(name, spec):
    """A motorcycle with a covered sidecar bolted to its right (-Y) side, one roof over both, and
    the local TODA (tricycle drivers' association) name painted on the sidecar."""
    body, det, glass, tyres = (K.Buf(f"{name}_body"), K.Buf(f"{name}_trim"), K.Buf(f"{name}_glass"),
                               K.Buf(f"{name}_wheels"))
    oy = 0.2                 # shifts the whole rig so the origin sits under its middle
    my, sy_ = 0.36 + oy, -0.5 + oy     # the motorcycle's line, the sidecar's centre line
    r, tw = 0.3, 0.13
    xf, xr = 0.9, -0.52
    tank, side, roof, trim = spec["tank"], spec["paint"], spec["roof"], spec["trim"]
    # The motorcycle.
    wheel(tyres, det, xf, my, r, tw, out=1, sides=22)
    wheel(tyres, det, xr, my, r, tw, out=1, sides=22)
    bx(det, (0.1, my, 0.45), (0.42, 0.26, 0.3), "chassis")                        # engine
    body.blob(Vector((0.3, my, 0.88)), (0.26, 0.16, 0.12), tank, subdiv=2)        # tank
    body.blob(Vector((-0.2, my, 0.84)), (0.32, 0.15, 0.07), "vinyl_dark", subdiv=2)   # seat
    bx(body, (-0.3, my, 0.66), (0.55, 0.22, 0.24), tank)                            # side covers
    rod(det, (0.66, my, 1.02), (0.05, my, 0.72), 0.045, "chassis")                 # frame
    rod(det, (0.05, my, 0.72), (xr, my, r), 0.035, "chassis")
    for dy in (-0.08, 0.08):
        rod(det, (xf, my + dy, r), (0.66, my + dy, 1.06), 0.03, "steel")         # fork
    rod(det, (0.63, my - 0.34, 1.14), (0.63, my + 0.34, 1.14), 0.025, "chassis")  # handlebar
    for sy in (-1, 1):
        rod(det, (0.63, my + sy * 0.26, 1.14), (0.63, my + sy * 0.36, 1.14), 0.035, "vinyl_dark")
    disc(det, (0.76, my, 1.0), 0.09, 0.08, "lamp_glass", "X", 14)
    disc(det, (0.72, my, 1.0), 0.11, 0.1, tank, "X", 14)
    rod(det, (0.15, my + 0.16, 0.38), (-0.75, my + 0.16, 0.5), 0.04, "steel")     # exhaust
    for x, a0, a1 in ((xf, math.radians(160), math.radians(25)), (xr, math.radians(155), math.radians(15))):
        band(det, x, r - 0.01, r + 0.03, r + 0.07, a0, a1, my - 0.09, my + 0.09, tank)
    bx(det, (-0.8, my, 0.62), (0.05, 0.14, 0.08), "signal_red")
    # The sidecar: a rounded tub with its own wheel outboard, a dark window band, posts, a
    # windscreen, and one roof carried over the rider on a bar.
    tub = [(-0.8, 0.26, 0.1), (0.72, 0.26, 0.22), (1.0, 0.62, 0.18), (0.86, 0.86, 0.06), (-0.8, 0.86, 0.08)]
    tpts, _ = fillet(tub)
    slab(body, [(p.x, p.y) for p in tpts], 0.42, side, y=sy_)
    wheel(tyres, det, 0.0, sy_ - 0.49, 0.27, tw, out=-1, sides=22)
    band(det, 0.0, 0.26, 0.3, 0.35, math.radians(170), math.radians(10), sy_ - 0.57, sy_ - 0.4, side)
    bx(det, (0.05, (sy_ + 0.42 + my - 0.1) / 2, 0.42), (0.12, my - 0.1 - sy_ - 0.42 + 0.08, 0.08), "chassis")
    slab(det, [(-0.8, 0.7), (0.9, 0.7), (0.9, 0.78), (-0.8, 0.78)], 0.432, trim, y=sy_)   # waist stripe
    # The cab over the tub: painted, with a window cut on each side and at the back (a solid
    # dark band read as a black box, review v3).
    cab = [(-0.72, 0.82, 0.05), (0.62, 0.82, 0.05), (0.62, 1.52, 0.06), (-0.72, 1.52, 0.06)]
    bpts, _ = fillet(cab)
    slab(body, [(p.x, p.y) for p in bpts], 0.38, side, y=sy_)
    win = [(-0.55, 0.96, 0.05), (0.46, 0.96, 0.05), (0.46, 1.36, 0.06), (-0.55, 1.36, 0.06)]
    wpts, _ = fillet(win, 3)
    slab(glass, [(p.x, p.y) for p in wpts], 0.392, "glass_dark", y=sy_)
    bx(glass, (-0.72, sy_, 1.16), (0.04, 0.5, 0.36), "glass_dark")
    bx(glass, (0.62, sy_, 1.16), (0.04, 0.5, 0.36), "glass_dark")
    for x in (-0.76, 0.66):
        for s in (-1, 1):
            bx(det, (x, sy_ + s * 0.39, 1.2), (0.07, 0.07, 0.8), trim)
    bx(glass, (0.9, sy_, 1.2), (0.04, 0.74, 0.62), "glass_dark")
    bx(det, (0.9, sy_, 0.9), (0.08, 0.84, 0.06), trim)
    for s in (-1, 1):
        rod(det, (0.66, sy_ + s * 0.39, 1.55), (0.9, sy_ + s * 0.39, 0.86), 0.03, trim)
    rf = [(-0.95, 1.55, 0.04), (1.02, 1.55, 0.04), (1.02, 1.66, 0.05), (-0.95, 1.66, 0.05)]
    rpts, _ = fillet(rf)
    slab(body, [(p.x, p.y) for p in rpts], 0.8, roof, y=(sy_ - 0.45 + my + 0.35) / 2)
    rod(det, (-0.62, my, 1.57), (-0.62, my, 0.8), 0.028, "chassis")
    rod(det, (0.55, my + 0.1, 1.57), (0.62, my + 0.1, 1.12), 0.022, "chassis")
    bx(det, (0.2, (sy_ - 0.45 + my + 0.35) / 2 - 0.8 - 0.005, 1.605), (1.9, 0.03, 0.06), trim)
    text(det, spec["toda"], 0.11, (-0.05, sy_ - 0.385, 1.44), "-Y", trim)
    bx(det, (-0.82, sy_ - 0.22, 0.6), (0.06, 0.16, 0.1), "signal_red")
    disc(det, (1.0, sy_ - 0.25, 0.56), 0.06, 0.06, "lamp_glass", "X", 12)
    body.finish(K_col(), bevel=0.04, segments=3)
    det.finish(K_col(), bevel=0.012, segments=2)
    glass.finish(K_col(), bevel=0.01, segments=1)
    tyres.finish(K_col(), bevel=0.045, segments=3)


# ------------------------------------------------------------------ the roster

VEHICLES = {
    "sedan_red":     {"kind": "sedan", "paint": "car_red"},
    "sedan_cream":   {"kind": "sedan", "paint": "plaster_cream"},
    "hatch_mint":    {"kind": "hatch", "paint": "plaster_mint"},
    "taxi":          {"kind": "taxi", "paint": "lane_yellow"},
    "pickup_mustard": {"kind": "pickup", "paint": "mustard"},
    "van_delivery":  {"kind": "van", "paint": "white", "band": "sign_green"},
    "bus_city":      {"kind": "bus", "paint": "plaster_cream", "lower": "sign_green", "stripe": "car_red"},
    "jeepney":       {"kind": "jeepney", "paint": "car_red", "roof": "plaster_cream", "stripe1": "sign_green",
                      "stripe2": "lane_yellow", "board": "lane_yellow", "board_ink": "car_red",
                      "board_text": "CUBAO", "route": "CUBAO - QUIAPO"},
    "tricycle":      {"kind": "tricycle", "paint": "sign_green", "tank": "car_red", "roof": "plaster_cream",
                      "trim": "lane_yellow", "toda": "KANTO TODA"},
}

_COL = [None]
_LET = [None]
_RND = [None]


def K_col():
    return _COL[0]


def build_vehicle(name):
    """Build one vehicle into its own collection (author_kanto_city.kit) and return it."""
    import author_kanto_city as C    # noqa: F401  (also merges the city palette into K.PALETTE)
    spec = VEHICLES[name]
    col = C.kit(name)
    _COL[0] = col
    _LET[0] = K.Buf(f"{name}_letters")
    _RND[0] = K.Buf(f"{name}_round")
    kind = spec["kind"]
    if kind in ("sedan", "hatch", "taxi", "pickup", "van"):
        car(name, spec)
    elif kind == "bus":
        bus(name, spec)
    elif kind == "jeepney":
        jeepney(name, spec)
    elif kind == "tricycle":
        tricycle(name, spec)
    _LET[0].keep_winding = True     # flat one-sided decals: never let recalc flip them
    for buf, bev in ((_LET[0], 0), (_RND[0], 0)):
        if len(buf.bm.faces):
            buf.finish(col, bevel=bev, segments=1, angle=60)
        else:
            buf.bm.free()
    _COL[0] = _LET[0] = _RND[0] = None
    return col


def face_count(col):
    dg = bpy.context.evaluated_depsgraph_get()
    n = 0
    for o in col.all_objects:
        if o.type == "MESH":
            ev = o.evaluated_get(dg)
            me = ev.to_mesh()
            n += len(me.polygons)
            ev.to_mesh_clear()
    return n


# ------------------------------------------------------------------ preview

def _bounds(col):
    pts = [o.matrix_world @ Vector(c) for o in col.all_objects if o.type == "MESH" for c in o.bound_box]
    return (Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))),
            Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts))))


def _person(at):
    b = K.Buf("person_1m6")
    b.cylinder(Vector((at[0], at[1], 0.55)), 0.26, 1.12, "concrete")
    b.blob(Vector((at[0], at[1], 1.38)), (0.22, 0.22, 0.22), "concrete")
    b.finish(bpy.context.scene.collection, bevel=0.03)


def _preview(version):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    K.USE_TEXTURES = True
    import author_kanto_city as C    # noqa: F401
    rows = [["tricycle", "hatch_mint", "sedan_red", "taxi", "jeepney"],
            ["sedan_cream", "pickup_mustard", "van_delivery", "bus_city"]]
    where = {}
    for ri, row in enumerate(rows):
        x = 0.0
        for name in row:
            col = build_vehicle(name)
            lo, hi = _bounds(col)
            off = Vector((x - lo.x, ri * 5.0, 0))
            for o in col.all_objects:
                o.location += off
            where[name] = off
            x += hi.x - lo.x + 1.6
            lo, hi = _bounds(col)
            print(f"[kanto-vehicles] {name}: {face_count(col)} faces, {hi.x - lo.x:.2f} x {hi.y - lo.y:.2f} x "
                  f"{hi.z - lo.z:.2f} m, lowest z {lo.z:.3f}")
    _person((where["sedan_red"].x - 0.8, -1.6))
    _person((where["jeepney"].x + 7.0, -1.8))
    ground = K.Buf("ground")
    ground.face([(-60, -60, 0), (80, -60, 0), (80, 60, 0), (-60, 60, 0)], "asphalt")
    ground.finish(bpy.context.scene.collection, bevel=0)
    K.setup_lighting()
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.sensor_fit = "HORIZONTAL"
    K.PREVIEWS.mkdir(parents=True, exist_ok=True)
    eye = 18 / math.tan(math.radians(47.5))   # 95 degrees across, the game's eye
    jx = where["jeepney"].x + 3.05
    mid = 15.0
    tx = where["tricycle"].x + 1.0
    cx = where["sedan_red"].x + 2.0
    for label, pos, look, lens in (
            ("lineup", Vector((mid + 17, -17, 11)), Vector((mid, 2.5, 0.6)), 30),
            ("eye", Vector((mid - 3, -8.0, 1.25)), Vector((mid - 1.0, 0.0, 1.1)), eye),
            ("jeepney", Vector((jx + 4.2, -4.6, 2.3)), Vector((jx - 0.1, 0.0, 1.05)), 30),
            ("jeepney_rear", Vector((jx - 9.5, -4.2, 2.2)), Vector((jx - 2.8, 0.0, 1.0)), 32),
            ("tricycle", Vector((tx + 3.2, -4.0, 1.8)), Vector((tx, 0.0, 0.8)), 32),
            ("cars", Vector((cx + 5.0, -6.5, 2.6)), Vector((cx, 0.0, 0.7)), 32)):
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (look - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(K.PREVIEWS / f"vehicles_{label}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[kanto-vehicles] preview", scene.render.filepath)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if "--preview" in argv:
        _preview(int(argv[argv.index("--preview") + 1]))
