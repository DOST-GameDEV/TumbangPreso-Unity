using System;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    // Body tells only. Private aiming destinations never travel in this state.
    public struct AbilityAimSnapshot : INetworkSerializable
    {
        public const int MaxWireBytes = 76;
        public byte Slot; // 0 none, 1/2 skills, 3 ultimate
        public FixedString64Bytes AbilityId;
        public float Held;
        public long Token;

        public bool IsValid => Slot <= 3 && !float.IsNaN(Held) && !float.IsInfinity(Held) && Held >= 0 && Held <= 30
            && (Slot == 0 ? AbilityId.Length == 0 && Held == 0 && Token == 0 : AbilityId.Length > 0 && Token > 0);

        public static long MakeToken(int epoch, uint sequence) => ((long)epoch << 32) | sequence;
        public static bool MatchesEpoch(long token, int epoch) => token > 0 && (token >> 32) == epoch;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Slot);
            serializer.SerializeValue(ref Held);
            serializer.SerializeValue(ref Token);
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

        public static bool TryRead(ref FastBufferReader reader, out AbilityAimSnapshot aim)
        {
            aim = default;
            int bytes = reader.Length - reader.Position;
            if (bytes < 15 || bytes > MaxWireBytes) return false;
            try
            {
                reader.ReadNetworkSerializable(out aim);
                return reader.Position == reader.Length && aim.IsValid;
            }
            catch (OverflowException) { return false; }
            catch (ArgumentException) { return false; }
        }
    }
}
