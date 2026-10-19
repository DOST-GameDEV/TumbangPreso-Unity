using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// ⚠️ THE HOST'S FIREWALL LINE IN THE REAL ROOM SCREEN (LAN-VPN-JOIN-1020), driven through a fake
    /// machine side so no test reads or changes this PC's firewall. The read, the verdict and the
    /// helper script are Core's and asserted in Core.Tests; this is the room: when the line shows,
    /// what the control does, what each answer from Windows looks like, and that pad, keyboard and
    /// touch reach the control and are never left holding a hidden one.
    /// </summary>
    public sealed class HubLobbyLanFirewallTests
    {
        private const string Exe = @"C:\Games\Tumbang Preso\TumbangPreso.exe";
        private const string BlockId = "UDP Query User{6C852D9C-85B4-4325-893A-58E63F4D643F}c:\\games\\tumbang preso\\tumbangpreso.exe";

        private static string Probe(params string[] rules) =>
            "active=4\nprofile=1\t1\t0\nprofile=2\t1\t0\nprofile=4\t1\t0\n" +
            string.Concat(rules.Select(r => r + "\n")) + "end\n";

        private static readonly string Nothing = Probe();
        private static readonly string Blocked = Probe(
            "rule=local\t" + BlockId + "\tv2.10|Action=Block|Active=TRUE|Dir=In|Protocol=17|Profile=Public|App=c:\\games\\tumbang preso\\tumbangpreso.exe|Name=tumbangpreso|");
        private static readonly string Ours = Probe(
            "rule=local\t{ours}\tv2.33|Action=Allow|Active=TRUE|Dir=In|Protocol=17|Profile=Private|Profile=Public|App=" + Exe + "|Name=" + LanFirewallRules.RuleName + "|");
        private static readonly string Policy = Probe(
            "rule=local\t{all}\tv2.33|Action=Block|Active=TRUE|Dir=In|Protocol=17|Profile=Public|Name=No games|");

        private sealed class Machine : ILanFirewallBackend
        {
            public bool Supported { get; set; } = true;
            public string ProgramPath => Exe;
            public string Now = Nothing, AfterHelper;
            public bool Decline;
            public int Reads, Runs;
            public string Script;
            public ManualResetEventSlim Gate;
            public string Probe() { Interlocked.Increment(ref Reads); return Now; }
            public int RunElevated(string script)
            {
                Runs++;
                Script = script;
                Gate?.Wait(5000);
                if (Decline) throw new LanFirewallDeclinedException();
                if (AfterHelper != null) Now = AfterHelper;
                return 0;
            }
        }

        private sealed class Host : IHubHost
        {
            public string OnlineBrowserMessage { get; set; } = "";
            public HubSeat[] Data = {
                new HubSeat { Slot = 0, Occupied = true, Mine = true, Host = true, Name = "YOU", CharacterPick = -1 },
                new HubSeat { Slot = 1, Bot = true, CharacterPick = -1 }, new HubSeat { Slot = 2, Bot = true, CharacterPick = -1 },
                new HubSeat { Slot = 3, Bot = true, CharacterPick = -1 } };
            public bool Hosting = true, Online;
            public MapPreviewSurface Preview => null; public bool OverlayOpen => false;
            public bool InRoom => true; public bool IsHost => Hosting; public bool LocalReady => false;
            public string RoomCode => "LANF"; public bool RoomOnline => Online; public string RoomTitle => "FIREWALL";
            public bool MatchInProgress { get; set; } public bool Spectating => false; public string RoomAddress => Online ? "" : "25.17.144.85";
            public bool MapVoting => false; public float MapVoteSecondsLeft => 0; public int MapVoteWinner => 0;
            public int MapVoteFor(int seat) => -1; public HubSeat[] Seats() => Data;
            public LobbySeatSwapOffer SeatSwapOffer { get; set; }
            public string SeatSwapResult => ""; public void RespondToSeatSwap(long id, bool accept) { }
            public bool CanTakeSeat(int seat) => false; public void TakeSeat(int seat) { }
            public bool CanEditBots => Hosting; public void SetSeatBot(int seat, bool bot) { }
            public void OpenSettings() { } public void OpenProfile() { } public void OpenCareer() { } public void OpenParty() { } public void OpenCustomRules() { }
            public void StartPractice() { } public string StartQueue(GameMode mode, QueueStake stake) => "";
            public void CancelQueue() { } public void AcceptBots() { } public void LeaveRoom() { }
            public Task<string> HostRoom(string title, string map, GameMode mode, RoomVisibility visibility, bool online) => Task.FromResult("");
            public Task<string> Join(string codeOrAddress) => Task.FromResult("");
            public void StartGame() { } public void ToggleReady() { } public void LockIn() { } public void VoteMap(int index) { }
            public void PublishPicks() { } public void SelectMap(string map) { } public void SelectMode(GameMode mode) { }
            public void Browse() { } public void RefreshRooms(bool lan) { } public List<HubRoom> Rooms(bool lan) => new List<HubRoom>(); public void ToggleChat() { } public void ToggleSpectate() { }
        }

        private GameObject _root, _hubRoot, _cameraRoot, _events;
        private Camera _camera;
        private RenderTexture _target;
        private Host _host;
        private Machine _machine;
        private HubLobby _screen;

        [SetUp]
        public void Before()
        {
            _host = new Host();
            _machine = new Machine();
            LanFirewall.Backend = _machine;
        }

        [TearDown]
        public void After()
        {
            _machine.Gate?.Set();
            LanFirewall.Backend = null;
            if (_events != null) Lifecycle(_events.GetComponent<EventSystem>(), "OnDisable");
            if (_camera != null) _camera.targetTexture = null;
            foreach (var made in new[] { _root, _hubRoot, _cameraRoot, _events })
                if (made != null) Object.DestroyImmediate(made);
            if (_target != null) { _target.Release(); Object.DestroyImmediate(_target); }
        }

        private void Open()
        {
            _hubRoot = new GameObject("Firewall hub");
            var hub = _hubRoot.AddComponent<TumpHub>();
            hub.enabled = false;
            typeof(TumpHub).GetProperty("Host").SetValue(hub, _host);
            _root = new GameObject("Firewall canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            typeof(TumpHub).GetProperty("Canvas").SetValue(hub, _root.GetComponent<Canvas>());
            _screen = _root.AddComponent<HubLobby>();
            _screen.enabled = false;
            typeof(HubScreen).GetProperty("Hub").SetValue(_screen, hub);
            typeof(HubScreen).GetProperty("Root").SetValue(_screen, _root.GetComponent<RectTransform>());
            _cameraRoot = new GameObject("Firewall UI camera", typeof(Camera));
            _camera = _cameraRoot.GetComponent<Camera>();
            _camera.enabled = false;
            _camera.cullingMask = 1 << 31;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(.47f, .45f, .38f);
            _target = new RenderTexture(960, 540, 24);
            _camera.targetTexture = _target;
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = 1;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            ScreenFocus.Install(_root);
            _screen.Build();
            foreach (var t in _root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            Settle();
        }

        private static void Lifecycle(EventSystem system, string message) =>
            typeof(EventSystem).GetMethod(message, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                                                   System.Reflection.BindingFlags.Public).Invoke(system, null);

        /// <summary>Ticks the room until the background read or request has landed and been drawn.</summary>
        private void Settle()
        {
            for (int i = 0; i < 600; i++)
            {
                _screen.Tick();
                if (!LanFirewall.Working) { _screen.Tick(); return; }
                Thread.Sleep(5);
            }
            Assert.Fail("the firewall read or request never finished");
        }

        private GameObject Notice => _root.transform.Find("LanFirewall").gameObject;
        private Text Line => Notice.transform.Find("Line").GetComponent<Text>();
        private HubButton Allow => _root.GetComponentsInChildren<HubButton>(true).First(b => b.name == "AllowLanPlay");
        private ScreenFocus Focus => _root.GetComponent<ScreenFocus>();

        [Test]
        public void AnUnreachableLanHostSeesOneLineAndAControlEveryDeviceReaches()
        {
            Open();
            Assert.IsTrue(Notice.activeInHierarchy, "the host of a LAN room nobody can reach must be told");
            Assert.AreEqual("Windows Firewall may block Hamachi or Radmin players.", Line.text);
            Assert.IsTrue(Allow.gameObject.activeInHierarchy && Allow.IsInteractable());
            Assert.AreEqual(LanFirewallRules.AllowLabel, HubKit.LabelOf(Allow).text);

            // Pad and keyboard: it is on the screen's focus path with real neighbours. Touch: it is a
            // pressable control with its own hit area.
            Focus.Rebuild();
            CollectionAssert.Contains(Focus.Order, Allow, "a pad must be able to walk to ALLOW LAN PLAY");
            var nav = Allow.navigation;
            Assert.IsTrue(nav.selectOnUp != null || nav.selectOnDown != null || nav.selectOnLeft != null || nav.selectOnRight != null,
                          "the control must link to its neighbours so the pad can leave it again");
            Assert.IsNotNull(Allow.targetGraphic);
            Assert.IsTrue(Allow.targetGraphic.raycastTarget, "a thumb or a mouse must be able to press it");
        }

        [Test]
        public void ThePressRunsTheHelperOnceWithTheNamedBlockRuleAndTheReadConfirms()
        {
            _machine.Now = Blocked;
            _machine.AfterHelper = Ours;
            Open();
            Assert.IsTrue(Allow.gameObject.activeInHierarchy);
            int reads = _machine.Reads;

            Allow.onClick.Invoke();
            Settle();

            Assert.AreEqual(1, _machine.Runs);
            StringAssert.Contains(LanFirewallRules.Quote(BlockId), _machine.Script, "the one block rule naming this program goes");
            StringAssert.Contains(LanFirewallRules.Quote(LanFirewallRules.NetshAddArguments(Exe)), _machine.Script);
            Assert.Greater(_machine.Reads, reads, "the line only says allowed once a fresh read agrees");
            Assert.IsFalse(Notice.activeInHierarchy, "a reachable room says nothing");
            Assert.AreEqual(1, LanFirewall.AllowedCount);
        }

        [Test]
        public void ADeclinedPromptChangesNothingAndOffersTheControlAgain()
        {
            _machine.Decline = true;
            Open();
            Allow.onClick.Invoke();
            Settle();
            Assert.AreEqual("Not changed: Windows did not get permission.", Line.text);
            Assert.IsTrue(Allow.gameObject.activeInHierarchy && Allow.IsInteractable());
            Assert.AreEqual(0, LanFirewall.AllowedCount);
        }

        [Test]
        public void AHelperThatDidNotOpenTheRoomSaysSo()
        {
            Open();
            Allow.onClick.Invoke();
            Settle();
            Assert.AreEqual(1, _machine.Runs);
            Assert.AreEqual("Windows Firewall still blocks players joining.", Line.text);
            Assert.IsTrue(Allow.gameObject.activeInHierarchy, "the player may try again");
        }

        [Test]
        public void APadThatPressedTheControlIsHandedToStartWhileWindowsAsks()
        {
            _machine.Gate = new ManualResetEventSlim(false);
            _machine.AfterHelper = Ours;
            // ⚠️ EDIT MODE NEVER ENABLES A MONOBEHAVIOUR BY ITSELF, so the event system is registered
            // by hand (and unregistered in TearDown) or `EventSystem.current` stays empty.
            _events = new GameObject("Firewall events", typeof(EventSystem));
            Lifecycle(_events.GetComponent<EventSystem>(), "OnEnable");
            EventSystem.current = _events.GetComponent<EventSystem>();
            Open();
            EventSystem.current.SetSelectedGameObject(Allow.gameObject);

            Allow.onClick.Invoke();
            Assert.AreEqual("Waiting for Windows to allow LAN play...", Line.text);
            Assert.IsFalse(Allow.gameObject.activeSelf, "no second request while Windows is asking");
            Assert.AreEqual("StartGame", EventSystem.current.currentSelectedGameObject.name);

            _machine.Gate.Set();
            Settle();
            Assert.IsFalse(Notice.activeInHierarchy);
        }

        [Test]
        public void ASettingTheGameMustNotChangeIsExplainedWithoutAControl()
        {
            _machine.Now = Policy;
            Open();
            Assert.AreEqual("A Windows Firewall setting blocks players joining.", Line.text);
            Assert.IsFalse(Allow.gameObject.activeSelf);
        }

        [Test]
        public void NothingIsShownWhenThereIsNothingToFix()
        {
            _machine.Now = Ours;
            Open();
            Assert.IsFalse(Notice.activeInHierarchy, "an installed copy reads open");
        }

        [Test]
        public void OnlineRoomsClientsAndOtherMachinesNeverReadTheFirewall()
        {
            _host.Online = true;
            Open();
            Assert.IsFalse(Notice.activeInHierarchy, "a Relay room needs no inbound rule");
            Assert.AreEqual(0, _machine.Reads);
            After(); Before();

            _host.Hosting = false;
            Open();
            Assert.IsFalse(Notice.activeInHierarchy, "a client hosts nothing");
            Assert.AreEqual(0, _machine.Reads);
            After(); Before();

            _machine.Supported = false;
            Open();
            Assert.IsFalse(Notice.activeInHierarchy, "not Windows: no Windows Firewall");
            Assert.AreEqual(0, _machine.Reads);
        }

        [Test]
        public void TheLineStepsAsideWhileChatHoldsTheColumn()
        {
            Open();
            _screen.SetChatPresented(true);
            _screen.Tick();
            Assert.IsFalse(Notice.activeInHierarchy);
            _screen.SetChatPresented(false);
            _screen.Tick();
            Assert.IsTrue(Notice.activeInHierarchy);
        }

        /// <summary>
        /// The review picture, written only when TUMP_FIREWALL_CAPTURE names a file: the room with
        /// the line and its control, and a second frame (`_policy` suffix) with the line alone.
        /// </summary>
        [Test]
        public void RenderTheFirewallLineForReview()
        {
            string path = System.Environment.GetEnvironmentVariable("TUMP_FIREWALL_CAPTURE");
            if (string.IsNullOrEmpty(path) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Open();
            Write(path);
            After(); Before();
            _machine.Now = Policy;
            Open();
            Write(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_policy.png"));
        }

        private void Write(string path)
        {
            foreach (var slap in _root.GetComponentsInChildren<HubSlap>(true))
            {
                slap.enabled = false;
                slap.transform.localScale = Vector3.one;
                slap.transform.localRotation = Quaternion.identity;
                var group = slap.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 1;
            }
            Canvas.ForceUpdateCanvases();
            _camera.Render();
            var previous = RenderTexture.active;
            var image = new Texture2D(960, 540, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = _target;
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
