"""Bakes the fall of Basilio's broken court plate (CONTINENTAL DRIFT's cutscene) as a rigid-body simulation, once.

  blender -b --python tools/bake_dante_plate_break.py -- <folder to write HeroIntroductionScene.DanteBreak.cs into>

Why a bake: the owner asked for the plate to break up as the longhorn's back comes up under it, "preferrably dropping
with simulated physics". The cutscene runs on its own clock with the game's physics paused, is scrubbed by the film, and
must be the same picture on every peer, so the simulation is run HERE, once, and the cutscene plays the result back by
its own time (`HeroIntroductionScene.Dante.cs`, `SampleDanteBreak`). It is still simulated: rigid slabs with convex
colliders, thrown by the back rising under them, knocking off its dome, shoulders, head and horns, and falling into
the sea. Nothing in it can touch the game's own colliders, because it never runs in the game.

Everything is typed in the STAGE's space (metres, +x his right, y up, +z ahead; Blender is given x, z, y). The plate is
the centre plate of the court (`DnCore`), cut into five big pieces and two corner bits round a small middle piece that
is under his feet. The middle piece is not simulated: it stays under him and is ground away by the stage.

The longhorn here is a handful of plain shapes moving up exactly as the cutscene lifts it (`_dante`'s `rise` keys in
`tools/author_ultimate_intros.py`): a dome for its back, a box for each shoulder, one for its head, one for each horn,
and a post where he stands so that nothing passes through him.

Writes HeroIntroductionScene.DanteBreak.cs: the pieces' outlines, and for each simulated piece and each sixtieth of a
second its place, the way its front points and the way its top points.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

OUT = sys.argv[sys.argv.index("--") + 1]

START, FPS, FRAMES, THICK, TOP = 3.65, 60, 100, 0.10, 0.07
RISE = [(0.0, 0.0), (3.45, 0.0), (4.0, 0.45), (5.2, 3.4), (7.0, 3.4)]
BACK = 3.4      # the height of its back when fully risen: its parts are typed at that height and ride `lift - BACK`


def lift(t):
    """The cutscene's own lift (`UltimatePerformance.LiftAt`): eased between the typed keys."""
    if t <= RISE[0][0]:
        return RISE[0][1]
    for (t0, y0), (t1, y1) in zip(RISE, RISE[1:]):
        if t <= t1:
            u = (t - t0) / (t1 - t0)
            return y0 + (y1 - y0) * u * u * (3 - 2 * u)
    return RISE[-1][1]


def heave(t):
    """The knock the plate takes as the back meets it: the dome is thrown a hand's width further up for a fifth of a second."""
    u = (t - START) / 0.22
    return 0.32 * math.sin(math.pi * u) if 0 < u < 1 else 0.0


# The plate's six corners (`DnCore`), the points its breaks pass through, and the pieces, each typed corner by corner.
C0, C1, C2, C3, C4, C5 = (1.55, .45), (.70, 1.75), (-.95, 1.50), (-1.70, .05), (-.80, -1.55), (.95, -1.40)
P0, P1, P2, P3, P4 = (.45, .15), (.10, .55), (-.45, .30), (-.40, -.35), (.20, -.45)
A, B, C, D = (1.30, .83), (1.20, .355), (-.35, -1.51), (-.67, -1.15)
PIECES = [
    [A, C1, P1, P0, B],          # front right
    [C1, C2, P2, P1],            # front
    [C2, C3, C4, D, P3, P2],     # the whole left side: the biggest
    [C, C5, P4, P3, D],          # back
    [C5, C0, B, P0, P4],         # right
    [C0, A, B],                  # a corner bit
    [C4, C, D],                  # a corner bit
    [P0, P1, P2, P3, P4],        # the middle, under his feet: NOT simulated
]
SIMULATED = 7


def to_blender(x, y, z):
    return (x, z, y)


def mesh_object(name, verts, faces, at):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    ob.location = at
    bpy.context.scene.collection.objects.link(ob)
    return ob


def rigid(ob, kind, shape, mass=1.0, friction=0.4, bounce=0.2):
    bpy.context.view_layer.objects.active = ob
    for o in bpy.context.scene.objects:
        o.select_set(o is ob)
    bpy.ops.rigidbody.object_add(type=kind)
    rb = ob.rigid_body
    rb.collision_shape = shape
    rb.mass, rb.friction, rb.restitution = mass, friction, bounce
    rb.use_margin, rb.collision_margin = True, 0.004
    if kind == "PASSIVE":
        rb.kinematic = True
    return ob


