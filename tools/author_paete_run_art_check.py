"""PAETE'S RUN, the art's CHECKS (2026-10-10). Run by tools/author_paete_run_art.py on the finished meshes, bevels applied.

  cover(objs)     EVERY LAYOUT SOLID'S TOP IS DRAWN BY ART. For each solid: its top face's centre and the four points at
                  60 per cent (the same five PaeteRunSceneBuilder.HideWhatTheArtCovers casts at), a ray straight down
                  from 1 cm over each; art must be met within 3 cm below to 1 cm above the collision's surface at all
                  five. A point that lies INSIDE another solid's body (a landing's top under a deck, a post's top under
                  the stone it carries) cannot be stood on and is counted as buried, not as a failure, if art is not
                  there. `hide_solids` is written true only when nothing fails.
  coplanar(objs)  NO Z-FIGHTING AMONG THE ART'S OWN FACES: pairs of faces in one plane (within 2 mm, facing the same
                  way) that overlap by more than a few square centimetres.
  lane(objs)      Nothing of the art stands in a lane: over every walked top and along every leg's line, from 15 cm to
                  2.2 m up, a ray must meet no art that is not itself a layout solid's piece.
"""
import math

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import author_paete_run_art_kit as K
import author_paete_run_art_world as W

UP = Vector((0, 0, 1))


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


def tree_of(objs, extra=()):
    verts, polys, tag = [], [], []
    for o in objs:
        v, p, m = mesh_data(o)
        base = len(verts)
        verts += v
        polys += [tuple(i + base for i in q) for q in p]
        tag += [(o.name, mm) for mm in m]
    for (name, v, p) in extra:
        base = len(verts)
        verts += v
        polys += [tuple(i + base for i in q) for q in p]
        tag += [(name, "pr_rock")] * len(p)
    return BVHTree.FromPolygons(verts, polys, all_triangles=False, epsilon=0.0), tag


def samples(s):
    if s.shape == "box":
        return [s.M @ Vector((a * 0.6 * s.sz / 2, b * 0.6 * s.sx / 2, s.sy / 2)) for a, b in ((0, 0), (-1, -1), (-1, 1), (1, -1), (1, 1))]
    return [Vector((s.c.x + a * 0.6 * s.r, s.c.y + b * 0.6 * s.r, s.top)) for a, b in ((0, 0), (-1, -1), (-1, 1), (1, -1), (1, 1))]


def inside_solid(p, skip=None, grow=-0.01):
    for o in W.SOLIDS:
        if o is skip:
            continue
        if o.shape == "box":
            q = o.M.inverted() @ p
            if abs(q.x) < o.sz / 2 + grow and abs(q.y) < o.sx / 2 + grow and abs(q.z) < o.sy / 2 + grow:
                return o
        elif math.hypot(p.x - o.c.x, p.y - o.c.y) < o.r * 0.99 + grow and o.bot - grow < p.z < o.top + grow:
            return o
    return None


def cover(objs, below=0.03, above=0.01):
    tree, tag = tree_of(objs)
    fails, buried, worst = [], 0, 0.0
    for s in W.SOLIDS:
        bad = []
        for i, p in enumerate(samples(s)):
            hit = tree.ray_cast(p + UP * (above + 0.0004), -UP, above + below + 0.0008)
            if hit[0] is not None:
                worst = max(worst, abs(hit[0].z - p.z))
                continue
            o = inside_solid(p, s)
            if o is not None:
                buried += 1
                continue
            far = tree.ray_cast(p + UP * 3.0, -UP, 9.0)
            bad.append((i, None if far[0] is None else round(far[0].z - p.z, 3), tag[far[2]] if far[0] is not None else None))
        if bad:
            fails.append((s.name, s.kind, bad))
    return fails, buried, worst


