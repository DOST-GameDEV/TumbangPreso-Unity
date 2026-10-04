from pathlib import Path
import struct,json,hashlib,numpy as np
p=Path('/workspace/scratch/0ed9da4aed7f/tump/Assets/TumbangPreso/Art/Kanto/Models/street_young.glb')
q=Path('/workspace/scratch/0ed9da4aed7f/tump-qa/recovery-1004b');original=p.read_bytes();(q/'street_young-original.glb').write_bytes(original)
n=struct.unpack_from('<I',original,12)[0];j=json.loads(original[20:20+n]);offset=20+n;data=bytearray(original[offset+8:]);ranges=[]
def arr(i):
 a=j['accessors'][i];v=j['bufferViews'][a['bufferView']];width={'VEC2':2,'VEC3':3,'VEC4':4,'SCALAR':1}[a['type']];assert 'byteStride' not in v
 start=v.get('byteOffset',0)+a.get('byteOffset',0);dtype={5126:'<f4',5123:'<u2',5125:'<u4'}[a['componentType']]
 return np.frombuffer(data,dtype=dtype,count=a['count']*width,offset=start).reshape(-1,width),start
for mesh in j['meshes']:
 if mesh['name']!='trunk':continue
 for primitive in mesh['primitives']:
  mat=j['materials'][primitive['material']]['name']
  if mat not in ('railing','metal_dark'):continue
  ids=primitive['attributes'];uv,start=arr(ids['TEXCOORD_0']);pos,_=arr(ids['POSITION']);normal,_=arr(ids['NORMAL'])
  assert np.all(uv==[0,1]),'Refuse already edited or unexpected source UVs'
  for i,(v,nrm) in enumerate(zip(pos,normal)):
   if abs(nrm[1])>.7:u,w=v[0]/2,-v[2]/2
   else:
    length=np.hypot(nrm[0],nrm[2]);assert length>1e-6
    u=(v[0]*nrm[2]-v[2]*nrm[0])/length/2;w=v[1]/2
   uv[i]=[u,1-w]
  a=j['accessors'][ids['TEXCOORD_0']]
  if 'min' in a:a['min']=uv.min(0).tolist()
  if 'max' in a:a['max']=uv.max(0).tolist()
  ranges.append((mat,start,start+uv.nbytes,len(uv)))
assert len(ranges)==2
before=original[offset+8:];allowed=np.zeros(len(data),dtype=bool)
for _,a,b,_ in ranges:allowed[a:b]=True
assert all(a==b or allowed[i] for i,(a,b) in enumerate(zip(before,data)))
js=json.dumps(j,separators=(',',':')).encode();js+=b' '*((-len(js))%4)
out=struct.pack('<III',0x46546c67,2,12+8+len(js)+8+len(data))+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(data),0x004e4942)+data
p.write_bytes(out)
(q/'kanto-guard-uv-repair.json').write_text(json.dumps({'original_sha256':hashlib.sha256(original).hexdigest(),'candidate_sha256':hashlib.sha256(out).hexdigest(),'changed_uv_ranges':ranges,'non_uv_binary_bytes_unchanged':True,'method':'Planar 2m guard mapping, same as canonical Buf.world_uvs; retain all bark/leaf coordinates and all geometry.'},indent=2));print(ranges)
