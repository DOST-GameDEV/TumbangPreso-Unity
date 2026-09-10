"""Author Kuro as an articulated blocky spirit in Blender, with real face targets.

The previous ghost was one merged mesh, so runtime mouth/eye/tail animation found
no targets. This native source keeps those simple graphic parts individually named.
Coordinates below use the existing GLB's Y-up, +Z-front convention.
"""
from pathlib import Path
import bpy,bmesh,sys,argparse,math
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--study-out',type=Path)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
if args.study_out:args.study_out=(ROOT/args.study_out).resolve()
OUT=(args.study_out/'pet-nemu-ghost.glb') if args.study_out else ROOT/'Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb'
SOURCE=args.study_out or ROOT/'MapSource/characters/kuro'
OUT.parent.mkdir(parents=True,exist_ok=True)
SOURCE.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
root=bpy.data.objects.new('GhostPetRoot',None);bpy.context.collection.objects.link(root)

def xyz(p):return (p[0],-p[2],p[1])

def material(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=.85
 return m
bodymat=material('Kuro friendly violet',(.16,.055,.28))
tailmat=material('Kuro lavender edge',(.27,.105,.43))
ink=material('Kuro friendly spirit eyes',(.80,.57,1.0))

# Each surface is authored as a connected volume. Soft ghost forms remain
# simple enough to read alongside the blocky people, without the former cube head.
def mesh(name,vertices,faces,mat,at=(0,0,0),scale=(1,1,1),rage=None,smooth=True):
 data=bpy.data.meshes.new(name);data.from_pydata([xyz(v) for v in vertices],[],faces);data.update()
 bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
 obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);obj.parent=root
 obj.location=xyz(at);obj.scale=(scale[0],scale[2],scale[1]);data.materials.append(mat)
 for poly in data.polygons:poly.use_smooth=smooth
 if rage:
  obj.shape_key_add(name='Basis');key=obj.shape_key_add(name='Rage')
  for point,target in zip(key.data,rage):point.co=xyz(target)
  key.value=0
 return obj

def ring_vertices(rings):
 vertices=[]
 for y,w,d,cx,cz in rings:
  for i in range(16):
   angle=2*math.pi*i/16
   x=math.copysign(abs(math.sin(angle))**.45,math.sin(angle));z=math.copysign(abs(math.cos(angle))**.45,math.cos(angle))
   # A broad face and chamfered corners share the giant's block language.
   if z>.70:z=1
   vertices.append((x*w+cx,y,z*d+cz))
 return vertices

def section(name,rings,mat,at=(0,0,0),rage=None):
 n=16;faces=[tuple(range(n-1,-1,-1))]
 for r in range(len(rings)-1):
  for i in range(n):faces.append((r*n+i,r*n+(i+1)%n,(r+1)*n+(i+1)%n,(r+1)*n+i))
 faces.append(tuple(range((len(rings)-1)*n,len(rings)*n)))
 return mesh(name,ring_vertices(rings),faces,mat,at,rage=ring_vertices(rage) if rage else None)

body=[(-.042,.022,.021,-.003,0),(-.031,.039,.029,-.002,0),(-.012,.051,.035,0,0),
 (.010,.052,.037,0,0),(.030,.052,.037,0,0),(.048,.051,.036,0,0),
 (.058,.041,.028,0,0),(.060,.039,.027,0,0)]
ragebody=[(y,w*(1.12 if y<.015 else 1.02),d,cx,cz) for y,w,d,cx,cz in body]
# One skinned surface connects the belly and tail. Three small tail bones
# retain the existing named motion targets without visible stacked-piece seams.
body=body[1:][::-1];ragebody=ragebody[1:][::-1]
vertices=ring_vertices(body);target=ring_vertices(ragebody)
tailpath=[(-.003,-.037,0,.031,.026),(-.002,-.045,.001,.024,.021),(.004,-.055,.002,.019,.016),
 (.016,-.065,.004,.013,.012),(.030,-.074,.005,.009,.008),(.043,-.076,.006,.006,.005),
 (.052,-.071,.006,.003,.0025),(.055,-.064,.006,.0005,.0005)]
for i,(x,y,z,w,d) in enumerate(tailpath):
 previous=Vector(tailpath[max(0,i-1)][:3]) if i else Vector((-.002,-.031,0))
 following=Vector(tailpath[min(len(tailpath)-1,i+1)][:3]);tangent=(following-previous).normalized()
 side=Vector((0,0,1)).cross(tangent).normalized();front=tangent.cross(side).normalized()
 for j in range(16):
  angle=j*2*math.pi/16;point=Vector((x,y,z))+side*(math.sin(angle)*w)+front*(math.cos(angle)*d)
  vertices.append(tuple(point));target.append(tuple(point))
