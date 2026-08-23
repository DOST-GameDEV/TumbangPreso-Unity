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

        private sealed class PermafrostSheetAbility : HeroAbility
        {
            // ⚠️ THE TELEGRAPH NUMBERS ARE THE SPAWN NUMBERS. 2.3 m is the radius handed to
            // `SpawnIceSheet` below and 2.8 m is how far in front it is placed. They are the
            // same measurement written twice, so `TelegraphsMatchWhatTheAbilityPlaces` asserts
            // the pair rather than trusting either.
            public PermafrostSheetAbility()
                : base("cheska_skill1", "PERMAFROST SHEET",
                       "Freezes a patch of court in front of you. Anyone who runs across it loses their footing and slides.",
                       7.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.Zone,
                       summary: "Frost patch ahead. Whoever crosses it slides.",
                       telegraphRadius: 2.3f, telegraphRange: 2.8f,
                       castAction: "hero-cheska-frostwave",
                       viewmodelAction: "frost-sweep")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_cheska_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ice_freeze", ctx.Position);

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
                : base("cheska_skill2", "ICE BARRICADE",
                       "Raises three ice pillars in front of you. Bodies and thrown tsinelas both stop at them, so the lata gets time.",
                       9.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.Wall,
                       summary: "Three ice pillars ahead. Bodies and tsinelas stop at them.",
                       telegraphRadius: 1.6f, telegraphRange: 2.2f,
                       castAction: "hero-cheska-raise",
                       viewmodelAction: "raise-barricade")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("sfx_ice_freeze", ctx.Position);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Squash(0.2f);

                Vector3 wallPos = ctx.Position + ctx.Forward * 2.2f;

                // ⚠️⚠️ THE 3.2 IS A DURATION IN SECONDS AND IT USED TO BE PASSED AS IF IT WERE A
                // RADIUS. `SpawnIceBarricade(position, forward, duration = 6.0f)` has no radius
                // parameter at all, so a calibration pass that meant "make the footprint 3.2 m"
                // silently halved how long the wall stands and left the footprint at its
                // default. Named now so the next reader cannot make the same mistake.
                //
                // ⚠️ AND THE 3.2 IS KEPT RATHER THAN RESTORED TO 6.0, on the balance rather than
                // on the history: the skill cools in 9 s, so a 6 s wall is up two thirds of
                // every cycle in front of a lata that only has to survive 90 s. It wants a real
                // measured pass against `BotBehaviourProbe`; `docs/TODO.md` carries it.
                HeroHazards.SpawnIceBarricade(wallPos, ctx.Forward, duration: 3.2f);
            }
        }

        private sealed class GlacialShatterBurstAbility : HeroAbility
        {
            public GlacialShatterBurstAbility()
                : base("cheska_ultimate", "GLACIAL NOVA",
                       "Freezes everyone standing near you and blows the loose tsinelas away. Your way out when the whole court is on you.",
                       0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.Burst,
                       summary: "Freezes everyone near you and clears the tsinelas away.",
                       telegraphRadius: 4.6f, telegraphRange: 0.0f,
                       castAction: "hero-cheska-nova",
                       viewmodelAction: "nova-burst")
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_cheska_ult", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ice_freeze", ctx.Position);
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
                            p.ApplyStagger(2.5f);
                            p.ApplyImpulse(diff.normalized * 8.5f + Vector3.up * 2.5f);
                            // ⚠️⚠️ NO PER-VICTIM CALLOUT. Three frozen players used to mean
                            // three FREEZE callouts stacked on top of the nova's own, which is
                            // four lines of text in one frame saying one thing. The ice prison
                            // and the dizzy stars are already on each victim and they are
                            // readable from any angle; the words are not.
                            DizzyStars.Attach(p.transform, 2.5f, UiTheme.HeroIceBright);
                            HeroHazards.SpawnIceCubePrison(p.transform, 2.5f);
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
