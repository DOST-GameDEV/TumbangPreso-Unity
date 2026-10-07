using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.Diagnostics
{
    // Runs only after the opt-in packaged startup acceptance has succeeded.
    public sealed class LocalReplayPackageProbe : MonoBehaviour
    {
        [Serializable] private sealed class Receipt
        {
            public bool passed;
            public string error;
            public bool requiredRecordedEffects;
            public int recordedEffectFrames, recordedQuads, meshVertices;
            public string effectsTexture;
            public List<string> stages = new List<string>();
        }
        private readonly Receipt _receipt = new Receipt();
        private string _folder, _library;
        private int _phase;
        private float _started, _phaseAt, _playingAt;
        private bool _finished;
        private LocalReplayPlayback _viewer;
        private bool _requireEffects;

        internal static bool TryBegin(string output)
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-tp-replay-package");
            if (Application.isEditor || at < 0 || at + 1 >= args.Length) return false;
            var root = new GameObject("~LocalReplayPackageProbe"); DontDestroyOnLoad(root);
            var probe = root.AddComponent<LocalReplayPackageProbe>();
            probe._folder = output; probe._library = Path.GetFullPath(args[at + 1]);
            probe._requireEffects = Array.IndexOf(args, "-tp-replay-effects-required") >= 0;
            probe._receipt.requiredRecordedEffects = probe._requireEffects;
            probe._started = Time.realtimeSinceStartup;
            return true;
        }
        private static void Require(bool condition, string error)
        { if (!condition) throw new InvalidOperationException(error); }
        private void Stage(string stage)
        { _receipt.stages.Add(stage); _phaseAt = Time.realtimeSinceStartup; Debug.Log("[ReplayPackage] " + stage); }
        private void CheckRecordedEffects()
        {
            const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            var view = (RecordedWorldView)typeof(LocalReplayPlayback).GetField("_view", hidden).GetValue(_viewer);
            Require(view?.RecordedEffects != null && view.RecordedEffects.Frames.Count > 0,
                "The required fresh recording has no decoded render-frame effects.");
            var entry = (LocalReplayEntry)typeof(LocalReplayPlayback).GetField("_entry", hidden).GetValue(_viewer);
            int loaded = (int)typeof(LocalReplayPlayback).GetField("_loaded", hidden).GetValue(_viewer);
            Require(loaded >= 0 && loaded < entry.Manifest.Segments.Count, "Effects have no actual loaded segment.");
            var segment = entry.Manifest.Segments[loaded];
            float sourceTime = segment.Start + Mathf.Clamp(_viewer.Position - segment.Offset, 0, segment.End - segment.Start);
            var quads = LocalReplayEffectsCodec.At(view.RecordedEffects, sourceTime);
            var owned = (RecordedArenaEffects)typeof(RecordedWorldView).GetField("_arenaEffects", hidden).GetValue(view);
            Require(owned != null && quads != null && quads.Length > 0, "Seek did not reconstruct recorded effects.");
            var mesh = (Mesh)typeof(RecordedArenaEffects).GetField("_mesh", hidden).GetValue(owned);
            var renderer = (MeshRenderer)typeof(RecordedArenaEffects).GetField("_renderer", hidden).GetValue(owned);
            Require(mesh != null && mesh.vertexCount == quads.Length * 4, "The owned mesh differs from the recorded quad count.");
            Require(renderer?.sharedMaterial?.mainTexture != null, "The actual recorded-effects atlas is missing.");
            _receipt.recordedEffectFrames = view.RecordedEffects.Frames.Count;
            _receipt.recordedQuads = quads.Length; _receipt.meshVertices = mesh.vertexCount;
            _receipt.effectsTexture = renderer.sharedMaterial.mainTexture.name;
            Stage("fresh-effects-decoded-and-owned-mesh-bound");
        }
        private static Button ButtonNamed(string name)
        { var root = GameObject.Find(name); return root == null ? null : root.GetComponent<Button>(); }
        private static void Click(string name)
        {
            var button = ButtonNamed(name); Require(button != null && button.IsInteractable(), name + " unavailable");
            var canvas = button.GetComponentInParent<Canvas>(); var rect = (RectTransform)button.transform;
            Canvas.ForceUpdateCanvases();
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Require(hits.Count > 0, name + " has no UI raycast");
            var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Require(target == button.gameObject, name + " is covered");
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
        }
        private void Update()
        {
            if (_finished) return;
            try { Step(); } catch (Exception error) { Finish(false, error.ToString()); }
        }
        private void Step()
        {
            Require(Time.realtimeSinceStartup - _started < 90, "Replay package stopping condition exceeded");
            if(_viewer!=null)Require(_viewer.Error==null,_viewer.Error);
            if (_phase == 0)
            {
                Require(LocalReplayStore.SetFolder(_library, out string error), error);
                TumpHub.Current.Push<HubMenu>(); Stage("menu-open"); _phase = 1;
            }
            else if (_phase == 1 && ButtonNamed("MenuREPLAYS") != null)
            { Click("MenuREPLAYS"); Stage("replays-route"); _phase = 2; }
            else if (_phase == 2 && ButtonNamed("Replay0") != null)
            {
                var field = GameObject.Find("ReplayFolder").GetComponent<InputField>();
                Require(Path.GetFullPath(field.text) == _library, "Library shows another save folder");
                Stage("recording-row-created"); _phase = 7;
            }
            else if (_phase == 7 && Time.realtimeSinceStartup - _phaseAt > .2f)
            {
                Click("Replay0"); Stage("recording-open"); _phase = 3;
            }
            else if (_phase == 3)
            {
                _viewer = UnityEngine.Object.FindAnyObjectByType<LocalReplayPlayback>();
                if (_viewer == null || _viewer.Frame == null) return;
                Require(_viewer.Error == null, _viewer.Error);
                Require(UnityEngine.Object.FindAnyObjectByType<CharacterMotor>() == null &&
                    UnityEngine.Object.FindAnyObjectByType<SliceRunner>() == null, "Replay created live gameplay");
                Require(_viewer.Paused, "Replay did not open paused");
                GameObject.Find("Timeline").GetComponent<Slider>().value = 17;
                Stage("seek-17"); _phase = 4;
            }
            else if (_phase == 4 && Time.realtimeSinceStartup - _phaseAt > 1)
            {
                Require(Mathf.Abs(_viewer.Position - 17) < .05f, "Actual slider did not seek");
                if (_requireEffects) CheckRecordedEffects();
                Click("Rewind"); Require(Mathf.Abs(_viewer.Position - 12) < .05f, "Rewind did not move five seconds");
                Click("Forward"); Require(Mathf.Abs(_viewer.Position - 17) < .05f, "Forward did not restore time");
                _viewer.SetSpeed(2); Click("PlayPause");
                Require(!_viewer.Paused && _viewer.Speed == 2, "Playback/speed failed");
                _playingAt = _viewer.Position; Stage("playing-2x"); _phase = 5;
            }
            else if (_phase == 5 && Time.realtimeSinceStartup - _phaseAt > 1)
            {
                Require(_viewer.Position > _playingAt + .5f, "Playback time did not advance");
                Click("PlayPause"); _viewer.Follow(0); Require(_viewer.Paused && _viewer.FollowSeat == 0, "Pause/follow failed");
                Stage("paused-follow"); Click("CloseReplay"); _phase = 6;
            }
            else if (_phase == 6 && TumpHub.Current != null && TumpHub.Current.ShowingHome)
            { Require(!LocalReplayPlayback.Active, "Viewer survived return Home"); Stage("returned-home"); Finish(true, ""); }
        }
        private void Finish(bool passed, string error)
        {
            _finished = true; _receipt.passed = passed; _receipt.error = error;
            File.WriteAllText(Path.Combine(_folder, "replay-result.json"), JsonUtility.ToJson(_receipt, true));
            Application.Quit(passed ? 0 : 1);
        }
    }
}