faces=[tuple(range(15,-1,-1))]
for ring in range(len(body)+len(tailpath)-1):
 for i in range(16):faces.append((ring*16+i,ring*16+(i+1)%16,(ring+1)*16+(i+1)%16,(ring+1)*16+i))
faces.append(tuple(range(len(vertices)-16,len(vertices))))
bodyobj=mesh('ghost-body',vertices,faces,bodymat,rage=target,smooth=False)
rigdata=bpy.data.armatures.new('Kuro spirit flow');rig=bpy.data.objects.new('SpiritRig',rigdata);bpy.context.collection.objects.link(rig);rig.parent=root
bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
core=rigdata.edit_bones.new('SpiritCore');core.head=xyz((0,0,0));core.tail=xyz((0,.04,0))
origins=[(-.003,-.037,0),(.008,-.058,.003),(.033,-.074,.005),(.055,-.064,.006)]
previous=core
names=['ghost-tail-upper','ghost-tail-middle','ghost-tail-tip']
for i,name in enumerate(names):
 bone=rigdata.edit_bones.new(name);bone.head=xyz(origins[i]);bone.tail=xyz(origins[i+1]);bone.parent=previous;previous=bone
bpy.ops.object.mode_set(mode='OBJECT')
groups=[bodyobj.vertex_groups.new(name=n) for n in ['SpiritCore']+names]
for i in range(len(body)*16):groups[0].add([i],1,'REPLACE')
for ring in range(len(tailpath)):
 t=max(0,min(3,ring/2.0));low=int(t);high=min(3,low+1);fraction=t-low
 for index in range((len(body)+ring)*16,(len(body)+ring+1)*16):
  groups[low].add([index],1-fraction,'REPLACE')
  if fraction:groups[high].add([index],fraction,'REPLACE')
bodyobj.parent=rig
modifier=bodyobj.modifiers.new('Continuous spirit tail','ARMATURE');modifier.object=rig


def face(name,profile,at,scale,rage=None):
 if rage:
  # A fan across only the outer rim cuts through a convex face. Conform a
  # subdivided graphic mouth to the actual body surface at the final pose.
  def front_surface(x,y):
   samples=sorted([(r[0],r[1],r[2]) for r in ragebody]+[(-.037,.031,.026)])
   w,d=samples[0][1:]
   for a,b in zip(samples,samples[1:]):
    if a[0]<=y<=b[0]:
     t=(y-a[0])/(b[0]-a[0]);w=a[1]*(1-t)+b[1]*t;d=a[2]*(1-t)+b[2]*t;break
   ratio=min(1,abs(x)/w);contour=[(0,1),(.382683,1),(.707107,1),(.923880,.382683),(1,0)]
   for a,b in zip(contour,contour[1:]):
    if a[0]<=ratio<=b[0]:return d*(a[1]+(b[1]-a[1])*(ratio-a[0])/(b[0]-a[0]))
   return d
  vertices=[(0,-.12,.5)];target=[(0,0,(front_surface(at[0],at[1])+.0025-at[2])/(scale[2]*2.2))]
  n=len(profile);steps=5
  for ring in range(1,steps+1):
   t=ring/steps
   for (x,y),(rx,ry) in zip(profile,rage):
    vertices.append((x*t,-.12+(y+.12)*t,.5))
    gx=rx*t;gy=ry*t
    surface=front_surface(at[0]+gx*scale[0]*3.4,at[1]+gy*scale[1]*5.8)
    target.append((gx,gy,(surface+.0025-at[2])/(scale[2]*2.2)))
  faces=[(0,1+i,1+(i+1)%n) for i in range(n)]
  for ring in range(steps-1):
   a=1+ring*n;b=a+n
   for i in range(n):faces.append((a+i,a+(i+1)%n,b+(i+1)%n,b+i))
  return mesh(name,vertices,[tuple(reversed(f)) for f in faces],ink,at,scale,target,False)
 n=len(profile);vertices=[(x,y,z) for z in [-.5,.5] for x,y in profile]
 faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
 for i in range(n):faces.append((i,(i+1)%n,(i+1)%n+n,i+n))
 target=[(x,y,z-max(0,abs(x)-.25)*12-max(0,-y-.2)*12) for z in [-.5,.5] for x,y in rage] if rage else None
 return mesh(name,vertices,faces,ink,at,scale,target,False)

