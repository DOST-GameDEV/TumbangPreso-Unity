using System;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Base class for hero abilities and ultimates.
    /// </summary>
    public abstract class HeroAbility
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public float Cooldown { get; protected set; }
        public float Duration { get; protected set; }

        public float CooldownRemaining { get; protected set; }
        public float DurationRemaining { get; protected set; }
        public bool IsActive => DurationRemaining > 0.0f;
        public bool IsReady => CooldownRemaining <= 0.0f;

        public float CooldownRatio => Cooldown > 0.0f ? Mathf.Clamp01(CooldownRemaining / Cooldown) : 0.0f;
        public float DurationRatio => Duration > 0.0f ? Mathf.Clamp01(DurationRemaining / Duration) : 0.0f;

        protected HeroAbility(string id, string name, string description, float cooldown, float duration = 0.0f)
        {
            Id = id;
            Name = name;
            Description = description;
            Cooldown = cooldown;
            Duration = duration;
        }

        public virtual bool CanActivate(AbilityContext ctx)
        {
            if (ctx == null || ctx.Motor == null) return false;
            if (!IsReady) return false;
            if (!ctx.Motor.CanAct()) return false;
            return true;
        }

        public virtual void Activate(AbilityContext ctx)
        {
            CooldownRemaining = Cooldown;
            DurationRemaining = Duration;
            OnActivate(ctx);
        }

        public virtual void Tick(AbilityContext ctx, float dt)
        {
            if (CooldownRemaining > 0.0f)
                CooldownRemaining = Mathf.Max(0.0f, CooldownRemaining - dt);

            if (DurationRemaining > 0.0f)
            {
                DurationRemaining = Mathf.Max(0.0f, DurationRemaining - dt);
                OnTick(ctx, dt);

                if (DurationRemaining <= 0.0f)
                    OnEnd(ctx);
            }
        }

        public virtual bool CanReactivate => false;

        public virtual void Reactivate(AbilityContext ctx)
        {
            EndEarly(ctx);
        }

        public void EndEarly(AbilityContext ctx)
        {
            if (DurationRemaining > 0.0f)
            {
                DurationRemaining = 0.0f;
                OnEnd(ctx);
            }
        }

        public virtual void Reset()
        {
            CooldownRemaining = 0.0f;
            DurationRemaining = 0.0f;
        }

        protected virtual void OnActivate(AbilityContext ctx) { }
        protected virtual void OnTick(AbilityContext ctx, float dt) { }
        protected virtual void OnEnd(AbilityContext ctx) { }
    }
}
