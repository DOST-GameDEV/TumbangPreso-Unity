using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class MatchmakingOwnerLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _previousAccount, _account;
        private CareerStore _previousCareer;
        private GameSettings _previousSettings;
        private GameObject _accountObject, _sessionObject;
        private NetSession _net;
        private Matchmaker _queue;
        private TaskCompletionSource<bool> _reply;
        private Task _operation;
        private int _joined;

        [SetUp]
        public void Before()
        {
            _previousAccount = GameServices.Account; _previousCareer = GameServices.Career;
            _previousSettings = SettingsStore.Current;
            Assert.IsTrue(Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-profile") >= 0);
            Assert.IsFalse(NetIdentity.IsOnline, "Queue ownership checks must never initialize SDK services.");
            SettingsStore.OverrideForTests(new GameSettings { AccountPlayerId = "queue-owner-a", PlayerName = "Owner" });
            _accountObject = new GameObject("Dormant queue owner"); _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account,
                new AccountProfile { PlayerId = "queue-owner-a", DisplayName = "Owner" });
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            typeof(GameServices).GetProperty("Career").SetValue(null, null);
            _sessionObject = new GameObject("Dormant owner-bound queue"); _sessionObject.SetActive(false);
            _net = _sessionObject.AddComponent<NetSession>();
            _queue = _sessionObject.AddComponent<Matchmaker>();
            typeof(Matchmaker).GetField("_net", Hidden).SetValue(_queue, _net);
            // Drive runtime subscription explicitly when the implementation has one.
            typeof(Matchmaker).GetMethod("OnEnable", Hidden)?.Invoke(_queue, null);
            _reply = new TaskCompletionSource<bool>(); _operation = null; _joined = 0;
            _queue.Joined += () => _joined++;
        }

        [TearDown]
        public async Task After()
        {
            _reply?.TrySetResult(false);
            if (_operation != null) await _operation;
            if (_queue != null)
            {
                _queue.Cancel();
                typeof(Matchmaker).GetMethod("OnDisable", Hidden)?.Invoke(_queue, null);
                typeof(Matchmaker).GetMethod("OnDestroy", Hidden).Invoke(_queue, null);
            }
            if (_sessionObject != null) Object.DestroyImmediate(_sessionObject);
            if (_accountObject != null) Object.DestroyImmediate(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            typeof(GameServices).GetProperty("Career").SetValue(null, _previousCareer);
            SettingsStore.OverrideForTests(_previousSettings);
        }

        private bool Busy => (bool)typeof(Matchmaker).GetField("_busy", Hidden).GetValue(_queue);
        private void Begin(bool join, QueueStake stake = QueueStake.Casual)
        {
            typeof(Matchmaker).GetProperty("Stake").SetValue(_queue, stake);
            Func<Task<bool>> start = () => _reply.Task;
            _operation = (Task)typeof(Matchmaker).GetMethod(join ? "JoinAsync" : "HostAsync", Hidden)
                .Invoke(_queue, join
                    ? new object[] { new ServerQuery.Entry { Id = "queue-room", RelayCode = "synthetic-relay" }, start }
                    : new object[] { start });
            Assert.IsFalse(_operation.IsCompleted); Assert.IsTrue(Busy);
            Assert.AreEqual(join ? QueueState.Joining : QueueState.Hosting, _queue.State);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task AGuestHandoverRetiresThePendingTicketEvenWhenThePrimaryReturns(bool join, bool returnPrimary)
        {
            Begin(join);
            _account.SignInAsGuest("OtherGuest");
            Assert.AreEqual(QueueState.Cancelled, _queue.State, "A new owner inherited the pending queue ticket.");
            Assert.IsFalse(Busy);
            if (returnPrimary) _account.LeaveGuest();
            _reply.SetResult(true); await _operation;
            Assert.AreEqual(QueueState.Cancelled, _queue.State, "An old connection reply revived the previous owner's queue.");
            Assert.IsFalse(Busy); Assert.AreEqual(0, _joined);
        }

        [Test]
        public async Task LosingSignedInEligibilityRetiresTheSameOwnersRankedTicket()
        {
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, true);
            Begin(false, QueueStake.Ranked);
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, false);
            var changed = (Action)typeof(PlayerAccount).GetField("Changed", Hidden | BindingFlags.Public).GetValue(_account);
            changed?.Invoke(); // The ordinary account notification after a service-session change.
            Assert.AreEqual(QueueState.Cancelled, _queue.State);
            _reply.SetResult(true); await _operation;
            Assert.AreEqual(QueueState.Cancelled, _queue.State); Assert.IsFalse(Busy);
        }

        [Test]
        public void AGuestCannotEnterRankedUsingThePrimarySessionsSignedInFlag()
        {
            _account.SignInAsGuest("OtherGuest");
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, true);
            var signed = (bool[])typeof(Matchmaker).GetMethod("PartySignedIn", Hidden).Invoke(_queue, null);
            Assert.AreEqual(PartyRefusal.MemberNotSignedIn, PartyRules.CanQueue(1, QueueStake.Ranked, new[] { 0 }, signed));
            Assert.AreEqual(PartyRefusal.None, PartyRules.CanQueue(1, QueueStake.Casual, new[] { 0 }, signed));
        }

        [Test]
        public async Task ACurrentOwnersProfileRefreshPreservesTheirPendingCasualTicket()
        {
            Begin(false);
            await _account.SetProfileAsync("Owner updated", "", "", ""); // Local-only public edit; no SDK dispatch.
            Assert.AreEqual(QueueState.Hosting, _queue.State); Assert.IsTrue(Busy);
            _reply.SetResult(true); await _operation;
            Assert.AreEqual(QueueState.Searching, _queue.State); Assert.IsFalse(Busy);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task TheCurrentOwnerStillCompletesTheirConnection(bool join)
        {
            Begin(join); _reply.SetResult(true); await _operation;
            Assert.AreEqual(join ? QueueState.Found : QueueState.Searching, _queue.State);
            Assert.AreEqual(join ? 1 : 0, _joined); Assert.IsFalse(Busy);
        }
    }
}
