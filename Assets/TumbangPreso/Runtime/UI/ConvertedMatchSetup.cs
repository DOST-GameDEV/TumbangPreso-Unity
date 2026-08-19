using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Net;
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
    ///
    /// ⚠️ LOBBY SYNCHRONIZATION (N5). Leader controls map and difficulty. All peers sync picks
    /// and ready status via MatchRpc, and the join code is surfaced when hosting.
    /// </summary>
    public sealed class ConvertedMatchSetup : ConvertedScreen
    {
        private int _map;
        private int _difficulty = 1;

        /// <summary>`match_setup.gd::_unhandled_input` backs out to the mode screen on Escape.</summary>
        protected override string CancelTarget => SceneFlow.ModeSelect;

        /// <summary>
        /// ⚠️⚠️ ONE STEP PER PRESS, ENFORCED, BECAUSE THE SELECTORS WERE REPORTED AS
        /// UNCONTROLLABLE. `match_setup.gd` wires `pressed` and has NO repeat and NO hold
        /// behaviour at all. 0.12s guard window prevents double-click skips.
        /// </summary>
        private const float CycleGuard = 0.12f;
        private float _lastCycle = -1.0f;

        private MapPreviewSurface _preview;
        private Transform _characterPanel;
        private Button _spectate;
        private bool _localReady;

        private readonly int[] _replicatedPicks = new int[Balance.PlayerCount * 4];

        /// <summary>The one string both the solo and the networked board reach for, so the two
        /// cannot drift into marking the same seat two different ways.</summary>
        private const string YouMark = "◀ YOU";

        private static readonly string[] Difficulties = { "EASY", "NORMAL", "HARD" };

        protected override void Wire()
        {
            for (int i = 0; i < _replicatedPicks.Length; i++) _replicatedPicks[i] = -1;

            var net = NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;

            if (isNetworked)
            {
                string code = net.Lobby.JoinCode;
                SetText("BannerLabel", string.IsNullOrEmpty(code) ? "MULTIPLAYER" : $"MULTIPLAYER · CODE {code}");
            }
            else
            {
                SetText("BannerLabel", "SINGLE PLAYER");
            }

            SetText("SeatHeading", "YOUR CHARACTER");

            SetText("SeatHint",
                    "Four players, one taya. The taya rotates every round, so everyone defends "
                    + "exactly once. Empty seats are bots, the kids from the street who fill in.");

            _map = Mathf.Max(0, Array.IndexOf(SceneFlow.Maps, SceneFlow.SelectedMap));
            _difficulty = Mathf.Clamp(Settings.SettingsStore.Current.AiDifficulty, 0, 2);

            var previewNode = Node("MapPreview");
            if (previewNode != null) _preview = previewNode.GetComponent<MapPreviewSurface>();

            _characterPanel = Node("CharacterSelectPanel");

            OnClick("MapPrevButton", () => OnMapCycle(-1));
            OnClick("MapNextButton", () => OnMapCycle(1));

            OnClick("DifficultyPrevButton", () => OnDifficultyCycle(-1));
            OnClick("DifficultyNextButton", () => OnDifficultyCycle(1));

            OnClick("CharacterButton", OpenCharacterSelect);
            OnClick("PrimaryButton", OnPrimaryPressed);
            OnClick("StartButton", OnPrimaryPressed);
            OnClick("BackButton", () =>
            {
                if (net != null && net.IsNetworked) net.Stop();
                SceneFlow.Go(SceneFlow.ModeSelect);
            });

            var modeRow = Node("ModeRow");
            if (modeRow != null) modeRow.gameObject.SetActive(false);

            BuildSpectateButton();
            WireSeats();

            MatchRpc.OnMapChanged += HandleMapSynced;
            MatchRpc.OnDifficultyChanged += HandleDifficultySynced;
            MatchRpc.OnLobbyPicksSynced += HandleLobbyPicksSynced;

            Refresh();
        }

        private void OnPrimaryPressed()
        {
            var net = NetSession.Instance;
            if (net == null || !net.IsNetworked)
            {
                SceneFlow.StartMatch();
                return;
            }

            _localReady = !_localReady;
            SetText("StatusLabel", _localReady ? "Ready! Waiting for other players..." : "");

            int localPeerId = net.LocalSlot >= 0 ? net.LocalSlot : 0;
            MatchRpc.Instance?.DeclareReadyServerRpc(localPeerId);

            if (NetAuthority.IsHost)
            {
                var readyGate = FindFirstObjectByType<ReadyGate>();
                if (readyGate != null)
                {
                    readyGate.DeclareReady(localPeerId);
                }
                else
                {
                    SceneFlow.StartMatch();
                }
            }
        }

        private void OnMapCycle(int delta)
        {
            if (!NetAuthority.IsHost && SceneFlow.Networked) return;

            Cycle(ref _map, SceneFlow.Maps.Length, delta);
            if (NetAuthority.IsHost && SceneFlow.Networked)
            {
                MatchRpc.Instance?.SelectMapServerRpc(_map);
            }
        }

        private void OnDifficultyCycle(int delta)
        {
            if (!NetAuthority.IsHost && SceneFlow.Networked) return;

            Cycle(ref _difficulty, Difficulties.Length, delta);
            if (NetAuthority.IsHost && SceneFlow.Networked)
            {
                MatchRpc.Instance?.SelectDifficultyServerRpc(_difficulty);
            }
        }

        private void HandleMapSynced(int mapIndex)
        {
            _map = Mathf.Clamp(mapIndex, 0, SceneFlow.Maps.Length - 1);
            Refresh();
        }

        private void HandleDifficultySynced(int difficulty)
        {
            _difficulty = Mathf.Clamp(difficulty, 0, Difficulties.Length - 1);
            Refresh();
        }

        private void HandleLobbyPicksSynced(int[] table)
        {
            if (table == null) return;
            for (int i = 0; i < Mathf.Min(table.Length, _replicatedPicks.Length); i++)
            {
                _replicatedPicks[i] = table[i];
            }
            RefreshSeats();
        }

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
            var net = NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;

            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                bool mine = !GameLaunch.Spectator && (isNetworked ? (net.LocalSlot == seat) : (seat == GameLaunch.SoloSeat));

                string characterName = "";
                int charPickIndex = seat * 4 + 1;
                if (charPickIndex < _replicatedPicks.Length && _replicatedPicks[charPickIndex] >= 0)
                {
                    characterName = Roster.At(Roster.People, _replicatedPicks[charPickIndex])?.Name ?? "";
                }

                string seatText;
                if (mine)
                {
                    seatText = $"{SeatName(seat)}   {YouMark}";
                }
                else if (isNetworked)
                {
                    bool isOccupied = net.Lobby.IsSeatOccupied(seat);
                    seatText = isOccupied
                        ? $"{SeatName(seat)}   · PLAYER {(string.IsNullOrEmpty(characterName) ? "" : $"({characterName})")}"
                        : $"{SeatName(seat)}   · BOT";
                }
                else
                {
                    seatText = $"{SeatName(seat)}   · BOT";
                }

                SetText($"SeatButton{seat}", seatText);

                var node = Node($"SeatButton{seat}");
                var button = node == null ? null : node.GetComponent<Button>();

                if (button != null) button.interactable = !GameLaunch.Spectator && (!isNetworked || NetAuthority.IsHost);
            }

            RefreshSpectate();
        }

        private void BuildSpectateButton()
        {
            var heading = Node("SeatHeading");
            if (heading == null) return;

            _spectate = MenuKit.WoodButton(heading.parent, "SPECTATE", Vector2.zero, Vector2.zero,
                                           new Vector2(176.0f, 46.0f), ToggleSpectate);

            _spectate.name = "SpectateButton";

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

            MenuSfx.Click();
            Refresh();
        }

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

        private void OnCharacterChosen()
        {
            var s = Settings.SettingsStore.Current;
            var net = NetSession.Instance;
            if (net != null && net.IsNetworked)
            {
                int localPeerId = net.LocalSlot >= 0 ? net.LocalSlot : 0;
                MatchRpc.Instance?.SelectLobbyPickServerRpc(localPeerId, s.CharacterPick, s.CanPick, s.SlipperPick);
            }
            Refresh();
        }

        private void Refresh()
        {
            SceneFlow.SelectedMap = SceneFlow.Maps[Mathf.Clamp(_map, 0, SceneFlow.Maps.Length - 1)];

            SetText("MapValueLabel",
                    SceneFlow.SelectedMap.ToUpperInvariant().Replace("BAYANPLAZA", "BAYAN PLAZA"));

            SetText("DifficultyValueLabel", Difficulties[_difficulty]);
            SetText("DetailLabel", SceneFlow.PreviewFor(SceneFlow.SelectedMap).Detail);

            if (_preview != null)
            {
                _preview.Show(SceneFlow.SelectedMap);
                _preview.ReapplyEnvironment();
            }

            var s = Settings.SettingsStore.Current;
            s.AiDifficulty = _difficulty;
            AIController.ApplyDifficulty(_difficulty);

            string person = Roster.At(Roster.People, s.CharacterPick)?.Name ?? "BERTO";
            string can = Roster.At(Roster.Cans, s.CanPick)?.Name ?? "PASIP";
            string slipper = Roster.At(Roster.Slippers, s.SlipperPick)?.Name ?? "TSINELAS";

            SetText("CharacterButton", $"{person} · {can} · {slipper}");

            RefreshSeats();
        }

        private void OnDestroy()
        {
            MatchRpc.OnMapChanged -= HandleMapSynced;
            MatchRpc.OnDifficultyChanged -= HandleDifficultySynced;
            MatchRpc.OnLobbyPicksSynced -= HandleLobbyPicksSynced;
        }
    }
}
