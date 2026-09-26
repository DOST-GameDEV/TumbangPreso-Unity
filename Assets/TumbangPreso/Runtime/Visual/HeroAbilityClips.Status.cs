using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // ⚠️ FEARED (ABILITY-2, owner 2026-09-26: *"Flee from kuro and drop slipper"*, *"make an animation for
    // feared too dont js make them walk back ahha"*). A shared clip on every rig, baked with the rooted
    // set by `Editor/RootedAnimationAuthor` (it loops), played by `CharacterAnimator.StepRootedPose`
    // while the body is Feared. The body runs AWAY on its own (`CharacterMotor.FleeWish`); this is what it
    // looks like doing it: hunched low, both arms thrown up over the head, knees pumping, and the head
    // snapping back over one shoulder then the other to look at what is chasing it. A run of panic, not
    // a walk backwards.
    // =============================================================================================
    public static partial class HeroAbilityClips
    {
        private static AnimationClip BuildFearedFlee(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder(RootedMotion.Feared, paths);
            // Stride one: left knee up, head snapped back over the right shoulder.
            PoseKey(b, 0f, .02f, V(18, 0, 0), V(-8, 58, 0), V(-156, 0, 28), V(-150, 0, -26), V(-40, 0, 7), V(30, 0, -7));
            PoseKey(b, .12f, .07f, V(21, 6, 3), V(-5, 44, 0), V(-148, 0, 32), V(-158, 0, -22), V(-6, 0, 7), V(-4, 0, -7));
            // Stride two: right knee up, head snapped the other way.
            PoseKey(b, .25f, .02f, V(18, 0, 0), V(-8, -56, 0), V(-150, 0, 26), V(-157, 0, -28), V(30, 0, 7), V(-40, 0, -7));
            PoseKey(b, .37f, .07f, V(21, -6, -3), V(-5, -42, 0), V(-158, 0, 22), V(-148, 0, -32), V(-4, 0, 7), V(-6, 0, -7));
            PoseKey(b, .5f, .02f, V(18, 0, 0), V(-8, 58, 0), V(-156, 0, 28), V(-150, 0, -26), V(-40, 0, 7), V(30, 0, -7));
            return b.Build();
        }
    }
}
