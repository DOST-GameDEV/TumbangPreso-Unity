using System.Threading.Tasks;
using TumbangPreso.Core;

namespace TumbangPreso.UI.Hub
{
    /// <summary>Who may see a hosted custom room. `docs/TODO.md` UX-1.6.</summary>
    public enum RoomVisibility
    {
        /// <summary>Listed in every online browser and answering its code.</summary>
        Public = 0,

        /// <summary>Unlisted; the code works, and presence shows it to friends.</summary>
        FriendsOnly = 1,

        /// <summary>Unlisted; the code works for whoever is told it, and presence does not show it.</summary>
        Private = 2,
    }

    /// <summary>One seat as the hub draws it, from the replicated roster.</summary>
    public struct HubSeat
    {
        public int Slot;
        public bool Occupied;
        public bool Mine;
        public bool Host;
        public bool Ready;
        public bool Bot;
        public string Name;
        public int CharacterPick;
    }

    /// <summary>
    /// What the hub asks of the screen controller underneath it.
    ///
    /// ⚠️⚠️ EVERY NETWORK DECISION STAYS IN `ConvertedMatchSetup`, AND THIS INTERFACE IS THE PROOF.
    /// The hub is a VIEW. Hosting, joining, queueing, readiness, the pick RPC and the match start are
    /// the methods the old preparation board already pressed (`docs/TODO.md` § 38.5: a second start
    /// path is how a maintained protocol becomes the one nothing calls). The hub never touches
    /// `NetSession` or `MatchRpc` directly.
    /// </summary>
    public interface IHubHost
    {
        MapPreviewSurface Preview { get; }

        /// <summary>True while an older overlay the controller owns (settings, the player hub, the
        /// custom rules sheet, the join card) is on screen, so the hub steps aside for it.</summary>
        bool OverlayOpen { get; }

        void OpenSettings();
        void OpenProfile();
        void OpenCareer();
        void OpenParty();
        void OpenCustomRules();

        void StartPractice();

        /// <summary>Start the queue. Returns an empty string, or the refusal sentence.</summary>
        string StartQueue(GameMode mode, QueueStake stake);
        void CancelQueue();
        void AcceptBots();

        Task<string> HostRoom(string title, string map, GameMode mode, RoomVisibility visibility, bool online);
        Task<string> Join(string codeOrAddress);
        void LeaveRoom();

        bool InRoom { get; }
        bool IsHost { get; }
        bool LocalReady { get; }
        string RoomCode { get; }
        bool RoomOnline { get; }
        string RoomTitle { get; }
        bool MatchInProgress { get; }

        HubSeat[] Seats();
        /// <summary>Choose a vacant/bot slot through the existing host-authoritative seat route.</summary>
        bool CanTakeSeat(int seat);
        void TakeSeat(int seat);

        /// <summary>Whether this machine may put a bot in an empty seat: the host of a custom
        /// room, before the match. Every other player sees the bots and cannot change them.</summary>
        bool CanEditBots { get; }

        /// <summary>ADD BOT (true) or REMOVE BOT (false) for one empty seat.</summary>
        void SetSeatBot(int seat, bool bot);

        Net.LobbySeatSwapOffer SeatSwapOffer { get; }
        string SeatSwapResult { get; }
        void RespondToSeatSwap(long requestId, bool accept);

        void StartGame();
        void ToggleReady();
        void LockIn();
        bool MapVoting { get; }
        float MapVoteSecondsLeft { get; }
        int MapVoteWinner { get; }
        int MapVoteFor(int seat);
        void VoteMap(int mapIndex);

        /// <summary>Tell the room this machine's current picks. Reads the saved settings.</summary>
        void PublishPicks();

        void SelectMap(string map);
        void SelectMode(GameMode mode);

        /// <summary>Start the LAN and online browse loops the JOIN screen reads.</summary>
        void Browse();

        /// <summary>The JOIN list's REFRESH: the LAN beacon forgets its rows and asks again, or the
        /// online browser queries now rather than at the end of its interval.</summary>
        void RefreshRooms(bool lan);
        string OnlineBrowserMessage { get; }

        /// <summary>Rooms to list: the LAN beacon's, or the online UGS lobbies. Public rooms only.</summary>
        System.Collections.Generic.List<HubRoom> Rooms(bool lan);

        /// <summary>Show or hide the room's chat, the old lobby's CHAT chip re-homed.</summary>
        void ToggleChat();

        /// <summary>WATCH INSTEAD / TAKE A SEAT, the old board's spectate toggle re-homed.</summary>
        void ToggleSpectate();
        bool Spectating { get; }

        /// <summary>The host's LAN address for joining by IP, or empty online and on a client.</summary>
        string RoomAddress { get; }
    }

    /// <summary>One advertised room, including whether a new player can actually join.</summary>
    public struct HubRoom
    {
        public string Name;
        public string Map;
        public int Players;
        public int Capacity;
        public bool InProgress;
        public bool IsJoinable;

        /// <summary>
        /// The host said a protocol and it is not ours (LAN beacon v3). The row reads DIFFERENT VERSION
        /// where JOIN would be, because approval would only refuse the press.
        /// </summary>
        public bool DifferentVersion;

        /// <summary>What JOIN passes to <see cref="IHubHost.Join"/>: a code, or an address.</summary>
        public string Key;

        /// <summary>What a LAN row from a host that does not say its map (a v1 or v2 beacon) shows.</summary>
        public const string UnknownMap = "UNKNOWN";

        public static HubRoom FromLan(Net.LanEntry entry) => new HubRoom
        {
            Name = entry.HostName, Map = MapName(entry.MapId), Players = entry.Players, Capacity = entry.MaxPlayers,
            InProgress = entry.InProgress, IsJoinable = entry.IsJoinable, DifferentVersion = entry.DifferentVersion,
            Key = $"{entry.Address}:{entry.Port}"
        };

        /// <summary>
        /// A map id as the MAP column shows it: the registry's display name, or the id itself for a map
        /// this build does not have (another version's pool), or <see cref="UnknownMap"/> when unsaid.
        ///
        /// ⚠️ NOT `SceneFlow.PreviewFor`. That falls back to the FIRST map with a warning, so a row from a
        /// build with a map we lack would read TONDO, a map that host is not playing, and log once a second.
        /// </summary>
        public static string MapName(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return UnknownMap;
            foreach (var entry in SceneFlow.MapRegistry)
                if (entry.Id == mapId) return entry.Name;
            return mapId.ToUpperInvariant();
        }

        public bool SameListing(HubRoom other)
            => Key == other.Key && Name == other.Name && Map == other.Map
                && Players == other.Players && Capacity == other.Capacity
                && InProgress == other.InProgress && IsJoinable == other.IsJoinable
                && DifferentVersion == other.DifferentVersion;
    }
}
