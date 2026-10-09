using System;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ CHESKA, CRYO, IN THE NEW SHAPE (ABILITY-2, owner 2026-09-26: *"cheska to cryo"*, with the
    /// table below). `docs/reports/ability-rework-2026-09-26/plan.md` § 3.2; numbers in `Core.CryoRules`.
    ///
    /// | Slot | Name | Owner's table |
    /// |---|---|---|
    /// | Signature | COLD FEET | a chilling field on the floor that inflicts Chilled indefinitely to players caught inside it; lasts7.5s;35s |
    /// | Attacking | FROSTBITE | imbue the slipper with Frozen; hitting another player with it inflicts Frozen; 35 s |
    /// | Defending | GLACIAL WALL | an arc-shaped icicle wall that blocks slippers and players; takes 3 slipper hits to shatter; 35 s |
    /// | Ultimate | ABSOLUTE ZERO | inflict Frozen on every player, followed by Chilled after thawing; 12 objective points |
    ///
    /// ⚠️ THE OLD KIT (Permafrost Sheet with two charges, Ice Barricade with one charge refilled by a
    /// retrieval, Glacial Nova round her feet) is replaced, not kept beside it; its reasoning survives in
    /// git history (`CheskaHeroKit.cs` before this commit) and the effects it built are reused: the sheet
    /// is Cold Feet's field, the barricade is the wall's body, the nova's prison is the freeze.
    /// </summary>
    public sealed class CheskaHeroKit : HeroKit, ITimedKitReplication
    {
        public CheskaHeroKit() : base("cheska", "YASMIN")
        {
            Skill1 = new ColdFeet();
            AttackingSkill = new Frostbite(this);
            DefendingSkill = new GlacialWall();
            Ultimate = new AbsoluteZero();
        }

        public override float UltimateCost => CryoRules.AbsoluteZeroCost;

        /// <summary>True during the Frostbite window; every throw in it carries frost.</summary>
        public bool IsFrostbiteLoaded { get; set; }
        private bool _joiningFrostbiteSettled;

        private CheskaFrostbiteCoating _loadedCue;

        public override void Tick(AbilityContext context, float dt)
        {
            base.Tick(context, dt);
            // Equipment may hydrate after the accepted timed-kit snapshot.
            // This only follows the existing load; it never starts or refreshes it.
            var held = context?.Carrier?.Held;
            if (IsFrostbiteLoaded && held != null && (_loadedCue == null || _loadedCue.Shoe != held))
                _loadedCue = CheskaFrostbiteCoating.Ensure(held.GetComponentInChildren<MeshFilter>(), held, this);
        }

        public TimedKitSnapshot CaptureTimedKit()
            => new TimedKitSnapshot(AttackingSkill, IsFrostbiteLoaded ? AttackingSkill.DurationRemaining : 0);

        public bool RestoreTimedKit(CharacterMotor motor, TimedKitSnapshot state)
        {
            float remaining = state.PersonalRemaining;
            if (motor == null || _joiningFrostbiteSettled || AttackingSkill.IsActive ||
                !float.IsFinite(remaining) || remaining < 0 || remaining > CryoRules.FrostbiteLoadSeconds ||
                (remaining > 0 && motor.IsDefender)) return false;
            _joiningFrostbiteSettled = true;
            if (remaining <= 0) return false;
            // Equipment hydration may follow this message. Restore the accepted
            // personal load without activating, spending or touching a slipper.
            IsFrostbiteLoaded = true;
            ((Frostbite)AttackingSkill).RestoreLoad(remaining);
            return true;
        }

        public override void Reset()
        {
            IsFrostbiteLoaded = false; _joiningFrostbiteSettled = false;
            base.Reset();
        }

        public override void ResetForRound(AbilityContext ctx)
        {
            base.ResetForRound(ctx);
            IsFrostbiteLoaded = false; _joiningFrostbiteSettled = false;
        }

        /// <summary>Explicit cancellation ends the window; ordinary throws do not.</summary>
        public void CancelFrostbiteWindow() => IsFrostbiteLoaded = false;

        // ================================================================== COLD FEET (signature)

        private sealed class ColdFeet : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;

            public ColdFeet()
                : base("cheska_skill1", "COLD FEET",
                       "Hold to aim, release to freeze a patch of street for 7.5 s. Anyone standing in it is Chilled: half speed, and it lingers.",
                       CryoRules.ColdFeetCooldown, 0.0f, AbilityGlyph.CheskaFrostSheet,
                       summary: "Freeze a patch of street. Anyone in it is Chilled.",
                       telegraphRadius: CryoRules.ColdFeetRadius, telegraphRange: CryoRules.ColdFeetMaxRange,
                       castAction: "hero-cheska-frostwave", viewmodelAction: "frost-sweep",
                       castCue: "sfx_cast_cheska_coldfeet")
            {
                AimByHolding(CryoRules.ColdFeetMinRange, CryoRules.ColdFeetMaxRange, rampSeconds: 1.0f, maxHoldSeconds: 0.0f);
                TelegraphStyle = GroundReticle.Style.Frost;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_cheska_grunt", ctx.Position);
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Squash(0.035f);
                // The field IS the old sheet: `IceSheetComponent` already refreshes Chilled on every
                // body inside it each step, which is the owner's "indefinitely while inside", and the
                // 5 s Chilled runs out after they step off.
                HeroHazards.SpawnIceSheet(AimedDestination(ctx), CryoRules.ColdFeetRadius, CryoRules.ColdFeetSeconds,
                                          ctx.Motor.PlayerSlot, 1.0f);
            }
        }

        // ================================================================== FROSTBITE (attacking)

        private sealed class Frostbite : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.Predicted;
            private readonly CheskaHeroKit _kit;

            public Frostbite(CheskaHeroKit kit)
                : base("cheska_skill2", "FROSTBITE",
                       "Attacking. Frost your slipper for 15 s. Every throw during that window inflicts Frozen on a player hit: no moving, no grabbing, for 2.5 s.",
                       CryoRules.FrostbiteCooldown, CryoRules.FrostbiteLoadSeconds, AbilityGlyph.CheskaFrostbite,
                       summary: "For 15 s, every throw can freeze a player.",
                       castAction: "hero-cheska-frostbite", viewmodelAction: "frost-load",
                       castCue: "sfx_cast_cheska_frostbite")
            {
                _kit = kit;
            }

            public override bool CanActivate(AbilityContext ctx)
                => base.CanActivate(ctx) && !ctx.Motor.IsDefender && ctx.Motor.HoldingSlipper;

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit._joiningFrostbiteSettled = true;
                _kit.IsFrostbiteLoaded = true;
            }

            public void RestoreLoad(float remaining) => RestoreLiveClock(remaining);

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (!_kit.IsFrostbiteLoaded) DurationRemaining = 0.0f;
            }

            protected override void OnEnd(AbilityContext ctx) => _kit.IsFrostbiteLoaded = false;
            protected override void OnCancelled(AbilityContext ctx) => _kit.IsFrostbiteLoaded = false;
        }

        // ================================================================== GLACIAL WALL (defending)

        private sealed class GlacialWall : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.HostConfirmed;

            public GlacialWall()
                : base("cheska_skill2d", "GLACIAL WALL",
                       "Defending. Hold to aim, release to raise an arc of icicles. Bodies and slippers stop at it; three slipper hits shatter it, or it expires after 10 s.",
                       CryoRules.GlacialWallCooldown, 0.0f, AbilityGlyph.CheskaBarricade,
                       summary: "Raise an icicle wall. Three slipper hits break it.",
                       telegraphRadius: CryoRules.GlacialWallArcLength * 0.5f, telegraphRange: CryoRules.GlacialWallMaxRange,
                       castAction: "hero-cheska-raise", viewmodelAction: "raise-barricade",
                       castCue: "sfx_cast_cheska_glacialwall")
            {
                AimByHolding(CryoRules.GlacialWallMinRange, CryoRules.GlacialWallMaxRange, rampSeconds: 1.0f, maxHoldSeconds: 0.0f);
                TelegraphStyle = GroundReticle.Style.Frost;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Squash(0.045f);
                // ⚠️ The barricade's body, spread to the arc's length (its three pillars measure 2.35 m
                // across at span 1). The curved arc itself is presentation work (plan § 5, TODO ABILITY-2).
                var wall = HeroHazards.SpawnIceBarricade(AimedDestination(ctx), ctx.Forward, CryoRules.GlacialWallSeconds,
                                                         arcLength: CryoRules.GlacialWallArcLength, arcRadius: CryoRules.GlacialWallArcRadius);
                var comp = wall != null ? wall.GetComponent<HeroHazards.IceBarricadeComponent>() : null;
                if (comp != null) comp.HitsToShatter = CryoRules.GlacialWallHits;
            }
        }

        // ================================================================== ABSOLUTE ZERO (ultimate)

        private sealed class AbsoluteZero : HeroAbility
        {
            public override AbilityNetworkMode NetworkMode => AbilityNetworkMode.SharedUltimate;
            // Her cutscene ends on everyone frozen, so the freeze lands as play resumes (`HeroAbility.IntroductionIsTheWindup`).
            // `CryoRules.AbsoluteZeroDelay` is still the wind-up of a cast that had no cutscene.
            protected override bool IntroductionIsTheWindup => true;
            public AbsoluteZero()
                : base("cheska_ultimate", "ABSOLUTE ZERO",
                       "Every other player is Frozen for 2.5 s, then Chilled for 5 s as they thaw.",
                       0.0f, 0.0f, AbilityGlyph.CheskaNova,
                       summary: "Freeze every player on the map, then chill them.",
                       castAction: "hero-cheska-nova", viewmodelAction: "nova-burst",
                       castCue: "sfx_cast_cheska_absolutezero")
            {
                TelegraphStyle = GroundReticle.Style.Frost;
                Windup = CryoRules.AbsoluteZeroDelay;
                SupportsPendingSnapshot = true;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_cheska_ult", ctx.Position);
                ctx.Motor.GetComponent<CharacterSquashStretch>()?.Stretch(0.065f);
                if (UnityEngine.Camera.main != null)
                    UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>()?.Shake(0.5f, 0.25f);

                var round = ctx.Round;
                if (round != null && NetAuthority.ShouldResolve())
                {
                    foreach (var p in round.Players)
                    {
                        if (p == null || p == ctx.Motor) continue;
                        // Frozen now; Chilled for the 5 s after the thaw (the timer runs through the
                        // freeze, where a slow changes nothing, so it is the thaw's 5 s exactly).
                        p.ApplyStagger(StatusRules.FrozenSeconds, StunElement.Ice, 9);
                        p.ApplyChilled(StatusRules.FrozenSeconds + StatusRules.ChilledSeconds);
                        HeroHazards.SpawnIceCubePrison(p.transform, StatusRules.FrozenSeconds);
                        MatchFlair.Announce(MatchFlair.Kind.UltimateImpact, ctx.Motor.PlayerSlot, p.PlayerSlot, ctx.Position);
                    }
                }
                FrostSurfacePresentation.Nova(ctx.Position, 4.6f);
            }
        }
    }
}
