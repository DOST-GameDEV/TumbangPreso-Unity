namespace TumbangPreso.Core
{
    /// <summary>
    /// ⚠️⚠️ A BODY THAT IS NOT A PLAYER GETS A SEAT OF ITS OWN (HERO-10 v3, Phaister's VOODOO DOLL; Nemu's KURO PLAYS is the same
    /// kind of body). The owner, 2026-09-28, of the doll while she attacks: *"Own slipper, throws"*: a real fifth player with its
    /// own slipper. The game names every body and every slipper by seat (a pose, a holder, a slipper's seat of origin, a throw),
    /// so the doll is given a seat rather than borrowing hers: a COMPANION SEAT, <see cref="Balance.PlayerCount"/> plus its
    /// owner's seat. One number is then one body on every peer, in every message that already carries a seat, and the owner is
    /// read straight off it.
    ///
    ///  * Seats 0 to 3 are the four players; 4 to 7 are their companions, at most one each.
    ///  * Points a companion earns are its owner's (*"The doll gives points gained to Phaister"*): `MatchDirector.AddScore`
    ///    maps through <see cref="OwnerOf"/>, so nothing that awards a point has to know companions exist.
    ///  * A companion is never a player: no chip, no result row, no rating. Loops over the four players stay four.
    /// </summary>
    public static class CompanionSeats
    {
        /// <summary>The first companion seat.</summary>
        public const int First = Balance.PlayerCount;

        /// <summary>Every seat a body can have: the players and one companion each.</summary>
        public const int BodyCount = Balance.PlayerCount * 2;

        /// <summary>The companion seat that belongs to <paramref name="owner"/>, or -1 for a seat that cannot own one.</summary>
        public static int For(int owner) => IsPlayer(owner) ? First + owner : -1;

        /// <summary>A player's seat.</summary>
        public static bool IsPlayer(int seat) => seat >= 0 && seat < Balance.PlayerCount;

        /// <summary>A companion's seat.</summary>
        public static bool IsCompanion(int seat) => seat >= First && seat < BodyCount;

        /// <summary>Any body's seat.</summary>
        public static bool IsBody(int seat) => seat >= 0 && seat < BodyCount;

        /// <summary>Whose points a seat earns: its own for a player, its owner's for a companion, -1 for anything else.</summary>
        public static int OwnerOf(int seat) => IsPlayer(seat) ? seat : IsCompanion(seat) ? seat - First : -1;
    }
}
