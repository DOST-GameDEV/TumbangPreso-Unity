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
        public bool SoundPlayed { get; private set; }

        public UltimatePhaseView(Transform owner, IReadOnlyList<UltimateCommit> commits)
        {
            try
            {
                var liveCamera = Camera.main;
                int watching = liveCamera?.GetComponent<CameraRig>()?.Following?.PlayerSlot ?? NetAuthority.LocalSlot;
                _stage = new GameObject("~UltimateRenderCopies"); _stage.SetActive(false);
                _stage.transform.SetParent(owner, false);
                foreach (var commit in commits)
                {
                    var actor = GameServices.Round.PlayerAt(commit.Seat);
                    var visual = actor.GetComponent<CharacterVisual>();
                    if (visual?.Model == null) continue;
                    var track = new MatchPoseHistory.Track(actor, visual.Model);
                    track.Record(Time.time); track.Record(Time.time + .05f);
                    var body = track.Clone(_stage.transform); if (body == null) continue;
                    track.Apply(body, track.Newest);
                    var entry = new ActorScene { Actor = actor, Body = body };
                    _actors.Add(entry);
                    entry.Clip = HeroAbilityClips.BuildUltimateIntroduction(body.Root.transform, actor.AbilitySystem.HeroId,
                        actor.GetComponent<Carrier>().Held != null);
                    if (entry.Clip == null) continue;
                    entry.Clip.SampleAnimation(body.Root, 0);
                    foreach (var surface in body.Renderers) surface.shadowCastingMode = ShadowCastingMode.On;
                    entry.Scene = new HeroIntroductionScene(_stage.transform, actor.AbilitySystem.HeroId, actor, body);
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
                header.anchoredPosition=new Vector2(0,-32); header.sizeDelta=new Vector2(1100,145);
                var plate = header.gameObject.AddComponent<Image>(); plate.color=new Color(.035f,.07f,.05f,.88f); plate.raycastTarget=false;
                var title=OwnerUiLayout.Text(header,"UltimateName",_primary?.Actor.AbilitySystem.Kit.Ultimate.Name ?? "ULTIMATE",58,OwnerUiLayout.TypeRole.Display);
                title.alignment=TextAnchor.MiddleCenter; title.color=OwnerUiTheme.Current.Pale;
                title.horizontalOverflow=HorizontalWrapMode.Overflow; title.verticalOverflow=VerticalWrapMode.Overflow;
                OwnerUiLayout.Place(title.rectTransform,20,0,1060,92);
                var castNames=OwnerUiLayout.Text(header,"CohortNames",string.Join("  +  ",commits.Select(c=>PlayerIdentity.Label(c.Seat)+" · "+SeatLabel.Raw(c.Seat))),28);
                castNames.alignment=TextAnchor.MiddleCenter; castNames.color=_primary != null && commits.Count == 1 ? PlayerIdentity.Colour(_primary.Actor.PlayerSlot) : OwnerUiTheme.Current.Pale;
                OwnerUiLayout.Place(castNames.rectTransform,20,94,1060,43);
                if (liveCamera != null && _primary != null)
                {
                    var go=new GameObject("UltimateSceneCamera"); go.transform.SetParent(_stage.transform,false);
                    _camera=go.AddComponent<Camera>(); _camera.CopyFrom(liveCamera); _camera.enabled=false; _camera.tag="Untagged";
                    _camera.cullingMask &= ~(1<<5); _camera.nearClipPlane=.05f;
                    _camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                    int width=Mathf.Clamp(Screen.width,960,1920), height=Mathf.Max(540,Mathf.RoundToInt(width*Screen.height/(float)Mathf.Max(1,Screen.width)));
                    _target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32) { name="SharedUltimateFrame" };
                    _target.Create(); _camera.targetTexture=_target; _picture.texture=_target;
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
            if (!_handedOff && age >= 2.4f)
            {
                _handedOff = true;
                foreach (var entry in _actors)
                    entry.Actor.GetComponent<CharacterAnimator>()?.StageIntroductionPose(entry.Body.Root, entry.Actor.AbilitySystem.Kit.Ultimate);
            }
            float returnBlend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.4f,2.8f,age));
            _fade.alpha=returnBlend;
            bool moving=Settings.SettingsStore.Current.CinematicCameraMotion && !Settings.SettingsStore.Current.ReducedUiMotion;
            _picture.enabled=moving && _camera!=null && _primary?.Scene!=null;
            if (!_picture.enabled) return;
            _primary.Scene.Shot(age,out var eye,out var target,out var fov,_camera.aspect);
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
            _primary.Body.ShowOnlyForCapture(true);_primary.Scene.SetVisibleForCapture(true);
            try{_camera.Render();}
            finally
            {
                _primary.Body.ShowOnlyForCapture(false);_primary.Scene.SetVisibleForCapture(false);
                for(int i=0;i<_hidden.Count;i++)if(_hidden[i]!=null)_hidden[i].forceRenderingOff=_wasHidden[i];
            }
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
            foreach(var entry in _actors){entry.Scene?.Dispose();if(entry.Clip!=null)Object.Destroy(entry.Clip);}
            _actors.Clear();if(_stage!=null)Object.Destroy(_stage);
            _stage=null;_canvas=null;_target=null;_camera=null;_primary=null;
        }
    }
}
