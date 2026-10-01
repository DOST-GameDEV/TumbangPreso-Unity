using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ZackBankShotTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [TearDown] public void Cleanup()
        { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }
        AbilityContext Actor(out ZackHeroKit kit)
        {
            var go = Track(new GameObject("Bank owner"));
            var body = go.AddComponent<CharacterMotor>(); body.PlayerSlot = 1; body.enabled = false;
            var system = go.AddComponent<HeroAbilitySystem>(); system.enabled = false; system.BindHero("zack");
            var carrier = go.AddComponent<Carrier>(); carrier.enabled = false;
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            kit = (ZackHeroKit)system.Kit;
            return new AbilityContext(body, carrier, null);
        }
        Slipper Shoe(AbilityContext ctx)
        {
            var shoe = Track(new GameObject("Bank slipper")).AddComponent<Slipper>(); shoe.enabled = false;
            shoe.OwnerSlot = ctx.Motor.PlayerSlot; shoe.SeatOfOrigin = ctx.Motor.PlayerSlot;
            shoe.HostForceEquip(ctx.Motor); return shoe;
        }
        [Test] public void BankShotUsesCooldownAndEightSecondLoad()
        {
            var kit = new ZackHeroKit();
            Assert.AreEqual("BANK SHOT", kit.AttackingSkill.Name);
            Assert.AreEqual(35, kit.AttackingSkill.Cooldown);
            Assert.AreEqual(8, kit.AttackingSkill.Duration);
            Assert.IsFalse(kit.AttackingSkill.UsesCharges);
        }
        [Test] public void BankShotRequiresHeldSlipperInsteadOfRecallingLooseOne()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            Assert.IsTrue(kit.AttackingSkill.CanActivate(ctx), "A held slipper must be loadable.");
            shoe.HostThrow(ctx.Motor, Vector3.up, Vector3.forward);
            Assert.IsFalse(kit.AttackingSkill.CanActivate(ctx));
            shoe.ApplySnapshotState(SlipperState.Loose, null, Vector3.zero, Quaternion.identity, Vector3.zero, 0, SlipperAffinity.Normal, -1);
            Assert.IsFalse(kit.AttackingSkill.CanActivate(ctx), "A loose shoe must still be retrieved normally.");
            Assert.IsNull(ctx.Carrier.Held);
        }
        [Test] public void ActualCarrierThrowConsumesLoadWithoutSpeedBoostOrZap()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            Assert.IsTrue(kit.IsBankShotLoadedFor(shoe));
            Vector3 origin = new Vector3(0, 1, -8), target = new Vector3(0, 0, 0);
            var expected = shoe.LaunchVelocityTo(origin, target, 1);
            using (NetCue.SuppressRelay()) ctx.Carrier.HostThrowAt(origin, target, 1);
            Assert.AreEqual(SlipperState.InFlight, shoe.State);
            Assert.AreEqual(SlipperAffinity.BankShot, shoe.Affinity);
            Assert.Less(Vector3.Distance(expected, shoe.Velocity), .001f);
            Assert.IsFalse(kit.IsOverchargeThrowActive); Assert.IsNull(ctx.Carrier.Held);
            kit.AttackingSkill.Tick(ctx, .02f); Assert.IsFalse(kit.AttackingSkill.IsActive);
            Assert.Greater(kit.AttackingSkill.CooldownRemaining, 34);
        }
        [Test] public void LoadExpiresDropsAndRoundResetDoNotTransferToAnotherShoe()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            kit.AttackingSkill.Tick(ctx, 8); Assert.IsFalse(kit.IsBankShotLoadedFor(shoe));
            kit.AttackingSkill.ApplyNetworkSnapshot(0, 0, true);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            shoe.ApplySnapshotState(SlipperState.Loose, null, Vector3.zero, Quaternion.identity, Vector3.zero, 0, SlipperAffinity.Normal, -1);
            kit.AttackingSkill.Tick(ctx, .02f); Assert.IsFalse(kit.IsOverchargeThrowActive);
            var replacement = Shoe(ctx); Assert.IsFalse(kit.IsBankShotLoadedFor(replacement));
            kit.AttackingSkill.ApplyNetworkSnapshot(0, 0, true);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            kit.ResetForRound(ctx); Assert.IsFalse(kit.IsBankShotLoadedFor(replacement));
        }
        static readonly MethodInfo Bounds = typeof(Slipper).GetMethod("BounceOffBounds", BindingFlags.Instance | BindingFlags.NonPublic);
        void Bank(Slipper shoe, bool corner = false)
        {
            shoe.transform.position = new Vector3(AIController.PlayableMaxX + 1, 1, corner ? AIController.PlayableMaxZ + 1 : 0);
            using (NetCue.SuppressRelay()) Bounds.Invoke(shoe, null);
        }
        [Test] public void PoweredCornerCountsOnceAndSecondNormalBankLosesCredit()
        {
            var ctx = Actor(out _); var shoe = Shoe(ctx);
            shoe.HostThrow(ctx.Motor, Vector3.up, new Vector3(10, 2, 8), SlipperAffinity.BankShot);
            var incoming = shoe.Velocity.magnitude; Bank(shoe, true);
            Assert.AreEqual(1, shoe.BankCount); Assert.AreEqual(1, shoe.ThrowerSlot);
            Assert.AreEqual(incoming * .85f, shoe.Velocity.magnitude, .001f);
            Assert.AreEqual(SlipperAffinity.Normal, shoe.Affinity);
            Bank(shoe); Assert.AreEqual(2, shoe.BankCount); Assert.AreEqual(-1, shoe.ThrowerSlot);
        }
        [Test] public void OverclockThrowHasTwoLosingBanksThenClearsCreditAndCannotLeakToNextThrow()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            Assert.IsTrue(kit.RestoreTimedKit(ctx.Motor, new TimedKitSnapshot(kit.AttackingSkill, 0, kit.Ultimate, 0, ultimatePermanent: true)));
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            Assert.AreEqual(SlipperAffinity.OverclockBank, kit.BankShotAffinityFor(shoe));
            shoe.HostThrow(ctx.Motor, Vector3.up, new Vector3(10, 2, 8), kit.BankShotAffinityFor(shoe));
            float speed = shoe.Velocity.magnitude; Bank(shoe);
            Assert.AreEqual(speed * .85f, shoe.Velocity.magnitude, .001f); Assert.AreEqual(SlipperAffinity.BankShot, shoe.Affinity);
            speed = shoe.Velocity.magnitude; Bank(shoe);
            Assert.AreEqual(speed * .85f, shoe.Velocity.magnitude, .001f); Assert.AreEqual(1, shoe.ThrowerSlot);
            Assert.AreEqual(SlipperAffinity.Normal, shoe.Affinity); Bank(shoe); Assert.AreEqual(-1, shoe.ThrowerSlot);
            shoe.HostThrow(ctx.Motor, Vector3.up, Vector3.right * 10);
            Bank(shoe); Assert.AreEqual(1, shoe.ThrowerSlot); Bank(shoe); Assert.AreEqual(-1, shoe.ThrowerSlot);
        }
        [Test] public void AgedLoadRecoveryWaitsForItsOwnEquipmentAndFlightSnapshotConsumesIt()
        {
            var ctx = Actor(out var kit);
            kit.AttackingSkill.ApplyNetworkSnapshot(20, 0, true);
            Assert.IsTrue(kit.RestoreJoiningCharges(ctx.Motor, 5, 0));
            kit.AttackingSkill.Tick(ctx, 1); Assert.IsTrue(kit.IsOverchargeThrowActive);
            var shoe = Shoe(ctx); Assert.IsTrue(kit.IsBankShotLoadedFor(shoe));
            Assert.AreEqual(4, kit.AttackingSkill.DurationRemaining, .001f);
            Assert.AreEqual(19, kit.AttackingSkill.CooldownRemaining, .001f);
            Assert.IsFalse(kit.RestoreJoiningCharges(ctx.Motor, 8, 0));
            shoe.ApplySnapshotState(SlipperState.InFlight, null, Vector3.up, Quaternion.identity, Vector3.right * 10, 0, SlipperAffinity.BankShot, 1);
            Assert.IsFalse(kit.IsOverchargeThrowActive);
            Assert.AreEqual(SlipperAffinity.BankShot, shoe.Affinity);
        }
        [TestCase(false)] [TestCase(true)] public void PoweredGuideMatchesActualWallFlight(bool overclock)
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.name = "FloorBank";
            floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(40, 1, 40);
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.name = "BankWall";
            wall.transform.position = new Vector3(0, 2, 2); wall.transform.localScale = new Vector3(8, 4, .4f);
            Physics.SyncTransforms();
            var ctx = Actor(out var kit); var loaded = Shoe(ctx);
            ctx.Motor.Teleport(new Vector3(-8, .1f, -8));
            if (overclock) kit.RestoreTimedKit(ctx.Motor, new TimedKitSnapshot(kit.AttackingSkill, 0, kit.Ultimate, 0, ultimatePermanent: true));
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            var guide = TrajectoryPreview.AttachTo(ctx.Motor); Track(guide.gameObject); guide.enabled = false;
            var shoe = Track(new GameObject("Actual flight")).AddComponent<Slipper>(); shoe.enabled = false;
            var affinity = overclock ? SlipperAffinity.OverclockBank : SlipperAffinity.BankShot;
            var origin = new Vector3(0, 1, -2); var velocity = new Vector3(0, 3, 7);
            Assert.IsTrue(guide.TryPredictLanding(origin, velocity, .9f, out var predicted));
            var step = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            using (NetCue.SuppressRelay())
            {
                shoe.HostThrow(null, origin, velocity, affinity, .9f);
                for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) step.Invoke(shoe, null);
            }
            Assert.AreEqual(SlipperState.Loose, shoe.State);
            var actual = shoe.transform.position; actual.y = predicted.y;
            Assert.Less(Vector3.Distance(actual, predicted), .12f);
        }
        [Test] public void BotPredictionAfterTwoPoweredBanksDoesNotRestoreFirstSpinBank()
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.name = "FloorBank";
            floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(40, 1, 40);
            var shoe = Track(new GameObject("Banked flight")).AddComponent<Slipper>(); shoe.enabled = false;
            shoe.HostThrow(null, Vector3.up, new Vector3(10, 4, 8), SlipperAffinity.OverclockBank, .9f);
            Bank(shoe); Bank(shoe);
            shoe.transform.position = new Vector3(0, 1, -2);
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.name = "Third wall";
            wall.transform.position = new Vector3(-1, 2, -1); wall.transform.localScale = new Vector3(.3f, 4, 8);
            Physics.SyncTransforms();
            var query = typeof(AIController).GetMethod("TryPredictedLanding", BindingFlags.Static | BindingFlags.NonPublic);
            var args = new object[] { shoe, Vector3.zero };
            Assert.IsTrue((bool)query.Invoke(null, args)); var predicted = (Vector3)args[1];
            var step = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            using (NetCue.SuppressRelay()) for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) step.Invoke(shoe, null);
            Assert.AreEqual(SlipperState.Loose, shoe.State);
            var actual = shoe.transform.position; actual.y = predicted.y;
            Assert.Less(Vector3.Distance(actual, predicted), .12f);
        }
    }
}
