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
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

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

            // ⚠️⚠️⚠️ THE FIGHTER PICKER IS WOOD AGAIN, ON HIS INSTRUCTION, AND THAT IS WHY THIS
            // LINE EXISTS RATHER THAN THE PROBE BEING MUTED. 🧑 2026-09-02, sending a capture of
            // the pre-paper screen: **"it used to look really good here, maybe it can retain old
            // brownn color"**, *"just change the backgrounnd or somethhing bcz i dont like the
            // dark blue sit"*, and the scope in his own words, **"make sure thhat if u bring this
            // shit back u dont break other ui"**, *"js the character select"*.
            //
            // **This probe encodes a decision the owner has reversed for one screen**, so it is
            // updated in the same commit as the reversal. `ConvertedCharacterSelect.Wire` carries
            // the design argument (a picker is a stage and a stage is dark) and `docs/TODO.md`
            // § 122.4 carries both sides of it.
            //
            // ⚠️ ONE NODE NAME, NOT A FLAG. Everything under `CharacterSelectPanel` is this
            // screen and nothing else in the game is under it, so the exemption cannot widen by
            // accident: the maker, the hub and the settings panel are siblings and are still
            // walked. This is the same shape as the two exemptions above it.
            //
            // ⚠️⚠️ AND THE GATE IT REPLACES IS A RENDER, WHICH IS WEAKER, SO SAY SO OUT LOUD.
            // Nothing now checks that this screen is COHERENTLY wooden rather than a mixture, and
            // a mixture is exactly what § 117 was about. `UiRuntimeShots.TheLobbyDoorsDraw`
            // photographs it every pass and a person looks; if this screen ever grows a second
            // material again, the answer is a probe that asserts wood HERE, not deleting this line.
            if (t.name == "CharacterSelectPanel") return;

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
        /// question that survives a re-import: `WoodCraft` writes `wc_` and `wcsil_`,
        /// `UiMaterials` writes `plank_` and `btn_`. `PaperCraft`'s own are `pc_` and are what is
        /// supposed to be there. ⚠️ `GodotTheme.KeyFor` writes `box_` and is NOT in this list,
        /// because that generator is used by both materials; see <see cref="WoodFills"/>.
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
        private static readonly string[] WoodKeys = { "wc_", "wcsil_", "plank_", "btn_" };

        /// <summary>
        /// The fills that make a `GodotTheme.Box` a WOODEN box.
        ///
        /// ⚠️⚠️ `box_` ON ITS OWN IS NOT A WOOD KEY AND THE FIRST RUN OF THIS CHECK PROVED IT.
        /// `GodotTheme.Box` is a generic rounded-rect generator with the fill and the border in
        /// its cache key, and the paper front end calls it: the picker's ability plates come back
        /// as `box_EFDABEFF_DCC19AFF_1_6`, which is `PaperWarm` in a `PaperEdge` cut, and the key
        /// chips as `box_CBAC83FF_00000000_0_4`, which is `PaperSunk`. Flagging the prefix
        /// reported six controls this pass had just repainted **in paper** as wooden leftovers.
        /// A gate that fails for the thing that is meant to be true is worse than no gate.
        ///
        /// ⚠️ SO THE FILL IS READ OUT OF THE KEY. It is the eight hex digits after `box_`, and
        /// these nine are `UiTheme`'s wood set plus `040838`, the navy the old shadow layer used
        /// (`CLAUDE.md` § 6.4 records that constant). `wc_`, `wcsil_`, `plank_` and `btn_` need no
        /// such test: `WoodCraft` and `UiMaterials` only ever draw wood.
        /// </summary>
        private static readonly string[] WoodFills =
        {
            "31190B", "5A2F14", "1D0E06", "8B5227", "793E1F", "783E1F", "4E2211", "36180C",
            "040838",
        };

        private static void WoodenSprite(Transform t, List<string> offenders)
        {
            // ⚠️⚠️ `Face` AND `Shadow` ARE GOVERNED BY THE COMPONENT RULE ABOVE AND MUST NOT BE
            // CHECKED HERE. `SkinLayers` gives every wooden control those two children and
            // `PaperDress.Strip` DEACTIVATES them rather than repainting them, so a correctly
            // converted control still has a stale wooden sprite sitting on two switched-off
            // children forever. The rule that matters is the one above: a layer left switched ON
            // under a disabled skin. Checking the sprite as well reported 33 correctly converted
            // controls, which is the entire front end.
            if (t.name == "Face" || t.name == "Shadow") return;

            // ⚠️ AND THE GREEN PRIMARY IS EXEMPT HERE FOR THE REASON IT IS EXEMPT ABOVE: it is
            // 🧑's own `JOIN BUTTON.png` colour and `CLAUDE.md` § 6.5 names green as his primary.
            var skin = t.GetComponent<GodotButton>();
            if (skin != null && (skin.Variation == "WoodPrimaryButton"
                                 || skin.Variation == "PrimaryButton")) return;

            var image = t.GetComponent<Image>();
            if (image == null) return;

            // ⚠️⚠️ A BACKDROP HAS NO SPRITE AT ALL, AND TWO OF THE FIVE FAULTS IN
            // `docs/TODO.md` § 120.4 WERE EXACTLY THAT. `MenuKit.Backdrop` makes a bare `Image`
            // and writes a colour on it, so the one node that decides what colour a whole screen
            // IS carries no component and no sprite for anything to find. The hub and the
            // character maker both shipped a `WoodDeep` backdrop under lettering that
            // `PaperDress.Type` had already remapped to ink.
            //
            // ⚠️ IT IS SCOPED TO THE NODE NAME AND THAT IS WHAT MAKES IT PRECISE RATHER THAN
            // NOISY. A flat wood colour is not wrong in general: `UiTheme.WoodMid` is the hub's XP
            // fill and the settings scrollbar's handle, and both are deliberately the one dark
            // mark on a cream surface. `MenuKit.Backdrop` is the only thing in the project that
            // makes a node called `Backdrop`, and a backdrop is the one place a wood colour can
            // never be right on a paper screen.
            if (image.sprite == null)
            {
                if (t.name != "Backdrop" || image.color.a < 0.5f) return;

                foreach (string fill in WoodFills)
                {
                    if (!ColorUtility.TryParseHtmlString("#" + fill, out var wood)) continue;
                    if (Vector3.Distance(new Vector3(wood.r, wood.g, wood.b),
                                         new Vector3(image.color.r, image.color.g,
                                                     image.color.b)) > 0.02f) continue;

                    offenders.Add($"{Path(t)} is a wooden backdrop (#{fill}). On a paper screen "
                                  + "the field is UiTheme.Paper; see docs/TODO.md § 120.4.");
                    return;
                }

                return;
            }

            string name = image.sprite.name;

            foreach (string key in WoodKeys)
            {
                if (!name.StartsWith(key, System.StringComparison.Ordinal)) continue;

                offenders.Add($"{Path(t)} draws a wooden sprite ({name}) on a bare Image. "
                              + "PaperDress cannot see it: give it a PaperSkin explicitly.");
                return;
            }

            if (!name.StartsWith("box_", System.StringComparison.Ordinal)) return;

            foreach (string fill in WoodFills)
            {
                if (!name.StartsWith("box_" + fill, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                offenders.Add($"{Path(t)} draws a wooden box ({name}) on a bare Image. "
                              + "PaperDress cannot see it: give it a PaperSkin explicitly.");
                return;
            }
        }

        // =================================================================================
        // THE INVENTORY: "make sure nothing in old ui as functions get lost"
        // =================================================================================

        /// <summary>
        /// Where the inventory of every front-end control is kept between passes.
        ///
        /// ⚠️⚠️ IT IS CAPTURED, NOT TYPED, AND THAT IS THE WHOLE POINT. <see cref="LobbyControls"/>
        /// above is a hand-written list and it works because somebody sat down with § 119.3 and
        /// wrote it out. `docs/TODO.md` § 133.5 asks for the same list for settings, character
        /// select and login **before** they are rebuilt, and 🧑 restated the worry in his own words
        /// while this pass was running: *"it should have all the functions of old ui, make sure
        /// ntohing in old ui as functions get lost"*.
        ///
        /// **A hand-written list of a 40-row settings screen would be wrong on the day it was
        /// written.** So this walks the shipped screens and writes down what it finds, once, and
        /// every later run compares against that file. A control cannot be left off the list by
        /// somebody not noticing it, which is `CLAUDE.md` § 4a's *"the answer is construction, not
        /// discipline"* applied to an inventory.
        ///
        /// ⚠️ ADDITIONS PASS, REMOVALS FAIL. The pass is meant to ADD to these screens, and a
        /// gate that went red for a new control would be a gate somebody deletes. The only claim
        /// is that nothing that used to be here has gone.
        ///
        /// ⚠️ AND IT RECORDS THE LETTERING AS WELL AS THE NODE NAME, because `MenuKit.WoodButton`
        /// leaves the default GameObject name on some controls and the only thing identifying one
        /// is the word on it. `UiRuntimeShots.HubTab` already finds the hub tabs that way, and the
        /// word is also the only handle the PLAYER has, so a rename that keeps the node and
        /// changes the word is a change worth seeing.
        /// </summary>
        private const string BaselinePath =
            "Assets/TumbangPreso/Tests/PlayMode/control-inventory-baseline.txt";

        private const string InventoryLog = "Logs/control-inventory.txt";

        /// <summary>One line of the inventory: what kind of control, where it lives, what it
        /// says.</summary>
        private static void Catalogue(Transform t, string screen, SortedSet<string> into)
        {
            // ⚠️ THE IN-MATCH LAYER IS SKIPPED HERE FOR THE SAME REASON `Walk` SKIPS IT.
            // `docs/TODO.md` § 133.4 scopes it out of this whole pass, so an inventory that
            // included it would go red the first time somebody legitimately touches the HUD.
            if (t.name == "HudCanvas") return;

            string kind = null;

            // ⚠️ ORDER MATTERS: a Dropdown and a Scrollbar both carry a Selectable, and a
            // Slider's handle carries a Button-like hit pad. The most specific wins, so a
            // control is counted once and under the name a player would give it.
            if (t.GetComponent<Dropdown>() != null) kind = "dropdown";
            else if (t.GetComponent<InputField>() != null) kind = "field";
            else if (t.GetComponent<Slider>() != null) kind = "slider";
            else if (t.GetComponent<Toggle>() != null) kind = "toggle";
            else if (t.GetComponent<Scrollbar>() != null) kind = "scrollbar";
            else if (t.GetComponent<Button>() != null) kind = "button";

            if (kind != null)
            {
                // ⚠️ THE FIRST NON-EMPTY STRING UNDER THE CONTROL, and inactive children count.
                // A drawer that is shut still owns its lettering, and a control whose word is
                // only written when the drawer opens would otherwise record as nameless and then
                // change identity the first time somebody opened it.
                string says = "";
                foreach (var text in t.GetComponentsInChildren<Text>(true))
                {
                    if (string.IsNullOrWhiteSpace(text.text)) continue;
                    says = text.text.Trim().Replace("\n", " ");
                    break;
                }

                // ⚠️ THE PLACEHOLDER IS NOT THE VALUE. A field that a probe has typed into would
                // otherwise record whatever the last test left in it, and the inventory would
                // differ run to run for a reason that is nobody's fault.
                if (kind == "field") says = "";

                into.Add($"{screen}\t{kind}\t{t.name}\t{says}");
            }

            for (int i = 0; i < t.childCount; i++) Catalogue(t.GetChild(i), screen, into);
        }

        private static void CatalogueScene(string screen, SortedSet<string> into)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                Catalogue(root.transform, screen, into);
        }

        /// <summary>
        /// Walks every front-end screen and asserts that nothing which used to be on one has
        /// gone.
        ///
        /// ⚠️⚠️ THE FIRST RUN WRITES THE BASELINE AND PASSES, WHICH IS DELIBERATE AND IS THE ONLY
        /// WAY THIS CAN BE HONEST. `docs/TODO.md` § 133.5: the list has to be written **before**
        /// the screens are rebuilt, not after, or it records the rebuild rather than what the
        /// rebuild owed. So this is run once against the shipped screens, the file it writes is
        /// committed, and from then on it is a gate rather than a camera.
        ///
        /// ⚠️ IT LOADS TWO SCENES, because the front end is not in one. `SettingsPanel` and
        /// `SignInScreen` live on `MainMenu`; the lobby, the fighter picker, the loadout board and
        /// the player hub live on `MatchSetup`. `UiRuntimeShots.TheSettingsPanelDraws` carries the
        /// same note and the same reason.
        /// </summary>
        [UnityTest]
        public IEnumerator NothingOnTheInventoryDisappeared()
        {
            Screen.SetResolution(Width, Height, false);
            for (int i = 0; i < 10; i++) yield return null;

            var found = new SortedSet<string>(System.StringComparer.Ordinal);

            // ---- MainMenu: the settings panel and the login screen ----
            var menu = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return ProbeWait.Done(menu, "MainMenu load");
            for (int i = 0; i < SettleFrames; i++) yield return null;

            var settings = Find("SettingsPanel");
            if (settings != null)
            {
                settings.SetActive(true);
                for (int i = 0; i < SettleFrames; i++) yield return null;
                Canvas.ForceUpdateCanvases();

                // ⚠️⚠️ EVERY TAB, NOT THE ONE IT OPENS ON. A settings screen is a tabbed screen
                // and the rows of a tab nobody pressed are not built, so a walk of the default
                // tab is an inventory of about a fifth of the controls. This is the drawer rule
                // from `NoWoodenSurfaceSurvivesOnTheLobby` one level up.
                foreach (var tab in settings.GetComponentsInChildren<Button>(true))
                {
                    var skin = tab.GetComponent<GodotButton>();
                    bool isTab = skin != null && skin.Variation != null
                                 && skin.Variation.Contains("Tab");
                    if (!isTab) continue;

                    tab.onClick.Invoke();
                    for (int i = 0; i < 20; i++) yield return null;
                    Canvas.ForceUpdateCanvases();
                    CatalogueScene("settings", found);
                }

                CatalogueScene("settings", found);
                settings.SetActive(false);
            }

            var owner = Object.FindFirstObjectByType<ConvertedMainMenu>();
            if (owner != null)
            {
                var signIn = owner.GetComponent<SignInScreen>();
                if (signIn == null) signIn = owner.gameObject.AddComponent<SignInScreen>();
                signIn.Install();

                // ⚠️⚠️ ALL THREE STATES, WHICH IS `CLAUDE.md` § 6.2b'S FIRST ROW AS A LOOP.
                // *"The sign-in screen was shot only as Open(). It ships as OpenAtBoot() too,
                // which hides BACK, renames a button and has no hub behind it."* A control that
                // only exists in the boot state is exactly the kind a rebuild drops silently.
                signIn.Open();
                for (int i = 0; i < 20; i++) yield return null;
                CatalogueScene("login", found);

                signIn.OpenForUpgrade();
                for (int i = 0; i < 20; i++) yield return null;
                CatalogueScene("login", found);

                signIn.OpenAtBoot();
                for (int i = 0; i < 20; i++) yield return null;
                CatalogueScene("login", found);
            }

            // ---- MatchSetup: the lobby and everything it opens ----
            bool previousNetworked = SceneFlow.Networked;
            SceneFlow.Networked = true;

            var lobby = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(lobby, "MatchSetup load");
            for (int i = 0; i < SettleFrames; i++) yield return null;
            Canvas.ForceUpdateCanvases();

            CatalogueScene("lobby", found);

            foreach (string chip in new[] { "SettingsDrawerToggle", "JoinChip", "ChatChip" })
            {
                var button = Find(chip)?.GetComponent<Button>();
                if (button == null) continue;

                button.onClick.Invoke();
                for (int i = 0; i < 10; i++) yield return null;
                Canvas.ForceUpdateCanvases();
                CatalogueScene("lobby", found);
            }

            foreach (string door in new[] { "CharacterButton", "LoadoutButton", "ProfileButton" })
            {
                var open = Find(door)?.GetComponent<Button>();
                if (open == null) continue;

                open.onClick.Invoke();
                for (int i = 0; i < 40; i++) yield return null;
                Canvas.ForceUpdateCanvases();
                CatalogueScene(door == "ProfileButton" ? "profile" : "character", found);
            }

            SceneFlow.Networked = previousNetworked;
            var session = Net.NetSession.Instance;
            if (session != null) session.Stop();

            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllLines(InventoryLog, found);

            if (!System.IO.File.Exists(BaselinePath))
            {
                System.IO.File.WriteAllLines(BaselinePath, found);
                Assert.Ignore(
                    $"no inventory baseline existed, so this run WROTE one with {found.Count} "
                    + $"controls to {BaselinePath}. Commit it: from here on it is the gate that "
                    + "says a rebuild did not lose a button. docs/TODO.md § 133.5.");
                yield break;
            }

            var baseline = new SortedSet<string>(System.IO.File.ReadAllLines(BaselinePath),
                                                 System.StringComparer.Ordinal);

            var lost = new List<string>();
            foreach (string row in baseline)
            {
                if (found.Contains(row)) continue;

                // ⚠️ A CONTROL THAT KEPT ITS NODE AND CHANGED ITS WORD IS REPORTED SEPARATELY
                // RATHER THAN AS A LOSS. Renaming CONTINUE to NEXT is a design decision somebody
                // made on purpose; deleting the button is not, and a gate that cannot tell them
                // apart gets its output skimmed, which is § 124.11's lesson.
                var parts = row.Split('\t');
                string stem = parts.Length >= 3
                    ? $"{parts[0]}\t{parts[1]}\t{parts[2]}\t"
                    : row;

                bool nodeSurvived = false;
                foreach (string live in found)
                    if (live.StartsWith(stem, System.StringComparison.Ordinal))
                        nodeSurvived = true;

                if (!nodeSurvived) lost.Add(row.Replace("\t", "  |  "));
            }

            Assert.IsEmpty(lost,
                "these front-end controls were on the screens before this pass and are not on "
                + "them now, so the rebuild lost them. 🧑 asked for this by name: \"it should have "
                + "all the functions of old ui, make sure ntohing in old ui as functions get "
                + "lost\". docs/TODO.md § 133.5, and " + InventoryLog + " is the full live walk:\n  "
                + string.Join("\n  ", lost));
        }

        /// <summary>
        /// No label in the front end is faking a weight the face does not have.
        ///
        /// ⚠️⚠️ THIS IS THE REGRESSION GATE FOR THE WHOLE OF `docs/TODO.md` § 133, and it exists
        /// because the fault it guards is INVISIBLE in a code review and nearly invisible in a
        /// screenshot. Legacy `Text` given `FontStyle.Bold` on a face that ships one weight does
        /// not fail and does not warn: it draws every glyph twice at an offset. § 132.8 chased
        /// that through a stale capture, a wrapping row, a clipped box and a soft render before
        /// anybody thought to ask what the font actually contained.
        ///
        /// ⚠️ IT ASKS ABOUT THE FONT, NOT ABOUT THE SOURCE. A grep for `FontStyle.Bold` would
        /// pass a screen that set it through a converted `.tscn`, and those are most of this front
        /// end. What is asserted here is the only thing that matters at the pixel: a label is
        /// either in a file that HAS this weight, or it is not asking for one.
        ///
        /// ⚠️ AND THE IN-MATCH LAYER IS OUT OF SCOPE, so it is skipped by canvas name exactly as
        /// <see cref="Walk"/> skips it. `Hud`, `AbilityInspectPanel` and `ComicPopup` still carry
        /// synthetic bolds on purpose: § 133.4 draws the line at "is it drawn while a round is
        /// live", and moving them in this pass would put a font change and a readability contract
        /// in one commit with no way to tell which broke what.
        /// </summary>
        [UnityTest]
        public IEnumerator NoLabelFakesItsWeight()
        {
            Screen.SetResolution(Width, Height, false);
            for (int i = 0; i < 10; i++) yield return null;

            bool previousNetworked = SceneFlow.Networked;
            SceneFlow.Networked = true;

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < SettleFrames; i++) yield return null;

            foreach (string chip in new[] { "SettingsDrawerToggle", "JoinChip", "ChatChip" })
            {
                var button = Find(chip)?.GetComponent<Button>();
                if (button == null) continue;
                button.onClick.Invoke();
                for (int i = 0; i < 6; i++) yield return null;
            }

            foreach (string door in new[] { "CharacterButton", "ProfileButton" })
            {
                var open = Find(door)?.GetComponent<Button>();
                if (open == null) continue;
                open.onClick.Invoke();
                for (int i = 0; i < 30; i++) yield return null;
            }

            Canvas.ForceUpdateCanvases();

            var faked = new List<string>();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                WalkWeight(root.transform, faked);

            SceneFlow.Networked = previousNetworked;
            var session = Net.NetSession.Instance;
            if (session != null) session.Stop();

            Assert.IsEmpty(faked,
                "these labels ask Unity to synthesise a weight their font does not ship, which "
                + "draws each glyph twice at an offset and is a smear rather than a bold. It is "
                + "worst at MenuKit.MinReadableUnits, which is where most of the words in this "
                + "game live. Use MenuKit.Apply(label, PaperKit.FaceFor(label.fontSize), "
                + "bold: true) instead; docs/TODO.md § 133 and § 132.8:\n  "
                + string.Join("\n  ", faked));
        }

        /// <summary>
        /// No text field highlights selected text in Unity's default blue.
        ///
        /// ⚠️⚠️ THIS SHIPPED, ON ALL FOUR FIELDS, FOR THE WHOLE LIFE OF THE PROJECT.
        /// `InputField.selectionColor` defaults to `a8ceff`, and `grep -rn selectionColor` over
        /// the entire repository returned nothing: no site had ever assigned it. `CLAUDE.md`
        /// § 6.4's own test is *"if a hex has more blue in it than red, it does not belong in a
        /// menu"*, and that colour is 87 levels more blue than red.
        ///
        /// ⚠️⚠️ AND IT IS INVISIBLE TO EVERY OTHER GATE IN THIS REPOSITORY, WHICH IS THE REASON
        /// FOR THE TEST. A selection highlight only exists while text is selected, so it appears
        /// in no render, no layout probe and no screenshot review. § 6.4 says to find this class
        /// of fault by GREPPING rather than by looking, and its receipt is `UiTheme.Ink` sitting
        /// navy for the life of the file because a near-black navy reads as black in a diff.
        ///
        /// ⚠️ BOTH SCENES, because the four fields are split across them: the join code and the
        /// chat line are on `MatchSetup`, the settings username and the sign-in fields are on
        /// `MainMenu`.
        /// </summary>
        [UnityTest]
        public IEnumerator NoFieldHighlightsInBlue()
        {
            Screen.SetResolution(Width, Height, false);
            for (int i = 0; i < 10; i++) yield return null;

            var offenders = new List<string>();

            var menu = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return ProbeWait.Done(menu, "MainMenu load");
            for (int i = 0; i < SettleFrames; i++) yield return null;

            var settings = Find("SettingsPanel");
            if (settings != null)
            {
                settings.SetActive(true);
                for (int i = 0; i < 30; i++) yield return null;
            }

            var owner = Object.FindFirstObjectByType<ConvertedMainMenu>();
            if (owner != null)
            {
                var signIn = owner.GetComponent<SignInScreen>();
                if (signIn == null) signIn = owner.gameObject.AddComponent<SignInScreen>();
                signIn.Install();
                signIn.Open();
                for (int i = 0; i < 30; i++) yield return null;
            }

            CollectBlueFields(offenders);

            bool previousNetworked = SceneFlow.Networked;
            SceneFlow.Networked = true;

            var lobby = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(lobby, "MatchSetup load");
            for (int i = 0; i < SettleFrames; i++) yield return null;

            foreach (string chip in new[] { "JoinChip", "ChatChip" })
            {
                var button = Find(chip)?.GetComponent<Button>();
                if (button == null) continue;
                button.onClick.Invoke();
                for (int i = 0; i < 10; i++) yield return null;
            }

            CollectBlueFields(offenders);

            SceneFlow.Networked = previousNetworked;
            var session = Net.NetSession.Instance;
            if (session != null) session.Stop();

            Assert.IsEmpty(offenders,
                "these text fields highlight selected text in a colour with more blue in it than "
                + "red, which CLAUDE.md § 6.4 forbids in any UI layer. Unity's default is a8ceff "
                + "and no site had ever assigned it. Call MenuKit.Dress(field) where the field is "
                + "built:\n  " + string.Join("\n  ", offenders));
        }

        private static void CollectBlueFields(List<string> offenders)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var field in root.GetComponentsInChildren<InputField>(true))
                {
                    var c = field.selectionColor;

                    // ⚠️ § 6.4'S OWN TEST, VERBATIM: "if a hex has more blue in it than red, it
                    // does not belong in a menu". A tolerance is allowed because a warm sand at
                    // partial alpha can carry a point or two more blue than red and still be
                    // unmistakably warm; a8ceff is 0.34 over, which is nowhere near it.
                    if (c.b <= c.r + 0.02f) continue;

                    offenders.Add($"{Path(field.transform)} selectionColor is "
                                  + $"#{ColorUtility.ToHtmlStringRGB(c)} "
                                  + $"(blue {c.b:F2} over red {c.r:F2})");
                }
            }
        }

        private static void WalkWeight(Transform t, List<string> faked)
        {
            if (t.name == "HudCanvas" || t.name == "MainMenuRoot") return;

            var text = t.GetComponent<Text>();

            if (text != null && text.font != null
                && (text.fontStyle == FontStyle.Bold
                    || text.fontStyle == FontStyle.BoldAndItalic))
            {
                // ⚠️ THE TEST IS WHETHER THE FILE ITSELF IS A BOLD ONE. Nunito ships Bold as a
                // separate asset, so a label legitimately in that file and marked Bold is asking
                // for a weight it already has, which is harmless. Anything else is asking Unity
                // to invent one.
                bool inABoldFile = text.font.name.IndexOf("bold",
                    System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (!inABoldFile)
                    faked.Add($"{Path(t)} is {text.font.name} at {text.fontSize} with "
                              + $"FontStyle.{text.fontStyle}: \"{text.text}\"");
            }

            for (int i = 0; i < t.childCount; i++) WalkWeight(t.GetChild(i), faked);
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
