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
                : base("zack_skill1", "BOLT SPRINT",
                       "Overcharges your skates. You move faster, and the trail you leave behind shocks anyone chasing you.",
                       6.0f, 2.5f, TumbangPreso.UI.AbilityGlyph.Dash,
                       summary: "Move faster, and shock whoever chases your trail.",
                       castAction: "hero-zack-sprint",
                       viewmodelAction: "sprint-electric")
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
                ComicPopup.Spawn(ctx.Position, "RAIL GRIND!", UiTheme.HeroElectricBright, 1.25f);
                HeroHazards.SpawnShockTrail(ctx.Position, 1.8f, 3.0f, ctx.Motor.PlayerSlot);
                _trailDropTimer = 0.25f;

                // ⚠️ THE SPARKS GO ON ZACK, NOT ON THE TRAIL DISCS. One dash drops up to thirty
                // of those, and thirty looping emitters is a different bug from the one this is
                // for. One aura on the body reads as speed and costs one system.
                Visual.AbilityVfx.AttachAura(ctx.Motor.transform,
                                             Visual.AbilityVfx.Aura.ElectricSpark, Duration);
            }

            protected override void OnTick(AbilityContext ctx, float dt)
            {
                // Speed boost during rail grind
                ctx.Motor.ApplyImpulse(ctx.Forward * 4.0f * dt);

                _trailDropTimer -= dt;
                if (_trailDropTimer <= 0.0f)
                {
                    _trailDropTimer = 0.30f;
                    HeroHazards.SpawnShockTrail(ctx.Position, 1.8f, 3.0f, ctx.Motor.PlayerSlot);
                }
            }
        }

        private sealed class OverchargeThrowAbility : HeroAbility
        {
            private readonly ZackHeroKit _kit;

            public OverchargeThrowAbility(ZackHeroKit kit)
                : base("zack_skill2", "STATIC CHARGE",
                       "Charges your next throw. It flies much faster and shocks the court where it lands.",
                       8.0f, 10.0f, TumbangPreso.UI.AbilityGlyph.Empower,
                       summary: "Your next throw flies faster and shocks where it lands.",
                       castAction: "hero-zack-charge",
                       viewmodelAction: "overcharge")
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                _kit.IsOverchargeThrowActive = true;
                Visual.AbilityVfx.AttachHandVfx(ctx.Motor.transform, Visual.AbilityVfx.Aura.ElectricSpark, Duration);
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
                : base("zack_ultimate", "THUNDERSTRIKE",
                       "Calls lightning down on where you stand. Everyone caught underneath is stunned where they are.",
                       0.0f, 7.0f, TumbangPreso.UI.AbilityGlyph.Slam,
                       summary: "Lightning on your position. Stuns everyone it catches.",
                       telegraphRadius: 4.5f, telegraphRange: 0.0f,
                       castAction: "hero-zack-summon",
                       viewmodelAction: "summon-lightning")
            {
                _kit = kit;
            }

            protected override void OnActivate(AbilityContext ctx)
            {
                GameServices.Audio?.PlayAt("hero_zack_ult", ctx.Position);
                GameServices.Audio?.PlayAt("sfx_lightning_strike", ctx.Position);
                HeroHazards.CreateThunderstrike(ctx.Position, 4.5f, ctx.Motor.PlayerSlot);
                Visual.AbilityVfx.SpawnElectricArcs(ctx.Position, 4.5f);
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
