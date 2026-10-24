using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // RAGO'S MAGMA FIST, THE PRAYER (owner, 2026-10-24: "his fist thing sucks", "why is he throwing something?", "he js does a
    // prayer pose tapos he long presses or smth to decide where it gets cast"). Two clips, `SeanMotionAuthor` bakes them:
    //  * `hero-sean-pray-aim`, held for as long as the key is down (`HeroAbility.AimPoseAction`, `CharacterAnimator.AimPose`):
    //    palms pressed together in front of his chest, head bowed over them, a slow breath. The read for everyone that the
    //    ground is about to answer him.
    //  * `hero-sean-pray`, the release: he drives both palms down at the court, and the puddle opens where he placed it.
    //
    // PoseKey (`HeroAbilityClips.Introductions.cs`): PoseKey(b, t, rootY, torso, head, LEFT arm, RIGHT arm, left leg, right
    // leg). Arms V(pitch, twist, spread): negative pitch swings FORWARD; a NEGATIVE spread on the left (positive on the right)
    // brings the arm across the chest. His arms are short and rigid (a two-palm pose buried a held prop in his chest,
    // `tools/author_hero_action.py` on Ignition), so the palms meet well out in front at a steep pitch, nothing held between.
    // =============================================================================================
    public static partial class HeroAbilityClips
    {
        private static readonly Vector3 SnRestLeft = new Vector3(0, 0, 15), SnRestRight = new Vector3(0, 0, -15);

        /// <summary>His authored prayer, held and released, grounded on the rig.</summary>
        public static AnimationClip[] BuildSeanAuthored(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("Rago rig is missing a required bone.");
            var clips = new[] { BuildSeanPrayAim(paths), BuildSeanPray(paths) };
            foreach (var clip in clips) GroundIntroduction(clip, root, paths["root"], anchorToRest: true);
            return clips;
        }

        /// <summary>The held prayer, a 1.6 s loop: palms together out in front of his chest, head bowed, a slow breath in and out.</summary>
        private static AnimationClip BuildSeanPrayAim(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-sean-pray-aim", paths);
            PoseKey(b, 0, -.02f, V(6, 0, 0), V(14, 0, 0), V(-86, 0, -40), V(-86, 0, 40), V(4, 0, 8), V(4, 0, -8));
            PoseKey(b, .8f, -.01f, V(3, 0, 0), V(11, 0, 0), V(-90, 0, -42), V(-90, 0, 42), V(4, 0, 8), V(4, 0, -8));
            PoseKey(b, 1.6f, -.02f, V(6, 0, 0), V(14, 0, 0), V(-86, 0, -40), V(-86, 0, 40), V(4, 0, 8), V(4, 0, -8));
            return b.Build();
        }

        /// <summary>
        /// The release, 0.55 s: out of the prayer he drops into his knees and drives both palms down at the court (0.16, the punch),
        /// holds it a beat as the ground answers, and stands.
        /// </summary>
        private static AnimationClip BuildSeanPray(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-sean-pray", paths);
            PoseKey(b, 0, -.02f, V(6, 0, 0), V(14, 0, 0), V(-86, 0, -40), V(-86, 0, 40), V(4, 0, 8), V(4, 0, -8));
            PoseKey(b, .07f, .01f, V(-4, 0, 0), V(4, 0, 0), V(-120, 0, -24), V(-120, 0, 24), V(2, 0, 8), V(2, 0, -8));
            b.PunchAt(.16f);
            b.HoldAt(.16f, .06f);
            PoseKey(b, .16f, -.08f, V(24, 0, 0), V(8, 0, 0), V(-38, 0, -18), V(-38, 0, 18), V(-10, 0, 12), V(-10, 0, -12));
            PoseKey(b, .36f, -.05f, V(14, 0, 0), V(4, 0, 0), V(-34, 0, -10), V(-34, 0, 10), V(-6, 0, 10), V(-6, 0, -10));
            PoseKey(b, .55f, 0, V(0, 0, 0), V(0, 0, 0), SnRestLeft, SnRestRight);
            return b.Build();
        }
    }
}
