using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // V2: chalk stroke = the square rule, at its real world boundary; stronger
    // chalk = armed, one outward sweep = restored. Nearest-exit emphasis is local
    // to a taggable viewer. Identity/power colours and HUD facts stay with their owners.
    public sealed class CourtBoundaryPresentation : MonoBehaviour
    {
        public const float SweepSeconds = .6f;
        private const float HalfWidth = .16f;
        private const int PiecesPerEdge = 48;
        private readonly List<Renderer> _authored = new List<Renderer>();
        private readonly List<bool> _authoredEnabled = new List<bool>();
        private int _edgeMask;
        private readonly CharacterMotor[] _actors = new CharacterMotor[Balance.PlayerCount];
        private readonly bool[] _danger = new bool[Balance.PlayerCount];
        private readonly int[] _epochs = new int[Balance.PlayerCount];
        private readonly int[] _teleports = new int[Balance.PlayerCount];
        private readonly float[] _escapedAt = new float[Balance.PlayerCount];
        private readonly RaycastHit[] _hits = new RaycastHit[24];
        private MaterialPropertyBlock _block;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private Lata _lata;
        private int _round;
        private bool _recorded, _armed, _replacing, _haveFrame;
        private float _floor, _restoreAt = -100;
        private Vector3 _restoreOrigin;
        private sealed class CameraState { public Camera Camera; public MaterialPropertyBlock Block; public bool Enabled; }
        private readonly List<CameraState> _cameraStates=new List<CameraState>();
        private readonly List<CameraState> _cameraPool=new List<CameraState>();
        public bool Armed => _armed;
        public bool Recorded => _recorded;
        public float Floor => _floor;
        public int EscapeCount { get; private set; }
        public int ScannedRenderers { get; private set; }
        public int AuthoredBoundaryCount
        {
            get { int count=0;for(int edge=0;edge<4;edge++)if((_edgeMask&(1<<edge))!=0)count++;return count; }
        }

        public static CourtBoundaryPresentation Install(Transform parent, Lata lata)
        {
            var go = new GameObject("CourtBoundaryPresentation"); go.SetActive(false); go.transform.SetParent(parent,false);
            var cue = go.AddComponent<CourtBoundaryPresentation>(); cue.Build(lata, false);
            go.SetActive(true); return cue;
        }
        public static CourtBoundaryPresentation CreateRecorded(Transform parent, Lata lata)
        {
            var go = new GameObject("RecordedCourtBoundary"); go.SetActive(false); go.transform.SetParent(parent,false);
            var cue = go.AddComponent<CourtBoundaryPresentation>(); cue.Build(lata, true);
            go.SetActive(true); cue.ShowForCapture(false); return cue;
        }
        private void Build(Lata lata, bool recorded)
        {
            _block = new MaterialPropertyBlock();
            _recorded = recorded; _lata = lata;
            for(int i=0;i<_escapedAt.Length;i++)_escapedAt[i]=float.NegativeInfinity;
            float seed = lata != null ? lata.transform.position.y : 0;
            _floor = GroundAt(Vector3.zero, seed);
            if (!recorded)
            {
                var renderers=FindObjectsByType<MeshRenderer>();ScannedRenderers=renderers.Length;
                foreach (var candidate in renderers)
                {
                    if(candidate.gameObject.scene!=gameObject.scene)continue;
                    var parent = candidate.transform.parent;
                    bool chalk=parent!=null && parent.name=="Chalk";
                    chalk|=candidate.name.IndexOf("chalk",System.StringComparison.OrdinalIgnoreCase)>=0;
                    bool materialChalk=false;
                    foreach(var surfaceMaterial in candidate.sharedMaterials)
                        if(surfaceMaterial!=null && surfaceMaterial.name.IndexOf("chalk",System.StringComparison.OrdinalIgnoreCase)>=0)materialChalk=true;
                    if(!chalk && !materialChalk)continue;
                    RegisterAuthoredEdge(candidate);
                }
            }
            _mesh = BuildMesh();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off; _renderer.receiveShadows = false;
            var material = new Material(Shader.Find("TumbangPreso/CourtSignal")) { name = "Live court chalk" };
            _renderer.sharedMaterial = material; VfxRenderTag.Own(gameObject,material);
            Apply(null, false, -1, Vector3.zero);
        }
        private void RegisterAuthoredEdge(MeshRenderer original)
        {
            // Imported chalk uses read-only meshes on generic "default" nodes.
            // Its material identifies the medium; world bounds identify one whole
            // rule edge without reading/mutating mesh data or touching other marks.
            Bounds bounds=original.bounds;Vector3 c=bounds.center, size=bounds.size;
            float r=Balance.ConfinementRadius;
            if(bounds.max.y>_floor+.55f || bounds.min.y<_floor-.3f)return;
            int edge=-1;
            if(Mathf.Abs(c.z)<.2f && Mathf.Abs(Mathf.Abs(c.x)-r)<.65f &&
                size.x<1.3f && Mathf.Abs(size.z-r*2)<.5f)edge=c.x>=0?0:1;
            else if(Mathf.Abs(c.x)<.2f && Mathf.Abs(Mathf.Abs(c.z)-r)<.65f &&
                size.z<1.3f && Mathf.Abs(size.x-r*2)<.5f)edge=c.z>=0?2:3;
            if(edge<0)return;
            _edgeMask|=1<<edge;_authored.Add(original);_authoredEnabled.Add(original.enabled);
        }
        private Mesh BuildMesh()
        {
            var vertices = new List<Vector3>(PiecesPerEdge*16);
            var uv = new List<Vector2>(PiecesPerEdge*16);
            var triangles = new List<int>(PiecesPerEdge*24);
            float r = Balance.ConfinementRadius;
            Vector3[] corners = { new Vector3(-r,0,-r),new Vector3(r,0,-r),new Vector3(r,0,r),new Vector3(-r,0,r) };
            for (int edge=0;edge<4;edge++)
            {
                Vector3 a=corners[edge], b=corners[(edge+1)%4];
                Vector3 side=Vector3.Cross((b-a).normalized,Vector3.up)*HalfWidth;
                for (int piece=0;piece<PiecesPerEdge;piece++)
                {
                    Vector3 p=Vector3.Lerp(a,b,piece/(float)PiecesPerEdge),q=Vector3.Lerp(a,b,(piece+1f)/PiecesPerEdge);
                    // Inset the sample a few millimetres from the exact edge of a kerb collider.
                    p.y=GroundAt(new Vector3(p.x*.999f,0,p.z*.999f),_floor)+.012f;
                    q.y=GroundAt(new Vector3(q.x*.999f,0,q.z*.999f),_floor)+.012f;
                    int at=vertices.Count;vertices.Add(transform.InverseTransformPoint(p-side));vertices.Add(transform.InverseTransformPoint(p+side));
                    vertices.Add(transform.InverseTransformPoint(q+side));vertices.Add(transform.InverseTransformPoint(q-side));
                    uv.Add(new Vector2(0,0));uv.Add(new Vector2(0,1));uv.Add(new Vector2(1,1));uv.Add(new Vector2(1,0));
                    triangles.Add(at);triangles.Add(at+1);triangles.Add(at+2);triangles.Add(at);triangles.Add(at+2);triangles.Add(at+3);
                }
            }
            var mesh=new Mesh {name="Confinement chalk square"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);
            mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private float GroundAt(Vector3 at,float fallback)
        {
            int count=Physics.RaycastNonAlloc(new Vector3(at.x,fallback+.7f,at.z),Vector3.down,_hits,1.7f,~0,QueryTriggerInteraction.Ignore);
            float best=float.NegativeInfinity;
            for(int i=0;i<count;i++)
            {
                var c=_hits[i].collider;
                if(c==null || c.GetComponentInParent<CharacterMotor>()!=null || c.GetComponentInParent<Slipper>()!=null || c.GetComponentInParent<Lata>()!=null)continue;
                if(_hits[i].normal.y<.65f)continue;
                best=Mathf.Max(best,_hits[i].point.y);
            }
            return float.IsNegativeInfinity(best)?fallback:best;
        }
        public static Vector3 ClosestExit(Vector3 position)
        {
            float r=Balance.ConfinementRadius;
            return Mathf.Abs(position.x)>=Mathf.Abs(position.z)
                ? new Vector3(position.x>=0?r:-r,position.y,Mathf.Clamp(position.z,-r,r))
                : new Vector3(Mathf.Clamp(position.x,-r,r),position.y,position.z>=0?r:-r);
        }
        public static CharacterMotor Viewer(Camera camera)
        {
            var rig=camera!=null?camera.GetComponent<CameraRig>():null;
            var round=GameServices.Round;
            if(rig==null || round==null)return null;
            foreach(var actor in round.Players)if(actor!=null && rig.IsFollowing(actor))return actor;
            return null;
        }
        public static bool IsThreatened(CharacterMotor actor,Lata lata)
            =>actor!=null && lata!=null && lata.IsUpright && actor.IsTaggable();
        private void OnEnable()
        {
            if(_recorded)return;
            Camera.onPreCull+=BeforeCamera;
            Camera.onPostRender+=AfterCamera;
            if(_lata!=null)_lata.UprightChanged+=OnCanState;
        }
        private void OnDisable()
        {
            if(_recorded)return;
            Camera.onPreCull-=BeforeCamera;
            Camera.onPostRender-=AfterCamera;
            for(int i=_cameraStates.Count-1;i>=0;i--)RestoreCameraState(_cameraStates[i]);
            _cameraStates.Clear();
            if(_lata!=null)_lata.UprightChanged-=OnCanState;
            ReplaceAuthored(false);GameServices.Audio?.SetCourtDanger(0,true);
        }
        private void OnCanState(bool upright)
        {
            if(!upright || GameServices.Round==null || !GameServices.Round.RoundActive)return;
            _restoreAt=Time.time;_restoreOrigin=_lata.transform.position;
        }
        private void LateUpdate()
        {
            if(_recorded || _renderer==null)return;
            var round=GameServices.Round;var profile=WorldCueProfile.Current;
            ReplaceAuthored(profile.Boundary>.001f);
            _renderer.enabled=profile.Boundary>.001f;
            _armed=round!=null && round.RoundActive && _lata!=null && _lata.IsUpright;
            int number=GameServices.Match!=null?GameServices.Match.RoundNumber:0;
            if(number!=_round){_round=number;_haveFrame=false;_restoreAt=-100;}
            var viewer=Viewer(Camera.main);
            bool personal=IsThreatened(viewer,_lata) && !PresentationClock.BlocksInput && !UI.Panel.AnyOpen;
            GameServices.Audio?.SetCourtDanger(personal?profile.DangerAudio:0);
            if(round==null)return;
            foreach(var actor in round.Players)
            {
                if(actor==null || actor.PlayerSlot<0 || actor.PlayerSlot>=_actors.Length)continue;
                int seat=actor.PlayerSlot;bool same=_actors[seat]==actor;
                bool escaped=_haveFrame && same && _danger[seat] && _armed && actor.HoldingSlipper &&
                    !actor.IsDefender && !actor.IsInsideBox() && actor.MovementEpoch==_epochs[seat] &&
                    actor.PresentationTeleportSerial==_teleports[seat] && Time.time-_escapedAt[seat]>.65f;
                if(escaped)
                {
                    _escapedAt[seat]=Time.time;EscapeCount++;
                    if(profile.Escape>.001f)
                    {
                        Vector3 at=actor.transform.position;at.y=GroundAt(at,_floor);
                        CourtEscapePuff.Play(at,profile.Escape);
                        GameServices.Audio?.PlayAtVaried("court_escape",at,1.08f,1.16f,.28f*profile.Escape);
                    }
                }
                _actors[seat]=actor;_epochs[seat]=actor.MovementEpoch;_teleports[seat]=actor.PresentationTeleportSerial;
                _danger[seat]=IsThreatened(actor,_lata);
            }
            _haveFrame=true;
        }
        private void BeforeCamera(Camera camera)
        {
            if(_renderer==null)return;
            CameraState state;
            if(_cameraPool.Count>0){int last=_cameraPool.Count-1;state=_cameraPool[last];_cameraPool.RemoveAt(last);}
            else state=new CameraState {Block=new MaterialPropertyBlock()};
            state.Camera=camera;state.Enabled=_renderer.enabled;_renderer.GetPropertyBlock(state.Block);_cameraStates.Add(state);
            Apply(Viewer(camera),_armed,Time.time-_restoreAt,_restoreOrigin);
        }
        private void AfterCamera(Camera camera)
        {
            for(int i=_cameraStates.Count-1;i>=0;i--)
                if(_cameraStates[i].Camera==camera)
                {var state=_cameraStates[i];_cameraStates.RemoveAt(i);RestoreCameraState(state);return;}
        }
        private void RestoreCameraState(CameraState state)
        {
            if(_renderer!=null){_renderer.SetPropertyBlock(state.Block);_renderer.enabled=state.Enabled;}
            state.Camera=null;_cameraPool.Add(state);
        }
        private void Apply(CharacterMotor viewer,bool armed,float restoreAge,Vector3 origin)
        {
            if(_renderer==null)return;
            var profile=WorldCueProfile.Current;
            _renderer.enabled=profile.Boundary>.001f;
            _block.SetColor("_Chalk",WorldLookPresentation.CourtChalk);_block.SetColor("_Ink",WorldLookPresentation.CourtEdge);
            _block.SetFloat("_Weight",Mathf.Clamp01(profile.Boundary));_block.SetFloat("_Armed",armed?1:0);
            bool exit=!_recorded && armed && IsThreatened(viewer,_lata) && !PresentationClock.BlocksInput;
            Vector3 closest=exit?ClosestExit(viewer.transform.position):Vector3.zero;
            _block.SetVector("_Exit",new Vector4(closest.x,closest.z,exit?Mathf.Clamp01(profile.NearestExit):0,0));
            bool sweep=armed && restoreAge>=0 && restoreAge<SweepSeconds && !Settings.SettingsStore.Current.ReducedUiMotion;
            float far=(new Vector2(Mathf.Abs(origin.x)+Balance.ConfinementRadius,Mathf.Abs(origin.z)+Balance.ConfinementRadius)).magnitude;
            _block.SetVector("_Sweep",new Vector4(origin.x,origin.z,sweep?far*restoreAge/SweepSeconds:-100,
                sweep?(Settings.SettingsStore.Current.ReducedEffects?.45f:1):0));
            _renderer.SetPropertyBlock(_block);
        }
        public void DrawRecorded(bool armed,float restoreAge,Vector3 origin)
        { _armed=armed;Apply(null,armed,restoreAge,origin); }
        public void ShowForCapture(bool visible) { if(_renderer!=null)_renderer.forceRenderingOff=!visible; }
        private void ReplaceAuthored(bool replace)
        {
            if(_replacing==replace)return;_replacing=replace;
            for(int i=0;i<_authored.Count;i++)
            {
                if(_authored[i]!=null)_authored[i].enabled=replace?false:_authoredEnabled[i];
            }
        }
        private void OnDestroy()
        {
            if(_mesh!=null)Destroy(_mesh);
        }
    }
}
