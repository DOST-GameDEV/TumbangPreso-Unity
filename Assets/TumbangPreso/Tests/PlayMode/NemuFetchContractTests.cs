using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
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
        private GhostPetCompanion _pet;
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
            var art = RosterBook.Load().FindPersonArt("nemu");
            var visual = _owner.AddComponent<CharacterVisual>();
            var model = new GameObject("Visual");
            model.transform.SetParent(_owner.transform, false);
            visual.SetModelRoot(model.transform);
            visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            _pet = visual.Companion;
            Assert.That(_pet, Is.Not.Null);
            _pet.enabled = false;
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
            Object.Destroy(_owner);
            Object.Destroy(_shoeRoot);
            NetAuthority.Provider = _previous;
            yield return null;
        }

        private void Collect()
        {
            using (NetCue.SuppressRelay())
            {
                _kit.AttackingSkill.Activate(_context);
                _pet.ApplyCastAnchor(_shoe.transform.position + Vector3.up * 0.9f);
                Assert.That(_pet.ErrandArrived, Is.True);
                _kit.Tick(_context, 0.01f);
            }
        }

        [Test]
        public void FetchNameAndCooldownMatchTheCurrentContract()
        {
            Assert.That(_kit.AttackingSkill.Name, Is.EqualTo("KURO: FETCH!"));
            Assert.That(_kit.AttackingSkill.Cooldown, Is.EqualTo(25f));
        }

        [Test]
        public void DeliveryLeavesTheSlipperLooseBesideItsEmptyHandedOwner()
        {
            Collect();
            _pet.ApplyCastAnchor(_motor.transform.position + Vector3.up * 0.9f);
            using (NetCue.SuppressRelay()) _kit.Tick(_context, 0.01f);
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose));
            Assert.That(_shoe.Holder, Is.Null);
            Assert.That(_motor.HoldingSlipper, Is.False);
            Assert.That(_shoe.transform.position.y, Is.LessThan(0.2f));
            Assert.That(Vector3.Distance(_shoe.transform.position, _motor.transform.position), Is.LessThan(1.2f));
            Assert.That(_kit.AttackingSkill.IsActive, Is.False);
            Assert.That(_pet.OnErrand, Is.False);
            Assert.That(_shoe.CanBeGrabbedBy(_motor), Is.True);
            using (NetCue.SuppressRelay()) Assert.That(_shoe.HostGrab(_motor), Is.True);
            Assert.That(_motor.HoldingSlipper, Is.True);
        }

        [Test]
        public void ARealPickupDuringTheReturnStopsFetchWithoutMovingTheHeldSlipper()
        {
            Collect();
            Assert.That(_shoe.HostForceEquip(_motor), Is.True);
            var heldPosition = _shoe.transform.position;
            _pet.ApplyCastAnchor(Vector3.right * 3f + Vector3.up);
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
            _pet.ApplyCastAnchor(Vector3.forward * 3f + Vector3.up * 2f);
            using (NetCue.SuppressRelay()) _kit.Tick(_context, 0.01f);
            _kit.AttackingSkill.EndEarly(_context);
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose));
            Assert.That(_shoe.transform.position.y, Is.LessThan(0.2f));
            Assert.That(_pet.OnErrand, Is.False);
            Assert.That(_motor.HoldingSlipper, Is.False);
        }


        [Test]
        public void ExpiryGroundsTheCarriedSlipperWithoutEquippingIt()
        {
            Collect();
            _pet.ApplyCastAnchor(Vector3.forward * 3f + Vector3.up * 2f);
            using (NetCue.SuppressRelay()) _kit.Tick(_context, 9f);
            Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose));
            Assert.That(_shoe.transform.position.y, Is.LessThan(0.2f));
            Assert.That(_motor.HoldingSlipper, Is.False);
            Assert.That(_pet.OnErrand, Is.False);
        }

        [Test]
        public void ChangedOwnershipStopsTheOldFetchWithoutMovingTheSlipper()
        {
            Collect();
            _shoe.OwnerSlot = 1;
            var position = _shoe.transform.position;
            _pet.ApplyCastAnchor(Vector3.right * 3f + Vector3.up);
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
                _pet.ApplyCastAnchor(Vector3.forward * 3f + Vector3.up * 0.9f);
                defender.transform.position = Vector3.forward * 3f;
                Assert.That(defender.CanAct(), Is.True);
                using (NetCue.SuppressRelay()) _kit.Tick(_context, 0.01f);
                Assert.That(_shoe.State, Is.EqualTo(SlipperState.Loose));
                Assert.That(_shoe.transform.position.y, Is.LessThan(0.2f));
                Assert.That(_shoe.transform.position.z, Is.EqualTo(3f).Within(0.01f));
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
            _pet.ApplyCastAnchor(Vector3.up * 0.9f);
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
