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

        private const string Board = "LoadoutBoard";
        private const string Door = "LoadoutDoor";

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
            Assert.IsNotEmpty(heroId, "the picker is on no hero, so the board has nothing to draw");

            var tiles = Tiles();
            Assert.AreEqual(4, tiles.Count,
                $"the board drew {tiles.Count} variant tiles. A hero has two skills and each has "
                + "two readings, so anything else means the board is drawing a kit that is not "
                + "the one on the stage. docs/TODO.md § 122.5.");

            foreach (var tile in tiles)
                StringAssert.StartsWith("Variant_" + heroId + ".", tile.name,
                    $"'{tile.name}' is on {heroId}'s board. Every tile has to belong to the hero "
                    + "the picker is showing, or the board is a flat list of everybody's builds "
                    + "again.");

            Assert.AreEqual(2, Glyphs().Count,
                "the board drew something other than two slot heads, so it is not showing one "
                + "hero's two skills.");
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
        /// </summary>
        [UnityTest]
        public IEnumerator BothSlotHeadsCarryTheAbilitysOwnGlyph()
        {
            yield return OpenBoard();

            var glyphs = Glyphs();
            Assert.AreEqual(2, glyphs.Count, "the board has no pair of slot heads to check.");

            foreach (var glyph in glyphs)
            {
                var image = glyph.GetComponent<Image>();
                Assert.IsNotNull(image, "a SlotGlyph with no Image on it.");
                Assert.IsNotNull(image.sprite,
                    "a SlotGlyph with no sprite, which draws as a solid square. "
                    + "AbilityIcons.For answered nothing for this ability's Glyph.");
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
            Assert.IsNotNull(tile, $"no tile for '{alternate.Id}' on the board.");

            string words = TextIn(tile);
            StringAssert.Contains("LOCKED", words.ToUpperInvariant(),
                $"the locked tile reads '{words}'. A control that offers a choice the game will "
                + "refuse has to say so before it is pressed.");

            StringAssert.Contains(alternate.Challenge, words,
                $"the locked tile reads '{words}' and never says what the challenge is.");
            StringAssert.Contains($"0 / {alternate.ChallengeTarget}", words,
                $"the locked tile reads '{words}' and never says how far along the player is. "
                + "A challenge with no counter is a wish.");

            tile.GetComponent<Button>().onClick.Invoke();
            yield return null;

            // ⚠️ THE STORE, NOT THE LABEL. The words above are what the player reads; this is
            // what the match will actually run.
            var build = HeroBuildRules.RowFor(settings.HeroBuilds, heroId);
            Assert.AreNotEqual(alternate.Id, build.Slot1VariantId,
                "pressing a locked tile wrote it into settings.json. It is refused again at "
                + "`CheckedHeroBuildFor`, so the match is safe, but the screen would then show a "
                + "build the game is not running.");

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
            Assert.IsNotNull(tile, $"no tile for '{alternate.Id}' after the ledger was filled.");
            tile.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.AreEqual(alternate.Id,
                            HeroBuildRules.RowFor(settings.HeroBuilds, heroId).Slot1VariantId,
                "an unlocked alternate was not written to the store when its tile was pressed.");

            // ⚠️ THE REBUILD IS THE ASSERTION. Everything above is still in one view.
            yield return Reopen();

            var again = Tile(alternate.Id);
            Assert.IsNotNull(again, $"'{alternate.Id}' lost its tile after the board was rebuilt.");
            StringAssert.Contains("EQUIPPED", TextIn(again).ToUpperInvariant(),
                "after rebuilding the board the equipped tile does not say so, so the choice did "
                + "not survive CLOSE and the player has no way to tell what they are bringing.");

            Assert.AreEqual(alternate.Id,
                            Settings.SettingsStore.CheckedHeroBuildFor(heroId).Slot1VariantId,
                "the tile shows the alternate and the checked build does not, so the screen and "
                + "the match disagree about what this player is bringing.");
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
                            + $"wide in a {band:0} unit band, so it hangs off the tile.");

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
                    $"{name}: the board drew no labels, so this proves nothing.");
                report.AppendLine($"{name,-14} {w}x{h}  {measured} labels on 4 tiles");
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

            SceneFlow.SelectedMode = GameMode.HeroStrike;

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
            if (Under(_panel.transform, Board) != null)
            {
                Press(Door);
                yield return null;
                yield return null;
            }

            Press(Door);
            yield return null;
            yield return null;

            Assert.IsNotNull(Under(_panel.transform, Board),
                "pressing LOADOUT built no board. § 122.5 moved the ability builds from the hub "
                + "onto this screen; `ConvertedCharacterSelect.BuildLoadoutBoard` is the builder "
                + "and `BuildStageDoors` is the door.");
        }

        private void Press(string node)
        {
            var t = Under(_panel.transform, node);
            Assert.IsNotNull(t,
                $"no '{node}' on the character select stage. In Hero Strike `BuildStageDoors` "
                + "builds LOADOUT above MAKE YOUR OWN; if the door has been renamed, rename it "
                + "here in the same commit.");

            var button = t.GetComponentInChildren<Button>(true);
            Assert.IsNotNull(button, $"'{node}' has no Button on it, so it is not a door.");
            button.onClick.Invoke();
        }

        // -------------------------------------------------------------------

        private string CurrentHero()
        {
            var board = Under(_panel.transform, Board);
            if (board == null) return "";

            // The tiles are named for the variants they carry, and every variant id opens with
            // its hero: `dante.1.tremor`. Reading the hero off the board rather than off the
            // picker's private cursor keeps this measuring what is DRAWN.
            foreach (Transform child in board)
            {
                if (!child.name.StartsWith("Variant_")) continue;
                string id = child.name.Substring("Variant_".Length);
                int dot = id.IndexOf('.');
                if (dot > 0) return id.Substring(0, dot);
            }

            return "";
        }

        private List<Transform> Tiles()
        {
            var found = new List<Transform>();
            var board = Under(_panel.transform, Board);
            if (board == null) return found;

            foreach (Transform child in board)
                if (child.name.StartsWith("Variant_")) found.Add(child);

            return found;
        }

        private List<Transform> Glyphs()
        {
            var found = new List<Transform>();
            var board = Under(_panel.transform, Board);
            if (board == null) return found;

            foreach (Transform child in board)
                if (child.name == "SlotGlyph") found.Add(child);

            return found;
        }

        private Transform Tile(string variantId)
        {
            foreach (var t in Tiles())
                if (t.name == "Variant_" + variantId) return t;

            return null;
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
