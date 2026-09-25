using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // AMIHAN'S BODY CASTS (2026-09-25). Keys on the rig's bones only, never on the mesh, because the
    // owner is still improving her model ("keep in mind im probably still gonna improve model so
    // just try to focus on everything else, u can already animate"): a rebuilt glb re-bakes from
    // this same table through `Editor/AmihanMotionAuthor`.
    //
    // ⚠️⚠️ HER DIRECTION IS THE SPIRAL. `tools/author_hero_action.py` gives each hero one motion
    // language no other shares (Sean forward, Zack sideways, Dante down, Cheska still, Rafi the
    // sell and the cut back). Amihan's is the wind's: every cast TURNS THROUGH THE BODY, the torso
    // twisting one way and following through the other, the feet light (the root lifts on the
    // release), nothing moving in a straight line. The direction baseline's six beats are keyed
    // explicitly: a tell (the counter-twist), the release (a punch key that hangs and snaps), the
    // hold, the follow-through, the recovery.
    //
    // PoseKey arms: V(pitch, twist, spread), negative pitch swings FORWARD (-90 straight ahead,
    // -180 overhead), spread 15 is rest. Torso V(lean forward, twist, lean left). Legs V(pitch, _,
    // spread), negative forward.
    // =============================================================================================
    public static partial class HeroAbilityClips
    {
        /// <summary>The flight pose's length: longer than Updraft's 10 s so a non-looping clip never
        /// runs out mid-flight; the bob keys repeat inside it.</summary>
        public const float AmihanHoverSeconds = 10.5f;

#if UNITY_EDITOR
        public static AnimationClip[] BuildAmihanAuthored(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("Amihan rig is missing a required bone.");
            var clips = new[]
            {
                BuildAmihanDash(paths), BuildAmihanUpdraft(paths), BuildAmihanHover(paths),
                BuildAmihanWhirlwind(paths), BuildAmihanStorm(paths),
            };
            foreach (var clip in clips) GroundIntroduction(clip, root, paths["root"], anchorToRest: true);
            return clips;
        }
#endif

        /// <summary>
        /// QUICK DASH, 0.66 s. Tell: a coil, weight sunk back and twisted away, both arms swept back.
        /// Release (0.12, punch): flung forward off the back foot, the LEFT arm leading low and out,
        /// the torso unwinding, feet off the road. Hold through the slipstream. Follow-through: the
        /// torso keeps turning past centre (the spiral), then settles.
        /// </summary>
        private static AnimationClip BuildAmihanDash(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-amihan-dash", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), V(0, 0, 15), V(0, 0, -15));
            PoseKey(b, .09f, -.05f, V(-6, 22, 3), V(4, -16, 0), V(34, 18, 30), V(40, -12, -34), V(-10, 0, 4), V(16, 0, -4));
            b.PunchAt(.13f);
            b.HoldAt(.13f, .16f);
            PoseKey(b, .13f, .05f, V(24, -20, -4), V(-10, 14, 0), V(-78, 22, 44), V(46, 16, -30), V(-34, 0, 6), V(28, 0, -8));
            PoseKey(b, .34f, .03f, V(16, -34, -5), V(-6, 22, 0), V(-58, 30, 52), V(30, 10, -36), V(-22, 0, 6), V(20, 0, -6));
            PoseKey(b, .48f, 0, V(6, -14, -2), V(0, 8, 0), V(-20, 16, 30), V(10, 0, -24), V(-6, 0, 3), V(6, 0, -3));
            PoseKey(b, .66f, 0, V(0, 0, 0), V(0, 0, 0), V(0, 0, 15), V(0, 0, -15));
            return b.Build();
        }

        /// <summary>
        /// UPDRAFT's launch, 0.82 s. Tell: she sinks with both palms pressed down at the road, the
        /// air gathering under her. Release (0.22, punch): springs up, arms thrown overhead and open,
        /// head back, legs trailing, the torso already turning with the column. Settles into the
        /// flight pose (`hero-amihan-hover`), which the animator holds while she is aloft.
        /// </summary>
        private static AnimationClip BuildAmihanUpdraft(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-amihan-updraft", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), V(0, 0, 15), V(0, 0, -15));
            PoseKey(b, .14f, -.09f, V(14, 6, 0), V(12, 0, 0), V(-24, 8, 36), V(-24, -8, -36), V(-16, 0, 8), V(-16, 0, -8));
            b.PunchAt(.22f);
            b.HoldAt(.22f, .14f);
            PoseKey(b, .22f, .10f, V(-10, 14, 0), V(-18, -8, 0), V(-168, 10, 30), V(-160, -14, -34), V(14, 0, 6), V(22, 0, -6));
            PoseKey(b, .46f, .06f, V(-6, 26, 2), V(-12, -14, 0), V(-120, 16, 64), V(-110, -18, -68), V(10, 0, 8), V(16, 0, -8));
            PoseKey(b, .82f, .02f, V(-4, 8, 0), V(-4, -4, 0), V(-24, 4, 70), V(-22, -4, -70), V(8, 0, 6), V(12, 0, -6));
            return b.Build();
        }

        /// <summary>
        /// UPDRAFT aloft, 10.5 s: arms out and a little forward like a glide, legs loose and trailing,
        /// and a slow breathing bob with the torso rocking into the turns. Keyed every 0.9 s so the
        /// body is never a statue; it is a base pose, not an action, and the animator holds it for as
        /// long as `CharacterMotor.IsFlying` says she is in the air.
        /// </summary>
        private static AnimationClip BuildAmihanHover(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-amihan-hover", paths);
            for (int i = 0; i * .9f <= AmihanHoverSeconds; i++)
            {
                float t = i * .9f;
                bool up = i % 2 == 0;
                float rock = (i % 4 < 2 ? 1 : -1) * 6;
                PoseKey(b, t, up ? .03f : -.02f,
                        V(-4, rock, up ? 2 : -2), V(-4, -rock * .6f, 0),
                        V(up ? -24 : -18, 4, up ? 72 : 64), V(up ? -20 : -26, -4, up ? -66 : -74),
                        V(up ? 8 : 14, 0, 6), V(up ? 16 : 8, 0, -6));
            }
            return b.Build();
        }

        /// <summary>
        /// WHIRLWIND, 0.92 s. Tell: a deep wind-up, torso twisted hard to her right, both arms swung
        /// back past the right hip. Release (0.32, punch): the torso whips through to the left and
        /// both arms sweep across the front at shoulder height, the gale leaving them. Follow-through
        /// past the left, then back.
        /// </summary>
        private static AnimationClip BuildAmihanWhirlwind(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-amihan-whirlwind", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), V(0, 0, 15), V(0, 0, -15));
            PoseKey(b, .2f, -.05f, V(6, 44, -4), V(0, -30, 0), V(-40, 40, 34), V(10, 34, -52), V(-8, 0, 8), V(10, 0, -8));
            b.PunchAt(.32f);
            b.HoldAt(.32f, .14f);
            PoseKey(b, .32f, -.02f, V(12, -40, 5), V(-4, 26, 0), V(-88, -28, 78), V(-92, -30, -40), V(-14, 0, 10), V(12, 0, -8));
            PoseKey(b, .56f, 0, V(8, -52, 6), V(-2, 32, 0), V(-66, -40, 70), V(-70, -40, -48), V(-10, 0, 8), V(8, 0, -6));
            PoseKey(b, .92f, 0, V(0, 0, 0), V(0, 0, 0), V(0, 0, 15), V(0, 0, -15));
            return b.Build();
        }

        /// <summary>
        /// STORM SURGE, 2.85 s, played at the press over the 2.5 s wind-up. It STARTS in the pose the
        /// introduction ends on (both palms driven forward, braced), so the cutscene hands straight to
        /// it. Through the gather she leans harder into the wind she is holding back, arms trembling
        /// forward; at 2.5 s (punch) she shoves it away with her whole body, the storm's release.
        /// </summary>
        private static AnimationClip BuildAmihanStorm(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-amihan-storm", paths);
            var legL = V(-22, 0, 10); var legR = V(20, 0, -12);
            PoseKey(b, 0, -.04f, V(18, -4, 0), V(-8, 0, 0), V(-94, 8, 22), V(-94, -8, -22), legL, legR);
            float[] tremble = { .5f, .9f, 1.3f, 1.7f, 2.1f };
            for (int i = 0; i < tremble.Length; i++)
            {
                float lean = 18 + i * 1.4f;
                float shake = i % 2 == 0 ? 3 : -3;
                PoseKey(b, tremble[i], -.05f - i * .004f, V(lean, -4 + shake * .5f, shake * .3f), V(-8 - i, 0, 0),
                        V(-96 - i, 8 + shake, 22), V(-96 - i, -8 + shake, -22), legL, legR);
            }
            PoseKey(b, 2.36f, -.07f, V(12, -2, 0), V(-6, 0, 0), V(-80, 6, 26), V(-80, -6, -26), V(-18, 0, 10), V(18, 0, -12));
            b.PunchAt(2.5f);
            b.HoldAt(2.5f, .16f);
            PoseKey(b, 2.5f, -.08f, V(30, 0, 0), V(-14, 0, 0), V(-88, 4, 12), V(-88, -4, -12), V(-30, 0, 12), V(26, 0, -14));
            PoseKey(b, 2.85f, 0, V(0, 0, 0), V(0, 0, 0), V(0, 0, 15), V(0, 0, -15));
            return b.Build();
        }
    }
}
