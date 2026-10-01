using System;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public struct TimedKitState : INetworkSerializable
    {
        public const int MaxWireBytes = 231;
        public int Seat;
        public GameplayActionScope Scope;
        public long Sequence;
        public FixedString64Bytes HeroId, PersonalId, UltimateId;
        public float PersonalRemaining, UltimateRemaining;
        public float RoundClock;
        public bool UltimatePending;
        public bool UltimatePermanent;

        public static TimedKitState Capture(HeroKit kit, TimedKitSnapshot state, int seat,
            GameplayActionScope scope, long sequence, float roundClock) => new TimedKitState
        {
            Seat = seat, Scope = scope, Sequence = sequence, RoundClock = roundClock,
            HeroId = new FixedString64Bytes(kit.HeroId),
            PersonalId = new FixedString64Bytes(state.PersonalAbility?.Id ?? ""),
            UltimateId = new FixedString64Bytes(state.UltimateAbility?.Id ?? ""),
            PersonalRemaining = state.PersonalRemaining, UltimateRemaining = state.UltimateRemaining,
            UltimatePending = state.UltimatePending,
            UltimatePermanent = state.UltimatePermanent,
        };

        public bool IsValid => Seat >= 0 && Seat < Balance.PlayerCount && Scope.IsValid && Sequence > 0 &&
            HeroId.Length > 0 && (PersonalId.Length > 0 || UltimateId.Length > 0) &&
            (PersonalId.Length == 0 || UltimateId.Length == 0 || !PersonalId.Equals(UltimateId)) &&
            NonNegative(PersonalRemaining) && NonNegative(UltimateRemaining) &&
            (PersonalId.Length > 0 || PersonalRemaining == 0) &&
            (UltimateId.Length > 0 || (UltimateRemaining == 0 && !UltimatePending && !UltimatePermanent)) &&
            (!UltimatePermanent || (!UltimatePending && UltimateRemaining == 0)) &&
            Clock(RoundClock);

        public bool TryResolve(HeroKit kit, float now, out TimedKitSnapshot state)
        {
            state = default;
            if (!IsValid || !Clock(now) ||
                kit == null || HeroId.ToString() != kit.HeroId || !(kit is ITimedKitReplication replication)) return false;
            var binding = replication.CaptureTimedKit();
            if (PersonalId.ToString() != (binding.PersonalAbility?.Id ?? "") ||
                UltimateId.ToString() != (binding.UltimateAbility?.Id ?? "")) return false;
            return binding.TryAge(PersonalRemaining, UltimateRemaining, UltimatePending,
                Math.Max(0, RoundClock - now), out state, UltimatePermanent);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Seat);
            serializer.SerializeValue(ref Scope);
            serializer.SerializeValue(ref Sequence);
            SerializeId(serializer, ref HeroId);
            SerializeId(serializer, ref PersonalId);
            SerializeId(serializer, ref UltimateId);
            serializer.SerializeValue(ref PersonalRemaining);
            serializer.SerializeValue(ref UltimateRemaining);
            serializer.SerializeValue(ref RoundClock);
            byte pending = UltimatePending ? (byte)1 : (byte)0;
            serializer.SerializeValue(ref pending);
            if (pending > 1) throw new ArgumentOutOfRangeException(nameof(UltimatePending));
            if (serializer.IsReader) UltimatePending = pending != 0;
            byte permanent = UltimatePermanent ? (byte)1 : (byte)0;
            serializer.SerializeValue(ref permanent);
            if (permanent > 1) throw new ArgumentOutOfRangeException(nameof(UltimatePermanent));
            if (serializer.IsReader) UltimatePermanent = permanent != 0;
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

        public static bool TryRead(ref FastBufferReader reader, out TimedKitState state)
        {
            state = default;
            int bytes = reader.Length - reader.Position;
            if (bytes < 48 || bytes > MaxWireBytes) return false;
            try
            {
                reader.ReadNetworkSerializable(out state);
                return reader.Position == reader.Length && state.IsValid;
            }
            catch (OverflowException) { return false; }
            catch (ArgumentException) { return false; }
        }

        private static bool NonNegative(float value) => value >= 0 && !float.IsInfinity(value);
        private static bool Clock(float value) => value >= 0 && value <= CustomGameRules.MaxRoundSeconds;
    }
}
