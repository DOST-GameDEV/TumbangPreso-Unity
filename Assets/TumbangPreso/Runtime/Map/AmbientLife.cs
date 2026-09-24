using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TumbangPreso
{
    /// <summary>Sparse, non-colliding neighborhood animals on authored clear routes.</summary>
    public sealed partial class AmbientLife : MonoBehaviour
    {
        [Serializable] public sealed class Animal
        {
            public string Id;
            public GameObject Model;
            public AnimationClip[] Clips;
            public Vector3[] Route;
            public bool Bird;
            public bool AerialWander;
            public Bounds FlightBounds;
            public Vector2 GlideDuration=new Vector2(.8f,2.4f);
            public float WalkSpeed=.65f,RunSpeed=3.6f;
            public float WalkCycleSpeed=.391f,RunCycleSpeed=1.278f;
            public int PeeWaypoint=-1;
            public Vector3 PeeTarget;
            public HabitatNode[] Habitat=Array.Empty<HabitatNode>();
            public ActivitySite[] Activities=Array.Empty<ActivitySite>();
            public bool QuietCat;
        }
        public Animal[] Animals=Array.Empty<Animal>();
        private Actor[] _actors;
        private System.Random _random;
        private Material _peeMaterial;
        public int ImpactReactions { get; private set; }
        private void OnEnable() => Visual.MatchFlair.Presented += OnImpact;
        private void OnDisable() => Visual.MatchFlair.Presented -= OnImpact;
        private void OnImpact(Visual.MatchFlair.Kind kind,int actor,int subject,Vector3 at,float strength)
        {
            if (_actors == null || GameServices.Round == null || !GameServices.Round.RoundActive) return;
            float radius = kind == Visual.MatchFlair.Kind.LataDown ? 6 :
                kind == Visual.MatchFlair.Kind.Thunder ? 12 : kind == Visual.MatchFlair.Kind.IceShatter ? 5 : 0;
            if (radius <= 0) return;
            // Existing peer-visible accepted outcomes, not speculative input or replay
            // callbacks. Per-animal quiet time keeps repeated field pulses from spam.
            foreach (var animal in _actors) if (animal.ReactToImpact(at,radius)) ImpactReactions++;
        }
        public int VisibleAnimals
        {
            get{int count=0;if(_actors!=null)foreach(var actor in _actors)if(actor.Visible)count++;return count;}
        }
        private void Start()
        {
            // A private stream makes visits unpredictable without changing a
            // throw, bot decision, match seed or another shared gameplay stream.
            _random=new System.Random(Guid.NewGuid().GetHashCode());
            _actors=new Actor[Animals.Length];
            for(int i=0;i<Animals.Length;i++)_actors[i]=new Actor(this,Animals[i],i);
        }
        private float Range(float a,float b)=>a+(float)_random.NextDouble()*(b-a);
        private void LateUpdate()
        {
            if(_actors==null)return;
            float dt=Time.deltaTime;if(dt<=0)return;
            foreach(var actor in _actors)actor.Step(dt);
        }
        private void OnDestroy()
        {if(_actors!=null)foreach(var actor in _actors)actor?.Dispose();if(_peeMaterial!=null)Destroy(_peeMaterial);}
#if UNITY_EDITOR
        public string DescribeForReview(string id)
        {for(int i=0;i<Animals.Length;i++)if(Animals[i].Id==id)return _actors[i].Description;return "missing";}
        // Stages art/proximity evidence only. Shipped visits keep their private
        // random scheduling; the test still exercises the real nearby-player path.
        public void StageBirdVisitForReview(string id)
        {
            if(_actors==null)throw new InvalidOperationException("Ambient actors have not started");
            for(int i=0;i<Animals.Length;i++)if(Animals[i].Id==id&&Animals[i].Bird){_actors[i].StageBird();return;}
            throw new InvalidOperationException("Unknown bird "+id);
        }
        public void StageDogPauseForReview(string id)
        {
            for(int i=0;i<Animals.Length;i++)if(Animals[i].Id==id){_actors[i].StageDogPause();return;}
            throw new InvalidOperationException("Unknown dog "+id);
        }
#endif

        private sealed partial class Actor
        {
            private readonly AmbientLife _owner;
            private readonly Animal _data;
            private readonly Transform _root;
            private readonly Quaternion _facing;
            private PlayableGraph _graph;
            private AnimationMixerPlayable _mixer;
            private AnimationClipPlayable _current,_previous;
            private AnimationClip _clip,_oldClip;
            private float _clipTime,_oldTime,_blend=1,_wait,_panic,_speed,_fidgetWait;
            private int _target=1,_direction=1,_birdPhase;
            private string _action;
            private Vector3 _flightFrom,_flightTo;
            private float _flightTime,_flightDuration;
            private Vector3 _flightControl1,_flightControl2;
            private bool _gliding;
            private float _wingWait,_glideHoldTime,_bank;
            private int _flightLegs;
            private float _peeCooldown,_peeTime;
            private float _impactQuietUntil;
            private float _raisedLegSide=1;
            private bool _peeing;
            private LineRenderer _peeLine;
            public bool Visible=>_root!=null&&_root.gameObject.activeSelf;
            public Actor(AmbientLife owner,Animal data,int index)
            {
                _owner=owner;_data=data;
                if(data.Model==null||data.Route==null||data.Route.Length<2)throw new InvalidOperationException("Incomplete ambient route: "+data.Id);
                _root=new GameObject("Ambient "+data.Id).transform;_root.SetParent(owner.transform,false);
                _root.position=data.Route[0];
                var model=Instantiate(data.Model,_root);model.name=data.Id;
                model.transform.localPosition=Vector3.zero;
                Transform forward=null;
                foreach(var child in model.GetComponentsInChildren<Transform>(true))if(child.name=="LookForward"){forward=child;break;}
                var direction=forward!=null?forward.position-model.transform.position:Vector3.forward;
                _facing=Quaternion.FromToRotation(direction.normalized,Vector3.forward);
                model.transform.localRotation=_facing*model.transform.localRotation;
                var animator=model.GetComponentInChildren<Animator>()??model.AddComponent<Animator>();
                animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                _graph=PlayableGraph.Create("Ambient "+data.Id);_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _mixer=AnimationMixerPlayable.Create(_graph,2);
                var output=AnimationPlayableOutput.Create(_graph,"Motion",animator);output.SetSourcePlayable(_mixer);_graph.Play();
                Play("idle");
                _mixer.SetInputWeight(1,1);_current.SetTime(0);_graph.Evaluate(0);
                // Imported animation bounds include the stride envelope. Using
                // that box left every dog5-7cm above the ground. Seat the actual
                // neutral skin once; authored root motion preserves its contact.
                var mesh=new Mesh();float bottom=float.PositiveInfinity;
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {skin.BakeMesh(mesh);foreach(var vertex in mesh.vertices)bottom=Mathf.Min(bottom,skin.transform.TransformPoint(vertex).y);}
                Destroy(mesh);
                if(!float.IsInfinity(bottom))model.transform.position+=Vector3.up*(_root.position.y-bottom);
                // glTF handedness can swap the visible side of named limbs.
                // Face the surface with the actual imported raised hind leg.
                foreach(var bone in model.GetComponentsInChildren<Transform>())
                    if(bone.name=="BackR")_raisedLegSide=Mathf.Sign(Vector3.Dot(bone.position-_root.position,_root.right));
                _clipTime=owner.Range(0,_clip.length);
                _wait=owner.Range(data.Bird?3:1,data.Bird?20:9)+index*.7f;
                _peeCooldown=owner.Range(90,180);
                InitializeHabitat(model);
                if(data.Bird)_root.gameObject.SetActive(false);
                if(data.AerialWander)
                {
                    if(!data.Bird||data.FlightBounds.size.x<=0||data.FlightBounds.size.y<=0||data.FlightBounds.size.z<=0)
                        throw new InvalidOperationException("Invalid aerial habitat: "+data.Id);
                    _root.rotation=Quaternion.LookRotation((data.Route[1]-data.Route[0]).normalized);
                    _wait=owner.Range(.2f,1.8f)+index*.7f;
                    foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            private AnimationClip FindClip(string name)
            {foreach(var clip in _data.Clips)if(clip!=null&&clip.name==name)return clip;return null;}
            private void Play(string action)
            {
                if(action==_action)return;
                var clip=FindClip(action);if(clip==null)throw new InvalidOperationException(_data.Id+" has no "+action+" clip");
                if(_previous.IsValid()){_graph.Disconnect(_mixer,0);_graph.DestroyPlayable(_previous);}
                _previous=_current;_oldClip=_clip;_oldTime=_clipTime;
                if(_previous.IsValid()){_graph.Disconnect(_mixer,1);_mixer.ConnectInput(0,_previous,0);}
                _current=AnimationClipPlayable.Create(_graph,clip);_current.SetApplyFootIK(false);_current.SetSpeed(0);
                _current.SetDuration(double.MaxValue);_mixer.ConnectInput(1,_current,0);
                _clip=clip;_clipTime=0;_action=action;_blend=_previous.IsValid()?0:1;
            }
            private bool Threat(out Vector3 nearest)
            {
                nearest=Vector3.zero;float best=(_data.Bird?2.8f:3.0f);best*=best;bool found=false;
                if(GameServices.Round==null)return false;
                foreach(var player in GameServices.Round.Players)
                {
                    if(player==null||!player.gameObject.activeInHierarchy)continue;
                    var body=player.transform.position+Vector3.up*.65f;
                    float distance=(body-_root.position).sqrMagnitude;
                    if(distance<best){best=distance;nearest=body;found=true;}
                }
                return found;
            }
            public bool ReactToImpact(Vector3 at,float radius)
            {
                if (!Visible || Time.time < _impactQuietUntil || (_root.position-at).sqrMagnitude > radius*radius) return false;
                if (_data.Bird)
                {
                    if(_data.AerialWander)
                    {ChooseAerialLeg(true);_gliding=false;_wingWait=_owner.Range(1.2f,2.4f);}
                    else
                    {
                        if (_birdPhase != 1 && _birdPhase != 2) return false;
                        BeginFlight(_root.position,_data.Route[_data.Route.Length-1],3);
                    }
                }
                else
                {
                    if (_panic > 0) return false;
                    if (_peeing) EndDogPause();
                    if (HasHabitat)
                    {
                        Retreat(at);_impactQuietUntil=Time.time+4;return true;
                    }
                    int before=Mathf.Clamp(_target-_direction,0,_data.Route.Length-1);
                    if ((_data.Route[before]-at).sqrMagnitude > (_data.Route[_target]-at).sqrMagnitude)
                    { _target=before; _direction=-_direction; }
                    _panic=1.15f; _wait=0;
                }
                _impactQuietUntil=Time.time+4;
                return true;
            }
            public void Step(float dt)
            {
                if(_data.AerialWander)AerialBird(dt);else if(_data.Bird)Bird(dt);else if(HasHabitat)HabitatAnimal(dt);else GroundAnimal(dt);
                if(!Visible)return;
                float rate=_action=="walk"?Mathf.Max(.25f,_speed/Mathf.Max(.1f,_data.WalkCycleSpeed)):
                    _action=="run"?Mathf.Max(.5f,_speed/Mathf.Max(.1f,_data.RunCycleSpeed)):1;
                if(_data.AerialWander&&_gliding)
                    _clipTime=Mathf.MoveTowards(_clipTime,_glideHoldTime,dt*.75f);
                else _clipTime+=dt*rate;
                _current.SetTime(Mathf.Repeat(_clipTime,Mathf.Max(.01f,_clip.length)));
                if(_previous.IsValid()){_oldTime+=dt;_previous.SetTime(Mathf.Repeat(_oldTime,Mathf.Max(.01f,_oldClip.length)));}
                _blend=Mathf.MoveTowards(_blend,1,dt*8);
                _mixer.SetInputWeight(0,1-_blend);_mixer.SetInputWeight(1,_blend);_graph.Evaluate(0);
                if(HasHabitat)PoseActivity(dt);
            }
            private void GroundAnimal(float dt)
            {
                _peeCooldown=Mathf.Max(0,_peeCooldown-dt);
                if(_peeing)
                {
                    if(Threat(out _)){EndDogPause();_panic=0;}
                    else{DogPause(dt);return;}
                }
                if(Threat(out var threat)&&_panic<=0)
                {
                    int before=Mathf.Clamp(_target-_direction,0,_data.Route.Length-1);
                    if((_data.Route[before]-threat).sqrMagnitude>(_data.Route[_target]-threat).sqrMagnitude)
                    {_target=before;_direction=-_direction;}
                    _panic=_owner.Range(2,4);_wait=0;
                }
                _panic=Mathf.Max(0,_panic-dt);
                if(_wait>0){_wait-=dt;_speed=0;Play("idle");return;}
                var delta=_data.Route[_target]-_root.position;
                if(delta.sqrMagnitude<.008f)
                {
                    _root.position=_data.Route[_target];
                    if(_panic<=0&&_target==_data.PeeWaypoint&&_peeCooldown<=0)
                    {
                        if(_owner._random.NextDouble()<.4){_peeing=true;_peeTime=0;_speed=0;return;}
                        _peeCooldown=_owner.Range(40,80);
                    }
                    bool endpoint=_target==0||_target==_data.Route.Length-1;
                    if(endpoint)_direction=-_direction;
                    _target=Mathf.Clamp(_target+_direction,0,_data.Route.Length-1);
                    if(_panic<=0&&(endpoint||_owner._random.NextDouble()<.05))_wait=_owner.Range(3,10);
                    return;
                }
                var flat=new Vector3(delta.x,0,delta.z);
                var rotation=Quaternion.LookRotation(flat.normalized);
                float angle=Quaternion.Angle(_root.rotation,rotation);
                _root.rotation=Quaternion.RotateTowards(_root.rotation,rotation,dt*300);
                float desired=angle>65?0:_panic>0?_data.RunSpeed:_data.WalkSpeed;
                _speed=Mathf.MoveTowards(_speed,desired,dt*9);
                _root.position=Vector3.MoveTowards(_root.position,_data.Route[_target],_speed*dt);
                Play(_speed<.05f?"alert":_panic>0?"run":"walk");
            }
            private void DogPause(float dt)
            {
                var direction=_data.PeeTarget-_root.position;direction.y=0;
                var facing=Quaternion.LookRotation(Vector3.Cross(direction.normalized,Vector3.up)*_raisedLegSide);
                _root.rotation=Quaternion.RotateTowards(_root.rotation,facing,dt*220);
                if(Quaternion.Angle(_root.rotation,facing)>5){Play("alert");return;}
                Play("pee");_peeTime+=dt;
                if(_peeTime>=_clip.length){EndDogPause();return;}
                if(_peeLine==null)
                {
                    if(_owner._peeMaterial==null)_owner._peeMaterial=new Material(Shader.Find("Sprites/Default"));
                    _peeLine=new GameObject("Brief surface arc").AddComponent<LineRenderer>();_peeLine.transform.SetParent(_root,false);
                    _peeLine.sharedMaterial=_owner._peeMaterial;_peeLine.positionCount=5;_peeLine.useWorldSpace=true;
                    _peeLine.startWidth=.005f;_peeLine.endWidth=.003f;
                    _peeLine.startColor=_peeLine.endColor=new Color(.64f,.61f,.40f,.28f);
                }
                _peeLine.enabled=_peeTime>.55f&&_peeTime<_clip.length-.4f;
                var start=_root.position-_root.forward*.15f+_root.right*(_raisedLegSide*.1f)+Vector3.up*.2f;
                var end=_data.PeeTarget;
                for(int i=0;i<5;i++)
                {float t=i/4f;_peeLine.SetPosition(i,Vector3.Lerp(start,end,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.07f));}
            }
            private void EndDogPause()
            {
                _peeing=false;_peeTime=0;_peeCooldown=_owner.Range(120,240);
                if(_peeLine!=null)_peeLine.enabled=false;
                _wait=0;
            }
            private void BeginFlight(Vector3 from,Vector3 to,int phase)
            {
                _flightFrom=from;_flightTo=to;_flightTime=0;
                _flightDuration=Mathf.Max(.6f,Vector3.Distance(from,to)/_owner.Range(3.5f,5));
                _birdPhase=phase;_root.gameObject.SetActive(true);Play("fly");
            }
            private Vector3 InFlightBounds(Vector3 p)
            {
                var b=_data.FlightBounds;
                return new Vector3(Mathf.Clamp(p.x,b.min.x,b.max.x),Mathf.Clamp(p.y,b.min.y,b.max.y),Mathf.Clamp(p.z,b.min.z,b.max.z));
            }
            private void ChooseAerialLeg(bool startled=false)
            {
                var b=_data.FlightBounds;var from=InFlightBounds(_root.position);var forward=_root.forward;
                Vector3 target=from;float best=float.NegativeInfinity;
                // Prefer a continuing transit, while giving each leg a new destination
                // and allowing a broad turn when approaching the habitat boundary.
                for(int i=0;i<12;i++)
                {
                    var candidate=new Vector3(_owner.Range(b.min.x,b.max.x),_owner.Range(b.min.y,b.max.y),_owner.Range(b.min.z,b.max.z));
                    if(startled)candidate.y=Mathf.Max(candidate.y,Mathf.Min(b.max.y,from.y+3));
                    var delta=candidate-from;float distance=delta.magnitude;
                    if(distance<12)continue;
                    float score=Vector3.Dot(forward,delta/distance)*24+Mathf.Min(distance,70)*.15f;
                    if(score>best){best=score;target=candidate;}
                }
                if(float.IsNegativeInfinity(best))target=InFlightBounds(from+(b.center-from)*.8f+Vector3.up);
                float length=Vector3.Distance(from,target);var heading=(target-from).normalized;
                var endHeading=Vector3.Slerp(forward,heading,.75f).normalized;
                _flightFrom=from;_flightTo=target;
                _flightControl1=InFlightBounds(from+forward*length*.30f);
                _flightControl2=InFlightBounds(target-endHeading*length*.30f);
                _flightTime=0;_flightDuration=Mathf.Max(2,length/_owner.Range(startled?5:3.8f,startled?6.2f:5.2f));
                _flightLegs++;_birdPhase=4;_root.gameObject.SetActive(true);Play("fly");
            }
            private void AerialBird(float dt)
            {
                if(_birdPhase==0)
                {
                    _wait-=dt;if(_wait>0)return;
                    ChooseAerialLeg();_wingWait=_owner.Range(.8f,1.8f);
                }
                _flightTime+=dt;float t=Mathf.Clamp01(_flightTime/_flightDuration),u=1-t;
                var next=u*u*u*_flightFrom+3*u*u*t*_flightControl1+3*u*t*t*_flightControl2+t*t*t*_flightTo;
                var tangent=3*u*u*(_flightControl1-_flightFrom)+6*u*t*(_flightControl2-_flightControl1)+3*t*t*(_flightTo-_flightControl2);
                _speed=tangent.magnitude/Mathf.Max(.01f,_flightDuration);
                _root.position=next;
                if(tangent.sqrMagnitude>.00001f)
                {
                    float turn=Vector3.SignedAngle(_root.forward,tangent,Vector3.up);
                    _bank=Mathf.MoveTowards(_bank,Mathf.Clamp(-turn*2,-25,25),dt*45);
                    var facing=Quaternion.LookRotation(tangent.normalized,Vector3.up)*Quaternion.AngleAxis(_bank,Vector3.forward);
                    _root.rotation=Quaternion.Slerp(_root.rotation,facing,1-Mathf.Exp(-dt*4));
                }
                _wingWait-=dt;
                if(_wingWait<=0)
                {
                    _gliding=!_gliding;
                    _wingWait=_gliding?_owner.Range(_data.GlideDuration.x,_data.GlideDuration.y):_owner.Range(.8f,1.8f);
                    // Authored fly phase zero has spread wings. Settle into that pose,
                    // instead of freezing a randomly folded wing midway through a flap.
                    if(_gliding)_glideHoldTime=Mathf.Round(_clipTime/_clip.length)*_clip.length;
                }
                if(t>=1)ChooseAerialLeg();
            }
            private void Bird(float dt)
            {
                if(_birdPhase==0)
                {
                    _wait-=dt;if(_wait>0)return;
                    _root.position=_data.Route[1];
                    // Passing a chance check never means arriving on a player.
                    if(_owner._random.NextDouble()>.55||Threat(out _)){_wait=_owner.Range(7,25);return;}
                    BeginFlight(_data.Route[0],_data.Route[1],1);
                }
                if(_birdPhase==1&&Threat(out _))BeginFlight(_root.position,_data.Route[_data.Route.Length-1],3);
                if(_birdPhase==1||_birdPhase==3)
                {
                    _flightTime+=dt;float t=Mathf.Clamp01(_flightTime/_flightDuration);
                    Vector3 next=Vector3.Lerp(_flightFrom,_flightTo,t);
                    var direction=next-_root.position;
                    if(direction.sqrMagnitude>.00001f)_root.rotation=Quaternion.LookRotation(direction.normalized);
                    _root.position=next;
                    if(t<1)return;
                    if(_birdPhase==3){_root.gameObject.SetActive(false);_birdPhase=0;_wait=_owner.Range(10,35);return;}
                    _birdPhase=2;_wait=_owner.Range(4,13);_fidgetWait=_owner.Range(1,3);
                    _root.rotation=Quaternion.Euler(0,_root.eulerAngles.y,0);Play("idle");
                    return;
                }
                _wait-=dt;
                if(Threat(out _)||_wait<=0)
                {BeginFlight(_root.position,_data.Route[_data.Route.Length-1],3);return;}
                _fidgetWait-=dt;
                if(_fidgetWait<=0){Play(_action=="idle"?"peck":"idle");_fidgetWait=_owner.Range(_action=="peck"?.8f:1.5f,_action=="peck"?1.4f:4);}
            }
#if UNITY_EDITOR
            public string Description=>FormattableString.Invariant($"{_action}|wait={_wait:F2}|panic={_panic:F2}|target={_target}|speed={_speed:F2}|near={Threat(out _)}|yaw={_root.eulerAngles.y:F1}|peeing={_peeing}|peeCooldown={_peeCooldown:F1}|aerial={_data.AerialWander}|glide={_gliding}|legs={_flightLegs}|activity={_activity}|site={_site}|visits={_visits}");
            public void StageBird()
            {
                if(_data.AerialWander)
                {
                    _root.position=_data.Route[0];_root.rotation=Quaternion.LookRotation((_data.Route[1]-_data.Route[0]).normalized);
                    _gliding=false;_wingWait=1;ChooseAerialLeg();return;
                }
                _root.position=_data.Route[1];_root.rotation=Quaternion.identity;_root.gameObject.SetActive(true);
                _birdPhase=2;_wait=30;_fidgetWait=1;Play("idle");
            }
            public void StageDogPause()
            {
                if(_data.PeeWaypoint<0)throw new InvalidOperationException(_data.Id+" has no suitable surface");
                _root.position=_data.Route[_data.PeeWaypoint];_target=_data.PeeWaypoint;_panic=0;_wait=0;
                _peeCooldown=0;_peeTime=0;_peeing=true;Play("idle");
            }
#endif
            public void Dispose(){if(_graph.IsValid())_graph.Destroy();if(_root!=null)Destroy(_root.gameObject);}
        }
    }
}
