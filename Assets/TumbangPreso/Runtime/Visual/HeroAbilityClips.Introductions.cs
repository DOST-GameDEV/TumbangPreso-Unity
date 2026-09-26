using UnityEngine;

namespace TumbangPreso.Visual
{
    public static partial class HeroAbilityClips
    {
        // Render-copy performances for the shared phase. They are deliberately
        // not registered as live ability clips: accepted-cast transport and the
        // common pause/resume boundary must be integrated before live activation.
        //
        // ⚠️⚠️ REFINE-2.11, 2026-09-24: THE POSES ARE NO LONGER TYPED HERE. Each hero's
        // introduction is authored in `tools/author_ultimate_intros.py` and read through
        // `UltimatePerformance`, with its own length (2.8 to 4.2 s) instead of one shared 2.8.
        // The previous tables were four or five keys each on a fixed clock, and the pose sheets
        // showed why they did not read: at 2.8 s with no holds, "arms overhead" (-140 raise at a
        // 35 degree drop) was in fact arms out sideways, and nothing registered long enough to be
        // a character. The new tables hold every signature shape and carry per-hero lift.
        public const float IntroductionSeconds = UltimatePerformance.DefaultSeconds;
        public const float IntroductionReturnSeconds = UltimatePerformance.HandoffLead;

        public static AnimationClip BuildUltimateIntroduction(Transform root, string hero, bool holdingSlipper = false)
        {
            var paths = ResolvePaths(root);
            if (paths == null) return null;
            var performance = UltimatePerformance.For(hero, holdingSlipper);
            if (performance == null || performance.Keys.Count < 2) return null;
            var b = new ClipBuilder("intro-" + hero, paths);
            foreach (var key in performance.Keys)
            {
                // Raw local eulers, already converted by the authoring script (arm drop from the
                // T-pose, Unity's imported handedness). The root y is replaced by grounding below.
                b.KeyPos(key.Time, 0, 0, 0);
                b.KeyRot("torso", key.Time, key.Torso.x, key.Torso.y, key.Torso.z);
                b.KeyRot("head", key.Time, key.Head.x, key.Head.y, key.Head.z);
                b.KeyRot("arm-left", key.Time, key.ArmLeft.x, key.ArmLeft.y, key.ArmLeft.z);
                b.KeyRot("arm-right", key.Time, key.ArmRight.x, key.ArmRight.y, key.ArmRight.z);
                b.KeyRot("leg-left", key.Time, key.LegLeft.x, key.LegLeft.y, key.LegLeft.z);
                b.KeyRot("leg-right", key.Time, key.LegRight.x, key.LegRight.y, key.LegRight.z);
            }
            foreach (float punch in performance.Punches) b.PunchAt(punch);
            // Hold the final authored pose to the boundary so a table that ends early never
            // leaves the curve extrapolating.
            var last = performance.Keys[performance.Keys.Count - 1];
            if (last.Time < performance.Seconds - .001f)
            {
                b.KeyPos(performance.Seconds, 0, 0, 0);
                b.KeyRot("torso", performance.Seconds, last.Torso.x, last.Torso.y, last.Torso.z);
                b.KeyRot("head", performance.Seconds, last.Head.x, last.Head.y, last.Head.z);
                b.KeyRot("arm-left", performance.Seconds, last.ArmLeft.x, last.ArmLeft.y, last.ArmLeft.z);
                b.KeyRot("arm-right", performance.Seconds, last.ArmRight.x, last.ArmRight.y, last.ArmRight.z);
                b.KeyRot("leg-left", performance.Seconds, last.LegLeft.x, last.LegLeft.y, last.LegLeft.z);
                b.KeyRot("leg-right", performance.Seconds, last.LegRight.x, last.LegRight.y, last.LegRight.z);
            }
            // SetCurve is supported in native players only for legacy clips.
            // This clip is sampled on a render copy, never put in the live Animator.
            var clip = b.Build(legacy: true);
            AnimationClip grounded = null;
            try
            {
                // Sample the authored pose, then build its final curves on an
                // unsampled clip. Rewriting a sampled legacy binding asserts in
                // Unity 6000.5 even after clearing all three position channels.
                GroundIntroduction(clip, root, paths["root"],
                    lift: performance.Lift.Count > 0 ? performance.LiftAt : null,
                    writeRoot: curves => grounded = b.Build(legacy: true, rootPosition: curves));
                return grounded != null ? grounded : clip;
            }
            finally
            {
                if (grounded != null)
                {
                    if (Application.isPlaying) Object.Destroy(clip); else Object.DestroyImmediate(clip);
                }
            }
        }

        // Retained for Rafi's authored live casts (`HeroAbilityClips.Rafi.cs`), which still key
        // in this shape. The introductions above take raw values from their tables instead.
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        private static void PoseKey(ClipBuilder b, float t, float y, Vector3 torso, Vector3 head,
            Vector3 left, Vector3 right, Vector3 leftLeg = default, Vector3 rightLeg = default)
        {
            b.KeyPos(t, 0, y, 0);
            b.KeyRot("torso", t, torso.x, torso.y, torso.z); b.KeyRot("head", t, head.x, head.y, head.z);
            // The retained GLB rigs are authored in a T-pose. Drop the shoulder
            // before pitching it, matching tools/glb_action.py's ARM_DROP and
            // Unity's imported handedness. Raw X on a T-pose only twists the arm.
            b.KeyRot("arm-left", t, left.x, left.y, 80 - left.z);
            b.KeyRot("arm-right", t, right.x, right.y, -80 - right.z);
            // Spread away from the centre in Unity's imported handedness.
            b.KeyRot("leg-left", t, leftLeg.x, leftLeg.y, -leftLeg.z);
            b.KeyRot("leg-right", t, rightLeg.x, rightLeg.y, -rightLeg.z);
        }
    }
}
