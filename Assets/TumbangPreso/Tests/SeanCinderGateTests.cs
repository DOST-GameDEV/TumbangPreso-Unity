using System;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class SeanCinderGateTests
    {
        private static WorldEffectSnapshot.Field Field() => new WorldEffectSnapshot.Field {
            Type=WorldEffectSnapshot.Kind.CinderGate,Owner=0,EventId=1901,Position=Vector3.zero,
            Forward=Vector3.forward,Duration=SeanGateRules.TotalSeconds,Remaining=SeanGateRules.TotalSeconds,
            Radius=SeanGateRules.HalfWidth,Path=Array.Empty<Vector3>() };
        [TearDown] public void Clear()
        { foreach(var field in Object.FindObjectsByType<SeanCinderGate>(FindObjectsSortMode.None))Object.DestroyImmediate(field.gameObject); }
        private static RecordedMatchClip Clip(WorldEffectSnapshot.Field start,WorldEffectSnapshot.Field end)
        {
            RecordedPoseTrack.Sample Pose(float time)=>new RecordedPoseTrack.Sample{Time=time,Epoch=1,
                Positions=new[]{Vector3.zero},Rotations=new[]{Quaternion.identity},Scales=new[]{Vector3.one},Active=new[]{true}};
            var environment=RecordedEnvironment.Capture();
            return new RecordedMatchClip{MatchId=1,Id=1,Round=1,Actor=0,Subject=-1,Mode=GameMode.HeroStrike,
                Map=SceneFlow.BayanPlaza,Reason="Cinder witness",Start=0,End=1,Contact=.6f,
                Objects=new[]{new RecordedObjectTrack{Kind=RecordedObjectKind.Can,Seat=-1,Skin=-1,
                    Pose=new RecordedPoseTrack(new[]{""},new[]{Pose(0),Pose(1)})}},
                FieldFrames=new[]{new RecordedFieldFrame{Time=0,Lighting=environment,Fields=new[]{new RecordedField{Id=1,State=start}}},
                    new RecordedFieldFrame{Time=1,Lighting=environment,Fields=new[]{new RecordedField{Id=1,State=end}}}}};
        }
        [Test] public void CinderRecordingPreservesConsumedDirection()
        {
            var before=Field();var after=before;after.Remaining-=1;after.Split=true;after.FirstScale=.6f;after.SecondScale=-1;
            Assert.IsTrue(RecordedMatchClip.TryDecode(Clip(before,after).Encode(),out var clip,out var error),error);
            var state=clip.FieldFrames[1].Fields[0].State;
            Assert.AreEqual(WorldEffectSnapshot.Kind.CinderGate,state.Type);Assert.AreEqual(before.EventId,state.EventId);
            Assert.IsTrue(state.Split);Assert.AreEqual(-1,state.SecondScale);Assert.AreEqual(.6f,state.FirstScale,.001f);
            Assert.IsEmpty(state.Path);
        }

        [Test] public void VersionElevenWaterRecordingRemainsReadable()
        {
            var before=Field();before.Type=WorldEffectSnapshot.Kind.Waterwall;before.Duration=4;before.Remaining=4;before.Radius=2;
            var after=before;after.Remaining=3;
            byte[] encoded=Clip(before,after).Encode();
            using var source=new System.IO.MemoryStream(encoded);
            using var expanded=new System.IO.MemoryStream();
            using(var unzip=new System.IO.Compression.DeflateStream(source,System.IO.Compression.CompressionMode.Decompress,true))unzip.CopyTo(expanded);
            expanded.Position=4;using(var writer=new System.IO.BinaryWriter(expanded,System.Text.Encoding.UTF8,true))writer.Write(11);
            expanded.Position=0;using var packed=new System.IO.MemoryStream();
            using(var zip=new System.IO.Compression.DeflateStream(packed,System.IO.Compression.CompressionLevel.Fastest,true))expanded.CopyTo(zip);
            Assert.IsTrue(RecordedMatchClip.TryDecode(packed.ToArray(),out var clip,out var error),error);
            Assert.AreEqual(WorldEffectSnapshot.Kind.Waterwall,clip.FieldFrames[0].Fields[0].State.Type);
        }
        [Test] public void ConcreteDefendingSlotUsesOneWarningAndSharedFieldIdentity()
        {
            var kit=new SeanHeroKit();
            Assert.AreEqual("sean_skill2d",kit.DefendingSkill.Id);
            Assert.AreEqual("CINDER GATE",kit.DefendingSkill.Name);
            Assert.AreEqual(35,kit.DefendingSkill.Cooldown);
            Assert.AreEqual(3.35f,kit.DefendingSkill.Duration,.0001f);
            Assert.AreEqual(0,kit.DefendingSkill.Windup);
            Assert.AreEqual(AbilityNetworkMode.HostConfirmed,kit.DefendingSkill.NetworkMode);
            Assert.AreEqual(16,(int)WorldEffectSnapshot.Kind.Waterwall);
            Assert.AreEqual(17,(int)WorldEffectSnapshot.Kind.CinderGate);
            Assert.IsTrue(WorldEffectSnapshot.UsesDynamicIdentity(WorldEffectSnapshot.Kind.CinderGate));
            Assert.IsTrue(WorldEffectSnapshot.UsesDynamicIdentity(WorldEffectSnapshot.Kind.Waterwall));
        }
        [Test] public void FieldValidationRejectsMalformedOrUntruthfulState()
        {
            var field=Field();Assert.IsTrue(WorldEffectSnapshot.Valid(field));
            field.Position=new Vector3(float.NaN,0,0);Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Field();field.Forward=Vector3.up;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Field();field.Remaining=4;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Field();field.SecondScale=1;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Field();field.Split=true;field.FirstScale=.6f;field.SecondScale=-1;Assert.IsTrue(WorldEffectSnapshot.Valid(field));
            field.SecondScale=2;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Field();field.EventId=0;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
        }
        [Test] public void RepeatedOrOlderFieldUpdatesCannotRewindOrReviveConsumedGate()
        {
            var state=Field();var gate=SeanCinderGate.Restore(state,.5f);Assert.IsNotNull(gate);
            float left=gate.Remaining;
            Assert.AreSame(gate,SeanCinderGate.Restore(state,.1f));Assert.AreEqual(left,gate.Remaining,.0001f);
            state.Split=true;state.FirstScale=.6f;state.SecondScale=1;state.Remaining=state.Duration-.6f;
            Assert.AreSame(gate,SeanCinderGate.Restore(state,0));Assert.IsTrue(gate.Spent);Assert.AreEqual(0,gate.ActiveRemaining);
            Assert.AreSame(gate,SeanCinderGate.Restore(Field(),0));Assert.IsTrue(gate.Spent);
            Assert.LessOrEqual(gate.Remaining,state.Remaining+.0001f);
            Assert.AreEqual(1,SeanCinderGate.Active.Count);
            Assert.IsNull(SeanCinderGate.Restore(Field(),4));
            Assert.IsEmpty(gate.GetComponentsInChildren<Collider>());
        }
    }
}
