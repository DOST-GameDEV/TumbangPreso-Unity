using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// PHASE 12's match half for LAST TSINELAS STANDING: the stock counter, the elimination, and
    /// the early end of a round. `docs/TODO.md` § 130.13.
    ///
    /// ⚠️⚠️ THE RULES ARE IN `CustomGameRules` AND NOT IN HERE, AND THAT SPLIT IS THE WHOLE
    /// REASON THIS FILE IS SHORT. `TsinelasLeft`, `IsOut`, `LastAttackerStanding`,
    /// `AliveAttackers` and `RoundIsDecided` are engine-free and asserted in about a second by
    /// `Core.Tests`; what is left here is the three things that genuinely need the engine: when a
    /// tag happened, which body to switch off, and how to tell the other three machines. Read
    /// `CustomGame.cs` before changing any number below, because none of them are here.
    ///
    /// ⚠️⚠️ EVERY TAG IN THIS GAME IS ALREADY A TAG ON A CARRIER, WHICH IS WHY THE LOSS
    /// CONDITION NEEDED NO NEW CHECK AND IS WORTH WRITING DOWN BEFORE SOMEBODY ADDS ONE.
    /// `CustomGameRules.TsinelasLeft`'s note says a tsinelas is spent when the taya tags you
    /// **while you are carrying it back**, which reads like a second condition to test on top of
    /// the tag. It is not: `CharacterMotor.IsTaggable` returns false unless `HoldingSlipper`, and
    /// `RoundDirector.ResolveTag` asks that same function before awarding anything. So a tag and
    /// a spent tsinelas are the same event, and re-testing `HoldingSlipper` here would read the
    /// flag one frame after `ApplyTagPenalty` sent the slipper home and silently never fire.
    ///
    /// ⚠️⚠️ AND THE OTHER HALF OF THAT NOTE, "a tsinelas is lost when the round ends with it
    /// still on the floor", CANNOT ELIMINATE ANYBODY AND IS NOT IMPLEMENTED AS AN ELIMINATION.
    /// Stock is PER ROUND (`IsOut` says "out for the rest of the round" and the stock resets on
    /// every whistle), so a loss charged at the final whistle is charged to a round that is
    /// already over. It would change no result and would fire after the winner had been paid.
    /// The clause still describes the format correctly for a reader; it just is not a code path.
    ///
    /// ⚠️⚠️ HOST-ONLY, AND THE ELIMINATION HAS TO TRAVEL, WHICH IS THE HALF THAT COSTS A
    /// PROTOCOL BUMP. `NetSession.ProtocolVersion` moved to 22 for this. An attacker who is out
    /// has `RoundActive` cleared so they cannot throw, grab or be tagged again, and on a client
    /// that flag is local: without `MatchRpc.BroadcastTsinelas` an eliminated player would go on
    /// moving and throwing on their own screen while the host ignored every request, which is the
    /// exact "two different games sharing one scoreboard" sentence `ProtocolVersion` uses.
    ///
    /// ⚠️ IT IS INERT IN EVERY OTHER FORMAT. `SceneFlow.SelectedFormat` is read once per round
    /// rather than every tag, so a Standard match pays for one enum comparison per whistle.
    /// </summary>
    public sealed class LastTsinelasDirector : MonoBehaviour
    {
        /// <summary>(slot) of an attacker who has just lost their last tsinelas.</summary>
        public event Action<int> AttackerEliminated;

        /// <summary>(winningSlot, or -1 when nobody survived and the round goes to the taya).</summary>
        public event Action<int> RoundDecided;

        /// <summary>Raised whenever a stock changes, so the HUD can redraw without polling.</summary>
        public event Action StocksChanged;

        private readonly int[] _stocks = new int[Balance.PlayerCount];

        private bool _live;
        private bool _decided;
        private int _defenderSlot = -1;

        /// <summary>Whether this round is being played under Last Tsinelas rules.</summary>
        public bool Live => _live;

        /// <summary>How many tsinelas a seat has left. 0 for the taya and for anyone out.</summary>
        public int StockFor(int slot)
            => slot < 0 || slot >= _stocks.Length ? 0 : _stocks[slot];

        public bool IsOut(int slot) => _live && StockFor(slot) <= 0 && slot != _defenderSlot;

        private void OnEnable()
        {
            if (GameServices.Match != null) GameServices.Match.RoundStarted += OnRoundStarted;
            if (GameServices.Round != null) GameServices.Round.Tagged += OnTagged;
        }

        private void OnDisable()
        {
            if (GameServices.Match != null) GameServices.Match.RoundStarted -= OnRoundStarted;
            if (GameServices.Round != null) GameServices.Round.Tagged -= OnTagged;
        }

        /// <summary>
        /// Clears the format out between matches, so a Standard match after a Last Tsinelas one
        /// cannot inherit a stock table and start switching bodies off.
        /// </summary>
        public void ResetForNewMatch()
        {
            _live = false;
            _decided = false;
            _defenderSlot = -1;
            Array.Clear(_stocks, 0, _stocks.Length);
            StocksChanged?.Invoke();
        }

        /// <summary>
        /// ⚠️ THE STOCK IS READ FROM THE LOBBY'S RULE SET AND CLAMPED AGAIN HERE.
        /// `CustomGameRules`' own header says every bound is a bound on the host rather than a
        /// suggestion to it, and a custom lobby is the one place a player writes a number every
        /// other machine then plays by. `Refusal` already refuses it in the lobby; this is the
        /// second gate, on the machine that actually runs the round.
        /// </summary>
        private void OnRoundStarted(int roundNumber, int defenderSlot)
        {
            _decided = false;
            _defenderSlot = defenderSlot;
            _live = UI.SceneFlow.SelectedFormat == MatchFormat.LastTsinelas;

            Array.Clear(_stocks, 0, _stocks.Length);

            if (!_live)
            {
                StocksChanged?.Invoke();
                return;
            }

            int stock = Mathf.Clamp(UI.SceneFlow.SelectedTsinelas,
                                    CustomGameRules.MinTsinelas,
                                    CustomGameRules.MaxTsinelas);

            for (int slot = 0; slot < _stocks.Length; slot++)
                _stocks[slot] = slot == defenderSlot ? 0 : stock;

            StocksChanged?.Invoke();
            Broadcast();
        }

        /// <summary>
        /// ⚠️⚠️ THE HOST GUARD IS HERE AND NOT AT THE CALL SITE, WHICH IS `MatchDirector.AddScore`'S
        /// ARGUMENT APPLIED ONE FEATURE ALONG. A client that could spend a tsinelas is a client
        /// that can eliminate a rival, and `RoundDirector.Tagged` is raised on every peer that
        /// receives a tag rather than only on the host.
        /// </summary>
        private void OnTagged(int defenderSlot, int attackerSlot)
        {
            if (!_live || _decided) return;
            if (!NetAuthority.ShouldResolve()) return;
            if (attackerSlot < 0 || attackerSlot >= _stocks.Length) return;
            if (attackerSlot == _defenderSlot) return;
            if (_stocks[attackerSlot] <= 0) return;

            _stocks[attackerSlot]--;
            StocksChanged?.Invoke();

            if (_stocks[attackerSlot] <= 0) Eliminate(attackerSlot);

            Broadcast();

            if (CustomGameRules.RoundIsDecided(_stocks, _defenderSlot)) Decide();
        }

        /// <summary>
        /// ⚠️ `RoundActive` IS THE SWITCH, NOT `FreezeForMatchEnd`. `CharacterMotor.CanAct` is
        /// `RoundActive && !IsStunned`, so clearing it stops the throw, the grab and the reset
        /// channel, and `IsTaggable` returns false with it, so a player who is out cannot be
        /// tagged again and cannot be charged a tsinelas they do not have. The freeze is for the
        /// end of a MATCH and parks `InputIntent` permanently, which would leave the body unable
        /// to move for the rest of the game rather than for the rest of the round.
        ///
        /// ⚠️ THEY CAN STILL WALK, AND THAT IS DELIBERATE. `CanMove` is `!IsStunned` and does not
        /// read `RoundActive`, so an eliminated attacker watches the rest of the round from their
        /// own feet instead of from a locked camera. A player frozen in place for up to a minute
        /// with no explanation is the format's worst moment, and `CLAUDE.md` § 6.3's dead end.
        /// </summary>
        private void Eliminate(int slot)
        {
            var motor = GameServices.Round?.PlayerAt(slot);
            if (motor != null) motor.RoundActive = false;

            AttackerEliminated?.Invoke(slot);
        }

        /// <summary>
        /// ⚠️⚠️ THE AWARD COMES BEFORE `BeginIntermission` AND THE ORDER IS LOAD-BEARING.
        /// `BeginIntermission` sets `MatchDirector.IsWarmupBuffer`, and `AddScore` returns early
        /// on exactly that flag, so paying the winner afterwards pays nobody, silently, on the one
        /// award the whole format exists to make.
        ///
        /// ⚠️ AND THE ROUND ENDS THE SAME WAY THE CLOCK ENDS IT. `RoundDirector.FixedUpdate` does
        /// `EndRound` then `BeginIntermission`; anything else here would be a second way for a
        /// round to finish, and the intermission card, the buffer skip vote and `SliceRunner`'s
        /// advance all hang off that pair.
        /// </summary>
        private void Decide()
        {
            if (_decided) return;
            _decided = true;

            int winner = CustomGameRules.LastAttackerStanding(_stocks, _defenderSlot);

            // ⚠️ NOBODY LEFT MEANS THE ROUND BELONGS TO THE TAYA, AND THE TAYA IS PAID NOTHING
            // EXTRA FOR IT. `CustomGameRules.LastAttackerStanding` says the round is theirs; it
            // does not say it is worth 100. The taya has already been paid a `Tag` for each of
            // the three attackers they put out, which is 300, and paying a fourth award on top
            // would make clearing the court worth more than the format's own headline prize.
            if (winner >= 0) GameServices.Match?.AddScore(winner, ScoreEvent.LastTsinelasStanding);

            RoundDecided?.Invoke(winner);

            GameServices.Round?.EndRound();
            GameServices.Match?.BeginIntermission();
        }

        private void Broadcast() => Net.MatchRpc.Instance?.BroadcastTsinelas(_stocks);

        /// <summary>
        /// A stock table the HOST computed, applied on this peer.
        ///
        /// ⚠️⚠️ IT SWITCHES BODIES OFF AS WELL AS DRAWING NUMBERS, WHICH IS THE HALF A HUD-ONLY
        /// REPLICATION WOULD HAVE MISSED. A client that only drew the counter would show an
        /// eliminated player a correct "0 LEFT" while their own body still threw, grabbed and
        /// charged a reset, every one of which the host then ignored. That is worse than a wrong
        /// number: it is a player being told the game is broken.
        ///
        /// ⚠️ AND IT NEVER RE-ENABLES A BODY. `RoundDirector.BeginRound` sets `RoundActive` true
        /// on every player at the whistle and is the only thing that should; a snapshot that
        /// raised the flag back up would resurrect an eliminated attacker on any packet that
        /// arrived out of order.
        /// </summary>
        public void ApplyNetworkStocks(int[] stocks)
        {
            if (stocks == null) return;

            _live = true;
            _defenderSlot = GameServices.Match != null ? GameServices.Match.DefenderSlot : -1;

            for (int slot = 0; slot < _stocks.Length && slot < stocks.Length; slot++)
            {
                _stocks[slot] = stocks[slot] < 0 ? 0 : stocks[slot];

                if (_stocks[slot] > 0 || slot == _defenderSlot) continue;

                var motor = GameServices.Round?.PlayerAt(slot);
                if (motor != null) motor.RoundActive = false;
            }

            StocksChanged?.Invoke();
        }
    }
}
