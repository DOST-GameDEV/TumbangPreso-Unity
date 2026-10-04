import sys,json
from pathlib import Path
sys.path.insert(0,'/workspace/scratch/0ed9da4aed7f/tump/tools')
import author_kanto_models as K
from mathutils import Matrix
results=[]
for mode,selected in [('keep',False),('keep',True),('default',False)]:
 b=K.Buf('UV control');b.box(Matrix.Identity(4),(1,1,1),'bark');b.box(Matrix.Translation((2,0,0)),(1,1,1),'railing');b.bm.normal_update()
 uv=b.bm.loops.layers.uv.verify()
 for f in b.bm.faces:
  for l in f.loops:l[uv].uv=(.23,.67)
 if mode=='keep':b.uv_mode='keep'
 if selected:b.world_uv_materials=('railing',)
 before=[[tuple(l[uv].uv) for l in f.loops] for f in b.bm.faces]
 b.world_uvs()
 for f,old in zip(b.bm.faces,before):
  now=[tuple(l[uv].uv) for l in f.loops];mat=b.mats[f.material_index]
  should_map=mode=='default' or (selected and mat=='railing')
  assert (now!=old)==should_map,(mode,mat)
 results.append({'mode':mode,'selected':selected,'pass':True});b.bm.free()
Path('/workspace/scratch/0ed9da4aed7f/tump-qa/recovery-1004b/kanto-uv-builder-tests.json').write_text(json.dumps(results,indent=2))
print('KANTO_UV_BUILDER_3_PASS')
