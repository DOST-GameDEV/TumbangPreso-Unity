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
                : base("cheska_skill1", "PERMAFROST SHEET", "Coats the ground in slippery ice, disrupting enemy footing.", 7.0f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_cheska_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_ice_freeze", ctx.Position);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.DashStretch(ctx.Forward, 0.25f);

                Vector3 target = ctx.Position + ctx.Forward * 3.5f;
                HeroHazards.SpawnIceSheet(target, 5.0f, 6.0f, ctx.Motor.PlayerSlot);
                ComicPopup.Spawn(target, "SLIP ZONE!", UiTheme.HeroIceBright, 1.2f);
            }
        }

        private sealed class IceBarricadeAbility : HeroAbility
        {
            public IceBarricadeAbility()
                : base("cheska_skill2", "ICE BARRICADE", "Deploys a crystalline ice wall to block attacks and routes.", 9.0f, 0.0f)
            {
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("sfx_ice_freeze", ctx.Position);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Squash(0.2f);

                Vector3 wallPos = ctx.Position + ctx.Forward * 2.4f;
                HeroHazards.SpawnIceBarricade(wallPos, ctx.Forward, 6.5f);
            }
        }

        private sealed class GlacialShatterBurstAbility : HeroAbility
        {
            public GlacialShatterBurstAbility()
                : base("cheska_ultimate", "GLACIAL BLIZZARD NOVA", "Freezes nearby enemies and violently deflects slippers.", 0.0f, 0.0f)
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
                    // Freeze all nearby opponents
                    foreach (var p in round.Players)
                    {
                        if (p == null || p.PlayerSlot == ctx.Motor.PlayerSlot) continue;

                        Vector3 diff = p.transform.position - ctx.Position;
                        diff.y = 0.0f;
                        if (diff.magnitude <= 7.5f)
                        {
                            p.ApplyStagger(2.5f);
                            p.ApplyImpulse(diff.normalized * 8.5f + Vector3.up * 2.5f);
                            DizzyStars.Attach(p.transform, 2.5f, UiTheme.HeroIceBright);
                            ComicPopup.Freeze(p.transform.position);
                        }
                    }

                    // Deflect slippers away
                    foreach (var s in UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                    {
                        if (s != null)
                        {
                            Vector3 away = s.transform.position - ctx.Position;
                            away.y = 0.0f;
                            if (away.magnitude <= 8.5f)
                            {
                                s.Deflect(away.normalized * 19.0f + Vector3.up * 4.5f, 1.1f);
                            }
                        }
                    }
                }

                // Spawn residual permafrost ring
                HeroHazards.SpawnIceSheet(ctx.Position, 6.5f, 5.0f, ctx.Motor.PlayerSlot);
            }
        }
    }
}
