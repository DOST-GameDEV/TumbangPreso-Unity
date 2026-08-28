using System;

namespace TumbangPreso.Net
{
    /// <summary>
    /// Replicated seat state across peers (N4, N8).
    ///
    /// ⚠️ AUTHORITATIVE HOST BROADCAST TABLE. Clients read this instead of querying an unpopulated
    /// client-side LobbySession. Carries human occupancy, player display name, and picked
    /// character, can, and slipper skins so MatchInstaller and ConvertedMatchSetup can render
    /// accurate seat representations without waiting for in-match RPC synchronization.
    /// </summary>
    [Serializable]
    public sealed class LobbySeatInfo
    {
        public int Seat;
        public int PeerId = -1;
        public string Name = "";
        public bool Occupied;
        public bool Spectator;
        public int CharacterPick = -1;
        public int CanPick = -1;
        public int SlipperPick = -1;

        /// <summary>
        /// Whether this seat has pressed READY.
        ///
        /// ⚠️⚠️ THE TALLY IS A COUNT AND THE LOBBY NEEDED A NAME. `MatchRpc.OnLobbyReadyChanged`
        /// carries (ready, expected), so every peer knew HOW MANY were ready and nobody knew
        /// WHICH, which is fine for a number on a button and useless for a tick over somebody's
        /// head. The PUBG-style lobby draws the answer per person, so the answer has to travel per
        /// person.
        ///
        /// ⚠️ IT RIDES `SyncLobbyPicks`, WHICH ALREADY GOES OUT ON EVERY SEAT CHANGE, rather than
        /// becoming a fifth message about readiness. `docs/TODO.md` § 38.5 found three verbs with
        /// two protocols each and the dead one being the maintained one; adding a message for a
        /// bool that an existing broadcast already has a natural place for is how that starts.
        ///
        /// ⚠️ AND IT IS HOST-AUTHORED. A peer never writes its own readiness into a table; it
        /// presses `DeclareReady` and the host decides what the table says. Same rule as the name
        /// and the seat, and § 54 records what trusting a peer-supplied field cost.
        /// </summary>
        public bool Ready;
    }
}
