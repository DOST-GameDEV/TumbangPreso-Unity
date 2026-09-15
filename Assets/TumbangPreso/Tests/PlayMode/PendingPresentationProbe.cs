using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class PendingPresentationProbe
    {
        private bool _bots,_spectator,_pinned;private int _seat;
        private CustomRules _rules;private INetProvider _net;
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnitySetUp]public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;_net=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown]public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();NetAuthority.Provider=_net;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest,Timeout(60000)]
        public IEnumerator SupportedPreparationsResumeTheRealBodyAndFirstPersonTimelines()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza,GameMode.HeroStrike);
            NetAuthority.Provider=new SoloProvider();var caster=GameServices.Round.PlayerAt(1);
            var rig=Object.FindFirstObjectByType<CameraRig>();rig.Follow(caster);
            string[] heroes={"dante","dante","cheska","zack"};
            for(int index=0;index<heroes.Length;index++)
            {
                string hero=heroes[index];var art=RosterBook.Load().FindPersonArt(hero);
                caster.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,hero);
                caster.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                caster.AbilitySystem.BindHero(hero);yield return null;yield return null;
                var slot=index==0?HeroAbilitySystem.Slot.Skill1:HeroAbilitySystem.Slot.Ultimate;
                var ability=index==0?caster.AbilitySystem.Kit.Skill1:caster.AbilitySystem.Kit.Ultimate;
                float elapsed=ability.Windup-.03f;
                Assert.IsTrue(caster.AbilitySystem.RestoreJoiningPreparation(slot,caster.transform.position,Vector3.forward,
                    caster.transform.position+Vector3.forward*6,.55f,.03f),hero);
                var animator=caster.GetComponentInChildren<CharacterAnimator>();
                Assert.AreEqual(ability.CastAction,typeof(CharacterAnimator).GetField("_current",Hidden).GetValue(animator));
                var mixer=(AnimationMixerPlayable)typeof(CharacterAnimator).GetField("_mixer",Hidden).GetValue(animator);
                Assert.That(mixer.GetInput(1).GetTime(),Is.EqualTo(elapsed).Within(.001),hero+" body time restarted.");
                Assert.That(mixer.GetInputWeight(1),Is.EqualTo(1).Within(.001),hero+" body was still blending from an unrelated old pose just before contact.");
                var arms=rig.GetComponentInChildren<ViewmodelArms>(true);Assert.IsNotNull(arms);
                Assert.AreEqual(ability.ViewmodelAction,typeof(ViewmodelArms).GetField("_actionName",Hidden).GetValue(arms));
                Assert.That((float)typeof(ViewmodelArms).GetField("_clipTime",Hidden).GetValue(arms),Is.EqualTo(elapsed).Within(.001),hero+" FPP time restarted.");
                caster.AbilitySystem.ResetKit();
            }
        }
    }
}
