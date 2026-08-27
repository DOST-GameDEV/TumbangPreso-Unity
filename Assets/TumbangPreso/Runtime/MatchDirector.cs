using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The match: four Classic rounds or eight Hero Strike rounds, four players, one taya per
    /// round. Hero Strike therefore runs two complete role rotations.
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
        public int TotalRounds => MatchRules.RoundCountFor(UI.SceneFlow.SelectedMode);
        public bool MatchInProgress { get; private set; }
        public bool IsWarmupBuffer { get; set; }

        public int ScoreFor(int slot) => _scores[slot];

        /// <summary>
        /// Seats ordered by score, highest first, from `match_manager.gd:154`.
        ///
        /// ⚠️ THE TIE-BREAK IS SEAT ORDER AND IT IS DELIBERATE. Equal scores fall back to the
        /// lower slot index, which makes the ordering STABLE — the results board does not
        /// reshuffle two tied players between frames, and every peer computes the same board
        /// from the same scores without sending an ordering over the wire.
        /// </summary>
        public int[] Ranking()
        {
            var order = new int[Balance.PlayerCount];
            for (int i = 0; i < order.Length; i++) order[i] = i;

            System.Array.Sort(order, (a, b) =>
                _scores[a] == _scores[b] ? a.CompareTo(b) : _scores[b].CompareTo(_scores[a]));

            return order;
        }

        /// <summary>
        /// ⚠️⚠️ HOST-SIDE ONLY, AND DELIBERATELY THE ONLY MUTATOR IN THE GAME.
        ///
        /// The guard is here rather than at each of the four call sites on purpose: a point
        /// that can only be CREATED in one function cannot be created on a client at all, and
        /// spreading the check outward is how the predecessor ended up with rules firing on
        /// the wrong peer. In single player this is a host with no peers, so it always passes
        /// and nothing needs special casing.
        ///
        /// ⚠️ WHEN PHASE 5 LANDS, THE SCORE IS BROADCAST FROM HERE AND NEVER RECOMPUTED ON A
        /// CLIENT. A client that derives its own score will disagree at exactly the moments
        /// that matter, because it cannot see the host's distance checks.
        /// </summary>
        public void AddScore(int slot, ScoreEvent e)
        {
            if (!MatchInProgress) return;
            if (IsWarmupBuffer) return;
            if (!NetAuthority.ShouldResolve()) return;

            _scores.Add(slot, e);
            Scored?.Invoke(slot, e);
        }

        /// <summary>
        /// The replicated match, as the host last described it.
        ///
        /// ⚠️⚠️ THE END OF A MATCH IS AN EVENT ON A CLIENT TOO, AND IT WAS NOT ONE. This method
        /// assigned the three fields and raised nothing, and it is the ONLY thing that moves them
        /// on a peer that is not the host: `AdvanceRound` and `BeginIntermission` are the only
        /// other writers and both are behind `SliceRunner`'s `NetAuthority.ShouldResolve()`. So
        /// `MatchEnded` fired on exactly one machine in the room.
        ///
        /// **What that costs is the whole end of the game for everybody except the host.**
        /// `UI.MatchResult` shows itself from `MatchEnded` and from nothing else, so a client
        /// never saw the final standings; `SliceRunner.OnMatchEnded` never ran there, so the
        /// round rules were never stopped; and the announcer's win line never played. Worst of
        /// all, **REMATCH lives on that board**, so the entire peer rematch vote (`docs/TODO.md`
        /// § 1) was unreachable for anyone but the host: a client had no button to press, and
        /// `RematchTally` and `BeginRematch` arrived at a screen that was never raised.
        ///
        /// ⚠️ ONLY THE TRUE-TO-FALSE EDGE, AND ONLY HERE. A joining client is told `false` before
        /// the match starts and `false` again after it ends, so raising the event on the VALUE
        /// would show the result board to somebody who has just walked into a lobby.
        ///
        /// ⚠️⚠️ AND `RoundStarted` AND `IntermissionStarted` ARE DELIBERATELY NOT RAISED HERE,
        /// WHICH IS THE HALF THAT LOOKS LIKE AN OVERSIGHT AND IS NOT. Both are wired to
        /// `SliceRunner`, and both of its handlers MUTATE THE WORLD: `OnRoundStarted` calls
        /// `ResetWorld`, which teleports all four bodies and hands out the tsinelas, and
        /// `OnIntermission` additionally schedules `Advance`, which calls `AdvanceRound` and would
        /// give every client its own second authority over the round number. Four peers each
        /// advancing a match is four matches, which is `VISION.md` § 4's first rule. The
        /// intermission CARD still needs a signal on a client and it needs a different one;
        /// `docs/TODO.md` § 57 carries that as its own item rather than solving it by raising an
        /// event that does six other things.
        ///
        /// ⚠️ THE HOST REACHES THIS TOO, through `MatchRpc.HostSyncPeer`, and it is a no-op there
        /// by construction: it passes the host its own `MatchInProgress` back, so the edge cannot
        /// fire.
        /// </summary>
        public void ApplySnapshot(int[] scores, int roundNumber, bool inProgress)
        {
            bool wasInProgress = MatchInProgress;

            _scores.SetAll(scores);
            RoundNumber = roundNumber;
            MatchInProgress = inProgress;

            if (wasInProgress && !inProgress) MatchEnded?.Invoke(_scores.WinningSlot());
        }

        public void StartMatch()
        {
            _scores.Reset();
            RoundNumber = 0;
            MatchInProgress = true;
            IsWarmupBuffer = false;
            AdvanceRound();
        }

        public void ResetForNewMatch()
        {
            _scores.Reset();
            RoundNumber = 0;
            MatchInProgress = false;
            IsWarmupBuffer = false;
        }

        public void AdvanceRound()
        {
            RoundNumber++;
            IsWarmupBuffer = false;

            if (RoundNumber > TotalRounds)
            {
                MatchInProgress = false;
                MatchEnded?.Invoke(_scores.WinningSlot());
                return;
            }

            RoundStarted?.Invoke(RoundNumber, DefenderSlot);
        }

        public void BeginIntermission()
        {
            int next = RoundNumber + 1;
            if (next > TotalRounds)
            {
                MatchInProgress = false;
                IsWarmupBuffer = false;
                MatchEnded?.Invoke(_scores.WinningSlot());
                return;
            }

            IsWarmupBuffer = true;
            IntermissionStarted?.Invoke(next, MatchRules.DefenderSlotFor(next));
        }
    }
}
