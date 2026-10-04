using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.CameraSystem;
using TumbangPreso.Net;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SeanCinderGateProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat,_quality,_graphics,_idle;
        private INetProvider _provider;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _provider=NetAuthority.Provider;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            _quality=QualitySettings.GetQualityLevel();_graphics=GraphicsProfiles.Current;
#if UNITY_EDITOR
            _idle=UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds;
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=1;
#endif
            int low=System.Array.IndexOf(QualitySettings.names,"Low");if(low>=0)QualitySettings.SetQualityLevel(low,true);
            yield return PlayModeWorld.Reset();yield return new WaitForSecondsRealtime(1);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_provider;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
            QualitySettings.SetQualityLevel(_quality,true);GraphicsProfiles.Apply(_graphics);
#if UNITY_EDITOR
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=_idle;
#endif
        }
        private static AbilityContext Context(CharacterMotor actor)
            =>new AbilityContext(actor,actor.GetComponent<Carrier>(),actor.GetComponent<CombatVerbs>());
        private static IEnumerator Start()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider=new SoloProvider();GraphicsProfiles.Apply(0);
            Object.FindFirstObjectByType<TumbangPreso.CameraSystem.CameraRig>().SetAimSource(TumbangPreso.CameraSystem.AimSource.Movement);
            Assert.IsTrue(GameServices.Round.RoundActive);
            foreach(var actor in GameServices.Round.Players)
            {actor.Intent.Clear();actor.Intent.Parked=true;actor.Teleport(new Vector3(7,.12f,7+actor.PlayerSlot));}
            var caster=GameServices.Round.Players.First(p=>p.IsDefender);
            caster.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"sean");caster.AbilitySystem.BindHero("sean");
            var art=RosterBook.Load().FindPersonArt("sean");Assert.IsTrue(art.Clips.Any(c=>c!=null&&c.name=="hero-sean-gate"));
            caster.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            caster.Teleport(new Vector3(-3,.12f,-5));caster.transform.rotation=Quaternion.identity;
            caster.Intent.AimPoint=new Vector3(-3,.12f,-2);caster.AbilitySystem.Kit.SetRole(true,Context(caster));
            for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
        }
        private static SeanCinderGate Cast()
        {
            var caster=GameServices.Round.Players.First(p=>p.IsDefender);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.Kit.CastSkill2(Context(caster)));
            Assert.AreEqual(35,caster.AbilitySystem.Kit.DefendingSkill.CooldownRemaining,.05f);
            Assert.AreEqual(AbilityGlyph.SeanCinderGate,caster.AbilitySystem.Kit.DefendingSkill.Glyph);
            Assert.AreEqual("CROSSING TRAP",AbilityIcons.LabelFor(AbilityGlyph.SeanCinderGate));
            return SeanCinderGate.Active.Single();
        }
        [UnityTest,Timeout(90000)] public IEnumerator OneGroundedCrossingPushesBackWithoutScoringAndThenExpires()
        {
            yield return Start();var rival=GameServices.Round.PlayerAt(1);
            rival.Teleport(new Vector3(-3,.12f,-2.65f));rival.Intent.Parked=false;
            for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
            var gate=Cast();Assert.IsFalse(gate.Spent);Assert.IsEmpty(gate.GetComponentsInChildren<Collider>());
            Assert.Greater(gate.Remaining,3.2f);
            yield return new WaitForSeconds(.42f);Assert.IsFalse(gate.Spent);
            var scores=new System.Collections.Generic.List<ScoreEvent>();
            System.Action<int,ScoreEvent> record=(slot,kind)=>scores.Add(kind);
            var match=GameServices.Match;match.Scored+=record;
            try
            {
            rival.Intent.Move=Vector2.up;
            float end=Time.time+1.2f;
            while(gate!=null&&!gate.Spent&&Time.time<end)yield return new WaitForFixedUpdate();
            Assert.IsNotNull(gate);Assert.IsTrue(gate.Spent,"Real grounded movement never consumed the line.");
            rival.Intent.Move=Vector2.zero;
            Assert.Less(rival.PresentationTravelVelocity.z,0,"Crossing from the negative side must push back toward that side.");
            Assert.AreEqual(0,GameServices.Round.Players.First(p=>p.IsDefender).AbilitySystem.Kit.DefendingSkill.DurationRemaining,.05f);
            Assert.IsTrue(scores.All(kind=>kind==ScoreEvent.DefenseTick),"Gate crossing must not award points or penalties; ordinary defence income remains valid.");
            var later=GameServices.Round.PlayerAt(2);later.Teleport(new Vector3(-3,.12f,-2.6f));later.Intent.Parked=false;later.Intent.Move=Vector2.up;
            yield return new WaitForSeconds(.6f);Assert.Greater(later.transform.position.z,-2.2f,"Spent seam blocked a later rival.");
            yield return new WaitForSeconds(3f);Assert.IsTrue(gate==null||!gate.gameObject.activeInHierarchy);
            }
            finally{match.Scored-=record;}
        }
        [UnityTest,Timeout(90000)] public IEnumerator JumpAndFlyingSlipperPassAndRoleChangeRetiresTheField()
        {
            yield return Start();var rival=GameServices.Round.PlayerAt(1);rival.Teleport(new Vector3(-3,.12f,-2.7f));rival.Intent.Parked=false;
            for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
            var gate=Cast();yield return new WaitForSeconds(.42f);
            rival.Intent.Set(Verb.Jump,true);rival.Intent.Move=Vector2.up;
            yield return new WaitForSeconds(.12f);rival.Intent.Set(Verb.Jump,false);
            Assert.IsFalse(rival.IsGrounded,"The counterplay fixture did not actually jump.");
            yield return new WaitForSeconds(.35f);rival.Intent.Move=Vector2.zero;
            Assert.Greater(rival.transform.position.z,-2);Assert.IsFalse(gate.Spent,"An airborne crossing consumed the line.");
            var thrower=GameServices.Round.PlayerAt(2);var shoe=thrower.GetComponent<Carrier>().Held;
            shoe.HostThrow(thrower,new Vector3(-3,.9f,-2.2f),Vector3.forward*5);
            yield return new WaitForSeconds(.12f);Assert.AreEqual(SlipperState.InFlight,shoe.State);Assert.IsFalse(gate.Spent);
            var owner=GameServices.Round.Players.First(p=>p.IsDefender);owner.IsDefender=false;owner.AbilitySystem.Kit.SetRole(false,Context(owner));
            yield return null;yield return null;Assert.IsTrue(gate==null||!gate.gameObject.activeInHierarchy);
        }
        [UnityTest,Timeout(90000)] public IEnumerator CoverRefusesPlacementAndImmuneCrossingConsumesBeforeRoundCleanup()
        {
            yield return Start();var caster=GameServices.Round.Players.First(p=>p.IsDefender);
            var context=Context(caster);var point=new Vector3(-3,.12f,-2);
            Assert.IsTrue(SeanCinderGate.CanPlace(context,point));
            Assert.IsFalse(SeanCinderGate.CanPlace(context,new Vector3(float.NaN,0,0)));
            Assert.IsFalse(SeanCinderGate.CanPlace(context,point+Vector3.up*2));
            Assert.IsFalse(SeanCinderGate.CanPlace(context,point+Vector3.forward*20));
            var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.transform.position=new Vector3(-3,.7f,-3.5f);cover.transform.localScale=new Vector3(1,2,.3f);
            Physics.SyncTransforms();
            try
            {
                Assert.IsFalse(SeanCinderGate.CanPlace(context,point),"A seam cannot be placed through solid cover.");
                Assert.AreNotEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.Kit.CastSkill2(context));
                Assert.AreEqual(0,caster.AbilitySystem.Kit.DefendingSkill.CooldownRemaining);
                Assert.IsEmpty(SeanCinderGate.Active);
            }
            finally{Object.Destroy(cover);}
            yield return null;Physics.SyncTransforms();
            var rival=GameServices.Round.PlayerAt(1);rival.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"dante");rival.AbilitySystem.BindHero("dante");
            rival.AbilitySystem.Kit.SetRole(false,Context(rival));
            rival.Teleport(new Vector3(-3,.12f,-2.65f));rival.Intent.Parked=false;
            for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
            Assert.IsTrue(rival.AbilitySystem.Kit.TryActivateSkill1(Context(rival)));
            Assert.IsTrue(rival.AbilitySystem.IsImmuneToStuns);
            var gate=Cast();yield return new WaitForSeconds(.42f);
            rival.Intent.Move=Vector2.up;float end=Time.time+1.2f;
            while(gate!=null&&!gate.Spent&&Time.time<end)yield return new WaitForFixedUpdate();
            Assert.IsNotNull(gate);Assert.IsTrue(gate.Spent,"The immune body must still consume the one crossing.");
            Assert.GreaterOrEqual(rival.PresentationTravelVelocity.z,-.05f,"Status immunity must refuse the gate impulse.");
            rival.Intent.Move=Vector2.zero;
            GameServices.Round.EndRound();yield return null;
            Assert.IsEmpty(SeanCinderGate.Active,"Round retirement must remove the live field.");
            Assert.IsTrue(gate==null||!gate.gameObject.activeInHierarchy);
        }
        [UnityTest] public IEnumerator CinderVisualKeepsBoundaryAndDeterministicRetirement()
        {
            var root=new GameObject("Cinder visual contract");
            var state=new WorldEffectSnapshot.Field {Type=WorldEffectSnapshot.Kind.CinderGate,
                Owner=0,EventId=1,Position=Vector3.zero,Forward=Vector3.forward,
                Radius=SeanGateRules.HalfWidth,Duration=SeanGateRules.TotalSeconds,
                Remaining=SeanGateRules.TotalSeconds,Path=System.Array.Empty<Vector3>()};
            try
            {
                var view=SeanCinderVisual.Build(root.transform,state);
                var renderers=view.GetComponentsInChildren<Renderer>();
                Assert.AreEqual(5,renderers.Length,"Keep the same bounded five-renderer footprint.");
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
                var seam=view.transform.Find("Charcoal footprint");
                var mesh=seam.GetComponent<MeshFilter>().sharedMesh;
                Assert.AreEqual(-1,mesh.bounds.min.x,.0001f);Assert.AreEqual(1,mesh.bounds.max.x,.0001f);
                float age=SeanGateRules.WarningSeconds+.2f;view.StepTo(age);
                Assert.AreEqual(SeanGateRules.HalfWidth,seam.localScale.x,.0001f);
                var teeth=Enumerable.Range(0,3).Select(i=>view.transform.Find("Cinder pressure tooth "+i)).ToArray();
                var scales=teeth.Select(t=>t.localScale).ToArray();
                foreach(var tooth in teeth)Assert.Greater(tooth.GetComponent<Renderer>().bounds.size.x,.06f,
                    "Armed flames must have a readable silhouette rather than a needle-thin edge.");
                yield return null;view.StepTo(age);
                for(int i=0;i<teeth.Length;i++)Assert.AreEqual(scales[i],teeth[i].localScale);
                Assert.AreSame(mesh,seam.GetComponent<MeshFilter>().sharedMesh);
                state.Split=true;state.FirstScale=age;state.SecondScale=1;view.SetState(state);
                view.StepTo(age+.1f);Assert.Less(Mathf.DeltaAngle(0,teeth[1].localEulerAngles.x),0);
                view.StepTo(age+.2f);var block=new MaterialPropertyBlock();
                foreach(var renderer in renderers){renderer.GetPropertyBlock(block);Assert.Zero(block.GetColor("_Color").a);}
                Assert.IsEmpty(root.GetComponentsInChildren<SeanCinderGate>());
            }
            finally{Object.Destroy(root);}
        }

        [UnityTest,Timeout(180000)] public IEnumerator ActualCastShowsAuthoredBodyAndFieldWithoutReplayGameplay()
        {
            yield return Start();var caster=GameServices.Round.Players.First(p=>p.IsDefender);
            var rival=GameServices.Round.PlayerAt(1);rival.Teleport(new Vector3(-3,.12f,-2.65f));rival.Intent.Parked=false;
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(caster);rig.SetAimSource(AimSource.Movement);
            var camera=new GameObject("Cinder action witness").AddComponent<Camera>();camera.enabled=false;
            camera.fieldOfView=52;camera.nearClipPlane=.05f;camera.farClipPlane=400;camera.allowHDR=true;camera.cullingMask&=~(1<<5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            bool accepted=false,consumed=false;WorldEffectSnapshot.Field recorded=default;
            caster.Intent.Parked=false;caster.Intent.FaceAimPoint=true;
            try
            {
                yield return ImprovementEvidenceProbe.Record(camera,"cinder-gate",4.6f,caster,time=>
                {
                    caster.Intent.AimPoint=new Vector3(-3,.12f,-2);
                    caster.Intent.Set(Verb.Skill2,time>=.25f&&time<.65f);
                    if(time>.65f&&caster.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Skill2)==HeroKit.CastOutcome.Cast
                        &&caster.AbilitySystem.SecondsSinceAnswer(HeroAbilitySystem.Slot.Skill2)<2.8f)accepted=true;
                    rival.Intent.Move=time>=1.4f&&!consumed?Vector2.up:Vector2.zero;
                    var gate=SeanCinderGate.Active.FirstOrDefault();
                    if(gate!=null&&gate.Spent){consumed=true;recorded=gate.Capture();}
                },witnessOffset:new Vector3(4,3,-4),witnessLookHeight:1.0f);
                Assert.IsTrue(accepted,"Actual held/released Skill2 input was never accepted.");Assert.IsTrue(consumed);Assert.AreEqual(0,SeanCinderGate.Active.Count);
                var stage=new GameObject("Recorded Cinder witness");
                try
                {
                    using(var view=new RecordedFieldView(stage.transform,recorded))
                    {
                        view.Sample(recorded,.2f);view.Visible(true);
                        Assert.IsEmpty(view.Root.GetComponentsInChildren<Collider>(true));
                        Assert.IsEmpty(view.Root.GetComponentsInChildren<SeanCinderGate>(true));
                        Assert.AreEqual(0,SeanCinderGate.Active.Count,"Replay cannot spawn a live crossing.");
                    }
                    yield return null;
                }
                finally{Object.Destroy(stage);}
            }
            finally{caster.Intent.Set(Verb.Skill2,false);rival.Intent.Move=Vector2.zero;Object.Destroy(camera.gameObject);}
        }
    }
}
