using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public static partial class HeroAbilityClips
    {
        // These rigs have no knees. Author compression through spine/stance and
        // bake root support, as the retained Sean animation workflow does. Work
        // on the supplied render copy, restoring every sampled transform afterward.
        private static void GroundIntroduction(AnimationClip clip, Transform model, string rootPath, bool anchorToRest = false,
            System.Func<float, float> lift = null, System.Action<AnimationCurve[]> writeRoot = null)
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
                if(anchorToRest)
                {
                    // Live clips can begin in a gathered pose. Anchor their
                    // support to the source rest floor, not an already sunk key.
                    for(int i=0;i<transforms.Length;i++)
                    {transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
                    floor=Lowest();
                }
                float worldPerLocalY = root.parent != null ? root.parent.TransformVector(Vector3.up).y : 1;
                if ((float.IsNaN(floor) || float.IsInfinity(floor)) || Mathf.Abs(worldPerLocalY) < .001f) return;
                int steps = Mathf.CeilToInt(clip.length * 60);
                // ⚠️⚠️ X, Y AND Z ARE WRITTEN TOGETHER ON THE SAME KEY TIMES. A legacy clip binds
                // `localPosition.x/y/z` as ONE vector curve; replacing only y with ~200 grounding
                // keys while x and z kept the table's handful made `SetCurve` assert "Key index
                // (198) is out of range [0, 198)" on every match's warm-up
                // (`UltimateIntroductionCache.WarmOne`, 2026-09-24 PlayMode gate: 25 fixtures red,
                // from the HUD probes to `BotBehaviourProbe`), and left the root sampling NaN
                // (`RecordedBody-P2`, Zack's `FingerSpark`). x and z are read off the same samples.
                // Every key is also finite: `Lowest()` is infinite on a sample where no skin has a
                // vertex, and such a sample keeps the previous support.
                var keys = new List<Keyframe>(steps + 1);
                var keysX = new List<Keyframe>(steps + 1); var keysZ = new List<Keyframe>(steps + 1);
                float last = root.localPosition.y;
                for (int i = 0; i <= steps; i++)
                {
                    float time = clip.length * i / steps;
                    if (keys.Count > 0 && time <= keys[keys.Count - 1].time + 1e-5f) continue;
                    clip.SampleAnimation(model.gameObject, time);
                    float low = Lowest();
                    float local = float.IsNaN(low) || float.IsInfinity(low)
                        ? last : root.localPosition.y + (floor - low) / worldPerLocalY;
                    last = local;
                    // ⚠️ AUTHORED LIFT IS ADDED AFTER THE FLOOR IS FOUND, so a performance that
                    // leaves the ground on purpose (Phaister's laughing levitation, REFINE-2.11)
                    // rises from wherever her feet really were rather than being pulled back down.
                    if (lift != null) local += lift(time) / worldPerLocalY;
                    if (float.IsNaN(local) || float.IsInfinity(local)) continue;
                    keys.Add(new Keyframe(time, local));
                    keysX.Add(new Keyframe(time, root.localPosition.x));
                    keysZ.Add(new Keyframe(time, root.localPosition.z));
                }
                if (keys.Count < 2) return;
                foreach (var list in new[] { keysX, keys, keysZ })
                    for (int i = 0; i < list.Count; i++)
                    {
                        var key = list[i];
                        key.inTangent = i == 0 ? 0 : (key.value - list[i-1].value) / (key.time - list[i-1].time);
                        key.outTangent = i + 1 == list.Count ? 0 : (list[i+1].value - key.value) / (list[i+1].time - key.time);
                        list[i] = key;
                    }
                var curves = new[] { new AnimationCurve(keysX.ToArray()), new AnimationCurve(keys.ToArray()), new AnimationCurve(keysZ.ToArray()) };
                if (writeRoot != null) writeRoot(curves);
                else
                {
                    clip.SetCurve(rootPath, typeof(Transform), "localPosition.x", curves[0]);
                    clip.SetCurve(rootPath, typeof(Transform), "localPosition.y", curves[1]);
                    clip.SetCurve(rootPath, typeof(Transform), "localPosition.z", curves[2]);
                }
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
