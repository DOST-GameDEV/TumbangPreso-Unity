"""Give each skinned primitive its own glTF mesh for the installed importer.

The current glTFast schedules a whole bone-buffer normalization job before reading
another primitive into that buffer. Separate child meshes avoid that job conflict
without changing vertex data, materials, joint transforms or animation channels.
"""
import json,struct
from pathlib import Path

def normalize(path):
 path=Path(path);raw=path.read_bytes();length=struct.unpack_from('<I',raw,12)[0]
 data=json.loads(raw[20:20+length]);cursor=20+length
 binary=b''
 if cursor<len(raw):
  size,kind=struct.unpack_from('<II',raw,cursor)
  assert kind==0x004e4942
  binary=raw[cursor+8:cursor+8+size]
 splits=0
 for node in list(data.get('nodes',[])):
  if 'mesh' not in node or 'skin' not in node:continue
  source=data['meshes'][node['mesh']]
  if len(source['primitives'])<=1:continue
  skin=node.pop('skin');node.pop('mesh');children=node.setdefault('children',[])
  for i,primitive in enumerate(source['primitives']):
   mesh={'name':source.get('name','Skin')+'_part'+str(i),'primitives':[primitive]}
   for key in ['weights','extras']:
    if key in source:mesh[key]=source[key]
   mesh_id=len(data['meshes']);data['meshes'].append(mesh)
   children.append(len(data['nodes']))
   data['nodes'].append({'name':node.get('name','Skin')+'_part'+str(i),'mesh':mesh_id,'skin':skin})
  splits+=1
 used=sorted({node['mesh'] for node in data['nodes'] if 'mesh' in node})
 mapping={old:new for new,old in enumerate(used)}
 data['meshes']=[data['meshes'][old] for old in used]
 for node in data['nodes']:
  if 'mesh' in node:node['mesh']=mapping[node['mesh']]
 encoded=json.dumps(data,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
 binary+=b'\0'*((-len(binary))%4)
 total=12+8+len(encoded)+(8+len(binary) if binary else 0)
 result=struct.pack('<III',0x46546c67,2,total)+struct.pack('<II',len(encoded),0x4e4f534a)+encoded
 if binary:result+=struct.pack('<II',len(binary),0x004e4942)+binary
 path.write_bytes(result);return splits

if __name__=='__main__':
 import sys
 print('Skinned meshes separated:',normalize(sys.argv[1]))
