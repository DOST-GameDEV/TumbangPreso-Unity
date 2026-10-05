using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class RelayJoinTimeoutTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private NetSession _session;
        private JoinAttemptGate _gate;
        private JoinAllocation _allocation;
        private int _calls;
        [SetUp] public void Before()
        {
            _calls = 0;
            Assert.IsFalse(NetIdentity.IsOnline, "This native fixture must not initialize live services.");
            _root = new GameObject("Dormant Relay join timeout fixture"); _root.SetActive(false);
            _session = _root.AddComponent<NetSession>();
            _gate = (JoinAttemptGate)typeof(NetSession).GetField("_joinAttempts", Hidden).GetValue(_session);
            _allocation = new JoinAllocation(Guid.NewGuid(), new List<RelayServerEndpoint>(), null,
                new byte[0], new byte[0], new byte[0], "synthetic", new byte[0]);
        }
        [TearDown] public void After() => UnityEngine.Object.DestroyImmediate(_root);
        private void Dispatch(Func<Task<JoinAllocation>> callback)
            => typeof(NetSession).GetField("_relayJoinDispatch", Hidden).SetValue(_session,
                (Func<string, Task<JoinAllocation>>)(code => { Assert.AreEqual("TESTCODE", code); _calls++; return callback(); }));
        private Task<JoinAllocation> Join(JoinAttemptGate.Attempt attempt)
            => (Task<JoinAllocation>)typeof(NetSession).GetMethod("RequestRelayJoinAsync", Hidden)
                .Invoke(_session, new object[] { "TESTCODE", attempt });
        [TestCase(RelayExceptionReason.NetworkError)]
        [TestCase(RelayExceptionReason.RequestTimeOut)]
        public async Task TransientTimeoutThenSuccessKeepsTheCurrentAttempt(RelayExceptionReason reason)
        {
            Dispatch(() => _calls == 1
                ? Task.FromException<JoinAllocation>(new RelayServiceException(reason, "Request timeout"))
                : Task.FromResult(_allocation));
            var attempt = _gate.Begin();
            Assert.AreSame(_allocation, await Join(attempt));
            Assert.AreEqual(2, _calls); Assert.IsTrue(attempt.CanContinue);
        }
        [Test] public async Task AlreadySuccessfulRequestDoesNotRepeat()
        {
            Dispatch(() => Task.FromResult(_allocation));
            Assert.AreSame(_allocation, await Join(_gate.Begin())); Assert.AreEqual(1, _calls);
        }
        [Test] public void ExpiredCodeIsNotRetriedOrReclassifiedAsTimeout()
        {
            Dispatch(() => Task.FromException<JoinAllocation>(new RelayServiceException(RelayExceptionReason.JoinCodeNotFound, "Expired code")));
            var error = Assert.ThrowsAsync<RelayServiceException>(async () => await Join(_gate.Begin()));
            Assert.AreEqual(RelayExceptionReason.JoinCodeNotFound, error.Reason); Assert.AreEqual(1, _calls);
        }
        [Test] public async Task RepeatedTimeoutStopsAfterOneRetry()
        {
            Dispatch(() => Task.FromException<JoinAllocation>(new RelayServiceException(RelayExceptionReason.NetworkError, "Request timeout")));
            try
            {
                await Join(_gate.Begin());
                Assert.Fail("A repeated timeout must remain a failure after the retry.");
            }
            catch (RelayServiceException error)
            {
                Assert.AreEqual(RelayExceptionReason.NetworkError, error.Reason);
            }
            Assert.AreEqual(2, _calls);
        }
        [Test] public async Task ReplacedAttemptCannotRetryOrInvalidateItsSuccessor()
        {
            var pending = new TaskCompletionSource<JoinAllocation>();
            Dispatch(() => pending.Task);
            var old = Join(_gate.Begin());
            var current = _gate.Begin();
            pending.SetException(new RelayServiceException(RelayExceptionReason.NetworkError, "Request timeout"));
            Assert.IsNull(await old); Assert.AreEqual(1, _calls); Assert.IsTrue(current.CanContinue);
        }
        [Test] public async Task CancelledAttemptCannotRetry()
        {
            using var cancellation = new System.Threading.CancellationTokenSource();
            var pending = new TaskCompletionSource<JoinAllocation>(); Dispatch(() => pending.Task);
            var task = Join(_gate.Begin(cancellation.Token)); cancellation.Cancel();
            pending.SetException(new RelayServiceException(RelayExceptionReason.NetworkError, "Request timeout"));
            Assert.IsNull(await task); Assert.AreEqual(1, _calls);
        }
    }
}
