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
        private sealed class Item { public RecordedObjectTrack Track; public MatchPoseHistory.Copy Copy; public Transform[] Bones; }
        private readonly List<Item> _items=new List<Item>(13);
        private readonly List<Renderer> _hidden=new List<Renderer>();
        private readonly List<bool> _previous=new List<bool>();
        private readonly RecordedMatchClip _clip;
        private GameObject _stage;
        private IDisposable _audioMix;
        private Canvas _canvas;
        private Camera _camera;
        private RenderTexture _target;
        private Text _state;
        private readonly Dictionary<int,RecordedFieldView> _fields=new Dictionary<int,RecordedFieldView>();
        private readonly HashSet<int> _visibleFields=new HashSet<int>();
        private readonly MaterialPropertyBlock _coatBlock=new MaterialPropertyBlock();
        private int _sound;
        private float _lastTime;
        public bool Ready {get;private set;}
        public string UnavailableReason {get;private set;}
        public RenderTexture Target=>_target;
        public RecordedWorldView(Transform owner,RecordedMatchClip clip)
        {
            _clip=clip;_lastTime=clip.Start;
            try
            {
                if(Camera.main==null||clip.Map!=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name){UnavailableReason="Camera or map not ready: camera="+(Camera.main!=null)+" scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" clip="+clip.Map;return;}
                _stage=new GameObject("~RecordedWorld");_stage.transform.SetParent(owner,false);_stage.SetActive(false);
                foreach(var track in clip.Objects)
                {
                    GameObject source=Source(track);
                    if((source==null||MatchReplayArchive.VisualKey(source)!=track.VisualKey)&&
                        (track.Kind==RecordedObjectKind.Can||track.Kind==RecordedObjectKind.Slipper))source=CataloguedProp(track);
                    if(source==null){UnavailableReason="Missing recorded art: "+track.Kind+" P"+(track.Seat+1)+" skin="+track.Skin+" person="+track.Person;return;}
                    string visualKey=MatchReplayArchive.VisualKey(source);
                    if(visualKey!=track.VisualKey){UnavailableReason="Changed recorded art: "+track.Kind+" P"+(track.Seat+1)+" expected="+track.VisualKey+" actual="+visualKey;return;}
                    var history=new MatchPoseHistory.Track(GameServices.Round.PlayerAt(Mathf.Clamp(track.Seat,0,3)),source);
                    history.Record(Time.time);history.Record(Time.time+.05f);
                    var copy=history.Clone(_stage.transform);if(copy==null){UnavailableReason="Render copy failed: "+track.Kind;return;}
                    var bones=track.Pose.Bind(copy.Root);if(bones==null){UnavailableReason="Recorded pose binding changed: "+track.Kind;return;}
                    if(!source.scene.IsValid())ToonSkin.Apply(copy.Root,ToonSkin.PropOutlineWidth);
                    _items.Add(new Item{Track=track,Copy=copy,Bones=bones});
                    track.Pose.Apply(bones,clip.Contact);
                }
                _stage.SetActive(true);
                var cameraGo=new GameObject("RecordedWorldCamera");cameraGo.transform.SetParent(_stage.transform,false);
                _camera=cameraGo.AddComponent<Camera>();_camera.CopyFrom(Camera.main);_camera.enabled=false;_camera.tag="Untagged";
                _camera.cullingMask&=~(1<<5);_camera.nearClipPlane=.08f;_camera.fieldOfView=58;
                cameraGo.AddComponent<ColourGrade>().AdoptFromScene();
                var focus=_items.FirstOrDefault(i=>i.Track.Kind==RecordedObjectKind.Player&&i.Track.Seat==clip.Actor);
                var subject=_items.FirstOrDefault(i=>clip.Subject>=0?i.Track.Kind==RecordedObjectKind.Player&&i.Track.Seat==clip.Subject:i.Track.Kind==RecordedObjectKind.Can);
                if(focus==null||subject==null)return;
                Vector3 a=focus.Bones[0].position,b=subject.Bones[0].position;
                Vector3 centre=(a+b)*.5f+Vector3.up*.6f;
                float distance=Mathf.Clamp(Vector3.Distance(a,b)*1.05f+4,5,17);
                Vector3 side=Vector3.Cross((b-a).normalized,Vector3.up);if(side.sqrMagnitude<.1f)side=Vector3.right;
                // A stable broad camera preserves the whole actual throw/chase.
                Vector3 eye=default;bool clear=false;
                for(int shot=0;shot<8;shot++)
                {
                    eye=centre+(Quaternion.AngleAxis(shot*45,Vector3.up)*side)*distance+Vector3.up*(distance*.6f);
                    if(Clear(centre,eye)){clear=true;break;}
                }
                if(!clear){UnavailableReason="No clear replay angle";return;}
                _camera.transform.position=eye;_camera.transform.LookAt(centre);
                int width=Mathf.Clamp(Screen.width,960,1920),height=Mathf.RoundToInt(width*Screen.height/(float)Mathf.Max(1,Screen.width));
                _target=new RenderTexture(width,Mathf.Max(540,height),24,RenderTextureFormat.ARGB32){name="RetainedMatchFrame"};_target.Create();_camera.targetTexture=_target;
                _canvas=OwnerUiLayout.Canvas(owner,"CanonicalReplayCanvas",240);
                var input=_canvas.GetComponent<InputLayer.ScreenFocus>();if(input!=null)input.enabled=false;
                var picture=OwnerUiLayout.Rect(_canvas.transform,"RecordedWorldFrame").gameObject.AddComponent<RawImage>();
                OwnerUiLayout.Fill(picture.rectTransform);picture.texture=_target;picture.raycastTarget=false;
                var band=OwnerUiLayout.Rect(_canvas.transform,"ReplayIdentity");band.anchorMin=new Vector2(0,1);band.anchorMax=Vector2.one;band.pivot=new Vector2(.5f,1);band.sizeDelta=new Vector2(0,100);
                var plate=band.gameObject.AddComponent<Image>();plate.color=new Color(.035f,.07f,.06f,.94f);plate.raycastTarget=false;
                var label=OwnerUiLayout.Text(band,"ReplayLabel","HALFTIME REPLAY  /  "+clip.Reason+"  /  "+PlayerIdentity.Label(clip.Actor)+" · "+(focus.Track.DisplayName??"PLAYER")+(clip.Subject>=0?" CAUGHT "+PlayerIdentity.Label(clip.Subject)+" · "+(_items.FirstOrDefault(i=>i.Track.Kind==RecordedObjectKind.Player&&i.Track.Seat==clip.Subject)?.Track.DisplayName??"PLAYER"):""),34,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Fill(label.rectTransform);label.alignment=TextAnchor.MiddleCenter;label.color=OwnerUiTheme.Current.Pale;
                var footer=OwnerUiLayout.Rect(_canvas.transform,"ReplayState");footer.anchorMin=Vector2.zero;footer.anchorMax=new Vector2(1,0);footer.pivot=new Vector2(.5f,0);footer.sizeDelta=new Vector2(0,62);
                var footerPlate=footer.gameObject.AddComponent<Image>();footerPlate.color=new Color(.035f,.07f,.06f,.92f);footerPlate.raycastTarget=false;
                _state=OwnerUiLayout.Text(footer,"RecordedCanState","",27,OwnerUiLayout.TypeRole.Display);OwnerUiLayout.Fill(_state.rectTransform);_state.alignment=TextAnchor.MiddleCenter;_state.color=OwnerUiTheme.Current.Pale;
                _audioMix=GameServices.Audio?.EnterReplayMix();
                Ready=true;
            }
            catch{Dispose();throw;}
        }
        private static GameObject CataloguedProp(RecordedObjectTrack track)
        {
            var book=RosterBook.Load();if(book==null)return null;
            var entries=track.Kind==RecordedObjectKind.Can?book.Cans:book.Slippers;
            foreach(var entry in entries)
                if(entry!=null&&entry.Model!=null&&MatchReplayArchive.VisualKey(entry.Model)==track.VisualKey)return entry.Model;
            return null;
        }
        private static GameObject Source(RecordedObjectTrack track)
        {
            var round=GameServices.Round;if(round==null)return null;
            var actor=round.PlayerAt(track.Seat);
            if(track.Kind==RecordedObjectKind.Player)
                return actor!=null&&actor.CharacterIndex==track.Skin&&Core.Roster.PersonIdAt(actor.Mode,actor.CharacterIndex)==track.Person?actor.GetComponent<CharacterVisual>()?.Model:null;
            if(track.Kind==RecordedObjectKind.Familiar)return actor?.GetComponent<CharacterVisual>()?.Companion?.gameObject;
            if(track.Kind==RecordedObjectKind.Can)return round.Lata!=null?MatchReplayArchive.PropModel(round.Lata.gameObject):null;
            foreach(var shoe in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include))if(shoe.SeatOfOrigin==track.Seat)return MatchReplayArchive.PropModel(shoe.gameObject);
            return null;
        }
        private static bool Clear(Vector3 centre,Vector3 eye)
        {
            foreach(var hit in Physics.RaycastAll(centre,(eye-centre).normalized,Vector3.Distance(centre,eye),~0,QueryTriggerInteraction.Ignore))
                if(hit.collider.GetComponentInParent<CharacterMotor>()==null&&hit.collider.GetComponentInParent<Slipper>()==null&&hit.collider.GetComponentInParent<Lata>()==null)return false;
            return true;
        }
        public void Draw(float time,bool audible=true)
        {
            if(!Ready)return;
            time=Mathf.Clamp(time,_clip.Start,_clip.End);
            var can=_items.FirstOrDefault(i=>i.Track.Kind==RecordedObjectKind.Can);
            if(can!=null&&_state!=null){int state=can.Track.Pose.StateAt(time).State;_state.text=(state&2)!=0?"CAN PROTECTED":(state&1)!=0?"CAN UPRIGHT":"CAN DOWN  /  RETRIEVE YOUR TSINELAS";}
            foreach(var item in _items)
            {
                item.Track.Pose.Apply(item.Bones,time);var state=item.Track.Pose.StateAt(time);if(!state.HasCoat)continue;
                bool ability=state.Element!=StunElement.None;var coat=StunCoat.For(state.Element);
                foreach(var surface in item.Copy.Renderers)
                {
                    surface.GetPropertyBlock(_coatBlock);_coatBlock.SetFloat("_FlashAmount",state.Flash*Mathf.Clamp01(Settings.SettingsStore.Current.FlashIntensity));
                    _coatBlock.SetFloat("_CaughtAmount",ability?0:state.Frost);_coatBlock.SetFloat("_FrostAmount",ability?state.Frost:0);
                    if(ability){_coatBlock.SetColor("_FrostColor",coat.Body);_coatBlock.SetColor("_FrostRimColor",coat.Rim);}
                    surface.SetPropertyBlock(_coatBlock);
                }
            }
            if(time<_lastTime){_sound=0;GameServices.Audio?.StopReplayCues();}
            while(_sound<_clip.Sounds.Length&&_clip.Sounds[_sound].Time<=time)
            {
                var cue=_clip.Sounds[_sound++];
                if(audible&&cue.Time>=_lastTime)
                {Vector3 p=_camera.WorldToViewportPoint(cue.Position);GameServices.Audio?.PlayReplayCue(cue.Id,cue.Pitch,cue.Gain*Mathf.Clamp01(1-(Vector3.Distance(_camera.transform.position,cue.Position)-2)/30),Mathf.Clamp(p.x*2-1,-1,1));}
            }
            _lastTime=time;_hidden.Clear();_previous.Clear();
            RecordedFieldFrame frame=null;
            foreach(var snapshot in _clip.FieldFrames){if(snapshot.Time>time)break;frame=snapshot;}
            _visibleFields.Clear();
            if(frame!=null)foreach(var field in frame.Fields)
            {
                if(field.State.Remaining<=time-frame.Time)continue;
                _visibleFields.Add(field.Id);
                if(_fields.TryGetValue(field.Id,out var existing)&&!existing.Matches(field.State)){existing.Dispose();_fields.Remove(field.Id);}
                if(!_fields.TryGetValue(field.Id,out var view))_fields[field.Id]=view=new RecordedFieldView(_stage.transform,field.State);
                view.Sample(field.State,time-frame.Time);
            }
            foreach(int id in _fields.Keys.ToArray())if(!_visibleFields.Contains(id)){_fields[id].Dispose();_fields.Remove(id);}
            foreach(var field in Net.WorldEffectSnapshot.Capture())if(field.Source!=null)Hide(field.Source);
            foreach(var field in _fields.Values)field.Visible(true);
            foreach(var actor in GameServices.Round.Players)if(actor!=null){Hide(actor.gameObject);var pet=actor.GetComponent<CharacterVisual>()?.Companion;if(pet!=null)Hide(pet.gameObject);}
            foreach(var shoe in Object.FindObjectsByType<Slipper>())Hide(shoe.gameObject);
            if(GameServices.Round.Lata!=null)Hide(GameServices.Round.Lata.gameObject);
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>())Hide(arms.gameObject);
            foreach(var item in _items)item.Copy.ShowOnlyForCapture(true);
            try{_camera.Render();}
            finally{foreach(var field in _fields.Values)field.Visible(false);foreach(var item in _items)item.Copy.ShowOnlyForCapture(false);for(int i=0;i<_hidden.Count;i++)if(_hidden[i]!=null)_hidden[i].forceRenderingOff=_previous[i];}
        }
        private void Hide(GameObject root)
        {foreach(var r in root.GetComponentsInChildren<Renderer>(true)){if(_hidden.Contains(r))continue;_hidden.Add(r);_previous.Add(r.forceRenderingOff);r.forceRenderingOff=true;}}
        public void Dispose()
        {
            Ready=false;GameServices.Audio?.StopReplayCues();_audioMix?.Dispose();_audioMix=null;
            if(_camera!=null)_camera.targetTexture=null;
            if(_target!=null){_target.Release();Object.Destroy(_target);}_target=null;
            if(_canvas!=null)Object.Destroy(_canvas.gameObject);_canvas=null;
            if(_stage!=null)Object.Destroy(_stage);_stage=null;
            _items.Clear();foreach(var field in _fields.Values)field.Dispose();_fields.Clear();
        }
    }
}
