using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using ArenaFx=TumbangPreso.Map.ArenaFx;

namespace TumbangPreso.CameraSystem
{
    public sealed class LocalReplayFxFrame {public float Time;public ArenaFx.RecordedQuad[] Quads;}
    public sealed class LocalReplayFxSegment {public readonly List<LocalReplayFxFrame> Frames=new List<LocalReplayFxFrame>();}
    public static class LocalReplayEffectsCodec
    {
        public const int MaxEncodedBytes=32*1024*1024,MaxDecodedBytes=256*1024*1024;
        private const int MaxFrames=4096,MaxQuads=ArenaFx.MaxParticles+ArenaFx.MaxImmediate;
        public static byte[] Encode(LocalReplayFxSegment segment)
        {
            if(segment==null||segment.Frames.Count<1||segment.Frames.Count>MaxFrames)throw new InvalidDataException("Invalid effects frame count.");
            long decodedSize=12;foreach(var frame in segment.Frames)
            {if(frame.Quads==null||frame.Quads.Length>MaxQuads)throw new InvalidDataException("Invalid effects pool size.");decodedSize+=8+frame.Quads.Length*90L;}
            if(decodedSize>MaxDecodedBytes)throw new InvalidDataException("Recorded effects exceed the decoded format limit.");
            using var encoded=new MemoryStream();
            using(var zip=new GZipStream(encoded,System.IO.Compression.CompressionLevel.Fastest,true))
            // Feed the compressor blocks rather than millions of scalar writes.
            // The version, payload order and exact recorded values stay intact.
            using(var buffered=new BufferedStream(zip,64*1024))
            using(var writer=new BinaryWriter(buffered))
            {
                writer.Write(0x46505854);writer.Write(1);writer.Write(segment.Frames.Count);
                foreach(var frame in segment.Frames)
                {
                    writer.Write(frame.Time);writer.Write(frame.Quads.Length);
                    foreach(var quad in frame.Quads)
                    {
                        writer.Write((byte)quad.Facing);writer.Write(quad.Cell);writer.Write(quad.Colour);
                        Vector(writer,quad.A);Vector(writer,quad.B);Vector(writer,quad.C);Vector(writer,quad.D);Vector(writer,quad.Eye);Vector(writer,quad.Right);Vector(writer,quad.Up);
                    }
                }
            }
            if(encoded.Length>MaxEncodedBytes)throw new InvalidDataException("Recorded effects exceed the encoded format limit.");
            return encoded.ToArray();
        }
        public static LocalReplayFxSegment Decode(byte[] encoded,float start,float end)
        {
            if(encoded.Length>MaxEncodedBytes)throw new InvalidDataException("Recorded effects exceed the encoded format limit.");
            using var source=new MemoryStream(encoded);using var zip=new GZipStream(source,CompressionMode.Decompress);
            using var decoded=new MemoryStream();var buffer=new byte[8192];int read;
            while((read=zip.Read(buffer,0,buffer.Length))>0)
            {if(decoded.Length+read>MaxDecodedBytes)throw new InvalidDataException("Recorded effects exceed the decoded format limit.");decoded.Write(buffer,0,read);}
            decoded.Position=0;using var reader=new BinaryReader(decoded);
            if(reader.ReadInt32()!=0x46505854||reader.ReadInt32()!=1)throw new InvalidDataException("Recorded effects version is incompatible.");
            int frames=reader.ReadInt32();if(frames<1||frames>MaxFrames)throw new InvalidDataException("Invalid effects frame count.");
            var result=new LocalReplayFxSegment();float previous=float.NegativeInfinity;
            for(int i=0;i<frames;i++)
            {
                float time=Number(reader);if(time<=previous||time<start-.1f||time>end+.1f)throw new InvalidDataException("Invalid effects timeline.");previous=time;
                int count=reader.ReadInt32();if(count<0||count>MaxQuads)throw new InvalidDataException("Invalid effects pool size.");
                var frame=new LocalReplayFxFrame{Time=time,Quads=new ArenaFx.RecordedQuad[count]};
                for(int q=0;q<count;q++)
                {
                    var facing=(ArenaFx.RecordedFacing)reader.ReadByte();byte cell=reader.ReadByte();uint colour=reader.ReadUInt32();
                    if((byte)facing>(byte)ArenaFx.RecordedFacing.Beam||cell>=16)throw new InvalidDataException("Invalid effects shape.");
                    frame.Quads[q]=new ArenaFx.RecordedQuad{Facing=facing,Cell=cell,Colour=colour,A=Vector(reader),B=Vector(reader),C=Vector(reader),D=Vector(reader),Eye=Vector(reader),Right=Vector(reader),Up=Vector(reader)};
                }
                result.Frames.Add(frame);
            }
            if(decoded.Position!=decoded.Length)throw new InvalidDataException("Unexpected recorded effects data.");return result;
        }
        private static void Vector(BinaryWriter writer,Vector3 value){writer.Write(value.x);writer.Write(value.y);writer.Write(value.z);}
        private static float Number(BinaryReader reader){float value=reader.ReadSingle();if(float.IsNaN(value)||float.IsInfinity(value))throw new InvalidDataException("Invalid effects number.");return value;}
        private static Vector3 Vector(BinaryReader reader)=>new Vector3(Number(reader),Number(reader),Number(reader));
        public static ArenaFx.RecordedQuad[] At(LocalReplayFxSegment segment,float time)
        {
            if(segment==null||segment.Frames.Count==0||time<segment.Frames[0].Time)return Array.Empty<ArenaFx.RecordedQuad>();
            var chosen=segment.Frames[0];foreach(var frame in segment.Frames){if(frame.Time>time)break;chosen=frame;}return chosen.Quads;
        }
    }
}
