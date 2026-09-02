using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public class PartyTests
    {
        private static readonly int[] NoCooldowns = { 0, 0, 0, 0 };
        private static readonly bool[] AllSignedIn = { true, true, true, true };

        /// <summary>
        /// ⚠️⚠️ THE RULE `FUTURE.md` § 6 DEFERRED AND § 19.9 STEP 9 ASKED FOR AS A TEST: a party of
        /// four is a full match and four friends can arrange results between themselves, so a
        /// four-stack cannot queue ranked. Two and three can, because the other seats are
        /// strangers who are trying to win.
        /// </summary>
        [Fact]
        public void AFourStackCannotQueueRankedAndATwoOrThreeStackCan()
        {
            Assert.Equal(PartyRefusal.FullStackInRanked,
                         PartyRules.CanQueue(4, QueueStake.Ranked, NoCooldowns, AllSignedIn));

            Assert.Equal(PartyRefusal.None, PartyRules.CanQueue(3, QueueStake.Ranked, NoCooldowns, AllSignedIn));
            Assert.Equal(PartyRefusal.None, PartyRules.CanQueue(2, QueueStake.Ranked, NoCooldowns, AllSignedIn));
            Assert.Equal(PartyRefusal.None, PartyRules.CanQueue(1, QueueStake.Ranked, NoCooldowns, AllSignedIn));
        }

        [Fact]
        public void AFourStackIsWelcomeInQuickMatchBecauseNothingIsAtStake()
        {
            Assert.Equal(PartyRefusal.None, PartyRules.CanQueue(4, QueueStake.Casual, NoCooldowns, AllSignedIn));
        }

        [Fact]
        public void APartyCannotBeBiggerThanAMatch()
        {
            Assert.Equal(PartyRefusal.TooBig, PartyRules.CanQueue(5, QueueStake.Casual, NoCooldowns, AllSignedIn));
            Assert.Equal(Balance.PlayerCount, PartyRules.MaxSize);
            Assert.Equal(Balance.PlayerCount - 1, PartyRules.MaxRankedSize);
        }

        [Fact]
        public void OneMemberOnACooldownStopsTheWholeTicket()
        {
            var cooldowns = new[] { 0, 0, 120, 0 };
            Assert.Equal(PartyRefusal.MemberOnCooldown,
                         PartyRules.CanQueue(4, QueueStake.Casual, cooldowns, AllSignedIn));
        }

        /// <summary>
        /// ⚠️ A GUEST CAN QUICK MATCH AND CANNOT PLAY RANKED, which is `FUTURE.md` § 0.5 rule 7
        /// respected rather than bent: nothing except the ladder sits behind a login, and a ladder
        /// genuinely cannot work for an identity that only exists on one machine.
        /// </summary>
        [Fact]
        public void AGuestCanQuickMatchAndCannotPlayRanked()
        {
            var guests = new[] { true, false, true, true };

            Assert.Equal(PartyRefusal.None, PartyRules.CanQueue(3, QueueStake.Casual, NoCooldowns, guests));
            Assert.Equal(PartyRefusal.MemberNotSignedIn,
                         PartyRules.CanQueue(3, QueueStake.Ranked, NoCooldowns, guests));
        }

        [Fact]
        public void EveryRefusalSaysWhatToDoNext()
        {
            Assert.Equal("", PartyRules.RefusalLabel(PartyRefusal.None));

            foreach (PartyRefusal refusal in new[]
            {
                PartyRefusal.TooBig, PartyRefusal.FullStackInRanked,
                PartyRefusal.MemberOnCooldown, PartyRefusal.MemberNotSignedIn,
            })
            {
                Assert.False(string.IsNullOrWhiteSpace(PartyRules.RefusalLabel(refusal)));
            }

            Assert.Contains("up to three", PartyRules.RefusalLabel(PartyRefusal.FullStackInRanked));
        }

        /// <summary>
        /// ⚠️ THE QUEUE LOOKS FOR ROOM FOR ALL OF THEM AT ONCE. A party of three offered a lobby
        /// with two chairs is one person left on the menu.
        /// </summary>
        [Fact]
        public void AQueueingPartyIsOnlyOfferedALobbyWithRoomForAllOfThem()
        {
            string pool = MatchmakingRules.PoolKey(GameMode.Classic, QueueStake.Casual,
                                                   InputDevice.KeyboardMouse, PlatformFamily.Desktop, 18);

            var twoFreeChairs = new LobbyAdvert(pool, new RatingBand(1400, 1600),
                                                1500, 1500, seated: 2, capacity: 4,
                                                inProgress: false, backfilling: false, hostPlayerId: "host");

            Assert.Equal(JoinRefusal.None,
                         MatchmakingRules.Evaluate(twoFreeChairs, "me", 1500, 0.0f, pool, null,
                                                   PartyRules.SeatsNeeded(2)));

            Assert.Equal(JoinRefusal.Full,
                         MatchmakingRules.Evaluate(twoFreeChairs, "me", 1500, 0.0f, pool, null,
                                                   PartyRules.SeatsNeeded(3)));
        }
    }
}
