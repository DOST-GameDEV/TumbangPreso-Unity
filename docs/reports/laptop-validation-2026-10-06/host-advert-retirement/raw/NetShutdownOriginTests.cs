using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Netcode;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class NetShutdownOriginTests
    {
        private ServerQuery _isolatedQuery;
        private bool _queryWasEnabled;
        private readonly Dictionary<string, object> _queryDispatches = new Dictionary<string, object>();
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown]
        public IEnumerator After()
        {
            var net = NetSession.Instance;
            net?.Stop();
            while (net != null && net.GetComponent<NetworkManager>().IsListening) yield return null;
            if (_isolatedQuery != null)
            {
                var deletion = _isolatedQuery.DeleteHostedLobbyAsync();
                while (!deletion.IsCompleted) yield return null;
                foreach (var dispatch in _queryDispatches)
                    typeof(ServerQuery).GetField(dispatch.Key, Hidden).SetValue(_isolatedQuery, dispatch.Value);
                _isolatedQuery.enabled = _queryWasEnabled;
            }
            _queryDispatches.Clear();
            _isolatedQuery = null;
            yield return PlayModeWorld.Reset();
        }

        [UnityTest]
        public IEnumerator RequestedStopIsDistinguishedFromUnexpectedManagerShutdown()
        {
            var net = NetSession.Ensure();
            var start = net.StartHostAsync(18762);
            while (!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result);
            LogAssert.Expect(UnityEngine.LogType.Log, new Regex(@"\[NetLifecycle\] requested-stop server=True relay=False origin=.*", RegexOptions.Singleline));
            LogAssert.Expect(UnityEngine.LogType.Log, "[NetLifecycle] server-stopped wasHost=True requestedStop=True");
            net.Stop();
            while (net.GetComponent<NetworkManager>().IsListening) yield return null;
            yield return null;
            Assert.IsFalse(net.Beacon.Advertising, "An intentionally closed host kept advertising on LAN.");

            start = net.StartHostAsync(18762);
            while (!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result);
            LogAssert.Expect(UnityEngine.LogType.Log, "[NetLifecycle] server-stopped wasHost=True requestedStop=False");
            net.GetComponent<NetworkManager>().Shutdown();
            while (net.GetComponent<NetworkManager>().IsListening) yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnexpectedHostStopRetiresItsLanBeacon()
        {
            var net = NetSession.Ensure();
            var start = net.StartHostAsync(18763);
            while (!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result);
            Assert.IsTrue(net.Beacon.Advertising, "The host did not advertise before the shutdown.");

            net.GetComponent<NetworkManager>().Shutdown();
            while (net.GetComponent<NetworkManager>().IsListening) yield return null;
            yield return null;

            Assert.IsFalse(net.Beacon.Advertising, "A stopped host is still advertised as joinable on LAN.");
            Assert.IsFalse(net.Beacon.Listening, "The stopped host retained its discovery socket.");
        }

        [UnityTest]
        public IEnumerator UnexpectedHostStopRetiresItsOnlinePublication()
        {
            var net = NetSession.Ensure();
            var start = net.StartHostAsync(18764);
            while (!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result);

            // Only service dispatch is isolated. Host shutdown uses the real NGO lifecycle.
            _isolatedQuery = net.Query;
            _queryWasEnabled = net.Query.enabled;
            foreach (string name in new[] { "_hostAuthDispatch", "_createHostedIdDispatch",
                         "_deleteHostedDispatch", "_updateHostedDispatch" })
                _queryDispatches[name] = typeof(ServerQuery).GetField(name, Hidden).GetValue(net.Query);
            net.Query.enabled = false; // The synthetic lobby must never send a real heartbeat.
            var deleted = new List<string>();
            const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ServerQuery).GetField("_hostAuthDispatch", hidden).SetValue(net.Query,
                (Func<Task<bool>>)(() => Task.FromResult(true)));
            typeof(ServerQuery).GetField("_createHostedIdDispatch", hidden).SetValue(net.Query,
                (Func<string, int, object, Task<string>>)((name, seats, options) => Task.FromResult("shutdown-test-lobby")));
            typeof(ServerQuery).GetField("_deleteHostedDispatch", hidden).SetValue(net.Query,
                (Func<string, Task>)(id => { deleted.Add(id); return Task.CompletedTask; }));
            typeof(ServerQuery).GetField("_updateHostedDispatch", hidden).SetValue(net.Query,
                (Func<string, object, Task>)((id, options) => Task.CompletedTask));
            var publication = net.Query.CreateHostedLobbyAsync("Shutdown test", "test", "isolated-relay", 1, 1,
                ServerQuery.HostedAdvert.None);
            while (!publication.IsCompleted) yield return null;
            Assert.AreEqual("shutdown-test-lobby", publication.Result);

            net.GetComponent<NetworkManager>().Shutdown();
            while (net.GetComponent<NetworkManager>().IsListening) yield return null;
            yield return null;

            CollectionAssert.AreEqual(new[] { "shutdown-test-lobby" }, deleted,
                "A stopped host left its online lobby alive and heartbeating.");
            Assert.IsNull(typeof(ServerQuery).GetField("_activeHostLobbyId", hidden).GetValue(net.Query));
        }
    }
}
