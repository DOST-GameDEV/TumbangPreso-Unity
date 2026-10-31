using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.CameraSystem
{
    // Playback owns rendering and its own audio voices, never simulation objects.
    public sealed class RecordedWorldView : IDisposable
    {
        private sealed class Item { public RecordedObjectTrack Track; public MatchPoseHistory.Copy Copy; public Transform[] Bones; public GroundContactVisual Contact; public Slipper HighlightSource; public MaterialPropertyBlock[] HighlightDefaults; }
        private readonly List<Item> _items=new List<Item>(13);
        private readonly List<Renderer> _hidden=new List<Renderer>();
        private readonly List<bool> _previous=new List<bool>();
        private readonly List<Canvas> _hiddenCanvases=new List<Canvas>();
        private readonly List<bool> _canvasWasEnabled=new List<bool>();
        private readonly List<Light> _hiddenLights=new List<Light>();
        private readonly List<bool> _lightWasEnabled=new List<bool>();
        private RecordedMatchClip _clip;
        private readonly bool _standalone;
        public LocalReplaySceneSegment RecordedScene { get; set; }
        public LocalReplayFxSegment RecordedEffects { get; set; }
        public LocalReplayScenerySegment RecordedScenery { get; set; }
        private RecordedSceneryView _scenery;
        private RecordedArenaEffects _arenaEffects;
        private int _followSeat=-1;
        // ⚠️ THE SHOT FOLLOWS THE PLAY (owner, October 22: "ang bobo ng replay cameras"). It used to be placed
        // once from where both players stood at the clip's FIRST frame, up to 17 m off and high, and never moved,
        // so the play ran out of a dead wide. Now it tracks the pair with a slow orbit, glides, cuts closer and
        // lower around the contact, and swings to another clear bearing when scenery comes between. The
        // free-camera replay viewer (`standalone`) keeps its own camera.
        private Item _shotActor,_shotSubject;
        private Vector3 _shotSide;
        private float _shotLens;
        private bool _tracking,_shotPlaced,_shotClose;
        private static bool IsBody(RecordedObjectTrack track)=>track.Kind==RecordedObjectKind.Player||track.Kind==RecordedObjectKind.Companion;
        private Vector3 _followOffset;
        private ReplayOverlay _overlay;
        private Image _contactFlash;
        private bool _contactLanded;
        private float _contactLandedAt;
        private GameObject _stage;
        private IDisposable _audioMix;
        private Canvas _canvas;
        private Camera _camera;
        private ColourGrade _grade;
        private Material _sky;
        private Light _skyFill;
        private RenderTexture _target;
        private Material _frameMaterial;
        private RecordedFieldPlayback _fields;
        // ⚠️ A CAUGHT BODY DOES NOT VANISH (owner, October 31: make it look way better). The record shows the victim sent home on the
        // tag's own frame, and the closest shot of the whole replay then framed a court's width of nothing. Through a catch the
        // body stays at the touch and takes the shove, as in the tag replay, and the bodies keep their spacing.
        private bool _catch;
        private float _holdAt;
        private Vector3 _holdActor,_holdSubject,_knockLine;
        private Quaternion _holdFacing;
        private float _knockRoom;
        private TagBodyBoxes _actorBoxes,_subjectBoxes;
        private CaughtRecoil _recoil;
        private float _actorBack;
        private readonly List<(Bounds box,Renderer renderer)> _shotProps=new List<(Bounds,Renderer)>();
        private ReplayImpact _impactLook;
        private readonly MaterialPropertyBlock _coatBlock=new MaterialPropertyBlock();
        private int _sound;
        private float _lastTime;
        private CourtBoundaryPresentation _court;
        private LataClockPresentation _lataClock;
        private GroundContactVisual _canLanding;
        private readonly Dictionary<int,CourtEscapePuff> _escapePuffs=new Dictionary<int,CourtEscapePuff>();
        private readonly Dictionary<int,CourtContactDust> _contactDust=new Dictionary<int,CourtContactDust>();
        private Map.ArenaStage _arena;
        public bool Ready {get;private set;}
        public string UnavailableReason {get;private set;}
        public RenderTexture Target=>_target;
        public Transform CameraTransform=>_camera!=null?_camera.transform:null;
        public void ResizeForScreen()
        {
            if(!_standalone||!Ready||Screen.width<=0||Screen.height<=0)return;
            int width=Mathf.Clamp(Screen.width,960,3840),height=Mathf.Max(540,Mathf.RoundToInt(width*Screen.height/(float)Screen.width));
            int samples=Mathf.Max(1,QualitySettings.antiAliasing);
            if(_target.width==width&&_target.height==height&&_target.antiAliasing==samples)return;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="LocalReplayFrame",antiAliasing=samples};
            target.Create();var previous=_target;_target=target;_camera.targetTexture=target;
            var picture=_canvas.GetComponentInChildren<RawImage>(true);if(picture!=null)picture.texture=target;
            previous.Release();Object.Destroy(previous);
        }
        public void FollowCamera(int seat,Vector3 offset){_followSeat=seat;_followOffset=offset;}
        public bool TryPlayerPosition(int seat,out Vector3 position)
        {
            var item=_items.FirstOrDefault(i=>IsBody(i.Track)&&i.Track.Seat==seat);
            position=item!=null?item.Bones[0].position:Vector3.zero;return item!=null;
        }
        public void ShowLabels(bool visible)
        {
            if(_canvas==null)return;
            foreach(Transform child in _canvas.transform)if(child.name!="RecordedWorldFrame")child.gameObject.SetActive(visible);
        }
        public bool UseClip(RecordedMatchClip clip,bool continuous=false)
        {
            if(!Ready||clip.Map!=_clip.Map||clip.MatchId!=_clip.MatchId||clip.Objects.Length!=_items.Count)return false;
            for(int i=0;i<_items.Count;i++)
            {
                var a=_items[i].Track;var b=clip.Objects[i];
                if(a.Kind!=b.Kind||a.Seat!=b.Seat||a.VisualKey!=b.VisualKey||!a.Pose.Paths.SequenceEqual(b.Pose.Paths))return false;
            }
            if(_arena!=null&&clip.Round!=_clip.Round)_arena.BeginReplay(clip.MatchId,clip.Round);
            for(int i=0;i<_items.Count;i++)_items[i].Track=clip.Objects[i];
            _fields?.Clear();
            foreach(var dust in _contactDust.Values)if(dust!=null)Object.Destroy(dust.gameObject);_contactDust.Clear();
            foreach(var puff in _escapePuffs.Values)if(puff!=null)Object.Destroy(puff.gameObject);_escapePuffs.Clear();
            _clip=clip;_sound=0;_lastTime=clip.Start;if(!continuous)GameServices.Audio?.StopReplayCues();return true;
        }
        /// <summary>Whether this view's own full-screen picture is shown. Off for a replay that is watched somewhere
        /// else (the Arena's big screens, `Map.ArenaScreens`): `Target` is still drawn by `Draw`.</summary>
        public void ShowOnScreen(bool visible){if(_canvas!=null)_canvas.enabled=visible;}
        public RecordedWorldView(Transform owner,RecordedMatchClip clip,bool standalone=false)
        {
            _clip=clip;_lastTime=clip.Start;_standalone=standalone;
            try
            {
                if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null){UnavailableReason="No rendering device";return;}
                if(Camera.main==null||clip.Map!=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name){UnavailableReason="Camera or map not ready: camera="+(Camera.main!=null)+" scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" clip="+clip.Map;return;}
                // Arena's stage rearranges every round. Stand it in this clip's own layout,
                // from the match and round the clip already carries, before any cast below.
                _arena=Map.ArenaStage.Instance;if(_arena!=null)_arena.BeginReplay(clip.MatchId,clip.Round);
                // Root pose samples and fields use world coordinates. Keep this owned
                // stage at world identity even when the overlay owner is transformed.
                _stage=new GameObject("~RecordedWorld");_stage.SetActive(false);
                _court=CourtBoundaryPresentation.CreateRecorded(_stage.transform,standalone?null:GameServices.Round?.Lata);
                _lataClock=LataClockPresentation.Install(_stage.transform,null,true);_lataClock.ShowForCapture(false);
                _canLanding=new GroundContactVisual(_stage.transform,"Recorded can footprint",true);
                foreach(var track in clip.Objects)
                {
                    GameObject source=standalone?CataloguedObject(track,clip.Mode):Source(track);
                    var highlightSource=track.Kind==RecordedObjectKind.Slipper?source?.GetComponentInParent<Slipper>():null;
                    if((source==null||MatchReplayArchive.VisualKey(source)!=track.VisualKey)&&
                        (track.Kind==RecordedObjectKind.Can||track.Kind==RecordedObjectKind.Slipper))source=CataloguedProp(track);
                    if(source==null){UnavailableReason="Missing recorded art: "+track.Kind+" P"+(track.Seat+1)+" skin="+track.Skin+" person="+track.Person;return;}
                    string visualKey=MatchReplayArchive.VisualKey(source);
                    if(visualKey!=track.VisualKey){UnavailableReason="Changed recorded art: "+track.Kind+" P"+(track.Seat+1)+" expected="+track.VisualKey+" actual="+visualKey;return;}
                    var history=new MatchPoseHistory.Track(standalone?null:GameServices.Round.BodyAt(track.Seat),source);
                    history.Record(Time.time);history.Record(Time.time+.05f);
                    var copy=history.Clone(_stage.transform);if(copy==null){UnavailableReason="Render copy failed: "+track.Kind;return;}
                    if(standalone&&IsBody(track))
                    {
                        // CharacterVisual adds this non-rendering carry anchor
                        // to the live rig. Its exact transform is in the poses,
                        // but the catalog prefab intentionally has no runtime
                        // anchor. Recreate only that known node, never a missing
                        // model bone or an unknown changed-art hierarchy.
                        foreach(string path in track.Pose.Paths)
                        {
                            if(!path.EndsWith("/HandAnchor",StringComparison.Ordinal)||copy.Root.transform.Find(path)!=null)continue;
                            string parentPath=path.Substring(0,path.Length-"/HandAnchor".Length);
                            var parent=copy.Root.transform.Find(parentPath);
                            if(parent!=null)new GameObject("HandAnchor").transform.SetParent(parent,false);
                        }
                    }
                    var bones=track.Pose.Bind(copy.Root);if(bones==null)
                    {
                        string missing=track.Pose.Paths.FirstOrDefault(path=>path.Length>0&&copy.Root.transform.Find(path)==null);
                        UnavailableReason="Recorded pose binding changed: "+track.Kind+" P"+(track.Seat+1)+" missing="+missing;return;
                    }
                    if(!source.scene.IsValid())
                    {
                        var art=track.Kind==RecordedObjectKind.Companion?PhaisterDollArt.LoadArt():
                            track.Kind==RecordedObjectKind.Player?RosterBook.Load()?.PersonArt(track.Skin,clip.Mode):null;
                        ToonSkin.Apply(copy.Root,IsBody(track)?ToonSkin.PersonOutlineWidth:ToonSkin.PropOutlineWidth,art?.Palette);
                        if(track.Kind==RecordedObjectKind.Companion)PhaisterDollArt.ApplyGlow(copy.Root);
                    }
                    MaterialPropertyBlock[] highlightDefaults=null;
                    if(track.Kind==RecordedObjectKind.Slipper)
                    {
                        highlightDefaults=new MaterialPropertyBlock[copy.Renderers.Length];
                        for(int i=0;i<copy.Renderers.Length;i++)
                        {highlightDefaults[i]=new MaterialPropertyBlock();copy.Renderers[i].GetPropertyBlock(highlightDefaults[i]);}
                    }
                    _items.Add(new Item{Track=track,Copy=copy,Bones=bones,HighlightSource=highlightSource,HighlightDefaults=highlightDefaults,
                        Contact=track.Kind==RecordedObjectKind.Familiar?null:new GroundContactVisual(_stage.transform,"Recorded object contact",true)});
                    track.Pose.Apply(bones,clip.Contact);
                }
                _stage.SetActive(true);
                var cameraGo=new GameObject("RecordedWorldCamera");cameraGo.transform.SetParent(_stage.transform,false);
                _camera=cameraGo.AddComponent<Camera>();_camera.CopyFrom(Camera.main);_camera.enabled=false;_camera.tag="Untagged";
                _camera.cullingMask&=~(1<<5);_camera.nearClipPlane=.08f;_camera.fieldOfView=58;
                _grade=cameraGo.AddComponent<ColourGrade>();_grade.AdoptFromScene();
                cameraGo.AddComponent<WorldOutline>().PrototypeEnabled=Camera.main.GetComponent<WorldOutline>()?.PrototypeEnabled??true;
                if(RenderSettings.skybox!=null)_sky=new Material(RenderSettings.skybox){name="RecordedSky"};
                var fill=new GameObject("RecordedWeatherFill");fill.transform.SetParent(_stage.transform,false);_skyFill=fill.AddComponent<Light>();
                _skyFill.type=LightType.Point;_skyFill.shadows=LightShadows.None;_skyFill.enabled=false;
                var focus=_items.FirstOrDefault(i=>IsBody(i.Track)&&i.Track.Seat==clip.Actor);
                var subject=_items.FirstOrDefault(i=>clip.Subject>=0?IsBody(i.Track)&&i.Track.Seat==clip.Subject:i.Track.Kind==RecordedObjectKind.Can);
                if(focus==null||subject==null)return;
                Vector3 a=focus.Bones[0].position,b=subject.Bones[0].position;
                Vector3 centre=(a+b)*.5f+Vector3.up*.6f;
                float distance=Mathf.Clamp(Vector3.Distance(a,b)*1.05f+4,5,17);
                Vector3 side=Vector3.Cross((b-a).normalized,Vector3.up);if(side.sqrMagnitude<.1f)side=Vector3.right;
                int width=Mathf.Clamp(Screen.width,960,standalone?3840:1920),height=Mathf.Max(540,Mathf.RoundToInt(width*Screen.height/(float)Mathf.Max(1,Screen.width)));
                float aspect=Mathf.Max(_camera.aspect,width/(float)height);
                float tangent=Mathf.Tan(_camera.fieldOfView*.5f*Mathf.Deg2Rad);
                float lensRadius=_camera.nearClipPlane*Mathf.Sqrt(1+tangent*tangent*(1+aspect*aspect));
                // A stable broad camera preserves the whole actual throw/chase.
                Vector3 eye=default;bool clear=false;
                for(int shot=0;shot<8;shot++)
                {
                    eye=centre+(Quaternion.AngleAxis(shot*45,Vector3.up)*side)*distance+Vector3.up*(distance*.6f);
                    if(Clear(centre,eye,lensRadius)){clear=true;break;}
                }
                if(!clear){UnavailableReason="No clear replay angle";return;}
                _camera.transform.position=eye;_camera.transform.LookAt(centre);
                _shotActor=focus;_shotSubject=subject;_shotLens=lensRadius;_tracking=!standalone;
                _shotSide=eye-centre;_shotSide.y=0;_shotSide=_shotSide.sqrMagnitude>.01f?_shotSide.normalized:side;
                _fields=new RecordedFieldPlayback(_stage.transform,seat=>_items.FirstOrDefault(i=>IsBody(i.Track)&&i.Track.Seat==seat)?.Copy.Root,seat=>_items.FirstOrDefault(i=>i.Track.Kind==RecordedObjectKind.Slipper&&i.Track.Seat==seat)?.Copy.Root);
                if(!standalone){PrepareCatch();ChooseBearing();}
                _target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="RetainedMatchFrame",
                    antiAliasing=Mathf.Max(2,QualitySettings.antiAliasing)};_target.Create();_camera.targetTexture=_target;
                _canvas=OwnerUiLayout.Canvas(owner,"CanonicalReplayCanvas",240);
                var input=_canvas.GetComponent<InputLayer.ScreenFocus>();if(input!=null)input.enabled=false;
                var picture=OwnerUiLayout.Rect(_canvas.transform,"RecordedWorldFrame").gameObject.AddComponent<RawImage>();
                OwnerUiLayout.Fill(picture.rectTransform);picture.texture=_target;picture.raycastTarget=false;
                // The camera has already composited the recorded world into RGB.
                // Its residual texture alpha must not blend present-time gameplay in.
                var frameShader=Resources.Load<Shader>("UI/OpaqueCameraFrame");
                if(frameShader!=null)
                {
                    _frameMaterial=new Material(frameShader){name="Opaque recorded camera frame",hideFlags=HideFlags.DontSave};
                    picture.material=_frameMaterial;
                }
                // The camcorder frame replaces the old band, credit and can-state footer (owner, 2026-10-22).
                // Halftime's replay is a shared, host-timed package, so it carries no skip prompt.
                // Its plate lands on the contact with a flash, as the tag replay's does, so the slowed moment has a hit in it.
                if(!standalone)
                {
                    _contactFlash=OwnerUiLayout.Rect(_canvas.transform,"RecordedContactFlash").gameObject.AddComponent<Image>();
                    OwnerUiLayout.Fill(_contactFlash.rectTransform);_contactFlash.raycastTarget=false;_contactFlash.color=new Color(1,1,1,0);
                    _impactLook=ReplayImpact.Build(_canvas.transform);
                    _overlay=ReplayOverlay.Build(_canvas.transform,"HIGHLIGHT",false);_overlay.HoldAnnouncement();
                }
                _audioMix=GameServices.Audio?.EnterReplayMix();
                Ready=true;
            }
            catch{Dispose();throw;}
        }
        private GameObject CataloguedProp(RecordedObjectTrack track)
        {
            var book=RosterBook.Load();if(book==null)return null;
            var entries=track.Kind==RecordedObjectKind.Can?book.Cans:book.Slippers;
            foreach(var entry in entries)
                if(entry!=null&&entry.Model!=null&&MatchReplayArchive.VisualKey(entry.Model)==track.VisualKey)return entry.Model;
            if(track.Kind==RecordedObjectKind.Slipper&&track.Pose.Paths.Any(p=>p=="BoulderStone"||p.EndsWith("/BoulderStone",StringComparison.Ordinal)))
            {
                foreach(var entry in entries)
                {
                    if(entry?.Model==null)continue;
                    var source=Object.Instantiate(entry.Model,_stage.transform,false);source.SetActive(false);
                    DanteBoulderStone.PrepareRecordedShape(source);
                    // Accept only the complete recorded art identity. Unknown
                    // hierarchy changes still fail the existing compatibility gate.
                    if(MatchReplayArchive.VisualKey(source)==track.VisualKey)return source;
                    Object.Destroy(source);
                }
            }
            return null;
        }
        private GameObject CataloguedObject(RecordedObjectTrack track,Core.GameMode mode)
        {
            var book=RosterBook.Load();if(book==null)return null;
            if(track.Kind==RecordedObjectKind.Companion)return track.Person==PhaisterDollArt.Id?PhaisterDollArt.LoadArt()?.Model:null;
            if(track.Kind==RecordedObjectKind.Player)return book.PersonArt(track.Skin,mode)?.Model;
            if(track.Kind==RecordedObjectKind.Familiar)return book.People.FirstOrDefault(p=>p!=null&&p.Id==track.Person)?.PetModel;
            return CataloguedProp(track);
        }
        private static GameObject Source(RecordedObjectTrack track)
        {
            var round=GameServices.Round;if(round==null)return null;
            var actor=round.BodyAt(track.Seat);
            if(track.Kind==RecordedObjectKind.Companion)
                return actor?.GetComponent<Abilities.VoodooDollBody>()!=null?actor.GetComponent<CharacterVisual>()?.Model:null;
            if(track.Kind==RecordedObjectKind.Player)
                return actor!=null&&actor.CharacterIndex==track.Skin&&Core.Roster.PersonIdAt(actor.Mode,actor.CharacterIndex)==track.Person?actor.GetComponent<CharacterVisual>()?.Model:null;
            if(track.Kind==RecordedObjectKind.Familiar)return actor?.GetComponent<CharacterVisual>()?.Companion?.gameObject;
            if(track.Kind==RecordedObjectKind.Can)return round.Lata!=null?MatchReplayArchive.PropModel(round.Lata.gameObject):null;
            foreach(var shoe in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include))if(shoe.SeatOfOrigin==track.Seat)return MatchReplayArchive.PropModel(shoe.gameObject);
            return null;
        }
        // Finds where the caught body really stood: the last sample before its teleport near the contact, or the contact.
        private void PrepareCatch()
        {
            if(_clip.Reason!="CATCH"||_clip.Subject<0||_shotActor==null||_shotSubject==null||!IsBody(_shotSubject.Track))return;
            var samples=_shotSubject.Track.Pose.Samples;_holdAt=_clip.Contact;
            for(int i=1;i<samples.Length;i++)
                if(samples[i].Time>=_clip.Contact-.2f&&samples[i].Time<=_clip.Contact+.5f&&samples[i].Epoch!=samples[i-1].Epoch)
                {_holdAt=Mathf.Min(_clip.Contact,samples[i-1].Time);break;}
            _shotSubject.Track.Pose.Apply(_shotSubject.Bones,_holdAt);
            _holdSubject=_shotSubject.Bones[0].position;_holdFacing=_shotSubject.Bones[0].rotation;
            // ⚠️ THE FEET STOP WHEN THE HAND HAS LANDED, NOT WHEN THE TAG WAS ACCEPTED. A running taya's recorded reach was
            // posed from where they had run to by then: held any earlier, the same arm stops a body's width short.
            _shotActor.Track.Pose.Apply(_shotActor.Bones,Mathf.Min(_clip.Contact+HandLands,_clip.End));_holdActor=_shotActor.Bones[0].position;
            _knockLine=_holdSubject-_holdActor;_knockLine.y=0;
            _knockLine=_knockLine.sqrMagnitude>.01f?_knockLine.normalized:Vector3.ProjectOnPlane(_holdFacing*Vector3.forward,Vector3.up).normalized;
            _knockRoom=TagBodyBoxes.KnockRoom(_holdSubject,_knockLine,CaughtAfterimage.KnockDistance);
            _actorBoxes=new TagBodyBoxes(_shotActor.Copy.Renderers,true);_subjectBoxes=new TagBodyBoxes(_shotSubject.Copy.Renderers,false);
            _recoil=new CaughtRecoil(_shotSubject.Bones);
            _catch=true;
        }
        private void HoldCaughtBody(float time)
        {
            if(!_catch||time<_holdAt)return;
            _shotSubject.Track.Pose.Apply(_shotSubject.Bones,_holdAt);_shotActor.Track.Pose.Apply(_shotActor.Bones,time);
            float v=Mathf.Clamp01((time-_clip.Contact-.12f)/.4f),e=1-(1-v)*(1-v)*(1-v);
            var root=_shotSubject.Bones[0];
            root.SetPositionAndRotation(_holdSubject+_knockLine*(_knockRoom*e)+Vector3.up*CaughtRecoil.Hop(time-_clip.Contact-.12f),
                Quaternion.AngleAxis(CaughtRecoil.Tilt*(_knockRoom/CaughtAfterimage.KnockDistance)*e,Vector3.Cross(Vector3.up,_knockLine))*_holdFacing);
            _recoil.Pose(_knockLine,time-_clip.Contact-.12f);
            // The taya's feet stop at the touch or they walk after the shoved body, and what the record shows of them backs off
            // only as far as these bodies measure. The record already carries the live hand on the touch, so this stays small.
            var actor=_shotActor.Bones[0];
            if(time>=_clip.Contact+HandLands)actor.position=_holdActor;
            actor.position-=_knockLine*_actorBack;
            float need=TagBodyBoxes.Overlap(_actorBoxes.Read(_knockLine),_subjectBoxes.Read(_knockLine));
            if(need>0)
            {
                float back=TagBodyBoxes.Room(actor.position,-_knockLine,Mathf.Min(need,Mathf.Max(0,MostHighlightBack-_actorBack)));
                _actorBack+=back;actor.position-=_knockLine*back;
            }
        }
        private const float MostHighlightBack=.35f,HandLands=.12f;
        private bool Foreground(Vector3 eye,Vector3 focus)
        {
            // A collider test alone let leaves and poles fill the frame: they have none. Rays across the frame, out to where the
            // play stands, each asked about scenery and about small props by their bounds.
            var turn=Quaternion.LookRotation(focus-eye,Vector3.up);
            float reach=Mathf.Max(.5f,Vector3.Distance(eye,focus)-1.6f);
            for(int y=-1;y<=1;y++)for(int x=-2;x<=2;x++)
            {
                var ray=new Ray(eye,turn*new Vector3(x*.3f,y*.24f,1).normalized);
                foreach(var hit in Physics.RaycastAll(ray,reach,~0,QueryTriggerInteraction.Ignore))if(IsReplayScenery(hit.collider))return true;
                foreach(var prop in _shotProps)if(prop.box.IntersectRay(ray,out float along)&&along<reach)return true;
            }
            return false;
        }
        // ⚠️ ONE GOOD BEARING, CHOSEN BEHIND THE WIPE, as the tag replay does (owner, October 22 and 31: "ang bobo ng replay cameras").
        private void ChooseBearing()
        {
            if(_shotActor==null||_shotSubject==null)return;
            Vector3 a=_catch?_holdActor:_shotActor.Track.Pose.StateAt(_clip.Contact).Positions[0];
            Vector3 b=_catch?_holdSubject:_shotSubject.Track.Pose.StateAt(_clip.Contact).Positions[0];
            Vector3 focus=Vector3.Lerp(a,b,.5f)+Vector3.up*.9f,line=b-a;line.y=0;line=line.sqrMagnitude>.01f?line.normalized:Vector3.forward;
            _shotProps.Clear();
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if(renderer.transform.IsChildOf(_stage.transform)||!renderer.enabled)continue;
                if(renderer.GetComponentInParent<CharacterMotor>()!=null||renderer.GetComponentInParent<Slipper>()!=null||renderer.GetComponentInParent<Lata>()!=null)continue;
                var box=renderer.bounds;
                if(box.extents.magnitude<5&&(box.center-focus).sqrMagnitude<16*16)_shotProps.Add((box,renderer));
            }
            Vector3 face=Vector3.ProjectOnPlane(_shotSubject.Track.Pose.StateAt(_clip.Contact).Rotations[0]*Vector3.forward,Vector3.up).normalized;
            Vector3 start=_shotSubject.Track.Pose.StateAt(_clip.Start).Positions[0]+Vector3.up*.9f;
            float best=float.MinValue;Vector3 chosen=_shotSide;
            for(int bearing=0;bearing<360;bearing+=30)
            {
                Vector3 dir=Quaternion.AngleAxis(bearing,Vector3.up)*Vector3.forward;
                Vector3 close=focus+dir*3.6f+Vector3.up*.75f,wide=focus+dir*6f+Vector3.up*1.6f;
                float along=Mathf.Abs(Vector3.Dot(dir,line));
                float score=1.2f*Vector3.Dot(dir,face)-1.8f*along-3*Mathf.Pow(Mathf.Max(0,along-.6f),2);
                if(!Clear(focus,close,_shotLens))score-=8;else if(Foreground(close,focus))score-=4;
                if(!Clear(focus,wide,_shotLens))score-=3;else if(Foreground(wide,focus))score-=2;
                if(!Clear(start,wide,_shotLens))score-=.5f;
                if(score>best){best=score;chosen=dir;}
            }
            _shotSide=chosen;
        }
        private void TrackShot(float time)
        {
            if(_shotActor==null||_shotSubject==null||_camera==null)return;
            Vector3 a=_shotActor.Bones[0].position,b=_shotSubject.Bones[0].position;
            Vector3 flat=b-a;flat.y=0;float separation=flat.magnitude;
            bool close=time>=_clip.Contact-.35f&&time<=_clip.Contact+.9f;
            Vector3 focus=Vector3.Lerp(a,b,close?.62f:.5f)+Vector3.up*.9f;
            // Near enough to read faces. A long chase frames its runner rather than backing off to a dead wide.
            float distance=close?Mathf.Clamp(separation*.6f+2.9f,3.2f,5.4f):Mathf.Clamp(separation*.7f+3.6f,4.2f,8.5f);
            float lift=close?.2f:.27f;
            // A slow drift round the pair, so even a still moment has motion. The bearing was chosen for this moment: stay near it.
            float yaw=(time-_clip.Contact)*3;
            Vector3 eye=focus;bool clear=false;
            for(int attempt=0;attempt<8&&!clear;attempt++)
            {
                Vector3 dir=Quaternion.AngleAxis(yaw+attempt*45*(attempt%2==0?1:-1)*((attempt+1)/2),Vector3.up)*_shotSide;
                eye=focus+dir*distance+Vector3.up*(distance*lift);
                clear=Clear(focus,eye,_shotLens)&&(attempt>=4||!Foreground(eye,focus));
            }
            if(!clear&&_shotPlaced)eye=_camera.transform.position;
            // The contact is a cut, not a glide: the lens jumps once into the close shot.
            bool cut=close!=_shotClose;_shotClose=close;
            float follow=!_shotPlaced||cut?1:1-Mathf.Exp(-6*Mathf.Max(.001f,Time.unscaledDeltaTime));
            _camera.transform.position=Vector3.Lerp(_camera.transform.position,eye,follow);
            var look=Quaternion.LookRotation(focus-_camera.transform.position,Vector3.up);
            _camera.transform.rotation=_shotPlaced&&!cut?Quaternion.Slerp(_camera.transform.rotation,look,follow):look;
            _shotPlaced=true;
            // The lens punches in on the contact and eases back; a short shake sells the hit.
            float since=time-_clip.Contact;
            bool calm=Settings.SettingsStore.Current.ReducedUiMotion;
            _camera.fieldOfView=58*(since<0||calm?1:1-.1f*Mathf.Exp(-since*6));
            if(since>=0&&!calm)
            {
                float shake=1.2f*Mathf.Exp(-since*12);
                _camera.transform.rotation*=Quaternion.Euler(Mathf.Sin(time*71)*shake,Mathf.Sin(time*53+1.7f)*shake,0);
            }
        }
        private void StepContactBeat(float time)
        {
            if(_overlay==null)return;
            if(!_contactLanded&&time>=_clip.Contact)
            {
                _contactLanded=true;_contactLandedAt=Time.unscaledTime;_overlay.Slam();
                if(_impactLook!=null&&_shotSubject!=null)
                {
                    Vector3 hit=_catch?Vector3.Lerp(_shotActor.Bones[0].position,_shotSubject.Bones[0].position,.55f)+Vector3.up*.85f:_shotSubject.Bones[0].position+Vector3.up*.2f;
                    _impactLook.Fire(_camera.WorldToViewportPoint(hit));
                }
            }
            if(_impactLook!=null)_impactLook.Closing=(_clip.Contact-time<.35f&&time-_clip.Contact<.9f?1f:0f)*.7f;
            if(_contactFlash==null||!_contactLanded)return;
            var look=Settings.SettingsStore.Current;
            float flash=look.ReducedEffects?0:.16f*Mathf.Clamp01(1-(Time.unscaledTime-_contactLandedAt)/.18f)*look.EffectiveFlashIntensity;
            _contactFlash.color=new Color(1,1,1,flash);
        }
        private static bool Clear(Vector3 centre,Vector3 eye,float lensRadius)
        {
            // A clear central ray can still leave a near-plane corner inside scenery.
            // Enclose the lens plane conservatively before accepting this replay angle.
            foreach(var collider in Physics.OverlapSphere(eye,lensRadius,~0,QueryTriggerInteraction.Ignore))
                if(IsReplayScenery(collider))return false;
            foreach(var hit in Physics.RaycastAll(centre,(eye-centre).normalized,Vector3.Distance(centre,eye),~0,QueryTriggerInteraction.Ignore))
                if(IsReplayScenery(hit.collider))return false;
            return true;
        }
        private static bool IsReplayScenery(Collider collider)=>collider.GetComponentInParent<CharacterMotor>()==null&&collider.GetComponentInParent<Slipper>()==null&&collider.GetComponentInParent<Lata>()==null&&collider.GetComponentInParent<AmbientLife>()==null;
        public void Draw(float time,bool audible=true)
        {
            if(!Ready)return;
            time=Mathf.Clamp(time,_clip.Start,_clip.End);
            StepContactBeat(time);
            var can=_items.FirstOrDefault(i=>i.Track.Kind==RecordedObjectKind.Can);
            if(can!=null)
            {
                bool previous=false,known=false;float restoredAt=float.NegativeInfinity,fallenAt=float.NegativeInfinity;Vector3 origin=Vector3.zero;
                foreach(var sample in can.Track.Pose.Samples)
                {
                    if(sample.Time>time)break;
                    bool upright=(sample.State&1)!=0;
                    if(known && !previous && upright){restoredAt=sample.Time;origin=sample.Positions[0];}
                    if(known && previous && !upright)fallenAt=sample.Time;
                    previous=upright;known=true;
                }
                bool up=(can.Track.Pose.StateAt(time).State&1)!=0;
                _court.DrawRecorded(up,time-restoredAt,origin);
                can.Track.Pose.Apply(can.Bones,time);
                float bottom=can.Bones[0].position.y-(up?0:WorldContactPresentation.RecordedCanSupport(can.Copy.Renderers,can.Bones[0].rotation));
                bool elevated=WorldGround.TryBelow(can.Bones[0].position,.5f,3.5f,out float ground) && bottom-ground>.12f;
                float settle=!up?Mathf.Clamp01(1-(time-fallenAt)/Core.Balance.ToppleTime):0;
                _canLanding.Place(can.Bones[0].position,bottom,new Vector2(.29f,.29f),
                    (elevated?.62f:settle*.62f)*WorldCueProfile.Current.HeroObjects,true);
            }
            HoldCaughtBody(time);
            foreach(var item in _items)
            {
                bool held=_catch&&time>=_holdAt&&(item==_shotSubject||item==_shotActor);
                if(!held)item.Track.Pose.Apply(item.Bones,time);
                var state=item.Track.Pose.StateAt(held&&item==_shotSubject?_holdAt:time);
                if(item.Contact!=null)
                {
                    bool player=IsBody(item.Track),shoe=item.Track.Kind==RecordedObjectKind.Slipper;
                    if(shoe && (state.State&255)!=(int)SlipperState.Loose)item.Contact.Hide();
                    else
                    {
                        float bottom=WorldContactPresentation.ModelBottom(item.Copy.Renderers,item.Bones[0].position.y);
                        item.Contact.Place(item.Bones[0].position,bottom,player?new Vector2(.45f,.45f):shoe?new Vector2(.24f,.15f):new Vector2(.25f,.22f),
                            WorldCueProfile.LightingWeight*(shoe?.16f:.42f));
                    }
                }
                if(item.Track.Kind==RecordedObjectKind.Can)
                {
                    LataClockPresentation.Unpack(state.State,out float restore,out float protection);
                    _lataClock.Draw(item.Bones[0].position,item.Bones[0].rotation,restore,protection);
                }
                int propState=state.State&255;
                bool recordedNonLoose=item.Track.Kind==RecordedObjectKind.Slipper&&
                    (propState==(int)SlipperState.Held||propState==(int)SlipperState.InFlight);
                for(int surfaceIndex=0;surfaceIndex<item.Copy.Renderers.Length;surfaceIndex++)
                {
                    var surface=item.Copy.Renderers[surfaceIndex];
                    // Loose keeps the original copy's unknown landing history. Restore
                    // exact blocks so non-loose overrides cannot leak across state edges.
                    if(item.HighlightDefaults!=null)surface.SetPropertyBlock(item.HighlightDefaults[surfaceIndex]);
                    surface.GetPropertyBlock(_coatBlock);_coatBlock.SetFloat("_TayaCue",0);
                    _coatBlock.SetFloat("_DepthReadability",item.Track.Kind==RecordedObjectKind.Slipper?0:WorldCueProfile.Current.DistanceReadability);
                    if(IsBody(item.Track))
                        _coatBlock.SetVector("_WorldBody",new Vector4(item.Bones[0].position.y,1.6f,1,0));
                    if(item.Track.Kind==RecordedObjectKind.Can)
                    {Vector3 axis=item.Bones[0].up;_coatBlock.SetVector("_WorldMetalAxis",new Vector4(axis.x,axis.y,axis.z,1));}
                    if(recordedNonLoose)Slipper.ApplyRecordedNonLooseHighlight(_coatBlock,item.HighlightSource);
                    surface.SetPropertyBlock(_coatBlock);
                }
                if(!state.HasCoat)continue;
                bool ability=state.Element!=StunElement.None;var coat=StunCoat.For(state.Element);
                foreach(var surface in item.Copy.Renderers)
                {
                    surface.GetPropertyBlock(_coatBlock);
                    var material=surface.sharedMaterial;
                    _coatBlock.SetFloat("_RimStrength",state.HasAccent?state.RimStrength:material!=null&&material.HasProperty("_RimStrength")?material.GetFloat("_RimStrength"):0);
                    _coatBlock.SetColor("_RimColor",state.HasAccent?state.RimColour:material!=null&&material.HasProperty("_RimColor")?material.GetColor("_RimColor"):Color.white);
                    _coatBlock.SetFloat("_FlashAmount",state.Flash*Mathf.Clamp01(Settings.SettingsStore.Current.EffectiveFlashIntensity));
                    _coatBlock.SetFloat("_CaughtAmount",ability?0:state.Frost);_coatBlock.SetFloat("_FrostAmount",ability?state.Frost:0);
                    if(ability){_coatBlock.SetColor("_FrostColor",coat.Body);_coatBlock.SetColor("_FrostRimColor",coat.Rim);}
                    surface.SetPropertyBlock(_coatBlock);
                }
            }
            if(_followSeat>=0&&TryPlayerPosition(_followSeat,out var followed))_camera.transform.position=followed+_followOffset;
            if(time<_lastTime){_sound=0;GameServices.Audio?.StopReplayCues();}
            while(_sound<_clip.Sounds.Length&&_clip.Sounds[_sound].Time<=time)
            {
                var cue=_clip.Sounds[_sound++];
                if(audible&&cue.Time>=_lastTime)
                {Vector3 p=_camera.WorldToViewportPoint(cue.Position);GameServices.Audio?.PlayReplayCue(cue.Id,cue.Pitch,cue.Gain*Mathf.Clamp01(1-(Vector3.Distance(_camera.transform.position,cue.Position)-2)/30),Mathf.Clamp(p.x*2-1,-1,1));}
            }
            _lastTime=time;_hidden.Clear();_previous.Clear();_hiddenLights.Clear();_lightWasEnabled.Clear();_hiddenCanvases.Clear();_canvasWasEnabled.Clear();
            var frame=_fields.Draw(_clip.FieldFrames,time);
            if(RecordedEffects!=null)
            {
                var material=Map.ArenaFx.Instance?.RecordedMaterial;
                if(material==null)throw new InvalidOperationException("Recorded Arena effect art is unavailable.");
                if(_arenaEffects==null)_arenaEffects=new RecordedArenaEffects(_stage.transform,material);
                _arenaEffects.Draw(LocalReplayEffectsCodec.At(RecordedEffects,time),_camera);_arenaEffects.Visible(false);
            }
            // Preparation also hides live objects and may lose its world owner.
            // Restore visibility even when playback fails before Camera.Render.
            try
            {
            if(RecordedScenery!=null){_scenery??=new RecordedSceneryView(_stage.transform);_scenery.Draw(LocalReplaySceneryCodec.At(RecordedScenery,time));}
            else _scenery?.Draw(null);
            foreach(var field in RecordedSpecialFields.Capture())if(field.Source!=null)Hide(field.Source);
            foreach(var effect in Object.FindObjectsByType<VfxRenderTag>())if(!effect.transform.IsChildOf(_stage.transform))Hide(effect.gameObject);
            foreach(var callout in Object.FindObjectsByType<ComicPopup>())Hide(callout.gameObject);
            // Animals use live private schedules, not recorded pose tracks.
            // Keep present-time visits out of past events; restore after render.
            foreach(var life in Object.FindObjectsByType<AmbientLife>())Hide(life.gameObject);
            // Arena's effects mesh is built facing the live camera, and its drones are live, not recorded.
            if(_arena!=null)
            {
                if(Map.ArenaFx.Instance!=null)Hide(Map.ArenaFx.Instance.gameObject);
                foreach(var drone in Object.FindObjectsByType<Map.ArenaDrone>())Hide(drone.gameObject);
            }
            _arenaEffects?.Visible(RecordedEffects!=null);
            _scenery?.Visible(RecordedScenery!=null);
            _fields.Visible(true);
            if(!_standalone)
            foreach(var actor in GameServices.Round.Bodies)if(actor!=null){Hide(actor.gameObject);var pet=actor.GetComponent<CharacterVisual>()?.Companion;if(pet!=null)Hide(pet.gameObject);}
            foreach(var shoe in Object.FindObjectsByType<Slipper>())Hide(shoe.gameObject);
            if(!_standalone&&GameServices.Round.Lata!=null)Hide(GameServices.Round.Lata.gameObject);
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>())Hide(arms.gameObject);
            foreach(var item in _items){item.Copy.ShowOnlyForCapture(true);item.Contact?.Visible(true);}
            _canLanding.Visible(true);
            _court.ShowForCapture(true);_lataClock.ShowForCapture(true);
            for(int i=0;i<_clip.Sounds.Length;i++)
            {
                var cue=_clip.Sounds[i];float age=time-cue.Time;
                if(CourtContactDust.Supports(cue.Id) && age>=0 && age<CourtContactDust.Life)
                {
                    if(!_contactDust.TryGetValue(i,out var dust) || dust==null)
                        _contactDust[i]=dust=CourtContactDust.Play(cue.Id,cue.Position,_stage.transform);
                    if(dust!=null){dust.Sample(age);dust.ShowForCapture(true);}
                }
                if(cue.Id!="court_escape" || age<0 || age>=CourtEscapePuff.Life)continue;
                if(!_escapePuffs.TryGetValue(i,out var puff))
                    _escapePuffs[i]=puff=CourtEscapePuff.Play(cue.Position,WorldCueProfile.Current.Escape,_stage.transform);
                puff.Sample(age);puff.ShowForCapture(true);
            }
            if(_tracking&&_followSeat<0)TrackShot(time);
            using var sceneState=LocalReplaySceneState.Apply(RecordedScene,time,UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            using var shaderTime=RecordedShaderClock.At(LocalReplaySceneState.ShaderTimeAt(RecordedScene,time));using var skyTime=NeighbourhoodSkyMotion.At(time);using var lighting=frame!=null?frame.Lighting.Use(_grade,_sky,_skyFill):null;_camera.Render();
            }
            finally{_scenery?.Visible(false);_arenaEffects?.Visible(false);foreach(var dust in _contactDust.Values)if(dust!=null)dust.ShowForCapture(false);_court.ShowForCapture(false);_lataClock.ShowForCapture(false);foreach(var puff in _escapePuffs.Values)puff.ShowForCapture(false);for(int i=0;i<_hiddenCanvases.Count;i++)if(_hiddenCanvases[i]!=null)_hiddenCanvases[i].enabled=_canvasWasEnabled[i];for(int i=0;i<_hiddenLights.Count;i++)if(_hiddenLights[i]!=null)_hiddenLights[i].enabled=_lightWasEnabled[i];_fields.Visible(false);foreach(var item in _items){item.Copy.ShowOnlyForCapture(false);item.Contact?.Visible(false);}_canLanding.Visible(false);for(int i=0;i<_hidden.Count;i++)if(_hidden[i]!=null)_hidden[i].forceRenderingOff=_previous[i];}
        }
        private void Hide(GameObject root)
        {
            foreach(var r in root.GetComponentsInChildren<Renderer>(true)){if(_hidden.Contains(r))continue;_hidden.Add(r);_previous.Add(r.forceRenderingOff);r.forceRenderingOff=true;}
            foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)){if(_hiddenCanvases.Contains(canvas))continue;_hiddenCanvases.Add(canvas);_canvasWasEnabled.Add(canvas.enabled);canvas.enabled=false;}
            foreach(var light in root.GetComponentsInChildren<Light>(true)){if(_hiddenLights.Contains(light))continue;_hiddenLights.Add(light);_lightWasEnabled.Add(light.enabled);light.enabled=false;}
        }
        public void Dispose()
        {
            _arenaEffects?.Dispose();_arenaEffects=null;
            _scenery?.Dispose();_scenery=null;
            Ready=false;GameServices.Audio?.StopReplayCues();_audioMix?.Dispose();_audioMix=null;
            if(_camera!=null)_camera.targetTexture=null;
            if(_target!=null){_target.Release();Object.Destroy(_target);}_target=null;
            if(_canvas!=null)Object.Destroy(_canvas.gameObject);_canvas=null;
            if(_frameMaterial!=null)Object.Destroy(_frameMaterial);_frameMaterial=null;
            if(_sky!=null)Object.Destroy(_sky);_sky=null;
            if(_stage!=null)Object.Destroy(_stage);_stage=null;
            if(_arena!=null)_arena.EndReplay();_arena=null;
            _items.Clear();_fields?.Dispose();_fields=null;_shotProps.Clear();
        }
    }
}
