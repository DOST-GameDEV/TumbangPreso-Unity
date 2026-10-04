using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>One authored patch of the five-part earthquake, sampled only by its accepted age.</summary>
    public sealed class DanteDriftFault : MonoBehaviour, IVfxTimeline
    {
        sealed class Ink
        {
            public Mesh Mesh;
            public Color[] Colours;
            public float[] Birth;
            public bool Hot;
        }
        readonly List<Ink> _ink = new List<Ink>(2);
        readonly List<Stone> _stones = new List<Stone>(4);
        readonly struct Stone
        {
            public readonly Transform Node;
            public readonly Vector3 At, Scale;
            public readonly Quaternion Rotation;
            public readonly float Delay, Lift, Lean;
            public Stone(Transform node, float delay, float lift, float lean)
            { Node=node; At=node.localPosition; Scale=node.localScale; Rotation=node.localRotation; Delay=delay; Lift=lift; Lean=lean; }
        }
        public float LifeSeconds => GeoRules.DriftVisualTail;

        // X is a fraction of the accepted half-width; Y is forward within this band.
        // The centre fault joins the neighbouring patch. Forks are deliberately unequal.
        static Vector2 P(float x,float z) => new Vector2(x,z);
        static Vector2[][] Pattern(int band)
        {
            switch(band)
            {
                case 0: return new[] {
                    new[]{P(0,0),P(-.045f,.22f),P(.065f,.49f),P(-.025f,.76f),P(.035f,1)},
                    new[]{P(-.045f,.22f),P(-.21f,.31f),P(-.47f,.27f),P(-.72f,.43f)},
                    new[]{P(.065f,.49f),P(.28f,.39f),P(.57f,.54f),P(.90f,.47f)},
                    new[]{P(-.025f,.76f),P(-.25f,.67f),P(-.61f,.88f)},
                    new[]{P(-.47f,.27f),P(-.58f,.16f),P(-.80f,.20f)},
                    new[]{P(.57f,.54f),P(.68f,.72f),P(.82f,.79f)} };
                case 1: return new[] {
                    new[]{P(.035f,0),P(.105f,.25f),P(-.075f,.56f),P(.04f,.82f),P(-.045f,1)},
                    new[]{P(.105f,.25f),P(.32f,.14f),P(.50f,.27f),P(.77f,.19f)},
                    new[]{P(-.075f,.56f),P(-.28f,.41f),P(-.58f,.50f),P(-.91f,.36f)},
                    new[]{P(.04f,.82f),P(.23f,.66f),P(.58f,.86f),P(.87f,.73f)},
                    new[]{P(-.58f,.50f),P(-.65f,.70f),P(-.81f,.82f)},
                    new[]{P(.50f,.27f),P(.62f,.42f)} };
                case 2: return new[] {
                    new[]{P(-.045f,0),P(-.11f,.29f),P(.025f,.48f),P(.09f,.73f),P(.01f,1)},
                    new[]{P(-.11f,.29f),P(-.29f,.19f),P(-.51f,.32f),P(-.84f,.23f)},
                    new[]{P(.025f,.48f),P(.24f,.34f),P(.44f,.44f),P(.73f,.32f)},
                    new[]{P(.09f,.73f),P(-.17f,.69f),P(-.42f,.84f),P(-.76f,.72f)},
                    new[]{P(.44f,.44f),P(.64f,.60f),P(.91f,.57f)},
                    new[]{P(-.42f,.84f),P(-.54f,.95f)} };
                case 3: return new[] {
                    new[]{P(.01f,0),P(.075f,.18f),P(-.055f,.46f),P(.06f,.69f),P(-.03f,1)},
                    new[]{P(.075f,.18f),P(.27f,.30f),P(.56f,.17f),P(.89f,.30f)},
                    new[]{P(-.055f,.46f),P(-.26f,.32f),P(-.49f,.48f),P(-.79f,.40f)},
                    new[]{P(.06f,.69f),P(.31f,.59f),P(.53f,.76f),P(.82f,.86f)},
                    new[]{P(-.49f,.48f),P(-.63f,.65f),P(-.91f,.59f)},
                    new[]{P(.31f,.59f),P(.39f,.44f)} };
                default: return new[] {
                    new[]{P(-.03f,0),P(-.095f,.24f),P(.055f,.51f),P(-.04f,.78f),P(0,.98f)},
                    new[]{P(-.095f,.24f),P(-.31f,.36f),P(-.54f,.22f),P(-.88f,.33f)},
                    new[]{P(.055f,.51f),P(.29f,.38f),P(.60f,.48f),P(.85f,.35f)},
                    new[]{P(-.04f,.78f),P(-.27f,.66f),P(-.57f,.83f),P(-.79f,.74f)},
                    new[]{P(.60f,.48f),P(.68f,.67f),P(.91f,.79f)},
                    new[]{P(-.27f,.66f),P(-.42f,.53f)} };
            }
        }

        public static DanteDriftFault Build(Transform parent, Vector3 at, Vector3 forward,
            float halfWidth, float depth, int band)
        {
            var root=new GameObject("ContinentalFault"+band);
            root.transform.SetParent(parent,false);
            root.transform.SetPositionAndRotation(VfxShapes.GroundPoint(at),Quaternion.LookRotation(forward));
            var view=root.AddComponent<DanteDriftFault>();
            var paths=Pattern(band);
            view.BuildInk(paths,halfWidth,depth,false);
            view.BuildInk(paths,halfWidth,depth,true);
            // Each patch has its own sparse group. No rotating ring or repeated stone row.
            switch(band)
            {
                case 0:
                    view.Chip(-.22f,.31f,.20f,.12f,.16f,17,.10f,-14,halfWidth,depth);
                    view.Chip(.30f,.40f,.13f,.08f,.24f,-28,.07f,9,halfWidth,depth);
                    view.Chip(-.48f,.81f,.11f,.07f,.13f,39,.05f,-8,halfWidth,depth); break;
                case 1:
                    view.Chip(-.31f,.43f,.25f,.10f,.14f,-12,.09f,12,halfWidth,depth);
                    view.Chip(.52f,.82f,.14f,.08f,.20f,31,.06f,-10,halfWidth,depth);
                    view.Chip(.30f,.18f,.09f,.055f,.11f,8,.04f,7,halfWidth,depth); break;
                case 2:
                    view.Chip(.28f,.37f,.18f,.12f,.21f,24,.11f,-13,halfWidth,depth);
                    view.Chip(-.46f,.80f,.23f,.075f,.12f,-36,.055f,8,halfWidth,depth);
                    view.Chip(.70f,.59f,.08f,.05f,.12f,11,.035f,-6,halfWidth,depth); break;
                case 3:
                    view.Chip(-.29f,.35f,.16f,.095f,.24f,37,.08f,14,halfWidth,depth);
                    view.Chip(.34f,.63f,.25f,.11f,.15f,-18,.09f,-9,halfWidth,depth);
                    view.Chip(-.69f,.62f,.10f,.06f,.14f,5,.045f,6,halfWidth,depth); break;
                default:
                    view.Chip(-.32f,.34f,.22f,.13f,.18f,-23,.10f,-12,halfWidth,depth);
                    view.Chip(.59f,.46f,.13f,.07f,.22f,34,.065f,11,halfWidth,depth);
                    view.Chip(-.55f,.79f,.095f,.05f,.12f,-7,.04f,-7,halfWidth,depth); break;
            }
            view.StepTo(0);return view;
        }

        void BuildInk(Vector2[][] paths,float halfWidth,float depth,bool hot)
        {
            var vertices=new List<Vector3>();var birth=new List<float>();
            for(int pathIndex=0;pathIndex<paths.Length;pathIndex++)
            {
                var path=paths[pathIndex];
                for(int i=0;i<path.Length-1;i++)
                {
                    Vector3 a=new Vector3(path[i].x*halfWidth,0,path[i].y*depth);
                    Vector3 b=new Vector3(path[i+1].x*halfWidth,0,path[i+1].y*depth);
                    float width=hot ? .013f : pathIndex==0 ? .078f : .046f;
                    width=Mathf.Min(width,Mathf.Min(halfWidth,depth)*.12f);
                    Vector3 side=Vector3.Cross(Vector3.up,b-a).normalized*width;
                    Vector3 end=side*(i==path.Length-2&&pathIndex!=0 ? .10f : .72f);
                    foreach(var corner in new[]{a-side,b-end,b+end,a-side,b+end,a+side})
                    {
                        var point=corner;point.x=Mathf.Clamp(point.x,-halfWidth,halfWidth);
                        point.z=Mathf.Clamp(point.z,0,depth);vertices.Add(point);
                        birth.Add(Mathf.Clamp01(point.z/depth)*.085f);
                    }
                }
            }
            var mesh=DanteFissurePillar.MeshFrom(vertices,hot ? "DriftCoolingSeam" : "DriftBrokenBasalt");
            var colours=new Color[vertices.Count];mesh.colors=colours;mesh.MarkDynamic();
            var node=VfxShapes.Lay(transform,mesh.name,mesh,1,0);
            var shader=Resources.Load<Shader>("UI/AimGuide") ?? Shader.Find("TumbangPreso/AimGuide");
            var material=new Material(shader){name="ContinentalDriftInk",renderQueue=hot ? 2992 : 2991};
            node.GetComponent<Renderer>().sharedMaterial=material;VfxRenderTag.Own(node,material);
            VfxShapes.DrapeToGround(node,hot ? .027f : .019f,.6f);
            _ink.Add(new Ink{Mesh=mesh,Colours=colours,Birth=birth.ToArray(),Hot=hot});
        }

        void Chip(float x,float z,float width,float height,float length,float yaw,float lift,float lean,float halfWidth,float depth)
        {
            var chip=VfxShapes.Stand(transform,"LiftedFaultStone",VfxShapes.Prism(5,1,.7f,.18f,.16f,71),1);
            chip.transform.localPosition=new Vector3(x*halfWidth,0,z*depth);
            chip.transform.position=VfxShapes.GroundPoint(chip.transform.position)+Vector3.up*.015f;
            chip.transform.localRotation=Quaternion.Euler(0,yaw,0);
            float footprint=Mathf.Min(halfWidth*.07f,depth*.055f);
            chip.transform.localScale=new Vector3(Mathf.Min(width,footprint),height,Mathf.Min(length,footprint));
            MaterialKit.Dress(chip.GetComponent<Renderer>(),new Color(.25f,.24f,.21f));
            ToonSkin.Apply(chip.GetComponent<Renderer>(),.007f);VfxRenderTag.Attach(chip);
            _stones.Add(new Stone(chip.transform,z*.085f,lift,lean));
        }

        public void StepTo(float seconds)
        {
            float clear=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,LifeSeconds,seconds));
            foreach(var ink in _ink)
            {
                float heat=ink.Hot ? Mathf.Exp(-Mathf.Max(0,seconds-.10f)*7) : 1;
                for(int i=0;i<ink.Colours.Length;i++)
                {
                    Color tint=ink.Hot ? new Color(.94f,.43f,.12f,.78f) : new Color(.024f,.021f,.017f,.88f);
                    tint.a*=Mathf.Clamp01((seconds-ink.Birth[i])/.025f)*clear*heat;
                    ink.Colours[i]=tint;
                }
                ink.Mesh.colors=ink.Colours;
            }
            foreach(var stone in _stones)
            {
                float age=seconds-stone.Delay;
                float rise=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.09f));
                float settle=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.48f,age));
                float visible=rise*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.32f,.60f,age)));
                stone.Node.gameObject.SetActive(age>=0&&age<.60f);
                stone.Node.localPosition=stone.At+Vector3.up*(stone.Lift*rise*settle-(1-visible)*stone.Scale.y);
                stone.Node.localScale=new Vector3(stone.Scale.x,stone.Scale.y*visible,stone.Scale.z);
                stone.Node.localRotation=stone.Rotation*Quaternion.Euler(stone.Lean*rise*settle,0,stone.Lean*.4f*rise*settle);
            }
        }
    }
}
