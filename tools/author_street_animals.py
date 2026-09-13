"""Original chunky street cats and aspins. Study outputs stay outside Unity until reviewed."""
import argparse,json,math,sys
from pathlib import Path
import bpy
from mathutils import Vector,Quaternion

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--out',default='MapSource/environment/ambient-life/street-animals/study-v1')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=ROOT/args.out;out.mkdir(parents=True,exist_ok=True)

def material(name,colour):
    result=bpy.data.materials.new(name);result.diffuse_color=(*colour,1);return result

def bind(obj,bone,mat):
    obj.data.materials.append(mat)
    obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))),1,'REPLACE')
    return obj

def box(name,at,size,bone,mat,bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at)
    obj=bpy.context.object;obj.name=name;obj.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        edge=obj.modifiers.new('Broad simple corners','BEVEL');edge.width=bevel;edge.segments=1
        bpy.ops.object.modifier_apply(modifier=edge.name)
    return bind(obj,bone,mat)

def prism(name,points,depth,bone,mat):
    n=len(points);vertices=[(x,y+offset,z) for offset in (-depth/2,depth/2) for x,y,z in points]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    return bind(obj,bone,mat)

def tail_mesh(name,points,radii,bone,mat):
    vertices=[];faces=[]
    for index,point in enumerate(points):
        at=Vector(point)
        tangent=Vector(points[min(index+1,len(points)-1)])-Vector(points[max(0,index-1)])
        tangent.normalize();across=Vector((1,0,0));other=tangent.cross(across).normalized()
        for side in range(6):
            angle=side*math.tau/6
            vertices.append(tuple(at+radii[index]*(math.cos(angle)*across+math.sin(angle)*other)))
    for ring in range(len(points)-1):
        for side in range(6):
            a=ring*6+side;b=ring*6+(side+1)%6;faces.append((a,b,b+6,a+6))
    faces.extend([tuple(range(5,-1,-1)),tuple(range((len(points)-1)*6,len(points)*6))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);return bind(obj,bone,mat)

def animate_animal(rig,animal,kind,size):
    scene=bpy.context.scene;scene.render.fps=30;rig.animation_data_create()
    for name,duration in [('idle',2.4),('walk',1.0),('run',.6),('alert',.7)]:
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        samples=48 if name=='idle' else 30
        for sample in range(samples+1):
            phase=sample/samples;frame=1+phase*duration*30
            for bone in rig.pose.bones:
                bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.location=Vector((0,0,0))
            for bone_name in ('FrontL','FrontR','BackL','BackR'):
                bone=rig.pose.bones[bone_name];base=bone.bone.matrix_local.to_quaternion();lift=0;angle=0
                if name in ('walk','run'):
                    offset=(0 if bone_name in ('FrontL','BackR') else .5) if name=='walk' else {'FrontL':0,'FrontR':.10,'BackL':.52,'BackR':.62}[bone_name]
                    cycle=(phase+offset)%1;stance=.60 if name=='walk' else .46;swing=max(0,(cycle-stance)/(1-stance))
                    reach=23 if name=='walk' else 36
                    angle=reach*(1-2*cycle/stance) if cycle<stance else reach*(-1+2*swing)
                    lift=(.045 if name=='walk' else .075)*math.sin(swing*math.pi)
                bone.rotation_quaternion=base.inverted()@Quaternion((1,0,0),math.radians(angle))@base
                bone.location=base.inverted()@Vector((0,0,lift))
            head=rig.pose.bones['Head'];base=head.bone.matrix_local.to_quaternion()
            glance=(math.sin(phase*math.tau)*8 if name=='idle' else math.sin(math.pi*phase)*22 if name=='alert' else 0)
            head.rotation_quaternion=base.inverted()@Quaternion((0,0,1),math.radians(glance))@base
            for tail_name,multiplier in [('TailBase',1),('TailTip',1.45)]:
                bone=rig.pose.bones[tail_name];base=bone.bone.matrix_local.to_quaternion()
                angle=math.sin(phase*math.tau*(2 if kind=='dog' else 1))*multiplier*(9 if name=='idle' else 5)
                bone.rotation_quaternion=base.inverted()@Quaternion((0,0,1),math.radians(angle))@base
            bpy.context.view_layer.update()
            evaluated=animal.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
            bottom=min((evaluated.matrix_world@v.co).z for v in mesh.vertices);evaluated.to_mesh_clear()
            root_bone=rig.pose.bones['Root'];base=root_bone.bone.matrix_local.to_quaternion()
            root_bone.location=base.inverted()@Vector((0,0,-bottom/size))
            for bone in rig.pose.bones:
                bone.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=bone.name)
                bone.keyframe_insert(data_path='location',frame=frame,group=bone.name)
    rig.animation_data.action=None
    for bone in rig.pose.bones:bone.rotation_quaternion=Quaternion();bone.location=Vector((0,0,0))
    bpy.context.view_layer.update()

