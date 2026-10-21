"""Render the MOTION review of one character-redesign prototype, in Blender, no Unity.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_motion.py -- dante v22
    ... -- cheska v03 --glb path/to/other.glb
    py -3 tools/sheet_character_redesign_motion.py dante v22

Shared by every character of the redesign (docs/CHARACTER_REDESIGN_DANTE.md). It reads
Assets/TumbangPreso/Art/CharacterRedesign/<id>/<id>-redesign.glb and writes single frames into
Logs/character-redesign-<id>/<version>/motion/. The sheet script then joins them into ONE gif
for the character (owner, 2026-10-05: "the animations i want them all in one gif for each
character") and a still sheet to LOOK at, because a gif cannot be judged frame by frame.

Segments, each from three cameras (front, side, back three-quarter):
  spin    the idle pose turned through 360 degrees
  idle, walk, sprint, jump, fall    the clips the .glb carries under those names
  look    a RIG TEST, not a game clip: the head bone alone turned 55 degrees each way and
          nodded 22, over the idle pose. The game drives the head in code, so cloth and hair
          round the neck must survive this.
The toon ramp and ink edge are the approximation of tools/render_character_redesign_dante.py.
"""
import math
import os
import sys

import bpy
from mathutils import Quaternion, Vector

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
args = sys.argv[sys.argv.index("--") + 1:]
WHO, VERSION = args[0], args[1]
GLB = args[args.index("--glb") + 1] if "--glb" in args else "%s/Assets/TumbangPreso/Art/CharacterRedesign/%s/%s-redesign.glb" % (REPO, WHO, WHO)
OUT = "%s/Logs/character-redesign-%s/%s/motion" % (REPO, WHO, VERSION)
os.makedirs(OUT, exist_ok=True)

# the toon material, ink hull, lights and camera of the Dante render script, with no shot run
_helpers = REPO + "/tools/render_character_redesign_dante.py"
_argv = sys.argv
sys.argv = sys.argv[:sys.argv.index("--")] + ["--", "_helpers", "none"]
g = {"__file__": _helpers, "__name__": "helpers"}
exec(compile(open(_helpers, encoding="utf-8").read(), "render", "exec"), g)
sys.argv = _argv

VIEWS = (("front", -12, 8), ("side", 90, 6), ("back", 148, 10))
SIZE = (400, 500)


def cameras():
    return [(name, g["camera"]((0.0, 0, 0.42), 6.0, lens=150, res=SIZE, azim=azim, elev=elev)) for name, azim, elev in VIEWS]


def shoot(tag, i, cams):
    for name, cam in cams:
        bpy.context.scene.camera = cam
        g["render"]("%s/%s_%s_%03d.png" % (OUT, tag, name, i))


def load(pose):
    g["scene"]()
    root, arm, _ = g["import_character"](GLB, None, pose=pose)
    return root, arm


# spin
root, arm = load("idle")
cams = cameras()
for i in range(24):
    root.rotation_euler.z = math.radians(i * 15.0)
    shoot("spin", i, cams)

# the clips
for clip in ("idle", "walk", "sprint", "jump", "fall"):
    try:
        root, arm = load(clip)
    except SystemExit as e:
        print("SKIPPED", clip, e)
        continue
    act = arm.animation_data.action
    first, last = [int(round(x)) for x in act.frame_range]
    cams = cameras()
    for i, f in enumerate(range(first, max(last, first + 1))):
        bpy.context.scene.frame_set(f)
        shoot(clip, i, cams)
    print("CLIP", clip, last - first, "frames")

# the head-turn rig test
root, arm = load("idle")
bpy.context.scene.frame_set(0)
arm.animation_data.action = None
pb = arm.pose.bones["head"]
pb.rotation_mode = "QUATERNION"
base = pb.rotation_quaternion.copy()
rest = pb.bone.matrix_local.to_quaternion()
cams = cameras()
KEYS = [(0, 0, 0), (10, 55, 0), (16, 55, 0), (36, -55, 0), (42, -55, 0), (52, 0, 0), (60, 0, -22), (64, 0, -22), (74, 0, 20), (78, 0, 20), (86, 0, 0)]


def at(f):
    for (f0, y0, p0), (f1, y1, p1) in zip(KEYS, KEYS[1:]):
        if f0 <= f <= f1:
            t = (f - f0) / float(f1 - f0)
            t = t * t * (3 - 2 * t)
            return y0 + (y1 - y0) * t, p0 + (p1 - p0) * t
    return 0, 0


for i, f in enumerate(range(0, 86, 2)):
    yaw, pitch = at(f)
    world = Quaternion(Vector((0, 0, 1)), math.radians(yaw)) @ Quaternion(Vector((1, 0, 0)), math.radians(pitch))
    pb.rotation_quaternion = base @ (rest.inverted() @ world @ rest)
    bpy.context.view_layer.update()
    shoot("look", i, cams)
print("MOTION FRAMES IN", OUT)
