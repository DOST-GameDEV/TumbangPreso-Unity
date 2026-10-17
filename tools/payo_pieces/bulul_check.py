"""PAYO, THE GUARDIANS: is the skin closed? Lists every edge that does not have exactly two faces.

  blender -b --python tools/payo_pieces/bulul_check.py -- <name> [<name> ...]
"""
import os
import sys

import bmesh
import bpy

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
WORK = os.path.join(ROOT, "Logs", "payo", "pieces", "bulul", "work")
for name in sys.argv[sys.argv.index("--") + 1:]:
    bpy.ops.wm.open_mainfile(filepath=os.path.join(WORK, name + "_low.blend"))
    me = bpy.data.objects[name].data
    bm = bmesh.new(); bm.from_mesh(me)
    bad = [e for e in bm.edges if len(e.link_faces) != 2]
    print("CHECK", name, "faces", len(bm.faces), "bad edges", len(bad))
    for e in bad[:12]:
        m = (e.verts[0].co + e.verts[1].co) / 2
        print("   faces on edge", len(e.link_faces), "length %.4f" % e.calc_length(), "at x %.2f y(up) %.2f z(front) %.2f" % (m.x, m.z, -m.y))
    bm.free()
