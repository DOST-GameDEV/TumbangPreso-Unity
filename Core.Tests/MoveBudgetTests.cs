using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The movement budget, driven adversarially.
    ///
    /// ⚠️⚠️ THE BUG THESE WERE WRITTEN AGAINST WAS REACHABLE AND SERIOUS: THE OLD ALLOWANCE
    /// RENEWED PER PACKET, SO PACKET FREQUENCY BOUGHT DISTANCE. `MatchRpc.AcceptMove` added
    /// `MoveBaseLeeway` (0.85 m) to every accepted message at whatever interval the client chose,
    /// so a client submitting its transform in a tight loop could cross the 14 m arena many times
    /// a second with every individual step "plausible". `docs/TODO.md` § 149.1 and
    /// <see cref="MoveBudget"/>'s own header carry the arithmetic.
    ///
    /// ⚠️⚠️ THESE RUN IN `Core.Tests` AND THAT IS THE POINT. The same adversarial sequences
    /// through a real transport are two built players, a shaped link and fifteen minutes; here
    /// they are a loop over a `double` and they run inside the 40 ms `CLAUDE.md` § 2.1b says to
    /// spend freely. A gate you can afford to run is a gate somebody runs.
    ///
    /// ⚠️ THE CLOCK IS A PARAMETER, WHICH IS WHY EVERY CASE BELOW IS DETERMINISTIC. Nothing here
    /// sleeps, nothing reads wall time, and a run on a loaded machine gets the same answer.
    /// </summary>
    public class MoveBudgetTests
    {
        /// <summary>The most a seat may legitimately have travelled in that much elapsed time.</summary>
        private static float Bound(double seconds) =>
            MoveBudget.Ceiling + (MoveBudget.MetresPerSecond * (float)seconds);

        /// <summary>
        /// Spends the budget in <paramref name="packets"/> equal steps over
        /// <paramref name="seconds"/> of host time, and returns the total distance accepted.
        /// </summary>
        private static float TotalAccepted(int packets, double seconds, float stepMetres)
        {
            var budget = new MoveBudget();
            double t = 0.0;
            double dt = seconds / packets;
            float total = 0.0f;

            for (int i = 0; i < packets; i++)
            {
                t += dt;
                if (budget.TryTravel(t, stepMetres)) total += stepMetres;
            }

            return total;
        }

        // -------------------------------------------------------------------
        // THE INVARIANT
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE ONE THAT MATTERS: **PACKET FREQUENCY MUST NOT BUY DISTANCE.** Under the old
        /// per-packet allowance this comparison came out 70 m against 4278 m for the same second
        /// of server time. A client chooses the left column; it must not choose the right one.
        /// </summary>
        [Fact]
        public void SendingMorePacketsOverTheSameServerSecondBuysNoExtraDistance()
        {
            const double second = 1.0;
            float allowed = Bound(second);

            // A step big enough that a per-packet allowance would be handing out its constant on
            // every message, which is exactly the shape the old code had.
            const float step = 0.80f;

            float atFifty = TotalAccepted(50, second, step);
            float atFiveHundred = TotalAccepted(500, second, step);
            float atFiveThousand = TotalAccepted(5000, second, step);

            Assert.True(atFifty <= allowed + 0.001f,
                $"50 packets moved {atFifty:0.0} m in one second against a bound of {allowed:0.0}.");
            Assert.True(atFiveHundred <= allowed + 0.001f,
                $"500 packets moved {atFiveHundred:0.0} m in one second against a bound of " +
                $"{allowed:0.0}. THE OLD CODE ALLOWED 453 m HERE.");
            Assert.True(atFiveThousand <= allowed + 0.001f,
                $"5000 packets moved {atFiveThousand:0.0} m in one second against a bound of " +
                $"{allowed:0.0}. THE OLD CODE ALLOWED 4278 m HERE, 300 arena widths.");

            // ⚠️ AND THEY LAND ON THE SAME NUMBER, not merely under the bound. A budget that came
            // in under the bound only because the STEP was small would pass the three assertions
            // above and still be a per-packet allowance.
            Assert.True(Math.Abs(atFifty - atFiveThousand) <= 1.0f,
                $"the same second of server time bought {atFifty:0.0} m at 50 Hz and " +
                $"{atFiveThousand:0.0} m at 5000 Hz. Frequency is still buying distance.");
        }

        /// <summary>⚠️ MANY TINY MOVES THEN A BIG ONE IS THE SAME EXPLOIT WITH A DISGUISE.</summary>
        [Fact]
        public void ManyTinyStepsDoNotBankAllowanceForALaterLeap()
        {
            var budget = new MoveBudget();
            double t = 0.0;

            // A thousand near-zero steps over a tenth of a second: under a per-packet allowance
            // each one renewed the constant while spending almost none of it.
            for (int i = 0; i < 1000; i++)
            {
                t += 0.0001;
                budget.TryTravel(t, 0.0005f);
            }

            Assert.True(budget.Available <= MoveBudget.Ceiling + 0.001f,
                $"the balance reached {budget.Available:0.00} m against a ceiling of " +
                $"{MoveBudget.Ceiling:0.00}, so near-silence is banking metres.");

            Assert.False(budget.TryTravel(t, MoveBudget.Ceiling + 1.0f),
                "a leap past the ceiling was accepted after a thousand tiny steps.");
        }

        /// <summary>⚠️ ZERO-DISTANCE PACKETS ARE FREE AND MUST STAY FREE.</summary>
        [Fact]
        public void ZeroDistancePacketsAreAcceptedAndSpendNothing()
        {
            var budget = new MoveBudget();
            budget.TryTravel(0.0, 0.0f);
            float before = budget.Available;

            for (int i = 0; i < 10000; i++)
            {
                Assert.True(budget.TryTravel(0.0, 0.0f),
                    "a standing body's unchanged transform must always be accepted.");
            }

            Assert.Equal(before, budget.Available, 4);
        }

        /// <summary>
        /// ⚠️⚠️ A REFUSAL MUST NOT REFRESH THE ALLOWANCE A LATER REQUEST SPENDS. This is the
        /// second half of § 149.1's brief, and it is the case a naive "reset the timer on every
        /// message" fix gets wrong.
        /// </summary>
        [Fact]
        public void RefusedRequestsNeitherSpendNorEarn()
        {
            var quiet = new MoveBudget();
            var noisy = new MoveBudget();

            quiet.TryTravel(0.0, 0.0f);
            noisy.TryTravel(0.0, 0.0f);

            // The noisy seat spends a second asking for the impossible, a thousand times.
            for (int i = 1; i <= 1000; i++)
            {
                double t = i / 1000.0;
                Assert.False(noisy.TryTravel(t, 10000.0f),
                    "a 10 km step was accepted, which is a different bug entirely.");
            }

            quiet.TryTravel(1.0, 0.0f);

            Assert.True(Math.Abs(quiet.Available - noisy.Available) <= 0.01f,
                $"a thousand refusals left {noisy.Available:0.00} m available against " +
                $"{quiet.Available:0.00} for a seat that said nothing. Being refused is not a way " +
                $"to top the budget up.");
        }

        /// <summary>⚠️ DUPLICATE PACKETS ARE PAID FOR TWICE, WHICH IS THE HONEST ANSWER.</summary>
        [Fact]
        public void ADuplicatedStepIsChargedTwiceRatherThanForgiven()
        {
            var budget = new MoveBudget();
            budget.TryTravel(0.0, 0.0f);

            float before = budget.Available;
            budget.TryTravel(0.0, 0.4f);
            budget.TryTravel(0.0, 0.4f);

            Assert.Equal(before - 0.8f, budget.Available, 3);
        }

        // -------------------------------------------------------------------
        // BURSTS, SILENCE AND THE CEILING
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ TWO SECONDS OF SILENCE USED TO BUY 56.85 m IN ONE PACKET, which is four arenas.
        /// `Math.Min(2.0, now - previous)` was the only bound and it bounded the RATE term rather
        /// than the total.
        /// </summary>
        [Fact]
        public void SilenceBanksAtMostTheCeiling()
        {
            var budget = new MoveBudget();
            budget.TryTravel(0.0, 0.0f);

            Assert.False(budget.TryTravel(60.0, MoveBudget.Ceiling + 0.5f),
                $"a minute of silence bought more than the {MoveBudget.Ceiling:0.0} m ceiling in " +
                $"one packet. The arena is 14 m across.");

            Assert.True(budget.TryTravel(60.0, MoveBudget.Ceiling - 0.01f),
                "and the ceiling itself has to be spendable, or a legitimate client coming back " +
                "from a stall is refused for ever.");
        }

        /// <summary>⚠️ A GENUINE STALL IS COVERED, which is what the ceiling is sized for.</summary>
        [Fact]
        public void AClientCatchingUpAfterASecondOfStallIsNotRefused()
        {
            var budget = new MoveBudget();
            budget.TryTravel(0.0, 0.0f);

            // An attacker at Speed x AttackerSpeedScale is about 2.53 m/s, and a lunge on top of
            // a second of that is well under eleven metres.
            Assert.True(budget.TryTravel(1.0, 11.0f),
                "an ordinary peer that hitched for a second and came back must not be refused. " +
                "That is what the catch-up ceiling exists for.");
        }

        // -------------------------------------------------------------------
        // HOSTILE PAYLOADS
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ NaN IS THE ONE THAT GETS THROUGH A NAIVE GUARD. In C# `distance > credit` is
        /// FALSE when distance is NaN, so a guard written the obvious way ACCEPTS it and then
        /// subtracts it, which admits the move and poisons the balance for the rest of the match.
        /// `docs/TODO.md` § 149.9.
        /// </summary>
        [Fact]
        public void NonFiniteAndNegativeDistancesAreRefusedAndPoisonNothing()
        {
            var budget = new MoveBudget();
            budget.TryTravel(0.0, 0.0f);
            float before = budget.Available;

            foreach (float hostile in new[]
                     {
                         float.NaN, float.PositiveInfinity, float.NegativeInfinity,
                         -1.0f, -0.0001f, float.MaxValue, float.MinValue,
                     })
            {
                Assert.False(budget.TryTravel(0.0, hostile),
                    $"a claimed step of {hostile} was accepted.");

                Assert.True(Math.Abs(before - budget.Available) < 0.0001f,
                    $"a claimed step of {hostile} changed the balance from {before} to " +
                    $"{budget.Available}. A refused request must cost nothing, and a NaN " +
                    $"subtraction makes every later comparison false as well.");
            }

            Assert.True(budget.TryTravel(0.0, 0.5f),
                "and an ordinary step still works afterwards, which is the half that proves the " +
                "balance was not poisoned.");
        }

        /// <summary>⚠️ A CLOCK THAT WENT BACKWARDS CREDITS NOTHING RATHER THAN GOING NEGATIVE.</summary>
        [Fact]
        public void AClockThatGoesBackwardsDoesNotDestroyTheBalance()
        {
            var budget = new MoveBudget();
            budget.TryTravel(10.0, 0.0f);
            float before = budget.Available;

            budget.TryTravel(5.0, 0.0f);

            Assert.Equal(before, budget.Available, 4);
        }

        // -------------------------------------------------------------------
        // LIFECYCLE
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ A CHAIR CHANGING HANDS STARTS THE NEW OWNER ON THE BURST, NOT ON A DRAINED BUDGET
        /// AND NOT ON THE CEILING. `MatchRpc.HostTakeSeatBackFromBot` drops the per-seat movement
        /// window for the same reason: an arriving player must not be refused for the bot's
        /// spending, and must not inherit a bank either.
        /// </summary>
        [Fact]
        public void ForgettingASeatStartsTheNextOwnerOnTheBurst()
        {
            var budget = new MoveBudget();
            budget.TryTravel(0.0, 0.0f);

            while (budget.TryTravel(0.0, 0.1f)) { }
            Assert.True(budget.Available < 0.11f, "the fixture needs the budget actually drained.");

            budget.Forget();

            Assert.True(budget.TryTravel(100.0, MoveBudget.BurstMetres - 0.01f),
                "a fresh owner gets the burst.");
            Assert.False(budget.TryTravel(100.0, MoveBudget.Ceiling),
                "and not the ceiling: a reconnect is not a free 28 m.");
        }

        /// <summary>
        /// ⚠️ THE SUSTAINED RATE COMFORTABLY COVERS THE FASTEST LEGITIMATE MOVEMENT. The budget
        /// has to be invisible to a real player or it is a new bug rather than a fix.
        /// </summary>
        [Fact]
        public void TheSustainedRateComfortablyCoversTheFastestLegitimateMovement()
        {
            Assert.True(MoveBudget.MetresPerSecond > Balance.LungeSpeed * 2.0f,
                $"the budget's rate is {MoveBudget.MetresPerSecond} m/s and the fastest impulse " +
                $"in the game is LungeSpeed {Balance.LungeSpeed:0.000} m/s. Any margin under 2x " +
                $"would start refusing real players on a bad frame.");

            var budget = new MoveBudget();
            budget.TryTravel(0.0, 0.0f);

            // Ten seconds of a body moving flat out at lunge speed, submitted at the physics rate.
            double t = 0.0;
            int refused = 0;
            for (int i = 0; i < 500; i++)
            {
                t += 0.02;
                if (!budget.TryTravel(t, Balance.LungeSpeed * 0.02f)) refused++;
            }

            Assert.True(refused == 0,
                $"{refused} of 500 honest steps at lunge speed were refused.");
        }
    }
}
