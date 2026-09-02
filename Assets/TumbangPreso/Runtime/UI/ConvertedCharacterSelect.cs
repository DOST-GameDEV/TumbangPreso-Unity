using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `character_select.gd`.
    ///
    /// ⚠️⚠️ THREE TABS, AND EACH RENAMES THE SAME THREE KEYS. The keys are bilis, lakas and
    /// tatag and they never change; only the LABELS differ per tab. Renaming a key to match its
    /// label is a silent flat-3 fallback on every entry, because a missing key resolves to
    /// neutral without erroring.
    ///
    /// ⚠️ RECOVERY IS ON tatag AND RESET IS ON bilis. They read alike and sit on different
    /// keys. Check the key, never the word.
    /// </summary>
    public sealed class ConvertedCharacterSelect : ConvertedScreen
    {
        /// <summary>Raised when the panel closes, so the setup screen can re-read the picks.</summary>
        public event System.Action Closed;

        private static readonly string[] TabNames = { "PERSON", "LATA", "TSINELAS" };

        private static readonly string[][] MeterLabels =
        {
            new[] { "SPEED", "POWER", "GRIT" },
            new[] { "RESET", "REBOUND", "STANCE" },
            new[] { "FLIGHT", "IMPACT", "RECOVERY" },
        };

        private int _tab;
        private readonly int[] _pick = new int[3];

        private Texture2D _backdropTexture;
        private Texture2D _glowTexture;
        private Texture2D _scrimTexture;
        private Sprite _backdropSprite;
        private Sprite _glowSprite;
        private Sprite _scrimSprite;
        private Image _glowImage;

        protected override void Wire()
        {
            ConfigureGodotBackdrop();
            // ⚠️ 66 IS THE SIZE THE SCENE AUTHORS IT AT, and it is passed in rather than read so
            // the fit starts from the same place every time this screen refreshes. See
            // `SetHeadline`: "CHOOSE YOUR LOADOUT" is nineteen characters into a 424 px box.
            SetHeadline("GameBannerLabel", SceneFlow.SelectedMode == GameMode.HeroStrike
                ? "CHOOSE YOUR HERO"
                : "CHOOSE YOUR LOADOUT", 66);

            var s = Settings.SettingsStore.Current;
            _pick[0] = Mathf.Max(0, s.CharacterPick);
            _pick[1] = Mathf.Max(0, s.CanPick);
            _pick[2] = Mathf.Max(0, s.SlipperPick);

            OnClick("CharPrevButton", () => CycleEntry(-1));
            OnClick("CharNextButton", () => CycleEntry(1));
            OnClick("ConfirmButton", Confirm);
            OnClick("BackButton", Dismiss);

            WireTabs();

            // ⚠️⚠️ ONE CALL DRESSES THIS WHOLE SCREEN IN PAPER, AND IT IS SCOPED TO THIS SUBTREE
            // ON PURPOSE. `GodotPanel` and `GodotButton` are the choke points every converted
            // screen is skinned through, so editing either of them would have repainted the main
            // menu and the in-match HUD, which 🧑 scoped out twice. `PaperKit.PaperDress.Screen`
            // walks a given root instead. See `docs/TODO.md` § 119.2 and § 119.5.
            //
            // ⚠️⚠️ AND IT RUNS BEFORE `Refresh`, WHICH IT DID NOT, AND THAT ORDER IS A BUG YOU CAN
            // SEE IN `Logs/shots-runtime/CharacterSelect-v57.png`: the HERO tab, the one you are
            // on, drew as a pale greyed pill. `RefreshTabs` asks for a `PaperSkin` and takes a
            // wooden fallback when there is none, so running it first meant it always took the
            // fallback, and the dress then flattened all three tabs onto one `Token`. With
            // `interactable = false` on the live tab (which is deliberate: you cannot press the
            // tab you are on) a plain `Token` draws as `Pose.Off`. **The selected tab was the
            // greyed-out one.**
            PaperDress.Screen(transform);

            PaperiseAuthoredBoard();

            Refresh();
        }

        /// <summary>
        /// Turns the picker's two authored wooden surfaces into paper.
        ///
        /// ⚠️⚠️ THIS IS THE ONE PLACE IN THE PASS THAT STOPS DRAWING ONE OF 🧑'S OWN PNGs, AND IT
        /// NEEDS SAYING OUT LOUD. `CharacterSelectPanel/ConfigPanel` draws
        /// `Art/ui/host-game/SETTINGS CONFIG PANEL.png` and `NameRow/CharSelector` draws
        /// `MAP MODE DISPLAY.png`; both are his art, `CLAUDE.md` § 6.4 forbids repainting it, and
        /// `PaperDress` cannot see either of them because a bare `Image` with an authored sprite
        /// carries no `GodotPanel` and no `WoodSkin`. **That is why they survived the paper pass
        /// and why the picker shipped as cream furniture standing on a dark wooden board.**
        ///
        /// ⚠️ WHAT MAKES THIS ALLOWED IS THAT IT IS A COMPOSITION CHANGE AND NOT A REPAINT. The
        /// files are untouched, the main menu still draws them, and it is the same decision the
        /// lobby already made: `LobbyChrome.BuildSettingsDrawer` takes `Rows` out of the authored
        /// `ConfigPanel` and leaves the board behind, because on a cream front end the biggest
        /// object in the frame cannot be the wooden one. 🧑 asked for this screen by name:
        /// **"MAKE SURE AS WELL CHARACTER SELECT ... HAS THE NEW THEME"**, and gave the permission
        /// in advance (§ 119, *"i give u permission to overhaul"*).
        ///
        /// ⚠️ HIS ART THAT IS A CONTROL STAYS. `GAME BANNER.png` is still the headline, the two
        /// arrows are still the arrows, and `BUTTON LONG.png` is still CHOOSE. § 119.1: wood is
        /// the ink, the frame and his own authored buttons standing on paper. The board was none
        /// of the three; it was the field.
        /// </summary>
        private void PaperiseAuthoredBoard()
        {
            var board = Node("ConfigPanel");
            if (board != null) PaperKit.Paperise(board.gameObject, PaperCraft.Surface.Sheet);

            // ⚠️ A `Tray`, because the selector is a VALUE with an arrow either side rather than
            // a thing you press: the arrows are the controls and they keep their own art.
            var selector = Node("CharSelector");
            if (selector != null) PaperKit.Paperise(selector.gameObject, PaperCraft.Surface.Tray);
        }

        /// <summary>
        /// Recreates the three generated textures in Godot's CharacterSelect.tscn. Older
        /// converted scenes flattened each GradientTexture2D to its first colour, which is why
        /// the Unity screen became a washed-out grey sheet instead of the slate-to-midnight
        /// stage shown in the reference captures.
        /// </summary>
        private void ConfigureGodotBackdrop()
        {
            _backdropTexture = VerticalBackdrop();
            _glowTexture = RadialGlow();
            _scrimTexture = HorizontalScrim();

            _backdropSprite = ApplyTexture("Backdrop", _backdropTexture);
            _glowSprite = ApplyTexture("BackdropGlow", _glowTexture);
            _scrimSprite = ApplyTexture("Scrim", _scrimTexture);
            _glowImage = Node("BackdropGlow")?.GetComponent<Image>();
        }

        private Sprite ApplyTexture(string nodeName, Texture2D texture)
        {
            var node = Node(nodeName);
            if (node == null || texture == null) return null;

            var image = node.GetComponent<Image>();
            if (image == null) return null;

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                                       new Vector2(0.5f, 0.5f), 100.0f);
            sprite.name = $"CharacterSelect_{nodeName}";
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            return sprite;
        }

        private static Texture2D VerticalBackdrop()
        {
            const int height = 256;
            var texture = NewTexture(8, height, "CharacterSelect_Backdrop");
            var pixels = new Color[texture.width * texture.height];

            // ⚠️⚠️ THIS WAS THREE STOPS OF NAVY AND IT IS THE BLUE 🧑 PHOTOGRAPHED ON 2026-09-01.
            // *"i dont want to see blue shit, thats not in theme"*, over a shot of the hero
            // picker sitting on a slate-to-midnight sheet. The paragraph this replaces called
            // that "the game's Bayan navy identity", which was a claim about a colour nothing
            // else in the front end uses: `CLAUDE.md` § 6.4 and `VISION.md` § 6 both name the
            // palette as wood, cream, amber and ink, and say outright that anything in a
            // different visual language is the thing that looks broken.
            //
            // ⚠️ THE THREE-STOP SHAPE IS KEPT AND ONLY THE HUE MOVES, which is what the note it
            // replaces was right about: the gradient's job is to sit the wood panel and the amber
            // banner on something with depth, and a flat fill loses the stage. Top is
            // `UiTheme.WoodEdge` lifted, middle is `WoodMid`, bottom is `WoodDeep`.
            //
            // ⚠️⚠️ AND ON 2026-09-02 IT IS PAPER, BECAUSE THIS SCREEN IS WIRED TO THE LOBBY AND
            // THE LOBBY IS CREAM. 🧑: **"MAKE SURE AS WELL CHARACTER SELECT AS WELL AS EVERYTHING
            // WIRED TO LOBBY HAS THE NEW THEME"**. `PaperDress.Screen` at the top of this file has
            // been converting the PANELS on this screen since the paper pass, so what shipped was
            // cream furniture standing on a dark wooden stage: **half of one language and half of
            // another, on the screen a player reaches from the lobby's FIGHTER row.**
            //
            // ⚠️ THE SHAPE IS UNCHANGED FOR THE REASON ABOVE. It is still three stops with the
            // light at the top, so the panel and the model still sit on something rather than on
            // a flat fill; the values are 4 per cent apart rather than 40, which is what a sheet
            // of paper under a raking light actually is. `Paper` at the top, `Paper` through the
            // middle and `PaperWarm` at the floor, which is the same pair every `Tray` in the
            // front end is cut out of.
            var top = WoodCraft.Lift(UiTheme.Paper, 0.02f);
            var middle = UiTheme.Paper;
            var bottom = UiTheme.PaperWarm;

            for (int y = 0; y < height; y++)
            {
                // Texture pixels run bottom-up; Godot's gradient offsets run top-down.
                float t = 1.0f - y / (float)(height - 1);
                Color colour = t <= 0.55f
                    ? Color.Lerp(top, middle, t / 0.55f)
                    : Color.Lerp(middle, bottom, (t - 0.55f) / 0.45f);

                for (int x = 0; x < texture.width; x++)
                    pixels[y * texture.width + x] = colour;
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D RadialGlow()
        {
            const int size = 256;
            var texture = NewTexture(size, size, "CharacterSelect_Glow");
            var pixels = new Color[size * size];
            var centre = new Vector2(0.70f, 1.0f - 0.42f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
                float t = Mathf.Clamp01(Vector2.Distance(uv, centre) / 0.45f);
                float alpha = t <= 0.45f
                    ? Mathf.Lerp(0.30f, 0.13f, t / 0.45f)
                    : Mathf.Lerp(0.13f, 0.0f, (t - 0.45f) / 0.55f);
                pixels[y * size + x] = new Color(1.0f, 1.0f, 1.0f, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D HorizontalScrim()
        {
            const int width = 256;
            var texture = NewTexture(width, 8, "CharacterSelect_Scrim");
            var pixels = new Color[texture.width * texture.height];
            // ⚠️ THE SAME REPAINT AS `VerticalBackdrop`. This scrim was the same navy, and it is
            // the layer the wood panel actually sits on, so leaving it would have kept a cold
            // edge down the middle of a screen whose background had just gone warm.
            // ⚠️⚠️ AND ON A PAPER FIELD IT IS A SHADE RATHER THAN A DIM, WHICH IS `CLAUDE.md`
            // § 6.2c QUESTION 3 ASKED AGAIN AFTER THE BACKGROUND CHANGED. This ran at 85 per cent
            // `WoodDark` down the left edge, and its whole job was to buy the wood panel some
            // separation from a wood backdrop of nearly the same value. The backdrop is cream
            // now, the panel is cut paper with its own halo and its own cast shadow, and 85 per
            // cent of a near-black over that would be a black bar down a third of the screen
            // protecting nothing. **A scrim is not decoration and it is not free**; when the thing
            // it protected against goes, the number goes with it.
            //
            // ⚠️ IT IS NOT DELETED, BECAUSE THE LEFT THIRD IS WHERE THE MODEL STANDS and a warm
            // shade under it is what stops a voxel character floating on a flat sheet. 14 per cent
            // of `PaperSunk` composites about three value steps down, which is the same weight
            // `UiRows.Band` arrived at from the other direction.
            var ink = UiTheme.PaperSunk;

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float alpha;
                if (t <= 0.36f) alpha = Mathf.Lerp(0.14f, 0.11f, t / 0.36f);
                else if (t <= 0.62f) alpha = Mathf.Lerp(0.11f, 0.03f, (t - 0.36f) / 0.26f);
                else alpha = Mathf.Lerp(0.03f, 0.0f, (t - 0.62f) / 0.38f);

                for (int y = 0; y < texture.height; y++)
                    pixels[y * texture.width + x] = new Color(ink.r, ink.g, ink.b, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D NewTexture(int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            return texture;
        }

        private void OnDestroy()
        {
            Destroy(_backdropSprite);
            Destroy(_glowSprite);
            Destroy(_scrimSprite);
            Destroy(_backdropTexture);
            Destroy(_glowTexture);
            Destroy(_scrimTexture);
        }

        /// <summary>
        /// One button per category, built from the roster rather than authored, exactly as
        /// `character_select.gd::_build_tabs` does it: adding a fourth category is then one
        /// entry in the roster and nothing in the scene changes.
        ///
        /// ⚠️ THE SHOWING TAB IS DISABLED RATHER THAN MERELY RESTYLED. The wood set already
        /// draws disabled as the sunk face, so that gets the "pushed in" read for free and, more
        /// usefully, makes the current tab unclickable: pressing the tab you are already on
        /// should do nothing.
        /// </summary>
        /// <summary>
        /// One size for every cell in the tab rail, including the door on the end of it.
        ///
        /// ⚠️ IT IS A CONSTANT BECAUSE IT WAS TWO NUMBERS IN TWO METHODS AND THEY DISAGREED. The
        /// three tabs took whatever `MenuKit.WoodButton` derived from the box they were handed and
        /// the door hard-coded `MenuKit.MinReadableUnits`; nothing connected the two, so the row
        /// shipped in two sizes. `docs/TODO.md` § 121.5.
        /// </summary>
        private const int TabLabelSize = 20;

        /// <summary>
        /// Fits one cell's lettering to the box the layout group actually gave it.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE SETTING ONE SIZE FOR THE ROW OVERFLOWED TWO OF THE FOUR CELLS.
        /// `Logs/crops/picker-tabs-v61b.png`: at 22 units `TSINELAS` and `MAKE YOUR OWN` drew
        /// **outside their own pills**. `MenuKit.WoodButton` fits a label to the size it is
        /// HANDED, and every cell in this rail is handed a number (180, or 300 for the door) that
        /// the `HorizontalLayoutGroup` discards a frame later: the three tabs end up at about 124
        /// units and the door at about 187. **A width passed to a control inside a layout group is
        /// not that control's width**, which is `CLAUDE.md` § 6.2c question 1 in the one form this
        /// file keeps meeting it.
        ///
        /// ⚠️ SO IT MEASURES THE RECT RATHER THAN TRUSTING A CONSTANT, and it runs from
        /// `RefreshTabs`, which happens after a layout pass rather than during construction. That
        /// is the two-step `BuildCustomDoor` has always done for the door alone and the other
        /// three cells never did.
        /// </summary>
        private static void FitTabLabel(Button button)
        {
            if (button == null) return;

            var label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;

            label.fontSize = TabLabelSize;

            // ⚠️⚠️ THE LAYOUT IS FORCED BEFORE THE RECT IS READ, AND WITHOUT IT THE FIT MEASURES
            // A BOX THAT DOES NOT EXIST YET. `Logs/crops/picker-tabs-final.png` is the receipt for
            // the version that did not: `MAKE YOUR OWN` still ran outside its own pill, because
            // `rect.width` was still the 300 `BuildCustomDoor` passed to `MenuKit.WoodButton`
            // rather than the ~187 the `HorizontalLayoutGroup` gives it. **An un-laid-out rect
            // reports the size somebody typed, not the size it will have**, which is the same
            // finding § 120.5 row 4 records for the settings footer and the reason that fix waits
            // on a forced canvas update too.
            var rt = (RectTransform)button.transform;
            var bar = rt.parent as RectTransform;
            if (bar != null) LayoutRebuilder.ForceRebuildLayoutImmediate(bar);

            // ⚠️ 14 AS THE FLOOR RATHER THAN 18, AND ONLY HERE. A tab's lettering is a NAME on a
            // control the player can also see the shape and position of, and this rail has to
            // hold `MAKE YOUR OWN` beside `LATA` in cells the layout group decides. The
            // alternative is the label drawing outside its own pill, which is what it was doing.
            float room = rt.rect.width - 24.0f;
            if (room > 1.0f) MenuKit.Fit(label, room, 14);
        }

        private void WireTabs()
        {
            var bar = Node("TabBar");
            if (bar == null) return;

            for (int i = bar.childCount - 1; i >= 0; i--) Destroy(bar.GetChild(i).gameObject);

            // ⚠️⚠️ THE CELLS NEED AIR BETWEEN THEM AND THE AUTHORED BAR GIVES THEM NONE.
            // `Logs/crops/picker-tabs-final.png`: four pills touching along a 560-unit rail, which
            // reads as one segmented plank rather than as four controls. `PlayerHub.BuildTabColumn`
            // hit the identical fault from the other axis and `docs/TODO.md` § 121.10 row 3 is the
            // silhouette half of it. `PaperKit.Gap` is the one spacing constant in this front end
            // (see its note: *"One spacing constant used everywhere is what makes a screen feel
            // calm without anybody being able to point at why"*), so it is the number here too
            // rather than a literal chosen for this rail.
            var barLayout = bar.GetComponent<HorizontalLayoutGroup>();
            if (barLayout != null) barLayout.spacing = PaperKit.Gap;

            _tabButtons.Clear();

            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;

                string tabName = i == 0 && SceneFlow.SelectedMode == GameMode.HeroStrike
                    ? "HERO"
                    : TabNames[i];
                var button = MenuKit.WoodButton(bar, tabName, Vector2.zero, Vector2.zero,
                                                new Vector2(180.0f, 56.0f), () =>
                                                {
                                                    _tab = index;
                                                    MenuSfx.Click();
                                                    Refresh();
                                                });

                var element = button.gameObject.AddComponent<LayoutElement>();
                element.preferredHeight = 56.0f;
                element.flexibleWidth = 1.0f;

                // ⚠️ EVERY CELL IN THIS RAIL IS ONE SIZE. See `TabLabelSize`: the door on the end
                // set its own and the row shipped in two.
                var tabLabel = button.GetComponentInChildren<Text>(true);
                if (tabLabel != null) tabLabel.fontSize = TabLabelSize;

                _tabButtons.Add(button);
            }

            BuildCustomDoor(bar);
        }

        /// <summary>
        /// The one door to the character creator, as the fourth control in the bar you land on.
        ///
        /// ⚠️⚠️ IT WAS A 200-UNIT CHIP ON THE END OF THE `STRENGTH` STRIP AND 🧑 COULD NOT FIND
        /// IT: *"and how do u even get to this"*, of a screen he had opened by other means. That
        /// is `docs/TODO.md` § 96 happening a second time, in the same shape: **a door placed
        /// wherever there happened to be room, rather than where the player is looking.** The hub
        /// put its only door in a corner chip that read as a status readout and the person who
        /// commissioned the hub never found it; this put its only door at the end of a row of
        /// colour-strength chips, in the one visual slot that says "another option for the control
        /// to my left".
        ///
        /// ⚠️⚠️ AND IT IS THE SAME DOOR MOVED, NOT A SECOND ONE. `CLAUDE.md` § 6.3: *"NEVER ADD A
        /// SECOND DOOR TO FIX A FINDABILITY PROBLEM. Fix the door or move it."* The chip is gone
        /// in the same commit. `RefreshCustomDoor` no longer exists.
        ///
        /// ⚠️⚠️ THE TAB BAR IS WHERE IT GOES BECAUSE IT COSTS NO VERTICAL BUDGET, AND THAT
        /// CONSTRAINT IS REAL RATHER THAN INHERITED. The previous note here recorded why a third
        /// strip row was refused: `HeroPickerLayoutProbe` dumps `Rows h=460 pref=644`, so the
        /// vertical group is already compressing every child to fit, and a new row reopens the
        /// 27 px dead band above the ability rows that § 94 records being "fixed" three times.
        /// **The bar is a `HorizontalLayoutGroup` that already exists**, so a fourth cell costs
        /// width the row has and height it does not have to find.
        ///
        /// ⚠️ IT IS AMBER RATHER THAN GREEN. `GodotTheme.ForButton`: green is ACT and it is
        /// CHOOSE, which is forty units below this. Two greens on one screen is two "press me"
        /// buttons with the more important one further from the hand, and the chip this replaces
        /// was `WoodPrimaryButton` sitting directly above a `WoodPrimaryButton` CHOOSE.
        ///
        /// ⚠️ AND 1.7 OF FLEXIBLE WIDTH, NOT 1.0. `MAKE YOUR OWN` is thirteen characters against
        /// `LATA`'s four, and `childForceExpandWidth` would give both the same cell: the label
        /// would then be ground down by `MenuKit.Fit` toward the 18-unit floor while LATA sat in
        /// a box three times the size of its word.
        /// </summary>
        private void BuildCustomDoor(Transform bar)
        {
            var door = MenuKit.WoodButton(bar, "MAKE YOUR OWN", Vector2.zero, Vector2.zero,
                                          new Vector2(300.0f, 56.0f),
                                          () =>
                                          {
                                              MenuSfx.Click();
                                              CustomCharacterScreen.Ensure().Open();
                                          },
                                          // ⚠️ PLAIN WOOD. This is a DOOR to another screen, not
                                          // the action of this one, and § 117.3 reserved amber
                                          // for the "look here" marker and green for "go". A door
                                          // painted in the accent competes with the choice the
                                          // player came to this screen to make.
                                          "WoodButton");

            var element = door.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 56.0f;

            // ⚠️⚠️ 2.2 OF FLEX AND IT WAS 1.5, BECAUSE THE CELL COULD NOT HOLD THE WORDS AT THE
            // ROW'S OWN SIZE. `Logs/crops/picker-tabs-final.png`: at 1.5 the door gets about 160
            // units of a 560-unit rail, `MAKE YOUR OWN` needs about 147 at `TabLabelSize` 20, and
            // 24 units of that is padding — so `FitTabLabel` ground the label down to about 15 and
            // the row shipped in two sizes again. **That is the fault 🧑 named twice** (*"these
            // buttons look ugly"*, *"these diff fonts look ugly"*) arriving through the fitter
            // instead of through a literal.
            //
            // ⚠️ WIDEN THE CELL RATHER THAN SHRINK THE TYPE, and the arithmetic says it fits: at
            // 2.2 over 5.2 of total flex the door is about 237 units, which holds 147 of lettering
            // with 90 of air. Shrinking the type was the other option and it is the one that made
            // this row look wrong in the first place.
            element.flexibleWidth = 2.2f;

            _customDoor = door;

            // ⚠️⚠️ THE LABEL IS RE-FITTED AGAINST THE CELL THE LAYOUT GROUP GIVES IT, AND THE
            // FIRST RENDER OF THIS TAB IS WHY. `Logs/ui/10-picker-colours.png`, 2026-09-01: the
            // words `MAKE YOUR OWN` ran past the button's own rounded frame and out to the panel's
            // inner edge. **`MenuKit.WoodButton` sizes its label from the size it is HANDED**, and
            // the 300 x 56 passed above is discarded by the `HorizontalLayoutGroup` a frame later,
            // so the label was fitted to a box three times the cell it ended up in. This is the
            // same two-step `CustomCharacterScreen.BuildSectionTabs` already does for the same
            // reason; the difference there is that its cells are handed a zero size, so nobody
            // could forget.
            //
            // ⚠️ `MinReadableUnits` 18, WHICH IS THE FLOOR AND NOT A CHOICE. Thirteen characters at
            // 18 units measure about 120, and the cell is 560 by 1.5 over 4.5 of flex, about 187
            // before spacing. It fits with room; a longer caption here would not, which is the
            // argument for the verb and the noun and nothing else.
            var label = door.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                // ⚠️⚠️ THE SAME SIZE AS THE THREE TABS BESIDE IT, AND IT WAS FOUR UNITS SMALLER.
                // 🧑 2026-09-02, of this row: **"these buttons look ugly"**, and of the same fault
                // one control over, **"these diff fonts look ugly"**. `Logs/crops/picker-tabs-v61.png`
                // is the receipt: `HERO`, `LATA` and `TSINELAS` at `TabLabelSize` and
                // `MAKE YOUR OWN` at 18, in one rail, at one height. **Two sizes of one typeface
                // side by side read as two typefaces**, because the eye compares the letterforms
                // directly instead of scanning down a column.
                //
                // ⚠️ IT FITS AT THE BIGGER SIZE, WHICH IS WHY THIS IS SAFE. Thirteen characters at
                // 22 units measure about 147 and the cell is 560 by 1.5 over 4.5 of flex, about
                // 187 before spacing. `MenuKit.Fit` is still called against the cell rather than
                // against the 300 passed above, because the `HorizontalLayoutGroup` discards that
                // number a frame later; that is the two-step this method has always needed and
                // the reason `Logs/ui/10-picker-colours.png` once showed this label running past
                // its own frame.
                label.fontSize = TabLabelSize;
                MenuKit.Stretch(label.rectTransform, -8.0f);
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        private readonly List<Button> _tabButtons = new List<Button>();

        private void RefreshTabs()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var button = _tabButtons[i];
                if (button == null) continue;

                bool active = i == _tab;
                button.transition = Selectable.Transition.None;
                button.interactable = !active;

                // ⚠️⚠️ THIS METHOD USED TO WRITE A `GodotTheme.Box` STRAIGHT ONTO THE IMAGE, AND
                // THAT IS A LEFTOVER OF THE OLD FRONT END THAT NO PROBE COULD SEE. `Install` runs
                // `PaperDress.Screen` once; this runs on every selection change and every tab
                // press, AFTER it, and `PaperSkin.Rebuild` early-outs when the height and the
                // surface have not changed, so **it never puts the paper sprite back.** The tab
                // bar on this screen has therefore been an amber-and-near-black nine-patch since
                // the paper pass, on a screen whose panels were all cream.
                //
                // ⚠️ `PaperPurityProbe` WOULD HAVE CAUGHT THIS AND DOES NOT REACH HERE: it builds
                // the lobby and the login screen only (§ 119.6). That is the argument for the
                // shot this pass adds rather than for widening the probe, because the fault is
                // "a sprite written after the dress" and the probe walks a tree at rest.
                bool paper = PaperKit.MarkLive(button, active);

                if (!paper && button.targetGraphic is Image face)
                {
                    face.sprite = GodotTheme.Box(
                        active ? UiTheme.Highlight : UiTheme.WoodDark,
                        active ? UiTheme.Cream : UiTheme.WoodEdge,
                        active ? 3 : 2, 6);
                    face.type = Image.Type.Sliced;
                }

                // ⚠️ FITTED HERE RATHER THAN AT BUILD TIME, because this method runs after a
                // layout pass and `WireTabs` does not. See `FitTabLabel`.
                FitTabLabel(button);

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    // ⚠️ CREAM ON THE LIVE PILL AND INK ON THE OUTLINE, which is the pair every
                    // other tab row in the game now uses. It was ink on amber against cream on
                    // wood, which is two inversions in one row.
                    //
                    // ⚠️ ON THE PAPER PATH `PaperButton.Restyle` BELOW OWNS THE COLOUR, because it
                    // reads it off the surface and is therefore the one writer. Setting it here as
                    // well is how a live tab ends up with the right plate and the wrong word.
                    if (!paper) label.color = active ? UiTheme.Ink : UiTheme.Cream;
                    label.fontStyle = FontStyle.Bold;
                }
            }

            // ⚠️ THE DOOR IS THE FOURTH CELL OF THIS RAIL AND IS NOT IN `_tabButtons`, because it
            // is not a tab: it opens another screen. It still has to be the same SIZE as the three
            // beside it, which is the whole of *"these diff fonts look ugly"*, so it is fitted
            // here with them rather than left on the number `BuildCustomDoor` gave it.
            FitTabLabel(_customDoor);
        }

        /// <summary>The MAKE YOUR OWN cell. ⚠️ Held so `RefreshTabs` can fit it with the tabs; see
        /// `FitTabLabel` for why fitting cannot happen at build time.</summary>
        private Button _customDoor;

        private void OnEnable()
        {
            var s = Settings.SettingsStore.Current;
            if (s != null)
            {
                _pick[0] = Mathf.Max(0, s.CharacterPick);
                _pick[1] = Mathf.Max(0, s.CanPick);
                _pick[2] = Mathf.Max(0, s.SlipperPick);
            }
            if (_tabButtons.Count > 0)
            {
                int n = Entries.Count;
                _pick[_tab] = Mathf.Clamp(_pick[_tab], 0, Mathf.Max(0, n - 1));
                Refresh();
            }
        }

        /// <summary>
        /// The trait meters, as chalk/wood gauge tally marks.
        /// Matches the 8-segment gauges from the Godot original screen.
        /// </summary>
        private void RefreshTraits(RosterEntry entry)
        {
            var rows = Node("TraitRows");
            if (rows == null) return;

            for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);

            // Hero Strike characters are defined by verbs and counter-play, not by the three
            // Classic trait modifiers. Showing SPEED / POWER / GRIT here made the hero picker
            // look like a stat-select screen while hiding the information that actually changes
            // how a hero plays. The prop tabs keep their measured meters because cans and
            // slippers use those values in both modes.
            // ⚠️⚠️ THERE IS NO COSMETIC CONTROL ON THIS SCREEN ANY MORE. The COLOURS, CLOTHES
            // and STRENGTH rows were built here and are deleted; see the note below
            // `_heroLoadoutHeight` for what went and why the capability behind it stayed.

            if (_tab == 0 && SceneFlow.SelectedMode == GameMode.HeroStrike)
            {
                // ⚠️⚠️ MEASURED, NOT 289. That constant was three 86 px rows added up, and it
                // went stale the moment a row stopped needing 86. Sizing the column to the rows
                // that were actually built is what keeps the ultimate's plate inside the wood.
                RefreshHeroLoadout(rows, entry.Id);

                float column = _heroLoadoutHeight;

                // ⚠️ THE SPACING COMES OFF THE GROUP, NOT OUT OF A CONSTANT, so a restyle of the
                // picker cannot silently under-size the block.
                if (rows.TryGetComponent<VerticalLayoutGroup>(out var heroColumn))
                {
                    column += heroColumn.spacing * 2.0f;
                    column += heroColumn.padding.top + heroColumn.padding.bottom;
                }

                if (rows.TryGetComponent<LayoutElement>(out var heroRowsLayout))
                    heroRowsLayout.preferredHeight = column;
                return;
            }

            if (rows.TryGetComponent<LayoutElement>(out var classicRowsLayout))
                classicRowsLayout.preferredHeight = 104.0f;

            var labels = MeterLabels[_tab];
            int[] points = { entry.Bilis, entry.Lakas, entry.Tatag };

            for (int i = 0; i < labels.Length && i < points.Length; i++)
                BuildTraitRow(rows, labels[i], points[i]);

            // The camera controls are discoverable only if something says they exist. One line,
            // inside the panel, rebuilt with the meters so a roster change cannot orphan it.
            var hint = MenuKit.Label(rows, "Drag to turn the view · scroll to zoom · right-click to reset",
                                     MenuKit.MinReadableUnits,
                                     new Color(0.961f, 0.902f, 0.784f, 0.65f),
                                     Vector2.zero, Vector2.zero, Vector2.zero,
                                     TextAnchor.MiddleLeft);

            hint.raycastTarget = false;
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 24.0f;
        }

        /// <summary>
        /// How tall the three ability rows came out, in the units the column is laid out in.
        ///
        /// ⚠️ A FIELD RATHER THAN A RETURN VALUE so the builder keeps its signature and the
        /// caller can size the column after it. See `RefreshTraits`, which used to hand the
        /// column a constant 289.
        /// </summary>
        private float _heroLoadoutHeight;

        // ⚠️⚠️ THE THREE COLOUR ROWS THAT LIVED HERE ARE DELETED, ON REQUEST, TWICE.
        // 🧑 2026-09-01: *"this shit shiuld be gone the clothes color and soft bold and shit"*,
        // and *"I asked for this shhit to be removed before, the color shit for the chracters bcz
        // i wanted customization to eb for the make your own only"*. `COLOURS` (the earned
        // palettes), `CLOTHES` (twelve hue swatches) and `STRENGTH` (SOFT / AS DRAWN / BOLD) are
        // gone with `RefreshPaletteRow`, `RefreshTintRows`, `StripRow`, `BuildTintSwatch`,
        // `BuildStrengthChip`, `BuildSwatch` and `RepresentativeSlot`. `docs/TODO.md` § 114.6.
        //
        // ⚠️⚠️ HIS SCREENSHOT ALSO SHOWS A SECOND, INDEPENDENT FAULT THAT THE DELETION MAKES
        // MOOT: the three rows drew ON TOP OF the ability list, so SEISMIC STOMP's row had
        // `AS DRAWN` and `BOLD` printed through it. The heights those two methods returned were
        // added to a column height that had already been computed, which is § 102.4's shape
        // exactly: **a vertical overflow, invisible to every probe in the project, because they
        // all measure horizontally.** Anything put back in this space has to be measured before
        // the column is sized, not after.
        //
        // ⚠️⚠️ AND THE CAPABILITY IS KEPT WHILE THE CONTROL IS DELETED. `PaletteRules`,
        // `PaletteVariants`, `LoadoutRules.PaletteFor` and `Settings.SettingsStore.LookFor` are
        // untouched: a palette still crosses the wire, remote seats still wear one, and § 101.1's
        // variant-naming fix is still asserted by `CosmeticsWireTests`. That is the deletion he
        // asked for. Customisation is MAKE YOUR OWN (`CustomCharacterScreen`) and nothing else.
        //
        // ⚠️⚠️ THE CONSEQUENCE THIS NOTE USED TO RECORD IS CLOSED AND THE FIX WAS THE OTHER END.
        // It read: *"a `mastery.<hero>.palette.alt1` reward is still awarded and still owned, and
        // there is no longer any surface that equips it"*, which is `docs/TODO.md` § 114.15 row 5.
        // **Nothing awards a palette any more.** Mastery 5 and 15 pay wearable hero titles
        // (`ProgressionRules.MasteryTable`), so the shelf no longer hands out an item the game
        // cannot spend, and `CosmeticsWireTests` asserts that no track pays one. The transport
        // above stays for the day an authored skin or MAKE YOUR OWN wants it.

        private void RefreshHeroLoadout(Transform rows, string heroId)
        {
            _heroLoadoutHeight = 0.0f;
            var kit = HeroAbilitySystem.CreateKitFor(heroId);
            Color accent = UiTheme.ColorForHero(heroId);

            // ⚠️⚠️ THE COLUMN IS LAID OUT BEFORE IT IS MEASURED, AND WITHOUT THIS THE FIRST OPEN
            // IS ALWAYS WRONG. 🧑 2026-08-30, of the CHOOSE YOUR HERO panel again: *"the box size
            // adjusts after a click, i want it to be good from the start"*, and before that
            // *"when u open its still fucken broken"* (§ 79.6).
            //
            // The loop below reads `rows.rect.width` to decide whether each ability summary
            // wraps, and reserves the taller two-line box whenever it cannot measure — correct,
            // and 66 px of surplus across three rows against a column that only overflows by 64.
            // § 79.6 answered that with `_refreshPending`, a re-run on the NEXT `LateUpdate`,
            // which fixes the second frame and leaves the first one exactly as reported.
            //
            // ⚠️ IT REBUILDS THE OUTERMOST LAYOUT ANCESTOR, NOT `rows`. See
            // `ConvertedScreen.ForceLayoutFor`: this column sits inside `ConfigPanel`'s own
            // group, and rebuilding the inner rect re-runs a pass that reads a width its parent
            // has not computed yet and returns the same 0.
            //
            // ⚠️ AND `_refreshPending` STAYS. A canvas that is inactive this frame cannot be
            // rebuilt at all, which `LayoutRebuilder` states outright, so the retry goes from
            // being the fix to being the fallback.
            if (rows is RectTransform toLayOut) ForceLayoutFor(toLayOut);

            var abilities = new (string action, HeroAbility ability, bool ult)[]
            {
                ("Skill1", kit.Skill1, false),
                ("Skill2", kit.Skill2, false),
                ("Ultimate", kit.Ultimate, true),
            };

            // The picker must answer what the whole hero does without extra clicks. Each power
            // therefore gets the same visual weight and keeps its summary directly below it.
            for (int i = 0; i < abilities.Length; i++)
            {
                var item = abilities[i];
                if (item.ability == null) continue;

                var rowGo = new GameObject($"AbilityRow_{i}");
                rowGo.AddComponent<RectTransform>();
                rowGo.transform.SetParent(rows, false);

                // ⚠️ THE ULTIMATE'S PLATE IS TINTED, NOT JUST OUTLINED. 🧑, on the picker:
                // *"ui here ugly and repetitive"*, and the three rows were the largest part of
                // that: same plate, same dark fill, same layout, three times down the panel,
                // separated only by a one-pixel difference in border width. The ultimate is the
                // thing a whole round is spent earning and it looked like the third item in a
                // list. A wash of the hero's own colour through the fill costs nothing and
                // makes the row read as a different KIND of thing at a glance.
                //
                // ⚠️ 0.14, AND DELIBERATELY UNDER THE TEXT'S CONTRAST FLOOR. The summary line
                // sits on this plate at full Cream; a heavier tint would start eating the
                // legibility that was just fixed a few lines below.
                //
                // ⚠️⚠️ AND THE PLATE IS CUT PAPER NOW, WHICH IS THE SAME ARGUMENT ONE MATERIAL
                // OVER. These three rows sit INSIDE a panel `PaperDress` turned cream at
                // `Install`, so a near-black `HeroPlate` with cream lettering on it was the old
                // front end drawn inside the new one: 🧑, on the overhaul,
                // *"MAKE SURE U COMPLETELY REPLACE UI BCZ I DOTN WANT LEFTOVER SHIT FROM OLD UI"*.
                // They are `Tray` colours (`PaperWarm` in a `PaperEdge` cut) because an ability
                // row is a thing you READ, which is what that surface means.
                //
                // ⚠️ THE ULTIMATE KEEPS ITS ACCENT WASH AND ITS THICKER RIM, which is the whole
                // point of the note above: the thing a round is spent earning must not look like
                // the third item in a list. 0.14 of the hero colour reads on cream at least as
                // well as it did on near-black, and the rim is the accent at full strength.
                Color plate = item.ult
                    ? Color.Lerp(UiTheme.PaperWarm, accent, 0.14f)
                    : UiTheme.PaperWarm;

                var rowBg = rowGo.AddComponent<Image>();
                rowBg.sprite = GodotTheme.Box(
                    plate,
                    item.ult ? accent : UiTheme.PaperEdge,
                    item.ult ? 2 : 1, 6);
                rowBg.type = Image.Type.Sliced;
                rowBg.raycastTarget = false;

                var rowCol = rowGo.AddComponent<VerticalLayoutGroup>();
                rowCol.childControlHeight = true;
                rowCol.childControlWidth = true;
                rowCol.childForceExpandHeight = false;
                rowCol.childForceExpandWidth = true;
                // 5 top and bottom rather than 6, which is the two pixels the bigger summary
                // needed. See the height note below.
                rowCol.spacing = 3.0f;
                rowCol.padding = new RectOffset(10, 10, 5, 5);

                // ⚠️⚠️ 61 IS THE PANEL'S BUDGET AND IT IS NOT NEGOTIABLE FROM IN HERE. I raised
                // this to 68 to make room for the bigger summary, and three rows times seven
                // pixels ate the wood panel's bottom padding: the ultimate's border ended up
                // sitting on the panel edge. 🧑: *"it goes out the box"*. The panel is authored
                // at a fixed height in `CharacterSelect.unity` and does not grow to fit, so a
                // row that wants more height has to find it INSIDE itself.
                //
                // The budget, and it balances exactly: 26 header + 20 description + 3 spacing +
                // 10 padding = 59, inside 61 with two pixels spare.
                // ⚠️⚠️ THE HEIGHT IS SET BELOW, ONCE THE SUMMARY'S REAL LINE COUNT IS KNOWN.
                // It was a flat 86 here, which is 26 header + 44 description + 3 spacing + 10
                // padding, and that 44 reserves TWO LINES of summary. Every shipped summary is
                // ONE line, so each row carried about 22 px of empty wood and three of them ran
                // the ultimate's plate off the bottom of the panel. 🧑 2026-08-29: *"fix this
                // overflow"*, with the ultimate's border drawn outside the box.
                //
                // ⚠️ THIS IS THE LATA CARD'S FAULT ON A SECOND SURFACE (`docs/TODO.md` § 78.3):
                // a box sized for the worst case is the wrong size almost always. There it was a
                // width, here a height, and the answer is the same, measure what is being shown.
                var rowLe = rowGo.AddComponent<LayoutElement>();

                // ---- header: glyph, key, name, timing ----
                var header = new GameObject("Header", typeof(RectTransform));
                header.transform.SetParent(rowGo.transform, false);

                var headerHlg = header.AddComponent<HorizontalLayoutGroup>();
                headerHlg.childControlHeight = true;
                headerHlg.childControlWidth = true;
                headerHlg.childForceExpandHeight = true;
                headerHlg.childForceExpandWidth = false;
                headerHlg.childAlignment = TextAnchor.MiddleLeft;
                headerHlg.spacing = 8.0f;
                header.AddComponent<LayoutElement>().preferredHeight = 26.0f;

                var glyphGo = new GameObject("Glyph");
                glyphGo.transform.SetParent(header.transform, false);
                var glyph = glyphGo.AddComponent<Image>();
                glyph.sprite = AbilityIcons.For(item.ability.Glyph);
                // ⚠️ `HeroGlyphOn` IS CREAM AND THESE ROWS ARE CREAM NOW. That constant is
                // correct where it was written, which is the in-match deck over a dark plate; here
                // it would draw the ability icon in the colour of the plate behind it. Ink, which
                // is what every other mark on a paper surface is.
                glyph.color = UiTheme.PaperInk;
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;

                var glyphLe = glyphGo.AddComponent<LayoutElement>();
                glyphLe.minWidth = 26;
                glyphLe.preferredWidth = 26;
                glyphLe.minHeight = 26;
                glyphLe.preferredHeight = 26;

                var chipGo = new GameObject("KeyChip");
                chipGo.transform.SetParent(header.transform, false);
                var chip = chipGo.AddComponent<Image>();
                chip.sprite = GodotTheme.Box(UiTheme.PaperSunk, new Color(0, 0, 0, 0), 0, 4);
                chip.type = Image.Type.Sliced;
                chip.raycastTarget = false;

                var chipLe = chipGo.AddComponent<LayoutElement>();
                chipLe.minWidth = 26;
                chipLe.preferredWidth = 26;
                chipLe.minHeight = 18;
                chipLe.preferredHeight = 18;

                var keyLabel = MenuKit.Label(chipGo.transform, Hud.KeyLabelFor(item.action), 13,
                    accent,
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
                keyLabel.fontStyle = FontStyle.Bold;
                keyLabel.raycastTarget = false;
                MenuKit.Stretch(keyLabel.rectTransform);

                var nameLbl = MenuKit.Label(header.transform, item.ability.Name, MenuKit.MinReadableUnits,
                    accent,
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
                nameLbl.fontStyle = FontStyle.Bold;
                nameLbl.raycastTarget = false;
                nameLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

                // ⚠️⚠️ A CHARGE ABILITY HAS NO COOLDOWN AND THIS USED TO PRINT ITS ZERO AS ONE.
                // `HeroAbility` states the rule: an ability is on a cooldown OR on charges,
                // never both, so `Cooldown` is exactly 0.0 on every charge power. This label
                // printed it unconditionally, so Seismic Stomp read "0s" and Ignition Cannon
                // read "0s · 10s". 🧑, on the picker: *"why does this say 1 second cooldown"*.
                // Two of the five heroes were being described by a number that means "this
                // field does not apply to me".
                //
                // ⚠️ `Hud.PaintSkillCard` ALREADY CARRIES THIS DISTINCTION and says why in as
                // many words: *"A CHARGE SKILL AT ZERO IS NOT 'COOLING', AND THAT DISTINCTION IS
                // THE WHOLE REASON THIS BRANCH EXISTS."* The deck learned it and the picker
                // never did, which is how the same fact ends up drawn two different ways.
                //
                // ⚠️ AND THE UNITS ARE NAMED NOW. The old format was two bare numbers with a dot
                // between them, so "34s · 0.6s" gave the reader nothing to tell a cooldown from
                // a duration; the shorter one is not obviously either. A picker exists to be
                // read by somebody who does not know the kit yet.
                string timing;

                if (item.ult)
                {
                    timing = "ULTIMATE";
                }
                else if (item.ability.UsesCharges)
                {
                    int max = item.ability.MaxCharges;
                    timing = max == 1 ? "1 USE" : $"{max} USES";
                    if (item.ability.Duration > 0.0f) timing += $" · {item.ability.Duration:0.#}s";
                }
                else
                {
                    timing = $"{item.ability.Cooldown:0.#}s CD";
                    if (item.ability.Duration > 0.0f) timing += $" · {item.ability.Duration:0.#}s";
                }

                // ⚠️ CREAM AT FULL ALPHA, NOT 0.75. 🧑: *"shit down there is small and cant be
                // seent"*. This sat at 13 pt and three quarters opacity on a dark plate, which
                // is the least readable thing on the screen carrying the only NUMBERS on it.
                var timingLbl = MenuKit.Label(header.transform, timing, 14,
                    UiTheme.PaperInk,
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleRight);
                timingLbl.fontStyle = FontStyle.Bold;
                timingLbl.raycastTarget = false;

                // ⚠️ WIDE ENOUGH FOR THE LONGEST STRING THIS CAN PRODUCE, which is now
                // "45s CD · 4s" rather than "45s · 4s". 86 px was sized for the old format and
                // `MenuKit.Label` does not wrap, so the extra characters would have pushed the
                // ability NAME along instead of overflowing visibly, which is worse: it looks
                // like a layout choice rather than a bug.
                timingLbl.gameObject.AddComponent<LayoutElement>().minWidth = 116.0f;

                // ⚠️⚠️ 15 pt AND FULL CREAM, UP FROM 13 pt MUTED. 🧑: *"shit down there is small
                // and cant be seent"*. This line is the only place the picker explains what a
                // power actually DOES, and it was the least legible text on the screen: the
                // smallest size in the panel, at `CreamMuted`, over a dark plate. Muted grey is
                // ⚠️⚠️ AT `MenuKit.MinReadableUnits`, LIKE THE INSPECT TRAY, AND IT WAS UNDER IT.
                // 🧑 2026-08-29: *"mahirap basahin yung text sa skill description"*. This is the
                // LEARN layer of `docs/VISION.md` § 3 and the tray is the RECALL layer; both
                // carried the same sentence at 15 units against a floor of 18, so the complaint
                // was true of the ability text everywhere it appears rather than of one screen.
                //
                // ⚠️ THE ROW AND ITS CONTAINER BOTH GREW, and the note below is the reason they
                // had to: 20 px held one line of 15 and holds none of 18. Two lines of 18 is 44,
                // the row goes 61 to 86, and three hero rows take the block from 214 to 289.
                // `HeroPickerLayoutProbe` is what checks the plate can still hold it.
                // for text the reader may skip, and a player choosing a hero for the first time
                // cannot skip this one.
                //
                // ⚠️ THE ROW GREW WITH IT. A taller line inside a `preferredHeight` that did not
                // move would push the description into the plate's bottom border, which is the
                // fault this was supposed to fix wearing a different hat.
                var descLbl = MenuKit.Label(rowGo.transform, item.ability.Summary, MenuKit.MinReadableUnits,
                    UiTheme.PaperInk, Vector2.zero, Vector2.zero, Vector2.zero,
                    TextAnchor.UpperLeft);
                descLbl.raycastTarget = false;
                descLbl.horizontalOverflow = HorizontalWrapMode.Wrap;
                descLbl.verticalOverflow = VerticalWrapMode.Overflow;
                // ⚠️⚠️ ONE LINE OR TWO, MEASURED, RATHER THAN ALWAYS RESERVING TWO. 44 is two
                // lines at `MinReadableUnits` and 22 is one. `preferredWidth` is what this exact
                // component needs for the string on ONE line, so comparing it with the room the
                // row actually has answers whether it will wrap, without depending on a layout
                // pass that has not run yet. Same idiom as `Hud.LineWidth`.
                //
                // ⚠️ AN UNMEASURABLE WIDTH RESERVES TWO LINES. A rect that has not been laid out
                // reports 0, and guessing "one line" there would clip a wrapped summary against
                // the plate's border. The safe direction is the taller one.
                float rowRoom = rows is RectTransform rowsRect ? rowsRect.rect.width - 20.0f : 0.0f;
                bool summaryWraps = rowRoom <= 1.0f || descLbl.preferredWidth > rowRoom;

                // ⚠️⚠️ AN UNMEASURABLE WIDTH IS THE ULTIMATE'S PLATE HANGING OUT OF THE PANEL, AND
                // 🧑 FOUND THE TELL: *"oh shit if i click next it gets fixed"*, *"but yea when u
                // open its still fucken broken"*. `docs/TODO.md` § 79.6.
                //
                // `rect.width` is 0 until the first layout pass, which is the frame this panel is
                // switched on, so `rowRoom` is 0 and the safe branch above reserves TWO lines for
                // EVERY row: 44 px each instead of 22. Three rows is **66 px** of surplus, against
                // the 64 px the column was measured overflowing by. Cycling the hero re-runs
                // `Refresh` when the rect is real, most summaries fit one line, and the column
                // shrinks back inside the wood — which is exactly the behaviour he described.
                //
                // ⚠️ SO THE FALLBACK IS CORRECT AND WHAT WAS MISSING IS THE SECOND PASS. Reserving
                // the taller box is the right guess when nothing can be measured (its own note
                // says so, and guessing one line would clip a wrapped summary). It just has to be
                // re-asked once there is a width, rather than left as the final answer.
                if (rowRoom <= 1.0f) _refreshPending = true;

                float descHeight = summaryWraps ? 44.0f : 22.0f;
                descLbl.gameObject.AddComponent<LayoutElement>().preferredHeight = descHeight;

                // 26 header + the summary + 3 spacing + 10 padding, the budget the note above
                // the row spells out.
                rowLe.preferredHeight = 26.0f + descHeight + 3.0f + 10.0f;
                rowLe.minHeight = rowLe.preferredHeight;
                _heroLoadoutHeight += rowLe.preferredHeight;
            }

            // The key chips already communicate Q, E and F. A fourth instruction line below
            // the cards duplicated that information and clipped against the wood panel.
        }

        // ⚠️⚠️ THE FILLED PIP IS WOOD AND IT WAS AMBER, WHICH IS `docs/TODO.md` § 119.10'S
        // MEASUREMENT ARRIVING ON A THIRD CONTROL. `(0.98, 0.78, 0.12)` is `UiTheme.Amber` written
        // out, and amber on the cream panel these pips now sit in is **1.7:1**: the filled half of
        // a trait bar is the half that disappears. On paper the marker is the one DARK thing, so a
        // filled pip is wood sitting in an empty pip's groove and the pair are about 6:1 apart.
        private static readonly Color PipFilled = UiTheme.WoodMid;
        private static readonly Color PipEmpty = new Color(UiTheme.PaperSunk.r, UiTheme.PaperSunk.g,
                                                           UiTheme.PaperSunk.b, 0.75f);

        /// <summary>
        /// ⚠️⚠️ AS MANY SEGMENTS AS A TRAIT HAS POINTS, WHICH IS FIVE. This was eight, and the
        /// consequence is not cosmetic: a trait is scored 1 to 5, so BERTO's GRIT of 5 drew as
        /// five lit pips out of eight and read as a middling stat when it is the maximum in the
        /// game. Every Godot capture in `docs/Godot_Character_Select_References` shows five
        /// segments, and the meter is the only place the roster's numbers reach the player.
        /// </summary>
        private const int GaugeSegments = Core.Roster.TraitMax;

        private static void BuildTraitRow(Transform parent, string name, int points)
        {
            var rowGo = new GameObject($"{name}Row");
            rowGo.AddComponent<RectTransform>();
            rowGo.transform.SetParent(parent, false);

            var row = rowGo.AddComponent<HorizontalLayoutGroup>();
            row.childControlHeight = true;
            row.childControlWidth = true;
            row.childForceExpandHeight = false;
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.spacing = 10.0f;

            rowGo.AddComponent<LayoutElement>().preferredHeight = 26.0f;

            var label = MenuKit.Label(rowGo.transform, name, 19, PipFilled, Vector2.zero,
                                      Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            label.raycastTarget = false;

            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 110.0f;

            var pipsGo = new GameObject("Pips");
            pipsGo.AddComponent<RectTransform>();
            pipsGo.transform.SetParent(rowGo.transform, false);

            var pips = pipsGo.AddComponent<HorizontalLayoutGroup>();
            pips.childControlHeight = true;
            pips.childControlWidth = true;
            pips.childForceExpandHeight = false;
            pips.childForceExpandWidth = false;
            pips.childAlignment = TextAnchor.MiddleLeft;
            pips.spacing = 4.0f;

            for (int i = 0; i < GaugeSegments; i++)
            {
                var pipGo = new GameObject($"Pip{i}");
                pipGo.AddComponent<RectTransform>();
                pipGo.transform.SetParent(pipsGo.transform, false);

                var pip = pipGo.AddComponent<Image>();
                pip.color = i < points ? PipFilled : PipEmpty;
                pip.raycastTarget = false;

                var element = pipGo.AddComponent<LayoutElement>();
                element.preferredWidth = 28.0f;
                element.preferredHeight = 12.0f;
            }
        }

        private IReadOnlyList<RosterEntry> Entries =>
            _tab == 0 ? Roster.GetPeople(SceneFlow.SelectedMode) : (_tab == 1 ? Roster.Cans : Roster.Slippers);

        private void CycleEntry(int delta)
        {
            int n = Entries.Count;
            _pick[_tab] = ((_pick[_tab] + delta) % n + n) % n;
            Refresh();
        }

        /// <summary>Set when the ability rows were sized against a rect that had not been laid
        /// out yet. See the note in `RefreshHeroLoadout`.</summary>
        private bool _refreshPending;

        /// <summary>
        /// ⚠️ ONE RETRY ON THE FRAME AFTER A LAYOUT-BLIND REFRESH. Unity has laid the canvas out
        /// by the next `LateUpdate`, so this is the earliest point `rect.width` is real. The flag
        /// is cleared BEFORE the refresh, so a second blind pass re-arms it rather than looping,
        /// and it costs one bool test on every other frame.
        ///
        /// ⚠️ IT IS THE SAME SHAPE AS `ConvertedMatchSetup`'s `_refitPending`, and for the same
        /// underlying reason: this project has several screens that measure themselves on the
        /// frame they are switched on, and `rect.width` is 0 there. `ModelPreview.EnsureTexture`
        /// and `LobbyChat`'s panel both carry a note about it.
        /// </summary>
        private void LateUpdate()
        {
            if (!_refreshPending) return;

            _refreshPending = false;
            Refresh();
        }

        private void Refresh()
        {
            int n = Entries.Count;
            if (n == 0) return;
            _pick[_tab] = Mathf.Clamp(_pick[_tab], 0, n - 1);
            var entry = Entries[_pick[_tab]];

            bool choosingHero = _tab == 0 && SceneFlow.SelectedMode == GameMode.HeroStrike;
            SetText("CharValueLabel", entry.Name);
            SetText("TaglineLabel", TaglineFor(entry.Id));

            // ⚠️⚠️ THE CAPTION IS GONE BECAUSE IT SAID WHAT THE TAB ABOVE IT ALREADY SAID.
            // 🧑, on the picker: *"here is said twice"*. The tab bar reads HERO | LATA | TSINELAS
            // with HERO selected, and eighty pixels below it this label read "HERO" again, in a
            // muted grey, against the selector holding the hero's actual name. On the other two
            // tabs it read "NAME", which is worse: the tab says LATA and the row says NAME, so a
            // word was being spent to announce that a name field contains a name.
            //
            // ⚠️ IT IS DISABLED RATHER THAN BLANKED. Setting the text to "" leaves the object in
            // the row's layout still holding its width, so the selector would keep the gap where
            // the redundant word used to be and the fix would look like a rendering fault.
            var caption = Node("NameCaption");
            if (caption != null && caption.gameObject.activeSelf)
                caption.gameObject.SetActive(false);

            // ⚠️⚠️ AND THE ROW HAS TO BE RE-CENTRED, OR REMOVING THE WORD JUST MOVES THE PROBLEM.
            // `NameRow` is authored as a `HorizontalLayoutGroup` with `m_ChildAlignment: 3`,
            // which is MiddleLeft: the caption held the left edge and the selector sat wherever
            // it landed after it. Hiding the caption on its own leaves the selector pinned left
            // with the gap where the word used to be, and a hole down the right of the panel.
            // 🧑: *"the uncentered shit looks ugly"*, *"maybe js remove hero and center this
            // shit"*, and the second half of that is the half that does the work.
            //
            // ⚠️ SET AT REFRESH RATHER THAN IN THE SCENE, because the caption is hidden here too
            // and the two facts are one decision: a scene edit that centred the row while the
            // caption was still active would centre the PAIR and look deliberate but wrong.
            var nameRow = Node("NameRow")?.GetComponent<HorizontalLayoutGroup>();
            if (nameRow != null) nameRow.childAlignment = TextAnchor.MiddleCenter;

            // ⚠️⚠️ THE TAGLINE FLOATS IN A BOX TWICE THE SIZE OF ITS TEXT, AND THAT IS THE GAP.
            // 🧑: *"theres big empty space in between character names and description"*. The
            // scene authors this label with `m_PreferredHeight: 96` and `m_Alignment: 3`, which
            // is MiddleLeft: two lines of 22 pt is about 56 px, so the text sits centred in 96
            // with roughly twenty dead pixels above it and twenty below. The space reads as a
            // layout mistake because it is one, and no amount of moving the rows fixes it while
            // the label keeps reserving the height.
            //
            // ⚠️ TOP-ALIGNED AND SIZED TO THE TEXT, not merely top-aligned. Aligning alone moves
            // the gap to the bottom of the box instead of removing it, and the rows below would
            // sit exactly where they do now. The change itself is in the tagline block further
            // down, which already owns this label's size.
            var value = Node("CharValueLabel")?.GetComponent<Text>();
            if (value != null)
            {
                value.fontSize = choosingHero ? 32 : 30;
                value.fontStyle = FontStyle.Bold;
                // ⚠️ INK ON THE CLASSIC TAB, because the panel behind this label is cream now.
                // The hero accent stays: it is a gameplay tell rather than decoration.
                value.color = choosingHero
                    ? UiTheme.ColorForHero(entry.Id) : UiTheme.PaperInk;
            }

            var tagline = Node("TaglineLabel")?.GetComponent<Text>();
            if (tagline != null)
            {
                tagline.fontSize = choosingHero ? 18 : 19;
                tagline.lineSpacing = 1.0f;

                // ⚠️⚠️ TOP-ALIGNED, AND THE ALIGNMENT IS WHY THE GAP LOOKED LIKE A BUG. The
                // scene authors this label `MiddleLeft`, so two lines of 18 pt sat vertically
                // CENTRED in whatever height was reserved: the text floated with dead space
                // above it and below it, and the description appeared to have been pushed away
                // from the name for no reason. 🧑: *"theres big empty space in between character
                // names and description"*.
                tagline.alignment = TextAnchor.UpperLeft;

                // ⚠️⚠️ THE `minHeight` IS THE ONE THAT MATTERED, AND WRITING ONLY
                // `preferredHeight` IS WHY THREE SEPARATE PASSES AT THIS GAP CHANGED NOTHING.
                // 🧑 reported the same band of empty wood on 2026-08-25 and again on 2026-08-26
                // (*"fix ui here, theres big open space"*) after it had been "fixed" by
                // top-aligning the label, then by setting its preferred height, then by
                // switching the `ContentSizeFitter`'s vertical axis off. All three were reasoned
                // from the source and none of them was measured.
                //
                // `HeroPickerLayoutProbe` measured it in one run:
                //
                //     TaglineLabel  h=96  LE(on=True, min=96, pref=46, prio=1)
                //
                // The preference WAS 46 and had been for a day. `LayoutUtility.GetPreferredHeight`
                // returns `Max(minHeight, preferredHeight)`, so a 96 px FLOOR beats a 46 px
                // preference every time, and the 50 px difference is the band.
                //
                // ⚠️ THE FLOOR COMES FROM THE .tscn AND NOT FROM THIS FILE. `TscnUiImporter`
                // writes `custom_minimum_size.y` straight into `minHeight`, and the Godot scene
                // authors this label at 96 for a THREE-line Classic tagline in a panel that had
                // no ability rows under it. Nothing in the conversion is wrong; the number simply
                // stopped being right when the hero variant of this screen was added.
                //
                // ⚠️ SO BOTH ARE WRITTEN, ALWAYS, AND THEY ALWAYS AGREE. One owner for one
                // number, stated twice because Unity reads it twice.
                float taglineBox = choosingHero ? HeroTaglineHeight(tagline) : 96.0f;

                if (tagline.TryGetComponent<LayoutElement>(out var taglineLayout))
                {
                    taglineLayout.minHeight = taglineBox;
                    taglineLayout.preferredHeight = taglineBox;

                    // ⚠️ AND NO FLEXIBLE HEIGHT. Left at -1 the column may ask this label to
                    // soak up the panel's spare 24 px, which would put the band straight back
                    // in a form nothing in this method could see.
                    taglineLayout.flexibleHeight = 0.0f;
                }

                // ⚠️ THE FITTER STAYS OFF ON THE VERTICAL AXIS. With the element now pinning
                // both ends of the height, a self-controller sizing the same axis to the text
                // would be a second answer to a settled question.
                if (tagline.TryGetComponent<ContentSizeFitter>(out var taglineFitter))
                    taglineFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            RefreshTabs();
            RefreshTraits(entry);
            RefreshBackdropAccent(entry);
            ShowModel(entry);
        }

        private void RefreshBackdropAccent(RosterEntry entry)
        {
            if (_glowImage == null) return;

            // ⚠️⚠️ THIS WAS `bayanBlue`, (0.64, 0.75, 1.0), AND IT IS THE PALE BLUE 🧑 CAN SEE ON
            // THE PICKER'S ARROWS AND ROUND THE FIGURE. `CLAUDE.md` § 6.4: the rule is the whole
            // palette, not just outlines, and this glow lit the arrow buttons and the panel edge
            // in a colour nothing else in the front end uses. **The neutral is cream now**, which
            // is the same job (a warm lift behind the model) in the game's own ink.
            //
            // ⚠️ THE HERO LERP IS UNCHANGED AND IS NOT THIS. `UiTheme.ColorForHero` is the hero
            // accent, which is a gameplay tell (`VISION.md` § 1.1: reading which kit an opponent
            // has is a skill), and Cheska's is deliberately cold. A hero accent is exempt for the
            // same reason `UiTheme.Defense` is; a decorative wash is not.
            // ⚠️⚠️ AND THE NEUTRAL IS A WARM SHADE NOW RATHER THAN CREAM, BECAUSE THE FIELD MOVED
            // UNDER IT. The glow texture is white with a soft radial alpha, so its colour is a
            // multiply: cream on a wooden backdrop was a visible lift and cream on a CREAM
            // backdrop is nothing at all. **A vignette on a light field is darker than the field,
            // not lighter.** `PaperEdge` is one step down, which keeps the halo behind the model
            // readable without turning it into a spotlight.
            //
            // ⚠️ THE HERO LERP STILL WINS ON THE HERO TAB, and that is the point of the lerp: it
            // is the one place on this screen the kit's own colour is allowed to wash the stage.
            var neutralGlow = new Color(UiTheme.PaperEdge.r, UiTheme.PaperEdge.g,
                                        UiTheme.PaperEdge.b, 1.0f);
            if (entry != null && _tab == 0)
                _glowImage.color = Color.Lerp(neutralGlow, UiTheme.ColorForHero(entry.Id), 0.65f);
            else
                _glowImage.color = neutralGlow;
        }

        /// <summary>
        /// ⚠️ THE SCREEN SPINS THE ACTUAL MODEL. `CharacterSelect.tscn` carries a SubViewport
        /// with two lights and a pivot, and the panel's own hint line tells the player they can
        /// drag it. A still portrait would make three of those controls lies.
        /// </summary>
        private void ShowModel(RosterEntry entry)
        {
            if (!Application.isPlaying) return;

            var stage = Node("CharacterPreview");
            if (stage == null) return;

            var preview = stage.GetComponent<ModelPreview>();

            if (preview == null)
            {
                preview = stage.gameObject.AddComponent<ModelPreview>();
                preview.Attach(stage.GetComponent<RectTransform>());
            }

            var book = RosterBook.Load();
            if (book == null) return;

            var art = _tab == 0 ? book.PersonArt(_pick[0], SceneFlow.SelectedMode)
                    : (_tab == 1 ? book.CanArt(_pick[1]) : book.SlipperArt(_pick[2]));

            // ⚠️ THE LOOK-DOWN ANGLE IS NOT PASSED IN ANY MORE, IT IS MEASURED. A lata and a
            // tsinelas lie on the ground and need a steeper pitch than a standing Person, and
            // the category is a poor proxy for that: `character_preview.gd` lerps the pitch on
            // the subject's own height:width ratio so a tall lata and a flat slipper get
            // different angles even though both are "not a person".
            //
            // ⚠️ AND THE CLIPS TRAVEL WITH THE MODEL, or the preview stands in a T-pose. They
            // are sub-assets of the `.glb` and this reference is what makes them ship.
            // ⚠️ THE TAB IS THE ONLY THING THAT KNOWS THIS IS A SHOE. Set before `Show`, because
            // `Show` is what dresses the model. See `ModelPreview.ShowingSlipper`.
            preview.ShowingSlipper = _tab == 2;

            // ⚠️⚠️ THE PREVIEW WEARS THE EQUIPPED PALETTE, AND THAT IS WHERE COSMETICS BELONG.
            // `FUTURE.md` PHASE 5: *"Preview through `ModelPreview` with the real shader, never a
            // flat icon."* A colour choice made anywhere else is a choice made blind, and this
            // screen already has the model, the real toon shader and the ink outline on it.
            // **You customise a character where you choose a character**, which is the journey
            // `CLAUDE.md` § 6.3 asks to be walked out loud: pick, see, done.
            //
            // ⚠️ PEOPLE ONLY. A lata and a tsinelas have their own skins and their own
            // categories; `PaletteVariants.For` would answer their authored colours anyway, but
            // asking the loadout for a slipper's palette would be a question with no meaning.
            var palette = art == null ? null : art.Palette;

            if (_tab == 0 && art != null)
                palette = PaletteVariants.For(art.Palette, Settings.SettingsStore.LookFor(
                    Roster.PersonIdAt(SceneFlow.SelectedMode, _pick[0])));

            preview.Show(art == null ? null : art.Model, art == null ? null : art.Clips,
                         palette, art == null ? null : art.PetModel);
        }

        /// <summary>
        /// ⚠️ THE SENTENCE AND THE METERS MUST AGREE. The roster rule is that the number is
        /// readable off the sentence: if a description says somebody is quick, SPEED is high. A
        /// stat nobody can predict from the lore is a random modifier, and a description nothing
        /// backs up is a lie the player finds out about in round 2.
        /// </summary>
        /// <summary>
        /// How tall a hero's two-line tagline box has to be.
        ///
        /// ⚠️ SOLVED FROM THE FONT SIZE AND THE LINE COUNT, NOT TYPED. Two of the three failed
        /// attempts at this gap used a literal, and a literal goes stale the moment the font
        /// size on the line above it changes, which it has done twice.
        ///
        /// ⚠️ 1.35 IS THE SAME FACTOR `TscnUiImporter` USES for a label's height floor, so the
        /// two places in this project that turn a font size into a box height agree. It is
        /// generous against the roughly 1.16 a Darumadrop line actually measures, which is what
        /// pays for the descenders.
        ///
        /// ⚠️ AND THE LINE COUNT IS COUNTED, NOT ASSUMED. `TaglineFor` returns ROLE + newline +
        /// sentence for every hero today; a third line added to one of them would otherwise be
        /// clipped, and a clipped sentence is a worse fault than the gap this replaces.
        /// </summary>
        private static float HeroTaglineHeight(Text tagline)
        {
            int lines = 1;
            string body = tagline.text ?? "";

            for (int i = 0; i < body.Length; i++)
                if (body[i] == '\n') lines++;

            return Mathf.Ceil(tagline.fontSize * 1.35f) * lines + 6.0f;
        }

        private static string TaglineFor(string id)
        {
            switch (id)
            {
                // Hero Strike Roster
                case "dante": return "EARTH JUGGERNAUT\nBreak formations with tremors, armor, and a map-splitting fissure.";
                case "cheska": return "ICE CONTROLLER\nCreate slip zones and barricades, then lock the lane with Glacial Nova.";
                case "sean": return "FIRE BRAWLER\nRush the lane, blast open space, and finish with Supernova.";
                case "zack": return "LIGHTNING SKIRMISHER\nSprint through fights, build charge, and call down Thunderstrike.";
                case "nemu": return "SPIRIT TRICKSTER\nSlip beyond reach, possess the street, and turn a seance into a trap.";
                case "phaister": return "STREET WITCH\nCurse the ground, blink out of trouble, and black out the whole street.";

                // Classic Roster
                case "bayan":
                case "berto": return "The original defender. Immovable, unhurriable, and still standing exactly where you left him.";
                case "maring": return "Quick hands, quicker mouth. She has talked her way out of more tags than she has dodged.";
                case "totoy": return "Raised barefoot in the eskinita. Nobody in this town has caught him twice.";
                case "inday": return "Minds the corner stall and is afraid of absolutely nothing that walks past it.";
                case "kuya_boy":
                case "iggy": return "Eldest of seven. He has been the taya since before he could count, and both the arm and the footwork know it.";
                case "ate_girlie": return "Queen of patintero, slumming it at tumbang preso. The footwork came with her.";
                case "tikboy": return "Always down to one tsinelas. Half the footwear, twice the throwing arm.";
                case "bebang": return "Hits like a jeepney door closing, and moves about as easily. Do not tease her about it, and do not stand in front of her.";
                case "jun_jun": return "The bunso of the street. Small, slippery, and impossible to corner. Also impossible to keep upright.";
                case "lola_pacing": return "Watches from the window most afternoons. On the good ones she comes down to play, and she does not miss twice.";
                case "mang_kanor": return "Tricycle driver. He knows every corner of this town by its potholes and he takes them at speed. Braking was never the strong suit.";
                case "aling_nena": return "She owns the sari-sari store, so she owns the rules. Nobody has ever argued a call twice.";

                case "pasip": return "Softdrink na hindi Pepsi. Tall, thin and empty, it goes over if you look at it hard, and it is back up before you have turned around.";
                case "boyben": return "Leftover fence paint, half set solid. Nothing on the mark stands its ground like it does, but righting it is a proper job.";
                case "decades": return "Flakes in oil from Aling Nena's. Squat and low, so tipping it is the hard part, and setting it back up is barely a motion.";
                case "metal": return "No label left, just ribs and rust. Heavy for its size, it sends the tsinelas across the street, and it is slow to stand back up.";
                case "piyesta": return "Fruit cocktail, saved for handaan and opened early anyway. The widest can on the mark and still full of syrup, so it plants itself and swallows the hit whole.";
                case "karne": return "Corned beef, the tin that tapers. Top-heavy over a narrow lid so it tips at the first excuse, but it is packed solid and it kicks the tsinelas back at you.";

                case "tsinelas": return "The street-game original. Thick layered sole, printed Y-strap, worn down at the heel. Balanced in flight, impact and recovery.";
                case "crocs": return "Holes in the top, strap swung round the back. Heavy and it does not fly straight, but whoever body-blocks it knows all about it.";
                case "pantulog": return "Lola's house slipper, fur worn flat and a bow hanging on by a thread. No weight behind it at all, but it is ready again before the taya has turned around.";
                case "sike": return "Definitely not the real brand. Light, loud, and the quickest thing off a hand on this street.";
                case "spartan": return "Black rubber and a red Y-strap, straight from the kanto. Hits harder than the basic pair, but takes longer to settle back into your hand.";
                case "alpombra": return "Somebody's good pair, block heel and a stoned buckle, borrowed off the rack by the door. It drops early and lands quiet, and it is back in your hand before the taya turns.";
                case "pambahay": return "The scuffed white slide that lives by the shower, somebody's toes moulded into the footbed. Light rubber that lands flat and soft, and you have it back before the puddle has dried.";
                case "heels": return "Completely impractical and brutally effective. Short-ranged, slow to recover, and the last thing anyone wants to body-block.";
                case "sandals": return "Strapped down and built for walking. Fast and steady through the air, but not made for rapid-fire throws.";
                case "loafers": return "Somebody's school shoe, buckle and all, still warm. Stiff leather with no give in it, so it does not sail, but it lands like a brick with homework in it.";

                default: return "";
            }
        }

        private void Confirm()
        {
            var s = Settings.SettingsStore.Current;
            s.CharacterPick = _pick[0];
            s.CanPick = _pick[1];
            s.SlipperPick = _pick[2];
            Settings.SettingsStore.Save();

            Dismiss();
        }

        /// <summary>
        /// ⚠️ ESCAPE LEAVES THIS SCREEN TOO. `character_select.gd` handles `ui_cancel` and the
        /// conversion dropped it; this is the only converted screen that is neither an overlay
        /// (which cancels through `ConvertedOverlay.Cancel`) nor a plain scene change (which
        /// declares a `CancelTarget`), so it was the one left with a dead Escape key.
        ///
        /// ⚠️ IT ROUTES THROUGH `Dismiss`, THE SAME METHOD THE BACK BUTTON CALLS, so the key and
        /// the button cannot come to mean different things — including the standalone fallback
        /// below, which a scene name in `CancelTarget` could not have expressed.
        /// </summary>
        protected override bool Cancel()
        {
            Dismiss();
            return true;
        }

        /// <summary>
        /// Closes the panel if it is one, and falls back to a scene change if this screen was
        /// ever loaded standalone.
        /// </summary>
        private void Dismiss()
        {
            Closed?.Invoke();

            if (transform.parent != null)
            {
                gameObject.SetActive(false);
                return;
            }

            SceneFlow.Go(SceneFlow.MatchSetup);
        }
    }
}
