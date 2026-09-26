using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ PAETE, THE NINTH HERO (owner, 2026-09-25): the forest on Mount Makiling, a plant (Dendro)
    /// kit on the signature plus role shape. The owner's table, with *"all skill names are to be made
    /// better but here are the concepts"*:
    ///
    /// | Slot | Name | Owner's table |
    /// |---|---|---|
    /// | Signature | KAPIT-BAGING (Vine Pull) | His arms extend in sync toward a direction and he flies to it, a web swing. *"i want it to be an escape"*. |
    /// | Attacking | PUNLANG TSINELAS (Throwing Slipper Plant) | A plant that grows wooden slippers and shoots them on command, 10 to 20 s apart. |
    /// | Defending | BAWI (Thorn Pull) | A construct that reaches every player and pulls their slippers to it, *"even the ones on the hands"*. |
    /// | Ultimate | YAKAP NG MAKILING (Nature's Wrath) | A sentry that pulls everyone within 9 m to it, Rooted, hold Interact 7 s to break free; they can still throw and cast. |
    ///
    /// ⚠️ RENAMED 2026-09-26 (owner: *"u can change name and description of all his skills to make it all sound
    /// better"*): LIANA LEAP, BAKYA BLOOM (bakya are the carved wooden clogs Paete's carvers are known for,
    /// which is what the sapling grows), THORN HARVEST and MAKILING'S EMBRACE (the translation of the old
    /// YAKAP NG MAKILING; the spirit of Mariang Makiling now watches over the cast). Ids are unchanged.
    ///
    /// Numbers: `Core.PaeteRules`. Design and the owner's answers:
    /// `docs/reports/paete-kit-2026-09-25/plan.md`; research: `research.md` beside it; the six-beat
    /// look, sound and motion follow `docs/reports/amihan-kit-2026-09-25/direction.md`.
    ///
    /// ⚠️ HOST AUTHORITY: every slipper moved, every carry on another body, every root and every
    /// score is behind `NetAuthority.ShouldResolve()` (`tools/audit_ability_authority.py`); a peer
    /// moves only its own body (the vine reel), as the other kits do.
    /// </summary>
    public sealed class PaeteHeroKit : HeroKit
    {
        public override float UltimateCost => PaeteRules.SentryCost;

        public PaeteHeroKit() : base("paete", "PAETE")
        {
            Skill1 = new KapitBaging();
            AttackingSkill = new PunlangTsinelas();
            DefendingSkill = new Bawi();
            Ultimate = new YakapNgMakiling();
        }

        // ================================================================== LIANA LEAP (signature)

        private sealed class KapitBaging : HeroAbility
        {
            private CharacterMotor _caster;
            private Vector3 _anchor;
            private float _elapsed;
            private bool _reeled;

            public KapitBaging()
                : base("paete_skill1", "LIANA LEAP",
                       "Vines lash from both arms to where you aim and haul you after them. An escape that works with a slipper in hand.",
                       PaeteRules.VineCooldown, 0.0f, AbilityGlyph.PaeteVine,
                       summary: "Swing to where you aim on vines from both arms.",
                       castAction: "hero-paete-vine", viewmodelAction: "vine-reach",
                       castCue: "sfx_cast_paete_vine")
            {
                // The tell: both arms draw back and the palms glow (research.md § 3). A short
                // wind-up is also the counterplay window for an escape tool.
                Windup = PaeteRules.VineTellSeconds;
                Duration = PaeteRules.VineReachSeconds + PaeteRules.VineHoldSeconds(PaeteRules.VineRange) + 0.35f;
            }

            public override void Activate(AbilityContext ctx)
            {
                base.Activate(ctx);
                if (IsWindingUp && ctx?.Motor != null) PaetePalmGlow.Attach(ctx.Motor, PaeteRules.VineTellSeconds);
            }

            public override bool CanActivate(AbilityContext ctx)
            {
                if (!base.CanActivate(ctx)) return false;
                // A rooted body cannot swing out of the sentry, and a flying one has nowhere to reel.
                // ⚠️ NO SLIPPER GUARD, UNLIKE UPDRAFT: the owner, 2026-09-25, *"i want it to be an
                // escape"*. The 30 s cooldown and the 0.12 s tell are its price.
                return !ctx.Motor.IsRooted && !ctx.Motor.IsFlying;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                _caster = ctx.Motor;
                _elapsed = 0.0f;
                _reeled = false;
                _anchor = PaeteVine.FindAnchor(ctx.Position, ctx.Forward, ctx.AimPoint);
                float distance = Flat(_anchor - ctx.Position).magnitude;
                float reel = PaeteRules.VineHoldSeconds(distance) + PaeteRules.VineReelSpeed / (2f * Balance.Friction);
                PaeteVineReach.Build(_caster, _anchor, PaeteRules.VineReachSeconds, reel);
                _caster.GetComponentInChildren<CharacterSquashStretch>()?.Stretch(0.14f);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                if (_caster == null || _reeled) return;
                _elapsed += dt;
                if (_elapsed < PaeteRules.VineReachSeconds) return;
                _reeled = true;
                // His own body: the owner simulates it (BeginCarry refuses anywhere else).
                Vector3 d = Flat(_anchor - _caster.transform.position);
                if (d.magnitude <= PaeteRules.VineStopShort) return;
                _caster.BeginCarry(d.normalized * PaeteRules.VineReelSpeed + Vector3.up * PaeteRules.VineLift,
                                   PaeteRules.VineHoldSeconds(d.magnitude));
            }

            protected override void OnEnd(AbilityContext ctx) { _caster = null; }

            private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0.0f, v.z);
        }

        // ================================================================== BAKYA BLOOM (attacking)

        private sealed class PunlangTsinelas : HeroAbility
        {
            // The seedling is a world object: the owner waits for the host before planting it, as
            // Cheska's barricade and Amihan's gale do.
            public override bool DefersPredictedEffect => true;

            public PunlangTsinelas()
                : base("paete_skill2", "BAKYA BLOOM",
                       "Attacking. Plant a sapling that grows bakya, wooden slippers. Press again: it hurls one where you aim. Uprootable after 15 s.",
                       PaeteRules.PlantCooldown, PaeteRules.PlantLifeSeconds, AbilityGlyph.PaeteSprout,
                       summary: "A sapling grows wooden slippers and throws them for you.",
                       telegraphRadius: 0.6f, telegraphRange: PaeteRules.PlantThrowRange,
                       castAction: "hero-paete-sprout", viewmodelAction: "seed-toss",
                       castCue: "sfx_cast_paete_sprout") { }

            // The second press is the command to throw (owner: *"he can control when they shoot"*).
            public override bool CanReactivate => true;

            // ⚠️⚠️ TWO PRESSES, TWO BODIES (owner, 2026-09-26: *"i want each of his skill to have their
            // own animation"*). The first press is the planting lob; the second is a COMMAND, a
            // sharp point at the target while the seedling does the throwing, so it gets its own
            // body clip, first-person gesture and click rather than replaying the lob.
            // `HeroAbilitySystem.PlayCastConfirm` reads these AFTER `Activate`/`Reactivate` return,
            // on the owner and on every observer (`ApplyNetworkCast` takes the same path).
            private const string PlantAction = "hero-paete-sprout", CommandAction = "hero-paete-command";

            private int _ownerSlot = -1;

            public override void Activate(AbilityContext ctx)
            {
                CastAction = PlantAction; ViewmodelAction = "seed-toss"; CastCue = "sfx_cast_paete_sprout";
                if (ctx?.Motor != null) _ownerSlot = ctx.Motor.PlayerSlot;
                base.Activate(ctx);
            }

            // ⚠️⚠️ THE COMMAND WAITS FOR A CLOG (owner, 2026-09-26: *"bug found unli cast for e, supposed to have cooldown"*). The
            // pod only ever threw a grown clog (`PaetePlant.Fire` checks `ShotReady`, 15 s between clogs), but every press while it
            // lived played the command gesture and its sound, so spamming E looked and sounded like casting without a cooldown.
            // Now a press with no clog is refused as NOT YET (`HeroAbility.ReactivateReady`), and the deck counts down to the next
            // clog instead of saying "Again". With no plant left the press still ends the ability, as before.
            public override bool ReactivateReady
            {
                get { var plant = PaetePlant.OwnedBy(_ownerSlot); return plant == null || plant.ShotReady; }
            }

            public override float ReactivateReadyIn
            {
                get { var plant = PaetePlant.OwnedBy(_ownerSlot); return plant == null ? 0f : plant.ShotIn; }
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                // Outside the taya's box only (owner, 2026-09-26: *"also make paete's E only placeable outside box"*).
                Vector3 at = PaeteVine.PlantTarget(ctx.Position, ctx.Forward, ctx.AimPoint);
                PaetePlant.Spawn(ctx.Position + Vector3.up * 1.2f, at, ctx.Motor.PlayerSlot);
            }

            public override void Reactivate(AbilityContext ctx)
            {
                // ⚠️ NOT `EndEarly`: a second press fires, it does not dig the plant up.
                var plant = ctx?.Motor != null ? PaetePlant.OwnedBy(ctx.Motor.PlayerSlot) : null;
                if (plant == null) { EndEarly(ctx); return; }
                CastAction = CommandAction; ViewmodelAction = "seed-command"; CastCue = "sfx_cast_paete_command";
                plant.Fire(ctx.AimPoint);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // The ability is live as long as its plant is: pulled or withered, it is over.
                if (ctx?.Motor != null && PaetePlant.OwnedBy(ctx.Motor.PlayerSlot) == null && DurationRemaining < PaeteRules.PlantLifeSeconds - 1f)
                    DurationRemaining = 0.0f;
            }

            public override void Reset()
            {
                foreach (var p in PaetePlant.Live.ToArray()) if (p != null) Object.Destroy(p.gameObject);
                base.Reset();
            }
        }

        // ================================================================== THORN HARVEST (defending)

        private sealed class Bawi : HeroAbility
        {
            public override bool DefersPredictedEffect => true;

            public Bawi()
                : base("paete_skill2d", "THORN HARVEST",
                       "Defending. Hold to place it: thorns race there, snatch every slipper within 7 m, even out of hands, and drag them in.",
                       PaeteRules.ThornCooldown, 0.0f, AbilityGlyph.PaeteThorn,
                       summary: "Placed thorns snatch every slipper near, even from hands.",
                       // The ring is the construct itself, not its 7 m reach: the reach is caught in one frame and
                       // leaves nothing on the ground for a bot to path round (`AiTuning.HazardAvoidMaxRadius`).
                       telegraphRadius: 0.8f, telegraphRange: 0.0f,
                       castAction: "hero-paete-thorns", viewmodelAction: "thorn-stamp",
                       castCue: "sfx_cast_paete_thorns")
            {
                // ⚠️⚠️ PLACED, NOT ON HIS BODY (owner, 2026-09-26: *"I WANT IT to be castable and not cast on body make it possible
                // for him to place it somewhere else like his ult and other skill"*). The same hold-to-aim as his ultimate: the ring
                // shows where he looks while held, release casts, the commit carries the spot (`HeroAbility.AimsWhereLooking`),
                // from his own feet out to `PaeteRules.ThornAimRange`.
                AimByHolding(PaeteRules.ThornAimMinRange, PaeteRules.ThornAimRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f, whereLooking: true);
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                // He still stamps: the thorns leave from his foot and race through the court to the spot (`PaeteThorns`).
                Vector3 at = AimedDestination(ctx);
                at.y = ctx.Position.y;
                PaeteThorns.Spawn(at, ctx.Position, ctx.Motor.PlayerSlot);
                ctx.Motor.GetComponentInChildren<CharacterSquashStretch>()?.Squash(0.12f);
            }
        }

        // ================================================================== MAKILING'S EMBRACE (ultimate)

        private sealed class YakapNgMakiling : HeroAbility
        {
            public YakapNgMakiling()
                : base("paete_ultimate", "MAKILING'S EMBRACE",
                       "Hold to choose where, release: a giant guardian tree comes up there and drags everyone in 9 m into its roots. They can still throw. Hold Interact 7 s to escape.",
                       0.0f, 0.0f, AbilityGlyph.PaeteSentry,
                       summary: "Hold to place it: a guardian tree drags everyone in 9 m into its roots.",
                       telegraphRadius: PaeteRules.SentryRadius, telegraphRange: PaeteRules.SentryThrowRange,
                       castAction: "hero-paete-sentry", viewmodelAction: "ground-call",
                       castCue: "sfx_cast_paete_sentry")
            {
                // ⚠️ HE CHOOSES WHERE IT COMES UP (owner, 2026-09-27: *"make it so that paete can choose as well where his ult will be
                // cast"*, then *"does the hhold to aim resemble the groot reference"*). Groot's walls are placed where he looks, with the
                // preview on the ground first; so this is the shared hold-to-aim (the 9 m ring shows while held, release casts) placed
                // WHERE HE LOOKS, between 3 m and the 8 m throw range (`HeroAbility.AimsWhereLooking`), not slid out by hold time. The
                // accepted commit carries the spot, so every peer and the cutscene compute the same place.
                AimByHolding(PaeteRules.SentryAimMinRange, PaeteRules.SentryThrowRange, rampSeconds: 0.55f, maxHoldSeconds: 0.0f, whereLooking: true);
            }

            /// <summary>
            /// ⚠️⚠️ v5, CALLED FROM THE GROUND IN PLAY TOO (owner, 2026-09-26 night: *"i also dont like that paete just throws seeds in
            /// his ult"*; direction.md 5.14). Nothing leaves his hand: he is down on his knee with his hands in the court (the cutscene's
            /// last pose, and the live clip's first), his roots race from between his hands to the spot, and the tree crawls out
            /// there. `PaeteGroundCall` keeps his body joined to the ground meanwhile; the timing, the reach and the catch are
            /// `PaeteSentry`'s, unchanged.
            /// </summary>
            protected override void OnActivate(AbilityContext ctx)
            {
                if (ctx?.Motor == null) return;
                // ⚠️ Never on the can (owner, 2026-09-27: *"dont let it be placed in a place it STANDS on can"*).
                // Where he chose (the ring he held), still never on the can.
                Vector3 at = PaeteVine.SentryTarget(ctx.Position, ctx.Forward, AimedDestination(ctx), ctx.Round?.Lata ?? GameServices.Round?.Lata);
                Vector3 hands = PaeteGroundCall.Begin(ctx.Motor, at);
                // ⚠️ PLAY PICKS UP WHERE THE CUTSCENE ENDS (owner, 2026-09-27: after the cutscene started showing the catch, the tree grew
                // and caught everyone a second time in play; asked, he chose *"Yes, no repeat"*). It comes up already standing and awake
                // and catches at once (`PaeteSentry.Spawn`'s hand-back).
                PaeteSentry.Spawn(hands, at, ctx.Motor.PlayerSlot, handBack: true);
            }

            public override void Reset()
            {
                foreach (var s in Object.FindObjectsByType<PaeteSentry>()) if (s != null) Object.Destroy(s.gameObject);
                base.Reset();
            }
        }
    }
}
