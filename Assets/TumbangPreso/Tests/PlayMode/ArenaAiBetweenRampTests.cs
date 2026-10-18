using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public sealed class ArenaAiBetweenRampTests
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
        [UnityTest,Timeout(300000)] public IEnumerator OrdinaryBotFindsAConnectedRampFromBetweenCrossings()
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
                ArenaStage.Shape outer=default,drum=default;
                foreach(var piece in stage.Pieces)
                {
                    if(piece.Id=="drum")drum=piece.Shapes[layout];
                    if(piece.Id==(stage.Layouts[layout].Name=="entablado"?"apron":"walk"))outer=piece.Shapes[layout];
                }
                Assert.IsTrue(outer.Exists&&drum.Exists);
                Vector3 start=ArenaStageMesh.Direction(stage.Layouts[layout].Name=="entablado"?0:45)*((outer.Inner+outer.Outer)*.5f);
                Vector3 goal=ArenaStageMesh.Direction(225)*Mathf.Max(.8f,drum.Outer-1.5f);
                Assert.IsTrue(Ground(stage,layout,start,out var startY));Assert.IsTrue(Ground(stage,layout,goal,out var goalY));
                actor.ClearStatuses();actor.ClearTrip();actor.ClearStun();actor.Stamina.RefillAndClearFatigue();
                actor.Teleport(start+Vector3.up*(startY+.04f));actor.RoundActive=true;actor.Intent.Parked=false;
                Assert.IsTrue(mine.HostForceEquip(actor));Assert.IsTrue(mine.HostDisarm());
                mine.OwnerSlot=actor.PlayerSlot;mine.transform.position=goal+Vector3.up*(goalY+.08f);Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(SlipperState.Loose,mine.State);Assert.IsTrue(actor.CanAct());
                brain.enabled=true;float began=Time.realtimeSinceStartup;end=began+35;
                Vector3 resting=mine.transform.position;
                float movedLoose=0;bool bodyRecovered=false;
                float lowest=actor.transform.position.y;float best=Vector3.Distance(actor.transform.position,mine.transform.position);
                while(Time.realtimeSinceStartup<end&&mine.Holder!=actor)
                {yield return null;bodyRecovered|=actor.IsEdgeRecovering;if(mine.State==SlipperState.Loose)movedLoose=Mathf.Max(movedLoose,Vector3.Distance(resting,mine.transform.position));lowest=Mathf.Min(lowest,actor.transform.position.y);best=Mathf.Min(best,Vector3.Distance(actor.transform.position,mine.transform.position));}
                bool retrieved=mine.Holder==actor;
                string line=$"{stage.Layouts[layout].Name}: retrieved={retrieved} bodyRecovered={bodyRecovered} movedLoose={movedLoose:F3} seconds={Time.realtimeSinceStartup-began:F3} start={start} target={goal} heights={startY:F3}/{goalY:F3} final={actor.transform.position} best3d={best:F3} lowY={lowest:F3} plan={brain.Plan}";
                report.Add(line);if(!retrieved||bodyRecovered||movedLoose>.2f)failures.Add(line);
            }
            Directory.CreateDirectory("Logs/arena-betweenramps");File.WriteAllLines("Logs/arena-betweenramps/approaches.txt",report);
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
