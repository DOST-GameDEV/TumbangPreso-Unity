using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Tests
{
    public sealed class HubDisconnectCancellationTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root, _queueRoot;
        private ConvertedMatchSetup _controller;
        private LobbyJoinPanel _panel;
        private NetSession _previousNet;
        private bool _previousNetworked;
        private string _previousTitle, _previousMap, _previousDisconnect;
        private int _previousVisibility;
        private readonly Dictionary<string, object> _queueState = new Dictionary<string, object>();
        private TaskCompletionSource<bool> _completion;
        private Task<string> _pending;
        private CancellationToken _joinToken;

        [SetUp]
        public void Before()
        {
            Assert.IsNull(Matchmaker.Current, "This fixture must not share an active matchmaker.");
            _previousNet = NetSession.Instance;
            _previousDisconnect = NetSession.LastDisconnectReason;
            typeof(NetSession).GetProperty("Instance").SetValue(null, null);
            _previousNetworked = SceneFlow.Networked;
            _previousTitle = NetSession.RoomTitle; _previousMap = NetSession.RoomMap;
            _previousVisibility = NetSession.RoomVisibility;
            _queueState.Clear();
            foreach (string name in new[] { "QueueRoom", "BotsAccepted", "Found", "StartedAt", "Mode", "Stake" })
                _queueState[name] = typeof(HubQueueWatch).GetProperty(name).GetValue(null);
            HubQueueWatch.End();
            _root = new GameObject("Dormant hub disconnect", typeof(RectTransform));
            _root.SetActive(false);
            _controller = _root.AddComponent<ConvertedMatchSetup>();
            var hub = Child("Hub").AddComponent<TumpHub>();
            var canvas = Child("Canvas").AddComponent<Canvas>();
            typeof(TumpHub).GetProperty("Canvas").SetValue(hub, canvas);
            Field(hub, "_screens", (RectTransform)Child("Screens").transform);
            Field(hub, "_popups", (RectTransform)Child("Popups").transform);
            Field(hub, "_shade", Child("Shade").AddComponent<Image>());
            Field(hub, "_plate", Child("QueuePlate").AddComponent<HubQueuePlate>());
            var toastRoot = Child("Toast");
            var toast = toastRoot.AddComponent<HubToast>();
            Field(toast, "_root", (RectTransform)toastRoot.transform);
            Field(toast, "_words", Child("ToastWords").AddComponent<Text>());
            Field(hub, "_toast", toast);
            Field(_controller, "_hubView", hub);

            // Exercise the existing native join boundary without building a rendered hub
            // or starting a transport. A missing native canvas makes refresh a no-op.
            _panel = Child("JoinPanel").AddComponent<LobbyJoinPanel>();
            Field(_panel, "_nativeJoin", true);
            Field(_panel, "_entry", Child("JoinCode").AddComponent<InputField>());
            Field(_controller, "_joinPanel", _panel);
            _completion = new TaskCompletionSource<bool>();
            _pending = null; _joinToken = default;
            _panel.Connection = (_, token) => { _joinToken = token; return _completion.Task; };
        }

        [TearDown]
        public async Task After()
        {
            _completion?.TrySetResult(false);
            if (_pending != null) await _pending;
            if (_queueRoot != null) Object.DestroyImmediate(_queueRoot);
            if (_root != null) Object.DestroyImmediate(_root);
            foreach (var saved in _queueState) typeof(HubQueueWatch).GetProperty(saved.Key).SetValue(null, saved.Value);
            typeof(NetSession).GetProperty("Instance").SetValue(null, _previousNet);
            SceneFlow.Networked = _previousNetworked;
            NetSession.RoomTitle = _previousTitle; NetSession.RoomMap = _previousMap;
            NetSession.RoomVisibility = _previousVisibility;
            NetSession.LastDisconnectReason = _previousDisconnect;
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(_root.transform, false);
            return child;
        }

        private static void Field(object target, string name, object value)
            => target.GetType().GetField(name, Hidden).SetValue(target, value);

        private void Disconnect()
            => Assert.IsTrue((bool)typeof(ConvertedMatchSetup).GetMethod("HubDisconnected", Hidden)
                .Invoke(_controller, new object[] { "The room closed." }));

        [Test]
        public void NewHomeConsumesTheArenaReasonOnceIntoAnAcknowledgedNotice()
        {
            NetSession.LastDisconnectReason = "The host connection closed. This match has stopped.";
            var present = typeof(ConvertedMatchSetup).GetMethod("PresentPendingDisconnect", Hidden);
            present.Invoke(_controller, null);
            present.Invoke(_controller, null);
            var notices = _root.GetComponentsInChildren<HubDisconnectedNotice>(true);
            Assert.AreEqual(1, notices.Length);
            Assert.IsEmpty(NetSession.LastDisconnectReason);
            Assert.AreEqual("The host connection closed. This match has stopped.", notices[0].Message);
            Assert.IsNotNull(notices[0].FirstFocus, "A persistent notice needs an acknowledgment control.");
            var text = System.Array.Find(notices[0].GetComponentsInChildren<Text>(true), t => t.name == "DisconnectReason");
            Assert.IsFalse(text.supportRichText);
        }

        [Test]
        public void OfflineFallbackHostDoesNotIgnoreTheActualDisconnect()
        {
            NetSession.LastDisconnectReason = "The host connection closed.";
            typeof(ConvertedMatchSetup).GetMethod("HandleClientDisconnected", Hidden)
                .Invoke(_controller, new object[] { "The host connection closed." });
            Assert.AreEqual(1, _root.GetComponentsInChildren<HubDisconnectedNotice>(true).Length);
            Assert.IsEmpty(NetSession.LastDisconnectReason);
            Assert.IsFalse(SceneFlow.Networked);
        }

        [Test]
        public void HostLossCancelsThePendingNativeJoin()
        {
            _pending = _controller.Join("ABCD");
            Assert.IsFalse(_pending.IsCompleted);
            Assert.IsTrue(_joinToken.CanBeCanceled);
            Disconnect();
            Assert.IsTrue(_joinToken.IsCancellationRequested,
                "Returning HOME left the native join operation alive.");
            Assert.IsFalse(SceneFlow.Networked);
        }

        [Test]
        public async Task LateJoinFailureCannotOverwriteTheNextRoute()
        {
            _pending = _controller.Join("ABCD");
            Disconnect();
            // A new online route was chosen while the old transport callback was delayed.
            SceneFlow.Networked = true;
            _completion.SetResult(false);
            string result = await _pending;
            Assert.IsTrue(SceneFlow.Networked,
                "The retired join changed the network state of the next route.");
            Assert.AreEqual("Room request cancelled.", result);
        }

        [Test]
        public void HostLossCancelsAnActiveQueueBeforeClearingItsPlate()
        {
            _queueRoot = new GameObject("Controlled queue");
            var queue = _queueRoot.AddComponent<Matchmaker>(); queue.enabled = false;
            typeof(Matchmaker).GetProperty("State").SetValue(queue, QueueState.Joining);
            HubQueueWatch.Begin(GameMode.HeroStrike, QueueStake.Casual);
            Assert.AreSame(queue, Matchmaker.Current);
            Disconnect();
            Assert.AreEqual(QueueState.Cancelled, queue.State,
                "The queue kept joining after its UI and transport were retired.");
            Assert.IsFalse(HubQueueWatch.QueueRoom);
        }
    }
}
