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

        private static IEnumerator StartFeedbackWorld()
        {
            SceneFlow.Networked=false;SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.Spectator=false;GameLaunch.AllBots=false;GameLaunch.SoloSeat=1;
            GameServices.Ensure();GameServices.Match.StartMatch();
            var round=GameServices.Round;round.enabled=false;
            round.Lata=new GameObject("Penalty can").AddComponent<Lata>();round.Lata.enabled=false;
            for(int slot=0;slot<Balance.PlayerCount;slot++)
            {
                var player=new GameObject("Penalty seat "+slot).AddComponent<CharacterMotor>();
                player.PlayerSlot=slot;player.IsDefender=slot==GameServices.Match.DefenderSlot;
                player.enabled=false;player.transform.position=new Vector3(slot*3,0,6);round.Register(player);
                player.gameObject.AddComponent<Carrier>().enabled=false;
                if(player.IsDefender)continue;
                var shoe=new GameObject("Penalty slipper "+slot).AddComponent<Slipper>();
                shoe.enabled=false;shoe.OwnerSlot=slot;Assert.IsTrue(shoe.HostForceEquip(player));
            }
            round.BeginRound();
            new GameObject("Penalty HUD").AddComponent<Hud>().Bind(round.PlayerAt(1));
            yield return null;
        }

        private static TumpMatchReadout BindHud(CharacterMotor player)
        {
            Hud.Instance.Bind(player);
            var view=Object.FindFirstObjectByType<TumpMatchReadout>();
            Assert.IsNotNull(view);
            view.Tick(player,false,false,false,false);
            return view;
        }

        private static HudBadge Marker(TumpMatchReadout view)
            =>view.Canvas.transform.Find("HitConfirmation").GetComponent<HudBadge>();

        [UnityTest]
        public IEnumerator OtherAttackersPenaltyDoesNotFlashTheDefendersCrosshair()
        {
            yield return StartFeedbackWorld();var round=GameServices.Round;var match=GameServices.Match;
            var defender=round.PlayerAt(match.DefenderSlot);
            defender.Teleport(round.Lata.transform.position+Vector3.right*3);
            var view=BindHud(defender);
            var attacker=round.PlayerAt(1);var shoe=attacker.GetComponent<Carrier>().Held;
            shoe.HostDisarm();shoe.transform.position=new Vector3(3,shoe.RestHeight,3);
            match.AddScore(1,ScoreEvent.LataKnocked);
            int ownBefore=match.ScoreFor(defender.PlayerSlot),otherBefore=match.ScoreFor(1);
            Step(14.99f);Step(.02f);Step(1);
            Assert.IsFalse(Marker(view).enabled,"Another attacker's penalty flashed the defender's crosshair.");
            view.Tick(defender,false,false,false,false);
            Assert.AreEqual(otherBefore-5,match.ScoreFor(1),"The attacker still receives the real penalty.");
            Assert.AreEqual(ownBefore,match.ScoreFor(defender.PlayerSlot));
            Assert.IsEmpty(view.WarningText,"The defender has no unresolved slipper.");
        }

        [UnityTest]
        public IEnumerator OtherDefendersPenaltyDoesNotFlashTheAttackersCrosshair()
        {
            yield return StartFeedbackWorld();var round=GameServices.Round;var match=GameServices.Match;
            var attacker=round.PlayerAt(1);var view=BindHud(attacker);
            var defender=round.PlayerAt(match.DefenderSlot);
            defender.Teleport(round.Lata.transform.position+Vector3.right);
            match.AddScore(defender.PlayerSlot,ScoreEvent.DefenseTick);
            int ownBefore=match.ScoreFor(1),otherBefore=match.ScoreFor(defender.PlayerSlot);
            Step(4.99f);Step(.02f);Step(1);
            Assert.IsFalse(Marker(view).enabled,"Another defender's camping flashed the attacker's crosshair.");
            view.Tick(attacker,false,false,false,false);
            Assert.AreEqual(otherBefore-5,match.ScoreFor(defender.PlayerSlot));
            Assert.AreEqual(ownBefore,match.ScoreFor(1));
            Assert.IsEmpty(view.WarningText,"An attacker holding their slipper is not idling.");
        }

        [UnityTest]
        public IEnumerator OwnSlipperPenaltyHasAdvanceWarningReasonAndStopsAfterRetrieval()
        {
            yield return StartFeedbackWorld();var round=GameServices.Round;var match=GameServices.Match;
            round.PlayerAt(match.DefenderSlot).Teleport(round.Lata.transform.position+Vector3.right*3);
            var player=round.PlayerAt(1);var shoe=player.GetComponent<Carrier>().Held;
            shoe.HostDisarm();shoe.transform.position=new Vector3(3,shoe.RestHeight,3);
            var view=BindHud(player);match.AddScore(1,ScoreEvent.LataKnocked);
            Step(7.5f);view.Tick(player,false,false,false,false);
            Assert.AreEqual("DO NOT IDLE - RETRIEVE YOUR SLIPPER",view.WarningText);
            Assert.IsFalse(Marker(view).enabled,"The advance warning is not a penalty tick.");
            Step(7.49f);Step(.02f);Step(1);
            Assert.IsTrue(Marker(view).enabled);
            Assert.AreEqual(UiTheme.Offense,Marker(view).color);
            view.Tick(player,false,false,false,false);
            Assert.AreEqual("DO NOT IDLE - RETRIEVE YOUR SLIPPER IMMEDIATELY",view.WarningText);
            shoe.HostForceEquip(player);Step(.1f);view.Tick(player,false,false,false,false);
            Assert.IsEmpty(view.WarningText);Assert.AreEqual(0,round.AttackerIdleSeconds(1));
        }

        [UnityTest]
        public IEnumerator ReceivedPenaltyEventsOnlyFlashTheirOwningSeat()
        {
            yield return StartFeedbackWorld();var round=GameServices.Round;
            var player=round.PlayerAt(1);var view=BindHud(player);
            GameServices.Match.ApplyNetworkScoreEvent(2,ScoreEvent.UnretrievedSlipperPenalty);
            Assert.IsFalse(Marker(view).enabled,"A received penalty belongs to its named seat.");
            GameServices.Match.ApplyNetworkScoreEvent(1,ScoreEvent.UnretrievedSlipperPenalty);
            Assert.IsTrue(Marker(view).enabled,"The receiving client's own penalty must keep its tick feedback.");
            Assert.AreEqual(UiTheme.Offense,Marker(view).color);
            Marker(view).enabled=false;
            Hud.Instance.EnterSpectatorMode();
            GameServices.Match.ApplyNetworkScoreEvent(1,ScoreEvent.UnretrievedSlipperPenalty);
            Assert.IsFalse(Marker(view).enabled,"A spectator has no personal penalty feedback.");
        }

        [UnityTest]
        public IEnumerator OwnCampingPenaltyKeepsItsReasonAndStopsAfterLeavingTheCan()
        {
            yield return StartFeedbackWorld();var round=GameServices.Round;var match=GameServices.Match;
            var defender=round.PlayerAt(match.DefenderSlot);var view=BindHud(defender);
            defender.Teleport(round.Lata.transform.position+Vector3.right);
            Step(2.5f);view.Tick(defender,false,false,false,false);
            Assert.AreEqual("DO NOT CAMP - MOVE AWAY FROM CAN",view.WarningText);
            Assert.IsFalse(Marker(view).enabled);
            Step(2.49f);Step(.02f);Step(1);
            Assert.IsTrue(Marker(view).enabled);Assert.AreEqual(UiTheme.Defense,Marker(view).color);
            view.Tick(defender,false,false,false,false);
            Assert.AreEqual("DO NOT CAMP - MOVE AWAY FROM CAN IMMEDIATELY",view.WarningText);
            defender.Teleport(round.Lata.transform.position+Vector3.right*3);
            Step(.1f);view.Tick(defender,false,false,false,false);
            Assert.IsEmpty(view.WarningText);Assert.AreEqual(0,round.TayaCampSeconds);
        }

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
