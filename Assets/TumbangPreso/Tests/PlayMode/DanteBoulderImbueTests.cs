using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteBoulderImbueTests
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
        [Test] public void BoulderRequiresARealHeldSlipper()
        {
            var root = new GameObject("Boulder eligibility");
            try
            {
                var motor = root.AddComponent<CharacterMotor>(); motor.enabled = false;
                var kit = new DanteHeroKit(); var context = new AbilityContext(motor, null, null);
                motor.HoldingSlipper = false;
                Assert.IsFalse(kit.AttackingSkill.CanActivate(context), "Boulder accepted an empty hand.");
                motor.HoldingSlipper = true;
                Assert.IsFalse(kit.AttackingSkill.CanActivate(context), "A stale holding flag is not a real slipper.");
                motor.IsDefender = true; Assert.IsFalse(kit.AttackingSkill.CanActivate(context));
                motor.IsDefender = false; motor.RoundActive = false;
                Assert.IsFalse(kit.AttackingSkill.CanActivate(context));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [UnityTest] public IEnumerator ImbuedThrowConcussesDefender() => ThrowAtBody(true, true);
        [UnityTest] public IEnumerator ImbuedThrowConcussesAttacker() => ThrowAtBody(false, true);
        [UnityTest] public IEnumerator OrdinaryThrowDoesNotConcuss() => ThrowAtBody(true, false);
        sealed class Observer : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 2;
            public int LocalPeerId => 2;
            public bool IsSeatlessReferee => false;
        }
        [UnityTest] public IEnumerator HeldChargeRestoresDropsAndClearsAtRoundReset()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            GameServices.Round.BeginRound();
            var caster=GameServices.Round.PlayerAt(1); caster.AbilitySystem.BindHero("dante");
            var carrier=caster.GetComponent<Carrier>(); var shoe=carrier.Held;
            Assert.IsNotNull(shoe);
            var kit=caster.AbilitySystem.Kit;
            var ctx=new AbilityContext(caster,carrier,caster.GetComponent<CombatVerbs>());
            Assert.AreEqual(35,kit.AttackingSkill.Cooldown);
            Assert.IsTrue(kit.AttackingSkill.CanActivate(ctx)); kit.AttackingSkill.Activate(ctx);
            Assert.AreEqual(SlipperAffinity.Concussed,shoe.Affinity);
            shoe.ApplySnapshotState(SlipperState.Held,caster,shoe.transform.position,Quaternion.identity,
                Vector3.zero,0,SlipperAffinity.Concussed,-1);
            Assert.AreEqual(SlipperAffinity.Concussed,shoe.Affinity);
            Assert.AreSame(shoe,carrier.Held);
            Assert.IsTrue(shoe.HostDisarm()); Assert.AreEqual(SlipperAffinity.Concussed,shoe.Affinity);
            shoe.ApplySnapshotState(SlipperState.Loose,null,shoe.transform.position,Quaternion.identity,
                Vector3.zero,0,SlipperAffinity.Concussed,-1);
            Assert.AreEqual(SlipperAffinity.Concussed,shoe.Affinity);
            Assert.IsTrue(shoe.HostForceEquip(caster));
            Object.FindAnyObjectByType<SliceRunner>().ResetWorld(0);
            Assert.AreEqual(SlipperAffinity.Normal,shoe.Affinity,"A prior round cannot retain an imbue.");
        }
        [UnityTest] public IEnumerator AcceptedObserverCannotImbueAuthoritativeSlipper()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            GameServices.Round.BeginRound();
            var caster=GameServices.Round.PlayerAt(1); caster.AbilitySystem.BindHero("dante");
            var carrier=caster.GetComponent<Carrier>();var shoe=carrier.Held;Assert.IsNotNull(shoe);
            var prior=NetAuthority.Provider;
            try
            {
                NetAuthority.Provider=new Observer();
                caster.AbilitySystem.Kit.AttackingSkill.Activate(new AbilityContext(caster,carrier,caster.GetComponent<CombatVerbs>()));
                Assert.AreEqual(SlipperAffinity.Normal,shoe.Affinity);
            }
            finally {NetAuthority.Provider=prior;}
        }
        private IEnumerator ThrowAtBody(bool defender, bool frosted)
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
            caster.AbilitySystem.BindHero("dante");
            victim.AbilitySystem.BindHero("sean");
            victim.IsDefender = defender;
            // Body tests begin after the unchanged .25-second release grace.
            // This lane also keeps the upright can out of the throw's path.
            caster.Teleport(new Vector3(-3, .12f, -5));
            victim.Teleport(new Vector3(-3, .12f, 1));
            var carrier = caster.GetComponent<Carrier>(); var shoe = carrier.Held;
            Assert.IsNotNull(shoe); shoe.enabled = false;
            var kit = (DanteHeroKit)caster.AbilitySystem.Kit;
            using (NetCue.SuppressRelay())
            {
                if (frosted)
                {
                    var context = new AbilityContext(caster, carrier, caster.GetComponent<CombatVerbs>());
                    Assert.IsTrue(kit.AttackingSkill.CanActivate(context));
                    kit.AttackingSkill.Activate(context);
                }
                carrier.HostThrowAt(caster.transform.position + Vector3.up * .65f,
                    victim.transform.position + Vector3.up * .65f, 1f);
                Assert.AreEqual(frosted ? SlipperAffinity.Concussed : SlipperAffinity.Normal, shoe.Affinity);
                Assert.AreEqual(0, Object.FindObjectsByType<DanteBoulder>(FindObjectsSortMode.None).Length, "Imbuing cannot spawn a separate rock.");
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
            Assert.AreEqual(frosted, victim.IsConcussed, "The actual body-impact path lost the Concussed payload.");
            if (frosted)
            {
                Assert.AreEqual(2.5f, victim.StatusLeft(StatusKind.Concussed), .02f);
                Assert.AreEqual(.25f, victim.StatusSpeedScale);
            }
            yield return null;
        }
    }
}
