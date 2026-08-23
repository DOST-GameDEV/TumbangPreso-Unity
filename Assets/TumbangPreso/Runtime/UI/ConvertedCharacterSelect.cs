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
            SetText("GameBannerLabel", "CHARACTER");

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

                var button = MenuKit.WoodButton(bar, TabNames[i], Vector2.zero, Vector2.zero,
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
                if (_tabButtons[i] != null) _tabButtons[i].interactable = i != _tab;
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
                RefreshHeroLoadout(rows, entry.Id);
                return;
            }

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

        private static int _heroAbilityInspectIndex = 0;

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

            if (_heroAbilityInspectIndex < 0 || _heroAbilityInspectIndex >= abilities.Length)
                _heroAbilityInspectIndex = 0;

            // ---- The kit, as three named rows ------------------------------------
            //
            // ⚠️⚠️ ALL THREE NAMES ARE ON SCREEN AT ONCE, AND THAT IS A CORRECTION. The first
            // pass was a horizontal ribbon of three glyph tiles with a details card underneath
            // showing only the SELECTED power, which meant a player choosing a hero could see
            // exactly one of that hero's three abilities without clicking. That is the wrong
            // trade on a PICKER: the whole question this screen answers is "what does this hero
            // do", and the answer is the kit, not one third of it. Overwatch's hero panel lists
            // every ability by name for the same reason.
            //
            // ⚠️ THE SELECTED ROW EXPANDS RATHER THAN A SEPARATE CARD APPEARING. One widget
            // that grows is one thing to follow; a list plus a card that changes underneath it
            // is two, and the eye has to work out which row the card belongs to every time.
            for (int i = 0; i < abilities.Length; i++)
            {
                int index = i;
                var item = abilities[i];
                if (item.ability == null) continue;

                bool isSelected = (index == _heroAbilityInspectIndex);

                var rowGo = new GameObject($"AbilityRow_{index}");
                rowGo.AddComponent<RectTransform>();
                rowGo.transform.SetParent(rows, false);

                var rowBg = rowGo.AddComponent<Image>();
                rowBg.sprite = GodotTheme.Box(
                    UiTheme.HeroPlateRaised,
                    isSelected ? accent : UiTheme.HeroRim,
                    isSelected ? 2 : 1, 6);
                rowBg.type = Image.Type.Sliced;
                rowBg.raycastTarget = true;

                var rowBtn = rowGo.AddComponent<Button>();
                rowBtn.targetGraphic = rowBg;
                rowBtn.onClick.AddListener(() =>
                {
                    _heroAbilityInspectIndex = index;
                    MenuSfx.Click();
                    var picked = Entries[_pick[_tab]];
                    RefreshTraits(picked);
                });

                var rowCol = rowGo.AddComponent<VerticalLayoutGroup>();
                rowCol.childControlHeight = true;
                rowCol.childControlWidth = true;
                rowCol.childForceExpandHeight = false;
                rowCol.childForceExpandWidth = true;
                rowCol.spacing = 2.0f;
                rowCol.padding = new RectOffset(8, 10, 5, 5);

                var rowLe = rowGo.AddComponent<LayoutElement>();
                rowLe.preferredHeight = isSelected ? 72.0f : 36.0f;
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
                glyph.color = isSelected ? UiTheme.HeroGlyphOn : UiTheme.HeroGlyphOff;
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
                    isSelected ? accent : UiTheme.Cream,
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
                keyLabel.fontStyle = FontStyle.Bold;
                keyLabel.raycastTarget = false;
                MenuKit.Stretch(keyLabel.rectTransform);

                var nameLbl = MenuKit.Label(header.transform, item.ability.Name, 16,
                    isSelected ? accent : UiTheme.Cream,
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
                nameLbl.fontStyle = FontStyle.Bold;
                nameLbl.raycastTarget = false;
                nameLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;

                string timing = item.ult
                    ? "ULTIMATE"
                    : (item.ability.Duration > 0.0f
                        ? $"{item.ability.Cooldown:0.#}s · {item.ability.Duration:0.#}s"
                        : $"{item.ability.Cooldown:0.#}s");

                var timingLbl = MenuKit.Label(header.transform, timing, 13,
                    new Color(0.961f, 0.902f, 0.784f, 0.75f),
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleRight);
                timingLbl.fontStyle = FontStyle.Bold;
                timingLbl.raycastTarget = false;
                timingLbl.gameObject.AddComponent<LayoutElement>().minWidth = 86.0f;

                if (!isSelected) continue;

                // ---- the selected row's own readout ----
                //
                // ⚠️ IT DRAWS `Summary`, NOT `Description`. This strip is 32 px at 14 pt,
                // which is two lines; the full tactical sentences run to four or five and
                // `Truncate` cuts them SILENTLY, so the screen a player uses to CHOOSE a hero
                // was describing that hero in a sentence that stopped mid-word. The full text
                // is one key away in the match, on the inspect tray, which does not truncate.
                var kindLbl = MenuKit.Label(rowGo.transform,
                    $"[{AbilityIcons.LabelFor(item.ability.Glyph)}]", 12,
                    new Color(accent.r, accent.g, accent.b, 0.9f),
                    Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
                kindLbl.fontStyle = FontStyle.Bold;
                kindLbl.raycastTarget = false;
                kindLbl.gameObject.AddComponent<LayoutElement>().preferredHeight = 14.0f;

                var descLbl = MenuKit.Label(rowGo.transform, item.ability.Summary, 14,
                    UiTheme.Cream, Vector2.zero, Vector2.zero, Vector2.zero,
                    TextAnchor.UpperLeft);
                descLbl.raycastTarget = false;
                descLbl.horizontalOverflow = HorizontalWrapMode.Wrap;
                descLbl.verticalOverflow = VerticalWrapMode.Overflow;
                descLbl.gameObject.AddComponent<LayoutElement>().preferredHeight = 32.0f;
            }

            var hint = MenuKit.Label(rows,
                "Click a power to read it · hold [" + Hud.KeyLabelFor("AbilityInfo") + "] in match",
                12, new Color(0.961f, 0.902f, 0.784f, 0.65f),
                Vector2.zero, Vector2.zero,
                Vector2.zero, TextAnchor.MiddleLeft);
            hint.raycastTarget = false;
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 16.0f;
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

            SetText("NameCaption", "NAME:");
            SetText("CharValueLabel", entry.Name);
            SetText("TaglineLabel", TaglineFor(entry.Id));

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
                case "dante": return "Earth / Demonic Juggernaut. Ground-shattering tremors, iron poise that resists stuns, and unstoppable momentum.";
                case "cheska": return "Ice / Frost Striker. Controls the court with permafrost slip zones, crystal ice barricades, and glacial freeze.";
                case "sean": return "Fire / Explosive Powerhouse. High-octane kinetic charge, explosive slipper cannons, and crater-smashing ultimates.";
                case "zack": return "Electric / Lightning Skater. High-speed electric dash, overcharged lightning throws, and thunderstrike overdrive.";
                case "nemu": return "Spirit / Ghost Summoner. Phases between dimensions, commands spectral companion Kuro, and creates drowsy seance voids.";

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
