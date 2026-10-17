using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Current Wiki mechanics: Earthbound, Unstoppable, held-slipper Boulder,
    /// following Bastion and the forward Continental Drift cascade. Stable ability
    /// IDs remain the network boundary. Authored presentation refinement is separate.
    /// </summary>
    public sealed class DanteHeroKit : HeroKit, ITimedKitReplication
    {
        public const float StompContactSeconds = .30f;
        public override float IncomingKnockbackDistanceScale => GeoRules.EarthboundDistanceScale;

        /// <summary>True while SHIELD holds: every status but Tagged is refused.</summary>
        public bool IsDemonicCarapaceActive => Skill1 != null && Skill1.IsActive;
        private bool _joiningCarapaceSettled;

        // ================================================================== MOUNTAIN FORM (after the ultimate)
        //
        // The owner, 2026-10-17 and 18: after CONTINENTAL DRIFT he keeps a suit of rock armour for 30 s (*"AFTER HIS ULT he retains
        // the rock armor for 30 secs"*), with buffs: a stronger throw, throws that stun a little and knock back on a player hit,
        // and immunity to debuffs. From 20 s pieces fall off until it is all off at 30 s (`DanteMountainArmour.Shed`).
        //
        // ONE CLOCK ON EVERY PEER: the ultimate's activation reaches every peer (SharedUltimate) and `Tick` runs this kit on every
        // peer, so `MountainLeft` counts down everywhere with nothing on the wire but the activation. The host decides what it
        // changes (the throw in `Carrier.HostThrowAt`, the hit in `Slipper.HostBodyAffinity`, the immunity through
        // `HeroAbilitySystem.IsImmuneToStatuses`); every peer draws it (the armour, his own clip set, the slipper's stone). A
        // late joiner gets the time left in the timed-kit snapshot's passive fields (Dante has no passive).

        /// <summary>Seconds of MOUNTAIN FORM left (0 when he is not in it).</summary>
        public float MountainLeft { get; private set; }
        public bool IsMountainForm => MountainLeft > 0f;
        private DanteMountainArmour _armour;
        // The armour's own clock (`DanteMountainArmour.StepTo`), on past the form's end while it sheds, and its `EndAt`. Kept by the
        // kit rather than read off the suit, so a suit built late (a joiner whose model arrived after the snapshot) joins the clock
        // where it already is.
        private float _armourClock, _armourEnd;
        private float _dressRetry;

        /// <summary>The form begins: from the end of his cutscene (the suit already formed on him in it), or from nothing, the
        /// suit then closing over him live (no shared cutscene, a test, a practice cast).</summary>
        internal void BeginMountain(CharacterMotor motor, bool afterCutscene)
        {
            if (motor == null) return;
            MountainLeft = GeoRules.MountainSeconds;
            // Status immunity starts with a cleanse, as UNSTOPPABLE's does: nothing he carried into the form stays on him.
            if (!motor.IsTagged) motor.ClearStun();
            motor.CleanseStatuses();
            StartArmourClock(afterCutscene ? DanteMountainArmour.CutEndAt : 0f);
            DressMountain(motor);
        }

        private void StartArmourClock(float start) { _armourClock = start; _armourEnd = start + GeoRules.MountainSeconds; }

        private void DressMountain(CharacterMotor motor)
        {
            if (_armour != null) UnityEngine.Object.Destroy(_armour.gameObject);
            _armour = DanteMountainArmour.Attach(motor);
            if (_armour != null) { _armour.EndAt = _armourEnd; _armour.StepTo(_armourClock); }
            SetMountainClips(motor, true);
        }

        private static void SetMountainClips(CharacterMotor motor, bool on)
        {
            var animator = motor != null ? motor.GetComponentInChildren<CharacterAnimator>() : null;
            if (animator != null) animator.ClipVariant = on ? CharacterAnimator.MountainVariant : null;
        }

        public override void Tick(AbilityContext ctx, float dt)
        {
            base.Tick(ctx, dt);
            // His model was rebuilt under the suit: drop it, and the form (below) dresses the new body on its next try.
            if (_armour != null && _armour.Orphaned) { UnityEngine.Object.Destroy(_armour.gameObject); _armour = null; _dressRetry = 0f; }
            if (MountainLeft > 0f)
            {
                MountainLeft = Mathf.Max(0f, MountainLeft - dt);
                // Immune, as UNSTOPPABLE holds it: a stun that reached him anyway (a tag excepted) is lifted at once.
                if (ctx?.Motor != null && !ctx.Motor.IsTagged) ctx.Motor.ClearStun();
                if (MountainLeft <= 0f) SetMountainClips(ctx?.Motor, false);
                // ⚠️ NO SUIT YET, AND THE FORM IS ON: his model was not built when it began (a joiner's snapshot can land before
                // the roster dresses the body). Try again every half second rather than every frame; `Attach` is cheap to refuse.
                else if (_armour == null && (_dressRetry -= dt) <= 0f) { _dressRetry = .5f; DressMountain(ctx?.Motor); }
                _armourClock += dt;
            }
            else if (_armour != null) _armourClock += dt;
            if (_armour == null) return;
            _armour.StepTo(_armourClock);
            if (_armourClock >= _armour.GoneAt) { UnityEngine.Object.Destroy(_armour.gameObject); _armour = null; }
        }

        /// <summary>A round boundary or a new match: no armour, no buffs and no armoured walk carried over.</summary>
        public override void ResetForRound(AbilityContext ctx)
        {
            EndMountain(ctx?.Motor);
            base.ResetForRound(ctx);
        }

        private void EndMountain(CharacterMotor motor)
        {
            MountainLeft = 0f;
            if (_armour != null) UnityEngine.Object.Destroy(_armour.gameObject);
            _armour = null;
            SetMountainClips(motor, false);
        }

        public override bool UltimateAftermath(out float left, out float total)
        { left = MountainLeft; total = GeoRules.MountainSeconds; return MountainLeft > 0f; }

        public TimedKitSnapshot CaptureTimedKit()
            => new TimedKitSnapshot(Skill1, IsDemonicCarapaceActive ? Skill1.DurationRemaining : 0,
                passiveRemaining: MountainLeft, passiveCapacity: GeoRules.MountainSeconds);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
        {
            bool changed = RestoreJoiningCarapace(motor, state.PersonalRemaining);
            float left = Mathf.Clamp(state.PassiveRemaining, 0f, GeoRules.MountainSeconds);
            if (motor == null || Mathf.Abs(left - MountainLeft) < .05f) return changed;
            if (left <= 0f) { EndMountain(motor); return true; }
            // A late joiner sees him in the suit as it is now: formed, or part shed (no flight of stones onto him), on a clock
            // that reads as if the cutscene had ended `MountainSeconds - left` ago.
            // ⚠️ A SUIT ALREADY ON HIM IS MOVED, NOT REBUILT: a correction of a few frames must not pop every stone off and on.
            bool worn = MountainLeft > 0f && _armour != null;
            if (MountainLeft <= 0f) StartArmourClock(DanteMountainArmour.CutEndAt);
            MountainLeft = left;
            _armourClock = _armourEnd - left;
            if (worn) _armour.StepTo(_armourClock);
            else DressMountain(motor);
            return true;
        }

        public bool RestoreJoiningCarapace(CharacterMotor motor, float remaining)
        {
            if (motor == null || _joiningCarapaceSettled || Skill1.IsActive) return false;
            _joiningCarapaceSettled = true;
            var ctx = new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>());
            ((Shield)Skill1).RestoreWard(ctx, remaining);
            return true;
        }

        public DanteHeroKit() : base("dante", "DANTE")
        {
            Skill1 = new Shield(this);
            AttackingSkill = new Boulder();
            DefendingSkill = new Barrier();
            Ultimate = new Earthquake(this);
        }

        public override float UltimateCost => GeoRules.EarthquakeCost;

        // ================================================================== SHIELD (signature)

        private sealed class Shield : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly DanteHeroKit _kit;
            private DanteCarapaceVisual _ward;

            public Shield(DanteHeroKit kit)
                : base("dante_skill1", "UNSTOPPABLE",
                       "Remove removable negative effects and gain Status Immunity for 15 s. Tagged cannot be removed.",
                       GeoRules.ShieldCooldown, GeoRules.ShieldSeconds, AbilityGlyph.DanteShield,
                       summary: "Cleanse removable effects and gain 15 s of Status Immunity.",
                       castAction: "hero-dante-roar", viewmodelAction: "carapace-guard",
                       castCue: "sfx_cast_dante_shield") { _kit = kit; }

            public override bool AllowsImpairedCast(AbilityContext ctx)
                => ctx?.Motor != null && ctx.Round?.RoundActive == true
                    && !PresentationClock.BlocksInput && !ctx.Motor.IsTagged && !ctx.Motor.IsTripped;

            public void RestoreWard(AbilityContext ctx, float remaining)
            {
                if (remaining <= 0) return;
                RestoreLiveClock(remaining);
                _ward = DanteCarapaceVisual.Attach(ctx.Motor, false, Duration);
                if (_ward != null) _ward.StepTo(Duration - DurationRemaining);
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit._joiningCarapaceSettled = true;
                NetCue.Play("guard_block", ctx.Position);
                // What he carried, read before the cleanse: each one shatters off him in its own colour (`DanteStatusShatter`).
                var had = new List<StatusKind>(4);
                foreach (StatusKind kind in Enum.GetValues(typeof(StatusKind)))
                    if (kind != StatusKind.Tagged && ctx.Motor.StatusLeft(kind) > 0f) had.Add(kind);
                if (!ctx.Motor.IsTagged) ctx.Motor.ClearStun();
                ctx.Motor.CleanseStatuses();
                Visual.DanteStatusShatter.Release(ctx.Motor.transform, had);
                if (_ward != null) UnityEngine.Object.Destroy(_ward.gameObject);
                _ward = DanteCarapaceVisual.Attach(ctx.Motor, false, Duration);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (!ctx.Motor.IsTagged) ctx.Motor.ClearStun();
                if (_ward == null) _ward = DanteCarapaceVisual.Attach(ctx.Motor, false, Duration);
                if (_ward != null) _ward.StepTo(_ward.LifeSeconds - DurationRemaining);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                if (_ward != null) UnityEngine.Object.Destroy(_ward.gameObject);
                _ward = null;
            }
        }

        // ================================================================== BOULDER (attacking)

        private sealed class Boulder : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;

            public Boulder()
                : base("dante_skill2", "BOULDER",
                       "Imbue your held slipper for 15 s. Your next throw inflicts Concussed on a player hit: 75% slower for 5 s.",
                       GeoRules.BoulderCooldown, 0.0f, AbilityGlyph.DanteBoulder,
                       summary: "Imbue your held slipper with Concussed.",
                       castAction: "hero-dante-boulder", viewmodelAction: "boulder-load",
                       castCue: "sfx_cast_dante_boulder") { }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && !ctx.Motor.IsDefender
                    && ctx.Carrier?.Held != null && ctx.Carrier.Held.State == SlipperState.Held
                    && ctx.Carrier.Held.Holder == ctx.Motor;

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_dante_grunt", ctx.Position);
                if (!NetAuthority.ShouldResolve()) return;
                var shoe = ctx.Carrier?.Held;
                if (shoe == null || shoe.State != SlipperState.Held || shoe.Holder != ctx.Motor) return;
                shoe.HostLoadTimedAffinity(SlipperAffinity.Concussed, GeoRules.BoulderLoadSeconds);
                Net.MatchRpc.Instance?.BroadcastSlipperState(shoe);
            }
        }

        // ================================================================== BARRIER (defending)

        private sealed class Barrier : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private Visual.DanteBastionWall _field;
            private CharacterMotor _owner;

            public Barrier()
                : base("dante_skill2d", "BASTION",
                       "Defending. A wide stone force field in front of you for 7.5 s. It follows you, and every slipper that hits it flies back.",
                       GeoRules.BarrierCooldown, GeoRules.BarrierSeconds, AbilityGlyph.DanteBarrier,
                       summary: "A force field in front of you reflects slippers.",
                       castAction: "hero-dante-bastion", viewmodelAction: "bastion-brace",
                       castCue: "sfx_cast_dante_barrier") { }

            protected override void OnActivate(AbilityContext ctx)
            {
                _owner = ctx.Motor;
                if (_field != null) UnityEngine.Object.Destroy(_field.gameObject);
                // Seven waist-high slabs and a see-through heat field, exactly the reflect area (`DanteBastionWall`).
                _field = Visual.DanteBastionWall.Build(ctx.Motor.transform, Duration);
                NetCue.Play("guard_block", ctx.Position);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_owner == null || !NetAuthority.ShouldResolve()) return;
                Transform t = _owner.transform;
                Vector3 fwd = t.forward; fwd.y = 0.0f; fwd = fwd.sqrMagnitude > 0.001f ? fwd.normalized : Vector3.forward;
                foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                {
                    if (s == null || s.State != SlipperState.InFlight || s.ThrowerSlot == _owner.PlayerSlot) continue;
                    Vector3 local = t.InverseTransformPoint(s.transform.position);
                    if (local.z < GeoRules.BarrierForward - 0.4f || local.z > GeoRules.BarrierForward + 0.4f) continue;
                    if (Mathf.Abs(local.x) > GeoRules.BarrierWidth * 0.5f || local.y > 2.6f) continue;
                    Vector3 v = s.Velocity; v.y = 0.0f;
                    if (Vector3.Dot(v, fwd) >= 0.0f) continue;          // only slippers coming AT him
                    s.Deflect(Vector3.Reflect(v, fwd), 1.0f);
                    NetCue.Play("sfx_dante_barrier_reflect", s.transform.position);
                }
            }

            // The wall stays where it stood and sinks into the court, outside first.
            protected override void OnEnd(AbilityContext ctx) { if (_field != null) _field.Release(); _field = null; }
            // A round reset or an interruption removes it at once: no wall sinks into the next round.
            protected override void OnCancelled(AbilityContext ctx) { if (_field != null) UnityEngine.Object.Destroy(_field.gameObject); _field = null; }
        }

        // ================================================================== EARTHQUAKE (ultimate)

        private sealed class Earthquake : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            private readonly DanteHeroKit _kit;
            public Earthquake(DanteHeroKit kit)
                : base("dante_ultimate", "CONTINENTAL DRIFT",
                       "He slams the court: five bands of earth erupt down the lane, knocking away everyone they reach. They land Concussed for 5 s.",
                       0.0f, 0.0f, AbilityGlyph.DanteFissure,
                       summary: "Knocks away everyone ahead, then 30 s of MOUNTAIN FORM.",
                       castAction: "hero-dante-fissure", viewmodelAction: "fissure-slam",
                       castCue: "sfx_cast_dante_earthquake")
            {
                _kit = kit;
                TelegraphStyle = GroundReticle.Style.Fissure;
                Windup = UltimateWindup;
                SupportsPendingSnapshot = true;
            }

            // The cutscene is his charge-up and ends on the slam; the cascade leaves his fists on the cut (owner, 2026-10-17:
            // "after nna malaunch ung ult, end nna dapat cutscene"), so no wind-up after it.
            protected override bool IntroductionIsTheWindup => true;

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_dante_ult", ctx.Position);
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Stretch(0.4f);
                // After his cutscene the cascade is already under way: its last frame drew the first band rising.
                DanteDriftWave.Spawn(ctx.Position, ctx.Forward, ctx.Motor.PlayerSlot, HadSharedIntroduction ? GeoRules.DriftCutsceneTail : 0f);
                // And the mountain stays on him: MOUNTAIN FORM for `GeoRules.MountainSeconds` (the kit's note).
                _kit.BeginMountain(ctx.Motor, HadSharedIntroduction);
            }
        }
    }

    /// <summary>
    /// BOULDER's rock (ABILITY-2). Spawned on every peer from the accepted cast; contact is resolved on
    /// the host by distance (never a trigger, `CLAUDE.md` § 4): a body within `GeoRules.BoulderHitRadius`
    /// in flight or while it rolls is Concussed and shoved once. It never knocks the can (plan § 7).
    /// </summary>
    public sealed class DanteBoulder : MonoBehaviour
    {
        private Vector3 _velocity;
        private int _owner;
        private bool _landed;
        private float _rolled, _age;
        private readonly HashSet<int> _hit = new HashSet<int>();

        public static DanteBoulder Spawn(Vector3 origin, Vector3 target, int ownerSlot)
        {
            var go = new GameObject("DanteBoulder");
            go.transform.position = origin;
            var b = go.AddComponent<DanteBoulder>();
            b._owner = ownerSlot;
            b._velocity = Slipper.SolveArc(origin, target, GeoRules.BoulderSpeed) * GeoRules.BoulderSpeed;
            DanteBoulderVisual.Build(go.transform);
            return b;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _age += dt;
            if (!_landed)
            {
                _velocity.y -= Balance.Gravity * dt;
                transform.position += _velocity * dt;
                float ground = Slipper.GroundY(transform.position);
                if (transform.position.y <= ground + 0.3f && _velocity.y < 0f)
                {
                    _landed = true;
                    transform.position = new Vector3(transform.position.x, ground + 0.3f, transform.position.z);
                    _velocity.y = 0.0f;
                    NetCue.Play("sfx_dante_boulder_hit", transform.position);
                }
            }
            else
            {
                Vector3 flat = new Vector3(_velocity.x, 0f, _velocity.z);
                float step = flat.magnitude * dt;
                if (_rolled < GeoRules.BoulderRollDistance && step > 0.0001f)
                {
                    transform.position += flat.normalized * Mathf.Min(step, GeoRules.BoulderRollDistance - _rolled);
                    _rolled += step;
                    transform.Rotate(Vector3.Cross(Vector3.up, flat.normalized), step / 0.3f * Mathf.Rad2Deg, Space.World);
                }
                else if (_age > 0.5f) { Destroy(gameObject, 0.6f); enabled = false; }
            }

            if (!NetAuthority.ShouldResolve()) return;
            var round = GameServices.Round;
            if (round == null) return;
            foreach (var p in round.Players)
            {
                if (p == null || p.PlayerSlot == _owner || _hit.Contains(p.PlayerSlot)) continue;
                Vector3 d = p.transform.position + Vector3.up * 0.8f - transform.position;
                if (d.magnitude > GeoRules.BoulderHitRadius + 0.4f) continue;
                _hit.Add(p.PlayerSlot);
                NetCue.Play("sfx_dante_boulder_hit", p.transform.position);
                p.ApplyConcussed();
                Vector3 push = new Vector3(_velocity.x, 0f, _velocity.z);
                push = (push.sqrMagnitude > 0.01f ? push.normalized : Vector3.forward) * GeoRules.BoulderShoveSpeed;
                p.ApplyResolvedImpact(push + Vector3.up * 1.5f);
                Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.HeroHit, _owner, p.PlayerSlot, p.transform.position, 2.0f);
            }
            if (transform.position.y < -20f) Destroy(gameObject);
        }
    }
}
