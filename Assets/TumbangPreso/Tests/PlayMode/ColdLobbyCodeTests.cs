using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ColdLobbyCodeTests
    {
        [UnityTest]
        public IEnumerator ACodeEnteredBeforeTheFirstBeaconResolvesLocally()
        {
            yield return PlayModeWorld.Reset();
            var root = new GameObject("Cold code discovery");
            var beacon = root.AddComponent<LanBeacon>();
            var query = root.AddComponent<ServerQuery>();
            try
            {
                var pending = query.ResolveCodeAsync("Q7AZ");
                Assert.IsFalse(pending.IsCompleted, "Cold discovery must wait for the first local advertisement.");
                yield return new WaitForSecondsRealtime(.1f);
                using (var sender = new UdpClient())
                {
                    byte[] payload = Encoding.UTF8.GetBytes(LanBeacon.BuildPayload(
                        19099, 1, 4, false, "Q7AZ", "TEST HOST", 1, 1, 4, "other-peer-01"));
                    sender.Send(payload, payload.Length, new IPEndPoint(IPAddress.Loopback, LanBeacon.DiscoveryPort));
                }
                float until = Time.realtimeSinceStartup + 2;
                while (!pending.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(pending.IsCompleted && !pending.IsFaulted, "The bounded local lookup did not finish.");
                Assert.IsTrue(pending.Result.HasValue && pending.Result.Value.Found && pending.Result.Value.IsLan,
                    "A real LAN packet arriving after the JOIN press must prevent online fallback.");
                Assert.AreEqual("127.0.0.1", pending.Result.Value.Address);
                Assert.AreEqual(19099, pending.Result.Value.Port);
                var warm = query.ResolveCodeAsync(" q7az ");
                Assert.IsTrue(warm.IsCompleted && warm.Result.HasValue,
                    "An already discovered code must keep its immediate path.");
            }
            finally
            {
                beacon.StopAll();
                Object.Destroy(root);
            }
            yield return PlayModeWorld.Reset();
        }
    }
}
