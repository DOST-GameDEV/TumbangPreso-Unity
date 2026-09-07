"""Check the roster's slide reference against Unity's actual imported artifact.

Run after Build Roster Book. No Unity scripts or gameplay code are generated.
This narrowly reads Unity 6000.5's version-23 artifact object table. Unexpected
layouts fail closed. It proves the serialized reference names slide and carries
rotation curves; it does not claim a rendered or networked playback test.
"""
import json
from pathlib import Path
import re
import struct
import subprocess
import sys

SLIDE_ID=-4776023958905061225
rows=[]
for roster in sorted(Path('Assets/TumbangPreso/Resources/Roster').glob('person_*.asset')):
    text=roster.read_text()
    if f'fileID: {SLIDE_ID},' not in text:
        continue
    guid=re.search(r'Model: \{fileID: -?\d+, guid: ([a-f0-9]+)',text)[1]
    candidates=[p for p in Path('Assets/TumbangPreso/Art/characters/persons').glob('*.glb.meta')
                if f'guid: {guid}' in p.read_text()]
    assert len(candidates)==1, roster
    model=candidates[0].name.removesuffix('.glb.meta')
    hits=subprocess.run(['rg','-a','-l','-F',model,'Library/Artifacts'],capture_output=True,text=True,check=True).stdout.splitlines()
    matches=[]
    for hit in hits:
        b=Path(hit).read_bytes()
        if len(b)<48 or struct.unpack_from('>I',b,8)[0]!=23:
            continue
        metadata,size,data=struct.unpack_from('>IQQ',b,20)
        if size!=len(b):
            continue
        marker=struct.pack('<q',SLIDE_ID)
        i=b.find(marker,48,data)
        if i<0:
            continue
        path_id,offset,length,type_id=struct.unpack_from('<qQII',b,i)
        start=data+offset
        assert start+length<=len(b)
        # Editor AnimationClip: object flags + three local PPtrs precede name.
        name_len=struct.unpack_from('<I',b,start+40)[0]
        name=b[start+44:start+44+name_len].decode('utf-8')
        assert name=='slide', (hit,name)
        curve_offset=start+44+((name_len+3)//4)*4+4
        rotations=struct.unpack_from('<I',b,curve_offset)[0]
        assert rotations==7,(hit,rotations)
        matches.append({'artifact':Path(hit).name,'name':name,'rotation_curves':rotations})
    assert matches, (roster,model)
    rows.append({'roster':roster.stem,'model':model,'guid':guid,'fileID':SLIDE_ID,'imported':matches})
assert rows,'No serialized slides. Run Build Roster Book first.'
source=Path('Assets/TumbangPreso/Runtime/Visual/CharacterAnimator.cs').read_text(encoding='utf-8')
assert re.search(r'\{ "slide", new\[\] \{ "slide",',source)
assert '_clips[c.name] = c;' in source and 'if (_clips.ContainsKey(name)) return name;' in source
report={'scope':'Unity imported clip and serialized body-action resolution, not motion photography',
        'rosters':rows,'count':len(rows)}
if len(sys.argv)>1:
    Path(sys.argv[1]).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
