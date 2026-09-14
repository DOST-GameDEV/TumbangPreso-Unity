"""Restore owner-selected backup art while preserving current motion and monster.

The four named backup branches contain identical chosen model blobs. Geometry is
copied from backup4; current animation, skeleton and the complete rage subtree
must remain identical. Every original is backed up under ignored Logs first.
"""
import copy
import hashlib
import json
from pathlib import Path
import subprocess

import numpy as np
from author_cast_finish import add_accessor, animation_digest, write
from build_ghost_pet_voxel import GHOST_BOXES, build_mesh
from glb_mesh_dump import COMPONENT, read_glb, read_accessor

ROOT=Path(__file__).resolve().parents[1]
REF='82524c7537fc5fcff00ebb845ee4c360acd468cb'
INDAY='Assets/TumbangPreso/Art/characters/persons/character-female-a.glb'
GHOST='Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb'


def backup(path):
    target=ROOT/'Logs/before-owner-backup-restoration'/path.relative_to(ROOT)
    target.parent.mkdir(parents=True,exist_ok=True)
    if not target.exists(): target.write_bytes(path.read_bytes())


def old_art(relative):
    data=subprocess.check_output(['git','show',REF+':'+relative],cwd=ROOT)
    path=ROOT/'Logs/backup-art-source'/Path(relative).name
    path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
    return read_glb(path),hashlib.sha256(data).hexdigest()


def copy_accessor(g,blob,old,old_blob,index):
    spec=old['accessors'][index]
    return add_accessor(g,blob,read_accessor(old,old_blob,index),spec['type'],spec['componentType'],COMPONENT[spec['componentType']][0])


def restore_inday():
    path=ROOT/INDAY;g,blob=read_glb(path)
    if g.get('extras',{}).get('ownerBackupGeometry')==REF: return {'id':'inday','state':'already restored'}
    (old,old_blob),source_hash=old_art(INDAY)
    assert old['nodes']==g['nodes'] and old['skins']==g['skins'],'Rig structure differs'
    assert old.get('materials')==g.get('materials'),'Material mapping differs'
    for a,b in zip(old['skins'],g['skins']):
        assert read_accessor(old,old_blob,a['inverseBindMatrices'])==read_accessor(g,blob,b['inverseBindMatrices'])
    before=animation_digest(g,blob);data=bytearray(blob)
    for index,old_mesh in enumerate(old['meshes']):
        mesh=copy.deepcopy(old_mesh)
        for primitive in mesh['primitives']:
            primitive['attributes']={name:copy_accessor(g,data,old,old_blob,accessor) for name,accessor in primitive['attributes'].items()}
            primitive['indices']=copy_accessor(g,data,old,old_blob,primitive['indices'])
        g['meshes'][index]=mesh
    g.setdefault('extras',{})['ownerBackupGeometry']=REF
    g['extras']['preserveOwnerBackupGeometry']=True
    g['extras'].pop('handVolume',None)
    assert animation_digest(g,data)==before
    backup(path);write(path,g,data)
    return {'id':'inday','source_sha256':source_hash,'preserved_animation_sha256':before,'animations':len(g['animations'])}


def rage_digest(g,blob):
    root=next(i for i,n in enumerate(g['nodes']) if n.get('name')=='RageForm')
    seen=set();todo=[root];hash_=hashlib.sha256()
    while todo:
        i=todo.pop()
        if i in seen: continue
        seen.add(i);n=g['nodes'][i];todo.extend(n.get('children',[]))
        hash_.update(json.dumps(n,sort_keys=True).encode())
        if 'skin' in n:
            skin=g['skins'][n['skin']];todo.extend(skin['joints'])
            hash_.update(json.dumps(skin,sort_keys=True).encode())
            hash_.update(repr(read_accessor(g,blob,skin['inverseBindMatrices'])).encode())
        if 'mesh' not in n:continue
        mesh=g['meshes'][n['mesh']];hash_.update(json.dumps(mesh,sort_keys=True).encode())
        for p in mesh['primitives']:
            for accessor in list(p['attributes'].values())+[p['indices']]:
                hash_.update(repr(read_accessor(g,blob,accessor)).encode())
            if 'material' in p:hash_.update(json.dumps(g['materials'][p['material']],sort_keys=True).encode())
    return hash_.hexdigest()


