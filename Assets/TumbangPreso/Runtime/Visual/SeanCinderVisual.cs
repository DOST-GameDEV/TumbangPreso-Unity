using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Pure field-age presentation shared by live observers and recorded views.
    public sealed class SeanCinderVisual : MonoBehaviour, IVfxTimeline
    {
        private WorldEffectSnapshot.Field _state;
        public float LifeSeconds => _state.Duration;
        private Transform _ink, _ember;
        private readonly Transform[] _flares = new Transform[3];
        private readonly Renderer[] _surfaces = new Renderer[5];
        private readonly Color[] _colours = new Color[5];
        private readonly Vector3[] _flareRest = new Vector3[3];
        private MaterialPropertyBlock _block;
        private Mesh _seamMesh;
        public static SeanCinderVisual Build(Transform parent, WorldEffectSnapshot.Field state)
        {
            var go = new GameObject("Cinder warning and pressure seam"); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(state.Position + Vector3.up * .014f, Quaternion.LookRotation(state.Forward));
            var visual = go.AddComponent<SeanCinderVisual>(); visual._state = state; visual._block = new MaterialPropertyBlock();
            visual._seamMesh = new Mesh { name = "Cinder flat seam" };
            // A continuous burned stroke, with hand-shaped shoulders and tapered ends.
            // Its exact three-metre boundary stays readable without a rectangular debug-line look.
            visual._seamMesh.vertices = new[] {
                new Vector3(-1,0,-.42f),new Vector3(-1,0,.42f),
                new Vector3(-.73f,0,-.76f),new Vector3(-.73f,0,.96f),
                new Vector3(-.25f,0,-1.0f),new Vector3(-.25f,0,.76f),
                new Vector3(.16f,0,-.49f),new Vector3(.16f,0,.75f),
                new Vector3(.69f,0,-.98f),new Vector3(.69f,0,.84f),
                new Vector3(1,0,-.35f),new Vector3(1,0,.35f) };
            visual._seamMesh.triangles = new[] { 0,1,3,0,3,2, 2,3,5,2,5,4,
                4,5,7,4,7,6, 6,7,9,6,9,8, 8,9,11,8,11,10 }; visual._seamMesh.RecalculateNormals(); visual._seamMesh.RecalculateBounds();
            visual._ink = visual.Seam("Charcoal footprint", 0, .10f, new Color(.12f,.055f,.025f,.84f), 0);
            visual._ember = visual.Seam("Live ember seam", 1, .026f, new Color(1,.34f,.025f,.85f), .22f);
            visual._ember.localPosition = Vector3.up * .003f;
            visual.Flare(0,-1.05f,.20f,-.18f,.08f);
            visual.Flare(1,.12f,.32f,.25f,.11f);
            visual.Flare(2,1.12f,.24f,-.12f,.085f);
            visual.StepTo(state.Duration-state.Remaining); return visual;
        }
        private Transform Seam(string name,int index,float width,Color colour,float emission)
        {
            var go = new GameObject(name); go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=_seamMesh;
            var renderer=go.AddComponent<MeshRenderer>(); renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false; VfxMaterial.Ghost(renderer,colour,emission);
            _surfaces[index]=renderer;_colours[index]=colour;
            go.transform.localScale=new Vector3(SeanGateRules.HalfWidth,1,width);return go.transform;
        }
        private void Flare(int index,float x,float height,float lean,float width)
        {
            // Tongue's base radius is .28, so normalize it before applying the
            // intended small world radius; multiplying twice made the flames needle-thin.
            var go=VfxShapes.Stand(transform,"Cinder pressure tooth "+index,
                VfxShapes.Tongue(4,.28f,lean,.5f,.14f,751+index),width/.28f,height,yaw:index==1?17:-11);
            go.transform.localPosition=new Vector3(x,0,0);
            var renderer=go.GetComponent<Renderer>();var colour=new Color(1,index==1?.48f:.29f,.025f,.88f);
            VfxMaterial.Ghost(renderer,colour,.3f);_surfaces[index+2]=renderer;_colours[index+2]=colour;
            _flares[index]=go.transform;_flareRest[index]=go.transform.localScale;
        }
        public void SetState(WorldEffectSnapshot.Field state){_state=state;}
        public void StepTo(float seconds)
        {
            float age=Mathf.Max(0,seconds);
            float draw=Mathf.Clamp01(age/SeanGateRules.WarningSeconds);
            float ending=Mathf.Clamp01((_state.Duration-age)/.2f);
            float consumed=_state.Split?Mathf.Clamp01((age-_state.FirstScale)/.18f):0;
            float weight=ending*(1-consumed);
            bool armed=age>=SeanGateRules.WarningSeconds;
            _ink.localScale=new Vector3(SeanGateRules.HalfWidth*draw,1,.10f);
            _ember.localScale=new Vector3(SeanGateRules.HalfWidth*draw,1,armed?.037f:.026f);
            for(int i=0;i<_flares.Length;i++)
            {
                float rise=armed?Mathf.Clamp01((age-SeanGateRules.WarningSeconds)/.07f):0;
                // Sample only field age so live, late join and replay agree exactly.
                float phase=i==0?0:i==1?1.4f:3.1f;
                float breath=.94f+.06f*Mathf.Sin(age*9+phase);
                float kick=_state.Split?1+.45f*Mathf.Sin(consumed*Mathf.PI):1;
                _flares[i].localScale=Vector3.Scale(_flareRest[i],new Vector3(1,rise*weight*breath*kick,1));
                _flares[i].localRotation=Quaternion.Euler(_state.Split?-_state.SecondScale*consumed*65:0,i==1?17:-11,0);
            }
            for(int i=0;i<_surfaces.Length;i++)
            {
                var colour=_colours[i];colour.a*=weight*(i==1&&!armed?.65f:1);
                _surfaces[i].GetPropertyBlock(_block);_block.SetColor("_Color",colour);_block.SetColor("_BaseColor",colour);
                _surfaces[i].SetPropertyBlock(_block);
            }
        }
        private void OnDestroy(){if(_seamMesh!=null)Destroy(_seamMesh);}
    }
}
