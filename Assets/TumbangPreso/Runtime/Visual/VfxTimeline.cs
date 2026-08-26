using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// A transient effect whose whole appearance is a function of how far through its life it
    /// is, and which can therefore be asked to show any single moment of that life.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE ONLY THING THE HARNESS COULD PHOTOGRAPH WAS THE HALF THAT
    /// DOES NOT MOVE. `docs/TODO.md` § 8 item 2, open since 2026-08-25:
    /// *"`AbilityShowcaseProbe` captures the persistent zones only, and every one of these
    /// changes is on a transient that lives 0.4 to 1.1 s, so the v7 captures do not show a
    /// single one of them."* Every blast core, every shockwave and every ultimate in this game
    /// is one of those transients. The whole § 8 silhouette pass — a nova shell instead of a
    /// sphere, a shockfront with a leading edge, an ion spire instead of a disc — was written,
    /// shipped and then reviewed against pictures that could not contain it.
    ///
    /// ⚠️ THE PROBE RUNS IN EDIT MODE, WHERE `Update` NEVER FIRES AND `Object.Destroy(go, t)`
    /// never comes due. So a blast spawned in a capture sits frozen at its FIRST frame: scale
    /// 0.35, full alpha, the moment before it becomes the thing anybody is arguing about.
    /// Reading the elapsed time from a field the capture can write is what turns that into a
    /// choice of which frame to photograph.
    ///
    /// ⚠️⚠️ AND THE ANIMATION IS NOT DUPLICATED FOR IT. Each implementer's `Update` is one line
    /// that calls `StepTo(_elapsed + Time.deltaTime)`, so the frame a capture shows is produced
    /// by exactly the code that produces the frame a player sees. A second "preview" path would
    /// be a second answer to what an explosion looks like, and the one that drifts is always the
    /// one nobody plays.
    /// </summary>
    public interface IVfxTimeline
    {
        /// <summary>How long this effect takes to run from spawn to spent.</summary>
        float LifeSeconds { get; }

        /// <summary>Show the frame at <paramref name="seconds"/> after spawn.</summary>
        void StepTo(float seconds);
    }

    /// <summary>Scene-wide operations on <see cref="IVfxTimeline"/>.</summary>
    public static class VfxTimeline
    {
        /// <summary>
        /// Wind every transient effect in the scene to the same moment of its own life,
        /// expressed as a FRACTION rather than in seconds.
        ///
        /// ⚠️ A FRACTION, BECAUSE THE LIVES DIFFER AND THE INTERESTING MOMENT DOES NOT. A core
        /// runs 0.5 s and its ground wave 0.4 s; asking both for "0.2 s" photographs one at 40
        /// per cent and the other at 50, which is a difference introduced by the capture rather
        /// than by the ability. Every effect at 0.35 of its life is the same instant of the same
        /// event.
        /// </summary>
        /// <returns>How many effects were stepped, so a caller can tell an empty scene from a
        /// broken one.</returns>
        public static int StepAll(float lifeFraction)
        {
            int stepped = 0;

            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!(behaviour is IVfxTimeline timeline)) continue;

                timeline.StepTo(timeline.LifeSeconds * Mathf.Clamp01(lifeFraction));
                stepped++;
            }

            return stepped;
        }
    }
}
