using Xunit;
using TumbangPreso.Core;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The score target: the one custom rule that can end a match before the clock does.
    ///
    /// ⚠️⚠️ IT IS IN THE CORE RATHER THAN IN `MatchDirector` BECAUSE `docs/FUTURE.md` § 19.12
    /// SAYS SO IN AS MANY WORDS: *"Every new mode adds its rules to
    /// `Packages/com.tumbangpreso.core/`, never to Unity code."* A win condition written inside a
    /// `MonoBehaviour` is a win condition that needs a scene, a frame and twelve minutes to
    /// assert; this one is asserted in about a millisecond, which is `CLAUDE.md` § 4's whole
    /// argument for the engine-free package.
    ///
    /// ⚠️ AND IT IS A SECOND END CONDITION RATHER THAN A REPLACEMENT. `MatchDirector` already
    /// ends a match when `RoundNumber > TotalRounds`; this can only ever end one EARLIER, which
    /// is why every test below also checks that a target of zero changes nothing.
    /// </summary>
    public class CustomGameScoreTargetTests
    {
        [Fact]
        public void ATargetOfZeroNeverEndsAMatch()
        {
            // ⚠️ ZERO IS "PLAY EVERY ROUND" AND IS HOW THE GAME SHIPS, which is
            // `CustomRules.ScoreTarget`'s own note. A rule that treated 0 as "the target is
            // already met" would end every standard match on its first tick.
            Assert.False(CustomGameRules.ScoreTargetReached(new[] { 9999, 0, 0, 0 }, 0));
            Assert.False(CustomGameRules.ScoreTargetReached(new[] { 0, 0, 0, 0 }, 0));
        }

        [Fact]
        public void ANegativeTargetIsTreatedAsOff()
        {
            // A rule set can arrive from the wire, and `ClampScoreTarget` bounds it at
            // `MinScoreTarget` 0 on the way in. This is the belt for that: a negative that got
            // past a clamp must read as OFF rather than as "everybody has already won".
            Assert.False(CustomGameRules.ScoreTargetReached(new[] { 0, 0, 0, 0 }, -100));
        }

        [Fact]
        public void TheTargetIsReachedTheMomentAnySeatIsAtOrAboveIt()
        {
            Assert.False(CustomGameRules.ScoreTargetReached(new[] { 400, 300, 200, 100 }, 500));
            Assert.True(CustomGameRules.ScoreTargetReached(new[] { 500, 300, 200, 100 }, 500));

            // ⚠️ AT OR ABOVE, NOT EXACTLY. Points are awarded in blocks of 100 and 300
            // (`MatchRules.PointsFor`), so a seat on 400 that lands a tag goes to 700 and never
            // equals a 500 target. An equality test would let a match run for ever.
            Assert.True(CustomGameRules.ScoreTargetReached(new[] { 100, 700, 200, 0 }, 500));
        }

        [Fact]
        public void AnySeatCanReachIt()
        {
            // Every seat, not only seat 0. The taya scores too (`MatchRules.PointsFor` pays for a
            // tag), so a rule that only watched the attackers would miss the one seat most likely
            // to run away with a short match.
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var scores = new int[Balance.PlayerCount];
                scores[slot] = 1000;

                Assert.True(CustomGameRules.ScoreTargetReached(scores, 1000));
            }
        }

        [Fact]
        public void ANullScoreboardIsNotAWin()
        {
            // ⚠️ THE SAME SHAPE `LastAttackerStanding` AND `AliveAttackers` BOTH TAKE: a null
            // list is answered rather than thrown on, because the caller is a host in the middle
            // of a round and an exception there ends the match for four people.
            Assert.False(CustomGameRules.ScoreTargetReached(null, 500));
        }

        /// <summary>
        /// ⚠️⚠️ THE SHIPPED RULE SETS ARE UNAFFECTED, AND THIS IS THE TEST THAT SAYS SO.
        /// `CustomGameRules.Defaults` sets `ScoreTarget` to 0 for both modes, so adding this
        /// condition to `MatchDirector` cannot change a single standard match. **A new end
        /// condition that could fire on the default rules would be a change to Classic**, which
        /// `docs/VISION.md` § 1.1 forbids: *"CLASSIC IS NOT HERO STRIKE WITH THE POWERS TURNED
        /// OFF ... do not let a Hero Strike balance change reach into `Balance.cs` values Classic
        /// shares without saying so out loud."*
        /// </summary>
        [Fact]
        public void TheShippedDefaultsCarryNoTargetInEitherMode()
        {
            Assert.Equal(0, CustomGameRules.Defaults(GameMode.Classic).ScoreTarget);
            Assert.Equal(0, CustomGameRules.Defaults(GameMode.HeroStrike).ScoreTarget);

            var huge = new int[Balance.PlayerCount];
            for (int i = 0; i < huge.Length; i++) huge[i] = 100000;

            Assert.False(CustomGameRules.ScoreTargetReached(
                huge, CustomGameRules.Defaults(GameMode.Classic).ScoreTarget));
        }

        /// <summary>
        /// ⚠️ THE ROUND COUNT AND THE TARGET ARE INDEPENDENT AND BOTH ARE BOUNDS ON THE HOST.
        /// A custom lobby can set twelve rounds and a 500 target, and the match ends on whichever
        /// arrives first. This asserts the clamps rather than the interaction, because the
        /// interaction lives in `MatchDirector` where a scene is needed to see it.
        /// </summary>
        [Fact]
        public void EveryCustomBoundClampsFromBothSides()
        {
            Assert.Equal(CustomGameRules.MinRounds, CustomGameRules.ClampRounds(-4));
            Assert.Equal(CustomGameRules.MaxRounds, CustomGameRules.ClampRounds(9999));

            Assert.Equal(CustomGameRules.MinRoundSeconds, CustomGameRules.ClampRoundSeconds(1));
            Assert.Equal(CustomGameRules.MaxRoundSeconds, CustomGameRules.ClampRoundSeconds(9999));

            Assert.Equal(CustomGameRules.MinScoreTarget, CustomGameRules.ClampScoreTarget(-1));
            Assert.Equal(CustomGameRules.MaxScoreTarget, CustomGameRules.ClampScoreTarget(999999));

            Assert.Equal(0, CustomGameRules.ClampBots(-1));
            Assert.Equal(CustomGameRules.MaxBots, CustomGameRules.ClampBots(99));
        }

        /// <summary>
        /// ⚠️⚠️ THE WIRE FORM SURVIVES A ROUND TRIP, WHICH IS THE ONE PROPERTY THE WHOLE FEATURE
        /// RESTS ON. `CustomGameRules.ToWire` and `Parse` are what let a lobby tell three other
        /// machines what they are about to play, and § 38.6's audit exists because netcode does
        /// not check that a writer and a reader agree: a field added to one is not an error, it
        /// is silently misread bytes.
        ///
        /// ⚠️ THE PASSWORD IS DELIBERATELY NOT ASSERTED TO SURVIVE, because it deliberately does
        /// not: `Parse` clears it and the file says why (*"a lobby advert is readable by
        /// everybody in the pool; a password in it is a lock with the key taped to the door"*).
        /// **A test that asserted it round-tripped would be asserting a security defect.**
        /// </summary>
        [Fact]
        public void ARuleSetSurvivesTheWireExceptForItsPassword()
        {
            var sent = new CustomRules
            {
                Mode = GameMode.Classic,
                Format = MatchFormat.LastTsinelas,
                Rounds = 7,
                RoundSeconds = 45,
                ScoreTarget = 1500,
                Tsinelas = 4,
                Bots = 2,
                BotDifficulty = (int)Difficulty.Astig,
                Private = true,
                Password = "hunter22",
            };

            var got = CustomGameRules.Parse(CustomGameRules.ToWire(sent), GameMode.HeroStrike);

            Assert.Equal(sent.Mode, got.Mode);
            Assert.Equal(sent.Format, got.Format);
            Assert.Equal(sent.Rounds, got.Rounds);
            Assert.Equal(sent.RoundSeconds, got.RoundSeconds);
            Assert.Equal(sent.ScoreTarget, got.ScoreTarget);
            Assert.Equal(sent.Tsinelas, got.Tsinelas);
            Assert.Equal(sent.Bots, got.Bots);
            Assert.Equal(sent.BotDifficulty, got.BotDifficulty);
            Assert.Equal(sent.Private, got.Private);

            Assert.Equal("", got.Password);
        }

        /// <summary>
        /// ⚠️⚠️ AN OLDER BUILD'S SHORTER STRING READS AS DEFAULTS RATHER THAN THROWING, which is
        /// `ToWire`'s own stated contract (*"FIELDS ARE APPENDED, NEVER INSERTED, AND A SHORT
        /// STRING IS READ AS DEFAULTS"*) and § 70.7's rule about a roster that only grows.
        /// **This is the assertion that makes that sentence true rather than intended.**
        /// </summary>
        [Fact]
        public void AShortWireStringFillsTheRestFromDefaults()
        {
            var got = CustomGameRules.Parse("0|1", GameMode.HeroStrike);
            var defaults = CustomGameRules.Defaults(GameMode.Classic);

            Assert.Equal(GameMode.Classic, got.Mode);
            Assert.Equal(MatchFormat.LastTsinelas, got.Format);
            Assert.Equal(defaults.RoundSeconds, got.RoundSeconds);
            Assert.Equal(defaults.ScoreTarget, got.ScoreTarget);
            Assert.Equal(defaults.Bots, got.Bots);

            Assert.NotNull(CustomGameRules.Parse("", GameMode.Classic));
            Assert.NotNull(CustomGameRules.Parse(null, GameMode.Classic));
            Assert.NotNull(CustomGameRules.Parse("nonsense|||", GameMode.Classic));
        }
    }
}
