using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Render-only rewind. The accepted teleport still owns gameplay; this body, eye and marker offset own the presentation.
    // One travelling body and one hologram rewind through the recorded route. Layered contours belong to the hologram
    // material, giving a dense temporal smear without independently moving past actors. Small electrical bursts mark
    // the activation and catch; gameplay still uses the accepted instant teleport.
    [DefaultExecutionOrder(2500)]
    public sealed class ZackBacktrackLook : MonoBehaviour
    {
        public const float RevealSeconds = .06f, TravelSeconds = .42f, CatchSeconds = .14f;
        /// <summary>The catch: the body reaches the old spot and the landing cue plays. Matches the 0.46 s pull cue.</summary>
        public const float PullSeconds = RevealSeconds + TravelSeconds;
        private const float Linger = .16f, EchoDelay = .055f, EchoSpacing = .026f;
        private static readonly Dictionary<int,ZackBacktrackLook> Live = new Dictionary<int,ZackBacktrackLook>();
        private CharacterMotor _motor;
        private Transform _model;
        private Vector3[] _route;
        private float[] _routeTime, _routeDistance;
        private Vector3 _from, _to, _modelOffset;
        private float _born, _poseNow;
        private int _serial, _round;
        private ZackFx.Echo _body;
        private ZackFx.Echo[] _moments = new ZackFx.Echo[0];
        private readonly Vector4[] _echoTrail = new Vector4[5];
        private Renderer[] _hidden;
        private bool[] _wasHidden;
        private bool _restored, _landed;
        public float Age => Mathf.Max(0,Time.time-_born);
        public bool Pulling => _motor!=null && Age<PullSeconds;
        public Vector3 VisualFeet => FeetAt(Age);
        /// <summary>Where the drawn body's model root actually is (minus the model's own offset).</summary>
        public Vector3 RenderedFeet => _body!=null?_body.Copy.Root.transform.position-_modelOffset:VisualFeet;
        public bool BodyDrawn { get; private set; }
        /// <summary>How many past moments are drawn this frame, for review traces.</summary>
        public int MomentsDrawn { get; private set; }
        /// <summary>Read-only moment pose and renderer state for recording and lifecycle checks.</summary>
        public bool TryMoment(int index, out Vector3 position, out bool drawn)
        {
            position = Vector3.zero; drawn = false;
            if (index < 0 || index >= _moments.Length || _moments[index]?.Copy.Root == null) return false;
            var moment = _moments[index]; position = moment.Copy.Root.transform.position;
            foreach (var surface in moment.Copy.Renderers)
                if (surface != null && surface.enabled && surface.gameObject.activeInHierarchy && !surface.forceRenderingOff)
                { drawn = true; break; }
            return true;
        }
        public bool LiveBodyHidden => _hidden!=null&&!_restored;
        public float RouteLength => _routeDistance!=null&&_routeDistance.Length>0?_routeDistance[_routeDistance.Length-1]:0;

        public static ZackBacktrackLook Play(CharacterMotor motor,Vector3 from,Vector3 to,Vector3[] path)
        {
            if(motor==null)return null;
            if(Live.TryGetValue(motor.PlayerSlot,out var old)&&old!=null)old.Finish();
            var v=new GameObject("ZackBacktrackRewind").AddComponent<ZackBacktrackLook>();
            v._motor=motor;v._from=from;v._to=to;v._born=Time.time;v._serial=motor.PresentationTeleportSerial;
            v._round=GameServices.Match?.RoundNumber??0;v._model=motor.GetComponent<CharacterVisual>()?.Model?.transform;
            v._body=ZackFx.Echo.Of(motor.PlayerSlot,v.transform);
            if(v._body!=null)
            {
                v._poseNow=v._body.Track.Newest;v._body.PoseAt(v._poseNow);v._body.Rim(.35f);
                v._modelOffset=v._body.Copy.Root.transform.position-from;v._modelOffset.y=0;
                foreach(var r in v._body.Copy.Renderers)if(r!=null)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                v.CopyShoe(v._body);
                // His seat ring and tag are not hidden: `CharacterNameplate` carries them along with the drawn body.
                var surfaces=new List<Renderer>();
                foreach(var r in motor.GetComponentsInChildren<Renderer>(true))if(r.GetComponentInParent<CharacterNameplate>()==null)surfaces.Add(r);
                var held=motor.GetComponent<Carrier>()?.Held;if(held!=null)surfaces.AddRange(held.GetComponentsInChildren<Renderer>(true));
                v._hidden=surfaces.ToArray();v._wasHidden=new bool[surfaces.Count];
                for(int i=0;i<surfaces.Count;i++){v._wasHidden[i]=surfaces[i].forceRenderingOff;surfaces[i].forceRenderingOff=true;}
            }
            if(!v.RouteFromHistory())v.RouteFromPath(path);
            if(v._body!=null)
            {
                // One hologram carries a layered contour trail in its shader, rather than a group of actors.
                var echo=ZackFx.Echo.Of(motor.PlayerSlot,v.transform);
                if(echo!=null){v.CopyShoe(echo);echo.Past(.1f);v._moments=new[]{echo};}
            }
            ZackFx.Sparks(to+Vector3.up*.15f,.22f,Settings.SettingsStore.Current.ReducedEffects?1:3,1.3f);
            Live[motor.PlayerSlot]=v;v.Step();return v;
        }
        public void SealTeleport(){if(_motor!=null)_serial=_motor.PresentationTeleportSerial;}
        public static Vector3 EyeOffset(CharacterMotor motor)
            => motor!=null&&Live.TryGetValue(motor.PlayerSlot,out var v)&&v!=null&&v._motor==motor&&v.Pulling
                ?v.VisualFeet-motor.transform.position:Vector3.zero;
        /// <summary>The flat offset of the drawn body from the gameplay body, for its seat ring and name tag.</summary>
        public static Vector3 MarkerOffset(CharacterMotor motor)
        {
            if(motor==null||!Live.TryGetValue(motor.PlayerSlot,out var v)||v==null||v._motor!=motor||v._restored)return Vector3.zero;
            var d=v.VisualFeet-motor.transform.position;d.y=0;return d;
        }

        // The live body is hidden during the pull, but its camera-specific tag cue still belongs on the drawn body.
        internal static bool TryReadabilityBody(CharacterMotor motor, out Renderer[] surfaces, out Vector3 feet)
        {
            surfaces = null; feet = motor != null ? motor.transform.position : Vector3.zero;
            if (motor == null || !Live.TryGetValue(motor.PlayerSlot, out var look) || look == null
                || look._motor != motor || look._restored || look._body == null) return false;
            surfaces = look._body.Copy.Renderers; feet = look.RenderedFeet;
            return true;
        }

        // ------------------------------------------------------------------ the route, departure first

        private bool RouteFromHistory()
        {
            if(_body==null)return false;var track=_body.Track;
            float best=float.PositiveInfinity,back=0;
            for(float t=_poseNow-.3f;t>=Mathf.Max(track.Oldest,_poseNow-3.8f);t-=.025f)
            {
                if(!track.TryRootAt(t,out var root,out _))return false;
                var d=root-_modelOffset-_to;d.y=0;
                if(d.sqrMagnitude<best){best=d.sqrMagnitude;back=t;}
            }
            if(best>1f)return false;
            var points=new List<Vector3>{_from};var times=new List<float>{_poseNow};
            for(float t=_poseNow-.05f;t>back;t-=.05f)
            {
                track.TryRootAt(t,out var root,out _);var p=root-_modelOffset;
                if((p-points[points.Count-1]).sqrMagnitude>.0025f){points.Add(p);times.Add(t);}
            }
            points.Add(_to);times.Add(back);Seal(points,times);return true;
        }
        private void RouteFromPath(Vector3[] path)
        {
            var points=new List<Vector3>{_from};
            if(path!=null)for(int i=path.Length-1;i>=0;i--)
                if((path[i]-points[points.Count-1]).sqrMagnitude>.0025f)points.Add(path[i]);
            if((_to-points[points.Count-1]).sqrMagnitude>.0001f)points.Add(_to);
            if(points.Count<2)points.Add(_to);
            var times=new List<float>();
            for(int i=0;i<points.Count;i++)times.Add(_poseNow-ZackRules.BacktrackSeconds*i/(points.Count-1f));
            Seal(points,times);
        }
        private void Seal(List<Vector3> points,List<float> times)
        {
            _route=points.ToArray();_routeTime=times.ToArray();_routeDistance=new float[_route.Length];
            for(int i=1;i<_route.Length;i++)_routeDistance[i]=_routeDistance[i-1]+Vector3.Distance(_route[i-1],_route[i]);
        }
        /// <summary>The route at fraction <paramref name="e"/> of its length: position, history time and travel direction.</summary>
        private Vector3 Route(float e,out float time,out Vector3 heading)
        {
            float length=RouteLength,d=Mathf.Clamp01(e)*length;int i=1;
            while(i<_route.Length-1&&_routeDistance[i]<d)i++;
            float span=_routeDistance[i]-_routeDistance[i-1],u=span>1e-5f?Mathf.Clamp01((d-_routeDistance[i-1])/span):1;
            time=Mathf.Lerp(_routeTime[i-1],_routeTime[i],u);
            heading=_route[i]-_route[i-1];heading.y=0;heading=heading.sqrMagnitude>1e-6f?heading.normalized:Vector3.zero;
            return Vector3.Lerp(_route[i-1],_route[i],u);
        }
        /// <summary>Distance from <paramref name="point"/> to the recorded route, flat.</summary>
        public float OffRoute(Vector3 point)
        {
            float best=float.PositiveInfinity;point.y=0;
            for(int i=1;i<_route.Length;i++)
            {
                Vector3 a=_route[i-1],b=_route[i];a.y=b.y=0;var ab=b-a;
                float u=ab.sqrMagnitude>1e-6f?Mathf.Clamp01(Vector3.Dot(point-a,ab)/ab.sqrMagnitude):0;
                best=Mathf.Min(best,Vector3.Distance(point,a+ab*u));
            }
            return best;
        }

        // ------------------------------------------------------------------ timing

        private static float Travel(float age)=>Mathf.Clamp01((age-RevealSeconds)/TravelSeconds);
        // A reel rewinding: it spins up quickly, runs, and eases into the catch.
        private static float Progress(float age){float x=Travel(age);return .02f*Mathf.SmoothStep(0,1,Mathf.Clamp01(age/RevealSeconds))+.98f*(.4f*x+.6f*x*x*(3-2*x));}
        private Vector3 FeetAt(float age)
        {
            var p=Route(Progress(age),out _,out _);if(_motor==null)return p;
            float merge=age>=PullSeconds?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,Travel(age)));
            return Vector3.Lerp(p,_motor.transform.position,merge);
        }

        private void CopyShoe(ZackFx.Echo echo)
        {
            var held=_motor.GetComponent<Carrier>()?.Held;var liveHand=_motor.GetComponent<CharacterVisual>()?.HandAnchor;
            Transform grip=null;foreach(var bone in echo.Copy.Bones)if(bone.name=="HandAnchor"){grip=bone;break;}
            if(held==null||liveHand==null||grip==null)return;
            var root=new GameObject("RewindingHeldSlipper");root.transform.SetParent(grip,false);
            foreach(var surface in held.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!surface.enabled||!surface.gameObject.activeInHierarchy||surface.GetComponent<VfxRenderTag>()!=null
                    ||!surface.TryGetComponent<MeshFilter>(out var mesh)||mesh.sharedMesh==null)continue;
                var part=new GameObject("HeldShoeSurface");part.transform.SetParent(root.transform,false);
                held.FitCarriedSurface(surface.transform,liveHand,part.transform);part.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                var r=part.AddComponent<MeshRenderer>();r.sharedMaterials=surface.sharedMaterials;
            }
        }
        private static void Show(ZackFx.Echo echo,bool on)
        {
            echo.Visible(on);
            foreach(var r in echo.Copy.Root.GetComponentsInChildren<MeshRenderer>())if(r.name=="HeldShoeSurface")r.forceRenderingOff=!on;
        }
        private void LateUpdate()
        {
            if(_motor==null||_motor.AbilitySystem?.HeroId!="zack"||_motor.IsTagged||_motor.IsStunned||_motor.IsTripped
                ||GameServices.Round?.RoundActive!=true||(GameServices.Match?.RoundNumber??0)!=_round||_motor.PresentationTeleportSerial!=_serial)
            {Finish();return;}Step();if(Age>PullSeconds+CatchSeconds+Linger)Finish();
        }

        // ------------------------------------------------------------------ one frame of the look

        private void Step()
        {
            float age=Age,x=Travel(age),e=Progress(age),c=Mathf.Clamp01((age-PullSeconds)/CatchSeconds);
            bool travelling=age<PullSeconds,catching=!travelling&&c<1,eyes=ZackFx.InOwnFirstPerson(_motor);
            bool calm=Settings.SettingsStore.Current.ReducedEffects;
            var eye=UnityEngine.Camera.main!=null?UnityEngine.Camera.main.transform.position:Vector3.zero;
            float length=RouteLength;
            var feet=FeetAt(age);Route(e,out float time,out _);
            if(_body!=null)
            {
                var track=_body.Track;
                // The reveal is a tape catching: his pose flicks back and forth twice before the rewind takes it. Then the run
                // plays backwards in step with the route, and the catch takes up his live stance.
                float stutter=calm?0:.12f*Mathf.Abs(Mathf.Sin(Mathf.Clamp01(age/RevealSeconds)*Mathf.PI*2));
                float poseTime=!travelling?track.Newest:age<RevealSeconds?_poseNow-stutter:time;
                _body.PoseAt(poseTime);
                var root=_body.Copy.Root.transform;
                var live=_model!=null?_model.rotation:root.rotation;
                var facing=catching||!travelling?live:Quaternion.Slerp(root.rotation,live,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1,x)));
                float settle=Mathf.Sin(Mathf.PI*c);
                root.SetPositionAndRotation(feet+_modelOffset-Vector3.up*.04f*settle,facing);
                root.localScale=Vector3.Scale(root.localScale,new Vector3(1+.08f*settle,1-.1f*settle,1+.08f*settle));
                // Each past moment he folds into brightens his rim; the catch is the last and brightest.
                _body.Rim(travelling?.35f+.35f*e:Mathf.Lerp(.9f,.2f,c));
                BodyDrawn=c<1&&!eyes;Show(_body,BodyDrawn);
                if(c<1){foreach(var r in _hidden)if(r!=null)r.forceRenderingOff=true;}else Restore();
            }
            // The single hologram repeats the travelling pose a fraction later. Its faint contour layers follow the
            // same traversed route, so the image reads as one fast subject with temporal persistence.
            MomentsDrawn=0;
            if(_moments.Length>0)
            {
                var echo=_moments[0];float echoAge=age-EchoDelay;
                var p=Route(Progress(Mathf.Clamp(echoAge,0,PullSeconds)),out float then,out _);
                echo.PoseAt(then);var root=echo.Copy.Root.transform;
                root.SetPositionAndRotation(p+_modelOffset,root.rotation);
                float enter=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.015f,.065f,echoAge));
                float leave=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(PullSeconds,PullSeconds+.065f,echoAge));
                float near=eyes?Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.1f,1.8f,Vector3.Distance(p+Vector3.up*1.2f,eye))):1;
                echo.Past(.1f,enter*leave*near);
                for(int layer=0;layer<_echoTrail.Length;layer++)
                {
                    float lagAge=echoAge-layer*EchoSpacing;
                    var q=Route(Progress(Mathf.Clamp(lagAge,0,PullSeconds)),out _,out _);
                    float visible=layer==0?1:calm?0:Mathf.Clamp01(lagAge/.045f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(PullSeconds,PullSeconds+.055f,lagAge)));
                    _echoTrail[layer]=new Vector4(q.x-p.x,q.y-p.y,q.z-p.z,visible*Mathf.Pow(.58f,layer));
                }
                echo.Trail(_echoTrail);
                bool on=enter*leave*near>.015f;Show(echo,on);MomentsDrawn=on?1:0;
            }
            if(!_landed&&!travelling)
            {
                _landed=true;
                ZackFx.Sparks(_to+Vector3.up*.12f,.38f,calm?2:5,2.1f);
                GameServices.Audio?.PlayAt("sfx_zack_backtrack_land",_to);
            }
        }
        private void Restore()
        {
            if(_restored||_hidden==null)return;_restored=true;
            for(int i=0;i<_hidden.Length;i++)if(_hidden[i]!=null)_hidden[i].forceRenderingOff=_wasHidden[i];
        }
        private void Finish(){Restore();if(_motor!=null&&Live.TryGetValue(_motor.PlayerSlot,out var v)&&v==this)Live.Remove(_motor.PlayerSlot);Destroy(gameObject);}
        private void OnDestroy()
        {
            Restore();_body?.Destroy();foreach(var m in _moments)m?.Destroy();
            if(_motor!=null&&Live.TryGetValue(_motor.PlayerSlot,out var v)&&v==this)Live.Remove(_motor.PlayerSlot);
        }
    }
}
