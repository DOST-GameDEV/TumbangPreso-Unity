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
        /// <summary>
        /// As `Apply`, on curves through the samples. A replay slowed to a fifth shows every straight join between 20 keys a
        /// second as a tick in the limb. A teleport nearby keeps `Apply`'s hard edge.
        /// </summary>
        public void ApplySmooth(Transform[] bones,float time)
        {
            int n=Samples.Length;
            if(bones==null||bones.Length!=Paths.Length||n<3){Apply(bones,time);return;}
            int k=0;while(k<n-2&&Samples[k+1].Time<time)k++;
            Sample a=Samples[Mathf.Max(0,k-1)],b=Samples[k],c=Samples[k+1],d=Samples[Mathf.Min(n-1,k+2)];
            if(a.Epoch!=b.Epoch||b.Epoch!=c.Epoch||c.Epoch!=d.Epoch){Apply(bones,time);return;}
            float span=c.Time-b.Time,t=span>0?Mathf.Clamp01((time-b.Time)/span):0;
            float tt=t*t,ttt=tt*t,h00=2*ttt-3*tt+1,h10=ttt-2*tt+t,h01=3*tt-2*ttt,h11=ttt-tt;
            float inScale=span/Mathf.Max(.001f,c.Time-a.Time),outScale=span/Mathf.Max(.001f,d.Time-b.Time);
            for(int i=0;i<bones.Length;i++)
            {
                var bone=bones[i];if(bone==null)continue;
                Vector3 p=b.Positions[i],q=c.Positions[i];
                bone.localPosition=h00*p+h10*((q-a.Positions[i])*inScale)+h01*q+h11*((d.Positions[i]-p)*outScale);
                Quaternion r=b.Rotations[i],s=Near(r,c.Rotations[i]),r0=Near(r,a.Rotations[i]),s1=Near(s,d.Rotations[i]);
                var turned=new Quaternion(
                    h00*r.x+h10*(s.x-r0.x)*inScale+h01*s.x+h11*(s1.x-r.x)*outScale,
                    h00*r.y+h10*(s.y-r0.y)*inScale+h01*s.y+h11*(s1.y-r.y)*outScale,
                    h00*r.z+h10*(s.z-r0.z)*inScale+h01*s.z+h11*(s1.z-r.z)*outScale,
                    h00*r.w+h10*(s.w-r0.w)*inScale+h01*s.w+h11*(s1.w-r.w)*outScale);
                bone.localRotation=Quaternion.Normalize(turned);
                bone.localScale=Vector3.Lerp(b.Scales[i],c.Scales[i],t);
                bone.gameObject.SetActive(time<c.Time?b.Active[i]:c.Active[i]);
            }
        }
        private static Quaternion Near(Quaternion to,Quaternion q)=>Quaternion.Dot(to,q)<0?new Quaternion(-q.x,-q.y,-q.z,-q.w):q;
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
