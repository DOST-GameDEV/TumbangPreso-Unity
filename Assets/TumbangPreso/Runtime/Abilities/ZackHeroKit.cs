using System;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed class ZackHeroKit : HeroKit
    {
        public bool IsOverchargeThrowActive { get; set; }
        public bool IsThunderstrikeActive => Ultimate != null && Ultimate.IsActive;

        public ZackHeroKit() : base("zack", "ZACK")
        {
            Skill1 = new StaticRailGrindAbility(this);
            Skill2 = new OverchargeThrowAbility(this);
            Ultimate = new ThunderstrikeOverdriveAbility(this);
        }

        private sealed class StaticRailGrindAbility : HeroAbility
        {
            private readonly ZackHeroKit _kit;
            private float _trailDropTimer;

            public StaticRailGrindAbility(ZackHeroKit kit)
                : base("zack_skill1", "STATIC RAIL-GRIND", "High-speed electric dash leaving a shock trail.", 6.0f, 2.5f, TumbangPreso.UI.AbilityGlyph.Dash)
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                Vector3 forward = ctx.Forward;
                forward.y = 0.0f;

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.DashStretch(forward, 0.3f);

                ctx.Motor.ApplyImpulse(forward.normalized * 12.0f);

                GameServices.Audio?.PlayAt("hero_zack_grunt", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_lightning_strike", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "RAIL GRIND!", UiTheme.HeroElectricBright, 1.2f);
                HeroHazards.SpawnShockTrail(ctx.Position, 2.2f, 3.5f, ctx.Motor.PlayerSlot);
                _trailDropTimer = 0.25f;
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Speed boost during rail grind
                ctx.Motor.ApplyImpulse(ctx.Forward * 4.0f * dt);

                _trailDropTimer -= dt;
                if (_trailDropTimer <= 0.0f)
                {
                    _trailDropTimer = 0.30f;
                    HeroHazards.SpawnShockTrail(ctx.Position, 2.0f, 3.0f, ctx.Motor.PlayerSlot);
                }
            }
        }

        private sealed class OverchargeThrowAbility : HeroAbility
        {
            private readonly ZackHeroKit _kit;

            public OverchargeThrowAbility(ZackHeroKit kit)
                : base("zack_skill2", "OVERCHARGE THROW", "Electrifies next throw for high velocity & chain lightning.", 8.0f, 10.0f, TumbangPreso.UI.AbilityGlyph.Empower)
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.IsOverchargeThrowActive = true;
                GameServices.Audio?.PlayAt("sfx_lightning_strike", ctx.Position);
                ComicPopup.Spawn(ctx.Position, "OVERCHARGE!", UiTheme.HeroElectricBright, 1.25f);
            }

            protected override void OnEnd(AbilityContext ctx)
            {
                _kit.IsOverchargeThrowActive = false;
            }
        }

        private sealed class ThunderstrikeOverdriveAbility : HeroAbility
        {
            private readonly ZackHeroKit _kit;

            public ThunderstrikeOverdriveAbility(ZackHeroKit kit)
                : base("zack_ultimate", "THUNDERSTRIKE OVERDRIVE", "Sky lightning strike and turbo overvoltage state.", 0.0f, 7.0f, TumbangPreso.UI.AbilityGlyph.Slam)
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_zack_ult", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_lightning_strike", ctx.Position);
                HeroHazards.CreateThunderstrike(ctx.Position, 7.0f, ctx.Motor.PlayerSlot);
                ComicPopup.Spawn(ctx.Position, "THUNDERSTRIKE!", UiTheme.HeroElectricBright, 1.5f);

                var squash = ctx.Motor.GetComponent<CharacterSquashStretch>();
                if (squash != null) squash.Stretch(0.4f);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                ctx.Motor.ApplyImpulse(ctx.Forward * 5.5f * dt);
            }
        }
    }
}
