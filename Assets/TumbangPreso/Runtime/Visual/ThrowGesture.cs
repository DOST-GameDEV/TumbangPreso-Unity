using UnityEngine;

namespace TumbangPreso.Visual
{
    // Shared timing and signed preparation for the retained seven-bone body and
    // rigid first-person forearms. Motion never changes the accepted trajectory.
    public static class ThrowGesture
    {
        public const float ReleaseSeconds=.46f;
        public const float ContactSeconds=.085f;
        public static string Action(float spin) => spin < -.05f ? "throw-pektus-left"
            : spin > .05f ? "throw-pektus-right" : "throw";
        public static bool IsThrow(string action) => action=="throw" || action=="throw-pektus-left" || action=="throw-pektus-right";
        public static float Spin(string action) => action=="throw-pektus-left" ? -1 : action=="throw-pektus-right" ? 1 : 0;

        public readonly struct Pose
        {
            public readonly Quaternion Torso,Head,Right,Left;
            public Pose(Vector3 torso,Vector3 head,Vector3 right,Vector3 left)
            {Torso=Quaternion.Euler(torso);Head=Quaternion.Euler(head);Right=Quaternion.Euler(right);Left=Quaternion.Euler(left);}
            private Pose(Quaternion torso,Quaternion head,Quaternion right,Quaternion left)
            {Torso=torso;Head=head;Right=right;Left=left;}
            public static Pose Lerp(Pose a,Pose b,float t)=>new Pose(Quaternion.Slerp(a.Torso,b.Torso,t),
                Quaternion.Slerp(a.Head,b.Head,t),Quaternion.Slerp(a.Right,b.Right,t),Quaternion.Slerp(a.Left,b.Left,t));
        }
        public static Pose Rest=>new Pose(Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero);
        public static Pose Prepare(float charge,float spin)
        {
            float p=Mathf.SmoothStep(0,1,Mathf.Clamp01(charge));spin=Mathf.Clamp(spin,-1,1);
            return new Pose(new Vector3(-16,14+10*spin,2*spin)*p,new Vector3(8,-8-5*spin,0)*p,
                new Vector3(-CameraSystem.ViewmodelArms.WindupRad*Mathf.Rad2Deg,-32*spin,-18*spin)*p,
                new Vector3(-22,0,18)*p);
        }
        public static Pose FollowThrough(float spin)=>new Pose(new Vector3(15,-12-14*spin,-3*spin),
            new Vector3(-8,7,0),new Vector3(-24,28*spin,24*spin),new Vector3(12,0,8));
        public static float Recovery(float seconds)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(ContactSeconds,ReleaseSeconds,seconds));
        public static Pose Release(Pose from,float seconds,float spin)=>seconds<ContactSeconds
            ? Pose.Lerp(from,FollowThrough(spin),Mathf.Clamp01(seconds/ContactSeconds))
            : Pose.Lerp(FollowThrough(spin),Rest,Recovery(seconds));
        public static Quaternion HandPreparation(float charge,float spin)=>Quaternion.Euler(
            CameraSystem.ViewmodelArms.WindupCarry*CameraSystem.ViewmodelArms.WindupRad*.5f*Mathf.Clamp01(charge)*Mathf.Rad2Deg,
            -16*spin*Mathf.Clamp01(charge),-14*spin*Mathf.Clamp01(charge));
        public static Quaternion HandFollowThrough(float spin)=>Quaternion.Euler(42,24*spin,32*spin);
    }
}
