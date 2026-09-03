using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Puts the world back between PlayMode tests, so no test can inherit one.
    ///
    /// ⚠️⚠️ THIS IS `docs/TODO.md` § 126.8, WHICH IS THE LARGEST THING THAT ENTRY FOUND: **the
    /// full PlayMode suite is not a reliable gate on this branch.** Two full runs of nearly the
    /// same code came back **42 red and 41 red with eleven suites swapping sides**, and the nine
    /// suites carrying about twenty of those failures produced **two** failures when re-run
    /// together on their own, on exactly the code that had just failed them. *"A gate whose red
    /// set moves is not measuring the code."*
    ///
    /// ⚠️⚠️ THE MECHANISM, AND IT IS ONE LINE IN FOUR FILES: `SceneManager.LoadSceneAsync(name,
    /// LoadSceneMode.Single)` **DESTROYS EVERY OBJECT IN THE PREVIOUS SCENE**, and not one of the
    /// five suites § 126.8 names by stack trace had a teardown of any kind. So the scene a test
    /// loaded was still the active scene when the NEXT test started, and a load still settling was
    /// still settling into the next test's objects.
    ///
    /// That is every symptom in the entry, from one cause:
    ///
    ///  * `MissingReferenceException: the object of type X has been destroyed`, **inside the
    ///    test**, at `SteeringTests.cs:177`, `SettingsWheelProbe.cs:117`, `UiClickProbe.cs:140`
    ///    and `VolcanicZoneTests.cs:60`. `SteeringTests` is the clearest: the failing test builds
    ///    a bare `Cube` floor and a `CharacterMotor` in the ACTIVE scene, waits twenty fixed
    ///    updates and then reads `go.transform`. **A single-mode scene load left in flight by the
    ///    test before it lands during those twenty frames and takes both objects with it.** The
    ///    reference is stale because something outside the test destroyed it, exactly as the entry
    ///    says.
    ///  * *"the arena built no SliceRunner"*, *"No main camera in the arena"*, *"the guided route
    ///    never installed"*, *"MatchSetup has no CharacterSelectPanel to open"*, *"the lobby must
    ///    have a door to the account screen"*. Every one of those is a scene that did not come up
    ///    the way the test expected, which is what asking for a scene while another load is
    ///    pending produces.
    ///
    /// ⚠️⚠️ AND IT IS WHY TARGETED RUNS ARE HONEST AND THE FULL RUN IS NOT. § 94.8 records
    /// *"PlayMode, targeted: 15/15"* and § 125's is *"`InputSurfaceProbe` 5/5"*. **Those runs
    /// pass because there is nothing ahead of them to inherit.** The suite only comes apart as one
    /// process, which is the one thing nobody had done on this branch.
    ///
    /// ⚠️ IT IS OPTION 1 OF THE TWO § 126.8 OFFERS, AND DELIBERATELY NOT OPTION 2. That entry
    /// closes with: *"DO NOT CLOSE IT BY WIDENING A BOUND OR BY ADDING A THIRD CATEGORY
    /// EXCLUSION ... a category meaning 'these tests do not work next to each other' would be
    /// hiding this finding rather than recording it."*
    ///
    /// ⚠️ AND IT DOES NOT REACH `UgsServicesProbe`, WHICH THE ENTRY ALSO SAYS AND WHICH IS TRUE.
    /// That suite's state lives on somebody else's server, so no amount of tearing a scene down
    /// touches it. It is fixed separately and differently in § 130.3: the probe was starting a
    /// SECOND UGS sign-in beside the one `NetIdentity` fires at boot, and UGS refuses a
    /// concurrent sign-in.
    ///
    /// <para>
    /// <b>How to use it.</b> Add both hooks to a PlayMode fixture:
    /// <code>
    /// [UnitySetUp]    public IEnumerator SetUp()    =&gt; PlayModeWorld.Reset();
    /// [UnityTearDown] public IEnumerator TearDown() =&gt; PlayModeWorld.Reset();
    /// </code>
    /// ⚠️ <b>BOTH, NOT ONE.</b> Teardown alone protects the suites that come after and leaves this
    /// one exposed to whatever ran before it, and until every fixture in the folder has the pair
    /// there is always something before it that does not. The empty-scene load is a few
    /// milliseconds; a run whose numbers cannot be quoted costs seventeen minutes.
    /// </para>
    /// </summary>
    public static class PlayModeWorld
    {
        /// <summary>
        /// The scene every test starts and finishes in.
        ///
        /// ⚠️ IT IS CREATED RATHER THAN LOADED FROM THE BUILD SETTINGS, so it needs no asset, is
        /// guaranteed empty, and cannot itself be the scene some other test was asserting about.
        /// `SceneManager.CreateScene` plus `SetActiveScene` is the whole thing.
        /// </summary>
        private const string EmptySceneName = "PlayModeWorldReset";

        /// <summary>
        /// Return the world to a single, empty, fully-settled scene.
        ///
        /// ⚠️⚠️ THE POINT IS THE WAIT AS MUCH AS THE UNLOAD. An `AsyncOperation` that has not
        /// reported `isDone` is a scene load that will land later, and "later" is inside somebody
        /// else's test. Creating a scene synchronously and unloading the rest with a yielded wait
        /// means nothing is ever in flight when this returns.
        /// </summary>
        public static IEnumerator Reset()
        {
            // ⚠️ A FRAME FIRST, so an operation started by the test that just ended has somewhere
            // to finish. Unloading out from under a pending load is the fault, not the fix.
            yield return null;

            // ⚠️⚠️ THE LIVE MATCH GOES BEFORE THE SCENES DO, AND THIS IS `SoloPracticeTests`'S
            // OWN FINDING GENERALISED. Its teardown records it in full and it is worth repeating
            // here because it is the half a scene unload cannot reach: **the directors are
            // `DontDestroyOnLoad`, so a live round OUTLIVES the scene.** The next suite's arena
            // then loads underneath a match that is still ticking, and its slipper is teleported
            // home by an intermission it knows nothing about. *"`LandedHighlightTests` failed
            // exactly that way, twice, and passes alone."*
            //
            // ⚠️ THAT SENTENCE IS § 126.8 IN MINIATURE AND IT WAS ALREADY WRITTEN DOWN. One suite
            // had noticed the class, fixed its own instance, and there was nowhere to put the
            // general version of the fix. This is that place.
            //
            // ⚠️ NULL-CONDITIONAL THROUGHOUT: a suite that never called `GameServices.Ensure()`
            // has no directors at all, and a reset that threw would fail the test it was cleaning
            // up after rather than the one that made the mess.
            TumbangPreso.GameServices.Round?.EndRound();
            TumbangPreso.GameServices.Match?.ResetForNewMatch();
            TumbangPreso.GameServices.Round?.ResetForNewMatch();

            var clean = SceneManager.CreateScene(
                EmptySceneName + "_" + Time.frameCount.ToString());

            SceneManager.SetActiveScene(clean);

            // ⚠️ EVERY OTHER LOADED SCENE GOES, INCLUDING ADDITIVE ONES. `MapPreviewSurface` and
            // the match both load arenas additively and cache them, so "the previous scene" is
            // routinely more than one scene and unloading only the active one leaves an arena
            // lighting the next test's world. § 126.8's *"the sky and fog changing while the
            // geometry does not"* is the same shape of complaint one subsystem over.
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || scene == clean) continue;

                // An unloaded or never-loaded scene has nothing to unload and Unity logs an
                // error rather than ignoring it, which would fail the test that called this.
                if (!scene.isLoaded) continue;

                // ⚠️⚠️ NEVER THE RUNNER'S OWN SCENE, AND THIS WAS MISSING UNTIL 2026-09-03.
                // `MatchRunTests.SetUp` has carried the note since it was written: *"Unloading
                // that takes the test framework's objects with it and the run dies rather than
                // fails."* This method was unloading everything that was not its own clean
                // scene, `InitTestScene` included, on every one of the two hooks in every
                // fixture. The five fixtures that had it survived because the bootstrap scene
                // happens to be empty by the time a test runs; **that is luck rather than a
                // guarantee**, and a full suite that dies has no `.xml` at all, which
                // `CLAUDE.md` § 7 says is indistinguishable from a crash that exits 0.
                if (scene.name != null &&
                    scene.name.Contains("InitTestScene")) continue;

                // ⚠️⚠️ AND NEVER THE LAST ONE LEFT, WHICH IS A LOGGED ERROR RATHER THAN AN
                // EXCEPTION AND SO SLIPS PAST THE `catch` BELOW. Unity answers
                // *"Unloading the last loaded scene ... is not supported"* through `Debug.LogError`,
                // and the test framework fails a test on any unexpected error log, so the reset
                // would fail the test it was cleaning up after. It appeared once in each of the
                // two full runs on 2026-09-03, both times on `Eskinita`, which means `clean`
                // above was not counted at that instant. **The count is re-read every iteration
                // rather than trusted from the top of the loop**, because scenes are being
                // removed inside it.
                if (SceneManager.sceneCount <= 1) break;

                AsyncOperation unload = null;
                try
                {
                    unload = SceneManager.UnloadSceneAsync(scene);
                }
                catch (System.ArgumentException)
                {
                    // Unity refuses to unload the last loaded scene. `clean` above exists so
                    // that case cannot arise, and this catch is here so a future change to that
                    // ordering degrades into a slower reset rather than a red suite.
                }

                if (unload == null) continue;
                while (!unload.isDone) yield return null;
            }

            // ⚠️⚠️ THE PERSISTENT SCREENS ARE DELIBERATELY LEFT ALONE, AND A SWEEP THAT REMOVED
            // THEM WAS BUILT, MEASURED AND WITHDRAWN ON 2026-09-03. `docs/TODO.md` § 126.8d has
            // the numbers. The argument for it was sound and is still true: nine of the fifteen
            // `DontDestroyOnLoad` call sites in `Runtime/` are SCREENS and JOBS rather than
            // services, and an unloaded scene does not take them with it. **What the measurement
            // said is that removing them costs more than it buys.** With the sweep in, every
            // screen suite went green and eleven MATCH suites went red with *"the arena built no
            // SliceRunner"*, *"No main camera in the arena"*, *"the match built no slipper"* and
            // *"the guided route did not install"*.
            //
            // ⚠️ **A CHANGE THAT MOVES ELEVEN SUITES FROM ONE SIDE TO THE OTHER IS THE THING
            // § 126.8 EXISTS TO WARN ABOUT**, whichever direction it moves them: *"a gate whose
            // red set moves is not measuring the code."* Trading eleven screen failures for
            // eleven match failures is not progress, and shipping it would have hidden the trade
            // behind a slightly smaller total.
            //
            // ⚠️ IT IS RECORDED RATHER THAN DELETED SO NOBODY REBUILDS IT WITHOUT KNOWING. The
            // right version of this idea needs to know WHICH persistent object a match install
            // depends on, and that is a measurement nobody has taken yet.
            yield return null;

            Resources.UnloadUnusedAssets();
            yield return null;
        }

    }
}
