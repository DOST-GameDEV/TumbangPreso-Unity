using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Phase 10's only surface: the LOADOUT board on the CHOOSE YOUR HERO stage.
    ///
    /// ⚠️⚠️ THIS FILE HAS NOW FOLLOWED THE FEATURE THROUGH THREE SCREENS, AND THAT IS THE POINT
    /// OF KEEPING IT RATHER THAN WRITING A NEW ONE. It was written against a collapsed
    /// `UiRows.Group` on the hub's CAREER tab; § 115.6 lifted that into a LOADOUT tab of its own
    /// because 🧑 could not find it (*"i also dont know hhow to navigae to loadouts section"*) and
    /// the probe was rewired; § 122.5 moved the whole thing off the hub onto the fighter picker
    /// (**"put loadout here, it makes no sense to be in profile"**) and the probe was **not**, so
    /// five of its cases spent a day failing with `no button reading 'LOADOUT' on the hub`.
    ///
    /// ⚠️⚠️ THAT IS THE THIRD TIME A SHIPPED MOVE HAS LEFT A PROBE KNOCKING ON THE OLD DOOR, and
    /// the failure is always the same shape: **the feature works and the coverage does not**.
    /// § 96 is the hub's one door nobody found; § 114 is `PlayerNameplate` no longer installed by
    /// any screen while `PlayerHubLayoutProbe` still drove it. A green probe for a screen nobody
    /// can reach is worse than a red one, and a red one for a screen that works is noise that
    /// teaches the next person to skim the results. `docs/TODO.md` § 124.11.
    ///
    /// ⚠️⚠️ SO THE FIRST CASE PRESSES THE REAL DOOR RATHER THAN CALLING `ToggleLoadoutBoard`.
    /// The board could be opened by reflection in one line, and every case here would then pass
    /// on a build where the chip had been deleted. `CLAUDE.md` § 6.3: *every destination has a
    /// visible door, and a door is a thing that looks pressable.* Pressing it is the assertion.
    ///
    /// ⚠️⚠️ THE STORE IS SNAPSHOTTED AND PUT BACK, INCLUDING WHEN A CASE FAILS. Equipping calls
    /// `SettingsStore.Save`, and the editor shares `Application.persistentDataPath` with the
    /// built player, so the file this probe writes is the file he plays with.
    /// `CosmeticSurfaceProbe`'s header records the run where exactly that left a palette he never
    /// chose: **put the whole list back, not the row you expected to touch.**
    /// </summary>
    public class LoadoutSurfaceProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE SETUP HALF OF `docs/TODO.md` § 126.8'S FIX, AND THIS FIXTURE GETS ONLY THE
        /// SETUP HALF ON PURPOSE. `PlayModeWorld`'s header asks for both hooks; this class
        /// already owns a `[UnityTearDown]` doing its own cleanup, and NUnit does not define an
        /// order between two teardowns of the same kind. **The setup reset is the half that
        /// protects THIS fixture**: it guarantees the world is empty and settled when the test
        /// below starts, whatever ran before it. With every fixture in the folder carrying it,
        /// no test can inherit a world at all, which is the property the entry actually wants.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

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

        /// <summary>
        /// ⚠️⚠️ THE SURFACE MOVED A FOURTH TIME AND THE CONSTANTS ARE WHERE THAT SHOWS.
        /// `LoadoutBoard` behind a `LoadoutDoor` was the picker's own overlay: four tiles and
        /// three slot heads on one board. The painted pass replaced it with `TumpSkillView`,
        /// which is **one slot at a time** on a screen of its own: `TumpSkills` on the picker
        /// opens `OwnerSkillsCanvas`, three tabs `TumpSkillSlot0` to `TumpSkillSlot2` choose the
        /// slot, and only that slot's readings are drawn, as `TumpVariant_&lt;id&gt;` buttons under
        /// `VariantChoices`, with one `TumpEquipSkill` under them.
        ///
        /// ⚠️⚠️ AND THAT IS A NARROWING OF WHAT THIS FILE CAN CLAIM, SAID OUT LOUD RATHER
        /// THAN QUIETLY DROPPED. "Four tiles on one board" was this probe's way of saying the
        /// screen is not a flat list of everybody's builds (`CLAUDE.md` § 6.2, *everything the
        /// feature can do is on screen at once*). One slot at a time is a STRONGER answer to the
        /// same worry, so the case below asserts the new shape rather than the old count: every
        /// reading on screen belongs to the hero on the stage AND to the slot the tabs say we
        /// are on.
        /// </summary>
        private const string Skills = "OwnerSkillsCanvas";
        private const string Choices = "VariantChoices";
        private const string Door = "TumpSkills";
        private const string Equip = "TumpEquipSkill";
        private const string Unlock = "UnlockState";

        /// <summary>The tab for a slot. ⚠️ `TumpSkillSlot0` IS THE ULTIMATE: the tabs read
        /// SKILL 1, SKILL 2, ULTIMATE and are numbered 1, 2, 0, because the number is the
        /// ability's own slot and the ultimate is slot zero everywhere else in the game.</summary>
        private static string Tab(int slot) => "TumpSkillSlot" + slot;

        private GameObject _panel;
        private Camera _camera;
        private RenderTexture _target;
        private readonly List<Canvas> _canvases = new List<Canvas>();

        private List<HeroBuild> _builds;
        private List<AbilityChallengeProgress> _challenges;

        /// <summary>⚠️ CAPTURED AT CONSTRUCTION, NOT IN THE FIXTURE. A case that fails before it
        /// reaches `OpenBoard` still runs teardown, and a zero-initialised field would put the
        /// process into Classic on the way out of a probe that never touched the mode.
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
                Settings.SettingsStore.Save();
                _builds = null;
                _challenges = null;
            }

            // ⚠️ THE PIN IS RELEASED HERE OR EVERY SUITE AFTER THIS ONE INHERITS IT.
            // `SceneFlow.RulesPinned` is process state, which is the same class of leak
            // `docs/TODO.md` § 126.8 is entirely about.
            SceneFlow.UnpinSelectedRules();
            SceneFlow.SelectedMode = _mode;
            _panel = null;
            yield return null;
        }

        // -------------------------------------------------------------------
        // § THE CASES
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ ONE HERO, TWO SLOTS, AND NO HERO STEPPER, WHICH IS THE WHOLE ARGUMENT FOR THE
        /// MOVE. The hub's version needed a stepper to ask "which hero?"; the picker has already
        /// answered that by construction, because the hero on the stage IS the selection. That is
        /// what takes the journey from five presses to two, and it is also what makes the § 92
        /// screen impossible here rather than merely avoided: there is nowhere for a second
        /// hero's rows to come from.
        ///
        /// ⚠️ SO THE ASSERTION IS THAT EVERY TILE BELONGS TO THE HERO ON SCREEN. Twelve rows at
        /// once was a real fault and not a preference (`CLAUDE.md` § 6.2: *everything the feature
        /// can do is on screen at once, in one flat list*), and this is that fault stated in a
        /// form the new surface can actually fail.
        /// </summary>
        [UnityTest]
        public IEnumerator OnlyTheHeroOnTheStageHasTilesOnTheBoard()
        {
            yield return OpenBoard();

            string heroId = CurrentHero();
            Assert.IsNotEmpty(heroId, "the picker is on no hero, so the skills screen has nothing to draw");

            // The screen opens on SKILL 1, which is `TumpSkillView._slot`'s initial value.
            var expected = HeroLoadoutRules.VariantsFor(heroId, 1);
            var tiles = Tiles();

            Assert.AreEqual(expected.Count, tiles.Count,
                $"the slot drew {tiles.Count} readings and this hero's first skill has "
                + $"{expected.Count}. Anything else means the column is showing a kit that is "
                + "not the one on the stage, or more than one slot at a time. docs/TODO.md "
                + "section 122.5 and section 153.20.");

            foreach (var tile in tiles)
                StringAssert.StartsWith("TumpVariant_" + heroId + ".1.", tile.name,
                    $"'{tile.name}' is on {heroId}'s SKILL 1 column. Every reading has to belong "
                    + "to the hero the picker is showing AND to the slot the tabs say we are on, "
                    + "or the column is a flat list of everybody's builds again.");

            // ⚠️⚠️ THREE TABS AND ONE COLUMN, WHICH IS NOT A CONTRADICTION AND IS THE WHOLE
            // POINT OF THE THIRD TAB. `docs/TODO.md` section 131.7: the old board showed two of a
            // hero's three powers, so a screen titled LOADOUT never mentioned TITAN FISSURE. The
            // ultimate has a TAB and no readings **on purpose**, because `AbilityVariant.Slot`
            // refuses it options: *"an ultimate is banked once or twice a match and reading which
            // one an opponent has is already a skill"*.
            for (int slot = 0; slot <= 2; slot++)
                Assert.IsNotNull(Under(SkillsScreen(), Tab(slot)),
                    $"the skills screen has no {Tab(slot)}, so one of this hero's three powers "
                    + "cannot be read at all. docs/TODO.md section 131.7.");

            Press(Tab(0));
            yield return null;
            yield return null;

            Assert.IsFalse(Under(SkillsScreen(), Choices).gameObject.activeInHierarchy,
                "the ultimate drew a column of readings. It has none to choose between, and a "
                + "chooser with one entry teaches a choice that is not there.");
        }

        /// <summary>
        /// ⚠️⚠️ THE GLYPH IS THE BESPOKE ONE THE DECK AND THE INSPECT PANEL DRAW, NOT A NEW ONE.
        /// `docs/VISION.md` § 3: *"the icon says what the power does to the WORLD, not what
        /// element it is made of"*, and *"the glyph lives on the ability, not in a lookup table,
        /// so a new hero cannot ship with three blank tiles"*. A build chosen in the lobby that is
        /// only ever illustrated during a match teaches nothing at the moment of choosing.
        ///
        /// ⚠️ BOTH READINGS OF ONE SKILL SHARE THE SLOT'S GLYPH ON PURPOSE. They do the same job
        /// in the world; a second icon would say they were different powers. That is why the
        /// glyph is on the slot HEAD here rather than on each tile, which is the same rule the
        /// hub's version enforced one control shape earlier.
        ///
        /// ⚠️⚠️ THE ULTIMATE'S HEAD IS CHECKED BY THE SAME LOOP AND THAT IS WHY IT REUSES
        /// `BuildSlotHead`. It has no tiles, so nothing else on this screen would ever ask
        /// whether `AbilityIcons.For` answers for a hero's ULTIMATE glyph, and a blank square in
        /// the one row that names TITAN FISSURE, GLACIAL NOVA or GRAND COVEN would be the most
        /// visible missing icon in the game.
        /// </summary>
        [UnityTest]
        public IEnumerator BothSlotHeadsCarryTheAbilitysOwnGlyph()
        {
            yield return OpenBoard();

            string heroId = CurrentHero();
            var kit = Abilities.HeroAbilitySystem.CreateKitFor(heroId);

            // ⚠️⚠️ THE GLYPH IS DRAWN, NOT LOADED, AND THAT CHANGED WHAT CAN BE ASSERTED.
            // `AbilityIcons.For` answered with a `Sprite` and a missing one drew a solid square;
            // `TumpAbilitySymbol` builds the pictogram in `OnPopulateMesh` from the
            // `AbilityGlyph` enum, so there is no sprite to be null. The two halves of the old
            // claim survive as two assertions: the tab carries the ability's OWN glyph rather
            // than a lookup, and the symbol actually puts geometry on screen.
            var tabs = new[] { (1, kit.Skill1), (2, kit.Skill2), (0, kit.Ultimate) };

            Canvas.ForceUpdateCanvases();
            yield return null;

            foreach (var (slot, ability) in tabs)
            {
                var tab = Under(SkillsScreen(), Tab(slot));
                Assert.IsNotNull(tab, $"no {Tab(slot)} on the skills screen.");

                var symbol = tab.GetComponentInChildren<TumpAbilitySymbol>(true);
                Assert.IsNotNull(symbol, $"{Tab(slot)} has no TumpAbilitySymbol, so the power is "
                    + "named and never shown.");

                Assert.AreEqual(ability.Glyph, symbol.Glyph,
                    $"{Tab(slot)} draws {symbol.Glyph} and the ability says {ability.Glyph}. "
                    + "docs/VISION.md section 3: the glyph lives on the ability, not in a lookup "
                    + "table, so a new hero cannot ship with three blank tiles.");

                Assert.Greater(symbol.canvasRenderer.GetMesh().vertexCount, 0,
                    $"{Tab(slot)}'s symbol drew no geometry at all, which reads as an empty box "
                    + "in the row that names this hero's power.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ A LOCKED TILE SAYS IT IS LOCKED, SAYS WHAT THE CHALLENGE IS, AND SAYS HOW FAR
        /// ALONG IT IS. § 114.15 row 3's own words: *"a challenge string a player can never see is
        /// worse than no challenge"*, and a challenge with no counter beside it is the same
        /// sentence with the number taken out.
        ///
        /// ⚠️⚠️ AND IT REFUSES TO EQUIP, WHICH IS THE HALF A LABEL CANNOT PROVE. `HeroBuildRules
        /// .Equipped` is the check and it runs on the receiving side too, but the SCREEN is where
        /// a player finds out, and a tile that silently writes an id the game will not honour is
        /// § 108's EQUIP button again: it looked fine and it did nothing.
        ///
        /// ⚠️ THE LOCKED TILE IS PRESSED RATHER THAN INSPECTED. `BuildVariantTile` gives every
        /// tile a live `Button` whatever its state, so "it is drawn dim" is not the same claim as
        /// "it refuses", and only one of the two is the one that matters.
        /// </summary>
        [UnityTest]
        public IEnumerator ALockedAlternateReadsItsProgressAndRefusesToEquip()
        {
            yield return OpenBoard();

            var settings = Settings.SettingsStore.Current;
            string heroId = CurrentHero();
            var alternate = Alternate(heroId, 1);

            Assert.IsFalse(HeroBuildRules.IsUnlocked(settings.AbilityChallenges, alternate),
                "the fixture cleared the ledger and the alternate is unlocked anyway, so this "
                + "case would prove nothing.");

            var tile = Tile(alternate.Id);
            Assert.IsNotNull(tile, $"no reading for '{alternate.Id}' in the SKILL 1 column.");

            // ⚠️⚠️ THE STATE IS READ OFF TWO PLACES AND BOTH ARE A PLAYER-FACING SENTENCE.
            // The row itself carries a `Status` word, and the detail side carries the challenge
            // and the counter under `UnlockState`. Reading only the row would pass a screen that
            // says LOCKED and never says what to do about it, which is section 114.15 row 3's
            // own complaint: *"a challenge string a player can never see is worse than no
            // challenge"*, and a challenge with no counter beside it is that sentence with the
            // number taken out.
            StringAssert.Contains("LOCKED", TextIn(tile).ToUpperInvariant(),
                $"the locked reading says '{TextIn(tile)}'. A control that offers a choice the "
                + "game will refuse has to say so before it is pressed.");

            tile.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;

            string detail = Words(Unlock);
            StringAssert.Contains(alternate.Challenge, detail,
                $"the locked reading's detail says '{detail}' and never says what the challenge is.");
            StringAssert.Contains($"0 / {alternate.ChallengeTarget}", detail,
                $"the locked reading's detail says '{detail}' and never says how far along the "
                + "player is. A challenge with no counter is a wish.");

            // ⚠️ THE EQUIP CONTROL IS PRESSED RATHER THAN INSPECTED. "It is drawn dim" is not
            // the same claim as "it refuses", and only one of the two is the one that matters.
            // section 108's EQUIP button looked fine and did nothing, which is the mirror of this.
            var equip = Under(SkillsScreen(), Equip).GetComponent<Button>();
            Assert.IsFalse(equip.interactable,
                "the equip control offers a locked reading. Choosing it is refused again at "
                + "`CheckedHeroBuildFor`, so the match is safe, but the screen would then show a "
                + "build the game is not running.");
            StringAssert.Contains("LOCKED", equip.GetComponentInChildren<Text>().text.ToUpperInvariant(),
                "the equip control does not say why it will not act.");

            equip.onClick.Invoke();
            yield return null;

            // ⚠️ THE STORE, NOT THE LABEL. The words above are what the player reads; this is
            // what the match will actually run.
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, heroId);
            Assert.AreNotEqual(alternate.Id, build.Slot1VariantId,
                "pressing equip on a locked reading wrote it into settings.json.");

            var vetted = Settings.SettingsStore.CheckedHeroBuildFor(heroId);
            Assert.AreEqual(HeroLoadoutRules.DefaultFor(heroId, 1).Id, vetted.Slot1VariantId,
                "a locked alternate reached the build a match would be played with.");
        }

        /// <summary>
        /// ⚠️⚠️ THE CHOICE SURVIVES CLOSING THE BOARD, AND THAT IS THE ONE THING THE FIRST VERSION
        /// OF THIS FEATURE COULD NOT DO. `BuildLoadoutBoard` destroys and rebuilds the whole board
        /// on every open, so a selection held only in the view is a selection that lasts until the
        /// player presses CLOSE. It has to be in `settings.HeroBuilds`, and the tile has to come
        /// back reading it.
        ///
        /// ⚠️ THE UNLOCK IS EARNED THE WAY A PLAYER EARNS IT, one `NoteAbilityCast` at a time,
        /// rather than by writing the ledger. `FUTURE.md` PHASE 10 promises every unlock is
        /// reachable in Practice against bots, and a probe that hands itself the row proves the
        /// screen and not the promise.
        /// </summary>
        [UnityTest]
        public IEnumerator AnUnlockedAlternateEquipsAndSurvivesRebuildingTheBoard()
        {
            yield return OpenBoard();

            var settings = Settings.SettingsStore.Current;
            string heroId = CurrentHero();
            var alternate = Alternate(heroId, 1);

            for (int i = 0; i < alternate.ChallengeTarget; i++)
                Settings.SettingsStore.NoteAbilityCast(heroId, 1);

            Assert.IsTrue(HeroBuildRules.IsUnlocked(settings.AbilityChallenges, alternate),
                $"{alternate.ChallengeTarget} successful casts did not finish "
                + $"'{alternate.Challenge}'. GameSettings.NoteAbilityCast is the counter.");

            yield return Reopen();

            var tile = Tile(alternate.Id);
            Assert.IsNotNull(tile, $"no reading for '{alternate.Id}' after the ledger was filled.");
            tile.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;

            var equip = Under(SkillsScreen(), Equip).GetComponent<Button>();
            Assert.IsTrue(equip.interactable,
                "an unlocked reading is selected and the equip control still refuses it.");
            equip.onClick.Invoke();
            yield return null;

            Assert.AreEqual(alternate.Id,
                            HeroBuildRules.RowFor(settings.HeroBuilds, heroId).Slot1VariantId,
                "an unlocked alternate was not written to the store when equip was pressed.");

            // ⚠️ THE REBUILD IS THE ASSERTION. Everything above is still in one view.
            yield return Reopen();

            var again = Tile(alternate.Id);
            Assert.IsNotNull(again, $"'{alternate.Id}' lost its reading after the screen was rebuilt.");
            StringAssert.Contains("EQUIPPED", TextIn(again).ToUpperInvariant(),
                "after reopening the skills screen the equipped reading does not say so, so the "
                + "choice did not survive BACK and the player has no way to tell what they are "
                + "bringing.");

            Assert.AreEqual(alternate.Id,
                            Settings.SettingsStore.CheckedHeroBuildFor(heroId).Slot1VariantId,
                "the screen shows the alternate and the checked build does not, so the screen "
                + "and the match disagree about what this player is bringing.");
        }

        /// <summary>
        /// ⚠️⚠️ EVERY WORD ON A TILE HAS TO FIT THE TILE, AND THIS CASE EXISTS BECAUSE THE ROWS
        /// WERE REWRITTEN ON 2026-09-02 AGAINST NOTHING. Each alternate was relabelled to name a
        /// play rather than a percentage, which was the right change; seven of the twelve
        /// descriptions and eleven of the twelve trade lines came out over the budget of the tile
        /// they are drawn on. `Phase10Tests.EveryVariantRowFitsTheTileItIsDrawnOn` carries the
        /// arithmetic and catches it in 40 ms; this measures the real rects on top of it.
        ///
        /// ⚠️⚠️ BOTH OVERFLOWS ARE SILENT AND THEY FAIL IN OPPOSITE DIRECTIONS. The body sets
        /// `verticalOverflow = Truncate`, which **drops a whole line with no warning** — § 122.14
        /// caught the equipped tile reading *"The stomp as it is tuned. One heavy shock at"* and
        /// stopping. The trade line is a `MenuKit.Label` with no wrap, which **overflows its box
        /// and draws over its neighbour** — § 108's stepper again. A probe that only compared
        /// preferred width against box width would see the first and miss the second, so this
        /// checks both axes.
        ///
        /// ⚠️ AT NINE RESOLUTIONS BECAUSE THE BOARD IS ANCHORED TO THE STAGE AND THE STAGE MOVES.
        /// `AspectRatioProbes` drives the same nine, and `CLAUDE.md` § 6.2b records that he plays
        /// in a short wide window every one of them is taller than.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryTileLabelFitsItsTileAtEveryShippedResolution()
        {
            var report = new StringBuilder();
            yield return OpenBoard();
            yield return DriveCanvases();

            foreach (var (w, h, name) in Resolutions)
            {
                yield return Resize(w, h);

                int measured = 0;

                foreach (var tile in Tiles())
                {
                    var box = (RectTransform)tile.transform;
                    float band = box.rect.width - 32.0f;

                    foreach (var label in tile.GetComponentsInChildren<Text>(true))
                    {
                        measured++;

                        var rt = label.rectTransform;

                        // ⚠️ NOT INSIDE AN `if`. § 101.1: *an assertion inside an `if` is an
                        // assertion that can decide not to run*, and the test that should have
                        // caught the palette bug had exactly that shape.
                        Assert.LessOrEqual(rt.rect.width, band + 1.0f,
                            $"{name}: '{label.name}' on '{tile.name}' is {rt.rect.width:0} units "
                            + $"wide in a {band:0} unit band, so it hangs off the row.");

                        if (label.horizontalOverflow == HorizontalWrapMode.Wrap)
                        {
                            // A wrapped label's preferred WIDTH is inside its box by definition,
                            // so the only honest question is its height. § 102.4: the overflow
                            // was vertical and every check in the project measured horizontally.
                            Assert.LessOrEqual(label.preferredHeight, rt.rect.height + 1.0f,
                                $"{name}: '{label.name}' on '{tile.name}' wraps to "
                                + $"{label.preferredHeight:0} units in a {rt.rect.height:0} unit "
                                + "box. verticalOverflow is Truncate here, so the extra line is "
                                + "dropped and nothing says so.");
                            continue;
                        }

                        Assert.LessOrEqual(label.preferredWidth, rt.rect.width + 1.0f,
                            $"{name}: '{label.name}' on '{tile.name}' needs "
                            + $"{label.preferredWidth:0} units in a {rt.rect.width:0} unit box "
                            + "and does not wrap, so it draws over whatever is beside it.");
                    }
                }

                Assert.Greater(measured, 0,
                    $"{name}: the slot drew no labels, so this proves nothing.");
                report.AppendLine($"{name,-14} {w}x{h}  {measured} labels on {Tiles().Count} readings");
            }

            Debug.Log($"[LoadoutSurfaceProbe]\n{report}");
        }

        // -------------------------------------------------------------------
        // § THE HARNESS
        // -------------------------------------------------------------------

        /// <summary>
        /// Boots the picker on Hero Strike, presses the LOADOUT door and waits for the board.
        ///
        /// ⚠️ HERO STRIKE, AND STATED RATHER THAN INHERITED. Ability builds is Hero Strike only
        /// (`VISION.md` § 1.1: Classic has no kit) and `BuildStageDoors` does not build the
        /// LOADOUT door at all in Classic. § 114.11 records this exact fixture inheriting Classic
        /// and never photographing the surface.
        /// </summary>
        private IEnumerator OpenBoard()
        {
            var settings = Settings.SettingsStore.Current;
            Assert.IsNotNull(settings, "no settings store, so there is nothing to equip into");

            _builds = settings.HeroBuilds ?? new List<HeroBuild>();
            _challenges = settings.AbilityChallenges ?? new List<AbilityChallengeProgress>();
            settings.HeroBuilds = new List<HeroBuild>();
            settings.AbilityChallenges = new List<AbilityChallengeProgress>();

            // ⚠️⚠️ PINNED, BECAUSE SETTING THE MODE WAS NOT ENOUGH AND THIS PROBE SPENT FOUR
            // FULL RUNS BEING BLAMED FOR IT. `ConvertedMatchSetup` restores
            // `GameSettings.CustomRulesWire` when it opens, so a mode chosen before the scene
            // load was silently replaced by whatever this machine last saved: here that wire read
            // `0|0|8|90|...`, Classic with eight rounds, and Classic builds no LOADOUT door.
            // **The probe was right and the product was wrong**; `docs/TODO.md` § 143.18 is the
            // defect and `SceneFlow.PinSelectedRules` is what a deliberate choice uses now.
            SceneFlow.PinSelectedRules(Core.CustomGameRules.Defaults(GameMode.HeroStrike));

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            yield return new WaitForSecondsRealtime(1.0f);

            _panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(_panel, "MatchSetup has no CharacterSelectPanel to open");
            _panel.SetActive(true);

            for (int i = 0; i < 6; i++) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            yield return Reopen();
        }

        /// <summary>
        /// The painted skills screen, looked up by its canvas.
        ///
        /// ⚠️⚠️ IT IS NOT UNDER `_panel` AND THAT IS WHY EVERY LOOKUP IN THIS FILE MOVED.
        /// `OwnerUiLayout.Canvas` builds its root DETACHED and binds lifetime through
        /// `CanvasLifetime` (§ 111.2), so the skills screen is a scene-root SIBLING of the
        /// character select panel that opened it. `Under(_panel.transform, ...)` therefore
        /// answered null about a screen drawing perfectly well, which is the shape of most of
        /// § 153.18.
        /// </summary>
        private static Transform SkillsScreen()
        {
            var canvas = Find(Skills);
            Assert.IsNotNull(canvas,
                "pressing SKILLS drew no " + Skills + ". `TumpPickerView` builds the TumpSkills "
                + "door and `TumpSkillView.Open` builds the screen behind it.");
            return canvas.transform;
        }

        /// <summary>
        /// Presses the LOADOUT door and waits for the board to be built.
        ///
        /// ⚠️⚠️ THROUGH THE DOOR, NEVER THROUGH `ToggleLoadoutBoard`. Reflection would open the
        /// board in one line and every case in this file would then pass on a build where the
        /// chip had been deleted, which is precisely the failure that made this whole file stale:
        /// the surface moved and nothing noticed. See the class note.
        ///
        /// ⚠️ THE BOARD IS CLOSED FIRST IF IT IS ALREADY OPEN, because `ToggleLoadoutBoard` is a
        /// toggle and a second press on an open board would close it. Reopening is how the
        /// persistence cases get a genuinely rebuilt board rather than the one they equipped on.
        /// </summary>
        private IEnumerator Reopen()
        {
            // ⚠️ THE DOOR IS NOT A TOGGLE ANY MORE, SO REOPENING MEANS BACKING OUT FIRST.
            // `ToggleLoadoutBoard` used to close an open board on a second press; `TumpSkills`
            // only ever opens. Leaving through `TumpSkillBack` is what a player does and it is
            // also what makes the persistence cases honest: the screen is genuinely rebuilt
            // rather than reused.
            var open = Find(Skills);
            if (open != null && open.activeSelf)
            {
                PressIn(open.transform, "TumpSkillBack");
                yield return null;
                yield return null;
            }

            PressIn(Picker(), Door);
            yield return null;
            yield return null;

            var screen = Find(Skills);
            Assert.IsNotNull(screen,
                "pressing SKILLS built no skills screen. " + S153 + " moved the ability builds off "
                + "the board onto `TumpSkillView`; `TumpPickerView.Collection` builds the door "
                + "and `TumpSkillView.Open` builds the screen.");
            Assert.IsTrue(screen.activeSelf, "the skills screen was built and left switched off.");

            Assert.IsNotNull(Under(screen.transform, Choices),
                "the skills screen drew no " + Choices + " column, so there is nothing to choose "
                + "between.");
        }

        private const string S153 = "docs/TODO.md section 153.20";

        /// <summary>The picker's own canvas, which is where the SKILLS door lives.</summary>
        private static Transform Picker()
        {
            var canvas = Find("OwnerLoadoutCanvas");
            Assert.IsNotNull(canvas,
                "the character select panel drew no OwnerLoadoutCanvas, so there is no picker to "
                + "open the skills from. `TumpPickerView.Build` is the builder.");
            return canvas.transform;
        }

        private void Press(string node) => PressIn(SkillsScreen(), node);

        private static void PressIn(Transform scope, string node)
        {
            var t = Under(scope, node);
            Assert.IsNotNull(t,
                $"no '{node}' under '{scope.name}'. If the control has been renamed, rename it "
                + "here in the same commit; " + S153 + " is the table of the last time this "
                + "happened to five cases at once.");

            var button = t.GetComponentInChildren<Button>(true);
            Assert.IsNotNull(button, $"'{node}' has no Button on it, so it is not a door.");
            button.onClick.Invoke();
        }

        // -------------------------------------------------------------------

        private string CurrentHero()
        {
            var column = Under(SkillsScreen(), Choices);
            if (column == null) return "";

            // The readings are named for the variants they carry, and every variant id opens
            // with its hero and its slot: `dante.1.tremor`. Reading the hero off the SCREEN
            // rather than off the picker's private cursor keeps this measuring what is DRAWN.
            foreach (Transform child in column)
            {
                if (!child.name.StartsWith("TumpVariant_")) continue;
                string id = child.name.Substring("TumpVariant_".Length);
                int dot = id.IndexOf('.');
                if (dot > 0) return id.Substring(0, dot);
            }

            return "";
        }

        private List<Transform> Tiles()
        {
            var found = new List<Transform>();
            var column = Under(SkillsScreen(), Choices);
            if (column == null) return found;

            foreach (Transform child in column)
                if (child.name.StartsWith("TumpVariant_")) found.Add(child);

            return found;
        }

        private Transform Tile(string variantId)
        {
            foreach (var t in Tiles())
                if (t.name == "TumpVariant_" + variantId) return t;

            return null;
        }

        /// <summary>What one named label on the skills screen currently says.</summary>
        private static string Words(string node)
        {
            var t = Under(SkillsScreen(), node);
            Assert.IsNotNull(t, $"the skills screen has no '{node}'.");
            var label = t.GetComponent<Text>();
            Assert.IsNotNull(label, $"'{node}' carries no Text.");
            return label.text;
        }

        /// <summary>Every word drawn on one tile, joined, so a case can ask what it says.</summary>
        private static string TextIn(Transform tile)
        {
            var sb = new StringBuilder();
            foreach (var label in tile.GetComponentsInChildren<Text>(true))
                sb.Append(label.text).Append("  ");

            return sb.ToString();
        }

        /// <summary>The one non-default reading of a slot.</summary>
        private static AbilityVariant Alternate(string heroId, int slot)
        {
            foreach (var option in HeroLoadoutRules.VariantsFor(heroId, slot))
                if (!option.IsDefault) return option;

            Assert.Fail($"{heroId} slot {slot} has no alternate, so there is no choice to make.");
            return null;
        }

        private static Transform Under(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;

            return null;
        }

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;

            return null;
        }

        // -------------------------------------------------------------------
        // § RESOLUTION
        //
        // ⚠️⚠️ A CANVAS IS DRIVEN THROUGH A RENDER TEXTURE, NOT BY RESIZING THE WINDOW. There is
        // no window in batch mode, and `Screen.SetResolution` is a request the player honours on
        // some later frame or not at all. `AspectRatioProbes` does the same thing for the same
        // reason: point the canvas at a camera, point the camera at a texture of the size you
        // want, and the `CanvasScaler` computes exactly what it would compute on that display.
        // -------------------------------------------------------------------

        private IEnumerator DriveCanvases()
        {
            var camGo = new GameObject("LoadoutProbeCamera");
            _camera = camGo.AddComponent<Camera>();
            _camera.enabled = false;

            foreach (var canvas in Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _camera;
                _canvases.Add(canvas);
            }

            yield return null;
        }

        private IEnumerator Resize(int w, int h)
        {
            if (_target != null) _target.Release();

            _target = new RenderTexture(w, h, 24);
            _camera.targetTexture = _target;

            foreach (var c in _canvases)
            {
                if (c == null) continue;
                var scaler = c.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = false;
                if (scaler != null) scaler.enabled = true;
            }

            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
        }
    }
}
