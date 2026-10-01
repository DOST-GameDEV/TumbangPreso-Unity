using System;
using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class SeanHeroKit : HeroKit, ITimedKitReplication, IWorldEffectBinding
    {
        public bool IsIgnitionCannonActive { get; set; }
        public override bool BlocksOwnLocomotion => Skill1.IsWindingUp || Skill1.IsActive;
        public override bool BlocksOwnActions => BlocksOwnLocomotion;
        public const float StokeDistance = 2f, StokeAnticipation = .18f, StokeRecovery = .25f;
        public static float StokeSpeed => Mathf.Sqrt(2f * Balance.Friction * StokeDistance);
        public static float StokeTravelSeconds => StokeSpeed / Balance.Friction;
        public bool IsEmpoweredThrowLoadedFor(Slipper shoe) => ((IgnitionCannonAbility)AttackingSkill).LoadedFor(shoe);
        private bool _joinChargeStateSettled;
        public TimedKitSnapshot CaptureTimedKit()
            => new TimedKitSnapshot(AttackingSkill, IsIgnitionCannonActive ? AttackingSkill.DurationRemaining : 0);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
            => RestoreJoiningIgnition(motor, state.PersonalRemaining);

        public HeroMovementState CaptureMovementState()=>((StokeStepAbility)Skill1).CaptureMovement();
        public bool RestoreJoiningMovement(CharacterMotor motor,HeroMovementState state,float age)
            => motor!=null && ((StokeStepAbility)Skill1).RestoreMovement(
                new AbilityContext(motor,motor.GetComponent<Carrier>(),motor.GetComponent<CombatVerbs>()),state,age);
        public void AdoptMovementFields(int owner) { } // Stoke Step creates no recoverable fire fields.
        public void RebindWorldEffects(CharacterMotor motor)
        { if (motor != null) AdoptMovementFields(motor.PlayerSlot); }

        public bool RestoreJoiningIgnition(CharacterMotor motor, float remaining)
        {
            // Initial hydration cannot overwrite a newer cast or resurrect a
            // charge already consumed while the joining snapshot was in flight.
            if (motor == null || _joinChargeStateSettled || IsIgnitionCannonActive || AttackingSkill.IsActive) return false;
            _joinChargeStateSettled = true;
            RestoreIgnition(motor, remaining);
            return true;
        }

        public void ConsumeIgnition()
        {
            _joinChargeStateSettled = true;
            IsIgnitionCannonActive = false;
        }

        public void RestoreIgnition(CharacterMotor motor, float remaining)
        {
            if (motor == null) return;
            var context = new AbilityContext(motor, motor.GetComponent<Carrier>(), motor.GetComponent<CombatVerbs>());
            ((IgnitionCannonAbility)AttackingSkill).RestoreCharge(context, remaining);
        }
        private float _supernovaPoseTime = -1;
        public float SupernovaPoseTime => Ultimate.IsWindingUp
            ? Mathf.Lerp(0, .20f, 1 - Ultimate.WindupRemaining / Mathf.Max(.01f, Ultimate.Windup))
            : _supernovaPoseTime;

        public SeanHeroKit() : base("sean", "SEAN")
        {
            Skill1 = new StokeStepAbility();
            // ABILITY-2: the four-slot shape; the defending slot waits for the owner's Pyro design.
            AttackingSkill = new IgnitionCannonAbility(this);
            DefendingSkill = new PlaceholderRoleAbility("sean_skill2d", "Sean", AbilityGlyph.SeanIgnite);
            Ultimate = new SupernovaSmashdownAbility(this);
        }

        /// <summary>
        /// ⚠️ PRICED ABOVE DANTE AND BELOW CHESKA BECAUSE IT PAYS A POINT DIRECTLY. Supernova's
        /// blast knocks the lata over, so unlike every other ultimate in the game it converts
        /// into score without needing a follow-up. It has to be aimed and it can be walked out
        /// of, which is what keeps it under Thunderstrike's 150.
        ///
        /// ⚠️ 15 CHARGES, WHICH IS 15 LATA KNOCKDOWNS. Was 130 against a knockdown worth 25,
        /// which is 5.2. `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        public override float UltimateCost => 15.0f;

        private sealed class StokeStepAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private bool _movementKnown, _movementRestored;
            private GameObject _rushAura;
            public StokeStepAbility()
                : base("sean_skill1", "STOKE STEP",
                    "Brace, then burst two metres along your aim. Recover before acting again. No knockdown or burning trail.",
                    30f, StokeTravelSeconds + StokeRecovery, AbilityGlyph.SeanRush,
                    summary: "A short committed burst, then a planted recovery.",
                    castAction: "hero-sean-dash", viewmodelAction: "thrust-fire", castCue: "sfx_cast_sean_rush")
            { Windup = StokeAnticipation; }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && ctx?.Motor != null && ctx.Motor.IsGrounded && !ctx.Motor.IsFlying;

            public HeroMovementState CaptureMovement() => IsActive
                ? new HeroMovementState { Remaining = DurationRemaining, Wake = Array.Empty<Vector3>() }
                : HeroMovementState.Empty;

            public bool RestoreMovement(AbilityContext ctx, HeroMovementState state, float age)
            {
                if (!state.Valid(Duration, 0, age) || (state.Wake?.Length ?? 0) != 0
                    || (_movementKnown && (!_movementRestored || !IsActive))) return false;
                float remaining = Mathf.Max(0, state.Remaining - age);
                if (_movementKnown) remaining = Mathf.Min(remaining, DurationRemaining);
                _movementKnown = true; _movementRestored = remaining > 0;
                if (remaining <= 0) { EndEarly(ctx); return true; }
                // The movement snapshot already owns position and velocity. Recovery
                // restores only the remaining gate; never launch a second impulse.
                RestoreLiveClock(remaining);
                return true;
            }

            public override void Tick(AbilityContext ctx, float dt)
            {
                if ((IsWindingUp || IsActive) && ctx?.Motor != null
                    && (!ctx.Motor.RoundActive || ctx.Motor.IsStunned || ctx.Motor.IsFeared || ctx.Motor.IsRooted))
                { _movementKnown = true; _movementRestored = false; RollBackPredictedCast(ctx, false); }
                base.Tick(ctx, dt);
                if (_rushAura != null && DurationRemaining <= StokeRecovery) ReleaseAura();
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _movementKnown = true; _movementRestored = false;
                if (ctx?.Motor == null || !ctx.Motor.IsGrounded || ctx.Motor.IsFlying
                    || ctx.Motor.IsStunned || ctx.Motor.IsFeared || ctx.Motor.IsRooted)
                { EndEarly(ctx); return; }
                Vector3 heading = ctx.Forward; heading.y = 0;
                if (heading.sqrMagnitude < .0001f) { EndEarly(ctx); return; }
                // Native motor owns friction, body/cover collision and confinement.
                // No teleport, vertical launch, hit loop or damaging world field.
                ctx.Motor.ApplyImpulse(heading.normalized * StokeSpeed);
                _rushAura = AbilityVfx.AttachAura(ctx.Motor.transform, AbilityVfx.Aura.FireEmber, StokeTravelSeconds);
            }

            private void ReleaseAura()
            {
                if (_rushAura != null) { _rushAura.SetActive(false); UnityEngine.Object.Destroy(_rushAura); }
                _rushAura = null;
            }
            protected override void OnEnd(AbilityContext ctx) => ReleaseAura();
            protected override void OnCancelled(AbilityContext ctx) => ReleaseAura();
            public override void Reset()
            { ReleaseAura(); base.Reset(); _movementKnown = _movementRestored = false; }
        }

        private sealed class IgnitionCannonAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly SeanHeroKit _kit;

            private Slipper _loaded;
            private int _joiningSeat = -1;
            public IgnitionCannonAbility(SeanHeroKit kit)
                : base("sean_skill2", "EMPOWERED THROW",
                       "Load your held slipper for one throw within eight seconds. Its first impact creates a compact pressure burst that nudges nearby rivals.",
                       EmpoweredThrowRules.Cooldown, EmpoweredThrowRules.LoadSeconds, AbilityGlyph.SeanIgnite,
                       summary: "One held throw, one compact pressure burst; no lingering fire.",
                       castAction: "hero-sean-ignite", viewmodelAction: "ignite", castCue: "sfx_cast_sean_cannon")
            { _kit = kit; }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && !ctx.Motor.IsDefender && ctx.Carrier?.Held != null
                    && ctx.Carrier.Held.State == SlipperState.Held && ctx.Carrier.Held.Holder == ctx.Motor;

            public bool LoadedFor(Slipper shoe)
                => _kit.IsIgnitionCannonActive && DurationRemaining > 0 && shoe != null
                    && shoe.State == SlipperState.Held && shoe.Holder != null
                    && (_loaded == shoe || (_loaded == null && _joiningSeat >= 0
                        && shoe.Affinity == SlipperAffinity.FireExplosive && shoe.Holder.PlayerSlot == _joiningSeat));

            protected override void OnActivate(AbilityContext ctx)
            {
                _loaded = ctx.Carrier?.Held; _joiningSeat = -1;
                _kit._joinChargeStateSettled = true;
                _kit.IsIgnitionCannonActive = _loaded != null;
                if (_loaded != null && NetAuthority.ShouldResolve())
                {
                    _loaded.Affinity = SlipperAffinity.FireExplosive;
                    Net.MatchRpc.Instance?.BroadcastSlipperState(_loaded);
                }
                RefreshEmber(ctx);
            }

            private void RefreshEmber(AbilityContext ctx)
            {
                var shoe = ctx.Carrier != null ? ctx.Carrier.Held : null;
                if (LoadedFor(shoe)) SeanIgnitionVisual.Ensure(shoe.GetComponentInChildren<MeshFilter>(), shoe, _kit);
            }

            public void RestoreCharge(AbilityContext ctx, float remaining)
            {
                if (remaining <= 0) { EndEarly(ctx); _kit.IsIgnitionCannonActive = false; return; }
                RestoreLiveClock(Mathf.Min(remaining, EmpoweredThrowRules.LoadSeconds));
                // Equipment recovery carries the held affinity on the exact object.
                // Never adopt an arbitrary shoe solely because it is currently held.
                _loaded = null; _joiningSeat = ctx.Motor.PlayerSlot;
                _kit.IsIgnitionCannonActive = true;
                RefreshEmber(ctx);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (!_kit.IsIgnitionCannonActive || ctx.Motor.IsDefender)
                { EndEarly(ctx); return; }
                if (_loaded == null && _joiningSeat >= 0)
                {
                    var held = ctx.Carrier?.Held;
                    if (held == null) return;
                    if (held.Holder != ctx.Motor || held.Affinity != SlipperAffinity.FireExplosive) return;
                    _loaded = held; _joiningSeat = -1;
                }
                if (_loaded == null || ctx.Carrier?.Held != _loaded
                    || _loaded.State != SlipperState.Held || _loaded.Holder != ctx.Motor)
                { EndEarly(ctx); return; }
                RefreshEmber(ctx);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                _kit.IsIgnitionCannonActive = false;
                if (_loaded != null && _loaded.State != SlipperState.InFlight
                    && _loaded.Affinity == SlipperAffinity.FireExplosive && NetAuthority.ShouldResolve())
                {
                    _loaded.Affinity = SlipperAffinity.Normal;
                    Net.MatchRpc.Instance?.BroadcastSlipperState(_loaded);
                }
                _loaded = null; _joiningSeat = -1;
            }
            public override void Reset()
            {
                OnEnd(null); base.Reset(); _kit._joinChargeStateSettled = false;
            }
        }

        private sealed class SupernovaSmashdownAbility : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            private float _airTimer;
            private readonly SeanHeroKit _kit;
            private float _landingAge;
            private bool _diving;
            private bool _hasLeftGround;
            private bool _smashed;

            public SupernovaSmashdownAbility(SeanHeroKit kit)
                // ⚠️⚠️ THE CARD USED TO SAY *"Knocks the lata over on impact"* AND 🧑 READ IT
                // EXACTLY AS WRITTEN: *"this too it reads as unusable on defender"*. It was an
                // honest description of an attacker-only power. Both halves are fixed rather than
                // just the sentence: `HeroHazards.CreateExplosion` no longer knocks over the
                // CASTER'S OWN objective, and the crater below is what the ultimate leaves for
                // either role. The text now leads with what is true in both.
                //
                // ⚠️ DURATION 2.0 s TO 5.6 s, AND IT IS THE CRATER'S LIFE PLUS THE SLAM. The old
                // 2.0 covered the leap and the landing and nothing else, which is why
                // `SkyEvent.SecondsFor` gave it the bare floor. The ability now stays active for
                // as long as the ground is burning, which is what `IsActive` should mean.
                : base("sean_ultimate", "SUPERNOVA",
                       "Launches you up and slams you back down. The blast throws everyone near it clear and leaves the road burning behind you.",
                       0.0f, 5.6f, TumbangPreso.UI.AbilityGlyph.SeanSupernova,
                       summary: "Leap and crash down. Leaves burning ground where you land.",
                       telegraphRadius: 5.4f, telegraphRange: 0.0f,
                       castAction: "hero-sean-supernova",
                       viewmodelAction: "supernova-slam",
                       castCue: "sfx_cast_sean_supernova")
            {
                _kit = kit;
                TelegraphStyle = Visual.GroundReticle.Style.Ember;
                Windup = UltimateWindup;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _airTimer = 0.55f;
                _landingAge = 0;
                _kit._supernovaPoseTime = .20f;
                _diving = false;
                _hasLeftGround = false;
                _smashed = false;

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Stretch(0.06f);

                // Launch upward
                ctx.Motor.ApplyImpulse(Vector3.up * 14.0f + ctx.Forward * 4.0f);
                NetCue.Play("hero_sean_ult", ctx.Position);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_smashed)
                {
                    _landingAge += dt;
                    _kit._supernovaPoseTime = _landingAge < .43f ? 1.12f + _landingAge : -1;
                    return;
                }
                if (!ctx.Motor.IsGrounded) _hasLeftGround = true;
                _airTimer -= dt;
                if (_airTimer <= 0.0f && !_diving)
                {
                    _diving = true;
                    // ⚠️ THE MIDDLE BEAT LOSES ITS CAPTION. "BLAST OFF!" on the launch and
                    // "SUPERNOVA!" on the landing are the two moments a player acts on; a third
                    // word 0.55 s into a 2 s ultimate is one the eye never finishes reading.
                    ctx.Motor.ApplyImpulse(Vector3.down * 28.0f);
                }

                _kit._supernovaPoseTime = !_diving
                    ? Mathf.Lerp(.20f, .78f, 1 - Mathf.Clamp01(_airTimer / .55f))
                    : Mathf.Lerp(.78f, 1.04f, Mathf.Clamp01(-_airTimer / .16f));
                if (!_diving) return;

                // An interrupted leap under a low ceiling may never report an airborne
                // frame. It may still impact while grounded, never from a timer in midair.
                if (ctx.Motor.IsGrounded && (_hasLeftGround || _airTimer <= -.12f))
                {
                    _smashed = true;
                    _kit._supernovaPoseTime = 1.12f;

                    var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                    if (squash != null) squash.Squash(0.08f);

                    NetCue.Play("sfx_explosion_heavy", ctx.Position);

                    // ⚠️ THE BLAST IS BIGGER AND THE COMIC TEXT IS GONE. 4.8 to 5.4 m, knockback
                    // 16 to 22, hold 2.2 to 2.6: 🧑 asked for *"more of an impact"* and this is
                    // the half that is a number rather than a picture. The word went with every
                    // other cast callout in § 31.8; a blast this size does not need announcing.
                    HeroHazards.CreateExplosion(ctx.Position, 5.4f, 22.0f, 2.6f,
                        ctx.Motor.PlayerSlot, comicText: null,
                        style: HeroHazards.ExplosionStyle.Fire);

                    // ⚠️⚠️ AND THE GROUND STAYS ON FIRE, WHICH IS THE PART THAT WAS MISSING.
                    // `SpawnSupernovaCrater` carries why: Sean's was the only ultimate in the
                    // game that left nothing behind, which is 🧑's *"it just reads as a one time
                    // down on laata and knockback"* exactly. It is also what makes the power
                    // worth pressing as a taya, now that the blast no longer topples his own can.
                    HeroHazards.SpawnSupernovaCrater(ctx.Position, 5.4f, 5.0f,
                                                     ctx.Motor.PlayerSlot);

                    // ⚠️⚠️ THIS WAS `SpawnMagmaEruption`, WHICH IS DANTE'S. Sean's whole identity
                    // is `HeroFire` and Dante's is `HeroMagmaCore`, and the biggest moment in
                    // Sean's kit was throwing up Dante's orange rock. Two heroes reading as one
                    // is the most expensive version of *"they all look repetitive"*, because it
                    // costs a CHARACTER rather than an effect. `SpawnCastFlash` in Sean's own
                    // colour is now inside `CreateExplosion` via the Fire style, so the eruption
                    // here is removed rather than recoloured: one blast, one set of particles.
                }
            }

            protected override void OnEnd(AbilityContext ctx) => _kit._supernovaPoseTime = -1;
        }
    }
}
