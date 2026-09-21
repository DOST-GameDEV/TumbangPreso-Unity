using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    // Walk the painted screens through their actual owners. Only visible controls
    // count. Explicit conditional controls are taken only from current owners;
    // their presence is not a claim that a service/device state was exercised.
    internal static class FrontEndControlWalk
    {
        internal static IEnumerator Capture(SortedSet<string> found)
        {
            var joystick = InputSystem.AddDevice<Joystick>("TumpInventoryUnrecognisedController");
            try { yield return CaptureScreens(found); }
            finally { if (joystick.added) InputSystem.RemoveDevice(joystick); }
        }

        private static IEnumerator CaptureScreens(SortedSet<string> found)
        {
            yield return PaintedScreens.OpenSettings();
            var view = Object.FindFirstObjectByType<TumpSettingsView>();
            for (int section = 0; section < TumpSettingsView.Sections.Length; section++)
            {
                yield return Press("SettingsSection" + section);
                Record("settings", found);
            }
            yield return Press("SettingsSection0");
            yield return Press("InputDeviceValue"); Record("settings-options", found);
            yield return Press("Option0");
            for (int device = 0; device < 2; device++)
            {
                yield return Choose("InputDeviceValue", device);
                for (int group = 0; group < Rebinding.Groups.Length; group++)
                {
                    yield return Choose("BindingGroupValue", group);
                    Record("settings", found);
                    foreach (string action in Rebinding.Groups[group].Actions)
                        Assert.IsNotNull(Button("Binding_" + action + "Action"), action);
                }
            }
            yield return Press("ControllerMapAction");
            Record("controller", found);
            yield return Press("Done");
            yield return Choose("InputDeviceValue", 2);
            Record("settings", found);
            yield return Press("TouchLayoutAction");
            Record("touch", found);
            yield return Press("TouchAdjustments");
            Record("touch", found);
            yield return Press("CancelTouchLayout");
            yield return Press("SettingsCredits");
            Record("credits", found);
            yield return Press("CreditsBack");
            view.Session.Discard();
            yield return Press("TumpSettingsBack");

            var owner = Object.FindFirstObjectByType<ConvertedMainMenu>();
            var signIn = owner.GetComponent<SignInScreen>();
            Assert.IsNotNull(signIn);
            signIn.Install();
            signIn.Open(); yield return Settle(); Record("login", found);
            yield return Press("CreateAccountTab"); Record("login", found);
            yield return Press("SignInTab"); Record("login", found);
            // Returning-account controls are constructed by the current form.
            // Inventory their identity without inventing an authenticated account.
            var loginCanvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(c => c.name == "OwnerSignInCanvas");
            RecordConditional("welcome-conditional", loginCanvas.GetComponentsInChildren<Button>(true), found);
            yield return Press("SignInBack");
            yield return Press("StartButton"); Record("play", found);

            // No host/connect/submit is invoked by an inventory. Online services
            // and physical-device behavior have their own qualification routes.
            SceneFlow.SelectedMode = GameMode.HeroStrike;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            SceneFlow.Networked = true;
            PlaySelectionScreen.RequestedLobbyMode = LobbyMode.Custom;
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
            yield return new WaitForSecondsRealtime(.7f);
            Record("lobby", found);
            var preparation = Object.FindFirstObjectByType<OwnerPreparationView>();
            Assert.IsNotNull(preparation);
            RecordConditional("ready-conditional", new[] { preparation.Primary }, found);
            var queue = Object.FindFirstObjectByType<QueueCard>(FindObjectsInactive.Include);
            Assert.IsNotNull(queue);
            RecordConditional("queue-conditional", queue.GetComponentsInChildren<Button>(true), found);
            yield return Press("ChatButton"); Record("lobby", found);
            yield return Press("ChatButton");
            yield return Press("CustomGameButton"); Record("rules", found);
            yield return Press("RoomRulesTab"); Record("rules", found);
            yield return Press("CustomRulesBack");
            yield return Press("JoinRoomButton"); Record("join", found);
            yield return Press("OnlineChip"); Record("join", found);
            yield return Press("NearbyChip"); Record("join", found);
            yield return Press("CloseJoinButton");
            yield return Press("LoadoutButton");
            for (int category = 0; category < 3; category++)
            {
                yield return Press("TumpCategory" + category);
                Record("character", found);
            }
            yield return Press("TumpCategory0");
            yield return Press("TumpSkills");
            foreach (int slot in new[] { 1, 2, 0 })
            {
                yield return Press("TumpSkillSlot" + slot);
                Record("skills", found);
            }
            yield return Press("TumpSkillBack");
            yield return Press("TumpBack");
            yield return Press("ProfileButton");
            foreach (string tab in new[] { "Profile", "Friends", "Career", "Matches", "Account" })
            {
                yield return Press("HubTab" + tab);
                Record("profile", found);
                // Open display-only collapsed groups; never press their actions.
                var titles = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Where(b => b.name == "ToggleGroup" && b.isActiveAndEnabled)
                    .Select(b => b.GetComponentInChildren<Text>().text)
                    .Where(t => t.StartsWith("+" )).ToArray();
                foreach (string title in titles)
                {
                    var group = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                        .FirstOrDefault(b => b.name == "ToggleGroup" && b.isActiveAndEnabled
                            && b.GetComponentInChildren<Text>().text == title);
                    Assert.IsNotNull(group, title);
                    group.onClick.Invoke(); yield return Settle(); Record("profile", found);
                }
            }
        }

        internal static Button Button(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.name == name && b.isActiveAndEnabled);

        internal static void VerifyMigration(string[] baseline, SortedSet<string> found)
        {
            const string path = "Assets/TumbangPreso/Tests/PlayMode/control-inventory-migration.tsv";
            var migrations = System.IO.File.ReadAllLines(path).Where(l => !l.StartsWith("#") && l.Length > 0)
                .Select(l => l.Split('\t')).ToDictionary(p => p[0] + ":" + p[1]);
            var live = new HashSet<string>(found.Select(l => l.Split('\t')).Select(p => p[1] + ":" + p[2]));
            var expected = new HashSet<string>();
            var failures = new List<string>();
            var receipt = new List<string> { "legacy row\tcurrent targets\tdisposition\treason" };
            foreach (string row in baseline)
            {
                var parts = row.Split('\t');
                string key = parts[1] + ":" + parts[2]; expected.Add(key);
                if (!migrations.TryGetValue(key, out var migration))
                { failures.Add("Unaccounted original control: " + key); continue; }
                Assert.AreEqual(4, migration.Length, key + " needs targets and a reason");
                Assert.IsNotEmpty(migration[3], key + " needs a migration explanation");
                string status = "retained";
                if (migration[2] == "@owner-removed")
                {
                    Assert.That(key, Is.EqualTo("button:CustomDoor").Or.EqualTo("button:QuitButton"),
                        "A new removal needs an explicit owner decision, not a baseline reset.");
                    status = "owner removed button; stated replacement/decision";
                }
                else foreach (string target in migration[2].Split(';'))
                    if (!live.Contains(target)) { failures.Add(key + " -> missing " + target); status = "missing"; }
                receipt.Add(row.Replace('\t', '|') + "\t" + migration[2] + "\t" + status + "\t" + migration[3]);
            }
            CollectionAssert.AreEquivalent(expected, migrations.Keys,
                "Every historical identity needs exactly one reviewed disposition; no stale migration rows.");
            System.IO.File.WriteAllLines("Logs/control-migration-results.tsv", receipt);
            Assert.IsEmpty(failures, "Original capabilities missing from current screen routes:\n" + string.Join("\n", failures));
        }

        private static IEnumerator Press(string name)
        {
            var button = Button(name);
            Assert.IsNotNull(button, "Inventory route is missing " + name);
            Assert.IsTrue(button.interactable, "Inventory route is disabled: " + name);
            button.onClick.Invoke(); yield return Settle();
        }

        private static IEnumerator Choose(string name, int index)
        { yield return Press(name); yield return Press("Option" + index); }

        private static IEnumerator Settle()
        { yield return null; yield return null; Canvas.ForceUpdateCanvases(); }

        private static void RecordConditional(string screen, IEnumerable<Button> controls, SortedSet<string> into)
        {
            foreach (var button in controls)
            {
                if (button == null) continue;
                into.Add(screen + "\tbutton\t" + button.name + "\tconditional built control");
            }
        }

        private static void Record(string screen, SortedSet<string> into)
        {
            foreach (var control in Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None))
            {
                if (!control.gameObject.activeInHierarchy) continue;
                var canvas = control.GetComponentInParent<Canvas>();
                if (canvas == null || !canvas.enabled) continue;
                string kind = control is Dropdown ? "dropdown" : control is InputField ? "field"
                    : control is Slider ? "slider" : control is Toggle ? "toggle"
                    : control is Scrollbar ? "scrollbar" : control is Button ? "button" : null;
                if (kind == null) continue;
                string says = kind == "field" ? "" : control.GetComponentsInChildren<Text>()
                    .Select(t => t.text.Trim().Replace("\n", " ")).FirstOrDefault(t => t.Length > 0) ?? "";
                into.Add(screen + "\t" + kind + "\t" + control.name + "\t" + says);
                if (control.name == "ToggleGroup")
                    into.Add(screen + "\t" + kind + "\t" + control.transform.parent.name + "/" + control.name + "\t" + says);
            }
        }
    }
}
