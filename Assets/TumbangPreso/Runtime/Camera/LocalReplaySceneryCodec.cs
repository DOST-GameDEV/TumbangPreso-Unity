using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    [Serializable]public sealed class LocalReplaySceneryFrame
    {public float Time;public List<LocalReplaySceneryRoot> Roots=new List<LocalReplaySceneryRoot>();}
    [Serializable]public sealed class LocalReplayScenerySegment
    {public int Version=1;public List<LocalReplaySceneryFrame> Frames=new List<LocalReplaySceneryFrame>();}
    public static class LocalReplaySceneryCodec
    {
        public const int MaxEncodedBytes=32*1024*1024,MaxDecodedBytes=256*1024*1024;
        public static byte[] Encode(LocalReplayScenerySegment segment)
        {
            Validate(segment,float.NegativeInfinity,float.PositiveInfinity);
            byte[] raw=Encoding.UTF8.GetBytes(JsonUtility.ToJson(segment));
            if(raw.Length>MaxDecodedBytes)throw new InvalidDataException("Recorded scenery exceeds the decoded format limit.");
            using var bytes=new MemoryStream();
            using(var zip=new GZipStream(bytes,System.IO.Compression.CompressionLevel.Fastest,true))zip.Write(raw,0,raw.Length);
            if(bytes.Length>MaxEncodedBytes)throw new InvalidDataException("Recorded scenery exceeds the encoded format limit.");
            return bytes.ToArray();
        }
        public static LocalReplayScenerySegment Decode(byte[] bytes,float start,float end)
        {
            if(bytes.Length>MaxEncodedBytes)throw new InvalidDataException("Recorded scenery exceeds the encoded format limit.");
            using var zip=new GZipStream(new MemoryStream(bytes),CompressionMode.Decompress);
            using var raw=new MemoryStream();var buffer=new byte[8192];int count;
            while((count=zip.Read(buffer,0,buffer.Length))>0)
            {if(raw.Length+count>MaxDecodedBytes)throw new InvalidDataException("Recorded scenery exceeds the decoded format limit.");raw.Write(buffer,0,count);}
            var segment=JsonUtility.FromJson<LocalReplayScenerySegment>(Encoding.UTF8.GetString(raw.ToArray()));Validate(segment,start,end);return segment;
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        private static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
        private static bool Finite(Vector4 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z)&&Finite(v.w);
        private static bool Finite(Quaternion v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z)&&Finite(v.w);
        private static bool Finite(Color v)=>Finite(v.r)&&Finite(v.g)&&Finite(v.b)&&Finite(v.a);
        private static bool Text(string value,bool empty=false)=>value!=null&&value.Length<=1024&&(empty||value.Length>0);
        public static void Validate(LocalReplayScenerySegment segment,float start,float end)
        {
            if(segment?.Frames==null||segment.Version!=1||segment.Frames.Count<1||segment.Frames.Count>4096)throw new InvalidDataException("Invalid recorded scenery frame count.");
            float previous=float.NegativeInfinity;
            foreach(var frame in segment.Frames)
            {
                if(frame==null||!Finite(frame.Time)||frame.Time<=previous||frame.Time<start-.1f||frame.Time>end+.1f||frame.Roots==null||frame.Roots.Count>64)
                    throw new InvalidDataException("Invalid recorded scenery frame.");
                previous=frame.Time;var ids=new HashSet<string>();
                foreach(var root in frame.Roots)
                {
                    if(root==null||(byte)root.Kind>2||!Text(root.Id)||!Text(root.Art)||root.Art.Length!=64||!Text(root.OwnerPath,true)||!Text(root.OwnerName,true)||!Text(root.Model,true)||!ids.Add(root.Kind+":"+root.OwnerPath+":"+root.Id+":"+root.Generation)||
                        root.Paths==null||root.Paths.Length<1||root.Paths.Length>256||root.Paths[0]!=""||root.Positions==null||root.Rotations==null||root.Scales==null||root.Active==null||
                        root.Positions.Length!=root.Paths.Length||root.Rotations.Length!=root.Paths.Length||root.Scales.Length!=root.Paths.Length||root.Active.Length!=root.Paths.Length||
                        root.Surfaces==null||root.Surfaces.Count>512||root.Lines==null||root.Lines.Count>16)
                        throw new InvalidDataException("Invalid recorded scenery root.");
                    var paths=new HashSet<string>();
                    for(int n=0;n<root.Paths.Length;n++)
                        if(!Text(root.Paths[n],n==0)||root.Paths[n].Split('/').Any(p=>p==".."||p==".")||!paths.Add(root.Paths[n])||!Finite(root.Positions[n])||!Finite(root.Rotations[n])||!Finite(root.Scales[n]))throw new InvalidDataException("Invalid recorded scenery pose.");
                    foreach(var surface in root.Surfaces)
                        if(surface==null||!Text(surface.Path,true)||!paths.Contains(surface.Path)||surface.Slot< -1||surface.Slot>256||!Finite(surface.Colour)||!Finite(surface.Uv))throw new InvalidDataException("Invalid recorded scenery surface.");
                    foreach(var line in root.Lines)
                        if(line==null||!Text(line.Path)||line.Points==null||line.Points.Length>256||Array.Exists(line.Points,p=>!Finite(p))||
                            !Finite(line.StartWidth)||!Finite(line.EndWidth)||line.StartWidth<0||line.EndWidth<0||!Finite(line.StartColour)||!Finite(line.EndColour))throw new InvalidDataException("Invalid recorded scenery line.");
                }
            }
        }
        public static LocalReplaySceneryFrame At(LocalReplayScenerySegment segment,float time)
        {
            if(segment?.Frames==null||segment.Frames.Count==0||time<segment.Frames[0].Time)return null;
            var frame=segment.Frames[0];foreach(var candidate in segment.Frames){if(candidate.Time>time)break;frame=candidate;}return frame;
        }
    }
}
