using System;
using System.Linq;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// Friends, blocks and presence. `docs/TODO.md` § 102.
    ///
    /// ⚠️⚠️ A FRIEND REQUEST IS THE ONE THING IN THIS GAME A STRANGER CAN PUT IN FRONT OF YOU,
    /// so every refusal here is an anti-spam rule rather than tidiness, and the server runs the
    /// same functions (`ugs/cloud-code/social.js`). `FUTURE.md` § 0.5 rule 6: a client never
    /// writes what it owns.
    /// </summary>
    public sealed class SocialTests
    {
        private static readonly DateTime Now = new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);

        private static string Stamp(int secondsAgo)
            => Now.AddSeconds(-secondsAgo).ToString("O");

        private static FriendRef Friend(string id, PresenceState state = PresenceState.Menu,
                                        int secondsAgo = 5, string joinCode = "")
            => new FriendRef
            {
                PlayerId = id,
                Handle = id.ToUpperInvariant() + "#1234",
                Presence = state,
                SeenUtc = Stamp(secondsAgo),
                JoinCode = joinCode,
            };

        // -------------------------------------------------------------------
        // § WHO MAY BE ADDED
        // -------------------------------------------------------------------

        [Fact]
        public void YouCannotAddYourself()
        {
            var list = new SocialList();
            Assert.False(SocialRules.CanRequest(list, "me", "me"));
            Assert.Equal("that is you", SocialRules.WhyCannotRequest(list, "me", "me"));
        }

        /// <summary>
        /// ⚠️⚠️ THE MACHINE THAT HAS NEVER REACHED THE SERVICE HAS AN EMPTY PLAYER ID, AND
        /// WITHOUT `IsAddressable` EVERY ONE OF THEM WOULD BE THE SAME PERSON. `docs/TODO.md`
        /// § 97: the boot screen exists so a player can keep playing with the cable out, and that
        /// player's `PlayerAccount.PlayerId` is `""`.
        /// </summary>
        [Fact]
        public void AnUnsignedMachineCannotBefriendAnotherUnsignedMachine()
        {
            var list = new SocialList();

            Assert.False(SocialRules.CanRequest(list, "", ""));
            Assert.False(SocialRules.CanRequest(list, "", "them"));
            Assert.False(SocialRules.CanRequest(list, "me", ""));
            Assert.False(SocialRules.IsFriend(list, ""));
            Assert.False(SocialRules.IsBlocked(new SocialList { Blocked = { "" } }, ""));
        }

        [Fact]
        public void ARequestAlreadySentIsRefusedWithAReasonRatherThanSilently()
        {
            var list = new SocialList();
            list.Outgoing.Add(Friend("them"));

            Assert.Equal("request already sent", SocialRules.WhyCannotRequest(list, "me", "them"));
        }

        /// <summary>
        /// ⚠️⚠️ TWO PEOPLE ADDING EACH OTHER AT ONCE IS THE COMMONEST RACE A FRIENDS LIST HAS,
        /// and the polite resolution is that the second press accepts the first request rather
        /// than reporting a conflict. `SocialRules.WhyCannotRequest` answers empty, and the caller
        /// is expected to accept.
        /// </summary>
        [Fact]
        public void AnIncomingRequestFromThemIsNotARefusal()
        {
            var list = new SocialList();
            list.Incoming.Add(Friend("them"));

            Assert.Equal("", SocialRules.WhyCannotRequest(list, "me", "them"));
        }

        [Fact]
        public void BlockingSomebodyRefusesTheirRequestOnTheSideThatReceivesIt()
        {
            var mine = new SocialList();
            mine.Blocked.Add("pest");

            // ⚠️ THE RECIPIENT'S CHECK, WHICH IS THE ONLY ONE THAT COUNTS. A modified client
            // simply lies about its own outgoing list; the write lands here.
            Assert.False(SocialRules.AcceptsRequestFrom(mine, "pest"));
            Assert.True(SocialRules.AcceptsRequestFrom(mine, "stranger"));
        }

        /// <summary>
        /// ⚠️⚠️ TWENTY PENDING, AND THE CAP IS THE ANTI-SPAM RULE RATHER THAN A DOCUMENT SIZE.
        /// An incoming request is something a stranger creates in your document.
        /// </summary>
        [Fact]
        public void AFullInboxRefusesFurtherRequests()
        {
            var mine = new SocialList();
            for (int i = 0; i < SocialRules.MaxPending; i++)
                mine.Incoming.Add(Friend($"p{i}"));

            Assert.False(SocialRules.AcceptsRequestFrom(mine, "onemore"));
        }

        [Fact]
        public void AFullFriendsListRefusesFurtherRequestsWithItsOwnReason()
        {
            var list = new SocialList();
            for (int i = 0; i < SocialRules.MaxFriends; i++)
                list.Friends.Add(Friend($"f{i}"));

            Assert.Equal("your friends list is full",
                         SocialRules.WhyCannotRequest(list, "me", "new"));
        }

        // -------------------------------------------------------------------
        // § PRESENCE
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ PRESENCE IS A TIMESTAMP AND NOT A SOCKET, SO STALE MUST READ AS OFFLINE. Without
        /// the bound, somebody who closed the game stays lit for ever and the one thing on the
        /// screen becomes a lie the first time anybody quits.
        /// </summary>
        [Fact]
        public void APresenceOlderThanTheBoundReadsAsOffline()
        {
            var fresh = Friend("a", PresenceState.InMatch, secondsAgo: 10);
            var stale = Friend("b", PresenceState.InMatch,
                               secondsAgo: SocialRules.PresenceStaleSeconds + 10);

            Assert.Equal(PresenceState.InMatch, SocialRules.EffectivePresence(fresh, Now));
            Assert.Equal(PresenceState.Offline, SocialRules.EffectivePresence(stale, Now));
        }

        /// <summary>⚠️ A MISSING OR UNREADABLE STAMP IS OFFLINE, NEVER ONLINE. Failing towards
        /// the loud answer sends a player to an empty lobby.</summary>
        [Theory]
        [InlineData("")]
        [InlineData("not a date")]
        [InlineData("0000")]
        public void AnUnreadableTimestampReadsAsOffline(string stamp)
        {
            var friend = new FriendRef
            {
                PlayerId = "a",
                Presence = PresenceState.Menu,
                SeenUtc = stamp,
            };

            Assert.False(SocialRules.PresenceIsFresh(friend, Now));
            Assert.Equal(PresenceState.Offline, SocialRules.EffectivePresence(friend, Now));
        }

        /// <summary>
        /// ⚠️ A JOIN CODE ON AN OFFLINE FRIEND IS NOT JOINABLE. The code outlives the session
        /// that published it, so a rail that offered JOIN on a stale row would send somebody to a
        /// lobby that closed an hour ago — which reads as the game being broken rather than as
        /// the friend having left.
        /// </summary>
        [Fact]
        public void AStaleJoinCodeIsNotOfferedAsJoinable()
        {
            var live = Friend("a", PresenceState.Menu, secondsAgo: 5, joinCode: "ABCD");
            var gone = Friend("b", PresenceState.Menu,
                              secondsAgo: SocialRules.PresenceStaleSeconds + 1, joinCode: "ABCD");
            var nowhere = Friend("c", PresenceState.Menu, secondsAgo: 5);

            Assert.True(SocialRules.IsJoinable(live, Now));
            Assert.False(SocialRules.IsJoinable(gone, Now));
            Assert.False(SocialRules.IsJoinable(nowhere, Now));
        }

        /// <summary>
        /// ⚠️⚠️ `FUTURE.md` § 0.5b QUESTION 1 AS AN ASSERTION. The one thing on a friends list is
        /// who is online now; an alphabetical list buries that under forty offline names.
        /// </summary>
        [Fact]
        public void TheRailPutsJoinableFirstThenOnlineThenEverybodyElse()
        {
            var list = new[]
            {
                Friend("zoffline", PresenceState.Menu, SocialRules.PresenceStaleSeconds + 5),
                Friend("aonline", PresenceState.InMatch, 5),
                Friend("mjoinable", PresenceState.Menu, 5, "ABCD"),
                Friend("bonline", PresenceState.Menu, 5),
            }.ToList();

            var order = SocialRules.Sorted(list, Now).Select(f => f.PlayerId).ToArray();

            Assert.Equal("mjoinable", order[0]);

            // The two online ones sort between themselves by handle.
            Assert.Equal(new[] { "aonline", "bonline" }, new[] { order[1], order[2] });
            Assert.Equal("zoffline", order[3]);
        }

        /// <summary>⚠️ SORTING MAY NOT WRITE. The list it is handed is the stored document.</summary>
        [Fact]
        public void SortingDoesNotReorderTheStoredList()
        {
            var stored = new[]
            {
                Friend("zoffline", PresenceState.Menu, SocialRules.PresenceStaleSeconds + 5),
                Friend("aonline"),
            }.ToList();

            SocialRules.Sorted(stored, Now);

            Assert.Equal("zoffline", stored[0].PlayerId);
        }

        // -------------------------------------------------------------------
        // § WHAT A STORED DOCUMENT MAY HOLD
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ BLOCKING SOMEBODY YOU ARE FRIENDS WITH HAS TO END THE FRIENDSHIP, or a block is a
        /// label rather than a boundary. The same rule clears a pending request from them, which
        /// would otherwise sit in the inbox for ever with no way to act on it.
        /// </summary>
        [Fact]
        public void BlockingRemovesThemFromEveryOtherList()
        {
            var list = new SocialList();
            list.Friends.Add(Friend("pest"));
            list.Incoming.Add(Friend("pest"));
            list.Outgoing.Add(Friend("pest"));
            list.Blocked.Add("pest");

            var clean = SocialRules.Normalise(list);

            Assert.Empty(clean.Friends);
            Assert.Empty(clean.Incoming);
            Assert.Empty(clean.Outgoing);
            Assert.Single(clean.Blocked);
        }

        [Fact]
        public void ARowInBothFriendsAndPendingResolvesToFriends()
        {
            var list = new SocialList();
            list.Friends.Add(Friend("them"));
            list.Incoming.Add(Friend("them"));
            list.Outgoing.Add(Friend("them"));

            var clean = SocialRules.Normalise(list);

            Assert.Single(clean.Friends);
            Assert.Empty(clean.Incoming);
            Assert.Empty(clean.Outgoing);
        }

        [Fact]
        public void DuplicatesAndUnaddressableRowsAreDroppedRatherThanKept()
        {
            var list = new SocialList();
            list.Friends.Add(Friend("them"));
            list.Friends.Add(Friend("them"));
            list.Friends.Add(new FriendRef { PlayerId = "" });
            list.Friends.Add(null);

            Assert.Single(SocialRules.Normalise(list).Friends);
        }

        [Fact]
        public void EveryListIsCapped()
        {
            var list = new SocialList();

            for (int i = 0; i < SocialRules.MaxFriends + 40; i++) list.Friends.Add(Friend($"f{i}"));
            for (int i = 0; i < SocialRules.MaxPending + 40; i++) list.Incoming.Add(Friend($"i{i}"));
            for (int i = 0; i < SocialRules.MaxBlocked + 40; i++) list.Blocked.Add($"b{i}");

            var clean = SocialRules.Normalise(list);

            Assert.Equal(SocialRules.MaxFriends, clean.Friends.Count);
            Assert.Equal(SocialRules.MaxPending, clean.Incoming.Count);
            Assert.Equal(SocialRules.MaxBlocked, clean.Blocked.Count);
        }

        /// <summary>
        /// ⚠️ A HANDLE ARRIVES FROM A SERVICE AND FROM A FILE ON DISK, AND NEITHER IS A PROMISE.
        /// A control character in a handle draws as a box or eats the rest of the row.
        /// </summary>
        [Fact]
        public void AHandleIsCleanedAndClampedRatherThanTrusted()
        {
            var list = new SocialList();
            list.Friends.Add(new FriendRef
            {
                PlayerId = "them",
                Handle = "bad\nname" + new string('x', 200),
            });

            string handle = SocialRules.Normalise(list).Friends[0].Handle;

            Assert.DoesNotContain('\n', handle);
            Assert.DoesNotContain('', handle);
            Assert.True(handle.Length <= AccountRules.HandleMax);
        }

        [Fact]
        public void NormalisingNullAnswersAnEmptyDocumentRatherThanThrowing()
        {
            var clean = SocialRules.Normalise(null);

            Assert.NotNull(clean.Friends);
            Assert.NotNull(clean.Incoming);
            Assert.NotNull(clean.Outgoing);
            Assert.NotNull(clean.Blocked);
        }

        // -------------------------------------------------------------------
        // § RECENT PLAYERS
        // -------------------------------------------------------------------

        private static MatchRecord FourPlayerRecord()
        {
            var record = new MatchRecord
            {
                MatchId = "m1",
                Mode = GameMode.Classic.ToString(),
                Players = new[]
                {
                    new PlayerMatchStats { Slot = 0, PlayerId = "me",      Handle = "ME#0001" },
                    new PlayerMatchStats { Slot = 1, PlayerId = "them",    Handle = "THEM#0002" },
                    new PlayerMatchStats { Slot = 2, PlayerId = "",        Handle = "BOT",  IsBot = true },
                    new PlayerMatchStats { Slot = 3, PlayerId = "someone", Handle = "SOMEONE#0004" },
                },
            };

            return record;
        }

        /// <summary>
        /// ⚠️⚠️ THE HIGHEST-CONVERTING SOCIAL PROMPT A GAME OF THIS SHAPE HAS (`FUTURE.md` § 6),
        /// and the only way to add somebody that does not require them to hand you anything.
        /// </summary>
        [Fact]
        public void RecentPlayersOffersTheHumansYouJustPlayedAndNobodyElse()
        {
            var found = SocialRules.RecentPlayers(FourPlayerRecord(), new SocialList(), "me");

            Assert.Equal(new[] { "them", "someone" }, found.Select(f => f.PlayerId).ToArray());
        }

        /// <summary>
        /// ⚠️ A ROW THAT WOULD BE REFUSED IS NOT DRAWN. `CLAUDE.md` § 6.3: a control that does
        /// nothing when pressed must not look pressable, and offering ADD beside somebody who is
        /// already a friend is exactly that.
        /// </summary>
        [Fact]
        public void RecentPlayersHidesFriendsBlocksAndPendingRequests()
        {
            var list = new SocialList();
            list.Friends.Add(Friend("them"));
            list.Blocked.Add("someone");

            Assert.Empty(SocialRules.RecentPlayers(FourPlayerRecord(), list, "me"));

            var pending = new SocialList();
            pending.Outgoing.Add(Friend("them"));

            Assert.Equal(new[] { "someone" },
                SocialRules.RecentPlayers(FourPlayerRecord(), pending, "me")
                           .Select(f => f.PlayerId).ToArray());
        }

        /// <summary>
        /// ⚠️⚠️ BOTS ARE EXCLUDED BY `IsBot`, NEVER BY THE NAME. `docs/TODO.md` § 94.1 records
        /// four lines coming out `IsBot: false` carrying one id, and what believing a name cost.
        /// </summary>
        [Fact]
        public void ABotIsNeverOfferedEvenWhenItCarriesAnId()
        {
            var record = FourPlayerRecord();
            record.Players[2].PlayerId = "looks-real";

            Assert.DoesNotContain("looks-real",
                SocialRules.RecentPlayers(record, new SocialList(), "me").Select(f => f.PlayerId));
        }

        [Fact]
        public void ARecordWithNoPlayersAnswersAnEmptyListRatherThanThrowing()
        {
            Assert.Empty(SocialRules.RecentPlayers(null, new SocialList(), "me"));
            Assert.Empty(SocialRules.RecentPlayers(new MatchRecord(), new SocialList(), "me"));
        }

        // -------------------------------------------------------------------
        // § THE RATE
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ `FUTURE.md` § 19.6 IS EXPLICIT: *"presence polling must not raise the service
        /// query rate."* `ServerQuery` browses lobbies every 4 seconds because a lobby list goes
        /// stale in seconds; presence changes when somebody presses PLAY. Writing it at the lobby
        /// rate would be fifteen times the writes for a fact that does not move.
        /// </summary>
        [Fact]
        public void PresenceIsWrittenFarLessOftenThanTheLobbyIsQueried()
        {
            Assert.True(SocialRules.PresenceWriteSeconds >= 30,
                "presence is being written at close to the lobby query rate, which FUTURE.md " +
                "19.6 rules out by name.");

            Assert.True(SocialRules.PresenceStaleSeconds > SocialRules.PresenceWriteSeconds * 2,
                "the staleness bound is not comfortably more than the write interval, so one " +
                "missed heartbeat blinks a friend offline.");
        }
    }
}
