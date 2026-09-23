using System.Collections.Generic;
using System.Threading.Tasks;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI.Hub;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// UX-1: this screen's controller serves the hub (`TumpHub`) as its view.
    ///
    /// ⚠️⚠️ EVERY METHOD HERE IS A THIN CALL INTO A PATH THE OLD PREPARATION BOARD ALREADY PRESSED.
    /// Hosting is `NetSession.StartHostAsync` / `StartRelayHost` (the auto-host and GO ONLINE
    /// paths), joining is `LobbyJoinPanel.AutomationJoin` (the join card's own `Connect`), the queue
    /// is `Matchmaker.StartQueue`, readiness is `OnPrimaryPressed`, a pick is
    /// `SelectLobbyPickServerRpc`, the map and mode are the cycle handlers' RPCs, and a match start
    /// is `HostStartMatch`. `docs/TODO.md` § 38.5 is why nothing new was written underneath.
    ///
    /// ⚠️ THE OLD BOARD IS STILL BUILT, AND HIDDEN. Its live court is handed to the hub as HOME's
    /// background, and its controls stay the owners of state the refresh paths write to, so no
    /// handler here needed a second copy of the lobby's bookkeeping. `docs/TODO.md` § 68.3's
    /// keep-the-old-chrome rule: restoring it is `HubEnabled = false`.
    /// </summary>
    public sealed partial class ConvertedMatchSetup : IHubHost
    {
        /// <summary>The hub is the view of this scene. False restores the preparation board.</summary>
        /// <remarks>
        /// ⚠️ `-tp-preparation-board` on the command line starts with the board restored. It is the
        /// documented way back to the old view for a player, and it is how the old fixtures are run
        /// against the view they were written for, so a failure can be sorted into "the hub caused
        /// this" and "this was already red" instead of guessed at.
        /// </remarks>
        public static bool HubEnabled = !BoardRequested();

        private static bool BoardRequested()
        {
            foreach (var arg in System.Environment.GetCommandLineArgs())
                if (string.Equals(arg, "-tp-preparation-board", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private TumpHub _hubView;
        private string _lastJoinStatus = "";

        private void InstallHubView()
        {
            if (!HubEnabled || _ownerPreparation == null) return;
            _hubView = TumpHub.Install(transform, this);
            _ownerPreparation.Canvas.gameObject.SetActive(false);
        }

        /// <summary>The parent the room chat hangs off: the hub when it is the view.</summary>
        private Transform ChatParent() => _hubView != null ? _hubView.Canvas.transform : _ownerPreparation.Canvas.transform;

        // ------------------------------------------------------------------ IHubHost

        public MapPreviewSurface Preview => _preview;

        public bool OverlayOpen
        {
            get
            {
                if (_hub != null && _hub.IsOpen) return true;
                if (_joinPanel != null && _joinPanel.IsOpen) return true;
                if (_ownerCustomRules != null && _ownerCustomRules.IsOpen) return true;
                if (_characterPanel != null && _characterPanel.gameObject.activeInHierarchy) return true;
                var settings = GetComponentInChildren<ConvertedSettingsPanel>(false);
                if (settings != null && settings.gameObject.activeInHierarchy) return true;
                if (HubCredits.IsOpen) return true;
                return ScreenTakeover.AnyOpen;
            }
        }

        public void OpenSettings() => OpenGameSettings();
        public void OpenProfile() { MenuSfx.Click(); _hub?.OpenTab(PlayerHub.Door.Profile); }
        public void OpenCareer() { MenuSfx.Click(); _hub?.OpenTab(PlayerHub.Door.Career); }
        public void OpenParty() { MenuSfx.Click(); _hub?.OpenTab(PlayerHub.Door.Party); }
        public void OpenCustomRules() => OpenOwnerCustomRules();

        public void StartPractice()
        {
            // ⚠️ PRACTICE IS THE OFFLINE MATCH THE OLD WITH BOTS ROUTE STARTED: no transport, bots
            // in the empty seats, the mode card's ruleset. A room or a search is left first, so a
            // practice match can never start while this machine is still offered to strangers.
            LeaveRoom();
            GameLaunch.Spectator = false;
            if (_difficulty == AIController.NoBotsIndex)
            {
                _difficulty = (int)Difficulty.Normal;
                Settings.SettingsStore.Current.AiDifficulty = _difficulty;
                Settings.SettingsStore.Save();
            }
            AIController.ApplyDifficulty(_difficulty);
            AIController.BotsEnabled = true;
            SceneFlow.StartMatch();
        }

        public string StartQueue(GameMode mode, QueueStake stake)
        {
            if (IsLive && !HubQueueWatch.QueueRoom) LeaveRoom();
            SceneFlow.SelectedMode = mode;
            _ownerRoute = stake == QueueStake.Ranked ? LobbyMode.Ranked : LobbyMode.Custom;
            var queue = Matchmaker.Ensure();
            if (!queue.StartQueue(mode, stake, 1)) return queue.Refusal;
            SceneFlow.Networked = true;
            return "";
        }

        public void CancelQueue()
        {
            var queue = Matchmaker.Current;
            if (queue != null && (queue.IsQueueing || queue.State == QueueState.Refused)) queue.Cancel();
            var net = NetSession.Instance;
            if (HubQueueWatch.QueueRoom && net != null && net.IsNetworked) net.Stop();
            if (HubQueueWatch.QueueRoom) SceneFlow.Networked = false;
            HubQueueWatch.End();
            _localReady = false;
        }

        public void AcceptBots()
        {
            // Phase 11's offer, re-homed from the old queue card: the room leaves the pool, and the
            // empty seats will be bots when the host starts.
            Matchmaker.Current?.Cancel();
            if (_difficulty == AIController.NoBotsIndex) _difficulty = (int)Difficulty.Normal;
            AIController.ApplyDifficulty(_difficulty);
            AIController.BotsEnabled = true;
        }

        public async Task<string> HostRoom(string title, string map, GameMode mode, RoomVisibility visibility, bool online)
        {
            LeaveRoom();
            NetSession.RoomTitle = title ?? "";
            NetSession.RoomMap = map ?? "";
            NetSession.RoomVisibility = (int)visibility;
            SelectMap(map);
            SelectMode(mode);
            SceneFlow.Networked = true;
            _ownerRoute = LobbyMode.Custom;

            var net = NetSession.Ensure();
            int port = NetBootstrap.LobbyPort > 0 ? NetBootstrap.LobbyPort : LobbySession.DefaultPort;
            bool ok = online ? await net.StartRelayHost() : await net.StartHostAsync(port);
            if (this == null) return "";
            if (!ok)
            {
                string reason = ReasonFor(net, online ? "Could not open an online room." : "Could not open a room on your network.");
                SceneFlow.Networked = false;
                NetSession.ClearRoomSettings();
                return reason;
            }

            MatchRpc.Instance?.SelectMapServerRpc(_map);
            MatchRpc.Instance?.SelectModeServerRpc((int)SceneFlow.SelectedMode);
            PublishPicks();
            Refresh();
            return "";
        }

        public async Task<string> Join(string codeOrAddress)
        {
            if (_joinPanel == null) return "The join path is not ready yet.";
            LeaveRoom();
            SceneFlow.Networked = true;
            _ownerRoute = LobbyMode.Custom;
            _lastJoinStatus = "";
            _joinPanel.Status += NoteJoinStatus;
            bool ok;
            try { ok = await _joinPanel.AutomationJoin(codeOrAddress); }
            finally { if (_joinPanel != null) _joinPanel.Status -= NoteJoinStatus; }
            if (this == null) return "";
            if (ok) return "";
            SceneFlow.Networked = false;
            return string.IsNullOrWhiteSpace(_lastJoinStatus) ? "Could not join that room." : _lastJoinStatus;
        }

        private void NoteJoinStatus(string line) => _lastJoinStatus = line;

        public void LeaveRoom()
        {
            if (HubQueueWatch.QueueRoom || (Matchmaker.Current != null && Matchmaker.Current.IsQueueing)) CancelQueue();
            var net = NetSession.Instance;
            if (net != null && net.IsNetworked) net.Stop();
            SceneFlow.Networked = false;
            NetSession.ClearRoomSettings();
            _localReady = false;
            if (_chat != null) _chat.SetPresented(false);
        }

        public bool InRoom => IsLive;
        public bool IsHost => IsLive && NetAuthority.IsHost;
        public bool LocalReady => _localReady;
        public string RoomCode => NetSession.Instance?.Lobby?.JoinCode ?? "";
        public bool RoomOnline => NetSession.Instance != null && NetSession.Instance.IsRelay;
        public bool MatchInProgress => NetSession.Instance?.Lobby != null && NetSession.Instance.Lobby.MatchInProgress;

        public string RoomTitle
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(NetSession.RoomTitle)) return NetSession.RoomTitle;

                // A joiner has no title of its own: read the room's name off the listing it came from.
                var net = NetSession.Instance;
                string code = RoomCode;
                if (net?.Query != null && !string.IsNullOrEmpty(code))
                    foreach (var entry in net.Query.Servers)
                        if (string.Equals(entry.JoinCode, code, System.StringComparison.OrdinalIgnoreCase)) return entry.Name;
                if (net?.Beacon != null && !string.IsNullOrEmpty(code))
                    foreach (var entry in net.Beacon.SortedEntries)
                        if (string.Equals(entry.JoinCode, code, System.StringComparison.OrdinalIgnoreCase)) return entry.HostName;
                return "";
            }
        }

        public HubSeat[] Seats()
        {
            var seats = new HubSeat[Balance.PlayerCount];
            bool live = IsLive;
            int local = live ? NetAuthority.LocalSlot : GameLaunch.SoloSeat;
            var settings = Settings.SettingsStore.Current;
            for (int i = 0; i < seats.Length; i++)
            {
                var info = live ? MatchRpc.Instance?.GetSeatInfo(i) : null;
                bool mine = !GameLaunch.Spectator && i == local && (live || i == local);
                bool occupied = mine || (info != null && info.Occupied);
                seats[i] = new HubSeat
                {
                    Slot = i,
                    Occupied = occupied,
                    Mine = mine,
                    Host = live && occupied && (mine ? NetAuthority.IsHost : info != null && info.PeerId == 0),
                    Ready = mine ? _localReady : info != null && info.Ready,
                    Bot = !occupied && AIController.BotsEnabled,
                    Name = mine ? "YOU" : info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : "PLAYER " + (i + 1),
                    CharacterPick = mine ? settings.CharacterPick : info?.CharacterPick ?? -1,
                };
            }
            return seats;
        }

        public void StartGame()
        {
            var net = NetSession.Instance;
            if (net == null || !net.IsNetworked) { SceneFlow.StartMatch(); return; }
            if (!NetAuthority.IsHost) return;

            if (!AIController.BotsEnabled && net.Lobby.OccupiedSeatCount() < Balance.PlayerCount)
            {
                if (HubQueueWatch.QueueRoom) AcceptBots();
                else
                {
                    _hubView?.Toast("Bots are off in this room's rules. Fill all four seats, or turn bots on under RULES.");
                    MenuSfx.Error();
                    return;
                }
            }

            // ⚠️ NO SECOND `SceneFlow.StartMatch()`: `HostStartMatch` fires `OnMatchStarted`, which
            // this screen answers with the load (`HandleMatchStarted`).
            MatchRpc.Instance?.HostStartMatch();
        }

        public void ToggleReady() => OnPrimaryPressed();

        public void LockIn()
        {
            PublishPicks();
            if (MatchRpc.Instance != null && MatchRpc.Instance.DeclareReadyServerRpc(true)) _localReady = true;
        }

        public void PublishPicks()
        {
            var s = Settings.SettingsStore.Current;
            if (IsLive) MatchRpc.Instance?.SelectLobbyPickServerRpc(s.CharacterPick, s.CanPick, s.SlipperPick);
            Refresh();
        }

        public void SelectMap(string map)
        {
            int index = System.Array.IndexOf(SceneFlow.Maps, map);
            if (index < 0) return;
            if (IsLive && !NetAuthority.IsHost) return;
            _map = index;
            SceneFlow.SelectedMap = map;
            if (IsLive) { NetSession.RoomMap = map; MatchRpc.Instance?.SelectMapServerRpc(_map); NetSession.Instance?.RepublishLobbyAdvert(); }
            if (_preview != null && _preview.Showing != map) _preview.Show(map);
            Refresh();
        }

        public void SelectMode(GameMode mode)
        {
            if (IsLive && !NetAuthority.IsHost) return;
            if (SceneFlow.SelectedMode != mode) OnModeCycle(1);
        }

        public void Browse()
        {
            var net = NetSession.Ensure();
            net.BrowseLan();
            net.Query?.StartBrowsing();
        }

        public List<HubRoom> Rooms(bool lan)
        {
            var rooms = new List<HubRoom>();
            var net = NetSession.Instance;
            if (net == null) return rooms;
            string own = IsLive && NetAuthority.IsHost ? RoomCode : "";

            if (lan)
            {
                foreach (var entry in net.Beacon?.SortedEntries ?? new List<LanEntry>())
                {
                    if (!string.IsNullOrEmpty(own) && string.Equals(entry.JoinCode, own, System.StringComparison.OrdinalIgnoreCase)) continue;
                    rooms.Add(new HubRoom
                    {
                        Name = entry.HostName,
                        Map = "LAN",
                        Players = entry.Players,
                        Capacity = entry.MaxPlayers,
                        InProgress = entry.InProgress,
                        Key = $"{entry.Address}:{entry.Port}",
                    });
                }
                return rooms;
            }

            foreach (var entry in net.Query?.Servers ?? new List<ServerQuery.Entry>())
            {
                // ⚠️ ONLY PUBLIC CUSTOM ROOMS ARE LISTED. A queue room carries a pool key and is found
                // by the matchmaker, never browsed; a FRIENDS ONLY or PRIVATE room answers its code.
                if (!string.IsNullOrEmpty(entry.PoolKey) || entry.Visibility != 0) continue;
                if (!string.IsNullOrEmpty(own) && string.Equals(entry.JoinCode, own, System.StringComparison.OrdinalIgnoreCase)) continue;
                rooms.Add(new HubRoom
                {
                    Name = entry.Name,
                    Map = string.IsNullOrEmpty(entry.Map) ? "" : SceneFlow.PreviewFor(entry.Map).Name,
                    Players = entry.Players,
                    Capacity = entry.Capacity <= 0 ? LobbySession.MaxPlayers : entry.Capacity,
                    InProgress = entry.InProgress,
                    Key = entry.JoinCode,
                });
            }
            return rooms;
        }

        void IHubHost.ToggleSpectate() => ToggleSpectate();
        public bool Spectating => GameLaunch.Spectator;
        public string RoomAddress => IsLive && NetAuthority.IsHost && !RoomOnline ? HostAddress() : "";

        public void ToggleChat()
        {
            if (_chat != null) _chat.SetPresented(!_chat.IsPresented && IsLive);
        }

        // ------------------------------------------------------------------ routing

        /// <summary>Escape, pad B and Android BACK, while the hub is the view.</summary>
        private bool HubCancel()
        {
            if (_chat != null && _chat.IsPresented) { _chat.SetPresented(false); return true; }
            if (OverlayOpen) return true;   // the overlay on top answers its own Escape
            _hubView.Back();
            return true;
        }

        /// <summary>The connection to a host ended. The hub says so and goes HOME.</summary>
        private bool HubDisconnected(string detail)
        {
            if (_hubView == null) return false;
            var net = NetSession.Instance;
            if (net != null) net.Stop();
            SceneFlow.Networked = false;
            NetSession.ClearRoomSettings();
            HubQueueWatch.End();
            _localReady = false;
            _hubView.Home();
            _hubView.Toast(string.IsNullOrWhiteSpace(detail) ? "The room closed." : detail);
            return true;
        }
    }
}
