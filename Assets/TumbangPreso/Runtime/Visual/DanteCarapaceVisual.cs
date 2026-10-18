using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // A fitted stone ward. It follows the actual torso and never fills its
    // wearer's first-person view with the former sphere and orbiting cubes.
    //
    // UNSTOPPABLE's beats around the owner-approved armour (`docs/reports/dante-presentation-2026-10-16/plan.md` section 2;
    // the fitted plates' shapes and the orbiting shields are kept exactly):
    //  * TELL (0 to .15 s): the court cracks in a small ring under his feet, the seams lit (`_ground`).
    //  * ARMOUR (.15 to about .6 s): the plates rise from the court and lock on him from the lowest to the highest, each a
    //    0.06 s jolt; a plate's seams light as it locks. The orbiting shields form last.
    //  * REFUSAL: while it holds, a status that would land flashes the plate nearest its colour's side and splashes chips of
    //    that colour off it (`CharacterMotor.StatusRefused`, `DanteStatusShatter.Refusal`).
    //  * WARNING (last 2 s): the seams cool from orange to dull red, the plates tremble, the orbit slows.
    //  * DISSIPATE (last .5 s): the plates crack and drop to the court, break smaller and sink into it. Never an alpha fade.
    // All of it posed from `StepTo(seconds)`, so a replay or a rejoiner draws the same frame.
    public sealed class DanteCarapaceVisual : MonoBehaviour, IVfxTimeline
    {
        private CharacterMotor _owner;
        private Transform _worldRoot;
        private float _recordedAge,_front;
        public CharacterMotor RecordedOwner=>_owner;
        public float RecordedAge=>_recordedAge;
        public bool RecordedHeavy=>_heavy;
        public float RecordedFront=>_front;
        public bool RecordedVisible=>_visible;
        private float _duration;
        private bool _heavy;
        private bool _visible=true;
        private readonly List<Plate> _plates=new List<Plate>();
        private readonly List<Plate> _orbiting=new List<Plate>();
        private GameObject _orbitRoot;
        private DanteWardBadge _badge;
        private Renderer[] _renderers;
        private int[] _rank;
        private float[] _flashAt;
        private GameObject _ground;
        private Material _groundSeam;
        private const float AssembleFrom = .15f, AssembleStep = .06f, WarnSeconds = 2f, CrumbleSeconds = .5f;
        private float OrbitFrom => AssembleFrom + _plates.Count * AssembleStep;
        public float LifeSeconds=>Mathf.Max(.1f,_duration);
        public IReadOnlyList<Renderer> VisiblePieces=>_renderers;

        private readonly struct Plate
        {
            public readonly Transform Node;
            public readonly Vector3 Position,Scale,Normal;
            public readonly Quaternion Rotation;
            public readonly Material Seam;
            public Plate(Transform node,Vector3 normal,Material seam)
            {Node=node;Position=node.localPosition;Scale=node.localScale;Rotation=node.localRotation;Normal=normal;Seam=seam;}
        }

        public static DanteCarapaceVisual Attach(CharacterMotor owner,bool heavy,float duration)
        {
            var visual=owner.GetComponent<CharacterVisual>();
            return visual!=null&&visual.Model!=null?BuildForModel(visual.Model,owner.transform,owner,heavy,duration,0):null;
        }
        public static DanteCarapaceVisual Recorded(GameObject model,Transform root,bool heavy,float duration,float front)
        {
            var ward=BuildForModel(model,root,null,heavy,duration,front);if(ward!=null)ward.enabled=false;return ward;
        }
        private static DanteCarapaceVisual BuildForModel(GameObject model,Transform worldRoot,CharacterMotor owner,bool heavy,float duration,float front)
        {
            SkinnedMeshRenderer body=null;int bone=-1;
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(skin.name!="body-mesh")continue;
                for(int i=0;i<skin.bones.Length;i++)if(skin.bones[i]!=null&&skin.bones[i].name=="torso"){body=skin;bone=i;break;}
                if(body!=null)break;
            }
            if(body==null)return null;
            var bounds=TorsoBounds(body,bone);
            var root=new GameObject("DanteStoneWard");root.transform.SetParent(body.bones[bone],false);root.transform.localPosition=bounds.center;
            var ward=root.AddComponent<DanteCarapaceVisual>();ward._owner=owner;ward._worldRoot=worldRoot;ward._duration=duration;ward._heavy=heavy;
            if(front==0)front=Mathf.Sign(Vector3.Dot(body.bones[bone].forward,worldRoot.forward));
            ward._front=front==0?1:front;ward.Build(bounds.extents,ward._front);ward._badge=DanteWardBadge.Create(worldRoot,owner);
            if(owner!=null)owner.StatusRefused+=ward.OnRefused;
            ward.StepTo(0);return ward;
        }

        private static Bounds TorsoBounds(SkinnedMeshRenderer body,int bone)
        {
            var mesh=body.sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;var weights=mesh.boneWeights;
            var bind=mesh.bindposes[bone];bool found=false,coreFound=false;var all=new Bounds();var core=new Bounds();
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];float weight=(w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)
                    +(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
                if(weight<.99f)continue;
                var point=bind.MultiplyPoint3x4(vertices[i]);
                if(found)all.Encapsulate(point);else{all=new Bounds(point,Vector3.zero);found=true;}
                int slot=uv.Length==vertices.Length?Mathf.FloorToInt(uv[i].x*16)/2+(Mathf.FloorToInt(uv[i].y*16)<=3?8:0):-1;
                // Dante's core leather, excluding the high collar and rear cape.
                if(slot!=3)continue;
                if(coreFound)core.Encapsulate(point);else{core=new Bounds(point,Vector3.zero);coreFound=true;}
            }
            return coreFound?core:all;
        }

        private void Build(Vector3 half,float front)
        {
            float depth=half.z+.018f;
            AddPlate("Keystone",0,false,new Vector3(0,half.y*.10f,front*depth),Vector3.forward*front,half.x*.56f,half.y*.82f);
            foreach(float side in new[]{-1f,1f})
                AddPlate("BreastplateWing",1,side<0,new Vector3(side*half.x*.75f,half.y*.08f,front*depth*.96f),
                    new Vector3(side*.22f,0,front).normalized,half.x*.50f,half.y*.89f);
            AddPlate("BackRidge",2,false,new Vector3(0,0,-front*depth),Vector3.back*front,half.x*.94f,half.y*.91f);
            foreach(float side in new[]{-1f,1f})
            {
                AddPlate("Flank",1,side<0,new Vector3(side*(half.x+.006f),-half.y*.05f,0),Vector3.right*side,half.z*.93f,half.y*.80f);
                AddPlate("HookedShoulder",3,side<0,new Vector3(side*half.x*1.02f,half.y*.22f,front*depth*.55f),
                    new Vector3(side*.7f,0,front).normalized,half.x*.37f,half.y*.62f);
            }
            _orbitRoot=new GameObject("DanteOrbitingWard");_orbitRoot.transform.SetParent(_worldRoot,false);
            float chestHeight=_worldRoot.InverseTransformPoint(transform.position).y;
            _orbitRoot.transform.localPosition=Vector3.up*Mathf.Clamp(chestHeight,.45f,.65f);
            for(int i=0;i<3;i++)
                _orbiting.Add(CreatePlate(_orbitRoot.transform,"OrbitingStoneProtector",4,i==1,
                    Vector3.zero,Vector3.forward,_heavy ? .22f:.20f,_heavy ? .29f:.26f,_heavy ? .065f:.048f));
            var all=new List<Renderer>(GetComponentsInChildren<Renderer>());
            all.AddRange(_orbitRoot.GetComponentsInChildren<Renderer>());
            BuildGroundRing();all.AddRange(_ground.GetComponentsInChildren<Renderer>());
            _renderers=all.ToArray();
            // Lowest plate locks first: rank by each plate's rest height on the torso.
            _rank=new int[_plates.Count];_flashAt=new float[_plates.Count];
            var order=new List<int>();for(int i=0;i<_plates.Count;i++){order.Add(i);_flashAt[i]=-99;}
            order.Sort((a,b)=>_plates[a].Position.y.CompareTo(_plates[b].Position.y));
            for(int r=0;r<order.Count;r++)_rank[order[r]]=r;
        }

        // The tell: seven cracks run out from under his feet, typed (angle deg, length m, half width, one kink).
        private static readonly (float angle,float length,float width,float kink)[] GroundCracks=
        {
            (8,.62f,.020f,.18f),(57,.44f,.016f,-.12f),(103,.70f,.022f,.10f),(151,.38f,.014f,-.20f),
            (204,.58f,.018f,.14f),(262,.66f,.021f,-.08f),(311,.48f,.015f,.16f),
        };
        private void BuildGroundRing()
        {
            _ground=new GameObject("UnstoppableGroundRing");_ground.transform.SetParent(_worldRoot,false);
            var tris=new List<Vector3>();
            foreach(var c in GroundCracks)
            {
                float a=c.angle*Mathf.Deg2Rad;var dir=new Vector2(Mathf.Sin(a),Mathf.Cos(a));var side=new Vector2(-dir.y,dir.x);
                var p0=dir*.16f;var p1=dir*(.16f+c.length*.5f)+side*c.kink*.5f;var p2=dir*(.16f+c.length);
                void Seg(Vector2 a0,Vector2 a1,float w0,float w1)
                {
                    var d=(a1-a0).normalized;var sd=new Vector2(-d.y,d.x);
                    var q0=new Vector3(a0.x-sd.x*w0,0,a0.y-sd.y*w0);var q1=new Vector3(a1.x-sd.x*w1,0,a1.y-sd.y*w1);
                    var q2=new Vector3(a1.x+sd.x*w1,0,a1.y+sd.y*w1);var q3=new Vector3(a0.x+sd.x*w0,0,a0.y+sd.y*w0);
                    Face(tris,q0,q1,q2,Vector3.up);Face(tris,q0,q2,q3,Vector3.up);
                }
                Seg(p0,p1,c.width,c.width*.8f);Seg(p1,p2,c.width*.8f,.002f);
            }
            var seam=VfxShapes.Stand(_ground.transform,"MoltenCourtCrack",MeshFrom(tris,"UnstoppableCourtCracks"),1);
            VfxMaterial.Ghost(seam.GetComponent<Renderer>(),new Color(1,.43f,.08f,.92f),.55f);
            _groundSeam=seam.GetComponent<Renderer>().sharedMaterial;
        }

        private void OnRefused(CharacterMotor who,StatusKind kind)
        {
            if(_plates.Count==0||!_visible)return;
            // The plate on the side the status's colour family maps to: a fixed choice per kind, so every frame agrees.
            int i=((int)kind*5+3)%_plates.Count;var plate=_plates[i];if(plate.Node==null)return;
            _flashAt[i]=_recordedAge;
            DanteStatusShatter.Refusal(plate.Node.position,plate.Node.TransformDirection(Vector3.up),kind);
        }

        private void AddPlate(string name,int profile,bool mirror,Vector3 at,Vector3 normal,float spanX,float spanZ)
            =>_plates.Add(CreatePlate(transform,name,profile,mirror,at,normal,spanX,spanZ,_heavy ? .026f:.018f));

        private Plate CreatePlate(Transform parent,string name,int profile,bool mirror,Vector3 at,Vector3 normal,float spanX,float spanZ,float thickness)
        {
            MakeStone(profile,mirror,name,out var stoneMesh,out var seamMesh);
            var stone=VfxShapes.Stand(parent,name,stoneMesh,1);
            stone.transform.localPosition=at;
            stone.transform.localRotation=Quaternion.LookRotation(Vector3.up,normal);
            stone.transform.localScale=new Vector3(spanX,thickness/.30f,spanZ);
            var renderer=stone.GetComponent<Renderer>();
            MaterialKit.Dress(renderer,_heavy?new Color(.13f,.16f,.16f):new Color(.22f,.25f,.245f));
            ToonSkin.Apply(renderer,.004f);VfxRenderTag.Attach(stone);
            Material seamMaterial=null;
            if(seamMesh.vertexCount>0)
            {
                var seam=VfxShapes.Stand(stone.transform,"MoltenCrack",seamMesh,1);
                VfxMaterial.Ghost(seam.GetComponent<Renderer>(),new Color(1,.43f,.08f,.92f),.50f);
                seamMaterial=seam.GetComponent<Renderer>().sharedMaterial;
            }
            else Destroy(seamMesh);
            return new Plate(stone.transform,normal,seamMaterial);
        }

        private static readonly Vector2[][] Profiles=
        {
            Polygon(-.50f,-.65f,0,-1,.55f,-.60f,1,.20f,.62f,.75f,0,.90f,-.80f,.60f,-1,.05f),
            Polygon(-1,-.85f,.45f,-1,1,-.25f,.55f,.38f,.90f,1,.18f,.78f,-.25f,.42f,-.90f,.30f),
            Polygon(-1,-.60f,-.65f,-1,.65f,-1,1,-.55f,.90f,.65f,.30f,1,-.30f,1,-.90f,.65f),
            Polygon(-.50f,-1,.40f,-.70f,.80f,.20f,.40f,.80f,1,1,0,.90f,-.75f,.30f,-1,-.30f),
            Polygon(-.65f,-.72f,0,-1,.62f,-.70f,1,.12f,.58f,.86f,.05f,1,-.70f,.72f,-1,.10f)
        };
        private static Vector2[] Polygon(params float[] xy)
        {
            var points=new Vector2[xy.Length/2];for(int i=0;i<points.Length;i++)points[i]=new Vector2(xy[i*2],xy[i*2+1]);return points;
        }

        private static void MakeStone(int profile,bool mirror,string role,out Mesh stone,out Mesh seam)
        {
            var shape=Profiles[profile];var cap=new Vector2[shape.Length];var facets=new List<Vector3>();
            float sign=mirror?-1:1;
            for(int i=0;i<shape.Length;i++)cap[i]=new Vector2(shape[i].x*sign,shape[i].y)*.82f;
            for(int i=0;i<shape.Length;i++)
            {
                int j=(i+1)%shape.Length;
                var a=new Vector3(shape[i].x*sign,0,shape[i].y);var b=new Vector3(shape[j].x*sign,0,shape[j].y);
                var c=new Vector3(cap[i].x,.18f,cap[i].y);var d=new Vector3(cap[j].x,.18f,cap[j].y);
                var outward=new Vector3(a.x+b.x,0,a.z+b.z).normalized;
                Face(facets,a,b,d,outward);Face(facets,a,d,c,outward);
                Face(facets,new Vector3(0,.30f,0),c,d,Vector3.up);
                if(profile==4)Face(facets,Vector3.zero,b,a,Vector3.down);
            }
            stone=MeshFrom(facets,"CarvedWardStone");
            var lines=new List<Vector3>();
            if(profile==4)
            {
                // Owner-approved orbiting shields: retain their exact drawing.
                var crack=Polygon(-.06f,-.60f,.08f,-.32f,-.04f,-.03f,.10f,.22f,-.10f,.57f);
                for(int i=0;i<crack.Length;i++)crack[i].x*=sign;
                for(int i=0;i<crack.Length-1;i++)Stroke(lines,crack[i],crack[i+1],.031f,cap);
                Stroke(lines,crack[2],new Vector2(-.34f*sign,.18f),.021f,cap);
            }
            else FittedFractures(lines,cap,role,mirror);
            seam=MeshFrom(lines,"WardSurfaceFracture");
        }

        private static void FittedFractures(List<Vector3> lines,Vector2[] cap,string role,bool left)
        {
            void Trace(float width,params float[] xy)
            {
                var path=Polygon(xy);
                for(int i=0;i<path.Length-1;i++)Stroke(lines,path[i],path[i+1],width,cap);
            }
            switch(role)
            {
                case "Keystone":
                    // One load-bearing diagonal fracture, offset from the centre.
                    Trace(.030f,.36f,.60f,.10f,.34f,.17f,.07f,-.18f,-.16f,-.07f,-.46f);
                    Trace(.015f,.35f,-.12f,.24f,-.29f);
                    break;
                case "BreastplateWing" when left:
                    Trace(.023f,-.54f,.23f,-.22f,.10f,.08f,.20f,.34f,-.04f);
                    Trace(.013f,-.24f,-.36f,-.06f,-.52f);
                    break;
                case "BreastplateWing":
                    Trace(.024f,.42f,-.40f,.25f,-.12f,.38f,.10f);
                    Trace(.013f,-.43f,.23f,-.31f,.37f);
                    break;
                case "BackRidge":
                    Trace(.022f,-.62f,.09f,-.27f,.03f,.03f,.14f,.24f,.06f,.56f,.18f);
                    Trace(.014f,-.43f,.51f,-.33f,.29f);
                    break;
                case "Flank" when left:
                    Trace(.015f,0,.54f,.12f,.28f);
                    Trace(.012f,.20f,-.35f,.05f,-.55f);
                    break;
                case "Flank":
                    Trace(.017f,-.15f,-.35f,.08f,-.20f,.15f,.07f);
                    break;
                // The shoulder hooks have strong silhouettes already. Leave
                // their broad planes clean instead of stamping another symbol.
            }
        }

        private static void Stroke(List<Vector3> lines,Vector2 a,Vector2 b,float halfWidth,Vector2[] cap)
        {
            var direction=(b-a).normalized;var side=new Vector2(-direction.y,direction.x)*halfWidth;
            Vector3 At(Vector2 p)=>new Vector3(p.x,CapHeight(p,cap)+.018f,p.y);
            Face(lines,At(a-side),At(b-side),At(b+side),Vector3.up);
            Face(lines,At(a-side),At(b+side),At(a+side),Vector3.up);
        }
        private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        private static float CapHeight(Vector2 point,Vector2[] cap)
        {
            for(int i=0;i<cap.Length;i++)
            {
                var b=cap[i];var c=cap[(i+1)%cap.Length];float area=Cross(b,c);
                if(Mathf.Abs(area)<.00001f)continue;
                float u=Cross(point,c)/area,v=Cross(b,point)/area;
                if(u>=-.001f&&v>=-.001f&&u+v<=1.001f)return .30f-.12f*(u+v);
            }
            return .18f;
        }
        private static void Face(List<Vector3> triangles,Vector3 a,Vector3 b,Vector3 c,Vector3 normal)
        {
            triangles.Add(a);
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)>=0){triangles.Add(b);triangles.Add(c);}
            else{triangles.Add(c);triangles.Add(b);}
        }
        private static Mesh MeshFrom(List<Vector3> vertices,string name)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);var indices=new int[vertices.Count];
            for(int i=0;i<indices.Length;i++)indices[i]=i;
            mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        public void StepTo(float seconds)
        {
            _recordedAge=seconds;
            if(_badge!=null)_badge.StepTo(seconds,LifeSeconds);
            _visible=seconds>=0&&seconds<LifeSeconds;
            float life=LifeSeconds;
            float warn=Mathf.Clamp01((seconds-(life-WarnSeconds))/WarnSeconds);
            float fall=Mathf.Clamp01((seconds-(life-CrumbleSeconds))/CrumbleSeconds);
            float ground=Slipper.GroundY(transform.position);
            float above=Mathf.Max(0,transform.position.y-ground);
            Color hot=new Color(1,.35f,.04f),cooled=new Color(.55f,.16f,.06f)*.55f;
            Color seamColour=Color.Lerp(hot,cooled,warn);
            for(int i=0;i<_plates.Count;i++)
            {
                var plate=_plates[i];if(plate.Node==null)continue;
                int rank=_rank!=null&&i<_rank.Length?_rank[i]:i;
                // ARMOUR: from the court up to its place in one jolt, lowest plate first.
                float lockAt=AssembleFrom+(rank+1)*AssembleStep;
                float jolt=Mathf.Clamp01((seconds-(lockAt-AssembleStep))/AssembleStep);
                float overshoot=jolt>=1?Mathf.Max(0,1-(seconds-lockAt)/.08f)*.012f:0;
                float drop=(1-jolt*jolt)*above+fall*fall*above+fall*.12f;
                var down=transform.InverseTransformVector(Vector3.down*drop);
                float tremble=warn*(1-fall)*1.8f;
                plate.Node.localPosition=plate.Position+down+plate.Normal*overshoot;
                plate.Node.localRotation=plate.Rotation*Quaternion.Euler((1-jolt)*30+fall*48*(i%2==0?1:-1)+Mathf.Sin(seconds*47f+i*1.7f)*tremble,
                    0,(1-jolt)*(i%2==0?18:-18)+Mathf.Sin(seconds*53f+i)*tremble);
                // DISSIPATE: it breaks smaller in two steps as it lands, and goes into the court.
                float broken=fall<.55f?1:fall<.8f?.72f:Mathf.Lerp(.72f,0,(fall-.8f)/.2f);
                plate.Node.localScale=plate.Scale*Mathf.Lerp(.55f,1,jolt)*broken;
                if(plate.Seam!=null)
                {
                    float lit=jolt>=1?1:0;
                    float flare=jolt>=1?Mathf.Max(0,1-(seconds-lockAt)/.2f)*.8f:0;
                    float refused=_flashAt!=null?Mathf.Max(0,1-(seconds-_flashAt[i])/.25f)*1.6f:0;
                    plate.Seam.SetColor("_EmissionColor",seamColour*
                        (((.42f+.10f*Mathf.Sin(seconds*4.2f+i*.6f))*(1-fall)+flare+refused)*lit));
                }
            }
            // The shields form last, slow in the warning, and drop with the plates.
            float opening=Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-OrbitFrom+.06f)/.30f));
            float closing=Mathf.SmoothStep(0,1,Mathf.Clamp01((life-seconds)/.28f));
            float speed=_heavy ? .85f:1.05f;
            float late=Mathf.Max(0,seconds-(life-WarnSeconds));
            float phase=speed*seconds-speed*.125f*late*late;
            float orbitHeight=_orbitRoot!=null?_orbitRoot.transform.localPosition.y:.55f;
            for(int i=0;i<_orbiting.Count;i++)
            {
                var plate=_orbiting[i];if(plate.Node==null)continue;
                float angle=i*Mathf.PI*2/3+.35f+phase;
                var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float radius=Mathf.Lerp(.30f,_heavy ? .80f:.73f,opening*closing);
                plate.Node.localPosition=outward*radius+Vector3.up*(Mathf.Sin(angle*1.4f)*.055f-(1-closing)*.13f-fall*fall*orbitHeight);
                plate.Node.localRotation=Quaternion.LookRotation(Vector3.up,outward)
                    *Quaternion.Euler((1-opening)*35+(1-closing)*65,0,Mathf.Sin(angle)*8);
                plate.Node.localScale=plate.Scale*Mathf.Lerp(.45f,1,opening)*Mathf.Lerp(.70f,1,closing);
                if(plate.Seam!=null)plate.Seam.SetColor("_EmissionColor",Color.Lerp(new Color(1,.40f,.055f),cooled,warn)*(.70f+.12f*Mathf.Sin(angle*2))*closing);
            }
            // TELL: the court cracks under his feet as he hunches, stays lit through the armour, then cools and closes.
            if(_ground!=null)
            {
                float run=Mathf.Clamp01(seconds/.15f),cool=Mathf.Clamp01((seconds-.7f)/.5f);
                _ground.SetActive(seconds<1.25f&&_visible);
                var feet=_worldRoot.InverseTransformPoint(new Vector3(_worldRoot.position.x,ground+.012f,_worldRoot.position.z));
                _ground.transform.localPosition=feet;
                _ground.transform.localScale=new Vector3(Mathf.Lerp(.2f,1,run),1,Mathf.Lerp(.2f,1,run))*(1-cool*cool);
                if(_groundSeam!=null)_groundSeam.SetColor("_EmissionColor",Color.Lerp(new Color(1,.42f,.06f),new Color(.55f,.16f,.06f)*.4f,cool)*.9f);
            }
        }

        private void OnEnable()=>Camera.onPreCull+=BeforeCamera;
        private void OnDisable()=>Camera.onPreCull-=BeforeCamera;
        private void OnDestroy()
        {
            if(_owner!=null)_owner.StatusRefused-=OnRefused;
            if(_orbitRoot!=null)Destroy(_orbitRoot);if(_ground!=null)Destroy(_ground);if(_badge!=null)Destroy(_badge.gameObject);
        }
        private void BeforeCamera(Camera camera)
        {
            if(_owner==null||_renderers==null)return;
            var rig=camera.GetComponent<CameraRig>();
            bool hidden=rig!=null&&rig.IsLocalFpp&&rig.IsFollowing(_owner);
            foreach(var renderer in _renderers)if(renderer!=null)renderer.enabled=_visible&&!hidden;
        }
    }
}
