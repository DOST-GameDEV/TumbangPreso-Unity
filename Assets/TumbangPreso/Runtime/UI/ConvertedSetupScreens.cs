using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ported from `match_setup.gd` (1,861 lines).
    ///
    /// ⚠️ THERE ARE FOUR SELECTORS HERE, NOT ONE. Map, MODE, difficulty and fighter. An earlier
    /// rebuild of this screen had only a map picker, which quietly dropped the mode and the bot
    /// difficulty out of the game entirely.
    ///
    /// ⚠️ AND THE SEAT LIST IS PART OF SETUP, NOT A LOBBY-ONLY THING. Four seat buttons show
    /// who is taya first and which seats are bots, in single player as well, because the taya
    /// rotation is the game's fairness argument and the player is entitled to see where they
    /// sit in it before committing.
    /// </summary>
    public sealed class ConvertedMatchSetup : ConvertedScreen
    {
        private int _map;
        private int _mode;
        private int _difficulty = 1;

        private static readonly string[] Modes = { "CLASSIC" };
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

            OnClick("MapPrevButton", () => Cycle(ref _map, SceneFlow.Maps.Length, -1));
            OnClick("MapNextButton", () => Cycle(ref _map, SceneFlow.Maps.Length, 1));

            OnClick("ModePrevButton", () => Cycle(ref _mode, Modes.Length, -1));
            OnClick("ModeNextButton", () => Cycle(ref _mode, Modes.Length, 1));

            OnClick("DifficultyPrevButton", () => Cycle(ref _difficulty, Difficulties.Length, -1));
            OnClick("DifficultyNextButton", () => Cycle(ref _difficulty, Difficulties.Length, 1));

            OnClick("CharacterButton", () => SceneFlow.Go(SceneFlow.CharacterSelect));
            OnClick("StartButton", SceneFlow.StartMatch);
            OnClick("BackButton", () => SceneFlow.Go(SceneFlow.ModeSelect));

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
            int firstTaya = MatchRules.DefenderSlotFor(1);

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                string label = $"P{slot + 1}";

                if (slot == firstTaya) label += "  ·  TAYA FIRST";
                label += slot == 0 && !SceneFlow.Networked ? "  ◄ YOU" : "  ·  BOT";

                SetText($"SeatButton{slot}", label);
            }
        }

        private void Cycle(ref int index, int count, int delta)
        {
            if (count <= 0) return;

            index = ((index + delta) % count + count) % count;
            Refresh();
        }

        private void Refresh()
        {
            SceneFlow.SelectedMap = SceneFlow.Maps[Mathf.Clamp(_map, 0, SceneFlow.Maps.Length - 1)];

            SetText("MapValueLabel", SceneFlow.SelectedMap.ToUpperInvariant().Replace("BAYANPLAZA", "BAYAN PLAZA"));
            SetText("ModeValueLabel", Modes[_mode]);
            SetText("DifficultyValueLabel", Difficulties[_difficulty]);

            SetText("DetailLabel", MapBlurbs.TryGetValue(SceneFlow.SelectedMap, out var blurb)
                ? blurb
                : SceneFlow.SelectedMap);

            var s = Settings.SettingsStore.Current;
            s.AiDifficulty = _difficulty;

            // The fighter row shows all three picks, because a player chooses three things.
            string person = Roster.At(Roster.People, s.CharacterPick)?.Name ?? "BERTO";
            string can = Roster.At(Roster.Cans, s.CanPick)?.Name ?? "PASIP";
            string slipper = Roster.At(Roster.Slippers, s.SlipperPick)?.Name ?? "TSINELAS";

            SetText("CharacterButton", $"{person} · {can} · {slipper}");
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

            OnClick("CharPrevButton", () => CycleEntry(-1));
            OnClick("CharNextButton", () => CycleEntry(1));
            OnClick("ConfirmButton", Confirm);
            OnClick("BackButton", () => SceneFlow.Go(SceneFlow.MatchSetup));

            WireTabs();
            Refresh();
        }

        /// <summary>The TabBar carries one button per tab, named for its list.</summary>
        private void WireTabs()
        {
            var bar = Node("TabBar");
            if (bar == null) return;

            for (int i = 0; i < bar.childCount && i < TabNames.Length; i++)
            {
                int index = i;
                var btn = bar.GetChild(i).GetComponent<Button>();
                if (btn == null) continue;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _tab = index;
                    GameServices.Audio?.PlayAt("ui_click", Vector3.zero);
                    Refresh();
                });
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
            var entry = Entries[_pick[_tab]];

            SetText("NameCaption", "NAME:");
            SetText("CharValueLabel", entry.Name);
            SetText("TaglineLabel", TaglineFor(entry.Id));

            var labels = MeterLabels[_tab];
            var rows = Node("TraitRows");
            if (rows == null) return;

            int[] points = { entry.Bilis, entry.Lakas, entry.Tatag };

            for (int i = 0; i < rows.childCount && i < 3; i++)
            {
                var row = rows.GetChild(i);
                var text = row.GetComponent<Text>() ?? row.GetComponentInChildren<Text>();
                if (text == null) continue;

                var sb = new System.Text.StringBuilder();
                sb.Append(labels[i].PadRight(10));

                // ⚠️ FIVE PIPS, ALWAYS, filled to the entry's points. A bar scaled to a
                // percentage hides that the scale is 1..5, and the whole design of these stats
                // is that a point is a small, countable thing.
                for (int p = 1; p <= Roster.TraitMax; p++) sb.Append(p <= points[i] ? "■" : "□");

                text.text = sb.ToString();
            }
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

            SceneFlow.Go(SceneFlow.MatchSetup);
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

            BuildAddressField();
            _net.BrowseLan();
            SetText("StatusLabel", _net.Status);
        }

        /// <summary>The converted JoinAddressEdit is a plain node; make it typable.</summary>
        private void BuildAddressField()
        {
            var t = Node("JoinAddressEdit");
            if (t == null) return;

            var img = t.GetComponent<Image>();
            if (img == null) img = t.gameObject.AddComponent<Image>();
            img.color = UiTheme.Card;

            var textGo = new GameObject("Text");
            textGo.AddComponent<RectTransform>();
            textGo.transform.SetParent(t, false);

            var text = textGo.AddComponent<Text>();
            text.font = MenuKit.Font;
            text.fontSize = 26;
            text.color = UiTheme.Ink;
            text.alignment = TextAnchor.MiddleCenter;
            MenuKit.Stretch(text.rectTransform);

            _address = t.gameObject.AddComponent<InputField>();
            _address.textComponent = text;
            _address.text = "127.0.0.1";
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
