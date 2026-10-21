using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class PlayerHubOwnerDetailTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _originalAccount, _account;
        private CareerStore _originalCareer;
        private SocialStore _originalSocial;
        private GameObject _accountRoot, _hubRoot;
        private PlayerHub _hub;

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _originalAccount = GameServices.Account; _originalCareer = GameServices.Career; _originalSocial = GameServices.Social;
            _accountRoot = new GameObject("Dormant scorecard account"); _accountRoot.SetActive(false);
            _account = _accountRoot.AddComponent<PlayerAccount>();
            typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account,
                AccountRules.Normalise(new AccountProfile { PlayerId = "scorecard-owner-a", DisplayName = "OwnerA" }));
            Service("Account", _account); Service("Career", null); Service("Social", null);
            _hubRoot = new GameObject("Current owner record hub");
            _hub = _hubRoot.AddComponent<PlayerHub>(); _hub.Install(); _hub.OpenTab(PlayerHub.Door.Profile);
            var record = new MatchRecord
            {
                MatchId = "old-owner-scorecard", Mode = "Classic", MapId = SceneFlow.Eskinita,
                Rounds = 4, DurationSeconds = 360, PlayedUtc = "2026-10-02T00:00:00Z", WinningSlot = 0,
                DefenderByRound = new[] { 0, 1, 2, 3 }, Players = new PlayerMatchStats[4]
            };
            for (int i = 0; i < 4; i++) record.Players[i] = new PlayerMatchStats
            { Slot = i, PlayerId = i == 0 ? _account.PlayerId : "record-only-" + i, Handle = "Historical" + i, Placement = i + 1, Score = 400 - i * 50 };
            typeof(PlayerHub).GetField("_shown", Hidden).SetValue(_hub, new List<MatchRecord> { record });
            var tab = System.Enum.Parse(typeof(PlayerHub).GetNestedType("Tab", BindingFlags.NonPublic), "Matches");
            typeof(PlayerHub).GetMethod("Show", Hidden).Invoke(_hub, new[] { tab });
            typeof(PlayerHub).GetField("_page", Hidden).SetValue(_hub, 2);
            _hub.SendMessage("OnDataChanged");
            yield return null;
            Assert.IsTrue(_hub.IsOpen);
            Assert.AreEqual("scorecard-owner-a", _account.PlayerId);
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_hubRoot != null) Object.Destroy(_hubRoot);
            yield return null; // Let the installed hub detach from its controlled account.
            if (_accountRoot != null) Object.Destroy(_accountRoot);
            Service("Account", _originalAccount); Service("Career", _originalCareer); Service("Social", _originalSocial);
            yield return null;
            yield return PlayModeWorld.Reset();
        }

        private static void Service(string name, object value) => typeof(GameServices).GetProperty(name).SetValue(null, value);
        private T Read<T>(string name) => (T)typeof(PlayerHub).GetField(name, Hidden).GetValue(_hub);
        private GameObject OpenActualScorecard()
        {
            var button = Read<Canvas>("_canvas").GetComponentsInChildren<Button>(true)
                .Single(item => item.name == "OpenMatchDetail" && item.isActiveAndEnabled);
            button.onClick.Invoke();
            var detail = Read<GameObject>("_detail");
            Assert.IsNotNull(detail); Assert.IsTrue(detail.activeInHierarchy);
            Assert.IsTrue(detail.GetComponentsInChildren<Text>().Any(text => text.text.Contains("Historical0")),
                "The actual scorecard did not draw the old account's record.");
            return detail;
        }

        [Test]
        public void AnAccountChangeClosesThePreviousOwnersActualScorecard()
        {
            var detail = OpenActualScorecard(); int request = Read<int>("_historyRequest");
            _account.SignInAsGuest("SecondGuest"); // Public local-only transition raises the subscribed Changed event.
            Assert.AreNotEqual("scorecard-owner-a", _account.PlayerId);
            Assert.IsTrue(_hub.IsOpen);
            Assert.IsFalse(detail.activeSelf, "New account rows appeared behind the previous account's still-open scorecard.");
            Assert.AreEqual(0, Read<List<MatchRecord>>("_shown").Count);
            Assert.AreEqual(0, Read<int>("_page"));
            Assert.Greater(Read<int>("_historyRequest"), request);
        }
        [Test]
        public void SameAccountDataRefreshKeepsTheCurrentScorecardOpen()
        {
            var detail = OpenActualScorecard(); int request = Read<int>("_historyRequest");
            _hub.SendMessage("OnDataChanged");
            Assert.IsTrue(detail.activeInHierarchy);
            Assert.AreEqual(1, Read<List<MatchRecord>>("_shown").Count);
            Assert.AreEqual(2, Read<int>("_page"));
            Assert.AreEqual(request, Read<int>("_historyRequest"));
        }
        [Test]
        public void AccountChangeBeforeAnyScorecardWasOpenedKeepsTheHubUsable()
        {
            Assert.IsNull(Read<GameObject>("_detail"));
            Assert.DoesNotThrow(() => _account.SignInAsGuest("SecondGuest"));
            Assert.IsTrue(_hub.IsOpen); Assert.IsNull(Read<GameObject>("_detail"));
            Assert.AreEqual(0, Read<List<MatchRecord>>("_shown").Count);
            Assert.AreEqual(0, Read<int>("_page"));
        }
    }
}
