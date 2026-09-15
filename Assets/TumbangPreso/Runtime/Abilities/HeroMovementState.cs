using System;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    // Remaining state for Sean Rush and Zack Sprint. This is not a new cast.
    public struct HeroMovementState
    {
        public const int MaxWakePoints = 4;
        public float Remaining, UntilNextEmission;
        public Vector3[] Wake;
        public uint KnownWake;

        public bool Valid(float duration, float interval, float age)
        {
            if(!Finite(Remaining) || !Finite(UntilNextEmission) || !Finite(age)
                || Remaining<0 || Remaining>duration+.0001f || age<0
                || UntilNextEmission<0 || UntilNextEmission>interval+.0001f
                || (Wake?.Length??0)>MaxWakePoints) return false;
            if((KnownWake>>(Wake?.Length??0))!=0) return false;
            if(Remaining==0 && (UntilNextEmission!=0 || (Wake?.Length??0)!=0)) return false;
            if(Wake!=null) foreach(var point in Wake)
                if(!Finite(point.x) || !Finite(point.y) || !Finite(point.z)) return false;
            return true;
        }

        // Missed drops already belong to the host's world snapshot. Never emit
        // them again or manufacture positions along a path this client did not see.
        public float NextEmission(float age, float interval, out int missed)
        {
            float left=UntilNextEmission-age;
            missed=left<=0?1+Mathf.FloorToInt(-left/interval):0;
            return left+missed*interval;
        }

        private static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);
        public static HeroMovementState Empty=>new HeroMovementState { Wake=Array.Empty<Vector3>() };
    }
}
