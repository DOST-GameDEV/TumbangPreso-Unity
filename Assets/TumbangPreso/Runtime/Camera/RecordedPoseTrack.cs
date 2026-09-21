using System;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // Detached values, so a retained highlight cannot be overwritten by the live
    // ring or depend on the next round's transforms. Rendering never runs actors.
    public sealed class RecordedPoseTrack
    {
        public sealed class Sample
        {
            public float Time;
            public Vector3[] Positions, Scales;
            public Quaternion[] Rotations;
            public bool[] Active;
        }
        public readonly string[] Paths;
        public readonly Sample[] Samples;
        public float Start => Samples.Length>0?Samples[0].Time:0;
        public float End => Samples.Length>0?Samples[Samples.Length-1].Time:0;
        public RecordedPoseTrack(string[] paths,Sample[] samples)
        {Paths=paths;Samples=samples;}
        public Transform[] Bind(GameObject renderOnlyRoot)
        {
            if(renderOnlyRoot==null)return null;
            var bones=new Transform[Paths.Length];
            for(int i=0;i<Paths.Length;i++)
            {
                bones[i]=Paths[i].Length==0?renderOnlyRoot.transform:renderOnlyRoot.transform.Find(Paths[i]);
                if(bones[i]==null)return null;
            }
            return bones;
        }
        public void Apply(Transform[] bones,float time)
        {
            if(bones==null||bones.Length!=Paths.Length||Samples.Length==0)return;
            var left=Samples[0];var right=left;
            for(int i=1;i<Samples.Length;i++)
            {right=Samples[i];if(right.Time>=time)break;left=right;}
            float t=right.Time>left.Time?Mathf.Clamp01((time-left.Time)/(right.Time-left.Time)):0;
            // A tag/recall teleport is an edge, never a fast walk through the map.
            bool discontinuity=(right.Positions[0]-left.Positions[0]).sqrMagnitude>4;
            if(discontinuity&&time<right.Time)t=0;
            for(int i=0;i<bones.Length;i++)
            {
                var bone=bones[i];if(bone==null)continue;
                bone.localPosition=Vector3.Lerp(left.Positions[i],right.Positions[i],t);
                bone.localRotation=Quaternion.Slerp(left.Rotations[i],right.Rotations[i],t);
                bone.localScale=Vector3.Lerp(left.Scales[i],right.Scales[i],t);
                bone.gameObject.SetActive(time<right.Time?left.Active[i]:right.Active[i]);
            }
        }
    }
}
