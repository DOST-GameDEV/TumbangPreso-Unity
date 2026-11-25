using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        // The host's final whistle (`MatchEndBeat`): this match's identity, the round it ended on and the host's start on
        // the shared clock. Sent once at the whistle and again inside every late joiner's snapshot (`BroadcastWorldSnapshot`),
        // on the same reliable stream as `SyncWorld`, so a peer learns the terminal state and the deadline in order.
        private const int MatchEndBytes = sizeof(long) + sizeof(int) + sizeof(double);

        public void BroadcastMatchEnd()
        {
            var beat = MatchEndBeat.Instance;
            var match = GameServices.Match;
            if (!NetAuthority.IsHost || _nm?.CustomMessagingManager == null || beat == null || !beat.Active || match == null
                || beat.MatchId != match.PresentationMatchId || beat.MatchId != PresentationMatchId) return;

            using var writer = new FastBufferWriter(MatchEndBytes, Allocator.Temp);
            writer.WriteValueSafe(beat.MatchId);
            writer.WriteValueSafe(beat.FinalRound);
            writer.WriteValueSafe(beat.Began);
            _nm.CustomMessagingManager.SendNamedMessageToAll("MatchEnd", writer);
        }

        private void OnMatchEndMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(senderClientId) || reader.Length - reader.Position != MatchEndBytes
                || !reader.TryBeginRead(MatchEndBytes)) return;
            reader.ReadValueSafe(out long match);
            reader.ReadValueSafe(out int finalRound);
            reader.ReadValueSafe(out double began);
            MatchEndBeat.Ensure()?.Receive(match, finalRound, began);
        }
    }
}
