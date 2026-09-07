"""Author the shared retrieval slide on one existing rig at a time.

Run with Blender 5.2 --background --python tools/author_retrieval_slide.py -- FILE.
The surgical GLB export preserves every original byte of mesh, skin, material,
texture and existing clip data. Only the named slide animation is appended.
No retargeting and no replacement skeleton. Import the result in Blender to edit
the native action named slide. --blend FILE also saves that authoring scene.

The rigid seven-bone rig has no knees or elbows. A split-leg hip skid, asymmetric
ground sweep and delayed bracing recovery express commitment within that limit.
All movement along the ground belongs to the motor; only pelvis height is keyed.
"""
import argparse
import copy
import json
import math
from pathlib import Path
import struct
import sys

import bpy
from mathutils import Euler, Matrix, Vector, Quaternion

sys.path.insert(0, str(Path(__file__).resolve().parent))
from glb_mesh_dump import read_glb, read_accessor
from build_person_voxel import write_glb

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
    g, original = read_glb(str(path))
    assert not any(a.get('name') == 'slide' for a in g['animations']), 'slide already exists; restore the source before reauthoring'
    before = copy.deepcopy(g)
    blob = bytearray(original)
    nodes = g['nodes']
    ids = {n.get('name'):i for i,n in enumerate(nodes)}
    assert all(n in ids for n in pose(0))
    parents = {c:i for i,n in enumerate(nodes) for c in n.get('children',[])}
    rest = {}
    def world(i, rotations=None):
        n = nodes[i]
        assert 'matrix' not in n and 'rotation' not in n and 'scale' not in n, 'Inspect nontranslation rest rig first'
        local = Matrix.Translation(Vector(n.get('translation',(0,0,0))))
        if rotations and n.get('name') in rotations:
            local = local @ rotations[n['name']].to_matrix().to_4x4()
        return (world(parents[i],rotations) @ local) if i in parents else local
    for i in range(len(nodes)):
        rest[i] = world(i)
    # Each source's real skinned vertices determine floor contact, so different
    # limb lengths never require changing the bind pose or burying a foot.
    vertices = []
    for n in nodes:
        if 'skin' not in n:
            continue
        joints = g['skins'][n['skin']]['joints']
        for prim in g['meshes'][n['mesh']]['primitives']:
            at = prim['attributes']
            positions = read_accessor(g,original,at['POSITION'])
            weights = read_accessor(g,original,at['WEIGHTS_0'])
            indices = read_accessor(g,original,at['JOINTS_0'])
            for p,ws,js in zip(positions,weights,indices):
                vertices.append([(joints[j], w, rest[joints[j]].inverted() @ Vector(p))
                                 for j,w in zip(js,ws) if w > 0])
    floor = min(sum((rest[j] @ p).y*w for j,w,p in v) for v in vertices)
    times = sorted(set([round(i/60,8) for i in range(58)] + [b[0] for b in BEATS]))
    tracks = {name:[] for name in pose(0)}
    heights=[]
    for t in times:
        rots = {n:Euler(tuple(math.radians(v) for v in angles),'XYZ').to_quaternion()
                for n,angles in pose(t).items()}
        # The source is T-pose, not arms-down. Lower the rigid arms first, then
        # swing about the shoulder's lateral axis. Reversing this multiplication
        # leaves the arms spread because pitching a horizontal arm cannot reach.
        for name,sign in [('arm-left',-1),('arm-right',1)]:
            pitch,_,spread=pose(t)[name]
            rots[name]=(Quaternion((1,0,0),math.radians(pitch)) @
                        Quaternion((0,0,1),math.radians(sign*80+spread)))
        matrices = {j:world(j,rots) for j in rest}
        low = min(sum((matrices[j] @ p).y*w for j,w,p in v) for v in vertices)
        heights.append((0.0,floor-low,0.0))
        for n,q in rots.items():
            tracks[n].append((q.x,q.y,q.z,q.w))
    def add(values,kind):
        blob.extend(b'\0'*((-len(blob))%4))
        start=len(blob)
        for value in values:
            blob.extend(struct.pack('<'+'f'*len(value),*value))
        view=len(g['bufferViews'])
        g['bufferViews'].append({'buffer':0,'byteOffset':start,'byteLength':len(blob)-start})
        idx=len(g['accessors'])
        acc={'bufferView':view,'componentType':5126,'count':len(values),'type':kind}
        if kind=='SCALAR':
            acc.update(min=[values[0][0]],max=[values[-1][0]])
        g['accessors'].append(acc)
        return idx
    clock=add([(t,) for t in times],'SCALAR')
    clip={'name':'slide','samplers':[],'channels':[]}
    for name,values,kind,prop in [(n,v,'VEC4','rotation') for n,v in tracks.items()]+[('root',heights,'VEC3','translation')]:
        output=add(values,kind)
        clip['channels'].append({'sampler':len(clip['samplers']),'target':{'node':ids[name],'path':prop}})
        clip['samplers'].append({'input':clock,'output':output,'interpolation':'LINEAR'})
    g['animations'].append(clip)
    g['buffers'][0]['byteLength']=len(blob)
    for key in ('nodes','skins','meshes','materials','textures','images'):
        assert g.get(key)==before.get(key), key
    assert g['animations'][:-1]==before['animations']
    assert blob[:len(original)]==original
    write_glb(str(path),g,blob)
    return {'file':path.name,'clip':'slide','duration':times[-1],'samples':len(times),
            'pelvis_drop':round(-min(v[1] for v in heights),5),
            'original_clips_preserved':len(before['animations']),
            'original_binary_preserved':True}


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
