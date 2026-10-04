using System;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using UnityEngine;
namespace TumbangPreso.Tests
{
    public sealed class RafiBahaTests
    {
        private static WorldEffectSnapshot.Field Field(float distance=12)
        {
            var path=new Vector3[9];for(int i=0;i<9;i++)path[i]=new Vector3(Mathf.Lerp(-3,3,i/8f),0,distance);
            return new WorldEffectSnapshot.Field{Type=WorldEffectSnapshot.Kind.Baha,Owner=1,EventId=1902,Position=Vector3.zero,
                Forward=Vector3.forward,Radius=3,FirstScale=5,SecondScale=distance,Duration=RafiRules.BahaDuration(distance),
                Remaining=RafiRules.BahaDuration(distance),Path=path};
        }
        [Test] public void NewKindHasAnExplicitBoundAndTheAdoptedCost()
        {
            Assert.AreEqual(18,(int)WorldEffectSnapshot.Kind.Baha);Assert.IsTrue(WorldEffectSnapshot.Valid(Field()));
            Assert.IsTrue(WorldEffectSnapshot.Valid(Field(64)));Assert.IsFalse(WorldEffectSnapshot.Valid(Field(65)));
            Assert.AreEqual(15,new RafiHeroKit().UltimateCost);Assert.AreEqual(.8f,RafiWaterField.Gather(WorldEffectSnapshot.Kind.Baha));
            Assert.AreEqual(.55f,RafiWaterField.Gather(WorldEffectSnapshot.Kind.Breakwater));
        }
        [Test] public void LaneForgeryAndLifetimeMismatchAreRejected()
        {
            var field=Field();field.Path[3]+=Vector3.right;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Field();field.Path[3]+=Vector3.forward;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Field();field.Duration+=.1f;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Field();field.Split=true;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
        }
        [Test] public void LegacyBreakwaterKeepsItsOldLimits()
        {
            var field=Field(8);field.Type=WorldEffectSnapshot.Kind.Breakwater;field.SecondScale=0;field.Duration=field.Remaining=2.15f;
            Assert.IsTrue(WorldEffectSnapshot.Valid(field));field.Duration=field.Remaining=5;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
        }
        private static RecordedMatchClip Clip(WorldEffectSnapshot.Field field)
        {
            RecordedPoseTrack.Sample Pose(float time)=>new RecordedPoseTrack.Sample{Time=time,Epoch=1,
                Positions=new[]{Vector3.zero},Rotations=new[]{Quaternion.identity},Scales=new[]{Vector3.one},Active=new[]{true}};
            var end=field;end.Remaining-=1;
            return new RecordedMatchClip{MatchId=1,Id=1,Round=1,Actor=1,Subject=-1,Mode=GameMode.HeroStrike,
                Map=SceneFlow.BayanPlaza,Reason="Baha witness",Start=0,End=1,Contact=.8f,
                Objects=new[]{new RecordedObjectTrack{Kind=RecordedObjectKind.Can,Seat=-1,Skin=-1,
                    Pose=new RecordedPoseTrack(new[]{""},new[]{Pose(0),Pose(1)})}},
                FieldFrames=new[]{new RecordedFieldFrame{Time=0,Lighting=RecordedEnvironment.Capture(),Fields=new[]{new RecordedField{Id=1,State=field}}},
                    new RecordedFieldFrame{Time=1,Lighting=RecordedEnvironment.Capture(),Fields=new[]{new RecordedField{Id=1,State=end}}}}};
        }
        private static byte[] WithVersion(byte[] bytes,int version)
        {
            using var source=new System.IO.MemoryStream(bytes);using var raw=new System.IO.MemoryStream();
            using(var unzip=new System.IO.Compression.DeflateStream(source,System.IO.Compression.CompressionMode.Decompress,true))unzip.CopyTo(raw);
            raw.Position=4;using(var writer=new System.IO.BinaryWriter(raw,System.Text.Encoding.UTF8,true))writer.Write(version);
            raw.Position=0;using var output=new System.IO.MemoryStream();
            using(var zip=new System.IO.Compression.DeflateStream(output,System.IO.Compression.CompressionLevel.Fastest,true))raw.CopyTo(zip);
            return output.ToArray();
        }
        [Test] public void BahaRecordingRoundtripKeepsRangeAndRejectsAnOlderSchema()
        {
            var clip=Clip(Field());var bytes=clip.Encode();
            Assert.IsTrue(RecordedMatchClip.TryDecode(bytes,out var restored,out var error),error);
            Assert.AreEqual(18,(int)restored.FieldFrames[0].Fields[0].State.Type);
            Assert.AreEqual(12,restored.FieldFrames[0].Fields[0].State.SecondScale);
            bytes=WithVersion(bytes,12);
            Assert.IsFalse(RecordedMatchClip.TryDecode(bytes,out _,out _));
        }
        [Test] public void UnsupportedSchemaIsReportedAsARefusalInsteadOfEscaping()
        {
            var bytes=WithVersion(Clip(Field()).Encode(),99);
            Assert.IsFalse(RecordedMatchClip.TryDecode(bytes,out var clip,out var error));
            Assert.IsNull(clip);StringAssert.Contains("Unsupported clip schema",error);
        }
        [Test] public void PreviousSchemaStillReadsLegacyWater()
        {
            var field=Field(8);field.Type=WorldEffectSnapshot.Kind.Breakwater;field.SecondScale=0;field.Duration=field.Remaining=2.15f;
            var bytes=Clip(field).Encode();bytes=WithVersion(bytes,12);
            Assert.IsTrue(RecordedMatchClip.TryDecode(bytes,out var restored,out var error),error);
            Assert.AreEqual(WorldEffectSnapshot.Kind.Breakwater,restored.FieldFrames[0].Fields[0].State.Type);
        }
    }
}
