using TumbangPreso.Core;
using Unity.Netcode;
using Unity.Collections;

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
        private void SendContactRecovery(ulong client, int seat, long request, GameplayActionScope scope, DeniedVerb verb, bool hit)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(14 + GameplayActionScope.WireBytes, Allocator.Temp);
            writer.WriteValueSafe(seat); writer.WriteValueSafe(request); writer.WriteValueSafe((byte)verb); writer.WriteValueSafe((byte)(hit ? 1 : 0));
            writer.WriteNetworkSerializable(scope);
            _nm.CustomMessagingManager.SendNamedMessage("ContactRecovery", client, writer);
        }

        private void OnContactRecoveryMsg(ulong sender, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || !FromHost(sender)
                || reader.Length - reader.Position != 14 + GameplayActionScope.WireBytes
                || !reader.TryBeginRead(14 + GameplayActionScope.WireBytes)) return;
            reader.ReadValueSafe(out int seat); reader.ReadValueSafe(out long request); reader.ReadValueSafe(out byte verb); reader.ReadValueSafe(out byte hit);
            if (!ValidSlot(seat) || seat != NetAuthority.LocalSlot || hit > 1
                || (verb != (byte)DeniedVerb.Punch && verb != (byte)DeniedVerb.Shove)) return;
            var actor = Unit(seat);
            if (!ReadCurrentActionScope(ref reader, actor, out var scope)
                || !TakeVerbDenial(seat, (DeniedVerb)verb, request, scope)) return;
            var combat = actor.GetComponent<CombatVerbs>();
            if (verb == (byte)DeniedVerb.Punch) combat?.ConfirmPunchResult(hit == 1);
            else combat?.ConfirmShoveResult(hit == 1);
        }

    }
}
