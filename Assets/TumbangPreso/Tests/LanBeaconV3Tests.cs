using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// ⚠️ BEACON v3, LAN-VPN-JOIN-1020. A host on another build was listed like any other and refused
    /// only after JOIN, and every LAN row's MAP read UNKNOWN. v3 carries the protocol and the map; v1
    /// and v2 still parse and stay joinable, and a host writes v3 and v2 so a peer on the same protocol
    /// from before v3 still lists it. These are the payload, the row it becomes, and the drain that
    /// merges a host's two copies and its two adapters.
    /// </summary>
    public sealed class LanBeaconV3Tests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Ermita = SceneFlow.IlalimNgTulay;
        private GameObject _root;
        private LanBeacon _beacon;
        private UdpClient _listener;

        [SetUp] public void Before()
        {
            _root = new GameObject("Owned LAN beacon v3 check");
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

        private static string V3(int protocol, string map, string name = "Kanto room", string id = "v3-host") =>
            LanBeacon.BuildPayloadV3(8910, 2, 4, false, "K7X9", name, 2, 3, 12, id, protocol, map);

        private static string V2(string name = "Kanto room", string id = "v3-host") =>
            LanBeacon.BuildPayload(8910, 2, 4, false, "K7X9", name, 2, 3, 12, id);

        private static LanEntry Parse(string payload, string address = "192.168.1.20")
        {
            Assert.IsTrue(LanBeacon.TryParsePayload(payload, address, out var entry), "Refused: " + payload);
            return entry;
        }

        /// <summary>Hand a parsed packet to the beacon exactly as the socket thread would, then drain it.</summary>
        private void Receive(string payload, string address)
        {
            var entry = Parse(payload, address);
            var type = typeof(LanBeacon).GetNestedType("ReceivedBeacon", BindingFlags.NonPublic);
            object received = Activator.CreateInstance(type);
            type.GetField("Listener").SetValue(received, _listener);
            type.GetField("Entry").SetValue(received, entry);
            object queue = typeof(LanBeacon).GetField("_inbox", Hidden).GetValue(_beacon);
            queue.GetType().GetMethod("Enqueue").Invoke(queue, new[] { received });
            typeof(LanBeacon).GetMethod("DrainInbox", Hidden).Invoke(_beacon, null);
        }

        /// <summary>Pin the beacon's idea of this machine's subnets, so a preference test means the same on every PC.</summary>
        private void Subnets(params string[] cidrs)
        {
            var list = new List<LanBeacon.LocalSubnet>();
            foreach (string cidr in cidrs)
            {
                string[] parts = cidr.Split('/');
                uint bits = uint.MaxValue << (32 - int.Parse(parts[1]));
                var mask = new IPAddress(new[] { (byte)(bits >> 24), (byte)(bits >> 16), (byte)(bits >> 8), (byte)bits });
                list.Add(new LanBeacon.LocalSubnet { Address = IPAddress.Parse(parts[0]), Mask = mask });
            }
            typeof(LanBeacon).GetField("_localSubnets", Hidden).SetValue(_beacon, list);
            typeof(LanBeacon).GetField("_localSubnetsRead", Hidden).SetValue(_beacon, float.PositiveInfinity);
        }

        [Test] public void V3RoundTripsTheProtocolTheMapAndEveryV2Field()
        {
            var entry = Parse(V3(NetSession.ProtocolVersion, Ermita, "Room | with a pipe"));
            Assert.AreEqual(8910, entry.Port);
            Assert.AreEqual("K7X9", entry.JoinCode);
            Assert.AreEqual(2, entry.Players);
            Assert.AreEqual(4, entry.MaxPlayers);
            Assert.AreEqual(2, entry.Occupied);
            Assert.AreEqual(3, entry.Connections);
            Assert.AreEqual(12, entry.MaxConnections);
            Assert.AreEqual("v3-host", entry.BeaconId);
            Assert.AreEqual(NetSession.ProtocolVersion, entry.Protocol);
            Assert.AreEqual(Ermita, entry.MapId);
            Assert.AreEqual("Room | with a pipe", entry.HostName, "The name is still the last, free-form field.");
            Assert.IsFalse(entry.DifferentVersion);
            Assert.IsTrue(entry.IsJoinable);
            StringAssert.StartsWith(LanBeacon.MagicV3 + "|", V3(1, Ermita));
        }

        [Test] public void V2AndV1StillParseSayNothingAndStayJoinable()
        {
            var v2 = Parse(V2());
            Assert.AreEqual("v3-host", v2.BeaconId);
            Assert.AreEqual(0, v2.Protocol, "A v2 host does not say its protocol.");
            Assert.AreEqual("", v2.MapId ?? "");
            Assert.AreEqual("Kanto room", v2.HostName);
            Assert.IsFalse(v2.DifferentVersion);
            Assert.IsTrue(v2.IsJoinable, "A v2 host is joinable exactly as before v3.");

            var v1 = Parse(LanBeacon.Magic + "|8910|1|4|0|K7X9|Old room");
            Assert.AreEqual(0, v1.Protocol);
            Assert.AreEqual("Old room", v1.HostName);
            Assert.IsTrue(v1.IsJoinable);
            Assert.AreEqual(HubRoom.UnknownMap, HubRoom.FromLan(v1).Map);
        }

        [Test] public void ADifferentProtocolIsListedButNeitherJoinableNorWatchable()
        {
            foreach (int other in new[] { NetSession.ProtocolVersion - 1, NetSession.ProtocolVersion + 1 })
            {
                var entry = Parse(V3(other, Ermita));
                Assert.IsTrue(entry.DifferentVersion, "protocol " + other);
                Assert.IsFalse(entry.IsJoinable);
                Assert.IsFalse(entry.CanSpectate);
                var room = HubRoom.FromLan(entry);
                Assert.IsTrue(room.DifferentVersion);
                Assert.IsFalse(room.IsJoinable);
                Assert.AreEqual(SceneFlow.PreviewFor(Ermita).Name, room.Map, "A v3 row names its map whatever its version.");
            }
        }

        [Test] public void AV3RowOnOurProtocolNamesItsMapAndJoins()
        {
            var room = HubRoom.FromLan(Parse(V3(NetSession.ProtocolVersion, SceneFlow.Kanto)));
            Assert.AreEqual(SceneFlow.PreviewFor(SceneFlow.Kanto).Name, room.Map);
            Assert.IsFalse(room.DifferentVersion);
            Assert.IsTrue(room.IsJoinable);
        }

        [Test] public void AMapThisBuildLacksReadsAsItsOwnIdNotAsTheFirstMap()
        {
            Assert.AreEqual("BAYANPLAZA", HubRoom.MapName("BayanPlaza"));
            Assert.AreEqual(HubRoom.UnknownMap, HubRoom.MapName(""));
            Assert.AreEqual(HubRoom.UnknownMap, HubRoom.MapName(null));
            foreach (var map in SceneFlow.MapRegistry) Assert.AreEqual(map.Name, HubRoom.MapName(map.Id));
        }

        [Test] public void TheMapIdCannotCarryTheSeparatorOrAPageOfText()
        {
            Assert.AreEqual("Badmapbb", LanBeacon.SanitiseMapId("Bad|map<b>b"));
            Assert.AreEqual(LanBeacon.MapIdMax, LanBeacon.SanitiseMapId(new string('a', 500)).Length);
            // Written through the sanitiser, a hostile map id cannot move the name.
            var entry = Parse(V3(NetSession.ProtocolVersion, "x|y|z", "Real name"));
            Assert.AreEqual("xyz", entry.MapId);
            Assert.AreEqual("Real name", entry.HostName);
            // And a hand-made packet's map is cut on the way in.
            entry = Parse($"{LanBeacon.MagicV3}|8910|1|4|0|K7X9|1|1|12|id|{NetSession.ProtocolVersion}|<color=red>Map</color>|Name");
            Assert.AreEqual("colorredMapcolor", entry.MapId);
        }

        [Test] public void AnUnreadableProtocolIsUnsaidAndATruncatedV3IsRefused()
        {
            var entry = Parse($"{LanBeacon.MagicV3}|8910|1|4|0|K7X9|1|1|12|id|banana|{Ermita}|Name");
            Assert.AreEqual(0, entry.Protocol);
            Assert.IsTrue(entry.IsJoinable, "An unreadable protocol behaves as a v2 row, not as a refusal.");
            Assert.IsFalse(LanBeacon.TryParsePayload($"{LanBeacon.MagicV3}|8910|1|4|0|K7X9|1|1|12|id|{NetSession.ProtocolVersion}|{Ermita}", "10.0.0.2", out _),
                "Twelve fields is not a v3 packet: it has no name field at all.");
        }

        [Test] public void AV2CopyAfterTheV3OneKeepsWhatV3Said()
        {
            Subnets("192.168.1.10/24");
            Receive(V3(NetSession.ProtocolVersion + 1, Ermita), "192.168.1.20");
            Receive(V2(), "192.168.1.20");
            var rows = _beacon.SortedEntries;
            Assert.AreEqual(1, rows.Count, "A host's two layouts are one room.");
            Assert.AreEqual(NetSession.ProtocolVersion + 1, rows[0].Protocol, "The v2 copy erased the protocol.");
            Assert.AreEqual(Ermita, rows[0].MapId, "The v2 copy erased the map.");
            Assert.IsTrue(rows[0].DifferentVersion);
            // And the next v3 copy still moves the map.
            Receive(V3(NetSession.ProtocolVersion + 1, SceneFlow.Kanto), "192.168.1.20");
            Assert.AreEqual(SceneFlow.Kanto, _beacon.SortedEntries[0].MapId);
        }

        [Test] public void ASharedLanBeatsHamachiAndASharedHamachiBeatsAnUnsharedLan()
        {
            var local = new List<LanBeacon.LocalSubnet>
            {
                new LanBeacon.LocalSubnet { Address = IPAddress.Parse("192.168.1.10"), Mask = IPAddress.Parse("255.255.255.0") },
                new LanBeacon.LocalSubnet { Address = IPAddress.Parse("25.16.130.116"), Mask = IPAddress.Parse("255.0.0.0") },
            };
            int sharedLan = LanBeacon.AddressPreference("192.168.1.20", local);
            int sharedHamachi = LanBeacon.AddressPreference("25.17.144.85", local);
            int otherLan = LanBeacon.AddressPreference("10.0.0.5", local);
            int otherHamachi = LanBeacon.AddressPreference("25.17.144.85", new List<LanBeacon.LocalSubnet>());
            int linkLocal = LanBeacon.AddressPreference("169.254.38.78", local);
            Assert.Greater(sharedLan, sharedHamachi);
            Assert.Greater(sharedHamachi, otherLan, "A VPN two players share beats an address neither can reach.");
            Assert.Greater(otherLan, otherHamachi);
            Assert.Greater(otherHamachi, linkLocal);
            Assert.AreEqual(0, LanBeacon.AddressPreference("127.0.0.1", local));
            Assert.AreEqual(0, LanBeacon.AddressPreference("not an address", local));
            Assert.Greater(sharedHamachi, LanBeacon.AddressPreference("26.1.2.3", local), "Radmin's 26/8 is a VPN range too.");
        }

        [Test] public void OneHostOnHamachiAndTheLanIsJoinedOverTheLan()
        {
            Subnets("192.168.1.10/24", "25.16.130.116/8");
            Receive(V3(NetSession.ProtocolVersion, Ermita), "25.17.144.85");
            Receive(V3(NetSession.ProtocolVersion, Ermita), "192.168.1.20");
            Receive(V2(), "25.17.144.85");
            Receive(V3(NetSession.ProtocolVersion, Ermita), "25.17.144.85");
            Assert.AreEqual(1, _beacon.SortedEntries.Count);
            Assert.AreEqual("192.168.1.20", _beacon.SortedEntries[0].Address);
            Assert.AreEqual("192.168.1.20:8910", HubRoom.FromLan(_beacon.SortedEntries[0]).Key);
        }

        [Test] public void ATieKeepsTheAddressAlreadyShownInsteadOfSwappingEveryPacket()
        {
            Subnets("192.168.1.10/24", "192.168.50.10/24");
            Receive(V3(NetSession.ProtocolVersion, Ermita), "192.168.1.20");
            for (int i = 0; i < 4; i++)
            {
                Receive(V3(NetSession.ProtocolVersion, Ermita), "192.168.50.20");
                Assert.AreEqual("192.168.1.20", _beacon.SortedEntries[0].Address, "packet " + i);
                Receive(V2(), "192.168.1.20");
            }
        }

        [Test] public void RefreshDropsEveryRowAndAsksAgain()
        {
            Receive(V3(NetSession.ProtocolVersion, Ermita), "192.168.1.20");
            Receive(V2("Other", "other-host"), "192.168.1.21");
            Assert.AreEqual(2, _beacon.SortedEntries.Count);
            int changes = 0;
            _beacon.EntriesChanged += () => changes++;
            _beacon.Refresh();
            Assert.IsEmpty(_beacon.SortedEntries);
            Assert.AreEqual(1, changes, "The list on screen was not told it emptied.");
            Assert.AreEqual(0f, (float)typeof(LanBeacon).GetField("_nextRequest", Hidden).GetValue(_beacon),
                "The next frame does not send the query.");
            Assert.Greater((float)typeof(LanBeacon).GetField("_browseUntil", Hidden).GetValue(_beacon), Time.unscaledTime);
        }
    }
}
