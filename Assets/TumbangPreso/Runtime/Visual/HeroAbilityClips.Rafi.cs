using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public static partial class HeroAbilityClips
    {
#if UNITY_EDITOR
        public static AnimationClip[] BuildRafiAuthored(Transform root)
        {
            var paths=ResolvePaths(root);
            if(paths==null)throw new System.InvalidOperationException("Rafi rig is missing a required bone.");
            var clips=new[]{BuildRafiCut(paths),BuildRafiFeint(paths),BuildRafiBreakwater(paths),BuildRafiWall(paths)};
            foreach(var clip in clips)GroundIntroduction(clip,root,paths["root"],anchorToRest:true);
            return clips;
        }
        public static AnimationClip BuildRafiWallAuthored(Transform root)
        {
            var paths=ResolvePaths(root);
            if(paths==null)throw new System.InvalidOperationException("Rafi wall rig is missing a required bone.");
            var clip=BuildRafiWall(paths);
            GroundIntroduction(clip,root,paths["root"],anchorToRest:true);
            return clip;
        }
#endif
        private static AnimationClip BuildRafiWall(Dictionary<string,string> paths)
        {
            var b=new ClipBuilder("hero-rafi-wall",paths);
            PoseKey(b,0,0,V(0,0,0),V(0,0,0),V(0,0,15),V(0,0,-15));
            // Knees gather the low water; both palms raise it instead of cutting sideways.
            PoseKey(b,.08f,-.045f,V(14,-4,0),V(-6,2,0),V(-22,-6,28),V(-22,6,-28),V(-10,0,4),V(8,0,-4));
            PoseKey(b,.18f,-.03f,V(6,0,0),V(-5,0,0),V(-55,-12,60),V(-55,12,-60),V(-6,0,3),V(5,0,-3));
            PoseKey(b,.25f,-.01f,V(-5,0,0),V(-5,0,0),V(-82,-12,74),V(-82,12,-74),V(-3,0,2),V(3,0,-2));
            PoseKey(b,.36f,-.01f,V(-3,0,0),V(-3,0,0),V(-76,-10,70),V(-76,10,-70),V(-3,0,2),V(3,0,-2));
            PoseKey(b,.52f,-.015f,V(2,0,0),V(0,0,0),V(-48,-14,34),V(-48,14,-34));
            PoseKey(b,.78f,0,V(0,0,0),V(0,0,0),V(0,0,15),V(0,0,-15));
            return b.Build();
        }
        private static AnimationClip BuildRafiCut(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-rafi-cut", paths);
            PoseKey(b, 0, 0, V(0,0,0), V(0,0,0), V(0,0,15), V(0,0,-15));
            PoseKey(b, .09f, -.02f, V(3,-16,-3), V(0,12,0), V(-48,-30,38), V(-12,8,-22), V(-7,0,3), V(5,0,-3));
            b.PunchAt(.18f);
            // ⚠️ 2026-09-24, bigger and HELD (🧑 *"the animation of all skill casting"*): filmed, the sweep was a
            // few degrees and gone. The hand now sweeps wide across the flight line and holds there.
            b.HoldAt(.18f, .18f);
            PoseKey(b, .18f, -.04f, V(8,32,6), V(-3,-22,0), V(-84,52,86), V(-14,-10,-28), V(-11,0,6), V(8,0,-6));
            PoseKey(b, .31f, -.02f, V(4,18,3), V(0,-12,0), V(-54,58,54), V(-10,0,-24));
            PoseKey(b, .58f, 0, V(0,0,0), V(0,0,0), V(0,0,15), V(0,0,-15));
            return b.Build();
        }
        private static AnimationClip BuildRafiFeint(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-rafi-feint", paths);
            PoseKey(b, 0, 0, V(0,0,0), V(0,0,0), V(0,0,15), V(0,0,-15));
            // ⚠️ 2026-09-24, a real feint: a big sell one way, held, then the cut back (🧑 *"all skill casting"*).
            b.HoldAt(.14f, .12f);
            PoseKey(b, .14f, -.045f, V(4,40,-9), V(0,-36,0), V(-40,14,36), V(-50,-16,-34), V(0,30,4), V(-8,-14,-6));
            PoseKey(b, .30f, -.02f, V(-3,-28,4), V(-6,20,0), V(-22,-24,44), V(-28,22,-26), V(-8,-20,6), V(7,14,-4));
            PoseKey(b, .47f, 0, V(0,-8,1), V(0,6,0), V(-8,0,20), V(-10,0,-16));
            PoseKey(b, .66f, 0, V(0,0,0), V(0,0,0), V(0,0,15), V(0,0,-15));
            return b.Build();
        }
        private static AnimationClip BuildRafiBreakwater(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-rafi-breakwater", paths);
            // The shared introduction ends at this same low hip gather.
            PoseKey(b, 0, -.06f, V(8,-25,-6), V(-4,20,0), V(-40,-24,32), V(-26,-24,-25), V(-10,0,5), V(8,0,-5));
            PoseKey(b, .26f, -.035f, V(-4,-10,-2), V(-8,10,0), V(-82,-12,40), V(-55,-8,-30), V(-8,0,4), V(5,0,-4));
            b.PunchAt(.55f);
            // ⚠️ 2026-09-24, the wave is sent with the whole body and held (🧑 *"all skill casting"*).
            b.HoldAt(.55f, .2f);
            PoseKey(b, .55f, -.05f, V(14,42,8), V(3,-24,0), V(-86,52,98), V(-38,24,-38), V(-15,0,8), V(13,0,-8));
            PoseKey(b, .76f, -.015f, V(4,20,3), V(0,-10,0), V(-38,48,48), V(-16,10,-22));
            PoseKey(b, 1.05f, 0, V(0,0,0), V(0,0,0), V(0,0,15), V(0,0,-15));
            return b.Build();
        }
    }
}
