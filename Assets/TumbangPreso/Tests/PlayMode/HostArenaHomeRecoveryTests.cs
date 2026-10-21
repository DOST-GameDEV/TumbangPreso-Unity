using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class HostArenaHomeRecoveryTests
    {
        NetSession net;bool bots,spectator,networked,pinned,enabledBots;int seat;string map;
        CustomRules rules;Action<string> disconnected;int events;string reason;
        readonly Dictionary<string,object> abandon=new Dictionary<string,object>();
        [UnitySetUp] public IEnumerator Before()
        {
            foreach(string name in new[]{"Cause","RawReason","RoundNumber","TotalRounds","AuthorityRevoked","MatchWasCompleted"})abandon[name]=typeof(MatchAbandon).GetProperty(name).GetValue(null);
            bots=GameLaunch.AllBots;spectator=GameLaunch.Spectator;seat=GameLaunch.SoloSeat;networked=SceneFlow.Networked;
            pinned=SceneFlow.RulesPinned;rules=SceneFlow.SelectedRules.Clone();map=SceneFlow.SelectedMap;enabledBots=AIController.BotsEnabled;
            yield return PlayModeWorld.Reset();net=NetSession.Ensure();net.Stop();
            while(net.GetComponent<NetworkManager>().IsListening)yield return null;
            events=0;reason=null;disconnected=line=>{events++;reason=line;};NetSession.ClientDisconnected+=disconnected;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if(disconnected!=null)NetSession.ClientDisconnected-=disconnected;net?.Stop();
            while(net!=null&&net.GetComponent<NetworkManager>().IsListening)yield return null;
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots=bots;GameLaunch.Spectator=spectator;GameLaunch.SoloSeat=seat;AIController.BotsEnabled=enabledBots;
            SceneFlow.AdoptRemoteRules(rules);SceneFlow.SelectedRules.Password=rules.Password;
            if(pinned)SceneFlow.PinSelectedRules(rules);else SceneFlow.UnpinSelectedRules();
            SceneFlow.SelectedRules.Password=rules.Password;SceneFlow.SelectedMap=map;SceneFlow.Networked=networked;
            foreach(var entry in abandon)typeof(MatchAbandon).GetProperty(entry.Key).SetValue(null,entry.Value);
            abandon.Clear();Time.timeScale=1;
        }
        [UnityTest,Timeout(240000)] public IEnumerator LiveListenHostLossReturnsHomeAndTheNextOfflineMatchRuns()
        {
            Assert.IsTrue(ConvertedMatchSetup.HubEnabled,"The current Home route must be enabled.");
            GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=0;AIController.BotsEnabled=true;
            var selected=CustomGameRules.Defaults(GameMode.Classic);selected.Bots=CustomGameRules.MaxBots;selected.ManualReady=false;
            selected.Rounds=3;selected.RoundSeconds=60;SceneFlow.PinSelectedRules(selected);SceneFlow.SelectedMap=SceneFlow.Arena;
            var start=net.StartHostAsync(18776);while(!start.IsCompleted)yield return null;
            Assert.IsTrue(start.Result);Assert.IsTrue(net.IsAdmitted);Assert.IsTrue(net.IsHost);
            SceneFlow.Networked=true;net.SetLocalSeating(0,false);
            MatchRpc.Instance.HostStartMatch();Assert.IsTrue(net.Lobby.MatchInProgress);
            SceneFlow.StartMatch();float deadline=Time.realtimeSinceStartup+75;
            while(Time.realtimeSinceStartup<deadline&&
                  (SceneManager.GetActiveScene().name!=SceneFlow.Arena||GameServices.Round==null||!GameServices.Round.RoundActive||PresentationClock.Held))yield return null;
            Assert.AreEqual(SceneFlow.Arena,SceneManager.GetActiveScene().name);Assert.IsTrue(GameServices.Round.RoundActive);
            Assert.IsFalse(PresentationClock.Held);Assert.IsTrue(GameServices.Match.MatchInProgress);Assert.IsTrue(net.IsNetworked);
            float before=GameServices.Round.TimeLeft;yield return new WaitForSeconds(.5f);
            Assert.Less(GameServices.Round.TimeLeft,before,"The actual host round clock never advanced.");
            net.GetComponent<NetworkManager>().Shutdown();deadline=Time.realtimeSinceStartup+30;
            while(Time.realtimeSinceStartup<deadline&&
                  (SceneManager.GetActiveScene().name!=SceneFlow.MatchSetup||net.GetComponent<NetworkManager>().IsListening))yield return null;
            Assert.AreEqual(SceneFlow.MatchSetup,SceneManager.GetActiveScene().name,"Recovery did not load the actual hub scene.");
            yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual(1,events);Assert.IsFalse(string.IsNullOrWhiteSpace(reason));
            Assert.IsFalse(SceneFlow.Networked);Assert.IsFalse(net.IsNetworked,"Home auto-hosted a replacement session.");
            Assert.IsFalse(net.GetComponent<NetworkManager>().IsListening);Assert.IsFalse(GameServices.Round.RoundActive);
            Assert.IsFalse(GameServices.Match.MatchInProgress);Assert.IsFalse(GameServices.Match.HasCompleted,"Loss manufactured a completed result.");
            var lines=new List<string>{"liveHostClockAdvanced=True","recoveryEvents="+events,"recoveryReason="+reason,"homeScene="+SceneManager.GetActiveScene().name,"homeAutoHosted=False","abandonedSimulationRetired=True","manufacturedResult=False"};
            SceneFlow.Networked=false;GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
            SceneFlow.PinSelectedRules(selected);SceneFlow.SelectedMap=SceneFlow.Arena;SceneFlow.StartMatch();deadline=Time.realtimeSinceStartup+75;
            while(Time.realtimeSinceStartup<deadline&&
                  (SceneManager.GetActiveScene().name!=SceneFlow.Arena||!GameServices.Round.RoundActive||PresentationClock.Held))yield return null;
            Assert.AreEqual(SceneFlow.Arena,SceneManager.GetActiveScene().name);Assert.IsTrue(GameServices.Round.RoundActive);
            Assert.IsFalse(PresentationClock.Held);Assert.IsTrue(NetAuthority.ShouldResolve(),"Old abandonment disabled the next offline match.");
            Assert.IsFalse(net.IsNetworked);before=GameServices.Round.TimeLeft;yield return new WaitForSeconds(.5f);
            Assert.Less(GameServices.Round.TimeLeft,before);lines.Add("nextOfflineAuthorityAndClock=True");
            Directory.CreateDirectory("Logs/host-home");File.WriteAllLines("Logs/host-home/recovery.txt",lines);
        }
    }
}
