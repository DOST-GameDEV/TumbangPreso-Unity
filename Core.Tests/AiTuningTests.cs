using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Asserts the three bot tiers against `ai_controller.gd`'s own `DIFFICULTY_TIERS`.
    ///
    /// ⚠️ THESE ARE TRANSCRIPTION TESTS, NOT BEHAVIOUR TESTS. They cannot tell you a bot
    /// plays well; they tell you the numbers it plays with are the numbers that were tuned.
    /// That is the failure this port keeps having — a system gets rebuilt with plausible
    /// values and nothing notices, because plausible values still produce a bot that moves.
    /// </summary>
    public class AiTuningTests
    {
        [Fact]
        public void BataIsTheSlowestToReactAndTheMostMistaken()
        {
            var bata = AiTuning.For(Difficulty.Bata);

            Assert.Equal(0.55f, bata.React);
            Assert.Equal(0.34f, bata.Think);
            Assert.Equal(0.30f, bata.Mistake);

            // ⚠️ BATA DOES NOT LEAD ITS TARGET AT ALL. Zero is the tuned value, not a gap.
            Assert.Equal(0.00f, bata.Lead);

            // ⚠️ AND IT NEVER SETTLES ITS AIM. 99.0 is a sentinel meaning "no patient shot".
            Assert.Equal(99.0f, bata.AimSettle);
        }

        [Fact]
        public void NormalIsTheShippedDefault()
        {
            var n = AiTuning.For(Difficulty.Normal);

            Assert.Equal(0.30f, n.React);
            Assert.Equal(0.45f, n.Lead);
            Assert.Equal(1.45f, n.AimError);
            Assert.Equal(1.18f, n.PowerMargin);
            Assert.Equal(2.6f, n.LungeRange);
            Assert.Equal(34.0f, n.LungeCone);
            Assert.Equal(0.10f, n.Mistake);
        }

        [Fact]
        public void AstigIsFastestAndStrictestButStillErrs()
        {
            var a = AiTuning.For(Difficulty.Astig);

            Assert.Equal(0.14f, a.React);
            Assert.Equal(0.85f, a.Lead);
            Assert.Equal(1.32f, a.PowerMargin);

            // A SMALLER cone is stricter, so Astig's is the tightest of the three.
            Assert.Equal(28.0f, a.LungeCone);

            // ⚠️ NOT ZERO. A bot that never errs reads as a cheat rather than as a hard one.
            Assert.Equal(0.02f, a.Mistake);
            Assert.True(a.Mistake > 0.0f);
        }

        [Fact]
        public void DifficultyIsMonotonicWhereItShouldBe()
        {
            var bata = AiTuning.For(Difficulty.Bata);
            var normal = AiTuning.For(Difficulty.Normal);
            var astig = AiTuning.For(Difficulty.Astig);

            // Harder reacts sooner, thinks sooner, leads more, errs less.
            Assert.True(astig.React < normal.React && normal.React < bata.React);
            Assert.True(astig.Think < normal.Think && normal.Think < bata.Think);
            Assert.True(astig.Lead > normal.Lead && normal.Lead > bata.Lead);
            Assert.True(astig.Mistake < normal.Mistake && normal.Mistake < bata.Mistake);

            // And aims tighter: AimError is scatter, so lower is better.
            Assert.True(astig.AimError < normal.AimError && normal.AimError < bata.AimError);
        }

        [Fact]
        public void EveryTierRespectsTheKeyboardLungeConeFloor()
        {
            // ⚠️ THE FLOOR IS SET BY THE EIGHT-WAY HEADING, NOT BY TASTE. A cone under 26°
            // asks a bot to hit an angle it has no key for.
            foreach (Difficulty tier in new[] { Difficulty.Bata, Difficulty.Normal, Difficulty.Astig })
                Assert.True(AiTuning.EffectiveLungeCone(tier) >= AiTuning.LungeConeFloor);

            // Astig's own 28 is above the floor, so it must survive unchanged.
            Assert.Equal(28.0f, AiTuning.EffectiveLungeCone(Difficulty.Astig));
        }

        [Fact]
        public void ArriveSlopIsTheGodotValueNotTheEarlierUnityGuess()
        {
            // An earlier Unity pass used 0.35 here. This is the number the .gd actually has,
            // and the gap is what made bots jitter on arrival instead of settling.
            Assert.Equal(0.55f, AiTuning.ArriveSlop);
        }

        [Fact]
        public void EightWayThresholdIsSinOfTwentyTwoPointFiveDegrees()
        {
            // 0.3827 is sin(22.5°) — the half-angle of a 45° compass sector.
            Assert.Equal(0.3827f, AiTuning.EightWayThreshold);
        }
    }
}

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The speed-zone stack, against `character_base.gd::_recompute_speed_multiplier()`.
    /// </summary>
    public class SpeedZoneStackTests
    {
        [Fact]
        public void TwoEqualSlowsDoNotCompound()
        {
            var stack = new SpeedZoneStack();
            stack.Enter(0.5f);
            stack.Enter(0.5f);

            // ⚠️ 0.5, NOT 0.25. The .gd takes minf across the stack. This implementation took
            // the product until 2026-08-15, which turned a pair of overlapping hazards into a
            // near-stun nobody tuned.
            Assert.Equal(0.5f, stack.Value, 3);
        }

        [Fact]
        public void FatigueAndAHazardTakeTheWorseOfTheTwoNotBoth()
        {
            var stack = new SpeedZoneStack();
            stack.Enter(Balance.FatigueSpeedScale);   // 0.75
            stack.Enter(0.5f);

            // Godot gives 0.5 here. The product would have given 0.375.
            Assert.Equal(0.5f, stack.Value, 3);
        }

        [Fact]
        public void ExitRemovesOneInstanceNotAllMatching()
        {
            var stack = new SpeedZoneStack();
            stack.Enter(0.5f);
            stack.Enter(0.5f);
            stack.Exit(0.5f);

            Assert.Equal(1, stack.Count);
            Assert.Equal(0.5f, stack.Value, 3);
        }

        [Fact]
        public void AnEmptyStackIsUnslowed()
        {
            Assert.Equal(1.0f, new SpeedZoneStack().Value, 3);
        }
    }
}

