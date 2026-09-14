using System;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    public static class CarryPoseAuthor
    {
        public static void Run()
        {
            try { BakeNemu(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        private static void BakeNemu()
        {
            var entry = RosterBook.Load().People.First(person => person.Id == "nemu");
            var original = entry.Clips.First(clip => clip.name == "holding-right");
            var model = Object.Instantiate(entry.Model);
            var copy = Object.Instantiate(original);
            try
            {
                var animator = model.GetComponentInChildren<Animator>();
                var root = animator != null ? animator.transform : model.transform;
                var arm = root.GetComponentsInChildren<Transform>().First(bone => bone.name == "arm-right");
                string path = AnimationUtility.CalculateTransformPath(arm, root);
                var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
                int samples = Math.Max(2, Mathf.CeilToInt(original.length * 60));
                // Yaw the carried shoe outward from the hood; lowering alone still crosses the hair.
                var correction = Quaternion.Euler(0, 20, 0);
                Quaternion previous = Quaternion.identity;
                for (int i = 0; i <= samples; i++)
                {
                    float time = original.length * i / samples;
                    original.SampleAnimation(root.gameObject, time);
                    var rotation = correction * arm.localRotation;
                    if (i > 0 && Quaternion.Dot(previous, rotation) < 0)
                        rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    curves[0].AddKey(time, rotation.x); curves[1].AddKey(time, rotation.y);
                    curves[2].AddKey(time, rotation.z); curves[3].AddKey(time, rotation.w);
                    previous = rotation;
                }
                foreach (var binding in AnimationUtility.GetCurveBindings(copy))
                    if (binding.path == path && (binding.propertyName.StartsWith("m_LocalRotation") || binding.propertyName.Contains("Euler")))
                        AnimationUtility.SetEditorCurve(copy, binding, null);
                string[] axes = { "x", "y", "z", "w" };
                for (int i = 0; i < axes.Length; i++)
                    AnimationUtility.SetEditorCurve(copy, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + axes[i]), curves[i]);
                copy.name = "holding-right";
                copy.EnsureQuaternionContinuity();
                const string folder = "Assets/TumbangPreso/Resources/CarryMotion";
                Directory.CreateDirectory(folder);
                string target = folder + "/" + DanceClip.ResourceName(root) + ".asset";
                var set = AssetDatabase.LoadAssetAtPath<GeneratedAnimationSet>(target);
                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<GeneratedAnimationSet>();
                    AssetDatabase.CreateAsset(set, target);
                }
                var saved = AssetDatabase.LoadAllAssetsAtPath(target).OfType<AnimationClip>().FirstOrDefault(clip => clip.name == copy.name);
                if (saved == null) { saved = copy; AssetDatabase.AddObjectToAsset(saved, set); copy = null; }
                else { EditorUtility.CopySerialized(copy, saved); EditorUtility.SetDirty(saved); }
                set.Clips = new[] { saved }; EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("[CarryPose] Preserved Nemu's rig, outfit and original source clip; baked a lower/outward carrying arm at " + target);
            }
            finally { Object.DestroyImmediate(model); if (copy != null) Object.DestroyImmediate(copy); }
        }
    }
}
