using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private const int MaxActionNameLength = 64;
        private GameplayActionScope CaptureActionScope(int slot) => new GameplayActionScope
        {
            Match = PresentationMatchId,
            Round = GameServices.Match?.RoundNumber ?? -1,
            Epoch = Unit(slot)?.MovementEpoch ?? -1
        };

        private bool ReadCurrentActionScope(ref FastBufferReader reader, CharacterMotor unit,
            out GameplayActionScope scope) => GameplayActionScope.TryReadTail(ref reader, out scope)
                && unit != null && scope.Matches(PresentationMatchId,
                    GameServices.Match?.RoundNumber ?? -1, unit.MovementEpoch);

        private static bool ReadActionName(ref FastBufferReader reader, out string action)
        {
            action = null;
            if (!reader.TryBeginRead(sizeof(uint))) return false;
            int start = reader.Position;
            reader.ReadValueSafe(out uint length);
            if (length == 0 || length > MaxActionNameLength
                || reader.Length - reader.Position != length * sizeof(char) + GameplayActionScope.WireBytes) return false;
            reader.Seek(start);
            reader.ReadValueSafe(out action);
            return true;
        }
    }
}
