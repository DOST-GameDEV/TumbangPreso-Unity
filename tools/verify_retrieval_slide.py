"""Independently sample the exported slide through Blender's glTF importer.

Run Blender --background --python tools/verify_retrieval_slide.py -- FILE.
This measures deformed meshes, not keyframe presence or a frame-zero photograph.
It does not substitute for the Unity motion-strip probe in TODO 151.16.
"""
import json
from pathlib import Path
import sys
import bpy

path=Path(sys.argv[sys.argv.index('--')+1])
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.fps=60
bpy.ops.import_scene.gltf(filepath=str(path.resolve()))
arm=next(o for o in scene.objects if o.type=='ARMATURE')
clip=bpy.data.actions.get('slide')
assert clip is not None, 'missing exact slide action'
arm.animation_data.action=clip
arm.animation_data.action_slot=clip.slots[0]
for track in arm.animation_data.nla_tracks:
    track.mute=True
meshes=[o for o in scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)]
assert meshes
samples=[]
for t in [0,.06,.14,.25,.342,.46,.61,.78,.95]:
    scene.frame_set(int(t*60),subframe=t*60-int(t*60))
    deps=bpy.context.evaluated_depsgraph_get()
    verts=[]
    for o in meshes:
        ev=o.evaluated_get(deps)
        mesh=ev.to_mesh()
        verts.extend(ev.matrix_world @ v.co for v in mesh.vertices)
        ev.to_mesh_clear()
    samples.append({'time':t,'floor':round(min(v.z for v in verts),5),
                    'top':round(max(v.z for v in verts),5),
                    'forward_extent':round(-min(v.y for v in verts),5)})
assert min(s['floor'] for s in samples)>-.005, samples
assert samples[-1]['top']-min(s['top'] for s in samples)>.1, 'insufficient body lowering'
assert abs(clip.frame_range[1]/60-.95)<.001
print('IMPORTED_SLIDE '+json.dumps({'file':path.name,'samples':samples}))
