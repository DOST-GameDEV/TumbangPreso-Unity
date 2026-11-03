using Unity.Collections;
using Unity.Netcode;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        /// <summary>One player agreeing to skip the match opening. See `OpeningSkipVote`.</summary>
        public void RequestSkipOpening()
        {
            long identity = EnsurePresentationMatch();
            if (identity <= 0) return;
            if (NetAuthority.IsHost)
            {
                FindFirstObjectByType<OpeningSkipVote>()?.HostCastVote(NetAuthority.LocalPeerId);
                return;
            }
            if (_nm == null || !_nm.IsConnectedClient || _nm.CustomMessagingManager == null) return;
            // The payload identifies the match, never the voter.
            using var writer = new FastBufferWriter(8, Allocator.Temp);
            writer.WriteValueSafe(identity);
            _nm.CustomMessagingManager.SendNamedMessage("SkipOpening", NetworkManager.ServerClientId, writer);
        }

        private void OnSkipOpeningMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost || senderClientId > int.MaxValue ||
                reader.Length - reader.Position != 8 || !reader.TryBeginRead(8)) return;
            reader.ReadValueSafe(out long match);
            if (match <= 0 || match != PresentationMatchId) return;
            FindFirstObjectByType<OpeningSkipVote>()?.HostCastVote((int)senderClientId);
        }

        public void BroadcastOpeningVotes(int votes, int needed, byte mask, ulong? peer = null)
        {
            if (!NetAuthority.IsHost || _nm?.CustomMessagingManager == null) return;
            long identity = EnsurePresentationMatch();
            if (identity <= 0) return;
            using var writer = new FastBufferWriter(17, Allocator.Temp);
            writer.WriteValueSafe(identity);
            writer.WriteValueSafe(votes); writer.WriteValueSafe(needed); writer.WriteValueSafe(mask);
            if (peer.HasValue) _nm.CustomMessagingManager.SendNamedMessage("OpeningVotes", peer.Value, writer, NetworkDelivery.ReliableSequenced);
            else _nm.CustomMessagingManager.SendNamedMessageToAll("OpeningVotes", writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnOpeningVotesMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(senderClientId) || reader.Length - reader.Position != 17 || !reader.TryBeginRead(17)) return;
            reader.ReadValueSafe(out long match);
            reader.ReadValueSafe(out int votes); reader.ReadValueSafe(out int needed); reader.ReadValueSafe(out byte mask);
            if (match <= 0 || match != PresentationMatchId || needed < 1 || needed > Balance.PlayerCount ||
                votes < 0 || votes > needed || (mask & ~((1 << Balance.PlayerCount) - 1)) != 0) return;
            int count = 0;
            for (int seat = 0; seat < Balance.PlayerCount; seat++) if ((mask & (1 << seat)) != 0) count++;
            if (count != votes) return;
            FindFirstObjectByType<OpeningSkipVote>()?.ApplyNetworkTally(votes, needed, mask);
        }
    }
}
