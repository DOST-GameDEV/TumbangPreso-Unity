"""Author the shared retrieval slide on one existing rig at a time.

Run with Blender 5.2 --background --python tools/author_retrieval_slide.py -- FILE.
The surgical GLB export preserves every original byte of mesh, skin, material,
texture and existing clip data. Only the named slide animation is appended.
No retargeting and no replacement skeleton. Import the result in Blender to edit
the native action named slide. --blend FILE also saves that authoring scene.

The rigid seven-bone rig has no knees or elbows. A split-leg hip skid, asymmetric
ground sweep and delayed bracing recovery express commitment within that limit.
All movement along the ground belongs to the motor; only pelvis height is keyed.

⚠️⚠️ THE JOINT ANGLES ARE NOT THE ANIMATION. THE REACH IS, AND ONE SET OF
ANGLES DOES NOT PRODUCE IT ON TWO DIFFERENT SKELETONS. Where a hand ends up is the
joint angle times the limb length, and this cast does not share limb lengths.
Measured across all twenty-two roster rigs by `tools/inspect_slide_rig.py`, the right
arm is as long as the shoulder is high on twenty of them (ratio 1.008 to 1.023, so
the fingertips graze the ground with the arm hanging straight down) and 0.712 on
`team-sean` and `team-iggy`, whose shoulder stands at 0.430 above a 0.306 arm. Those
two CANNOT touch the floor at any arm angle: they are 0.124 short standing still.
Applied blind, the shared pose left their hand 0.101 above the ground across the
whole contact window,
which is a character miming a pickup rather than making one.

So the reach is SOLVED per rig rather than posed, and `REACH_FRACTION` below is the
target. See its comment for why the target is not zero.
"""
import argparse
import json
import math
from pathlib import Path
import sys

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from glb_action import Rig, append_action, rotations as bone_rotations

# Seconds, root yaw/roll, torso pitch/yaw/roll, head pitch, left/right leg
# pitch, left/right arm pitch/roll. The fast drop precedes the long vulnerable
# recovery. At 0.342 s the motor stops; the body has to lever itself back up.
BEATS = [
    (0.00,  0,  0,  10,  0,  0, -8, -12, 12, -8, -6, -20, 5),
    (0.06, -5, -3,  35, -6, -3, -22, -48, 43, 12, -15, -75, 8),
    (0.14,-12, -7,  68, -8, -5, -28, -82, 78,  15, -14, -112, 5),
    (0.25,-10, -8,  74, -5, -6, -28, -86, 80,  12, -18, -125, 3),
    (0.342,-8,-7,  66,  4, -4, -30, -83, 76,  10, -20, -115, 8),
    (0.46, -6,-5,  47,  8, -3, -30, -65, 62, -8, -24, -75, 12),
    (0.61, -4,-3,  34,  6, -2, -22, -42, 40, -5, -16, -45, 10),
    (0.78, -1,-1,  18,  2,  0, -12, -18, 20, -10, -8, -22, 6),
    (0.95,  0, 0,   0,  0,  0,   0,   0,  0,   0,  0,   0, 0),
]


# ⚠️⚠️ THE HAND STOPS SHORT OF THE GROUND ON PURPOSE, AND A TARGET OF ZERO
# WOULD DELETE THE SKID. Pelvis height is solved from whatever the LOWEST skinned
# vertex is, so the moment the hand becomes that vertex the solve lifts the whole body
# to put the HAND on the floor and the hip comes off it. The skid is the move; a
# character balanced on one fingertip is not. The hand therefore has to stay just
# above the hip contact, and this is how far above.
#
# ⚠️ THE NUMBER IS MEASURED, NOT PICKED. It is the median clearance of the twelve
# Classic `character-*.glb` rigs as first authored on 2026-09-07, taken over the three
# contact beats (0.14, 0.25, 0.342) and divided by each rig's own authored height,
# which is what makes it carry across a 0.598 m rig and a 1.000 m one. Those twelve
# spread from 0.00 to 12.25 per cent because nothing was solving for it. Re-derive it
# with `tools/inspect_slide_rig.py` over the cast rather than nudging it.
REACH_FRACTION = 0.0617

# ⚠️⚠️ THE SOLVE IS AGAINST THE WHOLE CONTACT WINDOW AND NOT AGAINST ITS DEEPEST
# BEAT, WHICH IS THE BUG THAT FOUND THIS LINE. Solving at 0.25 s alone rolled
# `character-male-b` 23.4 degrees, because that rig's clearance at 0.25 was the
# cast's worst at 12.25 per cent. Its clearance at 0.14 was already 6.27 per cent,
# so the same roll drove THAT beat to zero: the hand became the lowest vertex, the
# pelvis solve lifted the body to stand on it, and the hip stopped skidding a tenth
# of a second before the pose the solver was looking at. The three beats are one
# move and the binding one is whichever is lowest.
CONTACT = (0.14, 0.25, 0.342)

# The correction rides the torso's own pitch curve, so it is full at the deepest
# contact and exactly zero at 0.95 s where that curve returns to rest. A clip that
# ended on a correction would leave the body rolled when the motor takes control back.
MAX_TORSO_PITCH = max(b[3] for b in BEATS)


