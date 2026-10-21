using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// LOOKOUT (owner 2026-10-15, replaces KURO: CATCH!): a small ghost minds the can for 15 s and, if the can is down, slowly
    /// stands it back up, once; an attacker near the can scares it off the job. Only the host stands the can up.
    /// (Replaces `NemuCatchContractTests` and `NemuGuardMotionTests`: Catch's knock-proof can is gone.)
    /// </summary>
    public sealed class NemuLookoutTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host;
            public bool IsNetworked => !Host;
            public int LocalSlot => Host ? 0 : 1;
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _provider;
        private CharacterMotor _motor, _attacker;
        private Lata _can;
        private NemuHeroKit _kit;
        private AbilityContext _context;

        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider; yield return PlayModeWorld.Reset();
            GameServices.Ensure(); NetAuthority.Provider = new Peer { Host = true };
            var owner = new GameObject("Lookout defender");
            _motor = owner.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 0; _motor.IsDefender = true; _motor.Mode = GameMode.HeroStrike;
            _motor.transform.position = new Vector3(4f, 0f, 0f);
            _attacker = new GameObject("Lookout attacker").AddComponent<CharacterMotor>(); _attacker.enabled = false;
            _attacker.PlayerSlot = 1; _attacker.transform.position = new Vector3(0f, 0f, 10f);
            GameServices.Round.Register(_motor); GameServices.Round.Register(_attacker);
            _can = new GameObject("Lookout can").AddComponent<Lata>(); _can.enabled = false;
            GameServices.Round.Lata = _can;
            GameServices.Round.BeginRound();
            _motor.transform.position = new Vector3(4f, 0f, 0f); _attacker.transform.position = new Vector3(0f, 0f, 10f);
            _can.transform.position = Vector3.zero;
            Assert.IsTrue(GameServices.Round.RoundActive);
            _context = new AbilityContext(_motor, null, null);
            _kit = new NemuHeroKit(); _kit.SetRole(true, _context);
            yield return null;
        }

        [UnityTearDown] public IEnumerator After()
        {
            _kit?.ResetForRound(_context);
            NemuLookout.RetireAll();
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider;
        }

        private void Cast()
        {
            Assert.IsTrue(_kit.Skill2.CanActivate(_context));
            using (NetCue.SuppressRelay()) _kit.Skill2.Activate(_context);
        }

        private void Tick(float seconds)
        {
            using (NetCue.SuppressRelay())
                for (float t = 0f; t < seconds - 1e-4f; t += .05f) _kit.Skill2.Tick(_context, .05f);
        }

        private void KnockDown() { using (NetCue.SuppressRelay()) _can.HostKnockDown(1); Assert.IsFalse(_can.IsUpright); }

        [Test] public void IdentityAndClockMatchTheOwnersBrief()
        {
            Assert.AreEqual("LOOKOUT", _kit.DefendingSkill.Name);
            Assert.AreEqual("nemu_skill2d", _kit.DefendingSkill.Id);
            Assert.AreEqual(15f, _kit.DefendingSkill.Duration, .001f);
            Assert.AreEqual(25f, _kit.DefendingSkill.Cooldown, .001f);
            _motor.IsDefender = false;
            Assert.IsFalse(_kit.DefendingSkill.CanActivate(_context), "Lookout is the defender's skill.");
        }

        [Test] public void ItSummonsItsGhostAndNoLongerMakesTheCanKnockProof()
        {
            Cast();
            Assert.IsNotNull(NemuLookout.OwnedBy(0), "No Lookout ghost at the can.");
            Assert.IsFalse(_can.IsProtected, "Lookout must not be Catch's knock-proof can.");
            KnockDown();
        }

        [Test] public void ItSlowlyStandsADownedCanBackUpOnceThenGoes()
        {
            Cast();
            KnockDown();
            Tick(NecroRules.LookoutRestoreSeconds - .2f);
            Assert.IsFalse(_can.IsUpright, "It stood the can up faster than its 3.5 s.");
            Tick(.3f);
            Assert.IsTrue(_can.IsUpright, "Lookout never stood the downed can back up.");
            Tick(1f);
            Assert.IsFalse(_kit.Skill2.IsActive, "Its job done, Lookout should go.");
        }

        [Test] public void AnAttackerAtTheCanScaresItOffTheJob()
        {
            Cast();
            KnockDown();
            _attacker.transform.position = _can.transform.position + Vector3.right * (NecroRules.LookoutScareRadius - .3f);
            Tick(NecroRules.LookoutRestoreSeconds + 1f);
            Assert.IsFalse(_can.IsUpright, "An attacker on the can did not scare Lookout off.");
            _attacker.transform.position = new Vector3(0f, 0f, 10f);
            Tick(NecroRules.LookoutRestoreSeconds + .2f);
            Assert.IsTrue(_can.IsUpright, "Once the attacker left, Lookout should finish the job.");
        }

        [Test] public void AnObserverRunsTheGhostButCannotStandTheCanUp()
        {
            KnockDown();
            NetAuthority.Provider = new Peer { Host = false };
            using (NetCue.SuppressRelay()) _kit.Skill2.Activate(_context);
            Tick(NecroRules.LookoutRestoreSeconds + .5f);
            Assert.IsFalse(_can.IsUpright, "Only the host stands the can up.");
        }

        [Test] public void ItLeavesAfterFifteenSeconds()
        {
            Cast();
            Tick(NecroRules.LookoutSeconds - .2f);
            Assert.IsTrue(_kit.Skill2.IsActive);
            Tick(.4f);
            Assert.IsFalse(_kit.Skill2.IsActive);
        }

        [Test] public void RecoveryUsesTheRemainingClockWithoutRecastingOrSpending()
        {
            Assert.IsInstanceOf<IPreparedWorldReplication>(_kit.Skill2);
            var recovery = (IPreparedWorldReplication)_kit.Skill2;
            Assert.IsFalse(recovery.RestorePreparedWorld(_context, _can.transform.position, 0, 6f));
            Assert.AreEqual(0f, _kit.Skill2.CooldownRemaining);
            Assert.IsTrue(recovery.CapturePreparedWorld(out var centre, out var preparation, out var remaining));
            Assert.AreEqual(_can.transform.position, centre); Assert.Zero(preparation); Assert.AreEqual(6f, remaining, .001f);
            Assert.IsFalse(recovery.RestorePreparedWorld(_context, centre, 0, 0));
            Assert.IsFalse(recovery.RestorePreparedWorld(_context, centre, 0, 16));
        }
    }
}
