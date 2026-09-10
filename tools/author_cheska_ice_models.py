"""Author Cheska's three fractured wall slabs and a small thaw shard in Blender.
Editable native source; render and collision use the same exported convex surface.
"""
from pathlib import Path
import bpy, bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/TumbangPreso/Resources/Models/CheskaIce'
SOURCE=ROOT/'MapSource/abilities/cheska'
OUT.mkdir(parents=True,exist_ok=True);SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

# Left shoulder, tall central cleft and right shoulder have individually drawn
# crowns. The face is broad enough to be a wall, never a row of floating gems.
profiles=[
 ('wall_left',[(-.425,0),(.425,0),(.425,1.87),(.19,2.22),(-.12,2.12),(-.425,1.81)],.55),
 ('wall_center',[(-.425,0),(.425,0),(.425,2.13),(.12,2.61),(-.18,2.45),(-.425,2.18)],.55),
 ('wall_right',[(-.425,0),(.425,0),(.425,1.83),(.20,2.10),(-.13,2.24),(-.425,1.94)],.55),
 ('thaw_shard',[(-.45,0),(.45,0),(.32,.66),(-.16,1),(-.40,.38)],.32),
]
for index,(name,profile,depth) in enumerate(profiles):
 n=len(profile)
 # Blender Z up, Y depth. A bevelled central ridge catches light on a broad face.
 verts=[(x,-depth*.5,z) for x,z in profile]+[(x,depth*.5,z) for x,z in profile]
 faces=[]
 faces.append(tuple(range(n-1,-1,-1)));faces.append(tuple(range(n,n*2)))
 for i in range(n):
  j=(i+1)%n;faces.append((i,j,n+j,n+i))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
 bpy.context.view_layer.objects.active=obj;obj.select_set(True)
 # A very small edge bevel describes ice thickness without diamond toppers.
 bevel=obj.modifiers.new('Fracture bevel','BEVEL');bevel.width=.045 if index<3 else .018;bevel.segments=1
 bpy.ops.object.modifier_apply(modifier=bevel.name)
 bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
 mat=bpy.data.materials.new(name+' ice');mat.diffuse_color=(.26,.64,.74,1);obj.data.materials.append(mat)
 bpy.ops.wm.obj_export(filepath=str(OUT/(name+'.obj')),export_selected_objects=True,export_materials=False,forward_axis='NEGATIVE_Z',up_axis='Y')
 obj.location.x=(-.75,0,.75,2)[index]
 obj.select_set(False)
 print(name,len(obj.data.vertices),'vertices',len(obj.data.polygons),'faces')
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
scene['design']='Three grounded fractured slabs. Their convex mesh is also the blocking surface. Narrow bevels read as thickness; no detached toppers.'
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.shading.color_type='MATERIAL'
   area.spaces.active.region_3d.view_distance=5
   area.spaces.active.region_3d.view_location=Vector((0,0,1.2))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ice_barricade.blend'))
