using System;
using System.Collections;
using System.Globalization;
using System.Threading.Tasks;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.CameraSystem
{
    // A replay loads map geometry, not MatchInstaller's gameplay path. Playback
    // time and its render-only camera never write match state or reward stores.
    public sealed class LocalReplayPlayback : MonoBehaviour
    {
        private static LocalReplayPlayback _current;
        public static bool Active => _current != null;
        private LocalReplayEntry _entry;
        private RecordedWorldView _view;
        private Canvas _canvas;
        private RectTransform _bar;
        private Text _status;
        private HubButton _play, _followButton, _speedButton;
        private Slider _seek;
        private InputField _jumpField;
        private Text _timeHint;
        private sealed class LoadedSegment { public RecordedMatchClip Clip; public LocalReplaySceneSegment Scene; public LocalReplayFxSegment Effects;public LocalReplayScenerySegment Scenery; }
        private Task<LoadedSegment> _loading;
        private LoadedSegment _prepared;
        private int _preparedIndex=-1;
        private string _preparedError;
        private int _loaded = -1, _requested = -1, _follow = -1;
        private float _time, _speed = 1;
        private bool _paused = true, _clean, _seeking, _silentNextFrame, _continuousRead;
        private Vector3 _followOffset;
        private GameMode _previousMode;
        public float Position => _time;
        public bool Paused => _paused;
        public float Speed => _speed;
        public int FollowSeat => _follow;
        public string Error { get; private set; }
        public RenderTexture Frame => _view?.Target;

        public static bool Open(LocalReplayEntry entry)
        {
            if (Active || entry == null || entry.Manifest.Segments.Count == 0 ||
                Net.NetSession.Instance?.IsNetworked == true || !Array.Exists(SceneFlow.Maps, m => m == entry.Manifest.Map)) return false;
            var go = new GameObject("LocalReplayPlayback");
            _current = go.AddComponent<LocalReplayPlayback>();
            _current._entry = entry; _current._previousMode = SceneFlow.SelectedMode;
            DontDestroyOnLoad(go);
            _current.StartCoroutine(_current.LoadCourt());
            return true;
        }
        private IEnumerator LoadCourt()
        {
            Enum.TryParse(_entry.Manifest.Mode, out GameMode mode);
            SceneFlow.SelectedMode = mode;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            _canvas = OwnerUiLayout.Canvas(transform, "LocalReplayControls", 260);
            DontDestroyOnLoad(_canvas.gameObject);
            _status = HubKit.Text(_canvas.transform, "ReplayStatus", "OPENING REPLAY…", HubStyle.Label, false, HubStyle.Paper);
            var outline=_status.gameObject.AddComponent<Outline>();outline.effectColor=UiTheme.InGameOutline;outline.effectDistance=new Vector2(1.5f,-1.5f);
            HubKit.Place(_status.rectTransform, HubKit.TopLeft, new Vector2(46, -28), new Vector2(1800, 72));
            var load = SceneManager.LoadSceneAsync(_entry.Manifest.Map);
            if (load == null) { Fail("This replay's court could not be opened."); yield break; }
            while (!load.isDone) yield return null;
            // Destination Start chooses the replay-only installer route.
            yield return null;
            BuildControls();
            Seek(0);
        }
        public static void PrepareCourt(Transform owner)
        {
            if (!Active) return;
            MatchInstaller.MeasurePlayableBounds();
            if (Camera.main == null)
            {
                var go = new GameObject("ReplayMapCamera"); go.transform.SetParent(owner, false);
                go.tag = "MainCamera"; var camera = go.AddComponent<Camera>();
                camera.transform.position = new Vector3(0, 10, -15); camera.transform.LookAt(Vector3.zero);
                camera.fieldOfView = 58; camera.nearClipPlane = .08f;
                go.AddComponent<Visual.ColourGrade>().AdoptFromScene();
                go.AddComponent<Visual.WorldOutline>();
            }
            if(UnityEngine.Object.FindAnyObjectByType<AudioListener>()==null)Camera.main.gameObject.AddComponent<AudioListener>();
            float floor=0;
            // A miss returns negative infinity, not the initialized fallback.
            // Arena can have an open centre; inspect nearby real support too.
            if(Visual.WorldGround.TryBelow(Vector3.up*50,0,100,out float found)&&!float.IsNaN(found)&&!float.IsInfinity(found))floor=found;
            else
            {
                foreach(var point in new[]{new Vector3(3,50,0),new Vector3(-3,50,0),new Vector3(0,50,3),new Vector3(0,50,-3)})
                    if(Visual.WorldGround.TryBelow(point,0,100,out found)&&!float.IsNaN(found)&&!float.IsInfinity(found)){floor=found;break;}
            }
            var worldLook=Visual.WorldLookPresentation.Install(owner,floor);
            Visual.CourtSurfacePresentation.Install(owner,worldLook);
            Visual.LagoonDeckPresentation.Install(owner);
            Visual.FiestaBunting.Install(owner);
        }
        private void BuildControls()
        {
            _bar = HubKit.Span(HubKit.Rect(_canvas.transform, "ReplayToolbar"), Vector2.zero, new Vector2(1, 0),
                new Vector2(32, 24), new Vector2(32, -310));
            HubKit.Plate(_bar, "Plate", HubStyle.Night, 1400);
            _seek = HubKit.Rect(_bar, "Timeline").gameObject.AddComponent<ReplayTimelineSlider>();
            HubKit.Span((RectTransform)_seek.transform, new Vector2(0, 1), Vector2.one, new Vector2(28, -54), new Vector2(28, 14));
            var track = HubKit.Rect(_seek.transform, "Track").gameObject.AddComponent<Image>();
            track.color = new Color(.65f, .55f, .38f, 1); HubKit.Stretch(track.rectTransform);
            var fill = HubKit.Rect(_seek.transform, "Fill").gameObject.AddComponent<Image>();
            fill.color = HubStyle.Honey; HubKit.Stretch(fill.rectTransform); _seek.fillRect = fill.rectTransform;
            var handle = HubKit.Rect(_seek.transform, "Handle").gameObject.AddComponent<Image>();
            handle.color = HubStyle.Paper; handle.rectTransform.sizeDelta = new Vector2(26, 48);
            _seek.handleRect = handle.rectTransform; _seek.targetGraphic = handle;
            _seek.minValue = 0; _seek.maxValue = _entry.Manifest.Duration;
            _seek.onValueChanged.AddListener(value => { if (!_seeking) Seek(value); });
            _play = Control("PlayPause", "PLAY", 28, 150, TogglePause,140);
            Control("Rewind", "−5 SEC", 194, 150, () => Seek(_time - 5),140);
            Control("Forward", "+5 SEC", 360, 150, () => Seek(_time + 5),140);
            Control("Slower", "SLOWER", 526, 150, () => SetSpeed(_speed*.5f),140);
            _speedButton = Control("Speed", "1x", 692, 150, () => SetSpeed(1),140);
            Control("Faster", "FASTER", 858, 150, () => SetSpeed(_speed*2),140);
            _followButton = Control("CameraMode", "FREE CAMERA", 1024, 240, () => Follow(_follow >= 3 ? -1 : _follow + 1),140);
            Control("CleanPicture", "HIDE UI", 1280, 210, () => SetClean(!_clean),140);
            Control("CloseReplay", "BACK", 1506, 150, Close,140);
            Control("JumpStart", "START",28,150,()=>Seek(0));
            Control("JumpEnd", "END",194,150,()=>{Pause();Seek(_entry.Manifest.Duration);});
            Control("StepBack", "STEP −",360,150,()=>Step(-1));
            Control("StepForward", "STEP +",526,150,()=>Step(1));
            _jumpField=HubField.Build(_bar,"ExactTime","","MM:SS or seconds",24,1401);
            HubKit.Place((RectTransform)_jumpField.transform,HubKit.BottomLeft,new Vector2(692,60),new Vector2(280,64));
            Control("JumpTime","GO",988,150,()=>SeekText(_jumpField.text));
            Control("PreviousRound","PREV ROUND",1154,230,()=>JumpRound(-1));
            Control("NextRound","NEXT ROUND",1400,230,()=>JumpRound(1));
            _timeHint=HubKit.Text(_bar,"TimeFeedback","",HubStyle.Body,false,HubStyle.Paper);
            HubKit.Place(_timeHint.rectTransform,HubKit.BottomLeft,new Vector2(1650,60),new Vector2(180,64));
            var help = HubKit.Text(_bar, "CameraHelp", "Right mouse: look · WASD: fly · Q/E: height · Space: pause · Arrows: seek · ,/.: step · H: hide UI · Esc: back", HubStyle.Body, false, HubStyle.Paper);
            HubKit.Place(help.rectTransform, HubKit.BottomLeft, new Vector2(28, 4), new Vector2(1730, 46));
        }
        private HubButton Control(string name, string label, float x, float width, Action action,float y=60)
        {
            var button = HubKit.Button(_bar, name, label, HubStyle.Honey, action, HubStyle.Label);
            HubKit.Place((RectTransform)button.transform, HubKit.BottomLeft, new Vector2(x, y), new Vector2(width, 64));
            HubKit.Fit(HubKit.LabelOf(button),width-36);
            return button;
        }
        public void TogglePause() { _paused = !_paused; HubKit.SetLabel(_play, _paused ? "PLAY" : "PAUSE"); }
        private void Pause(){_paused=true;HubKit.SetLabel(_play,"PLAY");GameServices.Audio?.StopReplayCues();}
        public void Step(int direction){Pause();Seek(_time+Mathf.Sign(direction)*MatchPoseHistory.Interval);}
        public void JumpRound(int direction)
        {
            int round=_entry.Manifest.Segments[SegmentAt(_time)].Round;
            int target=round+(direction<0?-1:1);
            foreach(var segment in _entry.Manifest.Segments)if(segment.Round==target){Seek(segment.Offset);return;}
            Seek(direction<0?0:_entry.Manifest.Duration);
        }
        public bool SeekText(string text)
        {
            string[] parts=(text??"").Trim().Split(':');double seconds=0;
            bool valid=parts.Length>=1&&parts.Length<=3;
            for(int i=0;valid&&i<parts.Length;i++)
            {
                valid=double.TryParse(parts[i],NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out double value)&&
                    !double.IsNaN(value)&&!double.IsInfinity(value)&&value>=0;
                if(valid&&i<parts.Length-1)valid=value==Math.Floor(value);
                if(valid&&parts.Length>1&&i>0)valid=value<60;
                if(valid)seconds=seconds*60+value;
            }
            valid=valid&&seconds<=_entry.Manifest.Duration;
            if(!valid){if(_timeHint!=null)_timeHint.text="INVALID TIME";return false;}
            if(_timeHint!=null)_timeHint.text="";Seek((float)seconds);return true;
        }
        public void SetSpeed(float speed)
        {
            if (float.IsNaN(speed) || float.IsInfinity(speed)) return;
            _speed = Mathf.Clamp(speed, .25f, 4); HubKit.SetLabel(_speedButton, _speed.ToString("0.##") + "x");
            HubKit.Fit(HubKit.LabelOf(_speedButton),114);
        }
        public void Follow(int seat)
        {
            _follow = Mathf.Clamp(seat, -1, 3);
            if (_follow >= 0 && _view != null && _view.TryPlayerPosition(_follow, out var position))
                _followOffset = _view.CameraTransform.position - position;
            HubKit.SetLabel(_followButton, _follow < 0 ? "FREE CAMERA" : "FOLLOW P" + (_follow + 1));
        }
        public void SetClean(bool clean)
        {
            _clean = clean; if (_bar != null) _bar.gameObject.SetActive(!clean);
            if (_status != null) _status.gameObject.SetActive(!clean);
            _view?.ShowLabels(false);
        }
        public void Seek(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            _time = Mathf.Clamp(value, 0, _entry.Manifest.Duration);
            int index=SegmentAt(_time);
            _continuousRead=false;
            if(index!=_loaded&&index!=_preparedIndex&&_loading==null)Request(index);
            GameServices.Audio?.StopReplayCues();
            _silentNextFrame=true;
        }
        private int SegmentAt(float time)
        {
            int index = _entry.Manifest.Segments.Count - 1;
            for (int i = 0; i < _entry.Manifest.Segments.Count; i++)
            {
                var segment = _entry.Manifest.Segments[i];
                if (time < segment.Offset + segment.End - segment.Start) { index = i; break; }
            }
            return index;
        }
        private void Request(int index,bool continuous=false)
        {
            _requested = index;_continuousRead=continuous;
            var entry = _entry;
            _loading = Task.Run(() => new LoadedSegment{Clip=LocalReplayStore.Read(entry,index),Scene=LocalReplayStore.ReadScene(entry,index),Effects=LocalReplayStore.ReadEffects(entry,index),Scenery=LocalReplayStore.ReadScenery(entry,index)});
        }
        private bool UseSegment(int index,bool continuous)
        {
            if(index==_loaded)return true;
            if(index!=_preparedIndex)
            {if(_loading==null)Request(index,continuous);return false;}
            if(_preparedError!=null){Fail(_preparedError);return false;}
            var segment=_prepared;_prepared=null;_preparedIndex=-1;
            try
            {
                Vector3 eye=_view?.CameraTransform.position??new Vector3(0,10,-15);
                Quaternion rotation=_view?.CameraTransform.rotation??Quaternion.identity;
                bool existing=_view!=null;
                if(_view==null||!_view.UseClip(segment.Clip,continuous))
                {
                    _view?.Dispose();_view=new RecordedWorldView(transform,segment.Clip,true);
                    if(!_view.Ready){Fail(_view.UnavailableReason??"The recorded world could not be opened.");return false;}
                    if(existing)_view.CameraTransform.SetPositionAndRotation(eye,rotation);
                    _view.ShowLabels(false);
                }
                _view.RecordedScene=segment.Scene;_view.RecordedEffects=segment.Effects;
                _view.RecordedScenery=segment.Scenery;
                _loaded=index;_continuousRead=false;return true;
            }
            catch(Exception error){Fail(error.Message);return false;}
        }
        private void LookAhead()
        {
            int next=_loaded+1;
            if(_loading==null&&_preparedIndex!=next&&next<_entry.Manifest.Segments.Count)
            { _prepared=null;_preparedError=null;_preparedIndex=-1;Request(next,true); }
        }
        private void Update()
        {
            if (_entry == null || _bar == null) return;
            try { UpdatePlayback(); }
            catch(Exception error) { Fail(error.Message); }
        }
        private void UpdatePlayback()
        {
            if (_loading != null && _loading.IsCompleted)
            {
                var task=_loading;int index=_requested;_loading=null;
                int wanted=SegmentAt(_time);
                // A seek can supersede a read before it finishes. Obsolete data
                // and failures never replace or poison the final requested view.
                bool relevant=index==wanted||(wanted==_loaded&&index==_loaded+1);
                if(relevant)
                {
                    _preparedIndex=index;
                    _preparedError=task.IsFaulted?task.Exception.GetBaseException().Message:task.IsCanceled?"The replay read was cancelled.":null;
                    _prepared=_preparedError==null?task.Result:null;
                }
                else if(task.IsFaulted){var observed=task.Exception;}
            }
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            var selected=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            bool editingTime=selected!=null&&selected.GetComponent<InputField>()!=null;
            if (keyboard?.escapeKey.wasPressedThisFrame == true) { Close(); return; }
            if (!editingTime&&keyboard?.spaceKey.wasPressedThisFrame == true) TogglePause();
            bool timelineFocused=selected!=null&&selected.GetComponent<Slider>()!=null;
            if (!editingTime&&!timelineFocused&&keyboard?.leftArrowKey.wasPressedThisFrame == true) Seek(_time - 5);
            if (!editingTime&&!timelineFocused&&keyboard?.rightArrowKey.wasPressedThisFrame == true) Seek(_time + 5);
            if (!editingTime&&keyboard?.commaKey.wasPressedThisFrame==true)Step(-1);
            if (!editingTime&&keyboard?.periodKey.wasPressedThisFrame==true)Step(1);
            if (!editingTime&&keyboard?.hKey.wasPressedThisFrame == true) SetClean(!_clean);
            bool aiming = mouse?.rightButton.isPressed == true && _view?.Ready == true;
            Cursor.lockState = aiming ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !aiming;
            if(Error!=null||!UseSegment(SegmentAt(_time),_continuousRead)||_view?.Ready!=true)return;
            LookAhead();
            _view.ResizeForScreen();
            if (!_paused)
            {
                _time = Mathf.Min(_entry.Manifest.Duration, _time + Time.unscaledDeltaTime * _speed);
                int wanted=SegmentAt(_time);
                if(wanted!=_loaded)
                {
                    _continuousRead=wanted==_loaded+1;
                    if(!UseSegment(wanted,_continuousRead))return;
                    LookAhead();
                }
                if (_time >= _entry.Manifest.Duration) { _paused = true; HubKit.SetLabel(_play, "PLAY"); }
            }
            if (aiming)
            {
                var camera = _view.CameraTransform; Vector2 delta = mouse.delta.ReadValue() * .12f;
                Vector3 angles = camera.eulerAngles; float pitch = angles.x > 180 ? angles.x - 360 : angles.x;
                camera.rotation = Quaternion.Euler(Mathf.Clamp(pitch - delta.y, -89, 89), angles.y + delta.x, 0);
                if (_follow < 0 && keyboard != null)
                {
                    Vector3 move = new Vector3((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                        (keyboard.eKey.isPressed ? 1 : 0) - (keyboard.qKey.isPressed ? 1 : 0),
                        (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                    camera.position += (camera.right * move.x + Vector3.up * move.y + camera.forward * move.z) *
                        (keyboard.leftShiftKey.isPressed ? 18 : 6) * Time.unscaledDeltaTime;
                }
                else if (_follow >= 0) _followOffset = camera.rotation * Vector3.back * Mathf.Max(2, _followOffset.magnitude);
            }
            var current = _entry.Manifest.Segments[_loaded];
            float sourceTime = current.Start + Mathf.Clamp(_time - current.Offset, 0, current.End - current.Start);
            _view.FollowCamera(_follow,_followOffset);
            _view.Draw(sourceTime, !_paused && _speed == 1 && !_silentNextFrame);
            _silentNextFrame=false;
            _seeking = true; _seek.SetValueWithoutNotify(_time); _seeking = false;
            _status.text = SceneFlow.PreviewFor(_entry.Manifest.Map).Name + " · ROUND " + current.Round + " · " +
                TimeLabel(_time) + " / " + TimeLabel(_entry.Manifest.Duration) +
                (_entry.Manifest.Completed ? "" : " · PARTIAL RECORDING");
        }
        private static string TimeLabel(float time) => ((int)time / 60).ToString("00") + ":" + ((int)time % 60).ToString("00");
        private void Fail(string error)
        {
            Error = error; _paused = true;
            if (_status != null) { _status.gameObject.SetActive(true); _status.text = "REPLAY UNAVAILABLE · " + error; }
            Debug.LogWarning("[LocalReplay] " + error);
        }
        public void Close()
        {
            SceneFlow.SelectedMode = _previousMode;
            Destroy(gameObject); _current = null;
            SceneFlow.Go(SceneFlow.MatchSetup);
        }
        private void OnDestroy()
        {
            var pending=_loading;_loading=null;_prepared=null;_preparedError=null;_preparedIndex=-1;_entry=null;
            if(pending!=null)pending.ContinueWith(task=>{var observed=task.Exception;},System.Threading.CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted|TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
            _view?.Dispose(); _view = null;
            if (_canvas != null) Destroy(_canvas.gameObject);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (_current == this) _current = null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _current = null;
    }
    // The stock Slider jumps ten percent of the whole range. A long replay needs
    // precise navigation: five seconds on keyboard/pad, continuous pointer seek.
    public sealed class ReplayTimelineSlider : Slider
    {
        public override void OnMove(UnityEngine.EventSystems.AxisEventData data)
        {
            if(data.moveDir==UnityEngine.EventSystems.MoveDirection.Left||data.moveDir==UnityEngine.EventSystems.MoveDirection.Right)
            {
                if(IsActive()&&IsInteractable())value+=data.moveDir==UnityEngine.EventSystems.MoveDirection.Left?-5:5;
                data.Use();return;
            }
            base.OnMove(data);
        }
    }
}
