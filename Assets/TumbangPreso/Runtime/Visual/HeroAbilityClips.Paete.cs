using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // PAETE'S BODY CASTS (HERO-9, 2026-09-26). Owner: *"i want u to really lock in and give paete his
    // own animations taht make sense wiht his shit"*, *"i want each of his skill to have their own
    // animation"*. Five clips for four skills, because BAKYA BLOOM is two different acts (the
    // planting and the command to fire), and each one ACTS OUT what its skill does to the world:
    // the vines drag him, the seed is lobbed like a gardener's, the command is an order not a throw,
    // the thorns are stamped out and then HAULED BACK like a rope, the sentry is thrown and then
    // closed on like an embrace (yakap).
    //
    // Keys on the rig's bones only, never on the mesh, so a rebuilt glb re-bakes from this table
    // through `Editor/PaeteMotionAuthor` with no change here (his verdict on model v17 is owed).
    //
    // ⚠️⚠️ HIS DIRECTION IS THE BEND AND THE SNAP-BACK. `tools/author_hero_action.py` gives each hero
    // one motion language no other shares (Sean forward, Zack sideways, Dante down, Cheska still,
    // Rafi the sell and the cut back, Amihan the spiral). Paete is a tree: every cast LOADS FROM THE
    // GROUND UP (the root sinks first, the trunk bends after it, the arms come last, a trunk taking
    // the wind) and RELEASES LIKE A BRANCH LET GO, one punch key that snaps past straight and
    // settles back. His feet stay planted wherever the kit allows it: the vine reel is the one cast
    // where they leave the ground, because it is the one ability that moves him.
    //
    // PoseKey (see `HeroAbilityClips.Introductions.cs`): PoseKey(b, t, rootY, torso, head, LEFT arm,
    // RIGHT arm, left leg, right leg). Arms V(pitch, twist, spread): negative pitch swings FORWARD
    // (-90 straight ahead, -180 overhead), rest is V(0, 0, 15) left and V(0, 0, -15) right; a
    // NEGATIVE spread on the left (positive on the right) brings the arm across the chest. Torso
    // V(lean forward, twist to his right, lean left). Legs V(pitch, _, spread), negative forward.
    //
    // ⚠️ THE TWO SHARED CLIPS AT THE BOTTOM ARE NOT HIS. `rooted-struggle` and `plant-heave` are
    // what ANY body does against his roots and his seedling, so they are baked onto every rig by
    // `Editor/RootedAnimationAuthor` and loaded from `RootedMotion.Folder`, the recovery motion's
    // pattern. They live here because they are keyed in the same shape and answer the same kit.
    // =============================================================================================
    public static partial class HeroAbilityClips
    {
#if UNITY_EDITOR
        public static AnimationClip[] BuildPaeteAuthored(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("Paete rig is missing a required bone.");
            var clips = new[]
            {
                BuildPaeteVine(paths), BuildPaeteSprout(paths), BuildPaeteCommand(paths), BuildPaeteThorns(paths),
                BuildPaeteSentry(paths),
            };
            foreach (var clip in clips) GroundIntroduction(clip, root, paths["root"], anchorToRest: true);
            return clips;
        }

        /// <summary>The two clips every rig carries for Paete's kit (struggle, heave), grounded on that rig.</summary>
        public static AnimationClip[] BuildRootedShared(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) return null;
            var clips = new[] { BuildRootedStruggle(paths), BuildPlantHeave(paths), BuildRootBreakout(paths), BuildFearedFlee(paths) };
            foreach (var clip in clips) GroundIntroduction(clip, root, paths["root"], anchorToRest: true);
            return clips;
        }
