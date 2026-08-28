using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The state of the LRT pass, which is the one thing on Ilalim ng Tulay that both game
    /// modes read and each answers differently.
    ///
    /// ⚠️⚠️ THE TWO MODES GET DIFFERENT ANSWERS FROM THE SAME EVENT, AND THAT IS THE DESIGN, NOT
    /// A SHORTCUT. `docs/VISION.md` § 1.1: Classic is not Hero Strike with the powers off, so a
    /// map event may not hand Classic a power. Hero Strike gets a real one, `CooldownRate`,
    /// because a 24 s metronome that opens a power window every cycle is exactly the readable
    /// counterplay that mode exists to add. Classic gets the spectacle and a Street Hype
    /// callout, which is cosmetic by construction and cannot touch a single point.
    ///
    /// ⚠️ IT IS A PLAIN STATIC AND IT IS RESET BY THE FLYBY THAT OWNS IT. There is one train on
    /// one map, so a service locator entry would be ceremony. What it must never become is
    /// something a second map also writes: `LrtTrainFlyby.OnDisable` puts it back, so loading
    /// Eskinita after this map cannot leave a 2x cooldown rate behind.
    /// </summary>
    public static class OverheadPassWindow
    {
        /// <summary>
        /// How fast ability cooldowns run right now. 1.0 everywhere else in the game.
        ///
        /// ⚠️ READ BY `HeroAbility.Tick` AND BY NOTHING ELSE. In particular it does NOT touch
        /// the ultimate charge: `docs/VISION.md` § 4 says nothing may reward waiting, and a
        /// meter that fills faster on a timer is a reason to stand still for twenty seconds. A
        /// cooldown that drains faster only pays a player who is already casting.
        /// </summary>
        public static float CooldownRate { get; private set; } = 1.0f;

        /// <summary>True while the consist is actually over the street.</summary>
        public static bool Overhead { get; private set; }

        /// <summary>True during the three second run-up, when the warning is on screen.</summary>
        public static bool Warning { get; private set; }

        /// <summary>
        /// How many seconds of cooldown one pass under the train is worth.
        ///
        /// ⚠️⚠️ THIS IS THE AUTHORED NUMBER NOW, AND THE MULTIPLIER IS DERIVED FROM IT. It used
        /// to be the other way round: `OverclockRate` was a hand-set 2.0, then a hand-set 3.5,
        /// and the thing a designer actually cares about — how much of a cooldown the window
        /// takes off — was nowhere in the file. `docs/Hero_Strike_Balance.md` § 4.5 argued the
        /// mechanic should stop being a multiplier *"on the grounds that a flat figure survives
        /// any later cooldown retune where a multiplier does not"*, and `docs/TODO.md` § 5
        /// carries it. This is that change.
        ///
        /// ⚠️⚠️ THE HISTORY IS THE ARGUMENT AND IT IS WORTH KEEPING. A rate `r` held for a
        /// window `W` advances the cooldown clock by `W * r`, so it SAVES `W * (r - 1)` seconds
        /// of real time, and that saving never depended on the cooldown at all. What moved was
        /// only what it was worth as a fraction of a cycle:
        ///
        ///   * At the old 6.5 s skill cooldowns, rate 2.0 saved 2.70 s: **41 per cent of a
        ///     cycle**, a real mechanic.
        ///   * Against the 30 to 45 s cooldowns that landed on 2026-08-25, the same rate saved
        ///     the same 2.70 s, now **6.0 to 9.0 per cent**. Not worth learning the train's
        ///     timing for, and a map mechanic nobody plays around does not exist.
        ///   * Rate 3.5 was then set to restore it, which saves 6.75 s: about a fifth of a 34 s
        ///     cooldown. Correct, and completely invisible in the constant `3.5f`.
        ///
        /// So the number below is the one every one of those bullets is really about, and the
        /// two ways of writing it agree exactly today: `1 + 6.75 / 2.70 = 3.5`.
        ///
        /// ⚠️ IT IS DELIBERATELY LESS THAN A WHOLE COOLDOWN. A window that refunded a cast
        /// outright would make the train the ability, and `docs/VISION.md` § 4 wants the
        /// counterplay to be timing rather than a free press.
        /// </summary>
        public const float OverclockSeconds = 6.75f;

        /// <summary>
        /// How long the consist is actually over the street.
        ///
        /// ⚠️ MIRRORED FROM `LrtTrainFlyby`, WHICH DERIVES IT: `(33.0 + 15.6) / 18 = 2.70 s`
        /// from nose entering to tail leaving. It is repeated here rather than read from the
        /// component because this class is a plain static that must answer before any flyby
        /// exists, and `OverclockWindowMatchesTheTrain` in `MapGradeSanityTests` asserts the two
        /// still agree so the copy cannot drift in silence.
        /// </summary>
        public const float PassSeconds = 2.70f;

        /// <summary>
        /// The rate applied while the train is overhead. See `docs/Ilalim_Ng_Tulay.md` § 3.5.
        ///
        /// ⚠️⚠️ DERIVED, NEVER TYPED. `OverclockSeconds` is the design decision; this is the
        /// arithmetic that delivers it through the one mechanism `HeroAbility.Tick` already
        /// reads. Retuning the train's speed now keeps the saving constant instead of silently
        /// changing it, which is the failure mode a hand-set multiplier has and the reason
        /// § 4.5 asked for the swap.
        ///
        /// ⚠️ IT STILL DOES NOT TOUCH THE ULTIMATE. See `CooldownRate` above: the meter is
        /// earned by acts, and a window that filled it would be a reason to stand still.
        /// </summary>
        public const float OverclockRate = 1.0f + OverclockSeconds / PassSeconds;

        /// <summary>
        /// The rate a pass ACTUALLY applies. It is <see cref="OverclockRate"/> and nothing in the
        /// game ever writes it.
        ///
        /// ⚠️⚠️ IT EXISTS SO THE SWEEP `docs/TODO.md` § 5 HAS OWED SINCE 2026-08-25 CAN BE
        /// RUN AT ALL. That entry wants the window compared at several values, and the shipped
        /// number is a `const` derived from `OverclockSeconds` by design, so the only other way
        /// to answer it is to edit the constant, recompile, run, and repeat: three builds whose
        /// only guarantee that they differ by one number is that somebody was careful. A field
        /// one probe writes and restores makes the comparison a single run of a single build,
        /// which is the whole reason the fixed-step probe was built (§ 10).
        ///
        /// ⚠️ NOTHING IN THE GAME MAY READ THIS EXCEPT `SetOverhead`, AND NOTHING MAY WRITE IT
        /// EXCEPT A MEASUREMENT. It is deliberately not a setting, not a difficulty, and not
        /// networked: two peers disagreeing about it would pay different cooldowns for the same
        /// train. `BotBehaviourProbe` restores it in a `finally`-shaped teardown for the reason
        /// `SoloPracticeTests` restores the difficulty, which is that a static left modified
        /// silently retunes every test after it.
        /// </summary>
        public static float AppliedRate { get; private set; } = OverclockRate;

        /// <summary>Set the applied rate for a measurement. See <see cref="AppliedRate"/>.</summary>
        public static void SetAppliedRateForMeasurement(float rate) => AppliedRate = rate;

        /// <summary>Put the applied rate back to the shipped one.</summary>
        public static void RestoreAppliedRate() => AppliedRate = OverclockRate;

        public static void SetWarning(bool on) => Warning = on;

        public static void SetOverhead(bool on)
        {
            Overhead = on;
            CooldownRate = on ? AppliedRate : 1.0f;
        }

        /// <summary>Back to neutral. Called when the flyby leaves the scene.</summary>
        public static void Clear()
        {
            Warning = false;
            Overhead = false;
            CooldownRate = 1.0f;
        }
    }
}
