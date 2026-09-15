"""Replace only Nemu's original stepped finger pieces with two rigid block palms.

Dry run by default. Preserve costume geometry, skeleton, materials and animation
samples. This never touches the familiar or the current monster model.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

import numpy as np
from author_hand_volume import groups
from author_cast_finish import Geometry, animation_digest, store_geometry, write
from glb_mesh_dump import read_glb, read_accessor

ROOT=Path(__file__).resolve().parents[1]
TARGET=ROOT/'Assets/TumbangPreso/Art/characters/persons/team-nemu.glb'
STAMP='nemu-block-palms-v1'


def semantic(attributes,triangles):
    rows=[]
    for triangle in triangles:
        rows.append(tuple(tuple(tuple(attributes[key][i]) for key in sorted(attributes)) for i in triangle))
    return hashlib.sha256(repr(rows).encode('utf-8')).hexdigest()


def run(apply):
    g,blob=read_glb(TARGET)
    node=next(n for n in g['nodes'] if 'skin' in n and 'body' in g['meshes'][n['mesh']].get('name','').lower())
    primitive=g['meshes'][node['mesh']]['primitives'][0]
    attrs={k:list(read_accessor(g,blob,a)) for k,a in primitive['attributes'].items()}
    indices=[v[0] for v in read_accessor(g,blob,primitive['indices'])]
    bones=[g['nodes'][i]['name'] for i in g['skins'][node['skin']]['joints']]
    removed=set();palms=[];details=[];already=g.get('extras',{}).get('blockPalms')==STAMP
    for bone_name in ['arm-left','arm-right']:
        bone=bones.index(bone_name)
        skin=[p for p in groups(attrs,indices,bone) if set(p['colours']).issubset({13,14})]
        assert len(skin)==(1 if already else 3),(bone_name,'unexpected skin components',len(skin))
        lo=np.min([p['lo'] for p in skin],axis=0);hi=np.max([p['hi'] for p in skin],axis=0)
        assert lo[1]>.15 and hi[1]<.22 and max(abs(lo[0]),abs(hi[0]))<.30,'Hand selection escaped the known cuff'
        details.append(dict(bone=bone_name,components=len(skin),bounds=[lo.tolist(),hi.tolist()]))
        for p in skin:removed.update(p['triangles'])
        palms.append(((lo+hi)*.5,hi-lo,bone))
    if already:return dict(state='already repaired',hands=details,sha256=hashlib.sha256(TARGET.read_bytes()).hexdigest())
    triangles=[tuple(indices[i:i+3]) for i in range(0,len(indices),3)]
    kept=[t for t in triangles if t not in removed]
    before=semantic(attrs,kept);animation=animation_digest(g,blob)
    result=dict(state='candidate',hands=details,removed_triangles=len(removed),animation_sha256=animation,unaffected_sha256=before)
    if not apply:return result
    backup=ROOT/'Logs/nemu-block-hands/original-team-nemu.glb';backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists():shutil.copy2(TARGET,backup)
    result['original_sha256']=hashlib.sha256(TARGET.read_bytes()).hexdigest()
    geometry=Geometry(attrs,[i for t in kept for i in t])
    for center,size,bone in palms:geometry.bevel(center,size,bone,13,min(size)*.035)
    used=sorted(set(geometry.indices));remap={old:new for new,old in enumerate(used)}
    compact={k:[values[i] for i in used] for k,values in geometry.data.items()}
    new_indices=[remap[i] for i in geometry.indices]
    unchanged=[tuple(remap[i] for i in t) for t in kept]
    assert semantic(compact,unchanged)==before,'Non-hand geometry changed'
    target_blob=bytearray(blob);store_geometry(g,target_blob,primitive,compact,new_indices)
    roundtrip={k:list(read_accessor(g,target_blob,a)) for k,a in primitive['attributes'].items()}
    assert semantic(roundtrip,unchanged)==before,'Serialized non-hand geometry changed'
    assert animation_digest(g,target_blob)==animation,'Proposed animation samples changed'
    g.setdefault('extras',{})['blockPalms']=STAMP;write(TARGET,g,target_blob)
    after_g,after_blob=read_glb(TARGET)
    assert animation_digest(after_g,after_blob)==animation,'Animation samples changed'
    result.update(state='repaired',sha256=hashlib.sha256(TARGET.read_bytes()).hexdigest())
    return result


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');args=parser.parse_args()
    result=run(args.apply);folder=ROOT/'Logs/nemu-block-hands';folder.mkdir(parents=True,exist_ok=True)
    (folder/(result['state'].replace(' ','-')+'.json')).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result,indent=2))
