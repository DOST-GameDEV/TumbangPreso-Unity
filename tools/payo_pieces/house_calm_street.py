"""The CALM roll-out's group shots for the house and gate families (Logs/payo/pieces/CALM_STYLE.md). Blender 5.0.

  blender -b --python tools/payo_pieces/house_calm_street.py -- <shots dir>
  then python tools/payo_pieces/house_calm_street.py sheet <shots dir> <out.png> [note]   (system Python)

1. The six houses as a STREET on the stepped slope of Logs/payo/pieces/house/work/street.json (three terraces), old
   against calm, from 40 m and 150 m.
2. The gate group: the portal with its stair in front, the two totems flanking, the guardian pair before them, old
   against calm, from 40 m and 150 m.
The stage, the light, the ink hull and the toon finish are calm_review.py's own (its functions are run from its
source, not copied), so these shots match the per-piece sheets. Every path is absolute.
"""
import json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
APP = os.path.join(ROOT, "ArtSource", "payo", "pieces-approved-2026-10-12")
CALM = os.path.join(ROOT, "Logs", "payo", "pieces", "calm")


def blender_main(shots):
    import bpy
    from mathutils import Vector
    src = open(os.path.join(HERE, "calm_review.py"), encoding="utf-8").read()
    src = src[:src.index('\nif args[0] == "piece"')]                  # the functions, not its command line
    sys_argv = sys.argv
    sys.argv = [sys_argv[0], "--", "none"]
    ns = {"__name__": "calm_review_lib"}
    exec(compile(src, os.path.join(HERE, "calm_review.py"), "exec"), ns)
    sys.argv = sys_argv
    stage, load, aim, shoot, show = ns["stage"], ns["load"], ns["aim"], ns["shoot"], ns["show"]
    os.makedirs(shots, exist_ok=True)

    def ground_box(lo, hi, mat):
        bpy.ops.mesh.primitive_cube_add(size=1, location=[(a + b) / 2 for a, b in zip(lo, hi)])
        o = bpy.context.object
        o.scale = [b - a for a, b in zip(lo, hi)]
        o.data.materials.append(mat)
        return o

    def place(meshes, at, rz):
        """Rotate a loaded piece about z through its own origin, then move it (the roots are the glb's nodes)."""
        roots = {o for o in meshes}
        roots = [o for o in roots if o.parent is None or o.parent not in roots]
        for o in roots:
            o.rotation_mode = "XYZ"
            o.rotation_euler[2] += math.radians(rz)
            o.location = Vector(o.location) + Vector(at)

    # ---------------------------------------------------------- 1. the street
    sc, cam = stage((1200, 680))
    street = json.load(open(os.path.join(ROOT, "Logs", "payo", "pieces", "house", "work", "street.json")))
    gmat = bpy.data.materials["ground"] if "ground" in bpy.data.materials else None
    for lo, hi in street["ground"]:
        ground_box(lo, hi[:2] + [hi[2] + 0.004], gmat)              # a hair above the stage plane: no z-fighting
    A, B, W = [], [], []                                  # old, calm, and calm with the OPTIONAL weathered pilot tower
    wt = os.path.join(CALM, "house", "tower_weathered", "house_tower.glb")
    for h in street["houses"]:
        n = h["name"]
        sets = [(A, os.path.join(APP, "house", n + ".glb")), (B, os.path.join(CALM, "house", n + ".glb"))]
        if n == "house_tower" and os.path.exists(wt):
            sets.append((W, wt))
        for lst, path in sets:
            ms, _, _ = load(path)
            place(ms, h["at"], h["rz"])
            lst += ms
    tower_calm = [o for o in B if o.name.startswith("house_tower")]
    look = Vector((1.0, 14.0, 6.0))
    d = Vector((0.42, -1.0, 0)).normalized()
    for dist, up, fov in ((40, 24.0, 54), (150, 60.0, 22)):
        aim(cam, look + d * dist + Vector((0, 0, up)), look, fov)
        show(W, False)
        show(A, True); show(B, False); shoot(sc, os.path.join(shots, "street_old_%dm.png" % dist))
        show(A, False); show(B, True); shoot(sc, os.path.join(shots, "street_calm_%dm.png" % dist))
        if W and dist == 40:
            show(tower_calm, False); show(W, True); shoot(sc, os.path.join(shots, "street_calmw_%dm.png" % dist))

    # ---------------------------------------------------------- 2. the gate group
    sc, cam = stage((1200, 680))
    group = [("gate_portal", (0, 0, 0), 0), ("gate_stair", (0, -14.5, 0), 0), ("gate_totem_a", (-15.5, -7, 0), 8),
             ("gate_totem_b", (15.5, -7, 0), -8), ("gate_guardians", (0, -26, 0), 0)]
    A, B = [], []
    for n, at, rz in group:
        for lst, path in ((A, os.path.join(APP, "gate", n + ".glb")), (B, os.path.join(CALM, "gate", n + ".glb"))):
            ms, _, _ = load(path)
            place(ms, at, rz)
            lst += ms
    look = Vector((0, -8.0, 5.0))
    for dist, up, fov in ((40, 12.0, 58), (150, 45.0, 22)):
        aim(cam, look + d * dist + Vector((0, 0, up)), look, fov)
        show(A, True); show(B, False); shoot(sc, os.path.join(shots, "gates_old_%dm.png" % dist))
        show(A, False); show(B, True); shoot(sc, os.path.join(shots, "gates_calm_%dm.png" % dist))
    print("done")


def sheet(shots, out, note=""):
    sys.path.insert(0, HERE)
    import calm_sheet
    rows = []
    for grp, label in (("street", "six houses as a street"), ("gates", "gate group")):
        for dist in ("40m", "150m"):
            rows.append((dist, [("OLD " + label, os.path.join(shots, "%s_old_%s.png" % (grp, dist))),
                                ("CALM " + label, os.path.join(shots, "%s_calm_%s.png" % (grp, dist)))]))
    w = os.path.join(shots, "street_calmw_40m.png")
    if os.path.exists(w):
        rows.insert(2, ("40m", [("CALM street, pilot tower as approved", os.path.join(shots, "street_calm_40m.png")),
                                ("CALM street, OPTION: pilot tower re-run with the weathered thatch", w)]))
    calm_sheet.grid(rows, "PAYO houses and gates together", out, note)


if __name__ == "__main__":
    if "--" in sys.argv:
        blender_main(os.path.abspath(sys.argv[sys.argv.index("--") + 1]))
    elif sys.argv[1] == "sheet":
        sheet(sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else "")
