using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class TournamentPenaltyTimingTests
    {
        private bool _bots,_spectator,_pinned;private int _seat;private CustomRules _rules;
        private static readonly MethodInfo Penalties=typeof(RoundDirector).GetMethod("StepTournamentPenalties",BindingFlags.Instance|BindingFlags.NonPublic);
        private static readonly MethodInfo Defense=typeof(RoundDirector).GetMethod("StepPassiveDefence",BindingFlags.Instance|BindingFlags.NonPublic);
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        private static IEnumerator Start()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var ready=Object.FindFirstObjectByType<ReadyGate>();ready.enabled=true;ready.StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            Assert.IsFalse(ready.AwaitingReady||ready.CountingDown);
            GameServices.Round.BeginRound();GameServices.Round.enabled=false;
            foreach(var player in GameServices.Round.Players){player.Intent.Clear();player.enabled=false;player.GetComponent<Carrier>().enabled=false;}
        }
        private static void Step(float dt)=>Penalties.Invoke(GameServices.Round,new object[]{dt});

        [UnityTest]
        public IEnumerator CampingUsesNewBoundaryWarnsThenPausesIncomeAndIncapacity()
        {
            yield return Start();var round=GameServices.Round;var match=GameServices.Match;
            var taya=round.PlayerAt(match.DefenderSlot);var can=round.Lata;
            taya.Teleport(can.transform.position+Vector3.right*1.49f);Assert.IsTrue(taya.CanAct());
            match.AddScore(taya.PlayerSlot,ScoreEvent.DefenseTick);
            int before=match.ScoreFor(taya.PlayerSlot);
            Step(2.49f);Assert.IsFalse(round.IsTayaCampWarningActive);
            Step(.02f);Assert.IsTrue(round.IsTayaCampWarningActive,"Warning starts at2.5seconds");
            Step(2.48f);Assert.IsFalse(round.IsTayaCampPenaltyActive);
            Step(.02f);Assert.IsTrue(round.IsTayaCampPenaltyActive);
            Step(1);Assert.AreEqual(before-5,match.ScoreFor(taya.PlayerSlot));
            Defense.Invoke(round,new object[]{1f});Assert.AreEqual(before-5,match.ScoreFor(taya.PlayerSlot),"Camping cannot keep defense income");
            float clock=round.TayaCampSeconds;taya.ApplyTagged();Step(2);
            Assert.AreEqual(clock,round.TayaCampSeconds);Assert.AreEqual(before-5,match.ScoreFor(taya.PlayerSlot));
            taya.ClearStun();taya.Teleport(can.transform.position+Vector3.right*1.75f);Step(.1f);
            Assert.IsTrue(round.IsTayaCampPenaltyActive,"Inside2metres retains the camping episode");
            taya.Teleport(can.transform.position+Vector3.right*2.01f);Step(.1f);
            Assert.AreEqual(0,round.TayaCampSeconds);Defense.Invoke(round,new object[]{1f});
            Assert.AreEqual(before+5,match.ScoreFor(taya.PlayerSlot));
            taya.Teleport(can.transform.position+Vector3.right*1.75f);Step(6);
            Assert.AreEqual(0,round.TayaCampSeconds,"Outside1.5metres cannot start a new camping episode");
        }

        [UnityTest]
        public IEnumerator LooseSlipperWarnsAtSevenPointFiveAndRingCountsToFifteen()
        {
            yield return Start();var round=GameServices.Round;var match=GameServices.Match;
            var player=round.PlayerAt(1);var shoe=player.GetComponent<Carrier>().Held;Assert.IsNotNull(shoe);
            player.Teleport(new Vector3(0,.1f,3));shoe.HostDisarm();shoe.transform.position=new Vector3(3,shoe.RestHeight,3);
            Assert.IsFalse(player.HoldingSlipper);Assert.IsTrue(player.CanAct());
            // Seed above the zero score floor so a deduction is observable.
            match.AddScore(1,ScoreEvent.LataKnocked);
            int before=match.ScoreFor(1);Step(7.49f);
            Assert.IsFalse(TournamentRules.IsSlipperWarning(round.AttackerIdleSeconds(1)));
            Step(.02f);Assert.IsTrue(TournamentRules.IsSlipperWarning(round.AttackerIdleSeconds(1)));
            Assert.AreEqual(before,match.ScoreFor(1));
            var recall=Object.FindFirstObjectByType<SlipperRecall>();Assert.IsNotNull(recall);
            round.ApplyNetworkTournamentState(0,new[]{0f,11.25f,0f,0f});recall.Track(player,shoe);
            Assert.IsTrue(recall.Drawing);Assert.AreEqual(.5f,recall.ClockFill,.002f,"The remaining7.5second grace counts on the existing shoe ring");
            var canvas=GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("Penalty-slipper-grace-960x540",canvas,960,540,false,true);
            Step(3.74f);Assert.IsFalse(TournamentRules.IsSlipperPenalty(round.AttackerIdleSeconds(1)));
            Step(.02f);Assert.IsTrue(TournamentRules.IsSlipperPenalty(round.AttackerIdleSeconds(1)));
            Step(1);Assert.AreEqual(before-5,match.ScoreFor(1));
            float clock=round.AttackerIdleSeconds(1);player.ApplyTagged();Step(3);
            Assert.AreEqual(clock,round.AttackerIdleSeconds(1));Assert.AreEqual(before-5,match.ScoreFor(1));
            player.ClearStun();shoe.HostForceEquip(player);Step(.1f);recall.Track(player,shoe);
            Assert.AreEqual(0,round.AttackerIdleSeconds(1));Assert.IsFalse(recall.Drawing);
        }
    }
}
