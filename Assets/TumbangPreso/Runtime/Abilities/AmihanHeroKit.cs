using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ AMIHAN, THE FIRST KIT IN THE NEW SHAPE (owner, 2026-09-25): a SIGNATURE that is the same
    /// in either role, a ROLE ability that is one power attacking and another defending, and an
    /// ultimate. *"there will be 2 abilities, one signature ability that doesnt change and stays no
    /// matter what role and one that changes."* Power: Anemo (wind).
    ///
    /// | Slot | Name | Owner's table |
    /// |---|---|---|
    /// | Signature | QUICK DASH | Propel forward in the target direction; inflicts Whirled and slightly pushes back other players. 40 s. |
    /// | Attacking | UPDRAFT | Fly for 10 seconds (owner: *"flies high and can throw slippers but cant pick up unless they choose to go down"*). |
    /// | Defending | WHIRLWIND | An arc-shaped gale that inflicts Whirled on players it hits as it swiftly moves forward. Lasts 2.5 s. 35 s. |
    /// | Ultimate | STORM SURGE | After a 2.5 s delay, a map-wide fan of wind in the target direction that greatly pushes back all players and slippers caught inside. 15 points. |
    ///
    /// Every number is in `Core.AmihanRules`; the design, the decisions the owner left open and
    /// every moving part are in `docs/reports/amihan-kit-2026-09-25/plan.md`, and the look, the
    /// sound and the motion follow `direction.md` beside it, which the owner made the baseline for
    /// every later skill rework.
    ///
    /// ⚠️ HOST AUTHORITY: every Whirl, push and carry on another body is behind
    /// `NetAuthority.ShouldResolve()` (`tools/audit_ability_authority.py`); a peer moves only its
    /// own body (the dash, the flight), exactly as the other kits do.
    /// </summary>
    public sealed class AmihanHeroKit : HeroKit
    {
        public override float UltimateCost => AmihanRules.StormSurgeCost;

        public AmihanHeroKit() : base("amihan", "AMIHAN")
        {
            Skill1 = new QuickDash();
            AttackingSkill = new Updraft();
            DefendingSkill = new Whirlwind();
            Ultimate = new StormSurge();
        }

        /// <summary>True while Updraft holds her in the air.</summary>
        public bool IsFlying => AttackingSkill != null && AttackingSkill.IsActive;

        // ================================================================== QUICK DASH (signature)

        private sealed class QuickDash : HeroAbility
        {
            private Vector3 _start, _dir;
            private CharacterMotor _caster;
            private readonly System.Collections.Generic.HashSet<int> _hit = new System.Collections.Generic.HashSet<int>();

            /// <summary>How long the dash hits for: the held carry plus the slide out of it.</summary>
            private static float HitWindow => AmihanRules.QuickDashHoldSeconds
                + AmihanRules.QuickDashSpeed / (2.0f * Balance.Friction) + 0.05f;

            public QuickDash()
                : base("amihan_skill1", "QUICK DASH",
                       "Throw yourself forward where you aim. Anyone you pass is shoved aside and Whirled: slipper dropped, no pickups for 2.5 s.",
                       AmihanRules.QuickDashCooldown, 0.0f, AbilityGlyph.AmihanQuickDash,
                       summary: "Dash where you aim. Whoever you pass is Whirled and shoved.",
                       castAction: "hero-amihan-dash", viewmodelAction: "gust-dash",
                       castCue: "sfx_cast_amihan_dash")
            {
                Duration = HitWindow;
            }

            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx)) return false;
                // Not from the air: Updraft's descent is the way down, and a dash that started 2.8 m
                // up would be a second flight.
                return !ctx.Motor.IsFlying;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                _caster = ctx.Motor;
                _start = ctx.Position;
                _dir = ctx.Forward; _dir.y = 0.0f;
                _dir = _dir.sqrMagnitude > 0.001f ? _dir.normalized : ctx.Motor.transform.forward;
                _hit.Clear();

                // Her own body: the owner simulates it (BeginCarry refuses anywhere else).
                ctx.Motor.BeginCarry(_dir * AmihanRules.QuickDashSpeed, AmihanRules.QuickDashHoldSeconds);

                Vector3 end = _start + _dir * AmihanRules.QuickDashDistance;
                end.x = Mathf.Clamp(end.x, -AIController.PlayableHalfX, AIController.PlayableHalfX);
                end.z = Mathf.Clamp(end.z, -AIController.PlayableHalfZ, AIController.PlayableHalfZ);
                AmihanDashWake.Build(_start, end);
                ctx.Motor.GetComponentInChildren<CharacterSquashStretch>()?.Stretch(0.18f);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_caster == null) return;
                if (!NetAuthority.ShouldResolve()) return;
                var round = GameServices.Round;
                if (round == null) return;
                Vector3 a = Flat(_start), b = Flat(_caster.transform.position);
                foreach (var p in round.Players)
                {
                    if (p == null || p == _caster || _hit.Contains(p.PlayerSlot)) continue;
                    Vector3 t = Flat(p.transform.position);
                    if (DistanceToSegment(t, a, b) > AmihanRules.QuickDashHitRadius) continue;
                    _hit.Add(p.PlayerSlot);

                    // Pushed off her line: sideways out of her path and a little along it, 1.2 m.
                    Vector3 onLine = a + Vector3.Project(t - a, _dir);
                    Vector3 side = t - onLine; side.y = 0.0f;
                    if (side.sqrMagnitude < 0.0001f) side = Vector3.Cross(Vector3.up, _dir);
                    Vector3 push = (side.normalized * 0.75f + _dir * 0.65f).normalized * AmihanRules.QuickDashPushSpeed;
                    p.ApplyWhirled();
                    p.ApplyResolvedImpact(push + Vector3.up * 1.5f);
                }
            }

            private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0.0f, v.z);

            private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
            {
                Vector3 ab = b - a;
                float length = ab.sqrMagnitude;
                if (length < 1e-6f) return Vector3.Distance(p, a);
                float u = Mathf.Clamp01(Vector3.Dot(p - a, ab) / length);
                return Vector3.Distance(p, a + ab * u);
            }
        }

        // ================================================================== UPDRAFT (attacking)

        private sealed class Updraft : HeroAbility
        {
            private CharacterMotor _flyer;

            public Updraft()
                : base("amihan_skill2", "UPDRAFT",
                       "Attacking. Fly high for 10 s, out of the taya's reach. Throw from the air; press again or grab to land and pick up.",
                       AmihanRules.UpdraftCooldown, AmihanRules.UpdraftSeconds, AbilityGlyph.AmihanUpdraft,
                       summary: "Fly high for 10 s. Throw from the air, land to grab.",
                       castAction: "hero-amihan-updraft", viewmodelAction: "updraft-lift",
                       castCue: "sfx_cast_amihan_updraft") { }

            public override bool CanReactivate => true;

            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx)) return false;
                if (ctx.Motor.IsFlying) return false;
                // ⚠️⚠️ NEVER AN ESCAPE FROM THE RETRIEVAL. A body aloft is out of the taya's reach
                // (`CharacterMotor.IsTaggable`), so taking off with a slipper from inside the box
                // would turn the one moment the game is built around (`VISION.md` § 0: *"going back
                // in for your tsinelas is the only moment you can be caught"*) into a free exit.
                return !(ctx.Motor.HoldingSlipper && ctx.Motor.IsInsideBox());
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                _flyer = ctx.Motor;
                // ⚠️ ON EVERY PEER: the flag is what the host's pickup and tag gates read and what
                // every peer poses her with; only the peer simulating her body moves it.
                _flyer.BeginFlight(AmihanRules.UpdraftHeight, AmihanRules.UpdraftRiseSeconds, AmihanRules.UpdraftDescentSpeed);
                AmihanUpdraftLaunch.Build(ctx.Position, AmihanRules.UpdraftHeight);
                AmihanHoverRing.Attach(_flyer);
                _flyer.GetComponentInChildren<CharacterSquashStretch>()?.Stretch(0.28f);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // A stun takes her out of the sky: the motor has already turned the hold into the
                // glide (`StepFlightVertical`), and the flight is over as far as the kit goes.
                if (_flyer != null && _flyer.IsStunned) DurationRemaining = 0.0f;
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                if (_flyer == null) return;
                _flyer.EndFlight();
                NetCue.PlayVaried("sfx_amihan_updraft_settle", _flyer.transform.position, 0.96f, 1.04f, 0.8f);
                _flyer = null;
            }

            protected override void OnCancelled(AbilityContext ctx)
            {
                // A round reset or a refused cast: straight back under gravity, no glide.
                _flyer?.EndFlightImmediately();
                _flyer = null;
            }
        }

        // ================================================================== WHIRLWIND (defending)

        private sealed class Whirlwind : HeroAbility
        {
            // ⚠️ THE GALE IS A WORLD OBJECT, SO THE OWNER WAITS FOR THE HOST before drawing it, as
            // Cheska's barricade does: a refused cast must not leave a front rolling across one
            // player's screen that exists nowhere else.
            public override bool DefersPredictedEffect => true;

            public Whirlwind()
                : base("amihan_skill2d", "WHIRLWIND",
                       "Defending. Roll an arc of gale down the court for 2.5 s. Everyone it passes is Whirled: slipper dropped, no pickups.",
                       AmihanRules.WhirlwindCooldown, 0.0f, AbilityGlyph.AmihanWhirlwind,
                       summary: "Roll a gale down a lane. Everyone it passes is Whirled.",
                       telegraphRadius: AmihanRules.WhirlwindWidth * 0.5f,
                       telegraphRange: AmihanRules.WhirlwindStart + AmihanRules.WhirlwindWidth * 0.5f,
                       castAction: "hero-amihan-whirlwind", viewmodelAction: "gale-sweep",
                       castCue: "sfx_cast_amihan_whirlwind") { }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                AmihanGale.Spawn(ctx.Position, ctx.Forward, ctx.Motor.PlayerSlot);
                ctx.Motor.GetComponentInChildren<CharacterSquashStretch>()?.Squash(0.06f);
            }
        }

        // ================================================================== STORM SURGE (ultimate)

        private sealed class StormSurge : HeroAbility
        {
            private AmihanStorm _storm;

            public StormSurge()
                : base("amihan_ultimate", "STORM SURGE",
                       "Plant and call the storm. After 2.5 s a map-wide fan of wind throws every player and loose slipper in it to the edge.",
                       0.0f, 0.0f, AbilityGlyph.AmihanStormSurge,
                       summary: "After 2.5 s, a map-wide wind blows everyone to the edge.",
                       castAction: "hero-amihan-storm", viewmodelAction: "storm-call",
                       castCue: "sfx_cast_amihan_storm")
            {
                // ⚠️⚠️ THE 2.5 s DELAY IS THE WIND-UP, NOT A TIMER INSIDE THE EFFECT. Owner's table:
                // *"After a 2.5 s delay"*. `HeroAbility.Windup` is uninterruptible and roots the caster
                // (its header has the whole argument), which is exactly this power: she plants and
                // calls the storm, everybody else gets 2.5 s to read the fan and leave it, and she
                // pays for the ultimate by standing still inside her own telegraph. Phaister's ritual
                // is the precedent for a wind-up longer than the shared 0.4 s.
                Windup = AmihanRules.StormSurgeGatherSeconds;
            }

            public override void Activate(AbilityContext ctx)
            {
                base.Activate(ctx);
                // The wind-up has begun: the fan goes down now, on every peer, from the accepted
                // pose, so what every player reads is the real wind.
                if (IsWindingUp && ctx?.Motor != null)
                {
                    if (_storm != null) Object.Destroy(_storm.gameObject);
                    _storm = AmihanStorm.Spawn(ctx.Position, ctx.Forward, ctx.Motor.PlayerSlot, this);
                }
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // The end of the delay: the wind leaves. A storm that was never spawned (an editor
                // test calling this directly, a headless harness) is spawned and released at once.
                if (_storm == null && ctx?.Motor != null)
                    _storm = AmihanStorm.Spawn(ctx.Position, ctx.Forward, ctx.Motor.PlayerSlot, this, AmihanRules.StormSurgeGatherSeconds);
                _storm?.Release();
                _storm = null;
            }

            public override void Reset()
            {
                if (_storm != null && !_storm.Released) Object.Destroy(_storm.gameObject);
                _storm = null;
                base.Reset();
            }
        }
    }
}
