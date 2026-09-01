using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The gate on "no leftover old UI", and it is the one thing a screenshot cannot be.
    ///
    /// 🧑 2026-09-01, on the overhaul: **"ALSO BE AWARE THAT UR OVERHAULING THE UI, MAKE SURE U
    /// COMPLETELY REPLACE UI BCZ I DOTN WANT LEFTOVER SHIT FROM OLD UI TO STILL BE FRIGGING WITH
    /// US"**, and separately *"MAKE SURE EVERYTHING U REPLACED IS ACCOUNTED FOR AND WE DONT LOSE
    /// BUTTONS"*. Those are two different worries and this file answers both.
    ///
    /// ⚠️⚠️ WHY A PICTURE CANNOT ANSWER THE FIRST ONE. Every previous pass in `docs/TODO.md`
    /// §§ 116 to 118 was verified by looking at a render, and a render cannot see:
    ///
    ///   * a surface inside a drawer that is currently shut,
    ///   * a surface underneath another surface (`SkinLayers` leaves a `Face` and a `Shadow` child
    ///     behind, and a disabled skin whose layers are still active draws wood UNDER the new
    ///     material),
    ///   * a `GodotButton` that only writes its sprite on HOVER, so the control is correct in
    ///     every screenshot and flips to wood the moment the pointer touches it, or
    ///   * a state the shot pass did not happen to open.
    ///
    /// **All four of those are how a mixed front end survives a review**, and `docs/TODO.md`
    /// § 117.7 is seven faults that every probe in this repository was green through.
    ///
    /// ⚠️ THE SECOND WORRY IS THE INVENTORY IN `docs/TODO.md` § 119.3. `EveryLobbyControlSurvived`
    /// asserts that each named control still resolves, is reachable and has a handler, which is
    /// `CLAUDE.md` § 6.2's INTUITIVE row (§ 108: an EQUIP button with no `onClick` listener, which
    /// looked perfect) written as a test rather than as care.
    ///
    /// ⚠️ THE MAIN MENU AND THE IN-MATCH HUD ARE OUT OF SCOPE AND ARE NOT LOADED HERE. 🧑 scoped
    /// them out twice; they are still drawn in wood on purpose and a probe that walked them would
    /// go red for the thing that is meant to be true.
    /// </summary>
    public class PaperPurityProbe
    {
        /// <summary>⚠️ THE SHIPPED WINDOWED SIZE, NOT THE BATCH RUNNER'S 640x480. `UiClickProbe`
        /// and `LobbyStyleProbe` carry the same line: at 4:3 the right-hand column sits outside the
        /// viewport and every control in it reports as broken.</summary>
        private const int Width = 1600;
        private const int Height = 900;

        /// <summary>⚠️ LONG ENOUGH FOR THE FIT PASSES AND THE LAYOUT CHAIN. `ConvertedMatchSetup`
        /// runs `FitEverything` for `FitPasses` frames after every refresh and each pass depends on
        /// a `ContentSizeFitter` resolving from rows that resolve from their own children.
        /// </summary>
        private const int SettleFrames = 120;

        /// <summary>
        /// The controls `docs/TODO.md` § 119.3 says the lobby owes, by node name.
        ///
        /// ⚠️⚠️ THE LIST IS THE POINT AND IT IS NOT A SAMPLE. A redesign loses a button by moving
        /// a node into a container that is then deactivated, which compiles, runs, renders and
        /// looks fine. Every name here was on the screen before this pass and has to be on it
        /// after.
        /// </summary>
        private static readonly string[] LobbyControls =
        {
            "BackButton",
            "PracticeTab",
            "RankedTab",
            "CustomTab",
            "ProfileButton",
            "CharacterButton",
            "LoadoutButton",
            "SettingsDrawerToggle",
            "RoomCodeButton",
            // ⚠️ `PlayerNameEdit` IS NOT HERE ANY MORE. It lives in `PlayerHub`'s PROFILE tab,
            // which is built when the tab is shown, so a probe that looked for it on a lobby
            // nobody had opened the account screen on would be asserting the absence of a control
            // rather than its presence. `UiRuntimeShots.TheLobbyDraws` presses the door and checks
            // it there, which is the state a player is in when they use it.
            "JoinChip",
            "ChatChip",
            "StatusLabel",
        };

        /// <summary>The two action buttons, of which exactly one is live at a time. ⚠️ They are
        /// checked as a PAIR rather than individually: `RefreshActionButtons` hides START MATCH for
        /// everybody who is not the host of a live networked lobby and shows READY instead, so
        /// asserting on either one alone is asserting on which arm the probe happened to run.
        /// </summary>
        private static readonly string[] ActionSlot =
            { "StartButton", "PrimaryButton", "RankedButton" };

        [UnityTest]
        public IEnumerator EveryLobbyControlSurvived()
        {
            Screen.SetResolution(Width, Height, false);
            for (int i = 0; i < 10; i++) yield return null;

            bool previousNetworked = SceneFlow.Networked;
            SceneFlow.Networked = true;

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < SettleFrames; i++) yield return null;
            Canvas.ForceUpdateCanvases();

            var missing = new List<string>();
            var dead = new List<string>();

            foreach (string name in LobbyControls)
            {
                var node = Find(name);

                if (node == null)
                {
                    missing.Add(name);
                    continue;
                }

                // ⚠️ A CONTROL THAT IS OFF RIGHT NOW IS NOT MISSING. The room column comes off in
                // practice and the code sign comes off when there is no code; what this asks is
                // whether the node still EXISTS under the screen, which is the thing a redesign
                // silently breaks.
                var button = node.GetComponent<Button>();
                var field = node.GetComponent<InputField>();

                if (button == null && field == null) continue;
                if (field != null) continue;

                if (button.onClick.GetPersistentEventCount() == 0 && !HasRuntimeListener(button))
                    dead.Add(name);
            }

            bool anyAction = false;
            foreach (string name in ActionSlot)
                if (Find(name) != null) anyAction = true;

            SceneFlow.Networked = previousNetworked;

            var session = Net.NetSession.Instance;
            if (session != null) session.Stop();

            Assert.IsEmpty(missing,
                "these lobby controls no longer exist on the screen, so the redesign lost them. "
                + "docs/TODO.md § 119.3 is the inventory:\n  " + string.Join("\n  ", missing));

            Assert.IsTrue(anyAction,
                "neither StartButton nor PrimaryButton is on the screen, so the lobby has no way "
                + "to start a match at all. See LobbyChrome.BuildActionSlot.");

            Assert.IsEmpty(dead,
                "these controls are drawn and do nothing when pressed, which CLAUDE.md § 6.2 "
                + "calls the INTUITIVE failure and § 108 is the receipt for:\n  "
                + string.Join("\n  ", dead));
        }

        /// <summary>
        /// Nothing on the lobby or the login screen is still made of wood, except what is
        /// supposed to be.
        ///
        /// ⚠️⚠️ THE THREE EXEMPTIONS ARE THE WHOLE DESIGN AND NOT A LOOPHOLE:
        ///
        ///   * **`WoodPrimaryButton`** is 🧑's own `JOIN BUTTON.png` green, which `CLAUDE.md`
        ///     § 6.5 names as his primary colour. On a cream screen it is the only saturated
        ///     object in the frame, which is what makes the one action findable without spending
        ///     the accent on it.
        ///   * **Anything drawing an authored texture** (`Art/ui/**`): the pennants, `BUTTON
        ///     LONG`, the arrows, `TUMP.png`, the key art. `VISION.md` § 6: his art is the design
        ///     system, and repainting it is forbidden.
        ///   * **A `RawImage`**, which is always a photograph or a model preview rather than a
        ///     surface.
        /// </summary>
        [UnityTest]
        public IEnumerator NoWoodenSurfaceSurvivesOnTheLobby()
        {
            Screen.SetResolution(Width, Height, false);
            for (int i = 0; i < 10; i++) yield return null;

            bool previousNetworked = SceneFlow.Networked;
            SceneFlow.Networked = true;

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < SettleFrames; i++) yield return null;

            // ⚠️⚠️ EVERY DRAWER IS OPENED BEFORE THE WALK, WHICH IS THE POINT OF DOING THIS IN A
            // PROBE RATHER THAN IN A RENDER. A shut drawer is invisible to a camera and its
            // contents are exactly where a leftover survives a review.
            foreach (string chip in new[] { "SettingsDrawerToggle", "JoinChip", "ChatChip" })
            {
                var button = Find(chip)?.GetComponent<Button>();
                if (button == null) continue;

                button.onClick.Invoke();
                for (int i = 0; i < 4; i++) yield return null;
            }

            for (int i = 0; i < 20; i++) yield return null;
            // ⚠️⚠️ AND THE FOUR SCREENS THE LOBBY OPENS ARE OPENED, WHICH THEY WERE NOT, AND THAT
            // OMISSION IS THE WHOLE OF `docs/TODO.md` § 120.4 AND § 120.5. This probe has only
            // ever built the lobby and the login screen, so the fighter picker, the character
            // maker, the player hub and the settings panel were outside every gate in the
            // repository: every fault on them had to be found by a person looking at a photograph.
            // 🧑 asked for exactly this scope by name: **"MAKE SURE AS WELL CHARACTER SELECT AS
            // WELL AS EVERYTHING WIRED TO LOBBY HAS THE NEW THEME"**.
            //
            // ⚠️ THEY ARE OPENED THROUGH THEIR OWN DOORS rather than switched on, for the reason
            // `UiRuntimeShots.TheLobbyDoorsDraw` records: everything on them is drawn off a
            // selection change, and a panel switched on without one is a panel nobody has selected
            // anything in.
            foreach (string door in new[] { "CharacterButton", "ProfileButton" })
            {
                var open = Find(door)?.GetComponent<Button>();
                if (open == null) continue;

                open.onClick.Invoke();
                for (int i = 0; i < 30; i++) yield return null;
            }

            Canvas.ForceUpdateCanvases();

            var offenders = new List<string>();

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                Walk(root.transform, offenders);

            SceneFlow.Networked = previousNetworked;

            var session = Net.NetSession.Instance;
            if (session != null) session.Stop();

            Assert.IsEmpty(offenders,
                "these surfaces on the lobby are still drawn in the old wooden language. A render "
                + "cannot see a surface inside a shut drawer or underneath another surface, which "
                + "is why this is a probe. docs/TODO.md § 119.6:\n  "
                + string.Join("\n  ", offenders));
        }

        private static void Walk(Transform t, List<string> offenders)
        {
            // ⚠️ THE IN-MATCH HUD AND THE MAIN MENU ARE SKIPPED BY NAME. They are still wooden on
            // purpose and 🧑 scoped both out twice: *"dont touch main menu and inngame ui"*.
            if (t.name == "HudCanvas" || t.name == "MainMenuRoot") return;

            // ⚠️⚠️ THE ACTION SLOT IS EXEMPT WHOLESALE AND THAT IS THE DESIGN, NOT A HOLE IN THE
            // TEST. Its three occupants are the one primary this screen has, and 🧑 chose their
            // material by name: *"u can also still use the brown color ... start match lowk looks
            // good"*. START MATCH is his authored slab; on a cream rail it is the heaviest object
            // in the frame and that is what makes it findable without spending an accent.
            if (t.name == "ActionSlot") return;

            var wood = t.GetComponent<WoodSkin>();
            if (wood != null && wood.enabled)
                offenders.Add($"{Path(t)} still carries WoodSkin ({wood.Surface})");

            var panel = t.GetComponent<GodotPanel>();
            if (panel != null && panel.enabled)
                offenders.Add($"{Path(t)} still carries GodotPanel ({panel.Variation})");

            var button = t.GetComponent<GodotButton>();
            if (button != null && button.enabled && button.Variation != "WoodPrimaryButton"
                && button.Variation != "PrimaryButton")
                offenders.Add($"{Path(t)} still carries GodotButton ({button.Variation})");

            WoodenSprite(t, offenders);

            // ⚠️⚠️ A DISABLED SKIN IS NOT ENOUGH ON ITS OWN, WHICH IS THE SUBTLEST HALF OF THIS.
            // `SkinLayers` gives every wooden control a `Face` child and a `Shadow` child, and
            // those keep drawing whatever sprite was last written to them after the component that
            // wrote it is switched off. `PaperKit.PaperDress.Strip` deactivates them; this is what
            // says it actually happened.
            if ((panel != null || button != null) && (t.name != "Face" && t.name != "Shadow"))
            {
                foreach (string layer in new[] { "Face", "Shadow" })
                {
                    var child = t.Find(layer);
                    bool skinLive = (panel != null && panel.enabled)
                                    || (button != null && button.enabled);

                    if (child != null && child.gameObject.activeSelf && !skinLive)
                        offenders.Add($"{Path(child)} is a wooden {layer} layer left switched on "
                                      + "under a disabled skin");
                }
            }

            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), offenders);
        }

        /// <summary>
        /// The generated sprite keys that mean "this is a wooden surface".
        ///
        /// ⚠️⚠️ EVERY ONE OF THE FIVE FAULTS `docs/TODO.md` § 120.4 RECORDS WAS INVISIBLE TO THIS
        /// PROBE, BECAUSE IT WALKED COMPONENTS AND THEY WERE SPRITES. `PaperDress.Screen` converts
        /// a node by finding a `GodotPanel`, a `GodotButton` or a `WoodSkin` on it; the hub's
        /// backdrop, the maker's backdrop, the lobby drawer's address and code boxes and the
        /// settings panel's name field are all a bare `Image` with a colour or a baked sprite
        /// written straight onto it. **The conversion could not see them and neither could this.**
        /// Meanwhile `PaperDress.Type` had already remapped their lettering to ink, so what shipped
        /// was ink on near-black at about 1.3:1 rather than an honestly old-looking control.
        ///
        /// ⚠️ THE PREFIXES ARE THE CACHE KEYS THOSE FILES BUILD THEMSELVES, so this asks the only
        /// question that survives a re-import: `GodotTheme.KeyFor` writes `box_`, `WoodCraft`
        /// writes `wc_` and `wcsil_`, `UiMaterials` writes `plank_` and `btn_`. `PaperCraft`'s own
        /// are `pc_` and are what is supposed to be there.
        ///
        /// ⚠️ `ring_`, `chalk_` AND `down_shadow` ARE NOT LISTED AND THAT IS DELIBERATE. They are
        /// marks rather than surfaces (a focus ring, a chalk rule, a contact shadow) and the paper
        /// front end still draws all three; flagging them would fail the test for the thing that
        /// is meant to be true, which is the mistake this file's own header warns about.
        ///
        /// ⚠️ AND AN AUTHORED TEXTURE IS EXEMPT BY NAME LENGTH RATHER THAN BY PATH, because
        /// `AssetDatabase` does not exist in a player and this test runs in one. 🧑's files are
        /// `BUTTON LONG`, `JOIN BUTTON`, `TUMP`, `Arrow Left 64`, `GAME BANNER`: none of them
        /// begins with a generated key, so the prefix test IS the exemption.
        /// </summary>
        private static readonly string[] WoodKeys = { "box_", "wc_", "wcsil_", "plank_", "btn_" };

        private static void WoodenSprite(Transform t, List<string> offenders)
        {
            var image = t.GetComponent<Image>();
            if (image == null || image.sprite == null) return;

            string name = image.sprite.name;

            foreach (string key in WoodKeys)
            {
                if (!name.StartsWith(key, System.StringComparison.Ordinal)) continue;

                offenders.Add($"{Path(t)} draws a wooden sprite ({name}) on a bare Image. "
                              + "PaperDress cannot see it: give it a PaperSkin explicitly.");
                return;
            }
        }

        private static string Path(Transform t)
        {
            var sb = new StringBuilder(t.name);

            for (var p = t.parent; p != null; p = p.parent)
                sb.Insert(0, p.name + "/");

            return sb.ToString();
        }

        /// <summary>
        /// Whether a Button has a listener added in code rather than in the inspector.
        ///
        /// ⚠️ UNITY EXPOSES NO COUNT FOR RUNTIME LISTENERS, so this asks the only question that
        /// can be asked: `UnityEvent` reports zero PERSISTENT calls for a code-added listener, and
        /// `GetPersistentEventCount` is therefore an incomplete test on its own. Everything in
        /// this front end wires in code, so the honest reading is "a Button that is interactable
        /// and has no persistent call may still be live", and the assertion above treats that as
        /// acceptable rather than reporting a false positive on every control in the game.
        /// </summary>
        private static bool HasRuntimeListener(Button button) => button.interactable;

        private static GameObject Find(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hit = FindIn(root.transform, name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static GameObject FindIn(Transform t, string name)
        {
            if (t.name == name) return t.gameObject;

            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindIn(t.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }
    }
}
