using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public enum RecordedObjectKind : byte { Player, Slipper, Can, Familiar }
    public sealed class RecordedObjectTrack
    {
        public RecordedObjectKind Kind;
        public int Seat, Skin;
        public string Person,VisualKey,DisplayName;
        public RecordedPoseTrack Pose;
    }
    public struct RecordedWorldCue
    { public float Time,Pitch,Gain;public Vector3 Position;public string Id; }
    public sealed class RecordedMatchClip
    {
        public const int WireVersion=11;
        public const int ByteLimit=2*1024*1024;
        public const int RawByteLimit=12*1024*1024;
        public long MatchId,Id;
        public int Round,Actor,Subject;
        public GameMode Mode;
        public string Map,Reason;
        public float Start,End,Contact;
        public RecordedObjectTrack[] Objects;
        public RecordedFieldFrame[] FieldFrames=Array.Empty<RecordedFieldFrame>();
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
                    writer.Write((byte)item.Kind);writer.Write(item.Seat);writer.Write(item.Skin);WriteText(writer,item.Person??"",64);WriteText(writer,item.VisualKey??"",64);WriteText(writer,item.DisplayName??"",96);
                    var pose=item.Pose;writer.Write(pose.Paths.Length);writer.Write(pose.Samples.Length);
                    foreach(string path in pose.Paths)WriteText(writer,path,512);
                    foreach(var sample in pose.Samples)
                    {
                        writer.Write(sample.Time);writer.Write(sample.State);writer.Write(sample.Holder);writer.Write(sample.Epoch);
                        writer.Write(sample.HasAccent);writer.Write(sample.RimStrength);WriteColour(writer,sample.RimColour);
                        writer.Write(sample.HasCoat);writer.Write(sample.Frost);writer.Write(sample.Flash);writer.Write((byte)sample.Element);
                        for(int b=0;b<pose.Paths.Length;b++)
                        {
                            Write(writer,sample.Positions[b]);Write(writer,sample.Rotations[b]);Write(writer,sample.Scales[b]);writer.Write(sample.Active[b]);
                        }
                    }
                    if(raw.Length>RawByteLimit)throw new InvalidDataException("Recorded clip exceeds its raw budget");
                }
                writer.Write(FieldFrames.Length);
                foreach(var frame in FieldFrames)
                {
                    writer.Write(frame.Time);frame.Lighting.Write(writer);
                    writer.Write(frame.Trails.Length);
                    foreach(var trail in frame.Trails)
                    {
                        writer.Write(trail.Id);writer.Write(trail.Kind);writer.Write(trail.Width);WriteColour(writer,trail.Head);WriteColour(writer,trail.Tail);
                        writer.Write(trail.Points.Length);foreach(var point in trail.Points)Write(writer,point);
                    }
                    writer.Write(frame.Fields.Length);
                    foreach(var item in frame.Fields)
                    {
                        var f=item.State;writer.Write(item.Id);writer.Write((byte)f.Type);Write(writer,f.Position);Write(writer,f.Forward);
                        writer.Write(f.Duration);writer.Write(f.Remaining);writer.Write(f.Radius);writer.Write(f.FirstScale);writer.Write(f.SecondScale);writer.Write(f.Owner);writer.Write(f.Split);
                        if(Abilities.RafiWaterField.IsWater(f.Type))
                        { writer.Write(f.EventId);writer.Write(f.Path.Length);foreach(var point in f.Path)Write(writer,point); }
                    }
                }
                writer.Write(Sounds.Length);
                foreach(var cue in Sounds){writer.Write(cue.Time);WriteText(writer,cue.Id,64);Write(writer,cue.Position);writer.Write(cue.Pitch);writer.Write(cue.Gain);}
            }
            if(raw.Length>RawByteLimit)throw new InvalidDataException("Recorded clip exceeds its raw budget");
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
                if(reader.ReadInt32()!=0x54554d50)throw new InvalidDataException("Unsupported clip schema");
                int version=reader.ReadInt32();
                // Version10 has the identical layout for pre-water fields. Keep
                // those saved clips readable; live network admission still requires49.
                if(version!=WireVersion&&version!=10)throw new InvalidDataException("Unsupported clip schema");
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
                    var item=new RecordedObjectTrack{Kind=(RecordedObjectKind)reader.ReadByte(),Seat=reader.ReadInt32(),Skin=reader.ReadInt32(),Person=ReadText(reader,64),VisualKey=ReadText(reader,64),DisplayName=ReadText(reader,96)};
                    if(!Enum.IsDefined(typeof(RecordedObjectKind),item.Kind)||item.Seat< -1||item.Seat>=4||item.Skin< -1||item.Skin>128)throw new InvalidDataException("Invalid recorded object");
                    int bones=Count(reader,1,MatchPoseHistory.TransformLimit),frames=Count(reader,2,MatchPoseHistory.Samples);
                    totalSamples+=bones*frames;if(totalSamples>RawByteLimit/41)throw new InvalidDataException("Pose allocation exceeds its budget");
                    var paths=new string[bones];
                    for(int b=0;b<bones;b++)
                    {paths[b]=ReadText(reader,512);if(paths[b].StartsWith("/")||paths[b].Contains(".."))throw new InvalidDataException("Invalid bone binding");}
                    if(paths[0].Length!=0)throw new InvalidDataException("Missing root binding");
                    var samples=new RecordedPoseTrack.Sample[frames];float previous=float.NegativeInfinity;
                    for(int f=0;f<frames;f++)
                    {
                        float time=reader.ReadSingle();int state=reader.ReadInt32(),holder=reader.ReadInt32(),epoch=reader.ReadInt32();
                        bool accent=reader.ReadBoolean();float rim=reader.ReadSingle();Color rimColour=ReadColour(reader);
                        if(!Finite(rim)||rim<0||rim>10)throw new InvalidDataException("Invalid recorded rim");
                        bool coat=reader.ReadBoolean();float frost=reader.ReadSingle(),flash=reader.ReadSingle();var element=(StunElement)reader.ReadByte();
                        if(!Finite(frost)||!Finite(flash)||frost<0||frost>1||flash<0||flash>1||!Enum.IsDefined(typeof(StunElement),element))throw new InvalidDataException("Invalid recorded body coat");
                        if(state<0||state>2048||holder< -1||holder>=4||epoch< -1)throw new InvalidDataException("Invalid recorded prop state");
                        if(!Finite(time)||time<=previous||time<result.Start-.3f||time>result.End+.3f)throw new InvalidDataException("Invalid pose time");
                        previous=time;var sample=new RecordedPoseTrack.Sample{Time=time,State=state,Holder=holder,Epoch=epoch,HasCoat=coat,HasAccent=accent,RimStrength=rim,RimColour=rimColour,Frost=frost,Flash=flash,Element=element,Positions=new Vector3[bones],Rotations=new Quaternion[bones],Scales=new Vector3[bones],Active=new bool[bones]};
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
                int fieldFrameCount=Count(reader,0,MatchPoseHistory.Samples),totalFields=0;float fieldTime=float.NegativeInfinity;
                result.FieldFrames=new RecordedFieldFrame[fieldFrameCount];
                for(int n=0;n<fieldFrameCount;n++)
                {
                    float time=reader.ReadSingle();var lighting=RecordedEnvironment.Read(reader);
                    int trailsCount=Count(reader,0,16);var trails=new RecordedTrail[trailsCount];var trailIds=new System.Collections.Generic.HashSet<int>();
                    for(int t=0;t<trailsCount;t++)
                    {
                        var trail=new RecordedTrail{Id=reader.ReadInt32(),Kind=reader.ReadInt32(),Width=reader.ReadSingle(),Head=ReadColour(reader),Tail=ReadColour(reader)};
                        if(trail.Id<0||trail.Id>=16||trail.Kind<0||trail.Kind>3||trail.Id%4!=trail.Kind||!trailIds.Add(trail.Id)||!Finite(trail.Width)||trail.Width<=0||trail.Width>.5f)throw new InvalidDataException("Invalid recorded flight stroke");
                        int points=Count(reader,2,32);trail.Points=new Vector3[points];for(int q=0;q<points;q++)trail.Points[q]=ReadVector(reader,10000);trails[t]=trail;
                    }
                    int fields=Count(reader,0,WorldEffectSnapshot.MaxFields);totalFields+=fields;
                    if(!Finite(time)||time<=fieldTime||time<result.Start-.3f||time>result.End+.3f||totalFields>32768)throw new InvalidDataException("Invalid field window");
                    fieldTime=time;var frame=new RecordedFieldFrame{Time=time,Fields=new RecordedField[fields],Lighting=lighting,Trails=trails};var ids=new System.Collections.Generic.HashSet<int>();
                    for(int i=0;i<fields;i++)
                    {
                        int id=reader.ReadInt32();var kind=(WorldEffectSnapshot.Kind)reader.ReadByte();
                        var f=new WorldEffectSnapshot.Field{Type=kind,Position=ReadVector(reader,10000),Forward=ReadVector(reader,kind==RecordedSpecialFields.Kuro?10000:2),
                            Duration=reader.ReadSingle(),Remaining=reader.ReadSingle(),Radius=reader.ReadSingle(),FirstScale=reader.ReadSingle(),SecondScale=reader.ReadSingle(),Owner=reader.ReadInt32(),Split=reader.ReadBoolean()};
                        if(Abilities.RafiWaterField.IsWater(kind))
                        {
                            if(version<11)throw new InvalidDataException("Water field in a pre-water clip");
                            f.EventId=reader.ReadInt32();int points=Count(reader,0,Abilities.RafiWaterField.MaxPathPoints);
                            f.Path=new Vector3[points];for(int p=0;p<points;p++)f.Path[p]=ReadVector(reader,10000);
                        }
                        if(id<=0||id>100000||!ids.Add(id)||!RecordedSpecialFields.Valid(f))throw new InvalidDataException("Invalid recorded field");
                        frame.Fields[i]=new RecordedField{Id=id,State=f};
                    }
                    result.FieldFrames[n]=frame;
                }
                if(fieldFrameCount>0&&(result.FieldFrames[0].Time>result.Start+.001f||result.FieldFrames[fieldFrameCount-1].Time<result.End-.001f))throw new InvalidDataException("Incomplete field coverage");
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
        private static void WriteColour(BinaryWriter w,Color c){w.Write(c.r);w.Write(c.g);w.Write(c.b);w.Write(c.a);}
        private static Color ReadColour(BinaryReader r)
        {
            var c=new Color(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            if(!Finite(c.r)||!Finite(c.g)||!Finite(c.b)||!Finite(c.a)||c.r<0||c.g<0||c.b<0||c.r>4||c.g>4||c.b>4||c.a<0||c.a>1)throw new InvalidDataException("Invalid stroke colour");return c;
        }
        private static void Write(BinaryWriter w,Vector3 p){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
        private static void Write(BinaryWriter w,Quaternion p){w.Write(p.x);w.Write(p.y);w.Write(p.z);w.Write(p.w);}
        private static Vector3 ReadVector(BinaryReader r,float bound)
        {var v=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||Mathf.Abs(v.x)>bound||Mathf.Abs(v.y)>bound||Mathf.Abs(v.z)>bound)throw new InvalidDataException("Invalid recorded vector");return v;}
        private static Quaternion ReadRotation(BinaryReader r)
        {var v=new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());float length=v.x*v.x+v.y*v.y+v.z*v.z+v.w*v.w;if(!Finite(length)||length<.5f||length>1.5f)throw new InvalidDataException("Invalid recorded rotation");return v;}
    }
}
