using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RecordedSeismicBoundaryTests
    {
        [UnitySetUp] public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After()=>PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator ActualZeroRadiusFissureWarningSurvivesTheStrictCodecAndRenderOnlyView()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,GameMode.HeroStrike);
            var actor=GameServices.Round.PlayerAt(1);
            actor.AbilitySystem.BindHero("dante");
            var power=actor.AbilitySystem.Kit.Ultimate;
            Assert.AreEqual(0,power.TelegraphRadius,"The authored forward fissure has no radial telegraph.");
            Assert.Greater(power.Windup,.05f);
            DanteSeismicVisual.Warn(actor,power,actor.transform.position,Vector3.forward,power.TelegraphRadius,true);
            var field=RecordedSpecialFields.Capture().Single(f=>f.Type==RecordedSpecialFields.Seismic);
            Assert.AreEqual(0,field.Radius);Assert.IsTrue(field.Split);Assert.AreEqual(1,field.FirstScale);
            Assert.IsTrue(RecordedSpecialFields.Valid(field),"The real accepted-cast feedback shape must be recordable.");
            Assert.IsTrue(RecordedMatchClip.TryDecode(Clip(field).Encode(),out var decoded,out var error),error);
            var restored=decoded.FieldFrames[0].Fields[0].State;
            Assert.AreEqual(0,restored.Radius);Assert.IsTrue(restored.Split);
            int players=GameServices.Round.Players.Count();
            var scores=GameServices.Round.Players.Select(a=>GameServices.Match.ScoreFor(a.PlayerSlot)).ToArray();
            var random=Random.state;var stage=new GameObject("Zero-radius recorded fissure proof");
            try
            {
                using(var view=new RecordedFieldView(stage.transform,restored))
                {
                    view.Sample(restored,.05f);
                    Assert.Greater(view.Root.GetComponentsInChildren<MeshFilter>(true).Length,0);
                    var behaviours=view.Root.GetComponentsInChildren<MonoBehaviour>(true);
                    Assert.IsTrue(behaviours.Where(b=>b.enabled).All(b=>b is VfxRenderTag || b is VfxShapes.GeneratedMeshOwner),
                        "Only render mesh/material ownership may remain enabled: "+string.Join(",",behaviours.Where(b=>b.enabled).Select(b=>b.GetType().Name)));
                    Assert.IsFalse(behaviours.OfType<DanteSeismicVisual>().Any(b=>b.enabled));
                    Assert.IsFalse(view.Root.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled));
                    Assert.AreEqual(1,RecordedSpecialFields.Capture().Count(f=>f.Type==RecordedSpecialFields.Seismic));
                }
                Assert.AreEqual(players,GameServices.Round.Players.Count());
                Assert.IsTrue(scores.SequenceEqual(GameServices.Round.Players.Select(a=>GameServices.Match.ScoreFor(a.PlayerSlot))));
                Assert.AreEqual(random,Random.state);
            }
            finally { Object.DestroyImmediate(stage); }
        }

        [UnityTest]
        public IEnumerator ActualPositiveRadiusStompStillRoundTrips()
        {
            DanteSeismicVisual.Impact(Vector3.zero,Vector3.forward,2.2f,false);
            var field=RecordedSpecialFields.Capture().Single(f=>f.Type==RecordedSpecialFields.Seismic);
            Assert.AreEqual(2.2f,field.Radius);Assert.IsFalse(field.Split);
            Assert.IsTrue(RecordedMatchClip.TryDecode(Clip(field).Encode(),out var decoded,out var error),error);
            Assert.AreEqual(2.2f,decoded.FieldFrames[0].Fields[0].State.Radius);
            yield return null;
        }

        [TestCase(-1f,true)]
        [TestCase(0f,false)]
        [TestCase(16f,true)]
        [TestCase(float.NaN,true)]
        public void StrictCodecStillRejectsInvalidRadialState(float radius,bool fissure)
        {
            var field=new WorldEffectSnapshot.Field{Type=RecordedSpecialFields.Seismic,Position=Vector3.zero,
                Forward=Vector3.forward,Duration=1,Remaining=1,Radius=radius,FirstScale=1,SecondScale=0,Owner=-1,Split=fissure};
            var error=Assert.Throws<InvalidDataException>(()=>Clip(field).Encode());
            StringAssert.Contains("Invalid recorded field",error.Message);
        }

        private static RecordedMatchClip Clip(WorldEffectSnapshot.Field field)
        {
            RecordedPoseTrack.Sample Pose(float time)=>new RecordedPoseTrack.Sample{Time=time,Epoch=1,
                Positions=new[]{Vector3.zero},Rotations=new[]{Quaternion.identity},Scales=new[]{Vector3.one},Active=new[]{true}};
            var end=field;end.Remaining=Mathf.Max(0,end.Remaining-.5f);
            return new RecordedMatchClip{MatchId=1,Id=1,Round=1,Actor=1,Subject=-1,Mode=GameMode.HeroStrike,
                Map=SceneFlow.Eskinita,Reason="Fissure boundary witness",Start=0,End=.5f,Contact=.25f,
                Objects=new[]{new RecordedObjectTrack{Kind=RecordedObjectKind.Can,Seat=-1,Skin=-1,
                    Pose=new RecordedPoseTrack(new[]{""},new[]{Pose(0),Pose(.5f)})}},
                FieldFrames=new[]{new RecordedFieldFrame{Time=0,Lighting=RecordedEnvironment.Capture(),Fields=new[]{new RecordedField{Id=1,State=field}}},
                    new RecordedFieldFrame{Time=.5f,Lighting=RecordedEnvironment.Capture(),Fields=new[]{new RecordedField{Id=1,State=end}}}}};
        }
    }
}