def pose(t):
    for a, b in zip(BEATS, BEATS[1:]):
        if t <= b[0] + 1e-8:
            u = max(0, min(1, (t-a[0])/(b[0]-a[0])))
            u = u*u*(3-2*u)
            v = [x+(y-x)*u for x,y in zip(a[1:],b[1:])]
            yaw,roll,tp,ty,tr,h,ll,lr,al,alz,ar,arz = v
            return {'root':(0,yaw,roll), 'torso':(tp,ty,tr),
                    'head':(h,-ty*.5,0), 'leg-left':(ll,0,0),
                    'leg-right':(lr,0,0), 'arm-left':(al,0,alz),
                    'arm-right':(ar,0,arz)}
    raise ValueError(t)


def author(path):
    # ⚠️ THE SURGERY IS `tools/glb_action.py`'s NOW, AND WHAT IS LEFT HERE IS THE POSE.
    # It was one file until the hero casts needed the same buffer append; a second
    # transcription of it would have been the copy that drifts. `Rig` loads and asserts,
    # `append_action` writes and asserts, and this function decides where the body goes.
    rig = Rig(path)
    assert all(n in rig.ids for n in pose(0))

    # ⚠️ THE REACHING HAND IS THE VERTICES THE RIGHT ARM OWNS, found by weight rather
    # than by name: this rig has no hand bone, so the arm is one rigid limb whose far
    # end is the hand by geometry alone.
    hand = rig.owned_by('arm-right')
    assert hand, 'no vertices are owned by arm-right; the reach cannot be measured'

    def rotations(t, roll):
        """The pose at t, with the solved reach roll on the torso's own envelope."""
        angles = dict(pose(t))
        pitch, yaw, tilt = angles['torso']
        angles['torso'] = (pitch, yaw, tilt + roll * pitch / MAX_TORSO_PITCH)
        return bone_rotations(angles)

    def clearance(t, roll):
        matrices = rig.posed(rotations(t, roll))
        low = rig.lowest(matrices)
        return min(rig.height_of(v, matrices) for v in hand) - low

    def reach(roll):
        return min(clearance(t, roll) for t in CONTACT)

    # ⚠️⚠️ ROLL, NOT PITCH, AND THAT WAS MEASURED BOTH WAYS BEFORE IT WAS CHOSEN.
    # Extra torso PITCH also brings the hand down and folds the whole silhouette with
    # it: on `team-sean` the 20 degrees that reach the ground drop the head from 74 to
    # 61 per cent of standing height, so the tall character face-plants while the rest
    # of the cast skids. Extra torso ROLL drops the REACHING SHOULDER alone, which is
    # what a person with short arms actually does to sweep a floor: 20 degrees moves
    # `team-sean`'s clearance from 12.2 to 5.3 per cent and its head from 74.0 to 73.2.
    # ⚠️ And extra ARM pitch is the one that does not work at all. The authored -125
    # degrees is already past the bottom of the arm's arc, so more of it swings the
    # hand back UP: 20 degrees more made the gap WORSE, 0.104 to 0.181.
    target = REACH_FRACTION * rig.height
    reach_roll = 0.0
    if reach(0.0) > target:
        lo, hi = 0.0, 40.0
        for _ in range(40):
            mid = (lo + hi) / 2
            if reach(mid) > target:
                lo = mid
            else:
                hi = mid
        reach_roll = hi
    # ⚠️ A rig that needs the whole 40 degrees has not been solved, it has been
    # clamped, so the report says so rather than letting a bad pose through quietly.
    reach_clamped = reach_roll >= 39.999

    times = sorted(set([round(i/60, 8) for i in range(58)] + [b[0] for b in BEATS]))
    order = list(pose(0))
    tracks = {name: [] for name in order}
    heights = []

    for t in times:
        rots = rotations(t, reach_roll)
        matrices = rig.posed(rots)
        heights.append((0.0, rig.floor - rig.lowest(matrices), 0.0))
        for n, q in rots.items():
            tracks[n].append((q.x, q.y, q.z, q.w))

    report = append_action(rig, 'slide', times, tracks, heights, order=order)
    report.update(pelvis_drop=round(-min(v[1] for v in heights), 5),
                  reach_roll_deg=round(reach_roll, 3),
                  reach_roll_clamped=reach_clamped,
                  reach_at_contact=round(reach(reach_roll), 5),
                  reach_fraction=round(reach(reach_roll) / rig.height, 5),
                  reach_fraction_unsolved=round(reach(0.0) / rig.height, 5),
                  reach_per_beat={str(t): round(clearance(t, reach_roll) / rig.height, 5)
                                  for t in CONTACT})
    return report


if __name__=='__main__':
    ap=argparse.ArgumentParser()
    ap.add_argument('file',type=Path)
    ap.add_argument('--blend',type=Path)
    args=ap.parse_args(sys.argv[sys.argv.index('--')+1:])
    report=author(args.file)
    if args.blend:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=str(args.file.resolve()))
        action=bpy.data.actions.get('slide')
        assert action is not None, 'Blender must import the exact action name'
        arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
        arm.animation_data.action=action
        if action.slots:
            arm.animation_data.action_slot=action.slots[0]
        for track in arm.animation_data.nla_tracks:
            track.mute=True
        bpy.context.scene.frame_end=round(.95*bpy.context.scene.render.fps)
        bpy.context.scene.frame_set(round(.25*bpy.context.scene.render.fps))
        bpy.ops.wm.save_as_mainfile(filepath=str(args.blend.resolve()))
    print('SLIDE_REPORT '+json.dumps(report))