#endif

        private static readonly Vector3 PaeteRestLeft = new Vector3(0, 0, 15), PaeteRestRight = new Vector3(0, 0, -15);

        /// <summary>
        /// LIANA LEAP, 1.0 s, over the 0.12 s tell, the 0.14 s reach and the reel (about 0.28 s at
        /// full range). A tree being DRAGGED, not a hero flying:
        ///  * tell (0 to 0.12): the root sinks, the trunk coils back, both fists pulled in low at the
        ///    hips, the palms glowing (`PaetePalmGlow`);
        ///  * release (0.14, punch): both arms thrown straight along the aim TOGETHER, locked, the
        ///    braids unravelling off them; the trunk only follows a beat later (arms first here,
        ///    because the vines are what pull);
        ///  * reel (0.3 to 0.56): hauled nearly flat, the trunk pitched 44 degrees, legs dragging
        ///    behind like roots torn out of the ground;
        ///  * arrival (0.70, punch): the legs swing through and plant, a heavy squash, and the arms
        ///    FOLD BACK IN across the chest as the vines reel into the forearms;
        ///  * settle: he straightens like a trunk after the wind.
        /// </summary>
        private static AnimationClip BuildPaeteVine(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-paete-vine", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            PoseKey(b, .11f, -.07f, V(-10, 0, 0), V(-6, 0, 0), V(28, 8, 30), V(28, -8, -30), V(-10, 0, 6), V(10, 0, -6));
            b.PunchAt(.14f);
            PoseKey(b, .14f, -.03f, V(6, 0, 0), V(-12, 0, 0), V(-108, 0, 5), V(-108, 0, -5), V(-6, 0, 6), V(12, 0, -6));
            PoseKey(b, .30f, .07f, V(34, 0, 0), V(-24, 0, 0), V(-112, 0, 6), V(-112, 0, -6), V(34, 0, 6), V(44, 0, -6));
            PoseKey(b, .56f, .09f, V(44, 0, 2), V(-30, 0, 0), V(-116, 2, 7), V(-116, -2, -7), V(46, 0, 8), V(54, 0, -8));
            b.PunchAt(.70f);
            b.HoldAt(.70f, .06f);
            PoseKey(b, .70f, -.09f, V(18, 0, 0), V(6, 0, 0), V(-60, 20, -10), V(-60, -20, 10), V(-22, 0, 8), V(14, 0, -8));
            PoseKey(b, .84f, -.03f, V(-4, 0, 0), V(-2, 0, 0), V(-20, 8, 6), V(-20, -8, -6), V(-6, 0, 6), V(4, 0, -6));
            PoseKey(b, 1.0f, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            return b.Build();
        }

        /// <summary>
        /// BAKYA BLOOM, the planting, 0.7 s: the gardener's lob.
        ///  * tell (0 to 0.16): both hands cupped together at the chest around the glowing seed, head
        ///    bowed to it, the weight rocking back onto the right foot;
        ///  * wind (0.24): the hands part, the right swings back low past the hip;
        ///  * release (0.34, punch): the underhand lob, the right arm sweeping up and open, the trunk
        ///    rocking forward over the planted left foot, the left hand opening toward the spot;
        ///  * follow-through, then the settle.
        /// </summary>
        private static AnimationClip BuildPaeteSprout(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-paete-sprout", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            PoseKey(b, .16f, -.03f, V(12, 4, 0), V(26, 0, 0), V(-62, 0, -24), V(-62, 0, 24), V(-6, 0, 5), V(8, 0, -5));
            PoseKey(b, .24f, -.05f, V(-4, 18, 0), V(6, 8, 0), V(-30, 0, 24), V(46, 6, -10), V(-18, 0, 6), V(14, 0, -4));
            b.PunchAt(.34f);
            b.HoldAt(.34f, .07f);
            PoseKey(b, .34f, -.02f, V(12, -10, 0), V(-10, -4, 0), V(-40, 0, 26), V(-80, -8, -12), V(-22, 0, 6), V(14, 0, -5));
            PoseKey(b, .50f, 0, V(6, -6, 0), V(-6, -2, 0), V(-20, 0, 22), V(-96, -6, -14), V(-12, 0, 5), V(8, 0, -4));
            PoseKey(b, .70f, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            return b.Build();
        }

        /// <summary>
        /// BAKYA BLOOM, the command (the second press), 0.5 s. The seedling throws, so HE only
        /// orders it: a sharp point of the right arm at the target, the left fist pulled back to the
        /// hip, the trunk turning into the point and the head nodding it home. Short and dry, so it
        /// never reads as a second planting.
        /// </summary>
        private static AnimationClip BuildPaeteCommand(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-paete-command", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            PoseKey(b, .07f, 0, V(-4, 10, 0), V(-4, 4, 0), V(0, 0, 18), V(-40, 10, -30), V(0, 0, 5), V(0, 0, -5));
            b.PunchAt(.13f);
            b.HoldAt(.13f, .12f);
            PoseKey(b, .13f, -.02f, V(8, -14, 0), V(10, -8, 0), V(24, 0, 20), V(-94, -4, -4), V(-8, 0, 5), V(6, 0, -5));
            PoseKey(b, .32f, -.01f, V(6, -10, 0), V(2, -6, 0), V(20, 0, 20), V(-88, -4, -6), V(-6, 0, 5), V(4, 0, -5));
            PoseKey(b, .50f, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            return b.Build();
        }

        /// <summary>
        /// THORN HARVEST, 1.05 s: the stamp, then the take. The thorns catch (hold 0.25 s) and yank (0.5 s),
        /// and his body does the yank WITH them:
        ///  * tell (0.14): the right knee high, both arms raised wide, the trunk rearing back;
        ///  * stamp (0.22, punch): the foot drives down, the root drops, both palms slammed at the
        ///    road as the thorns crack out;
        ///  * the catch (0.46): still low, both hands clawed forward over the ground, gripping;
        ///  * the yank (0.56, punch): both arms RIPPED BACK past the hips, the trunk hauling upright
        ///    and back like a man on a rope, held while the slippers tumble in;
        ///  * settle.
        /// </summary>
        private static AnimationClip BuildPaeteThorns(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-paete-thorns", paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            PoseKey(b, .14f, .03f, V(-8, 0, 0), V(-6, 0, 0), V(-34, 0, 54), V(-34, 0, -54), V(-2, 0, 6), V(-62, 0, -6));
            b.PunchAt(.22f);
            PoseKey(b, .22f, -.10f, V(24, 0, 0), V(14, 0, 0), V(24, 0, 38), V(24, 0, -38), V(-12, 0, 10), V(6, 0, -12));
            PoseKey(b, .46f, -.08f, V(28, 0, 0), V(8, 0, 0), V(-52, 0, 20), V(-52, 0, -20), V(-12, 0, 10), V(6, 0, -12));
            b.PunchAt(.56f);
            b.HoldAt(.56f, .10f);
            PoseKey(b, .56f, -.05f, V(-26, 0, 0), V(-16, 0, 0), V(58, 34, 26), V(58, -34, -26), V(-26, 0, 10), V(18, 0, -12));
            PoseKey(b, .82f, -.03f, V(-18, 0, 0), V(-10, 0, 0), V(46, 28, 24), V(46, -28, -24), V(-18, 0, 8), V(12, 0, -9));
            PoseKey(b, 1.05f, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            return b.Build();
        }

        /// <summary>
        /// MAKILING'S EMBRACE, 1.3 s. ⚠️ IT STARTS WHERE THE INTRODUCTION ENDS, ON THE RAISED POSE
        /// (`tools/author_ultimate_intros.py` `paete`: since 2026-09-26 he calls the tree up through the ground,
        /// kneeling, then rising with both arms high; he no longer throws). Then:
        ///  * raised (0 to 0.34): arms high in a V, head back, lifting a little further as the tree bursts;
        ///  * the embrace (0.72, punch): the branches take them and he CLOSES HIS ARMS, crossing them over the
        ///    chest, the trunk curling forward, hauling everyone in; held while they are dragged;
        ///  * release, straightening.
        /// </summary>
        private static AnimationClip BuildPaeteSentry(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-paete-sentry", paths);
            PoseKey(b, 0, .02f, V(-14, 0, 0), V(-24, 0, 0), V(-162, 0, 42), V(-162, 0, -42), V(-10, 0, 10), V(6, 0, -10));
            PoseKey(b, .16f, .03f, V(-17, 0, 0), V(-27, 0, 0), V(-168, 0, 46), V(-168, 0, -46), V(-10, 0, 10), V(6, 0, -10));
            PoseKey(b, .34f, .02f, V(-16, 0, 0), V(-22, 0, 0), V(-150, 0, 56), V(-150, 0, -56), V(-12, 0, 9), V(8, 0, -9));
            b.PunchAt(.72f);
            b.HoldAt(.72f, .20f);
            PoseKey(b, .72f, -.06f, V(22, 0, 0), V(10, 0, 0), V(-84, 20, -22), V(-84, -20, 22), V(-16, 0, 9), V(12, 0, -9));
            PoseKey(b, 1.02f, -.05f, V(18, 0, 0), V(8, 0, 0), V(-80, 18, -18), V(-80, -18, 18), V(-14, 0, 9), V(10, 0, -9));
            PoseKey(b, 1.30f, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            return b.Build();
        }

        // ------------------------------------------------------------------ SHARED BY EVERY RIG

        /// <summary>
        /// ROOTED, fighting the roots (research.md § 2, Dead by Daylight's wiggle): 1.2 s, looped
        /// while the player holds Interact. The feet do not move (they are held); everything above
        /// them does: a heave to the left, a heave to the right, a kick at the roots, and back to the
        /// first key, so the loop has no seam. The arms tug at the air as the torso throws its weight.
        /// </summary>
        private static AnimationClip BuildRootedStruggle(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(RootedMotion.Struggle, paths);
            var legL = V(0, 0, 7); var legR = V(0, 0, -7);
            PoseKey(b, 0, -.02f, V(8, 0, 0), V(4, 0, 0), V(-24, 0, 30), V(-24, 0, -30), legL, legR);
            b.PunchAt(.30f);
            PoseKey(b, .30f, -.03f, V(10, -14, 16), V(6, -8, 6), V(-58, 0, 44), V(-8, 0, -26), legL, legR);
            b.PunchAt(.62f);
            PoseKey(b, .62f, -.03f, V(10, 14, -16), V(6, 8, -6), V(-8, 0, 26), V(-58, 0, -44), legL, legR);
            b.PunchAt(.86f);
            PoseKey(b, .86f, .01f, V(-8, 0, 0), V(-10, 0, 0), V(14, 0, 34), V(14, 0, -34), legL, V(-26, 0, -7));
            PoseKey(b, 1.2f, -.02f, V(8, 0, 0), V(4, 0, 0), V(-24, 0, 30), V(-24, 0, -30), legL, legR);
            return b.Build();
        }

        /// <summary>
        /// ⚠️ BREAKING OUT OF THE SENTRY'S HOLD (owner, 2026-09-26: *"make sure to create the animation for
        /// getting out of his ult too"*; direction.md section 5.6): 0.9 s, played once when the roots let
        /// go (7 s of Interact, or the tree going back to sleep; a tag keeps its own reaction).
        ///  * rip (0 to 0.12): the root drops a hair (the last pull), then the body HEAVES UP, chest thrown
        ///    open, arms flung up and out, as the shin branches crack apart (`PaeteRootCoil.Break`);
        ///  * step (0.12 to 0.40): one knee yanked high out of the roots, then the other, a stumble forward;
        ///  * shake (0.40 to 0.76): a shake-off, one side then the other, arms flicking down;
        ///  * settle (to 0.9): back to stance.
        /// </summary>
        private static AnimationClip BuildRootBreakout(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(RootedMotion.Breakout, paths);
            PoseKey(b, 0, -.04f, V(10, 0, 0), V(6, 0, 0), V(-20, 0, 26), V(-20, 0, -26), V(0, 0, 7), V(0, 0, -7));
            b.PunchAt(.12f);
            PoseKey(b, .12f, .06f, V(-16, 0, 0), V(-14, 0, 0), V(-140, 0, 48), V(-140, 0, -48), V(0, 0, 9), V(0, 0, -9));
            PoseKey(b, .24f, .08f, V(-6, 6, 0), V(-6, 4, 0), V(-110, 0, 40), V(-120, 0, -38), V(-58, 0, 7), V(8, 0, -7));
            b.PunchAt(.38f);
            PoseKey(b, .38f, .05f, V(4, -6, 0), V(2, -4, 0), V(-60, 0, 30), V(-70, 0, -30), V(-10, 0, 7), V(-52, 0, -7));
            PoseKey(b, .52f, 0, V(6, 0, 10), V(4, 0, -8), V(-10, 0, 34), V(-10, 0, -20), V(0, 0, 7), V(0, 0, -7));
            PoseKey(b, .64f, 0, V(6, 0, -10), V(4, 0, 8), V(-10, 0, 20), V(-10, 0, -34), V(0, 0, 7), V(0, 0, -7));
            PoseKey(b, .76f, 0, V(2, 0, 0), V(0, 0, 0), V(-6, 0, 22), V(-6, 0, -22), V(0, 0, 6), V(0, 0, -6));
            PoseKey(b, .9f, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            return b.Build();
        }

        /// <summary>
        /// Pulling a seedling out (owner, 2026-09-25: *"i want the animation for pull out to be
        /// good"*): 1.4 s. The first 1.15 s (`RootedMotion.HeaveScrubSeconds`) are SCRUBBED by the hold,
        /// so the body is exactly as far into the pull as the ring says: grip (bent low, both hands
        /// at the stem), lean back, the heave (straining, head thrown back). The last 0.25 s play at
        /// normal speed once it comes free: the stumble back, arms flung up with it.
        /// </summary>
        private static AnimationClip BuildPlantHeave(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(RootedMotion.Heave, paths);
            PoseKey(b, 0, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            PoseKey(b, .25f, -.12f, V(40, 0, 0), V(18, 0, 0), V(-44, 0, 8), V(-44, 0, -8), V(-20, 0, 10), V(-16, 0, -10));
            PoseKey(b, .65f, -.09f, V(4, 0, 0), V(-6, 0, 0), V(-40, 0, 7), V(-40, 0, -7), V(-26, 0, 10), V(10, 0, -8));
            PoseKey(b, .95f, -.05f, V(-18, 0, 2), V(-16, 0, 0), V(-52, 2, 8), V(-52, -2, -8), V(-28, 0, 10), V(16, 0, -8));
            PoseKey(b, 1.15f, -.04f, V(-22, 0, -2), V(-18, 0, 0), V(-56, -2, 9), V(-56, 2, -9), V(-28, 0, 10), V(18, 0, -8));
            b.PunchAt(1.25f);
            PoseKey(b, 1.25f, .02f, V(-14, 0, 0), V(-10, 0, 0), V(-130, 0, 36), V(-130, 0, -36), V(-10, 0, 8), V(26, 0, -10));
            PoseKey(b, 1.4f, 0, V(0, 0, 0), V(0, 0, 0), PaeteRestLeft, PaeteRestRight);
            return b.Build();
        }
    }
}
