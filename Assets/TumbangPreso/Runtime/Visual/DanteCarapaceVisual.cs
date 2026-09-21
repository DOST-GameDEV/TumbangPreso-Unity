using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // A fitted stone ward. It follows the actual torso and never fills its
    // wearer's first-person view with the former sphere and orbiting cubes.
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
        private Renderer[] _renderers;
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
            ward._front=front==0?1:front;ward.Build(bounds.extents,ward._front);ward.StepTo(0);return ward;
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
            all.AddRange(_orbitRoot.GetComponentsInChildren<Renderer>());_renderers=all.ToArray();
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
            _visible=seconds>=0&&seconds<LifeSeconds;
            float release=Mathf.SmoothStep(0,1,Mathf.Clamp01((LifeSeconds-seconds)/.18f));
            for(int i=0;i<_plates.Count;i++)
            {
                var plate=_plates[i];if(plate.Node==null)continue;
                float settle=Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-i*.015f)/(_heavy ? .36f : .26f)));
                plate.Node.localPosition=plate.Position+plate.Normal*((1-settle)*.05f+(1-release)*.02f)
                    -Vector3.up*((1-settle)*.16f+(1-release)*.025f);
                plate.Node.localRotation=plate.Rotation*Quaternion.Euler((1-settle)*22,0,(1-settle)*(i%2==0?14:-14));
                plate.Node.localScale=plate.Scale*Mathf.Lerp(.90f,1,release);
                if(plate.Seam!=null)plate.Seam.SetColor("_EmissionColor",new Color(1,.35f,.04f)*
                    ((.42f+.10f*Mathf.Sin(seconds*4.2f+i*.6f))*release));
            }
            float opening=Mathf.SmoothStep(0,1,Mathf.Clamp01(seconds/.36f));
            float closing=Mathf.SmoothStep(0,1,Mathf.Clamp01((LifeSeconds-seconds)/.28f));
            for(int i=0;i<_orbiting.Count;i++)
            {
                var plate=_orbiting[i];if(plate.Node==null)continue;
                float angle=i*Mathf.PI*2/3+.35f+seconds*(_heavy ? .85f:1.05f);
                var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float radius=Mathf.Lerp(.30f,_heavy ? .80f:.73f,opening*closing);
                plate.Node.localPosition=outward*radius+Vector3.up*(Mathf.Sin(angle*1.4f)*.055f-(1-closing)*.13f);
                plate.Node.localRotation=Quaternion.LookRotation(Vector3.up,outward)
                    *Quaternion.Euler((1-opening)*35+(1-closing)*65,0,Mathf.Sin(angle)*8);
                plate.Node.localScale=plate.Scale*Mathf.Lerp(.45f,1,opening)*Mathf.Lerp(.70f,1,closing);
                if(plate.Seam!=null)plate.Seam.SetColor("_EmissionColor",new Color(1,.40f,.055f)*(.70f+.12f*Mathf.Sin(angle*2))*closing);
            }
        }

        private void OnEnable()=>Camera.onPreCull+=BeforeCamera;
        private void OnDisable()=>Camera.onPreCull-=BeforeCamera;
        private void OnDestroy(){if(_orbitRoot!=null)Destroy(_orbitRoot);}
        private void BeforeCamera(Camera camera)
        {
            if(_owner==null||_renderers==null)return;
            var rig=camera.GetComponent<CameraRig>();
            bool hidden=rig!=null&&rig.IsLocalFpp&&rig.IsFollowing(_owner);
            foreach(var renderer in _renderers)if(renderer!=null)renderer.enabled=_visible&&!hidden;
        }
    }
}
