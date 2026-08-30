using System;
using TumbangPreso.Core;

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

        /// <summary>
        /// The banner this seat is wearing, **already authorised by the host**.
        ///
        /// ⚠️⚠️ IT IS THE RESULT, NOT THE CLAIM, AND THAT IS THE WHOLE ARRANGEMENT. A peer sends
        /// what it wants to wear plus the XP and mastery that would authorise it; the host runs
        /// `BannerRules.Authorise` once and puts the answer here. **Every screen in the room then
        /// draws the same banner, because one machine decided it**, and nothing downstream has to
        /// know the difference between a claim and a right.
        ///
        /// ⚠️ SAME RULE AS <see cref="Ready"/> one field up: a peer never writes its own row in
        /// this table. `docs/TODO.md` § 54 records what trusting a peer-supplied field cost, and
        /// § 101 is the entry for this one.
        ///
        /// ⚠️ NEVER NULL. A seat with nothing equipped carries an empty selection, which is the
        /// state every account starts in and has to draw as "no decoration" rather than as a hole.
        /// </summary>
        public BannerSelection Banner = new BannerSelection();

        /// <summary>
        /// The character palette this seat is drawn in, authorised the same way.
        ///
        /// ⚠️⚠️ WITHOUT THIS EVERY REMOTE SEAT WORE ITS AUTHORED COLOURS AND `MatchInstaller`
        /// SAID SO IN A COMMENT: *"a remote peer's choice has to arrive over the wire before it
        /// can be drawn, and it does not yet"*. This is that field. ⚠️ **Guessing a remote peer's
        /// palette from this machine's settings would dress a stranger in the local player's
        /// choice**, which is why it was left blank rather than defaulted.
        /// </summary>
        public string PaletteId = "";
    }
}
