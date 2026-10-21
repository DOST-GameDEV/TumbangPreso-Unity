using Unity.Netcode;

namespace TumbangPreso.Net
{
    public struct MatchClockMessage : INetworkSerializable
    {
        public const int WireBytes = 24;
        public long Match, Sequence;
        public int Round;
        public float Scale;

        public bool IsValid => Match > 0 && Round >= 0 && Round <= 64 && Sequence > 0 && Scale >= 0 && Scale <= 1;
        public bool Matches(long match, int round) => IsValid && Match == match && Round == round;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Match);
            serializer.SerializeValue(ref Round);
            serializer.SerializeValue(ref Sequence);
            serializer.SerializeValue(ref Scale);
        }

        public static bool TryRead(ref FastBufferReader reader, out MatchClockMessage message)
        {
            message = default;
            if (reader.Length - reader.Position != WireBytes || !reader.TryBeginRead(WireBytes)) return false;
            reader.ReadNetworkSerializable(out message);
            return message.IsValid;
        }
    }
}
