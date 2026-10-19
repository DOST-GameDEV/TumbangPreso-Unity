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
        [Test]
        public void ARoomWithNoKnownWalletOffersOnlyTheStarterHeroes()
        {
            foreach (string id in EconomyRules.StarterHeroes)
                Assert.IsTrue(HubOwnership.OwnsHeroInRoom(id, null), id + " is a starter hero and must stay playable.");
            int locked = 0;
            foreach (var person in Roster.HeroPeople)
            {
                if (!EconomyRules.IsHero(person.Id) || System.Array.IndexOf(EconomyRules.StarterHeroes, person.Id) >= 0) continue;
                Assert.IsFalse(HubOwnership.OwnsHeroInRoom(person.Id, null), person.Id + " must be locked in a room until it is owned.");
                locked++;
            }
            Assert.Greater(locked, 0, "The roster has heroes beyond the starters, so a room must lock some.");
        }

        [Test]
        public void ClassicPeopleAreNeverLocked()
        {
            foreach (var person in Roster.ClassicPeople)
                Assert.IsTrue(HubOwnership.OwnsHeroInRoom(person.Id, null), person.Id + " is cosmetic and never locked.");
        }
    }
}
