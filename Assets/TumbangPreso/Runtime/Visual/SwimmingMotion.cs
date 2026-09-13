using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Shared timing for authored body loops and the first-person strokes.</summary>
    public static class SwimmingMotion
    {
        public const string Folder="SwimmingAnimations";
        public static readonly string[] Names={"swim","swim-holding","tread-water","tread-water-holding"};
        public static string Clip(bool moving,bool held)=>Names[(moving?0:2)+(held?1:0)];
        public static float Duration(bool moving)=>moving?1.55f:1.9f;
        public static bool ForwardStroke(CharacterMotor motor)
        {
            if(motor==null||!motor.IsSwimming||motor.IsTripped)return false;
            var velocity=Vector3.ProjectOnPlane(motor.Velocity,Vector3.up);
            var facing=Vector3.ProjectOnPlane(motor.transform.forward,Vector3.up).normalized;
            // Observed travel relative to facing works for owners and replicated
            // bodies. Sideways/backwards travel keeps an upright sculling pose.
            return velocity.magnitude>.25f&&Vector3.Dot(velocity.normalized,facing)>.45f;
        }
        public static Vector2 BreastReach(float phase)
        {
            float t=Mathf.Repeat(phase/(Mathf.PI*2),1);
            Vector2 glide=new Vector2(.12f,1),sweep=new Vector2(.98f,.32f),pull=new Vector2(.40f,-.16f);
            if(t<.28f)return glide;
            if(t<.53f)return Vector2.Lerp(glide,sweep,Mathf.SmoothStep(0,1,(t-.28f)/.25f));
            if(t<.72f)return Vector2.Lerp(sweep,pull,Mathf.SmoothStep(0,1,(t-.53f)/.19f));
            return Vector2.Lerp(pull,glide,Mathf.SmoothStep(0,1,(t-.72f)/.28f));
        }
        public static Vector3 HandDirection(float side,float phase,bool moving,bool held)
        {
            if(moving){var reach=BreastReach(phase);return new Vector3(side*reach.x,-.10f,reach.y);}
            if(held&&side>0)return new Vector3(.60f,.65f,.45f);
            float cycle=phase+(side>0?Mathf.PI:0);
            return new Vector3(side*(.66f+Mathf.Cos(cycle)*.13f),.30f+Mathf.Sin(cycle)*.08f,.36f+Mathf.Sin(cycle)*.24f);
        }
        public static float VisualLift(float phase,bool moving)=>(moving?.34f:0)+Mathf.Sin(phase)*(moving?.022f:.015f);
        public static Vector3 Rotation(string bone,float phase,bool moving,bool held)
        {
            float effort=moving?1:.38f;
            float left=Mathf.Sin(phase),right=Mathf.Sin(phase+Mathf.PI);
            switch(bone)
            {
                case "torso":return new Vector3(moving?76:12+Mathf.Sin(phase)*2,Mathf.Sin(phase)*2*effort,Mathf.Cos(phase)*2*effort);
                case "head":return new Vector3(moving?-60:-9,0,0);
                case "arm-left":return new Vector3(-68-left*42*effort,0,24+Mathf.Cos(phase)*18*effort);
                case "arm-right":return held?new Vector3(-100,0,-8):new Vector3(-68-right*42*effort,0,-24-Mathf.Cos(phase+Mathf.PI)*18*effort);
                case "leg-left":return Leg(-1,phase,moving);
                case "leg-right":return Leg(1,phase,moving);
                default:return Vector3.zero;
            }
        }
        private static Vector3 Leg(float side,float phase,bool moving)
        {
            if(!moving)return new Vector3(22+Mathf.Sin(phase+(side>0?Mathf.PI:0))*11,side*5,-side*(8+Mathf.Cos(phase)*3));
            float t=Mathf.Repeat(phase/(Mathf.PI*2),1);
            float tuck=t<.44f?0:t<.66f?Mathf.SmoothStep(0,1,(t-.44f)/.22f):1-Mathf.SmoothStep(0,1,(t-.66f)/.24f);
            return new Vector3(76-tuck*40,side*tuck*15,-side*(3+tuck*27));
        }
    }
}
