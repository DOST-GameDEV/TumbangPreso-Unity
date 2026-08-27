using System.IO;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Makes the character models animatable, and the props not.
    ///
    /// ⚠️⚠️ WITHOUT THIS THE CHARACTERS CANNOT ANIMATE AT ALL, AND IT FAILS SILENTLY. The GLBs
    /// carry 32 clips each and imported with no Avatar and no rig, so the clips exist in the
    /// asset and nothing can play them. A character simply stands still, which reads as
    /// "animation is not wired yet" rather than as an import setting, and no error is ever
    /// logged.
    ///
    /// ⚠️ GENERIC, NOT HUMANOID, DELIBERATELY. Humanoid retargeting is the right answer when
    /// clips come from elsewhere (Mixamo, another rig), but these clips ship WITH their own rig
    /// and are authored against it. Retargeting them through a humanoid avatar re-solves poses
    /// that were already correct and introduces foot sliding and wrist twist for no gain. When
    /// the art is replaced and animations start coming from a library, revisit this.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.ModelImportSetup.Run
    /// </summary>
    public static class ModelImportSetup
    {
        private const string PersonDir = "Assets/TumbangPreso/Art/characters/persons";
        private const string PropDir = "Assets/TumbangPreso/Art/models";

        [MenuItem("Tumbang Preso/Fix Model Import Settings")]
        public static void RunFromMenu() => Execute();

        public static void Run()
        {
            Execute();
            EditorApplication.Exit(0);
        }

        private static void Execute()
        {
            int people = 0, props = 0;

            // ⚠️⚠️ THE .glb FILES ARE NOT OWNED BY ModelImporter AND NEVER WILL BE. Unity has no
            // native glTF support, so glTFast's ScriptedImporter claims them. An earlier version
            // of this method cast to ModelImporter, got null for every character, skipped them
            // all, and logged "fixed 0 character models" as though they were already correct.
            // That is the exact shape of a silent no-op, so it is checked and reported now.
            //
            // ⚠️ AND NOTHING NEEDS FIXING THERE. glTFast already emits what the animation layer
            // requires: an Animator (with a null controller, which is correct for Playables)
            // and the skinned meshes. The clips live in the asset and are played directly. Do
            // not "fix" the null controller by authoring one; that is what Playables avoids.
            foreach (var path in Directory.GetFiles(PersonDir, "*.glb", SearchOption.AllDirectories))
            {
                var any = AssetImporter.GetAtPath(path.Replace('\\', '/'));

                if (any is ModelImporter)
                {
                    Debug.LogWarning($"[ModelImport] {Path.GetFileName(path)} is on ModelImporter, " +
                                     "which is unexpected for a .glb. Check glTFast is installed.");
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
                if (prefab == null)
                {
                    Debug.LogError($"[ModelImport] {Path.GetFileName(path)} produced no prefab.");
                    continue;
                }

                if (prefab.GetComponentInChildren<Animator>(true) == null)
                {
                    Debug.LogError($"[ModelImport] {Path.GetFileName(path)} has no Animator. " +
                                   "Animation cannot play, and it will fail silently.");
                    continue;
                }

                people++;
            }

            foreach (var path in Directory.GetFiles(PropDir, "*.obj", SearchOption.AllDirectories))
            {
                var importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as ModelImporter;
                if (importer == null) continue;

                bool changed = false;

                // ⚠️ A LATA AND A TSINELAS DO NOT WALK. Importing animation on a static prop
                // costs an Animator per instance for clips that do not exist.
                if (importer.importAnimation) { importer.importAnimation = false; changed = true; }
                if (importer.animationType != ModelImporterAnimationType.None)
                {
                    importer.animationType = ModelImporterAnimationType.None;
                    changed = true;
                }
                if (importer.addCollider) { importer.addCollider = false; changed = true; }

                // ⚠️⚠️ THE INKED PROPS NEED A CPU COPY, AND WITHOUT IT THE OUTLINE FIX SILENTLY
                // SKIPS THEM. `OutlineNormals.Weld` averages the normals sharing a position so the
                // inverted hull stops tearing at hard edges (docs/TODO.md § 52), and it reads
                // `mesh.vertices`. A mesh imported with Read/Write off has no CPU copy, so those
                // come back empty, `Weld` returns early and the lata and tsinelas keep the split
                // border the characters just lost. Measured: `OutlineWeldTests` failed on all nine
                // of these and on none of the `.glb` people, which arrive readable from glTFast.
                //
                // ⚠️ `env_` IS DELIBERATELY EXCLUDED, and the exclusion is a memory decision rather
                // than a stylistic one. Readable means a second copy of the mesh in system RAM for
                // the life of the process. The street never reaches `ToonSkin.Apply` at all (the
                // world toon pass was reverted on 2026-07-29, see `EnvColourPass`), so it has no
                // hull to weld and would be paying for a copy nothing reads. Only the four lata,
                // the four tsinelas and the first-person arm wear ink.
                bool inked = !Path.GetFileName(path).StartsWith("env_");

                if (inked && !importer.isReadable) { importer.isReadable = true; changed = true; }

                if (!changed) continue;

                importer.SaveAndReimport();
                props++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[ModelImport] {people} character models verified animatable " +
                      $"(glTFast), {props} props corrected.");
        }
    }
}
