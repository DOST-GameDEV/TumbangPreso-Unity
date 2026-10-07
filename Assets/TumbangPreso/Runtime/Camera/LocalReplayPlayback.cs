using System;
using System.Collections;
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
        private Task<RecordedMatchClip> _loading;
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
            Visual.WorldGround.TryBelow(Vector3.up*.5f,.5f,3.5f,out floor);
            var worldLook=Visual.WorldLookPresentation.Install(owner,floor);
            Visual.CourtSurfacePresentation.Install(owner,worldLook);
            Visual.LagoonDeckPresentation.Install(owner);
            Visual.FiestaBunting.Install(owner);
        }
        private void BuildControls()
        {
            _bar = HubKit.Span(HubKit.Rect(_canvas.transform, "ReplayToolbar"), Vector2.zero, new Vector2(1, 0),
                new Vector2(32, 24), new Vector2(32, -220));
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
            _play = Control("PlayPause", "PLAY", 28, 170, TogglePause);
            Control("Rewind", "−5 SEC", 214, 166, () => Seek(_time - 5));
            Control("Forward", "+5 SEC", 396, 166, () => Seek(_time + 5));
            _speedButton = Control("Speed", "1x", 578, 170, () => SetSpeed(_speed >= 4 ? .25f : _speed * 2));
            _followButton = Control("CameraMode", "FREE CAMERA", 764, 270, () => Follow(_follow >= 3 ? -1 : _follow + 1));
            Control("CleanPicture", "HIDE UI", 1050, 230, () => SetClean(!_clean));
            Control("CloseReplay", "BACK", 1296, 170, Close);
            var help = HubKit.Text(_bar, "CameraHelp", "Right mouse + WASD: fly · Q/E: down/up · Shift: faster · Space: play/pause · ←/→: seek · H: hide UI · Esc: back", HubStyle.Body, false, HubStyle.Paper);
            HubKit.Place(help.rectTransform, HubKit.BottomLeft, new Vector2(28, 4), new Vector2(1730, 46));
        }
        private HubButton Control(string name, string label, float x, float width, Action action)
        {
            var button = HubKit.Button(_bar, name, label, HubStyle.Honey, action, HubStyle.Label);
            HubKit.Place((RectTransform)button.transform, HubKit.BottomLeft, new Vector2(x, 60), new Vector2(width, 64));
            return button;
        }
        public void TogglePause() { _paused = !_paused; HubKit.SetLabel(_play, _paused ? "PLAY" : "PAUSE"); }
        public void SetSpeed(float speed)
        {
            if (float.IsNaN(speed) || float.IsInfinity(speed)) return;
            _speed = Mathf.Clamp(speed, .25f, 4); HubKit.SetLabel(_speedButton, _speed.ToString("0.##") + "x");
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
            if (index != _loaded && _loading == null) Request(index);
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
            _loading = Task.Run(() => LocalReplayStore.Read(entry, index));
        }
        private void Update()
        {
            if (_entry == null || _bar == null) return;
            if (_loading != null && _loading.IsCompleted)
            {
                var task = _loading; _loading = null;
                if (task.IsFaulted) { Fail(task.Exception.GetBaseException().Message); return; }
                try
                {
                    Vector3 eye = _view?.CameraTransform.position ?? new Vector3(0, 10, -15);
                    Quaternion rotation = _view?.CameraTransform.rotation ?? Quaternion.identity;
                    bool existing = _view != null;
                    if (_view == null || !_view.UseClip(task.Result,_continuousRead))
                    {
                        _view?.Dispose(); _view = new RecordedWorldView(transform, task.Result, true);
                        if (!_view.Ready) { Fail(_view.UnavailableReason ?? "The recorded world could not be opened."); return; }
                        if (existing) _view.CameraTransform.SetPositionAndRotation(eye, rotation);
                        _view.ShowLabels(false);
                    }
                    _loaded = _requested;
                    int wanted=SegmentAt(_time);if(wanted!=_loaded)Request(wanted);
                }
                catch (Exception error) { Fail(error.Message); return; }
            }
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard?.escapeKey.wasPressedThisFrame == true) { Close(); return; }
            if (keyboard?.spaceKey.wasPressedThisFrame == true) TogglePause();
            var selected=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            bool timelineFocused=selected!=null&&selected.GetComponent<Slider>()!=null;
            if (!timelineFocused&&keyboard?.leftArrowKey.wasPressedThisFrame == true) Seek(_time - 5);
            if (!timelineFocused&&keyboard?.rightArrowKey.wasPressedThisFrame == true) Seek(_time + 5);
            if (keyboard?.hKey.wasPressedThisFrame == true) SetClean(!_clean);
            bool aiming = mouse?.rightButton.isPressed == true && _view?.Ready == true;
            Cursor.lockState = aiming ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !aiming;
            if (_view?.Ready != true || _loading != null || Error != null) return;
            _view.ResizeForScreen();
            if (!_paused)
            {
                _time = Mathf.Min(_entry.Manifest.Duration, _time + Time.unscaledDeltaTime * _speed);
                // Segment boundaries are loaded lazily; keep at most one detached
                // segment in the view and one pending read, not the whole match.
                var segment = _entry.Manifest.Segments[_loaded];
                if (_time >= segment.Offset + segment.End - segment.Start && _loaded + 1 < _entry.Manifest.Segments.Count)
                { Request(_loaded + 1,true); return; }
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
