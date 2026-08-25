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
        /// The rate applied while the train is overhead. See `docs/Ilalim_Ng_Tulay.md` § 3.5.
        ///
        /// ⚠️⚠️ 3.5, UP FROM 2.0, AND THE RAISE IS ARITHMETIC RATHER THAN A BUFF. A rate
        /// multiplier saves the SAME ABSOLUTE 2.70 s of cooldown whatever the cooldown is, so
        /// its worth is entirely relative to the cycle it is measured against. At the old 6.5 s
        /// skill cooldowns, 2.0 for 2.70 s was **41 per cent of a cycle** and a real mechanic.
        /// Against the 30 to 45 s cooldowns that landed on 2026-08-25 the same number is
        /// **6.0 to 9.0 per cent**, which is not worth learning the train's timing for, and a
        /// map mechanic nobody plays around is a map mechanic that does not exist.
        ///
        /// 3.5 restores it to roughly 6.75 s off a 34 s cooldown, or a fifth of a cycle: less
        /// than it used to be worth in relative terms, which is correct, because a long cooldown
        /// should not be brushed aside by standing in the right place. `docs/TODO.md` § 5 asks
        /// for the A/B that settles this properly and it is still owed; what changed here is
        /// that the old number is now provably too small rather than merely unmeasured.
        ///
        /// ⚠️ IT STILL DOES NOT TOUCH THE ULTIMATE. See `CooldownRate` above: the meter is
        /// earned by acts, and a window that filled it would be a reason to stand still.
        /// </summary>
        public const float OverclockRate = 3.5f;

        public static void SetWarning(bool on) => Warning = on;

        public static void SetOverhead(bool on)
        {
            Overhead = on;
            CooldownRate = on ? OverclockRate : 1.0f;
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
