using System;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// PHASE 12's map rotation and map vote.
    ///
    /// ⚠️ `docs/TODO.md` § 128.2 calls this *"the cheapest unbuilt thing in the phase"* and
    /// `FUTURE.md` § 12 says why it comes before content: *"A map is the most expensive content in
    /// the game. Map rotation and a map vote are nearly free and buy most of the same freshness."*
    ///
    /// ⚠️ THREE MAPS IS THE SHIPPED COUNT (`SceneFlow.Maps`), and it is passed in rather than read
    /// so this file has no engine reference and a fourth map cannot silently change what these
    /// assert.
    /// </summary>
    public class MapRotationTests
    {
        private const int Maps = 3;

        // ---- rotation -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ A CYCLE VISITS EVERY MAP BEFORE REVISITING ANY, which is the freshness guarantee a
        /// random draw cannot make: with three maps a uniform draw replays the same one about a
        /// third of the time and the player cannot tell a repeat from a bug.
        /// </summary>
        [Fact]
        public void TheRotationVisitsEveryMapBeforeRepeatingOne()
        {
            var seen = new bool[Maps];
            int at = 0;

            for (int i = 0; i < Maps; i++)
            {
                Assert.False(seen[at], "the rotation revisited a map before finishing the cycle");
                seen[at] = true;
                at = MapRotationRules.NextInRotation(at, Maps);
            }

            Assert.Equal(0, at);
            Assert.All(seen, Assert.True);
        }

        /// <summary>⚠️ A ONE-MAP GAME IS A REAL STATE (an early build, or a custom lobby with one
        /// map allowed) and the rotation must not divide by it or index past it.</summary>
        [Fact]
        public void ARotationOverOneMapStaysOnThatMap()
        {
            Assert.Equal(0, MapRotationRules.NextInRotation(0, 1));
            Assert.Equal(0, MapRotationRules.NextInRotation(5, 1));
            Assert.Equal(0, MapRotationRules.NextInRotation(-1, 0));
        }

        /// <summary>⚠️ AN OUT-OF-RANGE CURRENT MAP STARTS THE CYCLE rather than throwing. A lobby
        /// restored from a save written before a map was removed hands this a stale index, and a
        /// crash there is a lobby nobody can open.</summary>
        [Fact]
        public void AStaleCurrentIndexRestartsTheCycleRatherThanThrowing()
        {
            Assert.Equal(0, MapRotationRules.NextInRotation(99, Maps));
            Assert.Equal(0, MapRotationRules.NextInRotation(-4, Maps));
        }

        // ---- the opening map ------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE PRE-EPOCH CLOCK, WHICH IS `CustomGameRules.MirrorIndex`'S OWN TEST APPLIED
        /// HERE. A venue machine with a flat CMOS battery boots in 2000, C# `%` keeps the sign of
        /// the left operand, and a negative week would index backwards out of the array. This is
        /// the case General Santos City is why.
        /// </summary>
        [Fact]
        public void AVenueMachineWithAFlatBatteryStillOpensOnARealMap()
        {
            for (int year = 1999; year <= 2031; year++)
            {
                int map = MapRotationRules.OpeningMap(Maps, new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc));

                Assert.InRange(map, 0, Maps - 1);
            }
        }

        /// <summary>⚠️ EVERY MACHINE AGREES WITHOUT A SERVICE, which is the whole reason it is
        /// derived from the week rather than stored. Two peers on the same UTC day must open on
        /// the same map with no wire field between them.</summary>
        [Fact]
        public void TwoMachinesOnTheSameWeekOpenOnTheSameMap()
        {
            var monday = new DateTime(2026, 9, 1, 3, 0, 0, DateTimeKind.Utc);
            var friday = new DateTime(2026, 9, 4, 21, 30, 0, DateTimeKind.Utc);

            Assert.Equal(MapRotationRules.OpeningMap(Maps, monday),
                         MapRotationRules.OpeningMap(Maps, friday));
        }

        /// <summary>And it actually moves, or it is not a rotation.</summary>
        [Fact]
        public void TheOpeningMapMovesFromWeekToWeek()
        {
            var week = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            int first = MapRotationRules.OpeningMap(Maps, week);
            int second = MapRotationRules.OpeningMap(Maps, week.AddDays(7));

            Assert.NotEqual(first, second);
        }

        // ---- the vote -------------------------------------------------------------------

        [Fact]
        public void APluralityWins()
        {
            var votes = new[] { 2, 2, 0, MapRotationRules.NoVote };

            Assert.Equal(2, MapRotationRules.TallyVote(votes, Maps, currentMap: 0));
        }

        /// <summary>
        /// ⚠️⚠️ THE TIE-BREAK THIS FEATURE LIVES OR DIES ON. The obvious rule is "lowest index
        /// wins", and it is wrong in a way no unit test catches unless somebody writes this one:
        /// a 2-2 split between the map you just played and another gives it to whichever sorts
        /// first, which is the CURRENT map half the time. `FUTURE.md` § 12 bought this to *"buy
        /// most of the same freshness"* as a new map, and a tie that replays the same street buys
        /// none of it.
        /// </summary>
        [Fact]
        public void ATiedVoteGoesToTheMapYouAreNotAlreadyOn()
        {
            var votes = new[] { 0, 0, 2, 2 };

            Assert.Equal(2, MapRotationRules.TallyVote(votes, Maps, currentMap: 0));
            Assert.Equal(0, MapRotationRules.TallyVote(votes, Maps, currentMap: 2));
        }

        /// <summary>⚠️ AND A MAJORITY CAN STILL KEEP THE MAP IT IS ON. The tie-break decides ties
        /// and nothing else: a room that loves Eskinita is allowed to stay there, it just cannot
        /// stay there by accident.</summary>
        [Fact]
        public void AMajorityMayKeepTheMapItIsAlreadyOn()
        {
            var votes = new[] { 0, 0, 0, 2 };

            Assert.Equal(0, MapRotationRules.TallyVote(votes, Maps, currentMap: 0));
        }

        /// <summary>
        /// ⚠️⚠️ NOT VOTING IS NOT A VOTE, AND `NoVote` IS -1 RATHER THAN 0 FOR THIS REASON. Zero
        /// is a real map index, so a tally that conflates "no answer" with "the first option"
        /// hands every silent lobby to Eskinita and looks exactly like a working vote.
        /// </summary>
        [Fact]
        public void AnAbstentionIsNotAVoteForTheFirstMap()
        {
            var silent = new[]
            {
                MapRotationRules.NoVote, MapRotationRules.NoVote,
                MapRotationRules.NoVote, MapRotationRules.NoVote,
            };

            Assert.Equal(MapRotationRules.NoVote, MapRotationRules.TallyVote(silent, Maps, currentMap: 1));
        }

        /// <summary>⚠️ A VOTE FOR A MAP THAT IS NOT THERE IS DISCARDED, not clamped. A clamp turns
        /// a peer on a build with four maps into a vote for map 2 on a build with three, which is
        /// a silently wrong answer rather than an absent one.</summary>
        [Fact]
        public void AVoteForAMapThisBuildDoesNotHaveIsDiscarded()
        {
            var votes = new[] { 7, 7, 1, MapRotationRules.NoVote };

            Assert.Equal(1, MapRotationRules.TallyVote(votes, Maps, currentMap: 0));
        }

        // ---- the two halves together ----------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ SILENCE FALLS THROUGH TO THE ROTATION, WHICH IS THE ONE LINE THAT MAKES THE
        /// FEATURE WORTH BUILDING. A lobby where nobody presses anything is the COMMON case: four
        /// people who have just finished a match are looking at a scoreboard, not a ballot. If
        /// silence meant "same map again", this would only ever fire for rooms already bored
        /// enough to act, which is exactly the rooms that did not need it.
        /// </summary>
        [Fact]
        public void ASilentLobbyStillGetsAFreshMap()
        {
            var silent = new[]
            {
                MapRotationRules.NoVote, MapRotationRules.NoVote,
                MapRotationRules.NoVote, MapRotationRules.NoVote,
            };

            int next = MapRotationRules.Decide(silent, Maps, currentMap: 0);

            Assert.NotEqual(0, next);
            Assert.Equal(MapRotationRules.NextInRotation(0, Maps), next);
        }

        [Fact]
        public void AVotedLobbyGetsWhatItVotedFor()
        {
            var votes = new[] { 2, 2, 2, MapRotationRules.NoVote };

            Assert.Equal(2, MapRotationRules.Decide(votes, Maps, currentMap: 0));
        }

        /// <summary>⚠️ EVERY PEER MUST COMPUTE THE SAME WINNER FROM THE SAME BALLOT, because every
        /// peer DRAWS it. A host-only answer would be correct and still show the wrong map in
        /// three lobbies until the next sync corrected it.</summary>
        [Fact]
        public void TheSameBallotAlwaysProducesTheSameWinner()
        {
            var votes = new[] { 1, 2, 2, 1 };

            int first = MapRotationRules.Decide(votes, Maps, currentMap: 0);

            for (int i = 0; i < 50; i++)
                Assert.Equal(first, MapRotationRules.Decide(votes, Maps, currentMap: 0));
        }

        // ---- when to stop waiting -------------------------------------------------------

        /// <summary>⚠️ IT COUNTS SEATS, NOT VOTES. A room of two would otherwise always wait the
        /// full twenty seconds for two seats that can never answer.</summary>
        [Fact]
        public void AFullRoomStopsTheClockAndAnEmptySeatDoesNotHoldIt()
        {
            var two = new[] { 1, 2, MapRotationRules.NoVote, MapRotationRules.NoVote };

            Assert.True(MapRotationRules.EveryoneHasVoted(two, occupiedSeats: 2));
            Assert.False(MapRotationRules.EveryoneHasVoted(two, occupiedSeats: 4));
        }

        [Fact]
        public void AVoteWindowIsShorterThanABotFillWait()
        {
            Assert.True(MapRotationRules.VoteSeconds < BotFillRules.CasualFillAfterSeconds,
                "a map vote must not hold a room longer than waiting for a real player does");
        }
    }
}
