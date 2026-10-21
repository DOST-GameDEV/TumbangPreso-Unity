using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class HostFailureRecoveryTests
    {
        private readonly Dictionary<string, object> _savedAbandon = new Dictionary<string, object>();
        private NetSession _net;
        private Action<string> _onDisconnected;
        private int _recoveryEvents;
        private bool _revokedAtEvent, _resolvingAtEvent;
        private string _reason;

        [UnitySetUp]
        public IEnumerator Before()
        {
            foreach (string name in new[] { "Cause", "RawReason", "RoundNumber", "TotalRounds",
                         "AuthorityRevoked", "MatchWasCompleted" })
                _savedAbandon[name] = typeof(MatchAbandon).GetProperty(name).GetValue(null);
            yield return PlayModeWorld.Reset();
            _net = NetSession.Ensure();
            _net.Stop();
            while (_net.GetComponent<NetworkManager>().IsListening) yield return null;

            // Exercise the real recovery event/latch without loading another UI scene.
            // MatchRpc leaves an already-open MatchSetup scene to its own controller.
            var lobbyContext = SceneManager.CreateScene(SceneFlow.MatchSetup);
            SceneManager.SetActiveScene(lobbyContext);
            MatchAbandon.Forget();
            _recoveryEvents = 0;
            _revokedAtEvent = false;
            _resolvingAtEvent = true;
            _reason = null;
            _onDisconnected = reason =>
            {
                _recoveryEvents++;
                _revokedAtEvent = MatchAbandon.AuthorityRevoked;
                _resolvingAtEvent = NetAuthority.ShouldResolve();
                _reason = reason;
            };
            NetSession.ClientDisconnected += _onDisconnected;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_onDisconnected != null) NetSession.ClientDisconnected -= _onDisconnected;
            _net?.Stop();
            while (_net != null && _net.GetComponent<NetworkManager>().IsListening) yield return null;
            yield return PlayModeWorld.Reset();
            foreach (var state in _savedAbandon)
                typeof(MatchAbandon).GetProperty(state.Key).SetValue(null, state.Value);
            _savedAbandon.Clear();
            _onDisconnected = null;
        }

        [UnityTest]
        public IEnumerator UnexpectedListenHostStopRevokesAuthorityBeforeRecovery()
        {
            var start = _net.StartHostAsync(18765);
            while (!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result);
            Assert.IsTrue(_net.IsNetworked);
            Assert.IsTrue(_net.IsHost);
            Assert.IsTrue(NetAuthority.ShouldResolve());

            _net.GetComponent<NetworkManager>().Shutdown();
            while (_net.GetComponent<NetworkManager>().IsListening) yield return null;
            yield return null;

            Assert.AreEqual(1, _recoveryEvents, "The established listen host skipped session recovery.");
            Assert.IsTrue(_revokedAtEvent, "Recovery was raised before abandoned-match authority was revoked.");
            Assert.IsFalse(_resolvingAtEvent, "The dropped host could still resolve its abandoned match.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(_reason), "Recovery provided no actionable reason.");
        }

        [UnityTest]
        public IEnumerator RequestedHostStopLeavesNavigationAndOfflineAuthorityAlone()
        {
            var start = _net.StartHostAsync(18766);
            while (!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result);

            _net.Stop();
            while (_net.GetComponent<NetworkManager>().IsListening) yield return null;
            yield return null;

            Assert.AreEqual(0, _recoveryEvents, "Normal Quit was overridden by session recovery.");
            Assert.IsFalse(MatchAbandon.AuthorityRevoked);
            Assert.IsTrue(NetAuthority.ShouldResolve(), "Normal Quit disabled the next offline game.");
        }
    }
}
