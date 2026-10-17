"""PAYO: the SACRED POOL below the gorge mouth, a sibling of the court platform (Blender 5.0, headless).

  blender -b --python tools/payo_pieces/pool_build.py            (court_run.sh runs it after the court)

Why: the owner circled the old pool as broken. The gorge's water rectangle, a round pool disc and the river ribbon's
start overlapped with hard straight edges, and the land banks where the pool met the gorge walls came out as jagged
green triangles (Logs/payo/terrain/dressed_d46_gorge_from_the_pool.png). So the gorge now OPENS into an authored
stone pool, and the water of gorge, pool and the outlet's first metres is ONE surface.

Same family and atlas as court_platform (material py_court_stone, the court's atlas), water on py_water (the name the
head piece's water uses). Blender metres, the court's frame (x east, y north to the valley, z up, court top z 0).

  plan        the gorge walls (x = +-30) end at the platform's front face (y 56). From there the pool's wall runs
              on: 2 m north, then a 45 degree chamfer in to x = +-20 (y 68), straight to y 80, a chamfer to
              (+-11, 89), across to the outlet (x = +-5), and the outlet's walls north to y 98. A soft stepped
              octagon about 40 m across, centred near (0, 74).
  heights     water -45.5 (gorge, pool and outlet to the lip), floor -48 (outlet floor past the lip -48.3);
              rim cap -44.75 .. -44.3, 0.15 over the water and 1.2 back over the land; outside it the skirt steps
              down twice (-45.4, -46.05) and then drops as a coursed face to -52, so the land can meet it anywhere
              between -44.75 and -52.
  outlet      a spillway lip across the outlet at y 91.4 .. 92.6, top -45.65; past it the water is at -45.6 to
              y 98, where two end piers close the rim. The river starts there.
Writes Logs/payo/pieces/calm/court/pool_basin_raw.glb and ArtSource/payo/pool_basin.blend; pool_finish.py then adds
the normal map and tangents (calm_glb.swap) and the top-surface height grid is pool_height_grid.py.
"""
import math
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import court_build as C  # noqa: E402
import court_layout as L  # noqa: E402
from mathutils import Vector  # noqa: E402

RAW = os.path.join(C.OUT, "pool_basin_raw.glb")
BLEND = os.path.join(C.ROOT, "ArtSource", "payo", "pool_basin.blend")

WATER, FLOOR, FLOOR_OUT, RIM = -45.5, -48.0, -48.3, -44.3
CAP_B = RIM - 0.45
LIP = (91.4, 92.6, -45.65)
WATER_OUT, Y_END = -45.6, 98.0
SKIRT_FOOT = -52.0
# the east wall line, from the platform's face to the outlet's end (the west one is its mirror)
EAST = [(30.0, 56.0), (30.0, 58.0), (20.0, 68.0), (20.0, 80.0), (11.0, 89.0), (5.0, 89.0), (5.0, Y_END)]
# both rim paths run with the WATER on their right (d > 0 is over the water, d < 0 the land)
P_EAST = EAST[::-1]
P_WEST = [(-x, y) for (x, y) in EAST]


def water_outline():
    """The water's edge, CCW: the gorge (with its back recess), the pool, the outlet to the lip, 0.1 m under the walls."""
    e = 0.1
    east = [(22.0, 28.5 - e), (22.0, 30.0), (30.0 + e, 30.0), (30.0 + e, 58.0 + e * 0.41), (20.0 + e, 68.0 + e * 0.41),
            (20.0 + e, 80.0 - e * 0.41), (11.0 + e * 0.41, 89.0 + e), (5.0 + e, 89.0 + e)]
    west = [(-x, y) for (x, y) in east[::-1]]
    return east + [(5.0 + e, LIP[1] - 0.6)] + [(-5.0 - e, LIP[1] - 0.6)] + west


def build_water():
    mat = bpy.data.materials.get("py_water") or bpy.data.materials.new("py_water")
    mat.use_nodes = True
    b = mat.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.30, 0.62, 0.66, 0.85)
    b.inputs["Alpha"].default_value = 0.85
    b.inputs["Roughness"].default_value = 0.9
    mat.blend_method = "BLEND" if hasattr(mat, "blend_method") else None
    wa = C.MB("pool_basin_water")
    white = (1, 1, 1, 1)
    ring = [(x, y, WATER) for (x, y) in water_outline()]
    wa.face(ring, [(p[0] / 8, p[1] / 8) for p in ring], [white] * len(ring))
    y0, y1 = LIP[1] - 0.6, LIP[1]                      # the spill over the lip, then the outlet's water
    for (ya, yb, za, zb) in ((y0, y1, WATER, WATER_OUT), (y1, Y_END + 0.5, WATER_OUT, WATER_OUT)):
        q = [(-5.1, ya, za), (5.1, ya, za), (5.1, yb, zb), (-5.1, yb, zb)]
        wa.face(q, [(p[0] / 8, p[1] / 8) for p in q], [white] * 4)
    return wa.obj(mat)


