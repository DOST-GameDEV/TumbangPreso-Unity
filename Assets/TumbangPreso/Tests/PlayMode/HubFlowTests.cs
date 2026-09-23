using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// UX-1: HOME and every door on it, the owner's flow walked press by press, with captures of
    /// each screen at the brief's five shapes. `docs/TODO.md` UX-1.1 to UX-1.11.
    ///
    /// ⚠️ EVERY DOOR IS PRESSED THROUGH ITS BUTTON, NOT BY PUSHING A SCREEN, so a door whose listener
    /// is missing fails here (`docs/TODO.md` § 108: an EQUIP with no `onClick` that looked perfect).
    /// ⚠️ AND EVERY SCREEN IS LEFT THROUGH `TumpHub.Back`, which is what Escape, pad B and Android
    /// BACK reach through `ConvertedMatchSetup.Cancel`, so a screen that traps a pad fails here.
    /// </summary>
    public sealed class HubFlowTests
    {
        private bool _contrast, _larger;
        [UnitySetUp] public IEnumerator Before()
        {
            _contrast = Settings.SettingsStore.Current.HighContrastHud;
            _larger = Settings.SettingsStore.Current.LargerText;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            // A nested UnityTest enumerator can fail before its parent finally is disposed.
            var settings = Settings.SettingsStore.Current;
            settings.HighContrastHud = _contrast; settings.LargerText = _larger;
            Settings.SettingsStore.Save();
            Net.NetSession.Instance?.Stop(); HubQueueWatch.End(); SceneFlow.Networked = false;
            yield return PlayModeWorld.Reset();
        }

        /// <summary>The brief's shapes: 960x540, 1280x720, 1920x1080, 4:3 and the owner's window.</summary>
        internal static readonly Vector2Int[] Shapes =
        {
            new Vector2Int(960, 540), new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
            new Vector2Int(1280, 960), new Vector2Int(1600, 680),
        };

        internal static IEnumerator OpenHome()
        {
            // This helper means a fresh HOME. Room-preserving return is tested separately.
            Net.NetSession.Instance?.Stop(); HubQueueWatch.End();
            yield return new WaitForSecondsRealtime(.4f);
            SceneFlow.Networked = false;
            SceneFlow.GoHome();
            float until = Time.realtimeSinceStartup + 20;
            while (Time.realtimeSinceStartup < until && (TumpHub.Current == null || !(TumpHub.Current.Top is HubHome))) yield return null;
            Assert.IsNotNull(TumpHub.Current, "TAP TO START's destination did not build the hub.");
            Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
            // Let the court's first frame land and the stickers settle.
            until = Time.realtimeSinceStartup + 12;
            var preview = TumpHub.Current.Host.Preview;
            while (Time.realtimeSinceStartup < until && preview != null && preview.GetComponent<RawImage>().texture == null) yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
        }

        internal static IEnumerator Press(string name)
        {
            var hub = TumpHub.Current;
            Button button = null;
            float until = Time.realtimeSinceStartup + 5;
            while (Time.realtimeSinceStartup < until)
            {
                button = hub.Canvas.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == name && b.IsInteractable());
                if (button != null) break;
                yield return null;
            }
            Assert.IsNotNull(button, "No pressable '" + name + "' on " + hub.Top?.GetType().Name);
            button.onClick.Invoke();
            yield return null;
            yield return new WaitForSecondsRealtime(0.45f);
        }

        internal static IEnumerator Shots(string name)
        {
            var hub = TumpHub.Current;
            var settings = Settings.SettingsStore.Current;
            string prefix = settings.HighContrastHud && settings.LargerText ? "Hub-A11y-" : "Hub-";
            foreach (var size in Shapes)
                yield return TumpUiCapture.Capture(prefix + name + "-" + size.x + "x" + size.y, hub.Canvas, size.x, size.y,
                                                   checkPalette: false, checkActionBounds: true);
            AssertFloor(hub.Canvas, name);
        }

        /// <summary>⚠️ THE BRIEF'S FLOOR: every label on a hub screen is 28 canvas units or more.</summary>
        internal static void AssertFloor(Canvas canvas, string where)
        {
            foreach (var text in canvas.GetComponentsInChildren<Text>())
            {
                if (!text.enabled || string.IsNullOrWhiteSpace(text.text)) continue;
                if (text.GetComponentInParent<LobbyChat>() != null) continue;   // the chat is its own surface
                Assert.GreaterOrEqual(text.fontSize, HubStyle.Floor, where + "/" + text.name + " is under the 28-unit floor.");
            }
        }

        private static void Back()
        {
            TumpHub.Current.Back();
        }

        private static IEnumerator BackToHome()
        {
            for (int i = 0; i < 6 && !(TumpHub.Current.Top is HubHome); i++) { Back(); yield return new WaitForSecondsRealtime(0.2f); }
            Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top, "BACK did not lead home.");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator HomeAndEveryDoorOpensItsScreenAndBackReturns()
        {
            int choice = TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice;
            try
            {
                yield return OpenHome();
                yield return Shots("Home");

                yield return Press("AvatarButton");
                Assert.IsInstanceOf<HubAvatar>(TumpHub.Current.Top);
                yield return Shots("Avatar");
                yield return BackToHome();

                yield return Press("SkillTreeButton");
                Assert.IsInstanceOf<HubSkillTree>(TumpHub.Current.Top);
                yield return Shots("SkillTree");
                yield return BackToHome();

                yield return Press("HeroButton");
                Assert.IsInstanceOf<HubHero>(TumpHub.Current.Top);
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Shots("Hero");
                yield return Press("Ability0");
                Assert.IsInstanceOf<HubAbilityPopup>(TumpHub.Current.Top);
                yield return Shots("AbilityDetail");
                Back(); yield return null;
                yield return Press("NextHero");
                yield return BackToHome();

                yield return Press("LoadoutButton");
                Assert.IsInstanceOf<HubLoadout>(TumpHub.Current.Top);
                yield return Shots("Loadout");
                yield return Press("UnownedTab");
                yield return Shots("Loadout-unowned");
                yield return Press("OwnedTab");
                yield return Press("LataTab");
                var tile = TumpHub.Current.Canvas.GetComponentsInChildren<Button>().First(b => b.name.StartsWith("Item_"));
                tile.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.8f);
                Assert.IsInstanceOf<HubItemPopup>(TumpHub.Current.Top);
                yield return Shots("ItemPopup");
                yield return Press("InspectButton");
                yield return Shots("ItemPopup-inspect");
                Back(); yield return null;   // leaves inspect first, innermost layer
                Assert.IsInstanceOf<HubItemPopup>(TumpHub.Current.Top, "BACK must leave inspect before the popup.");
                yield return BackToHome();

                yield return Press("ShopButton");
                Assert.IsInstanceOf<HubShopPopup>(TumpHub.Current.Top);
                yield return Shots("Shop");
                yield return Press("HeroShopDoor");
                Assert.IsInstanceOf<HubHero>(TumpHub.Current.Top);
                yield return BackToHome();

                yield return Press("TaskButton");
                Assert.IsInstanceOf<HubTasks>(TumpHub.Current.Top);
                yield return Shots("Tasks");
                yield return BackToHome();

                yield return Press("EarnButton");
                Assert.IsInstanceOf<HubTasks>(TumpHub.Current.Top, "The + beside the balance opens TASKS, never a store of currency.");
                yield return BackToHome();

                yield return Press("MenuButton");
                Assert.IsInstanceOf<HubMenu>(TumpHub.Current.Top);
                yield return Shots("Menu");
                yield return BackToHome();

                yield return Press("NamePlate");
                Assert.IsTrue(Object.FindFirstObjectByType<PlayerHub>().IsOpen, "The name plate is the door to profile settings.");
                Assert.IsFalse(TumpHub.Current.Canvas.enabled, "The hub steps aside for the profile screen.");
                yield return Press_Global("ClosePlayerHub");
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.IsTrue(TumpHub.Current.Canvas.enabled);
            }
            finally
            {
                TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice = choice;
            }
        }

        private static IEnumerator Press_Global(string name)
        {
            Button button = null;
            float until = Time.realtimeSinceStartup + 5;
            while (Time.realtimeSinceStartup < until && button == null)
            {
                button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && b.isActiveAndEnabled);
                yield return null;
            }
            Assert.IsNotNull(button, "No " + name + " on screen.");
            button.onClick.Invoke();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator GamemodeSelectSetsTheModeCardAndOpensTheCustomFlow()
        {
            int choice = TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice;
            try
            {
                yield return OpenHome();
                yield return Press("ModeCard");
                Assert.IsInstanceOf<HubModeSelect>(TumpHub.Current.Top);
                yield return Shots("GameModes");

                // Hover and focus reveal a card's description (the owner's darkened CLASSIC card).
                var classic = TumpHub.Current.Canvas.GetComponentsInChildren<HubButton>().First(b => b.name == "ClassicCard");
                classic.Select();
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.IsTrue(classic.transform.GetComponentsInChildren<Text>().Any(t => t.name == "Description" && t.isActiveAndEnabled));
                yield return Shots("GameModes-focus");

                // RANKED is always Hero Strike and returns HOME.
                yield return Press("RankedCard");
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
                Assert.AreEqual(0, HubHome.Choice);
                Assert.AreEqual(GameMode.HeroStrike, SceneFlow.SelectedMode);

                // CLASSIC asks which game, then returns HOME with that on the card.
                yield return Press("ModeCard");
                yield return Press("ClassicCard");
                Assert.IsInstanceOf<HubClassicPopup>(TumpHub.Current.Top);
                yield return Shots("ClassicPopup");
                yield return Press("ClassicChoice");
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
                Assert.AreEqual(1, HubHome.Choice);
                Assert.AreEqual(GameMode.Classic, SceneFlow.SelectedMode);
                Assert.IsTrue(TumpHub.Current.Canvas.GetComponentsInChildren<Text>().Any(t => t.name == "ModeSubtitle" && t.text == "CLASSIC"));

                // CUSTOM is a popup with HOST and JOIN, and each leads to its screen.
                yield return Press("ModeCard");
                yield return Press("CustomCard");
                Assert.IsInstanceOf<HubCustomPopup>(TumpHub.Current.Top);
                yield return Shots("CustomPopup");
                yield return Press("HostDoor");
                Assert.IsInstanceOf<HubHost>(TumpHub.Current.Top);
                yield return Shots("Host");
                yield return Press("MapDropdown");
                Assert.IsInstanceOf<HubChoicePopup>(TumpHub.Current.Top);
                yield return Shots("Host-map-list");
                yield return Press("Option1");
                Assert.AreEqual(SceneFlow.MapRegistry[1].Id, SceneFlow.SelectedMap, "The map choice moves the court behind the form.");
                Back(); yield return null;
                Assert.IsInstanceOf<HubModeSelect>(TumpHub.Current.Top, "BACK from HOST returns to GAMEMODE SELECT.");
                yield return Press("CustomCard");
                yield return Press("JoinDoor");
                Assert.IsInstanceOf<HubJoin>(TumpHub.Current.Top);
                yield return Shots("Join-online");
                yield return Press("Source1");
                yield return Shots("Join-lan");
                yield return Press("Source2");
                yield return Shots("Join-code");
                yield return BackToHome();
            }
            finally
            {
                TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice = choice;
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator QueuePlateMatchFoundCharacterSelectLobbyAndLoadingAreDrawn()
        {
            int choice = TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice;
            try
            {
                yield return OpenHome();
                HubHome.Choice = 2;
                yield return Press("PlayButton");

                // Offline the relay host cannot open, and the queue keeps searching: that is the plate.
                float until = Time.realtimeSinceStartup + 3;
                while (Time.realtimeSinceStartup < until && !HubQueueWatch.QueueRoom) yield return null;
                Assert.IsTrue(HubQueueWatch.QueueRoom, "PLAY did not start the queue.");
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.IsTrue(TumpHub.Current.Canvas.GetComponentsInChildren<Button>().Any(b => b.name == "CancelQueue"),
                              "The queue plate with its X is on screen while queued.");
                yield return Shots("Home-queued");
                yield return Press("CancelQueue");
                Assert.IsFalse(HubQueueWatch.QueueRoom, "X did not leave the queue.");
                Assert.IsFalse(Net.NetSession.Instance != null && Net.NetSession.Instance.IsNetworked, "Cancelling must close the queue's room.");

                TumpHub.Current.Push<HubMatchFound>();
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shots("MatchFound");
                until = Time.realtimeSinceStartup + 4;
                while (Time.realtimeSinceStartup < until && !(TumpHub.Current.Top is HubCharacterSelect)) yield return null;
                Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top, "MATCH FOUND advances to CHARACTER SELECT.");
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Shots("CharacterSelect");
                TumpHub.Current.Home();

                // A LAN room, hosted for real, and its lobby.
                var task = TumpHub.Current.Host.HostRoom("TEST ROOM", SceneFlow.Eskinita, GameMode.HeroStrike, RoomVisibility.Public, false);
                until = Time.realtimeSinceStartup + 15;
                while (Time.realtimeSinceStartup < until && !task.IsCompleted) yield return null;
                Assert.IsTrue(task.IsCompleted && string.IsNullOrEmpty(task.Result), "Hosting a LAN room failed: " + (task.IsCompleted ? task.Result : "timeout"));
                until = Time.realtimeSinceStartup + 5;
                while (Time.realtimeSinceStartup < until && !(TumpHub.Current.Top is HubLobby)) yield return null;
                Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top,
                    "A room opened outside the hub's buttons must automatically present LOBBY.");
                var lobby = TumpHub.Current.Top;
                TumpHub.Current.ShowLobby();
                Assert.AreSame(lobby, TumpHub.Current.Top, "Repeated room completion must reuse the lobby.");
                yield return new WaitForSecondsRealtime(1.0f);
                yield return Shots("Lobby");
                Assert.IsTrue(TumpHub.Current.Host.Seats().Any(s => s.Mine && s.Host), "The host's own seat carries the host mark.");
                yield return Press("CharacterDoor");
                Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top);
                TumpHub.Current.ShowLobby();
                yield return null;
                Assert.IsInstanceOf<HubCharacterSelect>(TumpHub.Current.Top,
                    "Repeated room observations must not dismiss a lobby's character subpage.");
                Assert.AreEqual(1, TumpHub.Current.Canvas.GetComponentsInChildren<HubLobby>(true).Length);
                yield return Shots("CharacterSelect-lobby");
                Back(); yield return null;
                Assert.IsInstanceOf<HubLobby>(TumpHub.Current.Top);
                Back(); yield return null;
                Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
                Assert.IsFalse(Net.NetSession.Instance.IsNetworked, "BACK from the lobby leaves the room.");

                // The loading curtain, drawn over the court without loading anything.
                HubLoading.Begin(SceneFlow.Eskinita, networked: true);
                yield return new WaitForSecondsRealtime(0.3f);
                var loading = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.name == "TumpLoadingCanvas");
                var settings = Settings.SettingsStore.Current;
                string prefix = settings.HighContrastHud && settings.LargerText ? "Hub-A11y-Loading-" : "Hub-Loading-";
                foreach (var size in Shapes)
                    yield return TumpUiCapture.Capture(prefix + size.x + "x" + size.y, loading, size.x, size.y,
                                                       checkPalette: false, checkActionBounds: true);
                AssertFloor(loading, "Loading");
                Object.Destroy(Object.FindFirstObjectByType<HubLoading>().gameObject);
            }
            finally
            {
                TumbangPreso.Settings.SettingsStore.Current.HubQueueChoice = choice;
                Net.NetSession.Instance?.Stop();
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator HighContrastAndLargerTextKeepEveryDoorAndLobbyReadable()
        {
            var settings = Settings.SettingsStore.Current;
            bool contrast = settings.HighContrastHud, larger = settings.LargerText;
            try
            {
                settings.HighContrastHud = true;
                settings.LargerText = true;
                // Walk the same public routes, with the same bounds and 28-unit assertions.
                // Distinct capture names retain both settings together without replacing normal evidence.
                yield return HomeAndEveryDoorOpensItsScreenAndBackReturns();
                yield return GamemodeSelectSetsTheModeCardAndOpensTheCustomFlow();
                yield return QueuePlateMatchFoundCharacterSelectLobbyAndLoadingAreDrawn();
            }
            finally
            {
                Settings.SettingsStore.Current.HighContrastHud = contrast;
                Settings.SettingsStore.Current.LargerText = larger;
                Settings.SettingsStore.Save();
            }
        }
    }
}
