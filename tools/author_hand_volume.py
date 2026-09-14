"""Repair flat hand volumes without changing faces, outfits, rigs or animations.

Dry-run by default. Each hand is selected from its actual rigid mesh components
and skin/glove palette. Existing source and animation bytes are backed up before
an applied edit. The stable stamp prevents regeneration from stacking anatomy.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess

import numpy as np
from author_cast_finish import Geometry, animation_digest, slot, store_geometry, write
from glb_mesh_dump import read_glb, read_accessor

ROOT = Path(__file__).resolve().parents[1]
VERSION = 'solid-block-hands-v1'


def groups(attributes, indices, bone):
    positions = np.asarray(attributes['POSITION'])
    joints = np.asarray(attributes['JOINTS_0'])[:, 0]
    weights = np.asarray(attributes['WEIGHTS_0'])[:, 0]
    triangles = [tuple(indices[i:i+3]) for i in range(0, len(indices), 3)
                 if all(joints[j] == bone and weights[j] > .99 for j in indices[i:i+3])]
    parent = {}
    def key(i): return tuple(np.round(positions[i], 6))
    def find(k):
        parent.setdefault(k, k)
        if parent[k] != k: parent[k] = find(parent[k])
        return parent[k]
    for triangle in triangles:
        root = find(key(triangle[0]))
        for i in triangle[1:]: parent[find(key(i))] = root
    parts = {}
    for triangle in triangles:
        parts.setdefault(find(key(triangle[0])), []).append(triangle)
    result = []
    for triangles in parts.values():
        used = sorted({i for triangle in triangles for i in triangle})
        points = positions[used]
        colours = Counter(slot(attributes['TEXCOORD_0'][i]) for i in used)
        result.append(dict(triangles=triangles, used=used, lo=points.min(0), hi=points.max(0), colours=colours))
    return result


def author(character, path, apply):
    g, blob = read_glb(path)
    if g.get('extras', {}).get('preserveOwnerBackupGeometry'):
        return dict(id=character,state='owner-restored backup geometry preserved')
    if g.get('extras', {}).get('handVolume') == VERSION:
        return dict(id=character, state='already authored')
    node = next(n for n in g['nodes'] if 'skin' in n and 'body' in g['meshes'][n['mesh']].get('name', '').lower())
    primitive = g['meshes'][node['mesh']]['primitives'][0]
    attributes = {name: list(read_accessor(g, blob, accessor)) for name, accessor in primitive['attributes'].items()}
    indices = [value[0] for value in read_accessor(g, blob, primitive['indices'])]
    names = [g['nodes'][index]['name'] for index in g['skins'][node['skin']]['joints']]
    report = dict(id=character, state='inspected', hands=[])
    removed = set(); additions = []
    for name, sign in [('arm-left', 1), ('arm-right', -1)]:
        bone = names.index(name)
        parts = groups(attributes, indices, bone)
        def along(part): return max(sign * part['lo'][0], sign * part['hi'][0])
        skin = [p for p in parts if sum(count for colour, count in p['colours'].items() if colour in (13,14,15)) >= len(p['used']) * .8]
        # Sean's exposed block hand uses his authored material slot0; a thin
        # skin-colored inset farther up his gauntlet must not be mistaken for it.
        candidates = skin if skin and max(along(p) for p in skin) >= max(along(p) for p in parts)-.03 else parts
        far = max(along(p) for p in candidates)
        # A hand's substantial block, not a tiny detached cap, defines its volume.
        distal = [p for p in candidates if along(p) >= far - .02]
        hand = max(distal, key=lambda p: float(np.prod(p['hi'] - p['lo'])))
        lo, hi = hand['lo'].copy(), hand['hi'].copy()
        size = hi - lo; centre = (lo + hi) * .5
        tone = hand['colours'].most_common(1)[0][0]
        changes = size[2] < size[1] * .78
        desired = size.copy(); desired[2] = max(size[2], size[1] * .84)
        if character == 'inday':
            # The old hand props were several intersecting plate/disc volumes,
            # including a plum slab beyond the palm. Keep her yellow sleeve,
            # dark hand and coral/plum wrist identity as connected simple blocks.
            sleeve=min((p for p in parts if p['colours'].most_common(1)[0][0]==7),key=along)
            sleeve_centre=(sleeve['lo']+sleeve['hi'])*.5
            centre[2]=sleeve_centre[2]
            desired[2]=size[1]*.95
            changes=True
            for part in parts:
                if part is not sleeve: removed.update(part['triangles'])
            wrist_x=min(sign*lo[0],sign*hi[0])+.009
            cuff_size=np.array([.036,size[1]*1.12,desired[2]*1.12])
            additions.append((np.array([sign*wrist_x,centre[1],centre[2]]),cuff_size,bone,5))
            additions.append((np.array([sign*(wrist_x-.01),centre[1],centre[2]]),cuff_size*np.array([.32,1.025,1.025]),bone,11))
        elif changes:
            # Keep an existing narrow wristband seated around the deeper hand.
            near=min(sign*lo[0],sign*hi[0])
            for part in parts:
                if part is hand: continue
                part_size=part['hi']-part['lo'];part_centre=(part['hi']+part['lo'])*.5
                if part_size[0]<.045 and near<=part_centre[0]*sign<=near+.05 and part_size[2]<desired[2]+.008:
                    ratio=(desired[2]+.008)/max(.001,part_size[2])
                    for vertex in part['used']:
                        p=np.array(attributes['POSITION'][vertex]);p[2]=part_centre[2]+(p[2]-part_centre[2])*ratio
                        n=np.array(attributes['NORMAL'][vertex]);n[2]/=ratio;n/=max(.000001,np.linalg.norm(n))
                        attributes['POSITION'][vertex]=tuple(p);attributes['NORMAL'][vertex]=tuple(n)
        report['hands'].append(dict(bone=name, bounds=[lo.tolist(),hi.tolist()], colour=int(tone),
            parts=[dict(size=(p['hi']-p['lo']).tolist(),lo=p['lo'].tolist(),hi=p['hi'].tolist(),colours=dict(p['colours'])) for p in parts],
            old_depth_ratio=float(size[2]/max(.00001,size[1])),new_size=desired.tolist(),change=bool(changes)))
        if changes:
            removed.update(hand['triangles'])
            additions.append((centre, desired, bone, tone))
    if not additions: return report
    report['state'] = 'candidate' if not apply else 'authored'
    if not apply: return report
    before = animation_digest(g, blob)
    backup = ROOT/'Logs/hand-volume-source-backup'/path.relative_to(ROOT)
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists(): shutil.copy2(path, backup)
    original_hash = hashlib.sha256(path.read_bytes()).hexdigest()
    triangles = [tuple(indices[i:i+3]) for i in range(0,len(indices),3)]
    kept = [i for triangle in triangles if triangle not in removed for i in triangle]
    geometry = Geometry(attributes, kept)
    for centre,size,bone,tone in additions:
        geometry.bevel(centre,size,bone,tone,min(size)*.035)
    used = sorted(set(geometry.indices)); remap = {old:new for new,old in enumerate(used)}
    compact = {key:[values[i] for i in used] for key,values in geometry.data.items()}
    data = bytearray(blob)
    store_geometry(g,data,primitive,compact,[remap[i] for i in geometry.indices])
    g.setdefault('extras',{})['handVolume']=VERSION
    assert animation_digest(g,data)==before, 'Animation changed'
    write(path,g,data)
    report.update(original_sha256=original_hash,animation_sha256=before,removed_triangles=len(removed))
    return report


if __name__ == '__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');parser.add_argument('--characters',default='zack,inday')
    args=parser.parse_args()
    assert subprocess.check_output(['git','branch','--show-current'],cwd=ROOT,text=True).strip()=='ASTRAReworks'
    source=(ROOT/'Assets/TumbangPreso/Editor/RosterBookBuilder.cs').read_text(encoding='utf-8')
    table=source.split('PersonModels =',1)[1].split('};',1)[0]
    entries=dict(re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]+\.glb)"',table))
    wanted=args.characters.split(',') if args.characters!='all' else [key for key in entries if not key.startswith('custom')]
    results=[]
    for character in wanted:
        path=(ROOT/'Assets/TumbangPreso/Art'/entries[character]).resolve()
        assert path.is_relative_to(ROOT.resolve())
        row=author(character,path,args.apply);results.append(row)
        print(character,row['state'],[(h['bone'],round(h['old_depth_ratio'],3)) for h in row.get('hands',[])])
    target=ROOT/'Logs'/('hand-volume-applied.json' if args.apply else 'hand-volume-inspection.json')
    target.write_text(json.dumps(results,indent=2)+'\n')
