using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ArenaAiObjectiveApproachTests
    {
        bool bots,spectator,networked,edge,enabledBots,pinned;int seat,savedDifficulty,savedFormat;string savedWire;
        Difficulty difficulty;CustomRules rules;
        [UnitySetUp] public IEnumerator Before()
        {
            bots=GameLaunch.AllBots;spectator=GameLaunch.Spectator;seat=GameLaunch.SoloSeat;
            networked=SceneFlow.Networked;rules=SceneFlow.SelectedRules.Clone();edge=AIController.EdgeSense;
            pinned=SceneFlow.RulesPinned;savedWire=Settings.SettingsStore.Current.CustomRulesWire;savedFormat=Settings.SettingsStore.Current.MatchFormat;
            difficulty=AIController.ActiveDifficulty;enabledBots=AIController.BotsEnabled;
            savedDifficulty=Settings.SettingsStore.Current.AiDifficulty;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GameLaunch.AllBots=bots;GameLaunch.Spectator=spectator;GameLaunch.SoloSeat=seat;
            SceneFlow.AdoptRemoteRules(rules);SceneFlow.SelectedRules.Password=rules.Password;SceneFlow.Networked=networked;
            if(pinned)SceneFlow.PinSelectedRules(rules);else SceneFlow.UnpinSelectedRules();SceneFlow.SelectedRules.Password=rules.Password;
            Settings.SettingsStore.Current.CustomRulesWire=savedWire;Settings.SettingsStore.Current.MatchFormat=savedFormat;
            Settings.SettingsStore.Current.AiDifficulty=savedDifficulty;AIController.ApplyDifficulty((int)difficulty);
            AIController.BotsEnabled=enabledBots;AIController.EdgeSense=edge;Time.timeScale=1;
        }
        [UnityTest,Timeout(300000)] public IEnumerator OrdinaryBotRetrievesAcrossEachAuthoredRamp()
        {
            GameLaunch.AllBots=true;GameLaunch.Spectator=true;GameLaunch.SoloSeat=1;SceneFlow.Networked=false;
            var selected=CustomGameRules.Defaults(GameMode.Classic);selected.RoundSeconds=120;
            selected.Bots=CustomGameRules.MaxBots;selected.BotDifficulty=(int)Difficulty.Astig;
            SceneFlow.SetSelectedRules(selected);Settings.SettingsStore.Current.AiDifficulty=(int)Difficulty.Astig;
            AIController.ApplyDifficulty((int)Difficulty.Astig);GameServices.Ensure();
            var load=SceneManager.LoadSceneAsync(SceneFlow.Arena,LoadSceneMode.Single);Assert.IsNotNull(load);
            yield return load;
            float end=Time.realtimeSinceStartup+60;
            while(Time.realtimeSinceStartup<end&&(!GameServices.Round.RoundActive||PresentationClock.Held||ArenaStage.Instance==null))yield return null;
            Assert.IsTrue(GameServices.Round.RoundActive);Assert.IsFalse(PresentationClock.Held);
            var stage=ArenaStage.Instance;var runner=Object.FindFirstObjectByType<SliceRunner>();Assert.IsNotNull(runner);
            runner.enabled=false;GameServices.Round.enabled=false;
            var actor=GameServices.Round.PlayerAt(1);Assert.IsNotNull(actor);
            var brain=actor.GetComponent<AIController>();Assert.IsNotNull(brain);
            foreach(var other in runner.Seats)
                if(other!=null&&other!=actor){GameServices.Round.Unregister(other);other.gameObject.SetActive(false);}
            foreach(var shoe in runner.Slippers)if(shoe!=null&&shoe!=runner.Slippers[1])shoe.gameObject.SetActive(false);
            var mine=runner.Slippers[1];Assert.IsNotNull(mine);
            actor.IsDefender=false;actor.IsBot=true;actor.RoundActive=true;actor.Intent.Parked=false;
            var report=new List<string>();var failures=new List<string>();
            for(int layout=0;layout<stage.LayoutCount;layout++)
            {
                brain.enabled=false;stage.HoldLayout=layout;yield return null;yield return new WaitForFixedUpdate();yield return null;
                Assert.AreEqual(layout,stage.Applied);
                ArenaStage.Shape ramp=default;bool found=false;
                foreach(var piece in stage.Pieces)
                {
                    var shape=piece.Shapes[layout];
                    if(piece.Id=="ramp2"&&shape.Exists&&shape.IsRamp&&!shape.Bonus){ramp=shape;found=true;break;}
                }
                Assert.IsTrue(found,"No ordinary central ramp in "+stage.Layouts[layout].Name);
                var along=ArenaStageMesh.Direction(ramp.A0);
                Vector3 start=along*(ramp.Outer+.5f);
                Vector3 goal=along*Mathf.Max(.8f,ramp.Inner-.6f);
                if(stage.Layouts[layout].Name=="entablado")
                {start=along*Mathf.Max(.8f,ramp.Inner-.6f);goal=along*(ramp.Outer+.6f);}
                Assert.IsTrue(Ground(stage,layout,start,out var startY));Assert.IsTrue(Ground(stage,layout,goal,out var goalY));
                actor.ClearStatuses();actor.ClearTrip();actor.ClearStun();actor.Stamina.RefillAndClearFatigue();
                actor.Teleport(start+Vector3.up*(startY+.04f));actor.RoundActive=true;actor.Intent.Parked=false;
                Assert.IsTrue(mine.HostForceEquip(actor));Assert.IsTrue(mine.HostDisarm());
                mine.OwnerSlot=actor.PlayerSlot;mine.transform.position=goal+Vector3.up*(goalY+.08f);Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(SlipperState.Loose,mine.State);Assert.IsTrue(actor.CanAct());
                brain.enabled=true;float began=Time.realtimeSinceStartup;end=began+20;
                Vector3 resting=mine.transform.position;
                float movedLoose=0;
                float lowest=actor.transform.position.y;float best=Vector3.Distance(actor.transform.position,mine.transform.position);
                while(Time.realtimeSinceStartup<end&&mine.Holder!=actor)
                {yield return null;if(mine.State==SlipperState.Loose)movedLoose=Mathf.Max(movedLoose,Vector3.Distance(resting,mine.transform.position));lowest=Mathf.Min(lowest,actor.transform.position.y);best=Mathf.Min(best,Vector3.Distance(actor.transform.position,mine.transform.position));}
                bool retrieved=mine.Holder==actor;
                string line=$"{stage.Layouts[layout].Name}: retrieved={retrieved} movedLoose={movedLoose:F3} seconds={Time.realtimeSinceStartup-began:F3} start={start} target={goal} heights={startY:F3}/{goalY:F3} final={actor.transform.position} best3d={best:F3} lowY={lowest:F3} plan={brain.Plan}";
                report.Add(line);if(!retrieved||movedLoose>.2f)failures.Add(line);
            }
            Directory.CreateDirectory("Logs/arena-objective");File.WriteAllLines("Logs/arena-objective/ramp-approaches.txt",report);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest,Timeout(120000)] public IEnumerator SupportedDecksKeepShoesButRoofsAndFloatingShoesRecover()
        {
            GameLaunch.AllBots=true;GameLaunch.Spectator=true;GameLaunch.SoloSeat=1;SceneFlow.Networked=false;
            var selected=CustomGameRules.Defaults(GameMode.Classic);selected.RoundSeconds=120;
            selected.Bots=CustomGameRules.MaxBots;SceneFlow.SetSelectedRules(selected);GameServices.Ensure();
            yield return SceneManager.LoadSceneAsync(SceneFlow.Arena,LoadSceneMode.Single);
            float end=Time.realtimeSinceStartup+60;
            while(Time.realtimeSinceStartup<end&&(!GameServices.Round.RoundActive||PresentationClock.Held||ArenaStage.Instance==null))yield return null;
            Assert.IsTrue(GameServices.Round.RoundActive);Assert.IsFalse(PresentationClock.Held);
            var stage=ArenaStage.Instance;var runner=Object.FindFirstObjectByType<SliceRunner>();Assert.IsNotNull(runner);
            runner.enabled=false;GameServices.Round.enabled=false;
            foreach(var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
            var actor=GameServices.Round.PlayerAt(1);var mine=runner.Slippers[1];Assert.IsNotNull(actor);Assert.IsNotNull(mine);
            foreach(var other in runner.Seats)if(other!=null&&other!=actor){GameServices.Round.Unregister(other);other.gameObject.SetActive(false);}
            foreach(var shoe in runner.Slippers)if(shoe!=null&&shoe!=mine)shoe.gameObject.SetActive(false);
            actor.IsDefender=false;actor.RoundActive=true;actor.Intent.Parked=false;
            var report=new List<string>();var failures=new List<string>();
            for(int layout=0;layout<stage.LayoutCount;layout++)
            {
                stage.HoldLayout=layout;yield return null;yield return new WaitForFixedUpdate();yield return null;
                ArenaStage.Shape ramp=default;
                foreach(var piece in stage.Pieces)if(piece.Id=="ramp2")ramp=piece.Shapes[layout];
                Assert.IsTrue(ramp.Exists&&ramp.IsRamp);
                var along=ArenaStageMesh.Direction(ramp.A0);Vector3 owner=along*(ramp.Outer+.5f),goal=along*Mathf.Max(.8f,ramp.Inner-.6f);
                if(stage.Layouts[layout].Name=="entablado"){owner=along*Mathf.Max(.8f,ramp.Inner-.6f);goal=along*(ramp.Outer+.6f);}
                Assert.IsTrue(Ground(stage,layout,owner,out float ownerY));Assert.IsTrue(Ground(stage,layout,goal,out float goalY));
                actor.Teleport(owner+Vector3.up*(ownerY+.04f));actor.ClearStatuses();actor.ClearTrip();actor.ClearStun();
                Assert.IsTrue(mine.HostForceEquip(actor));Assert.IsTrue(mine.HostDisarm());mine.OwnerSlot=actor.PlayerSlot;
                Vector3 expected=goal+Vector3.up*(goalY+mine.RestHeight);
                mine.HostFinishMapRecoveryAt(goal+Vector3.up*goalY);Physics.SyncTransforms();
                float landMoved=Vector3.Distance(expected,mine.transform.position);
                mine.transform.position=expected;Physics.SyncTransforms();
                yield return new WaitForSeconds(.65f);
                float looseMoved=Vector3.Distance(expected,mine.transform.position);
                string line=$"{stage.Layouts[layout].Name}: landingMoved={landMoved:F3} looseMoved={looseMoved:F3} ownerY={ownerY:F3} deckY={goalY:F3} rest={mine.RestHeight:F3}";
                report.Add(line);if(landMoved>.05f||looseMoved>.05f)failures.Add(line);
            }
            stage.HoldLayout=1;yield return null;yield return new WaitForFixedUpdate();yield return null;
            actor.Teleport(new Vector3(9, .04f,0));mine.transform.position=new Vector3(3,4.5f,0);Physics.SyncTransforms();
            yield return new WaitForSeconds(.65f);
            bool floatingRecovered=Vector2.Distance(new Vector2(9,0),new Vector2(mine.transform.position.x,mine.transform.position.z))<.1f;
            report.Add("floatingRecovered="+floatingRecovered);if(!floatingRecovered)failures.Add("Floating shoe did not recover");
            var roof=new GameObject("Unreachable roof control");roof.transform.position=new Vector3(3,4.4f,0);roof.AddComponent<BoxCollider>().size=new Vector3(2,.2f,2);
            mine.transform.position=new Vector3(3,4.5f+mine.RestHeight,0);Physics.SyncTransforms();
            typeof(Slipper).GetMethod("Land",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(mine,new object[]{false,4.5f});
            bool roofLandRecovered=Vector2.Distance(new Vector2(9,0),new Vector2(mine.transform.position.x,mine.transform.position.z))<.1f;
            mine.transform.position=new Vector3(3,4.5f+mine.RestHeight,0);Physics.SyncTransforms();yield return new WaitForSeconds(.65f);
            bool roofLooseRecovered=Vector2.Distance(new Vector2(9,0),new Vector2(mine.transform.position.x,mine.transform.position.z))<.1f;
            report.Add($"roofLandRecovered={roofLandRecovered} roofLooseRecovered={roofLooseRecovered}");
            if(!roofLandRecovered||!roofLooseRecovered)failures.Add("Inaccessible roof recovery changed");
            Object.Destroy(roof);yield return null;
            Directory.CreateDirectory("Logs/arena-objective");File.WriteAllLines("Logs/arena-objective/supported-recovery.txt",report);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        static bool Ground(ArenaStage stage,int layout,Vector3 at,out float height)
        {
            foreach(var hit in Physics.RaycastAll(at+Vector3.up*6,Vector3.down,12,~0,QueryTriggerInteraction.Ignore))
                if(hit.collider.transform.IsChildOf(stage.Layouts[layout].Colliders.transform)&&Vector3.Angle(hit.normal,Vector3.up)<=45)
                {height=hit.point.y;return true;}
            height=0;return false;
        }
    }
}
