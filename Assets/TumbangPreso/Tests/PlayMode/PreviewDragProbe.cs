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
        [UnityTest]
        public IEnumerator DraggingTheSubjectMovesItWithThePointer()
        {
            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

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

            input.OnDrag(Drag(new Vector2(40.0f, 0.0f)));
            yield return null;

            Vector3 after = cam.WorldToViewportPoint(mark);

            Debug.Log($"[Drag] right 40px: viewport x {before.x:F3} -> {after.x:F3}");

            Assert.Greater(after.x, before.x + 0.01f,
                "Dragging RIGHT moved the near face of the subject LEFT. The drag is a grab: " +
                "the surface under the pointer follows the pointer.");

            preview.ResetView();
            yield return null;

            // ---- VERTICAL -----------------------------------------------------------------
            mark = NearSurfacePoint(cam, preview);
            before = cam.WorldToViewportPoint(mark);

            // ⚠️ `PointerEventData.delta` COUNTS UP AS POSITIVE, so this is a drag UPWARDS.
            input.OnDrag(Drag(new Vector2(0.0f, 40.0f)));
            yield return null;

            after = cam.WorldToViewportPoint(mark);

            Debug.Log($"[Drag] up 40px: viewport y {before.y:F3} -> {after.y:F3}");

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
