using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The emote wheel's clips by name, for the animator's chains (`CharacterAnimator.EmoteClips`) and the bake
    /// (`EmoteMotionAuthor`). A rig that carries them plays these; one that does not falls back down its chain to the
    /// pack's old clip, exactly as before.
    /// </summary>
    public static class EmoteMotion
    {
        public const string Nod = "emote-nod", Nope = "emote-nope", SitDown = "emote-sitdown", Victory = "emote-victory",
            Groove = "emote-groove", TPose = "emote-tpose", Bow = "emote-bow";

        public static readonly string[] All = { Nod, Nope, SitDown, Victory, Groove, TPose, Bow };
    }

    /// <summary>
    /// THE EMOTES, AUTHORED (owner, 2026-10-23: "emotes are ... boring compared to all animations and rigid; all
    /// animations were updated except for them"). The wheel's seven played the pack's own two gestures, a crouch
    /// passed off as victory, the bind pose as the T-pose and a pick-up as the bow: stiff, small, no wind-up and no
    /// landing. These are keyed the way the casts are (`ClipBuilder`, `PunchAt`, `HoldAt`, the elbows on the redesign
    /// rigs), each with an anticipation, a hit and a follow-through, and pushed big enough to read from across a court
    /// on a flat-faced body: the feeling is in the lean, the head and two arms.
    ///
    ///   NOD      1.20 s loop   "YES!": a lean back and up, two big nods with a thumbs-up held at the chest.
    ///   NOPE     1.10 s loop   Leaned back, a hand on the hip, a finger wagging in front of the face, the head shaking.
    ///   SIT      1.30 s hold   A little hop up, a plop down onto the court with a bounce, then leaning back on the
    ///                          hands, legs out, looking round, at ease.
    ///   VICTORY  1.40 s hold   A crouch, a jump with both fists up, the landing, then the power pose: one fist high,
    ///                          the other on the hip, chest out, chin up.
    ///   DANCE    1.92 s loop   Four beats: a disco point each way, elbows pumping, arms out with a hip twist; the body
    ///                          bounces on every beat and the feet step.
    ///   T-POSE   0.90 s hold   A squeeze in, then the arms SNAP out past level and settle; a deadpan tilt of the head.
    ///   BOW      1.80 s loop   Up tall with a flourish, a deep theatre bow (a hand across the belly, the other out
    ///                          behind, a foot back), held, then up with a flourish.
    ///
    /// Grounded like the casts (`GroundIntroduction`): the lowest vertex stays on the court every frame, so a sit sits
    /// and a bow does not float; the jump and the dance's bounce are lifts over that.
    /// </summary>
    public static partial class HeroAbilityClips
    {
#if UNITY_EDITOR
        public static AnimationClip[] BuildEmotes(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("The rig is missing a required bone for the emotes.");
            var clips = new[]
            {
                Grounded(EmoteNod(paths), root, paths, null),
                Grounded(EmoteNope(paths), root, paths, null),
                Grounded(EmoteSitDown(paths), root, paths, t => 0.05f * Bump(t, 0.04f, 0.3f)),
                Grounded(EmoteVictory(paths), root, paths, t => 0.17f * Bump(t, 0.28f, 0.58f)),
                Grounded(EmoteGroove(paths), root, paths, t => 0.035f * Mathf.Abs(Mathf.Sin(Mathf.PI * t / GrooveBeat))),
                Grounded(EmoteTPose(paths), root, paths, t => 0.03f * Bump(t, 0.18f, 0.34f)),
                Grounded(EmoteBow(paths), root, paths, null),
            };
            return clips;
        }

        private static AnimationClip Grounded(AnimationClip clip, Transform root, Dictionary<string, string> paths, System.Func<float, float> lift)
        {
            GroundIntroduction(clip, root, paths["root"], anchorToRest: true, lift: lift);
            return clip;
        }
#endif

        /// <summary>A smooth bump: 0 before `from`, up to 1 in the middle, 0 again at `to`.</summary>
        private static float Bump(float t, float from, float to)
        {
            if (t <= from || t >= to) return 0.0f;
            return Mathf.Sin(Mathf.PI * (t - from) / (to - from));
        }

        private static readonly Vector3 EmoteRestLeft = new Vector3(0, 0, 14), EmoteRestRight = new Vector3(0, 0, -14);
        private static readonly Vector3 Zero = Vector3.zero;

        /// <summary>NOD: "YES!" A lean back and a look up, then two big nods, the thumbs-up held at the chest throughout.</summary>
        private static AnimationClip EmoteNod(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(EmoteMotion.Nod, paths);
            Vector3 thumb = V(-48, -10, -14);
            PoseKey(b, 0.00f, 0, V(2, 0, 0), V(-6, 0, 0), EmoteRestLeft, thumb, Zero, Zero, Fore(12), Fore(105));
            PoseKey(b, 0.10f, 0, V(-9, 0, 2), V(-26, 0, 4), V(4, 0, 18), V(-62, -10, -18), Zero, Zero, Fore(14), Fore(118));
            b.PunchAt(0.22f);
            PoseKey(b, 0.22f, 0, V(13, 0, 0), V(32, 0, -2), V(-6, 0, 12), V(-40, -10, -12), Zero, Zero, Fore(10), Fore(100));
            PoseKey(b, 0.36f, 0, V(0, 0, 0), V(-14, 0, 0), EmoteRestLeft, thumb, Zero, Zero, Fore(12), Fore(108));
            b.PunchAt(0.48f);
            PoseKey(b, 0.48f, 0, V(11, 0, 0), V(28, 0, 2), V(-6, 0, 12), V(-42, -10, -12), Zero, Zero, Fore(10), Fore(100));
            PoseKey(b, 0.66f, 0, V(-2, 0, -3), V(-4, 8, 6), EmoteRestLeft, V(-56, -10, -16), Zero, Zero, Fore(12), Fore(112));
            PoseKey(b, 0.90f, 0, V(0, 0, 0), V(-3, 0, 2), EmoteRestLeft, thumb, Zero, Zero, Fore(12), Fore(106));
            PoseKey(b, 1.20f, 0, V(2, 0, 0), V(-6, 0, 0), EmoteRestLeft, thumb, Zero, Zero, Fore(12), Fore(105));
            return b.Build();
        }

        /// <summary>NOPE: leaned back, a hand on the hip, a forefinger wagging in front of the face, the head shaking against it.</summary>
        private static AnimationClip EmoteNope(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(EmoteMotion.Nope, paths);
            Vector3 hip = V(12, 0, 40), wagMid = V(-82, 0, -18), wagIn = V(-82, 0, -4), wagOut = V(-82, 0, -32);
            PoseKey(b, 0.00f, 0, V(-9, -6, 0), V(-6, 0, 0), hip, wagMid, Zero, Zero, Fore(112), Fore(74));
            PoseKey(b, 0.12f, 0, V(-11, -8, 1), V(-4, 24, -5), hip, wagIn, Zero, Zero, Fore(112), Fore(70));
            PoseKey(b, 0.26f, 0, V(-11, -8, -1), V(-4, -24, 5), hip, wagOut, Zero, Zero, Fore(112), Fore(70));
            PoseKey(b, 0.40f, 0, V(-11, -8, 1), V(-4, 22, -5), hip, wagIn, Zero, Zero, Fore(112), Fore(70));
            PoseKey(b, 0.54f, 0, V(-11, -8, -1), V(-4, -22, 5), hip, wagOut, Zero, Zero, Fore(112), Fore(70));
            b.PunchAt(0.70f);
            PoseKey(b, 0.70f, 0, V(-14, -6, 0), V(-10, 0, 0), hip, V(-88, 0, -16), Zero, Zero, Fore(112), Fore(80));
            PoseKey(b, 0.85f, 0, V(-12, -6, 0), V(-6, 10, 7), hip, wagMid, Zero, Zero, Fore(112), Fore(76));
            PoseKey(b, 1.10f, 0, V(-9, -6, 0), V(-6, 0, 0), hip, wagMid, Zero, Zero, Fore(112), Fore(74));
            return b.Build();
        }

        /// <summary>SIT: a little hop, a plop down onto the court with a bounce, then leaning back on the hands, legs
        /// out, looking round, at ease. Held.</summary>
        private static AnimationClip EmoteSitDown(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(EmoteMotion.SitDown, paths);
            Vector3 legsOut = V(-88, 0, 10);
            PoseKey(b, 0.00f, 0, Zero, Zero, EmoteRestLeft, EmoteRestRight);
            PoseKey(b, 0.14f, 0, V(-6, 0, 0), V(-10, 0, 0), V(-22, 0, 42), V(-22, 0, -42), V(-10, 0, 4), V(-10, 0, 4), Fore(20), Fore(20));
            b.PunchAt(0.34f);
            b.HoldAt(0.34f, 0.04f);
            PoseKey(b, 0.34f, 0, V(10, 0, 0), V(14, 0, 0), V(-52, 0, 32), V(-52, 0, -32), legsOut, legsOut, Fore(24), Fore(24));
            PoseKey(b, 0.48f, 0, V(-5, 0, 0), V(-8, 0, 0), V(10, 0, 30), V(10, 0, -30), legsOut, legsOut, Fore(14), Fore(14));
            PoseKey(b, 0.68f, 0, V(-15, 0, 0), V(-8, 12, 8), V(42, 0, 30), V(40, 0, -30), legsOut, legsOut, Fore(8), Fore(8));
            PoseKey(b, 0.92f, 0, V(-14, 0, 2), V(-4, -8, 7), V(42, 0, 30), V(40, 0, -30), V(-88, 0, 12), V(-86, 0, 8), Fore(8), Fore(8));
            PoseKey(b, 1.30f, 0, V(-13, 0, 1), V(-3, -10, 6), V(42, 0, 30), V(40, 0, -30), V(-88, 0, 12), V(-86, 0, 8), Fore(8), Fore(8));
            return b.Build();
        }

        /// <summary>VICTORY: a crouch, a jump with both fists up, the landing, then the power pose: one fist high, the
        /// other on the hip, chest out, chin up. Held.</summary>
        private static AnimationClip EmoteVictory(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(EmoteMotion.Victory, paths);
            Vector3 high = V(-22, 0, -162), hip = V(12, 0, 40);
            PoseKey(b, 0.00f, 0, Zero, Zero, EmoteRestLeft, EmoteRestRight);
            PoseKey(b, 0.18f, 0, V(26, 0, 0), V(12, 0, 0), V(34, 0, 22), V(34, 0, -22), V(-14, 0, 6), V(-14, 0, 6), Fore(36), Fore(36));
            b.PunchAt(0.30f);
            PoseKey(b, 0.30f, 0, V(-16, 0, 0), V(-26, 0, 0), V(-12, 0, 152), V(-12, 0, -152), V(4, 0, 4), V(4, 0, 4), Fore(44), Fore(44));
            PoseKey(b, 0.43f, 0, V(-18, 0, 0), V(-30, 0, 0), V(-8, 0, 158), V(-8, 0, -158), V(22, 0, 8), V(18, 0, 8), Fore(40), Fore(40));
            b.PunchAt(0.58f);
            PoseKey(b, 0.58f, 0, V(18, 0, 0), V(10, 0, 0), V(-32, 0, 92), V(-32, 0, -92), V(-6, 0, 12), V(-6, 0, 12), Fore(30), Fore(30));
            PoseKey(b, 0.76f, 0, V(-13, 0, -4), V(-20, -10, 6), hip, high, V(0, 0, 14), V(0, 0, 14), Fore(116), Fore(16));
            PoseKey(b, 0.96f, 0, V(-11, 0, -4), V(-16, -10, 6), hip, V(-32, 0, -150), V(0, 0, 14), V(0, 0, 14), Fore(116), Fore(38));
            PoseKey(b, 1.16f, 0, V(-14, 0, -4), V(-22, -10, 7), hip, V(-20, 0, -164), V(0, 0, 14), V(0, 0, 14), Fore(116), Fore(10));
            PoseKey(b, 1.40f, 0, V(-13, 0, -4), V(-20, -10, 6), hip, high, V(0, 0, 14), V(0, 0, 14), Fore(116), Fore(14));
            return b.Build();
        }

        private const float GrooveBeat = 0.48f;

        /// <summary>DANCE: four beats: a disco point up-right with the other hand on the hip; the mirror; elbows pumping
        /// in front with a head bob; arms out wide with a hip twist. A bounce on every beat; the feet step.</summary>
        private static AnimationClip EmoteGroove(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(EmoteMotion.Groove, paths);
            float q = GrooveBeat * 0.5f;
            Vector3 stepL = V(-18, 0, 6), stepR = V(-18, 0, 6), back = V(10, 0, 6);
            // Beat 1: point up to the right, left hand down across the hip, sway right.
            PoseKey(b, 0 * q, 0, V(4, -14, -12), V(-12, -18, -10), V(-24, 0, 10), V(-28, 0, -152), back, stepR, Fore(84), Fore(8));
            PoseKey(b, 1 * q, 0, V(6, -8, -6), V(4, -10, -4), V(-30, 0, 14), V(-36, 0, -140), back, back, Fore(80), Fore(20));
            // Beat 2: the mirror.
            PoseKey(b, 2 * q, 0, V(4, 14, 12), V(-12, 18, 10), V(-28, 0, 152), V(-24, 0, -10), stepL, back, Fore(8), Fore(84));
            PoseKey(b, 3 * q, 0, V(6, 8, 6), V(4, 10, 4), V(-36, 0, 140), V(-30, 0, -14), back, back, Fore(20), Fore(80));
            // Beat 3: elbows pumping in front, the head bobbing, a dip.
            PoseKey(b, 4 * q, 0, V(10, 0, 0), V(18, 0, 0), V(-74, 0, 22), V(-74, 0, -22), stepL, back, Fore(104), Fore(104));
            PoseKey(b, 5 * q, 0, V(4, 0, 0), V(-8, 0, 0), V(-58, 0, 28), V(-58, 0, -28), back, stepR, Fore(120), Fore(120));
            // Beat 4: arms out wide, elbows up, a hip twist.
            PoseKey(b, 6 * q, 0, V(-6, 22, 0), V(-14, -14, 8), V(0, 0, 84), V(0, 0, -84), back, stepR, Fore(62), Fore(62));
            PoseKey(b, 7 * q, 0, V(-4, -22, 0), V(-10, 14, -8), V(-6, 0, 76), V(-6, 0, -76), stepL, back, Fore(70), Fore(70));
            // The seam: the first pose again.
            PoseKey(b, 8 * q, 0, V(4, -14, -12), V(-12, -18, -10), V(-24, 0, 10), V(-28, 0, -152), back, stepR, Fore(84), Fore(8));
            return b.Build();
        }

        /// <summary>T-POSE: a squeeze in, then the arms SNAP out past level and settle at it; a deadpan tilt. Held.</summary>
        private static AnimationClip EmoteTPose(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(EmoteMotion.TPose, paths);
            PoseKey(b, 0.00f, 0, Zero, Zero, EmoteRestLeft, EmoteRestRight);
            PoseKey(b, 0.12f, 0, V(8, 0, 0), V(10, 0, 0), V(0, 0, 2), V(0, 0, -2), V(0, 0, -2), V(0, 0, -2), Fore(30), Fore(30));
            b.PunchAt(0.22f);
            PoseKey(b, 0.22f, 0, V(-5, 0, 0), V(-10, 0, 0), V(0, 0, 86), V(0, 0, -86), V(0, 0, -1), V(0, 0, -1), Fore(0), Fore(0));
            PoseKey(b, 0.34f, 0, V(0, 0, 0), V(-4, 0, 0), V(0, 0, 79), V(0, 0, -79), Zero, Zero, Fore(0), Fore(0));
            PoseKey(b, 0.60f, 0, V(0, 0, 0), V(-6, 0, 11), V(0, 0, 80), V(0, 0, -80), Zero, Zero, Fore(0), Fore(0));
            PoseKey(b, 0.90f, 0, V(0, 0, 0), V(-6, 0, 9), V(0, 0, 80), V(0, 0, -80), Zero, Zero, Fore(0), Fore(0));
            return b.Build();
        }

        /// <summary>BOW: up tall with a flourish of the right hand, a deep theatre bow (that hand across the belly, the
        /// left out behind, the right foot back), held, then up again with a flourish.</summary>
        private static AnimationClip EmoteBow(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(EmoteMotion.Bow, paths);
            PoseKey(b, 0.00f, 0, Zero, Zero, EmoteRestLeft, EmoteRestRight);
            PoseKey(b, 0.22f, 0, V(-7, 0, 0), V(-10, 0, 0), V(0, 0, 40), V(-12, 0, -78), Zero, Zero, Fore(10), Fore(24));
            b.PunchAt(0.52f);
            b.HoldAt(0.52f, 0.36f);
            PoseKey(b, 0.52f, 0, V(58, 0, 0), V(24, 0, 0), V(30, 0, 56), V(-62, 0, -6), V(-4, 0, 2), V(22, 0, 2), Fore(10), Fore(112));
            PoseKey(b, 1.24f, 0, V(4, 0, 0), V(-4, 0, 0), V(10, 0, 30), V(-42, 0, -92), Zero, Zero, Fore(10), Fore(30));
            PoseKey(b, 1.50f, 0, V(-4, 0, 0), V(-6, 0, 6), V(4, 0, 20), V(-20, 0, -40), Zero, Zero, Fore(10), Fore(20));
            PoseKey(b, 1.80f, 0, Zero, Zero, EmoteRestLeft, EmoteRestRight);
            return b.Build();
        }
    }
}
