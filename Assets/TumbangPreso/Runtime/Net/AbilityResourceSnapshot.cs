using System;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public struct AbilityResourceSnapshot : INetworkSerializable
    {
        public const int MaxAbilities = 8;
        public const int MaxWireBytes = 704;
        public GameplayActionScope Scope;
        public int Seat;
        public long Sequence;
        public FixedString64Bytes HeroId;
        public float UltimateCharge;
        public Entry[] Abilities;

        public struct Entry
        {
            public FixedString64Bytes Id;
            public float Cooldown;
            public int Charges;
        }

        public static AbilityResourceSnapshot Capture(HeroKit kit, int seat, GameplayActionScope scope, long sequence)
        {
            var abilities = kit.AllAbilities;
            var entries = new Entry[abilities.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                var ability = abilities[i];
                if (ability == null) return default;
                entries[i] = new Entry { Id = new FixedString64Bytes(ability.Id),
                    Cooldown = ability.CooldownRemaining, Charges = ability.ChargesRemaining };
            }
            return new AbilityResourceSnapshot { Seat = seat, Scope = scope, Sequence = sequence,
                HeroId = new FixedString64Bytes(kit.HeroId), UltimateCharge = kit.UltimateCharge, Abilities = entries };
        }

        public bool IsValid
        {
            get
            {
                if (Seat < 0 || Seat >= Balance.PlayerCount || !Scope.IsValid || Sequence <= 0 ||
                    HeroId.Length == 0 || !NonNegative(UltimateCharge) || Abilities == null ||
                    Abilities.Length < 1 || Abilities.Length > MaxAbilities) return false;
                for (int i = 0; i < Abilities.Length; i++)
                {
                    var entry = Abilities[i];
                    if (entry.Id.Length == 0 || !NonNegative(entry.Cooldown) || entry.Charges < 0) return false;
                    for (int j = 0; j < i; j++) if (entry.Id.Equals(Abilities[j].Id)) return false;
                }
                return true;
            }
        }

        public bool TryApply(HeroKit kit, bool mayLower)
        {
            if (!IsValid || kit == null || HeroId.ToString() != kit.HeroId) return false;
            var local = kit.AllAbilities;
            if (local.Length != Abilities.Length) return false;
            // Validate the complete identity set before changing any resource.
            foreach (var entry in Abilities)
            {
                string id = entry.Id.ToString();
                bool found = false;
                foreach (var ability in local)
                    if (ability != null && ability.Id == id) { found = true; break; }
                if (!found) return false;
            }
            kit.ApplyNetworkUltimateCharge(UltimateCharge);
            foreach (var entry in Abilities)
            {
                string id = entry.Id.ToString();
                foreach (var ability in local)
                    if (ability.Id == id)
                    { ability.ApplyNetworkSnapshot(entry.Cooldown, entry.Charges, mayLower); break; }
            }
            return true;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Seat);
            serializer.SerializeValue(ref Scope);
            serializer.SerializeValue(ref Sequence);
            SerializeId(serializer, ref HeroId);
            serializer.SerializeValue(ref UltimateCharge);
            byte count = (byte)(Abilities?.Length ?? 0);
            serializer.SerializeValue(ref count);
            if (count < 1 || count > MaxAbilities) throw new ArgumentOutOfRangeException(nameof(Abilities));
            if (serializer.IsReader) Abilities = new Entry[count];
            for (int i = 0; i < count; i++)
            {
                SerializeId(serializer, ref Abilities[i].Id);
                serializer.SerializeValue(ref Abilities[i].Cooldown);
                serializer.SerializeValue(ref Abilities[i].Charges);
            }
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

        public static bool TryRead(ref FastBufferReader reader, out AbilityResourceSnapshot snapshot)
        {
            snapshot = default;
            int bytes = reader.Length - reader.Position;
            if (bytes < 35 || bytes > MaxWireBytes) return false;
            try
            {
                reader.ReadNetworkSerializable(out snapshot);
                return reader.Position == reader.Length && snapshot.IsValid;
            }
            catch (OverflowException) { return false; }
            catch (ArgumentException) { return false; }
        }

        private static bool NonNegative(float value) => value >= 0 && !float.IsInfinity(value);
    }
}
