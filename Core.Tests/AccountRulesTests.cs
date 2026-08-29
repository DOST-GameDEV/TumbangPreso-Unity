using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class AccountRulesTests
    {
        [Theory]
        [InlineData("Mat", "Mat")]
        [InlineData("  Maria   Clara  ", "Maria Clara")]
        [InlineData("abcdefghijklmnopq", "abcdefghijklmnop")]
        public void DisplayNamesAreNormalisedAtTheRuleBoundary(string raw, string expected)
        {
            Assert.True(AccountRules.TryDisplayName(raw, out string clean));
            Assert.Equal(expected, clean);
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
    }
}
