using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class RecentPlayerActionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Dictionary<string, object> _previousServices = new Dictionary<string, object>();
        private CareerStore _previousCareer;
        private SocialStore _previousSocial;
        private GameObject _services, _ui;
        private PlayerAccount _account;
        private RectTransform _people;

        [SetUp]
        public void Before()
        {
            _previousServices.Clear();
            foreach (string name in new[] { "Account", "Social", "Career", "Stats" })
                _previousServices[name] = typeof(GameServices).GetProperty(name).GetValue(null);
            _previousCareer = CareerStore.Instance;
            _previousSocial = SocialStore.Instance;
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"),
                "Recent-player action checks require an isolated profile.");
            Assert.IsNull(GameServices.Round, "This lightweight fixture must not adopt or alter a live round.");
            _services = new GameObject("Dormant recent-player services");
            _services.SetActive(false);
            _account = _services.AddComponent<PlayerAccount>();
            SetOwner("recent-action-self");
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, false);
            typeof(PlayerAccount).GetProperty("IsGuest").SetValue(_account, false);
            var social = _services.AddComponent<SocialStore>();
            var career = _services.AddComponent<CareerStore>();
            var stats = _services.AddComponent<MatchStatsCollector>();
            SetService("Account", _account); SetService("Social", social);
            SetService("Career", career); SetService("Stats", stats);
            typeof(MatchStatsCollector).GetProperty("Last").SetValue(stats, new MatchRecord
            {
                MatchId = "recent-action-unsaved-fixture",
                Players = new[]
                {
                    new PlayerMatchStats { Slot = 0, PlayerId = "recent-action-self", Handle = "Self#TEST", IsBot = false },
                    new PlayerMatchStats { Slot = 1, PlayerId = "recent-action-other", Handle = "Other#TEST", IsBot = false }
                }
            });

            // EditMode keeps runtime Awake dormant. The UI itself is active so the
            // real native buttons, labels and visibility checks retain their semantics.
            _ui = new GameObject("Recent-player native action fixture", typeof(RectTransform));
            var result = _ui.AddComponent<MatchResult>();
            result.enabled = false;
            _people = new GameObject("RecentPeopleFixture", typeof(RectTransform)).GetComponent<RectTransform>();
            _people.SetParent(_ui.transform, false);
            typeof(MatchResult).GetField("_nativePeople", Private).SetValue(result, _people);
            Assert.AreEqual(0, _people.childCount);
            typeof(MatchResult).GetMethod("NativeRecentPlayers", Private).Invoke(result, null);
            Assert.IsNotNull(ActionButton("AddRecentPlayer"));
            Assert.IsNotNull(ActionButton("ReportRecentPlayer"));
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

        private void SetOwner(string id)
            => typeof(PlayerAccount).GetField("_profile", Private).SetValue(_account,
                new AccountProfile { PlayerId = id });

        private Button ActionButton(string name)
            => _people.GetComponentsInChildren<Button>(true).Single(button => button.name == name);

        private Text Feedback(string name)
            => _people.GetComponentsInChildren<Text>(true).Single(text => text.name == name);

        private static Task Complete(Button button, Text feedback, Func<Task<bool>> submit)
        {
            var method = typeof(MatchResult).GetMethod("CompleteRecentAction", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Controlled completion cases apply only to the final UI helper.");
            return (Task)method.Invoke(null, new object[]
            {
                button, feedback, submit, "SENDING...", "REQUEST SENT",
                (Func<string>)(() => "Controlled refusal. Try again.")
            });
        }

        // These are the only two cases selected against the original UI. They
        // invoke the actual native button callbacks, with offline service guards.
        [TestCase("AddRecentPlayer", "REQUEST SENT", "FriendRequestStatus")]
        [TestCase("ReportRecentPlayer", "REPORTED", "ReportRequestStatus")]
        public void OfflineRecentActionNeverClaimsDelivery(string buttonName, string success, string feedbackName)
        {
            var button = ActionButton(buttonName);
            var label = button.GetComponentInChildren<Text>();
            string original = label.text;
            button.onClick.Invoke();
            Assert.AreNotEqual(success, label.text, "An offline action claimed successful delivery.");
            Assert.IsTrue(button.interactable, "An undelivered action disabled retry.");
            Assert.AreEqual(original, label.text);
            var feedback = Feedback(feedbackName);
            Assert.IsTrue(feedback.gameObject.activeSelf);
            Assert.That(feedback.text.ToLowerInvariant(), Does.Contain("sign in"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task RecentActionWaitsForItsAcknowledgement(bool accepted)
        {
            var button = ActionButton("AddRecentPlayer");
            var label = button.GetComponentInChildren<Text>();
            var feedback = Feedback("FriendRequestStatus");
            string original = label.text;
            int calls = 0;
            var pending = new TaskCompletionSource<bool>();
            Func<Task<bool>> submit = () => { calls++; return pending.Task; };
            var completion = Complete(button, feedback, submit);
            Assert.IsFalse(completion.IsCompleted);
            Assert.IsFalse(button.interactable);
            Assert.AreEqual("SENDING...", label.text);
            Assert.IsFalse(feedback.gameObject.activeSelf);
            await Complete(button, feedback, submit);
            Assert.AreEqual(1, calls, "A second press dispatched a duplicate action while pending.");
            pending.SetResult(accepted);
            await completion;
            Assert.AreEqual(!accepted, button.interactable);
            Assert.AreEqual(accepted ? "REQUEST SENT" : original, label.text);
            Assert.AreEqual(!accepted, feedback.gameObject.activeSelf);
            if (!accepted) Assert.AreEqual("Controlled refusal. Try again.", feedback.text);
        }

        [Test]
        public async Task ChangedOwnerCannotAcceptALatePositiveCompletion()
        {
            var button = ActionButton("AddRecentPlayer");
            var label = button.GetComponentInChildren<Text>();
            var feedback = Feedback("FriendRequestStatus");
            string original = label.text;
            var pending = new TaskCompletionSource<bool>();
            var completion = Complete(button, feedback, () => pending.Task);
            SetOwner("recent-action-new-owner");
            pending.SetResult(true);
            await completion;
            Assert.AreEqual(original, label.text);
            Assert.IsTrue(button.interactable);
            Assert.IsTrue(feedback.gameObject.activeSelf);
            Assert.That(feedback.text, Does.Contain("Account changed"));
        }

        [Test]
        public async Task DestroyedButtonIgnoresALateCompletion()
        {
            var button = ActionButton("AddRecentPlayer");
            var feedback = Feedback("FriendRequestStatus");
            var pending = new TaskCompletionSource<bool>();
            var completion = Complete(button, feedback, () => pending.Task);
            Object.DestroyImmediate(button.gameObject);
            pending.SetResult(true);
            await completion;
            Assert.IsTrue(button == null);
            Assert.IsFalse(feedback.gameObject.activeSelf, "A retired button's completion repainted feedback.");
        }
    }
}
