using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class RecentPlayerReportEligibilityTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Dictionary<string, object> _previousServices = new Dictionary<string, object>();
        private CareerStore _previousCareer;
        private SocialStore _previousSocial;
        private GameObject _services, _ui;
        private SocialStore _social;
        private MatchStatsCollector _stats;
        private MatchResult _result;
        private RectTransform _people;

        [SetUp]
        public void Before()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"));
            Assert.IsNull(GameServices.Round, "The row availability check must not load or alter a live world.");
            _previousServices.Clear();
            foreach (string name in new[] { "Account", "Social", "Career", "Stats" })
                _previousServices[name] = typeof(GameServices).GetProperty(name).GetValue(null);
            _previousCareer = CareerStore.Instance; _previousSocial = SocialStore.Instance;
            // Match the proven recent-action fixture: inactive services keep every Awake dormant.
            _services = new GameObject("Dormant report eligibility services"); _services.SetActive(false);
            var account = _services.AddComponent<PlayerAccount>();
            typeof(PlayerAccount).GetField("_profile", Private).SetValue(account, new AccountProfile { PlayerId = "recent-report-self" });
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(account, false);
            _social = _services.AddComponent<SocialStore>();
            var career = _services.AddComponent<CareerStore>();
            _stats = _services.AddComponent<MatchStatsCollector>();
            SetService("Account", account); SetService("Social", _social); SetService("Career", career); SetService("Stats", _stats);
            _ui = new GameObject("Report eligibility native row fixture", typeof(RectTransform));
            _result = _ui.AddComponent<MatchResult>(); _result.enabled = false;
            _people = new GameObject("EmptyRecentPeople", typeof(RectTransform)).GetComponent<RectTransform>();
            _people.SetParent(_ui.transform, false);
            typeof(MatchResult).GetField("_nativePeople", Private).SetValue(_result, _people);
        }

        [TearDown]
        public void After()
        {
            if (_ui != null) Object.DestroyImmediate(_ui);
            if (_services != null) Object.DestroyImmediate(_services);
            foreach (var previous in _previousServices) SetService(previous.Key, previous.Value);
            typeof(CareerStore).GetProperty("Instance").SetValue(null, _previousCareer);
            typeof(SocialStore).GetProperty("Instance").SetValue(null, _previousSocial);
        }

        private static void SetService(string name, object value)
            => typeof(GameServices).GetProperty(name).SetValue(null, value);

        [TestCase("friend", true, false)]
        [TestCase("blocked", true, false)]
        [TestCase("pending", true, false)]
        [TestCase("ordinary", true, true)]
        [TestCase("self", false, false)]
        [TestCase("bot", false, false)]
        [TestCase("empty", false, false)]
        [TestCase("no-social", true, false)]
        public void ReportsRemainAvailableIndependentlyOfFriendOffers(string relationship, bool expectedReport, bool expectedAdd)
        {
            string id = relationship == "self" ? "recent-report-self" : relationship == "empty" ? "" : "recent-report-other";
            if (relationship == "friend") _social.List.Friends.Add(new FriendRef { PlayerId = id });
            if (relationship == "blocked") _social.List.Blocked.Add(id);
            if (relationship == "pending") _social.List.Outgoing.Add(new FriendRef { PlayerId = id });
            if (relationship == "no-social") SetService("Social", null);
            typeof(MatchStatsCollector).GetProperty("Last").SetValue(_stats, new MatchRecord
            {
                MatchId = "recent-report-unsaved-fixture",
                Players = new[] { new PlayerMatchStats { Slot = 1, PlayerId = id, Handle = "Other#TEST", IsBot = relationship == "bot" } }
            });
            Assert.AreEqual(0, _people.childCount, "Populate the actual native row only once from an empty parent.");
            typeof(MatchResult).GetMethod("NativeRecentPlayers", Private).Invoke(_result, null);
            var buttons = _people.GetComponentsInChildren<Button>(true);
            var reports = buttons.Where(button => button.name == "ReportRecentPlayer").ToArray();
            var adds = buttons.Where(button => button.name == "AddRecentPlayer").ToArray();
            Assert.AreEqual(expectedReport ? 1 : 0, reports.Length, "A report action was coupled to friendship eligibility.");
            Assert.AreEqual(expectedAdd ? 1 : 0, adds.Length, "Friend eligibility changed while separating report availability.");
            foreach (var button in reports.Concat(adds))
            {
                Assert.IsTrue(button.interactable); Assert.IsTrue(button.gameObject.activeInHierarchy);
            }
        }
    }
}