oval=[(math.cos(a*2*math.pi/16)*.5,math.sin(a*2*math.pi/16)*.5) for a in range(16)]
face('ghost-eye-l',oval,(-.019,.029,.0368),(.0105,.017,.0012))
face('ghost-eye-r',oval,(.019,.029,.0368),(.0105,.017,.0012))
smile=[(-.5,.32),(-.28,.12),(0,.07),(.28,.15),(.5,.38),(.46,-.10),(.30,-.40),(0,-.5),(-.30,-.40),(-.46,-.10)]
maw=[(-.5,.30),(-.28,.47),(0,.50),(.28,.47),(.5,.30),(.50,-.20),(.30,-.46),(0,-.50),(-.30,-.46),(-.50,-.20)]
face('ghost-mouth',smile,(0,-.008,.0378),(.024,.0095,.0012),maw)

# Graphic expressions are separately authored silhouettes, not distorted ovals.
# Their parent starts collapsed, including in an unbound roster preview. Runtime
# selects one set; ordinary Transform clips can reproduce the exact trailer pose.
expressions=bpy.data.objects.new('KuroExpressions',None);bpy.context.collection.objects.link(expressions)
expressions.parent=root;expressions.scale=(0,0,0)
def expression_shape(name,points,at,scale):
 obj=face(name,points,at,scale)
 # Bake all placement into the vertices so visibility uses unit/zero scale.
 origin=Vector(xyz(at))
 for v in obj.data.vertices:v.co=origin+Vector((v.co.x*scale[0],v.co.y*scale[2],v.co.z*scale[1]))
 obj.location=(0,0,0);obj.scale=(1,1,1);obj.parent=expressions
 return obj
def stroke(points,width):
 path=[Vector(p) for p in points];a=[];b=[]
 for i,p in enumerate(path):
  tangent=(path[min(i+1,len(path)-1)]-path[max(0,i-1)]).normalized();normal=Vector((-tangent.y,tangent.x))*width*.5
  a.append(tuple(p+normal));b.append(tuple(p-normal))
 return a+list(reversed(b))
cat=stroke([(-.5,.13),(-.43,-.07),(-.28,-.19),(-.12,-.14),(0,.06),(.12,-.14),(.28,-.19),(.43,-.07),(.5,.13)],.11)
expression_shape('KuroCatMouth',cat,(0,-.008,.039),(.030,.028,.0012))
cross=[(-.5,-.32),(-.32,-.5),(0,-.18),(.32,-.5),(.5,-.32),(.18,0),(.5,.32),(.32,.5),(0,.18),(-.32,.5),(-.5,.32),(-.18,0)]
expression_shape('KuroCrossLeft',cross,(-.019,.029,.039),(.015,.016,.0012))
expression_shape('KuroCrossRight',cross,(.019,.029,.039),(.015,.016,.0012))
expression_shape('KuroGoofyMouth',oval,(0,-.008,.039),(.009,.012,.0012))
expression_shape('KuroShyEye',stroke([(.35,.40),(-.3,0),(.35,-.40)],.20),(.019,.029,.039),(.013,.013,.0012))
expression_shape('KuroPoutMouth',stroke([(-.4,-.12),(-.05,.1),(.32,.02)],.20),(0,-.008,.039),(.020,.015,.0012))

# One small curled crown echoes the giant's silhouette without adding anatomy.
section('ghost-crown-curl',[(.053,.013,.010,-.023,.002),(.064,.010,.008,-.027,.002),(.071,.007,.006,-.037,.002),(.066,.001,.002,-.047,.002)],tailmat)

# Small arms curl upward like a mischievous wave. The same mesh extends into
# curved reaching spirit arms, not flat triangular wings, during the ultimate.
def sweep(path,sign):
 verts=[]
 for i,(x,y,z,r) in enumerate(path):
  point=Vector((x,y,z));previous=Vector(path[max(0,i-1)][:3]);following=Vector(path[min(len(path)-1,i+1)][:3])
  tangent=(following-previous).normalized();side=tangent.cross(Vector((0,0,1))).normalized();front=tangent.cross(side).normalized()
  for j in range(10):
   a=j*2*math.pi/10;v=point+side*(math.cos(a)*r)+front*(math.sin(a)*r*.8)
   verts.append((v.x*sign,v.y,v.z))
 return verts
small=[(0,0,0,.011),(.010,-.006,.003,.012),(.020,-.005,.007,.010),(.028,.002,.011,.008),(.031,.010,.014,.004),(.030,.014,.015,.0008)]
large=[(0,0,0,.014),(.018,.004,.008,.016),(.040,-.003,.027,.014),(.055,-.020,.055,.011),(.045,-.040,.079,.006),(.028,-.038,.093,.0008)]
for sign,side in [(-1,'l'),(1,'r')]:
 faces=[tuple(range(9,-1,-1))]
 for r in range(len(small)-1):
  for i in range(10):faces.append((r*10+i,r*10+(i+1)%10,(r+1)*10+(i+1)%10,(r+1)*10+i))
 faces.append(tuple(range((len(small)-1)*10,len(small)*10)))
 mesh('ghost-arm-'+side,sweep(small,sign),faces,bodymat,at=(sign*.044,-.005,0),rage=sweep(large,sign),smooth=False)

