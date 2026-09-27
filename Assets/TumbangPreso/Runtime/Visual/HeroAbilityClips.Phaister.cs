using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // PHAISTER'S BODY CASTS (HERO-10, 2026-09-27). Owner: *"it needs really great presentation VFx ANIIMATION SFX AND
    // DIRECTING"*, *"refine the animations of phaister herself btw try to really show her personality in everyting"*,
    // *"make her a frigging witchh not a showman"*. One clip per skill, none shared (the old `hero-phaister-hex` served
    // both curses). Plan: `docs/reports/phaister-kit-2026-09-27/plan.md` sections 4 and 4.6.
    //
    // ⚠️⚠️ HER DIRECTION IS SMALL PRECISE HANDS AND AN UNHURRIED BODY. She is a witch who enjoys it: the magic is in the
    // wrists (a prick of a pin, a flick, a twist of a doll's head), the trunk barely moves, the weight settles onto one hip,
    // the head tilts toward what she is doing, and she never strains. The one big body moment is OMEN, where the power
    // lifts her off the court. Every cast ends on a small personal beat, each a different one: a hand to the hat brim
    // (VANISHING ACT), a glance down at the thrown doll's path with the throwing hand left open (MANIKA MISCHIEF), the pin
    // held out at her victim like a pointer (SPOTLIGHT PIN).
    //
    // PoseKey (`HeroAbilityClips.Introductions.cs`): PoseKey(b, t, rootY, torso, head, LEFT arm, RIGHT arm, left leg, right
    // leg). Arms V(pitch, twist, spread): negative pitch swings FORWARD (-90 straight ahead, -180 overhead); rest V(0,0,15)
    // left and V(0,0,-15) right; a NEGATIVE spread on the left (positive on the right) brings the arm across the chest.
    // Torso V(lean forward, twist to her right, lean left). Legs V(pitch, _, spread), negative forward.
    // =============================================================================================
    public static partial class HeroAbilityClips
    {
        private static readonly Vector3 PhRestLeft = new Vector3(0, 0, 15), PhRestRight = new Vector3(0, 0, -15);

        /// <summary>Her four authored casts, grounded on the rig; OMEN carries its own float (`PhaisterOmenLift`).</summary>
        public static AnimationClip[] BuildPhaisterAuthored(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("Phaister rig is missing a required bone.");
            var clips = new[] { BuildPhaisterSwarm(paths), BuildPhaisterManika(paths), BuildPhaisterPin(paths), BuildPhaisterOmen(paths) };
            foreach (var clip in clips)
                GroundIntroduction(clip, root, paths["root"], anchorToRest: true,
                    lift: clip.name == "hero-phaister-omen" ? (System.Func<float, float>)PhaisterOmenLift : null);
            return clips;
        }

        /// <summary>
        /// VANISHING ACT, 0.75 s. Her body is HIDDEN from 0 to 0.30 while the swarm carries her (`PhaisterSwarm`), so the part
        /// anyone sees is the ARRIVAL, and it is keyed for that:
        ///  * 0.00: the burst, arms flung wide and back, chin up (seen for a frame on some screens before the hide lands);
        ///  * 0.30 (punch, the reveal): knit back crouched low, knees bent, the left hand already rising, the right arm out for
        ///    balance, the head down: she is still being put together;
        ///  * 0.48: she straightens with the LEFT HAND ON HER HAT BRIM, settling it (her beat), the hip cocked;
        ///  * 0.75: rest.
        /// </summary>
        private static AnimationClip BuildPhaisterSwarm(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-swarm", paths);
            PoseKey(b, 0, 0, V(-12, 0, 0), V(-14, 0, 0), V(-40, 0, 70), V(-40, 0, -70), V(8, 0, 8), V(8, 0, -8));
            PoseKey(b, .12f, -.10f, V(20, 0, 0), V(18, 0, 0), V(-30, 0, 30), V(-20, 0, -40), V(-26, 0, 6), V(-18, 0, -6));
            b.PunchAt(.30f);
            b.HoldAt(.30f, .05f);
            PoseKey(b, .30f, -.12f, V(22, -6, 0), V(20, 0, 0), V(-110, 10, -10), V(-30, 0, -55), V(-30, 0, 6), V(-20, 0, -8));
            PoseKey(b, .48f, -.02f, V(2, -10, 4), V(-6, -8, -6), V(-168, 20, -18), V(-6, 0, -22), V(-8, 0, 4), V(4, 0, -6));
            PoseKey(b, .62f, 0, V(0, -6, 3), V(-4, -4, -4), V(-150, 16, -16), V(0, 0, -18), V(-4, 0, 3), V(2, 0, -5));
            PoseKey(b, .75f, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            return b.Build();
        }

        /// <summary>
        /// MANIKA MISCHIEF, 0.8 s, the prick and the throw:
        ///  * 0.00 to 0.14: the right hand unhooks the manika from her left hip (the arm across the body, low);
        ///  * 0.26: the doll up at her face in the right hand, her head tipped toward it, the LEFT hand pricking it with a pin
        ///    (both hands close together in front of her chin): the tell other players read;
        ///  * 0.40: the wind-up, the right arm drawn back over the shoulder, the trunk turning right, weight on the back foot;
        ///  * 0.48 (punch): the overhand flick, the arm snapping forward and down, the trunk turning through;
        ///  * 0.62: her beat: the throwing hand left OPEN in the air and her head following the doll's path;
        ///  * 0.80: rest.
        /// </summary>
        private static AnimationClip BuildPhaisterManika(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-manika", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            PoseKey(b, .14f, -.02f, V(10, -14, -4), V(14, -10, 0), V(0, 0, 14), V(-24, 10, 34), V(-4, 0, 4), V(4, 0, -4));
            PoseKey(b, .26f, 0, V(-2, 6, 4), V(10, 10, -10), V(-96, -10, -30), V(-118, 12, 18), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .40f, -.02f, V(-8, 24, 2), V(-6, 18, 0), V(-60, 0, 10), V(-168, 26, -12), V(-12, 0, 5), V(12, 0, -5));
            b.PunchAt(.48f);
            b.HoldAt(.48f, .04f);
            PoseKey(b, .48f, -.03f, V(14, -18, 0), V(4, -12, 0), V(-30, 0, 24), V(-70, -10, 6), V(-18, 0, 5), V(12, 0, -5));
            PoseKey(b, .62f, -.01f, V(8, -14, 0), V(6, -16, 0), V(-20, 0, 20), V(-84, -14, 2), V(-10, 0, 4), V(8, 0, -4));
            PoseKey(b, .80f, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            return b.Build();
        }

        /// <summary>
        /// SPOTLIGHT PIN, 0.8 s, the pin drawn and stabbed:
        ///  * 0.00 to 0.14: the right hand reaches up to her hat band and draws the pin (the arm up past the brim);
        ///  * 0.24: the pin held upright in front of her face like a wand, a small twirl of the wrist, her chin lifted;
        ///  * 0.34 (punch): she STABS it down into the air in front of her, the trunk dipping over it, the left arm opening out
        ///    to the side for balance;
        ///  * 0.52: her beat: the pin held out at arm's length, level, pointing at her victim, head tilted, weight on one hip;
        ///  * 0.80: rest (she slides the pin home).
        /// </summary>
        private static AnimationClip BuildPhaisterPin(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-pin", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            PoseKey(b, .14f, 0, V(-4, 4, -2), V(-8, 4, 0), V(0, 0, 18), V(-176, 10, 12), V(-2, 0, 4), V(2, 0, -4));
            PoseKey(b, .24f, .01f, V(-6, 0, 0), V(-12, 0, 0), V(-20, 0, 24), V(-120, -30, 16), V(-4, 0, 4), V(2, 0, -4));
            b.PunchAt(.34f);
            b.HoldAt(.34f, .05f);
            PoseKey(b, .34f, -.05f, V(18, 0, 0), V(12, 0, 0), V(-10, 0, 46), V(-46, 0, 10), V(-16, 0, 6), V(10, 0, -6));
            PoseKey(b, .52f, -.01f, V(4, -6, 4), V(-4, -6, -8), V(-6, 0, 22), V(-86, 0, 4), V(-6, 0, 5), V(4, 0, -5));
            PoseKey(b, .66f, 0, V(2, -4, 3), V(-2, -4, -6), V(-2, 0, 18), V(-80, 0, 2), V(-4, 0, 4), V(2, 0, -4));
            PoseKey(b, .80f, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            return b.Build();
        }

        /// <summary>
        /// OMEN's live cast, 2.8 s, PICKING UP WHERE THE CUTSCENE ENDS (the method: never show the same thing twice; the cutscene
        /// already showed her rise, the butterflies pouring out and the eye forming in her hands and thrown). So play opens on her
        /// hovering, arms open, watching the unstable eye she has just thrown; she channels into it (two small pulls of both hands
        /// toward it, 0.7 and 1.4 s); at 2.2 s, the moment it snaps open, she thrusts both palms at it (the punch); then she drops
        /// back to the court on one foot (2.5) and rests (2.8). `PhaisterOmenLift` holds her at the cutscene's last height.
        /// </summary>
        private static AnimationClip BuildPhaisterOmen(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-omen", paths);
            PoseKey(b, 0, 0, V(4, -6, 0), V(-4, -8, 4), V(-58, 0, 44), V(-46, 0, -40), V(-2, 0, 4), V(8, 0, -4));
            PoseKey(b, .7f, 0, V(8, -4, 0), V(-2, -6, 4), V(-80, 0, 30), V(-72, 0, -28), V(-2, 0, 4), V(10, 0, -4));
            PoseKey(b, 1.0f, 0, V(4, -6, 0), V(-4, -8, 4), V(-60, 0, 42), V(-50, 0, -38), V(-2, 0, 4), V(8, 0, -4));
            PoseKey(b, 1.4f, 0, V(9, -3, 0), V(-2, -5, 3), V(-84, 0, 28), V(-76, 0, -26), V(-2, 0, 4), V(10, 0, -4));
            PoseKey(b, 1.9f, 0, V(-6, 0, 0), V(-8, 0, 0), V(-120, 0, 20), V(-120, 0, -20), V(-4, 0, 4), V(12, 0, -4));
            b.PunchAt(2.2f);
            b.HoldAt(2.2f, .06f);
            PoseKey(b, 2.2f, 0, V(18, 0, 0), V(6, 0, 0), V(-86, 0, 10), V(-86, 0, -10), V(-8, 0, 5), V(14, 0, -5));
            PoseKey(b, 2.5f, -.05f, V(8, 0, 0), V(2, 0, 0), V(-40, 0, 16), V(-40, 0, -16), V(-14, 0, 6), V(8, 0, -6));
            PoseKey(b, 2.8f, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            return b.Build();
        }

        /// <summary>OMEN's float in play: held at the cutscene's last height (0.44 m) with a slow bob, down to the court at 2.2 to 2.5.</summary>
        private static float PhaisterOmenLift(float t)
        {
            if (t < 2.2f) return 0.44f + 0.03f * Mathf.Sin(t * 3f);
            if (t < 2.5f) { float u = (t - 2.2f) / 0.3f; return 0.44f * (1f - u * u); }
            return 0f;
        }
    }
}
