"""Author Kuro as an articulated blocky spirit in Blender, with real face targets.

The previous ghost was one merged mesh, so runtime mouth/eye/tail animation found
no targets. This native source keeps those simple graphic parts individually named.
Coordinates below use the existing GLB's Y-up, +Z-front convention.
"""
from pathlib import Path
import bpy,bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb'
SOURCE=ROOT/'MapSource/characters/kuro'
SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
root=bpy.data.objects.new('GhostPetRoot',None);bpy.context.collection.objects.link(root)

def xyz(p):return (p[0],-p[2],p[1])

def material(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=.85
 return m
bodymat=material('Kuro lavender linen',(.69,.67,.84))
tailmat=material('Kuro fading lavender',(.49,.48,.66))
ink=material('Kuro face ink',(.085,.045,.13))

# Every outline is a deliberate chamfer, not a stack of voxel boxes.
def mesh(name,vertices,faces,mat,at=(0,0,0),scale=(1,1,1)):
 data=bpy.data.meshes.new(name);data.from_pydata([xyz(v) for v in vertices],[],faces);data.update()
 bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
 obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);obj.parent=root
 obj.location=xyz(at);obj.scale=(scale[0],scale[2],scale[1]);data.materials.append(mat)
 return obj

def section(name,rings,mat,at=(0,0,0)):
 verts=[]
 for y,w,d,cx,cz in rings:
  # Broad flat front/back with clipped corners; a square-bodied ghost in this cast.
  outline=[(-w*.7,d), (w*.7,d),(w,d*.6),(w,-d*.6),(w*.7,-d),(-w*.7,-d),(-w,-d*.6),(-w,d*.6)]
  verts.extend((x+cx,y,z+cz) for x,z in outline)
 faces=[tuple(range(7,-1,-1))]
 for r in range(len(rings)-1):
  for i in range(8):faces.append((r*8+i,r*8+(i+1)%8,(r+1)*8+(i+1)%8,(r+1)*8+i))
 faces.append(tuple(range((len(rings)-1)*8,len(rings)*8)))
 return mesh(name,verts,faces,mat,at)

section('ghost-body',[
 (-.042,.033,.026,0,.004),(-.030,.054,.038,0,0),(.035,.054,.038,0,0),
 (.058,.039,.026,0,0),(.067,.023,.017,0,0)],bodymat)
section('ghost-tail-upper',[(.005,.032,.025,0,0),(-.022,.022,.019,-.009,.005),(-.033,.012,.013,-.014,.009)],bodymat,at=(0,-.042,.004))
section('ghost-tail-middle',[(.005,.017,.016,0,0),(-.016,.011,.011,.005,.004),(-.026,.008,.008,.014,.006)],tailmat,at=(-.014,-.071,.011))
section('ghost-tail-tip',[(.005,.010,.010,0,0),(-.009,.007,.007,.010,.001),(-.015,.001,.001,.021,0)],tailmat,at=(0,-.094,.017))

# Face geometry is unit-sized around its origin. Runtime can open the mouth and
# slant the eyes without moving a plane away from the broad flat face.
def face(name,at,scale,cut):
 profile=[(-.5+cut,-.5),(.5-cut,-.5),(.5,-.5+cut),(.5,.5-cut),(.5-cut,.5),(-.5+cut,.5),(-.5,.5-cut),(-.5,-.5+cut)]
 v=[(x,y,z) for z in [-.5,.5] for x,y in profile]
 f=[tuple(range(7,-1,-1)),tuple(range(8,16))]
 for i in range(8):f.append((i,(i+1)%8,(i+1)%8+8,i+8))
 return mesh(name,v,f,ink,at,scale)
face('ghost-eye-l',(-.024,.024,.0388),(.014,.019,.0012),.19)
face('ghost-eye-r',(.024,.024,.0388),(.014,.019,.0012),.19)
face('ghost-mouth',(0,-.006,.039),(.023,.008,.0012),.25)

for side,sign in [('l',-1),('r',1)]:
 profile=[(0,.012),(.021,-.004),(.038,-.032),(.027,-.026),(.031,-.040),(.012,-.025),(-.004,-.011)]
 verts=[(x*sign,y,z) for z in [-.011,.011] for x,y in profile];n=len(profile)
 faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
 for i in range(n):faces.append((i,(i+1)%n,(i+1)%n+n,i+n))
 mesh('ghost-arm-'+side,verts,faces,bodymat,at=(sign*.047,-.007,0))

scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
scene['design']='Cute blocky familiar with a broad readable face, tapered connected wisps, separate mouth/eyes/tail and grasping arm wisps. Runtime transforms this same ghost into the giant raging form.'
bpy.context.view_layer.objects.active=root
for obj in bpy.context.scene.objects:obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(OUT),export_format='GLB',use_selection=True,export_animations=False,export_yup=True,export_cameras=False,export_lights=False)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.shading.color_type='MATERIAL';space.region_3d.view_distance=.4
   space.region_3d.view_location=Vector((0,0,-.015))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'kuro.blend'))
print('Kuro parts:',[(o.name,len(o.data.vertices)) for o in scene.objects if o.type=='MESH'])
