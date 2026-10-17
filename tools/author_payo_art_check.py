"""PAYO, the art's CHECKS (2026-10-12). Run by tools/author_payo_art.py on the finished meshes, bevels applied.

  cover(layouts, kit, movers)   EVERY BLOCK'S TOP IS DRAWN BY ITS OWN PIECE, WHERE THE COLLIDER IS. For every layout and
                                every block in it (a stair where the layout says stair) and every mover: at the top's
                                centre and at the four points 60 per cent of the way to its corners, a ray straight down
                                from 1 cm over the collision surface must meet the piece between 3 cm below and 1 cm
                                above that surface. `hide_solids` is written true only when nothing fails. The pieces
                                are generated from the layout with their origin at the collision top's centre, so this
                                proves the generation and the bevel, not a hand placement.
  coplanar(objs)                NO Z-FIGHTING AMONG THE ART'S OWN FACES: pairs of faces in one plane (within 2 mm, facing
                                the same way) that overlap by more than 4 cm2. The land, the forest, the far world and the
                                falls are left out (rounded or sloping, never two in one plane by construction).
"""
import math

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import author_paete_run_art_kit as K

SKIP_OBJECTS = ("payo_mountain", "payo_paddy_water", "payo_forest", "payo_slopes", "payo_range_", "payo_clouds", "payo_falls", "payo_sky_", "payo_plant_")
SKIP_MATS = ("py_rock", "py_vine", "py_moss", "py_bulul", "py_fire", "py_fall", "py_cloud", "py_decal", "py_spill", "py_tree_trunk", "py_inlay")   # scattered growth, decals, round things


def tree_of(o):
    dg = bpy.context.evaluated_depsgraph_get()
    return BVHTree.FromObject(o, dg)


def cover(layouts, kit, movers):
    trees, fail, n, worst = {}, [], 0, 0.0

    def test(name, sx, sz, ramp=None):
        nonlocal n, worst
        if name not in kit:
            fail.append((name, "no such piece")); return
        if name not in trees:
            trees[name] = tree_of(kit[name])
        for (fx, fy) in ((0, 0), (0.3, 0.3), (-0.3, 0.3), (0.3, -0.3), (-0.3, -0.3)):
            x, y = fx * sx, fy * sz
            z = 0.0
            if ramp:
                d, drop = ramp
                t = (x / sx + 0.5) if d[1] == "x" else (y / sz + 0.5)
                z = -drop * (t if d[0] == "+" else 1.0 - t)
            hit = trees[name].ray_cast(Vector((x, y, z + 0.01)), Vector((0, 0, -1)), 2.0)
            n += 1
            if hit[0] is None:
                fail.append((name, "nothing under", (round(x, 2), round(y, 2)))); continue
            off = hit[0].z - z
            worst = max(worst, abs(off))
            if not (-0.03 <= off <= 0.01):
                fail.append((name, "art %.1f mm from the collision top" % (off * 1000), (round(x, 2), round(y, 2))))

    seen = set()
    for lay in layouts:
        for p in lay["pieces"]:
            st = "stairDown" in p
            name = "block_%s%s" % (p["id"], "_stair" if st else "")
            if name in seen:
                continue
            seen.add(name)
            test(name, p["sx"], p["sz"], (p["stairDown"].replace("z", "y"), p["top"] - p["low"]) if st else None)
        for m in lay.get("movers", []):
            name = movers[m["id"]]
            if name in seen:
                continue
            seen.add(name)
            test(name, m["sx"], m["sz"])
    ok = not fail
    text = ("%d pieces (every block id, every stair, every mover) have their own art within 3 cm below to 1 cm above the collision top at all five points; "
            "%d points fail; worst offset %.1f mm" % (len(seen), len(fail), worst * 1000))
    return {"ok": ok, "fail": fail, "text": text}


def mesh_data(o):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    mw = o.matrix_world
    verts = [mw @ v.co for v in me.vertices]
    polys = [tuple(p.vertices) for p in me.polygons]
    names = [m.name if m else "" for m in me.materials]
    mats = [names[p.material_index] if names else "" for p in me.polygons]
    ev.to_mesh_clear()
    return verts, polys, mats


def coplanar(objs, tol=0.002, min_area=0.0004, limit=40):
    buckets, nfaces = {}, 0
    for o in objs:
        if any(o.name.startswith(s) or o.data.name.startswith(s) for s in SKIP_OBJECTS):
            continue
        verts, polys, mats = mesh_data(o)
        for poly, mat in zip(polys, mats):
            if mat in SKIP_MATS:
                continue
            pts = [verts[i] for i in poly]
            n = Vector((0, 0, 0))
            for i in range(len(pts)):
                a, b = pts[i], pts[(i + 1) % len(pts)]
                n += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
            ar = n.length / 2
            if ar < min_area:
                continue
            n.normalize()
            d = n.dot(pts[0])
            rn = (round(n.x, 2), round(n.y, 2), round(n.z, 2))
            ax = max(range(3), key=lambda i: (abs(rn[i]), -i))    # from the ROUNDED normal, so every face of a plane is flattened the same way
            pick = {0: (1, 2), 1: (0, 2), 2: (0, 1)}[ax]
            p2 = [(p[pick[0]], p[pick[1]]) for p in pts]
            lo = (min(p[0] for p in p2), min(p[1] for p in p2)); hi = (max(p[0] for p in p2), max(p[1] for p in p2))
            key = (round(n.x, 2), round(n.y, 2), round(n.z, 2), int(math.floor(d / tol)))
            buckets.setdefault(key, []).append((d, lo, hi, p2, o.name, mat, n))
            nfaces += 1
    pairs, count, summary = [], 0, {}
    for key, faces in buckets.items():
        cand = list(faces)
        nb = buckets.get(key[:3] + (key[3] + 1,))
        for i, fa in enumerate(cand):
            others = cand[i + 1:] + (nb or [])
            for fb in others:
                if abs(fa[0] - fb[0]) > tol or fa[6].dot(fb[6]) < 0.999:
                    continue
                if fa[1][0] >= fb[2][0] or fb[1][0] >= fa[2][0] or fa[1][1] >= fb[2][1] or fb[1][1] >= fa[2][1]:
                    continue
                inter = K.intersect(K.ccw(fa[3]), K.ccw(fb[3]))
                if inter and abs(K.area2(inter)) > min_area:
                    count += 1
                    sk = (fa[4].rstrip("0123456789."), fa[5], fb[4].rstrip("0123456789."), fb[5], key[:3])
                    summary[sk] = summary.get(sk, 0) + 1
                    if len(pairs) < limit:
                        pairs.append((fa[4], fa[5], fb[4], fb[5], round(abs(K.area2(inter)), 4), "plane at %.3f" % fa[0], "A %s to %s" % (tuple(round(v, 2) for v in fa[1]), tuple(round(v, 2) for v in fa[2])),
                                      "B %s to %s" % (tuple(round(v, 2) for v in fb[1]), tuple(round(v, 2) for v in fb[2])), key[:3]))
    text = "%d overlapping coplanar pairs among %d faces of the stage, the pit, the shelf, the gate and the kit as placed (same plane within 2 mm, same facing, over 4 cm2)" % (count, nfaces)
    return {"count": count, "pairs": pairs, "text": text, "summary": sorted(summary.items(), key=lambda kv: -kv[1])}
