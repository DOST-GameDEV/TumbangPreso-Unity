using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The speed follows the ROLE, on every peer, on the frame the role changes.
    ///
    /// ⚠️⚠️ 🧑 2026-08-29, having just had the two role scales split apart: *"make sure speed
    /// changes when role changes okay, and not just host side"*. `docs/TODO.md` § 83.23.
    ///
    /// It is a fair thing to be suspicious of. This game's recurring network fault is a rule that
    /// resolves on one machine — `docs/TODO.md` §§ 82.1, 83.9, 83.12 and 83.16 are all that shape
    /// — and "your body moves at the wrong speed for a whole round" would be invisible to the
    /// person it is happening to, because they have nothing to compare against.
    ///
    /// **The answer is that it is already right, and these are the three reasons written down so
    /// they cannot quietly stop being true.**
    /// </summary>
    public class RoleSpeedTests
    {
        private const string Motor = "Assets/TumbangPreso/Runtime/CharacterMotor.cs";
        private const string Round = "Assets/TumbangPreso/Runtime/RoundDirector.cs";

        /// <summary>
        /// 1. The two roles are two different numbers, and the taya is the faster.
        /// </summary>
        [Test]
        public void TheTwoRolesMoveAtDifferentSpeeds()
        {
            float taya = Stamina.RoleSpeedScale(isDefender: true);
            float attacker = Stamina.RoleSpeedScale(isDefender: false);

            Assert.AreEqual(Balance.DefenderSpeedScale, taya, 0.0001f);
            Assert.AreEqual(Balance.AttackerSpeedScale, attacker, 0.0001f);

            Assert.Greater(taya, attacker,
                "the taya is the faster role, which is the whole balance of chase versus escape");
        }

        /// <summary>
        /// 2. ⚠️⚠️ THE SCALE IS READ PER STEP FROM THE LIVE FLAG, NOT CACHED AT SPAWN OR AT A
        /// ROUND BOUNDARY. That is what makes the change instant on every machine: whoever
        /// writes `IsDefender` — the host from its own rules, a client from the replicated
        /// snapshot — the very next physics step composes the speed from it.
        ///
        /// ⚠️ IT IS ASSERTED AGAINST THE SOURCE BECAUSE THE VALUE IS A LOCAL INSIDE `FixedUpdate`
        /// AND EXPOSING IT WOULD BE A HOOK THAT EXISTS ONLY FOR A TEST. `DeadFeatureAudit`'s
        /// header makes the same argument at length: the property being guarded is *where a line
        /// is written*, and reflection cannot see that.
        /// </summary>
        [Test]
        public void TheMotorReadsTheRoleScaleEveryStepRatherThanCachingIt()
        {
            string source = File.ReadAllText(Motor);

            StringAssert.Contains("Stamina.RoleSpeedScale(_isDefender)", source,
                "the role term must be read from the live flag where the speed is composed");

            int at = source.IndexOf("Stamina.RoleSpeedScale(_isDefender)");
            int fixedUpdate = source.IndexOf("private void FixedUpdate()");

            Assert.Greater(at, fixedUpdate,
                "the role scale is read inside the physics step, so a role change lands on the "
                + "next one. A copy taken at spawn or at a round boundary is a body that keeps "
                + "the previous round's speed, and on a client nothing would ever correct it.");

            Assert.IsFalse(source.Contains("_roleSpeedScale") || source.Contains("_cachedRoleScale"),
                "a cached copy of the role scale is the fault this test exists to refuse");
        }

        /// <summary>
        /// 3. ⚠️⚠️ AND A CLIENT'S `IsDefender` IS STAMPED FROM THE REPLICATED SNAPSHOT WITHOUT ANY
        /// GATE, WHICH IS THE HALF HE WAS ASKING ABOUT. `RoundDirector.ApplySnapshot` writes
        /// `RoundActive` onto the four bodies only `if (matchInProgress)` — that guard is
        /// load-bearing and its own note records a client frozen solid without it — and it writes
        /// `IsDefender` **outside** that block, unconditionally, at the 5 Hz `SyncWorld` cadence.
        ///
        /// So a taya rotation reaches every peer as data, each peer's motor reads it on its next
        /// physics step, and the speed changes there without the host being involved at all.
        /// Moving that line inside the guard would leave a client's role frozen between matches,
        /// which is why the test asserts the ORDER rather than merely the presence.
        /// </summary>
        [Test]
        public void AClientTakesTheRoleFromTheSnapshotWithNoGateOnIt()
        {
            string source = File.ReadAllText(Round);

            int gate = source.IndexOf("if (matchInProgress)");
            int stamp = source.IndexOf("player.IsDefender = player.PlayerSlot == defenderSlot");

            Assert.Greater(gate, -1, "ApplySnapshot's RoundActive guard is gone");
            Assert.Greater(stamp, -1, "ApplySnapshot no longer stamps the role");

            Assert.Greater(stamp, gate,
                "the role stamp must come after the RoundActive block, in its own unguarded "
                + "loop. Inside the guard it would stop replicating whenever a match is not in "
                + "progress, and a client would keep the last round's role and the last round's "
                + "speed with it.");
        }

        /// <summary>
        /// ⚠️ AND THE SPEED IS A PRODUCT, so nothing else in it is allowed to be role-shaped.
        /// `CharacterMotor` composes `Speed * RoleSpeedScale * PersonSpeedScale * sprint *
        /// SpeedZones`; a second term that quietly asked about the role would be a second place
        /// to keep in step and a second place for the host and a client to disagree.
        /// </summary>
        [Test]
        public void TheRoleAppearsInTheSpeedProductExactlyOnce()
        {
            string source = File.ReadAllText(Motor);

            int first = source.IndexOf("float speed = Balance.Speed");
            Assert.Greater(first, -1, "the speed product has moved; this test is out of date");

            int end = source.IndexOf(";", first);
            string product = source.Substring(first, end - first);

            int roleTerms = 0;
            foreach (string term in product.Split('*'))
                if (term.Contains("RoleSpeedScale") || term.Contains("IsDefender")) roleTerms++;

            Assert.AreEqual(1, roleTerms,
                $"the role must appear once in the speed product and appears {roleTerms} times:\n"
                + product);
        }
    }
}
