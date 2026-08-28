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

        /// <summary>
        /// ⚠️ ESCAPE CLOSES THE JOIN CARD FIRST AND LEAVES THE LOBBY SECOND. `ConvertedScreen`
        /// documents why this is an ACTION rather than a scene name: half the screens back out by
        /// closing something in place, and a `CancelTarget` alone cannot say so. Without this,
        /// Escape over an open join card would stop the transport and drop the player to the mode
        /// picker, which is two steps for one press.
        ///
        /// ⚠️ AND IT STOPS THE SESSION ON THE WAY OUT, which the BACK button already did and
        /// Escape did not. A lobby left listening behind the player keeps its port, keeps
        /// beaconing on the LAN and still shows up in other people's browsers as a game they can
        /// join and nobody is in.
        /// </summary>
        protected override bool Cancel()
        {
            if (_joinPanel != null && _joinPanel.IsOpen)
            {
                _joinPanel.Close();
                return true;
            }

            var net = NetSession.Instance;
            if (net != null && net.IsNetworked) net.Stop();

            return base.Cancel();
        }

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

        /// <summary>The four ways into somebody else's game, on this screen now. See
        /// <see cref="LobbyJoinPanel"/> and `docs/TODO.md` § 68.11.</summary>
        private LobbyJoinPanel _joinPanel;

        private GameObject _lobbyEntryRow;
        private Button _joinButton;
        private Button _onlineButton;

        /// <summary>True while a LAN/online swap is in flight. See <see cref="ToggleOnline"/>.</summary>
        private bool _switchingHost;

        /// <summary>The four bodies standing in the arena behind this screen, and their floating
        /// names. Null on the practice screen, which has no cast. See <see cref="LobbyCast"/>.</summary>
        private LobbyCast _cast;
        private LobbyNameplates _nameplates;

        /// <summary>
        /// What each seat is wearing, rebuilt on every refresh and handed to the cast.
        ///
        /// ⚠️ ONE ARRAY, REUSED. `Refresh` runs on every arrow press, every seat message, every
        /// ready tally and every pick table; allocating a four-int array on each of those is the
        /// shape `docs/TODO.md` § 52.3 measured costing 952 bytes a frame on the HUD.
        /// </summary>
        private readonly int[] _castPicks = new int[Balance.PlayerCount];

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

        /// <summary>
        /// ⚠️⚠️ "THIS IS THE MULTIPLAYER LOBBY" AND "A TRANSPORT IS UP" ARE TWO DIFFERENT
        /// QUESTIONS, AND UNTIL 2026-08-28 THIS SCREEN ONLY HAD ONE OF THEM. Every branch here
        /// asked `NetSession.IsNetworked` and read a false as "practice mode", which was correct
        /// only because the player could not REACH this screen in multiplayer without having
        /// already hosted or joined on a previous one.
        ///
        /// MULTIPLAYER now lands here directly (`ConvertedModeSelect`), so there is a real window
        /// where the answer to the first question is yes and to the second is no: while the
        /// auto-host is starting, and afterwards for good if the port bind was refused. Asking
        /// `IsNetworked` in that window drew the multiplayer lobby as PRACTICE MODE, with a
        /// START MATCH button that would have launched a solo game against bots out from under
        /// somebody waiting for a friend to join.
        ///
        /// `IsLobby` is the SCREEN's identity and comes from `SceneFlow.Networked`. `IsLive` is
        /// the TRANSPORT's state. Anything the player reads (the headline, the hint, whether the
        /// join panel is open) hangs off the first; anything that touches the wire hangs off the
        /// second. See `docs/TODO.md` § 68.5 for the four states this produces.
        /// </summary>
        private static bool IsLobby => SceneFlow.Networked;

        private static bool IsLive
        {
            get
            {
                var net = NetSession.Instance;
                return net != null && net.IsNetworked;
            }
        }

        protected override void Wire()
        {
            for (int i = 0; i < _replicatedPicks.Length; i++) _replicatedPicks[i] = -1;

            // ⚠️ THE SESSION IS CREATED HERE WHEN THIS IS THE LOBBY, rather than inherited from
            // a screen that ran first. `NetSession.Instance` was guaranteed non-null only because
            // `ConvertedMultiplayerSetup.Wire` called `Ensure()` before navigating here; arriving
            // straight from the mode picker skips that, and every `if (net != null)` block below
            // would then be quietly skipped, leaving a lobby subscribed to nothing.
            var net = IsLobby ? NetSession.Ensure() : NetSession.Instance;
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

                // ⚠️⚠️ AND THE LOBBY LEAVES WHEN THE CONNECTION DOES. Without this a client whose
                // approval was refused sat here forever on a screen headed LOBBY · CONNECTED,
                // with the other three chairs drawn as bots because no roster ever arrived. See
                // `NetSession.ClientDisconnected`.
                NetSession.ClientDisconnected += HandleClientDisconnected;
            }

            _map = Mathf.Max(0, Array.IndexOf(SceneFlow.Maps, SceneFlow.SelectedMap));
            _difficulty = Mathf.Clamp(Settings.SettingsStore.Current.AiDifficulty, 0, DifficultyOptionCount - 1);

            var previewNode = Node("MapPreview");
            if (previewNode != null) _preview = previewNode.GetComponent<MapPreviewSurface>();

            BuildCast(previewNode);

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
                // ⚠️ BACK CLOSES THE JOIN CARD BEFORE IT LEAVES THE SCREEN. Same rule
                // `ConvertedMultiplayerSetup` already applied to its two browser boxes: a modal
                // over a screen has to be dismissable by the button the player's hand is already
                // on, or BACK reads as having skipped a step.
                if (_joinPanel != null && _joinPanel.IsOpen)
                {
                    _joinPanel.Close();
                    return;
                }

                if (net != null && net.IsNetworked) net.Stop();
                SceneFlow.Go(SceneFlow.ModeSelect);
            });

            var modeRow = Node("ModeRow");
            if (modeRow != null) modeRow.gameObject.SetActive(true);

            BuildRightPanelNetwork();
            BuildLobbyEntryControls(net);
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

            RejoinRunningMatch();

            AutoHost();
        }

        /// <summary>
        /// Opens a LAN room the moment the player arrives, so MULTIPLAYER lands them somewhere
        /// rather than on a form.
        ///
        /// ⚠️⚠️ IT MUST FAIL SOFT. Binding <see cref="LobbySession.DefaultPort"/> is the one thing
        /// on this path that can be refused by something outside the game, and the commonest
        /// reason is the player's OWN second copy running for a two-machine test. A hard failure
        /// here would strand somebody on a lobby with no host, no explanation and no way forward,
        /// which is the exact shape `NetSession.ClientDisconnected` was written to stop. So a
        /// refusal leaves the screen in the fourth state of § 68.5: still the lobby, no transport,
        /// the real reason on the status label, and the join panel already open, because joining
        /// somebody else is the way out of a port you cannot have.
        ///
        /// ⚠️ AND IT IS SKIPPED WHEN A SESSION IS ALREADY LIVE. Arriving here as a client, or as a
        /// host coming back from a finished match, must not tear down the session that brought
        /// the player. `IsNetworked` is the whole test.
        ///
        /// ⚠️ THE `this == null` CHECK IS NOT DEFENSIVE PADDING. This is an async void continuing
        /// after an await, and the player can press BACK or ESC during the handshake; the screen
        /// is then a destroyed Unity object that still answers a C# reference, and touching a
        /// label on it throws inside a continuation nothing is watching.
        /// </summary>
        private async void AutoHost()
        {
            if (!IsLobby) return;

            var net = NetSession.Instance;
            if (net == null || net.IsNetworked) return;

            SetStatus("Opening your lobby...");

            bool ok = await net.StartHostAsync();

            if (this == null) return;

            if (ok)
            {
                SetStatus("Your lobby is open. Read the code out, or press JOIN to enter somebody else's.");
            }
            else
            {
                // ⚠️ THE TRANSPORT'S OWN REASON, NOT A FIXED SENTENCE. `NetSession` writes a
                // precise status on the way out of each failure and every caller used to
                // overwrite it, so a refused port, a dead adapter and a wedged previous session
                // all read identically. `ConvertedMultiplayerSetup.Reason` records what that cost.
                string detail = string.IsNullOrWhiteSpace(net.Status) ? "" : $"  ({net.Status})";
                SetStatus($"Could not open a lobby on port {LobbySession.DefaultPort}. " +
                          $"Another copy of the game may already have it. Press JOIN to enter " +
                          $"somebody else's instead.{detail}");

                OpenJoinPanel();
            }

            Refresh();
        }

        /// <summary>
        /// A client that lands on this screen while a match is already running belongs in the
        /// ARENA, not here.
        ///
        /// ⚠️⚠️ THIS CLOSES THE HOLE A REJOINING PLAYER FELL INTO, AND THE HOLE IS A RACE THAT
        /// NOTHING ELSE COULD CATCH. There were two independent ways into a running match and both
        /// of them are one-shot:
        ///
        ///   * `ConvertedMultiplayerSetup.Join` calls `SceneFlow.Go(MatchSetup)` the moment the
        ///     transport starts, and
        ///   * `MatchRpc.OnSeatingMsg` calls `SceneFlow.StartMatch()` when the host's seating
        ///     packet says a match is in progress.
        ///
        /// Both are `SceneManager.LoadScene` calls, both are deferred to the end of the frame, and
        /// **the seating packet can arrive before the lobby scene has finished loading**. When it
        /// does, the arena load is queued FIRST and the lobby load queued second, so the lobby
        /// wins and the arena request is gone: `OnSeatingMsg` has already fired and will not fire
        /// again. The player is left sitting in the lobby of a match that is running without them,
        /// and no button on this screen leads in — START is host-only and the seat rows are greyed
        /// out precisely because a match is in progress. 🧑 2026-08-28: *"you'll only get ported
        /// back to the lobby with no way of joining back"*.
        ///
        /// ⚠️ SO THE DECISION IS MADE ON ARRIVAL AS WELL AS ON THE PACKET, and whichever happens
        /// last is the one that works. `LobbySession.MatchInProgress` is written by
        /// `OnSeatingMsg` before it navigates, so by the time this screen wires itself the flag is
        /// already there to be read.
        ///
        /// ⚠️ THE HOST IS EXCLUDED. A host sits in this lobby legitimately between matches, and
        /// its own `MatchInProgress` is true from `HostStartMatch` until the match ends; sending
        /// it to the arena from here would fight the screen it just chose.
        ///
        /// ⚠️ AND A SPECTATOR GOES TOO. Somebody admitted to watch a running match has no seat,
        /// but the thing they came to watch is in the arena.
        /// </summary>
        private void RejoinRunningMatch()
        {
            var net = NetSession.Instance;
            if (net == null || !net.IsNetworked) return;
            if (NetAuthority.IsHost) return;
            if (!net.Lobby.MatchInProgress) return;

            SetStatus("Rejoining the match in progress...");
            SceneFlow.StartMatch();
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

        /// <summary>
        /// The row that gets you OUT of your own lobby and into somebody else's, plus the switch
        /// between a LAN room and an online one.
        ///
        /// ⚠️⚠️ ONLINE IS A FIRST-CLASS LOBBY AND NOT A LEFTOVER. 🧑 2026-08-28: *"make sure u can
        /// do online server lobby too"*. Auto-hosting on LAN is the LANDING state, not the only
        /// one, so GO ONLINE re-hosts the same lobby through Relay and publishes it to the online
        /// pool. The join side is already symmetric: `ResolveCodeAsync` answers `IsLan` and
        /// `LobbyJoinPanel` branches on it, so one four-character code reaches either kind and a
        /// player reading a code out never has to know which they are in.
        ///
        /// ⚠️ THE SWITCH IS A SECOND HOST → LEAVE → HOST IN ONE LAUNCH, which is `docs/TODO.md`
        /// § 65.1 from a third direction. It works because every `NetSession` start opens with
        /// `EnsureStoppedAsync`; it is on the two-process list because "works by construction" is
        /// what was believed the first three times.
        /// </summary>
        private void BuildLobbyEntryControls(NetSession net)
        {
            if (!IsLobby || net == null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            _joinPanel = LobbyJoinPanel.Build(canvas.transform, net);
            _joinPanel.Status += SetStatus;
            _joinPanel.Joined += HandleJoinedInPlace;

            if (_codeRow == null || _codeRow.transform.parent == null) return;

            var row = new GameObject("LobbyEntryRow");
            row.transform.SetParent(_codeRow.transform.parent, false);
            row.transform.SetSiblingIndex(_codeRow.transform.GetSiblingIndex() + 1);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var element = row.AddComponent<LayoutElement>();
            element.minHeight = 44;
            element.preferredHeight = 44;
            element.flexibleWidth = 1;

            _joinButton = MenuKit.WoodButton(row.transform, "JOIN A GAME", Vector2.zero,
                                             Vector2.zero, new Vector2(0.0f, 44.0f),
                                             OpenJoinPanel);
            _joinButton.name = "OpenJoinButton";
            _joinButton.gameObject.AddComponent<LayoutElement>().minHeight = 44;

            _onlineButton = MenuKit.WoodButton(row.transform, "GO ONLINE", Vector2.zero,
                                               Vector2.zero, new Vector2(0.0f, 44.0f),
                                               ToggleOnline);
            _onlineButton.name = "GoOnlineButton";
            _onlineButton.gameObject.AddComponent<LayoutElement>().minHeight = 44;

            _lobbyEntryRow = row;
        }

        /// <summary>
        /// Stands the cast in the arena behind the screen, and hangs their names over them.
        ///
        /// ⚠️⚠️ THE PRACTICE SCREEN GETS THE MAP SHOT AND NO CAST, AND THAT IS NOT AN OVERSIGHT.
        /// Offline this screen is a MAP PICKER with a bots row in it: the thing being chosen is
        /// the arena, so the wide shot from 22 m that every map's `Distance` and `Height` were
        /// tuned for is the correct picture, and four motionless strangers standing in the middle
        /// of it would be four seats nobody is sitting in. The lobby is the screen where who is
        /// here is the question.
        /// </summary>
        private void BuildCast(Transform previewNode)
        {
            if (_preview == null || previewNode == null) return;

            _preview.LobbyShot = IsLobby;

            if (!IsLobby) return;

            _cast = LobbyCast.Attach(_preview);

            var rect = previewNode as RectTransform;
            if (rect != null && _cast != null)
            {
                _nameplates = LobbyNameplates.Attach(rect, _preview, _cast);
            }
        }

        /// <summary>
        /// Puts each seat's PICKED character in its chair and writes its plate.
        ///
        /// ⚠️⚠️ THE PICK COMES FROM THE SEAT TABLE, WHICH IS THE SAME INT `MatchInstaller` BUILDS
        /// THE REAL BODY FROM. 🧑 2026-08-28: *"make sure the character for everyone corresponds
        /// to their actual character in the game"*. `LobbySeatInfo.CharacterPick` is host
        /// authoritative and already on the wire; resolving it through the same
        /// `RosterBook.PersonArt(index, mode)` the match uses is what makes the lobby a promise
        /// rather than a decoration.
        ///
        /// ⚠️ AND THE LOCAL SEAT READS FROM SETTINGS, NOT FROM THE TABLE. A pick made on this
        /// machine is applied the moment the character panel closes and only reaches the table
        /// after a round trip to the host; reading the table for your own seat would leave your
        /// own body a second behind your own choice, which is the one case somebody is watching
        /// for.
        ///
        /// ⚠️⚠️ THE READY TICK IS LOCAL-ONLY UNTIL THE WIRE CARRIES IT. `LobbySeatInfo` has no
        /// `Ready` field and `OnLobbyReadyChanged` is a COUNT, so the host knows how many are
        /// ready and nobody knows WHICH. Adding the field is a protocol bump, and `docs/TODO.md`
        /// § 68.2 holds every bump until § 69's chat so there is exactly one. Until then a remote
        /// seat's plate is honest about what it knows: the name, and no claim about readiness.
        /// </summary>
        private void RefreshCast()
        {
            if (_cast == null) return;

            var net = NetSession.Instance;
            bool live = IsLive;
            var settings = Settings.SettingsStore.Current;
            var people = Roster.GetPeople(SceneFlow.SelectedMode);

            int defender = MatchRules.DefenderSlotFor(1);

            for (int seat = 0; seat < _castPicks.Length; seat++)
            {
                bool mine = !GameLaunch.Spectator &&
                            (live ? (net != null && net.LocalSlot == seat) : (seat == GameLaunch.SoloSeat));

                var info = live ? MatchRpc.Instance?.GetSeatInfo(seat) : null;
                bool occupied = mine || (info != null && info.Occupied);

                int pick = mine
                    ? settings.CharacterPick
                    : (info != null && info.CharacterPick >= 0 ? info.CharacterPick : seat);

                if (people == null || people.Count == 0) pick = 0;
                else pick = ((pick % people.Count) + people.Count) % people.Count;

                _castPicks[seat] = pick;

                if (_nameplates == null) continue;

                string who = mine
                    ? "YOU"
                    : occupied
                        ? (string.IsNullOrEmpty(info?.Name) ? $"PLAYER {seat + 1}" : info.Name)
                        : "BOT";

                _nameplates.SetSeat(seat, who,
                                    ready: mine && _localReady,
                                    taya: seat == defender,
                                    you: mine);
            }

            _cast.Show(_castPicks, SceneFlow.SelectedMode);
        }

        private void OpenJoinPanel()
        {
            if (_joinPanel == null) return;

            _joinPanel.Open();
        }

        /// <summary>
        /// ⚠️⚠️ A JOIN THAT LANDS HERE REDRAWS, IT DOES NOT RELOAD. `ConvertedMultiplayerSetup`
        /// finished every join with `SceneFlow.Go(MatchSetup)` because it was on a different
        /// scene; from here that would destroy the map preview's cached arenas, both render
        /// textures and the cast, and `SceneFlow.Go`'s latch is scoped to one frame so it would
        /// not even be deduplicated.
        ///
        /// ⚠️⚠️ AND `RejoinRunningMatch` HAS TO BE ASKED AGAIN. It runs from `Wire()`, which a
        /// join in place never re-enters, and it is the ONLY thing that sends a client who joined
        /// a game already in progress into the arena. Its own header records the hole:
        /// *"you'll only get ported back to the lobby with no way of joining back"*. The seating
        /// packet that sets `MatchInProgress` can arrive either side of this, which is why the
        /// question is asked on arrival AND on the packet.
        /// </summary>
        private void HandleJoinedInPlace()
        {
            _localReady = false;
            _readyCount = 0;
            _readyExpected = 0;

            var s = Settings.SettingsStore.Current;
            MatchRpc.Instance?.SelectLobbyPickServerRpc(s.CharacterPick, s.CanPick, s.SlipperPick);

            Refresh();
            RejoinRunningMatch();
        }

        /// <summary>
        /// Swaps this lobby between a LAN room and a Relay one, in place.
        ///
        /// ⚠️ HOST ONLY, AND SILENTLY IMPOSSIBLE OTHERWISE. A client pressing this would tear
        /// down its own connection to become a host of nothing. The button is hidden rather than
        /// greyed for a client, because "go online" on a peer that is already in somebody's
        /// online game is not a disabled action, it is a meaningless one.
        /// </summary>
        private async void ToggleOnline()
        {
            var net = NetSession.Instance;
            if (net == null || !NetAuthority.IsHost) return;
            if (_switchingHost) return;

            _switchingHost = true;
            RefreshEntryControls();

            try
            {
                bool goingOnline = !net.IsRelay;

                SetStatus(goingOnline
                          ? "Opening an online room..."
                          : "Moving your room back onto your network...");

                bool ok = goingOnline
                    ? await net.StartRelayHost()
                    : await net.StartHostAsync();

                if (this == null) return;

                SetStatus(ok
                          ? (goingOnline
                             ? "Your room is online. Read the code out to anybody, anywhere."
                             : "Your room is back on your own network.")
                          : ReasonFor(net, goingOnline
                                      ? "Could not open an online room."
                                      : "Could not reopen a room on your network."));
            }
            finally
            {
                _switchingHost = false;

                if (this != null) Refresh();
            }
        }

        /// <summary>Headline plus the session's own detail. See `LobbyJoinPanel.Reason`.</summary>
        private static string ReasonFor(NetSession net, string headline)
        {
            string detail = net != null ? net.Status : null;
            return string.IsNullOrWhiteSpace(detail) ? headline : $"{headline}  ({detail})";
        }

        private void RefreshEntryControls()
        {
            if (_lobbyEntryRow == null) return;

            bool live = IsLive;
            bool host = NetAuthority.IsHost;

            _lobbyEntryRow.SetActive(IsLobby);

            if (_joinButton != null)
            {
                // ⚠️ IT STAYS PRESSABLE WHILE HOSTING, because leaving your own empty room to
                // join a friend's is the normal case, not an edge one. It is refused only while a
                // switch is already in flight.
                _joinButton.interactable = !_switchingHost;

                var label = _joinButton.GetComponentInChildren<Text>();
                if (label != null) label.text = live && !host ? "LEAVE AND JOIN ANOTHER" : "JOIN A GAME";
            }

            if (_onlineButton != null)
            {
                _onlineButton.gameObject.SetActive(live && host);
                _onlineButton.interactable = !_switchingHost;

                var net = NetSession.Instance;
                var label = _onlineButton.GetComponentInChildren<Text>();

                if (label != null)
                {
                    label.text = _switchingHost
                        ? "SWITCHING..."
                        : (net != null && net.IsRelay ? "GO BACK TO LAN" : "GO ONLINE");
                }
            }
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

        /// <summary>
        /// ⚠️ THE HOST IS NOT SENT BACK BY THIS. `ClientDisconnected` is raised only on the
        /// non-host branch of `NetSession.OnClientDisconnected`, but a listen host is also its
        /// own client, so the guard is kept here as well rather than relying on one at a
        /// distance: a host bounced out of its own lobby by a peer leaving would be absurd.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️ IT STAYS ON THIS SCREEN NOW. This used to navigate to `MultiplayerSetup`, which
        /// was the only place a refused or dropped client could try again from. That screen is no
        /// longer on the path (`ConvertedModeSelect`), and sending somebody there would drop them
        /// out of a lobby into a form they never chose to open.
        ///
        /// ⚠️ THE REASON IS SHOWN HERE RATHER THAN CARRIED. `NetSession.LastDisconnectReason`
        /// exists because the refusal used to arrive seconds after the join screen had already
        /// navigated away, so the one actionable line (a protocol mismatch is a thing a player can
        /// fix) was written to a label nobody was looking at. Landing on the screen that can act
        /// on it removes the whole problem, so the reason is read and cleared right here.
        ///
        /// ⚠️ AND THE JOIN PANEL OPENS, because "your connection ended" with no way to start
        /// another one is the same dead end from a different direction.
        /// </summary>
        private void HandleClientDisconnected(string reason)
        {
            if (NetAuthority.IsHost) return;

            string detail = !string.IsNullOrWhiteSpace(reason)
                ? reason
                : NetSession.LastDisconnectReason;

            NetSession.LastDisconnectReason = "";

            SetStatus(string.IsNullOrWhiteSpace(detail)
                      ? "The connection to the host ended. Press JOIN to try again."
                      : detail);

            _localReady = false;
            _readyCount = 0;
            _readyExpected = 0;

            OpenJoinPanel();
            Refresh();
        }

        private void HandleLobbyRosterSynced(LobbySeatInfo[] seats) => RefreshSeats();

        private void HandleModeSynced(int mode) => Refresh();

        private void HandleLobbyReadyChanged(int ready, int expected)
        {
            _readyCount = ready;
            _readyExpected = expected;

            // ⚠️ THE LOCAL TICK FOLLOWS THE HOST'S TALLY RATHER THAN A LOCAL BOOL. The button
            // used to toggle a field this screen owned, so a press the host refused (a spectator,
            // a peer with no seat) still drew as READY on the one screen that mattered.
            //
            // ⚠️ `RefreshActionButtons` REPLACED `RefreshReadyLabel` HERE, and it is not merely a
            // rename: the old one wrote to `PrimaryButton` unconditionally, which on a host now
            // writes a READY label onto a hidden node while START, the button the host can
            // actually see, kept a stale tally.
            RefreshActionButtons();
            RefreshSeats();
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
            //
            // ⚠️⚠️ THE HEADLINE ASKS `IsLobby`, NOT `IsNetworked`, AND THE DIFFERENCE IS A WHOLE
            // SECOND OF EVERY MULTIPLAYER SESSION. The transport is not up yet while the auto-host
            // is starting, so the old test drew the multiplayer lobby as PRACTICE MODE for the
            // length of the handshake and permanently if the port bind was refused. See `IsLobby`.
            SetHeadline("BannerLabel", IsLobby ? "LOBBY" : "PRACTICE MODE", 66);
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
            if (IsLobby && isNetworked)
            {
                SetText("SeatHeading", NetAuthority.IsHost ? "LOBBY  ·  YOU ARE HOSTING" : "LOBBY  ·  CONNECTED");
                SetText("SeatHint", NetAuthority.IsHost
                        ? "You pick the map and the mode for everyone. Click a free seat to move. "
                          + "Empty seats are played by bots. Read the code out to the others."
                        : "The lobby leader picks the map and the mode. Click a free seat to move. "
                          + "Empty seats are played by bots. Press READY when you are.");

                // Network rows
                if (_addressRow != null)
                {
                    _addressRow.SetActive(true);
                    if (_addressText != null) _addressText.text = HostAddress();
                }

                if (_codeRow != null)
                {
                    string code = net?.Lobby?.JoinCode ?? "";
                    _codeRow.SetActive(!string.IsNullOrEmpty(code));
                    if (_codeText != null) _codeText.text = code;
                }
            }
            else if (IsLobby)
            {
                // ⚠️ THE LOBBY WITHOUT A TRANSPORT. Either the auto-host is still handshaking or
                // it was refused; `AutoHost` has already written which on the status label, and
                // the join panel is open in the refused case. Nothing here may touch the wire.
                SetText("SeatHeading", "LOBBY  ·  NOT CONNECTED");
                SetText("SeatHint",
                        "Opening a room on your network. If it does not open, press JOIN and "
                        + "enter somebody else's game with their code or address.");

                if (_addressRow != null) _addressRow.SetActive(false);
                if (_codeRow != null) _codeRow.SetActive(false);
            }
            else
            {
                SetText("SeatHeading", "YOUR CHARACTER");
                SetText("SeatHint",
                        "Four players, one taya. The taya rotates every round, so everyone defends "
                        + "exactly once. Empty seats are bots, the kids from the street who fill in.");

                if (_addressRow != null) _addressRow.SetActive(false);
                if (_codeRow != null) _codeRow.SetActive(false);
            }

            RefreshActionButtons();
            RefreshLeaderControls();
            RefreshEntryControls();
            RefreshSeats();
            RefreshCast();
        }

        /// <summary>
        /// This machine's LAN address, for the row a joiner types in.
        ///
        /// ⚠️ THE PORT COMES FROM `LobbySession.DefaultPort`, NOT FROM AN 8910 WRITTEN OUT TWICE.
        /// It was a literal here and a literal in the fallback string beside it, so changing the
        /// port would have left this screen advertising the old one to every joiner.
        /// </summary>
        private static string HostAddress()
        {
            int port = LobbySession.DefaultPort;

            try
            {
                var ips = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
                foreach (var ip in ips)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !System.Net.IPAddress.IsLoopback(ip))
                    {
                        return $"{ip}:{port}";
                    }
                }
            }
            catch (System.Exception e)
            {
                // ⚠️ A MACHINE WITH NO RESOLVABLE HOSTNAME STILL GETS A LOBBY. This throws on a
                // box with no DNS suffix and the old code let it escape into `Refresh`, which
                // would have taken the seat rows and the map preview down with it.
                Debug.LogWarning($"[Lobby] could not read this machine's address: {e.Message}");
            }

            return $"127.0.0.1:{port}";
        }

        /// <summary>
        /// ⚠️⚠️ ONE BUTTON, TWO MEANINGS, AND THE HOST NEVER SEES BOTH. 🧑 2026-08-28: *"start
        /// should be ready for everyone else except for host"*. Until then the host saw READY and
        /// START MATCH side by side, and pressing READY as the host declared readiness to a gate
        /// the host is not blocked on: a control that reads as the way to begin and is not.
        ///
        /// ⚠️ THIS IS A LAYOUT CHANGE, NOT A RULE CHANGE. `docs/TODO.md` § 59.3 already made
        /// readiness an ANSWER the host reads rather than a trigger, on request, and the host's
        /// START is deliberately live whatever the tally says because a host plus three bots is a
        /// legitimate match. Both nodes keep their own handlers; only one is on screen.
        ///
        /// ⚠️ AND THE TALLY FOLLOWS THE VISIBLE BUTTON. It used to be appended to READY only, so
        /// the host, the one person who has to decide when to start, was the one person who could
        /// not see how many people were ready.
        /// </summary>
        private void RefreshActionButtons()
        {
            var primNode = Node("PrimaryButton");
            var startNode = Node("StartButton");

            bool live = IsLive;
            bool host = NetAuthority.IsHost;

            string tally = _readyExpected > 1 ? $"   {_readyCount}/{_readyExpected}" : "";

            if (IsLobby && live && host)
            {
                if (primNode != null) primNode.gameObject.SetActive(false);
                if (startNode != null)
                {
                    startNode.gameObject.SetActive(true);
                    SetText("StartButton", $"START MATCH{tally}");
                    var btn = startNode.GetComponent<Button>();
                    if (btn != null) btn.interactable = true;
                }
                return;
            }

            if (startNode != null) startNode.gameObject.SetActive(false);
            if (primNode == null) return;

            primNode.gameObject.SetActive(true);
            var prim = primNode.GetComponent<Button>();

            if (!IsLobby)
            {
                SetText("PrimaryButton", "START MATCH");
                if (prim != null) prim.interactable = true;
                return;
            }

            if (!live)
            {
                // ⚠️ NOT "START MATCH". A multiplayer lobby with no transport that offers to start
                // a match would drop somebody waiting for a friend into a solo game against bots.
                SetText("PrimaryButton", "CONNECTING...");
                if (prim != null) prim.interactable = false;
                return;
            }

            string label = GameLaunch.Spectator
                ? "SPECTATING"
                : _localReady ? "WAITING" : "READY";

            SetText("PrimaryButton", $"{label}{tally}");
            if (prim != null) prim.interactable = !GameLaunch.Spectator;
        }

        /// <summary>
        /// Greys the three cycle rows for anybody who is not the lobby leader.
        ///
        /// ⚠️⚠️ THEY USED TO LIGHT UP, CLICK, PLAY THEIR SOUND AND CHANGE NOTHING. `OnMapCycle`,
        /// `OnModeCycle` and `OnDifficultyCycle` each open with
        /// `if (!NetAuthority.IsHost &amp;&amp; SceneFlow.Networked) return;`, which is the correct
        /// authority and was the whole of the feedback: a live-looking arrow that silently does
        /// nothing is indistinguishable from a broken one, and "the buttons dont work" is a report
        /// this project has already chased four separate causes for.
        ///
        /// ⚠️ THE GUARDS STAY. This is the DISPLAY half; the refusal is still enforced in the
        /// handler, because a client that reaches the method by any other route must still be
        /// refused. Never replace a guard with a greyed button.
        /// </summary>
        private void RefreshLeaderControls()
        {
            bool allowed = !IsLobby || NetAuthority.IsHost;

            foreach (string node in LeaderOnlyButtons)
            {
                foreach (var t in Nodes(node))
                {
                    var btn = t.GetComponent<Button>();
                    if (btn != null) btn.interactable = allowed;
                }
            }
        }

        private static readonly string[] LeaderOnlyButtons =
        {
            "MapPrevButton", "MapNextButton",
            "ModePrevButton", "ModeNextButton",
            "DifficultyPrevButton", "DifficultyNextButton",
        };

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

            // ⚠️ THE CAST IS DESTROYED BY NAME RATHER THAN LEFT TO THE SCENE UNLOAD. Its root is
            // parented into the ADDITIVELY loaded arena, which `MapPreviewSurface` keeps cached
            // and deactivated across map cycles on purpose; a single-scene load does take those
            // with it, but a lobby that is reloaded while the same arena is cached would leave a
            // second stage of four bodies inside it and photograph both.
            if (_cast != null) Destroy(_cast.gameObject);

            if (_joinPanel != null)
            {
                _joinPanel.Status -= SetStatus;
                _joinPanel.Joined -= HandleJoinedInPlace;
            }

            MatchRpc.OnMapChanged -= HandleMapSynced;
            MatchRpc.OnDifficultyChanged -= HandleDifficultySynced;
            MatchRpc.OnLobbyPicksSynced -= HandleLobbyPicksSynced;
            MatchRpc.OnLobbyRosterSynced -= HandleLobbyRosterSynced;
            MatchRpc.OnLobbyReadyChanged -= HandleLobbyReadyChanged;
            MatchRpc.OnModeChanged -= HandleModeSynced;
            MatchRpc.OnMatchStarted -= HandleMatchStarted;
            NetSession.ClientDisconnected -= HandleClientDisconnected;
        }
    }
}

