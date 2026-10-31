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
            public int State,Holder=-1,Epoch=-1;
            public bool HasCoat,HasAccent;public float Frost,Flash,RimStrength;public StunElement Element;public Color RimColour;
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
        public RecordedPoseTrack WithKey(Sample key)
        {
            if(key==null||key.Time<Start||key.Time>End)return this;
            var samples=new System.Collections.Generic.List<Sample>(Samples.Length+1);
            foreach(var sample in Samples)if(Mathf.Abs(sample.Time-key.Time)>.00001f)samples.Add(sample);
            samples.Add(key);samples.Sort((a,b)=>a.Time.CompareTo(b.Time));
            return new RecordedPoseTrack(Paths,samples.ToArray());
        }
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
        public Sample StateAt(float time)
        {var result=Samples[0];foreach(var sample in Samples){if(sample.Time>time)break;result=sample;}return result;}
        /// <summary>The root's position on a curve through the samples: 20 keys a second kink visibly once slowed down.</summary>
        public Vector3 SmoothRoot(float time)
        {
            int n=Samples.Length;if(n==0)return Vector3.zero;
            if(n<2)return Samples[0].Positions[0];
            int i=0;while(i<n-2&&Samples[i+1].Time<time)i++;
            Sample a=Samples[Mathf.Max(0,i-1)],b=Samples[i],c=Samples[i+1],d=Samples[Mathf.Min(n-1,i+2)];
            float span=c.Time-b.Time;
            float t=span>0?Mathf.Clamp01((time-b.Time)/span):0;
            // A teleport anywhere near is an edge: leave it to the straight reading in Apply.
            if(a.Epoch!=b.Epoch||b.Epoch!=c.Epoch||c.Epoch!=d.Epoch||(c.Positions[0]-b.Positions[0]).sqrMagnitude>4)
                return t>=1?c.Positions[0]:Vector3.Lerp(b.Positions[0],c.Positions[0],b.Epoch!=c.Epoch?0:t);
            Vector3 p=b.Positions[0],q=c.Positions[0];
            Vector3 m0=a==b?q-p:(q-a.Positions[0])*(span/Mathf.Max(.001f,c.Time-a.Time));
            Vector3 m1=c==d?q-p:(d.Positions[0]-p)*(span/Mathf.Max(.001f,d.Time-b.Time));
            float tt=t*t,ttt=tt*t;
            return (2*ttt-3*tt+1)*p+(ttt-2*tt+t)*m0+(3*tt-2*ttt)*q+(ttt-tt)*m1;
        }
        public void Apply(Transform[] bones,float time)
        {
            if(bones==null||bones.Length!=Paths.Length||Samples.Length==0)return;
            var left=Samples[0];var right=left;
            for(int i=1;i<Samples.Length;i++)
            {right=Samples[i];if(right.Time>=time)break;left=right;}
            float t=right.Time>left.Time?Mathf.Clamp01((time-left.Time)/(right.Time-left.Time)):0;
            // A tag/recall teleport is an edge, never a fast walk through the map.
            bool flight=left.Holder<0&&right.Holder<0&&(left.State&255)==(int)SlipperState.InFlight&&(right.State&255)==(int)SlipperState.InFlight;
            bool discontinuity=left.Epoch>=0&&right.Epoch>=0?left.Epoch!=right.Epoch:!flight&&(right.Positions[0]-left.Positions[0]).sqrMagnitude>4;
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