# Keep the friendly form intact. The giant is a separately authored blocky rig
# beneath the same persistent pet asset, with its native animation retained.
calm=bpy.data.objects.new('CalmForm',None);bpy.context.collection.objects.link(calm);calm.parent=root
for obj in list(root.children):
 if obj==calm:continue
 obj.parent=calm
for obj in list(bpy.context.scene.objects):
 if obj.type=='MESH' and obj.data.shape_keys:obj.shape_key_clear()
rage_path=ROOT/'MapSource/characters/kuro/rage/kuro-rage.glb'
if not args.study_out:
 if not rage_path.exists():raise FileNotFoundError('Build the authored giant rig before exporting Kuro.')
 existing=set(bpy.context.scene.objects)
 bpy.ops.import_scene.gltf(filepath=str(rage_path))
 imported=[obj for obj in bpy.context.scene.objects if obj not in existing]
 # Blender creates display-only bone shapes when importing a rig. They are
 # authoring helpers, not part of the character geometry.
 shapes={b.custom_shape for obj in imported if obj.type=='ARMATURE' for b in obj.pose.bones if b.custom_shape}
 for obj in imported:
  if obj.type=='ARMATURE':
   for b in obj.pose.bones:b.custom_shape=None
 imported=[obj for obj in imported if obj not in shapes]
 for shape in shapes:bpy.data.objects.remove(shape,do_unlink=True)
 rage=bpy.data.objects.new('RageForm',None);bpy.context.collection.objects.link(rage);rage.parent=root
 for obj in imported:
  if obj.parent not in imported:obj.parent=rage
 # The source rig is3.5m. Combined with the existing2.38 bind and7.4 swell,
 # this gives a4.8m giant without distorting its authored proportions.
 rage.scale=(4.8/(3.5*2.38*7.4),)*3

scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
scene['design']='Same violet spirit in both forms: friendly chamfered familiar with authored graphic expressions, and a4.8m looming giant with an18-bone inhale/grasp cycle.'
bpy.context.view_layer.objects.active=root
for obj in bpy.context.scene.objects:obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(OUT),export_format='GLB',use_selection=True,export_animations=not bool(args.study_out),export_yup=True,export_cameras=False,export_lights=False)
sys.path.insert(0,str(ROOT/'tools'))
from normalize_gltf_skin_primitives import normalize
print('Normalized skinned meshes:',normalize(OUT))
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.shading.color_type='MATERIAL';space.region_3d.view_distance=.4
   space.region_3d.view_location=Vector((0,0,-.015))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'kuro.blend'))
print('Kuro parts:',[(o.name,len(o.data.vertices)) for o in scene.objects if o.type=='MESH'])

if args.study_out:
 scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
 scene.world.color=(.06,.055,.08)
 scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
 scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
 scene.display.shading.cavity_type='WORLD';scene.display.shading.curvature_ridge_factor=.4
 scene.display.shading.background_type='WORLD';scene.display.shading.show_object_outline=False
 camera_data=bpy.data.cameras.new('StudyCamera');camera=bpy.data.objects.new('StudyCamera',camera_data);scene.collection.objects.link(camera)
 camera_data.type='ORTHO';camera_data.ortho_scale=.23;camera.location=(.10,-.5,.06)
 camera.rotation_euler=(Vector((0,0,-.006))-camera.location).to_track_quat('-Z','Y').to_euler();scene.camera=camera
 scene.render.filepath=str(args.study_out/'calm.png');bpy.ops.render.render(write_still=True)
 for name,visible,hide in [('cat',['KuroCatMouth'],['ghost-mouth']),('goofy',['KuroCrossLeft','KuroCrossRight','KuroGoofyMouth'],['ghost-eye-l','ghost-eye-r','ghost-mouth']),('shy',['KuroShyEye','KuroPoutMouth'],['ghost-eye-r','ghost-mouth'])]:
  expressions.scale=(1,1,1)
  for obj in expressions.children:obj.hide_render=obj.name not in visible
  for part in ['ghost-eye-l','ghost-eye-r','ghost-mouth']:bpy.data.objects[part].hide_render=part in hide
  scene.render.filepath=str(args.study_out/(name+'.png'));bpy.ops.render.render(write_still=True)