def restore_ghost():
    path=ROOT/GHOST;g,blob=read_glb(path)
    if g.get('extras',{}).get('ownerBackupCalm')==REF:return {'id':'ghost','state':'already restored'}
    (old,old_blob),source_hash=old_art(GHOST)
    p=old['meshes'][0]['primitives'][0]
    positions=read_accessor(old,old_blob,p['attributes']['POSITION'])
    normals=read_accessor(old,old_blob,p['attributes']['NORMAL'])
    uv=read_accessor(old,old_blob,p['attributes']['TEXCOORD_0'])
    indices=[v[0] for v in read_accessor(old,old_blob,p['indices'])]
    generated=build_mesh()
    for expected,actual in zip(generated,(positions,normals,uv,indices)):
        assert np.allclose(expected,actual,atol=1e-7),'Backup does not match named source box layout'
    before=rage_digest(g,blob);animation=animation_digest(g,blob);data=bytearray(blob)
    calm=next(i for i,n in enumerate(g['nodes']) if n.get('name')=='CalmForm')
    expression=next(i for i,n in enumerate(g['nodes']) if n.get('name')=='KuroExpressions')
    wrapper=len(g['nodes']);g['nodes'].append({'name':'RestoredCalm','children':[]})
    material=len(g['materials']);g['materials'].append({'name':'RestoredKuroPalette','pbrMetallicRoughness':{'baseColorFactor':[1,1,1,1],'metallicFactor':0,'roughnessFactor':1}})
    parts={};centres={}
    for part,(name,*_) in enumerate(GHOST_BOXES):
        start=part*24;points=np.asarray(positions[start:start+24]);centre=(points.min(0)+points.max(0))*.5
        attributes={
            'POSITION':add_accessor(g,data,(points-centre).tolist(),'VEC3',5126,'f'),
            'NORMAL':add_accessor(g,data,normals[start:start+24],'VEC3',5126,'f'),
            'TEXCOORD_0':add_accessor(g,data,uv[start:start+24],'VEC2',5126,'f')}
        local=[(i-start,) for i in indices[part*36:part*36+36]]
        mesh=len(g['meshes']);g['meshes'].append({'name':name,'primitives':[{'attributes':attributes,'indices':add_accessor(g,data,local,'SCALAR',5125,'I'),'material':material,'mode':4}]})
        index=len(g['nodes']);g['nodes'].append({'name':name,'mesh':mesh,'translation':centre.tolist()})
        parts[name]=index;centres[name]=centre
        g['nodes'][wrapper]['children'].append(index)
    # Blink the eye, its glint and pupil as one coherent part.
    for side in ('l','r'):
        eye=parts['ghost-eye-'+side]
        g['nodes'][eye]['children']=[]
        for kind in ('glint','pupil'):
            name='ghost-eye-'+kind+'-'+side;child=parts[name]
            g['nodes'][wrapper]['children'].remove(child)
            g['nodes'][eye]['children'].append(child)
            g['nodes'][child]['translation']=(centres[name]-centres['ghost-eye-'+side]).tolist()
    # Existing staged expression shapes fit the restored face at its own centres.
    for i in g['nodes'][expression]['children']:
        name=g['nodes'][i]['name']
        mouth='Mouth' in name
        g['nodes'][i]['translation']=[0,-.006 if mouth else -.024,.0062]
    g['nodes'][calm]['children']=[wrapper,expression]
    g.setdefault('extras',{})['ownerBackupCalm']=REF
    assert rage_digest(g,data)==before,'Monster geometry/material/rig changed'
    assert animation_digest(g,data)==animation,'Existing motion changed'
    backup(path);write(path,g,data)
    return {'id':'ghost','source_sha256':source_hash,'preserved_rage_sha256':before,'preserved_animation_sha256':animation,'original_vertices':len(positions),'restored_parts':len(GHOST_BOXES)}


if __name__=='__main__':
    assert subprocess.check_output(['git','branch','--show-current'],cwd=ROOT,text=True).strip()=='ASTRAReworks'
    report={'backup_ref':REF,'results':[restore_inday(),restore_ghost()]}
    (ROOT/'Logs/owner-backup-restoration.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))
