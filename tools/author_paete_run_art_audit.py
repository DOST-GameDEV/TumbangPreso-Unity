"""PAETE'S RUN: DOES THE ART STAND WHERE THE GAME IS? An audit of the art against the CURRENT layout (2026-10-10).

  blender -b --python tools/author_paete_run_art_audit.py            (builds the art in memory, exports nothing)

Owner: "a lot of the map doesnt make any sense. it seems like you put the designed layout for the map but the real route
doesnt align with it... for the swinging parts there are arch ways fully decorated but the main thing to swing on is a
pillar with a hanging block on it. and the slide has the blocky one you probe played on, and the designed version."
(The scene he played wore art exported BEFORE the layout moved.) Since then every stage piece is built from
paete_run_layout.json at build time, solid by solid, anchor by anchor; this file proves it each time.

PRINTS, per leg 1..36 and per shortcut: layout solids whose top the art draws / all of them; vine anchors (and the
movers' hanging points) with art at them / all; the largest distance between a solid's top and the art's surface on it;
and ORPHAN ART: faces of the walked art (paving, the slides' channels, their water) that lie more than 0.5 m from every
layout solid. WRITES Logs/paete-run/art/audit/legNN.png (and cutN.png): the racer's eye at the leg's start looking at
its end, the layout drawn over the art as thin red wire, so a piece that stands off its solid is seen at once; and
Logs/paete-run/art/audit/audit.txt, the table.
"""
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent


def key_of(tag):
    if tag.isdigit():
        return "leg %02d" % int(tag)
    if tag.startswith("wp"):
        return "leg %02d" % max(1, int(tag[2:]))
    if tag.startswith("s") and "." in tag:
        return "shortcut %d" % (int(tag[1:].split(".")[0]) + 1)
    return "leg 01"


