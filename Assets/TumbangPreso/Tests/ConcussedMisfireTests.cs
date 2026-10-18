using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// A Concussed throw may MISFIRE (owner, 2026-10-17: *"theres a big chance it flies or goes somewhere completley off"*):
    /// `Carrier.ConcussedMisfire`, decided on the host from `Carrier.MisfireRoll`.
    /// </summary>
    public sealed class ConcussedMisfireTests
    {
        private static readonly Vector3 Aimed = new Vector3(0f, 3f, 14f);

        private static float FlatTurn(Vector3 from, Vector3 to)
            => Vector3.Angle(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z));

        [Test]
        public void AboutTheRuledShareOfThrowsMisfireFarOffHigherAndFaster()
        {
            int misfires = 0, left = 0, rolls = 2000;
            for (int i = 0; i < rolls; i++)
            {
                var flew = Carrier.ConcussedMisfire(Aimed, Carrier.MisfireRoll(i % 4, i, 1000 + i * 7));
                if (flew == Aimed) continue;
                misfires++;
                float turn = FlatTurn(Aimed, flew);
                Assert.That(turn, Is.InRange(StatusRules.ConcussedMisfireYawMin - .5f, StatusRules.ConcussedMisfireYawMax + .5f), "A misfire goes far off its aim.");
                float speed = flew.magnitude / Aimed.magnitude;
                Assert.That(speed, Is.InRange(StatusRules.ConcussedMisfireSpeedMin - .01f, StatusRules.ConcussedMisfireSpeedMax + .01f), "A misfire flies faster.");
                float rise = Mathf.Atan2(flew.y, new Vector2(flew.x, flew.z).magnitude) - Mathf.Atan2(Aimed.y, new Vector2(Aimed.x, Aimed.z).magnitude);
                Assert.That(rise * Mathf.Rad2Deg, Is.InRange(StatusRules.ConcussedMisfirePitchMin - .5f, StatusRules.ConcussedMisfirePitchMax + .5f), "A misfire goes higher.");
                if (flew.x < 0f) left++;
            }
            float share = misfires / (float)rolls;
            Assert.That(share, Is.InRange(StatusRules.ConcussedMisfireChance - .05f, StatusRules.ConcussedMisfireChance + .05f));
            Assert.That(left / (float)misfires, Is.InRange(.4f, .6f), "Misfires go to either side.");
        }

        [Test]
        public void TheRollIsTheSameForTheSameThrow()
        {
            Assert.AreEqual(Carrier.MisfireRoll(2, 5, 900), Carrier.MisfireRoll(2, 5, 900));
            Assert.AreNotEqual(Carrier.MisfireRoll(2, 5, 900), Carrier.MisfireRoll(2, 6, 900));
            var state = Random.state;
            Carrier.ConcussedMisfire(Aimed, Carrier.MisfireRoll(1, 1, 1));
            Assert.AreEqual(state, Random.state, "The misfire never draws on Unity's random state.");
        }
    }
}
