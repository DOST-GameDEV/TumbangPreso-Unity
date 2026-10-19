using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI.Hub;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// LAN rooms lock heroes the way Online rooms do (owner, 2026-10-20: "lan however does not have
    /// any locked characters. fix this, it should be the same way as online mode").
    ///
    /// ⚠️ THE CASE THAT MATTERS IS THE WALLET NOBODY HAS HEARD FROM. A LAN player who never signed in
    /// has no known wallet, and the general ownership rule (`HubOwnership.Owns`) opens everything in
    /// that state, which is exactly how every LAN hero stayed unlocked. The room rule must not.
    /// </summary>
    public sealed class LanHeroLockTests
    {
        /// <summary>
        /// ⚠️ NARROWED 2026-10-20 (owner: "make these characters the only unlocked characters. do
        /// this for both lan, multiplayer, and offline"). The room rule used to be the starters plus
        /// the wallet; it is now `EconomyRules.UnlockedHeroes` alone, with or without a wallet.
        /// </summary>
        [Test]
        public void ARoomOffersOnlyTheUnlockedHeroes()
        {
            foreach (string id in EconomyRules.UnlockedHeroes)
                Assert.IsTrue(HubOwnership.OwnsHeroInRoom(id, null), id + " is unlocked and must stay playable.");
            int locked = 0;
            foreach (var person in Roster.HeroPeople)
            {
                if (System.Array.IndexOf(EconomyRules.UnlockedHeroes, person.Id) >= 0) continue;
                Assert.IsFalse(HubOwnership.OwnsHeroInRoom(person.Id, null), person.Id + " must be locked in a room.");
                Assert.IsFalse(HubOwnership.OwnsHero(person.Id), person.Id + " must be locked offline too.");
                locked++;
            }
            Assert.Greater(locked, 0, "The roster has heroes beyond the unlocked five, so some must be locked.");
        }

        [Test]
        public void ClassicPeopleAreNeverLocked()
        {
            foreach (var person in Roster.ClassicPeople)
                Assert.IsTrue(HubOwnership.OwnsHeroInRoom(person.Id, null), person.Id + " is cosmetic and never locked.");
        }
    }
}
