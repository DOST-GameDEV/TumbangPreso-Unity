using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class ReadyCountdownAbandonTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private ReadyGate _gate;
        private readonly Dictionary<string, object> _abandonState = new Dictionary<string, object>();
        private readonly List<string> _ticks = new List<string>();
        private int _hidden, _began;

        [SetUp]
        public void Before()
        {
            Assert.IsTrue(Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-profile") >= 0);
            _abandonState.Clear();
            foreach (string name in new[] { "Cause", "RawReason", "RoundNumber", "TotalRounds", "AuthorityRevoked" })
                _abandonState[name] = typeof(MatchAbandon).GetProperty(name).GetValue(null);
            MatchAbandon.Forget();
            _root = new GameObject("Dormant interrupted ready countdown"); _root.SetActive(false);
            _gate = _root.AddComponent<ReadyGate>();
            _ticks.Clear(); _hidden = _began = 0;
            _gate.CountdownTick += tick => _ticks.Add(tick);
            _gate.CountdownHidden += () => _hidden++;
            _gate.RoundShouldBegin += () => _began++;
        }

        [TearDown]
        public void After()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            foreach (var state in _abandonState) typeof(MatchAbandon).GetProperty(state.Key).SetValue(null, state.Value);
        }

        private IEnumerator Countdown()
            => (IEnumerator)typeof(ReadyGate).GetMethod("RunReadyCountdown", Hidden).Invoke(_gate, null);

        [TestCase(1)]
        [TestCase(4)]
        public void AcceptedHostLossRetiresTheCountdownBeforeAnotherCueOrRoundStart(int completedYields)
        {
            var countdown = Countdown();
            for (int i = 0; i < completedYields; i++) Assert.IsTrue(countdown.MoveNext());
            int previousTicks = _ticks.Count;
            LogAssert.Expect(LogType.Warning, new Regex("^\\[Abandon\\]"));
            MatchAbandon.Note("Host left", wasLocal: false);
            Assert.IsTrue(MatchAbandon.AuthorityRevoked);
            // Resume the actual coroutine body while its old scene/object still exists.
            while (countdown.MoveNext()) { }
            Assert.AreEqual(0, _began, "The revoked countdown emitted the runner's new-match start event.");
            Assert.AreEqual(previousTicks, _ticks.Count, "The revoked countdown continued presenting new cues.");
            Assert.AreEqual(1, _hidden);
            Assert.IsFalse((bool)typeof(ReadyGate).GetField("_countingDown", Hidden).GetValue(_gate));
            Assert.IsFalse(_gate.AwaitingReady); Assert.IsFalse(_gate.AwaitingNetReady);
        }

        [Test]
        public void AnUninterruptedCountdownStillPresentsEveryCueAndStartsOnce()
        {
            var countdown = Countdown();
            while (countdown.MoveNext()) { }
            CollectionAssert.AreEqual(new[] { "3", "2", "1", "GO!" }, _ticks);
            Assert.AreEqual(1, _hidden); Assert.AreEqual(1, _began);
            Assert.IsFalse((bool)typeof(ReadyGate).GetField("_countingDown", Hidden).GetValue(_gate));
        }
    }
}
