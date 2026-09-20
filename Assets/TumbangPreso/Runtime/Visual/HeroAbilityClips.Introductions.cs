using UnityEngine;

namespace TumbangPreso.Visual
{
    public static partial class HeroAbilityClips
    {
        // Render-copy performances for the shared phase. They are deliberately
        // not registered as live ability clips: accepted-cast transport and the
        // common pause/resume boundary must be integrated before live activation.
        public const float IntroductionSeconds = 2.8f;
        public const float IntroductionReturnSeconds = .4f;

        public static AnimationClip BuildUltimateIntroduction(Transform root, string hero)
        {
            var paths = ResolvePaths(root);
            if (paths == null) return null;
            var b = new ClipBuilder("intro-" + hero, paths);
            PoseKey(b, 0, 0, Vector3.zero, Vector3.zero, new Vector3(0, 0, 15), new Vector3(0, 0, -15));
            switch (hero)
            {
                case "sean":
                    // Weight drops first, hands gather heat toward the ribs, then
                    // the whole silhouette coils into the start of the live leap.
                    PoseKey(b, .30f, -.06f, V(10, -8, 0), V(8, 6, 0), V(-25, 15, 22), V(15, -20, -25), V(-12, 0, 5), V(14, 0, -5));
                    PoseKey(b, .85f, -.12f, V(18, -12, 0), V(-8, 10, 0), V(-55, 25, 18), V(-35, -30, -20), V(-18, 0, 8), V(22, 0, -8));
                    PoseKey(b, 1.45f, -.12f, V(14, 4, 0), V(-14, -4, 0), V(-60, 12, 32), V(-60, -12, -32), V(-18, 0, 8), V(22, 0, -8));
                    PoseKey(b, 2.10f, -.17f, V(26, 0, 0), V(-18, 0, 0), V(24, 0, 24), V(24, 0, -24), V(-24, 0, 10), V(28, 0, -10));
                    PoseKey(b, 2.8f, -.17f, V(26, 0, 0), V(-18, 0, 0), V(24, 0, 24), V(24, 0, -24), V(-24, 0, 10), V(28, 0, -10));
                    break;
                case "phaister":
                    // An asymmetric trace becomes one wide overhead eclipse.
                    // The head remains visible; no deep backward face occlusion.
                    PoseKey(b, .45f, -.025f, V(0, 15, -3), V(-5, -12, 0), V(-30, 25, 25), V(-80, -25, -28));
                    PoseKey(b, 1.10f, .02f, V(-5, 8, 0), V(-12, -5, 0), V(-100, 12, 42), V(-135, -12, -38));
                    PoseKey(b, 1.75f, .02f, V(-5, 0, 0), V(-16, 0, 0), V(-140, 0, 45), V(-140, 0, -45));
                    PoseKey(b, 2.40f, 0, V(5, 0, 0), V(-5, 0, 0), V(-85, 12, 40), V(-85, -12, -40));
                    PoseKey(b, 2.8f, 0, V(5, 0, 0), V(-5, 0, 0), V(-85, 12, 40), V(-85, -12, -40));
                    break;
                case "zack":
                    // A narrow, angular silhouette: one conductor hand and one
                    // quiet counterweight, with a decisive change of attention.
                    PoseKey(b, .32f, -.03f, V(3, -20, 0), V(0, 18, 0), V(-15, 0, 12), V(-55, -20, -28), V(-8, 0, 3), V(10, 0, -3));
                    PoseKey(b, .95f, 0, V(-4, -20, 0), V(-18, 20, 0), V(8, 0, 15), V(-145, -15, -12), V(-8, 0, 3), V(10, 0, -3));
                    PoseKey(b, 1.75f, 0, V(-4, -20, 0), V(-18, 20, 0), V(8, 0, 15), V(-145, -15, -12), V(-8, 0, 3), V(10, 0, -3));
                    b.PunchAt(2.15f);
                    PoseKey(b, 2.15f, -.035f, V(8, 15, 0), V(5, -10, 0), V(12, 0, 15), V(-80, 5, -8), V(-8, 0, 3), V(10, 0, -3));
                    PoseKey(b, 2.8f, -.035f, V(8, 15, 0), V(5, -10, 0), V(12, 0, 15), V(-80, 5, -8), V(-8, 0, 3), V(10, 0, -3));
                    break;
                case "nemu":
                    // A conversation with Kuro's left-side staging: look, offer,
                    // draw inward, then open the space for the transformed familiar.
                    PoseKey(b, .45f, 0, V(0, -12, 0), V(4, -28, -5), V(-65, -15, 22), V(-15, 10, -15));
                    PoseKey(b, 1.0f, -.025f, V(6, -8, 0), V(8, -20, 0), V(-80, -10, 30), V(-60, 15, -22));
                    PoseKey(b, 1.60f, -.04f, V(8, 0, 0), V(6, 0, 0), V(-45, 28, 14), V(-45, -28, -14));
                    PoseKey(b, 2.35f, 0, V(-4, 0, 0), V(-5, 0, 0), V(-35, -18, 55), V(-35, 18, -55));
                    PoseKey(b, 2.8f, 0, V(-4, 0, 0), V(-5, 0, 0), V(-35, -18, 55), V(-35, 18, -55));
                    break;
                case "dante":
                    // Feet and hips lead. The striking shoulder loads last; the
                    // actual forward fissure remains a live, separate impact.
                    PoseKey(b, .45f, -.07f, V(8, -14, 0), V(-4, 12, 0), V(-25, 0, 30), V(20, -10, -30), V(-14, 0, 10), V(18, 0, -10));
                    PoseKey(b, 1.05f, -.11f, V(12, -26, -5), V(-8, 24, 0), V(-55, 15, 24), V(45, -25, -35), V(-20, 0, 12), V(24, 0, -12));
                    PoseKey(b, 1.80f, -.13f, V(18, -30, -6), V(-12, 28, 0), V(-65, 8, 30), V(65, -30, -32), V(-22, 0, 12), V(26, 0, -12));
                    PoseKey(b, 2.40f, -.13f, V(18, -30, -6), V(-12, 28, 0), V(-65, 8, 30), V(65, -30, -32), V(-22, 0, 12), V(26, 0, -12));
                    PoseKey(b, 2.8f, -.13f, V(18, -30, -6), V(-12, 28, 0), V(-65, 8, 30), V(65, -30, -32), V(-22, 0, 12), V(26, 0, -12));
                    break;
                case "cheska":
                    // Still shoulders make the small precise hand phrase legible.
                    // Gathering ice belongs between the hands, not over the face.
                    PoseKey(b, .55f, 0, V(0, 8, 0), V(8, -8, 0), V(-65, 16, 18), V(-30, -12, -16));
                    PoseKey(b, 1.25f, 0, V(0, 4, 0), V(7, -4, 0), V(-70, 18, 24), V(-70, -18, -24));
                    PoseKey(b, 1.90f, -.02f, V(3, 0, 0), V(4, 0, 0), V(-55, 30, 15), V(-55, -30, -15));
                    PoseKey(b, 2.35f, 0, V(0, 0, 0), V(0, 0, 0), V(-85, -8, 42), V(-85, 8, -42));
                    PoseKey(b, 2.8f, 0, V(0, 0, 0), V(0, 0, 0), V(-85, -8, 42), V(-85, 8, -42));
                    break;
                default: return null;
            }
            // SetCurve is supported in native players only for legacy clips.
            // This clip is sampled on a render copy, never put in the live Animator.
            var clip = b.Build(legacy: true);
            GroundIntroduction(clip, root, paths["root"]);
            return clip;
        }

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
