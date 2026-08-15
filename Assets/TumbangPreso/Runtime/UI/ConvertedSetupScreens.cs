using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `match_setup.gd` (2,015 lines).
    ///
    /// ⚠️ THERE ARE FOUR SELECTORS HERE, NOT ONE. Map, MODE, difficulty and fighter. An earlier
    /// rebuild of this screen had only a map picker, which quietly dropped the mode and the bot
    /// difficulty out of the game entirely.
    ///
    /// ⚠️ AND THE MODE ROW IS HIDDEN, DELIBERATELY. `match_setup.gd:289` sets
    /// `mode_row.visible = false`: the two-entry picker lost its second entry and a selector
    /// with one choice is a control that teaches the player it does nothing. The row stays in
    /// the scene because the mode is a real axis that may come back; it is just not offered.
    ///
    /// ⚠️⚠️ THE SEAT LIST IS PART OF SETUP AND IT IS CLICKABLE, IN SINGLE PLAYER TOO. Four seat
    /// rows show who is taya first and which seats are bots, and pressing one MOVES you, because
    /// the taya rotation is the game's fairness argument and the player is entitled to choose
    /// where they sit in it before committing. The conversion drew them as dead labels.
    ///
    /// ⚠️ AND THERE IS A FIFTH SEAT. SPECTATE is seat -1 and it is built in code rather than
    /// authored, because it belongs beside the four rows: it answers the same question they do.
    /// </summary>
    public sealed class ConvertedMatchSetup : ConvertedScreen
    {
        private int _map;
        private int _difficulty = 1;

        private MapPreviewSurface _preview;
        private Transform _characterPanel;
        private Button _spectate;

        /// <summary>The one string both the solo and the networked board reach for, so the two
        /// cannot drift into marking the same seat two different ways.</summary>
        private const string YouMark = "◀ YOU";

        private static readonly string[] Difficulties = { "EASY", "NORMAL", "HARD" };

        /// <summary>
        /// ⚠️ THE BLURBS ARE THE GODOT ONES, WORD FOR WORD. They tell a player what the arena
        /// actually is in the vocabulary the game teaches (sari-sari, sampay, kanal), which is
        /// how those words get learned.
        /// </summary>
        private static readonly Dictionary<string, string> MapBlurbs = new Dictionary<string, string>
        {
            { "Eskinita", "ESKINITA   Urban side street. Sari-sari, sampay, kanal." },
            { "BayanPlaza", "BAYAN PLAZA   Barangay plaza. Church, basketball ring, acacia." },
        };

        protected override void Wire()
        {
            SetText("BannerLabel", SceneFlow.Networked ? "MULTIPLAYER" : "SINGLE PLAYER");

            _map = Mathf.Max(0, System.Array.IndexOf(SceneFlow.Maps, SceneFlow.SelectedMap));
            _difficulty = Mathf.Clamp(Settings.SettingsStore.Current.AiDifficulty, 0, 2);

            var previewNode = Node("MapPreview");
            if (previewNode != null) _preview = previewNode.GetComponent<MapPreviewSurface>();

            _characterPanel = Node("CharacterSelectPanel");

            OnClick("MapPrevButton", () => Cycle(ref _map, SceneFlow.Maps.Length, -1));
            OnClick("MapNextButton", () => Cycle(ref _map, SceneFlow.Maps.Length, 1));

            OnClick("DifficultyPrevButton", () => Cycle(ref _difficulty, Difficulties.Length, -1));
            OnClick("DifficultyNextButton", () => Cycle(ref _difficulty, Difficulties.Length, 1));

            OnClick("CharacterButton", OpenCharacterSelect);
            OnClick("PrimaryButton", SceneFlow.StartMatch);
            OnClick("StartButton", SceneFlow.StartMatch);
            OnClick("BackButton", () => SceneFlow.Go(SceneFlow.ModeSelect));

            // ⚠️ HIDDEN, NOT DELETED. See the class note: one choice is not a choice.
            var modeRow = Node("ModeRow");
            if (modeRow != null) modeRow.gameObject.SetActive(false);

            BuildSpectateButton();
            WireSeats();
            Refresh();
        }

        /// <summary>
        /// ⚠️ SEAT 0 IS TAYA FIRST BY CONSTRUCTION, not by a flag. The defender is
        /// `(round - 1) % 4`, so round 1's taya is always seat 0. Printing it from the rule
        /// rather than hard-coding "P1" means the label cannot disagree with the game.
        /// </summary>
        private void WireSeats()
        {
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                int seat = slot;
                var node = Node($"SeatButton{slot}");
                if (node == null) continue;

                var button = node.GetComponent<Button>();
                if (button == null) continue;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    // Solo has no peers, so the seat buttons write the launch value directly.
                    GameLaunch.SoloSeat = seat;
                    GameLaunch.Spectator = false;

                    MenuSfx.Click();
                    RefreshSeats();
                });
            }

            RefreshSeats();
        }

        private static string SeatName(int seat)
        {
            string label = $"P{seat + 1}";

            if (seat == MatchRules.DefenderSlotFor(1)) label += "  ·  TAYA FIRST";
            return label;
        }

        private void RefreshSeats()
        {
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                bool mine = !GameLaunch.Spectator && seat == GameLaunch.SoloSeat;

                SetText($"SeatButton{seat}",
                        mine ? $"{SeatName(seat)}   {YouMark}" : $"{SeatName(seat)}   · BOT");

                // ⚠️ A SPECTATOR'S SEAT ROWS ALL GO DEAD. Leaving them live would let a player
                // highlight a chair while the button above says they are watching, which is two
                // controls asserting different things about the same choice.
                var node = Node($"SeatButton{seat}");
                var button = node == null ? null : node.GetComponent<Button>();

                if (button != null) button.interactable = !GameLaunch.Spectator;
            }

            RefreshSpectate();
        }

        /// <summary>
        /// ⚠️⚠️ THE FIFTH SEAT, BUILT IN CODE RATHER THAN AUTHORED, exactly as the Godot build
        /// does it. Spectating is seat -1: no role, no character. It sits beside the four rows
        /// because it answers the same question they do, and putting it anywhere else would make
        /// it read as a mode switch that discards the map and difficulty just chosen.
        /// </summary>
        private void BuildSpectateButton()
        {
            var heading = Node("SeatHeading");
            if (heading == null) return;

            _spectate = MenuKit.WoodButton(heading.parent, "SPECTATE", Vector2.zero, Vector2.zero,
                                           new Vector2(176.0f, 46.0f), ToggleSpectate);

            _spectate.name = "SpectateButton";

            // It shares the heading's row, pinned to the right of the panel.
            var rt = _spectate.GetComponent<RectTransform>();
            rt.SetSiblingIndex(heading.GetSiblingIndex());

            var element = _spectate.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 176.0f;
            element.preferredHeight = 46.0f;
            element.ignoreLayout = true;

            rt.anchorMin = new Vector2(1.0f, 1.0f);
            rt.anchorMax = new Vector2(1.0f, 1.0f);
            rt.pivot = new Vector2(1.0f, 1.0f);
            rt.anchoredPosition = new Vector2(-24.0f, -18.0f);
            rt.sizeDelta = new Vector2(176.0f, 46.0f);

            var label = _spectate.GetComponentInChildren<Text>();
            if (label != null) label.fontSize = 19;
        }

        private void ToggleSpectate()
        {
            GameLaunch.Spectator = !GameLaunch.Spectator;
            RefreshSeats();
        }

        private void RefreshSpectate()
        {
            if (_spectate == null) return;

            var skin = _spectate.GetComponent<GodotButton>();
            if (skin == null) return;

            // The lit face IS the on state, which is how a toggle reads without a tick.
            skin.Variation = GameLaunch.Spectator ? "WoodPrimaryButton" : "WoodButton";
            skin.Apply();
            skin.Refresh();
        }

        private void Cycle(ref int index, int count, int delta)
        {
            if (count <= 0) return;

            index = ((index + delta) % count + count) % count;
            Refresh();
        }

        /// <summary>
        /// ⚠️ SHOWN IN PLACE, NOT LOADED AS A SCENE. `MatchSetup.tscn` instances the whole
        /// character screen as a hidden child and reveals it, so the map, the difficulty and the
        /// seat the player already chose are still there behind it when they come back.
        /// </summary>
        private void OpenCharacterSelect()
        {
            if (_characterPanel == null)
            {
                SceneFlow.Go(SceneFlow.CharacterSelect);
                return;
            }

            _characterPanel.gameObject.SetActive(true);

            var select = _characterPanel.GetComponent<ConvertedCharacterSelect>();
            if (select == null) return;

            select.Closed -= OnCharacterChosen;
            select.Closed += OnCharacterChosen;
        }

        private void OnCharacterChosen() => Refresh();

        private void Refresh()
        {
            SceneFlow.SelectedMap = SceneFlow.Maps[Mathf.Clamp(_map, 0, SceneFlow.Maps.Length - 1)];

            SetText("MapValueLabel",
                    SceneFlow.SelectedMap.ToUpperInvariant().Replace("BAYANPLAZA", "BAYAN PLAZA"));

            SetText("DifficultyValueLabel", Difficulties[_difficulty]);

            SetText("DetailLabel", MapBlurbs.TryGetValue(SceneFlow.SelectedMap, out var blurb)
                ? blurb
                : SceneFlow.SelectedMap);

            // ⚠️ THE PREVIEW FOLLOWS THE SELECTOR. Picking a map that changes only a word is the
            // single loudest "this screen is a mock-up" signal the front end can send.
            if (_preview != null) _preview.Show(SceneFlow.SelectedMap);

            var s = Settings.SettingsStore.Current;
            s.AiDifficulty = _difficulty;
            AIController.ApplyDifficulty(_difficulty);

            // The fighter row shows all three picks, because a player chooses three things.
            string person = Roster.At(Roster.People, s.CharacterPick)?.Name ?? "BERTO";
            string can = Roster.At(Roster.Cans, s.CanPick)?.Name ?? "PASIP";
            string slipper = Roster.At(Roster.Slippers, s.SlipperPick)?.Name ?? "TSINELAS";

            SetText("CharacterButton", $"{person} · {can} · {slipper}");

            RefreshSeats();
        }
    }

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

    /// <summary>
    /// Ported from `multiplayer_setup.gd`.
    ///
    /// ⚠️ HOSTING ONLINE AND HOSTING ON THE LAN ARE TWO DIFFERENT BUTTONS, and they always were.
    /// The online path is what the Singapore VPS serves, and collapsing them into one control
    /// removes the mode the team actually ships to players outside the room.
    /// </summary>
    public sealed class ConvertedMultiplayerSetup : ConvertedScreen
    {
        private Net.NetSession _net;
        private InputField _address;

        protected override void Wire()
        {
            _net = Net.NetSession.Ensure();
            _net.StatusChanged += s => SetText("StatusLabel", s);

            SetText("BannerLabel", "MULTIPLAYER");

            OnClick("HostOnlineButton", () =>
            {
                if (_net.StartHost()) SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("HostButton", () =>
            {
                if (_net.StartHost()) SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("JoinButton", Join);
            OnClick("BackButton", () => SceneFlow.Go(SceneFlow.ModeSelect));

            BindAddressField();
            _net.BrowseLan();
            SetText("StatusLabel", _net.Status);
        }

        /// <summary>The converted JoinAddressEdit is a real LineEdit now, so this only seeds it.</summary>
        private void BindAddressField()
        {
            var t = Node("JoinAddressEdit");
            if (t == null) return;

            _address = t.GetComponent<InputField>();
            if (_address == null) return;

            if (string.IsNullOrWhiteSpace(_address.text)) _address.text = "127.0.0.1";
        }

        private void Join()
        {
            string addr = _address == null || string.IsNullOrWhiteSpace(_address.text)
                ? "127.0.0.1"
                : _address.text.Trim();

            _net.StartClient(addr);
        }
    }

    /// <summary>
    /// Ported from `match_result.gd`.
    ///
    /// ⚠️ IT RANKS, IT DOES NOT JUST LIST. Four places with a position, a name and points, and
    /// ⚠️ A TIE AT THE TOP IS AN HONEST DRAW rather than being broken by seat order, because
    /// breaking it that way would hand round 1's taya a structural advantage in a game whose
    /// whole fairness argument is that the seats are symmetric.
    /// </summary>
    public sealed class ConvertedMatchResult : ConvertedScreen
    {
        public static int[] Scores = new int[Balance.PlayerCount];
        public static string[] Names = { "P1", "P2", "P3", "P4" };
        public static int WinningSlot = -1;

        protected override void Wire()
        {
            SetText("MessageLabel", WinningSlot < 0
                ? "A DRAW"
                : $"{Names[Mathf.Clamp(WinningSlot, 0, Names.Length - 1)]} WINS");

            var order = new List<int>();
            for (int i = 0; i < Balance.PlayerCount; i++) order.Add(i);
            order.Sort((a, b) => Scores[b].CompareTo(Scores[a]));

            for (int place = 0; place < order.Count; place++)
            {
                var root = Node($"Place{place}");
                if (root == null) continue;

                int slot = order[place];
                SetChildText(root, "Place", $"{place + 1}");
                SetChildText(root, "Name", Names[slot]);
                SetChildText(root, "Points", Scores[slot].ToString());
            }

            OnClick("RematchButton", SceneFlow.StartMatch);
            OnClick("MenuButton", () => SceneFlow.Go(SceneFlow.MainMenu));
        }

        private static void SetChildText(Transform root, string childName, string value)
        {
            var child = root.Find(childName);
            if (child == null) return;

            var text = child.GetComponent<Text>();
            if (text != null) text.text = value;
        }
    }
}
