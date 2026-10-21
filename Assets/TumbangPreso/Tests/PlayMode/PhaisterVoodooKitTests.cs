using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ PHAISTER'S v3 KIT, CAST (HERO-10, the owner's table of 2026-09-27): TELEPORT, CURSE: DRAIN and CURSE: HEX pressed
    /// through the kit in a real Hero Strike round on Bayan Plaza, host-resolved as the solo host resolves them. The body's own
    /// voodoo clocks are `VoodooBodyWiringTests`; these check what the abilities add on top: the reach starts only at somebody in
    /// front of her, a reach that lands leaves the mark and keeps the whole cooldown, a broken one hands back half (plan 9.11,
    /// proposed), HEX's recast waits for its 10 s and then works while the 35 s cooldown runs without restarting it, and TELEPORT
    /// moves her and nobody else (*"Teleport to the target location instantly."*, the old shove dropped).
    /// </summary>
    public sealed class PhaisterVoodooKitTests
    {
        private INetProvider _net;
        private Vector3 _can;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            _can = GameServices.Round.Lata != null ? GameServices.Round.Lata.transform.position : Vector3.zero;
            _can.y = 0.0f;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                // Every seat starts well behind where the tests stand, out of any cone they aim.
                player.Teleport(_can + new Vector3(-9.0f + player.PlayerSlot * 0.9f, .12f, -11.0f));
            }
            yield return null;
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        private static CharacterMotor Attacker(int skip = -1)
            => GameServices.Round.Players.First(p => p != null && !p.IsDefender && p.PlayerSlot != skip);

        private IEnumerator Phaister(CharacterMotor who, Vector3 at, Vector3 facing)
        {
            who.AbilitySystem.BindHero("phaister");
            who.IsBot = true;
            who.Teleport(_can + at);
            Face(who, facing);
            yield return null;
            yield return null;
        }

        private static void Face(CharacterMotor who, Vector3 direction)
        {
            direction.y = 0.0f;
            who.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static AbilityContext Context(CharacterMotor who)
            => new AbilityContext(who, who.GetComponent<Carrier>(), who.GetComponent<CombatVerbs>());

        [UnityTest, Timeout(60000)]
        public IEnumerator DrainReachesTheOneSheFacesMarksThemAndDrainsWithoutARefund()
        {
            var her = Attacker();
            yield return Phaister(her, new Vector3(-4.0f, .12f, -6.0f), Vector3.right);
            var them = Attacker(her.PlayerSlot);
            them.Teleport(_can + new Vector3(1.0f, .12f, -6.0f));
            yield return null;

            var kit = her.AbilitySystem.Kit;
            var drain = kit.Skill2;
            Assert.AreEqual("phaister_skill2", drain.Id);
            Assert.AreEqual("CURSE: DRAIN", drain.Name);
            Assert.AreSame(them, PhaisterHeroKit.ReachTargetFor(her, her.transform.forward), "She does not pick the one she faces.");

            Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill2(Context(her)));
            Assert.IsTrue(her.IsVoodooReaching, "The press did not start a reach.");
            Assert.AreEqual(them.PlayerSlot, her.VoodooReachTarget);
            Assert.AreEqual(VoodooMarkKind.Drain, her.VoodooReachKind);

            yield return new WaitForSeconds(VoodooRules.ReachSeconds + 0.3f);
            Assert.IsFalse(her.IsVoodooReaching);
            Assert.AreEqual(VoodooMarkKind.Drain, them.VoodooMark, "Two seconds of reach did not mark them.");
            Assert.AreEqual(her.PlayerSlot, them.VoodooMarkSource);
            Assert.Greater(drain.CooldownRemaining, drain.Cooldown - VoodooRules.ReachSeconds - 1.0f,
                "A reach that landed handed cooldown back.");
            Assert.Greater(her.StatusSpeedScale, 1.0f, "VOODOO: she does not take their speed while her mark lives.");
            Assert.Less(them.StatusSpeedScale, 1.0f, "VOODOO: the marked one is not slower.");

            yield return new WaitForSeconds(VoodooRules.DrainDelaySeconds + 0.2f);
            Assert.IsTrue(them.IsDrained, "The mark did not go off as DRAINED 1.5 s later.");
            Assert.IsTrue(them.Stamina.RecoveryBlocked);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator APressAtNobodyIsRefusedAndABrokenReachHandsBackHalf()
        {
            var her = Attacker();
            yield return Phaister(her, new Vector3(-4.0f, .12f, -6.0f), Vector3.forward);
            var them = Attacker(her.PlayerSlot);
            them.Teleport(_can + new Vector3(1.0f, .12f, -6.0f));
            yield return null;

            var kit = her.AbilitySystem.Kit;
            var drain = kit.Skill2;
            Assert.IsNull(PhaisterHeroKit.ReachTargetFor(her, her.transform.forward));
            Assert.AreNotEqual(HeroKit.CastOutcome.Cast, kit.CastSkill2(Context(her)), "A press at nobody was cast.");
            Assert.AreEqual(0.0f, drain.CooldownRemaining, "A refused press spent the cooldown.");
            Assert.IsFalse(her.IsVoodooReaching);

            Face(her, Vector3.right);
            Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill2(Context(her)));
            Assert.IsTrue(her.IsVoodooReaching);
            yield return new WaitForSeconds(0.5f);

            // She turns her back on them: the thread snaps.
            Face(her, Vector3.left);
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(her.IsVoodooReaching, "The reach held with the target behind her.");
            Assert.AreEqual(VoodooMarkKind.None, them.VoodooMark, "A broken reach still marked them.");
            Assert.IsFalse(drain.IsActive, "A broken reach left the curse running.");
            float half = drain.Cooldown * VoodooRules.ReachBrokenRefund;
            Assert.Less(drain.CooldownRemaining, drain.Cooldown - half, "A broken reach handed nothing back.");
            Assert.Greater(drain.CooldownRemaining, drain.Cooldown - half - 1.5f, "A broken reach handed back more than half.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator HexArmsAfterTenSecondsAndItsRecastIgnoresTheCooldown()
        {
            var her = GameServices.Round.Players.First(p => p != null && p.IsDefender);
            yield return Phaister(her, new Vector3(-2.0f, .12f, -1.5f), Vector3.right);
            var them = Attacker();
            them.Teleport(_can + new Vector3(2.5f, .12f, -1.5f));
            yield return null;

            var kit = her.AbilitySystem.Kit;
            var hex = kit.Skill2;
            Assert.AreEqual("phaister_skill2d", hex.Id);
            Assert.AreEqual("CURSE: HEX", hex.Name);
            Assert.IsTrue(hex.CanReactivate);

            Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill2(Context(her)));
            yield return new WaitForSeconds(VoodooRules.ReachSeconds + 0.3f);
            Assert.AreEqual(VoodooMarkKind.Hex, them.VoodooMark, "Two seconds of reach did not mark them.");
            Assert.IsTrue(hex.IsActive, "The curse ended before its recast.");
            Assert.IsFalse(hex.ReactivateReady, "The hex was ready before its 10 s.");
            Assert.Greater(hex.ReactivateReadyIn, 0.0f);
            Assert.AreEqual(HeroKit.CastOutcome.NotYet, kit.CastSkill2(Context(her)), "An early recast was not NOT YET.");
            Assert.IsFalse(them.IsHexed);

            // Ten seconds on (the body's own clock is `VoodooBodyWiringTests`'); the cooldown is still running.
            typeof(CharacterMotor).GetField("_markAge", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(them, VoodooRules.HexArmSeconds + 0.05f);
            Assert.IsTrue(them.VoodooHexArmed);
            Assert.IsTrue(hex.ReactivateReady, "An armed hex is not ready to recast.");
            float cooling = hex.CooldownRemaining;
            Assert.Greater(cooling, 0.0f, "The test needs the cooldown still running.");

            Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill2(Context(her)), "The recast was refused while cooling.");
            Assert.IsTrue(them.IsHexed, "The recast did not HEX them.");
            Assert.AreEqual(VoodooMarkKind.None, them.VoodooMark, "The mark outlived its recast.");
            Assert.IsFalse(hex.IsActive);
            Assert.AreEqual(cooling, hex.CooldownRemaining, 0.05f, "The recast restarted or refunded the cooldown.");
            Assert.AreEqual(HeroKit.CastOutcome.Cooling, kit.CastSkill2(Context(her)), "A third press was not a cooling refusal.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TeleportMovesHerAndShovesNobody()
        {
            var her = Attacker();
            yield return Phaister(her, new Vector3(-4.0f, .12f, -6.0f), Vector3.right);
            var near = Attacker(her.PlayerSlot);
            near.Teleport(_can + new Vector3(-4.0f, .12f, -7.0f));
            yield return null;
            yield return new WaitForSeconds(0.3f);

            var kit = her.AbilitySystem.Kit;
            var teleport = kit.Skill1;
            Assert.AreEqual("TELEPORT", teleport.Name);
            Assert.AreEqual(VoodooRules.TeleportCooldown, teleport.Cooldown);

            Vector3 from = her.transform.position, standing = near.transform.position;
            Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill1(Context(her)));
            yield return new WaitForSeconds(0.6f);
            Vector3 moved = her.transform.position - from; moved.y = 0.0f;
            Assert.GreaterOrEqual(moved.magnitude, VoodooRules.TeleportMinRange - 0.2f, "She did not go anywhere.");
            Vector3 pushed = near.transform.position - standing; pushed.y = 0.0f;
            Assert.Less(pushed.magnitude, 0.1f, "The teleport still shoves whoever she left.");
            Assert.IsFalse(near.IsStunned, "The teleport still staggers whoever she left.");
        }
    }
}
