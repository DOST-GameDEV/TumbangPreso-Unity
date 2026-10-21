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
    public sealed class EmpoweredThrowTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        INetProvider _old;
        [UnitySetUp] public IEnumerator Before()
        { _old = NetAuthority.Provider; NetAuthority.Provider = null; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After()
        { yield return PlayModeWorld.Reset(); NetAuthority.Provider = _old; }
        [TearDown] public void Cleanup()
        { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }
        CharacterMotor Body(int seat, Vector3 at)
        {
            var body = Track(new GameObject("Pressure seat " + seat)).AddComponent<CharacterMotor>();
            body.PlayerSlot = seat; body.enabled = false; body.Teleport(at);
            GameServices.Round.Register(body); return body;
        }
        AbilityContext Actor(out SeanHeroKit kit)
        {
            GameServices.Ensure(); GameServices.Round.Clear();
            var body = Body(1, new Vector3(-4, 0, -4));
            var system = body.gameObject.AddComponent<HeroAbilitySystem>(); system.enabled = false; system.BindHero("sean");
            var carrier = body.gameObject.AddComponent<Carrier>(); carrier.enabled = false;
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            kit = (SeanHeroKit)system.Kit;
            return new AbilityContext(body, carrier, null);
        }
        Slipper Shoe(AbilityContext ctx)
        {
            var shoe = Track(new GameObject("Pressure slipper")).AddComponent<Slipper>(); shoe.enabled = false;
            shoe.OwnerSlot = 1; shoe.SeatOfOrigin = 1; shoe.HostForceEquip(ctx.Motor); return shoe;
        }
        void Impact(Slipper shoe)
        { using (NetCue.SuppressRelay()) typeof(Slipper).GetMethod("TriggerAffinityImpact", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shoe, null); }
        [Test] public void LoadHasFreeHeldRefusalAndExplicitCooldown()
        {
            var ctx = Actor(out var kit);
            Assert.IsFalse(kit.AttackingSkill.CanActivate(ctx)); Assert.AreEqual(0, kit.AttackingSkill.CooldownRemaining);
            Assert.AreEqual(35, kit.AttackingSkill.Cooldown); Assert.AreEqual(8, kit.AttackingSkill.Duration);
            Assert.IsFalse(kit.AttackingSkill.UsesCharges);
            var shoe = Shoe(ctx); Assert.IsTrue(kit.AttackingSkill.CanActivate(ctx));
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            Assert.IsTrue(kit.IsEmpoweredThrowLoadedFor(shoe)); Assert.AreEqual(SlipperAffinity.FireExplosive, shoe.Affinity);
            Assert.AreEqual(35, kit.AttackingSkill.CooldownRemaining);
        }
        [Test] public void ActualThrowConsumesExactLoadWithoutSpeedMultiplier()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            var origin = new Vector3(0, 1, -8); var target = Vector3.zero;
            var expected = shoe.LaunchVelocityTo(origin, target, 1);
            using (NetCue.SuppressRelay()) ctx.Carrier.HostThrowAt(origin, target, 1);
            Assert.AreEqual(SlipperState.InFlight, shoe.State); Assert.AreEqual(SlipperAffinity.FireExplosive, shoe.Affinity);
            Assert.Less(Vector3.Distance(expected, shoe.Velocity), .001f); Assert.IsFalse(kit.IsIgnitionCannonActive);
        }
        [Test] public void DropReplacementAndExpiryDoNotRetainAnEmber()
        {
            var ctx = Actor(out var kit); var first = Shoe(ctx);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            first.HostDisarm(); Assert.AreEqual(SlipperAffinity.Normal, first.Affinity);
            var other = Shoe(ctx); Assert.IsFalse(kit.IsEmpoweredThrowLoadedFor(other));
            kit.AttackingSkill.Tick(ctx, .02f); Assert.IsFalse(kit.IsIgnitionCannonActive);
            kit.AttackingSkill.ApplyNetworkSnapshot(0, 0, true);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            kit.AttackingSkill.Tick(ctx, 8); Assert.IsFalse(kit.IsIgnitionCannonActive);
            Assert.AreEqual(SlipperAffinity.Normal, other.Affinity);
        }
        [Test] public void BurstIsCompactOnceOnlyAndDoesNotStaggerOrLift()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            var near = Body(2, new Vector3(.8f, 0, 0)); var far = Body(3, new Vector3(1.4f, 0, 0));
            shoe.HostThrow(ctx.Motor, Vector3.up * .2f, Vector3.forward, affinity: SlipperAffinity.FireExplosive);
            Physics.SyncTransforms(); Impact(shoe);
            Assert.Greater(near.PresentationTravelVelocity.x, 0); Assert.AreEqual(0, near.PresentationTravelVelocity.y, .001f);
            Assert.LessOrEqual(near.PresentationTravelVelocity.magnitude, Mathf.Sqrt(2 * Balance.Friction) + .001f);
            Assert.IsFalse(near.IsStunned); Assert.IsFalse(near.IsTripped); Assert.IsFalse(near.IsZapped);
            Assert.AreEqual(Vector3.zero, far.PresentationTravelVelocity); Assert.AreEqual(Vector3.zero, ctx.Motor.PresentationTravelVelocity);
            var velocity = near.PresentationTravelVelocity; Impact(shoe);
            Assert.AreEqual(velocity, near.PresentationTravelVelocity); Assert.AreEqual(SlipperAffinity.Normal, shoe.Affinity);
        }
        [Test] public void SolidCoverStopsThePressure()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx); var near = Body(2, new Vector3(.8f, 0, 0));
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.transform.position = new Vector3(.4f, .5f, 0);
            wall.transform.localScale = new Vector3(.1f, 1, 2);
            shoe.HostThrow(ctx.Motor, Vector3.up * .2f, Vector3.forward, affinity: SlipperAffinity.FireExplosive);
            Physics.SyncTransforms(); Impact(shoe); Assert.AreEqual(Vector3.zero, near.PresentationTravelVelocity);
        }
        [Test] public void RecoveryAcceptsOnlyTheActualMarkedHeldSnapshot()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            Assert.IsTrue(kit.RestoreJoiningIgnition(ctx.Motor, 4));
            Assert.IsFalse(kit.IsEmpoweredThrowLoadedFor(shoe));
            shoe.ApplySnapshotState(SlipperState.Held, ctx.Motor, ctx.Position, Quaternion.identity,
                Vector3.zero, 0, SlipperAffinity.FireExplosive, -1);
            Assert.AreEqual(SlipperAffinity.FireExplosive, shoe.Affinity);
            Assert.IsTrue(kit.IsEmpoweredThrowLoadedFor(shoe));
            kit.AttackingSkill.Tick(ctx, .02f); Assert.AreEqual(3.98f, kit.AttackingSkill.DurationRemaining, .001f);
            Assert.IsFalse(kit.RestoreJoiningIgnition(ctx.Motor, 8));
        }
        [Test] public void RoundResetClearsTheHeldMarkerAndRecoveryWatermark()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            using (NetCue.SuppressRelay()) kit.AttackingSkill.Activate(ctx);
            kit.AttackingSkill.Reset(); Assert.IsFalse(kit.IsIgnitionCannonActive);
            Assert.AreEqual(SlipperAffinity.Normal, shoe.Affinity); Assert.IsFalse(kit.IsEmpoweredThrowLoadedFor(shoe));
            Assert.IsTrue(kit.RestoreJoiningIgnition(ctx.Motor, 3));
            Assert.IsFalse(kit.IsEmpoweredThrowLoadedFor(shoe));
        }
        private sealed class Replica : INetProvider
        { public bool IsHost => false; public bool IsNetworked => true; public int LocalSlot => 0; public int LocalPeerId => 1; public bool IsSeatlessReferee => false; }
        [Test] public void ReplicaBurstCannotApplyMovementOrCanOutcome()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx); var near = Body(2, new Vector3(.8f, 0, 0));
            shoe.HostThrow(ctx.Motor, Vector3.up * .2f, Vector3.forward, affinity: SlipperAffinity.FireExplosive);
            NetAuthority.Provider = new Replica(); Physics.SyncTransforms(); Impact(shoe);
            Assert.AreEqual(Vector3.zero, near.PresentationTravelVelocity); Assert.AreEqual(0, GameServices.Match.ScoreFor(1));
        }
        [Test] public void CanProtectionAndSingleKnockdownAuthorityArePreserved()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            var can = Track(new GameObject("Pressure can")).AddComponent<Lata>(); can.enabled = false; GameServices.Round.Lata = can;
            can.ApplySnapshotState(Vector3.zero, Quaternion.identity, false, 0);
            can.ApplySnapshotState(Vector3.zero, Quaternion.identity, true, 0);
            Assert.IsTrue(can.IsProtected);
            shoe.HostThrow(ctx.Motor, new Vector3(.8f, .2f, 0), Vector3.forward, affinity: SlipperAffinity.FireExplosive);
            Physics.SyncTransforms(); Impact(shoe); Assert.IsTrue(can.IsUpright); Assert.AreEqual(0, GameServices.Match.ScoreFor(1));
            typeof(Lata).GetField("_restoreProtectionLeft", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(can, 0f);
            shoe.Affinity = SlipperAffinity.FireExplosive; Impact(shoe);
            Assert.IsFalse(can.IsUpright); Assert.AreEqual(1, can.HostKnockdownSerial);
            int score = GameServices.Match.ScoreFor(1); Assert.Greater(score, 0);
            Impact(shoe); Assert.AreEqual(score, GameServices.Match.ScoreFor(1)); Assert.AreEqual(1, can.HostKnockdownSerial);
        }
        [Test] public void FirstActualWallContactConsumesThePayload()
        {
            var ctx = Actor(out var kit); var shoe = Shoe(ctx);
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.name = "EmpoweredWall";
            wall.transform.position = Vector3.up; wall.transform.localScale = new Vector3(4, 2, .2f);
            var start = new Vector3(0, .5f, -1);
            shoe.HostThrow(ctx.Motor, start, Vector3.forward * 10, affinity: SlipperAffinity.FireExplosive);
            shoe.transform.position = new Vector3(0, .5f, 1); Physics.SyncTransforms();
            using (NetCue.SuppressRelay()) typeof(Slipper).GetMethod("BounceOffObstacles", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(shoe, new object[] { start, .2f });
            Assert.Less(shoe.Velocity.z, 0); Assert.AreEqual(SlipperAffinity.Normal, shoe.Affinity);
        }
    }
}
