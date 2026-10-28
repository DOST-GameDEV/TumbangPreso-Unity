using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // Render-only rewind. The accepted teleport still owns gameplay; this body, eye and marker offset own the presentation.
    // One travelling body and one hologram rewind through the recorded route. Layered contours belong to the hologram
    // material, giving a dense temporal smear without independently moving past actors. Small electrical bursts mark
    // the activation and catch; gameplay still uses the accepted instant teleport.
    //
    // PAST MOMENTS (owner, 2026-10-26: "see other moments of Zack's past that he merges with during the rewind"). Up to three
    // frozen moments from the real route stand where he really was, in the pose and facing he really had, drawn in the same
    // approved electrical outline (fainter the older they are). They are chosen at the route's real corners first, then evenly,
    // never closer than 1.2 m to each other, his body or the landing, so they read as separate beats instead of a muddy lineup.
    // They stamp in during the reveal; as the rewinding body reaches each one it flashes, swells a touch and is absorbed with a
    // crack of sparks. The moving lead hologram still marks the destination and merges at the catch.
    [DefaultExecutionOrder(2500)]
    public sealed class ZackBacktrackLook : MonoBehaviour
    {
        public const float RevealSeconds = .06f, TravelSeconds = .42f, CatchSeconds = .14f;
        /// <summary>The catch: the body reaches the old spot and the landing cue plays. Matches the 0.46 s pull cue.</summary>
        public const float PullSeconds = RevealSeconds + TravelSeconds;
        private const float Linger = .16f, LeadStart = .5f, MergeFlash = .12f, StreakLife = .09f;
        private const int Streaks = 14, StreakPoints = 5;
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
        private const int MaxPast = 3;
        private const float PastSpacing = 1.2f, PastAbsorb = .07f;
        private ZackFx.Echo[] _past = new ZackFx.Echo[0];
        private float[] _pastAt = new float[0], _pastMergedAt = new float[0];
        private Vector3[] _pastFeet = new Vector3[0], _pastScale = new Vector3[0];
        /// <summary>How many past moments stand on the route this frame (not yet absorbed), for review traces.</summary>
        public int PastMomentsDrawn { get; private set; }
        public int PastMomentsChosen => _past.Length;
        /// <summary>Read-only past-moment state for traces and lifecycle checks: feet, the route share it sits at, absorbed.</summary>
        public bool TryPastMoment(int index, out Vector3 feet, out float at, out bool absorbed)
        {
            feet = Vector3.zero; at = 0; absorbed = false;
            if (index < 0 || index >= _past.Length) return false;
            feet = _pastFeet[index]; at = _pastAt[index]; absorbed = _pastMergedAt[index] >= 0;
            return true;
        }
        private readonly Vector4[] _echoTrail = new Vector4[5];
        private LineRenderer[] _streaks = new LineRenderer[0];
        private float[] _streakBorn = new float[0];
        private Renderer[] _hidden;
        private bool[] _wasHidden;
        private bool _restored, _landed;
        public float Age => Mathf.Max(0,Time.time-_born);
        public bool Pulling => _motor!=null && Age<PullSeconds;
        public Vector3 VisualFeet => FeetAt(Age) + CatchOffset(Mathf.Clamp01((Age-PullSeconds)/CatchSeconds));
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
            bool calm=Settings.SettingsStore.Current.ReducedEffects;
            if(v._body!=null)v.ChoosePastMoments(calm?1:MaxPast);
            ZackFx.Sparks(to+Vector3.up*.15f,.22f,calm?1:3,1.3f);
            // The kick-off: a crack of sparks at his feet where the present lets go of him.
            ZackFx.Sparks(from+Vector3.up*.2f,.35f,calm?2:5,.6f);
            if(!calm)
            {
                v._streaks=new LineRenderer[Streaks];v._streakBorn=new float[Streaks];
                for(int i=0;i<Streaks;i++)
                {
                    v._streaks[i]=ZackFx.Line(v.transform,"RewindStreak"+i,StreakPoints,.012f+.005f*(i%3),i%2==0?ZackFx.Core:ZackFx.Body,1.5f);
                    v._streaks[i].widthCurve=new AnimationCurve(new Keyframe(0,.08f),new Keyframe(.3f,1),new Keyframe(.7f,.7f),new Keyframe(1,.04f));
                    v._streaks[i].enabled=false;v._streakBorn[i]=-1;
                }
            }
            Live[motor.PlayerSlot]=v;v.Step();return v;
        }
        public void SealTeleport(){if(_motor!=null)_serial=_motor.PresentationTeleportSerial;}
        public static Vector3 EyeOffset(CharacterMotor motor)
            => motor!=null&&Live.TryGetValue(motor.PlayerSlot,out var v)&&v!=null&&v._motor==motor&&v.Pulling
                ?v.VisualFeet-motor.transform.position:Vector3.zero;
        /// <summary>
        /// Degrees added to his own first-person lens: the view yawns wide as he is rewound down the route (speed reads in
        /// the periphery), then punches in on the catch. Zero for every other body and after the settle.
        /// </summary>
        public static float FovKick(CharacterMotor motor)
        {
            if(motor==null||!Live.TryGetValue(motor.PlayerSlot,out var v)||v==null||v._motor!=motor
                ||Settings.SettingsStore.Current.ReducedEffects)return 0;
            float age=v.Age;
            if(age<RevealSeconds)return -3*Mathf.Sin(Mathf.PI*age/RevealSeconds);
            if(age<PullSeconds)return 8*Mathf.Sin(Mathf.PI*Mathf.Pow(Travel(age),.7f));
            float c=Mathf.Clamp01((age-PullSeconds)/CatchSeconds);
            return -6*Mathf.Sin(Mathf.PI*c)*(1-c);
        }
        /// <summary>The flat offset of the drawn body from the gameplay body, for its seat ring and name tag.</summary>
        public static Vector3 MarkerOffset(CharacterMotor motor)
        {
            if(motor==null||!Live.TryGetValue(motor.PlayerSlot,out var v)||v==null||v._motor!=motor||v._restored)return Vector3.zero;
            // Evaluate the same frame as the body, including its catch. Reading the rendered root here would lag one frame.
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

        // ------------------------------------------------------------------ past moments

        private void ChoosePastMoments(int most)
        {
            float length=RouteLength;
            if(_route==null||_route.Length<3||length<PastSpacing*2f)return;
            // Candidates: every route corner (heading change), sharpest first, then the even thirds of the route.
            var candidates=new List<(float share,float score)>();
            for(int i=1;i<_route.Length-1;i++)
            {
                var a=_route[i]-_route[i-1];var b=_route[i+1]-_route[i];a.y=b.y=0;
                if(a.sqrMagnitude<1e-4f||b.sqrMagnitude<1e-4f)continue;
                float turn=Vector3.Angle(a,b);
                if(turn>25f)candidates.Add((_routeDistance[i]/length,turn));
            }
            candidates.Sort((x,y)=>y.score.CompareTo(x.score));
            foreach(float even in new[]{.33f,.58f,.8f})candidates.Add((even,0));
            var chosen=new List<float>();
            foreach(var c in candidates)
            {
                if(chosen.Count>=most)break;
                float d=c.share*length;
                if(d<PastSpacing||length-d<PastSpacing)continue;
                bool clear=true;foreach(float other in chosen)if(Mathf.Abs(other*length-d)<PastSpacing){clear=false;break;}
                if(clear)chosen.Add(c.share);
            }
            chosen.Sort();
            _past=new ZackFx.Echo[chosen.Count];_pastAt=chosen.ToArray();_pastMergedAt=new float[chosen.Count];_pastFeet=new Vector3[chosen.Count];_pastScale=new Vector3[chosen.Count];
            for(int k=0;k<chosen.Count;k++)
            {
                _pastMergedAt[k]=-1;
                var echo=ZackFx.Echo.Of(_motor.PlayerSlot,transform);
                if(echo==null)continue;
                var feet=Route(chosen[k],out float then,out _);
                echo.PoseAt(then);
                var root=echo.Copy.Root.transform;
                root.SetPositionAndRotation(feet+_modelOffset,root.rotation);
                CopyShoe(echo);
                echo.Past(.25f+.15f*k,0);
                _past[k]=echo;_pastFeet[k]=feet;_pastScale[k]=root.localScale;
            }
        }

        private void StepPastMoments(float age,float e,bool eyes,Vector3 eye,bool calm)
        {
            PastMomentsDrawn=0;
            for(int k=0;k<_past.Length;k++)
            {
                var echo=_past[k];if(echo==null)continue;
                // Stamped in one after another during the reveal, nearest first.
                float enter=Mathf.Clamp01((age-.015f*k)/Mathf.Max(.02f,RevealSeconds));
                if(_pastMergedAt[k]<0&&e>=_pastAt[k])
                {
                    _pastMergedAt[k]=age;
                    ZackFx.Sparks(_pastFeet[k]+Vector3.up*.9f,.32f,calm?1:3,k*3.1f+age);
                }
                float absorb=_pastMergedAt[k]<0?0:Mathf.Clamp01((age-_pastMergedAt[k])/PastAbsorb);
                float near=eyes?Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.1f,1.8f,Vector3.Distance(_pastFeet[k]+Vector3.up*1.2f,eye))):1;
                // Absorbed: one bright beat as he passes through it, then gone.
                float glow=absorb>0?Mathf.Lerp(1.5f,0,absorb):1;
                float visible=enter*glow*near*(age<PullSeconds+.02f?1:0);
                echo.Past(.25f+.15f*k,visible);
                var root=echo.Copy.Root.transform;
                root.localScale=_pastScale[k]*(1+.15f*Mathf.Sin(Mathf.PI*absorb));
                bool on=visible>.015f;Show(echo,on);if(on&&absorb<=0)PastMomentsDrawn++;
            }
        }

        // ------------------------------------------------------------------ timing

        private static float Travel(float age)=>Mathf.Clamp01((age-RevealSeconds)/TravelSeconds);
        // A reel rewinding: it spins up quickly, runs, and eases into the catch.
        private static float Progress(float age){float x=Travel(age);return .02f*Mathf.SmoothStep(0,1,Mathf.Clamp01(age/RevealSeconds))+.98f*(.4f*x+.6f*x*x*(3-2*x));}
        // How far ahead (as a share of the route still to go) the hologram runs: it springs out of him during the reveal,
        // then the gap closes steadily until it is zero at the catch.
        private static float Lead(float age)
            =>LeadStart*Mathf.SmoothStep(0,1,Mathf.Clamp01(age/(RevealSeconds+.05f)))*Mathf.Pow(1-Travel(age),1.3f);
        // 0 to 1: how fast the rewind is going right now, for stretch and streaks.
        private static float Speed(float age){float x=Travel(age);return age<RevealSeconds||x>=1?0:Mathf.Clamp01((.4f+3.6f*x*(1-x))/1.3f);}
        private Vector3 CatchOffset(float c)
        {
            Route(1,out _,out var arrival);
            return arrival*(.26f*Mathf.Sin(Mathf.PI*Mathf.Min(1,c*1.6f))*(1-c));
        }
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
                // Speed: stretched along the line of travel at full rewind (he moves mostly along his own forward axis,
                // backwards), thinned a little to keep the blocky volume. The catch overshoots the old spot by a hand and
                // springs back with a crouch, so the stop has weight.
                float speed=calm?0:Speed(age),settle=Mathf.Sin(Mathf.PI*c);
                var overshoot=CatchOffset(c);
                root.SetPositionAndRotation(feet+_modelOffset+overshoot-Vector3.up*.06f*settle,facing);
                root.localScale=Vector3.Scale(root.localScale,new Vector3((1-.07f*speed)*(1+.12f*settle),(1-.05f*speed)*(1-.16f*settle),(1+.3f*speed)*(1+.12f*settle)));
                // Each past moment he folds into brightens his rim; the catch is the last and brightest.
                _body.Rim(travelling?.35f+.35f*e:Mathf.Lerp(1,.2f,Mathf.Sqrt(c)));
                BodyDrawn=c<1&&!eyes;Show(_body,BodyDrawn);
                if(c<1){foreach(var r in _hidden)if(r!=null)r.forceRenderingOff=true;}else Restore();
            }
            // The single hologram is his past running ahead of him. It springs out of his body toward the old spot at the
            // cast, stays a stride or two ahead down the route playing its own reversed run, and he closes on it until they
            // merge exactly at the catch. Its contour layers fill the gap back toward him, so one figure reads as many and
            // the gap itself shows the rewind. On the first leg "ahead" is toward an observer, so nothing hides it.
            MomentsDrawn=0;
            if(_moments.Length>0)
            {
                var echo=_moments[0];
                float lead=Lead(age),ahead=e+(1-e)*lead;
                var p=Route(ahead,out float then,out var forward);
                // Out of phase: when the route runs straight at this screen's camera the hologram would sit on top of him,
                // so it slips sideways on screen in proportion to that alignment, and slides back into him as the gap closes.
                var view=UnityEngine.Camera.main;
                if(view!=null&&!eyes&&forward.sqrMagnitude>0)
                {
                    var look=view.transform.forward;look.y=0;
                    float aligned=look.sqrMagnitude>1e-4f?Mathf.Abs(Vector3.Dot(look.normalized,forward)):0;
                    var right=view.transform.right;right.y=0;
                    p+=right.normalized*(.8f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.9f,aligned))*(lead/LeadStart));
                }
                echo.PoseAt(then);var root=echo.Copy.Root.transform;
                root.SetPositionAndRotation(p+_modelOffset,root.rotation);
                // Merged: one bright beat as the past closes over him, then it is gone.
                float merged=Mathf.Clamp01((age-PullSeconds)/MergeFlash);
                float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/RevealSeconds));
                float glow=travelling?1f:Mathf.Lerp(1.4f,0,merged);
                float near=eyes?Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.1f,1.8f,Vector3.Distance(p+Vector3.up*1.2f,eye))):1;
                echo.Past(.1f,enter*glow*near);
                for(int layer=0;layer<_echoTrail.Length;layer++)
                {
                    // Layer 0 is the hologram; the rest step back toward his body, fainter the closer they are to him.
                    // Keep the base projection at its readable screen offset; only the trailing contours follow the route.
                    float spacing=Mathf.Lerp(.08f,.2f,Speed(age))*(1-merged);
                    var q=layer==0?p:Route(Mathf.Clamp01(e-layer*spacing/Mathf.Max(.1f,length)),out _,out _);
                    float visible=layer==0?1:calm?0:Mathf.Pow(.62f,layer)*(1-merged);
                    _echoTrail[layer]=new Vector4(q.x-p.x,q.y-p.y,q.z-p.z,visible);
                }
                echo.Trail(_echoTrail);
                bool on=enter*glow*near>.015f;Show(echo,on);MomentsDrawn=on?1:0;
            }
            StepPastMoments(age,e,eyes,eye,calm);
            // Short electrical wisps sample the traversed route, including its bends. In first person they occupy the
            // periphery and sweep across the turn instead of remaining parallel to the camera's forward axis.
            Route(e,out _,out var along);
            float rush=calm?0:Speed(age);
            for(int i=0;i<_streaks.Length;i++)
            {
                var streak=_streaks[i];float life=age-_streakBorn[i];
                if((_streakBorn[i]<0||life>StreakLife)&&travelling&&rush>.15f&&along.sqrMagnitude>0)
                {
                    WriteStreak(streak,i,age,e,rush,eyes,eye,along);
                    _streakBorn[i]=age;life=0;
                }
                bool on=_streakBorn[i]>=0&&life<=StreakLife&&age<PullSeconds+.03f;
                streak.enabled=on;
                if(on){var tint=i%2==0?ZackFx.Core:ZackFx.Body;ZackFx.Tint(streak,new Color(tint.r,tint.g,tint.b,(1-life/StreakLife)*.9f));}
            }
            if(!_landed&&!travelling)
            {
                _landed=true;
                ZackFx.Sparks(_to+Vector3.up*.12f,.38f,calm?2:5,2.1f);
                if(!calm)ZackFx.Sparks(_to+Vector3.up*1.1f,.3f,3,4.7f);
                GameServices.Audio?.PlayAt("sfx_zack_backtrack_land",_to);
            }
        }
        private void WriteStreak(LineRenderer streak,int index,float age,float progress,float rush,bool eyes,Vector3 eye,Vector3 along)
        {
            float h=Hash(index,Mathf.FloorToInt(age*90)),g=Hash(index+31,Mathf.FloorToInt(age*70));
            var camera=UnityEngine.Camera.main;
            var side=eyes&&camera!=null?camera.transform.right:Vector3.Cross(along,Vector3.up).normalized;
            var up=eyes&&camera!=null?camera.transform.up:Vector3.up;
            float spin=h*Mathf.PI*2,radius=eyes?Mathf.Lerp(.65f,1.15f,g):Mathf.Lerp(.35f,.9f,g);
            var offset=(side*Mathf.Cos(spin)+up*Mathf.Sin(spin)*.8f)*radius;
            float height=eyes?Mathf.Clamp(eye.y-FeetAt(age).y,.8f,2f):.9f;
            var forward=eyes&&camera!=null?camera.transform.forward*1.15f:Vector3.zero;
            float behind=Mathf.Lerp(.12f,eyes?.85f:.65f,Hash(index+57,Mathf.FloorToInt(age*50)));
            float reach=Mathf.Lerp(.32f,eyes?.72f:1.05f,g)*rush;
            // Retain a full route fragment at departure instead of collapsing its points into an arrow-shaped stamp.
            float anchor=Mathf.Max(reach,progress*RouteLength-behind);
            for(int j=0;j<StreakPoints;j++)
            {
                float u=j/(StreakPoints-1f);
                var p=Route(Mathf.Clamp01((anchor-reach*u)/Mathf.Max(.1f,RouteLength)),out _,out _);
                float curl=Mathf.Sin(Mathf.PI*u)*(.04f+.05f*g);
                float knee=j==0||j==StreakPoints-1?0:(Hash(index*7+j,Mathf.FloorToInt(age*90))-.5f)*.06f;
                p+=Vector3.up*height+offset+forward+side*curl*(index%2==0?1:-1)+up*knee;
                streak.SetPosition(j,p);
            }
        }
        private static float Hash(int a,int b){float v=Mathf.Sin(a*127.1f+b*311.7f)*43758.5453f;return v-Mathf.Floor(v);}
        private void Restore()
        {
            if(_restored||_hidden==null)return;_restored=true;
            for(int i=0;i<_hidden.Length;i++)if(_hidden[i]!=null)_hidden[i].forceRenderingOff=_wasHidden[i];
        }
        private void Finish(){Restore();if(_motor!=null&&Live.TryGetValue(_motor.PlayerSlot,out var v)&&v==this)Live.Remove(_motor.PlayerSlot);Destroy(gameObject);}
        private void OnDestroy()
        {
            Restore();_body?.Destroy();foreach(var m in _moments)m?.Destroy();foreach(var m in _past)m?.Destroy();
            if(_motor!=null&&Live.TryGetValue(_motor.PlayerSlot,out var v)&&v==this)Live.Remove(_motor.PlayerSlot);
        }
    }
}
