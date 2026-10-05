using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class LanBrowserIdentityTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private LanBeacon _beacon;
        private UdpClient _listener;

        [SetUp] public void Before()
        {
            _root = new GameObject("Owned LAN browser identity check");
            _beacon = _root.AddComponent<LanBeacon>();
            _listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            typeof(LanBeacon).GetField("_listener", Hidden).SetValue(_beacon, _listener);
            typeof(LanBeacon).GetProperty("Listening").SetValue(_beacon, true);
        }
        [TearDown] public void After()
        {
            _beacon.StopAll();
            _listener.Close();
            UnityEngine.Object.DestroyImmediate(_root);
        }
        private void Receive(string address, string identity, string name)
        {
            string payload = LanBeacon.BuildPayload(8910, 1, 4, false, "ROOM", name, 1, 1, 12, identity);
            Assert.IsTrue(LanBeacon.TryParsePayload(payload, address, out var entry));
            var type = typeof(LanBeacon).GetNestedType("ReceivedBeacon", BindingFlags.NonPublic);
            object received = Activator.CreateInstance(type);
            type.GetField("Listener").SetValue(received, _listener);
            type.GetField("Entry").SetValue(received, entry);
            object queue = typeof(LanBeacon).GetField("_inbox", Hidden).GetValue(_beacon);
            queue.GetType().GetMethod("Enqueue").Invoke(queue, new[] { received });
            typeof(LanBeacon).GetMethod("DrainInbox", Hidden).Invoke(_beacon, null);
        }

        [Test] public void OneHostAcrossAdaptersHasOneUsableCurrentRow()
        {
            Receive("169.254.38.78", "same-host", "Initial room");
            Receive("192.168.1.7", "same-host", "Current room");
            Receive("169.254.38.78", "same-host", "Current room");
            Assert.AreEqual(1, _beacon.SortedEntries.Count, "One process appeared twice through different adapters.");
            Assert.AreEqual("192.168.1.7", _beacon.SortedEntries[0].Address);
            Assert.AreEqual("Current room", _beacon.SortedEntries[0].HostName);
        }

        [Test] public void SeparateProcessesWithTheSameRoomNameStaySeparate()
        {
            Receive("192.168.1.7", "first-host", "Same name");
            Receive("192.168.1.8", "second-host", "Same name");
            Assert.AreEqual(2, _beacon.SortedEntries.Count);
        }

        [Test] public void OlderBeaconsWithoutIdentityKeepDistinctEndpoints()
        {
            Receive("192.168.1.7", "", "Older room");
            Receive("192.168.1.8", "", "Older room");
            Assert.AreEqual(2, _beacon.SortedEntries.Count);
        }

        [Test] public void HostHintPrefersUsableIpv4OverLinkLocalAdapters()
        {
            var addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip =>
                ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)).ToArray();
            var usable = addresses.Where(ip => !IsLinkLocal(ip)).ToArray();
            string actual = (string)typeof(ConvertedMatchSetup).GetMethod("HostAddress",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            var expected = usable.Length > 0 ? usable : addresses;
            Assert.Contains(actual, expected.Length > 0
                ? expected.Select(ip => ip + ":" + LobbySession.DefaultPort).ToArray()
                : new[] { "127.0.0.1:" + LobbySession.DefaultPort });
        }
        private static bool IsLinkLocal(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();
            return bytes[0] == 169 && bytes[1] == 254;
        }
    }
}
