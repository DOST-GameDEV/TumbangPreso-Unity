using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ DANTE, GEO, IN THE NEW SHAPE (ABILITY-2, owner 2026-09-26: *"geo to dante"*, with the table
    /// below). `docs/reports/ability-rework-2026-09-26/plan.md` § 3.3; numbers in `Core.GeoRules`.
    ///
    /// | Slot | Name | Owner's table |
    /// |---|---|---|
    /// | Signature | SHIELD | Status immunity for 20 seconds |
    /// | Attacking | BOULDER | Throw rock -> Concussed |
    /// | Defending | BARRIER | a wide force field that reflects slippers in front of you; lasts 7.5 s and follows you around; 25 s |
    /// | Ultimate | EARTHQUAKE | Everyone concussed |
    ///
    /// ⚠️ THE SHIELD IS THE OLD CARAPACE'S BODY (its ward visual, its restore path for a rejoiner),
    /// lengthened to the owner's 20 s and widened from stuns to every status but Tagged, which nothing
    /// may be immune to (the status table). `IsDemonicCarapaceActive` keeps its name because the wire's
    /// restore path and the ability system read it.
    /// </summary>
    public sealed class DanteHeroKit : HeroKit
    {
        public const float StompContactSeconds = .30f;

        /// <summary>True while SHIELD holds: every status but Tagged is refused.</summary>
        public bool IsDemonicCarapaceActive => Skill1 != null && Skill1.IsActive;
        private bool _joiningCarapaceSettled;

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
            Ultimate = new Earthquake();
        }

        public override float UltimateCost => GeoRules.EarthquakeCost;

        // ================================================================== SHIELD (signature)

        private sealed class Shield : HeroAbility
        {
            private readonly DanteHeroKit _kit;
            private DanteCarapaceVisual _ward;

            public Shield(DanteHeroKit kit)
                : base("dante_skill1", "SHIELD",
                       "Stone armour for 20 s. No status can touch you (except a tag), and casting it shakes off what you already have.",
                       GeoRules.ShieldCooldown, GeoRules.ShieldSeconds, AbilityGlyph.DanteShield,
                       summary: "20 s of stone armour: no statuses land on you.",
                       castAction: "hero-dante-roar", viewmodelAction: "carapace-guard",
                       castCue: "sfx_cast_dante_carapace") { _kit = kit; }

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
                ctx.Motor.ClearStun();
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
            public override bool DefersPredictedEffect => true;

            public Boulder()
                : base("dante_skill2", "BOULDER",
                       "Attacking. Hold to aim, release to hurl a boulder. Whoever it hits or rolls into is Concussed: slower, no sprint, wobbly aim.",
                       GeoRules.BoulderCooldown, 0.0f, AbilityGlyph.DanteStomp,
                       summary: "Hurl a boulder. Whoever it hits is Concussed.",
                       telegraphRadius: GeoRules.BoulderHitRadius, telegraphRange: 9.0f,
                       castAction: "hero-dante-stomp", viewmodelAction: "stomp-heavy",
                       castCue: "sfx_cast_dante_stomp")
            {
                AimByHolding(3.0f, 9.0f, rampSeconds: 0.55f, maxHoldSeconds: 0.0f);
                TelegraphStyle = GroundReticle.Style.Fissure;
            }

            public override bool CanActivate(AbilityContext ctx) => base.CanActivate(ctx) && !ctx.Motor.IsDefender;

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_dante_grunt", ctx.Position);
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Squash(0.2f);
                Vector3 from = ctx.Position + Vector3.up * 1.6f + ctx.Forward * 0.5f;
                DanteBoulder.Spawn(from, AimedDestination(ctx), ctx.Motor.PlayerSlot);
            }
        }

        // ================================================================== BARRIER (defending)

        private sealed class Barrier : HeroAbility
        {
            private GameObject _field;
            private CharacterMotor _owner;

            public Barrier()
                : base("dante_skill2d", "BARRIER",
                       "Defending. A wide stone force field in front of you for 7.5 s. It follows you, and every slipper that hits it flies back.",
                       GeoRules.BarrierCooldown, GeoRules.BarrierSeconds, AbilityGlyph.DanteShield,
                       summary: "A force field in front of you reflects slippers.",
                       castAction: "hero-dante-roar", viewmodelAction: "carapace-guard",
                       castCue: "sfx_cast_dante_carapace") { }

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
                    NetCue.PlayImpact("hit_body", "guard_block", s.transform.position, 0.8f);
                }
            }

            protected override void OnEnd(AbilityContext ctx) { if (_field != null) UnityEngine.Object.Destroy(_field); _field = null; }
            protected override void OnCancelled(AbilityContext ctx) => OnEnd(ctx);
        }

        // ================================================================== EARTHQUAKE (ultimate)

        private sealed class Earthquake : HeroAbility
        {
            public Earthquake()
                : base("dante_ultimate", "EARTHQUAKE",
                       "Stamp and the whole court heaves. Every other player is Concussed: slower, no sprint, wobbly aim.",
                       0.0f, 0.0f, AbilityGlyph.DanteFissure,
                       summary: "The whole court heaves. Everyone is Concussed.",
                       castAction: "hero-dante-fissure", viewmodelAction: "fissure-slam",
                       castCue: "sfx_cast_dante_fissure")
            {
                TelegraphStyle = GroundReticle.Style.Fissure;
                Windup = UltimateWindup;
                SupportsPendingSnapshot = true;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_dante_ult", ctx.Position);
                DanteSeismicVisual.Impact(ctx.Position, ctx.Forward, 6.0f, true);
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Stretch(0.4f);
                if (UnityEngine.Camera.main != null)
                    UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>()?.Shake(0.8f, 0.6f);
                var round = ctx.Round;
                if (round == null || !NetAuthority.ShouldResolve()) return;
                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == ctx.Motor.PlayerSlot) continue;
                    p.ApplyConcussed();
                    p.ApplyResolvedImpact(Vector3.up * 3.0f);
                    HitFeel.Land(p, HitFeel.Weight.Ultimate, UiTheme.HeroEarthBright, ctx.Position);
                }
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
                    NetCue.Play("sfx_cast_dante_stomp", transform.position);
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
