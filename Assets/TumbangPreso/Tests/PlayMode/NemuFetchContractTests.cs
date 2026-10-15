using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// SKITTER (owner 2026-10-15, replaces KURO: FETCH!): a ghost of hers fetches her loose slipper; protocol95's delivery
    /// rules unchanged. Where these drove Kuro (`ApplyCastAnchor`), they now put Skitter's hands on the spot.
    /// </summary>
    public sealed class NemuFetchContractTests
    {
        private sealed class Host : INetProvider
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
        private GameObject _owner, _shoeRoot;
        private CharacterMotor _motor;
        private Slipper _shoe;
        private NemuHeroKit _kit;
        private AbilityContext _context;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _previous = NetAuthority.Provider;
            NetAuthority.Provider = new Host();
            _owner = new GameObject("Fetch owner");
            _motor = _owner.AddComponent<CharacterMotor>();
            _motor.PlayerSlot = 0;
            _motor.enabled = false;
            _shoeRoot = new GameObject("Owned slipper");
            _shoe = _shoeRoot.AddComponent<Slipper>();
            _shoe.OwnerSlot = 0;
            _shoe.enabled = false;
            _shoe.transform.position = Vector3.forward * 4f;
            _kit = new NemuHeroKit();
            _context = new AbilityContext(_motor, null, null);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            _kit.ResetForRound(_context);
            NemuSkitter.RetireAll();
            Object.Destroy(_owner);
            Object.Destroy(_shoeRoot);
            NetAuthority.Provider = _previous;
            yield return null;
        }

        private NemuSkitter Ghost => NemuSkitter.OwnedBy(0);

        private void Collect()
        {
            using (NetCue.SuppressRelay())
            {
                _kit.AttackingSkill.Activate(_context);
                Assert.That(Ghost, Is.Not.Null, "No Skitter was sent.");
                Ghost.TeleportHands(_shoe.transform.position + Vector3.up * 0.1f);
                _kit.Tick(_context, 0.01f);
                Assert.That(Ghost.Arrived, Is.True);
            }
        }

        [Test]
        public void FetchNameAndCooldownMatchTheCurrentContract()
        {
            Assert.That(_kit.AttackingSkill.Name, Is.EqualTo("SKITTER"));
            Assert.That(_kit.AttackingSkill.Id, Is.EqualTo("nemu_skill2"));
            Assert.That(_kit.AttackingSkill.Cooldown, Is.EqualTo(25f));
        }

        [Test]
        public void AtHerSideItWaitsHoldingTheSlipperUntilSheTakesIt()
        {
            // Owner 2026-10-15: *"she has to click a button (we have an interact button) to get slipper from the guy"*.
            Collect();
            Ghost.TeleportHands(_motor.transform.position + Vector3.up * 0.3f);
            using (NetCue.SuppressRelay()) for (int i = 0; i < 30; i++) _kit.Tick(_context, 0.05f);
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose), "It should not put it in her hand by itself.");
            Assert.That(_motor.HoldingSlipper, Is.False);
            Assert.That(_kit.AttackingSkill.IsActive, Is.True, "It should wait by her.");
            Assert.That(Ghost, Is.Not.Null);
            Assert.That(Ghost.Carrying && Ghost.Offering, Is.True);
            Assert.That(Vector3.Distance(_shoe.transform.position, Ghost.HoldPoint), Is.LessThan(0.05f));
            // Her interact: the normal grab, in reach of where it holds it out.
            Assert.That(_shoe.CanBeGrabbedBy(_motor), Is.True);
            using (NetCue.SuppressRelay()) Assert.That(_shoe.HostGrab(_motor), Is.True);
            using (NetCue.SuppressRelay()) _kit.Tick(_context, 0.01f);
            Assert.That(_shoe.Holder, Is.SameAs(_motor));
            Assert.That(_kit.AttackingSkill.IsActive, Is.False);
            Assert.That(Ghost, Is.Null, "Skitter should be leaving once she has taken it.");
        }

        [Test]
        public void ARealPickupDuringTheReturnStopsFetchWithoutMovingTheHeldSlipper()
        {
            Collect();
            Assert.That(_shoe.HostForceEquip(_motor), Is.True);
            var heldPosition = _shoe.transform.position;
            Ghost.TeleportHands(Vector3.right * 3f + Vector3.up);
            using (NetCue.SuppressRelay()) _kit.Tick(_context, 0.01f);
            Assert.That(_shoe.transform.position, Is.EqualTo(heldPosition));
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Held));
            Assert.That(_shoe.Holder, Is.SameAs(_motor));
            Assert.That(_kit.AttackingSkill.IsActive, Is.False);
        }

        [Test]
        public void CancellationGroundsTheCarriedLooseSlipper()
        {
            Collect();
            Ghost.TeleportHands(Vector3.forward * 3f + Vector3.up * 2f);
            using (NetCue.SuppressRelay()) _kit.Tick(_context, 0.01f);
            _kit.AttackingSkill.EndEarly(_context);
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose));
            Assert.That(_shoe.transform.position.y, Is.LessThan(0.2f));
            Assert.That(Ghost, Is.Null);
            Assert.That(_motor.HoldingSlipper, Is.False);
        }


        [Test]
        public void CarryingItFollowsHerPastItsDuration()
        {
            Collect();
            _motor.transform.position = Vector3.forward * 30f;
            using (NetCue.SuppressRelay()) _kit.Tick(_context, 9f);
            Assert.That(_kit.AttackingSkill.IsActive, Is.True, "It should keep following her until she takes it.");
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose));
            Assert.That(_motor.HoldingSlipper, Is.False);
            Assert.That(Vector3.Distance(_shoe.transform.position, Ghost.HoldPoint), Is.LessThan(0.05f));
        }

        [Test]
        public void ExpiryBeforeItReachesTheSlipperSendsItAway()
        {
            using (NetCue.SuppressRelay())
            {
                _kit.AttackingSkill.Activate(_context);
                _shoe.transform.position = Vector3.forward * 200f;
                _kit.Tick(_context, 9f);
            }
            Assert.That(_kit.AttackingSkill.IsActive, Is.False);
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose));
            Assert.That(Ghost, Is.Null);
        }

        [Test]
        public void ChangedOwnershipStopsTheOldFetchWithoutMovingTheSlipper()
        {
            Collect();
            _shoe.OwnerSlot = 1;
            var position = _shoe.transform.position;
            Ghost.TeleportHands(Vector3.right * 3f + Vector3.up);
            using (NetCue.SuppressRelay()) _kit.Tick(_context, 0.01f);
            Assert.That(_shoe.transform.position, Is.EqualTo(position));
            Assert.That(_kit.AttackingSkill.IsActive, Is.False);
        }
        [Test]
        public void DefenderInterceptionDropsForNormalPickup()
        {
            GameServices.Ensure();
            var defenderRoot = new GameObject("Fetch interceptor");
            var defender = defenderRoot.AddComponent<CharacterMotor>();
            defender.enabled = false;
            defender.PlayerSlot = 1;
            defender.IsDefender = true;
            GameServices.Round.Register(defender);
            try
            {
                Collect();
                Ghost.TeleportHands(Vector3.forward * 3f + Vector3.up * 0.5f);
                defender.transform.position = Vector3.forward * 3f;
                Assert.That(defender.CanAct(), Is.True);
                using (NetCue.SuppressRelay()) _kit.Tick(_context, 0.01f);
                Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose));
                Assert.That(_shoe.transform.position.y, Is.LessThan(0.2f));
                // (Within a step: it carries toward her for the one tick before the taya is on it.)
                Assert.That(_shoe.transform.position.z, Is.EqualTo(3f).Within(0.15f));
                Assert.That(_kit.AttackingSkill.IsActive, Is.False);
                Assert.That(_motor.HoldingSlipper, Is.False);
            }
            finally
            {
                GameServices.Round.Unregister(defender);
                Object.DestroyImmediate(defenderRoot);
            }
        }

        [Test]
        public void ObserverCannotMoveOrLandTheAuthoritativeSlipper()
        {
            Collect();
            NetAuthority.Provider = new Observer();
            var position = _shoe.transform.position;
            Ghost.TeleportHands(Vector3.up * 0.3f);
            using (NetCue.SuppressRelay()) _kit.Tick(_context, 0.01f);
            _kit.AttackingSkill.EndEarly(_context);
            Assert.That(_shoe.transform.position, Is.EqualTo(position));
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose));
            Assert.That(_motor.HoldingSlipper, Is.False);
        }

        [Test]
        public void CannotStartWhileTheOwnedSlipperIsInHand()
        {
            Assert.That(_shoe.HostForceEquip(_motor), Is.True);
            Assert.That(_kit.AttackingSkill.CanActivate(_context), Is.False);
        }
    }
}