def box(name, half, at):
    hx, hy, hz = half
    v = [(sx * hx, sy * hy, sz * hz) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
    f = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    return mesh_object(name, v, f, at)


bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = FPS
scene.frame_start, scene.frame_end = 1, FRAMES + 2
scene.gravity = (0, 0, -14.0)
bpy.ops.rigidbody.world_add()
world = scene.rigidbody_world
world.substeps_per_frame = 12
world.solver_iterations = 30
world.point_cache.frame_start, world.point_cache.frame_end = 1, FRAMES + 2

rest = lift(START) + TOP - THICK * 0.5
pieces = []
for i, outline in enumerate(PIECES[:SIMULATED]):
    cx = sum(p[0] for p in outline) / len(outline)
    cz = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    # Blender: x, then the stage's z, then up.
    verts = [(p[0] - cx, p[1] - cz, -THICK / 2) for p in outline] + [(p[0] - cx, p[1] - cz, THICK / 2) for p in outline]
    faces = [tuple(range(n)), tuple(range(n, 2 * n))] + [(k, (k + 1) % n, n + (k + 1) % n, n + k) for k in range(n)]
    ob = mesh_object("piece%d" % i, verts, faces, to_blender(cx, rest, cz))
    area = 0.5 * abs(sum(outline[k][0] * outline[(k + 1) % n][1] - outline[(k + 1) % n][0] * outline[k][1] for k in range(n)))
    rigid(ob, "ACTIVE", "CONVEX_HULL", mass=max(0.2, area * 2.0), friction=0.22, bounce=0.22)
    pieces.append((ob, cx, cz))

# The longhorn, as plain shapes typed at its risen height: (name, shape, half size, where).
bpy.ops.mesh.primitive_uv_sphere_add(radius=2.6, segments=24, ring_count=12, location=(0, 0, 0))
dome = bpy.context.active_object
dome.name = "dome"
movers = [(rigid(dome, "PASSIVE", "SPHERE", friction=0.12), (0.0, BACK - 2.6 - 0.06, -0.1), True)]
# ⚠️ ITS SHOULDERS AND HEAD ARE TIPPED OUTWARD HERE, more than the model's are. With level tops the first bake left three
# pieces riding up on them to the end of the lift, and the plate has to be gone before then. Each: half size, where, and
# how far its top is tipped (degrees about Blender's x, then y).
for name, half, at, tip in [("shoulder-l", (.9, 1.0, 1.5), (-2.6, 1.5, .55), (0, -24)), ("shoulder-r", (.9, 1.0, 1.5), (2.6, 1.5, .55), (0, 24)),
                            ("head", (1.15, .9, .85), (0.0, 1.2, 2.6), (-27, 0)), ("horn-l", (.35, .3, .8), (-1.9, 2.4, 2.55), (0, -20)),
                            ("horn-r", (.35, .3, .8), (1.9, 2.4, 2.55), (0, 20)), ("him", (.38, .38, .75), (0.0, BACK + 1.0, 0.0), (0, 0))]:
    ob = box(name, half, (0, 0, 0))
    ob.rotation_euler = (math.radians(tip[0]), math.radians(tip[1]), 0)
    movers.append((rigid(ob, "PASSIVE", "BOX", friction=0.12), at, False))

for f in range(1, FRAMES + 3):
    t = START + (f - 1) / FPS
    for ob, at, heaves in movers:
        ob.location = to_blender(at[0], at[1] + lift(t) - BACK + (heave(t) if heaves else 0.0), at[2])
        ob.keyframe_insert("location", frame=f)

rows, landed = [], []
for i in range(SIMULATED):
    rows.append([]); landed.append(-1)
for f in range(1, FRAMES + 2):
    scene.frame_set(f)
    bpy.context.view_layer.update()
    for i, (ob, cx, cz) in enumerate(pieces):
        m = ob.matrix_world
        p = m.translation
        front = m.to_3x3() @ Vector((0, 1, 0))     # the piece's own stage +z
        up = m.to_3x3() @ Vector((0, 0, 1))        # its own stage +y
        rows[i].append((p.x, p.z, p.y, front.x, front.z, front.y, up.x, up.z, up.y))
        if landed[i] < 0 and p.z < 0.06 and f > 2:
            landed[i] = f - 1


def vec2(points):
    return "new[] { " + ", ".join("new Vector2(%sf, %sf)" % (repr(round(x, 4)), repr(round(z, 4))) for x, z in points) + " }"


lines = []
lines.append("// ⚠️ WRITTEN BY tools/bake_dante_plate_break.py. Do not edit: change that script and run it again.")
lines.append("// The centre plate's pieces, and the rigid-body fall of the simulated ones, a sixtieth of a second a row.")
lines.append("using UnityEngine;")
lines.append("")
lines.append("namespace TumbangPreso.Visual")
lines.append("{")
lines.append("    public sealed partial class HeroIntroductionScene")
lines.append("    {")
lines.append("        private const int DnBreakFrames = %d, DnBreakSimulated = %d;" % (FRAMES + 1, SIMULATED))
lines.append("        private const float DnBreakRate = %df, DnBreakThick = %sf, DnBreakTop = %sf;" % (FPS, repr(THICK), repr(TOP)))
lines.append("        // Each piece's outline (x, z) in the stage's space. The last is the middle one under his feet, which is not simulated.")
lines.append("        private static readonly Vector2[][] DnBreakPiece =")
lines.append("        {")
for outline in PIECES:
    lines.append("            " + vec2(outline) + ",")
lines.append("        };")
lines.append("        // The frame each simulated piece meets the sea on, or -1 if it had not by the end.")
lines.append("        private static readonly int[] DnBreakLanded = { " + ", ".join(str(v) for v in landed) + " };")
lines.append("        // Piece by piece, frame by frame: where it is (3), which way its front points (3), which way its top points (3).")
lines.append("        private static readonly float[] DnBreakPath =")
lines.append("        {")
for i in range(SIMULATED):
    for row in rows[i]:
        lines.append("            " + ", ".join("%.4ff" % v for v in row) + ",")
lines.append("        };")
lines.append("    }")
lines.append("}")
with open(os.path.join(OUT, "HeroIntroductionScene.DanteBreak.cs"), "w", encoding="utf-8", newline="\n") as fh:
    fh.write("\n".join(lines) + "\n")
print("baked", SIMULATED, "pieces,", FRAMES + 1, "frames; landed on", landed)
for i in range(SIMULATED):
    print(i, ["%.2f" % rows[i][k][1] for k in (0, 10, 20, 30, 45, 60, 80, 100)], "far", "%.2f" % math.hypot(rows[i][-1][0], rows[i][-1][2]))
