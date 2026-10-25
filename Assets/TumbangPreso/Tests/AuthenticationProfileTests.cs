using NUnit.Framework;

namespace TumbangPreso.Tests
{
    public sealed class AuthenticationProfileTests
    {
        [TestCase(null, "default")]
        [TestCase("", "default")]
        [TestCase("  player_A-12  ", "player_A-12")]
        [TestCase("123456789012345678901234567890", "123456789012345678901234567890")]
        public void ExistingValidProfilesKeepTheirAccountName(string name, string expected)
            => Assert.AreEqual(expected, ProfilePaths.AuthenticationProfile(name));

        [TestCase("profile with spaces")]
        [TestCase("1234567890123456789012345678901")]
        [TestCase("tao_日本")]
        public void InvalidProfilesHaveStableBoundedSdkNamesWithoutChangingLocalPaths(string name)
        {
            string local = ProfilePaths.ForProfile("saves", name);
            string sdk = ProfilePaths.AuthenticationProfile(name);
            Assert.AreEqual(30, sdk.Length);
            StringAssert.IsMatch("^[A-Za-z0-9_-]+$", sdk);
            Assert.AreEqual(sdk, ProfilePaths.AuthenticationProfile(name));
            Assert.AreEqual(sdk, ProfilePaths.AuthenticationProfile(sdk));
            Assert.AreNotEqual(sdk, ProfilePaths.AuthenticationProfile(name + "!"));
            Assert.AreEqual(local, ProfilePaths.ForProfile("saves", name));
        }
    }
}
