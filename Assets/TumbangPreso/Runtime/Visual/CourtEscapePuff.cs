using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // V2: a small chalk puff at the crossing is the escape beat. It is neither a
    // hit starburst nor a score message. Its world cue also supplies replay timing.
    public sealed class CourtEscapePuff : MonoBehaviour
    {
        public const float Life = .42f;
        private Mesh _mesh;
        private readonly MeshRenderer[] _faces = new MeshRenderer[2];
        private MaterialPropertyBlock _block;
        private float _age, _weight;
        private Vector3 _origin, _side;
        private bool _recorded;
        public static CourtEscapePuff Play(Vector3 at,float weight,Transform recordedParent=null)
        {
            var go=new GameObject("CourtEscapeChalk");
            if(recordedParent!=null)go.transform.SetParent(recordedParent,false);
            var puff=go.AddComponent<CourtEscapePuff>();puff._origin=at;puff._weight=weight;
            puff._side=Mathf.Abs(at.x)>=Mathf.Abs(at.z)?Vector3.forward:Vector3.right;
            puff._block=new MaterialPropertyBlock();
            puff._recorded=recordedParent!=null;
            puff._mesh=new Mesh {name="Escape chalk puff"};
            puff._mesh.vertices=new[]{new Vector3(-.5f,-.25f,0),new Vector3(.5f,-.25f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            puff._mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            puff._mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            puff._mesh.triangles=new[]{0,1,2,0,2,3};puff._mesh.RecalculateBounds();
            puff._mesh.bounds=new Bounds(Vector3.zero,Vector3.one*1.2f);
            var material=new Material(Shader.Find("TumbangPreso/CourtSignal")){name="Escape chalk"};material.SetFloat("_Puff",1);
            for(int i=0;i<2;i++)
            {
                var face=new GameObject("Heel chalk "+i);face.transform.SetParent(go.transform,false);
                face.AddComponent<MeshFilter>().sharedMesh=puff._mesh;
                var renderer=face.AddComponent<MeshRenderer>();puff._faces[i]=renderer;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sharedMaterial=material;
            }
            VfxRenderTag.Own(go,material);
            puff.Sample(0);return puff;
        }
        private void Update()
        {
            if(_recorded)return;
            _age+=Time.deltaTime;
            if(_age>=Life || GameServices.Round==null || !GameServices.Round.RoundActive){Destroy(gameObject);return;}
            Sample(_age);
        }
        public void Sample(float age)
        {
            float u=Mathf.Clamp01(age/Life);
            transform.position=_origin;
            var profile=WorldCueProfile.Current;
            _block.SetColor("_Chalk",profile.Chalk);_block.SetColor("_Ink",profile.Ink);
            _block.SetFloat("_Weight",Mathf.Clamp01(_weight)*(1-u)*(1-u)*(Settings.SettingsStore.Current.ReducedEffects?.65f:1));
            for(int i=0;i<_faces.Length;i++)
            {
                var face=_faces[i];face.transform.position=_origin+_side*((i==0?-1:1)*(.5f+.12f*u))+Vector3.up*(.08f+u*.15f);
                face.transform.localScale=Vector3.one*Mathf.Lerp(.26f,.48f,u);face.SetPropertyBlock(_block);
            }
        }
        public void ShowForCapture(bool visible){foreach(var face in _faces)if(face!=null)face.forceRenderingOff=!visible;}
        private void OnDestroy(){if(_mesh!=null)Destroy(_mesh);}
    }
}
