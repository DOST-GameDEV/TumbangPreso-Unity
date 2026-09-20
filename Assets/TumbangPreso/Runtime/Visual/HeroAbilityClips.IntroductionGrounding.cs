using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public static partial class HeroAbilityClips
    {
        // These rigs have no knees. Author compression through spine/stance and
        // bake root support, as the retained Sean animation workflow does. Work
        // on the supplied render copy, restoring every sampled transform afterward.
        private static void GroundIntroduction(AnimationClip clip, Transform model, string rootPath)
        {
            var root = string.IsNullOrEmpty(rootPath) ? model : model.Find(rootPath);
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (root == null || skins.Length == 0) return;
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var positions = new Vector3[transforms.Length]; var rotations = new Quaternion[transforms.Length];
            var scales = new Vector3[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            { positions[i] = transforms[i].localPosition; rotations[i] = transforms[i].localRotation; scales[i] = transforms[i].localScale; }
            var mesh = new Mesh(); var vertices = new List<Vector3>();
            var carriesScale = new bool[skins.Length];
            Matrix4x4 Space(SkinnedMeshRenderer skin, bool scaled) => scaled
                ? Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one)
                : skin.transform.localToWorldMatrix;
            Vector2 Extent(Matrix4x4 matrix)
            {
                float lo = float.PositiveInfinity, hi = float.NegativeInfinity;
                foreach (var vertex in vertices)
                { float y = matrix.MultiplyPoint3x4(vertex).y; lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); }
                return new Vector2(lo, hi);
            }
            float Lowest()
            {
                float low = float.PositiveInfinity;
                for (int i = 0; i < skins.Length; i++)
                {
                    if (skins[i].sharedMesh == null) continue;
                    skins[i].BakeMesh(mesh); mesh.GetVertices(vertices);
                    low = Mathf.Min(low, Extent(Space(skins[i], carriesScale[i])).x);
                }
                return low;
            }
            try
            {
                clip.SampleAnimation(model.gameObject, 0);
                // Calibrate the bake convention per renderer against its rest span.
                // Applying PersonScale twice would hide the real foot penetration.
                for (int i = 0; i < skins.Length; i++)
                {
                    if (skins[i].sharedMesh == null) continue;
                    skins[i].BakeMesh(mesh); mesh.GetVertices(vertices);
                    var a = Extent(Space(skins[i], true)); var b = Extent(Space(skins[i], false));
                    float height = skins[i].bounds.size.y;
                    carriesScale[i] = Mathf.Abs(a.y - a.x - height) <= Mathf.Abs(b.y - b.x - height);
                }
                float floor = Lowest();
                float worldPerLocalY = root.parent != null ? root.parent.TransformVector(Vector3.up).y : 1;
                if ((float.IsNaN(floor) || float.IsInfinity(floor)) || Mathf.Abs(worldPerLocalY) < .001f) return;
                int steps = Mathf.CeilToInt(clip.length * 60);
                var keys = new Keyframe[steps + 1];
                for (int i = 0; i <= steps; i++)
                {
                    float time = clip.length * i / steps;
                    clip.SampleAnimation(model.gameObject, time);
                    float local = root.localPosition.y + (floor - Lowest()) / worldPerLocalY;
                    keys[i] = new Keyframe(time, local);
                }
                for (int i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    key.inTangent = i == 0 ? 0 : (key.value - keys[i-1].value) / (key.time - keys[i-1].time);
                    key.outTangent = i + 1 == keys.Length ? 0 : (keys[i+1].value - key.value) / (keys[i+1].time - key.time);
                    keys[i] = key;
                }
                clip.SetCurve(rootPath, typeof(Transform), "localPosition.y", new AnimationCurve(keys));
            }
            finally
            {
                for (int i = 0; i < transforms.Length; i++)
                { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
                if (Application.isPlaying) Object.Destroy(mesh); else Object.DestroyImmediate(mesh);
            }
        }
    }
}
