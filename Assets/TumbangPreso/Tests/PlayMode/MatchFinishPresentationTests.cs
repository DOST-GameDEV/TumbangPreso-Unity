using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class MatchFinishPresentationTests
    {
        private bool _bots,_spectator,_pinned,_reduced;private int _seat;private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();_reduced=SettingsStore.Current.ReducedUiMotion;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
            SettingsStore.Current.ReducedUiMotion=_reduced;
        }
        [UnityTest]
        public IEnumerator ResultsRetireWorldMarkersEvenWhenHudTrackingStops()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            Hud.Instance.ShowReadyPrompt(false);
            var actor=GameServices.Round.PlayerAt(1);var shoe=actor.GetComponent<Carrier>().Held;
            Assert.IsNotNull(shoe);shoe.HostDisarm();shoe.transform.position=actor.transform.position+actor.transform.forward*4;
            var markers=Object.FindAnyObjectByType<OffscreenIndicators>();var recall=Object.FindAnyObjectByType<SlipperRecall>();
            Assert.IsNotNull(markers);Assert.IsNotNull(recall);
            markers.UpdateArrows(actor,GameServices.Round.Lata.transform);recall.Track(actor,shoe);
            Assert.IsTrue(markers.CanMarkerVisible);Assert.IsTrue(recall.Drawing);
            GameServices.Round.EndRound();Object.FindAnyObjectByType<MatchResult>().OnMatchWon(-1);
            yield return null;yield return null;
            Assert.IsFalse(markers.CanMarkerVisible,"Live can marker remained over results.");
            Assert.IsFalse(recall.Drawing,"Last pickup ring remained over results after tracking stopped.");
            yield return GameplayShots.Render(Camera.main,"results-with-loose-shoe",true,"Logs/finish-performance",width:1280,height:720);
        }

        [UnityTest]
        public IEnumerator WinnerUsesOriginalRigDrawRemovesItAndReducedMotionStaysStill()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,GameMode.HeroStrike);
            GameServices.Match.AddScore(1,ScoreEvent.LataKnocked);
            var winner=GameServices.Round.PlayerAt(1);winner.LabelSuffix="";string winnerName=winner.DisplayName();winner.LabelSuffix=" P2";
            SettingsStore.Current.ReducedUiMotion=false;
            var result=Object.FindAnyObjectByType<MatchResult>();Assert.IsNotNull(result);
            GameServices.Round.EndRound();result.OnMatchWon(1);yield return null;
            var figure=GameObject.Find("WinnerFigure");Assert.IsNotNull(figure);
            var preview=figure.GetComponent<ModelPreview>();Assert.IsNotNull(preview.Subject);
            Assert.AreEqual(4,Object.FindObjectsByType<CharacterMotor>().Length,"Result spawned a fifth gameplay actor.");
            Assert.IsEmpty(preview.Subject.GetComponentsInChildren<Collider>(true));
            var caption=GameObject.Find("WinnerCaption").GetComponent<Text>();Assert.AreEqual("MATCH WINNER",caption.text);
            Assert.AreEqual("P2 · "+winnerName,GameObject.Find("FinisherName0").GetComponent<Text>().text,"A seat-prefixed row repeated the engine's duplicate-name suffix.");
            var arm=preview.Subject.GetComponentsInChildren<Transform>().First(t=>t.name=="arm-right");
            var sampler=figure.AddComponent<FinishPoseSample>();Quaternion before=Quaternion.identity,after=Quaternion.identity;
            yield return new WaitForSecondsRealtime(.13f);yield return Sample(()=>before=arm.localRotation);
            yield return new WaitForSecondsRealtime(.36f);yield return Sample(()=>after=arm.localRotation);
            Assert.Greater(Quaternion.Angle(before,after),8,"Winner acknowledgement did not animate while the match clock was stopped.");
            yield return GameplayShots.Render(Camera.main,"winner-720",true,"Logs/finish-performance",width:1280,height:720);
            var scores=Enumerable.Range(0,4).Select(GameServices.Match.ScoreFor).ToArray();
            SettingsStore.Current.ReducedUiMotion=true;yield return null;yield return null;
            yield return Sample(()=>before=arm.localRotation);yield return new WaitForSecondsRealtime(.35f);
            yield return Sample(()=>after=arm.localRotation);
            Assert.Less(Quaternion.Angle(before,after),.01f,"Reduced-motion result kept animating the rig.");
            CollectionAssert.AreEqual(scores,Enumerable.Range(0,4).Select(GameServices.Match.ScoreFor).ToArray());
            GameServices.Match.AddScore(2,ScoreEvent.LataKnocked);result.OnMatchWon(-1);yield return null;
            Assert.IsFalse(figure.activeSelf,"Draw retained a false winner figure.");
            Assert.IsFalse(preview.PreviewCamera.enabled,"Hidden result figure kept rendering its isolated stage.");
            Assert.AreEqual("EVEN AT THE TOP",caption.text);
            Assert.IsNotNull(GameObject.Find("DrawCan"));
            yield return GameplayShots.Render(Camera.main,"draw-16x10",true,"Logs/finish-performance",width:1280,height:800);
            IEnumerator Sample(Action read)
            {
                bool done=false;sampler.Read=()=>{read();done=true;};
                while(!done)yield return null;
            }
        }
        // Sample the presented pose after its overlay, not the temporarily
        // restored imported idle pose between Update and LateUpdate.
        [DefaultExecutionOrder(10000)] private sealed class FinishPoseSample:MonoBehaviour
        {
            public Action Read;
            private void LateUpdate(){var read=Read;Read=null;read?.Invoke();}
        }
    }
}
