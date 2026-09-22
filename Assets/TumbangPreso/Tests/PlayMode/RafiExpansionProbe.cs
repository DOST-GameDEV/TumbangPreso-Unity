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
            Assert.AreEqual(1,caster.AbilitySystem.Kit.Skill1.ChargesRemaining);
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

        [UnityTest,Timeout(60000)] public IEnumerator MirrorAndItsRecordedViewCannotCreateActorsOrEquipment()
        {
            yield return Start();var caster=Rafi();caster.Intent.Move=Vector2.right;
            yield return new WaitForSeconds(.45f);caster.Intent.Move=Vector2.zero;
            int actors=Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length;
            int shoes=Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).Length;
            Assert.AreEqual(HeroKit.CastOutcome.Cast,caster.AbilitySystem.Kit.CastSkill2(Context(caster)));yield return null;
            var field=RafiWaterField.Active.Single(f=>f.Capture().Type==WorldEffectSnapshot.Kind.Mirrorwake);
            var state=field.Capture();Assert.IsTrue(WorldEffectSnapshot.Valid(state));Assert.GreaterOrEqual(state.Path.Length,2);
            Assert.AreEqual(0,field.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(0,field.GetComponentsInChildren<CharacterMotor>(true).Length);
            var parent=new GameObject("Recorded water review");var before=caster.transform.position;
            using(var view=new RecordedFieldView(parent.transform,state))
            {
                view.Sample(state,.3f);view.Sample(state,0);
                Assert.AreEqual(0,view.Root.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(0,view.Root.GetComponentsInChildren<RafiWaterField>(true).Length);
            }
            Object.Destroy(parent);
            Assert.AreEqual(actors,Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(shoes,Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(before,caster.transform.position);
            Assert.IsFalse(caster.AbilitySystem.IsImmuneToTags);
        }

        [UnityTest,Timeout(120000)] public IEnumerator InnerAndOuterStairsLetBothModesLeaveTheWater()
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
