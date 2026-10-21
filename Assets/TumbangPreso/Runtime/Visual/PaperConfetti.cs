using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Celebration only. Paper has a fold, flutter and terminal fall, without
    // collision bodies or draws from gameplay's random stream.
    public sealed class PaperConfetti : MonoBehaviour
    {
        public const float Life=2.8f;
        private struct Paper {public Transform Body;public Renderer Renderer;public Vector3 Origin,Velocity;public float Phase;}
        private Paper[] _papers;
        private float _age;
        private static Mesh _fold;
        private static readonly Color[] Colours={new Color(1,.73f,.18f),new Color(.96f,.42f,.32f),new Color(.38f,.72f,.80f),new Color(.93f,.84f,.62f)};
        public int PaperCount=>_papers?.Length??0;
        public static PaperConfetti Play(Vector3 centre,int count=24)
        {
            if(WorldCueProfile.Current.InkEffects<=0)return null;
            bool reduced=Settings.SettingsStore.Current.ReducedEffects;
            count=Mathf.Clamp(count,0,reduced?8:48);
            if(count==0)return null;
            var go=new GameObject("Paper celebration");var owner=go.AddComponent<PaperConfetti>();
            VfxRenderTag.Attach(go);owner._papers=new Paper[count];
            var camera=Camera.main;
            for(int i=0;i<count;i++)
            {
                float angle=i*2.399963f,phase=i*1.71f;
                var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                Vector3 start=centre+radial*(.8f+(i%3)*.16f)+Vector3.up*(1.1f+(i%4)*.12f);
                if(camera!=null)
                {
                    Vector3 view=camera.WorldToViewportPoint(start);
                    // The two banks leave the central action uncovered at spawn.
                    if(view.z>camera.nearClipPlane && Mathf.Abs(view.x-.5f)<.18f && Mathf.Abs(view.y-.5f)<.22f)
                    {view.x=i%2==0?.29f:.71f;start=camera.ViewportToWorldPoint(view);}
                }
                var paper=new GameObject("Folded paper");paper.transform.SetParent(go.transform,false);
                paper.AddComponent<MeshFilter>().sharedMesh=Fold;
                var renderer=paper.AddComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                ConfettiPaperMaterial.Apply(renderer,Colours[i%Colours.Length]);
                paper.transform.localScale=new Vector3(.10f+(i%3)*.012f,.065f,.10f);
                owner._papers[i]=new Paper{Body=paper.transform,Renderer=renderer,Origin=start,Velocity=radial*(.35f+(i%3)*.12f)+Vector3.up*(2.6f+(i%4)*.25f),Phase=phase};
            }
            owner.Sample(0);return owner;
        }
        private static Mesh Fold
        {
            get
            {
                if(_fold!=null)return _fold;
                _fold=new Mesh{name="Folded paper sheet",hideFlags=HideFlags.DontSave};
                _fold.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(0,-.5f,.14f),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(0,.5f,.14f),new Vector3(.5f,.5f,0)};
                _fold.uv=new[]{Vector2.zero,new Vector2(.5f,0),Vector2.right,Vector2.up,new Vector2(.5f,1),Vector2.one};
                _fold.triangles=new[]{0,3,1,1,3,4,1,4,2,2,4,5};_fold.RecalculateBounds();return _fold;
            }
        }
        public void Sample(float age)
        {
            float t=Mathf.Clamp(age,0,Life),tail=Mathf.Clamp01((Life-t)/.45f);
            bool calm=Settings.SettingsStore.Current.ReducedUiMotion;
            foreach(var paper in _papers)
            {
                if(paper.Body==null)continue;
                float flutter=calm?0:(Mathf.Sin(t*9+paper.Phase)-Mathf.Sin(paper.Phase))*.12f;
                // Exponential rise drag settles into a slow paper descent.
                paper.Body.position=paper.Origin+paper.Velocity*((1-Mathf.Exp(-2*t))*.5f)+Vector3.down*(.65f*t)+new Vector3(flutter,0,flutter*.4f);
                paper.Body.rotation=Quaternion.Euler(calm?25:25+Mathf.Sin(t*8+paper.Phase)*65,paper.Phase*Mathf.Rad2Deg+t*(calm?15:90),paper.Phase*23);
                // Material fade is shared across this shower through a property block.
                ConfettiPaperMaterial.SetOpacity(paper.Renderer,tail*WorldCueProfile.Current.InkEffects);
            }
        }
        private void Update(){_age+=Time.deltaTime;if(_age>=Life){Destroy(gameObject);return;}Sample(_age);}
    }
}
