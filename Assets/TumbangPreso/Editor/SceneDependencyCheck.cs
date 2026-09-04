using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Refuse to ship a scene whose Inspector references are broken or whose match cannot start.
    ///
    /// ⚠️⚠️ THIS IS THE OTHER HALF OF `SceneScriptCheck` AND THE TWO MUST NOT BE MERGED. That one
    /// reads scenes AS TEXT, deliberately, because the fault it hunts (a component whose
    /// `m_Script` is an inline stub) is one the editor RESOLVES BY CLASS NAME and the player
    /// cannot, so opening the scene is what hides it. This one has the opposite requirement: a
    /// reference pointing at a deleted object, a component whose script is gone, and a missing
    /// camera are all things you can only see by OPENING the scene and looking at the objects.
    /// Neither technique can find the other's defect, and running both is why they are separate
    /// methods.
    ///
    /// **On `54924fc` it reads 9 build scenes, 11,536 components, 0 findings.** That is the
    /// number worth keeping: it is the first time anything in this repository has asserted that
    /// every shipped scene's references actually resolve.
    ///
    /// ⚠️⚠️ THE CLASS OF BUG IT IS FOR: "a human opened the build and it was broken." `SceneScriptCheck`'s
    /// own header records a shipped player that hard-crashed on a map select with every existing
    /// check green. A missing prefab reference is the quieter version of the same day: nothing
    /// crashes at load, and then a round starts with no lata, or a menu opens with no camera and
    /// draws nothing at all.
    ///
    /// WHAT IT ASSERTS
    ///
    ///  1. **No MISSING references.** ⚠️ Missing is not the same as null and the difference is the
    ///     whole reason this check has almost no false positives. A null reference is often
    ///     legitimate: an optional field, a hook a scene does not use. A MISSING reference is a
    ///     field that points at an instance id which no longer resolves, which is what a deleted
    ///     asset or a broken .meta leaves behind, and it is never correct.
    ///  2. **No missing scripts**, the same fault one level up: a component whose type is gone.
    ///  3. **Per scene, the things a match or a menu cannot run without**, declared in the table
    ///     below with the reason each one is on it.
    ///
    /// ⚠️ IT OPENS EACH SCENE ADDITIVELY AND CLOSES IT AGAIN, and it never SAVES. Saving would
    /// rewrite exactly the stubs `SceneScriptCheck` exists to find, which is the one way this
    /// check could destroy the evidence the other one needs.
    /// </summary>
    public static class SceneDependencyCheck
    {
        /// <summary>
        /// What a scene must contain, and why.
        ///
        /// ⚠️ THE REASON IS NOT DECORATION. A required-component list with no sentences is a list
        /// somebody deletes a row from when it goes red, and the row that goes red is exactly the
        /// one that was protecting something.
        /// </summary>
        private readonly struct Requirement
        {
            public readonly string ScenePart;
            public readonly Type Component;
            public readonly string Why;

            public Requirement(string scenePart, Type component, string why)
            {
                ScenePart = scenePart;
                Component = component;
                Why = why;
            }
        }

        private static readonly Requirement[] Required =
        {
            // ⚠️⚠️ THERE IS DELIBERATELY NO CAMERA REQUIREMENT FOR AN ARENA, AND THE FIRST
            // VERSION OF THIS FILE HAD ONE AND WAS WRONG ABOUT ALL THREE MAPS. A map scene
            // authors no camera because `MatchInstaller` BUILDS the rig at runtime, per seat:
            // `CLAUDE.md` § 4 is the reason ("the camera is FPP *and* TPP. Do not simplify it to
            // one"), and a Person is always FPP while a Prop is always TPP, which cannot be
            // authored into a scene that does not yet know who is sitting where. **Do not add
            // that row back.** `docs/TODO.md` § 126.8's "No main camera in the arena" is a
            // LEAKED-SCENE symptom at runtime, not a missing asset.
            new Requirement("Scenes/Ui/", typeof(Camera),
                "a menu scene with no camera draws no canvas at all, and every UI scene in the "
                + "build authors its own."),
        };

        /// <summary>
        /// Objects a scene must contain by NAME.
        ///
        /// ⚠️⚠️ EMPTY, AND THE TWO ROWS THAT WERE TRIED ARE RECORDED HERE BECAUSE BOTH LOOKED
        /// OBVIOUSLY RIGHT AND BOTH ASSERTED SOMETHING THE CODE DOES NOT DO.
        ///
        ///  * **`Spawn0` to `Spawn3`.** The maps do author them and it is tempting to require
        ///    them, but **nothing at runtime reads them.** `IlalimNgTulayBuilder` writes the
        ///    markers and that is the only mention outside this file; every seat is actually
        ///    placed from `Confinement.AttackerSpawnRing()`, in `SliceRunner`, `RoundDirector`
        ///    and `MatchBootstrap` alike. A required marker would be a check that goes red on a
        ///    map authored correctly.
        ///  * **`Floor`.** Eskinita and Bayan Plaza have one; Ilalim ng Tulay's ground is called
        ///    `AsphaltSurface`. The name is per map, and **`MapGeometryCheck` already owns the
        ///    real property**: it refuses an arena whose floor has holes, whose props float and
        ///    whose furniture stands inside the defender's box, which is the question worth
        ///    asking and a name check is not.
        ///
        /// **The lesson is the one this whole pass keeps meeting: a check that asserts a
        /// convention rather than a contract goes red on correct work, and a check that goes red
        /// on correct work gets deleted along with whatever it was protecting.**
        /// </summary>
        private static readonly (string ScenePart, string Name, string Why)[] RequiredObjects =
            new (string, string, string)[0];

        [MenuItem("Tumbang Preso/Check Scene Dependencies")]
        public static void RunFromMenu() => Execute(true);

        public static void Run() => EditorApplication.Exit(Execute(true) ? 0 : 1);

        public static bool Execute(bool writeReport)
        {
            var report = new StringBuilder();
            var failures = new List<string>();
            int scenesChecked = 0;
            int componentsChecked = 0;

            report.AppendLine("SCENE DEPENDENCY CHECK");
            report.AppendLine();

            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (!entry.enabled) continue;
                if (!File.Exists(entry.path))
                {
                    failures.Add($"{entry.path} is in the build settings and does not exist");
                    continue;
                }

                Scene scene = default;
                try
                {
                    scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
                    scenesChecked++;

                    var found = new HashSet<Type>();
                    var names = new HashSet<string>(StringComparer.Ordinal);
                    var sceneFailures = new List<string>();

                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                            names.Add(transform.name);

                        foreach (var component in root.GetComponentsInChildren<Component>(true))
                        {
                            // ⚠️ A NULL COMPONENT IN THIS ARRAY IS A MISSING SCRIPT. Unity returns
                            // one entry per component slot, and a slot whose type no longer
                            // resolves comes back null rather than being skipped.
                            if (component == null)
                            {
                                sceneFailures.Add("a GameObject holds a component whose script is "
                                                  + "missing entirely");
                                continue;
                            }

                            componentsChecked++;
                            found.Add(component.GetType());

                            foreach (string fault in MissingReferences(component))
                                sceneFailures.Add(fault);
                        }
                    }

                    foreach (var requirement in Required)
                    {
                        if (!entry.path.Replace('\\', '/').Contains(requirement.ScenePart)) continue;

                        bool present = false;
                        foreach (var type in found)
                            if (requirement.Component.IsAssignableFrom(type)) { present = true; break; }

                        if (!present)
                            sceneFailures.Add($"there is no {requirement.Component.Name}: " +
                                              requirement.Why);
                    }

                    foreach (var (scenePart, objectName, why) in RequiredObjects)
                    {
                        if (!entry.path.Replace('\\', '/').Contains(scenePart)) continue;
                        if (names.Contains(objectName)) continue;

                        sceneFailures.Add($"there is no object named '{objectName}': {why}");
                    }

                    string name = Path.GetFileNameWithoutExtension(entry.path);
                    if (sceneFailures.Count == 0)
                    {
                        report.AppendLine($"  OK    {name}");
                    }
                    else
                    {
                        report.AppendLine($"  FAIL  {name}");
                        foreach (string fault in sceneFailures)
                        {
                            report.AppendLine($"          {fault}");
                            failures.Add($"{name}: {fault}");
                        }
                    }
                }
                catch (Exception e)
                {
                    // ⚠️ A SCENE THAT CANNOT BE OPENED IS A FAILURE AND NOT A SKIP. That is the
                    // most severe version of what this check is for.
                    failures.Add($"{entry.path} could not be opened: {e.Message}");
                    report.AppendLine($"  FAIL  {entry.path}: {e.Message}");
                }
                finally
                {
                    // ⚠️⚠️ CLOSED WITHOUT SAVING, ALWAYS. Saving would rewrite the inline
                    // `MonoScript` stubs `SceneScriptCheck` exists to detect, so a tidy-up here
                    // would silently destroy the evidence of the crash class that check was
                    // written for.
                    if (scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }
            }

            report.AppendLine();
            report.AppendLine($"{scenesChecked} build scene(s), {componentsChecked} components, "
                              + $"{failures.Count} finding(s).");

            string text = report.ToString();
            Debug.Log(text);

            if (writeReport)
            {
                try
                {
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText("Logs/scene-dependency-check.txt", text);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SceneDependencyCheck] could not write its report: {e.Message}");
                }
            }

            return failures.Count == 0;
        }

        /// <summary>
        /// Serialized object references that point at something which no longer exists.
        ///
        /// ⚠️⚠️ THE TEST IS `objectReferenceValue == null && objectReferenceInstanceIDValue != 0`,
        /// AND EVERY WORD OF IT MATTERS. A field that was never assigned has a zero instance id
        /// and is usually legitimate: optional hooks are ordinary and flagging them would make
        /// this check unusable on the first run. A field with a NON-zero id that resolves to
        /// nothing is a reference to a deleted asset, and there is no reading of that which is
        /// correct. That distinction is why this can gate a build.
        /// </summary>
        private static IEnumerable<string> MissingReferences(Component component)
        {
            var faults = new List<string>();

            SerializedObject so;
            try
            {
                so = new SerializedObject(component);
            }
            catch (Exception e)
            {
                faults.Add($"{component.GetType().Name} could not be inspected: {e.Message}");
                return faults;
            }

            var property = so.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue != null) continue;

                // ⚠️⚠️ `objectReferenceInstanceIDValue` IS OBSOLETE-AS-AN-ERROR IN UNITY 6, not
                // merely deprecated: the editor assembly refuses to compile against it. The
                // replacement is `objectReferenceEntityIdValue`, and it is compared through its
                // string form on purpose rather than cast to an int. `EntityId`'s conversion
                // operators are not stable across 6.x point releases, and this check exists to
                // stop a build breaking rather than to be the thing that breaks one.
                string id = property.objectReferenceEntityIdValue.ToString();
                if (string.IsNullOrEmpty(id) || id == "0") continue;

                faults.Add($"{HierarchyPath(component)} :: {component.GetType().Name}.{property.propertyPath} " +
                           $"points at a deleted object (entity {id})");
            }

            so.Dispose();
            return faults;
        }

        private static string HierarchyPath(Component component)
        {
            var parts = new List<string>();
            var t = component.transform;
            while (t != null)
            {
                parts.Insert(0, t.name);
                t = t.parent;
            }
            return string.Join("/", parts);
        }
    }
}
