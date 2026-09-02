using System;
using System.Collections;
using UnityEngine;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// A bounded wait, for every place a probe used to spin on an `AsyncOperation` for ever.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE A PLAYMODE RUN HUNG FOR THREE HOURS AND TWENTY-FOUR MINUTES AND
    /// NAMED NOTHING. `docs/TODO.md` § 109. On 2026-08-31 a run of
    /// `-testFilter "ScoreWitnessProbe|QueueCardLayoutProbe"` was started at 11:37; at 15:01 it
    /// was still going, `Unity.exe` had burned 5.6 hours of CPU across its threads, and
    /// `Logs/witness.log` had 257 KB in it of which the last useful line was
    /// `[QueueCardLayoutProbe] wrote ... queue_card_v1.png`. **The test after it started and
    /// logged nothing at all**, and there is no way to tell a hung test from a slow one from
    /// outside.
    ///
    /// ⚠️⚠️ AND THE HANDOFF THAT REPORTED IT BLAMED THE WRONG THING, WHICH IS THE OTHER REASON
    /// THIS IS WRITTEN DOWN. It said `CloudEndpointActionProbe.Await` *"spins
    /// `while (!task.IsCompleted) yield return null` with no timeout"*. That method has had a
    /// 30-second deadline since `816af8b3` and it throws a `TimeoutException` when it expires. The
    /// unbounded waits were the SEVENTY `while (load != null && !load.isDone)` loops in the scene
    /// setup of forty-nine probes, which nobody had looked at because a scene load "obviously"
    /// finishes.
    ///
    /// ⚠️⚠️ THE BOUND IS FRAMES **AND** WALL CLOCK, AND THE FRAME HALF IS THE ONE THAT WORKS.
    /// Frames were still being pumped throughout that hang: `SocialStore`'s 60-second presence
    /// heartbeat fired about 230 times into the log while the test sat there, which is the
    /// evidence that the editor loop was alive and one particular coroutine was not progressing. A
    /// wall-clock bound alone would also have caught it; a frame bound alone would not catch a
    /// genuinely frozen editor. Both cost one comparison.
    ///
    /// ⚠️ IT FAILS RATHER THAN CONTINUING. A probe that gives up on a scene load and carries on is
    /// a probe that then asserts against an empty scene and reports something that has nothing to
    /// do with the fault. `CLAUDE.md` § 7: a run that writes no `.xml` and exits 0 is
    /// indistinguishable from a pass, and so is a run that never finishes.
    /// </summary>
    public static class ProbeWait
    {
        /// <summary>
        /// ⚠️ 6000 FRAMES AND 180 SECONDS, AND BOTH ARE CEILINGS RATHER THAN EXPECTATIONS. A cold
        /// `Eskinita` load in batchmode settles in well under a thousand frames and a few seconds;
        /// these are set far enough above that that a slow machine never trips them, and far
        /// enough below "for ever" that a stuck run costs three minutes instead of an afternoon.
        /// </summary>
        public const int MaxFrames = 6000;

        public const float MaxSeconds = 180.0f;

        /// <summary>
        /// Waits for an `AsyncOperation`, and fails the test with a named cause if it stalls.
        ///
        /// ⚠️ A NULL OPERATION IS "NOTHING TO WAIT FOR" AND RETURNS IMMEDIATELY, which is what
        /// `SceneManager.UnloadSceneAsync` answers when it refuses, and what every call site
        /// already guarded for with `op != null`.
        /// </summary>
        public static IEnumerator Done(AsyncOperation op, string what)
        {
            if (op == null) yield break;

            int frames = 0;
            float deadline = Time.realtimeSinceStartup + MaxSeconds;

            while (!op.isDone)
            {
                if (frames++ > MaxFrames || Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException(
                        $"'{what}' did not finish inside {MaxFrames} frames or {MaxSeconds} s "
                        + $"(progress {op.progress:P0}). This is docs/TODO.md § 109: the run used "
                        + "to sit here for ever and the log named nothing, so the fix is that the "
                        + "test fails with a name on it. Check Temp/UnityLockfile first "
                        + "(CLAUDE.md § 7) and then what this probe loads.");

                yield return null;
            }
        }
    }
}
