using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Render-only reverse travel. The accepted teleport still owns gameplay; this body, eye and marker offset own the pull.
    // One real-coloured Zack is snagged at the departure, yanked backwards along his recorded route while his run plays in
    // reverse, then caught at the old spot. A chattering gold tether reels him toward an anchor ring; no trailing copies.
    [DefaultExecutionOrder(2500)]
    public sealed class ZackBacktrackLook : MonoBehaviour
    {
        public const float SnagSeconds = .08f, TravelSeconds = .40f, CatchSeconds = .14f;
        /// <summary>The catch: the body reaches the old spot and the landing cue plays. Matches the 0.46 s pull cue.</summary>
        public const float PullSeconds = SnagSeconds + TravelSeconds;
        private const float RingHold = .4f, HipHeight = .7f, WaistHeight = .78f;
        private static readonly Dictionary<int,ZackBacktrackLook> Live = new Dictionary<int,ZackBacktrackLook>();
        private CharacterMotor _motor;
        private Transform _model;
        private Vector3[] _route;
        private float[] _routeTime, _routeDistance;
        private Vector3 _from, _to, _modelOffset;
        private float _born, _poseNow;
        private int _serial, _round;
        private ZackFx.Echo _body;
        private Renderer[] _hidden;
        private bool[] _wasHidden;
        private LineRenderer _tether, _tetherInk, _wake, _wakeInk, _crown, _ring, _ringInk, _leave;
        private bool _restored, _landed;
        public float Age => Mathf.Max(0,Time.time-_born);
        public bool Pulling => _motor!=null && Age<PullSeconds;
        public Vector3 VisualFeet => FeetAt(Age);
        /// <summary>Where the drawn body's model root actually is, lift and lean included (minus the model's own offset).</summary>
        public Vector3 RenderedFeet => _body!=null?_body.Copy.Root.transform.position-_modelOffset:VisualFeet;
        public bool BodyDrawn { get; private set; }
        public bool LiveBodyHidden => _hidden!=null&&!_restored;
        public float RouteLength => _routeDistance!=null&&_routeDistance.Length>0?_routeDistance[_routeDistance.Length-1]:0;

        public static ZackBacktrackLook Play(CharacterMotor motor,Vector3 from,Vector3 to,Vector3[] path)
        {
            if(motor==null)return null;
            if(Live.TryGetValue(motor.PlayerSlot,out var old)&&old!=null)old.Finish();
            var v=new GameObject("ZackBacktrackPull").AddComponent<ZackBacktrackLook>();
            v._motor=motor;v._from=from;v._to=to;v._born=Time.time;v._serial=motor.PresentationTeleportSerial;
            v._round=GameServices.Match?.RoundNumber??0;v._model=motor.GetComponent<CharacterVisual>()?.Model?.transform;
            v._body=ZackFx.Echo.Of(motor.PlayerSlot,v.transform);
            if(v._body!=null)
            {
                v._poseNow=v._body.Track.Newest;v._body.PoseAt(v._poseNow);v._body.Rim(.5f);
                v._modelOffset=v._body.Copy.Root.transform.position-from;v._modelOffset.y=0;
                foreach(var r in v._body.Copy.Renderers)if(r!=null)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                v.CopyShoe();
                // His seat ring and tag are not hidden: `CharacterNameplate` carries them along with the drawn body.
                var surfaces=new List<Renderer>();
                foreach(var r in motor.GetComponentsInChildren<Renderer>(true))if(r.GetComponentInParent<CharacterNameplate>()==null)surfaces.Add(r);
                var held=motor.GetComponent<Carrier>()?.Held;if(held!=null)surfaces.AddRange(held.GetComponentsInChildren<Renderer>(true));
                v._hidden=surfaces.ToArray();v._wasHidden=new bool[surfaces.Count];
                for(int i=0;i<surfaces.Count;i++){v._wasHidden[i]=surfaces[i].forceRenderingOff;surfaces[i].forceRenderingOff=true;}
            }
            if(!v.RouteFromHistory())v.RouteFromPath(path);
            v._tetherInk=ZackFx.Line(v.transform,"PullTetherInk",28,.1f,ZackFx.Ink,0);
            v._tether=ZackFx.Line(v.transform,"PullTether",28,.04f,ZackFx.Core,1.6f);
            v._wakeInk=ZackFx.Line(v.transform,"PullWakeInk",16,.15f,ZackFx.Ink,0);
            v._wake=ZackFx.Line(v.transform,"PullWake",16,.07f,ZackFx.Body,1.4f);
            v._crown=ZackFx.Line(v.transform,"PullWakeCrown",16,.03f,ZackFx.Core,1.4f);
            v._ringInk=ZackFx.Line(v.transform,"AnchorRingInk",24,.09f,ZackFx.Ink,0);
            v._ring=ZackFx.Line(v.transform,"AnchorRing",24,.045f,ZackFx.Body,1.3f);
            v._leave=ZackFx.Line(v.transform,"DepartureRing",20,.05f,ZackFx.Core,1.5f);
            v._ring.loop=v._ringInk.loop=v._leave.loop=true;
            ZackFx.Sparks(from+Vector3.up*.5f,.4f,5,1.3f);
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

        private static float Snag(float age)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(age/SnagSeconds));
        private static float Travel(float age)=>Mathf.Clamp01((age-SnagSeconds)/TravelSeconds);
        // Yanked: fastest at the start, slowing into the catch. A small hitch during the snag starts him moving.
        private static float Progress(float age)=>.025f*Snag(age)+.975f*(1-Mathf.Pow(1-Travel(age),2.2f));
        private Vector3 FeetAt(float age)
        {
            var p=Route(Progress(age),out _,out _);if(_motor==null)return p;
            float merge=age>=PullSeconds?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1,Travel(age)));
            return Vector3.Lerp(p,_motor.transform.position,merge);
        }

        private void CopyShoe()
        {
            var held=_motor.GetComponent<Carrier>()?.Held;var liveHand=_motor.GetComponent<CharacterVisual>()?.HandAnchor;
            Transform grip=null;foreach(var bone in _body.Copy.Bones)if(bone.name=="HandAnchor"){grip=bone;break;}
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
        private void LateUpdate()
        {
            if(_motor==null||_motor.AbilitySystem?.HeroId!="zack"||_motor.IsTagged||_motor.IsStunned||_motor.IsTripped
                ||GameServices.Round?.RoundActive!=true||(GameServices.Match?.RoundNumber??0)!=_round||_motor.PresentationTeleportSerial!=_serial)
            {Finish();return;}Step();if(Age>PullSeconds+CatchSeconds+RingHold)Finish();
        }

        // ------------------------------------------------------------------ one frame of the look

        private void Step()
        {
            float age=Age,snag=Snag(age),x=Travel(age),e=Progress(age),c=Mathf.Clamp01((age-PullSeconds)/CatchSeconds);
            bool travelling=age<PullSeconds,catching=!travelling&&c<1,eyes=ZackFx.InOwnFirstPerson(_motor);
            bool calm=Settings.SettingsStore.Current.ReducedEffects;
            var feet=FeetAt(age);Route(e,out float time,out var heading);
            float lift=travelling?.2f*Mathf.Sin(Mathf.PI*x):0;
            if(_body!=null)
            {
                var track=_body.Track;
                // The run plays backwards in step with the route, then the catch takes up his live stance.
                float poseTime=catching||!travelling?track.Newest:age<SnagSeconds?_poseNow:time;
                _body.PoseAt(poseTime);
                var root=_body.Copy.Root.transform;
                var live=_model!=null?_model.rotation:root.rotation;
                var facing=catching?live:Quaternion.Slerp(root.rotation,live,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1,x)));
                // Pulled from the waist: the head and arms trail toward where he is leaving, then he settles forward on the catch.
                float lean=travelling?Mathf.Lerp(16*snag,24,Mathf.Sin(Mathf.PI*Mathf.Min(1,x*1.4f)))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,x)))
                    :-7*Mathf.Sin(Mathf.PI*c);
                var dir=heading.sqrMagnitude>0?heading:(_to-_from).normalized;dir.y=0;
                float rad=lean*Mathf.Deg2Rad;var up=(Vector3.up*Mathf.Cos(rad)-dir*Mathf.Sin(rad)).normalized;
                var tilt=Quaternion.FromToRotation(Vector3.up,up);
                var basePos=feet+_modelOffset+Vector3.up*(lift-.05f*Mathf.Sin(Mathf.PI*c));
                var pivot=basePos+Vector3.up*HipHeight;
                root.SetPositionAndRotation(pivot+tilt*(basePos-pivot),tilt*facing);
                float squash=Mathf.Sin(Mathf.PI*c);
                root.localScale=Vector3.Scale(root.localScale,new Vector3(1+.09f*squash,1-.11f*squash,1+.09f*squash));
                float flicker=calm?0:.25f*Mathf.Abs(Mathf.Sin(age*95));
                _body.Rim(travelling?Mathf.Lerp(.5f,.75f,snag)+flicker:Mathf.Lerp(1,.25f,c));
                BodyDrawn=c<1&&!eyes;_body.Visible(BodyDrawn);
                foreach(var r in _body.Copy.Root.GetComponentsInChildren<MeshRenderer>())if(r.name=="HeldShoeSurface")r.forceRenderingOff=!BodyDrawn;
                if(c<1){foreach(var r in _hidden)if(r!=null)r.forceRenderingOff=true;}else Restore();
            }
            // The tether: from his waist along the route still to come, into the anchor ring. It shoots out during the snag.
            int seed=Mathf.FloorToInt(age*40);float reach=Mathf.Lerp(e,1,snag);
            var waist=feet+Vector3.up*(WaistHeight+lift);
            for(int i=0;i<28;i++)
            {
                float k=i/27f;var p=Route(Mathf.Lerp(e,reach,k),out _,out var h);
                p+=Vector3.up*Mathf.Lerp(WaistHeight+lift,.08f,k*k);if(i==0)p=waist;
                var side=Vector3.Cross(h.sqrMagnitude>0?h:Vector3.forward,Vector3.up);
                float knee=i==0||i==27||calm?0:((i+seed)%2==0?1:-1)*.07f*(.4f+.6f*Mathf.Abs(Mathf.Sin(i*2.3f+seed*1.7f)));
                p+=side*knee+Vector3.up*knee*.5f;_tether.SetPosition(i,p);_tetherInk.SetPosition(i,p-Vector3.up*.01f);
            }
            _tether.enabled=_tetherInk.enabled=travelling&&RouteLength>.1f;
            // The wake: a short streak along the route he has just been pulled through.
            float tail=Mathf.Max(0,e-.22f);
            for(int i=0;i<16;i++)
            {
                float k=i/15f;var p=Route(Mathf.Lerp(tail,e,k),out _,out _);float rise=lift*k;
                _wake.SetPosition(i,p+Vector3.up*(WaistHeight+rise));_wakeInk.SetPosition(i,p+Vector3.up*(WaistHeight+rise-.01f));
                _crown.SetPosition(i,p+Vector3.up*(1.45f+rise+.04f*Mathf.Sin(i*2.9f+age*80)));
            }
            _wake.widthMultiplier=.07f*WakeWidth(x);_wakeInk.widthMultiplier=.15f*WakeWidth(x);_crown.widthMultiplier=.03f*WakeWidth(x);
            _wake.enabled=_wakeInk.enabled=_crown.enabled=travelling&&x>0;
            // The anchor ring tightens as he nears it, then flashes outward on the catch.
            float radius=travelling?Mathf.Lerp(.9f,.5f,e):Mathf.Lerp(.5f,1.15f,Mathf.Sqrt(Mathf.Clamp01((age-PullSeconds)/RingHold)));
            float alpha=travelling?.55f+.4f*snag:Mathf.Clamp01(1-(age-PullSeconds)/RingHold);
            for(int i=0;i<24;i++)
            {
                float a=i/24f*Mathf.PI*2,wobble=calm?0:.035f*Mathf.Sin(i*3.1f+seed);
                var ring=_to+new Vector3(Mathf.Cos(a)*(radius+wobble),.055f,Mathf.Sin(a)*(radius+wobble));
                _ring.SetPosition(i,ring);_ringInk.SetPosition(i,ring-Vector3.up*.01f);
            }
            ZackFx.Tint(_ring,new Color(ZackFx.Body.r,ZackFx.Body.g,ZackFx.Body.b,alpha));
            ZackFx.Tint(_ringInk,new Color(ZackFx.Ink.r,ZackFx.Ink.g,ZackFx.Ink.b,alpha));
            // The departure snaps shut where he stood.
            float shut=Mathf.Clamp01(age/.22f);
            for(int i=0;i<20;i++)
            {
                float a=i/20f*Mathf.PI*2,r=Mathf.Lerp(.85f,.08f,shut);
                _leave.SetPosition(i,_from+new Vector3(Mathf.Cos(a)*r,.06f,Mathf.Sin(a)*r));
            }
            _leave.enabled=shut<1;ZackFx.Tint(_leave,new Color(ZackFx.Core.r,ZackFx.Core.g,ZackFx.Core.b,1-shut*shut));
            if(!_landed&&!travelling)
            {
                _landed=true;ZackFx.Sparks(_to+Vector3.up*.15f,.5f,6,2.1f);
                GameServices.Audio?.PlayAt("sfx_zack_backtrack_land",_to);
            }
        }
        private static float WakeWidth(float x)=>Mathf.Clamp01(x*6)*(1-.5f*x);
        private void Restore()
        {
            if(_restored||_hidden==null)return;_restored=true;
            for(int i=0;i<_hidden.Length;i++)if(_hidden[i]!=null)_hidden[i].forceRenderingOff=_wasHidden[i];
        }
        private void Finish(){Restore();if(_motor!=null&&Live.TryGetValue(_motor.PlayerSlot,out var v)&&v==this)Live.Remove(_motor.PlayerSlot);Destroy(gameObject);}
        private void OnDestroy(){Restore();_body?.Destroy();if(_motor!=null&&Live.TryGetValue(_motor.PlayerSlot,out var v)&&v==this)Live.Remove(_motor.PlayerSlot);}
    }
}
