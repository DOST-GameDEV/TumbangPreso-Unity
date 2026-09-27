using System;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public struct FamiliarEffectState : INetworkSerializable
    {
        public const int MaxWireBytes = 178;
        public int Seat;
        public GameplayActionScope Scope;
        public long Phase;
        public FixedString64Bytes HeroId, AbilityId;
        public Vector3 Position;
        public double ExpiresAt;
        public float Yaw;

        public bool IsValid => Seat >= 0 && Seat < Balance.PlayerCount && Scope.IsValid && Phase > 0 &&
            HeroId.Length > 0 && AbilityId.Length > 0 && Finite(Position.x) && Finite(Position.y) &&
            Finite(Position.z) && Finite(Yaw) && ExpiresAt >= 0 && !double.IsInfinity(ExpiresAt);

        public bool MatchesKit(HeroKit kit) => kit?.Ultimate != null &&
            HeroId.ToString() == kit.HeroId && AbilityId.ToString() == kit.Ultimate.Id;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Seat);
            serializer.SerializeValue(ref Scope);
            serializer.SerializeValue(ref Phase);
            SerializeId(serializer, ref HeroId);
            SerializeId(serializer, ref AbilityId);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref ExpiresAt);
            serializer.SerializeValue(ref Yaw);
        }

        private static void SerializeId<T>(BufferSerializer<T> serializer, ref FixedString64Bytes id) where T : IReaderWriter
        {
            ushort length = (ushort)id.Length;
            serializer.SerializeValue(ref length);
            if (length > id.Capacity) throw new ArgumentOutOfRangeException(nameof(id));
            if (serializer.IsReader) id.Length = length;
            for (int i = 0; i < length; i++)
            {
                byte value = serializer.IsReader ? (byte)0 : id[i];
                serializer.SerializeValue(ref value);
                if (serializer.IsReader) id[i] = value;
            }
        }

        public static bool TryRead(ref FastBufferReader reader, out FamiliarEffectState state)
        {
            state = default;
            int bytes = reader.Length - reader.Position;
            if (bytes < 56 || bytes > MaxWireBytes) return false;
            try
            {
                reader.ReadNetworkSerializable(out state);
                return reader.Position == reader.Length && state.IsValid;
            }
            catch (OverflowException) { return false; }
            catch (ArgumentException) { return false; }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
