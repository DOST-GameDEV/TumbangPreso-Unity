using System;
using TumbangPreso.Core;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    // Complete visual state for one Nemu. Gameplay still runs only on the host.
    // Two Blindfold lifetimes can overlap when the old trap springs just before a recast.
    public struct NemuGhostPose : INetworkSerializable
    {
        public const int WireBytes = 67;
        public byte Kind, Phase, Flags; // 1 Blindfold, 2 Skitter, 3 Lookout; phase4 leaves
        public long Id;
        public Vector3 Position, Forward, Auxiliary;
        public float Age, PhaseAge, Progress;
        public int Target, TargetEpoch;
        public bool IsValid(int owner) => Id > 0 && Kind >= 1 && Kind <= 3 && Phase >= 1 && Phase <= 4
            && Finite(Position) && Finite(Forward) && Finite(Auxiliary)
            && Mathf.Abs(Forward.y) < .01f && Mathf.Abs(Forward.sqrMagnitude - 1f) < .02f
            && Clock(Age) && Clock(PhaseAge) && float.IsFinite(Progress) && Progress >= 0 && Progress <= 1
            && (Kind == 1 ? Flags == 0 && Progress == 0 && (Phase == 1 ? Target == -1 && TargetEpoch == 0
                : Target >= -1 && Target < Balance.PlayerCount && Target != owner && TargetEpoch >= 0
                    && (Phase == 4 || Target >= 0))
                : Target == -1 && TargetEpoch == 0 && (Kind == 2 ? Phase != 3 && Flags <= 7 : Phase != 3 && Flags <= 3));
        private static bool Clock(float value) => float.IsFinite(value) && value >= 0 && value <= 60;
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z)
            && Mathf.Abs(value.x) <= 20000 && Mathf.Abs(value.y) <= 20000 && Mathf.Abs(value.z) <= 20000;
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Kind); s.SerializeValue(ref Id); s.SerializeValue(ref Phase);
            s.SerializeValue(ref Position); s.SerializeValue(ref Forward); s.SerializeValue(ref Auxiliary);
            s.SerializeValue(ref Age); s.SerializeValue(ref PhaseAge); s.SerializeValue(ref Progress);
            s.SerializeValue(ref Flags); s.SerializeValue(ref Target); s.SerializeValue(ref TargetEpoch);
        }
    }

    public struct NemuGhostFrame : INetworkSerializable
    {
        public const int MaxGhosts = 6, HeaderBytes = 33, MaxWireBytes = HeaderBytes + MaxGhosts * NemuGhostPose.WireBytes;
        public int Seat;
        public GameplayActionScope Scope;
        public long Sequence;
        public float RoundClock;
        public NemuGhostPose[] Ghosts;
        private static long _nextId;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetIds() => _nextId = 0;
        public static long NewId() => ++_nextId;
        public bool IsValid
        {
            get
            {
                if (Seat < 0 || Seat >= Balance.PlayerCount || !Scope.IsValid || Sequence <= 0
                    || !float.IsFinite(RoundClock) || RoundClock < 0 || RoundClock > 36000
                    || Ghosts == null || Ghosts.Length > MaxGhosts) return false;
                int blind = 0, skitter = 0, lookout = 0;
                for (int i = 0; i < Ghosts.Length; i++)
                {
                    if (!Ghosts[i].IsValid(Seat)) return false;
                    if (Ghosts[i].Kind == 1) blind++; else if (Ghosts[i].Kind == 2) skitter++; else lookout++;
                    for (int j = 0; j < i; j++) if (Ghosts[i].Id == Ghosts[j].Id) return false;
                }
                return blind <= 2 && skitter <= 2 && lookout <= 2;
            }
        }
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Seat); s.SerializeValue(ref Scope); s.SerializeValue(ref Sequence); s.SerializeValue(ref RoundClock);
            byte count = (byte)(Ghosts?.Length ?? 0); s.SerializeValue(ref count);
            if (count > MaxGhosts) throw new ArgumentOutOfRangeException(nameof(Ghosts));
            if (s.IsReader) Ghosts = new NemuGhostPose[count];
            for (int i = 0; i < count; i++) s.SerializeValue(ref Ghosts[i]);
        }
        public static bool TryRead(ref FastBufferReader reader, out NemuGhostFrame frame)
        {
            frame = default;
            int bytes = reader.Length - reader.Position;
            if (bytes < HeaderBytes || bytes > MaxWireBytes || !reader.TryBeginRead(bytes)) return false;
            try { reader.ReadNetworkSerializable(out frame); return reader.Position == reader.Length && frame.IsValid; }
            catch (OverflowException) { return false; } catch (ArgumentException) { return false; }
        }
    }
}
