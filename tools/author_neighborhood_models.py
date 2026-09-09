"""Author the project's own civic/frontage meshes at their existing origin/scale.

References and composition decisions live in docs/reports/improvement-2026-09-10/
world-direction.md. No downloaded geometry or advertising is copied. The original
Godot-generated meshes remain recoverable in history; this is the Unity source.
Run only while Unity is closed, then run NeighborhoodFinishAuthor.Run in Unity.
"""
import math
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Assets/TumbangPreso/Art/models"


class Model:
    def __init__(self, name, materials):
        self.name, self.materials, self.faces = name, materials, []

    def face(self, points, material):
        self.faces.append((points, material))

    def box(self, center, size, material):
        x, y, z = center
        a, b, c = (v / 2 for v in size)
        p = [(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
             (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
        for ids in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(0,1,5,4),(3,7,6,2)]:
            self.face([p[i] for i in ids], material)

    def profile(self, points, front, back, material):
        # Counter-clockwise profile in XY, extruded along Z with explicit front/back.
        a = [(x,y,front) for x,y in points]
        b = [(x,y,back) for x,y in points]
        self.face(a[::-1], material); self.face(b, material)
        for i in range(len(a)):
            j=(i+1)%len(a)
            self.face([a[i],a[j],b[j],b[i]], material)

    def arch(self, x, bottom, spring, radius, width, z, depth, material):
        for side in [-1,1]:
            self.box((x+side*(radius+width/2), (bottom+spring)/2,z+depth/2),
                     (width,spring-bottom,depth), material)
        for i in range(16):
            a,b=i*math.pi/16,(i+1)*math.pi/16
            self.profile([(x+radius*math.cos(a),spring+radius*math.sin(a)),
                          (x+(radius+width)*math.cos(a),spring+(radius+width)*math.sin(a)),
                          (x+(radius+width)*math.cos(b),spring+(radius+width)*math.sin(b)),
                          (x+radius*math.cos(b),spring+radius*math.sin(b))],z,z+depth,material)

    def arched_panel(self, x, bottom, spring, radius, z, depth, material):
        points=[(x-radius,bottom),(x+radius,bottom),(x+radius,spring)]
        points += [(x+radius*math.cos(i*math.pi/16),spring+radius*math.sin(i*math.pi/16)) for i in range(1,17)]
        self.profile(points,z,z+depth,material)

    def cylinder(self, x,y,z,radius,height,material,sides=12):
        bottom=[(x+radius*math.cos(i*math.tau/sides),y,z+radius*math.sin(i*math.tau/sides)) for i in range(sides)]
        top=[(a,b+height,c) for a,b,c in bottom]
        self.face(bottom,material); self.face(top[::-1],material)
        for i in range(sides):
            j=(i+1)%sides
            self.face([bottom[i],top[i],top[j],bottom[j]],material)

    def write(self, filename):
        lines=["# Authored by tools/author_neighborhood_models.py",f"mtllib {filename}.mtl",f"o {self.name}"]
        vertex=1
        # Group by material so existing material slots are stable and there is one
        # submesh per material, not one renderer per brick or trim segment.
        for material in self.materials:
            lines.append("usemtl "+material)
            for points, mat in self.faces:
                if mat != material: continue
                p=np.asarray(points,dtype=float)
                # The live imported map instances present local +Z to the court.
                # Preserve the asymmetric shop footprint while turning its frontage.
                p[:,2] = (-.7 if filename == "env_sari_sari_store" else 0) - p[:,2]
                p=p[::-1]
                normal=np.cross(p[1]-p[0],p[2]-p[0]); magnitude=np.linalg.norm(normal)
                if magnitude < 1e-9: continue
                normal/=magnitude
                for point in p: lines.append("v "+" ".join(f"{v:.6f}" for v in point))
                for point in p: lines.append(f"vt {point[0]:.6f} {point[1]+point[2]:.6f}")
                for _ in p: lines.append("vn "+" ".join(f"{v:.6f}" for v in normal))
                for i in range(1,len(p)-1):
                    indices=[vertex,vertex+i,vertex+i+1]
                    lines.append("f "+" ".join(f"{j}/{j}/{j}" for j in indices))
                vertex += len(p)
        (DEST/(filename+".obj")).write_text("\n".join(lines)+"\n")
        mtl=["# Authored by tools/author_neighborhood_models.py"]
        for name,color in self.materials.items():
            mtl += ["newmtl "+name,"Kd "+" ".join(map(str,color)),"Ks 0.04 0.04 0.04","Ns 18","d 1","illum 2"]
        (DEST/(filename+".mtl")).write_text("\n".join(mtl)+"\n")
        print(filename,"vertices",vertex-1,"triangles",sum(1 for l in lines if l.startswith("f ")))


def church():
    m=Model("ChurchFacade",dict(stone=(.72,.68,.59),timber=(.34,.21,.12),dark=(.115,.105,.085)))
    m.box((0,3.1,.1),(5.65,6.2,1.2),"stone")
    m.profile([(-2.8,6.1),(2.8,6.1),(0,7.95)],-.56,.66,"stone")
    for y,w in [(0.15,6),(1.0,5.9),(4.1,6),(6.05,6)]:
        m.box((0,y,-.64),(w,.16,.3),"stone")
    for x in [-2.65,-1.4,1.4,2.65]:
        m.box((x,2.0,-.66),(.33,3.9,.38),"stone")
        for y in [.22,3.83,4.02]: m.box((x,y,-.69),(.48,.16,.44),"stone")
        m.box((x,5.03,-.65),(.23,1.73,.26),"stone")
        m.box((x,5.96,-.70),(.42,.13,.36),"stone")
    m.arched_panel(0,.12,2.15,1.0,-.525,.025,"dark")
    m.arched_panel(0,.12,2.09,.86,-.57,.025,"timber")
    m.arch(0,.1,2.15,1,.22,-.82,.28,"stone")
    for x in [-.62,-.31,0,.31,.62]:
        m.box((x,1.08,-.61),(.035,1.87,.045),"dark")
    for y in [.55,1.25,1.92]: m.box((0,y,-.62),(1.69,.045,.05),"timber")
    for x in [-.16,.16]: m.cylinder(x,1.16,-.64,.038,.12,"dark",8)
    for x in [-2,2]:
        m.arched_panel(x,1.2,2.35,.31,-.525,.03,"dark")
        m.arch(x,1.18,2.35,.31,.10,-.70,.18,"stone")
        m.box((x,1.14,-.73),(.92,.12,.38),"stone")
    m.arched_panel(0,4.46,5.04,.46,-.525,.025,"dark")
    m.arch(0,4.43,5.04,.46,.13,-.73,.2,"stone")
    for x in [-.27,0,.27]: m.box((x,4.88,-.56),(.035,.72,.045),"timber")
    m.box((0,4.84,-.58),(.85,.045,.035),"timber")
    m.arched_panel(0,6.52,6.94,.22,-.58,.02,"dark")
    m.arch(0,6.5,6.94,.22,.09,-.70,.1,"stone")
    for side in [-1,1]:
        # Stepped masonry along the pitched pediment, rather than a stack of boxes.
        for i in range(12):
            x=side*(.12+i*.226); y=7.94-abs(x)*.656
            m.box((x,y,-.66),(.25,.13,.24),"stone")
    m.box((0,8.04,.08),(.10,.72,.12),"timber")
    m.box((0,8.17,.08),(.47,.09,.12),"timber")
    m.write("env_church_facade")


def tower():
    m=Model("BellTower",dict(stone=(.72,.68,.59),band=(.48,.44,.36),roof=(.52,.25,.15),window=(.12,.105,.08)))
    m.box((0,4.45,0),(2.12,8.9,2.12),"stone")
    for y in [.18,3.5,6.5,9.0,12.15]: m.box((0,y,0),(2.55,.21,2.55),"band")
    for x in [-.98,.98]:
        for z in [-.98,.98]:
            m.box((x,5.4,z),(.2,7.05,.2),"stone")
            m.box((x,10.57,z),(.29,2.9,.29),"stone")
    for y in [2.0,5.0,7.7]:
        m.arched_panel(0,y-.6,y+.26,.28,-1.07,.02,"window")
        m.arch(0,y-.63,y+.26,.28,.1,-1.17,.1,"band")
    for direction in range(4):
        start=len(m.faces)
        m.arch(0,9.12,10.92,.73,.24,-1.1,.26,"stone")
        angle=direction*math.pi/2
        for i in range(start,len(m.faces)):
            p,mat=m.faces[i]
            m.faces[i]=([(x*math.cos(angle)-z*math.sin(angle),y,x*math.sin(angle)+z*math.cos(angle)) for x,y,z in p],mat)
    m.cylinder(0,9.6,0,.38,.7,"band")
    m.cylinder(0,9.52,0,.49,.14,"band")
    m.box((0,11.25,0),(1.8,.16,.16),"roof")
    base=[(-1.26,12.25,-1.26),(1.26,12.25,-1.26),(1.26,12.25,1.26),(-1.26,12.25,1.26)]
    for i in range(4): m.face([base[i],(0,13.83,0),base[(i+1)%4]],"roof")
    m.box((0,13.97,0),(.09,.66,.09),"band")
    m.box((0,14.13,0),(.39,.08,.09),"band")
    m.write("env_bell_tower")


def hall():
    m=Model("MunicipalHall",dict(body=(.80,.75,.63),plinth=(.55,.53,.47),band=(.68,.63,.53),roof=(.58,.28,.18),window=(.16,.22,.19)))
    m.box((0,3.4,.35),(11.7,6.8,5.3),"body")
    m.box((0,.32,0),(12.2,.64,5.9),"plinth")
    for y in [.68,3.65,6.75]: m.box((0,y,-.02),(12.3,.19,5.96),"band")
    for x in [-4.8,-2.4,0,2.4,4.8]:
        m.arched_panel(x,.7,2.42,.77,-2.31,.04,"window")
        m.arch(x,.67,2.42,.77,.2,-2.8,.49,"body")
        m.box((x,5.13,-2.34),(1.20,1.82,.09),"window")
        for side in [-1,1]:
            m.box((x+side*.66,5.13,-2.47),(.10,2,.24),"band")
            # Louver shutters give the civic frontage a different surface from houses.
            for row in range(9): m.box((x+side*.37,4.35+row*.19,-2.42),(.46,.065,.11),"body")
        m.box((x,4.12,-2.48),(1.48,.13,.3),"band")
        m.box((x,6.13,-2.46),(1.52,.16,.25),"band")
    for x in [-5.9,-3.6,-1.2,1.2,3.6,5.9]:
        m.box((x,2.13,-2.72),(.35,2.9,.44),"body")
        m.box((x,.81,-2.74),(.51,.26,.49),"band")
        m.box((x,3.47,-2.74),(.54,.19,.5),"band")
    m.face([(-6.2,8.55,0),(6.2,8.55,0),(6.2,6.87,-3),(-6.2,6.87,-3)],"roof")
    m.face([(-6.2,6.87,3),(6.2,6.87,3),(6.2,8.55,0),(-6.2,8.55,0)],"roof")
    m.box((0,8.5,0),(12.4,.17,.17),"roof")
    for z in [-2.8,-2.3,-1.8,-1.3,-.8,.8,1.3,1.8,2.3,2.8]:
        y=8.55-abs(z)*.56
        m.box((0,y+.025,z),(12.3,.035,.055),"roof")
    m.write("env_municipal_hall")


def store():
    m=Model("SariSariStore",dict(concrete=(.62,.61,.54),timber=(.43,.29,.16),dark=(.15,.17,.145),tarp=(.60,.32,.20),stripe=(.84,.70,.38)))
    m.box((0,.52,-.2),(2.4,1.04,1.4),"concrete")
    m.box((0,1.63,.43),(2.35,1.2,.14),"dark")
    for x in [-1.13,1.13]: m.box((x,1.66,-.1),(.14,1.2,1.15),"concrete")
    m.box((0,1.08,-.41),(2.4,.1,1.48),"timber")
    for y in [1.36,1.74,2.11]:
        m.box((0,y,.10),(2.14,.06,.56),"timber")
        for i in range(9):
            x=-.92+i*.225
            m.cylinder(x,y+.03,-.06,.06,.22,"stripe" if i%3 else "tarp",8)
            m.cylinder(x,y+.25,-.06,.024,.05,"dark",8)
    for x in [-.96,-.64,-.32,0,.32,.64,.96]: m.box((x,1.68,-.70),(.024,1.12,.03),"dark")
    for y in [1.31,1.65,1.98]: m.box((0,y,-.715),(2.19,.024,.025),"dark")
    for i in range(7):
        x=-.9+i*.3
        m.box((x,2.09,-.78),(.19,.29,.035),"stripe" if i%2 else "tarp")
    # Each shallow rib follows the same roof slope; the eave has real thickness.
    for i in range(24):
        x=-1.175+i*.10
        m.profile([(x,2.40),(x+.045,2.425),(x+.095,2.40),(x+.095,2.375),(x,2.375)],-1.2,.5,"tarp")
    m.box((0,2.35,-1.15),(2.4,.14,.10),"timber")
    for x in [-1.08,1.08]: m.box((x,1.17,-1.10),(.06,2.34,.06),"timber")
    m.box((0,2.48,.26),(1.72,.24,.08),"stripe")
    m.write("env_sari_sari_store")


def shade_tree():
    m=Model("ShadeTree",dict(bark=(.30,.22,.13),leaf_shadow=(.13,.24,.09),leaf=(.21,.34,.12),leaf_light=(.30,.42,.17)))
    def branch(a,b,ra,rb):
        a,b=np.array(a),np.array(b);axis=b-a;axis/=np.linalg.norm(axis)
        u=np.cross(axis,(0,0,1));u/=np.linalg.norm(u);v=np.cross(axis,u)
        lower=[a+ra*(u*math.cos(i*math.tau/9)+v*math.sin(i*math.tau/9)) for i in range(9)]
        upper=[b+rb*(u*math.cos(i*math.tau/9)+v*math.sin(i*math.tau/9)) for i in range(9)]
        for i in range(9):
            j=(i+1)%9;p=np.array([lower[i],lower[j],upper[j],upper[i]])
            if np.dot(np.cross(p[1]-p[0],p[2]-p[0]),p.mean(0)-(a+b)/2)<0:p=p[::-1]
            m.face(p.tolist(),"bark")
        m.face(upper,"bark");m.face(lower[::-1],"bark")
    branch((0,0,0),(.15,3.7,.1),.34,.18)
    for a,b in [((.1,2.7,.1),(-2.2,5.3,.55)),((.13,3.2,.1),(2.3,5.45,-.2)),
                ((.15,3.6,.1),(.2,5.6,-1.9)),((.1,3.0,.1),(-.8,5.1,1.8))]:
        branch(a,b,.16,.06)
    def crown(center,radii,material):
        center=np.array(center);radii=np.array(radii)
        rings=[]
        for lat in range(1,6):
            phi=lat*math.pi/6
            rings.append([center+radii*np.array((math.cos(i*math.tau/11)*math.sin(phi),math.cos(phi),math.sin(i*math.tau/11)*math.sin(phi))) for i in range(11)])
        def outward(p):
            p=np.array(p)
            if np.dot(np.cross(p[1]-p[0],p[2]-p[0]),p.mean(0)-center)<0:p=p[::-1]
            m.face(p.tolist(),material)
        top=center+np.array((0,radii[1],0));bottom=center-np.array((0,radii[1],0))
        for i in range(11):
            j=(i+1)%11;outward([top,rings[0][i],rings[0][j]]);outward([bottom,rings[-1][j],rings[-1][i]])
            for lat in range(4):outward([rings[lat][i],rings[lat+1][i],rings[lat+1][j],rings[lat][j]])
    crown((0,5.7,0),(3.4,1.55,2.7),"leaf_shadow")
    crown((-2.2,5.55,.6),(2.0,1.15,1.8),"leaf")
    crown((2.3,5.6,-.3),(2.2,1.25,1.9),"leaf")
    crown((.2,5.5,-2),(2.0,1.0,1.8),"leaf_shadow")
    crown((.2,6.45,.35),(2.1,1.1,2.05),"leaf_light")
    crown((-.9,5.3,2),(1.9,1.0,1.65),"leaf")
    m.write("env_shade_tree")


if __name__ == "__main__":
    church(); tower(); hall(); store(); shade_tree()
