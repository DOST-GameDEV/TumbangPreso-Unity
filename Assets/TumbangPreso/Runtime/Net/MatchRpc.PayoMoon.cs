using TumbangPreso.Map;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    /// <summary>
    /// Payo's moon on the wire (`Map.PayoMoon`): ONE message, host to all, and the same message to a peer that has just
    /// arrived. The Arena balloon's pattern and layout (`MatchRpc.ArenaBalloon.cs`), under its own name.
    ///
    /// It carries the moon's shared state (the hits so far, whether a reward is running) and its last event (a hit: whose
    /// slipper, from where, along which line, launched when on the server's clock). Every peer draws the same flight,
    /// reaction and reward from it; the reward's every beat is a function of the revealing throw's launch time, so the
    /// snapshot sends that launch and a late joiner sees Mayari exactly as far along as the room.
    ///
    /// NOTHING IS TAKEN FROM A CLIENT. The host decides a hit from the throw it already resolves (`Carrier.HostThrowAt`)
    /// and what it tells the room changes no score, tag, can or rule. A client accepts it only from the host, at its exact
    /// length, with finite vectors and a count inside the rule's range.
    ///
    /// ⚠️ NO PROTOCOL STEP. A peer built before this message has no handler for the name and Netcode drops an unknown
    /// named message unread (`CustomMessagingManager.InvokeNamedMessage`): such a peer sees the slipper leave and come back
    /// (the slipper's own state, unchanged) and a moon that does not react. Nothing it judges differs from the host.
    /// </summary>
    public sealed partial class MatchRpc
    {
        // hits, kind, revealing, serial, seat, origin, direction, launch.
        private const int PayoMoonBytes = sizeof(byte) * 2 + sizeof(bool) + sizeof(int) * 2 + sizeof(float) * 6 + sizeof(double);

        private static void WritePayoMoon(FastBufferWriter writer, in PayoMoon.Wire state)
        {
            writer.WriteValueSafe(state.Hits);
            writer.WriteValueSafe(state.Kind);
            writer.WriteValueSafe(state.Revealing);
            writer.WriteValueSafe(state.Serial);
            writer.WriteValueSafe(state.Seat);
            writer.WriteValueSafe(state.Origin);
            writer.WriteValueSafe(state.Direction);
            writer.WriteValueSafe(state.Launch);
        }

        /// <summary>HOST: the moon's state and its newest event, to everybody.</summary>
        public void BroadcastPayoMoon(in PayoMoon.Wire state)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(PayoMoonBytes, Allocator.Temp);
            WritePayoMoon(writer, state);
            _nm.CustomMessagingManager.SendNamedMessageToAll("PayoMoon", writer);
        }

        /// <summary>HOST: the moon as it stands, to one arriving peer.</summary>
        private void SendPayoMoonSnapshot(ulong peer)
        {
            var moon = PayoMoon.Instance;
            if (!NetAuthority.IsHost || moon == null || _nm?.CustomMessagingManager == null || peer == _nm.LocalClientId) return;
            using var writer = new FastBufferWriter(PayoMoonBytes, Allocator.Temp);
            WritePayoMoon(writer, moon.HostSnapshot());
            _nm.CustomMessagingManager.SendNamedMessage("PayoMoon", peer, writer);
        }

        private void OnPayoMoonMsg(ulong senderClientId, FastBufferReader reader)
        {
            // The host is its own client: it has already applied what it sent.
            if (NetAuthority.IsHost || !FromHost(senderClientId)) return;
            if (reader.Length - reader.Position != PayoMoonBytes || !reader.TryBeginRead(PayoMoonBytes)) return;

            var state = new PayoMoon.Wire();
            reader.ReadValueSafe(out state.Hits);
            reader.ReadValueSafe(out state.Kind);
            reader.ReadValueSafe(out state.Revealing);
            reader.ReadValueSafe(out state.Serial);
            reader.ReadValueSafe(out state.Seat);
            reader.ReadValueSafe(out state.Origin);
            reader.ReadValueSafe(out state.Direction);
            reader.ReadValueSafe(out state.Launch);

            if (state.Hits > PayoMoon.RevealHits || state.Kind > PayoMoon.Wire.RevealHit ||
                !Finite(state.Origin) || !Finite(state.Direction) || double.IsNaN(state.Launch) || double.IsInfinity(state.Launch) ||
                state.Origin.sqrMagnitude > 200.0f * 200.0f) return;

            // No moon in this scene (another map): nothing to show.
            PayoMoon.Instance?.Apply(state);
        }
    }
}
