using Unity.Netcode;

namespace TumbangPreso.Net
{
    // Ownership identifies the caller; this identifies the world their input saw.
    public struct GameplayActionScope : INetworkSerializable
    {
        public const int WireBytes = sizeof(long) + sizeof(int) * 2;
        public long Match;
        public int Round, Epoch;

        public bool IsValid => Match > 0 && Round >= 0 && Round <= 64 && Epoch >= 0;
        public bool Matches(long match, int round, int epoch) =>
            IsValid && Match == match && Round == round && Epoch == epoch;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Match);
            serializer.SerializeValue(ref Round);
            serializer.SerializeValue(ref Epoch);
        }

        public static bool TryReadTail(ref FastBufferReader reader, out GameplayActionScope scope)
        {
            scope = default;
            if (reader.Length - reader.Position != WireBytes || !reader.TryBeginRead(WireBytes)) return false;
            reader.ReadNetworkSerializable(out scope);
            return scope.IsValid;
        }
    }
}
