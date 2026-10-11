"""Stills of any clip on a Classic redesign, for looking at the action clips (throw, pick up, tag, slide, sit ...).

    blender -b --python tools/render_character_redesign_classic_clips.py -- <id> [clip ...] [--at 0.1,0.5,0.9] [--glb path] [--back]

Writes Logs/character-redesign-classic/clips/<id>/<clip>_<percent>_<view>.png, front three-quarter and side, and
with --back a third from directly behind, where the player's camera is.
`tools/sheet_character_redesign_classic_clips.py <id>` tiles them. It borrows the lights, the toon material and the
ink outline from the character's own render script, so a still here looks like that character's other sheets.
"""
import math
import os
import sys

import bpy

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
args = sys.argv[sys.argv.index("--") + 1:]
WHO = args[0]
AT = (0.0, 0.25, 0.5, 0.75, 1.0)
GLB = None
BACK = False     # --back adds a third view from directly behind (azimuth 180), named "back"
clips = []
i = 1
while i < len(args):
    if args[i] == "--at":
        AT = tuple(float(x) for x in args[i + 1].split(",")); i += 2
    elif args[i] == "--glb":
        GLB = args[i + 1]; i += 2
    elif args[i] == "--back":
        BACK = True; i += 1
    else:
        clips.append(args[i]); i += 1
ACTIONS = ["holding-right", "holding-right-shoot", "pick-up", "attack-melee-right", "interact-right",
           "slide", "crouch", "sit", "die", "emote-yes", "emote-no"]
clips = clips or ACTIONS

# the character's own render script, up to where it starts rendering
source = open(REPO + "/tools/render_character_redesign_%s.py" % WHO, encoding="utf-8").read()
source = source[:source.index("\nTURN = ")]
argv = sys.argv
sys.argv = ["x", "--", "clips"]
space = {"__file__": REPO + "/tools/render_character_redesign_%s.py" % WHO, "__name__": "helpers"}
exec(compile(source, "helpers", "exec"), space)
sys.argv = argv

OUT = REPO + "/Logs/character-redesign-classic/clips/" + WHO
os.makedirs(OUT, exist_ok=True)
VIEWS = (("q", 32.0), ("side", 90.0)) + ((("back", 180.0),) if BACK else ())

for clip in clips:
    space["scene"]()
    root, arm = space["put"](GLB or space["NEW"], pose=clip)
    act = arm.animation_data.action
    first, last = act.frame_range
    for fr in AT:
        f = first + (last - first) * fr
        bpy.context.scene.frame_set(int(f), subframe=f - int(f))
        for vname, azim in VIEWS:
            cam = space["camera"]((0, 0, 0.40), 5.2, lens=135, elev=8, azim=azim, res=(520, 600))
            space["render"]("%s/%s_%03d_%s.png" % (OUT, clip, int(round(fr * 100)), vname))
            bpy.data.objects.remove(cam)
