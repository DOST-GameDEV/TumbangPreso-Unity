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
        public IEnumerator WinnerUsesOriginalRigDrawRemovesItAndReducedMotionStaysStill()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,GameMode.HeroStrike);
            GameServices.Match.AddScore(1,ScoreEvent.LataKnocked);
            SettingsStore.Current.ReducedUiMotion=false;
            var result=Object.FindAnyObjectByType<MatchResult>();Assert.IsNotNull(result);
            result.OnMatchWon(1);yield return null;
            var figure=GameObject.Find("WinnerFigure");Assert.IsNotNull(figure);
            var preview=figure.GetComponent<ModelPreview>();Assert.IsNotNull(preview.Subject);
            Assert.AreEqual(4,Object.FindObjectsByType<CharacterMotor>().Length,"Result spawned a fifth gameplay actor.");
            Assert.IsEmpty(preview.Subject.GetComponentsInChildren<Collider>(true));
            var caption=GameObject.Find("WinnerCaption").GetComponent<Text>();Assert.AreEqual("MATCH WINNER",caption.text);
            var arm=preview.Subject.GetComponentsInChildren<Transform>().First(t=>t.name=="arm-right");
            yield return new WaitForSecondsRealtime(.13f);var before=arm.localRotation;
            yield return new WaitForSecondsRealtime(.36f);
            Assert.Greater(Quaternion.Angle(before,arm.localRotation),8,"Winner acknowledgement did not animate while the match clock was stopped.");
            yield return GameplayShots.Render(Camera.main,"winner-720",true,"Logs/finish-performance",width:1280,height:720);
            var scores=Enumerable.Range(0,4).Select(GameServices.Match.ScoreFor).ToArray();
            SettingsStore.Current.ReducedUiMotion=true;yield return null;yield return null;
            before=arm.localRotation;yield return new WaitForSecondsRealtime(.35f);
            Assert.Less(Quaternion.Angle(before,arm.localRotation),.01f,"Reduced-motion result kept animating the rig.");
            CollectionAssert.AreEqual(scores,Enumerable.Range(0,4).Select(GameServices.Match.ScoreFor).ToArray());
            GameServices.Match.AddScore(2,ScoreEvent.LataKnocked);result.OnMatchWon(-1);yield return null;
            Assert.IsFalse(figure.activeSelf,"Draw retained a false winner figure.");
            Assert.AreEqual("EVEN AT THE TOP",caption.text);
            Assert.IsNotNull(GameObject.Find("DrawCan"));
            yield return GameplayShots.Render(Camera.main,"draw-16x10",true,"Logs/finish-performance",width:1280,height:800);
        }
    }
}
