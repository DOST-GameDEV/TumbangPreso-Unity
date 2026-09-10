"""Author Cheska's low permafrost geometry in Blender, preserving a unit-radius footprint.

The frozen film, uneven fracture lines and frosted edge are three small meshes.
They replace the raised platform/spikes composition. The native .blend is retained;
Unity supplies ground conformance, material, formation timing and gameplay radius.
"""
import bpy,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/TumbangPreso/Resources/Models/Permafrost'
SOURCE=ROOT/'MapSource/abilities/cheska'
OUT.mkdir(parents=True,exist_ok=True);SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
# Deliberately uneven nuclei, with tighter small fractures around the rim.
seeds=[(-.13,.08),(.23,-.12),(-.40,-.27),(.02,.43),(.44,.30),(-.48,.33),
       (.12,-.51),(-.28,-.58),(.55,-.34),(-.64,-.03),(.69,.04),(-.14,.70),
       (.33,.68),(-.63,.58),(.66,.56),(-.73,-.43),(.53,-.69),(-.04,-.83),
       (.88,-.28),(-.87,.19),(.10,.89),(-.43,.82),(.81,.39),(-.48,-.80)]
count=64
boundary=[]
for i in range(count):
 a=math.tau*i/count
 radius=.987+.008*math.sin(i*2.31)+.005*math.sin(i*5.13)
 boundary.append((radius*math.cos(a),radius*math.sin(a)))

def clip(poly,n,c):
 out=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  da=a[0]*n[0]+a[1]*n[1]-c;db=b[0]*n[0]+b[1]*n[1]-c
  if da<=1e-9:out.append(a)
  if (da<0)!=(db<0):
   t=da/(da-db);out.append((a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t))
 return out

skin_v=[];skin_f=[];vein_v=[];vein_f=[];edge_v=[];edge_f=[]
def polygon(vertices,faces,poly):
 start=len(vertices);vertices.extend((x,y,0) for x,y in poly)
 for i in range(1,len(poly)-1):faces.append((start,start+i,start+i+1))
def stroke(vertices,faces,a,b,width):
 dx=b[0]-a[0];dy=b[1]-a[1];length=math.hypot(dx,dy)
 if length<.012:return
 nx=-dy/length*width*.5;ny=dx/length*width*.5
 polygon(vertices,faces,[(a[0]-nx,a[1]-ny),(b[0]-nx,b[1]-ny),
                         (b[0]+nx,b[1]+ny),(a[0]+nx,a[1]+ny)])
seen=set()
for j,p in enumerate(seeds):
 poly=boundary[:]
 for q in seeds:
  if q==p:continue
  n=(q[0]-p[0],q[1]-p[1]);c=(q[0]**2+q[1]**2-p[0]**2-p[1]**2)*.5
  poly=clip(poly,n,c)
  if len(poly)<3:break
 if len(poly)<3:continue
 center=(sum(x for x,y in poly)/len(poly),sum(y for x,y in poly)/len(poly))
 inset=[(center[0]+(x-center[0])*.985,center[1]+(y-center[1])*.985) for x,y in poly]
 polygon(skin_v,skin_f,inset)
 for k,(a,b) in enumerate(zip(poly,poly[1:]+poly[:1])):
  key=tuple(sorted((tuple(round(v,4) for v in a),tuple(round(v,4) for v in b))))
  if key in seen:continue
  seen.add(key)
  # Leave quieter sectors so the interior reads as a slick, not tiled paving.
  if (j*7+k*3)%7==0:continue
  stroke(vein_v,vein_f,a,b,.0038 if (j+k)%3 else .0065)
  if (j+k)%5==0:
   mid=((a[0]+b[0])*.5,(a[1]+b[1])*.5)
   branch=(mid[0]+(center[0]-mid[0])*.48,mid[1]+(center[1]-mid[1])*.48)
   stroke(vein_v,vein_f,mid,branch,.0025)
# A continuous frosted boundary stays visible from the first active frame.
for i,(a,b) in enumerate(zip(boundary,boundary[1:]+boundary[:1])):
 width=.011+.009*(.5+.5*math.sin(i*1.93))
 stroke(edge_v,edge_f,a,b,width)
 if i%3==0:
  n=math.hypot(*a);inner=(a[0]*(1-.045/n),a[1]*(1-.045/n))
  stroke(edge_v,edge_f,a,inner,.005)

objects=[]
for i,(name,vertices,faces,color) in enumerate([
 ('permafrost_skin',skin_v,skin_f,(.38,.72,.80,1)),
 ('permafrost_veins',vein_v,vein_f,(.77,.92,.95,1)),
 ('permafrost_edge',edge_v,edge_f,(.69,.86,.90,1))]):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
 uv=mesh.uv_layers.new(name='SurfaceCoordinates')
 for face in mesh.polygons:
  for loop in face.loop_indices:
   v=mesh.vertices[mesh.loops[loop].vertex_index].co
   uv.data[loop].uv=(.5+.5*v.x,.5+.5*v.y)
 obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
 mat=bpy.data.materials.new(name);mat.diffuse_color=color;obj.data.materials.append(mat)
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 bpy.ops.wm.obj_export(filepath=str(OUT/(name+'.obj')),export_selected_objects=True,
                       export_materials=False,forward_axis='NEGATIVE_Z',up_axis='Y')
 obj.location.z=i*.002
 objects.append(obj)
 print(name,len(vertices),'vertices',len(faces),'triangles')
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.shading.color_type='MATERIAL';space.overlay.show_floor=False
   space.region_3d.view_distance=3.5;space.region_3d.view_location=Vector((0,0,0))
scene['design']='Low frozen film with irregular fractures and a legible frosted perimeter. Gameplay footprint remains a radius, with no barrier collision.'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'permafrost.blend'))