def wall_line(mb, pts, z_lo, z_hi, s0, inward, colf):
    s = s0
    for a, b in zip(pts, pts[1:]):
        C.wall_seg(mb, tuple(a), tuple(b), z_lo, z_hi, s, inward=inward, colf=colf, plain=True)
        s += (C.V2(b) - C.V2(a)).length


def build_stone(walls, rim, blocks, floor):
    deep = lambda z: C.tint(0.80 + 0.14 * min(1.0, max(0.0, (z - FLOOR) / (RIM - FLOOR))), 0.4)
    for k, path in enumerate((P_WEST, P_EAST)):
        s0 = 40.0 * k
        # the wall the water sees, from under the floor to the cap
        wall_line(walls, path, FLOOR_OUT - 0.2, CAP_B, s0, False, deep)
        walls.new_island()
        # the cap (0.15 over the water, the woven band on its land face) and the stepped skirt
        C.sweep(rim, path, False, [
            ((0.0, CAP_B), (0.15, CAP_B), "cap_under_in", (0, -1)),
            ((0.15, CAP_B), (0.15, RIM), "cap_in", (1, 0)),
            ((0.15, RIM), (-1.2, RIM), "cap_top", (0, 1)),
            ((-1.2, RIM), (-1.2, CAP_B), "cap_out", (-1, 0)),
            ((-1.2, CAP_B), (-1.05, CAP_B), "cap_under_out", (0, -1)),
            ((-1.05, CAP_B), (-1.05, -45.4), "lip_out", (-1, 0)),
            ((-1.05, -45.4), (-2.3, -45.4), "cap_top", (0, 1)),
            ((-2.3, -45.4), (-2.3, -46.05), "lip_out", (-1, 0)),
            ((-2.3, -46.05), (-3.55, -46.05), "cap_top", (0, 1)),
        ], s0, colf=C.g_const(1.0))
        rim.new_island()
        outer = [tuple(p) for p in C.offset_poly(path, -3.55)]
        wall_line(walls, outer, SKIRT_FOOT, -46.05, -s0, True, C.g_wall)
        walls.new_island()
    # the end piers where the river starts, and the spillway lip
    for sx in (1, -1):
        x0, x1 = sorted((sx * 4.75, sx * 8.9))
        C.prism(blocks, [(x0, 96.7), (x1, 96.7), (x1, 99.2), (x0, 99.2)], SKIRT_FOOT, RIM + 0.45, sides="wall", colf=C.g_const(1.0))
        C.prism(blocks, [(x0 - 0.15, 96.55), (x1 + 0.15, 96.55), (x1 + 0.15, 99.35), (x0 - 0.15, 99.35)], RIM + 0.4, RIM + 0.75,
                colf=C.g_const(1.04))
    C.prism(blocks, [(-5.05, LIP[0]), (5.05, LIP[0]), (5.05, LIP[1]), (-5.05, LIP[1])], FLOOR_OUT - 0.1, LIP[2], colf=C.g_const(0.98))
    # the floors: the pool (and the outlet to the lip) at -48, past the lip at -48.3; plain stone under the water
    pool = [(x, y) for (x, y) in EAST[1:6]]
    ring = [(-30.0, 56.0), (30.0, 56.0)] + pool[0:] + [(-x, y) for (x, y) in pool[::-1]]
    # ring so far: (-30,56) (30,56) (30,58) (20,68) (20,80) (11,89) (5,89) (-5,89) (-11,89) ... (-30,58)
    ring = ring[:7] + [(5.0, LIP[0]), (-5.0, LIP[0])] + ring[7:]
    pts = [(x, y, FLOOR) for (x, y) in ring]
    P = L.PLAIN
    floor.face(pts, [C.px(P[0] + 10 + (x + 30) / 60 * (P[2] - 20), P[1] + 10 + (92 - y) / 40 * (P[3] - 20)) for (x, y, _) in pts],
               [C.tint(0.76, 0.5)] * len(pts))
    q = [(-5.0, LIP[1], FLOOR_OUT), (5.0, LIP[1], FLOOR_OUT), (5.0, Y_END + 1.0, FLOOR_OUT), (-5.0, Y_END + 1.0, FLOOR_OUT)]
    floor.face(q, [C.patch_uv(L.DARK, p[0] + 5, p[1] - LIP[1]) for p in q], [C.tint(0.76, 0.5)] * 4)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mat = C.material(False)
    walls, rim, blocks, floor = (C.MB(n) for n in ("pool_walls", "pool_rim", "pool_blocks", "pool_floor"))
    build_stone(walls, rim, blocks, floor)
    objs = [walls.obj(mat, 0.0), rim.obj(mat, 0.05), blocks.obj(mat, 0.05), floor.obj(mat, 0.0)]
    water = build_water()
    tris = C.export(objs, RAW, "pool_basin", extra=[water])
    C.material(True)
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("POOL BASIN: %d triangles -> %s, %s" % (tris, RAW, BLEND))


if __name__ == "__main__":
    main()
