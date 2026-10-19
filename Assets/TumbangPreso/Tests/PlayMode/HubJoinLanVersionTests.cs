using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️ A HOST ON ANOTHER BUILD IS LISTED AS ONE AND CANNOT BE PRESSED (LAN-VPN-JOIN-1020, beacon v3).
    /// The real hub JOIN screen and the real `NetSession` beacon listening on the real discovery port; three
    /// hosts are real UDP senders on this machine, each sending what that build would send once a second:
    /// a host on another protocol (v3 and its v2 copy), a host on ours (v3 and v2), and a host from before
    /// v3 (v2 only). Then REFRESH empties the list, says it is looking, and the hosts come back.
    ///
    /// ⚠️ THE SENDERS ARE UNICAST TO 127.0.0.1, NOT BROADCAST, so a test run never advertises fake rooms to
    /// another machine's JOIN list on the same network.
    /// </summary>
    public sealed class HubJoinLanVersionTests
    {
        private string _settings, _pendingJoin;
        private bool _hubEnabled;
        private int _choice;
        private UdpClient _sender;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
            _pendingJoin = SceneFlow.PendingJoinCode;
            _hubEnabled = ConvertedMatchSetup.HubEnabled;
            _choice = HubHome.Choice;
            SceneFlow.PendingJoinCode = "";
            ConvertedMatchSetup.HubEnabled = true;
            HubHome.Choice = 1;
            _sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            _sender?.Close();
            // The online browser lives on `NetSession`, which outlives a test; leave it idle, not
            // holding this test's "unavailable" for the next fixture's JOIN screen.
            NetSession.Instance?.Query?.StopBrowsing();
            NetSession.Instance?.Stop();
            SceneFlow.Networked = false;
            SceneFlow.PendingJoinCode = _pendingJoin;
            ConvertedMatchSetup.HubEnabled = _hubEnabled;
            HubHome.Choice = _choice;
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            Settings.SettingsStore.Save();
            yield return PlayModeWorld.Reset();
        }

        /// <summary>One interval of all three hosts, in the order a real host sends its two layouts.</summary>
        private void Advertise()
        {
            int us = NetSession.ProtocolVersion;
            Send(LanBeacon.BuildPayloadV3(18910, 1, 4, false, "OTHR", "OTHER BUILD", 1, 1, 12, "otherbuild01", us + 1, SceneFlow.IlalimNgTulay));
            Send(LanBeacon.BuildPayload(18910, 1, 4, false, "OTHR", "OTHER BUILD", 1, 1, 12, "otherbuild01"));
            Send(LanBeacon.BuildPayloadV3(18920, 2, 4, false, "SAME", "SAME BUILD", 2, 2, 12, "samebuild001", us, SceneFlow.Kanto));
            Send(LanBeacon.BuildPayload(18920, 2, 4, false, "SAME", "SAME BUILD", 2, 2, 12, "samebuild001"));
            Send(LanBeacon.BuildPayload(18930, 1, 4, false, "OLDR", "OLDER BUILD", 1, 1, 12, "olderbuild01"));
        }

        private void Send(string payload)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            _sender.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, LanBeacon.DiscoveryPort));
        }

        private static RectTransform Row(TumpHub hub, string name)
        {
            var label = hub.Canvas.GetComponentsInChildren<Text>().FirstOrDefault(t => t.name == "Name" && t.text == name);
            return label == null ? null : (RectTransform)label.transform.parent;
        }

        private static string Text(RectTransform row, string child) => row.Find(child)?.GetComponent<Text>()?.text;

        /// <summary>Advertise once every half second until all three rows are drawn, or fail after <paramref name="seconds"/>.</summary>
        private IEnumerator AdvertiseUntilListed(TumpHub hub, float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds, next = 0;
            while (Time.realtimeSinceStartup < until)
            {
                if (Row(hub, "OTHER BUILD") != null && Row(hub, "SAME BUILD") != null && Row(hub, "OLDER BUILD") != null) yield break;
                if (Time.realtimeSinceStartup >= next) { next = Time.realtimeSinceStartup + .5f; Advertise(); }
                yield return null;
            }
            Assert.Fail("The three hosts were not all listed within " + seconds + " s. Rows: " + string.Join(", ",
                hub.Canvas.GetComponentsInChildren<Text>().Where(t => t.name == "Name").Select(t => t.text)));
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ADifferentProtocolHostIsListedAsDifferentVersionAndRefreshReasksTheNetwork()
        {
            yield return HubFlowTests.OpenHome();
            var hub = TumpHub.Current;
            NetSession.Instance.Query.StopBrowsing();
            var join = hub.Push<HubJoin>();
            yield return null;
            yield return HubFlowTests.Press("Source1");
            var beacon = NetSession.Instance.Beacon;
            Assert.IsTrue(beacon.Listening, "The LAN source did not start the real discovery listener.");

            yield return AdvertiseUntilListed(hub, 12);
            var rows = beacon.SortedEntries;
            var other = rows.First(e => e.HostName == "OTHER BUILD");
            Assert.AreEqual(NetSession.ProtocolVersion + 1, other.Protocol, "The v2 copy erased the v3 protocol on the live beacon.");
            Assert.IsTrue(other.DifferentVersion);
            Assert.Greater(rows.IndexOf(other), rows.FindIndex(e => e.HostName == "SAME BUILD"), "A row nobody can join sorted first.");

            var otherRow = Row(hub, "OTHER BUILD");
            Assert.AreEqual(HubJoin.DifferentVersionLabel, Text(otherRow, "Version"));
            Assert.IsEmpty(otherRow.GetComponentsInChildren<Selectable>(), "A different-version row offered a press.");
            Assert.AreEqual(SceneFlow.PreviewFor(SceneFlow.IlalimNgTulay).Name, Text(otherRow, "Map"));

            var same = Row(hub, "SAME BUILD");
            Assert.IsTrue(same.GetComponentsInChildren<Button>().Single().interactable);
            Assert.AreEqual(SceneFlow.PreviewFor(SceneFlow.Kanto).Name, Text(same, "Map"));
            var older = Row(hub, "OLDER BUILD");
            Assert.IsTrue(older.GetComponentsInChildren<Button>().Single().interactable, "A v2 host stopped being joinable.");
            Assert.AreEqual(HubRoom.UnknownMap, Text(older, "Map"));

            // The picture of the list, at 16:9, at the owner's short wide window and at 4:3, with every
            // control inside the frame and no text clipped (`TumpUiCapture`'s action-bounds check).
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1600, 680), new Vector2Int(1280, 960) })
            {
                Advertise();
                yield return TumpUiCapture.Capture("LanBeaconV3-v2-Join-lan-" + size.x + "x" + size.y, hub.Canvas,
                                                   size.x, size.y, false, checkActionBounds: true);
            }
            Assert.IsNotNull(Row(hub, "OTHER BUILD"), "A row left the list while it was photographed.");

            // REFRESH: the list empties at once and says it is looking, the button holds, and live hosts return.
            // ⚠️ ASSERTED IN THE PRESS'S OWN FRAME: the beacon drains in `Update`, so nothing (not even a real
            // host elsewhere on this network answering the new query) can have refilled it yet.
            var refresh = hub.Canvas.GetComponentsInChildren<HubButton>().First(b => b.name == "RefreshServers");
            Assert.IsTrue(refresh.IsInteractable(), "REFRESH was not ready on the LAN list.");
            refresh.onClick.Invoke();
            Assert.AreEqual("REFRESHING...", HubKit.LabelOf(refresh).text);
            Assert.IsFalse(refresh.interactable);
            Assert.IsEmpty(beacon.SortedEntries, "REFRESH kept the old rows.");
            var empty = hub.Canvas.GetComponentsInChildren<Text>().First(t => t.name == "EmptyState");
            Assert.AreEqual(HubJoin.LanRefreshingMessage, empty.text);
            yield return null;   // `Destroy` of the old rows lands at the end of the frame
            Assert.IsNull(Row(hub, "OTHER BUILD"), "The drawn list kept a row the beacon dropped.");
            yield return TumpUiCapture.Capture("LanBeaconV3-v2-Join-lan-refreshing-1600x680", hub.Canvas, 1600, 680, false, checkActionBounds: true);

            yield return AdvertiseUntilListed(hub, 6);
            Assert.IsNotNull(Row(hub, "OTHER BUILD").Find("Version"), "The re-listed host lost its tag.");
            float until = Time.realtimeSinceStartup + HubJoin.RefreshHoldSeconds + 2;
            while (Time.realtimeSinceStartup < until && !refresh.interactable) { Advertise(); yield return new WaitForSecondsRealtime(.25f); }
            Assert.IsTrue(refresh.interactable, "REFRESH never came back.");
            Assert.AreEqual("REFRESH", HubKit.LabelOf(refresh).text);
            Assert.AreNotEqual(Navigation.Mode.None, refresh.navigation.mode, "A pad cannot reach REFRESH.");

            // JOIN BY CODE has no list to refresh; the online list's REFRESH asks UGS on the next frame.
            yield return HubFlowTests.Press("Source2");
            Assert.IsFalse(refresh.gameObject.activeInHierarchy, "REFRESH stayed up over JOIN BY CODE.");
            yield return HubFlowTests.Press("Source0");
            Assert.IsTrue(refresh.gameObject.activeInHierarchy && refresh.interactable, "REFRESH was not ready on the online list.");
            var query = NetSession.Instance.Query;
            var sinceQuery = typeof(ServerQuery).GetField("_sinceQuery", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            sinceQuery.SetValue(query, 0f);
            refresh.onClick.Invoke();
            Assert.GreaterOrEqual((float)sinceQuery.GetValue(query), ServerQuery.QueryInterval, "The online REFRESH did not bring the query forward.");
            Assert.AreEqual("REFRESHING...", HubKit.LabelOf(refresh).text);
            refresh.onClick.Invoke();
            Assert.IsFalse(refresh.interactable, "A held or mashed REFRESH would query every frame.");

            hub.Back();
            Assert.IsNotInstanceOf<HubJoin>(hub.Top, "BACK did not leave JOIN GAME.");
        }
    }
}
