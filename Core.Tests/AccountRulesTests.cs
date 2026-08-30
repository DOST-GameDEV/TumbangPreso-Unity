using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class AccountRulesTests
    {
        [Theory]
        [InlineData("Mat", "Mat")]
        [InlineData("  Maria   Clara  ", "Maria Clara")]
        public void DisplayNamesAreNormalisedAtTheRuleBoundary(string raw, string expected)
        {
            Assert.True(AccountRules.TryDisplayName(raw, out string clean));
            Assert.Equal(expected, clean);
        }

        /// <summary>
        /// ⚠️ DERIVED FROM THE CONSTANT, NOT WRITTEN OUT. This read `abcdefghijklmnop` while the
        /// limit was briefly 16, so the test agreed with the wrong number instead of catching it.
        /// </summary>
        [Fact]
        public void ADisplayNameIsTruncatedToTheLimitRatherThanRefused()
        {
            string tooLong = new string('a', AccountRules.DisplayNameMax + 1);
            Assert.True(AccountRules.TryDisplayName(tooLong, out string clean));
            Assert.Equal(new string('a', AccountRules.DisplayNameMax), clean);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("name#1234")]
        [InlineData("line\nbreak")]
        public void InvalidDisplayNamesAreRefused(string raw) =>
            Assert.False(AccountRules.TryDisplayName(raw, out _));

        [Fact]
        public void OfflineDiscriminatorIsStableAndFourDigits()
        {
            string first = AccountRules.Discriminator("", "stable-player-id");
            Assert.Equal(first, AccountRules.Discriminator("", "stable-player-id"));
            Assert.Matches("^[0-9]{4}$", first);
        }

        [Fact]
        public void ValidRemoteValuesWinAndMissingRemoteValuesKeepLocalData()
        {
            var local = new AccountProfile
            {
                PlayerId = "local", DisplayName = "Local Name", Discriminator = "1234",
                Bio = "kept bio", Country = "PH", Pronouns = "they/them"
            };
            var remote = new AccountProfile
            {
                PlayerId = "remote", DisplayName = "Remote Name", Discriminator = "9876"
            };

            AccountProfile result = AccountRules.Resolve(local, remote, remoteAvailable: true);
            Assert.Equal("remote", result.PlayerId);
            Assert.Equal("Remote Name", result.DisplayName);
            Assert.Equal("9876", result.Discriminator);
            Assert.Equal("kept bio", result.Bio);
            Assert.Equal("PH", result.Country);
        }

        [Fact]
        public void UnreachableServiceNeverErasesTheLocalProfile()
        {
            var local = new AccountProfile { PlayerId = "local", DisplayName = "Offline", Bio = "bio" };
            var remote = new AccountProfile { PlayerId = "remote", DisplayName = "Remote" };
            AccountProfile result = AccountRules.Resolve(local, remote, remoteAvailable: false);
            Assert.Equal("local", result.PlayerId);
            Assert.Equal("Offline", result.DisplayName);
            Assert.Equal("bio", result.Bio);
        }

        [Fact]
        public void HandlesRoundTripWithoutLettingTheTagIntoTheName()
        {
            string handle = AccountRules.Handle("Matthew", "4417");
            Assert.True(AccountRules.TrySplitHandle(handle, out string name, out string tag));
            Assert.Equal("Matthew", name);
            Assert.Equal("4417", tag);
        }

        // -------------------------------------------------------------------
        // ARRIVAL, WHICH IS THE ONLY PLACE A NAME CROSSES FROM ANOTHER MACHINE
        // -------------------------------------------------------------------

        [Fact]
        public void ArrivalKeepsAFullHandleIntact()
        {
            string arrived = AccountRules.ArrivalHandle("Matthew#4417", "token-a");
            Assert.Equal("Matthew#4417", arrived);
        }

        /// <summary>
        /// ⚠️ THE REGRESSION THIS EXISTS FOR. A bare name is what every LAN peer, every build
        /// older than the account layer, and every client still loading its profile sends. The
        /// first cut answered `Player#tag` to all of them, so a four-machine hall rendered as
        /// four identical rows.
        /// </summary>
        [Fact]
        public void ArrivalKeepsABareNameAndOnlyAddsTheTag()
        {
            string arrived = AccountRules.ArrivalHandle("GuestPlayer", "token-guest");
            Assert.True(AccountRules.TrySplitHandle(arrived, out string name, out string tag));
            Assert.Equal("GuestPlayer", name);
            Assert.Equal(AccountRules.DiscriminatorDigits, tag.Length);
        }

        [Fact]
        public void ArrivalFallsBackToPlayerOnlyWhenTheNameCannotBeShown()
        {
            Assert.True(AccountRules.TrySplitHandle(
                AccountRules.ArrivalHandle("  ", "token-b"), out string blank, out _));
            Assert.Equal("Player", blank);

            // Too short to be a display name, so there is nothing to keep.
            Assert.True(AccountRules.TrySplitHandle(
                AccountRules.ArrivalHandle("ab", "token-c"), out string tiny, out _));
            Assert.Equal("Player", tiny);
        }

        /// <summary>
        /// Two different peers that both fail to supply a usable name must still be told apart,
        /// which is what deriving the tag from the durable token buys.
        /// </summary>
        [Fact]
        public void ArrivalSeparatesTwoUnnamedPeersByTheirToken()
        {
            string first = AccountRules.ArrivalHandle("", "token-one");
            string second = AccountRules.ArrivalHandle("", "token-two");
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void ArrivalIsStableForTheSameTokenAcrossRestarts()
        {
            Assert.Equal(
                AccountRules.ArrivalHandle("", "token-stable"),
                AccountRules.ArrivalHandle("", "token-stable"));
        }

        // -------------------------------------------------------------------
        // THE UPGRADE OFFER, WHICH IS REACHED FROM EVERY SINGLE POINT SCORED
        // -------------------------------------------------------------------

        [Fact]
        public void TheUpgradeOfferIsQueuedForAnAnonymousPlayerWhoJustScored()
            => Assert.True(AccountRules.ShouldQueueUpgradeOffer(
                isGuest: false, hasPassword: false, offerAlreadyShown: false, offerAlreadyPending: false));

        /// <summary>
        /// ⚠️ THE REGRESSION GUARD. Without the already-pending term the caller rewrote
        /// `settings.json` on roughly every score event, and passive defence pays +10 a second.
        /// </summary>
        [Fact]
        public void TheUpgradeOfferIsQueuedOnceAndNotAgainOnEveryLaterPoint()
            => Assert.False(AccountRules.ShouldQueueUpgradeOffer(
                isGuest: false, hasPassword: false, offerAlreadyShown: false, offerAlreadyPending: true));

        [Theory]
        // A guest has no progression to keep and no credential to attach.
        [InlineData(true, false, false)]
        // Already has a password, so there is nothing to upgrade to.
        [InlineData(false, true, false)]
        // Already offered and declined; do not ask again.
        [InlineData(false, false, true)]
        public void TheUpgradeOfferIsNotQueuedForAPlayerItDoesNotApplyTo(
            bool isGuest, bool hasPassword, bool alreadyShown)
            => Assert.False(AccountRules.ShouldQueueUpgradeOffer(
                isGuest, hasPassword, alreadyShown, offerAlreadyPending: false));

        /// <summary>
        /// ⚠️ ONE NAME LENGTH, NOT TWO. `LanBeacon` truncates to `Balance.PlayerNameMax`, the
        /// settings field limits to it and the HUD row was measured against it, so a longer
        /// account name would arrive clipped and render past the measurement.
        /// </summary>
        [Fact]
        public void TheAccountNameLimitIsTheOneTheWireAndTheHudUse()
        {
            Assert.Equal(Balance.PlayerNameMax, AccountRules.DisplayNameMax);
        }

        // -------------------------------------------------------------------
        // THE IMPERSONATION GUARD. `docs/TODO.md` § 88.1c and § 90.1.
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE WHOLE GUARD RESTS ON THE TAG BEING A FUNCTION OF THE ID. While UGS Player Names
        /// allocated it, neither the host nor the server could recompute it, which is exactly the
        /// sentence § 88.1c wrote the blocker down as.
        /// </summary>
        [Fact]
        public void ATagIsDerivedFromThePlayerIdAndOnlyFromThePlayerId()
        {
            Assert.Equal(AccountRules.DerivedTag("player-a"), AccountRules.DerivedTag("player-a"));
            Assert.NotEqual(AccountRules.DerivedTag("player-a"), AccountRules.DerivedTag("player-b"));
            Assert.Equal(AccountRules.DiscriminatorDigits, AccountRules.DerivedTag("player-a").Length);
        }

        /// <summary>
        /// ⚠️⚠️ THESE FOUR VECTORS WERE PRODUCED BY RUNNING `derivedTag` IN
        /// `ugs/cloud-code/player-account.js`, AND THAT IS THE ENTIRE POINT OF WRITING THEM OUT.
        /// Nothing on this machine compiles the JavaScript, so the two copies of FNV-1a can only
        /// be compared through values one of them produced. The server is the authority: if this
        /// goes red, the C# and the script have stopped agreeing about who owns which tag, and
        /// every online handle check starts refusing honest players.
        ///
        /// ⚠️ THE JS WALKS UTF-16 CODE UNITS BY INDEX for the same reason: `for...of` walks code
        /// POINTS and would disagree with C#'s `foreach (char c in ...)` on an astral character.
        /// </summary>
        [Theory]
        [InlineData("player-a", "8930")]
        [InlineData("player-b", "1311")]
        [InlineData("qmSg3PKweGRSWqRcd9g0Bo80UKH4", "3831")]
        [InlineData("guest-abc123", "9434")]
        public void TheDerivedTagMatchesTheServerScriptsCopyOfTheSameHash(string playerId, string expected)
            => Assert.Equal(expected, AccountRules.DerivedTag(playerId));

        [Fact]
        public void AHandleIsOwnedOnlyByTheAccountWhoseIdProducesItsTag()
        {
            string handle = AccountRules.OwnedHandle("Maria Clara", "player-a");

            Assert.True(AccountRules.OwnsHandle(handle, "player-a"));
            Assert.False(AccountRules.OwnsHandle(handle, "player-b"));
            Assert.False(AccountRules.OwnsHandle(handle, ""));
        }

        /// <summary>
        /// ⚠️ THE NAME IS NOT PART OF OWNERSHIP AND MUST NOT BECOME PART OF IT. Two people being
        /// called Maria Clara is the reason discriminators exist at all; refusing the second one
        /// would be a first-come-first-served global name registry nobody asked for.
        /// </summary>
        [Fact]
        public void TwoAccountsMayShareADisplayNameButNotATag()
        {
            string mine = AccountRules.OwnedHandle("Maria Clara", "player-a");
            string theirs = AccountRules.OwnedHandle("Maria Clara", "player-b");

            Assert.NotEqual(mine, theirs);
            Assert.True(AccountRules.OwnsHandle(mine, "player-a"));
            Assert.True(AccountRules.OwnsHandle(theirs, "player-b"));
        }

        [Fact]
        public void AVerifiedPeerIsCalledWhatTheServiceSaysRatherThanWhatItClaimed()
        {
            string resolved = AccountRules.VerifiedArrivalHandle(
                claimed: "Somebody Else#1234",
                token: "token-a",
                check: AccountRules.HandleCheck.Owned,
                ownedHandle: "Maria Clara#4417");

            Assert.Equal("Maria Clara#4417", resolved);
        }

        /// <summary>
        /// ⚠️⚠️ THIS IS THE ATTACK § 88.1c NAMES, AND THE ASSERTION THAT USED TO BE BACKWARDS.
        /// `LobbyAcceptsOnlyCanonicalAccountHandles` admitted a claimed `Maria Clara#4417`
        /// verbatim while rewriting the honest bare name beside it.
        /// </summary>
        [Fact]
        public void AnUnprovableClaimKeepsItsNameAndLosesTheTagItClaimed()
        {
            string resolved = AccountRules.VerifiedArrivalHandle(
                claimed: "Maria Clara#4417",
                token: "impostor-token",
                check: AccountRules.HandleCheck.NotOwned,
                ownedHandle: "");

            Assert.StartsWith("Maria Clara#", resolved);
            Assert.NotEqual("Maria Clara#4417", resolved);
            Assert.Equal(AccountRules.Handle("Maria Clara", AccountRules.Discriminator("", "impostor-token")),
                         resolved);
        }

        /// <summary>
        /// ⚠️⚠️ `FUTURE.md` § 0.5 RULE 7 AS A TEST. A LAN match may never sit behind a login, and
        /// venue Wi-Fi at the nationals may not exist at all, so neither "we did not ask" nor "it
        /// did not answer" may cost a player their name.
        /// </summary>
        [Theory]
        [InlineData(AccountRules.HandleCheck.NotAsked)]
        [InlineData(AccountRules.HandleCheck.Unreachable)]
        public void AnUncheckedPeerKeepsTheNameItClaimed(AccountRules.HandleCheck check)
        {
            Assert.Equal(
                AccountRules.ArrivalHandle("Maria Clara#4417", "token-a"),
                AccountRules.VerifiedArrivalHandle("Maria Clara#4417", "token-a", check, ""));

            Assert.Equal(
                AccountRules.ArrivalHandle("Maria Clara", "token-a"),
                AccountRules.VerifiedArrivalHandle("Maria Clara", "token-a", check, ""));
        }

        /// <summary>
        /// ⚠️ A BARE NAME IS NOT A CLAIM TO AN ACCOUNT, so a refusal has nothing to take away and
        /// must not re-tag a peer that never asserted a tag. This is § 88.1b one layer on: the
        /// four-machines-in-a-hall case is what a blanket demotion would break.
        /// </summary>
        [Fact]
        public void ARefusalDoesNotPunishAPeerThatNeverClaimedATag()
        {
            Assert.Equal(
                AccountRules.ArrivalHandle("Maria Clara", "token-a"),
                AccountRules.VerifiedArrivalHandle(
                    "Maria Clara", "token-a", AccountRules.HandleCheck.NotOwned, ""));
        }

        [Fact]
        public void AClaimWithATagIsWhatCountsAsClaimingAnAccount()
        {
            Assert.True(AccountRules.ClaimsASpecificAccount("Maria Clara#4417"));
            Assert.False(AccountRules.ClaimsASpecificAccount("Maria Clara"));
            Assert.False(AccountRules.ClaimsASpecificAccount(""));
            Assert.False(AccountRules.ClaimsASpecificAccount("Maria Clara#44"));
        }
    }
}
