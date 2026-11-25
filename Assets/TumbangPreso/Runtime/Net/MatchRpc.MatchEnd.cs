using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        // The host's final whistle (`MatchEndBeat`): this match's identity, the round it ended on, the host's start on the
        // shared clock and the highlights it chose (a count, then that many clip ids; protocol 195). Sent once at the
        // whistle and again inside every late joiner's snapshot (`BroadcastWorldSnapshot`), on the same reliable stream as
        // `SyncWorld`, so a peer learns the terminal state and the deadline in order.
        private const int MatchEndBytes = sizeof(long) + sizeof(int) + sizeof(double) + sizeof(byte);

        public void BroadcastMatchEnd()
        {
            var beat = MatchEndBeat.Instance;
            var match = GameServices.Match;
            if (!NetAuthority.IsHost || _nm?.CustomMessagingManager == null || beat == null || !beat.Active || match == null
                || beat.MatchId != match.PresentationMatchId || beat.MatchId != PresentationMatchId) return;

            using var writer = new FastBufferWriter(MatchEndBytes + beat.ClipCount * sizeof(long), Allocator.Temp);
            writer.WriteValueSafe(beat.MatchId);
            writer.WriteValueSafe(beat.FinalRound);
            writer.WriteValueSafe(beat.Began);
            writer.WriteValueSafe((byte)beat.ClipCount);
            for (int i = 0; i < beat.ClipCount; i++) writer.WriteValueSafe(beat.ClipAt(i));
            _nm.CustomMessagingManager.SendNamedMessageToAll("MatchEnd", writer);
        }

        private void OnMatchEndMsg(ulong senderClientId, FastBufferReader reader)
        {
            int left = reader.Length - reader.Position;
            if (NetAuthority.IsHost || !FromHost(senderClientId) || left < MatchEndBytes || !reader.TryBeginRead(MatchEndBytes)) return;
            reader.ReadValueSafe(out long match);
            reader.ReadValueSafe(out int finalRound);
            reader.ReadValueSafe(out double began);
            reader.ReadValueSafe(out byte count);
            if (count > MatchEndBeat.MaxClips || left != MatchEndBytes + count * sizeof(long) || !reader.TryBeginRead(count * sizeof(long))) return;
            var clips = new List<long>(count);
            for (int i = 0; i < count; i++) { reader.ReadValueSafe(out long clip); clips.Add(clip); }
            MatchEndBeat.Ensure()?.Receive(match, finalRound, began, clips);
        }
    }
}
