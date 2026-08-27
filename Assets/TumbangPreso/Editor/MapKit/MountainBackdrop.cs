using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Stands a flat painted mountain behind Eskinita, as a cutout billboard.
    ///
    /// ⚠️⚠️ IT IS `Unlit/Transparent` AND THAT CHOICE IS LOAD-BEARING, NOT LAZINESS. Two separate
    /// things depend on it and both would be wrong on a cutout `Standard` material:
    ///
    ///  1. **THE WORLD INK WOULD DRAW A RECTANGLE ROUND IT.** `Visual.WorldOutline` finds edges in
    ///     `_CameraDepthNormalsTexture`, which the built-in pipeline fills through Unity's
    ///     `Internal-DepthNormalsTexture` REPLACEMENT shader, selected by the `RenderType` tag. A
    ///     replacement shader brings its own fragment code, so an alpha `clip()` written in the
    ///     material's own shader is structurally invisible to it: the prepass would record the
    ///     full QUAD, and the outline would ink a hard box around the sky. `Unlit/Transparent`
    ///     tags `RenderType = "Transparent"`, which keeps the quad out of the prepass entirely, so
    ///     the only thing that can be inked is the geometry actually behind it.
    ///  2. **A LIT BACKDROP TRACKS THE SUN AND A PAINTING SHOULD NOT.** The art is already shaded,
    ///     with its own highlights painted in. Running it through a BRDF would multiply that by
    ///     the arena's key light and darken one side of a mountain that is supposed to read as
    ///     distance rather than as an object in the scene.
    ///
    /// ⚠️ IT IS PARENTED UNDER `Malayo`, WHICH IS WHERE THE MAP ALREADY PUTS ITS FAR DRESSING, and
    /// deliberately NOT under `Dressing`. `MapGeometryCheck` walks `Dressing` and fails anything
    /// whose underside floats over open air. A backdrop is 60 m up by definition, so parenting it
    /// there would add a permanent red to a check whose whole value is that it is green.
    ///
    /// ⚠️ THE QUAD KEEPS THE IMAGE'S OWN ASPECT. `Mountain.png` is 3852 x 2000, so 1.926:1, and a
    /// backdrop stretched off its own aspect is the one error in this that nobody notices until it
    /// is next to a screenshot of the original.
    ///
    /// ⚠️ SHADOWS OFF, BOTH WAYS. A vertical quad 60 m up casts a hard band across the whole
    /// street when the sun is low, and it has no business receiving one either.
    ///
    /// Run it from the menu, or:
    ///   Unity.exe -batchmode -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.MapKit.MountainBackdrop.Run
    /// </summary>
    public static class MountainBackdrop
    {
        private const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const string TexturePath = "Assets/TumbangPreso/Art/models/textures/Mountain.png";
        private const string MaterialPath = "Assets/TumbangPreso/Art/models/textures/Mountain.mat";
        private const string ObjectName = "MountainBackdrop";

        /// <summary>Where the far dressing already lives. See the class note.</summary>
        private const string FarGroup = "Malayo";

        /// <summary>
        /// ⚠️ THESE ARE A STARTING POSE, NOT A MEASURED VALUE, AND THEY ARE MEANT TO BE DRAGGED.
        /// Everything else in this file is reasoned from the asset or from a rule; this is taste,
        /// and taste belongs to whoever is looking at it. The script is idempotent and re-running
        /// it MOVES an existing backdrop back to these numbers, so edit them here rather than in
        /// the scene if a placement is worth keeping.
        ///
        /// 150 m out on +Z with the arena only 7 m across (`Balance.ConfinementRadius`) puts it
        /// far past every building, so it reads as landscape rather than as scenery on the street.
        /// </summary>
        private static readonly Vector3 Position = new Vector3(0.0f, 34.0f, 150.0f);

        /// <summary>Width in metres. The height follows from the image's aspect.</summary>
        private const float WidthMetres = 260.0f;

        [MenuItem("Tumbang Preso/Add Mountain Backdrop to Eskinita")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            if (!File.Exists(TexturePath))
            {
                Debug.LogError($"[MountainBackdrop] no texture at {TexturePath}.");
                return false;
            }

            ConfigureTexture();

            var material = BuildMaterial();
            if (material == null) return false;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var far = FindFarGroup();
            if (far == null)
            {
                Debug.LogError($"[MountainBackdrop] no '{FarGroup}' group in {ScenePath}. The far " +
                               "dressing group was renamed; parenting a backdrop under Dressing " +
                               "would fail MapGeometryCheck, so nothing was added.");
                return false;
            }

            var existing = far.Find(ObjectName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = ObjectName;
            quad.transform.SetParent(far, false);
            quad.transform.localPosition = Position;

            // Faces back down the street toward the arena.
            quad.transform.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            float aspect = texture == null || texture.height == 0
                ? 1.926f
                : (float)texture.width / texture.height;

            quad.transform.localScale = new Vector3(WidthMetres, WidthMetres / aspect, 1.0f);

            // ⚠️ THE COLLIDER GOES. `CreatePrimitive` attaches one, and a 260 m box in the sky
            // would sit in every physics query the match runs, including the throw arc.
            var collider = quad.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // ⚠️ NOT LIGHTMAPPED AND NOT A PROBE RECEIVER, for the same reason it is unlit: it is
            // a painting, and every one of those systems exists to make an object sit in the
            // scene's light.
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[MountainBackdrop] placed {ObjectName} under {FarGroup} at {Position}, " +
                      $"{WidthMetres:F0} x {WidthMetres / aspect:F0} m, aspect {aspect:F3}.");

            return true;
        }

        /// <summary>
        /// ⚠️ IT SEARCHES THE WHOLE HIERARCHY, NOT THE ROOTS AND THEIR CHILDREN. The first version
        /// used `Transform.Find`, which only looks one level down, and reported `Malayo` missing on
        /// a scene that plainly contains it. These maps came through `TscnImporter` from Godot and
        /// keep that tree's nesting, so a named group can sit several levels deep and there is no
        /// depth this is safe to assume.
        /// </summary>
        private static Transform FindFarGroup()
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = Search(root.transform);
                if (found != null) return found;
            }

            return null;
        }

        private static Transform Search(Transform t)
        {
            if (t.name == FarGroup) return t;

            for (int i = 0; i < t.childCount; i++)
            {
                var found = Search(t.GetChild(i));
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        /// ⚠️ `alphaIsTransparency` IS THE ONE THAT MATTERS AND IT IS OFF BY DEFAULT. Without it
        /// Unity leaves the colour of fully transparent texels alone, and this file's transparent
        /// area is WHITE (measured: the corner texel is 255,255,255 at alpha 0). Bilinear
        /// filtering then blends that white into every edge of the mountain and the silhouette
        /// gets a pale fringe against the sky. With it set, Unity bleeds the visible colour
        /// outward into the transparent region before filtering, which is exactly the fix.
        ///
        /// ⚠️ AND THE WRAP MODE IS CLAMP. A backdrop sampled a hair outside 0..1 at its own edge
        /// wraps the far side of the mountain into a one-pixel stripe down the opposite edge.
        /// </summary>
        private static void ConfigureTexture()
        {
            if (!(AssetImporter.GetAtPath(TexturePath) is TextureImporter importer)) return;

            bool changed = false;

            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
            if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                changed = true;
            }

            if (changed) importer.SaveAndReimport();
        }

        private static Material BuildMaterial()
        {
            var shader = Shader.Find("Unlit/Transparent");

            if (shader == null)
            {
                Debug.LogError("[MountainBackdrop] Unlit/Transparent is missing from this project.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return material;
        }
    }
}
