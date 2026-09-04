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
        /// <summary>
        /// How many rounds this match runs.
        ///
        /// ⚠️⚠️ IT READS THE RULE SET NOW RATHER THAN COMPUTING FROM THE MODE, AND THE SHIPPED
        /// ANSWER IS UNCHANGED. `CustomGameRules.Defaults` sets `Rounds` from
        /// `MatchRules.RoundCountFor` for both modes, which is `docs/VISION.md` § 1's rule
        /// (Classic plays four rounds, Hero Strike eight) written once instead of twice. **A rule
        /// set nobody has edited plays exactly what it always did**; what changes is that a custom
        /// lobby can say three and the match obeys, which is what `CustomRules.Rounds` has meant
        /// since Phase 12 was written and what nothing read.
        ///
        /// ⚠️ THE ROLE SCHEDULE IS UNAFFECTED AND MUST STAY THAT WAY. `DefenderSlotFor` is
        /// `(round - 1) % 4` and is DERIVED (`CLAUDE.md` § 4), so a three-round match simply
        /// stops before the fourth seat defends. **"Everybody defends exactly once" is a property
        /// of the shipped four-round Classic format, not of the rotation**, and a custom lobby
        /// choosing otherwise is choosing that knowingly.
        /// </summary>
        public int TotalRounds => UI.SceneFlow.SelectedRoundCount;

        /// <summary>
        /// Whether a custom score target has been reached, so this match ends before its last
        /// round does.
        ///
        /// ⚠️⚠️ THE RULE IS IN THE CORE AND ONLY THE QUESTION IS HERE.
        /// `CustomGameRules.ScoreTargetReached` is engine-free and asserted in about a
        /// millisecond by `CustomGameScoreTargetTests`; this reads the scoreboard and asks it,
        /// which is `docs/FUTURE.md` § 19.12's constraint (*"Every new mode adds its rules to
        /// `Packages/com.tumbangpreso.core/`, never to Unity code"*) kept honestly.
        ///
        /// ⚠️ ZERO IS OFF AND THAT IS HOW THE GAME SHIPS, so this answers false for every
        /// standard match and the two call sites below are unchanged for them.
        /// </summary>
        private bool ScoreTargetReached()
        {
            int target = UI.SceneFlow.SelectedRules.ScoreTarget;
            if (target <= 0) return false;

            var scores = new int[Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = _scores[i];

            return CustomGameRules.ScoreTargetReached(scores, target);
        }
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

            // ⚠️⚠️ THE AWARD IS ANNOUNCED, NOT ONLY RECORDED, AND THAT IS THE HALF THAT WAS
            // MISSING FROM EVERY CLIENT. The SCORE reaches a peer inside `SyncWorld` and the
            // EVENT did not, so on a client the numbers rose silently up to 200 ms later with no
            // sting, no `+100  LATA DOWN` toast and no scoreboard pulse: `Hud.OnScored` is what
            // produces all three and it hangs off this event. In a game whose entire feedback
            // loop is scoring, three of the four things that acknowledge a point were host-only.
            //
            // ⚠️ THE KIND TRAVELS, NOT THE DELTA, and that is why it needs a message rather than
            // a diff of the replicated scores. The toast and the sting both read the
            // `ScoreEvent` itself (`MatchRules.PointsFor` and the label), a delta does not carry
            // it, and two awards inside one 200 ms window would collapse into one.
            //
            // ⚠️ IT IS STILL ONE FUNCTION. This line is INSIDE the host guard above, so the
            // announcement cannot be made anywhere a point cannot be created. See this class's
            // header: a point that can only be created in one function cannot be created on a
            // client at all, and the same is now true of the noise it makes.
            Net.MatchRpc.Instance?.BroadcastScore(slot, e);

            // A first point is the first durable result the game can honestly call worth
            // keeping today. PlayerAccount records the offer for the next menu rather than
            // interrupting the round with a credential form.
            GameServices.Account?.MarkWorthKeeping();
        }

        /// <summary>
        /// A point the HOST awarded, replayed on this peer for its presentation only.
        ///
        /// ⚠️⚠️ IT DOES NOT TOUCH THE SCOREBOARD, AND THAT IS THE WHOLE POINT OF IT BEING A
        /// SEPARATE METHOD. The totals arrive in `SyncWorld` and `ApplySnapshot` sets them from
        /// the host's own numbers; adding here as well would make a client's board the sum of a
        /// replicated total and its own arithmetic, which disagree at exactly the moments that
        /// matter because a client cannot see the host's distance checks. This raises the EVENT
        /// and nothing else.
        /// </summary>
        public void ApplyNetworkScoreEvent(int slot, ScoreEvent e) => Scored?.Invoke(slot, e);

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

            if (inProgress) HostConfirmedInProgress = true;

            _scores.SetAll(scores);
            RoundNumber = roundNumber;
            MatchInProgress = inProgress;

            // ⚠️ THE EDGE NEEDS THE HOST TO HAVE CONFIRMED THIS MATCH FIRST. See
            // `IsPreStartSnapshot`: without that clause the falling edge also fires on a packet
            // the host wrote BEFORE it started, which is the whole of § 82.
            if (wasInProgress && !inProgress && HostConfirmedInProgress)
                MatchEnded?.Invoke(_scores.WinningSlot());
        }

        /// <summary>
        /// Whether the host has described THIS match as running at least once on this peer.
        ///
        /// Cleared by every local `StartMatch`, so it answers "has the host caught up with the
        /// match I just started", not "was a match ever running".
        /// </summary>
        public bool HostConfirmedInProgress { get; private set; }

        /// <summary>
        /// ⚠️⚠️ A SNAPSHOT FROM BEFORE THE HOST'S OWN ARENA FINISHED LOADING, AND IT ENDED THE
        /// MATCH FOR EVERY CLIENT IN THE ROOM. `docs/TODO.md` § 82. 🧑 2026-08-29, from a client:
        /// *"wtf the game started as done for all non hosts and host is still playing"*, with the
        /// final standings up at 01:26 of round 1 and every column reading 0 PTS.
        ///
        /// `HostStartMatch` sends `StartMatch` to the peers and THEN loads the host's own arena,
        /// and `MatchRpc.FixedUpdate` goes on writing `SyncWorld` at 5 Hz throughout that load
        /// carrying `match.MatchInProgress`, which is still **false** because the host's
        /// `SliceRunner.Begin` has not run yet. A client whose disk answered first is already
        /// in its arena with `MatchInProgress` true, so the next of those packets is a true → false
        /// edge and `ApplySnapshot` raised `MatchEnded` on it: `UI.MatchResult` drew the final
        /// standings over a match one second old, and `SliceRunner.OnMatchEnded` parked the cast
        /// for good. Scores kept arriving underneath it, which is why the HUD read 100/30/0/0
        /// behind a board that said everybody drew on nothing.
        ///
        /// It is a race, so it is a coin flip per client per match rather than a fault that shows
        /// up every time, and the host is immune by construction.
        ///
        /// ⚠️ THE WHOLE PACKET IS DROPPED, NOT JUST THE EVENT. `MatchRpc` hands the same
        /// `inProgress` to `RoundDirector.ApplySnapshot`, which clears `RoundActive` and with it
        /// `CanAct`; believing the event and applying the state would leave the body unable to
        /// move until the host caught up.
        ///
        /// ⚠️ IT COSTS NO WIRE CHANGE AND NO PROTOCOL BUMP, which is why it is written as a
        /// question about local state rather than as a match id in the payload. A generation
        /// counter incremented on each peer drifts the moment somebody joins late: a joiner
        /// adopts the host's number and then increments past it in its own `StartMatch`, after
        /// which every packet it receives looks stale forever.
        /// </summary>
        public bool IsPreStartSnapshot(bool inProgress) =>
            MatchInProgress && !inProgress && !HostConfirmedInProgress;

        public void StartMatch()
        {
            _scores.Reset();
            RoundNumber = 0;
            MatchInProgress = true;
            IsWarmupBuffer = false;

            // ⚠️⚠️ THE HIGHLIGHT LOG IS ZEROED WITH THE SCOREBOARD, AND FOR THE SAME REASON THE
            // LINE ABOVE IT EXISTS. `docs/TODO.md` § 143.5 found a real cross-match leak in this
            // process, and a marker that survived into the next match would put somebody else's
            // best moment on this match's reel with a timestamp that lands nowhere. It is also
            // where the match clock every marker is stamped against begins.
            Diagnostics.MatchHighlights.BeginMatch();

            // ⚠️ CLEARED HERE AND ONLY HERE. A rematch runs this again on every peer, and the
            // host reloads its arena on the same race the first match does, so a flag left
            // standing from the PREVIOUS match would let the stale packet through second time
            // round. See `IsPreStartSnapshot`.
            HostConfirmedInProgress = false;

            AdvanceRound();
        }

        public void ResetForNewMatch()
        {
            _scores.Reset();
            RoundNumber = 0;
            MatchInProgress = false;
            IsWarmupBuffer = false;
            HostConfirmedInProgress = false;
        }

        public void AdvanceRound()
        {
            RoundNumber++;
            IsWarmupBuffer = false;

            // ⚠️⚠️ THE TARGET IS ASKED BESIDE THE ROUND COUNT, NEVER INSTEAD OF IT. Both
            // conditions end the match through the same two lines, so a target that nobody
            // reaches changes nothing and a target that is reached can only ever end a match
            // EARLIER than the rounds would have. Writing it as an `else` would have made a
            // custom target able to EXTEND a match past its last round.
            if (RoundNumber > TotalRounds || ScoreTargetReached())
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

            // ⚠️ THE SAME PAIR OF CONDITIONS AS `AdvanceRound`, AND BOTH SITES ARE NEEDED. This
            // is the one that runs at the END of a round, so a target reached on the last award
            // of round two ends the match here rather than opening an intermission the player
            // then sits through for a match that is already decided.
            if (next > TotalRounds || ScoreTargetReached())
            {
                MatchInProgress = false;
                IsWarmupBuffer = false;
                MatchEnded?.Invoke(_scores.WinningSlot());
                return;
            }

            IsWarmupBuffer = true;
            SkipRequested = false;
            IntermissionStarted?.Invoke(next, MatchRules.DefenderSlotFor(next));
        }

        // -------------------------------------------------------------------
        // § SKIPPING THE BUFFER
        //
        // ⚠️⚠️ THE DECISION LIVES HERE BECAUSE THERE ARE TWO RUNNERS AND ONLY ONE RULE. 🧑
        // 2026-08-29: *"vote to skip buffer time"*. `SliceRunner` schedules the advance with
        // `Balance.WarmupBufferDuration` and `MatchBootstrap` with `Balance.IntermissionDuration`,
        // and both are `Invoke` calls on their own component. Putting the skip in either one
        // would give the shipped arena a feature the other path silently lacks, which is exactly
        // what `SliceRunner`'s own header forbids: *"it must not acquire rules of its own"*.
        // The director raises the event; each runner cancels its own pending `Invoke` and
        // advances, which is the one thing each of them genuinely owns.
        //
        // ⚠️ WHO MAY CALL IT IS NOT DECIDED HERE. `BufferSkipVote` counts the votes and only the
        // host calls this, for the same reason `AdvanceRound` is host-only: a round boundary is
        // a decision, and `CLAUDE.md` § 4 keeps decisions on one machine.
        // -------------------------------------------------------------------

        /// <summary>Raised when the intermission should end early. See the section note.</summary>
        public event Action BufferSkipRequested;

        /// <summary>
        /// True once the buffer has been skipped, so a second vote arriving a frame later cannot
        /// advance the round twice. Cleared by <see cref="BeginIntermission"/>.
        /// </summary>
        public bool SkipRequested { get; private set; }

        public void SkipBuffer()
        {
            if (!IsWarmupBuffer || SkipRequested) return;

            SkipRequested = true;
            BufferSkipRequested?.Invoke();
        }
    }
}
