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
        [UnityTest]
        public IEnumerator EveryShippedHeroCanRetainARealExchangeDuringTheirUltimate()
        {
            foreach(string hero in new[]{"sean","phaister","zack","nemu","dante","cheska"})
            {
                yield return PlayModeWorld.Reset();
                yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
                var round=GameServices.Round;var caster=round.PlayerAt(1);Hero(caster,hero);yield return null;
                foreach(var actor in round.Players)actor.Teleport(new Vector3(7,.12f,-6+actor.PlayerSlot*3));
                caster.Teleport(new Vector3(0,.12f,-4));caster.transform.forward=Vector3.forward;
                yield return new WaitForSeconds(2.7f);
                Assert.AreEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.Kit.CastUltimate(Context(caster)),hero+" real ultimate was refused");
                yield return new WaitForSeconds(.2f);
                var scorer=round.PlayerAt(3);var shoe=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.SeatOfOrigin==3);
                int serial=round.Lata.HostKnockdownSerial;
                shoe.HostThrow(scorer,round.Lata.transform.position+new Vector3(0,1.2f,-2),Vector3.forward*12);
                float until=Time.time+2;while(round.Lata.IsUpright&&Time.time<until)yield return null;
                Assert.AreEqual(serial+1,round.Lata.HostKnockdownSerial,hero+" contact did not resolve");
                yield return new WaitForSeconds(1.6f);
                var archive=Object.FindAnyObjectByType<MatchReplayArchive>();Assert.Greater(archive.Clips.Count,0,hero+": "+archive.LastSkip);
                Assert.IsTrue(RecordedMatchClip.TryDecode(archive.Clips[0].Bytes,out var clip,out var error),hero+": "+error);
                if(hero=="nemu")Assert.IsTrue(clip.Objects.Any(o=>o.Kind==RecordedObjectKind.Familiar&&o.Seat==1),"Kuro must be in Nemu's clip");
                using(var view=new RecordedWorldView(archive.transform,clip))
                {
                    Assert.IsTrue(view.Ready,hero+": "+view.UnavailableReason);int score=GameServices.Match.ScoreFor(3);
                    view.Draw(clip.Contact,false);view.Draw(clip.End,false);Assert.AreEqual(score,GameServices.Match.ScoreFor(3));
                }
                Debug.Log("[HeroReplay] "+hero+" bytes="+archive.Clips[0].Bytes.Length+" objects="+clip.Objects.Length+" max-fields="+clip.FieldFrames.Max(f=>f.Fields.Length));
            }
        }

        [UnityTest]
        public IEnumerator CloudClockMovesPausesAndRestoresAcrossReverseReplayAndFailure()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            Assert.IsNotNull(Object.FindAnyObjectByType<NeighbourhoodSkyMotion>());
            Assert.Greater(RenderSettings.skybox.GetFloat("_CloudSpeed"),0);
            var skyCamera=new GameObject("Cloud render proof").AddComponent<Camera>();skyCamera.enabled=false;
            skyCamera.clearFlags=CameraClearFlags.Skybox;skyCamera.cullingMask=0;skyCamera.transform.rotation=Quaternion.Euler(-24,35,0);
            var target=new RenderTexture(256,128,16);target.Create();skyCamera.targetTexture=target;
            var read=new Texture2D(256,128,TextureFormat.RGB24,false);var active=RenderTexture.active;
            Color32[] At(float seconds)
            {
                using(var sample=NeighbourhoodSkyMotion.At(seconds))skyCamera.Render();
                RenderTexture.active=target;read.ReadPixels(new Rect(0,0,256,128),0,0);read.Apply();return read.GetPixels32();
            }
            try
            {
                var initial=At(0);var moved=At(600);var rewind=At(0);
                Assert.Greater(initial.Where((pixel,i)=>!pixel.Equals(moved[i])).Count(),initial.Length/20,"The actual sky shader did not drift");
                CollectionAssert.AreEqual(initial,rewind,"Reverse seeking did not reproduce the same cloud frame");
            }
            finally{RenderTexture.active=active;skyCamera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(read);Object.Destroy(skyCamera.gameObject);}
            float start=Shader.GetGlobalFloat("_TumpSkyTime");yield return new WaitForSeconds(.15f);
            Assert.Greater(Shader.GetGlobalFloat("_TumpSkyTime"),start+.05f);
            Time.timeScale=0;yield return null;float frozen=Shader.GetGlobalFloat("_TumpSkyTime");
            try
            {
                yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(frozen,Shader.GetGlobalFloat("_TumpSkyTime"));
                try
                {
                    using(var forward=NeighbourhoodSkyMotion.At(57))
                    {
                        Assert.AreEqual(57,Shader.GetGlobalFloat("_TumpSkyTime"));
                        using(var rewind=NeighbourhoodSkyMotion.At(12))Assert.AreEqual(12,Shader.GetGlobalFloat("_TumpSkyTime"));
                        Assert.AreEqual(57,Shader.GetGlobalFloat("_TumpSkyTime"));
                        throw new System.InvalidOperationException("intentional sky-render failure");
                    }
                }
                catch(System.InvalidOperationException error){Assert.AreEqual("intentional sky-render failure",error.Message);}
                Assert.AreEqual(frozen,Shader.GetGlobalFloat("_TumpSkyTime"));
            }
            finally{Time.timeScale=1;}
            yield return new WaitForSeconds(.1f);Assert.Greater(Shader.GetGlobalFloat("_TumpSkyTime"),frozen);
        }

        [UnityTest]
        public IEnumerator RecordedWeatherRestoresTheLiveWorldEvenWhenRenderingFails()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            SkyEvent.Play(SkyEvent.Look.Eclipse,8);yield return new WaitForSeconds(.2f);
            var before=RecordedEnvironment.Capture();var savedSky=RenderSettings.skybox;
            Assert.IsTrue(before.HasExposure&&before.HasTint,"The authored cloud sky must participate in weather/replay tint and exposure.");
            var copy=savedSky!=null?new Material(savedSky):null;
            var root=new GameObject("RecordedWeatherProof");var camera=root.AddComponent<Camera>();camera.enabled=false;
            var grade=root.AddComponent<ColourGrade>();grade.AdoptFromScene();var fillRoot=new GameObject("RecordedFillProof");var fill=fillRoot.AddComponent<Light>();fill.enabled=false;
            var recorded=before;recorded.Sky=Color.red;recorded.FogColour=Color.blue;recorded.FillColour=Color.magenta;recorded.FillOn=true;
            recorded.SkyTint=new Color(.24f,.33f,.46f,1);recorded.Exposure=.43f;
            try
            {
                try
                {
                    using(recorded.Use(grade,copy,fill))
                    {
                        Assert.AreEqual(Color.red,RenderSettings.ambientSkyColor);Assert.IsTrue(fill.enabled);
                        SameTint(recorded.SkyTint,RenderSettings.skybox.GetColor("_Tint"),"recorded sky");
                        Assert.AreEqual(recorded.Exposure,RenderSettings.skybox.GetFloat("_Exposure"));
                        throw new System.InvalidOperationException("intentional renderer failure");
                    }
                }
                catch(System.InvalidOperationException failure){Assert.AreEqual("intentional renderer failure",failure.Message);}
                Assert.AreEqual(before.Sky,RenderSettings.ambientSkyColor);Assert.AreEqual(before.FogColour,RenderSettings.fogColor);
                Assert.AreSame(savedSky,RenderSettings.skybox);Assert.IsFalse(fill.enabled);
                SameTint(before.SkyTint,savedSky.GetColor("_Tint"),"restored live sky");Assert.AreEqual(before.Exposure,savedSky.GetFloat("_Exposure"));
                if(SkyEvent.RecordedFill!=null)Assert.AreEqual(before.FillOn,SkyEvent.RecordedFill.enabled);
                using var stream=new System.IO.MemoryStream();using(var writer=new System.IO.BinaryWriter(stream,System.Text.Encoding.UTF8,true))recorded.Write(writer);
                stream.Position=0;using var reader=new System.IO.BinaryReader(stream);var decoded=RecordedEnvironment.Read(reader);
                Assert.AreEqual(recorded.Sky,decoded.Sky);Assert.AreEqual(recorded.FillPosition,decoded.FillPosition);
                Assert.AreEqual(recorded.SkyTint,decoded.SkyTint);Assert.AreEqual(recorded.Exposure,decoded.Exposure);
            }
            finally{Object.Destroy(root);Object.Destroy(fillRoot);if(copy!=null)Object.Destroy(copy);SkyEvent.StopAll();}
            // Material color conversion can round by a few float ULPs. Require
            // sub-millionth agreement and record the actual delta, not 3-digit text.
            void SameTint(Color expected,Color actual,string stage)
            {
                float error=Vector4.Distance(expected,actual);
                Debug.Log($"[Recorded sky tint] {stage} delta={error:R}; expected=({expected.r:R},{expected.g:R},{expected.b:R}); actual=({actual.r:R},{actual.g:R},{actual.b:R})");
                Assert.Less(error,.000001f,stage+" tint was not restored");
            }
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
