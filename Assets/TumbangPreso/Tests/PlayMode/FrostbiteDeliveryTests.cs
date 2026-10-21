using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class FrostbiteDeliveryTests
    {
        private INetProvider _net;
        private CustomRules _rules;
        private bool _pinned, _bots, _spectator;
        private int _seat;
        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone();
            _pinned = SceneFlow.RulesPinned; _bots = GameLaunch.AllBots;
            _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        [Test] public void FrostbiteRequiresAHeldSlipperAndAnActionableAttacker()
        {
            var root = new GameObject("Frostbite eligibility");
            try
            {
                var motor = root.AddComponent<CharacterMotor>(); motor.enabled = false;
                var kit = new CheskaHeroKit(); var context = new AbilityContext(motor, null, null);
                motor.HoldingSlipper = false;
                Assert.IsFalse(kit.AttackingSkill.CanActivate(context), "Frostbite accepted an empty hand.");
                motor.HoldingSlipper = true;
                Assert.IsTrue(kit.AttackingSkill.CanActivate(context));
                motor.IsDefender = true; Assert.IsFalse(kit.AttackingSkill.CanActivate(context));
                motor.IsDefender = false; motor.RoundActive = false;
                Assert.IsFalse(kit.AttackingSkill.CanActivate(context));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [UnityTest] public IEnumerator ARealFrostedThrowFreezesTheDefender() => ThrowAtBody(true, true);
        [UnityTest] public IEnumerator ARealFrostedThrowFreezesAnotherAttacker() => ThrowAtBody(false, true);
        [UnityTest] public IEnumerator AnOrdinaryBodyBlockDoesNotFreeze() => ThrowAtBody(true, false);
        [UnityTest] public IEnumerator ARestoredFrostedThrowStillFreezesTheDefender() => ThrowAtBody(true, true, true);
        private IEnumerator ThrowAtBody(bool defender, bool frosted, bool restored = false)
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            GameServices.Round.BeginRound();
            foreach (var who in GameServices.Round.Players)
            {
                who.enabled = false; who.GetComponent<Carrier>().enabled = false;
                who.Teleport(new Vector3(6, .12f, 6 + who.PlayerSlot));
                who.IsDefender = false;
            }
            var caster = GameServices.Round.PlayerAt(1);
            var victim = GameServices.Round.PlayerAt(defender ? 0 : 2);
            caster.AbilitySystem.BindHero("cheska");
            victim.AbilitySystem.BindHero("sean");
            victim.IsDefender = defender;
            // Body tests begin after the unchanged .25-second release grace.
            // This lane also keeps the upright can out of the throw's path.
            caster.Teleport(new Vector3(-3, .12f, -5));
            victim.Teleport(new Vector3(-3, .12f, 1));
            var carrier = caster.GetComponent<Carrier>(); var shoe = carrier.Held;
            Assert.IsNotNull(shoe); shoe.enabled = false;
            var kit = (CheskaHeroKit)caster.AbilitySystem.Kit;
            using (NetCue.SuppressRelay())
            {
                if (frosted)
                {
                    var context = new AbilityContext(caster, carrier, caster.GetComponent<CombatVerbs>());
                    Assert.IsTrue(kit.AttackingSkill.CanActivate(context));
                    if (restored)
                    {
                        kit.AttackingSkill.ApplyNetworkSnapshot(30, 0, true);
                        Assert.IsTrue(kit.RestoreTimedKit(caster, new TimedKitSnapshot(kit.AttackingSkill, 5)));
                        Assert.AreEqual(30, kit.AttackingSkill.CooldownRemaining);
                    }
                    else kit.AttackingSkill.Activate(context);
                }
                carrier.HostThrowAt(caster.transform.position + Vector3.up * .65f,
                    victim.transform.position + Vector3.up * .65f, 1f);
                Assert.AreEqual(frosted ? SlipperAffinity.Frost : SlipperAffinity.Normal, shoe.Affinity);
                Assert.IsFalse(kit.IsFrostbiteLoaded, "A real release must consume the held load.");
                Physics.SyncTransforms();
                for (int step = 0; step < 40; step++)
                {
                    shoe.SendMessage("FixedUpdate");
                    int contacts = (int)typeof(Slipper).GetField("_bodyContacts",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shoe);
                    if ((contacts & (1 << victim.PlayerSlot)) != 0) break;
                }
            }
            int touched = (int)typeof(Slipper).GetField("_bodyContacts",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shoe);
            Assert.AreNotEqual(0, touched & (1 << victim.PlayerSlot), "The actual throw must hit the requested body.");
            Assert.AreEqual(SlipperAffinity.Normal, shoe.Affinity, "A body impact must spend the payload.");
            Assert.AreEqual(frosted ? StunElement.Ice : StunElement.None, victim.StunElement,
                "The actual body-impact path lost its frost payload before applying Frozen.");
            if (frosted) Assert.AreEqual(StatusRules.FrozenSeconds, victim.StunLeft, .02f);
            yield return null;
        }
    }
}
