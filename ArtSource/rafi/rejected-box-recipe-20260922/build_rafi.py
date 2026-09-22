"""Author selected Rafi B using the retained native seven-bone rig.

All ordinary animation channels and inverse binds are preserved. New geometry
uses the existing palette atlas and chunky body proportions. Never rewrites the
donor or another character. Run RosterBookBuilder afterward for Unity/FPP assets.
"""
from copy import deepcopy
import json
from pathlib import Path

from author_cast_finish import Geometry, animation_digest, store_geometry, write
from glb_mesh_dump import read_glb
import build_person_voxel as palette_writer

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/TumbangPreso/Art/characters/persons/team-rafi.glb'


def build():
    source = ROOT / 'Assets/TumbangPreso/Art/characters/persons/character-male-a.glb'
    g, original = read_glb(source)
    before = animation_digest(g, original)
    skeleton = deepcopy((g['skins'], g['nodes'][1:8]))
    blob = bytearray(original)
    counts = {}
    for node in (n for n in g['nodes'] if 'skin' in n):
        primitive = g['meshes'][node['mesh']]['primitives'][0]
        geo = Geometry({k: [] for k in primitive['attributes']}, [])
        def box(center, size, bone, color, bevel=.004):
            geo.bevel(center, size, bone, color, radius=bevel)
        if node['name'] == 'body-mesh':
            # Feet and legs sit on the unchanged hip pivot. No toes or fingers.
            for sign, leg, arm in ((1, 1, 4), (-1, 2, 5)):
                box((sign*.0836,.035,-.037),(.104,.054,.157),leg,14,.011)
                box((sign*.0836,.009,-.037),(.109,.018,.165),leg,3)
                box((sign*.0836,.055,-.060),(.107,.017,.025),leg,3)
                box((sign*.0836,.099,-.026),(.097,.093,.112),leg,14,.007)
                box((sign*.0836,.158,-.029),(.112,.087,.128),leg,2,.006)
                box((sign*.0836,.120,-.029),(.116,.018,.132),leg,10)
                # T-pose arm axis, broad unsegmented hands; cloth only near shoulder.
                box((sign*.207,.288,-.017),(.219,.097,.105),arm,14,.008)
                box((sign*.310,.288,-.017),(.077,.103,.112),arm,14,.012)
                box((sign*.133,.288,-.017),(.083,.112,.125),arm,0,.006)
                box((sign*.172,.288,-.017),(.022,.119,.132),arm,1,.004)
            box((0,.251,-.029),(.210,.157,.139),3,0,.010)
            box((0,.182,-.029),(.215,.030,.144),3,2)
            box((0,.314,-.029),(.086,.070,.090),3,14,.010)
            # Open collar, undershirt, stitched placket and a single practical pouch.
            box((0,.294,-.101),(.041,.060,.007),3,1,.001)
            box((-.036,.315,-.104),(.033,.040,.010),3,4,.003)
            box((.036,.315,-.104),(.033,.040,.010),3,4,.003)
            box((.012,.244,-.102),(.009,.060,.006),3,4,.001)
            box((-.058,.266,-.105),(.044,.031,.008),3,4,.002)
            box((.078,.191,-.119),(.067,.067,.036),3,5,.006)
            box((.078,.213,-.142),(.070,.024,.008),3,6,.002)
            box((.078,.205,-.148),(.011,.022,.007),3,7,.001)
            box((-.018,.185,-.106),(.017,.024,.014),3,7,.004)
            box((-.029,.166,-.106),(.008,.027,.009),3,7,.002)
            box((-.007,.161,-.108),(.008,.036,.009),3,7,.002)
        else:
            # Large native flat face. Hair silhouette comes from four broad masses.
            box((0,.498,-.012),(.302,.307,.254),6,14,.029)
            for sign in (-1,1):
                box((sign*.158,.477,-.003),(.043,.077,.085),6,14,.010)
                # Graphic face marks lie on the face, with no extruded boxes for
                # the outline pass to turn into floating glasses or a protruding mouth.
                def mark(cx,cy,w,h):
                    geo.face([(cx-w/2,cy-h/2,-.1398),(cx-w/2,cy+h/2,-.1398),
                              (cx+w/2,cy+h/2,-.1398),(cx+w/2,cy-h/2,-.1398)],6,8,(cx,cy,0))
                mark(sign*.075,.512,.021,.044)
                mark(sign*.076,.552,.050,.011)
            box((0,.650,.000),(.319,.139,.271),6,9,.023)
            box((-.132,.580,.033),(.068,.158,.208),6,9,.014)
            box((.122,.597,.020),(.068,.132,.225),6,9,.014)
            box((-.038,.612,-.137),(.215,.085,.040),6,9,.014)
            box((.098,.633,-.128),(.066,.070,.052),6,9,.010)
            # Low tied bun at the back, with a single understated binding.
            box((.012,.613,.159),(.095,.060,.041),6,3,.008)
            box((.014,.626,.199),(.151,.121,.116),6,9,.020)
            box((.014,.626,.190),(.024,.125,.113),6,4,.006)
            # Small asymmetric smile, graphic rather than sculpted lips.
            mark(-.019,.451,.040,.007)
            mark(.012,.456,.025,.007)
            mark(.030,.464,.007,.015)
        # Native imported people face +Z in source space. Reflect each authored
        # part about its own unchanged joint depth, not the skeleton or animation.
        # The first sheet exposed an accidental -Z face/back reversal.
        depth=(0,-.02875,-.02875,-.02875,-.01725,-.01725,-.0023612226)
        for i, point in enumerate(geo.data['POSITION']):
            bone=geo.data['JOINTS_0'][i][0]
            geo.data['POSITION'][i]=(point[0],point[1],2*depth[bone]-point[2])
            n=geo.data['NORMAL'][i];geo.data['NORMAL'][i]=(n[0],n[1],-n[2])
            if 'TANGENT' in geo.data:
                t=geo.data['TANGENT'][i];geo.data['TANGENT'][i]=(t[0],t[1],-t[2],-t[3])
        for i in range(0,len(geo.indices),3):geo.indices[i+1],geo.indices[i+2]=geo.indices[i+2],geo.indices[i+1]
        store_geometry(g, blob, primitive, geo.data, geo.indices)
        counts[node['name']] = {'vertices': len(geo.data['POSITION']), 'triangles':len(geo.indices)//3}
    assert skeleton == (g['skins'],g['nodes'][1:8])
    assert animation_digest(g,blob)==before
    # A rigid helper under the existing head, not a new bone. Its rest transform
    # and name survive imported portraits, live poses and archived replays.
    head_primitive=g['meshes'][1]['primitives'][0]
    gill_primitive=deepcopy(head_primitive)
    gills=Geometry({key:[] for key in gill_primitive['attributes']},[])
    for sign in (-1,1):
        for y in (.052,.067):
            x=sign*.1518
            gills.face([(x,y-.002,.006),(x,y+.002,.006),(x,y+.002,.031),(x,y-.002,.031)],
                       6,11,(0,y,.018))
    store_geometry(g,blob,gill_primitive,gills.data,gills.indices)
    gill_primitive['attributes'].pop('JOINTS_0',None);gill_primitive['attributes'].pop('WEIGHTS_0',None)
    gill_mesh=len(g['meshes']);g['meshes'].append({'name':'RafiMagicGills','primitives':[gill_primitive]})
    gill_node=len(g['nodes']);g['nodes'].append({'name':'rafi-gills','mesh':gill_mesh})
    g['nodes'][7].setdefault('children',[]).append(gill_node)
    assert animation_digest(g,blob)==before
    g['nodes'][0]['name']='team-rafi' 
    g['scenes'][0]['name']='team-rafi'
    g['extras']={'rafiAuthor':'selected-B-v4-native-gill-helper','preserveOwnerBackupGeometry':True}
    g['asset']['generator']='TUMP Rafi B native-rig author'
    write(OUT,g,blob)
    palette_writer.PALETTE={
        0:'667F6C',1:'D5D2B7',2:'2E364C',3:'242A35',4:'465E4F',5:'746044',
        6:'998261',7:'E2D4AA',8:'211E22',9:'29262D',10:'454D63',11:'5B837D',
        12:'A97149',13:'D7A574',14:'B88151',15:'F1DDAD'}
    palette_writer.write_palette(str(ROOT/'MapSource/materials_persons/person_team-rafi.tres'))
    receipt={'source':str(source.relative_to(ROOT)),'output':str(OUT.relative_to(ROOT)),
             'animation_sha256':before,'animations':len(g['animations']),'geometry':counts,'helper':'rafi-gills; rigid child of retained head; no new skin bone',
             'limits':'Authored geometry; imported appearance and FPP still require integration inspection.'}
    report=ROOT/'docs/reports/full-backlog-2026-09-21/rafi-authoring.json'
    report.write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt,indent=2))


if __name__=='__main__':
    build()
