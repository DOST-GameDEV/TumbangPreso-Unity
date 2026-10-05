using TumbangPreso.Map;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The Arena's slipper balloon on the wire (`Map.ArenaBalloon`): ONE message, host to all,
    /// and the same message to a peer that has just arrived.
    ///
    /// It carries the whole of the balloon's shared state (the hits so far, whether it is
    /// popped) and its last event (a hit: whose slipper, from where, along which line, launched
    /// when on the server's clock; or a re-inflation), so every peer draws the same flight,
    /// reaction and pop, and a late joiner sees the count and a popped balloon as they stand.
    ///
    /// WHY A MESSAGE AND NOT THE SLIPPER'S OWN STATE. A slipper that has left for the balloon is
    /// out of play (switched off, as on a map recovery), and an out-of-play slipper travels as
    /// Loose with no affinity (`SyncSlipperClientRpc`), so nothing on it can say "balloon"; and
    /// no slipper can carry the count or the popped state for a peer that joins later. `NetCue`
    /// carries a sound and a place, not a number. Protocol 146 (this same change set) covers it.
    ///
    /// NOTHING IS TAKEN FROM A CLIENT. There is no client message: the host decides a hit from
    /// the throw it already resolves (`Carrier.HostThrowAt`), and what it tells the room changes
    /// no score, tag, can or rule. A client's handler accepts it only from the host, at its exact
    /// length, with finite vectors and a count inside the rule's range.
    /// </summary>
    public sealed partial class MatchRpc
    {
        // hits, kind, popped, serial, seat, origin, direction, launch.
        private const int ArenaBalloonBytes = sizeof(byte) * 2 + sizeof(bool) + sizeof(int) * 2 + sizeof(float) * 6 + sizeof(double);

        // The writer is a handle: a copy writes into the same buffer.
        private static void WriteArenaBalloon(FastBufferWriter writer, in ArenaBalloon.Wire state)
        {
            writer.WriteValueSafe(state.Hits);
            writer.WriteValueSafe(state.Kind);
            writer.WriteValueSafe(state.Popped);
            writer.WriteValueSafe(state.Serial);
            writer.WriteValueSafe(state.Seat);
            writer.WriteValueSafe(state.Origin);
            writer.WriteValueSafe(state.Direction);
            writer.WriteValueSafe(state.Launch);
        }

        /// <summary>HOST: the balloon's state and its newest event, to everybody.</summary>
        public void BroadcastArenaBalloon(in ArenaBalloon.Wire state)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(ArenaBalloonBytes, Allocator.Temp);
            WriteArenaBalloon(writer, state);
            _nm.CustomMessagingManager.SendNamedMessageToAll("ArenaBalloon", writer);
        }

        /// <summary>HOST: the balloon as it stands, to one arriving peer. No event: it replays nothing.</summary>
        private void SendArenaBalloonSnapshot(ulong peer)
        {
            var balloon = ArenaBalloon.Instance;
            if (!NetAuthority.IsHost || balloon == null || _nm?.CustomMessagingManager == null || peer == _nm.LocalClientId) return;
            using var writer = new FastBufferWriter(ArenaBalloonBytes, Allocator.Temp);
            WriteArenaBalloon(writer, balloon.HostSnapshot());
            _nm.CustomMessagingManager.SendNamedMessage("ArenaBalloon", peer, writer);
        }

        private void OnArenaBalloonMsg(ulong senderClientId, FastBufferReader reader)
        {
            // The host is its own client: it has already applied what it sent (§ THE LOOPBACK).
            if (NetAuthority.IsHost || !FromHost(senderClientId)) return;
            if (reader.Length - reader.Position != ArenaBalloonBytes || !reader.TryBeginRead(ArenaBalloonBytes)) return;

            var state = new ArenaBalloon.Wire();
            reader.ReadValueSafe(out state.Hits);
            reader.ReadValueSafe(out state.Kind);
            reader.ReadValueSafe(out state.Popped);
            reader.ReadValueSafe(out state.Serial);
            reader.ReadValueSafe(out state.Seat);
            reader.ReadValueSafe(out state.Origin);
            reader.ReadValueSafe(out state.Direction);
            reader.ReadValueSafe(out state.Launch);

            if (state.Hits > ArenaBalloon.PopHits || state.Kind > ArenaBalloon.Wire.Reinflate ||
                !Finite(state.Origin) || !Finite(state.Direction) || double.IsNaN(state.Launch) || double.IsInfinity(state.Launch) ||
                state.Origin.sqrMagnitude > 200.0f * 200.0f) return;

            // No balloon in this scene (another map, or a scene built before it): nothing to show.
            ArenaBalloon.Instance?.Apply(state);
        }
    }
}
