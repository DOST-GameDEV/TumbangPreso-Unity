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

        /// <summary>The rate applied while the train is overhead. See `docs/Ilalim_Ng_Tulay.md`.</summary>
        public const float OverclockRate = 2.0f;

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