specs=[
    ('aspin-tan','dog',(0.58,.36,.19),'upright','curled',1),
    ('aspin-patched','dog',(.17,.18,.17),'floppy','straight',.92),
    ('aspin-cream','dog',(.72,.66,.49),'one-flop','curled',1.07),
    ('pusakal-tabby','cat',(.43,.39,.31),'cat','up',.76),
    ('pusakal-tuxedo','cat',(.13,.16,.16),'cat','curled',.72),
    ('pusakal-ginger','cat',(.64,.38,.18),'cat','up',.81),
]

for identity,kind,colour,ears,tail,size in specs:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    # Each native file owns four actions. Compatible bone names otherwise cause
    # the exporter to accumulate the previous animal's actions into later files.
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    cat=kind=='cat';parts=[]
    coat=material('Coat',colour);cream=material('Cream markings',(.79,.75,.63))
    ink=material('Graphic features',(.075,.079,.065));inner=material('Inner ear',(.53,.35,.29))
    accent=material('Quiet coat patches',tuple(v*.66 for v in colour))
    body_length=.62 if cat else .76;body_width=.28 if cat else .36
    parts.append(box('Torso',(0,0,.39),(body_width,body_length,.32),'Body',coat,.045))
    parts.append(box('Chest',(0,-body_length*.37,.39),(body_width*.87,.15,.27),'Body',cream,.024))
    head_y=-body_length*.56;head_z=.58
    parts.append(box('Neck',(0,head_y+.10,.47),(.22,.22,.29),'Body',coat,.025))
    parts.append(box('Head',(0,head_y,head_z),(.33 if cat else .31,.25 if cat else .29,.27),'Head',coat,.039))
    if cat:
        for side in (-1,1):
            parts.append(box('Muzzle cheek',(.047*side,head_y-.13,.532),(.095,.049,.066),'Head',cream,.014))
        parts.append(prism('Cat nose',[(-.018,head_y-.160,.555),(.018,head_y-.160,.555),(0,head_y-.160,.533)],.014,'Head',inner))
    else:
        parts.append(box('Muzzle',(0,head_y-.173,.525),(.20,.17,.115),'Head',cream,.024))
        parts.append(box('Small nose',(0,head_y-.264,.55),(.054,.03,.038),'Head',ink,.009))
    for side in (-1,1):
        eye_y=head_y-(.125 if cat else .143)
        parts.append(box('Eye',(.103*side,eye_y,.609),(.030 if cat else .025,.010,.032),'Head',ink,.004))
        parts.append(box('Eye glint',(.098*side,eye_y-.006,.618),(.007,.004,.008),'Head',cream,0))
        if ears=='floppy' or (ears=='one-flop' and side<0):
            points=[(.11*side,head_y,.70),(.22*side,head_y,.68),(.195*side,head_y,.47),(.145*side,head_y,.49)]
        else:
            height=.13 if cat else .21
            points=[(.057*side,head_y,.69),(.166*side,head_y,.68),(.142*side,head_y,.69+height)]
        parts.append(prism('Ear',points,.065,'Head',coat))
        if ears not in ('floppy',) and not(ears=='one-flop' and side<0):
            height=.085 if cat else .135
            parts.append(prism('Inner ear',[(.086*side,head_y-.037,.709),(.145*side,head_y-.037,.704),(.138*side,head_y-.037,.704+height)],.006,'Head',inner))
    for front in (True,False):
        for side in (-1,1):
            name=('Front' if front else 'Back')+('L' if side<0 else 'R')
            x=body_width*.34*side;y=(-1 if front else 1)*body_length*.34
            parts.append(box(name+' leg',(x,y,.21),(.090,.105,.31),name,coat,.012))
            parts.append(box(name+' paw',(x,y-.028,.042),(.105,.155,.084),name,cream,.017))
    tail_start=body_length*.42
    tail_z=.48
    if tail=='straight':
        parts.append(tail_mesh('Tail base',[(0,tail_start,tail_z),(0,tail_start+.14,tail_z+.015),(0,tail_start+.23,tail_z+.05)],[.045,.038,.030],'TailBase',coat))
        parts.append(tail_mesh('Tail tip',[(0,tail_start+.23,tail_z+.05),(0,tail_start+.34,tail_z+.065),(0,tail_start+.40,tail_z+.04)],[.030,.023,.013],'TailTip',cream))
    else:
        parts.append(tail_mesh('Tail rise',[(0,tail_start,tail_z),(0,tail_start+.10,tail_z+.09),(0,tail_start+.10,tail_z+.24)],[.045,.041,.034],'TailBase',coat))
        parts.append(tail_mesh('Tail curve',[(0,tail_start+.10,tail_z+.24),(0,tail_start+.065,tail_z+.33),
            (0,tail_start-.035,tail_z+(.34 if tail=='up' else .31)),(0,tail_start-.065,tail_z+(.30 if tail=='up' else .25))],
            [.034,.030,.024,.013],'TailTip',accent))
    if 'patched' in identity or 'tuxedo' in identity:
        parts.append(box('Broad flank patch',(body_width/2+.0001,.045,.39),(.0002,.22,.18),'Body',cream,0))
    if cat and 'tuxedo' not in identity:
        for stripe in range(3):
            parts.append(box('Back stripe',(0,-.06+stripe*.13,.5501),(body_width*.72,.027,.0002),'Body',accent,0))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();animal=bpy.context.object;animal.name=identity
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    data=bpy.data.armatures.new('StreetAnimalRig');rig=bpy.data.objects.new('StreetAnimalRig',data)
    bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;animal.select_set(False);rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bones={'Root':((0,0,0),(0,0,.1),None),'Body':((0,0,.32),(0,-.15,.40),'Root'),
        'Head':((0,head_y+.1,.47),(0,head_y,.66),'Body'),
        'TailBase':((0,tail_start,.48),(0,tail_start+.1,.67),'Body'),
        'TailTip':((0,tail_start+.07,.68),(0,tail_start+.1,.79),'TailBase')}
    for front in (True,False):
        for side in (-1,1):
            name=('Front' if front else 'Back')+('L' if side<0 else 'R')
            x=body_width*.34*side;y=(-1 if front else 1)*body_length*.34
            bones[name]=((x,y,.34),(x,y,.055),'Root')
    for name,(head,end,parent) in bones.items():
        bone=data.edit_bones.new(name);bone.head=head;bone.tail=end
        if parent:bone.parent=data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT');animal.parent=rig
    deform=animal.modifiers.new('Simple articulated animal','ARMATURE');deform.object=rig
    rig.scale=(size,size,size)
    marker=bpy.data.objects.new('LookForward',None);bpy.context.collection.objects.link(marker);marker.parent=rig;marker.location=(0,-1,0)
    animate_animal(rig,animal,kind,size)
    bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=rig
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(identity+'.blend')))
    bpy.ops.export_scene.gltf(filepath=str(out/(identity+'.glb')),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS')
    scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
    scene.world.color=(.17,.18,.19);shade=scene.display.shading;shade.light='STUDIO';shade.color_type='MATERIAL'
    shade.show_shadows=True;shade.show_specular_highlight=False;shade.background_type='WORLD'
    camera_data=bpy.data.cameras.new('Review');camera=bpy.data.objects.new('Review',camera_data);bpy.context.collection.objects.link(camera);scene.camera=camera
    camera.location=(1.4,-2.1,1.15);camera.rotation_euler=(Vector((0,-.03,.4*size))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera_data.type='ORTHO';camera_data.ortho_scale=1.5*size
    scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
    for pose in ('idle','walk'):
        for name in ('FrontL','FrontR','BackL','BackR'):
            bone=rig.pose.bones[name];base=bone.bone.matrix_local.to_quaternion()
            angle=0 if pose=='idle' else (25 if name in ('FrontL','BackR') else -25)
            bone.rotation_mode='QUATERNION';bone.rotation_quaternion=base.inverted()@Quaternion((1,0,0),math.radians(angle))@base
        bpy.context.view_layer.update();scene.render.filepath=str(out/(identity+'-'+pose+'.png'));bpy.ops.render.render(write_still=True)
    (out/(identity+'.json')).write_text(json.dumps({'id':identity,'kind':kind,'coat':colour,'ears':ears,'tail':tail,
        'scale':size,'bones':list(bones),'animations':['idle','walk','run','alert'],'vertices':len(animal.data.vertices),'author':'tools/author_street_animals.py','status':'unreviewed native study, not in game'},indent=2))
    print(identity,len(animal.data.vertices),'vertices')
