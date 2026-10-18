using NUnit.Framework;
using TumbangPreso.Abilities;

namespace TumbangPreso.Tests
{
    public sealed class HeroPassivesTests
    {
        // Paete's MAKILING'S BLESSING has no Wiki rule yet, so it is the one hero without a line.
        [TestCase("amihan")] [TestCase("cheska")] [TestCase("dante")] [TestCase("rafi")]
        [TestCase("zack")] [TestCase("sean")] [TestCase("nemu")] [TestCase("phaister")]
        public void EveryWikiPassiveFitsTheTrayLine(string hero)
        {
            var passive = HeroPassives.For(hero);
            Assert.IsTrue(passive.Exists, $"{hero} has no passive");
            // The tray line is two lines of about 100 characters at the readable floor.
            int length = "PASSIVE · ".Length + passive.Name.Length + 3 + passive.Description.Length;
            Assert.LessOrEqual(length, 190, $"{hero}'s passive line is {length} characters");
        }

        [Test]
        public void PaeteHasNoInventedPassive() => Assert.IsFalse(HeroPassives.For("paete").Exists);
    }
}
