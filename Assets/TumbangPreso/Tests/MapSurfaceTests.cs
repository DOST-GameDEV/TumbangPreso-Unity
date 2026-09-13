using System.Collections.Generic;
using System.Linq;
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
        public void ShopSignBackingsActuallyMeetTheirOwnFascia()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                int checkedSigns=0;
                foreach(var root in scene.GetRootGameObjects())
                foreach(var backing in root.GetComponentsInChildren<MeshRenderer>())
                {
                    if(!backing.name.StartsWith("Shop sign backing ")&&!backing.name.StartsWith("Banner backing "))continue;
                    var frontage=backing.transform.parent;
                    var fascia=frontage.Find("Roof slab").GetComponent<Renderer>();
                    var direction=frontage.forward;
                    var b=backing.bounds;var f=fascia.bounds;
                    float back=Vector3.Dot(b.center,direction)-Mathf.Abs(direction.x)*b.extents.x-Mathf.Abs(direction.z)*b.extents.z;
                    float front=Vector3.Dot(f.center,direction)+Mathf.Abs(direction.x)*f.extents.x+Mathf.Abs(direction.z)*f.extents.z;
                    Assert.That(back-front,Is.EqualTo(.003f).Within(.001f),backing.name+" floats in front of its fascia");
                    checkedSigns++;
                }
                Assert.AreEqual(11,checkedSigns);
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }

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
                foreach(var continuation in all.Where(t=>t.name=="RoadContinuationNorth"||t.name=="RoadContinuationSouth"))
                {
                    var roadBounds=continuation.GetComponent<Renderer>().bounds;
                    Assert.LessOrEqual(bounds.min.z,roadBounds.min.z+.02f,"Asphalt stops before the visible south road");
                    Assert.GreaterOrEqual(bounds.max.z,roadBounds.max.z-.02f,"Asphalt stops before the visible north road");
                }
                var crosses=all.Where(t=>t.name.StartsWith("BackgroundCrossroad_")&&t.name.EndsWith("_Surface")).ToArray();
                Assert.AreEqual(4,crosses.Length,"All four visible cross-street arms need the same road surface");
                foreach(var part in crosses.Concat(surfaces))
                {
                    var r=part.GetComponent<Renderer>();var material=r.sharedMaterial;
                    Assert.AreSame(renderer.sharedMaterial.mainTexture,material.mainTexture);
                    Assert.AreEqual(r.bounds.size.x/4f,material.mainTextureScale.x,.01f);
                    Assert.AreEqual(r.bounds.size.z/4f,material.mainTextureScale.y,.01f);
                    Assert.AreEqual(bounds.max.y,r.bounds.max.y,.002f);
                }
                Assert.AreEqual(0.001f, bounds.min.y, 0.002f);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
