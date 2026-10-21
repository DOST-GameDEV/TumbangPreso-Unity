using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class HubAdmissionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string _settings, _pendingJoin;
        private bool _hubEnabled;
        private int _choice, _width, _height;
        private FullScreenMode _fullScreen;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
            _pendingJoin = SceneFlow.PendingJoinCode;
            _hubEnabled = ConvertedMatchSetup.HubEnabled;
            _choice = HubHome.Choice;
            _width = Screen.width; _height = Screen.height; _fullScreen = Screen.fullScreenMode;
            SceneFlow.PendingJoinCode = "";
            ConvertedMatchSetup.HubEnabled = true;
            HubHome.Choice = 1;
            Settings.SettingsStore.Current.GraphicsQuality = 0;
            Settings.GraphicsProfiles.Apply(0);
            Screen.SetResolution(960, 540, FullScreenMode.Windowed);
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            NetSession.Instance?.Stop();
            HubQueueWatch.End();
            SceneFlow.Networked = false;
            SceneFlow.PendingJoinCode = _pendingJoin;
            ConvertedMatchSetup.HubEnabled = _hubEnabled;
            HubHome.Choice = _choice;
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            Settings.SettingsStore.Save();
            Settings.GraphicsProfiles.Apply(Settings.SettingsStore.Current.GraphicsQuality);
            Screen.SetResolution(_width, _height, _fullScreen);
            yield return PlayModeWorld.Reset();
        }

        private static int UnusedPort()
        {
            using var socket = new System.Net.Sockets.UdpClient(0);
            return ((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator HubEntersClientRoomOnlyAfterConnectionAndSeatAdmission()
        {
            yield return HubFlowTests.OpenHome();
            var hub = TumpHub.Current;
            var screen = Object.FindFirstObjectByType<ConvertedMatchSetup>();
            var net = NetSession.Instance;
            var started = net.StartClientAsync("127.0.0.1", UnusedPort());
            while (!started.IsCompleted) yield return null;
            Assert.IsTrue(started.Result, net.Status);
            Assert.IsTrue(net.IsNetworked, "The causal control did not start a listening client transport.");
            Assert.IsFalse(screen.InRoom, "A transport still awaiting admission was exposed as an entered room.");
            yield return null;
            Assert.IsInstanceOf<HubHome>(hub.Top, "Transport startup navigated HOME into an unadmitted lobby.");
            typeof(NetSession).GetField("_everConnected", Private).SetValue(net, true);
            Assert.IsFalse(screen.InRoom, "Connection without a seat was accepted as an entered room.");
            net.ApplyAssignedSeat(1);
            Assert.IsTrue(screen.InRoom);
            yield return null;
            Assert.IsInstanceOf<HubLobby>(hub.Top);
            net.ApplyAssignedSeat(-1);
            Assert.IsTrue(screen.InRoom, "An admitted spectator must still be in the room.");
            net.Stop();
            Assert.IsFalse(screen.InRoom, "A stopped client retained its entered-room state.");
            var hosted = net.StartHostAsync(UnusedPort());
            while (!hosted.IsCompleted) yield return null;
            Assert.IsTrue(hosted.Result, net.Status);
            Assert.IsTrue(screen.InRoom, "The admission gate excluded a real host.");
        }
    }
}
