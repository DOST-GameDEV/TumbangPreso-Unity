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

        /// <summary>
        /// What KIND of power this is, drawn as a shape wherever it is shown.
        ///
        /// ⚠⚠ IT LIVES ON THE ABILITY, NOT IN A LOOKUP TABLE IN THE HUD. A table keyed by
        /// ability id is a second place to forget: adding a hero would compile, run, and show
        /// three blank tiles. Here the compiler cannot let a new ability exist without one.
        /// </summary>
        public UI.AbilityGlyph Glyph { get; }
        public float Cooldown { get; protected set; }
        public float Duration { get; protected set; }

        public float CooldownRemaining { get; protected set; }
        public float DurationRemaining { get; protected set; }
        public bool IsActive => DurationRemaining > 0.0f;
        public bool IsReady => CooldownRemaining <= 0.0f;

        public float CooldownRatio => Cooldown > 0.0f ? Mathf.Clamp01(CooldownRemaining / Cooldown) : 0.0f;
        public float DurationRatio => Duration > 0.0f ? Mathf.Clamp01(DurationRemaining / Duration) : 0.0f;

        protected HeroAbility(string id, string name, string description, float cooldown,
                              float duration = 0.0f,
                              UI.AbilityGlyph glyph = UI.AbilityGlyph.Burst)
        {
            Id = id;
            Name = name;
            Description = description;
            Cooldown = cooldown;
            Duration = duration;
            Glyph = glyph;
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

        /// <summary>
        /// The round-boundary reset: end cleanly first, THEN zero.
        ///
        /// ⚠⚠ THE PARAMETERLESS `Reset` DROPS `DurationRemaining` WITHOUT RUNNING `OnEnd`,
        /// and for the abilities that grant something for a duration that is a leak, not a
        /// reset. Demonic Carapace hands out stun immunity in `OnActivate` and takes it back in
        /// `OnEnd`; Phantom Phase does the same for tag immunity. Zeroing the timer behind their
        /// backs at a round boundary leaves the grant switched on with no timer left to switch
        /// it off, so a hero who happened to be mid-Carapace when the round ended would start
        /// the next one permanently unstunnable.
        ///
        /// `EndEarly` is a no-op when nothing is running, so this is safe on every ability.
        /// </summary>
        public void ResetForRound(AbilityContext ctx)
        {
            EndEarly(ctx);
            Reset();
        }

        protected virtual void OnActivate(AbilityContext ctx) { }
        protected virtual void OnTick(AbilityContext ctx, float dt) { }
        protected virtual void OnEnd(AbilityContext ctx) { }
    }
}
