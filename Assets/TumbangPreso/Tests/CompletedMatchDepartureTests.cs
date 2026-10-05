using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.Tests
{
    public sealed class CompletedMatchDepartureTests
    {
        private readonly Dictionary<PropertyInfo, object> _departure = new();
        private GameObject _root;
        private MatchDirector _match, _previousMatch;
        private CustomRules _rules;
        private bool _pinned;

        [SetUp]
        public void Before()
        {
            _previousMatch = GameServices.Match;
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            foreach (var property in typeof(MatchAbandon).GetProperties(BindingFlags.Public | BindingFlags.Static))
                if (property.GetSetMethod(true) != null) _departure[property] = property.GetValue(null);
            MatchAbandon.Forget();
            _root = new GameObject("Dormant completed peer match"); _root.SetActive(false);
            _match = _root.AddComponent<MatchDirector>();
            typeof(GameServices).GetProperty("Match").SetValue(null, _match);
            Rules(4);
        }

        [TearDown]
        public void After()
        {
            typeof(GameServices).GetProperty("Match").SetValue(null, _previousMatch);
            Object.DestroyImmediate(_root);
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            foreach (var entry in _departure) entry.Key.SetValue(null, entry.Value);
            _departure.Clear();
        }

        private static void Rules(int rounds)
        {
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike); rules.Rounds = rounds;
            SceneFlow.PinSelectedRules(rules);
        }

        private void Finish(bool host)
        {
            _match.ApplySnapshot(new[] { 20, 110, 1725, 1700 }, 4, true);
            if (host) _match.BeginIntermission();
            else _match.ApplySnapshot(new[] { 20, 110, 1725, 1700 }, 4, false);
            Assert.IsFalse(_match.MatchInProgress);
        }

        private static void LoseHost()
        {
            LogAssert.Expect(LogType.Warning, new Regex("^\\[Abandon\\] HostLost:"));
            MatchAbandon.Note("Host left", wasLocal: false);
            Assert.IsTrue(MatchAbandon.AuthorityRevoked, "A stopped client must never become a referee.");
        }

        [TestCase(false)] [TestCase(true)]
        public void CompletedMatchIsRoomClosureEvenAfterLocalRulesReturn(bool host)
        {
            Finish(host);
            Rules(8); // The disconnected lobby no longer owns the host's four-round selection.
            Assert.AreEqual(8, _match.TotalRounds);
            LoseHost();
            StringAssert.DoesNotContain("ABANDONED", MatchAbandon.Diagnostic);
            Assert.AreEqual(4, MatchAbandon.TotalRounds, "Departure lost the completed match's rule count.");
            StringAssert.DoesNotContain("cannot continue", MatchAbandon.PlayerLine);
            var line = (string)typeof(NetSession).GetMethod("PlayerFacingDisconnectReason",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { "Host left" });
            StringAssert.DoesNotContain("cannot continue", line, "Actual disconnect UI still claims a finished match failed.");
        }

        [Test]
        public void FreshLiveMatchAfterCompletionStillReportsRealHostLoss()
        {
            Finish(false); _match.ResetForNewMatch(); Rules(8);
            _match.ApplySnapshot(new[] { 0, 0, 0, 0 }, 2, true);
            LoseHost();
            StringAssert.Contains("ABANDONED", MatchAbandon.Diagnostic);
            StringAssert.Contains("cannot continue", MatchAbandon.PlayerLine);
            Assert.AreEqual(8, MatchAbandon.TotalRounds);
        }

        [Test]
        public void APreStartLobbySnapshotIsNotACompletedMatch()
        {
            _match.ApplySnapshot(new[] { 0, 0, 0, 0 }, 1, false);
            LoseHost();
            StringAssert.Contains("ABANDONED", MatchAbandon.Diagnostic);
            StringAssert.Contains("cannot continue", MatchAbandon.PlayerLine);
        }
    }
}
