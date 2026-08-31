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
            Refresh();
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

            // The Godot layout is the baseline, but its neutral grey top made the select screen
            // feel detached from the game's Bayan navy identity. Keep the same three-stop shape
            // and deepen only the hue/chroma so the yellow banner, wood panel and ink outlines
            // remain the visual anchors.
            var top = new Color(0.400f, 0.455f, 0.610f, 1.0f);
            var middle = new Color(0.165f, 0.205f, 0.365f, 1.0f);
            var bottom = new Color(0.015686f, 0.031373f, 0.219608f, 1.0f);

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
            var ink = new Color(0.015686f, 0.031373f, 0.219608f, 1.0f);

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float alpha;
                if (t <= 0.36f) alpha = Mathf.Lerp(0.85f, 0.70f, t / 0.36f);
                else if (t <= 0.62f) alpha = Mathf.Lerp(0.70f, 0.12f, (t - 0.36f) / 0.26f);
                else alpha = Mathf.Lerp(0.12f, 0.0f, (t - 0.62f) / 0.38f);

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
        private void WireTabs()
        {
            var bar = Node("TabBar");
            if (bar == null) return;

            for (int i = bar.childCount - 1; i >= 0; i--) Destroy(bar.GetChild(i).gameObject);

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

                _tabButtons.Add(button);
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

                if (button.targetGraphic is Image face)
                {
                    face.sprite = GodotTheme.Box(
                        active ? UiTheme.Highlight : UiTheme.WoodDark,
                        active ? UiTheme.Cream : UiTheme.WoodEdge,
                        active ? 3 : 2, 6);
                    face.type = Image.Type.Sliced;
                }

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.color = active ? UiTheme.Ink : UiTheme.Cream;
                    label.fontStyle = FontStyle.Bold;
                }
            }
        }

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
            // ⚠️⚠️ THE COLOURS ROW GOES FIRST AND IT IS THE ONLY COSMETIC CONTROL IN THE GAME.
            // `FUTURE.md` § 0.5b's per-phase table asks Phase 5 for *"a locker, reached from the
            // hub"*, with *"the character, wearing it"* as the one thing on it, and this screen
            // is that already: the model, the real toon shader, the ink outline and the palette
            // are all here (`RefreshPreview`). **A locker built on the hub would be a second
            // screen showing the same character worse**, which is § 92's fault with a new name,
            // and the journey is what settles it (`CLAUDE.md` § 6.3): PLAY, pick, recolour, done
            // is three presses on the screen you are already looking at, against five that start
            // by hunting a corner chip nobody has found yet (`docs/TODO.md` § 96).
            // **§ 0.5 rule 11 says a phase that disagrees with that table corrects it**, so
            // `FUTURE.md` PHASE 5's row now says so.
            //
            // ⚠️ BOTH MODES, ABOVE THE EARLY RETURN. Hero Strike's column returns before the
            // trait meters, and a cosmetic that only existed in Classic would be the one thing on
            // this screen that changes meaning with the mode.
            float paletteHeight = RefreshPaletteRow(rows, entry) + RefreshTintRows(rows, entry);

            if (_tab == 0 && SceneFlow.SelectedMode == GameMode.HeroStrike)
            {
                // ⚠️⚠️ MEASURED, NOT 289. That constant was three 86 px rows added up, and it
                // went stale the moment a row stopped needing 86. Sizing the column to the rows
                // that were actually built is what keeps the ultimate's plate inside the wood.
                RefreshHeroLoadout(rows, entry.Id);

                float column = _heroLoadoutHeight + paletteHeight;

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
                classicRowsLayout.preferredHeight = 104.0f + paletteHeight;

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

        /// <summary>How tall a swatch is, and the row with it.</summary>
        private const float SwatchSize = 44.0f;
        private const float PaletteRowHeight = SwatchSize + 12.0f;

        /// <summary>
        /// The colours this character may be worn in, as swatches of the actual colour.
        ///
        /// ⚠️⚠️ IT IS NOT DRAWN AT ALL WHEN NOTHING IS EARNED, WHICH IS § 0.5b QUESTION 3
        /// ANSWERED RATHER THAN SKIPPED. A fresh account owns no palette, and a row reading
        /// "COLOURS: DEFAULT" with one dead swatch is the fifteen rows of `0/0 (needs 10 throws)`
        /// that taught a new player the game was broken (`docs/TODO.md` § 92.1 fault 4).
        /// **A control whose only option is the one you already have is not a control.**
        ///
        /// ⚠️⚠️ THE SWATCH IS THE COLOUR THE CHARACTER ACTUALLY BECOMES, DERIVED THROUGH THE
        /// SAME `PaletteVariants.For` THE MODEL GOES THROUGH. `FUTURE.md` PHASE 5: *"preview
        /// through `ModelPreview` with the real shader, never a flat icon"*, and the same
        /// argument one size down — a swatch painted from an authored constant is a swatch that
        /// can disagree with the model beside it, which is exactly what `ToonSkin.ApplySlipper`'s
        /// header records costing a shoe that changed colour per screen.
        ///
        /// ⚠️ AND THE MODEL IS THE PREVIEW, NOT THE SWATCH. The swatches are small and quiet on
        /// purpose: the one thing on this screen is the character, three feet tall, wearing the
        /// choice. `RefreshPreview` redraws it the moment one is pressed.
        ///
        /// Returns the height it took, so the column can size itself. See `RefreshTraits`.
        /// </summary>
        /// <summary>How many hue swatches the TINT strip draws.</summary>
        private const int TintSteps = 12;

        /// <summary>
        /// The three saturation steps, and the labels a player reads rather than a percentage.
        ///
        /// ⚠️ NAMES RATHER THAN NUMBERS, AND THREE RATHER THAN A SLIDER. `FUTURE.md`
        /// § 0.5b: "the type scale, three steps, never more", and the same discipline applies to
        /// a choice. 55, 100 and 145 are `PaletteRules.SaturationMin`, the authored value and
        /// `SaturationMax`, so the strip cannot offer a setting the rule would clamp.
        /// </summary>
        private static readonly (string Label, int Percent)[] TintStrengths =
        {
            ("SOFT", PaletteRules.SaturationMin),
            ("AS DRAWN", 100),
            ("BOLD", PaletteRules.SaturationMax),
        };

        /// <summary>
        /// TINT and STRENGTH: the free colour dial, which is the part of Phase 5 that is the point
        /// of Phase 5.
        ///
        /// ⚠️⚠️ IT IS NOT GATED ON ANYTHING AND EVERY ACCOUNT SEES IT ON ITS FIRST LAUNCH.
        /// The brief, 2026-08-31: *"the main purpose of the customizationn shit is so that ppl
        /// coudl spend their time making their own character"*. Two earned presets are a reward
        /// and a reward is not somewhere to spend an evening; **the row above this one is what
        /// you unlocked and this row is what you make.** `FUTURE.md` § 0.5 rule 4 is the rule
        /// that has to hold and it holds exactly: nothing on a progression track may change a
        /// gameplay number, and a colour changes none.
        ///
        /// ⚠️⚠️ AND IT IS THE ONE PLACE § 0.5b QUESTION 3 ANSWERS DIFFERENTLY FROM THE ROW
        /// ABOVE IT. `RefreshPaletteRow` draws NOTHING when nothing is earned, because a control
        /// whose only option is the one you already have is not a control. This one always has
        /// twelve options, so its empty state IS its full state, and hiding it for a new account
        /// would hide the only customisation a new account has.
        ///
        /// ⚠️ TWELVE SWATCHES AT 30 DEGREES, AND THE CORE TAKES ANY INTEGER. The strip is a
        /// UI choice: a wheel is more precise and much harder to press exactly, and every swatch
        /// here is the colour the character actually becomes. `CharacterLoadout.HueDegrees` is
        /// clamped to 0..359, so a finer control later is a change to this method and to nothing
        /// else.
        ///
        /// ⚠️ THERE IS NO BRIGHTNESS CONTROL AND THERE MUST NOT BE.
        /// `CharacterLoadout.SaturationPercent` and `PaletteVariants.Rotate` both record why: the
        /// toon shader bands on VALUE, so a player who could drag their own value could dress as a
        /// silhouette, and `VISION.md` § 2 rule 5 is a readability budget rather than a
        /// preference.
        ///
        /// Returns the height it took, so the column can size itself.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️ THIS ROW WAS DELETED ON 2026-08-31 AND IS BACK, NARROWED, AND THE NARROWING IS THE
        /// WHOLE POINT. `docs/TODO.md` § 107. 🧑 opened a build, saw a cyan Berto with magenta
        /// clothes and said *"i didnnt want all characters to be customizable"*. The pass that
        /// answered him removed the dial outright and replaced it with a button, which got three
        /// things wrong at once:
        ///
        /// 1. **It threw away the half he asked to keep.** The same sentence says *"maybe the
        ///    heroes we can change their clothes and shit"*. Clothes are half of what this dial
        ///    turns; skin was the other half and the only one he objected to.
        /// 2. **It could not undo the damage it was written for.** `ShowModel` still applies
        ///    `SettingsStore.LookFor`, so every hue already saved to disk was still being painted,
        ///    with the only screen that could reset it now deleted. **A player whose Berto was
        ///    green stayed green forever.**
        /// 3. **The button it left behind went nowhere.** It called
        ///    `FindFirstObjectByType&lt;CustomCharacterCreator&gt;()` and nothing in the project
        ///    ever creates one, so the press was silently swallowed. `CLAUDE.md` § 6.3: *"A dead
        ///    end is a bug. A button that dismisses to nothing is worse than no button."*
        ///
        /// **The fix is one slot list, not one screen.** `PaletteRules.IsProtectedSlot` now
        /// carries the skin ramp (13, 14, 15) beside the face (8), so the dial physically cannot
        /// reach a character's skin on any path, on either side of the wire, and the clothes stay
        /// free. The caption says CLOTHES because that is now what it does, and a control whose
        /// label overstates it is how the last version of this got reported as a bug.
        ///
        /// Returns the height it took, so the column can size itself.
        /// </summary>
        private float RefreshTintRows(Transform rows, RosterEntry entry)
        {
            if (_tab != 0 || entry == null) return 0.0f;

            var book = RosterBook.Load();
            var art = book != null ? book.PersonArt(_pick[0], SceneFlow.SelectedMode) : null;
            if (art == null || art.Palette == null || art.Palette.Length == 0) return 0.0f;

            string characterId = Roster.PersonIdAt(SceneFlow.SelectedMode, _pick[0]);
            var look = Settings.SettingsStore.LookFor(characterId);

            var tint = StripRow(rows, "CLOTHES");

            for (int step = 0; step < TintSteps; step++)
            {
                int hue = step * (360 / TintSteps);
                BuildTintSwatch(tint, art.Palette, characterId, look, hue);
            }

            var strength = StripRow(rows, "STRENGTH");

            foreach (var (label, percent) in TintStrengths)
                BuildStrengthChip(strength, characterId, look, label, percent);

            // ⚠️⚠️ THE DOOR RIDES THE STRENGTH ROW RATHER THAN TAKING A THIRD ONE, AND
            // `HeroPickerLayoutProbe` IS WHY. This column is already over-subscribed: the probe's
            // dump reads `Rows h=460 pref=644`, so the vertical group is compressing every child
            // to fit, and **a third strip row went straight into that budget and reopened the
            // 29 px dead band above the ability rows** that `docs/TODO.md` § 94 records being
            // "fixed" three times without moving. The button is 200 units wide beside three
            // 44-unit chips, which the row has room for at every one of the nine resolutions
            // `AspectRatioProbes` drives.
            RefreshCustomDoor(strength);

            return (PaletteRowHeight + 6.0f) * 2.0f;
        }

        /// <summary>
        /// The one door to the character creator, on the screen where you choose a character.
        ///
        /// ⚠️⚠️ IT IS A ROW ON THIS SCREEN RATHER THAN A SIXTH BUTTON SOMEWHERE, WHICH IS
        /// `CLAUDE.md` § 6.3 FOLLOWED RATHER THAN QUOTED. *"EVERY DESTINATION HAS A VISIBLE DOOR,
        /// AND A DOOR IS A THING THAT LOOKS PRESSABLE"*, and immediately after it, *"NEVER ADD A
        /// SECOND DOOR TO FIX A FINDABILITY PROBLEM"*. The journey is: press CHARACTER, see the
        /// cast, see MAKE YOUR OWN at the bottom of the same list, press it. Three presses from
        /// the main menu to a face you drew, and no control the player has to discover.
        ///
        /// ⚠️ IT NAMES THE ACTIVE SLOT ON THE BUTTON, so the row is a status readout and a door
        /// at once and the player never has to open it to find out which of the three is in play.
        /// § 96 is the entry about a corner chip that stated a name and a level and read as a
        /// status readout rather than a door; this one says MAKE YOUR OWN in the verb position.
        ///
        /// ⚠️ `MenuKit.WoodButton` RATHER THAN A BARE `Image` + `Button`. The version this
        /// replaced built an amber rectangle with a `Text` child at a zero-sized `RectTransform`,
        /// in a visual language nothing else on the screen speaks. `docs/VISION.md` § 6: *"His UI
        /// art is the design system... anything drawn in a different visual language is the thing
        /// that looks broken."*
        /// </summary>
        private void RefreshCustomDoor(Transform row)
        {
            var profile = CustomCharacterStore.Profile;
            var active = profile.GetActive();

            string caption = $"MAKE YOUR OWN  ·  {active.Name.ToUpperInvariant()}";

            var button = MenuKit.WoodButton(row, caption, new Vector2(0.0f, 0.5f),
                                            Vector2.zero, new Vector2(200.0f, SwatchSize),
                                            () =>
                                            {
                                                MenuSfx.Click();
                                                CustomCharacterScreen.Ensure().Open();
                                            },
                                            "WoodPrimaryButton");

            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 200.0f;
            element.preferredHeight = SwatchSize;
        }

        /// <summary>One captioned strip, built the same way the COLOURS row is.</summary>
        private Transform StripRow(Transform rows, string caption)
        {
            var row = new GameObject($"{caption}Row", typeof(RectTransform));
            row.transform.SetParent(rows, false);

            var group = row.AddComponent<HorizontalLayoutGroup>();
            group.childAlignment = TextAnchor.MiddleLeft;
            group.spacing = 6.0f;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            row.AddComponent<LayoutElement>().preferredHeight = PaletteRowHeight;

            var label = MenuKit.Label(row.transform, caption, MenuKit.MinReadableUnits,
                                      new Color(0.961f, 0.902f, 0.784f, 0.65f),
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            label.raycastTarget = false;
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 92.0f;

            return row.transform;
        }

        private void BuildTintSwatch(Transform row, Color[] authored, string characterId,
                                     CharacterLook current, int hue)
        {
            var go = new GameObject($"Tint_{hue}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(row, false);

            bool active = current.HueDegrees == hue;

            // ⚠️ DRAWN THROUGH `PaletteVariants.For`, LIKE EVERY OTHER SWATCH ON THIS SCREEN.
            // A swatch painted from an HSV constant is a swatch that can disagree with the model
            // standing beside it, which is the fault `ToonSkin.ApplySlipper`'s header records.
            var shown = new CharacterLook(current.PaletteId, hue, current.SaturationPercent);

            var face = go.GetComponent<Image>();
            face.color = PaletteVariants.For(authored, shown)[RepresentativeSlot(authored)];

            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = SwatchSize;
            element.preferredHeight = SwatchSize;

            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(go.transform, false);
            MenuKit.Stretch((RectTransform)ring.transform, -3.0f);

            var ringImage = ring.GetComponent<Image>();
            ringImage.sprite = GodotTheme.Box(new Color(0, 0, 0, 0),
                                              active ? UiTheme.Cream : UiTheme.WoodEdge,
                                              active ? 3 : 2, 4);
            ringImage.type = Image.Type.Sliced;
            ringImage.raycastTarget = false;

            var button = go.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(() =>
            {
                Settings.SettingsStore.SetLookFor(characterId, hue, current.SaturationPercent);
                MenuSfx.Click();
                Refresh();
            });

            go.AddComponent<TextureButtonFeedback>();
        }

        private void BuildStrengthChip(Transform row, string characterId, CharacterLook current,
                                       string label, int percent)
        {
            bool active = current.SaturationPercent == percent;

            var button = MenuKit.WoodButton(row, label, Vector2.zero, Vector2.zero,
                                            new Vector2(132.0f, PaletteRowHeight - 8.0f),
                                            () =>
                                            {
                                                Settings.SettingsStore.SetLookFor(
                                                    characterId, current.HueDegrees, percent);
                                                Refresh();
                                            },
                                            active ? "WoodAmberButton" : "WoodButton");

            button.name = $"Strength_{label}";

            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 132.0f;
            element.preferredHeight = PaletteRowHeight - 8.0f;
        }

        private float RefreshPaletteRow(Transform rows, RosterEntry entry)
        {
            // ⚠️ PEOPLE ONLY. A lata and a tsinelas have their own skins and their own tabs;
            // `RefreshPreview` makes the same call for the same reason.
            if (_tab != 0 || entry == null) return 0.0f;

            var owned = new List<Reward>();

            foreach (var reward in BannerRules.Earned(GameServices.Career?.Profile))
                if (reward != null && reward.Kind == RewardKind.Palette &&
                    PaletteRules.IsKnownVariant(reward.Id))
                    owned.Add(reward);

            if (owned.Count == 0) return 0.0f;

            var book = RosterBook.Load();
            var art = book != null ? book.PersonArt(_pick[0], SceneFlow.SelectedMode) : null;
            if (art == null || art.Palette == null || art.Palette.Length == 0) return 0.0f;

            string characterId = Roster.PersonIdAt(SceneFlow.SelectedMode, _pick[0]);
            string equipped = Settings.SettingsStore.PaletteFor(characterId);

            var row = new GameObject("PaletteRow", typeof(RectTransform));
            row.transform.SetParent(rows, false);

            var group = row.AddComponent<HorizontalLayoutGroup>();
            group.childAlignment = TextAnchor.MiddleLeft;
            group.spacing = 8.0f;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            row.AddComponent<LayoutElement>().preferredHeight = PaletteRowHeight;

            var caption = MenuKit.Label(row.transform, "COLOURS", MenuKit.MinReadableUnits,
                                        new Color(0.961f, 0.902f, 0.784f, 0.65f),
                                        Vector2.zero, Vector2.zero, Vector2.zero,
                                        TextAnchor.MiddleLeft);
            caption.raycastTarget = false;
            caption.gameObject.AddComponent<LayoutElement>().preferredWidth = 92.0f;

            // ⚠️ THE AUTHORED COLOURS ARE A CHOICE AND SO THEY ARE A SWATCH. Without a DEFAULT
            // there is no way back from a variant, which is a dead end and `CLAUDE.md` § 6.3
            // calls a dead end a bug.
            BuildSwatch(row.transform, art.Palette, PaletteRules.DefaultId, characterId, equipped);

            foreach (var reward in owned)
                BuildSwatch(row.transform, art.Palette, reward.Id, characterId, equipped);

            return PaletteRowHeight + 6.0f;
        }

        /// <summary>
        /// ⚠️⚠️ THE SLOT IS CHOSEN BY SATURATION RATHER THAN BY INDEX, AND SLOT 0 IS WHY. A
        /// character's sixteen slots are an atlas, not a ranking: on several of the cast slot 0
        /// is an off-white or a near-black, so a swatch reading it would paint three variants
        /// three shades of the same grey and the control would look broken rather than subtle.
        /// The most saturated slot is the one a player would call "their colour".
        ///
        /// ⚠️ THE FACE IS EXCLUDED, for the reason `PaletteRules.FaceSlot` gives: it is never
        /// rotated, so it is the one slot guaranteed to be identical on every variant.
        /// </summary>
        private static int RepresentativeSlot(Color[] authored)
        {
            int best = 0;
            float bestScore = -1.0f;

            for (int i = 0; i < authored.Length; i++)
            {
                if (i == PaletteRules.FaceSlot) continue;

                Color.RGBToHSV(authored[i], out _, out float sat, out float val);

                // ⚠️ SATURATION TIMES VALUE, NOT SATURATION. A fully saturated colour at 2 per
                // cent brightness is black, and it scores 1.0 on saturation alone.
                float score = sat * val;
                if (score <= bestScore) continue;

                bestScore = score;
                best = i;
            }

            return best;
        }

        private void BuildSwatch(Transform row, Color[] authored, string paletteId,
                                 string characterId, string equipped)
        {
            var go = new GameObject($"Swatch_{(paletteId.Length == 0 ? "default" : paletteId)}",
                                    typeof(RectTransform), typeof(Image));
            go.transform.SetParent(row, false);

            bool active = paletteId == equipped;

            // ⚠️ THE SWATCH SHOWS THE EARNED PRESET ON TOP OF THE DIAL THE PLAYER HAS
            // ALREADY TURNED, so pressing it produces the colour it was showing. A swatch drawn
            // at the preset alone would be a promise the model then breaks.
            var dial = Settings.SettingsStore.LookFor(characterId);
            var shown = new CharacterLook(paletteId, dial.HueDegrees, dial.SaturationPercent);

            var face = go.GetComponent<Image>();
            face.color = PaletteVariants.For(authored, shown)[RepresentativeSlot(authored)];

            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = SwatchSize;
            element.preferredHeight = SwatchSize;

            // ⚠️ THE BORDER IS THE SELECTED STATE AND IT IS A SEPARATE OBJECT, because tinting
            // the swatch itself to show selection would change the one thing the swatch is for.
            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(go.transform, false);
            MenuKit.Stretch((RectTransform)ring.transform, -3.0f);

            var ringImage = ring.GetComponent<Image>();
            ringImage.sprite = GodotTheme.Box(new Color(0, 0, 0, 0),
                                              active ? UiTheme.Cream : UiTheme.WoodEdge,
                                              active ? 3 : 2, 4);
            ringImage.type = Image.Type.Sliced;
            ringImage.raycastTarget = false;

            var button = go.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(() =>
            {
                Settings.SettingsStore.SetPaletteFor(characterId, paletteId);
                MenuSfx.Click();

                // ⚠️ THE WHOLE REFRESH, NOT JUST THE RINGS. `RefreshPreview` is what repaints
                // the model, and the model is the point of the control; redrawing only the
                // swatches would move a border and change nothing the player came here to see.
                Refresh();
            });

            // ⚠️ IT REACTS TO THE POINTER, because `CLAUDE.md` § 6.3 says a control that does
            // something must, and the plate that did not is exactly what § 96 is open about.
            go.AddComponent<TextureButtonFeedback>();
        }

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
                Color plate = item.ult
                    ? Color.Lerp(UiTheme.HeroPlate, accent, 0.14f)
                    : UiTheme.HeroPlate;

                var rowBg = rowGo.AddComponent<Image>();
                rowBg.sprite = GodotTheme.Box(
                    plate,
                    item.ult ? accent : UiTheme.HeroRim,
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
                glyph.color = UiTheme.HeroGlyphOn;
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
                chip.sprite = GodotTheme.Box(UiTheme.WoodDark, new Color(0, 0, 0, 0), 0, 4);
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
                    UiTheme.Cream,
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
                    UiTheme.Cream, Vector2.zero, Vector2.zero, Vector2.zero,
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

        private static readonly Color PipFilled = new Color(0.98f, 0.78f, 0.12f, 1.0f);
        private static readonly Color PipEmpty = new Color(0.35f, 0.24f, 0.18f, 0.55f);

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
                value.color = choosingHero ? UiTheme.ColorForHero(entry.Id) : UiTheme.Cream;
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

            var bayanBlue = new Color(0.64f, 0.75f, 1.0f, 1.0f);
            if (entry != null && _tab == 0)
                _glowImage.color = Color.Lerp(bayanBlue, UiTheme.ColorForHero(entry.Id), 0.65f);
            else
                _glowImage.color = bayanBlue;
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