namespace TumbangPreso.Core.Tests
{
    /// <summary>The per-bot roll. These assert REPRODUCIBILITY, which is the property the
    /// whole fairness measurement rests on.</summary>
    public class AiPersonalityRollTests
    {
        [Fact]
        public void TheSameSeatRollsTheSameBotEveryTime()
        {
            var a = new AiPersonalityRoll(2);
            var b = new AiPersonalityRoll(2);

            // ⚠️ IF THIS EVER FAILS, EVERY BALANCE MEASUREMENT TAKEN AGAINST BOTS IS NOISE.
            Assert.Equal(a.Tempo, b.Tempo);
            Assert.Equal(a.Hands, b.Hands);
            Assert.Equal(a.Nerves, b.Nerves);
            Assert.Equal(a.NerveForTheBox, b.NerveForTheBox);
            Assert.Equal(a.HomeBearing, b.HomeBearing);
            Assert.Equal(a.Hesitation, b.Hesitation);
        }

        [Fact]
        public void DifferentSeatsAreDifferentPeople()
        {
            var a = new AiPersonalityRoll(0);
            var b = new AiPersonalityRoll(1);

            // Three bots that behave identically read as one bot copied three times.
            Assert.True(a.Tempo != b.Tempo || a.Hands != b.Hands || a.Hesitation != b.Hesitation);
        }

        [Fact]
        public void EveryRolledValueLandsInsideItsDocumentedRange()
        {
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
            {
                var p = new AiPersonalityRoll(seat);

                Assert.InRange(p.Tempo, 0.85f, 1.20f);
                Assert.InRange(p.Hands, 0.80f, 1.25f);
                Assert.InRange(p.Nerves, 0.85f, 1.15f);
                Assert.InRange(p.NerveForTheBox, 0.75f, 1.30f);
                Assert.InRange(p.HomeBearing, -3.1416f, 3.1416f);
                Assert.InRange(p.Hesitation, 0.05f, 0.28f);
            }
        }

        [Fact]
        public void HesitationIsNeverZero()
        {
            // A bot that switches plan on the frame the world changes is the single most
            // machine-like thing a bot does.
            for (int seat = 0; seat < Balance.PlayerCount; seat++)
                Assert.True(new AiPersonalityRoll(seat).Hesitation > 0.0f);
        }
    }
}
