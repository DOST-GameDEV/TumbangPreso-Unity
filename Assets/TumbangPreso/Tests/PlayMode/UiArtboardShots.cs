using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.Net;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The current UI taken apart screen by screen for the revamp's Photoshop artboards
    /// (`UiArtboardCapture`). Review material: each step opens one state and photographs it.
    /// A step that cannot open its state is logged and skipped so the rest still run; the
    /// test fails at the end listing what was missed. Nothing here saves or submits.
    /// </summary>
    public sealed class UiArtboardShots
    {
        private const BindingFlags Priv = BindingFlags.Instance | BindingFlags.NonPublic;
        private string _settings;
        private int _soloSeat; private bool _allBots, _guided, _force, _booted, _offered, _networked;
        private static readonly List<string> Missed = new List<string>();

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _settings = JsonUtility.ToJson(SettingsStore.Current);
            _soloSeat = GameLaunch.SoloSeat; _allBots = GameLaunch.AllBots; _guided = GameLaunch.GuidedTutorial;
            _force = TouchHud.ForceVisible; _booted = SceneFlow.BootedThroughSplash; _offered = SceneFlow.LoginStepOffered;
            _networked = SceneFlow.Networked;
            Missed.Clear();
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            NetSession.Instance?.Stop(); HubQueueWatch.End(); HubLoading.Cancel();
            TouchHud.ForceVisible = _force;
            GameLaunch.SoloSeat = _soloSeat; GameLaunch.AllBots = _allBots; GameLaunch.GuidedTutorial = _guided;
            SceneFlow.BootedThroughSplash = _booted; SceneFlow.LoginStepOffered = _offered; SceneFlow.Networked = _networked;
            Time.timeScale = 1;
            yield return PlayModeWorld.Reset();
            SettingsStore.Restore(JsonUtility.FromJson<GameSettings>(_settings));
        }

        // ---------------------------------------------------------------- shared

        /// <summary>Runs a step; an exception inside it is logged and the step abandoned.</summary>
        private static IEnumerator Step(string name, IEnumerator body)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(body);
            while (stack.Count > 0)
            {
                object current; bool moved;
                try { moved = stack.Peek().MoveNext(); current = moved ? stack.Peek().Current : null; }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[ArtboardMissed] " + name + ": " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
                    Missed.Add(name); yield break;
                }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
        }

        private static void Report()
        {
            if (Missed.Count > 0) Assert.Fail("States not captured: " + string.Join(", ", Missed));
        }

        private static IEnumerator Shot(string id, string title, string group, string note = null)
            => UiArtboardCapture.Shoot(id, title, group, note);

        private static Button Global(string name)
        {
            var b = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == name && x.isActiveAndEnabled);
            Assert.IsNotNull(b, "No " + name + " on screen.");
            return b;
        }

        private static IEnumerator Click(string name, float wait = .45f)
        {
            float until = Time.realtimeSinceStartup + 5; Button b = null;
            while (b == null && Time.realtimeSinceStartup < until)
            {
                b = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == name && x.isActiveAndEnabled && x.IsInteractable());
                if (b == null) yield return null;
            }
            Assert.IsNotNull(b, "No pressable " + name);
            b.onClick.Invoke();
            yield return new WaitForSecondsRealtime(wait);
        }

        private static IEnumerator Wait(System.Func<bool> done, float seconds, string what)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(done(), "Timed out waiting for " + what);
        }

        /// <summary>A live offline round on a map with the opening and role card already past.</summary>
        internal static IEnumerator Match(string scene, GameMode mode)
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            yield return SceneManager.LoadSceneAsync(scene); yield return new WaitForSecondsRealtime(.4f);
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSecondsRealtime(3.7f);
            float until = Time.realtimeSinceStartup + 40;
            while ((Map.ArenaIntro.HidesUi || RoleSwapCard.Showing) && Time.realtimeSinceStartup < until) yield return null;
            // Past GO! and the TAYA reveal, so both have left the screen.
            yield return new WaitForSecondsRealtime(3.5f);
            var round = GameServices.Round; var local = round.PlayerAt(GameLaunch.SoloSeat);
            TumpUiCapture.StageHudReview(local);
            local.GetComponent<PlayerInputReader>().enabled = false; local.Intent.Clear(); local.enabled = false;
            round.enabled = false;
            local.PlayerName = "Juan_dela_Cruz_99";
            var scores = (Scoreboard)typeof(MatchDirector).GetField("_scores", Priv).GetValue(GameServices.Match);
            scores.SetAll(new[] { 2450, 1980, 640, 1120 });
            SettingsStore.Current.ReducedUiMotion = true;
        }

        private static CharacterMotor Local => GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
        private static TumpMatchReadout View => Object.FindFirstObjectByType<TumpMatchReadout>();

        private static void Quiet()
        {
            Local.ClearStatuses(); View.Tick(Local, false, false, false, false);
        }

        // ---------------------------------------------------------------- boot

        [UnityTest, Timeout(900000)]
        public IEnumerator Boot()
        {
            GameServices.Ensure();
            SceneFlow.BootedThroughSplash = true; SceneFlow.LoginStepOffered = false;
            SettingsStore.Current.AccountHasPassword = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu); yield return null;
            var menu = Object.FindFirstObjectByType<ConvertedMainMenu>();
            var signIn = menu.GetComponent<SignInScreen>();
            yield return new WaitForSecondsRealtime(.8f);
            yield return Step("boot-signup", Shot("boot-signup", "Sign up (boot)", "Boot"));
            yield return Step("boot-terms", Terms());
            yield return Step("boot-signin", SignInTab());
            yield return Step("boot-welcome", Welcome(signIn));
            yield return Step("boot-password", Password());
            yield return Step("boot-title", Title(signIn));
            Report();
        }

        private static IEnumerator Terms()
        {
            yield return Click("TermsLink", .7f);
            yield return Shot("boot-terms", "Terms and conditions", "Boot");
            yield return Click("TermsBack", .4f);
        }

        private static IEnumerator SignInTab()
        {
            yield return Click("SignInTab", .6f);
            yield return Shot("boot-signin", "Sign in (boot)", "Boot");
            yield return Click("CreateAccountTab", .4f);
        }

        private static IEnumerator Welcome(SignInScreen signIn)
        {
            var previous = GameServices.Account; var go = new GameObject("Artboard account"); go.SetActive(false);
            var account = go.AddComponent<PlayerAccount>();
            typeof(PlayerAccount).GetField("_profile", Priv).SetValue(account, new AccountProfile { PlayerId = "artboard", DisplayName = "Juan", Username = "juan.dc" });
            typeof(GameServices).GetProperty("Account").SetValue(null, account);
            SettingsStore.Current.AccountHasPassword = true;
            try { signIn.OpenAtBoot(); typeof(SignInScreen).GetField("_autoPassAt", Priv).SetValue(signIn, -1f); }
            catch { RestoreAccount(previous, go, signIn); throw; }
            yield return new WaitForSecondsRealtime(.7f);
            yield return Shot("boot-welcome", "Welcome back", "Boot");
            RestoreAccount(previous, go, signIn);
            yield return new WaitForSecondsRealtime(.4f);
        }

        private static void RestoreAccount(PlayerAccount previous, GameObject go, SignInScreen signIn)
        {
            SettingsStore.Current.AccountHasPassword = false;
            typeof(GameServices).GetProperty("Account").SetValue(null, previous);
            Object.DestroyImmediate(go);
            signIn.OpenAtBoot();
        }

        private static IEnumerator Password()
        {
            var owner = new GameObject("Artboard password");
            OwnerPasswordView.Open(owner.transform);
            yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("boot-password", "Change password", "Boot");
            Object.DestroyImmediate(owner);
            var stray = GameObject.Find("OwnerPasswordCanvas"); if (stray != null) Object.DestroyImmediate(stray);
            yield return null;
        }

        private static IEnumerator Title(SignInScreen signIn)
        {
            typeof(SignInScreen).GetMethod("Close", Priv).Invoke(signIn, null); yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("boot-title", "Title loading", "Boot");
        }

        // ---------------------------------------------------------------- hub

        [UnityTest, Timeout(1800000)]
        public IEnumerator Hub()
        {
            yield return Step("hub-home", HubScreen("hub-home", "HOME", null));
            yield return Step("hub-queue", Queue());
            yield return Step("hub-menu", HubScreen("hub-menu", "Menu", () => HubFlowTests.Press("MenuButton")));
            yield return Step("hub-menu-touch", TouchMenu());
            yield return Step("hub-avatar", HubScreen("hub-avatar", "Avatar picker", () => HubFlowTests.Press("AvatarButton")));
            foreach (var (tab, title) in new[] { ("Profile", "Player record / Profile"), ("Friends", "Player record / Friends"), ("Career", "Player record / Career"), ("Matches", "Player record / History"), ("Account", "Player record / Account") })
                yield return Step("hub-record-" + tab, Record(tab, title));
            yield return Step("hub-modes", HubScreen("hub-modes", "Game modes", () => HubFlowTests.Press("ModeCard")));
            foreach (var (card, title) in new[] { ("PracticeCard", "Game modes / Practice"), ("ClassicCard", "Game modes / Classic"), ("CustomCard", "Game modes / Custom") })
                yield return Step("hub-modes-" + card, HubScreen("hub-modes-" + card.Replace("Card", "").ToLowerInvariant(), title, () => ModePopup(card)));
            yield return Step("hub-host", HubScreen("hub-host", "Host game", () => Push<HubHost>()));
            yield return Step("hub-join", Join());
            yield return Step("hub-hero", HubScreen("hub-hero", "Hero", Hero));
            yield return Step("hub-ability", HubScreen("hub-ability", "Hero / Ability", () => Then(Hero(), HubFlowTests.Press("Ability0"))));
            yield return Step("hub-skilltree", HubScreen("hub-skilltree", "Skill tree", () => Push<HubSkillTree>(t => t.Hero = "sean", .9f)));
            yield return Step("hub-story", Story());
            yield return Step("hub-item", HubScreen("hub-item", "Items", () => HubFlowTests.Press("LoadoutButton")));
            yield return Step("hub-item-inspect", HubScreen("hub-item-inspect", "Items / Inspect", ItemPopup));
            yield return Step("hub-shop", HubScreen("hub-shop", "Shop", () => HubFlowTests.Press("ShopButton")));
            yield return Step("hub-tasks", HubScreen("hub-tasks", "Tasks", () => HubFlowTests.Press("TaskButton")));
            yield return Step("hub-matchfound", HubScreen("hub-matchfound", "Match found", () => Push<HubMatchFound>(null, .4f)));
            yield return Step("hub-pick", HubScreen("hub-pick", "Character select (timed)", () => Push<HubCharacterSelect>(s => s.Timed = true, 1f)));
            yield return Step("hub-toast", HubScreen("hub-toast", "Toast", Toast));
            yield return Step("hub-lost", HubScreen("hub-lost", "Connection lost", () => Push<HubDisconnectedNotice>(n => n.Message = "The host left the room.", .6f)));
            yield return Step("hub-replays", HubScreen("hub-replays", "Replays", () => Push<HubReplays>(null, .8f)));
            yield return Step("hub-credits", Credits());
            yield return Step("hub-rules-match", Rules("MatchRulesTab", "hub-rules-match", "Custom rules / The match"));
            yield return Step("hub-rules-room", Rules("RoomRulesTab", "hub-rules-room", "Custom rules / The room"));
            yield return Step("hub-loading", Loading());
            yield return Step("hub-letsplay", LetsPlay());
            Report();
        }

        private static IEnumerator Then(IEnumerator a, IEnumerator b) { yield return a; yield return b; }

        private static IEnumerator HubScreen(string id, string title, System.Func<IEnumerator> open)
        {
            yield return HubFlowTests.OpenHome();
            if (open != null) yield return open();
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot(id, title, "Hub");
        }

        private static IEnumerator Push<T>(System.Action<T> configure = null, float wait = .6f) where T : HubScreen
        {
            TumpHub.Current.Push(configure);
            yield return new WaitForSecondsRealtime(wait);
        }

        private static IEnumerator Queue()
        {
            yield return HubFlowTests.OpenHome();
            int choice = HubHome.Choice;
            HubHome.Choice = 2;
            yield return HubFlowTests.Press("PlayButton");
            yield return Wait(() => HubQueueWatch.QueueRoom, 10, "the queue");
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("hub-queue", "HOME / Searching", "Hub");
            yield return HubFlowTests.Press("CancelQueue");
            HubHome.Choice = choice;
        }

        private static IEnumerator TouchMenu()
        {
            yield return HubFlowTests.OpenHome();
            TouchHud.ForceVisible = true;
            TumpHub.Current.Push<HubMenu>();
            yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("hub-menu-touch", "Menu (touch)", "Mobile");
            TouchHud.ForceVisible = false;
            TumpHub.Current.Back();
        }

        private static IEnumerator Record(string tab, string title)
        {
            yield return HubFlowTests.OpenHome();
            yield return HubFlowTests.Press("NamePlate");
            yield return new WaitForSecondsRealtime(.6f);
            Global("HubTab" + tab).onClick.Invoke();
            yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("hub-record-" + tab.ToLowerInvariant(), title, "Hub");
            yield return Click("ClosePlayerHub", .3f);
        }

        private static IEnumerator ModePopup(string card)
        {
            yield return HubFlowTests.Press("ModeCard");
            yield return HubFlowTests.Press(card);
        }

        private static IEnumerator Join()
        {
            yield return HubFlowTests.OpenHome();
            var shot = Inject();
            try
            {
                shot.Lan = new List<HubRoom> { new HubRoom { Name = "KAPITBAHAY", Map = "ESKINITA", Players = 2, Capacity = 4, IsJoinable = true, Key = "192.168.1.9:7777" },
                                               new HubRoom { Name = "TONDO NIGHTS", Map = "KANTO", Players = 4, Capacity = 4, InProgress = true, Key = "192.168.1.12:7777" } };
                shot.Online = new List<HubRoom>();
                TumpHub.Current.Push<HubJoin>();
                yield return new WaitForSecondsRealtime(1.4f);
                yield return Shot("hub-join", "Join game / Rooms", "Hub");
                shot.Lan = new List<HubRoom>(); shot.BrowserAs = "No public rooms yet.";
                yield return new WaitForSecondsRealtime(1.3f);
                yield return Shot("hub-join-empty", "Join game / Empty", "Hub");
            }
            finally { Eject(shot); }
        }

        private static IEnumerator Hero()
        {
            yield return HubFlowTests.Press("HeroButton");
            yield return HubFlowTests.AwaitDeferredHero();
            yield return new WaitForSecondsRealtime(.8f);
        }

        private static IEnumerator Story()
        {
            yield return HubFlowTests.OpenHome();
            yield return Hero();
            yield return Click("StoryButton", .9f);
            yield return Shot("hub-story", "Hero / Story", "Hub");
            yield return Click("CloseCharacterStory", .3f);
        }

        private static IEnumerator ItemPopup()
        {
            yield return HubFlowTests.Press("LoadoutButton");
            var item = TumpHub.Current.Canvas.GetComponentsInChildren<Button>().First(b => b.name.StartsWith("Item_") && b.IsInteractable());
            item.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.8f);
        }

        private static IEnumerator Toast()
        {
            TumpHub.Current.Toast("Room code copied");
            yield return new WaitForSecondsRealtime(.25f);
        }

        private static IEnumerator Credits()
        {
            yield return HubFlowTests.OpenHome();
            yield return HubFlowTests.Press("MenuButton");
            yield return Click("MenuCREDITS", .9f);
            yield return Shot("hub-credits", "Credits", "Hub");
            yield return Click("CreditsBack", .3f);
        }

        private static IEnumerator Rules(string tab, string id, string title)
        {
            yield return HubFlowTests.OpenHome();
            TumpHub.Current.Host.OpenCustomRules();
            yield return new WaitForSecondsRealtime(.7f);
            Global(tab).onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot(id, title, "Hub");
            yield return Click("CustomRulesBack", .3f);
        }

        private static IEnumerator Loading()
        {
            yield return HubFlowTests.OpenHome();
            HubLoading.Begin(SceneFlow.Eskinita, externallyLoaded: true);
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("match-loading", "Match loading", "Match");
            var loading = Object.FindFirstObjectByType<HubLoading>(); loading.StopAllCoroutines();
            typeof(HubLoading).GetMethod("Fail", Priv).Invoke(loading, new object[] { "The match could not be loaded." });
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("match-loading-failed", "Match loading / Failed", "Match");
            HubLoading.Cancel();
            yield return null;
        }

        private static IEnumerator LetsPlay()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.ModeSelect);
            yield return new WaitForSecondsRealtime(1.6f);
            yield return Shot("hub-letsplay", "Let's play (from Career)", "Hub");
        }

        // ---------------------------------------------------------------- room

        [UnityTest, Timeout(900000)]
        public IEnumerator Room()
        {
            yield return HubFlowTests.OpenHome();
            var rules = SceneFlow.SelectedRules;
            var hosting = TumpHub.Current.Host.HostRoom("TEST ROOM", SceneFlow.Eskinita, GameMode.HeroStrike, RoomVisibility.Public, false);
            yield return Wait(() => hosting.IsCompleted, 15, "a LAN room");
            Assert.IsTrue(string.IsNullOrEmpty(hosting.Result), "Hosting failed: " + hosting.Result);
            yield return Wait(() => TumpHub.Current.Top is HubLobby, 5, "the lobby");
            yield return new WaitForSecondsRealtime(1f);
            yield return Step("room-host", Shot("room-host", "Room / Host", "Hub"));
            yield return Step("room-close", CloseRoom());
            yield return Step("room-chat", Chat());
            var shot = Inject();
            try
            {
                yield return Step("room-guest", Guest(shot));
                yield return Step("room-settings", RoomSettings());
                yield return Step("room-swap", Swap(shot));
                yield return Step("room-spectator", Spectator(shot));
            }
            finally { Eject(shot); }
            yield return Step("room-mapvote", MapVote());
            TumpHub.Current.Host.LeaveRoom(); NetSession.Instance?.Stop();
            SceneFlow.SetSelectedRules(rules);
            Report();
        }

        private static IEnumerator CloseRoom()
        {
            TumpHub.Current.Back();
            yield return new WaitForSecondsRealtime(.6f);
            Assert.IsInstanceOf<HubRoomClosePopup>(TumpHub.Current.Top);
            yield return Shot("room-close", "Room / Close room?", "Hub");
            yield return Click("KeepRoom", .4f);
        }

        private static IEnumerator Chat()
        {
            TumpHub.Current.Host.ToggleChat();
            yield return new WaitForSecondsRealtime(.8f);
            yield return Shot("room-chat", "Room / Chat open", "Hub");
            TumpHub.Current.Host.ToggleChat();
            yield return new WaitForSecondsRealtime(.6f);
        }

        private static HubSeat[] GuestSeats(bool mine) => new[]
        {
            new HubSeat { Slot = 0, Occupied = true, Host = true, Name = "MARIA", CharacterPick = 2, Ready = true },
            new HubSeat { Slot = 1, Occupied = true, Mine = mine, Name = mine ? "YOU" : "JUAN", CharacterPick = 0 },
            new HubSeat { Slot = 2, Bot = true, Occupied = true, Name = "BOT", CharacterPick = -1 },
            new HubSeat { Slot = 3, CharacterPick = -1 },
        };

        private static IEnumerator Guest(ShotHost shot)
        {
            shot.IsHostAs = false; shot.TitleAs = "MARIA'S ROOM"; shot.SeatsAs = GuestSeats(true);
            yield return new WaitForSecondsRealtime(.8f);
            yield return Shot("room-guest", "Room / Guest", "Hub");
        }

        private static IEnumerator RoomSettings()
        {
            TumpHub.Current.Push<HubMatchSettingsPopup>();
            yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("room-settings", "Room / Match settings", "Hub");
            TumpHub.Current.Back();
            yield return new WaitForSecondsRealtime(.3f);
        }

        private static IEnumerator Swap(ShotHost shot)
        {
            shot.Offer = new LobbySeatSwapOffer { Id = 21, FromSeat = 0, ToSeat = 1, Incoming = true, RequesterName = "Maria", ExpiresAt = double.MaxValue };
            yield return Wait(() => TumpHub.Current.Top is HubSeatSwapPopup, 3, "the seat swap popup");
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("room-swap", "Room / Switch seats?", "Hub");
            yield return Click("DeclineSeatSwap", .4f);
            shot.Offer = null;
        }

        private static IEnumerator Spectator(ShotHost shot)
        {
            shot.SpectatingAs = true; shot.SeatsAs = GuestSeats(false);
            yield return new WaitForSecondsRealtime(.8f);
            yield return Shot("room-spectator", "Room / Spectator", "Hub");
        }

        private static IEnumerator MapVote()
        {
            MatchRpc.Instance.HostBeginQueueMapVote();
            typeof(MatchRpc).GetField("_queueVoteEnds", Priv).SetValue(MatchRpc.Instance, Time.unscaledTime + 80);
            yield return Wait(() => TumpHub.Current.Top is HubMapVote, 5, "the map vote");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("room-mapvote", "Map vote", "Hub");
        }

        private static ShotHost Inject()
        {
            var shot = new ShotHost(TumpHub.Current.Host);
            typeof(TumpHub).GetProperty("Host").SetValue(TumpHub.Current, shot);
            return shot;
        }

        private static void Eject(ShotHost shot)
        {
            if (TumpHub.Current != null && TumpHub.Current.Host == shot) typeof(TumpHub).GetProperty("Host").SetValue(TumpHub.Current, shot.R);
        }

        /// <summary>Forwards to the live host and overrides only what a guest or spectator sees.</summary>
        private sealed class ShotHost : IHubHost
        {
            public readonly IHubHost R; public ShotHost(IHubHost r) { R = r; }
            public bool? IsHostAs, SpectatingAs; public HubSeat[] SeatsAs; public string TitleAs;
            public List<HubRoom> Lan, Online; public string BrowserAs; public LobbySeatSwapOffer Offer;
            public MapPreviewSurface Preview => R.Preview; public bool OverlayOpen => R.OverlayOpen;
            public void OpenSettings() => R.OpenSettings(); public void OpenProfile() => R.OpenProfile(); public void OpenCareer() => R.OpenCareer();
            public void OpenParty() => R.OpenParty(); public void OpenCustomRules() => R.OpenCustomRules(); public void StartPractice() => R.StartPractice();
            public string StartQueue(GameMode m, QueueStake s) => R.StartQueue(m, s); public void CancelQueue() => R.CancelQueue(); public void AcceptBots() => R.AcceptBots();
            public Task<string> HostRoom(string t, string m, GameMode g, RoomVisibility v, bool o) => R.HostRoom(t, m, g, v, o);
            public Task<string> Join(string c) => R.Join(c); public void LeaveRoom() => R.LeaveRoom();
            public bool InRoom => R.InRoom; public bool IsHost => IsHostAs ?? R.IsHost; public bool LocalReady => R.LocalReady;
            public string RoomCode => R.RoomCode; public bool RoomOnline => R.RoomOnline; public string RoomTitle => TitleAs ?? R.RoomTitle;
            public bool MatchInProgress => R.MatchInProgress; public HubSeat[] Seats() => SeatsAs ?? R.Seats();
            public bool CanTakeSeat(int s) => SeatsAs == null && R.CanTakeSeat(s); public void TakeSeat(int s) { }
            public bool CanEditBots => IsHost && R.CanEditBots; public void SetSeatBot(int s, bool b) { }
            public LobbySeatSwapOffer SeatSwapOffer => Offer ?? R.SeatSwapOffer; public string SeatSwapResult => R.SeatSwapResult;
            public void RespondToSeatSwap(long id, bool a) { if (Offer != null) { Offer = null; return; } R.RespondToSeatSwap(id, a); }
            public void StartGame() { } public void ToggleReady() { } public void LockIn() => R.LockIn();
            public bool MapVoting => R.MapVoting; public float MapVoteSecondsLeft => R.MapVoteSecondsLeft; public int MapVoteWinner => R.MapVoteWinner;
            public int MapVoteFor(int s) => R.MapVoteFor(s); public void VoteMap(int i) => R.VoteMap(i); public void PublishPicks() => R.PublishPicks();
            public void SelectMap(string m) => R.SelectMap(m); public void SelectMode(GameMode m) => R.SelectMode(m); public void Browse() => R.Browse();
            public void RefreshRooms(bool lan) => R.RefreshRooms(lan); public string OnlineBrowserMessage => BrowserAs ?? R.OnlineBrowserMessage;
            public List<HubRoom> Rooms(bool lan) => (lan ? Lan : Online) ?? R.Rooms(lan); public void ToggleChat() => R.ToggleChat();
            public void ToggleSpectate() => R.ToggleSpectate(); public bool Spectating => SpectatingAs ?? R.Spectating; public string RoomAddress => IsHost ? R.RoomAddress : "";
        }

        // ---------------------------------------------------------------- settings

        [UnityTest, Timeout(900000)]
        public IEnumerator SettingsScreens()
        {
            yield return HubFlowTests.OpenHome();
            TumpHub.Current.Host.OpenSettings();
            yield return new WaitForSecondsRealtime(.8f);
            var view = Object.FindFirstObjectByType<TumpSettingsView>();
            string[] names = { "Controls", "Audio", "Graphics", "Player", "Accessibility" };
            for (int i = 0; i < names.Length; i++)
            {
                int section = i;
                yield return Step("settings-" + names[i], SettingsSection(view, section, names[section]));
            }
            yield return Step("settings-keep", KeepChanges(view));
            yield return Step("settings-controller", ControllerMap());
            yield return Step("settings-touch", PhoneSettings());
            yield return Step("settings-touch-layout", TouchLayout());
            Report();
        }

        private static IEnumerator SettingsSection(TumpSettingsView view, int section, string name)
        {
            view.ShowSection(section);
            yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("settings-" + name.ToLowerInvariant(), "Settings / " + name, "Settings");
        }

        private static IEnumerator KeepChanges(TumpSettingsView view)
        {
            view.ShowSection(4);
            SettingsStore.Current.ReducedUiMotion = !SettingsStore.Current.ReducedUiMotion; view.Session.Preview(); view.Back();
            yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("settings-keep", "Settings / Keep your changes?", "Settings");
            yield return Click("KeepEditing", .3f);
            view.Session.Discard();
            yield return null;
        }

        private static IEnumerator ControllerMap()
        {
            // Through settings' own OPEN row, so the map takes over the way the player sees it.
            Object.FindFirstObjectByType<TumpSettingsView>().ShowSection(0);
            yield return new WaitForSecondsRealtime(.4f);
            yield return Click("ControllerMapAction", .9f);
            yield return Shot("settings-controller", "Settings / Controller map", "Settings");
            Object.FindFirstObjectByType<ControllerMapScreen>()?.Close();
            yield return new WaitForSecondsRealtime(.3f);
        }

        private static IEnumerator PhoneSettings()
        {
            yield return HubFlowTests.OpenHome();
            TouchHud.ForceVisible = true;
            var root = new GameObject("Artboard phone settings");
            try
            {
                var v = root.AddComponent<TumpSettingsView>();
                v.Open(root.transform, () => { }, () => { }, () => { });
                typeof(TumpSettingsView).GetField("_device", Priv).SetValue(v, InputDeviceKind.Touch);
                v.ShowSection(0);
            }
            catch { Object.DestroyImmediate(root); TouchHud.ForceVisible = false; throw; }
            yield return new WaitForSecondsRealtime(.8f);
            yield return Shot("settings-touch", "Settings (touch)", "Mobile");
            Object.DestroyImmediate(root); TouchHud.ForceVisible = false;
            yield return null;
        }

        private static IEnumerator TouchLayout()
        {
            yield return HubFlowTests.OpenHome();
            var root = new GameObject("Artboard touch layout");
            var editor = root.AddComponent<TumpTouchLayoutView>();
            editor.Open(root.transform, () => { });
            yield return new WaitForSecondsRealtime(.9f);
            yield return Shot("settings-touch-layout", "Touch layout editor", "Mobile");
            yield return Click("CancelTouchLayout", .3f);
            Object.DestroyImmediate(root);
            TouchHud.ForceVisible = false;
        }

        // ---------------------------------------------------------------- match

        [UnityTest, Timeout(1800000)]
        public IEnumerator MatchHud()
        {
            yield return Step("match-opening", Opening());
            yield return PlayModeWorld.Reset();

            GameLaunch.SoloSeat = ThrowerSeat;
            yield return Match("Eskinita", GameMode.HeroStrike);
            yield return Step("match-hud-thrower", HudShot("match-hud-thrower", "Match HUD / Thrower", Quiet));
            yield return Step("match-hud-crowded", HudShot("match-hud-crowded", "Match HUD / Busy moment", Crowded));
            foreach (var (kind, label) in new[] { (MatchMomentKind.SingleCatch, "1"), (MatchMomentKind.DoubleCatch, "2"), (MatchMomentKind.MultiKnockdown, "3") })
                yield return Step("match-moment-" + label, Moment(kind, label));
            yield return Step("match-tayareveal", Stamp("match-tayareveal", "Taya reveal", () => TayaReveal.Play(GameServices.Match.DefenderSlot), .4f));
            yield return Step("match-candown", Stamp("match-candown", "CAN DOWN", () => CanDownStamp.Play(GameLaunch.SoloSeat), .35f));
            yield return Step("match-powers", Powers("match-powers", "Held skills sheet", "Match"));
            yield return Step("match-emote", Emote());
            yield return Step("match-ultimate-card", UltimateCard());
            yield return Step("match-pause", Pause("match-pause", "Pause / Offline", false, false));
            yield return Step("match-pause-live", Pause("match-pause-live", "Pause / Online (live)", true, false));
            yield return Step("match-pause-leave", Leave());
            yield return Step("match-pause-ended", Pause("match-pause-ended", "Pause / Match ended", false, true));
            yield return Step("match-break", Break());
            yield return Step("match-result", Result());
            yield return PlayModeWorld.Reset();

            GameLaunch.SoloSeat = MatchRules.DefenderSlotFor(1);
            yield return Match("Eskinita", GameMode.HeroStrike);
            yield return Step("match-hud-taya", HudShot("match-hud-taya", "Match HUD / Taya", Quiet));
            yield return Step("match-halftime", Halftime());
            GameLaunch.SoloSeat = 0;
            yield return PlayModeWorld.Reset();

            yield return Match("Eskinita", GameMode.Classic);
            yield return Step("match-hud-classic", HudShot("match-hud-classic", "Match HUD / Classic", Quiet));
            yield return PlayModeWorld.Reset();

            yield return Step("match-ultimate-shared", SharedUltimate());
            yield return PlayModeWorld.Reset();
            yield return Step("match-catch", Catch());
            Report();
        }

        /// <summary>Second takes of the states the first full pass did not photograph correctly.</summary>
        [UnityTest, Timeout(1800000)]
        public IEnumerator Retakes()
        {
            yield return Step("match-opening", Opening());
            Report();
        }

        private static int ThrowerSeat => (MatchRules.DefenderSlotFor(1) + 1) % 4;

        private static IEnumerator Opening()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            SettingsStore.Current.CinematicCameraMotion = true;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return Wait(() => MatchArrivalPresentation.Active, 20, "the arrival");
            // The caption fades with the film's ink; mid-film it is fully up.
            float began = Time.realtimeSinceStartup;
            yield return Wait(() =>
            {
                var canvas = GameObject.Find("MatchArrivalCanvas");
                var caption = canvas != null ? canvas.transform.Find("ArrivalCaption") : null;
                var group = caption != null ? caption.GetComponent<CanvasGroup>() : null;
                if (group != null && group.alpha > .6f) return true;
                return Time.realtimeSinceStartup - began > 3f && MatchArrivalPresentation.Active;
            }, 15, "the arrival caption");
            yield return Shot("match-arrival", "Arrival caption", "Match");
            var gate = Object.FindFirstObjectByType<ReadyGate>(); string tick = null; gate.CountdownTick += s => tick = s;
            yield return Wait(() => tick == "3", 20, "the countdown");
            yield return new WaitForSecondsRealtime(.15f);
            yield return Shot("match-countdown", "Countdown", "Match");
        }

        private static IEnumerator HudShot(string id, string title, System.Action stage)
        {
            stage(); yield return null; yield return null;
            yield return Shot(id, title, "Match");
        }

        private static void Crowded()
        {
            var local = Local; var view = View;
            local.ClearStatuses();
            local.ApplyWhirled(30); local.ApplyChilled(30); local.ApplyRooted(30);
            var idle = new float[4]; idle[local.PlayerSlot] = 8; GameServices.Round.ApplyNetworkTournamentState(0, idle);
            view.Toast("OUT OF BOUNDS", 30);
            typeof(MatchDirector).GetMethod("PresentHostMoment", Priv).Invoke(GameServices.Match, new object[] { local.PlayerSlot, MatchMomentKind.FirstKnockdown, 1, 0 });
            var at = GameServices.Round.Lata.transform.position;
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Block, 2, 1, at);
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Tag, 0, 3, at);
            for (int i = 0; i < 3; i++) view.Tick(local, false, false, false, false);
        }

        private static IEnumerator Moment(MatchMomentKind kind, string tier)
        {
            Local.ClearStatuses(); GameServices.Round.ApplyNetworkTournamentState(0, new float[4]); View.Toast("", 0);
            var banner = Object.FindFirstObjectByType<MatchMomentBanner>();
            typeof(MatchMomentBanner).GetMethod("Hide", Priv).Invoke(banner, null);
            yield return null;
            typeof(MatchDirector).GetMethod("PresentHostMoment", Priv).Invoke(GameServices.Match, new object[] { Local.PlayerSlot, kind, 2, 50 });
            yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("match-moment-" + tier, "Earned moment / Tier " + tier, "Match");
            typeof(MatchMomentBanner).GetMethod("Hide", Priv).Invoke(banner, null);
        }

        private static IEnumerator Stamp(string id, string title, System.Action play, float at)
        {
            Quiet(); yield return null;
            play();
            yield return new WaitForSecondsRealtime(at);
            yield return Shot(id, title, "Match");
            yield return new WaitForSecondsRealtime(1.5f);
        }

        private static IEnumerator Powers(string id, string title, string group)
        {
            Quiet();
            var powers = View.GetComponent<TumpPowerReadout>();
            powers.OpenForCapture(Local.GetComponent<Abilities.HeroAbilitySystem>().Kit);
            yield return new WaitForSecondsRealtime(.4f);
            yield return Shot(id, title, group);
            powers.CloseCapture();
            yield return null;
        }

        private static IEnumerator Emote()
        {
            var wheel = Object.FindFirstObjectByType<EmoteWheel>();
            wheel.Open();
            for (int i = 0; i < 5; i++) yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            yield return Shot("match-emote", "Emote wheel", "Match");
            wheel.Close(play: false);
            yield return null;
        }

        private static IEnumerator UltimateCard()
        {
            var kit = Local.GetComponent<Abilities.HeroAbilitySystem>().Kit;
            var director = UltimatePresentationDirector.Instance;
            director.Show(Local, kit, kit.Ultimate);
            yield return null; yield return null;
            typeof(UltimatePresentationDirector).GetField("_clock", Priv).SetValue(director, .3f);
            yield return Shot("match-ultimate-card", "Ultimate card", "Match");
            yield return new WaitForSecondsRealtime(1.2f);
        }

        private static IEnumerator Pause(string id, string title, bool live, bool ended)
        {
            Quiet();
            var watcher = Object.FindFirstObjectByType<PauseWatcher>();
            var setEnded = typeof(MatchDirector).GetProperty("HasCompleted").GetSetMethod(true);
            if (ended) setEnded.Invoke(GameServices.Match, new object[] { true });
            if (live) SceneFlow.Networked = true;
            var pause = Panel.Open<PausePanel>(watcher); pause.Local = Local;
            yield return null;
            SceneFlow.Networked = false;
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot(id, title, "Match");
            pause.Close();
            if (ended) setEnded.Invoke(GameServices.Match, new object[] { false });
            yield return new WaitForSecondsRealtime(.3f);
        }

        private static IEnumerator Leave()
        {
            var pause = Panel.Open<PausePanel>(Object.FindFirstObjectByType<PauseWatcher>()); pause.Local = Local;
            yield return new WaitForSecondsRealtime(.4f);
            yield return Click("LeaveMatch", .5f);
            yield return Shot("match-pause-leave", "Pause / Leave the match?", "Match");
            yield return Click("StayInMatch", .3f);
            pause.Close();
            yield return new WaitForSecondsRealtime(.3f);
        }

        private static IEnumerator Break()
        {
            // The real intermission, as the end of round 1 shows it.
            var round = GameServices.Round; round.enabled = true;
            round.EndRound(); GameServices.Match.BeginIntermission();
            yield return Wait(() => GameObject.Find("OwnerRoundSwapCanvas") != null, 5, "the round break");
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("match-break", "Round break", "Match");
        }

        private static IEnumerator Halftime()
        {
            var round = GameServices.Round; var match = GameServices.Match;
            round.enabled = true;
            while (match.RoundNumber < 4) { round.EndRound(); match.AdvanceRound(); yield return null; }
            round.EndRound(); match.BeginIntermission();
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("match-halftime", "Halftime", "Match");
        }

        private static IEnumerator Result()
        {
            var result = Object.FindFirstObjectByType<MatchResult>();
            result.OnMatchWon(0);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("match-result-standings", "Result / Standings", "Match");
            Global("ResultTab1").onClick.Invoke(); yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("match-result-yourmatch", "Result / Your match", "Match");
            Global("ResultTab2").onClick.Invoke(); yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("match-result-players", "Result / Players", "Match");
            Global("ResultTab0").onClick.Invoke(); yield return null;
            ExecuteEvents.Execute(Global("ResultNextMap").gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("match-result-nextcourt", "Result / Next court", "Match");
        }

        private static IEnumerator SharedUltimate()
        {
            // The route SharedUltimatePhaseTests proves: a live round, Rago with a full meter.
            yield return MapRetrievalProbe.Load("Eskinita", GameMode.HeroStrike);
            Hud.Instance.ShowReadyPrompt(false);
            var actor = GameServices.Round.PlayerAt(1);
            const string hero = "sean";
            actor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
            var art = RosterBook.Load().People.First(p => p.Id == hero);
            actor.GetComponent<Visual.CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            actor.AbilitySystem.BindHero(hero); actor.AbilitySystem.Kit.AddUltimateCharge(actor.AbilitySystem.Kit.UltimateCost);
            actor.Intent.Clear(); actor.Intent.Parked = false;
            actor.Teleport(new Vector3(0, .18f, -5));
            foreach (var other in GameServices.Round.Players)
                if (other != actor) other.Teleport(new Vector3(6, other.transform.position.y, -6 + other.PlayerSlot * 3));
            Camera.main.GetComponent<CameraRig>().Follow(actor, true);
            SettingsStore.Current.CinematicCameraMotion = true; SettingsStore.Current.ReducedUiMotion = false;
            yield return Wait(() => Visual.UltimateIntroductionCache.Find(actor, actor.GetComponent<Carrier>().Held != null) != null, 6, "the ultimate rig");
            actor.Intent.Set(Verb.Ultimate, true); actor.Intent.BufferPress(Verb.Ultimate);
            yield return Wait(() => SharedUltimatePhase.BlocksActions, 6, "the shared ultimate");
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("match-ultimate-shared", "Shared ultimate", "Match");
            SharedUltimatePhase.Instance?.Cancel(); PresentationClock.RequestScale(1);
        }

        private static IEnumerator Catch()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita); yield return new WaitForSeconds(.2f);
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            Object.FindAnyObjectByType<SliceRunner>().Begin(); yield return null;
            var round = GameServices.Round;
            foreach (var actor in round.Players) { actor.Intent.Clear(); actor.ClearStun(); actor.ClearTrip(); }
            Hud.Instance.ShowReadyPrompt(false);
            yield return Wait(() => !round.Lata.IsProtected, 4, "an open can");
            var can = round.Lata.transform.position; var taya = round.PlayerAt(0); var victim = round.PlayerAt(1);
            victim.ClearStun(); victim.ClearTrip(); victim.Teleport(can + Vector3.back * 2.2f);
            taya.Teleport(can + Vector3.back * 3.2f); taya.transform.forward = Vector3.forward; victim.transform.forward = Vector3.forward;
            for (int i = 2; i < 4; i++) round.PlayerAt(i).Teleport(can + new Vector3(-5, 0, i * 2));
            SettingsStore.Current.ReducedUiMotion = false; SettingsStore.Current.CinematicCameraMotion = true;
            yield return new WaitForSeconds(CatchReconstruction.AnimationDuration + .1f);
            taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position, taya.transform.forward);
            yield return Wait(() => Object.FindAnyObjectByType<ReplayOverlay>() != null, 5, "the catch replay");
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("match-catch", "Catch replay", "Match");
            GameLaunch.SoloSeat = 0;
        }

        // ---------------------------------------------------------------- practice and spectator

        [UnityTest, Timeout(900000)]
        public IEnumerator PracticeAndSpectator()
        {
            yield return Step("match-training-menu", TrainingMenu());
            yield return PlayModeWorld.Reset();
            yield return Step("match-guided", Guided(false));
            yield return PlayModeWorld.Reset();
            yield return Step("match-spectator", Spectate(false));
            Report();
        }

        private static IEnumerator TrainingMenu()
        {
            yield return HubFlowTests.OpenHome();
            TumpHub.Current.Host.StartPractice();
            yield return Wait(() => SceneFlow.InMatch && PracticeRange.Active, 40, "the practice range");
            yield return new WaitForSecondsRealtime(4f);
            Panel.Open<PausePanel>(Object.FindFirstObjectByType<PauseWatcher>());
            yield return new WaitForSecondsRealtime(.7f);
            yield return Shot("match-training-menu", "Training menu", "Match");
        }

        private static IEnumerator Guided(bool touch)
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.GuidedTutorial = true; TouchHud.ForceVisible = touch;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return Wait(() => Object.FindFirstObjectByType<GuidedTrainingHud>() != null, 20, "guided training");
            yield return Wait(() => !ReadyGate.HidesHud, 30, "the training HUD");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot(touch ? "mobile-guided" : "match-guided", touch ? "Guided training (touch)" : "Guided training", touch ? "Mobile" : "Match");
            GameLaunch.GuidedTutorial = false; TouchHud.ForceVisible = false;
        }

        private static IEnumerator Spectate(bool touch)
        {
            GameLaunch.AllBots = true; TouchHud.ForceVisible = touch;
            SceneFlow.Networked = false; SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita); yield return new WaitForSeconds(.3f);
            Object.FindAnyObjectByType<SliceRunner>().Begin();
            // The instant replay needs its buffer filled first.
            yield return new WaitForSecondsRealtime(touch ? 6f : 16f);
            if (touch)
            {
                yield return Shot("mobile-spectator", "Spectator (touch)", "Mobile");
                yield return Click("SpectatorTouchTools", .5f);
                yield return Click("SpectatorTouchMatchTab", .5f);
                yield return Shot("mobile-spectator-drawer", "Spectator tools (touch)", "Mobile");
            }
            else
            {
                Hud.Instance.SetSpectatorControlsVisible(false);
                yield return new WaitForSecondsRealtime(.5f);
                yield return Shot("match-spectator", "Spectator HUD", "Match");
                Hud.Instance.SetSpectatorControlsVisible(true);
                yield return new WaitForSecondsRealtime(.5f);
                yield return Shot("match-spectator-controls", "Spectator controls", "Match");
                Hud.Instance.SetSpectatorControlsVisible(false);
                var watcher = Object.FindFirstObjectByType<SpectatorCamera>();
                SeedReplay(watcher);
                Assert.IsTrue(watcher.ExecuteViewCommand(SpectatorCamera.ViewCommand.Replay), "The instant replay did not start.");
                yield return new WaitForSecondsRealtime(1f);
                yield return Shot("match-spectator-replay", "Spectator instant replay", "Match",
                    "The replay ring was filled with one still of this live view: batch rendering never lands its asynchronous readbacks.");
            }
            GameLaunch.AllBots = false; TouchHud.ForceVisible = false;
        }

        /// <summary>
        /// Fills the spectator replay ring with sixty copies of the current view, the way
        /// SpectatorTouchUiTests seeds it, so the replay overlay can be photographed.
        /// </summary>
        private static void SeedReplay(SpectatorCamera watcher)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var before = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = before;
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var still = new Texture2D(960, 540, TextureFormat.RGB24, false);
            still.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); still.Apply();
            RenderTexture.active = previous; rt.Release(); Object.DestroyImmediate(rt);
            typeof(SpectatorCamera).GetMethod("CheckReplayMatch", Priv).Invoke(watcher, null);
            var ring = (IList)typeof(SpectatorCamera).GetField("_replayFrames", Priv).GetValue(watcher);
            var type = typeof(SpectatorCamera).GetNestedType("ReplayFrame", BindingFlags.NonPublic);
            for (int i = 0; i < 60; i++)
            {
                var frame = System.Activator.CreateInstance(type, true);
                type.GetField("Image").SetValue(frame, still); type.GetField("Aspect").SetValue(frame, 16f / 9);
                type.GetField("CapturedAt").SetValue(frame, Time.unscaledTime - 6f + i * .1f); type.GetField("Slot").SetValue(frame, -1);
                ring.Add(frame);
            }
        }

        // ---------------------------------------------------------------- mobile

        [UnityTest, Timeout(1200000)]
        public IEnumerator Mobile()
        {
            TouchHud.ForceVisible = true;
            yield return Match("Eskinita", GameMode.HeroStrike);
            yield return Step("mobile-hud", HudShot("mobile-hud", "Touch HUD / Hero Strike", Quiet));
            yield return Step("mobile-powers", Powers("mobile-powers", "Held skills sheet (phone)", "Mobile"));
            yield return PlayModeWorld.Reset();
            TouchHud.ForceVisible = true;
            yield return Match("Eskinita", GameMode.Classic);
            yield return Step("mobile-hud-classic", HudShot("mobile-hud-classic", "Touch HUD / Classic", Quiet));
            yield return PlayModeWorld.Reset();
            yield return Step("mobile-guided", Guided(true));
            yield return PlayModeWorld.Reset();
            yield return Step("mobile-spectator", Spectate(true));
            TouchHud.ForceVisible = false;
            Report();
        }
    }
}
