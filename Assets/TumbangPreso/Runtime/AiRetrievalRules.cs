using TumbangPreso.Core;

namespace TumbangPreso
{
    /// <summary>
    /// Which reasons to fetch outrank "somebody else has the better run".
    ///
    /// ⚠️⚠️ `AIController`'s § ONE RUNNER AT A TIME says a bot on the tournament clock, a bot that
    /// has stalled long enough, or a bot running at a downed can "never reaches this question at
    /// all", and until 2026-09-15 the code did not do that. `PlanAttacker` read
    /// `!FetchIsSafe(...) || !IHaveTheBestRun(...)`, so every override that made `FetchIsSafe`
    /// true still went on to ask whether a rival ranked higher. When the best-ranked rival was
    /// itself waiting (its own run was unsafe), every attacker below it yielded to a runner who
    /// was not running, and the anti-stall clock charged all of them.
    ///
    /// Measured, current build, `BotBehaviourProbe` with the per-penalty trace
    /// (docs/reports/claude-engineering-2026-09-15): Hero Strike on Eskinita seed 4242 seat 3 sat
    /// in Stalk for 10.7 s with its clock at 11 s while seat 0's loose shoe outranked it, and
    /// Ilalim seed 20260904 seat 2 did the same at 11 s. Both began fetching only when the rival
    /// picked up. Nothing here changes the pickup rule, the penalty or any balance number.
    /// </summary>
    public static class AiRetrievalRules
    {
        /// <summary>The same "already late" line `DoFetch` sprints on and `FetchIsSafe` reads.</summary>
        public static bool IsLate(float attackerIdleSeconds)
            => attackerIdleSeconds >= Balance.SlipperUnretrievedWarningTime * 0.5f;

        /// <summary>
        /// May this bot defer its retrieval to a rival with better odds? Only when none of the
        /// three overrides that the one-runner rule was written to exempt applies.
        /// </summary>
        public static bool MayYieldToBetterRun(float attackerIdleSeconds, float stalkSeconds,
                                               float stalkPatienceSeconds, bool lataUpright)
        {
            if (IsLate(attackerIdleSeconds)) return false;
            if (stalkSeconds >= stalkPatienceSeconds) return false;
            if (!lataUpright) return false;
            return true;
        }
    }
}
