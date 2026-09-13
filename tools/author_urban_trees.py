"""Author three original broadleaf forms for TUMP; study output stays in Logs.

Large canopy masses and visible branching replace cones and flattened stacked
crowns. These are stylized lowland urban trees, not botanical reconstructions.
"""
import bpy,bmesh,math,argparse,sys,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--out',default='Logs/urban-trees-v1')
parser.add_argument('--publish',action='store_true')
parser.add_argument('--only',nargs='+',help='Author selected forms without rewriting other source assets')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
OUT=(ROOT/args.out).resolve();OUT.mkdir(parents=True,exist_ok=True)

def material(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=.95
 return m
def mesh(name,vertices,faces,mat):
 d=bpy.data.meshes.new(name);d.from_pydata(vertices,[],faces);d.update()
 bm=bmesh.new();bm.from_mesh(d);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(d);bm.free()
 obj=bpy.data.objects.new(name,d);bpy.context.collection.objects.link(obj);d.materials.append(mat)
 return obj
def branch(name,points,radii,mat):
 verts=[];sides=7
 for j,p in enumerate(points):
  p=Vector(p);axis=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])).normalized()
  u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u).normalized()
  for i in range(sides):verts.append(p+(u*math.cos(i*math.tau/sides)+v*math.sin(i*math.tau/sides))*radii[j])
 faces=[tuple(range(sides-1,-1,-1)),tuple(range(len(verts)-sides,len(verts)))]
 for j in range(len(points)-1):
  for i in range(sides):faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
 return mesh(name,verts,faces,mat)
def crown(name,center,size,mat,yaw=0):
 # A domed, chamfered volume with sloping undersides, not a flat foliage plate.
 verts=[];sides=10
 for row,(height,radius,offset) in enumerate([(-.85,.30,.12),(-.48,.82,.06),(.06,1,0),(.60,.78,-.05),(.93,.28,-.10)]):
  for i in range(sides):
   a=i*math.tau/sides+yaw;rx=math.cos(a)*radius;ry=math.sin(a)*radius
   verts.append((center[0]+(rx+offset)*size[0],center[1]+ry*size[1],center[2]+height*size[2]))
 faces=[tuple(range(sides-1,-1,-1)),tuple(range(4*sides,5*sides))]
 for row in range(4):
  for i in range(sides):faces.append((row*sides+i,row*sides+(i+1)%sides,(row+1)*sides+(i+1)%sides,(row+1)*sides+i))
 obj=mesh(name,verts,faces,mat)
 for face in obj.data.polygons:face.use_smooth=True
 return obj

def lobed_crown(name,center,size,mat,phase=0):
 # Broad connected facets and an asymmetric outline, without per-leaf noise.
 # Unequal ring radii keep neighbouring masses from reading as identical bowls.
 verts=[];sides=11
 for row,(height,radius) in enumerate([(-.78,.30),(-.40,.78),(.08,1),(.60,.75),(.96,.19)]):
  for i in range(sides):
   a=i*math.tau/sides+phase
   rim=radius*(1+.11*math.sin(a*3+phase)+.055*math.cos(a*5-.8))
   verts.append((center[0]+(math.cos(a)*rim+.10*height)*size[0],
                 center[1]+(math.sin(a)*rim-.08*height)*size[1],
                 center[2]+(height+.07*math.sin(a*2+phase)*radius)*size[2]))
 faces=[tuple(range(sides-1,-1,-1)),tuple(range(4*sides,5*sides))]
 for row in range(4):
  for i in range(sides):faces.append((row*sides+i,row*sides+(i+1)%sides,(row+1)*sides+(i+1)%sides,(row+1)*sides+i))
 obj=mesh(name,verts,faces,mat)
 for face in obj.data.polygons:face.use_smooth=True
 return obj

forms=['street-broadleaf','plaza-shade','courtyard-tree','acacia-spread','mango-yard','narra-young']
if args.only:
 if any(n not in forms for n in args.only):raise ValueError('Unknown tree form')
 forms=args.only
