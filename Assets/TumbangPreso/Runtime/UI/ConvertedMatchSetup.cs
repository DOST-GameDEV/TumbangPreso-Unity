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

        /// <summary>`match_setup.gd::_unhandled_input` backs out to the mode screen on Escape.</summary>
        protected override string CancelTarget => SceneFlow.ModeSelect;

        /// <summary>
        /// ⚠️⚠️ ONE STEP PER PRESS, ENFORCED, BECAUSE THE SELECTORS WERE REPORTED AS
        /// UNCONTROLLABLE: *"clicking next on stuff like bot is a shitty experience u cant
        /// control it it goes too fast"*. `match_setup.gd` wires `pressed` and has NO repeat and
        /// NO hold behaviour at all — one click is one step, and anything faster than that is
        /// the conversion adding a behaviour the original never had.
        ///
        /// The guard is a time window rather than a flag because it has to survive whatever is
        /// producing the extra step (a doubled event, a second raycaster, a held pointer), and
        /// 0.12 s is under any real double-click while being far longer than a frame.
        /// </summary>
        private const float CycleGuard = 0.12f;
        private float _lastCycle = -1.0f;

        private MapPreviewSurface _preview;
        private Transform _characterPanel;
        private Button _spectate;

        /// <summary>The one string both the solo and the networked board reach for, so the two
        /// cannot drift into marking the same seat two different ways.</summary>
        private const string YouMark = "◀ YOU";

        private static readonly string[] Difficulties = { "EASY", "NORMAL", "HARD" };

        protected override void Wire()
        {
            SetText("BannerLabel", SceneFlow.Networked ? "MULTIPLAYER" : "SINGLE PLAYER");

            SetText("SeatHeading", "YOUR CHARACTER");

            // ⚠️ THE WHOLE HINT, WORD FOR WORD FROM `match_setup.gd::_setup_solo`. The
            // conversion carried the first sentence and dropped the second, which is the half
            // that explains what the empty seats are: *"Empty seats are bots — the kids from
            // the street who fill in."* It also carries the taya rotation, which is the game's
            // fairness argument and the reason the seat board is clickable at all.
            //
            // ⚠️ THE DASH IS THE GAME'S OWN, NOT THIS FILE'S PROSE. The house style bans em
            // dashes in anything WE write; this is a shipped string being transcribed verbatim
            // so the two builds read identically on screen, and changing it would be a
            // divergence, not a style fix.
            SetText("SeatHint",
                    "Four players, one taya. The taya rotates every round, so everyone defends "
                    + "exactly once. Empty seats are bots — the kids from the street who fill in.");

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
            if (Time.unscaledTime - _lastCycle < CycleGuard) return;

            _lastCycle = Time.unscaledTime;

            index = ((index + delta) % count + count) % count;

            // ⚠️ THE ARROWS CARRY THEIR OWN AUDIO. `match_setup.gd::_wire_selector` exists for
            // exactly this reason — "one helper rather than six pairs of lines, so a new
            // selector row cannot be added with half its audio missing". These are TextureButtons
            // rather than pennants, so they get nothing from the shared button skin.
            MenuSfx.Click();
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

            // ⚠️ FROM THE MAP REGISTRY, NOT A SECOND TABLE. `game_launch.gd` calls itself "the
            // single place a map is named", and a blurb dictionary living here is a second place
            // that is free to disagree with the picker beside it.
            SetText("DetailLabel", SceneFlow.PreviewFor(SceneFlow.SelectedMap).Detail);

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
}
