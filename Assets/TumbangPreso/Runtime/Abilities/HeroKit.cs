using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Holds the three abilities for a hero (Skill 1, Skill 2, Ultimate) and manages the ultimate meter.
    /// </summary>
    public class HeroKit
    {
        public string HeroId { get; }
        public string HeroName { get; }
        public HeroAbility Skill1 { get; protected set; }
        public HeroAbility Skill2 { get; protected set; }
        public HeroAbility Ultimate { get; protected set; }

        public float UltimateCharge { get; protected set; }
        public const float UltimateMax = 100.0f;
        public float UltimateRatio => Mathf.Clamp01(UltimateCharge / UltimateMax);
        public bool IsUltimateReady => UltimateCharge >= UltimateMax && (Ultimate == null || Ultimate.IsReady);

        public HeroKit(string heroId, string heroName)
        {
            HeroId = heroId;
            HeroName = heroName;
            UltimateCharge = 0.0f;
        }

        public void AddUltimateCharge(float amount)
        {
            UltimateCharge = Mathf.Clamp(UltimateCharge + amount, 0.0f, UltimateMax);
        }

        public virtual void Tick(AbilityContext ctx, float dt)
        {
            // Objective play is the main source of charge. The slow passive trickle
            // prevents a dry round without rewarding ability spam or hiding.
            if (UltimateCharge < UltimateMax)
            {
                AddUltimateCharge(Balance.UltimatePassiveChargePerSecond * dt);
            }

            Skill1?.Tick(ctx, dt);
            Skill2?.Tick(ctx, dt);
            Ultimate?.Tick(ctx, dt);
        }

        public virtual bool TryActivateSkill1(AbilityContext ctx)
        {
            if (Skill1 == null) return false;
            if (Skill1.IsActive && Skill1.CanReactivate)
            {
                Skill1.Reactivate(ctx);
                return true;
            }
            if (Skill1.CanActivate(ctx))
            {
                Skill1.Activate(ctx);
                return true;
            }
            return false;
        }

        public virtual bool TryActivateSkill2(AbilityContext ctx)
        {
            if (Skill2 == null) return false;
            if (Skill2.IsActive && Skill2.CanReactivate)
            {
                Skill2.Reactivate(ctx);
                return true;
            }
            if (Skill2.CanActivate(ctx))
            {
                Skill2.Activate(ctx);
                return true;
            }
            return false;
        }

        public virtual bool TryActivateUltimate(AbilityContext ctx)
        {
            if (IsUltimateReady && Ultimate != null && Ultimate.CanActivate(ctx))
            {
                Ultimate.Activate(ctx);
                UltimateCharge = 0.0f;
                return true;
            }
            return false;
        }

        public virtual void Reset()
        {
            UltimateCharge = 0.0f;
            Skill1?.Reset();
            Skill2?.Reset();
            Ultimate?.Reset();
        }
    }
}
