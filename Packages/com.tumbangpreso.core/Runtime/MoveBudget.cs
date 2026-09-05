using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// How far one seat is allowed to claim it has moved, as a function of TIME rather than of
    /// how many packets it sent.
    ///
    /// ⚠️⚠️ THE FAULT THIS REPLACES: THE OLD ALLOWANCE WAS PER PACKET, SO SENDING MORE PACKETS
    /// BOUGHT MORE DISTANCE. `MatchRpc.AcceptMove` computed
    ///
    ///     allowance = MoveBaseLeeway + MoveMaxMetresPerSecond * (now - lastAccepted)
    ///
    /// and compared ONE packet's step against it. The rate term is honest; the constant is not.
    /// `MoveBaseLeeway` is <see cref="BurstMetres"/>, and it was added to **every accepted
    /// packet**, at whatever interval the client chose. So over one second:
    ///
    /// | Submit rate | Per-packet allowance | Distance bought in one second |
    /// |---|---|---|
    /// | 50 Hz, the physics rate | 0.85 + 28 x 0.020 = 1.41 m | **70.5 m** |
    /// | 500 Hz | 0.85 + 28 x 0.002 = 0.906 m | **453 m** |
    /// | 5000 Hz | 0.85 + 28 x 0.0002 = 0.856 m | **4278 m** |
    ///
    /// The arena is 14 m across. A client that simply submitted its transform in a tight loop
    /// could cross it many times a second, every step individually "plausible", and every one of
    /// them accepted by the host as the authoritative position of that body. `docs/TODO.md`
    /// § 149.1.
    ///
    /// ⚠️⚠️ AND THE FIX IS NOT A PACKET-RATE LIMIT. A rate limit answers "how often may you
    /// speak", and the question is "how far may you have gone". A limit tuned for 50 Hz breaks a
    /// client that legitimately submits at 60 or 144, and one tuned loosely still multiplies the
    /// budget by however much slack it left. **The budget is a quantity of METRES that accrues
    /// with the host's own clock and is spent by accepted movement**, so the number of packets
    /// it is spent in cannot change the total.
    ///
    /// **The invariant, and it is what `MoveBudgetTests` asserts:** over any interval of
    /// authoritative elapsed time T, the total distance this seat can have accepted is at most
    /// <see cref="Ceiling"/> + <see cref="MetresPerSecond"/> x T, whatever the packet count.
    ///
    /// ⚠️⚠️ A REJECTED REQUEST SPENDS NOTHING AND EARNS NOTHING. Credit is a pure function of
    /// elapsed time, so asking and being refused cannot top the budget up: <see cref="Credit"/>
    /// after N refusals over T seconds is identical to the credit after zero refusals over T
    /// seconds. That is the second half of § 149.1's brief and it falls out of the shape rather
    /// than needing its own rule.
    ///
    /// ⚠️⚠️ NaN IS REFUSED, AND THE COMPARISON IS WRITTEN BACKWARDS ON PURPOSE. In C# every
    /// ordinary comparison against NaN is false, so `if (distance > credit) return false;` lets
    /// a NaN THROUGH, which is the exact shape most validation guards are written in.
    /// `!(distance <= credit)` is true for NaN, so the fail-closed answer is the default one.
    /// `docs/TODO.md` § 149.9.
    ///
    /// ⚠️ ENGINE-FREE, WHICH IS WHY THE ADVERSARIAL CASES COST 40 ms. `CLAUDE.md` § 4: the rules
    /// core never references `UnityEngine`. A client sending ten thousand packets over one second
    /// of server time is a loop over a `double` here and a fifteen-minute two-process run
    /// anywhere else.
    /// </summary>
    public sealed class MoveBudget
    {
        /// <summary>
        /// The fastest a body may be claimed to travel, sustained.
        ///
        /// ⚠️ IT IS `MatchRpc.MoveMaxMetresPerSecond`'S NUMBER, MOVED HERE RATHER THAN COPIED.
        /// `Balance.Speed` is 3.6 and every impulse in the game is derived from `Friction`; the
        /// fastest single thing a body does is a lunge at `LungeSpeed` 7.746 m/s, and this is
        /// roughly three and a half times that so a legitimate client is never near it. The
        /// number was always generous; what was wrong was that it was not the only term.
        /// </summary>
        public const float MetresPerSecond = 28.0f;

        /// <summary>
        /// The burst a seat may hold, which is the term that used to renew per packet.
        ///
        /// ⚠️ IT IS STILL 0.85 m AND IT IS STILL THERE FOR THE SAME REASON: one physics step of
        /// slack so an ordinary client is never refused for arriving a frame early or late. What
        /// changed is that it is a BALANCE that is spent and refilled by the clock, not a
        /// per-message gift.
        /// </summary>
        public const float BurstMetres = 0.85f;

        /// <summary>
        /// How much of the rate a seat may bank while it is quiet.
        ///
        /// ⚠️⚠️ IT IS THE CAP THAT MAKES "A BURST AFTER SILENCE" SAFE, AND THE OLD CODE'S
        /// EQUIVALENT WAS TWO SECONDS. `Math.Min(2.0, now - previous)` allowed a single packet
        /// after a two-second gap to move 0.85 + 56 = **56.85 m**, four arenas, in one step. A
        /// client that simply stopped talking for two seconds bought that.
        ///
        /// ⚠️ ONE SECOND IS MEASURED AGAINST WHAT A REAL CLIENT NEEDS RATHER THAN PICKED. A peer
        /// that stalls (a GC hitch, an alt-tab, a wifi blip) keeps simulating locally, so it comes
        /// back up to `Speed * stall` away: an attacker at `Speed x AttackerSpeedScale` is
        /// 2.53 m/s, so a full second of stall is about 2.5 m of catching up and a lunge on top of
        /// it is under 11. The ceiling below is 28.85 m, which covers that with a wide margin and
        /// is still half of what the old code handed out for free.
        /// </summary>
        public const float CatchUpSeconds = 1.0f;

        /// <summary>The most metres this seat can ever be holding at once.</summary>
        public static float Ceiling => BurstMetres + (MetresPerSecond * CatchUpSeconds);

        private double _lastCreditedAt;
        private float _credit;
        private bool _started;

        /// <summary>Metres currently available to spend. For diagnostics and tests.</summary>
        public float Available => _credit;

        /// <summary>
        /// Bring the balance up to date for the host's clock, then answer whether this claimed
        /// step fits inside it. A step that fits is DEBITED; one that does not costs nothing.
        /// </summary>
        /// <param name="now">
        /// The HOST'S own monotonic clock, in seconds. ⚠️ Never anything a client sends. The
        /// whole point of an elapsed-time budget is that the peer being limited cannot move the
        /// clock the limit is measured against.
        /// </param>
        /// <param name="distance">How far the claimed position is from the authoritative one.</param>
        public bool TryTravel(double now, float distance)
        {
            Accrue(now);

            // ⚠️ WRITTEN AS `!(<=)` AND NOT AS `>`. See the class note: `distance > _credit` is
            // FALSE when distance is NaN, so the naive form accepts a NaN step and then subtracts
            // it, which poisons the balance for the rest of the match as well as admitting the
            // move. This form rejects NaN, both infinities and every negative.
            if (!(distance >= 0.0f) || !(distance <= _credit)) return false;

            _credit -= distance;
            return true;
        }

        /// <summary>
        /// Add the metres the clock has earned since the last time anybody asked.
        ///
        /// ⚠️⚠️ IT IS CALLED ON EVERY REQUEST, ACCEPTED OR REFUSED, AND THAT IS WHAT MAKES THE
        /// TOTAL INDEPENDENT OF THE PACKET COUNT. Accrual is `rate x elapsed`, so sampling it
        /// once or a thousand times over the same interval credits the same metres. If it ran
        /// only on an ACCEPTED move, a refused one would leave time uncredited and the next
        /// accepted one would collect it late, which is the same number by a longer road; if it
        /// added a constant, we would be back where we started.
        /// </summary>
        private void Accrue(double now)
        {
            if (!_started)
            {
                // ⚠️ A SEAT OPENS ON THE BURST AND NOT ON THE CEILING. The first packet after a
                // seat is created is an ordinary step, and starting full would hand every
                // reconnect a free 28 m.
                _started = true;
                _lastCreditedAt = now;
                _credit = BurstMetres;
                return;
            }

            double elapsed = now - _lastCreditedAt;

            // ⚠️ A CLOCK THAT WENT BACKWARDS CREDITS NOTHING RATHER THAN THROWING. The host's
            // clock is monotonic, so this is defensive; it is written down because the failure it
            // would cause (a negative credit, then everything refused) looks like a network fault
            // rather than like a clock.
            if (!(elapsed > 0.0)) elapsed = 0.0;

            _lastCreditedAt = now;
            _credit = (float)Math.Min(Ceiling, _credit + (MetresPerSecond * elapsed));
        }

        /// <summary>
        /// Forget everything about this seat, for a chair changing hands.
        ///
        /// ⚠️ A NEW OWNER DOES NOT INHERIT A DRAINED BUDGET, AND DOES NOT INHERIT A FULL ONE.
        /// `MatchRpc.HostTakeSeatBackFromBot` already drops the per-seat movement window for
        /// exactly this reason: an arriving player must not be refused because the bot that was
        /// sitting there had just been corrected, and must not be handed a bank either.
        /// </summary>
        public void Forget()
        {
            _started = false;
            _credit = 0.0f;
            _lastCreditedAt = 0.0;
        }
    }
}
