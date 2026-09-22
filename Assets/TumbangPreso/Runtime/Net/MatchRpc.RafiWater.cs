using System;
using TumbangPreso.Abilities;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        // Optional bounded geometry belongs to water fields only. Existing field
        // payloads keep their order; protocol49 rejects older readers at admission.
        private static void WriteWaterExtra(FastBufferWriter writer, WorldEffectSnapshot.Field f)
        {
            if (!RafiWaterField.IsWater(f.Type)) return;
            writer.WriteValueSafe(f.EventId); writer.WriteValueSafe(f.Path.Length);
            foreach (var point in f.Path) writer.WriteValueSafe(point);
        }
        private static bool ReadWaterExtra(ref FastBufferReader reader, WorldEffectSnapshot.Kind kind,
            out int eventId, out Vector3[] path)
        {
            eventId = 0; path = Array.Empty<Vector3>();
            if (!RafiWaterField.IsWater(kind)) return true;
            if (!reader.TryBeginRead(8)) return false;
            reader.ReadValueSafe(out eventId); reader.ReadValueSafe(out int count);
            if (eventId <= 0 || count < 0 || count > RafiWaterField.MaxPathPoints || !reader.TryBeginRead(count * 12)) return false;
            path = new Vector3[count];
            for (int i = 0; i < count; i++) reader.ReadValueSafe(out path[i]);
            return true;
        }

        public void BroadcastRafiWater(WorldEffectSnapshot.Field f)
        {
            if (!NetAuthority.IsHost || _nm?.CustomMessagingManager == null || GameServices.Match == null
                || !RafiWaterField.IsWater(f.Type) || !WorldEffectSnapshot.Valid(f)) return;
            using var writer = new FastBufferWriter(512, Allocator.Temp);
            writer.WriteValueSafe(PresentationMatchId); writer.WriteValueSafe(GameServices.Match.RoundNumber);
            writer.WriteValueSafe((float)_nm.ServerTime.Time);
            writer.WriteValueSafe((int)f.Type); writer.WriteValueSafe(f.Position); writer.WriteValueSafe(f.Forward);
            writer.WriteValueSafe(f.Duration); writer.WriteValueSafe(f.Remaining); writer.WriteValueSafe(f.Radius);
            writer.WriteValueSafe(f.Owner); writer.WriteValueSafe(f.FirstScale); writer.WriteValueSafe(f.SecondScale); writer.WriteValueSafe(f.Split);
            WriteWaterExtra(writer, f);
            foreach (var peer in _nm.ConnectedClientsIds)
                if (peer != _nm.LocalClientId) _nm.CustomMessagingManager.SendNamedMessage("RafiWater", peer, writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnRafiWaterMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender) || !reader.TryBeginRead(73)) return;
            reader.ReadValueSafe(out long epoch); reader.ReadValueSafe(out int round); reader.ReadValueSafe(out float sentAt);
            if (epoch != PresentationMatchId || GameServices.Match?.RoundNumber != round || !Finite(sentAt)
                || GameServices.Round == null || !GameServices.Round.RoundActive) return;
            reader.ReadValueSafe(out int kind); reader.ReadValueSafe(out Vector3 position); reader.ReadValueSafe(out Vector3 forward);
            reader.ReadValueSafe(out float duration); reader.ReadValueSafe(out float remaining); reader.ReadValueSafe(out float radius);
            reader.ReadValueSafe(out int owner); reader.ReadValueSafe(out float speed); reader.ReadValueSafe(out float alternate); reader.ReadValueSafe(out bool split);
            if (!RafiWaterField.IsWater((WorldEffectSnapshot.Kind)kind)
                || !ReadWaterExtra(ref reader, (WorldEffectSnapshot.Kind)kind, out int eventId, out var path)) return;
            var f = new WorldEffectSnapshot.Field { Type = (WorldEffectSnapshot.Kind)kind, EventId = eventId,
                Position = position, Forward = forward, Duration = duration, Remaining = remaining, Radius = radius,
                Owner = owner, FirstScale = speed, SecondScale = alternate, Split = split, Path = path };
            if (WorldEffectSnapshot.Valid(f)) RafiWaterField.Restore(f, Mathf.Max(0, (float)_nm.ServerTime.Time - sentAt));
        }
    }
}
