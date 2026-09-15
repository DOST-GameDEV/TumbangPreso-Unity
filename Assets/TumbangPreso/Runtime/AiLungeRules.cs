using TumbangPreso.Core;

namespace TumbangPreso
{
    /// <summary>
    /// When a bot taya should NOT open a lunge charge: when its own punch will reach the target
    /// before the charge could be released.
    ///
    /// ⚠️⚠️ MEASURED, NOT ARGUED (docs/reports/claude-engineering-2026-09-15, C2). The historical
    /// "5 lunge hits in 125 attempts" was read as lunge aim. `AiDiagnosticProbe`'s whole-match
    /// lunge trace at 1/60 s, time scale 1, found that on Classic Eskinita seed 20260823
    /// **90 of 110 released lunges followed a tag the SAME taya's punch had already scored during
    /// the charge**. `StepLungeIntent` opens a charge at up to `LungeRange` (2.6 m on Normal),
    /// keeps walking in, and its punch branch fires first at `PunchRange` 1.7 m. The punch tags,
    /// the victim is reset out of the box, `TagTarget` has nobody, and the lunge button is
    /// released by the plan sweep. `CombatVerbs.StepLunge` has no cancel, so every such release
    /// is a real dash with a real 1.5 s cooldown at nobody, counted as a lunge attempt.
    ///
    /// ⚠️ THIS IS AN AI DECISION AND CHANGES NO HUMAN NUMBER. Lunge speed, reach, charge time,
    /// active time, cooldown, the punch and the tag radius are all untouched. A person holding
    /// right click while jabbing is still free to spend the lunge the same way.
    /// </summary>
    public static class AiLungeRules
    {
        /// <param name="predictedReach">Flat distance to where the bot expects the victim to be
        /// when the hold completes (`AheadOf(victim, LungeHoldTime)`).</param>
        /// <param name="punchCooldownLeft">The taya's live punch cooldown.</param>
        /// <param name="holdSeconds">How long the bot holds a lunge before releasing it.</param>
        /// <param name="approachSpeed">How fast the taya keeps closing while it holds; `DoHunt`
        /// never stops driving at the victim.</param>
        public static bool PunchWillArriveFirst(float predictedReach, float punchCooldownLeft,
                                                float holdSeconds, float approachSpeed)
        {
            if (punchCooldownLeft > holdSeconds) return false;
            return predictedReach - approachSpeed * holdSeconds <= Balance.PunchRange;
        }
    }
}
