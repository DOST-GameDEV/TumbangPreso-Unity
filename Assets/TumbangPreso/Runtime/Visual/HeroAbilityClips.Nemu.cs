using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // NEMU'S BODY CASTS FOR HER GHOSTS (owner 2026-10-15: *"cast animations lock in make it amazing"*). DON'T LOOK,
    // SKITTER and LOOKOUT used Kuro's old `hero-nemu-project` and `hero-nemu-guard`; each now has its own clip, and each
    // ACTS OUT what she asks of that ghost: the hush and the curse laid on the ground, the "go fetch" flick, the little
    // lantern ghost lifted out of her cupped hands and let go.
    //
    // ⚠️⚠️ HER DIRECTION IS THE MARIONETTE. Her ultimate ends on THE STRINGS, and her ghosts move in stop-motion; so her
    // casts DRIFT IN SLOWLY, soft and eerie, and then SNAP into the pose on one punch key and HANG there a beat (`HoldAt`),
    // like a puppet yanked on its strings, before she melts back to rest. Her head always moves first and tilts too far:
    // that is the creepy part. No other hero's casts hang on a crooked head.
    //
    // Keys on the rig's bones only, baked by `Editor/NemuMotionAuthor` on every roster build (`AnimationClip.SetCurve` is
    // editor-only for these clips). PoseKey (`HeroAbilityClips.Introductions.cs`): PoseKey(b, t, rootY, torso, head, LEFT
    // arm, RIGHT arm, left leg, right leg, left forearm, right forearm). Arms V(pitch, twist, spread): negative pitch swings
    // FORWARD (-90 straight ahead, -180 overhead); rest V(0,0,15) left and V(0,0,-15) right; a NEGATIVE spread on the left
    // (positive on the right) brings the arm across the chest. Torso V(lean forward, twist to her right, lean left). Head
    // V(down, turn left, tilt left). Legs V(pitch, _, spread), negative forward. `Fore(fold, stretch)`, 0 straight.
    // ⚠️ HER LEGS ARE SHORT UNDER A LONG HOODIE: big leg swings tip her whole silhouette onto one shoe (filmstrip v1's crouch,
    // legs -30/+28), so her legs stay within a few degrees and the bow is carried by the torso and the head.
    // =============================================================================================
    public static partial class HeroAbilityClips
    {
        private static readonly Vector3 NemuRestLeft = new Vector3(0, 0, 15), NemuRestRight = new Vector3(0, 0, -15);

#if UNITY_EDITOR
        /// <summary>Her three ghost casts, grounded on the rig (`NemuMotionAuthor`).</summary>
        public static AnimationClip[] BuildNemuAuthored(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("Nemu rig is missing a required bone.");
            var clips = new[] { BuildNemuDontLook(paths), BuildNemuSkitter(paths), BuildNemuLookout(paths) };
            foreach (var clip in clips) GroundIntroduction(clip, root, paths["root"], anchorToRest: true);
            return clips;
        }
#endif

        /// <summary>
        /// DON'T LOOK, 0.95 s: "shh", then the curse laid on the ground.
        ///  * 0.00 to 0.20: her head tips over, far, before anything else moves; her right forefinger comes up to her lips;
        ///  * 0.20 to 0.34: the hush, HELD (the finger on her lips, her head crooked, looking at you);
        ///  * 0.42 (punch): she SNAPS down into a crouch, her left palm slapped flat at the court in front of her, her head
        ///    jerked down to the spot: the ghost goes into the ground there;
        ///  * 0.62: her left hand drags up off the court, slow, fingers last, as if pulling a thread out of it;
        ///  * settle, the finger leaving her lips last.
        /// </summary>
        private static AnimationClip BuildNemuDontLook(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-nemu-dontlook", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), NemuRestLeft, NemuRestRight);
            PoseKey(b, .10f, 0, V(2, 0, 4), V(4, 0, 18), V(4, 0, 18), V(-30, 0, -6), V(0, 0, 4), V(0, 0, -4), Fore(10), Fore(70));
            PoseKey(b, .20f, -.01f, V(5, -6, 6), V(8, -4, 30), V(8, 0, 20), V(-62, 0, 26), V(0, 0, 4), V(0, 0, -4), Fore(10), Fore(140));
            PoseKey(b, .34f, -.01f, V(6, -6, 7), V(8, -4, 32), V(8, 0, 20), V(-62, 0, 26), V(0, 0, 4), V(0, 0, -4), Fore(10), Fore(142));
            b.PunchAt(.42f);
            b.HoldAt(.42f, .08f);
            PoseKey(b, .42f, -.12f, V(18, -10, 2), V(40, 6, 14), V(-64, 6, 2), V(-58, 0, 24), V(-4, 0, 12), V(4, 0, -12), Fore(16), Fore(138));
            PoseKey(b, .62f, -.08f, V(12, -6, 2), V(22, 4, 12), V(-80, 4, 6), V(-56, 0, 24), V(-2, 0, 9), V(2, 0, -9), Fore(64), Fore(136));
            PoseKey(b, .78f, -.03f, V(8, -2, 2), V(6, 0, 8), V(-30, 0, 14), V(-40, 0, 12), V(-6, 0, 5), V(6, 0, -5), Fore(40), Fore(90));
            PoseKey(b, .95f, 0, V(0, 0, 0), V(0, 0, 0), NemuRestLeft, NemuRestRight);
            return b.Build();
        }

        /// <summary>
        /// SKITTER, 0.75 s: "go on, fetch".
        ///  * 0.00 to 0.12: her head turns to the slipper first; her right hand draws back across her body to her left
        ///    shoulder, elbow folded, the trunk winding left;
        ///  * 0.18 (punch): the FLICK: the right arm cracks out at the slipper, two fingers pointing, the forearm overshooting
        ///    long, the trunk snapping round to it, the left arm flung back; held a beat;
        ///  * 0.30 to 0.48: the point stays on it and her fingers wiggle (a little beckon in reverse: "that one, go"), her head
        ///    tipping playfully;
        ///  * settle.
        /// </summary>
        private static AnimationClip BuildNemuSkitter(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-nemu-skitter", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), NemuRestLeft, NemuRestRight);
            PoseKey(b, .06f, 0, V(0, -4, 0), V(4, -14, 6), V(4, 0, 16), V(-24, 0, 18), V(0, 0, 4), V(0, 0, -4), Fore(10), Fore(60));
            PoseKey(b, .12f, -.03f, V(-4, -18, 0), V(6, -10, 10), V(10, 0, 22), V(-46, 10, 44), V(-4, 0, 5), V(6, 0, -5), Fore(14), Fore(124));
            b.PunchAt(.18f);
            b.HoldAt(.18f, .06f);
            PoseKey(b, .18f, -.04f, V(12, 16, -4), V(-6, 8, -8), V(36, 10, 30), V(-96, -6, -12), V(-12, 0, 6), V(10, 0, -6), Fore(20), Fore(0, 1.18f));
            PoseKey(b, .30f, -.02f, V(8, 12, -2), V(-4, 6, 14), V(24, 6, 24), V(-90, -4, -10), V(-8, 0, 5), V(6, 0, -5), Fore(24), Fore(14, 1.04f));
            PoseKey(b, .39f, -.02f, V(8, 12, -2), V(-4, 6, 6), V(24, 6, 24), V(-86, -4, -10), V(-8, 0, 5), V(6, 0, -5), Fore(24), Fore(34));
            PoseKey(b, .48f, -.02f, V(8, 12, -2), V(-4, 6, 16), V(24, 6, 24), V(-90, -4, -10), V(-8, 0, 5), V(6, 0, -5), Fore(24), Fore(10));
            PoseKey(b, .62f, 0, V(3, 4, 0), V(0, 2, 6), V(8, 0, 18), V(-40, 0, -14), V(-2, 0, 4), V(2, 0, -4), Fore(14), Fore(30));
            PoseKey(b, .75f, 0, V(0, 0, 0), V(0, 0, 0), NemuRestLeft, NemuRestRight);
            return b.Build();
        }

        /// <summary>
        /// LOOKOUT, 0.95 s: the little lantern ghost, lifted out of her cupped hands and let go.
        ///  * 0.00 to 0.18: she bows over her hands, both cupped together low in front of her, head bent to them (it is
        ///    in there, small), a slight dip;
        ///  * 0.36: she rises onto her toes, the cupped hands lifted to her face, her head tipping to look into them;
        ///  * 0.46 (punch): the hands SPRING open, up and apart, her chin snapping up after it, the trunk arching back: it is
        ///    out; held a beat;
        ///  * 0.66: her hands float down slowly, fingers trailing, her head following it off toward the can;
        ///  * settle.
        /// </summary>
        private static AnimationClip BuildNemuLookout(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-nemu-lookout", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), NemuRestLeft, NemuRestRight);
            PoseKey(b, .18f, -.05f, V(16, 0, 0), V(26, 0, -10), V(-46, 0, -26), V(-46, 0, 26), V(-4, 0, 4), V(4, 0, -4), Fore(72), Fore(72));
            PoseKey(b, .36f, .02f, V(2, 0, 0), V(6, 0, 16), V(-78, 0, -24), V(-78, 0, 24), V(0, 0, 3), V(0, 0, -3), Fore(104), Fore(104));
            b.PunchAt(.46f);
            b.HoldAt(.46f, .08f);
            PoseKey(b, .46f, .03f, V(-10, 0, 0), V(-18, 0, -4), V(-128, 0, 30), V(-128, 0, -30), V(0, 0, 4), V(0, 0, -4), Fore(14, 1.08f), Fore(14, 1.08f));
            PoseKey(b, .66f, 0, V(-4, 6, 0), V(-6, 12, -8), V(-84, 0, 28), V(-96, 0, -24), V(0, 0, 4), V(0, 0, -4), Fore(30), Fore(26));
            PoseKey(b, .82f, 0, V(-1, 3, 0), V(-2, 6, -4), V(-30, 0, 20), V(-38, 0, -20), V(0, 0, 4), V(0, 0, -4), Fore(24), Fore(24));
            PoseKey(b, .95f, 0, V(0, 0, 0), V(0, 0, 0), NemuRestLeft, NemuRestRight);
            return b.Build();
        }
    }
}
