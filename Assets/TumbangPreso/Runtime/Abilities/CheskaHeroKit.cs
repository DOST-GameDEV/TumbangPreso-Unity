using System;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class CheskaHeroKit : HeroKit
    {
        public CheskaHeroKit() : base("cheska", "CHESKA")
        {
            Skill1 = new PermafrostSheetAbility();
            Skill2 = new IceBarricadeAbility();
            Ultimate = new GlacialShatterBurstAbility();
        }

        /// <summary>
        /// ⚠️ SECOND MOST EXPENSIVE, BECAUSE IT IS TWO ULTIMATES IN ONE PRESS. Glacial Nova
        /// freezes everyone within 4.6 m AND clears every loose tsinelas out to 4.8 m, so it is
        /// simultaneously an escape for a surrounded taya and a reset of the ammunition on the
        /// court. It goes off around the caster, so like Thunderstrike it cannot miss.
        ///
        /// It sits under Zack's 150 only because the freeze is a stagger rather than a full
        /// stun. 140 is 5.6 lata knockdowns. `docs/Hero_Strike_Balance.md` § 3.1.
        /// </summary>
        public override float UltimateCost => 140.0f;

        private sealed class PermafrostSheetAbility : HeroAbility
        {
            // ⚠️ THE TELEGRAPH NUMBERS ARE THE SPAWN NUMBERS. 2.3 m is the radius handed to
            // `SpawnIceSheet` below and 2.8 m is how far in front it is placed. They are the
            // same measurement written twice, so `TelegraphsMatchWhatTheAbilityPlaces` asserts
            // the pair rather than trusting either.
            public PermafrostSheetAbility()
                // ⚠️⚠️ TWO CHARGES A ROUND AND NO RECHARGE, DOWN FROM 12.8 CASTS OFF A 7 s
                // COOLDOWN. It places a zone, so it is on the charge half of the split, and it
                // is one of the abilities meant to run out: Cheska has to decide WHICH two
                // approaches to the lata she is closing this round.
                //
                // ⚠️ THE RADIUS IS UNCHANGED AT 2.3 AND THAT IS DELIBERATE. It already sits
                // inside the 1.8 to 2.5 m budget. What was wrong with this power was never its
                // size but its RENDER: five overlapping translucent primitives, which is
                // `docs/VISION.md` § 2 rule 4 broken by one ability against itself. That is
                // fixed in `HeroHazards.SpawnIceSheet` rather than here.
                : base("cheska_skill1", "PERMAFROST SHEET",
                       "Freezes a patch of court in front of you. Anyone who runs across it loses their footing and slides.",
                       0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.CheskaFrostSheet,
                       summary: "Frost patch ahead. Whoever crosses it slides.",
                       telegraphRadius: 2.3f, telegraphRange: 2.8f,
                       castAction: "hero-cheska-frostwave",
                       viewmodelAction: "frost-sweep",
                       charges: 2)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                // ⚠️⚠️ `NetCue`, NOT `GameServices.Audio`, ACROSS ALL FIVE REMAINING KITS. Every
                // hero cast cue in the game is a WORLD event: a grunt, an element, an impact at a
                // point on the court that the other three players are meant to hear and locate.
                // `tools/audit_audio_reach.py` and `docs/TODO.md` § 25 have the audit; `NetCue`
                // is a no-op with no transport running, so nothing about the offline game, the
                // bot probes or the editor checks changes by this.
                //
                // ⚠️ THE ABILITY LAYER ITSELF IS STILL NOT REPLICATED (§ 25.1), so today this
                // buys nothing on its own: the cast does not reach another peer to make a sound
                // on. It is done now because the alternative is doing it later, from memory,
                // across five files, on the day the ability RPC lands.
                NetCue.Play("hero_cheska_grunt", ctx.Position);
                NetCue.Play("sfx_ice_freeze", ctx.Position);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.DashStretch(ctx.Forward, 0.25f);

                Vector3 target = ctx.Position + ctx.Forward * 2.8f;
                // ⚠️⚠️ ONE CALLOUT PER EVENT. This fired "SLIP ZONE!" and `SpawnIceSheet`
                // fired "SLIP & SLIDE!" at the same point on the same frame, saying the same
                // thing twice in two different phrasings. The hazard keeps its line because the
                // hazard is the thing that persists.
                HeroHazards.SpawnIceSheet(target, 2.3f, 5.0f, ctx.Motor.PlayerSlot);
            }
        }

        private sealed class IceBarricadeAbility : HeroAbility
        {
            // 1.6 m is the `HazardVolume` radius the barricade registers, which is the circle
            // bots steer around and therefore the honest footprint. The three pillars measure
            // 2.35 m across the face, so the ring reads as slightly tighter than the wall looks:
            // that is the right way round, because the ends of the wall are the gaps.
            public IceBarricadeAbility()
                // ⚠️⚠️ ONE CHARGE A ROUND, BACK ONE WHEN SHE RETRIEVES HER OWN TSINELAS. The
                // scarcest ability in the game, and it is the one that most deserves to be: a
                // wall in front of the lata closes a lane outright, and one per round makes
                // WHERE it goes the whole decision.
                //
                // ⚠️ THE RECHARGE PAYS THE ACT THE GAME IS BUILT AROUND. `docs/VISION.md` § 0:
                // *"The tension is the retrieval, not the throw."* Cheska gets her wall back by
                // going in and getting her slipper, which is the one moment she can be caught,
                // so the strongest defensive tool in the mode is refilled by taking the game's
                // central risk rather than by a timer. It is also the only recharge in the game
                // keyed to this event, which keeps it hers.
                : base("cheska_skill2", "ICE BARRICADE",
                       "Raises three ice pillars in front of you. Bodies and thrown tsinelas both stop at them, so the lata gets time.",
                       0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.CheskaBarricade,
                       summary: "Three ice pillars ahead. Bodies and tsinelas stop at them.",
                       telegraphRadius: 1.6f, telegraphRange: 2.2f,
                       castAction: "hero-cheska-raise",
                       viewmodelAction: "raise-barricade",
                       charges: 1,
                       rechargedBy: Recharge.OwnSlipperRetrieved)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("sfx_ice_freeze", ctx.Position);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Squash(0.2f);

                Vector3 wallPos = ctx.Position + ctx.Forward * 2.2f;

                // ⚠️⚠️ THE 3.2 IS A DURATION IN SECONDS AND IT USED TO BE PASSED AS IF IT WERE A
                // RADIUS. `SpawnIceBarricade(position, forward, duration = 6.0f)` has no radius
                // parameter at all, so a calibration pass that meant "make the footprint 3.2 m"
                // silently halved how long the wall stands and left the footprint at its
                // default. Named now so the next reader cannot make the same mistake.
                //
                // ✅ AND THE 3.2 IS NOW 6.0, WHICH CLOSES `docs/TODO.md` § 2 AS A CONSEQUENCE
                // RATHER THAN AS A MEASUREMENT. The whole argument for keeping 3.2 was that the
                // skill cooled in 9 s, so a 6 s wall stood for two thirds of every cycle in
                // front of a lata that only has to survive 90 s. **That premise is gone.** The
                // barricade is one charge per round now, refilled only by retrieving her own
                // tsinelas, so it is up for 6 s out of 90 rather than for 60 s out of 90.
                //
                // A wall you get ONCE has to be worth walking around, and 3.2 s is barely long
                // enough to cross the box. 6.0 s is what the signature always defaulted to and
                // what the ability was written against before the parameter mix-up on
                // 2026-08-23. `docs/Hero_Strike_Balance.md` § 3.2.
                HeroHazards.SpawnIceBarricade(wallPos, ctx.Forward, duration: 6.0f);
            }
        }

        private sealed class GlacialShatterBurstAbility : HeroAbility
        {
            public GlacialShatterBurstAbility()
                : base("cheska_ultimate", "GLACIAL NOVA",
                       "Freezes everyone standing near you and blows the loose tsinelas away. Your way out when the whole court is on you.",
                       0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.CheskaNova,
                       summary: "Freezes everyone near you and clears the tsinelas away.",
                       telegraphRadius: 4.6f, telegraphRange: 0.0f,
                       castAction: "hero-cheska-nova",
                       viewmodelAction: "nova-burst")
            {
                Windup = UltimateWindup;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                NetCue.Play("hero_cheska_ult", ctx.Position);

                // ⚠️ NOVA, NOT FREEZE, AND THE DIFFERENCE IS THE DIRECTION. `sfx_ice_freeze` is
                // ice FORMING, a rising chime, and it stays on Permafrost Sheet and the
                // Barricade where something is being built. This is ice BREAKING outward, so
                // `sfx_frost_nova` descends and leads with shards. Cheska's three abilities
                // fired one sound between them and the ultimate sounded like the skill.
                NetCue.Play("sfx_frost_nova", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "BLIZZARD NOVA!", UiTheme.HeroIceBright, 1.5f);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Stretch(0.35f);

                // Screen shake on main camera
                if (UnityEngine.Camera.main != null)
                {
                    var rig = UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>();
                    if (rig != null) rig.Shake(0.5f, 0.25f);
                }

                var round = ctx.Round;
                if (round != null)
                {
                    // Freeze all nearby opponents (balanced ultimate radius)
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == ctx.Motor.PlayerSlot) continue;

                        Vector3 diff = p.transform.position - ctx.Position;
                        diff.y = 0.0f;
                        if (diff.magnitude <= 4.6f)
                        {
                            // ⚠️ 9 PRESSES, THE MOST IN THE GAME. Per-skill mash cost was
                            // asked for as "dependent on how hard the skill is supposed to hit",
                            // and this is an ULTIMATE that freezes everyone it catches. It is
                            // also the one ability whose fiction and whose element are the same
                            // word, so if any stun should read as being encased, it is this.
                            p.ApplyStagger(2.5f, StunElement.Ice, 9);
                            p.ApplyImpulse(diff.normalized * 8.5f + Vector3.up * 2.5f);
                            // ⚠️⚠️ NO PER-VICTIM CALLOUT. Three frozen players used to mean
                            // three FREEZE callouts stacked on top of the nova's own, which is
                            // four lines of text in one frame saying one thing. The ice prison
                            // and the dizzy stars are already on each victim and they are
                            // readable from any angle; the words are not.
                            DizzyStars.Attach(p.transform, 2.5f, UiTheme.HeroIceBright);
                            HeroHazards.SpawnIceCubePrison(p.transform, 2.5f);
                            Visual.HitFeel.Land(p, Visual.HitFeel.Weight.Ultimate,
                                                UiTheme.HeroIceBright, ctx.Position);
                        }
                    }

                    // Deflect slippers away within ultimate blast
                    foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                    {
                        if (s != null)
                        {
                            Vector3 away = s.transform.position - ctx.Position;
                            away.y = 0.0f;
                            if (away.magnitude <= 4.8f)
                            {
                                s.Deflect(away.normalized * 19.0f + Vector3.up * 4.5f, 1.1f);
                            }
                        }
                    }
                }

                // Burst particles via AbilityVfx
                Visual.AbilityVfx.SpawnIceBurst(ctx.Position, 4.6f);
            }
        }
    }
}
