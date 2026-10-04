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

        public TimedKitSnapshot CaptureTimedKit()
            => new TimedKitSnapshot(Skill1, IsDemonicCarapaceActive ? Skill1.DurationRemaining : 0);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
            => RestoreJoiningCarapace(motor, state.PersonalRemaining);

        public bool RestoreJoiningCarapace(CharacterMotor motor, float remaining)
        {
            if (motor == null || _joiningCarapaceSettled || Skill1.IsActive) return false;
            _joiningCarapaceSettled = true;
            var ctx = new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>());
            ((Shield)Skill1).RestoreWard(ctx, remaining);
            return true;
        }

        public DanteHeroKit() : base("dante", "BASILIO")
        {
            Skill1 = new Shield(this);
            AttackingSkill = new Boulder();
            DefendingSkill = new Barrier();
            Ultimate = new Earthquake();
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
                if (!ctx.Motor.IsTagged) ctx.Motor.ClearStun();
                ctx.Motor.CleanseStatuses();
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
                       "Imbue your held slipper. Your next throw inflicts Concussed on a player hit: 75% slower for 2.5 s.",
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
                shoe.Affinity = SlipperAffinity.Concussed;
                Net.MatchRpc.Instance?.BroadcastSlipperState(shoe);
            }
        }

        // ================================================================== BARRIER (defending)

        private sealed class Barrier : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private GameObject _field;
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
                if (_field != null) UnityEngine.Object.Destroy(_field);
                _field = DanteBarrierVisual.Build(ctx.Motor.transform);
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

            protected override void OnEnd(AbilityContext ctx) { if (_field != null) UnityEngine.Object.Destroy(_field); _field = null; }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        // ================================================================== EARTHQUAKE (ultimate)

        private sealed class Earthquake : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            public Earthquake()
                : base("dante_ultimate", "CONTINENTAL DRIFT",
                       "A wide earthquake cascades forward in successive blasts. Players caught in each blast are Concussed for 2.5 s.",
                       0.0f, 0.0f, AbilityGlyph.DanteFissure,
                       summary: "Successive forward blasts inflict Concussed.",
                       castAction: "hero-dante-fissure", viewmodelAction: "fissure-slam",
                       castCue: "sfx_cast_dante_earthquake")
            {
                TelegraphStyle = GroundReticle.Style.Fissure;
                Windup = UltimateWindup;
                SupportsPendingSnapshot = true;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_dante_ult", ctx.Position);
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Stretch(0.4f);
                DanteDriftWave.Spawn(ctx.Position, ctx.Forward, ctx.Motor.PlayerSlot);
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
            b._velocity = Slipper.SolveArc(origin, target, GeoRules.BoulderSpeed);
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
