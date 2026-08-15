using System.Collections.Generic;
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

        protected override void Wire()
        {
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

        /// <summary>
        /// The trait meters, as chalk marks.
        ///
        /// ⚠️⚠️ FIVE TALLY SLOTS, NOT A PROGRESS BAR, AND THAT IS THE GAME'S OWN LANGUAGE. The
        /// whole match is played inside a chalk court: the base circle, the throwing line and
        /// the confinement square are all drawn as chalk on asphalt. A bar scaled to a
        /// percentage is the most generic UI object there is and it hides that the scale is 1..5;
        /// five marks scratched on the ground is what a kid keeping score in the street does,
        /// and a point stays a small countable thing.
        ///
        /// ⚠️ AND THE COLOUR IS `HIGHLIGHT`, the same yellow as the base-circle decal and the
        /// timer's urgency state, so a full meter reads as the same system rather than a new
        /// colour nobody has seen.
        /// </summary>
        private void RefreshTraits(RosterEntry entry)
        {
            var rows = Node("TraitRows");
            if (rows == null) return;

            for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);

            var labels = MeterLabels[_tab];
            int[] points = { entry.Bilis, entry.Lakas, entry.Tatag };

            for (int i = 0; i < labels.Length && i < points.Length; i++)
                BuildTraitRow(rows, labels[i], points[i]);

            // The camera controls are discoverable only if something says they exist. One line,
            // inside the panel, rebuilt with the meters so a roster change cannot orphan it.
            var hint = MenuKit.Label(rows, "Drag to turn the view  ·  scroll to zoom  ·  " +
                                     "right-click to reset", 15,
                                     new Color(0.961f, 0.902f, 0.784f, 0.5f),
                                     Vector2.zero, Vector2.zero, Vector2.zero,
                                     TextAnchor.MiddleLeft);

            hint.raycastTarget = false;
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 26.0f;
        }

        private static readonly Color PipFilled = new Color(0.973f, 0.816f, 0.157f);
        private static readonly Color PipEmpty = new Color(0.961f, 0.902f, 0.784f, 0.20f);

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
            row.spacing = 14.0f;

            rowGo.AddComponent<LayoutElement>().preferredHeight = 30.0f;

            var label = MenuKit.Label(rowGo.transform, name, 21, PipFilled, Vector2.zero,
                                      Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            label.raycastTarget = false;

            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 126.0f;

            var pipsGo = new GameObject("Pips");
            pipsGo.AddComponent<RectTransform>();
            pipsGo.transform.SetParent(rowGo.transform, false);

            var pips = pipsGo.AddComponent<HorizontalLayoutGroup>();
            pips.childControlHeight = true;
            pips.childControlWidth = true;
            pips.childForceExpandHeight = false;
            pips.childForceExpandWidth = false;
            pips.childAlignment = TextAnchor.MiddleLeft;
            pips.spacing = 6.0f;

            for (int i = 0; i < Roster.TraitMax; i++)
            {
                var pipGo = new GameObject($"Pip{i}");
                pipGo.AddComponent<RectTransform>();
                pipGo.transform.SetParent(pipsGo.transform, false);

                var pip = pipGo.AddComponent<Image>();
                pip.color = i < points ? PipFilled : PipEmpty;
                pip.raycastTarget = false;

                var element = pipGo.AddComponent<LayoutElement>();
                element.preferredWidth = 42.0f;
                element.preferredHeight = 12.0f;
            }
        }

        private IReadOnlyList<RosterEntry> Entries =>
            _tab == 0 ? Roster.People : (_tab == 1 ? Roster.Cans : Roster.Slippers);

        private void CycleEntry(int delta)
        {
            int n = Entries.Count;
            _pick[_tab] = ((_pick[_tab] + delta) % n + n) % n;
            Refresh();
        }

        private void Refresh()
        {
            var entry = Entries[Mathf.Clamp(_pick[_tab], 0, Entries.Count - 1)];

            SetText("NameCaption", "NAME:");
            SetText("CharValueLabel", entry.Name);
            SetText("TaglineLabel", TaglineFor(entry.Id));

            RefreshTabs();
            RefreshTraits(entry);
            ShowModel(entry);
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

            var art = _tab == 0 ? book.PersonArt(_pick[0])
                    : (_tab == 1 ? book.CanArt(_pick[1]) : book.SlipperArt(_pick[2]));

            // ⚠️ A LATA AND A TSINELAS LIE ON THE GROUND and need the steeper look-down angle;
            // a person is framed standing. Same preview, two framings, chosen by category.
            preview.Show(art == null ? null : art.Model, flat: _tab != 0);
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
                case "berto": return "The original defender. Immovable, unhurriable, and still standing exactly where you left him.";
                case "maring": return "Quick hands, quicker mouth. She has talked her way out of more tags than she has dodged.";
                case "totoy": return "Raised barefoot in the eskinita. Nobody in this town has caught him twice.";
                case "inday": return "Minds the corner stall and is afraid of absolutely nothing that walks past it.";
                case "kuya_boy": return "Eldest of seven. He has been the taya since before he could count, and both the arm and the footwork know it.";
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

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            MenuSfx.Back();
            Dismiss();
        }
    }
}
