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
    }
}
