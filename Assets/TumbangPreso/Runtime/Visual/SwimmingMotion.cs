using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Shared timing for authored body loops and the first-person strokes.</summary>
    public static class SwimmingMotion
    {
        public const string Folder="SwimmingAnimations";
        public static readonly string[] Names={"swim","swim-holding","tread-water","tread-water-holding"};
        public static string Clip(bool moving,bool held)=>Names[(moving?0:2)+(held?1:0)];
        public static float Duration(bool moving)=>moving?1.15f:1.65f;
        public static Vector3 Rotation(string bone,float phase,bool moving,bool held)
        {
            float effort=moving?1:.38f;
            float left=Mathf.Sin(phase),right=Mathf.Sin(phase+Mathf.PI);
            switch(bone)
            {
                case "torso":return new Vector3(moving?48:8,Mathf.Sin(phase)*3*effort,Mathf.Cos(phase)*2*effort);
                case "head":return new Vector3(moving?-32:-4,0,0);
                case "arm-left":return new Vector3(-68-left*42*effort,0,24+Mathf.Cos(phase)*18*effort);
                case "arm-right":return held?new Vector3(-100,0,-8):new Vector3(-68-right*42*effort,0,-24-Mathf.Cos(phase+Mathf.PI)*18*effort);
                case "leg-left":return new Vector3((moving?52:20)+Mathf.Sin(phase*2)*14*effort,0,4);
                case "leg-right":return new Vector3((moving?52:20)-Mathf.Sin(phase*2)*14*effort,0,-4);
                default:return Vector3.zero;
            }
        }
    }
}
