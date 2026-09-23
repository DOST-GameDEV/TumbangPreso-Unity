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

    /// <summary>One joinable room, as the JOIN screen's shared row draws it.</summary>
    public struct HubRoom
    {
        public string Name;
        public string Map;
        public int Players;
        public int Capacity;
        public bool InProgress;

        /// <summary>What JOIN passes to <see cref="IHubHost.Join"/>: a code, or an address.</summary>
        public string Key;
    }
}
