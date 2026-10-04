using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class SpectatorReplayMatchLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private MatchDirector _match, _previousMatch;
        private RoundDirector _previousRound;
        private SpectatorCamera _watcher;
        private Action _poll;
        private Action<string> _replay;
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private readonly Dictionary<FieldInfo, object> _highlightState = new Dictionary<FieldInfo, object>();
        private ArrayList _markers;
        private int _recorded, _deduplicated;

        [SetUp] public void Before()
        {
            _previousMatch = GameServices.Match; _previousRound = GameServices.Round;
            Assert.IsFalse(NetAuthority.IsNetworked); Assert.IsNull(SharedUltimatePhase.Instance);
            Assert.IsNull(HalftimePresentation.Instance, "The dormant fixture cannot retire an existing presentation");
            foreach (string name in new[] { "_matchStart", "_closeCallCount", "_closeCallWindowFrom" })
            {
                var field = typeof(MatchHighlights).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
                var value = field.GetValue(null); _highlightState[field] = value is Array array ? array.Clone() : value;
            }
            _markers = new ArrayList((ICollection)MatchHighlights.Log.Markers);
            _recorded = MatchHighlights.Log.Recorded; _deduplicated = MatchHighlights.Log.Deduplicated;
            _root = new GameObject("Dormant spectator match lifetime"); _root.SetActive(false);
            _match = _root.AddComponent<MatchDirector>(); _watcher = _root.AddComponent<SpectatorCamera>();
            typeof(GameServices).GetProperty("Match").SetValue(null, _match);
            typeof(GameServices).GetProperty("Round").SetValue(null, null);
            _poll = (Action)Delegate.CreateDelegate(typeof(Action), _watcher, typeof(SpectatorCamera).GetMethod("PollHighlights", Hidden));
            _replay = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), _watcher, typeof(SpectatorCamera).GetMethod("StartReplay", Hidden));
            _match.StartMatch(); _poll();
            Assert.AreEqual(1, _match.RoundNumber);
        }

        [TearDown] public void After()
        {
            if (_watcher != null)
            {
                typeof(SpectatorCamera).GetMethod("UnhookHighlights", Hidden).Invoke(_watcher, null);
                Ring.Clear(); Clip.Clear(); ((ICollection)Read("_texturePool")).GetType().GetMethod("Clear").Invoke(Read("_texturePool"), null);
            }
            Object.DestroyImmediate(_root);
            foreach (var texture in _textures) if (texture != null) Object.DestroyImmediate(texture);
            _textures.Clear();
            typeof(GameServices).GetProperty("Match").SetValue(null, _previousMatch);
            typeof(GameServices).GetProperty("Round").SetValue(null, _previousRound);
            foreach (var state in _highlightState)
                if (state.Value is Array array) Array.Copy(array, (Array)state.Key.GetValue(null), array.Length);
                else state.Key.SetValue(null, state.Value);
            _highlightState.Clear();
            if (_markers != null)
            {
                var markers = (IList)MatchHighlights.Log.Markers; markers.Clear(); foreach (var marker in _markers) markers.Add(marker);
                MatchHighlights.Log.GetType().GetProperty("Recorded").SetValue(MatchHighlights.Log, _recorded);
                MatchHighlights.Log.GetType().GetProperty("Deduplicated").SetValue(MatchHighlights.Log, _deduplicated);
            }
        }
        private object Read(string field) => typeof(SpectatorCamera).GetField(field, Hidden).GetValue(_watcher);
        private void Write(string field, object value) => typeof(SpectatorCamera).GetField(field, Hidden).SetValue(_watcher, value);
        private IList Ring => (IList)Read("_replayFrames");
        private IList Clip => (IList)Read("_replayClip");
        private object Frame(IList destination, bool pending = false)
        {
            var type = typeof(SpectatorCamera).GetNestedType("ReplayFrame", BindingFlags.NonPublic);
            var frame = Activator.CreateInstance(type, true);
            var texture = new Texture2D(2, 2, TextureFormat.RGB565, false); _textures.Add(texture);
            type.GetField("Image").SetValue(frame, texture); type.GetField("Pending").SetValue(frame, pending);
            type.GetField("CapturedAt").SetValue(frame, 10f + destination.Count * .1f);
            type.GetField("Aspect").SetValue(frame, 1f); type.GetField("Slot").SetValue(frame, -1);
            destination.Add(frame); return frame;
        }
        private void Restart()
        {
            long previous = _match.PresentationMatchId; _match.StartMatch();
            Assert.AreEqual(1, _match.RoundNumber); Assert.AreNotEqual(previous, _match.PresentationMatchId);
        }

        [Test] public void SameSceneRestartRetiresOldPlayingClipRingAndPendingMarker()
        {
            Frame(Ring); Frame(Clip); Write("_replaying", true);
            Write("_pendingMarkAt", 10f); Write("_pendingMarkReason", "OLD MATCH");
            Restart(); _poll();
            Assert.IsFalse(_watcher.Replaying, "Old-match footage is still covering the new live match");
            Assert.IsEmpty(Ring); Assert.IsEmpty(Clip); Assert.AreEqual(-1f, Read("_pendingMarkAt"));
            Assert.IsNull(Read("_pendingMarkReason"));
        }

        [Test] public void ReplayKeyCannotStartPreviousMatchFootageBeforeTheNextPoll()
        {
            for (int i = 0; i < 16; i++) Frame(Ring);
            Restart(); _replay("LAST PLAY");
            Assert.IsFalse(_watcher.Replaying, "The operator replay request selected the prior match's ready buffer");
            Assert.IsEmpty(Ring); Assert.IsEmpty(Clip);
        }

        [Test] public void NewMatchRetiresPendingFrameObjectsWithoutResettingReadbackAccounting()
        {
            var pending = Frame(Ring, true); Write("_outstandingReadbacks", 2);
            int generation = (int)Read("_captureGeneration"); Restart(); _poll();
            Assert.IsFalse(Ring.Contains(pending), "The old callback's existing membership guard would still accept this frame");
            Assert.IsNull(pending.GetType().GetField("Image").GetValue(pending));
            Assert.AreEqual(2, Read("_outstandingReadbacks")); Assert.AreEqual(generation, Read("_captureGeneration"));
        }

        [Test] public void FirstAcceptedNewMatchScoreRetiresOldBufferAndKeepsItsOwnMarker()
        {
            Frame(Ring); Restart(); _match.AddScore(1, Core.ScoreEvent.Tag);
            Assert.IsEmpty(Ring); Assert.AreEqual("TAG", Read("_pendingMarkReason"));
            Assert.AreEqual(1, Read("_pendingMarkSlot"));
        }

        [Test] public void OrdinaryRoundAdvancePreservesSameMatchFootageAndPlayingClip()
        {
            var frame = Frame(Ring); var clip = Frame(Clip); Write("_replaying", true);
            _match.AdvanceRound(); _poll();
            Assert.IsTrue(_watcher.Replaying); Assert.AreSame(frame, Ring[0]); Assert.AreSame(clip, Clip[0]);
        }

        [Test] public void CurrentMatchReadyFramesStillPlayOnTheOperatorRequest()
        {
            for (int i = 0; i < 16; i++) Frame(Ring);
            _replay("LAST PLAY"); Assert.IsTrue(_watcher.Replaying); Assert.GreaterOrEqual(Clip.Count, 4);
        }
    }
}
