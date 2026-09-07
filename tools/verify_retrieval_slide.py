"""Independently sample the exported slide through Blender's glTF importer.

Run Blender --background --python tools/verify_retrieval_slide.py -- FILE.
This measures deformed meshes, not keyframe presence or a frame-zero photograph.
It does not substitute for the Unity motion-strip probe in TODO 151.16.

⚠️⚠️ EVERY BOUND HERE IS A FRACTION OF THE RIG'S OWN STANDING HEIGHT, AND ONE OF
THEM WAS AN ABSOLUTE NUMBER UNTIL 2026-09-07, WHICH IS THE FAULT THAT FOUND IT.
The body-lowering check read `> 0.1` and `team-nemu` failed it at **0.0983** while
being a perfectly good slide: that rig stands **0.598** against the Classic
reference's 0.671 and the shipped cast's 1.000, so a metre-and-a-bit bound written
from one rig refuses a shorter one for being short. It is the same fault
`docs/VISION.md` § 2 records about the ability footprints, where a rule stated in
two units drifted into two different rules, and `CLAUDE.md` § 6.2c's *"what is this
size measured AGAINST"* one level down. Measured across the twenty-two roster rigs,
the drop runs **13.28 to 29.0 per cent** of each rig's own height, so the floor is
12 per cent and it is one claim rather than twenty-two.
"""
import json
from pathlib import Path
import sys
import bpy

# The silhouette has to visibly go down, as a fraction of this rig's own height.
# Measured range across the cast is 0.1328 (team-bayan, team-dante) to 0.290
# (character-female-a); 0.12 clears the lowest with margin and refuses a clip that
# barely crouches.
BODY_DROP_FLOOR = 0.12

# ⚠️⚠️ THE REACH IS THE CLIP. `author_retrieval_slide.REACH_FRACTION` solves the
# right hand to 6.17 per cent of standing height at the deepest contact; this is the
# INDEPENDENT check that the solve survived the export and Blender's own importer,
# measured off deformed vertices rather than off the author's arithmetic. The bound
# is deliberately looser than the target: it is asking "did the hand get near the
# ground", not "did the solver hit its number to three places". An unsolved
# long-legged rig reads 12.2 per cent here and is what this refuses.
REACH_CEILING = 0.10

# The three beats where the body is committed and the hand is sweeping.
CONTACT = (0.14, 0.25, 0.342)

path = Path(sys.argv[sys.argv.index('--') + 1])
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 60
bpy.ops.import_scene.gltf(filepath=str(path.resolve()))
arm = next(o for o in scene.objects if o.type == 'ARMATURE')
clip = bpy.data.actions.get('slide')
assert clip is not None, 'missing exact slide action'
arm.animation_data.action = clip
arm.animation_data.action_slot = clip.slots[0]
for track in arm.animation_data.nla_tracks:
    track.mute = True
meshes = [o for o in scene.objects
          if o.type == 'MESH' and any(m.type == 'ARMATURE' for m in o.modifiers)]
assert meshes

# ⚠️ THE HAND IS FOUND BY WEIGHT, ON THE IMPORTED FILE, and the indices are taken off
# the UNDEFORMED mesh on purpose: the armature modifier moves vertices and never
# reorders them, so an index set read once stays correct at every frame. Reading
# groups off the evaluated mesh instead would be re-deriving the same answer nine
# times and trusting the modifier stack not to have dropped the group.
hand = {}
for o in meshes:
    group = o.vertex_groups.get('arm-right')
    if group is None:
        continue
    hand[o.name] = {v.index for v in o.data.vertices
                    if sum(g.weight for g in v.groups if g.group == group.index) > 0.5}
assert any(hand.values()), 'no vertices are weighted to arm-right; the reach is unmeasurable'

samples = []
for t in [0, .06, .14, .25, .342, .46, .61, .78, .95]:
    scene.frame_set(int(t * 60), subframe=t * 60 - int(t * 60))
    deps = bpy.context.evaluated_depsgraph_get()
    verts, fingers = [], []
    for o in meshes:
        ev = o.evaluated_get(deps)
        mesh = ev.to_mesh()
        owned = hand.get(o.name, set())
        for v in mesh.vertices:
            world = ev.matrix_world @ v.co
            verts.append(world)
            if v.index in owned:
                fingers.append(world)
        ev.to_mesh_clear()
    samples.append({'time': t, 'floor': round(min(v.z for v in verts), 5),
                    'top': round(max(v.z for v in verts), 5),
                    'hand': round(min(v.z for v in fingers), 5) if fingers else None,
                    'forward_extent': round(-min(v.y for v in verts), 5)})

standing = samples[-1]['top']
drop = (standing - min(s['top'] for s in samples)) / standing
contact = [s for s in samples if s['time'] in CONTACT]
reach = min((s['hand'] - s['floor']) / standing for s in contact)

assert min(s['floor'] for s in samples) > -.005, samples
assert drop > BODY_DROP_FLOOR, f'insufficient body lowering: {drop:.4f} of {standing:.4f}'
assert reach < REACH_CEILING, f'the hand never reaches the ground: {reach:.4f} of standing height'
assert abs(clip.frame_range[1] / 60 - .95) < .001
print('IMPORTED_SLIDE ' + json.dumps({
    'file': path.name, 'standing_height': round(standing, 5),
    'body_drop_fraction': round(drop, 5), 'reach_fraction': round(reach, 5),
    'samples': samples}))