def cloth(static_objs, extra, cloth_points, near=0.03, own=("pr_rope", "pr_timber", "pr_bamboo")):
    """NO CLOTH THROUGH STONE: every cloth vertex (banners, awnings, pennants) must be more than 3 cm from every mesh that
    is not cloth, its own rod, pole or string excepted. Returns [(what, how many of its points, the nearest, where)]."""
    tree, tag = tree_of(static_objs, extra)
    bad = []
    for what, pts in cloth_points:
        n, worst = 0, None
        for p in pts:
            h = tree.find_nearest(p, near)
            if h[0] is not None and tag[h[2]][1] not in own and not tag[h[2]][1].startswith("pr_cloth"):
                n += 1
                if worst is None or h[3] < worst[0]:
                    worst = (round(h[3], 3), tag[h[2]], (round(p.x, 1), round(p.y, 1), round(p.z, 1)))
        if n:
            bad.append((what, n) + worst)
    return bad


def junctions(SO):
    """Where two walked tops meet at one height the WINNER (author_paete_run_art_solids.beats: a round platform over a
    plaza over a deck) is drawn whole and the loser not at all inside it. Returns (pairs, [violations])."""
    pairs = SO.joints()
    bad = []
    for (w, l) in pairs:
        wp = SO.poly_of(w)
        n = 0
        for (name, p, z) in SO.TOPS:
            if name == l.name:
                cx, cy = sum(q[0] for q in p) / len(p), sum(q[1] for q in p) / len(p)
                if K.inside(wp, cx, cy, 0.03):
                    n += 1
        if n:
            bad.append((w.name, l.name, n))
    return pairs, bad


def terrain(margin=0.15):
    """THE GROUND GOES UNDER EVERY SOLID: at a grid of points over each solid's top (edges included) the terrain must be
    at least 6 cm under it. Returns [(solid, how far the ground stands over the allowed height, where)]."""
    bad = []
    for s in W.SOLIDS:
        worst = None
        if s.shape == "box":
            na, nb = max(2, int(s.sz / 0.9)), max(2, int(s.sx / 0.9))
            pts = [s.M @ Vector((s.sz * (i / na - 0.5), s.sx * (j / nb - 0.5), s.sy / 2)) for i in range(na + 1) for j in range(nb + 1)]
        else:
            pts = [Vector((s.c.x + s.r * e * math.cos(a), s.c.y + s.r * e * math.sin(a), s.top)) for e in (0.0, 0.5, 1.0) for a in [math.tau * k / 12 for k in range(12)]]
        for p in pts:
            over = W.ground(p.x, p.y) - (p.z - margin)
            if over > 0 and (worst is None or over > worst[0]):
                worst = (round(over, 3), (round(p.x, 1), round(p.y, 1), round(p.z, 2)))
        if worst:
            bad.append((s.name,) + worst)
    return bad


