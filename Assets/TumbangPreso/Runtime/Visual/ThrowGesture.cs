using UnityEngine;

namespace TumbangPreso.Visual
{
    // Shared timing and signed preparation for the retained seven-bone body and
    // rigid first-person forearms. Motion never changes the accepted trajectory.
    public static class ThrowGesture
    {
        public const float ReleaseSeconds=.54f;
        public const float ContactSeconds=.055f;
        public const float FollowSeconds=.18f;
        public static string Action(float spin) => spin < -.05f ? "throw-pektus-left"
            : spin > .05f ? "throw-pektus-right" : "throw";
        public static bool IsThrow(string action) => action=="throw" || action=="throw-pektus-left" || action=="throw-pektus-right";
        public static float Spin(string action) => action=="throw-pektus-left" ? -1 : action=="throw-pektus-right" ? 1 : 0;

        public readonly struct Pose
        {
            public readonly Quaternion Torso,Head,Right,Left;
            public Pose(Vector3 torso,Vector3 head,Vector3 right,Vector3 left)
            {Torso=Quaternion.Euler(torso);Head=Quaternion.Euler(head);Right=Quaternion.Euler(right);Left=Quaternion.Euler(left);}
            public Pose(Quaternion torso,Quaternion head,Quaternion right,Quaternion left)
            {Torso=torso;Head=head;Right=right;Left=left;}
            public static Pose Lerp(Pose a,Pose b,float t)=>new Pose(Quaternion.Slerp(a.Torso,b.Torso,t),
                Quaternion.Slerp(a.Head,b.Head,t),Quaternion.Slerp(a.Right,b.Right,t),Quaternion.Slerp(a.Left,b.Left,t));
            public static Pose Apply(Pose basis,Pose offset)=>new Pose(offset.Torso*basis.Torso,
                offset.Head*basis.Head,offset.Right*basis.Right,offset.Left*basis.Left);
        }
        public static Pose Rest=>new Pose(Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero);
        public static Pose Prepare(float charge,float spin)
        {
            float p=(1-Mathf.Exp(-4.5f*Mathf.Clamp01(charge)))/(1-Mathf.Exp(-4.5f));
            spin=Mathf.Clamp(spin,-1,1);
            return new Pose(new Vector3(-13,24+10*spin,2*spin)*p,new Vector3(7,-12-4*spin,0)*p,
                new Vector3(-16,38-22*spin,-44-10*spin)*p,
                new Vector3(-24,-12,20)*p);
        }
        public static Pose Contact(float spin)=>new Pose(new Vector3(11,-10-8*spin,-2*spin),
            new Vector3(-5,7,0),new Vector3(-20,-18+12*spin,5+8*spin),new Vector3(2,0,9));
        public static Pose FollowThrough(float spin)=>new Pose(new Vector3(17,-25-14*spin,-3),
            new Vector3(-7,13,1),new Vector3(-8,-35+16*spin,12+16*spin),new Vector3(12,5,6));
        public static float Recovery(float seconds)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(FollowSeconds,ReleaseSeconds,seconds));
        public static Pose Release(Pose from,float seconds,float spin)=>Release(Rest,from,Rest,seconds,spin);
        public static Pose Release(Pose carryBasis,Pose from,Pose locomotion,float seconds,float spin)
        {
            var contact=Pose.Apply(carryBasis,Contact(spin));
            var follow=Pose.Apply(carryBasis,FollowThrough(spin));
            if(seconds<ContactSeconds)return Pose.Lerp(from,contact,Mathf.Clamp01(seconds/ContactSeconds));
            if(seconds<FollowSeconds)return Pose.Lerp(contact,follow,
                Mathf.SmoothStep(0,1,Mathf.InverseLerp(ContactSeconds,FollowSeconds,seconds)));
            return Pose.Lerp(follow,locomotion,Recovery(seconds));
        }
        public static Quaternion HandPreparation(float charge,float spin)=>Quaternion.Euler(
            CameraSystem.ViewmodelArms.WindupCarry*CameraSystem.ViewmodelArms.WindupRad*.5f*Mathf.Clamp01(charge)*Mathf.Rad2Deg,
            -16*spin*Mathf.Clamp01(charge),-14*spin*Mathf.Clamp01(charge));
        public static Quaternion HandFollowThrough(float spin)=>Quaternion.Euler(48,-23+24*spin,-12+32*spin);
        public static Quaternion ReleaseHand(Quaternion from,float seconds,float spin)
        {
            var contact=Quaternion.Euler(27,-12+14*spin,-6+14*spin);
            var follow=HandFollowThrough(spin);
            if(seconds<ContactSeconds)return Quaternion.Slerp(from,contact,Mathf.Clamp01(seconds/ContactSeconds));
            if(seconds<FollowSeconds)return Quaternion.Slerp(contact,follow,
                Mathf.SmoothStep(0,1,Mathf.InverseLerp(ContactSeconds,FollowSeconds,seconds)));
            return Quaternion.Slerp(follow,Quaternion.identity,Recovery(seconds));
        }
    }
}
