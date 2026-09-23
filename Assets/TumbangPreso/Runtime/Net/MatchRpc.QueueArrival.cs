using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public sealed partial class MatchRpc
    {
        public const float QueueVoteSeconds = 12;
        private readonly int[] _queueMapVotes = { -1, -1, -1, -1 };
        private bool _queueMapVoting;
        private float _queueVoteEnds, _queueVoteOpened, _queueVoteNextState, _queueVoteRevealEnds;
        private int _queueVoteSerial, _queueMapWinner = -1;

        public bool QueueMapVoting => _queueMapVoting;
        public int QueueMapWinner => _queueMapWinner;
        public float QueueMapSecondsLeft => Mathf.Max(0, _queueVoteEnds - Time.unscaledTime);
        public int QueueMapVoteFor(int seat) => seat >= 0 && seat < _queueMapVotes.Length ? _queueMapVotes[seat] : -1;

        /// <summary>Character lock-in advances the queued room to a host-owned ballot, not an arena.</summary>
        public void HostBeginQueueMapVote()
        {
            if (!NetAuthority.IsHost || _queueMapVoting || !InPreparationScene()) return;
            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null || lobby.MatchInProgress) return;
            _queueMapVoting = true;
            _queueVoteSerial++;
            _queueMapWinner = -1;
            _queueVoteOpened = Time.unscaledTime;
            _queueVoteEnds = _queueVoteOpened + QueueVoteSeconds;
            for (int i = 0; i < _queueMapVotes.Length; i++) _queueMapVotes[i] = -1;
            var rules = UI.SceneFlow.SelectedRules.Clone();
            rules.ManualReady = false;
            SelectRulesServerRpc(CustomGameRules.ToWire(rules));
            SendQueueVoteState();
        }

        private static bool InPreparationScene() =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == UI.SceneFlow.MatchSetup;

        private void ResetQueueArrival()
        {
            _queueMapVoting = false;
            _queueMapWinner = -1;
            _queueVoteSerial = 0;
            for (int i = 0; i < _queueMapVotes.Length; i++) _queueMapVotes[i] = -1;
        }

        private void HostReceiveQueueMapVote(int seat, int mapIndex)
        {
            if (!NetAuthority.IsHost || !_queueMapVoting || _queueMapWinner >= 0 || !InPreparationScene()) return;
            if (seat < 0 || seat >= Balance.PlayerCount || mapIndex < 0 || mapIndex >= UI.SceneFlow.Maps.Length) return;
            var info = GetSeatInfo(seat);
            if (info == null || !info.Occupied || _queueMapVotes[seat] == mapIndex) return;
            _queueMapVotes[seat] = mapIndex;
            MapVoteTallyClientRpc(_queueMapVotes);
            SendQueueVoteState();
        }

        private void ApplyQueueMapVotes(int[] votes)
        {
            if (!_queueMapVoting || votes == null) return;
            for (int i = 0; i < _queueMapVotes.Length && i < votes.Length; i++)
                _queueMapVotes[i] = votes[i] >= 0 && votes[i] < UI.SceneFlow.Maps.Length ? votes[i] : -1;
        }

        private void TickQueueArrival()
        {
            if (!_queueMapVoting) return;
            if (!InPreparationScene() || _nm == null || !_nm.IsListening)
            {
                _queueMapVoting = false;
                return;
            }
            if (!NetAuthority.IsHost) return;
            if (_queueMapWinner >= 0)
            {
                if (Time.unscaledTime >= _queueVoteRevealEnds)
                {
                    _queueMapVoting = false;
                    HostStartMatch();
                }
                return;
            }

            bool everyone = true;
            int occupied = 0;
            for (int i = 0; i < _queueMapVotes.Length; i++)
            {
                var seat = GetSeatInfo(i);
                if (seat == null || !seat.Occupied) { _queueMapVotes[i] = -1; continue; }
                occupied++;
                if (_queueMapVotes[i] < 0) everyone = false;
            }
            if (QueueMapSecondsLeft <= 0 || (occupied > 0 && everyone && Time.unscaledTime - _queueVoteOpened >= 3))
            {
                int current = System.Array.IndexOf(UI.SceneFlow.Maps, UI.SceneFlow.SelectedMap);
                int winner = MapRotationRules.TallyVote(_queueMapVotes, UI.SceneFlow.Maps.Length, current);
                _queueMapWinner = winner >= 0 ? winner : Mathf.Max(0, current);
                _queueVoteRevealEnds = Time.unscaledTime + 1;
                SelectMapServerRpc(_queueMapWinner);
                SendQueueVoteState();
            }
            else if (Time.unscaledTime >= _queueVoteNextState) SendQueueVoteState();
        }

    }
}
