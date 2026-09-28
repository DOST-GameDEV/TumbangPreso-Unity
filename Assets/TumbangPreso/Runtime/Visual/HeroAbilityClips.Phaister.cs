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
            var clips = new[]
            {
                BuildPhaisterSwarm(paths), BuildPhaisterManika(paths), BuildPhaisterPin(paths), BuildPhaisterOmen(paths),
                BuildPhaisterSwarmAim(paths), BuildPhaisterManikaAim(paths), BuildPhaisterOmenAim(paths),
                BuildPhaisterDrain(paths), BuildPhaisterDrainAim(paths), BuildPhaisterWring(paths),
                BuildPhaisterHexReach(paths), BuildPhaisterHexReachAim(paths), BuildPhaisterHexStab(paths),
            };
            foreach (var clip in clips)
                GroundIntroduction(clip, root, paths["root"], anchorToRest: true,
                    lift: clip.name == "hero-phaister-omen" ? (System.Func<float, float>)PhaisterOmenLift : null);
            return clips;
        }

        /// <summary>
        /// VANISHING ACT, 0.75 s. Her body is HIDDEN from 0 to 0.30 while the swarm carries her (`PhaisterSwarm`), so the part
        /// anyone sees is the ARRIVAL, and it is keyed for that:
        ///  * 0.00: the burst out of the aim tell (wrists crossed), arms flung wide and back, chin up (a frame at most, before the
        ///    hide lands);
        ///  * 0.30 (punch, the reveal): v2 (film v7: the old reveal pitched the torso 22 degrees and the head 20 more, so she landed
        ///    in a bow, a faceplant): knit back UPRIGHT and low, her arms wrapped round herself as the swarm packs in from the feet
        ///    up, head a little down: she is still being put together;
        ///  * 0.46: she rises out of it, a small pop, the LEFT HAND GOING UP TO HER HAT BRIM (her beat), the right hand flicking
        ///    out to the side, chin up;
        ///  * 0.60: settled on one hip, still holding the brim;
        ///  * 0.75: rest.
        /// </summary>
        private static AnimationClip BuildPhaisterSwarm(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-swarm", paths);
            PoseKey(b, 0, 0, V(-12, 0, 0), V(-14, 0, 0), V(-40, 0, 70), V(-40, 0, -70), V(8, 0, 8), V(8, 0, -8));
            PoseKey(b, .12f, -.06f, V(6, 0, 0), V(8, 0, 0), V(-58, 0, -30), V(-54, 0, 30), V(-8, 0, 6), V(-6, 0, -6));
            b.PunchAt(.30f);
            b.HoldAt(.30f, .05f);
            PoseKey(b, .30f, -.07f, V(5, -4, 0), V(9, -3, 0), V(-64, 4, -42), V(-60, -4, 40), V(-6, 0, 5), V(-4, 0, -5));
            PoseKey(b, .46f, .02f, V(-4, -8, 2), V(-7, -8, -5), V(-156, 18, -16), V(-18, 0, -34), V(-4, 0, 4), V(4, 0, -6));
            PoseKey(b, .60f, 0, V(-1, -6, 3), V(-4, -5, -5), V(-150, 16, -16), V(-4, 0, -22), V(-3, 0, 3), V(2, 0, -5));
            PoseKey(b, .75f, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            return b.Build();
        }

        // =============================================================================================
        // HER TELLS WHILE SHE AIMS (HERO-10, film v7: holding a skill left her in the shared idle, and the doll's prick came after
        // the release). Looping clips, held for as long as the key is down (`HeroAbility.AimPoseAction`,
        // `CharacterAnimator.AimPose`); the release clip starts from each. `PhaisterMotionAuthor` bakes them looping.
        // =============================================================================================

        /// <summary>
        /// VANISHING ACT's tell, a 1.6 s loop: wrists crossed at her chest (plan 4.1), a touch forward over them, her weight on one
        /// hip and her head up at where she is going. Moths crawl out of her cuffs (`PhaisterCuffMoths`). A slow sway, nothing more:
        /// she is sure of it.
        /// </summary>
        private static AnimationClip BuildPhaisterSwarmAim(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-swarm-aim", paths);
            PoseKey(b, 0, 0, V(4, 0, 2), V(2, -4, 0), V(-72, 6, -38), V(-70, -6, 38), V(-2, 0, 4), V(2, 0, -4));
            PoseKey(b, .8f, 0, V(5, 4, 1), V(3, 4, -2), V(-76, 6, -40), V(-74, -6, 40), V(-2, 0, 4), V(2, 0, -4));
            PoseKey(b, 1.6f, 0, V(4, 0, 2), V(2, -4, 0), V(-72, 6, -38), V(-70, -6, 38), V(-2, 0, 4), V(2, 0, -4));
            return b.Build();
        }

        /// <summary>
        /// MANIKA MISCHIEF's tell, a 1.24 s loop, two pricks (`PhaisterHandDoll.PrickEvery`): the doll up at her chin in her right
        /// hand, her head tipped to it, the left hand jabbing the pin in (a quick in at 0.07, out by 0.18), and between the pricks a
        /// glance up at her target with the smirk (her ink face does the smirk; the tilt sells it).
        /// </summary>
        private static AnimationClip BuildPhaisterManikaAim(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-manika-aim", paths);
            PoseKey(b, 0, 0, V(-2, 6, 4), V(10, 10, -10), V(-92, -10, -28), V(-118, 12, 18), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .07f, 0, V(0, 8, 4), V(12, 12, -12), V(-104, -8, -36), V(-120, 12, 18), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .18f, 0, V(-2, 6, 4), V(10, 10, -10), V(-92, -10, -28), V(-118, 12, 18), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .45f, 0, V(-3, 4, 4), V(2, 4, -6), V(-90, -10, -26), V(-116, 10, 16), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .62f, 0, V(-2, 6, 4), V(10, 10, -10), V(-92, -10, -28), V(-118, 12, 18), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .69f, 0, V(0, 8, 4), V(12, 12, -12), V(-104, -8, -36), V(-120, 12, 18), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .80f, 0, V(-2, 6, 4), V(10, 10, -10), V(-92, -10, -28), V(-118, 12, 18), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, 1.05f, 0, V(-3, 5, 4), V(4, 6, -12), V(-90, -10, -26), V(-117, 12, 17), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, 1.24f, 0, V(-2, 6, 4), V(10, 10, -10), V(-92, -10, -28), V(-118, 12, 18), V(-6, 0, 4), V(6, 0, -4));
            return b.Build();
        }

        /// <summary>
        /// OMEN's tell, a 1.4 s loop, while she chooses where AND HOW HIGH (plan 4.4; the ghost eye and its line are
        /// `PhaisterAimSigil`): her chin up at the spot in the air, her right hand raised toward it as if weighing it, her left
        /// holding her hat on as she looks up.
        /// </summary>
        private static AnimationClip BuildPhaisterOmenAim(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-omen-aim", paths);
            PoseKey(b, 0, 0, V(-4, 0, 2), V(-14, 0, 0), V(-150, 16, -16), V(-128, 0, -12), V(-2, 0, 4), V(4, 0, -4));
            PoseKey(b, .7f, 0, V(-5, 3, 2), V(-16, 4, 2), V(-152, 16, -16), V(-134, 4, -14), V(-2, 0, 4), V(4, 0, -4));
            PoseKey(b, 1.4f, 0, V(-4, 0, 2), V(-14, 0, 0), V(-150, 16, -16), V(-128, 0, -12), V(-2, 0, 4), V(4, 0, -4));
            return b.Build();
        }

        /// <summary>
        /// MANIKA MISCHIEF's release, 0.65 s, THE THROW ONLY. v2 (film v7): the clip used to begin on the release with the unhook
        /// and the prick, and throw at 0.48 s, while the doll leaves on the release, so it flew half a second before her arm moved.
        /// The unhook and the prick are the hold now (`hero-phaister-manika-aim`); this starts from that pose:
        ///  * 0.00: the doll up at her chin in the right hand (the aim pose);
        ///  * 0.05: a quick draw back over the shoulder, the trunk turning right;
        ///  * 0.11 (punch): the overhand flick, the arm snapping forward and down, the trunk turning through (the doll is away);
        ///  * 0.30: her beat: the throwing hand left OPEN in the air and her head following the doll's path, tipped with the smirk;
        ///  * 0.65: rest.
        /// </summary>
        private static AnimationClip BuildPhaisterManika(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-manika", paths);
            PoseKey(b, 0, 0, V(-2, 6, 4), V(10, 10, -10), V(-92, -10, -28), V(-118, 12, 18), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .05f, -.01f, V(-8, 22, 2), V(-6, 16, 0), V(-50, 0, 12), V(-165, 26, -12), V(-12, 0, 5), V(12, 0, -5));
            b.PunchAt(.11f);
            b.HoldAt(.11f, .04f);
            PoseKey(b, .11f, -.03f, V(14, -18, 0), V(4, -12, 0), V(-30, 0, 24), V(-70, -10, 6), V(-18, 0, 5), V(12, 0, -5));
            PoseKey(b, .30f, -.01f, V(8, -14, 0), V(6, -16, 6), V(-20, 0, 20), V(-84, -14, 2), V(-10, 0, 4), V(8, 0, -4));
            PoseKey(b, .65f, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
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

        // =============================================================================================
        // v3, THE REACH (HERO-10, plan 9.5): both curses mark by holding her hand out at someone for 2 s, the slipper tucked
        // into her belt at the back (`Carrier`, `CharacterMotor.StowsCarriedSlipper`) so the RIGHT hand is free to reach and the
        // LEFT unhooks the doll. Each curse reaches its own way, so a player reads which one is coming from her silhouette:
        // DRAIN at the chest, leaning back as if hauling a rope; HEX higher, at the head, the doll lifted to her own cheek and
        // her peeking over it. The lock is a one-shot; the hold loops for as long as the body is reaching
        // (`CharacterAnimator.ReachPose`), which is replicated body state, so every screen sees the same arm.
        // =============================================================================================

        /// <summary>
        /// CURSE: DRAIN's lock, 0.40 s:
        ///  * 0.06: the right hand dips back to the belt (the slipper goes in), the left unhooks the doll from her hip;
        ///  * 0.16 (punch): the right arm whips up at chest height, palm out at them, her weight thrown back onto the rear foot;
        ///  * 0.40: settled into the haul (`hero-phaister-drain-aim`'s first key).
        /// </summary>
        private static AnimationClip BuildPhaisterDrain(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-drain", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            PoseKey(b, .06f, 0, V(2, -6, 0), V(2, -4, 0), V(-10, 0, 8), V(22, 0, -22), V(-2, 0, 4), V(2, 0, -4));
            b.PunchAt(.16f);
            b.HoldAt(.16f, .04f);
            PoseKey(b, .16f, -.02f, V(-8, 12, 0), V(-6, 8, 0), V(-44, 0, -10), V(-94, -12, 6), V(-12, 0, 5), V(10, 0, -5));
            PoseKey(b, .40f, 0, V(-9, 10, 0), V(-5, 6, 0), V(-42, 0, -8), V(-90, -12, 4), V(-12, 0, 4), V(9, 0, -4));
            return b.Build();
        }

        /// <summary>
        /// CURSE: DRAIN's hold, a 0.9 s loop for as long as she reaches: leaning back on the rear foot as if hauling a rope, the
        /// reaching arm trembling a few degrees, a heave back at 0.45 as the thread tightens; the left hand low with the doll.
        /// </summary>
        private static AnimationClip BuildPhaisterDrainAim(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-drain-aim", paths);
            PoseKey(b, 0, 0, V(-9, 10, 0), V(-5, 6, 0), V(-42, 0, -8), V(-90, -12, 4), V(-12, 0, 4), V(9, 0, -4));
            PoseKey(b, .15f, 0, V(-10, 11, 0), V(-5, 7, 0), V(-42, 0, -8), V(-92, -10, 5), V(-12, 0, 4), V(9, 0, -4));
            PoseKey(b, .30f, 0, V(-9, 10, 0), V(-4, 6, 0), V(-43, 0, -8), V(-89, -13, 3), V(-12, 0, 4), V(9, 0, -4));
            PoseKey(b, .45f, -.01f, V(-12, 10, 0), V(-6, 6, 0), V(-40, 0, -9), V(-91, -11, 5), V(-13, 0, 4), V(11, 0, -4));
            PoseKey(b, .60f, 0, V(-10, 10, 0), V(-5, 6, 0), V(-42, 0, -8), V(-90, -12, 4), V(-12, 0, 4), V(9, 0, -4));
            PoseKey(b, .75f, 0, V(-9, 11, 0), V(-5, 7, 0), V(-43, 0, -8), V(-92, -12, 5), V(-12, 0, 4), V(9, 0, -4));
            PoseKey(b, .90f, 0, V(-9, 10, 0), V(-5, 6, 0), V(-42, 0, -8), V(-90, -12, 4), V(-12, 0, 4), V(9, 0, -4));
            return b.Build();
        }

        /// <summary>
        /// CURSE: DRAIN's wring, 1.85 s, from the mark to the drain (plan 9.5: *"both hands on the doll of them, twisting it
        /// tighter and tighter (she keeps walking)"*): both hands on the doll at her chest, three twists each harder than the last,
        /// the trunk turning with them; the last hard wring lands at 1.50, the moment DRAINED does, head bowed over it; then rest.
        /// </summary>
        private static AnimationClip BuildPhaisterWring(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-wring", paths);
            PoseKey(b, 0, 0, V(4, 0, 0), V(10, 0, 0), V(-70, -6, -30), V(-70, 6, 30), V(-2, 0, 4), V(2, 0, -4));
            PoseKey(b, .25f, 0, V(4, -6, 0), V(10, -3, 0), V(-76, -30, -30), V(-64, 30, 30), V(-2, 0, 4), V(2, 0, -4));
            PoseKey(b, .50f, 0, V(5, 6, 0), V(11, 3, 0), V(-64, 20, -32), V(-76, -20, 32), V(-2, 0, 4), V(2, 0, -4));
            PoseKey(b, .75f, 0, V(6, -9, 0), V(12, -4, 0), V(-78, -40, -30), V(-62, 40, 30), V(-2, 0, 4), V(2, 0, -4));
            PoseKey(b, 1.00f, 0, V(6, 9, 0), V(12, 4, 0), V(-62, 28, -33), V(-78, -28, 33), V(-2, 0, 4), V(2, 0, -4));
            PoseKey(b, 1.25f, 0, V(7, -12, 0), V(13, -5, 0), V(-80, -50, -30), V(-60, 50, 30), V(-2, 0, 4), V(2, 0, -4));
            b.PunchAt(1.50f);
            b.HoldAt(1.50f, .08f);
            PoseKey(b, 1.50f, -.03f, V(10, -14, 0), V(16, -6, 0), V(-84, -62, -28), V(-56, 62, 28), V(-4, 0, 4), V(4, 0, -4));
            PoseKey(b, 1.85f, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            return b.Build();
        }

        /// <summary>
        /// CURSE: HEX's lock, 0.40 s: the right hand tucks the slipper (0.06), then the arm rises HIGH, at their head, palm out
        /// (0.16, punch), while the left lifts the doll to her own cheek and her head tips to peek over it; settles into the hold.
        /// </summary>
        private static AnimationClip BuildPhaisterHexReach(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-hexreach", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            PoseKey(b, .06f, 0, V(2, -6, 0), V(2, -4, 0), V(-30, 0, 4), V(22, 0, -22), V(-2, 0, 4), V(2, 0, -4));
            b.PunchAt(.16f);
            b.HoldAt(.16f, .04f);
            PoseKey(b, .16f, .01f, V(-3, -6, -4), V(9, -11, 11), V(-120, -12, -19), V(-120, -8, 7), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .40f, 0, V(-2, -6, -4), V(8, -10, 10), V(-118, -12, -18), V(-116, -8, 6), V(-6, 0, 4), V(6, 0, -4));
            return b.Build();
        }

        /// <summary>
        /// CURSE: HEX's hold, a 1.2 s loop: peeking over the doll at her cheek, the reaching hand held high and steady, the head
        /// tilting a little from side to side as if deciding (she has ten seconds; she is enjoying it).
        /// </summary>
        private static AnimationClip BuildPhaisterHexReachAim(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-hexreach-aim", paths);
            PoseKey(b, 0, 0, V(-2, -6, -4), V(8, -10, 10), V(-118, -12, -18), V(-116, -8, 6), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .30f, 0, V(-2, -7, -4), V(6, -12, 12), V(-118, -12, -18), V(-118, -7, 7), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .60f, 0, V(-2, -5, -3), V(9, -9, 9), V(-120, -12, -19), V(-115, -9, 5), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .90f, 0, V(-2, -6, -4), V(7, -11, 11), V(-118, -12, -18), V(-117, -8, 6), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, 1.20f, 0, V(-2, -6, -4), V(8, -10, 10), V(-118, -12, -18), V(-116, -8, 6), V(-6, 0, 4), V(6, 0, -4));
            return b.Build();
        }

        /// <summary>
        /// CURSE: HEX's recast, 0.60 s (plan 9.5: *"she yanks the doll to her face and stabs the pin into its button eye,
        /// grinning"*): the doll yanked up in front of her face and the right hand up with the pin (0.10); the stab across into
        /// its eye (0.22, punch), the trunk turning into it; a twist of the pin with her head tipped to the grin (0.36); rest.
        /// </summary>
        private static AnimationClip BuildPhaisterHexStab(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-hexstab", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
            PoseKey(b, .10f, 0, V(-2, 4, 0), V(8, -4, 0), V(-130, -10, -30), V(-150, 10, 10), V(-4, 0, 4), V(4, 0, -4));
            b.PunchAt(.22f);
            b.HoldAt(.22f, .05f);
            PoseKey(b, .22f, -.02f, V(6, -8, 0), V(14, -4, 2), V(-128, -10, -32), V(-110, -20, 34), V(-8, 0, 4), V(8, 0, -4));
            PoseKey(b, .36f, 0, V(5, -8, 2), V(12, -2, 6), V(-126, -10, -30), V(-112, -24, 36), V(-6, 0, 4), V(6, 0, -4));
            PoseKey(b, .60f, 0, V(0, 0, 0), V(0, 0, 0), PhRestLeft, PhRestRight);
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
