using UnityEngine;
using TumbangPreso.Core;

namespace TumbangPreso.Visual
{
    // V2: blue filling collar = restore work; gold draining collar = THIS can's
    // protection. Close physical diameter, no bubble suggesting area immunity.
    // Observer timing is visual only and can never restore/protect a gameplay can.
    public sealed class LataClockPresentation : MonoBehaviour
    {
        public const int RecordedClockBit=1024;
        private static readonly System.Collections.Generic.Dictionary<Lata,LataClockPresentation> Live=new System.Collections.Generic.Dictionary<Lata,LataClockPresentation>();
        private Lata _lata;private Renderer _ring;private Mesh _mesh;private MaterialPropertyBlock _block;
        private Renderer[] _body;private GameObject _bodyRoot;private MaterialPropertyBlock _bodyBlock;
        private int _bodySkin=int.MinValue;
        private bool _recorded,_snapshot;private float _observedRestore,_observedProtection,_observedAt;
        public float RestoreRatio {get;private set;}
        public float ProtectionRatio {get;private set;}
        public static LataClockPresentation Install(Transform parent,Lata lata,bool recorded=false)
        {
            var go=new GameObject(recorded?"RecordedLataClock":"LataClock");go.transform.SetParent(parent,false);
            var cue=go.AddComponent<LataClockPresentation>();cue._lata=lata;cue._recorded=recorded;cue.Build();
            if(!recorded && lata!=null)Live[lata]=cue;return cue;
        }
        private void Build()
        {
            _block=new MaterialPropertyBlock();_mesh=BuildCollar();
            gameObject.AddComponent<MeshFilter>().sharedMesh=_mesh;_ring=gameObject.AddComponent<MeshRenderer>();
            _ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;_ring.receiveShadows=false;
            var material=new Material(Shader.Find("TumbangPreso/WorldClock")){name="Lata restore clock"};
            _ring.sharedMaterial=material;VfxRenderTag.Own(gameObject,material);_ring.enabled=false;
        }
        private static Mesh BuildCollar()
        {
            const int count=64;var vertices=new Vector3[(count+1)*4];var uv=new Vector2[vertices.Length];
            var triangles=new int[count*12];
            for(int i=0;i<=count;i++)
            {
                float t=i/(float)count,a=t*Mathf.PI*2;var p=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                vertices[i*4]=p*.28f;vertices[i*4+1]=p*.20f;
                vertices[i*4+2]=p*.28f-Vector3.up*.035f;vertices[i*4+3]=p*.28f+Vector3.up*.005f;
                uv[i*4]=new Vector2(t,0);uv[i*4+1]=new Vector2(t,1);
                uv[i*4+2]=new Vector2(t,0);uv[i*4+3]=new Vector2(t,1);
                if(i==count)continue;int v=i*4,k=i*12;
                triangles[k]=v;triangles[k+1]=v+1;triangles[k+2]=v+5;
                triangles[k+3]=v;triangles[k+4]=v+5;triangles[k+5]=v+4;
                triangles[k+6]=v+2;triangles[k+7]=v+3;triangles[k+8]=v+7;
                triangles[k+9]=v+2;triangles[k+10]=v+7;triangles[k+11]=v+6;
            }
            var mesh=new Mesh{name="Lata clock collar"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
        }
        public static float ActualRestore(Lata lata)
        {
            var round=GameServices.Round;
            if(lata==null || lata.IsUpright || round==null || !round.RoundActive)return 0;
            float ratio=0;
            foreach(var actor in round.Players)if(actor!=null && actor.IsDefender)
                ratio=Mathf.Max(ratio,actor.GetComponent<Carrier>()?.ChannelRatio??0);
            if(NetAuthority.IsHost && Net.MatchRpc.Instance!=null)
                ratio=Mathf.Max(ratio,Net.MatchRpc.Instance.ObservedHostResetRatio);
            return Mathf.Clamp01(ratio);
        }
        public void ApplySnapshot(float restore,float protection)
        {
            if(!float.IsFinite(restore)||!float.IsFinite(protection))return;
            _snapshot=true;_observedAt=Time.time;_observedRestore=Mathf.Clamp01(restore);
            _observedProtection=Mathf.Clamp(protection,0,Balance.ThrowRestoreCooldown);
        }
        public static LataClockPresentation For(Lata lata)
        {
            return lata!=null && Live.TryGetValue(lata,out var cue)?cue:null;
        }
        private void LateUpdate()
        {
            if(_recorded || _lata==null)return;
            var root=CameraSystem.MatchReplayArchive.PropModel(_lata.gameObject);
            if(root!=_bodyRoot || _bodySkin!=_lata.SkinIndex)
            {_bodyRoot=root;_bodySkin=_lata.SkinIndex;_body=root.GetComponentsInChildren<Renderer>(true);}
            _bodyBlock??=new MaterialPropertyBlock();
            if(_body!=null)foreach(var surface in _body)
            {
                if(surface==null || surface.sharedMaterial==null || !surface.sharedMaterial.HasProperty("_DepthReadability"))continue;
                surface.GetPropertyBlock(_bodyBlock);_bodyBlock.SetFloat("_DepthReadability",WorldCueProfile.Current.DistanceReadability);
                Vector3 axis=_lata.transform.up;_bodyBlock.SetVector("_WorldMetalAxis",new Vector4(axis.x,axis.y,axis.z,1));
                surface.SetPropertyBlock(_bodyBlock);
            }
            ReadLive(out float restore,out float protection);
            Draw(_lata.transform.position,_lata.transform.rotation,restore,protection);
        }
        private void ReadLive(out float restore,out float protection)
        {
            restore=ActualRestore(_lata);protection=_lata.ProtectionLeft/Balance.ThrowRestoreCooldown;
            if(NetAuthority.ShouldRequest() && _snapshot)
            {
                float age=Mathf.Max(0,Time.time-_observedAt);
                // Authoritative snapshots arrive at10Hz during a clock; stop
                // extrapolation when stale so disconnects cannot invent progress.
                protection=_lata.IsUpright?Mathf.Max(0,_observedProtection-age)/Balance.ThrowRestoreCooldown:0;
                var viewer=GameServices.Round?.PlayerAt(NetAuthority.LocalSlot);
                bool owner=viewer!=null && viewer.IsDefender;
                if(!owner)restore=!_lata.IsUpright && _observedRestore>0 && age<=.65f?
                    Mathf.Clamp01(_observedRestore+age/_lata.ResetChannelTime):0;
            }
        }
        public void Draw(Vector3 position,Quaternion rotation,float restore,float protection)
        {
            RestoreRatio=Mathf.Clamp01(restore);ProtectionRatio=Mathf.Clamp01(protection);
            // The first physical collar hid the beginning of its fill behind a
            // fallen can from half the court. Face each camera around the actual
            // body centre; depth test stays on, and its diameter stays close.
            transform.SetPositionAndRotation(position+rotation*Vector3.up*.18f,rotation);
            float ratio=ProtectionRatio>0?ProtectionRatio:RestoreRatio;
            _ring.enabled=ratio>0 && WorldCueProfile.Current.RestoreClock>0;
            _block.SetFloat("_Fill",ratio);_block.SetFloat("_Weight",WorldCueProfile.Current.RestoreClock);
            _block.SetFloat("_Billboard",1);
            _block.SetColor("_Face",ProtectionRatio>0?new Color(1,.83f,.45f):UI.UiTheme.Defense);
            _ring.SetPropertyBlock(_block);
        }
        public void ShowForCapture(bool show){if(_ring!=null)_ring.forceRenderingOff=!show;}
        public static int Pack(Lata lata)
        {
            var clock=For(lata);bool protectedCan=lata.IsProtected;
            float ratio=protectedCan?lata.ProtectionLeft/Balance.ThrowRestoreCooldown:ActualRestore(lata);
            if(clock!=null){clock.ReadLive(out float restore,out float protection);protectedCan=protection>0;ratio=protectedCan?protection:restore;}
            return RecordedClockBit | (Mathf.RoundToInt(Mathf.Clamp01(ratio)*255)<<2) | (lata.IsUpright?1:0) | (protectedCan?2:0);
        }
        public static void Unpack(int state,out float restore,out float protection)
        {
            restore=protection=0;if((state&RecordedClockBit)==0)return; // Legacy clips have no clock.
            float ratio=((state>>2)&255)/255f;
            if((state&2)!=0)protection=ratio;else if((state&1)==0)restore=ratio;
        }
        private void OnDestroy()
        {
            // The scene may destroy the can first. Its managed key still needs
            // removing even when Unity's overloaded null comparison says gone.
            if(!ReferenceEquals(_lata,null) && Live.TryGetValue(_lata,out var cue) && cue==this)Live.Remove(_lata);
            if(_mesh!=null)Destroy(_mesh);
        }
    }
}
