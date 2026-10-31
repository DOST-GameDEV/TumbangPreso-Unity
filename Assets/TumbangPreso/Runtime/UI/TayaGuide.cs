using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Owner, October 31: warn the taya what is not allowed, "going out of play area or tagging ung walang slipper". Then:
    /// "it doesnt look warningy". The words go on the match's one red refusal tile (`TumpMatchReadout.Refuse`), which brings
    /// the shake, the warning chip and the quiet refusal sound. Each is the rule's sentence split at its dash. It decides nothing.
    /// </summary>
    public static class TayaGuide
    {
        public const string StayInBox = "STAY INSIDE THE BOX - THE TAYA CAN'T LEAVE IT";
        public const string CanDown = "CANNOT TAG - CAN MUST BE UPRIGHT FIRST";
        public const string NoSlipper = "CANNOT TAG - THEY HAVE NO SLIPPER";
        public const string OutsideBox = "CANNOT TAG - THEY'RE OUTSIDE THE BOX";
        public const string OutOfReach = "CANNOT TAG - THEY'RE PROTECTED RIGHT NOW";

        /// <summary>Why a touch on this attacker would not land, or null if it would.</summary>
        public static string WhyNoTag(bool canUpright, bool protectedNow, bool vulnerable, bool holdingSlipper, bool insideBox)
        {
            if (!canUpright) return CanDown;
            if (protectedNow) return OutOfReach;
            if (vulnerable) return null;
            if (!holdingSlipper) return NoSlipper;
            return insideBox ? null : OutsideBox;
        }

        /// <summary>Shows a warning if this body is the one this screen plays. Repeats only keep it up.</summary>
        public static void Warn(CharacterMotor who, string words)
        {
            var hud = Hud.Instance;
            if (hud == null || who == null || hud.Local != who || GameLaunch.Spectator || string.IsNullOrEmpty(words)) return;
            hud.ShowRefusal(words);
        }
    }
}
