using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The CHARACTER screen's model preview: framed to the panel, posed, and movable.
    ///
    /// ⚠️⚠️ ALL FOUR OF THESE SHIPPED BROKEN TOGETHER AND NONE OF THEM FAILED ANY CHECK. The
    /// report was *"model isnt movable and its stretched"*, against a screen whose own hint line
    /// reads "Drag to turn the view · scroll to zoom · right-click to reset":
    ///
    ///  1. Nothing in the project ever called `Orbit` or `Zoom`, so all three controls were lies.
    ///  2. The render target was a fixed 512x640 stretched across a panel of another shape.
    ///  3. The framing fitted height alone and ignored the aspect entirely.
    ///  4. No clip was playing, so the subject stood in its bind pose with its arms straight out.
    ///
    /// The assertions below are the mechanism rather than the picture, because a picture cannot
    /// fail a build. The pictures are written beside them for the reader.
    /// </summary>
    public class ModelPreviewTests
    {
        private const string OutDir = "Logs/shots-preview";

        [UnityTest]
        public IEnumerator TheCharacterPreviewIsFramedPosedAndMovable()
        {
            Directory.CreateDirectory(OutDir);

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 30; i++) yield return null;

            var panel = Find("CharacterSelectPanel");
            Assert.IsNotNull(panel, "MatchSetup has no CharacterSelectPanel to open.");

            panel.SetActive(true);

            // The preview builds its target from the panel's rect, which is 0 until the first
            // layout pass, and re-frames on the frame after a subject is shown.
            for (int i = 0; i < 20; i++) yield return null;

            var preview = panel.GetComponentInChildren<ModelPreview>(true);
            Assert.IsNotNull(preview, "The character panel built no ModelPreview.");

            Assert.IsNotNull(preview.Subject,
                "Nothing was instanced to look at. The roster book has no model for this pick.");

            // ---- 2. THE PICTURE IS NOT SQUASHED --------------------------------------------
            var rect = ((RectTransform)preview.transform).rect;
            Assert.IsNotNull(preview.Target, "The preview has no render target.");

            float panelAspect = rect.width / rect.height;
            float targetAspect = (float)preview.Target.width / preview.Target.height;

            Assert.AreEqual(panelAspect, targetAspect, 0.02f,
                $"The render target is {preview.Target.width}x{preview.Target.height} on a " +
                $"{rect.width:F0}x{rect.height:F0} panel, so every subject is stretched by the " +
                "ratio between them.");

            // ---- 4. THE SUBJECT IS POSED, NOT IN ITS BIND POSE ------------------------------
            var animator = preview.Subject.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator,
                "No Animator on the previewed model, so no clip can be playing and the screen " +
                "shows the rig's T-pose.");

            // ⚠️ THE POSE MUST ACTUALLY MOVE. An Animator with a null Avatar accepts an
            // animation output and drives nothing, so "there is an Animator" and "a clip is
            // playing" are different claims and only the second one matters. The rig's bind
            // pose is arms straight out, which is the T-pose the screen must never show.
            var bone = DeepestChild(preview.Subject.transform);
            Quaternion pose = bone.localRotation;

            for (int i = 0; i < 30; i++) yield return null;

            Assert.Greater(Quaternion.Angle(pose, bone.localRotation), 0.01f,
                $"'{bone.name}' has not moved in 30 frames, so no clip is playing and the " +
                "preview is showing the rig's bind pose.");

            Capture("character-person");

            // ---- 1. A DRAG REACHES IT ------------------------------------------------------
            var input = preview.GetComponentInChildren<ModelPreviewInput>(true);
            Assert.IsNotNull(input,
                "The preview surface has no input component, so the panel's own hint line " +
                "promises three controls that do not exist.");

            var camera = preview.PreviewCamera;
            Assert.IsNotNull(camera);

            Quaternion before = camera.transform.rotation;

            input.OnDrag(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                delta = new Vector2(90.0f, 0.0f),
            });

            yield return null;

            Assert.Greater(Quaternion.Angle(before, camera.transform.rotation), 1.0f,
                "Dragging the preview did not move the camera.");

            Capture("character-dragged");

            // ---- ZOOM ----------------------------------------------------------------------
            float distance = Vector3.Distance(camera.transform.position, Vector3.zero);

            input.OnScroll(new PointerEventData(EventSystem.current)
            {
                scrollDelta = new Vector2(0.0f, 1.0f),
            });

            yield return null;

            Assert.AreNotEqual(distance,
                Vector3.Distance(camera.transform.position, Vector3.zero),
                "The wheel did not dolly the preview camera.");

            // ---- RIGHT-CLICK RESTORES THE FRAMED SHOT --------------------------------------
            input.OnPointerClick(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Right,
            });

            yield return null;

            // ⚠️ A FEW DEGREES OF SLACK, ON PURPOSE. The framing is re-measured from the POSED
            // bounds, and the idle clip is still running, so the subject's height-to-width ratio
            // drifts slightly between frames and the pitch lerp follows it. The claim being made
            // is "right-click puts the shot back", not "to the bit".
            Assert.Less(Quaternion.Angle(before, camera.transform.rotation), 5.0f,
                "Right-click did not restore the auto-framed shot.");

            Capture("character-reset");
        }

        /// <summary>
        /// The cast animates in a real match.
        ///
        /// ⚠️⚠️ "THE WHOLE CAST STANDS PERFECTLY STILL" HAS BITTEN THIS PORT TWICE, once because
        /// the clips were stripped from the build and once because glTFast emits an Animator
        /// with no Avatar and an animation output bound to one drives nothing at all. Neither
        /// failure logs anything: the characters simply stand in their bind pose, which on these
        /// rigs is arms out, and it reads as unfinished art rather than as a bug. This asserts
        /// the only thing that actually distinguishes the two: a bone that moves.
        /// </summary>
        [UnityTest]
        public IEnumerator TheCastAnimatesInAMatch()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 20; i++) yield return null;

            var seat = Object.FindFirstObjectByType<CharacterMotor>();
            Assert.IsNotNull(seat, "The arena built no seats.");

            var skinned = seat.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsNotNull(skinned, "The seat has no skinned model.");

            var animator = seat.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator, "The seat's model has no Animator.");

            Assert.IsNotNull(animator.avatar,
                "The Animator has no Avatar, so its animation output binds to nothing and the " +
                "character stands in its bind pose for the whole match.");

            var bone = DeepestChild(skinned.transform.root);
            Quaternion pose = bone.localRotation;

            for (int i = 0; i < 40; i++) yield return null;

            Assert.Greater(Quaternion.Angle(pose, bone.localRotation), 0.01f,
                $"'{bone.name}' has not moved in 40 frames of a live match.");
        }

        /// <summary>A bone well down the rig, so the sample is a limb rather than the root the
        /// clip may deliberately leave still.</summary>
        private static Transform DeepestChild(Transform root)
        {
            Transform best = root;
            int depth = 0;

            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                int d = 0;
                for (var step = t; step != root && step != null; step = step.parent) d++;

                if (d <= depth) continue;

                depth = d;
                best = t;
            }

            return best;
        }

        private static GameObject Find(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hit = FindIn(root.transform, name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static GameObject FindIn(Transform t, string name)
        {
            if (t.name == name) return t.gameObject;

            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindIn(t.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }

        /// <summary>
        /// ⚠️ AN OVERLAY CANVAS IS INVISIBLE TO Camera.Render, so it is flipped to
        /// ScreenSpaceCamera first and put in front of the near plane. Same rule UiRuntimeShots
        /// carries, and the reason a capture can come back as an empty scene.
        /// </summary>
        private static void Capture(string name)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = cam.nearClipPlane + 0.01f;
            }

            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = prev;

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
