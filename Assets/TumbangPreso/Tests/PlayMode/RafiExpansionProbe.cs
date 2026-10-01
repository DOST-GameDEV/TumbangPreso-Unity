using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // Focused initial integration, not full roster, network or balance acceptance.
    public sealed class RafiExpansionProbe
    {
        private bool _bots,_spectator,_pinned;
        private int _seat;
        private INetProvider _provider;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _provider=NetAuthority.Provider;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_provider;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        private static IEnumerator Start(string map=SceneFlow.BayanPlaza,GameMode mode=GameMode.HeroStrike)
        {
            yield return MapRetrievalProbe.Load(map,mode);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider=new SoloProvider();
            Assert.IsTrue(GameServices.Round.RoundActive,"Fixture did not enter a real active round.");
            foreach(var actor in GameServices.Round.Players)
            {actor.Intent.Clear();actor.Intent.Parked=true;actor.Teleport(new Vector3(7,.10f,7+actor.PlayerSlot));}
        }
        private static CharacterMotor Rafi()
        {
            var actor=GameServices.Round.PlayerAt(1);
            actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"rafi");actor.AbilitySystem.BindHero("rafi");
            actor.Teleport(new Vector3(0,.06f,-8));actor.transform.rotation=Quaternion.identity;
            actor.Intent.Parked=false;actor.Intent.AimPoint=new Vector3(0,.06f,2);
            return actor;
        }
        private static AbilityContext Context(CharacterMotor actor)=>new AbilityContext(actor,actor.GetComponent<Carrier>(),actor.GetComponent<CombatVerbs>());

        [UnityTest,Timeout(60000)] public IEnumerator CurrentSteersOneFlightWithoutStealingCredit()
        {
            yield return Start();var caster=Rafi();yield return null;
            var shoes=new[]{GameServices.Round.PlayerAt(2).GetComponent<Carrier>().Held,GameServices.Round.PlayerAt(3).GetComponent<Carrier>().Held};
            for(int i=0;i<2;i++)shoes[i].HostThrow(GameServices.Round.PlayerAt(i+2),new Vector3(i==0?-.18f:.18f,1.8f,-5.4f),new Vector3(0,0,-4.5f));
            var bent=new HashSet<int>();
            Assert.AreEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.Kit.CastSkill1(Context(caster)));
            float start=Time.time;
            while(Time.time-start<.65f)
            {
                yield return new WaitForFixedUpdate();
                for(int i=0;i<2;i++)if(shoes[i].State==SlipperState.InFlight&&Mathf.Abs(shoes[i].Velocity.x)>.5f)
                {
                    bent.Add(i);Assert.AreEqual(i+2,shoes[i].ThrowerSlot);Assert.AreEqual(i+2,shoes[i].OwnerSlot);
                    Assert.AreEqual(SlipperAffinity.Normal,shoes[i].Affinity);
                    Assert.AreEqual(4.5f,new Vector2(shoes[i].Velocity.x,shoes[i].Velocity.z).magnitude,.10f);
                }
            }
            Assert.AreEqual(1,bent.Count,"One current must intercept one flight, not zero or both.");
            Assert.AreEqual("rafi",caster.AbilitySystem.Kit.HeroId,"A fixture roster mismatch replaced the kit.");
            Assert.IsFalse(caster.AbilitySystem.Kit.Skill1.UsesCharges);
            Assert.That(caster.AbilitySystem.Kit.Skill1.CooldownRemaining,Is.InRange(34f,35f));
            Assert.IsFalse(caster.AbilitySystem.Kit.Skill1.CanActivate(Context(caster)));
        }

        [UnityTest,Timeout(60000)] public IEnumerator CurrentChoosesFirstContactRegardlessOfInventoryOrder()
        {
            yield return Start(); var caster=Rafi(); yield return null;
            var near=GameServices.Round.PlayerAt(2).GetComponent<Carrier>().Held;
            var far=GameServices.Round.PlayerAt(3).GetComponent<Carrier>().Held;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var inventory=typeof(RafiWaterField).GetField("_shoes",flags);
            var resolve=typeof(RafiWaterField).GetMethod("ResolveCurrent",flags);
            foreach(bool reversed in new[]{true,false})
            {
                Vector3 origin=caster.transform.position;
                near.HostThrow(GameServices.Round.PlayerAt(2),origin+new Vector3(0,.85f,1),Vector3.right*4.5f);
                far.HostThrow(GameServices.Round.PlayerAt(3),origin+new Vector3(0,.85f,3),Vector3.right*4.5f);
                var field=RafiWaterField.Cast(Context(caster),WorldEffectSnapshot.Kind.Current,.65f,8,.93f,false);
                Assert.IsNotNull(field);
                inventory.SetValue(field,reversed?new[]{far,near}:new[]{near,far});
                // A controlled four-metre host sweep through two real flights.
                resolve.Invoke(field,new object[]{4f});
                Assert.Greater(near.Velocity.z,1f,"The earlier contact must win even when enumerated last.");
                Assert.AreEqual(0,far.Velocity.z,.001f,"The later slipper remains unchanged.");
                Assert.AreEqual(4.5f,new Vector2(near.Velocity.x,near.Velocity.z).magnitude,.001f);
                Assert.AreEqual(2,near.ThrowerSlot); Assert.AreEqual(2,near.OwnerSlot);
                Assert.IsTrue(field.Capture().Split,"The current is spent after one interception.");
                Object.Destroy(field.gameObject); yield return null;
            }
        }

        [UnityTest,Timeout(60000)] public IEnumerator WaveCarriesLooseEquipmentWithoutScoringOrMovingHeldEquipment()
        {
            yield return Start();var caster=Rafi();yield return null;
            var ground=GameServices.Round.PlayerAt(2);ground.Teleport(new Vector3(.5f,.06f,-5));
            var raised=GameServices.Round.PlayerAt(3);
            var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.transform.position=new Vector3(1.8f,.55f,-4.5f);platform.transform.localScale=new Vector3(1.2f,1.1f,1.5f);
            raised.Teleport(new Vector3(1.8f,1.12f,-4.5f));Physics.SyncTransforms();
            var held=caster.GetComponent<Carrier>().Held;var loose=ground.GetComponent<Carrier>().Held;loose.HostDisarm();
            var shoeStart=new Vector3(-.8f,0,-5);shoeStart.y=Slipper.GroundY(shoeStart)+loose.RestHeight;loose.transform.position=shoeStart;
            var groundStart=ground.transform.position;var raisedStart=raised.transform.position;
            caster.AbilitySystem.Kit.Ultimate.Activate(Context(caster));
            yield return new WaitForSeconds(2.25f);
            Assert.IsTrue(GameServices.Round.Lata.IsUpright,"Water must not award a can knockdown.");
            Assert.AreSame(held,caster.GetComponent<Carrier>().Held);Assert.AreEqual(SlipperState.Held,held.State);
            Assert.AreEqual(SlipperState.Loose,loose.State,"Moving loose stock must not arm a throw.");
            Assert.AreEqual(2,loose.OwnerSlot);Assert.Greater(loose.transform.position.z-shoeStart.z,.6f);
            Assert.LessOrEqual(loose.transform.position.z-shoeStart.z,1.46f);
            Assert.Greater(ground.transform.position.z-groundStart.z,.12f);Assert.IsFalse(ground.IsStunned);
            Assert.Less(new Vector2(raised.transform.position.x-raisedStart.x,raised.transform.position.z-raisedStart.z).magnitude,.08f);
        }

        [UnityTest,Timeout(60000)] public IEnumerator SkimLoadUsesHeldIdentityAndExpiresWithoutAnEmptyHandGrant()
        {
            yield return Start(); var caster=Rafi(); yield return null;
            var carrier=caster.GetComponent<Carrier>(); var shoe=carrier.Held;
            var kit=(RafiHeroKit)caster.AbilitySystem.Kit; var ctx=Context(caster);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            Assert.IsTrue(kit.IsSkimLoaded); Assert.AreEqual(35,kit.AttackingSkill.Cooldown);
            kit.AttackingSkill.Tick(ctx,8.01f);
            Assert.IsFalse(kit.IsSkimLoaded); Assert.IsFalse(kit.AttackingSkill.IsActive);
            kit.ResetForRound(ctx);
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            Assert.IsTrue(shoe.HostDisarm());
            Assert.IsFalse(kit.ConsumeSkim(shoe),"Dropping cannot preserve the load for a later pickup.");
            kit.AttackingSkill.Tick(ctx,.01f); Assert.IsFalse(kit.AttackingSkill.IsActive);
            kit.ResetForRound(ctx);
            Assert.IsFalse(kit.AttackingSkill.CanActivate(ctx));
            Assert.AreEqual(0,kit.AttackingSkill.CooldownRemaining);
        }

        [UnityTest,Timeout(60000)] public IEnumerator SkimRealThrowContinuesTwoMetresThenBecomesNormallyLoose()
        {
            yield return Start(); var caster=Rafi(); yield return null;
            foreach(var actor in GameServices.Round.Players)
            { actor.enabled=false; actor.GetComponent<Carrier>().enabled=false; }
            caster.Teleport(new Vector3(-3,.12f,-5));
            var carrier=caster.GetComponent<Carrier>(); var shoe=carrier.Held; shoe.enabled=false;
            var kit=(RafiHeroKit)caster.AbilitySystem.Kit;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(Context(caster)));
            carrier.HostThrowAt(caster.transform.position+Vector3.up*.65f,new Vector3(-3,.12f,-2),.1f);
            Assert.AreEqual(SlipperAffinity.Skim,shoe.Affinity); Assert.IsFalse(kit.IsSkimLoaded);
            var started=typeof(Slipper).GetField("_skimStarted",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Vector3 first=Vector3.zero; bool saw=false;
            Physics.SyncTransforms();
            for(int step=0;step<300 && shoe.State==SlipperState.InFlight;step++)
            {
                shoe.SendMessage("FixedUpdate");
                if(!saw && (bool)started.GetValue(shoe))
                {
                    saw=true; first=shoe.transform.position;
                    Assert.IsTrue(shoe.IsSkimming);
                    Assert.IsFalse(shoe.HostSteerFlight(Vector3.right,40),"Ground skimming is not an airborne current target.");
                }
            }
            Assert.IsTrue(saw,"Real ground contact never began the skim.");
            Assert.AreEqual(SlipperState.Loose,shoe.State);
            Assert.AreEqual(SlipperAffinity.Normal,shoe.Affinity);
            Assert.That(Vector2.Distance(new Vector2(first.x,first.z),new Vector2(shoe.transform.position.x,shoe.transform.position.z)),Is.InRange(1.95f,2.01f));
            Assert.AreEqual(1,shoe.OwnerSlot); Assert.IsNull(carrier.Held);
            Assert.IsTrue(GameServices.Round.Lata.IsUpright);
            yield return null;
        }

        [UnityTest,Timeout(60000)] public IEnumerator LoadedSkimGuideMatchesTheRealThrowWithoutSpendingItsLoad()
        {
            yield return Start(); var caster = Rafi(); yield return null;
            foreach (var actor in GameServices.Round.Players)
            { actor.enabled = false; actor.GetComponent<Carrier>().enabled = false; }
            caster.Teleport(new Vector3(-3, .12f, -5));
            var carrier = caster.GetComponent<Carrier>(); var shoe = carrier.Held; shoe.enabled = false;
            var kit = (RafiHeroKit)caster.AbilitySystem.Kit;
            Assert.AreEqual(HeroKit.CastOutcome.Cast, kit.CastSkill2(Context(caster)));
            Assert.IsTrue(kit.IsSkimLoadedFor(shoe));
            var guide = TrajectoryPreview.AttachTo(caster); guide.enabled = false;
            var origin = caster.transform.position + Vector3.up * .65f;
            var target = new Vector3(-3, .12f, -2);
            var velocity = shoe.LaunchVelocityTo(origin, target, .1f);
            float remaining = kit.AttackingSkill.DurationRemaining;
            Assert.IsTrue(guide.TryPredictLanding(origin, velocity, 0, out var predicted));
            Assert.IsTrue(kit.IsSkimLoadedFor(shoe), "Reading the guide must not consume Skim.");
            Assert.AreEqual(remaining, kit.AttackingSkill.DurationRemaining);
            using (NetCue.SuppressRelay())
            {
                carrier.HostThrowAt(origin, target, .1f);
                Assert.AreEqual(SlipperAffinity.Skim, shoe.Affinity); Assert.IsFalse(kit.IsSkimLoaded);
                for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) shoe.SendMessage("FixedUpdate");
            }
            Assert.AreEqual(SlipperState.Loose, shoe.State);
            var actual = shoe.transform.position; actual.y = predicted.y;
            Assert.Less(Vector3.Distance(actual, predicted), .12f);
            Assert.AreEqual(1, shoe.OwnerSlot);
        }

        [UnityTest,Timeout(60000)] public IEnumerator SkimStopsBeforeNewSolidCover()
        {
            yield return Start(); var caster=Rafi(); yield return null;
            foreach(var actor in GameServices.Round.Players)
            { actor.enabled=false; actor.GetComponent<Carrier>().enabled=false; }
            var shoe=caster.GetComponent<Carrier>().Held; shoe.enabled=false;
            var at=new Vector3(-3,0,-5); at.y=Slipper.GroundY(at)+shoe.RestHeight+.01f;
            shoe.HostThrow(caster,at,new Vector3(0,-1,5),SlipperAffinity.Skim);
            for(int step=0;step<60 && !shoe.IsSkimming && shoe.State==SlipperState.InFlight;step++)shoe.SendMessage("FixedUpdate");
            Assert.IsTrue(shoe.IsSkimming,"Cover check must begin in the real ground phase.");
            var first=shoe.transform.position;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=first+new Vector3(0,.4f,.8f);wall.transform.localScale=new Vector3(1,1,.2f);
            Physics.SyncTransforms();
            for(int step=0;step<100 && shoe.State==SlipperState.InFlight;step++)shoe.SendMessage("FixedUpdate");
            Assert.AreEqual(SlipperState.Loose,shoe.State);
            Assert.That(shoe.transform.position.z-first.z,Is.InRange(0f,.61f));
            Assert.AreEqual(SlipperAffinity.Normal,shoe.Affinity);
            Object.Destroy(wall); yield return null;
        }

        [UnityTest,Timeout(60000)] public IEnumerator SkimJoiningLoadRestoresOnceWithoutRecasting()
        {
            yield return Start(); var caster=Rafi(); yield return null;
            var kit=(RafiHeroKit)caster.AbilitySystem.Kit;
            Assert.IsFalse(kit.RestoreTimedKit(caster,new TimedKitSnapshot(kit.AttackingSkill,float.NaN)));
            Assert.IsTrue(kit.RestoreTimedKit(caster,new TimedKitSnapshot(kit.AttackingSkill,4)));
            Assert.IsTrue(kit.IsSkimLoaded); Assert.AreEqual(4,kit.AttackingSkill.DurationRemaining);
            Assert.AreEqual(0,kit.AttackingSkill.CooldownRemaining,"Recovery is not an activation.");
            Assert.IsFalse(kit.RestoreTimedKit(caster,new TimedKitSnapshot(kit.AttackingSkill,7)));
            Assert.AreEqual(4,kit.AttackingSkill.DurationRemaining);
            caster.AbilitySystem.BindHero("rafi"); kit=(RafiHeroKit)caster.AbilitySystem.Kit;
            Assert.IsFalse(kit.RestoreTimedKit(caster,new TimedKitSnapshot(kit.AttackingSkill,0)));
            Assert.IsFalse(kit.RestoreTimedKit(caster,new TimedKitSnapshot(kit.AttackingSkill,4)));
            Assert.IsFalse(kit.IsSkimLoaded);
            kit.ResetForRound(Context(caster));
            Assert.IsTrue(kit.RestoreTimedKit(caster,new TimedKitSnapshot(kit.AttackingSkill,3)),
                "A new round must allow its own first recovery snapshot.");
            Assert.AreEqual(3,kit.AttackingSkill.DurationRemaining);
        }

        [UnityTest,Timeout(60000)] public IEnumerator SkimStaysInsideCourtAndStopsWhenTheRoundEnds()
        {
            yield return Start(); var caster=Rafi(); yield return null;
            foreach(var actor in GameServices.Round.Players)
            { actor.enabled=false; actor.GetComponent<Carrier>().enabled=false; }
            var shoe=caster.GetComponent<Carrier>().Held; shoe.enabled=false;
            var at=new Vector3(AIController.PlayableMaxX-.5f,0,-5);
            at.y=Slipper.GroundY(at)+shoe.RestHeight+.01f;
            shoe.HostThrow(caster,at,new Vector3(5,-1,0),SlipperAffinity.Skim);
            for(int step=0;step<80 && shoe.State==SlipperState.InFlight;step++)shoe.SendMessage("FixedUpdate");
            Assert.AreEqual(SlipperState.Loose,shoe.State);
            Assert.LessOrEqual(shoe.transform.position.x,AIController.PlayableMaxX);
            Assert.Less(shoe.transform.position.x-at.x,.51f);
            at=new Vector3(-3,0,-5);at.y=Slipper.GroundY(at)+shoe.RestHeight+.01f;
            shoe.HostThrow(caster,at,new Vector3(0,-1,5),SlipperAffinity.Skim);
            for(int step=0;step<60 && !shoe.IsSkimming && shoe.State==SlipperState.InFlight;step++)shoe.SendMessage("FixedUpdate");
            Assert.IsTrue(shoe.IsSkimming,"Round interruption must start after real ground contact.");
            GameServices.Round.EndRound();
            shoe.SendMessage("FixedUpdate");
            Assert.AreEqual(SlipperState.Loose,shoe.State);
        }

        [UnityTest,Timeout(60000)] public IEnumerator SkimBodyContactConsumesThePayloadNormally()
        {
            yield return Start(); var caster=Rafi(); yield return null;
            foreach(var actor in GameServices.Round.Players)
            { actor.enabled=false; actor.GetComponent<Carrier>().enabled=false; }
            var victim=GameServices.Round.PlayerAt(2);victim.Teleport(new Vector3(-3,.12f,-3.4f));
            var shoe=caster.GetComponent<Carrier>().Held;shoe.enabled=false;
            var at=new Vector3(-3,0,-5);at.y=Slipper.GroundY(at)+shoe.RestHeight+.01f;
            shoe.HostThrow(caster,at,new Vector3(0,-1,5),SlipperAffinity.Skim);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var contacts=typeof(Slipper).GetField("_bodyContacts",flags);
            Physics.SyncTransforms();
            for(int step=0;step<80 && shoe.State==SlipperState.InFlight;step++)
            {
                shoe.SendMessage("FixedUpdate");
                if(((int)contacts.GetValue(shoe)&(1<<2))!=0)break;
            }
            Assert.AreNotEqual(0,(int)contacts.GetValue(shoe)&(1<<2));
            Assert.AreEqual(SlipperAffinity.Normal,shoe.Affinity);
            Assert.AreEqual(0f,(float)typeof(Slipper).GetField("_skimLeft",flags).GetValue(shoe));
            Assert.AreEqual(StunElement.None,victim.StunElement);
            yield return null;
        }

        private static CharacterMotor WallCaster()
        {
            var caster=GameServices.Round.Players.First(p=>p.IsDefender);
            foreach(var actor in GameServices.Round.Players)
            { actor.enabled=false; actor.GetComponent<Carrier>().enabled=false; }
            caster.AbilitySystem.BindHero("rafi");caster.Teleport(new Vector3(-3,.12f,-5));
            caster.transform.rotation=Quaternion.identity;caster.Intent.AimPoint=new Vector3(-3,.12f,-2);
            caster.AbilitySystem.Kit.SetRole(true,Context(caster));
            return caster;
        }

        [UnityTest,Timeout(60000)] public IEnumerator WaterwallDropsOneCrossingOnItsApproachSide()
        {
            yield return Start(); var caster=WallCaster();var ctx=Context(caster);
            var kit=caster.AbilitySystem.Kit;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            Assert.AreEqual(35,kit.DefendingSkill.CooldownRemaining);
            var field=RafiWaterField.Active.Single(f=>f.Capture().Type==WorldEffectSnapshot.Kind.Waterwall);
            yield return new WaitForSeconds(.3f);
            Assert.AreEqual(0,field.GetComponentsInChildren<Collider>(true).Length,"People must pass through the curtain.");
            var state=field.Capture();Assert.IsTrue(WorldEffectSnapshot.Valid(state));
            var shoe=GameServices.Round.PlayerAt(2).GetComponent<Carrier>().Held;shoe.enabled=false;
            var at=state.Position-state.Forward*.1f+Vector3.up*.9f;
            shoe.HostThrow(GameServices.Round.PlayerAt(2),at,state.Forward*10);
            field.SendMessage("RememberShoes");shoe.SendMessage("FixedUpdate");field.SendMessage("FixedUpdate");
            Assert.AreEqual(SlipperState.Loose,shoe.State);
            Assert.Less(Vector3.Dot(shoe.transform.position-state.Position,state.Forward),0);
            Assert.AreEqual(2,shoe.OwnerSlot);Assert.IsTrue(field.Capture().Split);
            var later=GameServices.Round.PlayerAt(3).GetComponent<Carrier>().Held;later.enabled=false;
            later.HostThrow(GameServices.Round.PlayerAt(3),at,state.Forward*10);
            field.SendMessage("RememberShoes");later.SendMessage("FixedUpdate");field.SendMessage("FixedUpdate");
            Assert.AreEqual(SlipperState.InFlight,later.State,"The spent curtain cannot block another throw.");
            Assert.IsTrue(GameServices.Round.Lata.IsUpright);
        }

        [UnityTest,Timeout(60000)] public IEnumerator WaterwallRefusesSolidCoverWithoutSpendingAndExpires()
        {
            yield return Start();var caster=WallCaster();var ctx=Context(caster);var kit=caster.AbilitySystem.Kit;
            var point=new Vector3(-3,.12f,-2);
            Assert.IsTrue(RafiWaterField.CanPlaceWall(ctx,point));
            Assert.IsFalse(RafiWaterField.CanPlaceWall(ctx,new Vector3(-3,.12f,5)),"Out of range.");
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.position=new Vector3(-3,.8f,-3.5f);block.transform.localScale=new Vector3(1,2,.3f);
            Physics.SyncTransforms();
            Assert.IsFalse(RafiWaterField.CanPlaceWall(ctx,point));
            Assert.AreNotEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            Assert.AreEqual(0,kit.DefendingSkill.CooldownRemaining);
            Object.Destroy(block);yield return null;Physics.SyncTransforms();
            Assert.AreEqual(HeroKit.CastOutcome.Cast,kit.CastSkill2(ctx));
            var field=RafiWaterField.Active.Single(f=>f.Capture().Type==WorldEffectSnapshot.Kind.Waterwall);
            yield return new WaitForSeconds(4.1f);
            Assert.IsTrue(field==null || !field.isActiveAndEnabled);
        }

        [UnityTest,Timeout(60000)] public IEnumerator WaterwallRecoveryIsRenderOnlyWithNativeCourtViews()
        {
            yield return Start();var caster=WallCaster();caster.IsBot=false;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.Kit.CastSkill2(Context(caster)));
            var field=RafiWaterField.Active.Single(f=>f.Capture().Type==WorldEffectSnapshot.Kind.Waterwall);
            var rig=Object.FindFirstObjectByType<TumbangPreso.CameraSystem.CameraRig>();rig.Follow(caster);rig.SetAimSource(TumbangPreso.CameraSystem.AimSource.Movement);
            yield return new WaitForSeconds(.35f);
            var canvas=GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("Waterwall-active-960x540",canvas,960,540,false,true);
            var state=field.Capture();var parent=new GameObject("Waterwall replay witness");
            using(var view=new RecordedFieldView(parent.transform,state))
            {
                view.Sample(state,.4f);
                Assert.AreEqual(0,view.Root.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(0,view.Root.GetComponentsInChildren<RafiWaterField>(true).Length);
                var sheet=view.Root.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.sharedMesh.name=="RafiWaterCurtain").sharedMesh;
                Assert.AreEqual(78,sheet.vertexCount,"A folded sheet, not the former flat rectangle");
                var sampled=sheet.vertices;
                Assert.Greater(sampled.Max(v=>v.z)-sampled.Min(v=>v.z),.14f,"Visible curled lip/source trough");
                Assert.Less(sheet.colors[6*6+3].a,.25f,"Quiet transparent centre");
                view.Sample(state,.65f);view.Sample(state,.4f);
                CollectionAssert.AreEqual(sampled,sheet.vertices,"Replay sampling is independent of prior age");
                Assert.IsTrue(sheet.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)));
            }
            Object.Destroy(parent);
            var invalid=state;invalid.FirstScale=2;invalid.Split=false;
            Assert.IsFalse(WorldEffectSnapshot.Valid(invalid));
            state.Split=true;state.FirstScale=state.Duration-state.Remaining;
            Assert.AreSame(field,RafiWaterField.Restore(state,0));
            yield return new WaitForSeconds(.3f);
            yield return TumpUiCapture.Capture("Waterwall-broken-960x540",canvas,960,540,false,true);
        }

        [UnityTest,Timeout(300000)]
        public IEnumerator WaterwallUsesDedicatedShippingPoseAndAgeDrivenRivulets()
        {
            yield return Start(); var caster=WallCaster();caster.IsBot=false;
            caster.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"rafi");
            var art=RosterBook.Load().FindPersonArt("rafi");
            var clip=art.Clips.SingleOrDefault(c=>c!=null&&c.name=="hero-rafi-wall");
            Assert.IsNotNull(clip,"The shipping roster must reference the baked action, not just an Editor fallback.");
            Assert.Greater(clip.length,.7f);
            var visual=caster.GetComponent<TumbangPreso.Visual.CharacterVisual>();
            visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(caster);rig.SetAimSource(AimSource.Movement);
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.SetCharacter("rafi");
            yield return null;yield return null;
            var animator=caster.GetComponent<TumbangPreso.Visual.CharacterAnimator>();
            var arm=visual.Model.GetComponentsInChildren<Transform>().First(t=>t.name=="arm-left");
            var torso=visual.Model.GetComponentsInChildren<Transform>().First(t=>t.name=="torso");
            var restArm=arm.localRotation;var restTorso=torso.localRotation;
            var ability=caster.AbilitySystem.Kit.DefendingSkill;
            Assert.AreEqual("hero-rafi-wall",ability.CastAction);Assert.AreEqual("waterwall-lift",ability.ViewmodelAction);
            var witness=new GameObject("Rafi wall witness").AddComponent<Camera>();witness.CopyFrom(Camera.main);
            witness.enabled=false;witness.tag="Untagged";witness.fieldOfView=48;
            witness.gameObject.AddComponent<TumbangPreso.Visual.ColourGrade>().AdoptFromScene();
            witness.transform.position=caster.transform.position+new Vector3(4,2,3);
            witness.transform.LookAt(caster.transform.position+new Vector3(0,.9f,1.2f));
            var hdr=new RenderTexture(960,540,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear);
            var ldr=new RenderTexture(960,540,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
            string folder="Logs/rafi-wall-motion";
            System.IO.Directory.CreateDirectory(folder+"/owner");System.IO.Directory.CreateDirectory(folder+"/witness");
            var log=new System.Text.StringBuilder("frame,seconds,arm_angle,torso_angle,clip,split\n");
            int rate=Time.captureFramerate;Time.captureFramerate=30;
            float armMotion=0,torsoMotion=0,firstPersonMotion=0;bool broke=false;
            try
            {
                Assert.AreEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill2,
                    caster.transform.position,caster.transform.forward,caster.Intent.AimPoint,0,true,ability.Id,false));
                Assert.AreEqual("hero-rafi-wall",animator.CurrentClipName);
                var viewArms=Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None).First(a=>a.gameObject.activeInHierarchy);
                var viewFlags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                Assert.IsNotNull(typeof(ViewmodelArms).GetField("_clip",viewFlags).GetValue(viewArms),"The first-person action must resolve, not only kick the camera.");
                var field=RafiWaterField.Active.Single(f=>f.Capture().Type==WorldEffectSnapshot.Kind.Waterwall);
                var renderer=field.GetComponentsInChildren<MeshRenderer>().Single(r=>r.sharedMaterial.HasProperty("_CurtainFlow")&&r.sharedMaterial.GetFloat("_CurtainFlow")>.5f);
                Assert.AreEqual(1,renderer.sharedMaterial.GetFloat("_UseVertexTint"));
                var stateForReplay=field.Capture();
                for(int frame=0;frame<60;frame++)
                {
                    if(frame==40)
                    {
                        var state=field.Capture();var thrower=GameServices.Round.PlayerAt(2);var shoe=thrower.GetComponent<Carrier>().Held;
                        shoe.enabled=false;shoe.HostThrow(thrower,state.Position-state.Forward*.1f+Vector3.up,state.Forward*10);
                        field.SendMessage("RememberShoes");shoe.SendMessage("FixedUpdate");field.SendMessage("FixedUpdate");
                        Assert.AreEqual(SlipperState.Loose,shoe.State);stateForReplay=field.Capture();broke=stateForReplay.Split;
                    }
                    yield return null;
                    float armAngle=Quaternion.Angle(restArm,arm.localRotation),torsoAngle=Quaternion.Angle(restTorso,torso.localRotation);
                    armMotion=Mathf.Max(armMotion,armAngle);torsoMotion=Mathf.Max(torsoMotion,torsoAngle);
                    var offset=(Vector3)typeof(ViewmodelArms).GetField("_castLeft",viewFlags).GetValue(viewArms);
                    firstPersonMotion=Mathf.Max(firstPersonMotion,offset.magnitude);
                    log.AppendLine(System.FormattableString.Invariant($"{frame},{frame/30f:F3},{armAngle:F3},{torsoAngle:F3},{animator.CurrentClipName},{broke}"));
                    foreach(var camera in new[]{Camera.main,witness})
                    {
                        PaeteKitPlayProbe.RenderFilmView(camera,hdr);Graphics.Blit(hdr,ldr);
                        var old=RenderTexture.active;RenderTexture.active=ldr;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();RenderTexture.active=old;
                        System.IO.File.WriteAllBytes(folder+(camera==witness?"/witness/":"/owner/")+frame.ToString("D5")+".jpg",pixels.EncodeToJPG(90));
                    }
                }
                Assert.Greater(armMotion,25,"The actual body must visibly lift its arm.");Assert.Greater(torsoMotion,8,"The body participates in the scoop.");
                Assert.Greater(firstPersonMotion,.15f,"The accepted cast must lift the actual first-person palm.");
                Assert.IsTrue(broke,"The one-use break must be filmed from actual contact.");
                var parent=new GameObject("Wall material replay");
                using(var view=new RecordedFieldView(parent.transform,stateForReplay))
                {
                    view.Sample(stateForReplay,.2f);
                    var sheet=view.Root.GetComponentsInChildren<MeshRenderer>().Single(r=>r.sharedMaterial.HasProperty("_CurtainFlow")&&r.sharedMaterial.GetFloat("_CurtainFlow")>.5f);
                    var block=new MaterialPropertyBlock();sheet.GetPropertyBlock(block);float first=block.GetFloat("_FlowAge");
                    view.Sample(stateForReplay,.6f);view.Sample(stateForReplay,.2f);sheet.GetPropertyBlock(block);
                    Assert.AreEqual(first,block.GetFloat("_FlowAge"),.0001f,"Flow must be deterministic under non-monotonic replay sampling.");
                    Assert.AreEqual(0,view.Root.GetComponentsInChildren<Collider>(true).Length);
                }
                Object.Destroy(parent);
            }
            finally
            {
                Time.captureFramerate=rate;System.IO.File.WriteAllText(folder+"/motion.csv",log.ToString());
                Object.Destroy(witness.gameObject);Object.Destroy(hdr);Object.Destroy(ldr);Object.Destroy(pixels);
            }
        }

        [UnityTest,Timeout(180000)]
        public IEnumerator SkimUsesDedicatedShippingCoatMotionAndPreservesHeldShoe()
        {
            yield return Start();var caster=Rafi();caster.IsBot=false;GameLaunch.SoloSeat=caster.PlayerSlot;
            var art=RosterBook.Load().FindPersonArt("rafi");
            Assert.IsNotNull(art.Clips.SingleOrDefault(c=>c!=null&&c.name=="hero-rafi-skim"));
            var visual=caster.GetComponent<TumbangPreso.Visual.CharacterVisual>();
            visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(caster);rig.SetAimSource(AimSource.Movement);
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.SetCharacter("rafi");
            var view=Object.FindFirstObjectByType<TumpMatchReadout>();
            float settle=Time.realtimeSinceStartup+TumpPowerReadout.RoleSwapSeconds+.15f;
            while(Time.realtimeSinceStartup<settle){view.Tick(caster,false,false,false,false);yield return null;}
            yield return TumpUiCapture.Capture("Rafi-skim-settled-960x540",view.Canvas,960,540,false,true);
            var animator=caster.GetComponent<TumbangPreso.Visual.CharacterAnimator>();
            var arm=visual.Model.GetComponentsInChildren<Transform>().First(t=>t.name=="arm-left");
            var torso=visual.Model.GetComponentsInChildren<Transform>().First(t=>t.name=="torso");
            var restArm=arm.localRotation;var restTorso=torso.localRotation;
            var carrier=caster.GetComponent<Carrier>();var shoe=carrier.Held;Assert.IsNotNull(shoe);
            var ability=caster.AbilitySystem.Kit.Skill2;
            Assert.AreEqual("hero-rafi-skim",ability.CastAction);Assert.AreEqual("skim-coat",ability.ViewmodelAction);
            var witness=new GameObject("Rafi skim witness").AddComponent<Camera>();witness.CopyFrom(Camera.main);
            witness.enabled=false;witness.tag="Untagged";witness.fieldOfView=45;
            witness.gameObject.AddComponent<TumbangPreso.Visual.ColourGrade>().AdoptFromScene();
            witness.transform.position=caster.transform.position+new Vector3(3,1.6f,2.5f);
            witness.transform.LookAt(caster.transform.position+Vector3.up*.9f);
            var hdr=new RenderTexture(960,540,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear);
            var ldr=new RenderTexture(960,540,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
            const string folder="Logs/rafi-skim-motion";
            System.IO.Directory.CreateDirectory(folder+"/owner");System.IO.Directory.CreateDirectory(folder+"/witness");
            var log=new System.Text.StringBuilder("frame,arm_angle,torso_angle,left_offset\n");
            int rate=Time.captureFramerate;Time.captureFramerate=30;
            float armMotion=0,torsoMotion=0,firstPersonMotion=0;
            try
            {
                Assert.AreEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill2,
                    caster.transform.position,caster.transform.forward,caster.Intent.AimPoint,0,true,ability.Id,false));
                Assert.AreEqual("hero-rafi-skim",animator.CurrentClipName);
                Assert.IsTrue(((RafiHeroKit)caster.AbilitySystem.Kit).IsSkimLoadedFor(shoe));
                var viewArms=Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None).First(a=>a.gameObject.activeInHierarchy);
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                Assert.IsNotNull(typeof(ViewmodelArms).GetField("_clip",flags).GetValue(viewArms));
                for(int frame=0;frame<36;frame++)
                {
                    yield return null;
                    float a=Quaternion.Angle(restArm,arm.localRotation),t=Quaternion.Angle(restTorso,torso.localRotation);
                    float offset=((Vector3)typeof(ViewmodelArms).GetField("_castLeft",flags).GetValue(viewArms)).magnitude;
                    armMotion=Mathf.Max(a,armMotion);torsoMotion=Mathf.Max(t,torsoMotion);firstPersonMotion=Mathf.Max(offset,firstPersonMotion);
                    Assert.AreSame(shoe,carrier.Held);Assert.AreEqual(SlipperState.Held,shoe.State);
                    log.AppendLine(System.FormattableString.Invariant($"{frame},{a:F3},{t:F3},{offset:F3}"));
                    foreach(var camera in new[]{Camera.main,witness})
                    {
                        PaeteKitPlayProbe.RenderFilmView(camera,hdr);Graphics.Blit(hdr,ldr);
                        var old=RenderTexture.active;RenderTexture.active=ldr;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();RenderTexture.active=old;
                        System.IO.File.WriteAllBytes(folder+(camera==witness?"/witness/":"/owner/")+frame.ToString("D5")+".jpg",pixels.EncodeToJPG(90));
                    }
                }
                Assert.Greater(armMotion,25);Assert.Greater(torsoMotion,5);Assert.Greater(firstPersonMotion,.2f);
                Assert.IsTrue(((RafiHeroKit)caster.AbilitySystem.Kit).IsSkimLoadedFor(shoe));
            }
            finally
            {
                Time.captureFramerate=rate;System.IO.File.WriteAllText(folder+"/motion.csv",log.ToString());
                Object.Destroy(witness.gameObject);Object.Destroy(hdr);Object.Destroy(ldr);Object.Destroy(pixels);
            }
            var defender=WallCaster();defender.IsBot=false;GameLaunch.SoloSeat=defender.PlayerSlot;
            rig.Follow(defender);defender.AbilitySystem.Kit.SetRole(true,Context(defender));
            settle=Time.realtimeSinceStartup+TumpPowerReadout.RoleSwapSeconds+.15f;
            while(Time.realtimeSinceStartup<settle){view.Tick(defender,false,false,false,false);yield return null;}
            yield return TumpUiCapture.Capture("Rafi-wall-settled-960x540",view.Canvas,960,540,false,true);
        }

        [UnityTest,Timeout(180000)]
        public IEnumerator SkimCoatingFollowsRealLoadedShoeAndClearsOnExpiryOrRelease()
        {
            yield return Start();var caster=Rafi();caster.IsBot=false;GameLaunch.SoloSeat=caster.PlayerSlot;
            var art=RosterBook.Load().FindPersonArt("rafi");
            Assert.IsNotNull(art.Clips.SingleOrDefault(c=>c!=null&&c.name=="hero-rafi-skim"));
            var visual=caster.GetComponent<TumbangPreso.Visual.CharacterVisual>();
            visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(caster);rig.SetAimSource(AimSource.Movement);
            foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.SetCharacter("rafi");
            var view=Object.FindFirstObjectByType<TumpMatchReadout>();
            Object.FindFirstObjectByType<Hud>().Bind(caster);
            float settle=Time.realtimeSinceStartup+TumpPowerReadout.RoleSwapSeconds+.15f;
            while(Time.realtimeSinceStartup<settle){view.Tick(caster,false,false,false,false);yield return null;}
            yield return TumpUiCapture.Capture("Rafi-skim-settled-960x540",view.Canvas,960,540,false,true);
            var animator=caster.GetComponent<TumbangPreso.Visual.CharacterAnimator>();
            var arm=visual.Model.GetComponentsInChildren<Transform>().First(t=>t.name=="arm-left");
            var torso=visual.Model.GetComponentsInChildren<Transform>().First(t=>t.name=="torso");
            var restArm=arm.localRotation;var restTorso=torso.localRotation;
            var carrier=caster.GetComponent<Carrier>();var shoe=carrier.Held;Assert.IsNotNull(shoe);
            var ability=caster.AbilitySystem.Kit.Skill2;
            Assert.AreEqual("hero-rafi-skim",ability.CastAction);Assert.AreEqual("skim-coat",ability.ViewmodelAction);
            var witness=new GameObject("Rafi skim witness").AddComponent<Camera>();witness.CopyFrom(Camera.main);
            witness.enabled=false;witness.tag="Untagged";witness.fieldOfView=45;
            witness.gameObject.AddComponent<TumbangPreso.Visual.ColourGrade>().AdoptFromScene();
            witness.transform.position=caster.transform.position+new Vector3(3,1.6f,2.5f);
            witness.transform.LookAt(caster.transform.position+Vector3.up*.9f);
            var hdr=new RenderTexture(960,540,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear);
            var ldr=new RenderTexture(960,540,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
            const string folder="Logs/rafi-skim-coating";
            System.IO.Directory.CreateDirectory(folder+"/owner");System.IO.Directory.CreateDirectory(folder+"/witness");
            var log=new System.Text.StringBuilder("frame,arm_angle,torso_angle,left_offset\n");
            int rate=Time.captureFramerate;Time.captureFramerate=30;
            yield return null;
            float armMotion=0,torsoMotion=0,firstPersonMotion=0;
            try
            {
                Assert.AreEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.ApplyNetworkCast(HeroAbilitySystem.Slot.Skill2,
                    caster.transform.position,caster.transform.forward,caster.Intent.AimPoint,0,true,ability.Id,false));
                Assert.AreEqual("hero-rafi-skim",animator.CurrentClipName);
                Assert.IsTrue(((RafiHeroKit)caster.AbilitySystem.Kit).IsSkimLoadedFor(shoe));
                var viewArms=Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None).First(a=>a.gameObject.activeInHierarchy);
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                Assert.IsNotNull(typeof(ViewmodelArms).GetField("_clip",flags).GetValue(viewArms));
                for(int frame=0;frame<30;frame++)
                {
                    yield return null;
                    float a=Quaternion.Angle(restArm,arm.localRotation),t=Quaternion.Angle(restTorso,torso.localRotation);
                    float offset=((Vector3)typeof(ViewmodelArms).GetField("_castLeft",flags).GetValue(viewArms)).magnitude;
                    armMotion=Mathf.Max(a,armMotion);torsoMotion=Mathf.Max(t,torsoMotion);firstPersonMotion=Mathf.Max(offset,firstPersonMotion);
                    Assert.AreSame(shoe,carrier.Held);Assert.AreEqual(SlipperState.Held,shoe.State);
                    log.AppendLine(System.FormattableString.Invariant($"{frame},{a:F3},{t:F3},{offset:F3}"));
                    foreach(var camera in new[]{Camera.main,witness})
                    {
                        var surfaces=shoe.GetComponentsInChildren<Renderer>();
                        var shadows=surfaces.Select(r=>r.shadowCastingMode).ToArray();
                        if(camera==witness)foreach(var r in surfaces)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                        try{PaeteKitPlayProbe.RenderFilmView(camera,hdr);}
                        finally{for(int i=0;i<surfaces.Length;i++)surfaces[i].shadowCastingMode=shadows[i];}
                        Graphics.Blit(hdr,ldr);
                        var old=RenderTexture.active;RenderTexture.active=ldr;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();RenderTexture.active=old;
                        System.IO.File.WriteAllBytes(folder+(camera==witness?"/witness/":"/owner/")+frame.ToString("D5")+".jpg",pixels.EncodeToJPG(90));
                    }
                }
                Assert.Greater(armMotion,25);Assert.Greater(torsoMotion,5);Assert.Greater(firstPersonMotion,.2f);
                Assert.IsTrue(((RafiHeroKit)caster.AbilitySystem.Kit).IsSkimLoadedFor(shoe));
                var coatings=Object.FindObjectsByType<TumbangPreso.Visual.RafiSkimCoating>(FindObjectsSortMode.None);
                Assert.AreEqual(2,coatings.Count(c=>c.Shoe==shoe),"Actual world prop and private first-person copy each need a cue.");
                Assert.IsTrue(coatings.All(c=>c.VisibleStrength>.9f));
                Assert.AreEqual(0,coatings.Sum(c=>c.GetComponentsInChildren<Collider>().Length));
                // Natural remaining-time expiry, not a presentation-owned timer.
                float deadline=Time.realtimeSinceStartup+12;
                while(ability.IsActive&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsFalse(ability.IsActive);yield return null;
                Assert.AreEqual(0,Object.FindObjectsByType<TumbangPreso.Visual.RafiSkimCoating>(FindObjectsSortMode.None).Length);
                caster.AbilitySystem.Kit.Reset();
                Assert.AreEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.Kit.CastSkill2(Context(caster)));
                yield return null;
                Assert.IsTrue(((RafiHeroKit)caster.AbilitySystem.Kit).IsSkimLoadedFor(shoe));
                shoe.HostThrow(caster,shoe.transform.position,Vector3.forward*3);
                yield return null;yield return null;
                Assert.AreEqual(0,Object.FindObjectsByType<TumbangPreso.Visual.RafiSkimCoating>(FindObjectsSortMode.None).Length);
            }
            finally
            {
                Time.captureFramerate=rate;System.IO.File.WriteAllText(folder+"/motion.csv",log.ToString());
                Object.Destroy(witness.gameObject);Object.Destroy(hdr);Object.Destroy(ldr);Object.Destroy(pixels);
            }
            var defender=WallCaster();defender.IsBot=false;GameLaunch.SoloSeat=defender.PlayerSlot;
            rig.Follow(defender);Object.FindFirstObjectByType<Hud>().Bind(defender);
            defender.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"rafi");defender.AbilitySystem.Kit.SetRole(true,Context(defender));
            settle=Time.realtimeSinceStartup+TumpPowerReadout.RoleSwapSeconds+.15f;
            while(Time.realtimeSinceStartup<settle){view.Tick(defender,false,false,false,false);yield return null;}
            yield return TumpUiCapture.Capture("Rafi-wall-settled-960x540",view.Canvas,960,540,false,true);
        }

        [UnityTest,Timeout(90000)]
        public IEnumerator SkimAndWaterwallUseTheirOwnIllustrationsAndTruthfulJobLabels()
        {
            yield return Start();
            var art=RosterBook.Load().FindPersonArt("rafi");
            foreach(bool defending in new[]{false,true})
            {
                var actor=defending?WallCaster():Rafi();
                actor.IsBot=false;GameLaunch.SoloSeat=actor.PlayerSlot;
                actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"rafi");
                actor.GetComponent<TumbangPreso.Visual.CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                actor.AbilitySystem.Kit.SetRole(defending,Context(actor));
                var ability=defending?actor.AbilitySystem.Kit.DefendingSkill:actor.AbilitySystem.Kit.Skill2;
                var glyph=defending?AbilityGlyph.RafiWaterwall:AbilityGlyph.RafiSkim;
                Assert.AreEqual(glyph,ability.Glyph);
                Assert.AreEqual(defending?"SLIPPER SCREEN":"GROUND SKIM",AbilityIcons.LabelFor(glyph));
                var artSprite=Resources.Load<Sprite>("UI/ability-icons/"+glyph);
                Assert.IsNotNull(artSprite,"The new sprite must import natively.");
                Assert.AreSame(artSprite,AbilityIcons.For(glyph),"Do not silently pass through a generated placeholder.");
                Assert.AreNotSame(AbilityIcons.For(AbilityGlyph.RafiCrosscurrent),artSprite);
                Assert.AreNotSame(AbilityIcons.For(AbilityGlyph.RafiMirrorwake),artSprite);
                var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(actor);rig.SetAimSource(AimSource.Movement);
                foreach(var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))arms.SetCharacter("rafi");
                var view=Object.FindFirstObjectByType<TumpMatchReadout>();
                view.Tick(actor,false,false,false,false);yield return null;view.Tick(actor,false,false,false,false);
                Assert.IsTrue(view.Canvas.GetComponentsInChildren<TumpAbilitySymbol>().Any(i=>i.enabled&&i.Glyph==glyph&&i.mainTexture==artSprite.texture),"The real role deck must display the correct drawing through its custom symbol graphic.");
                yield return TumpUiCapture.Capture(defending?"Rafi-waterwall-icon-960x540":"Rafi-skim-icon-960x540",view.Canvas,960,540,false,true);
            }
        }

        [Ignore("Vaulted with the first Lagoon Court (owner, 2026-09-27: \"vault the old lagoon\"); its scene is out of the build. See docs/TODO.md LAGOON-1.7."),UnityTest,Timeout(120000)] public IEnumerator InnerAndOuterStairsLetBothModesLeaveTheWater()
        {
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                yield return Start(SceneFlow.Lagoon,mode);
                var swimmer=mode==GameMode.HeroStrike?Rafi():GameServices.Round.PlayerAt(1);
                swimmer.Intent.Parked=false;swimmer.Intent.FaceAimPoint=false;
                Object.FindFirstObjectByType<CameraRig>().Follow(GameServices.Round.PlayerAt(0));
                Assert.IsFalse(swimmer.IsDefender,"The stair witness must not be a confined defender.");
                foreach(bool inner in new[]{true,false})
                {
                    swimmer.Teleport(inner?new Vector3(16.1f,-1.98f,7.15f):new Vector3(26.65f,-1.98f,0));
                    swimmer.Intent.Move=Vector2.zero;yield return new WaitForSeconds(.15f);
                    Assert.IsTrue(swimmer.IsSwimming,mode+" did not start in the water.");
                    float start=Time.time;
                    while(Time.time-start<6 && swimmer.transform.position.y<0)
                    {swimmer.Intent.Move=inner?Vector2.down:Vector2.left;yield return null;}
                    swimmer.Intent.Move=Vector2.zero;
                    Assert.GreaterOrEqual(swimmer.transform.position.y,0,mode+" failed the "+(inner?"inner":"outer")+" stairs at "+swimmer.transform.position+" velocity "+swimmer.Velocity);
                    Assert.Less(Vector3.Distance(swimmer.transform.position,inner?new Vector3(16.1f,0,2):new Vector3(21.65f,0,0)),2,
                        "A respawn/recovery was mistaken for climbing the steps.");
                }
            }
        }
    }
}
