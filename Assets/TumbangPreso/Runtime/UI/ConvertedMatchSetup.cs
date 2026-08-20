using System;
using System.Collections;
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
    /// There are four selectors: Map, Mode, difficulty, and fighter.
    /// Mode row is hidden by default to match Godot match_setup.gd.
    /// In multiplayer, the screen acts as the Lobby, showing join code, host address,
    /// and ready status controls.
    /// </summary>
    public sealed class ConvertedMatchSetup : ConvertedScreen
    {
        private int _map;
        private int _difficulty = 1;

        protected override string CancelTarget => SceneFlow.ModeSelect;

        private const float CycleGuard = 0.12f;
        private float _lastCycle = -1.0f;

        private MapPreviewSurface _preview;
        private Transform _characterPanel;
        private Button _spectate;
        private bool _localReady;

        private GameObject _addressRow;
        private Text _addressText;
        private Button _addressCopyBtn;
        private Text _addressCopyBtnText;

        private GameObject _codeRow;
        private Text _codeText;
        private Button _codeCopyBtn;
        private Text _codeCopyBtnText;

        private readonly int[] _replicatedPicks = new int[Balance.PlayerCount * 4];

        private const string YouMark = "◀ YOU";

        private static readonly string[] Difficulties = { "EASY", "NORMAL", "HARD" };

        private static readonly string[] DifficultyDetails =
        {
            "EASY Slower reactions and looser angles. Good for learning the throw arc.",
            "NORMAL The default, and the tier every balance number in this project was measured at. Reads your bearing, leads the lata, and blocks about 38% of what you throw.",
            "HARD Snappier reads and tighter defense. Will punish greedy slipper retrievals."
        };

        protected override void Wire()
        {
            for (int i = 0; i < _replicatedPicks.Length; i++) _replicatedPicks[i] = -1;

            var net = NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;

            if (net != null)
            {
                net.Lobby.JoinCodeChanged += HandleJoinCodeChanged;
            }

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
            OnClick("StartButton", OnStartPressed);
            OnClick("BackButton", () =>
            {
                if (net != null && net.IsNetworked) net.Stop();
                SceneFlow.Go(SceneFlow.ModeSelect);
            });

            var modeRow = Node("ModeRow");
            if (modeRow != null) modeRow.gameObject.SetActive(false);

            BuildSpectateButton();
            BuildNetworkRows();
            WireSeats();

            MatchRpc.OnMapChanged += HandleMapSynced;
            MatchRpc.OnDifficultyChanged += HandleDifficultySynced;
            MatchRpc.OnLobbyPicksSynced += HandleLobbyPicksSynced;
            MatchRpc.OnMatchStarted += HandleMatchStarted;

            Refresh();
        }

        private void BuildNetworkRows()
        {
            var heading = Node("SeatHeading");
            if (heading == null || heading.parent == null) return;

            Transform rowsContainer = heading.parent;
            int insertIndex = heading.GetSiblingIndex() + 1;

            // 1. Address Row
            _addressRow = new GameObject("AddressRow");
            _addressRow.transform.SetParent(rowsContainer, false);
            _addressRow.transform.SetSiblingIndex(insertIndex++);

            var addrLayout = _addressRow.AddComponent<HorizontalLayoutGroup>();
            addrLayout.spacing = 10;
            addrLayout.childControlWidth = true;
            addrLayout.childControlHeight = true;
            addrLayout.childForceExpandWidth = false;
            addrLayout.childForceExpandHeight = true;

            var addrElement = _addressRow.AddComponent<LayoutElement>();
            addrElement.minHeight = 44;
            addrElement.preferredHeight = 44;
            addrElement.flexibleWidth = 1;

            // Address display box
            var addrBox = new GameObject("AddressBox");
            addrBox.transform.SetParent(_addressRow.transform, false);
            var addrBoxImg = addrBox.AddComponent<Image>();
            addrBoxImg.color = UiTheme.WoodDeep;
            var addrBoxPanel = addrBox.AddComponent<GodotPanel>();
            addrBoxPanel.Variation = "WoodDeep";
            var addrBoxElement = addrBox.AddComponent<LayoutElement>();
            addrBoxElement.flexibleWidth = 1;
            addrBoxElement.minHeight = 44;

            _addressText = MenuKit.Label(addrBox.transform, "", 22, UiTheme.Cream,
                                         new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                                         TextAnchor.MiddleCenter);
            MenuKit.Stretch(_addressText.rectTransform, 8);

            _addressCopyBtn = MenuKit.WoodButton(_addressRow.transform, "COPY", Vector2.zero, Vector2.zero,
                                                 new Vector2(96, 40), OnAddressCopyPressed);
            var addrCopyElement = _addressCopyBtn.gameObject.AddComponent<LayoutElement>();
            addrCopyElement.preferredWidth = 96;
            addrCopyElement.preferredHeight = 40;
            _addressCopyBtnText = _addressCopyBtn.GetComponentInChildren<Text>();

            // 2. Code Row
            _codeRow = new GameObject("CodeRow");
            _codeRow.transform.SetParent(rowsContainer, false);
            _codeRow.transform.SetSiblingIndex(insertIndex++);

            var codeLayout = _codeRow.AddComponent<HorizontalLayoutGroup>();
            codeLayout.spacing = 10;
            codeLayout.childControlWidth = true;
            codeLayout.childControlHeight = true;
            codeLayout.childForceExpandWidth = false;
            codeLayout.childForceExpandHeight = true;

            var codeElement = _codeRow.AddComponent<LayoutElement>();
            codeElement.minHeight = 44;
            codeElement.preferredHeight = 44;
            codeElement.flexibleWidth = 1;

            // Code caption
            var codeCaption = new GameObject("CodeCaption");
            codeCaption.transform.SetParent(_codeRow.transform, false);
            var codeCaptionElement = codeCaption.AddComponent<LayoutElement>();
            codeCaptionElement.preferredWidth = 72;
            codeCaptionElement.minHeight = 44;
            var captionText = MenuKit.Label(codeCaption.transform, "CODE", 26, UiTheme.Amber,
                                            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                                            TextAnchor.MiddleCenter);
            MenuKit.Stretch(captionText.rectTransform, 0);

            // Code display box
            var codeBox = new GameObject("CodeBox");
            codeBox.transform.SetParent(_codeRow.transform, false);
            var codeBoxImg = codeBox.AddComponent<Image>();
            codeBoxImg.color = UiTheme.WoodDeep;
            var codeBoxPanel = codeBox.AddComponent<GodotPanel>();
            codeBoxPanel.Variation = "WoodDeep";
            var codeBoxElement = codeBox.AddComponent<LayoutElement>();
            codeBoxElement.flexibleWidth = 1;
            codeBoxElement.minHeight = 44;

            _codeText = MenuKit.Label(codeBox.transform, "", 26, UiTheme.Cream,
                                      new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleCenter);
            MenuKit.Stretch(_codeText.rectTransform, 8);

            _codeCopyBtn = MenuKit.WoodButton(_codeRow.transform, "COPY", Vector2.zero, Vector2.zero,
                                              new Vector2(96, 40), OnCodeCopyPressed);
            var codeCopyElement = _codeCopyBtn.gameObject.AddComponent<LayoutElement>();
            codeCopyElement.preferredWidth = 96;
            codeCopyElement.preferredHeight = 40;
            _codeCopyBtnText = _codeCopyBtn.GetComponentInChildren<Text>();
        }

        private void OnAddressCopyPressed()
        {
            if (_addressText == null || string.IsNullOrEmpty(_addressText.text)) return;
            GUIUtility.systemCopyBuffer = _addressText.text;
            MenuSfx.Click();
            StartCoroutine(FlashButtonText(_addressCopyBtnText, "COPIED", "COPY"));
        }

        private void OnCodeCopyPressed()
        {
            if (_codeText == null || string.IsNullOrEmpty(_codeText.text)) return;
            GUIUtility.systemCopyBuffer = _codeText.text;
            MenuSfx.Click();
            StartCoroutine(FlashButtonText(_codeCopyBtnText, "COPIED", "COPY"));
            SetStatus("Join code copied. Send it to whoever you want in the game.");
        }

        private IEnumerator FlashButtonText(Text targetText, string flashMessage, string originalText)
        {
            if (targetText == null) yield break;
            targetText.text = flashMessage;
            yield return new WaitForSecondsRealtime(1.2f);
            if (targetText != null) targetText.text = originalText;
        }

        private void SetStatus(string message)
        {
            var label = Node("StatusLabel");
            if (label != null)
            {
                var text = label.GetComponent<Text>();
                if (text != null)
                {
                    text.color = UiTheme.Impact;
                    text.text = message;
                }
            }
        }

        private void HandleMatchStarted()
        {
            SceneFlow.StartMatch();
        }

        private void OnPrimaryPressed()
        {
            var net = NetSession.Instance;
            if (net == null || !net.IsNetworked)
            {
                SceneFlow.StartMatch();
                return;
            }

            if (GameLaunch.Spectator) return;

            _localReady = !_localReady;
            int localPeerId = net.LocalSlot >= 0 ? net.LocalSlot : 0;
            MatchRpc.Instance?.DeclareReadyServerRpc(localPeerId);

            if (_localReady)
            {
                SetStatus("Ready! Waiting for other players...");
            }
            else
            {
                SetStatus(NetAuthority.IsHost
                    ? "You are now the lobby leader - you pick the map, the mode, and when to start."
                    : "");
            }

            Refresh();
        }

        private void OnStartPressed()
        {
            var net = NetSession.Instance;
            if (net != null && net.IsNetworked && NetAuthority.IsHost)
            {
                var readyGate = FindFirstObjectByType<ReadyGate>();
                if (readyGate != null)
                {
                    int localPeerId = net.LocalSlot >= 0 ? net.LocalSlot : 0;
                    readyGate.DeclareReady(localPeerId);
                }
                else
                {
                    MatchRpc.Instance?.HostStartMatch();
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
                    seatText = isNetworked && _localReady
                        ? $"{SeatName(seat)}   {YouMark}   ···"
                        : $"{SeatName(seat)}   {YouMark}";
                }
                else if (isNetworked)
                {
                    bool isOccupied = (net != null && net.Lobby.IsSeatOccupied(seat)) ||
                                      (_replicatedPicks.Length > seat * 4 && _replicatedPicks[seat * 4] >= 0);
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
            Refresh();
        }

        private void RefreshSpectate()
        {
            if (_spectate == null) return;

            var skin = _spectate.GetComponent<GodotButton>();
            if (skin != null)
            {
                skin.Variation = GameLaunch.Spectator ? "WoodPrimaryButton" : "WoodButton";
                skin.Apply();
                skin.Refresh();
            }

            var label = _spectate.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = GameLaunch.Spectator ? "SPECTATING" : "SPECTATE";
            }
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
            var net = NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;

            SceneFlow.SelectedMap = SceneFlow.Maps[Mathf.Clamp(_map, 0, SceneFlow.Maps.Length - 1)];

            SetText("BannerLabel", isNetworked ? "LOBBY" : "SINGLE PLAYER");

            SetText("MapValueLabel",
                    SceneFlow.SelectedMap.ToUpperInvariant().Replace("BAYANPLAZA", "BAYAN PLAZA"));

            SetText("DifficultyValueLabel", Difficulties[_difficulty]);
            SetText("DetailLabel", DifficultyDetails[_difficulty]);

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

            // Heading & hints
            if (isNetworked)
            {
                SetText("SeatHeading", NetAuthority.IsHost ? "LOBBY  ·  YOU ARE HOSTING" : "LOBBY  ·  CONNECTED");
                SetText("SeatHint",
                        "You pick the map and the mode for everyone. Click a free seat to move. "
                        + "Empty seats are played by bots. Read the code above out to the others.");

                // Network rows
                if (_addressRow != null)
                {
                    _addressRow.SetActive(true);
                    string hostAddr = "127.0.0.1:8910";
                    if (net != null)
                    {
                        var ips = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
                        foreach (var ip in ips)
                        {
                            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))
                            {
                                hostAddr = $"{ip}:8910";
                                break;
                            }
                        }
                    }
                    if (_addressText != null) _addressText.text = hostAddr;
                }

                if (_codeRow != null)
                {
                    string code = net?.Lobby?.JoinCode ?? "";
                    _codeRow.SetActive(!string.IsNullOrEmpty(code));
                    if (_codeText != null) _codeText.text = code;
                }

                // Primary & Start button controls
                var primNode = Node("PrimaryButton");
                if (primNode != null)
                {
                    SetText("PrimaryButton", GameLaunch.Spectator ? "SPECTATING" : "READY");
                    var btn = primNode.GetComponent<Button>();
                    if (btn != null) btn.interactable = !GameLaunch.Spectator;
                }

                var startNode = Node("StartButton");
                if (startNode != null)
                {
                    startNode.gameObject.SetActive(NetAuthority.IsHost);
                }
            }
            else
            {
                SetText("SeatHeading", "YOUR CHARACTER");
                SetText("SeatHint",
                        "Four players, one taya. The taya rotates every round, so everyone defends "
                        + "exactly once. Empty seats are bots, the kids from the street who fill in.");

                if (_addressRow != null) _addressRow.SetActive(false);
                if (_codeRow != null) _codeRow.SetActive(false);

                var primNode = Node("PrimaryButton");
                if (primNode != null)
                {
                    SetText("PrimaryButton", "START MATCH");
                    var btn = primNode.GetComponent<Button>();
                    if (btn != null) btn.interactable = true;
                }

                var startNode = Node("StartButton");
                if (startNode != null) startNode.gameObject.SetActive(false);
            }

            RefreshSeats();
        }

        private void HandleJoinCodeChanged(string code)
        {
            if (_codeRow != null)
            {
                _codeRow.SetActive(!string.IsNullOrEmpty(code));
                if (_codeText != null) _codeText.text = code;
            }
        }

        private void OnDestroy()
        {
            var net = NetSession.Instance;
            if (net != null)
            {
                net.Lobby.JoinCodeChanged -= HandleJoinCodeChanged;
            }

            MatchRpc.OnMapChanged -= HandleMapSynced;
            MatchRpc.OnDifficultyChanged -= HandleDifficultySynced;
            MatchRpc.OnLobbyPicksSynced -= HandleLobbyPicksSynced;
            MatchRpc.OnMatchStarted -= HandleMatchStarted;
        }
    }
}

