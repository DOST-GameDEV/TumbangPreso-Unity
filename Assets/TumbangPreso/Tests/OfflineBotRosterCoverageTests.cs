using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;

namespace TumbangPreso.Tests
{
    public sealed class OfflineBotRosterCoverageTests
    {
        [TestCase(GameMode.Classic), TestCase(GameMode.HeroStrike)]
        public void OrdinarySoloBotsCanReachEveryCharacterAsTheHumanChangesPick(GameMode mode)
        {
            int count = Roster.GetPeople(mode).Count;
            var seen = new HashSet<int>();
            for (int human = 0; human < count; human++)
                foreach (int slot in new[] { 0, 2, 3 })
                    seen.Add(MatchInstaller.ResolveAiCharacterIndex(slot, human, mode));
            // ⚠️ SINCE 2026-10-20 A LOCKED HERO IS NEVER A BOT (`EconomyRules.UnlockedHeroes`), so the
            // claim is every UNLOCKED character, and no locked one.
            var people = Roster.GetPeople(mode);
            for (int character = 0; character < count; character++)
            {
                bool open = EconomyRules.IsHeroUnlocked(people[character].Id);
                Assert.AreEqual(open, seen.Contains(character), $"{Roster.PersonIdAt(mode, character)} " +
                    (open ? "never appears as a bot" : "is locked and still appears as a bot") + $" in ordinary solo {mode}");
            }
        }
        [TestCase(GameMode.Classic), TestCase(GameMode.HeroStrike)]
        public void SharedNetworkDefaultSelectionIsUnchanged(GameMode mode)
        {
            int count = Roster.GetPeople(mode).Count;
            var spread = new[] { 0, 3, 6, 9 };
            for (int slot = 0; slot < spread.Length; slot++)
                Assert.AreEqual(EconomyRules.NextUnlockedPick(Roster.GetPeople(mode), spread[slot] % count),
                    MatchInstaller.ResolveAiCharacterIndex(slot, -1, mode));
        }
    }
}
