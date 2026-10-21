using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class QuickCircuitTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        static Vector3 External(CharacterMotor motor) => (Vector3)typeof(CharacterMotor)
            .GetField("_externalVelocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(motor);
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [TearDown] public void Cleanup()
        { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        AbilityContext Actor(out ZackHeroKit kit)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); _built.Add(floor);
            floor.name = "FloorCircuit"; floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(40, 1, 40);
            var go = new GameObject("Circuit owner"); _built.Add(go);
            var body = go.AddComponent<CharacterMotor>(); body.PlayerSlot = 1; body.enabled = false;
            var system = go.AddComponent<HeroAbilitySystem>(); system.enabled = false; system.BindHero("zack");
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Round.Register(body);
            GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Round.ApplySnapshot(100, true, 0, true);
            kit = (ZackHeroKit)system.Kit; body.Teleport(new Vector3(0, .12f, -8)); body.Intent.Parked = false;
            return new AbilityContext(body, null, null);
        }
        sealed class RecastAbility : HeroAbility
        {
            public int Recasts;
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            public override bool CanReactivate => true;
            public RecastAbility() : base("recast", "Recast", "", 30, 2) { }
            public override void Reactivate(AbilityContext ctx) => Recasts++;
        }
        sealed class RecastKit : HeroKit
        { public RecastKit() : base("test", "Test") { Skill1 = new RecastAbility(); } }
        [Test] public void QuickCircuitUsesTheSpecifiedCooldownInsteadOfLegacySprint()
        {
            var kit = new ZackHeroKit();
            Assert.AreEqual("QUICK CIRCUIT", kit.Skill1.Name);
            Assert.AreEqual(30, kit.Skill1.Cooldown);
        }
        [Test] public void ZappedMustBlockAnAlreadyActiveFollowup()
        {
            var ctx = Actor(out _); var kit = new RecastKit();
            Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill1(ctx));
            ctx.Motor.ApplyZapped(); Assert.IsTrue(ctx.Motor.IsZapped);
            Assert.AreEqual(HeroKit.CastOutcome.CannotAct, kit.CastSkill1(ctx));
            Assert.AreEqual(0, ((RecastAbility)kit.Skill1).Recasts);
        }
        [TestCase(-1)] [TestCase(1)] public void ActualMotorCutsLaterallyWithoutSprintOrStunTrail(int side)
        {
            var ctx = Actor(out var kit); ctx.Motor.Intent.Move = new Vector2(side, 0);
            var cast = kit.Skill1.CaptureLocalCastContext(ctx); ctx.Motor.Intent.Move = Vector2.zero;
            var start = ctx.Motor.transform.position;
            using (NetCue.SuppressRelay())
            {
                Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill1(cast));
                Assert.IsTrue(kit.Skill1.IsWindingUp);
                kit.Skill1.Tick(ctx, .16f);
                var physics = typeof(CharacterMotor).GetMethod("FixedUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                for (int i = 0; i < 40; i++) { physics.Invoke(ctx.Motor, null); kit.Skill1.Tick(ctx, Time.fixedDeltaTime); }
            }
            var delta = ctx.Motor.transform.position - start;
            Assert.That(delta.x * side, Is.InRange(1.75f, 2.1f)); Assert.Less(Mathf.Abs(delta.z), .05f);
            Assert.AreEqual(1, kit.MovementSpeedScale);
            Assert.IsEmpty(Object.FindObjectsByType<HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None));
            Assert.IsFalse(kit.Skill1.IsActive); Assert.IsFalse(kit.Skill1.ReactivateReady);
            Assert.IsFalse(ctx.Motor.AbilitySystem.IsImmuneToStuns);
        }
        [Test] public void NoLateralInputFallsBackRightAndNormalCannotUseSecondCut()
        {
            var ctx = Actor(out var kit); var cast = kit.Skill1.CaptureLocalCastContext(ctx);
            Assert.Greater(cast.AimPoint.x, cast.Position.x);
            using (NetCue.SuppressRelay()) kit.CastSkill1(cast);
            kit.Skill1.Tick(ctx, .16f);
            Assert.AreEqual(HeroKit.CastOutcome.NotYet, kit.CastSkill1(cast));
            Assert.IsFalse(kit.Skill1.ReactivateReady);
        }
        void Overclock(ZackHeroKit kit, CharacterMotor body)
            => kit.RestoreTimedKit(body, new TimedKitSnapshot(kit.AttackingSkill, 0, kit.Ultimate, 0, ultimatePermanent: true));
        [Test] public void OverclockOffersExactlyOneOptionalFollowupWithoutResettingCooldown()
        {
            var ctx = Actor(out var kit); Overclock(kit, ctx.Motor);
            var cast = kit.Skill1.CaptureLocalCastContext(ctx);
            using (NetCue.SuppressRelay()) kit.CastSkill1(cast);
            kit.Skill1.Tick(ctx, .16f);
            Assert.IsFalse(kit.Skill1.ReactivateReady); Assert.Greater(kit.Skill1.ReactivateReadyIn, .5f);
            kit.Skill1.Tick(ctx, .6f); Assert.IsTrue(kit.Skill1.ReactivateReady);
            float cooldown = kit.Skill1.CooldownRemaining;
            using (NetCue.SuppressRelay()) Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill1(cast));
            Assert.AreEqual(cooldown, kit.Skill1.CooldownRemaining, .001f);
            kit.Skill1.Tick(ctx, .16f); kit.OnObjectiveAwarded(20);
            Assert.AreEqual(0, kit.Skill1.CooldownRemaining);
            Assert.IsFalse(kit.Skill1.ReactivateReady);
            Assert.AreEqual(HeroKit.CastOutcome.NotYet, kit.CastSkill1(cast), "A cooldown discount must not create a third cut during recovery.");
        }
        [Test] public void DeclinedFollowupExpiresAndCannotSurviveRoundReset()
        {
            var ctx = Actor(out var kit); Overclock(kit, ctx.Motor);
            using (NetCue.SuppressRelay()) kit.CastSkill1(kit.Skill1.CaptureLocalCastContext(ctx));
            kit.Skill1.Tick(ctx, .16f); kit.Skill1.Tick(ctx, 1.01f);
            Assert.IsFalse(kit.Skill1.IsActive); Assert.IsFalse(kit.Skill1.ReactivateReady);
            Assert.AreEqual(HeroKit.CastOutcome.Cooling, kit.CastSkill1(ctx));
            kit.ResetForRound(ctx); Assert.IsTrue(kit.IsOverclocked);
            Assert.IsFalse(kit.CaptureMovementState().QuickFollowup); Assert.IsFalse(kit.Skill1.ReactivateReady);
        }
        [Test] public void RecoveryAgesOptionalWindowWithoutImpulseCooldownOrRefresh()
        {
            var ctx = Actor(out var kit); var before = External(ctx.Motor);
            kit.Skill1.ApplyNetworkSnapshot(20, 0, true);
            var state = new HeroMovementState { Remaining = .8f, Wake = System.Array.Empty<Vector3>(), QuickFollowup = true };
            var invalid = state; invalid.QuickSecondCut = true;
            Assert.IsFalse(kit.RestoreJoiningMovement(ctx.Motor, invalid, 0));
            Assert.IsTrue(kit.RestoreJoiningMovement(ctx.Motor, state, .1f));
            Assert.AreEqual(.7f, kit.Skill1.DurationRemaining, .001f);
            Assert.AreEqual(20, kit.Skill1.CooldownRemaining); Assert.AreEqual(before, External(ctx.Motor));
            Assert.IsTrue(kit.RestoreJoiningMovement(ctx.Motor, state, 0));
            Assert.AreEqual(.7f, kit.Skill1.DurationRemaining, .001f);
            kit.Skill1.Tick(ctx, 1); Assert.IsFalse(kit.RestoreJoiningMovement(ctx.Motor, state, 0));
            kit.ResetForRound(ctx); Assert.IsTrue(kit.RestoreJoiningMovement(ctx.Motor, HeroMovementState.Empty, 0));
            Assert.IsFalse(kit.RestoreJoiningMovement(ctx.Motor, state, 0));
        }
        [Test] public void PendingSecondCutRecoveryNeverCreatesAnotherFollowup()
        {
            var ctx = Actor(out var kit); Overclock(kit, ctx.Motor);
            var state = new HeroMovementState { Wake = System.Array.Empty<Vector3>(), QuickSecondCut = true };
            Assert.IsTrue(kit.RestoreJoiningMovement(ctx.Motor, state, 0));
            var cast = kit.Skill1.CaptureLocalCastContext(ctx);
            Assert.IsTrue(kit.Skill1.RestoreJoiningPreparation(cast, .1f, 0));
            using (NetCue.SuppressRelay()) kit.Skill1.Tick(ctx, .11f);
            Assert.IsFalse(kit.Skill1.ReactivateReady);
            Assert.IsFalse(kit.CaptureMovementState().QuickFollowup);
        }
        [Test] public void StunAndInactiveRoundAlsoBlockActiveFollowups()
        {
            var ctx = Actor(out _); var kit = new RecastKit(); kit.CastSkill1(ctx);
            ctx.Motor.RoundActive = false;
            Assert.AreEqual(HeroKit.CastOutcome.CannotAct, kit.CastSkill1(ctx));
            ctx.Motor.RoundActive = true; ctx.Motor.ApplyStagger(1);
            Assert.AreEqual(HeroKit.CastOutcome.CannotAct, kit.CastSkill1(ctx));
            Assert.AreEqual(0, ((RecastAbility)kit.Skill1).Recasts);
        }
    }
}
