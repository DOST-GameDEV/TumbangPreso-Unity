using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // ZACK THE SPEEDSTER (2026-10-25 rework, `docs/reports/zack-kit-2026-10-25/plan.md` section 3). Five clips, `ZackMotionAuthor`
    // bakes them. The look family's motion rule is "snap, then chatter": everything arrives in one frame and then settles.
    //  * `hero-zack-backtrack`: a sharp inhale of current, a flicker-snap with the arms tucked, and the landing in his knees.
    //  * `hero-zack-afterimage`: the quick sideways shrug of the shoulders as the copy peels out of him, and a point after it.
    //  * `hero-zack-wire-aim`, held while the key is down (`HeroAbility.AimPoseAction`): he pulls a wire out from his left fist
    //    across his body, the right hand drawing it taut, a small chatter in the hands.
    //  * `hero-zack-wire`, the release: he throws the coil (post B drops where he aimed) and stamps post A in at his feet.
    //  * `hero-zack-overclock`, after the cutscene hands back: he sets off already charged, from a sprinter's set into a drive.
    //
    // PoseKey (`HeroAbilityClips.Introductions.cs`): PoseKey(b, t, rootY, torso, head, LEFT arm, RIGHT arm, left leg, right
    // leg). Arms V(pitch, twist, spread): negative pitch swings FORWARD; a NEGATIVE spread on the left (positive on the right)
    // brings the arm across the chest.
    // =============================================================================================
    public static partial class HeroAbilityClips
    {
        private static readonly Vector3 ZkRestLeft = new Vector3(0, 0, 15), ZkRestRight = new Vector3(0, 0, -15);

        /// <summary>His authored kit clips, grounded on the rig.</summary>
        public static AnimationClip[] BuildZackAuthored(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("Zack rig is missing a required bone.");
            var clips = new[]
            {
                BuildZackBacktrack(paths), BuildZackAfterimage(paths), BuildZackWireAim(paths), BuildZackWire(paths),
                BuildZackOverclock(paths),
            };
            foreach (var clip in clips) GroundIntroduction(clip, root, paths["root"], anchorToRest: true);
            return clips;
        }

        /// <summary>0.45 s: the inhale (arms flare), the snap (tucked, a frame), the landing in his knees.</summary>
        private static AnimationClip BuildZackBacktrack(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-backtrack", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), ZkRestLeft, ZkRestRight);
            PoseKey(b, .06f, .02f, V(-8, 0, 0), V(-10, 0, 0), V(-24, 0, 34), V(-24, 0, -34), V(2, 0, 6), V(2, 0, -6));
            b.PunchAt(.10f);
            PoseKey(b, .10f, -.02f, V(12, 0, 0), V(6, 0, 0), V(-46, 0, -14), V(-46, 0, 14), V(-8, 0, 6), V(-8, 0, -6));
            b.HoldAt(.10f, .05f);
            PoseKey(b, .28f, -.06f, V(10, 0, 0), V(2, 0, 0), V(-20, 0, 22), V(-20, 0, -22), V(-16, 0, 10), V(-16, 0, -10));
            PoseKey(b, .45f, 0, V(0, 0, 0), V(0, 0, 0), ZkRestLeft, ZkRestRight);
            return b.Build();
        }

        /// <summary>0.5 s: the shrug one way, the copy peels out the other, and his right hand points it on.</summary>
        private static AnimationClip BuildZackAfterimage(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-afterimage", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), ZkRestLeft, ZkRestRight);
            PoseKey(b, .08f, .01f, V(2, -16, 0), V(0, -10, 0), V(-10, 0, 30), V(10, 0, -12), V(0, 0, 6), V(0, 0, -6));
            b.PunchAt(.16f);
            PoseKey(b, .16f, 0, V(4, 14, 0), V(0, 10, 0), V(10, 0, 18), V(-78, 0, 22), V(0, 0, 6), V(0, 0, -6));
            b.HoldAt(.16f, .08f);
            PoseKey(b, .34f, 0, V(2, 6, 0), V(0, 4, 0), V(4, 0, 16), V(-50, 0, 6), V(0, 0, 6), V(0, 0, -6));
            PoseKey(b, .5f, 0, V(0, 0, 0), V(0, 0, 0), ZkRestLeft, ZkRestRight);
            return b.Build();
        }

        /// <summary>The held aim, a 1.2 s loop: the wire pulled from his left fist across his body, taut, the hands chattering.</summary>
        private static AnimationClip BuildZackWireAim(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-wire-aim", paths);
            PoseKey(b, 0, -.02f, V(4, 12, 0), V(0, 8, 0), V(-58, 0, -34), V(-30, 0, 46), V(2, 0, 8), V(-4, 0, -10));
            PoseKey(b, .3f, -.02f, V(4, 12, 0), V(0, 8, 0), V(-62, 0, -32), V(-28, 0, 50), V(2, 0, 8), V(-4, 0, -10));
            PoseKey(b, .6f, -.03f, V(5, 12, 0), V(0, 8, 0), V(-58, 0, -36), V(-32, 0, 44), V(2, 0, 8), V(-4, 0, -10));
            PoseKey(b, .9f, -.02f, V(4, 12, 0), V(0, 8, 0), V(-61, 0, -33), V(-29, 0, 48), V(2, 0, 8), V(-4, 0, -10));
            PoseKey(b, 1.2f, -.02f, V(4, 12, 0), V(0, 8, 0), V(-58, 0, -34), V(-30, 0, 46), V(2, 0, 8), V(-4, 0, -10));
            return b.Build();
        }

        /// <summary>0.55 s: the throw of the coil (0.14), and the stamp that bangs post A into the court at his feet (0.34).</summary>
        private static AnimationClip BuildZackWire(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-wire", paths);
            PoseKey(b, 0, -.02f, V(4, 12, 0), V(0, 8, 0), V(-58, 0, -34), V(-30, 0, 46), V(2, 0, 8), V(-4, 0, -10));
            b.PunchAt(.14f);
            PoseKey(b, .14f, 0, V(10, -14, 0), V(4, -8, 0), V(-20, 0, 20), V(-104, 0, 8), V(-2, 0, 8), V(4, 0, -8));
            PoseKey(b, .26f, .04f, V(6, -6, 0), V(2, -4, 0), V(-14, 0, 22), V(-60, 0, 6), V(-34, 0, 8), V(2, 0, -8));
            b.PunchAt(.34f);
            PoseKey(b, .34f, -.05f, V(14, 0, 0), V(6, 0, 0), V(-10, 0, 26), V(-30, 0, -20), V(6, 0, 8), V(-10, 0, -8));
            b.HoldAt(.34f, .05f);
            PoseKey(b, .55f, 0, V(0, 0, 0), V(0, 0, 0), ZkRestLeft, ZkRestRight);
            return b.Build();
        }

        /// <summary>0.6 s: a sprinter's set (deep, one hand down) driving up and out, already charged.</summary>
        private static AnimationClip BuildZackOverclock(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-overclock", paths);
            PoseKey(b, 0, -.14f, V(34, 0, 0), V(-14, 0, 0), V(-70, 0, 10), V(20, 0, -10), V(-40, 0, 6), V(20, 0, -6));
            PoseKey(b, .2f, -.10f, V(30, 0, 0), V(-12, 0, 0), V(-66, 0, 12), V(24, 0, -12), V(-36, 0, 6), V(18, 0, -6));
            b.PunchAt(.3f);
            PoseKey(b, .3f, .02f, V(18, 0, 0), V(-6, 0, 0), V(30, 0, 12), V(-60, 0, -12), V(16, 0, 6), V(-34, 0, -6));
            PoseKey(b, .45f, .01f, V(12, 0, 0), V(-2, 0, 0), V(-40, 0, 12), V(36, 0, -12), V(-28, 0, 6), V(14, 0, -6));
            PoseKey(b, .6f, 0, V(0, 0, 0), V(0, 0, 0), ZkRestLeft, ZkRestRight);
            return b.Build();
        }
    }
}
