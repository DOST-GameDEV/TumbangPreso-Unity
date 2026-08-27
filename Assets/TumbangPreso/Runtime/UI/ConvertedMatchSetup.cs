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
        private int _readyCount;
        private int _readyExpected;

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

        /// <summary>
        /// ⚠️⚠️ NONE IS LAST, AND IT IS AN ABSENCE RATHER THAN A TIER. 🧑, 2026-08-26: *"add
        /// None as an option there and make it so that theres actually no bots ... just you
        /// there no bots"*. The index is `AIController.NoBotsIndex`; its note explains why the
        /// entry could not go at the front of this array.
        ///
        /// ⚠️ AND IT IS OFFLINE ONLY. See <see cref="DifficultyOptionCount"/>: three empty seats
        /// in a networked lobby is a different feature with its own rules about who may join
        /// them, and nobody has asked for it.
        /// </summary>
        private static readonly string[] Difficulties = { "EASY", "NORMAL", "HARD", "NONE" };

        private static readonly string[] DifficultyDetails =
        {
            "EASY Slower reactions and looser angles. Good for learning the throw arc.",
            "NORMAL The default, and the tier every balance number in this project was measured at. Reads your bearing, leads the lata, and blocks about 38% of what you throw.",
            "HARD Snappier reads and tighter defense. Will punish greedy slipper retrievals.",
            "NONE An empty street. Nobody else spawns, so the lata, the tsinelas and the whole arena are yours to practise the throw and the retrieval run in."
        };

        /// <summary>
        /// How many entries of <see cref="Difficulties"/> this lobby may cycle through.
        ///
        /// ⚠️ A NETWORKED LOBBY STOPS AT HARD. NONE removes three seats from the match, and a
        /// seat is what a peer joins: replicating "there are no seats" to a lobby somebody is
        /// sitting in has no defined answer. Offline practice is the whole of what was asked
        /// for, so that is the whole of what ships.
        /// </summary>
        private static int DifficultyOptionCount
            => SceneFlow.Networked ? Difficulties.Length - 1 : Difficulties.Length;

        protected override void Wire()
        {
            for (int i = 0; i < _replicatedPicks.Length; i++) _replicatedPicks[i] = -1;

            var net = NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;

            if (net != null)
            {
                net.Lobby.JoinCodeChanged += HandleJoinCodeChanged;

                // ⚠⚠ THE SCREEN HAD NO WAY OF HEARING THAT IT HAD BEEN SEATED, OR THAT ANYBODY
                // ELSE HAD. It drew the four rows once from `Start` and then redrew them only when
                // a pick table happened to arrive, so the local "YOU" marker sat on P1 until
                // something unrelated moved and a peer joining an empty chair changed nothing on
                // screen. 🧑, 2026-08-27: "it also does not reflect when a person joins the
                // lobby." Three separate facts move the seat rows and all three now say so.
                net.SeatingChanged += HandleSeatingChanged;
            }

            _map = Mathf.Max(0, Array.IndexOf(SceneFlow.Maps, SceneFlow.SelectedMap));
            _difficulty = Mathf.Clamp(Settings.SettingsStore.Current.AiDifficulty, 0, DifficultyOptionCount - 1);

            var previewNode = Node("MapPreview");
            if (previewNode != null) _preview = previewNode.GetComponent<MapPreviewSurface>();

            _characterPanel = Node("CharacterSelectPanel");

            OnClick("MapPrevButton", () => OnMapCycle(-1));
            OnClick("MapNextButton", () => OnMapCycle(1));

            OnClick("ModePrevButton", () => OnModeCycle(-1));
            OnClick("ModeNextButton", () => OnModeCycle(1));

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
            if (modeRow != null) modeRow.gameObject.SetActive(true);

            BuildRightPanelNetwork();
            WireSeats();

            MatchRpc.OnMapChanged += HandleMapSynced;
            MatchRpc.OnDifficultyChanged += HandleDifficultySynced;
            MatchRpc.OnLobbyPicksSynced += HandleLobbyPicksSynced;
            MatchRpc.OnLobbyRosterSynced += HandleLobbyRosterSynced;
            MatchRpc.OnLobbyReadyChanged += HandleLobbyReadyChanged;
            MatchRpc.OnModeChanged += HandleModeSynced;
            MatchRpc.OnMatchStarted += HandleMatchStarted;

            var s = Settings.SettingsStore.Current;
            var modePeople = Roster.GetPeople(SceneFlow.SelectedMode);
            if (s.CharacterPick < 0 || s.CharacterPick >= modePeople.Count) s.CharacterPick = 0;
            if (s.CanPick < 0 || s.CanPick >= Roster.Cans.Count) s.CanPick = 0;
            if (s.SlipperPick < 0 || s.SlipperPick >= Roster.Slippers.Count) s.SlipperPick = 0;
            Settings.SettingsStore.Save();

            if (isNetworked)
            {
                MatchRpc.Instance?.SelectLobbyPickServerRpc(s.CharacterPick, s.CanPick, s.SlipperPick);
            }

            if (isNetworked && NetAuthority.IsHost)
            {
                SetStatus("You are now the lobby leader - you pick the map, the mode, and when to start.");
            }

            Refresh();
        }

        private void BuildRightPanelNetwork()
        {
            var heading = Node("SeatHeading");
            if (heading == null || heading.parent == null) return;

            Transform rows = heading.parent;
            int headingIndex = heading.GetSiblingIndex();

            // 1. HeaderRow (holds SeatHeading on left, SpectateButton on right)
            var headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(rows, false);
            headerRow.transform.SetSiblingIndex(headingIndex);

            var hLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 16;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            var hElement = headerRow.AddComponent<LayoutElement>();
            hElement.minHeight = 46;
            hElement.preferredHeight = 46;
            hElement.flexibleWidth = 1;

            heading.SetParent(headerRow.transform, false);
            var headElement = heading.GetComponent<LayoutElement>();
            if (headElement == null) headElement = heading.gameObject.AddComponent<LayoutElement>();
            headElement.flexibleWidth = 1;
            headElement.minHeight = 46;

            var headText = heading.GetComponent<Text>();
            if (headText != null)
            {
                headText.horizontalOverflow = HorizontalWrapMode.Overflow;
                headText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            _spectate = MenuKit.WoodButton(headerRow.transform, "SPECTATE", Vector2.zero, Vector2.zero,
                                           new Vector2(140.0f, 40.0f), ToggleSpectate);
            _spectate.name = "SpectateButton";
            var specElement = _spectate.gameObject.AddComponent<LayoutElement>();
            specElement.preferredWidth = 140.0f;
            specElement.preferredHeight = 40.0f;

            var label = _spectate.GetComponentInChildren<Text>();
            if (label != null) label.fontSize = 18;

            int insertIndex = headerRow.transform.GetSiblingIndex() + 1;

            // 2. Address Row (placed directly in rows container below headerRow)
            _addressRow = new GameObject("AddressRow");
            _addressRow.transform.SetParent(rows, false);
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
            addrBoxImg.sprite = GodotTheme.WoodBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            addrBoxImg.type = Image.Type.Sliced;
            addrBoxImg.color = Color.white;
            var addrBoxElement = addrBox.AddComponent<LayoutElement>();
            addrBoxElement.flexibleWidth = 1;
            addrBoxElement.minHeight = 44;

            _addressText = MenuKit.Label(addrBox.transform, "", 20, UiTheme.Cream,
                                         Vector2.zero, Vector2.zero, Vector2.zero,
                                         TextAnchor.MiddleLeft);
            _addressText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _addressText.verticalOverflow = VerticalWrapMode.Overflow;
            _addressText.rectTransform.anchorMin = Vector2.zero;
            _addressText.rectTransform.anchorMax = Vector2.one;
            _addressText.rectTransform.offsetMin = new Vector2(16, 0);
            _addressText.rectTransform.offsetMax = new Vector2(-16, 0);

            _addressCopyBtn = MenuKit.WoodButton(_addressRow.transform, "COPY", Vector2.zero, Vector2.zero,
                                                 new Vector2(96, 40), OnAddressCopyPressed);
            var addrCopyElement = _addressCopyBtn.gameObject.AddComponent<LayoutElement>();
            addrCopyElement.preferredWidth = 96;
            addrCopyElement.preferredHeight = 40;
            _addressCopyBtnText = _addressCopyBtn.GetComponentInChildren<Text>();

            // 3. Code Row (placed directly in rows container below addressRow)
            _codeRow = new GameObject("CodeRow");
            _codeRow.transform.SetParent(rows, false);
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
            codeCaptionElement.preferredWidth = 64;
            codeCaptionElement.minHeight = 44;
            var captionText = MenuKit.Label(codeCaption.transform, "CODE", 20, UiTheme.Amber,
                                            Vector2.zero, Vector2.zero, Vector2.zero,
                                            TextAnchor.MiddleCenter);
            captionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            MenuKit.Stretch(captionText.rectTransform, 0);

            // Code display box
            var codeBox = new GameObject("CodeBox");
            codeBox.transform.SetParent(_codeRow.transform, false);
            var codeBoxImg = codeBox.AddComponent<Image>();
            codeBoxImg.sprite = GodotTheme.WoodBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            codeBoxImg.type = Image.Type.Sliced;
            codeBoxImg.color = Color.white;
            var codeBoxElement = codeBox.AddComponent<LayoutElement>();
            codeBoxElement.flexibleWidth = 1;
            codeBoxElement.minHeight = 44;

            _codeText = MenuKit.Label(codeBox.transform, "", 20, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            _codeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _codeText.verticalOverflow = VerticalWrapMode.Overflow;
            _codeText.rectTransform.anchorMin = Vector2.zero;
            _codeText.rectTransform.anchorMax = Vector2.one;
            _codeText.rectTransform.offsetMin = new Vector2(16, 0);
            _codeText.rectTransform.offsetMax = new Vector2(-16, 0);

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

            // ⚠️ THE PRESS CARRIES ITS STATE, because the button is a toggle and the message was
            // not: un-readying sent a second "I am ready", which the host's set swallowed as a
            // duplicate, so the tick could be turned off on this screen and nowhere else.
            //
            // ⚠️⚠️ AND THE SCREEN DOES NOT CLAIM READY UNTIL THE HOST HAS ACTUALLY BEEN TOLD.
            // `NetAuthority.IsNetworked` is true from `StartClient` onward rather than from
            // connection approval, so a press made during the join window was written to a
            // transport with nowhere to send it. This line flipped anyway and the label read
            // "Ready! Waiting for other players..." to somebody the host was itself waiting for.
            // `ReadyGate.Update` holds and resends the in-match press for the same reason.
            bool wanted = !_localReady;
            bool delivered = MatchRpc.Instance != null &&
                             MatchRpc.Instance.DeclareReadyServerRpc(wanted);

            if (!delivered)
            {
                SetStatus("Still connecting. Press again in a moment.");
                Refresh();
                return;
            }

            _localReady = wanted;

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
                    MatchRpc.Instance?.DeclareReadyServerRpc();
                }
                else
                {
                    // ⚠️ NO SECOND `SceneFlow.StartMatch()` HERE. `HostStartMatch` fires
                    // `OnMatchStarted`, which this screen answers with the load. Calling it again
                    // on the next line queued a second load of the same arena in the same frame.
                    MatchRpc.Instance?.HostStartMatch();
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

        private void OnModeCycle(int delta)
        {
            if (!NetAuthority.IsHost && SceneFlow.Networked) return;

            SceneFlow.SelectedMode = SceneFlow.SelectedMode == GameMode.HeroStrike
                ? GameMode.Classic
                : GameMode.HeroStrike;

            var s = Settings.SettingsStore.Current;
            var list = Roster.GetPeople(SceneFlow.SelectedMode);
            if (s.CharacterPick >= list.Count || s.CharacterPick < 0)
            {
                s.CharacterPick = 0;
                Settings.SettingsStore.Save();
            }

            var net = NetSession.Instance;
            if (net != null && net.IsNetworked)
            {
                // ⚠️⚠️ THE MODE IS PUSHED TO THE LOBBY, AND UNTIL 2026-08-27 IT WAS NOT. The map
                // cycle three methods above sends `SelectMapServerRpc` and the difficulty cycle
                // below sends `SelectDifficultyServerRpc`; this one changed a static on the
                // host's own machine and told nobody. Every joined peer then sat in a lobby
                // reading the wrong mode, picked from the wrong roster, and built the wrong game
                // when the match started. `MatchRpc`'s § THE GAME MODE note has what that cost.
                //
                // ⚠️ IT GOES BEFORE THE PICK, because changing mode re-bases `CharacterPick`
                // against a different roster (the clamp directly above), so the pick being sent
                // is only meaningful once the far end knows which list it indexes.
                MatchRpc.Instance?.SelectModeServerRpc((int)SceneFlow.SelectedMode);
                MatchRpc.Instance?.SelectLobbyPickServerRpc(s.CharacterPick, s.CanPick, s.SlipperPick);
            }

            Refresh();
        }

        private void OnDifficultyCycle(int delta)
        {
            if (!NetAuthority.IsHost && SceneFlow.Networked) return;

            Cycle(ref _difficulty, DifficultyOptionCount, delta);
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
            _difficulty = Mathf.Clamp(difficulty, 0, DifficultyOptionCount - 1);
            Refresh();
        }

        private void HandleSeatingChanged() => Refresh();

        private void HandleLobbyRosterSynced(LobbySeatInfo[] seats) => RefreshSeats();

        private void HandleModeSynced(int mode) => Refresh();

        private void HandleLobbyReadyChanged(int ready, int expected)
        {
            _readyCount = ready;
            _readyExpected = expected;

            // ⚠️ THE LOCAL TICK FOLLOWS THE HOST'S TALLY RATHER THAN A LOCAL BOOL. The button
            // used to toggle a field this screen owned, so a press the host refused (a spectator,
            // a peer with no seat) still drew as READY on the one screen that mattered.
            var net = NetSession.Instance;
            RefreshReadyLabel(net != null && net.IsNetworked);
            RefreshSeats();
        }

        private void RefreshReadyLabel(bool isNetworked)
        {
            if (!isNetworked) return;

            var primNode = Node("PrimaryButton");
            if (primNode == null) return;

            string label = GameLaunch.Spectator
                ? "SPECTATING"
                : _localReady ? "WAITING" : "READY";

            if (_readyExpected > 1) label += $"   {_readyCount}/{_readyExpected}";
            SetText("PrimaryButton", label);
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
                    MenuSfx.Click();

                    var session = NetSession.Instance;
                    if (session != null && session.IsNetworked)
                    {
                        // ⚠⚠ IT ASKS THE HOST. `GameLaunch.SoloSeat` is read by the OFFLINE
                        // practice match and by nothing else, so writing it here was the whole of
                        // what pressing a seat in a networked lobby did. See the CHOOSING A CHAIR
                        // section of `MatchRpc` for what the request does instead.
                        MatchRpc.Instance?.RequestSeatServerRpc(seat);
                        return;
                    }

                    GameLaunch.SoloSeat = seat;
                    GameLaunch.Spectator = false;
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

                var seatInfo = isNetworked ? MatchRpc.Instance?.GetSeatInfo(seat) : null;
                string characterName = "";
                if (seatInfo != null && seatInfo.CharacterPick >= 0)
                {
                    var modePeople = Roster.GetPeople(SceneFlow.SelectedMode);
                    characterName = Roster.At(modePeople, seatInfo.CharacterPick)?.Name ?? "";
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
                    bool isOccupied = seatInfo != null && seatInfo.Occupied;
                    if (isOccupied)
                    {
                        string displayName = !string.IsNullOrEmpty(seatInfo.Name) ? seatInfo.Name : $"PLAYER {seat + 1}";
                        seatText = $"{SeatName(seat)}   · {displayName} {(string.IsNullOrEmpty(characterName) ? "" : $"({characterName})")}";
                    }
                    else
                    {
                        seatText = $"{SeatName(seat)}   · BOT";
                    }
                }
                else
                {
                    seatText = $"{SeatName(seat)}   · BOT";
                }

                SetText($"SeatButton{seat}", seatText);

                var node = Node($"SeatButton{seat}");
                var button = node == null ? null : node.GetComponent<Button>();

                // ⚠⚠ EVERY PEER MAY PRESS AN EMPTY CHAIR, NOT ONLY THE HOST. The old rule was
                // `!isNetworked || NetAuthority.IsHost`, which left all four rows dead on every
                // client. Seating is host-authoritative because the request is, not because the
                // button is: `LobbySession.TryTakeSeat` is what refuses a taken chair, a held one
                // or a move made after the match has started.
                //
                // ⚠️ A SPECTATOR MAY PRESS ONE TOO, because pressing a free seat is how you
                // stop spectating. What nobody may press is a chair somebody else is in, or their
                // own chair, which would be a request that changes nothing.
                if (button != null)
                {
                    bool occupiedByOther = isNetworked
                        ? (seatInfo != null && seatInfo.Occupied && !mine)
                        : false;

                    button.interactable = isNetworked
                        ? (!mine && !occupiedByOther && !(net != null && net.Lobby.MatchInProgress))
                        : !GameLaunch.Spectator;
                }
            }

            RefreshSpectate();
        }

        private void ToggleSpectate()
        {
            var net = NetSession.Instance;
            if (net != null && net.IsNetworked)
            {
                // ⚠️ SPECTATING IS A SEAT REQUEST FOR "NO SEAT". It used to flip a local
                // static, so the host went on counting the spectator towards the ready gate and
                // went on building them a body, and a spectator wanting to play again had no way
                // back into a chair.
                if (GameLaunch.Spectator)
                {
                    int free = net.Lobby.FirstFreeSeat();
                    if (free >= 0) MatchRpc.Instance?.RequestSeatServerRpc(free);
                }
                else
                {
                    MatchRpc.Instance?.RequestSeatServerRpc(-1);
                }
                return;
            }

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
                MatchRpc.Instance?.SelectLobbyPickServerRpc(s.CharacterPick, s.CanPick, s.SlipperPick);
            }
            Refresh();
        }

        private void Refresh()
        {
            var net = NetSession.Instance;
            bool isNetworked = net != null && net.IsNetworked;

            SceneFlow.SelectedMap = SceneFlow.Maps[Mathf.Clamp(_map, 0, SceneFlow.Maps.Length - 1)];

            // ⚠️ THE NAME AND TAGLINE COME FROM THE MAP REGISTRY, NOT FROM STRING SURGERY ON THE
            // SCENE ID. The old uppercase-and-patch-BAYANPLAZA line rendered the third map as
            // "ILALIMNGTULAY", and every map added after it would have needed another patch.
            var mapEntry = SceneFlow.PreviewFor(SceneFlow.SelectedMap);
            string mapName = mapEntry.Name;
            string tagline = mapEntry.Tagline;

            // ⚠️⚠️ "PRACTICE MODE", AND IT HAS BEEN RENAMED ONCE ALREADY. This screen sets up a
            // solo match against bots, with a BOTS difficulty row in the middle of it, and it was
            // renamed away from "SINGLE PLAYER" for that reason. The 2026-08-25 merge that
            // brought the network and UGS work across resolved this line back to the old string,
            // and 🧑 spotted it in the build: *"this shit still says single player"*.
            //
            // ⚠️ NOT TO BE CONFUSED WITH `HeroKit.PracticeMode`, which is an unrelated internal
            // flag for the between-round buffer where an ultimate is free. Same two words, two
            // different things, and neither is wrong: do not merge them.
            SetHeadline("BannerLabel", isNetworked ? "LOBBY" : "PRACTICE MODE", 66);
            SetText("MapValueLabel", mapName);

            SetText("ModeValueLabel", SceneFlow.SelectedMode == GameMode.HeroStrike ? "HERO STRIKE" : "CLASSIC");
            SetText("DifficultyValueLabel", Difficulties[_difficulty]);
            SetText("DetailLabel", $"{mapName}   {tagline}");

            if (_preview != null)
            {
                _preview.Show(SceneFlow.SelectedMap);
                _preview.ReapplyEnvironment();
            }

            var s = Settings.SettingsStore.Current;
            s.AiDifficulty = _difficulty;
            AIController.ApplyDifficulty(_difficulty);

            var modePeople = Roster.GetPeople(SceneFlow.SelectedMode);
            string person = Roster.At(modePeople, s.CharacterPick)?.Name ?? (SceneFlow.SelectedMode == GameMode.HeroStrike ? "DANTE" : "BAYAN");
            string can = Roster.At(Roster.Cans, s.CanPick)?.Name ?? "PASIP";
            string slipper = Roster.At(Roster.Slippers, s.SlipperPick)?.Name ?? "TSINELAS";

            SetText("CharacterButton", $"{person} · {can} · {slipper}  ▸");

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
                    RefreshReadyLabel(true);
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
                net.SeatingChanged -= HandleSeatingChanged;
            }

            MatchRpc.OnMapChanged -= HandleMapSynced;
            MatchRpc.OnDifficultyChanged -= HandleDifficultySynced;
            MatchRpc.OnLobbyPicksSynced -= HandleLobbyPicksSynced;
            MatchRpc.OnLobbyRosterSynced -= HandleLobbyRosterSynced;
            MatchRpc.OnLobbyReadyChanged -= HandleLobbyReadyChanged;
            MatchRpc.OnModeChanged -= HandleModeSynced;
            MatchRpc.OnMatchStarted -= HandleMatchStarted;
        }
    }
}

