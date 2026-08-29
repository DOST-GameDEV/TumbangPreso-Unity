using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The road surface the player actually sees, asserted from the saved scene.
    ///
    /// ⚠️⚠️ THE SCENE IS THE PRODUCT. `IlalimNgTulayBuilder` and `AsphaltRoadSurface` can both
    /// be correct while the saved map is stale, which is the same split `MapGradeSanityTests`
    /// guards for the camera grade. This opens the authored scene and measures its renderer so
    /// the test cannot pass on a generator that was never run.
    /// </summary>
    public sealed class MapSurfaceTests
    {
        private const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity";

        [Test]
        public void IlalimUsesOneContinuousAsphaltSkinAndNoPatchSlabs()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var all = new List<Transform>();

                foreach (var root in scene.GetRootGameObjects())
                    all.AddRange(root.GetComponentsInChildren<Transform>(true));

                var surfaces = all.FindAll(t => t.name == "AsphaltSurface");
                var patches = all.FindAll(t => t.name.StartsWith("AsphaltPatch_"));

                Assert.AreEqual(1, surfaces.Count,
                    "Ilalim must ship one continuous AsphaltSurface. A missing surface restores "
                    + "the flat kit swatch; two surfaces z-fight.");
                Assert.AreEqual(0, patches.Count,
                    "the dark rectangular AsphaltPatch slabs were explicitly removed from the road");

                Transform surface = surfaces[0];
                var renderer = surface.GetComponent<MeshRenderer>();

                Assert.IsNotNull(renderer, "AsphaltSurface has no renderer");
                Assert.IsNotNull(renderer.sharedMaterial, "AsphaltSurface has no material");
                Assert.IsNotNull(renderer.sharedMaterial.mainTexture,
                    "AsphaltSurface is still a flat colour rather than textured asphalt");
                Assert.AreEqual("asphalt", renderer.sharedMaterial.mainTexture.name);
                Assert.IsNull(surface.GetComponent<Collider>(),
                    "the asphalt skin is visual only and must not add a second floor collider");

                Bounds bounds = renderer.bounds;

                // The kerb lines are also the chalk lines at x = +/-7. The end walls are at
                // z = +/-16.5 and the skin carries two metres beyond each, so its seam stays
                // behind the wall without swallowing the 80 by 240 m backdrop again.
                Assert.AreEqual(14.0f, bounds.size.x, 0.02f);
                Assert.AreEqual(37.0f, bounds.size.z, 0.02f);
                Assert.AreEqual(0.001f, bounds.min.y, 0.002f);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
