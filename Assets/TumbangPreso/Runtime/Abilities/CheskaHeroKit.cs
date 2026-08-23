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
            public PermafrostSheetAbility()
                : base("cheska_skill1", "PERMAFROST SHEET", "ACTIVATE to coat the ground in slippery frost. Forces enemies to lose traction and slide uncontrollably, creating an opening to grab slippers or escape.", 7.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.Zone)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_cheska_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ice_freeze", ctx.Position);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.DashStretch(ctx.Forward, 0.25f);

                Vector3 target = ctx.Position + ctx.Forward * 2.8f;
                HeroHazards.SpawnIceSheet(target, 2.3f, 5.0f, ctx.Motor.PlayerSlot);
                ComicPopup.Spawn(target, "SLIP ZONE!", UiTheme.HeroIceBright, 1.25f);
            }
        }

        private sealed class IceBarricadeAbility : HeroAbility
        {
            public IceBarricadeAbility()
                : base("cheska_skill2", "ICE BARRICADE", "DEPLOY 3 crystalline ice pillars in front of you. Blocks enemy charge paths, deflects thrown slippers, and guards the lata from attackers.", 9.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.Wall)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("sfx_ice_freeze", ctx.Position);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Squash(0.2f);

                Vector3 wallPos = ctx.Position + ctx.Forward * 2.2f;
                HeroHazards.SpawnIceBarricade(wallPos, ctx.Forward, 3.2f);
            }
        }

        private sealed class GlacialShatterBurstAbility : HeroAbility
        {
            public GlacialShatterBurstAbility()
                : base("cheska_ultimate", "GLACIAL BLIZZARD NOVA", "UNLEASH a massive 4.6m frost shockwave. Instantly freezes all nearby opponents for 2.5s and violently deflects all slippers away from the zone.", 0.0f, 0.0f, TumbangPreso.UI.AbilityGlyph.Burst)
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
                            DizzyStars.Attach(p.transform, 2.5f, UiTheme.HeroIceBright);
                            ComicPopup.Freeze(p.transform.position);
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
