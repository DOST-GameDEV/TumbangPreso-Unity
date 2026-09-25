using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.CameraSystem
{
    // Scene cameras render copies into an overlay; the player's real camera and
    // its aim never move. Every viewer shares the phase, including reduced motion.
    public sealed class UltimatePhaseView : IDisposable
    {
        private sealed class ActorScene
        {
            public CharacterMotor Actor;
            public MatchPoseHistory.Copy Body;
            public AnimationClip Clip;
            public HeroIntroductionScene Scene;
            public GroundContactVisual Contact;
        }
        private readonly List<ActorScene> _actors = new List<ActorScene>(4);
        private readonly List<Renderer> _hidden = new List<Renderer>(128), _scratch = new List<Renderer>(128);
        private readonly List<bool> _wasHidden = new List<bool>(128);
        private readonly HashSet<Renderer> _seen = new HashSet<Renderer>();
        private GameObject _stage;
        private Camera _camera;
        private RenderTexture _target;
        private Canvas _canvas;
        private CanvasGroup _fade;
        private RawImage _picture;
        private ActorScene _primary;
        private Slipper[] _slippers;
        private bool _handedOff;
        private readonly RaycastHit[] _shotHits = new RaycastHit[32];
        // Per authored shot: mirror it, replace it with another clear shot, or give up (card).
        private bool[] _mirror;
        private int[] _useShot;
        private bool _safeShot = true, _stillMirror, _stillSafe;
        private float _duration = UltimatePerformance.DefaultSeconds;
        public bool SoundPlayed { get; private set; }
        /// <summary>Seats whose own voice line already played inside this introduction.</summary>
        public IEnumerable<int> VoicedSeats { get { foreach (var entry in _actors) if (entry.Scene != null && entry.Scene.VoicePlayed) yield return entry.Actor.PlayerSlot; } }

        public UltimatePhaseView(Transform owner, IReadOnlyList<UltimateCommit> commits, double duration = UltimatePerformance.DefaultSeconds)
        {
            _duration = (float)duration;
            try
            {
                var liveCamera = Camera.main;
                int watching = liveCamera?.GetComponent<CameraRig>()?.Following?.PlayerSlot ?? NetAuthority.LocalSlot;
                _stage = new GameObject("~UltimateRenderCopies");
                _stage.transform.SetParent(owner, false);
                foreach (var commit in commits)
                {
                    var actor = GameServices.Round.PlayerAt(commit.Seat);
                    var visual = actor.GetComponent<CharacterVisual>();
                    if (visual?.Model == null) continue;
                    var track = new MatchPoseHistory.Track(actor, visual.Model);
                    track.Record(Time.time); track.Record(Time.time + .05f);
                    var actorStage = new GameObject("RecordedCast-P" + (commit.Seat + 1));
                    actorStage.transform.SetParent(_stage.transform,false);
                    // MatchPoseHistory.Clone assigns meshes and their mapped bones
                    // separately. Keep that hierarchy inactive until both exist.
                    actorStage.SetActive(false);
                    var body = track.Clone(actorStage.transform); if (body == null) continue;
                    track.Apply(body, track.Newest);
                    body.Root.SetActive(true); // The committed caster is shown even if a prior power hid their live model.
                    var entry = new ActorScene { Actor = actor, Body = body,
                        Contact=new GroundContactVisual(_stage.transform,"Ultimate body contact",true) };
                    _actors.Add(entry);
                    entry.Clip = UltimateIntroductionCache.Find(actor,actor.GetComponent<Carrier>().Held != null);
                    if (entry.Clip == null) continue;
                    actorStage.SetActive(true);
                    entry.Clip.SampleAnimation(body.Root, 0);
                    if (actor.IsGrounded)
                    {
                        var surfaces=body.Renderers.Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                        if(surfaces.Length>0)
                            actorStage.transform.position+=Vector3.up*(Slipper.GroundY(actor.transform.position)-surfaces.Min(r=>r.bounds.min.y));
                    }
                    foreach (var surface in body.Renderers) surface.shadowCastingMode = ShadowCastingMode.On;
                    entry.Scene = new HeroIntroductionScene(actorStage.transform, actor.AbilitySystem.HeroId, actor, body) { Boundary = _duration };
                    if (_primary == null || commit.Seat == watching) _primary = entry;
                }
                _stage.SetActive(true);
                _slippers = Object.FindObjectsByType<Slipper>();
                _canvas = OwnerUiLayout.Canvas(owner, "SharedUltimateCanvas", 190);
                var focus = _canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
                _fade = _canvas.gameObject.AddComponent<CanvasGroup>(); _fade.blocksRaycasts = false; _fade.interactable = false;
                var root = (RectTransform)_canvas.transform;
                _picture = OwnerUiLayout.Rect(root, "UltimateScene").gameObject.AddComponent<RawImage>();
                OwnerUiLayout.Fill(_picture.rectTransform); _picture.raycastTarget = false;
                var header = OwnerUiLayout.Rect(root, "UltimateIdentity");
                header.anchorMin=header.anchorMax=header.pivot=new Vector2(.5f,1);
                bool together=commits.Count>1;
                header.anchoredPosition=new Vector2(0,-32);header.sizeDelta=new Vector2(900,together?(commits.Count>2?248:164):128);
                var plate=header.gameObject.AddComponent<CourtPopupGraphic>();plate.Brush=true;plate.color=CourtPresentationPalette.DeepRed;plate.raycastTarget=false;
                string NameForCast(int seat)
                {
                    var actor=GameServices.Round?.PlayerAt(seat);
                    return actor!=null?actor.AbilitySystem?.Kit?.Ultimate?.Name??"ULTIMATE":"ULTIMATE";
                }
                var title=OwnerUiLayout.Text(header,"UltimateName",together?"ULTIMATES INCOMING":NameForCast(commits[0].Seat),50,OwnerUiLayout.TypeRole.Display);
                title.alignment=TextAnchor.MiddleCenter;title.color=CourtPresentationPalette.Paper;title.supportRichText=false;
                title.horizontalOverflow=HorizontalWrapMode.Overflow;title.verticalOverflow=VerticalWrapMode.Overflow;
                OwnerUiLayout.Place(title.rectTransform,35,0,830,74);
                for(int i=0;i<commits.Count;i++)
                {
                    int seat=commits[i].Seat;
                    float x=together?35+(i%2)*430:35;
                    if(commits.Count==3&&i==2)x=250;
                    float y=74+(i/2)*84,width=together?400:830;
                    var name=OwnerUiLayout.Text(header,"CohortSeat"+seat,SeatLabel.WithIdentity(seat),25,OwnerUiLayout.TypeRole.Display);
                    name.alignment=TextAnchor.MiddleCenter;name.supportRichText=false;name.color=PlayerIdentity.Colour(seat);
                    name.verticalOverflow=VerticalWrapMode.Overflow;
                    OwnerUiLayout.Place(name.rectTransform,x,y,width,42);
                    if(together)
                    {
                        var abilityName=OwnerUiLayout.Text(header,"CohortAbility"+seat,NameForCast(seat),23);
                        abilityName.alignment=TextAnchor.MiddleCenter;abilityName.supportRichText=false;abilityName.color=CourtPresentationPalette.Paper;
                        abilityName.verticalOverflow=VerticalWrapMode.Overflow;
                        OwnerUiLayout.Place(abilityName.rectTransform,x,y+39,width,40);
                    }
                }
                if (liveCamera != null && _primary != null)
                {
                    var go=new GameObject("UltimateSceneCamera"); go.transform.SetParent(_stage.transform,false);
                    _camera=go.AddComponent<Camera>(); _camera.CopyFrom(liveCamera); _camera.enabled=false; _camera.tag="Untagged";
                    _camera.cullingMask &= ~(1<<5); _camera.nearClipPlane=.05f;
                    _camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    _camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled=liveCamera.GetComponent<WorldOutline>()?.PrototypeEnabled??true;
                    int width=Mathf.Clamp(Screen.width,960,1920), height=Mathf.Max(540,Mathf.RoundToInt(width*Screen.height/(float)Mathf.Max(1,Screen.width)));
                    _target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32) { name="SharedUltimateFrame" };
                    _target.Create(); _camera.targetTexture=_target; _picture.texture=_target;
                    ChooseShot();
                    SoundPlayed=_primary.Scene.StartSound();
                }
            }
            catch { Dispose(); throw; }
        }
        public void Draw(float age)
        {
            foreach (var entry in _actors)
            {
                if (entry.Clip == null || entry.Body.Root == null) continue;
                entry.Clip.SampleAnimation(entry.Body.Root,age); entry.Scene?.Sample(age);
            }
            // Each body starts blending back into live play 0.4 s before the SHARED boundary, which
            // is the longest caster's length: a shorter hero holds its last authored pose until then.
            if (!_handedOff && age >= Mathf.Max(0, _duration - UltimatePerformance.HandoffLead))
            {
                _handedOff = true;
                foreach (var entry in _actors)
                    entry.Actor.GetComponent<CharacterAnimator>()?.StageIntroductionPose(entry.Body.Root, entry.Actor.AbilitySystem.Kit.Ultimate);
            }
            // Keep one readable 3D view until the last beat. A full .4s dissolve
            // stacked two different court perspectives and washed out the handoff.
            // Pose staging starts HandoffLead before the boundary; shared time and live warning do not change.
            float returnBlend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(_duration-UltimatePerformance.ReturnSeconds,_duration,age));
            // ⚠️ REDUCED MOTION OR EFFECTS STILL GET THE PERFORMANCE (REFINE-2.11). Until now they
            // got no picture at all. They get the same acting from ONE locked shot with no cut and
            // no camera move, faded in rather than cut in; the scene itself drops its flashes.
            bool moving=Settings.SettingsStore.Current.CinematicCameraMotion && !Settings.SettingsStore.Current.ReducedUiMotion && !Settings.SettingsStore.Current.ReducedEffects;
            _fade.alpha=returnBlend*(moving?1:Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.3f)));
            _picture.enabled=(moving?_safeShot:_stillSafe) && _camera!=null && _primary?.Scene!=null;
            if (!_picture.enabled) return;
            Vector3 eye, target; float fov;
            if(moving)
            {
                int shot=_primary.Scene.ShotIndexAt(age);
                int use=_useShot!=null&&shot>=0?_useShot[shot]:shot;
                _primary.Scene.ShotAt(use,age,out eye,out target,out fov,_camera.aspect);
                if(_mirror!=null&&use>=0&&_mirror[use])eye=target+Vector3.Reflect(eye-target,_primary.Actor.transform.right);
            }
            else
            {
                _primary.Scene.StillShot(out eye,out target,out fov);
                if(_stillMirror)eye=target+Vector3.Reflect(eye-target,_primary.Actor.transform.right);
            }
            _camera.transform.position=eye; _camera.transform.LookAt(target); _camera.fieldOfView=fov;
            _hidden.Clear();_wasHidden.Clear();_seen.Clear();
            foreach(var actor in GameServices.Round.Players)
            {
                if (actor == null) continue;
                Hide(actor.transform);
                var pet=actor.GetComponent<CharacterVisual>()?.Companion; if(pet!=null)Hide(pet.transform);
            }
            foreach(var shoe in _slippers)if(shoe!=null)Hide(shoe.transform);
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>())Hide(arms.transform);
            if(WorldContactPresentation.Current!=null)Hide(WorldContactPresentation.Current.transform);
            _primary.Body.ShowOnlyForCapture(true);_primary.Scene.SetVisibleForCapture(true);
            float foot=WorldContactPresentation.ModelBottom(_primary.Body.Renderers,_primary.Body.Root.transform.position.y);
            _primary.Contact.Place(_primary.Body.Root.transform.position,foot,new Vector2(.45f,.45f),WorldCueProfile.LightingWeight*.20f);
            _primary.Contact.Visible(true);
            try{_camera.Render();}
            finally
            {
                _primary.Body.ShowOnlyForCapture(false);_primary.Scene.SetVisibleForCapture(false);
                _primary.Contact.Visible(false);
                for(int i=0;i<_hidden.Count;i++)if(_hidden[i]!=null)_hidden[i].forceRenderingOff=_wasHidden[i];
            }
        }
        private void ChooseShot()
        {
            // Judge every authored shot on both sides at its own most revealed moment (its end,
            // where the moves settle). A blocked shot is mirrored; one blocked on both sides
            // borrows the nearest clear shot; a tight alley with none clear gets the same-duration
            // card, never a camera inside a wall. The reduced-motion still is judged the same way.
            var scene=_primary.Scene;int count=scene.ShotCount;
            _mirror=new bool[Mathf.Max(0,count)];_useShot=new int[Mathf.Max(0,count)];
            var clear=new bool[Mathf.Max(0,count)];bool any=false;
            for(int i=0;i<count;i++)
            {
                float at=Mathf.Max(scene.ShotStart(i),scene.ShotEnd(i)-.05f);
                _primary.Clip.SampleAnimation(_primary.Body.Root,at);scene.Sample(at,false);
                scene.ShotAt(i,at,out var eye,out var target,out _,_camera.aspect);
                Vector3 alternate=target+Vector3.Reflect(eye-target,_primary.Actor.transform.right);
                bool first=ClearShot(target,eye),second=ClearShot(target,alternate);
                _mirror[i]=!first&&second;clear[i]=first||second;any|=clear[i];_useShot[i]=i;
            }
            for(int i=0;i<count;i++)
            {
                if(clear[i])continue;
                for(int d=1;d<count;d++)
                {
                    if(i+d<count&&clear[i+d]){_useShot[i]=i+d;break;}
                    if(i-d>=0&&clear[i-d]){_useShot[i]=i-d;break;}
                }
            }
            _safeShot=any;
            {
                float at=_duration*.6f;
                _primary.Clip.SampleAnimation(_primary.Body.Root,at);scene.Sample(at,false);
                scene.StillShot(out var eye,out var target,out _);
                Vector3 alternate=target+Vector3.Reflect(eye-target,_primary.Actor.transform.right);
                bool first=ClearShot(target,eye),second=ClearShot(target,alternate);
                _stillMirror=!first&&second;_stillSafe=first||second;
            }
            _primary.Clip.SampleAnimation(_primary.Body.Root,0);scene.Sample(0,false);
        }
        private bool ClearShot(Vector3 focus,Vector3 eye)
        {
            Vector3 line=eye-focus;float length=line.magnitude;if(length<.1f)return false;
            int count=Physics.RaycastNonAlloc(focus,line/length,_shotHits,length,~0,QueryTriggerInteraction.Ignore);
            if(count==_shotHits.Length)return false;
            for(int i=0;i<count;i++)
            {
                var collider=_shotHits[i].collider;if(collider==null)continue;
                if(collider.GetComponentInParent<CharacterMotor>()!=null||collider.GetComponentInParent<Slipper>()!=null
                    ||collider.GetComponentInParent<Lata>()!=null)continue;
                return false;
            }
            return true;
        }
        private void Hide(Transform root)
        {
            _scratch.Clear();root.GetComponentsInChildren<Renderer>(true,_scratch);
            foreach(var surface in _scratch)
            {
                if(!_seen.Add(surface))continue;
                _hidden.Add(surface);_wasHidden.Add(surface.forceRenderingOff);surface.forceRenderingOff=true;
            }
        }
        public void Dispose()
        {
            if(_camera!=null)_camera.targetTexture=null;
            if(_canvas!=null)Object.Destroy(_canvas.gameObject);
            if(_target!=null){_target.Release();Object.Destroy(_target);}
            foreach(var entry in _actors){entry.Scene?.Dispose();entry.Contact?.Dispose();}
            _actors.Clear();if(_stage!=null)Object.Destroy(_stage);
            _stage=null;_canvas=null;_target=null;_camera=null;_primary=null;
        }
    }
}