for name in forms:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bark=material(name+' bark',(.24,.155,.085));leaf=material(name+' foliage',(.20,.32,.12))
 light=material(name+' new growth',(.27,.39,.16));shade=material(name+' shaded foliage',(.17,.275,.115))
 if name=='acacia-spread':
  branch('Leaning mature trunk',[(0,0,0),(.08,.06,.8),(.28,.02,2.0),(.56,.16,3.1),(.4,.2,4.9)],[.43,.35,.30,.24,.09],bark)
  branch('Long west bough',[(.30,.05,2.15),(-.70,.02,3.0),(-2,.13,3.95),(-3.30,.20,4.9)],[.26,.22,.15,.05],bark)
  branch('Rising east bough',[(.50,.15,2.7),(1.5,.0,3.55),(2.80,-.1,4.25),(3.30,-.1,5.0)],[.23,.17,.12,.045],bark)
  branch('Back fork',[(.5,.1,3.0),(.15,1.3,3.9),(-.30,2.15,4.9)],[.18,.12,.04],bark)
  lobed_crown('West shade',(-2.55,.10,5.0),(2.28,1.95,.91),leaf,.4)
  lobed_crown('East shade',(2.68,-.08,5.22),(2.15,1.82,.91),shade,1.8)
  lobed_crown('Upper arch',(.45,-.12,5.75),(2.62,2.03,1.02),light,.8)
  lobed_crown('Rear crown',(-.25,1.65,5.25),(2.06,1.75,.97),leaf,2.1)
 elif name=='mango-yard':
  branch('Garden trunk',[(0,0,0),(-.04,.03,1.05),(.17,.04,2.1),(.18,.08,3.65)],[.28,.22,.18,.07],bark)
  branch('Low fork',[(.06,.02,1.55),(-.64,.0,2.35),(-1.10,.1,3.5)],[.17,.12,.045],bark)
  branch('Garden shoulder',[(.13,.04,2.1),(.9,.12,2.9),(1.35,.12,3.55)],[.14,.09,.04],bark)
  lobed_crown('Dense upper crown',(.05,.18,4.02),(1.60,1.42,1.40),leaf,.8)
  lobed_crown('Drooping west',(-1.02,-.03,3.22),(1.10,1.18,1.24),shade,1.4)
  lobed_crown('Drooping east',(1.13,-.12,3.45),(1.12,1.02,1.14),leaf,2.3)
  lobed_crown('Front growth',(-.15,-.83,3.56),(.98,.80,1.20),light,.2)
 elif name=='narra-young':
  branch('Young upright trunk',[(0,0,0),(-.08,.02,1.5),(.13,.10,3.0),(.3,.05,4.35),(.10,.05,5.95)],[.25,.19,.15,.11,.04],bark)
  branch('Left rising limb',[(.10,.10,2.65),(-.75,.12,3.35),(-1.10,.04,4.75)],[.14,.09,.025],bark)
  branch('Right rising limb',[(.17,.09,3.35),(.9,-.12,4.2),(1.12,-.02,5.35)],[.115,.08,.025],bark)
  lobed_crown('Higher leader',(.17,.03,5.85),(1.20,1.10,1.34),light,1.1)
  lobed_crown('West branch leaves',(-.99,.13,4.58),(1.05,1.15,1.26),leaf,.3)
  lobed_crown('East branch leaves',(.96,-.15,5.10),(.97,1.10,1.16),shade,2.4)
 elif name=='street-broadleaf':
  branch('Bent trunk',[(0,0,0),(.06,.04,1.2),(-.10,.08,2.65),(.10,.04,4.7)],[.26,.22,.18,.07],bark)
  branch('Left fork',[(-.10,.08,2.4),(-.85,.03,3.4),(-1.35,.05,4.6)],[.15,.10,.035],bark)
  branch('Right fork',[(0,.07,3.0),(.8,.2,3.8),(1.4,.28,4.65)],[.13,.09,.03],bark)
  crown('Left crown',(-1.10,.10,4.7),(1.25,1.30,1.25),shade,.12)
  crown('Right crown',(1.13,.24,4.88),(1.30,1.17,1.32),leaf,.4)
  crown('High crown',(.10,-.05,5.55),(1.55,1.42,1.44),light,.1)
 elif name=='plaza-shade':
  branch('Flared trunk',[(0,0,0),(.08,.03,1.4),(.18,.12,2.75),(.15,-.3,4.6)],[.39,.29,.24,.08],bark)
  branch('West bough',[(.1,.08,2.5),(-1.1,.1,3.5),(-2.25,.06,4.55)],[.21,.16,.045],bark)
  branch('East bough',[(.16,.1,2.75),(1.25,.2,3.8),(2.45,.1,4.6)],[.21,.13,.04],bark)
  branch('Back bough',[(.18,.1,3.0),(.1,1.35,3.95),(-.4,2.05,4.55)],[.16,.10,.035],bark)
  crown('West crown',(-2.10,.03,4.65),(1.80,1.70,1.24),leaf,.15)
  crown('East crown',(2.06,.13,4.94),(1.91,1.66,1.32),shade,.42)
  crown('Back crown',(-.32,1.51,5.06),(1.92,1.60,1.32),leaf,.23)
  crown('Front crown',(.05,-.82,5.49),(2.02,1.72,1.47),light,.05)
 elif name=='courtyard-tree':
  branch('Garden trunk',[(0,0,0),(-.03,.02,.9),(.12,.04,2.05),(.1,0,3.3)],[.17,.14,.105,.04],bark)
  branch('Garden fork',[(.04,.04,1.6),(-.62,.08,2.45),(-.77,.03,3.03)],[.095,.06,.02],bark)
  crown('Garden lower',(-.49,.12,2.95),(1.03,.95,1.02),shade,.3)
  crown('Garden upper',(.29,.01,3.55),(1.23,1.10,1.25),leaf,.1)
 # One static mesh with a few material groups; no per-leaf objects or motion.
 bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=bpy.context.selected_objects[0]
 bpy.ops.object.join();tree=bpy.context.object;tree.name=name
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
 bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),export_format='GLB',use_selection=True,export_animations=False)
 if args.publish:
  import shutil
  source=ROOT/'MapSource/environment/urban-trees';assets=ROOT/'Assets/TumbangPreso/Art/models/urban-trees'
  source.mkdir(parents=True,exist_ok=True);assets.mkdir(parents=True,exist_ok=True)
  shutil.copy2(OUT/(name+'.blend'),source/(name+'.blend'));shutil.copy2(OUT/(name+'.glb'),assets/(name+'.glb'))
 scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
 scene.world.color=(.11,.13,.15);s=scene.display.shading;s.light='STUDIO';s.color_type='MATERIAL';s.show_shadows=True;s.show_specular_highlight=False;s.background_type='WORLD'
 data=bpy.data.cameras.new('Tree review');cam=bpy.data.objects.new('Tree review',data);bpy.context.collection.objects.link(cam);scene.camera=cam
 cam.location=(10,-18,7);cam.rotation_euler=(Vector((0,0,3.3))-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=11 if name=='acacia-spread' else 8.6
 scene.render.resolution_x=950;scene.render.resolution_y=950;scene.render.resolution_percentage=100
 scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
 print(name,'vertices',len(tree.data.vertices),'polygons',len(tree.data.polygons))
