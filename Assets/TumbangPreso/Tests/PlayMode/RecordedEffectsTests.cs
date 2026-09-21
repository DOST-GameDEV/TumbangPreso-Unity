using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class RecordedEffectsTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        private static void Hero(CharacterMotor actor,string id)
        {
            actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,id);var art=RosterBook.Load().People.First(p=>p.Id==id);
            actor.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            actor.AbilitySystem.BindHero(id);actor.AbilitySystem.Kit.AddUltimateCharge(100);
        }
        private static AbilityContext Context(CharacterMotor actor)=>new AbilityContext(actor,actor.GetComponent<Carrier>(),actor.GetComponent<CombatVerbs>());
        [UnityTest]
        public IEnumerator RecordedHeroVisualsCannotCreateHazardsActorsScoresOrGameplayRandomness()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            var round=GameServices.Round;var dante=round.PlayerAt(1);var nemu=round.PlayerAt(2);var witch=round.PlayerAt(3);
            Hero(dante,"dante");Hero(nemu,"nemu");Hero(witch,"phaister");yield return null;
            var source=dante.GetComponent<CharacterVisual>().Model;string beforeKey=MatchReplayArchive.VisualKey(source);
            int beforeBones=MatchPoseHistory.StableTransforms(source).Length;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,dante.AbilitySystem.Kit.CastSkill2(Context(dante)));
            Assert.AreEqual(HeroKit.CastOutcome.Cast,nemu.AbilitySystem.Kit.CastUltimate(Context(nemu)));
            Assert.AreEqual(HeroKit.CastOutcome.Cast,witch.AbilitySystem.Kit.CastUltimate(Context(witch)));
            yield return new WaitForSeconds(.6f);
            DanteSeismicVisual.Impact(new Vector3(2,0,2),Vector3.forward,4,true);
            FrostSurfacePresentation.Nova(new Vector3(-2,0,2),4);yield return null;
            Assert.AreEqual(beforeKey,MatchReplayArchive.VisualKey(source),"Temporary armor cannot invalidate the stable rig identity");
            Assert.AreEqual(beforeBones,MatchPoseHistory.StableTransforms(source).Length);
            var fields=RecordedSpecialFields.Capture();
            foreach(var kind in new[]{RecordedSpecialFields.Coven,RecordedSpecialFields.Kuro,RecordedSpecialFields.Seismic,RecordedSpecialFields.Nova,RecordedSpecialFields.Ward})
                Assert.IsTrue(fields.Any(f=>f.Type==kind),"Missing actual effect "+kind);
            var stage=new GameObject("RecordedEffectsProof");stage.SetActive(false);
            var track=new MatchPoseHistory.Track(dante,source);track.Record(Time.time);track.Record(Time.time+.05f);
            var body=track.Clone(stage.transform);Assert.IsNotNull(body);track.Apply(body,track.Newest);stage.SetActive(true);
            int score=GameServices.Match.ScoreFor(1),hazards=Object.FindObjectsByType<HazardVolume>().Length,actors=Object.FindObjectsByType<CharacterMotor>().Length,pets=Object.FindObjectsByType<GhostPetCompanion>().Length;
            var random=Random.state;
            var views=new System.Collections.Generic.List<RecordedFieldView>();
            try
            {
                foreach(var field in fields)
                {
                    Assert.IsTrue(RecordedSpecialFields.Valid(field));
                    var view=new RecordedFieldView(stage.transform,field,field.Type==RecordedSpecialFields.Ward?body.Root:null);views.Add(view);
                    view.Sample(field,0);view.Visible(true);
                    Assert.IsTrue(view.Root.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"No recorded collider may enter physics, even in its construction frame");
                    Assert.IsEmpty(view.Root.GetComponentsInChildren<HazardVolume>(true));
                    Assert.AreEqual(hazards,Object.FindObjectsByType<HazardVolume>().Length);
                    Assert.AreEqual(actors,Object.FindObjectsByType<CharacterMotor>().Length);
                    Assert.AreEqual(pets,Object.FindObjectsByType<GhostPetCompanion>().Length);
                    Assert.AreEqual(score,GameServices.Match.ScoreFor(1));Assert.AreEqual(random,Random.state);
                    Assert.AreEqual(fields.Count,RecordedSpecialFields.Capture().Count,"Render copies cannot enter the live effect capture registry");
                }
                yield return null;
                foreach(var view in views)Assert.IsEmpty(view.Root.GetComponentsInChildren<Collider>(true),"Deferred primitive collider removal must finish before playback continues");
            }
            finally{foreach(var view in views)view.Dispose();}
            Object.Destroy(stage);
        }
        [Test]
        public void FastRecordedMotionInterpolatesButAuthoritativeTeleportsDoNot()
        {
            RecordedPoseTrack.Sample Pose(float at,float x,int epoch)=>new RecordedPoseTrack.Sample{Time=at,Epoch=epoch,
                Positions=new[]{new Vector3(x,0,0)},Rotations=new[]{Quaternion.identity},Scales=new[]{Vector3.one},Active=new[]{true}};
            var root=new GameObject("RecordedMotionProof");
            try
            {
                var a=Pose(0,0,1);var b=Pose(.05f,4,1);var pose=new RecordedPoseTrack(new[]{""},new[]{a,b});
                pose.Apply(pose.Bind(root),.025f);Assert.AreEqual(2,root.transform.position.x,.001f,"Fast dash is motion, not a teleport");
                b.Epoch=2;pose.Apply(pose.Bind(root),.025f);Assert.AreEqual(0,root.transform.position.x,.001f);
                pose.Apply(pose.Bind(root),.05f);Assert.AreEqual(4,root.transform.position.x,.001f);
                a.Epoch=b.Epoch=-1;a.State=b.State=(int)SlipperState.InFlight;
                pose.Apply(pose.Bind(root),.025f);Assert.AreEqual(2,root.transform.position.x,.001f,"A fast projectile must not stutter between samples");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
