using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// One Unity launch that re-authors the PC Express fascia, rebuilds the map, measures it and
    /// captures the showcase set.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE FOUR STEPS ARE ORDER-DEPENDENT AND A MISSED ONE IS INVISIBLE.
    /// `PcExpressSignAuthor` rewrites an `.obj` on disk, `IlalimNgTulayBuilder` instantiates that
    /// `.obj` into a scene, `MapGeometryCheck` opens the saved scene and `IlalimNgTulayShowcaseProbe`
    /// renders it. Running the capture without the rebuild in between produces pictures of the
    /// PREVIOUS map, which is the exact failure `CLAUDE.md` § 6.1 records: a review conducted
    /// against an image that no longer matches what is on disk. Four separate launches also cost
    /// four domain reloads, and each one is a chance to leave the project lock held.
    ///
    /// ⚠️ THE GEOMETRY CHECK RUNS BEFORE THE CAPTURE, NOT AFTER. A render of a map with a prop
    /// standing on air still looks fine from most angles, so the measurement has to be the thing
    /// that decides whether the pictures are worth looking at.
    ///
    ///     Unity.exe -batchmode -quit -projectPath . \
    ///               -executeMethod TumbangPreso.EditorTools.MapKit.IlalimNgTulayPipeline.Run
    /// </summary>
    public static class IlalimNgTulayPipeline
    {
        [MenuItem("Tumbang Preso/Rebuild and Capture Ilalim Ng Tulay")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        /// <summary>
        /// Author the fascia and capture. No rebuild, no geometry gate.
        ///
        /// ⚠⚠ THE FULL PIPELINE IS THE WRONG TOOL FOR LOOKING AT A SIGN, AND USING IT FOR
        /// THAT WASTED REAL TIME. `Run` rebuilds ~1250 prefab instances and then opens THREE
        /// scenes to run a 25-sample footprint test against every renderer in each of them, so a
        /// one-line change to a lightbox cost the whole gate. None of it is needed here: the
        /// fascia is geometry on `env_pc_express_store.obj` and the scene PREFAB-INSTANCES that
        /// file, so rewriting the `.obj` and reimporting it changes what the saved scene renders
        /// without the scene being touched.
        ///
        /// ⚠ IT IS NOT A SUBSTITUTE FOR `Run`. Anything that moves a prop, a group or a
        /// builder constant needs the rebuild and the gate, and skipping them is how a map ships
        /// with something standing on air. Use this while iterating on a look; use `Run` before
        /// believing the result.
        ///
        ///     Unity.exe -batchmode -quit -projectPath . \
        ///               -executeMethod TumbangPreso.EditorTools.MapKit.IlalimNgTulayPipeline.RunFast
        /// </summary>
        public static void RunFast() => EditorApplication.Exit(ExecuteFast() ? 0 : 1);

        [MenuItem("Tumbang Preso/Recapture Ilalim Ng Tulay (no rebuild)")]
        public static void RunFastFromMenu() => ExecuteFast();

        public static bool ExecuteFast()
        {
            if (!PcExpressSignAuthor.Execute())
            {
                Debug.LogError("[IlalimNgTulayPipeline] the PC Express fascia author failed.");
                return false;
            }

            IlalimNgTulayShowcaseProbe.FasciaOnly = true;
            bool captured = IlalimNgTulayShowcaseProbe.Execute();
            IlalimNgTulayShowcaseProbe.FasciaOnly = false;
            Debug.Log($"[IlalimNgTulayPipeline] fast recapture, no rebuild, no gate. " +
                      $"capture {(captured ? "OK" : "FAILED")}.");
            return captured;
        }

        public static bool Execute()
        {
            if (!PcExpressSignAuthor.Execute())
            {
                Debug.LogError("[IlalimNgTulayPipeline] the PC Express fascia author failed.");
                return false;
            }

            if (!IlalimNgTulayBuilder.Execute())
            {
                Debug.LogError("[IlalimNgTulayPipeline] the map builder failed.");
                return false;
            }

            if (!AsphaltRoadSurface.ExecuteForScene(IlalimNgTulayBuilder.ScenePath))
            {
                Debug.LogError("[IlalimNgTulayPipeline] the asphalt surface failed.");
                return false;
            }

            bool measured = MapGeometryCheck.Execute(true);

            // ⚠️ THE CAPTURE STILL RUNS ON A FAILED MEASUREMENT, ON PURPOSE. The findings name a
            // prop and a number; the picture is what says which of the two surfaces it should
            // have been resting on. Refusing to render on a red gate throws away the cheaper
            // half of the diagnosis, so the exit code carries the failure instead.
            bool captured = IlalimNgTulayShowcaseProbe.Execute();

            Debug.Log($"[IlalimNgTulayPipeline] fascia authored, map rebuilt, asphalt laid, " +
                      $"geometry {(measured ? "OK" : "FAILED")}, capture {(captured ? "OK" : "FAILED")}.");

            return measured && captured;
        }
    }
}
