using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class FamiliarReadoutTests
    {
        private bool _bots,_spectator,_pinned;private int _seat;private CustomRules _rules;
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
        [UnityTest]
        public IEnumerator PossessionNamesTheControlledFamiliarAndTheRealReturnAction()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,GameMode.HeroStrike);
            var actor=GameServices.Round.PlayerAt(1);actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"nemu");
            var art=RosterBook.Load().FindPersonArt("nemu");var visual=actor.GetComponent<CharacterVisual>();
            visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);actor.AbilitySystem.BindHero("nemu");
            Hud.Instance.Bind(actor);Hud.Instance.ShowReadyPrompt(false);actor.Intent.Parked=false;
            actor.Intent.Set(Verb.Skill2,true);yield return new WaitForSeconds(.08f);
            actor.Intent.Set(Verb.Skill2,false);yield return new WaitForSeconds(.12f);
            Assert.IsTrue(visual.Companion.IsPossessed,"The real projection was not accepted.");
            var readout=Object.FindAnyObjectByType<TumpMatchReadout>();readout.Tick(actor,false,false,false,false);
            Text Label(string name)=>readout.GetComponentsInChildren<Text>(true).First(t=>t.name==name);
            Assert.AreEqual("Controlling Kuro",Label("LocalRole").text);
            StringAssert.Contains("Nemu's body",Label("SlipperState").text);
            StringAssert.Contains("Bring Nemu to Kuro",Label("ActionPrompt").text);
            StringAssert.Contains("safe landing",Label("ActionDetail").text);
            Assert.AreEqual("Nemu stamina",Label("StaminaLabel").text);
            yield return GameplayShots.Render(Camera.main,"kuro-controls",true,"Logs/familiar-readout",width:1280,height:720);
            actor.AbilitySystem.ResetKit();yield return null;
            readout.Tick(actor,false,false,false,false);
            Assert.IsFalse(visual.Companion.IsPossessed);
            Assert.AreEqual("Attacker",Label("LocalRole").text);
            Assert.AreEqual("Stamina",Label("StaminaLabel").text);
            StringAssert.DoesNotContain("Bring Nemu",Label("ActionPrompt").text);
        }
    }
}
