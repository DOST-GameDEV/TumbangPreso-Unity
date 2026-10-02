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
    public sealed class PlayerHubFriendJoinHandlerTests
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

        // Exercise the production action bound by BuildFriendRows without making
        // the native control test depend on a row-parent or layout assumption.
        private static void InvokeFriendJoin(string code)
        {
            var playerHub = Object.FindFirstObjectByType<PlayerHub>();
            Assert.IsNotNull(playerHub);
            playerHub.OpenTab(PlayerHub.Door.Party);
            typeof(PlayerHub).GetMethod("JoinFriend", Private).Invoke(playerHub, new object[] { code });
            Assert.IsFalse(playerHub.IsOpen, "The Friends overlay still covered its join destination.");
        }
        private static LobbyJoinPanel Panel(ConvertedMatchSetup screen)
            => (LobbyJoinPanel)typeof(ConvertedMatchSetup).GetField("_joinPanel", Private).GetValue(screen);

        private static int UnusedPort()
        {
            using var socket = new System.Net.Sockets.UdpClient(0);
            return ((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator FriendActionFromHomeUsesExistingPanelAndKeepsRefusalVisible()
        {
            yield return HubFlowTests.OpenHome();
            var hub = TumpHub.Current;
            var screen = Object.FindFirstObjectByType<ConvertedMatchSetup>();
            var net = NetSession.Instance;
            net.Query.StopBrowsing();
            var pending = new TaskCompletionSource<bool>();
            int calls = 0; string requested = null;
            Panel(screen).Connection = (code, cancellation) =>
            {
                calls++; requested = code;
                return pending.Task;
            };
            var scene = SceneManager.GetActiveScene().handle;
            InvokeFriendJoin("ABCD");
            Assert.AreEqual(1, calls, "Friend JOIN did not reach the existing join controller from HOME.");
            Assert.AreEqual("ABCD", requested);
            Assert.IsInstanceOf<HubJoin>(hub.Top);
            Assert.IsEmpty(SceneFlow.PendingJoinCode, "An in-place join left a stale scene request.");
            Assert.IsFalse((bool)typeof(ServerQuery).GetField("_browsing", Private).GetValue(net.Query),
                "A known friend code unnecessarily started public room browsing.");
            yield return null;
            Assert.AreSame(hub, TumpHub.Current, "Friend JOIN reloaded the current hub.");
            Assert.AreEqual(scene, SceneManager.GetActiveScene().handle);
            pending.SetResult(false);
            yield return null;
            yield return null;
            Assert.IsInstanceOf<HubJoin>(hub.Top, "Refusal removed the retry screen.");
            var status = (Text)typeof(HubJoin).GetField("_status", Private).GetValue(hub.Top);
            var codeField = (InputField)typeof(HubJoin).GetField("_codeField", Private).GetValue(hub.Top);
            Assert.That(status.text, Does.Contain("Could not join"));
            Assert.IsTrue(status.gameObject.activeInHierarchy, "The refusal text is hidden.");
            Assert.AreEqual("ABCD", codeField.text);
            hub.Back();
            Assert.IsInstanceOf<HubHome>(hub.Top);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator FriendActionFromHostedRoomCanCancelWithoutLateLobbyEntry()
        {
            yield return HubFlowTests.OpenHome();
            var net = NetSession.Instance;
            var hosting = net.StartHostAsync(UnusedPort());
            while (!hosting.IsCompleted) yield return null;
            Assert.IsTrue(hosting.Result, net.Status);
            SceneFlow.Networked = true;
            yield return null;
            var hub = TumpHub.Current;
            Assert.IsInstanceOf<HubLobby>(hub.Top);
            var screen = Object.FindFirstObjectByType<ConvertedMatchSetup>();
            var pending = new TaskCompletionSource<bool>();
            int calls = 0; CancellationToken cancellation = default;
            Panel(screen).Connection = (code, token) =>
            {
                calls++; cancellation = token;
                return pending.Task;
            };
            InvokeFriendJoin("WXYZ");
            Assert.AreEqual(1, calls, "An existing hosted room swallowed Friend JOIN.");
            Assert.IsInstanceOf<HubJoin>(hub.Top);
            Assert.AreSame(hub, TumpHub.Current);
            hub.Back();
            Assert.IsTrue(cancellation.IsCancellationRequested, "BACK did not cancel the pending join.");
            pending.SetResult(true);
            yield return null;
            yield return null;
            Assert.IsFalse(hub.Top is HubLobby, "Late completion returned to the room after BACK.");
            Assert.IsFalse(SceneFlow.Networked);
            Assert.IsEmpty(SceneFlow.PendingJoinCode);
        }

    }
}
