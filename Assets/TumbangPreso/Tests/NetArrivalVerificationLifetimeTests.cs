using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class NetArrivalVerificationLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string AccountId = "arrival-proof-account", Proof = "synthetic-proof";
        private GameObject _root;
        private NetSession _session;
        private TaskCompletionSource<(AccountRules.HandleCheck Check, string Handle)> _answer;
        private int _calls;
        private string OwnedHandle => AccountRules.Handle("Verified", AccountRules.DerivedTag(AccountId));

        [SetUp]
        public void Before()
        {
            Assert.IsTrue(Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-profile") >= 0);
            Assert.IsFalse(NetIdentity.IsOnline, "Arrival checks must never initialize a service session.");
            _root = new GameObject("Dormant arrival verification session"); _root.SetActive(false);
            _session = _root.AddComponent<NetSession>();
            // Dormant Awake keeps all transports, sockets and services uninitialized.
            typeof(NetSession).GetProperty("IsRelay").SetValue(_session, true);
            _answer = new TaskCompletionSource<(AccountRules.HandleCheck, string)>(); _calls = 0;
            typeof(NetSession).GetField("_verifyHandleDispatch", Hidden).SetValue(_session,
                (Func<string, string, Task<(AccountRules.HandleCheck, string)>>)((id, proof) =>
                {
                    Assert.AreEqual(AccountId, id); Assert.AreEqual(Proof, proof);
                    _calls++; return _answer.Task;
                }));
            Admit(1, "arrival-token", "Original#1111"); Approve(1, "arrival-token");
        }

        [TearDown]
        public async Task After()
        {
            _answer?.TrySetResult((AccountRules.HandleCheck.Unreachable, ""));
            await Task.Yield();
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private IDictionary Approvals => (IDictionary)typeof(NetSession).GetField("_helloByClient", Hidden).GetValue(_session);
        private IDictionary Answers => (IDictionary)typeof(NetSession).GetField("_handleChecks", Hidden).GetValue(_session);
        private PeerRecord Admit(int peer, string token, string name)
        {
            var record = _session.Lobby.Admit(peer, token, name);
            record.AccountPlayerId = AccountId;
            return record;
        }
        private void Approve(int peer, string token)
        {
            var type = typeof(NetSession).GetNestedType("ConnectionHello", BindingFlags.NonPublic);
            var hello = Activator.CreateInstance(type, true);
            type.GetField("Token").SetValue(hello, token);
            type.GetField("AccountPlayerId").SetValue(hello, AccountId);
            type.GetField("HandleProof").SetValue(hello, Proof);
            Approvals[(ulong)peer] = hello;
        }
        private void Begin(int peer = 1)
        {
            _session.VerifyArrival(peer, AccountId, Proof);
            Assert.AreEqual(1, _calls); Assert.AreEqual(0, Answers.Count);
        }
        private async Task Complete()
        {
            _answer.SetResult((AccountRules.HandleCheck.Owned, OwnedHandle));
            await Task.Yield(); await Task.Yield();
        }
        private void AssertUnverified(PeerRecord record)
        {
            Assert.AreEqual(AccountRules.HandleCheck.NotAsked, record.HandleTrust,
                "An obsolete verification upgraded a different arrival's trust.");
            Assert.AreEqual(0, Answers.Count, "An obsolete arrival populated the current verification cache.");
        }

        [Test]
        public async Task ReusedPeerAndAccountIdsDoNotAcceptThePreviousApprovalReply()
        {
            Begin();
            var replacement = Admit(1, "arrival-token", "Replacement#2222");
            Approve(1, "arrival-token");
            string originalName = replacement.Name;
            await Complete();
            AssertUnverified(replacement); Assert.AreEqual(originalName, replacement.Name);
        }

        [Test]
        public async Task ANewHostSessionDoesNotInheritThePreviousLocalPeerReply()
        {
            _session.Lobby.Reset(); Approvals.Clear();
            Admit(0, "host-token", "Host#1111"); Begin(0);
            _session.CancelPendingOperation(); // Actual stop/start operation lifetime boundary.
            _session.Lobby.Reset();
            var replacement = Admit(0, "host-token", "Host#2222");
            await Complete(); AssertUnverified(replacement);
        }

        [Test]
        public async Task ARelayReplyCannotUpgradeAReplacementLanArrival()
        {
            Begin();
            typeof(NetSession).GetProperty("IsRelay").SetValue(_session, false);
            var replacement = Admit(1, "arrival-token", "Lan#2222");
            await Complete(); AssertUnverified(replacement);
        }

        [Test]
        public async Task ADepartedPeerDoesNotPopulateTheCurrentVerificationCache()
        {
            Begin(); _session.Lobby.Depart(1); Approvals.Remove((ulong)1);
            await Complete();
            Assert.IsNull(_session.Lobby.PeerById(1));
            Assert.AreEqual(0, Answers.Count, "A departed peer's reply was cached after its lifetime ended.");
        }

        [Test]
        public async Task NormalReidentificationKeepsTheApprovedConnectionVerification()
        {
            var first = _session.Lobby.PeerById(1); Begin();
            var introduced = Admit(1, "arrival-token", "Introduction#2222");
            Assert.AreNotSame(first, introduced, "The control must reproduce Identify's ordinary fresh peer record.");
            await Complete();
            Assert.AreEqual(AccountRules.HandleCheck.Owned, introduced.HandleTrust);
            Assert.AreEqual(OwnedHandle, introduced.Name); Assert.AreEqual(1, Answers.Count);
        }

        [Test]
        public async Task TheCurrentArrivalStillAppliesAndReusesItsCompletedProofAnswer()
        {
            Begin(); await Complete();
            var peer = _session.Lobby.PeerById(1);
            Assert.AreEqual(AccountRules.HandleCheck.Owned, peer.HandleTrust);
            Assert.AreEqual(OwnedHandle, peer.Name);
            _session.VerifyArrival(1, AccountId, Proof);
            Assert.AreEqual(1, _calls, "A completed same-proof answer dispatched another verification.");
            Assert.AreEqual(1, Answers.Count);
        }
    }
}
