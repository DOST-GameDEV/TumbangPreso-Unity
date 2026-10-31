using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI.Hub;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Owner, October 28: all nine heroes are available in LAN, online and offline.
    /// A fresh profile without a wallet must be able to select Zack too.
    /// </summary>
    public sealed class LanHeroLockTests
    {
        [Test]
        public void EveryHeroIsAvailableInRoomsAndOfflineWithoutAWallet()
        {
            Assert.AreEqual(9,Roster.HeroPeople.Count);
            foreach (var person in Roster.HeroPeople)
            {
                Assert.IsTrue(HubOwnership.OwnsHeroInRoom(person.Id,null),person.Id+" must be selectable in a room without a wallet.");
                Assert.IsTrue(HubOwnership.OwnsHero(person.Id),person.Id+" must be selectable offline.");
            }
        }

        [Test]
        public void ClassicPeopleAreNeverLocked()
        {
            foreach (var person in Roster.ClassicPeople)
                Assert.IsTrue(HubOwnership.OwnsHeroInRoom(person.Id, null), person.Id + " is cosmetic and never locked.");
        }
    }
}
