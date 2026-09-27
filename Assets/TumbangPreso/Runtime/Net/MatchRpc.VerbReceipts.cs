using TumbangPreso.Core;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private readonly PredictionReceiptWindow _verbPredictions = new PredictionReceiptWindow(System.Enum.GetValues(typeof(DeniedVerb)).Length);
        private GameplayActionScope _localVerbScope;
        private int _localVerbSeat = -1;
        private readonly GameplayActionScope[] _receivedVerbScopes = new GameplayActionScope[Balance.PlayerCount];
        private readonly long[] _receivedVerbRequests = new long[Balance.PlayerCount];
        private readonly ulong[] _receivedVerbOwners = new ulong[Balance.PlayerCount];

        private long BeginVerbRequest(int seat, DeniedVerb verb)
        {
            var scope = CaptureActionScope(seat);
            if (_localVerbSeat != seat || !_localVerbScope.Matches(scope.Match, scope.Round, scope.Epoch))
            {
                _verbPredictions.Reset(); _localVerbScope = scope; _localVerbSeat = seat;
            }
            return _verbPredictions.Begin((int)verb);
        }

        private bool ReadNewVerbRequest(ulong sender, CharacterMotor actor, ref FastBufferReader reader,
            out GameplayActionScope scope, out long request)
        {
            scope = default; request = 0;
            if (!reader.TryBeginRead(sizeof(long) + GameplayActionScope.WireBytes)) return false;
            reader.ReadValueSafe(out request);
            if (request <= 0 || !ReadCurrentActionScope(ref reader, actor, out scope)) return false;
            int seat = actor.PlayerSlot;
            if (!ValidSlot(seat)) return false;
            var previous = _receivedVerbScopes[seat];
            if (_receivedVerbOwners[seat] == sender && previous.Matches(scope.Match, scope.Round, scope.Epoch)
                && request <= _receivedVerbRequests[seat]) return false;
            _receivedVerbOwners[seat] = sender; _receivedVerbScopes[seat] = scope; _receivedVerbRequests[seat] = request;
            return true;
        }

        private bool TakeVerbDenial(int seat, DeniedVerb verb, long request, GameplayActionScope scope) =>
            seat == _localVerbSeat && _localVerbScope.Matches(scope.Match, scope.Round, scope.Epoch)
                && _verbPredictions.TryDeny((int)verb, request);
    }
}
