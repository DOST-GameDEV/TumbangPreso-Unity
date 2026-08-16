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
            if (!NetAuthority.ShouldResolve()) return;

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
        /// Wipe the board without starting anything. This is `match_manager.gd::reset()`, which
        /// the port never carried across.
        ///
        /// ⚠️⚠️ THE .gd FIXED THIS AS **B-14** AND ITS NOTE IS THE WHOLE DIAGNOSIS: *"nothing
        /// reset this autoload between matches, so a second match resumed the first one's score
        /// and round number. Called both when returning to the main menu and defensively at the
        /// top of `main.gd::_ready()` every time Main.tscn loads fresh."* Unity's `GameServices`
        /// is `DontDestroyOnLoad`, which reproduces an autoload's lifetime exactly — including
        /// the bug it had to be given a `reset()` for.
        ///
        /// ⚠️⚠️ THE FREE-ROAM WINDOW HAPPENS BEFORE `StartMatch`, AND UNTIL THIS EXISTED IT SHOWED
        /// THE PREVIOUS MATCH'S SCOREBOARD. `GameServices` is `DontDestroyOnLoad`, so this
        /// director outlives every scene change; `SliceRunner.Begin` — which is what calls
        /// `StartMatch` and clears the round — does not run until the 3 · 2 · 1 finishes. So a
        /// player who finished one match and started another spent the whole ready phase looking
        /// at the last match's final scores and "ROUND 6 / 6", with the LATA card up because the
        /// old round was still flagged active, and watched all of it snap to zero on "GO!".
        /// Caught in `Logs/shots-runtime/Eskinita.png`, where a freshly-loaded arena opens on four
        /// seats holding 900 points each.
        ///
        /// ⚠️ IT FIRES NOTHING, AND THAT IS THE POINT. `StartMatch` raises `RoundStarted`, which
        /// begins round 1 underneath the countdown — the exact fault `SliceRunner.AutoStart` was
        /// turned off to prevent. This only makes the HUD tell the truth while the player walks
        /// around; `StartMatch` still owns the beginning of the match and is idempotent over this.
        /// </summary>
        public void ResetForNewMatch()
        {
            _scores.Reset();
            RoundNumber = 0;
            MatchInProgress = false;
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
