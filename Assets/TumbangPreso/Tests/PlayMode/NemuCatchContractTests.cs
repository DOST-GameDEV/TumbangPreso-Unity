using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class NemuCatchContractTests
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
        private CharacterMotor _motor;
        private Lata _can;
        private NemuHeroKit _kit;
        private AbilityContext _context;
        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider; yield return PlayModeWorld.Reset();
            GameServices.Ensure(); NetAuthority.Provider = new Peer { Host = true };
            var owner = new GameObject("Catch defender");
            _motor = owner.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 0; _motor.IsDefender = true; _motor.Mode = GameMode.HeroStrike;
            _can = new GameObject("Catch can").AddComponent<Lata>(); _can.enabled = false;
            GameServices.Round.Lata = _can;
            _context = new AbilityContext(_motor, null, null);
            _kit = new NemuHeroKit(); _kit.SetRole(true, _context);
        }
        [UnityTearDown] public IEnumerator After()
        {
            _kit?.ResetForRound(_context);
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider;
        }
        private void Cast()
        {
            Assert.IsTrue(_kit.Skill2.CanActivate(_context));
            using (NetCue.SuppressRelay()) _kit.Skill2.Activate(_context);
        }
        [Test] public void CatchRejectsADownedCanAndAnAttacker()
        {
            Assert.IsTrue(_kit.Skill2.CanActivate(_context));
            using (NetCue.SuppressRelay()) _can.HostKnockDown(1);
            Assert.IsFalse(_can.IsUpright);
            Assert.IsFalse(_kit.Skill2.CanActivate(_context), "Catch accepted a downed can.");
            using (NetCue.SuppressRelay()) _can.HostRestore();
            _motor.IsDefender = false;
            Assert.IsFalse(_kit.DefendingSkill.CanActivate(_context));
        }
        [Test] public void CatchPreventsRealKnockdownForItsExactFiveSecondClockWithoutAModel()
        {
            Cast();
            using (NetCue.SuppressRelay()) _can.HostKnockDown(1);
            Assert.IsTrue(_can.IsUpright, "A legal Catch did not protect the actual objective.");
            Assert.AreEqual(5f, _can.ProtectionLeft, .001f);
            Assert.AreEqual(25f, _kit.Skill1.CooldownRemaining, .001f);
            _kit.Skill2.Tick(_context, 4.99f);
            Assert.IsTrue(_can.IsProtected);
            _kit.Skill2.Tick(_context, .02f);
            Assert.IsFalse(_can.IsProtected);
            using (NetCue.SuppressRelay()) _can.HostKnockDown(1);
            Assert.IsFalse(_can.IsUpright, "The expired Catch still prevented knockdown.");
        }
        [Test] public void CancellingCatchPreservesTheIndependentRestoreProtection()
        {
            using (NetCue.SuppressRelay()) { _can.HostKnockDown(1); _can.HostRestore(); }
            float restored = _can.ProtectionLeft; Assert.Greater(restored, 0);
            Cast(); Assert.AreEqual(5f, _can.ProtectionLeft, .001f);
            _kit.Skill2.EndEarly(_context);
            Assert.AreEqual(restored, _can.ProtectionLeft, .001f,
                "Cancelling Catch cleared another protection owner's clock.");
        }
        [Test] public void RoundResetReleasesCatchImmediately()
        {
            Cast(); Assert.IsTrue(_can.IsProtected);
            _kit.ResetForRound(_context);
            Assert.IsFalse(_can.IsProtected);
        }
        [Test] public void AnObservingReplicaCannotGrantObjectiveProtection()
        {
            NetAuthority.Provider = new Peer { Host = false };
            using (NetCue.SuppressRelay()) _kit.Skill2.Activate(_context);
            Assert.IsFalse(_can.IsProtected);
        }
        [Test] public void CatchRecoveryUsesTheRemainingClockWithoutRecastingOrSpending()
        {
            Assert.IsInstanceOf<IPreparedWorldReplication>(_kit.Skill2);
            var recovery = (IPreparedWorldReplication)_kit.Skill2;
            Assert.IsFalse(recovery.RestorePreparedWorld(_context, _can.transform.position, 0, 3f));
            Assert.AreEqual(3f, _can.ProtectionLeft, .001f);
            Assert.AreEqual(0f, _kit.Skill2.CooldownRemaining);
            Assert.IsTrue(recovery.CapturePreparedWorld(out var centre, out var preparation, out var remaining));
            Assert.AreEqual(_can.transform.position, centre); Assert.Zero(preparation); Assert.AreEqual(3f, remaining);
            Assert.IsFalse(recovery.RestorePreparedWorld(_context, centre, 0, 0));
            Assert.IsFalse(_can.IsProtected);
            Assert.IsFalse(recovery.RestorePreparedWorld(_context, centre, 0, 6));
        }
        [Test] public void ApprovedRecoveryProjectsProtectionOnAnObserverAndThenExpires()
        {
            NetAuthority.Provider = new Peer { Host = false };
            Assert.IsTrue(HeroAbilitySystem.RestorePreparedWorld(_motor, _kit.Skill2,
                _can.transform.position, 0, 3));
            Assert.AreEqual(3f, _can.ProtectionLeft, .001f,
                "An accepted recovery must project the protection clock on the observing peer.");
            _kit.Skill2.Tick(_context, 3.01f);
            Assert.IsFalse(_can.IsProtected);
            using (NetCue.SuppressRelay()) _can.HostKnockDown(1);
            Assert.IsTrue(_can.IsUpright, "A replica still cannot decide a knockdown after its projection expires.");
        }
        [Test] public void ResetAfterTheProtectedCanIsDestroyedDoesNotRebuildItsShell()
        {
            using (NetCue.SuppressRelay()) { _can.HostKnockDown(1); _can.HostRestore(); }
            Cast(); Object.DestroyImmediate(_can.gameObject);
            Assert.DoesNotThrow(() => _kit.ResetForRound(_context));
        }
    }
}
