using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The match: four rounds, four players, one taya per round.
    ///
    /// ⚠️⚠️ EVERY POINT IN THE GAME IS AWARDED THROUGH AddScore, AND ONLY ON THE HOST. The
    /// predecessor spread its win conditions across four files and the recurring bug class
    /// was a rule that fired on the wrong peer. A point that can only be created in one
    /// function cannot be created on a client at all. When Phase 5 adds netcode, this method
    /// gets a server guard and NOT a second client-side path.
    /// </summary>
    public sealed class MatchDirector : MonoBehaviour
    {
        public event Action<int, int> RoundStarted;        // (roundNumber, defenderSlot)
        public event Action<int, int> IntermissionStarted; // (nextRound, nextDefenderSlot)
        public event Action<int> MatchEnded;               // (winningSlot, or -1 for a draw)
        public event Action<int, ScoreEvent> Scored;

        private readonly Scoreboard _scores = new Scoreboard();

        public int RoundNumber { get; private set; }
        public int DefenderSlot => MatchRules.DefenderSlotFor(RoundNumber);
        public bool MatchInProgress { get; private set; }

        public int ScoreFor(int slot) => _scores[slot];

        /// <summary>
        /// ⚠️ HOST-SIDE ONLY, AND DELIBERATELY THE ONLY MUTATOR. Keep it that way.
        /// </summary>
        public void AddScore(int slot, ScoreEvent e)
        {
            if (!MatchInProgress) return;

            _scores.Add(slot, e);
            Scored?.Invoke(slot, e);
        }

        public void StartMatch()
        {
            _scores.Reset();
            RoundNumber = 0;
            MatchInProgress = true;
            AdvanceRound();
        }

        /// <summary>
        /// ⚠️ THE ROLE IS DERIVED FROM RoundNumber, NEVER ACCUMULATED. Incrementing the
        /// round is the whole of the rotation: there is no separate taya counter that could
        /// disagree with it, and nothing to resynchronise if a peer misses a call.
        /// </summary>
        public void AdvanceRound()
        {
            RoundNumber++;

            if (RoundNumber > Balance.Rounds)
            {
                MatchInProgress = false;
                MatchEnded?.Invoke(_scores.WinningSlot());
                return;
            }

            RoundStarted?.Invoke(RoundNumber, DefenderSlot);
        }

        /// <summary>
        /// ⚠️ SCORES PERSIST ACROSS THE BOUNDARY AND THERE IS NO PER-ROUND WINNER. Only the
        /// taya role rotates; the running totals are the whole game.
        /// </summary>
        public void BeginIntermission()
        {
            int next = RoundNumber + 1;
            if (next > Balance.Rounds)
            {
                MatchInProgress = false;
                MatchEnded?.Invoke(_scores.WinningSlot());
                return;
            }

            IntermissionStarted?.Invoke(next, MatchRules.DefenderSlotFor(next));
        }
    }
}
