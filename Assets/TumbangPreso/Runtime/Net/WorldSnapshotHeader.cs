using System;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public struct WorldSnapshotHeader : INetworkSerializable
    {
        public const int MaxWireBytes = 256;
        public int Generation, Round, Count;
        public FixedString128Bytes Scene;
        public float SentAt, RoundClock;
        public long Match, SkillEvent, OwnerRequest;
        public long UltimatePhase, OwnerUltimateRequest;
        public int UltimateStage;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Generation);
            serializer.SerializeValue(ref Round);
            // Bound the byte count even when Unity collection safety checks are disabled.
            ushort length = (ushort)Scene.Length;
            serializer.SerializeValue(ref length);
            if (length > Scene.Capacity) throw new ArgumentOutOfRangeException(nameof(Scene));
            if (serializer.IsReader) Scene.Length = length;
            for (int i = 0; i < length; i++)
            {
                byte value = serializer.IsReader ? (byte)0 : Scene[i];
                serializer.SerializeValue(ref value);
                if (serializer.IsReader) Scene[i] = value;
            }
            serializer.SerializeValue(ref Count);
            serializer.SerializeValue(ref SentAt);
            serializer.SerializeValue(ref Match);
            serializer.SerializeValue(ref SkillEvent);
            serializer.SerializeValue(ref OwnerRequest);
            serializer.SerializeValue(ref RoundClock);
            serializer.SerializeValue(ref UltimatePhase);
            serializer.SerializeValue(ref UltimateStage);
            serializer.SerializeValue(ref OwnerUltimateRequest);
        }

        public bool Matches(long match, int round, string scene)
            => Match > 0 && Match == match && Round >= 0 && Round == round && Scene.ToString() == scene
                && Generation > 0 && Count >= 0 && Count <= WorldEffectSnapshot.MaxFields
                && SkillEvent >= 0 && OwnerRequest >= 0 && Finite(SentAt) && ValidClock(RoundClock)
                && UltimatePhase >= 0 && OwnerUltimateRequest >= 0 && UltimateStage >= 0 && UltimateStage <= 2;

        // A cohort's handback creates effects without changing its phase ID.
        // Stage progression must therefore invalidate a pre-activation snapshot too.
        public bool IncludesUltimateState(long phase, int stage)
            => phase >= 0 && stage >= 0 && stage <= 2 && phase <= UltimatePhase
                && (phase < UltimatePhase || stage <= UltimateStage);

        public bool TryAge(float currentClock, out float elapsed)
        {
            elapsed = 0;
            if (!ValidClock(RoundClock) || !ValidClock(currentClock)) return false;
            elapsed = Mathf.Max(0, RoundClock - currentClock);
            return true;
        }

        public static bool TryRead(ref FastBufferReader reader, out WorldSnapshotHeader header)
        {
            header = default;
            if (reader.Length - reader.Position < 66 || reader.Length - reader.Position > MaxWireBytes) return false;
            try
            {
                reader.ReadNetworkSerializable(out header);
                return reader.Position == reader.Length;
            }
            catch (OverflowException) { return false; }
            catch (ArgumentException) { return false; }
        }

        private static bool ValidClock(float value)
            => Finite(value) && value >= 0 && value <= CustomGameRules.MaxRoundSeconds;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
