using System.Collections;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// WHICH WAY DOES THE CHARACTER PREVIEW TURN?
    ///
    /// ⚠️⚠️ THIS HAS NOW BEEN "FIXED" TWICE BY REASONING AND IS STILL WRONG, which is the whole
    /// case for measuring it. The signs were once copied straight from `character_preview.gd`,
    /// then flipped on an argument about handedness and about `PointerEventData.delta` counting
    /// up where Godot counts down. Both arguments are individually correct and the screen still
    /// turned the wrong way: 🧑, on the shipped build, *"i move model up, it goes down, i move
    /// left, it goes right"*.
    ///
    /// So this asserts the property the report is about, in the only terms it can be checked in.
    /// A drag is a GRAB: the surface of the subject nearest the camera follows the pointer. Take
    /// a point on that near surface, drag, and it must have moved the same way the pointer did.
    /// The point is fixed in WORLD space and only the camera moves, so this measures the picture
    /// rather than the intent.
    /// </summary>
    public class PreviewDragProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE SETUP HALF OF `docs/TODO.md` § 126.8'S FIX, AND THIS FIXTURE GETS ONLY THE
        /// SETUP HALF ON PURPOSE. `PlayModeWorld`'s header asks for both hooks; this class
        /// already owns a `[UnityTearDown]` doing its own cleanup, and NUnit does not define an
        /// order between two teardowns of the same kind. **The setup reset is the half that
        /// protects THIS fixture**: it guarantees the world is empty and settled when the test
        /// below starts, whatever ran before it. With every fixture in the folder carrying it,
        /// no test can inherit a world at all, which is the property the entry actually wants.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        /// <summary>
        /// How far the synthetic pointer travels, in pixels.
        ///
        /// ⚠️⚠️ IT WAS 40 AND THAT PUT THE ASSERTION ON A KNIFE EDGE. At 40 px the vertical drag
        /// moves the near face 0.0100 of a viewport against a required 0.0100, so the test
        /// decided on the last decimal place: a legitimate mesh change on the previewed
        /// character (the chamfer pass on `team-zack`, which pulls its near face in by up to
        /// 30 mm in model space) flipped it from just-passing to just-failing at 0.392978
        /// against 0.393014. That is a 0.009% miss reported as a broken drag.
        ///
        /// ⚠️ THE FIX IS A BIGGER DRAG, NOT A SMALLER MARGIN. This probe's own header says what
        /// it is for — *"it must have moved the same way the pointer did"* — so it is a test of
        /// DIRECTION, and loosening the epsilon toward zero would let an actually-stuck drag
        /// pass. A longer pointer travel keeps the margin meaningful and puts the measurement
        /// well clear of any one model's bounds. 120 px still lands inside the pitch clamp
        /// (`OrbitSensitivity` 0.4 gives 48 degrees against a range of roughly 71).
        /// </summary>
        private const float DragPixels = 120.0f;

        /// <summary>
        /// Take the scene down deliberately, and give Unity the frame it needs to do it.
        ///
        /// ⚠️⚠️ WITHOUT THIS, THIS PROBE FAILS ON SOMEBODY ELSE'S RUBBISH AND PASSES 1/1 ALONE.
        /// `docs/TODO.md` § 83.25 measured it: in the suite it goes red on an unhandled log
        /// message, *"Some objects were not cleaned up when closing the scene ...
        /// LobbyCastStage"*, which is `LobbyCast`'s stage. That object is parented into the
        /// additively loaded arena and destroyed BY NAME in `ConvertedMatchSetup.OnDestroy`;
        /// `Object.Destroy` is deferred to the end of the frame, so whether the destroy lands
        /// before the scene closes depends entirely on which test ran before this one and how
        /// its last frame ended.
        ///
        /// Loading an empty scene here ends this test's own MatchSetup on a frame this method
        /// controls, and the `yield return null` is what lets the deferred destroys actually
        /// run before the unload completes. Same shape as `TrainingStreetProbe.Quiesce` and
        /// `SoloPracticeTests`, both of which carry a note about live state outliving its scene
        /// and poisoning the NEXT suite. This is that fault from the receiving end.
        ///
        /// ⚠️⚠️ AND IT IS NOT `LogAssert.Expect`. § 83.25 says so outright: the message is true
        /// and the object it names is real. Silencing it would leave a real leak unreported and
        /// would go on hiding it for every probe that loads this screen afterwards.
        ///
        /// ⚠️ IT IS A `[UnityTearDown]` RATHER THAN A `[TearDown]` BECAUSE IT HAS TO YIELD. A
        /// plain `[TearDown]` cannot wait a frame, which is the only part of this that does any
        /// work.
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // ⚠️ A SCENE TO STAND IN FIRST. Unity refuses to unload the last loaded scene, and
            // the arena rides in ADDITIVELY beside MatchSetup, so "unload everything this test
            // opened" needs somewhere for the runner to live while it happens.
            var parking = SceneManager.CreateScene("PreviewDragProbeTeardown");
            SceneManager.SetActiveScene(parking);

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (scene == parking) continue;

                var unload = SceneManager.UnloadSceneAsync(scene);
                yield return ProbeWait.Done(unload, "scene unload");
            }

            // ⚠️ THE FRAME IS THE POINT OF THE WHOLE METHOD. `Object.Destroy` runs at the end of
            // a frame, so a teardown that returns without yielding leaves exactly the deferred
            // destroys this exists to drain.
            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingTheSubjectMovesItWithThePointer()
        {
            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 30; i++) yield return null;

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "MatchSetup has no CharacterSelectPanel.");

            panel.SetActive(true);

            for (int i = 0; i < 20; i++) yield return null;

            var preview = panel.GetComponentInChildren<ModelPreview>(true);
            Assert.IsNotNull(preview, "no ModelPreview on the character panel");
            Assert.IsNotNull(preview.Subject, "nothing to look at");

            var input = preview.GetComponentInChildren<ModelPreviewInput>(true);
            Assert.IsNotNull(input, "the preview surface has no input component");

            var cam = preview.PreviewCamera;
            Assert.IsNotNull(cam);

            // ---- HORIZONTAL ---------------------------------------------------------------
            Vector3 mark = NearSurfacePoint(cam, preview);
            Vector3 before = cam.WorldToViewportPoint(mark);

            input.OnDrag(Drag(new Vector2(DragPixels, 0.0f)));
            yield return null;

            Vector3 after = cam.WorldToViewportPoint(mark);

            Debug.Log($"[Drag] right {DragPixels}px: viewport x {before.x:F3} -> {after.x:F3}");

            Assert.Greater(after.x, before.x + 0.01f,
                "Dragging RIGHT moved the near face of the subject LEFT. The drag is a grab: " +
                "the surface under the pointer follows the pointer.");

            preview.ResetView();
            yield return null;

            // ---- VERTICAL -----------------------------------------------------------------
            mark = NearSurfacePoint(cam, preview);
            before = cam.WorldToViewportPoint(mark);

            // ⚠️ `PointerEventData.delta` COUNTS UP AS POSITIVE, so this is a drag UPWARDS.
            input.OnDrag(Drag(new Vector2(0.0f, DragPixels)));
            yield return null;

            after = cam.WorldToViewportPoint(mark);

            Debug.Log($"[Drag] up {DragPixels}px: viewport y {before.y:F3} -> {after.y:F3}");

            Assert.Greater(after.y, before.y + 0.01f,
                "Dragging UP moved the near face of the subject DOWN.");
        }

        /// <summary>
        /// A point on the subject's surface facing the camera. The near face is what a pointer
        /// is notionally holding on to, and it is the only part of an orbited subject whose
        /// screen motion has an unambiguous direction.
        /// </summary>
        private static Vector3 NearSurfacePoint(Camera cam, ModelPreview preview)
        {
            Vector3 aim = preview.Subject.transform.position + Vector3.up * 0.6f;
            Vector3 toCamera = (cam.transform.position - aim).normalized;

            return aim + toCamera * 0.30f;
        }

        private static PointerEventData Drag(Vector2 delta) =>
            new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                delta = delta,
            };

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                 FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;

            return null;
        }
    }
}
