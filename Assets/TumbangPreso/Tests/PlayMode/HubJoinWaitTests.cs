using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️ A JOIN NOBODY ANSWERS MUST LOOK LIKE WAITING AND THEN END (2026-10-20, a LAN row over Hamachi:
    /// "it's stuck on joining"). The real hub JOIN screen, the real transport, an address with nothing
    /// listening: the line counts the seconds, names the firewall after `NoAnswerHintSeconds`, and ends
    /// on "No answer" with the screen usable again.
    /// </summary>
    public sealed class HubJoinWaitTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string _settings, _pendingJoin;
        private bool _hubEnabled;
        private int _choice;

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
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            NetSession.Instance?.Stop();
            SceneFlow.Networked = false;
            SceneFlow.PendingJoinCode = _pendingJoin;
            ConvertedMatchSetup.HubEnabled = _hubEnabled;
            HubHome.Choice = _choice;
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            Settings.SettingsStore.Save();
            yield return PlayModeWorld.Reset();
        }

        private static int UnusedPort()
        {
            using var socket = new System.Net.Sockets.UdpClient(0);
            return ((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AnUnansweredAddressCountsItsWaitNamesTheFirewallAndEnds()
        {
            yield return HubFlowTests.OpenHome();
            var hub = TumpHub.Current;
            var net = NetSession.Instance;
            net.Query.StopBrowsing();
            var join = hub.Push<HubJoin>();
            yield return null;
            var status = (Text)typeof(HubJoin).GetField("_status", Private).GetValue(join);
            var busy = typeof(HubJoin).GetField("_busy", Private);
            string target = "127.0.0.1:" + UnusedPort();

            typeof(HubJoin).GetMethod("Join", Private).Invoke(join, new object[] { target });
            yield return null;
            if ((bool)busy.GetValue(join)) LogAssert.Expect(LogType.Error, "Failed to connect to server.");

            float started = Time.realtimeSinceStartup;
            while ((bool)busy.GetValue(join) && Time.realtimeSinceStartup < started + 1.5f) yield return null;
            Assert.IsTrue((bool)busy.GetValue(join), "An unanswered join ended within a second and a half.");
            StringAssert.StartsWith("Joining " + target + "... ", status.text);
            StringAssert.EndsWith(" s", status.text, "The wait does not count its seconds.");

            while ((bool)busy.GetValue(join) && Time.realtimeSinceStartup < started + JoinFailureRules.NoAnswerHintSeconds + .8f)
                yield return null;
            Assert.IsTrue((bool)busy.GetValue(join), "The transport gave up before the hint could show.");
            StringAssert.Contains("firewall", status.text, "A long unanswered wait does not name the usual cause.");

            while ((bool)busy.GetValue(join) && Time.realtimeSinceStartup < started + 45f) yield return null;
            Assert.IsFalse((bool)busy.GetValue(join), "The join never ended.");
            StringAssert.StartsWith("No answer from " + target, status.text);
            Assert.IsInstanceOf<HubJoin>(hub.Top, "The failure left the JOIN screen.");
            Assert.IsFalse(net.IsNetworked, "The failed join kept its transport.");
            Assert.IsFalse(SceneFlow.Networked);
            hub.Back();
            Assert.IsNotInstanceOf<HubJoin>(hub.Top);
        }
    }
}
