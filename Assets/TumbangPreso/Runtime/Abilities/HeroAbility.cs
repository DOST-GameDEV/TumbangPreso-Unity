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

        /// <summary>
        /// The full tactical sentence, for the hold-to-read tray.
        ///
        /// ⚠️ THE TRAY IS THE ONLY PLACE THIS BELONGS. The deck at the bottom of the screen
        /// carries what is true RIGHT NOW and nothing else; 🧑 2026-08-23 on why:
        /// *"games like valorant overwatch league etc dont clog their screen with text"*.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// One short line for anywhere the full sentence does not fit, chiefly the character
        /// select ribbon's details card.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE FOUR OF THE FIFTEEN POWERS DESCRIBED THEMSELVES IN A SENTENCE
        /// THAT STOPPED MID-WORD. The card draws `Description` into a 46 px box at 14 pt with
        /// `VerticalWrapMode.Truncate`, which is about three lines, and the tactical sentences
        /// run to four or five. Truncation is silent, so the screen a player uses to CHOOSE a
        /// hero was the one screen lying to them about what the hero does.
        ///
        /// Falls back to the full description, so an ability that has not been given one is
        /// merely long rather than blank.
        /// </summary>
        public string Summary { get; }

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

        /// <summary>
        /// How wide the ground telegraph is, in metres, or 0 for a power that puts nothing on
        /// the ground.
        ///
        /// ⚠️⚠️ IT LIVES ON THE ABILITY BECAUSE THE HUD USED TO INVENT IT.
        /// `HeroAbilitySystem.UpdateReticle` drew 7.5 m for ANY ultimate, 5.0 m for ANY first
        /// skill and 3.5 m for ANY second, and offset the ring forward only when the kit
        /// happened to be Cheska's. So Dante's 2.4 m stomp drew a 5.0 m ring, Nemu's 3.2 m void
        /// drew 7.5 m, and the void landed 3.5 m in front of the ring the player had just been
        /// shown. **A telegraph that lies is worse than no telegraph**, because a player
        /// believes it once and then stops believing all of them.
        ///
        /// Same reasoning as <see cref="Glyph"/>: a lookup table keyed by ability id is a
        /// second place to forget, and a new hero would compile, run and draw three wrong
        /// rings.
        /// </summary>
        public float TelegraphRadius { get; protected set; }

        /// <summary>
        /// How far in front of the caster the telegraph centre sits, in metres. 0 means it goes
        /// off around the caster.
        ///
        /// ⚠️ IT MUST MATCH THE NUMBER THE `OnActivate` ACTUALLY SPAWNS AT. These two are the
        /// same measurement written twice, which is the shape of drift `Design.md` opens by
        /// warning about, so `TelegraphsMatchWhatTheAbilityActuallyPlaces` asserts the pair.
        /// </summary>
        public float TelegraphRange { get; protected set; }

        public bool HasTelegraph => TelegraphRadius > 0.0f;

        protected HeroAbility(string id, string name, string description, float cooldown,
                              float duration = 0.0f,
                              UI.AbilityGlyph glyph = UI.AbilityGlyph.Burst,
                              string summary = null,
                              float telegraphRadius = 0.0f,
                              float telegraphRange = 0.0f)
        {
            Id = id;
            Name = name;
            Description = description;
            Summary = string.IsNullOrEmpty(summary) ? description : summary;
            Cooldown = cooldown;
            Duration = duration;
            Glyph = glyph;
            TelegraphRadius = telegraphRadius;
            TelegraphRange = telegraphRange;
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
