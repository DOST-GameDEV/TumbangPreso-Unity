using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Verifies that when a character model is rebuilt/swapped (e.g. via SyncPicksClientRpc,
    /// multiplayer round start, or roster selection change), the FPP camera rig re-applies
    /// self-hide (ShadowsOnly) on all newly instanced renderers so the camera never sits
    /// inside the character's head mesh.
    /// </summary>
    public class MultiplayerModelSwapSelfHideProbe
    {
        [UnityTest]
        public IEnumerator ModelRebuildMaintainsFppSelfHide()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 20; i++) yield return null;

            var rig = Object.FindFirstObjectByType<CameraRig>();
            Assert.IsNotNull(rig, "no CameraRig found in scene");

            var motor = Object.FindFirstObjectByType<CharacterMotor>();
            Assert.IsNotNull(motor, "no CharacterMotor found in scene");

            var visual = motor.GetComponent<CharacterVisual>();
            Assert.IsNotNull(visual, "no CharacterVisual on motor");

            rig.Follow(motor);
            yield return null;

            // Verify initial state is ShadowsOnly
            var initialRenderers = visual.GetComponentsInChildren<Renderer>(includeInactive: true);
            Assert.IsTrue(initialRenderers.Length > 0, "motor has no renderers");
            foreach (var r in initialRenderers)
            {
                Assert.AreEqual(ShadowCastingMode.ShadowsOnly, r.shadowCastingMode,
                    "initial renderer is not ShadowsOnly in FPP");
            }

            // Simulate multiplayer round start / pick sync by swapping model via RosterBook
            var book = RosterBook.Load();
            Assert.IsNotNull(book, "RosterBook could not be loaded");

            // Pick a different character index (e.g. index 2)
            int newPick = (motor.CharacterIndex + 1) % 4;
            var art = book.PersonArt(newPick, UI.SceneFlow.SelectedMode);
            Assert.IsNotNull(art, "PersonArt not found");
            Assert.IsNotNull(art.Model, "PersonArt model is null");

            visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);

            // Give a frame for LateUpdate
            yield return null;

            var newRenderers = visual.GetComponentsInChildren<Renderer>(includeInactive: true);
            Assert.IsTrue(newRenderers.Length > 0, "swapped model has no renderers");

            foreach (var r in newRenderers)
            {
                Assert.AreEqual(ShadowCastingMode.ShadowsOnly, r.shadowCastingMode,
                    $"Renderer '{r.name}' on rebuilt model is {r.shadowCastingMode} instead of ShadowsOnly! " +
                    "This causes the camera POV to render inside the character's head in FPP.");
            }
        }
    }
}
