using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
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
            if (_tab == 0 && SceneFlow.SelectedMode == GameMode.HeroStrike)
            {
                if (rows.TryGetComponent<LayoutElement>(out var heroRowsLayout))
                    heroRowsLayout.preferredHeight = 214.0f;
                RefreshHeroLoadout(rows, entry.Id);
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

        private void RefreshHeroLoadout(Transform rows, string heroId)
        {
            var kit = HeroAbilitySystem.CreateKitFor(heroId);
            Color accent = UiTheme.ColorForHero(heroId);

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
                var rowLe = rowGo.AddComponent<LayoutElement>();
                rowLe.preferredHeight = 61.0f;
                rowLe.minHeight = rowLe.preferredHeight;

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

                var nameLbl = MenuKit.Label(header.transform, item.ability.Name, 16,
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
                // for text the reader may skip, and a player choosing a hero for the first time
                // cannot skip this one.
                //
                // ⚠️ THE ROW GREW WITH IT. A taller line inside a `preferredHeight` that did not
                // move would push the description into the plate's bottom border, which is the
                // fault this was supposed to fix wearing a different hat.
                var descLbl = MenuKit.Label(rowGo.transform, item.ability.Summary, 15,
                    UiTheme.Cream, Vector2.zero, Vector2.zero, Vector2.zero,
                    TextAnchor.UpperLeft);
                descLbl.raycastTarget = false;
                descLbl.horizontalOverflow = HorizontalWrapMode.Wrap;
                descLbl.verticalOverflow = VerticalWrapMode.Overflow;
                descLbl.gameObject.AddComponent<LayoutElement>().preferredHeight = 20.0f;
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

                // ⚠️ AND THE BOX COMES DOWN TO THE TEXT. Aligning alone only moves the gap to
                // the bottom of the box; the rows below would not shift a pixel. Two lines at
                // 18 pt with `lineSpacing` 1.0 is about 44 px, so 48 holds them with a margin
                // and hands the rest of the panel back. The Classic tab keeps a taller box
                // because its taglines run longer and it has no ability rows competing for the
                // same height.
                if (tagline.TryGetComponent<LayoutElement>(out var taglineLayout))
                    taglineLayout.preferredHeight = choosingHero ? 48.0f : 96.0f;
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
            preview.Show(art == null ? null : art.Model, art == null ? null : art.Clips,
                         art == null ? null : art.Palette, art == null ? null : art.PetModel);
        }

        /// <summary>
        /// ⚠️ THE SENTENCE AND THE METERS MUST AGREE. The roster rule is that the number is
        /// readable off the sentence: if a description says somebody is quick, SPEED is high. A
        /// stat nobody can predict from the lore is a random modifier, and a description nothing
        /// backs up is a lie the player finds out about in round 2.
        /// </summary>
        private static string TaglineFor(string id)
        {
            switch (id)
            {
                // Hero Strike Roster
                case "dante": return "EARTH JUGGERNAUT\nBreak formations with tremors, armor, and a map-splitting fissure.";
                case "cheska": return "ICE CONTROLLER\nCreate slip zones and barricades, then lock the lane with Glacial Nova.";
                case "sean": return "FIRE BRAWLER\nRush the lane, blast open space, and finish with Supernova.";
                case "zack": return "LIGHTNING SKIRMISHER\nSprint through fights, build charge, and call down Thunderstrike.";
                case "nemu": return "SPIRIT TRICKSTER\nPhase through danger, project Kuro, and pull enemies into the void.";

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

                case "tsinelas": return "Plain rubber, one peso of it. Every child on this street has thrown a pair, and it does everything well enough.";
                case "crocs": return "Holes in the top, strap at the back. Heavy and it does not fly straight, but whoever body-blocks it knows all about it.";
                case "pantulog": return "Lola's house slipper, worn soft. No weight behind it at all, but it is ready again before the taya has turned around.";
                case "sike": return "Definitely not the real brand. Light, loud, and the quickest thing off a hand on this street.";

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
