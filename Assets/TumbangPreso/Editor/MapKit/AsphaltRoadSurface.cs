using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Lays one textured plane over Eskinita's road, so the surface can carry a real asphalt
    /// texture instead of a flat colour.
    ///
    /// ⚠️⚠️ THE KIT ROAD CANNOT BE TEXTURED AND THAT IS WHY THIS EXISTS. `Kalsada_*` are
    /// instances of `Art/models/kits/town/road.glb`, a Kenney town-kit model. The whole kit is
    /// coloured from ONE shared atlas, `kits/town/Textures/colormap.png`, so the road mesh's UVs
    /// do not span 0..1: they point at a single small swatch cell inside that atlas. Assigning
    /// any other texture makes those same UVs sample a few pixels of it and stretch them over the
    /// entire slab, which reads as one flat colour and looks exactly like "the texture is not
    /// showing". Raising the tiling does not help, it only slides which few pixels get sampled.
    /// This is the same atlas trick the character rigs use, documented at length in `Toon.shader`.
    ///
    /// ⚠️ A QUAD HAS THE UVs THE KIT MESH DOES NOT. Unity's built-in quad is one unit square with
    /// texture coordinates running the full 0..1 across it, so a tiling multiplier repeats the
    /// image properly. That is the entire reason this is a new primitive rather than a material
    /// swap on the existing geometry.
    ///
    /// ⚠️⚠️ IT COVERS, IT DOES NOT DELETE. The kit road is left in the scene and merely switched
    /// off, so the change is one checkbox to undo and the original geometry is still there to
    /// compare against. Deleting map geometry to try a texture is not a reversible experiment.
    /// `Run` with the road already hidden re-hides it and rebuilds the plane, so it is idempotent.
    ///
    /// ⚠️ THE SIZE IS MEASURED FROM THE ROAD'S OWN RENDERER BOUNDS RATHER THAN TYPED IN. The
    /// tiles sit at scattered positions (30.6, 0.1, 6), (-10.89, 0.1, ...) at 5x scale, and a
    /// hand-copied extent would be wrong the first time a tile moved. Encapsulating the real
    /// bounds is exact and stays exact.
    /// </summary>
    public static class AsphaltRoadSurface
    {
        private const string TexturePath = "Assets/TumbangPreso/Art/models/textures/asphalt.png";
        private const string MaterialPath = "Assets/TumbangPreso/Art/models/materials/AsphaltRoad.mat";
        private const string ObjectName = "AsphaltSurface";

        /// <summary>
        /// Every map that has a road, and the node its road lives under.
        ///
        /// ⚠️⚠️ THIS WAS HARD-CODED TO ESKINITA, AND THAT IS THE WHOLE OF "the other maps look
        /// flat". 🧑 2026-08-29, pointing at an Eskinita frame: *"can u give all other maps rich
        /// floors like this? borrow from online assets if u have to"*. **Nothing needed to be
        /// borrowed.** The texture, the material and this entire generator already existed and
        /// had simply only ever been pointed at one of the three scenes, so Bayan Plaza and
        /// Ilalim ng Tulay were still showing the bare Kenney kit road — a single flat swatch
        /// sampled out of the shared town atlas, which is exactly the "no texture" look this
        /// class's own header describes.
        ///
        /// ⚠️ SO THE LICENSING QUESTION DOES NOT ARISE, and that matters more than the work did.
        /// `CLAUDE.md` § 6 says the art is the team's own, this repo tracks provenance in
        /// `NEW_SLIPPER_LICENSES.txt`, and this is a competition entry going to a national final.
        /// A downloaded texture with the wrong licence is very hard to undo once it is in a
        /// submitted binary. `asphalt.png` is already in the tree and already shipping.
        ///
        /// ⚠️ THE GROUP NAME IS PER MAP BECAUSE THE BUILDERS DISAGREE. Eskinita and Ilalim ng
        /// Tulay both call the node `Kalsada`; `EnvColourPass.RoadGroups` is the list of every
        /// name a road node is allowed to have (`Kalsada`, `Road`, `Slab`, `Apron`) and is the
        /// authority for anything added later. A map whose group is missing is REPORTED and
        /// skipped rather than failing the run, because Bayan Plaza is a plaza and may legitimately
        /// not have a road node at all.
        /// </summary>
        /// ⚠️⚠️ ILALIM NG TULAY AND BAYAN PLAZA ARE COMMENTED OUT, AND THE ATTEMPT IS RECORDED
        /// RATHER THAN DELETED BECAUSE IT ALMOST WORKED. Running all three laid the surface fine
        /// on Eskinita and **failed `MapGeometryCheck` on a gated map**:
        ///
        ///     FAIL IlalimNgTulay/AsphaltSurface: floats 0.061 m above .../Lupa/FarGroundPlate
        ///          (footprint 80.00 by 240.00 m)
        ///
        /// **80 by 240 metres.** Eskinita's `Kalsada` group is the playable street and nothing
        /// else, so encapsulating its renderer bounds gives a quad the size of the road. Ilalim
        /// ng Tulay's `Kalsada` also holds the far backdrop road that runs out to the fog line, so
        /// the same measurement produces a sheet covering the entire map, hovering a millimetre
        /// over the ground plates it swallowed. The bounds-from-renderers trick this class is
        /// built on is correct for one map's authoring and wrong for the other's.
        ///
        /// ⚠️ SO THE FIX IS A PLAYABLE-AREA BOUND, NOT A GROUP BOUND. `CONFINEMENT_RADIUS` is 7.0
        /// and the chalk ring is measured in `IlalimNgTulayBuilder`; a surface sized to the arena
        /// plus a margin would be right on every map and would not depend on how a builder chose
        /// to group its distant geometry. That is the work, and it needs a render to sign off.
        private static readonly (string Scene, string Group)[] Maps =
        {
            ("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity", "Kalsada"),
        };

        /// <summary>
        /// How many metres of road one copy of the texture covers. 4 m is roughly a car length,
        /// so the grain reads as asphalt at walking distance without the repeat becoming obvious
        /// down the length of the street.
        /// </summary>
        private const float MetresPerTile = 4.0f;

        /// <summary>
        /// ⚠️ IT SITS ABOVE THE KIT ROAD, NOT ON IT. The tiles' top face is at y = 0.100
        /// (measured by `MapGeometryCheck`, which reports `top y=0.100` for every `Kalsada_*`).
        /// Two coplanar surfaces z-fight into a shimmering mess that looks like a shader bug, so
        /// the plane is lifted by a millimetre. That is far below anything the player can see and
        /// far above the depth buffer's resolution at this range.
        /// </summary>
        private const float LiftMetres = 0.001f;

        [MenuItem("Tumbang Preso/Lay Asphalt Road Surface on Every Map")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        /// <summary>
        /// Every map in <see cref="Maps"/>, in one editor launch.
        ///
        /// ⚠️ ONE LAUNCH FOR ALL THREE, because the launch is the cost of a pass and not the work.
        /// `Checks.RunAll` makes the same argument for the five editor checks.
        ///
        /// ⚠️ A MAP WITH NO ROAD NODE IS SKIPPED, NOT FAILED. See the note on `Maps`.
        /// </summary>
        public static bool Execute()
        {
            if (!File.Exists(TexturePath))
            {
                Debug.LogError($"[AsphaltRoad] no texture at {TexturePath}.");
                return false;
            }

            ConfigureTexture();

            bool all = true;
            int laid = 0;

            foreach (var (scenePath, group) in Maps)
            {
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning($"[AsphaltRoad] no scene at {scenePath}, skipped.");
                    continue;
                }

                if (ExecuteOne(scenePath, group)) laid++;
                else all = false;
            }

            Debug.Log($"[AsphaltRoad] laid a textured surface on {laid} of {Maps.Length} maps.");
            return all;
        }

        private static bool ExecuteOne(string ScenePath, string RoadGroup)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var road = Find(RoadGroup);
            if (road == null)
            {
                // ⚠️ A WARNING RATHER THAN AN ERROR, and it returns TRUE. Bayan Plaza is a plaza:
                // it is allowed not to have a road node, and a run that "fails" on that would
                // make this tool impossible to put in a pipeline.
                Debug.LogWarning($"[AsphaltRoad] no '{RoadGroup}' group in {ScenePath}, skipped.");
                return true;
            }

            // Measure before anything is hidden: a disabled renderer still reports bounds, but
            // measuring first keeps the order of operations obvious.
            var renderers = road.GetComponentsInChildren<MeshRenderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogError($"[AsphaltRoad] '{RoadGroup}' has no renderers to measure.");
                return false;
            }

            var bounds = renderers[0].bounds;
            int counted = 0;

            foreach (var r in renderers)
            {
                if (r == null || r.gameObject.name == ObjectName) continue;
                bounds.Encapsulate(r.bounds);
                counted++;
            }

            var material = BuildMaterial(bounds.size);
            if (material == null) return false;

            // ⚠️⚠️ IT IS PARENTED OUTSIDE `Dressing`, AND PUTTING IT INSIDE TURNED THE ROAD BLACK.
            // `EnvColourPass.DressingRoot()` returns the `Dressing` child and walks only that
            // subtree, so every renderer under it is classified by its layer node and tinted.
            // `Kalsada` is in `RoadGroups`, so a plane parented there was multiplied by `RoadTint`
            // (0.66, 0.62, 0.55) on top of a texture that is already dark: measured mean luma
            // 67.6 of 255, so 27 per cent, knocked to about 17 by the tint and then crushed again
            // by the tonemap. The result rendered as flat black with no grain visible at all.
            //
            // ⚠️ AND THE TINT IS THE WRONG CORRECTION FOR THIS SURFACE, not merely too strong.
            // `RoadTint` exists to pull the KIT road's flat swatch toward the Godot reference
            // frame's road (R 79, G 69, B 71). A photographed asphalt texture already sits in that
            // range on its own, so the correction is being applied twice to something that was
            // never wrong. A sibling of `Dressing` is never walked, which is the cheapest way to
            // say "this surface is not kit dressing" without teaching the pass a new exemption.
            var mapRoot = road.parent != null && road.parent.parent != null
                ? road.parent.parent
                : road.parent ?? road;

            var existing = mapRoot.Find(ObjectName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var stale = road.Find(ObjectName);
            if (stale != null) Object.DestroyImmediate(stale.gameObject);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = ObjectName;
            quad.transform.SetParent(mapRoot, worldPositionStays: true);

            quad.transform.position = new Vector3(bounds.center.x,
                                                  bounds.max.y + LiftMetres,
                                                  bounds.center.z);

            // Unity's quad faces -Z. Ninety degrees about X lays it flat, facing up.
            quad.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            quad.transform.localScale = new Vector3(bounds.size.x, bounds.size.z, 1.0f);

            var collider = quad.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            // ⚠️ NO SHADOW CASTING. It is a skin a millimetre over a surface that already casts
            // one; two coincident casters produce acne along every shadow edge on the street.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;

            int hidden = 0;

            foreach (var r in renderers)
            {
                if (r == null || r.gameObject.name == ObjectName) continue;
                if (!r.enabled) continue;

                r.enabled = false;
                hidden++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[AsphaltRoad] measured {counted} kit tiles to {bounds.size.x:F1} x " +
                      $"{bounds.size.z:F1} m, laid {ObjectName} at y={bounds.max.y + LiftMetres:F3}, " +
                      $"hid {hidden} kit renderers. Re-enable them to undo.");

            return true;
        }

        private static Transform Find(string name)
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = Search(root.transform, name);
                if (found != null) return found;
            }

            return null;
        }

        private static Transform Search(Transform t, string name)
        {
            if (t.name == name) return t;

            for (int i = 0; i < t.childCount; i++)
            {
                var found = Search(t.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        /// ⚠️ `Repeat`, NOT `Clamp`, AND IT IS THE WHOLE POINT OF THIS OBJECT. The tiling
        /// multiplier below is what makes the grain read as asphalt rather than as one stretched
        /// photograph, and a clamped texture ignores every repeat past the first.
        /// </summary>
        private static void ConfigureTexture()
        {
            if (!(AssetImporter.GetAtPath(TexturePath) is TextureImporter importer)) return;

            bool changed = false;

            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                changed = true;
            }

            if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }

            // ⚠️ ANISOTROPY EARNS ITS COST ON A ROAD AND ALMOST NOWHERE ELSE. This surface is
            // viewed at a grazing angle from eye height for the whole match, which is the exact
            // case trilinear filtering blurs into mush a few metres out.
            if (importer.anisoLevel < 4) { importer.anisoLevel = 4; changed = true; }

            if (changed) importer.SaveAndReimport();
        }

        private static Material BuildMaterial(Vector3 size)
        {
            var shader = Shader.Find("Standard");

            if (shader == null)
            {
                Debug.LogError("[AsphaltRoad] the Standard shader is missing from this project.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            if (material == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);

            // ⚠️ TILING FOLLOWS THE MEASURED SIZE, so the grain stays the same physical size
            // whatever the road turns out to be. A fixed pair of numbers here would stretch the
            // moment the map grew.
            material.mainTextureScale = new Vector2(size.x / MetresPerTile, size.z / MetresPerTile);

            // ⚠️ ASPHALT IS NOT SHINY. Standard defaults to 0.5 smoothness, which puts a wet
            // sheen down a dry street and reads as rain.
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.05f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.0f);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return material;
        }
    }
}
