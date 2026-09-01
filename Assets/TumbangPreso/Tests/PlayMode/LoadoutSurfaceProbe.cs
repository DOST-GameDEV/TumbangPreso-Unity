using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Phase 10's only surface: the Ability builds group on the hub's CAREER tab.
    ///
    /// ⚠️⚠️ THE GROUP EXISTED FOR A WHOLE PHASE AND DECIDED NOTHING, WHICH IS WHY THIS FILE IS
    /// MOSTLY ABOUT THE UNLOCK RATHER THAN ABOUT LAYOUT. `docs/TODO.md` § 114.15 row 3:
    /// `HeroLoadoutRules.ChallengesEnforced` was `false`, so all twelve alternates were handed
    /// out and every `AbilityVariant.Challenge` string on the screen described a thing the player
    /// had already been given. The flag is `true` now and `AbilityChallengeProgress` counts
    /// successful local casts, so the four cases below are the four ways that can be wrong:
    /// a locked row that does not say it is locked, a locked row that equips anyway, an unlocked
    /// row that does not survive a rebuild, and twelve rows on screen at once.
    ///
    /// ⚠️⚠️ TWELVE ROWS AT ONCE IS A REAL FAULT AND NOT A PREFERENCE. The first version of this
    /// group drew every hero's two skills, so opening CAREER on Hero Strike put twelve steppers
    /// on the screen for five heroes the player was not playing. `CLAUDE.md` § 6.2: *"everything
    /// the feature can do is on screen at once, in one flat list, with nothing saying what
    /// matters"*, which is § 92's complaint verbatim. It is one hero at a time now.
    ///
    /// ⚠️⚠️ THE STORE IS SNAPSHOTTED AND PUT BACK, INCLUDING WHEN A CASE FAILS. Equipping a
    /// variant calls `SettingsStore.Save`, and the editor shares `Application.persistentDataPath`
    /// with the built player, so the file this probe writes is the file he plays with.
    /// `CosmeticSurfaceProbe`'s header records the run where exactly that left a palette he never
    /// chose on a character it restored the wrong row for: **put the whole list back, not the row
    /// you expected to touch.**
    /// </summary>
    public class LoadoutSurfaceProbe
    {
        private static readonly (int W, int H, string Name)[] Resolutions =
        {
            (1280,  720, "16:9 720p"),
            (1600,  900, "16:9 900p"),
            (1920, 1080, "16:9 1080p"),
            (2560, 1440, "16:9 1440p"),
            (1366,  768, "16:9 laptop"),
            (1920, 1200, "16:10"),
            (2560, 1080, "21:9"),
            (3440, 1440, "21:9 1440p"),
            (1024,  768, "4:3"),
        };

        private const string Group = "Section_Ability builds";

        private GameObject _host;
        private PlayerHub _hub;
        private Camera _camera;
        private RenderTexture _target;
        private readonly List<Canvas> _canvases = new List<Canvas>();

        private List<HeroBuild> _builds;
        private List<AbilityChallengeProgress> _challenges;
        private bool _choiceMade;

        /// <summary>⚠️ CAPTURED AT CONSTRUCTION, NOT IN THE FIXTURE. A case that fails before it
        /// reaches `OpenAbilityBuilds` still runs teardown, and a zero-initialised field would
        /// put the process into Classic on the way out of a probe that never touched the mode.
        /// `SceneFlow.SelectedMode` is process state the suites after this one inherit.</summary>
        private readonly GameMode _mode = SceneFlow.SelectedMode;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var c in _canvases)
                if (c != null) c.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvases.Clear();

            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) _target.Release();

            var settings = Settings.SettingsStore.Current;
            if (settings != null && _builds != null)
            {
                settings.HeroBuilds = _builds;
                settings.AbilityChallenges = _challenges;
                settings.AccountChoiceMade = _choiceMade;
                Settings.SettingsStore.Save();
                _builds = null;
                _challenges = null;
            }

            SceneFlow.SelectedMode = _mode;

            // ⚠️⚠️ THE SCENE IS NOT BLANKED HERE AND THAT IS THE FIX FOR A RUN THAT WENT SILENT
            // THREE TIMES. The other UI probes end with "create an empty scene, then unload every
            // other loaded scene", and every one of them has FIRST replaced the test runner's own
            // scene with `LoadSceneMode.Single`. **This file never loads a scene at all**, so that
            // same loop unloads the runner's scene out from under the run: frames keep being
            // pumped (`SocialStore`'s presence heartbeat kept logging) and no test ever advances,
            // which is `docs/TODO.md` § 109's shape exactly and is indistinguishable from a slow
            // test from outside. `ProbeWait` bounds an `AsyncOperation` and cannot bound this.
            //
            // ⚠️ THERE IS NOTHING TO CLEAN UP INSTEAD. Everything this probe builds hangs off
            // `_host`, including the hub and its canvas, and `OpenAbilityBuilds` destroys any hub
            // it finds already in the scene before installing its own.
            if (_host != null) Object.Destroy(_host);
            yield return null;
        }

        // -------------------------------------------------------------------
        // § THE CASES
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ ONE HERO, TWO SKILLS, AND A STEPPER TO CHANGE WHICH HERO. Six heroes times two
        /// slots is twelve steppers, and twelve steppers is the § 92 screen.
        /// </summary>
        [UnityTest]
        public IEnumerator OnlyOneHerosTwoSkillsAreOfferedAtOnce()
        {
            yield return OpenAbilityBuilds();

            var skills = SkillRows();
            Assert.AreEqual(2, skills.Count,
                $"the Ability builds group drew {skills.Count} skill steppers. A hero has two "
                + "skills, so anything else means the group is drawing more than one hero's kit "
                + "at once. docs/TODO.md § 114.15 row 3.");

            Assert.IsNotNull(Under(Root(), "Row_Hero build"),
                "there is no hero stepper, so the five heroes whose rows are not on screen are "
                + "unreachable. One hero at a time is only a good screen if you can change which.");

            // ⚠️ THE STEPPER REALLY MOVES, which is § 108's receipt: an EQUIP button with no
            // `onClick` looked exactly like this one and did nothing.
            string before = SkillRows()[0].name;
            yield return StepForward("Row_Hero build");

            Assert.AreEqual(2, SkillRows().Count,
                "changing hero left something other than two skill rows on screen.");
            Assert.AreNotEqual(before, SkillRows()[0].name,
                "pressing the hero stepper redrew the same hero's skills, so the control reads "
                + "as broken to a player who presses it and watches nothing change.");
        }

        /// <summary>
        /// ⚠️⚠️ THE GLYPH IS THE BESPOKE ONE THE DECK AND THE INSPECT PANEL DRAW, NOT A NEW ONE.
        /// `docs/VISION.md` § 3: *"the icon says what the power does to the WORLD, not what
        /// element it is made of"*, and *"the glyph lives on the ability, not in a lookup table,
        /// so a new hero cannot ship with three blank tiles"*. A build chosen in the lobby that
        /// is only ever illustrated during a match teaches nothing at the moment of choosing.
        ///
        /// ⚠️ BOTH READINGS OF ONE SKILL SHARE A GLYPH ON PURPOSE. They do the same job in the
        /// world; a second icon would say they were different powers.
        /// </summary>
        [UnityTest]
        public IEnumerator BothSkillRowsCarryTheAbilitysOwnGlyph()
        {
            yield return OpenAbilityBuilds();

            foreach (var row in SkillRows())
            {
                var icon = Under(row, "AbilityIcon");
                Assert.IsNotNull(icon,
                    $"'{row.name}' has no AbilityIcon, so the row is a name with no picture and "
                    + "the deck's glyph teaches the player nothing about their own build.");

                var image = icon.GetComponent<Image>();
                Assert.IsNotNull(image, $"'{row.name}' has an AbilityIcon with no Image on it.");
                Assert.IsNotNull(image.sprite,
                    $"'{row.name}' has an AbilityIcon with no sprite, which draws as a solid "
                    + "square. AbilityIcons.For answered nothing for this variant's GlyphName.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ A LOCKED ROW SAYS IT IS LOCKED, SAYS WHAT THE CHALLENGE IS, AND SAYS HOW FAR
        /// ALONG IT IS. § 114.15 row 3's own words: *"a challenge string a player can never see
        /// is worse than no challenge"*, and a challenge with no counter beside it is the same
        /// sentence with the number taken out.
        ///
        /// ⚠️⚠️ AND IT REFUSES TO EQUIP, WHICH IS THE HALF A LABEL CANNOT PROVE. `HeroBuildRules.
        /// Equipped` is the check and it runs on the receiving side too, but the SCREEN is where
        /// a player finds out, and a stepper that silently writes an id the game will not honour
        /// is § 108's EQUIP button again: it looked fine and it did nothing.
        /// </summary>
        [UnityTest]
        public IEnumerator ALockedAlternateReadsItsProgressAndRefusesToEquip()
        {
            yield return OpenAbilityBuilds();

            var settings = Settings.SettingsStore.Current;
            string heroId = Roster.HeroPeople[0].Id;

            var alternate = Alternate(heroId, 1);
            Assert.IsFalse(HeroBuildRules.IsUnlocked(settings.AbilityChallenges, alternate),
                "the fixture cleared the ledger and the alternate is unlocked anyway, so this "
                + "case would prove nothing.");

            yield return StepForward(SkillRows()[0].name);

            string shown = StepperValue(SkillRows()[0]);
            StringAssert.Contains("LOCKED", shown.ToUpperInvariant(),
                $"the locked alternate reads '{shown}'. A row that offers a choice the game will "
                + "refuse has to say so before it is pressed.");

            string hint = HintOf(SkillRows()[0]);
            StringAssert.Contains(alternate.Challenge, hint,
                $"the locked row's hint is '{hint}' and never says what the challenge is.");
            StringAssert.Contains($"0 / {alternate.ChallengeTarget}", hint,
                $"the locked row's hint is '{hint}' and never says how far along the player is. "
                + "A challenge with no counter is a wish.");

            // ⚠️ THE STORE, NOT THE LABEL. The row above is what the player reads; this is what
            // the match will actually run.
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, heroId);
            Assert.AreNotEqual(alternate.Id, build.Slot1VariantId,
                "the stepper wrote a locked variant into settings.json. It is refused again at "
                + "`CheckedHeroBuildFor`, so the match is safe, but the screen would then show a "
                + "build the game is not running.");

            var checked_ = Settings.SettingsStore.CheckedHeroBuildFor(heroId);
            Assert.AreEqual(HeroLoadoutRules.DefaultFor(heroId, 1).Id, checked_.Slot1VariantId,
                "a locked alternate reached the build a match would be played with.");
        }

        /// <summary>
        /// ⚠️⚠️ THE UNLOCK SURVIVES CLOSING THE SCREEN, AND THAT IS THE ONE THING THE FIRST
        /// VERSION OF THIS FEATURE COULD NOT DO. The tabs are rebuilt on every switch
        /// (`PlayerHub.Show`), so a selection held only in the view is a selection that lasts
        /// until the player presses another tab. It has to be in `settings.HeroBuilds`, and the
        /// row has to come back reading it.
        /// </summary>
        [UnityTest]
        public IEnumerator AnUnlockedAlternateEquipsAndSurvivesRebuildingTheHub()
        {
            yield return OpenAbilityBuilds();

            var settings = Settings.SettingsStore.Current;
            string heroId = Roster.HeroPeople[0].Id;
            var alternate = Alternate(heroId, 1);

            // Earn it the way a player does, one successful cast at a time.
            for (int i = 0; i < alternate.ChallengeTarget; i++)
                Settings.SettingsStore.NoteAbilityCast(heroId, 1);

            Assert.IsTrue(HeroBuildRules.IsUnlocked(settings.AbilityChallenges, alternate),
                $"{alternate.ChallengeTarget} successful casts did not finish "
                + $"'{alternate.Challenge}'. GameSettings.NoteAbilityCast is the counter.");

            yield return Reopen();
            yield return StepForward(SkillRows()[0].name);

            Assert.AreEqual(alternate.Id,
                            HeroBuildRules.RowFor(settings.HeroBuilds, heroId).Slot1VariantId,
                "an unlocked alternate was not written to the store when it was stepped to.");

            // ⚠️ THE REBUILD IS THE ASSERTION. Everything above is still in one view.
            yield return Reopen();

            string shown = StepperValue(SkillRows()[0]);
            Assert.AreEqual(alternate.Name, shown,
                $"after rebuilding the hub the row reads '{shown}' rather than "
                + $"'{alternate.Name}'. The equipped build did not survive the tab switch.");

            Assert.AreEqual(alternate.Id,
                            Settings.SettingsStore.CheckedHeroBuildFor(heroId).Slot1VariantId,
                "the row shows the alternate and the checked build does not, so the screen and "
                + "the match disagree about what this player is bringing.");
        }

        /// <summary>
        /// ⚠️⚠️ THE STEPPER GREW AN ICON AND THE 336-UNIT CONTROL DID NOT GROW WITH IT, WHICH IS
        /// THE ONE WAY THIS FEATURE COULD BREAK EVERY OTHER SCREEN BUILT FROM `UiRows`.
        /// `UiRows.Cap`: the value column is about **368 units at 4:3**, and `docs/TODO.md` § 108
        /// is the receipt for what a wider one costs: the first `StepperRow` laid out to 476
        /// units, so at 1366x768 the right-hand arrow was simply not on screen and the row's hint
        /// drew through the value beside it, with the layout probe green.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryBuildControlFitsItsBoxAtEveryShippedResolution()
        {
            var report = new StringBuilder();
            yield return OpenAbilityBuilds();
            yield return DriveCanvases();

            // ⚠️ 368 IS `UiRows.Cap`'S OWN MEASUREMENT OF THE VALUE COLUMN AT 4:3, WRITTEN OUT
            // BECAUSE THAT METHOD IS PRIVATE AND TAKES THE WIDTH AS AN ARGUMENT. Its note is the
            // source: *"at 4:3 the column is about 368 px and every width this file hands out
            // still fits inside it. Widen one past that and it overhangs the row."*
            const float ValueColumnAt4By3 = 368.0f;
            Assert.LessOrEqual(UiRows.StepperWidth, ValueColumnAt4By3,
                $"the stepper is {UiRows.StepperWidth} units wide against a "
                + $"{ValueColumnAt4By3}-unit value column at 4:3, so it overhangs its own row "
                + "before any icon is drawn. docs/TODO.md § 108.");

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);

                int measured = 0;
                foreach (var row in SkillRows())
                {
                    measured += Measure(row, name, report);

                    // The icon lives INSIDE the control rather than beside it, so it may never
                    // reach past either arrow.
                    var icon = Under(row, "AbilityIcon");
                    Assert.IsNotNull(icon, $"{name}: '{row.name}' lost its icon on resize.");

                    // ⚠️ NOT INSIDE AN `if`. § 101.1: *"an assertion inside an `if` is an
                    // assertion that can decide not to run"*, and the test that should have
                    // caught the palette bug had exactly this shape.
                    var iconRect = (RectTransform)icon.transform;
                    Assert.Less(iconRect.anchoredPosition.x + (iconRect.sizeDelta.x * 0.5f),
                                UiRows.StepperWidth,
                                $"{name}: the ability icon on '{row.name}' reaches past the end "
                                + "of the 336-unit control, so it draws over the row beside it.");
                }

                Assert.Greater(measured, 0,
                    $"{name}: the build rows drew no labels, so this proves nothing.");
                report.AppendLine($"{name,-14} {w}x{h}  {measured} labels on 2 build rows");
            }

            Debug.Log($"[LoadoutSurfaceProbe]\n{report}");
        }

        // -------------------------------------------------------------------
        // § THE HARNESS
        // -------------------------------------------------------------------

        /// <summary>
        /// Boots the hub, opens CAREER and opens the Ability builds group, on a store with an
        /// empty ledger and no equipped builds.
        ///
        /// ⚠️ THE GROUP IS SHUT BY DEFAULT AND HAS TO BE PRESSED. `PlayerHub.Group` passes
        /// `openByDefault: false`, deliberately: `CLAUDE.md` § 6.2 question 3 asks what is on
        /// screen that the player does not need right now, and a closed group is not built at
        /// all rather than hidden (`UiRows.Section`). A probe that measured without pressing
        /// would measure a heading.
        /// </summary>
        private IEnumerator OpenAbilityBuilds()
        {
            // ⚠️⚠️ ANY HUB ALREADY IN THE SCENE IS DESTROYED FIRST, AND THE FIRST RUN OF THIS
            // FILE IS WHY. Suites run alphabetically, so `CosmeticSurfaceProbe` finishes
            // immediately before this one with `MatchSetup` still loaded, and the lobby chrome in
            // that scene builds its OWN `PlayerHub` behind the YOUR PROFILE door
            // (`LobbyChrome.BuildProfileButton`, § 114.7). `Root()` looks the canvas up by name
            // and answers with whichever it finds first, so this probe pressed one hub's CAREER
            // tab and then measured the other one's canvas: the first case in the file failed
            // reporting a missing group and the four after it passed, because this file's own
            // teardown had left an empty scene by then.
            //
            // ⚠️ DESTROYED BY COMPONENT RATHER THAN BY UNLOADING THE SCENE. `PlayerHubLayoutProbe`
            // does the same thing for the same reason. An earlier attempt blanked the scene here
            // instead and the run went silent with no xml, which is the one failure mode
            // `CLAUDE.md` § 7 says is indistinguishable from a broken install.
            foreach (var stale in Object.FindObjectsByType<PlayerHub>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (stale != null) Object.DestroyImmediate(stale);

            foreach (var canvas in Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas != null && canvas.name == "PlayerHubCanvas")
                    Object.DestroyImmediate(canvas.gameObject);

            var settings = Settings.SettingsStore.Current;
            Assert.IsNotNull(settings, "no settings store, so there is nothing to equip into");

            _builds = settings.HeroBuilds ?? new List<HeroBuild>();
            _challenges = settings.AbilityChallenges ?? new List<AbilityChallengeProgress>();
            _choiceMade = settings.AccountChoiceMade;
            settings.HeroBuilds = new List<HeroBuild>();
            settings.AbilityChallenges = new List<AbilityChallengeProgress>();

            // ⚠️ HERO STRIKE, AND STATED RATHER THAN INHERITED. Ability builds is Hero Strike
            // only (`VISION.md` § 1.1: Classic has no kit), and § 114.11 records this exact
            // fixture inheriting Classic and never photographing the group at all.
            SceneFlow.SelectedMode = GameMode.HeroStrike;

            // The boot login screen hides the hub root, so every case here would measure a
            // hidden screen. `PlayerHubLayoutProbe.Boot` carries the same line and the same why.
            settings.AccountChoiceMade = true;

            _host = new GameObject("LoadoutProbeHost");
            var nameplate = _host.AddComponent<PlayerNameplate>();
            nameplate.Install();
            yield return null;

            _hub = _host.GetComponent<PlayerHub>();
            Assert.IsNotNull(_hub, "the nameplate installed no hub");

            yield return Reopen();
        }

        /// <summary>Opens the hub on CAREER with the builds group expanded, from scratch.</summary>
        private IEnumerator Reopen()
        {
            _hub.Open();
            yield return null;

            Press("CAREER");
            yield return null;
            yield return null;

            // ⚠️ THE HEADER IS FOUND BY NODE NAME, NOT BY ITS TEXT. `UiRows.Section` draws
            // `"+  ABILITY BUILDS"`, upper-cased with a state mark in front of it, so matching
            // the title string would break the day the mark changes.
            var header = Under(Root(), Group);
            Assert.IsNotNull(header,
                "the CAREER tab has no Ability builds group. § 114.12: it bailed out at zero "
                + "matches before building it, which made Phase 10 unreachable on a fresh "
                + "account, and `EmptyCareer` builds it now.");

            var toggle = header.GetComponent<Button>();
            Assert.IsNotNull(toggle, "the Ability builds header is not pressable, so a group "
                                     + "shut by default can never be opened.");

            // ⚠️⚠️ PRESSED ONLY WHEN IT IS SHUT, BECAUSE IT IS A TOGGLE AND `PlayerHub._groups`
            // OUTLIVES A TAB SWITCH. The first version pressed unconditionally, so the second
            // call in `AnUnlockedAlternate...` CLOSED the group it had just opened and the case
            // failed reporting zero steppers on a screen that was working.
            //
            // ⚠️ THE STATE IS READ OFF THE HEADING'S OWN MARK, which `UiRows.Section` writes as
            // `"+  "` when shut and `"-  "` when open, deliberately in ASCII because Darumadrop
            // One has no chevron. Reading the shipping code's own state beats keeping a second
            // copy of it in here.
            if (HeadingMark(header) == '+')
            {
                toggle.onClick.Invoke();
                yield return null;
                yield return null;
            }

            Assert.AreEqual(2, SkillRows().Count,
                "opening the Ability builds group produced no skill steppers.");
        }

        /// <summary>
        /// The `+` or `-` a section header draws to say whether it is shut, or `?` if the
        /// heading is gone.
        /// </summary>
        private static char HeadingMark(Transform header)
        {
            foreach (var label in header.GetComponentsInChildren<Text>(true))
            {
                string text = label.text ?? "";
                if (text.StartsWith("+") || text.StartsWith("-")) return text[0];
            }

            return '?';
        }

        /// <summary>The two `Row_SKILL n · ABILITY` rows, in list order.</summary>
        private List<Transform> SkillRows()
        {
            var found = new List<Transform>();
            var root = Root();
            if (root == null) return found;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Row_SKILL ")) found.Add(t);

            return found;
        }

        private static AbilityVariant Alternate(string heroId, int slot)
        {
            foreach (var variant in HeroLoadoutRules.VariantsFor(heroId, slot))
                if (!variant.IsDefault) return variant;

            Assert.Fail($"{heroId} slot {slot} has no alternate, so there is nothing to unlock.");
            return null;
        }

        /// <summary>Presses the `&gt;` arrow of one row and lets the tab rebuild.</summary>
        private IEnumerator StepForward(string rowName)
        {
            var row = Under(Root(), rowName);
            Assert.IsNotNull(row, $"no row named '{rowName}' on screen");

            Button forward = null;
            foreach (var button in row.GetComponentsInChildren<Button>(true))
            {
                var text = button.GetComponentInChildren<Text>(true);
                if (text != null && text.text == ">") forward = button;
            }

            Assert.IsNotNull(forward, $"'{rowName}' has no forward arrow to press");
            forward.onClick.Invoke();

            yield return null;
            yield return null;
        }

        /// <summary>
        /// The big centred label of a stepper: the option the player is looking at.
        ///
        /// ⚠️⚠️ EVERY `Text` IN THIS ROW IS CALLED `Label`, because `MenuKit.Label` names them
        /// all that, so it CANNOT be found by node name and a first version that tried came back
        /// with the row's caption. It is found by the three things that are actually true of it
        /// and of nothing else in the row: it is centred (the caption and the hint are
        /// left-aligned), it is not an arrow, and it is not the `n / m` counter.
        /// </summary>
        private static string StepperValue(Transform row)
        {
            foreach (var label in row.GetComponentsInChildren<Text>(true))
            {
                if (label.alignment != TextAnchor.MiddleCenter) continue;
                if (label.text == "<" || label.text == ">") continue;
                if (System.Text.RegularExpressions.Regex.IsMatch(label.text, @"^\d+ / \d+$"))
                    continue;

                return label.text;
            }

            Assert.Fail($"'{row.name}' draws no stepper value at all, so the player is looking "
                        + "at two arrows with nothing between them.");
            return "";
        }

        /// <summary>Everything the row says under its label, joined. The hint carries the
        /// challenge and its counter.</summary>
        private static string HintOf(Transform row)
        {
            var all = new StringBuilder();
            foreach (var label in row.GetComponentsInChildren<Text>(true))
                all.Append(label.text).Append('\n');
            return all.ToString();
        }

        private static Transform Root()
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None))
                if (t.name == "PlayerHubCanvas") return t;
            return null;
        }

        private static Transform Under(Transform root, string name)
        {
            if (root == null) return null;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private void Press(string label)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                                                                    FindObjectsSortMode.None))
            {
                var text = button.GetComponentInChildren<Text>(true);
                if (text != null && text.text == label) { button.onClick.Invoke(); return; }
            }

            Assert.Fail($"no button reading '{label}' on the hub");
        }

        /// <summary>
        /// ⚠️ THE SAME `preferredWidth` MEASUREMENT `PlayerHubLayoutProbe` USES, scoped to one
        /// row. A wrapping label is measured VERTICALLY, because a wrapped label's preferred
        /// width is inside its box by definition and § 102.4's overflow was vertical while every
        /// check in the project measured horizontally.
        /// </summary>
        private static int Measure(Transform row, string resolution, StringBuilder report)
        {
            int measured = 0;

            foreach (var label in row.GetComponentsInChildren<Text>(false))
            {
                if (label == null || string.IsNullOrWhiteSpace(label.text)) continue;
                if (label.color.a < 0.05f || !label.isActiveAndEnabled) continue;

                float room = label.rectTransform.rect.width;
                if (room <= 1.0f) continue;

                measured++;

                Assert.GreaterOrEqual(label.fontSize, MenuKit.MinReadableUnits,
                    $"{resolution}: '{row.name}/{label.name}' is authored at {label.fontSize} "
                    + $"units, below the {MenuKit.MinReadableUnits}-unit floor.");

                if (label.horizontalOverflow == HorizontalWrapMode.Wrap)
                {
                    float tall = label.rectTransform.rect.height;
                    if (tall <= 1.0f) continue;

                    Assert.LessOrEqual(label.preferredHeight, tall + 1.0f,
                        $"{resolution}: '{row.name}/{label.name}' wraps to "
                        + $"{label.preferredHeight:F0} units in a {tall:F0}-unit box for "
                        + $"\"{label.text}\", so it draws over the row underneath it.");
                    continue;
                }

                Assert.LessOrEqual(label.preferredWidth, room + 1.0f,
                    $"{resolution}: '{row.name}/{label.name}' needs {label.preferredWidth:F0} "
                    + $"units for \"{label.text}\" and was given {room:F0}. It does not wrap and "
                    + "does not shrink, so it draws over whatever is beside it.");
            }

            return measured;
        }

        /// <summary>
        /// ⚠️ `Screen.SetResolution` DOES NOTHING IN BATCH MODE, so the canvases are switched to
        /// render through a camera whose target texture is the resolution. Same trick as
        /// `AspectRatioProbes`; it is the only one that works offscreen.
        /// </summary>
        private IEnumerator DriveCanvases()
        {
            _camera = Camera.main;

            if (_camera == null)
                foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude,
                                                                     FindObjectsSortMode.None))
                {
                    if (cam == null || cam.targetTexture != null) continue;
                    _camera = cam;
                    break;
                }

            if (_camera == null)
            {
                _camera = new GameObject("LoadoutProbeCamera", typeof(Camera))
                          .GetComponent<Camera>();
                _camera.transform.SetParent(_host.transform, false);
            }

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = _camera;
                c.planeDistance = _camera.nearClipPlane + 0.01f;
                _canvases.Add(c);
            }

            Assert.IsNotEmpty(_canvases, "no overlay canvas to resize: this would prove nothing");
            yield return null;
        }

        private IEnumerator Resize(int w, int h)
        {
            var next = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = next;

            if (_target != null) _target.Release();
            _target = next;

            // Three frames: the scaler recomputes in its own Update, the layout rebuild lands the
            // frame after, and a ContentSizeFitter inside a ScrollRect settles on the third.
            for (int i = 0; i < 3; i++) yield return null;
        }
    }
}
