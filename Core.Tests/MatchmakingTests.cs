using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The queue's arithmetic. ⚠️ THE FIRST TEST IN THIS FILE IS THE REASON THE FILE EXISTS.
    /// </summary>
    public class MatchmakingTests
    {
        private static string Pool => MatchmakingRules.PoolKey(
            GameMode.Classic, QueueStake.Casual, InputDevice.KeyboardMouse, PlatformFamily.Desktop, 18);

        private static LobbyAdvert Advert(int bandLow, int bandHigh, int seatedLow, int seatedHigh,
                                          int seated = 1, bool inProgress = false, bool backfilling = false,
                                          string host = "host", string pool = null)
            => new LobbyAdvert(pool ?? Pool, new RatingBand(bandLow, bandHigh), seatedLow, seatedHigh,
                               seated, 4, inProgress, backfilling, host);

        // ------------------------------------------------------------------------------
        // The metric
        // ------------------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE CASE `FUTURE.md` § 7 NAMES, WORD FOR WORD: "A lobby with one 1400 and three
        /// 900s is a bad match even though every team-based fairness formula calls it balanced."
        ///
        /// This asserts BOTH halves of that sentence, because only asserting the first half would
        /// leave the next person free to "simplify" the spread check into the team formula and
        /// still see green. The team formula's best split here is 1400+900 against 900+900, a gap
        /// of 250, which is inside what shipping team games accept. The spread is 500.
        /// </summary>
        [Fact]
        public void ALobbyOfOne1400AndThreeNineHundredsIsBadBySpreadAndBalancedByTeamFairness()
        {
            var ratings = new[] { 1400, 900, 900, 900 };

            Assert.Equal(500, MatchmakingRules.Spread(ratings));

            // The team-based formula, which this game deliberately does not use.
            double gap = MatchmakingRules.BestTeamSplitGap(ratings);
            Assert.Equal(250.0, gap, 3);

            // And the consequence. The room of three 900s is refused at every width short of the
            // widest, however the refusal is reached.
            var room = Advert(bandLow: 800, bandHigh: 1000, seatedLow: 900, seatedHigh: 900, seated: 3);
            for (float t = 0.0f; t < MatchmakingRules.SecondsToWidest; t += 5.0f)
            {
                Assert.NotEqual(JoinRefusal.None,
                                MatchmakingRules.Evaluate(room, "me", 1400, t, Pool, null));
            }

            // ⚠️ AND THE SPREAD IS THE CHECK THAT DOES IT WHEN NOTHING ELSE WOULD. Here both
            // bands are satisfied: the room advertises 1200 to 1600, which contains the 1400, and
            // the 1400's own 200-wide band contains the room's centre. A matchmaker that only
            // compared bands would seat them. The 900 already in the room is what makes it a bad
            // match, and only the spread can see that.
            var bandsAgree = Advert(bandLow: 1200, bandHigh: 1600, seatedLow: 900, seatedHigh: 1400, seated: 3);
            Assert.Equal(JoinRefusal.SpreadTooWide,
                         MatchmakingRules.Evaluate(bandsAgree, "me", 1400, 15.0f, Pool, null));

            Assert.True(gap < MatchmakingRules.MaxAcceptableSpread(15.0f),
                        "the team gap sits inside the width the spread check refuses, which is the whole point");
        }

        [Fact]
        public void FourEqualRatingsAreAPerfectMatchAndAnEmptyRoomTakesAnybody()
        {
            Assert.Equal(0, MatchmakingRules.Spread(new[] { 1200, 1200, 1200, 1200 }));

            // seatedLow > seatedHigh is how an empty room is expressed.
            Assert.Equal(0, MatchmakingRules.SpreadWith(int.MaxValue, int.MinValue, 1400));
        }

        // ------------------------------------------------------------------------------
        // Widening
        // ------------------------------------------------------------------------------

        [Fact]
        public void TheBandStartsAtAHundredWidensEveryFifteenSecondsAndStopsAtFiveHundred()
        {
            Assert.Equal(100, MatchmakingRules.HalfWidthAt(0.0f));
            Assert.Equal(100, MatchmakingRules.HalfWidthAt(14.9f));
            Assert.Equal(200, MatchmakingRules.HalfWidthAt(15.0f));
            Assert.Equal(300, MatchmakingRules.HalfWidthAt(30.0f));
            Assert.Equal(500, MatchmakingRules.HalfWidthAt(60.0f));

            // And it stops rather than growing for ever.
            Assert.Equal(500, MatchmakingRules.HalfWidthAt(600.0f));
            Assert.True(MatchmakingRules.TakesAnybody(60.0f));
            Assert.False(MatchmakingRules.TakesAnybody(59.9f));
        }

        /// <summary>
        /// ⚠️ THE WIDENING HAS TO BE VISIBLE, per `FUTURE.md` § 7: "show the widening, so a long
        /// queue reads as progress rather than as a hang". These are the two things the surface
        /// draws, and asserting them here is what stops the label being retyped in a MonoBehaviour.
        /// </summary>
        [Fact]
        public void TheSearchLabelSaysTheBandAndTheProgressBarFillsAndStays()
        {
            Assert.Equal("Searching 1400 to 1600 skill", MatchmakingRules.SearchLabel(1500, 0.0f));
            Assert.Equal("Searching 1300 to 1700 skill", MatchmakingRules.SearchLabel(1500, 15.0f));
            Assert.Equal("Searching every skill level", MatchmakingRules.SearchLabel(1500, 120.0f));

            Assert.Equal(0.0f, MatchmakingRules.WideningProgress(0.0f));
            Assert.True(MatchmakingRules.WideningProgress(30.0f) > 0.0f);
            Assert.Equal(1.0f, MatchmakingRules.WideningProgress(60.0f));
            Assert.Equal(1.0f, MatchmakingRules.WideningProgress(6000.0f));
        }

        /// <summary>
        /// ⚠️⚠️ THE PROMISE THE GAME HAS NEVER SAID OUT LOUD. `FUTURE.md` § 7 and
        /// `INSPIRATION.md` § 4.5. It is asserted rather than trusted because a sentence in a
        /// label is one careless edit from becoming a different claim, and this one is a statement
        /// about the rules: the taya role is derived `(round - 1) % 4`, so "everyone defends
        /// exactly once" is true by construction.
        /// </summary>
        [Fact]
        public void TheQueueSaysTheTayaRotatesAndEveryoneDefendsOnce()
        {
            string promise = MatchmakingRules.TayaRotationPromise;

            Assert.Contains("taya rotates", promise);
            Assert.Contains("everyone defends exactly once", promise);
            Assert.Contains("bad first round is not a lost match", promise);
        }

        // ------------------------------------------------------------------------------
        // Pools
        // ------------------------------------------------------------------------------

        [Fact]
        public void PoolsAreSeparatedByModeStakeInputDeviceAndPlatform()
        {
            string desktopKeyboard = MatchmakingRules.PoolKey(
                GameMode.Classic, QueueStake.Casual, InputDevice.KeyboardMouse, PlatformFamily.Desktop, 18);

            Assert.NotEqual(desktopKeyboard, MatchmakingRules.PoolKey(
                GameMode.HeroStrike, QueueStake.Casual, InputDevice.KeyboardMouse, PlatformFamily.Desktop, 18));
            Assert.NotEqual(desktopKeyboard, MatchmakingRules.PoolKey(
                GameMode.Classic, QueueStake.Ranked, InputDevice.KeyboardMouse, PlatformFamily.Desktop, 18));
            Assert.NotEqual(desktopKeyboard, MatchmakingRules.PoolKey(
                GameMode.Classic, QueueStake.Casual, InputDevice.Gamepad, PlatformFamily.Desktop, 18));
            Assert.NotEqual(desktopKeyboard, MatchmakingRules.PoolKey(
                GameMode.Classic, QueueStake.Casual, InputDevice.KeyboardMouse, PlatformFamily.Mobile, 18));

            // ⚠️ And a build that would be refused at connection approval is never offered.
            Assert.NotEqual(desktopKeyboard, MatchmakingRules.PoolKey(
                GameMode.Classic, QueueStake.Casual, InputDevice.KeyboardMouse, PlatformFamily.Desktop, 17));

            var otherPool = Advert(1400, 1600, 1500, 1500, pool: "v18.HeroStrike.Casual.KeyboardMouse.Desktop");
            Assert.Equal(JoinRefusal.WrongPool,
                         MatchmakingRules.Evaluate(otherPool, "me", 1500, 0.0f, desktopKeyboard, null));
        }

        // ------------------------------------------------------------------------------
        // The decision
        // ------------------------------------------------------------------------------

        [Fact]
        public void BothBandsHaveToContainTheOtherOrTheWideningOnlyAppliesToWhoeverWaitedLonger()
        {
            // A host that has been queuing two minutes advertises a very wide band.
            var patientHost = Advert(1000, 2000, seatedLow: 1500, seatedHigh: 1500);

            // A player four seconds in is still asking for plus or minus 100 and must not be
            // swallowed by it.
            Assert.Equal(JoinRefusal.OutsideOurBand,
                         MatchmakingRules.Evaluate(patientHost, "me", 1000, 4.0f, Pool, null));

            // The same player one minute later takes anybody, which is the schedule working.
            Assert.Equal(JoinRefusal.None,
                         MatchmakingRules.Evaluate(patientHost, "me", 1000, 60.0f, Pool, null));
        }

        [Fact]
        public void ABlockedHostIsNeverJoinedAtAnyBandWidth()
        {
            var advert = Advert(0, 4000, 1500, 1500, host: "bully");

            bool IsBlocked(string id) => id == "bully";

            Assert.Equal(JoinRefusal.Blocked,
                         MatchmakingRules.Evaluate(advert, "me", 1500, 0.0f, Pool, IsBlocked));

            // ⚠️ AND STILL BLOCKED AT THE WIDEST BAND. A block that expires after seventy-five
            // seconds of queuing is not a block. `FUTURE.md` § 6.
            Assert.Equal(JoinRefusal.Blocked,
                         MatchmakingRules.Evaluate(advert, "me", 1500, 600.0f, Pool, IsBlocked));
        }

        [Fact]
        public void OurOwnLobbyIsNeverAMatchAndAFullRoomIsNotEither()
        {
            var mine = Advert(1400, 1600, 1500, 1500, host: "me");
            Assert.Equal(JoinRefusal.OurOwn, MatchmakingRules.Evaluate(mine, "me", 1500, 0.0f, Pool, null));

            var full = Advert(1400, 1600, 1500, 1500, seated: 4);
            Assert.Equal(JoinRefusal.Full, MatchmakingRules.Evaluate(full, "me", 1500, 0.0f, Pool, null));
        }

        /// <summary>
        /// ⚠️ BACKFILL: "a match that loses a player advertises the seat rather than dying"
        /// (`FUTURE.md` § 7). A running match is refused UNLESS it has said it is backfilling,
        /// which is what stops the queue dropping people into the middle of a match that has all
        /// four chairs full and simply has not updated its counts yet.
        /// </summary>
        [Fact]
        public void ARunningMatchIsJoinableOnlyWhenItHasAdvertisedTheSeat()
        {
            var quiet = Advert(1400, 1600, 1500, 1500, seated: 3, inProgress: true, backfilling: false);
            Assert.Equal(JoinRefusal.InProgressWithNoSeat,
                         MatchmakingRules.Evaluate(quiet, "me", 1500, 0.0f, Pool, null));

            var backfilling = Advert(1400, 1600, 1500, 1500, seated: 3, inProgress: true, backfilling: true);
            Assert.Equal(JoinRefusal.None,
                         MatchmakingRules.Evaluate(backfilling, "me", 1500, 0.0f, Pool, null));
        }

        [Fact]
        public void TheTightestSpreadWinsAndOccupancyOnlyBreaksATie()
        {
            var loose = Advert(1300, 1700, seatedLow: 1300, seatedHigh: 1500, seated: 3, host: "loose");
            var tight = Advert(1400, 1600, seatedLow: 1500, seatedHigh: 1500, seated: 1, host: "tight");
            var tightAndFuller = Advert(1400, 1600, seatedLow: 1500, seatedHigh: 1500, seated: 3, host: "fuller");

            var list = new List<LobbyAdvert> { loose, tight, tightAndFuller };
            int best = MatchmakingRules.Best(list, "me", 1500, 20.0f, Pool, null);

            Assert.Equal(2, best);   // same spread as `tight`, more people in it
        }

        [Fact]
        public void NothingJoinableIsMinusOneRatherThanAnException()
        {
            Assert.Equal(-1, MatchmakingRules.Best(null, "me", 1500, 0.0f, Pool, null));
            Assert.Equal(-1, MatchmakingRules.Best(new List<LobbyAdvert>(), "me", 1500, 0.0f, Pool, null));
        }
    }
}
