using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public static partial class HeroAbilityClips
    {
        private static AnimationClip BuildRafiCut(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-rafi-cut", paths);
            PoseKey(b, 0, 0, V(0,0,0), V(0,0,0), V(0,0,15), V(0,0,-15));
            PoseKey(b, .09f, -.02f, V(3,-16,-3), V(0,12,0), V(-48,-30,38), V(-12,8,-22), V(-7,0,3), V(5,0,-3));
            b.PunchAt(.18f);
            PoseKey(b, .18f, -.025f, V(4,22,4), V(-3,-16,0), V(-66,38,60), V(-10,-8,-20), V(-8,0,4), V(6,0,-4));
            PoseKey(b, .31f, -.01f, V(2,12,2), V(0,-8,0), V(-40,45,38), V(-8,0,-18));
            PoseKey(b, .58f, 0, V(0,0,0), V(0,0,0), V(0,0,15), V(0,0,-15));
            return b.Build();
        }
        private static AnimationClip BuildRafiFeint(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-rafi-feint", paths);
            PoseKey(b, 0, 0, V(0,0,0), V(0,0,0), V(0,0,15), V(0,0,-15));
            PoseKey(b, .14f, -.025f, V(2,28,-6), V(0,-26,0), V(-28,10,25), V(-35,-12,-24), V(0,22,3), V(-6,-10,-4));
            PoseKey(b, .30f, -.01f, V(-2,-20,3), V(-4,14,0), V(-16,-18,32), V(-20,16,-18), V(-6,-14,4), V(5,10,-3));
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
            PoseKey(b, .55f, -.035f, V(8,32,6), V(3,-18,0), V(-68,42,75), V(-28,18,-28), V(-12,0,6), V(10,0,-6));
            PoseKey(b, .76f, -.015f, V(4,20,3), V(0,-10,0), V(-38,48,48), V(-16,10,-22));
            PoseKey(b, 1.05f, 0, V(0,0,0), V(0,0,0), V(0,0,15), V(0,0,-15));
            return b.Build();
        }
    }
}
