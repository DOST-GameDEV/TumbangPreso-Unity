using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>One live effect the HUD draws, with its own countdown.</summary>
    public struct StatusRow
    {
        public string Label;
        public float Remaining;

        /// <summary>
        /// What the row started at, so the bar can draw a RATIO rather than a raw number.
        ///
        /// ⚠️ IT IS THE EFFECT'S OWN FULL DURATION, NOT A SHARED MAXIMUM. A 1.25 s shove stun
        /// and a 5 s tag penalty drawn against one scale makes the shove a stub that never
        /// visibly empties, which is the opposite of what the bar is for.
        /// </summary>
        public float Total;

        /// <summary>
        /// ⚠️ VULNERABLE HAS NO COUNTDOWN AND THAT IS CORRECT. It lasts exactly as long as you
        /// choose to stand in the box holding a slipper, so it draws as a solid bar with no
        /// timer. Printing "VULNERABLE 0.0s" would read as an effect that had already expired,
        /// at the exact moment the player is in the most danger in the game.
        /// </summary>
        public bool Timed;
    }

    /// <summary>
    /// What the HUD shows about a unit's current state.
    ///
    /// ⚠️⚠️ A STUN THE PLAYER CANNOT TIME IS A STUN THEY CANNOT PLAY AROUND. Every row here
    /// exists so the player can see how long a thing lasts, which is the difference between a
    /// penalty and a mystery.
    ///
    /// ⚠️ THIS ASKS THE SAME FUNCTIONS THE RULES ASK, and that is a rule rather than a
    /// coincidence: the VULNERABLE row reads <see cref="CharacterMotor.IsTaggable"/>, which is
    /// the same call the tag itself makes. The warning a player sees and the check that
    /// catches them must be one function, or the HUD can promise safety the tag then ignores.
    ///
    /// ⚠️ AND A SHORT STUN INSIDE A LONGER ONE IS INVISIBLE HERE. That is a known, ACCEPTED
    /// cost of stuns overlapping by Max() rather than stacking: a 1.25 s shove stun inside the
    /// 5 s tag penalty reads as nothing happening. It is accepted because Max() is the entire
    /// bound on a stun chain in a 1-vs-3 game, and because both events announce themselves
    /// through channels that are not this stack, the shove with its own knockback and hit
    /// flash, the tag with its own toast. Only the ROW is merged; nothing is silent.
    /// </summary>
    public static class StatusStack
    {
        public static void Collect(CharacterMotor m, Carrier carrier, CombatVerbs verbs,
                                   List<StatusRow> into)
        {
            into.Clear();
            if (m == null) return;

            // ⚠️⚠️ THE STUN REPORTED NO TIME AT ALL, WHICH IS THE ONE THING THIS STACK EXISTS
            // FOR. 🧑 on the Godot build: *"add visible UI timers for all stun durations and
            // status effects so players clearly know when they can move or act again"*, and
            // most of what "the defender feels overpowered" actually was. A stun you cannot
            // time is a stun you cannot play around. It was hard-coded to 0.0 here, so the row
            // appeared with no countdown and drew a full bar for its whole duration.
            if (m.IsStunned)
                into.Add(new StatusRow
                {
                    Label = "STUNNED",
                    Remaining = m.StunLeft,
                    Total = m.StunTotal,
                    Timed = true,
                });

            if (m.Stamina.IsFatigued)
                into.Add(new StatusRow
                {
                    Label = "FATIGUED",
                    Remaining = m.Stamina.FatigueLeft,
                    Total = Balance.FatigueTime,
                    Timed = true,
                });

            // No countdown: it lasts as long as you choose to stand there.
            if (m.IsTaggable())
                into.Add(new StatusRow { Label = "VULNERABLE", Timed = false });

            // ⚠️ THE THROW LOCK IS A COOLDOWN THE PLAYER MEETS EVERY SINGLE RETRIEVAL and it
            // was missing from this list entirely. It is the beat between HAVING the slipper and
            // being able to throw it, and without a row the player presses fire, nothing
            // happens, and there is no reason on screen for it.
            if (carrier != null && carrier.ThrowLocked)
                into.Add(new StatusRow
                {
                    Label = "THROW CD",
                    Remaining = carrier.ThrowLockLeft,
                    Total = Balance.ThrowLockTime,
                    Timed = true,
                });

            if (verbs != null)
            {
                if (verbs.ShoveCooldownLeft > 0.0f)
                    into.Add(new StatusRow
                    {
                        Label = "SHOVE CD",
                        Remaining = verbs.ShoveCooldownLeft,
                        Total = Balance.ShoveCooldown,
                        Timed = true,
                    });

                if (verbs.LungeCooldownLeft > 0.0f)
                    into.Add(new StatusRow
                    {
                        Label = "LUNGE CD",
                        Remaining = verbs.LungeCooldownLeft,
                        Total = Balance.LungeCooldown,
                        Timed = true,
                    });

                if (verbs.PunchCooldownLeft > 0.0f)
                    into.Add(new StatusRow
                    {
                        Label = "PUNCH CD",
                        Remaining = verbs.PunchCooldownLeft,
                        Total = Balance.PunchCooldown,
                        Timed = true,
                    });
            }

            var abilitySystem = m.AbilitySystem;
            if (abilitySystem != null && abilitySystem.Kit != null)
            {
                var kit = abilitySystem.Kit;

                // Active Buffs
                if (kit.Skill1 != null && kit.Skill1.IsActive)
                    into.Add(new StatusRow
                    {
                        Label = kit.Skill1.Name,
                        Remaining = kit.Skill1.DurationRemaining,
                        Total = kit.Skill1.Duration,
                        Timed = true,
                    });

                if (kit.Skill2 != null && kit.Skill2.IsActive)
                    into.Add(new StatusRow
                    {
                        Label = kit.Skill2.Name,
                        Remaining = kit.Skill2.DurationRemaining,
                        Total = kit.Skill2.Duration,
                        Timed = true,
                    });

                if (kit.Ultimate != null && kit.Ultimate.IsActive)
                    into.Add(new StatusRow
                    {
                        Label = kit.Ultimate.Name,
                        Remaining = kit.Ultimate.DurationRemaining,
                        Total = kit.Ultimate.Duration,
                        Timed = true,
                    });

                // Specific kit states
                if (kit is ZackHeroKit zack && zack.IsOverchargeThrowActive)
                    into.Add(new StatusRow { Label = "OVERCHARGE", Timed = false });

                if (kit is SeanHeroKit sean && sean.IsIgnitionCannonActive)
                    into.Add(new StatusRow { Label = "IGNITION", Timed = false });

                // Ability Cooldowns (suffixed with " CD" to route to right-hand stack)
                if (kit.Skill1 != null && kit.Skill1.CooldownRemaining > 0.0f)
                    into.Add(new StatusRow
                    {
                        Label = $"{kit.Skill1.Name} CD",
                        Remaining = kit.Skill1.CooldownRemaining,
                        Total = kit.Skill1.Cooldown,
                        Timed = true,
                    });

                if (kit.Skill2 != null && kit.Skill2.CooldownRemaining > 0.0f)
                    into.Add(new StatusRow
                    {
                        Label = $"{kit.Skill2.Name} CD",
                        Remaining = kit.Skill2.CooldownRemaining,
                        Total = kit.Skill2.Cooldown,
                        Timed = true,
                    });
            }
        }

        /// <summary>
        /// The stamina bar, 0 to 1.
        ///
        /// ⚠️ IT IS A POINT POOL, NOT A TIMER, and the bar has to read as one. The pool is
        /// dimensioned to roughly one crossing of the danger zone, which is the retrieval the
        /// whole game is about, so what the player needs from this bar is "can I get back out
        /// from here", not "how many seconds of running is left".
        /// </summary>
        public static float StaminaRatio(CharacterMotor m) =>
            m == null ? 0.0f : Mathf.Clamp01(m.Stamina.Ratio);

        /// <summary>
        /// ⚠️ THE CROSSHAIR ASKS CanThrow, so it greys out for exactly the reasons the throw
        /// refuses. A second opinion about legality is a crosshair that promises a throw the
        /// rules then refuse, which is the most confusing possible failure: the player sees no
        /// reason for nothing to happen.
        /// </summary>
        public static bool ShowCrosshair(CharacterMotor m) =>
            GameServices.Round != null && GameServices.Round.CanThrow(m);
    }
}
