using System;
using TumbangPreso.Core;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public struct VoodooBodySnapshot : INetworkSerializable
    {
        public const int WireBytes = 27;
        public float Drained, Hexed, MarkAge, ReachElapsed;
        public byte MarkKind, ReachKind;
        public int MarkSource, ReachTarget;
        public bool ReachSucceeded;

        public static VoodooBodySnapshot Capture(CharacterMotor body) => new VoodooBodySnapshot
        {
            Drained = body.DrainedLeft, Hexed = body.HexedLeft,
            MarkKind = (byte)body.VoodooMark, MarkSource = body.VoodooMarkSource, MarkAge = body.VoodooMarkAge,
            ReachKind = (byte)body.VoodooReachKind, ReachTarget = body.VoodooReachTarget,
            ReachElapsed = body.VoodooReachElapsed, ReachSucceeded = body.VoodooReachSucceeded
        };

        private static bool Timer(float value, float maximum) => !float.IsNaN(value) && !float.IsInfinity(value)
            && value >= 0 && value <= maximum + .01f;
        private static bool Seat(int value) => value >= 0 && value < Balance.PlayerCount;
        public bool IsValid(int subject) => Seat(subject)
            && Timer(Drained, StatusRules.DrainedSeconds) && Timer(Hexed, StatusRules.HexedSeconds)
            && Timer(MarkAge, VoodooRules.HexMarkLifeSeconds) && Timer(ReachElapsed, VoodooRules.ReachSeconds)
            && MarkKind <= (byte)VoodooMarkKind.Hex && ReachKind <= (byte)VoodooMarkKind.Hex
            && (MarkKind == 0 ? MarkSource == -1 && MarkAge == 0 : Seat(MarkSource) && MarkSource != subject)
            && (ReachKind == 0 ? ReachTarget == -1 && ReachElapsed == 0
                : Seat(ReachTarget) && ReachTarget != subject && !ReachSucceeded);

        public void Apply(CharacterMotor body)
        {
            if (body == null || !IsValid(body.PlayerSlot)) return;
            body.ApplyNetworkVoodoo(Drained, Hexed, MarkKind, MarkSource, MarkAge,
                ReachKind, ReachTarget, ReachElapsed, ReachSucceeded);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Drained); serializer.SerializeValue(ref Hexed);
            serializer.SerializeValue(ref MarkKind); serializer.SerializeValue(ref MarkSource); serializer.SerializeValue(ref MarkAge);
            serializer.SerializeValue(ref ReachKind); serializer.SerializeValue(ref ReachTarget); serializer.SerializeValue(ref ReachElapsed);
            serializer.SerializeValue(ref ReachSucceeded);
        }

        public static bool TryRead(ref FastBufferReader reader, int subject, out VoodooBodySnapshot state)
        {
            state = default;
            if (!reader.TryBeginRead(WireBytes)) return false;
            try
            {
                reader.ReadNetworkSerializable(out state);
                return state.IsValid(subject);
            }
            catch (OverflowException) { return false; }
            catch (ArgumentException) { return false; }
        }
    }
}
