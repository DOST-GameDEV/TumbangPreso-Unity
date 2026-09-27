using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        private long _clockBroadcastSequence, _clockRequestSequence;
        private long _clockReceivedMatch, _clockReceivedSequence;
        private long _clockRequestsMatch;
        private int _clockRequestsRound = -1;
        private readonly Dictionary<ulong, long> _clockRequests = new Dictionary<ulong, long>();
        private readonly List<ulong> _retiredClockPeers = new List<ulong>();

        private MatchClockMessage CaptureMatchClock() => new MatchClockMessage
        {
            Match = EnsurePresentationMatch(), Round = GameServices.Match?.RoundNumber ?? 0,
            Sequence = ++_clockBroadcastSequence, Scale = PresentationClock.RequestedScale,
        };

        private void BroadcastMatchClock()
        {
            if (!NetAuthority.IsHost || _nm?.CustomMessagingManager == null || GameServices.Match == null) return;
            var message = CaptureMatchClock();
            if (!message.IsValid) return;
            using var writer = new FastBufferWriter(MatchClockMessage.WireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(message);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncTime", writer, NetworkDelivery.ReliableSequenced);
        }

        private bool AcceptClockRequest(ulong sender, MatchClockMessage message)
        {
            if (!message.Matches(PresentationMatchId, GameServices.Match?.RoundNumber ?? -1)) return false;
            if (_clockRequestsMatch != message.Match || _clockRequestsRound != message.Round)
            {
                _clockRequests.Clear(); _clockRequestsMatch = message.Match; _clockRequestsRound = message.Round;
            }
            if (_clockRequests.TryGetValue(sender, out long previous) && message.Sequence <= previous) return false;
            _clockRequests[sender] = message.Sequence;
            return true;
        }

        private void PruneClockRequests(LobbySession lobby)
        {
            _retiredClockPeers.Clear();
            foreach (ulong peer in _clockRequests.Keys)
                if (lobby.PeerById((int)peer) == null) _retiredClockPeers.Add(peer);
            foreach (ulong peer in _retiredClockPeers) _clockRequests.Remove(peer);
        }

        private void ApplyMatchClock(MatchClockMessage message)
        {
            if (!message.Matches(PresentationMatchId, GameServices.Match?.RoundNumber ?? -1)) return;
            if (_clockReceivedMatch != message.Match)
            { _clockReceivedMatch = message.Match; _clockReceivedSequence = 0; }
            if (message.Sequence <= _clockReceivedSequence) return;
            _clockReceivedSequence = message.Sequence;

            // Repeated snapshots must not cancel a local micro-hitstop. Only an
            // actual change to the requested match rate supersedes that effect.
            if (message.Scale != PresentationClock.RequestedScale)
            {
                Hitstop.End();
                PresentationClock.RequestScale(message.Scale);
            }
            TimeScaleChanged?.Invoke(message.Scale);
        }
    }
}
