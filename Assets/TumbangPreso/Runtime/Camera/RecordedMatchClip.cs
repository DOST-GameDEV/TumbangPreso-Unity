using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public enum RecordedObjectKind : byte { Player, Slipper, Can, Familiar }
    public sealed class RecordedObjectTrack
    {
        public RecordedObjectKind Kind;
        public int Seat, Skin;
        public string Person;
        public RecordedPoseTrack Pose;
    }
    public struct RecordedWorldCue
    { public float Time,Pitch,Gain;public Vector3 Position;public string Id; }
    public sealed class RecordedMatchClip
    {
        public const int WireVersion=1;
        public const int ByteLimit=2*1024*1024;
        public const int RawByteLimit=12*1024*1024;
        public long MatchId,Id;
        public int Round,Actor,Subject;
        public GameMode Mode;
        public string Map,Reason;
        public float Start,End,Contact;
        public RecordedObjectTrack[] Objects;
        public RecordedWorldCue[] Sounds=Array.Empty<RecordedWorldCue>();
        public float Duration=>End-Start;
        public byte[] Encode()
        {
            using var raw=new MemoryStream();using(var writer=new BinaryWriter(raw,Encoding.UTF8,true))
            {
                writer.Write(0x54554d50);writer.Write(WireVersion);writer.Write(MatchId);writer.Write(Id);
                writer.Write(Round);writer.Write(Actor);writer.Write(Subject);writer.Write((byte)Mode);
                WriteText(writer,Map,64);WriteText(writer,Reason,96);
                writer.Write(Start);writer.Write(End);writer.Write(Contact);writer.Write(Objects.Length);
                foreach(var item in Objects)
                {
                    writer.Write((byte)item.Kind);writer.Write(item.Seat);writer.Write(item.Skin);WriteText(writer,item.Person??"",64);
                    var pose=item.Pose;writer.Write(pose.Paths.Length);writer.Write(pose.Samples.Length);
                    foreach(string path in pose.Paths)WriteText(writer,path,512);
                    foreach(var sample in pose.Samples)
                    {
                        writer.Write(sample.Time);
                        for(int b=0;b<pose.Paths.Length;b++)
                        {
                            Write(writer,sample.Positions[b]);Write(writer,sample.Rotations[b]);Write(writer,sample.Scales[b]);writer.Write(sample.Active[b]);
                        }
                    }
                    if(raw.Length>RawByteLimit)throw new InvalidDataException("Recorded clip exceeds its raw budget");
                }
                writer.Write(Sounds.Length);
                foreach(var cue in Sounds){writer.Write(cue.Time);WriteText(writer,cue.Id,64);Write(writer,cue.Position);writer.Write(cue.Pitch);writer.Write(cue.Gain);}
            }
            using var packed=new MemoryStream();
            using(var deflate=new DeflateStream(packed,System.IO.Compression.CompressionLevel.Fastest,true))
            {raw.Position=0;raw.CopyTo(deflate);}
            if(packed.Length>ByteLimit)throw new InvalidDataException("Recorded clip exceeds its transport budget");
            var result=packed.ToArray();
            if(!TryDecode(result,out _,out string error))throw new InvalidDataException(error);
            return result;
        }
        public static bool TryDecode(byte[] bytes,out RecordedMatchClip clip,out string error)
        {
            clip=null;error=null;
            if(bytes==null||bytes.Length<8||bytes.Length>ByteLimit){error="Invalid clip byte length";return false;}
            try
            {
                using var packed=new MemoryStream(bytes,false);using var deflate=new DeflateStream(packed,CompressionMode.Decompress);
                using var raw=new MemoryStream();var buffer=new byte[8192];int read;
                while((read=deflate.Read(buffer,0,buffer.Length))>0)
                {if(raw.Length+read>RawByteLimit)throw new InvalidDataException("Expanded clip exceeds its budget");raw.Write(buffer,0,read);}
                raw.Position=0;using var reader=new BinaryReader(raw,Encoding.UTF8,true);
                if(reader.ReadInt32()!=0x54554d50||reader.ReadInt32()!=WireVersion)throw new InvalidDataException("Unsupported clip schema");
                var result=new RecordedMatchClip{MatchId=reader.ReadInt64(),Id=reader.ReadInt64(),Round=reader.ReadInt32(),Actor=reader.ReadInt32(),Subject=reader.ReadInt32(),Mode=(GameMode)reader.ReadByte()};
                result.Map=ReadText(reader,64);result.Reason=ReadText(reader,96);
                result.Start=reader.ReadSingle();result.End=reader.ReadSingle();result.Contact=reader.ReadSingle();
                if(result.MatchId<=0||result.Id<=0||result.Round<1||result.Round>64||result.Actor<0||result.Actor>=4||result.Subject< -1||result.Subject>=4
                    ||!Enum.IsDefined(typeof(GameMode),result.Mode)||!Finite(result.Start)||!Finite(result.End)||!Finite(result.Contact)
                    ||result.Duration<.5f||result.Duration>8.1f||result.Contact<result.Start||result.Contact>result.End)throw new InvalidDataException("Invalid clip identity/window");
                int count=Count(reader,1,13);result.Objects=new RecordedObjectTrack[count];
                int totalSamples=0;
                for(int i=0;i<count;i++)
                {
                    var item=new RecordedObjectTrack{Kind=(RecordedObjectKind)reader.ReadByte(),Seat=reader.ReadInt32(),Skin=reader.ReadInt32(),Person=ReadText(reader,64)};
                    if(!Enum.IsDefined(typeof(RecordedObjectKind),item.Kind)||item.Seat< -1||item.Seat>=4||item.Skin<0||item.Skin>128)throw new InvalidDataException("Invalid recorded object");
                    int bones=Count(reader,1,MatchPoseHistory.TransformLimit),frames=Count(reader,2,MatchPoseHistory.Samples);
                    totalSamples+=bones*frames;if(totalSamples>RawByteLimit/41)throw new InvalidDataException("Pose allocation exceeds its budget");
                    var paths=new string[bones];
                    for(int b=0;b<bones;b++)
                    {paths[b]=ReadText(reader,512);if(paths[b].StartsWith("/")||paths[b].Contains(".."))throw new InvalidDataException("Invalid bone binding");}
                    if(paths[0].Length!=0)throw new InvalidDataException("Missing root binding");
                    var samples=new RecordedPoseTrack.Sample[frames];float previous=float.NegativeInfinity;
                    for(int f=0;f<frames;f++)
                    {
                        float time=reader.ReadSingle();
                        if(!Finite(time)||time<=previous||time<result.Start-.3f||time>result.End+.3f)throw new InvalidDataException("Invalid pose time");
                        previous=time;var sample=new RecordedPoseTrack.Sample{Time=time,Positions=new Vector3[bones],Rotations=new Quaternion[bones],Scales=new Vector3[bones],Active=new bool[bones]};
                        for(int b=0;b<bones;b++)
                        {
                            sample.Positions[b]=ReadVector(reader,10000);sample.Rotations[b]=ReadRotation(reader);sample.Scales[b]=ReadVector(reader,100);
                            sample.Active[b]=reader.ReadBoolean();
                        }
                        samples[f]=sample;
                    }
                    if(samples[0].Time>result.Start+.001f||samples[frames-1].Time<result.End-.001f)throw new InvalidDataException("Incomplete object window");
                    item.Pose=new RecordedPoseTrack(paths,samples);result.Objects[i]=item;
                }
                int soundCount=Count(reader,0,256);result.Sounds=new RecordedWorldCue[soundCount];
                float soundTime=float.NegativeInfinity;
                for(int i=0;i<soundCount;i++)
                {
                    var cue=new RecordedWorldCue{Time=reader.ReadSingle(),Id=ReadText(reader,64),Position=ReadVector(reader,10000),Pitch=reader.ReadSingle(),Gain=reader.ReadSingle()};
                    if(!Finite(cue.Time)||cue.Time<soundTime||cue.Time<result.Start||cue.Time>result.End||!Finite(cue.Pitch)||cue.Pitch<.25f||cue.Pitch>3
                        ||!Finite(cue.Gain)||cue.Gain<0||cue.Gain>2)throw new InvalidDataException("Invalid recorded audio cue");
                    soundTime=cue.Time;result.Sounds[i]=cue;
                }
                if(raw.Position!=raw.Length)throw new InvalidDataException("Unexpected trailing clip data");
                clip=result;return true;
            }
            catch(Exception failure) when(failure is IOException||failure is ArgumentException||failure is OverflowException)
            {error=failure.Message;return false;}
        }
        private static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        private static int Count(BinaryReader reader,int min,int max)
        {int n=reader.ReadInt32();if(n<min||n>max)throw new InvalidDataException("Invalid clip count");return n;}
        private static void WriteText(BinaryWriter writer,string text,int limit)
        {var bytes=Encoding.UTF8.GetBytes(text??"");if(bytes.Length>limit)throw new InvalidDataException("Clip string too long");writer.Write((ushort)bytes.Length);writer.Write(bytes);}
        private static string ReadText(BinaryReader reader,int limit)
        {int count=reader.ReadUInt16();if(count>limit)throw new InvalidDataException("Clip string too long");var bytes=reader.ReadBytes(count);if(bytes.Length!=count)throw new EndOfStreamException();return new UTF8Encoding(false,true).GetString(bytes);}
        private static void Write(BinaryWriter w,Vector3 p){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
        private static void Write(BinaryWriter w,Quaternion p){w.Write(p.x);w.Write(p.y);w.Write(p.z);w.Write(p.w);}
        private static Vector3 ReadVector(BinaryReader r,float bound)
        {var v=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||Mathf.Abs(v.x)>bound||Mathf.Abs(v.y)>bound||Mathf.Abs(v.z)>bound)throw new InvalidDataException("Invalid recorded vector");return v;}
        private static Quaternion ReadRotation(BinaryReader r)
        {var v=new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());float length=v.x*v.x+v.y*v.y+v.z*v.z+v.w*v.w;if(!Finite(length)||length<.5f||length>1.5f)throw new InvalidDataException("Invalid recorded rotation");return v;}
    }
}