def run(walk, sc, logs, cams_only=None, render=True):
    import bpy
    from mathutils import Vector
    import author_paete_run_art_kit as K
    import author_paete_run_art_world as W
    import author_paete_run_art_check as CHK
    UP = Vector((0, 0, 1))
    tree, tag = CHK.tree_of(walk)
    rows = {}

    def row(k):
        return rows.setdefault(k, dict(solids=0, covered=0, anchors=0, anchored=0, worst=0.0, orphans=0, overhang=0, missing=[]))

    for s in W.SOLIDS:
        r = row(key_of(s.tag))
        r["solids"] += 1
        ok, worst = True, 0.0
        for p in CHK.samples(s):
            hit = tree.ray_cast(p + UP * 0.0104, -UP, 0.0412)
            if hit[0] is None:
                if CHK.inside_solid(p, s) is None:
                    ok = False
            else:
                worst = max(worst, abs(hit[0].z - p.z))
        r["covered"] += ok
        r["worst"] = max(r["worst"], worst)
        if not ok:
            r["missing"].append(s.name)
    for a in W.LAYOUT["vine_anchors"]:                            # the thing a vine catches IS a decorated thing
        r = row(key_of(str(a["leg"])))
        r["anchors"] += 1
        p = W.B(a["c"])
        if a.get("mover"):
            p = p + UP * 4.0                                      # a hanging chain: its top must be on a beam
        near = tree.find_nearest(p, 0.75)
        if near[0] is not None:
            r["anchored"] += 1
        else:
            r["missing"].append(a["name"])
    for v in W.MOVERS:                                            # every chain ends on a beam
        if v.kind not in ("boulder_chain", "pendulum"):
            continue
        r = row(key_of(v.tag))
        r["anchors"] += 1
        p = v.pivot if v.kind == "pendulum" else Vector((v.c.x, v.c.y, v.c.z + v.h / 2 + (9.0 - 0.055) * (v.r / 1.1)))
        if tree.find_nearest(p + UP * 0.3, 0.75)[0] is not None:
            r["anchored"] += 1
        else:
            r["missing"].append(v.name)
    orphans = []
    for o in walk:
        if o.name not in ("run_paving", "run_slides", "run_slide_water"):
            continue
        verts, polys, mats = CHK.mesh_data(o)
        for poly, mat in zip(polys, mats):
            if o.name == "run_slides" and mat in ("pr_rock", "pr_ashlar_dark"):
                continue                                          # the rock shoulder and the underside are the channel's setting
            c = sum((verts[i] for i in poly), Vector((0, 0, 0))) / len(poly)
            best, who = 9.0, None
            for s in W.SOLIDS:
                if abs(c.x - s.c.x) > s.rad + 1.5 or abs(c.y - s.c.y) > s.rad + 1.5 or c.z > s.top + 0.6 + s.sz * abs(s.fwd.z) or c.z < s.bot - 2.0 - s.sz * abs(s.fwd.z):
                    continue
                d = s.reach(c.x, c.y)
                if d < best:
                    best, who = d, s
            if best > 0.5:
                orphans.append((o.name, mat, (round(c.x, 1), round(c.y, 1), round(c.z, 1))))
                near, dd = W.near_legs(c.x, c.y, 30.0)
                row("leg %02d" % int(near[dd.argmin()][6]) if len(dd) and near[dd.argmin()][6] < 100 else "leg 01")["orphans"] += 1
    # FLOOR THAT IS NOT FLOOR (owner fell through a deck drawn wider than its solid): level art faces at a walked solid's
    # height (30 cm under it to 2 cm over) lying 0.25 to 1.6 m OUTSIDE its footprint, with no other solid under them
    over = []
    flat_kinds = ("sand", "rock", "wood")
    for o in walk:
        if o.name not in ("run_masonry", "run_paving"):
            continue
        verts, polys, mats = CHK.mesh_data(o)
        for poly, mat in zip(polys, mats):
            pts = [verts[i] for i in poly]
            n = (pts[1] - pts[0]).cross(pts[2] - pts[0])
            if n.length < 0.03 or n.z / n.length < 0.9:
                continue
            c = sum(pts, Vector((0, 0, 0))) / len(pts)
            hit = None
            for s in W.SOLIDS:
                if s.kind not in flat_kinds or s.pitched or abs(c.x - s.c.x) > s.rad + 1.7 or abs(c.y - s.c.y) > s.rad + 1.7 or not (s.top - 0.3 <= c.z <= s.top + 0.02):
                    continue
                d = s.reach(c.x, c.y)
                if d <= 0.25:
                    hit = None
                    break
                if d < 1.6:
                    hit = s
            if hit is not None and not any(o2.reach(c.x, c.y) <= 0.25 and o2.bot - 0.1 <= c.z <= o2.top + 0.05 for o2 in W.SOLIDS):
                over.append((hit.name, mat, (round(c.x, 1), round(c.y, 1), round(c.z, 2))))
                row(key_of(hit.tag))["overhang"] += 1
    lines = ["%-12s %-10s %-10s %-10s %-11s %s" % ("", "solids", "anchors", "worst mm", "orphan art", "floor past a solid's edge (over 0.25 m)")]
    tot = dict(solids=0, covered=0, anchors=0, anchored=0, orphans=0, overhang=0)
    for k in sorted(rows):
        r = rows[k]
        for t in tot:
            tot[t] += r[t]
        lines.append("%-12s %-10s %-10s %-10.1f %-11d %d%s" % (k, "%d/%d" % (r["covered"], r["solids"]), "%d/%d" % (r["anchored"], r["anchors"]), r["worst"] * 1000, r["orphans"], r["overhang"],
                                                        ("   MISSING: " + ", ".join(r["missing"])) if r["missing"] else ""))
    lines.append("%-12s %-10s %-10s %-10s %-11d %d" % ("ALL", "%d/%d" % (tot["covered"], tot["solids"]), "%d/%d" % (tot["anchored"], tot["anchors"]), "", tot["orphans"], tot["overhang"]))
    for o in over[:25]:
        lines.append("floor past the edge of %s: %s at %s" % o)
    for o in orphans[:20]:
        lines.append("orphan: %s %s at %s" % o)
    out = Path(logs) / "audit"
    out.mkdir(parents=True, exist_ok=True)
    (out / "audit.txt").write_text("\n".join(lines), encoding="utf-8")
    for ln in lines:
        print("[audit]", ln)
    if not render:
        return lines
    # ---- the layout as thin red wire, over the art
    wire = K.Buf("audit_wire", 3)
    mat = bpy.data.materials.new("audit_red")
    mat.use_nodes = True
    b = mat.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (1, 0, 0, 1); b.inputs["Emission Color"].default_value = (1, 0.02, 0.02, 1); b.inputs["Emission Strength"].default_value = 3.0
    K.MAT["audit_red"] = ("", (1, 0, 0), 1.0, "")
    for s in W.SOLIDS:
        if s.shape == "box":
            c = [s.M @ Vector((x * s.sz / 2, y * s.sx / 2, z * s.sy / 2)) for z in (-1, 1) for (x, y) in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
            edges = [(c[i], c[(i + 1) % 4]) for i in range(4)] + [(c[4 + i], c[4 + (i + 1) % 4]) for i in range(4)] + [(c[i], c[i + 4]) for i in range(4)]
        else:
            ring = lambda z: [Vector((s.c.x + s.r * math.cos(math.tau * k / 16), s.c.y + s.r * math.sin(math.tau * k / 16), z)) for k in range(16)]
            lo, hi = ring(s.bot), ring(s.top)
            edges = [(hi[k], hi[(k + 1) % 16]) for k in range(16)] + [(lo[k], lo[(k + 1) % 16]) for k in range(16)] + [(lo[k], hi[k]) for k in range(0, 16, 4)]
        for a, b_ in edges:
            wire.tube([a, b_], 0.02, "audit_red", n=3, caps=False)
    for a in W.LAYOUT["vine_anchors"]:
        p = W.B(a["aim"])
        for d in (Vector((0.5, 0, 0)), Vector((0, 0.5, 0)), Vector((0, 0, 0.5))):
            wire.tube([p - d, p + d], 0.03, "audit_red", n=3, caps=False)
    wo = wire.obj(0.0)
    sc.render.resolution_x, sc.render.resolution_y = 960, 540
    cams = [("leg%02d" % leg.i, leg) for leg in W.LEGS] + [("cut%d" % (k + 1), leg) for k, leg in enumerate(W.CUTS)]
    done = []
    for name, leg in cams:
        if cams_only and name not in cams_only:
            continue
        cd = bpy.data.cameras.new(name); co = bpy.data.objects.new("audit_" + name, cd); sc.collection.objects.link(co)
        cd.clip_start, cd.clip_end, cd.sensor_fit, cd.angle_y = 0.2, 3000, "VERTICAL", math.radians(63)
        e = leg.A - leg.f * 2.5 + UP * 1.6
        co.location = e
        co.rotation_euler = ((leg.C + UP * 1.0) - e).to_track_quat("-Z", "Y").to_euler()
        sc.camera = co
        sc.render.filepath = str(out / (name + ".png"))
        bpy.ops.render.render(write_still=True)
        done.append(name)
    wo.hide_render = True
    print("[audit] %d pictures in %s" % (len(done), out))
    return lines


if __name__ == "__main__":
    import runpy
    sys.argv = [a for a in sys.argv if a != "--"] + ["--", "--no-export", "--audit"] + (sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    runpy.run_path(str(HERE / "author_paete_run_art.py"), run_name="__main__")
