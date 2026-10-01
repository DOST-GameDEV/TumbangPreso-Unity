using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class NemuSitContractTests
    {
        private sealed class Offline : INetProvider
        {
            public bool IsHost => true;
            public bool IsNetworked => false;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }

        private sealed class Observer : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        private INetProvider _previous;
        private GameObject _root;
        private CharacterMotor _motor;
        private NemuHeroKit _kit;
        private AbilityContext _context;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _previous = NetAuthority.Provider;
            NetAuthority.Provider = new Offline();
            _root = new GameObject("Kuro Sit contract owner");
            _motor = _root.AddComponent<CharacterMotor>();
            _motor.PlayerSlot = 0;
            _motor.enabled = false;
            _kit = new NemuHeroKit();
            _context = new AbilityContext(_motor, null, null, Vector3.zero,
                Vector3.forward, Vector3.forward * 2f);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            _kit.ResetForRound(_context);
            Object.Destroy(_root);
            NetAuthority.Provider = _previous;
            yield return null;
        }

        private IPreparedWorldReplication Recovery()
        {
            Assert.That(_kit.Skill1, Is.InstanceOf<IPreparedWorldReplication>());
            return (IPreparedWorldReplication)_kit.Skill1;
        }

        [Test]
        public void SignatureMatchesTheCurrentSitContract()
        {
            Assert.That(_kit.Skill1.Id, Is.EqualTo("nemu_skill1"));
            Assert.That(_kit.Skill1.Name, Is.EqualTo("KURO: SIT!"));
            Assert.That(_kit.Skill1.Duration, Is.EqualTo(10f));
            Assert.That(_kit.Skill1.Cooldown, Is.EqualTo(25f));
            Assert.That(_kit.Skill1.CanReactivate, Is.True);
        }

        [Test]
        public void RecallMovesTheRealMotorOnceWithoutRestartingCooldown()
        {
            using (NetCue.SuppressRelay())
            {
                Assert.That(_kit.CastSkill1(_context), Is.EqualTo(HeroKit.CastOutcome.Cast));
                _kit.Tick(_context, 2f);
                float remaining = _kit.Skill1.CooldownRemaining;
                Assert.That(_kit.CastSkill1(_context), Is.EqualTo(HeroKit.CastOutcome.Cast));
                Assert.That(_motor.transform.position.z, Is.EqualTo(2f).Within(0.01f));
                Assert.That(_kit.Skill1.IsActive, Is.False);
                Assert.That(_kit.Skill1.CooldownRemaining, Is.EqualTo(remaining));
                Assert.That(_kit.AttackingSkill.CooldownRemaining, Is.EqualTo(remaining));
                Assert.That(_kit.CastSkill1(_context), Is.EqualTo(HeroKit.CastOutcome.Cooling));
                Assert.That(_motor.transform.position.z, Is.EqualTo(2f).Within(0.01f));
            }
        }

        [Test]
        public void ExpiryDoesNotRecallTheOwner()
        {
            using (NetCue.SuppressRelay()) _kit.Skill1.Activate(_context);
            _kit.Tick(_context, 10.1f);
            _kit.Skill1.Reactivate(_context);
            Assert.That(_motor.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(_kit.Skill1.IsActive, Is.False);
        }

        [Test]
        public void RoundResetDoesNotRecallTheOwner()
        {
            using (NetCue.SuppressRelay()) _kit.Skill1.Activate(_context);
            _kit.ResetForRound(_context);
            Assert.That(_motor.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(_kit.Skill1.IsActive, Is.False);
            Assert.That(_kit.Skill1.CooldownRemaining, Is.Zero);
        }

        [Test]
        public void RecoveryRestoresTheAnchorWithoutSpendingResources()
        {
            var recovery = Recovery();
            _kit.Skill1.ApplyNetworkSnapshot(7f, 0);
            recovery.RestorePreparedWorld(_context, new Vector3(1f, 0f, 3f), 0f, 6f);
            Assert.That(recovery.CapturePreparedWorld(out var centre, out var preparation, out var remaining), Is.True);
            Assert.That(centre, Is.EqualTo(new Vector3(1f, 0f, 3f)));
            Assert.That(preparation, Is.Zero);
            Assert.That(remaining, Is.EqualTo(6f));
            Assert.That(_kit.Skill1.CooldownRemaining, Is.EqualTo(7f));
            _kit.Skill1.Reactivate(_context);
            Assert.That(_motor.transform.position.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(_motor.transform.position.z, Is.EqualTo(3f).Within(0.01f));
            Assert.That(_kit.Skill1.CooldownRemaining, Is.EqualTo(7f));
        }

        [Test]
        public void EmptyRecoveryEndsTheAnchorWithoutTeleporting()
        {
            var recovery = Recovery();
            using (NetCue.SuppressRelay()) _kit.Skill1.Activate(_context);
            recovery.RestorePreparedWorld(_context, Vector3.zero, 0f, 0f);
            Assert.That(recovery.CapturePreparedWorld(out _, out _, out _), Is.False);
            Assert.That(_motor.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(_kit.Skill1.CooldownRemaining, Is.EqualTo(25f));
        }

        [Test]
        public void InvalidRecoveryCannotChangeAnExistingAnchor()
        {
            var recovery = Recovery();
            var anchor = new Vector3(1f, 0f, 3f);
            recovery.RestorePreparedWorld(_context, anchor, 0f, 6f);
            recovery.RestorePreparedWorld(_context, Vector3.right * 4f, 1f, 6f);
            recovery.RestorePreparedWorld(_context, Vector3.right * 4f, 0f, 11f);
            recovery.RestorePreparedWorld(_context, new Vector3(float.NaN, 0f, 0f), 0f, 6f);
            Assert.That(recovery.CapturePreparedWorld(out var centre, out _, out var remaining), Is.True);
            Assert.That(centre, Is.EqualTo(anchor));
            Assert.That(remaining, Is.EqualTo(6f));
        }

        [Test]
        public void ObserverPlaybackCannotTeleportAnotherPlayersMotor()
        {
            NetAuthority.Provider = new Observer();
            var context = new AbilityContext(_motor, null, null, Vector3.zero,
                Vector3.forward, Vector3.forward * 2f);
            using (NetCue.SuppressRelay())
            {
                _kit.Skill1.Activate(context);
                _kit.Skill1.Reactivate(context);
            }
            Assert.That(_motor.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(_kit.Skill1.IsActive, Is.False);
        }

        [Test]
        public void InactiveRoundCannotRecallAnExistingAnchor()
        {
            using (NetCue.SuppressRelay()) _kit.Skill1.Activate(_context);
            _motor.RoundActive = false;
            Assert.That(_kit.CastSkill1(_context), Is.EqualTo(HeroKit.CastOutcome.NotYet));
            Assert.That(_motor.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(_kit.Skill1.IsActive, Is.True);
        }
        [UnityTest, Timeout(60000)]
        public IEnumerator ActualCompanionStaysAtTheAnchorWhenTheOwnerMoves()
        {
            var art = RosterBook.Load().FindPersonArt("nemu");
            Assert.That(art, Is.Not.Null);
            Assert.That(art.PetModel, Is.Not.Null);
            var visual = _root.AddComponent<CharacterVisual>();
            var modelRoot = new GameObject("Visual");
            modelRoot.transform.SetParent(_root.transform, false);
            visual.SetModelRoot(modelRoot.transform);
            visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            var pet = visual.Companion;
            Assert.That(pet, Is.Not.Null);

            using (NetCue.SuppressRelay()) _kit.Skill1.Activate(_context);
            Assert.That(Recovery().CapturePreparedWorld(out var anchor, out _, out _), Is.True);
            _motor.Teleport(new Vector3(3f, 0f, 0f));
            _kit.Tick(_context, 0.1f);
            yield return null;
            yield return null;

            Assert.That(pet.OnErrand, Is.True);
            Assert.That(pet.transform.position.x, Is.EqualTo(anchor.x).Within(0.01f));
            Assert.That(pet.transform.position.z, Is.EqualTo(anchor.z).Within(0.01f));
            Assert.That(_motor.transform.position.x, Is.EqualTo(3f).Within(0.01f));
            _kit.Skill1.Reactivate(_context);
            Assert.That(pet.OnErrand, Is.False);
            Assert.That(_motor.transform.position.z, Is.EqualTo(anchor.z).Within(0.01f));
        }
    }
}
