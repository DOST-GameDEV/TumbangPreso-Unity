using TumbangPreso.Abilities;
using TumbangPreso.Core;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public struct SentryTargetState : INetworkSerializable
    {
        public const int WireBytes = 25;
        public long Match, Instance;
        public int Round, Owner;
        public byte Mask;

        public bool IsValid => Match > 0 && Round > 0 && Round <= 64 && Instance > 0 &&
            Owner >= 0 && Owner < Balance.PlayerCount && (Mask & ~((1 << Balance.PlayerCount) - 1)) == 0 &&
            (Mask & (1 << Owner)) == 0;

        public static SentryTargetState Capture(PaeteSentry sentry) => new SentryTargetState
        {
            Match = sentry.MatchId, Round = sentry.RoundNumber, Owner = sentry.OwnerSlot,
            Instance = sentry.InstanceId, Mask = sentry.Capture().TargetMask,
        };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Match); serializer.SerializeValue(ref Round);
            serializer.SerializeValue(ref Owner); serializer.SerializeValue(ref Instance); serializer.SerializeValue(ref Mask);
        }

        public static bool TryRead(ref FastBufferReader reader, out SentryTargetState state)
        {
            state = default;
            if (reader.Length - reader.Position != WireBytes || !reader.TryBeginRead(WireBytes)) return false;
            reader.ReadNetworkSerializable(out state);
            return state.IsValid;
        }
    }
}
