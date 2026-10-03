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
        /// <summary>One authored settle, breath and balance correction, with a closed loop.</summary>
        public const float AmihanHoverSeconds = 3.4f;

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

        public static AnimationClip[] BuildAmihanFlightAuthored(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("Amihan rig is missing a required bone.");
            var clips = new[] { BuildAmihanUpdraft(paths), BuildAmihanHover(paths) };
            foreach (var clip in clips) GroundIntroduction(clip, root, paths["root"], anchorToRest: true);
            return clips;
        }

        /// <summary>Only the Airburst cast, so re-baking it leaves the other four shipped clips untouched.</summary>
        public static AnimationClip[] BuildAmihanStormAuthored(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("Amihan rig is missing a required bone.");
            var clips = new[] { BuildAmihanStorm(paths) };
            foreach (var clip in clips) GroundIntroduction(clip, root, paths["root"], anchorToRest: true);
            return clips;
        }
#endif

        /// <summary>
        /// QUICK DASH, 0.66 s. Tell: a coil, weight sunk back and twisted away, both arms swept back.
        /// Release (0.12, punch): flung forward off the back foot, the LEFT arm leading low and out,
        /// the torso unwinding, feet off the road. Hold through the slipstream. Follow-through: the
        /// torso keeps turning past centre (the spiral), then settles.
        /// FLYING PASS (2026-10-02, owner: her run floats and *"look like she flying"*; offline witness sheets): the launch
        /// pitched her 24 degrees, so her large head dropped over both arms and her face left the frame for the whole carry.
        /// Now the dash is a short flight in the same language as her floating run: launched upright with the chin up and
        /// her eyes down the line, the lead hand reaching out low, the slipper hand swept back, BOTH legs trailing behind
        /// her off the road; the spiral keeps turning through the carry; then a light catching foot reaches forward and she
        /// settles without a stomp (`docs/reports/hero-reference-footage-2026-10-02/amihan.md`: a catching foot).
        /// </summary>
        private static AnimationClip BuildAmihanDash(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-amihan-dash", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), V(0, 0, 15), V(0, 0, -15));
            PoseKey(b, .09f, -.05f, V(-6, 22, 3), V(2, -18, 0), V(34, 18, 30), V(40, -12, -34), V(-10, 0, 4), V(16, 0, -4));
            b.PunchAt(.13f);
            b.HoldAt(.13f, .16f);
            PoseKey(b, .13f, .06f, V(14, -18, -4), V(-20, 16, 0), V(-84, 20, 40), V(52, 10, -40), V(12, 0, 4), V(30, 0, -6));
            PoseKey(b, .34f, .04f, V(12, -30, -5), V(-16, 24, 0), V(-62, 30, 50), V(36, 8, -40), V(6, 0, 5), V(22, 0, -6));
            PoseKey(b, .48f, -.02f, V(6, -12, -2), V(-4, 8, 0), V(-24, 16, 34), V(12, 0, -26), V(-24, 0, 4), V(14, 0, -4));
            PoseKey(b, .66f, 0, V(0, 0, 0), V(0, 0, 0), V(0, 0, 15), V(0, 0, -15));
            return b.Build();
        }

        /// <summary>
        /// FEATHERFALL's launch: press the air down, then balance on its lift. The accepted
        /// ability already moves her; the visual compression adds no root or gameplay delay.
        /// </summary>
        private static AnimationClip BuildAmihanUpdraft(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-amihan-updraft", paths);
            PoseKey(b, 0, -.035f, V(8, -8, 0), V(4, 5, 0), V(-12, 8, 32), V(-8, -6, -28), V(-12, 0, 7), V(-8, 0, -5));
            PoseKey(b, .10f, -.055f, V(10, -12, -2), V(0, 9, 0), V(12, 12, 44), V(18, -10, -34), V(6, 0, 8), V(12, 0, -6));
            b.PunchAt(.20f);
            PoseKey(b, .20f, .025f, V(-6, 12, 3), V(-8, -8, 0), V(28, 16, 54), V(22, -10, -40), V(16, 0, 6), V(26, 0, -5));
            PoseKey(b, .43f, .045f, V(-3, 20, 2), V(-5, -12, 0), V(4, 10, 60), V(10, -8, -44), V(-8, 0, 5), V(18, 0, -6));
            PoseKey(b, .68f, .01f, V(-2, 4, -2), V(-3, -3, 0), V(-18, 8, 54), V(-8, -6, -38), V(-18, 0, 6), V(12, 0, -4));
            return b.Build();
        }

        /// <summary>
        /// FEATHERFALL aloft: settle on the left, breathe, correct with the right hand, look back
        /// to play. Unequal intervals and silhouettes keep the wind from reading as a metronome.
        /// </summary>
        private static AnimationClip BuildAmihanHover(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-amihan-hover", paths);
            PoseKey(b, 0, .01f, V(-2, 4, -2), V(-3, -3, 0), V(-18, 8, 54), V(-8, -6, -38), V(-18, 0, 6), V(12, 0, -4));
            PoseKey(b, .55f, -.018f, V(1, 7, -3), V(-2, -5, 1), V(-12, 11, 48), V(-5, -4, -42), V(-12, 0, 5), V(16, 0, -5));
            PoseKey(b, 1.18f, .026f, V(-5, 2, -1), V(-5, -2, 0), V(-21, 6, 55), V(-13, -5, -40), V(-20, 0, 6), V(9, 0, -4));
            PoseKey(b, 1.72f, .018f, V(-3, -7, 3), V(-1, 8, -1), V(-14, 4, 47), V(-24, -12, -53), V(-9, 0, 4), V(19, 0, -6));
            PoseKey(b, 2.22f, -.012f, V(2, -3, 1), V(1, 7, 0), V(-10, 5, 45), V(-16, -9, -46), V(-14, 0, 5), V(14, 0, -5));
            PoseKey(b, 2.86f, .022f, V(-4, 5, -1), V(-4, -6, 0), V(-20, 9, 56), V(-9, -6, -39), V(-21, 0, 6), V(10, 0, -4));
            PoseKey(b, AmihanHoverSeconds, .01f, V(-2, 4, -2), V(-3, -3, 0), V(-18, 8, 54), V(-8, -6, -38), V(-18, 0, 6), V(12, 0, -4));
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
        /// AIRBURST, v3.2 (owner, 2026-10-03: *"show the ult actually hitting and knocking abck ppl already in the cutscene"*, *"no
        /// need to reshow it in fpp"*). The shared cutscene now shows the windup AND the release (v4: her drive at 3.85 s of 4.4,
        /// `HeroIntroductionScene.Amihan.cs`), and play resumes on the hit (`AmihanRules.StormSurgeDelaySeconds`, 0). So this
        /// clip only SETTLES: it starts in the pose the cutscene ends on (v8: her cute finish), so the hand-back does not jump, and she is back to her stand and free to run by 0.45 s.
        /// </summary>
        private static AnimationClip BuildAmihanStorm(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-amihan-storm", paths);
            // v8: the cutscene ends on her cute pose (leaning in, hands behind her back, a foot kicked up behind her, a wink:
            // `tools/author_ultimate_intros.py` amihan() finish), and she drops out of it into her stand.
            PoseKey(b, 0, 0, V(8, 4, 6), V(-12, -6, 14), V(46, 0, 10), V(46, 0, -10), V(-2, 0, 6), V(55, 0, -8));
            PoseKey(b, .2f, 0, V(5, 2, 4), V(-5, -3, 8), V(16, 0, 30), V(16, 0, -30), V(-1, 0, 6), V(18, 0, -6));
            PoseKey(b, .45f, 0, V(0, 0, 0), V(0, 0, 0), V(0, 0, 15), V(0, 0, -15));
            return b.Build();
        }
    }
}