def coplanar(objs, tol=0.002, min_area=0.0004, skip_mats=("pr_rock", "pr_far_rock", "pr_ground", "pr_vine"), limit=60):
    faces = []
    for o in objs:
        verts, polys, mats = mesh_data(o)
        for poly, mat in zip(polys, mats):
            if mat in skip_mats:
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
            c = sum(pts, Vector((0, 0, 0))) / len(pts)
            lo = [min(p[k] for p in pts) for k in range(3)]; hi = [max(p[k] for p in pts) for k in range(3)]
            faces.append((n, c, lo, hi, pts, o.name, mat))
    cells = {}
    CELL = 4.0
    for idx, (n, c, lo, hi, pts, on, mat) in enumerate(faces):
        nk = (round(n.x * 12), round(n.y * 12), round(n.z * 12))
        for i in range(int(math.floor(lo[0] / CELL)), int(math.floor(hi[0] / CELL)) + 1):
            for j in range(int(math.floor(lo[1] / CELL)), int(math.floor(hi[1] / CELL)) + 1):
                for k in range(int(math.floor(lo[2] / CELL)), int(math.floor(hi[2] / CELL)) + 1):
                    cells.setdefault((i, j, k), []).append((nk, idx))
    seen, found = set(), []
    for lst in cells.values():
        if len(lst) < 2:
            continue
        lst.sort()
        for u in range(len(lst)):
            nk, ia = lst[u]
            na, ca, loa, hia, pa, oa, ma = faces[ia]
            for v in range(u + 1, len(lst)):
                nkb, ib = lst[v]
                if abs(nkb[0] - nk[0]) > 1:
                    break
                if abs(nkb[1] - nk[1]) > 1 or abs(nkb[2] - nk[2]) > 1 or (ia, ib) in seen:
                    continue
                nb, cb, lob, hib, pb, ob, mb = faces[ib]
                if na.dot(nb) < 0.9995 or abs(nb.dot(ca - cb)) > tol or abs(na.dot(cb - ca)) > tol:
                    continue
                if any(loa[k] > hib[k] - 0.01 or lob[k] > hia[k] - 0.01 for k in range(3) if hia[k] - loa[k] > 0.02 and hib[k] - lob[k] > 0.02):
                    continue
                seen.add((ia, ib))
                ax = na.orthogonal().normalized()
                ay = na.cross(ax)
                A = K.ccw([((p - ca).dot(ax), (p - ca).dot(ay)) for p in pa])
                B = K.ccw([((p - ca).dot(ax), (p - ca).dot(ay)) for p in pb])
                X = K.intersect(A, B)
                if X and abs(K.area2(X)) > min_area:
                    q = ca + ax * (sum(p[0] for p in X) / len(X)) + ay * (sum(p[1] for p in X) / len(X))
                    found.append((round(abs(K.area2(X)), 4), (round(q.x, 2), round(q.y, 2), round(q.z, 3)), oa, ma, ob, mb, (round(na.x, 2), round(na.y, 2), round(na.z, 2))))
    found.sort(reverse=True)
    return found, len(faces)


def lane(objs, extra=(), skip_mats=("pr_vine", "pr_leaf_light", "pr_leaf_dark", "pr_water", "pr_water_running", "pr_rope")):
    tree, tag = tree_of(objs, extra)
    hits = []

    def probe(p, z0, z1, what):
        at = Vector((p.x, p.y, z1))
        while True:
            h = tree.ray_cast(at, -UP, at.z - z0)
            if h[0] is None:
                return
            if inside_solid(h[0], None, 0.14) is not None or any(o.reach(h[0].x, h[0].y) < 0.15 and o.top > h[0].z for o in W.SOLIDS):
                return                                            # a solid's own piece, or what holds one up
            if tag[h[2]][1] not in skip_mats:
                hits.append((what, (round(h[0].x, 1), round(h[0].y, 1), round(h[0].z, 2)), tag[h[2]]))
                return
            at = h[0] - UP * 0.01
            if at.z <= z0:
                return

    for s in W.SOLIDS:                                            # over every walked top
        if s.kind not in ("sand", "rock", "teal", "wood") or s.name.startswith(("pool_", "tier_", "cliff_", "buttress")):
            continue
        if s.shape == "box":
            na, nb = max(1, int(s.sz / 1.4)), max(1, int(s.sx / 1.4))
            pts = [s.M @ Vector(((i + 0.5) / na * s.sz - s.sz / 2, (j + 0.5) / nb * s.sx - s.sx / 2, s.sy / 2)) for i in range(na) for j in range(nb)]
            if s.kind == "teal" and "wall" in s.name:
                continue
        else:
            pts = [Vector((s.c.x + a * s.r * 0.55, s.c.y + b * s.r * 0.55, s.top)) for a, b in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1))]
        for p in pts:
            if inside_solid(p + UP * 0.2, s, 0.0) is None:
                probe(p, p.z + 0.012, p.z + 2.2, "over " + s.name)        # from 1.2 cm up: a plank hovering over a floor is caught too
    for row in W.SAMP:                                            # along every leg's line (hops, swings, dives: the air a body flies through)
        x, y, z, half, below, above, leg_i = row[:7]
        for off in (0.0,):
            p = Vector((x, y, z))
            probe(p, z - min(below, 6.0) + 0.3, z + 2.3, "leg %d line" % int(leg_i))
    seen, out = set(), []
    for h in hits:
        key = (h[0], h[2])
        if key not in seen:
            seen.add(key); out.append(h)
    return out, len(hits)
