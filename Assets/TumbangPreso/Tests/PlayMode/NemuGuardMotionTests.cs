using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class NemuGuardMotionTests
    {
        int _quality,_seat,_captureRate;bool _bots,_spectator,_pinned,_reduced;
        CustomRules _rules;Camera _observer;INetProvider _provider;Color _ambient;
        [UnitySetUp] public IEnumerator Before()
        {
            _reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            _provider=NetAuthority.Provider;_ambient=RenderSettings.ambientLight;
            _quality=QualitySettings.GetQualityLevel();_seat=GameLaunch.SoloSeat;
            _captureRate=Time.captureFramerate;_bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;
            _rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            Time.captureFramerate=_captureRate;
            if(_observer!=null)Object.Destroy(_observer.gameObject);
            yield return PlayModeWorld.Reset();
            Settings.SettingsStore.Current.ReducedUiMotion=_reduced;
            NetAuthority.Provider=_provider;RenderSettings.ambientLight=_ambient;
            QualitySettings.SetQualityLevel(_quality,true);GameLaunch.SoloSeat=_seat;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;
            SceneFlow.AdoptRemoteRules(_rules);if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest,Timeout(120000)] public IEnumerator CatchUsesItsOwnSmallCommandAndKeepsCanProtection()=>Review(false);
        [UnityTest,Timeout(60000)] public IEnumerator SmallStagePlaysRegisteredGuardAndKeepsProtection()=>Review(true);
        IEnumerator Review(bool smallStage)
        {
            int low=System.Array.IndexOf(QualitySettings.names,"Low");if(low>=0)QualitySettings.SetQualityLevel(low,true);
            if(smallStage)
            {
                NetAuthority.Provider=new SoloProvider();GameLaunch.SoloSeat=1;
                SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                GameServices.Ensure();GameServices.Round.Clear();
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(24,1,24);
                var lata=new GameObject("Guard stage can").AddComponent<Lata>();GameServices.Round.Lata=lata;
                var root=new GameObject("Guard stage actor",typeof(CharacterController));
                var cc=root.GetComponent<CharacterController>();cc.height=1.6f;cc.radius=.35f;cc.center=new Vector3(0,.8f,0);
                var motor=root.AddComponent<CharacterMotor>();motor.PlayerSlot=1;motor.Mode=GameMode.HeroStrike;motor.IsBot=false;
                root.AddComponent<Carrier>();root.AddComponent<CombatVerbs>();root.AddComponent<HeroAbilitySystem>().BindHero("nemu");
                root.AddComponent<CharacterVisual>();GameServices.Round.Register(motor);
                GameServices.Match.StartMatch();GameServices.Round.BeginRound();
                var owner=new GameObject("Guard stage owner");owner.tag="MainCamera";owner.AddComponent<CameraRig>();
                var sun=new GameObject("Guard stage light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;
                sun.transform.rotation=Quaternion.Euler(40,-30,0);RenderSettings.ambientLight=new Color(.55f,.57f,.62f);
                yield return null;
            }
            else
            {
                yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
                Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
            }
            foreach(var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
            foreach(var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))input.enabled=false;
            var actor=GameServices.Round.PlayerAt(1);var can=GameServices.Round.Lata;
            foreach(var other in GameServices.Round.Players)
            {other.Intent.Clear();other.Intent.Parked=true;if(other!=actor){other.IsDefender=false;other.Teleport(new Vector3(6,.18f,8+other.PlayerSlot));}}
            actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"nemu");actor.IsDefender=true;
            var art=RosterBook.Load().FindPersonArt("nemu");
            actor.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            actor.AbilitySystem.BindHero("nemu");actor.Teleport(new Vector3(0,.18f,-3));actor.transform.rotation=Quaternion.identity;
            actor.Intent.Parked=false;actor.Intent.AimPoint=can.transform.position;actor.Intent.FaceAimPoint=true;
            Camera.main.GetComponent<CameraRig>().Follow(actor,true);
            Camera.main.GetComponent<CameraRig>().SetAimSource(AimSource.Movement);
            yield return new WaitForSeconds(.25f);
            var pet=actor.GetComponent<CharacterVisual>().Companion;Assert.IsNotNull(pet);Assert.IsTrue(can.IsUpright);
            var kit=(NemuHeroKit)actor.AbilitySystem.Kit;
            _observer=new GameObject("Nemu command observer").AddComponent<Camera>();_observer.enabled=false;_observer.fieldOfView=55;
            _observer.transform.position=actor.transform.position+new Vector3(4,2.1f,4);
            _observer.transform.LookAt(actor.transform.position+new Vector3(0,.9f,1));
            string directory=System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/nemu-catch-review";
            Settings.SettingsStore.Current.ReducedUiMotion=false;
            var arms=Object.FindFirstObjectByType<ViewmodelArms>();Assert.IsNotNull(arms);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var handClip=typeof(ViewmodelArms).GetField("_clip",flags);
            var handAction=typeof(ViewmodelArms).GetField("_actionName",flags);
            var handOffset=typeof(ViewmodelArms).GetField("_castLeft",flags);
            bool sawOwnerGesture=false;float largestOwnerOffset=0;
            Time.captureFramerate=60;
            actor.Intent.Set(Verb.Skill2,true);actor.Intent.BufferPress(Verb.Skill2);yield return null;
            actor.Intent.Set(Verb.Skill2,false);bool protectedCan=false,sawGuardMotion=false;
            for(int frame=0;frame<24;frame++)
            {
                actor.Intent.AimPoint=can.transform.position;actor.Intent.FaceAimPoint=true;
                protectedCan|=can.IsProtected;
                sawOwnerGesture|=(string)handAction.GetValue(arms)=="kuro-guard"&&handClip.GetValue(arms)!=null;
                largestOwnerOffset=Mathf.Max(largestOwnerOffset,((Vector3)handOffset.GetValue(arms)).magnitude);
                sawGuardMotion|=actor.GetComponentInChildren<CharacterAnimator>().CurrentClipName=="hero-nemu-guard";
                yield return GameplayShots.Render(Camera.main,"owner-"+frame.ToString("D3"),false,directory,null,960,540);
                yield return GameplayShots.Render(_observer,"observer-"+frame.ToString("D3"),false,directory,actor,960,540);
            }
            Assert.AreEqual(HeroKit.CastOutcome.Cast,actor.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Skill2));
            Assert.IsTrue(protectedCan);Assert.IsTrue(kit.DefendingSkill.IsActive);
            Assert.AreEqual(5f,kit.DefendingSkill.Duration);Assert.AreSame(pet,actor.GetComponent<CharacterVisual>().Companion);
            Assert.AreEqual("hero-nemu-seance",kit.Ultimate.CastAction,"Haunt must keep its original action.");
            kit.DefendingSkill.ResetForRound(new AbilityContext(actor,actor.GetComponent<Carrier>(),actor.GetComponent<CombatVerbs>()));
            yield return null;Assert.IsFalse(can.IsProtected,"Cancelled Catch left can protection behind.");
            Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"scope.txt"),
                (smallStage?"Lightweight stage":"Eskinita")+"; actual accepted defender input and can protection/reset; fixed 60Hz paired body and real owner views. Not a player/peer/device/audio verdict.\n");
            Assert.AreEqual("hero-nemu-guard",kit.DefendingSkill.CastAction,"Catch still borrows Haunt's large seance.");
            Assert.AreEqual("kuro-guard",kit.DefendingSkill.ViewmodelAction);
            Assert.IsTrue(sawOwnerGesture,"The first-person command was never admitted to the real clip player.");
            Assert.Greater(largestOwnerOffset,.1f,"The authored free-hand path never moved the actual pivot.");
            Assert.IsTrue(sawGuardMotion,"Registered guard action never reached the live animator.");
            Assert.IsTrue(art.Clips.Any(c=>c!=null&&c.name=="hero-nemu-guard"&&c.length>.5f),"Shipping roster must bind the authored guard clip.");
        }
    }
}
