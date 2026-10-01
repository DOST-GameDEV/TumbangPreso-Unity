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
