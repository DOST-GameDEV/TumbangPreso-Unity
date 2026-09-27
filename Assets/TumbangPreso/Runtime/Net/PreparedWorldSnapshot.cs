using System;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public struct PreparedWorldSnapshot : INetworkSerializable
    {
        public const int MaxWireBytes = 128;
        public int Seat, Round, Generation;
        public long Match;
        public FixedString64Bytes AbilityId;
        public Vector3 Centre;
        public float Preparation, Remaining, RoundClock;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Seat);
            serializer.SerializeValue(ref Round);
            serializer.SerializeValue(ref Centre);
            serializer.SerializeValue(ref Preparation);
            serializer.SerializeValue(ref Remaining);
            serializer.SerializeValue(ref Match);
            serializer.SerializeValue(ref Generation);
            serializer.SerializeValue(ref RoundClock);
            ushort length = (ushort)AbilityId.Length;
            serializer.SerializeValue(ref length);
            if (length > AbilityId.Capacity) throw new ArgumentOutOfRangeException(nameof(AbilityId));
            if (serializer.IsReader) AbilityId.Length = length;
            for (int i = 0; i < length; i++)
            {
                byte value = serializer.IsReader ? (byte)0 : AbilityId[i];
                serializer.SerializeValue(ref value);
                if (serializer.IsReader) AbilityId[i] = value;
            }
        }

        public bool TryAge(float now, float maxPreparation, float maxRemaining, out float preparation, out float remaining)
        {
            preparation = remaining = 0;
            if (!Finite(Centre.x) || !Finite(Centre.y) || !Finite(Centre.z)
                || !Clock(RoundClock) || !Clock(now) || !Finite(Preparation) || !Finite(Remaining)
                || !Finite(maxPreparation) || !Finite(maxRemaining) || maxPreparation < 0 || maxRemaining < 0
                || Preparation < 0 || Preparation > maxPreparation || Remaining < 0 || Remaining > maxRemaining) return false;
            float age = Mathf.Max(0, RoundClock - now);
            preparation = Mathf.Max(0, Preparation - age);
            remaining = Mathf.Max(0, Remaining - Mathf.Max(0, age - Preparation));
            return true;
        }

        public static bool TryRead(ref FastBufferReader reader, out PreparedWorldSnapshot snapshot)
        {
            snapshot = default;
            int bytes = reader.Length - reader.Position;
            if (bytes < 47 || bytes > MaxWireBytes) return false;
            try
            {
                reader.ReadNetworkSerializable(out snapshot);
                return reader.Position == reader.Length && snapshot.AbilityId.Length > 0
                    && snapshot.Seat >= 0 && snapshot.Seat < Balance.PlayerCount
                    && snapshot.Match > 0 && snapshot.Round >= 0 && snapshot.Generation > 0;
            }
            catch (OverflowException) { return false; }
            catch (ArgumentException) { return false; }
        }

        private static bool Clock(float value) => Finite(value) && value >= 0 && value <= CustomGameRules.MaxRoundSeconds;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
