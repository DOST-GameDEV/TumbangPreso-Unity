using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    // Cosmetic camera data. Never feeds a motor, aim request, contact or score.
    public struct SpectatorViewState
    {
        public const int MinWireBytes = 111, MaxWireBytes = 686;
        public const byte FirstPerson = 1, HandsVisible = 2, BotView = 4;
        public long Match;
        public int Round, Slot, Epoch;
        public uint Sequence;
        public Vector3 Eye;
        public Quaternion Rotation;
        public float Fov, EyeDrop, SentAt;
        public Vector3 HandsSeat;
        public Quaternion HandsRotation;
        public FixedString64Bytes Action, Aim;
        public float ActionTime, VisualPhase;
        public uint ActionSequence;
        public byte Flags;
        public SpectatorPrivateView Private;

        public bool PoseIsValid => Finite(Eye.x) && Finite(Eye.y) && Finite(Eye.z)
            && Mathf.Abs(Eye.x) < 100000 && Mathf.Abs(Eye.y) < 100000 && Mathf.Abs(Eye.z) < 100000
            && Finite(Rotation.x) && Finite(Rotation.y) && Finite(Rotation.z) && Finite(Rotation.w)
            && Quaternion.Dot(Rotation, Rotation) > .8f && Quaternion.Dot(Rotation, Rotation) < 1.2f
            && Finite(Fov) && Fov >= 20 && Fov <= 140 && Finite(EyeDrop) && EyeDrop >= 0 && EyeDrop <= 10
            && Finite(SentAt) && SentAt >= 0 && (Flags & ~7) == 0
            && Finite(HandsSeat.x) && Finite(HandsSeat.y) && Finite(HandsSeat.z) && HandsSeat.sqrMagnitude < 64
            && Finite(HandsRotation.x) && Finite(HandsRotation.y) && Finite(HandsRotation.z) && Finite(HandsRotation.w)
            && Quaternion.Dot(HandsRotation, HandsRotation) > .8f && Quaternion.Dot(HandsRotation, HandsRotation) < 1.2f
            && Finite(ActionTime) && ActionTime >= 0 && ActionTime <= 30 && Finite(VisualPhase) && VisualPhase >= 0 && VisualPhase <= 1000000
            && NameValid(Action) && NameValid(Aim) && (Action.Length == 0 || ActionSequence != 0) && Private.IsValid;
        public bool IsValid => Match > 0 && Round > 0 && Slot >= 0 && Slot < Core.Balance.PlayerCount
            && Epoch >= 0 && Sequence != 0 && PoseIsValid;
        public bool Matches(long match, int round, CharacterMotor actor)
            => actor != null && actor.gameObject.activeInHierarchy && Match == match && Round == round
               && Slot == actor.PlayerSlot && Epoch == actor.MovementEpoch;
        public static bool Newer(uint value, uint previous) => value != 0 && unchecked((int)(value - previous)) > 0;
        public bool Timely(float now) => Finite(now) && SentAt <= now + .5f && now - SentAt <= 1.2f;

        public void Write(FastBufferWriter writer)
        {
            writer.WriteValueSafe(Match); writer.WriteValueSafe(Round); writer.WriteValueSafe(Slot);
            writer.WriteValueSafe(Epoch); writer.WriteValueSafe(Sequence); writer.WriteValueSafe(Eye);
            writer.WriteValueSafe(Rotation); writer.WriteValueSafe(Fov); writer.WriteValueSafe(EyeDrop); writer.WriteValueSafe(SentAt); writer.WriteValueSafe(Flags);
            writer.WriteValueSafe(HandsSeat); writer.WriteValueSafe(HandsRotation);
            writer.WriteValueSafe(ActionTime); writer.WriteValueSafe(VisualPhase); writer.WriteValueSafe(ActionSequence);
            WriteName(writer, Action); WriteName(writer, Aim);
            Private.Write(writer);
        }
        public static bool TryRead(ref FastBufferReader reader, out SpectatorViewState state)
        {
            state = default;
            int length = reader.Length - reader.Position;
            if (length < MinWireBytes || length > MaxWireBytes || !reader.TryBeginRead(105)) return false;
            reader.ReadValueSafe(out state.Match); reader.ReadValueSafe(out state.Round); reader.ReadValueSafe(out state.Slot);
            reader.ReadValueSafe(out state.Epoch); reader.ReadValueSafe(out state.Sequence); reader.ReadValueSafe(out state.Eye);
            reader.ReadValueSafe(out state.Rotation); reader.ReadValueSafe(out state.Fov); reader.ReadValueSafe(out state.EyeDrop);
            reader.ReadValueSafe(out state.SentAt);
            reader.ReadValueSafe(out state.Flags);
            reader.ReadValueSafe(out state.HandsSeat); reader.ReadValueSafe(out state.HandsRotation);
            reader.ReadValueSafe(out state.ActionTime); reader.ReadValueSafe(out state.VisualPhase); reader.ReadValueSafe(out state.ActionSequence);
            if (!ReadName(ref reader, out state.Action) || !ReadName(ref reader, out state.Aim)
                || !SpectatorPrivateView.TryRead(ref reader, out state.Private)) return false;
            return reader.Position == reader.Length && state.IsValid;
        }
        private static void WriteName(FastBufferWriter writer, FixedString64Bytes value)
        {
            writer.WriteValueSafe((ushort)value.Length);
            for (int i = 0; i < value.Length; i++) writer.WriteValueSafe(value[i]);
        }
        private static bool ReadName(ref FastBufferReader reader, out FixedString64Bytes value)
        {
            value = default;
            if (!reader.TryBeginRead(2)) return false;
            reader.ReadValueSafe(out ushort length);
            if (length > value.Capacity || !reader.TryBeginRead(length)) return false;
            value.Length = length;
            for (int i = 0; i < length; i++) { reader.ReadValueSafe(out byte c); value[i] = c; }
            return NameValid(value);
        }
        private static bool NameValid(FixedString64Bytes value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                byte c = value[i];
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '-' && c != '_') return false;
            }
            return true;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
