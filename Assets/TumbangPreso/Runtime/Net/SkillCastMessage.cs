using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TumbangPreso.Core;

namespace TumbangPreso.Net
{
    // One layout for requests and accepted playback. Identity and command intent
    // are gameplay facts; model, clip, effect and display names stay off the wire.
    public struct SkillCastMessage : INetworkSerializable
    {
        public const int MaxWireBytes = 192;
        public int Seat, Slot, Round;
        public FixedString64Bytes AbilityId;
        public Vector3 Position, Forward, AimPoint, FamiliarPosition;
        public float HeldSeconds;
        public bool HasFamiliar, Reactivation;
        public long Match, Request, Event, FlightIntent;
        public long AimToken;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Seat);
            serializer.SerializeValue(ref Slot);
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
            serializer.SerializeValue(ref Reactivation);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Forward);
            serializer.SerializeValue(ref AimPoint);
            serializer.SerializeValue(ref HeldSeconds);
            serializer.SerializeValue(ref HasFamiliar);
            serializer.SerializeValue(ref FamiliarPosition);
            serializer.SerializeValue(ref Match);
            serializer.SerializeValue(ref Round);
            serializer.SerializeValue(ref Request);
            serializer.SerializeValue(ref Event);
            serializer.SerializeValue(ref FlightIntent);
            serializer.SerializeValue(ref AimToken);
        }

        public bool IsValid(bool accepted)
            => Seat >= 0 && Seat < Balance.PlayerCount && Slot >= 0 && Slot <= 2
                && AbilityId.Length > 0 && Match > 0 && Round >= 0
                && (accepted ? Event > 0 && Request >= 0 : Event == 0 && Request > 0)
                && FlightIntent != long.MinValue && AimToken >= 0 && Finite(Position) && Finite(Forward)
                && Finite(AimPoint) && Finite(HeldSeconds) && HeldSeconds >= 0
                && (!HasFamiliar || Finite(FamiliarPosition));

        public static bool TryRead(ref FastBufferReader reader, out SkillCastMessage cast)
        {
            cast = default;
            int bytes = reader.Length - reader.Position;
            if (bytes < 108 || bytes > MaxWireBytes) return false;
            try
            {
                reader.ReadNetworkSerializable(out cast);
                return reader.Position == reader.Length;
            }
            catch (OverflowException) { return false; }
            catch (ArgumentException) { return false; }
        }

        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
